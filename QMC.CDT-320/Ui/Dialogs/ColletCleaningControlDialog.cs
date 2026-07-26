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
using QMC.CDT_320.Ui.Controls;
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
            /// <summary>그리드 셀에 마우스를 올리면 표시되는 한글 설명.</summary>
            public string ToolTip;
        }

        private static readonly SettingInfo[] SettingRows =
        {
            new SettingInfo { Key = SettingKey.CleanVelocity, Name = "Clean Z Speed", Unit = "mm/s", Numeric = true,
                ToolTip = "콜렛을 눌러 닦을 때 사용하는 Picker Z축 이동 속도입니다.\r\n" +
                          "값이 클수록 빨리 내려가고 충격이 커집니다. 처음에는 낮은 값으로 검증하세요." },
            new SettingInfo { Key = SettingKey.CleanAcceleration, Name = "Clean Z Acc", Unit = "mm/s2", Numeric = true,
                ToolTip = "누름 동작 Z축 가속도입니다. 속도와 함께 접촉 충격에 영향을 줍니다." },
            new SettingInfo { Key = SettingKey.CleanDeceleration, Name = "Clean Z Dec", Unit = "mm/s2", Numeric = true,
                ToolTip = "누름 동작 Z축 감속도입니다. 접촉 직전 감속에 영향을 줍니다." },
            new SettingInfo { Key = SettingKey.ContactZUserOffset, Name = "Contact Z Offset", Unit = "mm", Numeric = true,
                ToolTip = "접촉 Z 사용자 보정값입니다.\r\n" +
                          "부호 규약: + 값이면 Z축이 상승(덜 누름), - 값이면 Z축이 더 하강(더 누름).\r\n" +
                          "접촉 Z = Place 티칭 Z - 다이 높이 - 림 높이 - 필름 높이 + 이 보정값" },
            new SettingInfo { Key = SettingKey.MaxExtraPressDepth, Name = "Max Extra Press", Unit = "mm", Numeric = true,
                ToolTip = "과압 방지 한계입니다.\r\n" +
                          "계산된 접촉 Z보다 추가로 더 내려갈 수 있는 최대 깊이(양수)이며,\r\n" +
                          "Contact Z Offset이 음수로 이 값을 넘으면 실행을 차단합니다." },
            new SettingInfo { Key = SettingKey.ArriveDwellMs, Name = "Arrive Dwell", Unit = "ms", Numeric = true, Integer = true,
                ToolTip = "접촉 Z 위치에 도착한 뒤 그대로 눌러 유지하는 대기 시간입니다." },
            new SettingInfo { Key = SettingKey.CleanPressCount, Name = "Press Count", Unit = "ea", Numeric = true, Integer = true,
                ToolTip = "콜렛 1개당 눌렀다 떼는 동작을 반복하는 횟수입니다." },
            new SettingInfo { Key = SettingKey.RepeatLiftHeight, Name = "Repeat Lift Height", Unit = "mm", Numeric = true,
                ToolTip = "누름 반복 사이에 다시 올라가는 높이(양수)입니다.\r\n" +
                          "접촉 Z + 이 값 만큼 상승했다가 다시 내려갑니다." },
            new SettingInfo { Key = SettingKey.MoveTimeoutMs, Name = "Move Timeout", Unit = "ms", Numeric = true, Integer = true,
                ToolTip = "축 이동 완료를 기다리는 최대 시간입니다. 초과하면 알람으로 중단합니다." },
            new SettingInfo { Key = SettingKey.DieHeight, Name = "Die Height", Unit = "mm", Numeric = true,
                ToolTip = "Place 티칭 Z에 포함된 다이 높이입니다.\r\n" +
                          "티칭 Z는 이 높이만큼 올라가 있으므로 클리닝 접촉 Z 계산에서 빼줍니다." },
            new SettingInfo { Key = SettingKey.RimHeight, Name = "Rim Height", Unit = "mm", Numeric = true,
                ToolTip = "Place 티칭 Z에 포함된 림(rim) 높이입니다. 접촉 Z 계산에서 빼줍니다." },
            new SettingInfo { Key = SettingKey.FilmHeight, Name = "Film Height", Unit = "mm", Numeric = true,
                ToolTip = "Place 티칭 Z에 포함된 필름 높이입니다. 접촉 Z 계산에서 빼줍니다." },
            new SettingInfo { Key = SettingKey.MaxRetryCount, Name = "Retry On NG", Unit = "ea", Numeric = true, Integer = true,
                ToolTip = "콜렛 검사 결과가 NG일 때 클린 -> 검사를 다시 반복할 최대 횟수입니다.\r\n" +
                          "이 횟수를 모두 쓰고도 NG면 콜렛 교체 알람을 발생시킵니다." },
            new SettingInfo { Key = SettingKey.AllowPlaceOnCleanedCell, Name = "Place On Cleaned Cell", Unit = "", Options = new[] { "True", "False" },
                ToolTip = "클리닝에 사용한 NG 다이맵 셀에 생산 NG die 배치를 허용할지 여부입니다.\r\n" +
                          "True: 허용(해당 셀도 계속 사용)\r\nFalse: 그 셀을 배치 대상에서 제외" },
            new SettingInfo { Key = SettingKey.DisablePickerOnReplaceAlarm, Name = "Disable On Replace", Unit = "", Options = new[] { "True", "False" },
                ToolTip = "콜렛 교체 알람이 발생했을 때 해당 Picker만 생산에서 제외하고\r\n" +
                          "나머지 Picker로 계속 운전할지 여부입니다." },
            new SettingInfo { Key = SettingKey.UseTriggerOnWaferExchange, Name = "Trig Wafer Exchange", Unit = "", Options = new[] { "True", "False" },
                ToolTip = "자동 운전 중 웨이퍼 교체 횟수를 기준으로 콜렛 클리닝을 실행할지 여부입니다." },
            new SettingInfo { Key = SettingKey.WaferExchangeInterval, Name = "  Exchange Interval", Unit = "ea", Numeric = true, Integer = true,
                ToolTip = "웨이퍼 교체 트리거 주기입니다. 교체 n회마다 콜렛 클리닝을 실행합니다." },
            new SettingInfo { Key = SettingKey.UseTriggerOnProcessCount, Name = "Trig Process Count", Unit = "", Options = new[] { "True", "False" },
                ToolTip = "자동 운전 중 공정 처리 수량을 기준으로 콜렛 클리닝을 실행할지 여부입니다." },
            new SettingInfo { Key = SettingKey.ProcessCountInterval, Name = "  Process Interval", Unit = "ea", Numeric = true, Integer = true,
                ToolTip = "공정 횟수 트리거 주기입니다. 아래 Process Unit 기준 n개마다 실행합니다." },
            new SettingInfo { Key = SettingKey.ProcessCountUnit, Name = "  Process Unit", Unit = "", Options = new[] { "Die", "Wafer" },
                ToolTip = "공정 횟수 트리거의 계수 단위입니다.\r\nDie: 다이 개수 기준, Wafer: 웨이퍼 장수 기준" },
            new SettingInfo { Key = SettingKey.UseTriggerOnAutoStart, Name = "Trig Auto Start", Unit = "", Options = new[] { "True", "False" },
                ToolTip = "Auto 운전을 시작할 때(Ready 후 첫 Pick 전) 콜렛 클리닝을 1회 실행할지 여부입니다." }
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
            btnParameterSave.Click += delegate { SaveParameterSettingsFromUi(true); };
            btnReload.Click += delegate { LoadSettingsToUi(); RefreshHistory(); };
            btnClose.Click += delegate { Close(); };
            btnStart.Click += async delegate { await StartCleaningAsync().ConfigureAwait(true); };
            btnStop.Click += delegate { RequestActiveStop("StopButton"); };

            chkTargetAll.CheckedChanged += delegate
            {
                if (!_suppressUiEvents)
                    SetAllTargets(chkTargetAll.Checked);
            };

            // 숫자 항목은 직접 타이핑을 막고(ReadOnly) 더블클릭 시 숫자 키패드로만 수정한다.
            // 옵션(True/False, Die/Wafer) 항목은 콤보 선택이므로 편집을 허용한다.
            gridSettings.CellBeginEdit += gridSettings_CellBeginEdit;
            gridSettings.CellDoubleClick += gridSettings_CellDoubleClick;
            gridSettings.CellToolTipTextNeeded += gridSettings_CellToolTipTextNeeded;
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
            gridSettings.ShowCellToolTips = true;
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

                // CellToolTipTextNeeded 이벤트가 어떤 이유로 동작하지 않는 환경을 대비해
                // 각 셀의 ToolTipText도 직접 채워 둔다(둘 중 하나만 동작해도 설명이 보인다).
                if (!string.IsNullOrWhiteSpace(info.ToolTip))
                {
                    row.Cells[0].ToolTipText = info.ToolTip;
                    row.Cells[1].ToolTipText = info.ToolTip;
                    row.Cells[2].ToolTipText = info.ToolTip;
                }
            }
        }

        private void gridSettings_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridSettings.IsCurrentCellDirty)
                gridSettings.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        /// <summary>숫자 항목은 직접 타이핑을 막는다(키패드로만 수정).</summary>
        private void gridSettings_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            var info = gridSettings.Rows[e.RowIndex].Tag as SettingInfo;
            if (info != null && info.Numeric)
            {
                e.Cancel = true;
                lblStatus.Text = info.Name + " 값은 셀을 더블클릭해서 숫자 키패드로 입력하세요.";
            }
        }

        /// <summary>숫자 항목 더블클릭 시 숫자 키패드로 입력받는다.</summary>
        private void gridSettings_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            var info = row.Tag as SettingInfo;
            if (info == null || !info.Numeric)
                return;

            try
            {
                string current = Convert.ToString(row.Cells[1].Value);
                using (var dialog = new NumericKeypadDialog(info.Name, current, info.Unit))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    double value;
                    if (!double.TryParse(dialog.ValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        lblStatus.Text = info.Name + " 설정값이 숫자가 아닙니다. value=" + dialog.ValueText;
                        return;
                    }

                    _suppressUiEvents = true;
                    try
                    {
                        row.Cells[1].Value = FormatSettingValue(info, value);
                    }
                    finally
                    {
                        _suppressUiEvents = false;
                    }

                    lblStatus.Text = info.Name + " 값을 " + FormatSettingValue(info, value) +
                                     (string.IsNullOrEmpty(info.Unit) ? "" : " " + info.Unit) + "(으)로 입력했습니다. " +
                                     "SAVE를 눌러야 저장됩니다.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "숫자 입력 처리에 실패했습니다. " + ex.Message;
            }
            finally
            {
            }
        }

        /// <summary>파라미터 셀에 마우스를 올리면 한글 설명을 표시한다.</summary>
        private void gridSettings_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = gridSettings.Rows[e.RowIndex];
            var info = row != null ? row.Tag as SettingInfo : null;
            if (info != null && !string.IsNullOrWhiteSpace(info.ToolTip))
                e.ToolTipText = info.ToolTip;
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

        /// <summary>
        /// 앱이 사용 중인 라이브 CalibrationData를 가져온다.
        /// 디스크에서 따로 LoadOrCreate 하면 시퀀스(이력 저장)와 서로 다른 인스턴스를 각각 저장해
        /// 나중에 저장한 쪽이 상대의 값을 덮어써 설정이 기본값으로 되돌아간다.
        /// 다른 캘리브레이션 화면과 동일하게 항상 라이브 객체 한 개만 읽고 쓴다.
        /// </summary>
        private CalibrationData ResolveLiveCalibrationData()
        {
            string reason;
            Form1 host = ResolveHost(out reason);
            CalibrationData data = host != null
                ? CalibrationCoordinateService.ResolveData(host.Machine)
                : null;

            if (data == null)
                data = CalibrationDataStore.LoadOrCreate();

            data.EnsureObjects();
            return data;
        }

        private void LoadSettingsToUi()
        {
            try
            {
                CalibrationData data = ResolveLiveCalibrationData();
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

        private bool SaveParameterSettingsFromUi(bool showMessage)
        {
            try
            {
                ColletCleaningSettings settings = BuildSettingsFromUi();

                CalibrationData data = ResolveLiveCalibrationData();
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

                if (!SaveParameterSettingsFromUi(false))
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

                AppendLog("콜렛 클리닝을 시작합니다. pressCount=" + _settings.CleanPressCount +
                          ", dwell=" + _settings.ArriveDwellMs + "ms" +
                          ", lift=" + _settings.RepeatLiftHeight.ToString("0.###") + "mm");
                lblStatus.Text = "콜렛 클리닝 실행 중입니다.";

                // 화면 값이 시퀀스로 그대로 전달되는지 파일 로그에도 남긴다.
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCleaningDialogStart",
                    "다이얼로그에서 콜렛 클리닝을 시작합니다. pressCount=" + _settings.CleanPressCount +
                    ", arriveDwellMs=" + _settings.ArriveDwellMs +
                    ", repeatLiftHeight=" + _settings.RepeatLiftHeight.ToString("F6") +
                    ", cleanVelocity=" + _settings.CleanVelocity.ToString("F3") +
                    ", contactZUserOffset=" + _settings.ContactZUserOffset.ToString("F6") +
                    ", maxRetry=" + _settings.MaxRetryCount + " - Start");

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
                    // 실행하지 않고 건너뛴 경우를 "완료"로 표시하면 오해가 생기므로 사유를 그대로 노출한다.
                    lblStatus.Text = "[미실행] " + sequence.SkipReason;
                    AppendLog(lblStatus.Text);
                    QMC.Common.MessageDialog.Show(this, sequence.SkipReason, "COLLET CLEANING",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            btnParameterSave.Enabled = enabled;
            btnReload.Enabled = enabled;
            btnClose.Enabled = enabled;
        }

        // ---------------------------------------------------------------- 이력 / 로그

        private void RefreshHistory()
        {
            try
            {
                CalibrationData data = ResolveLiveCalibrationData();

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
