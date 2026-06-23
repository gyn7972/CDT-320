using System;
using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 모듈별 최근 검사 결과 저장 — 시퀀서/핸들러가 INSPECT 할 때 기록하고,
    /// 작업 모니터링 뷰(OperationPage)가 판정(OK/NG) + 결과 라인을 오버레이로 표시한다.
    /// inspector 별 최신 결과를 보관(같은 inspector 재실행 시 갱신).
    /// </summary>
    public static class ModuleResultStore
    {
        private class R { public bool Pass; public string Items; public DateTime Time; }
        private class Mk { public double X; public double Y; public double Score; }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, Dictionary<string, R>> _map =
            new Dictionary<string, Dictionary<string, R>>(StringComparer.OrdinalIgnoreCase);
        // 모듈 → (finder/inspector 키 → 최근 검출 마크). 오버레이로 핸들러에 전송.
        private static readonly Dictionary<string, Dictionary<string, Mk>> _marks =
            new Dictionary<string, Dictionary<string, Mk>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>검사 결과 기록(module, inspectorId, 합부, 항목문자열).</summary>
        public static void Record(string module, string inspId, bool pass, string items)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock)
            {
                if (!_map.TryGetValue(module, out var d))
                {
                    d = new Dictionary<string, R>(StringComparer.OrdinalIgnoreCase);
                    _map[module] = d;
                }
                d[inspId ?? ""] = new R { Pass = pass, Items = items ?? "", Time = DateTime.Now };
            }
        }

        /// <summary>모듈의 누적 판정(전부 OK면 true) + 결과 라인. 결과 없으면 false 반환.</summary>
        public static bool TryGet(string module, out bool allPass, out string[] lines)
        {
            allPass = true; lines = null;
            if (string.IsNullOrEmpty(module)) return false;
            lock (_lock)
            {
                if (!_map.TryGetValue(module, out var d) || d.Count == 0) return false;
                var list = new List<string>();
                foreach (var kv in d)
                {
                    if (!kv.Value.Pass) allPass = false;
                    string items = string.IsNullOrEmpty(kv.Value.Items) ? "" : "  " + kv.Value.Items;
                    list.Add($"[{kv.Key}] {(kv.Value.Pass ? "OK" : "NG")}{items}");
                }
                lines = list.ToArray();
            }
            return true;
        }

        /// <summary>검출 마크 1개 기록(module, finder/inspector 키, 이미지 좌표 x/y, score). 오버레이 표시용.</summary>
        public static void RecordMark(string module, string key, double x, double y, double score)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock)
            {
                if (!_marks.TryGetValue(module, out var d))
                {
                    d = new Dictionary<string, Mk>(StringComparer.OrdinalIgnoreCase);
                    _marks[module] = d;
                }
                d[key ?? ""] = new Mk { X = x, Y = y, Score = score };
            }
        }

        /// <summary>모듈의 최근 검출 마크들(이미지 좌표). 없으면 null.</summary>
        public static QMC.Common.Ui.Controls.FrameMark[] GetMarks(string module)
        {
            if (string.IsNullOrEmpty(module)) return null;
            lock (_lock)
            {
                if (!_marks.TryGetValue(module, out var d) || d.Count == 0) return null;
                var arr = new QMC.Common.Ui.Controls.FrameMark[d.Count];
                int i = 0;
                foreach (var v in d.Values)
                    arr[i++] = new QMC.Common.Ui.Controls.FrameMark { X = v.X, Y = v.Y, Score = v.Score };
                return arr;
            }
        }

        /// <summary>모듈 결과 초기화.</summary>
        public static void Clear(string module)
        {
            if (string.IsNullOrEmpty(module)) return;
            lock (_lock) { _map.Remove(module); _marks.Remove(module); }
        }
    }
}
