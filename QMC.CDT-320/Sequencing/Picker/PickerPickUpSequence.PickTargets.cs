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
                int collectResult = await CollectPendingBatchVisionResultsAsync(ct).ConfigureAwait(false);
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

                    int result = CalculateCurrentPickTarget();
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
        private async Task<int> CollectPendingBatchVisionResultsAsync(CancellationToken ct)
        {
            List<PickUpBatchItem> failedItems = null;
            PickUpBatchItem lastAppliedItem = null;

            for (int i = 0; i < _pickBatchItems.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                PickUpBatchItem item = _pickBatchItems[i];
                if (item == null)
                    continue;

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

                    if (offset == null)
                    {
                        int skipResult = SkipBatchItemForVisionResultFailure(item);
                        if (skipResult != 0)
                            return skipResult;

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

        /// <summary>RESULT 회수 실패 Die SKIP — prepare SkipCurrentVisionFailedDieAndContinue와 동일 자재 처리.</summary>
        private int SkipBatchItemForVisionResultFailure(PickUpBatchItem item)
        {
            try
            {
                string dieId = item != null && item.DieId != null ? item.DieId : string.Empty;
                int pickerNo = item != null ? item.PickerNo : 0;

                MaterialStateService.ReleaseInputStagePickReservation(dieId, PickerLocationKind, pickerNo);
                MaterialStateService.RemoveInspection(dieId, "InputPickVision");

                string message;
                bool syncOk = MaterialStateService.ApplyManualDieState(
                    dieId,
                    false,
                    DieResult.Unknown,
                    0,
                    "",
                    "PickUpVisionResultNgSkip",
                    out message);
                if (!syncOk)
                {
                    return Fail("PICKER-PICKUP-VISION-SKIP-FAIL", "Material",
                        "Input die vision RESULT 실패 Die SKIP 처리에 실패했습니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", message=" + message);
                }

                WriteLog("PickerPickUpSequence",
                    Name + " Input die vision RESULT 실패 Die를 SKIP 처리하고 다음 항목으로 진행합니다. " +
                    "die=" + dieId +
                    ", pickerNo=" + pickerNo + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-SKIP-EX", "Material",
                    "Input die vision RESULT 실패 Die SKIP 처리 중 예외가 발생했습니다. die=" +
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

                // 공정 Pick Z 유일한 대입점: Collet AF Z Offset을 여기서 1회만 가산한다(파생 이동/검증/배치 저장·복원에 자동 전파).
                // 한계 초과는 fail-closed(알람 중단) — 확정 정책.
                string pickAfZOffsetFailReason;
                double pickColletAfZOffset = ResolveColletAfZOffset(_currentPickerIndex, out pickAfZOffsetFailReason);
                if (pickAfZOffsetFailReason != null)
                    return Fail("PICKER-PICKUP-AF-ZOFFSET-LIMIT", Name, pickAfZOffsetFailReason);

                _targetStageY = coordinate.StageY;
                _targetPickerX = coordinate.PickerX;
                _targetPickerY = coordinate.PickerY;
                _targetPickerT = coordinate.PickerT;
                _targetPickerZ = coordinate.PickerZ + pickColletAfZOffset;
                _targetNeedleX = coordinate.NeedleX;
                _targetNeedleZ = coordinate.NeedleZ;
                _targetEjectPinZ = coordinate.EjectPinZ;
                _targetFormula = coordinate.Formula +
                    (pickColletAfZOffset != 0.0
                        ? " / pickerZFinal = pickerZTeaching(" + coordinate.PickerZ.ToString("F6") +
                          ") + colletAfZOffset(" + pickColletAfZOffset.ToString("F6") +
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
                    ", colletAfZOffset=" + pickColletAfZOffset.ToString("F6") +
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
