using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using QMC.Common.Ui.Controls;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Vision 의 그랩 스트림 송출(GrabStreamServer, 와이어: [4바이트 int32 LE 길이][JPEG]) 에 접속해
    /// 프레임을 받는 CameraView 영상 소스. 핸들러에서 <c>cam.AttachSource(new VisionViewerSource(host, port))</c>
    /// 로 연결하면 내장 툴바 Grab/Live 가 Vision 과 동일하게 동작한다.
    /// 뷰어 포트(모듈별): Wafer 5200 / BottomInspection 5201 / Bin 5203 / TopSide 5205 / BottomSide 5206.
    /// </summary>
    public sealed class VisionViewerSource : ICameraViewSource, IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _connectTimeoutMs;

        private Thread _thread;
        private volatile bool _running;
        private TcpClient _tcp;
        private Action<Bitmap> _onFrame;

        private readonly object _lastLock = new object();
        private Bitmap _last;   // 최근 수신 프레임(GrabFrame 단발용)

        public VisionViewerSource(string host, int port, int connectTimeoutMs = 2000)
        {
            _host = host; _port = port; _connectTimeoutMs = connectTimeoutMs;
        }

        public bool SupportsLive => true;

        public void StartLive(Action<Bitmap> onFrame)
        {
            if (_running) return;
            _onFrame = onFrame;
            _running = true;
            _thread = new Thread(RecvLoop) { IsBackground = true, Name = "VisionViewer-" + _port };
            _thread.Start();
        }

        public void StopLive()
        {
            _running = false;
            try { _tcp?.Close(); } catch { }
            try { _thread?.Join(800); } catch { }
            _thread = null;
        }

        public Bitmap GrabFrame()
        {
            // 라이브 중이면 최근 프레임 복제, 아니면 1회 접속해 한 프레임만 받아 반환.
            lock (_lastLock) { if (_last != null) return (Bitmap)_last.Clone(); }
            return ReadSingleFrame();
        }

        private void RecvLoop()
        {
            while (_running)
            {
                try
                {
                    using (var tcp = Connect())
                    {
                        if (tcp == null) { if (_running) Thread.Sleep(500); continue; }
                        _tcp = tcp;
                        var ns = tcp.GetStream();
                        while (_running)
                        {
                            var bmp = ReadFrame(ns);
                            if (bmp == null) break;          // 끊김/오류 → 재접속
                            SetLast(bmp);                    // _last 는 별도 복제본
                            var cb = _onFrame;
                            if (cb != null) cb(bmp);         // 소유권 이전(소비자가 Dispose)
                            else bmp.Dispose();
                        }
                    }
                }
                catch { }
                finally { _tcp = null; }
                if (_running) Thread.Sleep(300);             // 끊기면 잠시 후 재접속
            }
        }

        private Bitmap ReadSingleFrame()
        {
            try
            {
                using (var tcp = Connect())
                {
                    if (tcp == null) return null;
                    return ReadFrame(tcp.GetStream());
                }
            }
            catch { return null; }
        }

        private TcpClient Connect()
        {
            try
            {
                var tcp = new TcpClient();
                var ar = tcp.BeginConnect(_host, _port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(_connectTimeoutMs)) { try { tcp.Close(); } catch { } return null; }
                tcp.EndConnect(ar);
                tcp.NoDelay = true;
                tcp.ReceiveBufferSize = 256 * 1024;
                return tcp;
            }
            catch { return null; }
        }

        private static Bitmap ReadFrame(NetworkStream ns)
        {
            // 새 와이어 포맷([meta][jpeg], VisionFrameCodec) — 메타는 무시하고 이미지만(툴바 Live 용).
            QMC.Common.Ui.Controls.VisionFrameMeta meta;
            Bitmap bmp;
            return QMC.Common.Ui.Controls.VisionFrameCodec.ReadFrame(ns, out meta, out bmp) ? bmp : null;
        }

        private void SetLast(Bitmap bmp)
        {
            lock (_lastLock) { _last?.Dispose(); _last = (Bitmap)bmp.Clone(); }
        }

        public void Dispose()
        {
            StopLive();
            lock (_lastLock) { _last?.Dispose(); _last = null; }
        }
    }
}
