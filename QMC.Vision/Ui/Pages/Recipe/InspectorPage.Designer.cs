using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectorPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label        _title;
        private CameraView   _cam;
        private JogBox       _jog;
        private DataGridView _result;
        private Label        _lblVerdict;
        private Button _btnGrab, _btnLoad, _btnSave, _btnInspect, _btnEditRoi;
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this._title = new System.Windows.Forms.Label();
            this._cam = new QMC.Vision.Ui.Controls.CameraView();
            this._jog = new QMC.Vision.Ui.Controls.JogBox();
            this._result = new System.Windows.Forms.DataGridView();
            this._lblVerdict = new System.Windows.Forms.Label();
            this._btnGrab = new System.Windows.Forms.Button();
            this._btnLoad = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this._btnInspect = new System.Windows.Forms.Button();
            this._btnEditRoi = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this._result)).BeginInit();
            this.SuspendLayout();
            // 
            // _title
            // 
            this._title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._title.Dock = System.Windows.Forms.DockStyle.Top;
            this._title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._title.ForeColor = System.Drawing.Color.White;
            this._title.Location = new System.Drawing.Point(0, 0);
            this._title.Name = "_title";
            this._title.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._title.Size = new System.Drawing.Size(1300, 26);
            this._title.TabIndex = 0;
            this._title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cam
            // 
            this._cam.BackColor = System.Drawing.Color.Black;
            this._cam.InfoText = "STAGE\r\nW:640 H:480";
            this._cam.Location = new System.Drawing.Point(6, 34);
            this._cam.Name = "_cam";
            this._cam.ShowCrosshair = true;
            this._cam.ShowLiveLabel = true;
            this._cam.Size = new System.Drawing.Size(700, 500);
            this._cam.TabIndex = 1;
            this._cam.RoiEdited += new System.Action<string, QMC.Vision.Core.Roi>(this.OnCamRoiEdited);
            // 
            // _jog
            // 
            this._jog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._jog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._jog.Location = new System.Drawing.Point(456, 544);
            this._jog.Name = "_jog";
            this._jog.Size = new System.Drawing.Size(260, 280);
            this._jog.TabIndex = 2;
            // 
            // _result
            // 
            this._result.AllowUserToAddRows = false;
            this._result.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._result.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._result.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._result.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3});
            this._result.EnableHeadersVisualStyles = false;
            this._result.Font = new System.Drawing.Font("Consolas", 10F);
            this._result.Location = new System.Drawing.Point(720, 34);
            this._result.Name = "_result";
            this._result.ReadOnly = true;
            this._result.RowHeadersVisible = false;
            this._result.Size = new System.Drawing.Size(540, 270);
            this._result.TabIndex = 3;
            // 
            // _lblVerdict
            // 
            this._lblVerdict.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblVerdict.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblVerdict.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this._lblVerdict.ForeColor = System.Drawing.Color.DimGray;
            this._lblVerdict.Location = new System.Drawing.Point(720, 312);
            this._lblVerdict.Name = "_lblVerdict";
            this._lblVerdict.Size = new System.Drawing.Size(540, 50);
            this._lblVerdict.TabIndex = 4;
            this._lblVerdict.Text = "—";
            this._lblVerdict.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _btnGrab
            // 
            this._btnGrab.BackColor = System.Drawing.Color.White;
            this._btnGrab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnGrab.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnGrab.ForeColor = System.Drawing.Color.Black;
            this._btnGrab.Location = new System.Drawing.Point(720, 372);
            this._btnGrab.Name = "_btnGrab";
            this._btnGrab.Size = new System.Drawing.Size(170, 44);
            this._btnGrab.TabIndex = 5;
            this._btnGrab.Text = "GRAB";
            this._btnGrab.UseVisualStyleBackColor = false;
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            // 
            // _btnLoad
            // 
            this._btnLoad.BackColor = System.Drawing.Color.White;
            this._btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoad.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnLoad.ForeColor = System.Drawing.Color.Black;
            this._btnLoad.Location = new System.Drawing.Point(900, 372);
            this._btnLoad.Name = "_btnLoad";
            this._btnLoad.Size = new System.Drawing.Size(170, 44);
            this._btnLoad.TabIndex = 6;
            this._btnLoad.Text = "LOAD";
            this._btnLoad.UseVisualStyleBackColor = false;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            // 
            // _btnSave
            // 
            this._btnSave.BackColor = System.Drawing.Color.White;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.Black;
            this._btnSave.Location = new System.Drawing.Point(1080, 372);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(180, 44);
            this._btnSave.TabIndex = 7;
            this._btnSave.Text = "SAVE";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            // 
            // _btnInspect
            // 
            this._btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnInspect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnInspect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnInspect.ForeColor = System.Drawing.Color.White;
            this._btnInspect.Location = new System.Drawing.Point(720, 422);
            this._btnInspect.Name = "_btnInspect";
            this._btnInspect.Size = new System.Drawing.Size(540, 56);
            this._btnInspect.TabIndex = 8;
            this._btnInspect.Text = "INSPECT";
            this._btnInspect.UseVisualStyleBackColor = false;
            this._btnInspect.Click += new System.EventHandler(this.OnInspectClick);
            // 
            // _btnEditRoi
            // 
            this._btnEditRoi.BackColor = System.Drawing.Color.LightYellow;
            this._btnEditRoi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnEditRoi.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnEditRoi.ForeColor = System.Drawing.Color.Black;
            this._btnEditRoi.Location = new System.Drawing.Point(720, 488);
            this._btnEditRoi.Name = "_btnEditRoi";
            this._btnEditRoi.Size = new System.Drawing.Size(540, 36);
            this._btnEditRoi.TabIndex = 9;
            this._btnEditRoi.Text = "Edit INSPECTION ROI (drag)";
            this._btnEditRoi.UseVisualStyleBackColor = false;
            this._btnEditRoi.Click += new System.EventHandler(this.OnEditRoiClick);
            // 
            // _lblStatus
            // 
            this._lblStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(720, 532);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblStatus.Size = new System.Drawing.Size(540, 30);
            this._lblStatus.TabIndex = 10;
            this._lblStatus.Text = "Ready.";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Item";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Value";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Pass";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // InspectorPage
            // 
            this.Controls.Add(this._title);
            this.Controls.Add(this._cam);
            this.Controls.Add(this._jog);
            this.Controls.Add(this._result);
            this.Controls.Add(this._lblVerdict);
            this.Controls.Add(this._btnGrab);
            this.Controls.Add(this._btnLoad);
            this.Controls.Add(this._btnSave);
            this.Controls.Add(this._btnInspect);
            this.Controls.Add(this._btnEditRoi);
            this.Controls.Add(this._lblStatus);
            this.Name = "InspectorPage";
            this.Size = new System.Drawing.Size(1300, 747);
            ((System.ComponentModel.ISupportInitialize)(this._result)).EndInit();
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
    }
}
