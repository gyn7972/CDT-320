using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class HistoryTab
    {
        internal SidebarButton BtnAlarm;
        internal SidebarButton BtnEvent;
        internal SidebarButton BtnMessageEdit;

        private void InitializeComponent()
        {
            this.BtnAlarm = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnEvent = new QMC.CDT_320.Ui.Controls.SidebarButton();
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
            // BtnMessageEdit
            //
            this.BtnMessageEdit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnMessageEdit.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnMessageEdit.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnMessageEdit.Name = "BtnMessageEdit";
            this.BtnMessageEdit.Selected = false;
            this.BtnMessageEdit.Size = new System.Drawing.Size(184, 46);
            this.BtnMessageEdit.TabIndex = 3;
            this.BtnMessageEdit.Text = "MESSAGE EDIT";
            //
            // HistoryTab
            //
            this.Name = "HistoryTab";
            this.PnlSidebarButtons.Controls.Add(this.BtnAlarm);
            this.PnlSidebarButtons.Controls.Add(this.BtnEvent);
            this.PnlSidebarButtons.Controls.Add(this.BtnMessageEdit);
            this.PnlSidebarButtons.ResumeLayout(false);
            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
