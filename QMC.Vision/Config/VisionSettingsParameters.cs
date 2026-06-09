using System.Collections.Generic;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Config
{
    /// <summary>
    /// P1 — VisionSettings(전역, vision.json) 디스크립터 어댑터. ParameterTarget="Vision/Global".
    /// 계층(#5): Scale/방향 캘리브=Setup. 그 외(포트·뷰어·지연·언어·Provider·로그)=Config.
    /// QMC.Common 무수정 — Vision 측 어댑터로 래핑.
    /// </summary>
    public sealed class VisionSettingsParameters : IParameterProvider
    {
        private readonly VisionSettings _s;
        public VisionSettingsParameters(VisionSettings s) { _s = s; }

        public string ParameterTarget => "Vision/Global";

        public IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            // #5 Setup — 캘리브/방향
            yield return ParameterDescriptor.Double(t, "ScaleX", "Scale X", "mm/px", ParameterLayer.Setup, () => _s.ScaleX, v => _s.ScaleX = v, min: 0);
            yield return ParameterDescriptor.Double(t, "ScaleY", "Scale Y", "mm/px", ParameterLayer.Setup, () => _s.ScaleY, v => _s.ScaleY = v, min: 0);
            yield return ParameterDescriptor.Bool(t, "IsRotated", "Is Rotated", ParameterLayer.Setup, () => _s.IsRotated, v => _s.IsRotated = v);
            yield return ParameterDescriptor.Bool(t, "InvertedX", "Inverted X", ParameterLayer.Setup, () => _s.InvertedX, v => _s.InvertedX = v);
            yield return ParameterDescriptor.Bool(t, "InvertedY", "Inverted Y", ParameterLayer.Setup, () => _s.InvertedY, v => _s.InvertedY = v);
            // Config — HW/고정 사양
            yield return ParameterDescriptor.Enum(t, "Provider", "Vision Provider", ParameterLayer.Config, () => _s.Provider, v => _s.Provider = v);
            yield return ParameterDescriptor.Text(t, "Language", "Language", ParameterLayer.Config, () => _s.Language, v => _s.Language = v);
            yield return ParameterDescriptor.Bool(t, "LightUseSim", "Light Use Sim", ParameterLayer.Config, () => _s.LightUseSim, v => _s.LightUseSim = v);
            yield return ParameterDescriptor.Int(t, "DelayBeforeGrabMs", "Delay Before Grab", "ms", ParameterLayer.Config, () => _s.DelayBeforeGrabMs, v => _s.DelayBeforeGrabMs = v, min: 0);
            // 명령 포트
            yield return ParameterDescriptor.Int(t, "WaferVisionPort", "Wafer Vision Port", "", ParameterLayer.Config, () => _s.WaferVisionPort, v => _s.WaferVisionPort = v);
            yield return ParameterDescriptor.Int(t, "InspectionVisionPort", "Inspection Vision Port", "", ParameterLayer.Config, () => _s.InspectionVisionPort, v => _s.InspectionVisionPort = v);
            yield return ParameterDescriptor.Int(t, "BinVisionPort", "Bin Vision Port", "", ParameterLayer.Config, () => _s.BinVisionPort, v => _s.BinVisionPort = v);
            yield return ParameterDescriptor.Int(t, "FrontSideInspectionPort", "FrontSide Inspection Port", "", ParameterLayer.Config, () => _s.FrontSideInspectionPort, v => _s.FrontSideInspectionPort = v);
            yield return ParameterDescriptor.Int(t, "RearSideInspectionPort", "RearSide Inspection Port", "", ParameterLayer.Config, () => _s.RearSideInspectionPort, v => _s.RearSideInspectionPort = v);
            // 원격 뷰어
            yield return ParameterDescriptor.Bool(t, "RemoteViewerEnable", "Remote Viewer Enable", ParameterLayer.Config, () => _s.RemoteViewerEnable, v => _s.RemoteViewerEnable = v);
            yield return ParameterDescriptor.Text(t, "RemoteViewerSource", "Remote Viewer Source", ParameterLayer.Config, () => _s.RemoteViewerSource, v => _s.RemoteViewerSource = v);
            yield return ParameterDescriptor.Int(t, "RemoteViewerFps", "Remote Viewer FPS", "", ParameterLayer.Config, () => _s.RemoteViewerFps, v => _s.RemoteViewerFps = v, min: 1);
            yield return ParameterDescriptor.Int(t, "RemoteViewerQuality", "Remote Viewer Quality", "", ParameterLayer.Config, () => _s.RemoteViewerQuality, v => _s.RemoteViewerQuality = v, min: 1, max: 100);
            yield return ParameterDescriptor.Int(t, "WaferViewerPort", "Wafer Viewer Port", "", ParameterLayer.Config, () => _s.WaferViewerPort, v => _s.WaferViewerPort = v);
            yield return ParameterDescriptor.Int(t, "InspectionViewerPort", "Inspection Viewer Port", "", ParameterLayer.Config, () => _s.InspectionViewerPort, v => _s.InspectionViewerPort = v);
            yield return ParameterDescriptor.Int(t, "BinViewerPort", "Bin Viewer Port", "", ParameterLayer.Config, () => _s.BinViewerPort, v => _s.BinViewerPort = v);
            yield return ParameterDescriptor.Int(t, "FrontSideViewerPort", "FrontSide Viewer Port", "", ParameterLayer.Config, () => _s.FrontSideViewerPort, v => _s.FrontSideViewerPort = v);
            yield return ParameterDescriptor.Int(t, "RearSideViewerPort", "RearSide Viewer Port", "", ParameterLayer.Config, () => _s.RearSideViewerPort, v => _s.RearSideViewerPort = v);
            // 로그
            yield return ParameterDescriptor.Bool(t, "ImageLogEnable", "Image Log Enable", ParameterLayer.Config, () => _s.ImageLogEnable, v => _s.ImageLogEnable = v);
            yield return ParameterDescriptor.Text(t, "ImageLogPath", "Image Log Path", ParameterLayer.Config, () => _s.ImageLogPath, v => _s.ImageLogPath = v);
            yield return ParameterDescriptor.Bool(t, "DataLogEnable", "Data Log Enable", ParameterLayer.Config, () => _s.DataLogEnable, v => _s.DataLogEnable = v);
            yield return ParameterDescriptor.Text(t, "DataLogPath", "Data Log Path", ParameterLayer.Config, () => _s.DataLogPath, v => _s.DataLogPath = v);
            yield return ParameterDescriptor.Text(t, "MilDcfPath", "MIL DCF Path", ParameterLayer.Config, () => _s.MilDcfPath, v => _s.MilDcfPath = v);
        }
    }
}
