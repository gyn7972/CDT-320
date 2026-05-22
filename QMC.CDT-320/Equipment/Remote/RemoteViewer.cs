using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT320.Remote
{
    /// <summary>
    /// 외부 PC 에서 화면 모니터링 — Form 화면을 주기적으로 PNG 캡처 → TCP 클라이언트에 base64 로 푸시.
    /// 310 의 RemoteViewer 단순화 (코드 독자 작성).
    /// </summary>
    public class RemoteViewer : IDisposable
    {
        public event Action<string> Log;

        public int  Port            { get; }
        public int  IntervalMs      { get; set; } = 1000;   // 1 fps 기본
        public int  CaptureWidth    { get; set; } = 800;
        public int  CaptureHeight   { get; set; } = 600;
        public bool IsRunning       { get; private set; }

        private readonly Form _target;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public RemoteViewer(Form target, int port = 5099)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Port = port;
        }

        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                IsRunning = true;
                LogMsg($"[REMOTE] listening on {Port}");
                _ = Task.Run(() => AcceptLoop(_cts.Token));
                _ = Task.Run(() => CaptureLoop(_cts.Token));
            }
            catch (Exception ex) { LogMsg("[REMOTE] start fail: " + ex.Message); }
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
            LogMsg("[REMOTE] stopped");
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var c = await _listener.AcceptTcpClientAsync();
                    lock (_clients) _clients.Add(c);
                    LogMsg($"[REMOTE] viewer connected: {c.Client.RemoteEndPoint}");
                }
            }
            catch { }
        }

        private async Task CaptureLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    string b64 = await CaptureBase64Async();
                    if (b64 != null)
                    {
                        string line = "FRAME|" + b64 + "\n";
                        var data = Encoding.UTF8.GetBytes(line);
                        List<TcpClient> snapshot;
                        lock (_clients) snapshot = new List<TcpClient>(_clients);
                        foreach (var c in snapshot)
                        {
                            try { var s = c.GetStream(); s.Write(data, 0, data.Length); }
                            catch { lock (_clients) _clients.Remove(c); try { c.Close(); } catch { } }
                        }
                    }
                }
                catch { }
                try { await Task.Delay(IntervalMs, ct); } catch { break; }
            }
        }

        private Task<string> CaptureBase64Async()
        {
            var tcs = new TaskCompletionSource<string>();
            if (_target.IsDisposed) { tcs.SetResult(null); return tcs.Task; }
            try
            {
                _target.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        using (var bmp = new Bitmap(_target.Width, _target.Height))
                        {
                            _target.DrawToBitmap(bmp, new Rectangle(0, 0, _target.Width, _target.Height));
                            using (var resized = new Bitmap(bmp, new Size(CaptureWidth, CaptureHeight)))
                            using (var ms = new MemoryStream())
                            {
                                resized.Save(ms, ImageFormat.Png);
                                tcs.SetResult(Convert.ToBase64String(ms.ToArray()));
                            }
                        }
                    }
                    catch (Exception ex) { LogMsg("[REMOTE] capture err: " + ex.Message); tcs.SetResult(null); }
                }));
            }
            catch { tcs.SetResult(null); }
            return tcs.Task;
        }

        private void LogMsg(string s) { try { Log?.Invoke(s); } catch { } }

        public void Dispose() => Stop();
    }
}
