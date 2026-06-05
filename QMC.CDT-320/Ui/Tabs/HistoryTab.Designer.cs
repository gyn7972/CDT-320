using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class HistoryTab
    {
        internal SidebarButton BtnAlarm;
        internal SidebarButton BtnWarning;
        internal SidebarButton BtnEvent;
        internal SidebarButton BtnData;
        internal SidebarButton BtnWork;
        internal SidebarButton BtnMessageEdit;
        internal System.Windows.Forms.Panel PnlMessageSeparator;

        private void InitializeComponent()
        {
            this.BtnAlarm = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnWarning = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnEvent = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnData = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnWork = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.BtnMessageEdit = new QMC.CDT_320.Ui.Controls.SidebarButton();
            this.PnlMessageSeparator = new System.Windows.Forms.Panel();
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
            // BtnWarning
            //
            this.BtnWarning.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnWarning.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnWarning.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnWarning.Name = "BtnWarning";
            this.BtnWarning.Selected = false;
            this.BtnWarning.Size = new System.Drawing.Size(184, 46);
            this.BtnWarning.TabIndex = 1;
            this.BtnWarning.Text = "WARNING";
            //
            // BtnEvent
            //
            this.BtnEvent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnEvent.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnEvent.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnEvent.Name = "BtnEvent";
            this.BtnEvent.Selected = false;
            this.BtnEvent.Size = new System.Drawing.Size(184, 46);
            this.BtnEvent.TabIndex = 2;
            this.BtnEvent.Text = "EVENT";
            //
            // BtnData
            //
            this.BtnData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnData.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnData.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnData.Name = "BtnData";
            this.BtnData.Selected = false;
            this.BtnData.Size = new System.Drawing.Size(184, 46);
            this.BtnData.TabIndex = 3;
            this.BtnData.Text = "DATA";
            //
            // BtnWork
            //
            this.BtnWork.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnWork.Font = new System.Drawing.Font("?? ??", 11F);
            this.BtnWork.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnWork.Name = "BtnWork";
            this.BtnWork.Selected = false;
            this.BtnWork.Size = new System.Drawing.Size(184, 46);
            this.BtnWork.TabIndex = 4;
            this.BtnWork.Text = "WORK";
            //
            // PnlMessageSeparator
            //
            this.PnlMessageSeparator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.PnlMessageSeparator.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.PnlMessageSeparator.Name = "PnlMessageSeparator";
            this.PnlMessageSeparator.Size = new System.Drawing.Size(202, 2);
            this.PnlMessageSeparator.TabIndex = 5;
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
            this.PnlSidebarButtons.Controls.Add(this.BtnWarning);
            this.PnlSidebarButtons.Controls.Add(this.BtnEvent);
            this.PnlSidebarButtons.Controls.Add(this.BtnData);
            this.PnlSidebarButtons.Controls.Add(this.BtnWork);
            this.PnlSidebarButtons.Controls.Add(this.PnlMessageSeparator);
            this.PnlSidebarButtons.Controls.Add(this.BtnMessageEdit);
            this.PnlSidebarButtons.ResumeLayout(false);
            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
