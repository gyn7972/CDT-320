using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;   // LogDatePicker

namespace QMC.Vision.Ui.Pages
{
    partial class DataLogPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;   // 헤더(30) + 탭(Fill) — Dock 적층 모호성 제거(탭 띠 항상 표시)
        private Label      _hdr;
        private TabControl _tabs;
        private TabPage    _tpData;
        private TabPage    _tpLog;
        private TabPage    _tpAlarm;
        private TabPage    _tpUtility;

        // Data Log (검사 CSV)
        private FlowLayoutPanel _barData;
        private Label           _lblDataDate;
        private LogDatePicker   _dtData;
        private Button          _btnDataReload;
        private Button          _btnDataExport;
        private TextBox         _txtDataSearch;
        private DataGridView    _gridData;

        // Log (EventLogger)
        private FlowLayoutPanel _barLog;
        private Label           _lblLogDate;
        private LogDatePicker   _dtLog;
        private Button          _btnLogReload;
        private TextBox         _txtLogSearch;
        private DataGridView    _gridLog;

        // Alarm (AlarmManager)
        private FlowLayoutPanel _barAlarm;
        private CheckBox        _chkActiveOnly;
        private Button          _btnAlarmReload;
        private TextBox         _txtAlarmSearch;
        private DataGridView    _gridAlarm;

        // 빈 상태 안내
        private Label _emptyData;
        private Label _emptyLog;
        private Label _emptyAlarm;

        // Utility
        private FlowLayoutPanel _flowUtil;
        private Button          _btnOpenDataFolder;
        private Button          _btnOpenLogFolder;
        private Button          _btnRefreshAll;
        private Label           _lblUtilInfo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root       = new TableLayoutPanel();
            this._hdr        = new Label();
            this._tabs       = new TabControl();
            this._tpData     = new TabPage();
            this._tpLog      = new TabPage();
            this._tpAlarm    = new TabPage();
            this._tpUtility  = new TabPage();

            this._barData       = NewBar();
            this._lblDataDate   = NewBarLabel("일자");
            this._dtData        = NewDatePicker();
            this._btnDataReload = NewBarButton("조회 / 새로고침");
            this._btnDataExport = NewBarButton("CSV 내보내기");
            this._txtDataSearch = NewSearchBox();
            this._gridData      = NewGrid();

            this._barLog        = NewBar();
            this._lblLogDate    = NewBarLabel("일자");
            this._dtLog         = NewDatePicker();
            this._btnLogReload  = NewBarButton("조회 / 새로고침");
            this._txtLogSearch  = NewSearchBox();
            this._gridLog       = NewGrid();

            this._barAlarm      = NewBar();
            this._chkActiveOnly = new CheckBox { Text = "활성 알람만", AutoSize = true, Margin = new Padding(6, 6, 12, 0) };
            this._btnAlarmReload= NewBarButton("새로고침");
            this._txtAlarmSearch= NewSearchBox();
            this._gridAlarm     = NewGrid();

            this._emptyData  = NewEmptyLabel();
            this._emptyLog   = NewEmptyLabel();
            this._emptyAlarm = NewEmptyLabel();

