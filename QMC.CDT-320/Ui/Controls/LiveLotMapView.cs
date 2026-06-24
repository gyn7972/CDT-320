using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Bin;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Pages;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 작업 메인 화면 우상단 "Input Wafer Map" 라이브 뷰.
    /// <list type="bullet">
    ///   <item><description>표시 기준은 LotStorage.ActiveInputDieMap → 없으면 InputStage Wafer 복원.</description></item>
    ///   <item><description>OnPaint 는 캐시된 맵만 그린다. 무거운 복원/Normalize/전역 쓰기를 Paint 에서 하지 않는다.</description></item>
    ///   <item><description>MaterialStateService.StateChanged / LotStorage.ActiveLotChanged 로 dirty flag 만 세우고,
    ///   100ms UI Timer 가 signature 비교 후 변경분만 Invalidate 한다.</description></item>
    /// </list>
    /// </summary>
    public class LiveLotMapView : Control
    {
        private const int RefreshIntervalMs = 100;

        private System.Windows.Forms.Timer _refresh;
        private int _gridX = 5;
        private int _gridY = 5;

        // 이벤트(비-UI 스레드)는 이 플래그만 세운다. 실제 반영은 UI Timer 에서 수행.
        private int _dirty = 1;
        private bool _eventsHooked;

        // UI Timer 가 갱신하는 표시 캐시 (OnPaint 는 이 값만 사용).
        private DieMap _displayMap;
        private MapStats _stats;
        private string _lotText = "(no active lot)";
        private long _signature = long.MinValue;

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
                HookStateEvents();

                _refresh = new System.Windows.Forms.Timer { Interval = RefreshIntervalMs };
                _refresh.Tick += OnRefreshTick;
            }
        }

        /// <summary>그리드 크기 — 외부에서 Recipe.Frame.GridX 로 설정 가능.</summary>
        public int GridX { get { return _gridX; } set { _gridX = Math.Max(1, value); MarkDirty(); } }
        public int GridY { get { return _gridY; } set { _gridY = Math.Max(1, value); MarkDirty(); } }

        private void HookStateEvents()
        {
            if (_eventsHooked)
                return;

            MaterialStateService.StateChanged += OnMaterialStateChanged;
            LotStorage.ActiveLotChanged += OnActiveLotChanged;
            _eventsHooked = true;
        }

        private void UnhookStateEvents()
        {
            if (!_eventsHooked)
                return;

            MaterialStateService.StateChanged -= OnMaterialStateChanged;
            LotStorage.ActiveLotChanged -= OnActiveLotChanged;
            _eventsHooked = false;
        }

        // ─── 이벤트: 비-UI 스레드에서 호출될 수 있으므로 dirty flag 만 세운다. UI 접근 금지. ───
        private void OnMaterialStateChanged(MaterialSnapshot snapshot)
        {
            MarkDirty();
        }

        private void OnActiveLotChanged(Lot lot)
        {
            MarkDirty();
        }

        private void MarkDirty()
        {
            Interlocked.Exchange(ref _dirty, 1);
        }

        private void OnRefreshTick(object sender, EventArgs e)
        {
            try
            {
                if (!PageBase.ShouldRefreshVisible(this))
                    return;

                // 변경 이벤트(Material/Lot)가 없으면 아무 일도 하지 않는다. (idle tick 비용 0)
                // → 큰 다이맵(수천 개)을 매 100ms 마다 스캔/재계산하던 UI 스레드 부하 제거.
                bool dirtyEvent = Interlocked.Exchange(ref _dirty, 0) == 1;
                if (!dirtyEvent)
                    return;

                EnsureDisplayMap(true);

                long sig = ComputeSignature(_displayMap);
                if (sig == _signature)
                    return;

                _signature = sig;
                _stats = CalculateMapStats(_displayMap);
                Lot lot = LotStorage.ActiveLot;
                _lotText = lot != null ? lot.LotID : "(no active lot)";
                Invalidate();
            }
            catch
            {
                // 표시 갱신 중 일시적 실패는 무시한다.
            }
        }

        /// <summary>
        /// 표시용 맵 참조를 필요할 때만 갱신한다.<br/>
        /// ActiveInputDieMap 이 있으면 그것을 쓰고(새 참조일 때만 1회 Normalize),
        /// 없으면 이벤트/최초 1회에 한해 InputStage Wafer 에서 복원한다(매 Tick 재구성 금지).
        /// </summary>
        private void EnsureDisplayMap(bool dirtyEvent)
        {
            DieMap active = LotStorage.ActiveInputDieMap;
            if (active != null)
            {
                DieMapGenerator.Normalize(active);
                _displayMap = BuildDisplayMapFromMaterialState(active);
                return;
            }

            if (dirtyEvent || _displayMap == null)
            {
                try
                {
                    DieMap built = MaterialStateService.BuildInputDieMapFromStageWafer();
                    if (built != null)
                    {
                        DieMapGenerator.Normalize(built);
                        LotStorage.ActiveInputDieMap = built;
                        _displayMap = built;
                    }
                    else
                    {
                        _displayMap = null;
                    }
                }
                catch
                {
                    _displayMap = null;
                }
            }
        }

        private static DieMap BuildDisplayMapFromMaterialState(DieMap source)
        {
            DieMap display = CloneMap(source);
            if (display == null)
                return null;

            try
            {
                MaterialSnapshot state = MaterialStorage.State;
                if (state == null || state.Dies == null || state.Dies.Count == 0 || display.Entries == null)
                    return display;

                var dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
                var dieByGrid = new Dictionary<string, DieMaterial>(StringComparer.Ordinal);
                foreach (DieMaterial die in state.Dies)
                {
                    if (die == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(die.DieId) && !dieById.ContainsKey(die.DieId))
                        dieById.Add(die.DieId, die);

                    if (die.Wafer_IndexX >= 0 && die.Wafer_IndexY >= 0)
                    {
                        string key = BuildGridKey(die.Wafer_IndexX, die.Wafer_IndexY);
                        if (!dieByGrid.ContainsKey(key))
                            dieByGrid.Add(key, die);
                    }
                }

                foreach (DieMapEntry entry in display.Entries)
                {
                    if (entry == null)
                        continue;

                    DieMaterial die = null;
                    if (!string.IsNullOrWhiteSpace(entry.DieUid))
                        dieById.TryGetValue(entry.DieUid, out die);
                    if (die == null)
                        dieByGrid.TryGetValue(BuildGridKey(entry.DieMapX, entry.DieMapY), out die);
                    if (die == null)
                        continue;

                    entry.DieUid = die.DieId ?? entry.DieUid;
                    entry.IsTarget = die.IsInputTarget;
                    entry.Result = die.Result;
                    if (die.Input_BinCode > 0)
                        entry.BinCode = die.Input_BinCode;
                    else if (die.Output_BinCode > 0)
                        entry.BinCode = die.Output_BinCode;
                }
            }
            catch
            {
            }

            return display;
        }

        private static DieMap CloneMap(DieMap source)
        {
            if (source == null)
                return null;

            var clone = new DieMap
            {
                FrameObjId = source.FrameObjId,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                PitchX = source.PitchX,
                PitchY = source.PitchY,
                OriginX = source.OriginX,
                OriginY = source.OriginY,
                CreatedAt = source.CreatedAt
            };

            if (source.Entries != null)
            {
                foreach (DieMapEntry entry in source.Entries)
                {
                    if (entry == null)
                        continue;

                    clone.Entries.Add(new DieMapEntry
                    {
                        Index = entry.Index,
                        SequenceNo = entry.SequenceNo,
                        DieMapX = entry.DieMapX,
                        DieMapY = entry.DieMapY,
                        IsTarget = entry.IsTarget,
                        Result = entry.Result,
                        BinCode = entry.BinCode,
                        PosX = entry.PosX,
                        PosY = entry.PosY,
                        DieUid = entry.DieUid
                    });
                }
            }

            return clone;
        }

        private static string BuildGridKey(int x, int y)
        {
            return x.ToString() + ":" + y.ToString();
        }

        private static long ComputeSignature(DieMap map)
        {
            unchecked
            {
                long h = 17;
                Lot lot = LotStorage.ActiveLot;
                h = h * 31 + (lot != null ? lot.ProcessedDies : 0);
                h = h * 31 + (lot != null ? lot.GoodCount : 0);
                h = h * 31 + (lot != null ? lot.TotalDies : 0);

                if (map != null && map.Entries != null)
                {
                    h = h * 31 + map.DieMapX;
                    h = h * 31 + map.DieMapY;
                    foreach (var entry in map.Entries)
                    {
                        if (entry == null)
                            continue;
                        h = h * 31 + (entry.IsTarget ? 1 : 0);
                        h = h * 31 + (int)entry.Result;
                        h = h * 31 + entry.BinCode;
                    }
                }
                else
                {
                    h = h * 31 - 1;
                }

                return h;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // OnPaint 는 캐시된 표시 값만 그린다. (복원/Normalize/저장/전역 쓰기 금지)
            var g = e.Graphics;
            g.Clear(BackColor);

            DieMap dmap = _displayMap;
            MapStats stats = _stats;

            using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
            using (var f = new Font("Consolas", 9F, FontStyle.Bold))
            {
                string head = dmap != null
                    ? string.Format("INPUT WAFER MAP   LOT {0}  target={1}  done={2}  good={3}  ng={4}",
                                    _lotText, stats.Target, stats.Done, stats.Good, stats.Ng)
                    : string.Format("INPUT WAFER MAP   LOT {0}  (no input die map)", _lotText);
                g.DrawString(head, f, bf, 8, 4);
            }

            int gx = Math.Max(1, (dmap != null) ? dmap.DieMapX : _gridX);
            int gy = Math.Max(1, (dmap != null) ? dmap.DieMapY : _gridY);
            int margin = 8;
            int top = 22;
            int availW = Math.Max(50, Width - margin * 2);
            int availH = Math.Max(50, Height - top - margin);
            int cell = Math.Max(1, Math.Min(availW / gx, availH / gy));
            int totalW = cell * gx;
            int totalH = cell * gy;
            int x0 = (Width - totalW) / 2;
            int y0 = top + (availH - totalH) / 2;

            if (dmap != null)
            {
                if (dmap.Entries != null)
                {
                    foreach (var entry in dmap.Entries)
                    {
                        if (entry == null || !entry.IsTarget)
                            continue;

                        int x = x0 + entry.DieMapX * cell;
                        int y = y0 + entry.DieMapY * cell;
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

                using (var pen = new Pen(Color.FromArgb(0x44, 0x88, 0xCC), 1.5f))
                    g.DrawEllipse(pen, x0, y0, totalW, totalH);
            }
            else
            {
                // 입력 다이맵이 아직 없을 때만 Lot 카운트 기반 5x5 격자(레거시 fallback) 표시.
                int filled = 0;
                int goodFilled = 0;
                Lot lot = LotStorage.ActiveLot;
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

            int percentDone = dmap != null ? stats.Target : (LotStorage.ActiveLot != null ? LotStorage.ActiveLot.TotalDies : 0);
            int percentProcessed = dmap != null ? stats.Done : (LotStorage.ActiveLot != null ? LotStorage.ActiveLot.ProcessedDies : 0);
            if (percentDone > 0)
            {
                using (var bf = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
                using (var f = new Font("Consolas", 9F))
                {
                    string pct = string.Format("{0:F1} %", percentProcessed * 100.0 / percentDone);
                    var sz = g.MeasureString(pct, f);
                    g.DrawString(pct, f, bf, Width - sz.Width - 8, Height - sz.Height - 4);
                }
            }
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
            try
            {
                UnhookStateEvents();
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }
            base.OnHandleDestroyed(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRefreshTimer();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRefreshTimer();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateRefreshTimer();
        }

        private void UpdateRefreshTimer()
        {
            try
            {
                if (_refresh == null || IsDisposed)
                    return;

                if (PageBase.ShouldRefreshVisible(this))
                {
                    MarkDirty();
                    if (!_refresh.Enabled)
                        _refresh.Start();
                }
                else if (_refresh.Enabled)
                {
                    _refresh.Stop();
                }
            }
            catch
            {
            }
        }
    }
}
