using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class SequencerPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private Label            _hdr;
        private FlowLayoutPanel  _bar;
        private Label    _lblMod;  private ComboBox _cbModule;
        private Label    _lblTool; private ComboBox _cbTool;
        private Label    _lblMode; private ComboBox _cbMode;
        private Button   _btnStart, _btnStop, _btnStep, _btnClear;
        private Button   _btnLoadStart, _btnLoadStop;   // CPU/메모리 부하 체크
        private DataGridView _metrics;
        private TableLayoutPanel _logSplit;   // 시퀀서로그 | 통신로그 반반
        private Label    _lblSeqLog, _lblCommLog;
        private TextBox  _log;
        private TextBox  _commLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root    = new TableLayoutPanel();
            this._hdr     = new Label();
            this._bar     = new FlowLayoutPanel();
            this._lblMod  = new Label();  this._cbModule = new ComboBox();
            this._lblTool = new Label();  this._cbTool   = new ComboBox();
            this._lblMode = new Label();  this._cbMode   = new ComboBox();
            this._btnStart = new Button(); this._btnStop = new Button(); this._btnStep = new Button(); this._btnClear = new Button();
            this._btnLoadStart = new Button(); this._btnLoadStop = new Button();
            this._metrics = new DataGridView();
            this._logSplit  = new TableLayoutPanel();
            this._lblSeqLog = new Label();
            this._lblCommLog = new Label();
            this._log     = new TextBox();
            this._commLog = new TextBox();
            this._root.SuspendLayout();
            this._bar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._metrics)).BeginInit();
            this.SuspendLayout();
            //
            // _root
            //
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._root.Controls.Add(this._hdr,     0, 0);
            this._root.Controls.Add(this._bar,     0, 1);
            this._root.Controls.Add(this._metrics, 0, 2);
            this._root.Controls.Add(this._logSplit, 0, 3);
            this._root.Dock = DockStyle.Fill;
            this._root.Name = "_root";
            this._root.RowCount = 4;
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            //
            // _hdr
            //
            this._hdr.BackColor = Color.FromArgb(217, 119, 6);
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            this._hdr.ForeColor = Color.White;
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new Padding(10, 0, 0, 0);
            this._hdr.Text = "시퀀서 — 개별 테스트";
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _bar
            //
            this._bar.Dock = DockStyle.Fill;
            this._bar.WrapContents = false;
            this._bar.Padding = new Padding(6, 8, 6, 6);
            this._bar.Name = "_bar";
            this._bar.Controls.Add(this._lblMod);
            this._bar.Controls.Add(this._cbModule);
            this._bar.Controls.Add(this._lblTool);
            this._bar.Controls.Add(this._cbTool);
            this._bar.Controls.Add(this._lblMode);
            this._bar.Controls.Add(this._cbMode);
            this._bar.Controls.Add(this._btnStart);
            this._bar.Controls.Add(this._btnStop);
            this._bar.Controls.Add(this._btnStep);
            this._bar.Controls.Add(this._btnClear);
            this._bar.Controls.Add(this._btnLoadStart);
            this._bar.Controls.Add(this._btnLoadStop);
            //
            // labels / combos
            //
            this._lblMod.Text = "모듈"; this._lblMod.AutoSize = false; this._lblMod.Size = new Size(40, 30);
            this._lblMod.TextAlign = ContentAlignment.MiddleRight; this._lblMod.Margin = new Padding(2, 6, 2, 2);
            this._cbModule.DropDownStyle = ComboBoxStyle.DropDownList; this._cbModule.Width = 160;
            this._cbModule.Font = new Font("맑은 고딕", 10F); this._cbModule.Margin = new Padding(2, 4, 12, 2);
            this._lblTool.Text = "도구"; this._lblTool.AutoSize = false; this._lblTool.Size = new Size(40, 30);
            this._lblTool.TextAlign = ContentAlignment.MiddleRight; this._lblTool.Margin = new Padding(2, 6, 2, 2);
            this._cbTool.DropDownStyle = ComboBoxStyle.DropDownList; this._cbTool.Width = 200;
            this._cbTool.Font = new Font("맑은 고딕", 10F); this._cbTool.Margin = new Padding(2, 4, 12, 2);
            this._lblMode.Text = "모드"; this._lblMode.AutoSize = false; this._lblMode.Size = new Size(40, 30);
            this._lblMode.TextAlign = ContentAlignment.MiddleRight; this._lblMode.Margin = new Padding(2, 6, 2, 2);
            this._cbMode.DropDownStyle = ComboBoxStyle.DropDownList; this._cbMode.Width = 130;
            this._cbMode.Font = new Font("맑은 고딕", 10F); this._cbMode.Margin = new Padding(2, 4, 12, 2);
            //
            // buttons
            //
            // _btnStart
            this._btnStart.Text = "시작";
            this._btnStart.BackColor = Color.FromArgb(31, 157, 77);
            this._btnStart.ForeColor = Color.White;
            this._btnStart.FlatStyle = FlatStyle.Flat;
            this._btnStart.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnStart.Size = new Size(96, 34);
            this._btnStart.Margin = new Padding(3, 4, 3, 2);
            this._btnStart.UseVisualStyleBackColor = false;
            // _btnStop
            this._btnStop.Text = "정지";
            this._btnStop.BackColor = Color.FromArgb(90, 34, 34);
            this._btnStop.ForeColor = Color.White;
            this._btnStop.FlatStyle = FlatStyle.Flat;
            this._btnStop.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnStop.Size = new Size(96, 34);
            this._btnStop.Margin = new Padding(3, 4, 3, 2);
            this._btnStop.UseVisualStyleBackColor = false;
            // _btnStep
            this._btnStep.Text = "한 단계";
            this._btnStep.BackColor = Color.White;
            this._btnStep.ForeColor = Color.Black;
            this._btnStep.FlatStyle = FlatStyle.Flat;
            this._btnStep.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnStep.Size = new Size(96, 34);
            this._btnStep.Margin = new Padding(3, 4, 3, 2);
            this._btnStep.UseVisualStyleBackColor = false;
            // _btnClear
            this._btnClear.Text = "로그 Clear";
            this._btnClear.BackColor = Color.White;
            this._btnClear.ForeColor = Color.Black;
            this._btnClear.FlatStyle = FlatStyle.Flat;
            this._btnClear.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnClear.Size = new Size(96, 34);
            this._btnClear.Margin = new Padding(3, 4, 3, 2);
            this._btnClear.UseVisualStyleBackColor = false;
            // _btnLoadStart
            this._btnLoadStart.Text = "부하 체크 시작";
            this._btnLoadStart.BackColor = Color.FromArgb(33, 102, 172);
            this._btnLoadStart.ForeColor = Color.White;
            this._btnLoadStart.FlatStyle = FlatStyle.Flat;
            this._btnLoadStart.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnLoadStart.Size = new Size(96, 34);
            this._btnLoadStart.Margin = new Padding(3, 4, 3, 2);
            this._btnLoadStart.UseVisualStyleBackColor = false;
            // _btnLoadStop
            this._btnLoadStop.Text = "체크 완료(차트)";
            this._btnLoadStop.BackColor = Color.White;
            this._btnLoadStop.ForeColor = Color.Black;
            this._btnLoadStop.FlatStyle = FlatStyle.Flat;
            this._btnLoadStop.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnLoadStop.Size = new Size(96, 34);
            this._btnLoadStop.Margin = new Padding(3, 4, 3, 2);
            this._btnLoadStop.UseVisualStyleBackColor = false;
            this._btnLoadStart.Width = 120; this._btnLoadStop.Width = 120;
            this._btnStart.Click += new System.EventHandler(this.OnStartClick);
            this._btnStop.Click  += new System.EventHandler(this.OnStopClick);
            this._btnStep.Click  += new System.EventHandler(this.OnStepClick);
            this._btnClear.Click += new System.EventHandler(this.OnClearClick);
            this._btnLoadStart.Click += new System.EventHandler(this.OnLoadCheckStartClick);
            this._btnLoadStop.Click  += new System.EventHandler(this.OnLoadCheckStopClick);
            //
            // _metrics — 모듈별 사이클 ms / 그랩 FPS / 사이클 수 (읽기전용)
            //
            this._metrics.Dock = DockStyle.Fill;
            this._metrics.Name = "_metrics";
            this._metrics.AllowUserToAddRows = false;
            this._metrics.AllowUserToDeleteRows = false;
            this._metrics.ReadOnly = true;
            this._metrics.RowHeadersVisible = false;
            this._metrics.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._metrics.BackgroundColor = Color.White;
            this._metrics.Font = new Font("맑은 고딕", 9.5F);
            this._metrics.Margin = new Padding(6, 2, 6, 6);
            //
            // _logSplit — 시퀀서 로그 | 통신 로그 (반반)
            //
            this._logSplit.Dock = DockStyle.Fill;
            this._logSplit.Name = "_logSplit";
            this._logSplit.ColumnCount = 2;
            this._logSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._logSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._logSplit.RowCount = 2;
            this._logSplit.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            this._logSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._logSplit.Controls.Add(this._lblSeqLog,  0, 0);
            this._logSplit.Controls.Add(this._lblCommLog, 1, 0);
            this._logSplit.Controls.Add(this._log,     0, 1);
            this._logSplit.Controls.Add(this._commLog, 1, 1);
            //
            // _lblSeqLog / _lblCommLog
            //
            this._lblSeqLog.Text = "시퀀서 로그";
            this._lblSeqLog.Dock = DockStyle.Fill;
            this._lblSeqLog.TextAlign = ContentAlignment.MiddleLeft;
            this._lblSeqLog.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this._lblSeqLog.ForeColor = Color.FromArgb(180, 255, 180);
            this._lblSeqLog.BackColor = Color.FromArgb(24, 24, 24);
            this._lblSeqLog.Padding = new Padding(6, 0, 0, 0);
            this._lblCommLog.Text = "통신 로그";
            this._lblCommLog.Dock = DockStyle.Fill;
            this._lblCommLog.TextAlign = ContentAlignment.MiddleLeft;
            this._lblCommLog.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this._lblCommLog.ForeColor = Color.FromArgb(170, 210, 255);
            this._lblCommLog.BackColor = Color.FromArgb(24, 24, 24);
            this._lblCommLog.Padding = new Padding(6, 0, 0, 0);
            //
            // _log
            //
            this._log.Dock = DockStyle.Fill;
            this._log.Multiline = true;
            this._log.ReadOnly = true;
            this._log.ScrollBars = ScrollBars.Vertical;
            this._log.BackColor = Color.FromArgb(24, 24, 24);
            this._log.ForeColor = Color.FromArgb(180, 255, 180);
            this._log.Font = new Font("Consolas", 9.5F);
            this._log.Name = "_log";
            this._log.Margin = new Padding(0, 0, 3, 0);
            //
            // _commLog — 핸들러↔비전 TCP RX/TX (VisionCommLog 폴링, CommLink 페이지와 동일 소스)
            //
            this._commLog.Dock = DockStyle.Fill;
            this._commLog.Multiline = true;
            this._commLog.ReadOnly = true;
            this._commLog.ScrollBars = ScrollBars.Vertical;
            this._commLog.BackColor = Color.FromArgb(24, 24, 24);
            this._commLog.ForeColor = Color.FromArgb(170, 210, 255);
            this._commLog.Font = new Font("Consolas", 9.5F);
            this._commLog.Name = "_commLog";
            this._commLog.Margin = new Padding(3, 0, 0, 0);
            //
            // SequencerPage
            //
            this.Controls.Add(this._root);
            this.Name = "SequencerPage";
            this.Size = new Size(1200, 760);
            ((System.ComponentModel.ISupportInitialize)(this._metrics)).EndInit();
            this._root.ResumeLayout(false);
            this._bar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
