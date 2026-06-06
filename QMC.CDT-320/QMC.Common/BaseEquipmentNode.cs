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

        /// <summary>
        /// 영속화(저장/로드)에 사용하는 키. 파일명으로 쓰인다.
        /// 표시용 <see cref="Name"/> 을 리팩터링으로 변경해도 기존 데이터를 계속 읽도록
        /// 저장 키를 별도로 분리한다. 기본값은 <see cref="Name"/>.
        /// </summary>
        public string StorageKey { get; protected set; }

        /// <summary>기구적 설정값 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public ISetupData Setup { get; protected set; }

        /// <summary>고정 사양 파라미터 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public IConfigData Config { get; protected set; }

        /// <summary>공정별 작업 파라미터 데이터 (비형식화된 접근용 ? 하위 클래스에서 섀도잉됨)</summary>
        public IRecipeData Recipe { get; protected set; }

        protected BaseEquipmentNode(string name)
        {
            Name = name;
            StorageKey = name;
        }

        /// <summary>
        /// 노드 자신의 데이터를 저장한다.
        /// Composite Pattern에 의해 하위 클래스(Unit, Machine)는 이 메서드를 override하여
        /// 자식 노드들의 Save()를 연쇄 호출한다.
        /// </summary>
        public virtual void Save()
        {
        }

        /// <summary>
        /// Setup / Config 를 저장한다 (노드당 1개). 하위 클래스에서 Composite 연쇄로 override.
        /// 하나라도 실패하면 false 를 반환한다.
        /// </summary>
        public virtual bool SaveSettings()
        {
            return true;
        }

        /// <summary>
        /// Setup / Config 를 로드한다 (노드당 1개). 하위 클래스에서 Composite 연쇄로 override.
        /// </summary>
        public virtual void LoadSettings()
        {
        }

        /// <summary>
        /// 지정한 레시피 이름으로 Recipe 를 저장한다. 하위 클래스에서 Composite 연쇄로 override.
        /// 하나라도 실패하면 false 를 반환한다.
        /// </summary>
        public virtual bool SaveRecipe(string recipeName)
        {
            return true;
        }

        /// <summary>
        /// 지정한 레시피 이름의 Recipe 를 로드한다. 하위 클래스에서 Composite 연쇄로 override.
        /// </summary>
        public virtual void LoadRecipe(string recipeName)
        {
        }
    }
}
