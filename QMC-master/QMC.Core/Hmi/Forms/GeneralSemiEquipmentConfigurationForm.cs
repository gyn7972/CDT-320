using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.DotNetUtility;

using QMC.Equipments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.Hmi.Forms
{
    public partial class GeneralSemiEquipmentConfigurationForm : QMC.Hmi.Forms.GeneralTabControlElementConfigurationForm
    {
        public GeneralSemiEquipmentConfigurationForm(GeneralSemiEquipment target) : base(target)
        {
            InitializeComponent();
        }
        public GeneralSemiEquipmentConfigurationForm() : this(null) { }

    }
}
