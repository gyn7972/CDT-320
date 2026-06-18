using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    internal enum InputSequenceAutoStep
    {
        Mapping,
        ResolveSlot,
        PrepareStageLoad,
        LoadFeederFromCassette,
        LoadFeederToStage,
        RecoverFeeder,
        AlignStage,
        DieMapping,
        Complete
    }

    public class InputSequence : UnitSequenceBase
    {
        private InputSequenceAutoStep _autoStep = InputSequenceAutoStep.Mapping;
        private int _autoSlotIndex = -1;
        private string _autoWaferId = "";

        public InputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "Input")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await ExecuteInputAutoCycleAsync(ct).ConfigureAwait(false);
                    Context.StopIfCycleStopRequested("InputSequence.AutoCycleComplete");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "Input auto sequence canceled. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
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

        private async Task ExecuteInputAutoCycleAsync(CancellationToken ct)
        {
            RestoreInputStepSessionFromRuntimeState();

            await ExecuteInputLoadingStepsUntilStageReadyAsync(ct).ConfigureAwait(false);

            WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
            if (stageWafer == null)
            {
                await Task.Delay(100, ct).ConfigureAwait(false);
                return;
            }

            await WaitPickerToCompleteInputStageDiesAsync(ct).ConfigureAwait(false);
            await UnloadInputStageWaferIfPresentAsync(ct).ConfigureAwait(false);

            ResetInputAutoCycle();
        }

        private async Task ExecuteInputLoadingStepsUntilStageReadyAsync(CancellationToken ct)
        {
            while (_autoStep != InputSequenceAutoStep.Complete)
            {
                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input auto sequence failed. step=" + _autoStep + ", result=" + result);
            }
        }

        private async Task WaitPickerToCompleteInputStageDiesAsync(CancellationToken ct)
        {
            if (!MaterialStateService.HasReadyInputStagePickTarget())
                return;

            Context.Bus.Reset("InputStageDieComplete");
            Context.Bus.Set("InputStageReady");
            await Context.Bus.WaitAsync("InputStageDieComplete", ct).ConfigureAwait(false);
        }

        private async Task UnloadInputStageWaferIfPresentAsync(CancellationToken ct)
        {
            WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
            if (stageWafer == null)
                return;

            int slotIndex = ResolveSlotIndexFromWafer(stageWafer);
            int result = await ExecuteWaferUnloadingAsync(
                ct,
                slotIndex,
                false,
                0,
                SequenceStartMode.Resume).ConfigureAwait(false);

            if (result != 0)
                throw new InvalidOperationException("Input auto wafer unloading failed. slot=" + slotIndex + ", result=" + result);
        }

        private void ResetInputAutoCycle()
        {
            Context.Bus.Reset("InputStageDieComplete");
            Context.Bus.Reset("InputStageReady");
            _autoSlotIndex = -1;
            _autoWaferId = "";
            _autoStep = InputSequenceAutoStep.ResolveSlot;
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                if (_autoStep == InputSequenceAutoStep.Complete)
                    RestoreInputStepSessionFromRuntimeState();

                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input step sequence failed. step=" + _autoStep + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "Input step sequence canceled. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
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

        private void RestoreInputStepSessionFromRuntimeState()
        {
            try
            {
                _autoSlotIndex = -1;
                _autoWaferId = "";

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(stageWafer);
                    _autoWaferId = stageWafer.WaferId ?? "";
                    _autoStep = ResolveStageWaferResumeStep(stageWafer);
                    WriteLog("RestoreInputStepSession",
                        "Input sequence restored from InputStage wafer. wafer=" + _autoWaferId +
                        ", slot=" + _autoSlotIndex +
                        ", step=" + _autoStep + " - Ok");
                    return;
                }

                WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
                if (feederWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(feederWafer);
                    _autoWaferId = feederWafer.WaferId ?? "";
                    _autoStep = InputSequenceAutoStep.LoadFeederToStage;
                    WriteLog("RestoreInputStepSession",
                        "Input sequence restored from InputFeeder wafer. wafer=" + _autoWaferId +
                        ", slot=" + _autoSlotIndex +
                        ", step=" + _autoStep + " - Ok");
                    return;
                }

                _autoStep = IsInputCassetteMappedInRuntimeState()
                    ? InputSequenceAutoStep.ResolveSlot
                    : InputSequenceAutoStep.Mapping;

                WriteLog("RestoreInputStepSession",
                    "Input sequence restored from cassette state. step=" + _autoStep + " - Ok");
            }
            catch (Exception ex)
            {
                _autoStep = InputSequenceAutoStep.Mapping;
                _autoSlotIndex = -1;
                _autoWaferId = "";
                WriteLog("RestoreInputStepSession",
                    "Input sequence runtime restore failed: " + ex.Message + ". Restart from mapping. - Failed");
            }
            finally
            {
            }
        }

        private WaferMaterial ResolveStageWaferFromRuntimeState()
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                WaferMaterial wafer = stage != null ? stage.GetCurrentStageWaferMaterial() : null;
                if (wafer == null)
                    wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer != null && stage != null && stage.CurrentWaferMaterial == null)
                    stage.SetCurrentWaferMaterial(wafer);
                return wafer;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStageWaferFromRuntimeState", "Stage wafer runtime resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private WaferMaterial ResolveFeederWaferFromRuntimeState()
        {
            try
            {
                var feeder = Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null;
                WaferMaterial wafer = feeder != null ? feeder.CurrentWaferMaterial : null;
                if (wafer == null)
                    wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                if (wafer != null && feeder != null && feeder.CurrentWaferMaterial == null)
                    feeder.SetCurrentWaferMaterial(wafer);
                return wafer;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveFeederWaferFromRuntimeState", "Feeder wafer runtime resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private InputSequenceAutoStep ResolveStageWaferResumeStep(WaferMaterial wafer)
        {
            try
            {
                if (wafer != null &&
                    wafer.DieIds != null &&
                    wafer.DieIds.Count > 0 &&
                    !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId))
                {
                    return InputSequenceAutoStep.Complete;
                }

                if (wafer != null && wafer.HasInputStageAlignResult)
                    return InputSequenceAutoStep.DieMapping;

                return InputSequenceAutoStep.AlignStage;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStageWaferResumeStep", "Stage wafer resume step resolve failed: " + ex.Message + " - Failed");
                return InputSequenceAutoStep.AlignStage;
            }
            finally
            {
            }
        }

        private int ResolveSlotIndexFromWafer(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null)
                    return -1;

                if (wafer.SourceSlotNumber >= 0)
                    return wafer.SourceSlotNumber;

                if (wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                    wafer.CurrentLocation.SlotNumber >= 0)
                {
                    return wafer.CurrentLocation.SlotNumber;
                }
            }
            catch (Exception ex)
            {
                WriteLog("ResolveSlotIndexFromWafer", "Input slot resolve from wafer failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private bool IsInputCassetteMappedInRuntimeState()
        {
            try
            {
                if (MaterialStateService.State != null && MaterialStateService.State.Cassettes != null)
                {
                    foreach (var cassette in MaterialStateService.State.Cassettes)
                    {
                        if (cassette != null &&
                            cassette.Role == CassetteMaterialRole.Input1 &&
                            cassette.IsMapped)
                        {
                            return true;
                        }
                    }
                }

                var inputCassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                return inputCassette != null &&
                       inputCassette.WaferMap != null &&
                       inputCassette.WaferMap.Count > 0;
            }
            catch (Exception ex)
            {
                WriteLog("IsInputCassetteMappedInRuntimeState", "Input cassette mapped state resolve failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCurrentInputStepAsync(CancellationToken ct, bool requireVisionAlign)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step start. step=" + _autoStep + " - Start");

                int result;
                switch (_autoStep)
                {
                    // 맵핑 처리
                    case InputSequenceAutoStep.Mapping:
                        result = await ExecuteMappingAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-MAP", "InputSequence", "Input cassette mapping failed. result=" + result);
                        Context.Bus.Set("InputCassetteMapped");
                        _autoStep = InputSequenceAutoStep.ResolveSlot;
                        break;

                    // 슬롯 결정
                    case InputSequenceAutoStep.ResolveSlot:
                        _autoSlotIndex = ResolveCurrentOrNextInputSlot();
                        if (_autoSlotIndex < 0)
                            return StopAutoSequence("No ready wafer slot exists in input cassette.");
                        _autoWaferId = ResolveInputWaferId(_autoSlotIndex);
                        _autoStep = InputSequenceAutoStep.PrepareStageLoad;
                        break;

                    // 스테이지 로드 준비
                    case InputSequenceAutoStep.PrepareStageLoad:
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareLoad", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for prepare load.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunPrepareLoadAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, false, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-PREP", "InputSequence", "Input stage load prepare failed. result=" + result);
                        }
                        _autoStep = InputSequenceAutoStep.LoadFeederFromCassette;
                        break;
                    }

                    // 피더에서 카세트 로드
                    case InputSequenceAutoStep.LoadFeederFromCassette:
                    {
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveCurrentOrNextInputSlot();
                        if (_autoSlotIndex < 0)
                            return Fail("SEQ-IN-STEP-SLOT", "InputSequence", "Input slot was not resolved before feeder cassette loading.");

                        var feederSequence = new InputFeederSequence(Context);
                        InputFeederSequenceOptions feederOptions =
                            BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                        result = await feederSequence.RunLoadFromCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-FEEDER-CST", "InputSequence", "Input feeder cassette loading failed. result=" + result);
                        UpdateInputSlotState(_autoSlotIndex, SlotPresence.Exist, ProcessState.Processing);
                        _autoStep = InputSequenceAutoStep.LoadFeederToStage;
                        break;
                    }

                    // 피더로 스테이지 로드
                    case InputSequenceAutoStep.LoadFeederToStage:
                    {
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveFeederWaferFromRuntimeState());

                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputFeederToStage", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for feeder to stage.");

                            var feederSequence = new InputFeederSequence(Context);
                            InputFeederSequenceOptions feederOptions =
                                BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                            result = await feederSequence.RunLoadToStageAsync(ct, feederOptions).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-FEEDER-STAGE", "InputSequence", "Input feeder stage loading failed. result=" + result);
                        }
                        _autoStep = InputSequenceAutoStep.RecoverFeeder;
                        break;
                    }

                    // 복구 피더 처리
                    case InputSequenceAutoStep.RecoverFeeder:
                    {
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveStageWaferFromRuntimeState());

                        var feederSequence = new InputFeederSequence(Context);
                        InputFeederSequenceOptions feederOptions =
                            BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                        result = await feederSequence.RunRecoverAsync(ct, feederOptions).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-FEEDER-RECOVER", "InputSequence", "Input feeder recover failed. result=" + result);
                        _autoStep = InputSequenceAutoStep.AlignStage;
                        break;
                    }

                    // 얼라인 스테이지 처리
                    case InputSequenceAutoStep.AlignStage:
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputAlign", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for align.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunAlignAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, requireVisionAlign, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-ALIGN", "InputSequence", "Input stage align failed. result=" + result);
                        }
                        _autoStep = InputSequenceAutoStep.DieMapping;
                        break;
                    }

                    // 다이 맵핑 처리
                    case InputSequenceAutoStep.DieMapping:
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputDieMapping", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for die mapping.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunDieMappingAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, requireVisionAlign, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-DIEMAP", "InputSequence", "Input stage die mapping failed. result=" + result);
                        }
                        Context.Bus.Set("InputWaferLoaded");
                        Context.Bus.Set("InputStageReady");
                        _autoStep = InputSequenceAutoStep.Complete;
                        break;
                    }

                    // 시퀀스 완료 처리
                    case InputSequenceAutoStep.Complete:
                        LogPublic("[UNIT-INPUT] Input sequence already complete slot=" + _autoSlotIndex);
                        break;

                    default:
                        return Fail("SEQ-IN-STEP-UNKNOWN", "InputSequence", "Unknown input sequence step. step=" + _autoStep);
                }

                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step complete. nextStep=" + _autoStep + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step canceled. step=" + _autoStep + " - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-STEP-EX", "InputSequence", "Input sequence step failed. step=" + _autoStep + ", error=" + ex.Message);
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
                    return StopAutoSequence("No ready wafer slot exists in input cassette.");

                string waferId = ResolveInputWaferId(slotIndex);
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputPrepareLoad", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for manual wafer loading prepare.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunPrepareLoadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, waferId, false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-PREP", "InputSequence", "Input stage load prepare failed. result=" + result);
                }

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await feederSequence.RunLoadFromCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST", "InputSequence", "Input feeder cassette loading failed. result=" + result);

                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Processing);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputFeederToStage", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for manual feeder to stage.");

                    result = await feederSequence.RunLoadToStageAsync(ct, feederOptions).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-FEEDER-STAGE", "InputSequence", "Input feeder stage loading failed. result=" + result);
                }

                result = await feederSequence.RunRecoverAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-RECOVER", "InputSequence", "Input feeder recover failed after stage loading. result=" + result);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for manual align.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, waferId, false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-ALIGN", "InputSequence", "Input stage align failed. result=" + result);
                }

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
            catch (SequenceStopException)
            {
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

                int result;
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareUnload", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for prepare unload.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunPrepareUnloadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, ResolveInputWaferId(slotIndex), false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-UNLOAD-PREP", "InputSequence", "Input stage unload prepare failed. result=" + result);
                }

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputStageToFeeder", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for stage to feeder.");

                    result = await feederSequence.RunUnloadFromStageAsync(ct, feederOptions).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-FEEDER-STAGE-UNLOAD", "InputSequence", "Input stage to feeder unloading failed. result=" + result);
                }

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
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualWaferAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "InputStageArea resource acquire failed for wafer align.");

                    var stageSequence = new InputStageSequence(Context);
                    return await stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, "", false)).ConfigureAwait(false);
                }
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
                    return Fail("SEQ-IN-MAPPING", "InputSequence", "Input cassette mapping failed. result=" + result);

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
                return Fail("SEQ-IN-MAPPING-EX", "InputSequence", "Input cassette mapping exception: " + ex.Message);
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
                    return Fail("SEQ-INLOAD", "InputSequence", "Input sequence failed to load next wafer.");

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
                return Fail("SEQ-INLOAD-EX", "InputSequence", "LoadNextWafer exception: " + ex.Message);
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

        private Task<SequenceResourceLease> AcquireInputStageAreaAsync(string holder, CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;
            return Context.Resources.AcquireAsync(SequenceResourceKind.InputStageArea, safeHolder, 30000, ct);
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
            ApplyInputStageUnitParameters(options);
            return options;
        }

        private void ApplyInputStageUnitParameters(InputStageSequenceOptions options)
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                if (stage == null || stage.Config == null || options == null)
                    return;

                if (stage.Config.SequenceMoveTimeoutMs > 0)
                    options.MoveTimeoutMs = stage.Config.SequenceMoveTimeoutMs;
                if (stage.Config.AlignConvergenceThresholdDeg > 0.0)
                    options.AlignThetaToleranceDeg = stage.Config.AlignConvergenceThresholdDeg;
                if (stage.Config.MaxAlignIterations > 0)
                    options.AlignRetryCount = stage.Config.MaxAlignIterations;
            }
            catch (Exception ex)
            {
                WriteLog("BuildStageSequenceOptions", "InputStage sequence option parameter apply failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
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

                int slotIndex = cassette.FindNextProcessWaferSlot();
                if (slotIndex < 0 && cassette.IsInputCassetteProcessComplete())
                    cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);

                return slotIndex;
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

        private int ResolveCurrentOrNextInputSlot()
        {
            try
            {
                int slotIndex = ResolveProcessingInputSlot();
                if (slotIndex >= 0)
                    return slotIndex;

                return ResolveNextInputSlot();
            }
            catch (Exception ex)
            {
                WriteLog("ResolveCurrentOrNextInputSlot", "Input current/next slot resolve failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private int ResolveProcessingInputSlot()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null)
                    return -1;

                WaferCassetteMaterial material = cassette.GetWaferMaterialCassette();
                if (material == null || material.Slots == null)
                    return -1;

                for (int i = 0; i < material.Slots.Count; i++)
                {
                    WaferSlotState state = material.Slots[i];
                    if (state != null &&
                        state.Presence == SlotPresence.Exist &&
                        state.Process == ProcessState.Processing)
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("ResolveProcessingInputSlot", "Input processing slot resolve failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private int ResolveInputWaferSize()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette != null && cassette.Config != null)
                    return MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
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
                message = SequenceFailureStore.AppendRecentDetail(message, "InputSequence", alarmCode);
                SequenceFailureStore.Record("InputSequence", Kind.ToString(), "", alarmCode, source, message);
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

        private int StopAutoSequence(string reason)
        {
            try
            {
                WriteLog("InputSequence", "Input sequence stopped: " + reason + " - Stopped");
                LogPublic("[UNIT-INPUT-LOADER] STOP " + reason);
            }
            catch
            {
            }
            finally
            {
            }

            throw new SequenceStopException(reason);
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

