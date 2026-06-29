using System;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// Gap 정보 (Min, Max)
    /// </summary>
    public class Gap : Object
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg { get
            {
                return (Min + Max)/2;
            }
        }

        public override string ToString()
        {
            return "Min : " + Min.ToString("F3") + ", Max : " + Max.ToString("F3");
        }
    }

    /// <summary>
    /// 각 방향별 Gap 집합 클래스
    /// </summary>
    public class GapSet
    {
        public void SetOffset(double dOffset)
        {
            if(Left.Max >= 0.015)
            {
                Left.Max += dOffset;
                Left.Min += dOffset;
            }
            if (Right.Max >= 0.015)
            {
                Right.Max += dOffset;
                Right.Min += dOffset;
            }
            if (Top.Max >= 0.015)
            {
                Top.Max += dOffset;
                Top.Min += dOffset;
            }
            if (Bottom.Max >= 0.015)
            {
                Bottom.Max += dOffset;
                Bottom.Min += dOffset;
            }

            if (Left.Max < 0.015 || Left.Min <= 0.015)
            {
                Left.Max = 0;
                Left.Min = 0;
            }
            if (Right.Max < 0.015 || Right.Min <= 0.015)
            {
                Right.Max = 0;
                Right.Min = 0;
            }

            if (Top.Max < 0.015 || Top.Min <= 0.015)
            {
                Top.Max = 0;
                Top.Min = 0;
            }

            if (Bottom.Max < 0.015 || Bottom.Min <= 0.015)
            {
                Bottom.Max = 0;
                Bottom.Min = 0;
            }





        }
        public Gap Left { get; set; } = new Gap();
        public Gap Top { get; set; } = new Gap();
        public Gap Right { get; set; } = new Gap();
        public Gap Bottom { get; set; } = new Gap();
    }
}