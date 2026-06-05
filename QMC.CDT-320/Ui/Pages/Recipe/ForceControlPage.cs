namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ForceControlPage : PageBase
    {
        public ForceControlPage()
        {
            InitializeComponent();
            LoadSampleRows();
        }

        private void LoadSampleRows()
        {
            gridDo.Rows.Clear();
            gridDi.Rows.Clear();

            for (int i = 0; i < 10; i++)
            {
                gridDo.Rows.Add(i + 1, "Y" + i.ToString("D3"), "Sample DO " + i, "OFF");
                gridDi.Rows.Add(i + 1, "X" + i.ToString("D3"), "Sample DI " + i, "OFF");
            }
        }
    }
}
