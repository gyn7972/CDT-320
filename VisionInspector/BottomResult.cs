using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 검사 결과 및 칩핑 정보 클래스
    /// </summary>
    public class BottomResult : Object
    {
        // 오프셋
        public PointF Offset { get; set; }

        // 각도
        public double Angle { get; set; }

        // 너비
        public double Width { get; set; }

        // 높이
        public double Height { get; set; }

        // 코너 4포인트
        public PointF[] Corners { get; set; } = new PointF[4];

        public List<ChippingInfo> ChippingInfos { get; set; } = new List<ChippingInfo>();
        // 불량 코드
        public int DefectCode { get; set; }
        public double Channel1ChippingSize { get; set; } = 0;
        public double Channel2ChippingSize { get; set; } = 0;


        public double ChppingBottomSize { get; set; } = 0;
        public double ChppingLeftSize { get; set; } = 0;
        public double ChppingRightSize { get; set; } = 0;
        public double ChppingTopSize { get; set; } = 0;

        public double MaxDefactSize { get; set; } = 0;
        public double ForeingSize { get; set; } = 0;
        public double ForeingArea { get; set; } = 0;

        public Image DisplayImage = null;
        public int SaveCount = 0;
    }
    public class SideResult
    {
      
        public List<ChippingInfo> ChippingInfos { get; set; } = new List<ChippingInfo>();
        public double MaxChippingDepth
        {
            get
            {
                double dMax = 0;
                if(ChippingInfos!= null)
                {
                    if (ChippingInfos.Count > 0)
                    {
                        dMax = ChippingInfos.Max(t=>t.Depth);
                    }
                }
                return dMax;
            }
        }
        // 불량 코드
        public int DefectCode { get; set; }
    }

}
