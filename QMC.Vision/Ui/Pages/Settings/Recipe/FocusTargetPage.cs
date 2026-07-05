using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Comm;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 바텀 검사 '포커스' 타깃 페이지 — 패턴 매칭을 사용하지 않는 오토포커스 전용 화면.
    /// <para>
    /// 오토포커스 ROI1~4 를 드래그로 지정(타깃 콜렛/다이)하고, [포커스 측정]으로 현재 프레임의
    /// ROI별 포커스 Score 를 즉시 확인하며, 핸들러 Z스캔(FOCUS_START/VAL) 세션의 피크 곡선을 표시한다.
    /// </para>
    /// ROI SSOT 는 <see cref="AutoFocusRoiStore"/>(vision.json) — [설정 &gt; 오토 포커스]와 동일 데이터를 공유하고,
    /// 곡선/BEST 데이터는 <see cref="AutoFocusStore"/> 세션에서 읽는다(핸들러 FOCUS 흐름과 동일 소스).
    /// </summary>
    public sealed partial class FocusTargetPage : PageBase, ITargetPage
    {
        // ── Fields ─────────────────────────────────────
        private readonly IVisionModule _module;
        private readonly FocusCamera _camera;
        private FocusTarget _target = FocusTarget.Collet;
        private readonly Font _bold = new Font(UiTheme.ButtonFont, FontStyle.Bold);
        private readonly double?[] _measured = new double?[AutoFocusRoiStore.RoiCount];   // 마지막 [포커스 측정] ROI별 Score
        private IAlgorithmNode _node;                    // FocusFinder 노드 — 조명 레벨(Recipe.LightSettings) SSOT
        private InspectionLightPanel _lightPanel;        // 검사 조명 편집/점등 패널(독립 모드 — 자체 저장/적용)
        private long _lastLogRev = -1;                   // FOCUS 통신 로그 갱신 리비전(변경 시에만 다시 그림)
        private long _lastViewerSeq = -1;                // 모듈 뷰어 프레임 시퀀스 — 프로토콜(핸들러) 그랩 자동 표시용

        // ── Properties (ITargetPage) ───────────────────
        /// <summary>ROI 는 지정 즉시 vision.json 에 저장되므로 별도 dirty 상태가 없다.</summary>
        public bool IsDirty => false;

        /// <summary>현재 타깃에 지정된 ROI 가 하나라도 있으면 저장 데이터 있음.</summary>
        public bool HasSavedData
            => AutoFocusRoiStore.GetRois(_camera, _target).Any(r => r != null && r.Width > 0 && r.Height > 0);

        /// <summary>ITargetPage 계약용 — ROI 즉시 저장 구조라 dirty 전환이 없어 발생하지 않는다.</summary>
        public event EventHandler DirtyChanged;

        // ── Constructor ────────────────────────────────
        public FocusTargetPage()
        {
            InitializeComponent();
        }

        public FocusTargetPage(IVisionModule module, FocusCamera camera, string recipeName = "default")
        {
            _module = module;
            _camera = camera;
            InitializeComponent();
            if (IsDesignerMode()) return;

            // 검사 조명 — FocusFinder 노드의 Recipe.LightSettings 편집/점등(독립 모드: 패널 자체 저장/적용 버튼 사용).
            _node = _module?.GetAlgorithm("FocusFinder");
            _lightPanel = new InspectionLightPanel
            {
                Dock = DockStyle.Fill,
                EmbeddedMode = false,
                RecipeName = string.IsNullOrWhiteSpace(recipeName) ? "default" : recipeName
            };
            // 바텀 검사 채널 정책(리스광 ch2 + 엘파인 P08 ch6~8)은 패널이 SelectInspection 에서 자동 적용.
            _lightPanel.SelectInspection(_node, _module?.AlgorithmKey ?? "", "FocusFinder");
            pnlLightHost.Controls.Add(_lightPanel);
            _lightPanel.BringToFront();   // 호스트 내 Fill 도킹 우선(패널 자체 헤더 포함 전체 채움)

            GridTheme.Apply(grid);
            grid.Columns.Add("roi", "ROI");
            grid.Columns.Add("val", "측정값");
            grid.Columns.Add("bestp", "Best 위치");
            grid.Columns.Add("bests", "Best Score");
            grid.Columns.Add("n", "N");
            foreach (string c in new[] { "val", "bestp", "bests", "n" })
                grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["n"].FillWeight = 40;

            camView.AttachModule(_module);
            camView.SetActiveTool("FocusFinder");   // 툴바 Grab/Live 도 FocusFinder 조명/노출을 적용해 촬상
            camView.RoiEdited += camView_RoiEdited;

            btnTargetCollet.Click += (s, e) => SelectTarget(FocusTarget.Collet);
            btnTargetDie.Click += (s, e) => SelectTarget(FocusTarget.Die);
            var roiBtns = new[] { btnRoi0, btnRoi1, btnRoi2, btnRoi3 };
            for (int i = 0; i < roiBtns.Length; i++)
            {
                int idx = i;
                roiBtns[i].Click += (s, e) => BeginEditRoi(idx);
            }
            btnRoiClear.Click += (s, e) => ClearRois();
            btnExposureApply.Click += (s, e) => ApplyExposure();
            btnMeasure.Click += (s, e) => MeasureRois();

            timerRefresh.Tick += (s, e) => RefreshSessionView();
            SelectTarget(FocusTarget.Collet);
        }

        // ── Event Methods ──────────────────────────────
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (IsDesignerMode() || _module == null) return;
            if (Visible) { UpdateRoiOverlay(); RefreshSessionView(); timerRefresh.Start(); }
            else timerRefresh.Stop();
        }

        /// <summary>드래그 완료 콜백 — "AF{idx}" 종류면 현재 타깃의 ROI{idx+1} 로 저장 + 오버레이 갱신.</summary>
        private void camView_RoiEdited(string kind, Roi roi)
        {
            try
            {
                if (string.IsNullOrEmpty(kind) || !kind.StartsWith("AF")) return;
                int idx;
                if (!int.TryParse(kind.Substring(2), out idx)) return;

                bool ok = AutoFocusRoiStore.SetRoi(_camera, _target, idx, roi);
                if (ok)
                    VisionCommLog.Add("[FocusTargetPage] ROI" + (idx + 1) + " 저장 — " +
                                      "x=" + roi.CenterX.ToString("F0") + " y=" + roi.CenterY.ToString("F0") +
                                      " w=" + roi.Width.ToString("F0") + " h=" + roi.Height.ToString("F0") +
                                      " (" + _camera + "/" + _target + ")");
                else
                    VisionCommLog.Add("[FocusTargetPage] ROI" + (idx + 1) + " 저장 실패 — 설정 저장 오류.");
                UpdateRoiOverlay();
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] ROI 저장 처리 실패: " + ex.Message);
            }
        }

        // ── Public Methods (ITargetPage) ───────────────
        /// <summary>ROI 는 지정 즉시 저장되므로 별도 저장 동작 없음.</summary>
        public void SaveTarget() { }

        /// <summary>저장된 ROI 를 다시 읽어 오버레이/표를 갱신한다.</summary>
        public void LoadTarget()
        {
            UpdateRoiOverlay();
            RefreshSessionView();
            try { DirtyChanged?.Invoke(this, EventArgs.Empty); } catch { }
        }

        /// <summary>조명 지정(SettingsPage) 변경을 조명 그리드에 반영 — RecipePage 가 타깃 표시 시 호출.</summary>
        public void RefreshLightAssignment()
            => _lightPanel?.SelectInspection(_node, _module?.AlgorithmKey ?? "", "FocusFinder");

        // ── Private Methods ────────────────────────────
        /// <summary>포커스 대상(콜렛/다이) 전환 — ROI/노출/측정값/곡선을 해당 타깃 것으로 갱신.</summary>
        private void SelectTarget(FocusTarget target)
        {
            _target = target;
            for (int i = 0; i < _measured.Length; i++) _measured[i] = null;
            btnTargetCollet.BackColor = target == FocusTarget.Collet ? UiTheme.Accent : Color.White;
            btnTargetCollet.ForeColor = target == FocusTarget.Collet ? Color.White : Color.FromArgb(0x22, 0x22, 0x22);
            btnTargetDie.BackColor = target == FocusTarget.Die ? UiTheme.Accent : Color.White;
            btnTargetDie.ForeColor = target == FocusTarget.Die ? Color.White : Color.FromArgb(0x22, 0x22, 0x22);
            LoadExposureField();
            UpdateRoiOverlay();
            RefreshSessionView();
        }

        /// <summary>저장된 오토포커스 노출(µs)을 입력 필드에 표시. 미지정(0)이면 빈칸.</summary>
        private void LoadExposureField()
        {
            try
            {
                double us = AutoFocusRoiStore.GetExposureUs(_camera, _target);
                txtExposure.Text = us > 0 ? us.ToString("F0") : "";
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] 노출 값 로드 실패: " + ex.Message);
            }
        }

        /// <summary>[노출 적용] — 입력값(µs)을 (카메라,타깃) 오토포커스 노출로 저장하고 카메라에 즉시 적용.
        /// 빈칸/0 이면 미지정으로 저장(스캔 시 현재 카메라 노출 유지). FOCUS_START 가 스캔 전 자동 적용한다.</summary>
        private void ApplyExposure()
        {
            try
            {
                double us = 0;
                string raw = txtExposure.Text.Trim();
                if (raw.Length > 0 &&
                    (!double.TryParse(raw, System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture, out us) || us < 0))
                {
                    QMC.Common.MessageDialog.Show("노출 시간(µs)을 숫자로 입력하세요.", "포커스 노출",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool ok = AutoFocusRoiStore.SetExposureUs(_camera, _target, us);
                if (!ok)
                {
                    VisionCommLog.Add("[FocusTargetPage] 노출 저장 실패 (" + _camera + "/" + _target + ")");
                    return;
                }

                if (us > 0 && _module?.Camera != null)
                {
                    _module.Camera.ExposureUs = us;   // 즉시 카메라 반영(측정/라이브 확인용)
                    VisionCommLog.Add("[FocusTargetPage] 포커스 노출 저장+적용 " + us.ToString("F0") + "µs (" +
                                      _camera + "/" + _target + ")");
                }
                else
                {
                    VisionCommLog.Add("[FocusTargetPage] 포커스 노출 미지정으로 저장 — 스캔 시 현재 카메라 노출 유지 (" +
                                      _camera + "/" + _target + ")");
                }
                LoadExposureField();
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] 노출 적용 실패: " + ex.Message);
            }
        }

        /// <summary>ROI{idx+1} 드래그 편집 진입. 기존 값이 있으면 그 위치에서 시작.</summary>
        private void BeginEditRoi(int idx)
        {
            try
            {
                Roi current = AutoFocusRoiStore.GetRoi(_camera, _target, idx);
                camView.BeginRoiDrag("AF" + idx, current);
                VisionCommLog.Add("[FocusTargetPage] ROI" + (idx + 1) + " 지정 — 이미지에 사각형을 드래그하세요. (" +
                                  _camera + "/" + _target + ")");
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] ROI 편집 진입 실패: " + ex.Message);
            }
        }

        /// <summary>현재 타깃의 ROI1~4 전체 삭제 + 오버레이 갱신.</summary>
        private void ClearRois()
        {
            try
            {
                AutoFocusRoiStore.ClearRois(_camera, _target);
                for (int i = 0; i < _measured.Length; i++) _measured[i] = null;
                VisionCommLog.Add("[FocusTargetPage] ROI 전체 삭제 (" + _camera + "/" + _target + ")");
                UpdateRoiOverlay();
                RefreshSessionView();
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] ROI 삭제 실패: " + ex.Message);
            }
        }

        /// <summary>[포커스 측정] — 1장 그랩 후 ROI1~4 각각의 포커스 Score 를 계산해 표에 표시.
        /// 그랩은 카메라 블록이 있어 워커에서 수행하고 UI 는 완료 시 갱신한다.</summary>
        private void MeasureRois()
        {
            if (_module == null) return;
            Roi[] rois = AutoFocusRoiStore.GetRois(_camera, _target);
            if (!rois.Any(r => r != null && r.Width > 0 && r.Height > 0))
            {
                VisionCommLog.Add("[FocusTargetPage] 측정 불가 — ROI 가 지정되지 않았습니다. (" + _camera + "/" + _target + ")");
                return;
            }

            btnMeasure.Enabled = false;
            FocusTarget target = _target;
            Task.Run(() =>
            {
                var scores = new double?[AutoFocusRoiStore.RoiCount];
                string error = null;
                try
                {
                    // 촬상 준비 — FocusFinder 조명/노출 + 포커스 전용 노출(페이지의 '노출 적용' 저장값) 적용.
                    //   핸들러 FOCUS_START 와 동일 경로라 측정 조건이 스캔과 일치한다.
                    VisionCommandCore.PrepareFocusAcquisition(_module, _camera, target);
                    using (GrabResult g = _module.Grab())
                    {
                        if (g == null || !g.IsSuccess || g.Image == null)
                        {
                            error = g != null ? g.ErrorMessage : "grab 결과 없음";
                        }
                        else
                        {
                            for (int i = 0; i < rois.Length; i++)
                            {
                                Roi roi = rois[i];
                                if (roi == null || roi.Width <= 0 || roi.Height <= 0) continue;
                                using (Bitmap crop = CropRoi(g.Image, roi))
                                    if (crop != null) scores[i] = AutoFocusCore.Score(crop);
                            }
                        }
                    }
                }
                catch (Exception ex) { error = ex.Message; }

                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        btnMeasure.Enabled = true;
                        if (error != null)
                        {
                            VisionCommLog.Add("[FocusTargetPage] 포커스 측정 실패: " + error);
                            return;
                        }
                        if (_target != target) return;   // 측정 중 타깃 전환 — 결과 폐기
                        var log = new System.Text.StringBuilder("[FocusTargetPage] 포커스 측정 (" + _camera + "/" + target + ")");
                        for (int i = 0; i < scores.Length; i++)
                        {
                            _measured[i] = scores[i];
                            if (scores[i].HasValue) log.Append("  ROI" + (i + 1) + "=" + scores[i].Value.ToString("F1"));
                        }
                        VisionCommLog.Add(log.ToString());
                        // 그랩된 프레임 즉시 표시(모듈 뷰어 탭 반영분) — 타이머(400ms) 대기 없이 바로 갱신.
                        RefreshSessionView();
                    }));
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[FocusTargetPage] 측정 UI 갱신 실패: " + ex.Message); }
            });
        }

        /// <summary>ROI 영역을 프레임 경계로 클램프해 잘라낸다. 유효 영역이 없으면 null.</summary>
        private static Bitmap CropRoi(Bitmap src, Roi roi)
        {
            try
            {
                var b = roi.BoundingBox;
                int x = Math.Max(0, (int)Math.Round((double)b.X));
                int y = Math.Max(0, (int)Math.Round((double)b.Y));
                int r = Math.Min(src.Width, (int)Math.Round((double)(b.X + b.Width)));
                int bt = Math.Min(src.Height, (int)Math.Round((double)(b.Y + b.Height)));
                int w = r - x, h = bt - y;
                if (w <= 1 || h <= 1) return null;
                return src.Clone(new Rectangle(x, y, w, h), src.PixelFormat);
            }
            catch { return null; }
        }

        // ── UI Update Methods ──────────────────────────
        /// <summary>현재 타깃의 ROI1~4 를 카메라뷰에 4색 오버레이로 표시.</summary>
        private void UpdateRoiOverlay()
        {
            if (IsDisposed) return;
            try
            {
                Roi[] rois = AutoFocusRoiStore.GetRois(_camera, _target);
                camView.SetAutoFocusRois(rois, AutoFocusSession.PickupColors);
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[FocusTargetPage] ROI 오버레이 갱신 실패: " + ex.Message);
            }
        }

        /// <summary>세션(핸들러 Z스캔) + 마지막 측정값으로 표/피크 곡선/통신 로그/프레임 갱신.</summary>
        private void RefreshSessionView()
        {
            if (IsDesignerMode() || IsDisposed) return;
            AutoFocusSession sess = AutoFocusStore.Get(_camera, _target);
            RefreshGrid(sess);
            RefreshChart(sess);
            RefreshFocusLog();
            RefreshViewerFrame();
        }

        /// <summary>FOCUS 관련 통신/동작 로그를 하단 스트립에 표시(변경 시에만).
        /// 핸들러 FOCUS_START/VAL/BEST RX·TX 와 이 페이지의 ROI/노출/측정 로그가 모두 "FOCUS" 를 포함한다.</summary>
        private void RefreshFocusLog()
        {
            try
            {
                long rev = VisionCommLog.Revision;
                if (rev == _lastLogRev) return;
                _lastLogRev = rev;

                string[] lines = VisionCommLog.Snapshot()
                    .Where(l => l != null && l.IndexOf("FOCUS", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
                if (lines.Length > 200) lines = lines.Skip(lines.Length - 200).ToArray();   // 표시량 상한

                txtFocusLog.Lines = lines.Length > 0
                    ? lines
                    : new[] { "FOCUS 통신 대기 중... (핸들러 FOCUS_START/VAL/BEST 수신 시 표시)" };
                txtFocusLog.SelectionStart = txtFocusLog.TextLength;
                txtFocusLog.ScrollToCaret();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[FocusTargetPage] 로그 갱신 실패: " + ex.Message); }
        }

        /// <summary>프로토콜(핸들러 GRAB/FOCUS_VAL 등)로 그랩된 최신 프레임을 카메라뷰에 자동 표시.
        /// 모듈 뷰어 탭(ViewerFrameSeq) 변경을 감지해 갱신한다. 라이브 중에는 라이브 스트림이 우선.</summary>
        private void RefreshViewerFrame()
        {
            try
            {
                if (_module == null || camView.IsLive) return;
                long seq = _module.ViewerFrameSeq;
                if (seq == _lastViewerSeq) return;
                _lastViewerSeq = seq;
                if (seq <= 0) return;
                Bitmap f = _module.AcquireViewerFrame();
                if (f == null) return;
                try { camView.SetImage(f); }   // 내부 복제 — 원본은 여기서 해제
                finally { f.Dispose(); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[FocusTargetPage] 프레임 갱신 실패: " + ex.Message); }
        }

        private void RefreshGrid(AutoFocusSession sess)
        {
            grid.Rows.Clear();
            List<FocusBestRow> best = sess != null ? sess.BuildBestTable() : null;

            for (int k = 0; k < AutoFocusRoiStore.RoiCount; k++)
            {
                FocusBestRow row = best != null ? best.FirstOrDefault(x => x.PickupNo == k + 1) : null;
                string val = _measured[k].HasValue ? _measured[k].Value.ToString("F1") : "-";
                string bp = row != null && row.SampleCount > 0 ? row.BestMotorZ.ToString("F3") : "-";
                string bs = row != null && row.SampleCount > 0 ? row.BestScore.ToString("F1") : "-";
                int n = row != null ? row.SampleCount : 0;

                int r = grid.Rows.Add("ROI" + (k + 1), val, bp, bs, n);
                var cell = grid.Rows[r].Cells[0];
                cell.Style.ForeColor = AutoFocusSession.PickupColors[k % AutoFocusSession.PickupColors.Length];
                cell.Style.Font = _bold;
            }
        }

        /// <summary>ROI1~4 포커스 곡선(핸들러 Z스캔 세션) — 베스트 점은 ★ 마커로 강조.</summary>
        private void RefreshChart(AutoFocusSession sess)
        {
            chart.Series.Clear();
            for (int k = 0; k < AutoFocusRoiStore.RoiCount; k++)
            {
                int pno = k + 1;
                Color color = AutoFocusSession.PickupColors[k % AutoFocusSession.PickupColors.Length];
                var ser = new Series("ROI" + pno)
                {
                    ChartType = SeriesChartType.Line,
                    Color = color,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 4,
                    XValueType = ChartValueType.Double,
                    ChartArea = "main",
                    Legend = "legend"
                };

                List<FocusSample> samples = sess != null ? sess.CopySamples(pno) : new List<FocusSample>();
                int bestIdx = -1;
                double bestScore = double.MinValue;
                foreach (FocusSample sample in samples.OrderBy(p => p.MotorZ))
                {
                    int i = ser.Points.AddXY(sample.MotorZ, sample.Score);
                    if (sample.IsInitial)
                    {
                        ser.Points[i].MarkerStyle = MarkerStyle.Diamond;
                        ser.Points[i].MarkerSize = 11;
                        ser.Points[i].MarkerBorderColor = Color.White;
                    }
                    if (sample.Score > bestScore) { bestScore = sample.Score; bestIdx = i; }
                }

                if (bestIdx >= 0)
                {
                    var bp = ser.Points[bestIdx];
                    bp.MarkerStyle = MarkerStyle.Star5;
                    bp.MarkerSize = 15;
                    bp.MarkerColor = color;
                    bp.MarkerBorderColor = Color.White;
                    bp.MarkerBorderWidth = 2;
                    bp.Label = "ROI" + pno + " Best " + bestScore.ToString("F0");
                    bp.LabelForeColor = color;
                    bp.Font = _bold;
                }

                chart.Series.Add(ser);
            }
        }
    }
}
