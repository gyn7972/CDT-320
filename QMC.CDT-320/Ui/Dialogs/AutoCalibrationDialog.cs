using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class AutoCalibrationDialog : Form
    {
        private bool _busy;
        private bool _loading;
        private CancellationTokenSource _runCts;
        private AutoCalibrationSequence _activeSequence;
        private Action _controllerStopHandler;

        public AutoCalibrationDialog()
        {
            try
            {
                InitializeComponent();
                LoadSettingsToUi();
                UpdateSelectionStyles();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "AUTO-CAL-DLG-INIT",
                    "Auto Calibration 화면 초기화 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this,
                    "Auto Calibration 화면 초기화에 실패했습니다.\r\n" + ex.Message,
                    "AUTO CALIBRATION",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        public static AutoCalibrationDialog Open(Form owner)
        {
            var dialog = new AutoCalibrationDialog();
            dialog.Owner = owner;
            dialog.Show(owner);
            return dialog;
        }

        private void chkCalibration_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_loading)
                    lblStatus.Text = "사용 항목이 변경되었습니다. SAVE를 눌러 저장하세요.";
                UpdateSelectionStyles();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "AUTO-CAL-CHECK-STYLE",
                    "Auto Calibration 선택 표시 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadSettingsToUi();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettingsFromUi(true);
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            await StartAutoCalibrationAsync().ConfigureAwait(true);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            RequestStop("Auto Calibration 다이얼로그 STOP");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AutoCalibrationDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!_busy)
                    return;

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    "Auto Calibration이 실행 중입니다. 정지 요청 후 창을 닫을까요?",
                    "AUTO CALIBRATION",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                RequestStop("Auto Calibration 창 닫기");
                e.Cancel = true;
                lblStatus.Text = "정지 처리 중입니다. 완료 후 창을 닫으세요.";
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                EventLogger.Write(EventKind.Alarm, "UI", "AUTO-CAL-CLOSE",
                    "Auto Calibration 창 종료 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task StartAutoCalibrationAsync()
        {
            if (_busy)
                return;

            Form1 host = null;
            IDisposable actionScope = null;
            CancellationTokenSource runCts = null;
            Action stopHandler = null;

            try
            {
                _busy = true;
                SetControlsEnabled(false);
                lstHistory.Items.Clear();
                progressCalibration.Value = 0;

                string reason;
                if (!CheckRunReady(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "AUTO CALIBRATION",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!SaveSettingsFromUi(false))
                    return;

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                lblStatus.Text = "Picker 제품 유무와 시작 조건을 확인하고 있습니다.";
                if (!await CheckPreconditionsAsync(host).ConfigureAwait(true))
                    return;

                AutoCalibrationSettings settings = ReadSettingsFromUi();
                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                _activeSequence = new AutoCalibrationSequence(context, settings);
                _activeSequence.ProgressChanged += ActiveSequence_ProgressChanged;

                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence,
                    "AutoCalibration");
                runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
                _runCts = runCts;
                stopHandler = delegate { RequestStop("메인 STOP 요청"); };
                _controllerStopHandler = stopHandler;
                host.Controller.StopRequested += stopHandler;

                AppendHistory("Auto Calibration 시작");
                lblStatus.Text = "Auto Calibration을 실행 중입니다.";
                int result = await _activeSequence.RunAsync(runCts.Token).ConfigureAwait(true);
                if (result != 0)
                {
                    lblStatus.Text = "Auto Calibration이 실패했습니다. Alarm/Event Log와 진행 이력을 확인하세요.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "AUTO CALIBRATION",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                lblStatus.Text = "Auto Calibration 전체 작업을 완료했습니다.";
                AppendHistory(lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "AUTO CALIBRATION",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Auto Calibration이 정지 요청으로 중단되었습니다.";
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Event, "CAL", "AUTO-CAL-STOP", lblStatus.Text);
            }
            catch (SequenceStopException ex)
            {
                lblStatus.Text = "Auto Calibration 정지: " + ex.Message;
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Event, "CAL", "AUTO-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Auto Calibration 실행 중 예외가 발생했습니다: " + ex.Message;
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-DLG-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "AUTO CALIBRATION",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_activeSequence != null)
                    _activeSequence.ProgressChanged -= ActiveSequence_ProgressChanged;
                _activeSequence = null;

                if (host != null && host.Controller != null && stopHandler != null)
                    host.Controller.StopRequested -= stopHandler;
                if (ReferenceEquals(_controllerStopHandler, stopHandler))
                    _controllerStopHandler = null;
                if (ReferenceEquals(_runCts, runCts))
                    _runCts = null;

                if (runCts != null)
                    runCts.Dispose();
                if (actionScope != null)
                    actionScope.Dispose();

                _busy = false;
                SetControlsEnabled(true);
            }
        }

        private async Task<bool> CheckPreconditionsAsync(Form1 host)
        {
            if (!await CheckAllPickersEmptyAsync(host).ConfigureAwait(true))
                return false;

            if (chkPickZCal.Checked && !ConfirmEmptyInputWaferPrepared(host))
                return false;

            if (chkPlaceZCal.Checked && !ConfirmOutputStagesReady(host))
                return false;

            return true;
        }

        private async Task<bool> CheckAllPickersEmptyAsync(Form1 host)
        {
            var physicalDetected = new List<string>();
            var materialDetected = new List<string>();

            foreach (VisionFocusPickerSide side in new[]
                     {
                         VisionFocusPickerSide.Front,
                         VisionFocusPickerSide.Rear
                     })
            {
                for (int pickerNo = 4; pickerNo >= 1; pickerNo--)
                {
                    string pickerName = side + " #" + pickerNo;
                    MaterialLocationKind location = side == VisionFocusPickerSide.Front
                        ? MaterialLocationKind.PickerFront
                        : MaterialLocationKind.PickerRear;
                    DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                    if (die != null)
                        materialDetected.Add(pickerName + "=" + (die.DieId ?? "DATA"));

                    if (ShouldBypassPickerFlowCheck(host, side))
                    {
                        AppendHistory(pickerName + " Vacuum/Flow 확인: Simulation/DryRun Bypass");
                        continue;
                    }

                    bool flowDetected = false;
                    try
                    {
                        SetPickerVacuum(host, side, pickerNo, true);
                        await Task.Delay(300).ConfigureAwait(true);
                        flowDetected = IsPickerFlowDetected(host, side, pickerNo);
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = pickerName + " Vacuum/Flow 확인 실패: " + ex.Message;
                        EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-PICKER-FLOW-CHECK", lblStatus.Text);
                        return false;
                    }
                    finally
                    {
                        try
                        {
                            SetPickerVacuum(host, side, pickerNo, false);
                        }
                        catch (Exception ex)
                        {
                            EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-VAC-OFF",
                                pickerName + " Vacuum OFF 정리 실패: " + ex.Message);
                        }
                    }

                    AppendHistory(pickerName + " Vacuum/Flow=" + (flowDetected ? "DETECTED" : "EMPTY"));
                    if (flowDetected)
                        physicalDetected.Add(pickerName);
                }
            }

            if (physicalDetected.Count > 0)
            {
                string message = "다음 Picker에서 Vacuum ON 후 Flow가 감지되었습니다.\r\n" +
                                 string.Join(", ", physicalDetected.ToArray()) +
                                 "\r\n\r\n제품을 제거한 후 START를 다시 누르세요.";
                lblStatus.Text = "Picker에 제품이 감지되어 시작하지 않습니다.";
                QMC.Common.MessageDialog.Show(this, message, "AUTO CALIBRATION",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (materialDetected.Count > 0)
            {
                string message = "실제 Flow는 감지되지 않았지만 다음 Picker에 Material Data가 남아 있습니다.\r\n" +
                                 string.Join("\r\n", materialDetected.ToArray()) +
                                 "\r\n\r\nData를 유지한 상태로 Auto Calibration을 진행할까요?";
                if (QMC.Common.MessageDialog.Show(this, message, "AUTO CALIBRATION",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    lblStatus.Text = "Picker Material Data 확인에서 사용자가 진행을 취소했습니다.";
                    return false;
                }

                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationPickerDataConfirm",
                    "Picker Material Data가 남아 있는 상태로 진행 승인. data=" +
                    string.Join(",", materialDetected.ToArray()) + " - Check");
            }

            return true;
        }

        private bool ConfirmEmptyInputWaferPrepared(Form1 host)
        {
            InputStageUnit stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
            bool ring8 = stage != null && stage.WaferStage8RingCheckSensor != null &&
                         stage.WaferStage8RingCheckSensor.IsOn;
            bool ring12 = stage != null && stage.WaferStage12RingCheckSensor != null &&
                          stage.WaferStage12RingCheckSensor.IsOn;
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            string message =
                "PickZ Calibration 시작 조건을 확인합니다.\r\n\r\n" +
                "Input Stage에 다이가 없는 빈 웨이퍼를 준비했습니까?\r\n\r\n" +
                "8 inch Ring Sensor=" + ring8 + "\r\n" +
                "12 inch Ring Sensor=" + ring12 + "\r\n" +
                "Input Stage Data=" + (wafer != null ? wafer.WaferId : "-") + "\r\n\r\n" +
                "준비가 완료된 경우에만 Yes를 누르세요.";

            if (QMC.Common.MessageDialog.Show(this, message, "PICK Z CAL 준비 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationEmptyWaferConfirm",
                    "PickZ Calibration 빈 웨이퍼 준비 사용자 승인. ring8=" + ring8 +
                    ", ring12=" + ring12 +
                    ", inputStageData=" + (wafer != null ? wafer.WaferId : "-") + " - Check");
                return true;
            }

            lblStatus.Text = "빈 웨이퍼 준비가 확인되지 않아 PickZ Calibration을 시작하지 않습니다.";
            return false;
        }

        private bool ConfirmOutputStagesReady(Form1 host)
        {
            OutputStageUnit stage = host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;
            if (stage == null)
            {
                lblStatus.Text = "OutputStageUnit이 없어 PlaceZ Calibration 준비 상태를 확인할 수 없습니다.";
                return false;
            }

            bool bypassSensor = stage.IsOutputStageSimulationOrDryRun() ||
                                (AppSettingsStore.Current != null &&
                                 (AppSettingsStore.Current.BypassHardware ||
                                  AppSettingsStore.Current.SimulationMode ||
                                  AppSettingsStore.Current.DryRunMode)) ||
                                !AjinFactory.IsRealBoardReady;
            if (!bypassSensor && (stage.GoodBinRingSensor == null || stage.NgBinRingSensor == null))
            {
                lblStatus.Text = "Good/NG Output Stage 제품 확인 센서를 찾을 수 없어 PlaceZ Calibration을 시작하지 않습니다.";
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-PLACE-Z-SENSOR-MISSING", lblStatus.Text);
                return false;
            }

            bool goodDetected = !bypassSensor && stage.GoodBinRingSensor != null && stage.GoodBinRingSensor.IsOn;
            bool ngDetected = !bypassSensor && stage.NgBinRingSensor != null && stage.NgBinRingSensor.IsOn;
            WaferMaterial goodData = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
            WaferMaterial ngData = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);

            if (goodDetected || ngDetected)
            {
                string message = "Output Stage 제품 확인 센서가 감지 상태입니다.\r\n" +
                                 "Good Sensor=" + goodDetected + "\r\n" +
                                 "NG Sensor=" + ngDetected + "\r\n\r\n" +
                                 "PlaceZ Calibration은 Good/NG Stage가 비어 있는 상태가 기준입니다.\r\n" +
                                 "실제 상태를 확인했으며 이 센서 상태에서도 진행하려면 Yes를 누르세요.";
                if (QMC.Common.MessageDialog.Show(this, message, "PLACE Z CAL 준비 확인",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    lblStatus.Text = "Output Stage 제품 센서 확인에서 사용자가 진행을 취소했습니다.";
                    return false;
                }

                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationOutputSensorConfirm",
                    "Output Stage 제품 센서 감지 상태 진행 사용자 승인. goodDetected=" + goodDetected +
                    ", ngDetected=" + ngDetected + " - Check");
            }

            if (goodData != null || ngData != null)
            {
                string message = "Good/NG Stage 센서는 Empty이지만 Material Data가 남아 있습니다.\r\n" +
                                 "Good Data=" + (goodData != null ? goodData.WaferId : "-") + "\r\n" +
                                 "NG Data=" + (ngData != null ? ngData.WaferId : "-") + "\r\n\r\n" +
                                 "Data를 유지한 상태로 Good Stage 기준 PlaceZ Calibration 8회를 진행할까요?";
                if (QMC.Common.MessageDialog.Show(this, message, "PLACE Z CAL 준비 확인",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    lblStatus.Text = "Output Stage Material Data 확인에서 사용자가 진행을 취소했습니다.";
                    return false;
                }

                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationOutputDataConfirm",
                    "Output Stage Material Data가 남아 있는 상태로 진행 승인. goodData=" +
                    (goodData != null ? goodData.WaferId : "-") +
                    ", ngData=" + (ngData != null ? ngData.WaferId : "-") + " - Check");
            }

            return true;
        }

        private bool CheckRunReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!chkColletCal.Checked && !chkPickZCal.Checked && !chkPlaceZCal.Checked)
                {
                    reason = "실행할 Calibration 항목을 하나 이상 선택하세요.";
                    return false;
                }

                Form1 host = ResolveHost(out reason);
                if (host == null)
                    return false;
                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람을 해제한 후 실행하세요.";
                    return false;
                }
                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }
                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 Auto Calibration을 실행할 수 없습니다.";
                    return false;
                }
                if (host.Controller.Status == EquipmentStatus.ManualRunning || host.Controller.IsManualBusy)
                {
                    reason = "다른 수동 동작이 실행 중입니다.";
                    return false;
                }
                if (host.Controller.IsSequenceRunning)
                {
                    reason = "다른 시퀀스가 실행 중입니다.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                {
                    reason = "활성 Recipe가 없습니다. Calibration 결과를 저장할 Recipe를 먼저 로드하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Auto Calibration 준비 상태 확인 중 예외가 발생했습니다: " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void LoadSettingsToUi()
        {
            try
            {
                _loading = true;
                Form1 host;
                string reason;
                host = ResolveHost(out reason);
                CalibrationData data = ResolveCalibrationData(host);
                if (data == null)
                {
                    lblStatus.Text = reason.Length > 0 ? reason : "Auto Calibration 설정을 불러올 수 없습니다.";
                    return;
                }

                data.EnsureObjects();
                chkColletCal.Checked = data.AutoCalibration.UseColletCalibration;
                chkPickZCal.Checked = data.AutoCalibration.UsePickUpZCalibration;
                chkPlaceZCal.Checked = data.AutoCalibration.UsePlaceZCalibration;
                lblStatus.Text = "저장된 Auto Calibration 사용 설정을 불러왔습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Auto Calibration 설정 로드 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-LOAD", lblStatus.Text);
            }
            finally
            {
                _loading = false;
                UpdateSelectionStyles();
            }
        }

        private bool SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                string reason;
                Form1 host = ResolveHost(out reason);
                CalibrationData data = ResolveCalibrationData(host);
                if (host == null || data == null)
                {
                    lblStatus.Text = reason.Length > 0 ? reason : "Auto Calibration 설정 저장 대상이 없습니다.";
                    return false;
                }

                data.AutoCalibration = ReadSettingsFromUi();
                data.Touch(UserSession.Name);
                string saveReason;
                if (!CalibrationDataStore.Save(data, out saveReason))
                {
                    lblStatus.Text = "Auto Calibration 사용 설정 저장 실패: " + saveReason;
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "AUTO CALIBRATION",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!host.Machine.SaveSettings())
                {
                    lblStatus.Text = "Auto Calibration 설정을 장비 Config에 저장하지 못했습니다.";
                    return false;
                }

                lblStatus.Text = "Auto Calibration 사용 설정을 저장했습니다.";
                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationSaveSettings",
                    "UseCollet=" + chkColletCal.Checked +
                    ", UsePickZ=" + chkPickZCal.Checked +
                    ", UsePlaceZ=" + chkPlaceZCal.Checked + " - Ok");
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "AUTO CALIBRATION",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Auto Calibration 설정 저장 중 예외가 발생했습니다: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-SAVE", lblStatus.Text);
                return false;
            }
            finally
            {
            }
        }

        private AutoCalibrationSettings ReadSettingsFromUi()
        {
            return new AutoCalibrationSettings
            {
                UseColletCalibration = chkColletCal.Checked,
                UsePickUpZCalibration = chkPickZCal.Checked,
                UsePlaceZCalibration = chkPlaceZCal.Checked
            };
        }

        private CalibrationData ResolveCalibrationData(Form1 host)
        {
            if (host == null || host.Machine == null || host.Machine.VisionUnit == null ||
                host.Machine.VisionUnit.Config == null)
                return null;

            host.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            return host.Machine.VisionUnit.Config.CalibrationData;
        }

        private void RequestStop(string reason)
        {
            try
            {
                CancellationTokenSource cts = _runCts;
                if (cts != null && !cts.IsCancellationRequested)
                    cts.Cancel();
                if (_activeSequence != null)
                    _activeSequence.RequestImmediateStop(reason);

                lblStatus.Text = "Auto Calibration 정지 요청을 보냈습니다.";
                AppendHistory(lblStatus.Text + " reason=" + reason);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Auto Calibration 정지 요청 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-STOP-REQUEST", lblStatus.Text);
            }
            finally
            {
            }
        }

        private void ActiveSequence_ProgressChanged(AutoCalibrationProgress progress)
        {
            if (progress == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<AutoCalibrationProgress>(ActiveSequence_ProgressChanged), progress);
                return;
            }

            int maximum = Math.Max(1, progress.TotalCount);
            progressCalibration.Maximum = maximum;
            progressCalibration.Value = Math.Max(0, Math.Min(maximum, progress.CompletedCount));
            lblCurrentTarget.Text = progress.CalibrationKind + " / " + progress.Side +
                                    (progress.PickerNo > 0 ? " #" + progress.PickerNo : string.Empty) +
                                    " / " + progress.Step + " / " +
                                    progress.CompletedCount + " / " + progress.TotalCount;
            lblStatus.Text = progress.Message;
            AppendHistory(lblCurrentTarget.Text + " - " + progress.Message);
        }

        private void AppendHistory(string message)
        {
            if (lstHistory == null)
                return;

            string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + (message ?? string.Empty);
            lstHistory.Items.Add(line);
            while (lstHistory.Items.Count > 300)
                lstHistory.Items.RemoveAt(0);
            if (lstHistory.Items.Count > 0)
                lstHistory.TopIndex = lstHistory.Items.Count - 1;
        }

        private void SetControlsEnabled(bool enabled)
        {
            chkColletCal.Enabled = enabled;
            chkPickZCal.Enabled = enabled;
            chkPlaceZCal.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnStart.Enabled = enabled;
            btnClose.Enabled = enabled;
            btnStop.Enabled = !enabled;
        }

        private void UpdateSelectionStyles()
        {
            ApplyCheckStyle(chkColletCal);
            ApplyCheckStyle(chkPickZCal);
            ApplyCheckStyle(chkPlaceZCal);
        }

        private static void ApplyCheckStyle(CheckBox checkBox)
        {
            if (checkBox == null)
                return;

            checkBox.BackColor = checkBox.Checked
                ? System.Drawing.Color.FromArgb(238, 82, 24)
                : System.Drawing.Color.Gainsboro;
            checkBox.ForeColor = checkBox.Checked
                ? System.Drawing.Color.White
                : System.Drawing.Color.Black;
        }

        private static void SetPickerVacuum(Form1 host, VisionFocusPickerSide side, int pickerNo, bool on)
        {
            if (side == VisionFocusPickerSide.Front)
                host.Machine.PickerFrontUnit.SetPickerVacuum(pickerNo, on);
            else
                host.Machine.PickerRearUnit.SetPickerVacuum(pickerNo, on);
        }

        private static bool IsPickerFlowDetected(Form1 host, VisionFocusPickerSide side, int pickerNo)
        {
            return side == VisionFocusPickerSide.Front
                ? host.Machine.PickerFrontUnit.IsPickerFlowDetected(pickerNo, true)
                : host.Machine.PickerRearUnit.IsPickerFlowDetected(pickerNo, true);
        }

        private static bool ShouldBypassPickerFlowCheck(Form1 host, VisionFocusPickerSide side)
        {
            AppSettings settings = AppSettingsStore.Current;
            if ((settings != null &&
                 (settings.BypassHardware || settings.SimulationMode || settings.DryRunMode)) ||
                !AjinFactory.IsRealBoardReady)
                return true;

            if (host == null || host.Machine == null)
                return true;

            if (side == VisionFocusPickerSide.Front)
            {
                return host.Machine.PickerFrontUnit == null ||
                       (host.Machine.PickerFrontUnit.Config != null && host.Machine.PickerFrontUnit.Config.IsSimulationMode) ||
                       (host.Machine.PickerFrontUnit.Setup != null && host.Machine.PickerFrontUnit.Setup.IsSimulationMode);
            }

            return host.Machine.PickerRearUnit == null ||
                   (host.Machine.PickerRearUnit.Config != null && host.Machine.PickerRearUnit.Config.IsSimulationMode) ||
                   (host.Machine.PickerRearUnit.Setup != null && host.Machine.PickerRearUnit.Setup.IsSimulationMode);
        }

        private Form1 ResolveHost(out string reason)
        {
            reason = string.Empty;
            Form1 host = Owner as Form1;
            if (host == null)
                host = FindForm() as Form1;
            if (host == null)
            {
                foreach (Form form in Application.OpenForms)
                {
                    host = form as Form1;
                    if (host != null)
                        break;
                }
            }

            if (host == null)
                reason = "Main 화면을 찾을 수 없습니다.";
            else if (host.Machine == null)
            {
                reason = "장비 객체가 준비되지 않았습니다.";
                host = null;
            }

            return host;
        }
    }
}
