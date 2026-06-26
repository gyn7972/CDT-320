using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Comm;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 → 오토 포커스. 우측 서브 네비(바텀-콜렛/바텀-다이/앞측면/뒤측면) 선택 →
    /// BEST 그리드 + FOCUS 통신 로그 + 포커스 곡선(4색) 표시.
    /// 레이아웃은 Designer, 동작/데이터만 이 파일에 둔다.
    /// 데이터: <see cref="AutoFocusStore"/>(핸들러 TCP FOCUS_*). 로그: <see cref="VisionCommLog"/>.
    /// </summary>
    public sealed partial class AutoFocusPanel : PageBase
    {
        private readonly Font _bold = new Font(UiTheme.ButtonFont, FontStyle.Bold);

        private SidebarButton[] _navBtns;
        private FocusCamera[] _navCam;
        private FocusTarget[] _navTgt;

        private FocusCamera _camera = FocusCamera.Bottom;
        private FocusTarget _target = FocusTarget.Collet;
        private long _lastLogRev = -1;
        private readonly Random _rng = new Random();

        public AutoFocusPanel()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;

            GridTheme.Apply(grid);
            grid.Columns.Add("pickup", "Pickup");
            grid.Columns.Add("bestp", "Best 위치");
            grid.Columns.Add("bests", "Best Score");
            grid.Columns.Add("initp", "초기 위치");
            grid.Columns.Add("n", "N");
            foreach (string c in new[] { "bestp", "bests", "initp", "n" })
                grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["n"].FillWeight = 40;

            _navBtns = new[] { btnNav0, btnNav1, btnNav2, btnNav3 };
            _navCam = new[] { FocusCamera.Bottom, FocusCamera.Bottom, FocusCamera.Front, FocusCamera.Back };
            _navTgt = new[] { FocusTarget.Collet, FocusTarget.Die, FocusTarget.Side, FocusTarget.Side };
            for (int i = 0; i < _navBtns.Length; i++)
            {
                int idx = i;
                _navBtns[i].Click += (s, e) => SelectTarget(idx);
            }

            btnTestScan.Click += (s, e) => SimulateScan();
            btnTestStep.Click += (s, e) => SimulateStep();
            btnReset.Click += (s, e) => ResetSession();
            btnClearLog.Click += (s, e) => { VisionCommLog.Clear(); _lastLogRev = -1; RefreshLog(); };

            timer.Tick += (s, e) => RefreshView();
            SelectTarget(0);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (IsDesignerMode()) return;
            if (Visible) { AttachCurrentModule(); RefreshView(); timer.Start(); }
            else timer.Stop();
        }

        private void SelectTarget(int idx)
        {
            _camera = _navCam[idx];
            _target = _navTgt[idx];
            for (int i = 0; i < _navBtns.Length; i++)
                _navBtns[i].Selected = (i == idx);
            AttachCurrentModule();
            RefreshView();
        }

        private void RefreshView()
        {
            if (IsDesignerMode() || IsDisposed) return;

            AutoFocusSession sess = AutoFocusStore.Get(_camera, _target);
            chart.ChartAreas["main"].AxisX.Title =
                _camera == FocusCamera.Bottom ? "모터 Z (mm)" : "모터 위치 (mm)";

            RefreshGrid(sess);
            RefreshChart(sess);
            RefreshLog();
        }

        private static IVisionModule ModuleFor(Form1 host, FocusCamera cam)
        {
            switch (cam)
            {
                case FocusCamera.Bottom: return host.BottomMod;
                case FocusCamera.Front:  return host.TopSideVisionMod;
                default:                 return host.BottomSideVisionMod;
            }
        }

        /// <summary>라이브 이미지 뷰(CameraView)를 현재 카메라 모듈에 바인딩 — 툴바 Grab/Live 대상.</summary>
        private void AttachCurrentModule()
        {
            Form1 host = FindForm() as Form1;
            if (host == null) return;
            IVisionModule mod = ModuleFor(host, _camera);
            if (mod != null) camView.AttachModule(mod);
        }

        /// <summary>픽업 번호 목록 — 모든 카메라/타깃 공통 Pickup1~4.</summary>
        private int[] PickupNumbers()
        {
            return new[] { 1, 2, 3, 4 };
        }

        private void RefreshGrid(AutoFocusSession sess)
        {
            grid.Rows.Clear();

            if (sess != null)
            {
                foreach (var row in sess.BuildBestTable())   // 락 스냅샷(스레드 안전)
                    AddBestRow("Pickup" + row.PickupNo, row.Color,
                        row.SampleCount > 0 ? row.BestMotorZ.ToString("F3") : "-",
                        row.SampleCount > 0 ? row.BestScore.ToString("F1") : "-",
                        row.InitialMotorZ.HasValue ? row.InitialMotorZ.Value.ToString("F3") : "-",
                        row.SampleCount);
                return;
            }

            for (int k = 0; k < 4; k++)
                AddBestRow("Pickup" + (k + 1), AutoFocusSession.PickupColors[k], "-", "-", "-", 0);
        }

        private void AddBestRow(string label, Color color, string z, string score, string initz, int n)
        {
            int r = grid.Rows.Add(label, z, score, initz, n);
            var cell = grid.Rows[r].Cells[0];
            cell.Style.ForeColor = color;
            cell.Style.Font = _bold;
        }

        private void RefreshChart(AutoFocusSession sess)
        {
            chart.Series.Clear();
            int[] pickups = PickupNumbers();

            for (int k = 0; k < pickups.Length; k++)
            {
                int pno = pickups[k];
                Color color = AutoFocusSession.PickupColors[k % AutoFocusSession.PickupColors.Length];
                string name = "Pickup" + pno;

                List<FocusSample> samples = sess != null ? sess.CopySamples(pno) : new List<FocusSample>();
                bool isDefault = samples.Count == 0;
                if (isDefault) samples = DefaultCurve(k);   // 데이터 없으면 기본 예시 곡선(점선·흐림)

                var ser = new Series(name)
                {
                    ChartType = SeriesChartType.Line,
                    Color = isDefault ? Color.FromArgb(150, color) : color,
                    BorderWidth = 2,
                    BorderDashStyle = isDefault ? ChartDashStyle.Dash : ChartDashStyle.Solid,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = isDefault ? 3 : 4,
                    XValueType = ChartValueType.Double,
                    ChartArea = "main",
                    Legend = "legend"
                };

                int bestIdx = -1;
                double bestScore = double.MinValue;
                foreach (var sample in samples.OrderBy(p => p.MotorZ))
                {
                    int i = ser.Points.AddXY(sample.MotorZ, sample.Score);
                    if (sample.IsInitial)
                    {
                        ser.Points[i].MarkerStyle = MarkerStyle.Diamond;
                        ser.Points[i].MarkerSize = isDefault ? 8 : 11;
                        ser.Points[i].MarkerBorderColor = Color.White;
                    }
                    if (sample.Score > bestScore) { bestScore = sample.Score; bestIdx = i; }
                }

                // 베스트 스코어 강조(별 마커) + 점수 라벨.
                if (bestIdx >= 0)
                {
                    var bp = ser.Points[bestIdx];
                    bp.MarkerStyle = MarkerStyle.Star5;
                    bp.MarkerSize = isDefault ? 11 : 15;
                    bp.MarkerColor = color;
                    bp.MarkerBorderColor = Color.White;
                    bp.MarkerBorderWidth = 2;
                    bp.Label = name + " Best " + bestScore.ToString("F0");
                    bp.LabelForeColor = color;
                    bp.Font = _bold;
                }

                chart.Series.Add(ser);
            }
        }

        // 기본 예시 포커스 곡선(픽업별 정점 위치를 약간 달리한 종형). 실데이터 없을 때 표시용.
        private static readonly double[] DefPeak = { 19.6, 19.9, 20.1, 20.4 };

        private static List<FocusSample> DefaultCurve(int k)
        {
            var list = new List<FocusSample>();
            double peak = DefPeak[k % DefPeak.Length];
            double amp = 180 + k * 8;
            bool first = true;
            for (double z = 18.0; z <= 22.0 + 1e-9; z += 0.2)
            {
                double v = amp * Math.Exp(-Math.Pow(z - peak, 2) / (2 * 0.5 * 0.5));
                list.Add(new FocusSample(Math.Round(z, 2), Math.Round(v), first));
                first = false;
            }
            return list;
        }

        private void RefreshLog()
        {
            long rev = VisionCommLog.Revision;
            if (rev == _lastLogRev) return;
            _lastLogRev = rev;

            string[] focus = VisionCommLog.Snapshot()
                .Where(l => l != null && l.IndexOf("FOCUS", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            txtLog.Lines = focus.Length > 0
                ? focus
                : new[] { "FOCUS 통신 대기 중...", "핸들러 FOCUS_START / FOCUS_VAL 수신 시 표시됩니다." };
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        // ── 테스트(핸들러 없이 UI 시뮬레이션) ───────────────────

        private string ModuleName()
        {
            switch (_camera)
            {
                case FocusCamera.Bottom: return "BottomInspection";
                case FocusCamera.Front:  return "TopSideVision";
                default:                 return "BottomSideVision";
            }
        }

        /// <summary>현재 타깃에 대해 Z 스윕 시뮬레이션(전 픽업) → 세션 누적 + 통신 로그 기록.</summary>
        private void SimulateScan()
        {
            string mod = ModuleName();
            string cam = _camera.ToString().ToUpperInvariant();
            string tgt = _target.ToString().ToUpperInvariant();

            AutoFocusStore.Start(_camera, _target);
            VisionCommLog.Add(mod + "|FOCUS_START|" + cam + "|" + tgt + "  ->  ACK;OK");

            int[] pickups = PickupNumbers();
            for (int k = 0; k < pickups.Length; k++)
            {
                int p = pickups[k];
                double peak = DefPeak[k % DefPeak.Length];
                double amp = 190 + k * 8;
                bool first = true;
                for (double z = 18.0; z <= 22.0 + 1e-9; z += 0.2)
                {
                    double v = amp * Math.Exp(-Math.Pow(z - peak, 2) / (2 * 0.5 * 0.5)) + _rng.Next(-4, 5);
                    if (v < 0) v = 0;
                    double zr = Math.Round(z, 2);
                    double vr = Math.Round(v);
                    AutoFocusStore.AddSample(_camera, _target, p, zr, vr, first);
                    VisionCommLog.Add(mod + "|FOCUS_VAL|" + zr.ToString("F2") + "|" + cam + "|" + tgt +
                                      "|" + p + "|" + (first ? "1" : "0") + "  ->  OK;score=" + vr.ToString("F0"));
                    first = false;
                }
            }
            RefreshView();
        }

        /// <summary>1점만 추가(증분 테스트). Bottom=Pickup1, 측면=0.</summary>
        private void SimulateStep()
        {
            string mod = ModuleName();
            string cam = _camera.ToString().ToUpperInvariant();
            string tgt = _target.ToString().ToUpperInvariant();
            int p = 1;

            double z = Math.Round(18.0 + _rng.NextDouble() * 4.0, 2);
            double v = _rng.Next(40, 220);
            AutoFocusStore.AddSample(_camera, _target, p, z, v);
            VisionCommLog.Add(mod + "|FOCUS_VAL|" + z.ToString("F2") + "|" + cam + "|" + tgt +
                              "|" + p + "|0  ->  OK;score=" + v.ToString("F0"));
            RefreshView();
        }

        /// <summary>현재 타깃 세션 초기화.</summary>
        private void ResetSession()
        {
            AutoFocusStore.Start(_camera, _target);
            VisionCommLog.Add(ModuleName() + "|FOCUS_RESET|" +
                _camera.ToString().ToUpperInvariant() + "|" + _target.ToString().ToUpperInvariant());
            RefreshView();
        }
    }
}
