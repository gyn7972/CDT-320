using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 카메라 좌표계 벡터(부호/회전).  픽셀→mm 변환 시 적용.
    /// 310 의 SentiCore <c>CameraVector</c> + <c>IsRotated</c> 개념을 단일 클래스로 합침.
    /// </summary>
    [Serializable]
    public class CameraVector
    {
        /// <summary>카메라 X축 부호 반전 (오른손/왼손 좌표계).</summary>
        public bool InvertedX { get; set; }
        /// <summary>카메라 Y축 부호 반전.</summary>
        public bool InvertedY { get; set; }
        /// <summary>카메라가 90° 회전 장착(가로/세로 스왑).</summary>
        public bool IsRotated { get; set; }

        public CameraVector() { }

        public CameraVector(bool invX, bool invY, bool rotated)
        {
            InvertedX = invX; InvertedY = invY; IsRotated = rotated;
        }

        public CameraVector Clone() => new CameraVector(InvertedX, InvertedY, IsRotated);
    }
}
