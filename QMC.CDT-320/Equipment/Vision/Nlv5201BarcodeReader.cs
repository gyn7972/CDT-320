using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// NLV-5201 명령 트리거 방식의 바코드 리더입니다.
    /// 판독 시작(ESC Z CR)과 정지(ESC Y CR)를 매뉴얼 순서대로 보장합니다.
    /// </summary>
    public sealed class Nlv5201BarcodeReader : IBarcodeReader, IDisposable
    {
        private const byte Acknowledge = 0x06;
        private const byte NegativeAcknowledge = 0x15;
        private const byte StartOfText = 0x02;
        private const byte EndOfText = 0x03;
        private const int MaxPayloadBytes = 16384;

        private readonly SemaphoreSlim _readGate = new SemaphoreSlim(1, 1);
        private readonly string _readerName;
        private readonly IBarcodeSerialTransport _transport;
        private readonly Nlv5201Command _startCommand;

        public Nlv5201BarcodeReader(
            string readerName,
            string portName,
            int baudRate,
            string triggerCommand)
            : this(
                readerName,
                new BarcodeSerialAdapter(readerName, portName, baudRate),
                ResolveStartCommandOrDefault(readerName, triggerCommand))
        {
        }

        public Nlv5201BarcodeReader(
            string readerName,
            IBarcodeSerialTransport transport,
            Nlv5201Command startCommand)
        {
            _readerName = string.IsNullOrWhiteSpace(readerName) ? "NLV-5201" : readerName.Trim();
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _startCommand = ValidateStartCommand(startCommand);
        }

        public string ReaderName => _readerName;
        public bool IsConnected => _transport.IsOpen;

        /// <summary>마지막 정상 판독 결과입니다. 실패 시 기존 값을 덮어쓰지 않습니다.</summary>
        public string LastReadId { get; private set; } = "";

        public bool TryOpen()
        {
            return _transport.TryOpen();
        }

        public void Close()
        {
            _transport.Close();
        }

        public Task<string> ReadAsync(int timeoutMs = 3000)
        {
            int safeTimeoutMs = Math.Max(100, timeoutMs);
            return Task.Run(() => ReadCore(safeTimeoutMs));
        }

        public Task<string> GetSoftwareVersionAsync(int timeoutMs = 1000)
        {
            return QueryDiagnosticLineAsync(Nlv5201Commands.TransmitSoftwareVersion, timeoutMs);
        }

        public Task<string> GetCurrentBankAsync(int timeoutMs = 1000)
        {
            return QueryDiagnosticLineAsync(Nlv5201Commands.ConfirmCurrentBank, timeoutMs);
        }

        public Task<string[]> GetTuningExposureRangeAsync(int timeoutMs = 2000)
        {
            int safeTimeoutMs = Math.Max(100, timeoutMs);
            return Task.Run(() => QueryDiagnosticLinesCore(
                Nlv5201Commands.OutputTuningExposureRange,
                2,
                safeTimeoutMs,
                null));
        }

        public Task<string[]> RunReadingTestAsync(int sampleCount, int timeoutMs = 5000)
        {
            if (sampleCount < 1 || sampleCount > 100)
                throw new ArgumentOutOfRangeException(nameof(sampleCount), "NLV-5201 판독 성능시험 표본 수는 1~100이어야 합니다.");

            int safeTimeoutMs = Math.Max(100, timeoutMs);
            return Task.Run(() => QueryDiagnosticLinesCore(
                Nlv5201Commands.StartReadingTest,
                sampleCount,
                safeTimeoutMs,
                Nlv5201Commands.StopReadingTest));
        }

        private string ReadCore(int timeoutMs)
        {
            _readGate.Wait();
            bool startSent = false;
            try
            {
                if (!_transport.IsOpen && !_transport.TryOpen())
                    return "";

                _transport.DiscardInputBuffer();
                WriteCommand(_startCommand, "START");
                startSent = true;

                byte[] payloadBytes = ReadPayloadBytes(timeoutMs);
                string id = Nlv5201ResponseParser.ParseBarcodePayload(payloadBytes);
                if (string.IsNullOrWhiteSpace(id) ||
                    string.Equals(id, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase))
                {
                    return "";
                }

                LastReadId = id;
                EventLogger.Write(
                    EventKind.Event,
                    "SYS",
                    "BARCODE",
                    ReaderName + " RX HEX=" + Nlv5201CommandCodec.ToHex(payloadBytes) +
                    " DATA=" + ToLogText(id));
                return id;
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Warning,
                    "SYS",
                    "BARCODE",
                    ReaderName + " NLV-5201 READ timeout/err: " + ex.Message);
                return "";
            }
            finally
            {
                if (startSent)
                    TryStopReading();
                _readGate.Release();
            }
        }

        private byte[] ReadPayloadBytes(int timeoutMs)
        {
            var payload = new List<byte>();
            var elapsed = Stopwatch.StartNew();

            while (true)
            {
                int remainingMs = timeoutMs - (int)elapsed.ElapsedMilliseconds;
                if (remainingMs <= 0)
                    throw new TimeoutException("NLV-5201 바코드 수신 시간을 초과했습니다.");

                int value = _transport.ReadByte(remainingMs);
                if (value < 0)
                    throw new InvalidOperationException("NLV-5201 시리얼 포트가 수신 종료를 반환했습니다.");

                byte received = (byte)value;
                if (payload.Count == 0 && received == Acknowledge)
                    continue;
                if (payload.Count == 0 && received == NegativeAcknowledge)
                    throw new InvalidOperationException("NLV-5201이 Trigger 명령을 NAK로 거부했습니다.");
                if (payload.Count == 0 && received == StartOfText)
                    continue;

                if (received == EndOfText || received == (byte)'\r' || received == (byte)'\n')
                {
                    if (payload.Count == 0)
                        continue;
                    break;
                }

                payload.Add(received);
                if (payload.Count > MaxPayloadBytes)
                    throw new InvalidOperationException("NLV-5201 수신 데이터가 최대 허용 길이를 초과했습니다.");
            }

            return payload.ToArray();
        }

        private Task<string> QueryDiagnosticLineAsync(Nlv5201Command command, int timeoutMs)
        {
            int safeTimeoutMs = Math.Max(100, timeoutMs);
            return Task.Run(() =>
            {
                string[] lines = QueryDiagnosticLinesCore(command, 1, safeTimeoutMs, null);
                return lines.Length > 0 ? lines[0] : "";
            });
        }

        private string[] QueryDiagnosticLinesCore(
            Nlv5201Command startCommand,
            int lineCount,
            int timeoutMs,
            Nlv5201Command stopCommand)
        {
            if (startCommand == null || startCommand.Risk != Nlv5201CommandRisk.Diagnostic)
                throw new ArgumentException("NLV-5201 진단 API에는 Diagnostic 명령만 사용할 수 있습니다.", nameof(startCommand));

            _readGate.Wait();
            bool startSent = false;
            try
            {
                if (!_transport.IsOpen && !_transport.TryOpen())
                    return new string[0];

                _transport.DiscardInputBuffer();
                WriteCommand(startCommand, "DIAGNOSTIC");
                startSent = true;

                var results = new List<string>();
                var totalElapsed = Stopwatch.StartNew();
                for (int index = 0; index < lineCount; index++)
                {
                    int remainingMs = timeoutMs - (int)totalElapsed.ElapsedMilliseconds;
                    if (remainingMs <= 0)
                        throw new TimeoutException("NLV-5201 진단 응답 시간을 초과했습니다.");

                    byte[] response = ReadPayloadBytes(remainingMs);
                    results.Add(Nlv5201ResponseParser.ParseBarcodePayload(response));
                }
                return results.ToArray();
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Warning,
                    "SYS",
                    "BARCODE",
                    ReaderName + " NLV-5201 DIAGNOSTIC timeout/err CMD=" + startCommand.Id + ": " + ex.Message);
                return new string[0];
            }
            finally
            {
                if (startSent && stopCommand != null)
                {
                    try
                    {
                        if (_transport.IsOpen)
                            WriteCommand(stopCommand, "DIAGNOSTIC-STOP");
                    }
                    catch (Exception ex)
                    {
                        EventLogger.Write(
                            EventKind.Warning,
                            "SYS",
                            "BARCODE",
                            ReaderName + " NLV-5201 DIAGNOSTIC STOP 송신 실패: " + ex.Message);
                    }
                }
                _readGate.Release();
            }
        }

        private void WriteCommand(Nlv5201Command command, string operation)
        {
            byte[] packet = Nlv5201CommandCodec.Encode(command);
            _transport.Write(packet);
            EventLogger.Write(
                EventKind.Event,
                "SYS",
                "BARCODE",
                ReaderName + " NLV-5201 TX " + operation +
                " CMD=" + command.Id +
                " HEX=" + Nlv5201CommandCodec.ToHex(packet));
        }

        private void TryStopReading()
        {
            try
            {
                if (_transport.IsOpen)
                    WriteCommand(Nlv5201Commands.StopReading, "STOP");
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Warning,
                    "SYS",
                    "BARCODE",
                    ReaderName + " NLV-5201 STOP 송신 실패: " + ex.Message);
            }
        }

        public static bool TryValidateTriggerCommand(string triggerCommand, out string error)
        {
            try
            {
                ResolveStartCommand(triggerCommand);
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static Nlv5201Command ResolveStartCommandOrDefault(string readerName, string triggerCommand)
        {
            try
            {
                return ResolveStartCommand(triggerCommand);
            }
            catch (Exception ex)
            {
                EventLogger.Write(
                    EventKind.Warning,
                    "SYS",
                    "BARCODE",
                    (readerName ?? "NLV-5201") + " Trigger 설정이 잘못되어 기본 Z 명령을 사용합니다: " + ex.Message);
                return Nlv5201Commands.StartReading;
            }
        }

        private static Nlv5201Command ResolveStartCommand(string triggerCommand)
        {
            string commandId = (triggerCommand ?? "").Trim();
            if (commandId.Length == 0 || string.Equals(commandId, "Z", StringComparison.OrdinalIgnoreCase))
                return Nlv5201Commands.StartReading;

            for (int bank = 1; bank <= 7; bank++)
            {
                Nlv5201Command bankTrigger = Nlv5201Commands.TriggerBank(bank);
                if (string.Equals(commandId, bankTrigger.Id, StringComparison.OrdinalIgnoreCase))
                    return bankTrigger;
            }

            throw new ArgumentException(
                "NLV-5201 Trigger에는 빈 값, Z 또는 [TRGQ0Q1~[TRGQ0Q7만 사용할 수 있습니다.",
                nameof(triggerCommand));
        }

        private static Nlv5201Command ValidateStartCommand(Nlv5201Command startCommand)
        {
            if (startCommand == null)
                throw new ArgumentNullException(nameof(startCommand));
            if (startCommand.Risk != Nlv5201CommandRisk.Runtime)
                throw new ArgumentException("NLV-5201 판독 시작에는 Runtime 명령만 사용할 수 있습니다.", nameof(startCommand));
            Nlv5201CommandCodec.ValidateCommandId(startCommand.Id);
            return startCommand;
        }

        private static string ToLogText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var text = new StringBuilder();
            foreach (char ch in value.Take(256))
            {
                if (char.IsControl(ch))
                    text.Append("\\x").Append(((int)ch).ToString("X2"));
                else
                    text.Append(ch);
            }
            if (value.Length > 256)
                text.Append("...");
            return text.ToString();
        }

        public void Dispose()
        {
            Close();
            _transport.Dispose();
            _readGate.Dispose();
        }
    }
}
