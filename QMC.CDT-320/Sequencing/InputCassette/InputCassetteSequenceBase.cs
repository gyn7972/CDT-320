using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal abstract class InputCassetteSequenceBase<TStep> where TStep : struct
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
            CurrentStep = IdleStep;
        }

        protected MachineSequenceContext Context { get; private set; }
        protected InputCassetteSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected InputCassetteSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }
        protected abstract TStep IdleStep { get; }
        protected abstract TStep InitialStep { get; }
        protected abstract TStep CompleteStep { get; }
        protected abstract TStep ErrorStep { get; }

        protected InputCassetteUnit Cassette
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null; }
        }

        protected InputFeederUnit Feeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null; }
        }

        public async Task<int> RunAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            using (SequenceLog.Push(QMC.Common.Logging.EventKind.InputSeq, Name, () => CurrentStep.ToString()))
            try
            {
                Options = options ?? InputCassetteSequenceOptions.Default();
                CurrentStep = ResolveStartStep(InitialStep);
                SequenceResumeStore.MarkRunning(SequenceStateName, CurrentStep.ToString());

                while (!IsStep(CurrentStep, CompleteStep) && !IsStep(CurrentStep, ErrorStep))
                {
                    ct.ThrowIfCancellationRequested();
                    Context.LogPublic("[INPUT-CASSETTE] " + Options.RunMode + " " + Kind + " step=" + CurrentStep);

                    TStep executingStep = CurrentStep;
                    int result = await AwaitStepWithCancellationAsync(ExecuteCurrentStepAsync(ct), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (result != 0)
                        return result;

                    if (!IsStep(CurrentStep, ErrorStep))
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

        protected int CheckLot(TStep nextStep)
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

        protected int CheckCassetteDetected(TStep nextStep)
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

        protected int CheckCassetteSize(TStep nextStep, bool allowMismatch)
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

        protected int CheckCassetteMaterial(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                var material = cassette.GetWaferMaterialCassette();
                if (material == null)
                    return Fail("IN-CST-MATERIAL", cassette.Name, "Input cassette material information is missing.");
                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (slotCount <= 0 || material.Slots == null || material.Slots.Count != slotCount)
                    return Fail("IN-CST-MATERIAL-SLOT", cassette.Name, "Input cassette material slot information does not match cassette config.");

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

        protected int CheckMappingStartCondition(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                string readyReason;
                bool ready = cassette.CheckWaferCassetteMappingReady(out readyReason);
                if (!IsHardwareBypassed() && !ready)
                    return Fail("IN-CST-MAP-READY", cassette.Name, "Input cassette is not ready for mapping. " + readyReason);
                if (IsHardwareBypassed() && !ready)
                    Context.LogPublic("[INPUT-CASSETTE] Hardware bypass: mapping ready sensor check skipped. " + readyReason);
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

        protected int CheckFeederPosition(TStep nextStep)
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

        protected async Task<int> MoveLoadingPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                double target = cassette.Recipe.LoaingPosition;
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-LOAD-POS", cassette.Name,
                        "Move loading position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                result = await WaitCassetteZInPositionAsync(cassette, target, "IN-CST-LOAD", "Loading position", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = CompleteStep;
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

        protected async Task<int> MoveUnloadingPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                double target = cassette.Recipe.UnloadingPosition;
                int result = await cassette.MoveWaferLifterZ(target, Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-UNLOAD-POS", cassette.Name,
                        "Move unloading position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                result = await WaitCassetteZInPositionAsync(cassette, target, "IN-CST-UNLOAD", "Unloading position", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = CompleteStep;
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

        protected async Task<int> MoveMappingStartPositionAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");
                string readyReason;
                if (!cassette.CheckWaferCassetteMoveReady(out readyReason))
                    return Fail("IN-CST-MOVE-READY", cassette.Name, "Input cassette is not ready to move. " + readyReason);

                double target = cassette.Recipe.MappingStartPosition;
                int result = await cassette.MoveToWaferCassetteMappingStartPosition(Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-MAP-START", cassette.Name,
                        "Move mapping start position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                result = await WaitCassetteZInPositionAsync(cassette, target, "IN-CST-MAP-START", "Mapping start position", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = nextStep;
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

        protected async Task<int> MoveMappingEndPositionAsync(TStep nextStep, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                double target = cassette.Recipe.MappingEndPosition;
                int result = await cassette.MoveToWaferCassetteMappingEndPosition(Options.FineMove).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-CST-MAP-END", cassette.Name,
                        "Move mapping end position failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                result = await WaitCassetteZInPositionAsync(cassette, target, "IN-CST-MAP-END", "Mapping end position", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = nextStep;
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

        protected async Task<int> ScanSlotsAsync(TStep nextStep)
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
                    int result = await cassette.WaferScanFromCurrentStart(ResolveMoveTimeout(cassette), Options.FineMove).ConfigureAwait(false);
                    if (result != 0) return Fail("IN-CST-SCAN", cassette.Name, "Wafer scan failed. result=" + result);
                }

                CurrentStep = nextStep;
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

        protected int BuildWaferInfo(TStep nextStep)
        {
            try
            {
                var cassette = Cassette;
                int result = RegisterMappingResult(cassette);
                if (result != 0)
                    return Fail("IN-CST-BUILD-WAFER", cassette != null ? cassette.Name : "InputCassette", "Input cassette material mapping result registration failed. result=" + result);

                CurrentStep = nextStep;
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

        protected async Task<int> MoveFirstWaferSlotAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var cassette = Cassette;
                if (cassette == null)
                    return Fail("IN-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

                int slotCount = cassette.Config != null ? cassette.Config.SlotCount : 0;
                if (slotCount > 0)
                {
                    double target = cassette.CalculateWaferCassetteSlotTargetPosition(0);
                    int result = await cassette.MoveToWaferCassetteSlotPosition(0, Options.FineMove).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("IN-CST-FIRST-SLOT", cassette.Name,
                            "Move slot 1 failed. result=" + result + ". " + BuildCassetteZState(cassette, target));

                    result = await WaitCassetteZInPositionAsync(cassette, target, "IN-CST-FIRST-SLOT", "Slot 1 position", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                CurrentStep = CompleteStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("IN-CST-FIRST-SLOT-EX", Name, "Move slot 1 exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected int FailUnsupportedStep()
        {
            return Fail("IN-CST-STEP", Name, "Unsupported cassette sequence step: " + CurrentStep);
        }

        private async Task<int> WaitCassetteZInPositionAsync(
            InputCassetteUnit cassette,
            double target,
            string alarmPrefix,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (cassette == null)
                    return Fail(alarmPrefix + "-UNIT-MISSING", "InputCassette", description + " wait failed. Input cassette unit is null.");

                AxisMoveWaitResult waitResult = await cassette.WaitWaferLifterZMoveDoneInPosition(
                    target,
                    ResolveMoveTimeout(cassette),
                    ct).ConfigureAwait(false);
                if (waitResult.Success)
                    return 0;

                return Fail(ResolveAxisMoveWaitAlarmCode(alarmPrefix, waitResult.Failure), cassette.Name,
                    description + " move/in-position wait failed. waitResult=" + waitResult.Code +
                    ", reason=" + waitResult.Reason + ". " + waitResult.AxisState);
            }
            catch (Exception ex)
            {
                return Fail(alarmPrefix + "-WAIT-EX", cassette != null ? cassette.Name : Name,
                    description + " move/in-position wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitFailure failure)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, failure);
        }

        private string BuildCassetteZState(InputCassetteUnit cassette, double target)
        {
            if (cassette == null || cassette.InputLifterZ == null)
                return "CassetteZ=null, target=" + target;

            double tolerance = cassette.ResolveWaferLifterZInPositionTolerance();
            return "CassetteZ name=" + cassette.InputLifterZ.Name +
                   ", servo=" + cassette.InputLifterZ.IsServoOn +
                   ", alarm=" + cassette.InputLifterZ.IsAlarm +
                   ", alarmCode=" + cassette.InputLifterZ.AlarmCode +
                   ", moving=" + cassette.InputLifterZ.IsMoving +
                   ", actual=" + cassette.InputLifterZ.ActualPosition +
                   ", command=" + cassette.InputLifterZ.CommandPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   ", inPosition=" + cassette.IsWaferLifterZInPosition(target, tolerance);
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

        private TStep ResolveStartStep(TStep defaultStep)
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
                TStep parsed;
                if (Enum.TryParse(stepText, out parsed) &&
                    !IsStep(parsed, IdleStep) &&
                    !IsStep(parsed, CompleteStep) &&
                    !IsStep(parsed, ErrorStep))
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
            return MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
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
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Cassette != null && Cassette.Setup != null && Cassette.Setup.IsSimulationMode) ||
                   (Cassette != null && Cassette.Config != null && Cassette.Config.bDryRun);
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

                IReadOnlyList<bool> level1Map = BuildCassetteLevelMap(cassette.WaferMap, cassette.Config != null ? cassette.Config.SlotCount : arr.Length, 1);
                IReadOnlyList<bool> level2Map = BuildCassetteLevelMap(cassette.WaferMap, cassette.Config != null ? cassette.Config.SlotCount : arr.Length, 2);

                SlotMapperRegistry.Update("InputCassette", arr);
                int inchSelect = cassette.Config != null ? cassette.Config.InchSelect : 0;
                MaterialStateService.UpdateInputCassetteMapping(
                    ResolveInputCassetteLevelCount(cassette),
                    cassette.Config != null ? cassette.Config.SlotCount : arr.Length,
                    level1Map,
                    level2Map,
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

        private static IReadOnlyList<bool> BuildCassetteLevelMap(IReadOnlyList<bool> map, int slotCount, int level)
        {
            if (slotCount < 0)
                slotCount = 0;

            var levelMap = new bool[slotCount];
            if (map == null || slotCount == 0)
                return levelMap;

            int offset = Math.Max(0, level - 1) * slotCount;
            for (int i = 0; i < slotCount; i++)
            {
                int sourceIndex = offset + i;
                levelMap[i] = sourceIndex >= 0 && sourceIndex < map.Count && map[sourceIndex];
            }

            return levelMap;
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

        private static async Task<AxisMoveWaitResult> AwaitStepWithCancellationAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            if (stepTask == null)
                return new AxisMoveWaitResult(AxisMoveWaitFailure.AxisMissing, "Step task is null.", string.Empty);

            if (stepTask.IsCompleted)
                return await stepTask.ConfigureAwait(false);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, stepTask))
                ct.ThrowIfCancellationRequested();

            return await stepTask.ConfigureAwait(false);
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
