using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 알고리즘 1개의 카메라 매핑 + 카메라 파라미터 편집 패널.
    /// Stage 96 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 콤보채움·바인딩·연결/라이브 로직은 Code.
    /// </summary>
    public partial class CameraMappingPanel : UserControl
    {
        private string _algorithm;
        private bool   _suspendBinding;

        // C1 — 카메라 설정 SSOT = 모듈(BaseUnit) Config/Recipe. 패널은 AlgorithmCameraMapping 을
        // 워킹 버퍼로만 쓰고, 로드는 module.ExportCameraMapping / 저장은 module.ImportCameraMapping+SaveSettings.
        private AlgorithmCameraMapping _buffer;

        /// <summary>현재 알고리즘의 운영 모듈(Form1) — 없으면 null(테스트/디자인 시 구 store fallback).</summary>
        private Modules.IVisionModule Module()
            => string.IsNullOrEmpty(_algorithm) ? null : (FindForm() as Form1)?.ResolveModule(_algorithm);

        // ── 연결 상태 ──
        private ICamera _activeCam;
        private bool    _activeCamOwned;
        private bool    _isLive;
        private DateTime _fpsT0 = DateTime.Now;
        private int      _fpsCount;
        private SynchronizationContext _uiCtx;

        public CameraMappingPanel()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _uiCtx = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            BuildCombos();
            UpdateConnectButtons();
        }

        /// <summary>콤보 Items(enum) 동적 채움 — 런타임.</summary>
        private void BuildCombos()
        {
            foreach (var v in Enum.GetNames(typeof(CameraTriggerMode))) _cbTrigger.Items.Add(v);
            foreach (var v in Enum.GetNames(typeof(CameraPixelFormat))) _cbPixel.Items.Add(v);
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnAnyFieldChanged(object sender, EventArgs e) => OnFieldChanged();
        private void OnDiscoverClick(object sender, EventArgs e) => DiscoverCameras();
        private void OnMilDcfCheckedChanged(object sender, EventArgs e) { UpdateMilDcfVisibility(); OnMilFieldChanged(); }
        private void OnMilTextChanged(object sender, EventArgs e) => OnMilFieldChanged();
        private void OnSaveClick(object sender, EventArgs e) => SaveAll();
        private void OnCancelClick(object sender, EventArgs e) => CancelChanges();
        private void OnResetClick(object sender, EventArgs e) => ResetToDefaults();
        private void OnApplyClick(object sender, EventArgs e) => ApplyToRunningModule();
        private void OnTestGrabClick(object sender, EventArgs e) => TestGrab();
        private void OnConnectClick(object sender, EventArgs e) => ToggleConnect();
        private void OnLiveStartClick(object sender, EventArgs e) => LiveStart();
        private void OnLiveStopClick(object sender, EventArgs e) => LiveStop();

        public void SelectAlgorithm(string algorithm)
        {
            _algorithm = algorithm;
            _buffer    = null;   // 모듈 Config/Recipe 에서 새로 로드
            _lblAlgorithm.Text = "카메라 매핑 — " + VisionAlgorithm.Label(algorithm) + "  (" + algorithm + ")";
            BindFields();
            ResetScrollAsync();
        }

        /// <summary>Container 의 ActiveControl 변경 영향이 끝난 뒤 scroll position 을 원점으로 강제 복귀.</summary>
        private void ResetScrollAsync()
        {
            if (_body == null) return;
            BeginInvoke((Action)(() =>
            {
                try { _body.AutoScrollPosition = Point.Empty; } catch { }
            }));
        }

        private AlgorithmCameraMapping CurrentMapping()
        {
            if (string.IsNullOrEmpty(_algorithm)) return null;
            if (_buffer == null || !string.Equals(_buffer.Algorithm, _algorithm, StringComparison.OrdinalIgnoreCase))
                _buffer = LoadBuffer();
            return _buffer;
        }

        /// <summary>모듈 Config/Recipe → 워킹 버퍼. C3a — 모듈 미해결 시 null(구 algorithm_camera.json fallback 폐지).</summary>
        private AlgorithmCameraMapping LoadBuffer()
        {
            var mod = Module();
            if (mod == null) return null;
            var m = mod.ExportCameraMapping();
            m.Algorithm = _algorithm;
            return m;
        }

        private void BindFields()
        {
            var m = CurrentMapping();
            if (m == null)   // C3a — 운영 모듈 미해결: 명시 메시지 + 입력 비활성(조용한 구경로 금지)
            {
                if (_body != null) _body.Enabled = false;
                if (_lblStatus != null) { _lblStatus.ForeColor = Color.Firebrick; _lblStatus.Text = "설정 불러올 수 없음 — 운영 모듈 미해결"; }
                System.Diagnostics.Debug.WriteLine("[CameraMappingPanel] 모듈 미해결: " + _algorithm);
                return;
            }
            if (_body != null) _body.Enabled = true;
            _suspendBinding = true;
            try
            {
                SetSelectedById(_cbCameraId, m.CameraId);
                _numExposure.Value = Clamp((decimal)m.ExposureUs, _numExposure.Minimum, _numExposure.Maximum);
                _numGain    .Value = Clamp((decimal)m.Gain,       _numGain.Minimum,     _numGain.Maximum);
                _numFps     .Value = Clamp((decimal)m.FrameRate,  _numFps.Minimum,      _numFps.Maximum);
                _cbTrigger  .SelectedItem = m.TriggerMode ?? "Software"; if (_cbTrigger.SelectedIndex < 0) _cbTrigger.SelectedIndex = (int)CameraTriggerMode.Software;
                _cbPixel    .SelectedItem = m.PixelFormat ?? "Mono8";    if (_cbPixel.SelectedIndex   < 0) _cbPixel.SelectedIndex   = (int)CameraPixelFormat.Mono8;
                _numDelay   .Value = Clamp((decimal)m.DelayBeforeGrabMs, _numDelay.Minimum, _numDelay.Maximum);
                _numRoiX    .Value = Clamp(m.RoiOffsetX, _numRoiX.Minimum, _numRoiX.Maximum);
                _numRoiY    .Value = Clamp(m.RoiOffsetY, _numRoiY.Minimum, _numRoiY.Maximum);
                _numRoiW    .Value = Clamp(m.RoiWidth,   _numRoiW.Minimum, _numRoiW.Maximum);
                _numRoiH    .Value = Clamp(m.RoiHeight,  _numRoiH.Minimum, _numRoiH.Maximum);

                var cfg = VisionConfigStore.Current;
                if (_txtMilDcf != null) _txtMilDcf.Text = cfg?.MilDcfPath ?? "";
                // 저장된 DCF 가 있으면 체크박스를 자동 ON (그래야 경로칸이 보임)
                if (_chkMilDcf != null) _chkMilDcf.Checked = !string.IsNullOrEmpty(cfg?.MilDcfPath);
            }
            finally { _suspendBinding = false; }
            UpdateMilVisibility();
        }

        /// <summary>"Mil/..." 카메라가 선택됐을 때만 "DCF 직접 지정" 체크박스를 노출.</summary>
        private void UpdateMilVisibility()
        {
            string id = ItemToId(_cbCameraId?.SelectedItem) ?? _cbCameraId?.Text;
            bool isMil = !string.IsNullOrEmpty(id) && id.StartsWith("Mil/", StringComparison.OrdinalIgnoreCase);
            if (_chkMilDcf != null) _chkMilDcf.Visible = isMil;
            UpdateMilDcfVisibility();
        }

        /// <summary>DCF 경로칸은 MIL 카메라 선택 + "DCF 직접 지정" 체크 시에만 표시(기본 숨김).</summary>
        private void UpdateMilDcfVisibility()
        {
            bool show = _chkMilDcf != null && _chkMilDcf.Visible && _chkMilDcf.Checked;
            if (_lblMil    != null) _lblMil.Visible    = show;
            if (_txtMilDcf != null) _txtMilDcf.Visible = show;
        }

        /// <summary>MIL DCF 는 전역 VisionSettings 에 보관 (per-algorithm 아님). 체크 해제 시 비움 → enumerate 가 M_DEFAULT 사용.</summary>
        private void OnMilFieldChanged()
        {
            if (_suspendBinding) return;
            var cfg = VisionConfigStore.Current;
            if (cfg == null) return;
            bool useDcf = _chkMilDcf != null && _chkMilDcf.Checked;
            cfg.MilDcfPath = useDcf ? (_txtMilDcf?.Text ?? "") : "";
        }

        private static decimal Clamp(decimal v, decimal min, decimal max)
            => v < min ? min : (v > max ? max : v);

        private void OnFieldChanged()
        {
            if (_suspendBinding) return;
            var m = CurrentMapping();
            if (m == null) return;
            m.CameraId          = ItemToId(_cbCameraId.SelectedItem) ?? _cbCameraId.Text;
            m.ExposureUs        = (double)_numExposure.Value;
            m.Gain              = (double)_numGain.Value;
            m.FrameRate         = (double)_numFps.Value;
            m.TriggerMode       = (string)_cbTrigger.SelectedItem ?? "Software";
            m.PixelFormat       = (string)_cbPixel.SelectedItem   ?? "Mono8";
            m.DelayBeforeGrabMs = (int)_numDelay.Value;
            m.RoiOffsetX        = (int)_numRoiX.Value;
            m.RoiOffsetY        = (int)_numRoiY.Value;
            m.RoiWidth          = (int)_numRoiW.Value;
            m.RoiHeight         = (int)_numRoiH.Value;
            UpdateMilVisibility();
        }

        /// <summary>Validation — 빈 CameraId 거부, ROI 부분 입력 (W/H 한쪽만 0) 경고.</summary>
        private bool Validate(out string error)
        {
            error = null;
            var m = CurrentMapping();
            if (m == null) { error = "no mapping"; return false; }
            if (string.IsNullOrWhiteSpace(m.CameraId)) { error = "CameraId 비어 있음"; return false; }
            if (m.ExposureUs <= 0) { error = "Exposure 가 0 이하"; return false; }
            // ROI 부분 입력 경고 (둘 다 0 = full, 둘 다 양수 = OK, 그 외 = 모호)
            if ((m.RoiWidth > 0) != (m.RoiHeight > 0))
            {
                error = "ROI Width / Height 는 둘 다 0(full) 또는 둘 다 양수여야 함";
                return false;
            }
            return true;
        }

        private void DiscoverCameras()
        {
            try
            {
                _lblStatus.Text = "카메라 검색 중..."; _lblStatus.Refresh();
                var list = CameraFactory.EnumerateAll();
                var prevId = ItemToId(_cbCameraId.SelectedItem) ?? _cbCameraId.Text;
                _cbCameraId.Items.Clear();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var info in list)
                    if (!string.IsNullOrEmpty(info.Id) && seen.Add(info.Id))
                        _cbCameraId.Items.Add(new DeviceListItem(info));

                foreach (var fb in new[] { "Sim/Wafer", "Sim/Bin", "Sim/BottomInsp", "Sim/FrontSide", "Sim/RearSide", "Sim/0" })
                    if (seen.Add(fb)) _cbCameraId.Items.Add(fb);

                var m = CurrentMapping();
                var target = (m != null && !string.IsNullOrEmpty(m.CameraId)) ? m.CameraId : prevId;
                SetSelectedById(_cbCameraId, target);
                _lblStatus.Text = $"검색 완료 — {list.Count} 대 발견";
            }
            catch (Exception ex) { _lblStatus.Text = "검색 실패: " + ex.Message; }
        }

        private void SaveAll()
        {
            OnFieldChanged();
            if (!Validate(out var err)) { _lblStatus.Text = "저장 거부 — " + err; _lblStatus.ForeColor = Color.Firebrick; return; }
            var mod = Module();
            if (mod == null)   // C3a — 모듈 미해결: 저장 불가(구 store fallback 폐지)
            {
                _lblStatus.ForeColor = Color.Firebrick;
                _lblStatus.Text = "저장 불가 — 운영 모듈 미해결";
                return;
            }
            // 카메라 설정 SSOT = 모듈 Config/Recipe
            mod.ImportCameraMapping(_buffer);
            mod.SaveSettings();
            mod.SaveRecipe("default");
            OnMilFieldChanged();
            VisionConfigStore.Save();   // MIL DCF/System 등 전역 설정 영속
            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = $"저장 완료 — 모듈 [{mod.StorageKey}] Config/Recipe";
        }

        private void CancelChanges()
        {
            // 미저장 편집은 버퍼에만 존재 → 버퍼를 버리고 모듈 Config/Recipe 에서 재로드(라이브 카메라 무영향).
            _buffer = null;
            BindFields();
            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = "취소됨 — 저장된 값으로 되돌림";
        }

        private void ResetToDefaults()
        {
            if (string.IsNullOrEmpty(_algorithm)) return;
            var dialog = MessageBox.Show(
                $"[{VisionAlgorithm.Label(_algorithm)}] 항목을 기본값으로 되돌립니다.\n저장하지 않으면 디스크에는 반영되지 않습니다. 계속할까요?",
                "기본값 복원", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog != DialogResult.Yes) return;

            // 기본 매핑 산출(임시 subset) → 워킹 버퍼에만 반영. 저장 시 모듈 Config/Recipe 로 영속.
            var fresh = new AlgorithmCameraSubset();
            fresh.EnsureDefaults();
            var def = fresh.Get(_algorithm);
            _buffer = def ?? new AlgorithmCameraMapping { Algorithm = _algorithm };
            _buffer.Algorithm = _algorithm;
            BindFields();
            _lblStatus.ForeColor = Color.DarkSlateGray;
            _lblStatus.Text = "기본값 복원 — 저장 필요";
        }

        private void ApplyToRunningModule()
        {
            OnFieldChanged();
            if (!Validate(out var verr)) { _lblStatus.Text = "적용 거부 — " + verr; _lblStatus.ForeColor = Color.Firebrick; return; }
            var m = CurrentMapping();
            var form = this.FindForm() as Form1;
            if (form == null || m == null) { _lblStatus.Text = "메인 폼을 찾을 수 없음"; return; }
            // 모듈 Config/Recipe(SSOT)에 먼저 반영 후 라이브 카메라 Rebind(교체/파라미터 적용).
            Module()?.ImportCameraMapping(m);
            try
            {
                if (form.RebindAlgorithmCamera(_algorithm, m, out var rebindErr))
                {
                    _lblStatus.ForeColor = Color.DarkSlateGray;
                    _lblStatus.Text = $"[{VisionAlgorithm.Label(_algorithm)}] 실행 모듈에 적용됨";
                }
                else
                {
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = "적용 실패: " + rebindErr;
                }
            }
            catch (Exception ex) { _lblStatus.Text = "적용 실패: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
        }

        private void TestGrab()
        {
            OnFieldChanged();
            var m = CurrentMapping();
            if (m == null) return;
            if (_isLive)
            {
                _lblStatus.Text = "Live 중에는 단발 그랩 불가. Live Stop 후 시도하세요.";
                _lblStatus.ForeColor = Color.Firebrick;
                return;
            }
            _lblStatus.Text = "테스트 그랩 중..."; _lblStatus.Refresh();

            ICamera cam = _activeCam;
            bool ownCam = false;
            try
            {
                if (cam == null)
                {
                    cam = AlgorithmCameraBinder.CreateAndApply(m);
                    ownCam = true;
                }
                cam.TriggerMode = CameraTriggerMode.Software;
                using (var g = cam.Grab(3000))
                {
                    if (g.IsSuccess && g.Image != null)
                    {
                        _picPreview.Image?.Dispose();
                        _picPreview.Image = new Bitmap(g.Image);
                        _lblStatus.ForeColor = Color.DarkSlateGray;
                        _lblStatus.Text = $"그랩 OK — {g.Width}x{g.Height}  Exposure={m.ExposureUs}μs  Gain={m.Gain}dB";
                    }
                    else { _lblStatus.Text = "그랩 실패: " + (g.ErrorMessage ?? "-"); _lblStatus.ForeColor = Color.Firebrick; }
                }
            }
            catch (Exception ex) { _lblStatus.Text = "예외: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            finally { if (ownCam) { try { cam?.Dispose(); } catch { } } }
        }

        // ──────────────────────────────────────────
        //  Connect / Disconnect / Live
        // ──────────────────────────────────────────

        private void ToggleConnect()
        {
            if (_activeCam != null) Disconnect();
            else                    Connect();
            UpdateConnectButtons();
        }

        private void Connect()
        {
            OnFieldChanged();
            if (!Validate(out var err)) { _lblStatus.Text = "연결 거부 — " + err; _lblStatus.ForeColor = Color.Firebrick; return; }
            var m = CurrentMapping();
            try
            {
                // 운영 모듈(Form1)이 같은 카메라 보유 중이면 borrow (exclusive 점유 충돌 회피).
                var form = FindForm() as Form1;
                var mod  = form?.ResolveModule(_algorithm);
                if (mod != null)
                {
                    bool sameId = string.Equals(mod.Camera?.Info?.Id, m.CameraId, StringComparison.OrdinalIgnoreCase);
                    if (!sameId || mod.Camera == null || !mod.Camera.IsOpen)
                    {
                        if (!form.RebindAlgorithmCamera(_algorithm, m, out var rebindErr))
                        {
                            _lblStatus.ForeColor = Color.Firebrick;
                            _lblStatus.Text = "운영 모듈 Rebind 실패: " + rebindErr;
                            return;
                        }
                    }
                    var borrowed = mod.Camera;
                    if (borrowed == null || !borrowed.IsOpen)
                    {
                        _lblStatus.ForeColor = Color.Firebrick;
                        _lblStatus.Text = "운영 모듈 카메라가 열려있지 않음 (CameraId=" + m.CameraId + ")";
                        return;
                    }
                    borrowed.FrameReceived     += Cam_FrameReceived;
                    borrowed.ConnectionChanged += Cam_ConnectionChanged;
                    _activeCam      = borrowed;
                    _activeCamOwned = false;
                    _lblStatus.ForeColor = Color.DarkSlateGray;
                    _lblStatus.Text = $"Connected (운영 모듈 공유) — {m.CameraId}";
                    return;
                }

                // Fallback: 단독 모드 → 새 인스턴스 직접 open.
                var cam = AlgorithmCameraBinder.CreateAndApply(m, out var openErr, out var applyErr);
                if (cam == null || !cam.IsOpen)
                {
                    _lblStatus.ForeColor = Color.Firebrick;
                    _lblStatus.Text = "Open 실패: " + (openErr ?? "(unknown)");
                    try { cam?.Dispose(); } catch { }
                    return;
                }
                cam.FrameReceived     += Cam_FrameReceived;
                cam.ConnectionChanged += Cam_ConnectionChanged;
                _activeCam      = cam;
                _activeCamOwned = true;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = string.IsNullOrEmpty(applyErr)
                    ? $"Connected — {m.CameraId}"
                    : $"Connected (Apply warn: {applyErr})";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Connect error: " + ex.Message;
                _lblStatus.ForeColor = Color.Firebrick;
                if (_activeCamOwned) { try { _activeCam?.Dispose(); } catch { } }
                _activeCam = null;
                _activeCamOwned = false;
            }
        }

        private void Disconnect()
        {
            if (_activeCam == null) return;
            try { if (_isLive) _activeCam.StopLive(); } catch { }
            _isLive = false;
            try { _activeCam.FrameReceived     -= Cam_FrameReceived; } catch { }
            try { _activeCam.ConnectionChanged -= Cam_ConnectionChanged; } catch { }
            if (_activeCamOwned)
            {
                try { _activeCam.Close();   } catch { }
                try { _activeCam.Dispose(); } catch { }
            }
            _activeCam = null;
            _activeCamOwned = false;
            if (_lblStatus != null) { _lblStatus.ForeColor = Color.DarkSlateGray; _lblStatus.Text = "Disconnected"; }
        }

        private void LiveStart()
        {
            if (_activeCam == null || _isLive) return;
            try
            {
                _fpsT0 = DateTime.Now; _fpsCount = 0;
                _activeCam.TriggerMode = CameraTriggerMode.Continuous;
                _activeCam.StartLive();
                _isLive = true;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = "Live started";
            }
            catch (Exception ex) { _lblStatus.Text = "Live start error: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            UpdateConnectButtons();
        }

        private void LiveStop()
        {
            if (_activeCam == null || !_isLive) return;
            try
            {
                _activeCam.StopLive();
                _isLive = false;
                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = "Live stopped";
            }
            catch (Exception ex) { _lblStatus.Text = "Live stop error: " + ex.Message; _lblStatus.ForeColor = Color.Firebrick; }
            UpdateConnectButtons();
        }

        private void Cam_FrameReceived(GrabResult r)
        {
            if (r == null || !r.IsSuccess || r.Image == null) return;
            var bmp = (Bitmap)r.Image.Clone();
            _uiCtx.Post(_ => ShowLiveFrame(bmp), null);
        }

        private void ShowLiveFrame(Bitmap bmp)
        {
            if (_picPreview == null) { bmp.Dispose(); return; }
            var old = _picPreview.Image;
            _picPreview.Image = bmp;
            old?.Dispose();
            _fpsCount++;
            var dt = (DateTime.Now - _fpsT0).TotalSeconds;
            if (dt >= 1.0)
            {
                _lblStatus.Text = $"Live  {_fpsCount / dt:F1} FPS  ({bmp.Width}x{bmp.Height})";
                _fpsCount = 0; _fpsT0 = DateTime.Now;
            }
        }

        private void Cam_ConnectionChanged(CameraConnectionEvent ev)
        {
            _uiCtx.Post(_ =>
            {
                if (_lblStatus != null) _lblStatus.Text = "[evt] " + ev;
                UpdateConnectButtons();
            }, null);
        }

        private void UpdateConnectButtons()
        {
            if (_btnConnect == null) return;
            bool connected = _activeCam != null;
            bool live = _isLive;
            _btnConnect.Text = connected ? "Disconnect" : "Connect";
            _btnConnect.BackColor = connected ? Color.IndianRed : UiTheme.Accent;
            _btnLiveStart.Enabled = connected && !live;
            _btnLiveStop.Enabled  = connected && live;
            _btnTestGrab.Enabled  = !live;
            // 연결 중엔 카메라/매핑 변경 잠금
            _cbCameraId.Enabled  = !connected;
            if (_btnDiscover != null) _btnDiscover.Enabled = !connected;
            _btnApply.Enabled    = !connected;
        }

        // ──────────────────────────────────────────
        //  ComboBox 아이템 wrapper / helpers
        // ──────────────────────────────────────────

        /// <summary>"UserDefinedName [Model] IP" 표시용 wrapper. 매핑 저장값은 Info.Id (IP) 그대로.</summary>
        private class DeviceListItem
        {
            public CameraInfo Info { get; }
            public DeviceListItem(CameraInfo info) { Info = info; }
            public string Id => Info?.Id;
            public override string ToString()
            {
                if (Info == null) return "";
                if (Info.Transport == CameraTransport.Sim) return Info.Id;
                var uid = string.IsNullOrWhiteSpace(Info.UserDefinedName) ? "(no UserID)" : Info.UserDefinedName;
                return $"{uid}   [{Info.Model}]   {Info.IpAddress}";
            }
        }

        private static string ItemToId(object item)
        {
            if (item is DeviceListItem d) return d.Id;
            return item as string;
        }

        private static bool ItemMatches(object item, string id)
        {
            var s = ItemToId(item);
            return s != null && s.Equals(id, StringComparison.OrdinalIgnoreCase);
        }

        private static void SetSelectedById(ComboBox cb, string id)
        {
            if (string.IsNullOrEmpty(id)) { cb.SelectedIndex = -1; cb.Text = ""; return; }
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (ItemMatches(cb.Items[i], id)) { cb.SelectedIndex = i; cb.Text = cb.Items[i].ToString(); return; }
            }
            cb.Items.Add(id);
            cb.SelectedIndex = cb.Items.Count - 1;
            cb.Text = id;
        }
    }
}
