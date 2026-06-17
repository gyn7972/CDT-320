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
        private TableLayoutPanel _cardsHost;   // 5열 — 모듈 상태카드(런타임 채움, 비클릭)

        // 모니터링: 메인(Bottom) 1개 크게 + 나머지 4개 2×2
        private TableLayoutPanel _monitor;
        private CameraView       _camBig;       // 메인(Bottom Inspection)
        private TableLayoutPanel _smallHost;    // 2×2
        private CameraView       _camS1;        // Wafer
        private CameraView       _camS2;        // Bin
        private CameraView       _camS3;        // TopSide
        private CameraView       _camS4;        // BottomSide

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
            this._root      = new TableLayoutPanel();
            this._hdr       = new Label();
            this._cardsHost = new TableLayoutPanel();
            this._monitor   = new TableLayoutPanel();
            this._camBig    = new CameraView();
            this._smallHost = new TableLayoutPanel();
            this._camS1     = new CameraView();
            this._camS2     = new CameraView();
            this._camS3     = new CameraView();
            this._camS4     = new CameraView();

            this._root.SuspendLayout();
            this._monitor.SuspendLayout();
            this._smallHost.SuspendLayout();
            this.SuspendLayout();

            // _root — 3행(30 헤더 / 92 카드 / 나머지 모니터링)
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 3;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._root.BackColor = UiTheme.MainBg;
            this._root.Controls.Add(this._hdr,       0, 0);
            this._root.Controls.Add(this._cardsHost, 0, 1);
            this._root.Controls.Add(this._monitor,   0, 2);

            // _hdr — 주황 섹션 헤더
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Text = "작업 — 모니터링";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = UiTheme.StatusBarFg;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);
            this._hdr.Margin = new Padding(0);

            // _cardsHost — 5열 균등(카드는 런타임 생성, 비클릭)
            this._cardsHost.Dock = DockStyle.Fill;
            this._cardsHost.BackColor = UiTheme.MainBg;
            this._cardsHost.ColumnCount = 5;
            this._cardsHost.RowCount = 1;
            this._cardsHost.Margin = new Padding(0);
            this._cardsHost.Padding = new Padding(2);
            for (int i = 0; i < 5; i++)
                this._cardsHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            this._cardsHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // _monitor — 2열(메인 58% / 4분할 42%)
            this._monitor.Dock = DockStyle.Fill;
            this._monitor.BackColor = UiTheme.MainBg;
            this._monitor.ColumnCount = 2;
            this._monitor.RowCount = 1;
            this._monitor.Margin = new Padding(0);
            this._monitor.Padding = new Padding(4);
            this._monitor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            this._monitor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this._monitor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._monitor.Controls.Add(this._camBig,    0, 0);
            this._monitor.Controls.Add(this._smallHost, 1, 0);

            // _camBig — 메인(Bottom Inspection)
            this._camBig.Dock = DockStyle.Fill;
            this._camBig.Margin = new Padding(3);
            this._camBig.ShowCrosshair = true;
            this._camBig.ShowLiveLabel = true;
            this._camBig.InfoText = "BOTTOM INSPECTION (MAIN)\r\nGrab 대기";

            // _smallHost — 2×2
            this._smallHost.Dock = DockStyle.Fill;
            this._smallHost.BackColor = UiTheme.MainBg;
            this._smallHost.ColumnCount = 2;
            this._smallHost.RowCount = 2;
            this._smallHost.Margin = new Padding(0);
            this._smallHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._smallHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._smallHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this._smallHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this._smallHost.Controls.Add(this._camS1, 0, 0);
            this._smallHost.Controls.Add(this._camS2, 1, 0);
            this._smallHost.Controls.Add(this._camS3, 0, 1);
            this._smallHost.Controls.Add(this._camS4, 1, 1);

            ConfigureSmallCam(this._camS1, "WAFER VISION");
            ConfigureSmallCam(this._camS2, "BIN VISION");
            ConfigureSmallCam(this._camS3, "TOP SIDE");
            ConfigureSmallCam(this._camS4, "BOTTOM SIDE");

            // OperationPage
            this.Controls.Add(this._root);
            this.Name = "OperationPage";

            this._smallHost.ResumeLayout(false);
            this._monitor.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static void ConfigureSmallCam(CameraView cam, string title)
        {
            cam.Dock = DockStyle.Fill;
            cam.Margin = new Padding(3);
            cam.ShowCrosshair = false;
            cam.ShowLiveLabel = false;
            cam.InfoText = title + "\r\nGrab 대기";
        }
    }
}
