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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this._hdr = new System.Windows.Forms.Label();
            this._top = new System.Windows.Forms.Panel();
            this._lblItem = new System.Windows.Forms.Label();
            this._cbItem = new System.Windows.Forms.ComboBox();
            this._lblDate = new System.Windows.Forms.Label();
            this._dpDate = new System.Windows.Forms.DateTimePicker();
            this._lblLsl = new System.Windows.Forms.Label();
            this._nLsl = new System.Windows.Forms.NumericUpDown();
            this._lblUsl = new System.Windows.Forms.Label();
            this._nUsl = new System.Windows.Forms.NumericUpDown();
            this._btnReload = new System.Windows.Forms.Button();
            this._lblStats = new System.Windows.Forms.Label();
            this._chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this._top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nLsl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nUsl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).BeginInit();
            this.SuspendLayout();
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(0, 80);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(1084, 30);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "SPC X-bar Chart";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _top
            // 
            this._top.BackColor = System.Drawing.Color.WhiteSmoke;
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
            this._top.Dock = System.Windows.Forms.DockStyle.Top;
            this._top.Location = new System.Drawing.Point(0, 0);
            this._top.Name = "_top";
            this._top.Size = new System.Drawing.Size(1084, 80);
            this._top.TabIndex = 1;
            // 
            // _lblItem
            // 
            this._lblItem.AutoSize = true;
            this._lblItem.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblItem.Location = new System.Drawing.Point(8, 8);
            this._lblItem.Name = "_lblItem";
            this._lblItem.Size = new System.Drawing.Size(42, 20);
            this._lblItem.TabIndex = 0;
            this._lblItem.Text = "Item:";
            // 
            // _cbItem
            // 
            this._cbItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbItem.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbItem.Location = new System.Drawing.Point(60, 4);
            this._cbItem.Name = "_cbItem";
            this._cbItem.Size = new System.Drawing.Size(280, 28);
            this._cbItem.TabIndex = 1;
            this._cbItem.SelectedIndexChanged += new System.EventHandler(this.OnItemChanged);
            // 
            // _lblDate
            // 
            this._lblDate.AutoSize = true;
            this._lblDate.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblDate.Location = new System.Drawing.Point(360, 8);
            this._lblDate.Name = "_lblDate";
            this._lblDate.Size = new System.Drawing.Size(44, 20);
            this._lblDate.TabIndex = 2;
            this._lblDate.Text = "Date:";
            // 
            // _dpDate
            // 
            this._dpDate.CustomFormat = "yyyy-MM-dd";
            this._dpDate.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._dpDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this._dpDate.Location = new System.Drawing.Point(410, 4);
            this._dpDate.Name = "_dpDate";
            this._dpDate.Size = new System.Drawing.Size(160, 27);
            this._dpDate.TabIndex = 3;
            this._dpDate.ValueChanged += new System.EventHandler(this.OnDateChanged);
            // 
            // _lblLsl
            // 
            this._lblLsl.AutoSize = true;
            this._lblLsl.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblLsl.Location = new System.Drawing.Point(8, 44);
            this._lblLsl.Name = "_lblLsl";
            this._lblLsl.Size = new System.Drawing.Size(34, 20);
            this._lblLsl.TabIndex = 4;
            this._lblLsl.Text = "LSL:";
            // 
            // _nLsl
            // 
            this._nLsl.DecimalPlaces = 4;
            this._nLsl.Font = new System.Drawing.Font("Consolas", 10F);
            this._nLsl.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nLsl.Location = new System.Drawing.Point(60, 40);
            this._nLsl.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nLsl.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this._nLsl.Name = "_nLsl";
            this._nLsl.Size = new System.Drawing.Size(120, 23);
            this._nLsl.TabIndex = 5;
            this._nLsl.Value = new decimal(new int[] {
            5,
            0,
            0,
            -2147352576});
            this._nLsl.ValueChanged += new System.EventHandler(this.OnLslChanged);
            // 
            // _lblUsl
            // 
            this._lblUsl.AutoSize = true;
            this._lblUsl.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblUsl.Location = new System.Drawing.Point(200, 44);
            this._lblUsl.Name = "_lblUsl";
            this._lblUsl.Size = new System.Drawing.Size(38, 20);
            this._lblUsl.TabIndex = 6;
            this._lblUsl.Text = "USL:";
            // 
            // _nUsl
            // 
            this._nUsl.DecimalPlaces = 4;
            this._nUsl.Font = new System.Drawing.Font("Consolas", 10F);
            this._nUsl.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nUsl.Location = new System.Drawing.Point(240, 40);
            this._nUsl.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nUsl.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this._nUsl.Name = "_nUsl";
            this._nUsl.Size = new System.Drawing.Size(120, 23);
            this._nUsl.TabIndex = 7;
            this._nUsl.Value = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this._nUsl.ValueChanged += new System.EventHandler(this.OnUslChanged);
            // 
            // _btnReload
            // 
            this._btnReload.BackColor = System.Drawing.Color.White;
            this._btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReload.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnReload.Location = new System.Drawing.Point(580, 4);
            this._btnReload.Name = "_btnReload";
            this._btnReload.Size = new System.Drawing.Size(120, 28);
            this._btnReload.TabIndex = 8;
            this._btnReload.Text = "Reload CSV";
            this._btnReload.UseVisualStyleBackColor = false;
            this._btnReload.Click += new System.EventHandler(this.OnReloadClick);
            // 
            // _lblStats
            // 
            this._lblStats.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblStats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblStats.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStats.Location = new System.Drawing.Point(380, 40);
            this._lblStats.Name = "_lblStats";
            this._lblStats.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblStats.Size = new System.Drawing.Size(540, 28);
            this._lblStats.TabIndex = 9;
            this._lblStats.Text = "(no data)";
            this._lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _chart
            // 
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisX.Title = "Sample";
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisY.Title = "Value";
            chartArea1.BackColor = System.Drawing.Color.White;
            chartArea1.Name = "main";
            this._chart.ChartAreas.Add(chartArea1);
            this._chart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top;
            legend1.Name = "legend";
            this._chart.Legends.Add(legend1);
            this._chart.Location = new System.Drawing.Point(0, 0);
            this._chart.Name = "_chart";
            this._chart.Size = new System.Drawing.Size(1084, 696);
            this._chart.TabIndex = 2;
            // 
            // SpcChartPage
            // 
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._top);
            this.Controls.Add(this._chart);
            this.Name = "SpcChartPage";
            this.Size = new System.Drawing.Size(1084, 696);
            this._top.ResumeLayout(false);
            this._top.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nLsl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nUsl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
