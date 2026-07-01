using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using QMC.Vision.Config;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 자체 시퀀스를 <b>실제 TCP/IP 경로</b>로 구동하는 디스패처.
    /// <para>
    /// <see cref="DirectVisionCommandDispatcher"/> 가 모듈 API 를 in-process 로 직접 호출하는 것과 달리,
    /// 이 디스패처는 Vision 자기 자신의 <c>VisionTcpServer</c>(127.0.0.1:모듈포트)에 TCP 클라이언트로 접속해
    /// 핸들러와 동일한 <c>MODULE|CMD|args</c> 라인을 보내고 <c>ACK|MODULE|CMD|payload</c> 응답을 받는다.
    /// 즉 핸들러 없이 Vision 혼자서 "실제 통신 경로(서버+소켓+프로토콜)"까지 테스트한다.
    /// </para>
    /// <para>
    /// 반환값은 서버 응답의 payload(마지막 '|' 세그먼트)를 그대로 돌려주어
    /// <see cref="DirectVisionCommandDispatcher"/> 와 동일한 결과 문자열 계약("OK;..","PASS;..","FAIL;..","fail:..")을
    /// 유지한다 → <see cref="ToolSequence"/> 의 판정 로직이 경로에 무관하게 동작한다.
    /// </para>
    /// </summary>
    public sealed class TcpLoopbackVisionCommandDispatcher : IVisionCommandDispatcher, IDisposable
    {
        private const string Host = "127.0.0.1";
        private const int ConnectTimeoutMs = 3000;
        private const int IoTimeoutMs = 8000;

        // 모듈 1개 = 포트 1개 = 연결 1개. 포트별로 직렬화(락)해 병렬 모듈 루프끼리 간섭 없게 한다.
        private sealed class Conn
        {
            public TcpClient Client;
            public NetworkStream Stream;
            public readonly StringBuilder Rx = new StringBuilder();
            public readonly object Gate = new object();
        }

        private readonly Dictionary<int, Conn> _conns = new Dictionary<int, Conn>();
        private readonly object _mapLock = new object();
        private bool _disposed;

        public string Execute(IVisionModule module, string cmd, string[] args)
        {
            if (module == null) return "fail:no module";
            if (_disposed) return "fail:dispatcher disposed";

            int port = ResolvePort(module.Name);
            if (port <= 0) return "fail:no port for module " + module.Name;

            string line = BuildRequestLine(module.Name, cmd, args);

            Conn conn = GetConn(port);
            lock (conn.Gate)
            {
                try
                {
                    EnsureConnected(conn, port);
                    WriteLine(conn, line);
                    string resp = ReadAckLine(conn);
                    return ExtractPayload(resp);
                }
                catch (Exception ex)
                {
                    // 소켓 오류 시 연결을 버려 다음 호출에서 재접속하게 한다. 결과는 시퀀스가 NG 로 처리.
                    DropConn(conn);
                    return "fail:tcp " + ex.Message;
                }
            }
        }

        // ── 요청 라인 구성 (핸들러와 동일: MODULE|CMD|arg0|arg1...) ──
        private static string BuildRequestLine(string module, string cmd, string[] args)
        {
            var sb = new StringBuilder();
            sb.Append(module);
            sb.Append('|');
            sb.Append((cmd ?? string.Empty).ToUpperInvariant());
            if (args != null)
            {
                foreach (string a in args)
                {
                    sb.Append('|');
                    sb.Append(a ?? string.Empty);
                }
            }
            return sb.ToString();
        }

        // ── 응답에서 payload(마지막 '|' 세그먼트) 추출 → Direct 반환 계약과 동일 ──
        private static string ExtractPayload(string line)
        {
            if (string.IsNullOrEmpty(line)) return "fail:no response";
            string header = line.Length >= 3 ? line.Substring(0, 3) : line;
            int last = line.LastIndexOf('|');
            string payload = last >= 0 ? line.Substring(last + 1) : line;

            // 서버가 ERR 로 응답하면(예: not running) 시퀀스가 실패로 보게 "fail:" 접두.
            if (string.Equals(header, "ERR", StringComparison.OrdinalIgnoreCase))
                return "fail:" + payload;

            return payload;
        }

        // ── 소켓 I/O ──
        private void EnsureConnected(Conn conn, int port)
        {
            if (conn.Client != null && conn.Client.Connected && conn.Stream != null)
                return;

            DropConn(conn);
            var client = new TcpClient();
            var ar = client.BeginConnect(Host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(ConnectTimeoutMs))
            {
                try { client.Close(); } catch { }
                throw new TimeoutException("connect timeout " + Host + ":" + port);
            }
            client.EndConnect(ar);
            client.ReceiveTimeout = IoTimeoutMs;
            client.SendTimeout = IoTimeoutMs;
            conn.Client = client;
            conn.Stream = client.GetStream();
            conn.Rx.Clear();
        }

        private static void WriteLine(Conn conn, string line)
        {
            byte[] data = Encoding.UTF8.GetBytes(line + "\n");
            conn.Stream.Write(data, 0, data.Length);
        }

        /// <summary>ACK/ERR 라인 1개를 읽어 반환한다. EPD/ARM/RECIPEREQ 등 비동기 푸시는 건너뛴다.</summary>
        private static string ReadAckLine(Conn conn)
        {
            var buf = new byte[4096];
            while (true)
            {
                // 버퍼에 이미 완성된 라인이 있으면 우선 처리.
                string ready = TryTakeLine(conn, out bool had);
                if (had)
                {
                    if (IsAsyncPush(ready)) continue;   // 푸시는 무시하고 다음 라인 대기
                    return ready;
                }

                int n = conn.Stream.Read(buf, 0, buf.Length);
                if (n <= 0) throw new System.IO.IOException("connection closed");
                conn.Rx.Append(Encoding.UTF8.GetString(buf, 0, n));
            }
        }

        private static string TryTakeLine(Conn conn, out bool had)
        {
            string all = conn.Rx.ToString();
            int idx = all.IndexOfAny(new[] { '\n', '\r' });
            if (idx < 0) { had = false; return null; }

            string line = all.Substring(0, idx).Trim();
            string rest = all.Substring(idx + 1);
            conn.Rx.Clear();
            conn.Rx.Append(rest);

            if (line.Length == 0) { return TryTakeLine(conn, out had); }
            had = true;
            return line;
        }

        private static bool IsAsyncPush(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            // 헤더가 ACK/ERR 가 아니면 푸시(EPD/FPD/ARM/RECIPEREQ 등)로 간주.
            int bar = line.IndexOf('|');
            string header = bar >= 0 ? line.Substring(0, bar) : line;
            return !header.Equals("ACK", StringComparison.OrdinalIgnoreCase)
                && !header.Equals("ERR", StringComparison.OrdinalIgnoreCase);
        }

        private int ResolvePort(string moduleName)
        {
            VisionSettings cfg = VisionConfigStore.Current;
            if (cfg == null || string.IsNullOrEmpty(moduleName)) return 0;

            switch (moduleName)
            {
                case "WaferVision":      return cfg.WaferVisionPort;
                case "BinVision":        return cfg.BinVisionPort;
                case "BottomInspection": return cfg.InspectionVisionPort;
                case "FrontSideVision":  return cfg.FrontSidePort;
                case "RearSideVision":   return cfg.RearSidePort;
                default:                 return 0;
            }
        }

        private Conn GetConn(int port)
        {
            lock (_mapLock)
            {
                if (!_conns.TryGetValue(port, out Conn c))
                {
                    c = new Conn();
                    _conns[port] = c;
                }
                return c;
            }
        }

        private static void DropConn(Conn conn)
        {
            try { conn.Stream?.Close(); } catch { }
            try { conn.Client?.Close(); } catch { }
            conn.Stream = null;
            conn.Client = null;
            conn.Rx.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_mapLock)
            {
                foreach (var kv in _conns)
                {
                    lock (kv.Value.Gate) DropConn(kv.Value);
                }
                _conns.Clear();
            }
        }
    }
}
