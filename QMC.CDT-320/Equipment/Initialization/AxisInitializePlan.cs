using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    [DataContract]
    public class AxisInitializePlan
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public List<string> Help { get; set; } = new List<string>();
        [DataMember] public List<string> AllowedRunModes { get; set; } = new List<string>();
        [DataMember] public List<string> AllowedInterlockTargets { get; set; } = new List<string>();
        [DataMember] public List<string> AllowedInterlockStates { get; set; } = new List<string>();
        [DataMember] public List<AxisInitializeInterlockRule> InterlockExamples { get; set; } =
            new List<AxisInitializeInterlockRule>();
        [DataMember] public int Version { get; set; } = 1;
        [DataMember] public DateTime SavedAt { get; set; }
        [DataMember] public List<AxisInitializeStep> Steps { get; set; } =
            new List<AxisInitializeStep>();
    }

    [DataContract]
    public class AxisInitializeStep
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public int StepNo { get; set; }
        [DataMember] public string GroupName { get; set; }
        [DataMember] public List<string> AxisNames { get; set; } = new List<string>();
        [DataMember] public List<AxisInitializeAction> PreActions { get; set; } =
            new List<AxisInitializeAction>();
        [DataMember] public List<AxisInitializeAction> PostActions { get; set; } =
            new List<AxisInitializeAction>();
        [DataMember] public string RunMode { get; set; } = AxisInitializeRunMode.Serial;
        [DataMember] public string ParallelLane { get; set; } = AxisInitializeParallelLane.None;
        [DataMember] public string InterlockGroup { get; set; }
        [DataMember] public List<AxisInitializeInterlockRule> Interlocks { get; set; } =
            new List<AxisInitializeInterlockRule>();
        [DataMember] public bool Enabled { get; set; } = true;

        // 전체 초기화 실행 시 AxisInitializeSequence의 Step switch가 실제 Unit 객체를 연결합니다.
        // 문자열 필드는 Monitor/로그/기존 파일 호환용이며 실제 HOME 대상 선택에는 사용하지 않습니다.
        internal List<BaseAxis> RuntimeAxes { get; set; } = new List<BaseAxis>();
        internal List<BaseAxis> RuntimeInterlockAxes { get; set; } = new List<BaseAxis>();
        internal bool RuntimeParallelHome { get; set; }
        internal bool RuntimePickerYPairHome { get; set; }
    }

    [DataContract]
    public class AxisInitializeAction
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public string TargetType { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string Command { get; set; }
        [DataMember] public string PositionName { get; set; }
        [DataMember] public int TimeoutMs { get; set; } = 0;
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string Description { get; set; }

        // 실행 전 Step switch에서 연결되는 실제 장치입니다. DataContract 저장 대상이 아닙니다.
        internal BaseAxis RuntimeAxis { get; set; }
        internal BaseCylinder RuntimeCylinder { get; set; }
        internal double RuntimeTargetPosition { get; set; }
        internal bool HasRuntimeTargetPosition { get; set; }
        internal string RuntimeTargetName { get; set; }
    }

    [DataContract]
    public class AxisInitializeInterlockRule
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public string TargetType { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string ExpectedState { get; set; }
        [DataMember] public string PositionName { get; set; }
        [DataMember] public double ExpectedPosition { get; set; }
        [DataMember] public double Tolerance { get; set; } = 0.01;
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string Description { get; set; }

        // 인터락 판정은 실행 시 연결된 실제 Unit 장치를 우선 사용합니다.
        // 값이 없을 때만 구형/외부 Plan 호환을 위해 기존 이름 해석 경로를 사용합니다.
        internal BaseAxis RuntimeAxis { get; set; }
        internal BaseCylinder RuntimeCylinder { get; set; }
        internal BaseDigitalInput RuntimeDigitalInput { get; set; }
        internal bool RuntimeTargetBindingApplied { get; set; }
    }

    public static class AxisInitializeInterlockTarget
    {
        public const string Axis = "Axis";
        public const string Cylinder = "Cylinder";
        public const string DigitalInput = "DigitalInput";
        public const string Resource = "Resource";
        public const string Machine = "Machine";
        public const string Material = "Material";
    }

    public static class AxisInitializeInterlockState
    {
        public const string ServoOn = "ServoOn";
        public const string HomeDone = "HomeDone";
        public const string AlarmOff = "AlarmOff";
        public const string Fwd = "Fwd";
        public const string Bwd = "Bwd";
        public const string On = "On";
        public const string Off = "Off";
        public const string AllOk = "AllOk";
        public const string Stopped = "Stopped";
        public const string AtPosition = "AtPosition";
        public const string AtOrBelowPosition = "AtOrBelowPosition";
        public const string AutoStopped = "AutoStopped";
        public const string ManualStopped = "ManualStopped";
        public const string AllAxesStopped = "AllAxesStopped";
        public const string Empty = "Empty";
        public const string SharedRailHomeClear = "SharedRailHomeClear";
        public const string SafeForStageMove = "SafeForStageMove";
        public const string HomeOrAvoid = "HomeOrAvoid";
    }

    public static class AxisInitializeInterlockName
    {
        public const string Machine = "Machine";
        public const string Resources = "Resources";
        public const string InputFeeder = "InputFeeder";
        public const string OutputFeeder = "OutputFeeder";
        public const string OutputStageGood = "OutputStageGood";
        public const string OutputStageNg = "OutputStageNg";
    }

    public static class AxisInitializeSafetyInput
    {
        public const string ElecEmgOn = "ElecEmgOn";
        public const string OpEmgOn = "OpEmgOn";
        public const string RearEmgOn = "RearEmgOn";
        public const string RightEmgOn = "RightEmgOn";
        public const string LeftDoorCheck = "LeftDoorCheck";
        public const string RearDoorCheck = "RearDoorCheck";
        public const string RightDoorCheck = "RightDoorCheck";
        public const string WaferLifterDoorCheck = "WaferLifterDoorCheck";
        public const string BinLifterDoorCheck = "BinLifterDoorCheck";
        public const string WaferFeederAvoidPositionCheck = "WaferFeederAvoidPositionCheck";
        public const string BinFeederAvoidPositionCheck = "BinFeederAvoidPositionCheck";
    }

    public static class AxisInitializeActionCommand
    {
        public const string CylinderFwd = "CylinderFwd";
        public const string CylinderBwd = "CylinderBwd";
        public const string AxisTeachingMove = "AxisTeachingMove";
        public const string CustomHook = "CustomHook";
    }

    public static class AxisInitializeActionName
    {
        public const string PrepareOutputStageNgClamp = "PrepareOutputStageNgClamp";
    }

    public sealed class AxisInitializeStepProgress
    {
        public int StepNo { get; set; }
        public string GroupName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }

        public static AxisInitializeStepProgress Create(
            AxisInitializeStep step,
            string status,
            string message)
        {
            return new AxisInitializeStepProgress
            {
                StepNo = step != null ? step.StepNo : 0,
                GroupName = step != null ? step.GroupName : "",
                Status = status ?? "",
                Message = message ?? ""
            };
        }
    }

    public static class AxisInitializeStepStatus
    {
        public const string Waiting = "Waiting";
        public const string Running = "Running";
        public const string Complete = "Done";
        public const string Failed = "Failed";
        public const string Disabled = "Disabled";
        public const string ReinitializeRequired = "Reinitialize Required";
    }

    public static class AxisInitializeRunMode
    {
        public const string Serial = "Serial";
        public const string Parallel = "Parallel";

        public static bool IsParallel(string value)
        {
            try
            {
                return string.Equals(value, Parallel, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    public static class AxisInitializeParallelLane
    {
        public const string None = "";
        public const string Input = "Input";
        public const string Output = "Output";

        public static bool Is(string value, string expected)
        {
            try
            {
                return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    public static class AxisInitializePlanStore
    {
        public static AxisInitializePlan LoadOrCreateDefault(IEnumerable<BaseAxis> axes)
        {
            try
            {
                AxisInitializePlan plan = CreateDefault(axes);
                EnsureEditableHelp(plan);
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Built-in axis initialize plan created. - Ok");
                return plan;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Axis initialize plan load/create failed: " + ex.Message + " - Failed");
                return CreateDefault(axes);
            }
        }

        /// <summary>
        /// Monitor와 개별/그룹 초기화가 사용하는 기본 Plan을 반환합니다.
        /// Step 순서의 단일 정본은 AxisInitializeSequence.CreateDefaultPlan에 있습니다.
        /// </summary>
        public static AxisInitializePlan CreateDefault(IEnumerable<BaseAxis> axes)
        {
            return AxisInitializeSequence.CreateDefaultPlan(axes);
        }

        internal static void ApplyCommonSafetyInterlocks(AxisInitializePlan plan)
        {
            try
            {
                if (plan == null || plan.Steps == null)
                    return;

                foreach (AxisInitializeStep step in plan.Steps)
                {
                    if (step == null)
                        continue;

                    List<AxisInitializeInterlockRule> stepInterlocks = step.Interlocks ??
                        new List<AxisInitializeInterlockRule>();
                    step.Interlocks = new List<AxisInitializeInterlockRule>();

                    AddInterlock(step, AxisInitializeInterlockTarget.Machine,
                        AxisInitializeInterlockName.Machine, AxisInitializeInterlockState.AutoStopped,
                        "Auto와 Sequence를 정지한 후 초기화를 다시 실행하십시오.");
                    AddInterlock(step, AxisInitializeInterlockTarget.Machine,
                        AxisInitializeInterlockName.Machine, AxisInitializeInterlockState.ManualStopped,
                        "진행 중인 Manual 동작을 정지한 후 초기화를 다시 실행하십시오.");
                    AddInterlock(step, AxisInitializeInterlockTarget.Machine,
                        AxisInitializeInterlockName.Machine, AxisInitializeInterlockState.AllAxesStopped,
                        "모든 축이 완전히 정지했는지 확인한 후 초기화를 다시 실행하십시오.");

                    AddInterlock(step, AxisInitializeInterlockTarget.DigitalInput,
                        AxisInitializeSafetyInput.ElecEmgOn, AxisInitializeInterlockState.On,
                        "전장 E-Stop을 복귀하고 안전 상태를 확인하십시오.");
                    AddInterlock(step, AxisInitializeInterlockTarget.DigitalInput,
                        AxisInitializeSafetyInput.OpEmgOn, AxisInitializeInterlockState.On,
                        "조작반 E-Stop을 복귀하고 안전 상태를 확인하십시오.");
                    AddInterlock(step, AxisInitializeInterlockTarget.DigitalInput,
                        AxisInitializeSafetyInput.RearEmgOn, AxisInitializeInterlockState.On,
                        "후면 E-Stop을 복귀하고 안전 상태를 확인하십시오.");
                    AddInterlock(step, AxisInitializeInterlockTarget.DigitalInput,
                        AxisInitializeSafetyInput.RightEmgOn, AxisInitializeInterlockState.On,
                        "우측 E-Stop을 복귀하고 안전 상태를 확인하십시오.");

                    step.Interlocks.AddRange(stepInterlocks);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Common initialize safety interlock add failed. error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddFeederSafeInterlocks(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            bool inputFeeder)
        {
            try
            {
                string materialName = inputFeeder
                    ? AxisInitializeInterlockName.InputFeeder
                    : AxisInitializeInterlockName.OutputFeeder;
                string cylinderName = inputFeeder ? "InputFeederClamp" : "OutputFeederClamp";
                string feederName = inputFeeder ? "InputFeeder" : "OutputFeeder";

                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Material,
                    materialName,
                    AxisInitializeInterlockState.Empty,
                    feederName + "의 자재를 육안 확인한 후 자재가 있으면 Manual 화면에서 제거하십시오.");
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Cylinder,
                    cylinderName,
                    AxisInitializeInterlockState.Bwd,
                    feederName + "는 자동 Unclamp하지 않습니다. 자재를 육안 확인한 후 Manual 화면에서 Unclamp하십시오.");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Feeder initialize safety interlock add failed. group=" + groupName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddPickerXGlobalSafetyInterlocks(
            AxisInitializePlan plan,
            int stepNo,
            string groupName)
        {
            try
            {
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Axis,
                    "InputFeederY",
                    AxisInitializeInterlockState.AtPosition,
                    "InputFeederY를 Avoid 위치로 이동한 후 다시 실행하십시오.",
                    "AvoidPosition");
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck,
                    AxisInitializeInterlockState.On,
                    "Wafer Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift",
                    AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Axis,
                    "OutputFeederY",
                    AxisInitializeInterlockState.AtPosition,
                    "OutputFeederY를 Avoid 위치로 이동한 후 다시 실행하십시오.",
                    "AvoidPosition");
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.BinFeederAvoidPositionCheck,
                    AxisInitializeInterlockState.On,
                    "Bin Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Cylinder,
                    "OutputFeederLift",
                    AxisInitializeInterlockState.Bwd,
                    "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Axis,
                    "InputExpandingZ",
                    AxisInitializeInterlockState.AtOrBelowPosition,
                    "InputExpandingZ를 0 이하 위치로 이동한 후 다시 실행하십시오.",
                    null,
                    0.0);
                AddStepInterlock(plan, stepNo, groupName,
                    AxisInitializeInterlockTarget.Axis,
                    "OutputGoodStageZ",
                    AxisInitializeInterlockState.AtOrBelowPosition,
                    "OutputGoodStageZ를 Process Position 이하로 이동한 후 다시 실행하십시오.",
                    "ProcessPosition");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Picker X global initialize safety interlock add failed. step=" + stepNo +
                    ", group=" + groupName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddAxisHomeDoneInterlocks(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            params string[] axisNames)
        {
            try
            {
                if (axisNames == null)
                    return;

                foreach (string axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;

                    AddStepInterlock(plan, stepNo, groupName,
                        AxisInitializeInterlockTarget.Axis,
                        axisName,
                        AxisInitializeInterlockState.HomeDone,
                        axisName + " Home을 먼저 완료한 후 현재 Step을 다시 실행하십시오.");
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Initialize prerequisite axis interlock add failed. step=" + stepNo +
                    ", group=" + groupName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddStepInterlock(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string targetType,
            string name,
            string expectedState,
            string description,
            string positionName = null,
            double expectedPosition = 0.0,
            double tolerance = 0.01)
        {
            try
            {
                if (plan == null || plan.Steps == null)
                    return;

                AxisInitializeStep step = plan.Steps.FirstOrDefault(x => x != null &&
                    x.StepNo == stepNo &&
                    string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    return;

                AddInterlock(step, targetType, name, expectedState, description,
                    positionName, expectedPosition, tolerance);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Initialize step interlock add failed. step=" + stepNo +
                    ", group=" + groupName +
                    ", target=" + targetType + ":" + name +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        private static void AddInterlock(
            AxisInitializeStep step,
            string targetType,
            string name,
            string expectedState,
            string description,
            string positionName = null,
            double expectedPosition = 0.0,
            double tolerance = 0.01)
        {
            try
            {
                if (step == null)
                    return;

                if (step.Interlocks == null)
                    step.Interlocks = new List<AxisInitializeInterlockRule>();

                step.Interlocks.Add(new AxisInitializeInterlockRule
                {
                    Comment = "초기화 Step 실행 전 확인하는 안전 조건입니다.",
                    TargetType = targetType,
                    Name = name,
                    ExpectedState = expectedState,
                    PositionName = positionName,
                    ExpectedPosition = expectedPosition,
                    Tolerance = tolerance,
                    Enabled = true,
                    Description = description
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Initialize interlock add failed. target=" + targetType + ":" + name +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddActionOnlyStep(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string cylinderName,
            string command,
            string description,
            bool enabled = true)
        {
            try
            {
                if (plan == null)
                    return;

                var step = new AxisInitializeStep
                {
                    Comment = description,
                    StepNo = stepNo,
                    GroupName = groupName,
                    AxisNames = new List<string>(),
                    PreActions = new List<AxisInitializeAction>(),
                    PostActions = new List<AxisInitializeAction>(),
                    RunMode = AxisInitializeRunMode.Serial,
                    // Cylinder-only steps have no motion-axis group to stop. The common
                    // AllAxesStopped interlock and the step-specific prerequisites remain active.
                    InterlockGroup = "",
                    Interlocks = new List<AxisInitializeInterlockRule>(),
                    Enabled = enabled
                };

                step.PreActions.Add(new AxisInitializeAction
                {
                    Comment = "Excel sequence action row.",
                    TargetType = AxisInitializeInterlockTarget.Cylinder,
                    Name = cylinderName,
                    Command = command,
                    TimeoutMs = 0,
                    Enabled = enabled,
                    Description = description
                });

                plan.Steps.Add(step);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Action-only initialize step add failed. group=" + groupName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddAxisTeachingActionOnlyStep(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string axisName,
            string positionName,
            string description,
            bool enabled = true)
        {
            try
            {
                if (plan == null)
                    return;

                var step = new AxisInitializeStep
                {
                    Comment = description,
                    StepNo = stepNo,
                    GroupName = groupName,
                    AxisNames = new List<string>(),
                    PreActions = new List<AxisInitializeAction>(),
                    PostActions = new List<AxisInitializeAction>(),
                    RunMode = AxisInitializeRunMode.Serial,
                    // This action moves a real axis, so keep the stop target tied to that
                    // registered axis instead of the display-only step group name.
                    InterlockGroup = axisName,
                    Interlocks = new List<AxisInitializeInterlockRule>(),
                    Enabled = enabled
                };

                step.PreActions.Add(new AxisInitializeAction
                {
                    Comment = "독립 Step에서 실행되는 축 티칭 위치 이동입니다.",
                    TargetType = AxisInitializeInterlockTarget.Axis,
                    Name = axisName,
                    Command = AxisInitializeActionCommand.AxisTeachingMove,
                    PositionName = string.IsNullOrWhiteSpace(positionName) ? "AvoidPosition" : positionName,
                    TimeoutMs = 0,
                    Enabled = enabled,
                    Description = description
                });

                plan.Steps.Add(step);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Axis teaching action-only initialize step add failed. group=" + groupName +
                    ", axis=" + axisName +
                    ", position=" + positionName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddCustomActionOnlyStep(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string actionName,
            string description,
            bool enabled = true)
        {
            try
            {
                if (plan == null)
                    return;

                var step = new AxisInitializeStep
                {
                    Comment = description,
                    StepNo = stepNo,
                    GroupName = groupName,
                    AxisNames = new List<string>(),
                    PreActions = new List<AxisInitializeAction>(),
                    PostActions = new List<AxisInitializeAction>(),
                    RunMode = AxisInitializeRunMode.Serial,
                    // Custom action-only steps do not imply a motion-axis stop group.
                    // Any required axis group must be declared explicitly by the caller.
                    InterlockGroup = "",
                    Interlocks = new List<AxisInitializeInterlockRule>(),
                    Enabled = enabled
                };

                step.PreActions.Add(new AxisInitializeAction
                {
                    Comment = "조건에 따라 실행되는 초기화 전용 동작입니다.",
                    TargetType = AxisInitializeInterlockTarget.Machine,
                    Name = actionName,
                    Command = AxisInitializeActionCommand.CustomHook,
                    TimeoutMs = 0,
                    Enabled = enabled,
                    Description = description
                });

                plan.Steps.Add(step);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Custom action-only initialize step add failed. group=" + groupName +
                    ", action=" + actionName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddKnownStep(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used,
            int stepNo,
            string groupName,
            string runMode,
            string comment,
            params string[] axisNames)
        {
            try
            {
                if (plan == null || axisByName == null || used == null || axisNames == null)
                    return;

                var resolved = new List<string>();
                foreach (string axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;

                    BaseAxis axis;
                    if (!TryResolveAxis(axisByName, axisName, out axis) || axis == null)
                        continue;

                    if (used.Add(axis.Name))
                        resolved.Add(axis.Name);
                }

                if (resolved.Count == 0)
                    return;

                plan.Steps.Add(new AxisInitializeStep
                {
                    Comment = comment,
                    StepNo = stepNo,
                    GroupName = groupName,
                    AxisNames = resolved,
                    RunMode = runMode,
                    InterlockGroup = groupName,
                    Interlocks = new List<AxisInitializeInterlockRule>(),
                    Enabled = true
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Known initialize step add failed. group=" + groupName + ", error=" + ex.Message + " - Failed");
            }
        }

        internal static void AddKnownSingleStep(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            ISet<string> used,
            int stepNo,
            string groupName,
            string runMode,
            string comment,
            params string[] axisNameCandidates)
        {
            try
            {
                if (plan == null || axisByName == null || used == null || axisNameCandidates == null)
                    return;

                foreach (string axisName in axisNameCandidates)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;

                    BaseAxis axis;
                    if (!TryResolveAxis(axisByName, axisName, out axis) || axis == null)
                        continue;

                    if (!used.Add(axis.Name))
                        return;

                    plan.Steps.Add(new AxisInitializeStep
                    {
                        Comment = comment,
                        StepNo = stepNo,
                        GroupName = groupName,
                        AxisNames = new List<string> { axis.Name },
                        RunMode = runMode,
                        InterlockGroup = groupName,
                        Interlocks = new List<AxisInitializeInterlockRule>(),
                        Enabled = true
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Known single initialize step add failed. group=" + groupName + ", error=" + ex.Message + " - Failed");
            }
        }

        private static bool TryResolveAxis(
            IDictionary<string, BaseAxis> axisByName,
            string requestedName,
            out BaseAxis axis)
        {
            axis = null;
            try
            {
                if (axisByName == null || string.IsNullOrWhiteSpace(requestedName))
                    return false;

                string name = requestedName.Trim();
                if (axisByName.TryGetValue(name, out axis) && axis != null)
                    return true;

                string[] aliases = GetAxisAliases(name);
                if (aliases == null)
                    return false;

                foreach (string alias in aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;

                    if (axisByName.TryGetValue(alias.Trim(), out axis) && axis != null)
                        return true;
                }

                return false;
            }
            catch
            {
                axis = null;
                return false;
            }
        }

        private static string[] GetAxisAliases(string name)
        {
            switch (name)
            {
                // 구 FeederY 이름을 InputFeederY로 연결
                case "FeederY":
                    return new[] { "InputFeederY" };
                // InputFeederY 이름을 구 FeederY로 연결
                case "InputFeederY":
                    return new[] { "FeederY" };
                // 구 CameraX 이름을 InputVisionX로 연결
                case "CameraX":
                    return new[] { "InputVisionX" };
                // InputVisionX 이름을 구 CameraX로 연결
                case "InputVisionX":
                    return new[] { "CameraX" };
                // 구 ExpanderZ 이름을 InputExpandingZ로 연결
                case "ExpanderZ":
                    return new[] { "InputExpandingZ" };
                // InputExpandingZ 이름을 구 ExpanderZ로 연결
                case "InputExpandingZ":
                    return new[] { "ExpanderZ" };
                // 구 StageY 이름을 InputStageY로 연결
                case "StageY":
                    return new[] { "InputStageY" };
                // InputStageY 이름을 구 StageY로 연결
                case "InputStageY":
                    return new[] { "StageY" };
                // 구 StageT 이름을 InputStageT로 연결
                case "StageT":
                    return new[] { "InputStageT" };
                // InputStageT 이름을 구 StageT로 연결
                case "InputStageT":
                    return new[] { "StageT" };
                // 구 NeedleBlockX 이름을 NeedleX로 연결
                case "NeedleBlockX":
                    return new[] { "NeedleX" };
                // NeedleX 이름을 구 NeedleBlockX로 연결
                case "NeedleX":
                    return new[] { "NeedleBlockX" };
                // 구 FrontSideVisionY 이름을 FrontSideVisionY0로 연결
                case "FrontSideVisionY":
                    return new[] { "FrontSideVisionY0" };
                // FrontSideVisionY0 이름을 구 FrontSideVisionY로 연결
                case "FrontSideVisionY0":
                    return new[] { "FrontSideVisionY" };
                // 구 RearSideVisionY 이름을 RearSideVisionY0로 연결
                case "RearSideVisionY":
                    return new[] { "RearSideVisionY0" };
                // RearSideVisionY0 이름을 구 RearSideVisionY로 연결
                case "RearSideVisionY0":
                    return new[] { "RearSideVisionY" };
                // 구 GoodStage Z 이름을 OutputGoodStageZ로 연결
                case "GoodStage_StageZ":
                    return new[] { "OutputGoodStageZ" };
                // OutputGoodStageZ 이름을 구 GoodStage Z로 연결
                case "OutputGoodStageZ":
                    return new[] { "GoodStage_StageZ" };
                // 구 GoodStage Y 이름을 OutputGoodStageY로 연결
                case "GoodStage_StageY":
                    return new[] { "OutputGoodStageY" };
                // OutputGoodStageY 이름을 구 GoodStage Y로 연결
                case "OutputGoodStageY":
                    return new[] { "GoodStage_StageY" };
                // 구 NG Stage Y 이름을 OutputNGStageY로 연결
                case "NgStage_StageY":
                    return new[] { "OutputNGStageY" };
                // OutputNGStageY 이름을 구 NG Stage Y로 연결
                case "OutputNGStageY":
                    return new[] { "NgStage_StageY" };
                default:
                    return null;
            }
        }

        private static bool EnsureEditableHelp(AxisInitializePlan plan)
        {
            bool changed = false;
            try
            {
                if (plan == null)
                    return false;

                if (string.IsNullOrWhiteSpace(plan.Comment))
                {
                    plan.Comment = "Axis initialize plan. JSON 표준 주석은 사용할 수 없어서 Comment/Help 필드로 수정 기준을 남깁니다.";
                    changed = true;
                }

                if (plan.Help == null || plan.Help.Count == 0)
                {
                    plan.Help = new List<string>
                    {
                        "StepNo: 낮은 번호부터 실행합니다. 10, 20, 30처럼 여유 있게 번호를 두면 중간 삽입이 쉽습니다.",
                        "GroupName: INIT GROUP 버튼에서 선택 축의 UnitName과 같은 Step을 찾을 때 사용합니다.",
                        "AxisNames: 이 Step에서 HOME 초기화를 수행할 축 이름 목록입니다. Motion 화면의 KEY/축 이름과 맞춰야 합니다.",
                        "PreActions: 이 Step의 축 HOME 전에 실행할 실린더/커스텀 준비 동작입니다.",
                        "PostActions: 이 Step의 축 HOME 후에 실행할 실린더/커스텀 후처리 동작입니다.",
                        "Action Command: CylinderFwd, CylinderBwd, AxisTeachingMove, CustomHook을 사용할 수 있습니다.",
                        "AxisTeachingMove: TargetType=Axis, Name=축 이름, PositionName=AvoidPosition 같은 티칭 위치 이름을 사용합니다.",
                        "RunMode: Serial은 축을 순서대로 초기화하고, Parallel은 같은 Step의 축을 동시에 초기화합니다.",
                        "InterlockGroup: Step 시작 전에 해당 UnitName 그룹 또는 간섭 그룹 축을 Stop 합니다.",
                        "Interlocks: Step 실행 전에 확인할 조건입니다. Machine, Material, 축, 실린더, DI, Resource 조건을 넣을 수 있습니다.",
                        "Enabled: false이면 해당 Step 또는 Interlock 조건을 건너뜁니다."
                    };
                    changed = true;
                }

                if (plan.AllowedRunModes == null || plan.AllowedRunModes.Count == 0)
                {
                    plan.AllowedRunModes = new List<string>
                    {
                        AxisInitializeRunMode.Serial,
                        AxisInitializeRunMode.Parallel
                    };
                    changed = true;
                }

                if (plan.AllowedInterlockTargets == null || plan.AllowedInterlockTargets.Count == 0)
                {
                    plan.AllowedInterlockTargets = new List<string>
                    {
                        AxisInitializeInterlockTarget.Axis,
                        AxisInitializeInterlockTarget.Cylinder,
                        AxisInitializeInterlockTarget.DigitalInput,
                        AxisInitializeInterlockTarget.Resource,
                        AxisInitializeInterlockTarget.Machine,
                        AxisInitializeInterlockTarget.Material
                    };
                    changed = true;
                }

                if (plan.AllowedInterlockStates == null || plan.AllowedInterlockStates.Count == 0)
                {
                    plan.AllowedInterlockStates = new List<string>
                    {
                        "Axis: ServoOn, HomeDone, AlarmOff, Stopped, AtPosition, AtOrBelowPosition, SharedRailHomeClear",
                        "Cylinder: Fwd, Bwd, SafeForStageMove",
                        "DigitalInput: On, Off",
                        "Resource: AllOk",
                        "Machine: AutoStopped, ManualStopped, AllAxesStopped",
                        "Material: Empty"
                    };
                    changed = true;
                }

                if (plan.InterlockExamples == null || plan.InterlockExamples.Count == 0)
                {
                    plan.InterlockExamples = new List<AxisInitializeInterlockRule>
                    {
                        new AxisInitializeInterlockRule
                        {
                            Comment = "예시: 특정 축 초기화 전에 다른 축 Alarm이 없어야 하는 경우",
                            TargetType = AxisInitializeInterlockTarget.Axis,
                            Name = "StageY",
                            ExpectedState = AxisInitializeInterlockState.AlarmOff,
                            Enabled = false,
                            Description = "예시입니다. 실제 사용 시 Name을 실제 축 이름으로 변경하고 Enabled=true로 바꾸세요."
                        },
                        new AxisInitializeInterlockRule
                        {
                            Comment = "예시: 특정 축 초기화 전에 클램프 실린더가 후진이어야 하는 경우",
                            TargetType = AxisInitializeInterlockTarget.Cylinder,
                            Name = "ClampCylinder",
                            ExpectedState = AxisInitializeInterlockState.Bwd,
                            Enabled = false,
                            Description = "예시입니다. 실제 사용 시 Name을 실제 실린더 이름으로 변경하고 Enabled=true로 바꾸세요."
                        },
                        new AxisInitializeInterlockRule
                        {
                            Comment = "예시: 문 닫힘 센서 같은 DI가 ON이어야 하는 경우",
                            TargetType = AxisInitializeInterlockTarget.DigitalInput,
                            Name = "DoorCloseSensor",
                            ExpectedState = AxisInitializeInterlockState.On,
                            Enabled = false,
                            Description = "예시입니다. 실제 사용 시 Name을 실제 DI 이름으로 변경하고 Enabled=true로 바꾸세요."
                        },
                        new AxisInitializeInterlockRule
                        {
                            Comment = "예시: CDA/Vacuum 같은 Resource 상태가 모두 정상이어야 하는 경우",
                            TargetType = AxisInitializeInterlockTarget.Resource,
                            Name = "Resources",
                            ExpectedState = AxisInitializeInterlockState.AllOk,
                            Enabled = false,
                            Description = "예시입니다. 실제 사용 시 Enabled=true로 바꾸세요."
                        }
                    };
                    changed = true;
                }

                if (plan.Steps != null)
                {
                    foreach (var step in plan.Steps)
                    {
                        if (step == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(step.Comment))
                        {
                            step.Comment = "StepNo 순서대로 실행됩니다. AxisNames는 이 Step에서 HOME 잡을 축 이름입니다.";
                            changed = true;
                        }

                        if (step.AxisNames == null)
                        {
                            step.AxisNames = new List<string>();
                            changed = true;
                        }

                        if (step.PreActions == null)
                        {
                            step.PreActions = new List<AxisInitializeAction>();
                            changed = true;
                        }

                        if (step.PostActions == null)
                        {
                            step.PostActions = new List<AxisInitializeAction>();
                            changed = true;
                        }

                        if (step.Interlocks == null)
                        {
                            step.Interlocks = new List<AxisInitializeInterlockRule>();
                            changed = true;
                        }
                    }
                }

                return changed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanHelp",
                    "Axis initialize plan help update failed: " + ex.Message + " - Failed");
                return changed;
            }
        }
    }
}
