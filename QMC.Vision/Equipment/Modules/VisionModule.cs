using QMC.Common;
using QMC.Common.Data.Store;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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

        private void OnCameraExposureEnded()
        {
            _exposureEndFired = true;
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

        public GrabResult Grab(int timeoutMs = 3000)
        {
            if (!Camera.IsOpen) try { Camera.Open(); } catch { }
            if (DelayBeforeGrabMs > 0) System.Threading.Thread.Sleep(DelayBeforeGrabMs);
            _exposureEndFired = false;
            var g = Camera.Grab(timeoutMs);
            if (g.IsSuccess)
            {
                if (g.Image != null) TapFrame(g.Image);
                if (!_exposureEndFired) try { ExposureDone?.Invoke(Name); } catch { }
            }
            else try { Alarmed?.Invoke(Name, g.ErrorMessage); } catch { }
            return g;
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
            try { if (Camera != null) Camera.FrameReceived -= OnCameraFrameReceived; } catch { }
            try { Camera?.Dispose(); } catch { }
            lock (_tapLock) { _lastFrame?.Dispose(); _lastFrame = null; }
        }
    }
}
