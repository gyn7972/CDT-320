using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal abstract class OutputStageSequenceBase<TStep> where TStep : struct
    {
        private const string SequenceNamePrefix = "OutputStageSequence";

        protected OutputStageSequenceBase(MachineSequenceContext context, OutputStageSequenceKind kind, string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected OutputStageSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected OutputStageSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected OutputStageUnit Stage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null; }
        }

        protected PickerFrontUnit FrontPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null; }
        }

        protected PickerRearUnit RearPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null; }
        }

        protected OutputFeederUnit OutputFeeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputFeederUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.OutputSeq, Name, () => CurrentStep.ToString()))
            try
            {
                Options = options ?? OutputStageSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-STAGE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);
                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Output stage " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output stage " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-EX", Name, "Output stage " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckUnit(TStep nextStep)
        {
            try
            {
                var stage = Stage;
                if (stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");
                string axisReason = BuildRequiredAxisReason();
                if (!string.IsNullOrEmpty(axisReason))
                    return Fail("OUT-STAGE-AXIS-MISSING", stage.Name, "Output stage axis is not available. " + axisReason);

                axisReason = BuildAxisServoAlarmReason();
                if (!string.IsNullOrEmpty(axisReason))
                    return Fail("OUT-STAGE-AXIS-READY", stage.Name, "Output stage axis is not ready. " + axisReason);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-CHECK-EX", Name, "Output stage check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveSideAxesAsync(string positionName, TStep nextStep, CancellationToken ct)
        {
            try
            {
                var stage = Stage;
                if (stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");

                if (Options.Side == BinSide.Ng)
                {
                    int result = await MoveAxisAndVerifyAsync(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, positionName), positionName + " NG Y", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = nextStep;
                    return 0;
                }

                int move = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinY, ResolveTarget(BinStageAxis.GoodBinY, positionName), positionName + " Good Y", ct).ConfigureAwait(false);
                if (move != 0) return move;
                move = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinZ, ResolveTarget(BinStageAxis.GoodBinZ, positionName), positionName + " Good Z", ct).ConfigureAwait(false);
                if (move != 0) return move;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-MOVE-EX", Name, positionName + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveAllAvoidAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinZ, ResolveTarget(BinStageAxis.GoodBinZ, "Avoid"), "Good Z avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.GoodBinY, ResolveTarget(BinStageAxis.GoodBinY, "Avoid"), "Good Y avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, "Avoid"), "NG Y avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;
                result = await MoveAxisAndVerifyAsync(BinStageAxis.VisionX, ResolveTarget(BinStageAxis.VisionX, "Avoid"), "VisionX avoid", ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-EX", Name, "Move avoid exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveVisionProcessAsync(TStep nextStep, CancellationToken ct)
        {
            int result = await MoveAxisAndVerifyAsync(BinStageAxis.VisionX, ResolveTarget(BinStageAxis.VisionX, "Process"), "VisionX process", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = nextStep;
            return 0;
        }

        protected int Complete()
        {
            CurrentStep = CompleteStep;
            return 0;
        }

        protected int FailUnsupportedStep()
        {
            return Fail("OUT-STAGE-STEP", Name, "Unsupported output stage step: " + CurrentStep);
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
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[OUTPUT-STAGE] FAIL " + alarmCode + " - " + message);
            }
            catch (Exception ex)
            {
                WriteLog(source, "OutputStage 실패 처리 중 예외가 발생했습니다. alarmCode=" +
                    alarmCode + ", message=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        protected async Task<int> WaitPickersClearForOutputTransportAsync(string description, CancellationToken ct)
        {
            try
            {
                if (FrontPicker == null)
                    return Fail("OUT-STAGE-PICKER-MISSING", "FrontPicker", description + " 전 FrontPicker 유닛을 확인할 수 없습니다.");

                if (RearPicker == null)
                    return Fail("OUT-STAGE-PICKER-MISSING", "RearPicker", description + " 전 RearPicker 유닛을 확인할 수 없습니다.");

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
                        PickerWorkZone.Output,
                        out frontDetail);
                    bool rearBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        false,
                        PickerWorkZone.Output,
                        out rearDetail);

                    if (!frontBlocking && !rearBlocking)
                        return 0;

                    string alarmState = BuildPickerTransportAlarmState();
                    if (!string.IsNullOrEmpty(alarmState))
                    {
                        return Fail("OUT-STAGE-PICKER-ALARM", Name,
                            description + " 대기 불가: Picker 축 알람 상태입니다. " +
                            "front=" + frontDetail + ", rear=" + rearDetail + ", " + alarmState);
                    }

                    double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("OUT-STAGE-PICKER-OUTPUT-ZONE-TIMEOUT", Name,
                            description + " 대기 시간 초과: Picker가 Output zone에서 벗어나지 않았습니다. " +
                            "timeoutMs=" + timeoutMs + ", front=" + frontDetail +
                            ", rear=" + rearDetail + ", " + BuildPickerTransportMotionState());
                    }

                    if (!waitLogged)
                    {
                        WriteLog(Name,
                            description + " 전 Picker Output zone 해제 대기. " +
                            "front=" + frontDetail + ", rear=" + rearDetail +
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
                return Fail("OUT-STAGE-PICKER-OUTPUT-ZONE-WAIT-EX", Name,
                    description + " 전 Picker Output zone 해제 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildPickerTransportAlarmState()
        {
            string reason = string.Empty;
            AppendPickerAxisAlarm(ref reason, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisAlarm(ref reason, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            return reason;
        }

        private string BuildPickerTransportMotionState()
        {
            string state = string.Empty;
            AppendPickerAxisMotion(ref state, "FrontPickerX", FrontPicker != null ? FrontPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "FrontPickerY", FrontPicker != null ? FrontPicker.PickerY : null);
            AppendPickerAxisMotion(ref state, "RearPickerX", RearPicker != null ? RearPicker.PickerX : null);
            AppendPickerAxisMotion(ref state, "RearPickerY", RearPicker != null ? RearPicker.PickerY : null);
            return state;
        }

        private static void AppendPickerAxisAlarm(ref string reason, string label, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null || !axis.IsAlarm)
                return;

            if (reason.Length > 0)
                reason += " ";

            reason += label +
                "(servo=" + axis.IsServoOn +
                ", alarm=" + axis.IsAlarm +
                ", moving=" + axis.IsMoving +
                ", actual=" + axis.ActualPosition +
                ");";
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

        protected async Task<int> MoveAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Stage != null && !Stage.HasStageAxis(axis))
                {
                    if (axis == BinStageAxis.NgBinZ)
                    {
                        WriteLog(Name, description + " skipped because NG stage has no Z axis. axis=" + axis + " - Ok");
                        return 0;
                    }

                    return Fail("OUT-STAGE-AXIS-MISSING", Stage.Name,
                        description + " axis does not exist. axis=" + axis);
                }

                if (axis == BinStageAxis.NgBinY)
                {
                    int clearResult = await EnsureNgStageYMoveClearAsync(description, ct).ConfigureAwait(false);
                    if (clearResult != 0)
                        return clearResult;
                }

                int result = await AwaitStepWithCancellationAsync(Stage.MoveStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-MOVE", Stage.Name,
                        description + " 이동 명령 실패. axis=" + axis + ", target=" + target +
                        ", result=" + result + ". " + BuildAxisState(axis, target) + ". " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));

                AxisMoveWaitResult waitResult = await Stage.WaitStageAxisMoveDoneInPosition(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("OUT-STAGE-MOVE", waitResult), Stage.Name,
                        description + " 이동 완료/위치 확인 실패. axis=" + axis + ", target=" + target +
                        ". " + FormatAxisMoveWaitResult(waitResult, BuildAxisState(axis, target)));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-MOVE-EX", Stage != null ? Stage.Name : "OutputStage",
                    description + " 이동 처리 중 예외가 발생했습니다. axis=" + axis +
                    ", target=" + target +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureNgStageYMoveClearAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");

                int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        description + " 전 NG Clamp Lift 상승 명령 실패. result=" + result + ", " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        description + " 전 NG Clamp Lift 상승 확인 실패. " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));

                result = await Stage.EnsureBinGuideDownAsync(BinSide.Good, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-GOOD-GUIDE-DOWN", Stage.Name,
                        description + " 전 Good Guide Down 명령 실패. result=" + result + ", " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));

                if (!Stage.IsBinGuideDown(BinSide.Good))
                    return Fail("OUT-STAGE-GOOD-GUIDE-DOWN", Stage.Name,
                        description + " 전 Good Guide Down 확인 실패. " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));

                if (!Stage.IsGoodStageZInAvoidPosition())
                {
                    result = await Stage.MoveGoodStageZToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("OUT-STAGE-GOOD-Z-SAFE", Stage.Name,
                            description + " 전 Good Stage Z 안전 위치 이동 실패. result=" + result + ", " +
                            Stage.DescribeOutputStageInterlockState(BinSide.Ng));
                }

                if (!Stage.IsGoodStageZInAvoidPosition())
                    return Fail("OUT-STAGE-GOOD-Z-SAFE", Stage.Name,
                        description + " 전 Good Stage Z 안전 위치 확인 실패. " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-Y-CLEAR-EX", Stage != null ? Stage.Name : "OutputStage",
                    description + " 전 NG Stage Y 이동 조건 확보 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        protected double ResolveTarget(BinStageAxis axis, string positionName)
        {
            if (axis == BinStageAxis.NgBinZ)
            {
                return 0.0;
            }

            return Stage.GetStageTeachingPosition(axis, positionName);
        }

        protected double ResolveTolerance(BinStageAxis axis)
        {
            switch (axis)
            {
                // NG 스테이지 Y축 허용오차 반환
                case BinStageAxis.NgBinY:
                    return Stage.NgStage.StageY.Config != null ? Stage.NgStage.StageY.Config.InPositionTolerance : 0.01;
                // NG 스테이지 Z축 허용오차 반환
                case BinStageAxis.NgBinZ:
                    return 0.01;
                // GOOD 스테이지 Y축 허용오차 반환
                case BinStageAxis.GoodBinY:
                    return Stage.GoodStage.StageY.Config != null ? Stage.GoodStage.StageY.Config.InPositionTolerance : 0.01;
                // GOOD 스테이지 Z축 허용오차 반환
                case BinStageAxis.GoodBinZ:
                    return Stage.GoodStage.StageZ.Config != null ? Stage.GoodStage.StageZ.Config.InPositionTolerance : 0.01;
                // 비전 X축 허용오차 반환
                case BinStageAxis.VisionX:
                    return Stage.OutputCameraX.Config != null ? Stage.OutputCameraX.Config.InPositionTolerance : 0.01;
                default:
                    return 0.01;
            }
        }

        protected string BuildAxisState(BinStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveAxisOrNull(axis);
            if (item == null)
                return axis + "=null";

            double tolerance = ResolveTolerance(axis);
            return axis +
                   "[name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   "]";
        }

        protected static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, waitResult);
        }

        protected static string FormatAxisMoveWaitResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            return AxisMoveWaiter.FormatResult(waitResult, fallbackState);
        }

        private string BuildRequiredAxisReason()
        {
            string reason = string.Empty;
            AppendMissingAxis(ref reason, "GoodStage", Stage.GoodStage);
            AppendMissingAxis(ref reason, "NgStage", Stage.NgStage);
            AppendMissingAxis(ref reason, "GoodStageY", Stage.GoodStage != null ? Stage.GoodStage.StageY : null);
            AppendMissingAxis(ref reason, "GoodStageZ", Stage.GoodStage != null ? Stage.GoodStage.StageZ : null);
            AppendMissingAxis(ref reason, "NgStageY", Stage.NgStage != null ? Stage.NgStage.StageY : null);
            AppendMissingAxis(ref reason, "OutputCameraX", Stage.OutputCameraX);
            return reason;
        }

        private string BuildAxisServoAlarmReason()
        {
            string reason = string.Empty;
            AppendAxisReadyState(ref reason, BinStageAxis.GoodBinY);
            AppendAxisReadyState(ref reason, BinStageAxis.GoodBinZ);
            AppendAxisReadyState(ref reason, BinStageAxis.NgBinY);
            AppendAxisReadyState(ref reason, BinStageAxis.VisionX);
            return reason;
        }

        private void AppendAxisReadyState(ref string reason, BinStageAxis axis)
        {
            QMC.Common.Motion.BaseAxis item = ResolveAxisOrNull(axis);
            if (item == null)
                return;

            if (item.IsServoOn && !item.IsAlarm)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += BuildAxisState(axis, item.ActualPosition) + ";";
        }

        private static void AppendMissingAxis(ref string reason, string label, object item)
        {
            if (item != null)
                return;

            if (reason.Length > 0)
                reason += " ";
            reason += label + "=null;";
        }

        private QMC.Common.Motion.BaseAxis ResolveAxisOrNull(BinStageAxis axis)
        {
            if (Stage == null)
                return null;

            switch (axis)
            {
                // NG 스테이지 Y축 반환
                case BinStageAxis.NgBinY:
                    return Stage.NgStage != null ? Stage.NgStage.StageY : null;
                // NG 스테이지 Z축 반환
                case BinStageAxis.NgBinZ:
                    return Stage.NgStage != null ? Stage.NgStage.StageZ : null;
                // GOOD 스테이지 Y축 반환
                case BinStageAxis.GoodBinY:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageY : null;
                // GOOD 스테이지 Z축 반환
                case BinStageAxis.GoodBinZ:
                    return Stage.GoodStage != null ? Stage.GoodStage.StageZ : null;
                // 비전 X축 반환
                case BinStageAxis.VisionX:
                    return Stage.OutputCameraX;
                default:
                    return null;
            }
        }

        protected int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
        }

        private TStep ResolveStartStep(TStep defaultStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    return defaultStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, defaultStep.ToString());
                TStep parsed;
                if (Enum.TryParse(saved, out parsed) &&
                    !IsStep(parsed, IdleStep) &&
                    !IsStep(parsed, CompleteStep) &&
                    !IsStep(parsed, ErrorStep))
                    return parsed;

                return defaultStep;
            }
            catch (Exception ex)
            {
                WriteLog(Name, "OutputStage 시작 스텝 복원 중 예외가 발생했습니다. state=" +
                    SequenceStateName + ", error=" + ex.Message + " - Failed");
                return defaultStep;
            }
            finally
            {
            }
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + Kind; }
        }

        protected static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
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

        protected static async Task<bool> AwaitStepWithCancellationAsync(Task<bool> stepTask, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitBoolAsync(stepTask, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        protected static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
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

        protected BinSide ResolveSideFromGrade()
        {
            if (Options != null && Options.Grade == DieGrade.Ng)
                return BinSide.Ng;

            return BinSide.Good;
        }

        protected BinStageAxis ResolveYAxis(BinSide side)
        {
            if (side == BinSide.Ng)
                return BinStageAxis.NgBinY;

            return BinStageAxis.GoodBinY;
        }

        protected BinStageAxis ResolveZAxis(BinSide side)
        {
            if (side == BinSide.Ng)
                return BinStageAxis.NgBinZ;

            return BinStageAxis.GoodBinZ;
        }

        protected bool HasSideZAxis(BinSide side)
        {
            if (Stage == null)
                return false;

            return Stage.HasStageAxis(ResolveZAxis(side));
        }

        protected bool SkipMissingSideZAxis(BinSide side, string description)
        {
            if (HasSideZAxis(side))
                return false;

            WriteLog(Name, description + " skipped because " + side + " stage has no Z axis. - Ok");
            return true;
        }

        protected double ResolveSideTarget(BinSide side, string positionName)
        {
            return ResolveTarget(ResolveYAxis(side), positionName);
        }

        protected double ResolveSideZTarget(BinSide side, string positionName)
        {
            return ResolveTarget(ResolveZAxis(side), positionName);
        }

        protected static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OutputStage sequence log failed. source=" + source + ", error=" + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.OutputSeq, source, message);
        }
    }
}
