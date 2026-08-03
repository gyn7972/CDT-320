using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal abstract class InputFeederSequenceBase<TStep> where TStep : struct
    {
        #region 실행 구성 및 장비 접근

        private const string SequenceNamePrefix = "InputFeederSequence";

        protected InputFeederSequenceBase(
            MachineSequenceContext context,
            InputFeederSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputFeederSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputFeederSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected InputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null; }
        }

        #endregion

        #region State Machine 실행 수명주기

        public async Task<int> RunAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            Options = options ?? InputFeederSequenceOptions.Default();
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.InputSeq, Name, () => CurrentStep.ToString(), Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            try
            {
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceTrace.RunStart(Name, Options.RunMode.ToString(), "kind=" + Kind);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                // 재개(비-초기 스텝) 진입 시, 저장된 스텝의 모션을 실행하기 전에 피더 이동 안전 상태를
                // CheckUnit과 동일 기준으로 재확인한다. 정지 중 상태가 바뀌었으면 저장 스텝을 그대로 재개하지 않고
                // fail-closed(알람)로 정지한다. (재개 시 앞단 검증 스킵으로 인한 무검증 이동 방지)
                if (!IsStep(CurrentStep, InitialStep))
                {
                    int resumeSafety = VerifyResumeSafety();
                    if (resumeSafety != 0)
                    {
                        SequenceTrace.RunEnd(Name, "Failed", resumeSafety, "kind=" + Kind, "reason=ResumeSafetyBlocked", "step=" + CurrentStep);
                        return resumeSafety;
                    }
                }

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-FEEDER] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    SequenceTrace.StepStart(Name, executingStep.ToString(), "kind=" + Kind);
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                    {
                        SequenceTrace.StepFail(Name, executingStep.ToString(), result, "kind=" + Kind, "next=" + CurrentStep);
                        SequenceTrace.RunEnd(Name, "Failed", result, "kind=" + Kind, "step=" + executingStep);
                        return result;
                    }

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                    SequenceTrace.StepEnd(Name, executingStep.ToString(), result, "kind=" + Kind, "next=" + CurrentStep);
                }

                Context.LogPublic("[INPUT-FEEDER] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input feeder " + Kind + " sequence completed. - Ok");
                SequenceTrace.RunEnd(Name, "Completed", 0, "kind=" + Kind);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input feeder " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                SequenceTrace.RunEnd(Name, "Canceled", -1, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("IN-FEEDER-EX", Name, "Input feeder " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
                SequenceTrace.RunEnd(Name, "Failed", failResult, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        #endregion

        #region 공통 검사 및 이송 동작

        protected int CheckUnit(TStep nextStep)
        {
            if (Feeder == null)
                return Fail("IN-FEEDER-MISSING", "InputFeeder", "Input feeder unit is not available.");

            string readyReason;
            if (!Feeder.CheckWaferFeederMoveReady(out readyReason))
                return Fail("IN-FEEDER-UNSAFE", Feeder.Name, "Input feeder is not safe. " + readyReason);

            CurrentStep = nextStep;
            return 0;
        }

        // 재개 안전 재확인: 저장된 스텝(중간 모션 스텝)부터 재개할 때, 실행 전에 피더가 이동 안전 상태인지
        // CheckUnit과 동일 기준(CheckWaferFeederMoveReady)으로 다시 확인한다. 불만족이면 fail-closed로 차단한다.
        private int VerifyResumeSafety()
        {
            if (Feeder == null)
                return Fail("IN-FEEDER-MISSING", "InputFeeder", "재개 안전 확인 불가: Input feeder unit is not available.");

            string readyReason;
            if (!Feeder.CheckWaferFeederMoveReady(out readyReason))
                return Fail("IN-FEEDER-RESUME-UNSAFE", Feeder.Name,
                    "재개 전 InputFeeder 안전 상태 재확인 실패로 저장 스텝 재개를 차단합니다. step=" + CurrentStep + ". " + readyReason);

            return 0;
        }

        protected int CheckCassetteTransferReady(TStep nextStep)
        {
            string readyReason;
            if (!Feeder.CheckWaferCassetteReady(Options.SlotIndex, QMC.CDT320.TransferMode.Load, out readyReason))
                return Fail("IN-FEEDER-CST-READY", Feeder.Name, "Input feeder cassette load condition is not ready. " + readyReason);

            CurrentStep = nextStep;
            return 0;
        }

        protected int CheckStageTransferReady(QMC.CDT320.TransferMode mode, TStep nextStep)
        {
            string readyReason;
            if (!Feeder.CheckWaferStageReady(Options.WaferSize, mode, out readyReason))
                return Fail("IN-FEEDER-STAGE-READY", Feeder.Name, "Input feeder stage transfer condition is not ready. " + readyReason);

            CurrentStep = nextStep;
            return 0;
        }

        protected async Task<int> TransferCassetteToFeederAsync(CancellationToken ct)
        {
            int result = await Feeder.LoadWaferFromCassetteToFeeder(
                Options.SlotIndex,
                ResolveTimeout(),
                Options.FineMove,
                // 실제 Wafer barcode reader는 InputCameraX에 있다. Cassette -> Feeder 구간의
                // 기존 FeederY barcode hook은 사용하지 않고 Stage 적재 완료 후 별도 판독한다.
                false,
                Options.CassetteRole,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-CST-LOAD", Feeder.Name,
                    "Cassette to feeder transfer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederOccupied");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> TransferFeederToStageAsync(CancellationToken ct)
        {
            int result = await Feeder.LoadWaferFromFeederToStage(
                Options.WaferSize,
                ResolveTimeout(),
                Options.FineMove,
                Options.UseVacuum,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD", Feeder.Name,
                    "Feeder to input stage transfer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            if (Context.Machine.InputStageUnit != null)
                Context.Machine.InputStageUnit.SetCurrentWaferMaterial(
                    MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage));

            Context.Bus.Set("InputStageOccupied");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> TransferStageToFeederAsync(CancellationToken ct)
        {
            int result = await Feeder.UnloadWaferFromStageToFeeder(
                Options.WaferSize,
                ResolveTimeout(),
                Options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-STAGE-UNLOAD", Feeder.Name,
                    "Input stage to feeder transfer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            if (Context.Machine.InputStageUnit != null)
                Context.Machine.InputStageUnit.ClearCurrentWaferMaterial();

            Context.Bus.Set("InputFeederOccupied");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> TransferFeederToCassetteAsync(CancellationToken ct)
        {
            int result = await Feeder.UnloadWaferFromFeederToCassette(
                Options.SlotIndex,
                ResolveTimeout(),
                Options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-CST-UNLOAD", Feeder.Name,
                    "Feeder to cassette transfer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederEmpty");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> ExchangeSlotAsync(CancellationToken ct)
        {
            int result = await Feeder.ExchangeWaferFeederRingForNextSlot(
                Options.SlotIndex,
                Options.NextSlotIndex,
                ResolveTimeout(),
                Options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-EXCHANGE", Feeder.Name,
                    "Input feeder exchange failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederExchanged");
            CurrentStep = CompleteStep;
            return 0;
        }

        protected async Task<int> RecoverSafeAsync(CancellationToken ct)
        {
            int result = await Feeder.RecoverWaferFeederToSafeState(ResolveTimeout(), true, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-RECOVER", Feeder.Name,
                    "Input feeder recovery failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederRecovered");
            CurrentStep = CompleteStep;
            return 0;
        }

        #endregion

        #region 실패 처리 및 이동 완료 대기

        protected int FailUnsupportedStep()
        {
            return Fail("IN-FEEDER-STEP", Name, "Unsupported input feeder step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[INPUT-FEEDER] STOP " + message);
                    throw new SequenceStopException(message);
                }

                TStep failedStep = CurrentStep;
                CurrentStep = ErrorStep;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                SequenceFailureStore.Record(SequenceStateName, Kind.ToString(), failedStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                Context.LogPublic("[INPUT-FEEDER] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
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

        // 기존 조건: AxisMoveWaiter 실패 분류/문자열 헬퍼 — 현재 기준: waitCode+LastMotionFailureMessage(R3).
        protected static string FormatAxisMoveWaitCode(int waitCode, BaseAxis axis, string fallbackState)
        {
            string reason = axis != null && !string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage)
                ? axis.LastMotionFailureMessage
                : string.Empty;
            return "waitCode=" + waitCode + ", reason=" + reason + ". " + (fallbackState ?? string.Empty);
        }

        protected Task<int> WaitFeederYDoneAsync(Func<bool> inPosition, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool finalInPosition = inPosition == null || inPosition();

                if (!finalInPosition)
                {
                    string state = Feeder != null ? Feeder.GetWaferFeederTransferState() : "Feeder=null";
                    return Task.FromResult(Fail("IN-FEEDER-Y-POSITION", Feeder != null ? Feeder.Name : "InputFeeder",
                        description + " 최종 위치 확인 실패. 최종위치확인=" + finalInPosition +
                        ". " + state));
                }

                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                return Task.FromCanceled<int>(ct);
            }
            catch (Exception ex)
            {
                string state = Feeder != null ? Feeder.GetWaferFeederTransferState() : "Feeder=null";
                return Task.FromResult(Fail("IN-FEEDER-Y-POSITION-EX", Feeder != null ? Feeder.Name : "InputFeeder",
                    description + " 최종 위치 확인 중 예외가 발생했습니다. error=" + ex.Message + ". " + state));
            }
            finally
            {
            }
        }

        #endregion

        #region 재개·Timeout 및 공통 유틸리티

        protected virtual TStep ResolveStartStep(TStep initialStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Input feeder " + Kind + " sequence forced restart from step=" + initialStep + ". - Ok");
                    return initialStep;
                }

                string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
                TStep step;
                if (Enum.TryParse(saved, out step) &&
                    !IsStep(step, IdleStep) &&
                    !IsStep(step, CompleteStep) &&
                    !IsStep(step, ErrorStep))
                {
                    WriteLog("ResolveStartStep", "Input feeder " + Kind + " sequence resume step=" + step + ". - Ok");
                    return step;
                }

                return initialStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Input feeder " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
                return initialStep;
            }
            finally
            {
            }
        }

        protected int ResolveTimeout()
        {
            return Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 10000;
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

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.InputSeq, source, message);
        }

        #endregion
    }
}
