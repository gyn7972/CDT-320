using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 69 — 검사 1개의 조명 채널 1개 설정 (Recipe 레이어).
    /// Controller/Page 는 Setup 의 AlgorithmLightWiring 에서 추론 — 여기엔 값만 보관.
    /// </summary>
    [DataContract]
    public class InspectionLightSetting
    {
        // Stage 81 — 다중 컨트롤러 구분 (같은 채널 번호가 두 컨트롤러에 모두 있을 수 있어 Channel 만으론 모호).
        [DataMember(EmitDefaultValue = false)] public string ControllerPort { get; set; }
        [DataMember] public int  Channel          { get; set; }       // 소속 알고리즘 Wiring.Channels 풀 내 값
        [DataMember] public int  Level            { get; set; }       // 0 ~ MaxPower
        [DataMember] public bool On               { get; set; } = true;
        [DataMember] public int  StrobeTimeUs     { get; set; } = 0;
        // Stage 68 #4 — 그랩 안정화 지연(ms). 0 = 대기 없음. 다음 런타임 Stage 가 사용. UI 디바운스와 무관.
        [DataMember] public int  StabilizeDelayMs { get; set; } = 0;
        // Stage 70 — 페이지 선택 (Recipe 측으로 이동). 0 ~ controller.PageCount-1. PageCount==1 이면 0 고정.
        [DataMember(EmitDefaultValue = false)] public int Page { get; set; } = 0;

        public InspectionLightSetting Clone()
            => new InspectionLightSetting
            {
                ControllerPort = ControllerPort, Channel = Channel, Level = Level, On = On,
                StrobeTimeUs = StrobeTimeUs, StabilizeDelayMs = StabilizeDelayMs, Page = Page
            };
    }

    /// <summary>Stage 69 — 검사 1개의 조명 매핑 (풀 내 N채널 동시).</summary>
    [DataContract]
    public class InspectionLightOverride
    {
        [DataMember] public string InspectionId { get; set; } = "";
        [DataMember] public List<InspectionLightSetting> Settings { get; set; } = new List<InspectionLightSetting>();

        /// <summary>설정이 하나도 없으면 조명 미사용 (직렬화 정리 대상).</summary>
        public bool IsEmpty() => Settings == null || Settings.Count == 0;

        public InspectionLightOverride Clone()
        {
            var c = new InspectionLightOverride { InspectionId = InspectionId, Settings = new List<InspectionLightSetting>() };
            if (Settings != null) foreach (var s in Settings) c.Settings.Add(s.Clone());
            return c;
        }
    }
}
