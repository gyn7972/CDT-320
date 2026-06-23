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
        private CalibrationSetupDialog _colletDialog;
        private CalibrationSetupDialog _needleDialog;
        private CalibrationSetupDialog _colletZHeightDialog;
        private CalibrationSetupDialog _visionFocusDialog;
        private CalibrationSetupDialog _colletRotationCenterDialog;

        public CalibrationPage()
        {
            try
            {
                InitializeComponent();
                ApplyRuntimeStyle();
                WireEvents();
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

        private void ApplyRuntimeStyle()
        {
            try
            {
                BackColor = Color.FromArgb(207, 210, 214);
                rootLayout.BackColor = BackColor;
                headerPanel.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("맑은 고딕", 13F, FontStyle.Bold);
                lblStatus.ForeColor = Color.FromArgb(40, 40, 40);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "CAL-PAGE-STYLE", "CalibrationPage 스타일 적용 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            try
            {
                btnVisionCameraCal.Click += btnVisionCameraCal_Click;
                btnColletCal.Click += btnColletCal_Click;
                btnNeedleCal.Click += btnNeedleCal_Click;
                btnColletZHeightCal.Click += btnColletZHeightCal_Click;
                btnVisionFocusCal.Click += btnVisionFocusCal_Click;
                btnColletRotationCenterCal.Click += btnColletRotationCenterCal_Click;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-PAGE-EVENT", "CalibrationPage 이벤트 연결 실패: " + ex.Message);
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
            ShowDialogOnce(
                ref _colletDialog,
                "COLLET CAL",
                "Bottom 카메라로 Front/Rear 콜렛 1~4번의 X/Y/T 보정값을 찾는 캘리브레이션입니다.",
                "저장 제안: 콜렛별 물리 보정값은 Config에 저장하고, 레시피별 교체 조건은 별도 Recipe 연동을 검토합니다.");
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
            ShowDialogOnce(
                ref _visionFocusDialog,
                "VISION FOCUS CAL",
                "Bottom, Front Side, Rear Side 카메라의 Focus Score를 스캔하여 최적 위치를 찾는 캘리브레이션입니다.",
                "저장 제안: 스캔 범위와 Step 기본값은 Config, 제품별 Focus 위치가 다르면 Recipe override를 둡니다.");
        }

        private void btnColletRotationCenterCal_Click(object sender, EventArgs e)
        {
            ShowDialogOnce(
                ref _colletRotationCenterDialog,
                "COLLET ROTATION CENTER CAL",
                "Bottom 카메라에서 콜렛 회전 각도별 위치를 측정해 회전 중심과 보정 오프셋을 계산하는 캘리브레이션입니다.",
                "저장 제안: 콜렛별 회전 중심 보정값은 Config에 저장합니다.");
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
    }
}
