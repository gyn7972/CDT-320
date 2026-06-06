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
    public partial class GeneralSideUtilityForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSideUtilityForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Property

        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            if(function == "Account")
                accessibility = user != null && user.IsInRole(UserRole.UserView);

            return accessibility;
        }
        #endregion

        #region Event handler
        private void buttons_CheckedChanged(object sender, EventArgs e)
        {
            ChildForm child = null;
            RadioButton radioButton = sender as RadioButton;

            if(radioButton == null) return;
            if(radioButton.Checked == false) return;

            if(sender == this.buttonAccount)
            {
                child = Sys.Equipment.UtilityFormProvider.GetForm(EquipmentUtilityFormProvider.FormEnum.Account) as ChildForm;
            }

            if(child != null)
            {
                this.Cursor = Cursors.WaitCursor;
                FormManager.ShowWindow(child);
                this.Cursor = Cursors.Default;
            }
        }
        #endregion

        #region ChildForm Members
        protected override void ManageAccess()
        {
            base.ManageAccess();

            this.buttonAccount.Enabled = this.GetAccessibility("Account");
        }

        protected override void OnShowing(EventArgs e)
        {
            base.OnShowing(e);

            object sender = this.buttonAccount.Checked ? this.buttonAccount : null;

            this.buttons_CheckedChanged(sender, e);
        }
        #endregion
    }
}
