using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectorTargetPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _main;
        private Label _lblStatus;
        // 좌 (카메라 전용 — 확대)
        private TableLayoutPanel _left;
        private Label _secCam;
        private CameraView _cam;
        // 중 (ACTION 콤팩트 + 검사결과 + verdict)
        private TableLayoutPanel _center;
        private Label _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnInspect, _btnEditRoi;
        private Label _secResult;
        private DataGridView _result;
        private Label _lblVerdict;
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
            this._btnInspect = new Button(); this._btnEditRoi = new Button();
            this._secResult = new Label();
            this._result = new DataGridView();
            this._lblVerdict = new Label();
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
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // ── 섹션 라벨 ──
            this._secCam.Dock = DockStyle.Fill; this._secCam.Text = "CAMERA"; this._secCam.BackColor = UiTheme.StatusBarBg; this._secCam.ForeColor = Color.White; this._secCam.Font = UiTheme.SectionFont; this._secCam.TextAlign = ContentAlignment.MiddleLeft; this._secCam.Padding = new Padding(8, 0, 0, 0);
            this._secAction.Dock = DockStyle.Fill; this._secAction.Text = "ACTION"; this._secAction.BackColor = UiTheme.StatusBarBg; this._secAction.ForeColor = Color.White; this._secAction.Font = UiTheme.SectionFont; this._secAction.TextAlign = ContentAlignment.MiddleLeft; this._secAction.Padding = new Padding(8, 0, 0, 0);
            this._secResult.Dock = DockStyle.Fill; this._secResult.Text = "검사 결과"; this._secResult.BackColor = UiTheme.StatusBarBg; this._secResult.ForeColor = Color.White; this._secResult.Font = UiTheme.SectionFont; this._secResult.TextAlign = ContentAlignment.MiddleLeft; this._secResult.Padding = new Padding(8, 0, 0, 0);
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

            // ── 중 ACTION (콤팩트: INSPECT span + GRAB/LOAD + 이미지저장/EDIT ROI) ──
            // GRAB/LOAD/이미지저장은 CameraView 내장 툴바로 통일(중복 제거). 검사 전용 버튼만 유지.
            this._btnInspect.Dock = DockStyle.Fill; this._btnInspect.Text = "INSPECT"; this._btnInspect.FlatStyle = FlatStyle.Flat; this._btnInspect.Font = UiTheme.SectionFont; this._btnInspect.BackColor = UiTheme.Accent; this._btnInspect.ForeColor = Color.White;
            this._btnInspect.Click += new System.EventHandler(this.OnInspectClick);
            this._btnEditRoi.Dock = DockStyle.Fill; this._btnEditRoi.Text = "EDIT INSPECTION ROI"; this._btnEditRoi.FlatStyle = FlatStyle.Flat; this._btnEditRoi.Font = UiTheme.ButtonFont; this._btnEditRoi.BackColor = Color.LightYellow; this._btnEditRoi.ForeColor = Color.Black;
            this._btnEditRoi.Click += new System.EventHandler(this.OnEditRoiClick);
            this._actionPanel.Dock = DockStyle.Fill;
            this._actionPanel.ColumnCount = 2; this._actionPanel.RowCount = 2; this._actionPanel.Margin = Padding.Empty;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            this._actionPanel.Controls.Add(this._btnInspect, 0, 0);
            this._actionPanel.SetColumnSpan(this._btnInspect, 2);
            this._actionPanel.Controls.Add(this._btnEditRoi, 0, 1);
            this._actionPanel.SetColumnSpan(this._btnEditRoi, 2);

            // ── 중 검사결과 + verdict ──
            this._result.Dock = DockStyle.Fill;
            this._result.AllowUserToAddRows = false; this._result.RowHeadersVisible = false;
            this._result.BackgroundColor = Color.White; this._result.Font = UiTheme.ValueFont;
            this._result.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._result.Columns.Add("Item", "항목");
            this._result.Columns.Add("Value", "값");
            this._result.Columns.Add("Pass", "결과");
            this._lblVerdict.Dock = DockStyle.Fill; this._lblVerdict.Text = "-"; this._lblVerdict.Font = UiTheme.SectionFont; this._lblVerdict.ForeColor = Color.White; this._lblVerdict.BackColor = Color.Gray; this._lblVerdict.TextAlign = ContentAlignment.MiddleCenter;

            this._center.Dock = DockStyle.Fill;
            this._center.ColumnCount = 1; this._center.RowCount = 5; this._center.Margin = Padding.Empty;
            this._center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            this._center.Controls.Add(this._secAction, 0, 0);
            this._center.Controls.Add(this._actionPanel, 0, 1);
            this._center.Controls.Add(this._secResult, 0, 2);
            this._center.Controls.Add(this._result, 0, 3);
            this._center.Controls.Add(this._lblVerdict, 0, 4);

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
            this.Name = "InspectorTargetPage";
            this.Size = new Size(1710, 832);
            ((System.ComponentModel.ISupportInitialize)this._result).EndInit();
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
