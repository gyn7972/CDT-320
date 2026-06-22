using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Modules;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 전역 통신(MainComm) 서버 — 핸들러 <c>VisionHub.Main</c>(5104) 과 짝.
    /// 특정 카메라 모듈에 속하지 않는 설비 전역 명령(레시피 변경 등)을 처리한다.
    /// <para>프로토콜(line-delimited, UTF-8): 요청 "MainComm|CMD|args"  응답 "ACK|MainComm|CMD|result" / "ERR|MainComm|CMD|msg"</para>
    /// <para>지원 명령:</para>
    /// <list type="bullet">
    ///   <item>PING — 연결 확인</item>
    ///   <item>RECIPE &lt;번호&gt; &lt;명칭&gt; — VisionMachine 전체에 LoadRecipe(명칭) cascade(Machine→Unit→Component→Algorithm)</item>
    /// </list>
    /// </summary>
    public sealed class MainCommServer : IDisposable
    {
        public const string ModuleKey = "MainComm";

        public event Action<string> Log;

        private readonly VisionMachine _machine;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public int  Port      { get; }
        public bool IsRunning { get; private set; }

        /// <summary>핸들러 등 클라이언트가 1개 이상 접속해 있으면 true(작업 탭 RUN 가능 판정용).</summary>
        public bool HasClient { get { lock (_clients) { return _clients.Count > 0; } } }

        /// <summary>마지막으로 명령 라인을 수신한 시각(UTC). 워치독/최근수신 표시용. 미수신 시 default.</summary>
        public DateTime LastRxUtc { get; private set; }

        public MainCommServer(VisionMachine machine, int port = 5104)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            Port = port;
        }

        public void Start()
        {
            if (IsRunning) return;
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                IsRunning = true;
                LogMsg($"[{ModuleKey}:{Port}] listening");
                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                LogMsg($"[{ModuleKey}:{Port}] start failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            lock (_clients) { foreach (var c in _clients.ToList()) try { c.Close(); } catch { } _clients.Clear(); }
            IsRunning = false;
            LogMsg($"[{ModuleKey}:{Port}] stopped");
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    lock (_clients) _clients.Add(client);
                    LogMsg($"[{ModuleKey}:{Port}] client connected: {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => HandleClient(client, ct));
                }
            }
            catch { /* 종료 시 정상 예외 */ }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            var stream = client.GetStream();
            var buf    = new byte[4096];
            var sb     = new StringBuilder();
            try
            {
                while (!ct.IsCancellationRequested && client.Connected)
                {
                    int n = await stream.ReadAsync(buf, 0, buf.Length, ct);
                    if (n == 0) break;
                    sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                    string all = sb.ToString();
                    int idx;
                    while ((idx = all.IndexOfAny(new[] { '\n', '\r' })) >= 0)
                    {
                        var line = all.Substring(0, idx).Trim();
                        all = all.Substring(idx + 1);
                        if (line.Length > 0) ProcessLine(stream, line);
                    }
                    sb.Clear(); sb.Append(all);
                }
            }
            catch { }
            finally
            {
                lock (_clients) _clients.Remove(client);
                try { client.Close(); } catch { }
                LogMsg($"[{ModuleKey}:{Port}] client disconnected");
            }
        }

        private void ProcessLine(NetworkStream stream, string line)
        {
            LastRxUtc = DateTime.UtcNow;
            LogMsg($"[{ModuleKey}] RX: {line}");
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";

            if (!string.Equals(mod, ModuleKey, StringComparison.OrdinalIgnoreCase))
            {
                Send(stream, $"ERR|{mod}|{cmd}|unknown module");
                return;
            }
            try
            {
                string resp;
                switch (cmd)
                {
                    case "PING":   resp = "OK";            break;
                    case "RECIPE": resp = DoRecipe(parts); break;
                    default:       resp = null;            break;
                }
                if (resp == null) Send(stream, $"ERR|{ModuleKey}|{cmd}|unknown command");
                else              Send(stream, $"ACK|{ModuleKey}|{cmd}|{resp}");
            }
            catch (Exception ex)
            {
                Send(stream, $"ERR|{ModuleKey}|{cmd}|{ex.Message}");
            }
        }

        // ── 명령 핸들러 ────────────────────────────

        /// <summary>"MainComm|RECIPE|번호|명칭" — VisionMachine 전체에 LoadRecipe(명칭) cascade.</summary>
        private string DoRecipe(string[] parts)
        {
            if (parts.Length < 4) return "fail:need recipeNo recipeName";
            if (!int.TryParse(parts[2], out var no)) no = 0;
            string name = parts[3];
            if (string.IsNullOrWhiteSpace(name)) return "fail:empty name";

            // Machine → Unit(Module) → Component(Camera) → Algorithm 으로 Recipe cascade.
            _machine.LoadRecipe(name);
            _machine.Recipe.RecipeNo   = no;
            _machine.Recipe.RecipeName = name;

            LogMsg($"[{ModuleKey}] recipe applied: no={no} name={name}");
            return $"OK;no={no};name={name}";
        }

        // ── 송신 ───────────────────────────────────

        private void Send(NetworkStream stream, string line)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                stream.Write(data, 0, data.Length);
                LogMsg($"[{ModuleKey}] TX: {line}");
            }
            catch { }
        }

        private void LogMsg(string s) { try { Log?.Invoke(s); } catch { } }

        public void Dispose() => Stop();
    }
}
