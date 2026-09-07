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
                if (!TryValidateCameraOffsets(data.Camera, out summary) ||
                    !TryValidateColletRecords(data.Camera, data.Collet.FrontCollets, "Front", out summary) ||
                    !TryValidateColletRecords(data.Camera, data.Collet.RearCollets, "Rear", out summary))
                    return false;

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

        /// <summary>저장 후보의 카메라 변환값을 검증한다. 장비 Setup이나 파일은 변경하지 않는다.</summary>
        public static bool TryValidateCameraOffsets(VisionCameraCalibrationData camera, out string reason)
        {
            reason = string.Empty;
            if (camera == null)
            {
                reason = "카메라 캘리브레이션 데이터가 없습니다.";
                return false;
            }

            if (camera.InputReticle == null || !camera.InputReticle.HasVisionXPosition)
            {
                reason = "Input Reticle VisionX Encoder 값이 없습니다.";
                return false;
            }

            if (camera.OutputReticle == null || !camera.OutputReticle.HasVisionXPosition)
            {
                reason = "Output Reticle VisionX Encoder 값이 없습니다.";
                return false;
            }

            if (!IsFinite(camera.InputReticle.VisionXPosition) ||
                !IsFinite(camera.OutputReticle.VisionXPosition) ||
                !IsFinite(camera.InputToBottomOffsetX) ||
                !IsFinite(camera.InputToBottomOffsetY) ||
                !IsFinite(camera.OutputToBottomOffsetX) ||
                !IsFinite(camera.OutputToBottomOffsetY) ||
                !IsFinite(camera.InputToBottomManualCorrectionX) ||
                !IsFinite(camera.InputToBottomManualCorrectionY) ||
                !IsFinite(camera.OutputToBottomManualCorrectionX) ||
                !IsFinite(camera.OutputToBottomManualCorrectionY))
            {
                reason = "카메라 Encoder, Offset 또는 수동 보정에 유효하지 않은 숫자가 있습니다.";
                return false;
            }

            return CanUseReticleCameraBridge(camera, out reason);
        }

        /// <summary>한 콜렛의 최종 변환값만 계산한다. 후보 데이터와 장비 상태를 변경하지 않는다.</summary>
        public static bool TryCalculatePickerOffsets(
            VisionCameraCalibrationData camera,
            ColletCalibrationRecord record,
            out double inputX,
            out double inputY,
            out double outputX,
            out double outputY,
            out string reason)
        {
            inputX = inputY = outputX = outputY = 0.0;
            if (!TryValidateCameraOffsets(camera, out reason))
                return false;
            if (record == null || !record.Valid)
            {
                reason = "유효한 콜렛 캘리브레이션 기록이 없습니다.";
                return false;
            }
            if (!IsFinite(record.FinalPickerX) || !IsFinite(record.FinalPickerY))
            {
                reason = "콜렛 최종 Picker X/Y에 유효하지 않은 숫자가 있습니다.";
                return false;
            }

            // 기존 Offset은 측정 산식과 수동 보정이 합쳐진 최종값이다. 보정은 여기서 한 번만 반영한다.
            double candidateInputX = record.FinalPickerX - camera.InputReticle.VisionXPosition + camera.InputToBottomOffsetX;
            double candidateInputY = record.FinalPickerY + camera.InputToBottomOffsetY;
            double candidateOutputX = record.FinalPickerX - camera.OutputReticle.VisionXPosition + camera.OutputToBottomOffsetX;
            double candidateOutputY = record.FinalPickerY + camera.OutputToBottomOffsetY;
            if (!IsFinite(candidateInputX) || !IsFinite(candidateInputY) ||
                !IsFinite(candidateOutputX) || !IsFinite(candidateOutputY))
            {
                reason = "계산한 VisionToPicker Offset이 유효한 숫자 범위를 벗어났습니다.";
                return false;
            }

            inputX = candidateInputX;
            inputY = candidateInputY;
            outputX = candidateOutputX;
            outputY = candidateOutputY;
            reason = string.Empty;
            return true;
        }

        private static bool TryValidateColletRecords(
            VisionCameraCalibrationData camera,
            ColletCalibrationRecord[] records,
            string side,
            out string reason)
        {
            reason = string.Empty;
            if (records == null)
                return true;
            for (int i = 0; i < records.Length && i < 4; i++)
            {
                ColletCalibrationRecord record = records[i];
                if (record == null || !record.Valid)
                    continue;
                double inputX, inputY, outputX, outputY;
                if (!TryCalculatePickerOffsets(camera, record, out inputX, out inputY, out outputX, out outputY, out reason))
                {
                    reason = side + " Collet " + (i + 1) + ": " + reason;
                    return false;
                }
            }
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
                reason = "Bottom/Input/Output Reticle 측정값이 없습니다.";
                return false;
            }

            if (!IsFinite(camera.BottomReticle.MmX) || !IsFinite(camera.BottomReticle.MmY) ||
                !IsFinite(camera.InputReticle.MmX) || !IsFinite(camera.InputReticle.MmY) ||
                !IsFinite(camera.OutputReticle.MmX) || !IsFinite(camera.OutputReticle.MmY))
            {
                reason = "Reticle mm 값에 유효하지 않은 숫자가 있습니다.";
                return false;
            }

            // 수동 Offset도 측정 산식 + 명시적으로 저장한 수동 보정으로 검증한다.
            // 보정 필드가 없는 구버전 파일은 0 보정이므로 기존 산식 정합 검사를 그대로 받는다.
            const double toleranceMm = 0.001;
            double inputBridgeX = camera.ResolveEffectiveInputBridgeX();
            double inputBridgeY = camera.ResolveEffectiveInputBridgeY();
            double outputBridgeX = camera.ResolveEffectiveOutputBridgeX();
            double outputBridgeY = camera.ResolveEffectiveOutputBridgeY();
            if (!IsFinite(inputBridgeX) || !IsFinite(inputBridgeY) ||
                !IsFinite(outputBridgeX) || !IsFinite(outputBridgeY))
            {
                reason = "측정 산식과 수동 보정의 합이 유효한 숫자 범위를 벗어났습니다.";
                return false;
            }
            if (Math.Abs(inputBridgeX - camera.InputToBottomOffsetX) > toleranceMm ||
                Math.Abs(inputBridgeY - camera.InputToBottomOffsetY) > toleranceMm)
            {
                reason = "저장 InputToBottomOffset이 측정 산식 + 수동 보정과 다릅니다. expectedBridge=(" +
                         inputBridgeX.ToString("F6") + "," + inputBridgeY.ToString("F6") + "), storedOffset=(" +
                         camera.InputToBottomOffsetX.ToString("F6") + "," + camera.InputToBottomOffsetY.ToString("F6") +
                         "). Vision Camera Calibration에서 값을 확인하고 다시 저장하세요.";
                return false;
            }

            if (Math.Abs(outputBridgeX - camera.OutputToBottomOffsetX) > toleranceMm ||
                Math.Abs(outputBridgeY - camera.OutputToBottomOffsetY) > toleranceMm)
            {
                reason = "저장 OutputToBottomOffset이 측정 산식 + 수동 보정과 다릅니다. expectedBridge=(" +
                         outputBridgeX.ToString("F6") + "," + outputBridgeY.ToString("F6") + "), storedOffset=(" +
                         camera.OutputToBottomOffsetX.ToString("F6") + "," + camera.OutputToBottomOffsetY.ToString("F6") +
                         "). Vision Camera Calibration에서 값을 확인하고 다시 저장하세요.";
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

                // 수동 저장 후보와 기존 CALC/SAVE가 같은 계산을 사용한다.
                // 카메라 최종 Offset은 Picker X/Y에 한 번만 반영하며 StageY/NeedleX/맵 원점에는 넣지 않는다.
                double inputX, inputY, outputX, outputY;
                string calculationReason;
                if (!TryCalculatePickerOffsets(camera, record, out inputX, out inputY, out outputX, out outputY, out calculationReason))
                    throw new InvalidOperationException(side + " Collet " + (i + 1) + ": " + calculationReason);
                double outputFinalPickerX = record.FinalPickerX;

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
                ", inputManualCorrection=(" + camera.InputToBottomManualCorrectionX.ToString("F6") + "," + camera.InputToBottomManualCorrectionY.ToString("F6") + ")" +
                ", outputManualCorrection=(" + camera.OutputToBottomManualCorrectionX.ToString("F6") + "," + camera.OutputToBottomManualCorrectionY.ToString("F6") + ")" +
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
