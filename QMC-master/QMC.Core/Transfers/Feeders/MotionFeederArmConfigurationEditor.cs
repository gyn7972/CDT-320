using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Hmi.Controls;
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
    public partial class MotionFeederArmConfigurationEditor : ElementConfigurationEditorX
    {
        #region Constructor
        public MotionFeederArmConfigurationEditor(IElementConfigurable element) : base(element)
        {
            InitializeComponent();
        }
        public MotionFeederArmConfigurationEditor() : this(null) { }
        #endregion

        #region ElementConfigurationEditorX Members
        public new MotionFeederArm Owner
        {
            get { return base.Owner as MotionFeederArm; }
        }
        #endregion
    }
}
