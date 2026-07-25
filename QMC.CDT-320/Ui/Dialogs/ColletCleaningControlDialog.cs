using System;
using System.Globalization;
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
    /// Recipe -> Calibration 화면에서 열며, 선택한 콜렛(Front/Rear x C4~C1)을
    /// "전부 클린 -> 전부 검사 -> NG만 재시도" 순서로 실행한다.
    /// 자동 실행 조건(웨이퍼 교체 / 공정 횟수 / Auto 시작)도 이 화면에서 설정한다.
    /// 화면 구성은 COLLET CALIBRATION 다이얼로그와 동일한 디자인 규격을 따른다.
    /// </summary>
    public partial class ColletCleaningControlDialog : Form
    {
        private enum SettingKey
        {
            CleanVelocity,
            CleanAcceleration,
            CleanDeceleration,
            ContactZUserOffset,
            MaxExtraPressDepth,
            ArriveDwellMs,
            CleanPressCount,
            RepeatLiftHeight,
            MoveTimeoutMs,
            DieHeight,
            RimHeight,
            FilmHeight,
            MaxRetryCount,
            AllowPlaceOnCleanedCell,
            DisablePickerOnReplaceAlarm,
            UseTriggerOnWaferExchange,
            WaferExchangeInterval,
            UseTriggerOnProcessCount,
            ProcessCountInterval,
            ProcessCountUnit,
            UseTriggerOnAutoStart
        }

        private sealed class SettingInfo
        {
            public SettingKey Key;
            public string Name;
            public string Unit;
            public bool Numeric;
            public bool Integer;
            public string[] Options;
        }

        private static readonly SettingInfo[] SettingRows =
        {
            new SettingInfo { Key = SettingKey.CleanVelocity,      Name = "Clean Z Speed",      Unit = "mm/s",  Numeric = true },
            new SettingInfo { Key = SettingKey.CleanAcceleration,  Name = "Clean Z Acc",        Unit = "mm/s2", Numeric = true },
            new SettingInfo { Key = SettingKey.CleanDeceleration,  Name = "Clean Z Dec",        Unit = "mm/s2", Numeric = true },
            new SettingInfo { Key = SettingKey.ContactZUserOffset, Name = "Contact Z Offset",   Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.MaxExtraPressDepth, Name = "Max Extra Press",    Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.ArriveDwellMs,      Name = "Arrive Dwell",       Unit = "ms",    Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.CleanPressCount,    Name = "Press Count",        Unit = "ea",    Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.RepeatLiftHeight,   Name = "Repeat Lift Height", Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.MoveTimeoutMs,      Name = "Move Timeout",       Unit = "ms",    Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.DieHeight,          Name = "Die Height",         Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.RimHeight,          Name = "Rim Height",         Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.FilmHeight,         Name = "Film Height",        Unit = "mm",    Numeric = true },
            new SettingInfo { Key = SettingKey.MaxRetryCount,      Name = "Retry On NG",        Unit = "ea",    Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.AllowPlaceOnCleanedCell,     Name = "Place On Cleaned Cell", Unit = "", Options = new[] { "True", "False" } },
            new SettingInfo { Key = SettingKey.DisablePickerOnReplaceAlarm, Name = "Disable On Replace",    Unit = "", Options = new[] { "True", "False" } },
            new SettingInfo { Key = SettingKey.UseTriggerOnWaferExchange,   Name = "Trig Wafer Exchange",   Unit = "", Options = new[] { "True", "False" } },
            new SettingInfo { Key = SettingKey.WaferExchangeInterval,       Name = "  Exchange Interval",   Unit = "ea", Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.UseTriggerOnProcessCount,    Name = "Trig Process Count",    Unit = "", Options = new[] { "True", "False" } },
            new SettingInfo { Key = SettingKey.ProcessCountInterval,        Name = "  Process Interval",    Unit = "ea", Numeric = true, Integer = true },
            new SettingInfo { Key = SettingKey.ProcessCountUnit,            Name = "  Process Unit",        Unit = "", Options = new[] { "Die", "Wafer" } },
            new SettingInfo { Key = SettingKey.UseTriggerOnAutoStart,       Name = "Trig Auto Start",       Unit = "", Options = new[] { "True", "False" } }
        };

        private ColletCleaningSettings _settings = new ColletCleaningSettings();
        private CancellationTokenSource _runCts;
        private Action _activeStopRequest;
        private bool _busy;
        private bool _suppressUiEvents;

        public ColletCleaningControlDialog()
        {
            InitializeComponent();
            BuildSettingRows();
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

            chkTargetAll.CheckedChanged += delegate
            {
                if (!_suppressUiEvents)
                    SetAllTargets(chkTargetAll.Checked);
            };

            gridSettings.CellValueChanged += gridSettings_CellValueChanged;
            gridSettings.CurrentCellDirtyStateChanged += gridSettings_CurrentCellDirtyStateChanged;

            FormClosing += ColletCleaningControlDialog_FormClosing;
        }

        private void ColletCleaningControlDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy)
            {
                e.Cancel = true;
                AppendLog("실행 중에는 창을 닫을 수 없습니다. 먼저 SEQ STOP으로 정지하세요.");
            }
        }

        // ---------------------------------------------------------------- SETTING 그리드

        private void BuildSettingRows()
        {
            gridSettings.Rows.Clear();
            foreach (SettingInfo info in SettingRows)
            {
                int index = gridSettings.Rows.Add();
                DataGridViewRow row = gridSettings.Rows[index];
                row.Tag = info;
                row.Cells[0].Value = info.Name;
                row.Cells[2].Value = info.Unit;

                if (info.Options != null)
                {
                    var combo = new DataGridViewComboBoxCell();
                    combo.Items.AddRange(info.Options);
                    combo.FlatStyle = FlatStyle.Flat;
                    row.Cells[1] = combo;
                }
            }
        }

        private void gridSettings_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridSettings.IsCurrentCellDirty)
                gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void gridSettings_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressUiEvents || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            var info = row.Tag as SettingInfo;
            if (info == null || !info.Numeric)
                return;

            // 숫자 셀은 입력 즉시 파싱 가능 여부만 확인하고, 잘못된 값이면 이전 값으로 되돌린다.
            string text = Convert.ToString(row.Cells[1].Value);
            double parsed;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                _suppressUiEvents = true;
                try
                {
                    row.Cells[1].Value = FormatSettingValue(info, ReadSettingValue(info));
                }
                finally
                {
                    _suppressUiEvents = false;
                }

                lblStatus.Text = info.Name + " 값이 숫자가 아니어서 이전 값으로 되돌렸습니다.";
            }
        }

        private double ReadSettingValue(SettingInfo info)
        {
            switch (info.Key)
            {
                case SettingKey.CleanVelocity: return _settings.CleanVelocity;
                case SettingKey.CleanAcceleration: return _settings.CleanAcceleration;
                case SettingKey.CleanDeceleration: return _settings.CleanDeceleration;
                case SettingKey.ContactZUserOffset: return _settings.ContactZUserOffset;
                case SettingKey.MaxExtraPressDepth: return _settings.MaxExtraPressDepth;
                case SettingKey.ArriveDwellMs: return _settings.ArriveDwellMs;
                case SettingKey.CleanPressCount: return _settings.CleanPressCount;
                case SettingKey.RepeatLiftHeight: return _settings.RepeatLiftHeight;
                case SettingKey.MoveTimeoutMs: return _settings.MoveTimeoutMs;
                case SettingKey.DieHeight: return _settings.DieHeight;
                case SettingKey.RimHeight: return _settings.RimHeight;
                case SettingKey.FilmHeight: return _settings.FilmHeight;
                case SettingKey.MaxRetryCount: return _settings.MaxRetryCount;
                case SettingKey.WaferExchangeInterval: return _settings.WaferExchangeInterval;
                case SettingKey.ProcessCountInterval: return _settings.ProcessCountInterval;
                default: return 0.0;
            }
        }

        private string ReadSettingOption(SettingInfo info)
        {
            switch (info.Key)
            {
                case SettingKey.AllowPlaceOnCleanedCell: return _settings.AllowPlaceOnCleanedCell ? "True" : "False";
                case SettingKey.DisablePickerOnReplaceAlarm: return _settings.DisablePickerOnReplaceAlarm ? "True" : "False";
                case SettingKey.UseTriggerOnWaferExchange: return _settings.UseTriggerOnWaferExchange ? "True" : "False";
                case SettingKey.UseTriggerOnProcessCount: return _settings.UseTriggerOnProcessCount ? "True" : "False";
                case SettingKey.UseTriggerOnAutoStart: return _settings.UseTriggerOnAutoStart ? "True" : "False";
                case SettingKey.ProcessCountUnit:
                    return _settings.ProcessCountUnit == ColletCleaningProcessCountUnit.Wafer ? "Wafer" : "Die";
                default: return string.Empty;
            }
        }

        private static string FormatSettingValue(SettingInfo info, double value)
        {
            return info.Integer
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static void ApplySettingFromRow(
            ColletCleaningSettings target,
            SettingInfo info,
            DataGridViewRow row)
        {
            string text = Convert.ToString(row.Cells[1].Value);

            if (info.Options != null)
            {
                bool flag = string.Equals(text, "True", StringComparison.OrdinalIgnoreCase);
                switch (info.Key)
                {
                    case SettingKey.AllowPlaceOnCleanedCell: target.AllowPlaceOnCleanedCell = flag; break;
                    case SettingKey.DisablePickerOnReplaceAlarm: target.DisablePickerOnReplaceAlarm = flag; break;
                    case SettingKey.UseTriggerOnWaferExchange: target.UseTriggerOnWaferExchange = flag; break;
                    case SettingKey.UseTriggerOnProcessCount: target.UseTriggerOnProcessCount = flag; break;
                    case SettingKey.UseTriggerOnAutoStart: target.UseTriggerOnAutoStart = flag; break;
                    case SettingKey.ProcessCountUnit:
                        target.ProcessCountUnit = string.Equals(text, "Wafer", StringComparison.OrdinalIgnoreCase)
                            ? ColletCleaningProcessCountUnit.Wafer
                            : ColletCleaningProcessCountUnit.Die;
                        break;
                }

                return;
            }

            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return;

            switch (info.Key)
            {
                case SettingKey.CleanVelocity: target.CleanVelocity = value; break;
                case SettingKey.CleanAcceleration: target.CleanAcceleration = value; break;
                case SettingKey.CleanDeceleration: target.CleanDeceleration = value; break;
                case SettingKey.ContactZUserOffset: target.ContactZUserOffset = value; break;
                case SettingKey.MaxExtraPressDepth: target.MaxExtraPressDepth = value; break;
                case SettingKey.ArriveDwellMs: target.ArriveDwellMs = (int)Math.Round(value); break;
                case SettingKey.CleanPressCount: target.CleanPressCount = (int)Math.Round(value); break;
                case SettingKey.RepeatLiftHeight: target.RepeatLiftHeight = value; break;
                case SettingKey.MoveTimeoutMs: target.MoveTimeoutMs = (int)Math.Round(value); break;
                case SettingKey.DieHeight: target.DieHeight = value; break;
                case SettingKey.RimHeight: target.RimHeight = value; break;
                case SettingKey.FilmHeight: target.FilmHeight = value; break;
                case SettingKey.MaxRetryCount: target.MaxRetryCount = (int)Math.Round(value); break;
                case SettingKey.WaferExchangeInterval: target.WaferExchangeInterval = (int)Math.Round(value); break;
                case SettingKey.ProcessCountInterval: target.ProcessCountInterval = (int)Math.Round(value); break;
            }
        }

        // ---------------------------------------------------------------- 대상 선택

        private CheckBox[] TargetChecks
        {
            get
            {
                return new[]
                {
                    chkFront4, chkFront3, chkFront2, chkFront1,
                    chkRear4, chkRear3, chkRear2, chkRear1
                };
            }
        }

        private void SetAllTargets(bool selected)
        {
            _suppressUiEvents = true;
            try
            {
                foreach (CheckBox box in TargetChecks)
                    box.Checked = selected;
                chkTargetAll.Checked = selected;
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        private bool AreAllTargetsChecked()
        {
            foreach (CheckBox box in TargetChecks)
            {
                if (!box.Checked)
                    return false;
            }

            return true;
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
                    chkTargetAll.Checked = AreAllTargetsChecked();

                    foreach (DataGridViewRow row in gridSettings.Rows)
                    {
                        var info = row.Tag as SettingInfo;
                        if (info == null)
                            continue;

                        row.Cells[1].Value = info.Options != null
                            ? ReadSettingOption(info)
                            : FormatSettingValue(info, ReadSettingValue(info));
                    }
                }
                finally
                {
                    _suppressUiEvents = false;
                }

                lblStatus.Text = "대기 중입니다. 대상 콜렛과 클리닝 조건을 확인한 뒤 START를 실행하세요.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "설정 불러오기에 실패했습니다. " + ex.Message;
                AppendLog(lblStatus.Text);
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
            ColletCleaningSettings settings = _settings != null ? _settings.Clone() : new ColletCleaningSettings();

            settings.SetColletSelected(VisionFocusPickerSide.Front, 4, chkFront4.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 3, chkFront3.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 2, chkFront2.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Front, 1, chkFront1.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 4, chkRear4.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 3, chkRear3.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 2, chkRear2.Checked);
            settings.SetColletSelected(VisionFocusPickerSide.Rear, 1, chkRear1.Checked);

            foreach (DataGridViewRow row in gridSettings.Rows)
            {
                var info = row.Tag as SettingInfo;
                if (info == null)
                    continue;

                ApplySettingFromRow(settings, info, row);
            }

            settings.EnsureObjects();
            return settings;
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
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("ColletCleaningDialog.Start"))
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
            targetGroup.Enabled = enabled;
            gridSettings.Enabled = enabled;
            btnStart.Enabled = enabled;
            btnSelectAll.Enabled = enabled;
            btnSelectNone.Enabled = enabled;
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

                gridHistory.Rows.Clear();
                AppendHistoryRows(data, VisionFocusPickerSide.Front);
                AppendHistoryRows(data, VisionFocusPickerSide.Rear);
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
                gridHistory.Rows.Add(
                    side.ToString(),
                    colletNo.ToString(),
                    record != null && record.HasHistory
                        ? record.LastCleanedAt.ToString("yyyy-MM-dd HH:mm:ss")
                        : "-",
                    record != null ? record.TotalCleanCount.ToString() : "0",
                    record != null ? record.LastRetryUsed.ToString() : "0",
                    record != null ? record.LastResult.ToString() : "None");
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

            lstRunLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            if (lstRunLog.Items.Count > 500)
                lstRunLog.Items.RemoveAt(0);
            lstRunLog.TopIndex = lstRunLog.Items.Count - 1;
        }
    }
}
