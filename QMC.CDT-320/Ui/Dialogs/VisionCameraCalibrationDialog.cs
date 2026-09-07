using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraCalibrationDialog : Form
    {
        private const double CameraReticlePositionToleranceMm = 0.05;
        private const string MotionSpeedRow = "Move Speed";
        private const string MotionAccRow = "Move Acc";
        private const string MotionDecRow = "Move Dec";
        private const string MotionTimeoutRow = "Move Timeout";
        private const string BottomPixelXRow = "Bottom Pixel X";
        private const string BottomPixelYRow = "Bottom Pixel Y";
        private const string InputPixelXRow = "Input Pixel X";
        private const string InputPixelYRow = "Input Pixel Y";
        private const string OutputPixelXRow = "Output Pixel X";
        private const string OutputPixelYRow = "Output Pixel Y";
        private const string InputVisionXEncoderRow = "Input VisionX Encoder";
        private const string OutputVisionXEncoderRow = "Output VisionX Encoder";
        private const string BottomInputOffsetXRow = "Bottom-Input Offset X";
        private const string BottomInputOffsetYRow = "Bottom-Input Offset Y";
        private const string BottomOutputOffsetXRow = "Bottom-Output Offset X";
        private const string BottomOutputOffsetYRow = "Bottom-Output Offset Y";
        private VisionCameraCalibrationSequence _sequence;
        private CancellationTokenSource _cts;
        private bool _busy;

        // 실제 FIND 결과와 수동 편집을 구분하여, 저장할 Reticle 위치만 선택합니다.
        private bool _inputReticleMeasuredInSession;
        private bool _outputReticleMeasuredInSession;
        private double _measuredInputVisionX;
        private double _measuredOutputVisionX;

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
                CalibrationDialogGridBehavior.Apply(gridMeasurements, gridAppliedValues);
                gridAppliedValues.CellDoubleClick += gridAppliedValues_CellDoubleClick;
                gridAppliedValues.CellToolTipTextNeeded += gridAppliedValues_CellToolTipTextNeeded;
                UserSession.UserChanged += UserSession_UserChanged;
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
                UpdateSaveReticleButtonEnabled();
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
            if (_manualDraft != null && !_busy && QMC.Common.MessageDialog.Show(this,
                "저장하지 않은 수정값을 취소하고 닫으시겠습니까?", "VISION CAMERA CAL",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
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
                UserSession.UserChanged -= UserSession_UserChanged;
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
                btnSaveReticleValues.Text = "SAVE / APPLY";
                lblStatus.Text = "대기 중입니다.";

                toolTip.SetToolTip(btnLoadValues, "저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 읽어 표시합니다.");
                toolTip.SetToolTip(btnSaveReticleValues, "수정값을 저장하고 픽커 보정에 적용합니다.\r\n이번에 측정하거나 직접 수정한 Encoder 위치만 현재 Recipe에도 저장합니다.");
                toolTip.SetToolTip(gridAppliedValues, "Admin 권한에서 더블클릭하여 편집합니다. SAVE / APPLY 전까지 운전값은 변경되지 않습니다.");
                toolTip.SetToolTip(btnCheck, "자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n측정 버튼을 누르기 전에 현재 장비 상태가 안전한지 확인합니다.");
                toolTip.SetToolTip(btnRunAll, "사전 준비 후 Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 Rear Back -> Lift Down으로 복귀한 뒤 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동합니다. Front Slide는 Rear Back 기준으로 확인합니다.");
                toolTip.SetToolTip(btnFindBottom, "Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\n성공하면 X/Y/T/Score를 VisionUnit Config의 Bottom 측정값으로 저장합니다.");
                toolTip.SetToolTip(btnFindInput, "Input Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동한 뒤 InputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnFindOutput, "Output Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Input-side Avoid로 안전 순차 이동한 뒤 OutputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnRetractReticle, "Reticle을 촬영 준비 위치에서 복귀합니다.\r\nRear Slide 후진, Lift Down 순서로 실행하고 최종 위치를 확인합니다. Front Slide는 Rear Back 기준으로 확인합니다.");
                toolTip.SetToolTip(btnCalculateSave, "Bottom/Input/Output 측정값으로 카메라 간 Offset을 계산하고 수동 보정량을 유지합니다.\r\nSAVE / APPLY와 동일하게 파일과 픽커 보정을 함께 저장·적용합니다.");
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

                string recipeName = host.ActiveRecipeName;
                host.LoadMachineSettings();
                if (!string.IsNullOrWhiteSpace(recipeName) &&
                    !host.LoadMachineRecipe(recipeName))
                {
                    throw new InvalidOperationException(
                        "현재 Recipe 값을 다시 불러오지 못했습니다. recipe=" + recipeName);
                }

                _sequence = null;
                EnsureSequence();
                // 파일에서 다시 읽었으므로 세션 측정 기록은 무효다(과거값 저장 차단).
                DiscardManualDraft();
                ClearReticleMeasuredInSession();
                RefreshData();
                lblStatus.Text = "미저장 편집을 취소하고 저장된 Settings와 현재 Recipe를 불러왔습니다.";
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
            SaveAndApplyCameraValues(false);
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
                "- SAVE / APPLY: 수정값을 검증하여 저장·적용합니다. 이번에 측정하거나 수정한 Encoder만 현재 Recipe에 저장합니다.\r\n" +
                "- LOAD: 미저장 편집을 취소하고 저장된 Settings와 현재 Recipe를 다시 읽습니다.\r\n" +
                "- 수동 Offset: 입력한 최종값과 측정 산식의 차이를 보정량으로 보관하므로 CALC 후에도 보정이 유지됩니다.\r\n\r\n" +
                "순차 수행 작업\r\n\r\n" +
                "1. CHECK READY\r\n" +
                "   - 자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n\r\n" +
                "2. PREPARE && FIND BOTTOM\r\n" +
                "   - Input/Output VisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Lift Down, Front Slide는 Rear Back 기준 확인)\r\n" +
                "   - Front/Rear Picker Output-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Rear Slide 전진(Front Slide는 Rear Forward 기준 확인)\r\n" +
                "   - Bottom Vision ReticleFinder 촬영 및 X/Y/T/Score 저장\r\n\r\n" +
                "3. FIND INPUT\r\n" +
                "   - OutputVisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Lift Down, Front Slide는 Rear Back 기준 확인)\r\n" +
                "   - Front/Rear Picker Output-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Rear Slide 전진(Front Slide는 Rear Forward 기준 확인)\r\n" +
                "   - InputVisionX Reticle 위치 이동 후 ReticleFinder 촬영\r\n\r\n" +
                "4. FIND OUTPUT\r\n" +
                "   - InputVisionX Avoid 이동\r\n" +
                "   - Reticle 안전 위치 복귀(Rear Back -> Lift Down, Front Slide는 Rear Back 기준 확인)\r\n" +
                "   - Front/Rear Picker Input-side Avoid 이동(Z Avoid -> Y Avoid -> X 이동)\r\n" +
                "   - Reticle Lift Up -> Rear Slide 전진(Front Slide는 Rear Forward 기준 확인)\r\n" +
                "   - OutputVisionX Reticle 위치 이동 후 ReticleFinder 촬영\r\n\r\n" +
                "5. RETICLE BACK\r\n" +
                "   - Rear Slide 후진 -> Lift Down. Front Slide는 동작하지 않고 Rear Back 기준으로 확인합니다.\r\n\r\n" +
                "6. CALC / SAVE\r\n" +
                "   - Bottom/Input/Output 측정값으로 Offset을 계산하고 VisionUnit Config의 CalibrationData.Camera에 저장합니다.\r\n" +
                "   - 저장 후 Offset valid=True 상태인지 확인합니다.\r\n\r\n" +
                "7. SAVE / APPLY\r\n" +
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
            ManualCalibrationReadinessTarget readinessTarget = ManualCalibrationReadinessTarget.None,
            Action onSuccess = null)
        {
            if (_busy)
                return;
            if (_manualDraft != null && actionName != "RETICLE BACK")
            {
                lblStatus.Text = "미저장 편집이 있습니다. SAVE / APPLY 또는 LOAD 후 측정하세요.";
                return;
            }

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

                if (result == 0 && onSuccess != null)
                    onSuccess();

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
            bool rearForward = vision.IsVisionReticleRearSideForward();
            bool frontForward = rearForward;
            if (up && rearForward)
                return true;

            reason = "Bottom 촬영 준비 상태가 아닙니다. Reticle 상태를 확인하세요. 필요상태=Lift Up, Rear Slide 전진(Front Slide는 Rear Forward 기준 확인), 현재 up=" +
                     up + ", frontForwardByRear=" + frontForward + ", rearForward=" + rearForward + ", rawFrontForward=" + vision.IsVisionReticleFrontSideForward();
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
                    data = _manualDraft ?? (_sequence != null ? _sequence.CalibrationData : null);
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
                    (_manualDraft != null ? "[미저장 편집] " : "") + "Offset: Bottom-Input=(" + data.InputToBottomOffsetX.ToString("F6") + ", " + data.InputToBottomOffsetY.ToString("F6") + ") mm" +
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
                    data = _manualDraft ?? (_sequence != null ? _sequence.CalibrationData : null);
                    if (data != null)
                        data.EnsureObjects();
                }
                catch
                {
                    data = null;
                }

                if (data == null)
                {
                    AddAppliedValueRow(BottomPixelXRow, "-");
                    AddAppliedValueRow(BottomPixelYRow, "-");
                    AddAppliedValueRow(InputPixelXRow, "-");
                    AddAppliedValueRow(InputPixelYRow, "-");
                    AddAppliedValueRow(OutputPixelXRow, "-");
                    AddAppliedValueRow(OutputPixelYRow, "-");
                    AddAppliedValueRow(InputVisionXEncoderRow, "-");
                    AddAppliedValueRow(OutputVisionXEncoderRow, "-");
                    AddAppliedValueRow(BottomInputOffsetXRow, "-");
                    AddAppliedValueRow(BottomInputOffsetYRow, "-");
                    AddAppliedValueRow(BottomOutputOffsetXRow, "-");
                    AddAppliedValueRow(BottomOutputOffsetYRow, "-");
                    return;
                }

                AddAppliedValueRow(BottomPixelXRow, FormatPixelValue(data.BottomReticle, true));
                AddAppliedValueRow(BottomPixelYRow, FormatPixelValue(data.BottomReticle, false));
                AddAppliedValueRow(InputPixelXRow, FormatPixelValue(data.InputReticle, true));
                AddAppliedValueRow(InputPixelYRow, FormatPixelValue(data.InputReticle, false));
                AddAppliedValueRow(OutputPixelXRow, FormatPixelValue(data.OutputReticle, true));
                AddAppliedValueRow(OutputPixelYRow, FormatPixelValue(data.OutputReticle, false));
                AddAppliedValueRow(InputVisionXEncoderRow, FormatVisionXPosition(data.InputReticle));
                AddAppliedValueRow(OutputVisionXEncoderRow, FormatVisionXPosition(data.OutputReticle));
                AddAppliedValueRow(BottomInputOffsetXRow, FormatOffsetValue(data.InputToBottomOffsetX));
                AddAppliedValueRow(BottomInputOffsetYRow, FormatOffsetValue(data.InputToBottomOffsetY));
                AddAppliedValueRow(BottomOutputOffsetXRow, FormatOffsetValue(data.OutputToBottomOffsetX));
                AddAppliedValueRow(BottomOutputOffsetYRow, FormatOffsetValue(data.OutputToBottomOffsetY));
                AddAppliedValueRow(MotionSpeedRow, data.Motion.MoveVelocity.ToString("F6") + " mm/s", MotionSpeedRow);
                AddAppliedValueRow(MotionAccRow, data.Motion.MoveAcceleration.ToString("F6") + " mm/s2", MotionAccRow);
                AddAppliedValueRow(MotionDecRow, data.Motion.MoveDeceleration.ToString("F6") + " mm/s2", MotionDecRow);
                AddAppliedValueRow(MotionTimeoutRow, data.Motion.MoveTimeoutMs.ToString(CultureInfo.InvariantCulture) + " ms", MotionTimeoutRow);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "VISION-CAMERA-CAL-VALUE-GRID", "Vision Camera Calibration 적용값 표시 실패: " + ex.Message);
            }
            finally
            {
                UpdateAppliedValueEditorAccess();
            }
        }

        private void AddAppliedValueRow(string item, string value)
        {
            AddAppliedValueRow(item, value, null);
        }

        private void AddAppliedValueRow(string item, string value, string tag)
        {
            int row = gridAppliedValues.Rows.Add(item, string.Empty, string.Empty, value);
            gridAppliedValues.Rows[row].Tag = tag;
            ApplyAppliedValueEditState(gridAppliedValues.Rows[row]);
            ApplyAppliedValueToolTip(gridAppliedValues.Rows[row], GetAppliedValueToolTip(item));
        }

        private void gridAppliedValues_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            string item = Convert.ToString(gridAppliedValues.Rows[e.RowIndex].Cells[colValueName.Index].Value, CultureInfo.InvariantCulture);
            e.ToolTipText = GetAppliedValueToolTip(item);
        }

        private static void ApplyAppliedValueToolTip(DataGridViewRow row, string toolTipText)
        {
            if (row == null || string.IsNullOrWhiteSpace(toolTipText))
                return;

            foreach (DataGridViewCell cell in row.Cells)
                cell.ToolTipText = toolTipText;
        }

        private static string GetAppliedValueToolTip(string item)
        {
            switch (item)
            {
                case BottomPixelXRow:
                case BottomPixelYRow:
                    return "Bottom 카메라에서 Reticle Finder로 측정한 픽셀 좌표입니다. 카메라 간 Offset 계산의 기준이 됩니다.";
                case InputPixelXRow:
                case InputPixelYRow:
                    return "Input 카메라에서 Reticle Finder로 측정한 픽셀 좌표입니다. Bottom 기준 Input Camera Offset 계산에 사용합니다.";
                case OutputPixelXRow:
                case OutputPixelYRow:
                    return "Output 카메라에서 Reticle Finder로 측정한 픽셀 좌표입니다. Bottom 기준 Output Camera Offset 계산에 사용합니다.";
                case InputVisionXEncoderRow:
                    return "Input 카메라 Reticle 촬영 시 VisionX 실제 Encoder 위치입니다. 이번에 측정하거나 직접 수정한 경우 SAVE / APPLY로 현재 Recipe에 저장합니다.";
                case OutputVisionXEncoderRow:
                    return "Output 카메라 Reticle 촬영 시 VisionX 실제 Encoder 위치입니다. 이번에 측정하거나 직접 수정한 경우 SAVE / APPLY로 현재 Recipe에 저장합니다.";
                case BottomInputOffsetXRow:
                case BottomInputOffsetYRow:
                    return "측정 산식과 수동 보정을 합친 Input 최종 Offset입니다. 수정 후 SAVE / APPLY로 저장하며 CALC 후에도 수동 보정이 유지됩니다.";
                case BottomOutputOffsetXRow:
                case BottomOutputOffsetYRow:
                    return "측정 산식과 수동 보정을 합친 Output 최종 Offset입니다. 수정 후 SAVE / APPLY로 저장하며 CALC 후에도 수동 보정이 유지됩니다.";
                case MotionSpeedRow:
                    return "Vision Camera Calibration에서 Reticle 촬영 위치로 이동할 때 사용하는 전용 속도입니다. 더블클릭하면 키패드로 수정합니다.";
                case MotionAccRow:
                    return "Vision Camera Calibration 전용 이동 가속도입니다. 더블클릭하면 키패드로 수정합니다.";
                case MotionDecRow:
                    return "Vision Camera Calibration 전용 이동 감속도입니다. 더블클릭하면 키패드로 수정합니다.";
                case MotionTimeoutRow:
                    return "Reticle 촬영 위치 이동 완료를 기다리는 최대 시간입니다. 더블클릭하면 키패드로 수정합니다.";
                default:
                    return string.Empty;
            }
        }

        private void UserSession_UserChanged()
        {
            try
            {
                if (IsDisposed)
                    return;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UserSession_UserChanged));
                    return;
                }

                RefreshAppliedValueGrid();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void UpdateAppliedValueEditorAccess()
        {
            try
            {
                if (gridAppliedValues == null)
                    return;

                gridAppliedValues.ReadOnly = true;
                gridAppliedValues.EditMode = DataGridViewEditMode.EditProgrammatically;
                gridAppliedValues.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                colValueName.ReadOnly = true;
                colSavedValue.ReadOnly = true;
                colCurrentValue.ReadOnly = true;
                colApplyValue.ReadOnly = true;

                foreach (DataGridViewRow row in gridAppliedValues.Rows)
                    ApplyAppliedValueEditState(row);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyAppliedValueEditState(DataGridViewRow row)
        {
            if (row == null)
                return;

            string item = Convert.ToString(row.Cells[colValueName.Index].Value, CultureInfo.InvariantCulture);
            bool editable = CanEditAppliedValues() && IsAppliedValueEditableItem(item);

            foreach (DataGridViewCell cell in row.Cells)
                cell.ReadOnly = true;

            DataGridViewCell valueCell = row.Cells[colApplyValue.Index];
            valueCell.ReadOnly = true;
            valueCell.Style.BackColor = editable ? Color.FromArgb(255, 255, 230) : Color.FromArgb(245, 245, 245);
            valueCell.Style.ForeColor = editable ? Color.Black : Color.DimGray;
        }

        private static bool CanEditAppliedValues()
        {
            return UserSession.Has(UserLevel.Admin);
        }

        private static bool IsAppliedValueEditableItem(string item)
        {
            return item == BottomPixelXRow ||
                   item == BottomPixelYRow ||
                   item == InputPixelXRow ||
                   item == InputPixelYRow ||
                   item == OutputPixelXRow ||
                   item == OutputPixelYRow ||
                   item == InputVisionXEncoderRow ||
                   item == OutputVisionXEncoderRow ||
                   item == BottomInputOffsetXRow ||
                   item == BottomInputOffsetYRow ||
                   item == BottomOutputOffsetXRow ||
                   item == BottomOutputOffsetYRow ||
                   item == MotionSpeedRow ||
                   item == MotionAccRow ||
                   item == MotionDecRow ||
                   item == MotionTimeoutRow;
        }

        private static bool IsReticlePixelItem(string item)
        {
            return item == BottomPixelXRow ||
                   item == BottomPixelYRow ||
                   item == InputPixelXRow ||
                   item == InputPixelYRow ||
                   item == OutputPixelXRow ||
                   item == OutputPixelYRow;
        }

        private static bool IsCameraCalibrationGeometryItem(string item)
        {
            return item == BottomPixelXRow ||
                   item == BottomPixelYRow ||
                   item == InputPixelXRow ||
                   item == InputPixelYRow ||
                   item == OutputPixelXRow ||
                   item == OutputPixelYRow ||
                   item == InputVisionXEncoderRow ||
                   item == OutputVisionXEncoderRow ||
                   item == BottomInputOffsetXRow ||
                   item == BottomInputOffsetYRow ||
                   item == BottomOutputOffsetXRow ||
                   item == BottomOutputOffsetYRow;
        }

        private void ApplyManualAppliedValue(string item, string valueText)
        {
            VisionCameraCalibrationData data = CreateManualEditCandidate();
            if (data == null)
                throw new InvalidOperationException("Vision Camera Calibration 데이터가 준비되지 않았습니다.");

            data.EnsureObjects();

            if (item == BottomPixelXRow)
            {
                ApplyManualReticlePixelAxis(data.BottomReticle, data.BottomCamera, "Bottom", valueText, true);
                data.Valid = false;
            }
            else if (item == BottomPixelYRow)
            {
                ApplyManualReticlePixelAxis(data.BottomReticle, data.BottomCamera, "Bottom", valueText, false);
                data.Valid = false;
            }
            else if (item == InputPixelXRow)
            {
                ApplyManualReticlePixelAxis(data.InputReticle, data.InputCamera, "Input", valueText, true);
                data.Valid = false;
            }
            else if (item == InputPixelYRow)
            {
                ApplyManualReticlePixelAxis(data.InputReticle, data.InputCamera, "Input", valueText, false);
                data.Valid = false;
            }
            else if (item == OutputPixelXRow)
            {
                ApplyManualReticlePixelAxis(data.OutputReticle, data.OutputCamera, "Output", valueText, true);
                data.Valid = false;
            }
            else if (item == OutputPixelYRow)
            {
                ApplyManualReticlePixelAxis(data.OutputReticle, data.OutputCamera, "Output", valueText, false);
                data.Valid = false;
            }
            else if (item == InputVisionXEncoderRow)
            {
                ApplyManualVisionXPosition(data.InputReticle, "Input", valueText);
            }
            else if (item == OutputVisionXEncoderRow)
            {
                ApplyManualVisionXPosition(data.OutputReticle, "Output", valueText);
            }
            else if (item == BottomInputOffsetXRow)
            {
                ValidateManualOffsetBasis(data);
                data.InputToBottomOffsetX = ReadDoubleValue(item, valueText);
                data.InputToBottomManualCorrectionX = data.InputToBottomOffsetX - data.ResolveInputBridgeX();
                if (!IsFinite(data.InputToBottomManualCorrectionX))
                    throw new FormatException(item + " 보정량이 숫자 범위를 벗어났습니다.");
            }
            else if (item == BottomInputOffsetYRow)
            {
                ValidateManualOffsetBasis(data);
                data.InputToBottomOffsetY = ReadDoubleValue(item, valueText);
                data.InputToBottomManualCorrectionY = data.InputToBottomOffsetY - data.ResolveInputBridgeY();
                if (!IsFinite(data.InputToBottomManualCorrectionY))
                    throw new FormatException(item + " 보정량이 숫자 범위를 벗어났습니다.");
            }
            else if (item == BottomOutputOffsetXRow)
            {
                ValidateManualOffsetBasis(data);
                data.OutputToBottomOffsetX = ReadDoubleValue(item, valueText);
                data.OutputToBottomManualCorrectionX = data.OutputToBottomOffsetX - data.ResolveOutputBridgeX();
                if (!IsFinite(data.OutputToBottomManualCorrectionX))
                    throw new FormatException(item + " 보정량이 숫자 범위를 벗어났습니다.");
            }
            else if (item == BottomOutputOffsetYRow)
            {
                ValidateManualOffsetBasis(data);
                data.OutputToBottomOffsetY = ReadDoubleValue(item, valueText);
                data.OutputToBottomManualCorrectionY = data.OutputToBottomOffsetY - data.ResolveOutputBridgeY();
                if (!IsFinite(data.OutputToBottomManualCorrectionY))
                    throw new FormatException(item + " 보정량이 숫자 범위를 벗어났습니다.");
            }
            else if (item == MotionSpeedRow)
            {
                double value = ReadDoubleValue(item, valueText);
                if (value < 0.001) throw new FormatException(item + " 값은 0.001 이상이어야 합니다.");
                data.Motion.MoveVelocity = value;
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionAccRow)
            {
                double value = ReadDoubleValue(item, valueText);
                if (value < 0.001) throw new FormatException(item + " 값은 0.001 이상이어야 합니다.");
                data.Motion.MoveAcceleration = value;
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionDecRow)
            {
                double value = ReadDoubleValue(item, valueText);
                if (value < 0.001) throw new FormatException(item + " 값은 0.001 이상이어야 합니다.");
                data.Motion.MoveDeceleration = value;
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionTimeoutRow)
            {
                double value = ReadDoubleValue(item, valueText);
                if (value < 100 || value > int.MaxValue || value != Math.Truncate(value))
                    throw new FormatException(item + " 값은 100 이상인 정수여야 합니다.");
                data.Motion.MoveTimeoutMs = (int)value;
                data.Motion.EnsureDefaults();
            }
            else
            {
                throw new InvalidOperationException("수정할 수 없는 항목입니다. item=" + item);
            }

            if ((IsReticlePixelItem(item) || item == BottomInputOffsetXRow || item == BottomInputOffsetYRow ||
                 item == BottomOutputOffsetXRow || item == BottomOutputOffsetYRow) && data.CanCalculate &&
                !data.Calculate(UserSession.Name))
                throw new FormatException("수정 후 Offset 계산값이 유효하지 않습니다.");
            MarkManualCameraCalibrationUpdate(data);
            _manualDraft = data;
            _manualEditedItems.Add(item);
            UpdateSaveReticleButtonEnabled();
        }

        private void ApplyManualReticlePixelAxis(
            VisionReticleMeasurement measurement,
            VisionCameraPixelCalibration camera,
            string cameraName,
            string valueText,
            bool isXAxis)
        {
            if (measurement == null || camera == null)
                throw new InvalidOperationException(cameraName + " Reticle 데이터가 준비되지 않았습니다.");

            string axisName = isXAxis ? " Pixel X" : " Pixel Y";
            double pixelValue = ReadDoubleValue(cameraName + axisName, valueText);
            double size = isXAxis ? camera.ImageWidthPixel : camera.ImageHeightPixel;
            if (!IsFinite(size) || size <= 0 || pixelValue < 0 || pixelValue >= size)
                throw new FormatException(cameraName + axisName + " 값이 카메라 이미지 범위를 벗어났습니다.");

            measurement.Valid = true;
            measurement.CameraName = cameraName;
            if (isXAxis)
                measurement.PixelX = pixelValue;
            else
                measurement.PixelY = pixelValue;

            measurement.MmX = camera.PixelToMmOffsetX(measurement.PixelX);
            measurement.MmY = camera.PixelToMmOffsetY(measurement.PixelY);
            if (measurement.Score <= 0.0)
                measurement.Score = 1.0;
            measurement.MeasuredAt = DateTime.Now;
            measurement.Raw = "ADMIN:ManualEdit";
        }

        private void ApplyManualVisionXPosition(
            VisionReticleMeasurement measurement,
            string cameraName,
            string valueText)
        {
            if (measurement == null)
                throw new InvalidOperationException(cameraName + " Reticle 데이터가 준비되지 않았습니다.");

            double position = ReadDoubleValue(cameraName + " VisionX Encoder", valueText);
            measurement.CameraName = cameraName;
            measurement.VisionXPosition = position;
            measurement.HasVisionXPosition = true;
            measurement.MeasuredAt = DateTime.Now;
            measurement.Raw = "ADMIN:ManualEdit";
        }

        private void MarkManualCameraCalibrationUpdate(VisionCameraCalibrationData data)
        {
            if (data == null)
                return;

            data.UpdatedAt = DateTime.Now;
            data.UpdatedBy = UserSession.Name ?? string.Empty;
            data.EnsureSerializableDateTimes();
        }

        private static double ReadDoubleValue(string item, string text)
        {
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !IsFinite(value))
                throw new FormatException(item + " 값은 유효한 숫자 하나를 입력해야 합니다.");
            return value;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void gridAppliedValues_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0)
                return;

            DataGridViewRow row = gridAppliedValues.Rows[e.RowIndex];
            string item = Convert.ToString(row != null ? row.Cells[colValueName.Index].Value : null, CultureInfo.InvariantCulture);
            if (!IsAppliedValueEditableItem(item))
                return;

            if (!CanEditAppliedValues())
            {
                lblStatus.Text = "Admin 권한에서만 Vision Camera Calibration 값을 수정할 수 있습니다.";
                return;
            }

            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason)) throw new InvalidOperationException(reason);
                Form1 editHost = FindHostForm();
                CDT320_Machine editMachine = editHost?.Machine;
                string editRecipe = editHost?.ActiveRecipeName;
                VisionCameraCalibrationData editSource = Sequence.CalibrationData;
                string editSnapshot = Snapshot(editSource);
                ValidateManualEditContext(editHost);
                string valueText;
                if (!PromptAppliedValueWithKeypad(item, out valueText))
                    return;

                if (!CanRunManualCalibration(out reason)) throw new InvalidOperationException(reason);
                if (!ReferenceEquals(editMachine, editHost?.Machine) ||
                    !string.Equals(editRecipe, editHost?.ActiveRecipeName, StringComparison.Ordinal) ||
                    !ReferenceEquals(editSource, Sequence.CalibrationData) || editSnapshot != Snapshot(Sequence.CalibrationData))
                    throw new InvalidOperationException("키패드 입력 중 Recipe/카메라가 변경되었습니다. 값을 다시 확인하고 입력하세요.");
                ApplyManualAppliedValue(item, valueText);
                RefreshData();

                lblStatus.Text = item + " 수정값은 미저장 상태입니다. SAVE / APPLY로 저장·적용하세요. LOAD는 편집을 취소합니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = item + " 키패드 수정 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-KEYPAD-SAVE", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshAppliedValueGrid();
            }
            finally
            {
            }
        }

        private bool PromptAppliedValueWithKeypad(string item, out string valueText)
        {
            valueText = null;

            VisionCameraCalibrationData data = _manualDraft ?? Sequence.CalibrationData;
            if (data == null)
                throw new InvalidOperationException("Vision Camera Calibration 데이터가 준비되지 않았습니다.");

            data.EnsureObjects();

            double value;
            if (item == BottomPixelXRow)
            {
                if (!PromptSingleWithKeypad(item, data.BottomReticle.PixelX, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == BottomPixelYRow)
            {
                if (!PromptSingleWithKeypad(item, data.BottomReticle.PixelY, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == InputPixelXRow)
            {
                if (!PromptSingleWithKeypad(item, data.InputReticle.PixelX, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == InputPixelYRow)
            {
                if (!PromptSingleWithKeypad(item, data.InputReticle.PixelY, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == OutputPixelXRow)
            {
                if (!PromptSingleWithKeypad(item, data.OutputReticle.PixelX, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == OutputPixelYRow)
            {
                if (!PromptSingleWithKeypad(item, data.OutputReticle.PixelY, "px", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == InputVisionXEncoderRow)
            {
                if (!PromptSingleWithKeypad(item, data.InputReticle.HasVisionXPosition ? data.InputReticle.VisionXPosition : 0.0, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == OutputVisionXEncoderRow)
            {
                if (!PromptSingleWithKeypad(item, data.OutputReticle.HasVisionXPosition ? data.OutputReticle.VisionXPosition : 0.0, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == BottomInputOffsetXRow)
            {
                if (!PromptSingleWithKeypad(item, data.InputToBottomOffsetX, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == BottomInputOffsetYRow)
            {
                if (!PromptSingleWithKeypad(item, data.InputToBottomOffsetY, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == BottomOutputOffsetXRow)
            {
                if (!PromptSingleWithKeypad(item, data.OutputToBottomOffsetX, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == BottomOutputOffsetYRow)
            {
                if (!PromptSingleWithKeypad(item, data.OutputToBottomOffsetY, "mm", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == MotionSpeedRow)
            {
                if (!PromptSingleWithKeypad(item, data.Motion.MoveVelocity, "mm/s", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == MotionAccRow)
            {
                if (!PromptSingleWithKeypad(item, data.Motion.MoveAcceleration, "mm/s2", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == MotionDecRow)
            {
                if (!PromptSingleWithKeypad(item, data.Motion.MoveDeceleration, "mm/s2", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            if (item == MotionTimeoutRow)
            {
                if (!PromptSingleWithKeypad(item, data.Motion.MoveTimeoutMs, "ms", out value))
                    return false;

                valueText = FormatManualNumber(value);
                return true;
            }

            return false;
        }

        private bool PromptSingleWithKeypad(string title, double currentValue, string unit, out double value)
        {
            value = currentValue;
            using (NumericKeypadDialog dialog = new NumericKeypadDialog(title, FormatManualNumber(currentValue), unit, true))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                value = ReadDoubleValue(title, dialog.ValueText);
                return true;
            }
        }

        private static string FormatManualNumber(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private string FormatPixelValue(VisionReticleMeasurement measurement, bool isXAxis)
        {
            if (measurement == null || !measurement.Valid)
                return "-";

            return (isXAxis ? measurement.PixelX : measurement.PixelY).ToString("F3") + " px";
        }

        private string FormatOffsetValue(double value)
        {
            return value.ToString("F6") + " mm";
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
                gridMeasurements.Rows.Add(name, "-", "-", "-", "-", "-", "-", "-");
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
                measurement.PixelX.ToString("F3"),
                measurement.PixelY.ToString("F3"),
                measurement.MmX.ToString("F6"),
                measurement.MmY.ToString("F6"),
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

        /// <summary>
        /// FIND INPUT / FIND OUTPUT이 성공했을 때 실제 측정 사실을 기록한다.
        /// 기록 시점의 값과 Recipe를 함께 보관해 SAVE / APPLY 시점에 대조한다.
        /// </summary>
        private void MarkReticleMeasuredInSession(bool input, bool output)
        {
            VisionCameraCalibrationData data = Sequence != null ? Sequence.CalibrationData : null;
            if (data == null)
                return;

            string recipeName = FindHostForm()?.ActiveRecipeName;
            if (!ReferenceEquals(_measuredCameraSource, data) ||
                !string.Equals(_measuredRecipeName, recipeName, StringComparison.Ordinal))
                ClearReticleMeasuredInSession();
            data.EnsureObjects();
            if (input && data.InputReticle != null && data.InputReticle.HasVisionXPosition)
            {
                _inputReticleMeasuredInSession = true;
                _measuredInputVisionX = data.InputReticle.VisionXPosition;
            }

            if (output && data.OutputReticle != null && data.OutputReticle.HasVisionXPosition)
            {
                _outputReticleMeasuredInSession = true;
                _measuredOutputVisionX = data.OutputReticle.VisionXPosition;
            }

            _measuredCameraSource = data;
            _measuredRecipeName = recipeName;
            UpdateSaveReticleButtonEnabled();
        }

        /// <summary>LOAD 등으로 데이터가 다시 로드되면 세션 측정 기록을 폐기한다.</summary>
        private void ClearReticleMeasuredInSession()
        {
            _inputReticleMeasuredInSession = false;
            _outputReticleMeasuredInSession = false;
            _measuredInputVisionX = 0.0;
            _measuredOutputVisionX = 0.0;
            _measuredCameraSource = null;
            _measuredRecipeName = null;
            UpdateSaveReticleButtonEnabled();
        }

        private bool HasCameraValuesToSave()
        {
            return _inputReticleMeasuredInSession || _outputReticleMeasuredInSession || _manualDraft != null;
        }

        private void UpdateSaveReticleButtonEnabled()
        {
            try
            {
                btnSaveReticleValues.Enabled = !_busy && HasCameraValuesToSave();
            }
            catch
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnLoadValues.Enabled = enabled;
            // 측정한 위치 또는 명시적으로 편집한 값이 있을 때 저장할 수 있습니다.
            btnSaveReticleValues.Enabled = enabled && HasCameraValuesToSave();
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
