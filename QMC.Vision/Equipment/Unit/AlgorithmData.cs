using QMC.Common;
using QMC.Common.Recipes;
using QMC.Vision.Core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  알고리즘 레벨 Setup / Config / Recipe
    //  Finder/Inspector 가 공통으로 쓰는 재사용 데이터 타입.
    //  알고리즘마다 StorageKey 가 달라 파일이 분리되므로, 스키마는 공유해도
    //  저장/불러오기/삭제/수정은 알고리즘별로 독립이다.
    //  특정 알고리즘이 전용 필드가 필요하면 이 타입을 상속하지 말고
    //  새 구체 타입을 만들어 AddFinder<...>/AddInspector<...> 에 지정한다.
    // ──────────────────────────────────────────────────────────────

    // ── 공통 base (C2: 조명 흡수) ─────────────────────────────────
    //  조명 결선(Setup)/레벨(Recipe)을 Finder/Inspector 노드에 1회 정의.
    //  타입은 기존 QMC.Common.Recipes 재사용(Common 무수정). 직렬화는 노드 구체
    //  타입(typeof(T))으로 수행되므로 base [DataMember] 가 자연 포함된다([KnownType] 불요).
    //  구 JSON 에 키가 없으면 로드 시 null → [OnDeserializing] 으로 빈 리스트 초기화(비파괴).

    /// <summary>검출 모드 — Single(센터 최근접 1개) / Multi(검색ROI 내 전체 패턴 검출).
    /// (구 "각도 모드". int 직렬화 유지: Single=0, Multi=1.)</summary>
    public enum DieAngleMode
    {
        /// <summary>이미지 센터에 가장 가까운 1개만 검출(위치/각).</summary>
        Single = 0,
        /// <summary>검색ROI 내 모든 패턴 검출(멀티 서치). AlignDie 는 전체로 평균각 산출.</summary>
        Multi = 1,
    }

    /// <summary>알고리즘 Setup 공통 base — 검사 노드 고정 설정. (조명 컨트롤러/페이지 지정은 모듈 Setup 으로 이전)</summary>
    [DataContract]
    public abstract class AlgoSetupBase : ISetupData
    {
        // 조명 컨트롤러/페이지 지정(LightPages)은 모듈 Setup(VisionModuleSetupBase)으로 이전 — 카메라=조명 1:1 하드웨어 계층.
        // 검사 노드는 조명 레벨(Recipe.LightSettings)만 보유. 구 노드 json 의 LightPages 키는 로드 시 무시(비파괴, 구 파일 보존).

        /// <summary>true 면 이 도구(Finder/Inspector)의 GRAB 시 <see cref="SimSavedImagePath"/> 저장 이미지를 사용(모듈 설정보다 우선).</summary>
        [DataMember] public bool SimUseSavedImage { get; set; }

        /// <summary>이 도구 전용 시뮬 그랩 이미지 경로 — 측면은 채널1(0°). (웨이퍼 2점 정렬 이미지1/2처럼 도구별 지정).</summary>
        [DataMember] public string SimSavedImagePath { get; set; }

        /// <summary>측면 채널2(90°) 전용 시뮬 그랩 이미지 경로. 비면 채널 무관하게 <see cref="SimSavedImagePath"/> 사용.</summary>
        [DataMember] public string SimSavedImagePathCh2 { get; set; }

        /// <summary>true 면 INSPECT 시 검출 오버레이(에지/박스/결함/측정값)를 그린 결과 이미지를 <see cref="DebugSavePath"/>에 저장.</summary>
        [DataMember] public bool DebugSaveEnabled { get; set; }

        /// <summary>결과(디버그) 이미지 저장 폴더 경로. 비면 저장 안 함.</summary>
        [DataMember] public string DebugSavePath { get; set; }

        [OnDeserializing] private void OnDeserializingAlgoSetup(StreamingContext ctx)
        { SimUseSavedImage = false; SimSavedImagePath = string.Empty; SimSavedImagePathCh2 = string.Empty; DebugSaveEnabled = false; DebugSavePath = string.Empty; }
    }

    /// <summary>알고리즘 Recipe 공통 base — 검사 조명 레벨(제품별 값). 키 = (ControllerPort, Channel).</summary>
    [DataContract]
    public abstract class AlgoRecipeBase : IRecipeData
    {
        /// <summary>검사별 채널 레벨/점등/스트로브/페이지. 키 = (ControllerPort, Channel). 채널은 Setup.LightPages 지정 컨트롤러의 ChannelCount.</summary>
        [DataMember] public List<InspectionLightSetting> LightSettings { get; set; } = new List<InspectionLightSetting>();

        protected AlgoRecipeBase() { LightSettings = new List<InspectionLightSetting>(); }
        [OnDeserializing] private void OnDeserializingLight(StreamingContext ctx)
        { LightSettings = new List<InspectionLightSetting>(); }
    }

    // ── Finder ────────────────────────────────────────────────────

    /// <summary>Finder Setup — 전원 OFF 후 유지되는 기구적 설정 + 조명 결선(base). 그 외 항목 없음(학습 ROI/모델은 Recipe, 2026-06-09).</summary>
    [DataContract]
    public class FinderAlgoSetup : AlgoSetupBase
    {
    }

    /// <summary>Finder Config — 백엔드 고정 사양.</summary>
    [DataContract]
    public class FinderAlgoConfig : IConfigData
    {
        /// <summary>최대 인스턴스 수.</summary>
        [DataMember] public int MaxInstances { get; set; }

        /// <summary>각도(회전) 탐색 사용 여부. true 면 [-AngleToleranceDeg, +AngleToleranceDeg] 범위를 탐색해 회전된 패턴도 매칭한다.</summary>
        [DataMember] public bool AngleEnabled { get; set; }

        /// <summary>회전 탐색 허용각(± deg) — Train 대비 이 범위 안의 회전까지 매칭 성공. 0 이하이면 회전 미탐색(평행이동).</summary>
        [DataMember] public double AngleToleranceDeg { get; set; }

        /// <summary>회전 탐색 각도 스텝(deg) — 작을수록 정밀·느림(기본 1°).</summary>
        [DataMember] public double AngleStepDeg { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { MaxInstances = 1; AngleEnabled = false; AngleToleranceDeg = 10.0; AngleStepDeg = 1.0; }
    }

    /// <summary>Finder Recipe — 제품/공정별 탐색·학습 파라미터(학습 ROI/모델 포함, 2026-06-09) + 조명 레벨(base).</summary>
    [DataContract]
    public class FinderAlgoRecipe : AlgoRecipeBase
    {
        /// <summary>최소 허용 score (0.0~1.0).</summary>
        [DataMember] public double AcceptThreshold { get; set; }

        /// <summary>탐색 ROI.</summary>
        [DataMember] public Roi SearchRoi { get; set; }

        /// <summary>학습 ROI(패턴 추출 영역). 제품/패턴별로 변경되므로 Recipe.</summary>
        [DataMember] public Roi TrainRoi { get; set; }

        /// <summary>학습된 패턴 모델 파일 경로(향후 백엔드 모델 직렬화 연동용, 현재 POCO-only). 비어있으면 미학습. 제품별이므로 Recipe.</summary>
        [DataMember] public string TrainModelPath { get; set; }

        /// <summary>검출 모드 — Single(센터 최근접 1개) / Multi(전체 패턴 검출). 웨이퍼 finder 공통.
        /// (필드명은 호환 위해 AngleMode 유지. AlignDie 는 Multi 일 때 전체로 평균각 산출.)</summary>
        [DataMember] public DieAngleMode AngleMode { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { AcceptThreshold = 0.7; TrainModelPath = string.Empty; AngleMode = DieAngleMode.Single; }
    }

    // ── Inspector ─────────────────────────────────────────────────

    /// <summary>Inspector Setup — 전원 OFF 후 유지되는 캘리브 + 조명 결선(base).</summary>
    [DataContract]
    public class InspectorAlgoSetup : AlgoSetupBase
    {
        /// <summary>검사 캘리브 모델 경로. 비어있으면 미설정.</summary>
        [DataMember] public string CalibModelPath { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { CalibModelPath = string.Empty; }
    }

    /// <summary>Inspector Config — 검사 고정 사양.</summary>
    [DataContract]
    public class InspectorAlgoConfig : IConfigData
    {
        /// <summary>검사 사용 여부.</summary>
        [DataMember] public bool Enable { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { Enable = true; }
    }

    /// <summary>Inspector Recipe — 제품/공정별 검사 임계값 + 조명 레벨(base).</summary>
    [DataContract]
    public class InspectorAlgoRecipe : AlgoRecipeBase
    {
        /// <summary>이 검사기를 품목별로 사용할지 여부. false 면 시퀀스·핸들러 모두에서 이 검사를 건너뛴다(PASS 처리).
        /// 측면 Surface 검사기에서는 '오염검사 사용' 역할. 기본 true(검사 수행). (POCO-only — 런타임 백킹 없음, Collect/Apply 불간섭)</summary>
        [DataMember] public bool UseInspection { get; set; } = true;

        /// <summary>검사 ROI.</summary>
        [DataMember] public Roi InspectionRoi { get; set; }

        /// <summary>합/불 판정 임계값(Blob HardFixedThreshold, 0~255 그레이, 소수 허용). 소비자=CognexInspector.Threshold(double).</summary>
        [DataMember] public double Threshold { get; set; }
        /// <summary>안착 갭 검사 하한(PlacementGapInspector).</summary>
        [DataMember] public double GapLowerLimit { get; set; }
        /// <summary>안착 갭 검사 상한(PlacementGapInspector).</summary>
        [DataMember] public double GapUpperLimit { get; set; }
        /// <summary>안착 갭 측정 보정 오프셋(기본 0, PixelSize 미설정 시 px).</summary>
        [DataMember] public double GapOffset { get; set; }
        /// <summary>가로 mm/pixel(0이면 px 단위로 측정·판정). 캘리브 시 주입.</summary>
        [DataMember] public double PixelSizeXmm { get; set; }
        /// <summary>세로 mm/pixel(0이면 px 단위).</summary>
        [DataMember] public double PixelSizeYmm { get; set; }
        /// <summary>다이가 배경보다 어두우면 true(기본 false=밝은 다이).</summary>
        [DataMember] public bool DarkDie { get; set; }
        /// <summary>에지 검출 스텝(px, 기본 3).</summary>
        [DataMember] public int EdgeStep { get; set; }
        /// <summary>변 양끝 무시 비율(기본 0.05).</summary>
        [DataMember] public double BandTrim { get; set; }
        /// <summary>gap 이상치 제거 σ배수(기본 2.0).</summary>
        [DataMember] public double OutlierSigma { get; set; }

        // ── Bottom/Side 검사기(310 포팅) 파라미터 ──
        [DataMember] public double ChippingDepth { get; set; }
        [DataMember] public double ChippingUpperLimit { get; set; }   // 측면 칩핑 상한(NG 기준, 화면 UpperLimit)
        [DataMember] public double ChippingLowerLimit { get; set; }   // 측면 칩핑 하한(화면 LowerLimit)
        // CDT-310 FindLine 라인검출 조정 파라미터(측면 칩핑)
        [DataMember] public double ScanRate { get; set; }             // FindTopLineOfChip dMagin
        [DataMember] public int    EnvelopeBinSize { get; set; }      // FindLine.EnvelopeBinSize
        [DataMember] public double KeepQuantile { get; set; }         // FindLine.TopKeepQuantile
        [DataMember] public int    EdgeGap { get; set; }              // 칩핑 에지결합 허용[px]
        [DataMember] public double ChippingLength { get; set; }
        [DataMember] public double ForeignObjectSize { get; set; }
        [DataMember] public int    TopHatRadius { get; set; }
        [DataMember] public int    TopHatThreshold { get; set; }
        [DataMember] public int    MinForeignAreaFilterSize { get; set; }
        [DataMember] public int    MaxForeignAreaFilterSize { get; set; }
        [DataMember] public int    LinkDistance { get; set; }
        [DataMember] public int    ChipEdgeMargin { get; set; }       // 칩핑 검사영역 오프셋[px] (CDT-310 margin)
        [DataMember] public int    ForeignEdgeMargin { get; set; }    // 이물 검사영역 오프셋[px] (CDT-310 *MarginForeign)
        [DataMember] public double WidthUpperLimit  { get; set; }     // 바텀 너비 상/하한[mm] (0=미설정) — 차트 Limit + 사이즈 NG
        [DataMember] public double WidthLowerLimit  { get; set; }
        [DataMember] public double HeightUpperLimit { get; set; }     // 바텀 높이 상/하한[mm]
        [DataMember] public double HeightLowerLimit { get; set; }
        [DataMember] public bool   DarkChip { get; set; }
        [DataMember] public double ChipThickness { get; set; }
        [DataMember] public double BladeWidth { get; set; }
        [DataMember] public double FirstBladeDepth { get; set; }
        [DataMember] public double PixelSizeXmmBottom { get; set; }   // 0=검사기 기본 사용
        [DataMember] public double PixelSizeYmmBottom { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults()
        {
            UseInspection = true; Threshold = 128.0; GapLowerLimit = 0.0; GapUpperLimit = 50.0; GapOffset = 0.0;
            PixelSizeXmm = 0.0; PixelSizeYmm = 0.0; DarkDie = false; EdgeStep = 3; BandTrim = 0.05; OutlierSigma = 2.0;
            // 310 BottomInspectionParameter / SideInspectionParameter 기본값
            ChippingDepth = 0.020; ChippingUpperLimit = 0.050; ChippingLowerLimit = -0.025; ChippingLength = 0.0; ForeignObjectSize = 0.5;
            ScanRate = 0.9; EnvelopeBinSize = 6; KeepQuantile = 0.35; EdgeGap = 6;
            TopHatRadius = 21; TopHatThreshold = 30; MinForeignAreaFilterSize = 36; MaxForeignAreaFilterSize = 100000; LinkDistance = 25; ChipEdgeMargin = 0; ForeignEdgeMargin = 12; DarkChip = false;
            ChipThickness = 0.25; BladeWidth = 0.048; FirstBladeDepth = 0.050;
            PixelSizeXmmBottom = 0.0; PixelSizeYmmBottom = 0.0;
        }
    }
}
