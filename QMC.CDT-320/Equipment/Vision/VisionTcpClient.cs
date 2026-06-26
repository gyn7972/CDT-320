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
        private const int MatchResultPollIntervalMs = 100;

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
        private readonly SemaphoreSlim _commandGate = new SemaphoreSlim(1, 1);
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

            await _commandGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
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
            finally
            {
                _commandGate.Release();
            }
        }

        public async Task<VisionProtocolResponse> SendCommandAsync(VisionProtocolCommand command, int timeoutMs, CancellationToken ct, params object[] arguments)
        {
            VisionProtocolMessage message = VisionProtocolMessage.Create(ModuleName, command, arguments);
            string response = await SendAsync(message.ToLine(), timeoutMs, ct).ConfigureAwait(false);
            return VisionProtocolResponse.Parse(response);
        }

        public async Task<VisionProtocolResponse> SendCommandAsync(string command, int timeoutMs, CancellationToken ct, params object[] arguments)
        {
            VisionProtocolMessage message = VisionProtocolMessage.Create(ModuleName, command, arguments);
            string response = await SendAsync(message.ToLine(), timeoutMs, ct).ConfigureAwait(false);
            return VisionProtocolResponse.Parse(response);
        }

        // ─── High-level helpers ──────────────────────

        public async Task<bool> PingAsync()
        {
            try
            {
                VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Ping, 5000, CancellationToken.None).ConfigureAwait(false);
                return response.IsAck;
            }
            catch { return false; }
        }

        public async Task<bool> ExposeAsync(int index = 0, int timeoutMs = 5000)
        {
            return await ExposeAsync(index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> ExposeAsync(int index, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Expose, timeoutMs, ct, index).ConfigureAwait(false);
            return response.IsAck;
        }

        public async Task<bool> GrabAsync(int index = 0, int timeoutMs = 5000)
        {
            return await GrabAsync(index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> GrabAsync(int index, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Grab, timeoutMs, ct, index).ConfigureAwait(false);
            return response.IsAck;
        }

        public async Task<MatchResultDto> MatchAsync(string finder, int index = 0, int timeoutMs = 5000)
        {
            return await MatchAsync(finder, index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<MatchResultDto> MatchAsync(string finder, int index, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool started = await MatchAsyncStartAsync(finder, index, timeoutMs, ct).ConfigureAwait(false);
            if (!started)
            {
                return new MatchResultDto
                {
                    Success = false,
                    RawError = "MATCHASYNC STARTED ACK failed. finder=" + finder
                };
            }

            DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < timeoutAt)
            {
                ct.ThrowIfCancellationRequested();

                int remainMs = (int)Math.Max(1, (timeoutAt - DateTime.UtcNow).TotalMilliseconds);
                int pollTimeoutMs = Math.Min(1000, remainMs);
                AsyncMatchPoll poll = await PollMatchResultAsync(finder, pollTimeoutMs, ct).ConfigureAwait(false);
                if (poll.Error)
                {
                    return new MatchResultDto
                    {
                        Success = false,
                        RawError = poll.Raw
                    };
                }

                if (poll.Done)
                {
                    if (poll.Result != null)
                        return poll.Result;

                    return new MatchResultDto
                    {
                        Success = false,
                        RawError = poll.Raw
                    };
                }

                await Task.Delay(MatchResultPollIntervalMs, ct).ConfigureAwait(false);
            }

            return new MatchResultDto
            {
                Success = false,
                RawError = "MATCHRESULT timeout. finder=" + finder + ", timeoutMs=" + timeoutMs
            };
        }

        // ── 비동기 매칭 (MATCHASYNC 1차 ACK=STARTED → 백그라운드 알고리즘 → MATCHRESULT 폴링) ──
        /// <summary>비동기 매칭 시작 — 그랩 완료(1차 ACK=STARTED) 면 true. 핸들러는 이후 다음 스텝 진행 +
        /// <see cref="PollMatchResultAsync"/> 로 알고리즘 완료를 폴링한다.</summary>
        public async Task<bool> MatchAsyncStartAsync(string finder, int index = 0, int timeoutMs = 30000)
        {
            return await MatchAsyncStartAsync(finder, index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>비동기 매칭 시작 — STARTED ACK만 확인하고 즉시 반환한다.</summary>
        public async Task<bool> MatchAsyncStartAsync(string finder, int index, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.MatchAsync, timeoutMs, ct, finder, index).ConfigureAwait(false);
            return response.IsAck && response.IsResult("STARTED");
        }

        /// <summary>비동기 매칭 결과 폴링 — Done=false 면 아직 진행 중(반복 폴링), Done=true 면 Result 에 데이터.</summary>
        public async Task<AsyncMatchPoll> PollMatchResultAsync(string finder, int timeoutMs = 5000)
        {
            return await PollMatchResultAsync(finder, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>비동기 매칭 결과 폴링 — Done=false 면 아직 진행 중(반복 폴링), Done=true 면 Result 에 데이터.</summary>
        public async Task<AsyncMatchPoll> PollMatchResultAsync(string finder, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.MatchResult, timeoutMs, ct, finder).ConfigureAwait(false);
            return AsyncMatchPoll.Parse(response.RawLine);
        }

        // ── 비동기 INSPECT (그랩 후 1차 ACK=STARTED → 백그라운드 검사 → INSPECTRESULT 폴링) ──
        /// <summary>비동기 검사 시작 — 그랩 완료(1차 ACK=STARTED) 면 true. 이후 PollInspectResultAsync 로 완료 폴링.</summary>
        public async Task<bool> InspectAsyncStartAsync(string inspector, int index = 0, int timeoutMs = 30000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.InspectAsync, timeoutMs, CancellationToken.None, inspector, index).ConfigureAwait(false);
            return response.IsAck;
        }

        /// <summary>비동기 검사 결과 폴링 — Done=false 면 진행 중(반복 폴링), Done=true 면 Result(PASS/FAIL).</summary>
        public async Task<AsyncInspectPoll> PollInspectResultAsync(string inspector, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.InspectResult, timeoutMs, CancellationToken.None, inspector).ConfigureAwait(false);
            return AsyncInspectPoll.Parse(response.RawLine);
        }

        public async Task<InspectionResultDto> InspectAsync(string inspector, int index = 0, int timeoutMs = 5000)
        {
            return await InspectAsync(inspector, index, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<InspectionResultDto> InspectAsync(string inspector, int index, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Inspect, timeoutMs, ct, inspector, index).ConfigureAwait(false);
            return InspectionResultDto.Parse(response.RawLine);
        }

        public async Task<bool> TrainAsync(string finder, int timeoutMs = 5000)
        {
            return await TrainAsync(finder, timeoutMs, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> TrainAsync(string finder, int timeoutMs, CancellationToken ct)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Train, timeoutMs, ct, finder).ConfigureAwait(false);
            return response.IsAck;
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

            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Recipe, timeoutMs, ct, recipeNo, recipeName).ConfigureAwait(false);
            return response.IsAck;
        }

        public async Task<VisionScaleResult> ScaleAsync(double chipWidthMm, double chipHeightMm, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Scale, timeoutMs, CancellationToken.None, chipWidthMm, chipHeightMm).ConfigureAwait(false);
            return VisionScaleResult.Parse(response.RawLine);
        }

        public async Task<VisionRotationCenterResult> RotationCenterAsync(int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.RotationCenter, timeoutMs, CancellationToken.None).ConfigureAwait(false);
            return VisionRotationCenterResult.Parse(response.RawLine);
        }

        public async Task<bool> DistortAsync(int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.Distort, timeoutMs, CancellationToken.None).ConfigureAwait(false);
            return response.IsAck;
        }

        public async Task<VisionCameraSwitchResult> SwitchCameraAsync(string tool, bool liveOn, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.CameraSwitch, timeoutMs, CancellationToken.None, tool, liveOn ? 1 : 0).ConfigureAwait(false);
            return VisionCameraSwitchResult.Parse(response.RawLine);
        }

        public async Task<VisionFocusStartResult> FocusStartAsync(string camera, string target, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.FocusStart, timeoutMs, CancellationToken.None, camera, target).ConfigureAwait(false);
            return VisionFocusStartResult.Parse(response.RawLine);
        }

        public async Task<VisionFocusValueResult> FocusValueAsync(double motorZ, string camera, string target, int pickupNo, bool initial, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.FocusValue, timeoutMs, CancellationToken.None, motorZ, camera, target, pickupNo, initial ? 1 : 0).ConfigureAwait(false);
            return VisionFocusValueResult.Parse(response.RawLine);
        }

        public async Task<VisionFocusBestResult> FocusBestAsync(string camera, string target, int pickupNo = 0, int timeoutMs = 5000)
        {
            VisionProtocolResponse response = await SendCommandAsync(VisionProtocolCommand.FocusBest, timeoutMs, CancellationToken.None, camera, target, pickupNo).ConfigureAwait(false);
            return VisionFocusBestResult.Parse(response.RawLine);
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
                            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
                            // 비동기 푸시 — FPD/EPD/ARM/RECIPEREQ는 응답 큐와 무관하다.
                            if (response.IsPush &&
                                (string.Equals(response.Command, VisionProtocolPushCommands.ExposureDone, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(response.Command, VisionProtocolPushCommands.LegacyExposureDone, StringComparison.OrdinalIgnoreCase)))
                            {
                                try { ExposureDone?.Invoke(!string.IsNullOrWhiteSpace(response.Module) ? response.Module : ModuleName); } catch { }
                                continue;
                            }
                            if (response.IsPush &&
                                string.Equals(response.Command, VisionProtocolPushCommands.Alarm, StringComparison.OrdinalIgnoreCase))
                            {
                                try { Alarmed?.Invoke(!string.IsNullOrWhiteSpace(response.Module) ? response.Module : ModuleName,
                                                      response.Payload); } catch { }
                                continue;
                            }
                            // Vision → 핸들러 레시피 요청. 응답 큐와 무관. 구독측이 현재 레시피를 SendRecipeAsync 로 응답.
                            if (response.IsPush &&
                                string.Equals(response.Command, VisionProtocolPushCommands.RecipeRequest, StringComparison.OrdinalIgnoreCase))
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
        public bool   HasImageSize { get; set; }
        public double ImageWidthPixel { get; set; }
        public double ImageHeightPixel { get; set; }
        public string RawError { get; set; }

        public static MatchResultDto Parse(string line)
        {
            // "ACK|WaferVision|MATCHRESULT|1;x=..." 완료 응답의 x/y/r/score 본문을 OK 형식으로 정규화해서 파싱한다.
            var r = new MatchResultDto();
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            if (!response.IsAck)
            {
                r.RawError = response.ErrorMessage.Length > 0 ? response.ErrorMessage : line;
                return r;
            }

            r.Success = response.IsResult("OK");
            r.RawError = response.ErrorMessage;
            response.TryGetDouble("x", out var x);
            response.TryGetDouble("y", out var y);
            response.TryGetDouble("r", out var angle);
            if (angle == 0)
                response.TryGetDouble("t", out angle);
            if (angle == 0)
                response.TryGetDouble("theta", out angle);
            response.TryGetDouble("score", out var score);

            r.X = x;
            r.Y = y;
            r.AngleDeg = angle;
            r.Score = score;
            ApplyImageSize(response, r);
            return r;
        }

        private static void ApplyImageSize(VisionProtocolResponse response, MatchResultDto result)
        {
            double width;
            double height;
            if (!TryGetImageSize(response, out width, out height))
                return;

            result.ImageWidthPixel = width;
            result.ImageHeightPixel = height;
            result.HasImageSize = true;
        }

        private static bool TryGetImageSize(VisionProtocolResponse response, out double width, out double height)
        {
            width = 0;
            height = 0;
            if (response == null)
                return false;

            if (!response.TryGetDouble("width", out width) &&
                !response.TryGetDouble("w", out width) &&
                !response.TryGetDouble("imagew", out width) &&
                !response.TryGetDouble("imagewidth", out width) &&
                !response.TryGetDouble("image_width", out width) &&
                !response.TryGetDouble("resolutionx", out width) &&
                !response.TryGetDouble("resx", out width))
                return false;

            if (!response.TryGetDouble("height", out height) &&
                !response.TryGetDouble("h", out height) &&
                !response.TryGetDouble("imageh", out height) &&
                !response.TryGetDouble("imageheight", out height) &&
                !response.TryGetDouble("image_height", out height) &&
                !response.TryGetDouble("resolutiony", out height) &&
                !response.TryGetDouble("resy", out height))
                return false;

            return width > 0 && height > 0;
        }
    }

    /// <summary>비동기 매칭 폴링(MATCHRESULT) 결과 — Done/Error 플래그 + 완료 시 데이터.</summary>
    public class AsyncMatchPoll
    {
        public bool          Done;       // 알고리즘 완료(=1)
        public bool          Error;      // 실패(ERR) 또는 비정상 응답
        public string        Raw;        // 원문
        public MatchResultDto Result;    // Done 일 때 x/y/angle/score

        public static AsyncMatchPoll Parse(string line)
        {
            var p = new AsyncMatchPoll { Raw = line };
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            if (!response.IsAck) { p.Error = true; return p; }
            string payload = response.Payload;   // "0" / "1;x=..;y=..;r=..;score=.." / "ERR;사유"
            if (payload.StartsWith("1", StringComparison.OrdinalIgnoreCase))
            {
                p.Done = true;
                int semi = payload.IndexOf(';');
                string body = semi >= 0 ? payload.Substring(semi + 1) : "";
                p.Result = MatchResultDto.Parse("ACK|x|MATCHRESULT|OK;" + body);   // 'OK;본문' 으로 변환해 재사용
            }
            else if (payload.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            {
                p.Error = true;
            }
            // 그 외("0") → 진행 중: Done=false, Error=false
            return p;
        }
    }

    /// <summary>비동기 검사 폴링(INSPECTRESULT) 결과 — Done/Error + 완료 시 InspectionResultDto.</summary>
    public class AsyncInspectPoll
    {
        public bool                Done;       // 검사 완료(=1)
        public bool                Error;      // 실패(ERR) 또는 비정상 응답
        public string              Raw;        // 원문
        public InspectionResultDto Result;     // Done 일 때 PASS/FAIL + 항목

        public static AsyncInspectPoll Parse(string line)
        {
            var p = new AsyncInspectPoll { Raw = line };
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            if (!response.IsAck) { p.Error = true; return p; }
            string payload = response.Payload;   // "0" / "1;PASS;.." / "ERR;사유"
            if (payload.StartsWith("1", StringComparison.OrdinalIgnoreCase))
            {
                p.Done = true;
                int semi = payload.IndexOf(';');
                string body = semi >= 0 ? payload.Substring(semi + 1) : "";   // "PASS;.." / "FAIL;.."
                p.Result = InspectionResultDto.Parse("ACK|x|INSPECT|" + body);
            }
            else if (payload.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            {
                p.Error = true;
            }
            return p;
        }
    }

    public class InspectionResultDto
    {
        // INSPECT 응답 예:
        // ACK|Bin|INSPECT|PASS;x=320;y=240;t=0.010;score=0.98;width=640;height=480
        // x/y는 Vision PC의 pixel 좌표이며 Handler에서 mm로 변환해서 사용한다.
        public bool   IsPass    { get; set; }
        public bool   HasOffset { get; set; }
        public double OffsetX   { get; set; }
        public double OffsetY   { get; set; }
        public double OffsetT   { get; set; }
        public double Score     { get; set; }
        public bool   HasImageSize { get; set; }
        public double ImageWidthPixel { get; set; }
        public double ImageHeightPixel { get; set; }
        public string Raw       { get; set; }

        public static InspectionResultDto Parse(string line)
        {
            var r = new InspectionResultDto { Raw = line };
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            if (!response.IsAck)
                return r;

            string result = response.ResultToken;
            r.IsPass =
                string.Equals(result, "PASS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase);

            double parsed;
            if (TryGetAny(response, out parsed, "x", "dx", "offsetx", "offset_x"))
            {
                r.OffsetX = parsed;
                r.HasOffset = true;
            }

            if (TryGetAny(response, out parsed, "y", "dy", "offsety", "offset_y"))
            {
                r.OffsetY = parsed;
                r.HasOffset = true;
            }

            if (TryGetAny(response, out parsed, "r", "t", "dt", "theta", "offsett", "offset_t"))
            {
                r.OffsetT = parsed;
                r.HasOffset = true;
            }

            if (response.TryGetDouble("score", out parsed))
                r.Score = parsed;

            double width;
            double height;
            if (TryGetImageSize(response, out width, out height))
            {
                r.ImageWidthPixel = width;
                r.ImageHeightPixel = height;
                r.HasImageSize = true;
            }

            return r;
        }

        private static bool TryGetAny(VisionProtocolResponse response, out double value, params string[] keys)
        {
            value = 0;
            if (response == null || keys == null)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (response.TryGetDouble(keys[i], out value))
                    return true;
            }

            return false;
        }

        private static bool TryGetImageSize(VisionProtocolResponse response, out double width, out double height)
        {
            width = 0;
            height = 0;
            if (response == null)
                return false;

            if (!TryGetAny(response, out width, "width", "w", "imagew", "imagewidth", "image_width", "resolutionx", "resx"))
                return false;

            if (!TryGetAny(response, out height, "height", "h", "imageh", "imageheight", "image_height", "resolutiony", "resy"))
                return false;

            return width > 0 && height > 0;
        }
    }
}
