using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
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
        public static string RecoveryPath => Path.Combine(Dir, "material_state.recovery.json");
        public static string LastLoadedPath { get; private set; } = "";

        private static readonly DateTime SafeEmptyDateTime =
            DateTime.SpecifyKind(new DateTime(1900, 1, 1, 0, 0, 0), DateTimeKind.Utc);

        public static bool Exists()
        {
            if (File.Exists(SnapshotPath)) return true;
            if (File.Exists(RecoveryPath)) return true;
            if (File.Exists(BackupPath)) return true;
            if (File.Exists(SnapshotPath + ".tmp")) return true;

            try
            {
                return Directory.Exists(Dir) &&
                       Directory.GetFiles(Dir, "material_state.json.*.tmp").Length > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static MaterialSnapshot Load()
        {
            try
            {
                var candidates = LoadCandidates();
                if (candidates.Count == 0)
                {
                    LastLoadedPath = "";
                    return null;
                }

                SnapshotCandidate selected = SelectBestCandidate(candidates);
                if (selected == null)
                {
                    LastLoadedPath = "";
                    return null;
                }

                LastLoadedPath = selected.Path;
                if (!string.Equals(selected.Path, SnapshotPath, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                        "Material snapshot recovered from alternate file. file=" + selected.Path +
                        ", wafers=" + CountList(selected.Snapshot.Wafers) +
                        ", dies=" + CountList(selected.Snapshot.Dies) +
                        ", savedAt=" + selected.Snapshot.SavedAt.ToString("yyyy-MM-dd HH:mm:ss") + " - Ok");
                }

                NormalizeSnapshotStates(selected.Snapshot);
                return selected.Snapshot;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad", "Material snapshot load failed: " + SnapshotPath + " / " + ex.Message + " - Failed");
                LastLoadedPath = "";
                return null;
            }
            finally
            {
            }
        }

        private sealed class SnapshotCandidate
        {
            public string Path { get; set; } = "";
            public MaterialSnapshot Snapshot { get; set; }
            public DateTime LastWriteTime { get; set; }
            public int Score { get; set; }
        }

        private static List<SnapshotCandidate> LoadCandidates()
        {
            var candidates = new List<SnapshotCandidate>();
            AddCandidate(candidates, SnapshotPath);
            AddCandidate(candidates, RecoveryPath);
            AddCandidate(candidates, SnapshotPath + ".tmp");
            AddCandidate(candidates, BackupPath);

            try
            {
                if (Directory.Exists(Dir))
                {
                    foreach (string path in Directory.GetFiles(Dir, "material_state.json.*.tmp"))
                        AddCandidate(candidates, path);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                    "Material snapshot temp search failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return candidates;
        }

        private static void AddCandidate(List<SnapshotCandidate> candidates, string path)
        {
            try
            {
                if (candidates == null || string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                if (candidates.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
                    return;

                MaterialSnapshot snapshot;
                string error;
                if (!TryLoadFromPath(path, out snapshot, out error))
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                        "Material snapshot candidate load failed. file=" + path + ", error=" + error + " - Failed");
                    return;
                }

                candidates.Add(new SnapshotCandidate
                {
                    Path = path,
                    Snapshot = snapshot,
                    LastWriteTime = File.GetLastWriteTime(path),
                    Score = CalculateSnapshotScore(snapshot)
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                    "Material snapshot candidate check failed. file=" + path + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static SnapshotCandidate SelectBestCandidate(List<SnapshotCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            SnapshotCandidate newest = candidates
                .OrderByDescending(GetCandidateSavedAt)
                .ThenByDescending(c => c.LastWriteTime)
                .First();

            SnapshotCandidate richest = candidates
                .OrderByDescending(c => c.Score)
                .ThenByDescending(GetCandidateSavedAt)
                .ThenByDescending(c => c.LastWriteTime)
                .First();

            bool newestLooksPartialTemp =
                IsTempPath(newest.Path) &&
                richest.Score > newest.Score + 1000 &&
                !IsExplicitClearReason(newest.Snapshot != null ? newest.Snapshot.SaveReason : "");

            bool newestLooksStartupReset =
                IsStartupInitializeReason(newest.Snapshot != null ? newest.Snapshot.SaveReason : "") &&
                richest.Score > newest.Score + 1000;

            if (newestLooksPartialTemp || newestLooksStartupReset)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                    "Material snapshot newest candidate ignored because richer saved data exists. newest=" + newest.Path +
                    ", newestScore=" + newest.Score +
                    ", selected=" + richest.Path +
                    ", selectedScore=" + richest.Score + " - Check");
                return richest;
            }

            return newest;
        }

        private static bool TryLoadFromPath(string path, out MaterialSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = "";

            try
            {
                string json = NormalizeLegacyStateText(File.ReadAllText(path, Encoding.UTF8));
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "file is empty";
                    return false;
                }

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var ser = new DataContractJsonSerializer(typeof(MaterialSnapshot));
                    snapshot = (MaterialSnapshot)ser.ReadObject(ms);
                }

                if (snapshot == null)
                {
                    error = "snapshot is null";
                    return false;
                }

                NormalizeSnapshotStates(snapshot);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static DateTime GetCandidateSavedAt(SnapshotCandidate candidate)
        {
            if (candidate == null || candidate.Snapshot == null)
                return DateTime.MinValue;

            if (candidate.Snapshot.SavedAt <= DateTime.MinValue.AddDays(1))
                return candidate.LastWriteTime;

            return candidate.Snapshot.SavedAt;
        }

        private static bool IsTempPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExplicitClearReason(string reason)
        {
            string value = reason ?? "";
            return value.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Reset", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsStartupInitializeReason(string reason)
        {
            string value = reason ?? "";
            return value.Equals("DefaultState", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("InitializeForRecipe", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("InitializeFromRecipe", StringComparison.OrdinalIgnoreCase);
        }

        private static int CalculateSnapshotScore(MaterialSnapshot snapshot)
        {
            if (snapshot == null)
                return 0;

            int score = 0;
            score += CountList(snapshot.Wafers) * 100000;
            score += CountList(snapshot.Dies);

            if (snapshot.Cassettes != null)
            {
                foreach (var cassette in snapshot.Cassettes)
                {
                    if (cassette == null || cassette.Slots == null)
                        continue;

                    foreach (var slot in cassette.Slots)
                    {
                        if (slot != null && (slot.HasWafer || !string.IsNullOrWhiteSpace(slot.WaferId)))
                            score += 100;
                    }
                }
            }

            return score;
        }

        private static int CountList<T>(ICollection<T> list)
        {
            return list != null ? list.Count : 0;
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

            string tmp = null;
            try
            {
                Directory.CreateDirectory(Dir);
                MaterialSnapshot saveSnapshot = CloneSnapshotForSave(snapshot);
                saveSnapshot.SavedAt = DateTime.Now;
                NormalizeSnapshotDateTimes(saveSnapshot);

                tmp = Path.Combine(Dir,
                    "material_state.json." +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." +
                    Guid.NewGuid().ToString("N") + ".tmp");

                using (var fs = File.Create(tmp))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(MaterialSnapshot), saveSnapshot);
                }

                if (!ValidateWrittenSnapshot(tmp, saveSnapshot))
                {
                    CopyFailedSnapshotForDiagnosis(tmp);
                    DeleteTempFile(tmp);
                    return false;
                }

                bool committed = CommitSnapshot(tmp);
                if (committed)
                    CleanupStaleTempFiles();
                else
                    DeleteTempFile(tmp);

                return committed;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave", "Material snapshot save failed: " + SnapshotPath + " / " + ex.Message + " - Failed");
                if (!string.IsNullOrWhiteSpace(tmp))
                {
                    CopyFailedSnapshotForDiagnosis(tmp);
                    DeleteTempFile(tmp);
                }
                return false;
            }
            finally
            {
            }
        }

        private static MaterialSnapshot CloneSnapshotForSave(MaterialSnapshot source)
        {
            try
            {
                MaterialSnapshot clone = CloneObject(source) as MaterialSnapshot;
                return clone ?? source;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot save clone failed: " + ex.Message +
                    ". Runtime snapshot will be used. - Check");
                return source;
            }
            finally
            {
            }
        }

        private static object CloneObject(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
                return value;

            IList sourceList = value as IList;
            if (sourceList != null)
            {
                IList clonedList = (IList)Activator.CreateInstance(type);
                foreach (object item in sourceList)
                    clonedList.Add(CloneObject(item));
                return clonedList;
            }

            object clone = Activator.CreateInstance(type);
            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;
                if (property.GetIndexParameters().Length > 0)
                    continue;

                object propertyValue = property.GetValue(value, null);
                property.SetValue(clone, CloneObject(propertyValue), null);
            }

            return clone;
        }

        private static bool ValidateWrittenSnapshot(string path, MaterialSnapshot expected)
        {
            MaterialSnapshot loaded;
            string error;
            if (!TryLoadFromPath(path, out loaded, out error))
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot validation failed. file=" + path + ", error=" + error + " - Failed");
                return false;
            }

            int expectedWaferCount = CountList(expected.Wafers);
            int expectedDieCount = CountList(expected.Dies);
            int loadedWaferCount = CountList(loaded.Wafers);
            int loadedDieCount = CountList(loaded.Dies);

            if (expectedWaferCount != loadedWaferCount || expectedDieCount != loadedDieCount)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot validation failed. saved file count mismatch. file=" + path +
                    ", expectedWafer=" + expectedWaferCount +
                    ", loadedWafer=" + loadedWaferCount +
                    ", expectedDie=" + expectedDieCount +
                    ", loadedDie=" + loadedDieCount + " - Failed");
                return false;
            }

            return true;
        }

        private static bool CommitSnapshot(string tmp)
        {
            Exception lastError = null;

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    if (File.Exists(SnapshotPath))
                    {
                        if (File.Exists(BackupPath))
                            File.Delete(BackupPath);

                        File.Replace(tmp, SnapshotPath, BackupPath, true);
                    }
                    else
                    {
                        File.Move(tmp, SnapshotPath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                        "Material snapshot commit retry. attempt=" + attempt +
                        ", file=" + SnapshotPath +
                        ", error=" + ex.Message + " - Check");
                    Thread.Sleep(100);
                }
                finally
                {
                }
            }

            try
            {
                if (File.Exists(tmp))
                    File.Copy(tmp, RecoveryPath, true);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotRecovery",
                    "Material snapshot recovery copy failed. file=" + RecoveryPath +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }

            Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                "Material snapshot commit failed. recovery=" + RecoveryPath +
                ", error=" + (lastError != null ? lastError.Message : "-") + " - Failed");
            return false;
        }

        private static void CopyFailedSnapshotForDiagnosis(string tmp)
        {
            try
            {
                if (!File.Exists(tmp))
                    return;

                string failedPath = Path.Combine(Dir,
                    "material_state.failed." +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".json");
                File.Copy(tmp, failedPath, true);
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot failed candidate copied for diagnosis. file=" + failedPath + " - Check");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot failed candidate copy failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void CleanupStaleTempFiles()
        {
            try
            {
                if (File.Exists(SnapshotPath + ".tmp"))
                    File.Delete(SnapshotPath + ".tmp");

                foreach (string path in Directory.GetFiles(Dir, "material_state.json.*.tmp"))
                {
                    try { File.Delete(path); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot temp cleanup failed: " + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        private static void DeleteTempFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot temp delete failed. file=" + path +
                    ", error=" + ex.Message + " - Check");
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
                        NormalizeWaferNumbers(wafer);
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
                        die.PickedAt = NormalizeNullableDateTime(die.PickedAt);
                        NormalizeVisionOffset(die.WaferOffset);
                        NormalizeVisionOffset(die.BinOffset);

                        if (die.Inspections == null)
                            continue;

                        foreach (var inspection in die.Inspections)
                        {
                            if (inspection == null)
                                continue;

                            inspection.CreatedAt = NormalizeDateTime(inspection.CreatedAt, now);
                            inspection.UpdatedAt = NormalizeDateTime(inspection.UpdatedAt, now);
                            NormalizeVisionOffset(inspection.Offset);
                            NormalizeInspectionMeasurements(inspection);
                        }
                    }
                }

                int normalizedCount = NormalizeSnapshotObjectGraphDateTimes(
                    snapshot,
                    now,
                    new HashSet<object>(ReferenceEqualityComparer.Instance));
                if (normalizedCount > 0)
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotDateTime",
                        "Material snapshot DateTime values normalized before save. count=" +
                        normalizedCount + " - Check");
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

        private static int NormalizeSnapshotObjectGraphDateTimes(
            object value,
            DateTime fallback,
            HashSet<object> visited)
        {
            try
            {
                if (value == null)
                    return 0;

                Type type = value.GetType();
                if (IsSimpleSnapshotType(type))
                    return 0;

                if (!type.IsValueType)
                {
                    if (visited.Contains(value))
                        return 0;

                    visited.Add(value);
                }

                int count = 0;
                IList list = value as IList;
                if (list != null)
                {
                    foreach (object item in list)
                        count += NormalizeSnapshotObjectGraphDateTimes(item, fallback, visited);

                    return count;
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!property.CanRead || !property.CanWrite)
                        continue;
                    if (property.GetIndexParameters().Length > 0)
                        continue;

                    Type propertyType = property.PropertyType;
                    if (propertyType == typeof(DateTime))
                    {
                        DateTime before = (DateTime)property.GetValue(value, null);
                        DateTime after = NormalizeDateTimeForJson(before, fallback);
                        if (before != after || before.Kind != after.Kind)
                        {
                            property.SetValue(value, after, null);
                            count++;
                        }

                        continue;
                    }

                    Type nullableType = Nullable.GetUnderlyingType(propertyType);
                    if (nullableType == typeof(DateTime))
                    {
                        object beforeObject = property.GetValue(value, null);
                        if (beforeObject == null)
                            continue;

                        DateTime before = (DateTime)beforeObject;
                        DateTime after = NormalizeDateTimeForJson(before, fallback);
                        if (before != after || before.Kind != after.Kind)
                        {
                            property.SetValue(value, after, null);
                            count++;
                        }

                        continue;
                    }

                    if (IsSimpleSnapshotType(propertyType))
                        continue;

                    object child = property.GetValue(value, null);
                    count += NormalizeSnapshotObjectGraphDateTimes(child, fallback, visited);
                }

                return count;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotDateTime",
                    "Material snapshot DateTime graph normalize failed. type=" +
                    (value != null ? value.GetType().FullName : "-") +
                    ", error=" + ex.Message + " - Check");
                return 0;
            }
            finally
            {
            }
        }

        private static bool IsSimpleSnapshotType(Type type)
        {
            if (type == null)
                return true;
            if (type.IsPrimitive || type.IsEnum)
                return true;
            if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
                return true;

            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return IsSimpleSnapshotType(nullableType);

            return false;
        }

        private static void NormalizeWaferNumbers(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null)
                    return;

                wafer.SourceCassetteSlotPosition = NormalizeJsonDouble(wafer.SourceCassetteSlotPosition, -1.0);
                wafer.CurrentCassetteSlotPosition = NormalizeJsonDouble(wafer.CurrentCassetteSlotPosition, -1.0);
                wafer.InputStageAlignOriginX = NormalizeJsonDouble(wafer.InputStageAlignOriginX, 0.0);
                wafer.InputStageAlignOriginY = NormalizeJsonDouble(wafer.InputStageAlignOriginY, 0.0);
                wafer.InputStageAlignPitchX = NormalizeJsonDouble(wafer.InputStageAlignPitchX, 0.0);
                wafer.InputStageAlignPitchY = NormalizeJsonDouble(wafer.InputStageAlignPitchY, 0.0);
                wafer.InputStageAlignOffsetX = NormalizeJsonDouble(wafer.InputStageAlignOffsetX, 0.0);
                wafer.InputStageAlignOffsetY = NormalizeJsonDouble(wafer.InputStageAlignOffsetY, 0.0);
                wafer.InputStageDieMappingOffsetX = NormalizeJsonDouble(wafer.InputStageDieMappingOffsetX, 0.0);
                wafer.InputStageDieMappingOffsetY = NormalizeJsonDouble(wafer.InputStageDieMappingOffsetY, 0.0);
                wafer.OutputReceivePitchX = NormalizeJsonDouble(wafer.OutputReceivePitchX, 0.0);
                wafer.OutputReceivePitchY = NormalizeJsonDouble(wafer.OutputReceivePitchY, 0.0);
                wafer.OutputReceiveOriginX = NormalizeJsonDouble(wafer.OutputReceiveOriginX, 0.0);
                wafer.OutputReceiveOriginY = NormalizeJsonDouble(wafer.OutputReceiveOriginY, 0.0);

                if (wafer.OutputReceiveSlots == null)
                    return;

                foreach (var slot in wafer.OutputReceiveSlots)
                {
                    if (slot == null)
                        continue;

                    slot.PosX = NormalizeJsonDouble(slot.PosX, 0.0);
                    slot.PosY = NormalizeJsonDouble(slot.PosY, 0.0);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot wafer numeric normalize failed. wafer=" +
                    (wafer != null ? wafer.WaferId : "-") +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void NormalizeVisionOffset(VisionOffset offset)
        {
            try
            {
                if (offset == null)
                    return;

                offset.X = NormalizeJsonDouble(offset.X, 0.0);
                offset.Y = NormalizeJsonDouble(offset.Y, 0.0);
                offset.R = NormalizeJsonDouble(offset.R, 0.0);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void NormalizeInspectionMeasurements(DieInspectionRecord inspection)
        {
            try
            {
                if (inspection == null || inspection.Measurements == null)
                    return;

                foreach (var measurement in inspection.Measurements)
                {
                    if (measurement == null)
                        continue;

                    measurement.Value = NormalizeJsonDouble(measurement.Value, 0.0);
                    measurement.LowerLimit = NormalizeJsonDouble(measurement.LowerLimit, 0.0);
                    measurement.UpperLimit = NormalizeJsonDouble(measurement.UpperLimit, 0.0);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static double NormalizeJsonDouble(double value, double fallback)
        {
            try
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
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

        private static DateTime NormalizeNullableDateTime(DateTime value)
        {
            try
            {
                return NormalizeOptionalDateTimeForJson(value);
            }
            catch
            {
                return SafeEmptyDateTime;
            }
            finally
            {
            }
        }

        private static DateTime NormalizeDateTime(DateTime value, DateTime fallback)
        {
            try
            {
                return NormalizeDateTimeForJson(value, fallback);
            }
            catch
            {
                return NormalizeDateTimeKind(fallback);
            }
            finally
            {
            }
        }

        private static DateTime NormalizeOptionalDateTimeForJson(DateTime value)
        {
            try
            {
                if (value == default(DateTime) || value <= DateTime.MinValue.AddYears(1))
                    return SafeEmptyDateTime;

                if (value >= DateTime.MaxValue.AddYears(-1))
                    return NormalizeDateTimeKind(DateTime.Now);

                return NormalizeDateTimeKind(value);
            }
            catch
            {
                return SafeEmptyDateTime;
            }
            finally
            {
            }
        }

        private static DateTime NormalizeDateTimeForJson(DateTime value, DateTime fallback)
        {
            try
            {
                if (value == default(DateTime) ||
                    value <= DateTime.MinValue.AddYears(1) ||
                    value >= DateTime.MaxValue.AddYears(-1))
                    return NormalizeDateTimeKind(fallback);

                return NormalizeDateTimeKind(value);
            }
            catch
            {
                return NormalizeDateTimeKind(fallback);
            }
            finally
            {
            }
        }

        private static DateTime NormalizeDateTimeKind(DateTime value)
        {
            try
            {
                if (value == default(DateTime) ||
                    value <= DateTime.MinValue.AddYears(1) ||
                    value >= DateTime.MaxValue.AddYears(-1))
                    return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

                if (value.Kind == DateTimeKind.Unspecified)
                    return DateTime.SpecifyKind(value, DateTimeKind.Local);

                return value;
            }
            catch
            {
                return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            }
            finally
            {
            }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            private ReferenceEqualityComparer()
            {
            }

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
