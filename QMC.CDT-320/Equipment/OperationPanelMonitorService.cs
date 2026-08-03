using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;
using QMC.Common.IO;

namespace QMC.CDT320
{
    /// <summary>
    /// 물리 조작반의 START/STOP/RESET 및 비상정지 입력을 주기적으로 감시하고,
    /// 장비 상태에 맞춰 MachineController 명령과 Tower Lamp/부저 출력을 제어합니다.
    /// </summary>
    public sealed class OperationPanelMonitorService : IDisposable
    {
        #region Constants

        // 조작반 입력을 읽는 주기입니다. 짧은 버튼 입력을 놓치지 않도록 20ms마다 확인합니다.
        private const int MonitorPollIntervalMs = 20;

        // 접점 채터링을 실제 버튼 조작으로 오인하지 않기 위한 안정화 시간입니다.
        private const int ButtonDebounceMs = 50;

        // 불필요한 Digital Output 쓰기를 줄이기 위한 램프/부저 갱신 주기입니다.
        private const int LampRefreshIntervalMs = 100;

        #endregion

        #region Dependencies and Monitor Lifetime

        // 조작반 I/O에 접근하기 위한 장비 객체입니다.
        private readonly CDT320_Machine _machine;

        // START/STOP/RESET 명령을 실제 장비 제어 로직으로 전달하는 컨트롤러입니다.
        private readonly MachineController _controller;

        // 백그라운드 감시 루프의 취소와 종료 상태를 관리합니다.
        private CancellationTokenSource _cts;
        private Task _loopTask;

        #endregion

        #region Button Debounce and Edge State

        // 직전 Tick의 안정화된 버튼 상태입니다. OFF→ON 상승 엣지를 한 번만 검출할 때 사용합니다.
        private bool _prevStart;
        private bool _prevStop;
        private bool _prevReset;

        // I/O에서 마지막으로 읽은 원시 버튼 상태입니다.
        private bool _rawStart;
        private bool _rawStop;
        private bool _rawReset;

        // 디바운스 시간이 지난 뒤 확정된 버튼 상태입니다.
        private bool _stableStart;
        private bool _stableStop;
        private bool _stableReset;
        private bool _buttonStatesInitialized;

        // 버튼을 계속 누른 상태에서 명령이 반복 실행되지 않도록 OFF 해제를 요구하는 래치입니다.
        private bool _startReleaseRequired;
        private bool _stopReleaseRequired;
        private bool _resetReleaseRequired;

        // 각 원시 입력이 마지막으로 변한 Tick입니다. 디바운스 경과시간 계산에 사용합니다.
        private long _startChangedTick;
        private long _stopChangedTick;
        private long _resetChangedTick;

        #endregion

        #region Safety, Output and Command State

        // 모든 EMO 입력의 최초 상태 확인과 중복 비상정지 요청 방지에 사용하는 래치입니다.
        private bool _emergencyStatesInitialized;
        private bool _emergencyStopLatched;

        // Input/Output Feeder Overload의 최초 상태와 중복 Alarm 발생을 제어합니다.
        private bool _inputFeederOverloadInitialized;
        private bool _outputFeederOverloadInitialized;
        private bool _inputFeederOverloadLatched;
        private bool _outputFeederOverloadLatched;

        // 마지막 Tower Lamp/부저 출력 갱신 시점입니다.
        private long _lastLampRefreshTick;

        // 일반 명령(START)의 중복 실행을 막는 원자적 busy 플래그입니다.
        private int _commandBusy;

        // 부저 정지 요청과 Run Review 부저 요청을 여러 스레드에서 안전하게 공유합니다.
        private int _buzzerMuted;
        private int _runReviewBuzzerRequested;

        #endregion

        #region Construction and Lifetime

        /// <summary>조작반 I/O가 포함된 장비 객체와 명령을 처리할 컨트롤러를 연결합니다.</summary>
        public OperationPanelMonitorService(CDT320_Machine machine, MachineController controller)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <summary>기존 감시 루프를 정리하고 조작반 감시를 새로 시작합니다.</summary>
        public void Start()
        {
            Stop();
            ResetButtonMonitorState();
            ResetSimulatedCommandInputs();
            Interlocked.Exchange(ref _buzzerMuted, 0);
            Interlocked.Exchange(ref _runReviewBuzzerRequested, 0);
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        }

