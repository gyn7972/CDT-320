using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common;
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
        [DataMember] public string InterlockGroup { get; set; }
        [DataMember] public List<AxisInitializeInterlockRule> Interlocks { get; set; } =
            new List<AxisInitializeInterlockRule>();
        [DataMember] public bool Enabled { get; set; } = true;
    }

    [DataContract]
    public class AxisInitializeAction
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public string TargetType { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string Command { get; set; }
        [DataMember] public int TimeoutMs { get; set; } = 0;
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string Description { get; set; }
    }

    [DataContract]
    public class AxisInitializeInterlockRule
    {
        [DataMember] public string Comment { get; set; }
        [DataMember] public string TargetType { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string ExpectedState { get; set; }
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string Description { get; set; }
    }

    public static class AxisInitializeInterlockTarget
    {
        public const string Axis = "Axis";
        public const string Cylinder = "Cylinder";
        public const string DigitalInput = "DigitalInput";
        public const string Resource = "Resource";
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
    }

    public static class AxisInitializeActionCommand
    {
        public const string CylinderFwd = "CylinderFwd";
        public const string CylinderBwd = "CylinderBwd";
        public const string CustomHook = "CustomHook";
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
            finally
            {
            }
        }
    }

    public static class AxisInitializePlanStore
    {
        private const int CurrentDefaultVersion = 2;
        public static string RootDir => @"D:\CDT-320";
        public static string Dir => Path.Combine(RootDir, "Config");
        public static string PlanPath => Path.Combine(Dir, "axis_initialize_plan.json");
        public static string BackupPath => Path.Combine(Dir, "axis_initialize_plan.bak");

        public static AxisInitializePlan LoadOrCreateDefault(IEnumerable<BaseAxis> axes)
        {
            try
            {
                AxisInitializePlan plan = Load();
                if (plan != null && plan.Steps != null && plan.Steps.Count > 0)
                {
                    if (plan.Version < CurrentDefaultVersion)
                    {
                        plan = CreateDefault(axes);
                        EnsureEditableHelp(plan);
                        Save(plan);
                        Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                            "Axis initialize plan upgraded. file=" + PlanPath + " - Ok");
                        return plan;
                    }

                    if (EnsureEditableHelp(plan))
                        Save(plan);

                    return plan;
                }

                plan = CreateDefault(axes);
                EnsureEditableHelp(plan);
                Save(plan);
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Axis initialize plan created. file=" + PlanPath + " - Ok");
                return plan;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Axis initialize plan load/create failed: " + ex.Message + " - Failed");
                return CreateDefault(axes);
            }
            finally
            {
            }
        }

        public static AxisInitializePlan Load()
        {
            try
            {
                if (!File.Exists(PlanPath))
                    return null;

                using (var fs = File.OpenRead(PlanPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AxisInitializePlan));
                    return (AxisInitializePlan)serializer.ReadObject(fs);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Axis initialize plan load failed: " + PlanPath + " / " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static bool Save(AxisInitializePlan plan)
        {
            try
            {
                if (plan == null)
                {
                    Log.Write("Main", "SYSTEM", "AxisInitializePlanSave",
                        "Axis initialize plan save failed: plan is null. - Failed");
                    return false;
                }

                Directory.CreateDirectory(Dir);
                plan.SavedAt = DateTime.Now;
                if (plan.Steps == null)
                    plan.Steps = new List<AxisInitializeStep>();
                EnsureEditableHelp(plan);

                string tmp = PlanPath + ".tmp";
                using (var fs = File.Create(tmp))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AxisInitializePlan));
                    serializer.WriteObject(fs, plan);
                }

                if (File.Exists(PlanPath))
                {
                    try
                    {
                        if (File.Exists(BackupPath)) File.Delete(BackupPath);
                        File.Move(PlanPath, BackupPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Main", "SYSTEM", "AxisInitializePlanBackup",
                            "Axis initialize plan backup failed: " + BackupPath + " / " + ex.Message + " - Failed");
                    }
                    finally
                    {
                    }
                }

                if (File.Exists(PlanPath)) File.Delete(PlanPath);
                File.Move(tmp, PlanPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanSave",
                    "Axis initialize plan save failed: " + PlanPath + " / " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static AxisInitializePlan CreateDefault(IEnumerable<BaseAxis> axes)
        {
            var plan = new AxisInitializePlan
            {
                Comment = "CDT-320 automatic axis initialize sequence. JSON 표준 주석은 사용할 수 없어서 Comment/Help 필드로 수정 기준을 남깁니다.",
                Version = CurrentDefaultVersion,
                SavedAt = DateTime.Now,
                Steps = new List<AxisInitializeStep>()
            };

            try
            {
                var cleanAxes = (axes ?? Enumerable.Empty<BaseAxis>())
                    .Where(x => x != null)
                    .OrderBy(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue)
                    .ThenBy(x => x.Name)
                    .ToList();

                var axisByName = cleanAxes
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                AddKnownStep(plan, axisByName, used, 10, "PickerZ",
                    AxisInitializeRunMode.Parallel,
                    "Picker Z axes home first to secure vertical clearance.",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

                AddKnownStep(plan, axisByName, used, 10, "InputStageNeedleZ",
                    AxisInitializeRunMode.Parallel,
                    "NeedleZ/EjectPinZ home first with vertical clearance group.",
                    "NeedleZ", "EjectPinZ");

                AddKnownStep(plan, axisByName, used, 10, "OutputStageZ",
                    AxisInitializeRunMode.Parallel,
                    "Output stage Z axes home before Y axes.",
                    "GoodStage_StageZ", "NgStage_StageZ");

                AddKnownStep(plan, axisByName, used, 20, "PickerT",
                    AxisInitializeRunMode.Parallel,
                    "Picker T axes home after Picker Z clearance.",
                    "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3",
                    "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3");

                AddKnownStep(plan, axisByName, used, 20, "InputStageZ",
                    AxisInitializeRunMode.Serial,
                    "ExpanderZ home after needle vertical axes.",
                    "ExpanderZ");

                AddKnownStep(plan, axisByName, used, 30, "FrontPickerY",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerY home, then move to Avoid in axis post hook.",
                    "FrontPickerY");

                AddKnownStep(plan, axisByName, used, 40, "RearPickerY",
                    AxisInitializeRunMode.Serial,
                    "RearPickerY home, then move to Avoid in axis post hook.",
                    "RearPickerY");

                AddKnownStep(plan, axisByName, used, 40, "Vision",
                    AxisInitializeRunMode.Parallel,
                    "Side vision axes home.",
                    "FrontSideVisionY", "RearSideVisionY");

                AddActionOnlyStep(plan, 50, "InputFeederClamp", "InputFeederClamp", AxisInitializeActionCommand.CylinderBwd,
                    "Input feeder must be unclamped before InputFeederY home.");

                AddActionOnlyStep(plan, 60, "InputFeederLift", "InputFeederLift", AxisInitializeActionCommand.CylinderFwd,
                    "Input feeder must be up before InputFeederY home.");

                AddKnownStep(plan, axisByName, used, 701, "InputFeeder",
                    AxisInitializeRunMode.Serial,
                    "InputFeederY home. SharedRailX/InputFeeder relation is checked in prepare hook.",
                    "FeederY");

                AddKnownStep(plan, axisByName, used, 702, "SharedRailX",
                    AxisInitializeRunMode.Parallel,
                    "Common X rail axes home after InputFeeder relation check.",
                    "CameraX", "FrontPickerX", "RearPickerX", "OutputVisionX");

                AddKnownStep(plan, axisByName, used, 80, "InputCassette",
                    AxisInitializeRunMode.Serial,
                    "Input cassette lifter Z home after InputFeederY is safe.",
                    "InputLifterZ");

                AddKnownStep(plan, axisByName, used, 80, "InputStage",
                    AxisInitializeRunMode.Parallel,
                    "StageY/StageT home. StageY moves to Avoid in axis post hook before NeedleBlockX.",
                    "StageY", "StageT");

                AddKnownStep(plan, axisByName, used, 90, "InputStageNeedleX",
                    AxisInitializeRunMode.Serial,
                    "NeedleBlockX home after StageY home and Avoid move.",
                    "NeedleBlockX");

                AddActionOnlyStep(plan, 90, "OutputFeederClamp", "OutputFeederClamp", AxisInitializeActionCommand.CylinderBwd,
                    "Output feeder must be unclamped before OutputFeederY home.");

                AddActionOnlyStep(plan, 90, "OutputFeederLift", "OutputFeederLift", AxisInitializeActionCommand.CylinderFwd,
                    "Output feeder must be up before OutputFeederY home.");

                AddKnownStep(plan, axisByName, used, 901, "OutputFeeder",
                    AxisInitializeRunMode.Serial,
                    "OutputFeederY home. SharedRailX/OutputFeeder relation is checked in prepare hook.",
                    "OutputFeederY");

                AddKnownStepAllowDuplicate(plan, axisByName, 902, "SharedRailXOutput",
                    AxisInitializeRunMode.Parallel,
                    "Shared rail X output-side relation check step. Axes already homed are skipped by runtime.",
                    "CameraX", "FrontPickerX", "RearPickerX", "OutputVisionX");

                AddActionOnlyStep(plan, 100, "NGBinGuideClamp", "NGBinGuideClamp", AxisInitializeActionCommand.CylinderBwd,
                    "NG bin guide clamp UnClamp. Disabled until field direction is confirmed.", false);
                AddActionOnlyStep(plan, 110, "NGBinGuideClampLift", "NGBinGuideClampLift", AxisInitializeActionCommand.CylinderBwd,
                    "NG bin guide clamp lift UP. Disabled until field direction is confirmed.", false);
                AddActionOnlyStep(plan, 120, "NGBinGuideLift", "NGBinGuideLift", AxisInitializeActionCommand.CylinderBwd,
                    "NG bin guide lift UP. Disabled until field direction is confirmed.", false);
                AddActionOnlyStep(plan, 130, "GoodBinGuideClamp", "GoodBinGuideClamp", AxisInitializeActionCommand.CylinderBwd,
                    "Good bin guide clamp UnClamp. Disabled until field direction is confirmed.", false);
                AddActionOnlyStep(plan, 140, "GoodBinGuideClampLift", "GoodBinGuideClampLift", AxisInitializeActionCommand.CylinderBwd,
                    "Good bin guide clamp lift UP. Disabled until field direction is confirmed.", false);
                AddActionOnlyStep(plan, 150, "GoodBinGuideLift", "GoodBinGuideLift", AxisInitializeActionCommand.CylinderBwd,
                    "Good bin guide lift DOWN. Disabled until field direction is confirmed.", false);

                AddKnownStep(plan, axisByName, used, 160, "OutputStage",
                    AxisInitializeRunMode.Parallel,
                    "Output stage Y axes home after feeder relation and bin guide template steps.",
                    "GoodStage_StageY", "NgStage_StageY");

                AddKnownStep(plan, axisByName, used, 170, "OutputCassette",
                    AxisInitializeRunMode.Serial,
                    "Output cassette lifter Z home after OutputFeederY is safe.",
                    "OutputLifterZ");

                AddRemainingGroupedSteps(plan, cleanAxes, used, 200);
                AddDisabledCylinderTemplateStep(plan);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Axis initialize default plan create failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return plan;
        }

        private static void AddActionOnlyStep(
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
                    InterlockGroup = groupName,
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
            finally
            {
            }
        }

        private static void AddKnownStepAllowDuplicate(
            AxisInitializePlan plan,
            IDictionary<string, BaseAxis> axisByName,
            int stepNo,
            string groupName,
            string runMode,
            string comment,
            params string[] axisNames)
        {
            try
            {
                if (plan == null || axisByName == null || axisNames == null)
                    return;

                var resolved = new List<string>();
                foreach (string axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;

                    BaseAxis axis;
                    if (axisByName.TryGetValue(axisName.Trim(), out axis) && axis != null)
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
                    "Duplicate initialize step add failed. group=" + groupName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void AddPreCylinderAction(
            AxisInitializePlan plan,
            int stepNo,
            string cylinderName,
            string command,
            string description)
        {
            try
            {
                if (plan == null || string.IsNullOrWhiteSpace(cylinderName))
                    return;

                AxisInitializeStep step = plan.Steps
                    .FirstOrDefault(x => x != null && x.StepNo == stepNo);
                if (step == null)
                    return;

                if (step.PreActions == null)
                    step.PreActions = new List<AxisInitializeAction>();

                step.PreActions.Add(new AxisInitializeAction
                {
                    Comment = "Step 시작 전에 실행되는 실린더 준비 동작입니다.",
                    TargetType = AxisInitializeInterlockTarget.Cylinder,
                    Name = cylinderName,
                    Command = command,
                    TimeoutMs = 0,
                    Enabled = true,
                    Description = description
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Pre cylinder action add failed. cylinder=" + cylinderName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void AddDisabledCylinderTemplateStep(AxisInitializePlan plan)
        {
            try
            {
                if (plan == null || plan.Steps == null)
                    return;

                var actions = new List<AxisInitializeAction>
                {
                    CreateDisabledCylinderAction("ReticleLift", AxisInitializeActionCommand.CylinderBwd, "Reticle lift safe direction을 현장 기준에 맞게 Fwd/Bwd로 선택하세요."),
                    CreateDisabledCylinderAction("ReticleSideSlideFront", AxisInitializeActionCommand.CylinderBwd, "Front reticle side slide safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("ReticleSideSlideRear", AxisInitializeActionCommand.CylinderBwd, "Rear reticle side slide safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("NGBinGuideLift", AxisInitializeActionCommand.CylinderBwd, "NG bin guide lift safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("NGBinGuideClampLift", AxisInitializeActionCommand.CylinderBwd, "NG bin clamp lift safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("NGBinGuideClamp", AxisInitializeActionCommand.CylinderBwd, "NG bin clamp safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("GoodBinGuideLift", AxisInitializeActionCommand.CylinderBwd, "Good bin guide lift safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("GoodBinGuideClampLift", AxisInitializeActionCommand.CylinderBwd, "Good bin clamp lift safe direction을 현장 기준에 맞게 선택하세요."),
                    CreateDisabledCylinderAction("GoodBinGuideClamp", AxisInitializeActionCommand.CylinderBwd, "Good bin clamp safe direction을 현장 기준에 맞게 선택하세요.")
                };

                plan.Steps.Add(new AxisInitializeStep
                {
                    Comment = "실린더 초기화 템플릿 Step입니다. 필요한 항목만 Enabled=true로 바꾸고 StepNo를 원하는 위치로 조정하세요.",
                    StepNo = 900,
                    GroupName = "CylinderTemplate",
                    AxisNames = new List<string>(),
                    PreActions = actions,
                    PostActions = new List<AxisInitializeAction>(),
                    RunMode = AxisInitializeRunMode.Serial,
                    InterlockGroup = "CylinderTemplate",
                    Interlocks = new List<AxisInitializeInterlockRule>(),
                    Enabled = false
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Cylinder template step add failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static AxisInitializeAction CreateDisabledCylinderAction(
            string cylinderName,
            string command,
            string description)
        {
            return new AxisInitializeAction
            {
                Comment = "기본은 비활성입니다. 현장 안전 방향 확인 후 Enabled=true로 바꾸세요.",
                TargetType = AxisInitializeInterlockTarget.Cylinder,
                Name = cylinderName,
                Command = command,
                TimeoutMs = 0,
                Enabled = false,
                Description = description
            };
        }

        private static void AddKnownStep(
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
                    if (!axisByName.TryGetValue(axisName.Trim(), out axis) || axis == null)
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
            finally
            {
            }
        }

        private static void AddRemainingGroupedSteps(
            AxisInitializePlan plan,
            IList<BaseAxis> cleanAxes,
            ISet<string> used,
            int firstStepNo)
        {
            try
            {
                if (plan == null || cleanAxes == null || used == null)
                    return;

                var remainingGroups = cleanAxes
                    .Where(x => x != null && !used.Contains(x.Name))
                    .GroupBy(x => !string.IsNullOrWhiteSpace(x.Setup != null ? x.Setup.UnitName : "")
                        ? x.Setup.UnitName
                        : "Ungrouped")
                    .OrderBy(g => g.Min(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue))
                    .ThenBy(g => g.Key);

                int stepNo = firstStepNo;
                foreach (var group in remainingGroups)
                {
                    var names = group
                        .OrderBy(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue)
                        .ThenBy(x => x.Name)
                        .Select(x => x.Name)
                        .Where(x => used.Add(x))
                        .ToList();

                    if (names.Count == 0)
                        continue;

                    plan.Steps.Add(new AxisInitializeStep
                    {
                        Comment = "Known sequence에 없는 축을 UnitName 기준으로 보존한 자동 Step입니다.",
                        StepNo = stepNo,
                        GroupName = group.Key,
                        AxisNames = names,
                        RunMode = AxisInitializeRunMode.Serial,
                        InterlockGroup = group.Key,
                        Interlocks = new List<AxisInitializeInterlockRule>(),
                        Enabled = true
                    });
                    stepNo += 10;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Remaining initialize steps add failed: " + ex.Message + " - Failed");
            }
            finally
            {
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
                        "Action Command: CylinderFwd, CylinderBwd, CustomHook을 사용할 수 있습니다.",
                        "RunMode: Serial은 축을 순서대로 초기화하고, Parallel은 같은 Step의 축을 동시에 초기화합니다.",
                        "InterlockGroup: Step 시작 전에 해당 UnitName 그룹 또는 간섭 그룹 축을 Stop 합니다.",
                        "Interlocks: Step 실행 전에 확인할 조건입니다. 축, 실린더, DI, Resource 조건을 넣을 수 있습니다.",
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
                        AxisInitializeInterlockTarget.Resource
                    };
                    changed = true;
                }

                if (plan.AllowedInterlockStates == null || plan.AllowedInterlockStates.Count == 0)
                {
                    plan.AllowedInterlockStates = new List<string>
                    {
                        "Axis: ServoOn, HomeDone, AlarmOff",
                        "Cylinder: Fwd, Bwd",
                        "DigitalInput: On, Off",
                        "Resource: AllOk"
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
            finally
            {
            }
        }
    }
}
