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

            // 버튼들 (IC 직렬화 가능 — 헬퍼 호출 인라인)
            this._btnGrab.Location = new Point(720, 364); this._btnGrab.Size = new Size(170, 44); this._btnGrab.Text = "GRAB";
            this._btnGrab.FlatStyle = FlatStyle.Flat; this._btnGrab.Font = UiTheme.ButtonFont; this._btnGrab.BackColor = Color.White; this._btnGrab.ForeColor = Color.Black;
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            this._btnLoad.Location = new Point(900, 364); this._btnLoad.Size = new Size(170, 44); this._btnLoad.Text = "LOAD";
            this._btnLoad.FlatStyle = FlatStyle.Flat; this._btnLoad.Font = UiTheme.ButtonFont; this._btnLoad.BackColor = Color.White; this._btnLoad.ForeColor = Color.Black;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            this._btnSave.Location = new Point(1080, 364); this._btnSave.Size = new Size(180, 44); this._btnSave.Text = "SAVE";
            this._btnSave.FlatStyle = FlatStyle.Flat; this._btnSave.Font = UiTheme.ButtonFont; this._btnSave.BackColor = Color.White; this._btnSave.ForeColor = Color.Black;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            this._btnTrain.Location = new Point(720, 416); this._btnTrain.Size = new Size(265, 50); this._btnTrain.Text = "TRAIN";
            this._btnTrain.FlatStyle = FlatStyle.Flat; this._btnTrain.Font = UiTheme.ButtonFont; this._btnTrain.BackColor = UiTheme.Accent; this._btnTrain.ForeColor = Color.White;
            this._btnTrain.Click += new System.EventHandler(this.OnTrainClick);
            this._btnMatch.Location = new Point(995, 416); this._btnMatch.Size = new Size(265, 50); this._btnMatch.Text = "MATCH";
            this._btnMatch.FlatStyle = FlatStyle.Flat; this._btnMatch.Font = UiTheme.ButtonFont; this._btnMatch.BackColor = UiTheme.Accent; this._btnMatch.ForeColor = Color.White;
            this._btnMatch.Click += new System.EventHandler(this.OnMatchClick);
            this._btnEditSearch.Location = new Point(720, 478); this._btnEditSearch.Size = new Size(265, 36); this._btnEditSearch.Text = "Edit SEARCH ROI (drag)";
            this._btnEditSearch.FlatStyle = FlatStyle.Flat; this._btnEditSearch.Font = UiTheme.ButtonFont; this._btnEditSearch.BackColor = Color.LightYellow; this._btnEditSearch.ForeColor = Color.Black;
            this._btnEditSearch.Click += new System.EventHandler(this.OnEditSearchClick);
            this._btnEditTrain.Location = new Point(995, 478); this._btnEditTrain.Size = new Size(265, 36); this._btnEditTrain.Text = "Edit TRAIN ROI (drag)";
            this._btnEditTrain.FlatStyle = FlatStyle.Flat; this._btnEditTrain.Font = UiTheme.ButtonFont; this._btnEditTrain.BackColor = Color.LightYellow; this._btnEditTrain.ForeColor = Color.Black;
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
    }
}
