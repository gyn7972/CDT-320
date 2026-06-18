using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QMC.Vision.Ui.Dialogs
{
    partial class LoadChartDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Chart  _chart;
        private Label  _summary;
        private Panel  _btns;
        private Button _btnSaveCsv, _btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._chart = new Chart();
            this._summary = new Label();
            this._btns = new Panel();
            this._btnSaveCsv = new Button();
            this._btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)(this._chart)).BeginInit();
            this._btns.SuspendLayout();
            this.SuspendLayout();
            //
            // _chart
            //
            this._chart.Dock = DockStyle.Fill;
            this._chart.Name = "_chart";
            this._chart.BackColor = Color.White;
            //
            // _summary
            //
            this._summary.Dock = DockStyle.Bottom;
            this._summary.Height = 46;
            this._summary.Name = "_summary";
            this._summary.Font = new Font("맑은 고딕", 10F);
            this._summary.Padding = new Padding(10, 4, 10, 4);
            this._summary.TextAlign = ContentAlignment.MiddleLeft;
            this._summary.BackColor = Color.FromArgb(245, 245, 245);
            //
            // _btns
            //
            this._btns.Dock = DockStyle.Bottom;
            this._btns.Height = 44;
            this._btns.Name = "_btns";
            this._btns.Padding = new Padding(8, 6, 8, 6);
            this._btns.Controls.Add(this._btnClose);
            this._btns.Controls.Add(this._btnSaveCsv);
            //
            // _btnSaveCsv
            //
            this._btnSaveCsv.Dock = DockStyle.Right;
            this._btnSaveCsv.Width = 130;
            this._btnSaveCsv.FlatStyle = FlatStyle.Flat;
            this._btnSaveCsv.BackColor = Color.White;
            this._btnSaveCsv.Font = new Font("맑은 고딕", 10F);
            this._btnSaveCsv.Name = "_btnSaveCsv";
            this._btnSaveCsv.Text = "CSV 저장";
            this._btnSaveCsv.UseVisualStyleBackColor = false;
            this._btnSaveCsv.Click += new System.EventHandler(this.OnSaveCsvClick);
            //
            // _btnClose
            //
            this._btnClose.Dock = DockStyle.Right;
            this._btnClose.Width = 110;
            this._btnClose.FlatStyle = FlatStyle.Flat;
            this._btnClose.BackColor = Color.FromArgb(232, 93, 26);
            this._btnClose.ForeColor = Color.White;
            this._btnClose.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Text = "닫기";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.DialogResult = DialogResult.OK;
            //
            // LoadChartDialog
            //
            this.ClientSize = new Size(920, 580);
            this.MinimumSize = new Size(520, 360);
            this.Controls.Add(this._chart);
            this.Controls.Add(this._summary);
            this.Controls.Add(this._btns);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true; this.MinimizeBox = false; this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "LoadChartDialog";
            this.Text = "CPU / 메모리 부하 차트";
            this.CancelButton = this._btnClose;
            ((System.ComponentModel.ISupportInitialize)(this._chart)).EndInit();
            this._btns.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
