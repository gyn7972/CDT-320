using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class MessageEditPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        public MessageEditPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadSampleRows();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("hist.msgEdit");
            lblHeader.Tag = "i18n:hist.msgEdit";
            btnSave.Text = Lang.T("common.save");
            btnAdd.Text = Lang.T("common.add");
            btnDelete.Text = Lang.T("common.delete");
        }

        private void LoadSampleRows()
        {
            grid.Rows.Clear();
            grid.Rows.Add("90100", "EVENT", "(MAIN MENU) OPERATION PAGE button clicked.", "(MAIN MENU) OPERATION PAGE button clicked.");
            grid.Rows.Add("90101", "EVENT", "(MAIN MENU) WORKING PAGE button clicked.", "(MAIN MENU) WORKING PAGE button clicked.");
            grid.Rows.Add("8100", "ALARM", "PICK failed", "PICK failed");
            grid.Rows.Add("8101", "ALARM", "PLACE failed", "PLACE failed");
            grid.Rows.Add("7200", "WARN", "Vacuum low", "Vacuum low");
        }
    }
}
