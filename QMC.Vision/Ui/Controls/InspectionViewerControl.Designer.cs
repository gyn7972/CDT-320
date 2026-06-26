using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class InspectionViewerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // ── 상단 헤더(모드 토글 + 크로스라인 토글) ──
        private Panel _header;
        private Label _lblToggle;
        private CheckBox _chkCross;
        private Button _btnTest;

        // ── 본문 ──
        private TableLayoutPanel _root;
        private TableLayoutPanel _body;

        // 좌측: Picker 1~4 (단일 이미지 또는 Side 4채널)
        private TableLayoutPanel _pickerHost;
        private PickerView _pk1, _pk2, _pk3, _pk4;

        // 우측: 추세 차트2 + Map + 결과 그리드
        private TableLayoutPanel _rightHost;
        private SpcTrendChart _chart1, _chart2;
        private Panel _mapHost;
        private Label _mapTitle;
        private PositionMapStrip _waferMap;   // 위치별 4-맵(Width·Height·1ch·2ch ChippingSize)
        private DataGridView _grid;

        private static readonly Color HeaderBg = Color.FromArgb(0x2C, 0x30, 0x3A);
        private static readonly Color PanelBg  = Color.FromArgb(0xF4, 0xF4, 0xF4);
        private static readonly Color CellBg   = Color.White;

        private void InitializeComponent()
        {
            _root       = new TableLayoutPanel();
            _header     = new Panel();
            _lblToggle  = new Label();
            _chkCross   = new CheckBox();
            _btnTest    = new Button();
            _body       = new TableLayoutPanel();
            _pickerHost = new TableLayoutPanel();
            _rightHost  = new TableLayoutPanel();
            _pk1 = new PickerView(); _pk2 = new PickerView();
            _pk3 = new PickerView(); _pk4 = new PickerView();
            _chart1 = new SpcTrendChart(); _chart2 = new SpcTrendChart();
            _mapHost = new Panel(); _mapTitle = new Label(); _waferMap = new PositionMapStrip();
            _grid = new DataGridView();

            // _root : 헤더(36) + 본문
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.RowCount = 2;
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _root.Dock = DockStyle.Fill;
            _root.BackColor = PanelBg;
            _root.Controls.Add(_header, 0, 0);
            _root.Controls.Add(_body, 0, 1);

            // _header — 모드 토글 라벨만(레시피/Lot/Wafer/Judge 는 작업화면 상단 공용 헤더에서 표시)
            _header.Dock = DockStyle.Fill;
            _header.Margin = new Padding(0);
            _header.BackColor = HeaderBg;
            _header.Controls.Add(_lblToggle);
            _header.Controls.Add(_chkCross);
            _header.Controls.Add(_btnTest);

            _btnTest.Dock = DockStyle.Right;
            _btnTest.Width = 110;
            _btnTest.FlatStyle = FlatStyle.Flat;
            _btnTest.ForeColor = Color.White;
            _btnTest.BackColor = Color.FromArgb(0x1F, 0x6F, 0xA5);
            _btnTest.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            _btnTest.Text = "픽커 테스트";
            _btnTest.Click += OnTestClick;
            _lblToggle.Dock = DockStyle.Fill;
            _lblToggle.ForeColor = Color.White;
            _lblToggle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            _lblToggle.TextAlign = ContentAlignment.MiddleCenter;
            _lblToggle.Text = "(toggle)";

            _chkCross.Dock = DockStyle.Right;
            _chkCross.Width = 120;
            _chkCross.ForeColor = Color.White;
            _chkCross.Font = new Font("맑은 고딕", 8.5F);
            _chkCross.Text = "크로스라인";
            _chkCross.TextAlign = ContentAlignment.MiddleRight;
            _chkCross.CheckAlign = ContentAlignment.MiddleRight;
            _chkCross.Padding = new Padding(0, 0, 8, 0);
            _chkCross.CheckedChanged += OnCrossChanged;

            // _body : 좌 Picker(60%) / 우(40%)
            _body.Dock = DockStyle.Fill;
            _body.Margin = new Padding(0);
            _body.BackColor = PanelBg;
            _body.ColumnCount = 2;
            _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            _body.RowCount = 1;
            _body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _body.Controls.Add(_pickerHost, 0, 0);
            _body.Controls.Add(_rightHost, 1, 0);

            // _pickerHost : 2x2 검은 캔버스
            _pickerHost.Dock = DockStyle.Fill;
            _pickerHost.Margin = new Padding(4);
            _pickerHost.BackColor = PanelBg;
            _pickerHost.ColumnCount = 2;
            _pickerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _pickerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _pickerHost.RowCount = 2;
            _pickerHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _pickerHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _pk1.Dock = DockStyle.Fill; _pk1.Margin = new Padding(3);
            _pk2.Dock = DockStyle.Fill; _pk2.Margin = new Padding(3);
            _pk3.Dock = DockStyle.Fill; _pk3.Margin = new Padding(3);
            _pk4.Dock = DockStyle.Fill; _pk4.Margin = new Padding(3);
            _pickerHost.Controls.Add(_pk1, 0, 0);
            _pickerHost.Controls.Add(_pk2, 1, 0);
            _pickerHost.Controls.Add(_pk3, 0, 1);
            _pickerHost.Controls.Add(_pk4, 1, 1);

            // _rightHost : 차트1 / 차트2 / Map / 결과그리드
            _rightHost.Dock = DockStyle.Fill;
            _rightHost.Margin = new Padding(4);
            _rightHost.ColumnCount = 1;
            _rightHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _rightHost.RowCount = 4;
            _rightHost.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));
            _rightHost.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));
            _rightHost.RowStyles.Add(new RowStyle(SizeType.Percent, 22F));
            _rightHost.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            _chart1.Dock = DockStyle.Fill; _chart1.Margin = new Padding(3);
            _chart2.Dock = DockStyle.Fill; _chart2.Margin = new Padding(3);
            _rightHost.Controls.Add(_chart1, 0, 0);
            _rightHost.Controls.Add(_chart2, 0, 1);
            _rightHost.Controls.Add(_mapHost, 0, 2);
            _rightHost.Controls.Add(_grid, 0, 3);

            // _mapHost (Bottom 전용 placeholder)
            _mapHost.Dock = DockStyle.Fill;
            _mapHost.Margin = new Padding(3);
            _mapHost.BackColor = CellBg;
            _mapHost.BorderStyle = BorderStyle.FixedSingle;
            _mapHost.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);
            _mapHost.Controls.Add(_waferMap);
            _mapHost.Controls.Add(_mapTitle);
            _waferMap.Dock = DockStyle.Fill;
            _mapTitle.Dock = DockStyle.Top;
            _mapTitle.ForeColor = Color.Gainsboro;
            _mapTitle.Height = 18;
            _mapTitle.Font = new Font("Segoe UI", 8.5F);
            _mapTitle.Padding = new Padding(6, 2, 0, 0);
            _mapTitle.Text = "Map";

            // _grid (결과들)
            _grid.Dock = DockStyle.Fill;
            _grid.Margin = new Padding(3);
            _grid.BackgroundColor = CellBg;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // InspectionViewerControl
            AutoScaleMode = AutoScaleMode.None;
            BackColor = PanelBg;
            Controls.Add(_root);
            Name = "InspectionViewerControl";
            Size = new Size(1100, 760);
        }
    }
}
