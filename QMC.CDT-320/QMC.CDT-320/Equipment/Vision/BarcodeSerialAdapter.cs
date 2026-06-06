using System;
using System.Threading.Tasks;
using System.IO.Ports;
using QMC.CDT320.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Stage 43 — CDT-310 매뉴얼 사양: Wafer/Bin Barcode Communicator (Serial Port).
    /// IBarcodeReader 를 시리얼 포트로 어댑팅. 미연결 시 안전 fallback.
    /// </summary>
    public class BarcodeSerialAdapter : IBarcodeReader, IDisposable
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private SerialPort _port;

        /// <summary>마지막 읽기 결과 (캐시).</summary>
        public string LastReadId { get; private set; } = "";

        public BarcodeSerialAdapter(string portName, int baudRate = 9600)
        {
            _portName = portName;
            _baudRate = baudRate;
        }

        public bool TryOpen()
        {
            try
            {
                if (_port != null && _port.IsOpen) return true;
                _port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One);
                _port.ReadTimeout  = 3000;
                _port.WriteTimeout = 1000;
                _port.NewLine      = "\r\n";
                _port.Open();
                EventLogger.Write(EventKind.Event, "SYS", "BARCODE",
                    $"{_portName}@{_baudRate} OPEN OK");
                return true;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Event, "SYS", "BARCODE",
                    $"{_portName} OPEN failed: {ex.Message}");
                return false;
            }
        }

        public Task<string> ReadAsync(int timeoutMs = 3000)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_port == null || !_port.IsOpen)
                    {
                        if (!TryOpen()) return "WAFER-NULL-ID";
                    }
                    _port.ReadTimeout = timeoutMs;
                    // 트리거 전송 (장비 모델에 맞게 수정)
                    _port.WriteLine("READ?");
                    string id = _port.ReadLine().Trim();
                    LastReadId = id;
                    return string.IsNullOrEmpty(id) ? "WAFER-NULL-ID" : id;
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Event, "SYS", "BARCODE",
                        $"{_portName} READ timeout/err: {ex.Message}");
                    return "WAFER-NULL-ID";
                }
            });
        }

        public void Dispose()
        {
            try { _port?.Close(); } catch { }
            _port = null;
        }
    }
}
