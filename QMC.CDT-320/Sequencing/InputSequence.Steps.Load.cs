using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // Input 자동 시퀀스 로딩 구간 스텝 [1]~[6].
    // Mapping -> ResolveSlot -> PrepareStageLoad -> LoadFeederFromCassette -> LoadFeederToStage -> RecoverFeeder
    //
    // 이 파일은 InputSequence.ExecuteCurrentInputStepAsync의 switch에서 순수 추출한 것이다(2026-07-27).
    // 각 메서드는 0을 반환하면 정상 진행이고, 다음 스텝은 스스로 _autoStep에 지정한다.
    // 0이 아닌 값은 상위(DispatchInputStepAsync)가 그대로 반환해 사이클을 중단한다.
    public partial class InputSequence
    {
        // [1] Mapping: Input Cassette의 slot map/material 상태를 갱신한다.
        private async Task<int> ExecuteStepMappingAsync(CancellationToken ct)
        {
            int result = await ExecuteMappingAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
            if (result != 0)
                return Fail("SEQ-IN-STEP-MAP", "InputSequence", "Input cassette 매핑 실패. result=" + result);

            // Mapping 완료 후 다른 sequence가 확인할 수 있도록 cassette mapped bus를 올린다.
            Context.Bus.Set("InputCassetteMapped");
            _autoStep = InputSequenceAutoStep.ResolveSlot;
            return 0;
        }

        // [2] ResolveSlot: Processing 중인 slot을 우선 사용하고, 없으면 다음 Ready slot을 선택한다.
        private int ExecuteStepResolveSlot()
        {
            _autoSlotIndex = ResolveCurrentOrNextInputSlot();
            if (_autoSlotIndex < 0)
                return StopInputNoReadyWafer();

            // wafer id는 Stage/Feeder 하위 sequence option과 로그 추적에 사용된다.
            _autoWaferId = ResolveInputWaferId(_autoSlotIndex);
            _autoStep = InputSequenceAutoStep.PrepareStageLoad;
            return 0;
        }

        // [3] PrepareStageLoad: Picker가 Avoid로 빠진 상태에서 Stage 로드 준비 위치를 만든다.
        private async Task<int> ExecuteStepPrepareStageLoadAsync(CancellationToken ct)
        {
            int result = await ExecuteWithInputPickerAvoidGateAsync("InputPrepareLoad", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareLoad", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Load 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                    // InputStageSequence가 실제 Stage 준비 동작을 담당한다.
                    var stageSequence = new InputStageSequence(Context);
                    int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "PrepareLoad",
                        () => stageSequence.RunPrepareLoadAsync(
                            ct,
                            BuildStageSequenceOptions(false, SequenceStartMode.Resume, false, _autoWaferId, false)),
                        "wafer=" + _autoWaferId).ConfigureAwait(false);
                    if (stageResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-PREP", "InputStage",
                            "InputStage Load 준비 실패. result=" + stageResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _autoStep = InputSequenceAutoStep.LoadFeederFromCassette;
            return 0;
        }

        // [4] LoadFeederFromCassette: 선택된 cassette slot의 wafer를 InputFeeder로 로드한다.
        private async Task<int> ExecuteStepLoadFeederFromCassetteAsync(CancellationToken ct)
        {
            // 재개 상황에서 slot 정보가 비어 있으면 cassette 상태에서 다시 확인한다.
            if (_autoSlotIndex < 0)
                _autoSlotIndex = ResolveCurrentOrNextInputSlot();
            if (_autoSlotIndex < 0)
                return Fail("SEQ-IN-STEP-SLOT", "InputSequence", "Feeder 카세트 로딩 전에 Input Slot이 결정되지 않았습니다.");

            // InputFeederSequence가 cassette slot 접근과 feeder 적재 동작을 수행한다.
            var feederSequence = new InputFeederSequence(Context);
            InputFeederSequenceOptions feederOptions =
                BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
            int result = await ExecuteWithInputPickerAvoidGateAsync("InputLoadFromCassette", ct, () =>
                SequenceTrace.ChildAsync("InputFeederSequence", "LoadFromCassette",
                    () => feederSequence.RunLoadFromCassetteAsync(ct, feederOptions),
                    "slot=" + _autoSlotIndex)).ConfigureAwait(false);
            if (result != 0)
                return Fail("SEQ-IN-STEP-FEEDER-CST", "InputFeeder",
                    "InputFeeder cassette loading 실패. result=" + result);

            // slot은 이제 작업 중인 wafer로 표시해서 중복 선택을 막는다.
            UpdateInputSlotState(_autoSlotIndex, SlotPresence.Exist, ProcessState.Processing);
            _autoStep = InputSequenceAutoStep.LoadFeederToStage;
            return 0;
        }

        // [5] LoadFeederToStage: InputFeeder의 wafer를 InputStage로 넘긴다.
        private async Task<int> ExecuteStepLoadFeederToStageAsync(CancellationToken ct)
        {
            // Feeder에 wafer만 남은 재개 상황이면 wafer의 source slot에서 slot index를 복원한다.
            if (_autoSlotIndex < 0)
                _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveFeederWaferFromRuntimeState());

            int result = await ExecuteWithInputPickerAvoidGateAsync("InputFeederToStage", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputFeederToStage", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Feeder -> Stage 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                    var feederSequence = new InputFeederSequence(Context);
                    InputFeederSequenceOptions feederOptions =
                        BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                    int feederResult = await SequenceTrace.ChildAsync("InputFeederSequence", "LoadToStage",
                        () => feederSequence.RunLoadToStageAsync(ct, feederOptions),
                        "slot=" + _autoSlotIndex).ConfigureAwait(false);
                    if (feederResult != 0)
                        return Fail("SEQ-IN-STEP-FEEDER-STAGE", "InputFeeder",
                            "InputFeeder -> InputStage loading 실패. result=" + feederResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _autoStep = InputSequenceAutoStep.RecoverFeeder;
            return 0;
        }

        // [6] RecoverFeeder: wafer 전달 후 Feeder를 후속 동작 가능한 상태로 복귀시킨다.
        private async Task<int> ExecuteStepRecoverFeederAsync(CancellationToken ct)
        {
            // Stage에 올라간 wafer 기준으로 slot 정보를 복원할 수 있다.
            if (_autoSlotIndex < 0)
                _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveStageWaferFromRuntimeState());

            var feederSequence = new InputFeederSequence(Context);
            InputFeederSequenceOptions feederOptions =
                BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
            int result = await ExecuteWithInputPickerAvoidGateAsync("InputFeederRecover", ct, () =>
                SequenceTrace.ChildAsync("InputFeederSequence", "Recover",
                    () => feederSequence.RunRecoverAsync(ct, feederOptions),
                    "slot=" + _autoSlotIndex)).ConfigureAwait(false);
            if (result != 0)
                return Fail("SEQ-IN-STEP-FEEDER-RECOVER", "InputFeeder",
                    "InputFeeder recover 실패. result=" + result);

            _autoStep = InputSequenceAutoStep.AlignStage;
            return 0;
        }
    }
}
