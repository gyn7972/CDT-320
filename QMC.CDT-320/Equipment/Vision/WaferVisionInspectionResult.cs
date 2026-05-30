using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT_320.Equipment.Vision
{
    public class WaferVisionInspectionResult : InspectionResultInfoBase
    {
        public double DieOffsetX { get; set; }
        public double DieOffsetY { get; set; }
        public double DieRotation  { get; set; }
        public WaferVisionInspectionResult() : base()
        { 
                this.DieOffsetX = 0.0;
                this.DieOffsetY = 0.0;
                this.DieRotation = 0.0;
        }

    }
}
