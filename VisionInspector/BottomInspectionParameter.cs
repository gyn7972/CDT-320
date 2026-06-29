using System;
using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 검사 파라미터 및 칩 디텍팅 기능 제공
    /// </summary>
    public class BottomInspectionParameter : InspectionParameterBase
    {
        public BottomInspectionParameter()
        {
            TopHatRadius = 21;
        }

        // 치핑 뎁스
        private double _chippingDepth;
        public double ChippingDepth
        {
            get => _chippingDepth;
            set => _chippingDepth = value;
        }
      
        public double ForeignObjectSize { get; set; } = 0.5; // 이물질 크기 (기본값 0.0)

        public double ChippingLength { get; set; } = 0.0; // 칩 길이 (기본값 0.0)
        public SizeF ChipLowerSpecLimit { get; set; } = new SizeF(0, 0); // 칩 하한 스펙 리밋 (기본값 0,0)
        public SizeF ChipUpperSpecLimit { get; set; } = new SizeF(0, 0); // 칩 상한 스펙 리밋 (기본값 0,0)

        public double FirstPeekValueThreshold { get; set; } = 230;
        public double PeekValueThreshold { get; set; } = 40;
        public double Stdev { get; set; } = 0.01;


        public int TopHatRadius { get; set; }

        public int TopHatThreshold { get; set; } = 30;

        public int MinForeignAreaFilterSize { get; set; } = 36;
        public int LinkDistance { get; set; } = 20;

        public double PortentiolDefactMinSize { get; set; } = 20;

        public string FileSavePath { get; set; } = "Z:\\Log\\Image";
        public bool UseContaminationInspection { get; set; } = true;
    }

    public class SideInspectionParameter : InspectionParameterBase
    {

        // 치핑 뎁스
        private double _chippingDepth;
        public double ChippingDepth
        {
            get => _chippingDepth;
            set => _chippingDepth = value;
        }

        public double ForeignObjectSize { get; set; } = 0.5; // 이물질 크기 (기본값 0.0)

        public double ChippingLength { get; set; } = 0.0; // 칩 길이 (기본값 0.0)
        public SizeF ChipLowerSpecLimit { get; set; } = new SizeF(0, 0); // 칩 하한 스펙 리밋 (기본값 0,0)
        public SizeF ChipUpperSpecLimit { get; set; } = new SizeF(0, 0); // 칩 상한 스펙 리밋 (기본값 0,0)

        public double ChipThickness { get; set; } = 0.25;
        public double BladeWidth { get; set; } = 0.048;
        public double FirstBladeDepth { get; set; } = 0.050;


    }
}
