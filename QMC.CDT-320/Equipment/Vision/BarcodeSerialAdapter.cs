using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Wafer/Bin 바코드 리더용 Serial Port 어댑터입니다.
    /// TriggerCommand가 비어 있으면 별도 명령 없이 리더가 송신하는 값을 대기합니다.
    /// </summary>
    public sealed class BarcodeSerialAdapter : IBarcodeReader, IDisposable
    {
        private readonly object _portSync = new object();
        private readonly SemaphoreSlim _readGate = new SemaphoreSlim(1, 1);
        private readonly string _readerName;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly string _triggerCommand;
        private SerialPort _port;

        public BarcodeSerialAdapter(string portName, int baudRate = 9600)
            : this("BARCODE", portName, baudRate, "READ?")
        {
        }

        public BarcodeSerialAdapter(
            string readerName,
            string portName,
            int baudRate,
            string triggerCommand)
        {
            _readerName = string.IsNullOrWhiteSpace(readerName) ? "BARCODE" : readerName.Trim();
            _portName = string.IsNullOrWhiteSpace(portName) ? "" : portName.Trim();
            _baudRate = baudRate > 0 ? baudRate : 9600;
            _triggerCommand = triggerCommand ?? "";
        }

        public string ReaderName => _readerName;

        public bool IsConnected
        {
            get
            {
                lock (_portSync)
                    return _port != null && _port.IsOpen;
            }
        }

        /// <summary>마지막 정상 읽기 결과입니다. 실패 시에는 기존 값을 덮어쓰지 않습니다.</summary>
        public string LastReadId { get; private set; } = "";

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

        public Task<string> ReadAsync(int timeoutMs = 3000)
        {
            int safeTimeoutMs = Math.Max(100, timeoutMs);
            return Task.Run(() =>
            {
                _readGate.Wait();
                try
                {
                    lock (_portSync)
                    {
                        if ((_port == null || !_port.IsOpen) && !TryOpen())
                            return "";

                        _port.ReadTimeout = safeTimeoutMs;
                        if (!string.IsNullOrWhiteSpace(_triggerCommand))
                            _port.WriteLine(_triggerCommand.Trim());

                        string id = ReadPayloadNoLock();
                        if (string.IsNullOrWhiteSpace(id) ||
                            string.Equals(id, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase))
                        {
                            return "";
                        }

                        LastReadId = id;
                        return id;
                    }
                }
                catch (Exception ex)
                {
                    EventLogger.Write(
                        EventKind.Warning,
                        "SYS",
                        "BARCODE",
                        ReaderName + " " + _portName + " READ timeout/err: " + ex.Message);
                    return "";
                }
                finally
                {
                    _readGate.Release();
                }
            });
        }

        private string ReadPayloadNoLock()
        {
            var payload = new StringBuilder();
            while (true)
            {
                int value = _port.ReadChar();
                if (value < 0)
                    return "";

                char ch = (char)value;
                if (ch == '\x02')
                    continue;
                if (ch == '\x03' || ch == '\r' || ch == '\n')
                {
                    if (payload.Length == 0)
                        continue;
                    break;
                }
                payload.Append(ch);
            }

            return NormalizePayload(payload.ToString());
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
            _readGate.Dispose();
        }
    }
}
