namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class LogicPage : PageBase
    {
        public LogicPage()
        {
            InitializeComponent();
            this.contentHost.Controls.Add(new LogicDetailPage { Dock = System.Windows.Forms.DockStyle.Fill });
        }
    }
}
