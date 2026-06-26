using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 운영뷰 추세 차트의 상/하한(Limit) 기준값 보관 — 모드(Bottom/Side/Bin) · 차트(0/1)별.
    /// 검사기가 검사 실행 시 자신의 레시피 스펙(상/하한)을 <see cref="Set"/> 로 밀어넣고,
    /// 작업화면 뷰어가 <see cref="TryGet"/> 으로 읽어 차트 점선/축범위에 사용한다(하드코딩 제거 → 레시피로 설정).
    /// 값이 없으면(검사 전) 뷰어는 기존 기본값으로 폴백.
    /// </summary>
    public static class ChartLimitStore
    {
        public struct Limit { public double Upper, Lower; public bool Set; }

        private static readonly object _lock = new object();
        // key = mode + "#" + which(0/1)
        private static readonly Dictionary<string, Limit> _map =
            new Dictionary<string, Limit>(System.StringComparer.OrdinalIgnoreCase);

        private static string Key(string mode, int which) => (mode ?? "") + "#" + which;

        public static void Set(string mode, int which, double upper, double lower)
        {
            if (string.IsNullOrEmpty(mode)) return;
            lock (_lock) { _map[Key(mode, which)] = new Limit { Upper = upper, Lower = lower, Set = true }; }
        }

        public static bool TryGet(string mode, int which, out double upper, out double lower)
        {
            upper = 0; lower = 0;
            lock (_lock)
            {
                if (_map.TryGetValue(Key(mode, which), out var l) && l.Set)
                { upper = l.Upper; lower = l.Lower; return true; }
            }
            return false;
        }
    }
}
