using System;
using System.Collections.Generic;
using System.Drawing;
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
        private const int MaxRows = 500;
        private const int LiveFlushIntervalMs = 250;
        private const int MaxLiveFlushRows = 100;
        private const int MaxPendingLiveRows = 1000;

        // 사이드바 버튼(경고/데이터/작업)이 지정하는 초기 Kind 프리셋. null 이면 전체(이벤트).
        private readonly EventKind? _presetKind;
        private readonly object _pendingLiveRowsLock = new object();
        private readonly Queue<EventRow> _pendingLiveRows = new Queue<EventRow>();
        private readonly Timer _liveFlushTimer = new Timer();
        private bool _liveEventSubscribed;

        // 사용자가 직접 연 로그 파일 경로. null 이면 DATE 피커 날짜 기준으로 읽는다.
        private string _overridePath;

        // 첫 컬럼(시간)은 행 헤더처럼 동작한다. Shift 범위 선택의 기준이 되는 직전 클릭 행(-1 이면 없음).
        private int _lastRowClicked = -1;

        // 첫 컬럼(시간) = 행 전체 선택 트리거. 컬럼 순서: [0]When, Kind, User, Code, Source, [5]Description.
        private const int RowSelectColumnIndex = 0;

        // Description 컬럼(마지막). 더블클릭하면 전체 내용을 큰 창으로 보여준다.
        private const int DescriptionColumnIndex = 5;

        public EventLogPage()
            : this(null)
        {
        }

        public EventLogPage(EventKind? presetKind)
        {
            _presetKind = presetKind;
            InitializeComponent();
            ApplyKindHeader();
            WireEvents();
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
            // 초기 날짜를 오늘로 지정한다. ValueChanged 구독 전에 설정해 중복 로드를 막는다.
            _dp.Value = DateTime.Today;
            // 날짜를 바꾸면 파일 열기 모드를 해제하고 날짜 기준으로 돌아간다.
            _dp.ValueChanged += (s, e) => { _overridePath = null; ReloadCurrent(); };
            btnRefresh.Click += (s, e) => ReloadCurrent();
            btnOpenFile.Click += (s, e) => OpenFile();
            _liveFlushTimer.Interval = LiveFlushIntervalMs;
            _liveFlushTimer.Tick += (s, e) => FlushPendingLiveRows();
            // 행 헤더가 숨겨져 있으므로 첫 컬럼(시간)을 행 헤더처럼 써서 행 전체를 선택한다.
            _grid.CellClick += Grid_CellClick;
            // Description 셀을 더블클릭하면 전체 내용을 큰 창(읽기 전용)으로 보여준다.
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            Disposed += (s, e) =>
            {
                UnsubscribeLiveEvents();
                _liveFlushTimer.Stop();
                _liveFlushTimer.Dispose();
            };
            Load += (s, e) => ReloadCurrent();
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

                using (var dlg = new Ui.Dialogs.TextViewerDialog("DESCRIPTION", text, false))
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

        // 현재 소스(직접 연 파일 또는 DATE 날짜)를 다시 읽어 그리드에 채운다.
        private void ReloadCurrent()
        {
            var source = _overridePath != null
                ? EventLogger.ReadFile(_overridePath)
                : EventLogger.Read(_dp.Value.Date);
            LoadRows(source);
        }

        private void LoadRows(List<EventRow> source)
        {
            // 로딩 부하를 줄이기 위해 (1) 최근 1시간 이내 + (2) 최신 MaxRows(500)개로 제한한다.
            DateTime cutoff = DateTime.Now.AddHours(-1);

            // CSV 는 과거→최신 순이므로 뒤(최신)부터 훑어 최신순으로 최대 500개만 만든다.
            // (필요한 만큼만 BuildRow 하므로 거대 파일에서도 행 생성 비용이 500개로 제한됨)
            var rows = new List<DataGridViewRow>();
            for (int i = source.Count - 1; i >= 0 && rows.Count < MaxRows; i--)
            {
                var r = source[i];
                if (r.When < cutoff) continue;          // 최근 1시간만
                if (!PassesKindFilter(r.Kind)) continue;
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
            if (ShouldRefreshVisible(this))
            {
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
            if (r == null || !PassesKindFilter(r.Kind))
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

            DateTime cutoff = DateTime.Now.AddHours(-1);
            var prevAutoSize = _grid.AutoSizeColumnsMode;
            _grid.SuspendLayout();
            try
            {
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                foreach (EventRow row in rows)
                {
                    if (row == null || row.When < cutoff)
                        continue;

                    _grid.Rows.Insert(0, BuildRow(row));
                    while (_grid.Rows.Count > MaxRows)
                        _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
                }
            }
            finally
            {
                _grid.AutoSizeColumnsMode = prevAutoSize;
                _grid.ResumeLayout();
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
            // 메시지 카탈로그에 코드가 등록돼 있으면 그 문구(현재 언어)를 우선 표시하고, 없으면 기록된 설명을 그대로 쓴다.
            string lang = Lang.Current ?? "ko";
            string desc = MessageCatalog.Resolve(r.Kind, r.Code, lang, r.Description ?? "");

            var row = new DataGridViewRow();
            row.CreateCells(_grid,
                r.When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                r.Kind.ToString(),
                r.User ?? "",
                r.Code ?? "",
                r.Source ?? "",
                desc);

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
    }
}

