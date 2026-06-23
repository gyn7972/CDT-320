using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>마지막으로 라인을 수신한 UTC 시각(워치독/RX 표시용). 미수신이면 default.</summary>
        public DateTime LastRxUtc { get; private set; }

        // TcpClient.Connected 는 내부 소켓 상태 접근 시 폐기 경쟁으로 예외를 던질 수 있어, 자체 플래그로 관리한다.
        // 연결 성공 시 true, Disconnect/오류 시 false. (ReceiveLoop 가 끊김 감지 시 Disconnect 호출)
        private volatile bool _connected;
        public bool IsConnected => _connected;

        public event Action<bool>   ConnectionChanged;
        public event Action<string> Log;
        /// <summary>비동기 푸시 — Vision 측 EPD (Exposure Done) 수신.</summary>
        public event Action<string> ExposureDone;
        /// <summary>비동기 푸시 — Vision 측 ARM (Alarm) 수신. arg=reason.</summary>
        public event Action<string, string> Alarmed;
        /// <summary>비동기 푸시 — Vision 측이 "현재 레시피 알려줘"(RECIPEREQ) 요청. 핸들러는 현재 레시피를 SendRecipeAsync 로 응답해야 한다.</summary>
        public event Action RecipeRequested;

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
            return await ConnectAsync(timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ConnectAsync(int timeoutMs, CancellationToken ct)
        {
            if (IsConnected) return true;
            try
            {
                ct.ThrowIfCancellationRequested();
                // 로컬 변수로 다뤄, 연결 도중 Disconnect/Dispose 가 _client 를 null 로 바꿔도 NRE 가 나지 않게 한다.
                var client = new TcpClient();
                _client = client;
                var task = client.ConnectAsync(Host, Port);
                if (await Task.WhenAny(task, Task.Delay(timeoutMs, ct)).ConfigureAwait(false) != task)
                {
                    try { client.Close(); } catch { }
                    if (ReferenceEquals(_client, client)) _client = null;
                    LogMsg("connect timeout");
                    return false;
                }
                ct.ThrowIfCancellationRequested();
                // 대기 중 동시 Disconnect/Dispose 로 교체·해제됐으면 중단(이 연결은 폐기).
                if (!ReferenceEquals(_client, client) || !client.Connected)
                {
                    try { client.Close(); } catch { }
                    LogMsg("connect aborted (disposed)");
                    return false;
                }
                _stream = client.GetStream();
                _rxCts  = new CancellationTokenSource();
                _connected = true;
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
            finally
            {
            }
        }

        public void Disconnect()
        {
            _connected = false;
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
            return await SendAsync(line, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<string> SendAsync(string line, int timeoutMs, CancellationToken ct)
        {
            if (!IsConnected) throw new InvalidOperationException("not connected");
            ct.ThrowIfCancellationRequested();
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

            if (await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, ct)).ConfigureAwait(false) != tcs.Task)
            {
                lock (_pending) if (_pending.Count > 0) _pending.Dequeue();
                ct.ThrowIfCancellationRequested();
                throw new TimeoutException("vision response timeout: " + line);
            }
            return await tcs.Task.ConfigureAwait(false);
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
            return await ExposeAsync(index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ExposeAsync(int index, int timeoutMs, CancellationToken ct)
        {
            var r = await SendAsync($"{ModuleName}|EXPOSE|{index}", timeoutMs, ct).ConfigureAwait(false);
            return r.StartsWith("ACK|");
        }

        public async Task<MatchResultDto> MatchAsync(string finder, int index = 0, int timeoutMs = 5000)
        {
            return await MatchAsync(finder, index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<MatchResultDto> MatchAsync(string finder, int index, int timeoutMs, CancellationToken ct)
        {
            var r = await SendAsync($"{ModuleName}|MATCH|{finder}|{index}", timeoutMs, ct).ConfigureAwait(false);
            return MatchResultDto.Parse(r);
        }

        public async Task<InspectionResultDto> InspectAsync(string inspector, int index = 0, int timeoutMs = 5000)
        {
            return await InspectAsync(inspector, index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<InspectionResultDto> InspectAsync(string inspector, int index, int timeoutMs, CancellationToken ct)
        {
            var r = await SendAsync($"{ModuleName}|INSPECT|{inspector}|{index}", timeoutMs, ct).ConfigureAwait(false);
            return InspectionResultDto.Parse(r);
        }

        public async Task<bool> TrainAsync(string finder, int timeoutMs = 5000)
        {
            return await TrainAsync(finder, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> TrainAsync(string finder, int timeoutMs, CancellationToken ct)
        {
            var r = await SendAsync($"{ModuleName}|TRAIN|{finder}|0", timeoutMs, ct).ConfigureAwait(false);
            return r.StartsWith("ACK|");
        }

        /// <summary>
        /// 전역 레시피 변경 통보. 핸들러에서 활성 레시피가 바뀌면 Vision 측이
        /// 같은 이름의 로컬 레시피로 자동 전환하도록 "MODULE|RECIPE|번호|명칭"을 전송한다.
        /// Vision 측 응답이 ACK 이면 true 를 반환한다.
        /// </summary>
        public async Task<bool> SendRecipeAsync(int recipeNo, string recipeName, int timeoutMs = 5000)
        {
            return await SendRecipeAsync(recipeNo, recipeName, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> SendRecipeAsync(int recipeNo, string recipeName, int timeoutMs, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                throw new ArgumentException("recipeName is empty", nameof(recipeName));

            var r = await SendAsync($"{ModuleName}|RECIPE|{recipeNo}|{recipeName}", timeoutMs, ct).ConfigureAwait(false);
            return r.StartsWith("ACK|");
        }

        // ─── Receive loop ────────────────────────────

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    var stream = _stream;
                    if (stream == null) break;   // 동시 Disconnect 로 스트림이 사라지면 종료
                    int n = await stream.ReadAsync(buf, 0, buf.Length, ct);
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
                            LastRxUtc = DateTime.UtcNow;
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
                            // Vision → 핸들러 레시피 요청. 응답 큐와 무관. 구독측이 현재 레시피를 SendRecipeAsync 로 응답.
                            if (line.StartsWith("RECIPEREQ|"))
                            {
                                try { RecipeRequested?.Invoke(); } catch { }
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
                    // Vision 결과 X 좌표 파싱
                    case "x":     double.TryParse(v, out var xx);  r.X        = xx; break;
                    // Vision 결과 Y 좌표 파싱
                    case "y":     double.TryParse(v, out var yy);  r.Y        = yy; break;
                    // Vision 결과 회전각 파싱
                    case "r":     double.TryParse(v, out var rr);  r.AngleDeg = rr; break;
                    // Vision 결과 Score 파싱
                    case "score": double.TryParse(v, out var ss);  r.Score    = ss; break;
                }
            }
            return r;
        }
    }

    public class InspectionResultDto
    {
        // INSPECT 응답 예:
        // ACK|Bin|INSPECT|PASS;x=0.001;y=-0.002;t=0.010;score=0.98
        // x/y/t는 위치 보정 파라미터가 아니라 Vision이 측정한 die offset으로 저장한다.
        public bool   IsPass    { get; set; }
        public bool   HasOffset { get; set; }
        public double OffsetX   { get; set; }
        public double OffsetY   { get; set; }
        public double OffsetT   { get; set; }
        public double Score     { get; set; }
        public string Raw       { get; set; }

        public static InspectionResultDto Parse(string line)
        {
            var r = new InspectionResultDto { Raw = line };
            if (string.IsNullOrEmpty(line) || !line.StartsWith("ACK|")) return r;
            var parts = line.Split('|');
            if (parts.Length < 4) return r;

            var tokens = parts[3].Split(';');
            if (tokens.Length == 0) return r;

            string result = tokens[0].Trim();
            r.IsPass =
                string.Equals(result, "PASS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase);

            foreach (var token in tokens)
            {
                var eq = token.IndexOf('=');
                if (eq <= 0) continue;

                string key = token.Substring(0, eq).Trim();
                string value = token.Substring(eq + 1).Trim();
                double parsed;
                if (!TryParseDouble(value, out parsed))
                    continue;

                switch (key.ToLowerInvariant())
                {
                    case "x":
                    case "dx":
                    case "offsetx":
                    case "offset_x":
                        r.OffsetX = parsed;
                        r.HasOffset = true;
                        break;
                    case "y":
                    case "dy":
                    case "offsety":
                    case "offset_y":
                        r.OffsetY = parsed;
                        r.HasOffset = true;
                        break;
                    case "r":
                    case "t":
                    case "dt":
                    case "theta":
                    case "offsett":
                    case "offset_t":
                        r.OffsetT = parsed;
                        r.HasOffset = true;
                        break;
                    case "score":
                        r.Score = parsed;
                        break;
                }
            }

            return r;
        }

        private static bool TryParseDouble(string value, out double result)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return true;

            return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }
    }
}
