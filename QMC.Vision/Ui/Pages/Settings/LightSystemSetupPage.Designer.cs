using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class LightSystemSetupPage
    {
        private System.ComponentModel.IContainer components = null;

        // 헤더 / 툴바 / 상태
        private Label _hdr;
        private TableLayoutPanel _bar;   // 하단 버튼 툴바 (GENERAL 동일 프레임)
        private Button _btnSave, _btnReload, _btnAddCtrl, _btnDelCtrl, _btnMigrate, _btnRename, _btnConnect, _btnDisc;
        // _lblStatus 는 Code 측 partial 에 이미 선언됨

        // 레이아웃 컨테이너 (C3b-3: 결선 섹션 제거 — 컨트롤러 인벤토리만)
        private TableLayoutPanel _body;
        private Label _lblSecCtrl, _lblSecLabel;
        private Panel _lnSec1, _lnSec2;   // 섹션 타이틀 주황 밑줄 (GENERAL _ln 스타일)
        // 그리드는 Code 측 partial 에 이미 선언됨 (_gridCtrl, _gridLabel)

        // LFine 하드웨어 모드(SM) — 런타임 송신 (LightControllerMode 잔재의 Mode 컬럼 대체)
        private Panel    _pnlHwMode;
        private Label    _lblHwMode;
        private ComboBox _cbHwMode;
        private Button   _btnHwModeApply;
        private Label    _lblHwModeState;

        // 컬럼 (정적 구조)
        private DataGridViewTextBoxColumn  _colPort, _colName, _colBaud, _colChCount, _colPageCount, _colMaxPower;
        private DataGridViewComboBoxColumn _colVendor;
        private DataGridViewTextBoxColumn  _colLblCh, _colLblName, _colLblColor;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._bar = new TableLayoutPanel();
            this._btnSave = new Button(); this._btnReload = new Button();
            this._btnAddCtrl = new Button(); this._btnDelCtrl = new Button();
            this._btnMigrate = new Button(); this._btnRename = new Button();
            this._btnConnect = new Button(); this._btnDisc = new Button();
            this._lblStatus = new Label();
            this._body = new TableLayoutPanel();
            this._lblSecCtrl = new Label(); this._lblSecLabel = new Label();
            this._lnSec1 = new Panel(); this._lnSec2 = new Panel();
            this._gridCtrl = new DataGridView();
            this._gridLabel = new DataGridView();
            this._colPort = new DataGridViewTextBoxColumn(); this._colName = new DataGridViewTextBoxColumn();
            this._colBaud = new DataGridViewTextBoxColumn(); this._colChCount = new DataGridViewTextBoxColumn();
            this._colPageCount = new DataGridViewTextBoxColumn(); this._colMaxPower = new DataGridViewTextBoxColumn();
            this._colVendor = new DataGridViewComboBoxColumn();
            this._colLblCh = new DataGridViewTextBoxColumn(); this._colLblName = new DataGridViewTextBoxColumn();
            this._colLblColor = new DataGridViewTextBoxColumn();
            this._pnlHwMode = new Panel();
            this._lblHwMode = new Label();
            this._cbHwMode = new ComboBox();
            this._btnHwModeApply = new Button();
            this._lblHwModeState = new Label();

            this._bar.SuspendLayout();
            this._body.SuspendLayout();
            this._lblSecCtrl.SuspendLayout();
            this._lblSecLabel.SuspendLayout();
            ((ISupportInitialize)this._gridCtrl).BeginInit();
            ((ISupportInitialize)this._gridLabel).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;
            this.Padding = new Padding(12, 8, 12, 8);

            // ── 상단 주황 헤더 (GENERAL 동일) ──
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 32;
            this._hdr.Text = "조명 셋업";
            this._hdr.Tag = "i18n:set.lightSetup";
            this._hdr.BackColor = Color.FromArgb(217, 119, 6);
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // ── 하단 버튼 툴바 (TableLayoutPanel — 좌측 액션 / 스페이서 / 우측 불러오기·저장) ──
            this._bar.Dock = DockStyle.Bottom;
            this._bar.Height = 46;
            this._bar.ColumnCount = 9;
            this._bar.RowCount = 1;
            this._bar.Padding = new Padding(0, 0, 0, 0);
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F)); // 컨트롤러 추가
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F)); // 컨트롤러 삭제
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // io_set 가져오기
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // 포트 일괄 변경
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // 조명 연결
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // 조명 해제
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // 스페이서
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F)); // 불러오기
            this._bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F)); // 저장
            this._bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 컨트롤러 추가
            this._btnAddCtrl.Dock = DockStyle.Fill;
            this._btnAddCtrl.Text = "컨트롤러 추가";
            this._btnAddCtrl.FlatStyle = FlatStyle.Flat;
            this._btnAddCtrl.Font = new Font("맑은 고딕", 10.5F);
            this._btnAddCtrl.BackColor = Color.White;
            this._btnAddCtrl.ForeColor = Color.Black;
            this._btnAddCtrl.UseVisualStyleBackColor = false;
            // 컨트롤러 삭제
            this._btnDelCtrl.Dock = DockStyle.Fill;
            this._btnDelCtrl.Text = "컨트롤러 삭제";
            this._btnDelCtrl.FlatStyle = FlatStyle.Flat;
            this._btnDelCtrl.Font = new Font("맑은 고딕", 10.5F);
            this._btnDelCtrl.BackColor = Color.White;
            this._btnDelCtrl.ForeColor = Color.Black;
            this._btnDelCtrl.UseVisualStyleBackColor = false;
            // io_set 가져오기
            this._btnMigrate.Dock = DockStyle.Fill;
            this._btnMigrate.Text = "io_set 가져오기";
            this._btnMigrate.FlatStyle = FlatStyle.Flat;
            this._btnMigrate.Font = new Font("맑은 고딕", 10.5F);
            this._btnMigrate.BackColor = Color.White;
            this._btnMigrate.ForeColor = Color.Black;
            this._btnMigrate.UseVisualStyleBackColor = false;
            // 포트 일괄 변경
            this._btnRename.Dock = DockStyle.Fill;
            this._btnRename.Text = "포트 일괄 변경";
            this._btnRename.FlatStyle = FlatStyle.Flat;
            this._btnRename.Font = new Font("맑은 고딕", 10.5F);
            this._btnRename.BackColor = Color.White;
            this._btnRename.ForeColor = Color.Black;
            this._btnRename.UseVisualStyleBackColor = false;
            // 조명 연결 (녹색)
            this._btnConnect.Dock = DockStyle.Fill;
            this._btnConnect.Text = "조명 연결";
            this._btnConnect.FlatStyle = FlatStyle.Flat;
            this._btnConnect.Font = new Font("맑은 고딕", 10.5F);
            this._btnConnect.BackColor = Color.FromArgb(0x2E, 0x7D, 0x32);
            this._btnConnect.ForeColor = Color.White;
            this._btnConnect.UseVisualStyleBackColor = false;
            // 조명 해제 (흰색)
            this._btnDisc.Dock = DockStyle.Fill;
            this._btnDisc.Text = "조명 해제";
            this._btnDisc.FlatStyle = FlatStyle.Flat;
            this._btnDisc.Font = new Font("맑은 고딕", 10.5F);
            this._btnDisc.BackColor = Color.White;
            this._btnDisc.ForeColor = Color.Black;
            this._btnDisc.UseVisualStyleBackColor = false;
            // 불러오기 (흰색 — GENERAL 동일 개념)
            this._btnReload.Dock = DockStyle.Fill;
            this._btnReload.Text = "불러오기";
            this._btnReload.Tag = "i18n:common.load";
            this._btnReload.FlatStyle = FlatStyle.Flat;
            this._btnReload.Font = new Font("맑은 고딕", 10.5F);
            this._btnReload.BackColor = Color.White;
            this._btnReload.ForeColor = Color.Black;
            this._btnReload.UseVisualStyleBackColor = false;
            // 저장 (주황 강조 — GENERAL 동일)
            this._btnSave.Dock = DockStyle.Fill;
            this._btnSave.Text = "저장";
            this._btnSave.Tag = "i18n:common.save";
            this._btnSave.FlatStyle = FlatStyle.Flat;
            this._btnSave.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold);
            this._btnSave.BackColor = Color.FromArgb(232, 93, 26);
            this._btnSave.ForeColor = Color.White;
            this._btnSave.UseVisualStyleBackColor = false;

            this._btnSave.Click    += new System.EventHandler(this.OnSaveClick);
            this._btnReload.Click  += new System.EventHandler(this.OnReloadClick);
            this._btnAddCtrl.Click += new System.EventHandler(this.OnAddCtrlClick);
            this._btnDelCtrl.Click += new System.EventHandler(this.OnDelCtrlClick);
            this._btnMigrate.Click += new System.EventHandler(this.OnMigrateClick);
            this._btnRename.Click  += new System.EventHandler(this.OnRenameClick);
            this._btnConnect.Click += new System.EventHandler(this.OnConnectLightsClick);
            this._btnDisc.Click    += new System.EventHandler(this.OnDisconnectLightsClick);

            this._bar.Controls.Add(this._btnAddCtrl, 0, 0);
            this._bar.Controls.Add(this._btnDelCtrl, 1, 0);
            this._bar.Controls.Add(this._btnMigrate, 2, 0);
            this._bar.Controls.Add(this._btnRename,  3, 0);
            this._bar.Controls.Add(this._btnConnect, 4, 0);
            this._bar.Controls.Add(this._btnDisc,    5, 0);
            this._bar.Controls.Add(this._btnReload,  7, 0);
            this._bar.Controls.Add(this._btnSave,    8, 0);

            // ── 상태바 ──
            this._lblStatus.Dock = DockStyle.Bottom;
            this._lblStatus.Height = 24;
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.Padding = new Padding(8, 2, 0, 0);

            // ── 본문: 컨트롤러 인벤토리 + 채널 라벨 (C3b-3: 결선 섹션 제거) ──
            this._body.Dock = DockStyle.Fill;
            this._body.ColumnCount = 1;
            this._body.RowCount = 5;
            this._body.BackColor = UiTheme.MainBg;
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // 섹션 타이틀 1 — 컨트롤러 인벤토리 (주황 밑줄)
            this._lblSecCtrl.Controls.Add(this._lnSec1);
            this._lblSecCtrl.Dock = DockStyle.Fill;
            this._lblSecCtrl.Text = "컨트롤러 인벤토리 (PortName 고유)";
            this._lblSecCtrl.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold);
            this._lblSecCtrl.ForeColor = Color.FromArgb(45, 45, 45);
            this._lblSecCtrl.Padding = new Padding(2, 0, 0, 0);
            this._lblSecCtrl.TextAlign = ContentAlignment.MiddleLeft;
            // 밑줄 1
            this._lnSec1.BackColor = Color.FromArgb(217, 119, 6);
            this._lnSec1.Dock = DockStyle.Bottom;
            this._lnSec1.Height = 2;

            // 섹션 타이틀 2 — 채널 라벨 (주황 밑줄)
            this._lblSecLabel.Controls.Add(this._lnSec2);
            this._lblSecLabel.Dock = DockStyle.Fill;
            this._lblSecLabel.Text = "선택 컨트롤러의 채널 라벨 — Ch 번호 직접 지정 (1~Ch수, 비연속/부분 사용 가능)";
            this._lblSecLabel.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold);
            this._lblSecLabel.ForeColor = Color.FromArgb(45, 45, 45);
            this._lblSecLabel.Padding = new Padding(2, 0, 0, 0);
            this._lblSecLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 밑줄 2
            this._lnSec2.BackColor = Color.FromArgb(217, 119, 6);
            this._lnSec2.Dock = DockStyle.Bottom;
            this._lnSec2.Height = 2;

            // _gridCtrl + 컬럼
            this._gridCtrl.Dock = DockStyle.Fill; this._gridCtrl.AllowUserToAddRows = true; this._gridCtrl.AllowUserToDeleteRows = true; this._gridCtrl.RowHeadersVisible = false; this._gridCtrl.Font = UiTheme.ValueFont; this._gridCtrl.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._gridCtrl.BackgroundColor = Color.FromArgb(245, 246, 248);
            this._gridCtrl.BorderStyle = BorderStyle.None;
            this._gridCtrl.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this._gridCtrl.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            this._gridCtrl.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            this._gridCtrl.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 234, 237);
            this._gridCtrl.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this._gridCtrl.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(48, 52, 58);
            this._gridCtrl.ColumnHeadersHeight = 28;
            this._gridCtrl.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._gridCtrl.EnableHeadersVisualStyles = false;
            this._gridCtrl.GridColor = Color.FromArgb(226, 228, 232);
            this._gridCtrl.RowTemplate.Height = 30;
            this._gridCtrl.AllowUserToResizeRows = false;
            this._colPort.Name = "PortName"; this._colPort.HeaderText = "PortName";
            this._colName.Name = "Name"; this._colName.HeaderText = "Name";
            this._colBaud.Name = "BaudRate"; this._colBaud.HeaderText = "Baud";
            this._colChCount.Name = "ChannelCount"; this._colChCount.HeaderText = "Ch수";
            this._colPageCount.Name = "PageCount"; this._colPageCount.HeaderText = "Page수";
            this._colMaxPower.Name = "MaxPower"; this._colMaxPower.HeaderText = "MaxPwr";
            this._gridCtrl.Columns.Add(this._colPort);
            this._gridCtrl.Columns.Add(this._colName);
            this._gridCtrl.Columns.Add(this._colBaud);
            this._gridCtrl.Columns.Add(this._colChCount);
            this._gridCtrl.Columns.Add(this._colPageCount);
            this._gridCtrl.Columns.Add(this._colMaxPower);
            this._colVendor.Name = "Vendor"; this._colVendor.HeaderText = "Vendor"; this._colVendor.FlatStyle = FlatStyle.Flat;
            this._colVendor.Items.Add("LFine"); this._colVendor.Items.Add("Leesos");
            this._gridCtrl.Columns.Add(this._colVendor);
            this._colVendor.DisplayIndex = 1;
            this._gridCtrl.DataError       += new DataGridViewDataErrorEventHandler(this.OnGridDataError);
            this._gridCtrl.CellEndEdit     += new DataGridViewCellEventHandler(this.GridCtrl_VendorCellEndEdit);
            this._gridCtrl.SelectionChanged += new System.EventHandler(this.OnGridCtrlSelectionChanged);
            this._gridCtrl.KeyDown         += new KeyEventHandler(this.OnGridCtrlKeyDown);
            this._gridCtrl.CellEndEdit     += new DataGridViewCellEventHandler(this.GridCtrl_CellEndEdit);
            this._gridCtrl.CellBeginEdit   += new DataGridViewCellCancelEventHandler(this.OnGridCtrlCellBeginEdit);

            // _gridLabel + 컬럼
            this._gridLabel.Dock = DockStyle.Fill; this._gridLabel.AllowUserToAddRows = true; this._gridLabel.AllowUserToDeleteRows = true; this._gridLabel.RowHeadersVisible = false; this._gridLabel.Font = UiTheme.ValueFont; this._gridLabel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._gridLabel.BackgroundColor = Color.FromArgb(245, 246, 248);
            this._gridLabel.BorderStyle = BorderStyle.None;
            this._gridLabel.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this._gridLabel.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            this._gridLabel.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            this._gridLabel.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 234, 237);
            this._gridLabel.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this._gridLabel.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(48, 52, 58);
            this._gridLabel.ColumnHeadersHeight = 28;
            this._gridLabel.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._gridLabel.EnableHeadersVisualStyles = false;
            this._gridLabel.GridColor = Color.FromArgb(226, 228, 232);
            this._gridLabel.RowTemplate.Height = 30;
            this._gridLabel.AllowUserToResizeRows = false;
            this._colLblCh.Name = "Channel"; this._colLblCh.HeaderText = "Ch";
            this._colLblName.Name = "Name"; this._colLblName.HeaderText = "Name";
            this._colLblColor.Name = "Color"; this._colLblColor.HeaderText = "Color";
            this._gridLabel.Columns.Add(this._colLblCh);
            this._gridLabel.Columns.Add(this._colLblName);
            this._gridLabel.Columns.Add(this._colLblColor);
            this._gridLabel.UserAddedRow += new DataGridViewRowEventHandler(this.OnGridLabelUserAddedRow);
            this._gridLabel.RowsRemoved  += new DataGridViewRowsRemovedEventHandler(this.OnGridLabelRowsRemoved);
            this._gridLabel.CellEndEdit  += new DataGridViewCellEventHandler(this.OnGridLabelCellEndEdit);

            // ── LFine 하드웨어 모드(SM) 행 — 선택 컨트롤러가 LFine 일 때만 활성 (런타임 송신, 영속 아님) ──
            this._pnlHwMode.Dock = DockStyle.Fill;
            this._pnlHwMode.BackColor = Color.WhiteSmoke;
            this._lblHwMode.Location = new Point(8, 13);
            this._lblHwMode.Size = new Size(170, 22);
            this._lblHwMode.Font = UiTheme.ButtonFont;
            this._lblHwMode.ForeColor = Color.Black;
            this._lblHwMode.Text = "LFine HW Mode (SM)";
            this._lblHwMode.TextAlign = ContentAlignment.MiddleLeft;
            this._cbHwMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbHwMode.Font = UiTheme.ValueFont;
            this._cbHwMode.Location = new Point(182, 12);
            this._cbHwMode.Size = new Size(180, 24);
            this._cbHwMode.Items.Add("Page Trigger (0)");
            this._cbHwMode.Items.Add("Software Trigger (3)");
            this._btnHwModeApply.Location = new Point(372, 10);
            this._btnHwModeApply.Size = new Size(80, 28);
            this._btnHwModeApply.Text = "적용";
            this._btnHwModeApply.FlatStyle = FlatStyle.Flat;
            this._btnHwModeApply.Font = UiTheme.ButtonFont;
            this._btnHwModeApply.BackColor = Color.White;
            this._btnHwModeApply.ForeColor = Color.Black;
            this._btnHwModeApply.Click += new System.EventHandler(this.OnHwModeApplyClick);
            this._lblHwModeState.Location = new Point(462, 13);
            this._lblHwModeState.Size = new Size(560, 22);
            this._lblHwModeState.Font = UiTheme.ValueFont;
            this._lblHwModeState.ForeColor = Color.DarkSlateGray;
            this._lblHwModeState.TextAlign = ContentAlignment.MiddleLeft;
            this._pnlHwMode.Controls.Add(this._lblHwMode);
            this._pnlHwMode.Controls.Add(this._cbHwMode);
            this._pnlHwMode.Controls.Add(this._btnHwModeApply);
            this._pnlHwMode.Controls.Add(this._lblHwModeState);

            this._body.Controls.Add(this._lblSecCtrl, 0, 0);
            this._body.Controls.Add(this._gridCtrl, 0, 1);
            this._body.Controls.Add(this._pnlHwMode, 0, 2);
            this._body.Controls.Add(this._lblSecLabel, 0, 3);
            this._body.Controls.Add(this._gridLabel, 0, 4);

            // ── 최상위 컨트롤 추가 (docked: 나중에 Add된 것이 먼저 도킹 → 본문 Fill 마지막) ──
            this.Controls.Add(this._body);       // Fill (가장 먼저 Add → 나머지 docked 가 잠식 후 남는 공간)
            this.Controls.Add(this._lblStatus);  // Bottom
            this.Controls.Add(this._bar);        // Bottom (상태바 위)
            this.Controls.Add(this._hdr);        // Top
            this.Name = "LightSystemSetupPage";

            ((ISupportInitialize)this._gridCtrl).EndInit();
            ((ISupportInitialize)this._gridLabel).EndInit();
            this._lblSecCtrl.ResumeLayout(false);
            this._lblSecLabel.ResumeLayout(false);
            this._body.ResumeLayout(false);
            this._bar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // ── 툴바 버튼 공통 스타일(미사용 — 인라인 전환됨, 호환 위해 정의 유지) ──
        private static void ConfigureBarBtn(Button b, string text, int width, bool primary)
        {
            b.Size = new Size(width, 32);
            b.Margin = new Padding(0, 0, 6, 0);
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = UiTheme.ButtonFont;
            b.BackColor = primary ? UiTheme.Accent : Color.White;
            b.ForeColor = primary ? Color.White : Color.Black;
        }
    }
}
