using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QMC.Vision.Ui.Pages
{
    partial class SpcChartPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label          _hdr;
        private Panel          _top;
        private Label          _lblItem;
        private ComboBox       _cbItem;
        private Label          _lblDate;
        private DateTimePicker _dpDate;
        private Label          _lblLsl;
        private NumericUpDown  _nLsl;
        private Label          _lblUsl;
        private NumericUpDown  _nUsl;
        private Button         _btnReload;
        private Label          _lblStats;
        private Chart          _chart;
        private ChartArea      _ca;
        private Legend         _legend;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._top = new Panel();
            this._lblItem = new Label();
            this._cbItem = new ComboBox();
            this._lblDate = new Label();
            this._dpDate = new DateTimePicker();
            this._lblLsl = new Label();
            this._nLsl = new NumericUpDown();
            this._lblUsl = new Label();
            this._nUsl = new NumericUpDown();
            this._btnReload = new Button();
            this._lblStats = new Label();
            this._chart = new Chart();
            this._ca = new ChartArea("main");
            this._legend = new Legend("legend");
            this._top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nLsl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nUsl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).BeginInit();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "SPC X-bar Chart";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _top
            this._top.Dock = DockStyle.Top;
            this._top.Height = 80;
            this._top.BackColor = Color.WhiteSmoke;

            // _lblItem
            this._lblItem.Location = new Point(8, 8);
            this._lblItem.AutoSize = true;
            this._lblItem.Text = "Item:";
            this._lblItem.Font = UiTheme.ButtonFont;

            // _cbItem (Items/SelectedIndex 런타임)
            this._cbItem.Location = new Point(60, 4);
            this._cbItem.Size = new Size(280, 28);
            this._cbItem.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbItem.Font = UiTheme.ButtonFont;
            this._cbItem.SelectedIndexChanged += new System.EventHandler(this.OnItemChanged);

            // _lblDate
            this._lblDate.Location = new Point(360, 8);
            this._lblDate.AutoSize = true;
            this._lblDate.Text = "Date:";
            this._lblDate.Font = UiTheme.ButtonFont;

            // _dpDate (Value=Today 런타임)
            this._dpDate.Location = new Point(410, 4);
            this._dpDate.Size = new Size(160, 28);
            this._dpDate.Format = DateTimePickerFormat.Custom;
            this._dpDate.CustomFormat = "yyyy-MM-dd";
            this._dpDate.Font = UiTheme.ButtonFont;
            this._dpDate.ValueChanged += new System.EventHandler(this.OnDateChanged);

            // _lblLsl
            this._lblLsl.Location = new Point(8, 44);
            this._lblLsl.AutoSize = true;
            this._lblLsl.Text = "LSL:";
            this._lblLsl.Font = UiTheme.ButtonFont;

            // _nLsl (Value 리터럴 → 이벤트 연결 전에 설정해 초기 트리거 방지)
            this._nLsl.Location = new Point(60, 40);
            this._nLsl.Size = new Size(120, 28);
            this._nLsl.Minimum = -1000m;
            this._nLsl.Maximum = 1000m;
            this._nLsl.DecimalPlaces = 4;
            this._nLsl.Increment = 0.001m;
            this._nLsl.Font = UiTheme.ValueFont;
            this._nLsl.Value = -0.05m;
            this._nLsl.ValueChanged += new System.EventHandler(this.OnLslChanged);

            // _lblUsl
            this._lblUsl.Location = new Point(200, 44);
            this._lblUsl.AutoSize = true;
            this._lblUsl.Text = "USL:";
            this._lblUsl.Font = UiTheme.ButtonFont;

            // _nUsl
            this._nUsl.Location = new Point(240, 40);
            this._nUsl.Size = new Size(120, 28);
            this._nUsl.Minimum = -1000m;
            this._nUsl.Maximum = 1000m;
            this._nUsl.DecimalPlaces = 4;
            this._nUsl.Increment = 0.001m;
            this._nUsl.Font = UiTheme.ValueFont;
            this._nUsl.Value = 0.05m;
            this._nUsl.ValueChanged += new System.EventHandler(this.OnUslChanged);

            // _btnReload
            this._btnReload.Location = new Point(580, 4);
            this._btnReload.Size = new Size(120, 28);
            this._btnReload.Text = "Reload CSV";
            this._btnReload.FlatStyle = FlatStyle.Flat;
            this._btnReload.Font = UiTheme.ButtonFont;
            this._btnReload.BackColor = Color.White;
            this._btnReload.Click += new System.EventHandler(this.OnReloadClick);

            // _lblStats
            this._lblStats.Location = new Point(380, 40);
            this._lblStats.Size = new Size(540, 28);
            this._lblStats.Text = "(no data)";
            this._lblStats.Font = UiTheme.ValueFont;
            this._lblStats.BackColor = Color.WhiteSmoke;
            this._lblStats.BorderStyle = BorderStyle.FixedSingle;
            this._lblStats.TextAlign = ContentAlignment.MiddleLeft;
            this._lblStats.Padding = new Padding(8, 0, 0, 0);

            this._top.Controls.Add(this._lblItem);
            this._top.Controls.Add(this._cbItem);
            this._top.Controls.Add(this._lblDate);
            this._top.Controls.Add(this._dpDate);
            this._top.Controls.Add(this._lblLsl);
            this._top.Controls.Add(this._nLsl);
            this._top.Controls.Add(this._lblUsl);
            this._top.Controls.Add(this._nUsl);
            this._top.Controls.Add(this._btnReload);
            this._top.Controls.Add(this._lblStats);

            // _ca (ChartArea)
            this._ca.BackColor = Color.White;
            this._ca.AxisX.Title = "Sample";
            this._ca.AxisX.MajorGrid.LineColor = Color.LightGray;
            this._ca.AxisY.Title = "Value";
            this._ca.AxisY.MajorGrid.LineColor = Color.LightGray;

            // _legend
            this._legend.Docking = Docking.Top;

            // _chart (시리즈는 런타임 Plot 에서 생성)
            this._chart.Dock = DockStyle.Fill;
            this._chart.ChartAreas.Add(this._ca);
            this._chart.Legends.Add(this._legend);

            // SpcChartPage (원본 추가순서: 헤더→top→차트)
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._top);
            this.Controls.Add(this._chart);
            this.Name = "SpcChartPage";
            this._top.ResumeLayout(false);
            this._top.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nLsl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nUsl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
