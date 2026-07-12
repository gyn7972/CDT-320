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
                // TODO: SideVisionY/PickerZ 보정은 Bottom SurfaceInspector 원본 로그 확인 후 연결한다.
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

            double bottomOffsetX = ReadValidatedBottomOffset(result, "bottom_offset_x_mm", "bottom_item_offset_x");
            double bottomOffsetY = ReadValidatedBottomOffset(result, "bottom_offset_y_mm", "bottom_item_offset_y");

            // TODO: 실장비 로그 확인 후 아래 후보 중 하나를 SideVisionY / PickerZ 보정으로 연결한다.
            // double sideVisionYOffset = ReadCandidate(result, "bottom_offset_y_mm", "bottom_item_offset_y");
            // double pickerZOffset = ReadCandidate(result, "bottom_offset_x_mm", "bottom_item_offset_x");
            double sideVisionYOffset = 0.0;
            double pickerZOffset = 0.0;

            return new BottomVisionOffset
            {
                PickerNo = pickerNo,
                OffsetX = bottomOffsetX,
                OffsetY = bottomOffsetY,
                OffsetT = ok ? bottomAngleDeg : 0.0,
                SideVisionYOffset = sideVisionYOffset,
                PickerZOffset = pickerZOffset,
                HasSideInspectionCorrection = false,
                IsOk = ok,
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
                case AutoVisionChannel.Wafer:
                    offsetX += data.InputToBottomOffsetX;
                    offsetY += data.InputToBottomOffsetY;
                    break;
                case AutoVisionChannel.Bin:
                    offsetX += data.OutputToBottomOffsetX;
                    offsetY += data.OutputToBottomOffsetY;
                    break;
            }
        }
    }
}
