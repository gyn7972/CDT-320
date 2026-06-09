namespace QMC.Vision.Core.Parameters
{
    /// <summary>파라미터 값 종류. UI 편집기·직렬화 변환 선택에 사용.</summary>
    public enum ParameterType
    {
        Double,
        Int,
        Bool,
        Text,
        Enum
    }

    /// <summary>
    /// 파라미터 도메인(분류). 그리드 표시 필터용 — 조명은 전용 InspectionLightPanel 이 담당하므로
    /// 파라미터 그리드에선 제외(중복 방지). 저장·영속화·dirty 는 도메인 무관 전부 처리.
    /// </summary>
    public enum ParameterDomain
    {
        General,
        Lighting
    }
}
