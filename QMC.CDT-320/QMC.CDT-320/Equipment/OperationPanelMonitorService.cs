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
        private int _commandBusy;
        private int _buzzerMuted;

        public OperationPanelMonitorService(CDT320_Machine machine, MachineController controller)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Start()
        {
            Stop();
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

            bool start = IsOn(op.StartButton);
            bool stop = IsOn(op.StopButton);
            bool reset = IsOn(op.ResetButton);

            if (start && !_prevStart)
                RunCommand(HandleStartAsync);
            if (stop && !_prevStop)
                RunCommand(HandleStopAsync);
            if (reset && !_prevReset)
                RunCommand(HandleResetAsync);

            _prevStart = start;
            _prevStop = stop;
            _prevReset = reset;

            ApplyLampState(op, start, reset);
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
            TryUpdate(op.StartButton);
            TryUpdate(op.StopButton);
            TryUpdate(op.ResetButton);
            TryUpdate(op.EmgFront);
            TryUpdate(op.EmgLeft);
            TryUpdate(op.EmgRear);
            TryUpdate(op.OpEmgOn);
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
            if (IsAlarmActive())
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
