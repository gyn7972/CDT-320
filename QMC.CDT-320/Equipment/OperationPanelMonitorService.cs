using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;
using QMC.Common.IO;

namespace QMC.CDT320
{
    public sealed class OperationPanelMonitorService : IDisposable
    {
        private readonly CDT320_Machine _machine;
        private readonly MachineController _controller;
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private bool _prevStart;
        private bool _prevStop;
        private bool _prevReset;
        private bool _rawStart;
        private bool _rawStop;
        private bool _rawReset;
        private bool _stableStart;
        private bool _stableStop;
        private bool _stableReset;
        private bool _buttonStatesInitialized;
        private bool _startReleaseRequired;
        private bool _stopReleaseRequired;
        private bool _resetReleaseRequired;
        private long _startChangedTick;
        private long _stopChangedTick;
        private long _resetChangedTick;
        private bool _prevEmgFront;
        private int _commandBusy;
        private int _buzzerMuted;
        private const int ButtonDebounceMs = 250;

        public OperationPanelMonitorService(CDT320_Machine machine, MachineController controller)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Start()
        {
            Stop();
            ResetButtonMonitorState();
            ResetSimulatedCommandInputs();
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        }

        public void Stop()
        {
            var cts = _cts;
            var task = _loopTask;
            _cts = null;
            _loopTask = null;

            if (cts == null)
                return;

            try { cts.Cancel(); } catch { }
            try { task?.Wait(500); } catch { }
            try { cts.Dispose(); } catch { }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch
                {
                    // Keep the monitor alive; UI/manual operation should not die from one I/O read/write failure.
                }

                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }

        private void Tick()
        {
            var op = _machine.OpPanelUnit;
            if (op == null)
                return;

            UpdateInputs(op);

            // EMG FRONT 신호의 하강 엣지(ON→OFF)에서 등록된 전 서보 축을 ServoOff + MoveStop 한다.
            // 안전 동작이므로 RunCommand의 busy 가드를 거치지 않고 즉시 동기 실행한다.
            bool emgFront = IsOn(op.EmgFront);
            if (_prevEmgFront && !emgFront)
                HandleEmgFrontEdge(emgFront);
            _prevEmgFront = emgFront;

            bool forceCommandInputsOff = ShouldForceCommandInputsOff();
            bool rawStart = forceCommandInputsOff ? false : IsOn(op.StartButton);
            bool rawStop = forceCommandInputsOff ? false : IsOn(op.StopButton);
            bool rawReset = forceCommandInputsOff ? false : IsOn(op.ResetButton);
            if (!_buttonStatesInitialized)
            {
                InitializeButtonStates(rawStart, rawStop, rawReset);
                ApplyLampState(op, _stableStart, _stableReset);
                return;
            }

            bool start = ReadDebouncedButton(rawStart, ref _rawStart, ref _stableStart, ref _startChangedTick);
            bool stop = ReadDebouncedButton(rawStop, ref _rawStop, ref _stableStop, ref _stopChangedTick);
            bool reset = ReadDebouncedButton(rawReset, ref _rawReset, ref _stableReset, ref _resetChangedTick);

            if (!start)
                _startReleaseRequired = false;
            if (!stop)
                _stopReleaseRequired = false;
            if (!reset)
                _resetReleaseRequired = false;

            if (start && !_prevStart && !_startReleaseRequired)
            {
                _startReleaseRequired = true;
                if (CanAcceptStartCommand())
                    RunCommand(HandleStartAsync);
            }

            if (stop && !_prevStop && !_stopReleaseRequired)
            {
                _stopReleaseRequired = true;
                RunCommand(HandleStopAsync);
            }

            if (reset && !_prevReset && !_resetReleaseRequired)
            {
                _resetReleaseRequired = true;
                RunCommand(HandleResetAsync);
            }

            _prevStart = start;
            _prevStop = stop;
            _prevReset = reset;

            ApplyLampState(op, start, reset);
        }

