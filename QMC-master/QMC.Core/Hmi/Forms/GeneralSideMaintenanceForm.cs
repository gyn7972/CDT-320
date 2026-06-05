using System;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Secs;
using MechaSys.SoftBricks.Secs.Hmi;
using MechaSys.SoftBricks.Security;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSideMaintenanceForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Field
        #endregion

        #region Field
        private GraphicRadioButton buttonHost;

        private NotSupportedForm m_FormNotSupported;
        #endregion

        #region Constructor
        public GeneralSideMaintenanceForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            User user = UserManager.GetLogOnUser();
            bool accessibility = true;

            if(UserManager.Configuration.Body.SecurityEnabled == false) return true;

            if(function == "Equipment")
            {
                accessibility = user != null && user.IsInRole(UserRole.MaintenanceView);
            }
            else if(function == "Module")
            {
                accessibility = user != null && user.IsInRole(UserRole.MaintenanceView);
            }
            else if(function == "Host")
            {
                accessibility = user != null && user.IsInRole(UserRole.MaintenanceView);
            }
            else
            {
                accessibility = user != null && user.IsInRole(UserRole.MaintenanceView);
            }

            return accessibility;
        }
        #endregion

        #region Event Handler
        private void FormSideMaintenance_Load(object sender, EventArgs e)
        {
        }

        private void Equipment_SelectedModuleChanged(object sender, EventArgs e)
        {
            if(this.buttonModule.Checked == false) return;
            if(FormManager.ActiveFormSide != this) return;

            this.buttons_CheckedChanged(this.buttonModule, e);
        }

        private void buttons_CheckedChanged(object sender, EventArgs e)
        {
            ChildForm child = null;
            RadioButton radioButton = sender as RadioButton;

            if(radioButton == null) return;
            if(radioButton.Checked == false) return;
            if(Sys.Equipment == null) return;

            if(sender == this.buttonEquipment)
            {
                child = Sys.Equipment.ElementFormProvider.GetForm(ElementFormProvider.FormEnum.Maintenance) as ChildForm;
            }
            else if(sender == this.buttonModule)
            {
                Module module = Sys.Equipment.SelectedModule;
                if(module == null) return;
                child = module.ElementFormProvider.GetForm(ElementFormProvider.FormEnum.Maintenance) as ChildForm;
                if(child == null)
                {
                    if(this.m_FormNotSupported == null || this.m_FormNotSupported.IsDisposed == true)
                        this.m_FormNotSupported = new NotSupportedForm();
                    this.m_FormNotSupported.Caption = string.Format("{0}-[Not supported]", module.Name);
                    child = this.m_FormNotSupported;
                }
            }
            else if(sender == this.buttonHost)
            {
                child = ((SecsSemiEquipmentFormProvider)Sys.Equipment.EquipmentFormProvider).GetForm(SecsSemiEquipmentFormProvider.FormEnum.HostMaintenance) as ChildForm;
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
        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(Sys.Equipment == null) return;

            Sys.Equipment.SelectedModuleChanged += new EventHandler(Equipment_SelectedModuleChanged);

            #region host
            if(Sys.Equipment.EventReportProcessor is SecsEquipmentEventReportProcessor)
            {
                this.buttonHost = this.CreateButton("Host");
                this.buttonHost.CheckedChanged += new EventHandler(this.buttons_CheckedChanged);

                this.flowLayoutPanel.Controls.Add(this.buttonHost);
            }
            #endregion

            this.buttonEquipment.Text = QMCSystem.Translate(this.buttonEquipment.Text);
            this.buttonModule.Text = QMCSystem.Translate(this.buttonModule.Text);
        }

        protected override void ManageAccess()
        {
            base.ManageAccess();

            this.buttonEquipment.Enabled = this.GetAccessibility("Equipment");
            this.buttonModule.Enabled = this.GetAccessibility("Module");
            if(this.buttonHost != null)
                this.buttonHost.Enabled = this.GetAccessibility("Host");
        }

        protected override void OnShowing(EventArgs e)
        {
            object sender = null;

            base.OnShowing(e);

            if(this.buttonEquipment.Checked)
                sender = this.buttonEquipment;
            else if(this.buttonModule.Checked)
                sender = this.buttonModule;
            else if(this.buttonHost != null && this.buttonHost.Checked)
                sender = this.buttonHost;

            if(sender != null)
                this.buttons_CheckedChanged(sender, e);
        }
        #endregion
    }
}
