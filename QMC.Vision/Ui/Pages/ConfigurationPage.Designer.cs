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
            this._hdr = new Label();
            this._provGrp = new GroupBox();
            this._lblProvider = new Label();
            this._cbProvider = new ComboBox();
            this._lblBackendVer = new Label();
            this._cgxGrp = new GroupBox();
            this._cgxLabel = new Label();
            this._btnCgxRefresh = new Button();
            this._btnCgxTest = new Button();
            this._imgGrp = new GroupBox();
            this._lblImgPath = new Label();
            this._tbImagePath = new TextBox();
            this._btnBrowse = new Button();
            this._cbImageEnable = new CheckBox();
            this._provGrp.SuspendLayout();
            this._cgxGrp.SuspendLayout();
            this._imgGrp.SuspendLayout();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "Configuration — Module";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _provGrp
            this._provGrp.Location = new Point(20, 50);
            this._provGrp.Size = new Size(640, 110);
            this._provGrp.Text = "Vision Backend (재시작 후 반영)";
            this._provGrp.Font = UiTheme.SectionFont;

            // _lblProvider
            this._lblProvider.Location = new Point(16, 30);
            this._lblProvider.AutoSize = true;
            this._lblProvider.Text = "Provider";
            this._lblProvider.Font = UiTheme.ButtonFont;

            // _cbProvider (Items/SelectedItem 는 런타임 LoadConfig)
            this._cbProvider.Location = new Point(100, 26);
            this._cbProvider.Size = new Size(200, 26);
            this._cbProvider.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbProvider.Font = UiTheme.ButtonFont;
            this._cbProvider.SelectedIndexChanged += new System.EventHandler(this.OnProviderChanged);

            // _lblBackendVer (Text 런타임)
            this._lblBackendVer.Location = new Point(16, 66);
            this._lblBackendVer.Size = new Size(600, 30);
            this._lblBackendVer.Font = UiTheme.ValueFont;
            this._lblBackendVer.ForeColor = Color.DarkSlateGray;

            this._provGrp.Controls.Add(this._lblProvider);
            this._provGrp.Controls.Add(this._cbProvider);
            this._provGrp.Controls.Add(this._lblBackendVer);

            // _cgxGrp
            this._cgxGrp.Location = new Point(680, 50);
            this._cgxGrp.Size = new Size(560, 110);
            this._cgxGrp.Text = "Cognex VisionPro diagnostics";
            this._cgxGrp.Font = UiTheme.SectionFont;

            // _cgxLabel (Text 런타임 ProbeCognex)
            this._cgxLabel.Location = new Point(16, 30);
            this._cgxLabel.Size = new Size(530, 46);
            this._cgxLabel.Font = UiTheme.ValueFont;
            this._cgxLabel.ForeColor = Color.DarkSlateGray;

            // _btnCgxRefresh
            this._btnCgxRefresh.Location = new Point(330, 78);
            this._btnCgxRefresh.Size = new Size(100, 24);
            this._btnCgxRefresh.Text = "Refresh";
            this._btnCgxRefresh.FlatStyle = FlatStyle.Flat;
            this._btnCgxRefresh.Font = UiTheme.ButtonFont;
            this._btnCgxRefresh.BackColor = Color.White;
            this._btnCgxRefresh.Click += new System.EventHandler(this.OnCgxRefreshClick);

            // _btnCgxTest
            this._btnCgxTest.Location = new Point(440, 78);
            this._btnCgxTest.Size = new Size(100, 24);
            this._btnCgxTest.Text = "Run test";
            this._btnCgxTest.FlatStyle = FlatStyle.Flat;
            this._btnCgxTest.Font = UiTheme.ButtonFont;
            this._btnCgxTest.BackColor = UiTheme.Accent;
            this._btnCgxTest.ForeColor = Color.White;
            this._btnCgxTest.Click += new System.EventHandler(this.OnCgxTestClick);

            this._cgxGrp.Controls.Add(this._cgxLabel);
            this._cgxGrp.Controls.Add(this._btnCgxRefresh);
            this._cgxGrp.Controls.Add(this._btnCgxTest);

            // _imgGrp
            this._imgGrp.Location = new Point(20, 180);
            this._imgGrp.Size = new Size(640, 100);
            this._imgGrp.Text = "Image log saver";
            this._imgGrp.Font = UiTheme.SectionFont;

            // _lblImgPath
            this._lblImgPath.Location = new Point(16, 30);
            this._lblImgPath.AutoSize = true;
            this._lblImgPath.Text = "Path";
            this._lblImgPath.Font = UiTheme.ButtonFont;

            // _tbImagePath (Text 런타임)
            this._tbImagePath.Location = new Point(56, 26);
            this._tbImagePath.Size = new Size(440, 26);
            this._tbImagePath.Font = UiTheme.ValueFont;
            this._tbImagePath.TextChanged += new System.EventHandler(this.OnImagePathChanged);

            // _btnBrowse
            this._btnBrowse.Location = new Point(504, 24);
            this._btnBrowse.Size = new Size(100, 30);
            this._btnBrowse.Text = "Browse...";
            this._btnBrowse.FlatStyle = FlatStyle.Flat;
            this._btnBrowse.Font = UiTheme.ButtonFont;
            this._btnBrowse.Click += new System.EventHandler(this.OnBrowseClick);

            // _cbImageEnable (Checked 런타임)
            this._cbImageEnable.Location = new Point(16, 64);
            this._cbImageEnable.AutoSize = true;
            this._cbImageEnable.Text = "Enable";
            this._cbImageEnable.Font = UiTheme.ButtonFont;
            this._cbImageEnable.CheckedChanged += new System.EventHandler(this.OnImageEnableChanged);

            this._imgGrp.Controls.Add(this._lblImgPath);
            this._imgGrp.Controls.Add(this._tbImagePath);
            this._imgGrp.Controls.Add(this._btnBrowse);
            this._imgGrp.Controls.Add(this._cbImageEnable);

            // ConfigurationPage (원본 추가순서: 헤더→prov→cgx→img)
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._provGrp);
            this.Controls.Add(this._cgxGrp);
            this.Controls.Add(this._imgGrp);
            this.Name = "ConfigurationPage";
            this._provGrp.ResumeLayout(false);
            this._provGrp.PerformLayout();
            this._cgxGrp.ResumeLayout(false);
            this._imgGrp.ResumeLayout(false);
            this._imgGrp.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
