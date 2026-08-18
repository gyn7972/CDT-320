using System;
using QMC.CDT320.Calibration;

namespace QMC.CDT320.Sequencing
{
    // Side 비전 Front/Rear Y 절대식 계산 결과 (2026-07-29 확정식).
    // 통합(Bottom+Side)·단독 Side 시퀀스가 공용으로 사용한다.
    internal sealed class SideVisionYTargetResult
    {
        public double FrontProcessY;   // Recipe.FrontSideVision.Process0Position (콜렛 중심면 기준 티칭)
        public double RearProcessY;    // Recipe.RearSideVision.Process0Position
        public double DieSizeX;        // 가로(Width)
        public double DieSizeY;        // 세로(Height)
        public double HalfSize0;       // DieSizeY/2 — 0도에서 카메라축 방향 반쪽치수
        public double HalfSize90;      // DieSizeX/2 — 90도 회전 후 반쪽치수
        public double OffsetXmm;       // Bottom MRESULT bottom_offset_x_mm (미적용 시 0)
        public double OffsetYmm;       // Bottom MRESULT bottom_offset_y_mm (미적용 시 0)
        public bool OffsetApplied;     // false면 (0,0)으로 기하항+ΔY4만 반영
        public bool RotationCenterUsed;      // true=COC 회전 적용, false=회전중심 미사용 폴백
        public double RotationCenterPickerX; // Config.ColletRotationCenterX[콜렛] (PickerX 기계좌표)
        public double RotationCenterPickerY; // Config.ColletRotationCenterY[콜렛] (PickerY 기계좌표)
        public double BottomShotPickerX;     // Bottom 촬영 시점 PickerX 지령
        public double BottomShotPickerY;     // Bottom 촬영 시점 PickerY 지령
        public double CocEccentricX;   // cX = BottomShotPickerX − RotationCenterPickerX
        public double CocEccentricY;   // cY = BottomShotPickerY − RotationCenterPickerY
        public double Rot90Y;          // 90도 회전 후 다이중심 Y 변위
        public double DeltaY4;         // 콜렛Cal FinalPickerY(현재) − FinalPickerY(4번), 마지막 1회 가산
        public double Front0Y;
        public double Front90Y;
        public double Rear0Y;
        public double Rear90Y;

        public double ResolveY(bool frontCamera, int angleDeg)
        {
            if (frontCamera)
                return angleDeg == 90 ? Front90Y : Front0Y;
            return angleDeg == 90 ? Rear90Y : Rear0Y;
        }

        public string BuildTermLogText()
        {
            return "P_F=" + FrontProcessY.ToString("F6") +
                   ", P_R=" + RearProcessY.ToString("F6") +
                   ", dieSize=(" + DieSizeX.ToString("F6") + "," + DieSizeY.ToString("F6") + ")" +
                   ", half0=" + HalfSize0.ToString("F6") +
                   ", half90=" + HalfSize90.ToString("F6") +
                   ", offset=(" + OffsetXmm.ToString("F6") + "," + OffsetYmm.ToString("F6") +
                   ",applied=" + OffsetApplied + ")" +
                   ", rotCenterUsed=" + RotationCenterUsed +
                   ", recipeCoc=(" + RotationCenterPickerX.ToString("F6") + "," + RotationCenterPickerY.ToString("F6") + ")" +
                   ", bottomShotPicker=(" + BottomShotPickerX.ToString("F6") + "," + BottomShotPickerY.ToString("F6") + ")" +
                   ", cocEccentric=(" + CocEccentricX.ToString("F6") + "," + CocEccentricY.ToString("F6") + ")" +
                   ", rot90Y=" + Rot90Y.ToString("F6") +
                   ", deltaY4=" + DeltaY4.ToString("F6") +
                   ", Front0Y=" + Front0Y.ToString("F6") +
                   ", Front90Y=" + Front90Y.ToString("F6") +
                   ", Rear0Y=" + Rear0Y.ToString("F6") +
                   ", Rear90Y=" + Rear90Y.ToString("F6");
        }
    }

