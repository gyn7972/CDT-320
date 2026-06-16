using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Stage 58 — 작업 메인 화면의 우상단 "작업 맵" 셀에 들어가는 라이브 다이맵 뷰.
    /// LotStorage.ActiveInputDieMap 을 1초마다 폴링해 작업 중인 Input Die Map 상태를 시각화한다.
    /// 활성 맵이 없으면 InputStage 자재 상태에서 복원 가능한 맵을 사용하고, 그래도 없으면 기본 격자를 표시한다.
    /// </summary>
    public class LiveLotMapView : Control
    {
        private System.Windows.Forms.Timer _refresh;
        // 그리드 캐시 — Lot 의 처리 카운트만큼 칸을 채운 가상 wafer
        private int _gridX = 5;
        private int _gridY = 5;

        public LiveLotMapView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(0xDD, 0xDD, 0xDD);
            DoubleBuffered = true;

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
                _refresh.Tick += (s, e) => Invalidate();
                _refresh.Start();
            }
        }

        /// <summary>그리드 크기 — 외부에서 Recipe.Frame.GridX 로 설정 가능.</summary>
        public int GridX { get { return _gridX; } set { _gridX = Math.Max(1, value); Invalidate(); } }
        public int GridY { get { return _gridY; } set { _gridY = Math.Max(1, value); Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);

            var lot = LotStorage.ActiveLot;
            var dmap = ResolveDisplayMap();
            MapStats stats = CalculateMapStats(dmap);
            // 헤더 텍스트
            using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
            using (var f  = new Font("Consolas", 9F, FontStyle.Bold))
            {
                string lotText = lot != null ? lot.LotID : "(no active lot)";
                string head = dmap != null
                    ? string.Format("LOT {0}  target={1}  done={2}  good={3}  ng={4}",
                                    lotText, stats.Target, stats.Done, stats.Good, stats.Ng)
                    : string.Format("LOT {0}  (no input die map)", lotText);
                g.DrawString(head, f, bf, 8, 4);
            }

            // 격자 크기 — 다이맵이 있으면 그 grid 사용, 없으면 default 5x5
            int gx = Math.Max(1, (dmap != null) ? dmap.GridX : _gridX);
            int gy = Math.Max(1, (dmap != null) ? dmap.GridY : _gridY);
            int margin = 8;
            int top    = 22;
            int availW = Math.Max(50, Width  - margin * 2);
            int availH = Math.Max(50, Height - top - margin);
            int cell   = Math.Max(1, Math.Min(availW / gx, availH / gy));
            int totalW = cell * gx;
            int totalH = cell * gy;
            int x0 = (Width - totalW) / 2;
            int y0 = top + (availH - totalH) / 2;

            // 다이맵 모드: Input Die Map의 실제 셀 상태(Result/BinCode)를 그대로 표시한다.
            if (dmap != null)
            {
                if (dmap.Entries != null)
                {
                    foreach (var entry in dmap.Entries)
                    {
                        if (entry == null || !entry.IsTarget)
                            continue;

                        int x = x0 + entry.GridX * cell;
                        int y = y0 + entry.GridY * cell;
                        Color c = ResolveEntryColor(entry);

                        using (var br = new SolidBrush(c))
                            g.FillRectangle(br, x, y, Math.Max(1, cell - 1), Math.Max(1, cell - 1));
                        if (cell >= 4)
                        {
                            using (var pen = new Pen(Color.FromArgb(0x55, 0x55, 0x55), 1f))
                                g.DrawRectangle(pen, x, y, cell - 1, cell - 1);
                        }
                    }
                }

                // 원형 윤곽 표시 (참고용)
                using (var pen = new Pen(Color.FromArgb(0x44, 0x88, 0xCC), 1.5f))
                    g.DrawEllipse(pen, x0, y0, totalW, totalH);
            }
            else
            {
                // 레거시 5x5 사각 격자 (다이맵 없을 때)
                int filled = 0;
                int goodFilled = 0;
                int processed = lot != null ? lot.ProcessedDies : 0;
                int good = lot != null ? lot.GoodCount : 0;
                for (int j = 0; j < gy; j++)
                {
                    for (int i = 0; i < gx; i++)
                    {
                        int x = x0 + i * cell;
                        int y = y0 + j * cell;

                        Color c;
                        if (filled < processed)
                        {
                            if (goodFilled < good)
                            {
                                c = BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin);
                                goodFilled++;
                            }
                            else
                            {
                                c = Color.IndianRed;
                            }
                            filled++;
                        }
                        else
                        {
                            c = Color.FromArgb(0xEE, 0xEE, 0xEE);
                        }

                        using (var br = new SolidBrush(c))
                            g.FillRectangle(br, x, y, cell - 1, cell - 1);
                        using (var pen = new Pen(Color.FromArgb(0xAA, 0xAA, 0xAA), 1f))
                            g.DrawRectangle(pen, x, y, cell - 1, cell - 1);
                    }
                }
                using (var pen = new Pen(Color.FromArgb(0x66, 0x66, 0x66), 2f))
                    g.DrawRectangle(pen, x0 - 1, y0 - 1, totalW + 1, totalH + 1);
            }

            // 우하단 진행률
            int percentDone = dmap != null ? stats.Target : (lot != null ? lot.TotalDies : 0);
            int percentProcessed = dmap != null ? stats.Done : (lot != null ? lot.ProcessedDies : 0);
            if (percentDone > 0)
            {
                using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
                using (var f  = new Font("Consolas", 9F))
                {
                    string pct = string.Format("{0:F1} %", percentProcessed * 100.0 / percentDone);
                    var sz = g.MeasureString(pct, f);
                    g.DrawString(pct, f, bf, Width - sz.Width - 8, Height - sz.Height - 4);
                }
            }
        }

        private static DieMap ResolveDisplayMap()
        {
            DieMap map = LotStorage.ActiveInputDieMap;
            if (map != null)
            {
                DieMapGenerator.Normalize(map);
                return map;
            }

            try
            {
                map = MaterialStateService.BuildInputDieMapFromStageWafer();
                if (map != null)
                {
                    DieMapGenerator.Normalize(map);
                    LotStorage.ActiveInputDieMap = map;
                }
            }
            catch
            {
            }

            return map;
        }

        private static Color ResolveEntryColor(DieMapEntry entry)
        {
            if (entry == null || !entry.IsTarget)
                return Color.FromArgb(0x66, 0x66, 0x66);

            if (entry.Result == DieResult.Good)
                return BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin);

            if (entry.Result == DieResult.NG)
            {
                int binCode = entry.BinCode > 0 ? entry.BinCode : BinCodeMap.MaxBin;
                Color c = BinCodeMap.ConvertToBinCodeColor(binCode);
                return c.ToArgb() == Color.Black.ToArgb() ? Color.IndianRed : c;
            }

            if (entry.BinCode > 0)
                return BinCodeMap.ConvertToBinCodeColor(entry.BinCode);

            return Color.FromArgb(0xCC, 0xDD, 0xEE);
        }

        private static MapStats CalculateMapStats(DieMap map)
        {
            var stats = new MapStats();
            if (map == null || map.Entries == null)
                return stats;

            foreach (var entry in map.Entries)
            {
                if (entry == null || !entry.IsTarget)
                    continue;

                stats.Target++;
                if (entry.Result == DieResult.Good)
                {
                    stats.Good++;
                    stats.Done++;
                }
                else if (entry.Result == DieResult.NG)
                {
                    stats.Ng++;
                    stats.Done++;
                }
            }

            return stats;
        }

        private struct MapStats
        {
            public int Target;
            public int Done;
            public int Good;
            public int Ng;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
