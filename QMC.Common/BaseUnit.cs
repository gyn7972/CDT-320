using System;
using System.Collections.Generic;
using QMC.Common.Data.Store;

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
            try
            {
                bool ok = UnitDataStore.SaveSetup(Setup, StorageKey);
                ok &= UnitDataStore.SaveConfig(Config, StorageKey);

                foreach (BaseEquipmentNode component in Components)
                    ok &= component.SaveSettings();

                return ok;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Composite: 자신의 Setup / Config 를 로드한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                Setup = UnitDataStore.LoadSetup(StorageKey, Setup);
                Config = UnitDataStore.LoadConfig(StorageKey, Config);

                foreach (BaseEquipmentNode component in Components)
                    component.LoadSettings();
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// Composite: 레시피 이름별 Recipe 를 저장한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override bool SaveRecipe(string recipeName)
        {
            try
            {
                bool ok = UnitDataStore.SaveRecipe(Recipe, recipeName, StorageKey);

                foreach (BaseEquipmentNode component in Components)
                    ok &= component.SaveRecipe(recipeName);

                return ok;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Composite: 레시피 이름별 Recipe 를 로드한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override void LoadRecipe(string recipeName)
        {
            try
            {
                Recipe = UnitDataStore.LoadRecipe(recipeName, StorageKey, Recipe);

                foreach (BaseEquipmentNode component in Components)
                    component.LoadRecipe(recipeName);
            }
            catch
            {
            }
            finally
            {
            }
        }
        /// <summary>
        /// Composite: 자신의 Setup / Config 저장 파일을 삭제한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override bool DeleteSettings()
        {
            try
            {
                bool ok = UnitDataStore.DeleteSetup(StorageKey);
                ok &= UnitDataStore.DeleteConfig(StorageKey);

                foreach (BaseEquipmentNode component in Components)
                    ok &= component.DeleteSettings();

                return ok;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Composite: 레시피 이름별 Recipe 저장 파일을 삭제한 뒤 모든 자식 Component로 위임.
        /// </summary>
        public override bool DeleteRecipe(string recipeName)
        {
            try
            {
                bool ok = UnitDataStore.DeleteRecipe(recipeName, StorageKey);

                foreach (BaseEquipmentNode component in Components)
                    ok &= component.DeleteRecipe(recipeName);

                return ok;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

    }
}
