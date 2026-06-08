using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public enum InputFeederSequenceKind
    {
        LoadFromCassette,
        LoadToStage,
        UnloadFromStage,
        UnloadToCassette,
        Exchange,
        Recover
    }

    public enum InputFeederSequenceStep
    {
        Idle,
        CheckUnit,
        CheckCassetteTransferReady,
        CheckStageTransferReady,
        MoveCassetteLoad,
        TransferCassetteToFeeder,
        MoveStageLoad,
        TransferFeederToStage,
        MoveStageUnload,
        TransferStageToFeeder,
        MoveCassetteUnload,
        TransferFeederToCassette,
        ExchangeSlot,
        RecoverSafe,
        Complete,
        Error
    }

    public sealed class InputFeederSequenceOptions
    {
        public int SlotIndex { get; set; }
        public int NextSlotIndex { get; set; }
        public CassetteMaterialRole CassetteRole { get; set; }
        public int WaferSize { get; set; }
        public int MoveTimeoutMs { get; set; }
        public bool FineMove { get; set; }
        public bool UseBarcode { get; set; }
        public bool UseVacuum { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputFeederSequenceOptions Default()
        {
            return new InputFeederSequenceOptions
            {
                SlotIndex = 0,
                NextSlotIndex = 0,
                CassetteRole = CassetteMaterialRole.Input1,
                WaferSize = 12,
                MoveTimeoutMs = 10000,
                FineMove = false,
                UseBarcode = false,
                UseVacuum = true,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }

    public sealed class InputFeederSequence
    {
        private const string SequenceNamePrefix = "InputFeederSequence";
        private readonly MachineSequenceContext _context;
        private InputFeederSequenceKind _kind;
        private InputFeederSequenceOptions _options;
        private InputFeederSequenceStep _currentStep;

        public InputFeederSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
            _currentStep = InputFeederSequenceStep.Idle;
        }

        public Task<int> RunLoadFromCassetteAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.LoadFromCassette, InputFeederSequenceStep.CheckUnit, options);
        }

        public Task<int> RunLoadToStageAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.LoadToStage, InputFeederSequenceStep.CheckUnit, options);
        }

        public Task<int> RunUnloadFromStageAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.UnloadFromStage, InputFeederSequenceStep.CheckUnit, options);
        }

        public Task<int> RunUnloadToCassetteAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.UnloadToCassette, InputFeederSequenceStep.CheckUnit, options);
        }

        public Task<int> RunExchangeAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.Exchange, InputFeederSequenceStep.CheckUnit, options);
        }

        public Task<int> RunRecoverAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return RunAsync(ct, InputFeederSequenceKind.Recover, InputFeederSequenceStep.CheckUnit, options);
        }

        private async Task<int> RunAsync(
            CancellationToken ct,
            InputFeederSequenceKind kind,
            InputFeederSequenceStep initialStep,
            InputFeederSequenceOptions options)
        {
            try
            {
                _kind = kind;
                _options = options ?? InputFeederSequenceOptions.Default();
                _currentStep = ResolveStartStep(initialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, _currentStep.ToString());

                while (_currentStep != InputFeederSequenceStep.Complete &&
                       _currentStep != InputFeederSequenceStep.Error)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.LogPublic("[INPUT-FEEDER] " + _options.RunMode + " " + _kind + " step=" + _currentStep);

                    InputFeederSequenceStep executingStep = _currentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (_currentStep != InputFeederSequenceStep.Error)
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), _currentStep.ToString());
                }

                SequenceResumeStore.MarkCompleted(SequenceStateName);
                WriteLog("RunAsync", "Input feeder " + _kind + " sequence completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input feeder " + _kind + " sequence canceled at step=" + _currentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-EX", "InputFeederSequence", "Input feeder " + _kind + " exception at step=" + _currentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (_currentStep)
            {
                case InputFeederSequenceStep.CheckUnit:
                    return CheckUnit(ResolveFirstActionStep());

                case InputFeederSequenceStep.CheckCassetteTransferReady:
                    return CheckCassetteTransferReady(InputFeederSequenceStep.TransferCassetteToFeeder);

                case InputFeederSequenceStep.CheckStageTransferReady:
                    return CheckStageTransferReady(ResolveStageTransferStep());

                case InputFeederSequenceStep.TransferCassetteToFeeder:
                    return await TransferCassetteToFeederAsync(ct).ConfigureAwait(false);

                case InputFeederSequenceStep.TransferFeederToStage:
                    return await TransferFeederToStageAsync(ct).ConfigureAwait(false);

                case InputFeederSequenceStep.TransferStageToFeeder:
                    return await TransferStageToFeederAsync(ct).ConfigureAwait(false);

                case InputFeederSequenceStep.TransferFeederToCassette:
                    return await TransferFeederToCassetteAsync(ct).ConfigureAwait(false);

                case InputFeederSequenceStep.ExchangeSlot:
                    return await ExchangeSlotAsync(ct).ConfigureAwait(false);

                case InputFeederSequenceStep.RecoverSafe:
                    return await RecoverSafeAsync(ct).ConfigureAwait(false);

                default:
                    return Fail("IN-FEEDER-STEP", "InputFeederSequence", "Unsupported input feeder step: " + _currentStep);
            }
        }

        private static async Task<int> AwaitStepWithCancellationAsync(Task<int> stepTask, CancellationToken ct)
        {
            if (stepTask == null)
                return -1;

            if (stepTask.IsCompleted)
                return await stepTask.ConfigureAwait(false);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, stepTask))
                ct.ThrowIfCancellationRequested();

            return await stepTask.ConfigureAwait(false);
        }

        private InputFeederSequenceStep ResolveFirstActionStep()
        {
            switch (_kind)
            {
                case InputFeederSequenceKind.LoadFromCassette:
                    return InputFeederSequenceStep.CheckCassetteTransferReady;
                case InputFeederSequenceKind.LoadToStage:
                case InputFeederSequenceKind.UnloadFromStage:
                    return InputFeederSequenceStep.CheckStageTransferReady;
                case InputFeederSequenceKind.UnloadToCassette:
                    return InputFeederSequenceStep.TransferFeederToCassette;
                case InputFeederSequenceKind.Exchange:
                    return InputFeederSequenceStep.ExchangeSlot;
                case InputFeederSequenceKind.Recover:
                    return InputFeederSequenceStep.RecoverSafe;
                default:
                    return InputFeederSequenceStep.Error;
            }
        }

        private InputFeederSequenceStep ResolveStageTransferStep()
        {
            return _kind == InputFeederSequenceKind.UnloadFromStage
                ? InputFeederSequenceStep.TransferStageToFeeder
                : InputFeederSequenceStep.TransferFeederToStage;
        }

        private int CheckUnit(InputFeederSequenceStep nextStep)
        {
            if (Feeder == null)
                return Fail("IN-FEEDER-MISSING", "InputFeeder", "Input feeder unit is not available.");

            if (!Feeder.IsWaferFeederSafe())
                return Fail("IN-FEEDER-UNSAFE", Feeder.Name, "Input feeder is not safe. state=" + Feeder.GetWaferFeederTransferState());

            _currentStep = nextStep;
            return 0;
        }

        private int CheckCassetteTransferReady(InputFeederSequenceStep nextStep)
        {
            if (!Feeder.CheckWaferCassetteReady(_options.SlotIndex, QMC.CDT320.TransferMode.Load))
                return Fail("IN-FEEDER-CST-READY", Feeder.Name, "Input feeder cassette load condition is not ready.");

            _currentStep = nextStep;
            return 0;
        }

        private int CheckStageTransferReady(InputFeederSequenceStep nextStep)
        {
            QMC.CDT320.TransferMode mode = _kind == InputFeederSequenceKind.UnloadFromStage
                ? QMC.CDT320.TransferMode.Unload
                : QMC.CDT320.TransferMode.Load;

            if (!Feeder.CheckWaferStageReady(_options.WaferSize, mode))
                return Fail("IN-FEEDER-STAGE-READY", Feeder.Name, "Input feeder stage transfer condition is not ready.");

            _currentStep = nextStep;
            return 0;
        }

        private async Task<int> TransferCassetteToFeederAsync(CancellationToken ct)
        {
            int result = await Feeder.LoadWaferFromCassetteToFeeder(
                _options.SlotIndex,
                ResolveTimeout(),
                _options.FineMove,
                _options.UseBarcode,
                _options.CassetteRole,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-CST-LOAD", Feeder.Name, "Cassette to feeder transfer failed. result=" + result);

            _context.Bus.Set("InputFeederOccupied");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private async Task<int> TransferFeederToStageAsync(CancellationToken ct)
        {
            int result = await Feeder.LoadWaferFromFeederToStage(
                _options.WaferSize,
                ResolveTimeout(),
                _options.FineMove,
                _options.UseVacuum,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD", Feeder.Name, "Feeder to input stage transfer failed. result=" + result);

            if (_context.Machine.InputStageUnit != null)
                _context.Machine.InputStageUnit.SetCurrentWaferMaterial(
                    MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage));

            _context.Bus.Set("InputStageOccupied");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private async Task<int> TransferStageToFeederAsync(CancellationToken ct)
        {
            int result = await Feeder.UnloadWaferFromStageToFeeder(
                _options.WaferSize,
                ResolveTimeout(),
                _options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-STAGE-UNLOAD", Feeder.Name, "Input stage to feeder transfer failed. result=" + result);

            if (_context.Machine.InputStageUnit != null)
                _context.Machine.InputStageUnit.ClearCurrentWaferMaterial();

            _context.Bus.Set("InputFeederOccupied");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private async Task<int> TransferFeederToCassetteAsync(CancellationToken ct)
        {
            int result = await Feeder.UnloadWaferFromFeederToCassette(
                _options.SlotIndex,
                ResolveTimeout(),
                _options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-CST-UNLOAD", Feeder.Name, "Feeder to cassette transfer failed. result=" + result);

            _context.Bus.Set("InputFeederEmpty");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private async Task<int> ExchangeSlotAsync(CancellationToken ct)
        {
            int result = await Feeder.ExchangeWaferFeederRingForNextSlot(
                _options.SlotIndex,
                _options.NextSlotIndex,
                ResolveTimeout(),
                _options.FineMove,
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("IN-FEEDER-EXCHANGE", Feeder.Name, "Input feeder exchange failed. result=" + result);

            _context.Bus.Set("InputFeederExchanged");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private async Task<int> RecoverSafeAsync(CancellationToken ct)
        {
            int result = await Feeder.RecoverWaferFeederToSafeState(ResolveTimeout(), true, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-RECOVER", Feeder.Name, "Input feeder recovery failed. result=" + result);

            _context.Bus.Set("InputFeederRecovered");
            _currentStep = InputFeederSequenceStep.Complete;
            return 0;
        }

        private InputFeederSequenceStep ResolveStartStep(InputFeederSequenceStep initialStep)
        {
            if (_options.StartMode != SequenceStartMode.Resume)
                return initialStep;

            string saved = SequenceResumeStore.ResolveStartStep(SequenceStateName, initialStep.ToString());
            InputFeederSequenceStep step;
            return Enum.TryParse(saved, out step) ? step : initialStep;
        }

        private string SequenceStateName
        {
            get { return SequenceNamePrefix + "." + _kind; }
        }

        private InputFeederUnit Feeder
        {
            get { return _context != null && _context.Machine != null ? _context.Machine.InputFeederUnit : null; }
        }

        private int ResolveTimeout()
        {
            return _options.MoveTimeoutMs > 0 ? _options.MoveTimeoutMs : 10000;
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                InputFeederSequenceStep failedStep = _currentStep;
                _currentStep = InputFeederSequenceStep.Error;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                _context.LogPublic("[INPUT-FEEDER] FAIL " + alarmCode + " - " + message);
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

        private static void WriteLog(string source, string message)
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
