using QMC.CDT_320.Ui.Localization;
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
                InitializeLanguageBindings();
                Lang.BindReadOnlyCells(gridMeasurements, FormatLocalizedRow, cell => cell.ColumnIndex == colItem.Index);
                Lang.BindReadOnlyCells(gridAppliedValues, FormatLocalizedRow, cell => cell.ColumnIndex == colValueName.Index);
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
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.text", (object)(ex.Message));
            }
            finally
            {
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_manualDraft != null && !_busy && QMC.Common.MessageDialog.Show(this,
                Lang.T("visionUi.visionCameraCalibrationDialog.message.text"), Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"),
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

                Lang.BindKey(lblGuide, "visionUi.visionCameraCalibrationDialog.lblGuide.text");
                Lang.BindKey(lblValueTitle, "visionUi.visionCameraCalibrationDialog.lblValueTitle.text");
                Lang.BindKey(btnSaveReticleValues, "visionUi.visionCameraCalibrationDialog.btnSaveReticleValues.text");
                Lang.BindKey(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state2");

                Lang.BindKey(toolTip, btnLoadValues, "visionUi.visionCameraCalibrationDialog.tooltip.text");
                Lang.BindKey(toolTip, btnSaveReticleValues, "visionUi.visionCameraCalibrationDialog.tooltip.state2");
                Lang.BindKey(toolTip, gridAppliedValues, "visionUi.visionCameraCalibrationDialog.tooltip.state3");
                Lang.BindKey(toolTip, btnCheck, "visionUi.visionCameraCalibrationDialog.tooltip.state4");
                Lang.BindKey(toolTip, btnRunAll, "visionUi.visionCameraCalibrationDialog.tooltip.state5");
                Lang.BindKey(toolTip, btnFindBottom, "visionUi.visionCameraCalibrationDialog.tooltip.state6");
                Lang.BindKey(toolTip, btnFindInput, "visionUi.visionCameraCalibrationDialog.tooltip.state7");
                Lang.BindKey(toolTip, btnFindOutput, "visionUi.visionCameraCalibrationDialog.tooltip.state8");
                Lang.BindKey(toolTip, btnRetractReticle, "visionUi.visionCameraCalibrationDialog.tooltip.state9");
                Lang.BindKey(toolTip, btnCalculateSave, "visionUi.visionCameraCalibrationDialog.tooltip.state10");
                Lang.BindKey(toolTip, btnHelp, "visionUi.visionCameraCalibrationDialog.tooltip.state11");
                Lang.BindKey(toolTip, btnClose, "visionUi.visionCameraCalibrationDialog.tooltip.state12");
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
                    Lang.BindFormat(lblStatus, "visionUi.literal", (object)(reason));
                    QMC.Common.MessageDialog.Show(this, reason, Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                EnsureSequence();
                Lang.BindKey(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state3");
                RefreshData();
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state4", (object)(ex.Message));
                string statusLogText1 = "상태 확인 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-CHECK", statusLogText1);
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
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state5", (object)(ex.Message));
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
                    Lang.BindFormat(lblStatus, "visionUi.literal", (object)(reason));
                    QMC.Common.MessageDialog.Show(this, reason, Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                Lang.BindKey(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state6");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state7", (object)(ex.Message));
                string statusLogText2 = "값 로드 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-LOAD-VALUE", statusLogText2);
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
                Lang.BindKey(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state8");
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
                    Lang.BindFormat(lblStatus, "visionUi.literal", (object)(reason));
                    QMC.Common.MessageDialog.Show(this, reason, Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state9", (object)(actionName));

                int result = await operation(runCts.Token).ConfigureAwait(true);
                RefreshData();

                if (result == 0 && onSuccess != null)
                    onSuccess();

                {
                    if (result == 0)
                        Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state10", (object)(actionName));
                    else
                        Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state11", (object)(actionName), (object)(result));
                }
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state12", (object)(actionName));
                string statusLogText3 = actionName + " 작업이 취소되었습니다.";
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-STOP", statusLogText3);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state13", (object)(actionName), (object)(ex.Message));
                string statusLogText4 = actionName + " 예외 발생: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-RUN", statusLogText4);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    Lang.BindKey(lblOffsets, "visionUi.visionCameraCalibrationDialog.lblOffsets.text");
                    RefreshAppliedValueGrid();
                    return;
                }

                data.EnsureObjects();
                AddMeasurementRow("Bottom", data.BottomReticle);
                AddMeasurementRow("Input", data.InputReticle);
                AddMeasurementRow("Output", data.OutputReticle);

                Lang.BindFormat(lblOffsets, "visionUi.visionCameraCalibrationDialog.lblOffsets.state2", (object)((_manualDraft != null ? "[미저장 편집] " : "")), (object)(data.InputToBottomOffsetX.ToString("F6")), (object)(data.InputToBottomOffsetY.ToString("F6")), (object)(data.OutputToBottomOffsetX.ToString("F6")), (object)(data.OutputToBottomOffsetY.ToString("F6")), (object)(data.Valid));

                RefreshAppliedValueGrid();
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state14", (object)(ex.Message));
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
                    return Lang.T("visionUi.cameraCalibration.tip.1");
                case InputPixelXRow:
                case InputPixelYRow:
                    return Lang.T("visionUi.cameraCalibration.tip.2");
                case OutputPixelXRow:
                case OutputPixelYRow:
                    return Lang.T("visionUi.cameraCalibration.tip.3");
                case InputVisionXEncoderRow:
                    return Lang.T("visionUi.cameraCalibration.tip.4");
                case OutputVisionXEncoderRow:
                    return Lang.T("visionUi.cameraCalibration.tip.5");
                case BottomInputOffsetXRow:
                case BottomInputOffsetYRow:
                    return Lang.T("visionUi.cameraCalibration.tip.6");
                case BottomOutputOffsetXRow:
                case BottomOutputOffsetYRow:
                    return Lang.T("visionUi.cameraCalibration.tip.7");
                case MotionSpeedRow:
                    return Lang.T("visionUi.cameraCalibration.tip.8");
                case MotionAccRow:
                    return Lang.T("visionUi.cameraCalibration.tip.9");
                case MotionDecRow:
                    return Lang.T("visionUi.cameraCalibration.tip.10");
                case MotionTimeoutRow:
                    return Lang.T("visionUi.cameraCalibration.tip.11");
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
                Lang.BindKey(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state15");
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

                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state16", (object)(item));
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "visionUi.visionCameraCalibrationDialog.lblStatus.state17", (object)(item), (object)(ex.Message));
                string statusLogText5 = item + " 키패드 수정 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-CAMERA-CAL-KEYPAD-SAVE", statusLogText5);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("visionUi.visionCameraCalibrationDialog.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        // Translate only the rendered caption; raw row values remain available to editing and save logic.
        private static string FormatLocalizedRow(string value)
        {
            switch (value)
            {
                case "Bottom": return Lang.T("visionUi.visionCameraCalibrationDialog.row.1");
                case "Input": return Lang.T("visionUi.visionCameraCalibrationDialog.row.2");
                case "Output": return Lang.T("visionUi.visionCameraCalibrationDialog.row.3");
                case "Move Speed": return Lang.T("visionUi.visionCameraCalibrationDialog.row.4");
                case "Move Acc": return Lang.T("visionUi.visionCameraCalibrationDialog.row.5");
                case "Move Dec": return Lang.T("visionUi.visionCameraCalibrationDialog.row.6");
                case "Move Timeout": return Lang.T("visionUi.visionCameraCalibrationDialog.row.7");
                case "Bottom Pixel X": return Lang.T("visionUi.visionCameraCalibrationDialog.row.8");
                case "Bottom Pixel Y": return Lang.T("visionUi.visionCameraCalibrationDialog.row.9");
                case "Input Pixel X": return Lang.T("visionUi.visionCameraCalibrationDialog.row.10");
                case "Input Pixel Y": return Lang.T("visionUi.visionCameraCalibrationDialog.row.11");
                case "Output Pixel X": return Lang.T("visionUi.visionCameraCalibrationDialog.row.12");
                case "Output Pixel Y": return Lang.T("visionUi.visionCameraCalibrationDialog.row.13");
                case "Input VisionX Encoder": return Lang.T("visionUi.visionCameraCalibrationDialog.row.14");
                case "Output VisionX Encoder": return Lang.T("visionUi.visionCameraCalibrationDialog.row.15");
                case "Bottom-Input Offset X": return Lang.T("visionUi.visionCameraCalibrationDialog.row.16");
                case "Bottom-Input Offset Y": return Lang.T("visionUi.visionCameraCalibrationDialog.row.17");
                case "Bottom-Output Offset X": return Lang.T("visionUi.visionCameraCalibrationDialog.row.18");
                case "Bottom-Output Offset Y": return Lang.T("visionUi.visionCameraCalibrationDialog.row.19");
                default: return value;
            }
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblTitle, "visionUi.visionCameraCalibrationDialog.message.state2");
            Lang.BindKey(this.colItem, "visionUi.visionCameraCalibrationDialog.colItem.text");
            Lang.BindKey(this.colPixelX, "visionUi.visionCameraCalibrationDialog.colPixelX.text");
            Lang.BindKey(this.colPixelY, "visionUi.visionCameraCalibrationDialog.colPixelY.text");
            Lang.BindKey(this.colMmX, "visionUi.visionCameraCalibrationDialog.colMmX.text");
            Lang.BindKey(this.colMmY, "visionUi.visionCameraCalibrationDialog.colMmY.text");
            Lang.BindKey(this.colAngle, "visionUi.visionCameraCalibrationDialog.colAngle.text");
            Lang.BindKey(this.colAxis, "visionUi.visionCameraCalibrationDialog.colAxis.text");
            Lang.BindKey(this.colScore, "visionUi.visionCameraCalibrationDialog.colScore.text");
            Lang.BindKey(this.lblValueTitle, "visionUi.visionCameraCalibrationDialog.lblValueTitle.text");
            Lang.BindKey(this.colValueName, "visionUi.visionCameraCalibrationDialog.colValueName.text");
            Lang.BindKey(this.colSavedValue, "visionUi.visionCameraCalibrationDialog.colSavedValue.text");
            Lang.BindKey(this.colCurrentValue, "visionUi.visionCameraCalibrationDialog.colCurrentValue.text");
            Lang.BindKey(this.colApplyValue, "visionUi.visionCameraCalibrationDialog.colApplyValue.text");
            Lang.BindKey(this.btnLoadValues, "visionUi.visionCameraCalibrationDialog.btnLoadValues.text");
            Lang.BindKey(this.btnSaveReticleValues, "visionUi.visionCameraCalibrationDialog.btnSaveReticleValues.state2");
            Lang.BindKey(this.lblOffsets, "visionUi.visionCameraCalibrationDialog.lblOffsets.state3");
            Lang.BindKey(this.btnCheck, "visionUi.visionCameraCalibrationDialog.btnCheck.text");
            Lang.BindKey(this.btnRunAll, "visionUi.visionCameraCalibrationDialog.btnRunAll.text");
            Lang.BindKey(this.btnFindBottom, "visionUi.visionCameraCalibrationDialog.btnFindBottom.text");
            Lang.BindKey(this.btnFindInput, "visionUi.visionCameraCalibrationDialog.btnFindInput.text");
            Lang.BindKey(this.btnFindOutput, "visionUi.visionCameraCalibrationDialog.btnFindOutput.text");
            Lang.BindKey(this.btnRetractReticle, "visionUi.visionCameraCalibrationDialog.btnRetractReticle.text");
            Lang.BindKey(this.btnCalculateSave, "visionUi.visionCameraCalibrationDialog.btnCalculateSave.text");
            Lang.BindFormat(this.btnHelp, "visionUi.literal", (object)("?"));
            Lang.BindKey(this.btnClose, "visionUi.visionCameraCalibrationDialog.btnClose.text");
            Lang.BindKey(this, "visionUi.visionCameraCalibrationDialog.Text.text");
        }
    }
}
