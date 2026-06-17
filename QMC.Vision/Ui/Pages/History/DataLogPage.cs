using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Vision.Config;
using QMC.Vision.Ui.Controls;   // GridTheme

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

        public DataLogPage()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;
            GridTheme.Apply(_gridData);
            GridTheme.Apply(_gridLog);
            GridTheme.Apply(_gridAlarm);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (IsDesignerMode() || _initialized) return;
            _dtData.Value = DateTime.Today;
            _dtLog.Value  = DateTime.Today;
            _initialized  = true;
            RefreshAll();
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

        private void _tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_tabs.SelectedTab != null) _hdr.Text = "이력 — " + _tabs.SelectedTab.Text;
        }

        // ── Data Log (vision_yyyyMMdd.csv) ──
        private void LoadDataLog()
        {
            try
            {
                string file = DataLogFilePath(_dtData.Value);
                FillGridFromCsv(_gridData, file);
                _lblUtilInfo.Text = "Data Log: " + file + Environment.NewLine + "Event Log: " + EventLogger.LogDir;
            }
            catch (Exception ex) { ShowGridError(_gridData, ex); }
            UpdateEmpty(_gridData, _emptyData);
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

        // ── Log (EventLogger) ──
        private void LoadEventLog()
        {
            try { _gridLog.DataSource = new List<EventRow>(EventLogger.Read(_dtLog.Value.Date)); }
            catch (Exception ex) { ShowGridError(_gridLog, ex); }
            UpdateEmpty(_gridLog, _emptyLog);
        }

        // ── Alarm (AlarmManager) ──
        private void LoadAlarms()
        {
            try
            {
                IReadOnlyList<AlarmRecord> src = _chkActiveOnly.Checked ? AlarmManager.Active : AlarmManager.History;
                _gridAlarm.DataSource = new List<AlarmRecord>(src);
            }
            catch (Exception ex) { ShowGridError(_gridAlarm, ex); }
            UpdateEmpty(_gridAlarm, _emptyAlarm);
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
                    MessageBox.Show("선택한 날짜의 Data Log 파일이 없습니다.", "내보내기",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = Path.GetFileName(file) })
                    if (dlg.ShowDialog(this) == DialogResult.OK) File.Copy(file, dlg.FileName, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("내보내기 실패: " + ex.Message, "내보내기",
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
                MessageBox.Show("폴더 열기 실패: " + ex.Message, "Utility",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
