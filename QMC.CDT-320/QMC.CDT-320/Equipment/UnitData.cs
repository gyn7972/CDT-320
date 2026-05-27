using QMC.Common;

namespace QMC.CDT320
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Unit 공통 데이터 클래스
    //  CDT-320의 각 Unit이 공통으로 사용하는 빈 데이터 클래스.
    //  추후 Unit별 고유 파라미터가 생기면 각자의 전용 데이터 클래스로 교체한다.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit 공통 기구적 설정값.<br/>
    /// 각 Unit 고유의 기구 파라미터(원점, 리밋 등)가 추가될 때 이 클래스를 확장하거나
    /// Unit 전용 클래스로 분리한다.
    /// </summary>
    public class UnitSetup : ISetupData { }

    /// <summary>
    /// Unit 공통 고정 사양 파라미터.<br/>
    /// 시뮬레이션 모드 여부 등 Unit 수준의 사양값이 추가될 위치.
    /// </summary>
    public class UnitConfig : IConfigData { }

    /// <summary>
    /// Unit 공통 공정별 파라미터.<br/>
    /// 공정 레시피 교체 시 변경될 Unit 수준의 값이 추가될 위치.
    /// </summary>
    public class UnitRecipe : IRecipeData { }
}
