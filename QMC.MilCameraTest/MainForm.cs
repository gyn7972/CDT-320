using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Vision.Cameras.Mil;
using QMC.Vision.Core;

namespace QMC.MilCameraTest
{
    /// <summary>
    /// MilCamera 단독 구동 테스트 폼.
    /// Open → Grab(단발) / Live(연속) 로 이미지를 표시하고, ExposureEnded(HW 노출 종료) 이벤트를
    /// 수신해 카운트/시각을 표시한다. 카메라 제어는 ICamera 인터페이스만 사용한다.
    /// </summary>
    public partial class MainForm : Form
    {
        // ── Fields ─────────────────────────────────────
        private ICamera _camera;
        private int _exposureEndCount;
        private bool _closing;
        private readonly LFineLightTester _light = new LFineLightTester();   // 엘파인 LCP24 조명(페이지별 채널 밝기)
        private int _lightUiPage = 1;        // 그리드에 표시 중인 UI 페이지(1~12)
        private bool _suspendLightUi;        // 페이지 콤보 프로그램적 변경 중 이벤트 억제

        // ── Constructor ────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            InitializeLightUi();
            AddLog("프로그램 시작 — Camera Id 입력 후 Open (예: Mil/0)");
        }

        // ── Initialize Methods ─────────────────────────
        /// <summary>조명 UI 초기화 — 저장 설정(lfine_light.json) 복원 + 페이지(1~12) 콤보 + 채널 1~16 행 구성.
        /// 바텀 돔(CH6~8) 용도 표기. UI 페이지 1~12 = 장비 와이어 페이지 00~11.</summary>
        private void InitializeLightUi()
        {
            try
            {
                _light.LoadConfig();
                txtLightPort.Text = _light.PortName;

                _suspendLightUi = true;
                try
                {
                    cmbLightPage.Items.Clear();
                    for (int p = 1; p <= LFineLightTester.PageCount; p++)
                        cmbLightPage.Items.Add(p + " 페이지 (P" + (p - 1).ToString("00") + ")");
                    _lightUiPage = _light.SelectedPage;
                    cmbLightPage.SelectedIndex = _lightUiPage - 1;

                    gridLight.Rows.Clear();
                    int[] buf = _light.GetPageBuffer(_lightUiPage);
                    for (int ch = 1; ch <= LFineLightTester.ChannelCount; ch++)
                    {
                        string usage = (ch >= 6 && ch <= 8) ? "바텀 돔" + (ch - 5) : "";
                        gridLight.Rows.Add(ch, usage, buf[ch - 1]);
                    }
                }
                finally { _suspendLightUi = false; }
            }
            catch (Exception ex) { AddLog("조명 UI 초기화 실패: " + ex.Message); }
        }

        // ── Event Methods (UI) ─────────────────────────
        private void btnOpen_Click(object sender, EventArgs e)
        {
            _ = OpenCameraAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _ = CloseCameraAsync();
        }

        private void btnGrab_Click(object sender, EventArgs e)
        {
            AddLog("Grab 버튼 클릭");
            _ = GrabOnceAsync();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            ClearLog();
        }

        private void btnLive_Click(object sender, EventArgs e)
        {
            _ = ToggleLiveAsync();
        }

        private void btnLightConnect_Click(object sender, EventArgs e)
        {
            ToggleLightConnect();
        }

        private void btnLightApply_Click(object sender, EventArgs e)
        {
            ApplyLightChannels();
        }

        private void btnLightSave_Click(object sender, EventArgs e)
        {
            SaveLightToDevice();
        }

