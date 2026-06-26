using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Vision.Config;
using QMC.Vision.Ui.Controls;   // GridTheme, SortableBindingList
using QMC.Vision.Ui.Localization; // Lang

namespace QMC.Vision.Ui.Pages
{
    /// <summary>이력(History) — 4탭 실데이터 연동.
    /// • Data Log : 일자별 검사 CSV(DataLogSaver: {DataLogPath}\vision_yyyyMMdd.csv) → DataGridView.
    /// • Log      : EventLogger.Read(date) (QMC.Common.Logging).
    /// • Alarm    : AlarmManager.History / Active (QMC.Common.Alarms).
    /// • Utility  : 로그 폴더 열기 / 전체 새로고침.
    /// 정적 UI 골격은 .Designer.cs, 데이터 로직은 본 파일(AGENTS 디자이너 규칙).</summary>
    public partial class DataLogPage : PageBase
    {
        // Data Log 고정 칼럼(DataLogSaver: Timestamp + 30 항목) — 파일이 없어도 헤더는 항상 표시.
        private static readonly string[] DataLogColumns =
        {
            "Timestamp",
            "Material_ID", "Loading_Substrate_ID", "Loading_Substrate_X", "Loading_Substrate_Y",
            "Unloading_Substrate_ID", "Unloading_Substrate_X", "Unloading_Substrate_Y",
            "Die_Width", "Die_Height",
            "ChipLowerSpecLimitWidth", "ChipUpperSpecLimitWidth", "ChipLowerSpecLimitHeight", "ChipUpperSpecLimitHeight",
            "Back_Chipping_Top_Size", "Back_Chipping_Right_Size", "Back_Chipping_Bottom_Size", "Back_Chipping_Left_Size", "Back_Chipping_Length",
            "Side_Chipping_Bottom", "Side_Chipping_Left", "Side_Chipping_Top", "Side_Chipping_Right", "Side_Chipping_Length",
            "Back_Foreign_Size", "ForeignObjectSize",
            "Post_Place_Top_Gap_Avg", "Post_Place_Bottom_Gap_Avg", "Post_Place_Left_Gap_Avg", "Post_Place_Right_Gap_Avg",
            "Post_Place_Gap_UpperLimit", "Post_Place_Gap_LowerLimit",
        };

        private bool _initialized;
        private bool _langHooked;   // LanguageChanged 중복 구독 방지

        // Data Log 정렬/검색 상태
        private List<string> _dataHeaders;
        private List<string[]> _dataRowsAll;
        private int  _dataSortCol = -1;
        private bool _dataSortAsc = true;

        // 페이지네이션 + Log/Alarm 전체 캐시·정렬 상태
        private PaginationBar _pagerData, _pagerLog, _pagerAlarm;
        private List<EventRow>    _logRaw;       // 파일 원본(필터 전) — 검색 시 재읽기 방지용 캐시
        private List<EventRow>    _logRowsAll;   // 현재 뷰(필터+정렬 적용)
        private List<AlarmRecord> _alarmRowsAll;
        private string _logSortProp;   private bool _logSortAsc   = true;
        private string _alarmSortProp; private bool _alarmSortAsc = true;

        // 대량 데이터 비동기 로드 취소 토큰(마지막 요청만 반영) — 클릭 즉시 반응 + UI 멈춤 방지
        private CancellationTokenSource _dataCts;
        private CancellationTokenSource _logCts;

        public DataLogPage()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;
            GridTheme.Apply(_gridData);
            GridTheme.Apply(_gridLog);
            GridTheme.Apply(_gridAlarm);
            _gridData.ColumnHeaderMouseClick += GridData_HeaderClick;   // Data Log 헤더 클릭 정렬(언바운드)

            // Log·Alarm 칼럼: Description/Message 는 Fill(가장 크게), 나머지는 내용 맞춤 — DataBindingComplete 에서 적용.
            _gridLog.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None;
            _gridAlarm.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // 페이지네이션 바(각 탭 하단) — 페이지 크기 선택 + 번호 입력/이동 + 이전·다음
            _pagerData  = new PaginationBar(); _pagerData.PageChanged  += (s, e) => RenderDataGrid();  _tpData.Controls.Add(_pagerData);
            _pagerLog   = new PaginationBar(); _pagerLog.PageChanged   += (s, e) => RenderLogPage();   _tpLog.Controls.Add(_pagerLog);
            _pagerAlarm = new PaginationBar(); _pagerAlarm.PageChanged += (s, e) => RenderAlarmPage(); _tpAlarm.Controls.Add(_pagerAlarm);

