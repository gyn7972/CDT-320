using System;
using System.Collections.Generic;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// InputCamera 선행검사(비침습) 시작 안전 판정 게이트.
    /// PickerProcessSequence의 경계 트리거와 InputVisionPrefetchRunner(주기 재시도 실행자)가
    /// 동일 판정을 공유하도록 PickerProcessSequence에서 기계적으로 추출한 정적 클래스.
    /// 판정 내용: 허가 잔존 여부 + Front/Rear 픽커 Input 존 점유/이동 위험 +
    /// InputVisionX가 가장 가까운 검사 대기 다이까지 이동할 때의 공용 레일/모션 가드 dry-run(FIX-D).
    /// </summary>
    internal static class InputCameraPreInspectionStartGate
    {
        /// <summary>선행검사를 지금 시작해도 안전한지 판정한다. 불가하면 blockReason에 사유를 담는다.</summary>
        public static bool CanStart(MachineSequenceContext context, out string blockReason)
        {
            blockReason = string.Empty;

            try
            {
                if (context == null || context.Machine == null)
                {
                    blockReason = "Machine context 없음";
                    return false;
                }

                string pendingPermissionDetail;
                if (InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail))
                {
                    blockReason = "PickUp 허가가 이미 발급되어 InputVisionX Avoid 유지 필요. pendingPermission=" +
                        pendingPermissionDetail;
                    return false;
                }

                string frontDetail;
                bool frontBlocking = IsPickerBlockingInputCameraPreInspection(context, true, out frontDetail);
                string rearDetail;
                bool rearBlocking = IsPickerBlockingInputCameraPreInspection(context, false, out rearDetail);

                string railContentionDetail;
                bool exactTargetEvaluated;
                bool railContended = IsSharedRailContendedForInputVisionStart(
                    context,
                    out railContentionDetail,
                    out exactTargetEvaluated);
                if (railContended)
                {
                    blockReason = "InputVisionX가 가장 가까운 검사 대기 다이까지 이동할 때 공용 레일 또는 모션 가드가 차단됩니다. " +
                        railContentionDetail;
                    return false;
                }

                if ((frontBlocking || rearBlocking) && !exactTargetEvaluated)
                {
                    blockReason = "InputCamera 선행검사 시작 전 Picker Input 영역이 안전하게 비어있고, 실제 검사 다이 좌표까지 안전한지 확인되어야 합니다. " +
                        "frontBlocking=" + frontBlocking +
                        ", frontDetail=" + frontDetail +
                        ", rearBlocking=" + rearBlocking +
                        ", rearDetail=" + rearDetail +
                        ", targetDetail=" + railContentionDetail;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                blockReason = "선행검사 시작 조건 확인 중 예외. error=" + ex.Message;
                return false;
            }
        }

        public static bool IsPickerBlockingInputCameraPreInspection(
            MachineSequenceContext context,
            bool isFront,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    context != null ? context.Machine : null,
                    isFront,
                    PickerWorkZone.Input,
                    null,
                    "PickerProcess InputCamera 선행검사 시작 판단");

                bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
                bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
                bool inputRelated =
                    state != null &&
                    (state.CurrentZone == PickerWorkZone.Input ||
                     state.TargetZone == PickerWorkZone.Input ||
                     state.BlocksTransport ||
                     state.UnknownUnsafe);
                bool movingInputRisk = IsPickerInputZoneMotionRiskForProcess(state, xMoving, yMoving);
                bool busy = inputRelated || movingInputRisk;

                detail = (isFront ? "FrontPicker" : "RearPicker") +
                    ", busy=" + busy +
                    ", movingX=" + xMoving +
                    ", movingY=" + yMoving +
                    ", movingInputRisk=" + movingInputRisk +
                    ", " + (state != null ? state.Describe() : "state=null");

                return busy;
            }
            catch (Exception ex)
            {
                detail = (isFront ? "FrontPicker" : "RearPicker") +
                    " InputCamera 선행검사 시작 조건 확인 실패. error=" + ex.Message;
                return true;
            }
        }

        // FIX-D: InputVisionX가 작업(die)까지 전진할 때 공용 레일에서 상대 PickerX 실좌표와 겹치는지 dry-run으로 확인한다.
        // 실제 이동 인터락과 동일한 SharedRailX 충돌 판정을 사용하므로 zone 논리가 놓치는 물리 경합(예: Bottom 중 x=445.999)을 잡는다.
        public static bool IsSharedRailContendedForInputVisionStart(
            MachineSequenceContext context,
            out string detail,
            out bool exactTargetEvaluated)
        {
            detail = string.Empty;
            exactTargetEvaluated = false;

            try
            {
                CDT320_Machine machine = context != null ? context.Machine : null;
                if (machine == null)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(machine);
                if (service == null)
                {
                    detail = "SharedRailX 서비스가 없어 실제 검사 다이 좌표 안전 확인을 수행할 수 없습니다.";
                    return false;
                }

                BaseAxis inputVisionX = machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
                if (inputVisionX == null || !service.IsSharedRailAxis(inputVisionX))
                {
                    detail = "InputVisionX가 없거나 SharedRailX 축으로 등록되지 않아 실제 검사 다이 좌표 안전 확인을 수행할 수 없습니다.";
                    return false;
                }

                List<InputStagePickTargetCandidate> candidates =
                    MaterialStateService.GetReadyInputStagePickTargetCandidates();
                if (candidates == null || candidates.Count == 0)
                {
                    detail = "검사 대기 다이가 없습니다.";
                    return false;
                }

                BaseAxis frontPickerX = machine.PickerFrontUnit != null
                    ? machine.PickerFrontUnit.PickerX
                    : null;
                BaseAxis rearPickerX = machine.PickerRearUnit != null
                    ? machine.PickerRearUnit.PickerX
                    : null;

                InputStagePickTargetCandidate nearestCandidate = null;
                string nearestPicker = string.Empty;
                double nearestPickerReference = 0.0;
                double nearestDistance = double.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    InputStagePickTargetCandidate candidate = candidates[i];
                    if (candidate == null)
                        continue;

                    UpdateNearestInputVisionCandidate(
                        candidate,
                        frontPickerX,
                        "FrontPickerX",
                        ref nearestCandidate,
                        ref nearestPicker,
                        ref nearestPickerReference,
                        ref nearestDistance);
                    UpdateNearestInputVisionCandidate(
                        candidate,
                        rearPickerX,
                        "RearPickerX",
                        ref nearestCandidate,
                        ref nearestPicker,
                        ref nearestPickerReference,
                        ref nearestDistance);
                }

                if (nearestCandidate == null)
                {
                    detail = "PickerX와 비교할 수 있는 검사 대기 다이를 찾지 못했습니다.";
                    return false;
                }

                double probeTarget = nearestCandidate.TargetX;
                exactTargetEvaluated = true;

                string reason;
                if (!service.VerifySingleAxisMove(inputVisionX, probeTarget, out reason))
                {
                    detail = BuildNearestInputVisionTargetDetail(
                        nearestCandidate,
                        nearestPicker,
                        nearestPickerReference,
                        nearestDistance,
                        probeTarget,
                        inputVisionX,
                        "SharedRailX: " + reason);
                    return true;
                }

                string motionGuardReason;
                if (!MotionGuardRuntime.CanAxisTeachingMove(
                    inputVisionX,
                    probeTarget,
                    "InputCameraPreInspectionNearestDie;Die=" + nearestCandidate.DieId,
                    out motionGuardReason))
                {
                    detail = BuildNearestInputVisionTargetDetail(
                        nearestCandidate,
                        nearestPicker,
                        nearestPickerReference,
                        nearestDistance,
                        probeTarget,
                        inputVisionX,
                        "MotionGuard: " + motionGuardReason);
                    return true;
                }

                detail = BuildNearestInputVisionTargetDetail(
                    nearestCandidate,
                    nearestPicker,
                    nearestPickerReference,
                    nearestDistance,
                    probeTarget,
                    inputVisionX,
                    "Clear");
                return false;
            }
            catch (Exception ex)
            {
                detail = "공용 레일 경합 확인 중 예외. error=" + ex.Message;
                return true;
            }
        }

        private static string BuildNearestInputVisionTargetDetail(
            InputStagePickTargetCandidate nearestCandidate,
            string nearestPicker,
            double nearestPickerReference,
            double nearestDistance,
            double probeTarget,
            BaseAxis inputVisionX,
            string result)
        {
            return "die=" + (nearestCandidate != null ? nearestCandidate.DieId : "-") +
                    ", grid=(" + nearestCandidate.DieMapX + "," + nearestCandidate.DieMapY + ")" +
                    ", nearestPicker=" + nearestPicker +
                    ", pickerReferenceX=" + nearestPickerReference.ToString("F3") +
                    ", distance=" + nearestDistance.ToString("F3") +
                    ", probeTarget=" + probeTarget.ToString("F3") +
                    ", inputVisionXActual=" + (inputVisionX != null ? inputVisionX.ActualPosition.ToString("F3") : "-") +
                    ", result=" + result;
        }

        private static bool IsPickerInputZoneMotionRiskForProcess(PickerZoneTransportState state, bool xMoving, bool yMoving)
        {
            if (!xMoving && !yMoving)
                return false;

            if (state == null)
                return true;

            return state.CurrentZone == PickerWorkZone.Input ||
                   state.TargetZone == PickerWorkZone.Input ||
                   state.CurrentZone == PickerWorkZone.Unknown ||
                   state.TargetZone == PickerWorkZone.Unknown ||
                   state.UnknownUnsafe;
        }

        private static void UpdateNearestInputVisionCandidate(
            InputStagePickTargetCandidate candidate,
            BaseAxis pickerX,
            string pickerName,
            ref InputStagePickTargetCandidate nearestCandidate,
            ref string nearestPicker,
            ref double nearestPickerReference,
            ref double nearestDistance)
        {
            if (candidate == null || pickerX == null)
                return;

            double pickerReference = pickerX.IsMoving
                ? pickerX.CommandPosition
                : pickerX.ActualPosition;
            double distance = Math.Abs(candidate.TargetX - pickerReference);
            if (distance >= nearestDistance)
                return;

            nearestCandidate = candidate;
            nearestPicker = pickerName;
            nearestPickerReference = pickerReference;
            nearestDistance = distance;
        }
    }
}
