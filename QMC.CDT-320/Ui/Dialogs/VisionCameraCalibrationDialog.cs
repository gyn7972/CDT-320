using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
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

        // [측정 가드 2026-07-27] SAVE POS는 "이번 다이얼로그 세션에서 실제로 측정/입력한 값"만 저장한다.
        // 기존에는 data.InputReticle.HasVisionXPosition 만 확인했는데, 이 플래그는 파일에서 로드된
        // 과거 값에도 true라서 FIND INPUT/OUTPUT을 한 번도 하지 않고 SAVE POS를 눌러도 통과했고,
        // 그 결과 과거 VisionX 값이 현재 Recipe의 ReticlePosition을 덮어썼다.
        // 세션 내 성공 시점의 값을 함께 보관해, 이후 LOAD/재측정으로 값이 바뀌면 저장을 차단한다.
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
                lblStatus.Text = "대기 중입니다.";

                toolTip.SetToolTip(btnLoadValues, "저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 읽어 표시합니다.");
                toolTip.SetToolTip(btnSaveReticleValues, "FIND INPUT/OUTPUT에서 측정한 InputVisionX/OutputVisionX 위치를 ReticlePosition으로 저장합니다.\r\nBottom 위 Reticle 촬영 X Encoder 값도 VisionUnit Config에 함께 저장합니다.");
                toolTip.SetToolTip(gridAppliedValues, "Admin 권한에서 값을 더블클릭하면 키패드로 수정하고 VisionUnit CalibrationData에 저장합니다.");
                toolTip.SetToolTip(btnCheck, "자동 운전, 다른 수동 동작, 알람 상태를 확인합니다.\r\n측정 버튼을 누르기 전에 현재 장비 상태가 안전한지 확인합니다.");
                toolTip.SetToolTip(btnRunAll, "사전 준비 후 Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 Rear Back -> Lift Down으로 복귀한 뒤 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동합니다. Front Slide는 Rear Back 기준으로 확인합니다.");
                toolTip.SetToolTip(btnFindBottom, "Bottom Vision에 ReticleFinder 실행을 요청합니다.\r\n성공하면 X/Y/T/Score를 VisionUnit Config의 Bottom 측정값으로 저장합니다.");
                toolTip.SetToolTip(btnFindInput, "Input Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Output-side Avoid로 안전 순차 이동한 뒤 InputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnFindOutput, "Output Vision에 ReticleFinder 실행을 요청합니다.\r\nPicker 이동 전 Reticle을 안전 위치로 복귀하고 Front/Rear Picker를 Input-side Avoid로 안전 순차 이동한 뒤 OutputVisionX를 Reticle 위치로 이동합니다.");
                toolTip.SetToolTip(btnRetractReticle, "Reticle을 촬영 준비 위치에서 복귀합니다.\r\nRear Slide 후진, Lift Down 순서로 실행하고 최종 위치를 확인합니다. Front Slide는 Rear Back 기준으로 확인합니다.");
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
                ClearReticleMeasuredInSession();
                RefreshData();
                lblStatus.Text = "저장 파일에서 Machine Settings와 현재 Recipe 값을 다시 불러왔습니다. " +
                                 "SAVE POS를 하려면 FIND INPUT/OUTPUT을 다시 수행하세요.";
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
                    !data.InputReticle.HasVisionXPosition)
                    throw new InvalidOperationException("Input Reticle VisionX 위치가 없습니다. FIND INPUT을 수행하거나 Admin 수동 입력 후 저장하세요.");

                if (data.OutputReticle == null ||
                    !data.OutputReticle.HasVisionXPosition)
                    throw new InvalidOperationException("Output Reticle VisionX 위치가 없습니다. FIND OUTPUT을 수행하거나 Admin 수동 입력 후 저장하세요.");

                // [측정 가드 2026-07-27] HasVisionXPosition은 파일 로드값에도 true이므로 신뢰할 수 없다.
                // 이번 세션에서 실제로 측정/입력했는지, 그리고 그 값이 이후 바뀌지 않았는지까지 확인한다.
                if (!_inputReticleMeasuredInSession)
                    throw new InvalidOperationException(
                        "이번 세션에서 Input Reticle을 측정하지 않았습니다. FIND INPUT을 먼저 정상 완료하세요. " +
                        "(과거 저장값으로 Recipe ReticlePosition을 덮어쓰지 않도록 차단합니다.)");

                if (!_outputReticleMeasuredInSession)
                    throw new InvalidOperationException(
                        "이번 세션에서 Output Reticle을 측정하지 않았습니다. FIND OUTPUT을 먼저 정상 완료하세요. " +
                        "(과거 저장값으로 Recipe ReticlePosition을 덮어쓰지 않도록 차단합니다.)");

                const double ReticlePositionTolerance = 1e-6;
                if (Math.Abs(data.InputReticle.VisionXPosition - _measuredInputVisionX) > ReticlePositionTolerance ||
                    Math.Abs(data.OutputReticle.VisionXPosition - _measuredOutputVisionX) > ReticlePositionTolerance)
                {
                    ClearReticleMeasuredInSession();
                    throw new InvalidOperationException(
                        "마지막 측정 이후 Reticle VisionX 값이 변경되었거나 다시 로드되었습니다. " +
                        "잘못된 값 저장을 막기 위해 SAVE POS를 차단합니다. FIND INPUT/OUTPUT을 다시 수행하세요.");
                }

                double inputX = data.InputReticle.VisionXPosition;
                double outputX = data.OutputReticle.VisionXPosition;

                host.Machine.InputStageUnit.Recipe.VisionX.ReticlePosition = inputX;
                host.Machine.OutputStageUnit.Recipe.VisionX.ReticlePosition = outputX;

                data.InputReticle.VisionXPosition = inputX;
                data.InputReticle.HasVisionXPosition = true;
                data.OutputReticle.VisionXPosition = outputX;
                data.OutputReticle.HasVisionXPosition = true;
                host.Machine.VisionUnit.Config.CalibrationData.Touch(UserSession.Name);

                string recipeName = host.ActiveRecipeName;
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
            ManualCalibrationReadinessTarget readinessTarget = ManualCalibrationReadinessTarget.None,
            Action onSuccess = null)
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
                    return "Input 카메라 Reticle 촬영 시 VisionX 실제 Encoder 위치입니다. SAVE RETICLE VALUES로 ReticlePosition에 저장됩니다.";
                case OutputVisionXEncoderRow:
                    return "Output 카메라 Reticle 촬영 시 VisionX 실제 Encoder 위치입니다. SAVE RETICLE VALUES로 ReticlePosition에 저장됩니다.";
                case BottomInputOffsetXRow:
                case BottomInputOffsetYRow:
                    return "Bottom 카메라 좌표계를 기준으로 계산된 Input 카메라 보정값입니다.";
                case BottomOutputOffsetXRow:
                case BottomOutputOffsetYRow:
                    return "Bottom 카메라 좌표계를 기준으로 계산된 Output 카메라 보정값입니다.";
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
            VisionCameraCalibrationData data = Sequence.CalibrationData;
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
                data.InputToBottomOffsetX = ReadDoubleValue(item, valueText);
                data.Valid = true;
            }
            else if (item == BottomInputOffsetYRow)
            {
                data.InputToBottomOffsetY = ReadDoubleValue(item, valueText);
                data.Valid = true;
            }
            else if (item == BottomOutputOffsetXRow)
            {
                data.OutputToBottomOffsetX = ReadDoubleValue(item, valueText);
                data.Valid = true;
            }
            else if (item == BottomOutputOffsetYRow)
            {
                data.OutputToBottomOffsetY = ReadDoubleValue(item, valueText);
                data.Valid = true;
            }
            else if (item == MotionSpeedRow)
            {
                data.Motion.MoveVelocity = Math.Max(0.001, ReadDoubleValue(item, valueText));
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionAccRow)
            {
                data.Motion.MoveAcceleration = Math.Max(0.001, ReadDoubleValue(item, valueText));
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionDecRow)
            {
                data.Motion.MoveDeceleration = Math.Max(0.001, ReadDoubleValue(item, valueText));
                data.Motion.EnsureDefaults();
            }
            else if (item == MotionTimeoutRow)
            {
                data.Motion.MoveTimeoutMs = Math.Max(100, (int)Math.Round(ReadDoubleValue(item, valueText)));
                data.Motion.EnsureDefaults();
            }
            else
            {
                throw new InvalidOperationException("수정할 수 없는 항목입니다. item=" + item);
            }

            MarkManualCameraCalibrationUpdate(data);
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

        private void SaveManualAppliedValue(string item)
        {
            Form1 host = FindHostForm();
            if (host == null || host.Machine == null)
                throw new InvalidOperationException("장비 객체가 준비되지 않았습니다.");

            if (host.Machine.VisionUnit == null ||
                host.Machine.VisionUnit.Config == null ||
                host.Machine.VisionUnit.Config.CalibrationData == null)
                throw new InvalidOperationException("VisionUnit CalibrationData가 준비되지 않았습니다.");

            host.Machine.VisionUnit.Config.CalibrationData.Touch(UserSession.Name);

            if (IsCameraCalibrationGeometryItem(item))
            {
                string offsetSummary;
                PickerVisionOffsetCalibrationService.TryApplyAvailableOffsets(host.Machine, UserSession.Name, out offsetSummary);
                if (!string.IsNullOrWhiteSpace(offsetSummary))
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MANUAL-PICKER-OFFSET", offsetSummary);
            }

            if (!host.Machine.SaveSettings())
                throw new InvalidOperationException("CalibrationData 파일 저장에 실패했습니다.");

            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MANUAL-EDIT",
                "Vision Camera Calibration admin 수동 수정 저장. item=" + item + ", user=" + UserSession.Name);
        }

        private static double ReadDoubleValue(string item, string text)
        {
            double[] values;
            if (!TryReadNumbers(text, 1, out values))
                throw new FormatException(item + " 값은 숫자가 필요합니다.");

            double value = values[0];
            if (!IsFinite(value))
                throw new FormatException(item + " 값이 유효한 숫자가 아닙니다.");

            return value;
        }

        private static bool TryReadNumbers(string text, int minimumCount, out double[] values)
        {
            List<double> parsed = new List<double>();
            MatchCollection matches = Regex.Matches(text ?? string.Empty, @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?");
            foreach (Match match in matches)
            {
                double value;
                if (double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                    double.TryParse(match.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                {
                    parsed.Add(value);
                }
            }

            values = parsed.ToArray();
            return values.Length >= minimumCount;
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
                string valueText;
                if (!PromptAppliedValueWithKeypad(item, out valueText))
                    return;

                ApplyManualAppliedValue(item, valueText);
                SaveManualAppliedValue(item);
                // Admin 수동 입력도 정식 측정 경로로 인정한다(기존 안내 문구와 동일 기준).
                if (item == InputVisionXEncoderRow)
                    MarkReticleMeasuredInSession(true, false);
                else if (item == OutputVisionXEncoderRow)
                    MarkReticleMeasuredInSession(false, true);
                RefreshData();

                string suffix = IsReticlePixelItem(item)
                    ? " Offset 재계산이 필요하면 CALC / SAVE를 실행하세요."
                    : string.Empty;
                lblStatus.Text = item + " 키패드 수정값을 저장했습니다." + suffix;
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

            VisionCameraCalibrationData data = Sequence.CalibrationData;
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
            using (NumericKeypadDialog dialog = new NumericKeypadDialog(title, FormatManualNumber(currentValue), unit))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                value = ReadDoubleValue(title, dialog.ValueText);
                return true;
            }
        }

        private static string FormatManualNumber(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
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
        /// FIND INPUT / FIND OUTPUT 또는 Admin 수동 입력이 성공했을 때 세션 측정 사실을 기록한다.
        /// 기록 시점의 값을 함께 보관해 SAVE POS 시점에 대조한다.
        /// </summary>
        private void MarkReticleMeasuredInSession(bool input, bool output)
        {
            VisionCameraCalibrationData data = Sequence != null ? Sequence.CalibrationData : null;
            if (data == null)
                return;

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

            UpdateSaveReticleButtonEnabled();
        }

        /// <summary>LOAD 등으로 데이터가 다시 로드되면 세션 측정 기록을 폐기한다.</summary>
        private void ClearReticleMeasuredInSession()
        {
            _inputReticleMeasuredInSession = false;
            _outputReticleMeasuredInSession = false;
            _measuredInputVisionX = 0.0;
            _measuredOutputVisionX = 0.0;
            UpdateSaveReticleButtonEnabled();
        }

        private bool HasReticleMeasuredInSession()
        {
            return _inputReticleMeasuredInSession && _outputReticleMeasuredInSession;
        }

        private void UpdateSaveReticleButtonEnabled()
        {
            try
            {
                btnSaveReticleValues.Enabled = !_busy && HasReticleMeasuredInSession();
            }
            catch
            {
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnLoadValues.Enabled = enabled;
            // SAVE POS는 세션 내 Input/Output 측정이 모두 끝난 경우에만 활성화한다.
            btnSaveReticleValues.Enabled = enabled && HasReticleMeasuredInSession();
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
