using System;
using System.Collections.Generic;
using System.Drawing;
using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// 비전 모듈 베이스 — 카메라 + 이미지 처리 백엔드 + Finder/Inspector 를 한 단위로 묶음.
    /// <para>
    /// 설계 원칙: 비즈니스 로직은 <see cref="Camera"/> 를 통해서만 이미지를 얻는다.
    /// Backend 는 Finder/Inspector 생성에만 사용.
    /// </para>
    /// </summary>
    public abstract class VisionModule : IDisposable
    {
        public string          Name    { get; }
        public ICamera         Camera  { get; private set; }
        public IVisionBackend  Backend { get; }

        /// <summary>Stage 70 — VisionAlgorithm 상수 키 (조명/카메라 결선 조회용). 5 모듈이 override.</summary>
        public virtual string  AlgorithmKey => "";

        /// <summary>설정 페이지에서 알고리즘에 매핑된 카메라가 변경됐을 때 런타임 교체.
        /// 기존 카메라 Dispose 는 호출자가 책임진다 (이 메서드는 참조만 바꿈).</summary>
        public void SetCamera(ICamera newCamera)
        {
            if (newCamera == null) throw new ArgumentNullException(nameof(newCamera));
            if (Camera != null) { Camera.ExposureEnded -= OnCameraExposureEnded; Camera.FrameReceived -= OnCameraFrameReceived; }
            Camera = newCamera;
            Camera.ExposureEnded += OnCameraExposureEnded;
            Camera.FrameReceived += OnCameraFrameReceived;
        }

        public Dictionary<string, IPatternFinder> Finders    { get; } = new Dictionary<string, IPatternFinder>();
        public Dictionary<string, IInspector>     Inspectors { get; } = new Dictionary<string, IInspector>();

        /// <summary>그랩 직전 지연 (ms). VisionTcpServer 가 Config 에서 설정.</summary>
        public int DelayBeforeGrabMs { get; set; } = 0;

        /// <summary>그랩 시점 (EPD 비동기 이벤트 발화). string=ModuleName.</summary>
        public event Action<string> ExposureDone;
        /// <summary>실패 시 (ARM 비동기 이벤트 발화). args=(ModuleName, reason).</summary>
        public event Action<string, string> Alarmed;

        /// <summary>현재 Grab 사이클에서 카메라 HW ExposureEnd 로 이미 EPD 를 쐈는지 여부.</summary>
        private volatile bool _exposureEndFired;

        // ── 원격 뷰어용 frame tap (실제 그랩/라이브 프레임만 수동적으로 보관) ──
        private readonly object _tapLock = new object();
        private Bitmap _lastFrame;                       // clone 소유
        private long   _frameSeq;                        // TapFrame 마다 증가 (뷰어 새 프레임 판단용)

        protected VisionModule(string name, ICamera camera, IVisionBackend backend)
        {
            Name    = name;
            Camera  = camera  ?? throw new ArgumentNullException(nameof(camera));
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            Camera.ExposureEnded += OnCameraExposureEnded;
            Camera.FrameReceived += OnCameraFrameReceived;
        }

        /// <summary>카메라 HW 노출 종료 이벤트 → 즉시 EPD 발화 (전송 완료를 기다리지 않음).</summary>
        private void OnCameraExposureEnded()
        {
            _exposureEndFired = true;
            try { ExposureDone?.Invoke(Name); } catch { }
        }

        /// <summary>라이브 프레임 수신 → 뷰어용으로 tap.</summary>
        private void OnCameraFrameReceived(GrabResult r)
        {
            if (r != null && r.IsSuccess && r.Image != null) TapFrame(r.Image);
        }

        /// <summary>최신 프레임을 clone 하여 보관 (이전 프레임 Dispose).</summary>
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

        /// <summary>원격 뷰어용 프레임 1장(clone, 호출자가 Dispose). 실제 그랩/라이브로 tap 된 최신 프레임만 반환.
        /// 자체 그랩은 하지 않는다 — 테스트 그랩/실제 운행/라이브가 없으면 null.</summary>
        public Bitmap AcquireViewerFrame()
        {
            lock (_tapLock)
                if (_lastFrame != null) try { return (Bitmap)_lastFrame.Clone(); } catch { }
            return null;
        }

        /// <summary>tap 시퀀스 — TapFrame(실제 그랩/라이브) 마다 증가. 뷰어 서버가 "새 프레임" 판단에 사용.</summary>
        public long ViewerFrameSeq { get { lock (_tapLock) return _frameSeq; } }

        protected IPatternFinder AddFinder(string id)
        {
            var f = Backend.CreatePatternFinder(Name + "/" + id);
            Finders[id] = f;
            return f;
        }

        protected IInspector AddInspector(string id)
        {
            var i = Backend.CreateInspector(Name + "/" + id);
            Inspectors[id] = i;
            return i;
        }

        /// <summary>카메라 1장 촬영 — 비즈니스 로직의 유일한 이미지 획득 경로.</summary>
        public GrabResult Grab(int timeoutMs = 3000)
        {
            if (!Camera.IsOpen) try { Camera.Open(); } catch { }
            if (DelayBeforeGrabMs > 0) System.Threading.Thread.Sleep(DelayBeforeGrabMs);
            _exposureEndFired = false;
            var g = Camera.Grab(timeoutMs);
            if (g.IsSuccess)
            {
                if (g.Image != null) TapFrame(g.Image);   // 원격 뷰어용 tap
                // HW ExposureEnd 가 이미 EPD 를 쐈으면(노출 종료 시점) 중복 발화하지 않는다.
                // 미지원 카메라(Sim 등)는 여기서 fallback 으로 발화 → 전송 완료 시점 EPD.
                if (!_exposureEndFired) try { ExposureDone?.Invoke(Name); } catch { }
            }
            else try { Alarmed?.Invoke(Name, g.ErrorMessage); } catch { }
            return g;
        }

        public void RaiseAlarm(string reason)
        {
            try { Alarmed?.Invoke(Name, reason); } catch { }
        }

        // ─────────────────────────────────────────
        //  310 이식 — 추가 비전 명령
        // ─────────────────────────────────────────

        /// <summary>
        /// VisionScale 캘리브레이션. 알려진 chip 크기(mm) 와 그랩 이미지의 다이 픽셀 크기를 비교해
        /// scaleX/Y (mm/pixel) 산출. 간이 구현 — 백엔드의 ScaleFinder 가 있으면 그 결과를, 없으면
        /// 이미지 전체의 70% 를 칩으로 가정.
        /// </summary>
        public bool Calibrate(double chipWidthMm, double chipHeightMm,
                              out double scaleX, out double scaleY, out string err)
        {
            scaleX = 0; scaleY = 0; err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }

                double pxW = g.Width  * 0.7;
                double pxH = g.Height * 0.7;

                // 실 백엔드 — Scale finder 가 있을 경우 사용
                if (Finders.TryGetValue("ScaleFinder", out var sf))
                {
                    var r = sf.Match(g.Image);
                    if (r.Success && r.Best != null)
                    {
                        // Scale finder 가 chip 외곽 사각형 크기를 score 가 아닌
                        // CenterX/Y 로 반환한다고 가정
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
                // 간이: 이미지 4 corner 의 80% 위치
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

        /// <summary>4 ROI 별 포커스 값 측정. 간이 구현 — Sobel-like 분산 근사.</summary>
        public bool MeasureFocus(out List<KeyValuePair<string, double>> roiFocus, out string err)
        {
            roiFocus = new List<KeyValuePair<string, double>>();
            err = null;
            using (var g = Grab())
            {
                if (!g.IsSuccess) { err = g.ErrorMessage; return false; }
                int w = g.Width, h = g.Height;
                // 4 ROI: 좌상/우상/좌하/우하
                roiFocus.Add(new KeyValuePair<string, double>("Left top",     ApproxFocus(g.Image, 0,        0,        w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Right top",    ApproxFocus(g.Image, w/2,      0,        w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Left bottom",  ApproxFocus(g.Image, 0,        h/2,      w/2, h/2)));
                roiFocus.Add(new KeyValuePair<string, double>("Right bottom", ApproxFocus(g.Image, w/2,      h/2,      w/2, h/2)));
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
