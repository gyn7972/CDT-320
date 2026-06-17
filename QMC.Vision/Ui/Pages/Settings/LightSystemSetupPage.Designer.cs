using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class LightSystemSetupPage
    {
        private System.ComponentModel.IContainer components = null;

        // 헤더 / 툴바 / 상태
        private Label  _hdr;
        private Panel  _bar;
        private Button _btnSave, _btnReload, _btnAddCtrl, _btnDelCtrl, _btnMigrate, _btnRename, _btnConnect, _btnDisc;
        // _lblStatus 는 Code 측 partial 에 이미 선언됨

        // 레이아웃 컨테이너 (C3b-3: 결선 섹션 제거 — 컨트롤러 인벤토리만)
        private TableLayoutPanel _body;
        private Label _lblSecCtrl, _lblSecLabel;
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
            this._bar = new Panel();
            this._btnSave = new Button(); this._btnReload = new Button();
            this._btnAddCtrl = new Button(); this._btnDelCtrl = new Button();
            this._btnMigrate = new Button(); this._btnRename = new Button();
            this._btnConnect = new Button(); this._btnDisc = new Button();
            this._lblStatus = new Label();
            this._body = new TableLayoutPanel();
            this._lblSecCtrl = new Label(); this._lblSecLabel = new Label();
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
            ((ISupportInitialize)this._gridCtrl).BeginInit();
            ((ISupportInitialize)this._gridLabel).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // ── 헤더 ──
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "조명 시스템 설정 — 컨트롤러 인벤토리 (한 번 설정 후 거의 변경 없음)   ※ 검사별 컨트롤러/페이지 지정은 [설정>검사], 밝기/On-Off 는 [레시피]";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // ── 툴바 ──
            this._bar.Dock = DockStyle.Top;
            this._bar.Height = 40;
            this._bar.BackColor = Color.WhiteSmoke;
            this._btnSave.Location = new Point(8, 4); this._btnSave.Size = new Size(100, 32); this._btnSave.Text = "저장"; this._btnSave.FlatStyle = FlatStyle.Flat; this._btnSave.Font = UiTheme.ButtonFont; this._btnSave.BackColor = UiTheme.Accent; this._btnSave.ForeColor = Color.White;
            this._btnReload.Location = new Point(120, 4); this._btnReload.Size = new Size(100, 32); this._btnReload.Text = "취소"; this._btnReload.FlatStyle = FlatStyle.Flat; this._btnReload.Font = UiTheme.ButtonFont; this._btnReload.BackColor = Color.White; this._btnReload.ForeColor = Color.Black;
            this._btnAddCtrl.Location = new Point(210, 4); this._btnAddCtrl.Size = new Size(120, 32); this._btnAddCtrl.Text = "컨트롤러 추가"; this._btnAddCtrl.FlatStyle = FlatStyle.Flat; this._btnAddCtrl.Font = UiTheme.ButtonFont; this._btnAddCtrl.BackColor = Color.White; this._btnAddCtrl.ForeColor = Color.Black;
            this._btnDelCtrl.Location = new Point(340, 4); this._btnDelCtrl.Size = new Size(120, 32); this._btnDelCtrl.Text = "컨트롤러 삭제"; this._btnDelCtrl.FlatStyle = FlatStyle.Flat; this._btnDelCtrl.Font = UiTheme.ButtonFont; this._btnDelCtrl.BackColor = Color.White; this._btnDelCtrl.ForeColor = Color.Black;
            this._btnMigrate.Location = new Point(470, 4); this._btnMigrate.Size = new Size(130, 32); this._btnMigrate.Text = "io_set 가져오기"; this._btnMigrate.FlatStyle = FlatStyle.Flat; this._btnMigrate.Font = UiTheme.ButtonFont; this._btnMigrate.BackColor = Color.White; this._btnMigrate.ForeColor = Color.Black;
            this._btnRename.Location = new Point(610, 4); this._btnRename.Size = new Size(130, 32); this._btnRename.Text = "포트 일괄 변경"; this._btnRename.FlatStyle = FlatStyle.Flat; this._btnRename.Font = UiTheme.ButtonFont; this._btnRename.BackColor = Color.White; this._btnRename.ForeColor = Color.Black;
            this._btnConnect.Location = new Point(750, 4); this._btnConnect.Size = new Size(110, 32); this._btnConnect.Text = "조명 연결"; this._btnConnect.FlatStyle = FlatStyle.Flat; this._btnConnect.Font = UiTheme.ButtonFont; this._btnConnect.BackColor = Color.FromArgb(0x2E, 0x7D, 0x32); this._btnConnect.ForeColor = Color.White;
            this._btnDisc.Location = new Point(870, 4); this._btnDisc.Size = new Size(110, 32); this._btnDisc.Text = "조명 해제"; this._btnDisc.FlatStyle = FlatStyle.Flat; this._btnDisc.Font = UiTheme.ButtonFont; this._btnDisc.BackColor = Color.White; this._btnDisc.ForeColor = Color.Black;
            this._btnSave.Click    += new System.EventHandler(this.OnSaveClick);
            this._btnReload.Click  += new System.EventHandler(this.OnReloadClick);
            this._btnAddCtrl.Click += new System.EventHandler(this.OnAddCtrlClick);
            this._btnDelCtrl.Click += new System.EventHandler(this.OnDelCtrlClick);
            this._btnMigrate.Click += new System.EventHandler(this.OnMigrateClick);
            this._btnRename.Click  += new System.EventHandler(this.OnRenameClick);
            this._btnConnect.Click += new System.EventHandler(this.OnConnectLightsClick);
            this._btnDisc.Click    += new System.EventHandler(this.OnDisconnectLightsClick);
            this._bar.Controls.Add(this._btnSave);
            this._bar.Controls.Add(this._btnReload);
            this._bar.Controls.Add(this._btnAddCtrl);
            this._bar.Controls.Add(this._btnDelCtrl);
            this._bar.Controls.Add(this._btnMigrate);
            this._bar.Controls.Add(this._btnRename);
            this._bar.Controls.Add(this._btnConnect);
            this._bar.Controls.Add(this._btnDisc);

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
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            this._lblSecCtrl.Dock = DockStyle.Fill; this._lblSecCtrl.Text = "  컨트롤러 인벤토리 (PortName 고유)"; this._lblSecCtrl.Font = UiTheme.ButtonFont; this._lblSecCtrl.ForeColor = Color.Black; this._lblSecCtrl.BackColor = UiTheme.SidebarHeaderBg; this._lblSecCtrl.TextAlign = ContentAlignment.MiddleLeft;
            this._lblSecLabel.Dock = DockStyle.Fill; this._lblSecLabel.Text = "  선택 컨트롤러의 채널 라벨 — Ch 번호 직접 지정 (1~Ch수, 비연속/부분 사용 가능)"; this._lblSecLabel.Font = UiTheme.ButtonFont; this._lblSecLabel.ForeColor = Color.Black; this._lblSecLabel.BackColor = UiTheme.SidebarHeaderBg; this._lblSecLabel.TextAlign = ContentAlignment.MiddleLeft;

            // _gridCtrl + 컬럼
            this._gridCtrl.Dock = DockStyle.Fill; this._gridCtrl.AllowUserToAddRows = true; this._gridCtrl.AllowUserToDeleteRows = true; this._gridCtrl.RowHeadersVisible = false; this._gridCtrl.Font = UiTheme.ValueFont; this._gridCtrl.BackgroundColor = Color.White; this._gridCtrl.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            this._gridLabel.Dock = DockStyle.Fill; this._gridLabel.AllowUserToAddRows = true; this._gridLabel.AllowUserToDeleteRows = true; this._gridLabel.RowHeadersVisible = false; this._gridLabel.Font = UiTheme.ValueFont; this._gridLabel.BackgroundColor = Color.White; this._gridLabel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            this._lblHwMode.Location = new Point(8, 8);
            this._lblHwMode.Size = new Size(170, 22);
            this._lblHwMode.Font = UiTheme.ButtonFont;
            this._lblHwMode.ForeColor = Color.Black;
            this._lblHwMode.Text = "LFine HW Mode (SM)";
            this._lblHwMode.TextAlign = ContentAlignment.MiddleLeft;
            this._cbHwMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbHwMode.Font = UiTheme.ValueFont;
            this._cbHwMode.Location = new Point(182, 7);
            this._cbHwMode.Size = new Size(180, 24);
            this._cbHwMode.Items.Add("Page Trigger (0)");
            this._cbHwMode.Items.Add("Software Trigger (3)");
            this._btnHwModeApply.Location = new Point(372, 4);
            this._btnHwModeApply.Size = new Size(80, 28);
            this._btnHwModeApply.Text = "적용";
            this._btnHwModeApply.FlatStyle = FlatStyle.Flat;
            this._btnHwModeApply.Font = UiTheme.ButtonFont;
            this._btnHwModeApply.BackColor = Color.White;
            this._btnHwModeApply.ForeColor = Color.Black;
            this._btnHwModeApply.Click += new System.EventHandler(this.OnHwModeApplyClick);
            this._lblHwModeState.Location = new Point(462, 8);
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

            // ── 최상위 컨트롤 추가 ──
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._bar);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._body);
            this.Controls.SetChildIndex(this._body, 0);
            this.Name = "LightSystemSetupPage";

            ((ISupportInitialize)this._gridCtrl).EndInit();
            ((ISupportInitialize)this._gridLabel).EndInit();
            this._body.ResumeLayout(false);
            this._bar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
