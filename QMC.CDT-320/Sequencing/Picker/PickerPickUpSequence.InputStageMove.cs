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
        private async Task<int> MoveInputStageVisionPointForPickerAsync(
            InputStageUnit stage,
            double targetX,
            double targetY,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                double currentX = stage.CameraX != null ? stage.CameraX.ActualPosition : targetX;
                double currentY = stage.StageY != null ? stage.StageY.ActualPosition : targetY;
                double currentNeedleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : ResolveNeedleXForVisionX(currentX);
                double targetNeedleX = ResolveNeedleXForVisionX(targetX);

                WriteLog("PickerPickUpStagePath",
                    Name + " InputStage vision point path evaluate. description=" + description +
                    ", currentVisionX=" + currentX.ToString("F6") +
                    ", currentStageY=" + currentY.ToString("F6") +
                    ", currentNeedleX=" + currentNeedleX.ToString("F6") +
                    ", targetVisionX=" + targetX.ToString("F6") +
                    ", targetStageY=" + targetY.ToString("F6") +
                    ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                    " - Check");

                string needleAreaReason;
                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out needleAreaReason))
                {
                    return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
                        "니들 목표 위치가 작업 가능 영역을 벗어났습니다. " +
                        "description=" + description +
                        ", visionX=" + targetX.ToString("F6") +
                        ", needleX=" + targetNeedleX.ToString("F6") +
                        ", stageY=" + targetY.ToString("F6") +
                        ", reason=" + needleAreaReason);
                }

                bool visionXInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, targetX);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, targetY);
                if (visionXInPosition && stageYInPosition)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX/StageY already in position, move/check NeedleX only. description=" + description + " - Check");

                    int needleResult = await MoveNeedleXAndVerifyAsync(
                        stage,
                        targetNeedleX,
                        description + " NeedleX",
                        ct).ConfigureAwait(false);
                    if (needleResult != 0)
                        return needleResult;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                if (!visionXInPosition && stageYInPosition)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY in position, move VisionX+NeedleX. description=" + description + " - Check");

                    int result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                if (visionXInPosition && !stageYInPosition)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX in position, move StageY then NeedleX. description=" + description + " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveNeedleXAndVerifyAsync(
                        stage,
                        targetNeedleX,
                        description + " NeedleX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string xFirstReason;
                // 현재 기준: 경로 판단은 CameraX가 아니라 NeedleX/StageY 실축 조합으로 확인한다.
                bool canMoveXFirst = stage.IsNeedleWorkPointInArea(targetNeedleX, currentY, out xFirstReason);
                if (canMoveXFirst)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX/NeedleX first then StageY. description=" + description +
                        ", reason=" + xFirstReason + " - Check");

                    int result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string yFirstReason;
                bool canMoveYFirst = stage.IsNeedleWorkPointInArea(currentNeedleX, targetY, out yFirstReason);
                if (canMoveYFirst)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY first then VisionX/NeedleX. description=" + description +
                        ", reason=" + yFirstReason + " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        currentX,
                        targetY,
                        description + " StageY",
                        ct,
                        currentNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string finalTargetReason;
                if (stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out finalTargetReason))
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY safe enter then VisionX/NeedleX. description=" + description +
                        ", xFirstBlocked=" + xFirstReason +
                        ", yFirstBlocked=" + yFirstReason +
                        ", finalTargetReason=" + finalTargetReason +
                        " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY 안전 진입",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description + " VisionX/NeedleX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                return Fail("PICKER-PICKUP-STAGE-PATH", stage.Name,
                    description + " 위치로 이동할 안전한 X/Y 순서를 찾지 못했습니다. " +
                    "currentX=" + currentX.ToString("F6") +
                    ", currentY=" + currentY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") +
                    ", xFirst=" + xFirstReason +
                    ", yFirst=" + yFirstReason);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " X/Y 이동 순서 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXStageYAndNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double visionTarget,
            double stageYTarget,
            double needleTarget,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, visionTarget))
                {
                    int visionResult = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        visionTarget,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (visionResult != 0)
                        return Fail("PICKER-PICKUP-STAGE-XY-VISION", stage != null ? stage.Name : "InputStageUnit",
                            description + " VisionX 이동 명령 실패. result=" + visionResult +
                            ", " + BuildInputStageAxisState(stage, WaferStageAxis.VisionX, visionTarget));

                    visionResult = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        visionTarget,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (visionResult != 0)
                        return visionResult;
                }

                int result = await MoveNeedleXAndStageYForPickAsync(
                    stage,
                    needleTarget,
                    stageYTarget,
                    visionTarget,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-XY-ORDER-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 순서 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXAndVerifyAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISIONX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXAndNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double visionTarget,
            double needleTarget,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> visionMove = MoveInputVisionXAndVerifyAsync(
                    stage,
                    visionTarget,
                    description + " VisionX",
                    ct);
                Task<int> needleMove = MoveNeedleXAndVerifyAsync(
                    stage,
                    needleTarget,
                    description + " NeedleX",
                    ct);

                int[] results = await Task.WhenAll(visionMove, needleMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-VISION-NEEDLE-X", stage != null ? stage.Name : "InputStageUnit",
                        "InputVisionX와 NeedleX 동시 이동 실패. " +
                        "visionResult=" + results[0] +
                        ", needleResult=" + results[1] +
                        ", visionTarget=" + visionTarget.ToString("F6") +
                        ", needleTarget=" + needleTarget.ToString("F6") +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.VisionX, visionTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, needleTarget));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-NEEDLE-X-EX", stage != null ? stage.Name : "InputStageUnit",
                    "InputVisionX와 NeedleX 동시 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXAndStageYForPickAsync(
            InputStageUnit stage,
            double needleTarget,
            double stageYTarget,
            double workAreaVisionX,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-NEEDLE-STAGE-NO-UNIT", "InputStageUnit",
                        description + " 이동 중 InputStageUnit이 없습니다.");

                bool needleInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, needleTarget);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, stageYTarget);
                if (needleInPosition && stageYInPosition)
                    return 0;

                int safetyResult = await EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync(
                    stage,
                    description + " 이동 전 EjectPinZ 대기(Avoid)/Vacuum OFF 확인",
                    ct).ConfigureAwait(false);
                if (safetyResult != 0)
                    return safetyResult;

                bool moveNeedleXFirst;
                string reason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(needleTarget, stageYTarget, out moveNeedleXFirst, out reason))
                {
                    return Fail("PICKER-PICKUP-NEEDLE-STAGE-PATH", stage.Name,
                        description + " 이동 가능한 NeedleX/StageY 순서를 찾지 못했습니다. " + reason);
                }

                WriteLog("PickerPickUpStagePath",
                    Name + " NeedleX/StageY pick path selected. description=" + description +
                    ", order=" + (moveNeedleXFirst ? "NeedleX->StageY" : "StageY->NeedleX") +
                    ", reason=" + reason +
                    ", needleInPosition=" + needleInPosition +
                    ", stageYInPosition=" + stageYInPosition +
                    ", targetNeedleX=" + needleTarget.ToString("F6") +
                    ", targetStageY=" + stageYTarget.ToString("F6") +
                    ", workAreaVisionX=" + workAreaVisionX.ToString("F6") +
                    " - Check");

                if (moveNeedleXFirst)
                {
                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXAndVerifyAsync(
                            stage,
                            needleTarget,
                            description + " NeedleX",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }

                    if (!stageYInPosition)
                    {
                        int result = await MoveInputStageYAndVerifyAsync(
                            stage,
                            workAreaVisionX,
                            stageYTarget,
                            description + " StageY",
                            ct,
                            needleTarget).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
                }
                else
                {
                    if (!stageYInPosition)
                    {
                        int result = await MoveInputStageYAndVerifyAsync(
                            stage,
                            workAreaVisionX,
                            stageYTarget,
                            description + " StageY",
                            ct,
                            needleTarget).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }

                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXAndVerifyAsync(
                            stage,
                            needleTarget,
                            description + " NeedleX",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " NeedleX/StageY 이동 순서 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, target))
                    return CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, target, description);

                int result = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    target,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    target,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLEX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    "NeedleX 이동 중 예외가 발생했습니다. description=" + description +
                    ", target=" + target.ToString("F6") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageYAndVerifyAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double target,
            string description,
            CancellationToken ct,
            double? workAreaNeedleX = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, target))
                {
                    int safeResult = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                        stage,
                        description + " StageY 이동 전",
                        ct).ConfigureAwait(false);
                    if (safeResult != 0)
                        return safeResult;

                    int result = await MoveInputStageYForPickerWorkPointCommandAsync(
                        stage,
                        workAreaVisionX,
                        target,
                        description,
                        ct,
                        workAreaNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.WaferY,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGEY-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputStageVisionPointFinalPosition(
            InputStageUnit stage,
            double targetX,
            double targetY,
            string description)
        {
            int result = CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, targetX, description + " VisionX");
            if (result != 0)
                return result;

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, targetY, description + " StageY");
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.NeedleX,
                ResolveNeedleXForVisionX(targetX),
                description + " NeedleX");
        }

        private bool IsInputStageAxisAlreadyInPosition(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
                double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.05;
                if(item.IsMoving)
                {
                    return false;
                }
                return item.IsAtTargetPosition(target, tolerance);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageYForPickerWorkPointCommandAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double target,
            string description,
            CancellationToken ct,
            double? workAreaNeedleX = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(
                    PickerInputStageMoveHelper.MoveStageYForPickerWorkPointCommandAsync(
                        stage,
                        workAreaVisionX,
                        target,
                        Options != null && Options.FineMove,
                        BuildPickUpInputStageMoveTargetPrefix(),
                        workAreaNeedleX),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " move command failed. result=" + result +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisCommandAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("PICKUP", TactRequestId(), axis.ToString());

                int result;
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                string guardTargetName = "PickerPickUp;Side=" + Side + ";" + axis + ";" + description;
                if (axis == WaferStageAxis.VisionX)
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                            ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                            ct).ConfigureAwait(false);
                    }
                }

                if (result != 0)
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " move command failed. result=" + result +
                        ", " + BuildInputStageAxisState(stage, axis, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("PICKUP", TactRequestId(), axis.ToString());
                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitInputStageAxisInPositionResultAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitCode = await stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);

                if (waitCode != 0)
                    return Fail("PICKER-PICKUP-STAGE", stage.Name,
                        description + " move/in-position wait failed. waitCode=" + waitCode +
                        ". " + BuildInputStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                WriteLog("PickerPickUpStageMove",
                    Name + " InputStage axis wait complete. description=" + description +
                    ", " + BuildInputStageAxisState(stage, axis, target) +
                    " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStageUnit", description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item == null)
                    return Fail("PICKER-PICKUP-STAGE-AXIS", stage != null ? stage.Name : "InputStageUnit", description + " axis is not available. " + BuildInputStageAxisState(stage, axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("PICKER-PICKUP-STAGE-POSITION", stage.Name, description + " final position check failed. " + BuildInputStageAxisState(stage, axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-POSITION-EX", stage != null ? stage.Name : "InputStageUnit", description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerAxisCommandResultAsync(PickerAxis axis, double target, string description, CancellationToken ct, string targetName = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MovePickerAxisCommandAsync(axis, target, targetName).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-PICKUP-MOVE-CMD", Name, description + " move command failed. result=" + result + ", " + BuildPickerAxisState(axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-CMD-EX", Name, description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerAxisInPositionResultAsync(PickerAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                int waitCode = await WaitPickerAxisMoveDoneAsync(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("PICKER-PICKUP-MOVE", Name,
                        description + " move/in-position wait failed. waitCode=" + waitCode +
                        ". " + BuildPickerAxisState(axis, target));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-WAIT-EX", Name, description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckPickerAxisInPosition(PickerAxis axis, double target, string description)
        {
            if (!IsPickerAxisInPosition(axis, target))
                return Fail("PICKER-PICKUP-MOVE-CHECK", Name, description + " final position check failed. " + BuildPickerAxisState(axis, target));

            return 0;
        }

        private static QMC.Common.Motion.BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                case WaferStageAxis.WaferT:
                    return stage.StageT;
                case WaferStageAxis.WaferExpandingZ:
                    return stage.ExpanderZ;
                // 비전 X축 반환
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                case WaferStageAxis.NeedleX:
                    return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ:
                    return stage.NeedleZ;
                case WaferStageAxis.EjectPinZ:
                    return stage.EjectPinZ;
                default:
                    return null;
            }
        }

        private static string BuildInputStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance;
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private static async Task<TResult> AwaitStepWithCancellationAsync<TResult>(Task<TResult> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, default(TResult), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private void ReleaseInputReservationIfNeeded()
        {
            try
            {
                bool released = false;
                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    PickUpBatchItem item = _pickBatchItems[i];
                    if (item == null || item.DiePicked || string.IsNullOrWhiteSpace(item.DieId))
                        continue;

                    MaterialStateService.ReleaseInputStagePickReservation(
                        item.DieId,
                        PickerLocationKind,
                        item.PickerNo);

                    released = true;
                    WriteLog("PickerPickUpSequence",
                        Name + " released input die batch reservation. die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo + " - Ok");
                }

                if (!released && !_diePicked && !string.IsNullOrWhiteSpace(_currentDieId))
                {
                    MaterialStateService.ReleaseInputStagePickReservation(
                        _currentDieId,
                        PickerLocationKind,
                        _currentPickerNo);

                    WriteLog("PickerPickUpSequence",
                        Name + " released input die reservation. die=" + _currentDieId + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "Input die reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;
                ClearCurrentPickContext();
            }
        }

        private async Task<int> MoveEjectPinZToAvoidKeepNeedleZAsync(
            InputStageUnit stage,
            double needleTeachingTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, "PickerZ Stage Safe 도달 후 EjectPinZ AVOID 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                // 현재 기준: 정상 PickUp 완료 후 NeedleZ는 Pick teaching 위치를 유지하고 EjectPinZ만 복귀한다.
                int ejectResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 EjectPinZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                ejectResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 EjectPinZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectTarget, "PickUp 후 EjectPinZ Avoid 이동");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " Needle Vacuum OFF 후 EjectPinZ Avoid 완료 확인. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectTarget) +
                    " - Ok");

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTeachingTarget, "PickUp 후 NeedleZ teaching 유지");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " PickUp 후 NeedleZ teaching 유지. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget) +
                    " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-EJECT-PIN-AVOID-KEEP-NEEDLE-EX", Name,
                    "PickUp 후 EjectPinZ Avoid 및 NeedleZ teaching 유지 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool ShouldDeferCycleStopForPickUpDrain()
        {
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return false;
                if (Context == null || !Context.IsCycleStopRequested)
                    return false;
                if (IsAlarmStopActive())
                    return false;

                return CurrentStep != PickerPickUpStep.CheckUnit &&
                       CurrentStep != PickerPickUpStep.CheckPickerSideEnabled &&
                       CurrentStep != PickerPickUpStep.BuildEnabledPickerList &&
                       CurrentStep != PickerPickUpStep.CheckInputStageReady &&
                       CurrentStep != PickerPickUpStep.Complete &&
                       CurrentStep != PickerPickUpStep.Error;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void ReleaseInputStageArea()
        {
            try
            {
                if (_inputStageLease == null)
                    return;

                _inputStageLease.Dispose();
                _inputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "InputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
