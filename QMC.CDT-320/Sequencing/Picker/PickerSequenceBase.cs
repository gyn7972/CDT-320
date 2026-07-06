using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal abstract class PickerSequenceBase<TStep> where TStep : struct
    {
        private const double DefaultAutoProcessCorrectionMaxDistance = 2.0;
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
        protected CalibrationMotionSettings CalibrationMotion { get; private set; }
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

        protected SequenceResourceKind PickerResourceKind
        {
            get { return Side == PickerSequenceSide.Front ? SequenceResourceKind.FrontPicker : SequenceResourceKind.RearPicker; }
        }

        public async Task<int> RunAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            Options = options ?? PickerSequenceOptions.Default();
            Options.RunMode = Options.RunMode;

            using (SequenceLog.Push(
                Side == PickerSequenceSide.Front ? QMC.Common.Logging.EventKind.FrontHeadSeq : QMC.Common.Logging.EventKind.RearHeadSeq,
                Name, () => CurrentStep.ToString(), GetType().Name, Options.RunMode.ToString()))
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options.RunMode == SequenceRunMode.Auto,
                GetType().Name + ":" + Name + ":" + Options.RunMode))
            try
            {
                ct.ThrowIfCancellationRequested();
                SequenceTrace.RunStart(GetType().Name, Options.RunMode.ToString(), "name=" + Name, "side=" + Side, "kind=" + Kind);
                WriteLog("RunAsync", Name + " sequence start. kind=" + Kind + " - Start");
                int result = await ExecuteAsync(ct).ConfigureAwait(false);
                if (result == 0)
                {
                    WriteLog("RunAsync", Name + " sequence complete. kind=" + Kind + " - Ok");
                    SequenceTrace.RunEnd(GetType().Name, "Completed", result, "name=" + Name, "side=" + Side, "kind=" + Kind);
                }
                else
                {
                    SequenceTrace.RunEnd(GetType().Name, "Failed", result, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep);
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("RunAsync", Name + " sequence canceled. kind=" + Kind + " - Failed");
                SequenceTrace.RunEnd(GetType().Name, "Canceled", -1, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep);
                throw;
            }
            catch (SequenceStopException ex)
            {
                WriteLog("RunAsync", Name + " sequence stopped. kind=" + Kind + ", reason=" + ex.Message + " - Stopped");
                SequenceTrace.RunEnd(GetType().Name, "Stopped", -1, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep, "reason=" + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                int failResult = Fail("PICKER-SEQ-EX", Name, Name + " sequence exception: " + ex.Message);
                SequenceTrace.RunEnd(GetType().Name, "Failed", failResult, "name=" + Name, "side=" + Side, "kind=" + Kind, "step=" + CurrentStep, "error=" + ex.Message);
                return failResult;
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

        protected int ResolveMoveTimeout()
        {
            if (CalibrationMotion != null)
            {
                CalibrationMotion.EnsureDefaults();
                return CalibrationMotion.MoveTimeoutMs;
            }

            return ResolveTimeout();
        }

        protected int ResolveResourceTimeout()
        {
            return Options != null && Options.ResourceTimeoutMs > 0 ? Options.ResourceTimeoutMs : 30000;
        }

        protected void SetCalibrationMotion(CalibrationMotionSettings motion)
        {
            CalibrationMotion = motion != null ? motion.Clone() : null;
        }

        protected void SetOptionsForManualOperation(PickerSequenceOptions options)
        {
            Options = options ?? PickerSequenceOptions.Default();
        }

        protected void SaveRuntimeState(string reason)
        {
            try
            {
                if (Context == null || Context.Controller == null)
                    return;

                Context.Controller.SaveMachineRuntimeState(reason);
            }
            catch (Exception ex)
            {
                WriteLog("SaveRuntimeState", Name + " runtime state save failed. reason=" + reason + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
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

        protected string AppendAutoProcessCorrectionTargetTag(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                targetName = string.Empty;

            if (!IsRealEquipmentOrDryRunAutoSequence())
                return targetName;

            if (targetName.IndexOf("AutoProcessCorrection", StringComparison.OrdinalIgnoreCase) >= 0)
                return targetName;

            return targetName +
                   ";AutoSequence;AutoProcessCorrection;AutoProcessCorrectionMax=" +
                   DefaultAutoProcessCorrectionMaxDistance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected bool IsRealEquipmentOrDryRunAutoSequence()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return false;

            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null)
                    return false;

                return settings.UseAjin && (settings.DryRunMode || !settings.SimulationMode);
            }
            catch
            {
                return false;
            }
        }

        protected bool IsPickerMotionOnlyTestMode()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Auto &&
                   Options.PickerMotionOnlyTestMode;
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
                    if (Options != null &&
                        Options.RestrictToPickerNo > 0 &&
                        Options.RestrictToPickerNo != pickerNo)
                    {
                        continue;
                    }

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
            string targetName = null,
            bool forceMove = false)
        {
            Stopwatch totalWatch = Stopwatch.StartNew();
            long commandMs = 0;
            long waitMs = 0;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                if (!forceMove && IsPickerAxisAlreadyInPosition(axis, target))
                {
                    WriteLog("PickerMove",
                        Name + " " + description + " move skipped. Axis already in position. " +
                        BuildPickerAxisState(axis, target) + " - Ok");
                    SequenceTrace.MotionEnd("PickerMove", 0,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=AlreadyInPosition");
                    return 0;
                }

                PickerMoveAxisLogDetail axisDetail = BuildPickerMoveAxisLogDetail(axis, target);
                SequenceTrace.MotionStart("PickerMove",
                    "axis=" + axis,
                    "target=" + target,
                    "actual=" + axisDetail.Start,
                    "description=" + description,
                    "targetName=" + targetName,
                    "forceMove=" + forceMove);

                int sharedRailReadyResult = await WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                    axis,
                    target,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (sharedRailReadyResult != 0)
                    return sharedRailReadyResult;

                int yReadyResult = await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                    axis,
                    target,
                    null,
                    targetName,
                    description,
                    ct).ConfigureAwait(false);
                if (yReadyResult != 0)
                    return yReadyResult;

                int facingYReadyResult = await WaitPickerFacingYInterlockBeforeAutoMoveAsync(
                    axis,
                    target,
                    targetName,
                    description,
                    ct).ConfigureAwait(false);
                if (facingYReadyResult != 0)
                    return facingYReadyResult;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                Stopwatch commandWatch = Stopwatch.StartNew();
                int result = await SequenceAwaiter.AwaitAsync(
                    MovePickerAxisCommandAsync(axis, target, targetName, forceMove),
                    -1,
                    ct).ConfigureAwait(false);
                commandMs = commandWatch.ElapsedMilliseconds;
                if (result != 0)
                {
                    //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, null);
                    SequenceTrace.MotionEnd("PickerMove", result,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=CommandFailed");
                    return Fail("PICKER-MOVE-CMD", Name, BuildPickerMoveCommandFailureMessage(axis, target, description, result));
                }

                Stopwatch waitWatch = Stopwatch.StartNew();
                int moveTimeout = ResolveMoveTimeout();
                AxisMoveWaitResult waitResult = await WaitPickerAxisMoveDoneAsync(axis, target, moveTimeout, ct).ConfigureAwait(false);
                waitMs = waitWatch.ElapsedMilliseconds;
                if (waitResult == null || !waitResult.Success)
                {
                    //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, waitResult);
                    SequenceTrace.MotionEnd("PickerMove", -1,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=WaitFailed",
                        "timeoutMs=" + moveTimeout,
                        "wait=" + (waitResult != null ? waitResult.Code.ToString() : "null"));
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-MOVE", waitResult), Name,
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, target)));
                }

                if (!IsPickerAxisInPosition(axis, target))
                {
                    //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, waitResult);
                    SequenceTrace.MotionEnd("PickerMove", -1,
                        "axis=" + axis,
                        "target=" + target,
                        "description=" + description,
                        "status=FinalPositionFailed");
                    return Fail("PICKER-MOVE-FINAL-POS", Name,
                        description + " final position check failed after move. " +
                        BuildPickerAxisState(axis, target));
                }

                //WritePickerSequenceMoveElapsed(axisDetail, targetName, description, result, commandMs, waitMs, totalWatch.ElapsedMilliseconds, waitResult);
                SequenceTrace.MotionEnd("PickerMove", 0,
                    "axis=" + axis,
                    "target=" + target,
                    "actual=" + (GetPickerAxis(axis) != null ? GetPickerAxis(axis).ActualPosition.ToString() : ""),
                    "description=" + description,
                    "commandMs=" + commandMs,
                    "waitMs=" + waitMs,
                    "elapsedMs=" + totalWatch.ElapsedMilliseconds);
                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SequenceTrace.MotionEnd("PickerMove", -1,
                    "axis=" + axis,
                    "target=" + target,
                    "description=" + description,
                    "status=Exception",
                    "error=" + ex.Message);
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
            string targetName = null,
            bool forceMove = false)
        {
            Stopwatch totalWatch = Stopwatch.StartNew();
            long commandMs = 0;
            long waitMs = 0;
            try
            {
                if (targets == null || targets.Count == 0)
                    return 0;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                int sharedRailReadyResult = await WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                    targets,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (sharedRailReadyResult != 0)
                    return sharedRailReadyResult;

                int yReadyResult = await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                    targets,
                    targetName,
                    description,
                    ct,
                    forceMove).ConfigureAwait(false);
                if (yReadyResult != 0)
                    return yReadyResult;

                ct.ThrowIfCancellationRequested();
                if (IsAlarmStopActive())
                    return StopPickerMoveBecauseAlarmActive(description);

                //수정포인트 1
                var commandTasks = new List<Task<int>>();
                var commandTargets = new List<KeyValuePair<PickerAxis, double>>();
                var commandDetails = new List<PickerMoveAxisLogDetail>();
                double pairedXTarget;
                double pairedYTarget;
                bool hasPairedXTarget = targets.TryGetValue(PickerAxis.PickerX, out pairedXTarget);
                bool hasPairedYTarget = targets.TryGetValue(PickerAxis.PickerY, out pairedYTarget);
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsAlarmStopActive())
                        return StopPickerMoveBecauseAlarmActive(description);

                    if (!forceMove && IsPickerAxisAlreadyInPosition(pair.Key, pair.Value))
                    {
                        if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                        {
                            WriteLog("PickerMove",
                                Name + " " + description + " move skipped. Axis already in position. " +
                                BuildPickerAxisState(pair.Key, pair.Value) + " - Ok");
                        }
                        continue;
                    }

                    int facingYReadyResult = await WaitPickerFacingYInterlockBeforeAutoMoveAsync(
                        pair.Key,
                        pair.Value,
                        targetName,
                        description,
                        ct,
                        hasPairedXTarget ? (double?)pairedXTarget : null,
                        hasPairedYTarget ? (double?)pairedYTarget : null).ConfigureAwait(false);
                    if (facingYReadyResult != 0)
                        return facingYReadyResult;

                    commandTargets.Add(pair);
                    commandDetails.Add(BuildPickerMoveAxisLogDetail(pair.Key, pair.Value));
                    commandTasks.Add(MovePickerAxisCommandAsync(pair.Key, pair.Value, targetName, forceMove));
                }

                if (commandTasks.Count > 0)
                {
                    Stopwatch commandWatch = Stopwatch.StartNew();
                    int[] commandResults = await SequenceAwaiter.AwaitAsync(
                        Task.WhenAll(commandTasks),
                        new int[0],
                        ct).ConfigureAwait(false);
                    commandMs = commandWatch.ElapsedMilliseconds;
                    for (int commandIndex = 0; commandIndex < commandTargets.Count; commandIndex++)
                    {
                        KeyValuePair<PickerAxis, double> pair = commandTargets[commandIndex];
                        if (commandResults[commandIndex] != 0)
                        {
                            WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, commandResults[commandIndex], commandMs, waitMs, totalWatch.ElapsedMilliseconds, "CommandFailed:" + pair.Key);
                            return Fail("PICKER-MOVE-CMD", Name, BuildPickerMoveCommandFailureMessage(pair.Key, pair.Value, description, commandResults[commandIndex]));
                        }
                    }

                    var waitTasks = new List<Task<AxisMoveWaitResult>>();
                    foreach (KeyValuePair<PickerAxis, double> pair in commandTargets)
                        waitTasks.Add(WaitPickerAxisMoveDoneAsync(pair.Key, pair.Value, ResolveMoveTimeout(), ct));

                    Stopwatch waitWatch = Stopwatch.StartNew();
                    AxisMoveWaitResult[] waitResults = await SequenceAwaiter.AwaitAsync(
                        Task.WhenAll(waitTasks),
                        new AxisMoveWaitResult[0],
                        ct).ConfigureAwait(false);
                    waitMs = waitWatch.ElapsedMilliseconds;
                    for (int waitIndex = 0; waitIndex < commandTargets.Count; waitIndex++)
                    {
                        KeyValuePair<PickerAxis, double> pair = commandTargets[waitIndex];
                        if (waitResults[waitIndex] == null || !waitResults[waitIndex].Success)
                        {
                            string waitSummary = waitResults[waitIndex] != null ? waitResults[waitIndex].Failure.ToString() : "NullWaitResult";
                            WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, -1, commandMs, waitMs, totalWatch.ElapsedMilliseconds, "WaitFailed:" + pair.Key + ":" + waitSummary);
                            return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-MOVE", waitResults[waitIndex]), Name,
                                description + " move/in-position wait failed. " +
                                FormatAxisMoveWaitResult(waitResults[waitIndex], BuildPickerAxisState(pair.Key, pair.Value)));
                        }
                    }
                }
                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    if (!IsPickerAxisInPosition(pair.Key, pair.Value))
                    {
                        WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, -1, commandMs, waitMs, totalWatch.ElapsedMilliseconds, "FinalPositionFailed:" + pair.Key);
                        return Fail("PICKER-MOVE-FINAL-POS", Name,
                            description + " final position check failed after parallel move. " +
                            BuildPickerAxisState(pair.Key, pair.Value));
                    }
                }
                if (commandTargets.Count > 0 && (waitMs >= 200 || totalWatch.ElapsedMilliseconds >= 250))
                    WritePickerSequenceGroupMoveElapsed(targetName, description, commandDetails, 0, commandMs, waitMs, totalWatch.ElapsedMilliseconds, "Ok");

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

        private Task<int> WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove)
        {
            if (targets == null)
                return Task.FromResult(0);

            double pickerXTarget;
            if (!targets.TryGetValue(PickerAxis.PickerX, out pickerXTarget))
                return Task.FromResult(0);

            return WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
                PickerAxis.PickerX,
                pickerXTarget,
                targetName,
                description,
                ct,
                forceMove);
        }

        private async Task<int> WaitPickerXSharedRailDistanceBeforeAutoMoveAsync(
            PickerAxis axis,
            double target,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerX)
                    return 0;

                if (!forceMove && IsPickerAxisAlreadyInPosition(axis, target))
                    return 0;

                BaseAxis pickerX = GetPickerAxis(axis);
                if (pickerX == null)
                    return Fail("PICKER-SHARED-RAIL-X-AXIS-MISSING", Name,
                        description + " PickerX SharedRailX 거리 확인 실패. PickerX 축을 찾을 수 없습니다.");

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(Context != null ? Context.Machine : null);
                if (service == null || !service.IsSharedRailAxis(pickerX))
                    return 0;

                // 현재 기준: PickerX 이동은 명령 전 SharedRailX 거리 검사를 1순위로 확인한다.
                int timeoutMs = ResolveMoveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                string reason = string.Empty;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".PickerXSharedRailDistanceWait");

                    if (IsAlarmStopActive())
                        return StopPickerMoveBecauseAlarmActive(description);

                    if (service.VerifySingleAxisMove(pickerX, target, out reason))
                        break;

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("PICKER-SHARED-RAIL-X-DISTANCE-TIMEOUT", Name,
                            description + " PickerX 이동 전 SharedRailX 거리 대기 시간 초과. " +
                            "side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", reason=" + reason +
                            ", " + BuildPickerAxisState(axis, target));
                    }

                    if (!waitLogged)
                    {
                        WriteLog("PickerSharedRailXGate",
                            Name + " Auto PickerX 이동 전 SharedRailX 거리 대기. " +
                            "side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", description=" + description +
                            ", reason=" + reason + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerSharedRailXGate wait. side=" + Side +
                            ", target=" + target.ToString("F6") +
                            ", targetName=" + (targetName ?? "-") +
                            ", reason=" + reason +
                            ", pickerState=" + BuildPickerAxisState(axis, target));
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    WriteLog("PickerSharedRailXGate",
                        Name + " Auto PickerX 이동 전 SharedRailX 거리 대기 완료. " +
                        "side=" + Side +
                        ", target=" + target.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") +
                        ", elapsedMs=" + elapsedMs.ToString("0") + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerSharedRailXGate wait complete. side=" + Side +
                        ", target=" + target.ToString("F6") +
                        ", targetName=" + (targetName ?? "-") +
                        ", elapsedMs=" + elapsedMs.ToString("0"));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-SHARED-RAIL-X-GATE-EX", Name,
                    description + " PickerX SharedRailX 거리 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", target=" + target.ToString("F6") +
                    ", targetName=" + (targetName ?? "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerFacingYInterlockBeforeAutoMoveAsync(
            PickerAxis axis,
            double target,
            string targetName,
            string description,
            CancellationToken ct,
            double? pairedXTarget = null,
            double? pairedYTarget = null)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerX && axis != PickerAxis.PickerY)
                    return 0;

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".WaitPickerFacingYInterlock:" + axis);

                    string detail;
                    bool clear = PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock(
                        Context != null ? Context.Machine : null,
                        Side == PickerSequenceSide.Front,
                        axis,
                        target,
                        targetName,
                        pairedXTarget,
                        pairedYTarget,
                        out detail);
                    if (clear)
                        break;

                    if (!waitLogged)
                    {
                        WriteLog("PickerFacingYGate",
                            Name + " Auto Picker 이동 대기. Front/Rear PickerX가 마주보는 구간에서 양쪽 PickerY 돌출 간섭이 해제될 때까지 기다립니다. " +
                            "side=" + Side +
                            ", axis=" + axis +
                            ", target=" + target.ToString("0.###") +
                            ", targetName=" + (targetName ?? "-") +
                            ", description=" + description +
                            ", detail=" + detail + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerFacingYGate wait. side=" + Side +
                            ", axis=" + axis +
                            ", target=" + target.ToString("0.###") +
                            ", targetName=" + (targetName ?? "-") +
                            ", detail=" + detail);
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerFacingYGate",
                        Name + " Auto Picker 이동 대기 완료. Front/Rear PickerY 돌출 X거리 인터락 해제 확인. " +
                        "side=" + Side +
                        ", axis=" + axis +
                        ", targetName=" + (targetName ?? "-") +
                        ", description=" + description + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerFacingYGate wait complete. side=" + Side +
                        ", axis=" + axis +
                        ", targetName=" + (targetName ?? "-"));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-FACING-Y-GATE-EX", Name,
                    "Auto Picker 이동 전 Front/Rear PickerY 돌출 X거리 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", axis=" + axis +
                    ", targetName=" + (targetName ?? "-") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task<int> MovePickerXTThenYAndVerifyAsync(
            IDictionary<PickerAxis, double> targets,
            string description,
            CancellationToken ct,
            string targetName = null,
            bool forceMove = false)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            double pickerYTarget;
            bool hasPickerY = targets.TryGetValue(PickerAxis.PickerY, out pickerYTarget);

            var xtTargets = new Dictionary<PickerAxis, double>();
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (pair.Key == PickerAxis.PickerY)
                    continue;

                xtTargets[pair.Key] = pair.Value;
            }

            int result = await MovePickerAxesAndVerifyAsync(
                xtTargets,
                description + " X/T",
                ct,
                targetName,
                forceMove).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!hasPickerY || (!forceMove && IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, pickerYTarget)))
                return 0;

            WriteLog("PickerMove",
                Name + " " + description + " X/T 이동 완료 후 PickerY 전진을 시작합니다. " +
                "targetY=" + pickerYTarget +
                ", targetName=" + (targetName ?? "-") +
                " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                pickerYTarget,
                description + " Y",
                ct,
                targetName,
                forceMove).ConfigureAwait(false);
        }

        private async Task<int> WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            string description,
            CancellationToken ct,
            bool forceMove = false)
        {
            if (targets == null || !targets.ContainsKey(PickerAxis.PickerY))
                return 0;

            double target = targets[PickerAxis.PickerY];
            if (!forceMove && IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, target))
                return 0;

            double pairedXTarget;
            return await WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
                PickerAxis.PickerY,
                target,
                targets.TryGetValue(PickerAxis.PickerX, out pairedXTarget) ? (double?)pairedXTarget : null,
                targetName,
                description,
                ct).ConfigureAwait(false);
        }

        private async Task<int> WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync(
            PickerAxis axis,
            double target,
            double? pairedXTarget,
            string targetName,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (axis != PickerAxis.PickerY)
                    return 0;

                if (!IsForwardPickerYMoveTarget(targetName))
                    return 0;

                bool waitLogged = false;
                PickerWorkZone targetZone = ResolvePickerYForwardTargetZone(targetName);
                string gateDetail;
                while (!IsOppositePickerYReadyForForwardMove(targetZone, target, pairedXTarget, targetName, out gateDetail))
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".WaitOppositePickerYAvoid");

                    if (!waitLogged)
                    {
                        WriteLog("PickerYMoveGate",
                            Name + " Auto Y축 전진 이동 대기. 상대 PickerY가 Avoid 위치이고 이동 타깃이 정리될 때까지 기다립니다. " +
                            "side=" + Side +
                            ", targetName=" + (targetName ?? "-") +
                            ", targetZone=" + targetZone +
                            ", targetY=" + target.ToString("0.###") +
                            ", pairedXTarget=" + (pairedXTarget.HasValue ? pairedXTarget.Value.ToString("0.###") : "-") +
                            ", description=" + description +
                            ", gate=" + gateDetail +
                            ", opposite=" + BuildOppositePickerYState() + " - Wait");
                        WriteSharedRailXLog(
                            Name + " PickerYMoveGate wait. side=" + Side +
                            ", targetName=" + (targetName ?? "-") +
                            ", targetZone=" + targetZone +
                            ", targetY=" + target.ToString("0.###") +
                            ", pairedXTarget=" + (pairedXTarget.HasValue ? pairedXTarget.Value.ToString("0.###") : "-") +
                            ", gate=" + gateDetail +
                            ", description=" + description +
                            ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                        waitLogged = true;
                    }

                    //await Task.Delay(50, ct).ConfigureAwait(false);
                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerYMoveGate",
                        Name + " Auto Y축 전진 이동 대기 완료. 상대 PickerY Avoid 및 이동 타깃 해제 확인. " +
                        "side=" + Side +
                        ", targetName=" + (targetName ?? "-") +
                        ", targetZone=" + targetZone +
                        ", description=" + description + " - Ok");
                    WriteSharedRailXLog(
                        Name + " PickerYMoveGate wait complete. side=" + Side +
                        ", targetName=" + (targetName ?? "-") +
                        ", targetZone=" + targetZone +
                        ", description=" + description +
                        ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-Y-MOVE-GATE-EX", Name,
                    "Auto Y축 전진 이동 전 상대 PickerY Avoid 대기 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", targetName=" + (targetName ?? "-") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsForwardPickerYMoveTarget(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return true;

            if (targetName.IndexOf("AvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("InputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("OutputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("PickerPhase=SafeY", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        private bool IsOppositePickerYReadyForForwardMove(
            PickerWorkZone targetZone,
            double targetY,
            double? pairedXTarget,
            string targetName,
            out string detail)
        {
            detail = string.Empty;

            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;

                string facingDetail;
                bool facingClear = PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock(
                    Context != null ? Context.Machine : null,
                    Side == PickerSequenceSide.Front,
                    PickerAxis.PickerY,
                    targetY,
                    targetName,
                    pairedXTarget,
                    null,
                    out facingDetail);
                if (!facingClear)
                {
                    detail = "FacingYDistanceBlocked: " + facingDetail;
                    return false;
                }

                if (PickerZoneInterlockRules.IsProcessZone(targetZone) &&
                    IsOppositePickerInPlacePhase() &&
                    !IsOppositePickerYAtAvoidPosition())
                {
                    detail = "OppositePlacePhaseYOut";
                    return false;
                }

                PickerWorkZone workAreaZone;
                string workAreaOwner;
                bool oppositeWorkActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workAreaZone,
                    out workAreaOwner);
                if (PickerZoneInterlockRules.IsProcessZone(targetZone) &&
                    oppositeWorkActive &&
                    workAreaZone == PickerWorkZone.Output &&
                    !IsOppositePickerYAtAvoidPosition())
                {
                    detail = "OppositeOutputWorkAreaYOut owner=" + workAreaOwner;
                    return false;
                }

                PickerWorkZone activeTargetZone = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront);
                if (activeTargetZone != PickerWorkZone.Unknown)
                {
                    bool canShare = PickerZoneInterlockRules.CanShareForwardY(targetZone, activeTargetZone);
                    if (!canShare)
                        detail = "OppositeActiveYTargetZone=" + activeTargetZone;
                    return canShare;
                }

                if (IsOppositePickerYAtAvoidPosition())
                    return true;

                PickerWorkZone oppositeZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront);
                bool shareByZone = PickerZoneInterlockRules.CanShareForwardY(targetZone, oppositeZone);
                if (!shareByZone)
                    detail = "OppositeCurrentZone=" + oppositeZone;
                return shareByZone;
            }
            catch
            {
                detail = "OppositePickerYReadyCheckException";
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerInPlacePhase()
        {
            try
            {
                if (Context == null || Context.PickerPhases == null)
                    return false;

                PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                PickerPhaseState opposite = Side == PickerSequenceSide.Front ? snapshot.Rear : snapshot.Front;
                return opposite.Phase == PickerProcessPhase.Place;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static PickerWorkZone ResolvePickerYForwardTargetZone(string targetName)
        {
            string name = (targetName ?? string.Empty).Replace(" ", string.Empty);
            if (name.Length == 0)
                return PickerWorkZone.Unknown;

            if (name.IndexOf("PickerZone=Input", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DiePick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("PickPosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Input;
            if (name.IndexOf("PickerZone=Bottom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DieBottom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("BottomPosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Bottom;
            if (name.IndexOf("PickerZone=Side", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DieSide", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("SidePosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Side;
            if (name.IndexOf("PickerZone=Output", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("DiePlace", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("PlacePosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Output;
            if (name.IndexOf("PickerZone=Avoid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("AvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("InputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("OutputAvoidPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("SafeRetreat", StringComparison.OrdinalIgnoreCase) >= 0)
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private bool IsOppositePickerYAtAvoidPosition()
        {
            try
            {
                BaseAxis oppositeY = ResolveOppositePickerYAxis();
                if (oppositeY == null)
                    return true;

                // 현재 기준: 상대 PickerY 안전 판단은 Home(0) 또는 실제 AvoidPosition만 인정한다.
                if (IsAxisAtHomePosition(oppositeY))
                    return true;

                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                        return true;

                    return RearPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                }

                if (FrontPicker == null)
                    return true;

                return FrontPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

                // 기존 조건: Front/Rear 모두 InputAvoidPosition/OutputAvoidPosition도 PickerY 안전 위치로 보았다.
                //return OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition") ||
                //       OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition") ||
                //       OppositePicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition");
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsAxisAtHomePosition(BaseAxis axis)
        {
            if (axis == null)
                return true;

            return Math.Abs(axis.ActualPosition) <= ResolveAxisInPositionTolerance(axis);
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        private string BuildOppositePickerYState()
        {
            try
            {
                BaseAxis axis = ResolveOppositePickerYAxis();

                if (axis == null)
                    return "PickerY=null";

                return "name=" + axis.Name +
                       ", actual=" + axis.ActualPosition +
                       ", moving=" + (axis.IsMoving ? "Y" : "N") +
                       ", activeTargetZone=" + PickerZoneInterlockRules.GetPickerYActiveTargetZone(Side == PickerSequenceSide.Rear) +
                       ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                       ", alarm=" + (axis.IsAlarm ? "ON" : "OFF");
            }
            catch (Exception ex)
            {
                return "stateError=" + ex.Message;
            }
            finally
            {
            }
        }

        private BaseAxis ResolveOppositePickerYAxis()
        {
            return ResolveOppositePickerAxis(PickerAxis.PickerY);
        }

        private BaseAxis ResolveOppositePickerAxis(PickerAxis pickerAxis)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    BaseAxis axis;
                    if (RearPicker != null &&
                        RearPicker.Axes != null &&
                        RearPicker.Axes.TryGetValue(pickerAxis, out axis))
                        return axis;

                    return null;
                }

                BaseAxis frontAxis;
                if (FrontPicker != null &&
                    FrontPicker.Axes != null &&
                    FrontPicker.Axes.TryGetValue(pickerAxis, out frontAxis))
                    return frontAxis;

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private string BuildOppositePickerSharedRailXState(string movingAxes)
        {
            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                string oppositeName = oppositeIsFront ? "FrontPicker" : "RearPicker";
                BaseAxis x = ResolveOppositePickerAxis(PickerAxis.PickerX);
                BaseAxis y = ResolveOppositePickerAxis(PickerAxis.PickerY);
                PickerWorkZone currentXZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront);
                PickerWorkZone activeYTargetZone = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront);
                PickerWorkZone workAreaZone;
                string workAreaOwner;
                bool workAreaActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workAreaZone,
                    out workAreaOwner);

                return "opposite=" + oppositeName +
                       ", currentXZone=" + currentXZone +
                       ", activeYTargetZone=" + activeYTargetZone +
                       ", yAtAvoid=" + (IsOppositePickerYAtAvoidPosition() ? "Y" : "N") +
                       ", workArea=" + (workAreaActive ? workAreaZone.ToString() : "None") +
                       ", owner=" + (workAreaActive ? workAreaOwner : "-") +
                       ", movingAxes=" + (string.IsNullOrWhiteSpace(movingAxes) ? "-" : movingAxes) +
                       ", x=" + FormatSharedRailAxis(x) +
                       ", y=" + FormatSharedRailAxis(y);
            }
            catch (Exception ex)
            {
                return "stateError=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string FormatSharedRailAxis(BaseAxis axis)
        {
            if (axis == null)
                return "<null>";

            return axis.Name +
                   "(actual=" + axis.ActualPosition.ToString("0.###") +
                   ", moving=" + (axis.IsMoving ? "Y" : "N") +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") + ")";
        }

        private static void WriteSharedRailXLog(string message)
        {
            try
            {
                Log.Write("SharedRailX", message);
            }
            catch
            {
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

            PickerCalibratedZoneTarget zoneTarget = ResolvePickerZoneTarget(positionArrayName, index);
            var xyTargets = new Dictionary<PickerAxis, double>();
            xyTargets[PickerAxis.PickerX] = zoneTarget.X;
            xyTargets[PickerAxis.PickerY] = zoneTarget.Y;
            xyTargets[zoneTarget.PickerTAxis] = zoneTarget.T;

            result = await MovePickerAxesAndVerifyAsync(xyTargets, description + " XYT", ct, targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MovePickerAxisAndVerifyAsync(
                zoneTarget.PickerZAxis,
                zoneTarget.Z,
                description + " Z",
                ct,
                targetName).ConfigureAwait(false);
        }

        protected Task<int> MoveAllPickerZToAvoidAndVerifyAsync(string description, CancellationToken ct, bool forceMove = false)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerZ0] = GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition");
            targets[PickerAxis.PickerZ1] = GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition");
            targets[PickerAxis.PickerZ2] = GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition");
            targets[PickerAxis.PickerZ3] = GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition");
            return MovePickerAxesAndVerifyAsync(targets, description, ct, "AvoidPosition", forceMove);
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

        // INV-6: 인터락/리스 해제·정지·Abort 전에 자기 픽커를 물리적으로 안전(Z=Avoid → Y=Avoid)하게 후퇴시키고 검증한다.
        // 순서(Z 상승 후 Y 후퇴)가 안전의 핵심이다. 공용 레일 X는 여기서 움직이지 않는다(마주보기 위험 회피).
        protected async Task<int> EnsureSelfSafeAsync(string reason, CancellationToken ct)
        {
            string label = string.IsNullOrWhiteSpace(reason) ? "EnsureSelfSafe" : reason;
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    label + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    label + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-ENSURE-SELF-SAFE-EX", Name,
                    label + " 자기 픽커 안전 후퇴 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // INV-7: 시작/재개 첫 이동 전, "자기(현재 side)" 픽커가 물리적으로 안전 배치(Avoid = Y·Z·X·T Avoid)인지 이동 없이 확인만 한다.
        // 자기 픽커만 확인하는 이유: Front/Rear 공정은 병렬로 시작되므로 "양쪽"을 확인하면 상대가 정상적으로 첫 전진을 시작한 순간
        // 오탐(false fail)이 난다. 각 픽커가 자기 CheckUnit(첫 이동 전)에서 자기 Avoid를 확인하면 전체적으로 양쪽 Avoid가 보장된다.
        // Start 흐름은 항상 Ready 시퀀스로 상부축을 Avoid로 정렬한 뒤 공정을 시작하므로 정상 시작에서는 통과한다.
        protected bool VerifySafeStartConfig(out string detail)
        {
            detail = string.Empty;
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    bool frontEnabled = FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;
                    if (frontEnabled && !FrontPicker.IsFrontPickerInAvoidPosition())
                    {
                        detail = "FrontPicker가 Avoid 위치가 아닙니다.";
                        return false;
                    }
                }
                else
                {
                    bool rearEnabled = RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;
                    if (rearEnabled && !RearPicker.IsRearPickerInAvoidPosition())
                    {
                        detail = "RearPicker가 Avoid 위치가 아닙니다.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                detail = "안전 시작 배치 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
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

                if (Options != null && Options.RunMode == SequenceRunMode.Auto)
                    return await WaitOppositePickerReadyForAutoAsync(description, ct).ConfigureAwait(false);

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
                                description + " 실패. RearPicker 어보이드 이동 명령 실패. result=" + result);
                        }
                    }

                    if (!RearPicker.IsRearPickerInAvoidPosition())
                    {
                        return Fail("PICKER-OPPOSITE-AVOID-CHECK", "RearPickerUnit",
                            description + " 최종 위치 확인 실패. RearPicker가 Avoid 위치가 아닙니다.");
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
                            description + " 실패. FrontPicker 어보이드 이동 명령 실패. result=" + result);
                    }
                }

                if (!FrontPicker.IsFrontPickerInAvoidPosition())
                {
                    return Fail("PICKER-OPPOSITE-AVOID-CHECK", "FrontPickerUnit",
                        description + " 최종 위치 확인 실패. FrontPicker가 Avoid 위치가 아닙니다.");
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
                    description + " 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitOppositePickerReadyForAutoAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string oppositeName = Side == PickerSequenceSide.Front ? "RearPicker" : "FrontPicker";
                bool loggedWait = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    string movingAxes;
                    if (IsOppositePickerMoving(out movingAxes))
                    {
                        if (!loggedWait)
                        {
                            WriteLog("PickerOppositeWait",
                                Name + " auto wait. Opposite picker is moving. description=" + description +
                                ", opposite=" + oppositeName +
                                ", movingAxes=" + movingAxes + " - Check");
                            WriteSharedRailXLog(
                                Name + " PickerOppositeWait wait. reason=OppositeMoving" +
                                ", description=" + description +
                                ", opposite=" + oppositeName +
                                ", oppositeState=" + BuildOppositePickerSharedRailXState(movingAxes));
                            loggedWait = true;
                        }

                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }

                    PickerWorkZone oppositeZone;
                    string oppositeOwner;
                    bool oppositeWorkActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                        Side != PickerSequenceSide.Front,
                        out oppositeZone,
                        out oppositeOwner);

                    if (oppositeWorkActive &&
                        PickerZoneInterlockRules.IsProcessZone(oppositeZone))
                    {
                        if (!loggedWait)
                        {
                            WriteLog("PickerOppositeWait",
                                Name + " auto wait. Opposite picker is using inspection area. description=" + description +
                                ", opposite=" + oppositeName +
                                ", zone=" + oppositeZone +
                                ", owner=" + oppositeOwner + " - Check");
                            WriteSharedRailXLog(
                                Name + " PickerOppositeWait wait. reason=OppositeInspectionArea" +
                                ", description=" + description +
                                ", opposite=" + oppositeName +
                                ", zone=" + oppositeZone +
                                ", owner=" + oppositeOwner +
                                ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                            loggedWait = true;
                        }

                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }

                    if (loggedWait)
                    {
                        WriteLog("PickerOppositeWait",
                            Name + " auto wait complete. Opposite picker is ready. description=" + description +
                            ", opposite=" + oppositeName + " - Ok");
                        WriteSharedRailXLog(
                            Name + " PickerOppositeWait wait complete. description=" + description +
                            ", opposite=" + oppositeName +
                            ", oppositeState=" + BuildOppositePickerSharedRailXState(null));
                    }

                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-WAIT-EX", Name,
                    description + " 자동 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOppositePickerMoving(out string movingAxes)
        {
            movingAxes = string.Empty;

            try
            {
                IReadOnlyDictionary<PickerAxis, BaseAxis> axes = null;
                if (Side == PickerSequenceSide.Front)
                    axes = RearPicker != null ? RearPicker.Axes : null;
                else
                    axes = FrontPicker != null ? FrontPicker.Axes : null;

                if (axes == null)
                    return false;

                var moving = new List<string>();
                foreach (KeyValuePair<PickerAxis, BaseAxis> pair in axes)
                {
                    if (pair.Value != null && pair.Value.IsMoving)
                        moving.Add(pair.Value.Name);
                }

                movingAxes = moving.Count > 0 ? string.Join(",", moving.ToArray()) : string.Empty;
                return moving.Count > 0;
            }
            catch (Exception ex)
            {
                movingAxes = "opposite picker moving check failed: " + ex.Message;
                return true;
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

        protected void SetPickerBlow(int pickerNo, bool on)
        {
            if (Side == PickerSequenceSide.Front)
                FrontPicker.SetPickerBlow(pickerNo, on);
            else
                RearPicker.SetPickerBlow(pickerNo, on);
        }

        protected async Task<int> PickerBlowAsync(int pickerNo, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitMs = 100;
                if (Side == PickerSequenceSide.Front && FrontPicker != null)
                    waitMs = FrontPicker.ResolvePickerBlowTimeMs(pickerNo);
                if (Side == PickerSequenceSide.Rear && RearPicker != null)
                    waitMs = RearPicker.ResolvePickerBlowTimeMs(pickerNo);

                SetPickerBlow(pickerNo, true);
                if (waitMs > 0)
                    await Task.Delay(waitMs, ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BLOW", Name,
                    "Picker Blow 동작 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                try
                {
                    SetPickerBlow(pickerNo, false);
                }
                catch (Exception ex)
                {
                    WriteLog("PickerBlow",
                        Name + " Picker Blow OFF 정리 실패. side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", error=" + ex.Message + " - Failed");
                }
            }
        }

        protected async Task<int> VerifyPickerFlowStateAsync(int pickerNo, bool expected, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsPickerSimulationOrDryRun())
                {
                    WriteLog("PickerFlowCheck",
                        Name + " Flow Check 확인은 Simulation/DryRun 조건으로 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", expected=" + (expected ? "ON" : "OFF") +
                        ", description=" + description + " - Bypass");
                    return 0;
                }

                int timeoutMs = ResolvePickerIoTimeoutMs(pickerNo);
                DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
                bool actual = ReadPickerFlowState(pickerNo);

                while (DateTime.Now <= deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    actual = ReadPickerFlowState(pickerNo);
                    if (actual == expected)
                        return 0;

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                actual = ReadPickerFlowState(pickerNo);
                return Fail("PICKER-FLOW-CHECK", Name,
                    "Flow Check 확인 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", expected=" + (expected ? "ON" : "OFF") +
                    ", actual=" + (actual ? "ON" : "OFF") +
                    ", timeoutMs=" + timeoutMs +
                    ", description=" + description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-FLOW-CHECK-EX", Name,
                    "Flow Check 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", expected=" + (expected ? "ON" : "OFF") +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected int ResolvePickerIoTimeoutMs(int pickerNo)
        {
            try
            {
                if (Side == PickerSequenceSide.Front && FrontPicker != null)
                    return FrontPicker.ResolvePickerIoTimeoutMs(pickerNo);

                if (Side == PickerSequenceSide.Rear && RearPicker != null)
                    return RearPicker.ResolvePickerIoTimeoutMs(pickerNo);
            }
            catch (Exception ex)
            {
                WriteLog("PickerFlowCheck",
                    Name + " Picker I/O timeout 조회 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            return 5000;
        }

        protected bool ReadPickerFlowState(int pickerNo)
        {
            try
            {
                int index = ToPickerIndex(pickerNo);
                if (Side == PickerSequenceSide.Front &&
                    FrontPicker != null &&
                    FrontPicker.FlowChecks != null &&
                    index >= 0 &&
                    index < FrontPicker.FlowChecks.Length &&
                    FrontPicker.FlowChecks[index] != null)
                {
                    return FrontPicker.FlowChecks[index].IsOn;
                }

                if (Side == PickerSequenceSide.Rear &&
                    RearPicker != null &&
                    RearPicker.FlowChecks != null &&
                    index >= 0 &&
                    index < RearPicker.FlowChecks.Length &&
                    RearPicker.FlowChecks[index] != null)
                {
                    return RearPicker.FlowChecks[index].IsOn;
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerFlowCheck",
                    Name + " Picker Flow 신호 읽기 실패. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            return false;
        }

        protected Task<int> MovePickerAxisCommandAsync(PickerAxis axis, double target, string targetName = null, bool forceMove = false)
        {
            if (CalibrationMotion != null)
            {
                CalibrationMotion.EnsureDefaults();
                if (Side == PickerSequenceSide.Front)
                    return FrontPicker.MovePickerAxisCommandWithMotion(
                        axis,
                        target,
                        CalibrationMotion.MoveVelocity,
                        CalibrationMotion.MoveAcceleration,
                        CalibrationMotion.MoveDeceleration,
                        targetName,
                        forceMove);

                return RearPicker.MovePickerAxisCommandWithMotion(
                    axis,
                    target,
                    CalibrationMotion.MoveVelocity,
                    CalibrationMotion.MoveAcceleration,
                    CalibrationMotion.MoveDeceleration,
                    targetName,
                    forceMove);
            }

            bool fine = Options != null && Options.FineMove;
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommand(axis, target, fine, targetName, forceMove);
            return RearPicker.MovePickerAxisCommand(axis, target, fine, targetName, forceMove);
        }

        protected Task<int> MovePickerAxisCommandWithVelocityAsync(PickerAxis axis, double target, double velocity, string targetName = null)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommandWithVelocity(axis, target, velocity, targetName);
            return RearPicker.MovePickerAxisCommandWithVelocity(axis, target, velocity, targetName);
        }

        protected Task<int> MovePickerAxisCommandWithMotionAsync(PickerAxis axis, double target, double velocity, double acceleration, double deceleration, string targetName = null)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker.MovePickerAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration, targetName);
            return RearPicker.MovePickerAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration, targetName);
        }

        protected async Task<AxisMoveWaitResult> WaitPickerAxisMoveDoneAsync(PickerAxis axis, double target, int timeoutMs, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Side == PickerSequenceSide.Front)
                    return await FrontPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs, ct).ConfigureAwait(false);

                return await RearPicker.WaitPickerAxisMoveDoneInPosition(axis, target, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerMove",
                    Name + " picker axis wait exception. axis=" + axis +
                    ", target=" + target +
                    ", error=" + ex.Message + " - Failed");
                return new AxisMoveWaitResult(
                    AxisMoveWaitFailure.Timeout,
                    "Picker axis wait exception.",
                    "axis=" + axis + ", target=" + target + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        protected async Task DelayBeforeVisionInspectionAsync(CancellationToken ct)
        {
            if (ShouldSkipVisionInspectionDelay())
                return;

            int delayMs = ResolveVisionInspectionSettleMs();
            if (delayMs > 0)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        protected async Task DelaySideInspectionTurnSettleAsync(CancellationToken ct)
        {
            if (ShouldSkipVisionInspectionDelay())
                return;

            int delayMs = ResolveSideInspectionTurnSettleMs();
            if (delayMs > 0)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        private bool ShouldSkipVisionInspectionDelay()
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && !settings.UseVision)
                return true;

            return false;
        }

        protected bool IsDryRunWithVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode && !settings.UseVision;
        }

        private int ResolveVisionInspectionSettleMs()
        {
            int value = 0;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                value = FrontPicker.Config.VisionInspectionSettleMs;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                value = RearPicker.Config.VisionInspectionSettleMs;

            return value > 0 ? value : 0;
        }

        private int ResolveSideInspectionTurnSettleMs()
        {
            int value = 0;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                value = FrontPicker.Config.SideInspectionTurnSettleMs;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                value = RearPicker.Config.SideInspectionTurnSettleMs;

            return value > 0 ? value : 0;
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

        protected string BuildPickerMoveCommandFailureMessage(
            PickerAxis axis,
            double target,
            string description,
            int result)
        {
            string message =
                TranslatePickerMoveDescription(description) +
                " 이동 명령 실패. 결과=" + result +
                ". " + BuildPickerAxisStateKorean(axis, target);

            string failureReason = BuildPickerLastMotionFailureReason(axis, result);
            if (!string.IsNullOrWhiteSpace(failureReason))
                message += ". " + failureReason;

            return message;
        }

        private string BuildPickerAxisStateKorean(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            if (item == null)
                return "축=" + axis + ", 목표위치=" + target + ", 상태=축을 찾을 수 없음";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.001;

            return "축=" + axis +
                   ", 축이름=" + item.Name +
                   ", 서보=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", 알람=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", 이동중=" + (item.IsMoving ? "Y" : "N") +
                   ", 현재위치=" + item.ActualPosition +
                   ", 목표위치=" + target +
                   ", 허용오차=" + tolerance;
        }

        private string BuildPickerLastMotionFailureReason(PickerAxis axis, int result)
        {
            BaseAxis item = GetPickerAxis(axis);
            string lastFailure = item != null ? item.LastMotionFailureMessage : string.Empty;

            if (!string.IsNullOrWhiteSpace(lastFailure))
                return TranslateMotionFailureReason(lastFailure);

            if (result == -11)
            {
                if (axis == PickerAxis.PickerX)
                {
                    return "상세 원인=인터락 차단입니다. PickerX는 공유 X 레일 축이라 이동 목표 위치 또는 이동 경로에서 " +
                           "InputVisionX, OutputVisionX, FrontPickerX, RearPickerX 중 다른 축과 안전거리가 부족하면 이동이 차단됩니다. " +
                           "방해 축을 Avoid 위치로 이동하거나 Side/Bottom/Place 티칭 위치와 공유 레일 안전거리 설정을 확인하세요.";
                }

                return "상세 원인=인터락 차단입니다. 해당 축의 이동 조건, 상대 축 위치, 실린더 상태를 확인하세요.";
            }

            if (result == -2)
                return "상세 원인=축 이동 준비 조건이 맞지 않습니다. 서보 ON, 알람 OFF, 이동중 여부를 확인하세요.";

            return string.Empty;
        }

        private static string TranslateMotionFailureReason(string failure)
        {
            if (string.IsNullOrWhiteSpace(failure))
                return string.Empty;

            if (failure.IndexOf("SharedRailX pair clearance is too close", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "상세 원인=공유 X 레일 안전거리 인터락입니다. 이동 목표 위치에서 다른 X축과 안전거리가 부족하거나 경로가 겹칩니다. " +
                       "InputVisionX, OutputVisionX, FrontPickerX, RearPickerX 위치를 확인하세요. 원문=" + failure;
            }

            if (failure.IndexOf("SharedRailX real-time clearance guard stopped motion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "상세 원인=공유 X 레일 실시간 안전거리 인터락입니다. 이동 시작/목표 위치가 가능해 보여도 이동 중 다른 X축과 안전거리가 부족해 정지했습니다. " +
                       "동시에 움직이는 OutputVisionX/InputVisionX 또는 반대 PickerX 시퀀스 점유 상태를 확인하세요. 원문=" + failure;
            }

            if (failure.IndexOf("Interlock blocked", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=인터락 차단입니다. 원문=" + failure;

            if (failure.IndexOf("Servo is OFF", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=서보가 OFF 상태입니다. 원문=" + failure;

            if (failure.IndexOf("Axis alarm is ON", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=축 알람이 ON 상태입니다. 원문=" + failure;

            if (failure.IndexOf("Axis is already moving", StringComparison.OrdinalIgnoreCase) >= 0)
                return "상세 원인=축이 이미 이동 중입니다. 원문=" + failure;

            return "상세 원인=" + failure;
        }

        private static string TranslatePickerMoveDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "Picker 축";

            if (description.IndexOf("side inspection X", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 X축";
            if (description.IndexOf("side inspection Y", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 Y축";
            if (description.IndexOf("side inspection Z", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 Z축";
            if (description.IndexOf("side inspection T", StringComparison.OrdinalIgnoreCase) >= 0)
                return "사이드 검사 T축";
            if (description.IndexOf("bottom inspection X", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 X축";
            if (description.IndexOf("bottom inspection Y", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 Y축";
            if (description.IndexOf("bottom inspection Z", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 Z축";
            if (description.IndexOf("bottom inspection T", StringComparison.OrdinalIgnoreCase) >= 0)
                return "바텀 검사 T축";
            if (description.IndexOf("pick", StringComparison.OrdinalIgnoreCase) >= 0)
                return "픽업 " + description;
            if (description.IndexOf("place", StringComparison.OrdinalIgnoreCase) >= 0)
                return "플레이스 " + description;

            return description;
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
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).X;
        }

        protected double ResolvePickerZoneY(string positionArrayName, int pickerIndex)
        {
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).Y;
        }

        protected double ResolvePickerZoneT(string positionArrayName, int pickerIndex)
        {
            return ResolvePickerZoneTarget(positionArrayName, pickerIndex).T;
        }

        protected PickerCalibratedZoneTarget ResolvePickerZoneTarget(string positionArrayName, int pickerIndex)
        {
            return CalibrationCoordinateService.ResolvePickerZoneTarget(
                Context != null ? Context.Machine : null,
                ResolvePickerSide(),
                positionArrayName,
                pickerIndex,
                ResolvePickerRuntimeOffset(pickerIndex),
                true,
                true);
        }

        protected string ResolveZonePositionName(string positionArrayName)
        {
            return CalibrationCoordinateService.ResolveZonePositionName(positionArrayName);
        }

        protected PickerAxis GetPickerZAxis(int index)
        {
            return CalibrationCoordinateService.ResolvePickerZAxis(index);
        }

        protected PickerAxis GetPickerTAxis(int index)
        {
            return CalibrationCoordinateService.ResolvePickerTAxis(index);
        }

        protected double ResolvePickerAlignOffsetX(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(index);
            return (offset != null ? offset.AlignOffsetX : 0.0) + calibration.X;
        }

        protected double ResolvePickerAlignOffsetY(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(index);
            return (offset != null ? offset.AlignOffsetY : 0.0) + calibration.Y;
        }

        protected double ResolvePickerAlignOffsetT(int index)
        {
            PickerAlignOffset offset = ResolvePickerRuntimeOffset(index);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(index);
            return (offset != null ? offset.AlignOffsetT : 0.0) + calibration.T;
        }

        private PickerCalibrationOffset ResolvePickerCalibrationOffset(int index)
        {
            return CalibrationCoordinateService.ResolvePickerCalibrationOffset(
                Context != null ? Context.Machine : null,
                ResolvePickerSide(),
                index);
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

        protected bool TryResolveOutputVisionToPickerOffsets(BinSide outputSide, int index, out double offsetX, out double offsetY, out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                Context != null ? Context.Machine : null,
                Side,
                index,
                outputSide,
                out offsetX,
                out offsetY,
                out reason);
        }

        private PickerAlignOffset ResolvePickerRuntimeOffset(int index)
        {
            if (index < 0)
                return null;

            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.GetRuntimePickerOffset(index);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.GetRuntimePickerOffset(index);

            return null;
        }

        private VisionFocusPickerSide ResolvePickerSide()
        {
            return Side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
        }

        protected InspectionAlignmentSnapshot BuildPickerAlignmentSnapshot(
            string name,
            int pickerIndex,
            double targetX,
            double targetY,
            double targetT,
            double targetZ,
            VisionOffset offset)
        {
            try
            {
                BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
                BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(pickerIndex));
                BaseAxis zAxis = GetPickerAxis(GetPickerZAxis(pickerIndex));

                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = xAxis != null ? xAxis.ActualPosition : targetX,
                    Y = yAxis != null ? yAxis.ActualPosition : targetY,
                    T = tAxis != null ? tAxis.ActualPosition : targetT,
                    Z = zAxis != null ? zAxis.ActualPosition : targetZ,
                    XAxisName = xAxis != null ? xAxis.Name : "PickerX",
                    YAxisName = yAxis != null ? yAxis.Name : "PickerY",
                    TAxisName = tAxis != null ? tAxis.Name : GetPickerTAxis(pickerIndex).ToString(),
                    ZAxisName = zAxis != null ? zAxis.Name : GetPickerZAxis(pickerIndex).ToString(),
                    Offset = offset ?? new VisionOffset(),
                    IsValid = true
                };
            }
            catch (Exception ex)
            {
                WriteLog("PickerInspectionData",
                    Name + " picker alignment snapshot failed. snapshot=" +
                    name + ", error=" + ex.Message + " - Failed");
                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = targetX,
                    Y = targetY,
                    T = targetT,
                    Z = targetZ,
                    Offset = offset ?? new VisionOffset(),
                    IsValid = false
                };
            }
            finally
            {
            }
        }

        protected InspectionAlignmentSnapshot BuildInputStageAlignmentSnapshot(
            InputStageUnit stage,
            string name,
            VisionOffset offset)
        {
            try
            {
                BaseAxis xAxis = stage != null ? stage.CameraX : null;
                BaseAxis yAxis = stage != null ? stage.StageY : null;
                BaseAxis tAxis = stage != null ? stage.StageT : null;
                BaseAxis zAxis = stage != null ? stage.ExpanderZ : null;

                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    X = xAxis != null ? xAxis.ActualPosition : 0.0,
                    Y = yAxis != null ? yAxis.ActualPosition : 0.0,
                    T = tAxis != null ? tAxis.ActualPosition : 0.0,
                    Z = zAxis != null ? zAxis.ActualPosition : 0.0,
                    XAxisName = xAxis != null ? xAxis.Name : "InputVisionX",
                    YAxisName = yAxis != null ? yAxis.Name : "InputStageY",
                    TAxisName = tAxis != null ? tAxis.Name : "InputStageT",
                    ZAxisName = zAxis != null ? zAxis.Name : "InputExpandingZ",
                    Offset = offset ?? new VisionOffset(),
                    IsValid = stage != null
                };
            }
            catch (Exception ex)
            {
                WriteLog("PickerInspectionData",
                    Name + " input alignment snapshot failed. snapshot=" +
                    name + ", error=" + ex.Message + " - Failed");
                return new InspectionAlignmentSnapshot
                {
                    Name = name ?? "",
                    Offset = offset ?? new VisionOffset(),
                    IsValid = false
                };
            }
            finally
            {
            }
        }

        protected static InspectionMeasurement BuildMeasurement(
            string name,
            double value,
            string unit,
            MaterialInspectionResult result,
            string rawValue = "")
        {
            return new InspectionMeasurement
            {
                Name = name ?? "",
                Value = value,
                Unit = unit ?? "",
                RawValue = rawValue ?? "",
                Result = result
            };
        }

        protected static InspectionMeasurement BuildBooleanMeasurement(string name, bool ok)
        {
            return new InspectionMeasurement
            {
                Name = name ?? "",
                Value = ok ? 1.0 : 0.0,
                Unit = "bool",
                RawValue = ok ? "OK" : "NG",
                Result = ok ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
            };
        }

        protected async Task<SequenceResourceLease> AcquireResourceAsync(
            SequenceResourceKind resource,
            string holder,
            CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? Name : holder;
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    SequenceResourceLease manualLease = await Context.Resources
                        .AcquireAsync(resource, safeHolder, ResolveResourceTimeout(), ct)
                        .ConfigureAwait(false);
                    if (manualLease == null)
                    {
                        Fail("PICKER-RESOURCE", safeHolder,
                            "리소스 점유 실패. resource=" + resource);
                    }

                    return manualLease;
                }

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + ".AcquireResource:" + resource);

                    SequenceResourceLease autoLease = await Context.Resources
                        .AcquireAsync(resource, safeHolder, 200, ct, false)
                        .ConfigureAwait(false);
                    if (autoLease != null)
                    {
                        if (waitLogged)
                        {
                            Context.LogPublic("[SEQ] " + Name + " 리소스 대기 완료. resource=" +
                                resource + ", holder=" + safeHolder);
                        }

                        return autoLease;
                    }

                    if (!waitLogged)
                    {
                        string currentHolder = Context.Resources.GetHolder(resource);
                        Context.LogPublic("[SEQ] " + Name + " 리소스 사용 대기 중입니다. resource=" +
                            resource + ", holder=" + safeHolder + ", current=" +
                            (string.IsNullOrWhiteSpace(currentHolder) ? "-" : currentHolder));
                        waitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("PICKER-RESOURCE", safeHolder,
                    "리소스 점유 중 예외가 발생했습니다. resource=" + resource + ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        protected int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    Context.LogPublic("[" + Name + "] STOP " + message);
                    throw new SequenceStopException(message);
                }

                SequenceFailureStore.Record(Name, Kind.ToString(), CurrentStep.ToString(), alarmCode, source, message);
                WriteLog(source, message + " - Failed");
                if (IsAlarmStopActive())
                {
                    WriteLog(source,
                        "이미 활성 알람이 있어 피커 후속 알람 발생을 생략합니다. code=" +
                        alarmCode + ", message=" + message + " - Suppressed");
                }
                else
                {
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                }
                Context.LogPublic("[" + Name + "] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
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

        private bool IsAlarmStopActive()
        {
            try
            {
                if (AlarmManager.HasActive)
                    return true;

                return Context != null &&
                       Context.Controller != null &&
                       Context.Controller.Status == EquipmentStatus.Alarm;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int StopPickerMoveBecauseAlarmActive(string description)
        {
            WriteLog("PickerMove",
                Name + " " + description +
                " 이동 명령을 중단합니다. 이미 활성 알람 상태입니다. - Stopped");
            return -1;
        }

        private sealed class PickerMoveAxisLogDetail
        {
            public PickerAxis Axis { get; set; }
            public double Start { get; set; }
            public double Target { get; set; }
            public double Distance { get; set; }
            public double Velocity { get; set; }
            public double Acceleration { get; set; }
            public double Deceleration { get; set; }
        }

        private PickerMoveAxisLogDetail BuildPickerMoveAxisLogDetail(PickerAxis axis, double target)
        {
            BaseAxis item = GetPickerAxis(axis);
            double start = item != null ? item.ActualPosition : 0.0;
            double velocity = ResolvePickerMoveLogVelocity(item);
            double acceleration = ResolvePickerMoveLogAcceleration(item);
            double deceleration = ResolvePickerMoveLogDeceleration(item);

            return new PickerMoveAxisLogDetail
            {
                Axis = axis,
                Start = start,
                Target = target,
                Distance = Math.Abs(target - start),
                Velocity = velocity,
                Acceleration = acceleration,
                Deceleration = deceleration
            };
        }

        private double ResolvePickerMoveLogVelocity(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine && axis.Config.JogFineVelocity > 0.0)
                return axis.Config.JogFineVelocity;

            return MotionSpeedScale.ApplyDefaultVelocityScale(axis.Config.DefaultVelocity);
        }

        private double ResolvePickerMoveLogAcceleration(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine)
                return axis.Config.Acceleration;

            return MotionSpeedScale.ApplyDefaultAccelerationScale(axis.Config.Acceleration);
        }

        private double ResolvePickerMoveLogDeceleration(BaseAxis axis)
        {
            if (axis == null || axis.Config == null)
                return 0.0;

            bool fine = Options != null && Options.FineMove;
            if (fine)
                return axis.Config.Deceleration;

            return MotionSpeedScale.ApplyDefaultAccelerationScale(axis.Config.Deceleration);
        }

        private static string FormatPickerMoveAxisLogDetail(PickerMoveAxisLogDetail detail)
        {
            if (detail == null)
                return string.Empty;

            return detail.Axis +
                "(start=" + detail.Start.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", target=" + detail.Target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", distance=" + detail.Distance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", velocity=" + detail.Velocity.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", acc=" + detail.Acceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", dec=" + detail.Deceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ")";
        }

        private static string FormatPickerMoveAxisLogDetails(IList<PickerMoveAxisLogDetail> details)
        {
            if (details == null || details.Count == 0)
                return string.Empty;

            List<string> parts = new List<string>();
            for (int i = 0; i < details.Count; i++)
                parts.Add(FormatPickerMoveAxisLogDetail(details[i]));

            return string.Join("; ", parts);
        }

        private void WritePickerSequenceMoveElapsed(
            PickerMoveAxisLogDetail axisDetail,
            string targetName,
            string description,
            int result,
            long commandMs,
            long waitMs,
            long totalMs,
            AxisMoveWaitResult waitResult)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC",
                    "PK-MOVE-TIME",
                    Name + " " + (axisDetail != null ? axisDetail.Axis.ToString() : "-") +
                    " path=SequenceCommandVerify" +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "-") +
                    ", kind=" + Kind +
                    ", step=" + CurrentStep +
                    ", elapsedMs=" + totalMs +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", result=" + result +
                    ", wait=" + (waitResult != null ? waitResult.Failure.ToString() : "-") +
                    ", start=" + (axisDetail != null ? axisDetail.Start.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", target=" + (axisDetail != null ? axisDetail.Target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", distance=" + (axisDetail != null ? axisDetail.Distance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", description=" + (description ?? string.Empty) +
                    ", fine=" + (Options != null && Options.FineMove) +
                    ", velocity=" + (axisDetail != null ? axisDetail.Velocity.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", acc=" + (axisDetail != null ? axisDetail.Acceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-") +
                    ", dec=" + (axisDetail != null ? axisDetail.Deceleration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "-"));
            }
            catch
            {
            }
        }

        private void WritePickerSequenceGroupMoveElapsed(
            string targetName,
            string description,
            IList<PickerMoveAxisLogDetail> axisDetails,
            int result,
            long commandMs,
            long waitMs,
            long totalMs,
            string status)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Event,
                    "QMC",
                    "PK-MOVE-TIME",
                    Name + " group path=SequenceCommandVerify" +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "-") +
                    ", kind=" + Kind +
                    ", step=" + CurrentStep +
                    ", elapsedMs=" + totalMs +
                    ", commandMs=" + commandMs +
                    ", waitMs=" + waitMs +
                    ", result=" + result +
                    ", status=" + (status ?? string.Empty) +
                    ", targetName=" + (targetName ?? string.Empty) +
                    ", description=" + (description ?? string.Empty) +
                    ", fine=" + (Options != null && Options.FineMove) +
                    ", commandCount=" + (axisDetails != null ? axisDetails.Count : 0) +
                    ", axisDetails=" + FormatPickerMoveAxisLogDetails(axisDetails));
            }
            catch
            {
            }
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

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(Side == PickerSequenceSide.Front ? QMC.Common.Logging.EventKind.FrontHeadSeq : QMC.Common.Logging.EventKind.RearHeadSeq, source, message);
        }
    }
}
