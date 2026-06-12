using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal abstract class PickerSequenceBase<TStep> where TStep : struct
    {
        protected PickerSequenceBase(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceKind kind,
            string name)
        {
            Context = context ?? throw new ArgumentNullException("context");
            Side = side;
            Kind = kind;
            Name = name ?? side.ToString();
        }

        protected MachineSequenceContext Context { get; private set; }
        protected PickerSequenceSide Side { get; private set; }
        protected PickerSequenceKind Kind { get; private set; }
        protected string Name { get; private set; }
        protected PickerSequenceOptions Options { get; private set; }
        protected TStep CurrentStep { get; set; }

        protected PickerFrontUnit FrontPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerFrontUnit : null; }
        }

        protected PickerRearUnit RearPicker
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.PickerRearUnit : null; }
        }

        protected MaterialLocationKind PickerLocationKind
        {
            get { return Side == PickerSequenceSide.Front ? MaterialLocationKind.PickerFront : MaterialLocationKind.PickerRear; }
        }

        public async Task<int> RunAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            Options = options ?? PickerSequenceOptions.Default();
            Options.RunMode = Options.RunMode;

            try
            {
                ct.ThrowIfCancellationRequested();
                WriteLog("RunAsync", Name + " sequence start. kind=" + Kind + " - Start");
                int result = await ExecuteAsync(ct).ConfigureAwait(false);
                if (result == 0)
                    WriteLog("RunAsync", Name + " sequence complete. kind=" + Kind + " - Ok");
                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", Name + " sequence canceled. kind=" + Kind + " - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-SEQ-EX", Name, Name + " sequence exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected abstract Task<int> ExecuteAsync(CancellationToken ct);

        protected int ResolveTimeout()
        {
            return Options != null && Options.MoveTimeoutMs > 0 ? Options.MoveTimeoutMs : 30000;
        }

        protected int ResolveResourceTimeout()
        {
            return Options != null && Options.ResourceTimeoutMs > 0 ? Options.ResourceTimeoutMs : 30000;
        }

        protected int ResolvePickerNo()
        {
            int pickerNo = Options != null && Options.PickerNo > 0 ? Options.PickerNo : 1;
            if (pickerNo < 1)
                pickerNo = 1;
            if (pickerNo > 4)
                pickerNo = 4;
            return pickerNo;
        }

        protected async Task<int> MovePickerAxisAndVerifyAsync(
            PickerAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MovePickerAxisCommandAsync(axis, target).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-MOVE-CMD", Name, description + " move command failed. result=" + result + ", " + BuildPickerAxisState(axis, target));

                bool done = await WaitPickerAxisMoveDoneAsync(axis, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!done)
                    return Fail("PICKER-MOVE-TIMEOUT", Name, description + " move done timeout. " + BuildPickerAxisState(axis, target));

                if (!IsPickerAxisInPosition(axis, target))
                    return Fail("PICKER-MOVE-CHECK", Name, description + " final position check failed. " + BuildPickerAxisState(axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-MOVE-EX", Name, description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerAxesAndVerifyAsync(
            IDictionary<PickerAxis, double> targets,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (targets == null || targets.Count == 0)
                    return 0;

                ct.ThrowIfCancellationRequested();

                var commandTasks = new List<Task<int>>();
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                    commandTasks.Add(MovePickerAxisCommandAsync(pair.Key, pair.Value));

                int[] commandResults = await Task.WhenAll(commandTasks).ConfigureAwait(false);
                int commandIndex = 0;
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (commandResults[commandIndex] != 0)
                        return Fail("PICKER-MOVE-CMD", Name, description + " move command failed. result=" + commandResults[commandIndex] + ", " + BuildPickerAxisState(pair.Key, pair.Value));
                    commandIndex++;
                }

                var waitTasks = new List<Task<bool>>();
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                    waitTasks.Add(WaitPickerAxisMoveDoneAsync(pair.Key, ResolveTimeout(), ct));

                bool[] waitResults = await Task.WhenAll(waitTasks).ConfigureAwait(false);
                int waitIndex = 0;
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (!waitResults[waitIndex])
                        return Fail("PICKER-MOVE-TIMEOUT", Name, description + " move done timeout. " + BuildPickerAxisState(pair.Key, pair.Value));
                    waitIndex++;
                }

                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (!IsPickerAxisInPosition(pair.Key, pair.Value))
                        return Fail("PICKER-MOVE-CHECK", Name, description + " final position check failed. " + BuildPickerAxisState(pair.Key, pair.Value));
                }

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-MOVE-EX", Name, description + " parallel move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerToDiePositionAndVerifyAsync(string positionArrayName, int pickerNo, string description, CancellationToken ct)
        {
            int index = pickerNo - 1;
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " pre Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            var xyTargets = new Dictionary<PickerAxis, double>();
            xyTargets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, positionArrayName + "[" + index + "]") + ResolvePickerAlignOffsetX(index);
            xyTargets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, positionArrayName + "[" + index + "]") + ResolvePickerAlignOffsetY(index);
            xyTargets[GetPickerTAxis(index)] = ResolveTPosition(positionArrayName) + ResolvePickerAlignOffsetT(index);

            result = await MovePickerAxesAndVerifyAsync(xyTargets, description + " XYT", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(index),
                ResolveZPosition(positionArrayName),
                description + " Z",
                ct).ConfigureAwait(false);
        }

        protected Task<int> MoveAllPickerZToAvoidAndVerifyAsync(string description, CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            return MovePickerAxesAndVerifyAsync(targets, description, ct);
        }

        protected Task<int> MovePickerGroupAndVerifyAsync(string positionName, string description, CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, positionName);
            targets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, positionName);
            targets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, positionName);
            targets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, positionName);
            targets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, positionName);
            targets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, positionName);
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, positionName);
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, positionName);
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, positionName);
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, positionName);
            return MovePickerAxesAndVerifyAsync(targets, description, ct);
        }

        protected void SetPickerVacuum(int pickerNo, bool on)
        {
            if (Side == PickerSequenceSide.Front)
                FrontPicker.SetPickerVacuum(pickerNo, on);
            else
                RearPicker.SetPickerVacuum(pickerNo, on);
        }

        protected async Task PickerBlowAsync(int pickerNo, CancellationToken ct)
        {
            int waitMs = 100;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Recipe != null)
                waitMs = FrontPicker.Recipe.BlowTimeMs;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Recipe != null)
                waitMs = RearPicker.Recipe.BlowTimeMs;

            if (Side == PickerSequenceSide.Front)
                await FrontPicker.PickerBlowOn(pickerNo, waitMs).ConfigureAwait(false);
            else
                await RearPicker.PickerBlowOn(pickerNo, waitMs).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
        }

        private Task<int> MovePickerAxisCommandAsync(PickerAxis axis, double target)
        {
            bool fine = Options != null && Options.FineMove;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxis(axis, target, fine);
            return RearPicker.MovePickerAxis(axis, target, fine);
        }

        private async Task<bool> WaitPickerAxisMoveDoneAsync(PickerAxis axis, int timeoutMs, CancellationToken ct)
        {
            Task<bool> waitTask = Side == PickerSequenceSide.Front
                ? FrontPicker.WaitPickerAxisMoveDone(axis, timeoutMs)
                : RearPicker.WaitPickerAxisMoveDone(axis, timeoutMs);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(waitTask, cancelTask).ConfigureAwait(false);
            if (completed == cancelTask)
                ct.ThrowIfCancellationRequested();
            return await waitTask.ConfigureAwait(false);
        }

        private bool IsPickerAxisInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double tolerance = item != null && item.Config != null ? item.Config.InPositionTolerance : 0.001;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.IsPickerAxisInPosition(axis, target, tolerance);
            return RearPicker.IsPickerAxisInPosition(axis, target, tolerance);
        }

        protected string BuildPickerAxisState(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.001;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance;
        }

        protected string BuildRequiredPickerAxesReason()
        {
            PickerAxis[] axes =
            {
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3,
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            var failures = new List<string>();
            foreach (PickerAxis axis in axes)
            {
                BaseAxis item = GetPickerAxis(axis);
                if (item == null)
                {
                    failures.Add(axis + "=missing");
                    continue;
                }

                if (!item.IsServoOn || item.IsAlarm)
                    failures.Add(BuildPickerAxisState(axis, item.ActualPosition));
            }

            return failures.Count == 0 ? string.Empty : string.Join("; ", failures);
        }

        protected BaseAxis GetPickerAxis(PickerAxis axis)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Axes.ContainsKey(axis))
                return FrontPicker.Axes[axis];
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Axes.ContainsKey(axis))
                return RearPicker.Axes[axis];
            return null;
        }

        private double GetPickerTeachingPosition(PickerAxis axis, string positionName)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.GetPickerTeachingPosition(axis, positionName);
            return RearPicker.GetPickerTeachingPosition(axis, positionName);
        }

        private PickerAxis GetPickerZAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        private PickerAxis GetPickerTAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerT0;
            if (index == 1) return PickerAxis.PickerT1;
            if (index == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private double ResolveTPosition(string positionArrayName)
        {
            if (string.Equals(positionArrayName, "DieBottomPosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerT0, "BottomPosition");
            if (string.Equals(positionArrayName, "DieSidePosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerT0, "SidePosition");
            if (string.Equals(positionArrayName, "DiePlacePosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerT0, "PlacePosition");
            return GetPickerTeachingPosition(PickerAxis.PickerT0, "PickPosition");
        }

        private double ResolveZPosition(string positionArrayName)
        {
            if (string.Equals(positionArrayName, "DieBottomPosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerZ0, "BottomPosition");
            if (string.Equals(positionArrayName, "DieSidePosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerZ0, "SidePosition");
            if (string.Equals(positionArrayName, "DiePlacePosition", StringComparison.OrdinalIgnoreCase))
                return GetPickerTeachingPosition(PickerAxis.PickerZ0, "PlacePosition");
            return GetPickerTeachingPosition(PickerAxis.PickerZ0, "PickPosition");
        }

        private double ResolvePickerAlignOffsetX(int index)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.Picker != null && index < FrontPicker.Config.Picker.Length)
                return FrontPicker.Config.Picker[index].AlignOffsetX;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null && RearPicker.Config.Picker != null && index < RearPicker.Config.Picker.Length)
                return RearPicker.Config.Picker[index].AlignOffsetX;
            return 0.0;
        }

        private double ResolvePickerAlignOffsetY(int index)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.Picker != null && index < FrontPicker.Config.Picker.Length)
                return FrontPicker.Config.Picker[index].AlignOffsetY;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null && RearPicker.Config.Picker != null && index < RearPicker.Config.Picker.Length)
                return RearPicker.Config.Picker[index].AlignOffsetY;
            return 0.0;
        }

        private double ResolvePickerAlignOffsetT(int index)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.Picker != null && index < FrontPicker.Config.Picker.Length)
                return FrontPicker.Config.Picker[index].AlignOffsetT;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null && RearPicker.Config.Picker != null && index < RearPicker.Config.Picker.Length)
                return RearPicker.Config.Picker[index].AlignOffsetT;
            return 0.0;
        }

        protected async Task<SequenceResourceLease> AcquireResourceAsync(
            SequenceResourceKind resource,
            string holder,
            CancellationToken ct)
        {
            SequenceResourceLease lease = await Context.Resources
                .AcquireAsync(resource, holder, ResolveResourceTimeout(), ct)
                .ConfigureAwait(false);
            if (lease == null)
                Fail("PICKER-RESOURCE", holder, "Resource acquire failed. resource=" + resource);
            return lease;
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                SequenceFailureStore.Record(Name, Kind.ToString(), CurrentStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, alarmCode, source, message);
                Context.LogPublic("[" + Name + "] FAIL " + alarmCode + " - " + message);
            }
            catch (Exception ex)
            {
                WriteLog(source, "Failure handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        protected void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
