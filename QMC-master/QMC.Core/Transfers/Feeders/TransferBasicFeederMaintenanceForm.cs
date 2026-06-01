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
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks.Transfer;

namespace QMC.Transfers.Feeders
{
    public partial class TransferBasicFeederMaintenanceForm : FeederMaintenanceForm
    {
        #region Constructor
        public TransferBasicFeederMaintenanceForm(TransferBasicFeeder target) : base(target)
        {
            InitializeComponent();
        }
        public TransferBasicFeederMaintenanceForm() : this(null) { }
        #endregion

        #region Property
        protected ITransferred RelatedSecondary
        {
            get { return this.relatedTransferredModulePortLocationSelector1.SelectedModule; }
        }
        protected int RelatedSecondaryPortIndex
        {
            get { return this.relatedTransferredModulePortLocationSelector1.SelectedPortIndex; }
        }
        #endregion

        #region Method
        protected virtual void OnRelatedSecondaryChanged(ITransferred module)
        {
            this.tabPageXTeachingRelatedSecondary.Text = module != null ? module.Alias : "";
        }
        protected virtual void OnRelatedSecondaryPortIndexChanged(int portIndex)
        {
            this.ArrangeRelatedSecondaryTransferTeachingControls();

            this.OnDisplay();
        }
        protected virtual void ArrangeRelatedSecondaryTransferTeachingControls()
        {
            Port port = null;
            ElementAndControl elementAndControl = null;
            Control control = null;
            ISupportElementDetailedMaintenanceControl supportElementDetailedMaintenanceControl = null;

            foreach(Control item in this.tabPageXTeachingRelatedSecondary.Controls)
            {
                this.ElementAndControls.Remove(item);
            }
            this.tabPageXTeachingRelatedSecondary.Controls.Clear();

            port = this.RelatedSecondary.Ports[this.RelatedSecondaryPortIndex];
            supportElementDetailedMaintenanceControl = ElementList.GetOwner<ISupportElementDetailedMaintenanceControl>(port.MaterialStorablePart);
            if(supportElementDetailedMaintenanceControl == null) return;

            control = supportElementDetailedMaintenanceControl.GetElementMaintenanceControl(ElementMaintenancePurpose.TransferTeaching, this.Target);
            if(control == null) return;

            elementAndControl = new ElementAndControl(port.MaterialStorablePart, control, this.tabPageXTeachingRelatedSecondary);
            this.ElementAndControls.Add(elementAndControl);

            this.tabPageXTeachingRelatedSecondary.Controls.Add(control);
            control.Location = new Point(this.tabPageXTeachingRelatedSecondary.Padding.Left, this.tabPageXTeachingRelatedSecondary.Padding.Top);
        }
        protected override int CheckSecondary()
        {
            int ret = 0;
            Module partner = this.RelatedSecondary as Module;

            if((ret = base.CheckSecondary()) != 0) return ret;

            if(partner == null)
                return ErrorManager.Register(string.Format("Partner module doesnot selected."));

            if(partner.BehaviorState.CurrentStateValue != PartBehaviorStateMachine.StateEnum.Idle && partner.BehaviorState.CurrentStateValue != PartBehaviorStateMachine.StateEnum.IdleWithAlarms)
                return ErrorManager.Register("Partner behavior state is not Idle.");

            if(partner.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected)
                return ErrorManager.Register("Partner service state is not UserSelected.");

            if(Object.Equals(partner.ServiceStateManager.Requester, this.Target) == false)
                return ErrorManager.Register(string.Format("Partner service state requester is not {0}.", this.Target.Name));

            return ret;
        }
        protected virtual void GetRelatedSecondaryPorts(out int[] relatedSecondaryPorts)
        {
            relatedSecondaryPorts = new int[1];
            relatedSecondaryPorts[0] = this.RelatedSecondaryPortIndex;
        }
        protected override int GetTransferSpecification(TransferDirection direction, out TransferSpecification specification)
        {
            int ret = 0;
            int[] primaryPorts;
            int[] secondaryPorts;
            int[] relatedSecondaryPorts;
            TransferSpecification previousSpecification = null;
            ITransferred relatedSecondaryModule = null;

            this.GetRelatedSecondaryPorts(out relatedSecondaryPorts);

            specification = new RelatedTransferSpecification();
            if((ret = base.GetTransferSpecification(direction, out previousSpecification)) != 0) return ret;

            relatedSecondaryModule = this.RelatedSecondary;
            if(relatedSecondaryModule == null) return -1;

            specification.Primary = previousSpecification.Primary;
            specification.PrimaryPorts = previousSpecification.PrimaryPorts;
            specification.Secondary = previousSpecification.Secondary;
            if(this.Secondary is LoadPort)
            {
                specification.SecondaryPorts = previousSpecification.SecondaryPorts;
                specification.EnabledSecondaryCarrierPort = previousSpecification.EnabledSecondaryCarrierPort;
                specification.CarrierIndex = previousSpecification.CarrierIndex;
                specification.SecondaryCarrierPorts = previousSpecification.SecondaryCarrierPorts;
            }
            else
                specification.SecondaryPorts = previousSpecification.SecondaryPorts;

            ((RelatedTransferSpecification)specification).RelatedTransferred = relatedSecondaryModule.Name;
            if(relatedSecondaryModule is LoadPort)
            {
                foreach(int index in relatedSecondaryPorts)
                    ((RelatedTransferSpecification)specification).RelatedTransferredPorts.Add(index);

                ((RelatedTransferSpecification)specification).EnabledSecondaryCarrierPort = true;
                ((RelatedTransferSpecification)specification).CarrierIndex = this.relatedTransferredModulePortLocationSelector1.SelectedCarrierIndex;

                ((RelatedTransferSpecification)specification).SecondaryCarrierPorts.Clear();
                ((RelatedTransferSpecification)specification).SecondaryCarrierPorts.Add(this.relatedTransferredModulePortLocationSelector1.SelectedSlotIndex);
            }
            else
            {
                foreach(int index in relatedSecondaryPorts)
                    ((RelatedTransferSpecification)specification).RelatedTransferredPorts.Add(index);
            }

            specification.Role = previousSpecification.Role;
            specification.Type = previousSpecification.Type;
            specification.Direction = previousSpecification.Direction;

            return ret;
        }

