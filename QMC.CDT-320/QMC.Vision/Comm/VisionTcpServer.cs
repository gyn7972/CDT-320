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

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly Dictionary<string, VisionModule> _modules = new Dictionary<string, VisionModule>(StringComparer.OrdinalIgnoreCase);
        private VisionSettings _cfg;

        public int  Port      { get; }
        public bool IsRunning { get; private set; }
        public string ModuleName { get; }
        public VisionModule Module { get; }

        /// <summary>1 포트 = 1 모듈 전담 (매뉴얼 기준).</summary>
        public VisionTcpServer(VisionModule module, int port)
        {
            Module     = module;
            ModuleName = module.Name;
            Port       = port;
            _modules[module.Name] = module;

            // 비동기 이벤트 → Broadcast
            module.ExposureDone += OnExposureDone;
            module.Alarmed      += OnAlarmed;

            _cfg = VisionConfigStore.Current ?? new VisionSettings();
            module.DelayBeforeGrabMs = _cfg.DelayBeforeGrabMs;
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
            LogMsg($"[{ModuleName}] RX: {line}");
            var parts = line.Split('|');
            string mod = parts.Length > 0 ? parts[0] : "";
            string cmd = parts.Length > 1 ? parts[1].ToUpperInvariant() : "";

            if (!_modules.TryGetValue(mod, out var m))
            {
                Send(stream, $"ERR|{mod}|{cmd}|unknown module");
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
                    case "INSPECT":    resp = DoInspect(m, parts); break;
                    case "TRAIN":      resp = DoTrain(m, parts); break;
                    case "SCALE":      resp = DoScale(m, parts); break;
                    case "ROT_CENTER": resp = DoRotCenter(m);    break;
                    case "DISTORT":    resp = DoDistort(m);      break;
                    case "CAM_SWITCH": resp = DoCamSwitch(m, parts); break;
                    case "FOCUS_VAL":  resp = DoFocusVal(m);     break;
                    default:           resp = null;              break;
                }
                if (resp == null) Send(stream, $"ERR|{mod}|{cmd}|unknown command");
                else                Send(stream, $"ACK|{mod}|{cmd}|{resp}");
            }
            catch (Exception ex)
            {
                Send(stream, $"ERR|{mod}|{cmd}|{ex.Message}");
            }
        }

        // ── 명령 핸들러 ────────────────────────────

        private string DoExpose(VisionModule m)
        {
            using (var g = m.Grab())
                return g.IsSuccess ? $"w={g.Width};h={g.Height};frame={g.FrameNumber}" : "fail:" + g.ErrorMessage;
        }

        private string DoMatch(VisionModule m, string[] parts)
        {
            if (parts.Length < 3) return "fail:no finder";
            string finder = parts[2];
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (!m.Finders.TryGetValue(finder, out var f)) return "fail:finder not found";
            using (var g = m.Grab())
            {
                if (!g.IsSuccess) return "fail:" + g.ErrorMessage;
                var r = f.Match(g.Image);
                if (!r.Success) return "fail:" + r.ErrorMessage;
                var b = r.Best;
                if (b == null) return "fail:no match";

                double xOut = b.CenterX, yOut = b.CenterY;
                if (_cfg.ReturnMmCoordinates)
                {
                    var scale = new VisionScale(_cfg.ScaleX, _cfg.ScaleY);
                    var vec   = new CameraVector(_cfg.InvertedX, _cfg.InvertedY, _cfg.IsRotated);
                    VisionScale.ConvertPosition(scale, vec, g.Width, g.Height, b.CenterX, b.CenterY, out xOut, out yOut);
                }

                // 이미지 로그 (chipUid != Manual 인 경우만)
                if (!string.IsNullOrEmpty(chipUid) && chipUid != "Manual")
                {
                    try { ImageLogSaver.Save(_cfg, m.Name, finder, chipUid, g.Image); } catch { }
                }

                return $"OK;x={xOut:F3};y={yOut:F3};r={b.AngleDeg:F3};score={b.Score:F3}";
            }
        }

        private string DoInspect(VisionModule m, string[] parts)
        {
            if (parts.Length < 3) return "fail:no inspector";
            string insp = parts[2];
            string chipUid = parts.Length > 3 ? parts[3] : "";
            if (!m.Inspectors.TryGetValue(insp, out var ins)) return "fail:inspector not found";
            using (var g = m.Grab())
            {
                if (!g.IsSuccess) return "fail:" + g.ErrorMessage;
                var r = ins.Inspect(g.Image);
                var items = string.Join(",", r.Items.Select(i => $"{i.Name}={i.Value}"));

                // MaterialTracker 누적 + DataLogSaver
                if (!string.IsNullOrEmpty(chipUid) && chipUid != "Manual")
                {
                    try
                    {
                        // 모듈/inspector 이름으로 분류
                        if (insp.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0
                            || insp.IndexOf("Bottom",  StringComparison.OrdinalIgnoreCase) >= 0)
                            MaterialTracker.ApplyBottom(chipUid, r);
                        else if (insp.IndexOf("Side", StringComparison.OrdinalIgnoreCase) >= 0)
                            MaterialTracker.ApplySide(chipUid, r, _cfg.SideLocation);
                        else if (insp.IndexOf("Placement", StringComparison.OrdinalIgnoreCase) >= 0
                              || insp.IndexOf("DieGap",    StringComparison.OrdinalIgnoreCase) >= 0
                              || insp.IndexOf("Bin",       StringComparison.OrdinalIgnoreCase) >= 0)
                            MaterialTracker.ApplyDieGap(chipUid, r);

                        ImageLogSaver.Save(_cfg, m.Name, insp, chipUid, g.Image);
                        DataLogSaver.SaveIfDieGapComplete(_cfg, chipUid);
                    }
                    catch { }
                }

                return $"{(r.IsPass ? "PASS" : "FAIL")};{items}";
            }
        }

        private static string DoTrain(VisionModule m, string[] parts)
        {
            if (parts.Length < 3) return "fail:no finder";
            string finder = parts[2];
            if (!m.Finders.TryGetValue(finder, out var f)) return "fail:finder not found";
            using (var g = m.Grab())
            {
                if (!g.IsSuccess) return "fail:" + g.ErrorMessage;
                f.Train(g.Image);
                return "OK";
            }
        }

        private string DoScale(VisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need chipWmm chipHmm";
            if (!double.TryParse(parts[2], out var wMm)) return "fail:bad width";
            if (!double.TryParse(parts[3], out var hMm)) return "fail:bad height";
            if (!m.Calibrate(wMm, hMm, out var sx, out var sy, out var err))
                return "fail:" + err;
            // VisionConfig 자동 갱신
            _cfg.ScaleX = sx; _cfg.ScaleY = sy;
            try { VisionConfigStore.Save(); } catch { }
            return $"OK;scaleX={sx:F6};scaleY={sy:F6}";
        }

        private static string DoRotCenter(VisionModule m)
        {
            if (!m.MeasureRotationalCenter(out var corners, out var err))
                return "fail:" + err;
            var sb = new StringBuilder("OK");
            for (int i = 0; i < corners.Count; i++)
                sb.Append($";x{i}={corners[i].X:F2};y{i}={corners[i].Y:F2}");
            return sb.ToString();
        }

        private static string DoDistort(VisionModule m)
        {
            if (!m.LearnDistortion(out var err))
                return "fail:" + err;
            return "OK";
        }

        private static string DoCamSwitch(VisionModule m, string[] parts)
        {
            if (parts.Length < 4) return "fail:need toolName liveOn";
            string toolName = parts[2];
            string liveOn   = parts[3];
            // 단일 카메라 모듈에서는 no-op. 멀티 카메라 모듈에서 override 가능.
            return $"OK;tool={toolName};live={liveOn}";
        }

        private static string DoFocusVal(VisionModule m)
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
