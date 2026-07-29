using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common;
using QMC.Common.Data.Store;

namespace QMC.CDT320
{
    [DataContract]
    public class MachineAxisRuntimeState
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public bool IsServoOn { get; set; }
        [DataMember] public bool IsAlarm { get; set; }
        [DataMember] public bool IsHomeDone { get; set; }
        [DataMember] public bool IsInPosition { get; set; }
        [DataMember] public double ActualPosition { get; set; }
        [DataMember] public double CommandPosition { get; set; }
        [DataMember] public uint AlarmCode { get; set; }
    }

    [DataContract]
    public class MachineInitializeStepRuntimeState
    {
        [DataMember] public int StepNo { get; set; }
        [DataMember] public string GroupName { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class MachineCylinderRuntimeState
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public bool IsFwd { get; set; }
        [DataMember] public bool IsBwd { get; set; }
        [DataMember] public bool InFwdOn { get; set; }
        [DataMember] public bool InBwdOn { get; set; }
        [DataMember] public bool OutFwdOn { get; set; }
        [DataMember] public bool OutBwdOn { get; set; }
        [DataMember] public bool IsSingleSolenoid { get; set; }
        [DataMember] public bool IsSimulationMode { get; set; }
    }

    [DataContract]
    public class MachinePickerOffsetRuntimeState
    {
        [DataMember] public string Side { get; set; }
        [DataMember] public int PickerIndex { get; set; }
        [DataMember] public double AlignOffsetX { get; set; }
        [DataMember] public double AlignOffsetY { get; set; }
        [DataMember] public double AlignOffsetT { get; set; }
        [DataMember] public bool SideInspectionCorrectionValid { get; set; }
        [DataMember] public double BottomOffsetXmm { get; set; }
        [DataMember] public double BottomOffsetYmm { get; set; }
        [DataMember] public double PickerZOffset { get; set; }
        [DataMember] public string SideInspectionSourceDieId { get; set; }
        [DataMember] public DateTime SideInspectionUpdatedAt { get; set; }
    }

    [DataContract]
    public class MachinePickerWorkCounterRuntimeState
    {
        [DataMember] public string Side { get; set; }
        [DataMember] public int[] ColletUseCounts { get; set; }
        [DataMember] public int PickFailCount { get; set; }
        [DataMember] public int PlaceFailCount { get; set; }
    }

    [DataContract]
    public class MachineRuntimeState
    {
        [DataMember] public bool IsMachineInitialized { get; set; }
        [DataMember] public bool DeveloperMode { get; set; }
        [DataMember] public DateTime SavedAt { get; set; }
        [DataMember] public string SaveReason { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string MaterialSnapshotPath { get; set; }
        [DataMember] public List<MachineAxisRuntimeState> Axes { get; set; } =
            new List<MachineAxisRuntimeState>();
        [DataMember] public List<MachineCylinderRuntimeState> Cylinders { get; set; } =
            new List<MachineCylinderRuntimeState>();
        [DataMember] public List<MachinePickerOffsetRuntimeState> PickerOffsets { get; set; } =
            new List<MachinePickerOffsetRuntimeState>();
        [DataMember] public List<MachinePickerWorkCounterRuntimeState> PickerWorkCounters { get; set; } =
            new List<MachinePickerWorkCounterRuntimeState>();
        [DataMember] public List<MachineInitializeStepRuntimeState> InitializeSteps { get; set; } =
            new List<MachineInitializeStepRuntimeState>();
    }

    public static class MachineRuntimeStateStore
    {
        public static string RootDir => @"D:\CDT-320";
        public static string Dir => Path.Combine(RootDir, "State");
        public static string StatePath => Path.Combine(Dir, "machine_state.json");
        public static string BackupPath => Path.Combine(Dir, "machine_state.bak");

        public static bool Exists()
        {
            return File.Exists(StatePath);
        }

        public static MachineRuntimeState Load()
        {
            try
            {
                if (!File.Exists(StatePath))
                    return null;

                using (var fs = File.OpenRead(StatePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(MachineRuntimeState));
                    return (MachineRuntimeState)serializer.ReadObject(fs);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MachineRuntimeStateLoad",
                    "Machine runtime state load failed: " + StatePath + " / " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        public static bool Save(MachineRuntimeState state)
        {
            string tmp = StatePath + ".tmp";
            try
            {
                if (state == null)
                {
                    Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                        "Machine runtime state save failed: state is null. - Failed");
                    return false;
                }

                Directory.CreateDirectory(Dir);
                state.SavedAt = NormalizeDateTime(state.SavedAt, DateTime.Now);
                if (state.Axes == null)
                    state.Axes = new List<MachineAxisRuntimeState>();
                if (state.Cylinders == null)
                    state.Cylinders = new List<MachineCylinderRuntimeState>();
                if (state.PickerOffsets == null)
                    state.PickerOffsets = new List<MachinePickerOffsetRuntimeState>();
                if (state.PickerWorkCounters == null)
                    state.PickerWorkCounters = new List<MachinePickerWorkCounterRuntimeState>();
                if (state.InitializeSteps == null)
                    state.InitializeSteps = new List<MachineInitializeStepRuntimeState>();
                NormalizeNestedDateTimes(state);

                using (var fs = File.Create(tmp))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(MachineRuntimeState), state);
                }

                if (File.Exists(StatePath))
                {
                    try
                    {
                        if (File.Exists(BackupPath)) File.Delete(BackupPath);
                        File.Move(StatePath, BackupPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Main", "SYSTEM", "MachineRuntimeStateBackup",
                            "Machine runtime state backup failed: " + BackupPath + " / " + ex.Message + " - Failed");
                    }
                    finally
                    {
                    }
                }

                if (File.Exists(StatePath)) File.Delete(StatePath);
                File.Move(tmp, StatePath);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Machine runtime state save failed: " + StatePath + " / " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void NormalizeNestedDateTimes(MachineRuntimeState state)
        {
            try
            {
                if (state == null)
                    return;

                state.SavedAt = NormalizeDateTime(state.SavedAt, DateTime.Now);

                if (state.PickerOffsets == null)
                    return;

                DateTime fallback = NormalizeDateTime(state.SavedAt, DateTime.Now);
                foreach (var item in state.PickerOffsets)
                {
                    if (item == null)
                        continue;

                    item.SideInspectionUpdatedAt = NormalizeOptionalDateTime(item.SideInspectionUpdatedAt, fallback);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static DateTime NormalizeDateTime(DateTime value, DateTime fallback)
        {
            try
            {
                if (value == DateTime.MinValue || value == DateTime.MaxValue)
                    return fallback;

                if (value.Year < 2000 || value.Year > 2100)
                    return fallback;

                return value;
            }
            catch
            {
                return fallback;
            }
            finally
            {
            }
        }

        private static DateTime NormalizeOptionalDateTime(DateTime value, DateTime fallback)
        {
            try
            {
                if (value == DateTime.MinValue)
                    return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

                if (value == DateTime.MaxValue)
                    return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

                if (value.Year < 2000 || value.Year > 2100)
                    return fallback;

                return value;
            }
            catch
            {
                return fallback;
            }
            finally
            {
            }
        }
    }
}