        protected override int OnReceive()
        {
            int ret = 0;
            ITransferred secondaryModule = null;
            ITransferred relatedTransferredModule = null;
            TransferSpecification primarySpecification, secondarySpecification;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            secondaryModule = this.Secondary;
            relatedTransferredModule = this.RelatedSecondary;
            if(secondaryModule == null || relatedTransferredModule == null) return ErrorManager.Register("Sencondary is not selected.");

            if((ret = this.GetTransferSpecification(TransferDirection.Receive, out primarySpecification)) != 0) return ret;

            secondarySpecification = primarySpecification.GetOppositeSpecification();

            this.Primary.TransferState.Reactivate();
            if(this.Primary != secondaryModule)
                secondaryModule.TransferState.Reactivate();

            ars.Add(this.Primary.BeginReadyToTransfer(primarySpecification, null, null));
            if(this.Primary != secondaryModule)
                ars.Add(secondaryModule.BeginReadyToTransfer(secondarySpecification, null, null));
            ars.Add(relatedTransferredModule.BeginReadyToTransfer(primarySpecification, null, null));

            if((ret = ars.WaitReturn()) != 0) return ret;

            ret = this.Primary.ExecuteTransferSync(primarySpecification.Id);

            secondaryModule.CancelReadyToTransferSync(secondarySpecification.Id);
            relatedTransferredModule.CancelReadyToTransferSync(primarySpecification.Id);

            return ret;
        }

