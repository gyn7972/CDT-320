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
        [DataMember] public int  Channel          { get; set; }       // 소속 알고리즘 Wiring.Channels 풀 내 값
        [DataMember] public int  Level            { get; set; }       // 0 ~ MaxPower
        [DataMember] public bool On               { get; set; } = true;
        [DataMember] public int  StrobeTimeUs     { get; set; } = 0;
        // Stage 68 #4 — 그랩 안정화 지연(ms). 0 = 대기 없음. 다음 런타임 Stage 가 사용. UI 디바운스와 무관.
        [DataMember] public int  StabilizeDelayMs { get; set; } = 0;

        public InspectionLightSetting Clone()
            => new InspectionLightSetting
            {
                Channel = Channel, Level = Level, On = On,
                StrobeTimeUs = StrobeTimeUs, StabilizeDelayMs = StabilizeDelayMs
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
