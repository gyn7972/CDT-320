using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi;
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.Hmi.Forms.Semi;
using MechaSys.SoftBricks.IO;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Motions.Controls;
using MechaSys.SoftBricks.Security;
using MechaSys.SoftBricks.Transfer;
using QMC.Hmi.Controls;
using QMC.Parts;

namespace QMC.LoadPorts
{
    public partial class LifterPlateTransferAssistantMaintenanceControl : ElementMaintenanceControlX
    {
        #region Field
        private SingleAxisJoystick m_SingleAxisJoystick;
        private ElementMaintenancePurpose m_MaintenancePurpose;
        private Module m_Requester;
        private string m_SelectedAction;
        private int m_SelectedPlateIndex;
        private int m_SelectedCarrierIndex;
        #endregion

        #region Constructor
        public LifterPlateTransferAssistantMaintenanceControl(IElement owner) : base(owner)
        {
            InitializeComponent();

            this.m_SelectedPlateIndex = -1;
        }
        public LifterPlateTransferAssistantMaintenanceControl() : this(null) { }
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
        private RadioButtonX MakePlateRadioButton(LoadPortPlate plate)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.AutoSize = false;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = Sys.Translate(plate.Alias, Sys.LanguageDomains.Name);
            button.Tag = this.Owner.Owner.Plates.IndexOf(plate);
            button.Size = new Size(100, 40);
            button.CheckedChanged += PlateButton_CheckedChanged;
            return button;
        }
        private RadioButtonX MakeCarrierPortRadioButton(StackCarrier carrier, string name, int index)
        {
            RadioButtonX button = new RadioButtonX();

            button.Appearance = Appearance.Button;
            button.AutoSize = false;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.Text = QMCSystem.Translate(name);
            button.Tag = index;
            button.Size = new Size(100, 40);
            button.CheckedChanged += Button_CheckedChanged;
            return button;
        }

        protected SingleAxisJoystick CreateAxisJoystick(Motion motion, int index)
        {
            SingleAxisJoystick joystick = new SingleAxisJoystick();

            joystick.Caption = $"{Sys.Translate(motion.Owner.Alias, Sys.LanguageDomains.Name)}/{Sys.Translate(motion.Axes[index].Name)}";
            joystick.AxisIndex = index;
            joystick.Motion = motion;
            joystick.Mode = AxisJoystickOperationMode.Step;
            joystick.EnabledButtonSave = true;
            joystick.EnabledContinueMode = true;
            joystick.Step = 0.1;
            joystick.Velocity = 10;
            joystick.Save += M_SingleAxisJoystick_Save;
            joystick.Margin = new Padding(joystick.Margin.Left, joystick.Margin.Top, joystick.Margin.Right, 0);

            return joystick;
        }

