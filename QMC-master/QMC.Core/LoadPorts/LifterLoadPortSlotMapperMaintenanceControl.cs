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
using MechaSys.SoftBricks.Hmi.Controls;
using MechaSys.SoftBricks.LoadPorts;
using MechaSys.SoftBricks.Materials;

namespace QMC.LoadPorts
{
    public partial class LifterLoadPortSlotMapperMaintenanceControl : MechaSys.SoftBricks.LoadPorts.SlotMappers.AggregatedMotionSlotMapperMaintenanceControl
    {
        #region Field
        #endregion

        #region Constructor
        public LifterLoadPortSlotMapperMaintenanceControl(LifterLoadPortSlotMapper owner) : base(owner)
        {
            InitializeComponent();
        }
        public LifterLoadPortSlotMapperMaintenanceControl() : this(null) { }
        #endregion

        #region Property
        #endregion

        #region Event Handler
        private void PlateRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonX radioButton = sender as RadioButtonX;
            LoadPortPlate plate = null;

            if (radioButton.Checked == false) return;
            plate = radioButton.Tag as LoadPortPlate;

            this.Plate = plate;
            this.PrepareCarrier();
        }

        private void CarrierRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButtonX radioButton = sender as RadioButtonX;
            Carrier carrier = null;

            if (radioButton.Checked == false) return;
            carrier = radioButton.Tag as Carrier;
            this.Carrier = carrier;
            this.OnPositionChanged();
        }

        private void CarrierPresenceState_AfterTransit(object sender, LoadPortCarrierPresenceAfterStateTransitionEventArgs e)
        {
            LoadPortCarrierPresenceStateMachine presenceStateMachine = sender as LoadPortCarrierPresenceStateMachine;

            if (presenceStateMachine.Owner != this.Plate) return;

            this.PrepareCarrier();

            if (e.CurrentStateValue == LoadPortCarrierPresenceStateMachine.StateEnum.Exist)
            {
                if (this.radioButtonCarrierAbove.Checked) this.Carrier = this.radioButtonCarrierAbove.Tag as Carrier;
                if (this.radioButtonCarrierBelow.Checked) this.Carrier = this.radioButtonCarrierBelow.Tag as Carrier;
            }
        }
        #endregion

        #region Method
        private void PreparePlate()
        {
            if (this.Owner == null) return;

            this.radioButtonPlate0.Visible = 0 < this.Owner.Owner.Plates.Count;
            this.radioButtonPlate0.Text = Sys.Translate(this.Owner.Owner.Plate.Alias, Sys.LanguageDomains.Name);
            this.radioButtonPlate0.Tag = this.Owner.Owner.Plate;
            this.Owner.Owner.Plate.CarrierPresenceState.AfterTransit += this.CarrierPresenceState_AfterTransit;
            this.radioButtonPlate0.CheckedChanged += this.PlateRadioButton_CheckedChanged;

            if (1 < this.Owner.Owner.Plates.Count)
            {
                this.radioButtonPlate1.Visible = true;
                this.radioButtonPlate1.Text = Sys.Translate(this.Owner.Owner.Plates[1].Alias, Sys.LanguageDomains.Name);
                this.radioButtonPlate1.Tag = this.Owner.Owner.Plates[1];
                this.Owner.Owner.Plates[1].CarrierPresenceState.AfterTransit += this.CarrierPresenceState_AfterTransit;
                this.radioButtonPlate1.CheckedChanged += this.PlateRadioButton_CheckedChanged;
            }
            else
                this.radioButtonPlate1.Visible = false;

            this.radioButtonPlate0.Checked = true;
        }

        private void PrepareCarrier()
        {
            StackCarrier stack = null;
            Carrier carrier = null;

            if (this.Plate == null)
            {
                this.flowLayoutPanelXCarrierPort.Visible = false;
                this.Carrier = null;
                return;
            }

            carrier = this.Plate.Port.Location.GetMaterial() as Carrier;
            if (carrier == null || carrier.Presence != MaterialPresence.Exist)
            {
                this.flowLayoutPanelXCarrierPort.Visible = false;
                this.Carrier = null;
                return;
            }

            this.radioButtonCarrierBelow.Visible = true;
            this.radioButtonCarrierBelow.Tag = carrier;
            stack = carrier as StackCarrier;

            if (stack != null && stack.Above != null)
            {
                this.radioButtonCarrierAbove.Visible = true;
                this.radioButtonCarrierAbove.Tag = stack.Above;
            }
            else
                this.radioButtonCarrierAbove.Visible = false;

            if (this.radioButtonCarrierBelow.Checked == false && this.radioButtonCarrierAbove.Checked == false)
                this.radioButtonCarrierBelow.Checked = true;

            if (this.radioButtonCarrierAbove.Checked) this.Carrier = this.radioButtonCarrierAbove.Tag as Carrier;
            if (this.radioButtonCarrierBelow.Checked) this.Carrier = this.radioButtonCarrierBelow.Tag as Carrier;

            this.OnPositionChanged();
            this.flowLayoutPanelXCarrierPort.Visible = true;
        }
        #endregion

        #region ElementMaintenanceForm
        public new LifterLoadPortSlotMapper Owner
        {
            get { return base.Owner as LifterLoadPortSlotMapper; }
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();

            this.radioButtonPlate0.Checked = this.radioButtonPlate1.Checked = false;
            this.radioButtonCarrierBelow.Checked = this.radioButtonCarrierAbove.Checked = false;

            if (this.Owner == null) return;

            this.PreparePlate();

            this.radioButtonCarrierAbove.CheckedChanged += this.CarrierRadioButton_CheckedChanged;
            this.radioButtonCarrierBelow.CheckedChanged += this.CarrierRadioButton_CheckedChanged;

            this.radioButtonCarrierBelow.Text = QMCSystem.Translate(this.radioButtonCarrierBelow.Text);
            this.radioButtonCarrierAbove.Text = QMCSystem.Translate(this.radioButtonCarrierAbove.Text);
        }

        protected override void OnDisplay()
        {
            base.OnDisplay();
        }
        #endregion
    }
}