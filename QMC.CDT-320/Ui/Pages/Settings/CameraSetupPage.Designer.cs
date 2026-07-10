namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class CameraSetupPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox _cameraGroup;
        private System.Windows.Forms.GroupBox _actionGroup;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.TableLayoutPanel actionsLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnTest;
        private QMC.CDT_320.Ui.Controls.ActionButton btnApply;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this._cameraGroup = new System.Windows.Forms.GroupBox();
            this._actionGroup = new System.Windows.Forms.GroupBox();
            this._grid = new System.Windows.Forms.DataGridView();
            this.actionsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this._cameraGroup.SuspendLayout();
            this._actionGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.actionsLayout.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this._cameraGroup, 0, 1);
            this.rootLayout.Controls.Add(this._actionGroup, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "CAMERA SETUP";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            headerStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.White;
            this._grid.ColumnHeadersDefaultCellStyle = headerStyle;
            this._grid.Columns.Add("IDX", "INDEX");
            this._grid.Columns.Add("CH", "CHANNEL");
            this._grid.Columns.Add("ROLE", "ROLE");
            this._grid.Columns.Add("HOST", "HOST");
            this._grid.Columns.Add("PORT", "PORT");
            this._grid.Columns.Add("W", "WIDTH");
            this._grid.Columns.Add("H", "HEIGHT");
            this._grid.Columns.Add("EXP", "EXPOSURE(ms)");
            this._grid.Columns.Add("LIGHT", "LIGHT");
            this._grid.Columns.Add("TRG", "TRIGGER");
            this._grid.Columns.Add("AUTO", "AUTO CONN");
            this._grid.Columns["IDX"].ReadOnly = true;
            this._grid.Columns["CH"].ReadOnly = true;
            this._grid.Columns["ROLE"].ReadOnly = true;
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._grid.MultiSelect = false;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 26;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellEdit);
            // _cameraGroup — DataGridView를 담는 그룹(높이는 런타임 AdjustCameraGroupHeight가 행수에 맞춰 조정)
            this._cameraGroup.Controls.Add(this._grid);
            this._cameraGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this._cameraGroup.Height = 200;
            this._cameraGroup.Padding = new System.Windows.Forms.Padding(1, 8, 1, 1);
            this._cameraGroup.Text = "CAMERA SETUP";
            // _actionGroup — 액션 버튼 행을 담는 그룹
            this._actionGroup.Controls.Add(this.actionsLayout);
            this._actionGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._actionGroup.Text = "ACTION";
            this.actionsLayout.ColumnCount = 14;
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsLayout.Controls.Add(this.btnSave, 0, 0);
            this.actionsLayout.Controls.Add(this.btnReload, 1, 0);
            this.actionsLayout.Controls.Add(this.btnTest, 2, 0);
            this.actionsLayout.Controls.Add(this.btnApply, 3, 0);
            this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsLayout.RowCount = 1;
            this.actionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnSave.Text = "SAVE";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnReload.Text = "RELOAD";
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnTest.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnTest.Text = "TEST CONN";
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApply.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnApply.Text = "APPLY";
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "CameraSetupPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this._cameraGroup.ResumeLayout(false);
            this._actionGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.actionsLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
