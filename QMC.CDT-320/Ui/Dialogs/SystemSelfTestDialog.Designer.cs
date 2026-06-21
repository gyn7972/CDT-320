namespace QMC.CDT_320.Ui.Dialogs
{
    partial class SystemSelfTestDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.ProgressBar _pb;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.Button _btnRun;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colState;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetail;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._pb = new System.Windows.Forms.ProgressBar();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnRun = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.bottomLayout.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this._grid, 0, 1);
            this.rootLayout.Controls.Add(this._pb, 0, 2);
            this.rootLayout.Controls.Add(this.bottomLayout, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 12F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Text = "SYSTEM SELF-TEST";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._grid.AllowUserToAddRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            this._grid.ColumnHeadersDefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this._grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this._grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colName, this.colState, this.colDetail });
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._grid.MultiSelect = false;
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 26;
            this.colName.HeaderText = "Check";
            this.colName.Name = "NAME";
            this.colName.ReadOnly = true;
            this.colState.HeaderText = "State";
            this.colState.Name = "STATE";
            this.colState.ReadOnly = true;
            this.colDetail.HeaderText = "Detail";
            this.colDetail.Name = "DETAIL";
            this.colDetail.ReadOnly = true;
            this._pb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.ColumnCount = 4;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.bottomLayout.Controls.Add(this._btnRun, 1, 0);
            this.bottomLayout.Controls.Add(this._btnClose, 2, 0);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            this.bottomLayout.RowCount = 1;
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._btnRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRun.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnRun.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._btnRun.Text = "RUN";
            this._btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnClose.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._btnClose.Text = "CLOSE";
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(820, 560);
            this.Controls.Add(this.rootLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SystemSelfTestDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "System Self-Test";
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.bottomLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
