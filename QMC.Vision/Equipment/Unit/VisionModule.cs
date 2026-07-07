using QMC.Common;
using QMC.Common.Data.Store;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// 비전 모듈 베이스 — 카메라 + 이미지 처리 백엔드 + Finder/Inspector 를 한 단위로 묶음.
    /// 영속화는 BaseUnit Composite 구조 사용. 모듈 자신의 Setup/Config/Recipe 는 모듈별 고유 타입,
    /// 알고리즘(Finder/Inspector)은 AlgorithmNode 자식 노드로 등록되어 Save/Load/Delete 가 연쇄된다.
    /// </summary>
    public abstract class VisionModule<TSetup, TConfig, TRecipe>
        : BaseUnit<TSetup, TConfig, TRecipe>, IVisionModule
        where TSetup  : ISetupData,  new()
        where TConfig : IConfigData, new()
        where TRecipe : IRecipeData, new()
    {
        public ICamera         Camera  { get; private set; }
        public IVisionBackend  Backend { get; }

        /// <summary>VisionAlgorithm 상수 키 (조명/카메라 결선 조회용). 5 모듈이 override.</summary>
        public virtual string  AlgorithmKey => "";

        public Dictionary<string, IPatternFinder> Finders    { get; } = new Dictionary<string, IPatternFinder>();
        public Dictionary<string, IInspector>     Inspectors { get; } = new Dictionary<string, IInspector>();

        private readonly List<IAlgorithmNode> _algorithms = new List<IAlgorithmNode>();
        private readonly Dictionary<string, IAlgorithmNode> _algoById =
            new Dictionary<string, IAlgorithmNode>(StringComparer.OrdinalIgnoreCase);

        /// <summary>그랩 직전 지연 (ms).</summary>
        public int DelayBeforeGrabMs { get; set; } = 0;

        public event Action<string> ExposureDone;
        public event Action<string, string> Alarmed;

        private volatile bool _exposureEndFired;
        private volatile bool _grabInProgress;     // 모듈 Grab 진행 중 — 카메라 노출 이벤트→ExposureDone 승격 허용 창(라이브 발화 스팸 방지)
        private volatile bool _relayExposureEnd;   // 이 그랩에서 카메라 이벤트를 ExposureDone 으로 승격할지(EPD 억제 플래그를 그랩 시작 시 캡처)

        private readonly object _tapLock = new object();
        private Bitmap _lastFrame;
        private long   _frameSeq;

        protected VisionModule(string name, ICamera camera, IVisionBackend backend)
            : base(name)
        {
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));

            // 카메라 Component(Leaf) 등록 — 핸들러 BaseComponent 계층 정렬.
            // 카메라 설정(Setup/Config/Recipe) SSOT 는 이 노드이며 모듈 Components 로 Save/Load cascade.
            CameraNode = new VisionCamera(StorageKey + ".Camera");
            Components.Add(CameraNode);

            // camera null 허용: 부팅 시 모듈 먼저 생성 → LoadSettings(CameraNode.Config.CameraId) → SetCamera.
            Camera = camera;
            if (Camera != null)
            {
                Camera.ExposureEnded += OnCameraExposureEnded;
                Camera.FrameReceived += OnCameraFrameReceived;
            }
        }

        // ── Camera Component(Leaf) SSOT — 핸들러 BaseComponent 계층 정렬 ──
        /// <summary>카메라 Component 노드 — 카메라 Setup/Config/Recipe 독립 영속(모듈 Components 등록).</summary>
        public VisionCamera CameraNode { get; }

        /// <summary>CameraNode.Config.CameraId — Form1 이 카메라 생성에 사용(적용 아닌 생성 트리거).</summary>
        public string CameraId => CameraNode.Config.CameraId;

        /// <summary>픽셀↔mm 스케일(mm/px) — 설정의 카메라 ScaleX/Y. 전역 단위(mm/px) 환산에 사용.</summary>
        public double ScaleX => CameraNode?.Config?.ScaleX ?? 1.0;
        public double ScaleY => CameraNode?.Config?.ScaleY ?? 1.0;

        /// <summary>Camera Config/Recipe → AlgorithmCameraMapping(편집 UI/적용에서 재사용하는 스냅샷).</summary>
        public AlgorithmCameraMapping ExportCameraMapping()
        {
            var c = CameraNode.Config;
            var r = CameraNode.Recipe;
            var m = new AlgorithmCameraMapping { Algorithm = AlgorithmKey };
            m.CameraId = c.CameraId; m.Gain = c.Gain; m.FrameRate = c.FrameRate;
            m.TriggerMode = c.TriggerMode; m.PixelFormat = c.PixelFormat;
            m.DelayBeforeGrabMs = c.DelayBeforeGrabMs;
            m.RoiOffsetX = c.RoiOffsetX; m.RoiOffsetY = c.RoiOffsetY;
            m.RoiWidth = c.RoiWidth; m.RoiHeight = c.RoiHeight;
            m.ScaleX = c.ScaleX; m.ScaleY = c.ScaleY;
            m.IsRotated = c.IsRotated; m.InvertedX = c.InvertedX; m.InvertedY = c.InvertedY;
            m.ReturnMmCoordinates = c.ReturnMmCoordinates;
            m.CalibChipWidthMm = c.CalibChipWidthMm; m.CalibChipHeightMm = c.CalibChipHeightMm;
            m.SimUseSavedImage = c.SimUseSavedImage; m.SimSavedImagePath = c.SimSavedImagePath;
            m.NodeParams = c.NodeParams?.Select(p => p.Clone()).ToList() ?? new List<CameraNodeParam>();
            m.MvsFeatureFilePath = c.MvsFeatureFilePath;
            m.ExposureUs = r.Exposure;
            return m;
        }

        /// <summary>Camera Config/Recipe → Camera 적용(Binder 재활용). Camera null 시 no-op.</summary>
        public void ApplyCameraSettings()
        {
            if (Camera == null) return;
            AlgorithmCameraBinder.TryApplyParameters(Camera, ExportCameraMapping(), out _);
            DelayBeforeGrabMs = CameraNode.Config.DelayBeforeGrabMs;
        }

        /// <summary>Camera → Camera Config/Recipe 수집(저장 직전). Camera null 시 no-op.</summary>
        public void CollectCameraSettings()
        {
            if (Camera == null) return;
            var c = CameraNode.Config;
            var r = CameraNode.Recipe;
            try { c.Gain        = Camera.Gain; } catch { }
            try { c.FrameRate   = Camera.AcquisitionFrameRate; } catch { }
            try { c.TriggerMode = Camera.TriggerMode.ToString(); } catch { }
            try { c.PixelFormat = Camera.PixelFormat.ToString(); } catch { }
            try { var roi = Camera.Roi; c.RoiOffsetX = roi.X; c.RoiOffsetY = roi.Y; c.RoiWidth = roi.Width; c.RoiHeight = roi.Height; } catch { }
            c.DelayBeforeGrabMs = DelayBeforeGrabMs;
            try { r.Exposure = Camera.ExposureUs; } catch { }
            // CameraId 는 생성 트리거라 수집 안 함(UI 가 설정).
        }

        /// <summary>CameraMappingPanel 워킹버퍼(AlgorithmCameraMapping) → Camera Config/Recipe 반영(UI 편집 저장).</summary>
        public void ImportCameraMapping(AlgorithmCameraMapping m)
        {
            if (m == null) return;
            var c = CameraNode.Config;
            var r = CameraNode.Recipe;
            c.CameraId = m.CameraId; c.Gain = m.Gain; c.FrameRate = m.FrameRate;
            c.TriggerMode = m.TriggerMode; c.PixelFormat = m.PixelFormat;
            c.DelayBeforeGrabMs = m.DelayBeforeGrabMs;
            c.RoiOffsetX = m.RoiOffsetX; c.RoiOffsetY = m.RoiOffsetY;
            c.RoiWidth = m.RoiWidth; c.RoiHeight = m.RoiHeight;
            c.ScaleX = m.ScaleX; c.ScaleY = m.ScaleY;
            c.IsRotated = m.IsRotated; c.InvertedX = m.InvertedX; c.InvertedY = m.InvertedY;
            c.ReturnMmCoordinates = m.ReturnMmCoordinates;
            c.CalibChipWidthMm = m.CalibChipWidthMm; c.CalibChipHeightMm = m.CalibChipHeightMm;
            c.SimUseSavedImage = m.SimUseSavedImage; c.SimSavedImagePath = m.SimSavedImagePath;
            c.NodeParams = m.NodeParams?.Select(p => p.Clone()).ToList() ?? new List<CameraNodeParam>();
            c.MvsFeatureFilePath = m.MvsFeatureFilePath;
            r.Exposure = m.ExposureUs;
        }

        // ── 조명 지정 모듈 이전 마이그 — 구 검사 노드 LightPages + Recipe 레벨의 (Port,Page) → 모듈 Setup.LightPages 합집합 ──
        /// <summary>모듈 Setup.LightPages 가 비어 있으면, 소속 검사 노드들의 구 Setup json(LightPages 직독)과
        /// Recipe.LightSettings 의 (ControllerPort,Page)를 합집합 dedupe(키=Port+Page) 하여 모듈 지정 도출 후 저장.
        /// 카메라=조명 1:1 이라 보통 1건 수렴(다르면 전부 보존). 빈 모듈만 처리, 구 노드 파일 보존. 변경 시 true.</summary>
        public bool MigrateLightPages()
        {
            var msetup = Setup as VisionModuleSetupBase;
            if (msetup == null) return false;
            if (msetup.LightPages != null && msetup.LightPages.Count > 0) return false;   // 이미 모듈 지정 있음 → 스킵

            var collected = new List<LightPageRef>();
            foreach (var node in Algorithms)
            {
                // (a) 구 노드 Setup json 의 LightPages 직독(프로퍼티는 모듈로 이전됨 — 미지 멤버라 DTO 로 raw 로드)
                try
                {
                    var dto = UnitDataStore.LoadSetup<NodeLightPagesDto>(node.StorageKey);
                    if (dto?.LightPages != null) collected.AddRange(dto.LightPages);
                }
                catch { }
                // (b) 노드 Recipe 레벨의 (Port,Page) 보강(지정-only 가 아닌 레벨 데이터 흡수)
                var recipe = node.Recipe as AlgoRecipeBase;
                if (recipe?.LightSettings != null)
                    collected.AddRange(recipe.LightSettings
                        .Select(s => new LightPageRef { ControllerPort = s.ControllerPort, Page = s.Page }));
            }

            var pages = collected
                .Where(p => p != null && !string.IsNullOrEmpty(p.ControllerPort))
                .GroupBy(p => p.ControllerPort.ToUpperInvariant() + "/" + p.Page)
                .Select(g => new LightPageRef { ControllerPort = g.First().ControllerPort, Page = g.First().Page })
                .ToList();
            if (pages.Count == 0) return false;

            msetup.LightPages = pages;
            try { SaveSettings(); } catch { }
            return true;
        }

        /// <summary>마이그 전용 — 구 검사 노드 Setup json 의 LightPages 배열만 raw 직독(나머지 멤버 무시).</summary>
        [System.Runtime.Serialization.DataContract]
        internal sealed class NodeLightPagesDto
        {
            [System.Runtime.Serialization.DataMember] public List<LightPageRef> LightPages { get; set; }
        }

        public override void LoadSettings()       { base.LoadSettings();    ApplyCameraSettings(); }
        public override void LoadRecipe(string n) { base.LoadRecipe(n);     ApplyCameraSettings(); }
        // 저장 = Config/Recipe(SSOT)를 그대로 영속. CollectCameraSettings(라이브 카메라→Config) 호출은
        // UI 편집값을 카메라 현재값으로 덮어쓰던 버그(연결 시에만 저장 손실)라 제거 — 카메라 반영은 Apply 가 담당.
        public override bool SaveSettings()       { return base.SaveSettings(); }
        public override bool SaveRecipe(string n) { return base.SaveRecipe(n); }

        // ── 알고리즘 등록 (자식 노드) ──

        protected IPatternFinder AddFinder<TAlgoSetup, TAlgoConfig, TAlgoRecipe>(string id)
            where TAlgoSetup  : ISetupData,  new()
            where TAlgoConfig : IConfigData, new()
            where TAlgoRecipe : IRecipeData, new()
        {
            var finder = Backend.CreatePatternFinder(Name + "/" + id);
            Finders[id] = finder;
            RegisterAlgorithm(id, new FinderAlgorithm<TAlgoSetup, TAlgoConfig, TAlgoRecipe>(StorageKey + "." + id, finder));
            return finder;
        }

        protected IInspector AddInspector<TAlgoSetup, TAlgoConfig, TAlgoRecipe>(string id)
            where TAlgoSetup  : ISetupData,  new()
            where TAlgoConfig : IConfigData, new()
            where TAlgoRecipe : IRecipeData, new()
        {
            var inspector = Backend.CreateInspector(Name + "/" + id);
            Inspectors[id] = inspector;
            RegisterAlgorithm(id, new InspectorAlgorithm<TAlgoSetup, TAlgoConfig, TAlgoRecipe>(StorageKey + "." + id, inspector));
            return inspector;
        }

        private void RegisterAlgorithm(string id, IAlgorithmNode node)
        {
            _algorithms.Add(node);
            _algoById[id] = node;
            Components.Add((BaseEquipmentNode)node);
        }

        public IReadOnlyList<IAlgorithmNode> Algorithms => _algorithms;

        public IAlgorithmNode GetAlgorithm(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _algoById.TryGetValue(id, out var node) ? node : null;
        }

        /// <summary>비형식화 모듈 Setup 접근(IVisionModule) — 형식화 Setup(TSetup)을 ISetupData 로 노출.</summary>
        ISetupData IVisionModule.Setup => Setup;

        IReadOnlyDictionary<string, IPatternFinder> IVisionModule.Finders    => Finders;
        IReadOnlyDictionary<string, IInspector>     IVisionModule.Inspectors => Inspectors;

        public void SetCamera(ICamera newCamera)
        {
            if (newCamera == null) throw new ArgumentNullException(nameof(newCamera));
            if (Camera != null) { Camera.ExposureEnded -= OnCameraExposureEnded; Camera.FrameReceived -= OnCameraFrameReceived; }
            Camera = newCamera;
            Camera.ExposureEnded += OnCameraExposureEnded;
            Camera.FrameReceived += OnCameraFrameReceived;
        }

        /// <summary>카메라 HW 노출 종료 이벤트(SDK 콜백 스레드) — 모듈 Grab 진행 중에만 ExposureDone(EPD)으로 승격.
        /// 라이브 중 발화(Mil FRAME_START 폴백은 매 프레임 발화)는 EPD 로 승격하지 않는다.</summary>
        private void OnCameraExposureEnded()
        {
            if (!_grabInProgress) return;
            _exposureEndFired = true;
            // EPD 억제 플래그(ThreadStatic)는 SDK 콜백 스레드에서 보이지 않으므로 Grab 시작 시 캡처한 값으로 판단.
            if (!_relayExposureEnd) return;
            try { ExposureDone?.Invoke(Name); } catch { }
        }

        private void OnCameraFrameReceived(GrabResult r)
        {
            if (r != null && r.IsSuccess && r.Image != null) TapFrame(r.Image);
        }

        private void TapFrame(Bitmap src)
        {
            Bitmap clone;
            try { clone = (Bitmap)src.Clone(); } catch { return; }
            lock (_tapLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = clone;
                _frameSeq++;
            }
        }

        public Bitmap AcquireViewerFrame()
        {
            lock (_tapLock)
                if (_lastFrame != null) try { return (Bitmap)_lastFrame.Clone(); } catch { }
            return null;
        }

        public long ViewerFrameSeq { get { lock (_tapLock) return _frameSeq; } }

        private int _savedFrameSeq;   // '저장 이미지로 그랩' 프레임 번호

        private Bitmap _simOverride;

        /// <summary>
        /// 테스트용 in-memory 그랩 이미지. 설정 시 <see cref="Grab"/> 가 카메라/저장이미지 대신
        /// 이 이미지(복제본)를 반환한다. 오토포커스 테스트에서 화면에 표시(Load/Grab)한 이미지를
        /// 서버 grab 과 일치시키기 위해 사용. null 이면 해제.
        /// </summary>
        public void SetSimOverrideImage(Bitmap bmp)
        {
            Bitmap old = _simOverride;
            _simOverride = bmp != null ? (Bitmap)bmp.Clone() : null;
            try { old?.Dispose(); } catch { }
        }

        /// <summary>카메라 시뮬레이션 여부 — 카메라 미장착 또는 Sim 카메라.
        /// 저장이미지/오버라이드 그랩은 이 경우에만 허용(실카메라=항상 실제 촬상, 디스크 이미지 금지).</summary>
        public bool IsSimCameraMode
            => Camera == null || Camera.Info == null || Camera.Info.Transport == CameraTransport.Sim;

        public GrabResult Grab(int timeoutMs = 3000)
        {
            // 테스트 오버라이드 — 화면에 표시한 이미지를 grab 으로 반환(오토포커스 ROI 정렬용).
            Bitmap ov = _simOverride;
            if (ov != null)
            {
                Bitmap clone = (Bitmap)ov.Clone();
                TapFrame(clone);
                try { ExposureDone?.Invoke(Name); } catch { }
                return GrabResult.Success(clone, (int)_frameSeq, "sim-override");
            }

            // 모듈 레벨 시뮬 이미지 — 핸들러 GRAB 등 모듈 그랩에서 카메라 대신 저장 이미지를 사용(테스트).
            // 한 장을 그랩해 TapFrame → 핸들러 뷰어로 송출되고, 이후 finder MATCH 는 같은 프레임 ROI 검출.
            var simGrab = TryGrabModuleSavedImage();
            if (simGrab != null)
            {
                if (simGrab.IsSuccess && simGrab.Image != null) TapFrame(simGrab.Image);
                if (simGrab.IsSuccess) try { ExposureDone?.Invoke(Name); } catch { }
                else try { Alarmed?.Invoke(Name, simGrab.ErrorMessage); } catch { }
                return simGrab;
            }

            if (Camera == null) return GrabResult.Fail("camera not assigned", Name);
            if (!Camera.IsOpen) try { Camera.Open(); } catch { }
            if (DelayBeforeGrabMs > 0) System.Threading.Thread.Sleep(DelayBeforeGrabMs);

            // 실카메라 노출 종료(ExposureEnded) → ExposureDone(EPD) 승격 — 그랩 진행 중에만 허용.
            // 노출 이벤트는 전송 완료보다 먼저 도착하므로 핸들러가 EPD 수신 즉시 기구 동작을 앞당길 수 있다.
            _exposureEndFired = false;
            _relayExposureEnd = !VisionCommandCore.SuppressExposurePush;
            _grabInProgress = true;

            GrabResult g;
            try { g = Camera.Grab(timeoutMs); }
            finally { _grabInProgress = false; }

            // 폴백 — 노출 이벤트 미지원 카메라(Sim 등)는 그랩 완료 시점에 발화(핸들러 EPD 대기 멈춤 방지).
            // 명령 스레드 동기 발화라 Comm 링크의 ThreadStatic EPD 억제가 그대로 동작한다.
            if (!_exposureEndFired) try { ExposureDone?.Invoke(Name); } catch { }
            if (g != null && g.IsSuccess)
            {
                // 합성 OFF 빈 프레임(sim-blank)은 뷰어(_lastFrame)에 반영하지 않고 이전 화면 유지.
                if (g.Image != null && g.Source != "sim-blank")
                    TapFrame(g.Image);
            }
            else
            {
                try { Alarmed?.Invoke(Name, g != null ? g.ErrorMessage : "카메라 Grab 결과가 없습니다."); } catch { }
            }
            return g;
        }

        /// <summary>도구(Finder/Inspector) 단위 그랩 — 도구 전용 저장이미지가 있으면 우선, 없으면 <see cref="Grab(int)"/> 위임.
        /// 카메라 그랩 시 도구 전용 노출(Recipe.ExposureUs&gt;0)이 있으면 적용하고, 없으면 모듈 레시피 노출로
        /// 되돌려 도구 간 노출이 결정적으로 유지되게 한다.</summary>
        public GrabResult GrabForTool(string toolId, int timeoutMs = 3000)
        {
            // 새 촬상 시작 — 이전 검출 오버레이(마크/박스/검출 기하)를 먼저 지운다.
            // 새 프레임 위에 직전 결과가 겹쳐 '업데이트 안 되는 것처럼' 보이는 문제 방지.
            // 검사/매치가 끝나면 각 경로가 새 오버레이를 기록한다(결과 라인·판정 텍스트는 유지).
            try
            {
                QMC.Vision.Core.MatchOverlayStore.Clear(Name);
                QMC.Vision.Core.InspectionOverlayStore.Clear(Name);
                QMC.Vision.Core.ModuleResultStore.ClearMarks(Name);
            }
            catch (Exception ex) { LogGrab("오버레이 스토어 초기화 실패: " + ex.Message); }

            // 도구(Finder/Inspector) 전용 시뮬 저장이미지가 지정돼 있으면 우선 로드 — 없으면 Grab(모듈 저장이미지→실카메라)로 폴백.
            var saved = TryGrabSavedImageForTool(toolId);
            if (saved != null)
            {
                if (saved.IsSuccess && saved.Image != null) TapFrame(saved.Image);
                if (saved.IsSuccess) { LogGrab("저장이미지 그랩 성공 (" + saved.Width + "x" + saved.Height + ") toolId='" + toolId + "'"); try { ExposureDone?.Invoke(Name); } catch { } }
                else { LogGrab("저장이미지 로드 실패 → " + saved.ErrorMessage); try { Alarmed?.Invoke(Name, saved.ErrorMessage); } catch { } }
                return saved;
            }
            LogGrab("저장이미지 미사용 → 카메라 그랩 (toolId='" + (toolId ?? "(null)") + "')");
            PrepareToolAcquisition(toolId);
            return Grab(timeoutMs);
        }

        /// <summary>도구 촬상 준비 — 노출(도구 전용 or 모듈 기본) + 조명(도구 Recipe.LightSettings) 적용.
        /// 조명은 컨트롤러 배치 캐시가 동일 값이면 통신/안정화 대기를 생략하므로 그랩마다 호출해도 비용이 없다.
        /// GrabForTool(MATCH/INSPECT/툴바 그랩)과 라이브 시작(VisionModuleSource)이 호출한다.</summary>
        public void PrepareToolAcquisition(string toolId)
        {
            ApplyToolExposure(toolId);
            ApplyToolLights(toolId);
        }

        /// <summary>도구 조명 적용 — 노드 Recipe.LightSettings 를 컨트롤러별 페이지 배치로 송신.
        /// 미지정(빈 목록) 도구는 조명을 건드리지 않는다. 송신/대기는 컨트롤러 캐시가 관리(동일 값 = 생략).</summary>
        private void ApplyToolLights(string toolId)
        {
            try
            {
                var recipe = GetAlgorithm(toolId)?.Recipe as AlgoRecipeBase;
                var settings = recipe?.LightSettings;
                if (settings == null || settings.Count == 0) return;   // 조명 미지정 도구 — 현재 상태 유지

                var tasks = new List<Task<bool>>();
                foreach (var grp in settings.Where(s => !string.IsNullOrEmpty(s.ControllerPort)).GroupBy(s => s.ControllerPort))
                {
                    var ctrl = QMC.Vision.Comm.LightHub.Get(grp.Key);
                    if (ctrl == null)
                    {
                        LogGrab("조명 포트 '" + grp.Key + "' LightHub 미등록 → 건너뜀 (toolId='" + toolId + "')");
                        continue;
                    }
                    var list = grp.ToList();
                    string port = grp.Key;
                    tasks.Add(Task.Run(async () =>
                    {
                        bool allOk = true;
                        foreach (var pgrp in list.GroupBy(s => s.Page).OrderBy(g => g.Key))
                        {
                            await ctrl.SwitchPageAsync(pgrp.Key).ConfigureAwait(false);
                            int[] values = new int[ctrl.ChannelCount];   // 0 = OFF(미사용)
                            foreach (var s in pgrp)
                                if (s.Channel >= 1 && s.Channel <= ctrl.ChannelCount)
                                    values[s.Channel - 1] = s.On ? s.Level : 0;
                            // 컨트롤러가 캐시 히트면 송신/대기 생략, 미스면 송신 + SettleDelayMs 대기.
                            bool ok2 = await ctrl.SetChannelBatchAsync(pgrp.Key, values).ConfigureAwait(false);
                            LogGrab("도구 조명 " + port + " P" + pgrp.Key.ToString("00") +
                                    " [" + string.Join(",", values) + "] 결과=" + ok2 + " (toolId='" + toolId + "')");
                            allOk &= ok2;
                        }
                        return allOk;
                    }));
                }
                if (tasks.Count == 0) return;
                // 그랩 전에 조명 안정화까지 완료되어야 하므로 동기 대기(캐시 히트 시 즉시 반환).
                bool ok = Task.WhenAll(tasks).GetAwaiter().GetResult().All(r => r);
                if (!ok) LogGrab("조명 적용 일부 실패 (toolId='" + toolId + "') — 시리얼 연결/NAK 확인");
            }
            catch (Exception ex) { LogGrab("조명 적용 실패: " + ex.Message + " (toolId='" + (toolId ?? "") + "')"); }
        }

        /// <summary>도구 전용 노출 적용 — 도구 Recipe.ExposureUs&gt;0 이면 그 값, 아니면 모듈 레시피 노출.
        /// 현재 카메라 캐시값과 다를 때만 feature 를 쓴다(연속 그랩 시 카메라 왕복 최소화).</summary>
        private void ApplyToolExposure(string toolId)
        {
            try
            {
                if (Camera == null) return;
                double us = 0;
                var r = GetAlgorithm(toolId)?.Recipe as AlgoRecipeBase;
                if (r != null && r.ExposureUs > 0) us = r.ExposureUs;
                if (us <= 0) us = CameraNode?.Recipe?.Exposure ?? 0;   // 도구 미지정 → 모듈 기본으로 복원
                if (us <= 0) return;
                if (Math.Abs(Camera.ExposureUs - us) > 0.01)
                {
                    Camera.ExposureUs = us;
                    LogGrab("도구 노출 적용 " + us.ToString("F0") + "µs (toolId='" + (toolId ?? "") + "')");
                }
            }
            catch (Exception ex) { LogGrab("도구 노출 적용 실패: " + ex.Message); }
        }

        /// <summary>모듈(카메라) 레벨 SimUseSavedImage=true 면 저장 이미지를 로드. 아니면 null(카메라 그랩으로 위임).</summary>
        private GrabResult TryGrabModuleSavedImage()
        {
            var c = CameraNode?.Config;
            if (c == null || !c.SimUseSavedImage)
                return null;
            LogGrab("모듈 SimUseSavedImage=true, 경로='" + (c.SimSavedImagePath ?? "") + "' → 저장이미지 그랩 시도");
            return LoadImageAsGrab(c.SimSavedImagePath);
        }

        /// <summary>도구 노드 Setup.SimUseSavedImage=true 면 그 도구 전용 저장 이미지를 로드. 아니면 null(카메라로 위임).
        /// 폴백 사유는 EventLogger("VISION"/"GrabForTool")로 남겨 진단 가능.</summary>
        private GrabResult TryGrabSavedImageForTool(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
            {
                LogGrab("toolId 비어있음 → 폴백");
                return null;
            }
            var node = GetAlgorithm(toolId);
            if (node == null)
            {
                LogGrab("GetAlgorithm('" + toolId + "') = null (등록 키 불일치) → 폴백");
                return null;
            }
            var s = node.Setup as AlgoSetupBase;
            if (s == null)
            {
                LogGrab("toolId='" + toolId + "' Setup=" + (node.Setup?.GetType().Name ?? "null") + " (AlgoSetupBase 아님) → 폴백");
                return null;
            }
            if (!s.SimUseSavedImage)
            {
                LogGrab("toolId='" + toolId + "' SimUseSavedImage=false → 폴백");
                return null;
            }
            // 측면 90° 채널이면 Ch2 전용 이미지 사용(있을 때). 채널 홀수=90° — 신형 1(90°), 구형 1/3(Front·Back ch2) 모두 홀수라 호환.
            string path = s.SimSavedImagePath;
            int ch = QMC.Vision.Core.VisionCommandCore.CurrentInspectChannel(Name);
            if ((ch % 2) == 1 && !string.IsNullOrWhiteSpace(s.SimSavedImagePathCh2))
                path = s.SimSavedImagePathCh2;
            LogGrab("toolId='" + toolId + "' SimUseSavedImage=true, ch=" + ch + ", 경로='" + (path ?? "") + "' → 저장이미지 로드 시도");
            return LoadImageAsGrab(path);
        }

        /// <summary>그랩(도구 저장이미지/폴백) 진단 로그 — Vision DataLog(EventLogger)로 남긴다.</summary>
        private void LogGrab(string message)
        {
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "GrabForTool", Name + ": " + message); }
            catch { }
        }

        // ── 저장이미지 디코드 캐시(Sim 전용 최적화) ──
        // 고해상도 저장이미지를 그랩마다 디스크에서 재읽기+재디코드하면 회당 ~1초가 걸려
        // Bottom 4픽커 배치의 그랩 구간이 수 초를 차지한다. 같은 파일(경로+수정시각+크기)이면
        // 디코드된 마스터를 재사용하고 복제본만 만들어 반환한다(반환 이미지 소유권은 호출자 — 기존과 동일).
        // 실기 카메라 그랩 경로는 이 캐시를 타지 않는다.
        private static readonly object _savedImgCacheLock = new object();
        private static readonly System.Collections.Generic.Dictionary<string, System.Tuple<DateTime, long, System.Drawing.Bitmap>> _savedImgCache
            = new System.Collections.Generic.Dictionary<string, System.Tuple<DateTime, long, System.Drawing.Bitmap>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>지정 경로의 이미지를 GrabResult 로 로드(파일 잠금 방지 위해 복제본 생성). 경로/파일 문제는 실패 GrabResult.
        /// 동일 파일 반복 로드는 디코드 캐시를 사용(수정 시 자동 무효화).</summary>
        private GrabResult LoadImageAsGrab(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return GrabResult.Fail("저장 이미지 경로 미지정", Name);
            var fi = new System.IO.FileInfo(path);
            if (!fi.Exists)
                return GrabResult.Fail("저장 이미지 없음: " + path, Name);
            try
            {
                if (DelayBeforeGrabMs > 0) System.Threading.Thread.Sleep(DelayBeforeGrabMs);
                System.Drawing.Bitmap clone;
                lock (_savedImgCacheLock)
                {
                    System.Tuple<DateTime, long, System.Drawing.Bitmap> hit;
                    if (!_savedImgCache.TryGetValue(path, out hit)
                        || hit.Item1 != fi.LastWriteTimeUtc || hit.Item2 != fi.Length)
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(path);
                        System.Drawing.Bitmap master;
                        using (var ms = new System.IO.MemoryStream(bytes))
                        using (var tmp = System.Drawing.Image.FromStream(ms))
                        {
                            // 24bppRgb 마스터로 1회 변환 — 이후 그랩은 동일 포맷 Clone(=memcpy, 고속).
                            // 기존 new Bitmap(tmp)=32bpp 재변환이 12000² 기준 그랩마다 ~1초·576MB 할당을 유발했고,
                            // 검사(ToGray)도 LockBits(24bpp) 요청이라 24bpp 마스터면 잠금 시 포맷 변환도 사라진다.
                            var b24 = new System.Drawing.Bitmap(tmp.Width, tmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            using (var gg = System.Drawing.Graphics.FromImage(b24))
                                gg.DrawImage(tmp, 0, 0, tmp.Width, tmp.Height);
                            master = b24;
                        }
                        if (hit != null) { try { hit.Item3.Dispose(); } catch { } }
                        hit = System.Tuple.Create(fi.LastWriteTimeUtc, fi.Length, master);
                        _savedImgCache[path] = hit;
                    }
                    // GDI+ Bitmap 은 스레드 세이프하지 않으므로 복제도 락 안에서.
                    // 동일 포맷 Clone = 픽셀 memcpy — new Bitmap(...) 의 포맷 변환 경로보다 수 배 빠름.
                    clone = hit.Item3.Clone(new System.Drawing.Rectangle(0, 0, hit.Item3.Width, hit.Item3.Height), hit.Item3.PixelFormat);
                }
                int seq = System.Threading.Interlocked.Increment(ref _savedFrameSeq);
                return GrabResult.Success(clone, seq, "saved:" + System.IO.Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                return GrabResult.Fail("저장 이미지 로드 실패: " + ex.Message, Name);
            }
        }

        public void RaiseAlarm(string reason)
        {
            try { Alarmed?.Invoke(Name, reason); } catch { }
        }

        /// <summary>VisionScale 캘리브레이션 — 간이 구현.</summary>
        public bool Calibrate(double chipWidthMm, double chipHeightMm,
                              out double scaleX, out double scaleY, out string err)
        {
            scaleX = 0; scaleY = 0; err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }

                double pxW = g.Width  * 0.7;
                double pxH = g.Height * 0.7;

                if (Finders.TryGetValue("ScaleFinder", out var sf))
                {
                    var r = sf.Match(g.Image);
                    if (r.Success && r.Best != null)
                    {
                        pxW = r.Best.CenterX;
                        pxH = r.Best.CenterY;
                    }
                }

                if (pxW <= 0 || pxH <= 0) { err = "invalid pixel size"; return false; }
                scaleX = chipWidthMm  / pxW;
                scaleY = chipHeightMm / pxH;
                return true;
            }
        }

        /// <summary>회전 중심 측정 — 다이 4 corner 점 반환. 간이 구현.</summary>
        public bool MeasureRotationalCenter(out List<PointF> corners, out string err)
        {
            corners = new List<PointF>();
            err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }
                int w = g.Width, h = g.Height;
                corners.Add(new PointF(w * 0.1f, h * 0.1f));
                corners.Add(new PointF(w * 0.9f, h * 0.1f));
                corners.Add(new PointF(w * 0.9f, h * 0.9f));
                corners.Add(new PointF(w * 0.1f, h * 0.9f));
                return true;
            }
        }

        /// <summary>왜곡 보정 학습. 간이 구현.</summary>
        public bool LearnDistortion(out string err)
        {
            err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }
                if (Finders.TryGetValue("DistortionCompensation", out var df))
                {
                    df.Train(g.Image);
                    return true;
                }
                err = "no DistortionCompensation finder";
                return false;
            }
        }

        /// <summary>4 ROI 별 포커스 값 측정. 간이 구현.</summary>
        public bool MeasureFocus(out List<KeyValuePair<string, double>> roiFocus, out string err)
        {
            roiFocus = new List<KeyValuePair<string, double>>();
            err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }
                int w = g.Width, h = g.Height;
                roiFocus.Add(new KeyValuePair<string, double>("Left top",     ApproxFocus(g.Image, 0,   0,   w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Right top",    ApproxFocus(g.Image, w/2, 0,   w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Left bottom",  ApproxFocus(g.Image, 0,   h/2, w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Right bottom", ApproxFocus(g.Image, w/2, h/2, w/2, h/2)));
                return true;
            }
        }

        private static double ApproxFocus(Bitmap bmp, int x, int y, int w, int h)
        {
            try
            {
                int step = Math.Max(1, Math.Min(w, h) / 20);
                double sum = 0; int n = 0;
                for (int yy = y + step; yy < y + h - step; yy += step)
                    for (int xx = x + step; xx < x + w - step; xx += step)
                    {
                        var c1 = bmp.GetPixel(xx, yy);
                        var c2 = bmp.GetPixel(xx + step, yy);
                        var c3 = bmp.GetPixel(xx, yy + step);
                        int gx = Math.Abs(c2.R - c1.R) + Math.Abs(c2.G - c1.G) + Math.Abs(c2.B - c1.B);
                        int gy = Math.Abs(c3.R - c1.R) + Math.Abs(c3.G - c1.G) + Math.Abs(c3.B - c1.B);
                        sum += gx + gy;
                        n++;
                    }
                return n > 0 ? sum / n : 0;
            }
            catch { return 0; }
        }

        public void Dispose()
        {
            try { if (Camera != null) { Camera.ExposureEnded -= OnCameraExposureEnded; Camera.FrameReceived -= OnCameraFrameReceived; } } catch { }
            try { Camera?.Dispose(); } catch { }
            lock (_tapLock) { _lastFrame?.Dispose(); _lastFrame = null; }
        }
    }
}
