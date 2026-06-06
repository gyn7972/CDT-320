using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Die subset editor. Values are stored in RecipeProject.Die.</summary>
    public partial class DieSubsetPage : SubsetPageBase
    {
        public DieSubsetPage() : base("recipe.dieSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var d = _project.Die ?? new DieSubset();
            _tbName.Text = d.DieSpecName ?? "";
            _nW.Value = (decimal)d.WidthMm;
            _nH.Value = (decimal)d.HeightMm;
            _nT.Value = (decimal)d.ThicknessMm;
            _nWLow.Value = (decimal)d.ChipLowerSpecLimitWidth;
            _nWUp.Value = (decimal)d.ChipUpperSpecLimitWidth;
            _nHLow.Value = (decimal)d.ChipLowerSpecLimitHeight;
            _nHUp.Value = (decimal)d.ChipUpperSpecLimitHeight;
            _nChipDepth.Value = (decimal)d.ChippingDepthMax;
            _nChipLen.Value = (decimal)d.ChippingLengthMax;
            _nForeign.Value = (decimal)d.ForeignSizeMax;
        }

        protected override void SaveToRecipe()
        {
            var d = _project.Die ?? (_project.Die = new DieSubset());
            d.DieSpecName = _tbName.Text;
            d.WidthMm = (double)_nW.Value;
            d.HeightMm = (double)_nH.Value;
            d.ThicknessMm = (double)_nT.Value;
            d.ChipLowerSpecLimitWidth = (double)_nWLow.Value;
            d.ChipUpperSpecLimitWidth = (double)_nWUp.Value;
            d.ChipLowerSpecLimitHeight = (double)_nHLow.Value;
            d.ChipUpperSpecLimitHeight = (double)_nHUp.Value;
            d.ChippingDepthMax = (double)_nChipDepth.Value;
            d.ChippingLengthMax = (double)_nChipLen.Value;
            d.ForeignSizeMax = (double)_nForeign.Value;
        }
    }
}