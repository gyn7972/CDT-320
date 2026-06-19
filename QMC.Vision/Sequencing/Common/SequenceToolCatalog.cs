using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 모듈별 도구(Finder/Inspector) 실행 순서의 단일 소스(SSOT).
    /// CDT-310 검사 흐름을 도구 단위로 정의한다. GRAB 은 각 도구가 자체 수행하므로 목록에 포함하지 않는다.
    /// 모듈 사이클(<see cref="ModuleSequenceBase"/>)과 도구 개별 시퀀서(<see cref="ToolSequence"/>)가
    /// 모두 이 목록을 참조하여 순서 정의가 한 곳에만 존재하도록 한다.
    /// 항목 Key=명령("MATCH"/"INSPECT"), Value=도구 Id.
    /// </summary>
    public static class SequenceToolCatalog
    {
        private static readonly KeyValuePair<string, string>[] _empty
            = new KeyValuePair<string, string>[0];

        private static KeyValuePair<string, string> T(string cmd, string id)
            => new KeyValuePair<string, string>(cmd, id);

        // CDT-310 흐름(모듈별). GRAB 제외 — 각 도구가 자체 그랩.
        private static readonly Dictionary<SequenceModuleKind, KeyValuePair<string, string>[]> _map
            = new Dictionary<SequenceModuleKind, KeyValuePair<string, string>[]>
        {
            [SequenceModuleKind.WaferVision] = new[]
            {
                T("MATCH", "EjectPinFinder"),
                T("MATCH", "ReticleFinder"),
                T("MATCH", "FirstReferenceFinder"),
                T("MATCH", "SecondReferenceFinder"),
                T("MATCH", "AlignDieFinder"),
                T("MATCH", "DieFinder"),
                T("MATCH", "ScaleFinder"),
            },
            [SequenceModuleKind.BinVision] = new[]
            {
                T("MATCH",   "ReticleFinder"),
                T("MATCH",   "DieFinder"),
                T("INSPECT", "PlacementInspector"),
                T("MATCH",   "ScaleFinder"),
            },
            [SequenceModuleKind.BottomInspection] = new[]
            {
                T("MATCH",   "ReticleFinder"),
                T("MATCH",   "ColletFinder"),
                T("MATCH",   "DieFinder"),
                T("INSPECT", "SurfaceInspector"),
                T("MATCH",   "FocusFinder"),
                T("MATCH",   "ScaleFinder"),
                T("MATCH",   "DistortionCompensation"),
            },
            [SequenceModuleKind.TopSideVision] = new[]
            {
                T("MATCH",   "DieEdgeFinder"),
                T("INSPECT", "TopSurfaceInspector"),
                T("INSPECT", "TopChippingInspector"),
                T("MATCH",   "FocusFinder"),
            },
            [SequenceModuleKind.BottomSideVision] = new[]
            {
                T("MATCH",   "DieEdgeFinder"),
                T("INSPECT", "BottomSurfaceInspector"),
                T("INSPECT", "BottomChippingInspector"),
                T("MATCH",   "FocusFinder"),
            },
        };

        /// <summary>모듈 종류의 도구 순서(GRAB 제외). 정의 없으면 빈 목록.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tools(SequenceModuleKind kind)
            => _map.TryGetValue(kind, out var arr) ? arr : _empty;

        /// <summary>해당 모듈 종류에 도구 순서 정의가 있는지.</summary>
        public static bool Has(SequenceModuleKind kind) => _map.ContainsKey(kind);
    }
}