        protected override int OnSend()
        {
            int ret = 0;
            ITransferred secondaryModule = null;
            ITransferred relatedTransferredModule = null;
            TransferSpecification primarySpecification, secondarySpecification;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            secondaryModule = this.Secondary;
            relatedTransferredModule = this.RelatedSecondary;

            if(secondaryModule == null || relatedTransferredModule == null) return ErrorManager.Register("Sencondary is not selected.");

            if((ret = this.GetTransferSpecification(TransferDirection.Send, out primarySpecification)) != 0) return ret;

            secondarySpecification = primarySpecification.GetOppositeSpecification();

            this.Primary.TransferState.Reactivate();
            if(this.Primary != secondaryModule)
                secondaryModule.TransferState.Reactivate();

            ars.Add(this.Primary.BeginReadyToTransfer(primarySpecification, null, null));

            if(this.Primary != secondaryModule)
                ars.Add(secondaryModule.BeginReadyToTransfer(secondarySpecification, null, null));
            ars.Add(relatedTransferredModule.BeginReadyToTransfer(primarySpecification, null, null));

            if((ret = ars.WaitReturn()) != 0) return ret;

            ret = this.Primary.ExecuteTransferSync(primarySpecification.Id);

            secondaryModule.CancelReadyToTransferSync(secondarySpecification.Id);
            relatedTransferredModule.CancelReadyToTransferSync(primarySpecification.Id);

            return ret;
        }

        protected override int OnReadySecondary()
        {
            int ret = 0;
            ITransferred secondaryModule = null;
            ITransferred relatedTransferredModule = null;

            TransferSpecification primarySpecification, secondarySpecification;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();
            TransferDirection direction = TransferDirection.Receive;

            secondaryModule = this.Secondary;
            relatedTransferredModule = this.RelatedSecondary;

            if(secondaryModule == null || relatedTransferredModule == null) return ErrorManager.Register("Sencondary is not selected.");

            this.GetPrimaryTransferDirection(out direction);
            if((ret = this.GetTransferSpecification(direction, out primarySpecification)) != 0) return ret;

            secondarySpecification = primarySpecification.GetOppositeSpecification();

            secondaryModule.TransferState.Reactivate();
            relatedTransferredModule.TransferState.Reactivate();

            ars.Add(secondaryModule.BeginReadyToTransfer(secondarySpecification, null, null));
            ars.Add(relatedTransferredModule.BeginReadyToTransfer(primarySpecification, null, null));

            ret = ars.WaitReturn();

            secondaryModule.CancelReadyToTransferSync(secondarySpecification.Id);
            relatedTransferredModule.CancelReadyToTransferSync(primarySpecification.Id);

            return ret;
        }
        #endregion

        #region Event Handlers
        private void relatedTransferredModulePortLocationSelector1_BeforeSelectedModuleChanged(object sender, EventArgs e)
        {
            if(this.AccessManagementTrigger.Elements.Contains(this.relatedTransferredModulePortLocationSelector1.SelectedModule as Element) == true)
                this.AccessManagementTrigger.Elements.Remove(this.relatedTransferredModulePortLocationSelector1.SelectedModule as Element);
        }

        private void relatedTransferredModulePortLocationSelector1_SelectedModuleChanged(object sender, EventArgs e)
        {
            if(this.AccessManagementTrigger.Elements.Contains(this.relatedTransferredModulePortLocationSelector1.SelectedModule as Element) == false)
                this.AccessManagementTrigger.Elements.Add(this.relatedTransferredModulePortLocationSelector1.SelectedModule as Element);

            this.OnRelatedSecondaryChanged(this.RelatedSecondary);
        }

