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

                string tmp = StatePath + ".tmp";
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
                Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Machine runtime state save failed: " + StatePath + " / " + ex.Message + " - Failed");
                return false;
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
    }
}