        /// <summary>감시 루프에 취소를 요청하고 최대 500ms 동안 종료를 기다립니다.</summary>
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

        #endregion

        #region Monitor Loop and Button Command Routing

        /// <summary>서비스가 중지될 때까지 일정 주기로 조작반 상태를 갱신합니다.</summary>
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

                await Task.Delay(MonitorPollIntervalMs, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 한 번의 감시 주기에서 입력 갱신, EMG 엣지 처리, 버튼 명령 전달 및 출력 갱신을 수행합니다.
        /// </summary>
        private void Tick()
        {
            var op = _machine.OpPanelUnit;
            if (op == null)
                return;

            UpdateInputs(op);

            // Feeder Overload는 조작반 버튼과 별개인 안전 입력이므로 매 Tick마다 하드웨어 값을 갱신해 감시합니다.
            MonitorFeederOverloadInputs();

            // EMO 안전 입력은 정상 상태가 ON이며, 어느 하나라도 OFF가 되면 공통 비상정지를 실행합니다.
            // 여러 EMO 접점과 전장 안전 신호가 동시에 OFF되어도 한 번만 처리하고, 모두 복귀한 뒤 래치를 해제합니다.
            MonitorEmergencyInputs(op);

            bool forceCommandInputsOff = ShouldForceCommandInputsOff();
            bool rawStart = forceCommandInputsOff ? false : IsOn(op.StartButton);
            bool rawStop = forceCommandInputsOff ? false : IsOn(op.StopButton);
            bool rawReset = forceCommandInputsOff ? false : IsOn(op.ResetButton);
            if (!_buttonStatesInitialized)
            {
                InitializeButtonStates(rawStart, rawStop, rawReset);
                ApplyLampState(op, _stableStart, _stableReset, true);
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
                    RunCommand("START", HandleStartAsync);
            }

            if (stop && !_prevStop && !_stopReleaseRequired)
            {
                _stopReleaseRequired = true;
                RunPriorityCommand("STOP", HandleStopAsync);
            }

            if (reset && !_prevReset && !_resetReleaseRequired)
            {
                _resetReleaseRequired = true;
                RunPriorityCommand("RESET ALARM", HandleResetAsync);
            }

            bool buttonStateChanged = start != _prevStart || stop != _prevStop || reset != _prevReset;
            ApplyLampState(op, start, reset, buttonStateChanged);

            _prevStart = start;
            _prevStop = stop;
            _prevReset = reset;
        }

        #endregion

        #region Button State Initialization and Debounce

        /// <summary>감시 재시작 시 이전 버튼 엣지와 디바운스 상태를 모두 초기화합니다.</summary>
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
            _lastLampRefreshTick = 0;
            _buttonStatesInitialized = false;
            _emergencyStatesInitialized = false;
            _emergencyStopLatched = false;
            _inputFeederOverloadInitialized = false;
            _outputFeederOverloadInitialized = false;
            _inputFeederOverloadLatched = false;
            _outputFeederOverloadLatched = false;
        }

        /// <summary>
        /// Simulation 입력에 이전 실행의 ON 상태가 남아 즉시 명령이 발생하지 않도록 버튼 입력을 OFF로 맞춥니다.
        /// </summary>
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

        /// <summary>Simulation Mode가 허용된 Digital Input만 강제로 OFF 처리합니다.</summary>
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

        /// <summary>
        /// 서비스 시작 시 현재 입력을 기준 상태로 채택하여 이미 눌린 버튼을 신규 엣지로 처리하지 않습니다.
        /// </summary>
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

        /// <summary>원시 입력이 설정 시간 동안 유지됐을 때만 안정 상태를 변경합니다.</summary>
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

        /// <summary>Environment.TickCount의 오버플로를 허용하면서 경과시간을 계산합니다.</summary>
        private static int ElapsedMs(long startTick, long nowTick)
        {
            return unchecked((int)(nowTick - startTick));
        }

        /// <summary>
        /// Alarm 또는 다른 자동/수동 동작이 진행 중이면 조작반 START 명령을 차단합니다.
        /// 세부 START 안전 조건은 MachineController.StartAsync에서도 다시 검증합니다.
        /// </summary>
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

        #endregion

        #region Feeder Overload Safety Monitoring

