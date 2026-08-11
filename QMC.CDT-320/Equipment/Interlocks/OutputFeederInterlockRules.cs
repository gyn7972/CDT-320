using QMC.Common.IO;

using System;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class OutputFeederInterlockRules
    {
        #region 규칙 진입

        // 인터락 항목: OutputFeederY/Lift/Clamp 이동 요청을 각 Feeder 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederY", "FeederY_Output", "OutputFeederY"))
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "OutputFeederY", out reason))
                    return false;

                return VerifyBinFeederY(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederLift", "OutputFeeder Up/Down"))
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "OutputFeederLift", out reason))
                    return false;

                return VerifyOutputFeederLift(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederClamp", "OutputFeeder Clamp/UnClamp"))
                return VerifyOutputFeederClamp(request, out reason);

            return true;
        }

        #endregion

        #region Output Feeder Y

        // 인터락 항목: OutputFeederY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyBinFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (!VerifyOutputFeederYAbsoluteGuard(request, out reason))
                return false;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoOutputFeederY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualOutputFeederY(machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputFeederY(machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 절대 인터락: 기존 FeederY 분기 전에 Camera/Lift/Overload 최소 안전조건을 AND로 적용한다.
        private static bool VerifyOutputFeederYAbsoluteGuard(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            OutputFeederUnit feeder = machine != null ? machine.OutputFeederUnit : null;
            BaseAxis cameraX = stage != null ? stage.OutputCameraX : null;

            if (stage == null || cameraX == null || feeder == null || feeder.FeederY == null ||
                stage.Recipe == null || stage.Recipe.VisionX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY 절대 인터락 확인 불가: OutputStage/OutputCameraX/OutputFeederY teaching 정보가 없습니다.",
                    out reason);
            }

            // To do: [Feeder HOME 카메라 퇴피 폐지 2026-08-11] Step 260의 OutputVisionX PEL(+) 물리 퇴피를
            //        제거했다. 퇴피를 근거로 이 카메라 조건을 대체했던 초기화 전용 예외
            //        (MotionGuardRuntime.IsFeederHomeVisionRetreatActive)도 함께 폐지했다.
            //        기존 조건: Dog 여부와 무관하게 카메라 위치를 항상 요구했다(Input과 비대칭).
            //        현재 기준: Input과 동일하게 Feeder Avoid Dog 실입력을 안전 근거로 삼아,
            //                  Dog ON이면 카메라 위치를 보지 않고 통과한다(사용자 지시 2026-08-11).
            //        Dog OFF에서 카메라가 안전 위치가 아니면 차단하고, 작업자가 Feeder를 Avoid로
            //        빼서 Dog를 ON으로 만든 뒤 재시도한다(카메라를 자동으로 움직이지 않는다).
            if (!feeder.IsOutputFeederSimulationOrDryRun())
            {
                int avoidDogReadError = -1;
                if (feeder.BinFeederAvoidPositionCheckSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(
                        feeder.BinFeederAvoidPositionCheckSensor,
                        out avoidDogReadError))
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeeder Avoid Dog 센서 갱신 실패. error=" + avoidDogReadError,
                        out reason);
                }
            }

            bool feederAvoidDogOn = feeder.IsBinFeederAvoidPositionCheck();

            if (!feederAvoidDogOn)
            {
                if (!IsOutputVisionXInAvoidPosition(stage) &&
                    cameraX.ActualPosition < 1000.0)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY 이동 불가: Avoid Dog가 OFF이고 OutputCameraX가 정확한 Avoid 또는 1000 이상 위치도 아닙니다. " +
                        "OutputFeeder를 Avoid 위치로 이동시켜 Avoid Dog를 ON으로 만든 후 다시 실행하십시오. " +
                        "moveKind=" + request.MoveKind +
                        ", avoidDog=OFF" +
                        ", cameraActual=" + cameraX.ActualPosition.ToString("0.###") +
                        ", cameraInAvoid=False",
                        out reason);
            }
            else if (request.MoveKind == MotionGuardMoveKind.AxisHome)
            {
                // 계측: HOME은 드물게 발생하므로, Dog ON으로 카메라 조건을 통과시킨 사실을 남긴다.
                QMC.Common.Log.Write("Main", "INTERLOCK", "OutputFeederYAvoidDog",
                    "OutputFeederY HOME 카메라 조건을 Avoid Dog ON으로 통과했습니다. " +
                    "카메라를 이동시키지 않습니다. avoidDog=ON" +
                    ", cameraActual=" + cameraX.ActualPosition.ToString("0.###") +
                    ", cameraInAvoid=" + IsOutputVisionXInAvoidPosition(stage) + " - Check");
            }

            // 기존 Auto Load-to-Stage는 제품 전달 후 Lift Up 상태로 FeederY를 Avoid 복귀시킨다.
            // 이 Auto 경로의 Lift Down 강제는 시퀀스 변경 승인이 필요하므로 Manual/HOME에만 신규 적용한다.
            if (request.MoveKind == MotionGuardMoveKind.AxisMove ||
                request.MoveKind == MotionGuardMoveKind.AxisHome)
            {
                if (!feeder.IsOutputFeederSimulationOrDryRun())
                {
                    int downReadError = -1;
                    if (feeder.BinFeederDownSensor == null ||
                        !AjinIoScanService.TryReadHardwareInput(feeder.BinFeederDownSensor, out downReadError))
                    {
                        return MotionGuardRuleHelpers.Block(
                            "OutputFeederY",
                            "OutputFeederY Lift Down 센서 갱신 실패. error=" + downReadError,
                            out reason);
                    }
                }

                if (!feeder.IsFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY Manual/HOME 이동 불가: OutputFeeder Lift가 Down 상태여야 합니다.",
                        out reason);
            }

            if (!feeder.IsOutputFeederSimulationOrDryRun())
            {
                int overloadReadError = -1;
                if (feeder.BinFeederOverloadSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(feeder.BinFeederOverloadSensor, out overloadReadError))
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeeder Overload 센서 갱신 실패. error=" + overloadReadError,
                        out reason);
                }
            }

            if (feeder.IsFeederOverload())
            {
                double tolerance = feeder.FeederY.Config != null && feeder.FeederY.Config.InPositionTolerance > 0.0
                    ? feeder.FeederY.Config.InPositionTolerance
                    : 0.05;
                bool positiveJog = MotionGuardRuleHelpers.IsJogMove(request) &&
                    request.TargetValue > feeder.FeederY.ActualPosition + tolerance;
                if (!positiveJog)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeeder Overload 감지 중에는 +방향 Jog만 허용됩니다. " +
                        "current=" + feeder.FeederY.ActualPosition.ToString("0.###") +
                        ", target=" + request.TargetValue.ToString("0.###"),
                        out reason);
            }

            return true;
        }

        // 인터락 항목: 자동 OutputFeederY 이동은 Vision/Picker/Stage 안전 위치와 Lift/Clamp 상태를 확인한다.
        private static bool CanAutoOutputFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 방어 조건: 장비 참조가 없으면 OutputFeederY 자동 인터락을 적용하지 않는다.
            if (machine == null)
                return true;

            // 인터락 조건: OutputLifterZ가 이동 중이면 OutputFeederY 자동 이동을 차단한다.
            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputLifterZ가 이동 중이라 OutputFeederY 이동이 차단되었습니다.",
                    out reason);

            OutputStageUnit outputStage = machine.OutputStageUnit;
            // 인터락 조건: OutputVisionX가 Avoid 위치가 아니면 OutputFeederY 자동 이동을 차단한다.
            if (!IsOutputVisionXInAvoidPosition(outputStage))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputVisionX가 Avoid 위치가 아니라 OutputFeederY 이동이 차단되었습니다.",
                    out reason);

            string pickerDetail;
            // 인터락 조건: FrontPicker가 Output 존을 점유하거나 위치를 확정할 수 없으면 FeederY 이동을 차단한다.
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Output, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY 이동 차단. FrontPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            // 인터락 조건: RearPicker가 Output 존을 점유하거나 위치를 확정할 수 없으면 FeederY 이동을 차단한다.
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Output, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY 이동 차단. RearPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            OutputFeederUnit feeder = machine.OutputFeederUnit;
            // 방어 조건: Feeder 참조가 없으면 Feeder 센서 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: Feeder 과부하 센서가 감지되면 OutputFeederY 자동 이동을 차단한다.
            if (feeder.IsFeederOverload())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeeder 과부하 센서가 감지되어 OutputFeederY 이동이 차단되었습니다.",
                    out reason);

            return true;
        }

        // 인터락 항목: 수동 OutputFeederY 이동은 Bin 돌출, Vision/Picker/Stage 안전 위치, Lift/Clamp 상태를 확인한다.
        private static bool CanManualOutputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 방어 조건: 장비 참조가 없으면 OutputFeederY 수동 인터락을 적용하지 않는다.
                if (machine == null)
                    return true;

                string pickerDetail;
                // 현재 기준: Manual OutputFeederY 이동 전 FrontPicker가 Output 존에 있으면 차단한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Output, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY 이동 차단. FrontPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                        out reason);

                // 현재 기준: Manual OutputFeederY 이동 전 RearPicker가 Output 존에 있으면 차단한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Output, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY 이동 차단. RearPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                        out reason);

                string axisReason;

                // 인터락 조건: OutputVisionX가 AVOID 상태가 아니면 FeederY 수동 이동을 차단한다.
                if (!IsOutputVisionXInAvoidPosition(machine.OutputStageUnit))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputVisionX must be not homed yet or at Home position. ",
                        out reason);

                // 인터락 조건: FrontPickerX가 홈 준비 상태가 아니면 FeederY 수동 이동을 차단한다.
                //if (!IsFrontPickerXHomeReadyForOutputFeederHome(machine.PickerFrontUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //// 인터락 조건: RearPickerX가 홈 준비 상태가 아니면 FeederY 수동 이동을 차단한다.
                //if (!IsRearPickerXHomeReadyForOutputFeederHome(machine.PickerRearUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                OutputCassetteUnit cassette = machine.OutputCassetteUnit;
                // 인터락 조건: OutputLifterZ가 이동 중이면 OutputFeederY 수동 이동을 차단한다.
                //if (cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.IsMoving)
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputLifterZ is moving. OutputFeederY home is blocked.",
                //        out reason);

                // 인터락 조건: OutputLifterZ가 Avoid 위치가 아니면 OutputFeederY 수동 이동을 차단한다.
                //if (cassette != null && !cassette.IsBinLifterZInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputLifterZ must be at Avoid position.",
                //        out reason);

                OutputStageUnit outputStage = machine.OutputStageUnit;

                // 인터락 조건: GoodStageZ가 Avoid 위치가 아니면 OutputFeederY 수동 이동을 차단한다.
                //if (outputStage != null && outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. GoodBinZ(GoodStageZ) must be at Avoid position.",
                //        out reason);

                //// 인터락 조건: Good Bin Guide가 Down 상태가 아니면 OutputFeederY 수동 이동을 차단한다.
                //if (outputStage != null &&
                //    outputStage.GoodBinGuideDownSensor != null &&
                //    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                //    !outputStage.GoodBinGuideDownSensor.IsOn)
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. Good Bin Guide must be down.",
                //        out reason);

                OutputFeederUnit feeder = machine.OutputFeederUnit;
                // 방어 조건: Feeder 참조가 없으면 Feeder 센서/자재 조건은 적용하지 않는다.
                if (feeder == null)
                    return true;

                // 인터락 조건: Feeder 위에 자재 데이터나 검출 센서가 남아 있으면 홈 계열 이동을 차단한다.
                //if (!VerifyOutputFeederEmptyForHome(feeder, out reason))
                //    return false;

                // 인터락 조건: Feeder 과부하 센서가 감지되면 OutputFeederY 수동 이동을 차단한다.
                //if (feeder.IsFeederOverload())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputFeeder overload sensor is detected.",
                //        out reason);

                // 인터락 조건: 실장비 모드에서 Feeder가 Unclamp 상태가 아니면 수동 이동을 차단한다.
                //if (!ShouldBypassHardwareMechanismChecks() && !IsFeederUnclamp(feeder))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputFeeder must be unclamped.",
                //        out reason);

                // 인터락 조건: 실장비 모드에서 Feeder가 Up 상태가 아니면 수동 이동을 차단한다.
                //if (!ShouldBypassHardwareMechanismChecks() && !IsFeederUp(feeder))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputFeeder must be up.",
                //        out reason);

                // 인터락 조건: 실장비에서 Ring Check가 감지되면 OutputFeederY 수동 이동을 차단한다.
                //if (!feeder.IsOutputFeederSimulationOrDryRun() && feeder.IsBinFeederRingCheck())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputFeeder ring check is detected.",
                //        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "Exception occurred while verifying OutputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: OutputFeederY 홈은 주변 X축 홈 준비, 피커 Output 존 점유, 빈 자재 상태를 확인한다.
        private static bool CanHomeOutputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 방어 조건: 장비 참조가 없으면 OutputFeederY 홈 인터락을 적용하지 않는다.
                if (machine == null)
                    return true;


                string pickerDetail;
                // 현재 기준: Home OutputFeederY는 PickerY가 Home/Avoid이고 Picker X/Y/Z가 정지 상태면 초기 홈 이동을 허용한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransportForFeederHome(machine, true, PickerWorkZone.Output, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME 이동 차단. FrontPicker가 Output zone을 사용 중이거나 Home 안전 상태가 아닙니다. " + pickerDetail,
                        out reason);

                // 현재 기준: Home OutputFeederY는 RearPicker도 동일한 Home 안전 기준을 통과해야 한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransportForFeederHome(machine, false, PickerWorkZone.Output, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME 이동 차단. RearPicker가 Output zone을 사용 중이거나 Home 안전 상태가 아닙니다. " + pickerDetail,
                        out reason);

                // 기존 조건: Home OutputFeederY도 일반 이송처럼 Picker가 Output zone이면 무조건 차단했다.
                // 현재 필요 여부: 사용 안 함. 초기화 순서상 Picker 홈 전 X/Y가 Output으로 잡힐 수 있어 Home 전용 안전 기준을 사용한다.
                //if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Output, out pickerDetail))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY 이동 차단. FrontPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                //        out reason);
                //if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Output, out pickerDetail))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY 이동 차단. RearPicker가 Output zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                //        out reason);

                //string axisReason;
                //if (!IsOutputVisionXHomeReadyForOutputFeederHome(machine.OutputStageUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputVisionX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //if (!IsFrontPickerXHomeReadyForOutputFeederHome(machine.PickerFrontUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //if (!IsRearPickerXHomeReadyForOutputFeederHome(machine.PickerRearUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                OutputCassetteUnit cassette = machine.OutputCassetteUnit;
                // 인터락 조건: OutputLifterZ가 이동 중이면 OutputFeederY 홈 이동을 차단한다.
                if (cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputLifterZ is moving. OutputFeederY home is blocked.",
                        out reason);

                // 카세트가 있으면 이거 봐야하는데...
                //if (cassette != null && !cassette.IsBinLifterZInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputLifterZ must be at Avoid position.",
                //        out reason);

                OutputStageUnit outputStage = machine.OutputStageUnit;

                // 인터락 조건: GoodStageZ가 Home(0) 또는 Avoid 위치가 아니면 OutputFeederY 홈 이동을 차단한다.
                if (outputStage != null && outputStage.GoodStage != null && !IsGoodStageZHomeOrAvoid(outputStage))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. GoodBinZ(GoodStageZ) must be at Home(0) or Avoid position.",
                        out reason);

                // 인터락 조건: Good Bin Guide가 Down 상태가 아니면 OutputFeederY 홈 이동을 차단한다.
                if (outputStage != null &&
                    outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. Good Bin Guide must be down.",
                        out reason);

                OutputFeederUnit feeder = machine.OutputFeederUnit;
                // 방어 조건: Feeder 참조가 없으면 Feeder 센서/자재 조건은 적용하지 않는다.
                if (feeder == null)
                    return true;

                // 인터락 조건: Feeder 위에 자재 데이터나 검출 센서가 남아 있으면 홈 이동을 차단한다.
                if (!VerifyOutputFeederEmptyForHome(feeder, out reason))
                    return false;

                // 인터락 조건: Feeder 과부하 센서가 감지되면 OutputFeederY 홈 이동을 차단한다.
                if (feeder.IsFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder overload sensor is detected.",
                        out reason);

                // 인터락 조건: 실장비 모드에서 Feeder가 Unclamp 상태가 아니면 홈 이동을 차단한다.
                if (!ShouldBypassHardwareMechanismChecks() && !IsFeederUnclamp(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder must be unclamped.",
                        out reason);

                // 기존 조건(사용자 승인으로 비활성): 실장비 OutputFeeder HOME 전에 Lift Up을 요구했다.
                // if (!ShouldBypassHardwareMechanismChecks() && !IsFeederUp(feeder))
                //     return MotionGuardRuleHelpers.Block(
                //         "OutputFeederY",
                //         "OutputFeederY HOME blocked. OutputFeeder must be up.",
                //         out reason);

                // 인터락 조건: 실장비에서 Ring Check가 감지되면 OutputFeederY 홈 이동을 차단한다.
                if (!feeder.IsOutputFeederSimulationOrDryRun() && feeder.IsBinFeederRingCheck())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder ring check is detected.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "Exception occurred while verifying OutputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        #endregion

        #region Lift 실린더

        // 인터락 항목: OutputFeederLift 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyOutputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyOutputFeederLiftMaterialClear(request, out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeOutputFeederLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveOutputFeederLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "Exception occurred while verifying OutputFeederLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 절대 인터락: 기존 Ring/Override 센서 또는 Material 데이터가 있으면 Lift 양방향을 모두 차단한다.
        private static bool VerifyOutputFeederLiftMaterialClear(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            OutputFeederUnit feeder = request != null && request.Machine != null
                ? request.Machine.OutputFeederUnit
                : null;
            return VerifyOutputFeederMaterialClear(feeder, "OutputFeederLift", out reason);
        }

        // 전체 초기화 Preflight와 Lift Guard가 동일한 자재/센서 판정을 사용한다.
        internal static bool VerifyOutputFeederMaterialClear(
            OutputFeederUnit feeder,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            if (feeder == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: OutputFeederUnit 정보가 없습니다.",
                    out reason);

            if (!feeder.IsOutputFeederSimulationOrDryRun())
            {
                if (feeder.BinFeederRingCheckSensor == null)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 절대 인터락 확인 불가: OutputFeeder Ring/Override 센서가 없습니다.",
                        out reason);

                int errorCode;
                if (!AjinIoScanService.TryReadHardwareInput(feeder.BinFeederRingCheckSensor, out errorCode))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 절대 인터락 확인 불가: OutputFeeder Ring/Override 센서 갱신 실패. error=" + errorCode,
                        out reason);
            }

            bool dataEmpty = feeder.IsFeederTransferDataEmpty();
            bool ringDetected = feeder.IsBinFeederRingCheck();
            if (!dataEmpty || ringDetected)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Feeder 자재 데이터 또는 기존 Ring/Override 센서가 감지되었습니다. " +
                    "dataEmpty=" + dataEmpty + ", ring=" + ringDetected,
                    out reason);

            return true;
        }

        // 인터락 항목: OutputFeederLift 초기화는 FeederY 안전 위치, LifterZ, Clamp/자재 상태를 확인한다.
        private static bool CanInitializeOutputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null && IsFeederUnclamp(machine.OutputFeederUnit) == false)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize " + direction + " blocked. OutputFeeder must be unclamped before lift initialize.",
                    out reason);

            if (targetValue >= 0.5 &&
                machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.IsFeederTransferDataOccupied())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize Fwd/Up blocked. OutputFeeder material data exists before lift up.",
                    out reason);

            if (targetValue >= 0.5 &&
                machine.OutputFeederUnit != null &&
                !machine.OutputFeederUnit.IsOutputFeederSimulationOrDryRun() &&
                machine.OutputFeederUnit.IsBinFeederRingCheck())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize Fwd/Up blocked. OutputFeeder bin detect sensor is ON before lift up.",
                    out reason);

            return true;
        }

        // 인터락 항목: OutputFeederLift 이동은 FeederY 안전 위치, LifterZ, Clamp 상태를 확인한다.
        private static bool CanMoveOutputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                IsFeederUnclamp(machine.OutputFeederUnit) &&
                !machine.OutputFeederUnit.IsFeederTransferDataEmpty())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move blocked. OutputFeeder is unclamped, so feeder is assumed to be holding material.",
                    out reason);

            return true;
        }

        #endregion

        #region Clamp 실린더

        // 인터락 항목: OutputFeederClamp 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyOutputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeOutputFeederClamp(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveOutputFeederClamp(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "Exception occurred while verifying OutputFeederClamp cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: OutputFeederClamp 초기화는 일반 Clamp 이동 조건과 동일하게 확인한다.
        private static bool CanInitializeOutputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            return CanMoveOutputFeederClamp(machine, targetValue, out reason);
        }

        // 인터락 항목: OutputFeederClamp 이동은 FeederY 안전 위치, LifterZ, Lift Down 상태를 확인한다.
        private static bool CanMoveOutputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Clamp", "Bwd/Unclamp");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "OutputFeederClamp move " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "OutputFeederClamp move " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            return true;
        }

        #endregion

        #region 연관 장비 및 센서 상태

        private static string ResolveCylinderDirection(double targetValue, string fwdText, string bwdText)
        {
            return targetValue >= 0.5 ? fwdText : bwdText;
        }

        // 인터락 기준: OutputFeederY 홈 전 OutputVisionX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsOutputVisionXHomeReadyForOutputFeederHome(OutputStageUnit stage, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            return MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.OutputCameraX, "OutputVisionX", out reason);
        }

        // 인터락 기준: OutputFeederY 홈 전 FrontPickerX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsFrontPickerXHomeReadyForOutputFeederHome(PickerFrontUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(picker.PickerX, "FrontPickerX", out reason);
        }

        // 인터락 기준: OutputFeederY 홈 전 RearPickerX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsRearPickerXHomeReadyForOutputFeederHome(PickerRearUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(picker.PickerX, "RearPickerX", out reason);
        }

        // 인터락 항목: OutputFeederY 홈 전 Feeder 위 자재 존재 여부와 Vacuum 상태를 확인한다.
        private static bool VerifyOutputFeederEmptyForHome(OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
                if (wafer != null)
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder material data exists. waferId=" +
                        wafer.WaferId + ", state=" + wafer.State,
                        out reason);
                }

                if (feeder != null &&
                    !feeder.IsOutputFeederSimulationOrDryRun() &&
                    feeder.IsBinFeederRingCheck())
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder bin detect sensor is ON while material data is empty.",
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "Exception occurred while checking OutputFeeder material before home: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 기준: OutputFeederY 이동 전 OutputVisionX가 Avoid 위치인지 판단한다.
        private static bool IsOutputVisionXInAvoidPosition(OutputStageUnit stage)
        {
            if (stage == null)
                return true;

            return MotionGuardRuleHelpers.IsAt(stage.OutputCameraX, stage.Recipe.VisionX.AvoidPosition);
        }

        // 인터락 기준: FrontPicker가 Output 존 안에 있는지 판단한다.
        private static bool IsFrontPickerInOutputZone(PickerFrontUnit picker)
        {
            if (picker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (picker.IsFrontPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        // 인터락 기준: RearPicker가 Output 존 안에 있는지 판단한다.
        private static bool IsRearPickerInOutputZone(PickerRearUnit picker)
        {
            if (picker == null)
                return false;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (picker.IsRearPickerInDiePlacePosition(pickerNo))
                    return true;
            }

            return false;
        }

        // 인터락 기준: Output Stage 모듈이 Avoid 위치인지 판단한다.
        private static bool IsStageModuleAtAvoid(StageModule stage)
        {
            return stage == null || stage.IsAtAvoidPosition();
        }

        // 인터락 기준: OutputFeederY 홈 전 GoodStageZ가 Home(0) 또는 Avoid 위치인지 판단한다.
        private static bool IsGoodStageZHomeOrAvoid(OutputStageUnit outputStage)
        {
            if (outputStage == null)
                return true;

            BaseAxis goodStageZ = outputStage.GoodStage != null ? outputStage.GoodStage.StageZ : null;
            if (MotionGuardRuleHelpers.IsAt(goodStageZ, 0.0))
                return true;

            return outputStage.GoodStage == null || outputStage.GoodStage.IsAtAvoidPosition();
        }

        // 인터락 기준: OutputFeeder Lift가 Up 상태인지 센서/실린더 상태로 판단한다.
        private static bool IsFeederUp(OutputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsFeederUp())
                return true;

            BaseCylinder cylinder = feeder.FeederUpDownCyl;
            return cylinder != null && cylinder.IsFwd;
        }

        // 인터락 기준: OutputFeeder Clamp가 Unclamp 상태인지 센서/실린더 상태로 판단한다.
        private static bool IsFeederUnclamp(OutputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsFeederUnclamped())
                return true;

            BaseCylinder cylinder = feeder.FeederClampCyl;
            return cylinder != null && cylinder.IsBwd;
        }

        // 인터락 기준: DryRun 입력은 실제 센서가 없어도 안전 상태로 인정한다.
        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null &&
                   input.Config.IgnoreWaits && input.Config.IsSimulationMode;
        }

        private static bool ShouldBypassHardwareMechanismChecks()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings == null ||
                       settings.BypassHardware ||
                       settings.SimulationMode ||
                       !settings.UseAjin ||
                       !AjinFactory.IsRealBoardReady;
            }
            catch
            {
                return true;
            }
        }

        #endregion

        #region 차단 로그

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputFeederInterlock", reason + " - Blocked");
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
