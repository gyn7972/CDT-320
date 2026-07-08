using QMC.Common;
using QMC.Common.Recipes;
using QMC.Vision.Core;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// 비전 모듈의 비제네릭 공통 표면.
    /// 모듈별 Setup/Config/Recipe 타입이 달라(제네릭) 컬렉션/소비자(Form1, VisionTcpServer, UI Page)는
    /// 이 인터페이스로 모듈을 참조한다.
    /// 영속화(저장/불러오기/삭제)는 <see cref="BaseEquipmentNode"/> 의 Composite 메서드를 그대로 노출한다.
    /// </summary>
    public interface IVisionModule : IDisposable
    {
        // ── 식별 ──
        string Name { get; }
        string StorageKey { get; }
        string AlgorithmKey { get; }

        // ── 구성 요소 ──
        ICamera Camera { get; }
        IVisionBackend Backend { get; }

        // ── C1: 카메라 설정 SSOT(모듈 Config/Recipe) ──
        /// <summary>Config.CameraId — Form1 이 카메라 생성에 사용.</summary>
        string CameraId { get; }
        /// <summary>모듈 Config/Recipe → 현재 Camera 적용.</summary>
        void ApplyCameraSettings();
        /// <summary>CameraMappingPanel 워킹버퍼(AlgorithmCameraMapping) → 모듈 Config/Recipe 반영(UI 편집 저장).</summary>
        void ImportCameraMapping(AlgorithmCameraMapping m);
        /// <summary>모듈 Config/Recipe → AlgorithmCameraMapping(편집 UI 로드 스냅샷).</summary>
        AlgorithmCameraMapping ExportCameraMapping();

        /// <summary>조명 지정 모듈 이전 — 구 노드 LightPages/Recipe 레벨의 (Port,Page) → 모듈 Setup.LightPages 합집합(빈 모듈만).</summary>
        bool MigrateLightPages();

        /// <summary>모듈 Setup(조명 LightPages 등 카메라=조명 하드웨어 지정 포함) — 비형식화 접근.</summary>
        ISetupData Setup { get; }

        IReadOnlyDictionary<string, IPatternFinder> Finders { get; }
        IReadOnlyDictionary<string, IInspector> Inspectors { get; }

        /// <summary>모듈 안의 알고리즘(Finder/Inspector) 노드 목록 — 알고리즘별 Setup/Config/Recipe 관리.</summary>
        IReadOnlyList<IAlgorithmNode> Algorithms { get; }

        /// <summary>id(Finder/Inspector Id)로 알고리즘 노드를 찾는다. 없으면 null.</summary>
        IAlgorithmNode GetAlgorithm(string id);

        // ── 런타임 동작 ──
        int DelayBeforeGrabMs { get; set; }
        int AverageCount { get; set; }
        event Action<string> ExposureDone;
        event Action<string, string> Alarmed;
        long ViewerFrameSeq { get; }

        /// <summary>픽셀↔mm 스케일(mm/px) — 검사기 측정 mm 환산용(전역 단위 토글이 사용).</summary>
        double ScaleX { get; }
        double ScaleY { get; }

        GrabResult Grab(int timeoutMs = 3000);

        /// <summary>true 동안 <see cref="Grab"/> 가 라이브(연속 촬상)를 자동 정지하지 않는다.
        /// 오토포커스 측정처럼 라이브(스트로브) 상태에서 의도적으로 그랩할 때 설정한다.</summary>
        bool SuppressLiveAutoStopOnGrab { get; set; }

        /// <summary>카메라 시뮬레이션 여부 — 저장이미지/오버라이드 그랩은 이 경우에만 허용.
        /// 실카메라가 붙어 있으면 모든 그랩/검사는 항상 실제 촬상을 사용한다(디스크 이미지 금지).</summary>
        bool IsSimCameraMode { get; }

        /// <summary>도구(Finder/Inspector) 단위 그랩 — 카메라가 시뮬레이션이고 그 도구에 전용 저장이미지가
        /// 지정되어 있으면 그것을 우선 로드, 아니면 <see cref="Grab(int)"/>(실제 카메라)로 위임한다.</summary>
        GrabResult GrabForTool(string toolId, int timeoutMs = 3000);

        /// <summary>도구 촬상 준비 — 노출(도구 전용 or 모듈 기본) + 조명(도구 Recipe.LightSettings) 적용.
        /// 조명 컨트롤러 배치 캐시가 동일 값이면 통신/안정화 대기를 생략한다. 라이브 시작 전에도 호출한다.</summary>
        void PrepareToolAcquisition(string toolId);

        void SetCamera(ICamera newCamera);
        void RaiseAlarm(string reason);
        Bitmap AcquireViewerFrame();

        /// <summary>테스트용 in-memory 그랩 이미지 주입. 설정 시 Grab()이 이 이미지를 반환(null=해제).</summary>
        void SetSimOverrideImage(Bitmap bmp);

        bool Calibrate(double chipWidthMm, double chipHeightMm,
                       out double scaleX, out double scaleY, out string err);
        bool MeasureRotationalCenter(out List<PointF> corners, out string err);
        bool LearnDistortion(out string err);
        bool MeasureFocus(out List<KeyValuePair<string, double>> roiFocus, out string err);

        // ── 영속화 (BaseUnit Composite — 모듈 + 모든 알고리즘 자식 노드 연쇄) ──
        bool SaveSettings();
        void LoadSettings();
        bool SaveRecipe(string recipeName);
        void LoadRecipe(string recipeName);
        bool DeleteSettings();
        bool DeleteRecipe(string recipeName);
    }

    /// <summary>
    /// 모듈 안의 알고리즘 1개(Finder 또는 Inspector)의 비제네릭 표면.
    /// 자체 Setup/Config/Recipe 를 가지며 모듈과 독립적으로 저장/불러오기/삭제/수정된다.
    /// </summary>
    public interface IAlgorithmNode
    {
        string Name { get; }
        string StorageKey { get; }

        /// <summary>래핑한 Finder (Inspector 노드면 null).</summary>
        IPatternFinder Finder { get; }

        /// <summary>래핑한 Inspector (Finder 노드면 null).</summary>
        IInspector Inspector { get; }

        // 비형식화 데이터 접근 (형식화 접근은 구체 노드에서 new 섀도잉).
        ISetupData Setup { get; }
        IConfigData Config { get; }
        IRecipeData Recipe { get; }

        bool SaveSettings();
        void LoadSettings();
        bool SaveRecipe(string recipeName);
        void LoadRecipe(string recipeName);
        bool DeleteSettings();
        bool DeleteRecipe(string recipeName);
    }
}
