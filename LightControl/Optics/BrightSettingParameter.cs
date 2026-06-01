using Cognex.VisionPro.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common.Vision.Optics
{
    [Serializable]
    public class BrightSettingParameter
    {
        public int Channel { set; get; }
        public int OnTime { set; get; }
        public int Power { set; get; }
        public double ExposeTime { set; get; }
        public BrightSettingParameter() 
        {
            Channel = 0;
            OnTime = 10;
            Power = 10;
            ExposeTime = 1000;
        }
    }

    [Serializable]
    public class BottomBrightSettingParameter : BrightSettingParameter
    {
        
        public int Channel2 { set; get; }
        public int OnTime2 { set; get; }

        public int Page { set; get; }

        public BottomBrightSettingParameter() : base()
        {
            Channel = 1;

            Channel2 = 2;
            OnTime2 = 1000;

            Page = 1;
        }

    }

    [Serializable]
    public class TopBrightSettingParameter : BrightSettingParameter
    {
        public int Channel2 { set; get; }
        public int OnTime2 { set; get; }

        public int Channel3 { set; get; }
        public int OnTime3 { set; get; }

        public int Channel4 { set; get; }
        public int OnTime4 { set; get; }

        public int Page { set; get; }

        public TopBrightSettingParameter() : base()
        {
            Channel = 3;

            Channel2 = 4;
            OnTime2 = 1000;

            Channel3 = 5;
            OnTime3 = 1000;

            Channel4 = 6;
            OnTime4 = 1000;

            Page = 1;
        }

    }
}
