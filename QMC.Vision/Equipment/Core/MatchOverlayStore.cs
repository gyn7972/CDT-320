using System;
using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 모듈별 최근 MATCH(패턴 검출) 결과의 오버레이 데이터(찾은 위치/각/박스 + 검색 ROI)를 보관.
    /// <see cref="VisionCommandCore.Match"/> 가 기록하고, 원격 뷰어 metaProvider 가 읽어
    /// 프레임 메타(<c>VisionFrameMeta.Marks/Roi</c>)에 실어 핸들러 뷰어가 오버레이로 그린다.
    /// 좌표는 모두 이미지 px(원본 프레임 기준). 같은 모듈 재검출 시 최신으로 덮어쓴다.
    /// </summary>
    public static class MatchOverlayStore
    {
        public struct Mark
        {
            public double X, Y;        // 검출 중심(이미지 px)
            public double Angle;       // 검출 회전각(deg)
            public double Score;       // 매칭 점수
            public double BoxW, BoxH;  // 매칭 박스 크기(이미지 px, 0=박스 미표시)
        }

        public struct Overlay
        {
            public Mark[]   Marks;
            public double   RoiX, RoiY, RoiW, RoiH;   // 검색 ROI(이미지 px, 좌상단 기준)
            public DateTime Time;
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Overlay> _map =
            new Dictionary<string, Overlay>(StringComparer.OrdinalIgnoreCase);

        public static void Record(string module, Mark[] marks,
                                  double roiX, double roiY, double roiW, double roiH)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock)
                _map[module] = new Overlay
                {
                    Marks = marks ?? new Mark[0],
                    RoiX = roiX, RoiY = roiY, RoiW = roiW, RoiH = roiH,
                    Time = DateTime.Now
                };
        }

        public static bool TryGet(string module, out Overlay overlay)
        {
            overlay = default(Overlay);
            if (string.IsNullOrEmpty(module)) return false;
            lock (_lock) return _map.TryGetValue(module, out overlay);
        }

        public static void Clear(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) _map.Remove(module);
        }
    }
}
