using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QMC.Vision.Ui.Controls
{
    partial class SpcTrendChart
    {
        private System.ComponentModel.IContainer components = null;

        private Chart _chart;
        private Button _btnZoom;
        private Button _btnClear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._chart = new Chart();
            this._btnZoom = new Button();
            this._btnClear = new Button();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).BeginInit();
            this.SuspendLayout();

            // _chart
            this._chart.Dock = DockStyle.Fill;
            this._chart.MouseWheel += new MouseEventHandler(this.Chart_MouseWheel);
            this._chart.MouseEnter += new System.EventHandler(this._chart_MouseEnter);
            this._chart.DoubleClick += new System.EventHandler(this._chart_DoubleClick);

            // _btnZoom (확대 → 새 큰 창)
            this._btnZoom.Text = "⤢";
            this._btnZoom.Size = new Size(26, 22);
            this._btnZoom.FlatStyle = FlatStyle.Flat;
            this._btnZoom.BackColor = Color.FromArgb(0x3A, 0x40, 0x4C);
            this._btnZoom.ForeColor = Color.White;
            this._btnZoom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._btnZoom.Cursor = Cursors.Hand;
            this._btnZoom.Click += new System.EventHandler(this._btnZoom_Click);

            // _btnClear (클릭 커서선 지우기)
            this._btnClear.Text = "✕";
            this._btnClear.Size = new Size(26, 22);
            this._btnClear.FlatStyle = FlatStyle.Flat;
            this._btnClear.BackColor = Color.FromArgb(0x3A, 0x40, 0x4C);
            this._btnClear.ForeColor = Color.White;
            this._btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._btnClear.Cursor = Cursors.Hand;
            this._btnClear.Click += new System.EventHandler(this._btnClear_Click);

            // SpcTrendChart
            this.BackColor = Color.White;
            this.Controls.Add(this._btnClear);
            this.Controls.Add(this._btnZoom);
            this.Controls.Add(this._chart);
            this.Name = "SpcTrendChart";
            this.Resize += new System.EventHandler(this.SpcTrendChart_Resize);
            ((System.ComponentModel.ISupportInitialize)(this._chart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
