using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.IO
{
    public sealed class AjinIoSnapshot
    {
        public string Name { get; set; }
        public int Module { get; set; }
        public int Bit { get; set; }
        public bool IsOutput { get; set; }
        public bool IsOn { get; set; }
        public bool Nc { get; set; }
        public int ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 이 값이 시뮬레이션 주입에서 온 것인지 여부.
        /// [실장비 오염 차단 2026-07-28] 캐시 키가 모듈/비트라 시뮬 포인트와 실보드 포인트가 같은 칸을 쓴다.
        /// 실보드 입력/출력은 이 플래그가 true 인 스냅샷을 절대 적용하지 않는다.
        /// </summary>
        public bool IsSimulated { get; set; }
    }

    public sealed class AjinIoScanService : IDisposable
    {
        private static readonly object SimulationGate = new object();
        private static readonly Dictionary<string, bool> SimulationStates =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private readonly object _gate = new object();
        private readonly Dictionary<string, AjinIoSnapshot> _latest =
            new Dictionary<string, AjinIoSnapshot>(StringComparer.OrdinalIgnoreCase);

        private List<BaseDigitalInput> _inputs = new List<BaseDigitalInput>();
        private List<BaseDigitalOutput> _outputs = new List<BaseDigitalOutput>();
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private Func<bool> _isOpen;
        private int _intervalMs = 10;

        public static readonly object AxdSyncRoot = new object();
        public static AjinIoScanService Current { get; private set; }

        public event Action<AjinIoSnapshot> IoStatusUpdated;
        public event Action<IReadOnlyList<AjinIoSnapshot>> CycleCompleted;

        public bool IsRunning
        {
            get
            {
                lock (_gate)
                    return _cts != null && !_cts.IsCancellationRequested;
            }
        }

        public int IntervalMs
        {
            get { lock (_gate) return _intervalMs; }
        }

        public void Start(
            IEnumerable<BaseDigitalInput> inputs,
            IEnumerable<BaseDigitalOutput> outputs,
            int intervalMs = 10,
            Func<bool> isOpen = null)
        {
            if (intervalMs < 5) intervalMs = 5;
            Stop();

            lock (_gate)
            {
                _inputs = inputs != null ? new List<BaseDigitalInput>(inputs) : new List<BaseDigitalInput>();
                _outputs = outputs != null ? new List<BaseDigitalOutput>(outputs) : new List<BaseDigitalOutput>();
                _intervalMs = intervalMs;
                _isOpen = isOpen ?? (() => true);
                _cts = new CancellationTokenSource();
                Current = this;
                _loopTask = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
            }
        }

        public void Stop()
        {
            CancellationTokenSource cts = null;
            Task loopTask = null;

            lock (_gate)
            {
                cts = _cts;
                loopTask = _loopTask;
                _cts = null;
                _loopTask = null;
                if (ReferenceEquals(Current, this))
                    Current = null;
            }

            if (cts == null) return;

            try { cts.Cancel(); } catch { }
            try { if (loopTask != null) loopTask.Wait(300); } catch { }
            cts.Dispose();
        }

        public AjinIoSnapshot GetLatest(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            lock (_gate)
            {
                AjinIoSnapshot snapshot;
                return _latest.TryGetValue(name, out snapshot) ? snapshot : null;
            }
        }

        public AjinIoSnapshot GetLatest(int module, int bit, bool isOutput)
        {
            lock (_gate)
            {
                AjinIoSnapshot snapshot;
                return _latest.TryGetValue(MakeKey(module, bit, isOutput), out snapshot) ? snapshot : null;
            }
        }

        public AjinIoSnapshot[] GetAllLatest()
        {
            lock (_gate)
            {
                var values = new AjinIoSnapshot[_latest.Count];
                _latest.Values.CopyTo(values, 0);
                return values;
            }
        }

        /// <summary>
        /// 스캔 대상 입력 인스턴스를 동적으로 등록한다.<br/>
        /// 동일 인스턴스가 이미 등록되어 있으면 다시 추가하지 않는다.<br/>
        /// 서로 다른 실린더가 동일 (Module,Bit)을 공유하는 경우(예: 클램프 + 클램프 리프트가 같은 업 센서 공유)도 허용된다 — 각각 독립 인스턴스로 갱신됨.
        /// </summary>
        public void RegisterInput(BaseDigitalInput input)
        {
            if (input == null) return;
            lock (_gate)
            {
                for (int i = _inputs.Count - 1; i >= 0; i--)
                {
                    BaseDigitalInput existing = _inputs[i];
                    if (existing == null) { _inputs.RemoveAt(i); continue; }
                    if (ReferenceEquals(existing, input)) { _inputs.RemoveAt(i); continue; }
                }
                _inputs.Add(input);
            }
        }

        /// <summary>지정한 입력 인스턴스를 스캔 대상에서 제거한다.</summary>
        public void UnregisterInput(BaseDigitalInput input)
        {
            if (input == null) return;
            lock (_gate)
            {
                for (int i = _inputs.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_inputs[i], input))
                        _inputs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 스캔 대상 출력 인스턴스를 동적으로 등록한다.<br/>
        /// 동일 인스턴스가 이미 등록되어 있으면 다시 추가하지 않는다.<br/>
        /// 서로 다른 실린더가 동일 (Module,Bit)을 공유하는 경우도 허용된다.
        /// </summary>
        public void RegisterOutput(BaseDigitalOutput output)
        {
            if (output == null) return;
            lock (_gate)
            {
                for (int i = _outputs.Count - 1; i >= 0; i--)
                {
                    BaseDigitalOutput existing = _outputs[i];
                    if (existing == null) { _outputs.RemoveAt(i); continue; }
                    if (ReferenceEquals(existing, output)) { _outputs.RemoveAt(i); continue; }
                }
                _outputs.Add(output);
            }
        }

        /// <summary>지정한 출력 인스턴스를 스캔 대상에서 제거한다.</summary>
        public void UnregisterOutput(BaseDigitalOutput output)
        {
            if (output == null) return;
            lock (_gate)
            {
                for (int i = _outputs.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_outputs[i], output))
                        _outputs.RemoveAt(i);
                }
            }
        }

        public bool TryApplyLatest(BaseDigitalInput input)
        {
            if (input == null) return false;
            AjinIoSnapshot snapshot = GetLatest(input.Setup.ModuleNo, input.Setup.BitNo, false);
            if (snapshot == null || snapshot.ErrorCode != 0) return false;

            // [실장비 오염 차단 2026-07-28] 캐시 키가 모듈/비트라서 시뮬 포인트와 실보드 포인트가 같은 칸을 공유한다.
            // 예전에는 여기서 출처를 따지지 않아, 시뮬 주입값(SimulateInput -> SetSimulatedState -> UpdateCached)이
            // 같은 주소의 실보드 입력에 그대로 덮어씌워졌다. IsOn 게터가 매번 이 경로를 타므로
            // 실제 센서값이 조용히 뒤집혔다(2026-07-28 현장: OutputStage GoodBin 링 신호).
            // 실보드 포인트는 하드웨어에서 읽은 값만 쓴다. 시뮬 스냅샷은 적용하지 않는다.
            // Config 를 못 읽으면 모드를 확정할 수 없으므로 실보드로 간주해 차단한다(fail-closed).
            bool isSimulationPoint = input.Config != null && input.Config.IsSimulationMode;
            if (!isSimulationPoint && snapshot.IsSimulated)
                return false;

            input.ApplyScannedState(snapshot.IsOn);
            return true;
        }

        public static bool TryReadHardwareInput(BaseDigitalInput input, out int errorCode)
        {
            errorCode = -1;
            if (input == null) return false;

            bool raw = false;
            lock (AxdSyncRoot)
                errorCode = AXD.Read(input.Setup.ModuleNo, input.Setup.BitNo, ref raw);

            if (errorCode != 0)
                return false;

            bool logical = input.Setup.IsNormallyClosed ? !raw : raw;
            input.ApplyScannedState(logical);

            AjinIoScanService current = Current;
            if (current != null)
                current.UpdateCached(
                    input.Name,
                    input.Setup.ModuleNo,
                    input.Setup.BitNo,
                    false,
                    logical,
                    input.Setup.IsNormallyClosed,
                    0);

            return true;
        }

        public bool TryApplyLatest(BaseDigitalOutput output)
        {
            if (output == null) return false;
            AjinIoSnapshot snapshot = GetLatest(output.Setup.ModuleNo, output.Setup.BitNo, true);
            if (snapshot == null || snapshot.ErrorCode != 0) return false;

            // [실장비 오염 차단 2026-07-28] 입력과 같은 이유로 출력도 출처를 따진다. 모르면 실보드로 간주(fail-closed).
            bool isSimulationPoint = output.Config != null && output.Config.IsSimulationMode;
            if (!isSimulationPoint && snapshot.IsSimulated)
                return false;

            output.ApplyScannedState(snapshot.IsOn);
            return true;
        }

        public static void SetSimulatedState(BaseDigitalInput input, bool logicalState)
        {
            if (input == null) return;

            SetSimulatedState(
                input.Name,
                input.Setup.ModuleNo,
                input.Setup.BitNo,
                false,
                logicalState,
                input.Setup.IsNormallyClosed);
        }

        public static void SetSimulatedInput(
            string name,
            int module,
            int bit,
            bool logicalState,
            bool nc)
        {
            SetSimulatedState(name, module, bit, false, logicalState, nc);
        }

        public static void SetSimulatedState(BaseDigitalOutput output, bool logicalState)
        {
            if (output == null) return;

            SetSimulatedState(
                output.Name,
                output.Setup.ModuleNo,
                output.Setup.BitNo,
                true,
                logicalState,
                output.Setup.IsNormallyClosed);
        }

        public static void SetSimulatedOutput(
            string name,
            int module,
            int bit,
            bool logicalState,
            bool nc)
        {
            SetSimulatedState(name, module, bit, true, logicalState, nc);
        }

        public static bool TryGetSimulatedState(BaseDigitalInput input, out bool logicalState)
        {
            logicalState = false;
            if (input == null) return false;

            return TryGetSimulatedState(
                input.Setup.ModuleNo,
                input.Setup.BitNo,
                false,
                out logicalState);
        }

        public static bool TryGetSimulatedState(BaseDigitalOutput output, out bool logicalState)
        {
            logicalState = false;
            if (output == null) return false;

            return TryGetSimulatedState(
                output.Setup.ModuleNo,
                output.Setup.BitNo,
                true,
                out logicalState);
        }

        private static void SetSimulatedState(
            string name,
            int module,
            int bit,
            bool isOutput,
            bool logicalState,
            bool nc)
        {
            if (IsValidPoint(module, bit))
            {
                string key = MakeKey(module, bit, isOutput);
                lock (SimulationGate)
                {
                    SimulationStates[key] = logicalState;
                }
            }

            AjinIoScanService current = Current;
            if (current != null)
                current.UpdateCached(name, module, bit, isOutput, logicalState, nc, 0, true);
        }

        private static bool TryGetSimulatedState(
            int module,
            int bit,
            bool isOutput,
            out bool logicalState)
        {
            logicalState = false;
            if (!IsValidPoint(module, bit))
                return false;

            string key = MakeKey(module, bit, isOutput);
            lock (SimulationGate)
            {
                return SimulationStates.TryGetValue(key, out logicalState);
            }
        }

        public static int WriteOutput(BaseDigitalOutput output, bool logicalState)
        {
            if (output == null) return -1;

            bool physical = output.Setup.IsNormallyClosed ? !logicalState : logicalState;
            int ret;
            lock (AxdSyncRoot)
                ret = AXD.Write(output.Setup.ModuleNo, output.Setup.BitNo, physical);

            if (ret == 0)
            {
                output.ApplyScannedState(logicalState);
                AjinIoScanService current = Current;
                if (current != null)
                    current.UpdateCached(output.Name, output.Setup.ModuleNo, output.Setup.BitNo, true, logicalState, output.Setup.IsNormallyClosed, 0);
            }

            return ret;
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<BaseDigitalInput> inputs;
                List<BaseDigitalOutput> outputs;
                int delay;
                Func<bool> isOpen;
                lock (_gate)
                {
                    inputs = new List<BaseDigitalInput>(_inputs);
                    outputs = new List<BaseDigitalOutput>(_outputs);
                    delay = _intervalMs;
                    isOpen = _isOpen;
                }

                var cycle = new List<AjinIoSnapshot>(inputs.Count + outputs.Count);
                bool hardwareOpen = isOpen == null || isOpen();
                if (hardwareOpen || HasSimulationPorts(inputs, outputs))
                {
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        AjinIoSnapshot snapshot = ReadInput(inputs[i], hardwareOpen);
                        cycle.Add(snapshot);
                        RaiseIoStatusUpdated(snapshot);
                    }

                    for (int i = 0; i < outputs.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        AjinIoSnapshot snapshot = ReadOutput(outputs[i], hardwareOpen);
                        cycle.Add(snapshot);
                        RaiseIoStatusUpdated(snapshot);
                    }
                }

                RaiseCycleCompleted(cycle);

                try { await Task.Delay(delay, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        private AjinIoSnapshot ReadInput(BaseDigitalInput input, bool hardwareOpen)
        {
            if (input == null) return BuildSnapshot(string.Empty, 0, 0, false, false, false, -1);

            if (input.Config.IsSimulationMode || !hardwareOpen)
            {
                // [실장비 오염 차단 2026-07-28] 실보드로 설정된 포인트는 시뮬 저장값을 절대 쓰지 않는다.
                // 보드 미오픈(!hardwareOpen)일 때 실보드 포인트까지 이 분기로 들어와,
                // 같은 모듈/비트에 남아 있던 시뮬 주입값을 그대로 적용해 실제 센서값을 뒤집었다.
                // 이 경우 유효한 읽기가 없는 것이므로 값을 건드리지 않고 시뮬 스냅샷으로만 표시해
                // TryApplyLatest 에서 걸러지게 한다.
                if (!input.Config.IsSimulationMode)
                {
                    return UpdateCached(input.Name, input.Setup.ModuleNo, input.Setup.BitNo, false,
                        input.IsOn, input.Setup.IsNormallyClosed, 0, true);
                }

                bool simulatedLogical = input.IsOn;
                bool hasSimulatedState = TryGetSimulatedState(input, out simulatedLogical);
                if (hasSimulatedState)
                    input.ApplyScannedState(simulatedLogical);
                else
                    SetSimulatedState(input, simulatedLogical);

                return UpdateCached(input.Name, input.Setup.ModuleNo, input.Setup.BitNo, false, simulatedLogical, input.Setup.IsNormallyClosed, 0, true);
            }

            bool raw = false;
            int ret;
            lock (AxdSyncRoot)
                ret = AXD.Read(input.Setup.ModuleNo, input.Setup.BitNo, ref raw);

            bool logical = input.Setup.IsNormallyClosed ? !raw : raw;
            if (ret == 0)
                input.ApplyScannedState(logical);

            return UpdateCached(input.Name, input.Setup.ModuleNo, input.Setup.BitNo, false, logical, input.Setup.IsNormallyClosed, ret);
        }

        private AjinIoSnapshot ReadOutput(BaseDigitalOutput output, bool hardwareOpen)
        {
            if (output == null) return BuildSnapshot(string.Empty, 0, 0, true, false, false, -1);

            if (output.Config.IsSimulationMode || !hardwareOpen)
            {
                // [실장비 오염 차단 2026-07-28] 입력과 같은 이유. 실보드 출력은 시뮬 저장값으로 갱신하지 않는다.
                if (!output.Config.IsSimulationMode)
                {
                    return UpdateCached(output.Name, output.Setup.ModuleNo, output.Setup.BitNo, true,
                        output.IsOn, output.Setup.IsNormallyClosed, 0, true);
                }

                bool simulatedLogical = output.IsOn;
                bool hasSimulatedState = TryGetSimulatedState(output, out simulatedLogical);
                if (hasSimulatedState)
                    output.ApplyScannedState(simulatedLogical);
                else
                    SetSimulatedState(output, simulatedLogical);

                return UpdateCached(output.Name, output.Setup.ModuleNo, output.Setup.BitNo, true, simulatedLogical, output.Setup.IsNormallyClosed, 0, true);
            }

            bool raw = false;
            int ret;
            lock (AxdSyncRoot)
                ret = AXD.ReadOutput(output.Setup.ModuleNo, output.Setup.BitNo, ref raw);

            bool logical = output.Setup.IsNormallyClosed ? !raw : raw;
            if (ret == 0)
                output.ApplyScannedState(logical);

            return UpdateCached(output.Name, output.Setup.ModuleNo, output.Setup.BitNo, true, logical, output.Setup.IsNormallyClosed, ret);
        }

        private AjinIoSnapshot UpdateCached(string name, int module, int bit, bool isOutput, bool isOn, bool nc, int errorCode, bool isSimulated = false)
        {
            AjinIoSnapshot snapshot = BuildSnapshot(name, module, bit, isOutput, isOn, nc, errorCode, isSimulated);
            lock (_gate)
            {
                _latest[name] = snapshot;
                if (IsValidPoint(module, bit))
                    _latest[MakeKey(module, bit, isOutput)] = snapshot;
            }
            return snapshot;
        }

        private static AjinIoSnapshot BuildSnapshot(string name, int module, int bit, bool isOutput, bool isOn, bool nc, int errorCode, bool isSimulated = false)
        {
            return new AjinIoSnapshot
            {
                Name = name ?? string.Empty,
                Module = module,
                Bit = bit,
                IsOutput = isOutput,
                IsOn = isOn,
                Nc = nc,
                ErrorCode = errorCode,
                Timestamp = DateTime.Now,
                IsSimulated = isSimulated
            };
        }

        private static string MakeKey(int module, int bit, bool isOutput)
        {
            return (isOutput ? "O:" : "I:") + module + ":" + bit;
        }

        private static bool IsValidPoint(int module, int bit)
        {
            return module >= 0 && bit >= 0;
        }

        private static bool HasSimulationPorts(IReadOnlyList<BaseDigitalInput> inputs, IReadOnlyList<BaseDigitalOutput> outputs)
        {
            if (inputs != null)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    if (inputs[i] != null && inputs[i].Config.IsSimulationMode)
                        return true;
                }
            }

            if (outputs != null)
            {
                for (int i = 0; i < outputs.Count; i++)
                {
                    if (outputs[i] != null && outputs[i].Config.IsSimulationMode)
                        return true;
                }
            }

            return false;
        }

        private void RaiseIoStatusUpdated(AjinIoSnapshot snapshot)
        {
            var handler = IoStatusUpdated;
            if (handler == null) return;
            try { handler(snapshot); } catch { }
        }

        private void RaiseCycleCompleted(IReadOnlyList<AjinIoSnapshot> snapshots)
        {
            var handler = CycleCompleted;
            if (handler == null) return;
            try { handler(snapshots); } catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
