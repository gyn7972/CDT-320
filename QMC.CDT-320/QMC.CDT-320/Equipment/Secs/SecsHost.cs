using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

using Alarms = QMC.Common.Alarms;
namespace QMC.CDT320.Secs
{
    /// <summary>
    /// SECS/GEM Host 통신 베이스 — 310 의 DieTransferGemService 단순화.
    /// 본 라운드는 <b>골격</b> 만 — Stream/Function 디스패치 + RemoteCommand 4종 + EventReport 송신 + ZipRecipe.
    /// 실제 HSMS 연결은 별도 라운드(이번 라운드는 line-delimited TCP 시뮬 가능).
    /// </summary>
    public class SecsHost : IDisposable
    {
        public event Action<string> Log;

        // ── 설정 ──
        public int Port { get; }
        public bool IsRunning { get; private set; }

        /// <summary>true 면 HsmsConnection (4-byte length prefix + SecsMessage) 사용. false 면 line-protocol.</summary>
        public bool UseHsms { get; set; } = false;
        /// <summary>HSMS 모드일 때의 활성 연결.</summary>
        public HsmsConnection HsmsActive { get; private set; }

        // ── RemoteCommand 핸들러 (등록형) ──
        private readonly Dictionary<string, Func<string[], int>> _remoteCommands
            = new Dictionary<string, Func<string[], int>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, Func<string[], int>> RemoteCommands => _remoteCommands;

        // ── 이벤트 리포트 큐 ──
        private readonly ConcurrentQueue<string> _eventQueue = new ConcurrentQueue<string>();

        // ── TCP ──
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public SecsHost(int port = 5000)
        {
            Port = port;
            RegisterStandardCommands();
            // Stage 7 — AlarmManager 이벤트 자동 구독 → S5F1 broadcast
            try { QMC.Common.Alarms.AlarmManager.AlarmRaised += OnAlarmRaised; } catch { }
        }

        private void OnAlarmRaised(QMC.Common.Alarms.AlarmRecord rec)
        {
            try
            {
                if (rec == null) return;
                byte sev = (byte)(rec.Severity);
                RaiseEvent("AlarmPosted", rec.Code ?? "", sev.ToString(), rec.Source ?? "", rec.Message ?? "");
            }
            catch { }
        }

        // ─────────────────────────────────────────
        //  K2: RemoteCommand (호스트 → 장비)
        // ─────────────────────────────────────────
        private void RegisterStandardCommands()
        {
            _remoteCommands["ProceedWithTapeFrame"] = args =>
            {
                if (args == null || args.Length < 1) return -1;
                var f = MaterialStorage.GetFrame(args[0]);
                if (f == null) return -2;
                if (f.IdentifierState != IdentifierState.WaitingForHost) return -3;
                f.IdentifierState = IdentifierState.VerificationOk;
                LogMsg($"[RC] ProceedWithTapeFrame {args[0]} → VerificationOk");
                return 0;
            };
            _remoteCommands["StoppedWithTapeFrame"] = args =>
            {
                if (args == null || args.Length < 1) return -1;
                var f = MaterialStorage.GetFrame(args[0]);
                if (f == null) return -2;
                f.IdentifierState = IdentifierState.VerificationFailed;
                LogMsg($"[RC] StoppedWithTapeFrame {args[0]} → VerificationFailed");
                return 0;
            };
            _remoteCommands["ProceedWithMap"] = args =>
            {
                if (args == null || args.Length < 2) return -1;
                var f = MaterialStorage.GetFrame(args[0]);
                if (f == null) return -2;
                if (f.DieMapGenerateState != DieMapGenerateState.WaitingForHost) return -3;
                if (!File.Exists(args[1])) { f.DieMapGenerateState = DieMapGenerateState.VerificationFailed; return -4; }
                f.MapFileName = args[1];
                f.DieMapGenerateState = DieMapGenerateState.VerificationOk;
                LogMsg($"[RC] ProceedWithMap {args[0]} {args[1]} → VerificationOk");
                return 0;
            };
            _remoteCommands["StoppedWithMap"] = args =>
            {
                if (args == null || args.Length < 1) return -1;
                var f = MaterialStorage.GetFrame(args[0]);
                if (f == null) return -2;
                f.DieMapGenerateState = DieMapGenerateState.VerificationFailed;
                LogMsg($"[RC] StoppedWithMap {args[0]} → VerificationFailed");
                return 0;
            };
        }

        public void RegisterCommand(string name, Func<string[], int> handler)
        {
            if (string.IsNullOrEmpty(name) || handler == null) return;
            _remoteCommands[name] = handler;
        }

        // ─────────────────────────────────────────
        //  K3: EventReport (장비 → 호스트)
        // ─────────────────────────────────────────
        public void RaiseEvent(string eventName, params string[] data)
        {
            string line = $"EVT|{eventName}|{(data == null ? 0 : data.Length)}" +
                          (data == null || data.Length == 0 ? "" : "|" + string.Join("|", data));
            _eventQueue.Enqueue(line);
            BroadcastQueued();
        }

        public void RaiseAlarmPosted(string code, string source, string message)
            => RaiseEvent("AlarmPosted", code, source, message);

