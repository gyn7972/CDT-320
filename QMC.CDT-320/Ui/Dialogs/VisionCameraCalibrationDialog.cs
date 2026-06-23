using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraCalibrationDialog : Form
    {
        private VisionCameraCalibrationSequence _sequence;
        private CancellationTokenSource _cts;
        private bool _busy;

        public VisionCameraCalibrationDialog()
        {
            try
            {
                InitializeComponent();
                WireEvents();
                RefreshData();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-DIALOG-INIT", "Vision Camera Calibration 창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                EnsureSequence();
                RefreshData();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "초기 상태 확인 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_busy)
                    _cts?.Cancel();
            }
            catch
            {
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

        private void WireEvents()
        {
            btnCheck.Click += btnCheck_Click;
            btnFindBottom.Click += async delegate { await RunOperationAsync("Bottom Reticle 측정", ct => Sequence.FindBottomReticleAsync(ct)); };
            btnFindInput.Click += async delegate { await RunOperationAsync("Input Reticle 측정", ct => Sequence.FindInputReticleAsync(ct)); };
            btnFindOutput.Click += async delegate { await RunOperationAsync("Output Reticle 측정", ct => Sequence.FindOutputReticleAsync(ct)); };
            btnRunAll.Click += async delegate { await RunOperationAsync("현재 위치 기준 Vision Camera Calibration", ct => Sequence.RunAsync(ct)); };
            btnCalculateSave.Click += async delegate
            {
                await RunOperationAsync("Vision Camera Calibration 계산/저장", delegate(CancellationToken ct)
                {
                    ct.ThrowIfCancellationRequested();
                    int result = Sequence.CalculateCalibration();
                    if (result != 0)
                        return Task.FromResult(result);
                    return Task.FromResult(Sequence.SaveCalibration());
                });
            };
            btnClose.Click += delegate { Close(); };
        }

        private VisionCameraCalibrationSequence Sequence
        {
            get
            {
                EnsureSequence();
                return _sequence;
            }
        }

        private void EnsureSequence()
        {
            if (_sequence != null)
                return;

            Form1 host = FindHostForm();
            if (host == null || host.Machine == null)
                throw new InvalidOperationException("메인 장비 객체가 준비되지 않았습니다.");

            _sequence = new VisionCameraCalibrationSequence(
                host.Machine,
                host.SaveMachineSettings,
                () => UserSession.Name);
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                EnsureSequence();
                lblStatus.Text = "실행 가능 상태입니다. Reticle을 카메라 시야 안에 준비한 뒤 측정하세요.";
                RefreshData();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "상태 확인 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-CHECK", lblStatus.Text);
            }
            finally
            {
            }
        }

        private async Task RunOperationAsync(string actionName, Func<CancellationToken, Task<int>> operation)
        {
            if (_busy)
                return;

            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _busy = true;
                SetButtonsEnabled(false);
                _cts = new CancellationTokenSource();
                lblStatus.Text = actionName + " 실행 중...";

                int result = await operation(_cts.Token).ConfigureAwait(true);
                RefreshData();

                lblStatus.Text = result == 0
                    ? actionName + " 완료."
                    : actionName + " 실패. result=" + result;
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = actionName + " 작업이 취소되었습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = actionName + " 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }

                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private bool CanRunManualCalibration(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = FindHostForm();
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

        private void RefreshData()
        {
            try
            {
                gridMeasurements.Rows.Clear();

                VisionCameraCalibrationData data = null;
                try
                {
                    data = _sequence != null ? _sequence.CalibrationData : null;
                }
                catch
                {
                    data = null;
                }

                if (data == null)
                {
                    AddMeasurementRow("Bottom", null);
                    AddMeasurementRow("Input", null);
                    AddMeasurementRow("Output", null);
                    lblOffsets.Text = "Offset: 데이터 없음";
                    return;
                }

                data.EnsureObjects();
                AddMeasurementRow("Bottom", data.BottomReticle);
                AddMeasurementRow("Input", data.InputReticle);
                AddMeasurementRow("Output", data.OutputReticle);

                lblOffsets.Text =
                    "Offset: Input-Bottom=(" + data.InputToBottomOffsetX.ToString("F6") + ", " + data.InputToBottomOffsetY.ToString("F6") + ") mm" +
                    " / Output-Bottom=(" + data.OutputToBottomOffsetX.ToString("F6") + ", " + data.OutputToBottomOffsetY.ToString("F6") + ") mm" +
                    " / valid=" + data.Valid;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "측정값 표시 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private void AddMeasurementRow(string name, VisionReticleMeasurement measurement)
        {
            if (measurement == null || !measurement.Valid)
            {
                gridMeasurements.Rows.Add(name, "-", "-", "-", "-");
                return;
            }

            string axis = "-";
            if (measurement.HasVisionXPosition || measurement.HasStageYPosition)
            {
                axis = "VisionX=" + (measurement.HasVisionXPosition ? measurement.VisionXPosition.ToString("F3") : "-") +
                       ", StageY=" + (measurement.HasStageYPosition ? measurement.StageYPosition.ToString("F3") : "-");
            }

            gridMeasurements.Rows.Add(
                name,
                measurement.PixelX.ToString("F3") + " / " + measurement.PixelY.ToString("F3"),
                measurement.MmX.ToString("F6") + " / " + measurement.MmY.ToString("F6"),
                axis,
                measurement.Score.ToString("F3"));
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnCheck.Enabled = enabled;
            btnFindBottom.Enabled = enabled;
            btnFindInput.Enabled = enabled;
            btnFindOutput.Enabled = enabled;
            btnRunAll.Enabled = enabled;
            btnCalculateSave.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        private Form1 FindHostForm()
        {
            try
            {
                Form owner = Owner;
                Form1 host = owner as Form1;
                if (host != null)
                    return host;

                foreach (Form form in Application.OpenForms)
                {
                    host = form as Form1;
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
    }
}
