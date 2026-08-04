using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QMC.CDT320.VisionComm
{
    /// <summary>NLV-5201 명령이 장비 상태에 미치는 영향 수준입니다.</summary>
    public enum Nlv5201CommandRisk
    {
        Runtime,
        Diagnostic,
        ActiveSetting,
        PersistentWrite,
        ConnectionChanging,
        Destructive
    }

    /// <summary>NLV-5201 User's Manual에 정의된 Command ID와 위험도입니다.</summary>
    public sealed class Nlv5201Command
    {
        internal Nlv5201Command(string id, string description, Nlv5201CommandRisk risk)
        {
            Id = id ?? "";
            Description = description ?? "";
            Risk = risk;
        }

        public string Id { get; private set; }
        public string Description { get; private set; }
        public Nlv5201CommandRisk Risk { get; private set; }

        public override string ToString()
        {
            return Id + " - " + Description;
        }
    }

    /// <summary>
    /// NLV-5201 User's Manual 명령 카탈로그입니다.
    /// 일반 판독 경로에서는 Runtime/Diagnostic 명령만 사용하며 저장·초기화 명령은 분리합니다.
    /// </summary>
    public static class Nlv5201Commands
    {
        // Reading and timing (User's Manual 6.1.2)
        public static readonly Nlv5201Command StartReading = Runtime("Z", "명령 트리거 판독 시작");
        public static readonly Nlv5201Command StopReading = Runtime("Y", "명령 트리거 판독 정지");

        // Diagnostics and indicators (3.3)
        public static readonly Nlv5201Command TransmitSoftwareVersion = Diagnostic("Z1", "소프트웨어 버전 송신");
        public static readonly Nlv5201Command TransmitAsciiPrintableString = Diagnostic("ZA", "ASCII 출력 가능 진단 문자열 송신");
        public static readonly Nlv5201Command TransmitAsciiControlString = Diagnostic("YV", "ASCII 제어 진단 문자열 송신");
        public static readonly Nlv5201Command SoundGoodReadBeep = Runtime("B", "정상 판독 부저 출력");
        public static readonly Nlv5201Command SoundErrorBeep = Runtime("E", "오류 부저 출력");
        public static readonly Nlv5201Command FlashStatusLed = Runtime("L", "정상 상태 LED 점멸");
        public static readonly Nlv5201Command FlashErrorStatusLed = Runtime("N", "오류 상태 LED 점멸");

        // Command response and reading controls (3.3)
        public static readonly Nlv5201Command EnableCommandAck = Active("WC", "시리얼 명령 ACK/NAK 활성화");
        public static readonly Nlv5201Command DisableCommandAck = Active("WD", "시리얼 명령 ACK/NAK 비활성화");
        public static readonly Nlv5201Command Enable2DMenuCode = Active("[D1Y", "2D 메뉴 코드 활성화");
        public static readonly Nlv5201Command Disable2DMenuCode = Active("[D1Z", "2D 메뉴 코드 비활성화");
        public static readonly Nlv5201Command EnableCommandTrigger1DMenuCode = Active("[DFBQ2Q1", "명령 트리거의 1D 메뉴 코드 활성화");
        public static readonly Nlv5201Command DisableCommandTrigger1DMenuCode = Active("[DFBQ2Q0", "명령 트리거의 1D 메뉴 코드 비활성화");
        public static readonly Nlv5201Command EnableReadingOperation = Active("[EAT", "리더 판독 동작 활성화");
        public static readonly Nlv5201Command DisableReadingOperation = Active("[EAU", "리더 판독 동작 비활성화");
        public static readonly Nlv5201Command Reboot = ConnectionChanging("RV", "리더 소프트웨어 재부팅");
        public static readonly Nlv5201Command EnableModeKey = Active("[EHBQ1", "Mode Key 활성화");
        public static readonly Nlv5201Command DisableModeKey = Active("[EHBQ0", "Mode Key 비활성화");

        // Settings storage (3.2)
        public static readonly Nlv5201Command SaveStartupSettings = Persistent("Z2", "Active Settings를 Startup Settings에 저장");
        public static readonly Nlv5201Command RestoreRs232FactoryDefaults = Destructive("U2", "RS-232C 공장 기본값 적용");
        public static readonly Nlv5201Command ReadCustomSettings = Active("[BAP", "Custom Settings 불러오기");
        public static readonly Nlv5201Command SaveCustomSettings = Persistent("[BAQ", "Custom Settings 저장");

        // Image orientation (3.3.5)
        public static readonly Nlv5201Command DisableHorizontalMirror = Active("[EFU", "수평 미러 비활성화");
        public static readonly Nlv5201Command EnableHorizontalMirror = Active("[EFV", "수평 미러 활성화");
        public static readonly Nlv5201Command DisableVerticalMirror = Active("[E8J", "수직 미러 비활성화");
        public static readonly Nlv5201Command EnableVerticalMirror = Active("[E8I", "수직 미러 활성화");

        // External trigger (6.1.3)
        public static readonly Nlv5201Command ExternalTriggerHighActive = Active("YA", "외부 Trigger High Active");
        public static readonly Nlv5201Command ExternalTriggerLowActive = Active("YB", "외부 Trigger Low Active");
        public static readonly Nlv5201Command DisableExternalTriggerReception = Active("[EGOQ0", "외부 Trigger 입력 비활성화");
        public static readonly Nlv5201Command EnableExternalTriggerReception = Active("[EGOQ1", "외부 Trigger 입력 활성화");

        // Bank function (7.4)
        public static readonly Nlv5201Command ConfirmCurrentBank = Diagnostic("[DGQ", "현재 Bank 번호 조회");
        public static readonly Nlv5201Command OutputTuningExposureRange = Diagnostic("[DT4", "현재 Tuning 노출 조정 범위 조회");
        public static readonly Nlv5201Command StartReadingTest = Diagnostic(".V", "10회 판독 성능시험 연속 출력 시작");
        public static readonly Nlv5201Command StopReadingTest = Diagnostic(".W", "판독 성능시험 연속 출력 정지");
        public static readonly Nlv5201Command InitializeAllBanks = Destructive("[BRC", "전체 Bank 설정 초기화");

        // RS-232C interface (5.1)
        public static readonly Nlv5201Command SevenDataBits = ConnectionChanging("L0", "RS-232C 7 data bits");
        public static readonly Nlv5201Command EightDataBits = ConnectionChanging("L1", "RS-232C 8 data bits");
        public static readonly Nlv5201Command NoParity = ConnectionChanging("L2", "RS-232C parity 없음");
        public static readonly Nlv5201Command EvenParity = ConnectionChanging("L3", "RS-232C even parity");
        public static readonly Nlv5201Command OddParity = ConnectionChanging("L4", "RS-232C odd parity");
        public static readonly Nlv5201Command OneStopBit = ConnectionChanging("L5", "RS-232C 1 stop bit");
        public static readonly Nlv5201Command TwoStopBits = ConnectionChanging("L6", "RS-232C 2 stop bits");
        public static readonly Nlv5201Command NoHandshake = ConnectionChanging("P0", "RS-232C handshake 없음");
        public static readonly Nlv5201Command BusyReadyHandshake = ConnectionChanging("P1", "RS-232C BUSY/READY handshake");
        public static readonly Nlv5201Command ModemHandshake = ConnectionChanging("P2", "RS-232C MODEM handshake");
        // P3/P4는 WC/WD의 명령 수락 ACK/NAK가 아니라 판독 데이터 송신 handshake입니다.
        // Host ACK/NAK/DC1 응답 구현 없이 적용하면 데이터 송신이 중단될 수 있으므로 일반 UI에서는 노출하지 않습니다.
        public static readonly Nlv5201Command AckNakHandshake = ConnectionChanging("P3", "RS-232C 판독 데이터 ACK/NAK handshake");
        public static readonly Nlv5201Command AckNakNoResponseHandshake = ConnectionChanging("P4", "RS-232C 판독 데이터 ACK/NAK NO RESPONSE handshake");

        public static Nlv5201Command SelectBank(int bank)
        {
            ValidateBank(bank);
            return Active("[BRAQ0Q" + bank, "Bank " + bank + " 선택");
        }

        public static Nlv5201Command TriggerBank(int bank)
        {
            ValidateBank(bank);
            return Runtime("[TRGQ0Q" + bank, "Bank " + bank + " 선택 후 판독 시작");
        }

        public static Nlv5201Command InitializeBank(int bank)
        {
            ValidateBank(bank);
            return Destructive("[BRBQ0Q" + bank, "Bank " + bank + " 설정 초기화");
        }

        public static Nlv5201Command SetRs232BaudRate(int baudRate)
        {
            string id;
            switch (baudRate)
            {
                case 300: id = "K1"; break;
                case 600: id = "K2"; break;
                case 1200: id = "K3"; break;
                case 2400: id = "K4"; break;
                case 4800: id = "K5"; break;
                case 9600: id = "K6"; break;
                case 19200: id = "K7"; break;
                case 38400: id = "K8"; break;
                case 57600: id = "K9"; break;
                case 115200: id = "SZ"; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baudRate), "NLV-5201 매뉴얼에 없는 RS-232C 속도입니다.");
            }
            return ConnectionChanging(id, "RS-232C baud rate " + baudRate);
        }

        private static void ValidateBank(int bank)
        {
            if (bank < 1 || bank > 7)
                throw new ArgumentOutOfRangeException(nameof(bank), "NLV-5201 Bank 번호는 1~7이어야 합니다.");
        }

        private static Nlv5201Command Runtime(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.Runtime);
        }

        private static Nlv5201Command Diagnostic(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.Diagnostic);
        }

        private static Nlv5201Command Active(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.ActiveSetting);
        }

        private static Nlv5201Command Persistent(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.PersistentWrite);
        }

        private static Nlv5201Command ConnectionChanging(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.ConnectionChanging);
        }

        private static Nlv5201Command Destructive(string id, string description)
        {
            return new Nlv5201Command(id, description, Nlv5201CommandRisk.Destructive);
        }
    }

    /// <summary>NLV-5201의 ESC + Command ID + CR 패킷을 생성합니다.</summary>
    public static class Nlv5201CommandCodec
    {
        public const byte Escape = 0x1B;
        public const byte CarriageReturn = 0x0D;

        public static byte[] Encode(Nlv5201Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            return Encode(new[] { command });
        }

        public static byte[] Encode(params Nlv5201Command[] commands)
        {
            if (commands == null || commands.Length == 0)
                throw new ArgumentException("전송할 NLV-5201 명령이 없습니다.", nameof(commands));
            if (commands.Any(command => command == null))
                throw new ArgumentException("NLV-5201 명령 목록에 null이 있습니다.", nameof(commands));
            if (commands.Length > 1 && commands.Any(command => command.Id.Length == 1))
                throw new InvalidOperationException("NLV-5201 1자리 명령은 다른 명령과 한 패킷으로 결합할 수 없습니다.");

            var packet = new List<byte> { Escape };
            foreach (Nlv5201Command command in commands)
            {
                ValidateCommandId(command.Id);
                packet.AddRange(Encoding.ASCII.GetBytes(command.Id));
            }
            packet.Add(CarriageReturn);
            return packet.ToArray();
        }

        public static void ValidateCommandId(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("NLV-5201 Command ID가 비어 있습니다.", nameof(commandId));

            foreach (char ch in commandId)
            {
                if (ch < 0x21 || ch > 0x7E)
                    throw new ArgumentException("NLV-5201 Command ID에는 ASCII 명령 문자만 사용할 수 있습니다.", nameof(commandId));
            }
        }

        public static string ToHex(byte[] packet)
        {
            if (packet == null || packet.Length == 0)
                return "";
            return string.Join(" ", packet.Select(value => value.ToString("X2")));
        }
    }

    /// <summary>NLV-5201 수신 데이터에서 프로토콜 프레임만 제거하고 본문은 보존합니다.</summary>
    public static class Nlv5201ResponseParser
    {
        public static string ParseBarcodePayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return "";

            int start = 0;
            int end = payload.Length;
            while (start < end && IsLeadingFrame(payload[start]))
                start++;
            while (end > start && IsTrailingFrame(payload[end - 1]))
                end--;
            if (start >= end)
                return "";
            if (payload[start] == 0x15)
                throw new InvalidOperationException("NLV-5201이 명령을 NAK로 거부했습니다.");

            return Encoding.GetEncoding(28591).GetString(payload, start, end - start);
        }

        private static bool IsLeadingFrame(byte value)
        {
            return value == 0x02 || value == 0x06 || value == (byte)'\r' || value == (byte)'\n';
        }

        private static bool IsTrailingFrame(byte value)
        {
            return value == 0x03 || value == (byte)'\r' || value == (byte)'\n';
        }
    }
}
