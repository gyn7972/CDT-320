using QMC.Common;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// 카메라 Component(Leaf) — 핸들러의 <c>BaseComponent</c>(Axis/Cylinder…) 계층 정렬.
    /// 모듈(Unit) 하위에서 카메라 설정(Setup/Config/Recipe)을 독립적으로 영속화한다.
    /// StorageKey = "&lt;모듈키&gt;.Camera" (예: WaferVision.Camera).
    /// </summary>
    public sealed class VisionCamera : BaseComponent<CameraSetup, CameraConfig, CameraRecipe>
    {
        public VisionCamera(string storageKey) : base(storageKey) { }
    }
}
