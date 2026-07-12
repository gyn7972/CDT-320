using System;
using QMC.CDT320.Ajin;
using QMC.Common;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Calibration
{
    internal static class ColletCalibrationApplyService
    {
        public static int ApplyTHomeOffsetAndZero(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            int colletNo,
            out string message)
        {
            message = string.Empty;
            try
            {
                if (machine == null || machine.VisionUnit == null || machine.VisionUnit.Config == null)
                {
                    message = "장비 또는 CalibrationData가 없어 T축 보정값을 적용할 수 없습니다.";
                    return -1;
                }

                machine.VisionUnit.Config.EnsureCalibrationObjects();
                ColletCalibrationData data = machine.VisionUnit.Config.CalibrationData.Collet;
                data.EnsureObjects();
                ColletCalibrationRecord record = data.GetRecord(side, colletNo);
                if (record == null || !record.Valid)
                {
                    message = "유효한 Collet Calibration 결과가 없습니다. side=" + side + ", colletNo=" + colletNo;
                    return -1;
                }

                PickerAxis tAxisKind = CalibrationCoordinateService.ResolvePickerTAxis(NormalizeColletIndex(colletNo));
                BaseAxis tAxis = ResolvePickerAxis(machine, side, tAxisKind);
                if (tAxis == null || tAxis.Setup == null)
                {
                    message = "T축 또는 T축 설정을 찾을 수 없습니다. side=" + side +
                              ", colletNo=" + colletNo + ", axis=" + tAxisKind;
                    return -1;
                }

                tAxis.UpdateStatus();
                if (tAxis.IsAlarm || tAxis.IsMoving)
                {
                    message = "T축이 HomeOffset 적용 가능한 상태가 아닙니다. axis=" + tAxis.Name +
                              ", alarm=" + tAxis.IsAlarm + ", moving=" + tAxis.IsMoving;
                    return -1;
                }

                double oldHomeOffset = tAxis.Setup.HomeOffset;
                double oldActual = tAxis.ActualPosition;
                double oldCommand = tAxis.CommandPosition;
                double newHomeOffset = record.TZeroHomeOffset;

                tAxis.Setup.HomeOffset = newHomeOffset;
                tAxis.SetPosition(0.0);
                tAxis.UpdateStatus();

                double tolerance = tAxis.Config != null && tAxis.Config.InPositionTolerance > 0.0
                    ? tAxis.Config.InPositionTolerance
                    : 0.001;
                if (Math.Abs(tAxis.ActualPosition) > tolerance || Math.Abs(tAxis.CommandPosition) > tolerance)
                {
                    tAxis.Setup.HomeOffset = oldHomeOffset;
                    message = "T축 현재 좌표 0점 설정 확인에 실패했습니다. axis=" + tAxis.Name +
                              ", actual=" + tAxis.ActualPosition.ToString("F6") +
                              ", command=" + tAxis.CommandPosition.ToString("F6") +
                              ", tolerance=" + tolerance.ToString("F6");
                    return -1;
                }

                AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
                if (!machine.SaveSettings())
                {
                    message = "T축 HomeOffset 적용 후 장비 설정 저장에 실패했습니다. axis=" + tAxis.Name;
                    return -1;
                }

                message = "T축 HomeOffset 적용 및 현재 좌표 0점 설정 완료. side=" + side +
                          ", colletNo=" + colletNo +
                          ", axis=" + tAxis.Name +
                          ", oldOffset=" + oldHomeOffset.ToString("F6") +
                          ", newOffset=" + newHomeOffset.ToString("F6") +
                          ", oldActual=" + oldActual.ToString("F6") +
                          ", oldCommand=" + oldCommand.ToString("F6") +
                          ", newActual=" + tAxis.ActualPosition.ToString("F6") +
                          ", newCommand=" + tAxis.CommandPosition.ToString("F6");
                Log.Write("Calibration", "SYSTEM", "AutoColletApplyTHome", message + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                message = "T축 HomeOffset 적용 중 예외가 발생했습니다. side=" + side +
                          ", colletNo=" + colletNo + ", error=" + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-COLLET-T-HOME", message);
                return -1;
            }
            finally
            {
            }
        }

        public static int SaveRotationCenterToRecipe(
            CDT320_Machine machine,
            string recipeName,
            VisionFocusPickerSide side,
            int colletNo,
            double centerX,
            double centerY,
            out string message)
        {
            message = string.Empty;
            try
            {
                if (machine == null || string.IsNullOrWhiteSpace(recipeName))
                {
                    message = "장비 또는 활성 Recipe가 없어 회전 중심을 저장할 수 없습니다. recipe=" +
                              (recipeName ?? string.Empty);
                    return -1;
                }

                int index = NormalizeColletIndex(colletNo);
                if (side == VisionFocusPickerSide.Front)
                {
                    if (machine.PickerFrontUnit == null || machine.PickerFrontUnit.Recipe == null)
                    {
                        message = "Front Picker Recipe가 없습니다.";
                        return -1;
                    }

                    machine.PickerFrontUnit.Recipe.EnsurePositionObjects();
                    machine.PickerFrontUnit.Recipe.ColletRotationCenterX[index] = centerX;
                    machine.PickerFrontUnit.Recipe.ColletRotationCenterY[index] = centerY;
                    machine.PickerFrontUnit.Recipe.ColletRotationCenterValid[index] = true;
                }
                else
                {
                    if (machine.PickerRearUnit == null || machine.PickerRearUnit.Recipe == null)
                    {
                        message = "Rear Picker Recipe가 없습니다.";
                        return -1;
                    }

                    machine.PickerRearUnit.Recipe.EnsurePositionObjects();
                    machine.PickerRearUnit.Recipe.ColletRotationCenterX[index] = centerX;
                    machine.PickerRearUnit.Recipe.ColletRotationCenterY[index] = centerY;
                    machine.PickerRearUnit.Recipe.ColletRotationCenterValid[index] = true;
                }

                if (!machine.SaveRecipe(recipeName))
                {
                    message = "회전 중심 Recipe 저장에 실패했습니다. recipe=" + recipeName +
                              ", side=" + side + ", colletNo=" + colletNo;
                    return -1;
                }

                message = "회전 중심 Recipe 저장 완료. recipe=" + recipeName +
                          ", side=" + side + ", colletNo=" + colletNo +
                          ", centerX=" + centerX.ToString("F6") +
                          ", centerY=" + centerY.ToString("F6");
                Log.Write("Calibration", "SYSTEM", "AutoColletSaveCoc", message + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                message = "회전 중심 Recipe 저장 중 예외가 발생했습니다. side=" + side +
                          ", colletNo=" + colletNo + ", error=" + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-COLLET-COC-SAVE", message);
                return -1;
            }
            finally
            {
            }
        }

        private static BaseAxis ResolvePickerAxis(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            PickerAxis axisKind)
        {
            BaseAxis axis;
            if (side == VisionFocusPickerSide.Front)
            {
                return machine.PickerFrontUnit != null && machine.PickerFrontUnit.Axes != null &&
                       machine.PickerFrontUnit.Axes.TryGetValue(axisKind, out axis)
                    ? axis
                    : null;
            }

            return machine.PickerRearUnit != null && machine.PickerRearUnit.Axes != null &&
                   machine.PickerRearUnit.Axes.TryGetValue(axisKind, out axis)
                ? axis
                : null;
        }

        private static int NormalizeColletIndex(int colletNo)
        {
            if (colletNo <= 1)
                return 0;
            if (colletNo >= 4)
                return 3;
            return colletNo - 1;
        }
    }
}
