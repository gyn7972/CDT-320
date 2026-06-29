using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 치핑 정보 클래스
    /// </summary>
    public class ChippingInfo
    {
        // 치핑 컨투어 (PointF들의 집합)
        public List<PointF> Contour { get; set; } = new List<PointF>();

        // 치핑 뎁스
        public double Depth { get; set; }

        // 치핑 길이
        public double Length { get; set; }
    }
}