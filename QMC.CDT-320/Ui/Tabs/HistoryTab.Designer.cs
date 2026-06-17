using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class HistoryTab
    {
        internal SidebarButton BtnAlarm;
        internal SidebarButton BtnEvent;
        internal SidebarButton BtnInputSeq;
        internal SidebarButton BtnOutputSeq;
        internal SidebarButton BtnFrontHeadSeq;
        internal SidebarButton BtnRearHeadSeq;
        internal SidebarButton BtnMessageEdit;

        private void InitializeComponent()
        {
            this.BtnAlarm = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnEvent = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnInputSeq = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnOutputSeq = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnFrontHeadSeq = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnRearHeadSeq = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnMessageEdit = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.PnlSidebar.SuspendLayout();
            this.PnlSidebarButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // PnlSidebar
            //
            this.PnlSidebar.Location = new System.Drawing.Point(1678, 0);
            this.PnlSidebar.Size = new System.Drawing.Size(210, 900);
            //
            // LblSidebarHeader
            //
            this.LblSidebarHeader.Size = new System.Drawing.Size(210, 50);
            //
            // PnlContent
            //
            this.PnlContent.Size = new System.Drawing.Size(1678, 900);
            //
            // PnlSidebarButtons
            //
            //
            // BtnAlarm
            //
            this.BtnAlarm.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnAlarm.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnAlarm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnAlarm.Name = "BtnAlarm";
            this.BtnAlarm.Selected = false;
            this.BtnAlarm.Size = new System.Drawing.Size(184, 46);
            this.BtnAlarm.TabIndex = 0;
            this.BtnAlarm.Text = "ALARM";
            //
            // BtnEvent
            //
            this.BtnEvent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnEvent.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnEvent.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnEvent.Name = "BtnEvent";
            this.BtnEvent.Selected = false;
            this.BtnEvent.Size = new System.Drawing.Size(184, 46);
            this.BtnEvent.TabIndex = 1;
            this.BtnEvent.Text = "EVENT";
            //
            // BtnInputSeq
            //
            this.BtnInputSeq.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnInputSeq.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnInputSeq.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnInputSeq.Name = "BtnInputSeq";
            this.BtnInputSeq.Selected = false;
            this.BtnInputSeq.Size = new System.Drawing.Size(184, 46);
            this.BtnInputSeq.TabIndex = 2;
            this.BtnInputSeq.Text = "INPUT SEQ";
            //
            // BtnOutputSeq
            //
            this.BtnOutputSeq.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnOutputSeq.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnOutputSeq.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnOutputSeq.Name = "BtnOutputSeq";
            this.BtnOutputSeq.Selected = false;
            this.BtnOutputSeq.Size = new System.Drawing.Size(184, 46);
            this.BtnOutputSeq.TabIndex = 3;
            this.BtnOutputSeq.Text = "OUTPUT SEQ";
            //
            // BtnFrontHeadSeq
            //
            this.BtnFrontHeadSeq.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnFrontHeadSeq.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnFrontHeadSeq.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnFrontHeadSeq.Name = "BtnFrontHeadSeq";
            this.BtnFrontHeadSeq.Selected = false;
            this.BtnFrontHeadSeq.Size = new System.Drawing.Size(184, 46);
            this.BtnFrontHeadSeq.TabIndex = 4;
            this.BtnFrontHeadSeq.Text = "FRONT HEAD SEQ";
            //
            // BtnRearHeadSeq
            //
            this.BtnRearHeadSeq.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnRearHeadSeq.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnRearHeadSeq.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnRearHeadSeq.Name = "BtnRearHeadSeq";
            this.BtnRearHeadSeq.Selected = false;
            this.BtnRearHeadSeq.Size = new System.Drawing.Size(184, 46);
            this.BtnRearHeadSeq.TabIndex = 5;
            this.BtnRearHeadSeq.Text = "REAR HEAD SEQ";
            //
            // BtnMessageEdit
            //
            this.BtnMessageEdit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnMessageEdit.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnMessageEdit.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnMessageEdit.Name = "BtnMessageEdit";
            this.BtnMessageEdit.Selected = false;
            this.BtnMessageEdit.Size = new System.Drawing.Size(184, 46);
            this.BtnMessageEdit.TabIndex = 6;
            this.BtnMessageEdit.Text = "MESSAGE EDIT";
            //
            // HistoryTab
            //
            this.Name = "HistoryTab";
            this.PnlSidebarButtons.Controls.Add(this.BtnAlarm);
            this.PnlSidebarButtons.Controls.Add(this.BtnEvent);
            this.PnlSidebarButtons.Controls.Add(this.BtnInputSeq);
            this.PnlSidebarButtons.Controls.Add(this.BtnOutputSeq);
            this.PnlSidebarButtons.Controls.Add(this.BtnFrontHeadSeq);
            this.PnlSidebarButtons.Controls.Add(this.BtnRearHeadSeq);
            this.PnlSidebarButtons.Controls.Add(this.BtnMessageEdit);
            this.PnlSidebarButtons.ResumeLayout(false);
            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
