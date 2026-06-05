using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT_320.Equipment.Vision
{
    public class SideInspectionResult : InspectionResultInfoBase
    {
        public double ChippingDepth  { get; set; }
        public SideInspectionResult() : base()
        {
            this.ChippingDepth = 0.0;
        }
    }
}
