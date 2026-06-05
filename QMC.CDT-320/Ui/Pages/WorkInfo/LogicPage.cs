namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class LogicPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        public LogicPage()
        {
            InitializeComponent();
            this.contentHost.Controls.Add(new LogicDetailPage { Dock = System.Windows.Forms.DockStyle.Fill });
        }
    }
}

