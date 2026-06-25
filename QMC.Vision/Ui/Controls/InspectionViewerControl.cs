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
            double[] s1 = SampleData.Series(mode, 0, out up, out lo, out title, out col);
            _chart1.SetData(s1, up, lo, title, col);
            double[] s2 = SampleData.Series(mode, 1, out up, out lo, out title, out col);
            _chart2.SetData(s2, up, lo, title, col);

            // 웨이퍼 맵(Bottom 전용) — 중앙=흰색 / 상·하한 근접=붉음
            if (mode == InspectionMode.Bottom)
                _waferMap.SetData(SampleData.WaferRatios());

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

        private void OnStoreChanged(string mode)
        {
            if (IsDisposed || !string.Equals(mode, ModeKey(), StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                if (InvokeRequired) BeginInvoke((Action)RefreshFromStore);
                else RefreshFromStore();
            }
            catch { }
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
                // Side 4채널(Front ch1/2, Back ch1/2) 픽커별 바인딩 — 시퀀서/실데이터
                for (int p = 1; p <= 4; p++)
                    for (int c = 0; c < 4; c++)
                    {
                        var it = InspectionResultStore.LatestChannel(mode, p, c);
                        if (it != null && it.Image != null)
                            pks[p - 1].SetChannel(c, it.Image, it.Box, it.Pass, it.Pass ? "Good" : "NG", it.Lines, MarksOf(it));
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
            string k1, k2; ChartKeys(out k1, out k2);
            double up, lo; string title; Color col;
            double[] v1 = InspectionResultStore.Series(mode, k1);
            SampleData.Series(Mode, 0, out up, out lo, out title, out col);
            if (v1.Length > 0) _chart1.SetData(v1, up, lo, title, col);
            double[] v2 = InspectionResultStore.Series(mode, k2);
            SampleData.Series(Mode, 1, out up, out lo, out title, out col);
            if (v2.Length > 0) _chart2.SetData(v2, up, lo, title, col);

            // 결과 그리드 — 헤더 컬럼명으로 Values 매핑
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
        }

        private static PointF[] MarksOf(InspectionResultStore.Item it)
            => it.Defects == null ? null : it.Defects.Select(d => new PointF((float)d.X, (float)d.Y)).ToArray();

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
