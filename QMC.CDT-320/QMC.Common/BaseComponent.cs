using System;

namespace QMC.Common
{
    /// <summary>
    /// 계층 구조의 최하위 Leaf 노드 ? 하위 자식을 가지지 않는 단위 컴포넌트.
    /// 제네릭 제약조건(new())으로 생성자에서 데이터 인스턴스를 자동 할당한다.
    /// </summary>
    /// <typeparam name="TSetup">ISetupData를 구현한 컴포넌트 전용 Setup 타입</typeparam>
    /// <typeparam name="TConfig">IConfigData를 구현한 컴포넌트 전용 Config 타입</typeparam>
    /// <typeparam name="TRecipe">IRecipeData를 구현한 컴포넌트 전용 Recipe 타입</typeparam>
    public abstract class BaseComponent<TSetup, TConfig, TRecipe> : BaseEquipmentNode
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        /// <summary>
        /// 타입 캐스팅 없이 컴포넌트 전용 Setup 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TSetup Setup
        {
            get => (TSetup)base.Setup;
            private set => base.Setup = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 컴포넌트 전용 Config 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TConfig Config
        {
            get => (TConfig)base.Config;
            private set => base.Config = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 컴포넌트 전용 Recipe 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TRecipe Recipe
        {
            get => (TRecipe)base.Recipe;
            private set => base.Recipe = value;
        }

        protected BaseComponent(string name) : base(name)
        {
            // 제네릭 제약조건(new())을 이용해 데이터 인스턴스를 자동 생성
            Setup  = new TSetup();
            Config = new TConfig();
            Recipe = new TRecipe();
        }

        /// <summary>
        /// Leaf 노드의 Save: 자신의 데이터만 저장하고 자식 순회는 없다.
        /// </summary>
        public override void Save()
        {
            Console.WriteLine($"[Component] '{Name}' ? Setup / Config / Recipe 저장 완료");
        }
    }
}
