using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Secs.Hmi;
using MechaSys.SoftBricks.Secs;
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
    public partial class GeneralSideOperationForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Field
        private GraphicRadioButton buttonHost;
        #endregion

        #region Constructor
        public GeneralSideOperationForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Method
        protected virtual bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            if(user == null)
            {
                if(function == "Monitoring")
                    return true;
                else
                    return false;
            }

            if(function == "Operation")
            {
                accessibility = user != null && user.IsInRole(UserRole.OperateJob);
            }
            else if(function == "Host")
            {
                accessibility = user != null && user.IsInRole(UserRole.HostView);
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

            if(Sys.Equipment == null) return;

            if(sender == this.buttonMonitoring)
            {
                child = Sys.Equipment.EquipmentFormProvider.GetForm(EquipmentFormProvider.FormEnum.Monitoring) as ChildForm;
            }
            else if(sender == this.buttonOperation)
            {
                child = Sys.Equipment.EquipmentFormProvider.GetForm(EquipmentFormProvider.FormEnum.JobOperation) as ChildForm;
            }
            else if(sender == this.buttonHost)
            {
                child = ((SecsSemiEquipmentFormProvider)Sys.Equipment.EquipmentFormProvider).GetForm(SecsSemiEquipmentFormProvider.FormEnum.HostMonitoring) as ChildForm;
            }

            if(child != null)
            {
                this.Cursor = Cursors.WaitCursor;
                FormManager.ShowWindow(child);
                this.Cursor = Cursors.Default;
            }
        }
        #endregion

        #region FormSide Members
        protected override void ManageAccess()
        {
            User user = UserManager.GetLogOnUser();

            base.ManageAccess();

            if(UserManager.Configuration.Body.SecurityEnabled == true)
            {
                // go to default menu
                if(user == null && this.buttonMonitoring.Checked == false)
                {
                    this.buttonMonitoring.Checked = true;
                }
            }

            this.buttonMonitoring.Enabled = this.GetAccessibility("Monitoring");
            this.buttonOperation.Enabled = this.GetAccessibility("Operation");
            if(this.buttonHost != null)
                this.buttonHost.Enabled = this.GetAccessibility("Host");
        }

        protected override void OnShowing(EventArgs e)
        {
            object sender = null;

            base.OnShowing(e);

            if(this.buttonMonitoring.Checked)
                sender = this.buttonMonitoring;
            else if(this.buttonOperation.Checked)
                sender = this.buttonOperation;
            else if(this.buttonHost != null && this.buttonHost.Checked)
                sender = this.buttonHost;
            else
                sender = null;

            this.buttons_CheckedChanged(sender, e);
        }

        protected override void OnPrepare()
        {
            LoadPort[] loadports = null;

            if(Sys.Equipment == null) return;

            base.OnPrepare();

            // for manual track
            loadports = ElementList.GetByType<LoadPort>(true);
            this.buttonOperation.Visible = loadports.Length != 0;

            // host
            if(Sys.Equipment.EventReportProcessor is SecsEquipmentEventReportProcessor)
            {
                this.buttonHost = this.CreateButton("Host");
                this.buttonHost.CheckedChanged += new EventHandler(this.buttons_CheckedChanged);

                this.flowLayoutPanel.Controls.Add(this.buttonHost);
            }

            this.buttonMonitoring.Text = QMCSystem.Translate(this.buttonMonitoring.Text);
            this.buttonOperation.Text = QMCSystem.Translate(this.buttonOperation.Text);
        }
        #endregion
    }
}
