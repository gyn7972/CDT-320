using System;
using QMC.CDT_320.Ui.Localization;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class AlarmHistoryPage : PageBase
    {
        private const int MaxRows = 500;
        private const int LiveFlushIntervalMs = 250;
        private const int MaxLiveFlushRows = 50;
        private const int MaxPendingLiveRows = 500;

        // 첫 컬럼(시간) = 행 전체 선택 트리거. 컬럼 순서: [0]시간, Severity, Code, Source, [4]Message, [5]Cause, [6]Action.
        private const int RowSelectColumnIndex = 0;

        // 긴 텍스트 컬럼(Message/Cause/Action) 시작 인덱스. 이 이상 컬럼을 더블클릭하면 전체 내용을 큰 창으로 보여준다.
        private const int LongTextColumnStart = 4;

        private readonly object _pendingAlarmRowsLock = new object();
        private readonly Queue<AlarmRecord> _pendingAlarmRows = new Queue<AlarmRecord>();
        private readonly Timer _liveFlushTimer = new Timer();
        private bool _alarmEventSubscribed;
        private bool _initializingFilterControls;
        // 저장 이력은 화면 전용이다. 현재 알람 목록/해제 판단에 섞지 않는다.
        private IReadOnlyList<AlarmRecord> _savedHistory = new List<AlarmRecord>();
        private DateTime? _loadedHistoryDate;
        private int _historyReadVersion;
        private bool _historyReadRunning;
        private bool _historyReadPending;
        private volatile bool _acceptLiveAlarmRows;
        private bool _followToday = true;
        // 그리드에 이미 렌더된 알람 Id — 라이브 flush 삽입과 LoadGrid 전체 재빌드가 같은 레코드를 중복으로 그리지 않도록 한다.
        private readonly HashSet<int> _renderedAlarmIds = new HashSet<int>();

        // 첫 컬럼(시간)은 행 헤더처럼 동작한다. Shift 범위 선택의 기준이 되는 직전 클릭 행(-1 이면 없음).
        private int _lastRowClicked = -1;

        // Clear 버튼 상태 표시 색 — 지울(미해제) 알람이 있으면 빨강(알람 행과 동일 계열), 없으면 회색(비활성).
        private static readonly Color ClearActiveBack  = Color.FromArgb(192, 57, 43);
        private static readonly Color ClearActiveHover = Color.FromArgb(214, 89, 76);
        private static readonly Color ClearActiveDown  = Color.FromArgb(158, 42, 30);
        private static readonly Color ClearIdleBack    = Color.FromArgb(189, 195, 199);

        public AlarmHistoryPage()
        {
            InitializeComponent();
            Lang.BindKey(btnClear, "historyUi.clearActive");
            ApplyHistoryWhiteSurface();
            InitializeFilterControls();
            WireEvents();

            if (!IsDesignerMode())
            {
                LoadGrid();
            }
        }

        private void InitializeFilterControls()
        {
            _initializingFilterControls = true;
            try
            {
                dtpHistoryDate.Value = DateTime.Today;
                _cbSeverity.Items.Add("(All)");
                foreach (var s in Enum.GetNames(typeof(AlarmSeverity)))
                    _cbSeverity.Items.Add(s);
                _cbSeverity.SelectedIndex = 0;
            }
            finally
            {
                _initializingFilterControls = false;
            }
        }

        private void ApplyHistoryWhiteSurface()
        {
            // 배경색(페이지/rootLayout/filterLayout/_grid White)은 Designer(.Designer.cs)로 이관.
            rootLayout.Margin = Padding.Empty;
            rootLayout.RowStyles[1].Height = 40F;
            lblHeader.Margin = Padding.Empty;
            filterLayout.Margin = Padding.Empty;
            filterLayout.Padding = new Padding(8, 3, 8, 3);

            ConfigureFilterColumns();
            StyleFilterLabel(lblSeverity);
            StyleFilterLabel(lblSearch);
            StyleCountLabel(_lblCount);
            StyleToolbarControl(_cbSeverity);
            StyleToolbarControl(_tbFilter);
            StyleToolbarButton(btnClear, 220);
        }

        private void ConfigureFilterColumns()
        {
            SetFilterColumnWidth(0, 96F);   // Severity
            SetFilterColumnWidth(1, 160F);  // severity option
            SetFilterColumnWidth(2, 82F);   // Search
            SetFilterColumnWidth(3, 330F);  // search text
            SetFilterColumnWidth(4, 70F);   // count
            SetFilterColumnWidth(5, 238F);  // Clear active alarms
        }

        private void SetFilterColumnWidth(int index, float width)
        {
            if (filterLayout == null || index < 0 || index >= filterLayout.ColumnStyles.Count)
                return;

            filterLayout.ColumnStyles[index].SizeType = SizeType.Absolute;
            filterLayout.ColumnStyles[index].Width = width;
        }

        private static void StyleFilterLabel(Label label)
        {
            if (label == null)
                return;

            label.BackColor = Color.FromArgb(245, 247, 249);
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Dock = DockStyle.None;
            label.Anchor = AnchorStyles.Left;
            label.ForeColor = Color.FromArgb(35, 45, 57);
            label.Height = 24;
            label.Margin = new Padding(0, 0, 4, 0);
            label.Padding = new Padding(5, 0, 3, 0);
            label.Width = Math.Max(label.Width, TextRenderer.MeasureText(label.Text ?? "", label.Font).Width + label.Padding.Horizontal + 8);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void StyleCountLabel(Label label)
        {
            if (label == null)
                return;

            label.BackColor = Color.White;
            label.BorderStyle = BorderStyle.None;
            label.Dock = DockStyle.None;
            label.Anchor = AnchorStyles.Left;
            label.ForeColor = Color.FromArgb(35, 45, 57);
            label.Height = 24;
            label.Width = 58;
            label.Margin = new Padding(2, 0, 4, 0);
            label.Padding = new Padding(4, 0, 0, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void StyleToolbarControl(Control control)
        {
            if (control == null)
                return;

            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Margin = new Padding(3, 0, 3, 0);
            control.MinimumSize = Size.Empty;
        }

        private static void StyleToolbarButton(Button button, int minWidth)
        {
            if (button == null)
                return;

            button.AutoSize = false;
            button.AutoEllipsis = false;
            button.Dock = DockStyle.None;
            button.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            button.Height = 24;
            button.Margin = new Padding(4, 0, 4, 0);
            button.MinimumSize = new Size(minWidth, 24);
            button.Padding = new Padding(8, 0, 8, 0);
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void WireEvents()
        {
            _liveFlushTimer.Interval = LiveFlushIntervalMs;
            // 주기 갱신에 Clear 버튼 상태도 편승 — 다른 화면/시퀀스에서 알람이 해제돼도 곧 반영된다.
            _liveFlushTimer.Tick += timerLiveFlush_Tick;
        }

        private async void timerLiveFlush_Tick(object sender, EventArgs e)
        {
            if (IsDesignerMode() || !ShouldRefreshVisible(this))
                return;

            // 오늘 실시간 모드는 자정/재진입에도 오늘을 따라간다. 선택한 과거 날짜는 유지한다.
            if ((_followToday && dtpHistoryDate.Value.Date != DateTime.Today) ||
                (!_acceptLiveAlarmRows && dtpHistoryDate.Value.Date == DateTime.Today))
            {
                _followToday = true;
                await ReloadHistoryAsync();
            }

            FlushPendingAlarmRows();
            UpdateClearButtonState();
        }

        private void _cbSeverity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            LoadGrid();
        }

        private void _tbFilter_TextChanged(object sender, EventArgs e)
        {
            LoadGrid();
        }

        private async void dtpHistoryDate_ValueChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            _followToday = dtpHistoryDate.Value.Date == DateTime.Today;
            await ReloadHistoryAsync();
        }

        private async void btnRefreshHistory_Click(object sender, EventArgs e)
        {
            await ReloadHistoryAsync();
        }

        // 오늘은 기존 메모리/라이브 경로, 다른 날짜는 저장 JSON 조회만 사용한다.
        // 빠른 날짜 변경은 읽기 작업 하나와 마지막 대기 요청 하나로 합친다.
        private async Task ReloadHistoryAsync()
        {
            if (IsDesignerMode() || IsDisposed || dtpHistoryDate == null || !ShouldRefreshVisible(this))
                return;

            if (_followToday && dtpHistoryDate.Value.Date != DateTime.Today)
            {
                _initializingFilterControls = true;
                try { dtpHistoryDate.Value = DateTime.Today; }
                finally { _initializingFilterControls = false; }
            }

            _historyReadVersion++;
            _historyReadPending = dtpHistoryDate.Value.Date != DateTime.Today;
            _acceptLiveAlarmRows = !_historyReadPending;
            _loadedHistoryDate = null;
            _savedHistory = new List<AlarmRecord>();
            ClearPendingAlarmRows();
            LoadGrid();

            if (!_historyReadPending)
            {
                _followToday = true;
                lblHistoryStatus.Text = "오늘 · 실시간";
                return;
            }

            lblHistoryStatus.Text = "이력 파일 읽는 중...";
            if (_historyReadRunning)
                return;

            _historyReadRunning = true;
            try
            {
                while (_historyReadPending && !IsDisposed && ShouldRefreshVisible(this))
                {
                    _historyReadPending = false;
                    int version = _historyReadVersion;
                    DateTime date = dtpHistoryDate.Value.Date;
                    try
                    {
                        // 작업 스레드에서는 UI 컨트롤이나 현재 알람 상태를 변경하지 않는다.
                        var history = await Task.Run(() => AlarmManager.ReadSavedHistory(date, MaxRows));
                        if (!IsCurrentHistoryRead(version, date))
                            continue;

                        _savedHistory = history;
                        _loadedHistoryDate = date;
                        LoadGrid();
                        lblHistoryStatus.Text = history.Count == 0
                            ? "저장된 이력이 없습니다."
                            : "저장 당시 상태 · 최신 500건";
                    }
                    catch (Exception ex)
                    {
                        if (!IsCurrentHistoryRead(version, date))
                            continue;

                        lblHistoryStatus.Text = "조회 실패 · 파일 확인 필요";
                        QMC.Common.MessageDialog.Show(this,
                            date.ToString("yyyy-MM-dd") + " 알람 이력을 조회하지 못했습니다.\r\n" + ex.Message,
                            "알람 이력 조회", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            finally
            {
                _historyReadRunning = false;
            }
        }

        private bool IsCurrentHistoryRead(int version, DateTime date)
        {
            return version == _historyReadVersion && !IsDisposed &&
                ShouldRefreshVisible(this) && dtpHistoryDate.Value.Date == date && date != DateTime.Today;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            AlarmManager.ClearAll();
            LoadGrid();
        }

        // 긴 텍스트 컬럼(Message/Cause/Action)을 더블클릭하면 전체 내용을 큰 창(읽기 전용)으로 보여준다.
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < LongTextColumnStart) return;

                string text = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string ?? string.Empty;
                if (text.Length == 0) return;

                string title = _grid.Columns[e.ColumnIndex].HeaderText;
                if (string.IsNullOrEmpty(title)) title = "DESCRIPTION";

                using (var dlg = new Ui.Dialogs.TextViewerDialog(title, text, false))
                    dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 첫 컬럼(시간)을 행 헤더처럼 다뤄 행 전체를 선택한다. 다른 컬럼은 기본 셀 단위 선택을 유지한다.
        // 일반 클릭=단일 행, Ctrl+클릭=행 토글(다중), Shift+클릭=직전 클릭 행부터 범위 선택.
        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != RowSelectColumnIndex)
                return;

            Keys mod = Control.ModifierKeys;

            if ((mod & Keys.Control) == Keys.Control)
            {
                // Ctrl+클릭: 해당 행 선택을 토글하고 기존 선택은 유지한다.
                _grid.Rows[e.RowIndex].Selected = !_grid.Rows[e.RowIndex].Selected;
            }
            else if ((mod & Keys.Shift) == Keys.Shift && _lastRowClicked >= 0)
            {
                // Shift+클릭: 직전 클릭 행부터 현재 행까지 범위 선택.
                _grid.ClearSelection();
                int from = Math.Min(_lastRowClicked, e.RowIndex);
                int to   = Math.Max(_lastRowClicked, e.RowIndex);
                for (int i = from; i <= to && i < _grid.Rows.Count; i++)
                    _grid.Rows[i].Selected = true;
            }
            else
            {
                // 일반 클릭: 그 행만 선택한다.
                _grid.ClearSelection();
                _grid.Rows[e.RowIndex].Selected = true;
            }

            _lastRowClicked = e.RowIndex;
        }

        // 전체 재생성 — 초기 로드 / 필터 변경 / Clear 같은 사용자 액션에서만 호출한다.
        private void LoadGrid()
        {
            try
            {
                _renderedAlarmIds.Clear();
                _lastRowClicked = -1;
                var rows = new List<DataGridViewRow>();
                DateTime date = dtpHistoryDate.Value.Date;
                IEnumerable<AlarmRecord> history = date == DateTime.Today
                    ? AlarmManager.History.Reverse().Take(MaxRows)
                    : (_loadedHistoryDate == date ? _savedHistory : Enumerable.Empty<AlarmRecord>());
                foreach (var a in history) // 오늘 메모리/저장 파일 모두 최신순
                {
                    if (!PassesFilter(a)) continue;
                    rows.Add(BuildRow(a));
                    _renderedAlarmIds.Add(a.Id);
                }

                var prevAutoSize = _grid.AutoSizeColumnsMode;
                _grid.SuspendLayout();
                try
                {
                    _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                    _grid.Rows.Clear();
                    if (rows.Count > 0) _grid.Rows.AddRange(rows.ToArray());
                }
                finally
                {
                    _grid.AutoSizeColumnsMode = prevAutoSize;
                    _grid.ResumeLayout();
                }

                UpdateCount();
            }
            catch
            {
            }
        }

        // 현재 Severity / 검색어 필터를 통과하는지.
        private bool PassesFilter(AlarmRecord a)
        {
            if (a == null) return false;
            // 선택 날짜만 표시한다. 저장 이력의 미해제 표시는 저장 당시 상태다.
            if (a.Raised.Date != dtpHistoryDate.Value.Date) return false;
            string sev = _cbSeverity?.SelectedItem?.ToString() ?? "(All)";
            if (sev != "(All)" && a.Severity.ToString() != sev) return false;

            string filter = (_tbFilter?.Text ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(filter)
                && (a.Code ?? "").ToLowerInvariant().IndexOf(filter) < 0
                && (a.Source ?? "").ToLowerInvariant().IndexOf(filter) < 0
                && (a.Message ?? "").ToLowerInvariant().IndexOf(filter) < 0)
            {
                return false;
            }
            return true;
        }

        // 알람 1건 → 셀 값 + 심각도 배경색을 가진 행.
        private DataGridViewRow BuildRow(AlarmRecord a)
        {
            var def = AlarmMaster.Get(a.Code);
            string lang = Localization.Lang.Current ?? "ko";
            string message = ResolveAlarmMessage(a, def, lang);
            string cause = ResolveAlarmCause(a, def, lang, message);
            string action = ResolveAlarmAction(a, def, lang, message);

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                a.Raised.ToString("HH:mm:ss.fff"),
                a.Severity,
                a.Code,
                a.Source ?? "",
                message,
                cause,
                action);
            row.Tag = a.Id; // 행 ↔ 알람 식별 (후속 상태 표시용)

            // 활성/해제 시각 구분: 해제된 알람은 흐리게(회색), 활성은 심각도에 따라 강조.
            // (복원된 이전 세션 이력도 이 규칙으로 활성/해제가 구분되어 보인다.)
            if (a.Cleared.HasValue)
            {
                row.DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
            }
            else
            {
                row.DefaultCellStyle.ForeColor = a.Severity >= AlarmSeverity.Critical
                    ? System.Drawing.Color.FromArgb(192, 57, 43)    // 치명적 — 레드
                    : System.Drawing.Color.FromArgb(44, 62, 80);    // 활성 — 진한 남색
            }

            return row;
        }

        private static string ResolveAlarmMessage(AlarmRecord alarm, AlarmDefinition definition, string lang)
        {
            try
            {
                string rawMessage = alarm != null ? alarm.Message ?? string.Empty : string.Empty;
                if (!string.IsNullOrWhiteSpace(rawMessage))
                    return rawMessage;

                string resolved = MessageCatalog.Resolve(EventKind.Alarm, alarm != null ? alarm.Code : string.Empty, lang, rawMessage);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;

                return definition != null ? definition.GetTitle(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return alarm != null ? alarm.Message ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static string ResolveAlarmCause(AlarmRecord alarm, AlarmDefinition definition, string lang, string message)
        {
            try
            {
                string code = alarm != null ? alarm.Code ?? string.Empty : string.Empty;
                string source = alarm != null ? alarm.Source ?? string.Empty : string.Empty;
                message = message ?? string.Empty;

                if (IsInterlockAlarm(code))
                {
                    if (HasDetailedMessage(message))
                        return "인터락 차단 사유: " + message;

                    return "인터락 조건이 만족되지 않아 " + source + " 동작이 차단되었습니다.";
                }

                if (IsReticleCylinderAlarm(code))
                {
                    if (HasDetailedMessage(message))
                        return "Reticle 실린더 동작 실패 사유: " + message;

                    return "Reticle 실린더가 목표 센서 상태에 도달하지 못했습니다.";
                }

                return definition != null ? definition.GetCause(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return definition != null ? definition.GetCause(lang) ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static string ResolveAlarmAction(AlarmRecord alarm, AlarmDefinition definition, string lang, string message)
        {
            try
            {
                string code = alarm != null ? alarm.Code ?? string.Empty : string.Empty;
                string source = alarm != null ? alarm.Source ?? string.Empty : string.Empty;

                if (IsInterlockAlarm(code))
                    return "Cause의 차단 사유에 나온 축/실린더/센서 상태를 안전 위치로 복구한 뒤 알람 리셋 후 재시도하세요. 대상=" + source;

                if (IsReticleCylinderAlarm(code))
                    return "Reticle Lift/Slide 센서와 Picker/Camera 회피 위치를 확인하고, Cause의 인터락 사유를 먼저 해소한 뒤 다시 실행하세요.";

                return definition != null ? definition.GetAction(lang) ?? string.Empty : string.Empty;
            }
            catch
            {
                return definition != null ? definition.GetAction(lang) ?? string.Empty : string.Empty;
            }
            finally
            {
            }
        }

        private static bool IsInterlockAlarm(string code)
        {
            return string.Equals(code ?? string.Empty, "INTERLOCK", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(code ?? string.Empty, "INTERLOCK-GUARD", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReticleCylinderAlarm(string code)
        {
            return (code ?? string.Empty).StartsWith("VS-RETICLE-CYL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasDetailedMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string normalized = message.Trim();
            return !string.Equals(normalized, "인터록 차단", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(normalized, "Interlock blocked", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateCount()
        {
            if (_lblCount != null) _lblCount.Text = "(" + _grid.Rows.Count + ")";
            UpdateClearButtonState();
        }

        // 미해제 알람 유무/개수를 Clear 버튼의 색·문구·활성화에 반영한다.
        // 판정은 복원된(이전 세션) 알람 포함 — 이 버튼의 ClearAll 이 지우는 대상과 같은 기준이라
        // "그리드엔 활성 행이 보이는데 버튼은 회색"인 모순이 생기지 않는다.
        private void UpdateClearButtonState()
        {
            try
            {
                if (btnClear == null)
                    return;

                int active = 0;
                foreach (var a in AlarmManager.History)
                {
                    if (a != null && a.IsActive)
                        active++;
                }

                if (active > 0)
                {
                    btnClear.Enabled = true;
                    btnClear.BackColor = ClearActiveBack;
                    btnClear.ForeColor = Color.White;
                    btnClear.FlatAppearance.MouseOverBackColor = ClearActiveHover;
                    btnClear.FlatAppearance.MouseDownBackColor = ClearActiveDown;
                    Lang.BindFormat(btnClear, "historyUi.clearActiveCount", active);
                }
                else
                {
                    // 지울 알람이 없으면 회색 + 비활성화.
                    btnClear.Enabled = false;
                    btnClear.BackColor = ClearIdleBack;
                    btnClear.ForeColor = Color.White;
                    Lang.BindKey(btnClear, "historyUi.clearActive");
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 새 알람은 전체 재생성 없이 맨 위에 1행만 끼워넣는다 → 사용자의 선택이 유지된다.
        private void OnRaise(AlarmRecord r)
        {
            if (r == null || !_acceptLiveAlarmRows)
                return;

            lock (_pendingAlarmRowsLock)
            {
                _pendingAlarmRows.Enqueue(r);
                while (_pendingAlarmRows.Count > MaxPendingLiveRows)
                    _pendingAlarmRows.Dequeue();
            }
        }

        protected override async void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            await UpdateAlarmEventSubscriptionAsync();
        }

        private async Task UpdateAlarmEventSubscriptionAsync()
        {
            if (IsDesignerMode() || dtpHistoryDate == null)
                return;

            if (ShouldRefreshVisible(this))
            {
                SubscribeAlarmEvents();
                if (!_liveFlushTimer.Enabled)
                    _liveFlushTimer.Start();
                // 캐시된 페이지 재진입 시에도 현재 선택 날짜를 유지하며 다시 조회한다.
                await ReloadHistoryAsync();
            }
            else
            {
                _historyReadVersion++;
                _historyReadPending = false;
                _acceptLiveAlarmRows = false;
                UnsubscribeAlarmEvents();
                _liveFlushTimer.Stop();
                ClearPendingAlarmRows();
            }
        }

        private void SubscribeAlarmEvents()
        {
            if (_alarmEventSubscribed)
                return;

            AlarmManager.AlarmRaised += OnRaise;
            _alarmEventSubscribed = true;
        }

        private void UnsubscribeAlarmEvents()
        {
            if (!_alarmEventSubscribed)
                return;

            AlarmManager.AlarmRaised -= OnRaise;
            _alarmEventSubscribed = false;
        }

        private void FlushPendingAlarmRows()
        {
            if (!ShouldRefreshVisible(this))
                return;

            if (dtpHistoryDate.Value.Date != DateTime.Today)
            {
                ClearPendingAlarmRows();
                return;
            }

            List<AlarmRecord> rows = DequeuePendingAlarmRows();
            if (rows.Count == 0)
                return;

            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                foreach (AlarmRecord row in rows)
                {
                    if (!PassesFilter(row))
                        continue;

                    if (!_renderedAlarmIds.Add(row.Id))            // LoadGrid/이전 flush가 이미 그린 레코드면 중복 삽입 방지
                        continue;

                    _grid.Rows.Insert(0, BuildRow(row));           // 최신이 맨 위
                    while (_grid.Rows.Count > MaxRows)             // 상한 유지
                    {
                        int lastIdx = _grid.Rows.Count - 1;
                        object trimmedTag = _grid.Rows[lastIdx].Tag;
                        if (trimmedTag is int)
                            _renderedAlarmIds.Remove((int)trimmedTag);
                        _grid.Rows.RemoveAt(lastIdx);
                    }
                }

                UpdateCount();
            }
            catch
            {
            }
            finally
            {
                _grid.AutoSizeColumnsMode = prevAutoSize;
                _grid.ResumeLayout();
            }
        }

        private List<AlarmRecord> DequeuePendingAlarmRows()
        {
            var rows = new List<AlarmRecord>();
            lock (_pendingAlarmRowsLock)
            {
                while (_pendingAlarmRows.Count > 0 && rows.Count < MaxLiveFlushRows)
                    rows.Add(_pendingAlarmRows.Dequeue());
            }

            return rows;
        }

        private void ClearPendingAlarmRows()
        {
            lock (_pendingAlarmRowsLock)
            {
                _pendingAlarmRows.Clear();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _historyReadVersion++;
            _historyReadPending = false;
            _acceptLiveAlarmRows = false;
            try
            {
                UnsubscribeAlarmEvents();
                _liveFlushTimer.Stop();
                _liveFlushTimer.Dispose();
            }
            catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

