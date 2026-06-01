using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks;
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
    public partial class GeneralBottomForm : MechaSys.SoftBricks.Hmi.Forms.Semi.BottomForm
    {
        #region Constructor
        public GeneralBottomForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handler
        private void buttons_CheckedChanged(object sender, EventArgs e)
        {
            ChildForm side = null;
            RadioButton radioButton = sender as RadioButton;

            if(radioButton == null) return;
            if(radioButton.Checked == false) return;
            if(Sys.Equipment == null) return;

            if(sender == this.buttonOperation)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideOperation) as ChildForm;
            }
            else if(sender == this.buttonConfiguration)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideConfiguration) as ChildForm;
            }
            else if(sender == this.buttonMaintenance)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideMaintenance) as ChildForm;
            }
            else if(sender == this.buttonRecipe)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideRecipe) as ChildForm;
            }
            else if(sender == this.buttonDataLog)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideDataLog) as ChildForm;
            }
            else if(sender == this.buttonUtility)
            {
                side = Sys.Equipment.MenuFormProvider.GetForm(EquipmentMenuFormProvider.FormEnum.SideUtility) as ChildForm;
            }

            if(side != null)
            {
                this.Cursor = Cursors.WaitCursor;
                FormManager.ShowWindow(side);
                this.Cursor = Cursors.Default;
            }
        }

        private void buttonLogOn_Click(object sender, EventArgs e)
        {
            string message = "", title = "";
            User user = UserManager.GetLogOnUser();
            DialogResult result = DialogResult.Cancel;

            if(UserManager.Configuration.Body.SecurityEnabled == false) return;

            if(user == null)
            {
                // show log on dialog window
                Form form = Sys.Equipment.EquipmentFormProvider.GetForm(EquipmentFormProvider.FormEnum.LogOn);
                if(this.MdiParent != null)
                    form.ShowDialog(this.MdiParent);
                else if(this.Owner != null)
                    form.ShowDialog(this.Owner);
                else
                    form.ShowDialog();
            }
            else
            {
                message = "Are you sure you want to log off?";
                title = "Log Off";

                if(this.MdiParent != null)
                    result = MsgBox.Show(this.MdiParent, message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                else if(this.Owner != null)
                    result = MsgBox.Show(this.Owner, message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                else
                    result = MsgBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if(result != DialogResult.OK) return;
                UserManager.LogOff();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            SemiMechanicalDryRunAssistant assistant = null;

            if(Sys.Equipment == null) return;
            assistant = Sys.Equipment.MechanicalDryRunAssistant as SemiMechanicalDryRunAssistant;

            this.panelMechanicalDryRun.Visible = assistant != null && (0 < assistant.AppliedOptions.Count || assistant.RepetitionSpecification.Enabled);
            if(this.panelMechanicalDryRun.Visible == false) return;

            try
            {
                foreach(string option in assistant.AppliedOptions)
                {
                    if(this.listBoxAppliedOptions.Items.Contains(option) == false)
                        this.listBoxAppliedOptions.Items.Add(option);
                }
                for(int i = this.listBoxAppliedOptions.Items.Count - 1; 0 <= i; i--)
                {
                    if(assistant.AppliedOptions.Contains(this.listBoxAppliedOptions.Items[i]) == false)
                        this.listBoxAppliedOptions.Items.RemoveAt(i);
                }

                this.textBoxControlJobRepetition.Text = assistant.RepetitionSpecification.RepetitionCount.ToString();
                this.labelControlJobCompletionCount.Text = assistant.RepetitionSpecification.CompletionCount.ToString();
            }
            catch { }
        }
        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            // default menu
            if(user == null)
            {
                if(function == "Operation")
                    return true;
                else
                    return false;
            }

            if(function == "Configuration")
            {
                accessibility = user != null && user.IsInRole(UserRole.ConfigurationView);
            }
            else if(function == "Maintenance")
            {
                accessibility = user != null && user.IsInRole(UserRole.MaintenanceView);
            }
            else if(function == "Recipe")
            {
                accessibility = user != null && user.IsInRole(UserRole.RecipeView);
            }
            else if(function == "DataLog")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "Utility")
            {
                accessibility = true;
            }

            return accessibility;
        }

        public SideForm ShowSideWindow(EquipmentMenuFormProvider.FormEnum kind)
        {
            if(kind == EquipmentMenuFormProvider.FormEnum.SideRecipe)
            {
                this.ActiveControl = this.buttonRecipe;
                this.buttonRecipe.Checked = true;
            }

            return Sys.Equipment.MenuFormProvider.GetForm(kind) as SideForm;
        }
        #endregion

        #region ChildForm Members
        protected override void ManageAccess()
        {
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == true)
            {
                // go to default menu
                if(user == null && this.buttonOperation.Checked == false)
                    this.buttonOperation.Checked = true;

                this.buttonLogOn.Text = user != null ? "Log Off" : "Log On";
            }

            this.buttonOperation.Enabled = this.GetAccessibility("Operation");
            this.buttonConfiguration.Enabled = this.GetAccessibility("Configuration");
            this.buttonMaintenance.Enabled = this.GetAccessibility("Maintenance");
            this.buttonRecipe.Enabled = this.GetAccessibility("Recipe");
            this.buttonDataLog.Enabled = this.GetAccessibility("DataLog");
            this.buttonUtility.Enabled = this.GetAccessibility("Utility");
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(UserManager.Configuration.Body.SecurityEnabled == true)
            {
                this.buttonLogOn.Visible = true;
                this.buttonOperation.Checked = true;
                this.buttonUtility.Visible = true;
            }
            else
            {
                this.buttonLogOn.Visible = false;
                this.buttonMaintenance.Checked = true;
                this.buttonUtility.Visible = false;
            }

            this.buttonConfiguration.Text = QMCSystem.Translate(this.buttonConfiguration.Text);
            this.buttonDataLog.Text = QMCSystem.Translate(this.buttonDataLog.Text);
            this.buttonLogOn.Text = QMCSystem.Translate(this.buttonLogOn.Text);
            this.buttonMaintenance.Text = QMCSystem.Translate(this.buttonMaintenance.Text);
            this.buttonOperation.Text = QMCSystem.Translate(this.buttonOperation.Text);
            this.buttonRecipe.Text = QMCSystem.Translate(this.buttonRecipe.Text);
            this.buttonUtility.Text = QMCSystem.Translate(this.buttonUtility.Text);
        }

        protected override void OnHiding(EventArgs e)
        {
            base.OnHiding(e);

            this.timer.Enabled = false;
        }

        protected override void OnShowing(EventArgs e)
        {
            base.OnShowing(e);

            this.timer.Enabled = true;
        }
        #endregion
    }
}
