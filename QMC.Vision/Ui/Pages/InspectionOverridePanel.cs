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
    /// Stage 94 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 콤보 채움·바인딩·저장 로직은 Code.
    /// </summary>
    public partial class InspectionOverridePanel : UserControl
    {
        private string _algorithm, _inspectionId;

        /// <summary>override 저장/변경 후 SettingsPage 가 TreeView 노드 표시를 갱신하도록.</summary>
        public event Action<string, string> OverrideChanged;

        public InspectionOverridePanel()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildCombos();
        }

        /// <summary>콤보 Items(enum) 동적 채움 — 런타임.</summary>
        private void BuildCombos()
        {
            foreach (var v in Enum.GetNames(typeof(CameraTriggerMode))) _cbTrigger.Items.Add(v);
            foreach (var v in Enum.GetNames(typeof(CameraPixelFormat))) _cbPixel.Items.Add(v);
        }

        public void SelectInspection(string algorithm, string inspectionId)
        {
            _algorithm = algorithm;
            _inspectionId = inspectionId;
            _lblHeader.Text = "검사 카메라 오버라이드 — " + InspectionLabel.Get(algorithm, inspectionId)
                            + "  (" + VisionAlgorithm.Label(algorithm) + " / " + inspectionId + ")";
            BindFields();
        }

        // ── 체크박스 토글 핸들러 (Designer 에서 named 연결) ──
        private void OnCkExposure(object sender, EventArgs e) => _numExposure.Enabled = !_ckExposure.Checked;
        private void OnCkGain(object sender, EventArgs e)     => _numGain.Enabled     = !_ckGain.Checked;
        private void OnCkFps(object sender, EventArgs e)      => _numFps.Enabled      = !_ckFps.Checked;
        private void OnCkTrigger(object sender, EventArgs e)  => _cbTrigger.Enabled   = !_ckTrigger.Checked;
        private void OnCkPixel(object sender, EventArgs e)    => _cbPixel.Enabled     = !_ckPixel.Checked;
        private void OnCkDelay(object sender, EventArgs e)    => _numDelay.Enabled    = !_ckDelay.Checked;
        private void OnCkRoi(object sender, EventArgs e)
        { bool en = !_ckRoi.Checked; _numRoiX.Enabled = _numRoiY.Enabled = _numRoiW.Enabled = _numRoiH.Enabled = en; }

        // ── 버튼 핸들러 ──
        private void OnSaveClick(object sender, EventArgs e)   => Save();
        private void OnResetClick(object sender, EventArgs e)  => ResetAll();
        private void OnCancelClick(object sender, EventArgs e) => Cancel();
        private void OnTestClick(object sender, EventArgs e)   => TestGrab();

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
