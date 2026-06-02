using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;

namespace QMC.Vision.Optics.Leesos
{
    /// <summary>
    /// LeesOS 디지털 조명 컨트롤러 1대 (실장비, Stage 77).
    /// <para>응답형 프로토콜 — 명령마다 (송신→수신→echo 검증) 1쌍. <see cref="_ioGate"/> 로 직렬화.
    /// 밝기 = Volume(LC, 0~255). On/Off = LH. 점등확인 = LS. Strobe/Page 미지원(no-op).</para>
    /// 알람은 LFine 과 공통 LIGHT-* 재사용 (신규 0).
    /// </summary>
    public class LeesosLightController : ILightController
    {
        private readonly LeesosLightConfig _cfg;
        private readonly SemaphoreSlim _ioGate = new SemaphoreSlim(1, 1);  // (송신→수신) 쌍 직렬화
        private SerialPort _port;

        private readonly int[] _power;        // 1-기반 — 채널별 마지막 Volume
        private readonly int[] _lastOnPower;  // On 복원용

        public bool   IsConnected { get; private set; }
        public string PortName    => _cfg.PortName;
        public int    ChannelCount => _cfg.ChannelCount;

        public LeesosLightController(LeesosLightConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            int n = _cfg.ChannelCount <= 0 ? 8 : _cfg.ChannelCount;
            _power       = new int[n + 1];
            _lastOnPower = new int[n + 1];
        }

        // ── Connect / Disconnect ──
        public Task<bool> ConnectAsync()
        {
            try
            {
                _port = new SerialPort(_cfg.PortName, _cfg.BaudRate, ParseParity(_cfg.Parity),
                                       _cfg.DataBits, ParseStopBits(_cfg.StopBits))
                {
                    Handshake    = ParseHandshake(_cfg.Handshake),
                    ReadTimeout  = _cfg.TimeoutMs,
                    WriteTimeout = _cfg.TimeoutMs
                };
                _port.Open();
                IsConnected = true;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                AlarmManager.Raise(AlarmSeverity.Error, "LIGHT-OPEN-FAIL",
                    "Light/" + _cfg.PortName, $"시리얼 Open 실패 [{_cfg.PortName}]: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public Task DisconnectAsync()
        {
            try { if (_port != null && _port.IsOpen) _port.Close(); } catch { }
            IsConnected = false;
            return Task.CompletedTask;
        }

        // ── 명령 (응답형) ──
        public Task<bool> SetPowerAsync(int channel, int power)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            if (power < 0 || power > _cfg.MaxPower)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PWR-RANGE",
                    "Light/" + _cfg.PortName, $"Volume 범위 초과 ch={channel} power={power} (max={_cfg.MaxPower})");
                return Task.FromResult(false);
            }
            return Task.Run(() =>
            {
                bool ok = SendVerify(LeesosProtocol.BuildVolumeCommand(channel, power));
                if (ok) { _power[channel] = power; if (power > 0) _lastOnPower[channel] = power; }
                return ok;
            });
        }

        /// <summary>LeesOS 는 Strobe 미지원 — no-op + true.</summary>
        public Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs) => Task.FromResult(IsValid(channel));

