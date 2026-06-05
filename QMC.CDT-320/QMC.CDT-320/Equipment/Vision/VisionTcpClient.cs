using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// QMC.Vision TCP 서버(매뉴얼 기준 Wafer/Bin/Bottom 모듈별 포트)와 통신하는 1:1 클라이언트.
    /// 프로토콜(라인 단위, UTF-8):
    ///   TX: "MODULE|CMD|args..."
    ///   RX: "ACK|MODULE|CMD|result"  또는  "ERR|MODULE|CMD|msg"
    /// </summary>
    public class VisionTcpClient : IDisposable
    {
        public string Host       { get; }
        public int    Port       { get; }
        public string ModuleName { get; }

        public bool IsConnected => _client != null && _client.Connected;

        public event Action<bool>   ConnectionChanged;
        public event Action<string> Log;
        /// <summary>비동기 푸시 — Vision 측 EPD (Exposure Done) 수신.</summary>
        public event Action<string> ExposureDone;
        /// <summary>비동기 푸시 — Vision 측 ARM (Alarm) 수신. arg=reason.</summary>
        public event Action<string, string> Alarmed;

        private TcpClient       _client;
        private NetworkStream   _stream;
        private readonly object _ioLock = new object();

        // 응답 대기 큐 (순차 통신)
        private readonly Queue<TaskCompletionSource<string>> _pending = new Queue<TaskCompletionSource<string>>();
        private readonly StringBuilder _rxBuf = new StringBuilder();
        private CancellationTokenSource _rxCts;

        public VisionTcpClient(string moduleName, string host, int port)
        {
            ModuleName = moduleName; Host = host; Port = port;
        }

        public async Task<bool> ConnectAsync(int timeoutMs = 3000)
        {
            if (IsConnected) return true;
            try
            {
                _client = new TcpClient();
                var task = _client.ConnectAsync(Host, Port);
                if (await Task.WhenAny(task, Task.Delay(timeoutMs)) != task)
                {
                    _client.Close(); _client = null;
                    LogMsg("connect timeout");
                    return false;
                }
                _stream = _client.GetStream();
                _rxCts  = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoop(_rxCts.Token));
                LogMsg($"connected to {Host}:{Port}");
                RaiseConn(true);
                return true;
            }
            catch (Exception ex)
            {
                LogMsg("connect error: " + ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            try { _rxCts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null; _client = null;
            lock (_pending)
            {
                while (_pending.Count > 0) _pending.Dequeue().TrySetCanceled();
            }
            RaiseConn(false);
            LogMsg("disconnected");
        }

        /// <summary>라인 1개 전송 후 라인 1개 응답 대기.</summary>
        public async Task<string> SendAsync(string line, int timeoutMs = 5000)
        {
            if (!IsConnected) throw new InvalidOperationException("not connected");
            var tcs = new TaskCompletionSource<string>();
            lock (_pending) _pending.Enqueue(tcs);

            var data = Encoding.UTF8.GetBytes(line + "\n");
            try
            {
                lock (_ioLock) _stream.Write(data, 0, data.Length);
                LogMsg("TX: " + line);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                Disconnect();
            }

            if (await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)) != tcs.Task)
            {
                lock (_pending) if (_pending.Count > 0) _pending.Dequeue();
                throw new TimeoutException("vision response timeout: " + line);
            }
            return await tcs.Task;
        }

        // ─── High-level helpers ──────────────────────

        public async Task<bool> PingAsync()
        {
            try
            {
                var r = await SendAsync($"{ModuleName}|PING");
                return r.StartsWith("ACK|");
            }
            catch { return false; }
        }

        public async Task<bool> ExposeAsync(int index = 0, int timeoutMs = 5000)
        {
            var r = await SendAsync($"{ModuleName}|EXPOSE|{index}", timeoutMs);
            return r.StartsWith("ACK|");
        }

        public async Task<MatchResultDto> MatchAsync(string finder, int index = 0, int timeoutMs = 5000)
        {
            var r = await SendAsync($"{ModuleName}|MATCH|{finder}|{index}", timeoutMs);
            return MatchResultDto.Parse(r);
        }

        public async Task<InspectionResultDto> InspectAsync(string inspector, int index = 0, int timeoutMs = 5000)
        {
            var r = await SendAsync($"{ModuleName}|INSPECT|{inspector}|{index}", timeoutMs);
            return InspectionResultDto.Parse(r);
        }

        public async Task<bool> TrainAsync(string finder, int timeoutMs = 5000)
        {
            var r = await SendAsync($"{ModuleName}|TRAIN|{finder}|0", timeoutMs);
            return r.StartsWith("ACK|");
        }

        // ─── Receive loop ────────────────────────────

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && _client != null && _client.Connected)
                {
                    int n = await _stream.ReadAsync(buf, 0, buf.Length, ct);
                    if (n == 0) break;
                    _rxBuf.Append(Encoding.UTF8.GetString(buf, 0, n));
                    string all = _rxBuf.ToString();
                    int idx;
                    while ((idx = all.IndexOfAny(new[] { '\n', '\r' })) >= 0)
                    {
                        var line = all.Substring(0, idx).Trim();
                        all = all.Substring(idx + 1);
                        if (line.Length > 0)
                        {
                            LogMsg("RX: " + line);
                            // 비동기 푸시 — EPD/ARM 은 응답 큐와 무관.
                            if (line.StartsWith("EPD|"))
                            {
                                var pp = line.Split('|');
                                try { ExposureDone?.Invoke(pp.Length > 1 ? pp[1] : ModuleName); } catch { }
                                continue;
                            }
                            if (line.StartsWith("ARM|"))
                            {
                                var pp = line.Split('|');
                                try { Alarmed?.Invoke(pp.Length > 1 ? pp[1] : ModuleName,
                                                      pp.Length > 2 ? pp[2] : ""); } catch { }
                                continue;
                            }
                            TaskCompletionSource<string> tcs = null;
                            lock (_pending) if (_pending.Count > 0) tcs = _pending.Dequeue();
                            tcs?.TrySetResult(line);
                        }
                    }
                    _rxBuf.Clear(); _rxBuf.Append(all);
                }
            }
            catch { }
            finally { Disconnect(); }
        }

        private void RaiseConn(bool on) { try { ConnectionChanged?.Invoke(on); } catch { } }
        private void LogMsg(string s)    { try { Log?.Invoke($"[{ModuleName}:{Port}] {s}"); } catch { } }

        public void Dispose() => Disconnect();
    }

    // ─────────────────────────────────────────────
    //  응답 DTO
    // ─────────────────────────────────────────────

    public class MatchResultDto
    {
        public bool   Success  { get; set; }
        public double X        { get; set; }
        public double Y        { get; set; }
        public double AngleDeg { get; set; }
        public double Score    { get; set; }
        public string RawError { get; set; }

        public static MatchResultDto Parse(string line)
        {
            // "ACK|WaferVision|MATCH|OK;x=320.1;y=239.7;r=0.12;score=0.93"
            var r = new MatchResultDto();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("ACK|")) { r.RawError = line; return r; }
            var parts = line.Split('|');
            if (parts.Length < 4) { r.RawError = line; return r; }
            var body = parts[3];
            var kv = body.Split(';');
            if (kv.Length > 0 && kv[0] == "OK") r.Success = true;
            foreach (var s in kv)
            {
                var eq = s.IndexOf('=');
                if (eq <= 0) continue;
                string k = s.Substring(0, eq), v = s.Substring(eq + 1);
                switch (k)
                {
                    case "x":     double.TryParse(v, out var xx);  r.X        = xx; break;
                    case "y":     double.TryParse(v, out var yy);  r.Y        = yy; break;
                    case "r":     double.TryParse(v, out var rr);  r.AngleDeg = rr; break;
                    case "score": double.TryParse(v, out var ss);  r.Score    = ss; break;
                }
            }
            return r;
        }
    }

    public class InspectionResultDto
    {
        public bool   IsPass   { get; set; }
        public string Raw      { get; set; }
        public static InspectionResultDto Parse(string line)
        {
            var r = new InspectionResultDto { Raw = line };
            if (string.IsNullOrEmpty(line) || !line.StartsWith("ACK|")) return r;
            var parts = line.Split('|');
            if (parts.Length < 4) return r;
            r.IsPass = parts[3].StartsWith("PASS");
            return r;
        }
    }
}
