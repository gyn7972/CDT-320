using System.Collections.Generic;
using QMC.Common.Recipes;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Config
{
    /// <summary>
    /// P1 — AlgorithmCameraMapping(알고리즘별 카메라, algorithm_camera.json) 디스크립터 어댑터.
    /// ParameterTarget="Camera/&lt;Algorithm&gt;". 계층(#7): ExposureUs=Recipe(제품·재질별 노출), 그 외 HW=Config.
    /// QMC.Common 무수정 — Vision 측 어댑터.
    /// </summary>
    public sealed class CameraParameters : IParameterProvider
    {
        private readonly AlgorithmCameraMapping _m;
        public CameraParameters(AlgorithmCameraMapping m) { _m = m; }

        public string ParameterTarget => "Camera/" + (_m.Algorithm ?? "");

        public IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            // #7 ExposureUs = Recipe
            yield return ParameterDescriptor.Double(t, "ExposureUs", "Exposure", "us", ParameterLayer.Recipe, () => _m.ExposureUs, v => _m.ExposureUs = v, min: 0);
            // #7 그 외 = Config (HW 고정)
            yield return ParameterDescriptor.Double(t, "Gain", "Gain", "", ParameterLayer.Config, () => _m.Gain, v => _m.Gain = v, min: 0);
            yield return ParameterDescriptor.Text(t, "CameraId", "Camera Id", ParameterLayer.Config, () => _m.CameraId, v => _m.CameraId = v);
            yield return ParameterDescriptor.Double(t, "FrameRate", "Frame Rate", "fps", ParameterLayer.Config, () => _m.FrameRate, v => _m.FrameRate = v, min: 0);
            yield return ParameterDescriptor.Text(t, "TriggerMode", "Trigger Mode", ParameterLayer.Config, () => _m.TriggerMode, v => _m.TriggerMode = v);
            yield return ParameterDescriptor.Text(t, "PixelFormat", "Pixel Format", ParameterLayer.Config, () => _m.PixelFormat, v => _m.PixelFormat = v);
            yield return ParameterDescriptor.Int(t, "DelayBeforeGrabMs", "Delay Before Grab", "ms", ParameterLayer.Config, () => _m.DelayBeforeGrabMs, v => _m.DelayBeforeGrabMs = v, min: 0);
            yield return ParameterDescriptor.Int(t, "RoiOffsetX", "ROI Offset X", "px", ParameterLayer.Config, () => _m.RoiOffsetX, v => _m.RoiOffsetX = v, min: 0);
            yield return ParameterDescriptor.Int(t, "RoiOffsetY", "ROI Offset Y", "px", ParameterLayer.Config, () => _m.RoiOffsetY, v => _m.RoiOffsetY = v, min: 0);
            yield return ParameterDescriptor.Int(t, "RoiWidth", "ROI Width", "px", ParameterLayer.Config, () => _m.RoiWidth, v => _m.RoiWidth = v, min: 0);
            yield return ParameterDescriptor.Int(t, "RoiHeight", "ROI Height", "px", ParameterLayer.Config, () => _m.RoiHeight, v => _m.RoiHeight = v, min: 0);
        }
    }
}
