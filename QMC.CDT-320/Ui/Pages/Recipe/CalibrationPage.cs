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
        private CalibrationSetupDialog _colletZHeightDialog;
        private VisionFocusCalibrationDialog _visionFocusDialog;
        private CalibrationSetupDialog _colletRotationCenterDialog;
        private PickUpZCalibrationDialog _pickUpZDialog;
        private PlaceZCalibrationDialog _placeZDialog;
        private NeedleCalibrationDialog _needleZDialog;

        public CalibrationPage()
        {
            try
            {
                InitializeComponent();
                // 색/폰트(페이지·헤더·상태 라벨)는 Designer(.Designer.cs)로 이관
                // 버튼 Click 이벤트 연결도 Designer(InitializeComponent)로 이관
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

        private void btnColletZHeightCal_Click(object sender, EventArgs e)
        {
            ShowDialogOnce(
                ref _colletZHeightDialog,
                "COLLET Z HEIGHT CAL",
                "Vacuum과 Flow 센서 기준으로 Front/Rear 콜렛 1~4번의 Z 기준 높이를 측정하는 캘리브레이션입니다.",
                "저장 제안: 콜렛별 기준 높이는 Config, 제품 두께는 Recipe에 저장합니다.");
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
            ShowDialogOnce(
                ref _colletRotationCenterDialog,
                "COLLET ROTATION CENTER CAL",
                "Bottom 카메라에서 콜렛 회전 각도별 위치를 측정해 회전 중심과 보정 오프셋을 계산하는 캘리브레이션입니다.",
                "저장 제안: 콜렛별 회전 중심 보정값은 Config에 저장합니다.");
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
