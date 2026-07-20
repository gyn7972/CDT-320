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
        [DataMember] public string ParallelLane { get; set; } = AxisInitializeParallelLane.None;
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
        [DataMember] public string PositionName { get; set; }
        [DataMember] public double ExpectedPosition { get; set; }
        [DataMember] public double Tolerance { get; set; } = 0.01;
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string Description { get; set; }
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
            finally
            {
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
            finally
            {
            }
        }
    }

    public static class AxisInitializePlanStore
    {
        private const int CurrentDefaultVersion = 16;
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

                // 초기화 자재 정책: Input/Output Feeder는 Empty가 필수이고,
                // Input/Good/NG Stage는 자재를 유지한 상태로 축 초기화를 허용한다.

                // 1. 수직축을 먼저 Home하여 이후 실린더/평면축 이동 공간을 확보한다.
                AddKnownStep(plan, axisByName, used, 10, "FrontPickerZ", AxisInitializeRunMode.Parallel,
                    "Front Picker Z0~Z3 home together.",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");
                AddKnownStep(plan, axisByName, used, 20, "RearPickerZ", AxisInitializeRunMode.Parallel,
                    "Rear Picker Z0~Z3 home together.",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
                AddKnownStep(plan, axisByName, used, 30, "NeedleZ", AxisInitializeRunMode.Parallel,
                    "NeedleZ and EjectPinZ home together.", "NeedleZ", "EjectPinZ");

                // 2. Reticle은 Rear/Front Slide Bwd 완료 후 Lift Bwd 순서로 이동한다.
                AddActionOnlyStep(plan, 40, "ReticleSideSlideRear", "ReticleSideSlideRear",
                    AxisInitializeActionCommand.CylinderBwd, "Reticle rear slide moves backward.");
                AddAxisHomeDoneInterlocks(plan, 40, "ReticleSideSlideRear",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

                AddActionOnlyStep(plan, 50, "ReticleSideSlideFront", "ReticleSideSlideFront",
                    AxisInitializeActionCommand.CylinderBwd, "Reticle front slide moves backward.");
                AddAxisHomeDoneInterlocks(plan, 50, "ReticleSideSlideFront",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

                AddActionOnlyStep(plan, 60, "ReticleLift", "ReticleLift",
                    AxisInitializeActionCommand.CylinderBwd, "Reticle lift moves backward after both slides.");
                AddAxisHomeDoneInterlocks(plan, 60, "ReticleLift",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
                AddStepInterlock(plan, 60, "ReticleLift", AxisInitializeInterlockTarget.Cylinder,
                    "ReticleSideSlideRear", AxisInitializeInterlockState.Bwd,
                    "Reticle Rear Slide를 Bwd 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 60, "ReticleLift", AxisInitializeInterlockTarget.Cylinder,
                    "ReticleSideSlideFront", AxisInitializeInterlockState.Bwd,
                    "Reticle Front Slide를 Bwd 상태로 만든 후 다시 실행하십시오.");

                // 3. OutputStage 6개 실린더 중 축 Home에 필요한 상태만 조건부로 만든다.
                // NG Stage 제품 감지 시 Clamp는 유지하고, 비어 있을 때만 Bwd한 뒤 ClampLift를 Up한다.
                AddCustomActionOnlyStep(plan, 70, "OutputStageNGClampPrepare",
                    AxisInitializeActionName.PrepareOutputStageNgClamp,
                    "NG Stage가 비어 있으면 Clamp를 Bwd하고, 제품이 있으면 Clamp 상태를 유지합니다.");

                AddActionOnlyStep(plan, 80, "OutputStageNGClampLift", "NGBinGuideClampLift",
                    AxisInitializeActionCommand.CylinderFwd,
                    "NG ClampLift를 Up으로 이동합니다. 제품이 있으면 Clamp 상태는 변경하지 않습니다.");

                AddKnownSingleStep(plan, axisByName, used, 90, "OutputStageZ", AxisInitializeRunMode.Serial,
                    "OutputGoodStageZ home after NG Clamp conditional safety and ClampLift Up.",
                    "OutputGoodStageZ", "GoodStage_StageZ");
                AddFeederSafeInterlocks(plan, 90, "OutputStageZ", false);
                AddStepInterlock(plan, 90, "OutputStageZ", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 90, "OutputStageZ", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");

                AddKnownSingleStep(plan, axisByName, used, 100, "InputStageZ", AxisInitializeRunMode.Serial,
                    "InputExpandingZ home only when InputFeeder is empty and already unclamped.",
                    "InputExpandingZ", "ExpanderZ");
                AddFeederSafeInterlocks(plan, 100, "InputStageZ", true);
                AddAxisHomeDoneInterlocks(plan, 100, "InputStageZ", "NeedleZ", "EjectPinZ");

                // 4. Picker T와 기구 간섭이 없는 같은 Side Vision Y축을 함께 초기화한다.
                AddKnownStep(plan, axisByName, used, 110, "FrontPickerT", AxisInitializeRunMode.Parallel,
                    "Front Picker T0~T3 and Front Side Vision Y home together.",
                    "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3",
                    "FrontSideVisionY0");
                AddAxisHomeDoneInterlocks(plan, 110, "FrontPickerT",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");

                AddKnownStep(plan, axisByName, used, 120, "RearPickerT", AxisInitializeRunMode.Parallel,
                    "Rear Picker T0~T3 and Rear Side Vision Y home together.",
                    "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3",
                    "RearSideVisionY0");
                AddAxisHomeDoneInterlocks(plan, 120, "RearPickerT",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");

                // 5. Picker Y는 수직 Stage가 안전해진 후 양쪽 하드리밋을 탐색하고 동시에 Home한다.
                AddKnownStep(plan, axisByName, used, 150, "PickerYPair", AxisInitializeRunMode.Parallel,
                    "FrontPickerY MEL and RearPickerY PEL search, then simultaneous pair home.",
                    "FrontPickerY", "RearPickerY");
                AddAxisHomeDoneInterlocks(plan, 150, "PickerYPair",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3",
                    "InputExpandingZ", "OutputGoodStageZ");
                AddStepInterlock(plan, 150, "PickerYPair", AxisInitializeInterlockTarget.Cylinder,
                    "ReticleLift", AxisInitializeInterlockState.Bwd,
                    "Reticle Lift가 Bwd 상태인지 확인하십시오.");

                // 6. InputFeeder는 자동 Unclamp하지 않고 Empty/Unclamp 확인 후 Down/Home한다.
                AddActionOnlyStep(plan, 170, "InputFeederLift", "InputFeederLift",
                    AxisInitializeActionCommand.CylinderBwd,
                    "InputFeeder Lift moves Down only when empty and already unclamped.");
                AddFeederSafeInterlocks(plan, 170, "InputFeederLift", true);
                AddAxisHomeDoneInterlocks(plan, 170, "InputFeederLift", "FrontPickerY", "RearPickerY");

                AddKnownSingleStep(plan, axisByName, used, 180, "InputFeeder", AxisInitializeRunMode.Serial,
                    "InputFeederY home after Lift Down.", "InputFeederY", "FeederY");
                AddFeederSafeInterlocks(plan, 180, "InputFeeder", true);
                AddAxisHomeDoneInterlocks(plan, 180, "InputFeeder", "FrontPickerY", "RearPickerY");
                AddStepInterlock(plan, 180, "InputFeeder", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                // 7. InputStage 평면축과 Input Cassette를 초기화한다.
                AddKnownStep(plan, axisByName, used, 190, "InputStageY", AxisInitializeRunMode.Parallel,
                    "InputStageY and NeedleX home together when NeedleZ is at Home or Avoid.",
                    "InputStageY", "NeedleX");
                AddFeederSafeInterlocks(plan, 190, "InputStageY", true);
                AddAxisHomeDoneInterlocks(plan, 190, "InputStageY",
                    "InputFeederY", "EjectPinZ", "InputExpandingZ",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
                AddStepInterlock(plan, 190, "InputStageY", AxisInitializeInterlockTarget.Axis,
                    "NeedleZ", AxisInitializeInterlockState.HomeOrAvoid,
                    "NeedleZ를 Home(0) 또는 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 190, "InputStageY", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                AddAxisTeachingActionOnlyStep(plan, 200, "InputStageYAvoid", "InputStageY", "AvoidPosition",
                    "InputStageY moves to Avoid after home.");
                AddFeederSafeInterlocks(plan, 200, "InputStageYAvoid", true);
                AddAxisHomeDoneInterlocks(plan, 200, "InputStageYAvoid",
                    "InputStageY", "NeedleX", "InputFeederY", "InputExpandingZ", "EjectPinZ");
                AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.Axis,
                    "InputFeederY", AxisInitializeInterlockState.AtPosition,
                    "InputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "InputFeederY Home 후 Wafer Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, 200, "InputStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                AddKnownSingleStep(plan, axisByName, used, 210, "InputStageT", AxisInitializeRunMode.Serial,
                    "InputStageT home after InputStageY Avoid.", "InputStageT", "StageT");
                AddFeederSafeInterlocks(plan, 210, "InputStageT", true);
                AddAxisHomeDoneInterlocks(plan, 210, "InputStageT",
                    "InputStageY", "InputFeederY", "EjectPinZ", "InputExpandingZ",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
                AddStepInterlock(plan, 210, "InputStageT", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 210, "InputStageT", AxisInitializeInterlockTarget.Axis,
                    "InputStageY", AxisInitializeInterlockState.AtPosition,
                    "InputStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");

                AddKnownStep(plan, axisByName, used, 230, "InputCassette", AxisInitializeRunMode.Serial,
                    "InputLifterZ home after InputFeederY is safe.", "InputLifterZ");
                AddFeederSafeInterlocks(plan, 230, "InputCassette", true);
                AddAxisHomeDoneInterlocks(plan, 230, "InputCassette", "InputFeederY");
                AddStepInterlock(plan, 230, "InputCassette", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                // 8. OutputFeeder와 Output Stage Y축을 초기화한다.
                AddActionOnlyStep(plan, 240, "OutputGoodBinGuideDown", "GoodBinGuideLift",
                    AxisInitializeActionCommand.CylinderBwd,
                    "Good Bin Guide moves Down before OutputFeederY and NGStageY home.");
                AddAxisHomeDoneInterlocks(plan, 240, "OutputGoodBinGuideDown", "OutputGoodStageZ");

                AddActionOnlyStep(plan, 250, "OutputFeederLift", "OutputFeederLift",
                    AxisInitializeActionCommand.CylinderFwd,
                    "OutputFeeder Lift moves Up only when empty and already unclamped.");
                AddFeederSafeInterlocks(plan, 250, "OutputFeederLift", false);
                AddAxisHomeDoneInterlocks(plan, 250, "OutputFeederLift",
                    "FrontPickerY", "RearPickerY", "OutputGoodStageZ");
                AddStepInterlock(plan, 250, "OutputFeederLift", AxisInitializeInterlockTarget.Cylinder,
                    "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                    "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");

                AddKnownStep(plan, axisByName, used, 260, "OutputFeeder", AxisInitializeRunMode.Serial,
                    "OutputFeederY home after Lift Up and Good Guide Down.", "OutputFeederY");
                AddFeederSafeInterlocks(plan, 260, "OutputFeeder", false);
                AddAxisHomeDoneInterlocks(plan, 260, "OutputFeeder",
                    "FrontPickerY", "RearPickerY", "OutputGoodStageZ");
                AddStepInterlock(plan, 260, "OutputFeeder", AxisInitializeInterlockTarget.Cylinder,
                    "OutputFeederLift", AxisInitializeInterlockState.Fwd,
                    "OutputFeeder Lift를 Up 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 260, "OutputFeeder", AxisInitializeInterlockTarget.Cylinder,
                    "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                    "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");

                AddAxisTeachingActionOnlyStep(plan, 270, "OutputStageZAvoid", "OutputGoodStageZ", "AvoidPosition",
                    "OutputGoodStageZ moves to Avoid after OutputFeederY home.");
                AddAxisHomeDoneInterlocks(plan, 270, "OutputStageZAvoid", "OutputGoodStageZ", "OutputFeederY");
                AddFeederSafeInterlocks(plan, 270, "OutputStageZAvoid", false);
                AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Axis,
                    "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                    "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 270, "OutputStageZAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");

                AddActionOnlyStep(plan, 280, "OutputFeederLiftDown", "OutputFeederLift",
                    AxisInitializeActionCommand.CylinderBwd,
                    "OutputFeeder Lift moves Down after OutputFeederY home.");
                AddFeederSafeInterlocks(plan, 280, "OutputFeederLiftDown", false);
                AddAxisHomeDoneInterlocks(plan, 280, "OutputFeederLiftDown", "OutputFeederY");

                AddKnownSingleStep(plan, axisByName, used, 290, "OutputNGStageY", AxisInitializeRunMode.Serial,
                    "OutputNGStageY home after GoodStageZ Avoid and Good Guide Down.",
                    "OutputNGStageY", "NgStage_StageY");
                AddAxisHomeDoneInterlocks(plan, 290, "OutputNGStageY", "OutputGoodStageZ", "OutputFeederY");
                AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Axis,
                    "OutputGoodStageZ", AxisInitializeInterlockState.AtPosition,
                    "OutputGoodStageZ를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                    "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                    "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 290, "OutputNGStageY", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");

                AddAxisTeachingActionOnlyStep(plan, 300, "OutputNGStageYAvoid", "OutputNGStageY", "AvoidPosition",
                    "OutputNGStageY moves to Avoid after home.");
                AddAxisHomeDoneInterlocks(plan, 300, "OutputNGStageYAvoid", "OutputNGStageY", "OutputGoodStageZ", "OutputFeederY");
                AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "GoodBinGuideLift", AxisInitializeInterlockState.Bwd,
                    "Good Bin Guide를 Down 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 300, "OutputNGStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");

                AddKnownSingleStep(plan, axisByName, used, 310, "OutputGoodStageY", AxisInitializeRunMode.Serial,
                    "OutputGoodStageY home after OutputNGStageY Avoid.",
                    "OutputGoodStageY", "GoodStage_StageY");
                AddAxisHomeDoneInterlocks(plan, 310, "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "OutputFeederY");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                    "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                    "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                    "OutputGoodStageZ", AxisInitializeInterlockState.AtPosition,
                    "OutputGoodStageZ를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Axis,
                    "OutputNGStageY", AxisInitializeInterlockState.AtPosition,
                    "OutputNGStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 310, "OutputGoodStageY", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");

                AddAxisTeachingActionOnlyStep(plan, 320, "OutputGoodStageYAvoid", "OutputGoodStageY", "AvoidPosition",
                    "OutputGoodStageY moves to Avoid after home.");
                AddAxisHomeDoneInterlocks(plan, 320, "OutputGoodStageYAvoid",
                    "OutputGoodStageY", "OutputGoodStageZ", "OutputNGStageY", "OutputFeederY");
                AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Axis,
                    "OutputNGStageY", AxisInitializeInterlockState.AtPosition,
                    "OutputNGStageY를 Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClampLift", AxisInitializeInterlockState.Fwd,
                    "NG ClampLift가 Up 상태인지 확인하십시오.");
                AddStepInterlock(plan, 320, "OutputGoodStageYAvoid", AxisInitializeInterlockTarget.Cylinder,
                    "NGBinGuideClamp", AxisInitializeInterlockState.SafeForStageMove,
                    "NG Stage가 비어 있으면 Clamp Bwd, 제품이 있으면 Clamp 유지, 공통으로 ClampLift Up 상태인지 확인하십시오.");

                AddKnownStep(plan, axisByName, used, 330, "OutputCassette", AxisInitializeRunMode.Serial,
                    "OutputLifterZ home after OutputFeederY is safe.", "OutputLifterZ");
                AddFeederSafeInterlocks(plan, 330, "OutputCassette", false);
                AddAxisHomeDoneInterlocks(plan, 330, "OutputCassette", "OutputFeederY");
                AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.Axis,
                    "OutputFeederY", AxisInitializeInterlockState.AtPosition,
                    "OutputFeederY를 Home/Avoid 위치로 이동한 후 다시 실행하십시오.", "AvoidPosition");
                AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "OutputFeederY Home 후 Bin Feeder Avoid 센서를 확인하십시오.");
                AddStepInterlock(plan, 330, "OutputCassette", AxisInitializeInterlockTarget.Cylinder,
                    "OutputFeederLift", AxisInitializeInterlockState.Bwd,
                    "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");

                // 9. SharedRail X 4축은 마지막에 직렬 Home하며 현재→Home 전체 경로를 Pair Clearance로 검사한다.
                AddKnownSingleStep(plan, axisByName, used, 340, "InputVisionX", AxisInitializeRunMode.Serial,
                    "InputVisionX home first on the shared rail.", "InputVisionX", "CameraX");
                AddAxisHomeDoneInterlocks(plan, 340, "InputVisionX", "InputFeederY", "FrontPickerY", "RearPickerY");
                AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.Cylinder,
                    "InputFeederLift", AxisInitializeInterlockState.Bwd,
                    "InputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.WaferFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "InputVisionX HOME 전 InputFeeder Avoid Dog(X090)를 확인하십시오.");
                AddStepInterlock(plan, 340, "InputVisionX", AxisInitializeInterlockTarget.Axis,
                    "InputVisionX", AxisInitializeInterlockState.SharedRailHomeClear,
                    "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

                AddKnownStep(plan, axisByName, used, 350, "FrontPickerX", AxisInitializeRunMode.Serial,
                    "FrontPickerX home after InputVisionX.", "FrontPickerX");
                AddAxisHomeDoneInterlocks(plan, 350, "FrontPickerX",
                    "InputVisionX", "FrontPickerY", "RearPickerY",
                    "InputFeederY", "OutputFeederY", "InputExpandingZ", "OutputGoodStageZ",
                    "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3");
                AddPickerXGlobalSafetyInterlocks(plan, 350, "FrontPickerX");
                AddStepInterlock(plan, 350, "FrontPickerX", AxisInitializeInterlockTarget.Axis,
                    "FrontPickerX", AxisInitializeInterlockState.SharedRailHomeClear,
                    "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

                AddKnownStep(plan, axisByName, used, 360, "RearPickerX", AxisInitializeRunMode.Serial,
                    "RearPickerX home after FrontPickerX.", "RearPickerX");
                AddAxisHomeDoneInterlocks(plan, 360, "RearPickerX",
                    "InputVisionX", "FrontPickerY", "RearPickerY",
                    "InputFeederY", "OutputFeederY", "InputExpandingZ", "OutputGoodStageZ",
                    "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3");
                AddPickerXGlobalSafetyInterlocks(plan, 360, "RearPickerX");
                AddStepInterlock(plan, 360, "RearPickerX", AxisInitializeInterlockTarget.Axis,
                    "RearPickerX", AxisInitializeInterlockState.SharedRailHomeClear,
                    "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

                AddKnownStep(plan, axisByName, used, 370, "SharedRailXOutput", AxisInitializeRunMode.Serial,
                    "OutputVisionX home after both Picker X axes.", "OutputVisionX");
                AddAxisHomeDoneInterlocks(plan, 370, "SharedRailXOutput",
                    "FrontPickerX", "RearPickerX", "OutputFeederY");
                AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.Cylinder,
                    "OutputFeederLift", AxisInitializeInterlockState.Bwd,
                    "OutputFeeder Lift를 Down 상태로 만든 후 다시 실행하십시오.");
                AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.DigitalInput,
                    AxisInitializeSafetyInput.BinFeederAvoidPositionCheck, AxisInitializeInterlockState.On,
                    "OutputVisionX HOME 전 OutputFeeder Avoid Dog(X091)를 확인하십시오.");
                AddStepInterlock(plan, 370, "SharedRailXOutput", AxisInitializeInterlockTarget.Axis,
                    "OutputVisionX", AxisInitializeInterlockState.SharedRailHomeClear,
                    "SharedRail X축 현재 위치를 확인하고 간섭물을 제거한 후 다시 실행하십시오.");

                AssignParallelLane(plan, AxisInitializeParallelLane.Input,
                    "InputFeederLift",
                    "InputFeeder",
                    "InputStageY",
                    "InputStageYAvoid",
                    "InputStageT",
                    "InputCassette");

                AssignParallelLane(plan, AxisInitializeParallelLane.Output,
                    "OutputGoodBinGuideDown",
                    "OutputFeederLift",
                    "OutputFeeder",
                    "OutputStageZAvoid",
                    "OutputFeederLiftDown",
                    "OutputNGStageY",
                    "OutputNGStageYAvoid",
                    "OutputGoodStageY",
                    "OutputGoodStageYAvoid",
                    "OutputCassette");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Axis initialize default plan create failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            ApplyCommonSafetyInterlocks(plan);
            return plan;
        }

        private static void AssignParallelLane(
            AxisInitializePlan plan,
            string laneName,
            params string[] groupNames)
        {
            try
            {
                if (plan == null || plan.Steps == null || groupNames == null)
                    return;

                var groups = new HashSet<string>(
                    groupNames.Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);
                foreach (AxisInitializeStep step in plan.Steps)
                {
                    if (step == null || string.IsNullOrWhiteSpace(step.GroupName))
                        continue;

                    if (groups.Contains(step.GroupName))
                        step.ParallelLane = laneName ?? AxisInitializeParallelLane.None;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "AxisInitializePlanDefault",
                    "Initialize parallel lane assignment failed. lane=" + laneName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void ApplyCommonSafetyInterlocks(AxisInitializePlan plan)
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
            finally
            {
            }
        }

        private static void AddFeederSafeInterlocks(
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
            finally
            {
            }
        }

        private static void AddPickerXGlobalSafetyInterlocks(
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
            finally
            {
            }
        }

        private static void AddAxisHomeDoneInterlocks(
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
            finally
            {
            }
        }

        private static void AddStepInterlock(
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
            finally
            {
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
            finally
            {
            }
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
            finally
            {
            }
        }

        private static void AddCustomActionOnlyStep(
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
            finally
            {
            }
        }
    }
}
