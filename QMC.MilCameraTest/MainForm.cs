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

        // ── Constructor ────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            AddLog("프로그램 시작 — Camera Id 입력 후 Open (예: Mil/0)");
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
            _ = GrabOnceAsync();
        }

        private void btnLive_Click(object sender, EventArgs e)
        {
            _ = ToggleLiveAsync();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            try { CleanupCamera(); }
            catch (Exception ex) { AddLogSafe("종료 정리 실패: " + ex.Message); }
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
