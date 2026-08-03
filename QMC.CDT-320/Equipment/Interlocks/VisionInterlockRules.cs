using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;

namespace QMC.CDT320.Interlocks
{
    public static class VisionInterlockRules
    {
        #region 규칙 진입

        // 인터락 항목: SideVisionY와 Reticle 실린더 이동 요청을 Vision 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontSideVisionY", "FrontSideVisionY0"))
                return VerifyFrontSideVisionY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearSideVisionY", "RearSideVisionY0"))
                return VerifyRearSideVisionY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleLift", "Reticle Up/Down"))
                return VerifyReticleLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleSideSlideFront", "Reticle Front FW/BW"))
                return VerifyReticleFrontSlide(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleSideSlideRear", "Reticle Back FW/BW"))
                return VerifyReticleRearSlide(request, out reason);

            return true;
        }

        #endregion

        #region Front Side Vision Y

        // 인터락 항목: FrontSideVisionY 이동 종류별로 홈/수동/자동 조건을 선택한다.
        private static bool VerifyFrontSideVisionY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoFrontSideVisionY(request.Machine, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualFrontSideVisionY(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontSideVisionY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: FrontSideVisionY 홈은 Vision 장치 Busy 여부를 확인한다.
        private static bool CanHomeFrontSideVisionY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: FrontSideVisionY 홈은 현재 별도 차단 조건 없이 허용한다.
            return true;
        }

        // 인터락 항목: 수동 FrontSideVisionY 이동은 Vision 장치 Busy 여부를 확인한다.
        private static bool CanManualFrontSideVisionY(CDT320_Machine machine, out string reason)
        {
            // 인터락 조건: 수동 이동 전 FrontSideVisionY 홈 조건을 먼저 확인한다.
            if (!CanHomeFrontSideVisionY(machine, out reason))
                return false;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "FrontSideVisionY", out reason);
        }

        // 인터락 항목: 자동 FrontSideVisionY 이동은 InputStage 간섭과 Vision 장치 Busy 여부를 확인한다.
        private static bool CanAutoFrontSideVisionY(CDT320_Machine machine, out string reason)
        {
            // 인터락 조건: 자동 이동 전 FrontSideVisionY 홈 조건을 먼저 확인한다.
            if (!CanHomeFrontSideVisionY(machine, out reason))
                return false;

            // SideVisionY는 InputStage/InputVisionX와 기구 간섭이 없는 독립 검사축이다.
            // Auto 병렬 운전에서는 Input die vision 준비와 Side 검사 카메라 위치 이동이 겹칠 수 있으므로
            // InputStage/InputVisionX 이동 상태로 SideVisionY를 차단하지 않는다.
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "FrontSideVisionY", out reason);
        }

        #endregion

        #region Rear Side Vision Y

        // 인터락 항목: RearSideVisionY 이동 종류별로 홈/수동/자동 조건을 선택한다.
        private static bool VerifyRearSideVisionY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoRearSideVisionY(request.Machine, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualRearSideVisionY(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearSideVisionY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: RearSideVisionY 홈은 Vision 장치 Busy 여부를 확인한다.
        private static bool CanHomeRearSideVisionY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: RearSideVisionY 홈은 현재 별도 차단 조건 없이 허용한다.
            return true;
        }

        // 인터락 항목: 수동 RearSideVisionY 이동은 InputStage 간섭과 Vision 장치 Busy 여부를 확인한다.
        private static bool CanManualRearSideVisionY(CDT320_Machine machine, out string reason)
        {
            // 인터락 조건: 수동 이동 전 RearSideVisionY 홈 조건을 먼저 확인한다.
            if (!CanHomeRearSideVisionY(machine, out reason))
                return false;

            // SideVisionY는 InputStage/InputVisionX와 기구 간섭이 없는 독립 검사축이다.
            // Auto 병렬 운전에서는 Input die vision 준비와 Side 검사 카메라 위치 이동이 겹칠 수 있으므로
            // InputStage/InputVisionX 이동 상태로 SideVisionY를 차단하지 않는다.
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "RearSideVisionY", out reason);
        }

