using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXMotionService
    {
        private readonly CDT320_Machine _machine;
        private readonly SharedRailXConfig _config;
        private readonly SharedRailXCollisionValidator _validator;

        public SharedRailXMotionService(CDT320_Machine machine)
            : this(machine, SharedRailXConfig.CreateDefault())
        {
        }

        public SharedRailXMotionService(CDT320_Machine machine, SharedRailXConfig config)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _config = config ?? SharedRailXConfig.CreateDefault();
            _validator = new SharedRailXCollisionValidator(_config);
        }

        public SharedRailXConfig Config { get { return _config; } }

        public IReadOnlyList<SharedRailXAxisSetting> GetAxisSettings()
        {
            var list = new List<SharedRailXAxisSetting>();
            Add(list, SharedRailXAxis.InputVisionX, _machine.InputStageUnit != null ? _machine.InputStageUnit.CameraX : null);
            Add(list, SharedRailXAxis.FrontPickerX, _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null);
            Add(list, SharedRailXAxis.RearPickerX, _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null);
            Add(list, SharedRailXAxis.OutputVisionX, _machine.OutputStageUnit != null ? _machine.OutputStageUnit.OutputCameraX : null);
            return list;
        }

        public bool IsSharedRailAxis(BaseAxis axis)
        {
            return TryResolve(axis, out _);
        }

        public bool TryResolve(BaseAxis axis, out SharedRailXAxis railAxis)
        {
            railAxis = SharedRailXAxis.InputVisionX;
            if (axis == null)
                return false;

            foreach (SharedRailXAxisSetting setting in GetAxisSettings())
            {
                if (ReferenceEquals(setting.Axis, axis))
                {
                    railAxis = setting.RailAxis;
                    return true;
                }
            }

            return false;
        }

        public bool VerifySingleAxisMove(BaseAxis axis, double targetPosition, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            var plan = new SharedRailXMovePlan
            {
                Name = "SingleAxisGuard",
                Velocity = axis.Config != null ? axis.Config.DefaultVelocity : 0.0
            };
            plan.Add(railAxis, targetPosition);
            SharedRailXValidationResult result = _validator.Validate(GetAxisSettings(), plan);
            reason = result.Reason;
            return result.Allowed;
        }

        public bool VerifyJogMove(BaseAxis axis, int direction, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            SharedRailXValidationResult result = ValidateJogCurrentDistance(GetAxisSettings(), railAxis, direction, false);
            reason = result.Reason;
            return result.Allowed;
        }

        public bool VerifyJogCurrentDistance(BaseAxis axis, int direction, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            SharedRailXValidationResult result = ValidateJogCurrentDistance(GetAxisSettings(), railAxis, direction, true);
            reason = result.Reason;
            return result.Allowed;
        }

        public async Task<int> MoveAsync(SharedRailXMovePlan plan)
        {
            if (plan == null)
                return RaiseBlocked("SharedRailX move plan is null.");

            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            SharedRailXValidationResult motionGuard = VerifyMotionGuardTargets(settings, plan);
            if (!motionGuard.Allowed)
                return RaiseBlocked(motionGuard.Reason);

            SharedRailXValidationResult validation = _validator.Validate(settings, plan);
            if (!validation.Allowed)
                return RaiseBlocked(validation.Reason);

            List<SharedRailXTarget> targets = plan.Targets.ToList();
            if (targets.Count == 0)
                return RaiseBlocked("SharedRailX move target is empty.");

            SharedRailXAutoMoveGuard moveGuard = StartAutoMoveGuard(plan, targets, settings);
            try
            {
                int result = await DispatchMoveAsync(plan, targets, settings).ConfigureAwait(false);
                if (moveGuard != null && moveGuard.Blocked)
                    return -11;

                return result;
            }
            finally
            {
                if (moveGuard != null)
                    moveGuard.Dispose();
            }
        }

        private static SharedRailXValidationResult VerifyMotionGuardTargets(
            IReadOnlyList<SharedRailXAxisSetting> settings,
            SharedRailXMovePlan plan)
        {
            if (settings == null)
                return SharedRailXValidationResult.Block("SharedRailX settings are empty.");
            if (plan == null || plan.Targets == null)
                return SharedRailXValidationResult.Block("SharedRailX move plan is null.");

            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap =
                settings.Where(x => x != null && x.Axis != null).ToDictionary(x => x.RailAxis);

            foreach (SharedRailXTarget target in plan.Targets)
            {
                SharedRailXAxisSetting setting;
                if (!settingMap.TryGetValue(target.Axis, out setting) || setting == null || setting.Axis == null)
                    return SharedRailXValidationResult.Block("SharedRailX target axis is not mapped. axis=" + target.Axis);

                string reason;
                if (!MotionGuardRuntime.VerifyAxisMoveWithoutSharedRailX(setting.Axis, target.TargetPosition, out reason))
                    return SharedRailXValidationResult.Block(reason);
            }

            return SharedRailXValidationResult.Allow();
        }

        public Task<int> MoveAsync(SharedRailXAxis axis, double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create(axis.ToString(), velocity)
                .Add(axis, targetPosition));
        }

        public Task<int> MoveInputVisionAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.InputVisionX, targetPosition, velocity);
        }

        public Task<int> MoveOutputVisionAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.OutputVisionX, targetPosition, velocity);
        }

        public Task<int> MoveFrontPickerAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.FrontPickerX, targetPosition, velocity);
        }

        public Task<int> MoveRearPickerAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.RearPickerX, targetPosition, velocity);
        }

        public Task<int> MoveWaferVisionAndFrontPickerAsync(
            double waferVisionTarget,
            double frontPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("InputVisionX+FrontPickerX", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveWaferVisionAndRearPickerAsync(
            double waferVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("InputVisionX+RearPickerX", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveBinVisionAndFrontPickerAsync(
            double binVisionTarget,
            double frontPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+FrontPickerX", velocity)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveBinVisionAndRearPickerAsync(
            double binVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+RearPickerX", velocity)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveFrontAndRearPickerAsync(
            double frontPickerTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("FrontPickerX+RearPickerX", velocity)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveAllAsync(
            double waferVisionTarget,
            double binVisionTarget,
            double frontPickerTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("SharedRailX-All", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        private Task<int> DispatchMoveAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            // SharedRailX is intentionally commanded by individual absolute moves.
            // AXM.MoveMultiplePosition is not used until its equipment behavior is fully verified.
            return MoveSoftwareParallelAsync(plan, targets, settings);
        }

        private async Task<int> MoveSoftwareParallelAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            var tasks = new List<Task<int>>();
            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap =
                settings.ToDictionary(x => x.RailAxis);

            using (SharedRailXMotionRuntime.EnterInternalDispatch())
            {
                foreach (SharedRailXTarget target in targets)
                {
                    SharedRailXAxisSetting setting;
                    settingMap.TryGetValue(target.Axis, out setting);
                    if (setting == null || setting.Axis == null)
                        return RaiseBlocked("SharedRailX target axis is not mapped. axis=" + target.Axis);

                    double velocity = target.Velocity.HasValue ? target.Velocity.Value : plan.Velocity;
                    tasks.Add(setting.Axis.MoveAbsoluteAsync(target.TargetPosition, velocity));
                }

                int[] results = await Task.WhenAll(tasks);
                int fail = results.FirstOrDefault(x => x != 0);
                return fail;
            }
        }

        private void Add(List<SharedRailXAxisSetting> list, SharedRailXAxis railAxis, BaseAxis axis)
        {
            if (axis == null)
                return;

            list.Add(new SharedRailXAxisSetting(railAxis, axis)
            {
                SafetyDistance = _config.DefaultSafetyDistance
            });
        }

        private SharedRailXValidationResult ValidateJogCurrentDistance(
            IReadOnlyList<SharedRailXAxisSetting> settings,
            SharedRailXAxis movingRailAxis,
            int direction,
            bool stopAtLimit)
        {
            if (settings == null)
                return SharedRailXValidationResult.Block("SharedRailX settings are empty.");

            SharedRailXAxisSetting moving = settings.FirstOrDefault(x => x != null && x.Axis != null && x.RailAxis == movingRailAxis);
            if (moving == null)
                return SharedRailXValidationResult.Block("SharedRailX jog axis is not mapped. axis=" + movingRailAxis);

            foreach (SharedRailXAxisSetting other in settings)
            {
                if (other == null || other.Axis == null || other.RailAxis == movingRailAxis)
                    continue;
                if (!_config.IsCollisionPairEnabled(movingRailAxis, other.RailAxis))
                    continue;

                SharedRailXAxisPair pair;
                if (!_config.TryGetCollisionPair(movingRailAxis, other.RailAxis, out pair))
                    continue;

                if (!pair.HasClearanceRule)
                {
                    return SharedRailXValidationResult.Block(
                        "SharedRailX jog pair clearance rule is not configured. pair=" +
                        movingRailAxis + "<->" + other.RailAxis);
                }

                SharedRailXValidationResult clearance = ValidateJogPairClearance(
                    moving,
                    other,
                    pair,
                    direction,
                    stopAtLimit);
                if (!clearance.Allowed)
                    return clearance;
            }

            return SharedRailXValidationResult.Allow();
        }

        private static SharedRailXValidationResult ValidateJogPairClearance(
            SharedRailXAxisSetting moving,
            SharedRailXAxisSetting other,
            SharedRailXAxisPair pair,
            int direction,
            bool stopAtLimit)
        {
            int movingSign;
            int otherSign;
            ResolvePairSigns(moving.RailAxis, other.RailAxis, pair, out movingSign, out otherSign);

            double required = pair.SafetyDistance.HasValue
                ? pair.SafetyDistance.Value
                : Math.Max(moving.SafetyDistance, other.SafetyDistance);
            double clearance = CalculatePairClearance(
                pair.HomeClearance,
                movingSign,
                moving.Axis.ActualPosition,
                otherSign,
                other.Axis.ActualPosition);
            bool movingTowardOther = movingSign * direction > 0;

            if (movingTowardOther && clearance <= required)
            {
                return SharedRailXValidationResult.Block(
                    "SharedRailX jog pair clearance is too close. " +
                    moving.Axis.Name + " axis=" + moving.Axis.ActualPosition.ToString("F3") +
                    ", " + other.Axis.Name + " axis=" + other.Axis.ActualPosition.ToString("F3") +
                    ", direction=" + (direction < 0 ? "-" : "+") +
                    ", clearance=" + clearance.ToString("F3") +
                    ", required=" + required.ToString("F3") +
                    ", homeClearance=" + pair.HomeClearance.ToString("F3") +
                    ", movingToward=" + (movingTowardOther ? "Y" : "N") +
                    ", stopAtLimit=" + (stopAtLimit ? "Y" : "N"));
            }

            return SharedRailXValidationResult.Allow();
        }

        private static void ResolvePairSigns(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            SharedRailXAxisPair pair,
            out int signA,
            out int signB)
        {
            if (pair.AxisA == axisA && pair.AxisB == axisB)
            {
                signA = pair.AxisATowardSign;
                signB = pair.AxisBTowardSign;
                return;
            }

            signA = pair.AxisBTowardSign;
            signB = pair.AxisATowardSign;
        }

        private static double CalculatePairClearance(
            double homeClearance,
            int aTowardSign,
            double aPosition,
            int bTowardSign,
            double bPosition)
        {
            return homeClearance - (aTowardSign * aPosition) - (bTowardSign * bPosition);
        }

        private SharedRailXAutoMoveGuard StartAutoMoveGuard(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            if (plan == null || targets == null || settings == null)
                return null;

            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap = settings
                .Where(x => x != null && x.Axis != null)
                .ToDictionary(x => x.RailAxis);
            Dictionary<SharedRailXAxis, double> targetMap = targets
                .GroupBy(x => x.Axis)
                .ToDictionary(x => x.Key, x => x.Last().TargetPosition);

            var guardPairs = new List<SharedRailXGuardPair>();
            foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
            {
                if (!targetMap.ContainsKey(pair.AxisA) && !targetMap.ContainsKey(pair.AxisB))
                    continue;
                if (!pair.HasClearanceRule)
                    continue;

                SharedRailXAxisSetting settingA;
                SharedRailXAxisSetting settingB;
                if (!settingMap.TryGetValue(pair.AxisA, out settingA) ||
                    !settingMap.TryGetValue(pair.AxisB, out settingB))
                {
                    continue;
                }

                double targetA = targetMap.ContainsKey(pair.AxisA)
                    ? targetMap[pair.AxisA]
                    : settingA.Axis.ActualPosition;
                double targetB = targetMap.ContainsKey(pair.AxisB)
                    ? targetMap[pair.AxisB]
                    : settingB.Axis.ActualPosition;
                double initialClearance = CalculatePairClearance(
                    pair.HomeClearance,
                    pair.AxisATowardSign,
                    settingA.Axis.ActualPosition,
                    pair.AxisBTowardSign,
                    settingB.Axis.ActualPosition);
                double targetClearance = CalculatePairClearance(
                    pair.HomeClearance,
                    pair.AxisATowardSign,
                    targetA,
                    pair.AxisBTowardSign,
                    targetB);
                double required = pair.SafetyDistance.HasValue
                    ? pair.SafetyDistance.Value
                    : Math.Max(settingA.SafetyDistance, settingB.SafetyDistance);

                guardPairs.Add(new SharedRailXGuardPair(
                    pair,
                    settingA,
                    settingB,
                    required,
                    initialClearance,
                    targetClearance));
            }

            if (guardPairs.Count == 0)
                return null;

            return new SharedRailXAutoMoveGuard(plan.Name, settings, guardPairs);
        }

        private static int RaiseBlocked(string reason)
        {
            AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X", "SharedRailX", reason);
            return -11;
        }

        private sealed class SharedRailXGuardPair
        {
            private const double Epsilon = 0.001;

            public readonly SharedRailXAxisPair Pair;
            public readonly SharedRailXAxisSetting SettingA;
            public readonly SharedRailXAxisSetting SettingB;
            public readonly double RequiredClearance;
            public readonly double InitialClearance;
            public readonly double TargetClearance;
            private double _previousClearance;

            public SharedRailXGuardPair(
                SharedRailXAxisPair pair,
                SharedRailXAxisSetting settingA,
                SharedRailXAxisSetting settingB,
                double requiredClearance,
                double initialClearance,
                double targetClearance)
            {
                Pair = pair;
                SettingA = settingA;
                SettingB = settingB;
                RequiredClearance = requiredClearance;
                InitialClearance = initialClearance;
                TargetClearance = targetClearance;
                _previousClearance = initialClearance;
            }

            public bool IsUnsafe(out string reason)
            {
                reason = string.Empty;
                double clearance = CalculatePairClearance(
                    Pair.HomeClearance,
                    Pair.AxisATowardSign,
                    SettingA.Axis.ActualPosition,
                    Pair.AxisBTowardSign,
                    SettingB.Axis.ActualPosition);

                bool startedUnsafe = InitialClearance <= RequiredClearance;
                bool targetMovesAway = TargetClearance > InitialClearance + Epsilon;
                bool clearanceImproving = clearance + Epsilon >= _previousClearance;
                _previousClearance = clearance;

                if (clearance > RequiredClearance)
                    return false;

                if (startedUnsafe && targetMovesAway && clearanceImproving)
                    return false;

                reason =
                    "SharedRailX real-time clearance guard stopped motion. pair=" +
                    Pair.AxisA + "<->" + Pair.AxisB +
                    ", " + SettingA.Axis.Name + "=" + SettingA.Axis.ActualPosition.ToString("F3") +
                    ", " + SettingB.Axis.Name + "=" + SettingB.Axis.ActualPosition.ToString("F3") +
                    ", clearance=" + clearance.ToString("F3") +
                    ", required=" + RequiredClearance.ToString("F3") +
                    ", initialClearance=" + InitialClearance.ToString("F3") +
                    ", targetClearance=" + TargetClearance.ToString("F3") +
                    ", homeClearance=" + Pair.HomeClearance.ToString("F3") +
                    ", signs=" + Pair.AxisA + ":" + Pair.AxisATowardSign + "," +
                    Pair.AxisB + ":" + Pair.AxisBTowardSign;
                return true;
            }
        }

        private sealed class SharedRailXAutoMoveGuard : IDisposable
        {
            private readonly string _planName;
            private readonly IReadOnlyList<SharedRailXAxisSetting> _settings;
            private readonly IReadOnlyList<SharedRailXGuardPair> _pairs;
            private readonly CancellationTokenSource _cts;
            private readonly CancellationToken _token;
            private readonly Task _task;
            private int _blocked;
            private int _disposed;

            public SharedRailXAutoMoveGuard(
                string planName,
                IReadOnlyList<SharedRailXAxisSetting> settings,
                IReadOnlyList<SharedRailXGuardPair> pairs)
            {
                _planName = string.IsNullOrWhiteSpace(planName) ? "SharedRailX" : planName;
                _settings = settings;
                _pairs = pairs;
                _cts = new CancellationTokenSource();
                _token = _cts.Token;
                _task = Task.Run(() => MonitorAsync(_token), _token);
            }

            public bool Blocked
            {
                get { return _blocked != 0; }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "SHARED-RAIL-X-MOVE-GUARD",
                        "SharedRailX",
                        "SharedRailX 실시간 감시 취소 중 예외가 발생했습니다. plan=" +
                        _planName + ", message=" + ex.Message);
                }

                Task task = _task;
                if (task == null)
                {
                    DisposeCancellationTokenSource();
                    return;
                }

                task.ContinueWith(
                    t =>
                    {
                        try
                        {
                            if (t.IsFaulted && t.Exception != null)
                                t.Exception.Handle(_ => true);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            DisposeCancellationTokenSource();
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            private void DisposeCancellationTokenSource()
            {
                try
                {
                    _cts.Dispose();
                }
                catch
                {
                }
            }

            private async Task MonitorAsync(CancellationToken ct)
            {
                bool movementSeen = false;

                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(20, ct).ConfigureAwait(false);

                        bool anyMoving = _settings.Any(x => x != null && x.Axis != null && x.Axis.IsMoving);
                        if (!anyMoving)
                        {
                            if (movementSeen)
                                break;
                            continue;
                        }

                        movementSeen = true;
                        foreach (SharedRailXGuardPair pair in _pairs)
                        {
                            string reason;
                            if (pair.IsUnsafe(out reason))
                            {
                                Interlocked.Exchange(ref _blocked, 1);
                                StopSharedRailAxes();
                                AlarmManager.Raise(
                                    AlarmSeverity.Warning,
                                    "SHARED-RAIL-X-MOVE-GUARD",
                                    "SharedRailX",
                                    "plan=" + _planName + ". " + reason);
                                return;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "SHARED-RAIL-X-MOVE-GUARD",
                        "SharedRailX",
                        "SharedRailX real-time guard exception. plan=" + _planName + ", message=" + ex.Message);
                }
            }

            private void StopSharedRailAxes()
            {
                using (SharedRailXMotionRuntime.EnterInternalDispatch())
                {
                    foreach (SharedRailXAxisSetting setting in _settings)
                    {
                        if (setting == null || setting.Axis == null)
                            continue;

                        try { setting.Axis.Stop(); } catch { }
                    }
                }
            }
        }
    }
}
