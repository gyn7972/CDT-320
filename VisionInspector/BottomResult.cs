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

        // 이물 위치/크기 목록 — 좌표는 BottomInspect 반환 시 코너와 동일 규약(×0.5 + ChipRoi 좌상단)으로
        // 원본 입력 이미지 기준으로 환산되어 담긴다(2026-07-11, 종전에는 크기만 반환하고 위치는 버렸음).
        public List<ForeignInfo> ForeignInfos { get; set; } = new List<ForeignInfo>();

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
    /// <summary>
    /// 이물 1건의 위치/크기. Rect 좌표계는 수집 시 검사(2배 확장) 이미지 기준이며,
    /// BottomInspect 가 반환 직전 코너와 동일 규약(×0.5 + ChipRoi 좌상단)으로 원본 입력 좌표로 환산한다.
    /// </summary>
    public class ForeignInfo
    {
        public RectangleF Rect { get; set; }
        public double SizeMm { get; set; }
        public int Area { get; set; }
        public bool IsNg { get; set; }
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
