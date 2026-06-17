using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Recipes;

namespace QMC.Vision.Optics.Leesos
{
    /// <summary>
    /// LeesOS LPD-6524 (12BIT) 디지털 조명 컨트롤러 1대 (실장비, Stage 79 — 매뉴얼 §5.3 기준).
    /// <para>응답형 — 명령마다 (송신→수신→echo 검증) 1쌍, <see cref="_ioGate"/> 직렬화.
    /// 밝기 = Volume(LC, 0~4095). On/Off = LH(R{n1}OK/ER). 상태 = LS. Strobe/Page 미지원(no-op).
    /// PWM 연속 모드라 <see cref="Mode"/> 강제 Continuous → 동일값 캐시 skip 안전.</para>
    /// 알람은 LFine 과 공통 LIGHT-* 재사용.
    /// </summary>
    public class LeesosLightController : ILightController
    {
        private readonly LeesosLightConfig _cfg;
        private readonly SemaphoreSlim _ioGate = new SemaphoreSlim(1, 1);
        private SerialPort _port;

        private readonly int[]  _power;     // 1-기반 — 채널별 마지막 Volume
        private readonly bool[] _onState;   // 1-기반 — 채널별 On/Off

        public bool   IsConnected { get; private set; }
        public string PortName    => _cfg.PortName;
        public int    ChannelCount => _cfg.ChannelCount;

        public LeesosLightController(LeesosLightConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            int n = _cfg.ChannelCount <= 0 ? 4 : _cfg.ChannelCount;
            _power   = new int[n + 1];
            _onState = new bool[n + 1];
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

        // ── 명령 ──
        public async Task<bool> SetPowerAsync(int channel, int power)
        {
            if (!IsValid(channel)) return false;
            if (power < 0 || power > _cfg.MaxPower)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PWR-RANGE",
                    "Light/" + _cfg.PortName, $"Volume 범위 초과 ch={channel} v={power} (max={_cfg.MaxPower})");
                return false;
            }
            string resp = await SendReceiveAsync(LeesosProtocol.BuildVolumeCommand(channel, power)).ConfigureAwait(false);
            if (resp == null)                       { RaiseTimeout("LC"); return false; }
            if (LeesosProtocol.IsErrorResponse(resp)) { RaiseNak(resp);   return false; }
            if (!LeesosProtocol.ValidateEcho(resp, channel, power)) { RaiseInvalid(resp, "LC"); return false; }
            _power[channel] = power;
            return true;
        }

