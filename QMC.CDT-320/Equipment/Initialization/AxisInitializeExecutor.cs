using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 선택된 초기화 Step의 Prepare, Interlock, StopGroup, Action, HOME 실행을 관장합니다.
    /// 전체 Step 순서와 Input/Output 병렬 Lane 실행은 AxisInitializeSequence가 담당합니다.
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

        /// <summary>
        /// AxisInitializeSequence가 선택한 한 Step을 기존 안전 순서로 실행합니다.
        /// 병렬 Lane에서는 반대 Lane 축 이름만 동시 이동 허용 목록으로 전달됩니다.
        /// </summary>
        internal async Task<AxisInitializeResult> ExecuteStepAsync(
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

        internal void RaiseStepProgress(
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

    }
}
