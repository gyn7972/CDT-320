using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.LoadPorts
{
    public partial class LifterLoadPortMaintenanceForm : PlateTransferLoadPortMaintenanceForm
    {
        #region Constructor
        public LifterLoadPortMaintenanceForm(LifterLoadPort target) : base(target)
        {
            InitializeComponent();
        }
        public LifterLoadPortMaintenanceForm() : this(null) { }
        #endregion

        #region ElementMaintenanceForm
        public new LifterLoadPort Target
        {
            get { return base.Target as LifterLoadPort; }
        }
        #endregion
    }
}