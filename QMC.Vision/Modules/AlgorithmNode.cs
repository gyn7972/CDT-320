using QMC.Common;
using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// 모듈 안의 알고리즘 1개(Finder/Inspector)를 나타내는 Composite 자식 노드.
    /// <see cref="BaseUnit{TSetup,TConfig,TRecipe}"/> 를 상속하여 자체 Setup/Config/Recipe 를 가지며,
    /// 모듈(부모)의 Save/Load/Delete 가 <see cref="BaseUnit{TSetup,TConfig,TRecipe}.Components"/> 를 통해 이 노드로 연쇄된다.
    /// StorageKey 는 "모듈키.알고리즘Id" 형태로 부여해 알고리즘별 파일을 분리한다.
    /// </summary>
    public abstract class AlgorithmNode<TSetup, TConfig, TRecipe>
        : BaseUnit<TSetup, TConfig, TRecipe>, IAlgorithmNode
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        protected AlgorithmNode(string storageKey) : base(storageKey)
        {
        }

        /// <summary>래핑한 Finder. Inspector 노드면 null.</summary>
        public virtual IPatternFinder Finder => null;

        /// <summary>래핑한 Inspector. Finder 노드면 null.</summary>
        public virtual IInspector Inspector => null;

        // 비형식화 데이터 접근 — 형식화 접근(BaseUnit.Setup 등)을 인터페이스로 노출.
        ISetupData  IAlgorithmNode.Setup  => Setup;
        IConfigData IAlgorithmNode.Config => Config;
        IRecipeData IAlgorithmNode.Recipe => Recipe;
    }

    /// <summary>Finder 알고리즘 노드.</summary>
    public sealed class FinderAlgorithm<TSetup, TConfig, TRecipe>
        : AlgorithmNode<TSetup, TConfig, TRecipe>
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        private readonly IPatternFinder _finder;

        public FinderAlgorithm(string storageKey, IPatternFinder finder) : base(storageKey)
        {
            _finder = finder;
        }

        public override IPatternFinder Finder => _finder;
    }

    /// <summary>Inspector 알고리즘 노드.</summary>
    public sealed class InspectorAlgorithm<TSetup, TConfig, TRecipe>
        : AlgorithmNode<TSetup, TConfig, TRecipe>
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        private readonly IInspector _inspector;

        public InspectorAlgorithm(string storageKey, IInspector inspector) : base(storageKey)
        {
            _inspector = inspector;
        }

        public override IInspector Inspector => _inspector;
    }
}
