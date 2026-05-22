using System;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>
    /// <see cref="ICamera"/> 공용 구현 베이스.
    /// 상태 플래그 / 이벤트 발행 / 파라미터 캐시 등 공통 로직 제공.
    /// 실 벤더 SDK 호출은 파생 클래스에서 override.
    /// </summary>
    public abstract class CameraBase : ICamera
    {
        public CameraInfo Info { get; protected set; }

        public bool IsOpen     { get; protected set; }
        public bool IsGrabbing { get; protected set; }

        public event Action<GrabResult>           FrameReceived;
        public event Action<CameraConnectionEvent> ConnectionChanged;

        // ─── 파라미터 기본 캐시 ───────────────────
        private double _exposureUs = 10_000;
        private double _gain       = 0;
        private double _fps        = 10;
        private CameraTriggerMode _trigger = CameraTriggerMode.Continuous;
        private CameraPixelFormat _pixelFormat = CameraPixelFormat.Mono8;
        private Rectangle _roi = Rectangle.Empty;
        private Size _resolution = new Size(640, 480);

        public virtual double ExposureUs
        {
            get => _exposureUs;
            set { _exposureUs = value; OnExposureChanged(value); }
        }
        public virtual double Gain
        {
            get => _gain;
            set { _gain = value; OnGainChanged(value); }
        }
        public virtual double AcquisitionFrameRate
        {
            get => _fps;
            set { _fps = value; OnFrameRateChanged(value); }
        }
        public virtual CameraTriggerMode TriggerMode
        {
            get => _trigger;
            set { _trigger = value; OnTriggerModeChanged(value); }
        }
        public virtual CameraPixelFormat PixelFormat
        {
            get => _pixelFormat;
            set { _pixelFormat = value; OnPixelFormatChanged(value); }
        }
        public virtual Rectangle Roi
        {
            get => _roi;
            set { _roi = value; OnRoiChanged(value); }
        }
        public virtual Size Resolution
        {
            get => _resolution;
            protected set => _resolution = value;
        }

        protected CameraBase(CameraInfo info) { Info = info ?? new CameraInfo(); }

        // ─── Hooks (파생 클래스에서 override — SDK 호출) ───
        protected virtual void OnExposureChanged     (double us)                  { }
        protected virtual void OnGainChanged         (double gainDb)              { }
        protected virtual void OnFrameRateChanged    (double fps)                 { }
        protected virtual void OnTriggerModeChanged  (CameraTriggerMode mode)     { }
        protected virtual void OnPixelFormatChanged  (CameraPixelFormat fmt)      { }
        protected virtual void OnRoiChanged          (Rectangle roi)              { }

        // ─── 라이프사이클 ─────────────────────────
        public abstract void Open();
        public abstract void Close();

        public abstract GrabResult Grab(int timeoutMs = 3000);
        public abstract void StartLive();
        public abstract void StopLive();
        public abstract void TriggerSoftware();

        public virtual string GetRawParameter(string key) => null;
        public virtual void   SetRawParameter(string key, string value) { }

        // ─── 이벤트 발행 헬퍼 ─────────────────────
        protected void RaiseFrame(GrabResult r)
        {
            var h = FrameReceived;
            if (h != null) try { h(r); } catch { }
        }
        protected void RaiseConnectionChanged(CameraConnectionEvent ev)
        {
            var h = ConnectionChanged;
            if (h != null) try { h(ev); } catch { }
        }

        public virtual void Dispose() { try { Close(); } catch { } }
    }
}
