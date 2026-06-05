using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 모듈(검사 알고리즘 스테이션)별 그랩 영상을 TCP 로 송출하는 서버 (1:N).
    /// 와이어 프로토콜: [4바이트 int32 LE 길이][JPEG 바이트]. SP_RemoteViewer 의 RemoteViewerControl 호환.
    /// 프레임 소스는 frameProvider 로 주입 — 그랩 이미지(VisionModule.AcquireViewerFrame) 또는 화면영역 캡처.
    /// </summary>
    public sealed class GrabStreamServer : IDisposable
    {
        private sealed class Client
        {
            public TcpClient Tcp;
            public NetworkStream Stream;
            public Thread SendThread;
            public string EndPoint;
            public volatile bool Active;
            public long LastSentFrameId = -1;
            public ManualResetEventSlim FrameReady = new ManualResetEventSlim(false);
        }

        private readonly string _name;
        private readonly int _port;
        private readonly Func<Bitmap> _frameProvider;
        private readonly int _fps;
        private readonly long _quality;

        private TcpListener _listener;
        private Thread _acceptThread;
        private Thread _captureThread;
        private volatile bool _running;

        private readonly List<Client> _clients = new List<Client>();
        private readonly object _clientsLock = new object();

        private byte[] _latestFrame;
        private long _frameId;
        private readonly object _frameLock = new object();

        public event Action<string> Status;

        public string Name => _name;
        public int Port => _port;
        public bool IsRunning => _running;
        public int ConnectedClients
        {
            get { lock (_clientsLock) return _clients.Count(c => c.Active); }
        }

        public GrabStreamServer(string name, int port, Func<Bitmap> frameProvider,
                                int fps = 10, long jpegQuality = 60)
        {
            _name = name ?? "Viewer";
            _port = port;
            _frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
            _fps = Math.Max(1, Math.Min(60, fps));
            _quality = Math.Max(1, Math.Min(100, jpegQuality));
        }

        public void Start()
        {
            if (_running) return;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            OnStatus($"{_name} viewer 시작 — port {_port}, {_fps}fps, q{_quality}");

            _captureThread = new Thread(CaptureLoop) { IsBackground = true, Name = _name + "-cap" };
            _captureThread.Start();

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = _name + "-acc" };
            _acceptThread.Start();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            try { _listener?.Stop(); } catch { }

            if (_captureThread != null && _captureThread.IsAlive) try { _captureThread.Join(1000); } catch { }
            if (_acceptThread != null && _acceptThread.IsAlive) try { _acceptThread.Join(1000); } catch { }

            lock (_clientsLock)
            {
                foreach (var c in _clients) Disconnect(c);
                _clients.Clear();
            }
            OnStatus($"{_name} viewer 중지");
        }

        public void Dispose() => Stop();

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient tcp = _listener.AcceptTcpClient();
                    tcp.NoDelay = true;
                    tcp.SendBufferSize = 256 * 1024;

                    var c = new Client
                    {
                        Tcp = tcp,
                        Stream = tcp.GetStream(),
                        EndPoint = tcp.Client.RemoteEndPoint?.ToString() ?? "?",
                        Active = true
                    };
                    lock (_clientsLock) _clients.Add(c);
                    OnStatus($"{_name} viewer 클라이언트 연결: {c.EndPoint} (총 {ConnectedClients})");

                    c.SendThread = new Thread(() => SendLoop(c)) { IsBackground = true, Name = _name + "-send" };
                    c.SendThread.Start();
                }
                catch (SocketException) { break; }   // listener stopped
                catch (Exception ex) { if (_running) OnStatus($"{_name} accept 오류: {ex.Message}"); }
            }
        }

        private void CaptureLoop()
        {
            int interval = 1000 / _fps;
            while (_running)
            {
                DateTime t0 = DateTime.UtcNow;
                try
                {
                    Bitmap bmp = null;
                    try { bmp = _frameProvider(); } catch { bmp = null; }
                    if (bmp != null)
                    {
                        byte[] jpeg;
                        try { jpeg = CompressJpeg(bmp, _quality); }
                        finally { bmp.Dispose(); }

                        if (jpeg != null)
                        {
                            lock (_frameLock) { _latestFrame = jpeg; _frameId++; }
                            lock (_clientsLock)
                                foreach (var c in _clients) if (c.Active) c.FrameReady.Set();
                        }
                    }
                }
                catch (Exception ex) { if (_running) OnStatus($"{_name} capture 오류: {ex.Message}"); }

                int elapsed = (int)(DateTime.UtcNow - t0).TotalMilliseconds;
                int sleep = Math.Max(0, interval - elapsed);
                if (sleep > 0) Thread.Sleep(sleep);
            }
        }

        private void SendLoop(Client c)
        {
            try
            {
                while (_running && c.Active && c.Tcp.Connected)
                {
                    if (!c.FrameReady.Wait(1000)) continue;
                    c.FrameReady.Reset();

                    byte[] frame = null;
                    long id = 0;
                    lock (_frameLock)
                    {
                        id = _frameId;
                        if (id <= c.LastSentFrameId) continue;
                        frame = _latestFrame;
                    }
                    if (frame == null) continue;

                    try
                    {
                        byte[] sizeBytes = BitConverter.GetBytes(frame.Length); // int32 little-endian
                        c.Stream.Write(sizeBytes, 0, 4);
                        c.Stream.Write(frame, 0, frame.Length);
                        c.Stream.Flush();
                        c.LastSentFrameId = id;
                    }
                    catch { break; }   // client gone
                }
            }
            catch { }
            finally
            {
                lock (_clientsLock)
                {
                    if (_clients.Contains(c))
                    {
                        Disconnect(c);
                        _clients.Remove(c);
                        OnStatus($"{_name} viewer 클라이언트 해제: {c.EndPoint} (총 {ConnectedClients})");
                    }
                }
            }
        }

        private static void Disconnect(Client c)
        {
            c.Active = false;
            try { c.FrameReady.Set(); } catch { }
            try { c.Stream?.Close(); } catch { }
            try { c.Tcp?.Close(); } catch { }
            if (c.SendThread != null && c.SendThread.IsAlive && c.SendThread != Thread.CurrentThread)
                try { c.SendThread.Join(500); } catch { }
            try { c.FrameReady?.Dispose(); } catch { }
        }

        private static ImageCodecInfo _jpegCodec;
        private static byte[] CompressJpeg(Bitmap image, long quality)
        {
            if (_jpegCodec == null)
                _jpegCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var ms = new MemoryStream())
            {
                if (_jpegCodec != null)
                {
                    using (var ep = new EncoderParameters(1))
                    {
                        ep.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                        image.Save(ms, _jpegCodec, ep);
                    }
                }
                else image.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        /// <summary>ScreenRegion 소스용 — 지정 사각형(Empty 면 주 모니터 전체) 캡처.</summary>
        public static Bitmap CaptureScreenRegion(Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }
            catch { }
            return bmp;
        }

        private void OnStatus(string s) { var h = Status; if (h != null) try { h(s); } catch { } }
    }
}
