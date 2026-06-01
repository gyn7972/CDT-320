using System;
using System.Drawing;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Recipes;

namespace QMC.Recipes
{
    public partial class GeneralUnionRecipeEditorForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlUnionRecipeEditorForm
    {
        #region Define
        #endregion

        #region Field
        private UnionRecipeAssignerReadOnlyCollection Assigners;
        private SubsetRecipeKeyedCollection m_Previous;
        #endregion

        #region Constructor
        public GeneralUnionRecipeEditorForm(RecipeClass recipeClass)
            : base(recipeClass)
        {
            InitializeComponent();
        }
        public GeneralUnionRecipeEditorForm() : this(null) { }
        #endregion

        #region Event Handler
        private void buttonAssign_Click(object sender, EventArgs e)
        {
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            if(this.Recipe == null || this.Recipe.Verified == false) return;

            foreach(UnionRecipeAssigner assigner in this.Assigners)
            {
                ars.Add(assigner.BeginAssign(this.Recipe));
            }

            if(0 < ars.Count)
                WaitingBox.ShowElement(this, ars, "Assign ...", this.Assigners[0]);
        }
        #endregion

        #region Method
        #endregion

        #region RecipeEditorForm
        public new GeneralUnionRecipe Recipe
        {
            get { return base.Recipe as GeneralUnionRecipe; }
            protected set { base.Recipe = value; }
        }

        protected new UnionRecipeSpecification RecipeSpecification
        {
            get { return base.RecipeSpecification as UnionRecipeSpecification; }
        }

        protected override void OnRecipeChanged()
        {
            base.OnRecipeChanged();

            if(this.Recipe != null && this.Recipe.Verified == true)
                this.buttonAssign.Enabled = true;
            else
                this.buttonAssign.Enabled = false;
        }

        protected override int OnSaveToMemory()
        {
            int ret = 0;

            this.m_Previous = CopyUtility.GetDeepCopy<SubsetRecipeKeyedCollection>(this.Recipe.Body.SubsetRecipes);

            if((ret = base.OnSaveToMemory()) != 0) return ret;

            return ret;
        }

        protected override void OnAfterSave(int ret)
        {
            base.OnAfterSave(ret);

            try
            {
                if(ret == 0)
                {
                    if(this.Recipe.Body.SubsetRecipes != null)
                    {
                        for(int i = 0; i < this.Recipe.Body.SubsetRecipes.Count; i++)
                        {
                            DataComparer.DeepCompareLog(DataComparer.DataTypes.Recipe, $"[{this.Recipe.Name}] - {this.Recipe.Body.SubsetRecipes[i].Class.Name}", this.m_Previous[i].Body, this.Recipe.Body.SubsetRecipes[i].Body);
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }        
        }
        #endregion

        #region ChildForm
        protected override void OnPrepare()
        {
            //this.tabControl.TabPages.Clear();
            this.buttonAssign.Enabled = false;

            base.OnPrepare();

            UnionRecipeAssigner[] assigners = null;
            assigners = UnionRecipeAssigner.GetAssigners(this.RecipeClass);
            this.Assigners = new UnionRecipeAssignerReadOnlyCollection(assigners);

            this.buttonAssign.Text = QMCSystem.Translate(this.buttonAssign.Text);
        }
        #endregion
    }
}