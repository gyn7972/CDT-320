namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class ProjectPage
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel listButtonLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.TableLayoutPanel footerLayout;
        private System.Windows.Forms.TableLayoutPanel projectInfoLayout;
        private System.Windows.Forms.TableLayoutPanel mapSettingsLayout;
        private System.Windows.Forms.TableLayoutPanel advancedLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblCurrentProject;
        private System.Windows.Forms.Label lblFooterHint;
        private System.Windows.Forms.GroupBox grpProjects;
        private System.Windows.Forms.GroupBox grpSummary;
        private System.Windows.Forms.GroupBox grpGlobal;
        private System.Windows.Forms.GroupBox grpProjectOption;
        private System.Windows.Forms.GroupBox grpMap;
        private System.Windows.Forms.GroupBox grpXml;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.GroupBox grpInputMap;
        private System.Windows.Forms.GroupBox grpOutputMap;
        private System.Windows.Forms.GroupBox grpMapApproval;
        private System.Windows.Forms.ListBox listProjects;
        private System.Windows.Forms.DataGridView gridSummary;
        private System.Windows.Forms.DataGridView gridGlobal;
        private System.Windows.Forms.DataGridView gridProject;
        private System.Windows.Forms.DataGridView gridMap;
        private System.Windows.Forms.DataGridView gridXml;
        private System.Windows.Forms.DataGridView gridStatus;
        private System.Windows.Forms.DataGridView gridInputMap;
        private System.Windows.Forms.DataGridView gridOutputMap;
        private System.Windows.Forms.DataGridView gridMapApproval;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnManage;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnBrowseMap;
        private System.Windows.Forms.Button btnApplyCurrent;
        private System.Windows.Forms.Button btnSaveRecipe;
        private System.Windows.Forms.TabControl tabsRecipe;
        private System.Windows.Forms.TabPage tabBasic;
        private System.Windows.Forms.TabPage tabDetails;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.ContextMenuStrip menuManagement;
        private System.Windows.Forms.ToolStripMenuItem menuNewRecipe;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteRecipe;
        private System.Windows.Forms.ToolStripMenuItem menuProjectFolder;
        private System.Windows.Forms.ToolStripMenuItem menuRecipeFolder;
        private System.Windows.Forms.ToolStripMenuItem menuMapFolder;
        private System.Windows.Forms.ToolStripMenuItem menuXmlPath;
        private System.Windows.Forms.ToolStripSeparator menuFolderSeparator;
        private System.Windows.Forms.ToolStripSeparator menuAdvancedSeparator;
        private System.Windows.Forms.Timer timerSettingsMonitor;
        private System.Windows.Forms.ToolTip toolTipSettings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblCurrentProject = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpProjects = new System.Windows.Forms.GroupBox();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.listProjects = new System.Windows.Forms.ListBox();
            this.listButtonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnManage = new System.Windows.Forms.Button();
            this.tabsRecipe = new System.Windows.Forms.TabControl();
            this.tabBasic = new System.Windows.Forms.TabPage();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.projectInfoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSummary = new System.Windows.Forms.GroupBox();
            this.gridSummary = new System.Windows.Forms.DataGridView();
            this.grpProjectOption = new System.Windows.Forms.GroupBox();
            this.gridProject = new System.Windows.Forms.DataGridView();
            this.mapSettingsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpInputMap = new System.Windows.Forms.GroupBox();
            this.gridInputMap = new System.Windows.Forms.DataGridView();
            this.grpOutputMap = new System.Windows.Forms.GroupBox();
            this.gridOutputMap = new System.Windows.Forms.DataGridView();
            this.grpMapApproval = new System.Windows.Forms.GroupBox();
            this.gridMapApproval = new System.Windows.Forms.DataGridView();
            this.btnBrowseMap = new System.Windows.Forms.Button();
            this.tabDetails = new System.Windows.Forms.TabPage();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMap = new System.Windows.Forms.GroupBox();
            this.gridMap = new System.Windows.Forms.DataGridView();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.gridStatus = new System.Windows.Forms.DataGridView();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.advancedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpGlobal = new System.Windows.Forms.GroupBox();
            this.gridGlobal = new System.Windows.Forms.DataGridView();
            this.grpXml = new System.Windows.Forms.GroupBox();
            this.gridXml = new System.Windows.Forms.DataGridView();
            this.footerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblFooterHint = new System.Windows.Forms.Label();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSaveRecipe = new System.Windows.Forms.Button();
            this.btnApplyCurrent = new System.Windows.Forms.Button();
            this.menuManagement = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuNewRecipe = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteRecipe = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFolderSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.menuProjectFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecipeFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMapFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAdvancedSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.menuXmlPath = new System.Windows.Forms.ToolStripMenuItem();
            this.timerSettingsMonitor = new System.Windows.Forms.Timer(this.components);
            this.toolTipSettings = new System.Windows.Forms.ToolTip(this.components);
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpProjects.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.listButtonLayout.SuspendLayout();
            this.tabsRecipe.SuspendLayout();
            this.tabBasic.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.projectInfoLayout.SuspendLayout();
            this.grpSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSummary)).BeginInit();
            this.grpProjectOption.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridProject)).BeginInit();
            this.mapSettingsLayout.SuspendLayout();
            this.grpInputMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridInputMap)).BeginInit();
            this.grpOutputMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridOutputMap)).BeginInit();
            this.grpMapApproval.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMapApproval)).BeginInit();
            this.tabDetails.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMap)).BeginInit();
            this.grpStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStatus)).BeginInit();
            this.tabAdvanced.SuspendLayout();
            this.advancedLayout.SuspendLayout();
            this.grpGlobal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridGlobal)).BeginInit();
            this.grpXml.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridXml)).BeginInit();
            this.footerLayout.SuspendLayout();
            this.menuManagement.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblCurrentProject, 0, 1);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
            this.rootLayout.Controls.Add(this.footerLayout, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(4);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(12, 12);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(1654, 20);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "레시피 · 프로젝트";
            // 
            // lblCurrentProject
            // 
            this.lblCurrentProject.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(242)))), ((int)(((byte)(248)))));
            this.lblCurrentProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentProject.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblCurrentProject.Location = new System.Drawing.Point(12, 40);
            this.lblCurrentProject.Margin = new System.Windows.Forms.Padding(4);
            this.lblCurrentProject.Name = "lblCurrentProject";
            this.lblCurrentProject.Padding = new System.Windows.Forms.Padding(12, 0, 8, 0);
            this.lblCurrentProject.Size = new System.Drawing.Size(1654, 54);
            this.lblCurrentProject.TabIndex = 1;
            this.lblCurrentProject.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTipSettings.SetToolTip(this.lblCurrentProject, "선택 레시피와 현재 장비에 적용 중인 레시피를 구분합니다. 목록 선택만으로 장비 레시피가 바뀌지 않습니다.");
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 258F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpProjects, 0, 0);
            this.contentLayout.Controls.Add(this.tabsRecipe, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(12, 102);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(4);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1654, 722);
            this.contentLayout.TabIndex = 2;
            // 
            // grpProjects
            // 
            this.grpProjects.Controls.Add(this.leftLayout);
            this.grpProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProjects.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpProjects.Location = new System.Drawing.Point(4, 4);
            this.grpProjects.Margin = new System.Windows.Forms.Padding(4);
            this.grpProjects.Name = "grpProjects";
            this.grpProjects.Padding = new System.Windows.Forms.Padding(6);
            this.grpProjects.Size = new System.Drawing.Size(250, 714);
            this.grpProjects.TabIndex = 0;
            this.grpProjects.TabStop = false;
            this.grpProjects.Text = "레시피 목록";
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.listProjects, 0, 0);
            this.leftLayout.Controls.Add(this.listButtonLayout, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(6, 26);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(4);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.leftLayout.Size = new System.Drawing.Size(238, 682);
            this.leftLayout.TabIndex = 0;
            // 
            // listProjects
            // 
            this.listProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listProjects.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.listProjects.HorizontalScrollbar = true;
            this.listProjects.IntegralHeight = false;
            this.listProjects.ItemHeight = 20;
            this.listProjects.Location = new System.Drawing.Point(4, 4);
            this.listProjects.Margin = new System.Windows.Forms.Padding(4);
            this.listProjects.Name = "listProjects";
            this.listProjects.Size = new System.Drawing.Size(230, 624);
            this.listProjects.TabIndex = 0;
            this.listProjects.SelectedIndexChanged += new System.EventHandler(this.listProjects_SelectedIndexChanged);
            // 
            // listButtonLayout
            // 
            this.listButtonLayout.ColumnCount = 2;
            this.listButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listButtonLayout.Controls.Add(this.btnCopy, 0, 0);
            this.listButtonLayout.Controls.Add(this.btnManage, 1, 0);
            this.listButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listButtonLayout.Location = new System.Drawing.Point(4, 636);
            this.listButtonLayout.Margin = new System.Windows.Forms.Padding(4);
            this.listButtonLayout.Name = "listButtonLayout";
            this.listButtonLayout.RowCount = 1;
            this.listButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.listButtonLayout.Size = new System.Drawing.Size(230, 42);
            this.listButtonLayout.TabIndex = 1;
            // 
            // btnCopy
            // 
            this.btnCopy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.btnCopy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopy.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnCopy.ForeColor = System.Drawing.Color.Black;
            this.btnCopy.Location = new System.Drawing.Point(4, 4);
            this.btnCopy.Margin = new System.Windows.Forms.Padding(4);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(107, 34);
            this.btnCopy.TabIndex = 0;
            this.btnCopy.Text = "레시피 복사";
            this.toolTipSettings.SetToolTip(this.btnCopy, "현재 화면의 프로젝트 정보와 기존 장치별 레시피·맵 파일을 새 이름으로 복사합니다. 복사 후 장비 적용은 별도로 진행하세요.");
            this.btnCopy.UseVisualStyleBackColor = false;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // btnManage
            // 
            this.btnManage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.btnManage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnManage.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnManage.ForeColor = System.Drawing.Color.Black;
            this.btnManage.Location = new System.Drawing.Point(119, 4);
            this.btnManage.Margin = new System.Windows.Forms.Padding(4);
            this.btnManage.Name = "btnManage";
            this.btnManage.Size = new System.Drawing.Size(107, 34);
            this.btnManage.TabIndex = 1;
            this.btnManage.Text = "관리 ▾";
            this.toolTipSettings.SetToolTip(this.btnManage, "기본 생성, 삭제, 폴더 열기, 고급 XML 경로 기능을 모았습니다.");
            this.btnManage.UseVisualStyleBackColor = false;
            this.btnManage.Click += new System.EventHandler(this.btnManage_Click);
            // 
            // tabsRecipe
            // 
            this.tabsRecipe.Controls.Add(this.tabBasic);
            this.tabsRecipe.Controls.Add(this.tabDetails);
            this.tabsRecipe.Controls.Add(this.tabAdvanced);
            this.tabsRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsRecipe.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.tabsRecipe.ItemSize = new System.Drawing.Size(170, 38);
            this.tabsRecipe.Location = new System.Drawing.Point(262, 4);
            this.tabsRecipe.Margin = new System.Windows.Forms.Padding(4);
            this.tabsRecipe.Name = "tabsRecipe";
            this.tabsRecipe.SelectedIndex = 0;
            this.tabsRecipe.Size = new System.Drawing.Size(1388, 714);
            this.tabsRecipe.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabsRecipe.TabIndex = 1;
            // 
            // tabBasic
            // 
            this.tabBasic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.tabBasic.Controls.Add(this.centerLayout);
            this.tabBasic.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabBasic.Location = new System.Drawing.Point(4, 42);
            this.tabBasic.Margin = new System.Windows.Forms.Padding(4);
            this.tabBasic.Name = "tabBasic";
            this.tabBasic.Size = new System.Drawing.Size(1380, 668);
            this.tabBasic.TabIndex = 0;
            this.tabBasic.Text = "기본 확인";
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.projectInfoLayout, 0, 0);
            this.centerLayout.Controls.Add(this.mapSettingsLayout, 0, 1);
            this.centerLayout.Controls.Add(this.grpMapApproval, 0, 2);
            this.centerLayout.Controls.Add(this.btnBrowseMap, 0, 3);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(0, 0);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(4);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 4;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 155F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.centerLayout.Size = new System.Drawing.Size(1380, 668);
            this.centerLayout.TabIndex = 0;
            // 
            // projectInfoLayout
            // 
            this.projectInfoLayout.ColumnCount = 2;
            this.projectInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this.projectInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this.projectInfoLayout.Controls.Add(this.grpSummary, 0, 0);
            this.projectInfoLayout.Controls.Add(this.grpProjectOption, 1, 0);
            this.projectInfoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.projectInfoLayout.Location = new System.Drawing.Point(4, 4);
            this.projectInfoLayout.Margin = new System.Windows.Forms.Padding(4);
            this.projectInfoLayout.Name = "projectInfoLayout";
            this.projectInfoLayout.RowCount = 1;
            this.projectInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.projectInfoLayout.Size = new System.Drawing.Size(1372, 227);
            this.projectInfoLayout.TabIndex = 0;
            // 
            // grpSummary
            // 
            this.grpSummary.Controls.Add(this.gridSummary);
            this.grpSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSummary.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSummary.Location = new System.Drawing.Point(4, 4);
            this.grpSummary.Margin = new System.Windows.Forms.Padding(4);
            this.grpSummary.Name = "grpSummary";
            this.grpSummary.Padding = new System.Windows.Forms.Padding(6);
            this.grpSummary.Size = new System.Drawing.Size(623, 219);
            this.grpSummary.TabIndex = 0;
            this.grpSummary.TabStop = false;
            this.grpSummary.Text = "프로젝트 정보 · 편집 가능";
            this.toolTipSettings.SetToolTip(this.grpSummary, "맵 항목은 선택 레시피의 저장값을 읽기 전용으로 표시합니다. 항목명이나 값 위에 마우스를 올리면 변경 위치와 방법을 안내합니다.");
            // 
            // gridSummary
            // 
            this.gridSummary.AllowUserToAddRows = false;
            this.gridSummary.AllowUserToDeleteRows = false;
            this.gridSummary.AllowUserToResizeRows = false;
            this.gridSummary.BackgroundColor = System.Drawing.Color.White;
            this.gridSummary.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridSummary.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridSummary.DefaultCellStyle = dataGridViewCellStyle2;
            this.gridSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSummary.Location = new System.Drawing.Point(6, 26);
            this.gridSummary.Margin = new System.Windows.Forms.Padding(4);
            this.gridSummary.MultiSelect = false;
            this.gridSummary.Name = "gridSummary";
            this.gridSummary.RowHeadersVisible = false;
            this.gridSummary.RowTemplate.Height = 29;
            this.gridSummary.Size = new System.Drawing.Size(611, 187);
            this.gridSummary.TabIndex = 0;
            this.gridSummary.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridProjectEdit_CellValueChanged);
            // 
            // grpProjectOption
            // 
            this.grpProjectOption.Controls.Add(this.gridProject);
            this.grpProjectOption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProjectOption.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpProjectOption.Location = new System.Drawing.Point(635, 4);
            this.grpProjectOption.Margin = new System.Windows.Forms.Padding(4);
            this.grpProjectOption.Name = "grpProjectOption";
            this.grpProjectOption.Padding = new System.Windows.Forms.Padding(6);
            this.grpProjectOption.Size = new System.Drawing.Size(733, 219);
            this.grpProjectOption.TabIndex = 1;
            this.grpProjectOption.TabStop = false;
            this.grpProjectOption.Text = "다이 / 웨이퍼 사양 · 확인용";
            // 
            // gridProject
            // 
            this.gridProject.AllowUserToAddRows = false;
            this.gridProject.AllowUserToDeleteRows = false;
            this.gridProject.AllowUserToResizeRows = false;
            this.gridProject.BackgroundColor = System.Drawing.Color.White;
            this.gridProject.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridProject.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.gridProject.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(237)))), ((int)(((byte)(244)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridProject.DefaultCellStyle = dataGridViewCellStyle4;
            this.gridProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProject.Location = new System.Drawing.Point(6, 26);
            this.gridProject.Margin = new System.Windows.Forms.Padding(4);
            this.gridProject.MultiSelect = false;
            this.gridProject.Name = "gridProject";
            this.gridProject.ReadOnly = true;
            this.gridProject.RowHeadersVisible = false;
            this.gridProject.RowTemplate.Height = 29;
            this.gridProject.Size = new System.Drawing.Size(721, 187);
            this.gridProject.TabIndex = 0;
            // 
            // mapSettingsLayout
            // 
            this.mapSettingsLayout.ColumnCount = 2;
            this.mapSettingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mapSettingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mapSettingsLayout.Controls.Add(this.grpInputMap, 0, 0);
            this.mapSettingsLayout.Controls.Add(this.grpOutputMap, 1, 0);
            this.mapSettingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapSettingsLayout.Location = new System.Drawing.Point(4, 239);
            this.mapSettingsLayout.Margin = new System.Windows.Forms.Padding(4);
            this.mapSettingsLayout.Name = "mapSettingsLayout";
            this.mapSettingsLayout.RowCount = 1;
            this.mapSettingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapSettingsLayout.Size = new System.Drawing.Size(1372, 222);
            this.mapSettingsLayout.TabIndex = 1;
            // 
            // grpInputMap
            // 
            this.grpInputMap.Controls.Add(this.gridInputMap);
            this.grpInputMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInputMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInputMap.Location = new System.Drawing.Point(4, 4);
            this.grpInputMap.Margin = new System.Windows.Forms.Padding(4);
            this.grpInputMap.Name = "grpInputMap";
            this.grpInputMap.Padding = new System.Windows.Forms.Padding(6);
            this.grpInputMap.Size = new System.Drawing.Size(678, 214);
            this.grpInputMap.TabIndex = 0;
            this.grpInputMap.TabStop = false;
            this.grpInputMap.Text = "입력 맵 · 레시피 설정";
            // 
            // gridInputMap
            // 
            this.gridInputMap.AllowUserToAddRows = false;
            this.gridInputMap.AllowUserToDeleteRows = false;
            this.gridInputMap.AllowUserToResizeRows = false;
            this.gridInputMap.BackgroundColor = System.Drawing.Color.White;
            this.gridInputMap.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridInputMap.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.gridInputMap.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(237)))), ((int)(((byte)(244)))));
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridInputMap.DefaultCellStyle = dataGridViewCellStyle6;
            this.gridInputMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridInputMap.Location = new System.Drawing.Point(6, 26);
            this.gridInputMap.Margin = new System.Windows.Forms.Padding(4);
            this.gridInputMap.MultiSelect = false;
            this.gridInputMap.Name = "gridInputMap";
            this.gridInputMap.ReadOnly = true;
            this.gridInputMap.RowHeadersVisible = false;
            this.gridInputMap.RowTemplate.Height = 29;
            this.gridInputMap.Size = new System.Drawing.Size(666, 182);
            this.gridInputMap.TabIndex = 0;
            // 
            // grpOutputMap
            // 
            this.grpOutputMap.Controls.Add(this.gridOutputMap);
            this.grpOutputMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutputMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpOutputMap.Location = new System.Drawing.Point(690, 4);
            this.grpOutputMap.Margin = new System.Windows.Forms.Padding(4);
            this.grpOutputMap.Name = "grpOutputMap";
            this.grpOutputMap.Padding = new System.Windows.Forms.Padding(6);
            this.grpOutputMap.Size = new System.Drawing.Size(678, 214);
            this.grpOutputMap.TabIndex = 1;
            this.grpOutputMap.TabStop = false;
            this.grpOutputMap.Text = "출력 맵 · 레시피 설정";
            // 
            // gridOutputMap
            // 
            this.gridOutputMap.AllowUserToAddRows = false;
            this.gridOutputMap.AllowUserToDeleteRows = false;
            this.gridOutputMap.AllowUserToResizeRows = false;
            this.gridOutputMap.BackgroundColor = System.Drawing.Color.White;
            this.gridOutputMap.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridOutputMap.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.gridOutputMap.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle8.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(237)))), ((int)(((byte)(244)))));
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridOutputMap.DefaultCellStyle = dataGridViewCellStyle8;
            this.gridOutputMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridOutputMap.Location = new System.Drawing.Point(6, 26);
            this.gridOutputMap.Margin = new System.Windows.Forms.Padding(4);
            this.gridOutputMap.MultiSelect = false;
            this.gridOutputMap.Name = "gridOutputMap";
            this.gridOutputMap.ReadOnly = true;
            this.gridOutputMap.RowHeadersVisible = false;
            this.gridOutputMap.RowTemplate.Height = 29;
            this.gridOutputMap.Size = new System.Drawing.Size(666, 182);
            this.gridOutputMap.TabIndex = 0;
            // 
            // grpMapApproval
            // 
            this.grpMapApproval.Controls.Add(this.gridMapApproval);
            this.grpMapApproval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapApproval.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMapApproval.Location = new System.Drawing.Point(4, 469);
            this.grpMapApproval.Margin = new System.Windows.Forms.Padding(4);
            this.grpMapApproval.Name = "grpMapApproval";
            this.grpMapApproval.Padding = new System.Windows.Forms.Padding(6);
            this.grpMapApproval.Size = new System.Drawing.Size(1372, 147);
            this.grpMapApproval.TabIndex = 2;
            this.grpMapApproval.TabStop = false;
            this.grpMapApproval.Text = "등록 맵 확인 · 실제 운전 조건은 시작 시 별도 검사";
            this.toolTipSettings.SetToolTip(this.grpMapApproval, "저장된 등록 맵의 지원 여부·사양·승인을 확인합니다. 원격 입력 맵은 실제 수신 후 검사합니다. 자재·인터락·운전 준비 완료를 뜻하지 않습니다.");
            // 
            // gridMapApproval
            // 
            this.gridMapApproval.AllowUserToAddRows = false;
            this.gridMapApproval.AllowUserToDeleteRows = false;
            this.gridMapApproval.AllowUserToResizeRows = false;
            this.gridMapApproval.BackgroundColor = System.Drawing.Color.White;
            this.gridMapApproval.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridMapApproval.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.gridMapApproval.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle10.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(237)))), ((int)(((byte)(244)))));
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridMapApproval.DefaultCellStyle = dataGridViewCellStyle10;
            this.gridMapApproval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMapApproval.Location = new System.Drawing.Point(6, 26);
            this.gridMapApproval.Margin = new System.Windows.Forms.Padding(4);
            this.gridMapApproval.MultiSelect = false;
            this.gridMapApproval.Name = "gridMapApproval";
            this.gridMapApproval.ReadOnly = true;
            this.gridMapApproval.RowHeadersVisible = false;
            this.gridMapApproval.RowTemplate.Height = 28;
            this.gridMapApproval.Size = new System.Drawing.Size(1360, 115);
            this.gridMapApproval.TabIndex = 0;
            // 
            // btnBrowseMap
            // 
            this.btnBrowseMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(101)))), ((int)(((byte)(125)))));
            this.btnBrowseMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBrowseMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnBrowseMap.ForeColor = System.Drawing.Color.White;
            this.btnBrowseMap.Location = new System.Drawing.Point(4, 624);
            this.btnBrowseMap.Margin = new System.Windows.Forms.Padding(4);
            this.btnBrowseMap.Name = "btnBrowseMap";
            this.btnBrowseMap.Size = new System.Drawing.Size(1372, 40);
            this.btnBrowseMap.TabIndex = 3;
            this.btnBrowseMap.Text = "웨이퍼·맵 설정 열기";
            this.toolTipSettings.SetToolTip(this.btnBrowseMap, "현재 선택한 레시피와 장비 적용 레시피가 같을 때 웨이퍼 사양 화면을 엽니다. 이 버튼이 레시피를 자동 적용하지는 않습니다.");
            this.btnBrowseMap.UseVisualStyleBackColor = false;
            this.btnBrowseMap.Click += new System.EventHandler(this.btnBrowseMap_Click);
            // 
            // tabDetails
            // 
            this.tabDetails.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.tabDetails.Controls.Add(this.rightLayout);
            this.tabDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabDetails.Location = new System.Drawing.Point(4, 42);
            this.tabDetails.Margin = new System.Windows.Forms.Padding(4);
            this.tabDetails.Name = "tabDetails";
            this.tabDetails.Size = new System.Drawing.Size(0, 0);
            this.tabDetails.TabIndex = 1;
            this.tabDetails.Text = "파일·상태 상세";
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpMap, 0, 0);
            this.rightLayout.Controls.Add(this.grpStatus, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(0, 0);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(4);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.rightLayout.Size = new System.Drawing.Size(0, 0);
            this.rightLayout.TabIndex = 0;
            // 
            // grpMap
            // 
            this.grpMap.Controls.Add(this.gridMap);
            this.grpMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMap.Location = new System.Drawing.Point(4, 4);
            this.grpMap.Margin = new System.Windows.Forms.Padding(4);
            this.grpMap.Name = "grpMap";
            this.grpMap.Padding = new System.Windows.Forms.Padding(6);
            this.grpMap.Size = new System.Drawing.Size(1, 1);
            this.grpMap.TabIndex = 0;
            this.grpMap.TabStop = false;
            this.grpMap.Text = "기준 맵 / 공정 맵 파일";
            // 
            // gridMap
            // 
            this.gridMap.AllowUserToAddRows = false;
            this.gridMap.AllowUserToDeleteRows = false;
            this.gridMap.AllowUserToResizeRows = false;
            this.gridMap.BackgroundColor = System.Drawing.Color.White;
            this.gridMap.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridMap.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle11;
            this.gridMap.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle12.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridMap.DefaultCellStyle = dataGridViewCellStyle12;
            this.gridMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMap.Location = new System.Drawing.Point(6, 26);
            this.gridMap.Margin = new System.Windows.Forms.Padding(4);
            this.gridMap.MultiSelect = false;
            this.gridMap.Name = "gridMap";
            this.gridMap.ReadOnly = true;
            this.gridMap.RowHeadersVisible = false;
            this.gridMap.RowTemplate.Height = 28;
            this.gridMap.Size = new System.Drawing.Size(0, 0);
            this.gridMap.TabIndex = 0;
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.gridStatus);
            this.grpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStatus.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpStatus.Location = new System.Drawing.Point(4, 4);
            this.grpStatus.Margin = new System.Windows.Forms.Padding(4);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Padding = new System.Windows.Forms.Padding(6);
            this.grpStatus.Size = new System.Drawing.Size(1, 1);
            this.grpStatus.TabIndex = 1;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "레시피 파일 및 사양 점검";
            // 
            // gridStatus
            // 
            this.gridStatus.AllowUserToAddRows = false;
            this.gridStatus.AllowUserToDeleteRows = false;
            this.gridStatus.AllowUserToResizeRows = false;
            this.gridStatus.BackgroundColor = System.Drawing.Color.White;
            this.gridStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle13.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle13.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridStatus.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle13;
            this.gridStatus.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle14.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle14.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle14.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridStatus.DefaultCellStyle = dataGridViewCellStyle14;
            this.gridStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridStatus.Location = new System.Drawing.Point(6, 26);
            this.gridStatus.Margin = new System.Windows.Forms.Padding(4);
            this.gridStatus.MultiSelect = false;
            this.gridStatus.Name = "gridStatus";
            this.gridStatus.ReadOnly = true;
            this.gridStatus.RowHeadersVisible = false;
            this.gridStatus.RowTemplate.Height = 28;
            this.gridStatus.Size = new System.Drawing.Size(0, 0);
            this.gridStatus.TabIndex = 0;
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.tabAdvanced.Controls.Add(this.advancedLayout);
            this.tabAdvanced.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabAdvanced.Location = new System.Drawing.Point(4, 42);
            this.tabAdvanced.Margin = new System.Windows.Forms.Padding(4);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Size = new System.Drawing.Size(0, 0);
            this.tabAdvanced.TabIndex = 2;
            this.tabAdvanced.Text = "고급 설정";
            // 
            // advancedLayout
            // 
            this.advancedLayout.ColumnCount = 2;
            this.advancedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.advancedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.advancedLayout.Controls.Add(this.grpGlobal, 0, 0);
            this.advancedLayout.Controls.Add(this.grpXml, 1, 0);
            this.advancedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedLayout.Location = new System.Drawing.Point(0, 0);
            this.advancedLayout.Margin = new System.Windows.Forms.Padding(4);
            this.advancedLayout.Name = "advancedLayout";
            this.advancedLayout.RowCount = 1;
            this.advancedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.advancedLayout.Size = new System.Drawing.Size(0, 0);
            this.advancedLayout.TabIndex = 0;
            // 
            // grpGlobal
            // 
            this.grpGlobal.Controls.Add(this.gridGlobal);
            this.grpGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGlobal.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpGlobal.Location = new System.Drawing.Point(4, 4);
            this.grpGlobal.Margin = new System.Windows.Forms.Padding(4);
            this.grpGlobal.Name = "grpGlobal";
            this.grpGlobal.Padding = new System.Windows.Forms.Padding(6);
            this.grpGlobal.Size = new System.Drawing.Size(1, 1);
            this.grpGlobal.TabIndex = 0;
            this.grpGlobal.TabStop = false;
            this.grpGlobal.Text = "기타 / 고급 설정";
            // 
            // gridGlobal
            // 
            this.gridGlobal.AllowUserToAddRows = false;
            this.gridGlobal.AllowUserToDeleteRows = false;
            this.gridGlobal.AllowUserToResizeRows = false;
            this.gridGlobal.BackgroundColor = System.Drawing.Color.White;
            this.gridGlobal.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle15.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle15.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle15.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle15.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle15.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridGlobal.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle15;
            this.gridGlobal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle16.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle16.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle16.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle16.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle16.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle16.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle16.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridGlobal.DefaultCellStyle = dataGridViewCellStyle16;
            this.gridGlobal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridGlobal.Location = new System.Drawing.Point(6, 26);
            this.gridGlobal.Margin = new System.Windows.Forms.Padding(4);
            this.gridGlobal.MultiSelect = false;
            this.gridGlobal.Name = "gridGlobal";
            this.gridGlobal.RowHeadersVisible = false;
            this.gridGlobal.RowTemplate.Height = 28;
            this.gridGlobal.Size = new System.Drawing.Size(0, 0);
            this.gridGlobal.TabIndex = 0;
            this.gridGlobal.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridProjectEdit_CellValueChanged);
            // 
            // grpXml
            // 
            this.grpXml.Controls.Add(this.gridXml);
            this.grpXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpXml.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpXml.Location = new System.Drawing.Point(4, 4);
            this.grpXml.Margin = new System.Windows.Forms.Padding(4);
            this.grpXml.Name = "grpXml";
            this.grpXml.Padding = new System.Windows.Forms.Padding(6);
            this.grpXml.Size = new System.Drawing.Size(1, 1);
            this.grpXml.TabIndex = 1;
            this.grpXml.TabStop = false;
            this.grpXml.Text = "XML 추적 정보 · 고급";
            // 
            // gridXml
            // 
            this.gridXml.AllowUserToAddRows = false;
            this.gridXml.AllowUserToDeleteRows = false;
            this.gridXml.AllowUserToResizeRows = false;
            this.gridXml.BackgroundColor = System.Drawing.Color.White;
            this.gridXml.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle17.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle17.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle17.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle17.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle17.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle17.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridXml.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle17;
            this.gridXml.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle18.Font = new System.Drawing.Font("맑은 고딕", 10F);
            dataGridViewCellStyle18.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle18.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dataGridViewCellStyle18.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle18.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle18.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridXml.DefaultCellStyle = dataGridViewCellStyle18;
            this.gridXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridXml.Location = new System.Drawing.Point(6, 26);
            this.gridXml.Margin = new System.Windows.Forms.Padding(4);
            this.gridXml.MultiSelect = false;
            this.gridXml.Name = "gridXml";
            this.gridXml.RowHeadersVisible = false;
            this.gridXml.RowTemplate.Height = 28;
            this.gridXml.Size = new System.Drawing.Size(0, 0);
            this.gridXml.TabIndex = 0;
            this.gridXml.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridProjectEdit_CellValueChanged);
            // 
            // footerLayout
            // 
            this.footerLayout.ColumnCount = 4;
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.footerLayout.Controls.Add(this.lblFooterHint, 0, 0);
            this.footerLayout.Controls.Add(this.btnReload, 1, 0);
            this.footerLayout.Controls.Add(this.btnSaveRecipe, 2, 0);
            this.footerLayout.Controls.Add(this.btnApplyCurrent, 3, 0);
            this.footerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerLayout.Location = new System.Drawing.Point(12, 832);
            this.footerLayout.Margin = new System.Windows.Forms.Padding(4);
            this.footerLayout.Name = "footerLayout";
            this.footerLayout.RowCount = 1;
            this.footerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.Size = new System.Drawing.Size(1654, 56);
            this.footerLayout.TabIndex = 3;
            // 
            // lblFooterHint
            // 
            this.lblFooterHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFooterHint.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblFooterHint.Location = new System.Drawing.Point(4, 4);
            this.lblFooterHint.Margin = new System.Windows.Forms.Padding(4);
            this.lblFooterHint.Name = "lblFooterHint";
            this.lblFooterHint.Size = new System.Drawing.Size(1056, 48);
            this.lblFooterHint.TabIndex = 0;
            this.lblFooterHint.Text = "프로젝트 저장과 장비 적용은 별도입니다.";
            this.lblFooterHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.Black;
            this.btnReload.Location = new System.Drawing.Point(1068, 4);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(132, 48);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = "새로고침";
            this.toolTipSettings.SetToolTip(this.btnReload, "선택 레시피의 저장값을 다시 불러옵니다. 이 화면에서 저장하지 않은 편집값은 사라집니다.");
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnSaveRecipe
            // 
            this.btnSaveRecipe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(79)))), ((int)(((byte)(96)))));
            this.btnSaveRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveRecipe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveRecipe.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnSaveRecipe.ForeColor = System.Drawing.Color.White;
            this.btnSaveRecipe.Location = new System.Drawing.Point(1208, 4);
            this.btnSaveRecipe.Margin = new System.Windows.Forms.Padding(4);
            this.btnSaveRecipe.Name = "btnSaveRecipe";
            this.btnSaveRecipe.Size = new System.Drawing.Size(192, 48);
            this.btnSaveRecipe.TabIndex = 2;
            this.btnSaveRecipe.Text = "프로젝트 정보 저장";
            this.toolTipSettings.SetToolTip(this.btnSaveRecipe, "이 화면에서 편집한 프로젝트 정보를 파일에 저장합니다. 장비 적용은 [선택 레시피 장비 적용]에서 진행합니다.");
            this.btnSaveRecipe.UseVisualStyleBackColor = false;
            this.btnSaveRecipe.Click += new System.EventHandler(this.btnSaveRecipe_Click);
            // 
            // btnApplyCurrent
            // 
            this.btnApplyCurrent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(86)))), ((int)(((byte)(24)))));
            this.btnApplyCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyCurrent.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnApplyCurrent.ForeColor = System.Drawing.Color.White;
            this.btnApplyCurrent.Location = new System.Drawing.Point(1408, 4);
            this.btnApplyCurrent.Margin = new System.Windows.Forms.Padding(4);
            this.btnApplyCurrent.Name = "btnApplyCurrent";
            this.btnApplyCurrent.Size = new System.Drawing.Size(242, 48);
            this.btnApplyCurrent.TabIndex = 3;
            this.btnApplyCurrent.Text = "선택 레시피 장비 적용";
            this.toolTipSettings.SetToolTip(this.btnApplyCurrent, "선택 레시피를 검증한 뒤 장비에 적용하고 저장합니다. 다른 레시피로 변경할 때는 기존 자재 초기화 확인과 인터락을 따릅니다.");
            this.btnApplyCurrent.UseVisualStyleBackColor = false;
            this.btnApplyCurrent.Click += new System.EventHandler(this.btnApplyCurrent_Click);
            // 
            // menuManagement
            // 
            this.menuManagement.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuNewRecipe,
            this.menuDeleteRecipe,
            this.menuFolderSeparator,
            this.menuProjectFolder,
            this.menuRecipeFolder,
            this.menuMapFolder,
            this.menuAdvancedSeparator,
            this.menuXmlPath});
            this.menuManagement.Name = "menuManagement";
            this.menuManagement.Size = new System.Drawing.Size(195, 126);
            this.menuManagement.Opening += new System.ComponentModel.CancelEventHandler(this.menuManagement_Opening);
            // 
            // menuNewRecipe
            // 
            this.menuNewRecipe.Name = "menuNewRecipe";
            this.menuNewRecipe.Size = new System.Drawing.Size(194, 22);
            this.menuNewRecipe.Text = "기본 레시피 생성…";
            this.menuNewRecipe.Click += new System.EventHandler(this.menuNewRecipe_Click);
            // 
            // menuDeleteRecipe
            // 
            this.menuDeleteRecipe.Name = "menuDeleteRecipe";
            this.menuDeleteRecipe.Size = new System.Drawing.Size(194, 22);
            this.menuDeleteRecipe.Text = "선택 레시피 삭제…";
            this.menuDeleteRecipe.Click += new System.EventHandler(this.menuDeleteRecipe_Click);
            // 
            // menuFolderSeparator
            // 
            this.menuFolderSeparator.Name = "menuFolderSeparator";
            this.menuFolderSeparator.Size = new System.Drawing.Size(191, 6);
            // 
            // menuProjectFolder
            // 
            this.menuProjectFolder.Name = "menuProjectFolder";
            this.menuProjectFolder.Size = new System.Drawing.Size(194, 22);
            this.menuProjectFolder.Text = "전체 레시피 폴더 열기";
            this.menuProjectFolder.Click += new System.EventHandler(this.menuProjectFolder_Click);
            // 
            // menuRecipeFolder
            // 
            this.menuRecipeFolder.Name = "menuRecipeFolder";
            this.menuRecipeFolder.Size = new System.Drawing.Size(194, 22);
            this.menuRecipeFolder.Text = "선택 레시피 폴더 열기";
            this.menuRecipeFolder.Click += new System.EventHandler(this.menuRecipeFolder_Click);
            // 
            // menuMapFolder
            // 
            this.menuMapFolder.Name = "menuMapFolder";
            this.menuMapFolder.Size = new System.Drawing.Size(194, 22);
            this.menuMapFolder.Text = "선택 맵 폴더 열기";
            this.menuMapFolder.Click += new System.EventHandler(this.menuMapFolder_Click);
            // 
            // menuAdvancedSeparator
            // 
            this.menuAdvancedSeparator.Name = "menuAdvancedSeparator";
            this.menuAdvancedSeparator.Size = new System.Drawing.Size(191, 6);
            // 
            // menuXmlPath
            // 
            this.menuXmlPath.Name = "menuXmlPath";
            this.menuXmlPath.Size = new System.Drawing.Size(32, 19);
            this.menuXmlPath.Text = "고급: XML 저장 경로 선택…";
            this.menuXmlPath.Click += new System.EventHandler(this.menuXmlPath_Click);
            // 
            // timerSettingsMonitor
            // 
            this.timerSettingsMonitor.Interval = 2000;
            this.timerSettingsMonitor.Tick += new System.EventHandler(this.timerSettingsMonitor_Tick);
            // 
            // toolTipSettings
            // 
            this.toolTipSettings.AutoPopDelay = 20000;
            this.toolTipSettings.InitialDelay = 500;
            this.toolTipSettings.ReshowDelay = 100;
            this.toolTipSettings.ShowAlways = true;
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
            this.tabsRecipe.ResumeLayout(false);
            this.tabBasic.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.projectInfoLayout.ResumeLayout(false);
            this.grpSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSummary)).EndInit();
            this.grpProjectOption.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridProject)).EndInit();
            this.mapSettingsLayout.ResumeLayout(false);
            this.grpInputMap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridInputMap)).EndInit();
            this.grpOutputMap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridOutputMap)).EndInit();
            this.grpMapApproval.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMapApproval)).EndInit();
            this.tabDetails.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpMap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMap)).EndInit();
            this.grpStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridStatus)).EndInit();
            this.tabAdvanced.ResumeLayout(false);
            this.advancedLayout.ResumeLayout(false);
            this.grpGlobal.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridGlobal)).EndInit();
            this.grpXml.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridXml)).EndInit();
            this.footerLayout.ResumeLayout(false);
            this.menuManagement.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
