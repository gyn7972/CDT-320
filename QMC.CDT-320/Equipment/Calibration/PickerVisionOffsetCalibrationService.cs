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
                if (!data.Camera.Valid)
                {
                    summary = "Vision Camera Calibration is not valid.";
                    return false;
                }

                if (!data.Camera.InputReticle.HasVisionXPosition || !data.Camera.OutputReticle.HasVisionXPosition)
                {
                    summary = "Input/Output reticle VisionX encoder position is missing.";
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

                // 저장 offset 자체를 자동/수동 Pick 계산식의 inputVisionToPickerX로 사용한다.
                double inputX = record.FinalPickerX - camera.InputReticle.VisionXPosition;
                double inputY = Math.Abs(record.FinalPickerY);
                double outputX = record.FinalPickerX - camera.OutputReticle.VisionXPosition;
                double outputY = Math.Abs(record.FinalPickerY);

                inputOffsets.OffsetX[i] = inputX;
                inputOffsets.OffsetY[i] = inputY;
                outputOffsets.OffsetX[i] = outputX;
                outputOffsets.OffsetY[i] = outputY;
                count++;

                LogAppliedOffset(side, i, record, camera, inputX, inputY, outputX, outputY);
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
            double outputY)
        {
            QMC.Common.Log.Write("Calibration", "SYSTEM", "PickerVisionOffsetFormula",
                "VisionToPicker offset calculated. side=" + side +
                ", pickerNo=" + (pickerIndex + 1) +
                ", finalPicker=(" + record.FinalPickerX.ToString("F6") + "," + record.FinalPickerY.ToString("F6") + ")" +
                ", inputReticleVisionX=" + camera.InputReticle.VisionXPosition.ToString("F6") +
                ", outputReticleVisionX=" + camera.OutputReticle.VisionXPosition.ToString("F6") +
                ", formulaInputX=finalPickerX-inputVisionX=" +
                record.FinalPickerX.ToString("F6") + "-" +
                camera.InputReticle.VisionXPosition.ToString("F6") + "=" + inputX.ToString("F6") +
                ", formulaInputY=abs(finalPickerY)=" + inputY.ToString("F6") +
                ", formulaOutputX=finalPickerX-outputVisionX=" +
                record.FinalPickerX.ToString("F6") + "-" +
                camera.OutputReticle.VisionXPosition.ToString("F6") + "=" + outputX.ToString("F6") +
                ", formulaOutputY=abs(finalPickerY)=" + outputY.ToString("F6"));
        }
    }
}
