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
        [DataMember] public string PositionName { get; set; }
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
        public const string AxisTeachingMove = "AxisTeachingMove";
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
            finally
            {
            }
        }
    }

    public static class AxisInitializePlanStore
    {
        private const int CurrentDefaultVersion = 6;
        public static string RootDir => @"D:\CDT-320";
        public static string Dir => Path.Combine(RootDir, "Config");
        public static string PlanPath => Path.Combine(Dir, "axis_initialize_plan.json");
        public static string BackupPath => Path.Combine(Dir, "axis_initialize_plan.bak");

        public static AxisInitializePlan LoadOrCreateDefault(IEnumerable<BaseAxis> axes)
        {
            try
            {
                AxisInitializePlan plan = CreateDefault(axes);
                EnsureEditableHelp(plan);
                EnsureRequiredInitializeActions(plan);
                Log.Write("Main", "SYSTEM", "AxisInitializePlanLoad",
                    "Axis initialize plan loaded from CreateDefault. saved file ignored while sequence steps are being edited. file=" + PlanPath + " - Ok");
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

            //Test 완료 하고 불러오자. 
            return null;
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
            //Test 완료 하고 저장하자. 
            return true;

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
                EnsureRequiredInitializeActions(plan);

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

        // 여기서 초기화 순서 정의.!
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

                // Z/T/Y/X 초기화는 우선 전부 축 단위 순차 스텝으로 표시/실행한다.
                AddKnownStep(plan, axisByName, used, 10, "FrontPickerZ0",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerZ0 home first to secure vertical clearance.",
                    "FrontPickerZ0");

                AddKnownStep(plan, axisByName, used, 20, "FrontPickerZ1",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerZ1 home first to secure vertical clearance.",
                    "FrontPickerZ1");

                AddKnownStep(plan, axisByName, used, 30, "FrontPickerZ2",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerZ2 home first to secure vertical clearance.",
                    "FrontPickerZ2");

                AddKnownStep(plan, axisByName, used, 40, "FrontPickerZ3",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerZ3 home first to secure vertical clearance.",
                    "FrontPickerZ3");

                AddKnownStep(plan, axisByName, used, 50, "RearPickerZ0",
                    AxisInitializeRunMode.Serial,
                    "RearPickerZ0 home first to secure vertical clearance.",
                    "RearPickerZ0");

                AddKnownStep(plan, axisByName, used, 60, "RearPickerZ1",
                    AxisInitializeRunMode.Serial,
                    "RearPickerZ1 home first to secure vertical clearance.",
                    "RearPickerZ1");

                AddKnownStep(plan, axisByName, used, 70, "RearPickerZ2",
                    AxisInitializeRunMode.Serial,
                    "RearPickerZ2 home first to secure vertical clearance.",
                    "RearPickerZ2");

                AddKnownStep(plan, axisByName, used, 80, "RearPickerZ3",
                    AxisInitializeRunMode.Serial,
                    "RearPickerZ3 home first to secure vertical clearance.",
                    "RearPickerZ3");

                AddKnownStep(plan, axisByName, used, 90, "NeedleZ",
                    AxisInitializeRunMode.Serial,
                    "NeedleZ home first with vertical clearance.",
                    "NeedleZ");

                AddKnownStep(plan, axisByName, used, 100, "EjectPinZ",
                    AxisInitializeRunMode.Serial,
                    "EjectPinZ home first with vertical clearance.",
                    "EjectPinZ");

                AddActionOnlyStep(plan, 110, "OutputStageZClampLift", "NGBinGuideClampLift", AxisInitializeActionCommand.CylinderFwd,
                    "NG bin clamp lift moves up before OutputGoodStageZ home.");

                AddKnownSingleStep(plan, axisByName, used, 120, "OutputStageZ",
                    AxisInitializeRunMode.Serial,
                    "Output good stage Z home before Y axes. NG stage has Y axis only.",
                    "OutputGoodStageZ", "GoodStage_StageZ");

                AddKnownSingleStep(plan, axisByName, used, 130, "InputStageZ",
                    AxisInitializeRunMode.Serial,
                    "InputExpandingZ home after needle vertical axes.",
                    "InputExpandingZ", "ExpanderZ");

                AddKnownStep(plan, axisByName, used, 140, "FrontPickerT0",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerT0 home after Picker Z clearance.",
                    "FrontPickerT0");

                AddKnownStep(plan, axisByName, used, 150, "FrontPickerT1",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerT1 home after Picker Z clearance.",
                    "FrontPickerT1");

                AddKnownStep(plan, axisByName, used, 160, "FrontPickerT2",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerT2 home after Picker Z clearance.",
                    "FrontPickerT2");

                AddKnownStep(plan, axisByName, used, 170, "FrontPickerT3",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerT3 home after Picker Z clearance.",
                    "FrontPickerT3");

                AddKnownStep(plan, axisByName, used, 180, "RearPickerT0",
                    AxisInitializeRunMode.Serial,
                    "RearPickerT0 home after Picker Z clearance.",
                    "RearPickerT0");

                AddKnownStep(plan, axisByName, used, 190, "RearPickerT1",
                    AxisInitializeRunMode.Serial,
                    "RearPickerT1 home after Picker Z clearance.",
                    "RearPickerT1");

                AddKnownStep(plan, axisByName, used, 200, "RearPickerT2",
                    AxisInitializeRunMode.Serial,
                    "RearPickerT2 home after Picker Z clearance.",
                    "RearPickerT2");

                AddKnownStep(plan, axisByName, used, 210, "RearPickerT3",
                    AxisInitializeRunMode.Serial,
                    "RearPickerT3 home after Picker Z clearance.",
                    "RearPickerT3");

                AddKnownStep(plan, axisByName, used, 220, "FrontPickerY",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerY home, then move to Avoid in axis post hook.",
                    "FrontPickerY");

                AddAxisTeachingActionOnlyStep(plan, 230, "FrontPickerYAvoid", "FrontPickerY", "AvoidPosition",
                    "FrontPickerY moves to Avoid after home.");

                AddKnownStep(plan, axisByName, used, 240, "RearPickerY",
                    AxisInitializeRunMode.Serial,
                    "RearPickerY home, then move to Avoid in axis post hook.",
                    "RearPickerY");

                AddAxisTeachingActionOnlyStep(plan, 250, "RearPickerYAvoid", "RearPickerY", "AvoidPosition",
                    "RearPickerY moves to Avoid after home.");

                AddKnownSingleStep(plan, axisByName, used, 260, "FrontSideVisionY",
                    AxisInitializeRunMode.Serial,
                    "Front side vision Y home.",
                    "FrontSideVisionY0", "FrontSideVisionY");

                AddKnownSingleStep(plan, axisByName, used, 270, "RearSideVisionY",
                    AxisInitializeRunMode.Serial,
                    "Rear side vision Y home.",
                    "RearSideVisionY0", "RearSideVisionY");

                //Reticle 순서 뒤로 빠지고 다운해야함.
                AddActionOnlyStep(plan, 280, "ReticleSideSlideRear", "ReticleSideSlideRear", AxisInitializeActionCommand.CylinderBwd,
                    "Reticle side slide rear moves backward before shared rail/input feeder initialize.");

                AddActionOnlyStep(plan, 290, "ReticleSideSlideFront", "ReticleSideSlideFront", AxisInitializeActionCommand.CylinderBwd,
                    "Reticle side slide front moves backward before shared rail/input feeder initialize.");

                AddActionOnlyStep(plan, 300, "ReticleLift", "ReticleLift", AxisInitializeActionCommand.CylinderBwd,
                    "Reticle lift moves backward before shared rail/input feeder initialize.");

                AddKnownSingleStep(plan, axisByName, used, 310, "InputStageY",
                    AxisInitializeRunMode.Serial,
                    "InputStageY home. InputStageY moves to Avoid before NeedleX.",
                    "InputStageY", "StageY");

                AddAxisTeachingActionOnlyStep(plan, 320, "InputStageYAvoid", "InputStageY", "AvoidPosition",
                    "InputStageY moves to Avoid after home.");

                AddKnownSingleStep(plan, axisByName, used, 330, "InputStageT",
                    AxisInitializeRunMode.Serial,
                    "InputStageT home after InputStageY home.",
                    "InputStageT", "StageT");

                AddKnownSingleStep(plan, axisByName, used, 340, "InputStageNeedleX",
                    AxisInitializeRunMode.Serial,
                    "NeedleX home after InputStageY home and Avoid move.",
                    "NeedleX", "NeedleBlockX");

                AddActionOnlyStep(plan, 350, "InputFeederClamp", "InputFeederClamp", AxisInitializeActionCommand.CylinderBwd,
                    "Input feeder must be unclamped before InputFeederY home.");

                AddActionOnlyStep(plan, 360, "InputFeederLift", "InputFeederLift", AxisInitializeActionCommand.CylinderBwd,
                    "Input feeder must be Down before InputFeederY home.");

                AddKnownSingleStep(plan, axisByName, used, 370, "InputFeeder",
                    AxisInitializeRunMode.Serial,
                    "InputFeederY home. SharedRailX/InputFeeder relation is checked in prepare hook.",
                    "InputFeederY", "FeederY");

                AddActionOnlyStep(plan, 380, "InputFeederLiftDown", "InputFeederLift", AxisInitializeActionCommand.CylinderBwd,
                    "Input feeder lift moves down after InputFeederY home.");

                AddKnownStep(plan, axisByName, used, 390, "InputCassette",
                    AxisInitializeRunMode.Serial,
                    "Input cassette lifter Z home before InputFeederY. Lifter moves to Avoid in axis post hook.",
                    "InputLifterZ");

                AddKnownSingleStep(plan, axisByName, used, 400, "InputVisionX",
                    AxisInitializeRunMode.Serial,
                    "InputVisionX home after InputFeederY home and lift down.",
                    "InputVisionX", "CameraX");

                AddKnownStep(plan, axisByName, used, 410, "FrontPickerX",
                    AxisInitializeRunMode.Serial,
                    "FrontPickerX home after InputVisionX.",
                    "FrontPickerX");

                AddKnownStep(plan, axisByName, used, 420, "RearPickerX",
                    AxisInitializeRunMode.Serial,
                    "RearPickerX home after FrontPickerX.",
                    "RearPickerX");

                AddActionOnlyStep(plan, 430, "OutputFeederClamp", "OutputFeederClamp", AxisInitializeActionCommand.CylinderBwd,
                    "Output feeder must be unclamped before OutputFeederY home.");

                AddActionOnlyStep(plan, 440, "OutputFeederLift", "OutputFeederLift", AxisInitializeActionCommand.CylinderFwd,
                    "Output feeder must be up before OutputFeederY home.");

                AddKnownStep(plan, axisByName, used, 450, "OutputFeeder",
                    AxisInitializeRunMode.Serial,
                    "OutputFeederY home. SharedRailX/OutputFeeder relation is checked in prepare hook.",
                    "OutputFeederY");

                AddActionOnlyStep(plan, 460, "OutputFeederLiftDown", "OutputFeederLift", AxisInitializeActionCommand.CylinderBwd,
                    "Output feeder must be down after OutputFeederY home.");

                AddKnownStep(plan, axisByName, used, 470, "SharedRailXOutput",
                    AxisInitializeRunMode.Serial,
                    "OutputVisionX home after OutputFeederY home and lift down.",
                    "OutputVisionX");

                AddKnownSingleStep(plan, axisByName, used, 480, "OutputGoodStageY",
                    AxisInitializeRunMode.Serial,
                    "Output good stage Y home after feeder relation.",
                    "OutputGoodStageY", "GoodStage_StageY");

                AddKnownSingleStep(plan, axisByName, used, 490, "OutputNGStageY",
                    AxisInitializeRunMode.Serial,
                    "Output NG stage Y home after good stage Y.",
                    "OutputNGStageY", "NgStage_StageY");

                AddKnownStep(plan, axisByName, used, 500, "OutputCassette",
                    AxisInitializeRunMode.Serial,
                    "Output cassette lifter Z home after OutputFeederY is safe.",
                    "OutputLifterZ");

                //AddRemainingGroupedSteps(plan, cleanAxes, used, 330);
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

        private static void AddAxisTeachingActionOnlyStep(
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
                    InterlockGroup = groupName,
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
                var duplicateGuard = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;

                    BaseAxis axis;
                    if (TryResolveAxis(axisByName, axisName, out axis) && axis != null &&
                        duplicateGuard.Add(axis.Name))
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

        private static void AddPreCylinderAction(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string cylinderName,
            string command,
            string description)
        {
            try
            {
                if (plan == null || string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(cylinderName))
                    return;

                AxisInitializeStep step = plan.Steps
                    .FirstOrDefault(x => x != null &&
                                         x.StepNo == stepNo &&
                                         string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    return;

                AddPreCylinderAction(step, cylinderName, command, description);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Pre cylinder action add failed. group=" + groupName +
                    ", cylinder=" + cylinderName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void AddPreCylinderAction(
            AxisInitializeStep step,
            string cylinderName,
            string command,
            string description)
        {
            if (step == null || string.IsNullOrWhiteSpace(cylinderName))
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

        private static void AddPostCylinderAction(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string cylinderName,
            string command,
            string description)
        {
            try
            {
                if (plan == null || string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(cylinderName))
                    return;

                AxisInitializeStep step = plan.Steps
                    .FirstOrDefault(x => x != null &&
                                         x.StepNo == stepNo &&
                                         string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    return;

                AddPostCylinderAction(step, cylinderName, command, description);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Post cylinder action add failed. group=" + groupName +
                    ", cylinder=" + cylinderName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void AddPostCylinderAction(
            AxisInitializeStep step,
            string cylinderName,
            string command,
            string description)
        {
            if (step == null || string.IsNullOrWhiteSpace(cylinderName))
                return;

            if (step.PostActions == null)
                step.PostActions = new List<AxisInitializeAction>();

            step.PostActions.Add(new AxisInitializeAction
            {
                Comment = "Step HOME 완료 후 실행되는 실린더 후처리 동작입니다.",
                TargetType = AxisInitializeInterlockTarget.Cylinder,
                Name = cylinderName,
                Command = command,
                TimeoutMs = 0,
                Enabled = true,
                Description = description
            });
        }

        private static void AddPostAxisTeachingAction(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string axisName,
            string positionName,
            string description)
        {
            try
            {
                if (plan == null || string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(axisName))
                    return;

                AxisInitializeStep step = plan.Steps
                    .FirstOrDefault(x => x != null &&
                                         x.StepNo == stepNo &&
                                         string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    return;

                AddPostAxisTeachingAction(step, axisName, positionName, description);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Post axis teaching action add failed. group=" + groupName +
                    ", axis=" + axisName +
                    ", position=" + positionName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void AddPostAxisTeachingAction(
            AxisInitializeStep step,
            string axisName,
            string positionName,
            string description)
        {
            if (step == null || string.IsNullOrWhiteSpace(axisName))
                return;

            if (step.PostActions == null)
                step.PostActions = new List<AxisInitializeAction>();

            step.PostActions.Add(new AxisInitializeAction
            {
                Comment = "Step HOME 완료 후 실행되는 축 티칭 위치 이동입니다.",
                TargetType = AxisInitializeInterlockTarget.Axis,
                Name = axisName,
                Command = AxisInitializeActionCommand.AxisTeachingMove,
                PositionName = string.IsNullOrWhiteSpace(positionName) ? "AvoidPosition" : positionName,
                TimeoutMs = 0,
                Enabled = true,
                Description = description
            });
        }

        private static bool EnsureRequiredInitializeActions(AxisInitializePlan plan)
        {
            bool changed = false;

            //여기 우선 막자.
            return true;
            try
            {
                if (plan == null || plan.Steps == null)
                    return false;

                changed |= NormalizeRequiredInitializeActions(plan);

                changed |= EnsureActionOnlyCylinderStep(plan, 30, "OutputStageZClampLift", "NGBinGuideClampLift",
                    AxisInitializeActionCommand.CylinderFwd,
                    "NG bin clamp lift moves up before OutputGoodStageZ home.");
                changed |= EnsureActionOnlyAxisTeachingStep(plan, 80, "FrontPickerYAvoid", "FrontPickerY", "AvoidPosition",
                    "FrontPickerY moves to Avoid after home.");
                changed |= EnsureActionOnlyAxisTeachingStep(plan, 100, "RearPickerYAvoid", "RearPickerY", "AvoidPosition",
                    "RearPickerY moves to Avoid after home.");
                changed |= EnsureActionOnlyCylinderStep(plan, 210, "InputFeederLiftDown", "InputFeederLift",
                    AxisInitializeActionCommand.CylinderBwd,
                    "Input feeder lift moves down after InputFeederY home.");
                changed |= EnsureActionOnlyAxisTeachingStep(plan, 170, "InputStageAvoid", "InputStageY", "AvoidPosition",
                    "InputStageY moves to Avoid after home.");

                return changed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Required initialize action update failed: " + ex.Message + " - Failed");
                return changed;
            }
            finally
            {
            }
        }

        private static bool NormalizeRequiredInitializeActions(AxisInitializePlan plan)
        {
            bool changed = false;

            try
            {
                if (plan == null || plan.Steps == null)
                    return false;

                foreach (AxisInitializeStep step in plan.Steps)
                {
                    if (step == null || step.PostActions == null)
                        continue;

                    foreach (AxisInitializeAction action in step.PostActions)
                    {
                        if (action == null)
                            continue;

                        if (string.Equals(action.TargetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(action.Command, AxisInitializeActionCommand.AxisTeachingMove, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(action.Name, "GoodStage_StageZ", StringComparison.OrdinalIgnoreCase))
                        {
                            action.Name = "OutputGoodStageZ";
                            changed = true;
                        }
                    }
                }

                return changed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Required initialize action normalize failed: " + ex.Message + " - Failed");
                return changed;
            }
            finally
            {
            }
        }

        private static bool EnsurePostAxisTeachingAction(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string axisName,
            string positionName,
            string description)
        {
            AxisInitializeStep step = FindStep(plan, stepNo, groupName);
            if (step == null)
                return false;

            if (step.PostActions == null)
            {
                step.PostActions = new List<AxisInitializeAction>();
            }

            bool exists = step.PostActions.Any(x =>
                x != null &&
                x.Enabled &&
                string.Equals(x.TargetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Name, axisName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Command, AxisInitializeActionCommand.AxisTeachingMove, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizePositionName(x.PositionName), NormalizePositionName(positionName), StringComparison.OrdinalIgnoreCase));
            if (exists)
                return false;

            AddPostAxisTeachingAction(step, axisName, positionName, description);
            return true;
        }

        private static bool EnsurePostCylinderAction(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string cylinderName,
            string command,
            string description)
        {
            AxisInitializeStep step = FindStep(plan, stepNo, groupName);
            if (step == null)
                return false;

            if (step.PostActions == null)
            {
                step.PostActions = new List<AxisInitializeAction>();
            }

            bool exists = step.PostActions.Any(x =>
                x != null &&
                x.Enabled &&
                string.Equals(x.TargetType, AxisInitializeInterlockTarget.Cylinder, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Name, cylinderName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase));
            if (exists)
                return false;

            AddPostCylinderAction(step, cylinderName, command, description);
            return true;
        }

        private static bool EnsureActionOnlyCylinderStep(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string cylinderName,
            string command,
            string description)
        {
            try
            {
                if (plan == null)
                    return false;

                if (plan.Steps == null)
                    plan.Steps = new List<AxisInitializeStep>();

                AxisInitializeStep step = FindStep(plan, stepNo, groupName);
                if (step == null)
                {
                    AddActionOnlyStep(plan, stepNo, groupName, cylinderName, command, description);
                    return true;
                }

                bool changed = false;
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

                bool exists = step.PreActions.Any(x =>
                    x != null &&
                    x.Enabled &&
                    string.Equals(x.TargetType, AxisInitializeInterlockTarget.Cylinder, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Name, cylinderName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Command, command, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    AddPreCylinderAction(step, cylinderName, command, description);
                    changed = true;
                }

                return changed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Action-only cylinder step ensure failed. group=" + groupName +
                    ", cylinder=" + cylinderName +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool EnsureActionOnlyAxisTeachingStep(
            AxisInitializePlan plan,
            int stepNo,
            string groupName,
            string axisName,
            string positionName,
            string description)
        {
            try
            {
                if (plan == null)
                    return false;

                if (plan.Steps == null)
                    plan.Steps = new List<AxisInitializeStep>();

                AxisInitializeStep step = FindStep(plan, stepNo, groupName);
                if (step == null)
                {
                    AddAxisTeachingActionOnlyStep(plan, stepNo, groupName, axisName, positionName, description);
                    return true;
                }

                bool changed = false;
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

                bool exists = step.PreActions.Any(x =>
                    x != null &&
                    x.Enabled &&
                    string.Equals(x.TargetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Name, axisName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Command, AxisInitializeActionCommand.AxisTeachingMove, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizePositionName(x.PositionName), NormalizePositionName(positionName), StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    step.PreActions.Add(new AxisInitializeAction
                    {
                        Comment = "독립 Step에서 실행되는 축 티칭 위치 이동입니다.",
                        TargetType = AxisInitializeInterlockTarget.Axis,
                        Name = axisName,
                        Command = AxisInitializeActionCommand.AxisTeachingMove,
                        PositionName = NormalizePositionName(positionName),
                        TimeoutMs = 0,
                        Enabled = true,
                        Description = description
                    });
                    changed = true;
                }

                return changed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Action-only axis teaching step ensure failed. group=" + groupName +
                    ", axis=" + axisName +
                    ", position=" + positionName +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static AxisInitializeStep FindStep(AxisInitializePlan plan, int stepNo, string groupName)
        {
            if (plan == null || plan.Steps == null)
                return null;

            return plan.Steps.FirstOrDefault(x =>
                x != null &&
                x.StepNo == stepNo &&
                string.Equals(x.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizePositionName(string positionName)
        {
            if (string.IsNullOrWhiteSpace(positionName))
                return "AvoidPosition";

            string value = positionName.Trim();
            if (string.Equals(value, "Avoid", StringComparison.OrdinalIgnoreCase))
                return "AvoidPosition";

            return value;
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
            finally
            {
            }
        }

        private static void AddKnownSingleStep(
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
            finally
            {
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
            finally
            {
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
                        "Action Command: CylinderFwd, CylinderBwd, AxisTeachingMove, CustomHook을 사용할 수 있습니다.",
                        "AxisTeachingMove: TargetType=Axis, Name=축 이름, PositionName=AvoidPosition 같은 티칭 위치 이름을 사용합니다.",
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
