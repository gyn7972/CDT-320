using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Common.History;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class EventLogPage : PageBase
    {
        // 알람 행 강조용 폰트. 행마다 new Font 를 만들지 않도록 1회만 생성해 재사용한다.
        private static readonly Font AlarmFont = new Font("Consolas", 10F, FontStyle.Bold);

        // 표시 상한(최신 N개). 최근 1시간 필터와 함께 로딩 부하를 제한한다.
        private const int DefaultMaxRows = 500;
        private const int MaxAllRowsSafeLimit = 10000;
        private const int LiveFlushIntervalMs = 250;
        private const int MaxLiveFlushRows = 100;
        private const int MaxPendingLiveRows = 1000;

        // 이력 로드 스위치 — 설정(Config\settings.json)의 FileLogHistoryEnabled 를 따른다.
        // 설정 탭 GENERAL 의 "LOG SETTINGS" 창 'Log history view' 에서 빌드 없이 켜고 끌 수 있고(응급 차단),
        // 바꾼 값은 이력 페이지를 다시 방문하는 순간 반영된다. 끄면 안내 행만 표시한다.
        private static bool FileLogHistoryEnabled => QMC.CDT320.AppSettingsStore.Current.FileLogHistoryEnabled;

        // 사이드바 버튼(경고/데이터/작업)이 지정하는 초기 Kind 프리셋. null 이면 전체(이벤트).
        private readonly EventKind? _presetKind;
        private readonly object _pendingLiveRowsLock = new object();
        private readonly Queue<EventRow> _pendingLiveRows = new Queue<EventRow>();
        private readonly Timer _liveFlushTimer = new Timer();
        private bool _liveEventSubscribed;
        private bool _initializingFilterControls;
        private bool _updatingHeader;
        private bool _fileSnapshotMode;
        private volatile bool _acceptLiveRows;
        private volatile bool _viewActive;
        private volatile int _reloadVersion;
        private DateTime _readTime;
        private DateTime _nextExpiryCheck;
        private bool _pendingLiveOverflow;

        // 사용자가 직접 연 로그 파일 경로. null 이면 DATE 피커 날짜 기준으로 읽는다.
        private string _overridePath;

        // 첫 컬럼(시간)은 행 헤더처럼 동작한다. Shift 범위 선택의 기준이 되는 직전 클릭 행(-1 이면 없음).
        private int _lastRowClicked = -1;

        // 첫 컬럼(시간) = 행 전체 선택 트리거. 컬럼 순서: [0]When, Kind, ACTOR, Code, Source, [5]Description.
        private const int RowSelectColumnIndex = 0;

        // Description 컬럼(마지막). 더블클릭하면 전체 내용을 큰 창으로 보여준다.
        private const int DescriptionColumnIndex = 5;

        // 필터 조건 스냅샷. 백그라운드 읽기·라이브 이벤트 스레드가 UI 컨트롤을 직접 읽지 않도록
        // UI 스레드에서 떠 둔 값(순수 데이터)만 판정에 사용한다.
        private sealed class EventFilterSnapshot
        {
            public EventKind? PresetKind;
            public bool RecentHourOnly;
            public DateTime? Date;
            public DateTime ReadTime;
            public string RunId = "";
            public string Source = "";
            public string Search = "";

            // 읽기 단계 필터 — 종류/시간 조건만. 파일·메모리에서 최신 N개(캐시)를 모을 때 사용한다.
            public bool PassesRead(EventRow r)
            {
                return PassesAt(r, ReadTime);
            }

            public bool PassesAt(EventRow r, DateTime now)
            {
                return r != null &&
                    (PresetKind == null || r.Kind == PresetKind.Value) &&
                    EventLogDisplayBuffer.MatchesTimeRange(r, Date, RecentHourOnly, now);
            }

            // 표시 단계 텍스트 필터 — 이미 불러온 최신 N개(캐시) 안에서만 검색한다(파일 재읽기 없음).
            public bool PassesText(EventRow r)
            {
                if (r == null)
                    return false;

                if (Source.Length > 0 && IndexOfIgnoreCase(r.Source ?? "", Source) < 0)
                    return false;

                // 번역 조회(ResolveDescription)는 비용이 크므로, 문구 필터가 있을 때만 수행한다.
                if (RunId.Length == 0 && Search.Length == 0)
                    return true;

                string description = ResolveDescription(r);
                if (RunId.Length > 0 &&
                    IndexOfIgnoreCase(description, "run=" + RunId) < 0 &&
                    IndexOfIgnoreCase(description, RunId) < 0)
                {
                    return false;
                }

                if (Search.Length > 0 &&
                    IndexOfIgnoreCase(r.Code ?? "", Search) < 0 &&
                    IndexOfIgnoreCase(r.Source ?? "", Search) < 0 &&
                    IndexOfIgnoreCase(description, Search) < 0)
                {
                    return false;
                }

                return true;
            }
        }

        // 현재 필터 스냅샷(UI 스레드에서만 교체). 라이브 이벤트/백그라운드 읽기가 참조한다.
        private volatile EventFilterSnapshot _filterSnapshot;

        // 백그라운드 재로드 재진입 가드. 읽는 중 조건이 바뀌면 pending 으로 합쳐 마지막 조건으로 1회만 더 읽는다.
        // pending은 UI 스레드가 관리하고, 백그라운드 중단 판정에는 _reloadVersion을 사용한다.
        private bool _reloadRunning;
        private bool _reloadPending;

        // 마지막으로 읽어 온 최신 N개 캐시(종류/시간 조건만 적용된 원본).
        // 텍스트 필터(RunId/Source/Search)는 파일을 다시 읽지 않고 이 캐시 위에서만 동작한다.
        private readonly EventLogDisplayBuffer _displayBuffer = new EventLogDisplayBuffer();

        // 파일 읽기 진행 중 여부 — 진행 중에는 텍스트 필터 타이핑이 '읽는 중' 안내 행을 지우지 않게 한다.
        private bool _fileLoadInProgress;

        public EventLogPage()
            : this(null)
        {
        }

        public EventLogPage(EventKind? presetKind)
        {
            InitializeComponent();
            _presetKind = presetKind;
            InitializeFilterControls();
            // Designer에는 정적으로 작성한 제목을 유지하고 런타임 번역을 조회하지 않는다.
            if (!IsDesignerMode())
                ApplyKindHeader();
            // 이벤트는 항상 연결한다 — 켜짐/꺼짐 판정은 페이지가 보일 때마다(UpdateLiveEventSubscription,
            // ReloadCurrent) 설정값을 다시 읽어 반영하므로, 재시작 없이 토글이 적용된다.
            WireEvents();
        }

        private void InitializeFilterControls()
        {
            _initializingFilterControls = true;
            try
            {
                // 초기 날짜/표시 상한은 이벤트 구독 후에도 오발화하지 않도록 가드 안에서 설정한다.
                _dp.Value = DateTime.Today;
                cmbLimit.SelectedIndex = 0;
            }
            finally
            {
                _initializingFilterControls = false;
            }
        }

        // 설정이 꺼져 있을 때 표시하는 상태 — 필터를 잠그고 안내 행만 남긴다(켜면 SetFilterUiEnabled 로 원복).
        private void ApplyFileLogHistoryDisabledState()
        {
            ResetDisplayCache();
            SetFilterUiEnabled(false);

            if (_grid == null)
                return;

            _grid.Rows.Clear();
            _grid.Rows.Add(
                DateTime.Now.ToString("HH:mm:ss.fff"),
                EventKind.Event,
                "",
                "History",
                "FILE-LOG-DISABLED",
                "로그 이력 화면이 설정에서 꺼져 있습니다. 설정 탭 GENERAL 의 'LOG SETTINGS' 창에서 'Log history view' 를 ENABLE 로 바꾸면 다시 표시됩니다.");
        }

        // 필터/버튼 사용 가능 여부 일괄 전환(토글 켜짐/꺼짐에 따라).
        private void SetFilterUiEnabled(bool enabled)
        {
            if (filterLayout != null)
                filterLayout.Enabled = enabled;

            if (btnRefresh != null)
                btnRefresh.Enabled = enabled;

            if (btnOpenFile != null)
                btnOpenFile.Enabled = enabled;

            if (btnLive != null)
                btnLive.Enabled = enabled;
        }

        // 제목 번역과 현재 조회 모드를 함께 표시한다.
        private void ApplyKindHeader()
        {
            _updatingHeader = true;
            try
            {
                string key = KindToI18n(_presetKind);
                lblHeader.Tag = "i18n:" + key;
                string source = !_fileSnapshotMode ? "실시간 (오늘)" :
                    (_overridePath == null ? "파일 조회" : "파일: " + System.IO.Path.GetFileName(_overridePath));
                lblHeader.Text = Lang.T(key) + " · " + source +
                    (_fileLoadInProgress ? " (파일 읽는 중...)" : "");
            }
            finally
            {
                _updatingHeader = false;
            }
        }

        private void lblHeader_TextChanged(object sender, EventArgs e)
        {
            // 부모의 Lang.Apply가 제목을 번역한 뒤에도 조회 모드를 유지한다.
            // ApplyKindHeader 자체의 Text 변경은 다시 처리하지 않는다.
            if (!IsDesignerMode() && !_updatingHeader)
                ApplyKindHeader();
        }

        private static string KindToI18n(EventKind? kind)
        {
            switch (kind)
            {
                case EventKind.Warning:      return "hist.warning";
                case EventKind.Data:         return "hist.data";
                case EventKind.Work:         return "hist.work";
                case EventKind.Alarm:        return "hist.alarm";
                case EventKind.InputSeq:     return "hist.inputSeq";
                case EventKind.OutputSeq:    return "hist.outputSeq";
                case EventKind.FrontHeadSeq: return "hist.frontHeadSeq";
                case EventKind.RearHeadSeq:  return "hist.rearHeadSeq";
                default:                     return "hist.event";
            }
        }

        private void WireEvents()
        {
            // _liveFlushTimer 는 코드에서 생성한 컴포넌트(디자이너 미등록)라 Tick 구독은 코드 유지.
            _liveFlushTimer.Interval = LiveFlushIntervalMs;
            _liveFlushTimer.Tick += timerLiveFlush_Tick;
            // 페이지 수명 이벤트(라이브 구독 해제/타이머 정리와 짝)라 코드 유지.
            Disposed += (s, e) =>
            {
                _viewActive = false;
                _reloadVersion++;
                _reloadPending = false;
                _acceptLiveRows = false;
                UnsubscribeLiveEvents();
                _liveFlushTimer.Stop();
                _liveFlushTimer.Dispose();
                ClearPendingLiveRows();
            };
        }

        private async void EventLogPage_Load(object sender, EventArgs e)
        {
            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        private async void dp_ValueChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            _overridePath = null;
            _fileSnapshotMode = true;
            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        private async void cmbLimit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        // 이하 표준 컨트롤 이벤트는 디자이너(InitializeComponent)에서 구독한다. Grid_CellClick/Grid_CellDoubleClick 핸들러는 그대로 사용.
        // 텍스트 필터: 파일을 다시 읽지 않고 캐시(최신 N개) 안에서만 즉시 거른다. 세 필터 박스가 같은 동작이라 핸들러를 공유한다.
        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            if (IsDesignerMode())
                return;

            CaptureFilterSnapshot();
            ApplyTextFilterAndDisplay();
        }

        private async void chkRecentHour_CheckedChanged(object sender, EventArgs e)
        {
            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        // REFRESH는 파일 스냅샷이다. 실시간 로그는 섞지 않으며 btnLive로 복귀한다.
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            _fileSnapshotMode = true;
            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        private async void btnOpenFile_Click(object sender, EventArgs e)
        {
            await OpenFileAsync().ConfigureAwait(true);
        }

        private async void btnLive_Click(object sender, EventArgs e)
        {
            if (IsDesignerMode())
                return;

            _overridePath = null;
            _fileSnapshotMode = false;
            SetTodayWithoutReload();
            await ReloadCurrentAsync().ConfigureAwait(true);
        }

        private void SetTodayWithoutReload()
        {
            _initializingFilterControls = true;
            try { _dp.Value = DateTime.Today; }
            finally { _initializingFilterControls = false; }
        }

        private async void timerLiveFlush_Tick(object sender, EventArgs e)
        {
            if (!_viewActive || _fileSnapshotMode || !ShouldRefreshVisible(this))
                return;

            if (_dp.Value.Date != DateTime.Today)
            {
                SetTodayWithoutReload();
                await ReloadCurrentAsync().ConfigureAwait(true);
                return;
            }

            bool overflow;
            lock (_pendingLiveRowsLock)
                overflow = _pendingLiveOverflow;
            if (overflow && !_reloadRunning)
            {
                // UI가 처리할 양을 넘겼다면 기록기의 최신 메모리 범위로 다시 맞춘다.
                await ReloadCurrentAsync().ConfigureAwait(true);
                return;
            }

            FlushPendingLiveRows();
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

        // Description 셀을 더블클릭하면 전체 내용을 큰 창(읽기 전용)으로 보여준다.
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != DescriptionColumnIndex) return;

                string text = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string ?? string.Empty;
                if (text.Length == 0) return;

                using (var dlg = new Ui.Dialogs.TextViewerDialog("DESCRIPTION", FormatDescriptionForDetail(text), false))
                    dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // UI 컨트롤에서 필터 값을 읽어 스냅샷으로 떠 둔다. 반드시 UI 스레드에서 호출한다.
        private EventFilterSnapshot CaptureFilterSnapshot(bool newRead = false)
        {
            if (newRead)
                _readTime = DateTime.Now;

            var snap = new EventFilterSnapshot
            {
                PresetKind = _presetKind,
                Date = _overridePath == null ? (DateTime?)_dp.Value.Date : null,
                ReadTime = _readTime,
                RecentHourOnly = chkRecentHour != null && chkRecentHour.Checked,
                RunId = txtRunId != null ? (txtRunId.Text ?? "").Trim() : "",
                Source = txtSource != null ? (txtSource.Text ?? "").Trim() : "",
                Search = txtSearch != null ? (txtSearch.Text ?? "").Trim() : ""
            };
            _filterSnapshot = snap;
            return snap;
        }

        // 한 번에 파일 작업 하나만 실행한다. 조건이 바뀌면 이전 결과를 버리고 마지막 요청만 읽는다.
        private async Task ReloadCurrentAsync()
        {
            // Load/필터 이벤트가 Designer에서 발생해도 설정과 로그 저장소에는 접근하지 않는다.
            if (IsDesignerMode() || IsDisposed || !ShouldRefreshVisible(this))
                return;

            int version = _reloadVersion;
            try
            {
                _viewActive = true;
                version = ++_reloadVersion;
                _reloadPending = true;
                _acceptLiveRows = false;
                ResetDisplayCache();
                ClearPendingLiveRows();
                CaptureFilterSnapshot(true);
                ApplyKindHeader();

                if (!FileLogHistoryEnabled)
                {
                    StopLiveUpdates();
                    _reloadPending = false;
                    ApplyFileLogHistoryDisabledState();
                    return;
                }

                SetFilterUiEnabled(true);
                if (_fileSnapshotMode)
                    StopLiveUpdates();
                else
                {
                    // 조회 전에 구독해야 조회와 구독 사이에 들어온 행을 놓치지 않는다.
                    _acceptLiveRows = true;
                    SubscribeLiveEvents();
                    _liveFlushTimer.Start();
                }

                if (_reloadRunning)
                    return;

                _reloadRunning = true;
                try
                {
                    do
                    {
                        _reloadPending = false;
                        version = _reloadVersion;
                        int readVersion = version;
                        var snap = _filterSnapshot;
                        string overridePath = _overridePath;
                        DateTime date = _dp.Value.Date;
                        int maxRows = GetEffectiveReadLimit();
                        bool memorySource = !_fileSnapshotMode;
                        try
                        {
                            List<EventRow> source;
                            if (memorySource)
                            {
                                // 기록기 메모리 상한까지 참조를 기억해 조회 후 늦게 도착한 알림도 중복 제외한다.
                                source = EventLogger.ReadRecentMemory(
                                    _presetKind, MaxAllRowsSafeLimit, snap.PassesRead);
                            }
                            else
                            {
                                if (snap.RecentHourOnly && overridePath == null &&
                                    date.AddDays(1) <= snap.ReadTime.AddHours(-1))
                                {
                                    ShowRecentHourFilteredNotice();
                                    continue;
                                }

                                _fileLoadInProgress = true;
                                ApplyKindHeader();
                                ShowFileLoadingNotice();
                                source = await Task.Run(() =>
                                    overridePath != null
                                        ? EventLogger.ReadRecentFileTail(overridePath, maxRows, snap.PassesRead,
                                            () => IsReloadCancelRequested(readVersion), _presetKind)
                                        : EventLogger.ReadRecentTail(date, maxRows, snap.PassesRead,
                                            () => IsReloadCancelRequested(readVersion), _presetKind)).ConfigureAwait(true);
                            }

                            if (IsReloadCancelRequested(readVersion))
                                continue;

                            if (!FileLogHistoryEnabled)
                            {
                                StopLiveUpdates();
                                _reloadPending = false;
                                ApplyFileLogHistoryDisabledState();
                                return;
                            }

                            _fileLoadInProgress = false;
                            LoadRows(source);
                        }
                        catch (Exception ex)
                        {
                            if (!IsReloadCancelRequested(readVersion))
                                ShowReloadError(ex);
                        }
                        finally
                        {
                            if (!IsReloadCancelRequested(readVersion))
                            {
                                _fileLoadInProgress = false;
                                ApplyKindHeader();
                            }
                        }
                    }
                    while (_reloadPending && _viewActive && !IsDisposed);
                }
                finally
                {
                    _reloadRunning = false;
                    _fileLoadInProgress = false;
                }
            }
            catch (Exception ex)
            {
                if (!IsReloadCancelRequested(version))
                    ShowReloadError(ex);
            }
        }

        private void ShowReloadError(Exception ex)
        {
            QMC.Common.MessageDialog.Show(this, "로그를 조회하지 못했습니다.\r\n" + ex.Message,
                "로그 조회", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ResetDisplayCache()
        {
            _displayBuffer.Clear();
            _lastRowClicked = -1;
            _fileLoadInProgress = false;
            if (_grid != null)
                _grid.Rows.Clear();
        }

        // 읽기 결과를 캐시로 보관하고 텍스트 필터를 적용해 표시한다.
        private void LoadRows(List<EventRow> source)
        {
            _displayBuffer.ReplaceRows(source, GetEffectiveReadLimit());
            ApplyTextFilterAndDisplay();
        }

        // 캐시된 최신 N개에 텍스트 필터(RunId/Source/Search)만 적용해 그리드를 다시 그린다.
        // 파일을 다시 읽지 않으므로 검색어 타이핑에 즉시 반응한다.
        private void ApplyTextFilterAndDisplay()
        {
            // 파일 읽기 진행 중에는 '읽는 중' 안내 행을 유지한다(완료 시 LoadRows 가 자동 갱신).
            if (_fileLoadInProgress)
                return;

            var snap = _filterSnapshot ?? CaptureFilterSnapshot();
            int maxRows = GetRowLimit();
            DateTime now = _fileSnapshotMode ? snap.ReadTime : DateTime.Now;
            IReadOnlyList<EventRow> loadedRows = _displayBuffer.Rows;

            // 캐시는 과거→최신 순이므로 뒤(최신)부터 훑어 최신순으로 필요한 만큼만 만든다.
            var rows = new List<DataGridViewRow>();
            for (int i = loadedRows.Count - 1; i >= 0 && (maxRows <= 0 || rows.Count < maxRows); i--)
            {
                var r = loadedRows[i];
                if (!snap.PassesAt(r, now) || !snap.PassesText(r)) continue;
                rows.Add(BuildRow(r));                  // 뒤에서부터 → 이미 최신순
            }

            // 그리드 갱신은 레이아웃/오토사이즈를 멈춘 상태에서 AddRange 로 한 번에 처리한다.
            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                _lastRowClicked = -1;
                _grid.Rows.Clear();
                if (rows.Count > 0) _grid.Rows.AddRange(rows.ToArray());
            }
            finally
            {
                _grid.AutoSizeColumnsMode = prevAutoSize;
                _grid.ResumeLayout();
            }
        }

        // 로그 폴더에서 CSV 파일을 골라 그 내용을 그리드에 로드한다.
        private async Task OpenFileAsync()
        {
            if (IsDesignerMode() || !FileLogHistoryEnabled)
                return;

            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = Lang.T("hist.event");
                    dlg.InitialDirectory = EventLogger.LogDir;
                    dlg.Filter = "Event Log (*.csv)|*.csv|All Files (*.*)|*.*";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    _overridePath = dlg.FileName;
                    _fileSnapshotMode = true;
                    await ReloadCurrentAsync().ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "OPEN FILE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override async void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            await UpdateLiveEventSubscriptionAsync().ConfigureAwait(true);
        }

        private async Task UpdateLiveEventSubscriptionAsync()
        {
            if (IsDesignerMode())
                return;

            _viewActive = ShouldRefreshVisible(this);
            if (_viewActive)
            {
                if (!_fileSnapshotMode)
                    SetTodayWithoutReload();
                await ReloadCurrentAsync().ConfigureAwait(true);
            }
            else
            {
                _reloadVersion++;
                _reloadPending = false;
                StopLiveUpdates();
            }
        }

        private void StopLiveUpdates()
        {
            _acceptLiveRows = false;
            UnsubscribeLiveEvents();
            _liveFlushTimer.Stop();
            ClearPendingLiveRows();
        }

        private void SubscribeLiveEvents()
        {
            if (_liveEventSubscribed)
                return;

            EventLogger.EventLogged += OnLiveEvent;
            _liveEventSubscribed = true;
        }

        private void UnsubscribeLiveEvents()
        {
            if (!_liveEventSubscribed)
                return;

            EventLogger.EventLogged -= OnLiveEvent;
            _liveEventSubscribed = false;
        }

        private void OnLiveEvent(EventRow r)
        {
            if (r == null || !_acceptLiveRows)
                return;

            // 수집은 읽기 단계 필터(종류/시간)만 통과하면 한다 — 텍스트 필터는 표시 시점에 적용되므로
            // 검색어를 지웠을 때 그 사이 들어온 행도 다시 보이도록 캐시에는 남겨 둔다.
            int version = _reloadVersion;
            var snap = _filterSnapshot;
            if (snap == null || !snap.PassesAt(r, DateTime.Now))
                return;

            lock (_pendingLiveRowsLock)
            {
                // 구독 해제 전에 실행되던 이전 조회의 callback도 큐에 다시 넣지 않는다.
                if (!_acceptLiveRows || version != _reloadVersion)
                    return;

                _pendingLiveRows.Enqueue(r);
                while (_pendingLiveRows.Count > MaxPendingLiveRows)
                {
                    _pendingLiveRows.Dequeue();
                    _pendingLiveOverflow = true;
                }
            }
        }

        private void FlushPendingLiveRows()
        {
            if (!_viewActive || _fileSnapshotMode || _reloadRunning ||
                _fileLoadInProgress || !ShouldRefreshVisible(this))
                return;

            // 직접 연 파일을 보는 중이면 실시간 이벤트로 덮지 않는다.
            // 최신순 표시이므로 새 이벤트는 맨 위에 삽입한다.
            if (_overridePath != null || _dp == null || _dp.Value.Date != DateTime.Today)
            {
                ClearPendingLiveRows();
                return;
            }

            var snap = _filterSnapshot;
            if (snap == null)
                return;

            DateTime now = DateTime.Now;
            bool expired = false;
            if (now >= _nextExpiryCheck)
            {
                _nextExpiryCheck = now.AddSeconds(1);
                expired = _displayBuffer.RemoveOutsideRange(snap.Date, snap.RecentHourOnly, now);
            }

            List<EventRow> pending = DequeuePendingLiveRows();
            pending.RemoveAll(row => !snap.PassesAt(row, now));
            List<EventRow> rows = _displayBuffer.AppendRows(pending, GetEffectiveReadLimit());
            if (rows.Count == 0 && !expired)
                return;

            int maxRows = GetEffectiveReadLimit();
            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                _lastRowClicked = -1;
                // 캐시 상한/시간 범위에서 제외된 행은 검색 결과에서도 함께 제거한다.
                for (int i = _grid.Rows.Count - 1; i >= 0; i--)
                {
                    var row = _grid.Rows[i].Tag as EventRow;
                    if (row != null && !_displayBuffer.ContainsRow(row))
                        _grid.Rows.RemoveAt(i);
                }
                foreach (EventRow row in rows)
                {
                    // 화면 표시는 텍스트 필터까지 통과한 행만 (캐시에는 이미 반영됨).
                    if (snap != null && !snap.PassesText(row))
                        continue;

                    _grid.Rows.Insert(0, BuildRow(row));
                    while (maxRows > 0 && _grid.Rows.Count > maxRows)
                        _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
                }
            }
            finally
            {
                _grid.AutoSizeColumnsMode = prevAutoSize;
                _grid.ResumeLayout();
            }
        }

        // 백그라운드 읽기 중단 조건 — 새 요청이 대기 중이거나 페이지가 닫혔으면 더 읽지 않는다.
        // EventLogger 가 백그라운드 스레드에서 주기적으로 호출한다.
        private bool IsReloadCancelRequested(int version)
        {
            return version != _reloadVersion || !_viewActive || IsDisposed;
        }

        // '최근 1시간' 필터 때문에 과거 날짜 결과가 0건으로 확정일 때, 파일을 읽지 않고 이유를 안내한다.
        private void ShowRecentHourFilteredNotice()
        {
            _displayBuffer.Clear();
            _lastRowClicked = -1;
            _grid.Rows.Clear();
            _grid.Rows.Add(
                DateTime.Now.ToString("HH:mm:ss.fff"),
                "",
                "",
                "History",
                "LAST-1HOUR-FILTER",
                "'Last 1 hour' 필터가 켜져 있어 과거 날짜의 로그는 모두 걸러집니다. 체크를 해제하면 해당 날짜의 최신 로그를 표시합니다.");
        }

        // 파일을 읽는 동안 그리드에 안내 행 하나를 표시한다(완료되면 LoadRows 가 결과로 교체).
        private void ShowFileLoadingNotice()
        {
            _grid.Rows.Clear();
            _grid.Rows.Add(
                DateTime.Now.ToString("HH:mm:ss.fff"),
                "",
                "",
                "History",
                "FILE-LOADING",
                "로그 파일을 읽는 중입니다... 완료되면 자동으로 표시됩니다. (화면은 계속 사용할 수 있습니다)");
        }

        private List<EventRow> DequeuePendingLiveRows()
        {
            var rows = new List<EventRow>();
            lock (_pendingLiveRowsLock)
            {
                while (_pendingLiveRows.Count > 0 && rows.Count < MaxLiveFlushRows)
                    rows.Add(_pendingLiveRows.Dequeue());
            }

            return rows;
        }

        private void ClearPendingLiveRows()
        {
            lock (_pendingLiveRowsLock)
            {
                _pendingLiveRows.Clear();
                _pendingLiveOverflow = false;
            }
        }

        // EventRow 하나를 그리드에 넣을 DataGridViewRow 로 변환한다(셀 값 + Kind별 강조 스타일).
        private DataGridViewRow BuildRow(EventRow r)
        {
            string desc = ResolveDescription(r);

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                r.When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                r.Kind.ToString(),
                r.User ?? "",
                r.Code ?? "",
                r.Source ?? "",
                desc);
            row.Tag = r;

            // Kind 별 글씨 색상 — 페이지마다 한 종류만 표시되므로 서로 뚜렷이 구분되는 색을 쓴다(흰 배경에서 가독성 확보).
            switch (r.Kind)
            {
                case EventKind.Event:        // 슬레이트 그레이
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
                    break;
                case EventKind.Warning:      // 오렌지
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(211, 84, 0);
                    break;
                case EventKind.Alarm:        // 레드 + 굵게 강조
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);
                    row.DefaultCellStyle.Font = AlarmFont;
                    break;
                case EventKind.Data:         // 블루
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(41, 128, 185);
                    break;
                case EventKind.Work:         // 브라운
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(121, 85, 72);
                    break;
                case EventKind.InputSeq:     // 그린
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    break;
                case EventKind.FrontHeadSeq: // 퍼플
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(142, 68, 173);
                    break;
                case EventKind.RearHeadSeq:  // 시안/틸
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 131, 143);
                    break;
                case EventKind.OutputSeq:    // 마젠타
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(194, 24, 91);
                    break;
            }

            return row;
        }

        private static string ResolveDescription(EventRow r)
        {
            if (r == null)
                return "";

            // 메시지 카탈로그에 코드가 등록돼 있으면 그 문구(현재 언어)를 우선 표시하고, 없으면 기록된 설명을 그대로 쓴다.
            string lang = Lang.Current ?? "ko";
            return MessageCatalog.Resolve(r.Kind, r.Code, lang, r.Description ?? "");
        }

        private int GetRowLimit()
        {
            if (cmbLimit == null || cmbLimit.SelectedItem == null)
                return DefaultMaxRows;

            string value = cmbLimit.SelectedItem.ToString();
            if (string.Equals(value, "ALL", StringComparison.OrdinalIgnoreCase))
                return 0;

            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : DefaultMaxRows;
        }

        private int GetEffectiveReadLimit()
        {
            int limit = GetRowLimit();
            return limit > 0 ? limit : MaxAllRowsSafeLimit;
        }

        private static int IndexOfIgnoreCase(string text, string value)
        {
            if (text == null || value == null)
                return -1;

            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatDescriptionForDetail(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf('=') < 0)
                return text ?? "";

            List<string> tokens = SplitKeyValueTokens(text);
            if (tokens.Count <= 1)
                return text;

            var sb = new StringBuilder();
            foreach (string token in tokens)
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(token);
            }

            return sb.ToString();
        }

        private static List<string> SplitKeyValueTokens(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text))
                return tokens;

            var sb = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '"')
                    inQuote = !inQuote;

                if (!inQuote && char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    continue;
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens;
        }
    }
}

