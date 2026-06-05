using System;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Recipes;

namespace QMC.Recipes
{
    #region GeneralUnionRecipe
    [Serializable]
    public class GeneralUnionRecipe : UnionRecipe,
        ISupportRecipeEditor
    {
        #region Define
        #endregion

        #region Constructor
        public GeneralUnionRecipe() : base()
        {
            this.Body = new GeneralUnionRecipeBody();
        }
        #endregion

        #region ManagedRecipe
        public new GeneralUnionRecipeBody Body
        {
            get { return base.Body as GeneralUnionRecipeBody; }
            set { base.Body = value; }
        }
        #endregion

        #region Recipe
        protected override int OnVerify()
        {
            int ret = 0;

            if ((ret = base.OnVerify()) != 0) return ret;

            return ret;
        }
        #endregion

        #region ISupportRecipeEditor
        public virtual Control GetRecipeEditor(RecipeClass recipeClass)
        {
            return new GeneralUnionRecipeEditorForm(recipeClass);
        }
        #endregion
    }
    #endregion

    #region GeneralUnionRecipeBody
    [Serializable]
    public class GeneralUnionRecipeBody : UnionRecipeBody
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralUnionRecipeBody() : base()
        {
        }
        #endregion

        #region Property
        #endregion

        #region RecipeBody
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}