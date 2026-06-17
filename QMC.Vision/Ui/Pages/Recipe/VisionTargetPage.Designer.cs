using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class VisionTargetPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _main;
        private Label _lblStatus;
        // 좌 (카메라 전용 — 확대)
        private TableLayoutPanel _left;
        private Label _secCam;
        private CameraView _cam;
        // 중 (ACTION 콤팩트 + 결과)
        private TableLayoutPanel _center;
        private Label _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnGrab, _btnMatch, _btnTrain, _btnLoad, _btnSaveImg, _btnEditSearch, _btnEditTrain;
        private Label _secMatch;
        private DataGridView _result;
        private Label _secTrain;
        private PictureBox _trainPic;
        // 우 (PARAMETERS + 조명)
        private TableLayoutPanel _right;
        private Label _secParam;
        private ParameterGridControl _params;
        private Label _secLight;
        private Panel _lightHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new TableLayoutPanel();
            this._main = new TableLayoutPanel();
            this._lblStatus = new Label();
            this._left = new TableLayoutPanel();
            this._secCam = new Label();
            this._cam = new CameraView();
            this._center = new TableLayoutPanel();
            this._secAction = new Label();
            this._actionPanel = new TableLayoutPanel();
            this._btnGrab = new Button(); this._btnMatch = new Button(); this._btnTrain = new Button();
            this._btnLoad = new Button(); this._btnSaveImg = new Button(); this._btnEditSearch = new Button(); this._btnEditTrain = new Button();
            this._secMatch = new Label();
            this._result = new DataGridView();
            this._secTrain = new Label();
            this._trainPic = new PictureBox();
            this._right = new TableLayoutPanel();
            this._secParam = new Label();
            this._params = new ParameterGridControl();
            this._secLight = new Label();
            this._lightHost = new Panel();
            this._root.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._center.SuspendLayout();
            this._actionPanel.SuspendLayout();
            this._right.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this._result).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this._trainPic).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // ── 섹션 라벨 ──
            this._secCam.Dock = DockStyle.Fill; this._secCam.Text = "CAMERA"; this._secCam.BackColor = UiTheme.StatusBarBg; this._secCam.ForeColor = Color.White; this._secCam.Font = UiTheme.SectionFont; this._secCam.TextAlign = ContentAlignment.MiddleLeft; this._secCam.Padding = new Padding(8, 0, 0, 0);
            this._secAction.Dock = DockStyle.Fill; this._secAction.Text = "ACTION"; this._secAction.BackColor = UiTheme.StatusBarBg; this._secAction.ForeColor = Color.White; this._secAction.Font = UiTheme.SectionFont; this._secAction.TextAlign = ContentAlignment.MiddleLeft; this._secAction.Padding = new Padding(8, 0, 0, 0);
            this._secMatch.Dock = DockStyle.Fill; this._secMatch.Text = "MATCH RESULT"; this._secMatch.BackColor = UiTheme.StatusBarBg; this._secMatch.ForeColor = Color.White; this._secMatch.Font = UiTheme.SectionFont; this._secMatch.TextAlign = ContentAlignment.MiddleLeft; this._secMatch.Padding = new Padding(8, 0, 0, 0);
            this._secTrain.Dock = DockStyle.Fill; this._secTrain.Text = "TRAINED PATTERN"; this._secTrain.BackColor = UiTheme.StatusBarBg; this._secTrain.ForeColor = Color.White; this._secTrain.Font = UiTheme.SectionFont; this._secTrain.TextAlign = ContentAlignment.MiddleLeft; this._secTrain.Padding = new Padding(8, 0, 0, 0);
            this._secParam.Dock = DockStyle.Fill; this._secParam.Text = "PARAMETERS"; this._secParam.BackColor = UiTheme.StatusBarBg; this._secParam.ForeColor = Color.White; this._secParam.Font = UiTheme.SectionFont; this._secParam.TextAlign = ContentAlignment.MiddleLeft; this._secParam.Padding = new Padding(8, 0, 0, 0);
            this._secLight.Dock = DockStyle.Fill; this._secLight.Text = "검사 조명"; this._secLight.BackColor = UiTheme.StatusBarBg; this._secLight.ForeColor = Color.White; this._secLight.Font = UiTheme.SectionFont; this._secLight.TextAlign = ContentAlignment.MiddleLeft; this._secLight.Padding = new Padding(8, 0, 0, 0);

            // ── 좌 (카메라 전용) ──
            this._cam.Dock = DockStyle.Fill; this._cam.BackColor = Color.Black;
            this._left.Dock = DockStyle.Fill;
            this._left.ColumnCount = 1; this._left.RowCount = 2; this._left.Margin = Padding.Empty;
            this._left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._left.Controls.Add(this._secCam, 0, 0);
            this._left.Controls.Add(this._cam, 0, 1);

            // ── 중 ACTION (콤팩트 3×3, 버튼 h≈44) ──
            this._btnGrab.Dock = DockStyle.Fill; this._btnGrab.Text = "GRAB"; this._btnGrab.FlatStyle = FlatStyle.Flat; this._btnGrab.Font = UiTheme.ButtonFont; this._btnGrab.BackColor = UiTheme.Accent; this._btnGrab.ForeColor = Color.White;
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            this._btnMatch.Dock = DockStyle.Fill; this._btnMatch.Text = "MATCH"; this._btnMatch.FlatStyle = FlatStyle.Flat; this._btnMatch.Font = UiTheme.ButtonFont; this._btnMatch.BackColor = UiTheme.Accent; this._btnMatch.ForeColor = Color.White;
            this._btnMatch.Click += new System.EventHandler(this.OnMatchClick);
            this._btnTrain.Dock = DockStyle.Fill; this._btnTrain.Text = "TRAIN"; this._btnTrain.FlatStyle = FlatStyle.Flat; this._btnTrain.Font = UiTheme.ButtonFont; this._btnTrain.BackColor = UiTheme.Accent; this._btnTrain.ForeColor = Color.White;
            this._btnTrain.Click += new System.EventHandler(this.OnTrainClick);
            this._btnLoad.Dock = DockStyle.Fill; this._btnLoad.Text = "LOAD"; this._btnLoad.FlatStyle = FlatStyle.Flat; this._btnLoad.Font = UiTheme.ButtonFont; this._btnLoad.BackColor = Color.White; this._btnLoad.ForeColor = Color.Black;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            this._btnSaveImg.Dock = DockStyle.Fill; this._btnSaveImg.Text = "이미지 저장"; this._btnSaveImg.FlatStyle = FlatStyle.Flat; this._btnSaveImg.Font = UiTheme.ButtonFont; this._btnSaveImg.BackColor = Color.White; this._btnSaveImg.ForeColor = Color.Black;
            this._btnSaveImg.Click += new System.EventHandler(this.OnSaveImageClick);
            this._btnEditSearch.Dock = DockStyle.Fill; this._btnEditSearch.Text = "EDIT SEARCH ROI"; this._btnEditSearch.FlatStyle = FlatStyle.Flat; this._btnEditSearch.Font = UiTheme.ButtonFont; this._btnEditSearch.BackColor = Color.LightYellow; this._btnEditSearch.ForeColor = Color.Black;
            this._btnEditSearch.Click += new System.EventHandler(this.OnEditSearchClick);
            this._btnEditTrain.Dock = DockStyle.Fill; this._btnEditTrain.Text = "EDIT TRAIN ROI"; this._btnEditTrain.FlatStyle = FlatStyle.Flat; this._btnEditTrain.Font = UiTheme.ButtonFont; this._btnEditTrain.BackColor = Color.LightYellow; this._btnEditTrain.ForeColor = Color.Black;
            this._btnEditTrain.Click += new System.EventHandler(this.OnEditTrainClick);
            this._actionPanel.Dock = DockStyle.Fill;
            this._actionPanel.ColumnCount = 3; this._actionPanel.RowCount = 3; this._actionPanel.Margin = Padding.Empty;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            this._actionPanel.Controls.Add(this._btnGrab, 0, 0);
            this._actionPanel.Controls.Add(this._btnMatch, 1, 0);
            this._actionPanel.Controls.Add(this._btnTrain, 2, 0);
            this._actionPanel.Controls.Add(this._btnLoad, 0, 1);
            this._actionPanel.Controls.Add(this._btnSaveImg, 1, 1);
            this._actionPanel.Controls.Add(this._btnEditSearch, 2, 1);
            this._actionPanel.Controls.Add(this._btnEditTrain, 0, 2);

            // ── 중 결과 그리드 ──
            this._result.Dock = DockStyle.Fill;
            this._result.AllowUserToAddRows = false; this._result.RowHeadersVisible = false;
            this._result.BackgroundColor = Color.White; this._result.Font = UiTheme.ValueFont;
            this._result.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._result.Columns.Add("Idx", "Idx");
            this._result.Columns.Add("X", "X");
            this._result.Columns.Add("Y", "Y");
            this._result.Columns.Add("Angle", "Angle");
            this._result.Columns.Add("Score", "Score");

            // 학습 패턴 미리보기
            this._trainPic.Dock = DockStyle.Fill;
            this._trainPic.BackColor = Color.Black;
            this._trainPic.BorderStyle = BorderStyle.FixedSingle;
            this._trainPic.SizeMode = PictureBoxSizeMode.Zoom;

            this._center.Dock = DockStyle.Fill;
            this._center.ColumnCount = 1; this._center.RowCount = 6; this._center.Margin = Padding.Empty;
            this._center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));    // ACTION 헤더
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));   // 액션 3×3
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));    // MATCH RESULT 헤더
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 132f));   // 결과 그리드(축소)
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));    // TRAINED PATTERN 헤더
            this._center.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // 학습 이미지
            this._center.Controls.Add(this._secAction, 0, 0);
            this._center.Controls.Add(this._actionPanel, 0, 1);
            this._center.Controls.Add(this._secMatch, 0, 2);
            this._center.Controls.Add(this._result, 0, 3);
            this._center.Controls.Add(this._secTrain, 0, 4);
            this._center.Controls.Add(this._trainPic, 0, 5);

            // ── 우 (PARAMETERS + 조명) — 라이브튜닝 제거, 조명 240→440 확대 ──
            this._params.Dock = DockStyle.Fill;
            this._lightHost.Dock = DockStyle.Fill; this._lightHost.BackColor = UiTheme.MainBg;
            this._right.Dock = DockStyle.Fill;
            this._right.ColumnCount = 1; this._right.RowCount = 4; this._right.Margin = Padding.Empty;
            this._right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 440f));
            this._right.Controls.Add(this._secParam, 0, 0);
            this._right.Controls.Add(this._params, 0, 1);
            this._right.Controls.Add(this._secLight, 0, 2);
            this._right.Controls.Add(this._lightHost, 0, 3);

            // ── _main (3열) ──
            this._main.Dock = DockStyle.Fill;
            this._main.ColumnCount = 3; this._main.RowCount = 1; this._main.Margin = Padding.Empty;
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380f));
            this._main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._center, 1, 0);
            this._main.Controls.Add(this._right, 2, 0);

            // ── _lblStatus ──
            this._lblStatus.Dock = DockStyle.Fill; this._lblStatus.Text = "Ready.";
            this._lblStatus.Font = UiTheme.ValueFont; this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.BackColor = Color.WhiteSmoke; this._lblStatus.TextAlign = ContentAlignment.MiddleLeft; this._lblStatus.Padding = new Padding(8, 0, 0, 0);

            // ── _root ──
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1; this._root.RowCount = 2; this._root.Margin = Padding.Empty;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._root.Controls.Add(this._main, 0, 0);
            this._root.Controls.Add(this._lblStatus, 0, 1);

            this.Controls.Add(this._root);
            this.AutoScaleMode = AutoScaleMode.None;
            this.Name = "VisionTargetPage";
            this.Size = new Size(1710, 832);
            ((System.ComponentModel.ISupportInitialize)this._result).EndInit();
            ((System.ComponentModel.ISupportInitialize)this._trainPic).EndInit();
            this._right.ResumeLayout(false);
            this._actionPanel.ResumeLayout(false);
            this._center.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
