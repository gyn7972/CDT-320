using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Secs
{
    /// <summary>
    /// HSMS (SEMI E37) 연결 — 4-byte length-prefix 프레이밍.
    /// 본 라운드는 Stage 3 골격: 실 SECS-II 메시지 인코딩은 별도 단계.
    /// 메시지 전송/수신은 byte[] 페이로드 단위.
    /// </summary>
    public class HsmsConnection : IDisposable
    {
        public event Action<byte[]> MessageReceived;
        public event Action<string> Log;

        public string Host { get; }
        public int    Port { get; }
        public bool   IsConnected => _client != null && _client.Connected;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();

        public HsmsConnection(string host, int port)
        {
            Host = host; Port = port;
        }

        public async Task<bool> ConnectAsync(int timeoutMs = 5000)
        {
            if (IsConnected) return true;
            _client = new TcpClient();
            try
            {
                var task = _client.ConnectAsync(Host, Port);
                if (await Task.WhenAny(task, Task.Delay(timeoutMs)) != task)
                {
                    _client.Close(); _client = null;
                    LogMsg("[HSMS] connect timeout");
                    return false;
                }
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoop(_cts.Token));
                LogMsg($"[HSMS] connected {Host}:{Port}");
                return true;
            }
            catch (Exception ex) { LogMsg("[HSMS] connect err: " + ex.Message); return false; }
        }

        public void Disconnect()
        {
            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null; _client = null;
            LogMsg("[HSMS] disconnected");
        }

        /// <summary>4-byte big-endian length prefix + payload 송신.</summary>
        public bool Send(byte[] payload)
        {
            if (!IsConnected || payload == null) return false;
            try
            {
                int len = payload.Length;
                byte[] frame = new byte[4 + len];
                frame[0] = (byte)((len >> 24) & 0xFF);
                frame[1] = (byte)((len >> 16) & 0xFF);
                frame[2] = (byte)((len >> 8)  & 0xFF);
                frame[3] = (byte)( len        & 0xFF);
                Buffer.BlockCopy(payload, 0, frame, 4, len);
                lock (_lock) _stream.Write(frame, 0, frame.Length);
                LogMsg($"[HSMS] TX {len} bytes");
                return true;
            }
            catch (Exception ex) { LogMsg("[HSMS] tx err: " + ex.Message); return false; }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            byte[] header = new byte[4];
            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    int read = await ReadExactly(header, 4, ct);
                    if (read != 4) break;
                    int len = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                    if (len < 0 || len > 16 * 1024 * 1024)
                    {
                        LogMsg($"[HSMS] invalid length {len}");
                        break;
                    }
                    var payload = new byte[len];
                    if (len > 0)
                    {
                        read = await ReadExactly(payload, len, ct);
                        if (read != len) break;
                    }
                    LogMsg($"[HSMS] RX {len} bytes");
                    try { MessageReceived?.Invoke(payload); } catch { }
                }
            }
            catch (Exception ex) { LogMsg("[HSMS] rx err: " + ex.Message); }
            Disconnect();
        }

        private async Task<int> ReadExactly(byte[] buf, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int n = await _stream.ReadAsync(buf, total, count - total, ct);
                if (n == 0) return total;
                total += n;
            }
            return total;
        }

        private void LogMsg(string s) { try { Log?.Invoke(s); } catch { } }

        public void Dispose() => Disconnect();
    }
}
