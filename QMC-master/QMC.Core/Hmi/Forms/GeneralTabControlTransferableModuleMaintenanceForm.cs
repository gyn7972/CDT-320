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

namespace QMC.Hmi.Forms
{
    public partial class GeneralTabControlTransferableModuleMaintenanceForm : MechaSys.SoftBricks.Hmi.Forms.Semi.TabControlTransferableModuleMaintenanceForm
    {
        #region Field

        #endregion

        #region Constructor
        public GeneralTabControlTransferableModuleMaintenanceForm(Module target) : base(target)
        {
            InitializeComponent();
        }
        public GeneralTabControlTransferableModuleMaintenanceForm() : this(null) { }
        #endregion

        #region Method
        #endregion

        #region ChildForm Members
        protected override void OnPrepare()
        {
            base.OnPrepare();

            if(this.Target == null) return;
        }
        #endregion
    }
}
