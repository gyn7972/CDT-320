using System;
using System.Collections.Generic;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    internal enum PickerCoordinateCorrectionPolicy
    {
        // InputVisionToPicker X/Y already uses the collet final position, so Pick X/Y adds runtime only.
        InputPick,
        // Carry moves use runtime + collet for X/Y. T uses runtime only because collet T is handled by home zero.
        CarryRuntimeAndCollet,
        // Permanent calibration uses teaching coordinates without runtime/collet corrections.
        CalibrationNominal,
        // Re-measurement uses saved collet X/Y correction only. T is not added to motion targets.
        CalibrationSavedCollet
    }

    // Centralizes picker X/Y/T target math so auto sequences, manual map moves, and calibration moves use one formula path.
    internal static class PickerMotionTargetResolver
    {
        // Resolves InputVision -> Picker setup offsets, then calculates the input die pick target.
        public static bool TryCalculateInputPickTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double visionAlignOffsetX,
            double visionAlignOffsetY,
            double visionAlignOffsetT,
            bool logFormula,
            out PickCoordinateResult target,
            out string reason,
            double pickRuntimeOffsetX = 0.0,
            double pickRuntimeOffsetY = 0.0,
            double pickRuntimeOffsetT = 0.0,
            bool applyColletEccentricCompensation = false)
        {
            target = null;
            reason = string.Empty;

            double inputVisionToPickerX;
            double inputVisionToPickerY;
            string offsetReason;
            if (!PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                machine,
                side,
                pickerIndex,
                out inputVisionToPickerX,
                out inputVisionToPickerY,
                out offsetReason))
            {
                reason = offsetReason;
                return false;
            }

            target = CalculateInputPickTarget(
                machine,
                side,
                pickerIndex,
                sequenceName,
                dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                visionAlignOffsetX,
                visionAlignOffsetY,
                visionAlignOffsetT,
                logFormula,
                pickRuntimeOffsetX,
                pickRuntimeOffsetY,
                pickRuntimeOffsetT,
                applyColletEccentricCompensation);
            return true;
        }

        // Calculates the picker/input-stage target for picking a die already centered by InputVision.
        // runtimeT는 적용하고 ColletT는 홈 기준 보정이라 로그만 남기고 이동식에는 적용하지 않는다.
        public static PickCoordinateResult CalculateInputPickTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            double visionAlignOffsetX,
            double visionAlignOffsetY,
            double visionAlignOffsetT,
            bool logFormula,
            double pickRuntimeOffsetX = 0.0,
            double pickRuntimeOffsetY = 0.0,
            double pickRuntimeOffsetT = 0.0,
            bool applyColletEccentricCompensation = false)
        {
            double cameraOffsetX;
            double cameraOffsetY;
            InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(
                machine,
                out cameraOffsetX,
                out cameraOffsetY);

            PickerAlignOffset runtime = InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex);
            double runtimeX = runtime != null ? runtime.AlignOffsetX : 0.0;
            double runtimeY = runtime != null ? runtime.AlignOffsetY : 0.0;
            double runtimeT = runtime != null ? runtime.AlignOffsetT : 0.0;
            double colletT = InputPickerPickTargetResolver.ResolveColletTOffset(machine, side, pickerIndex);
            // Collet T offset은 Picker T 홈 기준 보정에 이미 반영되므로 Pick 이동식에는 다시 더하지 않는다.
            // double appliedColletT = colletT;
            double appliedColletT = 0.0;

            // 콜렛 편심 보상은 자동 픽업·수동 맵 이동만 opt-in(true)이고 캘·레시피 이동은 무보상 명목 좌표를 유지한다.
            double colletEccentricCompX = 0.0;
            double colletEccentricCompY = 0.0;
            if (applyColletEccentricCompensation)
            {
                ResolveColletEccentricCompensation(
                    machine,
                    side,
                    pickerIndex,
                    out colletEccentricCompX,
                    out colletEccentricCompY);
            }

            PickCoordinateResult result = DieCoordinateTransformService.CalculatePickTarget(
                sequenceName,
                side,
                pickerIndex,
                string.IsNullOrWhiteSpace(dieId) ? string.Empty : dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                runtimeX,
                runtimeY,
                runtimeT,
                cameraOffsetX,
                cameraOffsetY,
                visionAlignOffsetX,
                visionAlignOffsetY,
                visionAlignOffsetT,
                InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(machine),
                InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetY(machine),
                InputPickerPickTargetResolver.ResolvePickerYPickTeaching(machine, side),
                InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    machine,
                    side,
                    CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex),
                    "PickPosition"),
                InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    machine,
                    side,
                    CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex),
                    "PickPosition"),
                InputPickerPickTargetResolver.ResolveNeedleZPickTarget(machine),
                InputPickerPickTargetResolver.ResolveEjectPinZPickTarget(machine),
                logFormula,
                pickRuntimeOffsetX,
                pickRuntimeOffsetY,
                pickRuntimeOffsetT,
                colletEccentricCompX,
                colletEccentricCompY);

            WriteCoordinateLog(
                "InputPickTarget",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeX=" + F(runtimeX) +
                ", runtimeY=" + F(runtimeY) +
                ", runtimeT=" + F(runtimeT) +
                ", colletTOffset=" + F(colletT) +
                ", colletTAppliedToMove=" + F(appliedColletT) +
                ", inputVisionToPickerX=" + F(inputVisionToPickerX) +
                ", inputVisionToPickerY=" + F(inputVisionToPickerY) +
                ", cameraOffsetX=" + F(cameraOffsetX) +
                ", cameraOffsetY=" + F(cameraOffsetY) +
                ", visionAlignOffsetX=" + F(visionAlignOffsetX) +
                ", visionAlignOffsetY=" + F(visionAlignOffsetY) +
                ", visionAlignOffsetT=" + F(visionAlignOffsetT) +
                ", pickRuntimeOffsetX=" + F(pickRuntimeOffsetX) +
                ", pickRuntimeOffsetY=" + F(pickRuntimeOffsetY) +
                ", pickRuntimeOffsetT=" + F(pickRuntimeOffsetT) +
                ", colletEccentricCompRequested=" + applyColletEccentricCompensation +
                ", colletEccentricCompX=" + F(colletEccentricCompX) +
                ", colletEccentricCompY=" + F(colletEccentricCompY) +
                ", finalStageY=" + F(result.StageY) +
                ", finalPickerX=" + F(result.PickerX) +
                ", finalPickerY=" + F(result.PickerY) +
                ", finalPickerT=" + F(result.PickerT) +
                ", finalNeedleX=" + F(result.NeedleX) +
                ", formula=" + result.Formula);
            return result;
        }

        // Calculates bottom/side/place carry-position targets with runtime and saved collet X/Y corrections applied.
        public static PickerCalibratedZoneTarget ResolveCarryZoneTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            string positionArrayName,
            int pickerIndex)
        {
            return ResolveZoneTarget(
                machine,
                side,
                positionArrayName,
                pickerIndex,
                PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet);
        }

        // Applies the selected correction policy to a taught picker zone position.
        // T always excludes collet theta from move targets because theta is handled by homing zero.
        public static PickerCalibratedZoneTarget ResolveZoneTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            string positionArrayName,
            int pickerIndex,
            PickerCoordinateCorrectionPolicy policy)
        {
            bool includeRuntime = policy == PickerCoordinateCorrectionPolicy.InputPick ||
                                  policy == PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet;
            bool includeCollet = policy == PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet ||
                                 policy == PickerCoordinateCorrectionPolicy.CalibrationSavedCollet;

            PickerCalibratedZoneTarget target = CalibrationCoordinateService.ResolvePickerZoneTarget(
                machine,
                ToVisionFocusPickerSide(side),
                positionArrayName,
                pickerIndex,
                InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex),
                includeRuntime,
                includeCollet);

            WriteCoordinateLog(
                "PickerZoneTarget",
                "side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", positionArrayName=" + (positionArrayName ?? string.Empty) +
                ", policy=" + policy +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeX=" + F(target.RuntimeOffsetX) +
                ", runtimeY=" + F(target.RuntimeOffsetY) +
                ", runtimeT=" + F(target.RuntimeOffsetT) +
                ", colletX=" + F(target.ColletOffsetX) +
                ", colletY=" + F(target.ColletOffsetY) +
                ", colletTAppliedToMove=" + F(target.ColletOffsetT) +
                ", finalX=" + F(target.X) +
                ", finalY=" + F(target.Y) +
                ", finalT=" + F(target.T) +
                ", finalZ=" + F(target.Z) +
                ", formula=" + target.Formula);
            return target;
        }

        // Formats a corrected zone target by axis so operators can compare each correction source.
        public static string FormatZoneTargetByAxis(PickerCalibratedZoneTarget target, string lineBreak)
        {
            if (target == null)
                return string.Empty;

            string br = string.IsNullOrEmpty(lineBreak) ? System.Environment.NewLine : lineBreak;
            return
                "X축: teachingX(" + F(target.TeachingX) + ") + pitchX(" + F(target.PitchOffsetX) +
                ") + runtimeX(" + F(target.RuntimeOffsetX) + ") + colletX(" + F(target.ColletOffsetX) +
                ") = " + F(target.X) + " mm" + br +
                "Y축: teachingY(" + F(target.TeachingY) + ") + runtimeY(" + F(target.RuntimeOffsetY) +
                ") + colletY(" + F(target.ColletOffsetY) + ") = " + F(target.Y) + " mm" + br +
                "T축: teachingT(" + F(target.TeachingT) + ") + runtimeT(" + F(target.RuntimeOffsetT) +
                ") + colletT(homeZeroApplied)(" + F(target.ColletOffsetT) + ") = " + F(target.T) + " deg" + br +
                "Z축: teachingZ(" + F(target.TeachingZ) + ") = " + F(target.Z) + " mm";
        }

        // Calculates the output-stage place target using the same carried picker correction values as pickup.
        public static PlaceCoordinateResult CalculateOutputPlaceTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            BinSide targetSide,
            double outputStageBaseY,
            double receiveTargetX,
            double receiveTargetY,
            double outputVisionProcessX,
            double outputVisionToPickerX,
            double outputVisionToPickerY,
            double bottomOffsetX = 0.0,
            double bottomOffsetY = 0.0,
            double bottomOffsetT = 0.0,
            double placeRuntimeOffsetX = 0.0,
            double placeRuntimeOffsetY = 0.0,
            double placeRuntimeOffsetT = 0.0,
            double placeMechanicalOffsetX = 0.0,
            double placeMechanicalOffsetY = 0.0,
            bool bottomFinalItemOffsetYIsSoleColletYCorrection = false,
            double placeMechanicalOffsetT = 0.0)
        {
            PickerAlignOffset runtime = InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex);
            PickerCalibrationOffset collet = ResolveColletOffset(machine, side, pickerIndex);
            double runtimeOffsetX = runtime != null ? runtime.AlignOffsetX : 0.0;
            double runtimeOffsetY = runtime != null ? runtime.AlignOffsetY : 0.0;
            double runtimeOffsetT = runtime != null ? runtime.AlignOffsetT : 0.0;
            double colletOffsetX = collet != null ? collet.X : 0.0;
            double colletOffsetY = collet != null ? collet.Y : 0.0;
            double pickerYTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                PickerAxis.PickerY,
                "PlacePosition");
            double pickerTTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex),
                "PlacePosition");
            double pickerZTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex),
                "PlacePosition");
            PlaceCoordinateResult result = DieCoordinateTransformService.CalculatePlaceTarget(
                sequenceName,
                side,
                pickerIndex,
                string.IsNullOrWhiteSpace(dieId) ? string.Empty : dieId,
                targetSide,
                outputStageBaseY,
                receiveTargetY,
                outputVisionProcessX,
                receiveTargetX,
                outputVisionToPickerX,
                outputVisionToPickerY,
                colletOffsetY,
                runtimeOffsetX,
                runtimeOffsetY,
                pickerYTeaching,
                pickerTTeaching,
                runtimeOffsetT,
                pickerZTeaching,
                bottomOffsetX,
                bottomOffsetY,
                bottomOffsetT,
                placeRuntimeOffsetX,
                placeRuntimeOffsetY,
                placeRuntimeOffsetT,
                placeMechanicalOffsetX,
                placeMechanicalOffsetY,
                bottomFinalItemOffsetYIsSoleColletYCorrection,
                placeMechanicalOffsetT);

            WriteCoordinateLog(
                "OutputPlaceTarget",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", targetSide=" + targetSide +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeOffsetX=" + F(runtimeOffsetX) +
                ", runtimeOffsetY=" + F(runtimeOffsetY) +
                ", runtimeT=" + F(runtimeOffsetT) +
                ", colletXAlreadyInOutputVisionToPicker=" + F(colletOffsetX) +
                ", colletYAlreadyInOutputVisionToPicker=" + F(colletOffsetY) +
                ", colletTAppliedToMove=0.000000" +
                ", outputStageBaseY=" + F(outputStageBaseY) +
                ", receiveTargetX=" + F(receiveTargetX) +
                ", receiveTargetY=" + F(receiveTargetY) +
                ", outputVisionProcessX=" + F(outputVisionProcessX) +
                ", outputVisionToPickerX=" + F(outputVisionToPickerX) +
                ", outputVisionToPickerY=" + F(outputVisionToPickerY) +
                ", bottomOffsetX=" + F(bottomOffsetX) +
                ", bottomOffsetY=" + F(bottomOffsetY) +
                ", bottomOffsetT=" + F(bottomOffsetT) +
                ", placeRuntimeOffsetX=" + F(placeRuntimeOffsetX) +
                ", placeRuntimeOffsetY=" + F(placeRuntimeOffsetY) +
                ", placeRuntimeOffsetT=" + F(placeRuntimeOffsetT) +
                ", placeMechanicalOffsetX=" + F(placeMechanicalOffsetX) +
                ", placeMechanicalOffsetY=" + F(placeMechanicalOffsetY) +
                ", pickerYTeaching=" + F(pickerYTeaching) +
                ", pickerTTeaching=" + F(pickerTTeaching) +
                ", pickerZTeaching=" + F(pickerZTeaching) +
                ", finalOutputStageY=" + F(result.OutputStageY) +
                ", finalPickerX=" + F(result.PickerX) +
                ", finalPickerY=" + F(result.PickerY) +
                ", finalPickerT=" + F(result.PickerT) +
                ", formula=" + result.Formula);

            WriteCoordinateLog(
                "OutputPlaceFormula",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", targetSide=" + targetSide +
                ", formulaPickerX=outputVisionProcessX(" + F(outputVisionProcessX) +
                ")+receiveTargetX(" + F(receiveTargetX) +
                ")+outputVisionToPickerX(" + F(outputVisionToPickerX) +
                ")+runtimeOffsetX(" + F(runtimeOffsetX) +
                ")-bottomOffsetX(" + F(bottomOffsetX) +
                ")-placeRuntimeOffsetX(" + F(placeRuntimeOffsetX) +
                ")+placeMechanicalOffsetX(" + F(placeMechanicalOffsetX) +
                ")=" + F(result.PickerX) +
                ", colletXAlreadyInOutputVisionToPicker=" + F(colletOffsetX) +
                ", colletXNotAddedAgain=True" +
                ", pickerXIfColletDoubleAdded=" + F(result.PickerX + colletOffsetX) +
                ", outputCameraToPickerY=outputVisionToPickerY(" + F(outputVisionToPickerY) +
                ")-pickerYTeaching(" + F(pickerYTeaching) +
                ")=" + F(outputVisionToPickerY - pickerYTeaching) +
                ", formulaOutputStageY=outputStageBaseY(" + F(outputStageBaseY) +
                ")+receiveTargetY(" + F(receiveTargetY) +
                ")+outputCameraToPickerY(" + F(bottomFinalItemOffsetYIsSoleColletYCorrection ? 0.0 : outputVisionToPickerY - pickerYTeaching) +
                ")-bottomOffsetY(" + F(bottomOffsetY) +
                ")-placeRuntimeOffsetY(" + F(placeRuntimeOffsetY) +
                ")+placeMechanicalOffsetY(" + F(placeMechanicalOffsetY) +
                ")=" + F(result.OutputStageY) +
                ", runtimeOffsetYLoggedOnly=" + F(runtimeOffsetY) +
                ", colletYAlreadyInOutputVisionToPicker=" + F(colletOffsetY) +
                ", outputVisionToPickerYAppliedToOutputStageY=" + (!bottomFinalItemOffsetYIsSoleColletYCorrection) +
                ", BottomFinalItemOffsetYIsSoleColletYCorrection=" + bottomFinalItemOffsetYIsSoleColletYCorrection +
                ", pickerYFixed=" + F(result.PickerY) +
                ", pickerT=placeTeachingT(" + F(pickerTTeaching) +
                ")-bottomOffsetT(" + F(bottomOffsetT) +
                ")-placeRuntimeOffsetT(" + F(placeRuntimeOffsetT) +
                ")+placeMechanicalOffsetT(" + F(placeMechanicalOffsetT) +
                ")=" + F(result.PickerT) +
                ", pickerZ=placeTeachingZ(" + F(pickerZTeaching) +
                ")=" + F(result.PickerZ) +
                " - Calc");
            return result;
        }

        // ===== 콜렛 회전중심·콜렛원점 편심 픽업 XY 보상 (2026-08-25 팀장님 지시) =====
        // 물리: 콜렛 캘(원점 O)은 바텀 촬영각 상태에서 측정된 좌표라, 픽업각에서는 콜렛 중심이
        // 회전중심 C 둘레 원호만큼 이동해 있다. ΔP=(I−R(Δθ))·(C−O)를 픽 목표 PickerX/PickerY에
        // 가산해 원인 단계에서 제거한다(|Δθ|≈180°에서 ΔP=2(C−O)).
        // 프레임: T+ 지령=물리 CW(2026-08-18 실장비 확정) → R(θ)=[[cos,+sin],[−sin,cos]].
        //   비180° 일반화는 R의 X행 sin 부호가 실장비 미검증이라 각도 게이트로 차단한다.
        // Δθ는 티칭값만 사용: PickPosition − BottomPosition(존 DieBottomPosition의 T 매핑,
        //   CalibrationCoordinateService.ResolveZonePositionName과 동일). 런타임 T(±0.45°)는
        //   |e| 수십 µm에서 sin(0.45°)≈0.008배라 무시. record.MeasuredTPosition은 T홈 제로 이전
        //   좌표계라 프레임이 섞이므로 사용 금지.
        // 편심은 반드시 레코드 조합 C−O로 계산한다 — RotationCenterPixel 잔차 방식은 구C−신C
        //   오염이라 금지.
        // 부호 유보: 2(C−O) 방향은 실장비 보상 OFF/ON 1런으로 확정하고, 반대로 확인되면 팀장님
        //   보고 후 부호만 뒤집는다(구조 변경 금지).
        private const double ColletEccentricMaxDeltaThetaDeviationDeg = 5.0;
        private static readonly object ColletEccentricLogLock = new object();
        // 폴백 사유 도배 방지(지시서 §5-3): 콜렛(side+picker)당 마지막 폴백 사유를 기억해
        // 상태가 바뀔 때만 1줄 남긴다. 보상 적용으로 복귀하면 상태를 지워 재폴백 시 다시 남긴다.
        private static readonly Dictionary<string, string> ColletEccentricLastFallbackReasons =
            new Dictionary<string, string>();

        private static void ResolveColletEccentricCompensation(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            out double compX,
            out double compY)
        {
            compX = 0.0;
            compY = 0.0;

            double centerX = 0.0;
            double centerY = 0.0;
            double originX = 0.0;
            double originY = 0.0;
            double eccentricX = 0.0;
            double eccentricY = 0.0;
            double thetaPickTeaching = 0.0;
            double thetaCalTeaching = 0.0;
            double deltaTheta = 0.0;
            double candidateX = 0.0;
            double candidateY = 0.0;
            double limit = PickerPickUpMotionConfig.DefaultColletEccentricCompensationLimitMm;
            string gateReason = null;

            try
            {
                PickerPickUpMotionConfig pickUp = null;
                double[] rotationCenterX = null;
                double[] rotationCenterY = null;
                bool[] rotationCenterValid = null;
                if (machine == null)
                {
                    gateReason = "machine-null";
                }
                else if (side == PickerSequenceSide.Front &&
                         machine.PickerFrontUnit != null && machine.PickerFrontUnit.Config != null)
                {
                    pickUp = machine.PickerFrontUnit.Config.PickUp;
                    rotationCenterX = machine.PickerFrontUnit.Config.ColletRotationCenterX;
                    rotationCenterY = machine.PickerFrontUnit.Config.ColletRotationCenterY;
                    rotationCenterValid = machine.PickerFrontUnit.Config.ColletRotationCenterValid;
                }
                else if (side == PickerSequenceSide.Rear &&
                         machine.PickerRearUnit != null && machine.PickerRearUnit.Config != null)
                {
                    pickUp = machine.PickerRearUnit.Config.PickUp;
                    rotationCenterX = machine.PickerRearUnit.Config.ColletRotationCenterX;
                    rotationCenterY = machine.PickerRearUnit.Config.ColletRotationCenterY;
                    rotationCenterValid = machine.PickerRearUnit.Config.ColletRotationCenterValid;
                }
                else
                {
                    gateReason = "picker-unit-null";
                }

                if (gateReason == null && pickUp == null)
                    gateReason = "pickup-config-null";
                if (gateReason == null && !pickUp.UsePickRotationCenterCompensation)
                    gateReason = "disabled";
                if (gateReason == null)
                    limit = PickerPickUpMotionConfig.NormalizeColletEccentricCompensationLimit(
                        pickUp.ColletEccentricCompensationLimitMm);
                if (gateReason == null &&
                    (rotationCenterX == null || rotationCenterY == null || rotationCenterValid == null ||
                     pickerIndex < 0 ||
                     pickerIndex >= rotationCenterX.Length ||
                     pickerIndex >= rotationCenterY.Length ||
                     pickerIndex >= rotationCenterValid.Length))
                    gateReason = "rotation-center-array-invalid";
                if (gateReason == null && !rotationCenterValid[pickerIndex])
                    gateReason = "rotation-center-invalid";

                ColletCalibrationRecord record = null;
                if (gateReason == null)
                {
                    record = machine.VisionUnit != null &&
                             machine.VisionUnit.Config != null &&
                             machine.VisionUnit.Config.CalibrationData != null &&
                             machine.VisionUnit.Config.CalibrationData.Collet != null
                        ? machine.VisionUnit.Config.CalibrationData.Collet.GetRecord(
                            ToVisionFocusPickerSide(side), pickerIndex + 1)
                        : null;
                    if (record == null)
                        gateReason = "collet-record-null";
                }

                // 콜렛 캘 Valid 게이트(2026-08-25 팀장님 승인 확장): 미캘 레코드는 UpdatedAt이
                // 안전 초기값(2000-01-01)이라 세대 게이트를 통과해 버리므로 Valid를 함께 본다.
                if (gateReason == null && !record.Valid)
                    gateReason = "collet-cal-invalid";
                // 캘 세대 정합: 콜렛 캘만 재실행하고 COC를 안 돌리면 O만 갱신되어 e=C−O가 세대
                // 혼합으로 오염된다. COC 저장 시각이 콜렛 캘 저장 시각 이상일 때만 통과.
                if (gateReason == null && record.RotationCenterUpdatedAt < record.UpdatedAt)
                    gateReason = "calibration-generation-mismatch";

                if (gateReason == null)
                {
                    centerX = rotationCenterX[pickerIndex];
                    centerY = rotationCenterY[pickerIndex];
                    originX = record.FinalPickerX;
                    originY = record.FinalPickerY;
                    eccentricX = centerX - originX;
                    eccentricY = centerY - originY;

                    PickerAxis tAxis = CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex);
                    thetaPickTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                        machine, side, tAxis, "PickPosition");
                    thetaCalTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                        machine, side, tAxis, "BottomPosition");
                    deltaTheta = NormalizeDegreesPlusMinus180(thetaPickTeaching - thetaCalTeaching);
                    if (Math.Abs(Math.Abs(deltaTheta) - 180.0) > ColletEccentricMaxDeltaThetaDeviationDeg)
                        gateReason = "delta-theta-out-of-band";
                }

                if (gateReason == null)
                {
                    double rad = deltaTheta * Math.PI / 180.0;
                    double cos = Math.Cos(rad);
                    double sin = Math.Sin(rad);
                    candidateX = (1.0 - cos) * eccentricX - sin * eccentricY;
                    candidateY = sin * eccentricX + (1.0 - cos) * eccentricY;
                    if (Math.Abs(candidateX) > limit || Math.Abs(candidateY) > limit)
                        gateReason = "magnitude-over-limit";
                }
            }
            catch (Exception ex)
            {
                gateReason = "exception:" + ex.GetType().Name;
            }

            string values =
                "side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", C=(" + F(centerX) + "," + F(centerY) + ")" +
                ", O=(" + F(originX) + "," + F(originY) + ")" +
                ", e=C-O=(" + F(eccentricX) + "," + F(eccentricY) + ")" +
                ", thetaPickTeach=" + F(thetaPickTeaching) +
                ", thetaCalTeach=" + F(thetaCalTeaching) +
                ", deltaTheta=" + F(deltaTheta) + "(티칭 기준, ±360 정규화)" +
                ", deltaP=(" + F(candidateX) + "," + F(candidateY) + ")" +
                ", limit=" + F(limit);

            if (gateReason == null)
            {
                compX = candidateX;
                compY = candidateY;
                ClearColletEccentricFallbackState(side, pickerIndex);
                EventLogger.Write(EventKind.Event, "COORD", "PICK-COC-COMP",
                    "콜렛 편심 픽 보상 적용. " + values + ", gate=pass, formula=deltaP=(I-R(deltaTheta))*(C-O)");
            }
            else
            {
                WriteColletEccentricFallbackLog(side, pickerIndex, gateReason, values);
            }
        }

        private static void WriteColletEccentricFallbackLog(
            PickerSequenceSide side,
            int pickerIndex,
            string gateReason,
            string values)
        {
            string key = side + ":" + pickerIndex;
            lock (ColletEccentricLogLock)
            {
                string lastReason;
                if (ColletEccentricLastFallbackReasons.TryGetValue(key, out lastReason) &&
                    string.Equals(lastReason, gateReason, StringComparison.Ordinal))
                    return;
                ColletEccentricLastFallbackReasons[key] = gateReason;
            }

            // 크기 게이트 초과는 측정 불량/구값 방호라 Warning으로 승격한다(지시서 §2 게이트 5).
            EventKind kind = string.Equals(gateReason, "magnitude-over-limit", StringComparison.Ordinal)
                ? EventKind.Warning
                : EventKind.Event;
            EventLogger.Write(kind, "COORD", "PICK-COC-COMP",
                "콜렛 편심 픽 보상 0 폴백. reason=" + gateReason + ", " + values +
                " (같은 사유 반복은 콜렛당 상태 변화 시에만 기록)");
        }

        private static void ClearColletEccentricFallbackState(PickerSequenceSide side, int pickerIndex)
        {
            string key = side + ":" + pickerIndex;
            lock (ColletEccentricLogLock)
            {
                ColletEccentricLastFallbackReasons.Remove(key);
            }
        }

        private static double NormalizeDegreesPlusMinus180(double degrees)
        {
            double normalized = degrees % 360.0;
            if (normalized > 180.0)
                normalized -= 360.0;
            else if (normalized <= -180.0)
                normalized += 360.0;
            return normalized;
        }

        private static PickerCalibrationOffset ResolveColletOffset(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            return CalibrationCoordinateService.ResolvePickerCalibrationOffset(
                machine,
                ToVisionFocusPickerSide(side),
                pickerIndex);
        }

        private static VisionFocusPickerSide ToVisionFocusPickerSide(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
        }

        // Writes coordinate calculation values before motion so field logs can compare formula target and final axis position.
        private static void WriteCoordinateLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message + " - Calc");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }

        private static int ToPickerNo(int pickerIndex)
        {
            return pickerIndex + 1;
        }
    }
}
