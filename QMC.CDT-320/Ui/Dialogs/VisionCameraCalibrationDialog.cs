using System;
using System.Drawing;
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
        private const double CameraReticlePositionToleranceMm = 0.05;
        private VisionCameraCalibrationSequence _sequence;
        private CancellationTokenSource _cts;
        private bool _busy;

        private enum ManualCalibrationReadinessTarget
        {
            None,
            Bottom,
            Input,
            Output
        }

        public static VisionCameraCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "VisionCameraCalibrationDialog",
                owner,
                () => new VisionCameraCalibrationDialog());
        }

        public VisionCameraCalibrationDialog()
        {
            try
            {
                InitializeComponent();
                ApplyText();
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

        private void ApplyText()
        {
            try
            {
                Font koreanFont = new Font("맑은 고딕", 9F);
                Font koreanBoldFont = new Font("맑은 고딕", 9F, FontStyle.Bold);

                lblTitle.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                lblGuide.Font = koreanFont;
                lblSequenceGuide.Font = koreanFont;
                lblOffsets.Font = koreanBoldFont;
                lblStatus.Font = koreanFont;

                btnCheck.Font = koreanBoldFont;
                btnFindBottom.Font = koreanBoldFont;
                btnFindInput.Font = koreanBoldFont;
                btnFindOutput.Font = koreanBoldFont;
                btnRunAll.Font = koreanBoldFont;
                btnRetractReticle.Font = koreanBoldFont;
                btnCalculateSave.Font = koreanBoldFont;
                btnClose.Font = koreanBoldFont;

                lblGuide.Text = "Bottom/Input/Output 카메라가 같은 Reticle Mark를 찾은 좌표와 현재 모터 위치를 VisionUnit Config에 저장합니다.";
                lblSequenceGuide.Text =
                    "수행 순서\r\n\r\n" +
                    "1. CHECK READY\r\n" +
                    "2. PREPARE && FIND BOTTOM\r\n" +
                    "   - Input/Output VisionX Avoid 이동\r\n" +
                    "   - Front/Rear Picker Output Avoid 이동\r\n" +
                    "   - Reticle Lift Up -> Front Slide 전진 -> Rear Slide 전진\r\n" +
                    "   - Bottom Vision ReticleFinder 촬영 및 X/Y/T/Score 저장\r\n" +
                    "3. FIND INPUT\r\n" +
                    "   - Reticle Bottom 준비, Picker Output Avoid, Input 카메라 Reticle 위치 필요\r\n" +
                    "4. FIND OUTPUT\r\n" +
                    "   - Reticle Bottom 준비, Picker Input Avoid, Output 카메라 Reticle 위치 필요\r\n" +
                    "5. RETICLE BACK\r\n" +
                    "   - Rear Slide 후진 -> Front Slide 후진 -> Lift Down\r\n" +
                    "6. CALC / SAVE\r\n\r\n" +
                    "PREPARE && FIND BOTTOM 후 Reticle은 Bottom 촬영 준비 위치를 유지합니다. 복귀가 필요할 때만 RETICLE BACK을 실행하세요.";
                lblStatus.Text = "대기 중입니다.";

                toolTip.SetToolTip(btnCheck, "자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n측정 버튼을 누르기 전에 현재 장비 상태가 안전한지 확인합니다.");
                toolTip.SetToolTip(btnRunAll, "사전 준비 후 Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\nInput/Output VisionX와 Picker를 회피시키고 Reticle을 Bottom 촬영 위치로 전개한 뒤 Bottom 측정까지 수행합니다.");
                toolTip.SetToolTip(btnFindBottom, "Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\n성공하면 X/Y/T/Score를 VisionUnit Config의 Bottom 측정값으로 저장합니다.");
                toolTip.SetToolTip(btnFindInput, "Input Vision에 ReticleFinder 실행을 요청합니다.\r\nReticle Bottom 준비, Front/Rear Picker Output Avoid, Input 카메라 Reticle 위치 조건이 필요합니다.");
                toolTip.SetToolTip(btnFindOutput, "Output Vision에 ReticleFinder 실행을 요청합니다.\r\nReticle Bottom 준비, Front/Rear Picker Input Avoid, Output 카메라 Reticle 위치 조건이 필요합니다.");
                toolTip.SetToolTip(btnRetractReticle, "Reticle을 촬영 준비 위치에서 역순으로 복귀합니다.\r\nRear Slide 후진, Front Slide 후진, Lift Down 순서로 실행하고 최종 위치를 확인합니다.");
                toolTip.SetToolTip(btnCalculateSave, "Bottom/Input/Output 측정값으로 카메라 간 Offset을 계산합니다.\r\n계산된 값을 VisionUnit Config.CameraCalibration에 저장합니다.");
                toolTip.SetToolTip(btnClose, "Vision Camera Calibration 창을 닫습니다.");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "VISION-CAMERA-CAL-TEXT", "Vision Camera Calibration 문구 적용 실패: " + ex.Message);
            }
            finally
            {
            }
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
                lblStatus.Text = "실행 가능한 상태입니다. 각 카메라를 Reticle Mark가 보이는 위치에 준비한 뒤 Find를 실행하세요.";
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

        private async Task RunOperationAsync(
            string actionName,
            Func<CancellationToken, Task<int>> operation,
            ManualCalibrationReadinessTarget readinessTarget = ManualCalibrationReadinessTarget.None)
        {
            if (_busy)
                return;

            try
            {
                string reason;
                if (!CanRunManualCalibration(readinessTarget, out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _busy = true;
                SetButtonsEnabled(false);
                _cts = new CancellationTokenSource();
                lblStatus.Text = actionName + " 실행 중입니다. Vision 응답을 기다립니다.";

                int result = await operation(_cts.Token).ConfigureAwait(true);
                RefreshData();

                lblStatus.Text = result == 0
                    ? actionName + " 완료. 측정값이 VisionUnit Config에 반영되었습니다."
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
            return CanRunManualCalibration(ManualCalibrationReadinessTarget.None, out reason);
        }

        private bool CanRunManualCalibration(ManualCalibrationReadinessTarget readinessTarget, out string reason)
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

                if (!CanRunTargetCalibration(host, readinessTarget, out reason))
                    return false;

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

        private bool CanRunTargetCalibration(Form1 host, ManualCalibrationReadinessTarget readinessTarget, out string reason)
        {
            reason = string.Empty;

            if (readinessTarget == ManualCalibrationReadinessTarget.None)
                return true;

            if (host == null || host.Machine == null)
            {
                reason = "장비 객체가 준비되지 않아 캘리브레이션 준비 상태를 확인할 수 없습니다.";
                return false;
            }

            if (!IsReticleBottomReady(host.Machine, out reason))
                return false;

            if (readinessTarget == ManualCalibrationReadinessTarget.Bottom)
                return true;

            if (readinessTarget == ManualCalibrationReadinessTarget.Input)
            {
                if (!ArePickersAtOutputAvoid(host.Machine, out reason))
                    return false;

                return IsInputCameraAtReticleTeachingPosition(host.Machine, out reason);
            }

            if (readinessTarget == ManualCalibrationReadinessTarget.Output)
            {
                if (!ArePickersAtInputAvoid(host.Machine, out reason))
                    return false;

                return IsOutputCameraAtReticleTeachingPosition(host.Machine, out reason);
            }

            return true;
        }

        private bool IsReticleBottomReady(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (vision == null)
            {
                reason = "VisionUnit이 없어 Reticle 준비 상태를 확인할 수 없습니다.";
                return false;
            }

            if (IsCalibrationSimulationOrDryRun(vision))
                return true;

            bool up = vision.IsVisionReticleUp();
            bool frontForward = vision.IsVisionReticleFrontSideForward();
            bool rearForward = vision.IsVisionReticleRearSideForward();
            if (up && frontForward && rearForward)
                return true;

            reason = "Bottom 촬영 준비 상태가 아닙니다. Reticle 상태를 확인하세요. 필요상태=Lift Up, Front Slide 전진, Rear Slide 전진, 현재 up=" +
                     up + ", frontForward=" + frontForward + ", rearForward=" + rearForward;
            return false;
        }

        private bool ArePickersAtOutputAvoid(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null || machine.PickerFrontUnit == null || machine.PickerRearUnit == null)
            {
                reason = "Picker Unit이 없어 Output Avoid 상태를 확인할 수 없습니다.";
                return false;
            }

            bool front = machine.PickerFrontUnit.IsPickerInUnloadPosition();
            bool rear = machine.PickerRearUnit.IsPickerInUnloadPosition();
            if (front && rear)
                return true;

            reason = "Input 카메라 측정 전 Front/Rear Picker가 Output Avoid 위치에 있어야 합니다. frontOutputAvoid=" + front + ", rearOutputAvoid=" + rear;
            return false;
        }

        private bool ArePickersAtInputAvoid(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null || machine.PickerFrontUnit == null || machine.PickerRearUnit == null)
            {
                reason = "Picker Unit이 없어 Input Avoid 상태를 확인할 수 없습니다.";
                return false;
            }

            bool front = machine.PickerFrontUnit.IsPickerInLoadPosition();
            bool rear = machine.PickerRearUnit.IsPickerInLoadPosition();
            if (front && rear)
                return true;

            reason = "Output 카메라 측정 전 Front/Rear Picker가 Input Avoid 위치에 있어야 합니다. frontInputAvoid=" + front + ", rearInputAvoid=" + rear;
            return false;
        }

        private bool IsInputCameraAtReticleTeachingPosition(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            VisionReticleMeasurement target = ResolveReticleMeasurement(ManualCalibrationReadinessTarget.Input);
            if (target == null || !target.Valid || !target.HasVisionXPosition)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-INPUT-TEACH-MISSING",
                    "Input 카메라 Reticle 측정 기준 위치가 없어 현재 위치 검사는 생략합니다. 최초 측정 후에는 저장된 위치와 비교합니다.");
                return true;
            }

            if (machine == null || machine.InputStageUnit == null || machine.InputStageUnit.CameraX == null)
            {
                reason = "InputVisionX 축 정보가 없어 Reticle 측정 위치를 확인할 수 없습니다.";
                return false;
            }

            double actualX = machine.InputStageUnit.CameraX.ActualPosition;
            if (Math.Abs(actualX - target.VisionXPosition) > CameraReticlePositionToleranceMm)
            {
                reason = "Input 카메라가 Reticle 측정 위치가 아닙니다. actualX=" + actualX.ToString("F3") +
                         ", teachX=" + target.VisionXPosition.ToString("F3") +
                         ", tolerance=" + CameraReticlePositionToleranceMm.ToString("F3");
                return false;
            }

            return true;
        }

        private bool IsOutputCameraAtReticleTeachingPosition(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            VisionReticleMeasurement target = ResolveReticleMeasurement(ManualCalibrationReadinessTarget.Output);
            if (target == null || !target.Valid || !target.HasVisionXPosition)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-OUTPUT-TEACH-MISSING",
                    "Output 카메라 Reticle 측정 기준 위치가 없어 현재 위치 검사는 생략합니다. 최초 측정 후에는 저장된 위치와 비교합니다.");
                return true;
            }

            if (machine == null || machine.OutputStageUnit == null || machine.OutputStageUnit.OutputCameraX == null)
            {
                reason = "OutputVisionX 축 정보가 없어 Reticle 측정 위치를 확인할 수 없습니다.";
                return false;
            }

            double actualX = machine.OutputStageUnit.OutputCameraX.ActualPosition;
            if (Math.Abs(actualX - target.VisionXPosition) > CameraReticlePositionToleranceMm)
            {
                reason = "Output 카메라가 Reticle 측정 위치가 아닙니다. actualX=" + actualX.ToString("F3") +
                         ", teachX=" + target.VisionXPosition.ToString("F3") +
                         ", tolerance=" + CameraReticlePositionToleranceMm.ToString("F3");
                return false;
            }

            return true;
        }

        private VisionReticleMeasurement ResolveReticleMeasurement(ManualCalibrationReadinessTarget target)
        {
            try
            {
                VisionCameraCalibrationData data = Sequence.CalibrationData;
                if (data == null)
                    return null;

                data.EnsureObjects();
                if (target == ManualCalibrationReadinessTarget.Input)
                    return data.InputReticle;
                if (target == ManualCalibrationReadinessTarget.Output)
                    return data.OutputReticle;
                if (target == ManualCalibrationReadinessTarget.Bottom)
                    return data.BottomReticle;
            }
            catch
            {
            }
            finally
            {
            }

            return null;
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
                gridMeasurements.Rows.Add(name, "-", "-", "-", "-", "-");
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
                measurement.AngleDeg.ToString("F3"),
                axis,
                measurement.Score.ToString("F3"));
        }

        private bool IsCalibrationSimulationOrDryRun(VisionUnit vision)
        {
            try
            {
                if (vision != null &&
                    ((vision.Setup != null && vision.Setup.IsSimulationMode) ||
                     (vision.Config != null && vision.Config.IsSimulationMode)))
                    return true;

                return AppSettingsStore.Current != null &&
                       (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnCheck.Enabled = enabled;
            btnFindBottom.Enabled = enabled;
            btnFindInput.Enabled = enabled;
            btnFindOutput.Enabled = enabled;
            btnRunAll.Enabled = enabled;
            btnRetractReticle.Enabled = enabled;
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
