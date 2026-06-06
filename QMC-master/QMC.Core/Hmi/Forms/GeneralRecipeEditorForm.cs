using MechaSys.SoftBricks.Recipes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Hmi.Forms
{
    public partial class GeneralRecipeEditorForm : MechaSys.SoftBricks.Hmi.Forms.Semi.RecipeEditorForm
    {
        #region Constructor
        public GeneralRecipeEditorForm(RecipeClass recipeClass)
            : base(recipeClass)
        {
            InitializeComponent();
        }
        public GeneralRecipeEditorForm() : this(null) { }
        #endregion
    }
}
