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
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._pic = new QMC.Vision.Ui.Controls.CameraView();
            this._info = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.TableLayoutPanel();
            this._lblW = new System.Windows.Forms.Label();
            this._txtW = new System.Windows.Forms.TextBox();
            this._btnMeasW = new System.Windows.Forms.Button();
            this._lblWpx = new System.Windows.Forms.Label();
            this._lblH = new System.Windows.Forms.Label();
            this._txtH = new System.Windows.Forms.TextBox();
            this._btnMeasH = new System.Windows.Forms.Button();
            this._lblHpx = new System.Windows.Forms.Label();
            this._btnRow = new System.Windows.Forms.TableLayoutPanel();
            this._btnFit = new System.Windows.Forms.Button();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._root.SuspendLayout();
            this._grid.SuspendLayout();
            this._btnRow.SuspendLayout();
            this.SuspendLayout();
            // 
            // _root
            // 
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Controls.Add(this._pic, 0, 0);
            this._root.Controls.Add(this._info, 0, 1);
            this._root.Controls.Add(this._grid, 0, 2);
            this._root.Controls.Add(this._btnRow, 0, 3);
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Location = new System.Drawing.Point(0, 0);
            this._root.Name = "_root";
            this._root.Padding = new System.Windows.Forms.Padding(8);
            this._root.RowCount = 4;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this._root.Size = new System.Drawing.Size(1040, 860);
            this._root.TabIndex = 0;
            // 
            // _pic
            // 
            this._pic.BackColor = System.Drawing.Color.Black;
            this._pic.DisplayOrientation = System.Drawing.RotateFlipType.RotateNoneFlipNone;
            this._pic.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pic.InfoForeColor = System.Drawing.Color.LightGreen;
            this._pic.InfoText = "";
            this._pic.Location = new System.Drawing.Point(8, 8);
            this._pic.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._pic.MmPerPixelX = 0D;
            this._pic.MmPerPixelY = 0D;
            this._pic.Name = "_pic";
            this._pic.ShowCrosshair = false;
            this._pic.ShowCursorReadout = false;
            this._pic.ShowLiveLabel = false;
            this._pic.ShowToolbar = false;
            this._pic.Size = new System.Drawing.Size(1024, 680);
            this._pic.TabIndex = 0;
            // 
            // _info
            // 
            this._info.Dock = System.Windows.Forms.DockStyle.Fill;
            this._info.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._info.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._info.Location = new System.Drawing.Point(11, 694);
            this._info.Name = "_info";
            this._info.Size = new System.Drawing.Size(1018, 28);
            this._info.TabIndex = 1;
            this._info.Text = "칩 가로/세로 실제 mm를 입력하고, [가로 측정]·[세로 측정]으로 양 끝 두 점을 클릭하세요.";
            this._info.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _grid
            // 
            this._grid.ColumnCount = 4;
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.Controls.Add(this._lblW, 0, 0);
            this._grid.Controls.Add(this._txtW, 1, 0);
            this._grid.Controls.Add(this._btnMeasW, 2, 0);
            this._grid.Controls.Add(this._lblWpx, 3, 0);
            this._grid.Controls.Add(this._lblH, 0, 1);
            this._grid.Controls.Add(this._txtH, 1, 1);
            this._grid.Controls.Add(this._btnMeasH, 2, 1);
            this._grid.Controls.Add(this._lblHpx, 3, 1);
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(11, 725);
            this._grid.Name = "_grid";
            this._grid.RowCount = 2;
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._grid.Size = new System.Drawing.Size(1018, 78);
            this._grid.TabIndex = 2;
            // 
            // _lblW
            // 
            this._lblW.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblW.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._lblW.Location = new System.Drawing.Point(3, 0);
            this._lblW.Name = "_lblW";
            this._lblW.Size = new System.Drawing.Size(104, 40);
            this._lblW.TabIndex = 0;
            this._lblW.Text = "칩 가로 (mm)";
            this._lblW.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtW
            // 
            this._txtW.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtW.Font = new System.Drawing.Font("Consolas", 10F);
            this._txtW.Location = new System.Drawing.Point(112, 6);
            this._txtW.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this._txtW.Name = "_txtW";
            this._txtW.Size = new System.Drawing.Size(116, 23);
            this._txtW.TabIndex = 1;
            // 
            // _btnMeasW
            // 
            this._btnMeasW.BackColor = System.Drawing.Color.White;
            this._btnMeasW.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMeasW.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMeasW.Location = new System.Drawing.Point(232, 2);
            this._btnMeasW.Margin = new System.Windows.Forms.Padding(2);
            this._btnMeasW.Name = "_btnMeasW";
            this._btnMeasW.Size = new System.Drawing.Size(126, 36);
            this._btnMeasW.TabIndex = 2;
            this._btnMeasW.Text = "가로 측정";
            this._btnMeasW.UseVisualStyleBackColor = false;
            this._btnMeasW.Click += new System.EventHandler(this.OnMeasureWidthClick);
            // 
            // _lblWpx
            // 
            this._lblWpx.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblWpx.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblWpx.Location = new System.Drawing.Point(363, 0);
            this._lblWpx.Name = "_lblWpx";
            this._lblWpx.Size = new System.Drawing.Size(652, 40);
            this._lblWpx.TabIndex = 3;
            this._lblWpx.Text = "- px";
            this._lblWpx.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblH
            // 
            this._lblH.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblH.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._lblH.Location = new System.Drawing.Point(3, 40);
            this._lblH.Name = "_lblH";
            this._lblH.Size = new System.Drawing.Size(104, 40);
            this._lblH.TabIndex = 4;
            this._lblH.Text = "칩 세로 (mm)";
            this._lblH.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtH
            // 
            this._txtH.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtH.Font = new System.Drawing.Font("Consolas", 10F);
            this._txtH.Location = new System.Drawing.Point(112, 46);
            this._txtH.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this._txtH.Name = "_txtH";
            this._txtH.Size = new System.Drawing.Size(116, 23);
            this._txtH.TabIndex = 5;
            // 
            // _btnMeasH
            // 
            this._btnMeasH.BackColor = System.Drawing.Color.White;
            this._btnMeasH.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMeasH.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMeasH.Location = new System.Drawing.Point(232, 42);
            this._btnMeasH.Margin = new System.Windows.Forms.Padding(2);
            this._btnMeasH.Name = "_btnMeasH";
            this._btnMeasH.Size = new System.Drawing.Size(126, 36);
            this._btnMeasH.TabIndex = 6;
            this._btnMeasH.Text = "세로 측정";
            this._btnMeasH.UseVisualStyleBackColor = false;
            this._btnMeasH.Click += new System.EventHandler(this.OnMeasureHeightClick);
            // 
            // _lblHpx
            // 
            this._lblHpx.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblHpx.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblHpx.Location = new System.Drawing.Point(363, 40);
            this._lblHpx.Name = "_lblHpx";
            this._lblHpx.Size = new System.Drawing.Size(652, 40);
            this._lblHpx.TabIndex = 7;
            this._lblHpx.Text = "- px";
            this._lblHpx.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _btnRow
            // 
            this._btnRow.ColumnCount = 3;
            this._btnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this._btnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this._btnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this._btnRow.Controls.Add(this._btnFit, 0, 0);
            this._btnRow.Controls.Add(this._btnOk, 1, 0);
            this._btnRow.Controls.Add(this._btnCancel, 2, 0);
            this._btnRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnRow.Location = new System.Drawing.Point(11, 809);
            this._btnRow.Name = "_btnRow";
            this._btnRow.RowCount = 1;
            this._btnRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._btnRow.Size = new System.Drawing.Size(1018, 40);
            this._btnRow.TabIndex = 3;
            // 
            // _btnFit
            // 
            this._btnFit.BackColor = System.Drawing.Color.White;
            this._btnFit.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnFit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnFit.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnFit.Location = new System.Drawing.Point(2, 2);
            this._btnFit.Margin = new System.Windows.Forms.Padding(2);
            this._btnFit.Name = "_btnFit";
            this._btnFit.Size = new System.Drawing.Size(301, 36);
            this._btnFit.TabIndex = 0;
            this._btnFit.Text = "맞춤(Fit)";
            this._btnFit.UseVisualStyleBackColor = false;
            this._btnFit.Click += new System.EventHandler(this.OnFitClick);
            // 
            // _btnOk
            // 
            this._btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnOk.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnOk.ForeColor = System.Drawing.Color.White;
            this._btnOk.Location = new System.Drawing.Point(307, 2);
            this._btnOk.Margin = new System.Windows.Forms.Padding(2);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(403, 36);
            this._btnOk.TabIndex = 1;
            this._btnOk.Text = "계산 & 적용";
            this._btnOk.UseVisualStyleBackColor = false;
            this._btnOk.Click += new System.EventHandler(this.OnOkClick);
            // 
            // _btnCancel
            // 
            this._btnCancel.BackColor = System.Drawing.Color.White;
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnCancel.Location = new System.Drawing.Point(714, 2);
            this._btnCancel.Margin = new System.Windows.Forms.Padding(2);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(302, 36);
            this._btnCancel.TabIndex = 2;
            this._btnCancel.Text = "취소";
            this._btnCancel.UseVisualStyleBackColor = false;
            // 
            // ScaleCalibrationDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(1040, 860);
            this.Controls.Add(this._root);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 560);
            this.Name = "ScaleCalibrationDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "스케일 캘리브레이션 (측정 기반)";
            this._root.ResumeLayout(false);
            this._grid.ResumeLayout(false);
            this._grid.PerformLayout();
            this._btnRow.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
