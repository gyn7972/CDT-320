namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class VisionAlignPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Panel camPanel;
        private System.Windows.Forms.Label lblCameraInfo;
        private System.Windows.Forms.Label lblLive;
        private System.Windows.Forms.TableLayoutPanel sideLayout;
        private System.Windows.Forms.GroupBox grpAction;
        private System.Windows.Forms.TableLayoutPanel actionBody;
        private System.Windows.Forms.GroupBox grpResult;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAutoAlign;
        private QMC.CDT_320.Ui.Controls.ActionButton btnManualAlign;
        private QMC.CDT_320.Ui.Controls.ActionButton btnFirstMark;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSecondMark;
        private QMC.CDT_320.Ui.Controls.ActionButton btnThetaMatch;
        private QMC.CDT_320.Ui.Controls.ActionButton btnXyMatch;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnClose;
        private System.Windows.Forms.TableLayoutPanel resultLayout;
        private System.Windows.Forms.Label lblDeltaXCaption;
        private System.Windows.Forms.Label lblDeltaXValue;
        private System.Windows.Forms.Label lblDeltaYCaption;
        private System.Windows.Forms.Label lblDeltaYValue;
        private System.Windows.Forms.Label lblDeltaThetaCaption;
        private System.Windows.Forms.Label lblDeltaThetaValue;
        private System.Windows.Forms.Label lblScoreCaption;
        private System.Windows.Forms.Label lblScoreValue;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.camPanel = new System.Windows.Forms.Panel();
            this.lblCameraInfo = new System.Windows.Forms.Label();
            this.lblLive = new System.Windows.Forms.Label();
            this.sideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBody = new System.Windows.Forms.TableLayoutPanel();
            this.grpResult = new System.Windows.Forms.GroupBox();
            this.btnAutoAlign = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnManualAlign = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnFirstMark = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSecondMark = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatch = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatch = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnClose = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.resultLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblDeltaXCaption = new System.Windows.Forms.Label();
            this.lblDeltaXValue = new System.Windows.Forms.Label();
            this.lblDeltaYCaption = new System.Windows.Forms.Label();
            this.lblDeltaYValue = new System.Windows.Forms.Label();
            this.lblDeltaThetaCaption = new System.Windows.Forms.Label();
            this.lblDeltaThetaValue = new System.Windows.Forms.Label();
            this.lblScoreCaption = new System.Windows.Forms.Label();
            this.lblScoreValue = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.camPanel.SuspendLayout();
            this.sideLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBody.SuspendLayout();
            this.grpResult.SuspendLayout();
            this.resultLayout.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.camPanel, 0, 1);
            this.rootLayout.Controls.Add(this.sideLayout, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(0);
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.SetColumnSpan(this.lblHeader, 2);
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "VISION ALIGN";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.camPanel.BackColor = System.Drawing.Color.Black;
            this.camPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.camPanel.Controls.Add(this.lblCameraInfo);
            this.camPanel.Controls.Add(this.lblLive);
            this.camPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.camPanel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblCameraInfo.AutoSize = true;
            this.lblCameraInfo.BackColor = System.Drawing.Color.Black;
            this.lblCameraInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblCameraInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblCameraInfo.Location = new System.Drawing.Point(8, 8);
            this.lblCameraInfo.Text = "STAGE\r\nW : 640\r\nH : 480";
            this.lblLive.BackColor = System.Drawing.Color.Black;
            this.lblLive.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblLive.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblLive.ForeColor = System.Drawing.Color.LightGreen;
            this.lblLive.Height = 22;
            this.lblLive.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLive.Text = "Live";
            this.lblLive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.sideLayout.ColumnCount = 1;
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Controls.Add(this.grpAction, 0, 0);
            this.sideLayout.Controls.Add(this.grpResult, 0, 2);
            this.sideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideLayout.Margin = new System.Windows.Forms.Padding(0);
            this.sideLayout.RowCount = 3;
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 402F));
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 176F));
            //
            // grpAction
            //
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionBody);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpAction.Margin = new System.Windows.Forms.Padding(0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Padding = new System.Windows.Forms.Padding(4);
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            //
            // actionBody
            //
            this.actionBody.BackColor = System.Drawing.Color.White;
            this.actionBody.ColumnCount = 1;
            this.actionBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBody.Controls.Add(this.btnAutoAlign, 0, 0);
            this.actionBody.Controls.Add(this.btnManualAlign, 0, 1);
            this.actionBody.Controls.Add(this.btnFirstMark, 0, 2);
            this.actionBody.Controls.Add(this.btnSecondMark, 0, 3);
            this.actionBody.Controls.Add(this.btnThetaMatch, 0, 4);
            this.actionBody.Controls.Add(this.btnXyMatch, 0, 5);
            this.actionBody.Controls.Add(this.btnSave, 0, 6);
            this.actionBody.Controls.Add(this.btnClose, 0, 7);
            this.actionBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBody.Margin = new System.Windows.Forms.Padding(0);
            this.actionBody.Name = "actionBody";
            this.actionBody.Padding = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.actionBody.RowCount = 8;
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            //
            // grpResult
            //
            this.grpResult.BackColor = System.Drawing.Color.White;
            this.grpResult.Controls.Add(this.resultLayout);
            this.grpResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResult.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpResult.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpResult.Margin = new System.Windows.Forms.Padding(0);
            this.grpResult.Name = "grpResult";
            this.grpResult.Padding = new System.Windows.Forms.Padding(4);
            this.grpResult.TabStop = false;
            this.grpResult.Text = "RESULT";
            this.btnAutoAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoAlign.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnAutoAlign.Text = "AUTO ALIGN";
            this.btnManualAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualAlign.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnManualAlign.Text = "MANUAL ALIGN";
            this.btnFirstMark.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFirstMark.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnFirstMark.Text = "FIRST MARK";
            this.btnSecondMark.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSecondMark.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnSecondMark.Text = "SECOND MARK";
            this.btnThetaMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatch.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnThetaMatch.Text = "THETA MATCH";
            this.btnXyMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatch.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnXyMatch.Text = "X_Y MATCH";
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnSave.Text = "SAVE";
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnClose.Text = "CLOSE";
            this.resultLayout.ColumnCount = 2;
            this.resultLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.resultLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.resultLayout.Margin = new System.Windows.Forms.Padding(0);
            this.resultLayout.Padding = new System.Windows.Forms.Padding(1);
            this.resultLayout.Controls.Add(this.lblDeltaXCaption, 0, 0);
            this.resultLayout.Controls.Add(this.lblDeltaXValue, 1, 0);
            this.resultLayout.Controls.Add(this.lblDeltaYCaption, 0, 1);
            this.resultLayout.Controls.Add(this.lblDeltaYValue, 1, 1);
            this.resultLayout.Controls.Add(this.lblDeltaThetaCaption, 0, 2);
            this.resultLayout.Controls.Add(this.lblDeltaThetaValue, 1, 2);
            this.resultLayout.Controls.Add(this.lblScoreCaption, 0, 3);
            this.resultLayout.Controls.Add(this.lblScoreValue, 1, 3);
            this.resultLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultLayout.RowCount = 4;
            this.resultLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.resultLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.resultLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.resultLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.lblDeltaXCaption.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblDeltaXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaXCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDeltaXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDeltaXCaption.Text = "Delta X";
            this.lblDeltaXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDeltaXValue.BackColor = System.Drawing.Color.White;
            this.lblDeltaXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaXValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDeltaXValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDeltaXValue.Text = "0.000";
            this.lblDeltaXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblDeltaYCaption.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblDeltaYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaYCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDeltaYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDeltaYCaption.Text = "Delta Y";
            this.lblDeltaYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDeltaYValue.BackColor = System.Drawing.Color.White;
            this.lblDeltaYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaYValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDeltaYValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDeltaYValue.Text = "0.000";
            this.lblDeltaYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblDeltaThetaCaption.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblDeltaThetaCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaThetaCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaThetaCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDeltaThetaCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDeltaThetaCaption.Text = "Delta Theta";
            this.lblDeltaThetaCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDeltaThetaValue.BackColor = System.Drawing.Color.White;
            this.lblDeltaThetaValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDeltaThetaValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeltaThetaValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDeltaThetaValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDeltaThetaValue.Text = "0.000 deg";
            this.lblDeltaThetaValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblScoreCaption.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblScoreCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblScoreCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblScoreCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblScoreCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblScoreCaption.Text = "Score";
            this.lblScoreCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblScoreValue.BackColor = System.Drawing.Color.White;
            this.lblScoreValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblScoreValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblScoreValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblScoreValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblScoreValue.Text = "0.0";
            this.lblScoreValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionAlignPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.resultLayout.ResumeLayout(false);
            this.grpResult.ResumeLayout(false);
            this.actionBody.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.sideLayout.ResumeLayout(false);
            this.camPanel.ResumeLayout(false);
            this.camPanel.PerformLayout();
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }

    partial class WaferMapOpenPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox grpList;
        private System.Windows.Forms.ListBox lbMapFiles;
        private System.Windows.Forms.Panel mapPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grpList = new System.Windows.Forms.GroupBox();
            this.lbMapFiles = new System.Windows.Forms.ListBox();
            this.mapPanel = new System.Windows.Forms.Panel();
            this.rootLayout.SuspendLayout();
            this.grpList.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.mapPanel, 0, 1);
            this.rootLayout.Controls.Add(this.grpList, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(0);
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.SetColumnSpan(this.lblHeader, 2);
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "WAFER MAP OPEN";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpList
            //
            this.grpList.BackColor = System.Drawing.Color.White;
            this.grpList.Controls.Add(this.lbMapFiles);
            this.grpList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpList.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpList.Margin = new System.Windows.Forms.Padding(0);
            this.grpList.Name = "grpList";
            this.grpList.Padding = new System.Windows.Forms.Padding(4);
            this.grpList.TabStop = false;
            this.grpList.Text = "MAP FILE LIST";
            //
            // lbMapFiles
            //
            this.lbMapFiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lbMapFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbMapFiles.Font = new System.Drawing.Font("Consolas", 9F);
            this.lbMapFiles.Margin = new System.Windows.Forms.Padding(0);
            this.lbMapFiles.Name = "lbMapFiles";
            //
            // mapPanel
            //
            this.mapPanel.BackColor = System.Drawing.Color.Black;
            this.mapPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapPanel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "WaferMapOpenPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.grpList.ResumeLayout(false);
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}

