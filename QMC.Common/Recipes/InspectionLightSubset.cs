using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 69 — 검사 1개의 조명 채널 1개 설정 (Recipe 레이어).
    /// Controller/Page 는 노드 Setup.LightPages 지정 — 여기엔 채널 레벨 값만 보관(C3b-3).
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

    // (C3b-2) InspectionLightOverride(검사 1개의 조명 묶음)는 노드 Recipe(List<InspectionLightSetting>)가
    // SSOT 로 대체되어 제거. InspectionLightSetting(노드 Recipe 원소)은 유지.

    /// <summary>
    /// C3b-3 — 검사가 구동하는 (컨트롤러, 페이지) 지정 (노드 Setup 원소). "결선(채널 풀)" 개념 대체.
    /// 채널 열거 = 지정 컨트롤러의 LightControllerEntry.ChannelCount(1..N). 채널 사용 여부 = 레벨 0/양수(Recipe).
    /// </summary>
    [DataContract]
    public class LightPageRef
    {
        [DataMember] public string ControllerPort { get; set; }   // LightControllerEntry.PortName FK
        [DataMember] public int    Page           { get; set; }   // 0 ~ controller.PageCount-1

        /// <summary>이 지정이 사용하는 채널 목록(쉼표 구분, 예 "1,2"). 비우면 컨트롤러 전 채널(구버전 호환, 2026-07-06).
        /// 레시피 조명 그리드는 이 채널만 행 생성한다(모듈과 무관한 채널 숨김).</summary>
        [DataMember(EmitDefaultValue = false)] public string Channels { get; set; }

        /// <summary>Channels 파싱 — 유효 채널(1 이상) 오름차순 중복 제거. 비면 null(전체).</summary>
        public int[] ParseChannels()
        {
            if (string.IsNullOrWhiteSpace(Channels)) return null;
            var set = new System.Collections.Generic.SortedSet<int>();
            foreach (var tok in Channels.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(tok.Trim(), out int ch) && ch >= 1) set.Add(ch);
            if (set.Count == 0) return null;
            var arr = new int[set.Count]; set.CopyTo(arr); return arr;
        }

        public LightPageRef Clone() => new LightPageRef { ControllerPort = ControllerPort, Page = Page, Channels = Channels };
    }
}
