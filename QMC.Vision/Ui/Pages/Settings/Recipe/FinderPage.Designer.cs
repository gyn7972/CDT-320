using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class FinderPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label            _title;
        private TableLayoutPanel _root;
        private TableLayoutPanel _main;
        // 좌 (카메라 + 하단 조그/조명)
        private TableLayoutPanel _left;
        private Label            _secCam;
        private CameraView       _cam;
        private TableLayoutPanel _bottomLeft;
        private JogBox           _jog;
        private Panel            _illumHost;   // 런타임 InspectionLightPanel 호스트(.cs BuildChildPanels)
        // 우 (결과 + 액션)
        private TableLayoutPanel _right;
        private Label            _secResult;
        private DataGridView     _result;
        private Label            _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnGrab, _btnLoad, _btnSave, _btnTrain, _btnMatch, _btnEditSearch, _btnEditTrain;
        private Label  _lblStatus;
        private Panel  _ulCam, _ulResult, _ulAction;   // 섹션 라벨 주황 밑줄

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { StopLive(); _liveTimer?.Dispose(); } catch { }   // Stage 87 — 라이브 grab 정지
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._title = new Label();
            this._root = new TableLayoutPanel();
            this._main = new TableLayoutPanel();
            this._left = new TableLayoutPanel();
            this._secCam = new Label();
            this._cam = new CameraView();
            this._bottomLeft = new TableLayoutPanel();
            this._jog = new JogBox();
            this._illumHost = new Panel();
            this._right = new TableLayoutPanel();
            this._secResult = new Label();
            this._result = new DataGridView();
            this._secAction = new Label();
            this._actionPanel = new TableLayoutPanel();
            this._btnGrab = new Button();
            this._btnLoad = new Button();
            this._btnSave = new Button();
            this._btnTrain = new Button();
            this._btnMatch = new Button();
            this._btnEditSearch = new Button();
            this._btnEditTrain = new Button();
            this._lblStatus = new Label();
            this._ulCam = new Panel();
            this._ulResult = new Panel();
            this._ulAction = new Panel();
            this._root.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._bottomLeft.SuspendLayout();
            this._right.SuspendLayout();
            this._actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this._result).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // ── 상단 페이지 타이틀(주황, 런타임 Text) ──
            this._title.Dock = DockStyle.Top;
            this._title.Height = 26;
            this._title.BackColor = UiTheme.StatusBarBg;
            this._title.ForeColor = Color.White;
            this._title.Font = UiTheme.SectionFont;
            this._title.TextAlign = ContentAlignment.MiddleLeft;
            this._title.Padding = new Padding(10, 0, 0, 0);

            // ── 섹션 라벨(공통: 회색 배경 + 주황 밑줄) ──
            // _secCam
            this._secCam.Dock = DockStyle.Fill;
            this._secCam.Text = "CAMERA";
            this._secCam.BackColor = Color.FromArgb(232, 234, 237);
            this._secCam.ForeColor = Color.FromArgb(48, 52, 58);
            this._secCam.Font = UiTheme.SectionFont;
            this._secCam.TextAlign = ContentAlignment.MiddleLeft;
            this._secCam.Padding = new Padding(8, 0, 0, 0);
            this._ulCam.Dock = DockStyle.Bottom;
            this._ulCam.Height = 2;
            this._ulCam.BackColor = Color.FromArgb(217, 119, 6);
            this._secCam.Controls.Add(this._ulCam);
            // _secResult
            this._secResult.Dock = DockStyle.Fill;
            this._secResult.Text = "MATCH RESULT";
            this._secResult.BackColor = Color.FromArgb(232, 234, 237);
            this._secResult.ForeColor = Color.FromArgb(48, 52, 58);
            this._secResult.Font = UiTheme.SectionFont;
            this._secResult.TextAlign = ContentAlignment.MiddleLeft;
            this._secResult.Padding = new Padding(8, 0, 0, 0);
            this._ulResult.Dock = DockStyle.Bottom;
            this._ulResult.Height = 2;
            this._ulResult.BackColor = Color.FromArgb(217, 119, 6);
            this._secResult.Controls.Add(this._ulResult);
            // _secAction
            this._secAction.Dock = DockStyle.Fill;
            this._secAction.Text = "ACTION";
            this._secAction.BackColor = Color.FromArgb(232, 234, 237);
            this._secAction.ForeColor = Color.FromArgb(48, 52, 58);
            this._secAction.Font = UiTheme.SectionFont;
            this._secAction.TextAlign = ContentAlignment.MiddleLeft;
            this._secAction.Padding = new Padding(8, 0, 0, 0);
            this._ulAction.Dock = DockStyle.Bottom;
            this._ulAction.Height = 2;
            this._ulAction.BackColor = Color.FromArgb(217, 119, 6);
            this._secAction.Controls.Add(this._ulAction);

            // ── 좌: 카메라 ──
            this._cam.Dock = DockStyle.Fill; this._cam.BackColor = Color.Black;
            this._cam.RoiEdited += new Action<string, Roi>(this.OnCamRoiEdited);

            // ── 좌 하단: 조그 + 조명 호스트 ──
            this._jog.Dock = DockStyle.Fill; this._jog.Margin = new Padding(0, 0, 6, 0);
            this._illumHost.Dock = DockStyle.Fill; this._illumHost.BackColor = UiTheme.MainBg;
            this._bottomLeft.Dock = DockStyle.Fill; this._bottomLeft.Margin = Padding.Empty;
            this._bottomLeft.ColumnCount = 2; this._bottomLeft.RowCount = 1;
            this._bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270f));
            this._bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._bottomLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._bottomLeft.Controls.Add(this._jog, 0, 0);
            this._bottomLeft.Controls.Add(this._illumHost, 1, 0);

            this._left.Dock = DockStyle.Fill; this._left.Margin = Padding.Empty;
            this._left.ColumnCount = 1; this._left.RowCount = 3;
            this._left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 290f));
            this._left.Controls.Add(this._secCam, 0, 0);
            this._left.Controls.Add(this._cam, 0, 1);
            this._left.Controls.Add(this._bottomLeft, 0, 2);

            // ── 우: 결과 그리드 ──
            this._result.Dock = DockStyle.Fill;
            this._result.ReadOnly = true;
            this._result.AllowUserToAddRows = false;
            this._result.RowHeadersVisible = false;
            this._result.BackgroundColor = Color.White;
            this._result.Font = UiTheme.ValueFont;
            this._result.EnableHeadersVisualStyles = false;
            this._result.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._result.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0x50, 0x50, 0x50);
            this._result.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this._result.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this._result.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this._result.Columns.Add("Index", "Index");
            this._result.Columns.Add("X", "X");
            this._result.Columns.Add("Y", "Y");
            this._result.Columns.Add("R", "R");
            this._result.Columns.Add("Score", "Score");

            // ── 우: 액션 버튼(균일 3×3) — Neutral=흰/검, Primary=주황/흰, Edit=연노랑/검 ──
            // _btnGrab (Neutral)
            this._btnGrab.Dock = DockStyle.Fill;
            this._btnGrab.Margin = new Padding(3);
            this._btnGrab.Text = "GRAB";
            this._btnGrab.FlatStyle = FlatStyle.Flat;
            this._btnGrab.Font = UiTheme.ButtonFont;
            this._btnGrab.BackColor = Color.White;
            this._btnGrab.ForeColor = Color.Black;
            this._btnGrab.Click += new EventHandler(this.OnGrabClick);
            // _btnLoad (Neutral)
            this._btnLoad.Dock = DockStyle.Fill;
            this._btnLoad.Margin = new Padding(3);
            this._btnLoad.Text = "LOAD";
            this._btnLoad.FlatStyle = FlatStyle.Flat;
            this._btnLoad.Font = UiTheme.ButtonFont;
            this._btnLoad.BackColor = Color.White;
            this._btnLoad.ForeColor = Color.Black;
            this._btnLoad.Click += new EventHandler(this.OnLoadClick);
            // _btnSave (Neutral)
            this._btnSave.Dock = DockStyle.Fill;
            this._btnSave.Margin = new Padding(3);
            this._btnSave.Text = "SAVE";
            this._btnSave.FlatStyle = FlatStyle.Flat;
            this._btnSave.Font = UiTheme.ButtonFont;
            this._btnSave.BackColor = Color.White;
            this._btnSave.ForeColor = Color.Black;
            this._btnSave.Click += new EventHandler(this.OnSaveClick);
            // _btnTrain (Primary)
            this._btnTrain.Dock = DockStyle.Fill;
            this._btnTrain.Margin = new Padding(3);
            this._btnTrain.Text = "TRAIN";
            this._btnTrain.FlatStyle = FlatStyle.Flat;
            this._btnTrain.Font = UiTheme.ButtonFont;
            this._btnTrain.BackColor = UiTheme.Accent;
            this._btnTrain.ForeColor = Color.White;
            this._btnTrain.Click += new EventHandler(this.OnTrainClick);
            // _btnMatch (Primary)
            this._btnMatch.Dock = DockStyle.Fill;
            this._btnMatch.Margin = new Padding(3);
            this._btnMatch.Text = "MATCH";
            this._btnMatch.FlatStyle = FlatStyle.Flat;
            this._btnMatch.Font = UiTheme.ButtonFont;
            this._btnMatch.BackColor = UiTheme.Accent;
            this._btnMatch.ForeColor = Color.White;
            this._btnMatch.Click += new EventHandler(this.OnMatchClick);
            // _btnEditSearch (Edit)
            this._btnEditSearch.Dock = DockStyle.Fill;
            this._btnEditSearch.Margin = new Padding(3);
            this._btnEditSearch.Text = "Edit SEARCH ROI";
            this._btnEditSearch.FlatStyle = FlatStyle.Flat;
            this._btnEditSearch.Font = UiTheme.ButtonFont;
            this._btnEditSearch.BackColor = Color.LightYellow;
            this._btnEditSearch.ForeColor = Color.Black;
            this._btnEditSearch.Click += new EventHandler(this.OnEditSearchClick);
            // _btnEditTrain (Edit)
            this._btnEditTrain.Dock = DockStyle.Fill;
            this._btnEditTrain.Margin = new Padding(3);
            this._btnEditTrain.Text = "Edit TRAIN ROI";
            this._btnEditTrain.FlatStyle = FlatStyle.Flat;
            this._btnEditTrain.Font = UiTheme.ButtonFont;
            this._btnEditTrain.BackColor = Color.LightYellow;
            this._btnEditTrain.ForeColor = Color.Black;
            this._btnEditTrain.Click += new EventHandler(this.OnEditTrainClick);

            this._actionPanel.Dock = DockStyle.Fill; this._actionPanel.Margin = Padding.Empty;
            this._actionPanel.ColumnCount = 3; this._actionPanel.RowCount = 3;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            this._actionPanel.Controls.Add(this._btnGrab, 0, 0);
            this._actionPanel.Controls.Add(this._btnLoad, 1, 0);
            this._actionPanel.Controls.Add(this._btnSave, 2, 0);
            this._actionPanel.Controls.Add(this._btnTrain, 0, 1);
            this._actionPanel.Controls.Add(this._btnMatch, 1, 1);
            this._actionPanel.Controls.Add(this._btnEditSearch, 0, 2);
            this._actionPanel.Controls.Add(this._btnEditTrain, 1, 2);

            this._right.Dock = DockStyle.Fill; this._right.Margin = Padding.Empty;
            this._right.ColumnCount = 1; this._right.RowCount = 4;
            this._right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 156f));
            this._right.Controls.Add(this._secResult, 0, 0);
            this._right.Controls.Add(this._result, 0, 1);
            this._right.Controls.Add(this._secAction, 0, 2);
            this._right.Controls.Add(this._actionPanel, 0, 3);

            // ── _main (2열) ──
            this._main.Dock = DockStyle.Fill; this._main.Margin = Padding.Empty;
            this._main.ColumnCount = 2; this._main.RowCount = 1;
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            this._main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._right, 1, 0);

            // ── _lblStatus ──
            this._lblStatus.Dock = DockStyle.Fill;
            this._lblStatus.Text = "Ready.";
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.BackColor = Color.WhiteSmoke;
            this._lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this._lblStatus.Padding = new Padding(8, 0, 0, 0);

            // ── _root ──
            this._root.Dock = DockStyle.Fill; this._root.Margin = Padding.Empty;
            this._root.ColumnCount = 1; this._root.RowCount = 2;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            this._root.Controls.Add(this._main, 0, 0);
            this._root.Controls.Add(this._lblStatus, 0, 1);

            // FinderPage — Fill root 가 먼저, Top title 이 나중(Dock 순서상 title 이 위)
            this.Controls.Add(this._root);
            this.Controls.Add(this._title);
            this.Name = "FinderPage";
            ((System.ComponentModel.ISupportInitialize)this._result).EndInit();
            this._actionPanel.ResumeLayout(false);
            this._right.ResumeLayout(false);
            this._bottomLeft.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}
