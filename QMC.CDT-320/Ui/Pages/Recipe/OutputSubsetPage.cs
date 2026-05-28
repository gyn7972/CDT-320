using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class OutputSubsetPage : SubsetPageBase
    {
        public OutputSubsetPage() : base("recipe.outputSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var o = _project.Output ?? new OutputSubset();
            _nGoodMax.Value = o.GoodPlateMaxSlots;
            _nNgMax.Value = o.NgPlateMaxSlots;
            _nDiesPerWafer.Value = o.DiesPerWafer;
            _nWafersPerBatch.Value = o.WafersPerOutputBatch;
            _cbAutoBin.Checked = o.AutoBinTransition;
            _cbAlarmFull.Checked = o.AlarmOnFull;
            _tbDefaultGood.Text = o.DefaultGoodCassette ?? "";
        }

        protected override void SaveToRecipe()
        {
            var o = _project.Output ?? (_project.Output = new OutputSubset());
            o.GoodPlateMaxSlots = (int)_nGoodMax.Value;
            o.NgPlateMaxSlots = (int)_nNgMax.Value;
            o.DiesPerWafer = (int)_nDiesPerWafer.Value;
            o.WafersPerOutputBatch = (int)_nWafersPerBatch.Value;
            o.AutoBinTransition = _cbAutoBin.Checked;
            o.AlarmOnFull = _cbAlarmFull.Checked;
            o.DefaultGoodCassette = _tbDefaultGood.Text;
        }
    }
}