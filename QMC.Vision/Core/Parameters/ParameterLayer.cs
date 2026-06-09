namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// Recipe 3-Layer (§3-3). 파라미터 1개의 수명·저장소를 결정하는 계층 태그.
    /// <para>Setup = 전원 OFF 후에도 유지되는 기구/캘리브 설정(위치·오프셋·mm/px).
    /// Config = 고정 사양(포트·타임아웃·HW 고정값). Recipe = 제품·공정별 변경(검사 임계·패턴 ROI).</para>
    /// P1: enum 신설만 — 기존 ParameterGridScope(Ui.Controls)는 P4 통합 시 정리. 매핑 Recipe/Setup/Config 1:1.
    /// </summary>
    public enum ParameterLayer
    {
        Setup,
        Config,
        Recipe
    }
}