        // 인터락 항목: 자동 RearSideVisionY 이동은 InputStage 간섭과 Vision 장치 Busy 여부를 확인한다.
        private static bool CanAutoRearSideVisionY(CDT320_Machine machine, out string reason)
        {
            // 인터락 조건: 자동 이동 전 RearSideVisionY 홈 조건을 먼저 확인한다.
            if (!CanHomeRearSideVisionY(machine, out reason))
                return false;

            // SideVisionY는 InputStage/InputVisionX와 기구 간섭이 없는 독립 검사축이다.
            // Auto 병렬 운전에서는 Input die vision 준비와 Side 검사 카메라 위치 이동이 겹칠 수 있으므로
            // InputStage/InputVisionX 이동 상태로 SideVisionY를 차단하지 않는다.
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "RearSideVisionY", out reason);
        }

        #endregion

        #region Reticle Lift
        
        // 인터락 항목: ReticleLift 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyReticleLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyAllPickerZSafeForReticleMove(request.Machine, "ReticleLift", out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "Exception occurred while verifying ReticleLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: ReticleLift 초기화는 Vision 장치 Busy 여부를 확인한다.
        private static bool CanInitializeReticleLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            //if (!VerifyInputStageClear(machine, "ReticleLift", out reason))
            //{
            //    reason = "ReticleLift initialize " + direction + " blocked. " + reason;
            //    return false;
            //}

            // ReticleSideSlide Front/Rear가 모두 Backward 상태여야 ReticleLift 이동 가능.
            // (둘 중 하나라도 Backward가 아니면 차단/알람)
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (!vision.IsVisionReticleFrontSideBackward() || !vision.IsVisionReticleRearSideBackward())
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "ReticleLift move " + direction + " blocked. ReticleSideSlide Front/Rear가 모두 Backward 상태가 아닙니다.",
                    out reason);

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleLift", out reason);
        }

        // 인터락 항목: ReticleLift 이동은 Vision 장치 Busy 여부를 확인한다.
        private static bool CanMoveReticleLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            //if (!VerifyInputStageClear(machine, "ReticleLift", out reason))
            //{
            //    reason = "ReticleLift move " + direction + " blocked. " + reason;
            //    return false;
            //}

            // ReticleSideSlide Front/Rear가 모두 Backward 상태여야 ReticleLift 이동 가능.
            // (둘 중 하나라도 Backward가 아니면 차단/알람)
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (!vision.IsVisionReticleFrontSideBackward() || !vision.IsVisionReticleRearSideBackward())
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "ReticleLift move " + direction + " blocked. ReticleSideSlide Front/Rear가 모두 Backward 상태가 아닙니다.",
                    out reason);

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleLift", out reason);
        }

        #endregion

        #region Reticle Front Slide

        // 인터락 항목: ReticleFrontSlide 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyReticleFrontSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyAllPickerZSafeForReticleMove(request.Machine, "ReticleSideSlideFront", out reason))
                    return false;

                if (!VerifyReticleLiftUpBeforeSlideForward(
                    request.Machine, request.TargetValue, "ReticleSideSlideFront", out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleFrontSlide(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleFrontSlide(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleSideSlideFront",
                    "Exception occurred while verifying ReticleSideSlideFront cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: ReticleFrontSlide 초기화는 ReticleLift/Vision Busy 조건을 확인한다.
        private static bool CanInitializeReticleFrontSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideFront", out reason);
        }

        // 인터락 항목: ReticleFrontSlide 이동은 ReticleLift/Vision Busy 조건을 확인한다.
        private static bool CanMoveReticleFrontSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideFront", out reason);
        }

        #endregion

        #region Reticle Rear Slide

        // 인터락 항목: ReticleRearSlide 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyReticleRearSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyAllPickerZSafeForReticleMove(request.Machine, "ReticleSideSlideRear", out reason))
                    return false;

                if (!VerifyReticleLiftUpBeforeSlideForward(
                    request.Machine, request.TargetValue, "ReticleSideSlideRear", out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleRearSlide(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleRearSlide(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleSideSlideRear",
                    "Exception occurred while verifying ReticleSideSlideRear cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: ReticleRearSlide 초기화는 ReticleLift/Vision Busy 조건을 확인한다.
        private static bool CanInitializeReticleRearSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideRear", out reason);
        }

        // 인터락 항목: ReticleRearSlide 이동은 ReticleLift/Vision Busy 조건을 확인한다.
        private static bool CanMoveReticleRearSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideRear", out reason);
        }

        #endregion

        #region Stage·Picker·Busy 공통 확인

        // 절대 인터락: Reticle Slide 전진은 Lift가 완전한 UP 상태일 때만 허용한다.
        // 후진은 비정상 위치에서도 안전 복귀할 수 있도록 이 조건으로 차단하지 않는다.
        private static bool VerifyReticleLiftUpBeforeSlideForward(
            CDT320_Machine machine,
            double targetValue,
            string movingName,
            out string reason)
        {
            reason = string.Empty;

            if (targetValue < 0.5)
                return true;

            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            BaseCylinder lift = vision != null ? vision.ReticleLift : null;
            if (lift != null && lift.IsFwd)
                return true;

            bool upSensorOn = lift != null && lift.InFwd != null && lift.InFwd.IsOn;
            bool downSensorOn = lift != null && lift.InBwd != null && lift.InBwd.IsOn;
            return MotionGuardRuleHelpers.Block(
                movingName,
                "Reticle Slide 전진 차단: Reticle Lift가 완전한 UP 상태가 아닙니다. " +
                "liftUp=" + upSensorOn + ", liftDown=" + downSensorOn,
                out reason);
        }

        // 인터락 항목: SideVision 이동 전 InputStage 축 이동 중 여부를 확인한다.
        private static bool VerifyInputStageClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(stage.StageY))
                return MotionGuardRuleHelpers.Block(movingName, "InputStage StageY is moving.", out reason);
            if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
            //if (MotionGuardRuleHelpers.IsAxisMoving(stage.ExpanderZ))
            //    return MotionGuardRuleHelpers.Block(movingName, "InputStage ExpanderZ is moving.", out reason);

            return true;
        }

        // 절대 인터락: Reticle Lift/Slide 이동 전 Front/Rear PickerZ 8축이 정지 및 Avoid 또는 0 이상이어야 한다.
        private static bool VerifyAllPickerZSafeForReticleMove(
            CDT320_Machine machine,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            if (machine == null || machine.PickerFrontUnit == null || machine.PickerRearUnit == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: Front/Rear PickerUnit 정보가 없습니다.",
                    out reason);

            if (!PickerFrontInterlockRules.VerifyFrontPickerZAxesAvoidOrNonNegative(
                machine.PickerFrontUnit,
                movingName,
                out reason))
                return false;

            return PickerRearInterlockRules.VerifyRearPickerZAxesAvoidOrNonNegative(
                machine.PickerRearUnit,
                movingName,
                out reason);
        }

        // 인터락 항목: Vision 축/Reticle 이동 전 VisionUnit 내부 축 또는 실린더 Busy 여부를 확인한다.
        private static bool VerifyVisionNotBusy(VisionUnit vision, string movingName, out string reason)
        {
            reason = string.Empty;
            if (vision == null)
                return true;

            //서로 움직여도 상관없음.
            //if (IsMovingExcept(vision.FrontSideVisionY, movingName, "FrontSideVisionY", "FrontSideVisionY0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontSideVisionY is moving.", out reason);
            //if (IsMovingExcept(vision.RearSideVisionY, movingName, "RearSideVisionY", "RearSideVisionY0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearSideVisionY is moving.", out reason);


            // 간섭 무
            //if (IsCylinderMovingExcept(vision.ReticleLift, movingName, "ReticleLift", "Reticle Up/Down"))
            //    return MotionGuardRuleHelpers.Block(movingName, "ReticleLift is moving.", out reason);
            //if (IsCylinderMovingExcept(vision.ReticleFrontSideSlide, movingName, "ReticleSideSlideFront", "Reticle Front FW/BW"))
            //    return MotionGuardRuleHelpers.Block(movingName, "Reticle front slide is moving.", out reason);
            //if (IsCylinderMovingExcept(vision.ReticleRearSideSlide, movingName, "ReticleSideSlideRear", "Reticle Back FW/BW"))
            //    return MotionGuardRuleHelpers.Block(movingName, "Reticle rear slide is moving.", out reason);

            return true;
        }

        // 인터락 기준: 지정 축이 현재 이동 대상이 아닌데 이동 중인지 판단한다.
        private static bool IsMovingExcept(BaseAxis axis, string movingName, params string[] names)
        {
            if (!MotionGuardRuleHelpers.IsAxisMoving(axis))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        // 인터락 기준: 지정 실린더가 현재 이동 대상이 아닌데 이동 중인지 판단한다.
        private static bool IsCylinderMovingExcept(BaseCylinder cylinder, string movingName, params string[] names)
        {
            if (!MotionGuardRuleHelpers.IsCylinderMoving(cylinder))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static string ResolveCylinderDirection(double targetValue, string fwdText, string bwdText)
        {
            return targetValue >= 0.5 ? fwdText : bwdText;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "VisionInterlock", reason + " - Blocked");
            }
            catch
            {
            }
            finally
            {
            }
        }

        #endregion
    }
}
