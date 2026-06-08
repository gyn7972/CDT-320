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

        private Label        _title;
        private CameraView   _cam;
        private JogBox       _jog;
        private DataGridView _result;
        private Button _btnGrab, _btnLoad, _btnSave, _btnTrain, _btnMatch, _btnEditSearch, _btnEditTrain;
        private Label  _lblStatus;

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
            this._cam = new CameraView();
            this._jog = new JogBox();
            this._result = new DataGridView();
            this._btnGrab = new Button();
            this._btnLoad = new Button();
            this._btnSave = new Button();
            this._btnTrain = new Button();
            this._btnMatch = new Button();
            this._btnEditSearch = new Button();
            this._btnEditTrain = new Button();
            this._lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)this._result).BeginInit();
            this.SuspendLayout();

            // _title (Text 는 런타임 — 원본도 BuildLayout 시점 Text="" )
            this._title.Dock = DockStyle.Top;
            this._title.Height = 26;
            this._title.BackColor = UiTheme.StatusBarBg;
            this._title.ForeColor = Color.White;
            this._title.Font = UiTheme.SectionFont;
            this._title.TextAlign = ContentAlignment.MiddleLeft;
            this._title.Padding = new Padding(10, 0, 0, 0);

            // _cam
            this._cam.Location = new Point(6, 34);
            this._cam.Size = new Size(700, 500);
            this._cam.RoiEdited += new Action<string, Roi>(this.OnCamRoiEdited);

            // _jog
            this._jog.Location = new Point(456, 544);
            this._jog.Size = new Size(260, 280);

            // _result
            this._result.Location = new Point(720, 34);
            this._result.Size = new Size(540, 320);
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

            // 버튼들
            InitBtn(this._btnGrab, "GRAB", 720, 364, 170, 44, Color.White, Color.Black);
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            InitBtn(this._btnLoad, "LOAD", 900, 364, 170, 44, Color.White, Color.Black);
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            InitBtn(this._btnSave, "SAVE", 1080, 364, 180, 44, Color.White, Color.Black);
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            InitBtn(this._btnTrain, "TRAIN", 720, 416, 265, 50, UiTheme.Accent, Color.White);
            this._btnTrain.Click += new System.EventHandler(this.OnTrainClick);
            InitBtn(this._btnMatch, "MATCH", 995, 416, 265, 50, UiTheme.Accent, Color.White);
            this._btnMatch.Click += new System.EventHandler(this.OnMatchClick);
            InitBtn(this._btnEditSearch, "Edit SEARCH ROI (drag)", 720, 478, 265, 36, Color.LightYellow, Color.Black);
            this._btnEditSearch.Click += new System.EventHandler(this.OnEditSearchClick);
            InitBtn(this._btnEditTrain, "Edit TRAIN ROI (drag)", 995, 478, 265, 36, Color.LightYellow, Color.Black);
            this._btnEditTrain.Click += new System.EventHandler(this.OnEditTrainClick);

            // _lblStatus
            this._lblStatus.Location = new Point(720, 524);
            this._lblStatus.Size = new Size(540, 30);
            this._lblStatus.Text = "Ready.";
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._lblStatus.BorderStyle = BorderStyle.FixedSingle;
            this._lblStatus.BackColor = Color.WhiteSmoke;
            this._lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this._lblStatus.Padding = new Padding(8, 0, 0, 0);

            // FinderPage (원본 추가순서: title→cam→[illum:Code]→jog→result→버튼→status→[liveTuning:Code])
            this.Controls.Add(this._title);
            this.Controls.Add(this._cam);
            this.Controls.Add(this._jog);
            this.Controls.Add(this._result);
            this.Controls.Add(this._btnGrab);
            this.Controls.Add(this._btnLoad);
            this.Controls.Add(this._btnSave);
            this.Controls.Add(this._btnTrain);
            this.Controls.Add(this._btnMatch);
            this.Controls.Add(this._btnEditSearch);
            this.Controls.Add(this._btnEditTrain);
            this.Controls.Add(this._lblStatus);
            this.Name = "FinderPage";
            ((System.ComponentModel.ISupportInitialize)this._result).EndInit();
            this.ResumeLayout(false);
        }

        private void InitBtn(Button b, string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            b.Location = new Point(x, y); b.Size = new Size(w, h); b.Text = text;
            b.FlatStyle = FlatStyle.Flat; b.Font = UiTheme.ButtonFont; b.BackColor = bg; b.ForeColor = fg;
        }
    }
}
