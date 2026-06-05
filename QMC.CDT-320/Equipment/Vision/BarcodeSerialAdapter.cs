using System;
using System.Threading.Tasks;
using System.IO.Ports;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Stage 43 ??CDT-310 留ㅻ돱???ъ뼇: Wafer/Bin Barcode Communicator (Serial Port).
    /// IBarcodeReader 瑜??쒕━???ы듃濡??대뙌?? 誘몄뿰寃????덉쟾 fallback.
    /// </summary>
    public class BarcodeSerialAdapter : IBarcodeReader, IDisposable
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private SerialPort _port;

        /// <summary>留덉?留??쎄린 寃곌낵 (罹먯떆).</summary>
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
                    // ?몃━嫄??꾩넚 (?λ퉬 紐⑤뜽??留욊쾶 ?섏젙)
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

