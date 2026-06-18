using System.Runtime.Serialization;
using QMC.Common;

namespace QMC.Vision.Modules
{
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
        [DataMember] public bool   SaveGoodImage             { get; set; } = true;
        [DataMember] public bool   UseContaminationInspection { get; set; } = true;
        [DataMember] public string ImageSavePath             { get; set; } = @".\Log\Image";

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
            SaveGoodImage = d.SaveGoodImage; UseContaminationInspection = d.UseContaminationInspection;
            ImageSavePath = d.ImageSavePath;
        }
    }
}
