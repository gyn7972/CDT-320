using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Dialogs
{
    partial class ScaleCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private CameraView       _pic;
        private Label            _info;
        private TableLayoutPanel _grid;
        private Label   _lblW;  private TextBox _txtW;  private Button _btnMeasW;  private Label _lblWpx;
        private Label   _lblH;  private TextBox _txtH;  private Button _btnMeasH;  private Label _lblHpx;
        private TableLayoutPanel _btnRow;
        private Button  _btnFit, _btnOk, _btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root    = new TableLayoutPanel();
            this._pic     = new CameraView();
            this._info    = new Label();
            this._grid    = new TableLayoutPanel();
            this._lblW    = new Label();  this._txtW = new TextBox();  this._btnMeasW = new Button();  this._lblWpx = new Label();
            this._lblH    = new Label();  this._txtH = new TextBox();  this._btnMeasH = new Button();  this._lblHpx = new Label();
            this._btnRow  = new TableLayoutPanel();
            this._btnFit  = new Button();
            this._btnOk   = new Button();
            this._btnCancel = new Button();
            this._root.SuspendLayout();
            this._grid.SuspendLayout();
            this._btnRow.SuspendLayout();
            this.SuspendLayout();
            //
            // _root
            //
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._root.Controls.Add(this._pic,    0, 0);
            this._root.Controls.Add(this._info,   0, 1);
            this._root.Controls.Add(this._grid,   0, 2);
            this._root.Controls.Add(this._btnRow, 0, 3);
            this._root.Dock = DockStyle.Fill;
            this._root.Name = "_root";
            this._root.Padding = new Padding(8);
            this._root.RowCount = 4;
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            //
            // _pic
            //
            this._pic.BackColor = System.Drawing.Color.Black;
            this._pic.Dock = DockStyle.Fill;
            this._pic.Margin = new Padding(0, 0, 0, 6);
            this._pic.Name = "_pic";
            this._pic.ShowCrosshair = false;
            this._pic.ShowLiveLabel = false;
            this._pic.InfoText = "";
            //
            // _info
            //
            this._info.Dock = DockStyle.Fill;
            this._info.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._info.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._info.Name = "_info";
            this._info.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._info.Text = "칩 가로/세로 실제 mm를 입력하고, [가로 측정]·[세로 측정]으로 양 끝 두 점을 클릭하세요.";
            //
            // _grid
            //
            this._grid.ColumnCount = 4;
            this._grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            this._grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            this._grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            this._grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._grid.Controls.Add(this._lblW,    0, 0);
            this._grid.Controls.Add(this._txtW,    1, 0);
            this._grid.Controls.Add(this._btnMeasW,2, 0);
            this._grid.Controls.Add(this._lblWpx,  3, 0);
            this._grid.Controls.Add(this._lblH,    0, 1);
            this._grid.Controls.Add(this._txtH,    1, 1);
            this._grid.Controls.Add(this._btnMeasH,2, 1);
            this._grid.Controls.Add(this._lblHpx,  3, 1);
            this._grid.Dock = DockStyle.Fill;
            this._grid.Name = "_grid";
            this._grid.RowCount = 2;
            this._grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this._grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            //
            // _lblW / _txtW / _btnMeasW / _lblWpx
            //
            this._lblW.Dock = DockStyle.Fill; this._lblW.Name = "_lblW"; this._lblW.Text = "칩 가로 (mm)";
            this._lblW.TextAlign = System.Drawing.ContentAlignment.MiddleLeft; this._lblW.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtW.Dock = DockStyle.Fill; this._txtW.Name = "_txtW"; this._txtW.Margin = new Padding(2, 6, 2, 6);
            this._txtW.Font = new System.Drawing.Font("Consolas", 10F);
            this._btnMeasW.Dock = DockStyle.Fill; this._btnMeasW.Name = "_btnMeasW"; this._btnMeasW.Text = "가로 측정";
            this._btnMeasW.FlatStyle = FlatStyle.Flat; this._btnMeasW.BackColor = System.Drawing.Color.White; this._btnMeasW.Margin = new Padding(2);
            this._btnMeasW.Click += new System.EventHandler(this.OnMeasureWidthClick);
            this._lblWpx.Dock = DockStyle.Fill; this._lblWpx.Name = "_lblWpx"; this._lblWpx.Text = "- px";
            this._lblWpx.TextAlign = System.Drawing.ContentAlignment.MiddleLeft; this._lblWpx.Font = new System.Drawing.Font("Consolas", 10F);
            //
            // _lblH / _txtH / _btnMeasH / _lblHpx
            //
            this._lblH.Dock = DockStyle.Fill; this._lblH.Name = "_lblH"; this._lblH.Text = "칩 세로 (mm)";
            this._lblH.TextAlign = System.Drawing.ContentAlignment.MiddleLeft; this._lblH.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._txtH.Dock = DockStyle.Fill; this._txtH.Name = "_txtH"; this._txtH.Margin = new Padding(2, 6, 2, 6);
            this._txtH.Font = new System.Drawing.Font("Consolas", 10F);
            this._btnMeasH.Dock = DockStyle.Fill; this._btnMeasH.Name = "_btnMeasH"; this._btnMeasH.Text = "세로 측정";
            this._btnMeasH.FlatStyle = FlatStyle.Flat; this._btnMeasH.BackColor = System.Drawing.Color.White; this._btnMeasH.Margin = new Padding(2);
            this._btnMeasH.Click += new System.EventHandler(this.OnMeasureHeightClick);
            this._lblHpx.Dock = DockStyle.Fill; this._lblHpx.Name = "_lblHpx"; this._lblHpx.Text = "- px";
            this._lblHpx.TextAlign = System.Drawing.ContentAlignment.MiddleLeft; this._lblHpx.Font = new System.Drawing.Font("Consolas", 10F);
            //
            // _btnRow
            //
            this._btnRow.ColumnCount = 3;
            this._btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            this._btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this._btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            this._btnRow.Controls.Add(this._btnFit,    0, 0);
            this._btnRow.Controls.Add(this._btnOk,     1, 0);
            this._btnRow.Controls.Add(this._btnCancel, 2, 0);
            this._btnRow.Dock = DockStyle.Fill;
            this._btnRow.Name = "_btnRow";
            this._btnRow.RowCount = 1;
            this._btnRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            //
            // _btnFit
            //
            this._btnFit.Dock = DockStyle.Fill;
            this._btnFit.FlatStyle = FlatStyle.Flat;
            this._btnFit.BackColor = System.Drawing.Color.White;
            this._btnFit.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnFit.Margin = new Padding(2);
            this._btnFit.Name = "_btnFit";
            this._btnFit.Text = "맞춤(Fit)";
            this._btnFit.UseVisualStyleBackColor = false;
            this._btnFit.Click += new System.EventHandler(this.OnFitClick);
            //
            // _btnOk
            //
            this._btnOk.Dock = DockStyle.Fill;
            this._btnOk.FlatStyle = FlatStyle.Flat;
            this._btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnOk.ForeColor = System.Drawing.Color.White;
            this._btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnOk.Margin = new Padding(2);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Text = "계산 & 적용";
            this._btnOk.UseVisualStyleBackColor = false;
            this._btnOk.Click += new System.EventHandler(this.OnOkClick);
            //
            // _btnCancel
            //
            this._btnCancel.Dock = DockStyle.Fill;
            this._btnCancel.FlatStyle = FlatStyle.Flat;
            this._btnCancel.BackColor = System.Drawing.Color.White;
            this._btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnCancel.Margin = new Padding(2);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Text = "취소";
            this._btnCancel.UseVisualStyleBackColor = false;
            this._btnCancel.DialogResult = DialogResult.Cancel;
            //
            // ScaleCalibrationDialog
            //
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1040, 860);
            this.MinimumSize = new System.Drawing.Size(640, 560);
            this.Controls.Add(this._root);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true; this.MinimizeBox = false; this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "ScaleCalibrationDialog";
            this.Text = "스케일 캘리브레이션 (측정 기반)";
            this.CancelButton = this._btnCancel;
            this._root.ResumeLayout(false);
            this._grid.ResumeLayout(false);
            this._grid.PerformLayout();
            this._btnRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
