using System;
using System.Collections.Generic;
using QMC.Common.Persistence;

namespace QMC.Common
{
    /// <summary>
    /// 계층 구조의 중간 Composite 노드 ? 복수의 BaseEquipmentNode 자식을 관리한다.
    /// </summary>
    /// <typeparam name="TSetup">ISetupData를 구현한 유닛 전용 Setup 타입</typeparam>
    /// <typeparam name="TConfig">IConfigData를 구현한 유닛 전용 Config 타입</typeparam>
    /// <typeparam name="TRecipe">IRecipeData를 구현한 유닛 전용 Recipe 타입</typeparam>
    public abstract class BaseUnit<TSetup, TConfig, TRecipe> : BaseEquipmentNode
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        /// <summary>유닛이 관리하는 하위 노드(Component) 목록</summary>
        public List<BaseEquipmentNode> Components { get; } = new List<BaseEquipmentNode>();

        /// <summary>
        /// 타입 캐스팅 없이 유닛 전용 Setup 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TSetup Setup
        {
            get => (TSetup)base.Setup;
            private set => base.Setup = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 유닛 전용 Config 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TConfig Config
        {
            get => (TConfig)base.Config;
            private set => base.Config = value;
        }

        /// <summary>
        /// 타입 캐스팅 없이 유닛 전용 Recipe 데이터에 바로 접근하도록 섀도잉(new).
        /// </summary>
        public new TRecipe Recipe
        {
            get => (TRecipe)base.Recipe;
            private set => base.Recipe = value;
        }

        protected BaseUnit(string name) : base(name)
        {
            Setup  = new TSetup();
            Config = new TConfig();
            Recipe = new TRecipe();
        }

        /// <summary>
        /// Composite Save: 자신의 데이터를 먼저 저장한 뒤, 모든 자식 Component의 Save()를 연쇄 호출한다.
        /// </summary>
        public override void Save()
        {
            Console.WriteLine($"[Unit]      '{Name}' ? Setup / Config / Recipe 저장 완료");

            // Composite Pattern: 자식 노드들에게 Save()를 위임
            foreach (BaseEquipmentNode component in Components)
                component.Save();
        }

        /// <summary>
        /// Composite: 자신의 Setup / Config 를 저장한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override bool SaveSettings()
        {
            bool ok = EquipmentDataStore.Save(Setup,  StorageKey, "Setup");
            ok &= EquipmentDataStore.Save(Config, StorageKey, "Config");

            foreach (BaseEquipmentNode component in Components)
                ok &= component.SaveSettings();

            return ok;
        }

        /// <summary>
        /// Composite: 자신의 Setup / Config 를 로드한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override void LoadSettings()
        {
            Setup  = EquipmentDataStore.Load<TSetup>(StorageKey,  "Setup");
            Config = EquipmentDataStore.Load<TConfig>(StorageKey, "Config");

            foreach (BaseEquipmentNode component in Components)
                component.LoadSettings();
        }

        /// <summary>
        /// Composite: 레시피 이름별 Recipe 를 저장한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override bool SaveRecipe(string recipeName)
        {
            bool ok = UnitRecipeStore.Save(Recipe, recipeName, StorageKey);

            foreach (BaseEquipmentNode component in Components)
                ok &= component.SaveRecipe(recipeName);

            return ok;
        }

        /// <summary>
        /// Composite: 레시피 이름별 Recipe 를 로드한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override void LoadRecipe(string recipeName)
        {
            Recipe = UnitRecipeStore.Load<TRecipe>(recipeName, StorageKey);

            foreach (BaseEquipmentNode component in Components)
                component.LoadRecipe(recipeName);
        }
    }
}
