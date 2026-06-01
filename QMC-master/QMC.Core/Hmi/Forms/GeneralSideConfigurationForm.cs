using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Secs.Hmi;
using MechaSys.SoftBricks.Security;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSideConfigurationForm : QMC.Hmi.Forms.GeneralSideForm
    {
        #region Define
        #endregion

        #region Field
        private GraphicRadioButton buttonHost;

        private NotSupportedForm m_FormNotSupported;
        #endregion

        #region Constructor
        public GeneralSideConfigurationForm()
        {
            InitializeComponent();
        }
        #endregion

        #region Method
        private bool GetAccessibility(string function)
        {
            bool accessibility = true;
            User user = UserManager.GetLogOnUser();

            if(UserManager.Configuration.Body.SecurityEnabled == false)
            {
                return true;
            }

            if(function == "Equipment")
            {
                accessibility = user != null && user.IsInRole(UserRole.ConfigurationView);
            }
            else if(function == "Module")
            {
                accessibility = user != null && user.IsInRole(UserRole.ConfigurationView);
            }
            else if(function == "Host")
            {
                accessibility = user != null && user.IsInRole(UserRole.ConfigurationView);
            }
            else
            {
                accessibility = user != null && user.IsInRole(UserRole.ConfigurationView);
            }

            return accessibility;
        }
        #endregion

        #region Event Handler
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
                child = Sys.Equipment.ElementFormProvider.GetForm(ElementFormProvider.FormEnum.Configuration) as ChildForm;
            }
            else if(sender == this.buttonModule)
            {
                Module module = Sys.Equipment.SelectedModule;
                if(module == null) return;
                child = module.ElementFormProvider.GetForm(ElementFormProvider.FormEnum.Configuration) as ChildForm;
                if(child == null)
                {
                    if(this.m_FormNotSupported == null || this.m_FormNotSupported.IsDisposed == true)
                        this.m_FormNotSupported = new NotSupportedForm();
                    this.m_FormNotSupported.Caption = string.Format("{0}-[Not supported]", module.Name);
                    child = this.m_FormNotSupported;
                }
            }
            else if(sender == this.buttonCarrierConfiguration)
            {
                child = ((SemiEquipmentFormProvider)Sys.Equipment.EquipmentFormProvider).GetForm(SemiEquipmentFormProvider.FormEnum.CarrierConfiguration) as ChildForm;
            }
            else if(sender == this.buttonCarrierOperation)
            {
                child = ((SemiEquipmentFormProvider)Sys.Equipment.EquipmentFormProvider).GetForm(SemiEquipmentFormProvider.FormEnum.CarrierOperation) as ChildForm;
            }
            else if(sender == this.buttonHost)
            {
                child = ((SecsSemiEquipmentFormProvider)Sys.Equipment.EquipmentFormProvider).GetForm(SecsSemiEquipmentFormProvider.FormEnum.HostConfiguration) as ChildForm;
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
            SecsSemiEquipmentFormProvider formProvider = null;

            base.OnPrepare();

            if(Sys.Equipment == null) return;

            Sys.Equipment.SelectedModuleChanged += new EventHandler(Equipment_SelectedModuleChanged);

            #region host
            formProvider = Sys.Equipment.EquipmentFormProvider as SecsSemiEquipmentFormProvider;
            if(formProvider != null && formProvider.IsSupportedForm(SecsSemiEquipmentFormProvider.FormEnum.HostConfiguration) == true)
            {
                this.buttonHost = this.CreateButton("Host");
                this.buttonHost.CheckedChanged += new EventHandler(this.buttons_CheckedChanged);

                this.flowLayoutPanel.Controls.Add(this.buttonHost);
            }
            #endregion

            #region carrier
            LoadPort[] loadports = null;

            loadports = ElementList.GetByType<LoadPort>(true);
            this.buttonCarrierConfiguration.Visible = loadports.Length != 0;
            this.buttonCarrierOperation.Visible = loadports.Length != 0;
            #endregion

            this.buttonCarrierConfiguration.Text = QMCSystem.Translate(this.buttonCarrierConfiguration.Text);
            this.buttonCarrierOperation.Text = QMCSystem.Translate(this.buttonCarrierOperation.Text);
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
            else if(this.buttonCarrierConfiguration.Checked)
                sender = this.buttonCarrierConfiguration;
            else if(this.buttonCarrierOperation.Checked)
                sender = this.buttonCarrierOperation;
            else if(this.buttonHost != null && this.buttonHost.Checked)
                sender = this.buttonHost;

            if(sender != null)
                this.buttons_CheckedChanged(sender, e);
        }
        #endregion
    }
}
