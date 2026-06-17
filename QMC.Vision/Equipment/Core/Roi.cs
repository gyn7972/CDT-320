using System.Drawing;
using System.Runtime.Serialization;

namespace QMC.Vision.Core
{
    /// <summary>ROI(Region of Interest) — 픽셀 단위 사각/원형 영역.</summary>
    [DataContract]
    public class Roi
    {
        [DataMember] public string Name     { get; set; }
        [DataMember] public RoiShape Shape  { get; set; } = RoiShape.Rectangle;
        [DataMember] public double CenterX  { get; set; }
        [DataMember] public double CenterY  { get; set; }
        [DataMember] public double Width    { get; set; } = 100;
        [DataMember] public double Height   { get; set; } = 100;
        [DataMember] public double AngleDeg { get; set; }
        [DataMember] public double Radius   { get; set; } = 50; // 원형/환형에 사용
        [DataMember] public double InnerRadius { get; set; }     // 환형 내부 반경

        public Rectangle BoundingBox =>
            new Rectangle(
                (int)(CenterX - Width  / 2),
                (int)(CenterY - Height / 2),
                (int)Width,
                (int)Height);

        public Roi Clone() => (Roi)MemberwiseClone();
    }

    public enum RoiShape { Rectangle, Circle, Annulus, Polygon }
}
