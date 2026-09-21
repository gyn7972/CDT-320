using System;
using QMC.CDT_320.Ui.Localization;
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
    public partial class AutoCalibrationDialog : Form, ILocalizedView
    {
        private bool _busy;
        private bool _loading;
        private CancellationTokenSource _runCts;
        private AutoCalibrationSequence _activeSequence;
        private Action _controllerStopHandler;

        private void InitializeLocalization()
        {
            Lang.BindKey(lblHeader, "calibration.auto.lblHeader");
            Lang.BindKey(grpSelection, "calibration.auto.grpSelection");
            Lang.BindKey(chkColletCal, "calibration.auto.chkColletCal");
            Lang.BindKey(chkPickZCal, "calibration.auto.chkPickZCal");
            Lang.BindKey(chkPlaceZCal, "calibration.auto.chkPlaceZCal");
            Lang.BindKey(lblPlaceZGuide, "calibration.auto.lblPlaceZGuide");
            Lang.BindKey(grpProgress, "calibration.auto.grpProgress");
            Lang.BindKey(lblCurrentTarget, "calibration.auto.lblCurrentTarget");
            Lang.BindKey(lblStatus, "calibration.auto.lblStatus");
            Lang.BindKey(btnReload, "calibration.auto.btnReload");
            Lang.BindKey(btnSave, "calibration.auto.btnSave");
            Lang.BindKey(btnStart, "calibration.auto.btnStart");
            Lang.BindKey(btnStop, "calibration.auto.btnStop");
            Lang.BindKey(btnClose, "calibration.auto.btnClose");
            Lang.BindKey(this, "calibration.auto.this");
        }

        public void ApplyLanguage()
        {
            // 언어 변경은 표시만 무효화하며 선택/입력/설정값을 다시 불러오지 않습니다.
            Invalidate(true);
        }

        public AutoCalibrationDialog()
        {
            try
            {
                InitializeComponent();
            InitializeLocalization();
                LoadSettingsToUi();
                UpdateSelectionStyles();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "AUTO-CAL-DLG-INIT",
                    "Auto Calibration 화면 초기화 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(this,
                    Lang.Format("calibration.message.m008", ex.Message),
                    Lang.T("calibration.message.m009"),
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
                    Lang.BindFormat(lblStatus, "calibration.status.s118");
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

                // 사용자가 직접 닫을 때(UserClosing)만 창을 붙잡는다.
                // 앱/Windows/소유자(Form1) 종료 경로에서 e.Cancel을 세우면 Form1 종료가 취소되어
                // 프로그램을 끌 수 없게 된다(소유 폼에는 FormOwnerClosing으로 전달됨).
                if (e.CloseReason != CloseReason.UserClosing)
                {
                    RequestStop("Auto Calibration 창 종료(" + e.CloseReason + ")");
                    return;
                }

                DialogResult result = QMC.Common.MessageDialog.Show(this,
                    Lang.T("calibration.message.m010"),
                    Lang.T("calibration.message.m009"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                RequestStop("Auto Calibration 창 닫기");
                e.Cancel = true;
                Lang.BindFormat(lblStatus, "calibration.status.s008");
            }
            catch (Exception ex)
            {
                if (e.CloseReason == CloseReason.UserClosing)
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
                    CalibrationDialogText.BindStatus(lblStatus, reason);
                    QMC.Common.MessageDialog.Show(this, reason, Lang.T("calibration.message.m009"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!SaveSettingsFromUi(false))
                    return;

                host = ResolveHost(out reason);
                if (host == null)
                {
                    CalibrationDialogText.BindStatus(lblStatus, reason);
                    return;
                }

                Lang.BindFormat(lblStatus, "calibration.status.s119");
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
                Lang.BindFormat(lblStatus, "calibration.status.s120");
                int result = await _activeSequence.RunAsync(runCts.Token).ConfigureAwait(true);
                if (result != 0)
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s121");
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("calibration.message.m009"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                host.SaveMachineSettings();
                Lang.BindFormat(lblStatus, "calibration.status.s122");
                AppendHistory(lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("calibration.message.m009"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s123");
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Event, "CAL", "AUTO-CAL-STOP", lblStatus.Text);
            }
            catch (SequenceStopException ex)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s124", ex.Message);
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Event, "CAL", "AUTO-CAL-STOP", lblStatus.Text);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s125", ex.Message);
                AppendHistory(lblStatus.Text);
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-DLG-RUN", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("calibration.message.m009"),
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
                        Lang.BindFormat(lblStatus, "calibration.status.s126", pickerName, ex.Message);
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
                string message = Lang.Format("calibration.auto.confirm.pickerFlow", string.Join(", ", physicalDetected.ToArray()));
                Lang.BindFormat(lblStatus, "calibration.status.s127");
                QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m009"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (materialDetected.Count > 0)
            {
                string message = Lang.Format("calibration.auto.confirm.pickerMaterial", string.Join("\r\n", materialDetected.ToArray()));
                if (QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m009"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s128");
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
                Lang.Format("calibration.auto.confirm.inputWafer", ring8, ring12, (wafer != null ? wafer.WaferId : "-"));

            if (QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m011"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationEmptyWaferConfirm",
                    "PickZ Calibration 빈 웨이퍼 준비 사용자 승인. ring8=" + ring8 +
                    ", ring12=" + ring12 +
                    ", inputStageData=" + (wafer != null ? wafer.WaferId : "-") + " - Check");
                return true;
            }

            Lang.BindFormat(lblStatus, "calibration.status.s129");
            return false;
        }

        private bool ConfirmOutputStagesReady(Form1 host)
        {
            OutputStageUnit stage = host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;
            if (stage == null)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s130");
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
                Lang.BindFormat(lblStatus, "calibration.status.s131");
                EventLogger.Write(EventKind.Alarm, "CAL", "AUTO-CAL-PLACE-Z-SENSOR-MISSING", lblStatus.Text);
                return false;
            }

            bool goodDetected = !bypassSensor && stage.GoodBinRingSensor != null && stage.GoodBinRingSensor.IsOn;
            bool ngDetected = !bypassSensor && stage.NgBinRingSensor != null && stage.NgBinRingSensor.IsOn;
            WaferMaterial goodData = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
            WaferMaterial ngData = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);

            if (goodDetected || ngDetected)
            {
                string message = Lang.Format("calibration.auto.confirm.outputSensor", goodDetected, ngDetected);
                if (QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m012"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s132");
                    return false;
                }

                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationOutputSensorConfirm",
                    "Output Stage 제품 센서 감지 상태 진행 사용자 승인. goodDetected=" + goodDetected +
                    ", ngDetected=" + ngDetected + " - Check");
            }

            if (goodData != null || ngData != null)
            {
                string message = Lang.Format("calibration.auto.confirm.outputMaterial", (goodData != null ? goodData.WaferId : "-"), (ngData != null ? ngData.WaferId : "-"));
                if (QMC.Common.MessageDialog.Show(this, message, Lang.T("calibration.message.m012"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s133");
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
                if (string.IsNullOrWhiteSpace(host.ActiveRecipeName))
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
                    { if (reason.Length > 0) CalibrationDialogText.BindStatus(lblStatus, reason); else Lang.BindFormat(lblStatus, "calibration.status.s134"); }
                    return;
                }

                data.EnsureObjects();
                chkColletCal.Checked = data.AutoCalibration.UseColletCalibration;
                chkPickZCal.Checked = data.AutoCalibration.UsePickUpZCalibration;
                chkPlaceZCal.Checked = data.AutoCalibration.UsePlaceZCalibration;
                Lang.BindFormat(lblStatus, "calibration.status.s135");
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s136", ex.Message);
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
                    { if (reason.Length > 0) CalibrationDialogText.BindStatus(lblStatus, reason); else Lang.BindFormat(lblStatus, "calibration.status.s137"); }
                    return false;
                }

                data.AutoCalibration = ReadSettingsFromUi();
                data.Touch(UserSession.Name);
                string saveReason;
                if (!CalibrationDataStore.Save(data, out saveReason))
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s138", saveReason);
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("calibration.message.m009"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!host.Machine.SaveSettings())
                {
                    Lang.BindFormat(lblStatus, "calibration.status.s139");
                    return false;
                }

                Lang.BindFormat(lblStatus, "calibration.status.s140");
                QMC.Common.Log.Write("Calibration", UserSession.Name, "AutoCalibrationSaveSettings",
                    "UseCollet=" + chkColletCal.Checked +
                    ", UsePickZ=" + chkPickZCal.Checked +
                    ", UsePlaceZ=" + chkPlaceZCal.Checked + " - Ok");
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, Lang.T("calibration.message.m009"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s141", ex.Message);
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

                Lang.BindFormat(lblStatus, "calibration.status.s142");
                AppendHistory(lblStatus.Text + " reason=" + reason);
            }
            catch (Exception ex)
            {
                Lang.BindFormat(lblStatus, "calibration.status.s143", ex.Message);
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
            Lang.BindFormat(lblCurrentTarget, "calibration.status.s144", progress.CalibrationKind, progress.Side, (progress.PickerNo > 0 ? " #" + progress.PickerNo : string.Empty), progress.Step, progress.CompletedCount, progress.TotalCount);
            CalibrationDialogText.BindStatus(lblStatus, progress.Message);
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
