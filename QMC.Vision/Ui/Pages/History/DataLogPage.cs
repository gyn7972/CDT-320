using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
        private List<EventRow>    _logRowsAll;
        private List<AlarmRecord> _alarmRowsAll;
        private string _logSortProp;   private bool _logSortAsc   = true;
        private string _alarmSortProp; private bool _alarmSortAsc = true;

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
                                        : DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        /// <summary>객체의 모든 공개 속성 문자열에 검색어가 포함되는지(대소문자 무시).</summary>
        private static bool ObjMatches(object o, string q)
        {
            if (o == null) return false;
            foreach (var p in o.GetType().GetProperties())
            {
                try { var v = p.GetValue(o); if (v != null && v.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true; }
                catch { }
            }
            return false;
        }

        /// <summary>리스트를 속성명 기준 전역 정렬(페이지 무관). 동형 IComparable 우선, 그 외 문자열 비교.</summary>
        private static void SortByProp<T>(List<T> list, string prop, bool asc)
        {
            if (list == null || string.IsNullOrEmpty(prop)) return;
            var pi = typeof(T).GetProperty(prop);
            if (pi == null) return;
            list.Sort((a, b) =>
            {
                object va = null, vb = null;
                try { va = pi.GetValue(a); vb = pi.GetValue(b); } catch { }
                int cmp;
                if (va is IComparable ca && vb != null && va.GetType() == vb.GetType()) cmp = ca.CompareTo(vb);
                else cmp = string.Compare(va?.ToString(), vb?.ToString(), StringComparison.OrdinalIgnoreCase);
                return asc ? cmp : -cmp;
            });
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

        // ── 날짜 선택기 범위 = 실제 로그가 있는 일자 [최소~최대(또는 오늘)] ──
        private void SetDatePickerRanges()
        {
            try
            {
                var cfg = VisionConfigStore.Load();
                string dataDir = (cfg == null || string.IsNullOrEmpty(cfg.DataLogPath)) ? @".\Log\Data" : cfg.DataLogPath;
                ApplyRange(_dtData, CollectDates(dataDir, "vision_*.csv"));
                ApplyRange(_dtLog,  CollectDates(EventLogger.LogDir, "*"));
            }
            catch { }
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

        /// <summary>파일명에서 yyyyMMdd 8자리를 찾아 날짜로 해석.</summary>
        private static DateTime? ExtractDate(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i + 8 <= name.Length; i++)
            {
                string sub = name.Substring(i, 8);
                if (sub.All(char.IsDigit) &&
                    DateTime.TryParseExact(sub, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;
            }
            return null;
        }

        private static void ApplyRange(DateTimePicker dtp, List<DateTime> dates)
        {
            try
            {
                DateTime max = DateTime.Today;
                DateTime min = dates.Count > 0 ? dates.Min() : DateTime.Today;
                if (dates.Count > 0) { var dmax = dates.Max(); if (dmax > max) max = dmax; }
                if (min > max) min = max;
                dtp.MinDate = min;
                dtp.MaxDate = max;
            }
            catch { }
        }

        private void _tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateHeader();
        }

        // ── Data Log (vision_yyyyMMdd.csv) ──
        private void LoadDataLog()
        {
            try
            {
                string file = DataLogFilePath(_dtData.Value);
                ParseCsv(file);        // _dataHeaders / _dataRowsAll 채움
                RenderDataGrid();      // 검색 필터 + 정렬 적용해 그리드 채움
                _lblUtilInfo.Text = "Data Log: " + file + Environment.NewLine + "Event Log: " + EventLogger.LogDir;
            }
            catch (Exception ex) { ShowGridError(_gridData, ex); }
            UpdateEmpty(_gridData, _emptyData);
        }

        /// <summary>CSV → 헤더/행 캐시(_dataHeaders/_dataRowsAll). 파일 없으면 고정 칼럼.</summary>
        private void ParseCsv(string file)
        {
            string[] lines = File.Exists(file) ? File.ReadAllLines(file, Encoding.UTF8) : null;
            _dataHeaders = (lines != null && lines.Length > 0) ? SplitCsv(lines[0]) : new List<string>(DataLogColumns);
            if (_dataHeaders.Count == 0) _dataHeaders = new List<string>(DataLogColumns);
            _dataRowsAll = new List<string[]>();
            if (lines != null)
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var cells = SplitCsv(lines[i]);
                    var arr = new string[_dataHeaders.Count];
                    for (int c = 0; c < _dataHeaders.Count; c++) arr[c] = c < cells.Count ? cells[c] : "";
                    _dataRowsAll.Add(arr);
                }
        }

        /// <summary>캐시 → 검색 필터 + 정렬 → 그리드 렌더(언바운드).</summary>
        private void RenderDataGrid()
        {
            var g = _gridData;
            g.DataSource = null; g.Columns.Clear(); g.Rows.Clear();
            if (_dataHeaders == null) return;

            g.ColumnCount = _dataHeaders.Count;
            for (int c = 0; c < _dataHeaders.Count; c++)
            {
                g.Columns[c].Name = "col" + c;
                g.Columns[c].HeaderText = _dataHeaders[c];
                g.Columns[c].SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            if (g.ColumnCount > 0) g.Columns[0].Frozen = true;

            string q = _txtDataSearch?.Text?.Trim();
            IEnumerable<string[]> rows = _dataRowsAll;
            if (!string.IsNullOrEmpty(q))
                rows = rows.Where(r => r.Any(cell => cell != null && cell.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
            var list = rows.ToList();

            if (_dataSortCol >= 0 && _dataSortCol < _dataHeaders.Count)
                list.Sort((a, b) =>
                {
                    string sa = _dataSortCol < a.Length ? a[_dataSortCol] : "";
                    string sb = _dataSortCol < b.Length ? b[_dataSortCol] : "";
                    int cmp = (double.TryParse(sa, out var da) && double.TryParse(sb, out var db))
                        ? da.CompareTo(db) : string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
                    return _dataSortAsc ? cmp : -cmp;
                });

            _pagerData?.SetTotal(list.Count);
            foreach (var r in (_pagerData != null ? _pagerData.PageSlice(list) : list))
                g.Rows.Add(r.Cast<object>().ToArray());

            for (int c = 0; c < g.Columns.Count; c++)
                g.Columns[c].HeaderCell.SortGlyphDirection =
                    (c == _dataSortCol) ? (_dataSortAsc ? SortOrder.Ascending : SortOrder.Descending) : SortOrder.None;
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

        // ── Log (EventLogger) — 전체 캐시 + 전역 정렬/검색 + 페이지 단위 렌더 ──
        private void LoadEventLog()
        {
            try
            {
                IEnumerable<EventRow> rows = EventLogger.Read(_dtLog.Value.Date);
                string q = _txtLogSearch?.Text?.Trim();
                if (!string.IsNullOrEmpty(q)) rows = rows.Where(r => ObjMatches(r, q));
                _logRowsAll = rows.ToList();
                SortByProp(_logRowsAll, _logSortProp, _logSortAsc);
                _pagerLog?.Reset();
                _pagerLog?.SetTotal(_logRowsAll.Count);
                RenderLogPage();
            }
            catch (Exception ex) { _logRowsAll = null; ShowGridError(_gridLog, ex); UpdateEmpty(_gridLog, _emptyLog); }
        }

        /// <summary>현재 페이지 구간만 Log 그리드에 바인딩(전체 캐시=_logRowsAll).</summary>
        private void RenderLogPage()
        {
            if (_logRowsAll == null) { _gridLog.DataSource = null; UpdateEmpty(_gridLog, _emptyLog); return; }
            _gridLog.DataSource = (_pagerLog != null ? _pagerLog.PageSlice(_logRowsAll) : _logRowsAll).ToList();
            UpdateEmpty(_gridLog, _emptyLog);
        }

        private void GridLog_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || _logRowsAll == null) return;
            string prop = _gridLog.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(prop)) prop = _gridLog.Columns[e.ColumnIndex].Name;
            if (_logSortProp == prop) _logSortAsc = !_logSortAsc; else { _logSortProp = prop; _logSortAsc = true; }
            SortByProp(_logRowsAll, _logSortProp, _logSortAsc);
            _pagerLog?.Reset();
            RenderLogPage();
        }

        // ── Alarm (AlarmManager) — 전체 캐시 + 전역 정렬/검색 + 페이지 단위 렌더 ──
        private void LoadAlarms()
        {
            try
            {
                IEnumerable<AlarmRecord> src = _chkActiveOnly.Checked ? AlarmManager.Active : AlarmManager.History;
                string q = _txtAlarmSearch?.Text?.Trim();
                if (!string.IsNullOrEmpty(q)) src = src.Where(r => ObjMatches(r, q));
                _alarmRowsAll = src.ToList();
                SortByProp(_alarmRowsAll, _alarmSortProp, _alarmSortAsc);
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
            _gridAlarm.DataSource = (_pagerAlarm != null ? _pagerAlarm.PageSlice(_alarmRowsAll) : _alarmRowsAll).ToList();
            UpdateEmpty(_gridAlarm, _emptyAlarm);
        }

        private void GridAlarm_HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || _alarmRowsAll == null) return;
            string prop = _gridAlarm.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(prop)) prop = _gridAlarm.Columns[e.ColumnIndex].Name;
            if (_alarmSortProp == prop) _alarmSortAsc = !_alarmSortAsc; else { _alarmSortProp = prop; _alarmSortAsc = true; }
            SortByProp(_alarmRowsAll, _alarmSortProp, _alarmSortAsc);
            _pagerAlarm?.Reset();
            RenderAlarmPage();
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
