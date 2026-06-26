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
                        _mapHost.Visible = false;
                        BuildGridColumns("Index X", "Index Y", "Picker", "Front max", "Back max");
                        break;
                    case InspectionMode.Bin:
                        _lblToggle.Text = "위·좌 / 아래·우";
                        _mapHost.Visible = false;
                        BuildGridColumns("Index X", "Index Y", "Picker", "Right max", "Right min", "Bottom gap", "Offset X", "Offset Y", "Angle");
                        break;
                }

                if (mode == InspectionMode.Side)
                    PopulateBlankSide();   // Side 는 기본 빈칸(시퀀서가 채움)
                else
                    PopulateSample(mode);  // Bottom/Bin 샘플(폴백)
                RefreshFromStore();        // 실데이터 있으면 채움/덮어쓰기
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

            // 위치별 4-맵(Bottom 전용) — 실데이터 있으면 RefreshFromStore 가 채움/덮어씀
            if (mode == InspectionMode.Bottom)
                BuildBottomMaps();

            // 결과 그리드
            _grid.Rows.Clear();
            foreach (string[] row in SampleData.Rows(mode))
                _grid.Rows.Add((object[])row);
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
                if (vf.Length > 0) _chart1.SetData(vf, up, lo, title, col);
                double[] vb = dies.Select(d => d.BackMax).ToArray();
                SampleData.Series(Mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
                if (vb.Length > 0) _chart2.SetData(vb, up, lo, title, col);

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
                if (v1.Length > 0) _chart1.SetData(v1, up, lo, title, col);
                double[] v2 = InspectionResultStore.Series(mode, k2);
                SampleData.Series(Mode, 1, out up, out lo, out title, out col); ApplyChartLimits(1, ref up, ref lo);
                if (v2.Length > 0) _chart2.SetData(v2, up, lo, title, col);
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

            if (Mode == InspectionMode.Bottom) BuildBottomMaps();   // 위치별 4-맵 갱신
        }

        // ── 4-맵(Width · Height · 1ch · 2ch ChippingSize) — Bottom 전용, 열=Picker(1~4)·행=사이클 ──
        // 픽업이 1→2→3→4 순차 진행되므로 각 픽업을 열로 두고 사이클마다 아래로 누적(레퍼런스 세로 스트립 형태).
        private void BuildBottomMaps()
        {
            if (Mode != InspectionMode.Bottom || _waferMap == null) return;
            var hist = InspectionResultStore.History(InspectionResultStore.Bottom);
            if (hist.Count == 0) { _waferMap.SetMaps(null, null, null, null); return; }

            // 픽업(1~4)별로 시퀀스 순서대로 분류
            var byP = new System.Collections.Generic.List<InspectionResultStore.Item>[4];
            for (int i = 0; i < 4; i++) byP[i] = new System.Collections.Generic.List<InspectionResultStore.Item>();
            bool anyPicker = false; int rows = 0;
            foreach (var it in hist)
                if (it.Picker >= 1 && it.Picker <= 4) { byP[it.Picker - 1].Add(it); anyPicker = true; }
            if (!anyPicker) { _waferMap.SetMaps(null, null, null, null); return; }   // 픽업 미지정(컨텍스트 전) → 빈 맵
            for (int i = 0; i < 4; i++) if (byP[i].Count > rows) rows = byP[i].Count;
            if (rows <= 0) { _waferMap.SetMaps(null, null, null, null); return; }
            if (rows > 200) rows = 200;   // 표시 한도

            double[,] w = NewNaN(rows, 4), h = NewNaN(rows, 4), c1 = NewNaN(rows, 4), c2 = NewNaN(rows, 4);
            double V(InspectionResultStore.Item it, string k) => it.Values.TryGetValue(k, out double v) ? v : double.NaN;
            for (int p = 0; p < 4; p++)
            {
                var list = byP[p];
                int start = list.Count > rows ? list.Count - rows : 0;   // 최근 rows 개
                for (int r = 0; r + start < list.Count && r < rows; r++)
                {
                    var it = list[start + r];
                    w[r, p] = V(it, "Width");
                    h[r, p] = V(it, "Height");
                    // 1 Channel = 상/하 에지 칩핑 max, 2 Channel = 좌/우 에지 칩핑 max (장비 정의 확인 시 조정).
                    c1[r, p] = MaxNaN(V(it, "Chipping Top"),  V(it, "Chipping Bottom"));
                    c2[r, p] = MaxNaN(V(it, "Chipping Left"), V(it, "Chipping Right"));
                }
            }
            // Width/Height=평균 대비 편차 크기, Chipping=절대 크기로 정규화(0=흰색, 1=적색).
            _waferMap.SetMaps(NormDev(w), NormDev(h), NormMag(c1), NormMag(c2));
        }

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

        /// <summary>차트 상/하한을 레시피 기반 ChartLimitStore 값으로 덮어쓴다(없으면 기본값 유지).</summary>
        private void ApplyChartLimits(int which, ref double up, ref double lo)
        {
            if (QMC.Vision.Core.ChartLimitStore.TryGet(ModeKey(), which, out double u, out double l) && !(u == 0 && l == 0))
            { up = u; lo = l; }
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
