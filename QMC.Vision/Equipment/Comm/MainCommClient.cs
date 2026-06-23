using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Modules;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 전역 통신(MainComm) 클라이언트 — 핸들러 <c>VisionHub.Main</c>(서버, 5104) 에 접속한다.
    /// <para>토폴로지: 핸들러=서버, Vision=클라이언트. 핸들러가 보내는 전역 명령(레시피 변경 등)을 처리한다.</para>
    /// 프로토콜(line-delimited, UTF-8): 요청 "MainComm|CMD|args"  응답 "ACK|MainComm|CMD|result" / "ERR|MainComm|CMD|msg"
    /// <list type="bullet">
    ///   <item>PING — 연결 확인</item>
    ///   <item>RECIPE &lt;번호&gt; &lt;명칭&gt; — VisionMachine 전체에 LoadRecipe(명칭) cascade</item>
    /// </list>
    /// </summary>
    public sealed class MainCommClient : IDisposable
    {
        public const string ModuleKey = "MainComm";

        public event Action<string> Log;
        public event Action<bool>   ConnectionChanged;

        public string HandlerHost { get; }
        public int    Port        { get; }

        public bool IsRunning   { get; private set; }
        public bool IsConnected => _client != null && _client.Connected;

        public int ReconnectIntervalMs { get; set; } = 2000;

        private readonly VisionMachine _machine;
        private TcpClient     _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly object _txLock = new object();

        public MainCommClient(VisionMachine machine, string handlerHost, int port = 5104)
        {
            _machine    = machine ?? throw new ArgumentNullException(nameof(machine));
            HandlerHost = handlerHost;
            Port        = port;
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            _cts = new CancellationTokenSource();
            LogMsg($"[{ModuleKey}->{HandlerHost}:{Port}] client start");
            _ = Task.Run(() => ConnectLoop(_cts.Token));
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            try { _cts?.Cancel(); } catch { }
            CloseActive(false);
            LogMsg($"[{ModuleKey}->{HandlerHost}:{Port}] client stop");
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
                    LogMsg($"[{ModuleKey}] connected to handler {HandlerHost}:{Port}");
                    RaiseConn(true);
                    await ReceiveLoop(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogMsg($"[{ModuleKey}] connect/recv error: {ex.Message}");
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
            LogMsg($"[{ModuleKey}] RX: {line}");
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";

            if (!string.Equals(mod, ModuleKey, StringComparison.OrdinalIgnoreCase))
            {
                Send($"ERR|{mod}|{cmd}|unknown module");
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
                if (resp == null) Send($"ERR|{ModuleKey}|{cmd}|unknown command");
                else              Send($"ACK|{ModuleKey}|{cmd}|{resp}");
            }
            catch (Exception ex)
            {
                Send($"ERR|{ModuleKey}|{cmd}|{ex.Message}");
            }
        }

        /// <summary>"MainComm|RECIPE|번호|명칭" — 핸들러 수신 레시피로 셋팅.
        /// 레시피가 있으면 로드(cascade), 없으면 새로 생성(기본값 리셋+빈 레시피 저장) 후 적용. 마지막 레시피 영속화.</summary>
        private string DoRecipe(string[] parts)
        {
            if (parts.Length < 4) return "fail:need recipeNo recipeName";
            if (!int.TryParse(parts[2], out var no)) no = 0;
            string name = parts[3];
            if (string.IsNullOrWhiteSpace(name)) return "fail:empty name";

            bool exists = false;
            try { exists = System.IO.Directory.Exists(QMC.Common.Data.Store.RecipeDataStore.DirOf(name)); }
            catch { exists = false; }

            if (exists)
                _machine.SetRecipe(name);    // 기존 레시피 로드 + 명칭 고정
            else
                _machine.NewRecipe(name);    // 없으면 생성: 기본값 리셋 + 빈 레시피 저장 후 명칭 고정

            _machine.Recipe.RecipeNo   = no;
            _machine.Recipe.RecipeName = name;

            try
            {
                var cfg = QMC.Vision.Config.VisionConfigStore.Current;
                if (cfg != null) { cfg.LastRecipeName = name; QMC.Vision.Config.VisionConfigStore.Save(); }
            }
            catch { }

            LogMsg($"[{ModuleKey}] recipe {(exists ? "applied" : "created+applied")}: no={no} name={name}");
            return $"OK;no={no};name={name}";
        }

        private void Send(string line)
        {
            var stream = _stream;
            if (stream == null) return;
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                lock (_txLock) stream.Write(data, 0, data.Length);
                LogMsg($"[{ModuleKey}] TX: {line}");
            }
            catch (Exception ex)
            {
                LogMsg($"[{ModuleKey}] TX error: {ex.Message}");
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

        public void Dispose() => Stop();
    }
}
