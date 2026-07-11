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
    /// 뷰어 포트(모듈별): Wafer 5200 / BottomInspection 5201 / Bin 5203 / FrontSide 5205 / RearSide 5206.
    /// </summary>
    public sealed class VisionViewerSource : ICameraViewSource, IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _connectTimeoutMs;
        private readonly VisionTcpClient _cmd;   // 툴바 Grab 시 Vision 에 촬상(EXPOSE) 명령. null 이면 수동 수신만.

        private Thread _thread;
        private volatile bool _running;
        private volatile bool _liveCommandActive;
        private volatile bool _registryRegistered;
        private TcpClient _tcp;
        private Action<Bitmap> _onFrame;

        private readonly object _lastLock = new object();
        private Bitmap _last;   // 최근 수신 프레임(GrabFrame 단발용)

        /// <summary>프레임 메타(스케일/판정/결과/마크) 수신 — 이미지와 별개로 오버레이 표시용. 백그라운드 스레드.</summary>
        public event Action<QMC.Common.Ui.Controls.VisionFrameMeta> FrameMeta;

        /// <summary>상태 메시지(촬상 OK / READY 거부 / 미연결 등). UI 표시용.</summary>
        public event Action<string> Status;

        /// <summary>라이브가 (Vision 의 그랩 자동 정지에 따라) 종료됐을 때 발화 — CameraViewBase 가 툴바 Live 버튼을 해제한다.</summary>
        public event Action LiveStopped;

        private void OnStatus(string s) { var h = Status; if (h != null) try { h(s); } catch { } }
        private void RaiseLiveStopped() { var h = LiveStopped; if (h != null) try { h(); } catch { } }

        public VisionViewerSource(string host, int port, int connectTimeoutMs = 2000, VisionTcpClient commandClient = null)
        {
            _host = host; _port = port; _connectTimeoutMs = connectTimeoutMs; _cmd = commandClient;
        }

        public bool LiveEnabled { get; set; }

        public bool SupportsLive => LiveEnabled;

        public void StartLive(Action<Bitmap> onFrame)
        {
            if (_running) return;
            if (!LiveEnabled)
            {
                string blockReason = "Vision Live ON은 사용하지 않습니다. Grab 이미지 수신만 사용하세요.";
                OnStatus(blockReason);
                try
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Warning,
                        "VISION",
                        "VISION-LIVE-BLOCK",
                        "Vision Live 시작 요청을 차단했습니다. port=" + _port +
                        ", reason=" + blockReason);
                }
                catch { }
                throw new InvalidOperationException(blockReason);
            }

            // 핸들러 Live → Vision 카메라를 연속 촬상(Live)으로 전환. RUN/READY 중이면 Vision 이 거부(throw).
            // 이 명령이 있어야 Vision 이 프레임을 내보내고, 아래 RecvLoop 가 그 프레임을 받는다.
            RequestVisionLive(true);
            _liveCommandActive = true;
            _registryRegistered = true;
            _onFrame = onFrame;
            _running = true;
            VisionViewerRegistry.StreamStarted(_port);   // 스트리밍 상태 등록(설정 페이지 표시용)
            _thread = new Thread(RecvLoop) { IsBackground = true, Name = "VisionViewer-" + _port };
            _thread.Start();
        }

        /// <summary>Vision Live 명령 없이 뷰어 포트에서 Grab 이미지 프레임만 수신한다.
        /// CAM_SWITCH ON/OFF 를 절대 보내지 않으므로 카메라 Live 상태를 건드리지 않는다.</summary>
        public void StartGrabImageStream(Action<Bitmap> onFrame)
        {
            if (_running) return;

            _liveCommandActive = false;
            _registryRegistered = false;
            _onFrame = onFrame;
            _running = true;
            OnStatus("Grab 이미지 수신 시작");
            _thread = new Thread(RecvLoop) { IsBackground = true, Name = "VisionGrabImageViewer-" + _port };
            _thread.Start();
        }

        public void StopLive()
        {
            bool was = _running;
            bool liveCommandWasActive = _liveCommandActive;
            bool registryWasRegistered = _registryRegistered;
            _running = false;
            _liveCommandActive = false;
            _registryRegistered = false;
            if (was && registryWasRegistered) VisionViewerRegistry.StreamStopped(_port);
            try { _tcp?.Close(); } catch { }
            try { _thread?.Join(800); } catch { }
            _thread = null;
            // 라이브였을 때만 Vision 카메라 Live 정지 요청(재구성 시 불필요한 명령 방지).
            if (was && liveCommandWasActive) { try { RequestVisionLive(false); } catch { } }
            if (was) RaiseLiveStopped();
        }

        /// <summary>Vision 카메라 Live(연속 촬상) 시작/정지를 CAM_SWITCH 로 요청.
        /// 명령 채널이 없으면(수동 수신 전용) no-op. 시작 거부(RUN/READY 등) 시 예외로 던져
        /// CameraView 툴바가 Live 버튼 상태를 롤백하게 한다.</summary>
        private void RequestVisionLive(bool on, int timeoutMs = 3000)
        {
            if (_cmd == null) return;   // 명령 채널 없음 — 기존 수동 수신 동작 유지
            if (!_cmd.IsConnected)
            {
                OnStatus("명령 미연결 — CONNECT 확인");
                if (on) throw new InvalidOperationException("Vision 명령 미연결");
                return;
            }

            VisionCameraSwitchResult res;
            try { res = _cmd.SwitchCameraAsync(_cmd.ModuleName, on, timeoutMs).GetAwaiter().GetResult(); }
            catch (Exception ex)
            {
                if (on) throw new InvalidOperationException("Live 명령 실패: " + ex.Message, ex);
                return;   // 정지 실패는 조용히(스트림은 이미 끊음)
            }

            if (on && (res == null || !res.Success))
            {
                string reason = (res != null && !string.IsNullOrWhiteSpace(res.Raw)) ? res.Raw : "거부";
                OnStatus("Live 시작 거부 — " + reason);
                throw new InvalidOperationException(reason);
            }
            OnStatus(on ? "Vision Live 시작" : "Vision Live 정지");
        }

        public Bitmap GrabFrame()
        {
            // 핸들러 카메라뷰 툴바 Grab → Vision 에 실제 촬상(EXPOSE) 명령을 보내고 그 결과 프레임을 표시한다.
            // 허용/거부는 Vision 게이트가 결정(ACK/ERR). 정책: READY(O) armed 면 거부, 해제 상태면 가능.
            // 명령 클라이언트가 없으면 기존처럼 수동 수신만.
            if (_cmd != null)
            {
                if (!_cmd.IsConnected) { OnStatus("명령 미연결 — CONNECT 확인"); return null; }
                bool ack;
                try { ack = _cmd.ExposeAsync(0, 2000, System.Threading.CancellationToken.None).GetAwaiter().GetResult(); }
                catch (Exception ex) { OnStatus("촬상 실패: " + ex.Message); return null; }
                if (!ack) { OnStatus("촬상 거부 — Vision READY(O) 상태에서는 불가. READY 해제 후 다시 시도"); return null; }
                OnStatus("촬상 OK");
                // 라이브 중이면 RecvLoop 가 새 프레임을 표시하므로 여기선 null. 아니면 단발로 받아 반환.
                //   (라이브 중 그랩은 CameraViewBase 가 StopLive 를 먼저 수행하므로 여기 도달 시 _running=false 이다.)
                if (_running) return null;
                Bitmap frame = ReadSingleFrame();
                if (frame == null)
                    OnStatus("촬상 OK, 영상 프레임 수신 실패 — Viewer 포트/스트림 상태를 확인하세요.");
                return frame;
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
