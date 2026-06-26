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

        /// <summary>다이 1개(Index X/Y) 단위 집계 — 실제 운영뷰처럼 한 다이에 Front/Back max 칩핑을 모은다.
        /// 채널(0~1=Front, 2~3=Back) 결과가 들어올 때마다 해당 측 max 갱신. 그리드/추세 차트가 이걸 읽는다.</summary>
        public sealed class DieRecord
        {
            public int Picker, IndexX, IndexY;
            public double FrontMax, BackMax;
            public bool HasFront, HasBack;
            public DateTime Time = DateTime.Now;
        }

        private const int MaxHistory = 300;
        private static readonly object _lock = new object();
        // 모드 → (다이키 → 집계) + 입력순서 리스트(차트 X축)
        private static readonly Dictionary<string, Dictionary<long, DieRecord>> _dieMap =
            new Dictionary<string, Dictionary<long, DieRecord>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<DieRecord>> _dieOrder =
            new Dictionary<string, List<DieRecord>>(StringComparer.OrdinalIgnoreCase);
        private static long DieKey(int ix, int iy) => ((long)ix << 32) ^ (uint)iy;
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

                // 다이 단위 집계(측면 칩핑) — Front(채널0~1)/Back(채널2~3) max 누적.
                if (it.Channel >= 0 && it.Channel <= 3 && it.Values.TryGetValue("Max Chipping Depth", out double mc))
                {
                    if (!_dieMap.TryGetValue(it.Mode, out var map))
                    { map = new Dictionary<long, DieRecord>(); _dieMap[it.Mode] = map; _dieOrder[it.Mode] = new List<DieRecord>(); }
                    long key = DieKey(it.IndexX, it.IndexY);
                    if (!map.TryGetValue(key, out var die))
                    {
                        die = new DieRecord { IndexX = it.IndexX, IndexY = it.IndexY, Picker = it.Picker };
                        map[key] = die;
                        var ord = _dieOrder[it.Mode];
                        ord.Add(die);
                        if (ord.Count > MaxHistory) { var rm = ord[0]; ord.RemoveAt(0); map.Remove(DieKey(rm.IndexX, rm.IndexY)); }
                    }
                    die.Picker = it.Picker; die.Time = DateTime.Now;
                    if (it.Channel <= 1) { die.FrontMax = Math.Max(die.FrontMax, mc); die.HasFront = true; }
                    else                 { die.BackMax  = Math.Max(die.BackMax,  mc); die.HasBack  = true; }
                }
            }
            try { Changed?.Invoke(it.Mode); } catch { }
        }

        /// <summary>해당 모드의 다이 집계 목록(입력순=차트 X축). 그리드 한 행=한 다이.</summary>
        public static List<DieRecord> Dies(string mode)
        {
            lock (_lock) { return _dieOrder.TryGetValue(mode, out var list) ? new List<DieRecord>(list) : new List<DieRecord>(); }
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

        /// <summary>채널 범위[chMin..chMax]로 필터한 지표 시계열(측면 Front=0~1 / Back=2~3 분리 차트용).</summary>
        public static double[] Series(string mode, string key, int chMin, int chMax)
        {
            lock (_lock)
            {
                if (!_history.TryGetValue(mode, out var list)) return new double[0];
                return list.Where(i => i.Channel >= chMin && i.Channel <= chMax && i.Values.ContainsKey(key))
                           .Select(i => i.Values[key]).ToArray();
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
                if (_dieMap.TryGetValue(mode, out var dm)) dm.Clear();
                if (_dieOrder.TryGetValue(mode, out var dord)) dord.Clear();
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
            // 표시용 이미지는 썸네일로 축소 보관(원본 12000²=432MB → OOM 방지). 뷰어 픽커 패널은 작아 충분.
            // 박스/결함 좌표도 같은 배율로 축소해 이미지와 정합. 원본(r.Defects)은 보존(클론 축소).
            if (image != null)
            {
                try
                {
                    int f = ThumbFactor(image.Width, image.Height, 1600);
                    it.Image = MakeThumb(image, f);
                    if (f > 1)
                    {
                        if (it.Defects != null)
                        {
                            var sc = new List<DefectMark>(it.Defects.Count);
                            foreach (var d in it.Defects)
                                sc.Add(new DefectMark { X = d.X / f, Y = d.Y / f, Width = d.Width / f, Height = d.Height / f, Area = d.Area, Type = d.Type });
                            it.Defects = sc;
                        }
                        if (it.Box != null)
                        {
                            var bx = new PointF[it.Box.Length];
                            for (int i = 0; i < bx.Length; i++) bx[i] = new PointF(it.Box[i].X / f, it.Box[i].Y / f);
                            it.Box = bx;
                        }
                    }
                }
                catch { }
            }
            return it;
        }

        private static int ThumbFactor(int w, int h, int maxDim)
        {
            int f = 1; while (w / f > maxDim || h / f > maxDim) f++;
            return f;
        }

        /// <summary>정수배 축소 썸네일(24bpp). f<=1 이면 단순 클론.</summary>
        private static Bitmap MakeThumb(Bitmap src, int f)
        {
            if (f <= 1) return (Bitmap)src.Clone();
            int tw = Math.Max(1, src.Width / f), th = Math.Max(1, src.Height / f);
            var bmp = new Bitmap(tw, th, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, tw, th), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
            }
            return bmp;
        }
    }
}