        private void SetSlotItem(int slotCount)
        {
            this.comboBoxXSlot.Items.Clear();
            for(int i = 0; i < slotCount; i++)
            {
                this.comboBoxXSlot.Items.Add(i + 1);
            }
            if(this.comboBoxXSlot.Items.Count > 0)
                this.comboBoxXSlot.SelectedIndex = 0;
        }
        private void SetCarrierPortItem(Carrier carrier)
        {
            RadioButtonX carrierButton = null;

            this.flowLayoutPanelXCarrierPort.Controls.Clear();

            if(carrier is StackCarrier stackCarrier)
            {
                carrierButton = this.MakeCarrierPortRadioButton(stackCarrier.Below, "Below", 0);
                this.flowLayoutPanelXCarrierPort.Controls.Add(carrierButton);
                this.slotStateView1.DisplayState = MechaSys.SoftBricks.Materials.Controls.SlotStateView.StateType.SlotState;
                this.slotStateView1.Carrier = stackCarrier;
                this.slotStateView1.SlotCount = stackCarrier.Capacity;

                if(stackCarrier.Above != null)
                {
                    carrierButton = this.MakeCarrierPortRadioButton(stackCarrier.Above, "Above", 1);
                    this.flowLayoutPanelXCarrierPort.Controls.Add(carrierButton);
                    if(this.MaintenancePurpose != ElementMaintenancePurpose.TransferTeaching)
                    {
                        this.slotStateView2.DisplayState = MechaSys.SoftBricks.Materials.Controls.SlotStateView.StateType.SlotState;
                        this.slotStateView2.Carrier = stackCarrier.Above;
                        this.slotStateView2.SlotCount = stackCarrier.Above.Capacity;
                        this.slotStateView2.Visible = true;
                    }
                }
                else
                    this.slotStateView2.Visible = false;

                if(this.flowLayoutPanelXCarrierPort.Controls.Count > 0)
                    ((RadioButtonX)this.flowLayoutPanelXCarrierPort.Controls[0]).Checked = true;
            }
        }
        protected virtual void OnMaintenancePurposeChanged()
        {
            int width = 0, height = 0;

            this.slotStateView1.Visible = false;
            this.slotStateView2.Visible = false;
            this.groupBoxXMoveToSlot.Visible = false;

            this.flowLayoutPanelXPlates.Location = new Point(this.Padding.Left, this.flowLayoutPanelXPlates.Location.Y);
            this.flowLayoutPanelXCarrierPort.Location = new Point(this.Padding.Left, this.flowLayoutPanelXCarrierPort.Location.Y);
            this.actionPositionControl1.Location = new Point(this.Padding.Left, this.flowLayoutPanelXCarrierPort.Location.Y + this.flowLayoutPanelXCarrierPort.Height);

            this.Dock = DockStyle.Fill;
            //width = Math.Max(this.flowLayoutPanelXPlates.Width, this.flowLayoutPanelXCarrierPort.Width);
            //width = Math.Max(width, this.actionPositionControl1.Width);
            //
            //width += this.flowLayoutPanel1.Width;
            //
            //height = this.actionPositionControl1.Location.Y + this.actionPositionControl1.Height + this.Padding.Vertical;

            // this.Size = new Size(width + this.Padding.Horizontal, height);
        }
        #endregion

        #region Event Handlers
        private void M_SingleAxisJoystick_Save(object sender, System.EventArgs e)
        {
            SingleAxisJoystick control = sender as SingleAxisJoystick;

            if(this.m_SelectedAction == null) return;

            if(this.m_SelectedPlateIndex == -1) return;

            if(control.Tag is TargetXyztPositionRepository positionRepository)
            {
                positionRepository.GetPositionData(this.m_SelectedAction, this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out IXyztPositionData positionData);

                PositionDataUtility.SetOffsetZ(ref positionData, control.Position);

                positionRepository.Replace(positionData);

                ElementConfigurator.Save(positionRepository.Configuration);
                positionRepository.ApplyConfiguration(positionRepository.Configuration);
                control.SavedPostion = positionData.Position.Z;
            }
        }

        private void PositionRepository_SelectedChange(object sender, int e)
        {
            TargetXyztPositionRepository repository = sender as TargetXyztPositionRepository;

            if(this.m_SelectedAction == null) return;

            if(this.m_SelectedPlateIndex == -1) return;

            repository.GetPosition(this.m_SelectedAction, this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out Xyzt position);

            this.m_SingleAxisJoystick.SavedPostion = position.Z;
        }

        private void PositionRepository_AppliedConfiguration(object sender, EquipmentEventArgs e)
        {
            TargetXyztPositionRepository repository = sender as TargetXyztPositionRepository;

            if(this.m_SelectedAction == null) return;

            if(this.m_SelectedPlateIndex == -1) return;

            repository.GetPosition(this.m_SelectedAction, this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out Xyzt position);

            this.m_SingleAxisJoystick.SavedPostion = position.Z;
        }

