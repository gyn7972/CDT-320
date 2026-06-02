using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QMC.Common.Recipes;

namespace QMC.Vision.Optics.Sim
{
    /// <summary>
    /// Sim 조명 컨트롤러 — 하드웨어 없이 캐시만 갱신 (Stage 67).
    /// 모든 명령 0ms, IsConnected=true, 알람 없음. 실장비 결선 전 개발/테스트용.
    /// </summary>
    public class SimLightController : ILightController
    {
        private readonly int[] _power;      // 1-기반 → 인덱스 0 미사용
        private readonly int[] _strobeUs;
        private readonly int[] _lastOnPower; // On/Off 복원용
        private int _lastPage;               // 마지막 SwitchPage (합성 응답용)
        private readonly LightControllerMode _mode;
        private readonly Dictionary<int, int[]> _lastBatch = new Dictionary<int, int[]>();

        public bool   IsConnected  { get; private set; }
        public string PortName     => "Sim";
        public int    ChannelCount { get; }
        public LightControllerMode Mode => _mode;

        /// <summary>Stage 79 — 실제 송신(비-skip) 횟수 (테스트 검증용).</summary>
        public int BatchSendCount { get; private set; }

        /// <summary>채널별 마지막 명령 로그 (테스트 검증용).</summary>
        public event Action<string> Log;

        public SimLightController(int channels = 8) : this(channels, LightControllerMode.StrobeOnCommand) { }

        public SimLightController(int channels, LightControllerMode mode)
        {
            ChannelCount = channels <= 0 ? 8 : channels;
            _mode        = mode;
            _power       = new int[ChannelCount + 1];
            _strobeUs    = new int[ChannelCount + 1];
            _lastOnPower = new int[ChannelCount + 1];
            IsConnected  = true;
        }

        public Task<bool> ConnectAsync() { IsConnected = true; return Task.FromResult(true); }
        public Task       DisconnectAsync() { IsConnected = false; return Task.CompletedTask; }

        public Task<bool> SetPowerAsync(int channel, int power)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            _power[channel] = power;
            if (power > 0) _lastOnPower[channel] = power;
            Emit($"SetPower ch={channel} power={power}");
            return Task.FromResult(true);
        }

        public Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            _strobeUs[channel] = onTimeUs;
            Emit($"SetStrobeTime ch={channel} us={onTimeUs}");
            return Task.FromResult(true);
        }

        public Task<bool> SetOnOffAsync(int channel, bool on)
        {
            if (!IsValid(channel)) return Task.FromResult(false);
            int target = on ? (_lastOnPower[channel] > 0 ? _lastOnPower[channel] : 0) : 0;
            _power[channel] = target;
            Emit($"SetOnOff ch={channel} on={on} → power={target}");
            return Task.FromResult(true);
        }

        public Task<int>  GetPowerAsync(int channel)
            => Task.FromResult(IsValid(channel) ? _power[channel] : 0);

        public Task<bool> CheckPowerOnAsync(int channel)
            => Task.FromResult(IsConnected && IsValid(channel));

        public Task<bool> SwitchPageAsync(int page) { _lastPage = page; Emit($"SwitchPage page={page}"); return Task.FromResult(true); }

        /// <summary>Stage 79 — 일괄 적용. StrobeOnCommand 면 무조건 송신, 그 외는 동일값 skip. 송신 시 BatchSendCount++.</summary>
        public Task<bool> SetChannelBatchAsync(int page, int[] values)
        {
            if (values == null || values.Length != ChannelCount) return Task.FromResult(false);
            if (_mode != LightControllerMode.StrobeOnCommand
                && _lastBatch.TryGetValue(page, out var prev) && SeqEqual(prev, values))
            { Emit($"BatchSkip page={page}"); return Task.FromResult(true); }
            BatchSendCount++;
            _lastBatch[page] = (int[])values.Clone();
            for (int i = 0; i < values.Length; i++) { _power[i + 1] = values[i]; if (values[i] > 0) _lastOnPower[i + 1] = values[i]; }
            Emit($"BatchSend page={page} [{string.Join(",", values)}]");
            return Task.FromResult(true);
        }

        private static bool SeqEqual(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>Stage 75 — 합성 응답. 실장비 receive 대체 — 현재 페이지에서 점등(power&gt;0) 채널 수를 echo.</summary>
        public Task<string> ReceiveResponseAsync(int timeoutMs = 0)
        {
            int on = 0;
            for (int ch = 1; ch <= ChannelCount; ch++) if (_power[ch] > 0) on++;
            return Task.FromResult($"SIM:ACK p{_lastPage} on={on}");
        }

        private bool IsValid(int channel) => channel >= 1 && channel <= ChannelCount;
        private void Emit(string msg) { try { Log?.Invoke("[SimLight] " + msg); } catch { } }

        public void Dispose() { IsConnected = false; }
    }
}