    // 확정식(2026-07-29, 회전중심 소스 정정 2026-08-17):
    //   ΔY4  = 콜렛Cal FinalPickerY(현재 콜렛) − FinalPickerY(4번 콜렛)  — 마지막에 1회만 가산
    //
    //   [회전중심 유효(Config.ColletRotationCenterValid=true) + Bottom 측정 프레임 확보]
    //     cX   = Bottom 촬영 PickerX − Config.ColletRotationCenterX[콜렛]
    //     cY   = Bottom 촬영 PickerY − Config.ColletRotationCenterY[콜렛]
    //     rotY = cY − (OffsetX − cX)    ← CW 90도 회전 (x,y)→(y,−x)를 회전중심 c 기준으로 적용한 Y성분
    //   Recipe의 ColletRotationCenterX/Y는 "그 콜렛의 회전축이 Bottom 카메라 광축에 오는 PickerXY 기계좌표"다.
    //   따라서 (Bottom 촬영 PickerXY − 그 값) = 촬영 프레임에서 회전축이 카메라축으로부터 벗어난 편심량이며,
    //   이 편심 때문에 90도 회전 시 다이가 휘둘려 초점면이 이동한다. 0도에서는 회전중심이 수식에서 소거된다.
    //
    //   [회전중심 무효(COC VALID=false) 또는 Bottom 측정 없음 — 팀장님 지시 2026-08-17]
    //     회전중심 계산 없이(편심 cX=cY=0) 같은 회전식을 적용한다 ⇒ rotY = −OffsetX.
    //     결과적으로 0도의 −OffsetY 자리에 +OffsetX가 들어간 형태가 된다(카메라축 중심 회전).
    //
    //   0도:  FrontY = P_F − DieSizeY/2 − OffsetY + ΔY4
    //         RearY  = P_R + DieSizeY/2 − OffsetY + ΔY4
    //   90도: FrontY = P_F − DieSizeX/2 − rotY   + ΔY4
    //         RearY  = P_R + DieSizeX/2 − rotY   + ΔY4
    // 오프셋 추종 항(−OffsetY/−rotY/+ΔY4)은 두 카메라 공통, 반쪽치수 항만 카메라별(Front −, Rear +).
    internal static class SideVisionYTargetCalculator
    {
        public static bool TryBuild(
            CDT320_Machine machine,
            MachineController controller,
            VisionFocusPickerSide pickerSide,
            int pickerNo,
            double offsetXmm,
            double offsetYmm,
            bool offsetApplied,
            double bottomShotPickerX,
            double bottomShotPickerY,
            out SideVisionYTargetResult result,
            out string failReason)
        {
            result = null;
            failReason = string.Empty;

            try
            {
                VisionUnit vision = machine != null ? machine.VisionUnit : null;
                if (vision == null)
                {
                    failReason = "VisionUnit 없음";
                    return false;
                }

                if (pickerNo < 1 || pickerNo > 4)
                {
                    failReason = "pickerNo 범위 오류. pickerNo=" + pickerNo;
                    return false;
                }

                double frontProcessY = vision.GetVisionTeachingPosition(VisionAxis.FrontSideVisionY, "Process0Position");
                double rearProcessY = vision.GetVisionTeachingPosition(VisionAxis.RearSideVisionY, "Process0Position");

                double dieSizeX = 0.0;
                double dieSizeY = 0.0;
                QMC.CDT320.Recipes.RecipeProject recipe = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                QMC.CDT320.Recipes.TapeFrameSubset frame = recipe != null
                    ? (recipe.InputFrame ?? recipe.Frame)
                    : null;
                if (frame != null && frame.DieSizeX > 0.0 && frame.DieSizeY > 0.0)
                {
                    dieSizeX = frame.DieSizeX;
                    dieSizeY = frame.DieSizeY;
                }
                if ((dieSizeX <= 0.0 || dieSizeY <= 0.0) && controller != null)
                {
                    dieSizeX = controller.DieSizeXMm;
                    dieSizeY = controller.DieSizeYMm;
                }
                if (dieSizeX <= 0.0 || dieSizeY <= 0.0)
                {
                    failReason = "다이 사이즈 확보 실패(레시피 InputFrame/Frame, Controller 모두 무효). " +
                                 "dieSizeX=" + dieSizeX.ToString("F6") + ", dieSizeY=" + dieSizeY.ToString("F6");
                    return false;
                }

                // ΔY4: 콜렛Cal FinalPickerY 차. 콜렛Cal 자체가 없으면 Side 검사를 진행하지 않는다.
                ColletCalibrationRecord current = CalibrationCoordinateService.ResolveCollet(machine, pickerSide, pickerNo - 1);
                if (current == null)
                {
                    failReason = "콜렛Cal 없음(현재 콜렛). side=" + pickerSide + ", pickerNo=" + pickerNo;
                    return false;
                }

                ColletCalibrationRecord picker4 = CalibrationCoordinateService.ResolveCollet(machine, pickerSide, 3);
                if (picker4 == null)
                {
                    failReason = "콜렛Cal 없음(4번 콜렛, ΔY4 기준). side=" + pickerSide;
                    return false;
                }

                double deltaY4 = current.FinalPickerY - picker4.FinalPickerY;
                if (double.IsNaN(deltaY4) || double.IsInfinity(deltaY4))
                {
                    failReason = "ΔY4 계산 실패(FinalPickerY 무효). current=" + current.FinalPickerY +
                                 ", picker4=" + picker4.FinalPickerY;
                    return false;
                }

                double offsetX = offsetApplied ? offsetXmm : 0.0;
                double offsetY = offsetApplied ? offsetYmm : 0.0;
                if (double.IsNaN(offsetX) || double.IsInfinity(offsetX) ||
                    double.IsNaN(offsetY) || double.IsInfinity(offsetY))
                {
                    failReason = "Bottom Offset 값 무효. offset=(" + offsetXmm + "," + offsetYmm + ")";
                    return false;
                }

                double rotationCenterPickerX;
                double rotationCenterPickerY;
                bool rotationCenterValid = TryResolveRecipeRotationCenter(
                    machine,
                    pickerSide,
                    pickerNo,
                    out rotationCenterPickerX,
                    out rotationCenterPickerY);

                // 회전중심을 못 쓰는 경우(COC VALID=false 또는 Bottom 측정 프레임 없음)에는
                // 편심을 0으로 두고 같은 회전식을 그대로 적용한다 ⇒ rotY = −OffsetX.
                // 즉 90도 오프셋 항이 0도의 −OffsetY 자리에 +OffsetX가 들어간 형태가 된다.
                double cocEccentricX = 0.0;
                double cocEccentricY = 0.0;
                bool rotationCenterUsed = rotationCenterValid && offsetApplied;
                if (rotationCenterUsed)
                {
                    cocEccentricX = bottomShotPickerX - rotationCenterPickerX;
                    cocEccentricY = bottomShotPickerY - rotationCenterPickerY;
                }

                double rot90Y = cocEccentricY - (offsetX - cocEccentricX);

                if (double.IsNaN(rot90Y) || double.IsInfinity(rot90Y))
                {
                    failReason = "90도 회전 Y 계산 무효. rot90Y=" + rot90Y +
                                 ", rotationCenterUsed=" + rotationCenterUsed;
                    return false;
                }

                double half0 = dieSizeY / 2.0;
                double half90 = dieSizeX / 2.0;

                result = new SideVisionYTargetResult
                {
                    FrontProcessY = frontProcessY,
                    RearProcessY = rearProcessY,
                    DieSizeX = dieSizeX,
                    DieSizeY = dieSizeY,
                    HalfSize0 = half0,
                    HalfSize90 = half90,
                    OffsetXmm = offsetX,
                    OffsetYmm = offsetY,
                    OffsetApplied = offsetApplied,
                    RotationCenterUsed = rotationCenterUsed,
                    RotationCenterPickerX = rotationCenterPickerX,
                    RotationCenterPickerY = rotationCenterPickerY,
                    BottomShotPickerX = bottomShotPickerX,
                    BottomShotPickerY = bottomShotPickerY,
                    CocEccentricX = cocEccentricX,
                    CocEccentricY = cocEccentricY,
                    Rot90Y = rot90Y,
                    DeltaY4 = deltaY4,
                    Front0Y = frontProcessY - half0 - offsetY + deltaY4,
                    Rear0Y = rearProcessY + half0 - offsetY + deltaY4,
                    Front90Y = frontProcessY - half90 - rot90Y + deltaY4,
                    Rear90Y = rearProcessY + half90 - rot90Y + deltaY4
                };
                return true;
            }
            catch (Exception ex)
            {
                result = null;
                failReason = "Side Vision Y 절대식 계산 예외: " + ex.Message;
                return false;
            }
        }

