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
                WriteLog("ExecuteAutoAsync", "Input 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-EX", "InputSequence", "Input 자동 시퀀스 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task ExecuteInputAutoCycleAsync(CancellationToken ct)
        {
            try
            {
                RestoreInputStepSessionFromRuntimeState();

                await ExecuteInputLoadingStepsUntilStageReadyAsync(ct).ConfigureAwait(false);

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer == null)
                {
                    Context.StopIfCycleStopRequested("InputSequence.WaitStageWafer");
                    await Task.Delay(100, ct).ConfigureAwait(false);
                    return;
                }

                stageWafer = await EnsureInputStageFinishBeforePickerReadyAsync(stageWafer, ct).ConfigureAwait(false);
                PublishInputStageReadySignals(stageWafer);
                await WaitPickerToCompleteInputStageDiesAsync(stageWafer, ct).ConfigureAwait(false);

                await UnloadInputStageWaferIfPresentAsync(ct).ConfigureAwait(false);

                int completeResult = StopAutoSequenceIfInputCassetteComplete();
                if (completeResult != 0)
                    return;

                ResetInputAutoCycle();
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteInputAutoCycleAsync", "Input 자동 사이클이 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-CYCLE", "InputSequence", "Input 자동 사이클 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task<WaferMaterial> EnsureInputStageFinishBeforePickerReadyAsync(WaferMaterial stageWafer, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stageWafer == null)
                    return null;

                string finishReason;
                if (MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    return stageWafer;

                InputSequenceAutoStep resumeStep = ResolveStageWaferResumeStep(stageWafer);
                if (resumeStep == InputSequenceAutoStep.Complete)
                    resumeStep = InputSequenceAutoStep.DieMapping;

                _autoSlotIndex = ResolveSlotIndexFromWafer(stageWafer);
                _autoWaferId = stageWafer.WaferId ?? "";
                _autoStep = resumeStep;

                ResetInputStageReadySignals();
                WriteLog("EnsureInputStageFinishBeforePickerReady",
                    "InputStage가 아직 PickUp 준비 완료 상태가 아니어서 누락된 준비 step부터 재개합니다. " +
                    "wafer=" + _autoWaferId +
                    ", slot=" + _autoSlotIndex +
                    ", resumeStep=" + _autoStep +
                    ", reason=" + finishReason + " - Check");

                await ExecuteInputLoadingStepsUntilStageReadyAsync(ct).ConfigureAwait(false);

                WaferMaterial refreshedWafer = ResolveStageWaferFromRuntimeState();
                if (refreshedWafer == null)
                    throw new InvalidOperationException("InputStage 준비 재개 후 웨이퍼 Material 정보가 없습니다.");

                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage 준비 재개 후에도 Picker PickUp 가능 상태가 아닙니다. " + finishReason);

                return refreshedWafer;
            }
            catch (OperationCanceledException)
            {
                WriteLog("EnsureInputStageFinishBeforePickerReady",
                    "InputStage PickUp 준비 재확인 중 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STAGE-FINISH-RECOVER", "InputSequence",
                    "InputStage PickUp 준비 복구 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task ExecuteInputLoadingStepsUntilStageReadyAsync(CancellationToken ct)
        {
            while (_autoStep != InputSequenceAutoStep.Complete)
            {
                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input 자동 시퀀스 실패. step=" + _autoStep + ", result=" + result);
            }
        }

        private async Task WaitPickerToCompleteInputStageDiesAsync(WaferMaterial stageWafer, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stageWafer == null)
                    return;

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage Finish 상태가 완료가 아닙니다. " + finishReason);

                if (MaterialStateService.IsInputStagePickComplete())
                {
                    Context.Bus.Set("InputStageDieComplete");
                    WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                        "Input stage die pick already complete. wafer=" +
                        (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
                    return;
                }

                Context.Bus.Reset("InputStageDieComplete");
                PublishInputStageReadySignals(stageWafer);

                while (!ct.IsCancellationRequested)
                {
                    Context.StopIfCycleStopRequested("InputSequence.WaitInputStageDieComplete");

                    if (Context.Bus.IsSet("InputStageDieComplete"))
                    {
                        WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                            "Input stage die pick complete signal received. wafer=" +
                            (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
                        return;
                    }

                    if (MaterialStateService.IsInputStagePickComplete())
                    {
                        Context.Bus.Set("InputStageDieComplete");
                        WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                            "Input stage die pick complete by material state. wafer=" +
                            (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
                        return;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                    "InputStage Die Pick 완료 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-PICK-WAIT", "InputSequence",
                    "InputStage Die Pick 완료 대기 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task UnloadInputStageWaferIfPresentAsync(CancellationToken ct)
        {
            try
            {
                ResetInputStageReadySignals();

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
                    throw new InvalidOperationException("InputStage 웨이퍼 자동 언로딩 실패. slot=" + slotIndex + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("UnloadInputStageWaferIfPresentAsync", "InputStage 웨이퍼 언로딩이 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-UNLOAD", "InputSequence", "Input 자동 웨이퍼 언로딩 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void ResetInputAutoCycle()
        {
            ResetInputStageCycleSignals();
            _autoSlotIndex = -1;
            _autoWaferId = "";
            _autoStep = InputSequenceAutoStep.ResolveSlot;
        }

        private void PublishInputStageReadySignals(WaferMaterial stageWafer)
        {
            try
            {
                if (stageWafer == null)
                    throw new InvalidOperationException("InputStage 웨이퍼 Material 정보가 없습니다.");

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage가 Picker PickUp 가능 상태가 아닙니다. " + finishReason);

                Context.Bus.Set("InputWaferLoaded");
                Context.Bus.Set("InputStageDieMapped");
                Context.Bus.Set("InputStageFinishComplete");
                Context.Bus.Set("InputStageReady");

                WriteLog("PublishInputStageReadySignals",
                    "Input stage ready signals published. wafer=" +
                    stageWafer.WaferId +
                    ", slot=" + stageWafer.SourceSlotNumber +
                    ", dieCount=" + (stageWafer.DieIds != null ? stageWafer.DieIds.Count.ToString() : "0") +
                    " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PublishInputStageReadySignals",
                    "Input stage ready signal publish failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private void ResetInputStageReadySignals()
        {
            try
            {
                Context.Bus.Reset("InputStageReady");
                WriteLog("ResetInputStageReadySignals",
                    "Input stage ready signal reset. Picker pickup is blocked for wafer transfer. - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ResetInputStageReadySignals",
                    "Input stage ready signal reset failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private void ResetInputStageCycleSignals()
        {
            try
            {
                Context.Bus.Reset("InputStageDieComplete");
                Context.Bus.Reset("InputStageReady");
                Context.Bus.Reset("InputStageDieMapped");
                Context.Bus.Reset("InputStageFinishComplete");
                WriteLog("ResetInputStageCycleSignals", "Input stage cycle signals reset. - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ResetInputStageCycleSignals",
                    "Input stage cycle signal reset failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private int StopAutoSequenceIfInputCassetteComplete()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null
                    ? Context.Machine.InputCassetteUnit
                    : null;
                if (cassette == null)
                    return 0;

                if (!cassette.IsInputCassetteProcessComplete())
                    return 0;

                cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                NotifyInputCassetteReplacementRequired();
                return StopAutoSequence("입력 카세트의 모든 웨이퍼 작업이 완료되었습니다. 카세트를 교체하세요.");
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-COMPLETE-CHECK", "InputSequence",
                    "입력 카세트 완료 상태 확인 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private int StopInputNoReadyWafer()
        {
            try
            {
                int completeResult = StopAutoSequenceIfInputCassetteComplete();
                if (completeResult != 0)
                    return completeResult;

                string reason = "입력 카세트에서 작업 가능한 Ready 웨이퍼 슬롯을 찾을 수 없습니다. 카세트 매핑 상태와 슬롯의 Process 상태를 확인하세요.";
                AlarmManager.Raise(AlarmSeverity.Warning, "SEQ-IN-NO-READY-WAFER", "InputSequence", reason);
                return StopAutoSequence(reason);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-NO-READY-WAFER-CHECK", "InputSequence",
                    "입력 카세트 Ready 웨이퍼 확인 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                if (_autoStep == InputSequenceAutoStep.Complete)
                    RestoreInputStepSessionFromRuntimeState();

                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input 수동/스텝 시퀀스 실패. step=" + _autoStep + ", result=" + result);
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
                Fail("SEQ-IN-STEP-EX", "InputSequence", "Input 수동/스텝 시퀀스 실패: " + ex.Message);
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
                if (wafer == null)
                    return InputSequenceAutoStep.AlignStage;

                if (wafer.HasInputStageAlignResult &&
                    wafer.HasInputStageDieMappingResult &&
                    wafer.DieIds != null &&
                    wafer.DieIds.Count > 0 &&
                    !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId))
                {
                    return InputSequenceAutoStep.Complete;
                }

                if (wafer.HasInputStageAlignResult)
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
                        return Fail("SEQ-IN-STEP-MAP", "InputSequence", "Input cassette 매핑 실패. result=" + result);
                        Context.Bus.Set("InputCassetteMapped");
                        _autoStep = InputSequenceAutoStep.ResolveSlot;
                        break;

                    // 슬롯 결정
                    case InputSequenceAutoStep.ResolveSlot:
                        _autoSlotIndex = ResolveCurrentOrNextInputSlot();
                        if (_autoSlotIndex < 0)
                            return StopInputNoReadyWafer();
                        _autoWaferId = ResolveInputWaferId(_autoSlotIndex);
                        _autoStep = InputSequenceAutoStep.PrepareStageLoad;
                        break;

                    // 스테이지 로드 준비
                    case InputSequenceAutoStep.PrepareStageLoad:
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareLoad", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Load 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunPrepareLoadAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, false, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-PREP", "InputStage",
                                    "InputStage Load 준비 실패. result=" + result);
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
                            return Fail("SEQ-IN-STEP-SLOT", "InputSequence", "Feeder 카세트 로딩 전에 Input Slot이 결정되지 않았습니다.");

                        var feederSequence = new InputFeederSequence(Context);
                        InputFeederSequenceOptions feederOptions =
                            BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                        result = await feederSequence.RunLoadFromCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-FEEDER-CST", "InputFeeder",
                                "InputFeeder cassette loading 실패. result=" + result);
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
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Feeder -> Stage 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                            var feederSequence = new InputFeederSequence(Context);
                            InputFeederSequenceOptions feederOptions =
                                BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                            result = await feederSequence.RunLoadToStageAsync(ct, feederOptions).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-FEEDER-STAGE", "InputFeeder",
                                    "InputFeeder -> InputStage loading 실패. result=" + result);
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
                            return Fail("SEQ-IN-STEP-FEEDER-RECOVER", "InputFeeder",
                                "InputFeeder recover 실패. result=" + result);
                        _autoStep = InputSequenceAutoStep.AlignStage;
                        break;
                    }

                    // 얼라인 스테이지 처리
                    case InputSequenceAutoStep.AlignStage:
                    {
                        using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputAlign", ct).ConfigureAwait(false))
                        {
                            if (lease == null)
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunAlignAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, requireVisionAlign, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-ALIGN", "InputStage",
                                    "InputStage align 실패. result=" + result);
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
                                return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Die mapping 중 InputStageArea 리소스 점유에 실패했습니다.");

                            var stageSequence = new InputStageSequence(Context);
                            result = await stageSequence.RunDieMappingAsync(
                                ct,
                                BuildStageSequenceOptions(false, SequenceStartMode.Resume, requireVisionAlign, _autoWaferId, false)).ConfigureAwait(false);
                            if (result != 0)
                                return Fail("SEQ-IN-STEP-STAGE-DIEMAP", "InputStage",
                                    "InputStage die mapping 실패. result=" + result);
                        }
                        PublishInputStageReadySignals(ResolveStageWaferFromRuntimeState());
                        _autoStep = InputSequenceAutoStep.Complete;
                        break;
                    }

                    // 시퀀스 완료 처리
                    case InputSequenceAutoStep.Complete:
                        LogPublic("[UNIT-INPUT] Input sequence already complete slot=" + _autoSlotIndex);
                        break;

                    default:
                        return Fail("SEQ-IN-STEP-UNKNOWN", "InputSequence", "알 수 없는 Input 시퀀스 스텝입니다. step=" + _autoStep);
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
                return Fail("SEQ-IN-STEP-EX", "InputSequence", "Input 시퀀스 스텝 실패. step=" + _autoStep + ", error=" + ex.Message);
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
                return Fail("SEQ-IN-MAP-EX", "InputSequence", "Input cassette mapping 시퀀스 실패: " + ex.Message);
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
                return Fail("SEQ-IN-CST-LOAD-EX", "InputSequence", "Input cassette loading 시퀀스 실패: " + ex.Message);
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
                return Fail("SEQ-IN-CST-UNLOAD-EX", "InputSequence", "Input cassette unloading 시퀀스 실패: " + ex.Message);
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
                    return Fail("SEQ-IN-WAFER-MAP", "InputSequence", "웨이퍼 로딩 전 Input cassette mapping 실패. result=" + result);

                int slotIndex = ResolveNextInputSlot();
                if (slotIndex < 0)
                    return StopInputNoReadyWafer();

                string waferId = ResolveInputWaferId(slotIndex);
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputPrepareLoad", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 웨이퍼 로딩 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunPrepareLoadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, waferId, false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-PREP", "InputStage",
                            "InputStage load 준비 실패. result=" + result);
                }

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await feederSequence.RunLoadFromCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST", "InputSequence", "InputFeeder cassette loading 실패. result=" + result);

                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Processing);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputFeederToStage", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 Feeder -> Stage 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                    result = await feederSequence.RunLoadToStageAsync(ct, feederOptions).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-FEEDER-STAGE", "InputSequence", "InputFeeder -> InputStage 로딩 실패. result=" + result);
                }

                result = await feederSequence.RunRecoverAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-RECOVER", "InputSequence", "Stage loading 후 InputFeeder recover 실패. result=" + result);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, waferId, false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-ALIGN", "InputSequence", "InputStage align 실패. result=" + result);
                }

                Context.Bus.Set("InputWaferLoaded");
                ResetInputStageReadySignals();
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
                return Fail("SEQ-IN-WAFER-LOAD-EX", "InputSequence", "Input 웨이퍼 로딩 시퀀스 실패: " + ex.Message);
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
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Unload 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                    var stageSequence = new InputStageSequence(Context);
                    result = await stageSequence.RunPrepareUnloadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, ResolveInputWaferId(slotIndex), false)).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-STAGE-UNLOAD-PREP", "InputStage",
                            "InputStage unload 준비 실패. result=" + result);
                }

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputStageToFeeder", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Stage -> Feeder 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                    result = await feederSequence.RunUnloadFromStageAsync(ct, feederOptions).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("SEQ-IN-FEEDER-STAGE-UNLOAD", "InputSequence", "InputStage -> InputFeeder 언로딩 실패. result=" + result);
                }

                result = await feederSequence.RunUnloadToCassetteAsync(ct, feederOptions).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST-UNLOAD", "InputSequence", "InputFeeder -> 카세트 언로딩 실패. result=" + result);

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
                return Fail("SEQ-IN-WAFER-UNLOAD-EX", "InputSequence", "Input 웨이퍼 언로딩 시퀀스 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCurrentWaferUnloadingAsync(
            CancellationToken ct,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                int slotIndex = ResolveSlotIndexFromWafer(stageWafer);
                if (slotIndex < 0)
                    slotIndex = ResolveProcessingInputSlot();

                if (slotIndex < 0)
                    return Fail("SEQ-IN-MANUAL-UNLOAD-SLOT", "InputSequence", "Input UNLOAD 대상 슬롯을 확인할 수 없습니다. InputStage 자재와 카세트 Processing 슬롯 상태를 확인하세요.");

                return await ExecuteWaferUnloadingAsync(
                    ct,
                    slotIndex,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCurrentWaferUnloadingAsync", "Input Manual UNLOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MANUAL-UNLOAD-EX", "InputSequence", "Input Manual UNLOAD 중 예외가 발생했습니다: " + ex.Message);
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
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Wafer align 중 InputStageArea 리소스 점유에 실패했습니다.");

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
                return Fail("SEQ-IN-WAFER-ALIGN-EX", "InputSequence", "Input 웨이퍼 Align 시퀀스 실패: " + ex.Message);
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
                    return Fail("SEQ-IN-MAPPING", "InputSequence", "Input cassette mapping 실패. result=" + result);

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
                return Fail("SEQ-INLOAD", "InputSequence", "다음 Input wafer loading에 실패했습니다.");

                Context.Bus.Set("InputWaferLoaded");
                ResetInputStageReadySignals();
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
                return Fail("SEQ-INLOAD-EX", "InputSequence", "다음 웨이퍼 로딩 실행 중 예외가 발생했습니다: " + ex.Message);
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
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<SequenceResourceLease> AcquireInputStageAreaAsync(string holder, CancellationToken ct)
        {
            try
            {
                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    SequenceResourceKind.InputStageArea,
                    safeHolder,
                    30000,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception)
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
                {
                    cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                    NotifyInputCassetteReplacementRequired();
                }

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
            catch (Exception ex)
            {
                WriteLog("ResolveInputWaferSize", "Input wafer size 확인 중 예외가 발생했습니다: " + ex.Message + " - Failed");
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
                WriteLog(source, "실패 처리 중 예외가 발생했습니다: " + ex.Message + " - Failed");
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
                WriteLog("InputSequence", "Input 시퀀스 정지: " + reason + " - Stopped");
                LogPublic("[UNIT-INPUT-LOADER] STOP " + reason);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence StopAutoSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            throw new SequenceStopException(reason);
        }

        private void NotifyInputCassetteReplacementRequired()
        {
            try
            {
                Context.RequestOperatorMessage(
                    "입력 카세트 교체",
                    "입력 카세트의 모든 웨이퍼 작업이 완료되었습니다.\r\n카세트를 교체한 뒤 필요한 작업을 진행하세요.");
            }
            catch (Exception ex)
            {
                WriteLog("NotifyInputCassetteReplacementRequired",
                    "입력 카세트 교체 메시지 표시 요청 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void LogPublic(string message)
        {
            try
            {
                Context.LogPublic(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence public log failed: " + ex.Message);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence log failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}