        public void RaiseMaterialChanged(string objId, string state)
            => RaiseEvent("MaterialChanged", objId, state);

        public void RaiseJobOrderStateChanged(string uid, string type, string state)
            => RaiseEvent("JobOrderStateChanged", uid, type, state);

        // ─────────────────────────────────────────
        //  K4: Recipe ZIP 직렬화 (310 ZipRecipeBodySecsItemConverter 동등)
        // ─────────────────────────────────────────
        public static string SerializeZipBase64(RecipeProject recipe)
        {
            if (recipe == null) return null;
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                {
                    var ser = new DataContractJsonSerializer(typeof(RecipeProject));
                    ser.WriteObject(zip, recipe);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static RecipeProject DeserializeZipBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using (var ms = new MemoryStream(bytes))
                using (var zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    var ser = new DataContractJsonSerializer(typeof(RecipeProject));
                    return (RecipeProject)ser.ReadObject(zip);
                }
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────
        //  TCP 서버 (line-delimited 시뮬 모드)
        // ─────────────────────────────────────────
        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                IsRunning = true;
                LogMsg($"[SECS] listening on {Port}  mode={(UseHsms ? "HSMS" : "line")}");
                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex) { LogMsg("[SECS] start failed: " + ex.Message); }
        }

        /// <summary>접속한 host 와 HSMS 핸드셰이크 후 SecsMessage 큐 처리.</summary>
        private void HandleHsmsClient(TcpClient client)
        {
            // HsmsConnection 은 host 측에서 ConnectAsync 하지만, 우리가 server 라서
            // 이미 연결된 socket 을 NetworkStream 으로 바로 wrapping.
            try
            {
                var stream = client.GetStream();
                var buf = new byte[10];
                while (client.Connected && !_cts.Token.IsCancellationRequested)
                {
                    int read = ReadExact(stream, buf, 4);
                    if (read != 4) break;
                    int len = (buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3];
                    if (len < 0 || len > 16 * 1024 * 1024) break;
                    var payload = new byte[len];
                    if (len > 0)
                    {
                        read = ReadExact(stream, payload, len);
                        if (read != len) break;
                    }
                    var msg = SecsMessage.Parse(payload);
                    if (msg == null)
                    {
                        LogMsg("[HSMS] bad message");
                        continue;
                    }
                    LogMsg($"[HSMS] RX {msg}");
                    HandleHsmsMessage(stream, msg);
                }
            }
            catch (Exception ex) { LogMsg("[HSMS] client err: " + ex.Message); }
        }

        private static int ReadExact(NetworkStream s, byte[] buf, int n)
        {
            int total = 0;
            while (total < n)
            {
                int r = s.Read(buf, total, n - total);
                if (r == 0) return total;
                total += r;
            }
            return total;
        }

        private void HandleHsmsMessage(NetworkStream stream, SecsMessage msg)
        {
            // S1F1 → S1F2 (PING/PONG)
            if (msg.Stream == 1 && msg.Function == 1)
            {
                var reply = new SecsMessage { Stream = 1, Function = 2, ReplyExpected = false, SystemBytes = msg.SystemBytes };
                SendHsmsBytes(stream, reply.ToBytes());
                return;
            }

            // S2F41 (Host 명령 — RemoteCommand 디스패치). 실 SECS 는 list 디코딩 필요.
            // 우리 단순 버전: TextPayload 가 "RC|<cmd>|arg1|..." 형식
            if (msg.Stream == 2 && msg.Function == 41 && msg.ReplyExpected)
            {
                string text = msg.TextPayload ?? "";
                int rc = -99;
                string cmdName = "";
                try
                {
                    var parts = text.Split('|');
                    if (parts.Length >= 2 && parts[0] == "RC")
                    {
                        cmdName = parts[1];
                        var args = parts.Length > 2 ? new string[parts.Length - 2] : new string[0];
                        Array.Copy(parts, 2, args, 0, args.Length);
                        if (_remoteCommands.TryGetValue(cmdName, out var h)) rc = h(args);
                        else rc = -1;
                    }
                }
                catch (Exception ex) { LogMsg("[HSMS] RC err: " + ex.Message); rc = -98; }

                var reply = new SecsMessage
                {
                    Stream = 2, Function = 42, ReplyExpected = false,
                    SystemBytes = msg.SystemBytes, TextPayload = $"RCACK|{cmdName}|{rc}"
                };
                SendHsmsBytes(stream, reply.ToBytes());
                return;
            }

            // 미정의 — 비워둠
        }

