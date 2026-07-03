using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;

namespace QMC.Vision.Comm
{
    /// <summary>
    /// 메인 제어 프로그램이 접속하는 비전 TCP 서버.
    /// <para>
    /// 프로토콜 (line-delimited, UTF-8):
    ///   요청:  "MODULE|CMD|arg1|arg2|..."
    ///   응답:  "ACK|MODULE|CMD|result"   또는  "ERR|MODULE|CMD|msg"
    /// </para>
    /// <para>지원 명령:</para>
    /// <list type="bullet">
    ///   <item>PING — 연결 확인</item>
    ///   <item>EXPOSE / GRAB — 1장 그랩</item>
    ///   <item>MATCH &lt;finder&gt; [chipUid] — 패턴 매칭. ReturnMmCoordinates=true 면 mm 좌표.</item>
    ///   <item>INSPECT &lt;inspector&gt; [chipUid] — 외관/배치 검사. MaterialTracker / DataLog 자동 누적.</item>
    ///   <item>TRAIN &lt;finder&gt; — 패턴 학습</item>
    ///   <item>SCALE &lt;chipWmm&gt; &lt;chipHmm&gt; — VisionScale 자동 캘리브레이션</item>
    ///   <item>ROT_CENTER — 회전 중심 corners 측정</item>
    ///   <item>DISTORT — 왜곡 보정 학습</item>
    ///   <item>CAM_SWITCH &lt;toolName&gt; &lt;liveOnOff&gt; — 멀티카메라 전환</item>
    ///   <item>FOCUS_VAL — 4 ROI 별 포커스 값</item>
    /// </list>
    /// <para>비동기 푸시 (Vision → Handler):</para>
    /// <list type="bullet">
    ///   <item>"EPD|MODULE" — Exposure Done</item>
    ///   <item>"ARM|MODULE|reason" — 알람</item>
    /// </list>
    /// </summary>
    public class VisionTcpServer : IDisposable
    {
        public event Action<string> Log;

        /// <summary>명령 수락 게이트 — null 이면 항상 허용. false 면 PING 외 명령을 거부(RUN 아닐 때).</summary>
        public Func<bool> IsCommandAllowed { get; set; }

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly Dictionary<string, IVisionModule> _modules = new Dictionary<string, IVisionModule>(StringComparer.OrdinalIgnoreCase);
        private VisionSettings _cfg;
        private readonly object _grabGate = new object();   // 백그라운드 그랩 직렬화 — 실기 카메라 동시 그랩 방지 + 픽커 순서 보존

        public int  Port      { get; }
        public bool IsRunning { get; private set; }
        public string ModuleName { get; }
        public IVisionModule Module { get; }

        /// <summary>핸들러 등 클라이언트가 1개 이상 접속해 있으면 true(통신 상태 표시용).</summary>
        public bool HasClient { get { lock (_clients) { return _clients.Count > 0; } } }

        /// <summary>마지막으로 명령 라인을 수신한 시각(UTC). 워치독/최근수신 표시용. 미수신 시 default.</summary>
        public DateTime LastRxUtc { get; private set; }

