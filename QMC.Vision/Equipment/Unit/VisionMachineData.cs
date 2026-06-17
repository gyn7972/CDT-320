using QMC.Common;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  Vision 설비 수준 데이터 클래스 — 핸들러 CDT320Machine(POCO) 정렬.
    // ──────────────────────────────────────────────────────────────

    /// <summary>Vision 설비 수준의 기구적 설정값.</summary>
    public class VisionMachineSetup : ISetupData
    {
        /// <summary>설비 시리즈 명칭.</summary>
        public string MachineSeries { get; set; } = "CDT-320-VISION";
    }

    /// <summary>Vision 설비 수준의 고정 사양 파라미터.</summary>
    public class VisionMachineConfig : IConfigData
    {
        /// <summary>소프트웨어 모델 버전.</summary>
        public string ModelVersion { get; set; } = "v1.0";
    }

    /// <summary>Vision 설비 수준의 공정별 작업 파라미터(현재 활성 레시피 메타).</summary>
    public class VisionMachineRecipe : IRecipeData
    {
        /// <summary>현재 활성 레시피 번호(핸들러 수신 동기화 대상).</summary>
        public int RecipeNo { get; set; } = 0;

        /// <summary>현재 활성 레시피 명칭.</summary>
        public string RecipeName { get; set; } = "";
    }
}
