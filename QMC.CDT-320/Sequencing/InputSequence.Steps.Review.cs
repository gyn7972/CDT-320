using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // Input 자동 시퀀스 사용자 확인 구간 스텝 [9]~[10].
    // ReviewStage -> Complete
    //
    // 이 파일은 InputSequence.ExecuteCurrentInputStepAsync의 switch에서 순수 추출한 것이다(2026-07-27).
    public partial class InputSequence
    {
        // [9] ReviewStage: Align/Die Mapping 결과를 표시하고 작업자의 진행/재실행 결정을 기다린다.
        private async Task<int> ExecuteStepReviewStageAsync(CancellationToken ct)
        {
            InputStageUnit stage = Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
            MachineController controller = Context != null ? Context.Controller : null;
            WaferMaterial reviewWafer = ResolveStageWaferFromRuntimeState();
            if (stage == null || controller == null || reviewWafer == null)
                return Fail("SEQ-IN-REVIEW-MATERIAL", "InputSequence",
                    "InputStage 리뷰 대상 장비 또는 Wafer Material이 없습니다.");

            if (!reviewWafer.HasInputStageAlignResult ||
                !reviewWafer.HasInputStageThetaAlignResult ||
                !reviewWafer.HasInputStageDieMappingResult ||
                reviewWafer.InputStageDieMappingInvalidatedByAlignChange)
            {
                return Fail("SEQ-IN-REVIEW-STATE", "InputSequence",
                    "InputStage 리뷰 전 Align/T Align/Die Mapping 상태가 유효하지 않습니다. wafer=" +
                    (reviewWafer.WaferId ?? ""));
            }

            ResetInputStageReadySignals();
            string pickerReason;
            if (!controller.AreInputStageRunReviewPickersSafe(out pickerReason))
            {
                return Fail("SEQ-IN-REVIEW-PICKER-STATE", "InputSequence",
                    "InputStage Review 진입 전 통합 Picker 안전 조건이 유효하지 않습니다. " +
                    pickerReason);
            }

            string sessionReason;
            if (!controller.TryEnterInputStageRunReviewManual(
                Context,
                reviewWafer.WaferId,
                out sessionReason))
            {
                return Fail("SEQ-IN-REVIEW-MANUAL-ENTER", "InputSequence", sessionReason);
            }

            WriteLog("InputStageRunReview",
                "Align/Die Mapping 사용자 확인을 Manual 상태에서 기다립니다. wafer=" +
                (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex +
                ", outputSequenceMaintained=True - Wait");

            while (controller.IsInputStageRunReviewManualActive)
            {
                UserConfirmResult reviewResult = await stage.WaitForUserConfirmAsync(ct).ConfigureAwait(false);
                InputStageRunReviewDecision decision = reviewResult != null && reviewResult.IsConfirmed
                    ? InputStageRunReviewDecision.ConfirmAndContinue
                    : (reviewResult != null ? reviewResult.Decision : InputStageRunReviewDecision.RetryAlign);

                if (decision == InputStageRunReviewDecision.ConfirmAndContinue)
                {
                    controller.CancelInputStageRunReviewAction();
                    if (!await WaitForInputStageRunReviewActionStopAsync(controller, ct).ConfigureAwait(false))
                    {
                        stage.NotifyUserConfirmProcessingFailed(
                            "Review 수동 동작 또는 Jog가 아직 진행 중입니다. STOP 후 다시 확인하세요.");
                        continue;
                    }

                    int avoidResult = await MoveInputCameraXToAvoidAfterReviewAsync(stage, ct).ConfigureAwait(false);
                    if (avoidResult != 0)
                    {
                        stage.NotifyUserConfirmProcessingFailed(
                            "Input Camera X를 Avoid 위치로 복귀하지 못했습니다. Alarm/인터락을 확인한 뒤 다시 시도하세요.");
                        continue;
                    }

                    string approvalReason;
                    if (!MaterialStateService.CommitInputStageRunReview(
                        reviewWafer,
                        reviewResult,
                        out approvalReason))
                    {
                        stage.NotifyUserConfirmProcessingFailed(approvalReason);
                        continue;
                    }

                    if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                    {
                        string resetReason;
                        WaferMaterial approvalWafer = ResolveStageWaferFromRuntimeState() ?? reviewWafer;
                        bool approvalCleared = MaterialStateService.SetInputStageRunReviewApproval(
                            approvalWafer,
                            false,
                            0,
                            out resetReason);
                        if (!approvalCleared)
                        {
                            return Fail("SEQ-IN-REVIEW-AUTO-RESUME-RESET", "InputSequence",
                                sessionReason + " Auto 복귀 실패 후 Review 승인을 해제하지 못했습니다. " +
                                resetReason);
                        }

                        stage.NotifyUserConfirmProcessingFailed(
                            sessionReason + " Auto 복귀가 완료되지 않아 Review 승인을 해제했습니다. 다시 확인하세요.");
                        WriteLog("InputStageRunReview",
                            "Review Commit 후 Auto 복귀가 실패하여 승인을 fail-closed 해제했습니다. wafer=" +
                            (reviewWafer.WaferId ?? "") + ", reason=" + sessionReason +
                            ", reset=" + resetReason + " - Reset");
                        continue;
                    }

                    _autoStep = InputSequenceAutoStep.Complete;
                    WriteLog("InputStageRunReview",
                        approvalReason + ", CameraXAvoid=True, sameCoordinator=True - Ok");
                    break;
                }

                if (decision == InputStageRunReviewDecision.RetryMapping)
                {
                    string resetReason;
                    MaterialStateService.SetInputStageRunReviewApproval(
                        reviewWafer,
                        false,
                        0,
                        out resetReason);
                    SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                    _restartDieMappingFromReview = true;
                    _autoStep = InputSequenceAutoStep.DieMapping;

                    if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                    {
                        stage.NotifyUserConfirmProcessingFailed(sessionReason);
                        continue;
                    }

                    WriteLog("InputStageRunReview",
                        "사용자가 Die Mapping 개별 재실행을 선택했습니다. wafer=" +
                        (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex + " - Restart");
                    break;
                }

                if (decision == InputStageRunReviewDecision.Stop)
                {
                    string resetReason;
                    MaterialStateService.SetInputStageRunReviewApproval(
                        reviewWafer,
                        false,
                        0,
                        out resetReason);
                    controller.CancelInputStageRunReviewAction();
                    controller.TryExitInputStageRunReviewManual(Context, false, out sessionReason);
                    Context.RequestCycleStop();
                    throw new SequenceStopException(
                        "InputStage Review 중 STOP 요청으로 동일 Auto Coordinator를 안전 정지합니다.");
                }

                string alignResetReason;
                MaterialStateService.SetInputStageRunReviewApproval(
                    reviewWafer,
                    false,
                    0,
                    out alignResetReason);
                SequenceResumeStore.Clear(InputStageAlignSequenceStateName);
                SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                _restartAlignFromReview = true;
                _restartDieMappingFromReview = true;
                _autoStep = InputSequenceAutoStep.AlignStage;

                if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                {
                    stage.NotifyUserConfirmProcessingFailed(sessionReason);
                    continue;
                }

                WriteLog("InputStageRunReview",
                    "사용자가 Align 재실행을 선택하여 센터 검출/T Align과 종속 Die Mapping을 다시 실행합니다. wafer=" +
                    (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex + " - Restart");
                break;
            }

            return 0;
        }

        // [10] Complete: 로딩/정렬/맵핑/사용자 확인이 끝났고, 상위 cycle에서 Picker 완료를 기다리는 상태이다.
        private int ExecuteStepComplete()
        {
            LogPublic("[UNIT-INPUT] Input sequence already complete slot=" + _autoSlotIndex);
            return 0;
        }
    }
}
