using System;
using QMC.CDT320.VisionComm;

namespace QMC.CDT320.Calibration
{
    public static class VisionCameraCalibrationTransform
    {
        public static Func<VisionCameraCalibrationData> CalibrationProvider { get; set; }

        public static VisionAlignResult ToAlignResult(AutoVisionChannel channel, MatchResultDto match, double pitchMm)
        {
            if (match == null || !match.Success)
                return null;

            VisionCameraCalibrationData data = ResolveCalibrationData();
            VisionCameraPixelCalibration camera = ResolveCamera(data, channel);
            ApplyImageSize(camera, match);

            double offsetX = camera.PixelToMmOffsetX(match.X);
            double offsetY = camera.PixelToMmOffsetY(match.Y);
            ApplyBottomReferenceOffset(data, channel, ref offsetX, ref offsetY);

            return new VisionAlignResult
            {
                DeltaX = offsetX,
                DeltaY = offsetY,
                DeltaTheta = match.AngleDeg,
                PitchX = pitchMm,
                PitchY = pitchMm
            };
        }

        public static BottomVisionOffset ToBottomVisionOffset(int pickerNo, MatchResultDto match, double scoreThreshold)
        {
            VisionCameraCalibrationData data = ResolveCalibrationData();
            VisionCameraPixelCalibration camera = ResolveCamera(data, AutoVisionChannel.BottomInspection);
            ApplyImageSize(camera, match);

            bool ok = match != null && match.Success && match.Score >= scoreThreshold;
            return new BottomVisionOffset
            {
                PickerNo = pickerNo,
                // 현재 Bottom Vision X/Y는 Side 검사 보정 계산에 사용하지 않으므로 0으로 고정한다.
                OffsetX = 0.0,
                OffsetY = 0.0,
                OffsetT = ok ? match.AngleDeg : 0.0,
                BottomCenterOffsetX = 0.0,
                BottomCenterOffsetY = 0.0,
                HasBottomCenterOffset = false,
                // MATCH X/Y는 pixel 좌표이므로 Side 각도별 mm 보정에는 사용하지 않는다.
                SideVisionYOffset = 0.0,
                PickerZOffset = 0.0,
                HasSideInspectionCorrection = false,
                IsOk = ok,
                Raw = match != null ? match.RawError : ""
            };
        }

