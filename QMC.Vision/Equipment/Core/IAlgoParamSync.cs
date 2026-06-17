using QMC.Common;

namespace QMC.Vision.Core
{
    /// <summary>
    /// ①(per-algorithm 파라미터 인프라) — 검사 전용 파라미터가 런타임 백엔드(finder/inspector)에 실제 반영돼야 할 때
    /// 백엔드가 선택 구현하는 훅. 공용 ApplyToRuntime/CollectFromRuntime 은 부모 POCO 타입만 보므로(파생 전용필드 못 봄),
    /// 백엔드가 자기 구체 Recipe/Config/Setup 타입으로 캐스트해 전용필드를 읽고/쓴다.
    /// <para>미구현 백엔드 = no-op (back-compat). 노드가 공통 sync 후 호출.</para>
    /// </summary>
    public interface IAlgoParamSync
    {
        /// <summary>POCO(노드 Recipe/Config/Setup) → 백엔드 (전용필드 적용). Load 직후 공통 sync 뒤 호출.</summary>
        void ApplyParams(IRecipeData recipe, IConfigData config, ISetupData setup);

        /// <summary>백엔드 → POCO (전용필드 수집). Save 직전 공통 collect 뒤 호출.</summary>
        void CollectParams(IRecipeData recipe, IConfigData config, ISetupData setup);
    }
}