        private void cmbLightPage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendLightUi) return;
            SwitchLightPage(cmbLightPage.SelectedIndex + 1);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            try { CleanupCamera(); }
            catch (Exception ex) { AddLogSafe("종료 정리 실패: " + ex.Message); }
            try { _light.SaveConfig(); _light.Dispose(); }
            catch (Exception ex) { AddLogSafe("조명 정리 실패: " + ex.Message); }
        }

        // ── Event Methods (Camera — MIL 내부 스레드에서 호출) ──
        private void OnCameraFrameReceived(GrabResult r)
        {
            try
            {
                if (_closing || r == null || !r.IsSuccess) return;
                var bmp = r.DetachImage();
                if (bmp == null) return;
                try { BeginInvoke(new Action(() => ShowImage(bmp))); }
                catch { bmp.Dispose(); }   // 폼 종료 중 마샬링 실패 — 이미지 누수만 방지
            }
            catch (Exception ex) { AddLogSafe("FrameReceived 처리 실패: " + ex.Message); }
        }

        private void OnCameraExposureEnded()
        {
            try
            {
                int n = System.Threading.Interlocked.Increment(ref _exposureEndCount);
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                if (_closing) return;
                BeginInvoke(new Action(() =>
                {
                    lblExposureEnd.Text = "ExposureEnd: " + n;
                    AddLog("ExposureEnd #" + n + " 수신 (" + time + ")");
                }));
            }
            catch (Exception ex) { AddLogSafe("ExposureEnd 처리 실패: " + ex.Message); }
        }

        private void OnCameraConnectionChanged(CameraConnectionEvent ev)
        {
            AddLogSafe("연결 상태: " + ev);
        }

        // ── Sequence / Motion Methods (카메라 제어) ─────
        private async Task OpenCameraAsync()
        {
            if (_camera != null) { AddLog("이미 열려 있습니다. Close 후 다시 시도하세요."); return; }

            string id = txtCameraId.Text.Trim();
            if (string.IsNullOrEmpty(id)) id = "Mil/0";
            btnOpen.Enabled = false;
            try
            {
                var camera = new MilCamera(new CameraInfo { Id = id, Vendor = "Matrox", Transport = CameraTransport.CoaXPress });
                camera.FrameReceived     += OnCameraFrameReceived;
                camera.ExposureEnded     += OnCameraExposureEnded;
                camera.ConnectionChanged += OnCameraConnectionChanged;

                await Task.Run(() => camera.Open());

                _camera = camera;
                _exposureEndCount = 0;
                AddLog("Open 성공: " + id + " (" + camera.Resolution.Width + "x" + camera.Resolution.Height + ")");
            }
            catch (Exception ex)
            {
                AddLog("Open 실패: " + ex.Message);
                MessageBox.Show(this, "카메라 Open 실패\n" + ex.Message, "MilCameraTest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdateUiState();
            }
        }

        private async Task CloseCameraAsync()
        {
            if (_camera == null) return;
            btnClose.Enabled = false;
            btnGrab.Enabled  = false;
            btnLive.Enabled  = false;
            try
            {
                await Task.Run(() => CleanupCamera());
                AddLog("Close 완료");
            }
            catch (Exception ex)
            {
                AddLog("Close 실패: " + ex.Message);
                MessageBox.Show(this, "카메라 Close 실패\n" + ex.Message, "MilCameraTest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdateUiState();
            }
        }

        private async Task GrabOnceAsync()
        {
            var camera = _camera;
            if (camera == null) return;
            btnGrab.Enabled = false;
            try
            {
                // 동기 그랩(MdigGrab 블록) — UI Thread 를 막지 않도록 워커에서 수행.
                var r = await Task.Run(() => camera.Grab(3000));
                if (r != null && r.IsSuccess)
                {
                    ShowImage(r.DetachImage());
                    AddLog("Grab 성공 (frame " + r.FrameNumber + ", " + r.GrabTime.ToString("HH:mm:ss.fff") + ")");
                }
                else
                {
                    string msg = r?.ErrorMessage ?? "unknown";
                    AddLog("Grab 실패: " + msg);
                    MessageBox.Show(this, "Grab 실패\n" + msg, "MilCameraTest",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AddLog("Grab 예외: " + ex.Message);
                MessageBox.Show(this, "Grab 예외\n" + ex.Message, "MilCameraTest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdateUiState();
            }
        }

        private async Task ToggleLiveAsync()
        {
            var camera = _camera;
            if (camera == null) return;
            btnLive.Enabled = false;
            try
            {
                if (!camera.IsGrabbing)
                {
                    await Task.Run(() => camera.StartLive());
                    AddLog("Live 시작");
                }
                else
                {
                    // StopLive 는 MdigHalt 로 블록될 수 있으므로 워커에서 수행.
                    await Task.Run(() => camera.StopLive());
                    AddLog("Live 정지");
                }
            }
            catch (Exception ex)
            {
                AddLog("Live 전환 실패: " + ex.Message);
                MessageBox.Show(this, "Live 전환 실패\n" + ex.Message, "MilCameraTest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UpdateUiState();
            }
        }

        // ── Private Methods (조명 — 엘파인 LCP24) ──────
        /// <summary>조명 시리얼 연결/해제 토글. 실패 시 로그 + 메시지박스.</summary>
        private void ToggleLightConnect()
        {
            try
            {
                if (_light.IsOpen)
                {
                    _light.Close();
                    btnLightConnect.Text = "연결";
                    txtLightPort.Enabled = true;
                    AddLog("조명 연결 해제 (" + _light.PortName + ")");
                    return;
                }

                string port = txtLightPort.Text.Trim();
                if (string.IsNullOrEmpty(port)) port = "COM4";
                if (!_light.Open(port, out string err))
                {
                    AddLog("조명 연결 실패 [" + port + "]: " + err);
                    MessageBox.Show(this, "조명 연결 실패 [" + port + "]\n" + err +
                        "\n\n(QMC.Vision 실행 중이면 같은 포트를 점유하므로 먼저 종료하세요.)",
                        "엘파인 조명", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                btnLightConnect.Text = "해제";
                txtLightPort.Enabled = false;
                AddLog("조명 연결 (" + port + ", 115200bps)");
            }
            catch (Exception ex)
            {
                AddLog("조명 연결 처리 실패: " + ex.Message);
            }
        }

        /// <summary>페이지 전환 — 현재 그리드 값을 이전 페이지 버퍼에 보관 후 새 페이지 값을 표시.</summary>
        private void SwitchLightPage(int newUiPage)
        {
            try
            {
                int[] current = CollectLightValues();
                if (current != null)
                    Array.Copy(current, _light.GetPageBuffer(_lightUiPage), LFineLightTester.ChannelCount);

                _lightUiPage = newUiPage;
                _light.SelectedPage = newUiPage;
                RefreshLightGrid();
                AddLog("조명 페이지 전환 → " + newUiPage + " 페이지 (장비 P" + (newUiPage - 1).ToString("00") + ")");
            }
            catch (Exception ex)
            {
                AddLog("조명 페이지 전환 실패: " + ex.Message);
            }
        }

        /// <summary>그리드 채널 값(µs)을 현재 페이지에 일괄 적용(SP) + 설정 저장. 실패 시 로그+메시지박스.</summary>
        private void ApplyLightChannels()
        {
            try
            {
                int[] values = CollectLightValues();
                if (values == null) return;
                if (!_light.ApplyChannels(_lightUiPage, values, out string err))
                {
                    AddLog("조명 적용 실패: " + err);
                    MessageBox.Show(this, "조명 적용 실패\n" + err, "엘파인 조명",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _light.SaveConfig();
                RefreshLightGrid();   // 클램프(0~1500)된 실제 적용값 재표시
                AddLog("조명 적용(SP) — " + _lightUiPage + " 페이지 (장비 P" + (_lightUiPage - 1).ToString("00") + ") [" +
                       string.Join(",", _light.GetPageBuffer(_lightUiPage)) + "] µs");
            }
            catch (Exception ex)
            {
                AddLog("조명 적용 처리 실패: " + ex.Message);
            }
        }

        /// <summary>현재 페이지 값을 장비 플래시에 저장(WP) — 전원 재투입 후에도 유지.</summary>
        private void SaveLightToDevice()
        {
            try
            {
                if (!_light.SaveToDevice(_lightUiPage, out string err))
                {
                    AddLog("장비 저장 실패: " + err);
                    MessageBox.Show(this, "장비 저장 실패\n" + err, "엘파인 조명",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AddLog("장비 저장(WP) 완료 — " + _lightUiPage + " 페이지 (장비 P" + (_lightUiPage - 1).ToString("00") + ")");
            }
            catch (Exception ex)
            {
                AddLog("장비 저장 처리 실패: " + ex.Message);
            }
        }

        /// <summary>그리드에서 채널 1~16 ON-TIME(µs) 수집. 숫자 아님/범위 밖이면 안내 후 null.</summary>
        private int[] CollectLightValues()
        {
            var values = new int[LFineLightTester.ChannelCount];
            foreach (DataGridViewRow row in gridLight.Rows)
            {
                if (row.IsNewRow) continue;
                if (!int.TryParse(row.Cells["colLightCh"].Value?.ToString(), out int ch) ||
                    ch < 1 || ch > LFineLightTester.ChannelCount) continue;
                string raw = row.Cells["colLightOnTime"].Value?.ToString()?.Trim() ?? "0";
                if (!int.TryParse(raw, out int us) || us < 0 || us > LFineLightTester.MaxOnTimeUs)
                {
                    AddLog("CH" + ch + " 값 오류: '" + raw + "' (0~" + LFineLightTester.MaxOnTimeUs + "µs)");
                    MessageBox.Show(this, "CH" + ch + " ON-TIME 값이 올바르지 않습니다: '" + raw +
                        "'\n0~" + LFineLightTester.MaxOnTimeUs + "µs 범위로 입력하세요 (10µs 단위).",
                        "엘파인 조명", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                values[ch - 1] = us;
            }
            return values;
        }

        // ── Private Methods ────────────────────────────
        /// <summary>카메라 이벤트 해제 + Live 정지 + Close/Dispose. (워커/UI 어디서든 호출 가능)</summary>
        private void CleanupCamera()
        {
            var camera = _camera;
            _camera = null;
            if (camera == null) return;
            try
            {
                camera.FrameReceived     -= OnCameraFrameReceived;
                camera.ExposureEnded     -= OnCameraExposureEnded;
                camera.ConnectionChanged -= OnCameraConnectionChanged;
                camera.StopLive();
                camera.Close();
            }
            finally
            {
                camera.Dispose();
            }
        }

        // ── UI Update Methods ──────────────────────────
        private void ShowImage(System.Drawing.Bitmap bmp)
        {
            if (bmp == null) return;
            try
            {
                var old = picView.Image;
                picView.Image = bmp;
                old?.Dispose();
            }
            catch (Exception ex)
            {
                bmp.Dispose();
                AddLog("이미지 표시 실패: " + ex.Message);
            }
        }

        private void UpdateUiState()
        {
            bool open = _camera != null && _camera.IsOpen;
            bool live = open && _camera.IsGrabbing;
            btnOpen.Enabled  = !open;
            btnClose.Enabled = open;
            btnGrab.Enabled  = open && !live;
            btnLive.Enabled  = open;
            btnLive.Text     = live ? "Live Stop" : "Live";
            txtCameraId.Enabled = !open;
            lblStatus.Text = open ? (live ? "Live" : "Opened") : "Closed";
            lblStatus.ForeColor = open ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DimGray;
        }

        /// <summary>조명 그리드 값 갱신 — 현재 페이지 버퍼(클램프 반영)를 다시 표시.</summary>
        private void RefreshLightGrid()
        {
            try
            {
                int[] buf = _light.GetPageBuffer(_lightUiPage);
                foreach (DataGridViewRow row in gridLight.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (int.TryParse(row.Cells["colLightCh"].Value?.ToString(), out int ch) &&
                        ch >= 1 && ch <= LFineLightTester.ChannelCount)
                        row.Cells["colLightOnTime"].Value = buf[ch - 1];
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("RefreshLightGrid 실패: " + ex.Message); }
        }

        /// <summary>로그 리스트 전체 비우기.</summary>
        private void ClearLog()
        {
            try
            {
                lstLog.Items.Clear();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("ClearLog 실패: " + ex.Message); }
        }

        private void AddLog(string msg)
        {
            try
            {
                lstLog.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg);
                if (lstLog.Items.Count > 500) lstLog.Items.RemoveAt(0);
                lstLog.TopIndex = lstLog.Items.Count - 1;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("AddLog 실패: " + ex.Message); }
        }

        /// <summary>비 UI 스레드에서 안전하게 로그 추가.</summary>
        private void AddLogSafe(string msg)
        {
            try
            {
                if (_closing || IsDisposed) return;
                if (InvokeRequired) BeginInvoke(new Action(() => AddLog(msg)));
                else AddLog(msg);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("AddLogSafe 실패: " + ex.Message); }
        }
    }
}
