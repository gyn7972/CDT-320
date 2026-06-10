using QMC.Common;
using QMC.Vision.Backends.Cognex;
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

        // ── POCO ↔ 런타임 finder/inspector 동기화 (A) ──
        /// <summary>POCO(Setup/Config/Recipe) → 런타임 finder/inspector 주입. Load 직후 호출.</summary>
        protected virtual void ApplyToRuntime() { }
        /// <summary>런타임 finder/inspector → POCO 수집. Save 직전 호출.</summary>
        protected virtual void CollectFromRuntime() { }

        public override void LoadSettings()       { base.LoadSettings();    ApplyToRuntime(); }
        public override void LoadRecipe(string n) { base.LoadRecipe(n);     ApplyToRuntime(); }
        public override bool SaveSettings()       { CollectFromRuntime();   return base.SaveSettings(); }
        public override bool SaveRecipe(string n) { CollectFromRuntime();   return base.SaveRecipe(n); }
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

        // 직접: SearchRoi/TrainRoi(Recipe), AcceptThreshold(Recipe), MaxInstances(Config).
        // POCO-only: TrainModelPath(Recipe, 문자열만), AngleEnabled(Config). 모델 직렬화는 후속.
        protected override void ApplyToRuntime()
        {
            if (_finder == null) return;
            if (Recipe is FinderAlgoRecipe r)
            {
                if (r.SearchRoi != null) _finder.SearchRoi = r.SearchRoi.Clone();
                if (r.TrainRoi  != null) _finder.TrainRoi  = r.TrainRoi.Clone();
                _finder.AcceptThreshold = r.AcceptThreshold;
            }
            if (Config is FinderAlgoConfig c)
                _finder.MaxInstances = c.MaxInstances;
        }

        protected override void CollectFromRuntime()
        {
            if (_finder == null) return;
            if (Recipe is FinderAlgoRecipe r)
            {
                r.SearchRoi = _finder.SearchRoi?.Clone();
                r.TrainRoi  = _finder.TrainRoi?.Clone();
                r.AcceptThreshold = _finder.AcceptThreshold;
            }
            if (Config is FinderAlgoConfig c)
                c.MaxInstances = _finder.MaxInstances;
        }
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

        // 직접: InspectionRoi(Recipe). 백엔드한정: Threshold↔Cognex.Threshold(int 변환).
        // POCO-only: Enable(Config), CalibModelPath(Setup), Sim/OpenCv 의 Threshold.
        protected override void ApplyToRuntime()
        {
            if (_inspector == null) return;
            if (Recipe is InspectorAlgoRecipe r)
            {
                if (r.InspectionRoi != null) _inspector.InspectionRoi = r.InspectionRoi.Clone();
                if (_inspector is CognexInspector cog) cog.Threshold = r.Threshold;   // int↔int (소실 제거)
            }
        }

        protected override void CollectFromRuntime()
        {
            if (_inspector == null) return;
            if (Recipe is InspectorAlgoRecipe r)
            {
                r.InspectionRoi = _inspector.InspectionRoi?.Clone();
                if (_inspector is CognexInspector cog) r.Threshold = cog.Threshold;
            }
        }
    }
}
