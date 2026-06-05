using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common;
using QMC.Common.Data.Store;
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
        [DataMember] public string RunMode { get; set; } = AxisInitializeRunMode.Serial;
        [DataMember] public string InterlockGroup { get; set; }
        [DataMember] public List<AxisInitializeInterlockRule> Interlocks { get; set; } =
            new List<AxisInitializeInterlockRule>();
        [DataMember] public bool Enabled { get; set; } = true;
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
                    JsonPrettySerializer.WriteObject(fs, typeof(AxisInitializePlan), plan);
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
                Comment = "Axis initialize plan. JSON 표준 주석은 사용할 수 없어서 Comment/Help 필드로 수정 기준을 남깁니다.",
                Version = 1,
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

                var groups = cleanAxes
                    .GroupBy(x => !string.IsNullOrWhiteSpace(x.Setup != null ? x.Setup.UnitName : "")
                        ? x.Setup.UnitName
                        : "Ungrouped")
                    .OrderBy(g => g.Min(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue))
                    .ThenBy(g => g.Key);

                int stepNo = 10;
                foreach (var group in groups)
                {
                    plan.Steps.Add(new AxisInitializeStep
                    {
                        Comment = "StepNo 순서대로 실행됩니다. AxisNames는 이 Step에서 HOME 잡을 축 이름입니다.",
                        StepNo = stepNo,
                        GroupName = group.Key,
                        AxisNames = group
                            .OrderBy(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue)
                            .ThenBy(x => x.Name)
                            .Select(x => x.Name)
                            .ToList(),
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
                    "Axis initialize default plan create failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return plan;
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