        private void ResetButtonMonitorState()
        {
            _prevStart = false;
            _prevStop = false;
            _prevReset = false;
            _rawStart = false;
            _rawStop = false;
            _rawReset = false;
            _stableStart = false;
            _stableStop = false;
            _stableReset = false;
            _startReleaseRequired = false;
            _stopReleaseRequired = false;
            _resetReleaseRequired = false;
            _startChangedTick = 0;
            _stopChangedTick = 0;
            _resetChangedTick = 0;
            _buttonStatesInitialized = false;
        }

        private void ResetSimulatedCommandInputs()
        {
            try
            {
                var op = _machine != null ? _machine.OpPanelUnit : null;
                if (op == null)
                    return;

                SimulateOffIfAllowed(op.StartButton);
                SimulateOffIfAllowed(op.StopButton);
                SimulateOffIfAllowed(op.ResetButton);
            }
            catch
            {
            }
        }

        private static void SimulateOffIfAllowed(BaseDigitalInput input)
        {
            try
            {
                if (input != null && input.Config != null && input.Config.IsSimulationMode)
                    input.SimulateInput(false);
            }
            catch
            {
            }
        }

        private void InitializeButtonStates(bool start, bool stop, bool reset)
        {
            long now = Environment.TickCount;
            _rawStart = _stableStart = _prevStart = start;
            _rawStop = _stableStop = _prevStop = stop;
            _rawReset = _stableReset = _prevReset = reset;
            _startChangedTick = now;
            _stopChangedTick = now;
            _resetChangedTick = now;
            _buttonStatesInitialized = true;
        }

        private static bool ReadDebouncedButton(
            bool current,
            ref bool raw,
            ref bool stable,
            ref long changedTick)
        {
            long now = Environment.TickCount;
            if (current != raw)
            {
                raw = current;
                changedTick = now;
            }

            if (stable != raw && ElapsedMs(changedTick, now) >= ButtonDebounceMs)
                stable = raw;

            return stable;
        }

        private static int ElapsedMs(long startTick, long nowTick)
        {
            return unchecked((int)(nowTick - startTick));
        }

        private bool CanAcceptStartCommand()
        {
            if (IsAlarmActive())
                return false;

            if (_controller.IsSequenceRunning ||
                _controller.IsManualBusy ||
                _controller.Status == EquipmentStatus.AutoRunning ||
                _controller.Status == EquipmentStatus.ManualRunning)
                return false;

            return true;
        }

        /// <summary>
        /// EMG FRONT 신호의 상승/하강 엣지에서 등록된 전 서보 축을 검색해 ServoOff 한 뒤 MoveStop(정지)한다.
        /// </summary>
        private void HandleEmgFrontEdge(bool isOn)
        {
            try
            {
                var axes = QMC.CDT320.Ajin.AjinFactory.AxisManager.GetAll();
                if (axes == null)
                    return;

                int count = 0;
                foreach (var axis in axes)
                {
                    if (axis == null)
                        continue;

                    try
                    {
                      axis.Stop();
                      Thread.Sleep(100);
                      axis.ServoOff();
                    }
                    catch // 전축 move stop
                    {
                    }        

                    count++;
                }

                QMC.Common.Log.Write("Main", "SAFETY", "OperationPanelMonitor",
                    "EMG FRONT " + (isOn ? "ON" : "OFF") + " edge: servo off + move stop applied. axes=" + count + " - Ok");
            }
            catch (Exception ex)
            {
                try
                {
                    QMC.Common.Log.Write("Main", "SAFETY", "OperationPanelMonitor",
                        "EMG FRONT edge handling failed: " + ex.Message + " - Failed");
                }
                catch
                {
                }
            }
        }

        public void StopBuzzer()
        {
            Interlocked.Exchange(ref _buzzerMuted, 1);
            var op = _machine.OpPanelUnit;
            if (op != null)
                Write(op.Buzzer, false);
        }

