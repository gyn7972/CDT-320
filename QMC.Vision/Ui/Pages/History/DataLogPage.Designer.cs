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
            this._barData       = new FlowLayoutPanel();
            this._lblDataDate   = new Label();
            this._dtData        = new LogDatePicker();
            this._btnDataReload = new Button();
            this._btnDataExport = new Button();
            this._txtDataSearch = new TextBox();
            this._gridData      = new DataGridView();
            this._barLog        = new FlowLayoutPanel();
            this._lblLogDate    = new Label();
            this._dtLog         = new LogDatePicker();
            this._btnLogReload  = new Button();
            this._txtLogSearch  = new TextBox();
            this._gridLog       = new DataGridView();
            this._barAlarm      = new FlowLayoutPanel();
            this._chkActiveOnly = new CheckBox();
            this._btnAlarmReload= new Button();
            this._txtAlarmSearch= new TextBox();
            this._gridAlarm     = new DataGridView();
            this._emptyData     = new Label();
            this._emptyLog      = new Label();
            this._emptyAlarm    = new Label();
            this._flowUtil          = new FlowLayoutPanel();
            this._btnOpenDataFolder = new Button();
            this._btnOpenLogFolder  = new Button();
            this._btnRefreshAll     = new Button();
            this._lblUtilInfo       = new Label();
            ((System.ComponentModel.ISupportInitialize)(this._gridData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridAlarm)).BeginInit();
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

            // ── 공통 바/라벨/입력/그리드 스타일 ──
            // _barData
            this._barData.Dock = DockStyle.Top;
            this._barData.Height = 42;
            this._barData.WrapContents = false;
            this._barData.Padding = new Padding(8, 7, 8, 7);
            this._barData.BackColor = UiTheme.OptionPanelBg;
            // _lblDataDate
            this._lblDataDate.Text = "일자";
            this._lblDataDate.AutoSize = true;
            this._lblDataDate.Margin = new Padding(2, 8, 6, 0);
            // _dtData
            this._dtData.Width = 130; this._dtData.Height = 23; this._dtData.Margin = new Padding(2, 5, 12, 3);
            // _btnDataReload
            this._btnDataReload.Text = "조회 / 새로고침";
            this._btnDataReload.AutoSize = true; this._btnDataReload.Height = 27;
            this._btnDataReload.Margin = new Padding(2, 1, 6, 1); this._btnDataReload.FlatStyle = FlatStyle.System;
            // _btnDataExport
            this._btnDataExport.Text = "CSV 내보내기";
            this._btnDataExport.AutoSize = true; this._btnDataExport.Height = 27;
            this._btnDataExport.Margin = new Padding(2, 1, 6, 1); this._btnDataExport.FlatStyle = FlatStyle.System;
            // _txtDataSearch
            this._txtDataSearch.Width = 180; this._txtDataSearch.Margin = new Padding(8, 3, 6, 3);
            // _gridData
            this._gridData.Dock = DockStyle.Fill;
            this._gridData.ReadOnly = true;
            this._gridData.AllowUserToAddRows = false;
            this._gridData.AllowUserToDeleteRows = false;
            this._gridData.RowHeadersVisible = false;
            this._gridData.MultiSelect = false;
            this._gridData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._gridData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            this._gridData.AllowUserToResizeRows = false;
            this._gridData.BorderStyle = BorderStyle.None;

            // _barLog
            this._barLog.Dock = DockStyle.Top;
            this._barLog.Height = 42;
            this._barLog.WrapContents = false;
            this._barLog.Padding = new Padding(8, 7, 8, 7);
            this._barLog.BackColor = UiTheme.OptionPanelBg;
            // _lblLogDate
            this._lblLogDate.Text = "일자";
            this._lblLogDate.AutoSize = true;
            this._lblLogDate.Margin = new Padding(2, 8, 6, 0);
            // _dtLog
            this._dtLog.Width = 130; this._dtLog.Height = 23; this._dtLog.Margin = new Padding(2, 5, 12, 3);
            // _btnLogReload
            this._btnLogReload.Text = "조회 / 새로고침";
            this._btnLogReload.AutoSize = true; this._btnLogReload.Height = 27;
            this._btnLogReload.Margin = new Padding(2, 1, 6, 1); this._btnLogReload.FlatStyle = FlatStyle.System;
            // _txtLogSearch
            this._txtLogSearch.Width = 180; this._txtLogSearch.Margin = new Padding(8, 3, 6, 3);
            // _gridLog
            this._gridLog.Dock = DockStyle.Fill;
            this._gridLog.ReadOnly = true;
            this._gridLog.AllowUserToAddRows = false;
            this._gridLog.AllowUserToDeleteRows = false;
            this._gridLog.RowHeadersVisible = false;
            this._gridLog.MultiSelect = false;
            this._gridLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._gridLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            this._gridLog.AllowUserToResizeRows = false;
            this._gridLog.BorderStyle = BorderStyle.None;

            // _barAlarm
            this._barAlarm.Dock = DockStyle.Top;
            this._barAlarm.Height = 42;
            this._barAlarm.WrapContents = false;
            this._barAlarm.Padding = new Padding(8, 7, 8, 7);
            this._barAlarm.BackColor = UiTheme.OptionPanelBg;
            // _chkActiveOnly
            this._chkActiveOnly.Text = "활성 알람만";
            this._chkActiveOnly.AutoSize = true;
            this._chkActiveOnly.Margin = new Padding(6, 6, 12, 0);
            // _btnAlarmReload
            this._btnAlarmReload.Text = "새로고침";
            this._btnAlarmReload.AutoSize = true; this._btnAlarmReload.Height = 27;
            this._btnAlarmReload.Margin = new Padding(2, 1, 6, 1); this._btnAlarmReload.FlatStyle = FlatStyle.System;
            // _txtAlarmSearch
            this._txtAlarmSearch.Width = 180; this._txtAlarmSearch.Margin = new Padding(8, 3, 6, 3);
            // _gridAlarm
            this._gridAlarm.Dock = DockStyle.Fill;
            this._gridAlarm.ReadOnly = true;
            this._gridAlarm.AllowUserToAddRows = false;
            this._gridAlarm.AllowUserToDeleteRows = false;
            this._gridAlarm.RowHeadersVisible = false;
            this._gridAlarm.MultiSelect = false;
            this._gridAlarm.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._gridAlarm.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            this._gridAlarm.AllowUserToResizeRows = false;
            this._gridAlarm.BorderStyle = BorderStyle.None;

            // _emptyData
            this._emptyData.Dock = DockStyle.Bottom;
            this._emptyData.Height = 26;
            this._emptyData.Text = "표시할 데이터가 없습니다 — 검사·이벤트·알람이 누적되면 표시됩니다.";
            this._emptyData.TextAlign = ContentAlignment.MiddleCenter;
            this._emptyData.ForeColor = Color.DimGray;
            this._emptyData.BackColor = Color.FromArgb(248, 249, 250);
            this._emptyData.Visible = false;
            // _emptyLog
            this._emptyLog.Dock = DockStyle.Bottom;
            this._emptyLog.Height = 26;
            this._emptyLog.Text = "표시할 데이터가 없습니다 — 검사·이벤트·알람이 누적되면 표시됩니다.";
            this._emptyLog.TextAlign = ContentAlignment.MiddleCenter;
            this._emptyLog.ForeColor = Color.DimGray;
            this._emptyLog.BackColor = Color.FromArgb(248, 249, 250);
            this._emptyLog.Visible = false;
            // _emptyAlarm
            this._emptyAlarm.Dock = DockStyle.Bottom;
            this._emptyAlarm.Height = 26;
            this._emptyAlarm.Text = "표시할 데이터가 없습니다 — 검사·이벤트·알람이 누적되면 표시됩니다.";
            this._emptyAlarm.TextAlign = ContentAlignment.MiddleCenter;
            this._emptyAlarm.ForeColor = Color.DimGray;
            this._emptyAlarm.BackColor = Color.FromArgb(248, 249, 250);
            this._emptyAlarm.Visible = false;

            // Utility
            this._flowUtil.Dock = DockStyle.Fill;
            this._flowUtil.Padding = new Padding(16);
            this._flowUtil.BackColor = UiTheme.OptionPanelBg;
            // _btnOpenDataFolder
            this._btnOpenDataFolder.Text = "Data Log 폴더 열기";
            this._btnOpenDataFolder.Width = 200; this._btnOpenDataFolder.Height = 40;
            this._btnOpenDataFolder.Margin = new Padding(6); this._btnOpenDataFolder.FlatStyle = FlatStyle.System;
            // _btnOpenLogFolder
            this._btnOpenLogFolder.Text = "Event Log 폴더 열기";
            this._btnOpenLogFolder.Width = 200; this._btnOpenLogFolder.Height = 40;
            this._btnOpenLogFolder.Margin = new Padding(6); this._btnOpenLogFolder.FlatStyle = FlatStyle.System;
            // _btnRefreshAll
            this._btnRefreshAll.Text = "전체 새로고침";
            this._btnRefreshAll.Width = 200; this._btnRefreshAll.Height = 40;
            this._btnRefreshAll.Margin = new Padding(6); this._btnRefreshAll.FlatStyle = FlatStyle.System;
            // _lblUtilInfo
            this._lblUtilInfo.AutoSize = true;
            this._lblUtilInfo.Margin = new Padding(6, 16, 0, 0);
            this._lblUtilInfo.ForeColor = Color.DimGray;

            // Data Log 탭
            this._barData.Controls.Add(this._lblDataDate);
            this._barData.Controls.Add(this._dtData);
            this._barData.Controls.Add(this._btnDataReload);
            this._barData.Controls.Add(this._btnDataExport);
            this._barData.Controls.Add(this._txtDataSearch);
            this._txtDataSearch.TextChanged += this._txtDataSearch_TextChanged;
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
            this._txtLogSearch.TextChanged += this._txtLogSearch_TextChanged;
            this._tpLog.Controls.Add(this._gridLog);
            this._tpLog.Controls.Add(this._emptyLog);
            this._tpLog.Controls.Add(this._barLog);
            this._btnLogReload.Click += this._btnLogReload_Click;
            this._dtLog.ValueChanged += this._dtLog_ValueChanged;

            // Alarm 탭
            this._barAlarm.Controls.Add(this._chkActiveOnly);
            this._barAlarm.Controls.Add(this._btnAlarmReload);
            this._barAlarm.Controls.Add(this._txtAlarmSearch);
            this._txtAlarmSearch.TextChanged += this._txtAlarmSearch_TextChanged;
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

            ((System.ComponentModel.ISupportInitialize)(this._gridData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridAlarm)).EndInit();
            this._tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
