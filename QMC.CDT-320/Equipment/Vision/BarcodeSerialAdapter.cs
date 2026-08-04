using System;
using System.IO.Ports;
using System.Text;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// 바코드 리더 프로토콜이 사용하는 Serial Port 바이트 입출력 어댑터입니다.
    /// 장비별 명령 프레임과 응답 해석은 처리하지 않습니다.
    /// </summary>
    public sealed class BarcodeSerialAdapter : IBarcodeSerialTransport
    {
        private readonly object _portSync = new object();
        private readonly string _readerName;
        private readonly string _portName;
        private readonly int _baudRate;
        private SerialPort _port;

        public BarcodeSerialAdapter(string portName, int baudRate = 9600)
            : this("BARCODE", portName, baudRate)
        {
        }

        public BarcodeSerialAdapter(
            string readerName,
            string portName,
            int baudRate)
        {
            _readerName = string.IsNullOrWhiteSpace(readerName) ? "BARCODE" : readerName.Trim();
            _portName = string.IsNullOrWhiteSpace(portName) ? "" : portName.Trim();
            _baudRate = baudRate > 0 ? baudRate : 9600;
        }

        public string ReaderName => _readerName;

        public bool IsOpen
        {
            get
            {
                lock (_portSync)
                    return _port != null && _port.IsOpen;
            }
        }

        public bool TryOpen()
        {
            lock (_portSync)
            {
                try
                {
                    if (_port != null && _port.IsOpen)
                        return true;
                    if (string.IsNullOrWhiteSpace(_portName))
                        return false;

                    CloseNoLock();
                    _port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 3000,
                        WriteTimeout = 1000,
                        NewLine = "\r\n"
                    };
                    _port.Open();
                    EventLogger.Write(
                        EventKind.Event,
                        "SYS",
                        "BARCODE",
                        ReaderName + " " + _portName + "@" + _baudRate + " OPEN OK");
                    return true;
                }
                catch (Exception ex)
                {
                    CloseNoLock();
                    EventLogger.Write(
                        EventKind.Warning,
                        "SYS",
                        "BARCODE",
                        ReaderName + " " + _portName + " OPEN failed: " + ex.Message);
                    return false;
                }
            }
        }

        public void DiscardInputBuffer()
        {
            lock (_portSync)
            {
                EnsureOpenNoLock();
                _port.DiscardInBuffer();
            }
        }

        public void Write(byte[] packet)
        {
            if (packet == null || packet.Length == 0)
                throw new ArgumentException("송신 패킷이 비어 있습니다.", nameof(packet));

            lock (_portSync)
            {
                EnsureOpenNoLock();
                _port.Write(packet, 0, packet.Length);
            }
        }

        public int ReadByte(int timeoutMs)
        {
            lock (_portSync)
            {
                EnsureOpenNoLock();
                _port.ReadTimeout = Math.Max(100, timeoutMs);
                return _port.ReadByte();
            }
        }

        private void EnsureOpenNoLock()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException(ReaderName + " 시리얼 포트가 열려 있지 않습니다.");
        }

        public static string NormalizePayload(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var normalized = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\x02' || ch == '\x03' || char.IsWhiteSpace(ch))
                    continue;
                if (char.IsControl(ch))
                    return "";
                normalized.Append(ch);
            }
            return normalized.ToString();
        }

        public void Close()
        {
            lock (_portSync)
                CloseNoLock();
        }

        private void CloseNoLock()
        {
            SerialPort port = _port;
            _port = null;
            if (port == null)
                return;

            try { if (port.IsOpen) port.Close(); } catch { }
            try { port.Dispose(); } catch { }
        }

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// 장비 프로토콜과 SerialPort 구현을 분리하기 위한 바이트 입출력 계약입니다.
    /// </summary>
    public interface IBarcodeSerialTransport : IDisposable
    {
        bool IsOpen { get; }
        bool TryOpen();
        void Close();
        void DiscardInputBuffer();
        void Write(byte[] packet);
        int ReadByte(int timeoutMs);
    }
}
