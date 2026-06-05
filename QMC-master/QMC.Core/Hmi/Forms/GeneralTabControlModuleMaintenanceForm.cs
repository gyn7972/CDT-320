using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MechaSys.SoftBricks;

namespace QMC.Hmi.Forms
{
    public partial class GeneralTabControlModuleMaintenanceForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlModuleMaintenanceForm
    {
        #region Constructor
        public GeneralTabControlModuleMaintenanceForm(Module target) : base(target)
        {
            InitializeComponent();
        }
        public GeneralTabControlModuleMaintenanceForm() : this(null) { }
        #endregion
    }
}
