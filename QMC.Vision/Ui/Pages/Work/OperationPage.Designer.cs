using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;   // CameraView

namespace QMC.Vision.Ui.Pages
{
    partial class OperationPage
    {
        private System.ComponentModel.IContainer components = null;

        // 3행 레이아웃(헤더 / 모듈 상태카드 / 모니터링) — Dock 적층 모호성 제거용 루트 TLP.
        private TableLayoutPanel _root;
        private Label            _hdr;
        private Button           _btnRun;        // RUN / STOP 토글
        private Button           _btnReady;      // READY — RUN 왼쪽, 핸들러 VISION 사용 승인
        private TableLayoutPanel _cardsHost;   // 5열 — 모듈 상태카드(런타임 채움, 비클릭)

        // 모니터링: 상단(메인 Bottom 크게 + Wafer/Bin) / 하단(TopSide·BottomSide 전체폭 가로 띠, 위아래 스택)
        private TableLayoutPanel _monitor;
        private CameraView       _camBig;       // 메인(Bottom Inspection)
        private TableLayoutPanel _smallHost;    // Wafer/Bin (상단 우측 2열)
        private TableLayoutPanel _sideHost;     // TopSide/BottomSide (하단 전체폭, 위아래 스택)
        private CameraView       _camS1;        // Wafer
        private CameraView       _camS2;        // Bin
        private CameraView       _camS3;        // TopSide (4000×700 가로 띠)
        private CameraView       _camS4;        // BottomSide (4000×700 가로 띠)

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopLive();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._hdr = new System.Windows.Forms.Label();
            this._btnRun = new System.Windows.Forms.Button();
            this._btnReady = new System.Windows.Forms.Button();
            this._cardsHost = new System.Windows.Forms.TableLayoutPanel();
            this._monitor = new System.Windows.Forms.TableLayoutPanel();
            this._camBig = new QMC.Vision.Ui.Controls.CameraView();
            this._smallHost = new System.Windows.Forms.TableLayoutPanel();
            this._sideHost = new System.Windows.Forms.TableLayoutPanel();
            this._camS1 = new QMC.Vision.Ui.Controls.CameraView();
            this._camS2 = new QMC.Vision.Ui.Controls.CameraView();
            this._camS3 = new QMC.Vision.Ui.Controls.CameraView();
            this._camS4 = new QMC.Vision.Ui.Controls.CameraView();
            this._root.SuspendLayout();
            this._hdr.SuspendLayout();
            this._monitor.SuspendLayout();
            this._smallHost.SuspendLayout();
            this._sideHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // _root
            // 
            this._root.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Controls.Add(this._hdr, 0, 0);
            this._root.Controls.Add(this._cardsHost, 0, 1);
            this._root.Controls.Add(this._monitor, 0, 2);
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Location = new System.Drawing.Point(0, 0);
            this._root.Name = "_root";
            this._root.RowCount = 3;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Size = new System.Drawing.Size(1379, 865);
            this._root.TabIndex = 0;
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Controls.Add(this._btnRun);
            this._hdr.Controls.Add(this._btnReady);
            // Dock=Right 적층: 자식 인덱스가 높을수록 우측 끝. RUN 이 우측 끝, READY 는 그 왼쪽이 되도록 순서 보정.
            this._hdr.Controls.SetChildIndex(this._btnReady, 0);
            this._hdr.Controls.SetChildIndex(this._btnRun, 1);
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(0, 0);
            this._hdr.Margin = new System.Windows.Forms.Padding(0);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(1379, 30);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "작업 — 모니터링";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _btnRun
            // 
            this._btnRun.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(157)))), ((int)(((byte)(77)))));
            this._btnRun.Dock = System.Windows.Forms.DockStyle.Right;
            this._btnRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRun.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnRun.ForeColor = System.Drawing.Color.White;
            this._btnRun.Location = new System.Drawing.Point(1283, 0);
            this._btnRun.Margin = new System.Windows.Forms.Padding(0);
            this._btnRun.Name = "_btnRun";
            this._btnRun.Size = new System.Drawing.Size(96, 30);
            this._btnRun.TabIndex = 0;
            this._btnRun.Text = "RUN";
            this._btnRun.UseVisualStyleBackColor = false;
            this._btnRun.Click += new System.EventHandler(this.OnRunToggleClick);
            //
            // _btnReady
            //
            this._btnReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._btnReady.Dock = System.Windows.Forms.DockStyle.Right;
            this._btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReady.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnReady.ForeColor = System.Drawing.Color.White;
            this._btnReady.Location = new System.Drawing.Point(1187, 0);
            this._btnReady.Margin = new System.Windows.Forms.Padding(0);
            this._btnReady.Name = "_btnReady";
            this._btnReady.Size = new System.Drawing.Size(96, 30);
            this._btnReady.TabIndex = 1;
            this._btnReady.Text = "READY";
            this._btnReady.UseVisualStyleBackColor = false;
            this._btnReady.Click += new System.EventHandler(this.OnReadyToggleClick);
            // 
            // _cardsHost
            // 
            this._cardsHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._cardsHost.ColumnCount = 5;
            this._cardsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this._cardsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this._cardsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this._cardsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this._cardsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this._cardsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cardsHost.Location = new System.Drawing.Point(0, 30);
            this._cardsHost.Margin = new System.Windows.Forms.Padding(0);
            this._cardsHost.Name = "_cardsHost";
            this._cardsHost.Padding = new System.Windows.Forms.Padding(2);
            this._cardsHost.RowCount = 1;
            this._cardsHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._cardsHost.Size = new System.Drawing.Size(1379, 92);
            this._cardsHost.TabIndex = 1;
            // 
            // _monitor
            // 
            this._monitor.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._monitor.ColumnCount = 2;
            this._monitor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this._monitor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this._monitor.Controls.Add(this._camBig, 0, 0);
            this._monitor.Controls.Add(this._smallHost, 1, 0);
            this._monitor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._monitor.Location = new System.Drawing.Point(0, 122);
            this._monitor.Margin = new System.Windows.Forms.Padding(0);
            this._monitor.Name = "_monitor";
            this._monitor.Padding = new System.Windows.Forms.Padding(4);
            this._monitor.RowCount = 1;
            this._monitor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._monitor.Size = new System.Drawing.Size(1379, 743);
            this._monitor.TabIndex = 2;
            // 
            // _camBig
            // 
            this._camBig.BackColor = System.Drawing.Color.Black;
            this._camBig.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camBig.InfoText = "BOTTOM INSPECTION (MAIN)\r\nGrab 대기";
            this._camBig.Location = new System.Drawing.Point(7, 7);
            this._camBig.MmPerPixelX = 0D;
            this._camBig.MmPerPixelY = 0D;
            this._camBig.Name = "_camBig";
            this._camBig.ShowCrosshair = true;
            this._camBig.ShowLiveLabel = true;
            this._camBig.ShowToolbar = false;
            this._camBig.Size = new System.Drawing.Size(789, 729);
            this._camBig.TabIndex = 0;
            // 
            // _smallHost
            // 
            this._smallHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._smallHost.ColumnCount = 2;
            this._smallHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._smallHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._smallHost.Controls.Add(this._camS1, 0, 0);
            this._smallHost.Controls.Add(this._camS2, 1, 0);
            this._smallHost.Controls.Add(this._sideHost, 0, 1);
            this._smallHost.SetColumnSpan(this._sideHost, 2);   // TopSide/BottomSide 스택을 우측 영역 전체폭으로
            this._smallHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._smallHost.Location = new System.Drawing.Point(799, 4);
            this._smallHost.Margin = new System.Windows.Forms.Padding(0);
            this._smallHost.Name = "_smallHost";
            this._smallHost.RowCount = 2;
            this._smallHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this._smallHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this._smallHost.Size = new System.Drawing.Size(576, 735);
            this._smallHost.TabIndex = 1;
            //
            // _sideHost — TopSide/BottomSide 전체폭 가로 띠(위아래 스택, 4000×700 대응)
            //
            this._sideHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._sideHost.ColumnCount = 1;
            this._sideHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._sideHost.Controls.Add(this._camS3, 0, 0);
            this._sideHost.Controls.Add(this._camS4, 0, 1);
            this._sideHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._sideHost.Margin = new System.Windows.Forms.Padding(0);
            this._sideHost.Name = "_sideHost";
            this._sideHost.RowCount = 2;
            this._sideHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._sideHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._sideHost.TabIndex = 2;
            // 
            // _camS1
            // 
            this._camS1.BackColor = System.Drawing.Color.Black;
            this._camS1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camS1.InfoText = "WAFER VISION\r\nGrab 대기";
            this._camS1.Location = new System.Drawing.Point(3, 3);
            this._camS1.MmPerPixelX = 0D;
            this._camS1.MmPerPixelY = 0D;
            this._camS1.Name = "_camS1";
            this._camS1.ShowCrosshair = false;
            this._camS1.ShowLiveLabel = false;
            this._camS1.ShowToolbar = false;
            this._camS1.Size = new System.Drawing.Size(282, 361);
            this._camS1.TabIndex = 0;
            // 
            // _camS2
            // 
            this._camS2.BackColor = System.Drawing.Color.Black;
            this._camS2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camS2.InfoText = "BIN VISION\r\nGrab 대기";
            this._camS2.Location = new System.Drawing.Point(291, 3);
            this._camS2.MmPerPixelX = 0D;
            this._camS2.MmPerPixelY = 0D;
            this._camS2.Name = "_camS2";
            this._camS2.ShowCrosshair = false;
            this._camS2.ShowLiveLabel = false;
            this._camS2.ShowToolbar = false;
            this._camS2.Size = new System.Drawing.Size(282, 361);
            this._camS2.TabIndex = 1;
            // 
            // _camS3
            // 
            this._camS3.BackColor = System.Drawing.Color.Black;
            this._camS3.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camS3.InfoText = "TOP SIDE\r\nGrab 대기";
            this._camS3.Location = new System.Drawing.Point(3, 370);
            this._camS3.MmPerPixelX = 0D;
            this._camS3.MmPerPixelY = 0D;
            this._camS3.Name = "_camS3";
            this._camS3.ShowCrosshair = false;
            this._camS3.ShowLiveLabel = false;
            this._camS3.ShowToolbar = false;
            this._camS3.Size = new System.Drawing.Size(282, 362);
            this._camS3.TabIndex = 2;
            // 
            // _camS4
            // 
            this._camS4.BackColor = System.Drawing.Color.Black;
            this._camS4.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camS4.InfoText = "BOTTOM SIDE\r\nGrab 대기";
            this._camS4.Location = new System.Drawing.Point(291, 370);
            this._camS4.MmPerPixelX = 0D;
            this._camS4.MmPerPixelY = 0D;
            this._camS4.Name = "_camS4";
            this._camS4.ShowCrosshair = false;
            this._camS4.ShowLiveLabel = false;
            this._camS4.ShowToolbar = false;
            this._camS4.Size = new System.Drawing.Size(282, 362);
            this._camS4.TabIndex = 3;
            // 
            // OperationPage
            // 
            this.Controls.Add(this._root);
            this.Name = "OperationPage";
            this.Size = new System.Drawing.Size(1379, 865);
            this._root.ResumeLayout(false);
            this._hdr.ResumeLayout(false);
            this._monitor.ResumeLayout(false);
            this._smallHost.ResumeLayout(false);
            this._sideHost.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
