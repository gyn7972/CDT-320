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
        // 좌
        private TableLayoutPanel _left;
        private Label _secCam;
        private CameraView _cam;
        private Label _secMatch;
        private DataGridView _result;
        // 중
        private TableLayoutPanel _center;
        private Label _secRoi;
        private TableLayoutPanel _roiPanel;
        private RadioButton _rdoMain, _rdoSub, _rdoChip, _rdoCross;
        private RadioButton _rdoIndex1, _rdoIndex2, _rdoIndex4, _rdoIndex8;
        private Label _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnGrab, _btnMatch, _btnTrain, _btnLoad, _btnSave, _btnEditSearch, _btnEditTrain;
        // 우
        private TableLayoutPanel _right;
        private Label _secParam;
        private ParameterGridControl _params;
        private Label _secJog;
        private JogBox _jog;
        private Label _secSpeed;
        private TableLayoutPanel _speedPanel;
        private TrackBar _trkSpeed;
        private Label _lblSpeed;

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
            this._secMatch = new Label();
            this._result = new DataGridView();
            this._center = new TableLayoutPanel();
            this._secRoi = new Label();
            this._roiPanel = new TableLayoutPanel();
            this._rdoMain = new RadioButton(); this._rdoSub = new RadioButton(); this._rdoChip = new RadioButton(); this._rdoCross = new RadioButton();
            this._rdoIndex1 = new RadioButton(); this._rdoIndex2 = new RadioButton(); this._rdoIndex4 = new RadioButton(); this._rdoIndex8 = new RadioButton();
            this._secAction = new Label();
            this._actionPanel = new TableLayoutPanel();
            this._btnGrab = new Button(); this._btnMatch = new Button(); this._btnTrain = new Button();
            this._btnLoad = new Button(); this._btnSave = new Button(); this._btnEditSearch = new Button(); this._btnEditTrain = new Button();
            this._right = new TableLayoutPanel();
            this._secParam = new Label();
            this._params = new ParameterGridControl();
            this._secJog = new Label();
            this._jog = new JogBox();
            this._secSpeed = new Label();
            this._speedPanel = new TableLayoutPanel();
            this._trkSpeed = new TrackBar();
            this._lblSpeed = new Label();
            this._root.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._center.SuspendLayout();
            this._roiPanel.SuspendLayout();
            this._actionPanel.SuspendLayout();
            this._right.SuspendLayout();
            this._speedPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this._result).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this._trkSpeed).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // ── 섹션 라벨(주황바, 인라인) ──
            this._secCam.Dock = DockStyle.Fill; this._secCam.Text = "CAMERA"; this._secCam.BackColor = UiTheme.StatusBarBg; this._secCam.ForeColor = Color.White; this._secCam.Font = UiTheme.SectionFont; this._secCam.TextAlign = ContentAlignment.MiddleLeft; this._secCam.Padding = new Padding(8, 0, 0, 0);
            this._secMatch.Dock = DockStyle.Fill; this._secMatch.Text = "MATCH RESULT"; this._secMatch.BackColor = UiTheme.StatusBarBg; this._secMatch.ForeColor = Color.White; this._secMatch.Font = UiTheme.SectionFont; this._secMatch.TextAlign = ContentAlignment.MiddleLeft; this._secMatch.Padding = new Padding(8, 0, 0, 0);
            this._secRoi.Dock = DockStyle.Fill; this._secRoi.Text = "ROI"; this._secRoi.BackColor = UiTheme.StatusBarBg; this._secRoi.ForeColor = Color.White; this._secRoi.Font = UiTheme.SectionFont; this._secRoi.TextAlign = ContentAlignment.MiddleLeft; this._secRoi.Padding = new Padding(8, 0, 0, 0);
            this._secAction.Dock = DockStyle.Fill; this._secAction.Text = "ACTION"; this._secAction.BackColor = UiTheme.StatusBarBg; this._secAction.ForeColor = Color.White; this._secAction.Font = UiTheme.SectionFont; this._secAction.TextAlign = ContentAlignment.MiddleLeft; this._secAction.Padding = new Padding(8, 0, 0, 0);
            this._secParam.Dock = DockStyle.Fill; this._secParam.Text = "PARAMETERS"; this._secParam.BackColor = UiTheme.StatusBarBg; this._secParam.ForeColor = Color.White; this._secParam.Font = UiTheme.SectionFont; this._secParam.TextAlign = ContentAlignment.MiddleLeft; this._secParam.Padding = new Padding(8, 0, 0, 0);
            this._secJog.Dock = DockStyle.Fill; this._secJog.Text = "JOG"; this._secJog.BackColor = UiTheme.StatusBarBg; this._secJog.ForeColor = Color.White; this._secJog.Font = UiTheme.SectionFont; this._secJog.TextAlign = ContentAlignment.MiddleLeft; this._secJog.Padding = new Padding(8, 0, 0, 0);
            this._secSpeed.Dock = DockStyle.Fill; this._secSpeed.Text = "SPEED"; this._secSpeed.BackColor = UiTheme.StatusBarBg; this._secSpeed.ForeColor = Color.White; this._secSpeed.Font = UiTheme.SectionFont; this._secSpeed.TextAlign = ContentAlignment.MiddleLeft; this._secSpeed.Padding = new Padding(8, 0, 0, 0);

            // ── 좌 (camera + match) ──
            this._cam.Dock = DockStyle.Fill;
            this._cam.BackColor = Color.Black;
            this._result.Dock = DockStyle.Fill;
            this._result.AllowUserToAddRows = false;
            this._result.RowHeadersVisible = false;
            this._result.BackgroundColor = Color.White;
            this._result.Font = UiTheme.ValueFont;
            this._result.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._result.Columns.Add("Idx", "Idx");
            this._result.Columns.Add("X", "X");
            this._result.Columns.Add("Y", "Y");
            this._result.Columns.Add("Angle", "Angle");
            this._result.Columns.Add("Score", "Score");
            this._left.Dock = DockStyle.Fill;
            this._left.ColumnCount = 1;
            this._left.RowCount = 4;
            this._left.Margin = Padding.Empty;
            this._left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            this._left.Controls.Add(this._secCam, 0, 0);
            this._left.Controls.Add(this._cam, 0, 1);
            this._left.Controls.Add(this._secMatch, 0, 2);
            this._left.Controls.Add(this._result, 0, 3);

            // ── 중 ROI 라디오 (인라인) ──
            this._rdoMain.Dock = DockStyle.Fill; this._rdoMain.Text = "Main"; this._rdoMain.Checked = true; this._rdoMain.Font = UiTheme.ButtonFont;
            this._rdoSub.Dock = DockStyle.Fill; this._rdoSub.Text = "Sub"; this._rdoSub.Font = UiTheme.ButtonFont;
            this._rdoChip.Dock = DockStyle.Fill; this._rdoChip.Text = "Chip"; this._rdoChip.Font = UiTheme.ButtonFont;
            this._rdoCross.Dock = DockStyle.Fill; this._rdoCross.Text = "Cross"; this._rdoCross.Font = UiTheme.ButtonFont;
            this._rdoIndex1.Dock = DockStyle.Fill; this._rdoIndex1.Text = "Index 1"; this._rdoIndex1.Checked = true; this._rdoIndex1.Font = UiTheme.ButtonFont;
            this._rdoIndex2.Dock = DockStyle.Fill; this._rdoIndex2.Text = "Index 2"; this._rdoIndex2.Font = UiTheme.ButtonFont;
            this._rdoIndex4.Dock = DockStyle.Fill; this._rdoIndex4.Text = "Index 4"; this._rdoIndex4.Font = UiTheme.ButtonFont;
            this._rdoIndex8.Dock = DockStyle.Fill; this._rdoIndex8.Text = "Index 8"; this._rdoIndex8.Font = UiTheme.ButtonFont;
            this._roiPanel.Dock = DockStyle.Fill;
            this._roiPanel.ColumnCount = 4;
            this._roiPanel.RowCount = 2;
            this._roiPanel.BackColor = UiTheme.OptionPanelBg;
            this._roiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this._roiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this._roiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this._roiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            this._roiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this._roiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this._roiPanel.Controls.Add(this._rdoMain, 0, 0);
            this._roiPanel.Controls.Add(this._rdoSub, 1, 0);
            this._roiPanel.Controls.Add(this._rdoChip, 2, 0);
            this._roiPanel.Controls.Add(this._rdoCross, 3, 0);
            this._roiPanel.Controls.Add(this._rdoIndex1, 0, 1);
            this._roiPanel.Controls.Add(this._rdoIndex2, 1, 1);
            this._roiPanel.Controls.Add(this._rdoIndex4, 2, 1);
            this._roiPanel.Controls.Add(this._rdoIndex8, 3, 1);

            // ── 중 액션 3×3 (인라인) ──
            this._btnGrab.Dock = DockStyle.Fill; this._btnGrab.Text = "GRAB"; this._btnGrab.FlatStyle = FlatStyle.Flat; this._btnGrab.Font = UiTheme.ButtonFont; this._btnGrab.BackColor = UiTheme.Accent; this._btnGrab.ForeColor = Color.White;
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            this._btnMatch.Dock = DockStyle.Fill; this._btnMatch.Text = "MATCH"; this._btnMatch.FlatStyle = FlatStyle.Flat; this._btnMatch.Font = UiTheme.ButtonFont; this._btnMatch.BackColor = UiTheme.Accent; this._btnMatch.ForeColor = Color.White;
            this._btnMatch.Click += new System.EventHandler(this.OnMatchClick);
            this._btnTrain.Dock = DockStyle.Fill; this._btnTrain.Text = "TRAIN"; this._btnTrain.FlatStyle = FlatStyle.Flat; this._btnTrain.Font = UiTheme.ButtonFont; this._btnTrain.BackColor = UiTheme.Accent; this._btnTrain.ForeColor = Color.White;
            this._btnTrain.Click += new System.EventHandler(this.OnTrainClick);
            this._btnLoad.Dock = DockStyle.Fill; this._btnLoad.Text = "LOAD"; this._btnLoad.FlatStyle = FlatStyle.Flat; this._btnLoad.Font = UiTheme.ButtonFont; this._btnLoad.BackColor = Color.White; this._btnLoad.ForeColor = Color.Black;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            this._btnSave.Dock = DockStyle.Fill; this._btnSave.Text = "SAVE"; this._btnSave.FlatStyle = FlatStyle.Flat; this._btnSave.Font = UiTheme.ButtonFont; this._btnSave.BackColor = Color.White; this._btnSave.ForeColor = Color.Black;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            this._btnEditSearch.Dock = DockStyle.Fill; this._btnEditSearch.Text = "EDIT SEARCH ROI"; this._btnEditSearch.FlatStyle = FlatStyle.Flat; this._btnEditSearch.Font = UiTheme.ButtonFont; this._btnEditSearch.BackColor = Color.LightYellow; this._btnEditSearch.ForeColor = Color.Black;
            this._btnEditSearch.Click += new System.EventHandler(this.OnEditSearchClick);
            this._btnEditTrain.Dock = DockStyle.Fill; this._btnEditTrain.Text = "EDIT TRAIN ROI"; this._btnEditTrain.FlatStyle = FlatStyle.Flat; this._btnEditTrain.Font = UiTheme.ButtonFont; this._btnEditTrain.BackColor = Color.LightYellow; this._btnEditTrain.ForeColor = Color.Black;
            this._btnEditTrain.Click += new System.EventHandler(this.OnEditTrainClick);
            this._actionPanel.Dock = DockStyle.Fill;
            this._actionPanel.ColumnCount = 3;
            this._actionPanel.RowCount = 3;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            this._actionPanel.Controls.Add(this._btnGrab, 0, 0);
            this._actionPanel.Controls.Add(this._btnMatch, 1, 0);
            this._actionPanel.Controls.Add(this._btnTrain, 2, 0);
            this._actionPanel.Controls.Add(this._btnLoad, 0, 1);
            this._actionPanel.Controls.Add(this._btnSave, 1, 1);
            this._actionPanel.Controls.Add(this._btnEditSearch, 2, 1);
            this._actionPanel.Controls.Add(this._btnEditTrain, 0, 2);

            this._center.Dock = DockStyle.Fill;
            this._center.ColumnCount = 1;
            this._center.RowCount = 4;
            this._center.Margin = Padding.Empty;
            this._center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._center.Controls.Add(this._secRoi, 0, 0);
            this._center.Controls.Add(this._roiPanel, 0, 1);
            this._center.Controls.Add(this._secAction, 0, 2);
            this._center.Controls.Add(this._actionPanel, 0, 3);

            // ── 우 (params + jog + speed) ──
            this._params.Dock = DockStyle.Fill;
            this._jog.Dock = DockStyle.Fill;
            this._trkSpeed.Dock = DockStyle.Fill;
            this._trkSpeed.Minimum = 0;
            this._trkSpeed.Maximum = 100;
            this._trkSpeed.Value = 50;
            this._trkSpeed.TickFrequency = 10;
            this._trkSpeed.Scroll += new System.EventHandler(this.OnSpeedScroll);
            this._lblSpeed.Dock = DockStyle.Fill;
            this._lblSpeed.Text = "50%";
            this._lblSpeed.Font = UiTheme.ValueFont;
            this._lblSpeed.TextAlign = ContentAlignment.MiddleCenter;
            this._speedPanel.Dock = DockStyle.Fill;
            this._speedPanel.ColumnCount = 2;
            this._speedPanel.RowCount = 1;
            this._speedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._speedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
            this._speedPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._speedPanel.Controls.Add(this._trkSpeed, 0, 0);
            this._speedPanel.Controls.Add(this._lblSpeed, 1, 0);

            this._right.Dock = DockStyle.Fill;
            this._right.ColumnCount = 1;
            this._right.RowCount = 6;
            this._right.Margin = Padding.Empty;
            this._right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            this._right.Controls.Add(this._secParam, 0, 0);
            this._right.Controls.Add(this._params, 0, 1);
            this._right.Controls.Add(this._secJog, 0, 2);
            this._right.Controls.Add(this._jog, 0, 3);
            this._right.Controls.Add(this._secSpeed, 0, 4);
            this._right.Controls.Add(this._speedPanel, 0, 5);

            // ── _main (3열) ──
            this._main.Dock = DockStyle.Fill;
            this._main.ColumnCount = 3;
            this._main.RowCount = 1;
            this._main.Margin = Padding.Empty;
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380f));
            this._main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._center, 1, 0);
            this._main.Controls.Add(this._right, 2, 0);

            // ── _lblStatus ──
            this._lblStatus.Dock = DockStyle.Fill;
            this._lblStatus.Text = "Ready.";
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.BackColor = Color.WhiteSmoke;
            this._lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this._lblStatus.Padding = new Padding(8, 0, 0, 0);

            // ── _root (main % + status 24) ──
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 2;
            this._root.Margin = Padding.Empty;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._root.Controls.Add(this._main, 0, 0);
            this._root.Controls.Add(this._lblStatus, 0, 1);

            this.Controls.Add(this._root);
            this.Name = "VisionTargetPage";
            ((System.ComponentModel.ISupportInitialize)this._result).EndInit();
            ((System.ComponentModel.ISupportInitialize)this._trkSpeed).EndInit();
            this._speedPanel.ResumeLayout(false);
            this._right.ResumeLayout(false);
            this._actionPanel.ResumeLayout(false);
            this._roiPanel.ResumeLayout(false);
            this._center.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
