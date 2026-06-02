using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Recipes;

namespace QMC.Vision.Optics.LFine
{
    /// <summary>
    /// LFine 디지털 조명 컨트롤러 1대 (실장비, Stage 67).
    /// SerialPort 송신 + 명령 직렬화(lock) + Vision 로컬 알람.
    /// 무응답 프로토콜 — 송신만, 응답 파싱 없음 (Stage 66 #4 확정).
    /// </summary>
    public class LFineLightController : ILightController
    {
        private readonly LFineLightConfig _cfg;
        private readonly object _txLock = new object();
        private SerialPort _port;

        private readonly int[] _power;       // 1-기반 — 채널별 on-time 캐시 (밝기 = strobe on-time)
        private readonly int[] _lastOnPower; // On/Off 복원용 직전 on-time
        private int _currentPage;            // 현재 페이지 (SC/SP 명령마다 포함; 레퍼런스에 별도 전환 명령 없음)
        private readonly Dictionary<int, int[]> _lastPageTimes = new Dictionary<int, int[]>();  // batch 캐시(page→times)

        public bool   IsConnected { get; private set; }
        public string PortName    => _cfg.PortName;
        public int    ChannelCount => _cfg.ChannelCount;
        public LightControllerMode Mode => _cfg.Mode;

        public LFineLightController(LFineLightConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            int n = _cfg.ChannelCount <= 0 ? 8 : _cfg.ChannelCount;
            _power       = new int[n + 1];
            _lastOnPower = new int[n + 1];
        }

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