        private void OperationButton_Click(object sender, EventArgs e)
        {
            if(sender == null) return;

            if(this.m_SelectedPlateIndex == -1) return;

            if(sender == this.buttonXMoveToSlot)
            {
                int selectSlot = Convert.ToInt32(this.comboBoxXSlot.SelectedItem.ToString());
                int slotIndex = selectSlot - 1;
                WaitingBox.ShowElement(this, this.Owner.BeginMoveToTransfer(TransferDirection.SendAndReceive, this.m_SelectedPlateIndex, this.m_SelectedCarrierIndex, slotIndex), "Move...", this.Owner);
            }
        }
        private void CarrierPresenceState_AfterTransit(object sender, LoadPortCarrierPresenceAfterStateTransitionEventArgs e)
        {
            Carrier carrier = null;
            LoadPortPlate plate = null;

            plate = this.Owner.Owner.Plates[this.m_SelectedPlateIndex];
            carrier = plate.Port.Location.GetMaterial() as Carrier;
            if(carrier != null && carrier.Presence == MaterialPresence.Exist)
                this.groupBoxXMoveToSlot.Enabled = true;
            else
                this.groupBoxXMoveToSlot.Enabled = false;

            this.SetCarrierPortItem(carrier);
            this.SetSlotItem(carrier.Capacity);
        }
        private void PlateButton_CheckedChanged(object sender, EventArgs e)
        {
            Xyzt position = new Xyzt();
            Carrier carrier = null;
            RadioButtonX carrierButton = null;
            LoadPortPlate plate = null;
            int index = 0;
            if(sender == null) return;

            RadioButtonX button = sender as RadioButtonX;
            if(button.Checked == false) return;

            index = Convert.ToInt32(button.Tag);
            this.m_SelectedPlateIndex = index;
            plate = this.Owner.Owner.Plates[index];
            if(plate is LockableLoadPortPlate lockableLoadPortPlate)
            {
                this.carrierLockerControl1.Visible = true;
                this.carrierLockerControl1.CarrierLocker = lockableLoadPortPlate.Locker as CarrierLocker;
            }
            else
                this.carrierLockerControl1.Visible = false;

            carrier = plate.Port.Location.GetMaterial() as Carrier;

            this.SetCarrierPortItem(carrier);

            if(carrier != null && carrier.Presence == MaterialPresence.Exist)
                this.groupBoxXMoveToSlot.Enabled = true;
            else
                this.groupBoxXMoveToSlot.Enabled = false;

            if(plate.MaterialDetector == null)
                this.groupBoxXMaterialDetector.Visible = false;
            else
            {
                this.groupBoxXMaterialDetector.Text = Sys.Translate(((DiPart)plate.MaterialDetector).Alias, Sys.LanguageDomains.Name);
                this.digitalGroupBoxMaterialDetector.Points = ((DiPart)plate.MaterialDetector).Inputs.ToArray();
            }

            if(this.m_SelectedAction != null && this.m_SelectedPlateIndex != -1)
            {
                this.Owner.PositionRepository.GetPosition(this.m_SelectedAction.ToString(), this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out position);
                this.m_SingleAxisJoystick.SavedPostion = position.Z;
            }

            if(plate.ProtrusionSensor != null)
            {
                this.digitalBoxProtrusionSensor.DioPoint = plate.ProtrusionSensor.Inputs[0];
                this.digitalBoxProtrusionSensor.Text = plate.ProtrusionSensor.Alias;
            }
            else
                this.digitalBoxProtrusionSensor.Visible = false;
        }
        private void Button_CheckedChanged(object sender, EventArgs e)
        {
            Carrier carrier = null;
            LoadPortPlate plate = null;
            if(sender == null) return;

            RadioButtonX button = sender as RadioButtonX;
            if(button.Checked == false) return;

            this.m_SelectedCarrierIndex = (int)button.Tag;
        }

        private void ActionPositionControl1_ChangedAction(object sender, Hmi.Controls.ActionChangedEventArgs e)
        {
            ActionPositionControl control = sender as ActionPositionControl;

            if(this.m_SelectedPlateIndex == -1) return;

            if(control.Tag is TargetXyztPositionRepository positionRepository)
            {
                positionRepository.GetPosition(e.ActionKey, this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out Xyzt position);

                this.m_SingleAxisJoystick.SavedPostion = position.Z;
                this.m_SelectedAction = e.ActionKey;
            }
        }

