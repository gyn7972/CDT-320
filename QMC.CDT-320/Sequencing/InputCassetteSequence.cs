using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Lots;
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

        public static InputCassetteSequenceOptions Default()
        {
            return new InputCassetteSequenceOptions
            {
                FineMove = false,
                RequireActiveLot = false,
                RequiredCassetteSize = 0,
                MoveTimeoutMs = 0,
                RunMode = SequenceRunMode.Auto
            };
        }
    }

    /// <summary>
    /// Input cassette loading/mapping/unloading sequence coordinator.
    /// Unit classes own axis and I/O operations; this class owns ordered checks,
    /// inter-unit conditions, alarm decisions, and step transitions.
    /// </summary>
    public sealed class InputCassetteSequence
    {
        private readonly MachineSequenceContext _context;

        public InputCassetteSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentStep = InputCassetteSequenceStep.Idle;
        }

        public InputCassetteSequenceStep CurrentStep { get; private set; }
        public InputCassetteSequenceKind CurrentKind { get; private set; }

        public Task<bool> RunLoadingAsync(CancellationToken ct)
        {
            return RunLoadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<bool> RunLoadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return RunAsync(InputCassetteSequenceKind.Loading, ct, options);
        }

        public Task<bool> RunMappingAsync(CancellationToken ct)
        {
            return RunMappingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<bool> RunMappingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return RunAsync(InputCassetteSequenceKind.Mapping, ct, options);
        }

        public Task<bool> RunUnloadingAsync(CancellationToken ct)
        {
            return RunUnloadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<bool> RunUnloadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return RunAsync(InputCassetteSequenceKind.Unloading, ct, options);
        }

        private async Task<bool> RunAsync(InputCassetteSequenceKind kind, CancellationToken ct, InputCassetteSequenceOptions options)
        {
            if (options == null) options = InputCassetteSequenceOptions.Default();

            CurrentKind = kind;
            CurrentStep = GetInitialStep(kind);

            InputCassetteUnit cassette = _context.Machine.InputCassette;
            InputFeederUnit feeder = _context.Machine.InputFeeder;
            int result;

            while (CurrentStep != InputCassetteSequenceStep.Complete &&
                   CurrentStep != InputCassetteSequenceStep.Error)
            {
                ct.ThrowIfCancellationRequested();
                _context.LogPublic("[INPUT-CASSETTE] " + options.RunMode + " " + kind + " step=" + CurrentStep);

                switch (CurrentStep)
                {
                    case InputCassetteSequenceStep.CheckFeederPosition:
                        if (!IsFeederAllowedForCassetteMove(feeder))
                            return Fail("IN-CST-FEEDER-POS", feeder != null ? feeder.Name : "InputFeeder", "Feeder must be in Avoid or Exchange position.");
                        if (kind == InputCassetteSequenceKind.Loading)
                            CurrentStep = InputCassetteSequenceStep.CheckCassetteDetected;
                        else if (kind == InputCassetteSequenceKind.Mapping)
                            CurrentStep = InputCassetteSequenceStep.MoveMappingStartPosition;
                        else
                            CurrentStep = InputCassetteSequenceStep.MoveUnloadingPosition;
                        break;

                    case InputCassetteSequenceStep.CheckLot:
                        if (options.RequireActiveLot && LotStorage.ActiveLot == null)
                            return Fail("IN-CST-NO-LOT", "InputCassetteSequence", "Active lot is required for cassette mapping.");
                        CurrentStep = InputCassetteSequenceStep.CheckCassetteDetected;
                        break;

                    case InputCassetteSequenceStep.CheckCassetteDetected:
                        if (cassette == null || !cassette.IsWaferCassetteExist(ResolveCassetteSize(cassette, options)))
                            return Fail("IN-CST-MISSING", cassette != null ? cassette.Name : "InputCassette", "Input cassette is not detected.");
                        CurrentStep = kind == InputCassetteSequenceKind.Mapping || kind == InputCassetteSequenceKind.Unloading
                            ? InputCassetteSequenceStep.CheckCassetteSize
                            : InputCassetteSequenceStep.MoveLoadingPosition;
                        break;

                    case InputCassetteSequenceStep.CheckCassetteSize:
                        if (!IsCassetteSizeMatched(cassette, options))
                        {
                            if (kind != InputCassetteSequenceKind.Unloading)
                                return Fail("IN-CST-SIZE", cassette.Name, "Input cassette size does not match recipe/config.");

                            _context.LogPublic("[INPUT-CASSETTE] Unloading continues although cassette size does not match recipe/config.");
                        }

                        CurrentStep = kind == InputCassetteSequenceKind.Mapping
                            ? InputCassetteSequenceStep.CheckCassetteMaterial
                            : InputCassetteSequenceStep.MoveLoadingPosition;
                        if (kind == InputCassetteSequenceKind.Unloading)
                            CurrentStep = InputCassetteSequenceStep.CheckFeederPosition;
                        break;

                    case InputCassetteSequenceStep.CheckCassetteMaterial:
                        if (cassette.GetWaferMaterialCassette() == null)
                            return Fail("IN-CST-MATERIAL", cassette.Name, "Input cassette material information is missing.");
                        CurrentStep = InputCassetteSequenceStep.CheckMappingStartCondition;
                        break;

                    case InputCassetteSequenceStep.CheckMappingStartCondition:
                        if (!cassette.CheckWaferCassetteMappingReady())
                            return Fail("IN-CST-MAP-READY", cassette.Name, "Input cassette is not ready for mapping.");
                        if (!HasProcessWaferIfMapped(cassette))
                            _context.LogPublic("[INPUT-CASSETTE] No unprocessed wafer is currently registered. Mapping will refresh wafer information.");
                        CurrentStep = InputCassetteSequenceStep.CheckFeederPosition;
                        break;

                    case InputCassetteSequenceStep.MoveLoadingPosition:
                        result = await cassette.MoveWaferLifterZ(cassette.Recipe.LoaingPosition, options.FineMove).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-LOAD-POS", cassette.Name, "Move loading position failed. result=" + result);
                        result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette, options)).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-LOAD-WAIT", cassette.Name, "Loading position move timeout.");
                        CurrentStep = InputCassetteSequenceStep.Complete;
                        break;

                    case InputCassetteSequenceStep.MoveUnloadingPosition:
                        result = await cassette.MoveWaferLifterZ(cassette.Recipe.UnloadingPosition, options.FineMove).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-UNLOAD-POS", cassette.Name, "Move unloading position failed. result=" + result);
                        result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette, options)).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-UNLOAD-WAIT", cassette.Name, "Unloading position move timeout.");
                        CurrentStep = InputCassetteSequenceStep.Complete;
                        break;

                    case InputCassetteSequenceStep.MoveMappingStartPosition:
                        result = await cassette.MoveToWaferCassetteMappingStartPosition(options.FineMove).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-MAP-START", cassette.Name, "Move mapping start position failed. result=" + result);
                        result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette, options)).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-MAP-START-WAIT", cassette.Name, "Mapping start position move timeout.");
                        CurrentStep = InputCassetteSequenceStep.MoveMappingEndPosition;
                        break;

                    case InputCassetteSequenceStep.MoveMappingEndPosition:
                        result = await cassette.MoveToWaferCassetteMappingEndPosition(options.FineMove).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-MAP-END", cassette.Name, "Move mapping end position failed. result=" + result);
                        result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette, options)).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-MAP-END-WAIT", cassette.Name, "Mapping end position move timeout.");
                        CurrentStep = InputCassetteSequenceStep.ScanSlots;
                        break;

                    case InputCassetteSequenceStep.ScanSlots:
                        result = await cassette.WaferScan(ResolveMoveTimeout(cassette, options), options.FineMove).ConfigureAwait(false);
                        if (result != 0) return Fail("IN-CST-SCAN", cassette.Name, "Wafer scan failed. result=" + result);
                        CurrentStep = InputCassetteSequenceStep.BuildWaferInfo;
                        break;

                    case InputCassetteSequenceStep.BuildWaferInfo:
                        RegisterMappingResult(cassette);
                        CurrentStep = InputCassetteSequenceStep.MoveFirstWaferSlot;
                        break;

                    case InputCassetteSequenceStep.MoveFirstWaferSlot:
                        int firstSlot = cassette.FindNextProcessWaferSlot();
                        if (firstSlot >= 0)
                        {
                            result = await cassette.MoveToWaferCassetteSlotPosition(firstSlot, options.FineMove).ConfigureAwait(false);
                            if (result != 0) return Fail("IN-CST-FIRST-SLOT", cassette.Name, "Move first wafer slot failed. result=" + result);
                            result = await cassette.WaitWaferLifterZMoveDone(ResolveMoveTimeout(cassette, options)).ConfigureAwait(false);
                            if (result != 0) return Fail("IN-CST-FIRST-SLOT-WAIT", cassette.Name, "First wafer slot move timeout.");
                        }
                        CurrentStep = InputCassetteSequenceStep.Complete;
                        break;

                    default:
                        return Fail("IN-CST-STEP", "InputCassetteSequence", "Unsupported cassette sequence step: " + CurrentStep);
                }

            }

            _context.LogPublic("[INPUT-CASSETTE] " + options.RunMode + " " + kind + " complete");
            return true;
        }

        private InputCassetteSequenceStep GetInitialStep(InputCassetteSequenceKind kind)
        {
            if (kind == InputCassetteSequenceKind.Mapping)
                return InputCassetteSequenceStep.CheckLot;
            if (kind == InputCassetteSequenceKind.Unloading)
                return InputCassetteSequenceStep.CheckCassetteDetected;
            return InputCassetteSequenceStep.CheckFeederPosition;
        }

        private bool IsFeederAllowedForCassetteMove(InputFeederUnit feeder)
        {
            if (feeder == null) return false;
            return feeder.IsWaferFeederInAvoidPosition() || feeder.IsWaferFeederInExchangePosition();
        }

        private int ResolveCassetteSize(InputCassetteUnit cassette, InputCassetteSequenceOptions options)
        {
            if (options.RequiredCassetteSize == 8 || options.RequiredCassetteSize == 12)
                return options.RequiredCassetteSize;
            return cassette.Config.InchSelect == 0 ? 8 : 12;
        }

        private bool IsCassetteSizeMatched(InputCassetteUnit cassette, InputCassetteSequenceOptions options)
        {
            int size = ResolveCassetteSize(cassette, options);
            return cassette.IsWaferCassetteExist(size);
        }

        private int ResolveMoveTimeout(InputCassetteUnit cassette, InputCassetteSequenceOptions options)
        {
            return options.MoveTimeoutMs > 0 ? options.MoveTimeoutMs : cassette.Config.ElevatorMoveTimeoutMs;
        }

        private bool HasProcessWaferIfMapped(InputCassetteUnit cassette)
        {
            return cassette.WaferMap == null || cassette.WaferMap.Count == 0 || cassette.HasMoreProcessWafer();
        }

        private void RegisterMappingResult(InputCassetteUnit cassette)
        {
            var arr = new bool[cassette.WaferMap.Count];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = cassette.WaferMap[i];

            SlotMapperRegistry.Update("InputCassette", arr);
            _context.Controller.ApplyInputCassetteMappingCompleted();
        }

        private bool Fail(string alarmCode, string source, string message)
        {
            CurrentStep = InputCassetteSequenceStep.Error;
            AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
            _context.LogPublic("[INPUT-CASSETTE] FAIL " + alarmCode + " - " + message);
            return false;
        }
    }
}
