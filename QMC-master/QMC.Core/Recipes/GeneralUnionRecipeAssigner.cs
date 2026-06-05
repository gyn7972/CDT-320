using System;
using System.ComponentModel;


using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Recipes;

namespace QMC.Recipes
{
    #region GeneralUnionRecipeAssigner
    public class GeneralUnionRecipeAssigner : UnionRecipeAssigner
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralUnionRecipeAssigner(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralUnionRecipeAssigner() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Event Handler
        #endregion

        #region Method
        #endregion

        #region UnionRecipeAssigner
        protected override int OnAssign(UnionRecipe recipe)
        {
            int ret = 0;
            return ret;
        }
        #endregion

        #region Element
        protected new GeneralUnionRecipeAssignerConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralUnionRecipeAssignerConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralUnionRecipeAssignerConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region GeneralUnionRecipeAssignerConstructConfiguration
    [Serializable]
    public class GeneralUnionRecipeAssignerConstructConfiguration : UnionRecipeAssignerConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralUnionRecipeAssignerConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralUnionRecipeAssignerConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}