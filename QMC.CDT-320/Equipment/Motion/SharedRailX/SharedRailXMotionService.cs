using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
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
            Add(list, SharedRailXAxis.InputVisionX, _machine.InputStage != null ? _machine.InputStage.CameraX : null);
            Add(list, SharedRailXAxis.FrontPickerX, _machine.PickerFront != null ? _machine.PickerFront.PickerX : null);
            Add(list, SharedRailXAxis.RearPickerX, _machine.PickerRear != null ? _machine.PickerRear.PickerX : null);
            Add(list, SharedRailXAxis.OutputVisionX, _machine.OutputStage != null ? _machine.OutputStage.BinCameraX : null);
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

        public async Task<int> MoveAsync(SharedRailXMovePlan plan)
        {
            if (plan == null)
                return RaiseBlocked("SharedRailX move plan is null.");

            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            SharedRailXValidationResult validation = _validator.Validate(settings, plan);
            if (!validation.Allowed)
                return RaiseBlocked(validation.Reason);

            List<SharedRailXTarget> targets = plan.Targets.ToList();
            if (targets.Count == 0)
                return RaiseBlocked("SharedRailX move target is empty.");

            return await DispatchMoveAsync(plan, targets, settings);
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
                case SharedRailXMoveMode.AjinMultiPosition:
                    return MoveAjinMultiPositionOrFallbackAsync(plan, targets, settings);
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
            double[] targetPositions = positions.ToArray();
            double[] targetVelocities = velocities.ToArray();
            double[] targetAccelerations = accelerations.ToArray();
            double[] targetDecelerations = decelerations.ToArray();

            using (SharedRailXMotionRuntime.EnterInternalDispatch())
            {
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
                    ajinAxes[i].BeginSynchronizedAbsoluteMove(targetPositions[i], targetVelocities[i]);

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

        private static int RaiseBlocked(string reason)
        {
            AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X", "SharedRailX", reason);
            return -11;
        }
    }
}
