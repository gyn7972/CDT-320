using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Add(list, SharedRailXAxis.WaferVisionX, _machine.InputStage != null ? _machine.InputStage.CameraX : null);
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
            railAxis = SharedRailXAxis.WaferVisionX;
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

            SharedRailXValidationResult validation = _validator.Validate(GetAxisSettings(), plan);
            if (!validation.Allowed)
                return RaiseBlocked(validation.Reason);

            List<SharedRailXTarget> targets = plan.Targets.ToList();
            if (targets.Count == 0)
                return RaiseBlocked("SharedRailX move target is empty.");

            return await MoveSoftwareParallelAsync(plan, targets);
        }

        public Task<int> MoveAsync(SharedRailXAxis axis, double targetPosition, double velocity)
        {
            var plan = new SharedRailXMovePlan { Velocity = velocity };
            plan.Add(axis, targetPosition);
            return MoveAsync(plan);
        }

        private async Task<int> MoveSoftwareParallelAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets)
        {
            var tasks = new List<Task<int>>();
            foreach (SharedRailXTarget target in targets)
            {
                SharedRailXAxisSetting setting = GetAxisSettings().FirstOrDefault(x => x.RailAxis == target.Axis);
                if (setting == null || setting.Axis == null)
                    return RaiseBlocked("SharedRailX target axis is not mapped. axis=" + target.Axis);

                double velocity = target.Velocity.HasValue ? target.Velocity.Value : plan.Velocity;
                tasks.Add(setting.Axis.MoveAbsoluteAsync(target.TargetPosition, velocity));
            }

            int[] results = await Task.WhenAll(tasks);
            int fail = results.FirstOrDefault(x => x != 0);
            return fail;
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
