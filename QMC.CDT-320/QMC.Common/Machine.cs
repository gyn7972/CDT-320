using System;
using System.Collections.Generic;

namespace QMC.Common
{
    /// <summary>
    /// 계층 구조의 최상위 루트 노드.
    /// 설비(Machine) 수준의 Setup / Config / Recipe 데이터와
    /// 복수의 Unit을 관리한다.
    /// </summary>
    /// <typeparam name="TSetup">ISetupData를 구현한 설비 전용 Setup 타입</typeparam>
    /// <typeparam name="TConfig">IConfigData를 구현한 설비 전용 Config 타입</typeparam>
    /// <typeparam name="TRecipe">IRecipeData를 구현한 설비 전용 Recipe 타입</typeparam>
    public abstract class Machine<TSetup, TConfig, TRecipe> : BaseEquipmentNode
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        /// <summary>설비가 관리하는 하위 유닛(Unit) 목록</summary>
        public List<BaseEquipmentNode> Units { get; } = new List<BaseEquipmentNode>();

        /// <summary>
        /// 타입 캐스팅 없이 설비 전용 Setup 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TSetup Setup
        {
            get => (TSetup)base.Setup;
            private set => base.Setup = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 설비 전용 Config 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TConfig Config
        {
            get => (TConfig)base.Config;
            private set => base.Config = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 설비 전용 Recipe 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TRecipe Recipe
        {
            get => (TRecipe)base.Recipe;
            private set => base.Recipe = value;
        }

        protected Machine(string name) : base(name)
        {
            Setup  = new TSetup();
            Config = new TConfig();
            Recipe = new TRecipe();
        }

        /// <summary>
        /// Composite Save: 설비 자신의 데이터를 먼저 저장한 뒤, 모든 하위 Unit의 Save()를 연쇄 호출한다.
        /// </summary>
        public override void Save()
        {
            Console.WriteLine($"[Machine]   '{Name}' ? Setup / Config / Recipe 저장 완료");

            // Composite Pattern: 자식 Unit들에게 Save()를 위임 → Unit은 다시 Component에 위임
            foreach (BaseEquipmentNode unit in Units)
                unit.Save();
        }
    }
}
