namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class ForceControlPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.GroupBox grpDo;
        private System.Windows.Forms.GroupBox grpDi;
        private System.Windows.Forms.DataGridView gridDo;
        private System.Windows.Forms.DataGridView gridDi;
        private System.Windows.Forms.TableLayoutPanel actionPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnForceOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnForceOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllOff;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle gridHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpDo = new System.Windows.Forms.GroupBox();
            this.grpDi = new System.Windows.Forms.GroupBox();
            this.gridDo = new System.Windows.Forms.DataGridView();
            this.gridDi = new System.Windows.Forms.DataGridView();
            this.actionPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnForceOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnForceOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpDo.SuspendLayout();
            this.grpDi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDi)).BeginInit();
            this.actionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionPanel, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.forceControl";
            this.lblHeader.Text = "FORCE CONTROL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.grpDo, 0, 0);
            this.contentLayout.Controls.Add(this.grpDi, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1400, 806);
            this.contentLayout.TabIndex = 1;
            // 
            // grpDo
            // 
            this.grpDo.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpDo.Controls.Add(this.gridDo);
            this.grpDo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDo.Margin = new System.Windows.Forms.Padding(4);
            this.grpDo.Name = "grpDo";
            this.grpDo.Size = new System.Drawing.Size(684, 790);
            this.grpDo.TabIndex = 0;
            this.grpDo.TabStop = false;
            this.grpDo.Text = "DO FORCE";
            // 
            // grpDi
            // 
            this.grpDi.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpDi.Controls.Add(this.gridDi);
            this.grpDi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDi.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDi.Margin = new System.Windows.Forms.Padding(4);
            this.grpDi.Name = "grpDi";
            this.grpDi.Size = new System.Drawing.Size(684, 790);
            this.grpDi.TabIndex = 1;
            this.grpDi.TabStop = false;
            this.grpDi.Text = "DI STATE";
            // 
            // grid style
            // 
            gridHeaderStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            gridHeaderStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            gridHeaderStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            gridHeaderStyle.ForeColor = System.Drawing.Color.White;
            this.gridDo.AllowUserToAddRows = false;
            this.gridDo.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDo.BackgroundColor = System.Drawing.Color.White;
            this.gridDo.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.gridDo.Columns.Add("IDX", "IDX");
            this.gridDo.Columns.Add("SYM", "SYM");
            this.gridDo.Columns.Add("DESC", "DESCRIPTION");
            this.gridDo.Columns.Add("STATE", "STATE");
            this.gridDo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDo.EnableHeadersVisualStyles = false;
            this.gridDo.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridDo.Location = new System.Drawing.Point(3, 26);
            this.gridDo.Name = "gridDo";
            this.gridDo.RowHeadersVisible = false;
            this.gridDo.RowTemplate.Height = 26;
            this.gridDo.Size = new System.Drawing.Size(678, 761);
            this.gridDo.TabIndex = 0;
            this.gridDi.AllowUserToAddRows = false;
            this.gridDi.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDi.BackgroundColor = System.Drawing.Color.White;
            this.gridDi.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.gridDi.Columns.Add("IDX", "IDX");
            this.gridDi.Columns.Add("SYM", "SYM");
            this.gridDi.Columns.Add("DESC", "DESCRIPTION");
            this.gridDi.Columns.Add("STATE", "STATE");
            this.gridDi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDi.EnableHeadersVisualStyles = false;
            this.gridDi.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridDi.Location = new System.Drawing.Point(3, 26);
            this.gridDi.Name = "gridDi";
            this.gridDi.ReadOnly = true;
            this.gridDi.RowHeadersVisible = false;
            this.gridDi.RowTemplate.Height = 26;
            this.gridDi.Size = new System.Drawing.Size(678, 761);
            this.gridDi.TabIndex = 0;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.actionPanel.ColumnCount = 5;
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.actionPanel.Controls.Add(this.btnForceOn, 0, 0);
            this.actionPanel.Controls.Add(this.btnForceOff, 1, 0);
            this.actionPanel.Controls.Add(this.btnAllOff, 2, 0);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(8);
            this.actionPanel.RowCount = 1;
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionPanel.Size = new System.Drawing.Size(1400, 64);
            this.actionPanel.TabIndex = 2;
            this.btnForceOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnForceOn.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnForceOn.Margin = new System.Windows.Forms.Padding(4);
            this.btnForceOn.Name = "btnForceOn";
            this.btnForceOn.Size = new System.Drawing.Size(142, 48);
            this.btnForceOn.TabIndex = 0;
            this.btnForceOn.Text = "FORCE ON";
            this.btnForceOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnForceOff.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnForceOff.Margin = new System.Windows.Forms.Padding(4);
            this.btnForceOff.Name = "btnForceOff";
            this.btnForceOff.Size = new System.Drawing.Size(142, 48);
            this.btnForceOff.TabIndex = 1;
            this.btnForceOff.Text = "FORCE OFF";
            this.btnAllOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllOff.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnAllOff.Margin = new System.Windows.Forms.Padding(4);
            this.btnAllOff.Name = "btnAllOff";
            this.btnAllOff.Size = new System.Drawing.Size(142, 48);
            this.btnAllOff.TabIndex = 2;
            this.btnAllOff.Text = "ALL OFF";
            // 
            // ForceControlPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "ForceControlPage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpDo.ResumeLayout(false);
            this.grpDi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridDo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDi)).EndInit();
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
