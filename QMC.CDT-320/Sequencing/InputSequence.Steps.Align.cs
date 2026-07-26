using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    // Input 자동 시퀀스 정렬 구간 스텝 [7]~[8].
    // AlignStage -> DieMapping
    //
    // 이 파일은 InputSequence.ExecuteCurrentInputStepAsync의 switch에서 순수 추출한 것이다(2026-07-27).
    public partial class InputSequence
    {
        // [7] AlignStage: InputStageArea를 점유하고 wafer align을 수행한다.
        private async Task<int> ExecuteStepAlignStageAsync(CancellationToken ct, bool requireVisionAlign)
        {
            SequenceStartMode alignStartMode = _restartAlignFromReview
                ? SequenceStartMode.Restart
                : SequenceStartMode.Resume;
            int result = await ExecuteWithInputPickerAvoidGateAsync("InputAlign", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                    // requireVisionAlign이 true이면 StageSequence 내부에서 vision align 조건을 함께 요구한다.
                    var stageSequence = new InputStageSequence(Context);
                    int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "Align",
                        () => stageSequence.RunAlignAsync(
                            ct,
                            BuildStageSequenceOptions(false, alignStartMode, requireVisionAlign, _autoWaferId, false)),
                        "wafer=" + _autoWaferId,
                        "requireVisionAlign=" + requireVisionAlign,
                        "startMode=" + alignStartMode).ConfigureAwait(false);
                    if (stageResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-ALIGN", "InputStage",
                            "InputStage align 실패. result=" + stageResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _restartAlignFromReview = false;
            _autoStep = InputSequenceAutoStep.DieMapping;
            return 0;
        }

        // [8] DieMapping: Align 결과를 기반으로 Stage wafer의 die map 정보를 생성한다.
        private async Task<int> ExecuteStepDieMappingAsync(CancellationToken ct, bool requireVisionAlign)
        {
            SequenceStartMode mappingStartMode = _restartDieMappingFromReview
                ? SequenceStartMode.Restart
                : SequenceStartMode.Resume;
            string dieMappingResumeStep;
            if (mappingStartMode == SequenceStartMode.Resume &&
                ShouldRestartWaferAlignForDieMappingResume(out dieMappingResumeStep))
            {
                // Align부터 다시 하도록 _autoStep을 되돌린다. 이번 스텝은 여기서 끝낸다.
                RestartWaferAlignAfterMissingDieMapPoints(dieMappingResumeStep);
                return 0;
            }

            int result = await ExecuteWithInputPickerAvoidGateAsync("InputDieMapping", ct, async () =>
            {
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputDieMapping", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Die mapping 중 InputStageArea 리소스 점유에 실패했습니다.");

                    InputStageSequenceOptions mappingOptions =
                        BuildStageSequenceOptions(false, mappingStartMode, requireVisionAlign, _autoWaferId, false);
                    // Auto에서는 사용자 리뷰 승인 전 Picker Ready 신호를 발행하지 않는다.
                    mappingOptions.PublishReadySignals = false;

                    // DieMapping이 끝나면 MaterialStateService의 Stage finish 조건이 만족되어야 한다.
                    var stageSequence = new InputStageSequence(Context);
                    int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "DieMapping",
                        () => stageSequence.RunDieMappingAsync(
                            ct,
                            mappingOptions),
                        "wafer=" + _autoWaferId,
                        "requireVisionAlign=" + requireVisionAlign).ConfigureAwait(false);
                    if (stageResult != 0)
                        return Fail("SEQ-IN-STEP-STAGE-DIEMAP", "InputStage",
                            "InputStage die mapping 실패. result=" + stageResult);
                }

                return 0;
            }).ConfigureAwait(false);
            if (result != 0)
                return result;

            _restartDieMappingFromReview = false;
            // Picker Ready는 사용자 리뷰 승인 후에만 발행한다.
            _autoStep = InputSequenceAutoStep.ReviewStage;
            return 0;
        }
    }
}
