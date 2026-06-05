namespace QMC.Common
{
    /// <summary>
    /// 설비 노드의 Setup 데이터임을 나타내는 마커 인터페이스.
    /// 전원 OFF 후에도 유지되어야 하는 기구적 설정값 (예: 원점, 한계 위치).
    /// </summary>
    public interface ISetupData { }

    /// <summary>
    /// 설비 노드의 Configuration 데이터임을 나타내는 마커 인터페이스.
    /// 설비 사양에 따라 결정되는 고정 파라미터 (예: 펄스당 이동거리, 모터 방향).
    /// </summary>
    public interface IConfigData { }

    /// <summary>
    /// 설비 노드의 Recipe 데이터임을 나타내는 마커 인터페이스.
    /// 제품(공정)별로 변경되는 작업 파라미터 (예: 이송 속도, 픽업 높이).
    /// </summary>
    public interface IRecipeData { }
}
