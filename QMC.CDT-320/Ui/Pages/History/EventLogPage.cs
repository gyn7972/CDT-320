using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Logging;
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
            public string RunId = "";
            public string Source = "";
            public string Search = "";

            // 읽기 단계 필터 — 종류/시간 조건만. 파일·메모리에서 최신 N개(캐시)를 모을 때 사용한다.
            public bool PassesRead(EventRow r)
            {
                if (r == null)
                    return false;
                if (PresetKind != null && r.Kind != PresetKind.Value)
                    return false;
                if (RecentHourOnly && r.When < DateTime.Now.AddHours(-1))
                    return false;
                return true;
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
        // _reloadPending 은 진행 중인 백그라운드 읽기의 '중단 신호'로도 쓰이므로(다른 스레드가 읽음) volatile.
        private bool _reloadRunning;
        private volatile bool _reloadPending;
        private bool _reloadPendingForceFile;

        // 마지막으로 읽어 온 최신 N개 캐시(종류/시간 조건만 적용된 원본).
        // 텍스트 필터(RunId/Source/Search)는 파일을 다시 읽지 않고 이 캐시 위에서만 동작한다.
        private List<EventRow> _loadedRows = new List<EventRow>();

        // 파일 읽기 진행 중 여부 — 진행 중에는 텍스트 필터 타이핑이 '읽는 중' 안내 행을 지우지 않게 한다.
        private bool _fileLoadInProgress;

        public EventLogPage()
            : this(null)
        {
        }

        public EventLogPage(EventKind? presetKind)
        {
            _presetKind = presetKind;
            InitializeComponent();
            InitializeFilterControls();
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
                cmbLimit.Items.AddRange(new object[] { "500", "2000", "ALL" });
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
        }

        // 프리셋 Kind 에 맞춰 헤더 라벨 i18n 키를 교체한다.
        private void ApplyKindHeader()
        {
            string key = KindToI18n(_presetKind);
            lblHeader.Tag = "i18n:" + key;
            lblHeader.Text = Lang.T(key);
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
            _liveFlushTimer.Tick += (s, e) => FlushPendingLiveRows();
            // 페이지 수명 이벤트(라이브 구독 해제/타이머 정리와 짝)라 코드 유지.
            Disposed += (s, e) =>
            {
                UnsubscribeLiveEvents();
                _liveFlushTimer.Stop();
                _liveFlushTimer.Dispose();
            };
        }

        private void EventLogPage_Load(object sender, EventArgs e)
        {
            ReloadCurrent();
        }

        private void dp_ValueChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            _overridePath = null;
            ReloadCurrent();
        }

        private void cmbLimit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializingFilterControls)
                return;

            ReloadCurrent();
        }

        // 이하 표준 컨트롤 이벤트는 디자이너(InitializeComponent)에서 구독한다. Grid_CellClick/Grid_CellDoubleClick 핸들러는 그대로 사용.
        // 텍스트 필터: 파일을 다시 읽지 않고 캐시(최신 N개) 안에서만 즉시 거른다. 세 필터 박스가 같은 동작이라 핸들러를 공유한다.
        private void FilterTextBox_TextChanged(object sender, EventArgs e)
        {
            CaptureFilterSnapshot();
            ApplyTextFilterAndDisplay();
        }

        private void chkRecentHour_CheckedChanged(object sender, EventArgs e)
        {
            ReloadCurrent();
        }

        // REFRESH 는 파일 강제 재로드 — 재시작 후 '오늘'의 앱 시작 이전 로그까지 파일에서 다시 불러온다.
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ReloadCurrent(true);
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFile();
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

        // 페이지에 지정된 고정 Kind 만 표시한다(프리셋이 없으면 전체 표시).
        // DATE 변경·OPEN FILE 모두 이 필터를 거치므로, 해당 kind 의 로그만 로드된다.
        private bool PassesKindFilter(EventKind kind)
        {
            return _presetKind == null || kind == _presetKind.Value;
        }

        // UI 컨트롤에서 필터 값을 읽어 스냅샷으로 떠 둔다. 반드시 UI 스레드에서 호출한다.
        private EventFilterSnapshot CaptureFilterSnapshot()
        {
            var snap = new EventFilterSnapshot
            {
                PresetKind = _presetKind,
                RecentHourOnly = chkRecentHour != null && chkRecentHour.Checked,
                RunId = txtRunId != null ? (txtRunId.Text ?? "").Trim() : "",
                Source = txtSource != null ? (txtSource.Text ?? "").Trim() : "",
                Search = txtSearch != null ? (txtSearch.Text ?? "").Trim() : ""
            };
            _filterSnapshot = snap;
            return snap;
        }

        // 현재 소스를 다시 읽어 그리드에 채운다.
        // '오늘'(DATE=오늘, 파일 미지정, 강제 아님)은 메모리 최근 버퍼에서 즉시 —
        // 과거 날짜/OPEN FILE/REFRESH(forceFile)는 파일을 백그라운드에서 읽어 UI 를 멈추지 않는다.
        // 읽는 중 조건이 또 바뀌면(pending) 끝난 뒤 최신 조건으로 한 번만 더 읽는다.
        private async void ReloadCurrent(bool forceFile = false)
        {
            if (!FileLogHistoryEnabled)
            {
                ApplyFileLogHistoryDisabledState();
                return;
            }

            try
            {
                CaptureFilterSnapshot();   // 백그라운드에서 컨트롤을 읽지 않도록 UI 스레드에서 먼저 떠 둔다.

                if (_reloadRunning)
                {
                    _reloadPending = true;
                    _reloadPendingForceFile |= forceFile;
                    return;
                }

                _reloadRunning = true;
                try
                {
                    do
                    {
                        _reloadPending = false;
                        bool useFile = forceFile || _reloadPendingForceFile;
                        _reloadPendingForceFile = false;
                        forceFile = false;   // pending 재실행은 그때 요청된 force 여부를 따른다.

                        var snap = _filterSnapshot;
                        string overridePath = _overridePath;
                        DateTime date = _dp.Value.Date;
                        int maxRows = GetEffectiveReadLimit();
                        bool memorySource = !useFile && overridePath == null && date == DateTime.Today;

                        List<EventRow> source;
                        if (memorySource)
                        {
                            // 메모리 버퍼 조회는 즉시 끝나므로 UI 스레드에서 바로 수행한다.
                            source = EventLogger.ReadRecentMemory(_presetKind, maxRows, snap.PassesRead);
                        }
                        else
                        {
                            // 헛읽기 차단 — '최근 1시간' 필터가 켜진 채 과거 날짜를 조회하면 그날 하루 전체가
                            // 이미 컷오프(지금-1시간) 이전이라 결과가 0건으로 확정된다. 파일을 읽지 않고 바로 안내한다.
                            if (snap.RecentHourOnly && overridePath == null &&
                                date.AddDays(1) <= DateTime.Now.AddHours(-1))
                            {
                                ShowRecentHourFilteredNotice();
                                continue;   // pending 요청이 있으면 최신 조건으로 재실행, 없으면 종료.
                            }

                            // 파일 읽기는 백그라운드 + 역방향(tail) — 최신 로그가 파일 끝에 있으므로
                            // 수 GB 파일도 필요한 만큼(보통 끝의 수 MB)만 읽고 끝난다.
                            // 진행 표시는 헤더 + 그리드 안내 행 두 곳에 — 탭 전환 시 i18n 갱신이
                            // 헤더 텍스트를 덮어써도 그리드 안내 행은 유지된다.
                            lblHeader.Text = Lang.T(KindToI18n(_presetKind)) + "  (파일 읽는 중...)";
                            _fileLoadInProgress = true;
                            ShowFileLoadingNotice();
                            try
                            {
                                source = await Task.Run(() =>
                                    overridePath != null
                                        ? EventLogger.ReadRecentFileTail(overridePath, maxRows, snap.PassesRead, IsReloadCancelRequested, _presetKind)
                                        : EventLogger.ReadRecentTail(date, maxRows, snap.PassesRead, IsReloadCancelRequested, _presetKind));
                            }
                            finally
                            {
                                _fileLoadInProgress = false;
                                if (!IsDisposed)
                                    ApplyKindHeader();
                            }

                            // 페이지가 닫혔으면 결과를 버리고 즉시 끝낸다(닫힌 컨트롤 접근 방지).
                            if (IsDisposed)
                                return;

                            // 읽는 중 새 요청이 들어와 중단된 결과(불완전)는 버리고 최신 조건으로 다시 읽는다.
                            if (_reloadPending)
                                continue;
                        }

                        if (IsDisposed)
                            return;

                        LoadRows(source);
                    }
                    while (_reloadPending);
                }
                finally
                {
                    _reloadRunning = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "HISTORY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 읽기 결과를 캐시로 보관하고 텍스트 필터를 적용해 표시한다.
        private void LoadRows(List<EventRow> source)
        {
            _loadedRows = source ?? new List<EventRow>();
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

            // 캐시는 과거→최신 순이므로 뒤(최신)부터 훑어 최신순으로 필요한 만큼만 만든다.
            var rows = new List<DataGridViewRow>();
            for (int i = _loadedRows.Count - 1; i >= 0 && (maxRows <= 0 || rows.Count < maxRows); i--)
            {
                var r = _loadedRows[i];
                if (r == null || !snap.PassesText(r)) continue;
                rows.Add(BuildRow(r));                  // 뒤에서부터 → 이미 최신순
            }

            // 그리드 갱신은 레이아웃/오토사이즈를 멈춘 상태에서 AddRange 로 한 번에 처리한다.
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
        }

        // 로그 폴더에서 CSV 파일을 골라 그 내용을 그리드에 로드한다.
        private void OpenFile()
        {
            if (!FileLogHistoryEnabled)
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
                    ReloadCurrent();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "OPEN FILE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateLiveEventSubscription();
        }

        private void UpdateLiveEventSubscription()
        {
            // 설정이 꺼져 있으면 구독/타이머를 정리하고, 보이는 동안엔 안내만 표시한다(토글 즉시 반영 지점).
            if (!FileLogHistoryEnabled)
            {
                UnsubscribeLiveEvents();
                _liveFlushTimer.Stop();
                ClearPendingLiveRows();
                if (ShouldRefreshVisible(this))
                    ApplyFileLogHistoryDisabledState();
                return;
            }

            if (ShouldRefreshVisible(this))
            {
                // 꺼져 있던 상태에서 켜졌을 수 있으므로 필터 UI 를 다시 활성화한다.
                SetFilterUiEnabled(true);
                // 페이지는 캐시되어 재사용되므로(TabBase.PageCache), 다시 보일 때마다 CSV를 재로드한다.
                // 재로드하지 않으면 페이지가 숨겨진 동안 기록된 이벤트(라이브 큐는 숨김 시 비워짐)가 누락된다.
                ReloadCurrent();
                SubscribeLiveEvents();
                if (!_liveFlushTimer.Enabled)
                    _liveFlushTimer.Start();
            }
            else
            {
                UnsubscribeLiveEvents();
                _liveFlushTimer.Stop();
                ClearPendingLiveRows();
            }
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
            if (r == null)
                return;

            // 수집은 읽기 단계 필터(종류/시간)만 통과하면 한다 — 텍스트 필터는 표시 시점에 적용되므로
            // 검색어를 지웠을 때 그 사이 들어온 행도 다시 보이도록 캐시에는 남겨 둔다.
            var snap = _filterSnapshot;
            if (snap != null ? !snap.PassesRead(r) : !PassesKindFilter(r.Kind))
                return;

            lock (_pendingLiveRowsLock)
            {
                _pendingLiveRows.Enqueue(r);
                while (_pendingLiveRows.Count > MaxPendingLiveRows)
                    _pendingLiveRows.Dequeue();
            }
        }

        private void FlushPendingLiveRows()
        {
            if (!ShouldRefreshVisible(this))
                return;

            // 직접 연 파일을 보는 중이면 실시간 이벤트로 덮지 않는다.
            // 최신순 표시이므로 새 이벤트는 맨 위에 삽입한다.
            if (_overridePath != null || _dp == null || _dp.Value.Date != DateTime.Today)
            {
                ClearPendingLiveRows();
                return;
            }

            List<EventRow> rows = DequeuePendingLiveRows();
            if (rows.Count == 0)
                return;

            // 캐시에도 반영해 두어야 나중에 검색어를 바꿔도 방금 들어온 행이 검색 대상에 포함된다.
            int readLimit = GetEffectiveReadLimit();
            _loadedRows.AddRange(rows);
            while (_loadedRows.Count > readLimit)
                _loadedRows.RemoveAt(0);

            var snap = _filterSnapshot;
            int maxRows = GetEffectiveReadLimit();
            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
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
        private bool IsReloadCancelRequested()
        {
            return _reloadPending || IsDisposed;
        }

        // '최근 1시간' 필터 때문에 과거 날짜 결과가 0건으로 확정일 때, 파일을 읽지 않고 이유를 안내한다.
        private void ShowRecentHourFilteredNotice()
        {
            try
            {
                _grid.Rows.Clear();
                _grid.Rows.Add(
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    "",
                    "",
                    "History",
                    "LAST-1HOUR-FILTER",
                    "'Last 1 hour' 필터가 켜져 있어 과거 날짜의 로그는 모두 걸러집니다. 체크를 해제하면 해당 날짜의 최신 로그를 표시합니다.");
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 파일을 읽는 동안 그리드에 안내 행 하나를 표시한다(완료되면 LoadRows 가 결과로 교체).
        private void ShowFileLoadingNotice()
        {
            try
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
            catch
            {
            }
            finally
            {
            }
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

