using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class AutoCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private GroupBox grpSelection;
        private TableLayoutPanel selectionLayout;
        private CheckBox chkColletCal;
        private CheckBox chkPickZCal;
        private CheckBox chkPlaceZCal;
        private Label lblPlaceZGuide;
        private GroupBox grpProgress;
        private TableLayoutPanel progressLayout;
        private Label lblCurrentTarget;
        private ProgressBar progressCalibration;
        private ListBox lstHistory;
        private Label lblStatus;
        private TableLayoutPanel buttonLayout;
        private Button btnReload;
        private Button btnSave;
        private Button btnStart;
        private Button btnStop;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grpSelection = new System.Windows.Forms.GroupBox();
            this.selectionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chkColletCal = new System.Windows.Forms.CheckBox();
            this.chkPickZCal = new System.Windows.Forms.CheckBox();
            this.chkPlaceZCal = new System.Windows.Forms.CheckBox();
            this.lblPlaceZGuide = new System.Windows.Forms.Label();
            this.grpProgress = new System.Windows.Forms.GroupBox();
            this.progressLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblCurrentTarget = new System.Windows.Forms.Label();
            this.progressCalibration = new System.Windows.Forms.ProgressBar();
            this.lstHistory = new System.Windows.Forms.ListBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.grpSelection.SuspendLayout();
            this.selectionLayout.SuspendLayout();
            this.grpProgress.SuspendLayout();
            this.progressLayout.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grpSelection, 0, 1);
            this.rootLayout.Controls.Add(this.grpProgress, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Controls.Add(this.buttonLayout, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(10);
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 155F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.rootLayout.Size = new System.Drawing.Size(980, 720);
            this.rootLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(10, 10);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(960, 50);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "AUTO CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpSelection
            //
            this.grpSelection.Controls.Add(this.selectionLayout);
            this.grpSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSelection.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSelection.Location = new System.Drawing.Point(10, 66);
            this.grpSelection.Margin = new System.Windows.Forms.Padding(0, 6, 0, 4);
            this.grpSelection.Name = "grpSelection";
            this.grpSelection.Padding = new System.Windows.Forms.Padding(10);
            this.grpSelection.Size = new System.Drawing.Size(960, 145);
            this.grpSelection.TabIndex = 1;
            this.grpSelection.TabStop = false;
            this.grpSelection.Text = "사용 항목 선택 및 저장";
            //
            // selectionLayout
            //
            this.selectionLayout.ColumnCount = 3;
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.selectionLayout.Controls.Add(this.chkColletCal, 0, 0);
            this.selectionLayout.Controls.Add(this.chkPickZCal, 1, 0);
            this.selectionLayout.Controls.Add(this.chkPlaceZCal, 2, 0);
            this.selectionLayout.Controls.Add(this.lblPlaceZGuide, 0, 1);
            this.selectionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectionLayout.Location = new System.Drawing.Point(10, 28);
            this.selectionLayout.Margin = new System.Windows.Forms.Padding(0);
            this.selectionLayout.Name = "selectionLayout";
            this.selectionLayout.RowCount = 2;
            this.selectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.selectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.selectionLayout.SetColumnSpan(this.lblPlaceZGuide, 3);
            this.selectionLayout.Size = new System.Drawing.Size(940, 107);
            this.selectionLayout.TabIndex = 0;
            //
            // chkColletCal
            //
            this.chkColletCal.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkColletCal.BackColor = System.Drawing.Color.Gainsboro;
            this.chkColletCal.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkColletCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkColletCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkColletCal.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.chkColletCal.Location = new System.Drawing.Point(5, 5);
            this.chkColletCal.Margin = new System.Windows.Forms.Padding(5);
            this.chkColletCal.Name = "chkColletCal";
            this.chkColletCal.Size = new System.Drawing.Size(303, 52);
            this.chkColletCal.TabIndex = 0;
            this.chkColletCal.Text = "COLLET CAL 사용";
            this.chkColletCal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkColletCal.UseVisualStyleBackColor = false;
            //
            // chkPickZCal
            //
            this.chkPickZCal.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPickZCal.BackColor = System.Drawing.Color.Gainsboro;
            this.chkPickZCal.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPickZCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkPickZCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPickZCal.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.chkPickZCal.Location = new System.Drawing.Point(318, 5);
            this.chkPickZCal.Margin = new System.Windows.Forms.Padding(5);
            this.chkPickZCal.Name = "chkPickZCal";
            this.chkPickZCal.Size = new System.Drawing.Size(303, 52);
            this.chkPickZCal.TabIndex = 1;
            this.chkPickZCal.Text = "PICK Z CAL 사용";
            this.chkPickZCal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPickZCal.UseVisualStyleBackColor = false;
            //
            // chkPlaceZCal
            //
            this.chkPlaceZCal.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlaceZCal.BackColor = System.Drawing.Color.Gainsboro;
            this.chkPlaceZCal.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPlaceZCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkPlaceZCal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlaceZCal.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.chkPlaceZCal.Location = new System.Drawing.Point(631, 5);
            this.chkPlaceZCal.Margin = new System.Windows.Forms.Padding(5);
            this.chkPlaceZCal.Name = "chkPlaceZCal";
            this.chkPlaceZCal.Size = new System.Drawing.Size(304, 52);
            this.chkPlaceZCal.TabIndex = 2;
            this.chkPlaceZCal.Text = "PLACE Z CAL 사용";
            this.chkPlaceZCal.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPlaceZCal.UseVisualStyleBackColor = false;
            //
            // lblPlaceZGuide
            //
            this.lblPlaceZGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceZGuide.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPlaceZGuide.ForeColor = System.Drawing.Color.DimGray;
            this.lblPlaceZGuide.Location = new System.Drawing.Point(5, 62);
            this.lblPlaceZGuide.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblPlaceZGuide.Name = "lblPlaceZGuide";
            this.lblPlaceZGuide.Size = new System.Drawing.Size(930, 45);
            this.lblPlaceZGuide.TabIndex = 3;
            this.lblPlaceZGuide.Text = "진행 순서: Front 4→3→2→1, Rear 4→3→2→1 / PlaceZ는 Good Stage 기준 8회 측정하며 Good·NG Stage 모두 Empty 확인";
            this.lblPlaceZGuide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpProgress
            //
            this.grpProgress.Controls.Add(this.progressLayout);
            this.grpProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProgress.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpProgress.Location = new System.Drawing.Point(10, 215);
            this.grpProgress.Margin = new System.Windows.Forms.Padding(0);
            this.grpProgress.Name = "grpProgress";
            this.grpProgress.Padding = new System.Windows.Forms.Padding(10);
            this.grpProgress.Size = new System.Drawing.Size(960, 369);
            this.grpProgress.TabIndex = 2;
            this.grpProgress.TabStop = false;
            this.grpProgress.Text = "진행 상태";
            //
            // progressLayout
            //
            this.progressLayout.ColumnCount = 1;
            this.progressLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.progressLayout.Controls.Add(this.lblCurrentTarget, 0, 0);
            this.progressLayout.Controls.Add(this.progressCalibration, 0, 1);
            this.progressLayout.Controls.Add(this.lstHistory, 0, 2);
            this.progressLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressLayout.Location = new System.Drawing.Point(10, 28);
            this.progressLayout.Margin = new System.Windows.Forms.Padding(0);
            this.progressLayout.Name = "progressLayout";
            this.progressLayout.RowCount = 3;
            this.progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.progressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.progressLayout.Size = new System.Drawing.Size(940, 331);
            this.progressLayout.TabIndex = 0;
            //
            // lblCurrentTarget
            //
            this.lblCurrentTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentTarget.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblCurrentTarget.Location = new System.Drawing.Point(3, 0);
            this.lblCurrentTarget.Name = "lblCurrentTarget";
            this.lblCurrentTarget.Size = new System.Drawing.Size(934, 45);
            this.lblCurrentTarget.TabIndex = 0;
            this.lblCurrentTarget.Text = "대기";
            this.lblCurrentTarget.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // progressCalibration
            //
            this.progressCalibration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressCalibration.Location = new System.Drawing.Point(3, 50);
            this.progressCalibration.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.progressCalibration.Name = "progressCalibration";
            this.progressCalibration.Size = new System.Drawing.Size(934, 25);
            this.progressCalibration.TabIndex = 1;
            //
            // lstHistory
            //
            this.lstHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstHistory.Font = new System.Drawing.Font("Consolas", 9F);
            this.lstHistory.FormattingEnabled = true;
            this.lstHistory.HorizontalScrollbar = true;
            this.lstHistory.ItemHeight = 18;
            this.lstHistory.Location = new System.Drawing.Point(3, 83);
            this.lstHistory.Name = "lstHistory";
            this.lstHistory.Size = new System.Drawing.Size(934, 245);
            this.lstHistory.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(10, 590);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(0, 6, 0, 4);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(960, 50);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "설정을 확인한 후 START를 누르세요.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonLayout
            //
            this.buttonLayout.ColumnCount = 5;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.buttonLayout.Controls.Add(this.btnReload, 0, 0);
            this.buttonLayout.Controls.Add(this.btnSave, 1, 0);
            this.buttonLayout.Controls.Add(this.btnStart, 2, 0);
            this.buttonLayout.Controls.Add(this.btnStop, 3, 0);
            this.buttonLayout.Controls.Add(this.btnClose, 4, 0);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(10, 644);
            this.buttonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 1;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Size = new System.Drawing.Size(960, 66);
            this.buttonLayout.TabIndex = 4;
            //
            // btnReload
            //
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(4, 7);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(164, 52);
            this.btnReload.TabIndex = 0;
            this.btnReload.Text = "RELOAD";
            this.btnReload.UseVisualStyleBackColor = true;
            //
            // btnSave
            //
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new System.Drawing.Point(176, 7);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(164, 52);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = true;
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(238, 82, 24);
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(348, 7);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(260, 52);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = false;
            //
            // btnStop
            //
            this.btnStop.BackColor = System.Drawing.Color.Firebrick;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Enabled = false;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(616, 7);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(164, 52);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = false;
            //
            // btnClose
            //
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(788, 7);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(168, 52);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // events
            //
            this.chkColletCal.CheckedChanged += new System.EventHandler(this.chkCalibration_CheckedChanged);
            this.chkPickZCal.CheckedChanged += new System.EventHandler(this.chkCalibration_CheckedChanged);
            this.chkPlaceZCal.CheckedChanged += new System.EventHandler(this.chkCalibration_CheckedChanged);
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutoCalibrationDialog_FormClosing);
            //
            // AutoCalibrationDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(980, 720);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutoCalibrationDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AUTO CALIBRATION";
            this.rootLayout.ResumeLayout(false);
            this.grpSelection.ResumeLayout(false);
            this.selectionLayout.ResumeLayout(false);
            this.grpProgress.ResumeLayout(false);
            this.progressLayout.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
