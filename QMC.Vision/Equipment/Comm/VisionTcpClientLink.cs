using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 핸들러(메인 제어 PC)에 <b>접속(dial-out)</b>하는 모듈별 TCP 클라이언트.
    /// <para>
    /// 토폴로지: <b>핸들러 = TCP 서버(listen)</b>, <b>Vision = TCP 클라이언트(connect)</b>.
    /// 명령 마스터는 핸들러 — 핸들러가 내려보낸 명령 라인을 받아 <see cref="VisionCommandRouter"/>
    /// 로 처리하고 ACK/ERR 로 응답한다. 비동기 이벤트는 EPD/ARM 으로 핸들러에 푸시한다.
    /// </para>
    /// 프로토콜(line-delimited, UTF-8):
    ///   RX(핸들러→Vision): "MODULE|CMD|args"
    ///   TX(Vision→핸들러): "ACK|MODULE|CMD|result" / "ERR|MODULE|CMD|msg"
    ///   푸시:              "EPD|MODULE" / "ARM|MODULE|reason"
    /// </summary>
    public sealed class VisionTcpClientLink : IDisposable
    {
        public event Action<string> Log;

        /// <summary>명령 수락 게이트 — null 이면 항상 허용. false 면 PING 외 명령 거부(RUN 아닐 때).</summary>
        public Func<bool> IsCommandAllowed { get; set; }

        /// <summary>접속 상태 변화 통지(true=핸들러 접속됨).</summary>
        public event Action<bool> ConnectionChanged;

        public string HandlerHost { get; }
        public int    Port        { get; }
        public string ModuleName  { get; }
        public IVisionModule Module { get; }

        public bool IsRunning   { get; private set; }
        public bool IsConnected => _client != null && _client.Connected;

        /// <summary>끊긴 뒤 재접속 시도 간격(ms).</summary>
        public int ReconnectIntervalMs { get; set; } = 2000;

        private TcpClient     _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private VisionSettings _cfg;
        private readonly object _txLock = new object();

        public VisionTcpClientLink(IVisionModule module, string handlerHost, int port)
        {
            Module      = module ?? throw new ArgumentNullException(nameof(module));
            ModuleName  = module.Name;
            HandlerHost = handlerHost;
            Port        = port;

            // 비동기 이벤트 → 핸들러로 푸시
            module.ExposureDone += OnExposureDone;
            module.Alarmed      += OnAlarmed;

            _cfg = VisionConfigStore.Current ?? new VisionSettings();
            module.DelayBeforeGrabMs = _cfg.DelayBeforeGrabMs;
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _cts = new CancellationTokenSource();
            LogMsg($"[{ModuleName}->{HandlerHost}:{Port}] client start");
            _ = Task.Run(() => ConnectLoop(_cts.Token));
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            try { _cts?.Cancel(); } catch { }
            CloseActive(false);
            LogMsg($"[{ModuleName}->{HandlerHost}:{Port}] client stop");
        }

        private async Task ConnectLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var c = new TcpClient();
                    await c.ConnectAsync(HandlerHost, Port).ConfigureAwait(false);
                    _client = c;
                    _stream = c.GetStream();
                    LogMsg($"[{ModuleName}] connected to handler {HandlerHost}:{Port}");
                    RaiseConn(true);
                    await ReceiveLoop(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogMsg($"[{ModuleName}] connect/recv error: {ex.Message}");
                }
                finally
                {
                    CloseActive(true);
                }

                if (ct.IsCancellationRequested) break;
                try { await Task.Delay(ReconnectIntervalMs, ct).ConfigureAwait(false); } catch { break; }
            }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[4096];
            var sb  = new StringBuilder();
            var stream = _stream;
            while (!ct.IsCancellationRequested && _client != null && _client.Connected && stream != null)
            {
                int n = await stream.ReadAsync(buf, 0, buf.Length, ct).ConfigureAwait(false);
                if (n == 0) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                string all = sb.ToString();
                int idx;
                while ((idx = all.IndexOfAny(new[] { '\n', '\r' })) >= 0)
                {
                    var line = all.Substring(0, idx).Trim();
                    all = all.Substring(idx + 1);
                    if (line.Length > 0) ProcessLine(line);
                }
                sb.Clear(); sb.Append(all);
            }
        }

        private void ProcessLine(string line)
        {
            LogMsg($"[{ModuleName}] RX: {line}");
            string resp = VisionCommandRouter.Process(Module, _cfg, ModuleName, line, IsCommandAllowed);
            Send(resp);
        }

        // ── 비동기 이벤트 → 핸들러 푸시 ───────────

        private void OnExposureDone(string moduleName) => Send($"EPD|{moduleName}");
        private void OnAlarmed(string moduleName, string reason) => Send($"ARM|{moduleName}|{reason}");

        // ── 송신 ───────────────────────────────────

        private void Send(string line)
        {
            var stream = _stream;
            if (stream == null) return;
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                lock (_txLock) stream.Write(data, 0, data.Length);
                LogMsg($"[{ModuleName}] TX: {line}");
            }
            catch (Exception ex)
            {
                LogMsg($"[{ModuleName}] TX error: {ex.Message}");
            }
        }

        private void CloseActive(bool raise)
        {
            bool wasConnected = IsConnected;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null; _client = null;
            if (raise && wasConnected) RaiseConn(false);
        }

        private void RaiseConn(bool on) { try { ConnectionChanged?.Invoke(on); } catch { } }
        private void LogMsg(string s)   { try { Log?.Invoke(s); } catch { } }

        public void Dispose()
        {
            Stop();
            if (Module != null)
            {
                Module.ExposureDone -= OnExposureDone;
                Module.Alarmed      -= OnAlarmed;
            }
        }
    }
}
