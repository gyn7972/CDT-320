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
    #region FeederConfigurationForm
    public partial class FeederConfigurationForm : QMC.Hmi.Forms.GeneralTabControlElementConfigurationForm
    {
        #region Constructor
        public FeederConfigurationForm(Feeder target) : base(target)
        {
            InitializeComponent();
        }
        public FeederConfigurationForm() : this(null) { }
        #endregion
    }
    #endregion
}
