namespace QMC.Vision.Ui.Pages
{
    partial class CommLinkPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label _hdr;
        private System.Windows.Forms.GroupBox grpPorts;
        private System.Windows.Forms.TableLayoutPanel portLayout;

        private System.Windows.Forms.Label lblWafer;
        private System.Windows.Forms.TextBox _tbWafer;
        private System.Windows.Forms.Label _lampWafer;
        private System.Windows.Forms.Label _rxWafer;

        private System.Windows.Forms.Label lblInsp;
        private System.Windows.Forms.TextBox _tbInsp;
        private System.Windows.Forms.Label _lampInsp;
        private System.Windows.Forms.Label _rxInsp;

        private System.Windows.Forms.Label lblBin;
        private System.Windows.Forms.TextBox _tbBin;
        private System.Windows.Forms.Label _lampBin;
        private System.Windows.Forms.Label _rxBin;

        private System.Windows.Forms.Label lblMain;
        private System.Windows.Forms.TextBox _tbMain;
        private System.Windows.Forms.Label _lampMain;
        private System.Windows.Forms.Label _rxMain;

        private System.Windows.Forms.Label lblTop;
        private System.Windows.Forms.TextBox _tbTop;
        private System.Windows.Forms.Label _lampTop;
        private System.Windows.Forms.Label _rxTop;

        private System.Windows.Forms.Label lblBot;
        private System.Windows.Forms.TextBox _tbBot;
        private System.Windows.Forms.Label _lampBot;
        private System.Windows.Forms.Label _rxBot;

        private System.Windows.Forms.Label lblHint;

        // 뷰어 포트(5200대, 영상 스트림) 입력 — 명령 포트 우측 컬럼. Main 채널은 영상 없음(없음).
        private System.Windows.Forms.TextBox _tbWaferV;
        private System.Windows.Forms.TextBox _tbInspV;
        private System.Windows.Forms.TextBox _tbBinV;
        private System.Windows.Forms.TextBox _tbTopV;
        private System.Windows.Forms.TextBox _tbBotV;
        private System.Windows.Forms.Label   _hdrCmd;
        private System.Windows.Forms.Label   _hdrView;
        // 뷰어 포트 접속 상태 램프(영상 스트림). Main 은 영상 없음.
        private System.Windows.Forms.Label   _vlampWafer;
        private System.Windows.Forms.Label   _vlampInsp;
        private System.Windows.Forms.Label   _vlampBin;
        private System.Windows.Forms.Label   _vlampTop;
        private System.Windows.Forms.Label   _vlampBot;

        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox _txtLog;

