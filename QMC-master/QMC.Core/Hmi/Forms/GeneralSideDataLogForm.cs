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
using System.Security;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSideDataLogForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralSideDataLogForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            if(function == "Alarm")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "Material")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "Tracking")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "ControlJob")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "Job")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }
            else if(function == "Log")
            {
                accessibility = user != null && user.IsInRole(UserRole.LogView);
            }

            return accessibility;
        }
        #endregion

        #region Event Handler
        private void buttons_CheckedChanged(object sender, EventArgs e)
        {
            ChildForm child = null;
            RadioButton radioButton = sender as RadioButton;

            if(radioButton == null) return;
            if(radioButton.Checked == false) return;

            if(sender == this.buttonAlarm)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.AlarmHistory) as ChildForm;
            }
            else if(sender == this.buttonTracking)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.TrackingHistory) as ChildForm;
            }
            else if(sender == this.buttonMaterial)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.MaterialViewer) as ChildForm;
            }
            else if(sender == this.buttonControlJob)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.ControlJobViewer) as ChildForm;
            }
            else if(sender == this.buttonJob)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.JobOrderViewer) as ChildForm;
            }
            else if(sender == this.buttonLog)
            {
                child = Sys.Equipment.DataLogFormProvider.GetForm(EquipmentDataLogFormProvider.FormEnum.LogViewer) as ChildForm;
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

            this.buttonAlarm.Enabled = this.GetAccessibility("Alarm");
            this.buttonMaterial.Enabled = this.GetAccessibility("Material");
            this.buttonTracking.Enabled = this.GetAccessibility("Tracking");
            this.buttonControlJob.Enabled = this.GetAccessibility("ControlJob");
            this.buttonJob.Enabled = this.GetAccessibility("Job");
            this.buttonLog.Enabled = this.GetAccessibility("Log");
        }

        protected override void OnShowing(EventArgs e)
        {
            object sender = null;

            base.OnShowing(e);

            sender = this.buttonAlarm.Checked ? this.buttonAlarm :
                this.buttonTracking.Checked ? this.buttonTracking :
                this.buttonMaterial.Checked ? this.buttonMaterial :
                this.buttonControlJob.Checked ? this.buttonControlJob :
                this.buttonJob.Checked ? this.buttonJob :
                this.buttonLog.Checked ? this.buttonLog : null;

            this.buttons_CheckedChanged(sender, e);
        }
        #endregion

        #region MyRegion
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.buttonAlarm.Text = QMCSystem.Translate(this.buttonAlarm.Text);
            this.buttonControlJob.Text = QMCSystem.Translate(this.buttonControlJob.Text);
            this.buttonJob.Text = QMCSystem.Translate(this.buttonJob.Text);
            this.buttonLog.Text = QMCSystem.Translate(this.buttonLog.Text);
            this.buttonMaterial.Text = QMCSystem.Translate(this.buttonMaterial.Text);
            this.buttonTracking.Text = QMCSystem.Translate(this.buttonTracking.Text);
        }
        #endregion
    }
}