        /// <summary>
        /// Input/Output Feeder Overload 센서를 감시합니다.
        /// 센서는 NC 논리로 ON이 정상, OFF가 Overload이며 실장비에서 매 Tick마다 직접 갱신합니다.
        /// </summary>
        private void MonitorFeederOverloadInputs()
        {
            MonitorInputFeederOverload();
            MonitorOutputFeederOverload();
        }

        /// <summary>Input Feeder Overload 신규 감지 시 InputFeederY를 즉시 EStop하고 Alarm을 발생시킵니다.</summary>
        private void MonitorInputFeederOverload()
        {
            InputFeederUnit feeder = _machine != null ? _machine.InputFeederUnit : null;
            if (feeder == null || feeder.IsWaferFeederSimulationOrDryRun())
                return;

            BaseDigitalInput sensor = feeder.WaferFeederOverloadSensor;
            int readError = -1;
            if (sensor == null || !AjinIoScanService.TryReadHardwareInput(sensor, out readError))
                return;

            bool overload = feeder.IsWaferFeederOverload();
            if (!_inputFeederOverloadInitialized)
            {
                _inputFeederOverloadInitialized = true;
                _inputFeederOverloadLatched = overload;
                if (overload)
                    HandleFeederOverload("InputFeeder", feeder.FeederY, "INPUT-FEEDER-OVERLOAD-LIVE");
                return;
            }

            if (!overload)
            {
                _inputFeederOverloadLatched = false;
                return;
            }

            if (_inputFeederOverloadLatched)
                return;

            _inputFeederOverloadLatched = true;
            HandleFeederOverload("InputFeeder", feeder.FeederY, "INPUT-FEEDER-OVERLOAD-LIVE");
        }

        /// <summary>Output Feeder Overload 신규 감지 시 OutputFeederY를 즉시 EStop하고 Alarm을 발생시킵니다.</summary>
        private void MonitorOutputFeederOverload()
        {
            OutputFeederUnit feeder = _machine != null ? _machine.OutputFeederUnit : null;
            if (feeder == null || feeder.IsOutputFeederSimulationOrDryRun())
                return;

            BaseDigitalInput sensor = feeder.BinFeederOverloadSensor;
            int readError = -1;
            if (sensor == null || !AjinIoScanService.TryReadHardwareInput(sensor, out readError))
                return;

            bool overload = feeder.IsFeederOverload();
            if (!_outputFeederOverloadInitialized)
            {
                _outputFeederOverloadInitialized = true;
                _outputFeederOverloadLatched = overload;
                if (overload)
                    HandleFeederOverload("OutputFeeder", feeder.FeederY, "OUTPUT-FEEDER-OVERLOAD-LIVE");
                return;
            }

            if (!overload)
            {
                _outputFeederOverloadLatched = false;
                return;
            }

            if (_outputFeederOverloadLatched)
                return;

            _outputFeederOverloadLatched = true;
            HandleFeederOverload("OutputFeeder", feeder.FeederY, "OUTPUT-FEEDER-OVERLOAD-LIVE");
        }

        /// <summary>
        /// Overload가 발생한 Feeder Y축을 가장 먼저 EStop한 뒤 Error Alarm을 발생시킵니다.
        /// 공통 AlarmResponseService가 이어서 전체 축 EStop과 실행 중 시퀀스 취소를 수행합니다.
        /// Servo OFF는 수행하지 않습니다.
        /// </summary>
        private static void HandleFeederOverload(string feederName, QMC.Common.Motion.BaseAxis feederYAxis, string alarmCode)
        {
            string source = feederYAxis != null && !string.IsNullOrWhiteSpace(feederYAxis.Name)
                ? feederYAxis.Name
                : feederName;

            try
            {
                if (feederYAxis != null)
                    feederYAxis.EStop();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SAFETY",
                    "OperationPanelMonitor",
                    feederName + " Y축 Overload 즉시 EStop 실패. error=" + ex.Message + " - Failed");
            }

            AlarmManager.Raise(
                AlarmSeverity.Error,
                alarmCode,
                source,
                feederName + " Overload 센서가 감지되었습니다. " +
                "해당 Feeder Y축을 즉시 EStop하고 전체 축 비상정지를 요청했습니다. Servo는 ON 상태를 유지합니다.");
        }

