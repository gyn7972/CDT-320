using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Recipes;
using MechaSys.SoftBricks.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MechaSys.SoftBricks.Diagnostics;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSideRecipeForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Define
        private class FormAndButton
        {
            public Form m_Form;
            public GraphicRadioButton m_Button;
        }

        private class FormAndButtonCollection : Collection<FormAndButton>
        {
        }
        #endregion

        #region Field
        private FormAndButtonCollection m_FormAndButtons;
        #endregion

        #region Constructor
        public GeneralSideRecipeForm()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call
        }
        #endregion

        #region Method
        private void CreateFormAndButtons()
        {
            int controlIndex = 0;
            RecipeTreeItemCollection tree;
            Recipe recipe;
            Control control = null;
            Form form;

            tree = RecipeManager.GetCreatableRecipeTree();
            if (tree == null || tree.Count == 0) return;

            this.SuspendLayout();

            for (int i = 0; i < tree.Count; i++)
            {
                if (tree[i].Specification != null && tree[i].Specification.VisibleOnMenu == false) continue;
                if (tree[i].ClassType == RecipeClassType.Subset) continue;

                recipe = RecipeManager.CreateNewRecipeInstance(tree[i].Class);
                if (recipe == null) continue;
                if ((recipe is ISupportRecipeEditor) == false) continue;
                control = ((ISupportRecipeEditor)recipe).GetRecipeEditor(tree[i].Class);
                form = control as Form;
                if (form == null)
                {
                    form = new MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlUnionRecipeEditorForm(tree[i].Class);
                }

                Size formSize = new Size();

                formSize.Width = FormManager.FormMainSize.Width - FormManager.FormSideSize.Width;
                formSize.Height = FormManager.FormMainSize.Height - FormManager.FormTopSize.Height - FormManager.FormBottomSize.Height;

                form.Size = formSize;

                // create buttons
                GraphicRadioButton button = new GraphicRadioButton();
                button.AutoSize = false;
                button.Text = tree[i].ReadOnlyAlias;
                button.Tag = tree[i];
                button.Size = new Size(90, 90);

                button.CheckedChanged += new EventHandler(this.OnButton_Changed);

                this.flowLayoutPanel.Controls.Add(button);

                // create forms
                FormAndButton formAlias = new FormAndButton();
                formAlias.m_Form = form;
                formAlias.m_Button = (GraphicRadioButton)this.flowLayoutPanel.Controls[controlIndex];

                this.m_FormAndButtons.Add(formAlias);

                controlIndex++;
            }

            this.ResumeLayout(false);
        }

        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if (UserManager.Configuration.Body.SecurityEnabled == false)
            {
                return true;
            }

            if (function == "Recipe")
            {
                accessibility = user != null && user.IsInRole(UserRole.RecipeView);
            }

            return accessibility;
        }

        public RecipeEditorForm ShowRecipeWindow(RecipeClass recipeclass)
        {
            RecipeEditorForm form = null;

            for (int i = 0; i < this.m_FormAndButtons.Count; i++)
            {
                RecipeTreeItem item = this.m_FormAndButtons[i].m_Button.Tag as RecipeTreeItem;
                if (item == null) continue;
                if (item.Class != recipeclass) continue;

                form = this.m_FormAndButtons[i].m_Form as RecipeEditorForm;
                // added by biglake 2008/04/13
                this.ActiveControl = this.m_FormAndButtons[i].m_Button;
                this.m_FormAndButtons[i].m_Button.Checked = true;

                break;
            }

            return form;
        }
        #endregion

        #region Event Handler
        private void OnButton_Changed(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            if (radioButton == null) return;
            if (radioButton.Checked != true) return;

            RecipeTreeItem item = (RecipeTreeItem)((GraphicRadioButton)sender).Tag;

            for (int i = 0; i < this.m_FormAndButtons.Count; i++)
            {
                if (sender == this.m_FormAndButtons[i].m_Button)
                {
                    if (this.m_FormAndButtons[i].m_Form != null)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        FormManager.ShowWindow((ChildForm)this.m_FormAndButtons[i].m_Form);
                        this.Cursor = Cursors.Default;
                        break;
                    }
                }
            }
        }
        #endregion

        #region ChildForm Members
        protected override void OnPrepare()
        {
            base.OnPrepare();

            if (this.DesignMode == true) return;

            this.m_FormAndButtons = new FormAndButtonCollection();
            this.CreateFormAndButtons();

            // Form을 미리 만들어보자.
            int fisrt_recipe = 0;
            if (this.m_FormAndButtons.Count > 0)
                this.m_FormAndButtons[fisrt_recipe].m_Button.Checked = true;
        }

        protected override void ManageAccess()
        {
            bool accessibility = true;

            base.ManageAccess();

            accessibility = this.GetAccessibility("Recipe");
            for (int i = 0; i < this.m_FormAndButtons.Count; i++)
            {
                this.m_FormAndButtons[i].m_Button.Enabled = accessibility;
            }
        }

        protected override void OnShowing(EventArgs e)
        {
            base.OnShowing(e);

            for (int i = 0; i < this.m_FormAndButtons.Count; i++)
            {
                if (this.m_FormAndButtons[i].m_Button.Checked == true)
                {
                    this.OnButton_Changed(this.m_FormAndButtons[i].m_Button, e);
                    break;
                }
            }
        }
        #endregion
    }
}