        public Task<bool> SetOnOffAsync(int channel, bool on)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            return Task.Run(() =>
            {
                bool ok = SendVerify(LeesosProtocol.BuildOnOffCommand(channel, on));
                // LH 는 출력 게이트만 — Volume 캐시는 유지. (off 라고 Volume 을 0 으로 바꾸지 않음)
                if (ok && on && _lastOnPower[channel] > 0) _power[channel] = _lastOnPower[channel];
                return ok;
            });
        }

        public Task<int> GetPowerAsync(int channel)
            => Task.FromResult(IsValid(channel) ? _power[channel] : 0);

        public Task<bool> CheckPowerOnAsync(int channel)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            return Task.Run(() =>
            {
                _ioGate.Wait();
                try
                {
                    if (!WriteFrame(LeesosProtocol.WrapFrame(LeesosProtocol.BuildStatusCommand(channel)))) return false;
                    string resp = ReadFrame(_cfg.TimeoutMs);
                    if (LeesosProtocol.Classify(resp) == LeesosProtocol.RespKind.Nak)
                    { RaiseNak(resp); return false; }
                    return LeesosProtocol.ParseStatusOn(resp);
                }
                finally { _ioGate.Release(); }
            });
        }

        /// <summary>LeesOS 는 Page 미지원 — no-op + true.</summary>
        public Task<bool> SwitchPageAsync(int page) => Task.FromResult(true);

        /// <summary>응답 1프레임 수신 (적용 후 검증용). NAK 이면 LIGHT-NAK + null. 무응답/타임아웃 null.
        /// ※ LeesOS 는 각 Set 명령이 이미 응답을 소비하므로, 적용 직후 호출 시 보통 잔여 응답이 없다.</summary>
        public Task<string> ReceiveResponseAsync(int timeoutMs = 0)
        {
            int eff = timeoutMs > 0 ? timeoutMs : (_cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000);
            return Task.Run(() =>
            {
                _ioGate.Wait();
                try
                {
                    string resp = ReadFrame(eff);
                    if (LeesosProtocol.Classify(resp) == LeesosProtocol.RespKind.Nak) { RaiseNak(resp); return (string)null; }
                    return resp;
                }
                finally { _ioGate.Release(); }
            });
        }

        // ── 송수신 1쌍 (gate 보유 상태에서 호출) ──
        private bool SendVerify(string cmd)
        {
            _ioGate.Wait();
            try
            {
                if (!WriteFrame(LeesosProtocol.WrapFrame(cmd))) return false;
                string resp = ReadFrame(_cfg.TimeoutMs);
                switch (LeesosProtocol.Classify(resp))
                {
                    case LeesosProtocol.RespKind.Ok:  return true;
                    case LeesosProtocol.RespKind.Nak: RaiseNak(resp); return false;
                    default:
                        if (resp == null)
                            AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TIMEOUT",
                                "Light/" + _cfg.PortName, $"응답 타임아웃 (cmd={cmd})");
                        else
                            AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-INVALID-RESP",
                                "Light/" + _cfg.PortName, $"형식 불일치 응답 [{resp}] (cmd={cmd})");
                        return false;
                }
            }
            finally { _ioGate.Release(); }
        }

        private bool WriteFrame(byte[] frame)
        {
            if (_port == null || !_port.IsOpen)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL",
                    "Light/" + _cfg.PortName, "포트 미개방 상태에서 송신 시도");
                return false;
            }
            try { _port.Write(frame, 0, frame.Length); return true; }
            catch (TimeoutException)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TIMEOUT", "Light/" + _cfg.PortName, "송신 타임아웃");
                return false;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL", "Light/" + _cfg.PortName, "시리얼 쓰기 예외: " + ex.Message);
                return false;
            }
        }

        /// <summary>Etx2(0x0A) 또는 per-byte 타임아웃까지 읽어 payload 로 디코딩. (gate 보유 상태 호출)</summary>
        private string ReadFrame(int timeoutMs)
        {
            if (_port == null || !_port.IsOpen) return null;
            int saved = _port.ReadTimeout;
            var buf = new List<byte>();
            try
            {
                _port.ReadTimeout = timeoutMs > 0 ? timeoutMs : _cfg.TimeoutMs;
                while (true)
                {
                    int b;
                    try { b = _port.ReadByte(); }
                    catch (TimeoutException) { break; }
                    if (b < 0) break;
                    buf.Add((byte)b);
                    if (b == LeesosProtocol.Etx2) break;
                }
            }
            catch { }
            finally { try { _port.ReadTimeout = saved; } catch { } }
            return buf.Count == 0 ? null : LeesosProtocol.UnwrapFrame(buf.ToArray());
        }

        private void RaiseNak(string resp)
            => AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-NAK",
                   "Light/" + _cfg.PortName, $"NAK 응답 [{resp}]");

        private bool IsValid(int channel) => channel >= 1 && channel <= ChannelCount;

        private static Parity    ParseParity(string s)    => Enum.TryParse(s, true, out Parity p)    ? p : Parity.None;
        private static StopBits  ParseStopBits(string s)  => Enum.TryParse(s, true, out StopBits b)  ? b : StopBits.One;
        private static Handshake ParseHandshake(string s) => Enum.TryParse(s, true, out Handshake h) ? h : Handshake.None;

        public void Dispose()
        {
            try { DisconnectAsync().Wait(200); } catch { }
            try { _port?.Dispose(); } catch { }
            try { _ioGate?.Dispose(); } catch { }
            _port = null;
        }
    }
}
