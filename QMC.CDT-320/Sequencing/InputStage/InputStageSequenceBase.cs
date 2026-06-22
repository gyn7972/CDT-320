using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;

namespace QMC.CDT320.Sequencing
{
    internal abstract class InputStageSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "InputStageSequence";

        protected InputStageSequenceBase(
            MachineSequenceContext context,
            InputStageSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputStageSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputStageSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected InputStageUnit Stage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null; }
        }

        protected PickerFrontUnit FrontPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null; }
        }

        protected PickerRearUnit RearPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.InputSeq, Name, () => CurrentStep.ToString()))
            try
            {
                Options = options ?? InputStageSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-STAGE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[INPUT-STAGE] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input stage " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input stage " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-EX", Name, "Input stage " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckUnit(TStep nextStep)
        {
            if (Stage == null)
                return Fail("IN-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            string axisReason = BuildRequiredAxisAvailabilityReason();
            if (!string.IsNullOrEmpty(axisReason))
                return Fail("IN-STAGE-AXIS", Stage.Name, "Input stage axis is not available. " + axisReason);

            axisReason = BuildRequiredAxisAlarmReason();
            if (!string.IsNullOrEmpty(axisReason))
                return Fail("IN-STAGE-ALARM", Stage.Name, "Input stage axis alarm exists. " + axisReason);

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> MoveLoadPositionAsync(CancellationToken ct)
        {
            if (Options.EnableMotion)
            {
                int pickerReady = await WaitPickersClearForInputTransportAsync("InputStage Load 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

                int result = await Stage.LoadAndPrepareWaferAsync(
                    Options.WaferId,
                    Options.RequireMapData,
                    Options.FineMove,
                    () => WaitPickersClearForInputTransportAsync("InputStage Load Z 이동 직전", ct)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-LOAD-PREP", Stage.Name, "Input stage load prepare failed. result=" + result + BuildStageMoveFailureDetail());
            }

            Context.Bus.Set("InputStageLoadPrepared");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveAlignPositionAsync(TStep nextStep)
        {
            if (Options.EnableMotion)
            {
                int result = await Stage.VisionAlignAndSetupOriginAsync(Options.RequireVisionAlign, Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-ALIGN", Stage.Name, "Input stage vision align/setup failed. result=" + result);
            }

            CurrentStep = nextStep;
            return 0;
        }

        protected int BuildDiePositions()
        {
            Context.Bus.Set("InputStageAligned");
            Context.Bus.Set("InputStageReady");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveUnloadPositionAsync(CancellationToken ct)
        {
            if (Options.EnableMotion)
            {
                int pickerReady = await WaitPickersClearForInputTransportAsync("InputStage Unload 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

                int result = await Stage.PrepareUnloadWaferAsync(
                    Options.FineMove,
                    () => WaitPickersClearForInputTransportAsync("InputStage Unload Z 이동 직전", ct)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-STAGE-UNLOAD-PREP", Stage.Name, "Input stage unload prepare failed. result=" + result);
            }

            Context.Bus.Set("InputStageUnloadPrepared");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> MoveAvoidPositionAsync(CancellationToken ct)
        {
            if (Options.EnableMotion)
            {
                ct.ThrowIfCancellationRequested();
                int result = await MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis.NeedleZ, Stage.Recipe.NeedleZ.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis.NeedleZ, Stage.Recipe.NeedleZ.AvoidPosition, ct).ConfigureAwait(false);
                if (result != 0) return result;

                ct.ThrowIfCancellationRequested();
                result = await MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis.WaferY, Stage.Recipe.WaferY.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis.WaferY, Stage.Recipe.WaferY.AvoidPosition, ct).ConfigureAwait(false);
                if (result != 0) return result;

                ct.ThrowIfCancellationRequested();
                result = await MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition).ConfigureAwait(false);
                if (result != 0) return result;

                result = await WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis.VisionX, Stage.Recipe.VisionX.AvoidPosition, ct).ConfigureAwait(false);
                if (result != 0) return result;
            }

            Context.Bus.Set("InputStageAvoidReady");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected int FailUnsupportedStep()
        {
            return Fail("IN-STAGE-STEP", Name, "Unsupported input stage step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[INPUT-STAGE] FAIL " + alarmCode + " - " + message);
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        protected async Task<int> WaitPickersClearForInputTransportAsync(string description, CancellationToken ct)
        {
            try
            {
                if (FrontPicker == null)
                    return Fail("IN-STAGE-PICKER-MISSING", "FrontPicker", description + " 전 FrontPicker 유닛을 확인할 수 없습니다.");

                if (RearPicker == null)
                    return Fail("IN-STAGE-PICKER-MISSING", "RearPicker", description + " 전 RearPicker 유닛을 확인할 수 없습니다.");

                int timeoutMs = ResolveTimeout();
                DateTime startTime = DateTime.UtcNow;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    string frontDetail;
                    string rearDetail;
                    bool frontBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        true,
                        PickerWorkZone.Input,
                        out frontDetail);
                    bool rearBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        false,
                        PickerWorkZone.Input,
                        out rearDetail);

                    bool pickerTransportMoving = IsPickerTransportAxisMoving();
                    if (!frontBlocking && !rearBlocking && !pickerTransportMoving)
                        return 0;

                    string alarmState = BuildPickerAlarmState();
                    if (!string.IsNullOrEmpty(alarmState))
                    {
                        return Fail("IN-STAGE-PICKER-ALARM", Name,
                            description + " 대기 불가: Picker 축 알람 상태입니다. " +
                            "front=" + frontDetail + ", rear=" + rearDetail + ", " + alarmState);
                    }

                    double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("IN-STAGE-PICKER-INPUT-ZONE-TIMEOUT", Name,
                            description + " 대기 시간 초과: Picker가 Input zone에서 벗어나지 않았거나 X/Y 이동 중입니다. " +
                            "timeoutMs=" + timeoutMs + ", " +
                            "front=" + frontDetail + ", rear=" + rearDetail + ", " + BuildPickerMotionState());
                    }

                    if (!waitLogged)
                    {
                        WriteLog(Name,
                            description + " 전 Picker Input zone 해제 대기. " +
                            "front=" + frontDetail + ", rear=" + rearDetail +
                            ", pickerTransportMoving=" + pickerTransportMoving +
                            ", timeoutMs=" + timeoutMs + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(50, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-PICKER-INPUT-ZONE-WAIT-EX", Name,
                    description + " 전 Picker Input zone 해제 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisCommandAsync(QMC.CDT320.WaferStageAxis axis, double target)
        {
            int result = await Stage.MoveInputStageAxis(axis, target, Options.FineMove).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-STAGE-MOVE", Stage.Name, "Input stage axis move failed. axis=" + axis + ", target=" + target + ", result=" + result);

            return 0;
        }

        private string BuildStageMoveFailureDetail()
        {
            try
            {
                if (Stage == null || string.IsNullOrWhiteSpace(Stage.LastStageMoveFailureMessage))
                    return string.Empty;

                return ", lastStageMoveFailure=" + Stage.LastStageMoveFailureMessage;
            }
            catch (Exception ex)
            {
                return ", lastStageMoveFailure read failed. error=" + ex.Message;
            }
            finally
            {
            }
        }

        private async Task<int> WaitAxisInPositionResultAsync(QMC.CDT320.WaferStageAxis axis, double target, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await Stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-STAGE-MOVE", waitResult), Stage.Name,
                        "Input stage axis 이동 완료/위치 확인 실패. axis=" + axis + ", target=" + target + ". " +
                        FormatAxisMoveWaitResult(waitResult, BuildAxisState(axis, target)));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-STAGE-MOVE-WAIT-EX", Stage != null ? Stage.Name : "InputStage",
                    "Input stage axis 이동 완료 대기 중 예외가 발생했습니다. axis=" + axis +
                    ", target=" + target +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(QMC.CDT320.WaferStageAxis axis)
        {
            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case QMC.CDT320.WaferStageAxis.WaferY: return Stage.StageY;
                // 웨이퍼 T축 반환
                case QMC.CDT320.WaferStageAxis.WaferT: return Stage.StageT;
                // 웨이퍼 확장 Z축 반환
                case QMC.CDT320.WaferStageAxis.WaferExpandingZ: return Stage.ExpanderZ;
                // 비전 X축 반환
                case QMC.CDT320.WaferStageAxis.VisionX: return Stage.CameraX;
                // 니들 X축 반환
                case QMC.CDT320.WaferStageAxis.NeedleX: return Stage.NeedleBlockX;
                // 니들 Z축 반환
                case QMC.CDT320.WaferStageAxis.NeedleZ: return Stage.NeedleZ;
                // 이젝트 핀 Z축 반환
                case QMC.CDT320.WaferStageAxis.EjectPinZ: return Stage.EjectPinZ;
                default: return null;
            }
        }

        protected string BuildAxisState(QMC.CDT320.WaferStageAxis axis, double target)
        {
            return BuildAxisState(axis.ToString(), ResolveStageAxis(axis), target);
        }

        protected static string BuildAxisState(string label, QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return label + "=null";

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            return label +
                "[name=" + axis.Name +
                ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                ", moving=" + (axis.IsMoving ? "Y" : "N") +
                ", actual=" + axis.ActualPosition +
                ", target=" + target +
                ", tolerance=" + tolerance +
                "]";
        }

        private string BuildPickerAlarmState()
        {
            string reason = string.Empty;
            AppendPickerAxisAlarm(ref reason, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerT0", FrontPicker != null ? FrontPicker.PickerT0 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerT1", FrontPicker != null ? FrontPicker.PickerT1 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerT2", FrontPicker != null ? FrontPicker.PickerT2 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerT3", FrontPicker != null ? FrontPicker.PickerT3 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerZ0", FrontPicker != null ? FrontPicker.PickerZ0 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerZ1", FrontPicker != null ? FrontPicker.PickerZ1 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerZ2", FrontPicker != null ? FrontPicker.PickerZ2 : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerZ3", FrontPicker != null ? FrontPicker.PickerZ3 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerT0", RearPicker != null ? RearPicker.PickerT0 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerT1", RearPicker != null ? RearPicker.PickerT1 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerT2", RearPicker != null ? RearPicker.PickerT2 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerT3", RearPicker != null ? RearPicker.PickerT3 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerZ0", RearPicker != null ? RearPicker.PickerZ0 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerZ1", RearPicker != null ? RearPicker.PickerZ1 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerZ2", RearPicker != null ? RearPicker.PickerZ2 : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerZ3", RearPicker != null ? RearPicker.PickerZ3 : null);
            return reason;
        }

        private bool IsPickerTransportAxisMoving()
        {
            return IsAxisMoving(FrontPicker != null ? FrontPicker.PickerX : null) ||
                   IsAxisMoving(FrontPicker != null ? FrontPicker.PickerY : null) ||
                   IsAxisMoving(RearPicker != null ? RearPicker.PickerX : null) ||
                   IsAxisMoving(RearPicker != null ? RearPicker.PickerY : null);
        }

        private static bool IsAxisMoving(QMC.Common.Motion.BaseAxis axis)
        {
            return axis != null && axis.IsMoving;
        }

        private string BuildPickerMotionState()
        {
            string state = string.Empty;
            AppendPickerAxisMotion(ref state, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisMotion(ref state, "FrontPickerT0", FrontPicker != null ? FrontPicker.PickerT0 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerT1", FrontPicker != null ? FrontPicker.PickerT1 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerT2", FrontPicker != null ? FrontPicker.PickerT2 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerT3", FrontPicker != null ? FrontPicker.PickerT3 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerZ0", FrontPicker != null ? FrontPicker.PickerZ0 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerZ1", FrontPicker != null ? FrontPicker.PickerZ1 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerZ2", FrontPicker != null ? FrontPicker.PickerZ2 : null);
            AppendPickerAxisMotion(ref state, "FrontPickerZ3", FrontPicker != null ? FrontPicker.PickerZ3 : null);
            AppendPickerAxisMotion(ref state, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            AppendPickerAxisMotion(ref state, "RearPickerT0", RearPicker != null ? RearPicker.PickerT0 : null);
            AppendPickerAxisMotion(ref state, "RearPickerT1", RearPicker != null ? RearPicker.PickerT1 : null);
            AppendPickerAxisMotion(ref state, "RearPickerT2", RearPicker != null ? RearPicker.PickerT2 : null);
            AppendPickerAxisMotion(ref state, "RearPickerT3", RearPicker != null ? RearPicker.PickerT3 : null);
            AppendPickerAxisMotion(ref state, "RearPickerZ0", RearPicker != null ? RearPicker.PickerZ0 : null);
            AppendPickerAxisMotion(ref state, "RearPickerZ1", RearPicker != null ? RearPicker.PickerZ1 : null);
            AppendPickerAxisMotion(ref state, "RearPickerZ2", RearPicker != null ? RearPicker.PickerZ2 : null);
            AppendPickerAxisMotion(ref state, "RearPickerZ3", RearPicker != null ? RearPicker.PickerZ3 : null);
            return state;
        }

        private static void AppendPickerAxisAlarm(ref string reason, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null || !axis.IsAlarm)
                return;

            if (reason.Length > 0)
                reason += " ";

            reason += BuildAxisState(label, axis, axis.ActualPosition) + ";";
        }

        private static void AppendPickerAxisMotion(ref string state, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return;

            if (state.Length > 0)
                state += " ";

            state += label +
                "(servo=" + axis.IsServoOn +
                ", alarm=" + axis.IsAlarm +
                ", moving=" + axis.IsMoving +
                ", actual=" + axis.ActualPosition +
                ");";
        }

        protected static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, waitResult);
        }

        protected static string FormatAxisMoveWaitResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            return AxisMoveWaiter.FormatResult(waitResult, fallbackState);
        }

        private string BuildRequiredAxisAvailabilityReason()
        {
            string reason = string.Empty;
            AppendMissingAxis(ref reason, "StageY", Stage.StageY);
            AppendMissingAxis(ref reason, "StageT", Stage.StageT);
            AppendMissingAxis(ref reason, "ExpanderZ", Stage.ExpanderZ);
            AppendMissingAxis(ref reason, "CameraX", Stage.CameraX);
            return reason;
        }

        private string BuildRequiredAxisAlarmReason()
        {
            string reason = string.Empty;
            AppendAlarmAxis(ref reason, "StageY", Stage.StageY);
            AppendAlarmAxis(ref reason, "StageT", Stage.StageT);
            AppendAlarmAxis(ref reason, "ExpanderZ", Stage.ExpanderZ);
            AppendAlarmAxis(ref reason, "CameraX", Stage.CameraX);
            return reason;
        }

        private static void AppendMissingAxis(ref string reason, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis != null)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += label + "=null;";
        }

        private static void AppendAlarmAxis(ref string reason, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null || !axis.IsAlarm)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += BuildAxisState(label, axis, axis.ActualPosition) + ";";
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private TStep ResolveStartStep(TStep initialStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence forced restart from step=" + initialStep + ". - Ok");
                    return initialStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
                TStep step;
                if (Enum.TryParse(saved, out step) &&
                    !IsStep(step, IdleStep) &&
                    !IsStep(step, CompleteStep) &&
                    !IsStep(step, ErrorStep))
                {
                    WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence resume step=" + step + ". - Ok");
                    return step;
                }

                return initialStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Input stage " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
                return initialStep;
            }
            finally
            {
            }
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitIntAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAxisWaitAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsStep(TStep left, TStep right)
        {
            return object.Equals(left, right);
        }

        protected static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
