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
            {
                _finder.MaxInstances      = c.MaxInstances;
                _finder.AngleEnabled      = c.AngleEnabled;
                _finder.AngleToleranceDeg = c.AngleToleranceDeg;
                _finder.AngleStepDeg      = c.AngleStepDeg;
            }
            // 검출 모드(레시피 AngleMode) → finder: Single=센터 최근접 1개 / Multi=전체(점수 상위 다수).
            if (Recipe is FinderAlgoRecipe rm)
            {
                bool single = (rm.AngleMode == DieAngleMode.Single);
                _finder.PreferNearestCenter = single;
                _finder.MaxInstances = single ? 1 : System.Math.Max(_finder.MaxInstances, 64);
            }
            // ① per-algorithm 전용필드 — 백엔드 선택 구현. 미구현 = no-op.
            if (_finder is IAlgoParamSync s) s.ApplyParams(Recipe, Config, Setup);
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
            {
                c.MaxInstances      = _finder.MaxInstances;
                c.AngleEnabled      = _finder.AngleEnabled;
                c.AngleToleranceDeg = _finder.AngleToleranceDeg;
                c.AngleStepDeg      = _finder.AngleStepDeg;
            }
            if (_finder is IAlgoParamSync s) s.CollectParams(Recipe, Config, Setup);
        }

        // ── 학습 패턴(PNG) 영속화 — 레시피 폴더에 co-locate(<StorageKey>.train.png).
        //    레시피 로드/저장 때 런타임 finder 의 TrainImage 를 복원/보존한다.
        //    (기존엔 UI 페이지에서만 복원 → 재시작 후 런타임/TCP/시퀀서 finder 는 패턴이 비어 MATCH 실패. 노드 레벨로 이동해 항상 복원.)
        public override void LoadRecipe(string recipeName)
        {
            base.LoadRecipe(recipeName);     // POCO 로드 + ApplyToRuntime
            LoadTrainPattern(recipeName);    // 학습 패턴 복원(런타임 finder 주입)
        }

        public override bool SaveRecipe(string recipeName)
        {
            bool ok = base.SaveRecipe(recipeName);
            SaveTrainPattern(recipeName);
            return ok;
        }

        private string TrainPatternPath(string recipeName)
            => System.IO.Path.Combine(
                QMC.Common.Data.Store.RecipeDataStore.DirOf(recipeName),
                QMC.Common.Data.Store.StorageName.Safe(StorageKey) + ".train.png");

        private void LoadTrainPattern(string recipeName)
        {
            if (_finder == null) return;
            try
            {
                string path = TrainPatternPath(recipeName);
                if (!System.IO.File.Exists(path))
                {
                    _finder.LoadTrainImage(null);
                    LogTrain("LoadTrainPattern recipe='" + recipeName + "' key='" + StorageKey + "' → PNG 없음(" + path + ") → 패턴 비움(Match 불가)");
                    return;
                }
                // 작동하는 그랩(LoadImageAsGrab)과 동일하게: 바이트→MemoryStream→FromStream(기본 검증).
                // (FromStream(fs,false,false)=검증 생략 조합은 일부 PNG 에서 "GDI+ 일반 오류" 유발.)
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                using (var ms = new System.IO.MemoryStream(bytes))
                using (var src = System.Drawing.Image.FromStream(ms))
                    _finder.LoadTrainImage((System.Drawing.Bitmap)src);   // 내부에서 new Bitmap 깊은 복사
                LogTrain("LoadTrainPattern recipe='" + recipeName + "' key='" + StorageKey + "' → PNG 로드(" + path + "), 복원=" + (_finder.TrainImage != null));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[FinderAlgorithm] train pattern load fail (" + StorageKey + "): " + ex.Message);
                LogTrain("LoadTrainPattern 실패 recipe='" + recipeName + "' key='" + StorageKey + "': " + ex.Message);
            }
        }

        private void SaveTrainPattern(string recipeName)
        {
            if (_finder == null) return;
            try
            {
                string path = TrainPatternPath(recipeName);
                var ti = _finder.TrainImage;
                if (ti == null)
                {
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    LogTrain("SaveTrainPattern recipe='" + recipeName + "' key='" + StorageKey + "' → TrainImage=null → PNG 삭제/미저장(" + path + ")");
                    return;
                }
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                using (var bmp = new System.Drawing.Bitmap(ti))
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                LogTrain("SaveTrainPattern recipe='" + recipeName + "' key='" + StorageKey + "' → PNG 저장(" + path + ")");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[FinderAlgorithm] train pattern save fail (" + StorageKey + "): " + ex.Message);
                LogTrain("SaveTrainPattern 실패 recipe='" + recipeName + "' key='" + StorageKey + "': " + ex.Message);
            }
        }

        /// <summary>학습 패턴 저장/복원 진단 로그 — Vision DataLog(EventLogger User=VISION, Code=TrainPattern).</summary>
        private void LogTrain(string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "TrainPattern", message); }
            catch { }
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
                if (_inspector is CognexInspector cog) cog.Threshold = r.Threshold;   // double↔double
                else if (_inspector is QMC.Vision.Core.PlacementGapInspector pg)
                {
                    pg.Threshold     = r.Threshold;
                    pg.GapLowerLimit = r.GapLowerLimit;
                    pg.GapUpperLimit = r.GapUpperLimit;
                    pg.GapOffset     = r.GapOffset;
                    pg.PixelSizeXmm  = r.PixelSizeXmm;
                    pg.PixelSizeYmm  = r.PixelSizeYmm;
                    pg.DarkDie       = r.DarkDie;
                    pg.EdgeStep      = r.EdgeStep;
                    pg.BandTrim      = r.BandTrim;
                    pg.OutlierSigma  = r.OutlierSigma;
                }
                else if (_inspector is QMC.Vision.Core.BottomInspector bi)
                {
                    bi.ChipThreshold            = (int)r.Threshold;
                    bi.ChippingDepth            = r.ChippingDepth;
                    bi.ChippingLength           = r.ChippingLength;
                    bi.ForeignObjectSize        = r.ForeignObjectSize;
                    bi.TopHatRadius             = r.TopHatRadius;
                    bi.TopHatThreshold          = r.TopHatThreshold;
                    bi.MinForeignAreaFilterSize = r.MinForeignAreaFilterSize;
                    if (r.MaxForeignAreaFilterSize > 0) bi.MaxForeignAreaFilterSize = r.MaxForeignAreaFilterSize;
                    if (r.LinkDistance > 0)             bi.LinkDistance             = r.LinkDistance;
                    bi.ChipEdgeMargin           = r.ChipEdgeMargin;
                    bi.ForeignEdgeMargin        = r.ForeignEdgeMargin;
                    bi.ChipLowerSpecLimit       = new System.Drawing.SizeF((float)r.WidthLowerLimit, (float)r.HeightLowerLimit);
                    bi.ChipUpperSpecLimit       = new System.Drawing.SizeF((float)r.WidthUpperLimit, (float)r.HeightUpperLimit);
                    bi.DarkChip                 = r.DarkChip;
                    if (r.PixelSizeXmmBottom > 0) bi.PixelSizeWidthMm  = r.PixelSizeXmmBottom;
                    if (r.PixelSizeYmmBottom > 0) bi.PixelSizeHeightMm = r.PixelSizeYmmBottom;
                }
                else if (_inspector is QMC.Vision.Core.SideAppearanceInspector si)
                {
                    si.ChipThreshold            = (int)r.Threshold;
                    si.ChippingDepth            = r.ChippingDepth;
                    si.ChippingUpperLimit       = r.ChippingUpperLimit;
                    si.ChippingLowerLimit       = r.ChippingLowerLimit;
                    si.ScanRate                 = r.ScanRate > 0 ? r.ScanRate : si.ScanRate;
                    si.EnvelopeBinSize          = r.EnvelopeBinSize > 0 ? r.EnvelopeBinSize : si.EnvelopeBinSize;
                    si.KeepQuantile             = r.KeepQuantile > 0 ? r.KeepQuantile : si.KeepQuantile;
                    si.EdgeGap                  = r.EdgeGap > 0 ? r.EdgeGap : si.EdgeGap;
                    si.ChippingLength           = r.ChippingLength;
                    si.ForeignObjectSize        = r.ForeignObjectSize;
                    si.ChipThickness            = r.ChipThickness;
                    si.TopHatRadius             = r.TopHatRadius;
                    si.TopHatThreshold          = r.TopHatThreshold;
                    si.MinForeignAreaFilterSize = r.MinForeignAreaFilterSize;
                    si.MaxForeignAreaFilterSize = r.MaxForeignAreaFilterSize;
                    si.LinkDistance             = r.LinkDistance;
                    if (r.PixelSizeXmmBottom > 0) si.PixelSizeWidthMm  = r.PixelSizeXmmBottom;
                }
            }
            // ① per-algorithm 전용필드 — 백엔드 선택 구현. 미구현 = no-op.
            if (_inspector is IAlgoParamSync s) s.ApplyParams(Recipe, Config, Setup);

            // ② 추세 차트 상/하한(Limit) → ChartLimitStore: 레시피 적용 즉시 운영뷰 차트에 반영(검사 실행 전에도 빨간 점선 표시).
            try
            {
                if (_inspector is QMC.Vision.Core.BottomInspector bl)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Bottom", 0, bl.ChipUpperSpecLimit.Width,  bl.ChipLowerSpecLimit.Width);
                    QMC.Vision.Core.ChartLimitStore.Set("Bottom", 1, bl.ChipUpperSpecLimit.Height, bl.ChipLowerSpecLimit.Height);
                }
                else if (_inspector is QMC.Vision.Core.SideAppearanceInspector sl)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Side", 0, sl.ChippingUpperLimit, sl.ChippingLowerLimit);
                    QMC.Vision.Core.ChartLimitStore.Set("Side", 1, sl.ChippingUpperLimit, sl.ChippingLowerLimit);
                }
                else if (_inspector is QMC.Vision.Core.PlacementGapInspector pl)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Bin", 0, pl.GapUpperLimit, pl.GapLowerLimit);
                    QMC.Vision.Core.ChartLimitStore.Set("Bin", 1, pl.GapUpperLimit, pl.GapLowerLimit);
                }
            }
            catch { }
        }

        protected override void CollectFromRuntime()
        {
            if (_inspector == null) return;
            if (Recipe is InspectorAlgoRecipe r)
            {
                r.InspectionRoi = _inspector.InspectionRoi?.Clone();
                if (_inspector is CognexInspector cog) r.Threshold = cog.Threshold;
                else if (_inspector is QMC.Vision.Core.PlacementGapInspector pg)
                {
                    r.Threshold     = pg.Threshold;
                    r.GapLowerLimit = pg.GapLowerLimit;
                    r.GapUpperLimit = pg.GapUpperLimit;
                    r.GapOffset     = pg.GapOffset;
                    r.PixelSizeXmm  = pg.PixelSizeXmm;
                    r.PixelSizeYmm  = pg.PixelSizeYmm;
                    r.DarkDie       = pg.DarkDie;
                    r.EdgeStep      = pg.EdgeStep;
                    r.BandTrim      = pg.BandTrim;
                    r.OutlierSigma  = pg.OutlierSigma;
                }
                else if (_inspector is QMC.Vision.Core.BottomInspector bi)
                {
                    r.Threshold                = bi.ChipThreshold;
                    r.ChippingDepth            = bi.ChippingDepth;
                    r.ChippingLength           = bi.ChippingLength;
                    r.ForeignObjectSize        = bi.ForeignObjectSize;
                    r.TopHatRadius             = bi.TopHatRadius;
                    r.TopHatThreshold          = bi.TopHatThreshold;
                    r.MinForeignAreaFilterSize = bi.MinForeignAreaFilterSize;
                    r.MaxForeignAreaFilterSize = bi.MaxForeignAreaFilterSize;
                    r.LinkDistance             = bi.LinkDistance;
                    r.ChipEdgeMargin           = bi.ChipEdgeMargin;
                    r.ForeignEdgeMargin        = bi.ForeignEdgeMargin;
                    r.WidthLowerLimit          = bi.ChipLowerSpecLimit.Width;
                    r.HeightLowerLimit         = bi.ChipLowerSpecLimit.Height;
                    r.WidthUpperLimit          = bi.ChipUpperSpecLimit.Width;
                    r.HeightUpperLimit         = bi.ChipUpperSpecLimit.Height;
                    r.DarkChip                 = bi.DarkChip;
                    r.PixelSizeXmmBottom       = bi.PixelSizeWidthMm;
                    r.PixelSizeYmmBottom       = bi.PixelSizeHeightMm;
                }
                else if (_inspector is QMC.Vision.Core.SideAppearanceInspector si)
                {
                    r.Threshold                = si.ChipThreshold;
                    r.ChippingDepth            = si.ChippingDepth;
                    r.ChippingUpperLimit       = si.ChippingUpperLimit;
                    r.ChippingLowerLimit       = si.ChippingLowerLimit;
                    r.ScanRate                 = si.ScanRate;
                    r.EnvelopeBinSize          = si.EnvelopeBinSize;
                    r.KeepQuantile             = si.KeepQuantile;
                    r.EdgeGap                  = si.EdgeGap;
                    r.ChippingLength           = si.ChippingLength;
                    r.ForeignObjectSize        = si.ForeignObjectSize;
                    r.ChipThickness            = si.ChipThickness;
                    r.TopHatRadius             = si.TopHatRadius;
                    r.TopHatThreshold          = si.TopHatThreshold;
                    r.MinForeignAreaFilterSize = si.MinForeignAreaFilterSize;
                    r.MaxForeignAreaFilterSize = si.MaxForeignAreaFilterSize;
                    r.LinkDistance             = si.LinkDistance;
                    r.PixelSizeXmmBottom       = si.PixelSizeWidthMm;
                }
            }
            if (_inspector is IAlgoParamSync s) s.CollectParams(Recipe, Config, Setup);
        }
    }
}
