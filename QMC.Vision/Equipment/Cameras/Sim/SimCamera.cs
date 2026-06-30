using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using QMC.Vision.Core;

namespace QMC.Vision.Cameras.Sim
{
    /// <summary>합성 이미지 생성 카메라 — 실 하드웨어 없이 개발/테스트용.</summary>
    public class SimCamera : CameraBase
    {
        private Timer _liveTimer;
        private int _frameCounter;
        private readonly Random _rnd = new Random();

        // 합성 프레임 캐시 — 매 grab 마다 144MP 비트맵을 새로 그리면 느리므로(대형 해상도에서 수백 ms),
        // 한 번 그려두고 grab 시 사본만 반환한다(생성+드로잉 → 클론 1회로 단축).
        private Bitmap _cachedFrame;
        private Size _cachedSize;
        private readonly object _frameLock = new object();

        public SimCamera(string id) : base(new CameraInfo
        {
            Id           = id,
            Model        = "Synthetic",
            Vendor       = "QMC",
            SerialNumber = id,
            Transport    = CameraTransport.Sim,
            MaxResolution= new Size(640, 480)
        })
        { Resolution = new Size(640, 480); }

        public override void Open()
        {
            IsOpen = true;
            RaiseConnectionChanged(CameraConnectionEvent.Opened);
        }

        public override void Close()
        {
            StopLive();
            lock (_frameLock) { try { _cachedFrame?.Dispose(); } catch { } _cachedFrame = null; }
            IsOpen = false;
            RaiseConnectionChanged(CameraConnectionEvent.Closed);
        }

        public override GrabResult Grab(int timeoutMs = 3000)
        {
            if (!IsOpen) return GrabResult.Fail("camera not open", Info.Id);
            return new GrabResult(GetFrameClone(), _frameCounter++, Info.Id);
        }

        /// <summary>캐시된 합성 프레임의 사본을 반환(소비자가 Dispose). 해상도 변경 시 1회 재생성.</summary>
        private Bitmap GetFrameClone()
        {
            lock (_frameLock)
            {
                if (_cachedFrame == null || _cachedSize != Resolution)
                {
                    try { _cachedFrame?.Dispose(); } catch { }
                    _cachedFrame = BuildFrame(0);
                    _cachedSize = Resolution;
                }
                return (Bitmap)_cachedFrame.Clone();
            }
        }

        public override void StartLive()
        {
            if (!IsOpen) return;
            StopLive();
            IsGrabbing = true;
            int periodMs = Math.Max(20, (int)(1000.0 / Math.Max(1, AcquisitionFrameRate)));
            _liveTimer = new Timer(_ =>
            {
                if (!IsGrabbing) return;
                try { RaiseFrame(Grab()); } catch { }
            }, null, 0, periodMs);
        }

        public override void StopLive()
        {
            IsGrabbing = false;
            _liveTimer?.Dispose();
            _liveTimer = null;
        }

        public override void TriggerSoftware()
        {
            if (!IsOpen) return;
            var g = Grab();
            RaiseFrame(g);
        }

        private Bitmap BuildFrame(int frame)
        {
            var bmp = new Bitmap(Resolution.Width, Resolution.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(30, 30, 30));
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 노이즈
                for (int i = 0; i < 300; i++)
                    bmp.SetPixel(_rnd.Next(bmp.Width), _rnd.Next(bmp.Height),
                        Color.FromArgb(_rnd.Next(60, 160), _rnd.Next(60, 160), _rnd.Next(60, 160)));

                // 다이 그리드
                using (var p = new Pen(Color.FromArgb(140, 140, 180), 1f))
                {
                    int pitch = 50, off = frame % 50;
                    for (int y = off; y < bmp.Height; y += pitch) g.DrawLine(p, 0, y, bmp.Width, y);
                    for (int x = off; x < bmp.Width;  x += pitch) g.DrawLine(p, x, 0, x, bmp.Height);
                }

                // 가이드 사각
                using (var pR = new Pen(Color.OrangeRed, 1f)) g.DrawRectangle(pR, 120,  60, 400, 360);
                using (var pY = new Pen(Color.Yellow,     1f)) g.DrawRectangle(pY, 270, 190, 100, 100);

                // Overlay
                using (var sf = new Font("Consolas", 10F))
                using (var br = new SolidBrush(Color.LightGreen))
                {
                    g.DrawString(Info.Id,              sf, br, 6,  6);
                    g.DrawString("Frame " + frame,     sf, br, 6, 22);
                    g.DrawString($"Exp={ExposureUs:F0}us Gain={Gain:F1}", sf, br, 6, 38);
                }
            }
            return bmp;
        }

        public static CameraInfo[] Enumerate() => new[]
        {
            new CameraInfo { Id = "Sim/0", Model = "Synthetic", Vendor = "QMC", Transport = CameraTransport.Sim, MaxResolution = new Size(640, 480) },
            new CameraInfo { Id = "Sim/1", Model = "Synthetic", Vendor = "QMC", Transport = CameraTransport.Sim, MaxResolution = new Size(640, 480) },
        };
    }
}
