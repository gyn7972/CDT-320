using System.Runtime.Serialization;
using QMC.Common;

namespace QMC.Vision.Modules
{
    /// <summary>검사 이미지 저장 대상 — 레시피(품목)별 선택.</summary>
    public enum ImageSaveMode
    {
        /// <summary>양품(PASS) 이미지만 저장.</summary>
        OK = 0,
        /// <summary>불량(NG) 이미지만 저장.</summary>
        NG = 1,
        /// <summary>OK/NG 모두 저장.</summary>
        ALL = 2,
    }

    // ──────────────────────────────────────────────────────────────
    //  Vision 설비 수준 데이터 클래스 — 핸들러/기존 Vision DTO 와 동일하게
    //  [DataContract]/[DataMember] 사용(DataContractJsonSerializer 영속화).
    // ──────────────────────────────────────────────────────────────

    /// <summary>Vision 설비 수준의 기구적 설정값.</summary>
    [DataContract]
    public class VisionMachineSetup : ISetupData
    {
        /// <summary>설비 시리즈 명칭.</summary>
        [DataMember] public string MachineSeries { get; set; } = "CDT-320-VISION";
    }

    /// <summary>Vision 설비 수준의 고정 사양 파라미터.</summary>
    [DataContract]
    public class VisionMachineConfig : IConfigData
    {
        /// <summary>소프트웨어 모델 버전.</summary>
        [DataMember] public string ModelVersion { get; set; } = "v1.0";
    }

    /// <summary>Vision 설비 수준의 공정별 작업 파라미터(현재 활성 레시피 메타 + 품목 공통).</summary>
    [DataContract]
    public class VisionMachineRecipe : IRecipeData
    {
        /// <summary>현재 활성 레시피 번호(핸들러 수신 동기화 대상).</summary>
        [DataMember] public int RecipeNo { get; set; } = 0;

        /// <summary>현재 활성 레시피 명칭.</summary>
        [DataMember] public string RecipeName { get; set; } = "";

        // ── 품목 공통(카메라 모듈 공통) 설정 — CDT-310 Inspection/Side/Bottom 파라미터 기반 ──
        // 식별
        [DataMember] public string PartId  { get; set; } = "";   // 품목명
        [DataMember] public string LotId   { get; set; } = "";   // Lot
        [DataMember] public string WaferId { get; set; } = "";   // Wafer ID

        // 다이 기준 치수/스펙 (mm) — Bottom 치수 합부판정 공통
        [DataMember] public double ChipWidthMm        { get; set; } = 0;
        [DataMember] public double ChipHeightMm       { get; set; } = 0;
        [DataMember] public double ChipWidthLowerMm   { get; set; } = 0;
        [DataMember] public double ChipWidthUpperMm   { get; set; } = 0;
        [DataMember] public double ChipHeightLowerMm  { get; set; } = 0;
        [DataMember] public double ChipHeightUpperMm  { get; set; } = 0;

        // 물리 치수 (mm) — Side/Bottom 검사 공통 입력
        [DataMember] public double ChipThicknessMm    { get; set; } = 0.25;
        [DataMember] public double TapeThicknessMm    { get; set; } = 0.10;
        [DataMember] public double BladeWidthMm       { get; set; } = 0.048;
        [DataMember] public double FirstBladeDepthMm  { get; set; } = 0.050;

        // 공통 허용치 (mm) — 검사 기본 한계
        [DataMember] public double MaxChippingDepthMm  { get; set; } = 0;
        [DataMember] public double MaxChippingLengthMm { get; set; } = 0;
        [DataMember] public double MaxForeignSizeMm    { get; set; } = 0.5;

        // 공통 옵션
        /// <summary>이 레시피(품목)로 운전 중 로그(이미지+데이터) 기록 여부. 전역 ImageLogEnable/DataLogEnable 이 켜져 있어도
        /// 이 값이 false 면 해당 레시피는 로그를 남기지 않는다(레시피별 토글).</summary>
        [DataMember] public bool          LogEnable                 { get; set; } = true;
        /// <summary>검사 이미지 저장 대상 — OK/NG/ALL. (구 SaveGoodImage 대체)</summary>
        [DataMember] public ImageSaveMode ImageSaveMode             { get; set; } = ImageSaveMode.ALL;
        // 오염검사 사용(구 UseContaminationInspection)은 검사기별 InspectorAlgoRecipe.UseInspection 으로 이전 — 품목 공통 필드 제거(도구 단위 일원화).
        // 이미지 저장 경로는 장비 단위 전역 설정(VisionSettings.ImageLogPath, 설정→일반)으로 일원화 — 레시피 필드 제거(중복 제거).

        // 구버전 키 마이그레이션 — 구 json 의 "SaveGoodImage"(bool) 가 있으면 ImageSaveMode 로 이전 후 비운다(다음 Save 시 사라짐).
        [DataMember(Name = "SaveGoodImage", EmitDefaultValue = false)]
        public bool? LegacySaveGoodImage { get; set; }

        // DataContractJsonSerializer 는 프로퍼티 이니셜라이저를 실행하지 않으므로, 구 json 에 없는 신규 키는
        // 기본값 대신 default(T)(LogEnable=false, ImageSaveMode=OK) 로 남는다 → 역직렬화 전에 안전 기본값을 심어
        // 기존 동작(로그 ON, 전부 저장)을 보존한다.
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext ctx)
        {
            LogEnable     = true;
            ImageSaveMode = ImageSaveMode.ALL;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            if (LegacySaveGoodImage.HasValue)
            {
                // 구 의미: 양품 저장 ON → ALL, OFF → NG (양품 저장 안 함 = 불량만).
                ImageSaveMode       = LegacySaveGoodImage.Value ? ImageSaveMode.ALL : ImageSaveMode.NG;
                LegacySaveGoodImage = null;
            }
        }

        /// <summary>공통 설정을 기본값으로 제자리 초기화(레시피 번호/명칭은 호출측에서 유지).
        /// Machine.Recipe 는 private set 이므로 새 인스턴스 대신 이 메서드로 리셋한다.</summary>
        public void ResetToDefaults()
        {
            var d = new VisionMachineRecipe();
            PartId = d.PartId; LotId = d.LotId; WaferId = d.WaferId;
            ChipWidthMm = d.ChipWidthMm; ChipHeightMm = d.ChipHeightMm;
            ChipWidthLowerMm = d.ChipWidthLowerMm; ChipWidthUpperMm = d.ChipWidthUpperMm;
            ChipHeightLowerMm = d.ChipHeightLowerMm; ChipHeightUpperMm = d.ChipHeightUpperMm;
            ChipThicknessMm = d.ChipThicknessMm; TapeThicknessMm = d.TapeThicknessMm;
            BladeWidthMm = d.BladeWidthMm; FirstBladeDepthMm = d.FirstBladeDepthMm;
            MaxChippingDepthMm = d.MaxChippingDepthMm; MaxChippingLengthMm = d.MaxChippingLengthMm;
            MaxForeignSizeMm = d.MaxForeignSizeMm;
            LogEnable = d.LogEnable; ImageSaveMode = d.ImageSaveMode;
        }
    }
}
