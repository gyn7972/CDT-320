namespace QMC.Common
{
    /// <summary>
    /// Machine / Unit / Component 모든 계층의 공통 추상 베이스 클래스.
    /// Composite Pattern의 "Component" 역할을 담당한다.
    /// </summary>
    public abstract class BaseEquipmentNode
    {
        /// <summary>노드의 고유 이름 (예: "CDT-320", "Transfer_Unit", "Z_Axis_Motor")</summary>
        public string Name { get; protected set; }

        /// <summary>기구적 설정값 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public ISetupData Setup { get; protected set; }

        /// <summary>고정 사양 파라미터 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public IConfigData Config { get; protected set; }

        /// <summary>공정별 작업 파라미터 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public IRecipeData Recipe { get; protected set; }

        protected BaseEquipmentNode(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 노드 자신의 데이터를 저장한다.
        /// Composite Pattern에 의해 하위 클래스(Unit, Machine)는 이 메서드를 override하여
        /// 자식 노드들의 Save()를 연쇄 호출한다.
        /// </summary>
        public virtual void Save()
        {
        }
    }
}