        #endregion

        #region Emergency Input Safety Handling

        /// <summary>
        /// 전장, 조작반, 후면, 우측 EMO 입력을 하나의 안전 조건으로 감시합니다.
        /// 최초 I/O 값은 기동 오검출 방지를 위한 기준 상태로만 사용하고,
        /// 모든 입력이 ON으로 확인된 뒤 어느 하나가 OFF가 되면 비상정지를 실행합니다.
        /// </summary>
        private void MonitorEmergencyInputs(OperationPanelUnit op)
        {
            if (op == null)
                return;

            bool elecEmgOn = IsOn(op.EmgFront);
            bool opEmgOn = IsOn(op.OpEmgOn);
            bool rearEmgOn = IsOn(op.EmgRear);
            bool rightEmgOn = IsOn(op.EmgLeft);
            bool allEmergencyInputsOn = elecEmgOn && opEmgOn && rearEmgOn && rightEmgOn;

            if (!_emergencyStatesInitialized)
            {
                _emergencyStatesInitialized = true;
                _emergencyStopLatched = !allEmergencyInputsOn;
                return;
            }

            if (allEmergencyInputsOn)
            {
                if (_emergencyStopLatched)
                {
                    QMC.Common.Log.Write(
                        "Main",
                        "SAFETY",
                        "OperationPanelMonitor",
                        "모든 EMO 안전 입력이 ON으로 복귀했습니다. 다음 EMO 감지를 활성화합니다. - Reset");
                }

                _emergencyStopLatched = false;
                return;
            }

            if (_emergencyStopLatched)
                return;

            _emergencyStopLatched = true;
            HandleEmergencyStop(elecEmgOn, opEmgOn, rearEmgOn, rightEmgOn, "RuntimeEdge");
        }

        /// <summary>
        /// 감지된 EMO 입력 상태를 기록하고 MachineController의 공통 비상정지 경로를 즉시 실행합니다.
        /// 이 경로는 전축 E-STOP/Servo OFF, 실행 취소, Alarm 상태 및 Tower Lamp/부저 처리를 포함합니다.
        /// </summary>
        private void HandleEmergencyStop(
            bool elecEmgOn,
            bool opEmgOn,
            bool rearEmgOn,
            bool rightEmgOn,
            string detectionSource)
        {
            try
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SAFETY",
                    "OperationPanelMonitor",
                    "EMO 안전 입력 OFF 감지. source=" + (detectionSource ?? "Unknown") +
                    ", ElecEmgOn=" + elecEmgOn +
                    ", OpEmgOn=" + opEmgOn +
                    ", RearEmgOn=" + rearEmgOn +
                    ", RightEmgOn=" + rightEmgOn + " - EmergencyStop");

