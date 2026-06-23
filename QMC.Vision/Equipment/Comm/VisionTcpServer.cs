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
            LogMsg($"[{ModuleName}] RX: {line}");
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";

            if (!_modules.TryGetValue(mod, out var m))
            {
                Send(stream, $"ERR|{mod}|{cmd}|unknown module");
                return;
            }
            // RUN 게이트 — RUN 상태가 아니면 PING 외 명령 거부(핸들러 명령은 RUN 시에만 수락).
            if (cmd != "PING" && IsCommandAllowed != null && !IsCommandAllowed())
            {
                Send(stream, $"ERR|{mod}|{cmd}|not running (press RUN)");
                return;
            }
            try
            {
                string resp;
                switch (cmd)
                {
                    case "PING":       resp = "OK";              break;
                    case "EXPOSE":
                    case "GRAB":       resp = DoExpose(m);       break;
                    case "MATCH":      resp = DoMatch(m, parts); break;
                    case "MATCHASYNC": resp = DoMatchAsync(m, parts); break;   // 1차 ACK(STARTED) 후 백그라운드 알고리즘
                    case "MATCHRESULT":resp = DoMatchResult(m, parts); break;  // 폴링: 0/1;data/ERR
                    case "INSPECT":    resp = DoInspect(m, parts); break;
                    case "INSPECTASYNC": resp = DoInspectAsync(m, parts); break;   // 1차 ACK(STARTED) 후 백그라운드 검사
                    case "INSPECTRESULT":resp = DoInspectResult(m, parts); break;  // 폴링: 0/1;PASS|FAIL../ERR
                    case "TRAIN":      resp = DoTrain(m, parts); break;
                    case "SCALE":      resp = DoScale(m, parts); break;
                    case "ROT_CENTER": resp = DoRotCenter(m);    break;
                    case "DISTORT":    resp = DoDistort(m);      break;
                    case "CAM_SWITCH": resp = DoCamSwitch(m, parts); break;
                    case "FOCUS_VAL":  resp = DoFocusVal(m);     break;
                    default:           resp = null;              break;
                }
                if (resp == null) Send(stream, $"ERR|{mod}|{cmd}|unknown command");
                else
                {
                    // MATCH(ASYNC/RESULT)/INSPECT/TRAIN 은 대상(finder/inspector)을 응답에 echo → 핸들러가 어떤 도구 결과인지 식별.
                    //   예: ACK|WaferVision|MATCH|AlignDieFinder|OK;x=...;y=...;r=...;score=...
                    string target = (cmd == "MATCH" || cmd == "MATCHASYNC" || cmd == "MATCHRESULT"
                                     || cmd == "INSPECT" || cmd == "INSPECTASYNC" || cmd == "INSPECTRESULT"
                                     || cmd == "TRAIN") && parts.Length > 2 ? parts[2] : null;
                    Send(stream, string.IsNullOrEmpty(target)
                        ? $"ACK|{mod}|{cmd}|{resp}"
                        : $"ACK|{mod}|{cmd}|{target}|{resp}");
                }
            }
            catch (Exception ex)
            {
                Send(stream, $"ERR|{mod}|{cmd}|{ex.Message}");
            }
        }

        // ── 명령 핸들러 ────────────────────────────

        // 명령 실행은 공통 코어(VisionCommandCore)로 위임 — 자체 시퀀서(DirectVisionCommandDispatcher)와 동일 구현 공유.
        private string DoExpose(IVisionModule m) => VisionCommandCore.Grab(m);

        private string DoMatch(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            return VisionCommandCore.Match(m, _cfg, finder, chipUid);
        }

        /// <summary>비동기 매칭 시작 — 그랩(동기) 후 즉시 "STARTED"(1차 ACK), 알고리즘은 백그라운드.
        /// 완료 결과는 <see cref="AsyncMatchStore"/> 에 저장되고 핸들러는 MATCHRESULT 로 폴링한다.</summary>
        private string DoMatchAsync(IVisionModule m, string[] parts)
        {
            string finder  = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(finder)) return "fail:no finder";
            if (!m.Finders.TryGetValue(finder, out var f)) return "fail:finder not found";

            // 그랩은 동기 — 1차 ACK(STARTED) 전에 영상이 확보돼야 핸들러가 안전하게 다음 스텝 진행.
            var g = m.GrabForTool(finder);
            if (g == null || !g.IsSuccess) { try { g?.Dispose(); } catch { } return "fail:" + (g?.ErrorMessage ?? "grab"); }

            AsyncMatchStore.Start(m.Name, finder);
            var cfg = _cfg;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string res = VisionCommandCore.MatchOnImage(m, cfg, finder, f, g.Image, chipUid);
                    if (res != null && res.StartsWith("OK;"))
                        AsyncMatchStore.Complete(m.Name, finder, res.Substring(3));   // 'OK;' 제거 → x=..;y=..;r=..;score=..
                    else
                        AsyncMatchStore.Fail(m.Name, finder, res ?? "no result");
                }
                catch (Exception ex) { AsyncMatchStore.Fail(m.Name, finder, ex.Message); }
                finally { try { g.Dispose(); } catch { } }
            });
            return "STARTED";
        }

        /// <summary>비동기 매칭 결과 폴링 — "0"(진행)/"1;x=..;y=..;r=..;score=.."(완료)/"ERR;사유"(실패)/"0"(미시작).</summary>
        private string DoMatchResult(IVisionModule m, string[] parts)
        {
            string finder = parts.Length > 2 ? parts[2] : "";
            if (string.IsNullOrEmpty(finder)) return "fail:no finder";
            var st = AsyncMatchStore.TryGet(m.Name, finder, out string payload);
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

        /// <summary>비동기 검사 시작 — 그랩(동기) 후 즉시 "STARTED"(1차 ACK), 검사 알고리즘은 백그라운드.
        /// 검사 사용 OFF(게이트)면 그랩 없이 즉시 완료. 결과는 <see cref="AsyncMatchStore"/> 에 저장.</summary>
        private string DoInspectAsync(IVisionModule m, string[] parts)
        {
            string insp    = parts.Length > 2 ? parts[2] : "";
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
            if (!m.Inspectors.TryGetValue(insp, out var ins)) return "fail:inspector not found";

            // 검사 사용 게이트 OFF → 그랩 없이 즉시 완료(스킵).
            if (VisionCommandCore.IsInspectionSkipped(m, insp))
            {
                ModuleResultStore.Record(m.Name, insp, true, "inspection=skip");
                AsyncMatchStore.Complete(m.Name, insp, "PASS;inspection=skip");
                return "STARTED";
            }

            var g = m.GrabForTool(insp);
            if (g == null || !g.IsSuccess) { try { g?.Dispose(); } catch { } return "fail:" + (g?.ErrorMessage ?? "grab"); }

            AsyncMatchStore.Start(m.Name, insp);
            var cfg = _cfg;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string res = VisionCommandCore.InspectOnImage(m, cfg, insp, ins, g.Image, chipUid);
                    if (res != null && (res.StartsWith("PASS") || res.StartsWith("FAIL")))
                        AsyncMatchStore.Complete(m.Name, insp, res);   // payload = PASS;.. / FAIL;..
                    else
                        AsyncMatchStore.Fail(m.Name, insp, res ?? "no result");
                }
                catch (Exception ex) { AsyncMatchStore.Fail(m.Name, insp, ex.Message); }
                finally { try { g.Dispose(); } catch { } }
            });
            return "STARTED";
        }

        /// <summary>비동기 검사 결과 폴링 — "0"(진행)/"1;PASS|FAIL;.."(완료)/"ERR;사유"(실패)/"0"(미시작).</summary>
        private string DoInspectResult(IVisionModule m, string[] parts)
        {
            string insp = parts.Length > 2 ? parts[2] : "";
            if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
            var st = AsyncMatchStore.TryGet(m.Name, insp, out string payload);
            switch (st)
            {
                case AsyncMatchStore.State.Done:    return "1;" + payload;
                case AsyncMatchStore.State.Error:   return "ERR;" + payload;
                case AsyncMatchStore.State.Running: return "0";
                default:                            return "0";
            }
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

        private static string DoFocusVal(IVisionModule m)
        {
            if (!m.MeasureFocus(out var rois, out var err))
                return "fail:" + err;
            return "OK;" + string.Join(";", rois.Select(p => $"{p.Key}={p.Value:F2}"));
        }

        // ── 비동기 이벤트 ────────────────────────

        private void OnExposureDone(string moduleName)
        {
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

        private void Send(NetworkStream stream, string line)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(line + "\n");
                stream.Write(data, 0, data.Length);
                LogMsg($"[{ModuleName}] TX: {line}");
            }
            catch { }
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