        private void SendHsmsBytes(NetworkStream stream, byte[] payload)
        {
            try
            {
                int len = payload.Length;
                byte[] frame = new byte[4 + len];
                frame[0] = (byte)((len >> 24) & 0xFF);
                frame[1] = (byte)((len >> 16) & 0xFF);
                frame[2] = (byte)((len >> 8)  & 0xFF);
                frame[3] = (byte)( len        & 0xFF);
                Buffer.BlockCopy(payload, 0, frame, 4, len);
                stream.Write(frame, 0, frame.Length);
                LogMsg($"[HSMS] TX {len} bytes");
            }
            catch (Exception ex) { LogMsg("[HSMS] tx err: " + ex.Message); }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            lock (_clients)
            {
                foreach (var c in _clients) try { c.Close(); } catch { }
                _clients.Clear();
            }
            IsRunning = false;
            LogMsg("[SECS] stopped");
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    lock (_clients) _clients.Add(client);
                    LogMsg($"[SECS] host connected: {client.Client.RemoteEndPoint}  mode={(UseHsms ? "HSMS" : "line")}");
                    if (UseHsms)
                        _ = Task.Run(() => HandleHsmsClient(client));
                    else
                        _ = Task.Run(() => HandleClient(client, ct));
                }
            }
            catch { }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    var buf = new byte[4096];
                    var sb = new StringBuilder();
                    while (!ct.IsCancellationRequested && client.Connected)
                    {
                        int n = await stream.ReadAsync(buf, 0, buf.Length, ct);
                        if (n == 0) break;
                        sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                        string all = sb.ToString();
                        int idx;
                        while ((idx = all.IndexOf('\n')) >= 0)
                        {
                            var line = all.Substring(0, idx).Trim();
                            all = all.Substring(idx + 1);
                            if (line.Length > 0) ProcessLine(stream, line);
                        }
                        sb.Clear(); sb.Append(all);
                    }
                }
            }
            catch { }
            finally
            {
                lock (_clients) _clients.Remove(client);
                LogMsg("[SECS] host disconnected");
            }
        }

        private void ProcessLine(NetworkStream stream, string line)
        {
            LogMsg($"[SECS] RX: {line}");
            // 라인 형식 — 시뮬 모드:  RC|<command>|<arg1>|<arg2>|...
            //               or       RECIPE_PUT|<base64-zip>
            //               or       PING
            try
            {
                var parts = line.Split('|');
                string typ = parts[0];
                if (typ == "PING")
                {
                    Send(stream, "PONG");
                    return;
                }
                if (typ == "RC")
                {
                    if (parts.Length < 2) { Send(stream, "ERR|need command"); return; }
                    string cmd = parts[1];
                    string[] args = parts.Length > 2 ? new string[parts.Length - 2] : new string[0];
                    Array.Copy(parts, 2, args, 0, args.Length);
                    if (_remoteCommands.TryGetValue(cmd, out var h))
                    {
                        int rc = h(args);
                        Send(stream, $"RCACK|{cmd}|{rc}");
                    }
                    else Send(stream, $"ERR|unknown command {cmd}");
                    return;
                }
                if (typ == "RECIPE_PUT")
                {
                    if (parts.Length < 2) { Send(stream, "ERR|empty recipe"); return; }
                    var rcp = DeserializeZipBase64(parts[1]);
                    if (rcp == null) { Send(stream, "ERR|deserialize fail"); return; }
                    RecipeStore.Save(rcp);
                    Send(stream, $"RECIPE_OK|{rcp.FileName}");
                    return;
                }
                if (typ == "RECIPE_GET")
                {
                    if (parts.Length < 2) { Send(stream, "ERR|need name"); return; }
                    var rcp = RecipeStore.Load(parts[1]);
                    if (rcp == null) { Send(stream, "ERR|not found"); return; }
                    Send(stream, "RECIPE_DATA|" + SerializeZipBase64(rcp));
                    return;
                }
                Send(stream, "ERR|unknown msg");
            }
            catch (Exception ex) { Send(stream, "ERR|" + ex.Message); }
        }

        private void Send(NetworkStream stream, string line)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                stream.Write(data, 0, data.Length);
                LogMsg($"[SECS] TX: {line}");
            }
            catch { }
        }

        private void BroadcastQueued()
        {
            List<TcpClient> snapshot;
            lock (_clients) snapshot = new List<TcpClient>(_clients);
            while (_eventQueue.TryDequeue(out var line))
            {
                if (UseHsms)
                {
                    // S6F11 (Event report send) — TextPayload 에 line 을 그대로
                    var msg = SecsMessage.S6F11(line);
                    msg.SystemBytes = (uint)Environment.TickCount;
                    byte[] payload = msg.ToBytes();
                    int len = payload.Length;
                    byte[] frame = new byte[4 + len];
                    frame[0] = (byte)((len >> 24) & 0xFF);
                    frame[1] = (byte)((len >> 16) & 0xFF);
                    frame[2] = (byte)((len >> 8)  & 0xFF);
                    frame[3] = (byte)( len        & 0xFF);
                    Buffer.BlockCopy(payload, 0, frame, 4, len);
                    foreach (var c in snapshot)
                    {
                        try { var s = c.GetStream(); s.Write(frame, 0, frame.Length); } catch { }
                    }
                    LogMsg($"[HSMS] PUSH S6F11: {line}");
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(line + "\n");
                    foreach (var c in snapshot)
                    {
                        try { var s = c.GetStream(); s.Write(data, 0, data.Length); } catch { }
                    }
                    LogMsg($"[SECS] PUSH: {line}");
                }
            }
        }

        private void LogMsg(string s) { try { Log?.Invoke(s); } catch { } }

        public void Dispose() => Stop();
    }
}
