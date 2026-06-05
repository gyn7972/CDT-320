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
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Motions.Controls;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks.Transfer;
using QMC.Hmi.Controls;
using static MechaSys.SoftBricks.Visions.Services.CameraTiltCalibrationServiceExecutor.Result;

namespace QMC.Transfers.Feeders
{
    public partial class SingleAxisMotionFeederArmMaintenanceControl : MotionFeederArmMaintenanceControl
    {
        #region Field
        private SingleAxisJoystick m_SingleAxisJoystick;
        private string m_SelectedAction;
        private string m_SelectedSecondary;
        private int m_SelectedSecondaryPort;
        #endregion

        #region Constructor
        public SingleAxisMotionFeederArmMaintenanceControl(IElement owner) : base(owner)
        {
            InitializeComponent();
        }
        public SingleAxisMotionFeederArmMaintenanceControl() : this(null) { }
        #endregion

        #region Event Handlers
        private void Joystick_Save(object sender, EventArgs e)
        {
            SingleAxisJoystick control = sender as SingleAxisJoystick;

            if(this.m_SelectedAction == null) return;

            if(control.Tag is TransferXyztPositionRepository positionRepository)
            {
                positionRepository.GetPositionData(this.m_SelectedAction, this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), this.m_SelectedSecondary, this.m_SelectedSecondaryPort, out IXyztPositionData positionData);

                PositionDataUtility.SetOffsetY(ref positionData, this.m_SingleAxisJoystick.Position);

                positionRepository.Replace(positionData);

                ElementConfigurator.Save(positionRepository.Configuration);
                positionRepository.ApplyConfiguration(positionRepository.Configuration);
                this.m_SingleAxisJoystick.SavedPostion = positionData.Position.Y;
            }
        }