        private System.Windows.Forms.TableLayoutPanel _toolbar;
        private System.Windows.Forms.Button _btnClearLog;
        private System.Windows.Forms.Button _btnLoad;
        private System.Windows.Forms.Button _btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this._hdr = new System.Windows.Forms.Label();
            this.grpPorts = new System.Windows.Forms.GroupBox();
            this.portLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblWafer = new System.Windows.Forms.Label();
            this._tbWafer = new System.Windows.Forms.TextBox();
            this._lampWafer = new System.Windows.Forms.Label();
            this._rxWafer = new System.Windows.Forms.Label();
            this.lblInsp = new System.Windows.Forms.Label();
            this._tbInsp = new System.Windows.Forms.TextBox();
            this._lampInsp = new System.Windows.Forms.Label();
            this._rxInsp = new System.Windows.Forms.Label();
            this.lblBin = new System.Windows.Forms.Label();
            this._tbBin = new System.Windows.Forms.TextBox();
            this._lampBin = new System.Windows.Forms.Label();
            this._rxBin = new System.Windows.Forms.Label();
            this.lblMain = new System.Windows.Forms.Label();
            this._tbMain = new System.Windows.Forms.TextBox();
            this._lampMain = new System.Windows.Forms.Label();
            this._rxMain = new System.Windows.Forms.Label();
            this.lblTop = new System.Windows.Forms.Label();
            this._tbTop = new System.Windows.Forms.TextBox();
            this._lampTop = new System.Windows.Forms.Label();
            this._rxTop = new System.Windows.Forms.Label();
            this.lblBot = new System.Windows.Forms.Label();
            this._tbBot = new System.Windows.Forms.TextBox();
            this._lampBot = new System.Windows.Forms.Label();
            this._rxBot = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this._tbWaferV = new System.Windows.Forms.TextBox();
            this._tbInspV = new System.Windows.Forms.TextBox();
            this._tbBinV = new System.Windows.Forms.TextBox();
            this._tbTopV = new System.Windows.Forms.TextBox();
            this._tbBotV = new System.Windows.Forms.TextBox();
            this._hdrCmd = new System.Windows.Forms.Label();
            this._hdrView = new System.Windows.Forms.Label();
            this._vlampWafer = new System.Windows.Forms.Label();
            this._vlampInsp = new System.Windows.Forms.Label();
            this._vlampBin = new System.Windows.Forms.Label();
            this._vlampTop = new System.Windows.Forms.Label();
            this._vlampBot = new System.Windows.Forms.Label();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this._txtLog = new System.Windows.Forms.TextBox();
            this._toolbar = new System.Windows.Forms.TableLayoutPanel();
            this._btnClearLog = new System.Windows.Forms.Button();
            this._btnLoad = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.grpPorts.SuspendLayout();
            this.portLayout.SuspendLayout();
            this.grpLog.SuspendLayout();
            this._toolbar.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this._hdr, 0, 0);
            this.rootLayout.Controls.Add(this.grpPorts, 0, 1);
            this.rootLayout.Controls.Add(this.grpLog, 0, 2);
            this.rootLayout.Controls.Add(this._toolbar, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 352F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.Size = new System.Drawing.Size(1100, 760);
            this.rootLayout.TabIndex = 0;
            //
            // _hdr
            //
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "통신 (핸들러 ↔ Vision TCP)";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpPorts
            //
            this.grpPorts.Controls.Add(this.portLayout);
            this.grpPorts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPorts.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPorts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpPorts.Name = "grpPorts";
            this.grpPorts.Padding = new System.Windows.Forms.Padding(10);
            this.grpPorts.TabIndex = 1;
            this.grpPorts.TabStop = false;
            this.grpPorts.Text = "TCP 포트 / 상태 (Vision = 서버 · 포트 변경은 재시작 후 반영)";
            //
            // portLayout
            //
            this.portLayout.ColumnCount = 6;
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.portLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            // 헤더 행(row 0) — 두 입력 컬럼 의미 표시
            this.portLayout.Controls.Add(this._hdrCmd, 1, 0);
            this.portLayout.Controls.Add(this._hdrView, 2, 0);
            // 채널 행(1~6): 라벨 / 명령포트 / 뷰어포트 / 명령상태 / 뷰어상태 / 최근수신
            this.portLayout.Controls.Add(this.lblWafer, 0, 1);
            this.portLayout.Controls.Add(this._tbWafer, 1, 1);
            this.portLayout.Controls.Add(this._tbWaferV, 2, 1);
            this.portLayout.Controls.Add(this._lampWafer, 3, 1);
            this.portLayout.Controls.Add(this._vlampWafer, 4, 1);
            this.portLayout.Controls.Add(this._rxWafer, 5, 1);
            this.portLayout.Controls.Add(this.lblInsp, 0, 2);
            this.portLayout.Controls.Add(this._tbInsp, 1, 2);
            this.portLayout.Controls.Add(this._tbInspV, 2, 2);
            this.portLayout.Controls.Add(this._lampInsp, 3, 2);
            this.portLayout.Controls.Add(this._vlampInsp, 4, 2);
            this.portLayout.Controls.Add(this._rxInsp, 5, 2);
            this.portLayout.Controls.Add(this.lblBin, 0, 3);
            this.portLayout.Controls.Add(this._tbBin, 1, 3);
            this.portLayout.Controls.Add(this._tbBinV, 2, 3);
            this.portLayout.Controls.Add(this._lampBin, 3, 3);
            this.portLayout.Controls.Add(this._vlampBin, 4, 3);
            this.portLayout.Controls.Add(this._rxBin, 5, 3);
            this.portLayout.Controls.Add(this.lblMain, 0, 4);
            this.portLayout.Controls.Add(this._tbMain, 1, 4);
            this.portLayout.Controls.Add(this._lampMain, 3, 4);
            this.portLayout.Controls.Add(this._rxMain, 5, 4);
            this.portLayout.Controls.Add(this.lblTop, 0, 5);
            this.portLayout.Controls.Add(this._tbTop, 1, 5);
            this.portLayout.Controls.Add(this._tbTopV, 2, 5);
            this.portLayout.Controls.Add(this._lampTop, 3, 5);
            this.portLayout.Controls.Add(this._vlampTop, 4, 5);
            this.portLayout.Controls.Add(this._rxTop, 5, 5);
            this.portLayout.Controls.Add(this.lblBot, 0, 6);
            this.portLayout.Controls.Add(this._tbBot, 1, 6);
            this.portLayout.Controls.Add(this._tbBotV, 2, 6);
            this.portLayout.Controls.Add(this._lampBot, 3, 6);
            this.portLayout.Controls.Add(this._vlampBot, 4, 6);
            this.portLayout.Controls.Add(this._rxBot, 5, 6);
            this.portLayout.Controls.Add(this.lblHint, 0, 7);
            this.portLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.portLayout.Name = "portLayout";
            this.portLayout.RowCount = 8;
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.portLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.portLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.portLayout.TabIndex = 0;
            //
            // header labels
            //
            InitHeader(this._hdrCmd, "명령 포트");
            InitHeader(this._hdrView, "뷰어 포트(5200)");
            //
            // row controls
            //
            InitRow(this.lblWafer, "Wafer Vision", this._tbWafer, this._lampWafer, this._rxWafer, 0);
            InitRow(this.lblInsp, "Bottom Inspection", this._tbInsp, this._lampInsp, this._rxInsp, 4);
            InitRow(this.lblBin, "Bin Vision", this._tbBin, this._lampBin, this._rxBin, 8);
            InitRow(this.lblMain, "Main Comm", this._tbMain, this._lampMain, this._rxMain, 12);
            InitRow(this.lblTop, "Top Side Vision", this._tbTop, this._lampTop, this._rxTop, 16);
            InitRow(this.lblBot, "Bottom Side Vision", this._tbBot, this._lampBot, this._rxBot, 20);
            InitPortBox(this._tbWaferV, 2);
            InitPortBox(this._tbInspV, 6);
            InitPortBox(this._tbBinV, 10);
            InitPortBox(this._tbTopV, 18);
            InitPortBox(this._tbBotV, 22);
            // 뷰어 상태 램프
            InitLamp(this._vlampWafer);
            InitLamp(this._vlampInsp);
            InitLamp(this._vlampBin);
            InitLamp(this._vlampTop);
            InitLamp(this._vlampBot);
            //
            // lblHint
            //
            this.portLayout.SetColumnSpan(this.lblHint, 6);
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Name = "lblHint";
            this.lblHint.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblHint.TabIndex = 24;
            this.lblHint.Text = "명령 포트=핸들러 명령(GRAB/MATCH 등) · 뷰어 포트(5200대)=영상 스트림.  램프 2개 = 명령/뷰어 각 접속 상태(● 중지/대기/접속됨, 뷰어는 :포트 ×N=클라이언트 수).  Main 은 영상 없음.  RX=마지막 수신 경과(무통신 30s↑ 주황).";
            //
            // grpLog
            //
            this.grpLog.Controls.Add(this._txtLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpLog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpLog.Name = "grpLog";
            this.grpLog.Padding = new System.Windows.Forms.Padding(8);
            this.grpLog.TabIndex = 2;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "통신 로그 (TX / RX / EPD / ARM)";
            //
            // _txtLog
            //
            this._txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtLog.Font = new System.Drawing.Font("Consolas", 9.5F);
            this._txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            this._txtLog.Multiline = true;
            this._txtLog.Name = "_txtLog";
            this._txtLog.ReadOnly = true;
            this._txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._txtLog.WordWrap = false;
            this._txtLog.TabIndex = 0;
            //
            // _toolbar
            //
            this._toolbar.ColumnCount = 4;
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.Controls.Add(this._btnClearLog, 0, 0);
            this._toolbar.Controls.Add(this._btnLoad, 2, 0);
            this._toolbar.Controls.Add(this._btnSave, 3, 0);
            this._toolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._toolbar.Name = "_toolbar";
            this._toolbar.RowCount = 1;
            this._toolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.TabIndex = 3;
            //
            // _btnClearLog
            //
            this._btnClearLog.BackColor = System.Drawing.Color.White;
            this._btnClearLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearLog.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this._btnClearLog.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnClearLog.Name = "_btnClearLog";
            this._btnClearLog.TabIndex = 0;
            this._btnClearLog.Text = "로그 지움";
            this._btnClearLog.UseVisualStyleBackColor = false;
            //
            // _btnLoad
            //
            this._btnLoad.BackColor = System.Drawing.Color.White;
            this._btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoad.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this._btnLoad.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnLoad.Name = "_btnLoad";
            this._btnLoad.TabIndex = 1;
            this._btnLoad.Text = "불러오기";
            this._btnLoad.UseVisualStyleBackColor = false;
            //
            // _btnSave
            //
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnSave.Name = "_btnSave";
            this._btnSave.TabIndex = 2;
            this._btnSave.Text = "저장";
            this._btnSave.UseVisualStyleBackColor = false;
            //
            // CommLinkPage
            //
            this.Controls.Add(this.rootLayout);
            this.Name = "CommLinkPage";
            this.Size = new System.Drawing.Size(1100, 760);
            this.rootLayout.ResumeLayout(false);
            this.grpPorts.ResumeLayout(false);
            this.portLayout.ResumeLayout(false);
            this.portLayout.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this._toolbar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // 컬럼 헤더 라벨(명령/뷰어) 공통 속성.
        private void InitHeader(System.Windows.Forms.Label lbl, string text)
        {
            lbl.Dock = System.Windows.Forms.DockStyle.Fill;
            lbl.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.DimGray;
            lbl.Text = text;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        }

        // 상태 램프(뷰어) 공통 속성 — 명령 램프와 동일 스타일.
        private void InitLamp(System.Windows.Forms.Label lamp)
        {
            lamp.Dock = System.Windows.Forms.DockStyle.Fill;
            lamp.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            lamp.ForeColor = System.Drawing.Color.Gray;
            lamp.Text = "● 중지";
            lamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        // 추가 포트 입력칸(뷰어 포트) 공통 속성.
        private void InitPortBox(System.Windows.Forms.TextBox port, int tabIndex)
        {
            port.Dock = System.Windows.Forms.DockStyle.Fill;
            port.Font = new System.Drawing.Font("맑은 고딕", 10F);
            port.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            port.TabIndex = tabIndex;
        }

        // 한 행(라벨/포트입력/상태램프/최근수신)의 공통 속성 세팅.
        private void InitRow(System.Windows.Forms.Label name, string text,
                             System.Windows.Forms.TextBox port,
                             System.Windows.Forms.Label lamp,
                             System.Windows.Forms.Label rx, int tabBase)
        {
            name.Dock = System.Windows.Forms.DockStyle.Fill;
            name.Font = new System.Drawing.Font("맑은 고딕", 10F);
            name.Text = text;
            name.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            name.TabIndex = tabBase;

            port.Dock = System.Windows.Forms.DockStyle.Fill;
            port.Font = new System.Drawing.Font("맑은 고딕", 10F);
            port.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            port.TabIndex = tabBase + 1;

            lamp.Dock = System.Windows.Forms.DockStyle.Fill;
            lamp.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            lamp.ForeColor = System.Drawing.Color.Gray;
            lamp.Text = "● 중지";
            lamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lamp.TabIndex = tabBase + 2;

            rx.Dock = System.Windows.Forms.DockStyle.Fill;
            rx.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            rx.ForeColor = System.Drawing.Color.DimGray;
            rx.Text = "RX -";
            rx.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            rx.TabIndex = tabBase + 3;
        }
    }
}
