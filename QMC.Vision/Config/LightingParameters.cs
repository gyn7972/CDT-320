using System.Collections.Generic;
using QMC.Common.Recipes;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Config
{
    /// <summary>
    /// P1 — InspectionLightSetting(검사별 조명 채널 1개) 디스크립터 어댑터. ParameterTarget=&lt;InspectionId&gt;.
    /// 계층(#7 분리): Level/On/Strobe/StabilizeDelay/Page=Recipe(제품별 조명) / Channel/ControllerPort(결선)=Setup(HW 배선).
    /// QMC.Common 무수정 — Vision 측 어댑터. (채널 식별을 위해 Key 에 ControllerPort/Channel 접두.)
    /// </summary>
    public sealed class LightingParameters : IParameterProvider
    {
        private readonly InspectionLightSetting _s;
        private readonly string _inspectionId;
        public LightingParameters(string inspectionId, InspectionLightSetting s)
        {
            _inspectionId = inspectionId; _s = s;
        }

        public string ParameterTarget => _inspectionId ?? "";

        public IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            string pfx = (_s.ControllerPort ?? "") + "/" + _s.Channel + ".";   // 다채널 구분 키 접두
            // #7 결선 → Setup
            yield return ParameterDescriptor.Text(t, pfx + "ControllerPort", "Controller Port", ParameterLayer.Setup, () => _s.ControllerPort, v => _s.ControllerPort = v);
            yield return ParameterDescriptor.Int(t, pfx + "Channel", "Channel", "", ParameterLayer.Setup, () => _s.Channel, v => _s.Channel = v, min: 0);
            // 조명 값 → Recipe
            yield return ParameterDescriptor.Int(t, pfx + "Level", "Level", "", ParameterLayer.Recipe, () => _s.Level, v => _s.Level = v, min: 0);
            yield return ParameterDescriptor.Bool(t, pfx + "On", "On", ParameterLayer.Recipe, () => _s.On, v => _s.On = v);
            yield return ParameterDescriptor.Int(t, pfx + "StrobeTimeUs", "Strobe Time", "us", ParameterLayer.Recipe, () => _s.StrobeTimeUs, v => _s.StrobeTimeUs = v, min: 0);
            yield return ParameterDescriptor.Int(t, pfx + "StabilizeDelayMs", "Stabilize Delay", "ms", ParameterLayer.Recipe, () => _s.StabilizeDelayMs, v => _s.StabilizeDelayMs = v, min: 0);
            yield return ParameterDescriptor.Int(t, pfx + "Page", "Page", "", ParameterLayer.Recipe, () => _s.Page, v => _s.Page = v, min: 0);
        }
    }
}
