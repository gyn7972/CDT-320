using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사 오버레이 기하(측면 칩핑) 모듈명별 보관 — 작업 모니터링 뷰가 레시피 INSPECT 페이지와
    /// 동일한 오버레이(상/하 실제 에지 프로파일 + 검출 기준선)를 그리도록 한다.
    /// 결함 박스/판정/결과라인은 기존 <see cref="MatchOverlayStore"/>/<see cref="ModuleResultStore"/>가 담당.
    /// </summary>
    public static class InspectionOverlayStore
    {
        /// <summary>검사 종류 — 종류별 전용 오버레이 분기용(모든 Inspection 이 동일 구조로 자기 오버레이 보유).</summary>
        public enum OverlayKind { None, Bottom, Side, Bin }

        public sealed class Geom
        {
            public OverlayKind Kind = OverlayKind.Side;   // 기본 Side(기존 호환)
            // 공통(절대 이미지 좌표)
            public PointF[] Corners;                       // 검출 박스 4점(Bottom/Bin 다이/배치 박스)
            public List<DefectMark> Defects;               // 종류별 결함(Type=Chipping/Foreign 등)
            public string  Caption;                        // 사이즈/각도 라벨(예 "W 3.50 H 2.50 θ0.0")
            // Side 전용
            public PointF[] TopProfile;   // 상단 실제 에지 프로파일(절대 이미지 좌표)
            public PointF[] BotProfile;   // 하단 실제 에지 프로파일
            public PointF[] RefCorners;   // 4점: [0]-[1]=상 기준선, [3]-[2]=하 기준선
            public bool     Pass;
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Geom> _map =
            new Dictionary<string, Geom>(System.StringComparer.OrdinalIgnoreCase);

        public static void Record(string module, Geom g)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) { _map[module] = g; }
        }

        public static bool TryGet(string module, out Geom g)
        {
            g = null;
            if (string.IsNullOrEmpty(module)) return false;
            lock (_lock) { return _map.TryGetValue(module, out g) && g != null; }
        }

        public static void Clear(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) { _map.Remove(module); }
        }
    }
}
