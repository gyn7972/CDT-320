using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace QMC.RemoteViewer

{
    /// <summary>
    /// 원격 화면을 표시하는 커스텀 UI 컨트롤
    /// </summary>
    public class RemoteViewerControl : Control
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected;
        private Bitmap _displayImage; // 화면에 표시할 최종 이미지 (화면 크기에 맞춰진 상태)
        private readonly object _imageLock = new object();
        private bool _pendingInvalidate = false;

        public event EventHandler<string> StatusChanged;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// 이미지 크기에 맞춰 자동 조절할지 여부
        /// </summary>
        public bool AutoSizeToImage { get; set; } = false;

        /// <summary>
        /// 이미지를 컨트롤 크기에 맞춰 늘릴지 여부
        /// </summary>
        public bool StretchImage { get; set; } = true;

        public RemoteViewerControl()
        {
            // 더블 버퍼링으로 깜빡임 방지
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                   ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.Opaque, true);
            this.UpdateStyles();

            this.BackColor = Color.Black;
        }

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public void Connect(string serverAddress, int port)
        {
            if (_isConnected)
            {
                Disconnect();
            }

            try
            {
                _client = new TcpClient();
                _client.ReceiveBufferSize = 512 * 1024; // 512KB로 증가
                _client.NoDelay = true;
                _client.Connect(serverAddress, port);
                _stream = _client.GetStream();
                _isConnected = true;

                OnStatusChanged($"서버에 연결됨: {serverAddress}:{port}");
                OnConnected();

                // 수신 스레드 시작
                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.IsBackground = true;
                _receiveThread.Priority = ThreadPriority.Highest; // 최고 우선순위
                _receiveThread.Start();
            }
            catch (Exception ex)
            {
                OnStatusChanged($"연결 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 서버 연결 해제
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;

            // 수신 스레드 종료 대기
            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1000);
            }

            _stream?.Close();
            _client?.Close();

            lock (_imageLock)
            {
                _displayImage?.Dispose();
                _displayImage = null;
            }

            OnStatusChanged("연결 해제됨");
            OnDisconnected();

            // 화면 갱신
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.Invalidate()));
            }
            else
            {
                this.Invalidate();
            }
        }

        private void ReceiveLoop()
        {
            byte[] sizeBuffer = new byte[4];

            while (_isConnected && _client.Connected)
            {
                try
                {
                    // 데이터 크기 수신
                    int bytesRead = ReadExactly(_stream, sizeBuffer, 0, 4);
                    if (bytesRead != 4)
                    {
                        break;
                    }

                    int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
                    if (dataSize <= 0 || dataSize > 10 * 1024 * 1024) // 10MB 제한
                    {
                        break;
                    }

                    // 이미지 데이터 수신
                    byte[] imageData = new byte[dataSize];
                    bytesRead = ReadExactly(_stream, imageData, 0, dataSize);
                    if (bytesRead != dataSize)
                    {
                        break;
                    }

                    // 이미지 생성 및 화면 크기에 맞게 리사이즈
                    using (MemoryStream ms = new MemoryStream(imageData))
                    using (Bitmap receivedImage = new Bitmap(ms))
                    {
                        Bitmap displayBitmap = null;

                        if (StretchImage && this.Width > 0 && this.Height > 0)
                        {
                            // 스케일 비율 계산
                            double dRatioW = ((double)this.Width) / receivedImage.Width;
                            double dRatioH = ((double)this.Height) / receivedImage.Height;
                            double dRatio = Math.Min(dRatioW, dRatioH);

                            int targetWidth = (int)(receivedImage.Width * dRatio);
                            int targetHeight = (int)(receivedImage.Height * dRatio);

                            // 화면 크기에 맞춰진 비트맵 생성
                            displayBitmap = new Bitmap(this.Width, this.Height, PixelFormat.Format24bppRgb);
                            using (Graphics g = Graphics.FromImage(displayBitmap))
                            {
                                // 배경을 검은색으로
                                g.Clear(Color.Black);

                                // 고속 렌더링 설정
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.CompositingQuality = CompositingQuality.HighSpeed;
                                g.InterpolationMode = InterpolationMode.Low;
                                g.SmoothingMode = SmoothingMode.None;
                                g.PixelOffsetMode = PixelOffsetMode.Half;

                                // 중앙에 이미지 그리기
                                int x = (this.Width - targetWidth) / 2;
                                int y = (this.Height - targetHeight) / 2;
                                g.DrawImage(receivedImage, x, y, targetWidth, targetHeight);
                            }
                        }
                        else
                        {
                            // 원본 크기 그대로 사용
                            displayBitmap = new Bitmap(receivedImage);
                        }

                        lock (_imageLock)
                        {
                            _displayImage?.Dispose();
                            _displayImage = displayBitmap;
                        }

                        // UI 스레드에서 다시 그리기 (중복 호출 방지)
                        if (!_pendingInvalidate)
                        {
                            _pendingInvalidate = true;

                            if (this.InvokeRequired)
                            {
                                this.BeginInvoke(new Action(() =>
                                       {
                                           _pendingInvalidate = false;
                                           if (AutoSizeToImage)
                                           {
                                               lock (_imageLock)
                                               {
                                                   if (_displayImage != null)
                                                   {
                                                       this.Size = _displayImage.Size;
                                                   }
                                               }
                                           }
                                           this.Invalidate();
                                       }));
                            }
                            else
                            {
                                _pendingInvalidate = false;
                                if (AutoSizeToImage)
                                {
                                    lock (_imageLock)
                                    {
                                        if (_displayImage != null)
                                        {
                                            this.Size = _displayImage.Size;
                                        }
                                    }
                                }
                                this.Invalidate();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_isConnected)
                    {
                        OnStatusChanged($"수신 오류: {ex.Message}");
                        break;
                    }
                }
            }

            // 연결 해제
            if (_isConnected)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 정확한 바이트 수만큼 읽기
        /// </summary>
        private int ReadExactly(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    return totalRead; // 연결 종료
                }
                totalRead += read;
            }
            return totalRead;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // 크기가 변경되면 다음 프레임에서 자동으로 새 크기에 맞춰 리사이즈됨
            // 기존 이미지는 그대로 표시
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // base.OnPaint 생략 (Opaque 모드이므로 불필요)

            Graphics g = e.Graphics;

            // 최고속 렌더링 설정
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.Low;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            lock (_imageLock)
            {
                if (_displayImage != null)
                {
                    // 이미 화면 크기에 맞춰진 이미지를 그대로 그리기 (가장 빠름)
                    g.DrawImageUnscaled(_displayImage, 0, 0);
                }
                else
                {
                    // 배경 지우기
                    g.Clear(Color.Black);

                    // 텍스트 렌더링을 위해 CompositingMode 변경
                    g.CompositingMode = CompositingMode.SourceOver;

                    // 연결되지 않았을 때 메시지 표시
                    string message = _isConnected ? "이미지 수신 대기 중..." : "연결되지 않음";
                    using (Font font = new Font("맑은 고딕", 12))
                    {
                        SizeF textSize = g.MeasureString(message, font);
                        PointF textPos = new PointF(
                          (this.Width - textSize.Width) / 2,
                               (this.Height - textSize.Height) / 2
                               );
                        g.DrawString(message, font, Brushes.White, textPos);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();

                lock (_imageLock)
                {
                    _displayImage?.Dispose();
                    _displayImage = null;
                }
            }
            base.Dispose(disposing);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        protected virtual void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