        public Task<bool> SetPowerAsync(int channel, int power)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            if (power < 0 || power > _cfg.MaxPower)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PWR-RANGE",
                    "Light/" + _cfg.PortName, $"Power 범위 초과 ch={channel} power={power} (max={_cfg.MaxPower})");
                return Task.FromResult(false);
            }
            // 밝기 = strobe on-time. 단일 채널 명령(SC)으로 현재 페이지에 송신.
            bool ok = SendFrame(LFineProtocol.ChannelOnTimeFrame(_currentPage, channel, power));
            if (ok) { _power[channel] = power; if (power > 0) _lastOnPower[channel] = power; }
            return Task.FromResult(ok);
        }

        public Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            if (onTimeUs < 0 || onTimeUs > _cfg.MaxOnTimeUs)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-PWR-RANGE",
                    "Light/" + _cfg.PortName, $"StrobeTime 범위 초과 ch={channel} us={onTimeUs} (max={_cfg.MaxOnTimeUs})");
                return Task.FromResult(false);
            }
            // LFine PS 디지털: strobe on-time 이 곧 밝기 — 단일 채널 명령(SC) 으로 현재 페이지에 송신.
            bool ok = SendFrame(LFineProtocol.ChannelOnTimeFrame(_currentPage, channel, onTimeUs));
            if (ok) { _power[channel] = onTimeUs; if (onTimeUs > 0) _lastOnPower[channel] = onTimeUs; }
            return Task.FromResult(ok);
        }

        public Task<bool> SetOnOffAsync(int channel, bool on)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            int target = on ? (_lastOnPower[channel] > 0 ? _lastOnPower[channel] : 0) : 0;
            return SetPowerAsync(channel, target);
        }

        public Task<int> GetPowerAsync(int channel)
            => Task.FromResult(IsValid(channel) ? _power[channel] : 0);

        public Task<bool> CheckPowerOnAsync(int channel)
            => Task.FromResult(IsConnected && (_port != null && _port.IsOpen) && IsValid(channel));

        /// <summary>페이지 전환 — 레퍼런스 프로토콜엔 별도 전환 명령이 없고 page 는 SC/SP 명령마다 포함된다.
        /// 따라서 현재 페이지 값만 보관하고, 이후 SetPower/SetStrobeTime 이 이 페이지로 송신한다.</summary>
        public Task<bool> SwitchPageAsync(int page)
        {
            _currentPage = page < 0 ? 0 : page;
            return Task.FromResult(true);
        }

        /// <summary>Stage 79 — 페이지 채널 일괄 적용 = SP 1프레임. StrobeOnCommand 면 무조건 송신,
        /// 그 외 모드는 직전 times 와 동일하면 skip(트리거 아님이라 안전).</summary>
        public Task<bool> SetChannelBatchAsync(int page, int[] times)
        {
            if (times == null || times.Length != ChannelCount) return Task.FromResult(false);
            if (Mode != LightControllerMode.StrobeOnCommand
                && _lastPageTimes.TryGetValue(page, out var prev) && SeqEqual(prev, times))
                return Task.FromResult(true);   // skip — 값 동일 + 발사 트리거 아님

            bool ok = SendFrame(LFineProtocol.PageOnTimeFrame(page, times));   // 페이지 전체 1프레임
            if (ok)
            {
                _currentPage = page;
                _lastPageTimes[page] = (int[])times.Clone();
                for (int i = 0; i < times.Length; i++)
                {
                    _power[i + 1] = times[i];
                    if (times[i] > 0) _lastOnPower[i + 1] = times[i];
                }
            }
            return Task.FromResult(ok);
        }

        private static bool SeqEqual(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>Stage 75 — 시리얼에서 응답 1프레임 수신 (적용 후 검증용). 무응답/타임아웃이면 null.
        /// 백그라운드 스레드에서 동기 읽기 수행 (UI 비차단).</summary>
        public Task<string> ReceiveResponseAsync(int timeoutMs = 0)
        {
            int eff = timeoutMs > 0 ? timeoutMs : (_cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000);
            return Task.Run(() => ReadResponseFrame(eff));
        }

        /// <summary>Etx2(0x0A) 또는 per-byte 타임아웃까지 읽어 LFineProtocol.UnwrapFrame 으로 디코딩.</summary>
        private string ReadResponseFrame(int timeoutMs)
        {
            if (_port == null || !_port.IsOpen) return null;
            int saved = _port.ReadTimeout;
            var buf = new System.Collections.Generic.List<byte>();
            try
            {
                _port.ReadTimeout = timeoutMs;
                while (true)
                {
                    int b;
                    try { b = _port.ReadByte(); }
                    catch (TimeoutException) { break; }   // 더 이상 데이터 없음 → 종료
                    if (b < 0) break;
                    buf.Add((byte)b);
                    if (b == LFineProtocol.Etx2) break;   // 프레임 끝(0x0A)
                }
            }
            catch { /* 수신 오류는 검증용이므로 조용히 무시 (null 반환) */ }
            finally { try { _port.ReadTimeout = saved; } catch { } }
            return buf.Count == 0 ? null : LFineProtocol.UnwrapFrame(buf.ToArray());
        }

        /// <summary>프레임 송신 — lock 으로 직렬화. 송신 실패 시 LIGHT-TX-FAIL.</summary>
        private bool SendFrame(byte[] frame)
        {
            if (_port == null || !_port.IsOpen)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL",
                    "Light/" + _cfg.PortName, "포트 미개방 상태에서 송신 시도");
                return false;
            }
            try
            {
                lock (_txLock) { _port.Write(frame, 0, frame.Length); }
                return true;
            }
            catch (TimeoutException)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TIMEOUT",
                    "Light/" + _cfg.PortName, "송신 타임아웃");
                return false;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LIGHT-TX-FAIL",
                    "Light/" + _cfg.PortName, "시리얼 쓰기 예외: " + ex.Message);
                return false;
            }
        }

        private bool IsValid(int channel) => channel >= 1 && channel <= ChannelCount;

        // ── enum 파싱 (Config 문자열 → System.IO.Ports enum) ──
        private static Parity   ParseParity(string s)    => Enum.TryParse(s, true, out Parity p)    ? p : Parity.None;
        private static StopBits ParseStopBits(string s)  => Enum.TryParse(s, true, out StopBits b)  ? b : StopBits.One;
        private static Handshake ParseHandshake(string s)=> Enum.TryParse(s, true, out Handshake h) ? h : Handshake.None;

        public void Dispose()
        {
            try { DisconnectAsync().Wait(200); } catch { }
            try { _port?.Dispose(); } catch { }
            _port = null;
        }
    }
}
