using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class RecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _body;          // 3열: 콘텐츠(에디터) / Finder 레일 / 모듈 레일(우측)
        private TableLayoutPanel _contentHost;   // 콘텐츠 col: 브레드크럼 + 에디터 스왑
        private Panel            _hdrPanel;       // 브레드크럼(상단 SAVE 제거)
        private Label            _hdr;
        private Panel            _content;        // 에디터 스왑 영역

        private Panel            _finderRail;     // 우측 1: 현 모듈의 finder/inspector 세로 목록 + 저장
        private Label            _setHdr;
        private FlowLayoutPanel  _setFlow;
        private Panel            _recipeBar;
        private Button           _btnSaveRecipe;  // 레시피 저장(품목별) — Finder 레일 하단

        private Panel            _sidebar;        // 우측 2(고정): 모듈(검사 알고리즘) 평면 선택
        private Label            _sideHdr;
        private FlowLayoutPanel  _sideFlow;

        // 좌측: 레시피(품목) 리스트 (핸들러 ProjectPage 미러)
        private Panel           _projRail;
        private Panel           _projLeft, _projRight;   // 2분할: 좌 목록 / 우 공통설정
        private Label           _projHdr;
        private ListBox         _projList;
        private FlowLayoutPanel _projBtnBar;
        private Button          _btnRecApply, _btnRecNew, _btnRecCopy, _btnRecSaveAs, _btnRecDelete;
        private Label           _commonHdr;
        private QMC.Vision.Ui.Controls.ParameterGridControl _commonGrid;
        private Panel           _commonBar;
        private Button          _btnCommonSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._body = new System.Windows.Forms.TableLayoutPanel();
            this._contentHost = new System.Windows.Forms.TableLayoutPanel();
            this._hdrPanel = new System.Windows.Forms.Panel();
            this._hdr = new System.Windows.Forms.Label();
            this._content = new System.Windows.Forms.Panel();
            this._projRail = new System.Windows.Forms.Panel();
            this._projRight = new System.Windows.Forms.Panel();
            this._commonGrid = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._commonBar = new System.Windows.Forms.Panel();
            this._btnCommonSave = new System.Windows.Forms.Button();
            this._commonHdr = new System.Windows.Forms.Label();
            this._projLeft = new System.Windows.Forms.Panel();
            this._projList = new System.Windows.Forms.ListBox();
            this._projBtnBar = new System.Windows.Forms.FlowLayoutPanel();
            this._btnRecApply = new System.Windows.Forms.Button();
            this._btnRecNew = new System.Windows.Forms.Button();
            this._btnRecCopy = new System.Windows.Forms.Button();
            this._btnRecSaveAs = new System.Windows.Forms.Button();
            this._btnRecDelete = new System.Windows.Forms.Button();
            this._projHdr = new System.Windows.Forms.Label();
            this._finderRail = new System.Windows.Forms.Panel();
            this._setFlow = new System.Windows.Forms.FlowLayoutPanel();
            this._recipeBar = new System.Windows.Forms.Panel();
            this._btnSaveRecipe = new System.Windows.Forms.Button();
            this._setHdr = new System.Windows.Forms.Label();
            this._sidebar = new System.Windows.Forms.Panel();
            this._sideFlow = new System.Windows.Forms.FlowLayoutPanel();
            this._sideHdr = new System.Windows.Forms.Label();
            this._root.SuspendLayout();
            this._body.SuspendLayout();
            this._contentHost.SuspendLayout();
            this._hdrPanel.SuspendLayout();
            this._content.SuspendLayout();
            this._projRail.SuspendLayout();
            this._projRight.SuspendLayout();
            this._commonBar.SuspendLayout();
            this._projLeft.SuspendLayout();
            this._projBtnBar.SuspendLayout();
            this._finderRail.SuspendLayout();
            this._recipeBar.SuspendLayout();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();
            // 
            // _root
            // 
            this._root.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Controls.Add(this._body, 0, 0);
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Location = new System.Drawing.Point(0, 0);
            this._root.Margin = new System.Windows.Forms.Padding(0);
            this._root.Name = "_root";
            this._root.RowCount = 1;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Size = new System.Drawing.Size(1091, 696);
            this._root.TabIndex = 0;
            // 
            // _body
            // 
            this._body.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._body.ColumnCount = 3;
            this._body.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._body.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this._body.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this._body.Controls.Add(this._contentHost, 0, 0);
            this._body.Controls.Add(this._finderRail, 1, 0);
            this._body.Controls.Add(this._sidebar, 2, 0);
            this._body.Dock = System.Windows.Forms.DockStyle.Fill;
            this._body.Location = new System.Drawing.Point(0, 0);
            this._body.Margin = new System.Windows.Forms.Padding(0);
            this._body.Name = "_body";
            this._body.RowCount = 1;
            this._body.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._body.Size = new System.Drawing.Size(1091, 696);
            this._body.TabIndex = 0;
            // 
            // _contentHost
            // 
            this._contentHost.ColumnCount = 1;
            this._contentHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._contentHost.Controls.Add(this._hdrPanel, 0, 0);
            this._contentHost.Controls.Add(this._content, 0, 1);
            this._contentHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._contentHost.Location = new System.Drawing.Point(0, 0);
            this._contentHost.Margin = new System.Windows.Forms.Padding(0);
            this._contentHost.Name = "_contentHost";
            this._contentHost.RowCount = 2;
            this._contentHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._contentHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._contentHost.Size = new System.Drawing.Size(681, 696);
            this._contentHost.TabIndex = 0;
            // 
            // _hdrPanel
            // 
            this._hdrPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdrPanel.Controls.Add(this._hdr);
            this._hdrPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdrPanel.Location = new System.Drawing.Point(0, 0);
            this._hdrPanel.Margin = new System.Windows.Forms.Padding(0);
            this._hdrPanel.Name = "_hdrPanel";
            this._hdrPanel.Size = new System.Drawing.Size(681, 40);
            this._hdrPanel.TabIndex = 0;
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(0, 0);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(681, 40);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "Recipe";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _content
            // 
            this._content.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._content.Controls.Add(this._projRail);
            this._content.Dock = System.Windows.Forms.DockStyle.Fill;
            this._content.Location = new System.Drawing.Point(0, 40);
            this._content.Margin = new System.Windows.Forms.Padding(0);
            this._content.Name = "_content";
            this._content.Size = new System.Drawing.Size(681, 656);
            this._content.TabIndex = 1;
            // 
            // _projRail
            // 
            this._projRail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._projRail.Controls.Add(this._projRight);
            this._projRail.Controls.Add(this._projLeft);
            this._projRail.Dock = System.Windows.Forms.DockStyle.Fill;
            this._projRail.Location = new System.Drawing.Point(0, 0);
            this._projRail.Margin = new System.Windows.Forms.Padding(0);
            this._projRail.Name = "_projRail";
            this._projRail.Size = new System.Drawing.Size(681, 656);
            this._projRail.TabIndex = 0;
            this._projRail.Visible = false;
            // 
            // _projRight
            // 
            this._projRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._projRight.Controls.Add(this._commonGrid);
            this._projRight.Controls.Add(this._commonBar);
            this._projRight.Controls.Add(this._commonHdr);
            this._projRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this._projRight.Location = new System.Drawing.Point(280, 0);
            this._projRight.Name = "_projRight";
            this._projRight.Size = new System.Drawing.Size(401, 656);
            this._projRight.TabIndex = 0;
            // 
            // _commonGrid
            // 
            this._commonGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._commonGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._commonGrid.Location = new System.Drawing.Point(0, 30);
            this._commonGrid.Margin = new System.Windows.Forms.Padding(0);
            this._commonGrid.Name = "_commonGrid";
            this._commonGrid.Size = new System.Drawing.Size(401, 580);
            this._commonGrid.TabIndex = 0;
            // 
            // _commonBar
            // 
            this._commonBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._commonBar.Controls.Add(this._btnCommonSave);
            this._commonBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._commonBar.Location = new System.Drawing.Point(0, 610);
            this._commonBar.Name = "_commonBar";
            this._commonBar.Padding = new System.Windows.Forms.Padding(8);
            this._commonBar.Size = new System.Drawing.Size(401, 46);
            this._commonBar.TabIndex = 1;
            // 
            // _btnCommonSave
            // 
            this._btnCommonSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnCommonSave.Dock = System.Windows.Forms.DockStyle.Right;
            this._btnCommonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCommonSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnCommonSave.ForeColor = System.Drawing.Color.White;
            this._btnCommonSave.Location = new System.Drawing.Point(233, 8);
            this._btnCommonSave.Name = "_btnCommonSave";
            this._btnCommonSave.Size = new System.Drawing.Size(160, 30);
            this._btnCommonSave.TabIndex = 0;
            this._btnCommonSave.Text = "공통 저장 (품목)";
            this._btnCommonSave.UseVisualStyleBackColor = false;
            this._btnCommonSave.Click += new System.EventHandler(this.OnCommonSaveClick);
            // 
            // _commonHdr
            // 
            this._commonHdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._commonHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._commonHdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._commonHdr.ForeColor = System.Drawing.Color.White;
            this._commonHdr.Location = new System.Drawing.Point(0, 0);
            this._commonHdr.Name = "_commonHdr";
            this._commonHdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._commonHdr.Size = new System.Drawing.Size(401, 30);
            this._commonHdr.TabIndex = 2;
            this._commonHdr.Text = "공통 설정 (품목)";
            this._commonHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _projLeft
            // 
            this._projLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._projLeft.Controls.Add(this._projList);
            this._projLeft.Controls.Add(this._projBtnBar);
            this._projLeft.Controls.Add(this._projHdr);
            this._projLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this._projLeft.Location = new System.Drawing.Point(0, 0);
            this._projLeft.Name = "_projLeft";
            this._projLeft.Size = new System.Drawing.Size(280, 656);
            this._projLeft.TabIndex = 1;
            // 
            // _projList
            // 
            this._projList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._projList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._projList.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._projList.IntegralHeight = false;
            this._projList.ItemHeight = 17;
            this._projList.Location = new System.Drawing.Point(0, 44);
            this._projList.Name = "_projList";
            this._projList.Size = new System.Drawing.Size(280, 532);
            this._projList.TabIndex = 0;
            // 
            // _projBtnBar
            // 
            this._projBtnBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._projBtnBar.Controls.Add(this._btnRecApply);
            this._projBtnBar.Controls.Add(this._btnRecNew);
            this._projBtnBar.Controls.Add(this._btnRecCopy);
            this._projBtnBar.Controls.Add(this._btnRecSaveAs);
            this._projBtnBar.Controls.Add(this._btnRecDelete);
            this._projBtnBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._projBtnBar.Location = new System.Drawing.Point(0, 576);
            this._projBtnBar.Name = "_projBtnBar";
            this._projBtnBar.Padding = new System.Windows.Forms.Padding(6);
            this._projBtnBar.Size = new System.Drawing.Size(280, 80);
            this._projBtnBar.TabIndex = 1;
            // 
            // _btnRecApply
            // 
            this._btnRecApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnRecApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecApply.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnRecApply.ForeColor = System.Drawing.Color.White;
            this._btnRecApply.Location = new System.Drawing.Point(8, 8);
            this._btnRecApply.Margin = new System.Windows.Forms.Padding(2);
            this._btnRecApply.Name = "_btnRecApply";
            this._btnRecApply.Size = new System.Drawing.Size(85, 30);
            this._btnRecApply.TabIndex = 0;
            this._btnRecApply.Text = "적용";
            this._btnRecApply.UseVisualStyleBackColor = false;
            this._btnRecApply.Click += new System.EventHandler(this.OnRecipeApplyClick);
            // 
            // _btnRecNew
            // 
            this._btnRecNew.BackColor = System.Drawing.Color.White;
            this._btnRecNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecNew.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._btnRecNew.Location = new System.Drawing.Point(97, 8);
            this._btnRecNew.Margin = new System.Windows.Forms.Padding(2);
            this._btnRecNew.Name = "_btnRecNew";
            this._btnRecNew.Size = new System.Drawing.Size(85, 30);
            this._btnRecNew.TabIndex = 1;
            this._btnRecNew.Text = "New";
            this._btnRecNew.UseVisualStyleBackColor = false;
            this._btnRecNew.Click += new System.EventHandler(this.OnRecipeNewClick);
            // 
            // _btnRecCopy
            // 
            this._btnRecCopy.BackColor = System.Drawing.Color.White;
            this._btnRecCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecCopy.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._btnRecCopy.Location = new System.Drawing.Point(186, 8);
            this._btnRecCopy.Margin = new System.Windows.Forms.Padding(2);
            this._btnRecCopy.Name = "_btnRecCopy";
            this._btnRecCopy.Size = new System.Drawing.Size(85, 30);
            this._btnRecCopy.TabIndex = 2;
            this._btnRecCopy.Text = "Copy";
            this._btnRecCopy.UseVisualStyleBackColor = false;
            this._btnRecCopy.Click += new System.EventHandler(this.OnRecipeCopyClick);
            // 
            // _btnRecSaveAs
            // 
            this._btnRecSaveAs.BackColor = System.Drawing.Color.White;
            this._btnRecSaveAs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecSaveAs.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._btnRecSaveAs.Location = new System.Drawing.Point(8, 42);
            this._btnRecSaveAs.Margin = new System.Windows.Forms.Padding(2);
            this._btnRecSaveAs.Name = "_btnRecSaveAs";
            this._btnRecSaveAs.Size = new System.Drawing.Size(85, 30);
            this._btnRecSaveAs.TabIndex = 3;
            this._btnRecSaveAs.Text = "Save As";
            this._btnRecSaveAs.UseVisualStyleBackColor = false;
            this._btnRecSaveAs.Click += new System.EventHandler(this.OnRecipeSaveAsClick);
            // 
            // _btnRecDelete
            // 
            this._btnRecDelete.BackColor = System.Drawing.Color.White;
            this._btnRecDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRecDelete.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._btnRecDelete.Location = new System.Drawing.Point(97, 42);
            this._btnRecDelete.Margin = new System.Windows.Forms.Padding(2);
            this._btnRecDelete.Name = "_btnRecDelete";
            this._btnRecDelete.Size = new System.Drawing.Size(85, 30);
            this._btnRecDelete.TabIndex = 4;
            this._btnRecDelete.Text = "삭제";
            this._btnRecDelete.UseVisualStyleBackColor = false;
            this._btnRecDelete.Click += new System.EventHandler(this.OnRecipeDeleteClick);
            // 
            // _projHdr
            // 
            this._projHdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._projHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._projHdr.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._projHdr.ForeColor = System.Drawing.Color.Black;
            this._projHdr.Location = new System.Drawing.Point(0, 0);
            this._projHdr.Name = "_projHdr";
            this._projHdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._projHdr.Size = new System.Drawing.Size(280, 44);
            this._projHdr.TabIndex = 2;
            this._projHdr.Text = "레시피 (품목)";
            this._projHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _finderRail
            // 
            this._finderRail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._finderRail.Controls.Add(this._setFlow);
            this._finderRail.Controls.Add(this._recipeBar);
            this._finderRail.Controls.Add(this._setHdr);
            this._finderRail.Dock = System.Windows.Forms.DockStyle.Fill;
            this._finderRail.Location = new System.Drawing.Point(681, 0);
            this._finderRail.Margin = new System.Windows.Forms.Padding(0);
            this._finderRail.Name = "_finderRail";
            this._finderRail.Size = new System.Drawing.Size(200, 696);
            this._finderRail.TabIndex = 1;
            // 
            // _setFlow
            // 
            this._setFlow.AutoScroll = true;
            this._setFlow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._setFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._setFlow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._setFlow.Location = new System.Drawing.Point(0, 44);
            this._setFlow.Name = "_setFlow";
            this._setFlow.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this._setFlow.Size = new System.Drawing.Size(200, 602);
            this._setFlow.TabIndex = 0;
            this._setFlow.WrapContents = false;
            // 
            // _recipeBar
            // 
            this._recipeBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._recipeBar.Controls.Add(this._btnSaveRecipe);
            this._recipeBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._recipeBar.Location = new System.Drawing.Point(0, 646);
            this._recipeBar.Name = "_recipeBar";
            this._recipeBar.Padding = new System.Windows.Forms.Padding(6, 6, 6, 8);
            this._recipeBar.Size = new System.Drawing.Size(200, 50);
            this._recipeBar.TabIndex = 1;
            // 
            // _btnSaveRecipe
            // 
            this._btnSaveRecipe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSaveRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSaveRecipe.FlatAppearance.BorderSize = 0;
            this._btnSaveRecipe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSaveRecipe.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._btnSaveRecipe.ForeColor = System.Drawing.Color.White;
            this._btnSaveRecipe.Location = new System.Drawing.Point(6, 6);
            this._btnSaveRecipe.Name = "_btnSaveRecipe";
            this._btnSaveRecipe.Size = new System.Drawing.Size(188, 36);
            this._btnSaveRecipe.TabIndex = 0;
            this._btnSaveRecipe.Text = "레시피 저장 (품목별)";
            this._btnSaveRecipe.UseVisualStyleBackColor = false;
            this._btnSaveRecipe.Click += new System.EventHandler(this.OnSaveRecipeClick);
            // 
            // _setHdr
            // 
            this._setHdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._setHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._setHdr.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._setHdr.ForeColor = System.Drawing.Color.Black;
            this._setHdr.Location = new System.Drawing.Point(0, 0);
            this._setHdr.Name = "_setHdr";
            this._setHdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._setHdr.Size = new System.Drawing.Size(200, 44);
            this._setHdr.TabIndex = 2;
            this._setHdr.Text = "Finder / Inspector";
            this._setHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _sidebar
            // 
            this._sidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._sidebar.Controls.Add(this._sideFlow);
            this._sidebar.Controls.Add(this._sideHdr);
            this._sidebar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._sidebar.Location = new System.Drawing.Point(881, 0);
            this._sidebar.Margin = new System.Windows.Forms.Padding(0);
            this._sidebar.Name = "_sidebar";
            this._sidebar.Size = new System.Drawing.Size(210, 696);
            this._sidebar.TabIndex = 2;
            // 
            // _sideFlow
            // 
            this._sideFlow.AutoScroll = true;
            this._sideFlow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._sideFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._sideFlow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._sideFlow.Location = new System.Drawing.Point(0, 44);
            this._sideFlow.Name = "_sideFlow";
            this._sideFlow.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this._sideFlow.Size = new System.Drawing.Size(210, 652);
            this._sideFlow.TabIndex = 0;
            this._sideFlow.WrapContents = false;
            // 
            // _sideHdr
            // 
            this._sideHdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._sideHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._sideHdr.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._sideHdr.ForeColor = System.Drawing.Color.Black;
            this._sideHdr.Location = new System.Drawing.Point(0, 0);
            this._sideHdr.Name = "_sideHdr";
            this._sideHdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._sideHdr.Size = new System.Drawing.Size(210, 44);
            this._sideHdr.TabIndex = 1;
            this._sideHdr.Text = "모듈 (Module)";
            this._sideHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // RecipePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._root);
            this.Name = "RecipePage";
            this.Size = new System.Drawing.Size(1091, 696);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this._root.ResumeLayout(false);
            this._body.ResumeLayout(false);
            this._contentHost.ResumeLayout(false);
            this._hdrPanel.ResumeLayout(false);
            this._content.ResumeLayout(false);
            this._projRail.ResumeLayout(false);
            this._projRight.ResumeLayout(false);
            this._commonBar.ResumeLayout(false);
            this._projLeft.ResumeLayout(false);
            this._projBtnBar.ResumeLayout(false);
            this._finderRail.ResumeLayout(false);
            this._recipeBar.ResumeLayout(false);
            this._sidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
