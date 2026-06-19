using System;
using System.Drawing;
using QMC.Common.Ui.Controls;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// <see cref="IVisionModule"/> 를 범용 <see cref="ICameraViewSource"/> 로 감싸는 어댑터.
    /// CameraView 내장 툴바의 Grab/Live 가 비전 모듈에 의존하지 않도록 한다.
    /// </summary>
    internal sealed class VisionModuleSource : ICameraViewSource
    {
        private readonly IVisionModule _m;
        private Action<Bitmap> _onFrame;
        private Action<GrabResult> _handler;

        public VisionModuleSource(IVisionModule m) { _m = m; }

        public Bitmap GrabFrame()
        {
            try
            {
                using (var g = _m.Grab())
                    return (g != null && g.IsSuccess && g.Image != null) ? (Bitmap)g.Image.Clone() : null;
            }
            catch { return null; }
        }

        public bool SupportsLive => _m?.Camera != null;

        public void StartLive(Action<Bitmap> onFrame)
        {
            var cam = _m?.Camera;
            if (cam == null) return;
            _onFrame = onFrame;
            _handler = r =>
            {
                if (r == null || !r.IsSuccess || r.Image == null) return;
                Bitmap b;
                try { b = (Bitmap)r.Image.Clone(); } catch { return; }
                _onFrame?.Invoke(b);
            };
            cam.FrameReceived += _handler;
            try { cam.TriggerMode = CameraTriggerMode.Continuous; } catch { }
            cam.StartLive();
        }

        public void StopLive()
        {
            var cam = _m?.Camera;
            if (cam == null) return;
            try { cam.StopLive(); } catch { }
            try { if (_handler != null) cam.FrameReceived -= _handler; } catch { }
            _handler = null;
        }
    }
}
