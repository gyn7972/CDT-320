using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 공통 검사 결과 뷰어 — Bottom/Side/Bin 세 모드를 한 컨트롤로 표시.
    /// Picker 1~4 = 검은 캔버스 이미지 뷰어(VisionImageView, 줌/팝업),
    /// 우측 = SPC 추세 차트 2개(줌/팝업) + Map + 결과 그리드.
    /// <see cref="SetMode"/> 로 라벨/차트/그리드/이미지를 모드별로 스왑한다.
    /// Side 는 기본 빈칸(NO IMAGE)으로 두고 시퀀서/실데이터(InspectionResultStore)가 채운다.
    /// Bottom/Bin 은 <see cref="SampleData"/> 샘플로 폴백(하드웨어 없이 시연), 실데이터 있으면 덮어쓴다.
    /// 컨트롤 선언/배치는 .Designer.cs, 동작 로직은 본 파일(AGENTS 디자이너 규칙).
    /// </summary>
    public partial class InspectionViewerControl : UserControl
    {
        public InspectionMode Mode { get; private set; } = InspectionMode.Bottom;

        public InspectionViewerControl()
        {
            InitializeComponent();
            SetMode(InspectionMode.Bottom);
        }

        /// <summary>표시 모드 전환 — 라벨/그래프/Map/그리드 + 샘플 이미지·데이터 구성.</summary>
        public void SetMode(InspectionMode mode)
        {
            Mode = mode;
            try
            {
                switch (mode)
                {
                    case InspectionMode.Bottom:
                        _lblToggle.Text = "너비와 높이";
                        _mapTitle.Text = "Map — Width · Height · 1ch · 2ch ChippingSize";
                        _mapHost.Visible = true;
                        BuildGridColumns("Index X", "Index Y", "Picker", "Width", "Height", "Angle", "Offset X", "Offset Y");
                        break;
                    case InspectionMode.Side:
                        _lblToggle.Text = "Channel 1 / Channel 2";
                        _mapTitle.Text = "Map — 측면 칩핑(리밋 근접 색칠)";
                        _mapHost.Visible = true;
                        BuildGridColumns("Index X", "Index Y", "Picker", "Front max", "Back max");
                        break;
                    case InspectionMode.Bin:
                        _lblToggle.Text = "위·좌 / 아래·우";
                        _mapTitle.Text = "Map — Bin gap(리밋 근접 색칠)";
                        _mapHost.Visible = true;
                        BuildGridColumns("Index X", "Index Y", "Picker", "Right max", "Right min", "Bottom gap", "Offset X", "Offset Y", "Angle");
                        break;
                }

                if (mode == InspectionMode.Side)
                    PopulateBlankSide();      // Side 는 기본 빈칸(시퀀서가 채움)
                else
                    PopulateBlank(mode);      // Bottom/Bin 기본 빈칸 — 값 없으면 없는대로, 실데이터가 채움
                RefreshFromStore();           // 실데이터 있으면 채움/덮어쓰기
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[InspectionViewerControl] SetMode 실패: " + ex.Message);
            }
        }

        /// <summary>샘플 데이터로 Picker 이미지/차트/그리드를 채운다(시연용).</summary>
        private void PopulateSample(InspectionMode mode)
        {
            // Picker 1~4 — Side 는 4채널(Front ch1/2, Back ch1/2), 그 외는 단일 이미지
            var pks = new[] { _pk1, _pk2, _pk3, _pk4 };
            for (int i = 0; i < pks.Length; i++)
            {
                if (mode == InspectionMode.Side)
                {
                    pks[i].SetChannels(SampleData.MakeSideChannels(i + 1));
                }
                else
                {
                    SampleChip sc = SampleData.MakeChip(mode, i + 1);
                    pks[i].SetSingle(sc.Image, sc.Box, sc.Pass, sc.Verdict, sc.Lines, sc.Marks);
                }
                pks[i].SetCrossline(_chkCross.Checked);
            }

            // 추세 차트 2개
            double up, lo; string title; Color col;
            double[] s1 = SampleData.Series(mode, 0, out up, out lo, out title, out col); ApplyChartLimits(0, ref up, ref lo);
            _chart1.SetData(s1, up, lo, title, col);
            double[] s2 = SampleData.Series(mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
            _chart2.SetData(s2, up, lo, title, col);

            // 위치별 4-맵(Bottom/Side/Bin) — 실데이터 있으면 RefreshFromStore 가 채움/덮어씀
            BuildPositionMaps(mode);

            // 결과 그리드
            _grid.Rows.Clear();
            foreach (string[] row in SampleData.Rows(mode))
                _grid.Rows.Add((object[])row);
        }

        /// <summary>Bottom/Bin 기본 빈칸 — 픽커 NO IMAGE, 차트는 상/하한선만(데이터 없음), 그리드·맵 비움.
        /// 값이 없으면 없는대로 표시하고, 시퀀서/실데이터(InspectionResultStore)가 들어오면 채워진다.</summary>
        private void PopulateBlank(InspectionMode mode)
        {
            foreach (var pk in new[] { _pk1, _pk2, _pk3, _pk4 })
            {
                pk.ClearSingle();
                pk.SetCrossline(_chkCross.Checked);
            }
            System.Array.Clear(_boundCh, 0, _boundCh.Length);
            double up, lo; string title; Color col;
            SampleData.Series(mode, 0, out up, out lo, out title, out col); ApplyChartLimits(0, ref up, ref lo);
            _chart1.SetData(new double[0], up, lo, UnitTitle(title), col);
            SampleData.Series(mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
            _chart2.SetData(new double[0], up, lo, UnitTitle(title), col);
            _grid.Rows.Clear();
            _waferMap?.SetMaps(null, null, null, null);
        }

        /// <summary>Side 기본 빈칸 — 4채널 NO IMAGE, 차트는 상/하한선만, 그리드 비움. 시퀀서 동작 시 채워짐.</summary>
        private void PopulateBlankSide()
        {
            foreach (var pk in new[] { _pk1, _pk2, _pk3, _pk4 })
            {
                pk.ClearChannels();
                pk.SetCrossline(_chkCross.Checked);
            }
            System.Array.Clear(_boundCh, 0, _boundCh.Length);   // 바인딩 추적 초기화(다시 채워지게)
            double up, lo; string title; Color col;
            SampleData.Series(InspectionMode.Side, 0, out up, out lo, out title, out col);
            _chart1.SetData(new double[0], up, lo, title, col);
            SampleData.Series(InspectionMode.Side, 1, out up, out lo, out title, out col);
            _chart2.SetData(new double[0], up, lo, title, col);
            _grid.Rows.Clear();
            _waferMap?.SetMaps(null, null, null, null);
        }

        /// <summary>크로스라인 체크 변경 → 모든 픽커(단일/4채널)에 적용.</summary>
        private void OnCrossChanged(object sender, EventArgs e)
        {
            bool on = _chkCross.Checked;
            foreach (var pk in new[] { _pk1, _pk2, _pk3, _pk4 })
                if (pk != null) pk.SetCrossline(on);
        }

        // ── 결과 스토어 구독 ──
        private string ModeKey()
        {
            switch (Mode)
            {
                case InspectionMode.Side: return InspectionResultStore.Side;
                case InspectionMode.Bin:  return InspectionResultStore.Bin;
                default:                  return InspectionResultStore.Bottom;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InspectionResultStore.Changed += OnStoreChanged;
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            InspectionResultStore.Changed -= OnStoreChanged;
            base.OnHandleDestroyed(e);
        }

        private volatile bool _refreshPending;   // 갱신 합치기(8샷/사이클의 Changed 폭주 → 1회)
        private readonly InspectionResultStore.Item[,] _boundCh = new InspectionResultStore.Item[5, 4];  // 픽커×채널 직전 바인딩(중복 생략)

        private void OnStoreChanged(string mode)
        {
            if (IsDisposed || !string.Equals(mode, ModeKey(), StringComparison.OrdinalIgnoreCase)) return;
            if (_refreshPending) return;          // 이미 갱신 예약됨 → 폭주 흡수(완성 상태로 1회만)
            _refreshPending = true;
            try
            {
                if (InvokeRequired) BeginInvoke((Action)(() => { _refreshPending = false; RefreshFromStore(); }));
                else { _refreshPending = false; RefreshFromStore(); }
            }
            catch { _refreshPending = false; }
        }

        /// <summary>스토어 실데이터로 Picker(단일)·차트·그리드 갱신. 데이터 없으면 샘플 유지.</summary>
        private void RefreshFromStore()
        {
            string mode = ModeKey();
            var hist = InspectionResultStore.History(mode);
            if (hist.Count == 0) return;

            var pks = new[] { _pk1, _pk2, _pk3, _pk4 };
            if (Mode == InspectionMode.Side)
            {
                // Side 4채널(Front ch1/2, Back ch1/2) 픽커별 바인딩 — 직전과 같은 결과면 재바인딩 생략(깜빡임 방지)
                for (int p = 1; p <= 4; p++)
                    for (int c = 0; c < 4; c++)
                    {
                        var it = InspectionResultStore.LatestChannel(mode, p, c);
                        if (it != null && it.Image != null && !ReferenceEquals(_boundCh[p, c], it))
                        {
                            // 바인딩이 실제로 성공(클론 OK)했을 때만 기록 — 실패 시 다음 갱신에 재시도(빈 채로 굳지 않게)
                            if (pks[p - 1].SetChannel(c, it.Image, it.Box, it.Pass, it.Pass ? "Good" : "NG", it.Lines, MarksOf(it)))
                                _boundCh[p, c] = it;
                        }
                    }
            }
            else
            {
                // Picker 단일 이미지(Bottom/Bin)
                for (int p = 1; p <= 4; p++)
                {
                    var it = InspectionResultStore.Latest(mode, p);
                    if (it != null && it.Image != null)
                        pks[p - 1].SetSingle(it.Image, it.Box, it.Pass, it.Pass ? "Good" : "NG", it.Lines, MarksOf(it));
                }
            }

            // 차트(상/하한·제목·색은 SampleData 에서, 값은 스토어에서)
            double up, lo; string title; Color col;
            if (Mode == InspectionMode.Side)
            {
                // 실제 운영뷰와 동일 — 다이 단위 집계. Front/Back max 추세 + 다이별 한 행.
                var dies = InspectionResultStore.Dies(mode);
                double[] vf = dies.Select(d => d.FrontMax).ToArray();
                SampleData.Series(Mode, 0, out up, out lo, out title, out col); ApplyChartLimits(0, ref up, ref lo);
                if (vf.Length > 0) _chart1.SetData(DispSeries(vf), up, lo, UnitTitle(title), col);
                double[] vb = dies.Select(d => d.BackMax).ToArray();
                SampleData.Series(Mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
                if (vb.Length > 0) _chart2.SetData(DispSeries(vb), up, lo, UnitTitle(title), col);

                _grid.Rows.Clear();
                foreach (var d in dies)
                {
                    var row = new object[_grid.Columns.Count];
                    for (int c = 0; c < _grid.Columns.Count; c++)
                    {
                        string h = _grid.Columns[c].HeaderText;
                        if (h == "Index X") row[c] = d.IndexX;
                        else if (h == "Index Y") row[c] = d.IndexY;
                        else if (h == "Picker") row[c] = d.Picker;
                        else if (h == "Front max") row[c] = d.HasFront ? d.FrontMax.ToString("F4") : "";
                        else if (h == "Back max")  row[c] = d.HasBack  ? d.BackMax.ToString("F4") : "";
                        else row[c] = "";
                    }
                    _grid.Rows.Add(row);
                }
                return;
            }

            // Bottom/Bin — 기존 history 기반
            {
                string k1, k2; ChartKeys(out k1, out k2);
                double[] v1 = InspectionResultStore.Series(mode, k1);
                SampleData.Series(Mode, 0, out up, out lo, out title, out col); ApplyChartLimits(0, ref up, ref lo);
                if (v1.Length > 0) _chart1.SetData(DispSeries(v1), up, lo, UnitTitle(title), col);
                double[] v2 = InspectionResultStore.Series(mode, k2);
                SampleData.Series(Mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
                if (v2.Length > 0) _chart2.SetData(DispSeries(v2), up, lo, UnitTitle(title), col);
            }

            _grid.Rows.Clear();
            foreach (var it in hist)
            {
                var row = new object[_grid.Columns.Count];
                for (int c = 0; c < _grid.Columns.Count; c++)
                {
                    string h = _grid.Columns[c].HeaderText;
                    if (h == "Index X") row[c] = it.IndexX;
                    else if (h == "Index Y") row[c] = it.IndexY;
                    else if (h == "Picker") row[c] = it.Picker;
                    else row[c] = it.Values.TryGetValue(h, out double dv) ? dv.ToString("F4") : "";
                }
                _grid.Rows.Add(row);
            }

            BuildPositionMaps(Mode);   // 위치별 4-맵 갱신(Bottom/Side/Bin)
        }

        // ── 위치별 4-맵 — Bottom/Side/Bin 공통 ──
        // 条件1: 레시피 웨이퍼 사양(Grid X/Y) 좌표에 각 결과(Index X/Index Y)를 배치해 그린다.
        // 条件2: 셀 색은 리밋 근접도로 그라데이션. 폭/높이처럼 양측 밴드면 밴드 중심=흰색·상/하한=빨강,
        //        칩핑/갭처럼 0이 최적이면 0=흰색·상한=빨강. 리밋은 차트(ChartLimitStore)와 동일 소스 사용.
        private void BuildPositionMaps(InspectionMode mode)
        {
            if (_waferMap == null) return;
            // Bottom 은 위치별 누적 스토어(BottomCells, 이력 한도와 무관)로 그려 한 랏 전체 위치 유지(스크롤/사라짐 없음).
            if (mode == InspectionMode.Bottom) { BuildBottomPositionMaps(); return; }
            var hist = InspectionResultStore.History(StoreKeyOf(mode));
            if (hist.Count == 0) { _waferMap.SetMaps(null, null, null, null); return; }

            // 웨이퍼 격자 — 레시피 사양(ActiveRecipeContext) 우선, 결과 인덱스가 벗어나면 확장.
            var recipe = QMC.Vision.Core.ActiveRecipeContext.Current;
            int gridX = recipe != null ? recipe.WaferGridX : 0;
            int gridY = recipe != null ? recipe.WaferGridY : 0;
            foreach (var it in hist)
            {
                if (it.IndexX + 1 > gridX) gridX = it.IndexX + 1;
                if (it.IndexY + 1 > gridY) gridY = it.IndexY + 1;
            }
            if (gridX <= 0 || gridY <= 0) { _waferMap.SetMaps(null, null, null, null); return; }
            if (gridX > 600) gridX = 600;
            if (gridY > 600) gridY = 600;

            // 차트 2지표의 키 + 상/하한(대시선과 동일 소스).
            ChartKeys(out string k1, out string k2);
            double up0, lo0, up1, lo1; string t; Color col;
            SampleData.Series(mode, 0, out up0, out lo0, out t, out col); ApplyChartLimits(0, ref up0, ref lo0);
            SampleData.Series(mode, 1, out up1, out lo1, out t, out col); ApplyChartLimits(1, ref up1, ref lo1);

            double[,] m0 = NewNaN(gridY, gridX), m1 = NewNaN(gridY, gridX);
            foreach (var it in hist)
            {
                int cx = it.IndexX, cy = it.IndexY;
                if (cx < 0 || cy < 0 || cx >= gridX || cy >= gridY) continue;
                double v0 = Val(it, k1); if (!double.IsNaN(v0)) m0[cy, cx] = v0;
                double v1 = Val(it, k2); if (!double.IsNaN(v1)) m1[cy, cx] = v1;
            }

            if (mode == InspectionMode.Bottom)
            {
                // Bottom 은 1ch/2ch 칩핑 맵 2개 추가(칩핑 한계 대비 비율).
                double chipLimit = recipe != null ? recipe.MaxChippingDepthMm : 0;
                double[,] c1 = NewNaN(gridY, gridX), c2 = NewNaN(gridY, gridX);
                foreach (var it in hist)
                {
                    int cx = it.IndexX, cy = it.IndexY;
                    if (cx < 0 || cy < 0 || cx >= gridX || cy >= gridY) continue;
                    c1[cy, cx] = MaxNaN(Val(it, "Chipping Top"), Val(it, "Chipping Bottom"));
                    c2[cy, cx] = MaxNaN(Val(it, "Chipping Left"), Val(it, "Chipping Right"));
                }
                _waferMap.SetMap(0, "Width", NormAuto(m0, lo0, up0));
                _waferMap.SetMap(1, "Height", NormAuto(m1, lo1, up1));
                _waferMap.SetMap(2, "1 Channel ChippingSize", NormByMax(c1, chipLimit));
                _waferMap.SetMap(3, "2 Channel ChippingSize", NormByMax(c2, chipLimit));
            }
            else
            {
                // Side/Bin 은 차트 2지표를 그대로 맵 2개로(나머지 2칸 비움).
                _waferMap.SetMap(0, k1, NormAuto(m0, lo0, up0));
                _waferMap.SetMap(1, k2, NormAuto(m1, lo1, up1));
                _waferMap.SetMap(2, "", null);
                _waferMap.SetMap(3, "", null);
            }
        }

        /// <summary>Bottom 위치 고정 맵 — 위치별 누적 스토어(BottomCells)에서 Index X/Y 격자로 그린다.
        /// 롤링 이력과 무관해 다이가 많아도 오래된 위치가 사라지지 않는다(스크롤/리셋 없음).</summary>
        private void BuildBottomPositionMaps()
        {
            var cells = InspectionResultStore.BottomCells(InspectionResultStore.Bottom);
            if (cells.Count == 0) { _waferMap.SetMaps(null, null, null, null); return; }

            var recipe = QMC.Vision.Core.ActiveRecipeContext.Current;
            int gridX = recipe != null ? recipe.WaferGridX : 0;
            int gridY = recipe != null ? recipe.WaferGridY : 0;
            foreach (var c in cells)
            {
                if (c.IndexX + 1 > gridX) gridX = c.IndexX + 1;
                if (c.IndexY + 1 > gridY) gridY = c.IndexY + 1;
            }
            if (gridX <= 0 || gridY <= 0) { _waferMap.SetMaps(null, null, null, null); return; }
            if (gridX > 600) gridX = 600;
            if (gridY > 600) gridY = 600;

            double up0, lo0, up1, lo1; string t; Color col;
            SampleData.Series(InspectionMode.Bottom, 0, out up0, out lo0, out t, out col); ApplyChartLimits(0, ref up0, ref lo0);
            SampleData.Series(InspectionMode.Bottom, 1, out up1, out lo1, out t, out col); ApplyChartLimits(1, ref up1, ref lo1);
            double chipLimit = recipe != null ? recipe.MaxChippingDepthMm : 0;

            double[,] m0 = NewNaN(gridY, gridX), m1 = NewNaN(gridY, gridX), c1 = NewNaN(gridY, gridX), c2 = NewNaN(gridY, gridX);
            foreach (var c in cells)
            {
                int cx = c.IndexX, cy = c.IndexY;
                if (cx < 0 || cy < 0 || cx >= gridX || cy >= gridY) continue;
                m0[cy, cx] = c.Width; m1[cy, cx] = c.Height;
                c1[cy, cx] = c.Chip1; c2[cy, cx] = c.Chip2;
            }
            _waferMap.SetMap(0, "Width", NormAuto(m0, lo0, up0));
            _waferMap.SetMap(1, "Height", NormAuto(m1, lo1, up1));
            _waferMap.SetMap(2, "1 Channel ChippingSize", NormByMax(c1, chipLimit));
            _waferMap.SetMap(3, "2 Channel ChippingSize", NormByMax(c2, chipLimit));
        }

        private static string StoreKeyOf(InspectionMode mode)
        {
            switch (mode)
            {
                case InspectionMode.Side: return InspectionResultStore.Side;
                case InspectionMode.Bin:  return InspectionResultStore.Bin;
                default:                  return InspectionResultStore.Bottom;
            }
        }

        /// <summary>현재 모드(Bottom/Side/Bin)의 검사 결과를 CSV 로 수동 저장(작업화면용 빠른 내보내기).
        /// 정식 다이별 데이터로그는 DataLogSaver(vision_YYYYMMDD.csv)가 자동 기록한다.</summary>
        private void OnExportCsvClick(object sender, EventArgs e)
        {
            try
            {
                string mode = StoreKeyOf(Mode);
                using (var dlg = new SaveFileDialog
                {
                    Filter = "검사결과 CSV (*.csv)|*.csv",
                    FileName = mode + "_results_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    bool ok = InspectionResultCsv.Save(mode, dlg.FileName);
                    MessageBox.Show(ok ? "결과 CSV를 저장했습니다.\n" + dlg.FileName
                                       : "저장할 결과가 없습니다(검사 데이터 없음).",
                        "결과 CSV", MessageBoxButtons.OK,
                        ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("결과 CSV 저장에 실패했습니다.\n" + ex.Message, "결과 CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>상/하한 형태로 정규화 방식 자동 선택: 하한&gt;0 양측 밴드, 아니면 0~상한, 둘 다 없으면 상대 편차.</summary>
        private static double[,] NormAuto(double[,] raw, double lo, double up)
        {
            if (lo > 1e-6 && up > lo) return NormByLimit(raw, lo, up);   // 양측 밴드(Width/Height 등)
            if (up > 1e-6) return NormByMax(raw, up);                    // 0~상한(칩핑/갭 등)
            return NormDev(raw);                                          // 리밋 미설정 → 상대 편차
        }

        private static double Val(InspectionResultStore.Item it, string k)
            => it.Values.TryGetValue(k, out double v) ? v : double.NaN;

        /// <summary>리밋 밴드 중심(=흰색)에서 상/하한(=빨강)까지의 근접도로 대칭 정규화. 리밋 미설정 시 상대 편차 폴백.</summary>
        private static double[,] NormByLimit(double[,] raw, double lo, double up)
        {
            if (!(up > lo)) return NormDev(raw);   // 리밋 미설정/역전 → 상대 편차로 표시
            double center = (lo + up) / 2.0;
            double half = (up - lo) / 2.0;
            int rows = raw.GetLength(0), cols = raw.GetLength(1);
            var o = NewNaN(rows, cols);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
            {
                double v = raw[r, c];
                if (double.IsNaN(v)) continue;
                o[r, c] = half > 1e-9 ? Clamp01(System.Math.Abs(v - center) / half) : 0;
            }
            return o;
        }

        /// <summary>절대 크기를 리밋 대비 비율(0~1)로 정규화. 리밋 미설정 시 상대 최대 폴백(칩핑/이물).</summary>
        private static double[,] NormByMax(double[,] raw, double limit)
        {
            if (limit <= 1e-9) return NormMag(raw);   // 리밋 미설정 → 상대 최대로 표시
            int rows = raw.GetLength(0), cols = raw.GetLength(1);
            var o = NewNaN(rows, cols);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
            {
                double v = raw[r, c];
                if (double.IsNaN(v)) continue;
                o[r, c] = Clamp01(v / limit);
            }
            return o;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static double[,] NewNaN(int rows, int cols)
        {
            var a = new double[rows, cols];
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) a[r, c] = double.NaN;
            return a;
        }
        private static double MaxNaN(double a, double b)
        {
            bool na = double.IsNaN(a), nb = double.IsNaN(b);
            if (na && nb) return double.NaN;
            if (na) return b; if (nb) return a;
            return a > b ? a : b;
        }
        /// <summary>평균 대비 편차 크기를 0~1 로 정규화(중앙=흰색, 최대편차=적색).</summary>
        private static double[,] NormDev(double[,] g)
        {
            int rows = g.GetLength(0), cols = g.GetLength(1);
            double sum = 0; int n = 0;
            foreach (var v in g) if (!double.IsNaN(v)) { sum += v; n++; }
            if (n == 0) return g;
            double mean = sum / n, maxDev = 0;
            foreach (var v in g) if (!double.IsNaN(v)) { double d = System.Math.Abs(v - mean); if (d > maxDev) maxDev = d; }
            var o = NewNaN(rows, cols);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
                if (!double.IsNaN(g[r, c])) o[r, c] = maxDev > 1e-9 ? System.Math.Abs(g[r, c] - mean) / maxDev : 0;
            return o;
        }
        /// <summary>절대 크기를 0~1 로 정규화(0=흰색, 최대=적색). 칩핑/이물용.</summary>
        private static double[,] NormMag(double[,] g)
        {
            int rows = g.GetLength(0), cols = g.GetLength(1);
            double max = 0;
            foreach (var v in g) if (!double.IsNaN(v) && v > max) max = v;
            var o = NewNaN(rows, cols);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
                if (!double.IsNaN(g[r, c])) o[r, c] = max > 1e-9 ? g[r, c] / max : 0;
            return o;
        }

        private static PointF[] MarksOf(InspectionResultStore.Item it)
            => it.Defects == null ? null : it.Defects.Select(d => new PointF((float)d.X, (float)d.Y)).ToArray();

        /// <summary>차트 상/하한을 레시피 기반 ChartLimitStore 값으로 덮어쓴다(없으면 기본값 유지). px 모드면 mm→px 환산.</summary>
        private void ApplyChartLimits(int which, ref double up, ref double lo)
        {
            if (QMC.Vision.Core.ChartLimitStore.TryGet(ModeKey(), which, out double u, out double l) && !(u == 0 && l == 0))
            { up = u; lo = l; }
            double s = UnitScale();
            if (s > 0 && s != 1.0) { up /= s; lo /= s; }   // mm 저장값 → px 표시
        }

        /// <summary>현재 표시 단위 환산 계수(px 모드일 때 모드 카메라 스케일, mm 모드면 1).</summary>
        private double UnitScale()
        {
            if (QMC.Vision.Core.UnitContext.DisplayMm) return 1.0;
            QMC.Vision.Core.UnitContext.GetModeScale(ModeKey(), out double sx, out double sy);
            return sx > 0 ? sx : 1.0;
        }

        /// <summary>mm 시계열 → 표시 단위(px 모드면 ÷scale).</summary>
        private double[] DispSeries(double[] mm)
        {
            double s = UnitScale();
            if (mm == null || s <= 0 || s == 1.0) return mm;
            var o = new double[mm.Length];
            for (int i = 0; i < mm.Length; i++) o[i] = mm[i] / s;
            return o;
        }

        private string UnitTitle(string t)
        {
            if (string.IsNullOrEmpty(t)) return t;
            string u = "[" + QMC.Vision.Core.UnitContext.UnitLabel + "]";
            int a = t.IndexOf('['), b = t.IndexOf(']');
            return (a >= 0 && b > a) ? t.Substring(0, a) + u + t.Substring(b + 1) : t + " " + u;
        }

        private void ChartKeys(out string k1, out string k2)
        {
            switch (Mode)
            {
                case InspectionMode.Side: k1 = "Max Chipping Depth"; k2 = "Chipping Bottom"; break;
                case InspectionMode.Bin:  k1 = "Right max"; k2 = "Bottom gap"; break;
                default:                  k1 = "Width"; k2 = "Height"; break;
            }
        }

        // ── 픽커별 수동 테스트(제공 이미지 기준) ──
        private IInspector CreateInspector(string mode)
        {
            if (mode == InspectionResultStore.Side) return new SideAppearanceInspector("manual");
            if (mode == InspectionResultStore.Bin)  return new PlacementGapInspector("manual");
            return new BottomInspector("manual");
        }

        private void OnTestClick(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog
                {
                    Multiselect = true,
                    Title = "픽커별 검사 테스트 이미지 선택 (최대 4 — 픽커 1~4 순서)",
                    Filter = "Image|*.png;*.bmp;*.jpg;*.jpeg;*.tif;*.tiff|All|*.*"
                })
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                    string mode = ModeKey();
                    string[] files = dlg.FileNames;
                    for (int i = 0; i < files.Length && i < 4; i++)
                    {
                        using (var bmp = new Bitmap(files[i]))
                        {
                            IInspector ins = CreateInspector(mode);
                            ins.InspectionRoi = new Roi
                            {
                                Name = "manual",
                                CenterX = bmp.Width / 2.0,
                                CenterY = bmp.Height / 2.0,
                                Width = bmp.Width,
                                Height = bmp.Height
                            };
                            InspectionResult r = ins.Inspect(bmp);
                            InspectionResultStore.Record(InspectionResultStore.FromResult(mode, i + 1, 0, i + 1, r, bmp));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("픽커 테스트 실패: " + ex.Message);
            }
        }

        private void BuildGridColumns(params string[] headers)
        {
            _grid.Columns.Clear();
            foreach (string h in headers)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    HeaderText = h,
                    Name = "col_" + h.Replace(" ", "_"),
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                _grid.Columns.Add(col);
            }
        }
    }
}
