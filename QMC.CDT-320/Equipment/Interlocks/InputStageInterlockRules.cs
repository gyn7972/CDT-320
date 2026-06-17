using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class InputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageY", "StageY", "WaferY"))
                return VerifyWaferStageY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageT", "StageT", "WaferT"))
                return VerifyWaferStageT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferExpandingZ", "ExpanderZ"))
                return VerifyWaferExpandingZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputVisionX", "CameraX"))
                return VerifyWaferVisionX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleX", "NeedleBlockX"))
                return VerifyNeedleX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleZ"))
                return VerifyNeedleZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "EjectPinZ"))
                return VerifyEjectPinZ(request, out reason);

            return true;
        }

        private static bool VerifyWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveWaferStageY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferStageY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "WaferStageY", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferY, "WaferStageY", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageY", out reason);
        }

        private static bool VerifyWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveWaferStageT(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferStageT(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "WaferStageT", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferT, "WaferStageT", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageT", out reason);
        }

        private static bool VerifyWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveWaferExpandingZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferExpandingZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeWaferExpandingZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            bool positiveMove = IsExpanderZPositiveMove(request, stage);

            if (!VerifyInputFeederClear(machine, "ExpanderZ", out reason))
                return false;

            if (!VerifyInputVisionXClearForExpanderZ(machine, out reason))
                return false;

            if (!VerifyFrontPickerClearForExpanderZ(machine != null ? machine.PickerFrontUnit : null, positiveMove, out reason))
                return false;

            if (!VerifyRearPickerClearForExpanderZ(machine != null ? machine.PickerRearUnit : null, positiveMove, out reason))
                return false;

            return VerifyInputStageNotBusy(stage, "ExpanderZ", out reason);
        }

        private static bool VerifyWaferVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveInputVisionX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeInputVisionX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveInputVisionX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "InputVisionX", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.VisionX, "InputVisionX", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "InputVisionX", out reason);
        }

        private static bool VerifyNeedleX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveNeedleX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeNeedleX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveNeedleX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "NeedleX", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleX, "NeedleX", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleX", out reason);
        }

        private static bool CanHomeInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                if (feeder != null && !feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeeder lift cylinder must be down.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputVisionX",
                    "Exception occurred while verifying InputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeWaferStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. NeedleZ must be at Avoid position.",
                        out reason);

                //여기 조건에 따라 다르다.
                //InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                //if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "InputStageY",
                //        "InputStageY HOME blocked. InputFeederY must be at Avoid position.",
                //        out reason);

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageY", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageY",
                    "Exception occurred while verifying InputStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeWaferStageT(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageT",
                        "InputStageT HOME blocked. NeedleZ must be at Avoid position.",
                        out reason);

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageT", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageT",
                    "Exception occurred while verifying InputStageT home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeNeedleX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "NeedleX",
                        "NeedleX HOME blocked. NeedleZ must be at Avoid position.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "NeedleX",
                    "Exception occurred while verifying NeedleX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveNeedleZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeNeedleZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeNeedleZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "NeedleZ", out reason))
                return false;

            if (!IsContinuousJogMove(request) &&
                !VerifyInputStageWorkArea(request, WaferStageAxis.NeedleZ, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleZ", out reason);
        }

        private static bool VerifyEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveEjectPinZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeEjectPinZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeEjectPinZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "EjectPinZ", out reason))
                return false;

            if (!IsContinuousJogMove(request) &&
                !VerifyInputStageWorkArea(request, WaferStageAxis.EjectPinZ, "EjectPinZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "EjectPinZ", out reason);
        }

        private static bool IsContinuousJogMove(MotionGuardRuleContext request)
        {
            if (request == null)
                return false;

            string targetName = MotionGuardRuleHelpers.NormalizeTargetName(request.TargetName);
            return string.Equals(targetName, "ContinuousJog", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool VerifyInputStageWorkArea(MotionGuardRuleContext request, WaferStageAxis axis, string movingName, out string reason)
        {
            reason = string.Empty;
            InputStageUnit stage = request != null && request.Machine != null ? request.Machine.InputStageUnit : null;
            if (stage == null)
                return true;

            string areaReason;
            double overrideWorkAreaX;
            if (axis == WaferStageAxis.WaferY &&
                TryResolveInputStageWorkAreaX(request, out overrideWorkAreaX))
            {
                double targetY = request != null ? request.TargetValue : 0.0;
                if (stage.IsInputStageWorkPointInArea(overrideWorkAreaX, targetY, out areaReason))
                {
                    if (stage.IsNeedleZInSafePosition())
                        return true;

                    double needleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition :
                        stage.Recipe != null && stage.Recipe.NeedleX != null ? stage.Recipe.NeedleX.ProcessPosition : 0.0;
                    if (stage.IsNeedleWorkPointInArea(needleX, targetY, out areaReason))
                        return true;
                }

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " blocked by InputStage work area. " + areaReason +
                    ", overrideWorkAreaX=" + overrideWorkAreaX.ToString("F3"),
                    out reason);
            }

            if (stage.IsInputStageAxisTargetAllowedInWorkArea(axis, request != null ? request.TargetValue : 0.0, out areaReason))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " blocked by InputStage work area. " + areaReason,
                out reason);
        }

        private static bool TryResolveInputStageWorkAreaX(MotionGuardRuleContext request, out double workAreaX)
        {
            workAreaX = 0.0;
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.TargetName))
                    return false;

                const string key = "InputStageWorkAreaX=";
                int index = request.TargetName.IndexOf(key, System.StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return false;

                int valueStart = index + key.Length;
                int valueEnd = request.TargetName.IndexOf(';', valueStart);
                string value = valueEnd >= valueStart
                    ? request.TargetName.Substring(valueStart, valueEnd - valueStart)
                    : request.TargetName.Substring(valueStart);

                return double.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out workAreaX);
            }
            catch
            {
                workAreaX = 0.0;
                return false;
            }
            finally
            {
            }
        }

        private static bool VerifyInputFeederClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(feeder.FeederY))
                return MotionGuardRuleHelpers.Block(movingName, "InputFeederY is moving.", out reason);

            return true;
        }

        private static bool VerifyInputStageNotBusy(InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            if (IsMovingExcept(stage.StageY, movingName, "WaferStageY", "StageY", "WaferY"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageY is moving.", out reason);
            if (IsMovingExcept(stage.StageT, movingName, "WaferStageT", "StageT", "WaferT"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageT is moving.", out reason);
            if (IsMovingExcept(stage.ExpanderZ, movingName, "WaferExpandingZ", "ExpanderZ"))
                return MotionGuardRuleHelpers.Block(movingName, "ExpanderZ is moving.", out reason);
            if (IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
            if (IsMovingExcept(stage.NeedleBlockX, movingName, "NeedleX", "NeedleBlockX"))
                return MotionGuardRuleHelpers.Block(movingName, "NeedleX is moving.", out reason);

            //서로 같이 움직여도 상관없음.
            //if (IsMovingExcept(stage.NeedleZ, movingName, "NeedleZ"))
            //    return MotionGuardRuleHelpers.Block(movingName, "NeedleZ is moving.", out reason);
            //if (IsMovingExcept(stage.EjectPinZ, movingName, "EjectPinZ"))
            //    return MotionGuardRuleHelpers.Block(movingName, "EjectPinZ is moving.", out reason);

            return true;
        }

        private static bool IsExpanderZPositiveMove(MotionGuardRuleContext request, InputStageUnit stage)
        {
            try
            {
                if (request == null || stage == null || stage.ExpanderZ == null)
                    return false;

                double tolerance = stage.ExpanderZ.Config != null && stage.ExpanderZ.Config.InPositionTolerance > 0.0
                    ? stage.ExpanderZ.Config.InPositionTolerance
                    : 0.05;

                return request.TargetValue > stage.ExpanderZ.ActualPosition + tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool VerifyInputVisionXClearForExpanderZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null)
                    return true;

                if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ move blocked. InputVisionX is moving.",
                        out reason);

                if (stage.IsVisionXInAvoidPosition())
                    return true;

                double actual = stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0;
                double avoid = stage.Recipe != null && stage.Recipe.VisionX != null ? stage.Recipe.VisionX.AvoidPosition : 0.0;
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. InputVisionX must be at Avoid position before StageZ moves. actual=" +
                    actual.ToString("F3") + ", avoid=" + avoid.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying InputVisionX clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyFrontPickerClearForExpanderZ(PickerFrontUnit picker, bool positiveMove, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (picker == null)
                    return true;

                return VerifyPickerClearForExpanderZ(
                    "FrontPicker",
                    picker.PickerX,
                    picker.PickerY,
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "PickPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "BottomPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "SidePosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "PlacePosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "OutputAvoidPosition"),
                    positiveMove,
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying FrontPicker clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyRearPickerClearForExpanderZ(PickerRearUnit picker, bool positiveMove, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (picker == null)
                    return true;

                return VerifyPickerClearForExpanderZ(
                    "RearPicker",
                    picker.PickerX,
                    picker.PickerY,
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "PickPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "BottomPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "SidePosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "PlacePosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "OutputAvoidPosition"),
                    positiveMove,
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying RearPicker clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyPickerClearForExpanderZ(
            string pickerName,
            BaseAxis pickerX,
            BaseAxis pickerY,
            bool pickerXAtInputZone,
            bool pickerXAtBottomZone,
            bool pickerXAtSideZone,
            bool pickerXAtOutputZone,
            bool pickerXAtInputAvoid,
            bool pickerXAtMainAvoid,
            bool pickerXAtOutputAvoid,
            bool blockInputZone,
            out string reason)
        {
            reason = string.Empty;

            if (MotionGuardRuleHelpers.IsAxisMoving(pickerX))
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + "X is moving.",
                    out reason);

            if (MotionGuardRuleHelpers.IsAxisMoving(pickerY))
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + "Y is moving.",
                    out reason);

            bool pickerXAtSafeZone =
                pickerXAtBottomZone ||
                pickerXAtSideZone ||
                pickerXAtOutputZone ||
                pickerXAtInputAvoid ||
                pickerXAtMainAvoid ||
                pickerXAtOutputAvoid;

            if (pickerXAtSafeZone)
                return true;

            if (!pickerXAtInputZone)
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + " X zone is unknown. " +
                    BuildPickerZoneState(
                        pickerName,
                        pickerX,
                        pickerY,
                        pickerXAtInputZone,
                        pickerXAtBottomZone,
                        pickerXAtSideZone,
                        pickerXAtOutputZone,
                        pickerXAtInputAvoid,
                        pickerXAtMainAvoid,
                        pickerXAtOutputAvoid),
                    out reason);

            if (!blockInputZone)
                return true;

            return MotionGuardRuleHelpers.Block(
                "ExpanderZ",
                "ExpanderZ positive move blocked. " + pickerName + " is in Input zone. " +
                BuildPickerZoneState(
                    pickerName,
                    pickerX,
                    pickerY,
                    pickerXAtInputZone,
                    pickerXAtBottomZone,
                    pickerXAtSideZone,
                    pickerXAtOutputZone,
                    pickerXAtInputAvoid,
                    pickerXAtMainAvoid,
                    pickerXAtOutputAvoid) +
                ", positiveMove=" + blockInputZone,
                out reason);
        }

        private static string BuildPickerZoneState(
            string pickerName,
            BaseAxis pickerX,
            BaseAxis pickerY,
            bool pickerXAtInputZone,
            bool pickerXAtBottomZone,
            bool pickerXAtSideZone,
            bool pickerXAtOutputZone,
            bool pickerXAtInputAvoid,
            bool pickerXAtMainAvoid,
            bool pickerXAtOutputAvoid)
        {
            try
            {
                return pickerName +
                       "X=" + FormatAxisPosition(pickerX) +
                       ", " + pickerName + "Y=" + FormatAxisPosition(pickerY) +
                       ", inputZone=" + pickerXAtInputZone +
                       ", bottomZone=" + pickerXAtBottomZone +
                       ", sideZone=" + pickerXAtSideZone +
                       ", outputZone=" + pickerXAtOutputZone +
                       ", inputAvoid=" + pickerXAtInputAvoid +
                       ", avoid=" + pickerXAtMainAvoid +
                       ", outputAvoid=" + pickerXAtOutputAvoid;
            }
            catch
            {
                return pickerName + " state unavailable.";
            }
            finally
            {
            }
        }

        private static string FormatAxisPosition(BaseAxis axis)
        {
            if (axis == null)
                return "null";

            return axis.ActualPosition.ToString("F3") +
                   ", moving=" + axis.IsMoving +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm;
        }

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

        private static bool VerifyPickerZAxesAvoid(PickerFrontUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyPickerZAxesAvoid(PickerRearUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "InputStageInterlock", reason + " - Blocked");
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
