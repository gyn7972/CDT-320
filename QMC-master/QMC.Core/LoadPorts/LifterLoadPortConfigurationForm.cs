using QMC.Hmi.Forms;
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
    public partial class LifterLoadPortConfigurationForm : GeneralTabControlElementConfigurationForm
    {
        #region Constructor
        public LifterLoadPortConfigurationForm(LifterLoadPort target) : base(target)
        {
            InitializeComponent();
        }
        public LifterLoadPortConfigurationForm() : this(null) { }
        #endregion

    }
}
