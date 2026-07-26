using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static DateTime _lastTempCleanupUtc = DateTime.MinValue;

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
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                MaterialSnapshot fastSnapshot;
                string fastReason;
                if (TryLoadPrimarySnapshotFast(out fastSnapshot, out fastReason))
                {
                    LogLoadElapsed("Fast", sw.ElapsedMilliseconds, fastSnapshot);
                    return fastSnapshot;
                }

                if (!string.IsNullOrWhiteSpace(fastReason) && File.Exists(SnapshotPath))
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                        "Material snapshot fast load skipped. reason=" + fastReason + " - Check");
                }

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
                        ", pickerDies=" + CountPickerRelatedDies(selected.Snapshot) +
                        ", savedAt=" + selected.Snapshot.SavedAt.ToString("yyyy-MM-dd HH:mm:ss") + " - Ok");
                }

                NormalizeSnapshotStates(selected.Snapshot);
                LogLoadElapsed("Fallback", sw.ElapsedMilliseconds, selected.Snapshot);
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

        private static bool TryLoadPrimarySnapshotFast(out MaterialSnapshot snapshot, out string reason)
        {
            snapshot = null;
            reason = "";

            try
            {
                if (!File.Exists(SnapshotPath))
                {
                    reason = "primary snapshot does not exist";
                    return false;
                }

                DateTime snapshotWriteTime = File.GetLastWriteTime(SnapshotPath);
                string alternatePath;
                if (HasNewerAlternateSnapshotCandidate(snapshotWriteTime, out alternatePath))
                {
                    reason = "newer alternate snapshot candidate exists. file=" + alternatePath;
                    return false;
                }

                string error;
                if (!TryLoadFromPath(SnapshotPath, out snapshot, out error))
                {
                    reason = "primary snapshot load failed. error=" + error;
                    return false;
                }

                if (IsStartupInitializeReason(snapshot.SaveReason))
                {
                    reason = "primary snapshot is startup initialize reason. reason=" + snapshot.SaveReason;
                    snapshot = null;
                    return false;
                }

                LastLoadedPath = SnapshotPath;
                return true;
            }
            catch (Exception ex)
            {
                snapshot = null;
                reason = "fast load exception. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool HasNewerAlternateSnapshotCandidate(DateTime snapshotWriteTime, out string alternatePath)
        {
            alternatePath = "";

            try
            {
                if (IsFileNewerThan(RecoveryPath, snapshotWriteTime))
                {
                    alternatePath = RecoveryPath;
                    return true;
                }

                string legacyTemp = SnapshotPath + ".tmp";
                if (IsFileNewerThan(legacyTemp, snapshotWriteTime))
                {
                    alternatePath = legacyTemp;
                    return true;
                }

                if (!Directory.Exists(Dir))
                    return false;

                foreach (string path in Directory.GetFiles(Dir, "material_state.json.*.tmp"))
                {
                    if (!IsFileNewerThan(path, snapshotWriteTime))
                        continue;

                    alternatePath = path;
                    return true;
                }

                return false;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static bool IsFileNewerThan(string path, DateTime baseline)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            FileInfo file = new FileInfo(path);
            return file.Length >= 16 && file.LastWriteTime > baseline;
        }

        private static void LogLoadElapsed(string mode, long elapsedMs, MaterialSnapshot snapshot)
        {
            try
            {
                if (elapsedMs < 300)
                    return;

                Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                    "Material snapshot load completed. mode=" + mode +
                    ", elapsedMs=" + elapsedMs +
                    ", wafers=" + CountList(snapshot != null ? snapshot.Wafers : null) +
                    ", dies=" + CountList(snapshot != null ? snapshot.Dies : null) +
                    ", file=" + LastLoadedPath + " - Ok");
            }
            catch
            {
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
            score += CountPickerRelatedDies(snapshot) * 10000;
            score += CountLocatedWafers(snapshot) * 1000;

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

        private static int CountPickerRelatedDies(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null || snapshot.Dies == null)
                    return 0;

                int count = 0;
                foreach (var die in snapshot.Dies)
                {
                    if (die == null)
                        continue;

                    MaterialLocation location = die.CurrentLocation;
                    MaterialLocationKind kind = location != null ? location.Kind : MaterialLocationKind.Unknown;
                    if (kind == MaterialLocationKind.PickerFront ||
                        kind == MaterialLocationKind.PickerRear ||
                        die.PickedPickerNo > 0 ||
                        die.ReservedPickerNo > 0)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static int CountLocatedWafers(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null || snapshot.Wafers == null)
                    return 0;

                int count = 0;
                foreach (var wafer in snapshot.Wafers)
                {
                    if (wafer == null || wafer.CurrentLocation == null)
                        continue;

                    MaterialLocationKind kind = wafer.CurrentLocation.Kind;
                    if (kind != MaterialLocationKind.Unknown)
                        count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
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
            if (snapshot == null)
                return;

            snapshot.LotId = string.IsNullOrWhiteSpace(snapshot.LotId)
                ? ""
                : snapshot.LotId.Trim();

            if (snapshot.Wafers != null)
            {
                foreach (var wafer in snapshot.Wafers)
                {
                    if (wafer != null)
                        wafer.State = WaferMaterialStateText.Normalize(wafer.State);
                }
            }

            NormalizeDuplicatePickerDieLocations(snapshot);
        }

        private static void NormalizeDuplicatePickerDieLocations(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null || snapshot.Dies == null)
                    return;

                var pickerDies = snapshot.Dies
                    .Where(d =>
                        d != null &&
                        d.CurrentLocation != null &&
                        IsPickerLocation(d.CurrentLocation.Kind) &&
                        d.CurrentLocation.PickerNo > 0)
                    .GroupBy(d => d.CurrentLocation.Kind.ToString() + ":" + d.CurrentLocation.PickerNo);

                foreach (var group in pickerDies)
                {
                    List<DieMaterial> ordered = group
                        .OrderByDescending(GetPickerDieSortTime)
                        .ThenByDescending(d => d != null ? d.InputSequenceNo : 0)
                        .ToList();

                    if (ordered.Count <= 1)
                        continue;

                    DieMaterial keep = ordered[0];
                    MaterialLocationKind location = keep.CurrentLocation.Kind;
                    int pickerNo = keep.CurrentLocation.PickerNo;

                    for (int i = 1; i < ordered.Count; i++)
                    {
                        DieMaterial duplicate = ordered[i];
                        if (duplicate == null)
                            continue;

                        duplicate.CurrentLocation = MaterialLocation.Unknown();
                        duplicate.ReservedPickerLocation = MaterialLocationKind.Unknown;
                        duplicate.ReservedPickerNo = -1;
                        duplicate.UpdatedAt = DateTime.Now;

                        Log.Write("Main", "SYSTEM", "MaterialSnapshotNormalize",
                            "피커 위치 중복 Material 정리. 위치=" + location +
                            ", pickerNo=" + pickerNo +
                            ", 유지Die=" + (keep != null ? keep.DieId : "-") +
                            ", 정리Die=" + duplicate.DieId + " - Check");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotNormalize",
                    "피커 위치 중복 Material 정리 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool IsPickerLocation(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.PickerFront ||
                   kind == MaterialLocationKind.PickerRear;
        }

        private static DateTime GetPickerDieSortTime(DieMaterial die)
        {
            if (die == null)
                return DateTime.MinValue;

            DateTime updated = die.UpdatedAt;
            DateTime picked = die.PickedAt;
            return updated >= picked ? updated : picked;
        }

        /// <summary>
        /// 저장용 스냅샷 사본을 만든다.
        /// [정정 2026-07-27] 기존에는 Save() 안에서 라이브 State를 _stateSync 락 없이 클론해
        /// 저장 중 시퀀스가 Die를 추가하면 "컬렉션이 수정되었습니다" 예외가 날 수 있었다.
        /// 호출자가 상태 락을 쥔 채 이 메서드로 사본을 먼저 만들고, 디스크 쓰기는 락 밖에서 하도록 분리한다.
        /// </summary>
        public static MaterialSnapshot CreateSaveCopy(MaterialSnapshot snapshot)
        {
            MaterialSnapshot copy = CloneSnapshotForSave(snapshot);
            TrimInspectionDetailIfDisabled(copy);
            return copy;
        }

        /// <summary>
        /// 설정(LOG SETTINGS → MATERIAL SNAPSHOT)이 꺼져 있으면 저장 사본에서 검사 측정값 상세를 비운다.
        /// 런타임 객체는 그대로 두므로 화면/CSV/시퀀스 판정에는 영향이 없고, 디스크에 쓰는 양만 줄어든다.
        /// (2026-07-27 실측: Measurements/Alignments가 스냅샷 용량의 약 80%를 차지)
        /// </summary>
        private static void TrimInspectionDetailIfDisabled(MaterialSnapshot copy)
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && settings.SaveMaterialInspectionDetail)
                    return;

                if (copy == null || copy.Dies == null)
                    return;

                foreach (DieMaterial die in copy.Dies)
                {
                    if (die == null || die.Inspections == null)
                        continue;

                    foreach (DieInspectionRecord record in die.Inspections)
                    {
                        if (record == null)
                            continue;

                        // 재개에 필요한 InspectionType/Result/Offset/NgCodes는 남기고 상세만 비운다.
                        if (record.Measurements != null && record.Measurements.Count > 0)
                            record.Measurements = new List<InspectionMeasurement>();
                        if (record.Alignments != null && record.Alignments.Count > 0)
                            record.Alignments = new List<InspectionAlignmentSnapshot>();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "검사 측정값 상세 제외 처리에 실패했습니다(상세 포함 상태로 저장). error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        public static bool Save(MaterialSnapshot snapshot)
        {
            return Save(snapshot, false);
        }

        /// <param name="alreadyCopied">
        /// true이면 snapshot이 이미 CreateSaveCopy로 만든 사본이므로 다시 클론하지 않는다.
        /// </param>
        public static bool Save(MaterialSnapshot snapshot, bool alreadyCopied)
        {
            if (snapshot == null)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave", "Material snapshot save failed: snapshot is null. - Failed");
                return false;
            }

            string tmp = null;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(Dir);
                MaterialSnapshot saveSnapshot = alreadyCopied ? snapshot : CloneSnapshotForSave(snapshot);
                saveSnapshot.SavedAt = DateTime.Now;
                NormalizeSnapshotStates(saveSnapshot);
                NormalizeSnapshotDateTimes(saveSnapshot);

                tmp = Path.Combine(Dir,
                    "material_state.json." +
                    DateTime.Now.ToString("yyyyMMddHHmmssfff") + "." +
                    Guid.NewGuid().ToString("N") + ".tmp");

                using (var fs = File.Create(tmp))
                {
                    WriteSnapshotCompact(fs, saveSnapshot);
                }

                if (!IsWrittenSnapshotFileUsable(tmp))
                {
                    CopyFailedSnapshotForDiagnosis(tmp);
                    DeleteTempFile(tmp);
                    return false;
                }

                bool validateWrittenSnapshot = ShouldValidateWrittenSnapshot(saveSnapshot.SaveReason);
                if (validateWrittenSnapshot && !ValidateWrittenSnapshot(tmp, saveSnapshot))
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

                if (committed)
                    LogSaveElapsed(sw.ElapsedMilliseconds, saveSnapshot, validateWrittenSnapshot);

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

        private static void WriteSnapshotCompact(Stream stream, MaterialSnapshot snapshot)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var serializer = new DataContractJsonSerializer(typeof(MaterialSnapshot));
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, false, "  "))
            {
                serializer.WriteObject(writer, snapshot);
                writer.Flush();
            }
        }

        private static bool IsWrittenSnapshotFileUsable(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                        "Material snapshot validation failed. file does not exist. file=" + path + " - Failed");
                    return false;
                }

                long length = new FileInfo(path).Length;
                if (length < 16)
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                        "Material snapshot validation failed. file is too small. file=" + path +
                        ", length=" + length + " - Failed");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot validation failed. file check error. file=" + path +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool ShouldValidateWrittenSnapshot(string reason)
        {
            string value = reason ?? "";
            return value.IndexOf("ApplicationExit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Initialize", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Manual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Mapping", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("MapTransfer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Dialog", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // [검증 계측 2026-07-27] 저장 소요 누적 통계.
        // 저장은 5초 병합이라 분당 3회 수준이므로 매 저장을 남겨도 로그 부담이 없다.
        private const long SlowSnapshotSaveMs = 300;
        private static readonly object _saveStatSync = new object();
        private static long _saveStatCount;
        private static long _saveStatTotalMs;
        private static long _saveStatMaxMs;

        /// <summary>
        /// 스냅샷 저장 소요를 기록한다.
        /// [정정 2026-07-27] 기존에는 Log.Write(class,...) 경로를 써서 Release(ProductionMinimal)에서
        /// 전부 억제되었고, 300ms 초과분만 기록해 개선 전후를 비교할 수가 없었다.
        /// LogLevel 지정 오버로드는 LogPolicy를 거치지 않으므로 Release에서도 남는다.
        /// 매 저장의 소요/누적평균/최대와 파일 크기를 함께 남겨, 프로퍼티 캐시 적용 효과와
        /// 스냅샷 증가에 따른 악화 추이를 현장 로그만으로 확인할 수 있게 한다.
        /// </summary>
        private static void LogSaveElapsed(long elapsedMs, MaterialSnapshot snapshot, bool validated)
        {
            try
            {
                long count;
                long avgMs;
                long maxMs;
                lock (_saveStatSync)
                {
                    _saveStatCount++;
                    _saveStatTotalMs += elapsedMs;
                    if (elapsedMs > _saveStatMaxMs)
                        _saveStatMaxMs = elapsedMs;

                    count = _saveStatCount;
                    avgMs = _saveStatTotalMs / _saveStatCount;
                    maxMs = _saveStatMaxMs;
                }

                bool slow = elapsedMs >= SlowSnapshotSaveMs;
                Log.Write(
                    slow ? LogLevel.AboveNormal : LogLevel.Normal,
                    "Main",
                    "MaterialSnapshotSave",
                    "Material snapshot save completed. elapsedMs=" + elapsedMs +
                    ", avgMs=" + avgMs +
                    ", maxMs=" + maxMs +
                    ", saves=" + count +
                    ", validated=" + validated +
                    ", wafers=" + CountList(snapshot != null ? snapshot.Wafers : null) +
                    ", dies=" + CountList(snapshot != null ? snapshot.Dies : null) +
                    ", bytes=" + ResolveSnapshotFileLength() +
                    (slow ? " - Check" : " - Ok"));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static long ResolveSnapshotFileLength()
        {
            try
            {
                var info = new FileInfo(SnapshotPath);
                return info.Exists ? info.Length : 0L;
            }
            catch
            {
                return -1L;
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

        // [택타임 개선 2026-07-27] 스냅샷 저장은 전체 객체 그래프를 리플렉션으로 두 번 순회한다
        // (딥클론 + DateTime 정규화). Type.GetProperties()는 호출할 때마다 새 배열을 만들고 메타데이터를
        // 조회하므로, Die 수천 개 + Inspection 규모에서는 이 호출 자체가 저장 비용의 대부분이었다.
        // (실측: 회당 343~447ms, material_state.json 3.9MB -> 7.5MB로 커지며 계속 악화)
        // 타입별 프로퍼티 목록은 런타임에 변하지 않으므로 캐시해서 재사용한다.
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _snapshotPropertyCache =
            new ConcurrentDictionary<Type, PropertyInfo[]>();

        private static PropertyInfo[] GetSnapshotProperties(Type type)
        {
            return _snapshotPropertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                    .ToArray());
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
            foreach (PropertyInfo property in GetSnapshotProperties(type))
            {
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
            string expectedLotId = string.IsNullOrWhiteSpace(expected.LotId) ? "" : expected.LotId.Trim();
            string loadedLotId = string.IsNullOrWhiteSpace(loaded.LotId) ? "" : loaded.LotId.Trim();

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

            if (!string.Equals(expectedLotId, loadedLotId, StringComparison.Ordinal))
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot validation failed. saved LOT ID mismatch. file=" + path +
                    ", expectedLotId=" + expectedLotId +
                    ", loadedLotId=" + loadedLotId + " - Failed");
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
                DateTime now = DateTime.UtcNow;
                if ((now - _lastTempCleanupUtc).TotalSeconds < 30)
                    return;

                _lastTempCleanupUtc = now;

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

                foreach (PropertyInfo property in GetSnapshotProperties(type))
                {
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
                wafer.InputStageAlignResultMode = wafer.InputStageAlignResultMode ?? "";
                wafer.InputStageAlignResultRunId = wafer.InputStageAlignResultRunId ?? "";
                wafer.InputStageDieMappingResultMode = wafer.InputStageDieMappingResultMode ?? "";
                wafer.InputStageDieMappingAlignRunId = wafer.InputStageDieMappingAlignRunId ?? "";
                wafer.InputStageAlignOriginX = NormalizeJsonDouble(wafer.InputStageAlignOriginX, 0.0);
                wafer.InputStageAlignOriginY = NormalizeJsonDouble(wafer.InputStageAlignOriginY, 0.0);
                wafer.InputStageAlignPitchX = NormalizeJsonDouble(wafer.InputStageAlignPitchX, 0.0);
                wafer.InputStageAlignPitchY = NormalizeJsonDouble(wafer.InputStageAlignPitchY, 0.0);
                wafer.InputStageDieSizeX = NormalizeJsonDouble(wafer.InputStageDieSizeX, 0.0);
                wafer.InputStageDieSizeY = NormalizeJsonDouble(wafer.InputStageDieSizeY, 0.0);
                wafer.InputStageOuterDiameterMm = NormalizeJsonDouble(wafer.InputStageOuterDiameterMm, 0.0);
                wafer.InputStageAlignOffsetX = NormalizeJsonDouble(wafer.InputStageAlignOffsetX, 0.0);
                wafer.InputStageAlignOffsetY = NormalizeJsonDouble(wafer.InputStageAlignOffsetY, 0.0);
                wafer.InputStageAlignReferenceT = NormalizeJsonDouble(wafer.InputStageAlignReferenceT, 0.0);
                wafer.InputStageAlignCorrectedT = NormalizeJsonDouble(wafer.InputStageAlignCorrectedT, 0.0);
                wafer.InputStageAlignOffsetT = NormalizeJsonDouble(wafer.InputStageAlignOffsetT, 0.0);
                wafer.InputStageDieMappingOffsetX = NormalizeJsonDouble(wafer.InputStageDieMappingOffsetX, 0.0);
                wafer.InputStageDieMappingOffsetY = NormalizeJsonDouble(wafer.InputStageDieMappingOffsetY, 0.0);
                wafer.InputStageDieMappingOriginX = NormalizeJsonDouble(wafer.InputStageDieMappingOriginX, 0.0);
                wafer.InputStageDieMappingOriginY = NormalizeJsonDouble(wafer.InputStageDieMappingOriginY, 0.0);
                wafer.InputStageDieMappingCorrectedT = NormalizeJsonDouble(wafer.InputStageDieMappingCorrectedT, 0.0);
                wafer.OutputReceivePitchX = NormalizeJsonDouble(wafer.OutputReceivePitchX, 0.0);
                wafer.OutputReceivePitchY = NormalizeJsonDouble(wafer.OutputReceivePitchY, 0.0);
                wafer.OutputReceiveDieSizeX = NormalizeJsonDouble(wafer.OutputReceiveDieSizeX, 0.0);
                wafer.OutputReceiveDieSizeY = NormalizeJsonDouble(wafer.OutputReceiveDieSizeY, 0.0);
                wafer.OutputReceiveOuterDiameterMm = NormalizeJsonDouble(wafer.OutputReceiveOuterDiameterMm, 0.0);
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
                if (inspection == null)
                    return;

                if (inspection.Measurements != null)
                {
                    foreach (var measurement in inspection.Measurements)
                    {
                        if (measurement == null)
                            continue;

                        measurement.Value = NormalizeJsonDouble(measurement.Value, 0.0);
                        measurement.LowerLimit = NormalizeJsonDouble(measurement.LowerLimit, 0.0);
                        measurement.UpperLimit = NormalizeJsonDouble(measurement.UpperLimit, 0.0);
                    }
                }

                if (inspection.Alignments != null)
                {
                    foreach (var alignment in inspection.Alignments)
                    {
                        if (alignment == null)
                            continue;

                        alignment.X = NormalizeJsonDouble(alignment.X, 0.0);
                        alignment.Y = NormalizeJsonDouble(alignment.Y, 0.0);
                        alignment.T = NormalizeJsonDouble(alignment.T, 0.0);
                        alignment.Z = NormalizeJsonDouble(alignment.Z, 0.0);
                        NormalizeVisionOffset(alignment.Offset);
                    }
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