        private static void UpdateInputs(OperationPanelUnit op)
        {
            if (ShouldForceCommandInputsOff())
            {
                TryUpdate(op.EmgFront);
                TryUpdate(op.EmgLeft);
                TryUpdate(op.EmgRear);
                TryUpdate(op.OpEmgOn);
                return;
            }

            TryUpdate(op.StartButton);
            TryUpdate(op.StopButton);
            TryUpdate(op.ResetButton);
            TryUpdate(op.EmgFront);
            TryUpdate(op.EmgLeft);
            TryUpdate(op.EmgRear);
            TryUpdate(op.OpEmgOn);
        }

        private static bool ShouldForceCommandInputsOff()
        {
            var settings = AppSettingsStore.Current;
            return settings != null && (settings.BypassHardware || settings.SimulationMode);
        }

        private void ApplyLampState(OperationPanelUnit op, bool startPressed, bool resetPressed)
        {
            bool alarm = IsAlarmActive();
            bool autoRunning = IsAutoRunning();
            bool manualRunning = IsManualRunning();
            bool running = autoRunning || manualRunning;

            if (alarm)
            {
                Write(op.TlRed, true);
                Write(op.TlYellow, false);
                Write(op.TlGreen, false);
                Write(op.Buzzer, Interlocked.CompareExchange(ref _buzzerMuted, 0, 0) == 0);
            }
            else if (autoRunning)
            {
                Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, false);
                Write(op.TlGreen, true);
                Write(op.Buzzer, false);
            }
            else if (manualRunning)
            {
                Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, true);
                Write(op.TlGreen, true);
                Write(op.Buzzer, false);
            }
            else
            {
                Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, true);
                Write(op.TlGreen, false);
                Write(op.Buzzer, false);
            }

            Write(op.StartLamp, startPressed || running);
            Write(op.StopLamp, !running);
            Write(op.ResetLamp, resetPressed);
        }

        private bool IsAlarmActive()
        {
            return _controller.Status == EquipmentStatus.Alarm ||
                   (AlarmManager.Active != null && AlarmManager.Active.Count > 0);
        }

        private bool IsAutoRunning()
        {
            return _controller.Status == EquipmentStatus.AutoRunning ||
                   (_controller.IsSequenceRunning &&
                    _controller.ActiveSequenceRunMode == SequenceRunMode.Auto);
        }

        private bool IsManualRunning()
        {
            return _controller.IsManualBusy ||
                   _controller.Status == EquipmentStatus.ManualRunning ||
                   (_controller.IsSequenceRunning &&
                    _controller.ActiveSequenceRunMode.HasValue &&
                    _controller.ActiveSequenceRunMode.Value != SequenceRunMode.Auto);
        }

        private void RunCommand(Func<Task> action)
        {
            if (Interlocked.Exchange(ref _commandBusy, 1) == 1)
                return;

            Task.Run(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch
                {
                }
                finally
                {
                    Interlocked.Exchange(ref _commandBusy, 0);
                }
            });
        }

        private async Task HandleStartAsync()
        {
            if (!CanAcceptStartCommand())
                return;

            await _controller.StartAsync().ConfigureAwait(false);
        }

        private async Task HandleStopAsync()
        {
            await _controller.StopSequenceAsync().ConfigureAwait(false);
            await _controller.StopAsync().ConfigureAwait(false);
        }

        private async Task HandleResetAsync()
        {
            var op = _machine.OpPanelUnit;
            if (op != null)
                Write(op.ResetLamp, true);

            await _controller.ResetAlarmAsync().ConfigureAwait(false);

            await Task.Delay(150).ConfigureAwait(false);
        }

        private static void TryUpdate(BaseDigitalInput input)
        {
            try { input?.UpdateStatus(); } catch { }
        }

        private static bool IsOn(BaseDigitalInput input)
        {
            try { return input != null && input.IsOn; } catch { return false; }
        }

        private static void Write(BaseDigitalOutput output, bool on)
        {
            if (output == null)
                return;

            try
            {
                if (on)
                    output.On();
                else
                    output.Off();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
