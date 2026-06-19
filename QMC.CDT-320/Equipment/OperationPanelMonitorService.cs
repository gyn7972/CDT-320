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
        private bool _prevEmgFront;
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

            // EMG FRONT 신호의 하강 엣지(ON→OFF)에서 등록된 전 서보 축을 ServoOff + MoveStop 한다.
            // 안전 동작이므로 RunCommand의 busy 가드를 거치지 않고 즉시 동기 실행한다.
            bool emgFront = IsOn(op.EmgFront);
            if (_prevEmgFront && !emgFront)
                HandleEmgFrontEdge(emgFront);
            _prevEmgFront = emgFront;

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
