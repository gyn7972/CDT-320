using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class CalibrationPage : PageBase
    {
        private VisionCameraCalibrationDialog _visionCameraDialog;
        private ColletCalibrationDialog _colletDialog;
        private NeedlePinCalibrationDialog _needleDialog;
        private SideVisionFocusCalibrationDialog _sideVisionFocusDialog;
        private VisionFocusCalibrationDialog _visionFocusDialog;
        private ColletCalibrationDialog _colletRotationCenterDialog;
        private PickUpZCalibrationDialog _pickUpZDialog;
        private PlaceZCalibrationDialog _placeZDialog;
        private NeedleCalibrationDialog _needleZDialog;
        private AutoCalibrationDialog _autoCalibrationDialog;
        private ColletCleaningControlDialog _colletCleaningDialog;

        private bool _loadingSafeMovePercent;

        public CalibrationPage()
        {
            try
            {
                InitializeComponent();
                // 색/폰트(페이지·헤더·상태 라벨)는 Designer(.Designer.cs)로 이관
                // 버튼 Click 이벤트 연결도 Designer(InitializeComponent)로 이관
                LoadSafeMovePercentToUi();
                lblStatus.Text = "캘리브레이션 항목을 선택하세요. 각 기능은 모달리스 창으로 열립니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-PAGE-INIT", "CalibrationPage 초기화 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "Calibration 화면 초기화 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            // 페이지 생성 시점에는 host(Form1)가 아직 붙지 않아 값을 못 읽을 수 있으므로, 화면 표시될 때 다시 로드한다.
            if (Visible)
                LoadSafeMovePercentToUi();
        }

        // 안전이동 % 값을 장비 설정(CalibrationData.SafeMovePercent)에서 읽어 UI에 반영한다.
        private void LoadSafeMovePercentToUi()
        {
            try
            {
                Form1 host = ResolveHostForm();
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null ||
                    host.Machine.VisionUnit.Config == null || host.Machine.VisionUnit.Config.CalibrationData == null)
                    return;

                double percent = host.Machine.VisionUnit.Config.CalibrationData.SafeMovePercent;
                if (percent < (double)numSafeMovePercent.Minimum) percent = (double)numSafeMovePercent.Minimum;
                if (percent > (double)numSafeMovePercent.Maximum) percent = (double)numSafeMovePercent.Maximum;

                _loadingSafeMovePercent = true;
                try
                {
                    numSafeMovePercent.Value = (decimal)percent;
                }
                finally
                {
                    _loadingSafeMovePercent = false;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "CAL-SAFEMOVE-LOAD", "안전이동 % 로드 실패: " + ex.Message);
            }
        }

        private void numSafeMovePercent_ValueChanged(object sender, EventArgs e)
        {
            if (_loadingSafeMovePercent)
                return;

            try
            {
                Form1 host = ResolveHostForm();
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null ||
                    host.Machine.VisionUnit.Config == null || host.Machine.VisionUnit.Config.CalibrationData == null)
                {
                    lblStatus.Text = "장비가 준비되지 않아 안전이동 %를 저장할 수 없습니다.";
                    return;
                }

                double percent = (double)numSafeMovePercent.Value;
                host.Machine.VisionUnit.Config.CalibrationData.SafeMovePercent = percent;
                host.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                host.SaveMachineSettings();

                lblStatus.Text = "안전위치(Avoid) 이동 속도 %를 " + percent.ToString("F1") +
                                 "%로 저장했습니다. (각 축 Default × %) 측정 속도와는 무관합니다.";
                QMC.Common.Log.Write("Calibration", "SYSTEM", "CalSafeMovePercent",
                    "캘리브레이션 안전이동 SafeMovePercent 저장. percent=" + percent.ToString("F3"));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-SAFEMOVE-SAVE", "안전이동 % 저장 실패: " + ex.Message);
                lblStatus.Text = "안전이동 % 저장 실패: " + ex.Message;
            }
        }

        private Form1 ResolveHostForm()
        {
            return FindForm() as Form1;
        }

        private void btnVisionCameraCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_visionCameraDialog == null || _visionCameraDialog.IsDisposed)
                {
                    _visionCameraDialog = new VisionCameraCalibrationDialog();
                    _visionCameraDialog.Owner = host;
                    _visionCameraDialog.StartPosition = FormStartPosition.Manual;
                    _visionCameraDialog.Location = ResolveDialogLocation(_visionCameraDialog);
                    _visionCameraDialog.Show(host);
                    lblStatus.Text = "VISION CAMERA CAL 설정창을 열었습니다.";
                    return;
                }

                if (!_visionCameraDialog.Visible)
                    _visionCameraDialog.Show(host);

                _visionCameraDialog.Activate();
                _visionCameraDialog.BringToFront();
                lblStatus.Text = "VISION CAMERA CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-OPEN", "VISION CAMERA CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "VISION CAMERA CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnColletCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_colletDialog == null || _colletDialog.IsDisposed)
                {
                    _colletDialog = ColletCalibrationDialog.Open(host);
                    ActivateDialog(host, _colletDialog);
                    lblStatus.Text = "COLLET CAL 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _colletDialog);
                lblStatus.Text = "COLLET CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CAL-OPEN", "COLLET CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "COLLET CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }
        private void btnNeedleCal_Click(object sender, EventArgs e)
        {
            ShowDialogOnce(
                ref _needleDialog,
                "NEEDLE CAL",
                "터치 센서 기준으로 Needle Cap Touch 위치, Needle Pin Flush 위치, Needle Pin Ready 위치를 찾는 캘리브레이션입니다.",
                "저장 제안: Needle Cap/Pin 기준 높이는 장비 기준값이므로 Config에 저장합니다.");
        }

        private void btnSideVisionFocusCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_sideVisionFocusDialog == null || _sideVisionFocusDialog.IsDisposed)
                {
                    _sideVisionFocusDialog = SideVisionFocusCalibrationDialog.Open(host);
                    _sideVisionFocusDialog.StartPosition = FormStartPosition.Manual;
                    _sideVisionFocusDialog.Location = ResolveDialogLocation(_sideVisionFocusDialog);
                    lblStatus.Text = "SIDE VISION FOCUS CAL 설정창을 열었습니다.";
                    return;
                }

                if (!_sideVisionFocusDialog.Visible)
                    _sideVisionFocusDialog.Show(host);

                _sideVisionFocusDialog.Activate();
                _sideVisionFocusDialog.BringToFront();
                lblStatus.Text = "SIDE VISION FOCUS CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "SIDE-VISION-FOCUS-CAL-OPEN", "SIDE VISION FOCUS CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "SIDE VISION FOCUS CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnVisionFocusCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_visionFocusDialog == null || _visionFocusDialog.IsDisposed)
                {
                    _visionFocusDialog = VisionFocusCalibrationDialog.Open(host);
                    _visionFocusDialog.StartPosition = FormStartPosition.Manual;
                    _visionFocusDialog.Location = ResolveDialogLocation(_visionFocusDialog);
                    lblStatus.Text = "VISION FOCUS CAL 설정창을 열었습니다.";
                    return;
                }

                if (!_visionFocusDialog.Visible)
                    _visionFocusDialog.Show(host);

                _visionFocusDialog.Activate();
                _visionFocusDialog.BringToFront();
                lblStatus.Text = "VISION FOCUS CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-FOCUS-CAL-OPEN", "VISION FOCUS CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "VISION FOCUS CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnColletRotationCenterCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_colletRotationCenterDialog == null || _colletRotationCenterDialog.IsDisposed)
                    _colletRotationCenterDialog = ColletCalibrationDialog.Open(host);

                ActivateDialog(host, _colletRotationCenterDialog);
                lblStatus.Text = "COLLET CAL 화면에서 COC START를 실행하세요.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-COC-OPEN", "COLLET COC 화면 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "COLLET COC 화면 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnPickUpZCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_pickUpZDialog == null || _pickUpZDialog.IsDisposed)
                {
                    _pickUpZDialog = PickUpZCalibrationDialog.Open(host);
                    _pickUpZDialog.StartPosition = FormStartPosition.Manual;
                    _pickUpZDialog.Location = ResolveDialogLocation(_pickUpZDialog);
                    lblStatus.Text = "PICKUP Z CAL 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _pickUpZDialog);
                lblStatus.Text = "PICKUP Z CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PICKUP-Z-CAL-OPEN", "PICKUP Z CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "PICKUP Z CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnPlaceZCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_placeZDialog == null || _placeZDialog.IsDisposed)
                {
                    _placeZDialog = PlaceZCalibrationDialog.Open(host);
                    _placeZDialog.StartPosition = FormStartPosition.Manual;
                    _placeZDialog.Location = ResolveDialogLocation(_placeZDialog);
                    lblStatus.Text = "PLACE Z CAL 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _placeZDialog);
                lblStatus.Text = "PLACE Z CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PLACE-Z-CAL-OPEN", "PLACE Z CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "PLACE Z CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnNeedleZCal_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_needleZDialog == null || _needleZDialog.IsDisposed)
                {
                    _needleZDialog = NeedleCalibrationDialog.Open(host);
                    _needleZDialog.StartPosition = FormStartPosition.Manual;
                    _needleZDialog.Location = ResolveDialogLocation(_needleZDialog);
                    lblStatus.Text = "NEEDLE Z CAL 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _needleZDialog);
                lblStatus.Text = "NEEDLE Z CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NEEDLE-Z-CAL-OPEN", "NEEDLE Z CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "NEEDLE Z CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnAutoCalibration_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_autoCalibrationDialog == null || _autoCalibrationDialog.IsDisposed)
                {
                    _autoCalibrationDialog = AutoCalibrationDialog.Open(host);
                    _autoCalibrationDialog.StartPosition = FormStartPosition.Manual;
                    _autoCalibrationDialog.Location = ResolveDialogLocation(_autoCalibrationDialog);
                    lblStatus.Text = "AUTO CALIBRATION 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _autoCalibrationDialog);
                lblStatus.Text = "AUTO CALIBRATION 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "AUTO-CAL-OPEN",
                    "AUTO CALIBRATION 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this,
                    "AUTO CALIBRATION 설정창 열기 실패:\r\n" + ex.Message,
                    "Calibration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnColletCleaning_Click(object sender, EventArgs e)
        {
            try
            {
                Form host = FindForm();
                if (_colletCleaningDialog == null || _colletCleaningDialog.IsDisposed)
                {
                    _colletCleaningDialog = ColletCleaningControlDialog.Open(host);
                    _colletCleaningDialog.StartPosition = FormStartPosition.Manual;
                    _colletCleaningDialog.Location = ResolveDialogLocation(_colletCleaningDialog);
                    lblStatus.Text = "COLLET CLEANING 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, _colletCleaningDialog);
                lblStatus.Text = "COLLET CLEANING 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "COLLET-CLEAN-OPEN",
                    "COLLET CLEANING 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this,
                    "COLLET CLEANING 설정창 열기 실패:\r\n" + ex.Message,
                    "Calibration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ShowDialogOnce(ref NeedlePinCalibrationDialog dialog, string title, string purpose, string storageGuide)
        {
            try
            {
                Form host = FindForm();
                if (dialog == null || dialog.IsDisposed)
                {
                    dialog = NeedlePinCalibrationDialog.Open(host);
                    dialog.StartPosition = FormStartPosition.Manual;
                    dialog.Location = ResolveDialogLocation(dialog);
                    lblStatus.Text = "NEEDLE PIN CAL 설정창을 열었습니다.";
                    return;
                }

                ActivateDialog(host, dialog);
                lblStatus.Text = "NEEDLE PIN CAL 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "NEEDLE-PIN-CAL-OPEN", "NEEDLE PIN CAL 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "NEEDLE PIN CAL 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ShowDialogOnce(ref CalibrationSetupDialog dialog, string title, string purpose, string storageGuide)
        {
            try
            {
                Form host = FindForm();
                if (dialog == null || dialog.IsDisposed)
                {
                    dialog = new CalibrationSetupDialog(title, purpose, storageGuide);
                    dialog.Owner = host;
                    dialog.StartPosition = FormStartPosition.Manual;
                    dialog.Location = ResolveDialogLocation(dialog);
                    dialog.Show(host);
                    lblStatus.Text = title + " 설정창을 열었습니다.";
                    return;
                }

                if (!dialog.Visible)
                    dialog.Show(host);

                dialog.Activate();
                dialog.BringToFront();
                lblStatus.Text = title + " 설정창이 이미 열려 있습니다.";
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-OPEN", title + " 설정창 열기 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, title + " 설정창 열기 실패:\r\n" + ex.Message, "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private Point ResolveDialogLocation(Form dialog)
        {
            try
            {
                Form owner = FindForm();
                if (owner == null || dialog == null)
                    return new Point(120, 120);

                int x = owner.Left + Math.Max(20, owner.Width - dialog.Width - 260);
                int y = owner.Top + 150;
                return new Point(x, y);
            }
            catch
            {
                return new Point(120, 120);
            }
            finally
            {
            }
        }

        private void ActivateDialog(Form host, Form dialog)
        {
            try
            {
                if (dialog == null || dialog.IsDisposed)
                    return;

                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = ResolveDialogLocation(dialog);

                if (!dialog.Visible)
                    dialog.Show(host);

                if (dialog.WindowState == FormWindowState.Minimized)
                    dialog.WindowState = FormWindowState.Normal;

                dialog.BringToFront();
                dialog.Activate();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-ACTIVATE", "Calibration 설정창 표시 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }
    }
}
