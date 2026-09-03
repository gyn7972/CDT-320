using System;
using System.Text;

namespace QMC.CDT320.Calibration
{
    public static class PickerVisionOffsetCalibrationService
    {
        public static bool TryApplyAvailableOffsets(CDT320_Machine machine, string updatedBy, out string summary)
        {
            summary = string.Empty;
            try
            {
                if (machine == null)
                {
                    summary = "machine is null.";
                    return false;
                }

                CalibrationData data = CalibrationCoordinateService.ResolveData(machine);
                if (data == null || data.Camera == null || data.Collet == null)
                {
                    summary = "calibration data is missing.";
                    return false;
                }

                data.Camera.EnsureObjects();
                data.Collet.EnsureObjects();
                if (!data.Camera.Valid && !CanUseStoredCameraOffsets(data.Camera, out summary))
                {
                    summary = "Vision Camera Calibration is not valid. " + summary;
                    return false;
                }

                if (!data.Camera.InputReticle.HasVisionXPosition || !data.Camera.OutputReticle.HasVisionXPosition)
                {
                    summary = "Input/Output reticle VisionX encoder position is missing.";
                    return false;
                }

                string bridgeReason;
                if (!CanUseReticleCameraBridge(data.Camera, out bridgeReason))
                {
                    summary = "Camera bridge(reticle Mm) is not usable. " + bridgeReason;
                    return false;
                }

                StringBuilder sb = new StringBuilder();
                int count = 0;
                count += ApplySide(
                    machine.PickerFrontUnit,
                    data.Collet.FrontCollets,
                    VisionFocusPickerSide.Front,
                    data.Camera,
                    sb);
                count += ApplySide(
                    machine.PickerRearUnit,
                    data.Collet.RearCollets,
                    VisionFocusPickerSide.Rear,
                    data.Camera,
                    sb);

                if (count <= 0)
                {
                    summary = "No valid Collet Calibration records. Skip VisionToPicker offset update.";
                    return false;
                }

                data.Touch(updatedBy);
                summary = "Picker VisionToPicker offset update complete. count=" + count + ". " + sb;
                if (!data.Camera.Valid)
                    summary = "Vision Camera Calibration Valid=false; stored reticle/offset values were used. " + summary;
                QMC.Common.Log.Write("Calibration", updatedBy ?? "SYSTEM", "PickerVisionOffsetApply", summary);
                return true;
            }
            catch (Exception ex)
            {
                summary = "Picker VisionToPicker offset update exception: " + ex.Message;
                QMC.Common.Log.Write("Calibration", updatedBy ?? "SYSTEM", "PickerVisionOffsetApplyFail", summary);
                return false;
            }
            finally
            {
            }
        }

