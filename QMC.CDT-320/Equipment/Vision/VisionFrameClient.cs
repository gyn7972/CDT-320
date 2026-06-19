using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using QMC.Common.Ui.Controls;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Vision 그랩 스트림(VisionFrameCodec: [4B metaLen][meta][4B jpegLen][JPEG]) 수신 클라이언트.
    /// 프레임마다 <see cref="Frame"/>(meta, bitmap) 이벤트 — 비트맵은 소비자가 표시 후 Dispose.
    /// 끊기면 자동 재접속. 콜백은 백그라운드 스레드이므로 UI 는 소비자가 마샬링해야 한다.
    /// </summary>
    public sealed class VisionFrameClient : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _connectTimeoutMs;
        private Thread _thread;
        private volatile bool _running;
        private TcpClient _tcp;

        /// <summary>프레임 수신 — (meta, bitmap). bitmap 은 소비자가 표시 후 Dispose.</summary>
        public event Action<VisionFrameMeta, Bitmap> Frame;
        /// <summary>상태 메시지(연결/끊김/오류).</summary>
        public event Action<string> Status;

        public VisionFrameClient(string host, int port, int connectTimeoutMs = 2000)
        {
            _host = host; _port = port; _connectTimeoutMs = connectTimeoutMs;
        }

        public bool IsRunning => _running;

        public void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Loop) { IsBackground = true, Name = "VisionFrameClient-" + _port };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _tcp?.Close(); } catch { }
            try { _thread?.Join(800); } catch { }
            _thread = null;
        }

        public void Dispose() => Stop();

        private void Loop()
        {
            while (_running)
            {
                try
                {
                    using (var tcp = Connect())
                    {
                        if (tcp == null) { if (_running) Thread.Sleep(500); continue; }
                        _tcp = tcp;
                        OnStatus("connected " + _host + ":" + _port);
                        var ns = tcp.GetStream();
                        while (_running)
                        {
                            VisionFrameMeta meta; Bitmap bmp;
                            if (!VisionFrameCodec.ReadFrame(ns, out meta, out bmp)) break;
                            var h = Frame;
                            if (h != null) h(meta, bmp); else bmp?.Dispose();
                        }
                    }
                }
                catch (Exception ex) { OnStatus("error: " + ex.Message); }
                finally { _tcp = null; }
                if (_running) { OnStatus("reconnecting..."); Thread.Sleep(400); }
            }
            OnStatus("stopped");
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

        private void OnStatus(string s) { var h = Status; if (h != null) try { h(s); } catch { } }
    }
}
