using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Comm;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Dialogs;

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
        private Button[] _roiBtns;

        /// <summary>TCP 스캔 테스트에서 각 FOCUS_VAL 전 Z축 이동 시간 시뮬레이션(ms). 실모션이 없는 시뮬에서
        /// "Z 이동 후 측정" 흐름을 재현(0이면 즉시 연속 전송). 실핸들러에선 실제 Z 이동 시간이 이 역할.</summary>
        private int SimZMoveDelayMs = 200;

        private FocusCamera _camera = FocusCamera.Bottom;
        private FocusTarget _target = FocusTarget.Collet;
        private long _lastLogRev = -1;
        private long _lastTactRev = -1;
        private IVisionModule _module;      // 라이브 이미지 뷰 프레임 탭 대상(현재 카메라 모듈)
        private long _lastViewerSeq = -1;   // ViewerFrameSeq 변경 감지용(핸들러 TCP 그랩 프레임 자동 표시)
        private readonly Random _rng = new Random();

        public AutoFocusPanel()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;

            GridTheme.Apply(grid);
            grid.Columns.Add("pickup", "ROI");
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
            btnTcpScan.Click += (s, e) => TcpScan();
            btnTcpVal.Click += (s, e) => TcpStep();
            btnImgSeq.Click += (s, e) => RunImageSequence();
            btnTestStep.Click += (s, e) => SimulateStep();
            btnReset.Click += (s, e) => ResetSession();
            btnClearLog.Click += (s, e) =>
            {
                VisionCommLog.Clear(); _lastLogRev = -1; RefreshLog();
                AutoFocusTactLog.Clear(); _lastTactRev = -1; RefreshTact();
            };

            // ROI1~4 지정(이미지 드래그) + 전체 지우기 — 현재 선택 타깃에 저장.
            _roiBtns = new[] { btnRoi0, btnRoi1, btnRoi2, btnRoi3 };
            for (int i = 0; i < _roiBtns.Length; i++)
            {
                int idx = i;
                _roiBtns[i].Click += (s, e) => BeginEditRoi(idx);
            }
            btnRoiClear.Click += (s, e) => ClearCurrentRois();
            btnRoiJog.Click += (s, e) => OpenRoiJog();
            btnProcImg.Click += (s, e) => OpenProcImg();
            camView.RoiEdited += camView_RoiEdited;

            timer.Tick += (s, e) => { RefreshView(); RefreshViewerFrame(); };
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
            CloseRoiPopups();   // 이전 타깃에 묶인 팝업 닫기
            ClearSimGrabOverride();   // 이전 카메라 grab 오버라이드 해제
            _camera = _navCam[idx];
            _target = _navTgt[idx];
            for (int i = 0; i < _navBtns.Length; i++)
                _navBtns[i].Selected = (i == idx);
            AttachCurrentModule();
            UpdateRoiOverlay();
            RefreshView();
        }

        // ── ROI 편집 (현재 선택 타깃의 ROI1~4 — [설정 > 오토 포커스] 에서만 지정) ──

        /// <summary>ROI{idx+1} 드래그 편집 진입. 기존 값이 있으면 그 위치에서 시작.</summary>
        private void BeginEditRoi(int idx)
        {
            try
            {
                Roi current = AutoFocusRoiStore.GetRoi(_camera, _target, idx);
                camView.BeginRoiDrag("AF" + idx, current);
                VisionCommLog.Add("[AutoFocusPanel] ROI" + (idx + 1) + " 지정 — 이미지에 사각형을 드래그하세요. (" +
                                  _camera + "/" + _target + ")");
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[AutoFocusPanel] ROI 편집 진입 실패: " + ex.Message);
            }
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
                    VisionCommLog.Add("[AutoFocusPanel] ROI" + (idx + 1) + " 저장 — " +
                                      "x=" + roi.CenterX.ToString("F0") + " y=" + roi.CenterY.ToString("F0") +
                                      " w=" + roi.Width.ToString("F0") + " h=" + roi.Height.ToString("F0") +
                                      " (" + _camera + "/" + _target + ")");
                else
                    VisionCommLog.Add("[AutoFocusPanel] ROI" + (idx + 1) + " 저장 실패 — 설정 저장 오류.");

                UpdateRoiOverlay();
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[AutoFocusPanel] ROI 저장 처리 실패: " + ex.Message);
            }
        }

        /// <summary>현재 타깃의 ROI1~4 전체 삭제 + 오버레이 갱신.</summary>
        private void ClearCurrentRois()
        {
            try
            {
                AutoFocusRoiStore.ClearRois(_camera, _target);
                VisionCommLog.Add("[AutoFocusPanel] ROI 전체 삭제 (" + _camera + "/" + _target + ")");
                UpdateRoiOverlay();
            }
            catch (Exception ex)
            {
                VisionCommLog.Add("[AutoFocusPanel] ROI 삭제 실패: " + ex.Message);
            }
        }

        private RoiJogDialog _roiJogDlg;
        private ProcessedImageDialog _procDlg;

        /// <summary>ROI 조그 팝업 열기(모덜리스). 변경 시 오버레이 갱신.</summary>
        private void OpenRoiJog()
        {
            try
            {
                if (_roiJogDlg != null && !_roiJogDlg.IsDisposed) { _roiJogDlg.Activate(); return; }
                _roiJogDlg = new RoiJogDialog(_camera, _target);
                _roiJogDlg.RoiChanged += () => { try { UpdateRoiOverlay(); } catch { } };
                _roiJogDlg.Show(FindForm());
            }
            catch (Exception ex) { VisionCommLog.Add("[AutoFocusPanel] ROI 조그 열기 실패: " + ex.Message); }
        }

        /// <summary>처리 이미지 팝업 열기(모덜리스). 현재 카메라 프레임을 처리해 표시.</summary>
        private void OpenProcImg()
        {
            try
            {
                if (_procDlg != null && !_procDlg.IsDisposed) { _procDlg.Activate(); return; }
                // 처리 대상 = 카메라뷰에 현재 표시된 원본 프레임(Grab/Live/Load 모두 반영, 오버레이 없음).
                Func<Bitmap> provider = () =>
                {
                    try { Bitmap f = camView.CurrentFrame; return f != null ? (Bitmap)f.Clone() : null; }
                    catch { return null; }
                };
                _procDlg = new ProcessedImageDialog(_camera, _target, provider);
                _procDlg.Show(FindForm());
            }
            catch (Exception ex) { VisionCommLog.Add("[AutoFocusPanel] 처리 이미지 열기 실패: " + ex.Message); }
        }

        /// <summary>타깃 전환 시 열려있는 팝업은 닫는다(이전 타깃에 묶여 있으므로).</summary>
        private void CloseRoiPopups()
        {
            try { if (_roiJogDlg != null && !_roiJogDlg.IsDisposed) _roiJogDlg.Close(); } catch { }
            try { if (_procDlg != null && !_procDlg.IsDisposed) _procDlg.Close(); } catch { }
            _roiJogDlg = null; _procDlg = null;
        }

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
                VisionCommLog.Add("[AutoFocusPanel] ROI 오버레이 갱신 실패: " + ex.Message);
            }
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
            RefreshTact();
        }

        private void RefreshTact()
        {
            long rev = AutoFocusTactLog.Revision;
            if (rev == _lastTactRev) return;
            _lastTactRev = rev;
            txtTact.Lines = AutoFocusTactLog.Snapshot();
            txtTact.SelectionStart = txtTact.TextLength;
            txtTact.ScrollToCaret();
        }

        private static IVisionModule ModuleFor(Form1 host, FocusCamera cam)
        {
            switch (cam)
            {
                case FocusCamera.Bottom: return host.BottomMod;
                case FocusCamera.Front:  return host.FrontSideVisionMod;
                default:                 return host.RearSideVisionMod;
            }
        }

        /// <summary>라이브 이미지 뷰(CameraView)를 현재 카메라 모듈에 바인딩 — 툴바 Grab/Live 대상.</summary>
        private void AttachCurrentModule()
        {
            Form1 host = FindForm() as Form1;
            if (host == null) return;
            IVisionModule mod = ModuleFor(host, _camera);
            if (mod != null) { camView.AttachModule(mod); camView.StageName = mod.Name; }   // 명칭=선택 카메라, W/H=실그랩 자동
            _module = mod;              // 뷰어 탭 대상 갱신
            _lastViewerSeq = -1;        // 새 모듈 → 다음 프레임을 반드시 한 번 표시
        }

        /// <summary>핸들러 TCP(FOCUS_*/GRAB) 로 그랩된 최신 프레임을 라이브 이미지 뷰에 자동 표시.
        /// 모듈 뷰어 탭(ViewerFrameSeq) 변경을 감지해 갱신한다. 라이브 중에는 라이브 스트림이 우선.
        /// (FocusTargetPage.RefreshViewerFrame 과 동일 패턴 — AutoFocus 페이지에 누락돼 있어
        ///  실측 데이터는 갱신되지만 라이브 이미지가 'No Image' 로 남던 문제 수정.)</summary>
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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[AutoFocusPanel] 프레임 갱신 실패: " + ex.Message); }
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
                    AddBestRow("ROI" + row.PickupNo, row.Color,
                        row.SampleCount > 0 ? row.BestMotorZ.ToString("F3") : "-",
                        row.SampleCount > 0 ? row.BestScore.ToString("F1") : "-",
                        row.InitialMotorZ.HasValue ? row.InitialMotorZ.Value.ToString("F3") : "-",
                        row.SampleCount);
                return;
            }

            for (int k = 0; k < 4; k++)
                AddBestRow("ROI" + (k + 1), AutoFocusSession.PickupColors[k], "-", "-", "-", 0);
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
                string name = "ROI" + pno;

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
                case FocusCamera.Front:  return "FrontSideVision";
                default:                 return "RearSideVision";
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
            ClearSimGrabOverride();
            VisionCommLog.Add(ModuleName() + "|FOCUS_RESET|" +
                _camera.ToString().ToUpperInvariant() + "|" + _target.ToString().ToUpperInvariant());
            RefreshView();
        }

        // ── 실 TCP 통신 테스트(핸들러처럼 자기 자신 서버 포트로 접속) ─────────

        /// <summary>TCP 테스트 시 화면(camView)에 표시된 프레임을 모듈 grab 소스로 주입 — ROI 좌표 일치.</summary>
        private void ApplySimGrabFromView()
        {
            try
            {
                Form1 host = FindForm() as Form1;
                if (host == null) return;
                IVisionModule vm = ModuleFor(host, _camera);
                Bitmap f = camView.CurrentFrame;
                if (vm != null && f != null) vm.SetSimOverrideImage(f);
            }
            catch { }
        }

        /// <summary>grab 오버라이드 해제(테스트 종료/타깃 전환 시).</summary>
        private void ClearSimGrabOverride()
        {
            try
            {
                Form1 host = FindForm() as Form1;
                if (host == null) return;
                IVisionModule vm = ModuleFor(host, _camera);
                if (vm != null) vm.SetSimOverrideImage(null);
            }
            catch { }
        }

        private static int PortFor(FocusCamera cam)
        {
            var cfg = VisionConfigStore.Current;
            switch (cam)
            {
                case FocusCamera.Bottom: return cfg.InspectionVisionPort;
                case FocusCamera.Front:  return cfg.FrontSidePort;
                default:                 return cfg.RearSidePort;
            }
        }

        private void SetTestButtonsEnabled(bool en)
        {
            try { btnTcpScan.Enabled = en; btnTcpVal.Enabled = en; btnTestScan.Enabled = en; btnImgSeq.Enabled = en; }
            catch { }
        }

        /// <summary>한 줄 송신 후 응답 한 줄 수신(서버가 RX/TX 를 통신 로그에 남김). 모든 FOCUS 명령은 응답을 받는다.</summary>
        private static string SendRecv(NetworkStream ns, string line, int timeoutMs = 5000)
        {
            byte[] data = Encoding.UTF8.GetBytes(line + "\n");
            ns.Write(data, 0, data.Length);
            ns.ReadTimeout = timeoutMs;
            var sb = new StringBuilder();
            int b;
            while ((b = ns.ReadByte()) != -1)
            {
                if (b == '\n') break;
                if (b != '\r') sb.Append((char)b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 실제 TCP로 FOCUS_START → (Z당 1회 FOCUS_VAL) → BEST 전송. 새 ROI 설계와 동일하게
        /// 한 Z(=한 grab)에서 서버가 ROI1~4 를 채점한다(픽업별 4콜 아님). Z 21스텝 = 21콜.
        /// 루프백은 정지 프레임이라 곡선은 평탄할 수 있음(실 곡선=핸들러 실 Z / 이미지 시퀀스).
        /// </summary>
        private void TcpScan()
        {
            FocusCamera cam = _camera; FocusTarget tgt = _target;
            int port = PortFor(cam);
            string mod = ModuleName();
            string camS = cam.ToString().ToUpperInvariant();
            string tgtS = tgt.ToString().ToUpperInvariant();
            var inv = CultureInfo.InvariantCulture;

            ApplySimGrabFromView();   // 화면 프레임을 grab 소스로(ROI 정렬)
            SetTestButtonsEnabled(false);
            Task.Run(() =>
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect("127.0.0.1", port);
                        using (var ns = client.GetStream())
                        {
                            SendRecv(ns, mod + "|FOCUS_START|" + camS + "|" + tgtS);
                            bool first = true;
                            for (double z = 18.0; z <= 22.0 + 1e-9; z += 0.2)
                            {
                                // 실핸들러 흐름 흉내: [Z축을 이 위치로 이동(이동 시간 대기) → 그 자리에서 측정 요청].
                                // 시뮬은 실제 모션이 없으므로 이동 시간을 딜레이로 대신한다(없으면 데이터가 즉시 쏟아져 비현실적).
                                if (SimZMoveDelayMs > 0) System.Threading.Thread.Sleep(SimZMoveDelayMs);

                                // Z당 1콜 — FOCUS_VAL 응답 = "그랩 완료" ACK(채점은 백그라운드). ACK 받고 다음 Z로.
                                // 타임아웃 넉넉히(10s): 큐 backpressure 로 ACK 가 백그라운드 처리만큼 지연될 수 있음.
                                SendRecv(ns, mod + "|FOCUS_VAL|" + Math.Round(z, 2).ToString("F2", inv) +
                                             "|" + camS + "|" + tgtS + "|1|" + (first ? "1" : "0"), 10000);
                                first = false;
                            }
                            // 완료 신호 — 서버가 백그라운드 처리 완료를 기다린 뒤 best 응답(처리 시간 고려 긴 타임아웃).
                            SendRecv(ns, mod + "|FOCUS_BEST|" + camS + "|" + tgtS, 120000);
                        }
                    }
                }
                catch (Exception ex) { VisionCommLog.Add("[AutoFocusPanel] TCP 스캔 오류: " + ex.Message); }
                finally { try { BeginInvoke((Action)(() => SetTestButtonsEnabled(true))); } catch { } }
            });
        }

        private double _tcpStepZ = 18.0;       // TCP 스텝 현재 Z
        private bool _tcpStepNeedStart = true; // 첫 스텝이면 FOCUS_START 먼저

        /// <summary>
        /// TCP 스텝 — 클릭마다 다음 Z(0.2씩)로 FOCUS_VAL 1회 전송(실통신). 첫 클릭은 FOCUS_START 선행,
        /// 마지막(>22) 클릭에 FOCUS_BEST 후 리셋. 핸들러처럼 한 스텝씩 수동 진행할 때 사용.
        /// </summary>
        private void TcpStep()
        {
            FocusCamera cam = _camera; FocusTarget tgt = _target;
            int port = PortFor(cam);
            string mod = ModuleName();
            string camS = cam.ToString().ToUpperInvariant();
            string tgtS = tgt.ToString().ToUpperInvariant();
            var inv = CultureInfo.InvariantCulture;

            double z = Math.Round(_tcpStepZ, 2);
            bool start = _tcpStepNeedStart;
            bool init = start;

            // 다음 클릭용 상태 갱신
            _tcpStepNeedStart = false;
            _tcpStepZ = Math.Round(z + 0.2, 2);
            bool finish = _tcpStepZ > 22.0 + 1e-9;
            if (finish) { _tcpStepZ = 18.0; _tcpStepNeedStart = true; }

            ApplySimGrabFromView();   // 화면 프레임을 grab 소스로(ROI 정렬)
            Task.Run(() =>
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect("127.0.0.1", port);
                        using (var ns = client.GetStream())
                        {
                            if (start) SendRecv(ns, mod + "|FOCUS_START|" + camS + "|" + tgtS);
                            // FOCUS_VAL 응답 = "그랩 완료" ACK(채점은 백그라운드) — 받고 다음 스텝으로.
                            SendRecv(ns, mod + "|FOCUS_VAL|" + z.ToString("F2", inv) +
                                         "|" + camS + "|" + tgtS + "|1|" + (init ? "1" : "0"));
                            // 마지막 스텝에서만 완료 신호 → 백그라운드 처리 완료 후 best 응답.
                            if (finish) SendRecv(ns, mod + "|FOCUS_BEST|" + camS + "|" + tgtS, 120000);
                        }
                    }
                }
                catch (Exception ex) { VisionCommLog.Add("[AutoFocusPanel] TCP 스텝 오류: " + ex.Message); }
            });
        }

        // ── 이미지 시퀀스 테스트(시퀀서식 + 실제 tact time) ──────────────────

        private static string CamFolder(FocusCamera cam)
        {
            switch (cam)
            {
                case FocusCamera.Bottom: return "Bottom";
                case FocusCamera.Front:  return "Front";
                default:                 return "Back";
            }
        }

        /// <summary>
        /// 포커스가 서로 다른 10장을 순차로 grab(파일)→Score 하며 단계별·전체 tact time 측정.
        /// 파일(TestImages\AutoFocus\&lt;cam&gt;\*.png)이 있으면 사용, 없으면 메모리 생성.
        /// </summary>
        private void RunImageSequence()
        {
            FocusCamera cam = _camera; FocusTarget tgt = _target;
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestImages", "AutoFocus", CamFolder(cam));
            var inv = CultureInfo.InvariantCulture;

            SetTestButtonsEnabled(false);
            Task.Run(() =>
            {
                List<Bitmap> imgs = null;
                try
                {
                    imgs = LoadFocusImages(dir);
                    AutoFocusStore.Start(cam, tgt);
                    AutoFocusTactLog.MarkCycleStart("ImageSeq " + cam + "/" + tgt);
                    VisionCommLog.Add("[ImageSeq] START cam=" + cam + " target=" + tgt +
                                      " imgs=" + imgs.Count + " (" + (Directory.Exists(dir) ? "파일" : "메모리생성") + ")");

                    double zStep = imgs.Count > 1 ? 4.0 / (imgs.Count - 1) : 0.4;
                    var swTotal = Stopwatch.StartNew();
                    long sumMs = 0;
                    for (int k = 0; k < imgs.Count; k++)
                    {
                        double z = Math.Round(18.0 + k * zStep, 3);
                        var sw = Stopwatch.StartNew();
                        double score = AutoFocusCore.Score(imgs[k]);   // 실제 채점(=tact 측정 대상)
                        sw.Stop();
                        sumMs += sw.ElapsedMilliseconds;
                        AutoFocusStore.AddSample(cam, tgt, 1, z, Math.Round(score), k == 0);
                        AutoFocusTactLog.Add("z=" + z.ToString("F2", inv) +
                                             "  algo=" + sw.ElapsedMilliseconds + "ms" +
                                             "  score=" + score.ToString("F1", inv));
                        VisionCommLog.Add("[ImageSeq] " + (k + 1).ToString("00") + "/" + imgs.Count +
                                          "  z=" + z.ToString("F2", inv) +
                                          "  score=" + score.ToString("F0") +
                                          "  tact=" + sw.ElapsedMilliseconds + "ms");
                    }
                    swTotal.Stop();
                    long avg = imgs.Count > 0 ? sumMs / imgs.Count : 0;
                    AutoFocusTactLog.MarkCycleEnd("ImageSeq " + cam + "/" + tgt);
                    VisionCommLog.Add("[ImageSeq] DONE  total=" + swTotal.ElapsedMilliseconds +
                                      "ms  avg=" + avg + "ms/step  (" + imgs.Count + " steps)");
                }
                catch (Exception ex) { VisionCommLog.Add("[ImageSeq] 오류: " + ex.Message); }
                finally
                {
                    if (imgs != null) foreach (var b in imgs) { try { b.Dispose(); } catch { } }
                    try { BeginInvoke((Action)(() => SetTestButtonsEnabled(true))); } catch { }
                }
            });
        }

        private static List<Bitmap> LoadFocusImages(string dir)
        {
            var list = new List<Bitmap>();
            if (Directory.Exists(dir))
            {
                foreach (string f in Directory.GetFiles(dir, "*.png").OrderBy(x => x))
                {
                    try { using (var tmp = Image.FromFile(f)) list.Add(new Bitmap(tmp)); }
                    catch { }
                }
            }
            if (list.Count == 0)
                for (int k = 0; k < 10; k++) list.Add(GenFocusBitmap(k, 5));
            return list;
        }

        /// <summary>포커스 변화 합성 이미지(메모리 폴백). focusIdx 에서 가장 선명, 멀수록 블러.</summary>
        private static Bitmap GenFocusBitmap(int k, int focusIdx)
        {
            const int W = 640, H = 480;
            Bitmap baseBmp = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(baseBmp))
            {
                g.Clear(Color.FromArgb(40, 40, 40));
                g.FillRectangle(Brushes.Gray, 180, 120, 280, 240);
                using (Pen pen = new Pen(Color.White, 1))
                    for (int x = 190; x < 460; x += 6) g.DrawLine(pen, x, 130, x, 350);
                g.FillRectangle(Brushes.LightGray, 300, 200, 40, 40);
            }

            int dist = Math.Abs(k - focusIdx);
            if (dist == 0) return baseBmp;

            int factor = 1 + dist * 2;
            int sw = Math.Max(1, W / factor), sh = Math.Max(1, H / factor);
            using (Bitmap small = new Bitmap(sw, sh))
            {
                using (Graphics g = Graphics.FromImage(small)) { g.InterpolationMode = InterpolationMode.Bilinear; g.DrawImage(baseBmp, 0, 0, sw, sh); }
                Bitmap blur = new Bitmap(W, H);
                using (Graphics g = Graphics.FromImage(blur)) { g.InterpolationMode = InterpolationMode.Bilinear; g.DrawImage(small, 0, 0, W, H); }
                baseBmp.Dispose();
                return blur;
            }
        }
    }
}