        private static bool CanUseStoredCameraOffsets(VisionCameraCalibrationData camera, out string reason)
        {
            reason = string.Empty;
            if (camera == null)
            {
                reason = "camera is null.";
                return false;
            }

            camera.EnsureObjects();
            if (camera.InputReticle == null || !camera.InputReticle.HasVisionXPosition)
            {
                reason = "Input reticle VisionX encoder position is missing.";
                return false;
            }

            if (camera.OutputReticle == null || !camera.OutputReticle.HasVisionXPosition)
            {
                reason = "Output reticle VisionX encoder position is missing.";
                return false;
            }

            if (!IsFinite(camera.InputReticle.VisionXPosition) ||
                !IsFinite(camera.OutputReticle.VisionXPosition) ||
                !IsFinite(camera.InputToBottomOffsetX) ||
                !IsFinite(camera.InputToBottomOffsetY) ||
                !IsFinite(camera.OutputToBottomOffsetX) ||
                !IsFinite(camera.OutputToBottomOffsetY))
            {
                reason = "stored camera offset contains invalid number.";
                return false;
            }

            reason = "OK";
            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        // 카메라 브리지는 Bottom/Input 레티클의 Mm 측정값을 직접 쓰므로, 측정이 초기화·구버전이면
        // 조용히 0 브리지(= 0.55mm 오차)로 진행하지 않고 중단한다.
        private static bool CanUseReticleCameraBridge(VisionCameraCalibrationData camera, out string reason)
        {
            reason = string.Empty;
            if (camera == null || camera.BottomReticle == null || camera.InputReticle == null || camera.OutputReticle == null)
            {
                reason = "bottom/input/output reticle measurement is missing.";
                return false;
            }

            if (!IsFinite(camera.BottomReticle.MmX) || !IsFinite(camera.BottomReticle.MmY) ||
                !IsFinite(camera.InputReticle.MmX) || !IsFinite(camera.InputReticle.MmY) ||
                !IsFinite(camera.OutputReticle.MmX) || !IsFinite(camera.OutputReticle.MmY))
            {
                reason = "reticle Mm value contains invalid number.";
                return false;
            }

            // 저장 Input/OutputToBottomOffset은 VisionCameraCalibrationData의 브리지 산식으로 계산돼 있어야 한다.
            // 산식이 바뀐 뒤 CALC/SAVE를 안 한 상태로 가산하면 상수 오차가 생기므로 여기서 차단한다.
            const double toleranceMm = 0.001;
            double inputBridgeX = camera.ResolveInputBridgeX();
            double inputBridgeY = camera.ResolveInputBridgeY();
            if (Math.Abs(inputBridgeX - camera.InputToBottomOffsetX) > toleranceMm ||
                Math.Abs(inputBridgeY - camera.InputToBottomOffsetY) > toleranceMm)
            {
                reason = "stored InputToBottomOffset is not the current pick-bridge value. expectedBridge=(" +
                         inputBridgeX.ToString("F6") + "," + inputBridgeY.ToString("F6") + "), storedOffset=(" +
                         camera.InputToBottomOffsetX.ToString("F6") + "," + camera.InputToBottomOffsetY.ToString("F6") +
                         ") — 구버전 산식 값입니다. Vision Camera Calibration(CALC/SAVE)을 다시 실행하세요.";
                return false;
            }

            double outputBridgeX = camera.ResolveOutputBridgeX();
            double outputBridgeY = camera.ResolveOutputBridgeY();
            if (Math.Abs(outputBridgeX - camera.OutputToBottomOffsetX) > toleranceMm ||
                Math.Abs(outputBridgeY - camera.OutputToBottomOffsetY) > toleranceMm)
            {
                reason = "stored OutputToBottomOffset is not the current place-bridge value. expectedBridge=(" +
                         outputBridgeX.ToString("F6") + "," + outputBridgeY.ToString("F6") + "), storedOffset=(" +
                         camera.OutputToBottomOffsetX.ToString("F6") + "," + camera.OutputToBottomOffsetY.ToString("F6") +
                         ") — 구버전 산식 값입니다. Vision Camera Calibration(CALC/SAVE)을 다시 실행하세요.";
                return false;
            }

            reason = "OK";
            return true;
        }

        private static int ApplySide(
            object pickerUnit,
            ColletCalibrationRecord[] records,
            VisionFocusPickerSide side,
            VisionCameraCalibrationData camera,
            StringBuilder summary)
        {
            PickerVisionCoordinateOffsets inputOffsets;
            PickerVisionCoordinateOffsets outputOffsets;
            if (!TryResolveOffsetObjects(pickerUnit, out inputOffsets, out outputOffsets))
                return 0;

            int count = 0;
            if (records == null)
                return count;

            inputOffsets.EnsureArrays();
            outputOffsets.EnsureArrays();

            for (int i = 0; i < records.Length && i < 4; i++)
            {
                ColletCalibrationRecord record = records[i];
                if (record == null || !record.Valid)
                    continue;

                // 저장 offset은 자동/수동 Pick 계산식에서 그대로 쓰는 최종 Vision->Picker 보정값이다.
                // Bottom-Input Offset(카메라 브리지)은 콜렛계와 다이계를 잇는 유일한 다리다:
                //   FinalPickerX/Y = Bottom 카메라 라인 기준(콜렛 캘), Die/Needle = Input 카메라 라인 기준(다이맵·니들 캘).
                // 두 카메라 라인의 물리적 간격을 여기서 딱 1회 반영해야 콜렛이 다이 위에 정확히 온다.
                // 반영 위치는 PickerX/PickerY 전용인 이 저장값이며, StageY/NeedleX/맵 원점에는 절대 들어가지 않는다
                // (라이브 Vision 가산 방식은 축이 갈려 상쇄가 깨지는 이중 적용 버그 — ApplyBottomReferenceOffset Wafer 제거로 차단됨).
                // 부호는 소스(VisionCameraCalibrationData.Calculate)에서 이미 브리지 정의로 저장된다:
                //   InputToBottomOffset = -(Bottom.Mm + Input.Mm)  — Bottom 상방 카메라의 축 반전 반영.
                //   오늘값 X = -0.556500 (기존 수동 MechanicalOffsetX -0.58과 오차 0.024), Y = -0.261239 (Bottom 잔차 0.245와 크기 일치).
                // 여기서는 변환 없이 그대로 가산(+)만 한다.
                // 기구 보정(MechanicalOffsetX)은 PickerX/NeedleX에 동시 적용되어 니들까지 틀어놓으므로,
                // 카메라 브리지는 반드시 PickerX/PickerY 전용인 이 저장값에 넣고 MechanicalOffsetX는 0으로 되돌린다.
                double cameraBridgeX = camera.InputToBottomOffsetX;
                double cameraBridgeY = camera.InputToBottomOffsetY;
                double inputX = record.FinalPickerX - camera.InputReticle.VisionXPosition + cameraBridgeX;
                double inputY = record.FinalPickerY + cameraBridgeY;
                // Output도 동일 규약: 저장값이 브리지 -(Bottom.Mm + Output.Mm)이므로 변환 없이 그대로 가산(+).
                double outputFinalPickerX = record.FinalPickerX;
                double outputX = outputFinalPickerX - camera.OutputReticle.VisionXPosition + camera.OutputToBottomOffsetX;
                double outputY = record.FinalPickerY + camera.OutputToBottomOffsetY;

                inputOffsets.OffsetX[i] = inputX;
                inputOffsets.OffsetY[i] = inputY;
                outputOffsets.OffsetX[i] = outputX;
                outputOffsets.OffsetY[i] = outputY;
                count++;

                LogAppliedOffset(side, i, record, camera, inputX, inputY, outputX, outputY, outputFinalPickerX);
                if (summary != null)
                {
                    summary.Append(side).Append(" C").Append(i + 1)
                        .Append(" input=(").Append(inputX.ToString("F3")).Append(",").Append(inputY.ToString("F3")).Append(")")
                        .Append(" output=(").Append(outputX.ToString("F3")).Append(",").Append(outputY.ToString("F3")).Append("); ");
                }
            }

            return count;
        }

        private static bool TryResolveOffsetObjects(
            object pickerUnit,
            out PickerVisionCoordinateOffsets inputOffsets,
            out PickerVisionCoordinateOffsets outputOffsets)
        {
            inputOffsets = null;
            outputOffsets = null;

            PickerFrontUnit front = pickerUnit as PickerFrontUnit;
            if (front != null && front.Setup != null)
            {
                front.Setup.EnsureGeometryData();
                inputOffsets = front.Setup.InputVisionToPicker;
                outputOffsets = front.Setup.OutputVisionToPicker;
                return inputOffsets != null && outputOffsets != null;
            }

            PickerRearUnit rear = pickerUnit as PickerRearUnit;
            if (rear != null && rear.Setup != null)
            {
                rear.Setup.EnsureGeometryData();
                inputOffsets = rear.Setup.InputVisionToPicker;
                outputOffsets = rear.Setup.OutputVisionToPicker;
                return inputOffsets != null && outputOffsets != null;
            }

            return false;
        }

        private static void LogAppliedOffset(
            VisionFocusPickerSide side,
            int pickerIndex,
            ColletCalibrationRecord record,
            VisionCameraCalibrationData camera,
            double inputX,
            double inputY,
            double outputX,
            double outputY,
            double outputFinalPickerX)
        {
            QMC.Common.Log.Write("Calibration", "SYSTEM", "PickerVisionOffsetFormula",
                "VisionToPicker offset calculated. side=" + side +
                ", pickerNo=" + (pickerIndex + 1) +
                ", finalPicker=(" + record.FinalPickerX.ToString("F6") + "," + record.FinalPickerY.ToString("F6") + ")" +
                ", outputFinalPickerX=" + outputFinalPickerX.ToString("F6") +
                ", outputXMode=ColletFinalPickerX" +
                ", outputYMode=PickBasisPlusPlaceBridge" +
                ", inputReticleVisionX=" + camera.InputReticle.VisionXPosition.ToString("F6") +
                ", outputReticleVisionX=" + camera.OutputReticle.VisionXPosition.ToString("F6") +
                ", inputCameraOffset=(" + camera.InputToBottomOffsetX.ToString("F6") + "," + camera.InputToBottomOffsetY.ToString("F6") + ")" +
                ", outputCameraOffset=(" + camera.OutputToBottomOffsetX.ToString("F6") + "," + camera.OutputToBottomOffsetY.ToString("F6") + ")" +
                ", reticleMmBottom=(" + camera.BottomReticle.MmX.ToString("F6") + "," + camera.BottomReticle.MmY.ToString("F6") + ")" +
                ", reticleMmInput=(" + camera.InputReticle.MmX.ToString("F6") + "," + camera.InputReticle.MmY.ToString("F6") + ")" +
                ", cameraBridge=storedInputToBottomOffset=(" +
                camera.InputToBottomOffsetX.ToString("F6") + "," +
                camera.InputToBottomOffsetY.ToString("F6") + ")" +
                ", formulaInputX=finalPickerX-inputVisionX+cameraBridgeX=" +
                record.FinalPickerX.ToString("F6") + "-" +
                camera.InputReticle.VisionXPosition.ToString("F6") + "+(" +
                camera.InputToBottomOffsetX.ToString("F6") + ")=" + inputX.ToString("F6") +
                ", formulaInputY=finalPickerY+cameraBridgeY=" +
                record.FinalPickerY.ToString("F6") + "+(" +
                camera.InputToBottomOffsetY.ToString("F6") + ")=" + inputY.ToString("F6") +
                ", cameraBridgeAppliedOnceToPickerXYOnly=True(NotToStageY/NeedleX/DieMap)" +
                ", inputOutputSameBridgeConvention=True" +
                ", formulaOutputX=outputFinalPickerX-outputVisionX+placeBridgeX=" +
                outputFinalPickerX.ToString("F6") + "-" +
                camera.OutputReticle.VisionXPosition.ToString("F6") + "+(" +
                camera.OutputToBottomOffsetX.ToString("F6") + ")=" + outputX.ToString("F6") +
                ", formulaOutputY=finalPickerY+placeBridgeY=" +
                record.FinalPickerY.ToString("F6") + "+(" +
                camera.OutputToBottomOffsetY.ToString("F6") + ")=" + outputY.ToString("F6"));
        }
    }
}
