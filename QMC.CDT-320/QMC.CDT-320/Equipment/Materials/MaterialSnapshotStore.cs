using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.Common;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Materials
{
    public static class MaterialSnapshotStore
    {
        public static string RootDir => @"D:\CDT-320";
        public static string Dir => Path.Combine(RootDir, "State");
        public static string SnapshotPath => Path.Combine(Dir, "material_state.json");
        public static string BackupPath => Path.Combine(Dir, "material_state.bak");

        public static bool Exists()
        {
            return File.Exists(SnapshotPath);
        }

        public static MaterialSnapshot Load()
        {
            if (!File.Exists(SnapshotPath)) return null;
            try
            {
                string json = NormalizeLegacyStateText(File.ReadAllText(SnapshotPath, Encoding.UTF8));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var ser = new DataContractJsonSerializer(typeof(MaterialSnapshot));
                    var snapshot = (MaterialSnapshot)ser.ReadObject(ms);
                    NormalizeSnapshotStates(snapshot);
                    return snapshot;
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad", "Material snapshot load failed: " + SnapshotPath + " / " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private static string NormalizeLegacyStateText(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            return json
                .Replace("\"Unknown\"", "\"Empty\"")
                .Replace("\"Scanned\"", "\"Ready\"")
                .Replace("\"IdRead\"", "\"WorkReady\"")
                .Replace("\"LoadedToStage\"", "\"WorkReady\"")
                .Replace("\"Aligned\"", "\"WorkReady\"")
                .Replace("\"Processing\"", "\"Working\"")
                .Replace("\"Completed\"", "\"Finish\"")
                .Replace("\"Returned\"", "\"Finish\"");
        }

        private static void NormalizeSnapshotStates(MaterialSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Wafers == null)
                return;

            foreach (var wafer in snapshot.Wafers)
            {
                if (wafer != null)
                    wafer.State = WaferMaterialStateText.Normalize(wafer.State);
            }
        }

        public static bool Save(MaterialSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave", "Material snapshot save failed: snapshot is null. - Failed");
                return false;
            }

            try
            {
                Directory.CreateDirectory(Dir);
                snapshot.SavedAt = DateTime.Now;
                NormalizeSnapshotDateTimes(snapshot);

                string tmp = SnapshotPath + ".tmp";
                using (var fs = File.Create(tmp))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(MaterialSnapshot), snapshot);
                }

                if (File.Exists(SnapshotPath))
                {
                    try
                    {
                        if (File.Exists(BackupPath)) File.Delete(BackupPath);
                        File.Move(SnapshotPath, BackupPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialSnapshotBackup", "Material snapshot backup failed: " + BackupPath + " / " + ex.Message + " - Failed");
                    }
                    finally
                    {
                    }
                }

                if (File.Exists(SnapshotPath)) File.Delete(SnapshotPath);
                File.Move(tmp, SnapshotPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave", "Material snapshot save failed: " + SnapshotPath + " / " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void NormalizeSnapshotDateTimes(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return;

                DateTime now = DateTime.Now;
                snapshot.SavedAt = NormalizeDateTime(snapshot.SavedAt, now);

                if (snapshot.Cassettes != null)
                {
                    foreach (var cassette in snapshot.Cassettes)
                    {
                        if (cassette == null)
                            continue;

                        cassette.LastScanTime = NormalizeDateTime(cassette.LastScanTime, now);
                    }
                }

                if (snapshot.Wafers != null)
                {
                    foreach (var wafer in snapshot.Wafers)
                    {
                        if (wafer == null)
                            continue;

                        wafer.CreatedAt = NormalizeDateTime(wafer.CreatedAt, now);
                        wafer.UpdatedAt = NormalizeDateTime(wafer.UpdatedAt, now);
                    }
                }

                if (snapshot.Dies != null)
                {
                    foreach (var die in snapshot.Dies)
                    {
                        if (die == null)
                            continue;

                        die.CreatedAt = NormalizeDateTime(die.CreatedAt, now);
                        die.UpdatedAt = NormalizeDateTime(die.UpdatedAt, now);

                        if (die.Inspections == null)
                            continue;

                        foreach (var inspection in die.Inspections)
                        {
                            if (inspection == null)
                                continue;

                            inspection.CreatedAt = NormalizeDateTime(inspection.CreatedAt, now);
                            inspection.UpdatedAt = NormalizeDateTime(inspection.UpdatedAt, now);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotDateTime", "Material snapshot DateTime normalize failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static DateTime NormalizeDateTime(DateTime value, DateTime fallback)
        {
            try
            {
                if (value == default(DateTime) || value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
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
