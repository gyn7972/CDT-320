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
                lblValueTitle.Font = koreanBoldFont;
                gridAppliedValues.Font = koreanFont;
                lblOffsets.Font = koreanBoldFont;
                lblStatus.Font = koreanFont;

                btnLoadValues.Font = koreanBoldFont;
                btnSaveReticleValues.Font = koreanBoldFont;
                btnCheck.Font = koreanBoldFont;
                btnFindBottom.Font = koreanBoldFont;
                btnFindInput.Font = koreanBoldFont;
                btnFindOutput.Font = koreanBoldFont;
                btnRunAll.Font = koreanBoldFont;
                btnRetractReticle.Font = koreanBoldFont;
                btnCalculateSave.Font = koreanBoldFont;
                btnHelp.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                btnClose.Font = koreanBoldFont;
                CalibrationDialogButtonStyle.ApplyCompactButtons(btnLoadValues, btnSaveReticleValues);
                CalibrationDialogButtonStyle.ApplyFooterButtons(
                    new[] { btnCheck, btnRunAll, btnFindBottom, btnFindInput, btnFindOutput, btnRetractReticle, btnClose },
                    new[] { btnCalculateSave },
                    null,
                    new[] { btnHelp });

                lblGuide.Text = "Bottom/Input/Output 카메라가 같은 Reticle Mark를 찾은 좌표와 현재 모터 위치를 VisionUnit Config에 저장합니다.";
                lblValueTitle.Text = "FIDUCIAL OFFSET";
                lblStatus.Text = "대기 중입니다.";

                toolTip.SetToolTip(btnLoadValues, "저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 읽어 표시합니다.");
                toolTip.SetToolTip(btnSaveReticleValues, "FIND INPUT/OUTPUT에서 측정한 InputVisionX/OutputVisionX 위치를 ReticlePosition으로 저장합니다.\r\nBottom 위 Reticle 촬영 X Encoder 값도 VisionUnit Config에 함께 저장합니다.");
                toolTip.SetToolTip(btnCheck, "자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n측정 버튼을 누르기 전에 현재 장비 상태가 안전한지 확인합니다.");
                toolTip.SetToolTip(btnRunAll, "사전 준비 후 Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 Rear Back -> Front Back -> Lift Down으로 복귀한 뒤 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동합니다.");
                toolTip.SetToolTip(btnFindBottom, "Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\n성공하면 X/Y/T/Score를 VisionUnit Config의 Bottom 측정값으로 저장합니다.");
                toolTip.SetToolTip(btnFindInput, "Input Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동한 뒤 InputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnFindOutput, "Output Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Input-side Avoid로 안전 순차 이동한 뒤 OutputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnRetractReticle, "Reticle을 촬영 준비 위치에서 역순으로 복귀합니다.\r\nRear Slide 후진, Front Slide 후진, Lift Down 순서로 실행하고 최종 위치를 확인합니다.");
                toolTip.SetToolTip(btnCalculateSave, "Bottom/Input/Output 측정값으로 카메라 간 Offset을 계산합니다.\r\n계산된 값을 CalibrationData.Camera에 저장합니다.");
                toolTip.SetToolTip(btnHelp, "Vision Camera Calibration 수행 순서를 표시합니다.");
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

        private void btnHelp_Click(object sender, EventArgs e)
        {
            try
            {
                HelpTextDialog.ShowDialog(this, "VISION CAMERA CAL 초기 셋팅 및 수행 순서", BuildSequenceGuideText());
            }
            catch (Exception ex)
            {
                lblStatus.Text = "수행 순서 표시 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private void btnLoadValues_Click(object sender, EventArgs e)
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

                Form1 host = FindHostForm();
                if (host == null || host.Machine == null)
                    throw new InvalidOperationException("장비 객체가 준비되지 않았습니다.");

                string recipeName = host.CurrentRecipeName;
                host.LoadMachineSettings();
                if (!string.IsNullOrWhiteSpace(recipeName))
                    host.LoadMachineRecipe(recipeName);

                _sequence = null;
                EnsureSequence();
                RefreshData();
                lblStatus.Text = "저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 불러왔습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "값 로드 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-LOAD-VALUE", lblStatus.Text);
            }
            finally
            {
            }
        }

        private void btnSaveReticleValues_Click(object sender, EventArgs e)
        {
            try
            {
                Form1 host = FindHostForm();
                if (host == null || host.Machine == null)
                    throw new InvalidOperationException("장비 객체가 준비되지 않았습니다.");

                if (host.Machine.InputStageUnit == null ||
                    host.Machine.InputStageUnit.Recipe == null ||
                    host.Machine.InputStageUnit.Recipe.VisionX == null)
                    throw new InvalidOperationException("InputVisionX ReticlePosition 저장을 위한 Recipe 정보가 없습니다.");

                if (host.Machine.OutputStageUnit == null ||
                    host.Machine.OutputStageUnit.Recipe == null ||
                    host.Machine.OutputStageUnit.Recipe.VisionX == null)
                    throw new InvalidOperationException("OutputVisionX ReticlePosition 저장을 위한 Recipe 정보가 없습니다.");

                VisionCameraCalibrationData data = Sequence.CalibrationData;
                if (data == null)
                    throw new InvalidOperationException("Vision Camera Calibration 데이터가 준비되지 않았습니다.");

                data.EnsureObjects();
                if (data.InputReticle == null ||
                    !data.InputReticle.Valid ||
                    !data.InputReticle.HasVisionXPosition)
                    throw new InvalidOperationException("Input Reticle 측정 위치가 없습니다. FIND INPUT을 먼저 수행하세요.");

                if (data.OutputReticle == null ||
                    !data.OutputReticle.Valid ||
                    !data.OutputReticle.HasVisionXPosition)
                    throw new InvalidOperationException("Output Reticle 측정 위치가 없습니다. FIND OUTPUT을 먼저 수행하세요.");

                double inputX = data.InputReticle.VisionXPosition;
                double outputX = data.OutputReticle.VisionXPosition;

                host.Machine.InputStageUnit.Recipe.VisionX.ReticlePosition = inputX;
                host.Machine.OutputStageUnit.Recipe.VisionX.ReticlePosition = outputX;

                data.InputReticle.VisionXPosition = inputX;
                data.InputReticle.HasVisionXPosition = true;
                data.OutputReticle.VisionXPosition = outputX;
                data.OutputReticle.HasVisionXPosition = true;
                host.Machine.VisionUnit.Config.CalibrationData.Touch(UserSession.Name);

                string recipeName = host.CurrentRecipeName;
                if (string.IsNullOrWhiteSpace(recipeName))
                    throw new InvalidOperationException("활성 Recipe가 없어 ReticlePosition을 저장할 수 없습니다.");

                if (!host.Machine.SaveRecipe(recipeName))
                    throw new InvalidOperationException("현재 Recipe 파일 저장에 실패했습니다. recipe=" + recipeName);

                if (!host.Machine.SaveSettings())
                    throw new InvalidOperationException("CalibrationData 파일 저장에 실패했습니다.");

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SAVE-RETICLE-POS",
                    "Vision Camera Calibration 측정 위치로 ReticlePosition 및 Bottom Reticle X Encoder 저장. recipe=" + recipeName +
                    ", InputVisionX=" + inputX.ToString("F3") +
                    ", OutputVisionX=" + outputX.ToString("F3"));

                RefreshAppliedValueGrid();
                lblStatus.Text = "FIND INPUT/OUTPUT에서 측정한 위치를 Recipe ReticlePosition과 CalibrationData의 X Encoder 값으로 저장했습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "ReticlePosition 저장 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-SAVE-RETICLE-POS", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private string BuildSequenceGuideText()
        {
            return
                "목적\r\n\r\n" +
                "Bottom/Input/Output 카메라가 같은 Reticle Mark를 기준으로 Die 좌표를 1:1로 맞추기 위한 캘리브레이션입니다.\r\n" +
                "소프트웨어 보정은 기구 셋팅 후 남는 미세 오차를 저장하는 용도입니다. 센터에서 크게 벗어난 상태를 억지로 보정하는 용도로 사용하지 마세요.\r\n\r\n" +
                "초기 기구 셋팅 시 필요 작업\r\n\r\n" +
                "1. Bottom 카메라 위 Reticle Mark를 기구적으로 카메라 화면 센터에 최대한 맞춥니다.\r\n" +
                "2. Input 카메라가 같은 Reticle Mark를 화면 센터 근처에서 볼 수 있도록 InputVisionX Reticle 위치를 맞춥니다.\r\n" +
                "3. Output 카메라가 같은 Reticle Mark를 화면 센터 근처에서 볼 수 있도록 OutputVisionX Reticle 위치를 맞춥니다.\r\n" +
                "4. 각 카메라 화면에서 Reticle Mark가 Finder 검색 영역 안에 안정적으로 들어오는지 확인합니다.\r\n" +
                "5. 조명, 초점, Reticle 실린더 위치가 반복 동작해도 흔들리지 않는지 확인합니다.\r\n\r\n" +
                "버튼별 의미\r\n\r\n" +
                "- CALC / SAVE: Bottom/Input/Output 측정값으로 카메라 간 Offset을 계산하고 CalibrationData.Camera에 저장합니다. 정상 저장 후 valid=True가 됩니다.\r\n" +
                "- SAVE POS: FIND INPUT/OUTPUT 때 측정된 InputVisionX/OutputVisionX Encoder 위치를 현재 Recipe의 ReticlePosition으로 저장합니다. Offset 계산값 valid 상태는 변경하지 않습니다.\r\n" +
                "- LOAD: 저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 읽어 화면에 표시합니다.\r\n\r\n" +
                "순차 수행 작업\r\n\r\n" +
                "1. CHECK READY\r\n" +
                "   - 자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n\r\n" +
                "2. PREPARE && FIND BOTTOM\r\n" +
                "   - Input/Output VisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Front Back -> Lift Down)\r\n" +
                "   - Front/Rear Picker Output-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Front Slide 전진 -> Rear Slide 전진\r\n" +
                "   - Bottom Vision ReticleFinder 촬영 및 X/Y/T/Score 저장\r\n\r\n" +
                "3. FIND INPUT\r\n" +
                "   - OutputVisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Front Back -> Lift Down)\r\n" +
                "   - Front/Rear Picker Output-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Front Slide 전진 -> Rear Slide 전진\r\n" +
                "   - InputVisionX Reticle 위치 이동 후 ReticleFinder 촬영\r\n\r\n" +
                "4. FIND OUTPUT\r\n" +
                "   - InputVisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Front Back -> Lift Down)\r\n" +
                "   - Front/Rear Picker Input-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Front Slide 전진 -> Rear Slide 전진\r\n" +
                "   - OutputVisionX Reticle 위치 이동 후 ReticleFinder 촬영\r\n\r\n" +
                "5. RETICLE BACK\r\n" +
                "   - Rear Slide 후진 -> Front Slide 후진 -> Lift Down\r\n\r\n" +
                "6. CALC / SAVE\r\n" +
                "   - Bottom/Input/Output 측정값으로 Offset을 계산하고 VisionUnit Config의 CalibrationData.Camera에 저장합니다.\r\n" +
                "   - 저장 후 Offset valid=True 상태인지 확인합니다.\r\n\r\n" +
                "7. SAVE POS\r\n" +
                "   - FIND INPUT/OUTPUT 때 측정한 InputVisionX/OutputVisionX Encoder 값을 Recipe ReticlePosition에 저장합니다.\r\n" +
                "   - 저장 시점의 현재 축 위치를 다시 읽지 않습니다. 반드시 측정했던 위치값을 저장합니다.\r\n\r\n" +
                "주의 사항\r\n\r\n" +
                "- PREPARE && FIND BOTTOM 후 Reticle은 Bottom 촬영 준비 위치를 유지합니다. 복귀가 필요할 때만 RETICLE BACK을 실행하세요.\r\n" +
                "- 실장비에서는 Vision PC가 ReticleFinder 결과 X/Y/T/Score를 정상 응답해야 합니다.\r\n" +
                "- 비전 미연결 시뮬/드라이런 테스트에서는 카메라 센터 근처 픽셀 오차를 임의 생성하여 같은 계산 루트를 검증합니다.\r\n" +
                "- 기구 셋팅 없이 Reticle Mark가 화면 가장자리로 치우친 상태에서 저장하면 렌즈 왜곡, 조명 편차, Finder 검색 실패로 보정 신뢰도가 떨어질 수 있습니다.";
        }

        private async Task RunOperationAsync(
            string actionName,
            Func<CancellationToken, Task<int>> operation,
            ManualCalibrationReadinessTarget readinessTarget = ManualCalibrationReadinessTarget.None)
        {
            if (_busy)
                return;

            Form1 host = null;
            Action stopHandler = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;

            try
            {
                string reason;
                if (!CanRunManualCalibration(readinessTarget, out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = FindHostForm();
                if (host == null || host.Controller == null)
                    throw new InvalidOperationException("MachineController가 준비되지 않았습니다.");

                _busy = true;
                SetButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence,
                    "VisionCameraCalibration:" + actionName);
                runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
                _cts = runCts;
                stopHandler = delegate
                {
                    try
                    {
                        CancellationTokenSource cts = _cts;
                        if (cts != null && !cts.IsCancellationRequested)
                            cts.Cancel();

                        QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionCameraCalStop",
                            "메인 STOP 요청으로 Vision Camera Calibration 정지 요청. action=" + actionName);
                    }
                    catch
                    {
                    }
                };
                host.Controller.StopRequested += stopHandler;

                lblStatus.Text = actionName + " 실행 중입니다. Vision 응답을 기다립니다.";

                int result = await operation(runCts.Token).ConfigureAwait(true);
                RefreshData();

                lblStatus.Text = result == 0
                    ? actionName + " 완료. 측정값이 VisionUnit Config에 반영되었습니다."
                    : actionName + " 실패. result=" + result;
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = actionName + " 작업이 취소되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = actionName + " 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (host != null && host.Controller != null && stopHandler != null)
                    host.Controller.StopRequested -= stopHandler;

                if (ReferenceEquals(_cts, runCts))
                    _cts = null;

                if (runCts != null)
                    runCts.Dispose();

                if (actionScope != null)
                    actionScope.Dispose();

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
                return true;
            }

            if (readinessTarget == ManualCalibrationReadinessTarget.Output)
            {
                return true;
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
                reason = "Picker Unit이 없어 Output-side Avoid 상태를 확인할 수 없습니다.";
                return false;
            }

            bool front = machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition();
            bool rear = machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition();
            if (front && rear)
                return true;

            reason = "Input 카메라 측정 전 Front/Rear Picker가 Output-side Avoid 위치에 있어야 합니다. frontOutputSideAvoid=" + front + ", rearOutputSideAvoid=" + rear;
            return false;
        }

        private bool ArePickersAtInputAvoid(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null || machine.PickerFrontUnit == null || machine.PickerRearUnit == null)
            {
                reason = "Picker Unit이 없어 Input-side Avoid 상태를 확인할 수 없습니다.";
                return false;
            }

            bool front = machine.PickerFrontUnit.IsPickerInInputSideAvoidPosition();
            bool rear = machine.PickerRearUnit.IsPickerInInputSideAvoidPosition();
            if (front && rear)
                return true;

            reason = "Output 카메라 측정 전 Front/Rear Picker가 Input-side Avoid 위치에 있어야 합니다. frontInputSideAvoid=" + front + ", rearInputSideAvoid=" + rear;
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
                    RefreshAppliedValueGrid();
                    return;
                }

                data.EnsureObjects();
                AddMeasurementRow("Bottom", data.BottomReticle);
                AddMeasurementRow("Input", data.InputReticle);
                AddMeasurementRow("Output", data.OutputReticle);

                lblOffsets.Text =
                    "Offset: Bottom-Input=(" + data.InputToBottomOffsetX.ToString("F6") + ", " + data.InputToBottomOffsetY.ToString("F6") + ") mm" +
                    " / Bottom-Output=(" + data.OutputToBottomOffsetX.ToString("F6") + ", " + data.OutputToBottomOffsetY.ToString("F6") + ") mm" +
                    " / valid=" + data.Valid;

                RefreshAppliedValueGrid();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "측정값 표시 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        private void RefreshAppliedValueGrid()
        {
            try
            {
                if (gridAppliedValues == null)
                    return;

                gridAppliedValues.Rows.Clear();

                VisionCameraCalibrationData data = null;
                try
                {
                    data = _sequence != null ? _sequence.CalibrationData : null;
                    if (data != null)
                        data.EnsureObjects();
                }
                catch
                {
                    data = null;
                }

                if (data == null)
                {
                    AddAppliedValueRow("Bottom Pixel X/Y", "-");
                    AddAppliedValueRow("Input Pixel X/Y", "-");
                    AddAppliedValueRow("Output Pixel X/Y", "-");
                    AddAppliedValueRow("Input VisionX Encoder", "-");
                    AddAppliedValueRow("Output VisionX Encoder", "-");
                    AddAppliedValueRow("Bottom-Input Offset", "-");
                    AddAppliedValueRow("Bottom-Output Offset", "-");
                    return;
                }

                AddAppliedValueRow("Bottom Pixel X/Y", FormatPixel(data.BottomReticle));
                AddAppliedValueRow("Input Pixel X/Y", FormatPixel(data.InputReticle));
                AddAppliedValueRow("Output Pixel X/Y", FormatPixel(data.OutputReticle));
                AddAppliedValueRow("Input VisionX Encoder", FormatVisionXPosition(data.InputReticle));
                AddAppliedValueRow("Output VisionX Encoder", FormatVisionXPosition(data.OutputReticle));
                AddAppliedValueRow("Bottom-Input Offset", FormatOffset(data.InputToBottomOffsetX, data.InputToBottomOffsetY));
                AddAppliedValueRow("Bottom-Output Offset", FormatOffset(data.OutputToBottomOffsetX, data.OutputToBottomOffsetY));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "VISION-CAMERA-CAL-VALUE-GRID", "Vision Camera Calibration 적용값 표시 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddAppliedValueRow(string item, string value)
        {
            gridAppliedValues.Rows.Add(item, string.Empty, string.Empty, value);
        }

        private string FormatPixel(VisionReticleMeasurement measurement)
        {
            if (measurement == null || !measurement.Valid)
                return "-";

            return measurement.PixelX.ToString("F3") + ", " + measurement.PixelY.ToString("F3") + " px";
        }

        private string FormatOffset(double x, double y)
        {
            return x.ToString("F6") + ", " + y.ToString("F6") + " mm";
        }

        private string FormatVisionXPosition(VisionReticleMeasurement measurement)
        {
            if (measurement == null || !measurement.HasVisionXPosition)
                return "-";

            return measurement.VisionXPosition.ToString("F3") + " mm";
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
            btnLoadValues.Enabled = enabled;
            btnSaveReticleValues.Enabled = enabled;
            btnCheck.Enabled = enabled;
            btnFindBottom.Enabled = enabled;
            btnFindInput.Enabled = enabled;
            btnFindOutput.Enabled = enabled;
            btnRunAll.Enabled = enabled;
            btnRetractReticle.Enabled = enabled;
            btnCalculateSave.Enabled = enabled;
            btnHelp.Enabled = enabled;
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
