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
            _byModuleName[module.Name] = this;   // 모듈명 푸시 레지스트리(XYT 등) — 재생성 시 최신으로 덮어씀

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
                    case "FOCUS_START":resp = VisionCommandCore.FocusStart(m, parts); break;
                    case "FOCUS_VAL":  resp = VisionCommandCore.FocusValue(m, parts); break;
                    case "FOCUS_BEST": resp = VisionCommandCore.FocusBest(m, parts); break;
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

        /// <summary>RUN 게이트 면제 명령 — PING/그랩/캘리브레이션용 비전 명령은 수동 셋업에서도 허용한다.</summary>
        private static bool IsGateExemptCommand(string cmd)
            => cmd == "PING" || cmd == "EXPOSE" || cmd == "GRAB"
            || cmd == "CAM_SETTING"
            || cmd == "MATCHASYNC" || cmd == "MATCHRESULT"
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
            // 신형 고정 8파트(finder|fb|collet|die_index|channel|chip_uid) — chip_uid 는 맨 뒤.
            if (ColletAddress.TryParseWire(parts, out _, out _, out _, out _, out string newUid))
                chipUid = newUid;
            return VisionCommandCore.Match(m, _cfg, finder, chipUid);
        }

        /// <summary>비동기 매칭 시작 — 요청 즉시 "STARTED"(그랩 전 1차 ACK) 반환, 그랩과 알고리즘은 모두 백그라운드.
        /// 결과는 <see cref="AsyncMatchStore"/> 에 (모듈,finder,chip_uid) 키로 저장되고 핸들러는 MATCHRESULT|finder|chip_uid 로 폴링한다.
        /// ※ 그랩 전 ACK 이므로 핸들러는 EPD(촬상완료) 동기화 후 스테이지를 이동해야 한다(요청한 번호의 첫 영상 보장).</summary>
        private string DoMatchAsync(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            // 신형 고정 8파트(finder|fb|collet|die_index|channel|chip_uid) — chip_uid 는 맨 뒤.
            if (ColletAddress.TryParseWire(parts, out _, out _, out _, out _, out string newUid))
                chipUid = newUid;
            if (string.IsNullOrEmpty(finder)) return "fail:no finder";

            AsyncMatchStore.Start(m.Name, finder, chipUid);   // 번호별 기존 결과 무효화 + Running 표시
            if (!m.Finders.TryGetValue(finder, out var f))
            {
                AsyncMatchStore.Fail(m.Name, finder, chipUid, "finder not found: " + finder);
                return "STARTED";
            }

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

        /// <summary>동기 검사. 신형 고정 8파트(inspector|fb|collet|die_index|channel|chip_uid)면
        /// (fb,collet)→전역 픽커(1~8) 컨텍스트를 걸고 실행, 구형(≤7파트)은 기존 그대로.</summary>
        private string DoInspect(IVisionModule m, string[] parts)
        {
            string insp = parts.Length > 2 ? parts[2] : "";
            if (ColletAddress.TryParseWire(parts, out int fb, out int collet, out int dieIndex, out int channel, out string uid))
            {
                int picker = ColletAddress.ToGlobalPicker(fb, collet);
                int ix = 0, iy = 0;   // die_index=-1(메뉴얼) 또는 0 이면 맵 매칭 생략
                if (dieIndex > 0 && !QMC.Vision.DieMaps.PickupOrderResolver.TryGetCell(dieIndex, out ix, out iy))
                { ix = 0; iy = 0; }
                VisionCommandCore.SetInspectContext(m.Name, picker, channel, ix, iy);
                try { return VisionCommandCore.Inspect(m, _cfg, insp, uid); }
                finally { VisionCommandCore.SetInspectContext(m.Name, 0, -1, 0, 0); }
            }
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Inspect(m, _cfg, insp, chipUid);
        }

        /// <summary>비동기 검사 시작 — 즉시 STARTED(그랩 전 1차 ACK). 실행은 <see cref="AsyncInspectCore"/>(공용 엔진,
        /// 일반 시퀀서 DirectVisionCommandDispatcher 와 공유 — TCP/직접 경로 동작 동일).
        /// <para>신형(고정 8파트): MODULE|INSPECTASYNC|inspector|fb|collet|die_index|channel|chip_uid
        ///  • fb=0(Front)/1(Back), collet=1~4 → 전역 픽커 1~8(<see cref="ColletAddress"/>).
        ///  • die_index = 픽업 순서 1-base, -1=다이 없음(메뉴얼 — 맵 매칭/uid 숫자 폴백 미적용).
        ///  • channel   = 항상 0/1 — Side 0(0°)/1(90°), Bottom/Bin 은 0°로 간주해 0. chip_uid 는 맨 뒤(결과 매칭 키).</para>
        /// <para>구형(≤7파트, 하위호환): inspector|picker_id|chip_uid[|die_index[|channel]] —
        /// die_index 생략 시 chip_uid 가 숫자면 그 값.</para></summary>
        private string DoInspectAsync(IVisionModule m, string[] parts)
        {
            string insp = parts.Length > 2 ? parts[2] : "";
            int picker = 0, dieIndex = 0, channel = -1; string chipUid = "";
            if (ColletAddress.TryParseWire(parts, out int fb, out int collet, out dieIndex, out channel, out chipUid))
            {
                picker = ColletAddress.ToGlobalPicker(fb, collet);   // 신형 — 폴백 없음(die_index=-1 존중)
            }
            else
            {
                if (parts.Length >= 5) { int.TryParse(parts[3], out picker); chipUid = parts[4]; }   // picker_id | chip_uid
                else if (parts.Length == 4) { chipUid = parts[3]; }                                   // 구형: chip_uid 만
                if (parts.Length >= 6) int.TryParse(parts[5], out dieIndex);
                if (parts.Length >= 7 && !int.TryParse(parts[6], out channel)) channel = -1;
                if (dieIndex <= 0) int.TryParse(chipUid, out dieIndex);   // uid 가 숫자면 곧 die_index(구형 전용)
            }
            return AsyncInspectCore.Start(m, _cfg, insp, picker, chipUid, dieIndex, channel);
        }

        /// <summary>비동기 검사 결과 — 대기형 응답(요청 1회 = 데이터 응답 1회). <see cref="AsyncInspectCore.WaitResult"/> 위임.</summary>
        private string DoInspectResult(IVisionModule m, string[] parts)
        {
            string insp    = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return AsyncInspectCore.WaitResult(m, _cfg, insp, chipUid);
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

        // ── 모듈명 → 서버 인스턴스 레지스트리(비동기 푸시 진입점) ──
        // BottomXytPushService 등 코어 계층이 모듈명만으로 해당 채널에 푸시(XYT 등)할 수 있게 한다.
        // 생성 시 등록(재기동 시 최신 인스턴스로 덮어씀). 현 토폴로지(서버=진짜 앱) 전용 —
        // 클라이언트 링크 토폴로지는 미지원(필요 시 VisionTcpClientLink 에 동일 레지스트리 추가).
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, VisionTcpServer> _byModuleName =
            new System.Collections.Concurrent.ConcurrentDictionary<string, VisionTcpServer>(StringComparer.OrdinalIgnoreCase);

        /// <summary>모듈명으로 등록된 서버에 푸시 1줄 송신. 서버 없음/미접속이면 false(로그는 호출자).</summary>
        public static bool TryBroadcast(string moduleName, string line)
        {
            try
            {
                if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(line)) return false;
                if (!_byModuleName.TryGetValue(moduleName, out var svr) || svr == null) return false;
                svr.Broadcast(line);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Bottom 외곽 종료(EventSearchDieEnd) XYT 비동기 푸시 —
        /// "XYT|MODULE|fb|collet|chip_uid|x=..;y=..;t=..;ix=..;iy=..;valid=0|1" (x/y=px, t=deg).
        /// 정책(2026-07-04): 외곽 미검출이면 x/y/t 를 0 으로 보내고 valid=0 — 수신측은 진행(정지하지 않음).
        /// EPD/ARM 과 같은 푸시 계열(응답 큐 무관). Side 공정이 Bottom 완료 대기 없이 XYT 를 소비한다.
        /// </summary>
        public static bool PushBottomXyt(string moduleName, int fb, int collet, string chipUid,
                                         double x, double y, double t, int ix, int iy, bool valid)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string line = "XYT|" + moduleName + "|" + fb + "|" + collet + "|" + (chipUid ?? "") + "|" +
                          "x=" + x.ToString("F3", inv) + ";y=" + y.ToString("F3", inv) +
                          ";t=" + t.ToString("F4", inv) + ";ix=" + ix + ";iy=" + iy +
                          ";valid=" + (valid ? "1" : "0");
            return TryBroadcast(moduleName, line);
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