        /// <summary>1 포트 = 1 모듈 전담 (매뉴얼 기준).</summary>
        public VisionTcpServer(IVisionModule module, int port)
        {
            Module     = module;
            ModuleName = module.Name;
            Port       = port;
            _modules[module.Name] = module;

            // 비동기 이벤트 → Broadcast
            module.ExposureDone += OnExposureDone;
            module.Alarmed      += OnAlarmed;

            _cfg = VisionConfigStore.Current ?? new VisionSettings();
            // DelayBeforeGrabMs SSOT = 모듈 CameraConfig(ApplyCameraSettings 가 설정). 전역 vision.json 덮어쓰기 제거.
        }

        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            IsRunning = true;
            LogMsg($"[{ModuleName}:{Port}] listening");
            _ = Task.Run(() => AcceptLoop(_cts.Token));
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            lock (_clients) { foreach (var c in _clients.ToList()) try { c.Close(); } catch { } _clients.Clear(); }
            IsRunning = false;
            LogMsg($"[{ModuleName}:{Port}] stopped");
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    lock (_clients) _clients.Add(client);
                    LogMsg($"[{ModuleName}:{Port}] client connected: {client.Client.RemoteEndPoint}");
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
                LogMsg($"[{ModuleName}:{Port}] client disconnected");
            }
        }

        private void ProcessLine(NetworkStream stream, string line)
        {
            LastRxUtc = DateTime.UtcNow;
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";
            // 결과 폴링(RESULT 계열)은 진행중("0") 응답이 초당 수 회 반복돼 로그 홍수 → 완료/실패 때만 RX/TX 기록.
            bool isPollCmd = (cmd == "INSPECTRESULT" || cmd == "MATCHRESULT");
            if (!isPollCmd) LogMsg($"[{ModuleName}] RX: {line}");

            if (!_modules.TryGetValue(mod, out var m))
            {
                Send(stream, $"ERR|{mod}|{cmd}|unknown module");
                return;
            }
            // RUN 게이트 — RUN 상태가 아니면 명령 거부. 단, PING(상태확인)과 단발 그랩(EXPOSE/GRAB)은 면제:
            // 단발 그랩은 모션을 유발하지 않는 카메라 촬상이라 셋업/수동 테스트를 위해 RUN 아닐 때도 허용한다.
            if (!IsGateExemptCommand(cmd) && IsCommandAllowed != null && !IsCommandAllowed())
            {
                Send(stream, $"ERR|{mod}|{cmd}|not running (press RUN)");
                return;
            }

            try
            {
                string resp = string.Empty;
                string echo = ResolveEchoToken(cmd, parts);

                bool isAsyncStart = (cmd == "MATCHASYNC" || cmd == "INSPECTASYNC");

                // ── 1단계: 비동기 시작은 STARTED 를 "그랩 전에" 먼저 ──
                switch (cmd)
                {
                    case "MATCHASYNC":
                    case "INSPECTASYNC":
                        resp = "STARTED";
                        break;   // 1차 ACK(STARTED) 후 백그라운드 알고리즘/검사
                }

                if (isAsyncStart)                       // ★ 비동기 시작만 여기서 ACK
                    Send(stream, string.IsNullOrEmpty(echo)
                        ? $"ACK|{mod}|{cmd}|{resp}"
                        : $"ACK|{mod}|{cmd}|{echo}|{resp}");

                // ── 2단계: 실제 작업 (비동기=백그라운드 그랩+알고리즘 시작 / 동기·폴링=결과 계산) ──
                switch (cmd)
                {
                    case "PING": resp = "OK"; break;
                    case "EXPOSE":
                    case "GRAB": resp = DoExpose(m); break;
                    case "MATCH": resp = DoMatch(m, parts); break;
                    case "MATCHASYNC": resp = DoMatchAsync(m, parts); break;   // 그랩+알고리즘 백그라운드 (ACK는 1단계에서 이미 보냄)
                    case "MATCHRESULT": resp = DoMatchResult(m, parts); break;  // 폴링: 0/1;data/ERR
                    case "INSPECT": resp = DoInspect(m, parts); break;
                    case "INSPECTASYNC": resp = DoInspectAsync(m, parts); break;   // 그랩+검사 백그라운드 (ACK는 1단계)
                    case "INSPECTRESULT": resp = DoInspectResult(m, parts); break;  // 폴링: 0/1;PASS|FAIL../ERR
                    case "TRAIN": resp = DoTrain(m, parts); break;
                    case "SCALE": resp = DoScale(m, parts); break;
                    case "ROT_CENTER": resp = DoRotCenter(m); break;
                    case "DISTORT": resp = DoDistort(m); break;
                    case "CAM_SWITCH": resp = DoCamSwitch(m, parts); break;
                    case "CAM_SETTING":resp = DoCameraSetting(m); break;
                    case "FOCUS_START":resp = VisionCommandCore.FocusStart(parts); break;
                    case "FOCUS_VAL":  resp = VisionCommandCore.FocusValue(m, parts); break;
                    case "FOCUS_BEST": resp = VisionCommandCore.FocusBest(parts); break;
                    default: resp = null; break;
                }

                // ── 3단계: 동기/폴링 결과 ACK (비동기 시작은 1단계에서 이미 보냈으므로 제외) ──
                // FOCUS_VAL 응답 = "그랩 완료" ACK(점수 아님, 채점은 백그라운드). 핸들러가 이 ACK를 받고 다음 Z 이동.
                if (!isAsyncStart)
                {
                    bool quiet = isPollCmd && resp == "0";                       // 진행중 폴링 — 로그 생략
                    if (isPollCmd && !quiet) LogMsg($"[{ModuleName}] RX: {line}");   // 완료/실패 시점만 RX 기록
                    string target = ResolveEchoToken(cmd, parts);
                    Send(stream, string.IsNullOrEmpty(target)
                        ? $"ACK|{mod}|{cmd}|{resp}"
                        : $"ACK|{mod}|{cmd}|{echo}|{resp}", quiet);
                }
            }
            catch (Exception ex)
            {
                Send(stream, $"ERR|{mod}|{cmd}|{ex.Message}");
            }
        }

        // ── 명령 핸들러 ────────────────────────────

        /// <summary>RUN 게이트 면제 명령 — PING(상태확인)과 단발 그랩(EXPOSE/GRAB, 모션 없음·수동/셋업 테스트용).</summary>
        private static bool IsGateExemptCommand(string cmd)
            => cmd == "PING" || cmd == "EXPOSE" || cmd == "GRAB"
            || cmd == "CAM_SETTING"
            || cmd == "FOCUS_START" || cmd == "FOCUS_VAL" || cmd == "FOCUS_BEST";   // 오토포커스=셋업/캘리브레이션, RUN 아닐 때도 허용(그랩만, 모션은 핸들러 책임)

        /// <summary>응답 ACK 의 echo 토큰 선택.
        /// 동기 도구 계열(MATCH/INSPECT/TRAIN)은 finder/inspector(parts[2])를 echo 하여 핸들러가 어떤 도구 결과인지 식별.
        /// 비동기 계열(MATCHASYNC/RESULT, INSPECTASYNC/RESULT)은 echo 없음 — 번호(chip_uid)는 저장 키로만 쓰고 응답엔 넣지 않는다.</summary>
        private static string ResolveEchoToken(string cmd, string[] parts)
        {
            switch (cmd)
            {
                case "MATCH":
                case "INSPECT":
                case "TRAIN":
                    return parts.Length > 2 ? parts[2] : null;   // finder / inspector
                default:
                    return null;                                  // 비동기 계열 등은 echo 없음(예: ACK|MODULE|MATCHASYNC|STARTED)
            }
        }

        // 명령 실행은 공통 코어(VisionCommandCore)로 위임 — 자체 시퀀서(DirectVisionCommandDispatcher)와 동일 구현 공유.
        private string DoExpose(IVisionModule m) => VisionCommandCore.Grab(m);

        private string DoMatch(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Match(m, _cfg, finder, chipUid);
        }

        /// <summary>비동기 매칭 시작 — 요청 즉시 "STARTED"(그랩 전 1차 ACK) 반환, 그랩과 알고리즘은 모두 백그라운드.
        /// 결과는 <see cref="AsyncMatchStore"/> 에 (모듈,finder,chip_uid) 키로 저장되고 핸들러는 MATCHRESULT|finder|chip_uid 로 폴링한다.
        /// ※ 그랩 전 ACK 이므로 핸들러는 EPD(촬상완료) 동기화 후 스테이지를 이동해야 한다(요청한 번호의 첫 영상 보장).</summary>
        private string DoMatchAsync(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(finder)) return "fail:no finder";
            if (!m.Finders.TryGetValue(finder, out var f)) return "fail:finder not found";

            AsyncMatchStore.Start(m.Name, finder, chipUid);   // 번호별 기존 결과 무효화 + Running 표시
            var cfg = _cfg;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var g = m.GrabForTool(finder);            // 그랩도 백그라운드(ACK 이후 수행)
                    if (g == null || !g.IsSuccess)
                    {
                        try { g?.Dispose(); } catch { }
                        AsyncMatchStore.Fail(m.Name, finder, chipUid, g?.ErrorMessage ?? "grab");
                        return;
                    }
                    try
                    {
                        string res = VisionCommandCore.MatchOnImage(m, cfg, finder, f, g.Image, chipUid);
                        if (res != null && res.StartsWith("OK;"))
                            AsyncMatchStore.Complete(m.Name, finder, chipUid, res.Substring(3));   // 'OK;' 제거 → x=..;y=..;r=..;score=..
                        else
                            AsyncMatchStore.Fail(m.Name, finder, chipUid, res ?? "no result");
                    }
                    finally { try { g.Dispose(); } catch { } }
                }
                catch (Exception ex) { AsyncMatchStore.Fail(m.Name, finder, chipUid, ex.Message); }
            });
            return "STARTED";
        }

        /// <summary>비동기 매칭 결과 폴링 — (모듈,finder,chip_uid) 키로 조회.
        /// "0"(진행/미시작)/"1;x=..;y=..;r=..;score=.."(완료)/"ERR;사유"(실패).</summary>
        private string DoMatchResult(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(finder)) return "fail:no finder";
            var st = AsyncMatchStore.TryGet(m.Name, finder, chipUid, out string payload);
            switch (st)
            {
                case AsyncMatchStore.State.Done:    return "1;" + payload;
                case AsyncMatchStore.State.Error:   return "ERR;" + payload;
                case AsyncMatchStore.State.Running: return "0";
                default:                            return "0";   // 미시작/없음
            }
        }

        private string DoInspect(IVisionModule m, string[] parts)
        {
            string insp    = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Inspect(m, _cfg, insp, chipUid);
        }

        /// <summary>비동기 검사 시작(방식 B) — 요청 즉시 "STARTED"(그랩 전 1차 ACK) 반환.
        /// 백그라운드에서 '그랩만' 해 <see cref="PendingGrabStore"/> 에 보관(검사 X). 보관 개수가 예상 픽커 수에
        /// 도달하는 순간 그 자리에서 일괄 병렬 검사를 자동 시작한다. 검사 사용 OFF면 그랩 없이 즉시 완료(PASS).</summary>
        private string DoInspectAsync(IVisionModule m, string[] parts)
        {
            string insp = parts.Length > 2 ? parts[2] : "";
            // 형식: MODULE|INSPECTASYNC|inspector|picker_id|chip_uid[|die_index]  (선택 die_index=픽업 순서 1-base)
            // 핸들러는 실제 칩 위치 대신 '인덱스 번호'만 보내고, Vision 이 활성 레시피의 칩위치(픽업 순서)로
            // 인덱스→셀(IndexX/IndexY)을 매칭해 Bottom 모니터링 맵에 그린다. 구형 호환: inspector|chip_uid.
            int picker = 0, dieIndex = 0; string chipUid = "";
            if (parts.Length >= 5) { int.TryParse(parts[3], out picker); chipUid = parts[4]; }   // picker_id | chip_uid
            else if (parts.Length == 4) { chipUid = parts[3]; }                                   // 구형: chip_uid 만
            if (parts.Length >= 6) int.TryParse(parts[5], out dieIndex);
            // die_index 필드가 없고 chip_uid 가 숫자면 그것이 곧 die_index (Sim/단순 핸들러: inspector|picker|die_index 3필드).
            if (dieIndex <= 0) int.TryParse(chipUid, out dieIndex);
            int ix = 0, iy = 0;
            if (dieIndex > 0 && !QMC.Vision.DieMaps.PickupOrderResolver.TryGetCell(dieIndex, out ix, out iy))
            { ix = 0; iy = 0; }   // 레시피 순서를 못 구하면 (0,0) — 맵 표시만 생략되고 검사는 정상 진행
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
            if (!m.Inspectors.TryGetValue(insp, out var ins)) return "fail:inspector not found";

            // 검사 사용 게이트 OFF → 그랩 없이 즉시 완료(스킵).
            if (VisionCommandCore.IsInspectionSkipped(m, insp))
            {
                ModuleResultStore.Record(m.Name, insp, true, "inspection=skip");
                AsyncMatchStore.Complete(m.Name, insp, chipUid, "PASS;inspection=skip");
                return "STARTED";
            }

            AsyncMatchStore.Start(m.Name, insp, chipUid);
            PendingGrabStore.NoteGrabStarted(m.Name);   // 안전망 게이트 — 그랩 완료/실패 시 finally 에서 해제
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // ── 방식 B: '그랩만' 하고 보관(검사 X). 데이터 처리는 4개가 다 모이면 일괄 병렬. ──
                    // 그랩 게이트: 연속 INSPECTASYNC 의 백그라운드 그랩이 겹치면 실기 카메라는 실패/순서 꼬임
                    // → 직렬화(요청 순서 = 그랩 순서). ACK 는 이미 나갔고, 핸들러 모션은 EPD 푸시 기준.
                    GrabResult g;
                    lock (_grabGate) { g = m.GrabForTool(insp); }
                    if (g == null || !g.IsSuccess)
                    {
                        try { g?.Dispose(); } catch { }
                        AsyncMatchStore.Fail(m.Name, insp, chipUid, g?.ErrorMessage ?? "grab");
                        return;
                    }
                    // 사본 대신 소유권 이전 — 고해상도(수백 MB) 복제 제거(Sim/실기 공통 이득).
                    System.Drawing.Bitmap keep = g.DetachImage();
                    g.Dispose();
                    int n = PendingGrabStore.Add(m.Name, insp, chipUid, picker, ix, iy, keep);

                    // 보관 개수가 예상 픽커 수에 도달 → 그 자리에서 일괄 병렬 처리 자동 시작(멱등).
                    if (n >= ExpectedPickerCount(m) && PendingGrabStore.TryBeginProcessing(m.Name))
                        ProcessPendingBatchParallel(m, insp);
                }
                catch (Exception ex) { AsyncMatchStore.Fail(m.Name, insp, chipUid, ex.Message); }
                finally { PendingGrabStore.NoteGrabEnded(m.Name); }
            });
            return "STARTED";
        }

        /// <summary>비동기 검사 결과 — 대기형 응답(요청 1회 = 데이터 응답 1회).
        /// <para>핸들러 흐름 6단계: 결과 요청이 오면 완료까지 서버가 대기했다가 "1;PASS|FAIL;.." 로 응답한다.
        /// 핸들러는 반복 폴링 없이 요청 1회로 데이터를 받고 다음 사이클(1번 그랩요청)로 진행.</para>
        /// <para>대기 상한은 루프백/핸들러 IO 타임아웃(8s)보다 짧게 6s — 만료 시 "0"(구형 폴링 호환, 재요청하면 됨).
        /// 미시작(None)은 즉시 "0". 실패는 "ERR;사유".</para>
        /// <para>안전망(부분 배치 구제)은 2s 유예 후에만 발화 — 그랩이 백그라운드로 아직 쌓이는 중에
        /// 조기 발화하면 배치가 1+3 등으로 쪼개져 진짜 병렬이 깨지므로(기존 버그), 정상 흐름(4번째 그랩
        /// 자동 트리거)이 일어날 시간을 준 뒤에만 남은 보관분을 처리한다.</para></summary>
        private string DoInspectResult(IVisionModule m, string[] parts)
        {
            string insp    = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";

            const int WaitMs = 6000;    // 응답 대기 상한(클라이언트 IO 타임아웃 8s 미만)
            const int StepMs = 30;      // 완료 확인 주기
            const int SafetyMs = 2000;  // 부분 배치 안전망 발화 유예
            bool safetyTried = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (;;)
            {
                var st = AsyncMatchStore.TryGet(m.Name, insp, chipUid, out string payload);
                switch (st)
                {
                    case AsyncMatchStore.State.Done:  return "1;" + payload;
                    case AsyncMatchStore.State.Error: return "ERR;" + payload;
                    case AsyncMatchStore.State.None:  return "0";   // 미시작(INSPECTASYNC 전) — 즉시 반환
                }
                // 안전망: 진행 중 그랩이 하나라도 있으면 발화 금지(부분 배치 쪼개짐 방지).
                // 그랩이 모두 끝났는데(4번째 자동 트리거 미발생 = 부분 배치) 유예도 지났을 때만 잔여분 처리.
                if (!safetyTried && sw.ElapsedMilliseconds >= SafetyMs && !PendingGrabStore.HasInFlight(m.Name))
                {
                    safetyTried = true;
                    if (PendingGrabStore.TryBeginProcessing(m.Name))
                        ProcessPendingBatchParallel(m, insp);
                }
                if (sw.ElapsedMilliseconds >= WaitMs) return "0";
                System.Threading.Thread.Sleep(StepMs);
            }
        }

        /// <summary>예상 픽커 수 — 한 번의 Bottom 검사 배치에서 그랩되는 픽커 개수.
        /// 기존 관례(자동시퀀스 picker %4)에 맞춰 기본 4. TODO: 머신/설정 상수로 승격.</summary>
        private static int ExpectedPickerCount(IVisionModule m) => 4;

        /// <summary>보관된 그랩 전체를 '픽커별 독립 인스펙터 인스턴스'로 병렬 검사(진짜 병렬 — 공유 인스펙터 락 회피).
        /// CDT-310 코어(CDTInspector.BottomInspect)는 그대로 호출하며, 결과는 chip_uid별로 <see cref="AsyncMatchStore"/> 에 저장.
        /// 각 픽커는 DomainInspectorFactory 로 새 인스턴스를 만들고 모듈 인스펙터의 레시피 파라미터를 복제해 사용한다.</summary>
        private void ProcessPendingBatchParallel(IVisionModule m, string insp)
        {
            var items = PendingGrabStore.Take(m.Name);
            if (items.Count == 0) return;
            var cfg = _cfg;
            var template = m.Inspectors.TryGetValue(insp, out var t) ? t : null;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    long curGen = QMC.Vision.Core.InspectionResultStore.GenerationOf(m.Name);
                    System.Threading.Tasks.Parallel.For(0, items.Count, i =>
                    {
                        var it = items[i];
                        // 웨이퍼 경계(Clear) 이전에 그랩된 잔여 항목 — 검사/기록하면 새 맵에 유령 셀이 생기므로 폐기.
                        if (it.Gen != curGen)
                        { AsyncMatchStore.Fail(m.Name, it.Insp, it.ChipUid, "stale wafer batch(폐기 — 새 웨이퍼 초기화 이후 도착)"); return; }
                        int picker = it.Picker > 0 ? it.Picker : (i + 1);   // 명령에 실린 picker 우선, 없으면 도착 순서
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            // 픽커마다 새 인스펙터 인스턴스 → 각자 내부 상태(_libInspector/Last*)를 보유해 레이스 없음.
                            if (!QMC.Vision.Core.DomainInspectorFactory.TryCreate(it.Insp, out var ins))
                            { AsyncMatchStore.Fail(m.Name, it.Insp, it.ChipUid, "inspector create fail"); return; }

                            QMC.Vision.Core.UnitContext.ApplyScale(ins, m.ScaleX, m.ScaleY);
                            if (template != null) VisionCommandCore.CopyInspectorConfig(template, ins);   // 레시피 파라미터 복제

                            string res = VisionCommandCore.InspectOnImageExplicit(
                                m, cfg, it.Insp, ins, it.Image, it.ChipUid, picker, -1, it.IndexX, it.IndexY);
                            sw.Stop();

                            if (res != null && (res.StartsWith("PASS") || res.StartsWith("FAIL")))
                            {
                                // 규약 완료 포맷: PASS|FAIL;x=..;y=..;t=..;score=..  (Bottom 표면은 x/y/score 미산출 → 빈 값)
                                string verdict = res.StartsWith("PASS") ? "PASS" : "FAIL";
                                string payload = verdict + ";x=;y=;t=" + sw.ElapsedMilliseconds + ";score=";
                                AsyncMatchStore.Complete(m.Name, it.Insp, it.ChipUid, payload);
                            }
                            else
                                AsyncMatchStore.Fail(m.Name, it.Insp, it.ChipUid, res ?? "no result");
                        }
                        catch (Exception ex) { AsyncMatchStore.Fail(m.Name, it.Insp, it.ChipUid, ex.Message); }
                        finally { try { it.Image?.Dispose(); } catch { } }
                    });
                }
                catch (Exception ex)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "InspectBatch", m.Name + " 병렬 처리 실패: " + ex.Message);
                }
            });
        }

        private static string DoTrain(IVisionModule m, string[] parts)
        {
            string finder = parts.Length > 2 ? parts[2] : "";
            return VisionCommandCore.Train(m, finder);
        }

        private string DoScale(IVisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need chipWmm chipHmm";
            if (!double.TryParse(parts[2], out var wMm)) return "fail:bad width";
            if (!double.TryParse(parts[3], out var hMm)) return "fail:bad height";
            if (!m.Calibrate(wMm, hMm, out var sx, out var sy, out var err))
                return "fail:" + err;
            // 모듈별 CameraConfig 스케일 갱신 + 영속(SSOT=모듈)
            var map = m.ExportCameraMapping();
            map.ScaleX = sx; map.ScaleY = sy;
            m.ImportCameraMapping(map);
            try { m.SaveSettings(); } catch { }
            return $"OK;scaleX={sx:F6};scaleY={sy:F6}";
        }

        private static string DoRotCenter(IVisionModule m)
        {
            if (!m.MeasureRotationalCenter(out var corners, out var err))
                return "fail:" + err;
            var sb = new StringBuilder("OK");
            for (int i = 0; i < corners.Count; i++)
                sb.Append($";x{i}={corners[i].X:F2};y{i}={corners[i].Y:F2}");
            return sb.ToString();
        }

        private static string DoDistort(IVisionModule m)
        {
            if (!m.LearnDistortion(out var err))
                return "fail:" + err;
            return "OK";
        }

        private static string DoCamSwitch(IVisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need toolName liveOn";
            string toolName = parts[2];
            string liveOn   = parts[3];
            // 단일 카메라 모듈에서는 no-op. 멀티 카메라 모듈에서 override 가능.
            return $"OK;tool={toolName};live={liveOn}";
        }

        private static string DoCameraSetting(IVisionModule m)
        {
            var map = m.ExportCameraMapping();
            int width = 0;
            int height = 0;
            try
            {
                if (m.Camera != null)
                {
                    var resolution = m.Camera.Resolution;
                    width = resolution.Width;
                    height = resolution.Height;
                }
            }
            catch { }

            if (width <= 0 || height <= 0)
            {
                width = map.RoiWidth > 0 ? map.RoiWidth : 640;
                height = map.RoiHeight > 0 ? map.RoiHeight : 480;
            }

            return $"OK|w={width};h={height};scaleX={map.ScaleX:F9};scaleY={map.ScaleY:F9}";
        }

        private static string DoFocusVal(IVisionModule m)
        {
            if (!m.MeasureFocus(out var rois, out var err))
                return "fail:" + err;
            return "OK;" + string.Join(";", rois.Select(p => $"{p.Key}={p.Value:F2}"));
        }

        // ── 비동기 이벤트 ────────────────────────

        private void OnExposureDone(string moduleName)
        {
            // 오토포커스 등 내부 grab 중에는 EPD 푸시 안 함(ACK 응답 스트림 오염 방지).
            if (QMC.Vision.Core.VisionCommandCore.SuppressExposurePush) return;
            Broadcast($"EPD|{moduleName}");
        }

        private void OnAlarmed(string moduleName, string reason)
        {
            Broadcast($"ARM|{moduleName}|{reason}");
        }

        /// <summary>모든 연결된 클라이언트에 1줄 송신.</summary>
        public void Broadcast(string line)
        {
            byte[] data = Encoding.UTF8.GetBytes(line + "\n");
            List<TcpClient> snapshot;
            lock (_clients) snapshot = _clients.ToList();
            LogMsg($"[{ModuleName}] PUSH: Before {line}");
            foreach (var c in snapshot)
            {
                try
                {
                    var s = c.GetStream();
                    s.Write(data, 0, data.Length);
                }
                catch { }
            }
            LogMsg($"[{ModuleName}] PUSH: {line}");
        }

        private void Send(NetworkStream stream, string line, bool quiet = false)
        {
            if (stream == null) { LogMsg($"[{ModuleName}] TX dropped (no stream): {line}"); return; }
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                stream.Write(data, 0, data.Length);
                if (!quiet) LogMsg($"[{ModuleName}] TX: {line}");   // quiet=진행중 폴링 응답(로그 홍수 방지)
            }
            catch (Exception ex)
            {
                // 진단: 응답 전송 실패(소켓 닫힘 등).
                LogMsg($"[{ModuleName}] TX error: {ex.Message}");
            }
        }

        private void LogMsg(string s) { try { Log?.Invoke(s); } catch { } }

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
