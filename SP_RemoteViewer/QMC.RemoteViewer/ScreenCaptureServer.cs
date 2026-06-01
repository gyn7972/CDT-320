using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.RemoteViewer
{
    /// <summary>
    /// 화면을 캡처하고 TCP/IP로 전송하는 서버 (1:N 연결 지원)
    /// </summary>
    public class ScreenCaptureServer
    {
        private class ClientConnection
        {
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
            public Thread SendThread { get; set; }
            public string EndPoint { get; set; }
            public bool IsActive { get; set; }
            public long LastFrameId { get; set; } = -1; // 마지막으로 전송한 프레임 ID
            public ManualResetEventSlim FrameAvailable { get; set; } = new ManualResetEventSlim(false);
        }

        private TcpListener _listener;
        private List<ClientConnection> _clients = new List<ClientConnection>();
        private object _clientsLock = new object();
        private bool _isRunning;
        private Thread _acceptThread;
        private Thread _captureThread;
        private int _port;
        private int _fps = 10; // 초당 프레임 수
        private long _quality = 50L; // JPEG 압축 품질 (1-100)
        private int _screenIndex = 0; // 캡처할 모니터 인덱스

        private byte[] _latestFrame = null;
        private long _frameId = 0; // 프레임 ID
        private object _frameLock = new object();

        public event EventHandler<string> StatusChanged;
        public event EventHandler<string> ClientConnected;
        public event EventHandler ClientDisconnected;

        public bool IsRunning => _isRunning;
        public int Port => _port;
        public int ConnectedClientsCount
        {
            get
            {
                lock (_clientsLock)
                {
                    return _clients.Count(c => c.IsActive);
                }
            }
        }

        /// <summary>
        /// JPEG 압축 품질 (1-100)
        /// </summary>
        public long Quality
        {
            get => _quality;
            set => _quality = Math.Max(1, Math.Min(100, value));
        }

        /// <summary>
        /// 초당 전송 프레임 수
        /// </summary>
        public int FPS
        {
            get => _fps;
            set => _fps = Math.Max(1, Math.Min(60, value));
        }

        /// <summary>
        /// 캡처할 모니터 인덱스 (0부터 시작)
        /// </summary>
        public int ScreenIndex
        {
            get => _screenIndex;
            set
            {
                int screenCount = System.Windows.Forms.Screen.AllScreens.Length;
                _screenIndex = Math.Max(0, Math.Min(value, screenCount - 1));
            }
        }

        public ScreenCaptureServer(int port = 8888)
        {
            _port = port;
        }

        /// <summary>
        /// 사용 가능한 모든 모니터 정보 가져오기
        /// </summary>
        public static System.Windows.Forms.Screen[] GetAllScreens()
        {
            return System.Windows.Forms.Screen.AllScreens;
        }

        /// <summary>
        /// 서버 시작
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;

                var selectedScreen = System.Windows.Forms.Screen.AllScreens[_screenIndex];
                OnStatusChanged($"서버 시작됨 - 포트: {_port}, 모니터: {_screenIndex + 1} ({selectedScreen.Bounds.Width}x{selectedScreen.Bounds.Height})");

                // 화면 캡처 스레드 시작
                _captureThread = new Thread(CaptureLoop);
                _captureThread.IsBackground = true;
                _captureThread.Priority = ThreadPriority.AboveNormal; // 캡처 우선순위 상승
                _captureThread.Start();

                // 클라이언트 연결 수락 스레드 시작
                _acceptThread = new Thread(AcceptClientsLoop);
                _acceptThread.IsBackground = true;
                _acceptThread.Start();
            }
            catch (Exception ex)
            {
                OnStatusChanged($"서버 시작 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 서버 중지
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            // 리스너 중지
            _listener?.Stop();

            // 캡처 스레드 중지
            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join(1000);
            }

            // 수락 스레드 중지
            if (_acceptThread != null && _acceptThread.IsAlive)
            {
                _acceptThread.Join(1000);
            }

            // 모든 클라이언트 연결 종료
            lock (_clientsLock)
            {
                foreach (var client in _clients)
                {
                    DisconnectClient(client);
                }
                _clients.Clear();
            }

            OnStatusChanged("서버 중지됨");
        }

        private void AcceptClientsLoop()
        {
            while (_isRunning)
            {
                try
                {
                    OnStatusChanged($"클라이언트 연결 대기 중... (현재 연결: {ConnectedClientsCount}명)");
                    
                    TcpClient tcpClient = _listener.AcceptTcpClient();
                    
                    // TCP 버퍼 설정 최적화
                    tcpClient.SendBufferSize = 256 * 1024; // 256KB
                    tcpClient.NoDelay = true; // Nagle 알고리즘 비활성화 (지연 감소)
                    
                    string clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();

                    var clientConnection = new ClientConnection
                    {
                        Client = tcpClient,
                        Stream = tcpClient.GetStream(),
                        EndPoint = clientEndPoint,
                        IsActive = true
                    };

                    lock (_clientsLock)
                    {
                        _clients.Add(clientConnection);
                    }

                    OnClientConnected(clientEndPoint);
                    OnStatusChanged($"클라이언트 연결됨: {clientEndPoint} (총 {ConnectedClientsCount}명)");

                    // 각 클라이언트에게 프레임 전송하는 스레드 시작
                    clientConnection.SendThread = new Thread(() => SendToClientLoop(clientConnection));
                    clientConnection.SendThread.IsBackground = true;
                    clientConnection.SendThread.Start();
                }
                catch (SocketException)
                {
                    // 리스너가 중지되면 발생
                    if (_isRunning)
                    {
                        OnStatusChanged("클라이언트 수락 중단됨");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnStatusChanged($"클라이언트 수락 오류: {ex.Message}");
                    }
                }
            }
        }

        private void CaptureLoop()
        {
            int interval = 1000 / _fps;

            while (_isRunning)
            {
                try
                {
                    DateTime startTime = DateTime.Now;

                    // 화면 캡처
                    using (Bitmap screenshot = CaptureScreen())
                    {
                        // JPEG로 압축
                        byte[] imageData = CompressImage(screenshot, _quality);

                        // 최신 프레임 저장 및 프레임 ID 증가
                        lock (_frameLock)
                        {
                            _latestFrame = imageData;
                            _frameId++;
                        }

                        // 모든 클라이언트에게 새 프레임 알림
                        lock (_clientsLock)
                        {
                            foreach (var client in _clients)
                            {
                                if (client.IsActive)
                                {
                                    client.FrameAvailable.Set();
                                }
                            }
                        }
                    }

                    // FPS 유지를 위한 대기
                    int elapsed = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    int sleepTime = Math.Max(0, interval - elapsed);
                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnStatusChanged($"캡처 오류: {ex.Message}");
                    }
                }
            }
        }

        private void SendToClientLoop(ClientConnection clientConnection)
        {
            try
            {
                while (_isRunning && clientConnection.IsActive && clientConnection.Client.Connected)
                {
                    // 새 프레임이 준비될 때까지 대기 (타임아웃 1초)
                    if (!clientConnection.FrameAvailable.Wait(1000))
                    {
                        continue;
                    }

                    clientConnection.FrameAvailable.Reset();

                    byte[] frameToSend = null;
                    long currentFrameId = 0;

                    // 최신 프레임 가져오기
                    lock (_frameLock)
                    {
                        currentFrameId = _frameId;
                        
                        // 이미 전송한 프레임이면 건너뛰기
                        if (currentFrameId <= clientConnection.LastFrameId)
                        {
                            continue;
                        }

                        if (_latestFrame != null)
                        {
                            frameToSend = _latestFrame;
                        }
                    }

                    if (frameToSend != null)
                    {
                        try
                        {
                            // 데이터 크기 전송 (4 바이트)
                            byte[] sizeBytes = BitConverter.GetBytes(frameToSend.Length);
                            clientConnection.Stream.Write(sizeBytes, 0, sizeBytes.Length);

                            // 이미지 데이터 전송
                            clientConnection.Stream.Write(frameToSend, 0, frameToSend.Length);
                            clientConnection.Stream.Flush();

                            // 전송 완료된 프레임 ID 업데이트
                            clientConnection.LastFrameId = currentFrameId;
                        }
                        catch (Exception ex)
                        {
                            OnStatusChanged($"클라이언트 {clientConnection.EndPoint} 전송 오류: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"클라이언트 {clientConnection.EndPoint} 처리 오류: {ex.Message}");
            }
            finally
            {
                // 연결 종료
                lock (_clientsLock)
                {
                    if (_clients.Contains(clientConnection))
                    {
                        DisconnectClient(clientConnection);
                        _clients.Remove(clientConnection);
                        OnClientDisconnected();
                        OnStatusChanged($"클라이언트 연결 종료: {clientConnection.EndPoint} (남은 연결: {ConnectedClientsCount}명)");
                    }
                }
            }
        }

        private void DisconnectClient(ClientConnection client)
        {
            client.IsActive = false;
            
            // 대기 중인 스레드 깨우기
            client.FrameAvailable?.Set();
            
            try
            {
                client.Stream?.Close();
            }
            catch { }

            try
            {
                client.Client?.Close();
            }
            catch { }

            if (client.SendThread != null && client.SendThread.IsAlive)
            {
                client.SendThread.Join(500);
            }

            client.FrameAvailable?.Dispose();
        }

        /// <summary>
        /// 화면 캡처 (선택된 모니터)
        /// </summary>
        private Bitmap CaptureScreen()
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            
            // 유효한 모니터 인덱스 확인
            if (_screenIndex < 0 || _screenIndex >= screens.Length)
            {
                _screenIndex = 0;
            }

            System.Windows.Forms.Screen selectedScreen = screens[_screenIndex];
            Rectangle bounds = selectedScreen.Bounds;
            int w = 680;
            int h = 680;
            Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.X + 288, bounds.Y + 16 , 0, 0,new Size(w,h), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        /// <summary>
        /// 이미지를 JPEG로 압축
        /// </summary>
        private byte[] CompressImage(Bitmap image, long quality)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // JPEG 인코더 설정
                ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                image.Save(ms, jpegCodec, encoderParams);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 이미지 코덱 가져오기
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        protected virtual void OnClientConnected(string clientInfo)
        {
            ClientConnected?.Invoke(this, clientInfo);
        }

        protected virtual void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
