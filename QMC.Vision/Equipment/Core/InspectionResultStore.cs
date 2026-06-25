using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 검사 결과 구조화 스토어 — 모드(Bottom/Side/Bin)별로 per-picker 최신 결과 + per-die 누적 이력을 보관.
    /// 검사 실행부(VisionCommandCore.InspectOnImage, 수동 테스트)가 Record 로 밀어넣고,
    /// 작업화면 뷰어(InspectionViewerControl)가 Changed 를 구독해 Picker 이미지/추세 차트/결과 그리드를 갱신한다.
    /// 모드 키는 문자열("Bottom"/"Side"/"Bin")로 두어 UI enum 과의 결합을 피한다.
    /// </summary>
    public static class InspectionResultStore
    {
        public const string Bottom = "Bottom";
        public const string Side   = "Side";
        public const string Bin    = "Bin";

        /// <summary>검사 결과 1건(한 칩/픽커).</summary>
        public class Item
        {
            public string Mode;
            public int Picker;              // 1~4 (0=미지정)
            public int Channel = -1;        // Side 채널 0~3(Front ch1/2, Back ch1/2), -1=단일
            public int IndexX, IndexY;
            public bool Pass;
            public Dictionary<string, double> Values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public string[] Lines;          // 패널 표시 텍스트
            public PointF[] Box;            // 검출 박스(이미지 px)
            public List<DefectMark> Defects;
            public Bitmap Image;            // 표시 이미지(클론 보관)
            public DateTime Time = DateTime.Now;
        }

        private const int MaxHistory = 300;
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, List<Item>> _history =
            new Dictionary<string, List<Item>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Item[]> _latest =
            new Dictionary<string, Item[]>(StringComparer.OrdinalIgnoreCase);
        // 모드 → [picker 1..4][channel 0..3] 최신(Side 4채널 뷰어 바인딩)
        private static readonly Dictionary<string, Item[][]> _latestCh =
            new Dictionary<string, Item[][]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>모드별 변경 통지(UI 스레드 마샬링은 구독자 책임).</summary>
        public static event Action<string> Changed;

        /// <summary>결과 1건 기록. picker(1~4)면 per-picker 최신도 갱신.</summary>
        public static void Record(Item it)
        {
            if (it == null || string.IsNullOrEmpty(it.Mode)) return;
            lock (_lock)
            {
                if (!_history.TryGetValue(it.Mode, out var list))
                {
                    list = new List<Item>(); _history[it.Mode] = list;
                }
                list.Add(it);
                if (list.Count > MaxHistory) list.RemoveRange(0, list.Count - MaxHistory);

                if (it.Picker >= 1 && it.Picker <= 4)
                {
                    if (!_latest.TryGetValue(it.Mode, out var arr))
                    {
                        arr = new Item[5]; _latest[it.Mode] = arr;
                    }
                    var old = arr[it.Picker];
                    arr[it.Picker] = it;
                    if (old != null && !ReferenceEquals(old.Image, it.Image)) { try { old.Image?.Dispose(); } catch { } }

                    // Side 채널별 최신
                    if (it.Channel >= 0 && it.Channel <= 3)
                    {
                        if (!_latestCh.TryGetValue(it.Mode, out var grid))
                        {
                            grid = new Item[5][];
                            for (int p = 0; p < 5; p++) grid[p] = new Item[4];
                            _latestCh[it.Mode] = grid;
                        }
                        var oldc = grid[it.Picker][it.Channel];
                        grid[it.Picker][it.Channel] = it;
                        if (oldc != null && !ReferenceEquals(oldc.Image, it.Image) && !ReferenceEquals(oldc, old))
                        { try { oldc.Image?.Dispose(); } catch { } }
                    }
                }
            }
            try { Changed?.Invoke(it.Mode); } catch { }
        }

        /// <summary>해당 모드 picker(1~4)·channel(0~3)의 최신 결과(없으면 null).</summary>
        public static Item LatestChannel(string mode, int picker, int channel)
        {
            lock (_lock)
            {
                return (_latestCh.TryGetValue(mode, out var grid) && picker >= 1 && picker <= 4 && channel >= 0 && channel <= 3)
                    ? grid[picker][channel] : null;
            }
        }

        /// <summary>해당 모드 picker(1~4)의 최신 결과(없으면 null).</summary>
        public static Item Latest(string mode, int picker)
        {
            lock (_lock)
            {
                return (_latest.TryGetValue(mode, out var arr) && picker >= 1 && picker <= 4) ? arr[picker] : null;
            }
        }

        /// <summary>해당 모드 이력 스냅샷(오래된→최신).</summary>
        public static List<Item> History(string mode)
        {
            lock (_lock)
            {
                return _history.TryGetValue(mode, out var list) ? new List<Item>(list) : new List<Item>();
            }
        }

        /// <summary>해당 모드의 특정 지표 시계열(차트용).</summary>
        public static double[] Series(string mode, string key)
        {
            lock (_lock)
            {
                if (!_history.TryGetValue(mode, out var list)) return new double[0];
                return list.Where(i => i.Values.ContainsKey(key)).Select(i => i.Values[key]).ToArray();
            }
        }

        public static void Clear(string mode)
        {
            lock (_lock)
            {
                if (_history.TryGetValue(mode, out var list)) list.Clear();
                if (_latest.TryGetValue(mode, out var arr))
                    for (int i = 0; i < arr.Length; i++) { try { arr[i]?.Image?.Dispose(); } catch { } arr[i] = null; }
                if (_latestCh.TryGetValue(mode, out var grid))
                    for (int p = 0; p < grid.Length; p++)
                        for (int c = 0; c < grid[p].Length; c++) grid[p][c] = null;
            }
            try { Changed?.Invoke(mode); } catch { }
        }

        /// <summary>모듈/검사기 id → 모드 키(Bottom/Side/Bin). 매칭 없으면 null.</summary>
        public static string ModeOf(string moduleOrId)
        {
            if (string.IsNullOrEmpty(moduleOrId)) return null;
            bool Has(string k) => moduleOrId.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0;
            if (Has("Placement") || Has("Bin")) return Bin;
            if (Has("Side") || Has("Chipping")) return Side;
            if (Has("Bottom") || Has("Surface")) return Bottom;
            return null;
        }

        /// <summary>InspectionResult → 스토어 Item 변환(Items 의 숫자값을 Values 로 파싱).</summary>
        public static Item FromResult(string mode, int picker, int ix, int iy, InspectionResult r, Bitmap image)
            => FromResult(mode, picker, -1, ix, iy, r, image);

        /// <summary>채널 지정 변환(Side 4채널: channel 0~3, 그 외 -1).</summary>
        public static Item FromResult(string mode, int picker, int channel, int ix, int iy, InspectionResult r, Bitmap image)
        {
            var it = new Item { Mode = mode, Picker = picker, Channel = channel, IndexX = ix, IndexY = iy, Pass = r != null && r.IsPass };
            var lines = new List<string>();
            if (r?.Items != null)
                foreach (var item in r.Items)
                {
                    lines.Add(item.Name + " : " + item.Value);
                    if (double.TryParse(item.Value, out double v)) it.Values[item.Name] = v;
                }
            it.Lines = lines.ToArray();
            it.Defects = r?.Defects;
            if (image != null) { try { it.Image = (Bitmap)image.Clone(); } catch { } }
            return it;
        }
    }
}