        /// <summary>LeesOS 는 Strobe 미지원 — no-op + true.</summary>
        public Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs) => Task.FromResult(IsValid(channel));

        public async Task<bool> SetOnOffAsync(int channel, bool on)
        {
            if (!IsValid(channel)) return false;
            string resp = await SendReceiveAsync(LeesosProtocol.BuildOnOffCommand(channel, on)).ConfigureAwait(false);
            if (resp == null)                       { RaiseTimeout("LH"); return false; }
            if (LeesosProtocol.IsErrorResponse(resp)) { RaiseNak(resp);   return false; }   // R{n1}ER
            _onState[channel] = on;
            return true;
        }

        public Task<int> GetPowerAsync(int channel)
            => Task.FromResult(IsValid(channel) ? _power[channel] : 0);

        public async Task<bool> CheckPowerOnAsync(int channel)
        {
            if (!IsValid(channel)) return false;
            string resp = await SendReceiveAsync(LeesosProtocol.BuildStatusCommand(channel, LeesosStatusType.OnOff)).ConfigureAwait(false);
            if (resp == null) { RaiseTimeout("LS"); return false; }
            if (LeesosProtocol.IsErrorResponse(resp)) { RaiseNak(resp); return false; }
            return LeesosProtocol.ParseStatusOn(resp);   // R{n1}ON
        }

        /// <summary>LeesOS 는 Page 미지원 — no-op + true.</summary>
        public Task<bool> SwitchPageAsync(int page) => Task.FromResult(true);

        /// <summary>Stage 79 — 일괄 적용. 전체 동일값이면 LCT 1프레임, 그 외 전 채널 LC loop(항상 송신 — 캐시 skip 잔재 제거).</summary>
        public async Task<bool> SetChannelBatchAsync(int page, int[] values)
        {
            if (values == null || values.Length != ChannelCount) return false;

            if (AllSame(values))
            {
                int v = Clamp(values[0]);
                string resp = await SendReceiveAsync(LeesosProtocol.BuildVolumeAllCommand(v)).ConfigureAwait(false);
                if (resp == null)                       { RaiseTimeout("LCT"); return false; }
                if (LeesosProtocol.IsErrorResponse(resp)) { RaiseNak(resp);     return false; }
                if (!LeesosProtocol.ValidateAllEcho(resp, v)) { RaiseInvalid(resp, "LCT"); return false; }
                for (int i = 1; i <= ChannelCount; i++) _power[i] = v;
                return true;
            }

            // 그 외: 전 채널 LC (SetPowerAsync 가 송신 + 에코 검증)
            for (int i = 0; i < values.Length; i++)
            {
                if (!await SetPowerAsync(i + 1, Clamp(values[i])).ConfigureAwait(false)) return false;
            }
            return true;
        }

        /// <summary>Leesos 는 Continuous PWM — 하드웨어 모드 개념 없음. no-op + true.</summary>
        public Task<bool> SetHardwareModeAsync(LFine.LFineHardwareMode mode) => Task.FromResult(true);

        /// <summary>응답 1프레임 수신 (적용 후 검증용). NAK 이면 LIGHT-NAK + null.</summary>
        public Task<string> ReceiveResponseAsync(int timeoutMs = 0)
        {
            int eff = timeoutMs > 0 ? timeoutMs : (_cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000);
            return Task.Run(() =>
            {
                _ioGate.Wait();
                try
                {
                    string resp = ReadFrame(eff);
                    if (LeesosProtocol.IsErrorResponse(resp)) { RaiseNak(resp); return (string)null; }
                    return resp;
                }
                finally { _ioGate.Release(); }
            });
        }

        // ── 송수신 1쌍 (백그라운드 + gate) ──
        private Task<string> SendReceiveAsync(string cmd)
            => Task.Run(() =>
            {
                _ioGate.Wait();
                try
                {
                    if (!WriteFrame(LeesosProtocol.WrapFrame(cmd))) return (string)null;
                    return ReadFrame(_cfg.TimeoutMs);
                }
                finally { _ioGate.Release(); }
            });

        private bool WriteFrame(byte[] frame)
        {
            if (_port == null || !_port.IsOpen)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL",
                    "Light/" + _cfg.PortName, "포트 미개방 상태에서 송신 시도");
                return false;
            }
            try { _port.Write(frame, 0, frame.Length); return true; }
            catch (TimeoutException) { AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TIMEOUT", "Light/" + _cfg.PortName, "송신 타임아웃"); return false; }
            catch (Exception ex)     { AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL", "Light/" + _cfg.PortName, "시리얼 쓰기 예외: " + ex.Message); return false; }
        }

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
                    if (b == LeesosProtocol.Lf) break;   // 프레임 끝(0x0A)
                }
            }
            catch { }
            finally { try { _port.ReadTimeout = saved; } catch { } }
            return buf.Count == 0 ? null : LeesosProtocol.UnwrapFrame(buf.ToArray());
        }

        private int Clamp(int v) => v < 0 ? 0 : (v > _cfg.MaxPower ? _cfg.MaxPower : v);
        private static bool AllSame(int[] v)
        {
            for (int i = 1; i < v.Length; i++) if (v[i] != v[0]) return false;
            return v.Length > 0;
        }
        private bool IsValid(int channel) => channel >= 1 && channel <= ChannelCount;

        private void RaiseNak(string resp)     => AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-NAK", "Light/" + _cfg.PortName, $"NAK 응답 [{resp}]");
        private void RaiseTimeout(string cmd)  => AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TIMEOUT", "Light/" + _cfg.PortName, $"응답 타임아웃 (cmd={cmd})");
        private void RaiseInvalid(string r, string cmd) => AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-INVALID-RESP", "Light/" + _cfg.PortName, $"형식 불일치 응답 [{r}] (cmd={cmd})");

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
