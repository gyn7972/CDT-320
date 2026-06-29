using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.PerspectiveProjection
{
    public class LowPassFilter
    {
        double dX, dY;  
        double dFirstX, dFirstY;
        double CutoffFrequence = 0.01;
        int AddedCount = 0;
        public void SetCutoffFrequence(double cutoffFrequence)
        {
            this.CutoffFrequence = cutoffFrequence;
        }
        public void Reset()
        {
            dX = 0; 
            dY = 0;
            dFirstX = 0;
            dFirstY = 0;
            AddedCount = 0;
        }
        public (double OffsetX, double OffsetY) AddData(double dxOffset, double dyOffset)
        {
            if(AddedCount == 0)
            {
                dFirstX = dxOffset;
                dFirstY = dyOffset;
            }
            else
            {
                dxOffset = dxOffset + dX;
                dyOffset = dyOffset + dY;
            }
                double dCurrentX = dxOffset;
            double dCurrentY = dyOffset ;
            
            dX = dX  *  (1- CutoffFrequence) + dCurrentX * CutoffFrequence;
            dY = dY * (1- CutoffFrequence) + dCurrentY * CutoffFrequence;

            AddedCount++;
            return (dX, dY);
        }

        public (double OffsetX, double OffsetY) GetOffset(int colletIndex)
        {
            return (dX, dY);
        }
    }
}
