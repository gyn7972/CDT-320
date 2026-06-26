using QMC.Common;
using QMC.Common.Recipes;
using System.Collections.Generic;
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

        // ── 픽셀↔mm 스케일 / 좌표변환 (모듈별, 310 VisionScale 정렬) ──
        /// <summary>X 스케일 mm/pixel.</summary>
        [DataMember] public double ScaleX             { get; set; }
        /// <summary>Y 스케일 mm/pixel.</summary>
        [DataMember] public double ScaleY             { get; set; }
        /// <summary>카메라 90° 회전 장착 — 결과 X↔Y 스왑.</summary>
        [DataMember] public bool   IsRotated          { get; set; }
        /// <summary>X 부호 반전.</summary>
        [DataMember] public bool   InvertedX          { get; set; }
        /// <summary>Y 부호 반전.</summary>
        [DataMember] public bool   InvertedY          { get; set; }
        /// <summary>MATCH 응답 좌표를 mm 로 변환해 반환.</summary>
        [DataMember] public bool   ReturnMmCoordinates{ get; set; }

        // ── 캘리브레이션 입력(칩 실제 치수) — '스케일 계산' 버튼이 이 값으로 산출 ──
        /// <summary>칩 실제 가로(mm). 스케일 자동계산 입력.</summary>
        [DataMember] public double CalibChipWidthMm  { get; set; }
        /// <summary>칩 실제 세로(mm). 스케일 자동계산 입력.</summary>
        [DataMember] public double CalibChipHeightMm { get; set; }

        // ── 모듈(카메라) 레벨 시뮬 이미지 — 핸들러 GRAB 시 카메라 대신 이 이미지를 그랩(테스트용) ──
        /// <summary>true 면 모듈 GRAB(<c>VisionModule.Grab</c>) 시 카메라 대신 <see cref="SimSavedImagePath"/> 저장 이미지를 사용.
        /// 도구별(Finder) SimUseSavedImage 와 독립 — GRAB 한 장으로 모든 finder 가 같은 프레임을 공유한다.</summary>
        [DataMember] public bool   SimUseSavedImage  { get; set; }
        /// <summary>모듈 GRAB 시 사용할 저장 이미지 경로(<see cref="SimUseSavedImage"/>=true 일 때).</summary>
        [DataMember] public string SimSavedImagePath { get; set; }

        // ── 제네릭 카메라 노드 파라미터(노드 카탈로그 정의 항목의 값). MVS Feature Tree 확장 파라미터. ──
        /// <summary>GenICam 노드명↔저장값 목록. 노드 타입/옵션은 카메라 노드 카탈로그가 정의한다.</summary>
        [DataMember] public List<CameraNodeParam> NodeParams { get; set; }

        /// <summary>MVS Feature Save 파일(.mfs) 경로 — 카메라 전체 노드값 일괄 적용/저장.</summary>
        [DataMember] public string MvsFeatureFilePath { get; set; }

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
            ScaleX = 1.0; ScaleY = 1.0;
            IsRotated = false; InvertedX = false; InvertedY = false;
            ReturnMmCoordinates = false;
            CalibChipWidthMm = 0; CalibChipHeightMm = 0;
            SimUseSavedImage = false; SimSavedImagePath = string.Empty;
            NodeParams = new List<CameraNodeParam>();
            MvsFeatureFilePath = string.Empty;
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
