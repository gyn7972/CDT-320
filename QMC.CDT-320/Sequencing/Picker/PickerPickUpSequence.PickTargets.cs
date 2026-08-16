using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPickUpSequence
    {
        // 기존 조건: 동기 메서드 CalculatePickTargets(bool) — 배치의 모든 항목이 VisionOffset을 보유한 상태로 진입했다.
        // 현재 기준(조기 허가): EPD 핸들만 보유한 항목의 RESULT를 좌표 계산 직전에 회수한다 — async 전환.
        private async Task<int> CalculatePickTargetsAsync(bool moveVisionAfterCalculation, CancellationToken ct)
        {
            try
            {
                // [사용자 승인 2026-07-28, A안] 첫 픽 RESULT만 동기 회수하고 나머지는 백그라운드로
                //   회수한다 — 4건 전량 동기 회수(실측 0.4~2.5초)가 비전 독립 회피(약 1.15초)보다
                //   길어지는 사이클에서 픽커 진입이 늦어 팔로잉이 성립하지 못했다(19사이클 중 7건 실패).
                //   아직 RESULT가 없는 항목은 보정 없는 다이 좌표에 안전 마진을 적용한 잠정 목표로
                //   회피 클리어런스만 산출하고, 각 픽 직전에 정확 좌표로 재확정한다.
                int collectResult = await CollectPendingBatchVisionResultsAsync(ct, true).ConfigureAwait(false);
                if (collectResult != 0)
                    return collectResult;

                if (_pickBatchItems.Count == 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision RESULT 회수 후 남은 PickUp 대상이 없어 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerPickUpStep.Complete;
                    ReleaseInputStageArea();
                    return 0;
                }

                _pickCursor = 0;

                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    SetCurrentBatchItem(_pickBatchItems[i]);

                    int result = _visionOffset != null
                        ? CalculateCurrentPickTarget()
                        : CalculateProvisionalPickTargetWithoutVisionOffset();
                    if (result != 0)
                        return result;

                    SaveCurrentStateToBatchItem();
                }

                CurrentStep = moveVisionAfterCalculation
                    ? PickerPickUpStep.MoveInputVisionToAvoidForPickerMove
                    : PickerPickUpStep.SelectNextPickTarget;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-TARGET-BATCH-EX", Name,
                    "Pick target batch calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// 조기 허가(EPD 시점 허가) 배치의 RESULT 회수/적용 — 좌표 계산 직전 수행.
        /// 항목 순서(피커 4→1)대로: ①VisionOffset이 없으면 회수 코어로 RESULT 회수(실패 시 해당 Die SKIP —
        /// prepare의 skip 처리와 동일 경로) ②자재 기록 미적용 항목은 InputPickVision 기록 적용.
        /// 루프 후 픽업이 기록을 적용한 마지막 항목이 배치 마지막이면 미촬영 다이 좌표 전파를 1회 수행
        /// (prepare ApplyInputDieVisionOffset의 마지막 다이 처리와 동일). 내부 경로/이미 적용된 항목은 전부 통과.
        /// </summary>
        private async Task<int> CollectPendingBatchVisionResultsAsync(CancellationToken ct, bool firstItemOnly = false)
        {
            List<PickUpBatchItem> failedItems = null;
            PickUpBatchItem lastAppliedItem = null;

            for (int i = 0; i < _pickBatchItems.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                PickUpBatchItem item = _pickBatchItems[i];
                if (item == null)
                    continue;

                // [사용자 승인 2026-07-28, A안] 첫 픽만 동기 회수하고 나머지는 여기서 중단한다 —
                //   남은 항목은 각 픽 직전(SelectNextPickTarget)에 회수·확정한다. 회수 자체는
                //   Vision 서비스가 EPD 시점부터 백그라운드로 진행 중이므로 대기 시간은 짧다.
                if (firstItemOnly && i > 0 && item.VisionOffset == null)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision RESULT 회수를 각 픽 직전으로 이연합니다(A안 — 첫 픽만 선회수). " +
                        "die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo +
                        ", pickIndex=" + (i + 1) +
                        "/" + _pickBatchItems.Count + " - Check");
                    continue;
                }

                if (item.VisionOffset == null)
                {
                    if (item.VisionRequest == null)
                    {
                        return Fail("PICKER-PICKUP-VISION-COLLECT-HANDLE", "Vision",
                            "RESULT 회수 대상 항목에 Vision 핸들이 없습니다. die=" + item.DieId +
                            ", pickerNo=" + item.PickerNo);
                    }

                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision RESULT 회수 시작(조기 허가 — CalculatePickTargets 시점). " +
                        "die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo +
                        ", requestIndex=" + item.VisionRequestIndex + " - Start");

                    VisionAlignResult offset = await InputDieVisionPrepareSequence.CollectInputDieVisionResultCoreAsync(
                        item.VisionRequest,
                        ct).ConfigureAwait(false);

                    // 과대 보정 가드(Y 전용, 사용자 확정 2026-07-29): 비전이 인접 다이를 오매칭해
                    //   다이 피치급 보정값을 돌려주면 스테이지가 한 피치 이동해 이미 픽업된 옆자리로
                    //   내려간다(실장비 2026-07-29 확인). 한계 초과 보정은 적용하지 않고 RESULT 미수신과
                    //   동일하게 Wait로 되돌린다. 판정은 Y만 — X/T는 판정하지 않는다.
                    string offsetRejectReason = null;
                    if (offset != null)
                    {
                        double limitY;
                        double dieSizeY;
                        string rangeDetail;
                        if (IsInputDieVisionOffsetYOutOfRange(offset, out limitY, out dieSizeY, out rangeDetail))
                        {
                            offsetRejectReason = "OffsetYOutOfRange";
                            WriteLog("PickerPickUpSequence",
                                Name + " Input die vision Y 보정값이 한계를 초과해 폐기하고 Die를 Wait로 남깁니다. " +
                                "die=" + item.DieId +
                                ", pickerNo=" + item.PickerNo +
                                ", deltaY=" + offset.DeltaY.ToString("F6") +
                                ", limitY=" + limitY.ToString("F6") +
                                ", dieSizeY=" + dieSizeY.ToString("F6") +
                                ", ratio=0.50" +
                                ", deltaX=" + offset.DeltaX.ToString("F6") + "(판정제외)" +
                                ", detail=" + rangeDetail + " - Check");
                        }
                        else if (!string.IsNullOrEmpty(rangeDetail))
                        {
                            WriteLog("PickerPickUpSequence",
                                Name + " Input die vision 과대 보정 가드를 적용하지 않습니다. " +
                                "die=" + item.DieId +
                                ", pickerNo=" + item.PickerNo +
                                ", detail=" + rangeDetail + " - Check");
                        }
                    }

                    if (offset == null || offsetRejectReason != null)
                    {
                        int waitResult = ReturnBatchItemToWaitForVisionResultFailure(
                            item,
                            offsetRejectReason ?? "ResultNotReceived");
                        if (waitResult != 0)
                            return waitResult;

                        if (failedItems == null)
                            failedItems = new List<PickUpBatchItem>();
                        failedItems.Add(item);
                        continue;
                    }

                    item.VisionOffset = offset;
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision RESULT 회수 완료(조기 허가). die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo +
                        ", dx=" + offset.DeltaX +
                        ", dy=" + offset.DeltaY +
                        ", dt=" + offset.DeltaTheta + " - Ok");
                }

                if (!item.VisionOffsetApplied)
                {
                    int applyResult = ApplyInputPickVisionRecordForBatchItem(item);
                    if (applyResult != 0)
                        return applyResult;

                    item.VisionOffsetApplied = true;
                    lastAppliedItem = item;
                }
            }

            if (failedItems != null)
            {
                for (int i = 0; i < failedItems.Count; i++)
                    _pickBatchItems.Remove(failedItems[i]);
            }

            // 마지막 다이 오프셋의 미촬영 다이 전파 — prepare Apply 루프의 마지막 반복과 동일 의미:
            // 픽업이 이번에 기록을 적용한 항목이 있고, 그 항목이 (SKIP 제거 후) 배치 마지막일 때 1회.
            if (lastAppliedItem != null &&
                _pickBatchItems.Count > 0 &&
                ReferenceEquals(_pickBatchItems[_pickBatchItems.Count - 1], lastAppliedItem))
            {
                int propagateResult = ApplyLastVisionOffsetToPendingDiesForBatch(lastAppliedItem);
                if (propagateResult != 0)
                    return propagateResult;
            }

            return 0;
        }

        // [사용자 지시 2026-07-28] RESULT 미회수 항목의 "잠정" 목표 — 비전 보정 없는 다이 좌표로
        //   계산한 뒤 PickerX만 안전 방향으로 ProvisionalPickerXMarginMm 만큼 당긴다(예: 200 → 199).
        //   용도는 오직 비전 회피 클리어런스 산출이며, 실제 픽 좌표는 각 픽 직전에 RESULT를 회수해
        //   CalculateCurrentPickTarget으로 재확정한다(이 잠정값으로는 픽 이동을 하지 않는다).
        //   방향 근거: 페어 간격 = homeClearance − (비전 − 피커) 이므로 피커 X가 작을수록 제약이
        //   커진다 — 1mm 감산이 곧 보수적(더 깊은 회피) 방향이다. 실측 비전 보정량은 X 0.001mm,
        //   Y 0.04mm 수준으로 1mm를 넘을 수 없다(사용자 확인).
        private const double ProvisionalPickerXMarginMm = 1.0;

        private int CalculateProvisionalPickTargetWithoutVisionOffset()
        {
            VisionAlignResult savedOffset = _visionOffset;
            try
            {
                _visionOffset = new VisionAlignResult();
                int result = CalculateCurrentPickTarget();
                if (result != 0)
                    return result;

                double provisionalPickerX = _targetPickerX - ProvisionalPickerXMarginMm;
                WriteLog("PickerPickUpSequence",
                    Name + " Input die vision RESULT 미회수 항목을 잠정 목표로 계산했습니다(회피 클리어런스 전용). " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", nominalPickerX=" + _targetPickerX.ToString("F3") +
                    ", provisionalPickerX=" + provisionalPickerX.ToString("F3") +
                    ", marginMm=" + ProvisionalPickerXMarginMm.ToString("F3") + " - Check");
                _targetPickerX = provisionalPickerX;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PROVISIONAL-TARGET-EX", Name,
                    "RESULT 미회수 항목의 잠정 목표 계산 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", error=" + ex.Message);
            }
            finally
            {
                // 잠정 계산이었음을 남긴다 — 이후 각 픽 직전 회수·재확정의 트리거가 된다.
                _visionOffset = savedOffset;
            }
        }

        // [사용자 승인 2026-07-28, A안] 픽 직전 RESULT 확정 — 현재 배치 항목의 RESULT가 아직
        //   없으면 여기서 회수하고 정확 좌표로 재계산한다. 회수 실패는 기존 SKIP 경로와 동일하게
        //   처리하고 다음 항목으로 넘긴다(호출자가 커서 이동 없이 재선택하도록 true 반환).
        private async Task<int> EnsureCurrentPickTargetVisionResultAsync(CancellationToken ct)
        {
            PickUpBatchItem item = _currentBatchItem;
            if (item == null || item.VisionOffset != null)
                return 0;

            if (item.VisionRequest == null)
            {
                return Fail("PICKER-PICKUP-VISION-COLLECT-HANDLE", "Vision",
                    "픽 직전 RESULT 회수 대상 항목에 Vision 핸들이 없습니다. die=" + item.DieId +
                    ", pickerNo=" + item.PickerNo);
            }

            DateTime collectStart = DateTime.UtcNow;
            WriteLog("PickerPickUpSequence",
                Name + " Input die vision RESULT 회수 시작(A안 — 픽 직전 확정). " +
                "die=" + item.DieId +
                ", pickerNo=" + item.PickerNo +
                ", requestIndex=" + item.VisionRequestIndex + " - Start");

            VisionAlignResult offset = await InputDieVisionPrepareSequence.CollectInputDieVisionResultCoreAsync(
                item.VisionRequest,
                ct).ConfigureAwait(false);

            int waitedMs = (int)(DateTime.UtcNow - collectStart).TotalMilliseconds;

            // 과대 보정 가드(Y 전용, 사용자 확정 2026-07-29) — 선회수 경로와 동일 판정.
            string offsetRejectReason = null;
            if (offset != null)
            {
                double limitY;
                double dieSizeY;
                string rangeDetail;
                if (IsInputDieVisionOffsetYOutOfRange(offset, out limitY, out dieSizeY, out rangeDetail))
                {
                    offsetRejectReason = "OffsetYOutOfRange";
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision Y 보정값이 한계를 초과해 폐기하고 Die를 Wait로 남깁니다. " +
                        "die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo +
                        ", deltaY=" + offset.DeltaY.ToString("F6") +
                        ", limitY=" + limitY.ToString("F6") +
                        ", dieSizeY=" + dieSizeY.ToString("F6") +
                        ", ratio=0.50" +
                        ", deltaX=" + offset.DeltaX.ToString("F6") + "(판정제외)" +
                        ", detail=" + rangeDetail + " - Check");
                }
                else if (!string.IsNullOrEmpty(rangeDetail))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision 과대 보정 가드를 적용하지 않습니다. " +
                        "die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo +
                        ", detail=" + rangeDetail + " - Check");
                }
            }

            if (offset == null || offsetRejectReason != null)
            {
                int waitResult = ReturnBatchItemToWaitForVisionResultFailure(
                    item,
                    offsetRejectReason ?? "ResultNotReceived");
                if (waitResult != 0)
                    return waitResult;

                _pickBatchItems.Remove(item);
                SetCurrentBatchItem(null);
                WriteLog("PickerPickUpSequence",
                    Name + " 픽 직전 RESULT 확정 실패로 해당 Die를 배치에서 제외하고 Wait로 남겼습니다. " +
                    "die=" + item.DieId +
                    ", pickerNo=" + item.PickerNo +
                    ", reason=" + (offsetRejectReason ?? "ResultNotReceived") +
                    ", waitedMs=" + waitedMs + " - Check");
                return 0;
            }

            item.VisionOffset = offset;
            if (!item.VisionOffsetApplied)
            {
                int applyResult = ApplyInputPickVisionRecordForBatchItem(item);
                if (applyResult != 0)
                    return applyResult;

                item.VisionOffsetApplied = true;

                // 배치 마지막 항목의 기록을 적용한 시점에 미촬영 다이 좌표 전파를 1회 수행한다
                // (전량 선회수 경로의 루프 후 처리와 동일 의미 — 이연 경로에서도 보존).
                if (_pickBatchItems.Count > 0 &&
                    ReferenceEquals(_pickBatchItems[_pickBatchItems.Count - 1], item))
                {
                    int propagateResult = ApplyLastVisionOffsetToPendingDiesForBatch(item);
                    if (propagateResult != 0)
                        return propagateResult;
                }
            }

            // 잠정 목표(보정 없음 + 마진)를 실제 RESULT 기반 정확 목표로 재확정한다.
            SetCurrentBatchItem(item);
            int exactResult = CalculateCurrentPickTarget();
            if (exactResult != 0)
                return exactResult;

            SaveCurrentStateToBatchItem();
            WriteLog("PickerPickUpSequence",
                Name + " Input die vision RESULT 회수 완료 및 정확 목표 재확정(A안). die=" + item.DieId +
                ", pickerNo=" + item.PickerNo +
                ", dx=" + offset.DeltaX +
                ", dy=" + offset.DeltaY +
                ", dt=" + offset.DeltaTheta +
                ", pickerX=" + _targetPickerX.ToString("F3") +
                ", waitedMs=" + waitedMs + " - Ok");
            return 0;
        }

        /// <summary>
        /// RESULT 회수 실패/과대 보정 Die를 Wait로 되돌린다 — 다음 라운드에 다시 촬영·픽업된다.
        /// [사용자 확정 2026-07-29] 기존에는 SKIP(IsInputTarget=false)으로 영구 제외했다.
        ///   재시도 상한(InputDieVisionWaitRetryLimit, 기본 3회) 초과 시에만 기존 SKIP으로 전환한다.
        /// </summary>
        private int ReturnBatchItemToWaitForVisionResultFailure(PickUpBatchItem item, string reason)
        {
            try
            {
                string dieId = item != null && item.DieId != null ? item.DieId : string.Empty;
                int pickerNo = item != null ? item.PickerNo : 0;

                int waitCount;
                int waitLimit;
                string detail;
                InputDieVisionWaitResult decision = ResolveInputDieVisionWait(
                    dieId,
                    pickerNo,
                    reason,
                    out waitCount,
                    out waitLimit,
                    out detail);

                if (decision == InputDieVisionWaitResult.Failed)
                {
                    return Fail("PICKER-PICKUP-VISION-WAIT-FAIL", "Material",
                        "Input die vision RESULT 실패 Die의 Wait 복귀에 실패했습니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", reason=" + reason +
                        ", detail=" + detail);
                }

                if (decision == InputDieVisionWaitResult.SkipByLimit)
                {
                    // 상한 초과 — 기존 SKIP(영구 제외) 경로를 그대로 유지한다.
                    MaterialStateService.ReleaseInputStagePickReservation(dieId, PickerLocationKind, pickerNo);
                    MaterialStateService.RemoveInspection(dieId, "InputPickVision");

                    string message;
                    bool syncOk = MaterialStateService.ApplyManualDieState(
                        dieId,
                        false,
                        DieResult.Unknown,
                        0,
                        "",
                        "PickUpVisionResultNgSkipByWaitLimit",
                        ManualDieStateSyncScope.InputMapOnly,
                        out message);
                    if (!syncOk)
                    {
                        return Fail("PICKER-PICKUP-VISION-SKIP-FAIL", "Material",
                            "Input die vision RESULT 실패 Die SKIP 처리에 실패했습니다. die=" + dieId +
                            ", pickerNo=" + pickerNo +
                            ", message=" + message);
                    }

                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision 재시도 한계를 초과해 Die를 SKIP(영구 제외) 처리합니다. " +
                        "die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", waitCount=" + waitCount +
                        ", limit=" + waitLimit +
                        ", lastReason=" + reason +
                        ", detail=" + detail + " - Failed");
                    return 0;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " Input die vision RESULT 실패 Die를 Wait로 남기고 다음 항목으로 진행합니다. " +
                    "die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", reason=" + reason +
                    ", waitCount=" + waitCount + "/" + waitLimit + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-WAIT-EX", "Material",
                    "Input die vision RESULT 실패 Die Wait 복귀 중 예외가 발생했습니다. die=" +
                    (item != null ? item.DieId : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>InputPickVision 자재 기록 — prepare ApplyInputDieVisionOffset의 기록부와 동일 내용.</summary>
        private int ApplyInputPickVisionRecordForBatchItem(PickUpBatchItem item)
        {
            try
            {
                VisionOffset offset = new VisionOffset
                {
                    X = item.VisionOffset.DeltaX,
                    Y = item.VisionOffset.DeltaY,
                    R = item.VisionOffset.DeltaTheta,
                    IsValid = true
                };

                InputStageUnit stage = ResolveInputStage();
                MaterialStateService.UpsertInspection(item.DieId, new DieInspectionRecord
                {
                    InspectionType = "InputPickVision",
                    Result = MaterialInspectionResult.Ok,
                    Offset = offset,
                    Alignments = new List<InspectionAlignmentSnapshot>
                    {
                        BuildInputStageAlignmentSnapshot(stage, "Input", offset)
                    },
                    Measurements = new List<InspectionMeasurement>
                    {
                        BuildMeasurement("InputAlignOffsetX", item.VisionOffset.DeltaX, "mm", MaterialInspectionResult.Ok),
                        BuildMeasurement("InputAlignOffsetY", item.VisionOffset.DeltaY, "mm", MaterialInspectionResult.Ok),
                        BuildMeasurement("InputAlignOffsetT", item.VisionOffset.DeltaTheta, "deg", MaterialInspectionResult.Ok),
                        BuildBooleanMeasurement("InputVisionResult", true)
                    }
                });

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-RECORD-EX", Name,
                    "Input die vision offset 자재 기록 중 예외가 발생했습니다. die=" +
                    (item != null ? item.DieId : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>마지막 회수 오프셋의 미촬영 다이 좌표 전파 — prepare Apply의 마지막 다이 처리(한계 검사 포함)와 동일.</summary>
        private int ApplyLastVisionOffsetToPendingDiesForBatch(PickUpBatchItem lastItem)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();

                // Wafer 채널 라이브 Delta는 카메라 순수 오프셋(raw)이므로 InputToBottomOffset 감산 없이 그대로 전파한다.
                double pendingMapOffsetX = lastItem.VisionOffset.DeltaX;
                double pendingMapOffsetY = -lastItem.VisionOffset.DeltaY;
                string limitReason;
                if (stage != null &&
                    !stage.IsManualDieDetectOffsetWithinLimit(pendingMapOffsetX, pendingMapOffsetY, out limitReason))
                {
                    return Fail("PICKER-PICKUP-PENDING-OFFSET-LIMIT", "Material",
                        "마지막 Input Vision 보정값이 허용 범위를 벗어나 미촬영 Die 좌표에 적용할 수 없습니다. " +
                        "referenceDie=" + lastItem.DieId +
                        ", offsetX=" + pendingMapOffsetX.ToString("F6") +
                        ", offsetY=" + pendingMapOffsetY.ToString("F6") +
                        ", reason=" + limitReason);
                }

                int updatedCount;
                int skippedCount;
                string updateDetail;
                if (!MaterialStateService.TryApplyLastVisionOffsetToPendingInputDies(
                    lastItem.DieId,
                    pendingMapOffsetX,
                    pendingMapOffsetY,
                    "InputLastPreparedVisionOffset:" + lastItem.DieId,
                    out updatedCount,
                    out skippedCount,
                    out updateDetail))
                {
                    return Fail("PICKER-PICKUP-PENDING-OFFSET-APPLY", "Material", updateDetail);
                }

                WriteLog("PickerPickUpSequence",
                    Name + " 조기 허가 배치 마지막 촬영 결과를 아직 촬영하지 않은 Die 좌표에 적용했습니다. " +
                    "referenceDie=" + lastItem.DieId +
                    ", appliedOffsetX=" + pendingMapOffsetX.ToString("F6") +
                    ", appliedOffsetY=" + pendingMapOffsetY.ToString("F6") +
                    ", updated=" + updatedCount +
                    ", skipped=" + skippedCount + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PENDING-OFFSET-EX", "Material",
                    "미촬영 Die 좌표 전파 중 예외가 발생했습니다. referenceDie=" +
                    (lastItem != null ? lastItem.DieId : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CalculateCurrentPickTarget()
        {
            try
            {
                if (_pickTarget == null)
                    return Fail("PICKER-PICKUP-DIE-TARGET", "Material",
                        "Input die pick target is missing before target calculation. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                if (_visionOffset == null)
                    return Fail("PICKER-PICKUP-VISION-OFFSET", "Vision",
                        "Input die vision offset is missing before target calculation. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                // Input Vision 검사에서 받은 X/Y/T 보정값을 Pick 이동식에 직접 적용한다.
                double alignOffsetX = _visionOffset.DeltaX;
                double alignOffsetY = _visionOffset.DeltaY;
                double alignOffsetT = _visionOffset.DeltaTheta;
                PickerPickUpMotionConfig pickUpConfig = ResolvePickUpMotionConfig();
                double pickMechanicalOffsetX = pickUpConfig.GetMechanicalOffsetX(_currentPickerIndex);
                double pickMechanicalOffsetY = pickUpConfig.GetMechanicalOffsetY(_currentPickerIndex);
                // Pick 런타임 보정: Enable일 때만 필터 상태를 적용하고, Disable이면 0을 전달한다
                // (Disable이어도 필터 학습·저장은 Bottom 검사 경로에서 계속된다).
                bool pickRuntimeEnabled = PickRuntimeOffsetService.IsEnabled;
                double pickRuntimeOffsetX = 0.0;
                double pickRuntimeOffsetY = 0.0;
                double pickRuntimeOffsetT = 0.0;
                if (pickRuntimeEnabled)
                {
                    PickRuntimeOffsetService.GetOffset(
                        Side,
                        _currentPickerNo,
                        out pickRuntimeOffsetX,
                        out pickRuntimeOffsetY,
                        out pickRuntimeOffsetT);
                }

                PickCoordinateResult coordinate;
                string coordinateReason;
                if (!PickerMotionTargetResolver.TryCalculateInputPickTarget(
                    Context != null ? Context.Machine : null,
                    Side,
                    _currentPickerIndex,
                    Name,
                    _currentDieId,
                    _pickTarget.TargetX,
                    _pickTarget.TargetY,
                    alignOffsetX,
                    alignOffsetY,
                    alignOffsetT,
                    true,
                    out coordinate,
                    out coordinateReason,
                    pickRuntimeOffsetX,
                    pickRuntimeOffsetY,
                    pickRuntimeOffsetT))
                {
                    return Fail("PICKER-PICKUP-COORD-OFFSET", Name,
                        "Input pick coordinate target resolve failed. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", reason=" + coordinateReason);
                }

                coordinate = DieCoordinateTransformService.ApplyPickMechanicalOffsets(
                    coordinate,
                    pickMechanicalOffsetX,
                    pickMechanicalOffsetY);

                // 공정 Pick Z 유일한 대입점: 헤드 공통 + 콜렛별 Pick Overdrive를 여기서 1회만 가산한다
                // (파생 이동/검증/배치 저장·복원에 자동 전파). PickPosition 티칭 자체는 콜렛/다이 AF가 산식으로 갱신한다(승인 2026-07-29).
                double pickHeadOverdrive = ResolveHeadPickOverdrive();
                double pickColletOverdrive = ResolveColletPickOverdrive(_currentPickerIndex);
                double pickOverdrive = pickHeadOverdrive + pickColletOverdrive;

                // [PickerZ 런타임 폐루프 2026-08-16] Side FrontSide ch0 필터값을 이동 목표에서만 감산.
                // 배치 저장·복원(MotionResolvers)은 이 값을 그대로 실어 나르므로 이중 적용 없음.
                double pickerZRuntimeOffset = CapturePickerZRuntimeOffset(
                    _currentPickerNo, "PickZ", coordinate.PickerZ + pickOverdrive);

                _targetStageY = coordinate.StageY;
                _targetPickerX = coordinate.PickerX;
                _targetPickerY = coordinate.PickerY;
                _targetPickerT = coordinate.PickerT;
                _targetPickerZ = coordinate.PickerZ + pickOverdrive - pickerZRuntimeOffset;
                _targetNeedleX = coordinate.NeedleX;
                _targetNeedleZ = coordinate.NeedleZ;
                _targetEjectPinZ = coordinate.EjectPinZ;
                _targetFormula = coordinate.Formula +
                    (pickOverdrive != 0.0
                        ? " / pickerZFinal = pickerZTeaching(" + coordinate.PickerZ.ToString("F6") +
                          ") + headOverdrive(" + pickHeadOverdrive.ToString("F6") +
                          ") + colletOverdrive(" + pickColletOverdrive.ToString("F6") +
                          ") = " + _targetPickerZ.ToString("F6")
                        : string.Empty);

                double cameraOffsetX;
                double cameraOffsetY;
                InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(
                    Context != null ? Context.Machine : null,
                    out cameraOffsetX,
                    out cameraOffsetY);

                WriteLog("PickerPickUpSequence",
                    Name + " calculated pick target. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerY=" + _targetPickerY +
                    ", pickerT=" + _targetPickerT +
                    ", pickerZ=" + _targetPickerZ +
                    ", headOverdrive=" + pickHeadOverdrive.ToString("F6") +
                    ", colletOverdrive=" + pickColletOverdrive.ToString("F6") +
                    ", pickerZRuntimeOffset=" + pickerZRuntimeOffset.ToString("F6") +
                    ", needleX=" + _targetNeedleX +
                    ", needleZ=" + _targetNeedleZ +
                    ", ejectPinZ=" + _targetEjectPinZ +
                    ", inputVisionX=" + _pickTarget.TargetX +
                    ", inputStageY=" + _pickTarget.TargetY +
                    ", formula=" + coordinate.Formula +
                    ", cameraOffsetX=" + cameraOffsetX +
                    ", cameraOffsetY=" + cameraOffsetY +
                    ", cameraOffsetAppliedOnceInInputVisionToPickerXY=True(LiveVisionDeltaIsRaw)" +
                    ", alignOffsetX=" + alignOffsetX +
                    ", alignOffsetY=" + alignOffsetY +
                    ", visionTotalOffsetX=" + _visionOffset.DeltaX +
                    ", visionTotalOffsetY=" + _visionOffset.DeltaY +
                    ", visionOffsetXAppliedToPickerAndNeedle=True" +
                    ", visionOffsetYAppliedToStage=True(OppositeSign)" +
                    ", visionOffsetYAppliedToPicker=False(FixedPickY)" +
                    ", pickMechanicalOffsetX=" + pickMechanicalOffsetX.ToString("F3") +
                    ", pickMechanicalOffsetXAppliedToPickerAndNeedle=True" +
                    ", pickMechanicalOffsetY=" + pickMechanicalOffsetY.ToString("F3") +
                    ", pickMechanicalOffsetYAppliedToPicker=True" +
                    ", pickMechanicalOffsetYAppliedToStage=False" +
                    ", needleYToVisionYOffset=" + ResolveNeedleCalibrationOffsetY() +
                    ", alignOffsetT=" + alignOffsetT +
                    ", pickRuntimeEnabled=" + pickRuntimeEnabled +
                    ", pickRuntimeOffsetX=" + pickRuntimeOffsetX.ToString("F6") +
                    ", pickRuntimeOffsetY=" + pickRuntimeOffsetY.ToString("F6") +
                    ", pickRuntimeOffsetT=" + pickRuntimeOffsetT.ToString("F6") + " - Ok");

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-TARGET-EX", Name, "Pick target calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // [사용자 승인 2026-07-28, A안] 픽 직전 RESULT 확정을 위해 async로 전환한다 — 선택한
        //   항목의 RESULT가 아직 없으면 여기서 회수·정확 목표 재확정 후 진행한다(대부분 이미 도착).
        //   회수 실패로 항목이 배치에서 제외되면 커서 이동 없이 같은 커서로 재선택한다.
        private async Task<int> SelectNextPickTargetAsync(CancellationToken ct)
        {
            while (true)
            {
                int selectResult = SelectNextPickTarget();
                if (selectResult != 0)
                    return selectResult;

                if (CurrentStep != PickerPickUpStep.MoveOppositePickerToAvoidForPickerMove)
                    return 0;

                int ensureResult = await EnsureCurrentPickTargetVisionResultAsync(ct).ConfigureAwait(false);
                if (ensureResult != 0)
                    return ensureResult;

                if (_currentBatchItem != null)
                    return 0;

                // RESULT 회수 실패로 제외됨 — 같은 커서 위치에서 다음 항목을 다시 선택한다.
                CurrentStep = PickerPickUpStep.SelectNextPickTarget;
            }
        }

        private int SelectNextPickTarget()
        {
            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("SelectNextPickTarget");

            if (_pickCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            SetCurrentBatchItem(_pickBatchItems[_pickCursor]);

            WriteLog("PickerPickUpSequence",
                Name + " selected pick target. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", pickIndex=" + (_pickCursor + 1) +
                "/" + _pickBatchItems.Count + " - Ok");

            // CycleTime 계측 시작 (Auto 운전만) — 모터 세그먼트는 이동 헬퍼에서 자동 기록된다.
            if (Options != null && Options.RunMode == SequenceRunMode.Auto)
                QMC.CDT320.Diagnostics.HandlerTactLog.CycleStart(
                    "PICKUP",
                    TactRequestId(),
                    Side == PickerSequenceSide.Front ? "FRONT" : "REAR",
                    _currentPickerNo,
                    _currentBatchItem != null && _currentBatchItem.PickTarget != null
                        ? _currentBatchItem.PickTarget.OrderIndex : _pickCursor,
                    _currentDieId);

            CurrentStep = PickerPickUpStep.MoveOppositePickerToAvoidForPickerMove;
            return 0;
        }

        /// <summary>CycleTime 계측 키 — 사이클 동안 불변(피커+다이).</summary>
        private string TactRequestId()
        {
            return (Side == PickerSequenceSide.Front ? "F" : "R") + _currentPickerNo + "-" +
                (string.IsNullOrEmpty(_currentDieId) ? ("c" + _pickCursor) : _currentDieId);
        }

    }
}
