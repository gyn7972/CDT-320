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
using MechaSys.SoftBricks.Motions;
using MechaSys.SoftBricks.Motions.Controls;

namespace QMC.Transfers.Feeders
{
    public partial class MotionFeederArmMaintenanceControl : FeederArmMaintenanceControl
    {
        #region Constructor
        public MotionFeederArmMaintenanceControl(IElement owner) : base(owner)
        {
            InitializeComponent();
        }
        public MotionFeederArmMaintenanceControl() : this(null)
        {
        }
        #endregion

        #region ElementMaintenanceControlX Members
        public new MotionFeederArm Owner
        {
            get { return base.Owner as MotionFeederArm; }
        }
        #endregion
    }
}