        // Picker Recipe의 콜렛별 회전 중심(PickerXY 기계좌표)을 읽는다.
        // COC 캘리브레이션이 "회전축이 Bottom 카메라 광축에 오는 PickerXY"로 저장한 값이며,
        // 콜렛 교체 시 값이 바뀌므로 픽셀 잔차 대신 이 값을 회전 중심 기준으로 사용한다.
        private static bool TryResolveRecipeRotationCenter(
            CDT320_Machine machine,
            VisionFocusPickerSide pickerSide,
            int pickerNo,
            out double centerPickerX,
            out double centerPickerY)
        {
            centerPickerX = 0.0;
            centerPickerY = 0.0;

            try
            {
                int index = pickerNo - 1;
                if (index < 0 || index > 3)
                    return false;

                double[] centerX;
                double[] centerY;
                bool[] valid;
                if (pickerSide == VisionFocusPickerSide.Front)
                {
                    if (machine == null || machine.PickerFrontUnit == null || machine.PickerFrontUnit.Recipe == null)
                        return false;
                    machine.PickerFrontUnit.Recipe.EnsurePositionObjects();
                    centerX = machine.PickerFrontUnit.Config.ColletRotationCenterX;
                    centerY = machine.PickerFrontUnit.Config.ColletRotationCenterY;
                    valid = machine.PickerFrontUnit.Config.ColletRotationCenterValid;
                }
                else
                {
                    if (machine == null || machine.PickerRearUnit == null || machine.PickerRearUnit.Recipe == null)
                        return false;
                    machine.PickerRearUnit.Recipe.EnsurePositionObjects();
                    centerX = machine.PickerRearUnit.Config.ColletRotationCenterX;
                    centerY = machine.PickerRearUnit.Config.ColletRotationCenterY;
                    valid = machine.PickerRearUnit.Config.ColletRotationCenterValid;
                }

                if (centerX == null || centerY == null || valid == null ||
                    index >= centerX.Length || index >= centerY.Length || index >= valid.Length)
                    return false;

                if (!valid[index])
                    return false;

                centerPickerX = centerX[index];
                centerPickerY = centerY[index];
                if (double.IsNaN(centerPickerX) || double.IsInfinity(centerPickerX) ||
                    double.IsNaN(centerPickerY) || double.IsInfinity(centerPickerY))
                {
                    centerPickerX = 0.0;
                    centerPickerY = 0.0;
                    return false;
                }

                return true;
            }
            catch
            {
                centerPickerX = 0.0;
                centerPickerY = 0.0;
                return false;
            }
            finally
            {
            }
        }
    }
}
