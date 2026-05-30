using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT_320.Equipment.Vision
{
    public class BinInspectionResult : InspectionResultInfoBase
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double OffsetR { get; set; }

        public double DieGapTop { get; set; }
        public double DieGapBottom { get; set; }
        public double DieGapLeft { get; set; }
        public double DieGapRight { get; set; }
        public double AlignmentX { get; set; }
        public double AlignmentY { get; set; }
        
        public BinInspectionResult() : base() 
        {
            OffsetX = 0;
            OffsetY = 0;
            OffsetR = 0;
            DieGapTop = 0;
            DieGapBottom = 0;
            DieGapLeft = 0;
            DieGapRight = 0;
            AlignmentX = 0;
            AlignmentY = 0;

        }
    
            
    }
}
