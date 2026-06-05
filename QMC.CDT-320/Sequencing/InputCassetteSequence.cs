using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public enum InputCassetteSequenceKind
    {
        Loading,
        Mapping,
        Unloading
    }

    public enum InputCassetteSequenceStep
    {
        Idle,
        CheckFeederPosition,
        CheckLot,
        CheckCassetteDetected,
        CheckCassetteSize,
        CheckCassetteMaterial,
        CheckMappingStartCondition,
        MoveLoadingPosition,
        MoveUnloadingPosition,
        MoveMappingStartPosition,
        MoveMappingEndPosition,
        ScanSlots,
        BuildWaferInfo,
        MoveFirstWaferSlot,
        Complete,
        Error
    }

    public sealed class InputCassetteSequenceOptions
    {
        public bool FineMove { get; set; }
        public bool RequireActiveLot { get; set; }
        public int RequiredCassetteSize { get; set; }
        public int MoveTimeoutMs { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputCassetteSequenceOptions Default()
        {
            return new InputCassetteSequenceOptions
            {
                FineMove = false,
                RequireActiveLot = false,
                RequiredCassetteSize = 0,
                MoveTimeoutMs = 0,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }

    public sealed class InputCassetteSequence
    {
        private readonly MachineSequenceContext _context;

        public InputCassetteSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<int> RunLoadingAsync(CancellationToken ct)
        {
            return RunLoadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunLoadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteLoadingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMappingAsync(CancellationToken ct)
        {
            return RunMappingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunMappingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteMappingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct)
        {
            return RunUnloadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteUnloadingSequence(_context).RunAsync(ct, options);
        }
    }

    internal abstract class InputCassetteSequenceBase
    {
        private const string SequenceNamePrefix = "InputCassetteSequence";

        protected InputCassetteSequenceBase(
            MachineSequenceContext context,
            InputCassetteSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Kind = kind;
            Name = name ?? kind.ToString();
            CurrentStep = InputCassetteSequenceStep.Idle;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputCassetteSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputCassetteSequenceOptions Options { get; private set; }
        protected InputCassetteSequenceStep CurrentStep { get; set; }
        protected abstract InputCassetteSequenceStep InitialStep { get; }

        protected InputCassetteUnit Cassette
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputCassette : null; }
        }

        protected InputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputFeeder : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            try
            {
                Options = options ?? InputCassetteSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (CurrentStep != InputCassetteSequenceStep.Complete &&
                       CurrentStep != InputCassetteSequenceStep.Error)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-CASSETTE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    InputCassetteSequenceStep executingStep = CurrentStep;
                    int result = await ExecuteCurrentStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    if (CurrentStep != InputCassetteSequenceStep.Error)
                        SequenceResumeStore.MarkStepCompleted(SequenceStateName, executingStep.ToString(), CurrentStep.ToString());
                }

                Context.LogPublic("[INPUT-CASSETTE] " + Options.RunMode + " " + Kind + " complete");
                WriteLog("RunAsync", "Input cassette " + Kind + " sequence completed. - Ok");
                SequenceResumeStore.MarkCompleted(SequenceStateName);
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", "Input cassette " + Kind + " sequence canceled at step=" + CurrentStep + ". - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-EXCEPTION", Name, "Input cassette " + Kind + " sequence exception at step=" + CurrentStep + ": " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteCurrentStepAsync(CancellationToken ct);

        protected int CheckLot(InputCassetteSequenceStep nextStep)
        {
            try
            {
                if (Options.RequireActiveLot && LotStorage.ActiveLot == null)
                    return Fail("IN-CST-NO-LOT", Name, "Active lot is required for cassette mapping.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-LOT-EX", Name, "Lot check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteDetected(InputCassetteSequenceStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                bool detected = cassette.IsWaferCassetteExist(ResolveCassetteSize(cassette));
                if (!IsHardwareBypassed() && !detected)
                    return Fail("IN-CST-MISSING", cassette.Name, "Input cassette is not detected.");
                if (IsHardwareBypassed() && !detected)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: cassette detect sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-DETECT-EX", Name, "Cassette detect check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteSize(InputCassetteSequenceStep nextStep, bool allowMismatch)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                bool matched = IsCassetteSizeMatched(cassette);
                if (!IsHardwareBypassed() && !matched)
                {
                    if (!allowMismatch)
                        return Fail("IN-CST-SIZE", cassette.Name, "Input cassette size does not match recipe/config.");

                    Context.LogPublic("[INPUT-CASSETTE] Unloading continues although cassette size does not match recipe/config.");
                }

                if (IsHardwareBypassed() && !matched)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: cassette size sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-SIZE-EX", Name, "Cassette size check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckCassetteMaterial(InputCassetteSequenceStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                if (cassette.GetWaferMaterialCassette() == null)
                    return Fail("IN-CST-MATERIAL", cassette.Name, "Input cassette material information is missing.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MATERIAL-EX", Name, "Cassette material check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckMappingStartCondition(InputCassetteSequenceStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                bool ready = cassette.CheckWaferCassetteMappingReady();
                if (!IsHardwareBypassed() && !ready)
                    return Fail("IN-CST-MAP-READY", cassette.Name, "Input cassette is not ready for mapping.");
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: mapping ready sensor check skipped.");
                if (!HasProcessWaferIfMapped(cassette))
                    Context.LogPublic("[INPUT-CASSETTE] No unprocessed wafer is currently registered. Mapping will refresh wafer information.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-READY-EX", Name, "Mapping start condition check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int CheckFeederPosition(InputCassetteSequenceStep nextStep)
        {
            try
            {
                var feeder = Feeder;
                bool ready = IsFeederAllowedForCassetteMove(feeder);
                if (!IsHardwareBypassed() && !ready)
                    return Fail("IN-CST-FEEDER-POS", feeder != null ? feeder.Name : "InputFeeder", "Feeder must be in Avoid or Exchange position.");
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: feeder position sensor check skipped.");

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-FEEDER-EX", Name, "Feeder position check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveLoadingPositionAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                if (!cassette.CheckWaferCassetteMoveReady())
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move.");

                int result = await cassette.MoveWaferLifterZ(cassette.Recipe.LoaingPosition, Options.FineMove).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-LOAD-POS", cassette.Name, "Move loading position failed. result=" + result);
                result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette)).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-LOAD-WAIT", cassette.Name, "Loading position move timeout.");

                CurrentStep = InputCassetteSequenceStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-LOAD-EX", Name, "Move loading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveUnloadingPositionAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                if (!cassette.CheckWaferCassetteMoveReady())
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move.");

                int result = await cassette.MoveWaferLifterZ(cassette.Recipe.UnloadingPosition, Options.FineMove).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-UNLOAD-POS", cassette.Name, "Move unloading position failed. result=" + result);
                result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette)).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-UNLOAD-WAIT", cassette.Name, "Unloading position move timeout.");

                CurrentStep = InputCassetteSequenceStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-UNLOAD-EX", Name, "Move unloading position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveMappingStartPositionAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                if (!cassette.CheckWaferCassetteMoveReady())
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move.");

                int result = await cassette.MoveToWaferCassetteMappingStartPosition(Options.FineMove).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-MAP-START", cassette.Name, "Move mapping start position failed. result=" + result);
                result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette)).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-MAP-START-WAIT", cassette.Name, "Mapping start position move timeout.");

                CurrentStep = InputCassetteSequenceStep.MoveMappingEndPosition;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-START-EX", Name, "Move mapping start position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveMappingEndPositionAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                int result = await cassette.MoveToWaferCassetteMappingEndPosition(Options.FineMove).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-MAP-END", cassette.Name, "Move mapping end position failed. result=" + result);
                result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette)).ConfigureAwait(false);
                if (result != 0) return Fail("IN-CST-MAP-END-WAIT", cassette.Name, "Mapping end position move timeout.");

                CurrentStep = InputCassetteSequenceStep.ScanSlots;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-MAP-END-EX", Name, "Move mapping end position exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> ScanSlotsAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                if (IsHardwareBypassed())
                {
                    cassette.BuildSimulatedWaferMap();
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: simulated wafer map generated.");
                }
                else
                {
                    int result = await cassette.WaferScan(ResolveMoveTimeout(cassette), Options.FineMove).ConfigureAwait(false);
                    if (result != 0) return Fail("IN-CST-SCAN", cassette.Name, "Wafer scan failed. result=" + result);
                }

                CurrentStep = InputCassetteSequenceStep.BuildWaferInfo;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-SCAN-EX", Name, "Wafer scan exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int BuildWaferInfo()
        {
            try
            {
                var cassette = Cassette;
                int result = RegisterMappingResult(cassette);
                if (result != 0)
                    return Fail("IN-CST-BUILD-WAFER", cassette != null ? cassette.Name : "InputCassette", "Input cassette material mapping result registration failed. result=" + result);

                CurrentStep = InputCassetteSequenceStep.MoveFirstWaferSlot;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-BUILD-WAFER-EX", Name, "Build wafer information exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveFirstWaferSlotAsync()
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                int firstSlot = cassette.FindNextProcessWaferSlot();
                if (firstSlot >= 0)
                {
                    int result = await cassette.MoveToWaferCassetteSlotPosition(firstSlot, Options.FineMove).ConfigureAwait(false);
                    if (result != 0) return Fail("IN-CST-FIRST-SLOT", cassette.Name, "Move first wafer slot failed. result=" + result);
                    result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette)).ConfigureAwait(false);
                    if (result != 0) return Fail("IN-CST-FIRST-SLOT-WAIT", cassette.Name, "First wafer slot move timeout.");
                }

                CurrentStep = InputCassetteSequenceStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-FIRST-SLOT-EX", Name, "Move first wafer slot exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int FailUnsupportedStep()
        {
            return Fail("IN-CST-STEP", Name, "Unsupported cassette sequence step: " + CurrentStep);
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                InputCassetteSequenceStep failedStep = CurrentStep;
                CurrentStep = InputCassetteSequenceStep.Error;
                SequenceResumeStore.MarkAlarm(SequenceStateName, failedStep.ToString(), message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[INPUT-CASSETTE] FAIL " + alarmCode + " - " + message);
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

        private InputCassetteSequenceStep ResolveStartStep(InputCassetteSequenceStep defaultStep)
        {
            try
            {
                if (Options.StartMode == SequenceStartMode.Restart)
                {
                    SequenceResumeStore.Clear(SequenceStateName);
                    WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence forced restart from step=" + defaultStep + ". - Ok");
                    return defaultStep;
                }

                string stepText = SequenceResumeStore.ResolveStartStep(SequenceStateName, defaultStep.ToString());
                InputCassetteSequenceStep parsed;
                if (Enum.TryParse(stepText, out parsed) &&
                    parsed != InputCassetteSequenceStep.Idle &&
                    parsed != InputCassetteSequenceStep.Complete &&
                    parsed != InputCassetteSequenceStep.Error)
                {
                    WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence resume step=" + parsed + ". - Ok");
                    return parsed;
                }

                return defaultStep;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStartStep", "Input cassette " + Kind + " sequence resume step resolve failed: " + ex.Message + " - Failed");
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

        private bool IsFeederAllowedForCassetteMove(InputFeederUnit feeder)
        {
            if (feeder == null) return false;
            return feeder.IsWaferFeederInAvoidPosition() || feeder.IsWaferFeederInExchangePosition();
        }

        private int ResolveCassetteSize(InputCassetteUnit cassette)
        {
            if (Options.RequiredCassetteSize == 8 || Options.RequiredCassetteSize == 12)
                return Options.RequiredCassetteSize;
            return cassette.Config.InchSelect == 0 ? 8 : 12;
        }

        private bool IsCassetteSizeMatched(InputCassetteUnit cassette)
        {
            int size = ResolveCassetteSize(cassette);
            return cassette.IsWaferCassetteExist(size);
        }

        private bool IsHardwareBypassed()
        {
            var settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun);
        }

        private int ResolveMoveTimeout(InputCassetteUnit cassette)
        {
            if (Options.MoveTimeoutMs > 0)
                return Options.MoveTimeoutMs;

            int configured = cassette != null ? cassette.ResolveWaferLifterZMoveTimeoutMs() : 0;
            return configured > 0 ? configured : 3000;
        }

        private bool HasProcessWaferIfMapped(InputCassetteUnit cassette)
        {
            return cassette.WaferMap == null || cassette.WaferMap.Count == 0 || cassette.HasMoreProcessWafer();
        }

        private int RegisterMappingResult(InputCassetteUnit cassette)
        {
            try
            {
                if (cassette == null)
                {
                    WriteLog("RegisterMappingResult", "Input cassette unit is not available. - Failed");
                    return -1;
                }

                if (cassette.WaferMap == null)
                {
                    WriteLog("RegisterMappingResult", "Input cassette wafer map is not available. - Failed");
                    return -1;
                }

                var arr = new bool[cassette.WaferMap.Count];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = cassette.WaferMap[i];

                SlotMapperRegistry.Update("InputCassette", arr);
                int inchSelect = cassette.Config != null ? cassette.Config.InchSelect : 0;
                MaterialStateService.UpdateInputCassetteMapping(
                    ResolveInputCassetteLevelCount(cassette),
                    cassette.Config != null ? cassette.Config.SlotCount : arr.Length,
                    cassette.WaferMap,
                    null,
                    BuildCassetteLevelSlotPositions(cassette, 1),
                    BuildCassetteLevelSlotPositions(cassette, 2),
                    LotStorage.ActiveLot != null ? LotStorage.ActiveLot.LotID : "",
                    MaterialStateService.ResolveRecipeTapeFrameSpecName(inchSelect));
                Context.Controller.ApplyInputCassetteMappingCompleted();
                WriteLog("RegisterMappingResult", "Input cassette material mapping result registered. - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                WriteLog("RegisterMappingResult", "Input cassette material mapping result registration exception: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static double[] BuildCassetteLevelSlotPositions(InputCassetteUnit cassette, int level)
        {
            try
            {
                int count = cassette != null && cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (count < 0)
                    count = 0;

                var positions = new double[count];
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = cassette.CalculateCassetteLevelSlotPosition(level, i);

                return positions;
            }
            catch (Exception ex)
            {
                WriteLog("BuildCassetteLevelSlotPositions", "Cassette level slot positions build failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static int ResolveInputCassetteLevelCount(InputCassetteUnit cassette)
        {
            int configured = cassette != null && cassette.Config != null ? cassette.Config.SelectedCassetteLevel : 1;
            return configured >= 2 ? 2 : 1;
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

    internal sealed class InputCassetteLoadingSequence : InputCassetteSequenceBase
    {
        public InputCassetteLoadingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Loading, "InputCassetteLoadingSequence")
        {
        }

        protected override InputCassetteSequenceStep InitialStep
        {
            get { return InputCassetteSequenceStep.CheckFeederPosition; }
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                switch (CurrentStep)
                {
                    case InputCassetteSequenceStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteSequenceStep.CheckCassetteDetected));
                    case InputCassetteSequenceStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteSequenceStep.MoveLoadingPosition));
                    case InputCassetteSequenceStep.MoveLoadingPosition:
                        return MoveLoadingPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-LOAD-STEP-EX", "InputCassetteLoadingSequence", "Loading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }

    internal sealed class InputCassetteUnloadingSequence : InputCassetteSequenceBase
    {
        public InputCassetteUnloadingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Unloading, "InputCassetteUnloadingSequence")
        {
        }

        protected override InputCassetteSequenceStep InitialStep
        {
            get { return InputCassetteSequenceStep.CheckCassetteDetected; }
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                switch (CurrentStep)
                {
                    case InputCassetteSequenceStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteSequenceStep.CheckCassetteSize));
                    case InputCassetteSequenceStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(InputCassetteSequenceStep.CheckFeederPosition, true));
                    case InputCassetteSequenceStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteSequenceStep.MoveUnloadingPosition));
                    case InputCassetteSequenceStep.MoveUnloadingPosition:
                        return MoveUnloadingPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-UNLOAD-STEP-EX", "InputCassetteUnloadingSequence", "Unloading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }

    internal sealed class InputCassetteMappingSequence : InputCassetteSequenceBase
    {
        public InputCassetteMappingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Mapping, "InputCassetteMappingSequence")
        {
        }

        protected override InputCassetteSequenceStep InitialStep
        {
            get { return InputCassetteSequenceStep.CheckLot; }
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                switch (CurrentStep)
                {
                    case InputCassetteSequenceStep.CheckLot:
                        return Task.FromResult(CheckLot(InputCassetteSequenceStep.CheckCassetteDetected));
                    case InputCassetteSequenceStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteSequenceStep.CheckCassetteSize));
                    case InputCassetteSequenceStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(InputCassetteSequenceStep.CheckCassetteMaterial, false));
                    case InputCassetteSequenceStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(InputCassetteSequenceStep.CheckMappingStartCondition));
                    case InputCassetteSequenceStep.CheckMappingStartCondition:
                        return Task.FromResult(CheckMappingStartCondition(InputCassetteSequenceStep.CheckFeederPosition));
                    case InputCassetteSequenceStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteSequenceStep.MoveMappingStartPosition));
                    case InputCassetteSequenceStep.MoveMappingStartPosition:
                        return MoveMappingStartPositionAsync();
                    case InputCassetteSequenceStep.MoveMappingEndPosition:
                        return MoveMappingEndPositionAsync();
                    case InputCassetteSequenceStep.ScanSlots:
                        return ScanSlotsAsync();
                    case InputCassetteSequenceStep.BuildWaferInfo:
                        return Task.FromResult(BuildWaferInfo());
                    case InputCassetteSequenceStep.MoveFirstWaferSlot:
                        return MoveFirstWaferSlotAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-MAP-STEP-EX", "InputCassetteMappingSequence", "Mapping step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
