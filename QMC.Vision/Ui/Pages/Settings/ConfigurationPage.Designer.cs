using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class ConfigurationPage
    {
        private System.ComponentModel.IContainer components = null;

        // 루트 / 헤더 / 언어 body
        private TableLayoutPanel rootLayout;
        private Label            _hdr;
        private TableLayoutPanel bodyLayout;
        private Label            _lblLanguage;
        private ComboBox         _cbLang;

        // Vision Backend (Provider)
        private GroupBox         _provGrp;
        private TableLayoutPanel provLayout;
        private Label            _lblProvider;
        private ComboBox         _cbProvider;
        private Label            _lblBackendVer;

        // Cognex 진단
        private GroupBox         _cgxGrp;
        private TableLayoutPanel cgxLayout;
        private Label            _cgxLabel;
        private Button           _btnCgxRefresh;
        private Button           _btnCgxTest;

        // 이미지 로그 저장
        private GroupBox         _imgGrp;
        private TableLayoutPanel imgLayout;
        private Label            _lblImgPath;
        private TextBox          _tbImagePath;
        private Button           _btnBrowse;
        private CheckBox         _cbImageEnable;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout    = new System.Windows.Forms.TableLayoutPanel();
            this._hdr          = new System.Windows.Forms.Label();
            this.bodyLayout    = new System.Windows.Forms.TableLayoutPanel();
            this._lblLanguage  = new System.Windows.Forms.Label();
            this._cbLang       = new System.Windows.Forms.ComboBox();
            this._provGrp      = new System.Windows.Forms.GroupBox();
            this.provLayout    = new System.Windows.Forms.TableLayoutPanel();
            this._lblProvider  = new System.Windows.Forms.Label();
            this._cbProvider   = new System.Windows.Forms.ComboBox();
            this._lblBackendVer= new System.Windows.Forms.Label();
            this._cgxGrp       = new System.Windows.Forms.GroupBox();
            this.cgxLayout     = new System.Windows.Forms.TableLayoutPanel();
            this._cgxLabel     = new System.Windows.Forms.Label();
            this._btnCgxRefresh= new System.Windows.Forms.Button();
            this._btnCgxTest   = new System.Windows.Forms.Button();
            this._imgGrp       = new System.Windows.Forms.GroupBox();
            this.imgLayout     = new System.Windows.Forms.TableLayoutPanel();
            this._lblImgPath   = new System.Windows.Forms.Label();
            this._tbImagePath  = new System.Windows.Forms.TextBox();
            this._btnBrowse    = new System.Windows.Forms.Button();
            this._cbImageEnable= new System.Windows.Forms.CheckBox();
            this.rootLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this._provGrp.SuspendLayout();
            this.provLayout.SuspendLayout();
            this._cgxGrp.SuspendLayout();
            this.cgxLayout.SuspendLayout();
            this._imgGrp.SuspendLayout();
            this.imgLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this._hdr,       0, 0);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 1);
            this.rootLayout.Controls.Add(this._provGrp,   0, 2);
            this.rootLayout.Controls.Add(this._cgxGrp,    0, 3);
            this.rootLayout.Controls.Add(this._imgGrp,    0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 6;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1331, 636);
            this.rootLayout.TabIndex = 0;
            //
            // _hdr
            //
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "GENERAL";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // bodyLayout
            //
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.bodyLayout.Controls.Add(this._lblLanguage, 0, 0);
            this.bodyLayout.Controls.Add(this._cbLang,      1, 0);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.bodyLayout.TabIndex = 1;
            //
            // _lblLanguage
            //
            this._lblLanguage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this._lblLanguage.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLanguage.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblLanguage.Margin = new System.Windows.Forms.Padding(2);
            this._lblLanguage.Name = "_lblLanguage";
            this._lblLanguage.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this._lblLanguage.TabIndex = 0;
            this._lblLanguage.Text = "언어 설정";
            this._lblLanguage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbLang
            //
            this._cbLang.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLang.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbLang.Margin = new System.Windows.Forms.Padding(2);
            this._cbLang.Name = "_cbLang";
            this._cbLang.TabIndex = 1;
            this._cbLang.SelectedIndexChanged += new System.EventHandler(this.OnLanguageChanged);
            //
            // _provGrp
            //
            this._provGrp.Controls.Add(this.provLayout);
            this._provGrp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._provGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._provGrp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._provGrp.Name = "_provGrp";
            this._provGrp.Padding = new System.Windows.Forms.Padding(8);
            this._provGrp.TabIndex = 2;
            this._provGrp.TabStop = false;
            this._provGrp.Text = "Vision Backend (재시작 후 반영)";
            //
            // provLayout
            //
            this.provLayout.ColumnCount = 2;
            this.provLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.provLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.provLayout.Controls.Add(this._lblProvider,   0, 0);
            this.provLayout.Controls.Add(this._cbProvider,    1, 0);
            this.provLayout.Controls.Add(this._lblBackendVer, 0, 1);
            this.provLayout.SetColumnSpan(this._lblBackendVer, 2);
            this.provLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.provLayout.Name = "provLayout";
            this.provLayout.RowCount = 2;
            this.provLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.provLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.provLayout.TabIndex = 0;
            //
            // _lblProvider
            //
            this._lblProvider.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblProvider.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblProvider.Margin = new System.Windows.Forms.Padding(2);
            this._lblProvider.Name = "_lblProvider";
            this._lblProvider.TabIndex = 0;
            this._lblProvider.Text = "Provider";
            this._lblProvider.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbProvider
            //
            this._cbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbProvider.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbProvider.Margin = new System.Windows.Forms.Padding(2);
            this._cbProvider.Name = "_cbProvider";
            this._cbProvider.Size = new System.Drawing.Size(220, 28);
            this._cbProvider.TabIndex = 1;
            this._cbProvider.SelectedIndexChanged += new System.EventHandler(this.OnProviderChanged);
            //
            // _lblBackendVer
            //
            this._lblBackendVer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblBackendVer.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblBackendVer.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblBackendVer.Margin = new System.Windows.Forms.Padding(2);
            this._lblBackendVer.Name = "_lblBackendVer";
            this._lblBackendVer.TabIndex = 2;
            this._lblBackendVer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cgxGrp
            //
            this._cgxGrp.Controls.Add(this.cgxLayout);
            this._cgxGrp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cgxGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._cgxGrp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._cgxGrp.Name = "_cgxGrp";
            this._cgxGrp.Padding = new System.Windows.Forms.Padding(8);
            this._cgxGrp.TabIndex = 3;
            this._cgxGrp.TabStop = false;
            this._cgxGrp.Text = "Cognex VisionPro diagnostics";
            //
            // cgxLayout
            //
            this.cgxLayout.ColumnCount = 3;
            this.cgxLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cgxLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.cgxLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.cgxLayout.Controls.Add(this._cgxLabel,      0, 0);
            this.cgxLayout.Controls.Add(this._btnCgxRefresh, 1, 1);
            this.cgxLayout.Controls.Add(this._btnCgxTest,    2, 1);
            this.cgxLayout.SetColumnSpan(this._cgxLabel, 3);
            this.cgxLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cgxLayout.Name = "cgxLayout";
            this.cgxLayout.RowCount = 2;
            this.cgxLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cgxLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.cgxLayout.TabIndex = 0;
            //
            // _cgxLabel
            //
            this._cgxLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cgxLabel.Font = new System.Drawing.Font("Consolas", 10F);
            this._cgxLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._cgxLabel.Margin = new System.Windows.Forms.Padding(2);
            this._cgxLabel.Name = "_cgxLabel";
            this._cgxLabel.TabIndex = 0;
            //
            // _btnCgxRefresh
            //
            this._btnCgxRefresh.BackColor = System.Drawing.Color.White;
            this._btnCgxRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCgxRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCgxRefresh.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnCgxRefresh.Margin = new System.Windows.Forms.Padding(2);
            this._btnCgxRefresh.Name = "_btnCgxRefresh";
            this._btnCgxRefresh.TabIndex = 1;
            this._btnCgxRefresh.Text = "Refresh";
            this._btnCgxRefresh.UseVisualStyleBackColor = false;
            this._btnCgxRefresh.Click += new System.EventHandler(this.OnCgxRefreshClick);
            //
            // _btnCgxTest
            //
            this._btnCgxTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnCgxTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCgxTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCgxTest.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnCgxTest.ForeColor = System.Drawing.Color.White;
            this._btnCgxTest.Margin = new System.Windows.Forms.Padding(2);
            this._btnCgxTest.Name = "_btnCgxTest";
            this._btnCgxTest.TabIndex = 2;
            this._btnCgxTest.Text = "Run test";
            this._btnCgxTest.UseVisualStyleBackColor = false;
            this._btnCgxTest.Click += new System.EventHandler(this.OnCgxTestClick);
            //
            // _imgGrp
            //
            this._imgGrp.Controls.Add(this.imgLayout);
            this._imgGrp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._imgGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._imgGrp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._imgGrp.Name = "_imgGrp";
            this._imgGrp.Padding = new System.Windows.Forms.Padding(8);
            this._imgGrp.TabIndex = 4;
            this._imgGrp.TabStop = false;
            this._imgGrp.Text = "Image log saver";
            //
            // imgLayout
            //
            this.imgLayout.ColumnCount = 3;
            this.imgLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.imgLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.imgLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.imgLayout.Controls.Add(this._lblImgPath,    0, 0);
            this.imgLayout.Controls.Add(this._tbImagePath,   1, 0);
            this.imgLayout.Controls.Add(this._btnBrowse,     2, 0);
            this.imgLayout.Controls.Add(this._cbImageEnable, 0, 1);
            this.imgLayout.SetColumnSpan(this._cbImageEnable, 3);
            this.imgLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imgLayout.Name = "imgLayout";
            this.imgLayout.RowCount = 2;
            this.imgLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.imgLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.imgLayout.TabIndex = 0;
            //
            // _lblImgPath
            //
            this._lblImgPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblImgPath.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblImgPath.Margin = new System.Windows.Forms.Padding(2);
            this._lblImgPath.Name = "_lblImgPath";
            this._lblImgPath.TabIndex = 0;
            this._lblImgPath.Text = "Path";
            this._lblImgPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbImagePath
            //
            this._tbImagePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbImagePath.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbImagePath.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this._tbImagePath.Name = "_tbImagePath";
            this._tbImagePath.TabIndex = 1;
            this._tbImagePath.TextChanged += new System.EventHandler(this.OnImagePathChanged);
            //
            // _btnBrowse
            //
            this._btnBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBrowse.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnBrowse.Margin = new System.Windows.Forms.Padding(2);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.TabIndex = 2;
            this._btnBrowse.Text = "Browse...";
            this._btnBrowse.UseVisualStyleBackColor = true;
            this._btnBrowse.Click += new System.EventHandler(this.OnBrowseClick);
            //
            // _cbImageEnable
            //
            this._cbImageEnable.AutoSize = true;
            this._cbImageEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbImageEnable.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbImageEnable.Margin = new System.Windows.Forms.Padding(2);
            this._cbImageEnable.Name = "_cbImageEnable";
            this._cbImageEnable.TabIndex = 3;
            this._cbImageEnable.Text = "Enable";
            this._cbImageEnable.UseVisualStyleBackColor = true;
            this._cbImageEnable.CheckedChanged += new System.EventHandler(this.OnImageEnableChanged);
            //
            // ConfigurationPage
            //
            this.Controls.Add(this.rootLayout);
            this.Name = "ConfigurationPage";
            this.Size = new System.Drawing.Size(1331, 636);
            this.rootLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this._provGrp.ResumeLayout(false);
            this.provLayout.ResumeLayout(false);
            this._cgxGrp.ResumeLayout(false);
            this.cgxLayout.ResumeLayout(false);
            this._imgGrp.ResumeLayout(false);
            this.imgLayout.ResumeLayout(false);
            this.imgLayout.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
