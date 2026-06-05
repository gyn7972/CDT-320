using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT_320.Equipment.Vision
{
    public class InspectionResultInfoBase
    {
        public string WaferId { get; set; } = "";
        public string BinId { get; set; } = "";
        public string LotId { get; set; } = "";
        public string RecipeName { get; set; } = "";
        public DateTime InspectionTime { get; set; }
        public int WaferIndexX { get; set; }
        public int WaferIndexY { get; set; }
        public int BinIndexX { get; set; }
        public int BinIndexY { get; set; }
        public int BinCode { get; set; }

        public InspectionResultInfoBase() 
        { 
        }
    }
}
