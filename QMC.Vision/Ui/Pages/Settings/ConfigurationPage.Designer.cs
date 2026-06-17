using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class ConfigurationPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _hdr;
        private GroupBox _provGrp;
        private Label    _lblProvider;
        private ComboBox _cbProvider;
        private Label    _lblBackendVer;
        private GroupBox _cgxGrp;
        private Label    _cgxLabel;
        private Button   _btnCgxRefresh;
        private Button   _btnCgxTest;
        private GroupBox _imgGrp;
        private Label    _lblImgPath;
        private TextBox  _tbImagePath;
        private Button   _btnBrowse;
        private CheckBox _cbImageEnable;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new System.Windows.Forms.Label();
            this._provGrp = new System.Windows.Forms.GroupBox();
            this._lblProvider = new System.Windows.Forms.Label();
            this._cbProvider = new System.Windows.Forms.ComboBox();
            this._lblBackendVer = new System.Windows.Forms.Label();
            this._cgxGrp = new System.Windows.Forms.GroupBox();
            this._cgxLabel = new System.Windows.Forms.Label();
            this._btnCgxRefresh = new System.Windows.Forms.Button();
            this._btnCgxTest = new System.Windows.Forms.Button();
            this._imgGrp = new System.Windows.Forms.GroupBox();
            this._lblImgPath = new System.Windows.Forms.Label();
            this._tbImagePath = new System.Windows.Forms.TextBox();
            this._btnBrowse = new System.Windows.Forms.Button();
            this._cbImageEnable = new System.Windows.Forms.CheckBox();
            this._provGrp.SuspendLayout();
            this._cgxGrp.SuspendLayout();
            this._imgGrp.SuspendLayout();
            this.SuspendLayout();
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(0, 0);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(1331, 30);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "Configuration — Module";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _provGrp
            // 
            this._provGrp.Controls.Add(this._lblProvider);
            this._provGrp.Controls.Add(this._cbProvider);
            this._provGrp.Controls.Add(this._lblBackendVer);
            this._provGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._provGrp.Location = new System.Drawing.Point(20, 50);
            this._provGrp.Name = "_provGrp";
            this._provGrp.Size = new System.Drawing.Size(640, 110);
            this._provGrp.TabIndex = 1;
            this._provGrp.TabStop = false;
            this._provGrp.Text = "Vision Backend (재시작 후 반영)";
            // 
            // _lblProvider
            // 
            this._lblProvider.AutoSize = true;
            this._lblProvider.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblProvider.Location = new System.Drawing.Point(16, 30);
            this._lblProvider.Name = "_lblProvider";
            this._lblProvider.Size = new System.Drawing.Size(65, 20);
            this._lblProvider.TabIndex = 0;
            this._lblProvider.Text = "Provider";
            // 
            // _cbProvider
            // 
            this._cbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbProvider.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbProvider.Location = new System.Drawing.Point(100, 26);
            this._cbProvider.Name = "_cbProvider";
            this._cbProvider.Size = new System.Drawing.Size(200, 28);
            this._cbProvider.TabIndex = 1;
            this._cbProvider.SelectedIndexChanged += new System.EventHandler(this.OnProviderChanged);
            // 
            // _lblBackendVer
            // 
            this._lblBackendVer.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblBackendVer.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblBackendVer.Location = new System.Drawing.Point(16, 66);
            this._lblBackendVer.Name = "_lblBackendVer";
            this._lblBackendVer.Size = new System.Drawing.Size(600, 30);
            this._lblBackendVer.TabIndex = 2;
            // 
            // _cgxGrp
            // 
            this._cgxGrp.Controls.Add(this._cgxLabel);
            this._cgxGrp.Controls.Add(this._btnCgxRefresh);
            this._cgxGrp.Controls.Add(this._btnCgxTest);
            this._cgxGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._cgxGrp.Location = new System.Drawing.Point(680, 50);
            this._cgxGrp.Name = "_cgxGrp";
            this._cgxGrp.Size = new System.Drawing.Size(560, 147);
            this._cgxGrp.TabIndex = 2;
            this._cgxGrp.TabStop = false;
            this._cgxGrp.Text = "Cognex VisionPro diagnostics";
            // 
            // _cgxLabel
            // 
            this._cgxLabel.Font = new System.Drawing.Font("Consolas", 10F);
            this._cgxLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._cgxLabel.Location = new System.Drawing.Point(6, 50);
            this._cgxLabel.Name = "_cgxLabel";
            this._cgxLabel.Size = new System.Drawing.Size(530, 46);
            this._cgxLabel.TabIndex = 0;
            // 
            // _btnCgxRefresh
            // 
            this._btnCgxRefresh.BackColor = System.Drawing.Color.White;
            this._btnCgxRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCgxRefresh.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnCgxRefresh.Location = new System.Drawing.Point(205, 99);
            this._btnCgxRefresh.Name = "_btnCgxRefresh";
            this._btnCgxRefresh.Size = new System.Drawing.Size(100, 32);
            this._btnCgxRefresh.TabIndex = 1;
            this._btnCgxRefresh.Text = "Refresh";
            this._btnCgxRefresh.UseVisualStyleBackColor = false;
            this._btnCgxRefresh.Click += new System.EventHandler(this.OnCgxRefreshClick);
            // 
            // _btnCgxTest
            // 
            this._btnCgxTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnCgxTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCgxTest.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnCgxTest.ForeColor = System.Drawing.Color.White;
            this._btnCgxTest.Location = new System.Drawing.Point(332, 98);
            this._btnCgxTest.Name = "_btnCgxTest";
            this._btnCgxTest.Size = new System.Drawing.Size(100, 43);
            this._btnCgxTest.TabIndex = 2;
            this._btnCgxTest.Text = "Run test";
            this._btnCgxTest.UseVisualStyleBackColor = false;
            this._btnCgxTest.Click += new System.EventHandler(this.OnCgxTestClick);
            // 
            // _imgGrp
            // 
            this._imgGrp.Controls.Add(this._lblImgPath);
            this._imgGrp.Controls.Add(this._tbImagePath);
            this._imgGrp.Controls.Add(this._btnBrowse);
            this._imgGrp.Controls.Add(this._cbImageEnable);
            this._imgGrp.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._imgGrp.Location = new System.Drawing.Point(20, 180);
            this._imgGrp.Name = "_imgGrp";
            this._imgGrp.Size = new System.Drawing.Size(640, 100);
            this._imgGrp.TabIndex = 3;
            this._imgGrp.TabStop = false;
            this._imgGrp.Text = "Image log saver";
            // 
            // _lblImgPath
            // 
            this._lblImgPath.AutoSize = true;
            this._lblImgPath.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblImgPath.Location = new System.Drawing.Point(16, 30);
            this._lblImgPath.Name = "_lblImgPath";
            this._lblImgPath.Size = new System.Drawing.Size(40, 20);
            this._lblImgPath.TabIndex = 0;
            this._lblImgPath.Text = "Path";
            // 
            // _tbImagePath
            // 
            this._tbImagePath.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbImagePath.Location = new System.Drawing.Point(56, 26);
            this._tbImagePath.Name = "_tbImagePath";
            this._tbImagePath.Size = new System.Drawing.Size(440, 23);
            this._tbImagePath.TabIndex = 1;
            this._tbImagePath.TextChanged += new System.EventHandler(this.OnImagePathChanged);
            // 
            // _btnBrowse
            // 
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBrowse.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnBrowse.Location = new System.Drawing.Point(502, 41);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.Size = new System.Drawing.Size(100, 30);
            this._btnBrowse.TabIndex = 2;
            this._btnBrowse.Text = "Browse...";
            this._btnBrowse.Click += new System.EventHandler(this.OnBrowseClick);
            // 
            // _cbImageEnable
            // 
            this._cbImageEnable.AutoSize = true;
            this._cbImageEnable.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._cbImageEnable.Location = new System.Drawing.Point(16, 64);
            this._cbImageEnable.Name = "_cbImageEnable";
            this._cbImageEnable.Size = new System.Drawing.Size(74, 24);
            this._cbImageEnable.TabIndex = 3;
            this._cbImageEnable.Text = "Enable";
            this._cbImageEnable.CheckedChanged += new System.EventHandler(this.OnImageEnableChanged);
            // 
            // ConfigurationPage
            // 
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._provGrp);
            this.Controls.Add(this._cgxGrp);
            this.Controls.Add(this._imgGrp);
            this.Name = "ConfigurationPage";
            this.Size = new System.Drawing.Size(1331, 636);
            this._provGrp.ResumeLayout(false);
            this._provGrp.PerformLayout();
            this._cgxGrp.ResumeLayout(false);
            this._imgGrp.ResumeLayout(false);
            this._imgGrp.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
