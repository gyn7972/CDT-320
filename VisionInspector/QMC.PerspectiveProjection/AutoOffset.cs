using QMC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace QMC.PerspectiveProjection
{
    public class AutoOffset
    {
        int ColletCount;
        public double CutoffFrequence = 0.01;
        List<LowPassFilter> LowPassFilters;
        bool bFirst = true;
        public AutoOffset(int colletCount = 4) 
        { 
            this.ColletCount = colletCount;
            LowPassFilters = new List<LowPassFilter>();
            for (int iter = 0; iter < ColletCount; iter++)
            {
             
                LowPassFilters.Add(new LowPassFilter());
            }
            ResetAutoOffset();
        }

        public void ResetAutoOffset()
        {
            for(int iter = 0;iter < LowPassFilters.Count; iter++)
            {
                LowPassFilters[iter].Reset();
            }
            bFirst = true;
        }

        public (double OffsetX, double OffsetY) GetOffset(int ColletIndex)
        {

            
            if (ColletIndex < LowPassFilters.Count)
            {
                var v = LowPassFilters[ColletIndex].GetOffset(ColletIndex);
                v.OffsetY *= 0;
                return v;
            }
            return (0, 0);
            
        }



        public (double OffsetX,double OffsetY)AddOffset(int ColletIndex,double OffsetX,double OffsetY)
        {
            //double dX = 0;
            //double dY = 0;
            //return (dX,dY);

            if(bFirst)
            {
                foreach(LowPassFilter filter in LowPassFilters)
                {
                    filter.AddData(OffsetX, OffsetY);
                }
                bFirst = false;
            }
            if(ColletIndex < LowPassFilters.Count)
            {
                LowPassFilters[ColletIndex].AddData(OffsetX, OffsetY);
                var v = GetOffset(ColletIndex);

                Log.Write("LowPassFilter", "Index" + (ColletIndex + 1).ToString(),
                    "Offset X = " + OffsetX.ToString()
                    + "  Offset Y = " + OffsetY.ToString()
                    + "  Return Offset X = " + v.OffsetX.ToString()
                    + "  Return Offset Y = " + v.OffsetY.ToString()
                    );
                

                return v;
            }

            // return (OffsetX,OffsetY);
            return (0, 0);
        }

        public void SetCutoffFrequence(double frequence)
        {
            if(frequence > 0.5)
            {
                frequence = 0.5;
            }
            if(frequence < 0.001)
            {
                frequence = 0.001;
            }
            this.CutoffFrequence = frequence;
            for(int iter = 0; iter< LowPassFilters.Count; iter++)
            {
                LowPassFilters[iter].SetCutoffFrequence(frequence);
            }
        }
    }
}
