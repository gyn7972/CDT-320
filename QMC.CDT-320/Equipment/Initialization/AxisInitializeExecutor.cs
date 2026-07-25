using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// AxisInitializePlan의 공통 구간과 Input/Output 병렬 Lane 실행을 관장합니다.
    /// 실제 Action과 HOME은 AxisInitializeRuntime을 통해 기존 실행 경로에 위임합니다.
    /// </summary>
    internal sealed class AxisInitializeExecutor
    {
        private readonly AxisInitializeRuntime _runtime;

        public AxisInitializeExecutor(AxisInitializeRuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public event Action<AxisInitializeStepProgress> StepProgressChanged;

        public void BeginRun(IEnumerable<AxisInitializeStep> steps)
        {
            _runtime.BeginRun();
            try
            {
                ResetStepProgressForRun(steps);
            }
            catch
            {
                _runtime.EndRun();
                throw;
            }
        }

        public void EndRun()
        {
            _runtime.EndRun();
        }

        public Task<AxisInitializeResult> ExecuteStepAsync(AxisInitializeStep step)
        {
            return ExecuteStepAsync(step, null, string.Empty);
        }

        public AxisInitializeResult VerifySingleAxisStep(
            AxisInitializeStep step,
            BaseAxis axis)
        {
            if (step == null || axis == null)
            {
                string invalidMessage = "개별축 HOME 검증 정보가 없습니다. axis=" +
                    (axis != null ? axis.Name : "-") +
                    ", step=" + (step != null ? step.StepNo : 0);
                return AxisInitializeResult.Failure(-1, step, string.Empty, invalidMessage);
            }

            string interlockReason;
            if (!_runtime.VerifyStep(step, out interlockReason))
            {
                string failureMessage = "개별축 HOME 초기화 계획 인터락 실패. axis=" + axis.Name +
                    ", step=" + step.StepNo + ", group=" + step.GroupName +
                    ", detail=" + interlockReason;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                    failureMessage + " - Failed");
                return AxisInitializeResult.Failure(-1, step, string.Empty, failureMessage);
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                "Single axis initialize plan interlock verified. axis=" + axis.Name +
                ", step=" + step.StepNo + ", group=" + step.GroupName +
                ", preActions=Skipped, postActions=Skipped - Ok");
            return AxisInitializeResult.Success();
        }

        public async Task<AxisInitializeResult> ExecuteSingleAxisHomeAsync(
            AxisInitializeStep step,
            BaseAxis axis)
        {
            bool runStarted = false;
            try
            {
                _runtime.BeginRun();
                runStarted = true;

                if (axis == null)
                {
                    const string invalidMessage = "초기화할 축 정보가 없습니다.";
                    return AxisInitializeResult.Failure(-1, step, string.Empty, invalidMessage);
                }

                int result = await _runtime.ExecuteSingleAxisHomeAsync(axis).ConfigureAwait(false);
                if (result == 0)
                    return AxisInitializeResult.Success();

                string failureMessage = _runtime.GetLastFailureMessage();
                if (string.IsNullOrWhiteSpace(failureMessage))
                {
                    failureMessage = "개별축 HOME 실행 실패. axis=" + axis.Name +
                        ", step=" + (step != null ? step.StepNo : 0) +
                        ", result=" + result;
                }

                return AxisInitializeResult.Failure(
                    result,
                    step,
                    string.Empty,
                    failureMessage);
            }
            catch (Exception ex)
            {
                string message = "개별축 HOME 실행 시작 실패. axis=" +
                    (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-AXIS-EXECUTOR",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(
                    -1,
                    step,
                    string.Empty,
                    message);
            }
            finally
            {
                if (runStarted)
                    _runtime.EndRun();
            }
        }

        public async Task<AxisInitializeResult> ExecuteAsync(IList<AxisInitializeStep> steps)
        {
            bool runStarted = false;
            try
            {
                if (steps == null || steps.Count == 0)
                {
                    const string message = "초기화 Step 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize failed: step list is empty. - Failed");
                    return AxisInitializeResult.Failure(-1, null, string.Empty, message);
                }

                var enabledSteps = steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ToList();

                AxisInitializeResult axisRegistrationResult =
                    VerifyDeclaredStepAxes(enabledSteps);
                if (!axisRegistrationResult.Succeeded)
                    return axisRegistrationResult;

                BeginRun(enabledSteps);
                runStarted = true;

                return await ExecutePlanWithParallelLanesAsync(enabledSteps).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = "초기화 Step 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    "Axis initialize step execution failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-STEP-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
            finally
            {
                if (runStarted)
                    EndRun();
            }
        }

        public AxisInitializeResult VerifyDeclaredStepAxes(
            IEnumerable<AxisInitializeStep> steps)
        {
            foreach (AxisInitializeStep step in steps ?? new AxisInitializeStep[0])
            {
                List<BaseAxis> axes;
                List<string> missingAxisNames;
                if (_runtime.TryResolveAxesByNames(
                    step != null ? step.AxisNames : null,
                    out axes,
                    out missingAxisNames))
                {
                    continue;
                }

                string message =
                    "초기화 Plan에 등록되지 않은 축이 포함되어 있어 실행을 시작할 수 없습니다. step=" +
                    (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", missing=" + string.Join(",", missingAxisNames.ToArray());
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PLAN-AXIS-NOTFOUND",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(
                    -1,
                    step,
                    string.Empty,
                    message);
            }

            return AxisInitializeResult.Success();
        }

        private async Task<AxisInitializeResult> ExecutePlanWithParallelLanesAsync(
            IList<AxisInitializeStep> enabledSteps)
        {
            try
            {
                var inputLaneSteps = enabledSteps
                    .Where(x => x != null && AxisInitializeParallelLane.Is(
                        x.ParallelLane,
                        AxisInitializeParallelLane.Input))
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .ToList();
                var outputLaneSteps = enabledSteps
                    .Where(x => x != null && AxisInitializeParallelLane.Is(
                        x.ParallelLane,
                        AxisInitializeParallelLane.Output))
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .ToList();

                if (inputLaneSteps.Count == 0 || outputLaneSteps.Count == 0)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Initialize parallel lane metadata is incomplete. Serial fallback selected. inputSteps=" +
                        inputLaneSteps.Count + ", outputSteps=" + outputLaneSteps.Count + " - Ok");
                    return await ExecuteStepBatchesSerialAsync(
                        enabledSteps,
                        "SerialFallback").ConfigureAwait(false);
                }

                int firstLaneStepNo = Math.Min(
                    inputLaneSteps.Min(x => x.StepNo),
                    outputLaneSteps.Min(x => x.StepNo));
                int lastLaneStepNo = Math.Max(
                    inputLaneSteps.Max(x => x.StepNo),
                    outputLaneSteps.Max(x => x.StepNo));
                var unlabeledStepsInsideLaneBarrier = enabledSteps
                    .Where(x => x != null &&
                        x.StepNo >= firstLaneStepNo &&
                        x.StepNo <= lastLaneStepNo &&
                        !AxisInitializeParallelLane.Is(x.ParallelLane, AxisInitializeParallelLane.Input) &&
                        !AxisInitializeParallelLane.Is(x.ParallelLane, AxisInitializeParallelLane.Output))
                    .ToList();
                if (unlabeledStepsInsideLaneBarrier.Count > 0)
                {
                    string message = "병렬 초기화 Lane 구간 안에 Lane이 지정되지 않은 Step이 있습니다. steps=" +
                        string.Join(",", unlabeledStepsInsideLaneBarrier.Select(x =>
                            x.StepNo + ":" + x.GroupName).ToArray());
                    return FailPreparation(unlabeledStepsInsideLaneBarrier[0], message);
                }

                var preLaneSteps = enabledSteps
                    .Where(x => x != null && x.StepNo < firstLaneStepNo)
                    .ToList();
                var postLaneSteps = enabledSteps
                    .Where(x => x != null && x.StepNo > lastLaneStepNo)
                    .ToList();

                AxisInitializeResult preResult = await ExecuteStepBatchesSerialAsync(
                    preLaneSteps,
                    "CommonPreLane").ConfigureAwait(false);
                if (!preResult.Succeeded)
                    return preResult;

                AxisInitializeResult parallelResult = await ExecuteParallelLanesAsync(
                    inputLaneSteps,
                    outputLaneSteps).ConfigureAwait(false);
                if (!parallelResult.Succeeded)
                    return parallelResult;

                return await ExecuteStepBatchesSerialAsync(
                    postLaneSteps,
                    "SharedRailPostLane").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = "병렬 초기화 플랜 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PARALLEL-PLAN-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
        }

        private async Task<AxisInitializeResult> ExecuteStepBatchesSerialAsync(
            IList<AxisInitializeStep> steps,
            string phase)
        {
            try
            {
                if (steps == null || steps.Count == 0)
                    return AxisInitializeResult.Success();

                foreach (var batch in steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .GroupBy(x => x.StepNo))
                {
                    var batchSteps = batch.OrderBy(x => x.GroupName).ToList();
                    if (batchSteps.Count == 1)
                    {
                        AxisInitializeResult singleResult = await ExecuteStepAsync(
                            batchSteps[0],
                            null,
                            string.Empty).ConfigureAwait(false);
                        if (!singleResult.Succeeded)
                            return singleResult;
                        continue;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize same-step serial batch start. phase=" + phase +
                        ", step=" + batch.Key +
                        ", groups=" + string.Join(",", batchSteps.Select(x => x.GroupName).ToArray()) + " - Start");
                    foreach (AxisInitializeStep batchStep in batchSteps)
                    {
                        AxisInitializeResult serialResult = await ExecuteStepAsync(
                            batchStep,
                            null,
                            string.Empty).ConfigureAwait(false);
                        if (!serialResult.Succeeded)
                            return serialResult;
                    }

                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize same-step serial batch completed. phase=" + phase +
                        ", step=" + batch.Key + " - Ok");
                }

                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string message = "직렬 초기화 구간 실행 실패: phase=" + phase +
                    ", error=" + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    message + " - Failed");
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
        }

        private async Task<AxisInitializeResult> ExecuteParallelLanesAsync(
            IList<AxisInitializeStep> inputLaneSteps,
            IList<AxisInitializeStep> outputLaneSteps)
        {
            ParallelLaneExecutionState executionState = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                HashSet<string> inputLaneAxisNames = _runtime.ResolveLaneAxisNames(inputLaneSteps);
                HashSet<string> outputLaneAxisNames = _runtime.ResolveLaneAxisNames(outputLaneSteps);
                var overlappingAxes = inputLaneAxisNames
                    .Intersect(outputLaneAxisNames, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (overlappingAxes.Count > 0)
                {
                    string message = "Input/Output 병렬 초기화 Lane에 중복 축이 있습니다. axes=" +
                        string.Join(",", overlappingAxes.ToArray());
                    return FailPreparation(null, message);
                }

                AxisInitializeStep firstInputStep = inputLaneSteps
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .FirstOrDefault();
                AxisInitializeStep firstOutputStep = outputLaneSteps
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .FirstOrDefault();
                string preflightReason;
                if (!_runtime.VerifyStep(firstInputStep, out preflightReason))
                {
                    RaiseStepProgress(
                        firstInputStep,
                        AxisInitializeStepStatus.Failed,
                        preflightReason);
                    return AxisInitializeResult.Failure(
                        -1,
                        firstInputStep,
                        AxisInitializeParallelLane.Input,
                        preflightReason);
                }

                if (!_runtime.VerifyStep(firstOutputStep, out preflightReason))
                {
                    RaiseStepProgress(
                        firstOutputStep,
                        AxisInitializeStepStatus.Failed,
                        preflightReason);
                    return AxisInitializeResult.Failure(
                        -1,
                        firstOutputStep,
                        AxisInitializeParallelLane.Output,
                        preflightReason);
                }

                executionState = new ParallelLaneExecutionState();
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Input/Output initialize lanes start concurrently. inputSteps=" +
                    string.Join(",", inputLaneSteps.Select(x => x.StepNo + ":" + x.GroupName).ToArray()) +
                    ", outputSteps=" +
                    string.Join(",", outputLaneSteps.Select(x => x.StepNo + ":" + x.GroupName).ToArray()) +
                    ", inputAxes=" + string.Join(",", inputLaneAxisNames.ToArray()) +
                    ", outputAxes=" + string.Join(",", outputLaneAxisNames.ToArray()) + " - Start");

                Task<AxisInitializeResult> inputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Input,
                    inputLaneSteps,
                    outputLaneAxisNames,
                    executionState));
                Task<AxisInitializeResult> outputTask = Task.Run(() => ExecuteLaneAsync(
                    AxisInitializeParallelLane.Output,
                    outputLaneSteps,
                    inputLaneAxisNames,
                    executionState));
                AxisInitializeResult[] results = await Task.WhenAll(inputTask, outputTask).ConfigureAwait(false);

                AxisInitializeResult failure = executionState.GetFailure();
                if (failure != null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Input/Output initialize lanes failed. firstFailureLane=" + failure.FailedLane +
                        ", step=" + failure.FailedStepNo +
                        ", group=" + failure.FailedGroup +
                        ", elapsedMs=" + stopwatch.ElapsedMilliseconds +
                        ", message=" + failure.ErrorMessage + " - Failed");
                    return failure;
                }

                AxisInitializeResult failedResult = results.FirstOrDefault(x => x != null && !x.Succeeded);
                if (failedResult != null)
                {
                    const string message = "병렬 초기화 Lane이 실패했지만 상세 실패 정보가 없습니다.";
                    return AxisInitializeResult.Failure(
                        failedResult.ResultCode,
                        null,
                        string.Empty,
                        message);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Input/Output initialize lanes completed concurrently. elapsedMs=" +
                    stopwatch.ElapsedMilliseconds + " - Ok");
                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                if (executionState != null)
                {
                    executionState.Cancel();
                    if (executionState.TryRequestAxisStop())
                        await _runtime.StopAllAxesAsync().ConfigureAwait(false);
                }

                string message = "병렬 초기화 Lane 실행 예외: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PARALLEL-LANE-EX",
                    "MachineController",
                    message);
                return AxisInitializeResult.Failure(-1, null, string.Empty, message);
            }
            finally
            {
                stopwatch.Stop();
                if (executionState != null)
                    executionState.Dispose();
            }
        }

        private async Task<AxisInitializeResult> ExecuteLaneAsync(
            string laneName,
            IList<AxisInitializeStep> laneSteps,
            ISet<string> allowedConcurrentAxisNames,
            ParallelLaneExecutionState executionState)
        {
            AxisInitializeStep currentStep = null;
            try
            {
                var orderedSteps = (laneSteps ?? new AxisInitializeStep[0])
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ThenBy(x => x.GroupName)
                    .ToList();
                for (int i = 0; i < orderedSteps.Count; i++)
                {
                    currentStep = orderedSteps[i];
                    if (executionState != null && executionState.Token.IsCancellationRequested)
                    {
                        string message = "반대 Lane 실패로 병렬 초기화가 중단되었습니다. lane=" + laneName;
                        MarkLaneStepsCancelled(orderedSteps, i, message);
                        return AxisInitializeResult.Failure(-1, currentStep, laneName, message);
                    }

                    AxisInitializeResult stepResult = await ExecuteStepAsync(
                        currentStep,
                        allowedConcurrentAxisNames,
                        laneName).ConfigureAwait(false);
                    if (stepResult.Succeeded &&
                        executionState != null &&
                        executionState.Token.IsCancellationRequested)
                    {
                        string reinitializeMessage =
                            "반대 Lane 실패 중 동작이 정지되었으므로 재초기화가 필요합니다. lane=" + laneName;
                        RaiseStepProgress(
                            currentStep,
                            AxisInitializeStepStatus.ReinitializeRequired,
                            reinitializeMessage);
                        string cancelledMessage =
                            "반대 Lane 실패로 병렬 초기화가 중단되었습니다. lane=" + laneName;
                        MarkLaneStepsCancelled(orderedSteps, i + 1, cancelledMessage);
                        return AxisInitializeResult.Failure(
                            -1,
                            currentStep,
                            laneName,
                            reinitializeMessage);
                    }

                    if (stepResult.Succeeded)
                        continue;

                    string failureMessage = ResolveParallelLaneFailureMessage(
                        stepResult,
                        currentStep,
                        laneName);
                    AxisInitializeResult failure = AxisInitializeResult.Failure(
                        stepResult.ResultCode,
                        currentStep,
                        laneName,
                        failureMessage);
                    await ReportParallelLaneFailureAsync(
                        executionState,
                        failure).ConfigureAwait(false);
                    return failure;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeLane",
                    "Initialize parallel lane completed. lane=" + laneName + " - Ok");
                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string failureMessage = "병렬 초기화 Lane 예외. lane=" + laneName +
                    ", step=" + (currentStep != null ? currentStep.StepNo : 0) +
                    ", group=" + (currentStep != null ? currentStep.GroupName : "-") +
                    ", error=" + ex.Message;
                AxisInitializeResult failure = AxisInitializeResult.Failure(
                    -1,
                    currentStep,
                    laneName,
                    failureMessage);
                await ReportParallelLaneFailureAsync(
                    executionState,
                    failure).ConfigureAwait(false);
                return failure;
            }
        }

        private async Task ReportParallelLaneFailureAsync(
            ParallelLaneExecutionState executionState,
            AxisInitializeResult failure)
        {
            try
            {
                if (executionState == null || failure == null)
                    return;

                bool firstFailure = executionState.TrySetFailure(failure);
                executionState.Cancel();

                if (executionState.TryRequestAxisStop())
                {
                    int stopResult = await _runtime.StopAllAxesAsync().ConfigureAwait(false);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        "Parallel lane peer-stop requested. lane=" + failure.FailedLane +
                        ", step=" + failure.FailedStepNo +
                        ", stopResult=" + stopResult +
                        (stopResult == 0 ? " - Ok" : " - Failed"));
                }

                if (firstFailure)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                        failure.ErrorMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-PARALLEL-" +
                            (string.IsNullOrWhiteSpace(failure.FailedLane)
                                ? "LANE"
                                : failure.FailedLane.ToUpperInvariant()),
                        "MachineController",
                        failure.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Parallel lane failure handling failed. error=" + ex.Message + " - Failed");
            }
        }

        private async Task<AxisInitializeResult> ExecuteStepAsync(
            AxisInitializeStep step,
            ISet<string> allowedConcurrentAxisNames,
            string laneName)
        {
            try
            {
                if (step == null || !step.Enabled)
                    return AxisInitializeResult.Success();

                List<BaseAxis> axes;
                List<string> missingAxisNames;
                if (!_runtime.TryResolveAxesByNames(
                    step.AxisNames,
                    out axes,
                    out missingAxisNames))
                {
                    string missingMessage =
                        "초기화 Step에 등록되지 않은 축이 포함되어 있습니다. step=" +
                        step.StepNo + ", group=" + step.GroupName +
                        ", missing=" + string.Join(",", missingAxisNames.ToArray());
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                        missingMessage + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-STEP-AXIS-NOTFOUND",
                        "MachineController",
                        missingMessage);
                    return AxisInitializeResult.Failure(
                        -1,
                        step,
                        laneName,
                        missingMessage);
                }

                bool hasActions = _runtime.HasEnabledActions(step);
                if (axes.Count == 0 && !hasActions)
                {
                    string message = "초기화 Step에 유효한 축이 없습니다. step=" + step.StepNo +
                        ", group=" + step.GroupName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                        "Axis initialize step failed: no valid axes. step=" + step.StepNo +
                        ", group=" + step.GroupName + " - Failed");
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "INIT-STEP-EMPTY",
                        "MachineController",
                        message);
                    return AxisInitializeResult.Failure(-1, step, laneName, message);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step start. step=" + step.StepNo +
                    ", group=" + step.GroupName +
                    ", mode=" + step.RunMode +
                    ", parallelLane=" +
                        (string.IsNullOrWhiteSpace(step.ParallelLane) ? "-" : step.ParallelLane) +
                    ", interlockGroup=" + step.InterlockGroup +
                    ", axes=" + string.Join(",", axes.Select(x => x.Name).ToArray()) + " - Start");
                RaiseStepProgress(step, AxisInitializeStepStatus.Running, "");

                int prepareResult = await _runtime.PrepareStepAsync(step).ConfigureAwait(false);
                if (prepareResult != 0)
                {
                    AxisInitializeResult failure = CreateStepFailure(
                        prepareResult,
                        step,
                        laneName);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                string interlockReason;
                if (!_runtime.VerifyStep(
                    step,
                    allowedConcurrentAxisNames,
                    out interlockReason))
                {
                    AxisInitializeResult failure = AxisInitializeResult.Failure(
                        -1,
                        step,
                        laneName,
                        interlockReason);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                int stopInterlockGroupResult =
                    await _runtime.StopInterlockGroupAsync(step).ConfigureAwait(false);
                if (stopInterlockGroupResult != 0)
                {
                    AxisInitializeResult failure = CreateStepFailure(
                        stopInterlockGroupResult,
                        step,
                        laneName);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                int preActionResult = await _runtime.ExecuteActionsAsync(
                    step,
                    step.PreActions,
                    "PreActions").ConfigureAwait(false);
                if (preActionResult != 0)
                {
                    AxisInitializeResult failure = CreateStepFailure(
                        preActionResult,
                        step,
                        laneName);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                int result = 0;
                if (axes.Count > 0)
                {
                    if (_runtime.IsPickerYPairStep(step))
                    {
                        result = await _runtime.ExecutePickerYPairAsync(
                            step,
                            axes).ConfigureAwait(false);
                    }
                    else
                    {
                        result = AxisInitializeRunMode.IsParallel(step.RunMode)
                            ? await _runtime.ExecuteParallelHomeAsync(step, axes).ConfigureAwait(false)
                            : await _runtime.ExecuteSerialHomeAsync(step, axes).ConfigureAwait(false);
                    }
                }

                if (result != 0)
                {
                    AxisInitializeResult failure = CreateStepFailure(
                        result,
                        step,
                        laneName);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                int postActionResult = await _runtime.ExecuteActionsAsync(
                    step,
                    step.PostActions,
                    "PostActions").ConfigureAwait(false);
                if (postActionResult != 0)
                {
                    AxisInitializeResult failure = CreateStepFailure(
                        postActionResult,
                        step,
                        laneName);
                    RaiseStepProgress(step, AxisInitializeStepStatus.Failed, failure.ErrorMessage);
                    return failure;
                }

                // 제거 요망 함수.
                // 개별 축에서 문제된다고 판단. AxisInitializenPlan에 이동 함수 만들어서 사용.
                //int completeResult = await CompleteInitializeStepAsync(step).ConfigureAwait(false);
                //if (completeResult != 0)
                //{
                //    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                //    return completeResult;
                //}

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step completed. step=" + step.StepNo +
                    ", group=" + step.GroupName + " - Ok");
                RaiseStepProgress(step, AxisInitializeStepStatus.Complete, "");
                return AxisInitializeResult.Success();
            }
            catch (Exception ex)
            {
                string message = "초기화 Step 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step execution failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-STEP-EX",
                    "MachineController",
                    message);
                RaiseStepProgress(step, AxisInitializeStepStatus.Failed, message);
                return AxisInitializeResult.Failure(-1, step, laneName, message);
            }
        }

        private AxisInitializeResult CreateStepFailure(
            int resultCode,
            AxisInitializeStep step,
            string laneName)
        {
            string message = _runtime.GetLastFailureMessage();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "초기화 Step 실행 실패. step=" +
                    (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "-") +
                    ", result=" + resultCode;
            }

            return AxisInitializeResult.Failure(resultCode, step, laneName, message);
        }

        private string ResolveParallelLaneFailureMessage(
            AxisInitializeResult stepResult,
            AxisInitializeStep step,
            string laneName)
        {
            try
            {
                string stepMessage = stepResult != null ? stepResult.ErrorMessage : "";
                if (string.IsNullOrWhiteSpace(stepMessage))
                    stepMessage = _runtime.GetLastFailureMessage();
                if (string.IsNullOrWhiteSpace(stepMessage))
                    stepMessage = "상세 실패 원인이 없습니다.";

                return "병렬 초기화 Lane 실패. lane=" + laneName +
                    ", step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "-") +
                    ", result=" + (stepResult != null ? stepResult.ResultCode : -1) +
                    ", reason=" + stepMessage;
            }
            catch (Exception ex)
            {
                return "병렬 초기화 Lane 실패 원인 확인 중 예외가 발생했습니다. lane=" + laneName +
                    ", error=" + ex.Message;
            }
        }

        private AxisInitializeResult FailPreparation(
            AxisInitializeStep step,
            string message)
        {
            QMC.Common.Log.Write("Main", "SYSTEM", "InitializePreparation", message + " - Failed");
            AlarmManager.Raise(
                AlarmSeverity.Error,
                "INIT-PREP",
                "MachineController",
                message);
            return AxisInitializeResult.Failure(-1, step, string.Empty, message);
        }

        private void MarkLaneStepsCancelled(
            IList<AxisInitializeStep> orderedSteps,
            int startIndex,
            string message)
        {
            try
            {
                if (orderedSteps == null)
                    return;

                for (int i = Math.Max(0, startIndex); i < orderedSteps.Count; i++)
                {
                    RaiseStepProgress(
                        orderedSteps[i],
                        AxisInitializeStepStatus.ReinitializeRequired,
                        message);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeParallelLanes",
                    "Cancelled lane progress update failed. error=" + ex.Message + " - Failed");
            }
        }

        private void RaiseStepProgress(
            AxisInitializeStep step,
            string status,
            string message)
        {
            Action<AxisInitializeStepProgress> handler = StepProgressChanged;
            if (handler == null)
                return;

            try
            {
                handler(AxisInitializeStepProgress.Create(step, status, message));
            }
            catch
            {
            }
        }

        private void ResetStepProgressForRun(IEnumerable<AxisInitializeStep> steps)
        {
            if (steps == null)
                return;

            foreach (AxisInitializeStep step in steps.Where(x => x != null && x.Enabled))
            {
                RaiseStepProgress(
                    step,
                    AxisInitializeStepStatus.Waiting,
                    "");
            }
        }

        private sealed class ParallelLaneExecutionState : IDisposable
        {
            private readonly object _failureLock = new object();
            private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
            private AxisInitializeResult _failure;
            private int _axisStopRequested;

            public CancellationToken Token
            {
                get { return _cancellation.Token; }
            }

            public bool TrySetFailure(AxisInitializeResult failure)
            {
                try
                {
                    if (failure == null)
                        return false;

                    lock (_failureLock)
                    {
                        if (_failure != null)
                            return false;

                        _failure = failure;
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            public AxisInitializeResult GetFailure()
            {
                try
                {
                    lock (_failureLock)
                    {
                        return _failure;
                    }
                }
                catch
                {
                    return null;
                }
            }

            public void Cancel()
            {
                try
                {
                    if (!_cancellation.IsCancellationRequested)
                        _cancellation.Cancel();
                }
                catch
                {
                }
            }

            public bool TryRequestAxisStop()
            {
                return Interlocked.CompareExchange(ref _axisStopRequested, 1, 0) == 0;
            }

            public void Dispose()
            {
                try
                {
                    _cancellation.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
