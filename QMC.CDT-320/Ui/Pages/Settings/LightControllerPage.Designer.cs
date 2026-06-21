namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class LightControllerPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.TableLayoutPanel actionsLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllOff;

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
            this.lblSubHeader = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.DataGridView();
            this.actionsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.actionsLayout.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblSubHeader, 0, 1);
            this.rootLayout.Controls.Add(this._grid, 0, 2);
            this.rootLayout.Controls.Add(this.actionsLayout, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "LIGHT SETUP";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSubHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblSubHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSubHeader.Text = "LIGHT CONTROLLER - channel level and mode";
            this.lblSubHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            headerStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.White;
            this._grid.ColumnHeadersDefaultCellStyle = headerStyle;
            this._grid.Columns.Add("CH", "CH");
            this._grid.Columns.Add("NAME", "NAME");
            this._grid.Columns.Add("COM", "COM PORT");
            this._grid.Columns.Add("LEVEL", "LEVEL (0~255)");
            this._grid.Columns.Add("MODE", "MODE");
            this._grid.Columns.Add("COLOR", "COLOR");
            this._grid.Columns.Add("ACTIVE", "ACTIVE");
            this._grid.Columns["CH"].ReadOnly = true;
            this._grid.Columns["NAME"].ReadOnly = true;
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._grid.MultiSelect = false;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 26;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.actionsLayout.ColumnCount = 5;
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsLayout.Controls.Add(this.btnSave, 0, 0);
            this.actionsLayout.Controls.Add(this.btnReload, 1, 0);
            this.actionsLayout.Controls.Add(this.btnAllOn, 2, 0);
            this.actionsLayout.Controls.Add(this.btnAllOff, 3, 0);
            this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsLayout.RowCount = 1;
            this.actionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnSave.Text = "SAVE";
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnReload.Text = "RELOAD";
            this.btnAllOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllOn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllOn.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllOn.Text = "ALL ON";
            this.btnAllOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllOff.Text = "ALL OFF";
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "LightControllerPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.actionsLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
