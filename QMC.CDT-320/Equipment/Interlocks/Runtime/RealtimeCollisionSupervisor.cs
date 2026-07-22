using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public sealed class RealtimeCollisionSupervisor : IDisposable
    {
        private const int DefaultMonitorPeriodMs = 100;
        private const int RiskLogThrottleMs = 10000;
        private const int StateLogThrottleMs = 30000;
        private const int HardStopRepeatStopMs = 250;
        private const int HardStopRepeatAlarmMs = 1000;

        private readonly object _sync = new object();
        private readonly CDT320_Machine _machine;
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private int _monitorPeriodMs = DefaultMonitorPeriodMs;
        private DateTime _lastRiskLogUtc = DateTime.MinValue;
        private DateTime _lastStateLogUtc = DateTime.MinValue;
        private DateTime _lastHardStopStopUtc = DateTime.MinValue;
        private DateTime _lastHardStopAlarmUtc = DateTime.MinValue;
        private int _hardStopRaised;
        private PickerSafetyPhase _frontPhase = PickerSafetyPhase.Idle;
        private PickerSafetyPhase _rearPhase = PickerSafetyPhase.Idle;
        private bool _frontCarrying;
        private bool _rearCarrying;
        private Action _stopAllAxesHandler;

        public RealtimeCollisionSupervisor(CDT320_Machine machine, SharedRailXConfig sharedRailXConfig)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// <summary>
        /// 실시간 위험 감지 시 실행할 전축 하드정지 동작을 등록한다(사용자 정책: 전축 하드정지).
        /// 미등록 시에는 Front/Rear Picker X/Y만 정지하는 안전 폴백을 사용한다.
        /// </summary>
        public void SetStopAllAxesHandler(Action handler)
        {
            lock (_sync)
                _stopAllAxesHandler = handler;
        }

        public bool IsRunning
        {
            get
            {
                lock (_sync)
                    return _cts != null && !_cts.IsCancellationRequested;
            }
        }

        public void Start(int monitorPeriodMs)
        {
            if (monitorPeriodMs < 5)
                monitorPeriodMs = 5;

            Stop();

            lock (_sync)
            {
                _monitorPeriodMs = monitorPeriodMs;
                _hardStopRaised = 0;
                _cts = new CancellationTokenSource();
                _loopTask = Task.Run(() => MonitorLoopAsync(_cts.Token), _cts.Token);
            }

            bool stopAllRegistered;
            lock (_sync)
                stopAllRegistered = _stopAllAxesHandler != null;

            QMC.Common.Log.Write("Main", "SYSTEM", "RealtimeCollisionSupervisor",
                "실시간 충돌 감시 시작. mode=HardStop, stopPolicy=" + (stopAllRegistered ? "전축" : "PickerXY폴백") +
                ", periodMs=" + monitorPeriodMs + " - Ok");
        }

        public void Stop()
        {
            CancellationTokenSource cts = null;
            Task loopTask = null;

            lock (_sync)
            {
                cts = _cts;
                loopTask = _loopTask;
                _cts = null;
                _loopTask = null;
            }

            if (cts == null)
                return;

            try { cts.Cancel(); } catch { }
            try { if (loopTask != null) loopTask.Wait(300); } catch { }
            try { cts.Dispose(); } catch { }

            QMC.Common.Log.Write("Main", "SYSTEM", "RealtimeCollisionSupervisor",
                "실시간 충돌 감시 정지. - Ok");
        }

        public void SetPickerPhase(PickerSafetySide side, PickerSafetyPhase phase, bool carrying)
        {
            lock (_sync)
            {
                if (side == PickerSafetySide.Front)
                {
                    _frontPhase = phase;
                    _frontCarrying = carrying;
                }
                else
                {
                    _rearPhase = phase;
                    _rearCarrying = carrying;
                }
            }
        }

        public CollisionGateResult CanMove()
        {
            return CollisionGateResult.Allow();
        }

        public MotionSafetyState CaptureState()
        {
            PickerSafetyPhase frontPhase;
            PickerSafetyPhase rearPhase;
            bool frontCarrying;
            bool rearCarrying;

            lock (_sync)
            {
                frontPhase = _frontPhase;
                rearPhase = _rearPhase;
                frontCarrying = _frontCarrying;
                rearCarrying = _rearCarrying;
            }

            PickerSafetySnapshot front = BuildPickerSnapshot(PickerSafetySide.Front, frontPhase, frontCarrying);
            PickerSafetySnapshot rear = BuildPickerSnapshot(PickerSafetySide.Rear, rearPhase, rearCarrying);
            AxisPairSafetySnapshot frontRearPair = BuildFrontRearFacingSnapshot(front, rear);

            return new MotionSafetyState
            {
                Front = front,
                Rear = rear,
                FrontRearPickerPair = frontRearPair,
                CapturedUtc = DateTime.UtcNow
            };
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    MotionSafetyState state = CaptureState();
                    EvaluateRealtime(state);
                    await Task.Delay(_monitorPeriodMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    WriteThrottledRiskLog("실시간 충돌 감시 중 예외가 발생했습니다. error=" + ex.Message);
                    try { await Task.Delay(_monitorPeriodMs, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        private void EvaluateRealtime(MotionSafetyState state)
        {
            if (state == null || state.Front == null || state.Rear == null)
                return;

            AxisPairSafetySnapshot pair = state.FrontRearPickerPair;
            if (pair == null)
                return;

            bool bothYNotRetracted = IsYNotRetracted(state.Front.YState) && IsYNotRetracted(state.Rear.YState);
            bool xPathUnsafe = DoesXPathEnterFacingClearance(state.Front, state.Rear, pair.RequiredClearance);
            bool safePairInitialize = IsSafePickerYPairInitialize(state);
            if (bothYNotRetracted && xPathUnsafe && !safePairInitialize)
            {
                string reason =
                    "실시간 충돌 감시 정지. Front/Rear PickerY가 둘 다 안전 위치가 아닌 상태에서 PickerX 거리가 안전거리 안으로 들어옵니다. " +
                    pair.Describe() + ", " + state.Front.Describe() + ", " + state.Rear.Describe();
                RaiseHardStop(reason);
                return;
            }

            Interlocked.Exchange(ref _hardStopRaised, 0);
            _lastHardStopStopUtc = DateTime.MinValue;
            _lastHardStopAlarmUtc = DateTime.MinValue;
            WriteThrottledStateLog(
                "실시간 충돌 감시 상태. " +
                pair.Describe() + ", " + state.Front.Describe() + ", " + state.Rear.Describe());
        }

        private bool IsSafePickerYPairInitialize(MotionSafetyState state)
        {
            if (state == null || state.Front == null || state.Rear == null ||
                state.Front.Carrying || state.Rear.Carrying ||
                state.Front.XMoving || state.Rear.XMoving)
            {
                return false;
            }

            BaseAxis frontYBase = ResolvePickerY(true);
            BaseAxis rearYBase = ResolvePickerY(false);
            if (frontYBase == null || rearYBase == null)
                return false;

            if (MotionGuardRuntime.IsPickerYPairInitializeHomeActive(frontYBase, rearYBase))
                return true;

            AjinAxis frontY = frontYBase as AjinAxis;
            AjinAxis rearY = rearYBase as AjinAxis;
            if (frontY == null || rearY == null)
                return false;

            return MotionGuardRuntime.IsPickerYPairLimitSearchActive(frontY, rearY) &&
                   (state.Front.YMoving || state.Rear.YMoving) &&
                   frontY.IsInitializeHardwareLimitSearchActive(-1) &&
                   rearY.IsInitializeHardwareLimitSearchActive(1);
        }

        private AxisPairSafetySnapshot BuildFrontRearFacingSnapshot(PickerSafetySnapshot front, PickerSafetySnapshot rear)
        {
            return new AxisPairSafetySnapshot
            {
                PairName = "FrontPickerX<->RearPickerX",
                Configured = true,
                AxisAActual = front != null ? front.XActual : 0.0,
                AxisBActual = rear != null ? rear.XActual : 0.0,
                AxisACommand = front != null ? front.XCommand : 0.0,
                AxisBCommand = rear != null ? rear.XCommand : 0.0,
                AxisAMoving = front != null && front.XMoving,
                AxisBMoving = rear != null && rear.XMoving,
                RequiredClearance = ResolvePickerYFacingClearance(),
                ActualClearance = front != null && rear != null ? Math.Abs(front.XActual - rear.XActual) : 0.0,
                TargetClearance = front != null && rear != null ? Math.Abs(front.XCommand - rear.XCommand) : 0.0,
                RuleKind = "PickerYFacingDistance"
            };
        }

        private static bool IsYNotRetracted(PickerSafetyYState state)
        {
            return state == PickerSafetyYState.Forward ||
                   state == PickerSafetyYState.Moving ||
                   state == PickerSafetyYState.Unknown;
        }

        private static bool DoesXPathEnterFacingClearance(
            PickerSafetySnapshot front,
            PickerSafetySnapshot rear,
            double clearance)
        {
            if (front == null || rear == null || clearance <= 0.0)
                return false;

            double currentDistance = Math.Abs(front.XActual - rear.XActual);
            if (currentDistance <= clearance)
                return true;

            if (!front.XMoving && !rear.XMoving)
                return false;

            double frontStart = front.XActual;
            double frontEnd = front.XMoving ? front.XCommand : front.XActual;
            double rearStart = rear.XActual;
            double rearEnd = rear.XMoving ? rear.XCommand : rear.XActual;
            double frontMin = Math.Min(frontStart, frontEnd) - clearance;
            double frontMax = Math.Max(frontStart, frontEnd) + clearance;
            double rearMin = Math.Min(rearStart, rearEnd);
            double rearMax = Math.Max(rearStart, rearEnd);
            return rearMax >= frontMin && rearMin <= frontMax;
        }

        private void RaiseHardStop(string reason)
        {
            DateTime now = DateTime.UtcNow;
            bool firstRaise = Interlocked.Exchange(ref _hardStopRaised, 1) == 0;

            // 현재 기준: 위험 상태가 남아 있으면 알람 리셋 후에도 주기적으로 EStop을 재실행한다.
            if (firstRaise || (now - _lastHardStopStopUtc).TotalMilliseconds >= HardStopRepeatStopMs)
            {
                _lastHardStopStopUtc = now;
                StopAllForCollision();
            }

            if (!firstRaise && (now - _lastHardStopAlarmUtc).TotalMilliseconds < HardStopRepeatAlarmMs)
                return;

            _lastHardStopAlarmUtc = now;

            QMC.Common.Log.Write("Main", "INTERLOCK", "RealtimeCollisionSupervisor", reason + " - Blocked");
            AlarmManager.Raise(
                AlarmSeverity.Critical,
                "PICKER-FACING-X-INTERLOCK",
                "RealtimeCollisionSupervisor",
                reason);
        }

        // 사용자 정책: 전축 하드정지. 핸들러가 등록되어 있으면 전체 축을 즉시 EStop, 없으면 Front/Rear Picker X/Y만 정지(안전 폴백).
        private void StopAllForCollision()
        {
            Action handler;
            lock (_sync)
                handler = _stopAllAxesHandler;

            if (handler != null)
            {
                try
                {
                    handler();
                    return;
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "INTERLOCK", "RealtimeCollisionSupervisor",
                        "전축 하드정지 핸들러 실행 중 예외가 발생해 Picker X/Y 폴백 정지를 수행합니다. error=" + ex.Message + " - Failed");
                }
            }

            StopFrontRearPickerXY();
        }

        private void StopFrontRearPickerXY()
        {
            TryEmergencyStop(ResolvePickerX(true));
            TryEmergencyStop(ResolvePickerY(true));
            TryEmergencyStop(ResolvePickerX(false));
            TryEmergencyStop(ResolvePickerY(false));
        }

        private static void TryEmergencyStop(BaseAxis axis)
        {
            try
            {
                if (axis != null)
                    axis.EStop();
            }
            catch
            {
            }
        }

        private PickerSafetySnapshot BuildPickerSnapshot(PickerSafetySide side, PickerSafetyPhase phase, bool carrying)
        {
            bool isFront = side == PickerSafetySide.Front;
            BaseAxis x = ResolvePickerX(isFront);
            BaseAxis y = ResolvePickerY(isFront);
            TryUpdateAxis(x);
            TryUpdateAxis(y);

            return new PickerSafetySnapshot
            {
                Side = side,
                Phase = phase,
                Carrying = carrying,
                ForwardYPermitted = IsForwardYPermitted(phase, carrying),
                XActual = x != null ? x.ActualPosition : 0.0,
                XCommand = x != null ? x.CommandPosition : 0.0,
                XMoving = x != null && x.IsMoving,
                YActual = y != null ? y.ActualPosition : 0.0,
                YCommand = y != null ? y.CommandPosition : 0.0,
                YMoving = y != null && y.IsMoving,
                YState = ResolveYState(isFront, y)
            };
        }

        private PickerSafetyYState ResolveYState(bool isFront, BaseAxis y)
        {
            try
            {
                if (y == null)
                    return PickerSafetyYState.Unknown;

                if (y.IsMoving)
                    return PickerSafetyYState.Moving;

                if (MotionGuardRuleHelpers.IsPickerYAtExactTeachingAvoid(
                    _machine,
                    isFront,
                    y.ActualPosition))
                    return PickerSafetyYState.Retracted;

                return PickerSafetyYState.Forward;
            }
            catch
            {
                return PickerSafetyYState.Unknown;
            }
        }

        private bool IsPickerYSafeByPosition(bool isFront, double position, double tolerance)
        {
            // 현재 기준: X 안전거리 안에서 PickerY 안전 위치는 Home(0) 또는 실제 AvoidPosition만 인정한다.
            if (Math.Abs(position) <= tolerance)
                return true;

            if (IsNearPickerYTeachingPosition(isFront, "AvoidPosition", position, tolerance))
                return true;

            // 기존 조건: InputAvoidPosition/OutputAvoidPosition도 실시간 감시 안전 위치로 보았다.
            // 현재 필요 여부: 사용 안 함. Input/OutputSideAvoid는 작업존으로 보고 실제 Avoid/Home만 안전 위치로 인정한다.
            //return IsNearPickerYTeachingPosition(isFront, "InputAvoidPosition", position, tolerance) ||
            //       IsNearPickerYTeachingPosition(isFront, "OutputAvoidPosition", position, tolerance);

            return false;
        }

        private bool IsNearPickerYTeachingPosition(bool isFront, string positionName, double position, double tolerance)
        {
            try
            {
                double target = isFront
                    ? _machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerY, positionName)
                    : _machine.PickerRearUnit.GetPickerTeachingPosition(PickerAxis.PickerY, positionName);
                return Math.Abs(position - target) <= tolerance;
            }
            catch
            {
                return false;
            }
        }

        private BaseAxis ResolvePickerX(bool isFront)
        {
            return isFront
                ? (_machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null)
                : (_machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null);
        }

        private BaseAxis ResolvePickerY(bool isFront)
        {
            return isFront
                ? (_machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerY : null)
                : (_machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerY : null);
        }

        private double ResolvePickerYFacingClearance()
        {
            double front = 0.0;
            double rear = 0.0;
            try
            {
                if (_machine.PickerFrontUnit != null && _machine.PickerFrontUnit.Setup != null)
                    front = _machine.PickerFrontUnit.Setup.PickerYFacingXClearance;
                if (_machine.PickerRearUnit != null && _machine.PickerRearUnit.Setup != null)
                    rear = _machine.PickerRearUnit.Setup.PickerYFacingXClearance;
            }
            catch
            {
            }

            double resolved = Math.Max(front, rear);
            return resolved > 0.0 ? resolved : 150.0;
        }

        private double ResolvePickerYOutDistance(bool isFront)
        {
            try
            {
                double value = isFront
                    ? (_machine.PickerFrontUnit != null && _machine.PickerFrontUnit.Setup != null
                        ? _machine.PickerFrontUnit.Setup.PickerYOutDistance
                        : 0.0)
                    : (_machine.PickerRearUnit != null && _machine.PickerRearUnit.Setup != null
                        ? _machine.PickerRearUnit.Setup.PickerYOutDistance
                        : 0.0);
                return value > 0.0 ? value : 1.0;
            }
            catch
            {
                return 1.0;
            }
        }

        private static bool IsForwardYPermitted(PickerSafetyPhase phase, bool carrying)
        {
            if (!carrying && phase != PickerSafetyPhase.PickUp)
                return false;

            return phase == PickerSafetyPhase.PickUp ||
                   phase == PickerSafetyPhase.BottomInspect ||
                   phase == PickerSafetyPhase.SideInspect ||
                   phase == PickerSafetyPhase.Place;
        }

        private static void TryUpdateAxis(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return;

                // 시뮬 모드에서는 UpdateStatus가 SimulateMotion으로 ActualPosition을 전진시키는 부작용이 있다.
                // 감시 루프(10ms)가 이를 호출하면 각 축의 이동 루프와 ActualPosition을 두고 read-modify-write 레이스가 나서
                // 이동 진행분이 덮여 오토 모션이 느려지고 끊긴다. 시뮬에서는 이동 루프가 이미 위치를 갱신하므로
                // 감시자는 값을 읽기만 하고 UpdateStatus는 호출하지 않는다.
                // 실장비에서는 최신 엔코더 위치가 필요하므로 UpdateStatus를 호출한다.
                if (axis.Config != null && axis.Config.IsSimulationMode)
                    return;

                axis.UpdateStatus();
            }
            catch
            {
            }
        }

        private void WriteThrottledRiskLog(string message)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastRiskLogUtc).TotalMilliseconds < RiskLogThrottleMs)
                return;

            _lastRiskLogUtc = now;
            QMC.Common.Log.Write("Main", "SYSTEM", "RealtimeCollisionSupervisor", message + " - Check");
        }

        private void WriteThrottledStateLog(string message)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastStateLogUtc).TotalMilliseconds < StateLogThrottleMs)
                return;

            _lastStateLogUtc = now;
            QMC.Common.Log.Write("Main", "SYSTEM", "RealtimeCollisionSupervisor", message + " - Ok");
        }
    }
}
