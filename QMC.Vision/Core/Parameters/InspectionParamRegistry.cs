using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// P3 (G3) — ② InspectionParameters ↔ 정규 target 단일 매핑 레지스트리.
    /// 옛 문자열 tool 키("BottomInspection" 등)·정규 target·② 팩토리를 1곳에서 정의.
    /// SettingsPage/ParameterEditorHost 의 문자열 분기와 부트스트랩의 하드코딩 ② 생성을 대체.
    /// </summary>
    public sealed class InspectionParamEntry
    {
        public string Tool { get; }      // 옛 SettingsPage tool 키
        public string Target { get; }    // 정규 id (= ②.ParameterTarget)
        public Func<InspectionParametersBase> Create { get; }
        public bool Orphan { get; }      // true = 소비 백엔드 없음(정의/편집/저장만)

        public InspectionParamEntry(string tool, string target, Func<InspectionParametersBase> create, bool orphan)
        {
            Tool = tool; Target = target; Create = create; Orphan = orphan;
        }
    }

    public static class InspectionParamRegistry
    {
        public static readonly IReadOnlyList<InspectionParamEntry> Entries = new[]
        {
            new InspectionParamEntry("BottomInspection", "BottomInspection/SurfaceInspector",      () => new BottomInspectionParameters(), false),
            new InspectionParamEntry("Distortion",       "BottomInspection/DistortionCompensation",() => new DistortionParameters(),       false),
            new InspectionParamEntry("VisionScale",      "VisionScale",                            () => new VisionScaleParameters(),      true),
            new InspectionParamEntry("SideInspection",   "SideInspection/?",                       () => new SideInspectionParameters(),   true),
            new InspectionParamEntry("DieGapInspection", "DieGapInspection/?",                     () => new DieGapInspectionParameters(), true),
        };

        public static InspectionParamEntry ByTool(string tool)
            => Entries.FirstOrDefault(e => string.Equals(e.Tool, tool, StringComparison.OrdinalIgnoreCase));

        public static InspectionParamEntry ByTarget(string target)
            => Entries.FirstOrDefault(e => string.Equals(e.Target, target, StringComparison.OrdinalIgnoreCase));
    }
}
