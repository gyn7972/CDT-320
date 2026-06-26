using System.Runtime.Serialization;

namespace QMC.Vision.DieMaps
{
    /// <summary>픽업 시작 코너(핸들러 QMC.CDT320.Recipes 이식).</summary>
    public enum PickupStartCorner { TopLeft = 0, BottomLeft = 1, TopRight = 2, BottomRight = 3 }

    /// <summary>픽업 진행 방향.</summary>
    public enum PickupDirection { Horizontal = 0, Vertical = 1 }

    /// <summary>픽업 진행 패턴(직선/지그재그).</summary>
    public enum PickupPattern { Straight = 0, ZigZag = 1 }

    /// <summary>웨이퍼 다이 픽업 순서 옵션(시작 코너 + 가로/세로 + 지그재그/직선).</summary>
    [DataContract]
    public class PickupSubset
    {
        [DataMember] public PickupStartCorner StartCorner { get; set; } = PickupStartCorner.TopRight;
        [DataMember] public PickupDirection Direction { get; set; } = PickupDirection.Vertical;
        [DataMember] public PickupPattern Pattern { get; set; } = PickupPattern.ZigZag;
    }
}
