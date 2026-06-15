using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Interlocks;
using QMC.Common.Alarms;
using QMC.Common.Motion.Ajin;
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

            return await DispatchMoveAsync(plan, targets, settings);
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
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveWaferVisionAndRearPickerAsync(
            double waferVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("InputVisionX+RearPickerX", velocity)
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveBinVisionAndFrontPickerAsync(
            double binVisionTarget,
            double frontPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+FrontPickerX", velocity)
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveBinVisionAndRearPickerAsync(
            double binVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+RearPickerX", velocity)
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveFrontAndRearPickerAsync(
            double frontPickerTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("FrontPickerX+RearPickerX", velocity)
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
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
                .UseMode(SharedRailXMoveMode.AjinMultiPosition)
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
            switch (plan.Mode)
            {
                // Ajin 동기 다축 이동 모드
                case SharedRailXMoveMode.AjinMultiPosition:
                    return MoveAjinMultiPositionOrFallbackAsync(plan, targets, settings);
                // 소프트웨어 병렬 이동 모드
                case SharedRailXMoveMode.SoftwareParallel:
                default:
                    return MoveSoftwareParallelAsync(plan, targets, settings);
            }
        }

        private Task<int> MoveAjinMultiPositionOrFallbackAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            if (targets.Count < 2)
                return MoveSoftwareParallelAsync(plan, targets, settings);

            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap =
                settings.ToDictionary(x => x.RailAxis);
            var ajinAxes = new List<AjinAxis>();
            var positions = new List<double>();
            var velocities = new List<double>();
            var accelerations = new List<double>();
            var decelerations = new List<double>();

            foreach (SharedRailXTarget target in targets)
            {
                SharedRailXAxisSetting setting;
                settingMap.TryGetValue(target.Axis, out setting);
                AjinAxis ajinAxis = setting != null ? setting.Axis as AjinAxis : null;
                if (ajinAxis == null || !ajinAxis.CanUseSynchronizedAbsoluteMove)
                    return MoveSoftwareParallelAsync(plan, targets, settings);

                double velocity = target.Velocity.HasValue ? target.Velocity.Value : plan.Velocity;
                double resolvedVelocity;
                int ready = ajinAxis.ValidateSynchronizedAbsoluteMove(target.TargetPosition, velocity, out resolvedVelocity);
                if (ready != 0)
                    return Task.FromResult(ready);

                ajinAxes.Add(ajinAxis);
                positions.Add(target.TargetPosition);
                velocities.Add(resolvedVelocity);
                accelerations.Add(target.Acceleration.HasValue ? target.Acceleration.Value : ajinAxis.Config.Acceleration);
                decelerations.Add(target.Deceleration.HasValue ? target.Deceleration.Value : ajinAxis.Config.Deceleration);
            }

            return MoveAjinMultiPositionAsync(ajinAxes, positions, velocities, accelerations, decelerations);
        }

        private async Task<int> MoveAjinMultiPositionAsync(
            IReadOnlyList<AjinAxis> ajinAxes,
            IReadOnlyList<double> positions,
            IReadOnlyList<double> velocities,
            IReadOnlyList<double> accelerations,
            IReadOnlyList<double> decelerations)
        {
            int[] axisNos = ajinAxes.Select(x => x.AxisNo).ToArray();
            double[] targetPositions = ajinAxes
                .Select((axis, index) => axis.ToBoardPosition(positions[index]))
                .ToArray();
            double[] targetVelocities = ajinAxes
                .Select((axis, index) => axis.ToBoardVelocity(velocities[index]))
                .ToArray();
            double[] targetAccelerations = ajinAxes
                .Select((axis, index) => axis.ToBoardAcceleration(accelerations[index]))
                .ToArray();
            double[] targetDecelerations = ajinAxes
                .Select((axis, index) => axis.ToBoardAcceleration(decelerations[index]))
                .ToArray();

            using (SharedRailXMotionRuntime.EnterInternalDispatch())
            {
                foreach (AjinAxis axis in ajinAxes)
                {
                    int modeRet = AXM.SetAbsRelMode(axis.AxisNo, true);
                    if (modeRet != 0)
                    {
                        foreach (AjinAxis failAxis in ajinAxes)
                            failAxis.FailSynchronizedAbsoluteMove(modeRet);
                        return modeRet;
                    }
                }

                int ret = AXM.MoveMultiplePosition(
                    axisNos,
                    targetPositions,
                    targetVelocities,
                    targetAccelerations,
                    targetDecelerations);

                if (ret != 0)
                {
                    foreach (AjinAxis axis in ajinAxes)
                        axis.FailSynchronizedAbsoluteMove(ret);
                    return ret;
                }

                for (int i = 0; i < ajinAxes.Count; i++)
                    ajinAxes[i].BeginSynchronizedAbsoluteMove(positions[i], velocities[i]);

                int[] waitResults = await Task.WhenAll(ajinAxes.Select(x => x.WaitSynchronizedMoveDoneAsync()));
                return waitResults.FirstOrDefault(x => x != 0);
            }
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

            SharedRailXAxisGeometry geometry;
            if (!_config.Geometry.TryGetValue(railAxis, out geometry) || geometry == null)
                geometry = new SharedRailXAxisGeometry();

            list.Add(new SharedRailXAxisSetting(railAxis, axis)
            {
                BodyOffsetMin = geometry.BodyOffsetMin,
                BodyOffsetMax = geometry.BodyOffsetMax,
                RailOriginOffset = geometry.RailOriginOffset,
                PositionScale = geometry.PositionScale,
                SafetyDistance = geometry.SafetyDistance.HasValue
                    ? geometry.SafetyDistance.Value
                    : _config.DefaultSafetyDistance
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

            if (movingRailAxis == SharedRailXAxis.InputVisionX && direction < 0)
                return SharedRailXValidationResult.Allow();

            double movingMin = moving.GetMinAt(moving.Axis.ActualPosition);
            double movingMax = moving.GetMaxAt(moving.Axis.ActualPosition);
            double railDirection = (direction < 0 ? -1.0 : 1.0) * moving.PositionScale;

            foreach (SharedRailXAxisSetting other in settings)
            {
                if (other == null || other.Axis == null || other.RailAxis == movingRailAxis)
                    continue;
                if (!_config.IsCollisionPairEnabled(movingRailAxis, other.RailAxis))
                    continue;

                SharedRailXAxisPair pair;
                if (_config.TryGetCollisionPair(movingRailAxis, other.RailAxis, out pair) && pair.HasClearanceRule)
                {
                    SharedRailXValidationResult clearance = ValidateJogPairClearance(
                        moving,
                        other,
                        pair,
                        direction,
                        stopAtLimit);
                    if (!clearance.Allowed)
                        return clearance;
                    continue;
                }

                double otherMin = other.GetMinAt(other.Axis.ActualPosition);
                double otherMax = other.GetMaxAt(other.Axis.ActualPosition);
                double required = Math.Max(moving.SafetyDistance, other.SafetyDistance);
                double gap = CalculateCurrentGap(movingMin, movingMax, otherMin, otherMax);
                bool movingTowardOther = IsMovingTowardOther(
                    movingMin,
                    movingMax,
                    otherMin,
                    otherMax,
                    railDirection);

                if ((gap < required && (stopAtLimit || movingTowardOther)) ||
                    (stopAtLimit && movingTowardOther && gap <= required))
                {
                    return SharedRailXValidationResult.Block(
                        "SharedRailX jog distance is too close. " +
                        moving.Axis.Name + " rail=" + moving.ToRailPosition(moving.Axis.ActualPosition).ToString("F3") +
                        ", " + other.Axis.Name + " rail=" + other.ToRailPosition(other.Axis.ActualPosition).ToString("F3") +
                        ", direction=" + (direction < 0 ? "-" : "+") +
                        ", gap=" + gap.ToString("F3") +
                        ", required=" + required.ToString("F3"));
                }
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

        private static bool IsMovingTowardOther(
            double movingMin,
            double movingMax,
            double otherMin,
            double otherMax,
            double railDirection)
        {
            if (railDirection == 0.0)
                return true;

            if (movingMax <= otherMin)
                return railDirection > 0.0;
            if (otherMax <= movingMin)
                return railDirection < 0.0;

            double movingCenter = (movingMin + movingMax) / 2.0;
            double otherCenter = (otherMin + otherMax) / 2.0;
            if (otherCenter > movingCenter)
                return railDirection > 0.0;
            if (otherCenter < movingCenter)
                return railDirection < 0.0;

            return false;
        }

        private static double CalculateCurrentGap(double aMin, double aMax, double bMin, double bMax)
        {
            if (aMax <= bMin)
                return bMin - aMax;
            if (bMax <= aMin)
                return aMin - bMax;
            return 0.0;
        }

        private static int RaiseBlocked(string reason)
        {
            AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X", "SharedRailX", reason);
            return -11;
        }
    }
}
