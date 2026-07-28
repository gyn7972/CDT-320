using QMC.Common.Motion;

using QMC.Common.IO;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class OutputStageInterlockRules
    {
        // 인터락 항목: OutputStage의 Good/NG/VisionX/실린더 이동 요청을 해당 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
                return VerifyBinGoodY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "OutputGoodStageZ", out reason))
                    return false;

                return VerifyBinGoodZ(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return VerifyBinNgY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputVisionX", "OutputCameraX", "BinCameraX"))
                return VerifyBinVisionX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "GoodBinGuideLift"))
                return VerifyOutputStageCylinder(request, "GoodBinGuideLift", out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "GoodBinGuideClampLift"))
                return VerifyOutputStageCylinder(request, "GoodBinGuideClampLift", out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "GoodBinGuideClamp"))
                return VerifyOutputStageCylinder(request, "GoodBinGuideClamp", out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NGBinGuideLift"))
                return VerifyOutputStageCylinder(request, "NGBinGuideLift", out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NGBinGuideClampLift"))
                return VerifyOutputStageCylinder(request, "NGBinGuideClampLift", out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NGBinGuideClamp"))
                return VerifyOutputStageCylinder(request, "NGBinGuideClamp", out reason);

            return true;
        }

        // 인터락 항목: OutputGoodStageY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyBinGoodY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputGoodStageY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputGoodStageY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputGoodStageY(request, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 GoodStageY 이동은 Feeder/NG Clamp/기구 간섭/Stage Busy 조건을 확인한다.
        private static bool CanAutoOutputGoodStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;

            // OutputFeederY가 Avoid 위치여야 이동 가능.
            if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputGoodStageY", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                return false;

            // Feeder -> Stage Load 준비 중에는 OutputFeeder가 bin을 잡고 있어야 하므로
            // FeederY가 Avoid 위치라면 clamp/unclamp 상태로 GoodStageY 이동을 막지 않는다.

            // 인터락 조건: OutputFeeder 과부하가 감지되면 GoodStageY 자동 이동을 차단한다.
            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                return false;

            // 인터락 조건: GoodStageY 목표가 NG Stage/Guide와 기구 간섭 없는 위치인지 확인한다.
            if (!VerifyGoodStageYMechanicalClear(request, "OutputGoodStageY", out reason))
                return false;

            // 인터락 조건: NG Clamp Lift가 Up 상태가 아니면 GoodStageY 자동 이동을 차단한다.
            if (!VerifyNgClampSafeForStageMove(stage, "OutputGoodStageY", out reason))
                return false;

            // 인터락 조건: Picker/Feeder 등 Output transport 점유 상태가 해제되어 있는지 확인한다.
            if (!VerifyOutputTransportClear(machine, "OutputGoodStageY", out reason))
                return false;

            return VerifyOutputStageNotBusy(stage, "OutputGoodStageY", out reason);
        }

        // OutputGoodStageY 이동 전제: OutputFeederY가 Avoid 위치가 아니면 차단/알람.
        // 인터락 항목: GoodStageY 이동 전 OutputFeederY가 Avoid 또는 안전 위치인지 확인한다.
        private static bool VerifyOutputFeederYAvoidForGoodStageY(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (!feeder.IsBinFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: OutputFeederY가 Avoid 위치가 아닙니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying OutputFeederY avoid for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: 홈 이동 전 OutputFeederY가 Home(0) 또는 Avoid 위치인지 확인한다.
        private static bool VerifyOutputFeederYHomeOrAvoid(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (MotionGuardRuleHelpers.IsAt(feeder.FeederY, 0.0) ||
                    feeder.IsBinFeederYInAvoidPosition())
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " HOME 이동 불가: OutputFeederY가 Home(0) 또는 Avoid 위치가 아닙니다.",
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying OutputFeederY home/avoid for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // OutputGoodStageY 이동 전제 ①: OutputFeeder Ring Check 센서가 감지되면 차단/알람.
        // 인터락 항목: GoodStageY 이동 전 OutputFeeder Ring 감지 상태가 해제되어 있는지 확인한다.
        private static bool VerifyOutputFeederRingClearForGoodStageY(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (feeder.IsOutputFeederSimulationOrDryRun())
                    return true;

                if (feeder.IsBinFeederRingCheck())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: OutputFeeder Ring Check 센서가 감지되었습니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying OutputFeeder Ring Check for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // OutputGoodStageY 이동 전제 ②: OutputFeeder가 Unclamp 상태가 아니면 차단/알람.
        // 인터락 항목: GoodStageY 이동 전 OutputFeeder Clamp가 Unclamp 상태인지 확인한다.
        private static bool VerifyOutputFeederUnclampForGoodStageY(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (!feeder.IsBinFeederUnclamp())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: OutputFeeder가 Unclamp 상태가 아닙니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying OutputFeeder Unclamp for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // OutputGoodStageY 이동 전제 ③: OutputFeeder Overload 센서가 감지되면 차단/알람.
        // 인터락 항목: GoodStageY 이동 전 OutputFeeder Overload 감지 상태를 확인한다.
        private static bool VerifyOutputFeederOverloadClearForGoodStageY(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (feeder.IsFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: OutputFeeder Overload 센서가 감지되었습니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying OutputFeeder Overload for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: OutputGoodStageZ 이동 종류별로 홈/수동/자동 조건을 선택한다.
        private static bool VerifyBinGoodZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            if (!VerifyGoodStageZUpwardAbsoluteGuard(request, "OutputGoodStageZ", out reason))
                return false;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputGoodStageZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputGoodStageZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputGoodStageZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: GoodStageZ 홈은 OutputStage Busy 여부를 확인한다.
        private static bool CanHomeOutputGoodStageZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: NG Clamp Lift가 Up 상태가 아니면 GoodStageZ 홈 이동을 차단한다.
            if (!VerifyNgClampSafeForStageMove(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 GoodStageZ 홈 이동을 차단한다.
            if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            // 인터락 조건: OutputFeeder 과부하가 감지되면 GoodStageZ 홈 이동을 차단한다.
            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            return true;
        }

        // 인터락 항목: 수동 GoodStageZ 이동은 OutputStage Busy와 NG StageY 기구 간섭을 확인한다.
        private static bool CanManualOutputGoodStageZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: NG Clamp Lift가 Up 상태가 아니면 GoodStageZ 수동 이동을 차단한다.
            if (!VerifyNgClampSafeForStageMove(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason))
                return false;

            // 인터락 조건: GoodStageZ 비Avoid 이동이 NG/Guide와 간섭 없는지 확인한다.
            if (!VerifyGoodStageZNonAvoidMoveClear(request, "OutputGoodStageZ", out reason))
                return false;

            // 현재 기준: GoodStageZ가 플러스 방향으로 올라갈 때 Output 존 PickerZ0~Z3는 0 이상 또는 Avoid 위치여야 한다.
            if (!PickerZoneInterlockRules.VerifyPickerZAtOrAboveZeroForZoneStageZMove(
                machine,
                PickerWorkZone.Output,
                "OutputGoodStageZ",
                IsGoodStageZMovingPositive(request),
                out reason))
                return false;

            // OutputFeederY가 Avoid 위치여야 이동 가능.
            if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 GoodStageZ 수동 이동을 차단한다.
            if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            // 인터락 조건: OutputFeeder 과부하가 감지되면 GoodStageZ 수동 이동을 차단한다.
            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            return true;
        }

        // 인터락 항목: 자동 GoodStageZ 이동은 OutputStage Busy와 NG StageY 기구 간섭을 확인한다.
        private static bool CanAutoOutputGoodStageZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            // 현재 기준: Auto OutputGoodStageZ도 Manual OutputGoodStageZ 기본 인터락을 먼저 통과해야 한다.
            if (!CanManualOutputGoodStageZ(request, out reason))
                return false;

            // 현재 기준: Auto에서는 Output transport 점유 상태를 추가로 확인한다.
            if (!VerifyOutputTransportClear(machine, "OutputGoodStageZ", out reason))
                return false;

            // 기존 조건: Auto GoodStageZ는 OutputFeeder clamp/unclamp 상태를 강제하지 않았다.
            // 현재 필요 여부: 사용 안 함. Auto도 Manual 기본 인터락을 먼저 통과시키고, Auto 전용 예외는 별도 검토한다.

            return VerifyOutputStageNotBusy(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason);
        }

        // 인터락 항목: OutputNGStageY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            if (!VerifyNgStageYAbsoluteGuard(request, "OutputNGStageY", out reason))
                return false;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputNgStageY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputNgStageY(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputNgStageY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 NGStageY 이동은 GoodStageZ/GoodStageY 기구 간섭과 OutputStage Busy를 확인한다.
        private static bool CanAutoOutputNgStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            // 인터락 조건: NGStageY 목표가 GoodStage/Guide와 기구 간섭 없는 위치인지 확인한다.
            if (!VerifyOutputNgStageYMechanicalClear(request, "OutputNGStageY", out reason))
                return false;

            // 인터락 조건: Picker/Feeder 등 Output transport 점유 상태가 해제되어 있는지 확인한다.
            if (!VerifyOutputTransportClear(machine, "OutputNGStageY", out reason))
                return false;

            // OutputFeederY가 Avoid 위치여야 이동 가능.
            if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;


            // 인터락 조건: OutputFeeder 과부하가 감지되면 NGStageY 자동 이동을 차단한다.
            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;

            // 인터락 조건: GoodStageZ가 Avoid 위치가 아니면 NGStageY 자동 이동을 차단한다.
            if (stage != null &&
                stage.GoodStage != null &&
                !stage.IsGoodStageZAtAvoid())
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Avoid 위치여야 합니다.",
                    out reason);

            return VerifyOutputStageNotBusy(stage, "OutputNGStageY", out reason);
        }

        // 인터락 항목: OutputVisionX 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyBinVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputVisionX(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputVisionX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputVisionX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 OutputVisionX 이동 조건을 확인한다.
        // 기존 조건: OutputStage Busy(GoodStageY/GoodStageZ/NgStageY 이동 중) 시 OutputVisionX 이동 차단.
        // 현재 기준(사용자 지시 2026-07-26): OutputStage 축과 OutputVisionX는 물리 간섭이 없어
        //   상호 moving 인터락을 해제한다 — 독립 회피가 다음 Place의 StageY 이동과 중첩되는
        //   설계에서 "Interlock blocked. moving=OutputVisionX. GoodStage Y is moving." 차단 제거.
        //   픽커 존/피더/transport 인터락은 그대로 유지한다.
        private static bool CanAutoOutputVisionX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            // 인터락 조건: 자동 OutputVisionX 이동 전 홈 조건을 먼저 확인한다.
            if (!CanHomeOutputVisionX(machine, out reason))
                return false;

            // 인터락 조건: OutputCameraX는 Picker가 Output 영역을 점유 중이면 +방향 Avoid 퇴피만 허용한다.
            if (!VerifyFrontRearPickerOutputZoneClearForOutputCameraX(request, out reason))
                return false;

            // 인터락 조건: Picker/Feeder 등 Output transport 점유 상태가 해제되어 있는지 확인한다.
            return VerifyOutputTransportClear(machine, "OutputVisionX", out reason);
        }

        // OutputVisionX는 OutputFeederY가 정지된 Avoid/Down 상태일 때만 HOME·수동·자동·Jog 이동한다.
        private static bool VerifyOutputFeederAvoidAndDownForOutputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
                if (feeder == null || feeder.FeederY == null || feeder.Recipe == null)
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX 이동 불가: OutputFeederY Avoid/Down 상태를 확인할 수 없습니다.",
                        out reason);

                if (feeder.FeederY.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX 이동 불가: OutputFeederY가 이동 중입니다.",
                        out reason);

                if (!feeder.IsBinFeederAvoidPositionCheck())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX 이동 불가: OutputFeeder Avoid Dog(X091)가 ON이 아닙니다. actual=" +
                        feeder.FeederY.ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", avoid=" + feeder.Recipe.AvoidPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                        out reason);

                if (!feeder.IsFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX 이동 불가: OutputFeeder Lift가 정지된 Down 상태가 아닙니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputVisionX",
                    "OutputVisionX 이동 전 OutputFeederY Avoid/Down 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: 수동 OutputVisionX 이동은 OutputStage Busy와 Good/NG Stage 안전 위치를 확인한다.
        private static bool CanManualOutputVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                // 인터락 조건: OutputFeederY가 Avoid/Down 상태가 아니면 OutputCameraX 이동을 차단한다.
                if (!VerifyOutputFeederAvoidAndDownForOutputVisionX(machine, out reason))
                    return false;

                // 인터락 조건: OutputCameraX는 Picker가 Output 영역을 점유 중이면 +방향 Avoid 퇴피만 허용한다.
                return VerifyFrontRearPickerOutputZoneClearForOutputCameraX(request, out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputVisionX",
                    "Exception occurred while verifying OutputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: OutputCameraX 이동 전 Front/Rear Picker의 Output 존 X/Y 이동 위험을 확인한다.
        private static bool VerifyFrontRearPickerOutputZoneClearForOutputCameraX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (machine == null)
                return true;

            try
            {
                if (!VerifyPickerOutputZoneClearForOutputCameraX(request, machine, true, "Front", out reason))
                    return false;

                if (!VerifyPickerOutputZoneClearForOutputCameraX(request, machine, false, "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputVisionX",
                    "OutputCameraX 이동 전 Picker Output 영역 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: OutputCameraX 이동 전 지정 Picker의 Output 존 점유와 퇴피 방향을 확인한다.
        private static bool VerifyPickerOutputZoneClearForOutputCameraX(
            MotionGuardRuleContext request,
            CDT320_Machine machine,
            bool isFront,
            string prefix,
            out string reason)
        {
            reason = string.Empty;

            PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                machine,
                isFront,
                PickerWorkZone.Output,
                null,
                "OutputCameraX 이동 전 Picker Output 영역 확인");

            bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
            bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
            bool movingIntoOrInsideOutput = IsPickerOutputZoneMotionRisk(state, xMoving, yMoving);
            bool blocking = state != null && (state.BlocksTransport || movingIntoOrInsideOutput);
            string outputCameraMoveDetail;
            bool outputCameraRetreat = IsOutputCameraXAvoidOrPositiveDirectionMove(request, machine, out outputCameraMoveDetail);
            string detail =
                "movingX=" + xMoving +
                ", movingY=" + yMoving +
                ", movingOutputRisk=" + movingIntoOrInsideOutput +
                ", outputCameraRetreat=" + outputCameraRetreat +
                ", " + outputCameraMoveDetail +
                ", " + (state != null ? state.Describe() : "state=null");

            if (!blocking)
                return true;

            // 현재 기준: Picker가 정지 상태로 Output 존을 점유 중이어도 OutputCameraX가 +방향 Avoid로 빠지는 이동은 허용한다.
            if (!movingIntoOrInsideOutput && outputCameraRetreat)
                return true;

            // 제3 분기(사용자 승인 2026-07-24): 피커가 존을 점유/이동(퇴장 포함) 중이어도, 비전 이동 목표와
            // 해당 피커 X의 Actual/Command 양쪽이 SharedRailX 페어 간격식으로 SafetyDistance를 만족하면
            // 진입을 허용한다 (팔로잉 진입의 유지 간격 50mm > 요구 10mm, RetreatExtra 미포함 — R5.
            // 피커가 비전 쪽으로 접근 중이면 Command 판정에서 차단된다 — fail-closed).
            BaseAxis clearanceVisionAxis = machine.OutputStageUnit != null ? machine.OutputStageUnit.OutputCameraX : null;
            BaseAxis clearancePickerAxis = state != null ? state.PickerX : null;
            string clearanceDetail;
            if (request != null &&
                MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry(
                    machine, clearanceVisionAxis, request.TargetValue, clearancePickerAxis, out clearanceDetail))
                return true;

            return MotionGuardRuleHelpers.Block(
                "OutputVisionX",
                "OutputCameraX 이동 불가: " + prefix + "Picker가 Output 영역을 점유하거나 간섭 중이고 페어 간격도 부족합니다. " + detail,
                out reason);
        }

        // 인터락 기준: OutputCameraX는 +방향이 Avoid/퇴피 방향이고 -방향은 PickerX 접근 방향이다.
        private static bool IsOutputCameraXAvoidOrPositiveDirectionMove(MotionGuardRuleContext request, CDT320_Machine machine, out string detail)
        {
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            BaseAxis axis = stage != null ? stage.OutputCameraX : null;
            if (axis == null && request != null)
                axis = request.GetAxis("OutputVisionX") ?? request.GetAxis("OutputCameraX") ?? request.GetAxis("BinCameraX");

            double target = request != null ? request.TargetValue : 0.0;
            double actual = axis != null ? axis.ActualPosition : target;
            double avoid = stage != null && stage.Recipe != null && stage.Recipe.VisionX != null
                ? stage.Recipe.VisionX.AvoidPosition
                : 0.0;
            double tolerance = ResolveAxisPositionTolerance(axis);
            bool positiveDirection = axis != null && target > actual + tolerance;
            bool staysAtOrBeyondAvoid = actual >= avoid - tolerance && target >= avoid - tolerance;
            bool targetAtOrBeyondAvoid = target >= avoid - tolerance;

            detail =
                "outputCameraActual=" + actual.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", outputCameraTarget=" + target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", outputCameraAvoid=" + avoid.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", tolerance=" + tolerance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", targetAtOrBeyondAvoid=" + targetAtOrBeyondAvoid +
                ", positiveDirection=" + positiveDirection +
                ", staysAtOrBeyondAvoid=" + staysAtOrBeyondAvoid;

            return positiveDirection || staysAtOrBeyondAvoid;
        }

        // 인터락 기준: Picker가 Output 존에 머물거나 진입/이탈 중인지 판단한다.
        private static bool IsPickerOutputZoneMotionRisk(PickerZoneTransportState state, bool xMoving, bool yMoving)
        {
            if (!xMoving && !yMoving)
                return false;

            if (state == null)
                return true;

            return state.CurrentZone == PickerWorkZone.Output ||
                   state.TargetZone == PickerWorkZone.Output ||
                   state.CurrentZone == PickerWorkZone.Unknown ||
                   state.TargetZone == PickerWorkZone.Unknown ||
                   state.UnknownUnsafe;
        }

        // 인터락 항목: OutputVisionX 홈은 OutputStage Busy와 Good/NG Stage 안전 위치를 확인한다.
        private static bool CanHomeOutputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // PickerX와 OutputVisionX 간 거리는 SharedRailX Pair Clearance 룰에서 판단한다.
                return VerifyOutputFeederAvoidAndDownForOutputVisionX(machine, out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputVisionX",
                    "Exception occurred while verifying OutputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 수동 GoodStageY 이동은 Feeder/NG Clamp/기구 간섭/Stage Busy 조건을 확인한다.
        private static bool CanManualOutputGoodStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 인터락 조건: GoodStageY 목표가 NG Stage/Guide와 기구 간섭 없는 위치인지 확인한다.
                if (!VerifyGoodStageYMechanicalClear(request, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: NG Clamp Lift가 Up 상태가 아니면 GoodStageY 수동 이동을 차단한다.
                if (!VerifyNgClampSafeForStageMove(outputStage, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeederY가 Home(0) 또는 Avoid 위치여야 홈 이동 가능.
                if (!VerifyOutputFeederYHomeOrAvoid(machine, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 GoodStageY 수동 이동을 차단한다.
                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder 과부하가 감지되면 GoodStageY 수동 이동을 차단한다.
                if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputGoodStageY",
                    "Exception occurred while verifying OutputGoodStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: GoodStageY 홈은 Feeder/NG Clamp/기구 간섭/Stage Busy 조건을 확인한다.
        private static bool CanHomeOutputGoodStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 인터락 조건: GoodStageY 홈 목표가 NG Stage/Guide와 기구 간섭 없는 위치인지 확인한다.
                if (!VerifyGoodStageYHomeMechanicalClear(outputStage, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: NG Clamp Lift가 Up 상태가 아니면 GoodStageY 홈 이동을 차단한다.
                if (!VerifyNgClampSafeForStageMove(outputStage, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 GoodStageY 홈 이동을 차단한다.
                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder 과부하가 감지되면 GoodStageY 홈 이동을 차단한다.
                if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputGoodStageY",
                    "Exception occurred while verifying OutputGoodStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 수동 NGStageY 이동은 GoodStageZ/GoodStageY 기구 간섭과 OutputStage Busy를 확인한다.
        private static bool CanManualOutputNgStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 방어 조건: OutputStage 참조가 없으면 NGStageY 수동 인터락을 적용하지 않는다.
                if (outputStage == null)
                    return true;

                // 인터락 조건: GoodStageZ가 Avoid 위치가 아니면 NGStageY 수동 이동을 차단한다.
                if (outputStage.GoodStage != null && !outputStage.IsGoodStageZAtAvoid())
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Avoid 위치여야 합니다.",
                        out reason);

                // 인터락 조건: GoodBinGuideDown 센서를 갱신할 수 없으면 NGStageY 수동 이동을 차단한다.
                if (!RefreshRequiredHardwareInput(outputStage.GoodBinGuideDownSensor, "OutputNGStageY", "GoodBinGuideDown", out reason))
                    return false;
                // 인터락 조건: Good Bin Guide가 Down 상태가 아니면 NGStageY 수동 이동을 차단한다.
                if (outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 Good Bin Guide가 반드시 Down 상태여야 합니다.",
                        out reason);

                // 인터락 조건: NG Clamp/Unclamp 상태와 관계없이 Clamp Lift Up만 확인한다.
                if (!VerifyNgClampSafeForStageMove(outputStage, "OutputNGStageY", out reason))
                    return false;

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 NGStageY 수동 이동을 차단한다.
                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder 과부하가 감지되면 NGStageY 수동 이동을 차단한다.
                if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "Exception occurred while verifying OutputNGStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: NGStageY 홈은 GoodStageZ/GoodStageY 기구 간섭과 OutputStage Busy를 확인한다.
        private static bool CanHomeOutputNgStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 방어 조건: OutputStage 참조가 없으면 NGStageY 홈 인터락을 적용하지 않는다.
                if (outputStage == null)
                    return true;

                // 인터락 조건: GoodStageZ가 Home(0) 또는 Avoid 위치가 아니면 NGStageY 홈 이동을 차단한다.
                if (outputStage.GoodStage != null && !IsGoodStageZHomeOrAvoid(outputStage))
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Home(0) 또는 Avoid 위치여야 합니다.",
                        out reason);

                // 인터락 조건: GoodBinGuideDown 센서를 갱신할 수 없으면 NGStageY 홈 이동을 차단한다.
                if (!RefreshRequiredHardwareInput(outputStage.GoodBinGuideDownSensor, "OutputNGStageY", "GoodBinGuideDown", out reason))
                    return false;
                // 인터락 조건: Good Bin Guide가 Down 상태가 아니면 NGStageY 홈 이동을 차단한다.
                if (outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 Good Bin Guide가 반드시 Down 상태여야 합니다.",
                        out reason);

                // 인터락 조건: NG Clamp/Unclamp 상태와 관계없이 Clamp Lift Up만 확인한다.
                if (!VerifyNgClampSafeForStageMove(outputStage, "OutputNGStageY", out reason))
                    return false;

                // OutputFeederY가 Home(0) 또는 Avoid 위치여야 홈 이동 가능.
                if (!VerifyOutputFeederYHomeOrAvoid(machine, "OutputNGStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder가 Unclamp 상태가 아니면 NGStageY 홈 이동을 차단한다.
                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // 인터락 조건: OutputFeeder 과부하가 감지되면 NGStageY 홈 이동을 차단한다.
                if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "Exception occurred while verifying OutputNGStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: OutputStage 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyOutputStageCylinder(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyOutputStageCylinderAbsoluteGuard(request, movingName, out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeOutputStageCylinder(request.Machine, movingName, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveOutputStageCylinder(request.Machine, movingName, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying " + movingName + " cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 절대 인터락: 기존 실린더 조건과 별도로 Clamp Back 및 NG Stage Avoid 조건을 AND로 추가한다.
        private static bool VerifyOutputStageCylinderAbsoluteGuard(
            MotionGuardRuleContext request,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null
                ? request.Machine.OutputStageUnit
                : null;
            if (outputStage == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: OutputStageUnit 정보가 없습니다.",
                    out reason);

            if (string.Equals(movingName, "GoodBinGuideClampLift", System.StringComparison.OrdinalIgnoreCase) &&
                !VerifyBinGuideClampBack(outputStage, BinSide.Good, movingName, out reason))
                return false;

            if (string.Equals(movingName, "NGBinGuideClampLift", System.StringComparison.OrdinalIgnoreCase) &&
                !VerifyBinGuideClampBack(outputStage, BinSide.Ng, movingName, out reason))
                return false;

            // Good Guide Down은 안전 복귀이므로 NG Stage 위치 조건을 새로 추가하지 않는다.
            if (string.Equals(movingName, "GoodBinGuideLift", System.StringComparison.OrdinalIgnoreCase) &&
                request != null &&
                request.TargetValue >= 0.5 &&
                !outputStage.IsNgStageInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "GoodBinGuideLift Up 불가: OutputNGStageY가 정확한 Avoid 위치가 아닙니다.",
                    out reason);
            }

            return true;
        }

        // 절대 인터락: ClampLift Up/Down 전에 해당 Clamp가 Bwd/Unclamp 상태인지 확인한다.
        private static bool VerifyBinGuideClampBack(
            OutputStageUnit outputStage,
            BinSide side,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            BaseCylinder clampCylinder = side == BinSide.Ng
                ? outputStage.NgBinGuideClampCylinder
                : outputStage.GoodBinGuideClampCylinder;
            string sideName = side == BinSide.Ng ? "NG" : "GOOD";

            if (clampCylinder == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: " + sideName + " Bin Guide Clamp 실린더 정보가 없습니다.",
                    out reason);

            BaseDigitalInput backStateSensor = clampCylinder.Setup != null && clampCylinder.Setup.UseBwdSensor
                ? clampCylinder.InBwd
                : clampCylinder.InFwd;

            if (!RefreshRequiredHardwareInput(backStateSensor, movingName, sideName + "BinClampBack", out reason))
                return false;

            if (!outputStage.IsBinGuideUnclamped(side))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: " + sideName + " Bin Guide Clamp가 Bwd/Unclamp 상태여야 합니다.",
                    out reason);

            return true;
        }

        // 절대 인터락: NGStageY는 Good Z 최소 안전 높이, NG ClampLift Up, Good Guide Down을 모두 만족해야 한다.
        private static bool VerifyNgStageYAbsoluteGuard(
            MotionGuardRuleContext request,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null
                ? request.Machine.OutputStageUnit
                : null;
            BaseAxis goodZ = outputStage != null && outputStage.GoodStage != null
                ? outputStage.GoodStage.StageZ
                : null;

            if (outputStage == null || goodZ == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: OutputStageUnit/OutputGoodStageZ 정보가 없습니다.",
                    out reason);

            if (!outputStage.IsGoodStageZAtAvoid() && goodZ.ActualPosition > 0.0)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: OutputGoodStageZ가 정확한 Avoid 또는 0 이하 위치여야 합니다. " +
                    "goodZActual=" + goodZ.ActualPosition.ToString("0.###"),
                    out reason);

            if (!VerifyNgClampSafeForStageMove(outputStage, movingName, out reason))
                return false;

            return VerifyGoodBinGuideDown(outputStage, movingName, out reason);
        }

        // 절대 인터락: GoodStageZ의 실제 상승 명령은 목표가 Avoid여도 NG Stage exact Avoid를 요구한다.
        private static bool VerifyGoodStageZUpwardAbsoluteGuard(
            MotionGuardRuleContext request,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null
                ? request.Machine.OutputStageUnit
                : null;
            BaseAxis goodZ = outputStage != null && outputStage.GoodStage != null
                ? outputStage.GoodStage.StageZ
                : null;

            if (outputStage == null || goodZ == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: OutputStageUnit/OutputGoodStageZ 정보가 없습니다.",
                    out reason);

            if (request.TargetValue > goodZ.ActualPosition &&
                !outputStage.IsNgStageInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 상승 이동 불가: OutputNGStageY가 정확한 Avoid 위치여야 합니다. " +
                    "current=" + goodZ.ActualPosition.ToString("0.###") +
                    ", target=" + request.TargetValue.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        // 인터락 항목: OutputStage 실린더 초기화는 OutputStage 이송부 안전 상태를 확인한다.
        private static bool CanInitializeOutputStageCylinder(
            CDT320_Machine machine,
            string movingName,
            double targetValue,
            out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd", "Bwd");

            if (!VerifyOutputTransportClear(machine, movingName, out reason))
            {
                reason = movingName + " initialize " + direction + " blocked. " + reason;
                return false;
            }

            return VerifyOutputStageNotBusy(
                machine != null ? machine.OutputStageUnit : null,
                movingName,
                out reason);
        }

        // 인터락 항목: OutputStage 실린더 이동은 OutputStage 이송부 안전 상태를 확인한다.
        private static bool CanMoveOutputStageCylinder(
            CDT320_Machine machine,
            string movingName,
            double targetValue,
            out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd", "Bwd");

            if (!VerifyOutputTransportClear(machine, movingName, out reason))
            {
                reason = movingName + " move " + direction + " blocked. " + reason;
                return false;
            }

            return VerifyOutputStageNotBusy(
                machine != null ? machine.OutputStageUnit : null,
                movingName,
                out reason);
        }

        // 인터락 항목: OutputStage 이동 전 OutputFeederY 이송부가 안전 위치인지 확인한다.
        private static bool VerifyOutputTransportClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            OutputFeederUnit feeder = machine.OutputFeederUnit;
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, "OutputFeederY is moving.", out reason);

            OutputCassetteUnit cassette = machine.OutputCassetteUnit;
            if (cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, "OutputLifterZ is moving.", out reason);

            return true;
        }

        private static string ResolveCylinderDirection(double targetValue, string fwdText, string bwdText)
        {
            return targetValue >= 0.5 ? fwdText : bwdText;
        }

        public static bool TryGetNgStageMaterialPresence(
            OutputStageUnit outputStage,
            string operationName,
            out bool materialPresent,
            out string reason)
        {
            materialPresent = false;
            reason = string.Empty;

            try
            {
                if (outputStage == null)
                {
                    reason = "OutputStageUnit을 찾을 수 없습니다.";
                    return false;
                }

                if (!RefreshRequiredHardwareInput(
                    outputStage.NgBinRingSensor,
                    operationName,
                    "NgBinRing",
                    out reason))
                    return false;

                WaferMaterial storedMaterial = MaterialStateService.GetWaferAtLocation(
                    MaterialLocationKind.OutputStageNg);
                bool ringDetected = outputStage.NgBinRingSensor != null &&
                    outputStage.NgBinRingSensor.IsOn;
                materialPresent = storedMaterial != null || ringDetected;
                return true;
            }
            catch (System.Exception ex)
            {
                reason = "NG Stage 제품 감지 확인 중 예외가 발생했습니다. operation=" +
                    operationName + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        // 인터락 항목: NG Stage Clamp/Unclamp 상태와 관계없이 Clamp Lift Up만 확인한다.
        public static bool VerifyNgClampSafeForStageMove(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (outputStage == null)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: OutputStageUnit을 찾을 수 없습니다.",
                        out reason);

                if (!RefreshRequiredHardwareInput(outputStage.NgBinClampUpSensor, movingName, "NgBinClampUp", out reason))
                    return false;

                if (!outputStage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: NG Bin Clamp Lift가 Up 상태여야 합니다. " +
                        "NG Bin Clamp의 Clamp/Unclamp 상태는 이동 조건에 포함되지 않습니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " NG Clamp 안전 조건 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: GoodStageY 이동 전 GoodStageZ 하강/NGStageY 위치에 따른 기구 간섭을 확인한다.
        private static bool VerifyGoodStageYMechanicalClear(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
            if (outputStage == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: OutputStageUnit 정보가 없습니다.",
                    out reason);

            if (!outputStage.IsNgStageInAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: GoodStageY 이동 전 NG Stage가 반드시 Avoid 위치여야 합니다.",
                    out reason);

            if (!VerifyGoodBinGuideDown(outputStage, movingName, out reason))
                return false;

            bool requiresGoodZAvoid = request == null ||
                                      request.MoveKind == MotionGuardMoveKind.AxisHome ||
                                      IsGoodStageYTargetRequiringGoodZAvoid(outputStage, request.TargetValue);

            if (requiresGoodZAvoid && !outputStage.IsGoodStageZAtAvoid())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: GoodStageY Load/Avoid/Unload/Home 이동 전 OutputGoodStageZ가 반드시 Avoid 위치여야 합니다.",
                    out reason);

            if (!requiresGoodZAvoid && !outputStage.IsGoodStageZInAvoidOrProcessPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: GoodStageY 공정 이동 전 OutputGoodStageZ는 Avoid 또는 Process 위치여야 합니다.",
                    out reason);

            return true;
        }

        // 인터락 기준: 홈 이동 전 GoodStageZ가 Home(0) 또는 Avoid 위치인지 판단한다.
        private static bool IsGoodStageZHomeOrAvoid(OutputStageUnit outputStage)
        {
            if (outputStage == null)
                return true;

            BaseAxis goodStageZ = outputStage.GoodStage != null ? outputStage.GoodStage.StageZ : null;
            if (MotionGuardRuleHelpers.IsAt(goodStageZ, 0.0))
                return true;

            return outputStage.IsGoodStageZAtAvoid();
        }

        // 인터락 항목: GoodStageY 홈 전 GoodStageZ와 NGStageY 기구 간섭을 확인한다.
        private static bool VerifyGoodStageYHomeMechanicalClear(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (outputStage == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " HOME 절대 인터락 확인 불가: OutputStageUnit 정보가 없습니다.",
                    out reason);

            if (!outputStage.IsNgStageInAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " HOME 이동 불가: NG Stage가 정확한 Avoid 위치여야 합니다.",
                    out reason);

            if (!VerifyGoodBinGuideDown(outputStage, movingName, out reason))
                return false;

            if (!IsGoodStageZHomeOrAvoid(outputStage))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " HOME 이동 불가: OutputGoodStageZ가 Home(0) 또는 Avoid 위치가 아닙니다.",
                    out reason);

            return true;
        }

        // 절대 인터락: GoodStageY 일반/HOME 이동 전에 Good Bin Guide Down을 확인한다.
        private static bool VerifyGoodBinGuideDown(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (outputStage == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Good Bin Guide Down 상태를 확인할 OutputStageUnit 정보가 없습니다.",
                    out reason);

            if (!RefreshRequiredHardwareInput(
                outputStage.GoodBinGuideDownSensor,
                movingName,
                "GoodBinGuideDown",
                out reason))
                return false;

            if (!outputStage.IsBinGuideDown(BinSide.Good))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Good Bin Guide가 Down 상태여야 합니다.",
                    out reason);

            return true;
        }

        // 인터락 기준: GoodStageY 목표가 GoodStageZ Avoid를 요구하는 위치인지 판단한다.
        private static bool IsGoodStageYTargetRequiringGoodZAvoid(OutputStageUnit outputStage, double target)
        {
            if (outputStage == null || outputStage.Recipe == null || outputStage.Recipe.GoodStageY == null)
                return true;

            StageAxisPositions y = outputStage.Recipe.GoodStageY;
            BaseAxis axis = outputStage.GoodStage != null ? outputStage.GoodStage.StageY : null;

            return IsTargetPosition(axis, target, y.AvoidPosition) ||
                   IsTargetPosition(axis, target, y.LoadPosition) ||
                   IsTargetPosition(axis, target, y.UnloadPosition);
        }

        // 인터락 항목: GoodStageZ가 Avoid 외 위치로 움직일 때 NGStageY 간섭을 확인한다.
        private static bool VerifyGoodStageZNonAvoidMoveClear(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
            if (outputStage == null)
                return true;

            double target = request != null ? request.TargetValue : 0.0;
            if (IsGoodStageZAvoidTarget(outputStage, target))
                return true;

            if (!outputStage.IsNgStageInAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: GoodStageZ가 Avoid 외 위치로 상승/이동하려면 NG Stage가 반드시 Avoid 위치여야 합니다.",
                    out reason);

            return true;
        }

        // 인터락 항목: NGStageY 이동 전 GoodStageY/GoodStageZ 기구 간섭을 확인한다.
        private static bool VerifyOutputNgStageYMechanicalClear(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
            if (outputStage == null)
                return true;

            if (!RefreshRequiredHardwareInput(outputStage.GoodBinGuideDownSensor, movingName, "GoodBinGuideDown", out reason))
                return false;
            if (outputStage.GoodBinGuideDownSensor != null &&
                !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                !outputStage.GoodBinGuideDownSensor.IsOn)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: NG StageY 이동 전 Good Bin Guide가 반드시 Down 상태여야 합니다.",
                    out reason);

            if (!VerifyNgClampSafeForStageMove(outputStage, movingName, out reason))
                return false;

            return true;
        }

        // 인터락 기준: NGStageY 목표가 Avoid 위치인지 판단한다.
        private static bool IsNgStageYAvoidTarget(OutputStageUnit outputStage, double target)
        {
            if (outputStage == null || outputStage.Recipe == null || outputStage.Recipe.NGStageY == null)
                return false;

            return System.Math.Abs(target - outputStage.Recipe.NGStageY.AvoidPosition) <= 0.001;
        }

        // 인터락 기준: GoodStageZ 목표가 Avoid 위치인지 판단한다.
        private static bool IsGoodStageZAvoidTarget(OutputStageUnit stage, double target)
        {
            if (stage == null || stage.Recipe == null || stage.Recipe.GoodStageZ == null)
                return false;

            BaseAxis axis = stage.GoodStage != null ? stage.GoodStage.StageZ : null;
            return IsTargetPosition(axis, target, stage.Recipe.GoodStageZ.AvoidPosition);
        }

        // 인터락 기준: GoodStageZ가 양방향 상승 이동 중인지 판단한다.
        private static bool IsGoodStageZMovingPositive(MotionGuardRuleContext request)
        {
            try
            {
                OutputStageUnit stage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
                if (stage == null)
                    return false;

                BaseAxis axis = stage.GoodStage != null ? stage.GoodStage.StageZ : null;
                double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;
                return axis != null && request != null && request.TargetValue > axis.ActualPosition + tolerance;
            }
            catch
            {
                return false;
            }
        }

        // 인터락 기준: GoodStageZ 목표가 Load 또는 Unload 위치인지 판단한다.
        private static bool IsGoodStageZLoadOrUnloadTarget(OutputStageUnit stage, double target)
        {
            if (stage == null || stage.Recipe == null || stage.GoodStage == null || stage.GoodStage.StageZ == null)
                return false;

            return IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.LoadPosition) ||
                   IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.UnloadPosition);
        }

        // 인터락 기준: 목표 위치가 지정 위치 허용오차 안인지 판단한다.
        private static bool IsTargetPosition(BaseAxis axis, double target, double position)
        {
            double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;

            return System.Math.Abs(target - position) <= tolerance;
        }

        // 인터락 기준: 축별 InPosition 허용오차를 우선 사용하고 없으면 기본 허용오차를 사용한다.
        private static double ResolveAxisPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
        }

        // 인터락 항목: OutputStage 내부 다른 축/실린더가 이동 중인지 확인한다.
        // 기존 조건: OutputVisionX 이동 중에도 스테이지 축/실린더 이동을 차단했다.
        // 현재 기준(사용자 지시 2026-07-26): OutputStage 축과 OutputVisionX는 물리 간섭이 없어
        //   OutputVisionX moving 항목을 제거한다(양방향 분리). 스테이지 축 간 상호 busy는 유지.
        private static bool VerifyOutputStageNotBusy(OutputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            //if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageY : null, movingName, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
            //    return MotionGuardRuleHelpers.Block(movingName, "GoodStage Y is moving.", out reason);
            if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageZ : null, movingName, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
                return MotionGuardRuleHelpers.Block(movingName, "GoodStage Z is moving.", out reason);
            if (IsMovingExcept(stage.NgStage != null ? stage.NgStage.StageY : null, movingName, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return MotionGuardRuleHelpers.Block(movingName, "NgStage Y is moving.", out reason);

            return true;
        }

        // 인터락 기준: DryRun 입력은 실제 센서가 없어도 안전 상태로 인정한다.
        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null &&
                   input.Config.IgnoreWaits && input.Config.IsSimulationMode;
        }

        private static bool RefreshRequiredHardwareInput(BaseDigitalInput input, string movingName, string signalName, out string reason)
        {
            reason = string.Empty;

            if (!IsStrictHardwareMode())
                return true;

            if (input == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    signalName + " sensor is not registered in real hardware mode.",
                    out reason);
            // Todo : 김영남 초기화 확인후. 가드 활성화
            //if (input.Config != null && (input.Config.IsSimulationMode || input.Config.IgnoreWaits))
            //    return MotionGuardRuleHelpers.Block(
            //        movingName,
            //        signalName + " sensor is still simulation/dry-run mode in real hardware mode. " +
            //        "Refresh DIO configuration before movement.",
            //        out reason);

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(input, out errorCode))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    signalName + " hardware read failed in real hardware mode. error=" + errorCode,
                    out reason);

            return true;
        }

        // 인터락 기준: 실제 하드웨어 센서를 엄격하게 볼 모드인지 판단한다.
        private static bool IsStrictHardwareMode()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings != null &&
                       settings.UseAjin &&
                       !settings.SimulationMode &&
                       !settings.DryRunMode &&
                       !settings.BypassHardware;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
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

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputStageInterlock", reason + " - Blocked");
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
