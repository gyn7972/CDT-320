using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT_320.Equipment.Vision
{
    public class BottomInspectionResult :  InspectionResultInfoBase
    {
      

        public double Width { get; set; }
        public double Height { get; set; }
        public double ChippingLeft { get; set; }
        public double ChippingRight { get; set; }
        public double ChippingTop { get; set; }
        public double ChippingBottom { get; set; }
        public double ForignMaxSize { get; set; }
        public string NgCode { get; set; } = "";

        public string GetBinCode()
        {
            // 예시: NG 코드에 따라 BinCode 산출 (실제 로직은 NG 코드 규칙에 따라 구현)
            if (NgCode.Contains("WIDTH")) return "Bin1";
            if (NgCode.Contains("HEIGHT")) return "Bin2";
            if (NgCode.Contains("CHIPPING")) return "Bin3";
            if (NgCode.Contains("FORIGN")) return "Bin4";
            return "Good";
        }
        public override string ToString()
        {
            return $"BottomInspectionResult [Width={Width}, Height={Height}, ChippingLeft={ChippingLeft}, " +
                   $"ChippingRight={ChippingRight}, ChippingTop={ChippingTop}, ChippingBottom={ChippingBottom}, " +
                   $"ForignMaxSize={ForignMaxSize}, NgCode='{NgCode}', BinCode='{GetBinCode()}']";
        }
        public BottomInspectionResult Clone()
        {
            return new BottomInspectionResult
            {
                Width = this.Width,
                Height = this.Height,
                ChippingLeft = this.ChippingLeft,
                ChippingRight = this.ChippingRight,
                ChippingTop = this.ChippingTop,
                ChippingBottom = this.ChippingBottom,
                ForignMaxSize = this.ForignMaxSize,
                NgCode = this.NgCode
            };
        }
        public void UpdateFrom(BottomInspectionResult other)
        {
            if (other == null) return;
            this.Width = other.Width;
            this.Height = other.Height;
            this.ChippingLeft = other.ChippingLeft;
            this.ChippingRight = other.ChippingRight;
            this.ChippingTop = other.ChippingTop;
            this.ChippingBottom = other.ChippingBottom;
            this.ForignMaxSize = other.ForignMaxSize;
            this.NgCode = other.NgCode;
        }
        //기본 생성자
        public BottomInspectionResult() 
            : base()
        { 
            this.Width = 0;
            this.Height = 0;
            this.ChippingLeft = 0;
            this.ChippingRight = 0;
            this.ChippingTop = 0;
            this.ChippingBottom = 0;
            this.ForignMaxSize = 0;
            this.NgCode = "";


        }


    }
}
