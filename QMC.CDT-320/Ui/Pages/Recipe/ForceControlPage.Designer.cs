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
        private System.Windows.Forms.TableLayoutPanel actionRow;
        private System.Windows.Forms.GroupBox grpActions;
        private QMC.CDT_320.Ui.Controls.ManualActionPanelControl manualActionPanel;

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
            this.actionRow = new System.Windows.Forms.TableLayoutPanel();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.manualActionPanel = new QMC.CDT_320.Ui.Controls.ManualActionPanelControl();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpDo.SuspendLayout();
            this.grpDi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDi)).BeginInit();
            this.actionRow.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionRow, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 68F));
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
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.forceControl";
            this.lblHeader.Text = "FORCE CONTROL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // contentLayout
            //
            this.contentLayout.BackColor = System.Drawing.Color.White;
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.grpDo, 0, 0);
            this.contentLayout.Controls.Add(this.grpDi, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(1);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1400, 770);
            this.contentLayout.TabIndex = 1;
            //
            // grpDo
            //
            this.grpDo.BackColor = System.Drawing.Color.White;
            this.grpDo.Controls.Add(this.gridDo);
            this.grpDo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDo.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.grpDo.Name = "grpDo";
            this.grpDo.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpDo.Size = new System.Drawing.Size(684, 754);
            this.grpDo.TabIndex = 0;
            this.grpDo.TabStop = false;
            this.grpDo.Text = "DO FORCE";
            //
            // grpDi
            //
            this.grpDi.BackColor = System.Drawing.Color.White;
            this.grpDi.Controls.Add(this.gridDi);
            this.grpDi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDi.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDi.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.grpDi.Name = "grpDi";
            this.grpDi.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpDi.Size = new System.Drawing.Size(684, 754);
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
            this.gridDo.AllowUserToResizeRows = false;
            this.gridDo.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDo.BackgroundColor = System.Drawing.Color.White;
            this.gridDo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridDo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridDo.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.gridDo.Columns.Add("IDX", "IDX");
            this.gridDo.Columns.Add("SYM", "SYM");
            this.gridDo.Columns.Add("DESC", "DESCRIPTION");
            this.gridDo.Columns.Add("STATE", "STATE");
            this.gridDo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDo.EnableHeadersVisualStyles = false;
            this.gridDo.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridDo.Location = new System.Drawing.Point(6, 26);
            this.gridDo.Name = "gridDo";
            this.gridDo.RowHeadersVisible = false;
            this.gridDo.RowTemplate.Height = 26;
            this.gridDo.Size = new System.Drawing.Size(672, 726);
            this.gridDo.TabIndex = 0;
            this.gridDi.AllowUserToAddRows = false;
            this.gridDi.AllowUserToResizeRows = false;
            this.gridDi.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDi.BackgroundColor = System.Drawing.Color.White;
            this.gridDi.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridDi.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridDi.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.gridDi.Columns.Add("IDX", "IDX");
            this.gridDi.Columns.Add("SYM", "SYM");
            this.gridDi.Columns.Add("DESC", "DESCRIPTION");
            this.gridDi.Columns.Add("STATE", "STATE");
            this.gridDi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDi.EnableHeadersVisualStyles = false;
            this.gridDi.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridDi.Location = new System.Drawing.Point(6, 26);
            this.gridDi.Name = "gridDi";
            this.gridDi.ReadOnly = true;
            this.gridDi.RowHeadersVisible = false;
            this.gridDi.RowTemplate.Height = 26;
            this.gridDi.Size = new System.Drawing.Size(672, 726);
            this.gridDi.TabIndex = 0;
            //
            // actionRow  (하단 액션 영역 — 좌측 50%만 사용)
            //
            this.actionRow.BackColor = System.Drawing.Color.White;
            this.actionRow.ColumnCount = 2;
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRow.Controls.Add(this.grpActions, 0, 0);
            this.actionRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionRow.Margin = new System.Windows.Forms.Padding(0);
            this.actionRow.Name = "actionRow";
            this.actionRow.RowCount = 1;
            this.actionRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionRow.TabIndex = 2;
            //
            // grpActions  (하단 MANUAL ACTION — 1행 3열, 좌측 50%)
            //
            this.grpActions.BackColor = System.Drawing.Color.White;
            this.grpActions.Controls.Add(this.manualActionPanel);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpActions.Margin = new System.Windows.Forms.Padding(1, 0, 1, 1);
            this.grpActions.Name = "grpActions";
            this.grpActions.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpActions.TabIndex = 2;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "MANUAL ACTION";
            //
            // manualActionPanel
            //
            this.manualActionPanel.Location = new System.Drawing.Point(6, 17);
            this.manualActionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.manualActionPanel.Name = "manualActionPanel";
            this.manualActionPanel.Size = new System.Drawing.Size(800, 45);
            this.manualActionPanel.TabIndex = 0;
            //
            // ForceControlPage
            //
            this.Controls.Add(this.rootLayout);
            this.Name = "ForceControlPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpDo.ResumeLayout(false);
            this.grpDi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridDo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridDi)).EndInit();
            this.grpActions.ResumeLayout(false);
            this.actionRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