                // EMO는 START/STOP/RESET busy 상태보다 우선하므로 감시 Tick에서 즉시 동기 실행합니다.
                _controller.EmergencyStopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogCommandException("EMERGENCY STOP", ex);
            }
        }

        #endregion

        #region Buzzer Control

        /// <summary>현재 부저를 끄고 Alarm/Run Review 상태가 유지돼도 다시 켜지지 않도록 음소거합니다.</summary>
        public void StopBuzzer()
        {
            Interlocked.Exchange(ref _buzzerMuted, 1);
            var op = _machine.OpPanelUnit;
            if (op != null)
                Write(op.Buzzer, false);
        }

        /// <summary>Input Stage Run Review가 작업자 확인을 요구할 때 부저 출력을 요청합니다.</summary>
        public void StartRunReviewBuzzer()
        {
            Interlocked.Exchange(ref _runReviewBuzzerRequested, 1);
            Interlocked.Exchange(ref _buzzerMuted, 0);

            var op = _machine.OpPanelUnit;
            if (op != null)
                ApplyLampState(op, _stableStart, _stableReset, true);
        }

        /// <summary>Run Review 부저 요청을 해제하고 현재 장비 상태에 맞게 출력을 다시 계산합니다.</summary>
        public void EndRunReviewBuzzer()
        {
            Interlocked.Exchange(ref _runReviewBuzzerRequested, 0);
            if (!IsAlarmActive())
                Interlocked.Exchange(ref _buzzerMuted, 0);

            var op = _machine.OpPanelUnit;
            if (op != null)
                ApplyLampState(op, _stableStart, _stableReset, true);
        }

        #endregion

        #region Operation Panel Input Update

        /// <summary>
        /// 조작반 Digital Input 상태를 장치에서 갱신합니다.
        /// Hardware Bypass/Simulation에서는 START/STOP/RESET 실입력을 읽지 않고 EMG 계열만 갱신합니다.
        /// </summary>
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

        /// <summary>Hardware Bypass 또는 Simulation 설정이면 물리 명령 버튼을 강제로 OFF 취급합니다.</summary>
        private static bool ShouldForceCommandInputsOff()
        {
            var settings = AppSettingsStore.Current;
            return settings != null && (settings.BypassHardware || settings.SimulationMode);
        }

        #endregion

        #region Tower Lamp, Button Lamp and Buzzer Output

        /// <summary>
        /// Alarm/Auto/Manual/Idle 우선순위로 Tower Lamp와 부저를 결정하고 버튼 램프를 갱신합니다.
        /// Alarm은 적색, Auto는 녹색, Manual은 황색+녹색, 대기는 황색으로 표시합니다.
        /// </summary>
        private void ApplyLampState(OperationPanelUnit op, bool startPressed, bool resetPressed, bool force = false)
        {
            long now = Environment.TickCount;
            if (!force && ElapsedMs(_lastLampRefreshTick, now) < LampRefreshIntervalMs)
                return;

            _lastLampRefreshTick = now;

            bool alarm = IsAlarmActive();
            bool autoRunning = IsAutoRunning();
            bool manualRunning = IsManualRunning();
            bool running = autoRunning || manualRunning;
            bool runReviewBuzzerRequested =
                Interlocked.CompareExchange(ref _runReviewBuzzerRequested, 0, 0) != 0;

            if (alarm)
            {
                Write(op.TlRed, true);
                Write(op.TlYellow, false);
                Write(op.TlGreen, false);
            }
            else if (autoRunning)
            {
                if (!runReviewBuzzerRequested)
                    Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, false);
                Write(op.TlGreen, true);
            }
            else if (manualRunning)
            {
                if (!runReviewBuzzerRequested)
                    Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, true);
                Write(op.TlGreen, true);
            }
            else
            {
                if (!runReviewBuzzerRequested)
                    Interlocked.Exchange(ref _buzzerMuted, 0);
                Write(op.TlRed, false);
                Write(op.TlYellow, true);
                Write(op.TlGreen, false);
            }

            bool buzzerOn = (alarm || runReviewBuzzerRequested) &&
                            Interlocked.CompareExchange(ref _buzzerMuted, 0, 0) == 0;
            Write(op.Buzzer, buzzerOn);

            Write(op.StartLamp, startPressed || running);
            Write(op.StopLamp, !running);
            Write(op.ResetLamp, resetPressed);
        }

        #endregion

        #region Controller State Queries

        /// <summary>컨트롤러 상태 또는 AlarmManager에 활성 알람이 하나라도 있는지 확인합니다.</summary>
        private bool IsAlarmActive()
        {
            return _controller.Status == EquipmentStatus.Alarm ||
                   (AlarmManager.Active != null && AlarmManager.Active.Count > 0);
        }

        /// <summary>Input Stage Review 수동 상태를 제외한 Auto 시퀀스 실행 여부를 확인합니다.</summary>
        private bool IsAutoRunning()
        {
            if (_controller.IsInputStageRunReviewManualActive)
                return false;

            return _controller.Status == EquipmentStatus.AutoRunning ||
                   (_controller.IsSequenceRunning &&
                    _controller.ActiveSequenceRunMode == SequenceRunMode.Auto);
        }

        /// <summary>수동 동작, Run Review 또는 Auto가 아닌 시퀀스 실행 여부를 확인합니다.</summary>
        private bool IsManualRunning()
        {
            return _controller.IsInputStageRunReviewManualActive ||
                   _controller.IsManualBusy ||
                   _controller.Status == EquipmentStatus.ManualRunning ||
                   (_controller.IsSequenceRunning &&
                    _controller.ActiveSequenceRunMode.HasValue &&
                    _controller.ActiveSequenceRunMode.Value != SequenceRunMode.Auto);
        }

        #endregion

        #region Controller Command Dispatch

        /// <summary>
        /// START처럼 중복 실행하면 안 되는 일반 명령을 하나만 백그라운드에서 실행합니다.
        /// </summary>
        private void RunCommand(string commandName, Func<Task> action)
        {
            if (Interlocked.Exchange(ref _commandBusy, 1) == 1)
                return;

            Task.Run(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogCommandException(commandName, ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _commandBusy, 0);
                }
            });
        }

        /// <summary>
        /// STOP/RESET처럼 일반 START busy 상태와 무관하게 접수해야 하는 우선 명령을 실행합니다.
        /// </summary>
        private void RunPriorityCommand(string commandName, Func<Task> action)
        {
            Task.Run(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogCommandException(commandName, ex);
                }
            });
        }

        /// <summary>
        /// START 접수 조건과 전 축 HOME 준비 상태를 다시 확인한 뒤 자동 운전 시작을 요청합니다.
        /// 사이드 START의 AutoFocus 선택창 기본값과 동일하게, 시작 AutoFocus가 활성화된 경우
        /// 물리 버튼은 Rough + Fine을 기본 선택으로 사용합니다.
        /// </summary>
        private async Task HandleStartAsync()
        {
            if (!CanAcceptStartCommand())
                return;

            string homeReadyReason;
            if (!_controller.AreAllAxesHomeReady(out homeReadyReason))
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "OperationPanelMonitor",
                    "조작반 START 불가: 축 HOME END(원점복귀)가 완료되지 않았습니다. " +
                    (homeReadyReason ?? string.Empty) + " - Blocked");
                return;
            }

            RuntimeAutoFocusScanMode startupAutoFocusMode =
                _controller.IsRuntimeAutoFocusOnStartEnabled()
                    ? RuntimeAutoFocusScanMode.RoughAndFine
                    : RuntimeAutoFocusScanMode.None;

            int result = await _controller.StartAsync(startupAutoFocusMode).ConfigureAwait(false);
            if (result != 0)
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "OperationPanelMonitor",
                    "조작반 START 실패. result=" + result +
                    ", reason=" + (_controller.LastActionFailureMessage ?? string.Empty) + " - Failed");
            }
        }

        /// <summary>현재 운전 모드에 맞는 일반 정지 처리를 MachineController에 요청합니다.</summary>
        private async Task HandleStopAsync()
        {
            await _controller.StopAsync().ConfigureAwait(false);
        }

        /// <summary>RESET 램프를 점등하고 전체 축 및 AlarmManager의 알람 리셋을 요청합니다.</summary>
        private async Task HandleResetAsync()
        {
            var op = _machine.OpPanelUnit;
            if (op != null)
                Write(op.ResetLamp, true);

            await _controller.ResetAlarmAsync().ConfigureAwait(false);

            await Task.Delay(150).ConfigureAwait(false);
        }

        /// <summary>백그라운드 조작반 명령의 처리 예외를 명령 이름과 함께 기록합니다.</summary>
        private static void LogCommandException(string commandName, Exception ex)
        {
            try
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "OperationPanelMonitor",
                    "조작반 " + (commandName ?? "COMMAND") + " 처리 중 예외가 발생했습니다. error=" +
                    (ex != null ? ex.Message : "Unknown") + " - Failed");
            }
            catch
            {
            }
        }

        #endregion

        #region Digital I/O Helpers

        /// <summary>개별 Digital Input 읽기 실패가 감시 루프 전체를 중단시키지 않도록 보호합니다.</summary>
        private static void TryUpdate(BaseDigitalInput input)
        {
            try { input?.UpdateStatus(); } catch { }
        }

        /// <summary>Digital Input의 현재 ON 상태를 안전하게 조회합니다.</summary>
        private static bool IsOn(BaseDigitalInput input)
        {
            try { return input != null && input.IsOn; } catch { return false; }
        }

        /// <summary>현재 출력과 목표가 다를 때만 Digital Output을 변경합니다.</summary>
        private static void Write(BaseDigitalOutput output, bool on)
        {
            if (output == null)
                return;

            try
            {
                if (output.IsOn == on)
                    return;

                if (on)
                    output.On();
                else
                    output.Off();
            }
            catch
            {
            }
        }

        #endregion

        #region IDisposable

        /// <summary>서비스 해제 시 실행 중인 감시 루프를 종료합니다.</summary>
        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
