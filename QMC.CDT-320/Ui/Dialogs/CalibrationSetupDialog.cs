using System;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class CalibrationSetupDialog : Form
    {
        private readonly string _title;
        private readonly string _purpose;
        private readonly string _storageGuide;
        private bool _busy;

        public CalibrationSetupDialog(string title, string purpose, string storageGuide)
        {
            try
            {
                _title = string.IsNullOrWhiteSpace(title) ? "CALIBRATION" : title;
                _purpose = purpose ?? string.Empty;
                _storageGuide = storageGuide ?? string.Empty;

                InitializeComponent();
                ApplyText();
                WireEvents();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-INIT", "Calibration 설정창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void ApplyText()
        {
            Text = _title;
            lblTitle.Text = _title;
            lblPurpose.Text = _purpose;
            lblStorageGuide.Text = _storageGuide;
            lblStatus.Text = "대기 중입니다. 실제 캘리브레이션 시퀀스는 다음 단계에서 연결합니다.";
        }

        private void WireEvents()
        {
            try
            {
                btnCheck.Click += btnCheck_Click;
                btnClose.Click += delegate { Close(); };
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-EVENT", _title + " 이벤트 연결 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            if (_busy)
                return;

            try
            {
                _busy = true;
                SetButtonsEnabled(false);

                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, _title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblStatus.Text = "수동 캘리브레이션 실행 가능 상태입니다. 실제 시퀀스 연결 후 이 게이트를 공통으로 사용합니다.";
            }
            catch (Exception ex)
            {
                string message = _title + " 상태 확인 중 예외가 발생했습니다: " + ex.Message;
                lblStatus.Text = message;
                EventLogger.Write(EventKind.Alarm, "UI", "CAL-DIALOG-CHECK", message);
                QMC.Common.MessageDialog.Show(this, message, _title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private bool CanRunManualCalibration(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = Owner as Form1;
                if (host == null)
                    host = FindHostForm();

                if (host == null || host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않아 캘리브레이션을 실행할 수 없습니다.";
                    return false;
                }

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 캘리브레이션을 실행하세요.";
                    return false;
                }

                EquipmentStatus status = host.Controller.Status;
                if (status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 캘리브레이션을 실행할 수 없습니다.";
                    return false;
                }

                if (status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다. 완료 후 다시 실행하세요.";
                    return false;
                }

                if (host.Controller.IsSequenceRunning)
                {
                    reason = "시퀀스가 실행 중입니다. 완료 후 캘리브레이션을 실행하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "캘리브레이션 실행 조건 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    Form1 host = form as Form1;
                    if (host != null)
                        return host;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            try
            {
                btnCheck.Enabled = enabled;
                btnClose.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
