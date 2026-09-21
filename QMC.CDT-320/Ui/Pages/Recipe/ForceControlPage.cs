using QMC.CDT_320.Ui.Localization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ForceControlPage : PageBase
    {
        public ForceControlPage()
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
            BuildActions();
            DisableColumnSorting();
            LoadSampleRows();
        }

        /// <summary>모든 열의 헤더 클릭 정렬(오름/내림차순) 기능을 끈다.</summary>
        private void DisableColumnSorting()
        {
            foreach (DataGridViewColumn col in gridDo.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            foreach (DataGridViewColumn col in gridDi.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void BuildActions()
        {
            manualActionPanel.ColumnCount = 3;
            manualActionPanel.RowHeight = 45;
            manualActionPanel.SetItems(new[]
            {
                ManualActionItem.Create("FORCE ON", () => Task.CompletedTask),
                ManualActionItem.Create("FORCE OFF", () => Task.CompletedTask),
                ManualActionItem.Create("ALL OFF", () => Task.CompletedTask)
            });
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
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "recipeUi.forceControlPage.lblHeader.text");
            Lang.BindKey(this.grpDo, "recipeUi.forceControlPage.grpDo.text");
            Lang.BindKey(this.grpDi, "recipeUi.forceControlPage.grpDi.text");
            Lang.BindKey(this.grpActions, "recipeUi.forceControlPage.grpActions.text");
        }
    }
}