        private void ActionPositionControl1_MovePosition(object sender, Hmi.Controls.ActionMoveEventArgs e)
        {
            ActionPositionControl control = sender as ActionPositionControl;
            MethodCallerAsyncResult ar;

            if(this.m_SelectedPlateIndex == -1) return;

            if(control.Tag is TargetXyztPositionRepository positionRepository)
            {
                positionRepository.GetPosition(e.ActionKey, this.Owner.Owner.Plates[this.m_SelectedPlateIndex], out Xyzt position);

                if(Enum.TryParse < LifterPlateTransferAssistant.Positions>(e.ActionKey, out LifterPlateTransferAssistant.Positions positionKey) == false) return;

                if(positionKey == LifterPlateTransferAssistant.Positions.Ready)
                    ar = this.Owner.BeginMoveToReady(this.m_SelectedPlateIndex, this.m_SelectedCarrierIndex);
                else if(positionKey == LifterPlateTransferAssistant.Positions.Transfer)
                {
                    int selectSlot = Convert.ToInt32(this.comboBoxXSlot.SelectedItem.ToString());
                    ar = this.Owner.BeginMoveToTransfer(TransferDirection.SendAndReceive, this.m_SelectedPlateIndex, this.m_SelectedCarrierIndex, selectSlot - 1);
                }
                else if(positionKey == LifterPlateTransferAssistant.Positions.Scan)
                    ar = this.Owner.BeginMoveToScan(this.m_SelectedPlateIndex, this.m_SelectedCarrierIndex);
                else
                    throw new NotSupportedException(this.m_SelectedAction.ToString());

                WaitingBox.ShowElement(this, ar, e.ActionKey, this.Owner);
            }
        }
        #endregion

        #region ElementMaintenanceControlX Members
        public new LifterPlateTransferAssistant Owner
        {
            get { return base.Owner as LifterPlateTransferAssistant; }
        }
        protected override void ManageAccess()
        {
            Module requester = null, module = null;
            bool accessibility = true;
            bool authority = true;
            User user = null;
            Carrier carrier = null;
            LoadPortPlate plate = null;

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

            plate = this.Owner.Owner.Plates[this.m_SelectedPlateIndex];
            carrier = plate.Port.Location.GetMaterial() as Carrier;

            this.groupBoxXMoveToSlot.Enabled = accessibility && authority && carrier != null && carrier.Presence == MaterialPresence.Exist;
            this.actionPositionControl1.Enabled = accessibility && authority;
            this.m_SingleAxisJoystick.Enabled = accessibility && authority;
            this.flowLayoutPanelXPlates.Enabled = accessibility && authority;
            this.flowLayoutPanelXCarrierPort.Enabled = accessibility && authority;
        }
        #endregion

        #region UserControlX Members
        protected override void OnDisplay()
        {
            base.OnDisplay();

            this.m_SingleAxisJoystick.Display();
            this.carrierLockerControl1.Display();
        }
        protected override void OnPrepare()
        {
            RadioButtonX button = null;
            SingleAxisJoystick joystick = null;

            base.OnPrepare();

            joystick = this.CreateAxisJoystick(this.Owner.Owner.Z, 0);
            joystick.Tag = this.Owner.PositionRepository;

            this.m_SingleAxisJoystick = joystick;

            this.flowLayoutPanel1.Controls.Add(joystick);
            this.flowLayoutPanel1.Controls.SetChildIndex(joystick, 0);

            for(int i = 0; i < this.Owner.Owner.Plates.Count; i++)
            {
                button = this.MakePlateRadioButton(this.Owner.Owner.Plates[i]);
                this.flowLayoutPanelXPlates.Controls.Add(button);
            }
            if(this.flowLayoutPanelXPlates.Controls.Count > 0)
                ((RadioButtonX)this.flowLayoutPanelXPlates.Controls[0]).Checked = true;

            this.actionPositionControl1.MovePosition += ActionPositionControl1_MovePosition;
            this.actionPositionControl1.ChangedAction += ActionPositionControl1_ChangedAction;
            this.actionPositionControl1.Tag = this.Owner.PositionRepository;
            this.actionPositionControl1.Title = $"{Sys.Translate(this.Owner.Owner.Alias, Sys.LanguageDomains.Name)} {Sys.Translate("position")}";
            this.actionPositionControl1.SetActionKeys<LifterPlateTransferAssistant.Positions>();

            for(int i = 0; i < this.Owner.Owner.Plates.Count; i++)
            {
                this.Owner.Owner.Plates[i].CarrierPresenceState.AfterTransit += CarrierPresenceState_AfterTransit;
            }

            this.Owner.PositionRepository.SelectedChange += PositionRepository_SelectedChange;
            this.Owner.PositionRepository.AppliedConfiguration += PositionRepository_AppliedConfiguration;

            this.groupBoxXMoveToSlot.Text = QMCSystem.Translate(this.groupBoxXMoveToSlot.Text);
            this.buttonXMoveToSlot.Text = QMCSystem.Translate(this.buttonXMoveToSlot.Text);
        }
        #endregion
    }
}
