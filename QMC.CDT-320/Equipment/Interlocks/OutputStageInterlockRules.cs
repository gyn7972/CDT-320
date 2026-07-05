using QMC.Common.Motion;

using QMC.Common.IO;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class OutputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
                return VerifyBinGoodY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
                return VerifyBinGoodZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return VerifyBinNgY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputVisionX", "OutputVisionX"))
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

            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                return false;

            if (!VerifyGoodStageYMechanicalClear(request, "OutputGoodStageY", out reason))
                return false;

            if (!VerifyNgClampLiftUpForGoodStageMove(stage, "OutputGoodStageY", out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputGoodStageY", out reason))
                return false;

            return VerifyOutputStageNotBusy(stage, "OutputGoodStageY", out reason);
        }

        // OutputGoodStageY 이동 전제: OutputFeederY가 Avoid 위치가 아니면 차단/알람.
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

        // OutputGoodStageY 이동 전제 ①: OutputFeeder Ring Check 센서가 감지되면 차단/알람.
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

        private static bool VerifyBinGoodZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

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

        private static bool CanHomeOutputGoodStageZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (!VerifyNgClampLiftUpForGoodStageMove(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            return true;
        }

        private static bool CanManualOutputGoodStageZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyNgClampLiftUpForGoodStageMove(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason))
                return false;

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

            if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputGoodStageZ", out reason))
                return false;

            return true;
        }

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

        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

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

        private static bool CanAutoOutputNgStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            if (!VerifyOutputNgStageYMechanicalClear(request, "OutputNGStageY", out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputNGStageY", out reason))
                return false;

            // OutputFeederY가 Avoid 위치여야 이동 가능.
            if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;

            // OutputFeeder 상태 — 세 조건 개별 확인.
            if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;


            if (!VerifyOutputFeederOverloadClearForGoodStageY(machine, "OutputNGStageY", out reason))
                return false;

            if (stage != null &&
                stage.GoodStage != null &&
                !stage.IsGoodStageZAtAvoid())
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Avoid 위치여야 합니다.",
                    out reason);

            return VerifyOutputStageNotBusy(stage, "OutputNGStageY", out reason);
        }

        private static bool VerifyBinVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputVisionX(request.Machine, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputVisionX(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputVisionX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanAutoOutputVisionX(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeOutputVisionX(machine, out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputVisionX", out reason))
                return false;

            return VerifyOutputStageNotBusy(machine != null ? machine.OutputStageUnit : null, "OutputVisionX", out reason);
        }

        private static bool CanManualOutputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // PickerX와 OutputVisionX 간 거리는 SharedRailX Pair Clearance 룰에서 판단한다.

                OutputFeederUnit outputFeeder = machine != null ? machine.OutputFeederUnit : null;
                if (outputFeeder != null && !outputFeeder.IsBinFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeederY must be at Avoid position.",
                        out reason);

                if (outputFeeder != null && !outputFeeder.IsFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeeder lift cylinder must be down.",
                        out reason);

                return true;
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

        private static bool CanHomeOutputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // PickerX와 OutputVisionX 간 거리는 SharedRailX Pair Clearance 룰에서 판단한다.

                OutputFeederUnit outputFeeder = machine != null ? machine.OutputFeederUnit : null;
                if (outputFeeder != null && !outputFeeder.IsBinFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeederY must be at Avoid position.",
                        out reason);

                if (outputFeeder != null && !outputFeeder.IsFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeeder lift cylinder must be down.",
                        out reason);

                return true;
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

        private static bool CanManualOutputGoodStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (!VerifyGoodStageYMechanicalClear(request, "OutputGoodStageY", out reason))
                    return false;

                if (!VerifyNgClampLiftUpForGoodStageMove(outputStage, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

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

        private static bool CanHomeOutputGoodStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (!VerifyGoodStageYHomeMechanicalClear(outputStage, "OutputGoodStageY", out reason))
                    return false;

                if (!VerifyNgClampLiftUpForGoodStageMove(outputStage, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputGoodStageY", out reason))
                    return false;

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

        private static bool CanManualOutputNgStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (outputStage == null)
                    return true;

                if (outputStage.GoodStage != null && !outputStage.IsGoodStageZAtAvoid())
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Avoid 위치여야 합니다.",
                        out reason);

                if (!RefreshRequiredHardwareInput(outputStage.GoodBinGuideDownSensor, "OutputNGStageY", "GoodBinGuideDown", out reason))
                    return false;
                if (outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 Good Bin Guide가 반드시 Down 상태여야 합니다.",
                        out reason);

                if (!RefreshRequiredHardwareInput(outputStage.NgBinClampUpSensor, "OutputNGStageY", "NgBinClampUp", out reason))
                    return false;
                if (outputStage.NgBinClampUpSensor != null &&
                    !IsDryRunInput(outputStage.NgBinClampUpSensor) &&
                    !outputStage.NgBinClampUpSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 NG Bin Clamp Lift가 반드시 Up 상태여야 합니다.",
                        out reason);

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

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

        private static bool CanHomeOutputNgStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (outputStage == null)
                    return true;

                if (outputStage.GoodStage != null && !outputStage.IsGoodStageZAtAvoid())
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 GoodStageZ가 반드시 Avoid 위치여야 합니다.",
                        out reason);

                if (!RefreshRequiredHardwareInput(outputStage.GoodBinGuideDownSensor, "OutputNGStageY", "GoodBinGuideDown", out reason))
                    return false;
                if (outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 Good Bin Guide가 반드시 Down 상태여야 합니다.",
                        out reason);

                if (!RefreshRequiredHardwareInput(outputStage.NgBinClampUpSensor, "OutputNGStageY", "NgBinClampUp", out reason))
                    return false;
                if (outputStage.NgBinClampUpSensor != null &&
                    !IsDryRunInput(outputStage.NgBinClampUpSensor) &&
                    !outputStage.NgBinClampUpSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY 이동 불가: NG StageY 이동 전 NG Bin Clamp Lift가 반드시 Up 상태여야 합니다.",
                        out reason);

                // OutputFeederY가 Avoid 위치여야 이동 가능.
                if (!VerifyOutputFeederYAvoidForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                // OutputFeeder 상태 — 세 조건 개별 확인.
                if (!VerifyOutputFeederRingClearForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

                if (!VerifyOutputFeederUnclampForGoodStageY(machine, "OutputNGStageY", out reason))
                    return false;

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

        private static bool VerifyOutputStageCylinder(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
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

        private static bool VerifyNgClampLiftUpForGoodStageMove(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (outputStage == null)
                return true;

            if (!RefreshRequiredHardwareInput(outputStage.NgBinClampUpSensor, movingName, "NgBinClampUp", out reason))
                return false;

            if (!outputStage.IsBinGuideClampLiftUp(BinSide.Ng))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " move blocked. NG Bin Clamp Lift must be up before GoodStage movement.",
                    out reason);

            return true;
        }

        private static bool VerifyGoodStageYMechanicalClear(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
            if (outputStage == null)
                return true;

            if (!outputStage.IsNgStageInAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: GoodStageY 이동 전 NG Stage가 반드시 Avoid 위치여야 합니다.",
                    out reason);

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

        private static bool VerifyGoodStageYHomeMechanicalClear(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (outputStage == null)
                return true;

            if (!outputStage.IsGoodStageZAtAvoid())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " HOME 이동 불가: OutputGoodStageZ가 Avoid 위치가 아닙니다.",
                    out reason);

            return true;
        }

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

            if (!RefreshRequiredHardwareInput(outputStage.NgBinClampUpSensor, movingName, "NgBinClampUp", out reason))
                return false;
            if (outputStage.NgBinClampUpSensor != null &&
                !IsDryRunInput(outputStage.NgBinClampUpSensor) &&
                !outputStage.NgBinClampUpSensor.IsOn)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: NG StageY 이동 전 NG Bin Clamp Lift가 반드시 Up 상태여야 합니다.",
                    out reason);

            return true;
        }

        private static bool IsNgStageYAvoidTarget(OutputStageUnit outputStage, double target)
        {
            if (outputStage == null || outputStage.Recipe == null || outputStage.Recipe.NGStageY == null)
                return false;

            return System.Math.Abs(target - outputStage.Recipe.NGStageY.AvoidPosition) <= 0.001;
        }

        private static bool IsGoodStageZAvoidTarget(OutputStageUnit stage, double target)
        {
            if (stage == null || stage.Recipe == null || stage.Recipe.GoodStageZ == null)
                return false;

            BaseAxis axis = stage.GoodStage != null ? stage.GoodStage.StageZ : null;
            return IsTargetPosition(axis, target, stage.Recipe.GoodStageZ.AvoidPosition);
        }

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

        private static bool IsGoodStageZLoadOrUnloadTarget(OutputStageUnit stage, double target)
        {
            if (stage == null || stage.Recipe == null || stage.GoodStage == null || stage.GoodStage.StageZ == null)
                return false;

            return IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.LoadPosition) ||
                   IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.UnloadPosition);
        }

        private static bool IsTargetPosition(BaseAxis axis, double target, double position)
        {
            double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;

            return System.Math.Abs(target - position) <= tolerance;
        }

        private static bool VerifyOutputStageNotBusy(OutputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageY : null, movingName, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
                return MotionGuardRuleHelpers.Block(movingName, "GoodStage Y is moving.", out reason);
            if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageZ : null, movingName, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
                return MotionGuardRuleHelpers.Block(movingName, "GoodStage Z is moving.", out reason);
            if (IsMovingExcept(stage.NgStage != null ? stage.NgStage.StageY : null, movingName, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return MotionGuardRuleHelpers.Block(movingName, "NgStage Y is moving.", out reason);
            if (IsMovingExcept(stage.OutputCameraX, movingName, "OutputVisionX"))
                return MotionGuardRuleHelpers.Block(movingName, "OutputVisionX is moving.", out reason);

            return true;
        }

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

            if (input.Config != null && (input.Config.IsSimulationMode || input.Config.IgnoreWaits))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    signalName + " sensor is still simulation/dry-run mode in real hardware mode. " +
                    "Refresh DIO configuration before movement.",
                    out reason);

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(input, out errorCode))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    signalName + " hardware read failed in real hardware mode. error=" + errorCode,
                    out reason);

            return true;
        }

        private static bool IsStrictHardwareMode()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings != null &&
                       settings.UseAjin &&
                       !settings.SimulationMode &&
                       !settings.DryRunMode;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
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
