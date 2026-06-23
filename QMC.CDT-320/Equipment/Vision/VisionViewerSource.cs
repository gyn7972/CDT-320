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
        private readonly VisionTcpClient _cmd;   // 툴바 Grab 시 Vision 에 촬상(EXPOSE) 명령. null 이면 수동 수신만.

        private Thread _thread;
        private volatile bool _running;
        private TcpClient _tcp;
        private Action<Bitmap> _onFrame;

        private readonly object _lastLock = new object();
        private Bitmap _last;   // 최근 수신 프레임(GrabFrame 단발용)

        /// <summary>프레임 메타(스케일/판정/결과/마크) 수신 — 이미지와 별개로 오버레이 표시용. 백그라운드 스레드.</summary>
        public event Action<QMC.Common.Ui.Controls.VisionFrameMeta> FrameMeta;

        /// <summary>상태 메시지(촬상 OK / READY 거부 / 미연결 등). UI 표시용.</summary>
        public event Action<string> Status;

        private void OnStatus(string s) { var h = Status; if (h != null) try { h(s); } catch { } }

        public VisionViewerSource(string host, int port, int connectTimeoutMs = 2000, VisionTcpClient commandClient = null)
        {
            _host = host; _port = port; _connectTimeoutMs = connectTimeoutMs; _cmd = commandClient;
        }

        public bool SupportsLive => true;

        public void StartLive(Action<Bitmap> onFrame)
        {
            if (_running) return;
            _onFrame = onFrame;
            _running = true;
            VisionViewerRegistry.StreamStarted(_port);   // 스트리밍 상태 등록(설정 페이지 표시용)
            _thread = new Thread(RecvLoop) { IsBackground = true, Name = "VisionViewer-" + _port };
            _thread.Start();
        }

        public void StopLive()
        {
            bool was = _running;
            _running = false;
            if (was) VisionViewerRegistry.StreamStopped(_port);
            try { _tcp?.Close(); } catch { }
            try { _thread?.Join(800); } catch { }
            _thread = null;
        }

        public Bitmap GrabFrame()
        {
            // 핸들러 카메라뷰 툴바 Grab → Vision 에 실제 촬상(EXPOSE) 명령을 보내고 그 결과 프레임을 표시한다.
            // (RUN 게이트: Vision 이 RUN 아닐 때=READY 면 거부됨. 명령 클라이언트가 없으면 기존처럼 수동 수신만.)
            if (_cmd != null)
            {
                if (!_cmd.IsConnected) { OnStatus("명령 미연결 — CONNECT 확인"); return null; }
                bool ack;
                try { ack = _cmd.ExposeAsync(0, 2000, System.Threading.CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { OnStatus("촬상 실패: " + ex.Message); return null; }
                if (!ack) { OnStatus("촬상 거부 — Vision RUN 상태에서만 가능(READY 불가)"); return null; }
                OnStatus("촬상 OK");
                // 라이브 중이면 RecvLoop 가 새 프레임을 표시하므로 여기선 null. 아니면 단발로 받아 반환.
                if (_running) return null;
                return ReadSingleFrame();
            }

            // 명령 채널 없음: 기존 동작 — 라이브 중이면 최근 프레임, 아니면 단발 수신.
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
                    var ns = tcp.GetStream();
                    // 단발 Grab 은 UI 스레드에서 동기로 읽히므로, 프레임이 안 오면 멈추지 않도록 읽기 타임아웃을 건다.
                    // (Live 의 RecvLoop 는 별도 연결이라 타임아웃 없음 — 아이들 대기 정상.)
                    try { ns.ReadTimeout = 1500; } catch { }
                    return ReadFrame(ns);
                }
            }
            catch { return null; }   // 타임아웃/끊김 시 프레임 없음으로 처리(멈추지 않음)
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

        private Bitmap ReadFrame(NetworkStream ns)
        {
            // 새 와이어 포맷([meta][jpeg], VisionFrameCodec) — 이미지는 반환하고, 메타는 FrameMeta 이벤트로 통지(오버레이용).
            QMC.Common.Ui.Controls.VisionFrameMeta meta;
            Bitmap bmp;
            if (!QMC.Common.Ui.Controls.VisionFrameCodec.ReadFrame(ns, out meta, out bmp))
                return null;

            if (meta != null)
            {
                var h = FrameMeta;
                if (h != null) try { h(meta); } catch { }
            }
            return bmp;
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
