using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Sequencing;
using QMC.CDT320.Sequencing.Calibration;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 콜렛 클리닝 수동 제어 다이얼로그.
    /// Recipe -> Calibration 화면에서 열며, 선택한 콜렛(Front/Rear x 4,3,2,1)을
    /// "전부 클린 -> 전부 검사 -> NG만 재시도" 순서로 실행한다.
    /// 자동 실행 조건(웨이퍼 교체/공정 횟수/Auto 시작)도 이 화면에서 설정한다.
    /// </summary>
    public partial class ColletCleaningControlDialog : Form
    {
        private ColletCleaningSettings _settings = new ColletCleaningSettings();
        private CancellationTokenSource _runCts;
        private Action _activeStopRequest;
        private bool _busy;
        private bool _suppressUiEvents;

        public ColletCleaningControlDialog()
        {
            InitializeComponent();
            WireEvents();
            LoadSettingsToUi();
            RefreshHistory();
        }

        public static ColletCleaningControlDialog Open(Form owner)
        {
            var dialog = new ColletCleaningControlDialog();
            dialog.Show(owner);
            return dialog;
        }

        private void WireEvents()
        {
            btnSelectAll.Click += delegate { SetAllTargets(true); };
            btnSelectNone.Click += delegate { SetAllTargets(false); };
            btnSave.Click += delegate { SaveSettingsFromUi(true); };
            btnReload.Click += delegate { LoadSettingsToUi(); RefreshHistory(); };
            btnClose.Click += delegate { Close(); };
            btnStart.Click += async delegate { await StartCleaningAsync().ConfigureAwait(true); };
            btnStop.Click += delegate { RequestActiveStop("StopButton"); };
            FormClosing += ColletCleaningControlDialog_FormClosing;
        }

        private void ColletCleaningControlDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy)
            {
                e.Cancel = true;
                AppendLog("실행 중에는 창을 닫을 수 없습니다. 먼저 STOP으로 정지하세요.");
            }
        }

        private void SetAllTargets(bool selected)
        {
            _suppressUiEvents = true;
            try
            {
                chkFront4.Checked = selected;
                chkFront3.Checked = selected;
                chkFront2.Checked = selected;
                chkFront1.Checked = selected;
                chkRear4.Checked = selected;
                chkRear3.Checked = selected;
                chkRear2.Checked = selected;
                chkRear1.Checked = selected;
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        // ---------------------------------------------------------------- 설정 로드/저장

        private void LoadSettingsToUi()
        {
            try
            {
                CalibrationData data = CalibrationDataStore.LoadOrCreate();
                data.EnsureObjects();
                _settings = data.ColletCleaning.Clone();

                _suppressUiEvents = true;
                try
                {
                    chkFront4.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Front, 4);
                    chkFront3.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Front, 3);
                    chkFront2.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Front, 2);
                    chkFront1.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Front, 1);
                    chkRear4.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Rear, 4);
                    chkRear3.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Rear, 3);
                    chkRear2.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Rear, 2);
                    chkRear1.Checked = _settings.IsColletSelected(VisionFocusPickerSide.Rear, 1);

                    SetNumeric(numCleanVelocity, _settings.CleanVelocity);
                    SetNumeric(numCleanAcceleration, _settings.CleanAcceleration);
                    SetNumeric(numCleanDeceleration, _settings.CleanDeceleration);
                    SetNumeric(numContactZUserOffset, _settings.ContactZUserOffset);
                    SetNumeric(numMaxExtraPressDepth, _settings.MaxExtraPressDepth);
                    SetNumeric(numArriveDwellMs, _settings.ArriveDwellMs);
                    SetNumeric(numCleanPressCount, _settings.CleanPressCount);
                    SetNumeric(numRepeatLiftHeight, _settings.RepeatLiftHeight);

                    SetNumeric(numDieHeight, _settings.DieHeight);
                    SetNumeric(numRimHeight, _settings.RimHeight);
                    SetNumeric(numFilmHeight, _settings.FilmHeight);
                    SetNumeric(numMaxRetryCount, _settings.MaxRetryCount);
                    chkAllowPlaceOnCleanedCell.Checked = _settings.AllowPlaceOnCleanedCell;
                    chkDisablePickerOnReplaceAlarm.Checked = _settings.DisablePickerOnReplaceAlarm;

                    chkUseTriggerOnWaferExchange.Checked = _settings.UseTriggerOnWaferExchange;
                    SetNumeric(numWaferExchangeInterval, _settings.WaferExchangeInterval);
                    chkUseTriggerOnProcessCount.Checked = _settings.UseTriggerOnProcessCount;
                    SetNumeric(numProcessCountInterval, _settings.ProcessCountInterval);
                    cmbProcessCountUnit.SelectedIndex =
                        _settings.ProcessCountUnit == ColletCleaningProcessCountUnit.Wafer ? 1 : 0;
                    chkUseTriggerOnAutoStart.Checked = _settings.UseTriggerOnAutoStart;
                }
                finally
                {
                    _suppressUiEvents = false;
                }

                lblStatus.Text = "설정을 불러왔습니다.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "설정 불러오기에 실패했습니다. " + ex.Message;
                AppendLog("설정 불러오기 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool SaveSettingsFromUi(bool showMessage)
        {
            try
            {
                ColletCleaningSettings settings = BuildSettingsFromUi();

                CalibrationData data = CalibrationDataStore.LoadOrCreate();
                data.EnsureObjects();
                data.ColletCleaning = settings;
                data.Touch("ColletCleaningControlDialog");

                string message;
                if (!CalibrationDataStore.Save(data, out message))
                {
                    lblStatus.Text = "설정 저장에 실패했습니다. " + message;
                    if (showMessage)
                        QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CLEANING",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                _settings = settings;
                lblStatus.Text = "설정을 저장했습니다.";
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningSettings",
                    "콜렛 클리닝 설정을 저장했습니다. pressCount=" + settings.CleanPressCount +
                    ", contactOffset=" + settings.ContactZUserOffset.ToString("F4") +
                    ", maxRetry=" + settings.MaxRetryCount +
                    ", allowPlaceOnCleanedCell=" + settings.AllowPlaceOnCleanedCell + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "설정 저장 중 예외가 발생했습니다. " + ex.Message;
                AppendLog(lblStatus.Text);
                return false;
            }
            finally
            {
            }
        }

        private ColletCleaningSettings BuildSettingsFromUi()
        {
            var settings = _settings != null ? _settings.Clone() : new ColletCleaningSettings();

            settings.SetColletSelected(VisionFocusPickerSide.Front, 4, chkFront4.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 3, chkFront3.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 2, chkFront2.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 1, chkFront1.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 4, chkRear4.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 3, chkRear3.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 2, chkRear2.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 1, chkRear1.Checked);

            settings.CleanVelocity = (double)numCleanVelocity.Value;
            settings.CleanAcceleration = (double)numCleanAcceleration.Value;
            settings.CleanDeceleration = (double)numCleanDeceleration.Value;
            settings.ContactZUserOffset = (double)numContactZUserOffset.Value;
            settings.MaxExtraPressDepth = (double)numMaxExtraPressDepth.Value;
            settings.ArriveDwellMs = (int)numArriveDwellMs.Value;
            settings.CleanPressCount = (int)numCleanPressCount.Value;
            settings.RepeatLiftHeight = (double)numRepeatLiftHeight.Value;

            settings.DieHeight = (double)numDieHeight.Value;
            settings.RimHeight = (double)numRimHeight.Value;
            settings.FilmHeight = (double)numFilmHeight.Value;
            settings.MaxRetryCount = (int)numMaxRetryCount.Value;
            settings.AllowPlaceOnCleanedCell = chkAllowPlaceOnCleanedCell.Checked;
            settings.DisablePickerOnReplaceAlarm = chkDisablePickerOnReplaceAlarm.Checked;

            settings.UseTriggerOnWaferExchange = chkUseTriggerOnWaferExchange.Checked;
            settings.WaferExchangeInterval = (int)numWaferExchangeInterval.Value;
            settings.UseTriggerOnProcessCount = chkUseTriggerOnProcessCount.Checked;
            settings.ProcessCountInterval = (int)numProcessCountInterval.Value;
            settings.ProcessCountUnit = cmbProcessCountUnit.SelectedIndex == 1
                ? ColletCleaningProcessCountUnit.Wafer
                : ColletCleaningProcessCountUnit.Die;
            settings.UseTriggerOnAutoStart = chkUseTriggerOnAutoStart.Checked;

            settings.EnsureObjects();
            return settings;
        }

        private static void SetNumeric(NumericUpDown control, double value)
        {
            if (control == null)
                return;

            decimal current = (decimal)value;
            if (current < control.Minimum) current = control.Minimum;
            if (current > control.Maximum) current = control.Maximum;
            control.Value = current;
        }

        private static void SetNumeric(NumericUpDown control, int value)
        {
            SetNumeric(control, (double)value);
        }

        // ---------------------------------------------------------------- 실행

        private async Task StartCleaningAsync()
        {
            if (_busy)
                return;

            Form1 host = null;
            IDisposable actionScope = null;
            Action stopHandler = null;
            CancellationTokenSource runCts = null;

            try
            {
                _busy = true;
                SetControlsEnabled(false);

                string reason;
                if (!CanRunManualCleaning(out reason))
                {
                    lblStatus.Text = reason;
                    QMC.Common.MessageDialog.Show(this, reason, "COLLET CLEANING",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                host = ResolveHost(out reason);
                if (host == null)
                {
                    lblStatus.Text = reason;
                    return;
                }

                if (!SaveSettingsFromUi(false))
                    return;

                if (!_settings.HasAnySelection())
                {
                    lblStatus.Text = "선택된 콜렛이 없습니다.";
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CLEANING",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                actionScope = host.Controller.BeginManualActionScope(
                    ManualMotionScopeKind.ProcessSequence, "ColletCleaning");
                runCts = CancellationTokenSource.CreateLinkedTokenSource(host.Controller.ManualOperationToken);
                _runCts = runCts;
                stopHandler = CreateStopRequestAction("ColletCleaning");
                _activeStopRequest = stopHandler;
                host.Controller.StopRequested += stopHandler;
                btnStop.Enabled = true;

                AppendLog("콜렛 클리닝을 시작합니다.");
                lblStatus.Text = "콜렛 클리닝 실행 중입니다.";

                var context = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new AutoColletCleaningSequence(context, _settings, SequenceRunMode.Manual);
                sequence.ProgressChanged += OnProgressChanged;

                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("ColletCleaningControlDialog.Start"))
                {
                    result = await sequence.RunAsync(runCts.Token).ConfigureAwait(true);
                }

                sequence.ProgressChanged -= OnProgressChanged;
                RefreshHistory();

                if (result != 0)
                {
                    lblStatus.Text = "콜렛 클리닝 실패. Alarm/Event Log를 확인하세요.";
                    AppendLog(lblStatus.Text);
                    QMC.Common.MessageDialog.Show(this, lblStatus.Text, "COLLET CLEANING",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (sequence.SkippedNoBin)
                {
                    lblStatus.Text = sequence.SkipReason;
                    AppendLog(sequence.SkipReason);
                    return;
                }

                lblStatus.Text = "콜렛 클리닝을 완료했습니다.";
                AppendLog(lblStatus.Text);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "콜렛 클리닝이 정지되었습니다.";
                AppendLog(lblStatus.Text);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "콜렛 클리닝 중 예외가 발생했습니다. " + ex.Message;
                AppendLog(lblStatus.Text);
                EventLogger.Write(EventKind.Alarm, "CAL", "COLLET-CLEAN-DIALOG",
                    "콜렛 클리닝 다이얼로그 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (host != null && host.Controller != null && stopHandler != null)
                    host.Controller.StopRequested -= stopHandler;
                if (ReferenceEquals(_runCts, runCts))
                    _runCts = null;
                if (ReferenceEquals(_activeStopRequest, stopHandler))
                    _activeStopRequest = null;
                if (runCts != null)
                    runCts.Dispose();
                if (actionScope != null)
                    actionScope.Dispose();

                _busy = false;
                SetControlsEnabled(true);
                btnStop.Enabled = false;
            }
        }

        private void OnProgressChanged(ColletCleaningProgress progress)
        {
            if (progress == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<ColletCleaningProgress>(OnProgressChanged), progress);
                return;
            }

            AppendLog("[" + progress.Side + "] " + progress.Step + " - " + progress.Message +
                      " (" + progress.CompletedSideCount + "/" + progress.TotalSideCount + ")");
        }

        private bool CanRunManualCleaning(out string reason)
        {
            reason = string.Empty;
            try
            {
                Form1 host = ResolveHost(out reason);
                if (host == null)
                    return false;

                if (AlarmManager.HasActive)
                {
                    reason = "현재 알람 상태입니다. 알람 해제 후 콜렛 클리닝을 실행하세요.";
                    return false;
                }

                if (host.Controller == null)
                {
                    reason = "MachineController가 준비되지 않았습니다.";
                    return false;
                }

                if (host.Controller.Status == EquipmentStatus.AutoRunning)
                {
                    reason = "자동 운전 중에는 수동 콜렛 클리닝을 실행할 수 없습니다.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "콜렛 클리닝 실행 조건 확인 중 예외가 발생했습니다. " + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private Form1 ResolveHost(out string reason)
        {
            reason = string.Empty;
            Form1 host = Owner as Form1;
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
            {
                reason = "Main 화면을 찾을 수 없어 콜렛 클리닝을 실행할 수 없습니다.";
                return null;
            }

            if (host.Machine == null)
            {
                reason = "Machine이 준비되지 않아 콜렛 클리닝을 실행할 수 없습니다.";
                return null;
            }

            return host;
        }

        private Action CreateStopRequestAction(string actionName)
        {
            return delegate
            {
                try
                {
                    CancellationTokenSource cts = _runCts;
                    if (cts != null && !cts.IsCancellationRequested)
                        cts.Cancel();

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningStop",
                        "콜렛 클리닝 정지 요청. action=" + actionName);
                }
                catch
                {
                }
            };
        }

        private void RequestActiveStop(string source)
        {
            try
            {
                Action request = _activeStopRequest;
                if (request != null)
                {
                    request();
                    AppendLog("정지를 요청했습니다. source=" + source);
                }
            }
            catch (Exception ex)
            {
                AppendLog("정지 요청 중 예외가 발생했습니다. " + ex.Message);
            }
            finally
            {
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            grpTarget.Enabled = enabled;
            grpMotion.Enabled = enabled;
            grpHeight.Enabled = enabled;
            grpTrigger.Enabled = enabled;
            btnStart.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        // ---------------------------------------------------------------- 이력 / 로그

        private void RefreshHistory()
        {
            try
            {
                CalibrationData data = CalibrationDataStore.LoadOrCreate();
                data.EnsureObjects();

                lstHistory.BeginUpdate();
                try
                {
                    lstHistory.Items.Clear();
                    AppendHistoryRows(data, VisionFocusPickerSide.Front);
                    AppendHistoryRows(data, VisionFocusPickerSide.Rear);
                }
                finally
                {
                    lstHistory.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                AppendLog("이력 갱신 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AppendHistoryRows(CalibrationData data, VisionFocusPickerSide side)
        {
            for (int colletNo = 4; colletNo >= 1; colletNo--)
            {
                ColletCleaningHistoryRecord record = data.ColletCleaningHistory.Get(side, colletNo);
                var item = new ListViewItem(side.ToString());
                item.SubItems.Add(colletNo.ToString());
                item.SubItems.Add(record != null && record.HasHistory
                    ? record.LastCleanedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    : "-");
                item.SubItems.Add(record != null ? record.TotalCleanCount.ToString() : "0");
                item.SubItems.Add(record != null ? record.LastResult.ToString() : "None");
                lstHistory.Items.Add(item);
            }
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            lstLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            if (lstLog.Items.Count > 500)
                lstLog.Items.RemoveAt(0);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
}
