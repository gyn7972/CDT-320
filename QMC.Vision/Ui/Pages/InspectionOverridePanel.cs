using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Stage 64 — 검사 1개의 카메라 파라미터 오버라이드 편집 패널.
    /// 필드별 "기본값" 체크박스 + 입력칸. 체크 = 알고리즘 기본값 상속(비활성), 해제 = override 입력(활성).
    /// ROI 는 4 필드 묶음(단일 체크박스). 빈 override 는 저장 시 제거됨.
    /// </summary>
    public class InspectionOverridePanel : UserControl
    {
        private Label    _lblHeader;
        private Label    _lblCamera;

        private CheckBox _ckExposure, _ckGain, _ckFps, _ckTrigger, _ckPixel, _ckDelay, _ckRoi;
        private NumericUpDown _numExposure, _numGain, _numFps, _numDelay;
        private ComboBox _cbTrigger, _cbPixel;
        private NumericUpDown _numRoiX, _numRoiY, _numRoiW, _numRoiH;

        private Button _btnSave, _btnReset, _btnCancel, _btnTest;
        private Label  _lblStatus;

        private string _algorithm, _inspectionId;

        /// <summary>override 저장/변경 후 SettingsPage 가 TreeView 노드 표시를 갱신하도록.</summary>
        public event Action<string, string> OverrideChanged;

        public InspectionOverridePanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

        public void SelectInspection(string algorithm, string inspectionId)
        {
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            _lblHeader.Text = "검사 카메라 오버라이드 — " + InspectionLabel.Get(algorithm, inspectionId)
                            + "  (" + VisionAlgorithm.Label(algorithm) + " / " + inspectionId + ")";
            BindFields();
        }

        private AlgorithmCameraMapping BaseMapping()
            => AlgorithmCameraMapStore.Current?.Get(_algorithm);

        private InspectionCameraOverride CurrentOverride(bool create)
        {
            var bm = BaseMapping();
            if (bm == null) return null;
            if (create) return bm.GetOrCreateOverride(_inspectionId);
            if (bm.Inspections == null) return null;
            foreach (var o in bm.Inspections)
                if (string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase)) return o;
            return null;
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            _lblHeader = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "검사 카메라 오버라이드",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblHeader);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg, AutoScroll = true, Padding = new Padding(10) };
            Controls.Add(body);

            int xCk = 20, xLbl = 150, xVal = 320, y = 10, dy = 34;

            _lblCamera = new Label { Location = new Point(xCk, y), Size = new Size(560, 22), Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray, Text = "카메라: -" };
            body.Controls.Add(_lblCamera);
            y += dy;

            body.Controls.Add(new Label { Location = new Point(xCk, y - 24), Size = new Size(560, 20), Text = "─ 필드별 토글 (체크 = 알고리즘 기본값 상속) ─", Font = UiTheme.ButtonFont, ForeColor = Color.Gray });

            _ckExposure = MakeCheck(xCk, y); body.Controls.Add(_ckExposure);
            body.Controls.Add(L("Exposure (μs)", xLbl, y));
            _numExposure = MakeNum(xVal, y, 1, 1_000_000, 0, 100); body.Controls.Add(_numExposure);
            y += dy;

            _ckGain = MakeCheck(xCk, y); body.Controls.Add(_ckGain);
            body.Controls.Add(L("Gain (dB)", xLbl, y));
            _numGain = MakeNum(xVal, y, 0, 48, 1, 0.5M); body.Controls.Add(_numGain);
            y += dy;

            _ckFps = MakeCheck(xCk, y); body.Controls.Add(_ckFps);
            body.Controls.Add(L("Frame rate (fps)", xLbl, y));
            _numFps = MakeNum(xVal, y, 1, 1000, 0, 1); body.Controls.Add(_numFps);
            y += dy;

            _ckTrigger = MakeCheck(xCk, y); body.Controls.Add(_ckTrigger);
            body.Controls.Add(L("Trigger mode", xLbl, y));
            _cbTrigger = new ComboBox { Location = new Point(xVal, y - 2), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            foreach (var v in Enum.GetNames(typeof(CameraTriggerMode))) _cbTrigger.Items.Add(v);
            body.Controls.Add(_cbTrigger);
            y += dy;

            _ckPixel = MakeCheck(xCk, y); body.Controls.Add(_ckPixel);
            body.Controls.Add(L("Pixel format", xLbl, y));
            _cbPixel = new ComboBox { Location = new Point(xVal, y - 2), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            foreach (var v in Enum.GetNames(typeof(CameraPixelFormat))) _cbPixel.Items.Add(v);
            body.Controls.Add(_cbPixel);
            y += dy;

            _ckDelay = MakeCheck(xCk, y); body.Controls.Add(_ckDelay);
            body.Controls.Add(L("Delay before grab (ms)", xLbl, y));
            _numDelay = MakeNum(xVal, y, 0, 60_000, 0, 10); body.Controls.Add(_numDelay);
            y += dy;

            // ROI 묶음 (단일 체크박스)
            _ckRoi = MakeCheck(xCk, y); body.Controls.Add(_ckRoi);
            body.Controls.Add(L("ROI (X/Y/W/H 묶음)", xLbl, y));
            _numRoiX = MakeNum(xVal,       y, 0, 8000, 0, 1); _numRoiX.Width = 70; body.Controls.Add(_numRoiX);
            _numRoiY = MakeNum(xVal + 78,  y, 0, 8000, 0, 1); _numRoiY.Width = 70; body.Controls.Add(_numRoiY);
            _numRoiW = MakeNum(xVal + 156, y, 0, 8000, 0, 1); _numRoiW.Width = 70; body.Controls.Add(_numRoiW);
            _numRoiH = MakeNum(xVal + 234, y, 0, 8000, 0, 1); _numRoiH.Width = 70; body.Controls.Add(_numRoiH);
            y += dy + 12;

            _btnSave   = MakeBtn("저장",        xCk,        y, 110, UiTheme.Accent, Color.White); _btnSave.Click   += (s, e) => Save();      body.Controls.Add(_btnSave);
            _btnReset  = MakeBtn("기본값 복원",  xCk + 120,  y, 120, Color.White, Color.Black);   _btnReset.Click  += (s, e) => ResetAll();  body.Controls.Add(_btnReset);
            _btnCancel = MakeBtn("취소",        xCk + 250,  y, 90,  Color.White, Color.Black);    _btnCancel.Click += (s, e) => Cancel();    body.Controls.Add(_btnCancel);
            _btnTest   = MakeBtn("테스트 그랩",  xCk + 350,  y, 120, Color.White, Color.Black);   _btnTest.Click   += (s, e) => TestGrab();  body.Controls.Add(_btnTest);
            y += 44;

            _lblStatus = new Label { Location = new Point(xCk, y), Size = new Size(640, 24), Text = "", Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray };
            body.Controls.Add(_lblStatus);

            // 체크박스 → 입력칸 활성/비활성 토글
            _ckExposure.CheckedChanged += (s, e) => _numExposure.Enabled = !_ckExposure.Checked;
            _ckGain    .CheckedChanged += (s, e) => _numGain.Enabled     = !_ckGain.Checked;
            _ckFps     .CheckedChanged += (s, e) => _numFps.Enabled      = !_ckFps.Checked;
            _ckTrigger .CheckedChanged += (s, e) => _cbTrigger.Enabled   = !_ckTrigger.Checked;
            _ckPixel   .CheckedChanged += (s, e) => _cbPixel.Enabled     = !_ckPixel.Checked;
            _ckDelay   .CheckedChanged += (s, e) => _numDelay.Enabled    = !_ckDelay.Checked;
            _ckRoi     .CheckedChanged += (s, e) => { bool en = !_ckRoi.Checked; _numRoiX.Enabled = _numRoiY.Enabled = _numRoiW.Enabled = _numRoiH.Enabled = en; };
        }

        private static CheckBox MakeCheck(int x, int y)
            => new CheckBox { Location = new Point(x, y), Size = new Size(120, 24), Text = "기본값", Checked = true, Font = UiTheme.ButtonFont };
        private static Label L(string t, int x, int y)
            => new Label { Location = new Point(x, y + 2), Size = new Size(160, 22), Text = t, Font = UiTheme.ButtonFont };
        private static NumericUpDown MakeNum(int x, int y, decimal min, decimal max, int dp, decimal inc)
            => new NumericUpDown { Location = new Point(x, y - 2), Size = new Size(150, 26), Minimum = min, Maximum = max, DecimalPlaces = dp, Increment = inc, Font = UiTheme.ValueFont };
        private static Button MakeBtn(string t, int x, int y, int w, Color bg, Color fg)
            => new Button { Location = new Point(x, y), Size = new Size(w, 32), Text = t, FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = bg, ForeColor = fg };

        private static decimal Clamp(decimal v, decimal min, decimal max) => v < min ? min : (v > max ? max : v);

        private void BindFields()
        {
            var bm = BaseMapping();
            if (bm == null) { _lblStatus.Text = "알고리즘 매핑 없음"; return; }
            var ov = CurrentOverride(false);
            {
                _lblCamera.Text = "카메라: " + bm.CameraId + "  (알고리즘에서 상속)";

                // Exposure
                _ckExposure.Checked = ov?.ExposureUs == null;
                _numExposure.Value  = Clamp((decimal)(ov?.ExposureUs ?? bm.ExposureUs), _numExposure.Minimum, _numExposure.Maximum);
                _numExposure.Enabled = !_ckExposure.Checked;
                // Gain
                _ckGain.Checked = ov?.Gain == null;
                _numGain.Value  = Clamp((decimal)(ov?.Gain ?? bm.Gain), _numGain.Minimum, _numGain.Maximum);
                _numGain.Enabled = !_ckGain.Checked;
                // Fps
                _ckFps.Checked = ov?.FrameRate == null;
                _numFps.Value  = Clamp((decimal)(ov?.FrameRate ?? bm.FrameRate), _numFps.Minimum, _numFps.Maximum);
                _numFps.Enabled = !_ckFps.Checked;
                // Trigger
                _ckTrigger.Checked = string.IsNullOrEmpty(ov?.TriggerMode);
                _cbTrigger.SelectedItem = string.IsNullOrEmpty(ov?.TriggerMode) ? (bm.TriggerMode ?? "Software") : ov.TriggerMode;
                if (_cbTrigger.SelectedIndex < 0) _cbTrigger.SelectedIndex = 0;
                _cbTrigger.Enabled = !_ckTrigger.Checked;
                // Pixel
                _ckPixel.Checked = string.IsNullOrEmpty(ov?.PixelFormat);
                _cbPixel.SelectedItem = string.IsNullOrEmpty(ov?.PixelFormat) ? (bm.PixelFormat ?? "Mono8") : ov.PixelFormat;
                if (_cbPixel.SelectedIndex < 0) _cbPixel.SelectedIndex = 0;
                _cbPixel.Enabled = !_ckPixel.Checked;
                // Delay
                _ckDelay.Checked = ov?.DelayBeforeGrabMs == null;
                _numDelay.Value  = Clamp(ov?.DelayBeforeGrabMs ?? bm.DelayBeforeGrabMs, _numDelay.Minimum, _numDelay.Maximum);
                _numDelay.Enabled = !_ckDelay.Checked;
                // ROI (묶음)
                bool roiOv = ov != null && ov.HasRoiOverride;
                _ckRoi.Checked = !roiOv;
                _numRoiX.Value = Clamp(roiOv ? ov.RoiOffsetX.Value : bm.RoiOffsetX, _numRoiX.Minimum, _numRoiX.Maximum);
                _numRoiY.Value = Clamp(roiOv ? ov.RoiOffsetY.Value : bm.RoiOffsetY, _numRoiY.Minimum, _numRoiY.Maximum);
                _numRoiW.Value = Clamp(roiOv ? ov.RoiWidth.Value   : bm.RoiWidth,   _numRoiW.Minimum, _numRoiW.Maximum);
                _numRoiH.Value = Clamp(roiOv ? ov.RoiHeight.Value  : bm.RoiHeight,  _numRoiH.Minimum, _numRoiH.Maximum);
                _numRoiX.Enabled = _numRoiY.Enabled = _numRoiW.Enabled = _numRoiH.Enabled = roiOv;

                _lblStatus.ForeColor = Color.DarkSlateGray;
                _lblStatus.Text = (ov == null || ov.IsEmpty()) ? "전부 알고리즘 기본값 상속" : "override 있음";
            }
        }

        /// <summary>UI → override 객체 반영. 체크된 필드는 null(상속), 해제된 필드는 입력값.</summary>
        private bool Validate(out string err)
        {
            err = null;
            // ROI override 인데 W·H 가 0 이면 무효 (묶음이라 4필드 다 있지만 W/H 양수 요구)
            if (!_ckRoi.Checked && (_numRoiW.Value <= 0 || _numRoiH.Value <= 0))
            {
                err = "ROI override 시 Width/Height 는 양수여야 함";
                return false;
            }
            if (!_ckExposure.Checked && _numExposure.Value <= 0) { err = "Exposure 는 0 보다 커야 함"; return false; }
            return true;
        }

        private void Save()
        {
            var bm = BaseMapping();
            if (bm == null) { SetStatus("알고리즘 매핑 없음", true); return; }
            if (!Validate(out var verr)) { SetStatus("저장 거부 — " + verr, true); return; }

            var ov = bm.GetOrCreateOverride(_inspectionId);
            ov.ExposureUs        = _ckExposure.Checked ? (double?)null : (double)_numExposure.Value;
            ov.Gain              = _ckGain.Checked     ? (double?)null : (double)_numGain.Value;
            ov.FrameRate         = _ckFps.Checked      ? (double?)null : (double)_numFps.Value;
            ov.TriggerMode       = _ckTrigger.Checked  ? null          : (string)_cbTrigger.SelectedItem;
            ov.PixelFormat       = _ckPixel.Checked    ? null          : (string)_cbPixel.SelectedItem;
            ov.DelayBeforeGrabMs = _ckDelay.Checked    ? (int?)null    : (int)_numDelay.Value;
            if (_ckRoi.Checked)
            {
                ov.RoiOffsetX = ov.RoiOffsetY = ov.RoiWidth = ov.RoiHeight = null;
            }
            else
            {
                ov.RoiOffsetX = (int)_numRoiX.Value; ov.RoiOffsetY = (int)_numRoiY.Value;
                ov.RoiWidth   = (int)_numRoiW.Value; ov.RoiHeight  = (int)_numRoiH.Value;
            }

            // 빈 override 는 리스트에서 제거 (Store.Save 도 정리하지만 즉시 일관성)
            if (ov.IsEmpty()) bm.Inspections?.RemoveAll(o => string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase));

            AlgorithmCameraMapStore.Save();
            OverrideChanged?.Invoke(_algorithm, _inspectionId);
            SetStatus("저장 완료 — " + AlgorithmCameraMapStore.Path_, false);
        }

        private void ResetAll()
        {
            var bm = BaseMapping();
            if (bm == null) return;
            var dlg = MessageBox.Show(
                "이 검사의 override 를 모두 지우고 알고리즘 기본값 상속으로 되돌립니다. 계속할까요?",
                "기본값 복원", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dlg != DialogResult.Yes) return;
            bm.Inspections?.RemoveAll(o => string.Equals(o.InspectionId, _inspectionId, StringComparison.OrdinalIgnoreCase));
            AlgorithmCameraMapStore.Save();
            BindFields();
            OverrideChanged?.Invoke(_algorithm, _inspectionId);
            SetStatus("기본값 복원 완료 (상속만)", false);
        }

        private void Cancel()
        {
            AlgorithmCameraMapStore.Load();
            BindFields();
            OverrideChanged?.Invoke(_algorithm, _inspectionId);
            SetStatus("취소됨 — 디스크 값으로 되돌림", false);
        }

        private void TestGrab()
        {
            var bm = BaseMapping();
            if (bm == null) { SetStatus("알고리즘 매핑 없음", true); return; }
            if (!Validate(out var verr)) { SetStatus("테스트 거부 — " + verr, true); return; }

            // 미저장 UI 상태를 반영하기 위해 임시 override 적용본 산출 (저장은 하지 않음)
            var tempOv = new InspectionCameraOverride { InspectionId = _inspectionId };
            if (!_ckExposure.Checked) tempOv.ExposureUs = (double)_numExposure.Value;
            if (!_ckGain.Checked)     tempOv.Gain = (double)_numGain.Value;
            if (!_ckFps.Checked)      tempOv.FrameRate = (double)_numFps.Value;
            if (!_ckTrigger.Checked)  tempOv.TriggerMode = (string)_cbTrigger.SelectedItem;
            if (!_ckPixel.Checked)    tempOv.PixelFormat = (string)_cbPixel.SelectedItem;
            if (!_ckDelay.Checked)    tempOv.DelayBeforeGrabMs = (int)_numDelay.Value;
            if (!_ckRoi.Checked)
            {
                tempOv.RoiOffsetX = (int)_numRoiX.Value; tempOv.RoiOffsetY = (int)_numRoiY.Value;
                tempOv.RoiWidth   = (int)_numRoiW.Value; tempOv.RoiHeight  = (int)_numRoiH.Value;
            }
            var eff = tempOv.IsEmpty() ? bm.Clone() : tempOv.ApplyOver(bm);

            ICamera cam = null;
            try
            {
                cam = AlgorithmCameraBinder.CreateAndApply(eff, out var openErr, out var applyErr);
                using (var g = cam.Grab(3000))
                {
                    if (g.IsSuccess && g.Image != null)
                        SetStatus($"그랩 OK — {g.Width}x{g.Height}  Exposure={eff.ExposureUs}μs Gain={eff.Gain}dB", false);
                    else
                        SetStatus("그랩 실패: " + (g.ErrorMessage ?? openErr ?? applyErr ?? "-"), true);
                }
            }
            catch (Exception ex) { SetStatus("예외: " + ex.Message, true); }
            finally { try { cam?.Dispose(); } catch { } }
        }

        private void SetStatus(string msg, bool error)
        {
            _lblStatus.ForeColor = error ? Color.Firebrick : Color.DarkSlateGray;
            _lblStatus.Text = msg;
        }
    }
}
