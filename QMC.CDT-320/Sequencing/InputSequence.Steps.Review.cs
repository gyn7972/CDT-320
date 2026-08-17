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

            // [시뮬 Review 건너뛰기 2026-08-07] 시뮬레이션에서 웨이퍼를 연속 반복할 때 매 장마다
            // 작업자 확인 조작을 요구하지 않도록, 설정(설정→일반: SKIP RUN REVIEW IN SIMULATION)이
            // 켜져 있으면 확인 화면 없이 바로 운전을 시작한다.
            // [안전] SimulationMode 에서만 적용된다. 실장비/Dry Run 에서는 설정값과 무관하게 항상 확인한다.
            // PickUp 가능 판정(IsInputStageFinishComplete)이 Review 승인을 요구하므로, 확인 화면 대신
            // "작업자가 아무것도 바꾸지 않고 확인만 누른 것"과 동일한 기본 승인을 자동으로 기록한다.
            string reviewBypassReason;
            if (ShouldBypassInputStageRunReviewInSimulation(out reviewBypassReason))
            {
                // Review 확정 후와 동일하게 Input Camera X를 Avoid로 복귀시켜 Picker 진입 경로를 연다.
                int bypassAvoidResult = await MoveInputCameraXToAvoidAfterReviewAsync(stage, ct).ConfigureAwait(false);
                if (bypassAvoidResult != 0)
                    return bypassAvoidResult;

                string autoApprovalReason;
                if (!MaterialStateService.TryApproveInputStageRunReviewWithDefaultOrder(
                        reviewWafer,
                        out autoApprovalReason))
                {
                    return Fail("SEQ-IN-REVIEW-SIM-AUTO-APPROVE", "InputSequence",
                        "시뮬레이션 사용자 확인 자동 승인에 실패했습니다. " + autoApprovalReason);
                }

                _autoStep = InputSequenceAutoStep.Complete;
                WriteLog("InputStageRunReview",
                    "시뮬레이션 설정에 따라 사용자 확인 화면을 건너뛰고 바로 운전을 시작합니다. wafer=" +
                    (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex +
                    ", " + reviewBypassReason + ", CameraXAvoid=True, " + autoApprovalReason + " - Ok");
                return 0;
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
                UserConfirmResult reviewResult;
                try
                {
                    // 사용자 확인 대기는 작업자 입력이 없으면 무기한이다. 경계 폴링으로는 깨울 수 없으므로
                    // CYCLE STOP 토큰을 함께 관찰시켜 정지 요청 시 대기가 즉시 풀리게 한다.
                    using (CancellationTokenSource stoppable = Context.CreateCycleStopLinkedSource(ct))
                    {
                        reviewResult = await stage
                            .WaitForUserConfirmAsync(stoppable.Token)
                            .ConfigureAwait(false);
                    }
                }
                catch (System.OperationCanceledException) when (!ct.IsCancellationRequested &&
                                                                Context.IsCycleStopRequested)
                {
                    // 알람/하드 취소가 아니라 CYCLE STOP으로 깨어난 경우다.
                    // Review 세션과 수동 동작을 정리한 뒤 STOP 선택과 동일한 안전 정지 경로로 합류한다.
                    string stopResetReason;
                    MaterialStateService.SetInputStageRunReviewApproval(
                        reviewWafer,
                        false,
                        0,
                        out stopResetReason);
                    controller.CancelInputStageRunReviewAction();
                    controller.TryExitInputStageRunReviewManual(Context, false, out sessionReason);
                    WriteLog("InputStageRunReview",
                        "CYCLE STOP 요청으로 사용자 확인 대기를 종료했습니다. wafer=" +
                        (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex + " - Stop");
                    Context.StopIfCycleStopRequested("InputSequence.InputStageRunReviewConfirm");
                    throw new SequenceStopException(
                        "InputStage Review 사용자 확인 대기 중 CYCLE STOP 요청으로 안전 정지합니다.");
                }

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
                    // [무언정지 방지 2026-08-17] Review STOP 결정이 CycleStop의 발원임을 디스크에 남긴다.
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        "SYSTEM",
                        "SEQ-REVIEW-STOP-DECISION",
                        "InputSequence",
                        "Review STOP 결정으로 Auto Coordinator를 정지합니다. wafer=" + (reviewWafer.WaferId ?? ""));
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

        /// <summary>
        /// 시뮬레이션에서 Run Review 사용자 확인 화면을 건너뛸지 판단한다.
        /// 실장비 보호를 위해 SimulationMode 가 아니면 설정값과 무관하게 항상 false 를 반환한다.
        /// </summary>
        private static bool ShouldBypassInputStageRunReviewInSimulation(out string reason)
        {
            reason = string.Empty;

            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                // 실장비/Dry Run 에서는 어떤 설정으로도 확인을 생략하지 않는다.
                if (!settings.SimulationMode)
                    return false;

                if (!settings.SkipInputStageRunReviewInSimulation)
                    return false;

                reason = "simulationMode=True, skipRunReviewInSimulation=True";
                return true;
            }
            catch
            {
                // 판단 실패 시에는 확인을 수행하는 안전한 방향으로 처리한다.
                return false;
            }
        }

        // [10] Complete: 로딩/정렬/맵핑/사용자 확인이 끝났고, 상위 cycle에서 Picker 완료를 기다리는 상태이다.
        private int ExecuteStepComplete()
        {
            LogPublic("[UNIT-INPUT] Input sequence already complete slot=" + _autoSlotIndex);
            return 0;
        }
    }
}
