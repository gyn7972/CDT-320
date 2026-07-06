namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class ProjectPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel listButtonLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.TableLayoutPanel footerLayout;
        private System.Windows.Forms.GroupBox grpProjects;
        private System.Windows.Forms.GroupBox grpSummary;
        private System.Windows.Forms.GroupBox grpGlobal;
        private System.Windows.Forms.GroupBox grpProjectOption;
        private System.Windows.Forms.GroupBox grpMap;
        private System.Windows.Forms.GroupBox grpXml;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.ListBox listProjects;
        private System.Windows.Forms.DataGridView gridSummary;
        private System.Windows.Forms.DataGridView gridGlobal;
        private System.Windows.Forms.DataGridView gridProject;
        private System.Windows.Forms.DataGridView gridMap;
        private System.Windows.Forms.DataGridView gridXml;
        private System.Windows.Forms.DataGridView gridStatus;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnOpenRecipeFolder;
        private System.Windows.Forms.Button btnBrowseMap;
        private System.Windows.Forms.Button btnOpenMap;
        private System.Windows.Forms.Button btnBrowseXml;
        private System.Windows.Forms.Button btnApplyCurrent;
        private System.Windows.Forms.Button btnSaveRecipe;
        private System.Windows.Forms.Button btnSaveAs;
        private System.Windows.Forms.Label lblCurrentProject;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpProjects = new System.Windows.Forms.GroupBox();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.listProjects = new System.Windows.Forms.ListBox();
            this.listButtonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSummary = new System.Windows.Forms.GroupBox();
            this.gridSummary = new System.Windows.Forms.DataGridView();
            this.grpGlobal = new System.Windows.Forms.GroupBox();
            this.gridGlobal = new System.Windows.Forms.DataGridView();
            this.grpProjectOption = new System.Windows.Forms.GroupBox();
            this.gridProject = new System.Windows.Forms.DataGridView();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMap = new System.Windows.Forms.GroupBox();
            this.gridMap = new System.Windows.Forms.DataGridView();
            this.grpXml = new System.Windows.Forms.GroupBox();
            this.gridXml = new System.Windows.Forms.DataGridView();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.gridStatus = new System.Windows.Forms.DataGridView();
            this.footerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblCurrentProject = new System.Windows.Forms.Label();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnOpenRecipeFolder = new System.Windows.Forms.Button();
            this.btnBrowseMap = new System.Windows.Forms.Button();
            this.btnOpenMap = new System.Windows.Forms.Button();
            this.btnBrowseXml = new System.Windows.Forms.Button();
            this.btnApplyCurrent = new System.Windows.Forms.Button();
            this.btnSaveRecipe = new System.Windows.Forms.Button();
            this.btnSaveAs = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpProjects.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.listButtonLayout.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSummary)).BeginInit();
            this.grpGlobal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridGlobal)).BeginInit();
            this.grpProjectOption.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridProject)).BeginInit();
            this.rightLayout.SuspendLayout();
            this.grpMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMap)).BeginInit();
            this.grpXml.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridXml)).BeginInit();
            this.grpStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStatus)).BeginInit();
            this.footerLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.footerLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.project";
            this.lblHeader.Text = "PROJECT";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 340F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.contentLayout.Controls.Add(this.grpProjects, 0, 0);
            this.contentLayout.Controls.Add(this.centerLayout, 1, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 2, 0);
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(1);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 812);
            this.contentLayout.TabIndex = 1;
            // 
            // grpProjects
            // 
            this.grpProjects.Controls.Add(this.leftLayout);
            this.grpProjects.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProjects.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpProjects.Location = new System.Drawing.Point(12, 12);
            this.grpProjects.Margin = new System.Windows.Forms.Padding(1);
            this.grpProjects.Name = "grpProjects";
            this.grpProjects.Size = new System.Drawing.Size(332, 788);
            this.grpProjects.TabIndex = 0;
            this.grpProjects.TabStop = false;
            this.grpProjects.Text = "PROJECT LIST";
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.listProjects, 0, 0);
            this.leftLayout.Controls.Add(this.listButtonLayout, 0, 1);
            this.leftLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(3, 21);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 8);
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 122F));
            this.leftLayout.Size = new System.Drawing.Size(326, 764);
            this.leftLayout.TabIndex = 0;
            // 
            // listProjects
            // 
            this.listProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listProjects.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.listProjects.FormattingEnabled = true;
            this.listProjects.ItemHeight = 15;
            this.listProjects.Location = new System.Drawing.Point(11, 15);
            this.listProjects.Name = "listProjects";
            this.listProjects.Size = new System.Drawing.Size(304, 616);
            this.listProjects.TabIndex = 0;
            // 
            // listButtonLayout
            // 
            this.listButtonLayout.ColumnCount = 2;
            this.listButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listButtonLayout.Controls.Add(this.btnNew, 0, 0);
            this.listButtonLayout.Controls.Add(this.btnOpen, 1, 0);
            this.listButtonLayout.Controls.Add(this.btnCopy, 0, 1);
            this.listButtonLayout.Controls.Add(this.btnDelete, 1, 1);
            this.listButtonLayout.Controls.Add(this.btnOpenFolder, 0, 2);
            this.listButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listButtonLayout.Location = new System.Drawing.Point(11, 637);
            this.listButtonLayout.Name = "listButtonLayout";
            this.listButtonLayout.RowCount = 3;
            this.listButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.listButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.listButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.listButtonLayout.Size = new System.Drawing.Size(304, 116);
            this.listButtonLayout.TabIndex = 1;
            // 
            // btnNew
            // 
            this.btnNew.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNew.Location = new System.Drawing.Point(2, 2);
            this.btnNew.Margin = new System.Windows.Forms.Padding(2);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(148, 34);
            this.btnNew.TabIndex = 0;
            this.btnNew.Text = "NEW";
            // 
            // btnOpen
            // 
            this.btnOpen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpen.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOpen.Location = new System.Drawing.Point(154, 2);
            this.btnOpen.Margin = new System.Windows.Forms.Padding(2);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(148, 34);
            this.btnOpen.TabIndex = 1;
            this.btnOpen.Text = "OPEN";
            // 
            // btnCopy
            // 
            this.btnCopy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopy.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCopy.Location = new System.Drawing.Point(2, 40);
            this.btnCopy.Margin = new System.Windows.Forms.Padding(2);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(148, 34);
            this.btnCopy.TabIndex = 2;
            this.btnCopy.Text = "COPY";
            // 
            // btnDelete
            // 
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDelete.Location = new System.Drawing.Point(154, 40);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(2);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(148, 34);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "DELETE";
            // 
            // btnOpenFolder
            // 
            this.listButtonLayout.SetColumnSpan(this.btnOpenFolder, 2);
            this.btnOpenFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFolder.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOpenFolder.Location = new System.Drawing.Point(2, 78);
            this.btnOpenFolder.Margin = new System.Windows.Forms.Padding(2);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(300, 36);
            this.btnOpenFolder.TabIndex = 4;
            this.btnOpenFolder.Text = "OPEN PROJECT FOLDER";
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpSummary, 0, 0);
            this.centerLayout.Controls.Add(this.grpGlobal, 0, 1);
            this.centerLayout.Controls.Add(this.grpProjectOption, 0, 2);
            this.centerLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(352, 8);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 3;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 37F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.centerLayout.Size = new System.Drawing.Size(625, 796);
            this.centerLayout.TabIndex = 1;
            // 
            // grpSummary
            // 
            this.grpSummary.Controls.Add(this.gridSummary);
            this.grpSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSummary.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSummary.Location = new System.Drawing.Point(4, 4);
            this.grpSummary.Margin = new System.Windows.Forms.Padding(1);
            this.grpSummary.Name = "grpSummary";
            this.grpSummary.Size = new System.Drawing.Size(617, 214);
            this.grpSummary.TabIndex = 0;
            this.grpSummary.TabStop = false;
            this.grpSummary.Text = "PROJECT SUMMARY";
            // 
            // gridSummary
            // 
            this.gridSummary.AllowUserToAddRows = false;
            this.gridSummary.AllowUserToDeleteRows = false;
            this.gridSummary.AllowUserToResizeRows = false;
            this.gridSummary.BackgroundColor = System.Drawing.Color.White;
            this.gridSummary.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSummary.Location = new System.Drawing.Point(3, 21);
            this.gridSummary.Name = "gridSummary";
            this.gridSummary.RowHeadersVisible = false;
            this.gridSummary.RowTemplate.Height = 24;
            this.gridSummary.Size = new System.Drawing.Size(611, 190);
            this.gridSummary.TabIndex = 0;
            // 
            // grpGlobal
            // 
            this.grpGlobal.Controls.Add(this.gridGlobal);
            this.grpGlobal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGlobal.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpGlobal.Location = new System.Drawing.Point(4, 226);
            this.grpGlobal.Margin = new System.Windows.Forms.Padding(1);
            this.grpGlobal.Name = "grpGlobal";
            this.grpGlobal.Size = new System.Drawing.Size(617, 286);
            this.grpGlobal.TabIndex = 1;
            this.grpGlobal.TabStop = false;
            this.grpGlobal.Text = "GLOBAL OPTION";
            // 
            // gridGlobal
            // 
            this.gridGlobal.AllowUserToAddRows = false;
            this.gridGlobal.AllowUserToDeleteRows = false;
            this.gridGlobal.AllowUserToResizeRows = false;
            this.gridGlobal.BackgroundColor = System.Drawing.Color.White;
            this.gridGlobal.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridGlobal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridGlobal.Location = new System.Drawing.Point(3, 21);
            this.gridGlobal.Name = "gridGlobal";
            this.gridGlobal.RowHeadersVisible = false;
            this.gridGlobal.RowTemplate.Height = 24;
            this.gridGlobal.Size = new System.Drawing.Size(611, 262);
            this.gridGlobal.TabIndex = 0;
            // 
            // grpProjectOption
            // 
            this.grpProjectOption.Controls.Add(this.gridProject);
            this.grpProjectOption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpProjectOption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProjectOption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpProjectOption.Location = new System.Drawing.Point(4, 520);
            this.grpProjectOption.Margin = new System.Windows.Forms.Padding(1);
            this.grpProjectOption.Name = "grpProjectOption";
            this.grpProjectOption.Size = new System.Drawing.Size(617, 272);
            this.grpProjectOption.TabIndex = 2;
            this.grpProjectOption.TabStop = false;
            this.grpProjectOption.Text = "PROJECT OPTION";
            // 
            // gridProject
            // 
            this.gridProject.AllowUserToAddRows = false;
            this.gridProject.AllowUserToDeleteRows = false;
            this.gridProject.AllowUserToResizeRows = false;
            this.gridProject.BackgroundColor = System.Drawing.Color.White;
            this.gridProject.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridProject.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProject.Location = new System.Drawing.Point(3, 21);
            this.gridProject.Name = "gridProject";
            this.gridProject.RowHeadersVisible = false;
            this.gridProject.RowTemplate.Height = 24;
            this.gridProject.Size = new System.Drawing.Size(611, 248);
            this.gridProject.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpMap, 0, 0);
            this.rightLayout.Controls.Add(this.grpXml, 0, 1);
            this.rightLayout.Controls.Add(this.grpStatus, 0, 2);
            this.rightLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(985, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.rightLayout.Size = new System.Drawing.Size(681, 796);
            this.rightLayout.TabIndex = 2;
            // 
            // grpMap
            // 
            this.grpMap.Controls.Add(this.gridMap);
            this.grpMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMap.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpMap.Location = new System.Drawing.Point(4, 4);
            this.grpMap.Margin = new System.Windows.Forms.Padding(1);
            this.grpMap.Name = "grpMap";
            this.grpMap.Size = new System.Drawing.Size(673, 262);
            this.grpMap.TabIndex = 0;
            this.grpMap.TabStop = false;
            this.grpMap.Text = "MAP FILE";
            // 
            // gridMap
            // 
            this.gridMap.AllowUserToAddRows = false;
            this.gridMap.AllowUserToDeleteRows = false;
            this.gridMap.AllowUserToResizeRows = false;
            this.gridMap.BackgroundColor = System.Drawing.Color.White;
            this.gridMap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridMap.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMap.Location = new System.Drawing.Point(3, 21);
            this.gridMap.Name = "gridMap";
            this.gridMap.RowHeadersVisible = false;
            this.gridMap.RowTemplate.Height = 24;
            this.gridMap.Size = new System.Drawing.Size(667, 238);
            this.gridMap.TabIndex = 0;
            // 
            // grpXml
            // 
            this.grpXml.Controls.Add(this.gridXml);
            this.grpXml.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpXml.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpXml.Location = new System.Drawing.Point(4, 274);
            this.grpXml.Margin = new System.Windows.Forms.Padding(1);
            this.grpXml.Name = "grpXml";
            this.grpXml.Size = new System.Drawing.Size(673, 214);
            this.grpXml.TabIndex = 1;
            this.grpXml.TabStop = false;
            this.grpXml.Text = "XML TRACE FILE";
            // 
            // gridXml
            // 
            this.gridXml.AllowUserToAddRows = false;
            this.gridXml.AllowUserToDeleteRows = false;
            this.gridXml.AllowUserToResizeRows = false;
            this.gridXml.BackgroundColor = System.Drawing.Color.White;
            this.gridXml.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridXml.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridXml.Location = new System.Drawing.Point(3, 21);
            this.gridXml.Name = "gridXml";
            this.gridXml.RowHeadersVisible = false;
            this.gridXml.RowTemplate.Height = 24;
            this.gridXml.Size = new System.Drawing.Size(667, 190);
            this.gridXml.TabIndex = 0;
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.gridStatus);
            this.grpStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStatus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpStatus.Location = new System.Drawing.Point(4, 496);
            this.grpStatus.Margin = new System.Windows.Forms.Padding(1);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(673, 296);
            this.grpStatus.TabIndex = 2;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "RECIPE DATA STATUS";
            // 
            // gridStatus
            // 
            this.gridStatus.AllowUserToAddRows = false;
            this.gridStatus.AllowUserToDeleteRows = false;
            this.gridStatus.AllowUserToResizeRows = false;
            this.gridStatus.BackgroundColor = System.Drawing.Color.White;
            this.gridStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridStatus.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridStatus.Location = new System.Drawing.Point(3, 21);
            this.gridStatus.Name = "gridStatus";
            this.gridStatus.ReadOnly = true;
            this.gridStatus.RowHeadersVisible = false;
            this.gridStatus.RowTemplate.Height = 24;
            this.gridStatus.Size = new System.Drawing.Size(667, 272);
            this.gridStatus.TabIndex = 0;
            // 
            // footerLayout
            // 
            this.footerLayout.ColumnCount = 9;
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.Controls.Add(this.lblCurrentProject, 0, 0);
            this.footerLayout.Controls.Add(this.btnReload, 1, 0);
            this.footerLayout.Controls.Add(this.btnOpenRecipeFolder, 2, 0);
            this.footerLayout.Controls.Add(this.btnBrowseMap, 3, 0);
            this.footerLayout.Controls.Add(this.btnOpenMap, 4, 0);
            this.footerLayout.Controls.Add(this.btnBrowseXml, 5, 0);
            this.footerLayout.Controls.Add(this.btnApplyCurrent, 6, 0);
            this.footerLayout.Controls.Add(this.btnSaveRecipe, 7, 0);
            this.footerLayout.Controls.Add(this.btnSaveAs, 8, 0);
            this.footerLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.footerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerLayout.Location = new System.Drawing.Point(0, 842);
            this.footerLayout.Margin = new System.Windows.Forms.Padding(0);
            this.footerLayout.Name = "footerLayout";
            this.footerLayout.Padding = new System.Windows.Forms.Padding(8);
            this.footerLayout.RowCount = 1;
            this.footerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.Size = new System.Drawing.Size(1678, 58);
            this.footerLayout.TabIndex = 2;
            // 
            // lblCurrentProject
            // 
            this.lblCurrentProject.BackColor = System.Drawing.Color.White;
            this.lblCurrentProject.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCurrentProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentProject.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCurrentProject.Location = new System.Drawing.Point(11, 8);
            this.lblCurrentProject.Name = "lblCurrentProject";
            this.lblCurrentProject.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCurrentProject.Size = new System.Drawing.Size(626, 42);
            this.lblCurrentProject.TabIndex = 0;
            this.lblCurrentProject.Text = "Current Project: -";
            this.lblCurrentProject.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnReload
            // 
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(643, 11);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(114, 36);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = "RELOAD";
            // 
            // btnOpenRecipeFolder
            // 
            this.btnOpenRecipeFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenRecipeFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenRecipeFolder.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOpenRecipeFolder.Location = new System.Drawing.Point(763, 11);
            this.btnOpenRecipeFolder.Name = "btnOpenRecipeFolder";
            this.btnOpenRecipeFolder.Size = new System.Drawing.Size(164, 36);
            this.btnOpenRecipeFolder.TabIndex = 2;
            this.btnOpenRecipeFolder.Text = "OPEN RECIPE DATA";
            // 
            // btnBrowseMap
            // 
            this.btnBrowseMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowseMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBrowseMap.Location = new System.Drawing.Point(933, 11);
            this.btnBrowseMap.Name = "btnBrowseMap";
            this.btnBrowseMap.Size = new System.Drawing.Size(114, 36);
            this.btnBrowseMap.TabIndex = 3;
            this.btnBrowseMap.Text = "MAP BROWSE";
            // 
            // btnOpenMap
            // 
            this.btnOpenMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnOpenMap.Location = new System.Drawing.Point(1053, 11);
            this.btnOpenMap.Name = "btnOpenMap";
            this.btnOpenMap.Size = new System.Drawing.Size(114, 36);
            this.btnOpenMap.TabIndex = 4;
            this.btnOpenMap.Text = "MAP OPEN";
            // 
            // btnBrowseXml
            // 
            this.btnBrowseXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowseXml.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseXml.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBrowseXml.Location = new System.Drawing.Point(1173, 11);
            this.btnBrowseXml.Name = "btnBrowseXml";
            this.btnBrowseXml.Size = new System.Drawing.Size(114, 36);
            this.btnBrowseXml.TabIndex = 5;
            this.btnBrowseXml.Text = "XML PATH";
            // 
            // btnApplyCurrent
            // 
            this.btnApplyCurrent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.btnApplyCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyCurrent.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnApplyCurrent.ForeColor = System.Drawing.Color.White;
            this.btnApplyCurrent.Location = new System.Drawing.Point(1293, 11);
            this.btnApplyCurrent.Name = "btnApplyCurrent";
            this.btnApplyCurrent.Size = new System.Drawing.Size(134, 36);
            this.btnApplyCurrent.TabIndex = 6;
            this.btnApplyCurrent.Text = "APPLY CURRENT";
            this.btnApplyCurrent.UseVisualStyleBackColor = false;
            // 
            // btnSaveRecipe
            // 
            this.btnSaveRecipe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnSaveRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveRecipe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveRecipe.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveRecipe.ForeColor = System.Drawing.Color.White;
            this.btnSaveRecipe.Location = new System.Drawing.Point(1433, 11);
            this.btnSaveRecipe.Name = "btnSaveRecipe";
            this.btnSaveRecipe.Size = new System.Drawing.Size(114, 36);
            this.btnSaveRecipe.TabIndex = 7;
            this.btnSaveRecipe.Text = "SAVE";
            this.btnSaveRecipe.UseVisualStyleBackColor = false;
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveAs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveAs.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveAs.Location = new System.Drawing.Point(1553, 11);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(114, 36);
            this.btnSaveAs.TabIndex = 8;
            this.btnSaveAs.Text = "SAVE AS";
            // 
            // ProjectPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "ProjectPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpProjects.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.listButtonLayout.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSummary)).EndInit();
            this.grpGlobal.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridGlobal)).EndInit();
            this.grpProjectOption.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridProject)).EndInit();
            this.rightLayout.ResumeLayout(false);
            this.grpMap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMap)).EndInit();
            this.grpXml.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridXml)).EndInit();
            this.grpStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridStatus)).EndInit();
            this.footerLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
