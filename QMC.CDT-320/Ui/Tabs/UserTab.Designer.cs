using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class UserTab
    {
        internal SidebarButton BtnUser;

        private void InitializeComponent()
        {
            this.BtnUser = new QMC.CDT_320.Ui.Controls.SidebarButton();
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
            // BtnUser
            //
            this.BtnUser.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnUser.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.BtnUser.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.BtnUser.Name = "BtnUser";
            this.BtnUser.Selected = false;
            this.BtnUser.Size = new System.Drawing.Size(184, 46);
            this.BtnUser.TabIndex = 0;
            this.BtnUser.Text = "USER";
            //
            // UserTab
            //
            this.Name = "UserTab";
            this.PnlSidebarButtons.Controls.Add(this.BtnUser);
            this.PnlSidebarButtons.ResumeLayout(false);
            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
