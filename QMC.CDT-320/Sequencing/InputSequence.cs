using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public class InputSequence : UnitSequenceBase
    {
        public InputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "InputLoader")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                int result = await ExecuteWaferLoadingAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input auto sequence failed at wafer loading. result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "Input auto sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-EX", "InputSequence", "Input auto sequence failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await ExecuteWaferLoadingAsync(ct, false, 0, SequenceStartMode.Resume, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input step sequence failed at wafer loading. result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "Input step sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STEP-EX", "InputSequence", "Input step sequence failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunMappingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingAsync", "Input cassette mapping sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAP-EX", "InputSequence", "Input cassette mapping sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteLoadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunLoadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteLoadingAsync", "Input cassette loading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-LOAD-EX", "InputSequence", "Input cassette loading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                var sequence = new InputCassetteSequence(Context);
                return await sequence.RunUnloadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteUnloadingAsync", "Input cassette unloading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-UNLOAD-EX", "InputSequence", "Input cassette unloading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteWaferLoadingAsync(
            CancellationToken ct,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume,
            bool requireVisionAlign = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT] Wafer loading start");
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence start. - Start");

                int result = await ExecuteMappingAsync(ct, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-WAFER-MAP", "InputSequence", "Input cassette mapping failed before wafer loading. result=" + result);

                int slotIndex = ResolveNextInputSlot();
                if (slotIndex < 0)
                    return Fail("SEQ-IN-WAFER-NEXT", "InputSequence", "No ready wafer slot exists in input cassette.");

                var stageSequence = new InputStageSequence(Context);
                string waferId = ResolveInputWaferId(slotIndex);
                result = await stageSequence.RunPrepareLoadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, waferId, false)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-STAGE-PREP", "InputSequence", "Input stage load prepare failed. result=" + result);

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await feederSequence.RunLoadFromCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST", "InputSequence", "Input feeder cassette loading failed. result=" + result);

                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Processing);

                result = await feederSequence.RunLoadToStageAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-STAGE", "InputSequence", "Input feeder stage loading failed. result=" + result);

                result = await feederSequence.RunRecoverAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-RECOVER", "InputSequence", "Input feeder recover failed after stage loading. result=" + result);

                result = await stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, waferId, false)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-STAGE-ALIGN", "InputSequence", "Input stage align failed. result=" + result);

                Context.Bus.Set("InputWaferLoaded");
                Context.Bus.Set("InputStageReady");
                LogPublic("[UNIT-INPUT] Wafer loading complete slot=" + slotIndex);
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence completed. slot=" + slotIndex + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-LOAD-EX", "InputSequence", "Input wafer loading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteWaferUnloadingAsync(
            CancellationToken ct,
            int slotIndex,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT] Wafer unloading start slot=" + slotIndex);
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence start. slot=" + slotIndex + " - Start");

                var stageSequence = new InputStageSequence(Context);
                int result = await stageSequence.RunPrepareUnloadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, ResolveInputWaferId(slotIndex), false)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-STAGE-UNLOAD-PREP", "InputSequence", "Input stage unload prepare failed. result=" + result);

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await feederSequence.RunUnloadFromStageAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-STAGE-UNLOAD", "InputSequence", "Input stage to feeder unloading failed. result=" + result);

                result = await feederSequence.RunUnloadToCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST-UNLOAD", "InputSequence", "Input feeder to cassette unloading failed. result=" + result);

                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Done);
                ClearInputStageRuntime();
                Context.Bus.Set("InputWaferUnloaded");
                LogPublic("[UNIT-INPUT] Wafer unloading complete slot=" + slotIndex);
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence completed. slot=" + slotIndex + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-UNLOAD-EX", "InputSequence", "Input wafer unloading sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteWaferAlignAsync(
            CancellationToken ct,
            bool bFine = false,
            SequenceStartMode startMode = SequenceStartMode.Resume,
            bool requireVisionAlign = false)
        {
            try
            {
                var stageSequence = new InputStageSequence(Context);
                return await stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, "", false)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferAlignAsync", "Input wafer align sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-ALIGN-EX", "InputSequence", "Input wafer align sequence failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteMappingFirstAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping start");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping requested as first input sequence step. - Start");

                int result = await ExecuteMappingAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (result != 0)
                    return Fail("SEQ-IN-MAPPING", "InputLoaderSequence", "Input cassette mapping failed. result=" + result);

                Context.Bus.Set("InputCassetteMapped");
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping complete");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping completed as first input sequence step. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAPPING-EX", "InputLoaderSequence", "Input cassette mapping exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteLoadOnceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer start");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer requested. - Start");

                bool ok = await Context.Controller.LoadNextWaferAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (!ok)
                    return Fail("SEQ-INLOAD", "InputLoaderSequence", "InputLoader failed to load next wafer.");

                Context.Bus.Set("InputStageReady");
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer complete");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-INLOAD-EX", "InputLoaderSequence", "LoadNextWafer exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private InputCassetteSequenceOptions BuildCassetteSequenceOptions(bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            try
            {
                var options = InputCassetteSequenceOptions.Default();
                options.FineMove = bFine;
                options.MoveTimeoutMs = moveTimeoutMs;
                options.RunMode = Mode;
                options.StartMode = startMode;
                return options;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private InputFeederSequenceOptions BuildFeederSequenceOptions(
            int slotIndex,
            int nextSlotIndex,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            var options = InputFeederSequenceOptions.Default();
            options.SlotIndex = slotIndex;
            options.NextSlotIndex = nextSlotIndex;
            options.WaferSize = ResolveInputWaferSize();
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private InputStageSequenceOptions BuildStageSequenceOptions(
            bool bFine,
            SequenceStartMode startMode,
            bool requireVisionAlign,
            string waferId,
            bool requireMapData)
        {
            var options = InputStageSequenceOptions.Default();
            options.FineMove = bFine;
            options.RunMode = Mode;
            options.StartMode = startMode;
            options.RequireVisionAlign = requireVisionAlign;
            options.WaferId = waferId ?? "";
            options.RequireMapData = requireMapData;
            return options;
        }

        private string ResolveInputWaferId(int slotIndex)
        {
            return "INPUT-SLOT-" + (slotIndex + 1).ToString("00");
        }

        private void UpdateInputSlotState(int slotIndex, SlotPresence presence, ProcessState state)
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette != null && slotIndex >= 0)
                    cassette.UpdateWaferCassetteSlotState(slotIndex, presence, state);
            }
            catch (Exception ex)
            {
                WriteLog("UpdateInputSlotState", "Input slot state update failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ClearInputStageRuntime()
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                if (stage != null)
                    stage.ClearCurrentWaferMap();
            }
            catch (Exception ex)
            {
                WriteLog("ClearInputStageRuntime", "Input stage runtime clear failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private int ResolveNextInputSlot()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null)
                    return -1;

                return cassette.FindNextProcessWaferSlot();
            }
            catch (Exception ex)
            {
                WriteLog("ResolveNextInputSlot", "Input next slot resolve failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private int ResolveInputWaferSize()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette != null && cassette.Config != null)
                    return cassette.Config.InchSelect == 0 ? 8 : 12;
            }
            catch
            {
            }
            finally
            {
            }

            return 12;
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                LogPublic("[UNIT-INPUT-LOADER] FAIL " + alarmCode + " - " + message);
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

        private void LogPublic(string message)
        {
            try
            {
                Context.LogPublic(message);
            }
            catch
            {
            }
            finally
            {
            }
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