            // 검색 시 1페이지로(디자이너 핸들러 뒤에 추가)
            _txtDataSearch.TextChanged += (s, e) => { _pagerData.Reset(); RenderDataGrid(); };

            // Log·Alarm 전역 정렬(헤더 클릭) — 페이지 넘어가도 정렬 유지
            _gridLog.ColumnHeaderMouseClick   += GridLog_HeaderClick;
            _gridAlarm.ColumnHeaderMouseClick += GridAlarm_HeaderClick;

            // List 바인딩(IBindingList 아님) → 자동정렬 예외 방지 + 칼럼 폭 적용.
            _gridLog.DataBindingComplete   += (s, e) => ApplyGridColumnLayout(_gridLog);
            _gridAlarm.DataBindingComplete += (s, e) => ApplyGridColumnLayout(_gridAlarm);
        }

        /// <summary>List 바인딩 그리드 칼럼: 정렬모드 Programmatic + 폭(Description/Message=Fill 최대, 나머지=AllCells 내용맞춤).</summary>
        private static void ApplyGridColumnLayout(DataGridView g)
        {
            foreach (DataGridViewColumn c in g.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.Programmatic;
                string key = string.IsNullOrEmpty(c.DataPropertyName) ? c.Name : c.DataPropertyName;
                bool isDesc = string.Equals(key, "Description", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(key, "Message", StringComparison.OrdinalIgnoreCase);
                c.AutoSizeMode = isDesc ? DataGridViewAutoSizeColumnMode.Fill
                                        : DataGridViewAutoSizeColumnMode.DisplayedCells;   // 보이는 행만 측정(버벅임 방지)
            }
        }

        /// <summary>문자열에 검색어가 포함되는지(대소문자 무시).</summary>
        private static bool Contains(string s, string q)
            => !string.IsNullOrEmpty(s) && s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

        // ── EventRow 타입 검색/정렬(리플렉션 제거 — 대량 정렬 성능) ──
        private static bool EventMatches(EventRow r, string q)
        {
            if (r == null) return false;
            return Contains(r.When.ToString("yyyy-MM-dd HH:mm:ss"), q)
                || Contains(r.Kind.ToString(), q)
                || Contains(r.User, q)
                || Contains(r.Code, q)
                || Contains(r.Source, q)
                || Contains(r.Description, q);
        }

        private static int CompareEvent(EventRow a, EventRow b, string prop, bool asc)
        {
            int cmp;
            switch (prop)
            {
                case "When":        cmp = a.When.CompareTo(b.When); break;
                case "Kind":        cmp = string.Compare(a.Kind.ToString(), b.Kind.ToString(), StringComparison.OrdinalIgnoreCase); break;
                case "User":        cmp = string.Compare(a.User, b.User, StringComparison.OrdinalIgnoreCase); break;
                case "Code":        cmp = string.Compare(a.Code, b.Code, StringComparison.OrdinalIgnoreCase); break;
                case "Source":      cmp = string.Compare(a.Source, b.Source, StringComparison.OrdinalIgnoreCase); break;
                case "Description": cmp = string.Compare(a.Description, b.Description, StringComparison.OrdinalIgnoreCase); break;
                default:            cmp = 0; break;
            }
            return asc ? cmp : -cmp;
        }

        // ── AlarmRecord 타입 검색/정렬 ──
        private static bool AlarmMatches(AlarmRecord r, string q)
        {
            if (r == null) return false;
            return Contains(r.Code, q)
                || Contains(r.Source, q)
                || Contains(r.Message, q)
                || Contains(r.Severity.ToString(), q)
                || Contains(r.Raised.ToString("yyyy-MM-dd HH:mm:ss"), q)
                || Contains(r.Id.ToString(), q);
        }

        private static int CompareAlarm(AlarmRecord a, AlarmRecord b, string prop, bool asc)
        {
            int cmp;
            switch (prop)
            {
                case "Id":       cmp = a.Id.CompareTo(b.Id); break;
                case "Raised":   cmp = a.Raised.CompareTo(b.Raised); break;
                case "Cleared":  cmp = Nullable.Compare(a.Cleared, b.Cleared); break;
                case "Severity": cmp = ((int)a.Severity).CompareTo((int)b.Severity); break;
                case "Code":     cmp = string.Compare(a.Code, b.Code, StringComparison.OrdinalIgnoreCase); break;
                case "Source":   cmp = string.Compare(a.Source, b.Source, StringComparison.OrdinalIgnoreCase); break;
                case "Message":  cmp = string.Compare(a.Message, b.Message, StringComparison.OrdinalIgnoreCase); break;
                case "IsActive": cmp = a.IsActive.CompareTo(b.IsActive); break;
                default:         cmp = 0; break;
            }
            return asc ? cmp : -cmp;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (IsDesignerMode() || _initialized) return;
            _dtData.Value = DateTime.Today;
            _dtLog.Value  = DateTime.Today;
            _initialized  = true;
            SetDatePickerRanges();   // 로그 있는 일자 범위로 제한(범위 밖 비활성) + 오늘 동그라미(기본)

            // 언어 동기화 — 현재 언어로 표시 문구 적용 + 변경 구독(1회).
            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();

            RefreshAll();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_langHooked) { Lang.LanguageChanged -= OnLanguageChanged; _langHooked = false; }
            try { _logCts?.Cancel(); _dataCts?.Cancel(); }   // 진행 중 비동기 로드 취소(폐기 후 콜백 방지)
            catch (Exception ex) { Debug.WriteLine("[DataLogPage] OnHandleDestroyed 취소 실패: " + ex.Message); }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 이벤트 — UI 스레드로 마샬링 후 표시 문구 재적용.</summary>
        private void OnLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)ApplyLanguage); } catch { } return; }
            ApplyLanguage();
        }

        /// <summary>현재 언어로 헤더/탭/바(라벨·버튼·체크박스)/빈 안내 문구를 적용.</summary>
        private void ApplyLanguage()
        {
            _tpData.Text    = Lang.T("hist.tab.data");
            _tpLog.Text     = Lang.T("hist.tab.log");
            _tpAlarm.Text   = Lang.T("hist.tab.alarm");
            _tpUtility.Text = Lang.T("hist.tab.utility");
            UpdateHeader();

            _lblDataDate.Text = Lang.T("common.date");
            _lblLogDate.Text  = Lang.T("common.date");
            _btnDataReload.Text = Lang.T("hist.queryRefresh");
            _btnLogReload.Text  = Lang.T("hist.queryRefresh");
            _btnDataExport.Text = Lang.T("hist.csvExport");
            _chkActiveOnly.Text = Lang.T("hist.activeOnly");
            _btnAlarmReload.Text = Lang.T("common.refresh");

            _emptyData.Text  = Lang.T("hist.empty");
            _emptyLog.Text   = Lang.T("hist.empty");
            _emptyAlarm.Text = Lang.T("hist.empty");

            _btnOpenDataFolder.Text = Lang.T("hist.openDataFolder");
            _btnOpenLogFolder.Text  = Lang.T("hist.openLogFolder");
            _btnRefreshAll.Text     = Lang.T("hist.refreshAll");
        }

        /// <summary>헤더 = "이력 — &lt;선택 탭&gt;" (현재 언어).</summary>
        private void UpdateHeader()
        {
            string tab = _tabs.SelectedTab != null ? _tabs.SelectedTab.Text : Lang.T("hist.tab.data");
            _hdr.Text = Lang.T("tab.history") + " — " + tab;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!IsDesignerMode() && Visible && _initialized) RefreshAll();
        }

        private void RefreshAll()
        {
            LoadDataLog();
            LoadEventLog();
            LoadAlarms();
        }

        // ── 날짜 선택기 = 실제 로그가 있는 일자만 선택 가능(굵게 표시 + 최근일 기본 선택) ──
        private void SetDatePickerRanges()
        {
            try
            {
                var cfg = VisionConfigStore.Load();
                string dataDir = (cfg == null || string.IsNullOrEmpty(cfg.DataLogPath)) ? @".\Log\Data" : cfg.DataLogPath;

                var dataDates = CollectDates(dataDir, "vision_*.csv");       // 검사 데이터: vision_yyyyMMdd.csv
                var logDates  = CollectDates(EventLogger.LogDir, "*.csv");   // 이벤트 로그: yyyy-MM-dd.csv

                _dtData.SetAvailableDates(dataDates);
                _dtLog.SetAvailableDates(logDates);

                // 로그가 있는 가장 최근 날짜를 기본 선택(없으면 오늘 유지) → 첫 화면 빈 그리드 방지.
                var latestData = _dtData.GetLatestAvailable();
                if (latestData.HasValue) _dtData.SetValueSilent(latestData.Value);
                var latestLog = _dtLog.GetLatestAvailable();
                if (latestLog.HasValue) _dtLog.SetValueSilent(latestLog.Value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DataLogPage] SetDatePickerRanges 실패: " + ex.Message);
            }
        }

        private static List<DateTime> CollectDates(string dir, string pattern)
        {
            var dates = new List<DateTime>();
            try
            {
                if (Directory.Exists(dir))
                    foreach (var f in Directory.GetFiles(dir, pattern))
                    {
                        var d = ExtractDate(Path.GetFileNameWithoutExtension(f));
                        if (d.HasValue) dates.Add(d.Value);
                    }
            }
            catch { }
            return dates;
        }

        /// <summary>파일명에서 날짜를 해석. yyyy-MM-dd(이벤트 로그) 우선, 없으면 yyyyMMdd 8자리(데이터 로그).</summary>
        private static DateTime? ExtractDate(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // 1) yyyy-MM-dd : 이벤트 로그(예: 2026-06-22.csv)
            var m = System.Text.RegularExpressions.Regex.Match(name, @"\d{4}-\d{2}-\d{2}");
            if (m.Success &&
                DateTime.TryParseExact(m.Value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var dDash))
                return dDash;

            // 2) yyyyMMdd : 데이터 로그(예: vision_20260622.csv)
            for (int i = 0; i + 8 <= name.Length; i++)
            {
                string sub = name.Substring(i, 8);
                if (sub.All(char.IsDigit) &&
                    DateTime.TryParseExact(sub, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;
            }
            return null;
        }

        private void _tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateHeader();
        }

        // ── Data Log (vision_yyyyMMdd.csv) — 비동기 파싱(취소 토큰) + 캐시 렌더 ──
        private async void LoadDataLog()
        {
            string file = DataLogFilePath(_dtData.Value);
            _dataCts?.Cancel();
            var cts = new CancellationTokenSource();
            _dataCts = cts;
            var token = cts.Token;
            SetDataLoading(true);
            try
            {
                var parsed = await Task.Run(() => ParseCsvCore(file), token);   // 파일 읽기/파싱(백그라운드)
                if (token.IsCancellationRequested || IsDisposed) return;
                _dataHeaders = parsed.Item1;
                _dataRowsAll = parsed.Item2;
                RenderDataGrid();              // 검색 필터 + 정렬 적용해 그리드 채움
                _lblUtilInfo.Text = "Data Log: " + file + Environment.NewLine + "Event Log: " + EventLogger.LogDir;
            }
            catch (OperationCanceledException) { /* 새 요청으로 대체됨 — 무시 */ }
            catch (Exception ex)
            {
                if (!IsDisposed && !token.IsCancellationRequested) ShowGridError(_gridData, ex);
            }
            finally
            {
                if (!IsDisposed && _dataCts == cts) { SetDataLoading(false); UpdateEmpty(_gridData, _emptyData); }
            }
        }

        /// <summary>CSV → (헤더, 행) 파싱. 파일 없으면 고정 칼럼. UI 비의존 → 백그라운드 스레드 호출 안전.</summary>
        private static Tuple<List<string>, List<string[]>> ParseCsvCore(string file)
        {
            string[] lines = File.Exists(file) ? File.ReadAllLines(file, Encoding.UTF8) : null;
            List<string> headers = (lines != null && lines.Length > 0) ? SplitCsv(lines[0]) : new List<string>(DataLogColumns);
            if (headers.Count == 0) headers = new List<string>(DataLogColumns);
            var rowsAll = new List<string[]>();
            if (lines != null)
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var cells = SplitCsv(lines[i]);
                    var arr = new string[headers.Count];
                    for (int c = 0; c < headers.Count; c++) arr[c] = c < cells.Count ? cells[c] : "";
                    rowsAll.Add(arr);
                }
            return Tuple.Create(headers, rowsAll);
        }

        /// <summary>Data Log 로딩 중 표시(하단 안내 띠 + 대기 커서 + 새로고침 버튼 비활성).</summary>
        private void SetDataLoading(bool on)
        {
            try
            {
                if (_btnDataReload != null) _btnDataReload.Enabled = !on;
                this.Cursor = on ? Cursors.WaitCursor : Cursors.Default;
                if (_emptyData == null) return;
                if (on) { _gridData.Rows.Clear(); _emptyData.Text = Lang.T("hist.loading"); _emptyData.Visible = true; }
                else    { _emptyData.Text = Lang.T("hist.empty"); }
            }
            catch (Exception ex) { Debug.WriteLine("[DataLogPage] SetDataLoading 실패: " + ex.Message); }
        }

        /// <summary>캐시 → 검색 필터 + 정렬 → 그리드 렌더(언바운드).</summary>
        private void RenderDataGrid()
        {
            var g = _gridData;
            if (_dataHeaders == null) { g.DataSource = null; g.Columns.Clear(); g.Rows.Clear(); return; }

            // SuspendLayout + 페이지 단위 렌더로 대량 데이터 버벅임 방지.
            g.SuspendLayout();
            try
            {
                g.DataSource = null; g.Columns.Clear(); g.Rows.Clear();

                g.ColumnCount = _dataHeaders.Count;
                for (int c = 0; c < _dataHeaders.Count; c++)
                {
                    g.Columns[c].Name = "col" + c;
                    g.Columns[c].HeaderText = _dataHeaders[c];
                    g.Columns[c].SortMode = DataGridViewColumnSortMode.Programmatic;
                }
                if (g.ColumnCount > 0) g.Columns[0].Frozen = true;

                string q = _txtDataSearch?.Text?.Trim();
                bool hasFilter = !string.IsNullOrEmpty(q);
                bool hasSort = _dataSortCol >= 0 && _dataSortCol < _dataHeaders.Count;

                // 무필터·무정렬이면 원본을 그대로 사용(전체 ToList 복사 제거 — 페이지 이동 시 대량 복사 방지).
                IList<string[]> list;
                if (!hasFilter && !hasSort)
                {
                    list = _dataRowsAll;
                }
                else
                {
                    IEnumerable<string[]> rows = _dataRowsAll;
                    if (hasFilter)
                        rows = rows.Where(r => r.Any(cell => cell != null && cell.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
                    var l = rows.ToList();
                    if (hasSort)
                        l.Sort((a, b) =>
                        {
                            string sa = _dataSortCol < a.Length ? a[_dataSortCol] : "";
                            string sb = _dataSortCol < b.Length ? b[_dataSortCol] : "";
                            int cmp = (double.TryParse(sa, out var da) && double.TryParse(sb, out var db))
                                ? da.CompareTo(db) : string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
                            return _dataSortAsc ? cmp : -cmp;
                        });
                    list = l;
                }

                _pagerData?.SetTotal(list.Count);
                foreach (var r in (_pagerData != null ? _pagerData.PageSlice(list) : list))
                    g.Rows.Add(r.Cast<object>().ToArray());

                for (int c = 0; c < g.Columns.Count; c++)
                    g.Columns[c].HeaderCell.SortGlyphDirection =
                        (c == _dataSortCol) ? (_dataSortAsc ? SortOrder.Ascending : SortOrder.Descending) : SortOrder.None;
            }
            finally { g.ResumeLayout(); }

            // 전체 행이 아닌 "현재 보이는 행"만 측정해 칼럼 폭 조정(AllCells 대비 대폭 경량).
            g.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void GridData_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            if (_dataSortCol == e.ColumnIndex) _dataSortAsc = !_dataSortAsc;
            else { _dataSortCol = e.ColumnIndex; _dataSortAsc = true; }
            RenderDataGrid();
        }

        private static string DataLogFilePath(DateTime date)
        {
            var cfg = VisionConfigStore.Load();
            string root = (cfg == null || string.IsNullOrEmpty(cfg.DataLogPath)) ? @".\Log\Data" : cfg.DataLogPath;
            return Path.Combine(root, "vision_" + date.ToString("yyyyMMdd") + ".csv");
        }

        /// <summary>CSV 를 DataGridView 에 직접 채운다(System.Data 비의존).
        /// 파일이 없거나 비어도 고정 칼럼(DataLogColumns)으로 헤더는 항상 표시한다.</summary>
        private static void FillGridFromCsv(DataGridView g, string file)
        {
            g.DataSource = null;
            g.Columns.Clear();
            g.Rows.Clear();

            string[] lines = File.Exists(file) ? File.ReadAllLines(file, Encoding.UTF8) : null;

            // 헤더: 파일이 있으면 파일 헤더, 없으면 고정 칼럼.
            List<string> headers = (lines != null && lines.Length > 0)
                ? SplitCsv(lines[0])
                : new List<string>(DataLogColumns);
            if (headers.Count == 0) headers = new List<string>(DataLogColumns);

            g.ColumnCount = headers.Count;
            for (int c = 0; c < headers.Count; c++)
            {
                g.Columns[c].Name = "col" + c;
                g.Columns[c].HeaderText = headers[c];
            }
            if (g.ColumnCount > 0) g.Columns[0].Frozen = true;   // Timestamp 가로 스크롤 시 고정

            if (lines == null) return;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                List<string> cells = SplitCsv(lines[i]);
                var arr = new object[headers.Count];
                for (int c = 0; c < headers.Count; c++) arr[c] = c < cells.Count ? cells[c] : "";
                g.Rows.Add(arr);
            }
        }

        /// <summary>RFC 유사 CSV 파서(따옴표/이스케이프 처리) — DataLogSaver.Csv 대응.</summary>
        private static List<string> SplitCsv(string line)
        {
            var list = new List<string>();
            if (line == null) return list;

            var sb = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (inQuote)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuote = false;
                    }
                    else sb.Append(ch);
                }
                else
                {
                    if (ch == ',') { list.Add(sb.ToString()); sb.Clear(); }
                    else if (ch == '"') inQuote = true;
                    else sb.Append(ch);
                }
            }
            list.Add(sb.ToString());
            return list;
        }

        // ── Log (EventLogger) — 원본 캐시(_logRaw) + 뷰(_logRowsAll) 분리, 비동기 로드/정렬 ──
        /// <summary>일자 변경/새로고침: 파일을 백그라운드로 읽어 _logRaw 캐시에 담은 뒤 현재 필터/정렬 뷰를 적용한다.</summary>
        private async void LoadEventLog()
        {
            DateTime date = _dtLog.Value.Date;
            var cts = NewLogCts();
            var token = cts.Token;
            SetLogLoading(true);
            try
            {
                List<EventRow> raw = await Task.Run(() => EventLogger.Read(date), token);
                if (token.IsCancellationRequested || IsDisposed) return;
                _logRaw = raw;
                await ApplyLogViewCore(token);
            }
            catch (OperationCanceledException) { /* 새 요청으로 대체됨 — 무시 */ }
            catch (Exception ex)
            {
                if (!IsDisposed && !token.IsCancellationRequested)
                {
                    _logRaw = null; _logRowsAll = null;
                    ShowGridError(_gridLog, ex);
                    UpdateEmpty(_gridLog, _emptyLog);
                }
            }
            finally { if (!IsDisposed && _logCts == cts) SetLogLoading(false); }
        }

        /// <summary>검색/정렬만 변경: 재읽기 없이 _logRaw 캐시에서 필터+정렬해 뷰를 재구성한다.</summary>
        private async void ApplyLogView()
        {
            if (_logRaw == null) { LoadEventLog(); return; }
            var cts = NewLogCts();
            var token = cts.Token;
            SetLogLoading(true);
            try { await ApplyLogViewCore(token); }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed && !token.IsCancellationRequested)
                {
                    _logRowsAll = null; ShowGridError(_gridLog, ex); UpdateEmpty(_gridLog, _emptyLog);
                }
            }
            finally { if (!IsDisposed && _logCts == cts) SetLogLoading(false); }
        }

        /// <summary>_logRaw → (검색 필터 + 타입 정렬)을 백그라운드에서 수행 후 UI 스레드에서 렌더.</summary>
        private async Task ApplyLogViewCore(CancellationToken token)
        {
            string q = _txtLogSearch?.Text?.Trim();
            string sortProp = _logSortProp; bool sortAsc = _logSortAsc;
            List<EventRow> src = _logRaw;

            List<EventRow> view = await Task.Run(() =>
            {
                IEnumerable<EventRow> rows = src;
                if (!string.IsNullOrEmpty(q)) rows = rows.Where(r => EventMatches(r, q));
                var list = rows.ToList();
                token.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(sortProp)) list.Sort((a, b) => CompareEvent(a, b, sortProp, sortAsc));
                return list;
            }, token);

            if (token.IsCancellationRequested || IsDisposed) return;
            _logRowsAll = view;
            _pagerLog?.Reset();
            _pagerLog?.SetTotal(_logRowsAll.Count);
            RenderLogPage();
        }

        /// <summary>이전 Log 작업을 취소하고 새 토큰 소스를 만든다(마지막 클릭만 반영).</summary>
        private CancellationTokenSource NewLogCts()
        {
            _logCts?.Cancel();
            _logCts = new CancellationTokenSource();
            return _logCts;
        }

        /// <summary>현재 페이지 구간만 Log 그리드에 바인딩(전체 캐시=_logRowsAll).</summary>
        private void RenderLogPage()
        {
            if (_logRowsAll == null) { _gridLog.DataSource = null; UpdateEmpty(_gridLog, _emptyLog); return; }
            _gridLog.DataSource = (_pagerLog != null ? _pagerLog.PageSlice(_logRowsAll) : (IEnumerable<EventRow>)_logRowsAll).ToList();
            UpdateEmpty(_gridLog, _emptyLog);
        }

        private void GridLog_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || _logRaw == null) return;
            string prop = _gridLog.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(prop)) prop = _gridLog.Columns[e.ColumnIndex].Name;
            if (_logSortProp == prop) _logSortAsc = !_logSortAsc; else { _logSortProp = prop; _logSortAsc = true; }
            ApplyLogView();   // 캐시에서 정렬만 재적용(재읽기 없음, 백그라운드)
        }

        // ── Alarm (AlarmManager) — 메모리 데이터 + 타입 정렬/검색 + 페이지 단위 렌더 ──
        private void LoadAlarms()
        {
            try
            {
                IEnumerable<AlarmRecord> src = _chkActiveOnly.Checked ? AlarmManager.Active : AlarmManager.History;
                string q = _txtAlarmSearch?.Text?.Trim();
                if (!string.IsNullOrEmpty(q)) src = src.Where(r => AlarmMatches(r, q));
                _alarmRowsAll = src.ToList();
                if (!string.IsNullOrEmpty(_alarmSortProp))
                    _alarmRowsAll.Sort((a, b) => CompareAlarm(a, b, _alarmSortProp, _alarmSortAsc));
                _pagerAlarm?.Reset();
                _pagerAlarm?.SetTotal(_alarmRowsAll.Count);
                RenderAlarmPage();
            }
            catch (Exception ex) { _alarmRowsAll = null; ShowGridError(_gridAlarm, ex); UpdateEmpty(_gridAlarm, _emptyAlarm); }
        }

        /// <summary>현재 페이지 구간만 Alarm 그리드에 바인딩(전체 캐시=_alarmRowsAll).</summary>
        private void RenderAlarmPage()
        {
            if (_alarmRowsAll == null) { _gridAlarm.DataSource = null; UpdateEmpty(_gridAlarm, _emptyAlarm); return; }
            _gridAlarm.DataSource = (_pagerAlarm != null ? _pagerAlarm.PageSlice(_alarmRowsAll) : (IEnumerable<AlarmRecord>)_alarmRowsAll).ToList();
            UpdateEmpty(_gridAlarm, _emptyAlarm);
        }

        private void GridAlarm_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || _alarmRowsAll == null) return;
            string prop = _gridAlarm.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(prop)) prop = _gridAlarm.Columns[e.ColumnIndex].Name;
            if (_alarmSortProp == prop) _alarmSortAsc = !_alarmSortAsc; else { _alarmSortProp = prop; _alarmSortAsc = true; }
            _alarmRowsAll.Sort((a, b) => CompareAlarm(a, b, _alarmSortProp, _alarmSortAsc));
            _pagerAlarm?.Reset();
            RenderAlarmPage();
        }

        /// <summary>Log 로딩 중 표시(하단 안내 띠 + 대기 커서 + 새로고침 버튼 비활성).</summary>
        private void SetLogLoading(bool on)
        {
            try
            {
                if (_btnLogReload != null) _btnLogReload.Enabled = !on;
                this.Cursor = on ? Cursors.WaitCursor : Cursors.Default;
                if (_emptyLog == null) return;
                if (on)
                {
                    _gridLog.DataSource = null;
                    _emptyLog.Text = Lang.T("hist.loading");
                    _emptyLog.Visible = true;
                }
                else
                {
                    _emptyLog.Text = Lang.T("hist.empty");
                }
            }
            catch (Exception ex) { Debug.WriteLine("[DataLogPage] SetLogLoading 실패: " + ex.Message); }
        }

        private static void ShowGridError(DataGridView g, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[DataLogPage] load 실패: " + ex.Message);
            g.DataSource = null;
        }

        /// <summary>행이 없으면 하단 안내 띠 표시(컬럼/헤더는 계속 보임).</summary>
        private static void UpdateEmpty(DataGridView g, Label empty)
        {
            if (empty != null) empty.Visible = (g.RowCount == 0);
        }

        // ── 이벤트 핸들러 (컨트롤명_이벤트명) ──
        private void _btnDataReload_Click(object sender, EventArgs e)        { LoadDataLog(); }
        private void _dtData_ValueChanged(object sender, EventArgs e)        { LoadDataLog(); }
        private void _btnLogReload_Click(object sender, EventArgs e)         { LoadEventLog(); }
        private void _dtLog_ValueChanged(object sender, EventArgs e)         { LoadEventLog(); }
        private void _btnAlarmReload_Click(object sender, EventArgs e)       { LoadAlarms(); }
        private void _chkActiveOnly_CheckedChanged(object sender, EventArgs e) { LoadAlarms(); }
        private void _btnRefreshAll_Click(object sender, EventArgs e)        { RefreshAll(); }
        private void _txtDataSearch_TextChanged(object sender, EventArgs e)  { RenderDataGrid(); }
        private void _txtLogSearch_TextChanged(object sender, EventArgs e)   { ApplyLogView(); }   // 재읽기 없이 캐시 필터(버벅임 방지)
        private void _txtAlarmSearch_TextChanged(object sender, EventArgs e) { LoadAlarms(); }

        private void _btnDataExport_Click(object sender, EventArgs e)
        {
            try
            {
                string file = DataLogFilePath(_dtData.Value);
                if (!File.Exists(file))
                {
                    MessageBox.Show(Lang.T("hist.noDataFile"), Lang.T("hist.export"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = Path.GetFileName(file) })
                    if (dlg.ShowDialog(this) == DialogResult.OK) File.Copy(file, dlg.FileName, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("hist.exportFail") + ex.Message, Lang.T("hist.export"),
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void _btnOpenDataFolder_Click(object sender, EventArgs e)
        {
            var cfg = VisionConfigStore.Load();
            OpenFolder((cfg == null || string.IsNullOrEmpty(cfg.DataLogPath)) ? @".\Log\Data" : cfg.DataLogPath);
        }

        private void _btnOpenLogFolder_Click(object sender, EventArgs e) => OpenFolder(EventLogger.LogDir);

        private static void OpenFolder(string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                Directory.CreateDirectory(full);
                Process.Start("explorer.exe", "\"" + full + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("hist.openFolderFail") + ex.Message, Lang.T("hist.tab.utility"),
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
