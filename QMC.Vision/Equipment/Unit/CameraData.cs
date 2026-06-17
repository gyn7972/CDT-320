using QMC.Common;
using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  Camera Component 데이터 — 모듈(Unit) 하위 카메라 노드 전용 Setup/Config/Recipe.
    //  (구 VisionModuleConfigBase 카메라 필드 + VisionModuleRecipeBase.Exposure 를 이전)
    //  핸들러의 Component(Camera/Axis…) 계층 정렬.
    // ──────────────────────────────────────────────────────────────

    /// <summary>카메라 Setup — 기구/고정(현재 비어 있음, 확장 지점).</summary>
    [DataContract]
    public sealed class CameraSetup : ISetupData { }

    /// <summary>카메라 Config — 카메라/그랩 고정 사양(SSOT).</summary>
    [DataContract]
    public sealed class CameraConfig : IConfigData
    {
        [DataMember] public string CameraId          { get; set; }
        [DataMember] public double Gain              { get; set; }
        [DataMember] public double FrameRate         { get; set; }
        [DataMember] public string TriggerMode       { get; set; }
        [DataMember] public string PixelFormat       { get; set; }
        [DataMember] public int    DelayBeforeGrabMs { get; set; }
        [DataMember] public int    RoiOffsetX        { get; set; }
        [DataMember] public int    RoiOffsetY        { get; set; }
        [DataMember] public int    RoiWidth          { get; set; }
        [DataMember] public int    RoiHeight         { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults()
        {
            CameraId = string.Empty;
            Gain = 1.0;
            FrameRate = 30.0;
            TriggerMode = string.Empty;
            PixelFormat = string.Empty;
            DelayBeforeGrabMs = 0;
            RoiOffsetX = 0; RoiOffsetY = 0; RoiWidth = 0; RoiHeight = 0;
        }

        public CameraConfig() { SetDefaults(); }
    }

    /// <summary>카메라 Recipe — 제품/공정별 노출.</summary>
    [DataContract]
    public sealed class CameraRecipe : IRecipeData
    {
        [DataMember] public double Exposure { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { Exposure = 5000; }

        public CameraRecipe() { SetDefaults(); }
    }
}
