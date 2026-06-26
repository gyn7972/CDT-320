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
            VisionCameraPixelCalibration camera = ResolveCamera(data, AutoVisionChannel.Bottom);
            ApplyImageSize(camera, match);

            bool ok = match != null && match.Success && match.Score >= scoreThreshold;
            return new BottomVisionOffset
            {
                PickerNo = pickerNo,
                OffsetX = ok ? camera.PixelToMmOffsetX(match.X) : 0.0,
                OffsetY = ok ? camera.PixelToMmOffsetY(match.Y) : 0.0,
                OffsetT = ok ? match.AngleDeg : 0.0,
                IsOk = ok
            };
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
                case AutoVisionChannel.Bottom:
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
                    offsetX -= data.InputToBottomOffsetX;
                    offsetY -= data.InputToBottomOffsetY;
                    break;
                case AutoVisionChannel.Bin:
                    offsetX -= data.OutputToBottomOffsetX;
                    offsetY -= data.OutputToBottomOffsetY;
                    break;
            }
        }
    }
}
