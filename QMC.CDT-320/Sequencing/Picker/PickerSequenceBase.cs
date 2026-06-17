using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
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
        private IDisposable pickerWorkAreaScope;
        private PickerWorkZone pickerWorkAreaZone = PickerWorkZone.Unknown;

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

            using (SequenceLog.Push(
                Side == PickerSequenceSide.Front ? QMC.Common.Logging.EventKind.FrontHeadSeq : QMC.Common.Logging.EventKind.RearHeadSeq,
                Name, () => CurrentStep.ToString()))
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

        protected bool IsPickerSideEnabled()
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;
            return RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;
        }

        protected bool IsPickerSimulationOrDryRun()
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && (settings.BypassHardware || settings.DryRunMode))
                return true;

            if (Context != null && Context.Controller != null && Context.Controller.GlobalDryRun)
                return true;

            if (Side == PickerSequenceSide.Front)
                return IsFrontPickerSimulationOrDryRun();

            if (Side == PickerSequenceSide.Rear)
                return IsRearPickerSimulationOrDryRun();

            return false;
        }

        protected bool IsFrontPickerSimulationOrDryRun()
        {
            return FrontPicker != null &&
                   ((FrontPicker.Setup != null && FrontPicker.Setup.IsSimulationMode) ||
                    (FrontPicker.Config != null && FrontPicker.Config.bDryRun));
        }

        protected bool IsRearPickerSimulationOrDryRun()
        {
            return RearPicker != null &&
                   ((RearPicker.Setup != null && RearPicker.Setup.IsSimulationMode) ||
                    (RearPicker.Config != null && RearPicker.Config.bDryRun));
        }

        protected List<int> BuildEnabledPickerIndexes()
        {
            var result = new List<int>();

            bool[] usePicker = ResolveUsePickerArray();
            PickerRunOrderMode orderMode = ResolveRunOrderMode();
            int[] order = orderMode == PickerRunOrderMode.Ascending
                ? new[] { 0, 1, 2, 3 }
                : new[] { 3, 2, 1, 0 };

            for (int i = 0; i < order.Length; i++)
            {
                int index = order[i];
                if (usePicker != null && index < usePicker.Length && usePicker[index])
                    result.Add(index);
            }

            return result;
        }

        protected List<int> BuildLoadedPickerIndexesInRunOrder(string ownerName)
        {
            var result = new List<int>();

            try
            {
                List<int> enabled = BuildEnabledPickerIndexes();
                for (int i = 0; i < enabled.Count; i++)
                {
                    int index = enabled[i];
                    int pickerNo = ToPickerNo(index);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                    {
                        WriteLog(ownerName,
                            Name + " picker has no die. pickerNo=" + pickerNo +
                            ", pickerIndex=" + index + " - Check");
                        continue;
                    }

                    result.Add(index);
                    WriteLog(ownerName,
                        Name + " picker loaded die selected. die=" + die.DieId +
                        ", pickerNo=" + pickerNo +
                        ", pickerIndex=" + index +
                        ", result=" + die.Result +
                        ", location=" + (die.CurrentLocation != null ? die.CurrentLocation.ToString() : "-") +
                        " - Check");
                }
            }
            catch (Exception ex)
            {
                WriteLog(ownerName,
                    Name + " build loaded picker list failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return result;
        }

        protected int ToPickerNo(int pickerIndex)
        {
            if (pickerIndex < 0)
                return 1;
            if (pickerIndex > 3)
                return 4;
            return pickerIndex + 1;
        }

        protected int ToPickerIndex(int pickerNo)
        {
            if (pickerNo <= 1)
                return 0;
            if (pickerNo >= 4)
                return 3;
            return pickerNo - 1;
        }

        protected bool IsPickerIndexEnabled(int pickerIndex)
        {
            bool[] usePicker = ResolveUsePickerArray();
            return usePicker != null &&
                   pickerIndex >= 0 &&
                   pickerIndex < usePicker.Length &&
                   usePicker[pickerIndex];
        }

        private bool[] ResolveUsePickerArray()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
            {
                FrontPicker.Config.EnsureArrays();
                return FrontPicker.Config.UsePicker;
            }

            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
            {
                RearPicker.Config.EnsureArrays();
                return RearPicker.Config.UsePicker;
            }

            return null;
        }

        private PickerRunOrderMode ResolveRunOrderMode()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                return FrontPicker.Config.RunOrderMode;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                return RearPicker.Config.RunOrderMode;
            return PickerRunOrderMode.Descending;
        }

        protected async Task<int> MovePickerAxisAndVerifyAsync(
            PickerAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsPickerAxisAlreadyInPosition(axis, target))
                {
                    WriteLog("PickerMove",
                        Name + " " + description + " move skipped. Axis already in position. " +
                        BuildPickerAxisState(axis, target) + " - Ok");
                    return 0;
                }

                int result = await MovePickerAxisCommandAsync(axis, target, targetName).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-MOVE-CMD", Name, description + " move command failed. result=" + result + ", " + BuildPickerAxisState(axis, target));

                AxisMoveWaitResult waitResult = await WaitPickerAxisMoveDoneAsync(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-MOVE", waitResult), Name,
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, target)));

                if (!IsPickerAxisInPosition(axis, target))
                {
                    return Fail("PICKER-MOVE-FINAL-POS", Name,
                        description + " final position check failed after move. " +
                        BuildPickerAxisState(axis, target));
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
                return Fail("PICKER-MOVE-EX", Name, description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerAxesAndVerifyAsync(
            IDictionary<PickerAxis, double> targets,
            string description,
            CancellationToken ct,
            string targetName = null)
        {
            try
            {
                if (targets == null || targets.Count == 0)
                    return 0;

                ct.ThrowIfCancellationRequested();

                var commandTasks = new List<Task<int>>();
                var commandTargets = new List<KeyValuePair<PickerAxis, double>>();
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (IsPickerAxisAlreadyInPosition(pair.Key, pair.Value))
                    {
                        WriteLog("PickerMove",
                            Name + " " + description + " move skipped. Axis already in position. " +
                            BuildPickerAxisState(pair.Key, pair.Value) + " - Ok");
                        continue;
                    }

                    commandTargets.Add(pair);
                    commandTasks.Add(MovePickerAxisCommandAsync(pair.Key, pair.Value, targetName));
                }

                if (commandTasks.Count > 0)
                {
                    int[] commandResults = await Task.WhenAll(commandTasks).ConfigureAwait(false);
                    for (int commandIndex = 0; commandIndex < commandTargets.Count; commandIndex++)
                    {
                        KeyValuePair<PickerAxis, double> pair = commandTargets[commandIndex];
                        if (commandResults[commandIndex] != 0)
                            return Fail("PICKER-MOVE-CMD", Name, description + " move command failed. result=" + commandResults[commandIndex] + ", " + BuildPickerAxisState(pair.Key, pair.Value));
                    }

                    var waitTasks = new List<Task<AxisMoveWaitResult>>();
                    foreach (KeyValuePair<PickerAxis, double> pair in commandTargets)
                        waitTasks.Add(WaitPickerAxisMoveDoneAsync(pair.Key, pair.Value, ResolveTimeout(), ct));

                    AxisMoveWaitResult[] waitResults = await Task.WhenAll(waitTasks).ConfigureAwait(false);
                    for (int waitIndex = 0; waitIndex < commandTargets.Count; waitIndex++)
                    {
                        KeyValuePair<PickerAxis, double> pair = commandTargets[waitIndex];
                        if (waitResults[waitIndex] == null || !waitResults[waitIndex].Success)
                            return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-MOVE", waitResults[waitIndex]), Name,
                                description + " move/in-position wait failed. " +
                                FormatAxisMoveWaitResult(waitResults[waitIndex], BuildPickerAxisState(pair.Key, pair.Value)));
                    }
                }

                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (!IsPickerAxisInPosition(pair.Key, pair.Value))
                    {
                        return Fail("PICKER-MOVE-FINAL-POS", Name,
                            description + " final position check failed after parallel move. " +
                            BuildPickerAxisState(pair.Key, pair.Value));
                    }
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
            string targetName = positionArrayName + "[" + index + "]";

            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " pre Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            var xyTargets = new Dictionary<PickerAxis, double>();
            xyTargets[PickerAxis.PickerX] = ResolvePickerZoneX(positionArrayName, index);
            xyTargets[PickerAxis.PickerY] = ResolvePickerZoneY(positionArrayName, index);
            xyTargets[GetPickerTAxis(index)] = ResolveTPosition(positionArrayName) + ResolvePickerAlignOffsetT(index);

            result = await MovePickerAxesAndVerifyAsync(xyTargets, description + " XYT", ct, targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(index),
                ResolveZPosition(positionArrayName),
                description + " Z",
                ct,
                targetName).ConfigureAwait(false);
        }

        protected Task<int> MoveAllPickerZToAvoidAndVerifyAsync(string description, CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            return MovePickerAxesAndVerifyAsync(targets, description, ct, "AvoidPosition");
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
            return MovePickerAxesAndVerifyAsync(targets, description, ct, positionName);
        }

        protected async Task<int> MoveCurrentPickerToAvoidAndVerifyAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    description + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    description + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    description + " X축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeX").ConfigureAwait(false);
                if (result != 0)
                    return result;

                var tTargets = new Dictionary<PickerAxis, double>();
                tTargets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                tTargets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                tTargets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                tTargets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");

                result = await MovePickerAxesAndVerifyAsync(
                    tTargets,
                    description + " T축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeT").ConfigureAwait(false);
                if (result != 0)
                    return result;

                PickerAxis[] finalAxes =
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

                foreach (PickerAxis axis in finalAxes)
                {
                    double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                    if (!IsPickerAxisInPosition(axis, target))
                    {
                        return Fail("PICKER-AVOID-FINAL-POS", Name,
                            description + " 최종 Avoid 위치 확인 실패. " +
                            BuildPickerAxisState(axis, target));
                    }
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
                return Fail("PICKER-AVOID-EX", Name,
                    description + " Avoid 이동 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MoveOppositePickerToAvoidAndVerifyAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool fine = Options != null && Options.FineMove;

                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                    {
                        WriteLog("PickerOppositeAvoid",
                            Name + " opposite picker avoid skipped. RearPickerUnit is null. description=" + description + " - Check");
                        return 0;
                    }

                    if (!RearPicker.IsRearPickerInAvoidPosition())
                    {
                        int result = await RearPicker.MoveToRearPickerAvoidPosition(fine).ConfigureAwait(false);
                        if (result != 0)
                        {
                            return Fail("PICKER-OPPOSITE-AVOID", "RearPickerUnit",
                                description + " failed. result=" + result);
                        }
                    }

                    if (!RearPicker.IsRearPickerInAvoidPosition())
                    {
                        return Fail("PICKER-OPPOSITE-AVOID-CHECK", "RearPickerUnit",
                            description + " final position check failed. RearPicker is not at AvoidPosition.");
                    }

                    return 0;
                }

                if (FrontPicker == null)
                {
                    WriteLog("PickerOppositeAvoid",
                        Name + " opposite picker avoid skipped. FrontPickerUnit is null. description=" + description + " - Check");
                    return 0;
                }

                if (!FrontPicker.IsFrontPickerInAvoidPosition())
                {
                    int result = await FrontPicker.MoveToFrontPickerAvoidPosition(fine).ConfigureAwait(false);
                    if (result != 0)
                    {
                        return Fail("PICKER-OPPOSITE-AVOID", "FrontPickerUnit",
                            description + " failed. result=" + result);
                    }
                }

                if (!FrontPicker.IsFrontPickerInAvoidPosition())
                {
                    return Fail("PICKER-OPPOSITE-AVOID-CHECK", "FrontPickerUnit",
                        description + " final position check failed. FrontPicker is not at AvoidPosition.");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-AVOID-EX", Name,
                    description + " exception: " + ex.Message);
            }
            finally
            {
            }
        }

        protected void EnsurePickerWorkAreaReserved(PickerWorkZone zone, string description)
        {
            try
            {
                if (zone == PickerWorkZone.Unknown || zone == PickerWorkZone.Avoid)
                    return;

                if (pickerWorkAreaScope != null && pickerWorkAreaZone == zone)
                    return;

                ReleasePickerWorkArea();

                pickerWorkAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                    Side == PickerSequenceSide.Front,
                    zone,
                    Name + ":" + description);
                pickerWorkAreaZone = zone;

                WriteLog("PickerWorkArea",
                    Name + " reserved picker work area. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " picker work area reservation failed. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected void ReleasePickerWorkArea()
        {
            try
            {
                if (pickerWorkAreaScope == null)
                    return;

                PickerWorkZone releasedZone = pickerWorkAreaZone;
                pickerWorkAreaScope.Dispose();
                pickerWorkAreaScope = null;
                pickerWorkAreaZone = PickerWorkZone.Unknown;

                WriteLog("PickerWorkArea",
                    Name + " released picker work area. side=" + Side +
                    ", zone=" + releasedZone + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " picker work area release failed. side=" + Side +
                    ", zone=" + pickerWorkAreaZone +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
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
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                waitMs = FrontPicker.ResolvePickerBlowTimeMs(pickerNo);
            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                waitMs = RearPicker.ResolvePickerBlowTimeMs(pickerNo);

            if (Side == PickerSequenceSide.Front)
                await FrontPicker.PickerBlowOn(pickerNo, waitMs).ConfigureAwait(false);
            else
                await RearPicker.PickerBlowOn(pickerNo, waitMs).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
        }

        protected Task<int> MovePickerAxisCommandAsync(PickerAxis axis, double target, string targetName = null)
        {
            bool fine = Options != null && Options.FineMove;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxis(axis, target, fine, targetName);
            return RearPicker.MovePickerAxis(axis, target, fine, targetName);
        }

        protected async Task<AxisMoveWaitResult> WaitPickerAxisMoveDoneAsync(PickerAxis axis, double target, int timeoutMs, CancellationToken ct)
        {
            Task<AxisMoveWaitResult> waitTask = Side == PickerSequenceSide.Front
                ? FrontPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs)
                : RearPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs);

            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(waitTask, cancelTask).ConfigureAwait(false);
            if (completed == cancelTask)
                ct.ThrowIfCancellationRequested();
            return await waitTask.ConfigureAwait(false);
        }

        protected static string ResolveAxisMoveWaitAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            return AxisMoveWaiter.ResolveAlarmCode(prefix, waitResult);
        }

        protected static string FormatAxisMoveWaitResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            return AxisMoveWaiter.FormatResult(waitResult, fallbackState);
        }

        protected bool IsPickerAxisInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double tolerance = item != null && item.Config != null ? item.Config.InPositionTolerance : 0.001;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.IsPickerAxisInPosition(axis, target, tolerance);
            return RearPicker.IsPickerAxisInPosition(axis, target, tolerance);
        }

        protected bool IsPickerAxisAlreadyInPosition(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null || item.IsMoving)
                return false;

            return IsPickerAxisInPosition(axis, target);
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

        protected double GetPickerTeachingPosition(PickerAxis axis, string positionName)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.GetPickerTeachingPosition(axis, positionName);
            return RearPicker.GetPickerTeachingPosition(axis, positionName);
        }

        protected double ResolvePickerZoneX(string positionArrayName, int pickerIndex)
        {
            return GetPickerTeachingPosition(PickerAxis.PickerX, ResolveZonePositionName(positionArrayName)) +
                   ResolvePickerAlignOffsetX(pickerIndex);
        }

        protected double ResolvePickerZoneY(string positionArrayName, int pickerIndex)
        {
            return GetPickerTeachingPosition(PickerAxis.PickerY, ResolveZonePositionName(positionArrayName)) +
                   ResolvePickerAlignOffsetY(pickerIndex);
        }

        protected string ResolveZonePositionName(string positionArrayName)
        {
            if (string.Equals(positionArrayName, "DieBottomPosition", StringComparison.OrdinalIgnoreCase))
                return "BottomPosition";
            if (string.Equals(positionArrayName, "DieSidePosition", StringComparison.OrdinalIgnoreCase))
                return "SidePosition";
            if (string.Equals(positionArrayName, "DiePlacePosition", StringComparison.OrdinalIgnoreCase))
                return "PlacePosition";
            return "PickPosition";
        }

        protected PickerAxis GetPickerZAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        protected PickerAxis GetPickerTAxis(int index)
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

        protected double ResolvePickerAlignOffsetX(int index)
        {
            PickerAlignOffset offset = ResolvePickerAlignOffset(index);
            return offset != null ? offset.AlignOffsetX : 0.0;
        }

        protected double ResolvePickerAlignOffsetY(int index)
        {
            PickerAlignOffset offset = ResolvePickerAlignOffset(index);
            return offset != null ? offset.AlignOffsetY : 0.0;
        }

        protected double ResolvePickerAlignOffsetT(int index)
        {
            PickerAlignOffset offset = ResolvePickerAlignOffset(index);
            return offset != null ? offset.AlignOffsetT : 0.0;
        }

        protected double ResolveInputVisionToPickerXOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveInputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetX;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve InputVisionToPicker X offset. pickerIndex=" +
                index + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveInputVisionToPickerYOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveInputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetY;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve InputVisionToPicker Y offset. pickerIndex=" +
                index + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveOutputVisionToPickerXOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveOutputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetX;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve OutputVisionToPicker X offset. pickerIndex=" +
                index + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected double ResolveOutputVisionToPickerYOffset(int index)
        {
            double offsetX;
            double offsetY;
            string reason;
            if (TryResolveOutputVisionToPickerOffsets(index, out offsetX, out offsetY, out reason))
                return offsetY;

            WriteLog("PickerCoordinate",
                Name + " failed to resolve OutputVisionToPicker Y offset. pickerIndex=" +
                index + ", reason=" + reason + " - Failed");
            return 0.0;
        }

        protected bool TryResolveInputVisionToPickerOffsets(int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                out offsetX,
                out offsetY,
                out reason);
        }

        protected bool TryResolveOutputVisionToPickerOffsets(int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                out offsetX,
                out offsetY,
                out reason);
        }

        private PickerAlignOffset ResolvePickerAlignOffset(int index)
        {
            if (index < 0)
                return null;

            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.GetRuntimePickerOffset(index);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.GetRuntimePickerOffset(index);

            return null;
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
