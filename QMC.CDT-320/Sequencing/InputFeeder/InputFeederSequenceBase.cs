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

        public async Task<int> RunAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.InputSeq, Name, () => CurrentStep.ToString()))
            try
            {
                Options = options ?? InputFeederSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-FEEDER] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[INPUT-FEEDER] " + Options.RunMode + " " + Kind + " complete");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input feeder " + Kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input feeder " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-EX", Name, "Input feeder " + Kind + " exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

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
                Options.UseBarcode,
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

        protected static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, waitResult);
        }

        protected static string FormatAxisMoveWaitResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            return AxisMoveWaiter.FormatResult(waitResult, fallbackState);
        }

        protected async Task<int> WaitFeederYDoneAsync(Func<bool> inPosition, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await Feeder.WaitWaferFeederYMoveDoneInPosition(
                    Feeder.FeederY.CommandPosition,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                bool finalInPosition = inPosition == null || inPosition();

                if (waitResult == null || !waitResult.Success || !finalInPosition)
                {
                    string state = Feeder != null ? Feeder.GetWaferFeederTransferState() : "Feeder=null";
                    return Fail(ResolveAxisMoveWaitAlarmCode("IN-FEEDER-Y", waitResult), Feeder.Name,
                        description + " 이동 완료/위치 확인 실패. " +
                        FormatAxisMoveWaitResult(waitResult, state) +
                        ", 최종위치확인=" + finalInPosition +
                        ". " + state);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string state = Feeder != null ? Feeder.GetWaferFeederTransferState() : "Feeder=null";
                return Fail("IN-FEEDER-Y-WAIT-EX", Feeder != null ? Feeder.Name : "InputFeeder",
                    description + " 이동 완료 대기 중 예외가 발생했습니다. error=" + ex.Message + ". " + state);
            }
            finally
            {
            }
        }

        private TStep ResolveStartStep(TStep initialStep)
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
    }
}
