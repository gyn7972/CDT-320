using QMC.Common;
using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Vision PC 설비 루트 — 핸들러 <c>CDT320Machine</c> 정렬.
    /// <see cref="Machine{TSetup,TConfig,TRecipe}"/> 루트로, 5개 비전 모듈(Unit)을 <c>Units</c> 로 소유한다.
    /// 계층: Machine → Unit(Module) → Component(Camera) → Algorithm.
    /// Save/Load/Recipe 는 Composite 로 전 모듈에 연쇄된다(레시피 동기화 단일 진입점).
    /// </summary>
    public sealed class VisionMachine : Machine<VisionMachineSetup, VisionMachineConfig, VisionMachineRecipe>
    {
        public WaferVisionModule      WaferVision      { get; }
        public BinVisionModule        BinVision        { get; }
        public BottomInspectionModule BottomInspection { get; }
        public TopSideVisionModule    TopSideVision    { get; }
        public BottomSideVisionModule BottomSideVision { get; }

        /// <summary>현재 활성 레시피 명칭(핸들러 수신 = Recipe.RecipeName). 미설정 시 "default".</summary>
        public string CurrentRecipeName =>
            string.IsNullOrWhiteSpace(Recipe?.RecipeName) ? "default" : Recipe.RecipeName;

        /// <summary>사용자(또는 핸들러)가 활성 레시피(품목)를 전환 — 전 모듈 Composite 재로드 후 레시피명 고정.</summary>
        public void SetRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "default";
            LoadRecipe(name);                       // Composite: 머신+5모듈 해당 레시피 로드
            if (Recipe != null) Recipe.RecipeName = name;
        }

        /// <summary>새 레시피(품목) 생성 — 런타임을 기본값으로 리셋 후 그 이름으로 저장(빈 레시피).
        /// Composite LoadRecipe 는 파일이 없으면 현재값을 유지하므로, New 는 명시적으로 리셋해야 한다.</summary>
        public void NewRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "default";
            ResetRuntimeToDefaults();
            if (Recipe != null) Recipe.RecipeName = name;
            SaveRecipe(name);   // CollectFromRuntime(리셋된 값) → 빈 레시피로 저장(폴더 생성)
        }

        /// <summary>전 모듈 Finder/Inspector 런타임을 기본값으로 리셋(ROI·임계·학습이미지). 머신 공통값도 초기화.</summary>
        public void ResetRuntimeToDefaults()
        {
            // Machine.Recipe 는 private set → 새 인스턴스 대신 제자리 리셋(명칭/번호 유지).
            Recipe?.ResetToDefaults();

            foreach (var unit in Units)
            {
                if (!(unit is IVisionModule m)) continue;
                foreach (var f in m.Finders.Values)
                {
                    f.SearchRoi = new Roi { Name = f.Id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
                    f.TrainRoi  = new Roi { Name = f.Id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
                    f.AcceptThreshold = 0.7;
                    f.MaxInstances    = 1;
                    f.LoadTrainImage(null);   // 학습 패턴 제거
                }
                foreach (var ins in m.Inspectors.Values)
                {
                    ins.InspectionRoi = new Roi { Name = ins.Id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
                }
            }
        }

        public VisionMachine(
            WaferVisionModule wafer,
            BinVisionModule bin,
            BottomInspectionModule bottom,
            TopSideVisionModule topSide,
            BottomSideVisionModule bottomSide)
            : base("CDT-320-VISION")
        {
            WaferVision      = wafer;
            BinVision        = bin;
            BottomInspection = bottom;
            TopSideVision    = topSide;
            BottomSideVision = bottomSide;

            // Composite: 핸들러 CDT320Machine 과 동일하게 하위 Unit 을 Units 에 등록.
            if (wafer      != null) Units.Add(wafer);
            if (bin        != null) Units.Add(bin);
            if (bottom     != null) Units.Add(bottom);
            if (topSide    != null) Units.Add(topSide);
            if (bottomSide != null) Units.Add(bottomSide);
        }
    }
}