        private void relatedTransferredModulePortLocationSelector1_SelectedPortIndexChanged(object sender, EventArgs e)
        {
            this.OnRelatedSecondaryPortIndexChanged(this.RelatedSecondaryPortIndex);
        }
        protected override void buttonLock_Click(object sender, EventArgs e)
        {
            Module module = this.Secondary as Module;
            Module relatedModule = this.RelatedSecondary as Module;
            MethodCallerAsyncResultCollection ars = new MethodCallerAsyncResultCollection();

            if(module == null) return;

            if((Object.Equals(module.ServiceStateManager.Requester, this.Target) == false || module.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected)
                && (Object.Equals(relatedModule.ServiceStateManager.Requester, this.Target) == false || relatedModule.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected))
            {
                ars.Add(module.ServiceStateManager.BeginChangeService(this.Target, PartServiceStateMachine.StateEnum.UserSelected));
                ars.Add(relatedModule.ServiceStateManager.BeginChangeService(this.Target, PartServiceStateMachine.StateEnum.UserSelected));
            }
            else if((Object.Equals(module.ServiceStateManager.Requester, this.Target) && module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected)
                || (Object.Equals(relatedModule.ServiceStateManager.Requester, this.Target) && relatedModule.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected))
            {
                ars.Add(module.ServiceStateManager.BeginChangeService(EquipmentAccount.User, PartServiceStateMachine.StateEnum.UserSelected));
                ars.Add(relatedModule.ServiceStateManager.BeginChangeService(EquipmentAccount.User, PartServiceStateMachine.StateEnum.UserSelected));
            }
            else
            {
                ars.Add(module.ServiceStateManager.BeginChangeService(this.Target, PartServiceStateMachine.StateEnum.InService));
                ars.Add(relatedModule.ServiceStateManager.BeginChangeService(this.Target, PartServiceStateMachine.StateEnum.InService));
            }

            if(ars != null)
                WaitingBox.ShowPart(this, ars, "Change service state", module);
        }
        #endregion

        #region ElementMaintenanceForm Members
        public new TransferBasicFeeder Target
        {
            get { return base.Target as TransferBasicFeeder; }
        }
        #endregion

        #region DisplayForm Members
        protected override void ManageAccess()
        {
            Module requester = null;
            bool accessibility = true;
            bool authority = true;
            User user = UserManager.GetLogOnUser();

            base.ManageAccess();

            if(UserManager.Configuration.Body.SecurityEnabled == true)
            {
                authority = user != null && user.IsInRole(UserRole.MaintenanceOperate);
            }

            requester = this.Target.ServiceStateManager.Requester as Module;
            accessibility = (requester == null || requester == this.Target) && this.Target.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected;

            this.relatedTransferredModulePortLocationSelector1.Enabled = accessibility && authority;
        }
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.relatedTransferredModulePortLocationSelector1.Owner = this.Primary;
            this.relatedTransferredModulePortLocationSelector1.Display();
        }
        protected override void OnDisplay()
        {
            Module module = null;
            Module relatedModule = null;

            base.OnDisplay();

            this.relatedTransferredModulePortLocationSelector1.Display();

            module = this.Secondary as Module;
            relatedModule = this.RelatedSecondary as Module;

            if(module == null || relatedModule == null) return;

            if(Object.Equals(module.ServiceStateManager.Requester, this.Target) == false || module.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected ||
                Object.Equals(relatedModule.ServiceStateManager.Requester, this.Target) == false || relatedModule.ServiceState.CurrentStateValue != PartServiceStateMachine.StateEnum.UserSelected)
                this.buttonLock.Text = Sys.Translate("Lock");
            else
                this.buttonLock.Text = Sys.Translate("Unlock");

            foreach(Control control in this.tabPageTeachingPrimary.Controls)
            {
                if(control is UserControlX primary)
                    primary.Display();
            }

            foreach(Control control in this.tabPageTeachingSecondary.Controls)
            {
                if(control is UserControlX secondary)
                    secondary.Display();
            }

            foreach(Control control in this.tabPageXTeachingRelatedSecondary.Controls)
            {
                if(control is UserControlX related)
                    related.Display();
            }
        }
        #endregion       
    }
}
