using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.IO;
using QMC.CDT320.Ajin;
using QMC.CDT320.Alarms;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// AxisInitializeExecutor가 MachineController 전체에 접근하지 않도록
    /// 현재 단계에 필요한 초기화 기능만 명시적으로 제공합니다.
    /// </summary>
    internal sealed class AxisInitializeRuntime
    {
        private const int InitializeAxisStopWaitTimeoutMs = 5000;
        private const int InitializeAxisStopPollIntervalMs = 20;

        private readonly CDT320_Machine _machine;
        private readonly AxisInterferenceMap _axisInterferenceMap;
        private readonly Func<IEnumerable<BaseAxis>> _enumerateAxes;
        private readonly AxisInitializeInterlockService _interlockService;
        private readonly AxisInitializeRunState _runState = new AxisInitializeRunState();
        private readonly SemaphoreSlim _pickerYHomeGate = new SemaphoreSlim(1, 1);
        private string _lastFailureMessage = string.Empty;

        private sealed class PickerYHomeServoRestoreState
        {
            public BaseAxis TargetAxis { get; set; }
            public BaseAxis PairedAxis { get; set; }
            public bool RestoreServoOn { get; set; }
            public bool RestoreHomeDone { get; set; }
            public bool Restored { get; set; }
        }

        public AxisInitializeRuntime(
            CDT320_Machine machine,
            AxisInterferenceMap axisInterferenceMap,
            Func<IEnumerable<BaseAxis>> enumerateAxes,
            AxisInitializeInterlockService interlockService)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _axisInterferenceMap = axisInterferenceMap ??
                throw new ArgumentNullException(nameof(axisInterferenceMap));
            _enumerateAxes = enumerateAxes ?? throw new ArgumentNullException(nameof(enumerateAxes));
            _interlockService = interlockService ??
                throw new ArgumentNullException(nameof(interlockService));
        }

        public void BeginRun()
        {
            if (!_runState.TryBegin())
            {
                throw new InvalidOperationException(
                    "다른 축 초기화 작업이 이미 실행 중입니다.");
            }

            SetLastFailureMessage(string.Empty);
        }

        public void EndRun()
        {
            _runState.End();
        }

        public List<BaseAxis> ResolveAxesByNames(IEnumerable<string> axisNames)
        {
            List<BaseAxis> axes;
            List<string> missingAxisNames;
            TryResolveAxesByNames(axisNames, out axes, out missingAxisNames);
            return axes;
        }

        public bool TryResolveAxesByNames(
            IEnumerable<string> axisNames,
            out List<BaseAxis> axes,
            out List<string> missingAxisNames)
        {
            axes = new List<BaseAxis>();
            missingAxisNames = new List<string>();
            try
            {
                if (axisNames == null)
                    return true;

                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName) || !visited.Add(axisName.Trim()))
                        continue;

                    var axis = FindAxisByName(axisName);
                    if (axis == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByNames",
                            "Axis resolve failed: axis not found. axis=" + axisName + " - Failed");
                        missingAxisNames.Add(axisName.Trim());
                        continue;
                    }

                    axes.Add(axis);
                }

                return missingAxisNames.Count == 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByNames",
                    "Axis resolve failed: " + ex.Message + " - Failed");
                missingAxisNames.Add("축 목록 해석 예외: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        public HashSet<string> ResolveLaneAxisNames(IList<AxisInitializeStep> laneSteps)
        {
            return ResolveInitializeLaneAxisNames(laneSteps);
        }

        public bool HasEnabledActions(AxisInitializeStep step)
        {
            return HasEnabledInitializeActions(step);
        }

        public Task<int> PrepareStepAsync(AxisInitializeStep step)
        {
            // 현재 활성 초기화 경로의 Prepare 단계는 의도적으로 no-op입니다.
            // 과거 MachineController의 도달 불가능한 Prepare 코드를 이 경계에서 다시 활성화하지 않습니다.
            return Task.FromResult(0);
        }

        public bool VerifyStep(AxisInitializeStep step, out string reason)
        {
            return VerifyStep(step, null, out reason);
        }

        public bool VerifyStep(
            AxisInitializeStep step,
            ISet<string> allowedConcurrentAxisNames,
            out string reason)
        {
            return _interlockService.VerifyStep(
                step,
                allowedConcurrentAxisNames,
                out reason);
        }

        public Task<int> StopInterlockGroupAsync(AxisInitializeStep step)
        {
            return StopInitializeInterlockGroupAsync(step);
        }

        public Task<int> ExecuteActionsAsync(
            AxisInitializeStep step,
            IList<AxisInitializeAction> actions,
            string phase)
        {
            return ExecuteInitializeActionsAsync(step, actions, phase);
        }

        public bool IsPickerYPairStep(AxisInitializeStep step)
        {
            return step != null &&
                   string.Equals(step.GroupName, "PickerYPair", StringComparison.OrdinalIgnoreCase);
        }

        public Task<int> ExecutePickerYPairAsync(
            AxisInitializeStep step,
            IList<BaseAxis> axes)
        {
            return ExecutePickerYPairInitializeAsync(step, axes);
        }

        private HashSet<string> ResolveInitializeLaneAxisNames(IList<AxisInitializeStep> laneSteps)
        {
            var axisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (AxisInitializeStep step in laneSteps ?? new AxisInitializeStep[0])
                {
                    foreach (BaseAxis axis in ResolveAxesByNames(step != null ? step.AxisNames : null))
                    {
                        if (axis != null && !string.IsNullOrWhiteSpace(axis.Name))
                            axisNames.Add(axis.Name);
                    }

                    AddInitializeActionAxisNames(axisNames, step != null ? step.PreActions : null);
                    AddInitializeActionAxisNames(axisNames, step != null ? step.PostActions : null);
                }

                return axisNames;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveInitializeLaneAxes",
                    "Initialize lane axis resolve failed. error=" + ex.Message + " - Failed");
                return axisNames;
            }
            finally
            {
            }
        }

        private void AddInitializeActionAxisNames(
            ISet<string> axisNames,
            IList<AxisInitializeAction> actions)
        {
            try
            {
                if (axisNames == null || actions == null)
                    return;

                foreach (AxisInitializeAction action in actions)
                {
                    if (action == null || !action.Enabled ||
                        !string.Equals(
                            action.TargetType,
                            AxisInitializeInterlockTarget.Axis,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    BaseAxis axis = FindInitializeActionAxis(action.Name);
                    if (axis != null && !string.IsNullOrWhiteSpace(axis.Name))
                        axisNames.Add(axis.Name);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveInitializeLaneAxes",
                    "Initialize lane action axis resolve failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private bool HasEnabledInitializeActions(AxisInitializeStep step)
        {
            try
            {
                return HasEnabledInitializeActions(step != null ? step.PreActions : null) ||
                       HasEnabledInitializeActions(step != null ? step.PostActions : null);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool HasEnabledInitializeActions(IList<AxisInitializeAction> actions)
        {
            try
            {
                if (actions == null)
                    return false;

                return actions.Any(x => x != null && x.Enabled);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private BaseAxis FindAxisByName(string axisName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(axisName))
                    return null;

                foreach (var axis in _enumerateAxes())
                {
                    if (string.Equals(axis.Name, axisName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return axis;
                }

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

        private List<BaseAxis> ResolveAxesByGroup(string groupName)
        {
            var axes = new List<BaseAxis>();
            try
            {
                if (string.IsNullOrWhiteSpace(groupName))
                    return axes;

                foreach (var axis in _enumerateAxes())
                {
                    string unitName = axis.Setup != null ? axis.Setup.UnitName : "";
                    if (string.Equals(unitName, groupName.Trim(), StringComparison.OrdinalIgnoreCase))
                        axes.Add(axis);
                }

                return axes;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByGroup",
                    "Resolve axes by group failed. group=" + groupName + ", error=" + ex.Message + " - Failed");
                return axes;
            }
            finally
            {
            }
        }

        private async Task<int> StopInitializeInterlockGroupAsync(AxisInitializeStep step)
        {
            try
            {
                if (step == null || string.IsNullOrWhiteSpace(step.InterlockGroup))
                    return 0;

                var axes = ResolveAxesByGroup(step.InterlockGroup)
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                string resolutionSource = "UnitName";

                if (axes.Count == 0)
                {
                    IReadOnlyList<string> registeredInterferenceAxes;
                    bool hasRegisteredInterferenceGroup =
                        _axisInterferenceMap.TryResolveRegisteredInterferenceAxes(
                            step.InterlockGroup,
                            out registeredInterferenceAxes);
                    var mappedAxisNames = (registeredInterferenceAxes ?? new string[0])
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var mappedAxes = new List<string>();
                    var unresolvedMappedAxes = new List<string>();

                    foreach (string mappedAxisName in mappedAxisNames)
                    {
                        BaseAxis mappedAxis = FindAxisByName(mappedAxisName);
                        if (mappedAxis != null)
                            mappedAxes.Add(mappedAxis.Name);
                        else
                            unresolvedMappedAxes.Add(mappedAxisName);
                    }

                    if (hasRegisteredInterferenceGroup)
                    {
                        if (unresolvedMappedAxes.Count > 0)
                        {
                            return FailInitializeAxisStop(
                                "초기화 Step 간섭 그룹 축 해석. step=" + step.StepNo +
                                ", group=" + step.GroupName +
                                ", interlockGroup=" + step.InterlockGroup,
                                "간섭맵에 등록되지 않은 축이 포함되어 있습니다. unresolved=" +
                                string.Join(",", unresolvedMappedAxes.ToArray()));
                        }

                        axes = mappedAxes;
                        resolutionSource = "InterferenceMap";
                    }
                    else
                    {
                        BaseAxis directAxis = FindAxisByName(step.InterlockGroup);
                        if (directAxis != null)
                        {
                            axes.Add(directAxis.Name);
                            resolutionSource = "InterferenceMap";
                        }
                    }
                }

                // InterlockGroup은 FrontPickerZ처럼 실제 UnitName/축명이 아닌 논리 그룹명일 수 있다.
                // 이 경우 초기화 계획이 보유한 AxisNames를 실제 등록 축으로 재검증해 정지한다.
                if (axes.Count == 0)
                {
                    var stepAxisNames = (step.AxisNames ?? new List<string>())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var resolvedStepAxes = new List<string>();
                    var unresolvedStepAxes = new List<string>();

                    foreach (string stepAxisName in stepAxisNames)
                    {
                        BaseAxis stepAxis = FindAxisByName(stepAxisName);
                        if (stepAxis != null)
                            resolvedStepAxes.Add(stepAxis.Name);
                        else
                            unresolvedStepAxes.Add(stepAxisName);
                    }

                    if (unresolvedStepAxes.Count > 0)
                    {
                        return FailInitializeAxisStop(
                            "초기화 Step 간섭 그룹 축 해석. step=" + step.StepNo +
                            ", group=" + step.GroupName +
                            ", interlockGroup=" + step.InterlockGroup,
                            "초기화 계획의 축이 현재 장비에 등록되어 있지 않습니다. unresolved=" +
                            string.Join(",", unresolvedStepAxes.ToArray()));
                    }

                    axes = resolvedStepAxes;
                    resolutionSource = "StepAxisNames";
                }

                if (axes.Count == 0)
                {
                    return FailInitializeAxisStop(
                        "초기화 Step 간섭 그룹 축 해석. step=" + step.StepNo +
                        ", group=" + step.GroupName +
                        ", interlockGroup=" + step.InterlockGroup,
                        "정지할 실제 등록 축을 찾지 못했습니다.");
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "StopInitializeInterlockGroup",
                    "Initialize interlock group stop requested. step=" + step.StepNo +
                    ", interlockGroup=" + step.InterlockGroup +
                    ", resolutionSource=" + resolutionSource +
                    ", axes=" + string.Join(",", axes.ToArray()) + " - Start");

                return await StopAxesAndWaitUntilStoppedAsync(
                    axes,
                    false,
                    "초기화 Step 간섭 그룹 정지. step=" + step.StepNo +
                    ", group=" + step.GroupName +
                    ", interlockGroup=" + step.InterlockGroup).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = "초기화 Step 간섭 그룹 정지 실패. step=" +
                    (step != null ? step.StepNo : 0) + ", group=" +
                    (step != null ? step.GroupName : "-") + ", error=" + ex.Message;
                SetLastFailureMessage(message);
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInitializeInterlockGroup",
                    message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private int FailInitializeAxisStop(string context, string detail)
        {
            string message = "축 초기화 정지 확인 실패. context=" + context + ", detail=" + detail;
            SetLastFailureMessage(message);
            QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisStop",
                message + " - Failed");
            AlarmManager.Raise(
                AlarmSeverity.Error,
                "INIT-AXIS-STOP",
                "MachineController",
                message);
            return -1;
        }

        private async Task<int> StopAxesAndWaitUntilStoppedAsync(
            IEnumerable<string> axisNames,
            bool emergencyStop,
            string context)
        {
            try
            {
                var names = (axisNames ?? Enumerable.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (names.Count == 0)
                    return 0;

                int stopResult = await StopAxesAsync(names, emergencyStop).ConfigureAwait(false);
                if (stopResult != 0)
                {
                    return FailInitializeAxisStop(
                        context,
                        "Stop 요청 실패. axes=" + string.Join(",", names.ToArray()) +
                        ", result=" + stopResult);
                }

                var axes = new List<BaseAxis>();
                foreach (string name in names)
                {
                    BaseAxis axis = FindAxisByName(name);
                    if (axis == null)
                    {
                        return FailInitializeAxisStop(
                            context,
                            "정지 확인 축을 찾을 수 없습니다. axis=" + name);
                    }

                    axes.Add(axis);
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                while (true)
                {
                    var movingStates = new List<string>();
                    foreach (BaseAxis axis in axes)
                    {
                        try
                        {
                            axis.UpdateStatus();
                        }
                        catch (Exception statusEx)
                        {
                            return FailInitializeAxisStop(
                                context,
                                "정지 상태 갱신 실패. axis=" + axis.Name +
                                ", error=" + statusEx.Message);
                        }

                        if (axis.IsMoving)
                            movingStates.Add(BuildInitializeAxisStopState(axis));
                    }

                    if (movingStates.Count == 0)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisStop",
                            "Axis stop verified. context=" + context +
                            ", axes=" + string.Join(",", names.ToArray()) +
                            ", elapsedMs=" + stopwatch.ElapsedMilliseconds + " - Ok");
                        return 0;
                    }

                    if (stopwatch.ElapsedMilliseconds >= InitializeAxisStopWaitTimeoutMs)
                    {
                        return FailInitializeAxisStop(
                            context,
                            "Stop 후 정지 확인 시간 초과. timeoutMs=" +
                            InitializeAxisStopWaitTimeoutMs +
                            ", movingAxes=" + string.Join(" | ", movingStates.ToArray()));
                    }

                    await Task.Delay(InitializeAxisStopPollIntervalMs).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return FailInitializeAxisStop(
                    context,
                    "Stop 및 정지 확인 중 예외. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> StopAxesAsync(
            IEnumerable<string> axisNames,
            bool emergencyStop)
        {
            try
            {
                int failed = 0;
                var normalizedAxisNames = (axisNames ?? Enumerable.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (string axisName in normalizedAxisNames)
                {
                    BaseAxis axis = FindAxisByName(axisName);
                    if (axis == null)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed: axis not found. axis=" + axisName + " - Failed");
                        continue;
                    }

                    try
                    {
                        if (emergencyStop)
                            axis.EStop();
                        else
                            axis.Stop();

                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stopped. axis=" + axis.Name +
                            ", emergency=" + emergencyStop + " - Ok");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed. axis=" + axis.Name +
                            ", error=" + ex.Message + " - Failed");
                    }
                }

                await Task.Yield();
                return failed == 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                    "Axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private static string BuildInitializeAxisStopState(BaseAxis axis)
        {
            if (axis == null)
                return "axis=null";

            return "axis=" + axis.Name +
                ", moving=" + axis.IsMoving +
                ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                ", actual=" + axis.ActualPosition.ToString(
                    "0.###",
                    System.Globalization.CultureInfo.InvariantCulture) +
                ", command=" + axis.CommandPosition.ToString(
                    "0.###",
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task<int> ExecuteInitializeActionsAsync(
            AxisInitializeStep step,
            IList<AxisInitializeAction> actions,
            string phase)
        {
            try
            {
                if (actions == null || actions.Count == 0)
                    return 0;

                foreach (var action in actions)
                {
                    if (action == null || !action.Enabled)
                        continue;

                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                        "Initialize action start. step=" + (step != null ? step.StepNo : 0) +
                        ", group=" + (step != null ? step.GroupName : "") +
                        ", phase=" + phase +
                        ", target=" + action.TargetType + ":" + action.Name +
                        ", command=" + action.Command + " - Start");

                    int result = await ExecuteInitializeActionAsync(step, action, phase).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                        "Initialize action completed. step=" + (step != null ? step.StepNo : 0) +
                        ", group=" + (step != null ? step.GroupName : "") +
                        ", phase=" + phase +
                        ", target=" + action.TargetType + ":" + action.Name +
                        ", command=" + action.Command + " - Ok");
                }

                return 0;
            }
            catch (Exception ex)
            {
                string message = "초기화 Action 실행 실패: " + ex.Message;
                SetLastFailureMessage(message);
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                    "Initialize action failed. phase=" + phase + ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-ACTION-EX", "MachineController", message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeActionAsync(
            AxisInitializeStep step,
            AxisInitializeAction action,
            string phase)
        {
            try
            {
                string targetType = action.TargetType ?? "";
                string command = action.Command ?? "";

                if (string.Equals(targetType, AxisInitializeInterlockTarget.Cylinder, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteInitializeCylinderActionAsync(action).ConfigureAwait(false);

                if (string.Equals(targetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(command, AxisInitializeActionCommand.AxisTeachingMove, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteInitializeAxisTeachingActionAsync(step, action).ConfigureAwait(false);

                if (string.Equals(command, AxisInitializeActionCommand.CustomHook, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteCustomInitializeActionAsync(step, action, phase).ConfigureAwait(false);

                return FailInitializePreparation("지원하지 않는 초기화 Action입니다. target=" +
                    targetType + ":" + action.Name + ", command=" + command);
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("초기화 Action 예외. target=" +
                    (action != null ? action.TargetType + ":" + action.Name : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeAxisTeachingActionAsync(
            AxisInitializeStep step,
            AxisInitializeAction action)
        {
            try
            {
                if (action == null)
                    return FailInitializePreparation("축 티칭 이동 Action 정보가 없습니다.");

                BaseAxis axis = FindInitializeActionAxis(action.Name);
                if (axis == null)
                    return FailInitializePreparation("초기화 축 티칭 이동 대상을 찾을 수 없습니다. axis=" + action.Name);

                double targetPosition;
                string targetName;
                if (!TryResolveInitializeAxisTeachingPosition(axis, action, out targetPosition, out targetName))
                {
                    return FailInitializePreparation("초기화 축 티칭 위치를 찾을 수 없습니다. axis=" +
                        action.Name + ", position=" + action.PositionName);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching action start. axis=" + axis.Name +
                    ", requested=" + action.Name +
                    ", position=" + targetName +
                    ", target=" + targetPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    " - Start");

                double explicitVelocity = 0.0;

                bool useJogCoarseVelocity =
                    step != null &&
                    step.StepNo == 300 &&
                    string.Equals(
                        step.GroupName,
                        "OutputNGStageYAvoid",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        action.Name,
                        "OutputNGStageY",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        action.PositionName,
                        "AvoidPosition",
                        StringComparison.OrdinalIgnoreCase);

                if (useJogCoarseVelocity)
                {
                    if (axis.Config == null || axis.Config.JogCoarseVelocity <= 0.0)
                    {
                        return FailInitializePreparation(
                            "OutputNGStageY 초기화 Avoid 이동의 JogCoarseVelocity가 올바르지 않습니다. velocity=" +
                            (axis.Config != null
                                ? axis.Config.JogCoarseVelocity.ToString(
                                    "0.###",
                                    System.Globalization.CultureInfo.InvariantCulture)
                                : "ConfigNull"));
                    }

                    explicitVelocity = axis.Config.JogCoarseVelocity;
                }

                int result = await MoveAxisTeachingAsync(
                    axis,
                    targetPosition,
                    targetName,
                    explicitVelocity).ConfigureAwait(false);

                if (result != 0)
                    return result;

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching action completed. axis=" + axis.Name +
                    ", position=" + targetName + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("축 티칭 이동 Action 예외. axis=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisTeachingAsync(
            BaseAxis axis,
            double targetPosition,
            string targetName,
            double explicitVelocity = 0.0)
        {
            try
            {
                if (axis == null)
                    return 0;

                if (!axis.IsHomeDone)
                    return -1;

                double moveVelocity = explicitVelocity > 0.0
                    ? explicitVelocity
                    : ResolveAxisDefaultVelocity(axis);

                using (MotionGuardRuntime.BeginAxisTeachingMove(
                    axis,
                    targetPosition,
                    targetName))
                {
                    int result = await SharedRailXMotionRuntime.MoveAxisAsync(
                        axis,
                        targetPosition,
                        moveVelocity).ConfigureAwait(false);
                    if (result != 0 || axis.IsAlarm)
                    {
                        string message = BuildAxisMotionFailureMessage(
                            axis,
                            "Avoid 이동 실패",
                            result);
                        return FailInitializePreparation(message);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation(
                    "Avoid move exception. axis=" +
                    (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static double ResolveAxisDefaultVelocity(BaseAxis axis)
        {
            return MotionSpeedScale.ApplyDefaultVelocityScale(
                axis != null && axis.Config != null && axis.Config.GetRawDefaultVelocity() > 0.0
                    ? axis.Config.GetRawDefaultVelocity()
                    : 5.0);
        }

        private async Task<int> ExecuteInitializeCylinderActionAsync(AxisInitializeAction action)
        {
            try
            {
                BaseCylinder cylinder = FindCylinderByName(action.Name);
                if (cylinder == null)
                    return FailInitializePreparation("초기화 실린더를 찾을 수 없습니다. cylinder=" + action.Name);

                string command = action.Command ?? "";
                if (string.Equals(command, AxisInitializeActionCommand.CylinderFwd, StringComparison.OrdinalIgnoreCase))
                {
                    bool ok;
                    using (MotionGuardRuntime.BeginCylinderInitializeMove(cylinder, true, command))
                    {
                        ok = await cylinder.MoveFwdAsync().ConfigureAwait(false);
                    }
                    return ok ? 0 : FailInitializePreparation("실린더 전진 실패. cylinder=" + cylinder.Name);
                }

                if (string.Equals(command, AxisInitializeActionCommand.CylinderBwd, StringComparison.OrdinalIgnoreCase))
                {
                    bool ok;
                    using (MotionGuardRuntime.BeginCylinderInitializeMove(cylinder, false, command))
                    {
                        ok = await cylinder.MoveBwdAsync().ConfigureAwait(false);
                    }
                    return ok ? 0 : FailInitializePreparation("실린더 후진 실패. cylinder=" + cylinder.Name);
                }

                return FailInitializePreparation("지원하지 않는 실린더 Action입니다. cylinder=" +
                    cylinder.Name + ", command=" + command);
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("실린더 Action 예외. cylinder=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private BaseAxis FindInitializeActionAxis(string axisName)
        {
            try
            {
                BaseAxis axis = FindAxisByName(axisName);
                if (axis != null)
                    return axis;

                string alias = ResolveInitializeAxisAlias(axisName);
                if (!string.IsNullOrWhiteSpace(alias))
                    return FindAxisByName(alias);

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

        private static string ResolveInitializeAxisAlias(string axisName)
        {
            if (string.IsNullOrWhiteSpace(axisName))
                return null;

            switch (axisName.Trim())
            {
                case "InputStageY":
                    return "StageY";
                case "InputStageT":
                    return "StageT";
                case "InputVisionX":
                    return "CameraX";
                case "OutputGoodStageY":
                    return "GoodStage_StageY";
                case "OutputGoodStageZ":
                    return "GoodStage_StageZ";
                case "GoodStage_StageZ":
                    return "OutputGoodStageZ";
                case "OutputNGStageY":
                    return "NgStage_StageY";
                case "NgStage_StageY":
                    return "OutputNGStageY";
                case "GoodStage_StageY":
                    return "OutputGoodStageY";
                default:
                    return null;
            }
        }

        private bool TryResolveInitializeAxisTeachingPosition(
            BaseAxis axis,
            AxisInitializeAction action,
            out double targetPosition,
            out string targetName)
        {
            targetPosition = 0.0;
            targetName = "";

            try
            {
                if (axis == null || action == null)
                    return false;

                string positionName = NormalizeInitializePositionName(action.PositionName);
                if (!string.Equals(positionName, "AvoidPosition", StringComparison.OrdinalIgnoreCase))
                    return false;

                string requestedName = !string.IsNullOrWhiteSpace(action.Name) ? action.Name.Trim() : axis.Name;
                string axisName = axis.Name ?? "";

                if (IsInitializeAxisName(requestedName, axisName, "FrontPickerY"))
                {
                    if (_machine.PickerFrontUnit == null ||
                        _machine.PickerFrontUnit.Recipe == null ||
                        _machine.PickerFrontUnit.Recipe.PickerY == null)
                        return false;

                    targetPosition = _machine.PickerFrontUnit.Recipe.PickerY.AvoidPosition;
                    targetName = "FrontPickerY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "RearPickerY"))
                {
                    if (_machine.PickerRearUnit == null ||
                        _machine.PickerRearUnit.Recipe == null ||
                        _machine.PickerRearUnit.Recipe.PickerY == null)
                        return false;

                    targetPosition = _machine.PickerRearUnit.Recipe.PickerY.AvoidPosition;
                    targetName = "RearPickerY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "FrontPickerX"))
                {
                    if (_machine.PickerFrontUnit == null ||
                        _machine.PickerFrontUnit.Recipe == null ||
                        _machine.PickerFrontUnit.Recipe.PickerX == null)
                        return false;

                    targetPosition = _machine.PickerFrontUnit.Recipe.PickerX.AvoidPosition;
                    targetName = "FrontPickerX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "RearPickerX"))
                {
                    if (_machine.PickerRearUnit == null ||
                        _machine.PickerRearUnit.Recipe == null ||
                        _machine.PickerRearUnit.Recipe.PickerX == null)
                        return false;

                    targetPosition = _machine.PickerRearUnit.Recipe.PickerX.AvoidPosition;
                    targetName = "RearPickerX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "InputStageY", "StageY"))
                {
                    if (_machine.InputStageUnit == null ||
                        _machine.InputStageUnit.Recipe == null ||
                        _machine.InputStageUnit.Recipe.WaferY == null)
                        return false;

                    targetPosition = _machine.InputStageUnit.Recipe.WaferY.AvoidPosition;
                    targetName = "InputStageY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "InputVisionX", "CameraX"))
                {
                    if (_machine.InputStageUnit == null ||
                        _machine.InputStageUnit.Recipe == null ||
                        _machine.InputStageUnit.Recipe.VisionX == null)
                        return false;

                    targetPosition = _machine.InputStageUnit.Recipe.VisionX.AvoidPosition;
                    targetName = "InputVisionX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputVisionX"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.VisionX == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.VisionX.AvoidPosition;
                    targetName = "OutputVisionX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputGoodStageY", "GoodStage_StageY"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.GoodStageY == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.GoodStageY.AvoidPosition;
                    targetName = "OutputGoodStageY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputGoodStageZ", "GoodStage_StageZ"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.GoodStageZ == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.GoodStageZ.AvoidPosition;
                    targetName = "OutputGoodStageZ.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputNGStageY", "NgStage_StageY"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.NGStageY == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.NGStageY.AvoidPosition;
                    targetName = "OutputNGStageY.Avoid";
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching position resolve failed. axis=" +
                    (axis != null ? axis.Name : "-") +
                    ", action=" + (action != null ? action.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsInitializeAxisName(string requestedName, string actualName, params string[] names)
        {
            if (names == null)
                return false;

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.Equals(requestedName, name, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(actualName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeInitializePositionName(string positionName)
        {
            if (string.IsNullOrWhiteSpace(positionName))
                return "AvoidPosition";

            string value = positionName.Trim();
            if (string.Equals(value, "Avoid", StringComparison.OrdinalIgnoreCase))
                return "AvoidPosition";

            return value;
        }

        private async Task<int> ExecuteCustomInitializeActionAsync(
            AxisInitializeStep step,
            AxisInitializeAction action,
            string phase)
        {
            try
            {
                if (action == null)
                    return FailInitializePreparation("Custom 초기화 Action 정보가 없습니다.");

                if (string.Equals(
                    action.Name,
                    AxisInitializeActionName.PrepareOutputStageNgClamp,
                    StringComparison.OrdinalIgnoreCase))
                    return await PrepareOutputStageNgClampForInitializeAsync(action).ConfigureAwait(false);

                return FailInitializePreparation("지원하지 않는 Custom 초기화 Action입니다. action=" +
                    action.Name + ", phase=" + phase);
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("Custom 초기화 Action 예외. action=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareOutputStageNgClampForInitializeAsync(AxisInitializeAction action)
        {
            try
            {
                OutputStageUnit outputStage = _machine != null ? _machine.OutputStageUnit : null;
                if (outputStage == null)
                    return FailInitializePreparation("NG Clamp 초기화 준비 실패: OutputStageUnit을 찾을 수 없습니다.");

                bool materialPresent;
                string detectionReason;
                if (!OutputStageInterlockRules.TryGetNgStageMaterialPresence(
                    outputStage,
                    "InitializePrepareNgClamp",
                    out materialPresent,
                    out detectionReason))
                {
                    return FailInitializePreparation("NG Clamp 초기화 준비 실패: " + detectionReason);
                }

                WaferMaterial storedMaterial = MaterialStateService.GetWaferAtLocation(
                    MaterialLocationKind.OutputStageNg);
                bool ringDetected = outputStage.NgBinRingSensor != null &&
                    outputStage.NgBinRingSensor.IsOn;
                var releaseAction = new AxisInitializeAction
                {
                    TargetType = AxisInitializeInterlockTarget.Cylinder,
                    Name = "NGBinGuideClamp",
                    Command = AxisInitializeActionCommand.CylinderBwd,
                    TimeoutMs = action != null ? action.TimeoutMs : 0,
                    Enabled = true,
                    Description = "NG Stage material state independent: Clamp Bwd before ClampLift Up."
                };

                int result = await ExecuteInitializeCylinderActionAsync(releaseAction).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!outputStage.IsBinGuideUnclamped(BinSide.Ng))
                    return FailInitializePreparation(
                        "NG Bin Clamp가 Bwd/Unclamp 상태에 도달하지 못했습니다. " +
                        "materialPresent=" + materialPresent +
                        ", storedWafer=" + (storedMaterial != null ? storedMaterial.WaferId : "-") +
                        ", ringDetected=" + ringDetected);

                QMC.Common.Log.Write("Main", "SYSTEM", "PrepareOutputStageNgClamp",
                    "NG Stage material state independent. Clamp moved to Bwd/Unclamp before ClampLift Up." +
                    " materialPresent=" + materialPresent +
                    ", storedWafer=" + (storedMaterial != null ? storedMaterial.WaferId : "-") +
                    ", ringDetected=" + ringDetected + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("NG Clamp Bwd 초기화 준비 예외. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private BaseCylinder FindCylinderByName(string cylinderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cylinderName))
                    return null;

                BaseCylinder cylinder;
                if (CylinderManager.Items.TryGetValue(cylinderName.Trim(), out cylinder) &&
                    cylinder != null)
                    return cylinder;

                foreach (var item in CylinderManager.Items.Values)
                {
                    if (item != null && string.Equals(item.Name, cylinderName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                return CylinderManager.Get(cylinderName.Trim());
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteSingleAxisHomeAsync(BaseAxis axis)
        {
            bool pickerYHomeGateEntered = false;
            PickerYHomeServoRestoreState pickerYServoState = null;
            try
            {
                if (axis == null)
                {
                    const string message = "초기화할 축 정보가 없습니다.";
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: axis is null. - Failed");
                    return -1;
                }

                if (_runState.IsAxisHomed(axis))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize skipped: already homed in current initialize sequence. axis=" +
                        axis.Name + " - Ok");
                    return 0;
                }

                bool isPickerYHome = IsPickerYHomeAxis(axis);
                if (isPickerYHome)
                {
                    await _pickerYHomeGate.WaitAsync().ConfigureAwait(false);
                    pickerYHomeGateEntered = true;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize requested. axis=" + axis.Name + " - Start");

                string homeInterlockReason;
                if (!MotionGuardRuntime.VerifyAxisHome(axis, out homeInterlockReason))
                {
                    string message = "축 HOME 사전 MotionGuard 인터락 실패. axis=" + axis.Name +
                        ", reason=" + homeInterlockReason;
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        message + " - Failed");
                    return -11;
                }

                IReadOnlyList<string> stopAxes = _axisInterferenceMap.ResolveInterferenceAxes(axis.Name);
                int stopResult = await StopAxesAndWaitUntilStoppedAsync(
                    stopAxes,
                    false,
                    "HOME 전 간섭축 정지. axis=" + axis.Name).ConfigureAwait(false);
                if (stopResult != 0)
                    return stopResult;

                if (isPickerYHome)
                {
                    int pickerYPrepareResult = PreparePickerYHomeServoPair(axis, out pickerYServoState);
                    if (pickerYPrepareResult != 0)
                        return pickerYPrepareResult;
                }
                else
                {
                    int servoOffSettleMs = IsPickerZHomeAxis(axis) ? 1000 : 500;
                    axis.ServoOff();
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis Servo OFF settle before HOME. axis=" + axis.Name +
                        ", waitMs=" + servoOffSettleMs + " - Start");
                    await Task.Delay(servoOffSettleMs).ConfigureAwait(false);
                }

                axis.ResetAlarm();
                Thread.Sleep(500);

                axis.ServoOn();
                Thread.Sleep(500);

                if (!axis.IsServoOn)
                {
                    string message = axis.Name + " Servo ON 실패";
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: servo is off. axis=" + axis.Name + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-SERVO-" + axis.Name, axis.Name, message);
                    return -1;
                }

                if (pickerYServoState != null &&
                    pickerYServoState.PairedAxis != null &&
                    pickerYServoState.PairedAxis.IsServoOn)
                {
                    return FailInitializePreparation(
                        axis.Name + " HOME 불가: 반대 PickerY Servo가 OFF 상태를 유지하지 못했습니다. pairedAxis=" +
                        pickerYServoState.PairedAxis.Name);
                }

                int homeResult = await axis.HomeSearchAsync().ConfigureAwait(false);
                if (homeResult != 0)
                {
                    string message = BuildAxisMotionFailureMessage(axis, "HOME 실패", homeResult);
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: home search failed. axis=" + axis.Name +
                        ", result=" + homeResult + ", message=" + message + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + axis.Name, axis.Name, message);
                    return homeResult;
                }

                if (axis.IsAlarm)
                {
                    string message = axis.Name + " HOME 중 Alarm 발생. code=" + axis.AlarmCode;
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: axis alarm. axis=" + axis.Name +
                        ", code=" + axis.AlarmCode + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + axis.Name, axis.Name, message);
                    return -1;
                }

                if (!axis.IsHomeDone)
                {
                    string message = axis.Name + " HOME 완료 신호가 없습니다.";
                    SetLastFailureMessage(message);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: home done is false. axis=" + axis.Name + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-NOTDONE-" + axis.Name, axis.Name, message);
                    return -1;
                }

                if (pickerYServoState != null)
                {
                    int pickerYRestoreResult = RestorePairedPickerYServoAfterHome(
                        pickerYServoState,
                        true);
                    if (pickerYRestoreResult != 0)
                        return pickerYRestoreResult;
                }

                // 홈 잡고 Avoid로 움직이는 부분 막자.
                // 홈 시컨스에서 제어 하도록 하고 메뉴얼로 할떄는 홈잡고 멈춰야한다!
                //int completeHomeResult = await CompleteAxisHomeConditionAsync(axis).ConfigureAwait(false);
                //if (completeHomeResult != 0)
                //    return completeHomeResult;

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize completed. axis=" + axis.Name + " - Ok");
                _runState.MarkAxisHomed(axis);
                return 0;
            }
            catch (Exception ex)
            {
                string message = "축 초기화 실패: " + (axis != null ? axis.Name : "-") + " / " + ex.Message;
                SetLastFailureMessage(message);
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize failed. axis=" + (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-CORE-EX", "MachineController", message);
                return -1;
            }
            finally
            {
                if (pickerYServoState != null && !pickerYServoState.Restored)
                    RestorePairedPickerYServoAfterHome(pickerYServoState, false);

                if (pickerYHomeGateEntered)
                    _pickerYHomeGate.Release();
            }
        }

        public async Task<int> ExecuteSerialHomeAsync(
            AxisInitializeStep step,
            IList<BaseAxis> axes)
        {
            try
            {
                foreach (BaseAxis axis in axes)
                {
                    int result = await ExecuteSingleAxisHomeAsync(axis).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (Exception ex)
            {
                string message = "Serial 초기화 Step 실패: " + ex.Message;
                SetLastFailureMessage(message);
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStepSerial",
                    "Axis initialize serial step failed. step=" + (step != null ? step.StepNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-SERIAL-EX",
                    "MachineController",
                    message);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteParallelHomeAsync(
            AxisInitializeStep step,
            IList<BaseAxis> axes)
        {
            try
            {
                Task<int>[] tasks = axes
                    .Select(axis => ExecuteSingleAxisHomeAsync(axis))
                    .ToArray();
                int[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] != 0)
                        return results[i];
                }

                return 0;
            }
            catch (Exception ex)
            {
                string message = "Parallel 초기화 Step 실패: " + ex.Message;
                SetLastFailureMessage(message);
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStepParallel",
                    "Axis initialize parallel step failed. step=" + (step != null ? step.StepNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PARALLEL-EX",
                    "MachineController",
                    message);
                return -1;
            }
            finally
            {
            }
        }

        // 시뮬레이션 판정: 실보드가 준비되지 않았거나 대상 축이 Ajin 실축이 아니면(SimAxis) 시뮬 초기화 경로로 처리한다.
        private static bool IsPickerYPairSimulationInitialize(IList<BaseAxis> axes)
        {
            if (axes == null || axes.Count == 0)
                return false;

            if (!AjinFactory.IsRealBoardReady)
                return true;

            return axes.Any(axis => !(axis is AjinAxis));
        }

        public async Task<int> MoveFrontPickerYToAvoidAfterHomeAsync()
        {
            if (_machine.PickerFrontUnit == null ||
                _machine.PickerFrontUnit.PickerY == null ||
                _machine.PickerFrontUnit.Recipe == null ||
                _machine.PickerFrontUnit.Recipe.PickerY == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.PickerFrontUnit.PickerY,
                _machine.PickerFrontUnit.Recipe.PickerY.AvoidPosition,
                "FrontPickerY.Avoid",
                0.0).ConfigureAwait(false);
        }

        public async Task<int> MoveRearPickerYToAvoidAfterHomeAsync()
        {
            if (_machine.PickerRearUnit == null ||
                _machine.PickerRearUnit.PickerY == null ||
                _machine.PickerRearUnit.Recipe == null ||
                _machine.PickerRearUnit.Recipe.PickerY == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.PickerRearUnit.PickerY,
                _machine.PickerRearUnit.Recipe.PickerY.AvoidPosition,
                "RearPickerY.Avoid",
                0.0).ConfigureAwait(false);
        }

        // 시뮬레이션 PickerYPair 초기화: 실장비 페어 경로와 동일한 서보 준비/HOME 순서를 따르되,
        // SimAxis에는 없는 Ajin 하드리밋(MEL/PEL) 동시 탐색과 Ajin 센서 재검증 단계만 생략한다.
        // 순수 시뮬(양쪽 IsSimulationMode)에서는 PickerY HOME 인터락이 MEL/PEL·반대축 서보 조건을 건너뛰고
        // "홈 대상 축 Servo ON"만 요구하므로, 두 축을 ServoOn 한 뒤 Front -> Rear 순차 HOME 하면 실장비와 유사하게 완료된다.
        private async Task<int> ExecutePickerYPairSimulationInitializeAsync(
            AxisInitializeStep step,
            IList<BaseAxis> axes)
        {
            BaseAxis frontY = null;
            BaseAxis rearY = null;
            IDisposable pairInitializeScope = null;
            try
            {
                // 대상 확정: SimAxis라 AjinAxis 타입은 요구하지 않되, 장비의 Front/Rear PickerY와 동일 참조인지는 검증한다.
                frontY = axes.FirstOrDefault(axis =>
                    axis != null && string.Equals(axis.Name, "FrontPickerY", StringComparison.OrdinalIgnoreCase));
                rearY = axes.FirstOrDefault(axis =>
                    axis != null && string.Equals(axis.Name, "RearPickerY", StringComparison.OrdinalIgnoreCase));
                if (frontY == null || rearY == null ||
                    _machine == null || _machine.PickerFrontUnit == null || _machine.PickerRearUnit == null ||
                    !ReferenceEquals(frontY, _machine.PickerFrontUnit.PickerY) ||
                    !ReferenceEquals(rearY, _machine.PickerRearUnit.PickerY))
                {
                    return FailInitializePreparation(
                        "PickerYPair 시뮬레이션 초기화 대상이 장비의 FrontPickerY/RearPickerY 축과 일치하지 않습니다.");
                }

                if (_machine.PickerFrontUnit.Recipe == null ||
                    _machine.PickerFrontUnit.Recipe.PickerY == null ||
                    _machine.PickerRearUnit.Recipe == null ||
                    _machine.PickerRearUnit.Recipe.PickerY == null)
                {
                    return FailInitializePreparation(
                        "PickerYPair 시뮬레이션 초기화 후 Avoid 이동에 필요한 Front/Rear PickerY teaching 정보가 없습니다.");
                }

                pairInitializeScope = MotionGuardRuntime.BeginPickerYPairLimitSearch(frontY, rearY);
                if (!MotionGuardRuntime.BeginPickerYPairInitializeHome(frontY, rearY))
                    return FailInitializePreparation(
                        "PickerYPair 시뮬레이션 실시간 충돌 감시 HOME 범위 전환에 실패했습니다.");

                // Pair 두 축 자체의 정지 확인을 유지하고, 외부 간섭축만 이름으로 구분한다.
                var interferenceAxes = ResolvePickerYPairStopAxisNames(frontY, rearY);
                int stopResult = await StopAxesAndWaitUntilStoppedAsync(
                    interferenceAxes,
                    false,
                    "PickerYPair 시뮬 HOME 전 간섭축 정지").ConfigureAwait(false);
                if (stopResult != 0)
                    return stopResult;

                // 서보 준비: 실장비 페어 경로(Stop -> ServoOff -> ResetAlarm -> ServoOn)와 동일 순서.
                frontY.Stop();
                rearY.Stop();
                frontY.ServoOff();
                rearY.ServoOff();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.ResetAlarm();
                rearY.ResetAlarm();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.ServoOn();
                rearY.ServoOn();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.UpdateStatus();
                rearY.UpdateStatus();
                if (!frontY.IsServoOn || !rearY.IsServoOn)
                    return FailInitializePreparation(
                        "PickerYPair 시뮬 Servo 준비 실패. frontServo=" + frontY.IsServoOn +
                        ", rearServo=" + rearY.IsServoOn);

                // HOME: 실장비와 동일하게 Front -> Rear 순차. 시뮬 HomeSearchAsync가 MotionGuard(Home)를 통과한 뒤 IsHomeDone을 세운다.
                int frontHomeResult = await frontY.HomeSearchAsync().ConfigureAwait(false);
                frontY.UpdateStatus();
                if (frontHomeResult != 0 || !frontY.IsHomeDone || frontY.IsAlarm)
                {
                    frontY.Stop();
                    rearY.Stop();
                    return FailInitializePreparation(
                        "PickerYPair 시뮬 FrontPickerY HOME 실패. result=" + frontHomeResult +
                        ", home=" + frontY.IsHomeDone + ", alarm=" + frontY.IsAlarm);
                }

                int rearHomeResult = await rearY.HomeSearchAsync().ConfigureAwait(false);
                rearY.UpdateStatus();
                if (rearHomeResult != 0 || !rearY.IsHomeDone || rearY.IsAlarm)
                {
                    frontY.Stop();
                    rearY.Stop();
                    return FailInitializePreparation(
                        "PickerYPair 시뮬 RearPickerY HOME 실패. result=" + rearHomeResult +
                        ", home=" + rearY.IsHomeDone + ", alarm=" + rearY.IsAlarm);
                }

                int frontAvoidResult = await MoveFrontPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                frontY.UpdateStatus();
                if (frontAvoidResult != 0 ||
                    !_machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    frontY.Stop();
                    rearY.Stop();
                    return FailInitializePreparation(
                        "PickerYPair 시뮬 FrontPickerY HOME 후 Avoid 이동 실패. result=" + frontAvoidResult +
                        ", actual=" + frontY.ActualPosition.ToString("0.###"));
                }

                int rearAvoidResult = await MoveRearPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                rearY.UpdateStatus();
                if (rearAvoidResult != 0 ||
                    !_machine.PickerRearUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    frontY.Stop();
                    rearY.Stop();
                    return FailInitializePreparation(
                        "PickerYPair 시뮬 RearPickerY HOME 후 Avoid 이동 실패. result=" + rearAvoidResult +
                        ", actual=" + rearY.ActualPosition.ToString("0.###"));
                }

                _runState.MarkAxisHomed(frontY);
                _runState.MarkAxisHomed(rearY);
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerYPairInitialize",
                    "Simulation PickerYPair initialize completed: Front HOME -> Rear HOME -> Front Avoid -> Rear Avoid " +
                    "(hardware limit search skipped). step=" +
                    (step != null ? step.StepNo : 0) + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                if (frontY != null) { try { frontY.Stop(); } catch { } }
                if (rearY != null) { try { rearY.Stop(); } catch { } }
                string message = "PickerYPair 시뮬레이션 초기화 예외: " + ex.Message;
                SetLastFailureMessage(message);
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PICKER-Y-PAIR-SIM",
                    "MachineController",
                    message);
                return -1;
            }
            finally
            {
                if (pairInitializeScope != null)
                    pairInitializeScope.Dispose();
            }
        }

        private async Task<int> ExecutePickerYPairInitializeAsync(
            AxisInitializeStep step,
            IList<BaseAxis> axes)
        {
            AjinAxis frontY = null;
            AjinAxis rearY = null;
            CancellationTokenSource limitSearchCancellation = null;
            IDisposable pairInitializeScope = null;
            try
            {
                if (axes == null || axes.Count != 2)
                    return FailInitializePreparation(
                        "PickerYPair 초기화에는 FrontPickerY와 RearPickerY 두 축만 필요합니다.");

                // 시뮬레이션 축(SimAxis)은 Ajin 하드리밋 동시 탐색 경로를 사용할 수 없다.
                // 실보드가 아니면 실장비와 동일한 HOME 완료/좌표 결과가 되도록 개별 축 초기화 경로로 Front -> Rear를 순차 HOME 처리한다.
                if (IsPickerYPairSimulationInitialize(axes))
                    return await ExecutePickerYPairSimulationInitializeAsync(step, axes).ConfigureAwait(false);

                frontY = axes.OfType<AjinAxis>().FirstOrDefault(axis =>
                    string.Equals(axis.Name, "FrontPickerY", StringComparison.OrdinalIgnoreCase));
                rearY = axes.OfType<AjinAxis>().FirstOrDefault(axis =>
                    string.Equals(axis.Name, "RearPickerY", StringComparison.OrdinalIgnoreCase));
                if (frontY == null || rearY == null ||
                    _machine == null || _machine.PickerFrontUnit == null || _machine.PickerRearUnit == null ||
                    !ReferenceEquals(frontY, _machine.PickerFrontUnit.PickerY) ||
                    !ReferenceEquals(rearY, _machine.PickerRearUnit.PickerY))
                {
                    return FailInitializePreparation(
                        "PickerYPair 초기화 대상이 장비의 FrontPickerY/RearPickerY Ajin 축과 일치하지 않습니다.");
                }

                if (_machine.PickerFrontUnit.Recipe == null ||
                    _machine.PickerFrontUnit.Recipe.PickerY == null ||
                    _machine.PickerRearUnit.Recipe == null ||
                    _machine.PickerRearUnit.Recipe.PickerY == null)
                {
                    return FailInitializePreparation(
                        "PickerYPair 초기화 후 Avoid 이동에 필요한 Front/Rear PickerY teaching 정보가 없습니다.");
                }

                // Pair 두 축 자체의 정지 확인을 유지하고, 외부 간섭축만 이름으로 구분한다.
                var interferenceAxes = ResolvePickerYPairStopAxisNames(frontY, rearY);
                int stopResult = await StopAxesAndWaitUntilStoppedAsync(
                    interferenceAxes,
                    false,
                    "PickerYPair 리밋 탐색 전 간섭축 정지").ConfigureAwait(false);
                if (stopResult != 0)
                    return stopResult;

                // 간섭축 정지 완료 후, 실장비도 Servo/Alarm 준비 전에 Pair 보호 범위를 연다.
                // X축이 움직이는 준비 구간에는 예외를 열지 않고 기존 초기화 이동 순서는 유지한다.
                pairInitializeScope = MotionGuardRuntime.BeginPickerYPairLimitSearch(frontY, rearY);

                frontY.Stop();
                rearY.Stop();
                frontY.ServoOff();
                rearY.ServoOff();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.ResetAlarm();
                rearY.ResetAlarm();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.ServoOn();
                rearY.ServoOn();
                await Task.Delay(500).ConfigureAwait(false);
                frontY.UpdateStatus();
                rearY.UpdateStatus();
                bool frontAlarmReady = !frontY.IsAlarm ||
                    (frontY.Sensor_MEL && frontY.AlarmCode == 21u);
                bool rearAlarmReady = !rearY.IsAlarm ||
                    (rearY.Sensor_PEL && rearY.AlarmCode == 20u);
                if (!frontY.IsServoOn || !rearY.IsServoOn || !frontAlarmReady || !rearAlarmReady)
                {
                    return FailInitializePreparation(
                        "PickerYPair Servo/Alarm 준비 실패. frontServo=" + frontY.IsServoOn +
                        ", rearServo=" + rearY.IsServoOn +
                        ", frontAlarm=" + frontY.IsAlarm +
                        ", frontAlarmCode=" + frontY.AlarmCode +
                        ", frontMEL=" + frontY.Sensor_MEL +
                        ", rearAlarm=" + rearY.IsAlarm +
                        ", rearAlarmCode=" + rearY.AlarmCode +
                        ", rearPEL=" + rearY.Sensor_PEL);
                }

                double frontVelocity = Math.Max(0.000001, frontY.Config.JogFineVelocity);
                double rearVelocity = Math.Max(0.000001, rearY.Config.JogFineVelocity);
                limitSearchCancellation = new CancellationTokenSource();
                int[] searchResults;
                Task<int> frontSearch = frontY.SearchHardwareLimitForInitializeAsync(
                    -1,
                    frontVelocity,
                    30000,
                    limitSearchCancellation.Token);
                Task<int> rearSearch = rearY.SearchHardwareLimitForInitializeAsync(
                    1,
                    rearVelocity,
                    30000,
                    limitSearchCancellation.Token);

                Task<int> firstSearch = await Task.WhenAny(frontSearch, rearSearch).ConfigureAwait(false);
                int firstSearchResult = await firstSearch.ConfigureAwait(false);
                if (firstSearchResult != 0)
                {
                    limitSearchCancellation.Cancel();
                    StopPickerYPairInitialize(frontY, rearY);
                }

                searchResults = await Task.WhenAll(frontSearch, rearSearch).ConfigureAwait(false);

                if (searchResults.Any(result => result != 0))
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair 하드리밋 탐색 실패. frontResult=" + searchResults[0] +
                        ", rearResult=" + searchResults[1]);
                }

                frontY.UpdateStatus();
                rearY.UpdateStatus();
                if (!frontY.Sensor_MEL || !rearY.Sensor_PEL || frontY.IsMoving || rearY.IsMoving)
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair 하드리밋/정지 재검증 실패. frontMEL=" + frontY.Sensor_MEL +
                        ", rearPEL=" + rearY.Sensor_PEL +
                        ", frontMoving=" + frontY.IsMoving +
                        ", rearMoving=" + rearY.IsMoving);
                }

                if (!MotionGuardRuntime.BeginPickerYPairInitializeHome(frontY, rearY))
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair 실시간 충돌 감시 HOME 범위 전환에 실패했습니다.");
                }

                using (MotionGuardRuntime.BeginPickerYPairHome(frontY, rearY))
                {
                    string pairHomeReason;
                    if (!MotionGuardRuntime.AuthorizePickerYPairHome(frontY, rearY, out pairHomeReason))
                    {
                        StopPickerYPairInitialize(frontY, rearY);
                        return FailInitializePreparation(
                            "PickerYPair HOME MotionGuard 사전 검증 실패. " + pairHomeReason);
                    }

                    int frontHomeResult = await frontY.HomeSearchAsync().ConfigureAwait(false);
                    frontY.UpdateStatus();
                    if (frontHomeResult != 0 || !frontY.IsHomeDone || frontY.IsAlarm)
                    {
                        frontY.Stop();
                        rearY.Stop();
                        return FailInitializePreparation(
                            "PickerYPair FrontPickerY HOME 실패. result=" + frontHomeResult +
                            ", home=" + frontY.IsHomeDone +
                            ", alarm=" + frontY.IsAlarm);
                    }

                    int rearHomeResult = await rearY.HomeSearchAsync().ConfigureAwait(false);
                    rearY.UpdateStatus();
                    if (rearHomeResult != 0 || !rearY.IsHomeDone || rearY.IsAlarm)
                    {
                        frontY.Stop();
                        rearY.Stop();
                        return FailInitializePreparation(
                            "PickerYPair RearPickerY HOME 실패. result=" + rearHomeResult +
                            ", home=" + rearY.IsHomeDone +
                            ", alarm=" + rearY.IsAlarm);
                    }
                }

                frontY.UpdateStatus();
                rearY.UpdateStatus();
                if (!frontY.IsHomeDone || !rearY.IsHomeDone || frontY.IsAlarm || rearY.IsAlarm)
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair HOME 완료 검증 실패. frontHome=" + frontY.IsHomeDone +
                        ", rearHome=" + rearY.IsHomeDone +
                        ", frontAlarm=" + frontY.IsAlarm +
                        ", rearAlarm=" + rearY.IsAlarm);
                }

                int frontAvoidResult = await MoveFrontPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                frontY.UpdateStatus();
                if (frontAvoidResult != 0 ||
                    !_machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair FrontPickerY HOME 후 Avoid 이동 실패. result=" + frontAvoidResult +
                        ", actual=" + frontY.ActualPosition.ToString("0.###"));
                }

                int rearAvoidResult = await MoveRearPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                rearY.UpdateStatus();
                if (rearAvoidResult != 0 ||
                    !_machine.PickerRearUnit.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    StopPickerYPairInitialize(frontY, rearY);
                    return FailInitializePreparation(
                        "PickerYPair RearPickerY HOME 후 Avoid 이동 실패. result=" + rearAvoidResult +
                        ", actual=" + rearY.ActualPosition.ToString("0.###"));
                }

                _runState.MarkAxisHomed(frontY);
                _runState.MarkAxisHomed(rearY);
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerYPairInitialize",
                    "Front MEL/Rear PEL simultaneous search and Front HOME -> Rear HOME -> Front Avoid -> Rear Avoid completed. step=" +
                    (step != null ? step.StepNo : 0) + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                StopPickerYPairInitialize(frontY, rearY);
                string message = "PickerYPair 초기화 예외: " + ex.Message;
                SetLastFailureMessage(message);
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "INIT-PICKER-Y-PAIR",
                    "MachineController",
                    message);
                return -1;
            }
            finally
            {
                if (pairInitializeScope != null)
                    pairInitializeScope.Dispose();
                if (limitSearchCancellation != null)
                    limitSearchCancellation.Dispose();
                if (frontY != null)
                    frontY.ReleaseInitializeHardwareLimitSearch();
                if (rearY != null)
                    rearY.ReleaseInitializeHardwareLimitSearch();
            }
        }

        private List<string> ResolvePickerYPairStopAxisNames(
            BaseAxis frontY,
            BaseAxis rearY)
        {
            var pairAxisNames = new List<string>();
            if (frontY != null && !string.IsNullOrWhiteSpace(frontY.Name))
                pairAxisNames.Add(frontY.Name);
            if (rearY != null && !string.IsNullOrWhiteSpace(rearY.Name))
                pairAxisNames.Add(rearY.Name);

            var externalAxisNames = _axisInterferenceMap
                .ResolveInterferenceAxes(frontY != null ? frontY.Name : string.Empty)
                .Concat(_axisInterferenceMap.ResolveInterferenceAxes(
                    rearY != null ? rearY.Name : string.Empty))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Where(name =>
                    !IsSameInitializeAxisName(name, frontY != null ? frontY.Name : string.Empty) &&
                    !IsSameInitializeAxisName(name, rearY != null ? rearY.Name : string.Empty));

            return pairAxisNames
                .Concat(externalAxisNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsSameInitializeAxisName(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            string canonicalLeft = AjinAxisDefaults.ResolveName(left.Trim());
            string canonicalRight = AjinAxisDefaults.ResolveName(right.Trim());
            return string.Equals(
                canonicalLeft,
                canonicalRight,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void StopPickerYPairInitialize(AjinAxis frontY, AjinAxis rearY)
        {
            if (frontY != null)
            {
                try { frontY.StopInitializeHardwareLimitSearch(); } catch { }
            }
            if (rearY != null)
            {
                try { rearY.StopInitializeHardwareLimitSearch(); } catch { }
            }
        }

        private static bool IsPickerYHomeAxis(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return false;

                return string.Equals(axis.Name, "FrontPickerY", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(axis.Name, "RearPickerY", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsPickerZHomeAxis(BaseAxis axis)
        {
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    return false;

                return axis.Name.StartsWith("FrontPickerZ", StringComparison.OrdinalIgnoreCase) ||
                       axis.Name.StartsWith("RearPickerZ", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private int PreparePickerYHomeServoPair(
            BaseAxis targetAxis,
            out PickerYHomeServoRestoreState restoreState)
        {
            restoreState = null;
            try
            {
                BaseAxis frontY = _machine.PickerFrontUnit != null
                    ? _machine.PickerFrontUnit.PickerY
                    : null;
                BaseAxis rearY = _machine.PickerRearUnit != null
                    ? _machine.PickerRearUnit.PickerY
                    : null;
                if (frontY == null || rearY == null || targetAxis == null)
                    return FailInitializePreparation(
                        "Picker Y HOME Servo 준비 실패: Front/Rear PickerY 축 정보를 확인할 수 없습니다.");

                bool targetIsFront = ReferenceEquals(targetAxis, frontY) ||
                    string.Equals(targetAxis.Name, "FrontPickerY", StringComparison.OrdinalIgnoreCase);
                bool targetIsRear = ReferenceEquals(targetAxis, rearY) ||
                    string.Equals(targetAxis.Name, "RearPickerY", StringComparison.OrdinalIgnoreCase);
                if (!targetIsFront && !targetIsRear)
                    return FailInitializePreparation(
                        "Picker Y HOME Servo 준비 실패: 선택 축이 Front/Rear PickerY가 아닙니다. axis=" +
                        targetAxis.Name);

                BaseAxis pairedAxis = targetIsFront ? rearY : frontY;
                restoreState = new PickerYHomeServoRestoreState
                {
                    TargetAxis = targetAxis,
                    PairedAxis = pairedAxis,
                    RestoreServoOn = pairedAxis.IsServoOn,
                    RestoreHomeDone = pairedAxis.IsHomeDone,
                    Restored = false
                };

                frontY.Stop();
                rearY.Stop();
                Thread.Sleep(100);

                frontY.ServoOff();
                rearY.ServoOff();
                Thread.Sleep(500);

                try { frontY.UpdateStatus(); } catch { }
                try { rearY.UpdateStatus(); } catch { }

                if (frontY.IsMoving || rearY.IsMoving)
                    return FailInitializePreparation(
                        "Picker Y HOME Servo 준비 실패: 두 PickerY 정지가 확인되지 않았습니다. frontMoving=" +
                        frontY.IsMoving + ", rearMoving=" + rearY.IsMoving);

                if (frontY.IsServoOn || rearY.IsServoOn)
                    return FailInitializePreparation(
                        "Picker Y HOME Servo 준비 실패: 두 PickerY Servo OFF가 확인되지 않았습니다. frontServo=" +
                        (frontY.IsServoOn ? "ON" : "OFF") +
                        ", rearServo=" + (rearY.IsServoOn ? "ON" : "OFF"));

                QMC.Common.Log.Write("Main", "SYSTEM", "PreparePickerYHomeServoPair",
                    "Picker Y pair stopped and Servo OFF before single-axis HOME. target=" + targetAxis.Name +
                    ", paired=" + pairedAxis.Name +
                    ", pairedRestoreServoOn=" + restoreState.RestoreServoOn +
                    ", pairedRestoreHomeDone=" + restoreState.RestoreHomeDone +
                    ", frontY=" + frontY.ActualPosition.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    ", rearY=" + rearY.ActualPosition.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation(
                    "Picker Y HOME Servo 준비 중 예외가 발생했습니다. axis=" +
                    (targetAxis != null ? targetAxis.Name : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int RestorePairedPickerYServoAfterHome(
            PickerYHomeServoRestoreState restoreState,
            bool reportFailure)
        {
            try
            {
                if (restoreState == null || restoreState.Restored)
                    return 0;

                if (restoreState.TargetAxis != null && restoreState.TargetAxis.IsMoving)
                {
                    restoreState.TargetAxis.Stop();
                    Thread.Sleep(100);
                }

                if (!restoreState.RestoreServoOn)
                {
                    restoreState.Restored = true;
                    return 0;
                }

                BaseAxis pairedAxis = restoreState.PairedAxis;
                if (pairedAxis == null)
                    return ReportPickerYServoRestoreFailure(
                        "반대 PickerY 축 정보가 없습니다.",
                        reportFailure);

                if (pairedAxis.IsAlarm)
                    return ReportPickerYServoRestoreFailure(
                        "반대 PickerY에 Alarm이 있어 Servo 상태를 복원할 수 없습니다. axis=" + pairedAxis.Name +
                        ", alarmCode=" + pairedAxis.AlarmCode,
                        reportFailure);

                pairedAxis.ServoOn();
                Thread.Sleep(500);
                try { pairedAxis.UpdateStatus(); } catch { }

                if (!pairedAxis.IsServoOn)
                    return ReportPickerYServoRestoreFailure(
                        "반대 PickerY Servo ON 복원에 실패했습니다. axis=" + pairedAxis.Name,
                        reportFailure);

                if (restoreState.RestoreHomeDone)
                {
                    AjinAxis ajinAxis = pairedAxis as AjinAxis;
                    if (ajinAxis != null)
                        ajinAxis.RestoreHomeDoneSignal(true, false);

                    if (!pairedAxis.IsHomeDone)
                        return ReportPickerYServoRestoreFailure(
                            "반대 PickerY HomeDone 상태 복원에 실패했습니다. axis=" + pairedAxis.Name,
                            reportFailure);
                }

                restoreState.Restored = true;
                QMC.Common.Log.Write("Main", "SYSTEM", "RestorePairedPickerYServoAfterHome",
                    "Paired PickerY state restored after single-axis HOME. target=" +
                    (restoreState.TargetAxis != null ? restoreState.TargetAxis.Name : "-") +
                    ", paired=" + pairedAxis.Name +
                    ", servo=ON, homeDone=" + pairedAxis.IsHomeDone + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return ReportPickerYServoRestoreFailure(
                    "반대 PickerY 상태 복원 중 예외가 발생했습니다. error=" + ex.Message,
                    reportFailure);
            }
            finally
            {
            }
        }

        private int ReportPickerYServoRestoreFailure(string message, bool reportFailure)
        {
            try
            {
                string fullMessage = "Picker Y HOME 후 상태 복원 실패: " + message;
                if (reportFailure)
                    return FailInitializePreparation(fullMessage);

                QMC.Common.Log.Write("Main", "SYSTEM", "RestorePairedPickerYServoAfterHome",
                    fullMessage + " - Failed");
                return -1;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private int FailInitializePreparation(string message)
        {
            SetLastFailureMessage(message);
            QMC.Common.Log.Write("Main", "SYSTEM", "InitializePreparation", message + " - Failed");
            AlarmManager.Raise(AlarmSeverity.Error, "INIT-PREP", "MachineController", message);
            return -1;
        }

        private void SetLastFailureMessage(string message)
        {
            _lastFailureMessage = message ?? string.Empty;
        }

        private static string BuildAxisMotionFailureMessage(BaseAxis axis, string action, int result)
        {
            if (axis == null)
                return (action ?? "Motion") + " 실패. result=" + result;

            if (!string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                return axis.LastMotionFailureMessage;

            string reason;
            if (axis.IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + axis.AlarmCode.ToString("X4");
            else if (!axis.IsServoOn)
                reason = "Servo is OFF.";
            else if (result == -11)
                reason = "Motion guard/interlock blocked.";
            else if (result == -2)
                reason = "Axis is not ready.";
            else
                reason = "Motion failed.";

            string unit = axis.Setup != null ? axis.Setup.Unit : string.Empty;
            return axis.Name + " " + (action ?? "Motion") +
                   ". result=" + result +
                   ", reason=" + reason +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                   ", pos=" + axis.ActualPosition.ToString(
                       "0.###",
                       System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
        }

        public async Task<int> StopAllAxesAsync()
        {
            try
            {
                var axes = _enumerateAxes()
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => x.Name)
                    .ToList();

                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop requested. count=" + axes.Count +
                    ", emergency=False - Start");
                return await StopAxesAsync(axes, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public string GetLastFailureMessage()
        {
            return _lastFailureMessage ?? string.Empty;
        }
    }
}