        private void ActionTransferablePositionControl1_MovePosition(object sender, Hmi.Controls.ActionTransferablePositionMoveEventArgs e)
        {
            MethodCallerAsyncResult ar;
            SingleAxisMotionFeederArm.Positions position;
            if(Enum.TryParse<MotionFeederArm.Positions>(e.Action, out position) == false)
            {
                e.Result = -1;
                return;
            }
            if(position == MotionFeederArm.Positions.Extend)
                ar = this.Owner.MicroTransferableAgent.BeginExtend(TransferDirection.Send, e.Secondary, e.SecondaryPort);
            else
                ar = this.Owner.MicroTransferableAgent.BeginRetract(e.Secondary, e.SecondaryPort);

            WaitingBox.ShowPart(this, ar, e.Action.ToString(), this.Owner);
        }
        private void ActionTransferablePositionControl1_ChangedPosition(object sender, Hmi.Controls.ActionTransferablePositionChangedEventArgs e)
        {
            ActionTransferablePositionControl control = sender as ActionTransferablePositionControl;

            if(control.Tag is TransferXyztPositionRepository positionRepository)
            {
                positionRepository.GetPosition(e.Action, this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), e.Secondary, e.SecondaryPort, out Xyzt position);

                this.m_SingleAxisJoystick.SavedPostion = position.Y;
                this.m_SelectedAction = e.Action;
                this.m_SelectedSecondary = e.Secondary;
                this.m_SelectedSecondaryPort = e.SecondaryPort;
            }
        }

        private void PositionRepository_SelectedChange(object sender, int e)
        {
            TransferXyztPositionRepository repository = sender as TransferXyztPositionRepository;

            if(this.m_SelectedAction == null) return;

            if(this.m_SelectedSecondary == null) return;

            repository.GetPosition(this.m_SelectedAction, this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), this.m_SelectedSecondary, this.m_SelectedSecondaryPort, out Xyzt position);

            this.m_SingleAxisJoystick.SavedPostion = position.Y;
        }

        private void PositionRepository_AppliedConfiguration(object sender, EquipmentEventArgs e)
        {
            TransferXyztPositionRepository repository = sender as TransferXyztPositionRepository;

            if(this.m_SelectedAction == null) return;

            if(this.m_SelectedSecondary == null) return;

            repository.GetPosition(this.m_SelectedAction, this.Owner.EndEffector.Ports.IndexOf(this.Owner.EndEffector.Port), this.m_SelectedSecondary, this.m_SelectedSecondaryPort, out Xyzt position);

            this.m_SingleAxisJoystick.SavedPostion = position.Y;
        }
        #endregion

        #region Method
        protected SingleAxisJoystick CreateAxisJoystick(Motion motion, int index)
        {
            SingleAxisJoystick joystick = new SingleAxisJoystick();

            joystick.Caption = $"{Sys.Translate(motion.Owner.Alias, Sys.LanguageDomains.Name)}/{Sys.Translate(motion.Axes[index].Name, Sys.LanguageDomains.Name)}";
            joystick.AxisIndex = index;
            joystick.Motion = motion;
            joystick.Mode = AxisJoystickOperationMode.Step;
            joystick.EnabledButtonSave = true;
            joystick.EnabledContinueMode = true;
            joystick.Step = 0.1;
            joystick.Velocity = 10;
            joystick.Save += Joystick_Save;
            joystick.Margin = new Padding(joystick.Margin.Left, joystick.Margin.Top, joystick.Margin.Right, 0);

            return joystick;
        }
        #endregion

        #region FeederArmMaintenanceControl Members
        protected override void OnMaintenancePurposeChanged()
        {
            base.OnMaintenancePurposeChanged();

            this.actionTransferablePositionControl1.Location = new Point(this.Padding.Left, this.actionTransferablePositionControl1.Location.Y);

            this.Dock = DockStyle.Fill;
        }
        #endregion

        #region ElementMaintenanceControlX Members
        public new SingleAxisMotionFeederArm Owner
        {
            get { return base.Owner as SingleAxisMotionFeederArm; }
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
            requester = module.ServiceStateManager.Requester as Module;
            accessibility = (requester == null || requester.Name == module.Name) && module.ServiceState.CurrentStateValue == PartServiceStateMachine.StateEnum.UserSelected && module.BehaviorState.CurrentStateValue != PartBehaviorStateMachine.StateEnum.None;

            this.actionTransferablePositionControl1.Enabled = accessibility && authority;
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            base.OnDisplay();

            if(this.Owner == null) return;

            this.m_SingleAxisJoystick.Display();
        }
        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.m_SingleAxisJoystick = this.CreateAxisJoystick(this.Owner.TravelMotion, 0);
            this.m_SingleAxisJoystick.Tag = this.Owner.PositionRepository;

            this.flowLayoutPanel1.Controls.Add(this.m_SingleAxisJoystick);
            this.flowLayoutPanel1.Controls.SetChildIndex(this.m_SingleAxisJoystick, 0);

            ITransferable transferable = this.Owner.GetModule() as ITransferable;
            if(transferable != null)
            {
                foreach(TransferItem item in transferable.TransferItems)
                {
                    foreach(int primaryport in item.TransferablePorts)
                    {
                        foreach(int secondaryPort in item.TransferredPorts)
                        {
                            this.actionTransferablePositionControl1.SetTransferableKeys(transferable.Name, primaryport, item.TransferredModule, secondaryPort);
                        }
                    }
                }
            }

            this.actionTransferablePositionControl1.MovePosition += ActionTransferablePositionControl1_MovePosition;
            this.actionTransferablePositionControl1.ChangedPosition += ActionTransferablePositionControl1_ChangedPosition;
            this.actionTransferablePositionControl1.Tag = this.Owner.PositionRepository;
            this.actionTransferablePositionControl1.Title = $"{Sys.Translate(this.Owner.Alias, Sys.LanguageDomains.Name)} {Sys.Translate("position")}";
            this.actionTransferablePositionControl1.SetActionKeys<MotionFeederArm.Positions>();

            this.Owner.PositionRepository.SelectedChange += PositionRepository_SelectedChange;
            this.Owner.PositionRepository.AppliedConfiguration += PositionRepository_AppliedConfiguration;
        }
        #endregion
    }
}
