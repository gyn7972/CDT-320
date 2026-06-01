using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechaSys.SoftBricks.Recipes;

namespace QMC.Jobs
{
    #region GeneralSemiProcessJobStateController
    public class GeneralSemiProcessJobStateController : SemiProcessJobStateController
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSemiProcessJobStateController(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralSemiProcessJobStateController() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        protected virtual int OnAssignRecipe(string identifier)
        {
            int ret = 0;
            RecipeIdentifier recipeIdentifier = null;
            IHaveUnionRecipeAssigner haveUnionRecipeAssigner = null;
            UnionRecipeAssigner assigner = null;
            UnionRecipe recipe = null;

            haveUnionRecipeAssigner = Sys.Equipment as IHaveUnionRecipeAssigner;
            if (haveUnionRecipeAssigner == null) return ret;

            assigner = ((IHaveUnionRecipeAssigner)Sys.Equipment).RecipeAssigner;
            if (assigner == null) return ret;

            recipeIdentifier = RecipeIdentifier.Parse(identifier);
            if(recipeIdentifier == null) return ret;

            // 김영남 이사 요청으로 주석 처리
            //if(assigner.AssignedRecipeIdentifier == recipeIdentifier)
            //{
            //    return ret;
            //}
            recipe = RecipeManager.LoadRecipe<UnionRecipe>(recipeIdentifier);
            if (recipe == null || recipe.Verified == false) return ret;
            
            if ((ret = assigner.Assign(recipe)) != 0) return ret;

            return ret;
        }
        #endregion

        #region ProcessJobStateController Members
        protected override void OnBeforeSettingUp(ProcessJob processJob, EquipmentEventArgs e)
        {
            base.OnBeforeSettingUp(processJob, e);
            if (e.Result != 0) return;

            if ((e.Result = this.OnAssignRecipe(processJob.RecipeID)) != 0) return;
        }
        #endregion

        #region Element Members
        protected new GeneralSemiProcessJobStateControllerConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralSemiProcessJobStateControllerConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralSemiProcessJobStateControllerConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region GeneralSemiProcessJobStateControllerConstructConfiguration
    [Serializable]
    public class GeneralSemiProcessJobStateControllerConstructConfiguration : SemiProcessJobStateControllerConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSemiProcessJobStateControllerConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralSemiProcessJobStateControllerConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}
