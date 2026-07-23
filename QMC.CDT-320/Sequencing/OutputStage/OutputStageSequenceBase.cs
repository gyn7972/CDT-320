using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Motion.SharedRailX;
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
            Options = options ?? OutputStageSequenceOptions.Default();
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.OutputSeq, Name, () => CurrentStep.ToString(), Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            try
            {
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceTrace.RunStart(Name, Options.RunMode.ToString(), "kind=" + Kind, "side=" + Options.Side);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                // 재개(비-초기 스텝) 진입 시, Stage 모션을 실행하기 전에 OutputFeederY가 Avoid에서 정지 상태인지 재확인한다.
                // 모든 Output Stage 서브시퀀스는 정상 흐름상 피더가 Avoid로 빠진 상태에서만 실행되므로,
                // 정지 중 피더 상태가 바뀌었으면 저장 스텝을 그대로 재개하지 않고 fail-closed(알람)로 정지한다.
                // (리프트 Up/Down·클램프는 kind별로 다르므로 Y 위치와 정지 여부만 확인한다.)
                if (!IsStep(CurrentStep, InitialStep))
                {
                    int resumeSafety = VerifyResumeSafety();
                    if (resumeSafety != 0)
                    {
                        SequenceTrace.RunEnd(Name, "Failed", resumeSafety, "kind=" + Kind, "side=" + Options.Side, "reason=ResumeSafetyBlocked", "step=" + CurrentStep);
                        return resumeSafety;
                    }
                }

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[OUTPUT-STAGE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);
                    TStep executingStep = CurrentStep;
                    SequenceTrace.StepStart(Name, executingStep.ToString(), "kind=" + Kind, "side=" + Options.Side);
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                    {
                        SequenceTrace.StepFail(Name, executingStep.ToString(), result, "kind=" + Kind, "side=" + Options.Side, "next=" + CurrentStep);
                        SequenceTrace.RunEnd(Name, "Failed", result, "kind=" + Kind, "side=" + Options.Side, "step=" + executingStep);
                        return result;
                    }

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                    SequenceTrace.StepEnd(Name, executingStep.ToString(), result, "kind=" + Kind, "side=" + Options.Side, "next=" + CurrentStep);
                }

                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Output stage " + Kind + " sequence completed. - Ok");
                SequenceTrace.RunEnd(Name, "Completed", 0, "kind=" + Kind, "side=" + Options.Side);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Output stage " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                SequenceTrace.RunEnd(Name, "Canceled", -1, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("OUT-STAGE-EX", Name, "Output stage " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
                SequenceTrace.RunEnd(Name, "Failed", failResult, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        // 재개 안전 재확인: 저장된 스텝(중간 모션 스텝)부터 재개할 때, Stage 축을 움직이기 전에
        // OutputFeederY가 Avoid 위치에서 정지해 있는지 확인한다. 불만족이면 fail-closed로 차단한다.
        private int VerifyResumeSafety()
        {
            OutputFeederUnit feeder = OutputFeeder;
            if (feeder == null || feeder.FeederY == null)
                return Fail("OUT-STAGE-RESUME-FEEDER-MISSING", Name,
                    "재개 안전 확인 불가: OutputFeeder 유닛/축 정보를 확인할 수 없습니다. step=" + CurrentStep);

            if (feeder.FeederY.IsMoving || !feeder.IsBinFeederAvoidPositionCheck())
                return Fail("OUT-STAGE-RESUME-FEEDER-AVOID", Name,
                    "재개 전 OutputFeederY가 Avoid 정지 상태가 아니어서 저장 스텝 재개를 차단합니다. " +
                    "피더를 안전 위치로 복귀(Recover)한 뒤 다시 시작하세요. step=" + CurrentStep +
                    ", " + feeder.DescribeBinFeederYMoveDoneState());

            return 0;
        }

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
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[OUTPUT-STAGE] STOP " + message);
                    throw new SequenceStopException(message);
                }

                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[OUTPUT-STAGE] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
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

                    bool normalPlaceOccupancy = IsOutputZoneOccupiedByPickerPlace(true) ||
                                                IsOutputZoneOccupiedByPickerPlace(false);
                    double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (!normalPlaceOccupancy && elapsedMs >= timeoutMs)
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
                            ", timeoutMs=" + (normalPlaceOccupancy ? "Place점유해제까지" : timeoutMs.ToString()) + " - Wait");
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

        private static bool IsOutputZoneOccupiedByPickerPlace(bool isFront)
        {
            try
            {
                PickerWorkZone zone;
                string owner;
                if (!PickerZoneInterlockRules.TryGetPickerWorkArea(isFront, out zone, out owner))
                    return false;

                if (zone != PickerWorkZone.Output || string.IsNullOrWhiteSpace(owner))
                    return false;

                return owner.IndexOf("PickerPlaceSequence", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       owner.IndexOf(":Place", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
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

                string targetName = "OutputStageSequence;" + Name + ";" + description;
                if (axis == BinStageAxis.VisionX)
                {
                    int visionXClearResult = await WaitOutputVisionXSharedRailClearAsync(
                        target,
                        targetName,
                        description,
                        ct).ConfigureAwait(false);
                    if (visionXClearResult != 0)
                        return visionXClearResult;
                }

                SequenceTrace.MotionStart("OutputStageMove",
                    "axis=" + axis,
                    "target=" + target,
                    "side=" + Options.Side,
                    "description=" + description);
                int result = await AwaitStepWithCancellationAsync(
                    Stage.MoveStageAxis(
                        axis,
                        target,
                        Options.FineMove,
                        targetName),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    SequenceTrace.MotionEnd("OutputStageMove", result,
                        "axis=" + axis,
                        "target=" + target,
                        "side=" + Options.Side,
                        "status=CommandFailed");
                    return Fail("OUT-STAGE-MOVE", Stage.Name,
                        description + " 이동 명령 실패. axis=" + axis + ", target=" + target +
                        ", result=" + result + ". " + BuildAxisState(axis, target) + ". " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));
                }

                ct.ThrowIfCancellationRequested();
                SequenceTrace.MotionEnd("OutputStageMove", 0,
                    "axis=" + axis,
                    "target=" + target,
                    "side=" + Options.Side,
                    "status=Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SequenceTrace.MotionEnd("OutputStageMove", -1,
                    "axis=" + axis,
                    "target=" + target,
                    "status=Exception",
                    "error=" + ex.Message);
                return Fail("OUT-STAGE-MOVE-EX", Stage != null ? Stage.Name : "OutputStage",
                    description + " 이동 처리 중 예외가 발생했습니다. axis=" + axis +
                    ", target=" + target +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitOutputVisionXSharedRailClearAsync(
            double target,
            string targetName,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Stage == null || Stage.OutputCameraX == null)
                    return 0;

                BaseAxis outputVisionX = Stage.OutputCameraX;
                if (IsAxisAlreadyInPosition(outputVisionX, target))
                    return 0;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                bool sharedRailApplicable = service != null && service.IsSharedRailAxis(outputVisionX);

                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                string reason = string.Empty;
                SequenceTrace.WaitStart("OutputVisionXSharedRailClear",
                    "sequence=" + Name,
                    "target=" + target.ToString("F3"),
                    "side=" + Options.Side,
                    "description=" + description);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    // Current rule: OutputVisionX enters only after SharedRailX distance and MotionGuard are clear.
                    string sharedRailReason = string.Empty;
                    bool sharedRailClear = !sharedRailApplicable ||
                        service.VerifySingleAxisMove(outputVisionX, target, out sharedRailReason);

                    string guardReason = string.Empty;
                    bool guardClear = sharedRailClear &&
                        MotionGuardRuntime.CanAxisTeachingMove(outputVisionX, target, targetName, out guardReason);

                    if (sharedRailClear && guardClear)
                        break;

                    reason = !sharedRailClear
                        ? "SharedRailX: " + sharedRailReason
                        : "MotionGuard: " + guardReason;

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        SequenceTrace.WaitEnd("OutputVisionXSharedRailClear",
                            -1,
                            "sequence=" + Name,
                            "status=Timeout",
                            "elapsedMs=" + elapsedMs.ToString("0"),
                            "timeoutMs=" + timeoutMs,
                            "reason=" + reason);
                        string timeoutAlarmCode = !sharedRailClear
                            ? "OUT-STAGE-VISION-X-SHARED-RAIL-X-TIMEOUT"
                            : "OUT-STAGE-VISION-X-MOTION-GUARD-TIMEOUT";
                        return Fail(timeoutAlarmCode, Stage.Name,
                            description + " OutputVisionX wait before move timed out. " +
                            "target=" + target.ToString("F6") +
                            ", side=" + Options.Side +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", reason=" + reason +
                            ". " + BuildAxisState(BinStageAxis.VisionX, target));
                    }

                    if (!waitLogged)
                    {
                        WriteLog("OutputVisionXSharedRailClear",
                            Name + " OutputVisionX wait before move. " +
                            "target=" + target.ToString("F6") +
                            ", side=" + Options.Side +
                            ", description=" + description +
                            ", reason=" + reason + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    WriteLog("OutputVisionXSharedRailClear",
                        Name + " OutputVisionX wait before move complete. " +
                        "target=" + target.ToString("F6") +
                        ", elapsedMs=" + elapsedMs.ToString("0") + " - Ok");
                }

                SequenceTrace.WaitEnd("OutputVisionXSharedRailClear",
                    0,
                    "sequence=" + Name,
                    "status=Clear",
                    "elapsedMs=" + ((DateTime.UtcNow - start).TotalMilliseconds).ToString("0"),
                    "target=" + target.ToString("F3"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-VISION-X-WAIT-EX", Stage != null ? Stage.Name : "OutputStage",
                    description + " OutputVisionX wait before move exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsAxisAlreadyInPosition(BaseAxis axis, double target)
        {
            if (axis == null || axis.IsMoving || axis.IsAlarm)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private async Task<int> EnsureNgStageYMoveClearAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Stage == null)
                    return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available.");

                int result = await Stage.EnsureNgStageYMoveClearAsync(
                    description,
                    ResolveTimeout(),
                    Options.FineMove,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-NG-Y-CLEAR", Stage.Name,
                        description + " 전 NG Stage Y 이동 조건 확보 실패. result=" + result + ", " +
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

        // 기존 조건: AxisMoveWaiter 실패 분류/문자열 헬퍼 — 현재 기준: waitCode+LastMotionFailureMessage(R3).
        protected static string FormatAxisMoveWaitCode(int waitCode, BaseAxis axis, string fallbackState)
        {
            string reason = axis != null && !string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage)
                ? axis.LastMotionFailureMessage
                : string.Empty;
            return "waitCode=" + waitCode + ", reason=" + reason + ". " + (fallbackState ?? string.Empty);
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
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 300000;
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
            // Good/NG가 동일 Kind를 공유하므로 Side를 키에 포함해야 재개 스텝이 반대 side로 섞이지 않는다.
            get { return SequenceNamePrefix + "." + Kind + "." + (Options != null ? Options.Side.ToString() : "-"); }
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
