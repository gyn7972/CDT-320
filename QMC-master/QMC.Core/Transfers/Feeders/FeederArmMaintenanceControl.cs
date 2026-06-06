using MechaSys.SoftBricks.Hmi.Controls;
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

namespace QMC.Transfers.Feeders
{
    #region FeederArmMaintenanceControl
    public partial class FeederArmMaintenanceControl : ElementMaintenanceControlX
    {
        #region Field
        private ElementMaintenancePurpose m_MaintenancePurpose;
        private Module m_Requester;
        #endregion

        #region Constructor
        public FeederArmMaintenanceControl(IElement owner) : base(owner)
        {
            InitializeComponent();
        }
        public FeederArmMaintenanceControl() : this(null)
        {
        }
        #endregion

        #region Property
        public ElementMaintenancePurpose MaintenancePurpose
        {
            get { return this.m_MaintenancePurpose; }
            set
            {
                if(this.m_MaintenancePurpose == value) return;
                this.m_MaintenancePurpose = value;
                this.OnMaintenancePurposeChanged();
            }
        }
        public Module Requester
        {
            get { return this.m_Requester; }
            set { this.m_Requester = value; }
        }
        #endregion

        #region Method
        protected virtual void OnMaintenancePurposeChanged()
        {

        }
        #endregion

        #region ElementMaintenanceControlX Members
        public new FeederArm Owner
        {
            get { return base.Owner as FeederArm; }
        }
        protected override void ManageAccess()
        {
            Module requester = null, module = null;
            bool accessibility = true;
            bool authority = true;
            User user = null;

            base.ManageAccess();

            if(UserManager.Configuration.Body.SecurityEnabled == true)
            {
                user = UserManager.GetLogOnUser();
                authority = user != null && user.IsInRole(UserRole.MaintenanceOperate);
            }
            module = this.Owner.GetModule();
            if(this.Requester != null)
            {
                requester = module.ServiceStateManager.Requester as Module;
                accessibility = requester == this.Requester && module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected && module.BehaviorState.CurrentStateValue != PartBehaviorStateMachine.StateEnum.None;
            }
            else
                accessibility = module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected && module.BehaviorState.CurrentStateValue != PartBehaviorStateMachine.StateEnum.None;
            
            this.flowLayoutPanel1.Enabled = accessibility && authority;
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            base.OnDisplay();

            if(this.Owner == null) return;

            this.materialStorablePartControl1.Display();

            this.verticalCylinderControl1.Display();
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.DesignMode == true) return;

            this.materialStorablePartControl1.MaterialStorablePart = this.Owner.EndEffector;
            this.materialStorablePartControl1.Title = this.Owner.Alias;

            if(this.Owner.OverloadSensor != null)
            {
                this.digitalBoxOverload.DioPoint = this.Owner.OverloadSensor.Inputs[0];
                this.digitalBoxOverload.EnabledControl = true;
                this.digitalBoxOverload.Text = this.Owner.OverloadSensor.Alias;
            }
            else
                this.digitalBoxOverload.Visible = false;

            if(this.Owner.EvasiveVerticalCylinder != null)
            {
                this.verticalCylinderControl1.Cylinder = this.Owner.EvasiveVerticalCylinder;
                this.verticalCylinderControl1.Title = this.Owner.EvasiveVerticalCylinder.Alias;
            }
            else
                this.verticalCylinderControl1.Visible = false;
        }
        #endregion
    }
    #endregion
}
