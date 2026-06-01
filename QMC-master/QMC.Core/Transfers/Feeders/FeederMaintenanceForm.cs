using MechaSys.SoftBricks.Hmi;
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
    #region FeederMaintenanceForm
    public partial class FeederMaintenanceForm : QMC.Hmi.Forms.GeneralTabControlTransferableModuleMaintenanceForm
    {
        #region Constructor
        public FeederMaintenanceForm(Feeder target) : base(target)
        {
            InitializeComponent();
        }
        public FeederMaintenanceForm() : this(null) { }
        #endregion

        #region TabControlTransferableModuleMaintenanceForm Members
        protected override void ArrangePrimaryTransferTeachingControls()
        {
            Point location = new Point();
            ElementAndControl elementAndControl = null;
            Control control = null;

            location.X = this.tabPageTeachingPrimary.Padding.Left;
            location.Y = this.tabPageTeachingPrimary.Padding.Top;

            foreach(FeederArm arm in this.Target.Arms)
            {
                control = arm.GetElementMaintenanceControl(ElementMaintenancePurpose.TransferTeaching);
                if(control == null) continue;

                elementAndControl = new ElementAndControl(arm, control, this.tabPageTeaching);
                this.tabPageTeachingPrimary.Controls.Add(control);

                control.Location = location;

                this.ElementAndControls.Add(elementAndControl);

                location.X += control.Width + this.Padding.Left;
            }
        }
        #endregion

        #region ElementMaintenanceForm Members
        public new Feeder Target
        {
            get { return base.Target as Feeder; }
        }
        #endregion
    }
    #endregion
}