            this._flowUtil          = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = UiTheme.OptionPanelBg };
            this._btnOpenDataFolder = NewUtilButton("Data Log 폴더 열기");
            this._btnOpenLogFolder  = NewUtilButton("Event Log 폴더 열기");
            this._btnRefreshAll     = NewUtilButton("전체 새로고침");
            this._lblUtilInfo       = new Label { AutoSize = true, Margin = new Padding(6, 16, 0, 0), ForeColor = Color.DimGray };

            this._tabs.SuspendLayout();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Height = 30;
            this._hdr.Text = "이력 — Data Log";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = UiTheme.StatusBarFg;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _tabs
            this._tabs.Dock = DockStyle.Fill;
            this._tabs.Font = UiTheme.ButtonFont;
            this._tpData.Text    = "Data Log";
            this._tpLog.Text     = "Log";
            this._tpAlarm.Text   = "Alarm";
            this._tpUtility.Text = "Utility";
            this._tabs.Controls.Add(this._tpData);
            this._tabs.Controls.Add(this._tpLog);
            this._tabs.Controls.Add(this._tpAlarm);
            this._tabs.Controls.Add(this._tpUtility);
            this._tabs.SelectedIndexChanged += this._tabs_SelectedIndexChanged;

            // Data Log 탭
            this._barData.Controls.Add(this._lblDataDate);
            this._barData.Controls.Add(this._dtData);
            this._barData.Controls.Add(this._btnDataReload);
            this._barData.Controls.Add(this._btnDataExport);
            this._barData.Controls.Add(this._txtDataSearch);
            this._txtDataSearch.TextChanged += (s, e) => RenderDataGrid();
            this._tpData.Controls.Add(this._gridData);
            this._tpData.Controls.Add(this._emptyData);
            this._tpData.Controls.Add(this._barData);
            this._btnDataReload.Click += this._btnDataReload_Click;
            this._btnDataExport.Click += this._btnDataExport_Click;
            this._dtData.ValueChanged += this._dtData_ValueChanged;

            // Log 탭
            this._barLog.Controls.Add(this._lblLogDate);
            this._barLog.Controls.Add(this._dtLog);
            this._barLog.Controls.Add(this._btnLogReload);
            this._barLog.Controls.Add(this._txtLogSearch);
            this._txtLogSearch.TextChanged += (s, e) => ApplyLogView();   // 재읽기 없이 캐시에서 필터(대량 데이터 버벅임 방지)
            this._tpLog.Controls.Add(this._gridLog);
            this._tpLog.Controls.Add(this._emptyLog);
            this._tpLog.Controls.Add(this._barLog);
            this._btnLogReload.Click += this._btnLogReload_Click;
            this._dtLog.ValueChanged += this._dtLog_ValueChanged;

            // Alarm 탭
            this._barAlarm.Controls.Add(this._chkActiveOnly);
            this._barAlarm.Controls.Add(this._btnAlarmReload);
            this._barAlarm.Controls.Add(this._txtAlarmSearch);
            this._txtAlarmSearch.TextChanged += (s, e) => LoadAlarms();
            this._tpAlarm.Controls.Add(this._gridAlarm);
            this._tpAlarm.Controls.Add(this._emptyAlarm);
            this._tpAlarm.Controls.Add(this._barAlarm);
            this._btnAlarmReload.Click += this._btnAlarmReload_Click;
            this._chkActiveOnly.CheckedChanged += this._chkActiveOnly_CheckedChanged;

            // Utility 탭
            this._flowUtil.Controls.Add(this._btnOpenDataFolder);
            this._flowUtil.Controls.Add(this._btnOpenLogFolder);
            this._flowUtil.Controls.Add(this._btnRefreshAll);
            this._flowUtil.SetFlowBreak(this._btnRefreshAll, true);
            this._flowUtil.Controls.Add(this._lblUtilInfo);
            this._tpUtility.Controls.Add(this._flowUtil);
            this._btnOpenDataFolder.Click += this._btnOpenDataFolder_Click;
            this._btnOpenLogFolder.Click += this._btnOpenLogFolder_Click;
            this._btnRefreshAll.Click += this._btnRefreshAll_Click;

            // _root: 2행(헤더 30 / 탭 Fill) — 탭 띠가 항상 보이도록 결정적 배치
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 2;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._root.Controls.Add(this._hdr,  0, 0);
            this._root.Controls.Add(this._tabs, 0, 1);

            // DataLogPage
            this.Controls.Add(this._root);
            this.Name = "DataLogPage";

            this._tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // ── Designer 보조 팩토리 ──
        private static FlowLayoutPanel NewBar()
            => new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                WrapContents = false,
                Padding = new Padding(8, 7, 8, 7),
                BackColor = UiTheme.OptionPanelBg
            };

        private static Label NewBarLabel(string text)
            => new Label { Text = text, AutoSize = true, Margin = new Padding(2, 8, 6, 0) };

        private static LogDatePicker NewDatePicker()
            => new LogDatePicker { Width = 130, Height = 23, Margin = new Padding(2, 5, 12, 3) };

        /// <summary>검색 입력 박스(플레이스홀더 비슷한 안내는 PlaceholderText 미지원 .NET FW → 빈 칸).</summary>
        private static TextBox NewSearchBox()
            => new TextBox { Width = 180, Margin = new Padding(8, 3, 6, 3) };

        private static Button NewBarButton(string text)
            => new Button { Text = text, AutoSize = true, Height = 27, Margin = new Padding(2, 1, 6, 1), FlatStyle = FlatStyle.System };

        private static Button NewUtilButton(string text)
            => new Button { Text = text, Width = 200, Height = 40, Margin = new Padding(6), FlatStyle = FlatStyle.System };

        private static Label NewEmptyLabel()
            => new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "표시할 데이터가 없습니다 — 검사·이벤트·알람이 누적되면 표시됩니다.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                BackColor = Color.FromArgb(248, 249, 250),
                Visible = false
            };

        private static DataGridView NewGrid()
            => new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,   // 전체 행 측정 방지(버벅임) — 렌더 후 보이는 행만 측정
                AllowUserToResizeRows = false,
                BorderStyle = BorderStyle.None
            };
    }
}
