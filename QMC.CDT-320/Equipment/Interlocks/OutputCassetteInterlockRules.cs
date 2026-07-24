using QMC.Common;
using System;
using System.Threading.Tasks;

namespace QMC.CDT320.Interlocks
{
    public static class OutputCassetteInterlockRules
    {
        // 인터락 항목: OutputLifterZ 이동 요청을 Output Cassette 리프터 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputLifterZ", "ElevatorZ_Output", "OutputLifterZ"))
                return VerifyBinLifterZ(request, out reason);

            return true;
        }

        // 인터락 항목: OutputLifterZ 이동 종류별로 홈/티칭 리프터 조건을 선택한다.
        private static bool VerifyBinLifterZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputCassetteUnit Cassette = request.Machine.OutputCassetteUnit;
                OutputFeederUnit feeder = request.Machine.OutputFeederUnit;
                PickerFrontUnit frontPicker = request.Machine.PickerFrontUnit;
                PickerRearUnit rearPicker = request.Machine.PickerRearUnit;

                switch (request.MoveKind)
                {
                    // 일반 이동 인터락 확인
                    case MotionGuardMoveKind.AxisMove:
                    // 홈 이동 인터락 확인
                    case MotionGuardMoveKind.AxisHome:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                            return false;

                        return CanHomeBinLifterZ(Cassette, feeder, out reason);
                    // 티칭 이동 인터락 확인
                    case MotionGuardMoveKind.AxisTeachingMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                            return false;

                        return CanMoveBinLifterZ(Cassette, feeder, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch(Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    $"Exception occurred while verifying OutputLifterZ motion guard rules: {ex.Message}",
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
            
        }

        // 인터락 조건: OutputLifterZ 이동 전 FrontPickerX가 정확한 AvoidPosition인지 확인한다.
        private static bool VerifyFrontPickerXAvoidPosition(
            PickerFrontUnit frontPicker,
            out string reason)
        {
            reason = string.Empty;

            if (frontPicker == null ||
                frontPicker.PickerX == null ||
                frontPicker.Recipe == null ||
                frontPicker.Recipe.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "FrontPickerX AvoidPosition을 확인할 수 없습니다. OutputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = frontPicker.Recipe.PickerX.AvoidPosition;
            if (!frontPicker.IsFrontPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "FrontPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + frontPicker.PickerX.ActualPosition.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        // 인터락 조건: OutputLifterZ 이동 전 RearPickerX가 정확한 AvoidPosition인지 확인한다.
        // (기존에는 Front Picker만 확인해 Rear Picker 간섭 위치에서도 리프터 이동이 허용되었다.)
        private static bool VerifyRearPickerXAvoidPosition(
            PickerRearUnit rearPicker,
            out string reason)
        {
            reason = string.Empty;

            if (rearPicker == null ||
                rearPicker.PickerX == null ||
                rearPicker.Recipe == null ||
                rearPicker.Recipe.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "RearPickerX AvoidPosition을 확인할 수 없습니다. OutputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = rearPicker.Recipe.PickerX.AvoidPosition;
            if (!rearPicker.IsRearPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "RearPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + rearPicker.PickerX.ActualPosition.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        // 인터락 항목: OutputLifterZ 홈은 Bin 돌출, OutputFeederY 이동 중, 카세트 안전 위치를 확인한다.
        private static bool CanHomeBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            // 인터락 조건: Bin 돌출이 감지되면 OutputLifterZ 홈 이동을 차단한다.
            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ home is blocked.",
                    out reason);
            }

            // 방어 조건: Feeder 참조가 없으면 Feeder 연동 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: OutputFeederY가 이동 중이면 OutputLifterZ 홈 이동을 차단한다.
            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ home is blocked.",
                    out reason);
            }

            // 인터락 조건: OutputFeederY가 카세트 안전 위치가 아니면 OutputLifterZ 홈 이동을 차단한다.
            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ home.",
                    out reason);
            }

            return true;
        }

        // 인터락 항목: OutputLifterZ 이동은 Bin 돌출, OutputFeederY 이동 중, 카세트 안전 위치를 확인한다.
        private static bool CanMoveBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ move.",
                    out reason);
            }

            return true;
        }

        // 인터락 기준: OutputLifterZ 이동 전 OutputFeederY가 Avoid 또는 카세트/스테이지 안전 위치인지 판단한다.
        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
                return true;

            if (feeder.IsBinFeederYInAvoidPosition())
                return true;

            return IsOutputFeederYSafeForOutputLifterZ(feeder, BinSide.Good) ||
                   IsOutputFeederYSafeForOutputLifterZ(feeder, BinSide.Ng);
        }

        // 인터락 기준: Good/NG별 OutputFeederY 카세트/스테이지 로드·언로드 안전 위치를 판단한다.
        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder, BinSide side)
        {
            return feeder.IsBinFeederYInCassetteLoadPosition(side) ||
                   feeder.IsBinFeederYInCassetteUnloadPosition(side) ||
                   feeder.IsBinFeederYInStageLoadPosition(side) ||
                   feeder.IsBinFeederYInStageLoadAvoidPosition(side) ||
                   feeder.IsBinFeederYInStageUnloadPosition(side) ||
                   feeder.IsBinFeederYInStageUnloadAvoidPosition(side);
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputCassetteInterlock", reason + " - Blocked");
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
