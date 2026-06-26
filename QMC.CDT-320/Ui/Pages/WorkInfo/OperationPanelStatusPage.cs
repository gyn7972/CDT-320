using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OperationPanelStatusPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;

        public OperationPanelStatusPage()
        {
            InitializeComponent();
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                Refresh4();
            };
            VisibleChanged += (s, e) => { if (Visible) _timer.Start(); else _timer.Stop(); };
            HandleDestroyed += (s, e) => _timer.Stop();

            _btnCommTest.Click += async (s, e) => await RunCommTest();

            _cbVisionModule.Items.AddRange(new object[] { "Wafer", "Inspection", "Bin", "TopSide", "BottomSide" });
            _cbVisionModule.SelectedIndex = 0;
            _btnGrab.Click += async (s, e) => await RunGrab();
            _btnMatch.Click += async (s, e) => await RunMatch();
            _btnInspect.Click += async (s, e) => await RunInspect();
        }

        /// <summary>선택한 Vision 모듈 채널.</summary>
        private VisionTcpClient SelectedVisionModule()
        {
            switch (_cbVisionModule.SelectedIndex)
            {
                case 0:  return VisionHub.Wafer;
                case 1:  return VisionHub.Inspection;
                case 2:  return VisionHub.Bin;
                case 3:  return VisionHub.TopSide;
                case 4:  return VisionHub.BottomSide;
                default: return VisionHub.Wafer;
            }
        }

        /// <summary>선택 모듈에 EXPOSE(1장 그랩) 요청 후 결과(w/h/frame)를 표시.</summary>
        private async Task RunGrab()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            if (c == null)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblGrabResult.Text = name + ": 모듈 없음";
                return;
            }
            if (!c.IsConnected)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblGrabResult.Text = $"{name} ({c.Port}): 미연결 — 먼저 연결하세요";
                return;
            }

            _btnGrab.Enabled = false;
            _lblGrabResult.ForeColor = System.Drawing.Color.DimGray;
            _lblGrabResult.Text = name + " GRAB 중...";
            try
            {
                VisionProtocolResponse resp = await c.SendCommandAsync(VisionProtocolCommand.Grab, 5000, System.Threading.CancellationToken.None, 0);
                bool ok = resp != null && resp.IsAck;
                string body = resp != null ? resp.Payload : "응답 없음";
                _lblGrabResult.ForeColor = ok ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                _lblGrabResult.Text = $"{c.ModuleName} GRAB: {(ok ? body : (resp != null ? resp.RawLine : body))}";
            }
            catch (Exception ex)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblGrabResult.Text = name + " GRAB 실패: " + ex.Message;
                MessageBox.Show(name + " GRAB 실패: " + ex.Message, "VISION 동작 테스트",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnGrab.Enabled = true;
            }
        }

        /// <summary>선택 모듈에 MATCH(finder) 요청 → 좌표/각도/score 데이터 파싱·표시.</summary>
        private async Task RunMatch()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            string finder = (_txtFinder.Text ?? "").Trim();
            if (c == null || !c.IsConnected)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblMatchResult.Text = $"{name}: 미연결 — 먼저 연결하세요";
                return;
            }
            if (finder.Length == 0)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblMatchResult.Text = "finder 이름을 입력하세요";
                return;
            }

            _btnMatch.Enabled = false;
            _lblMatchResult.ForeColor = System.Drawing.Color.DimGray;
            _lblMatchResult.Text = $"{name} MATCH({finder}) 중...";
            try
            {
                MatchResultDto r = await c.MatchAsync(finder);
                if (r.Success)
                {
                    _lblMatchResult.ForeColor = System.Drawing.Color.SeaGreen;
                    _lblMatchResult.Text = $"{c.ModuleName} MATCH OK  x={r.X:F2}  y={r.Y:F2}  θ={r.AngleDeg:F3}  score={r.Score:F3}";
                }
                else
                {
                    _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                    _lblMatchResult.Text = $"{c.ModuleName} MATCH 실패: {(string.IsNullOrEmpty(r.RawError) ? "no match" : r.RawError)}";
                }
            }
            catch (Exception ex)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblMatchResult.Text = name + " MATCH 실패: " + ex.Message;
                MessageBox.Show(name + " MATCH 실패: " + ex.Message, "VISION 동작 테스트",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnMatch.Enabled = true;
            }
        }

        /// <summary>선택 모듈에 INSPECT(inspector) 요청 → PASS/FAIL 판정 데이터 파싱·표시.</summary>
        private async Task RunInspect()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            string inspector = (_txtInspector.Text ?? "").Trim();
            if (c == null || !c.IsConnected)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblInspectResult.Text = $"{name}: 미연결 — 먼저 연결하세요";
                return;
            }
            if (inspector.Length == 0)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblInspectResult.Text = "inspector 이름을 입력하세요";
                return;
            }

            _btnInspect.Enabled = false;
            _lblInspectResult.ForeColor = System.Drawing.Color.DimGray;
            _lblInspectResult.Text = $"{name} INSPECT({inspector}) 중...";
            try
            {
                InspectionResultDto r = await c.InspectAsync(inspector);
                bool acked = r.Raw != null && r.Raw.StartsWith("ACK|");
                if (acked)
                {
                    _lblInspectResult.ForeColor = r.IsPass ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                    _lblInspectResult.Text = $"{c.ModuleName} INSPECT: {(r.IsPass ? "PASS" : "FAIL")}   ({r.Raw})";
                }
                else
                {
                    _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                    _lblInspectResult.Text = $"{c.ModuleName} INSPECT 실패: {r.Raw}";
                }
            }
            catch (Exception ex)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblInspectResult.Text = name + " INSPECT 실패: " + ex.Message;
                MessageBox.Show(name + " INSPECT 실패: " + ex.Message, "VISION 동작 테스트",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnInspect.Enabled = true;
            }
        }

        /// <summary>미접속이면 ConnectAll 후 6채널(Wafer/Inspection/Bin/Main/TopSide/BottomSide) PING → 채널별 OK/FAIL 표시.</summary>
        private async Task RunCommTest()
        {
            _btnCommTest.Enabled = false;
            _lblCommResult.ForeColor = System.Drawing.Color.DimGray;
            _lblCommResult.Text = "통신 테스트 중...";
            try
            {
                var cfg = AppSettingsStore.Current;
                if (!VisionHub.AnyConnected)
                {
                    await VisionHub.ConnectAllAsync(
                        cfg.VisionHost,
                        cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                        cfg.VisionMainPort,  cfg.VisionTopSidePort,    cfg.VisionBottomSidePort);
                }

                int ok = 0, total = 0;
                var sb = new StringBuilder();
                foreach (var ch in new[]
                {
                    ("Wafer",      VisionHub.Wafer),
                    ("Inspection", VisionHub.Inspection),
                    ("Bin",        VisionHub.Bin),
                    ("Main",       VisionHub.Main),
                    ("TopSide",    VisionHub.TopSide),
                    ("BottomSide", VisionHub.BottomSide),
                })
                {
                    total++;
                    string line = await PingLine(ch.Item1, ch.Item2);
                    if (line.Contains(": OK")) ok++;
                    sb.AppendLine(line);
                }

                string summary = $"통신 테스트 완료 — {ok}/{total} OK";
                _lblCommResult.ForeColor = (ok == total) ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                _lblCommResult.Text = summary;
                MessageBox.Show(sb.ToString().TrimEnd(), summary,
                    MessageBoxButtons.OK, (ok == total) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                _lblCommResult.ForeColor = System.Drawing.Color.Firebrick;
                _lblCommResult.Text = "통신 테스트 실패: " + ex.Message;
                MessageBox.Show("통신 테스트 실패: " + ex.Message, "VISION 통신 테스트",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnCommTest.Enabled = true;
            }
        }

        private static async Task<string> PingLine(string name, VisionTcpClient c)
        {
            if (c == null) return $"{name}: - (없음)";
            if (!c.IsConnected) return $"{name} ({c.Port}): 미연결";
            bool ok = false;
            try { ok = await c.PingAsync(); } catch { ok = false; }
            return $"{name} ({c.Port}): {(ok ? "OK" : "FAIL")}";
        }

        private void Refresh4()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            var op = host.Machine.OpPanelUnit;
            var res = host.Machine.ResourcesUnit;
            var ion = host.Machine.IonizerUnit;

            if (op != null)
            {
                _dotStart.IsOn = op.StartButton.IsOn;
                _dotStop.IsOn = op.StopButton.IsOn;
                _dotReset.IsOn = op.ResetButton.IsOn;
                _dotEmgF.IsOn = op.EmgFront.IsOn;
                _dotEmgL.IsOn = op.EmgLeft.IsOn;
                _dotEmgR.IsOn = op.EmgRear.IsOn;

                _ledStartLamp.IsOn = op.StartLamp.IsOn;
                _ledStopLamp.IsOn = op.StopLamp.IsOn;
                _ledResetLamp.IsOn = op.ResetLamp.IsOn;

                _tlRed.IsOn = op.TlRed.IsOn;
                _tlYellow.IsOn = op.TlYellow.IsOn;
                _tlGreen.IsOn = op.TlGreen.IsOn;
                _ledBuzzer.IsOn = op.Buzzer.IsOn;
            }

            if (res != null)
            {
                _dotCda1.IsOn = res.MainCda1Check.IsOn;
                _dotCda2.IsOn = res.MainCda2Check.IsOn;
                _dotVac1.IsOn = res.MainVacuum1Check.IsOn;
                _dotVac2.IsOn = res.MainVacuum2Check.IsOn;
                _dotVac3.IsOn = res.MainVacuum3Check.IsOn;
                _dotVac4.IsOn = res.MainVacuum4Check.IsOn;
            }

            if (ion != null)
            {
                _dotIonizer.IsOn = ion.IsHealthy;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