        public static BottomVisionOffset ToBottomVisionOffset(int pickerNo, InspectionResultDto result)
        {
            bool ok = result != null && result.IsPass;
            double bottomAngleDeg = 0.0;
            if (ok && result.TryGetDoubleValue(out bottomAngleDeg, "bottom_angle_deg", "bottom_item_angle"))
            {
                // Bottom SurfaceInspector Angle 원본값이다. T 보정 적용 여부는 별도 검증 후 결정한다.
            }

            // Place 보정용 Bottom Offset은 mm 소량 값만 허용하는 검증 경로로 읽는다.
            double bottomOffsetX = ReadValidatedBottomOffset(result, "bottom_offset_x_mm", "bottom_item_offset_x");
            double bottomOffsetY = ReadValidatedBottomOffset(result, "bottom_offset_y_mm", "bottom_item_offset_y");

            // 기존 OffsetX/Y는 MRESULT의 canonical 보정값을 우선하는 Side 보정 계약이므로 유지한다.
            // Place 보정은 최종 RESULT의 bottom_item_offset_x/y만 별도 필드로 분리하여 사용한다.
            double bottomItemOffsetX = 0.0;
            double bottomItemOffsetY = 0.0;
            bool bottomItemOffsetXPass = false;
            bool bottomItemOffsetYPass = false;
            bool measureValid = false;
            bool hasBottomItemOffsetX = result != null &&
                result.TryGetDoubleValue(out bottomItemOffsetX, "bottom_item_offset_x") &&
                IsFinite(bottomItemOffsetX);
            bool hasBottomItemOffsetY = result != null &&
                result.TryGetDoubleValue(out bottomItemOffsetY, "bottom_item_offset_y") &&
                IsFinite(bottomItemOffsetY);
            bool hasBottomItemOffsetXPass = result != null &&
                result.TryGetBooleanValue(out bottomItemOffsetXPass, "bottom_item_offset_x_pass");
            bool hasBottomItemOffsetYPass = result != null &&
                result.TryGetBooleanValue(out bottomItemOffsetYPass, "bottom_item_offset_y_pass");
            bool hasMeasureValid = result != null &&
                result.TryGetBooleanValue(out measureValid, "measure_valid");

            // TODO: 실장비 로그 확인 후 아래 후보 중 하나를 SideVisionY / PickerZ 보정으로 연결한다.
            // double sideVisionYOffset = ReadCandidate(result, "bottom_offset_y_mm", "bottom_item_offset_y");
            // double pickerZOffset = ReadCandidate(result, "bottom_offset_x_mm", "bottom_item_offset_x");
            double sideVisionYOffset = 0.0;
            double pickerZOffset = 0.0;
            double bottomCenterOffsetX = 0.0;
            double bottomCenterOffsetY = 0.0;
            bool hasBottomCenterOffsetX = result != null && result.TryGetDoubleValue(
                out bottomCenterOffsetX,
                "bottom_offset_x_mm",
                "bottom_center_offset_x_mm",
                "bottom_center_x_offset_mm",
                "bottom_center_x_mm",
                "center_offset_x_mm",
                "center_x_offset_mm",
                "center_x_mm",
                "bottom_item_offset_x");
            bool hasBottomCenterOffsetY = result != null && result.TryGetDoubleValue(
                out bottomCenterOffsetY,
                "bottom_offset_y_mm",
                "bottom_center_offset_y_mm",
                "bottom_center_y_offset_mm",
                "bottom_center_y_mm",
                "center_offset_y_mm",
                "center_y_offset_mm",
                "center_y_mm",
                "bottom_item_offset_y");
            bool hasBottomCenterOffset = hasBottomCenterOffsetX &&
                                         hasBottomCenterOffsetY &&
                                         IsFinite(bottomCenterOffsetX) &&
                                         IsFinite(bottomCenterOffsetY);

            return new BottomVisionOffset
            {
                PickerNo = pickerNo,
                OffsetX = bottomOffsetX,
                OffsetY = bottomOffsetY,
                OffsetT = ok ? bottomAngleDeg : 0.0,
                BottomCenterOffsetX = bottomCenterOffsetX,
                BottomCenterOffsetY = bottomCenterOffsetY,
                HasBottomCenterOffset = hasBottomCenterOffset,
                SideVisionYOffset = sideVisionYOffset,
                PickerZOffset = pickerZOffset,
                HasSideInspectionCorrection = false,
                IsOk = ok,
                BottomItemOffsetX = bottomItemOffsetX,
                BottomItemOffsetY = bottomItemOffsetY,
                HasBottomItemOffsetX = hasBottomItemOffsetX,
                HasBottomItemOffsetY = hasBottomItemOffsetY,
                BottomItemOffsetXPass = bottomItemOffsetXPass,
                BottomItemOffsetYPass = bottomItemOffsetYPass,
                HasBottomItemOffsetXPass = hasBottomItemOffsetXPass,
                HasBottomItemOffsetYPass = hasBottomItemOffsetYPass,
                MeasureValid = measureValid,
                HasMeasureValid = hasMeasureValid,
                RequestId = result != null ? result.RequestId : "",
                GroupId = result != null ? result.GroupId : "",
                DieId = "",
                DieIndex = -1,
                Raw = result != null ? result.Raw : "",
                Values = result != null && result.Values != null
                    ? new System.Collections.Generic.Dictionary<string, string>(result.Values, StringComparer.OrdinalIgnoreCase)
                    : new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private static double ReadValidatedBottomOffset(InspectionResultDto result, params string[] keys)
        {
            if (result == null || keys == null)
                return 0.0;

            double value;
            if (!result.TryGetDoubleValue(out value, keys) ||
                double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;

            // Bottom Die 중심 오프셋은 mm 단위의 소량 값만 허용하고 절대 픽셀 좌표 유입은 차단합니다.
            return Math.Abs(value) <= 50.0 ? value : 0.0;
        }

        public static InspectionResultDto ToInspectionResult(AutoVisionChannel channel, InspectionResultDto result)
        {
            if (result == null)
                return null;

            VisionCameraCalibrationData data = ResolveCalibrationData();
            VisionCameraPixelCalibration camera = ResolveCamera(data, channel);
            ApplyImageSize(camera, result);

            if (result.HasOffset)
            {
                double offsetX = camera.PixelToMmOffsetX(result.OffsetX);
                double offsetY = camera.PixelToMmOffsetY(result.OffsetY);
                ApplyBottomReferenceOffset(data, channel, ref offsetX, ref offsetY);
                result.OffsetX = offsetX;
                result.OffsetY = offsetY;
            }

            return result;
        }

        public static VisionCameraPixelCalibration ResolveCamera(VisionCameraCalibrationData data, AutoVisionChannel channel)
        {
            data = data ?? ResolveCalibrationData();
            data.EnsureObjects();

            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return data.InputCamera;
                case AutoVisionChannel.BottomInspection:
                    return data.BottomCamera;
                case AutoVisionChannel.Bin:
                    return data.OutputCamera;
                case AutoVisionChannel.FrontSide:
                    return data.FrontSideCamera;
                case AutoVisionChannel.RearSide:
                    return data.RearSideCamera;
                default:
                    return data.BottomCamera;
            }
        }

        private static VisionCameraCalibrationData ResolveCalibrationData()
        {
            VisionCameraCalibrationData data = null;
            try
            {
                if (CalibrationProvider != null)
                    data = CalibrationProvider();
            }
            catch
            {
                data = null;
            }
            finally
            {
            }

            if (data == null)
                data = new VisionCameraCalibrationData();

            data.EnsureObjects();
            return data;
        }

        private static void ApplyImageSize(VisionCameraPixelCalibration camera, MatchResultDto match)
        {
            if (camera == null || match == null || !match.HasImageSize)
                return;

            camera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);
        }

        private static void ApplyImageSize(VisionCameraPixelCalibration camera, InspectionResultDto result)
        {
            if (camera == null || result == null || !result.HasImageSize)
                return;

            camera.ApplyImageSize(result.ImageWidthPixel, result.ImageHeightPixel);
        }

        private static void ApplyBottomReferenceOffset(VisionCameraCalibrationData data, AutoVisionChannel channel, ref double offsetX, ref double offsetY)
        {
            if (data == null || !data.Valid)
                return;

            switch (channel)
            {
                // Wafer(Input) 채널은 카메라 순수 오프셋(raw)을 그대로 반환한다.
                // InputToBottomOffset(카메라 브리지)은 저장 InputVisionToPicker X/Y에서 딱 1회만 반영된다
                // (PickerVisionOffsetCalibrationService.ApplySide) — PickerX/PickerY 전용.
                // 라이브 결과에 가산하면 StageY/맵 원점/NeedleX까지 실려 축이 갈리고 상쇄가 깨진다(이중 적용 버그).
                case AutoVisionChannel.Bin:
                    offsetX += data.OutputToBottomOffsetX;
                    offsetY += data.OutputToBottomOffsetY;
                    break;
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
