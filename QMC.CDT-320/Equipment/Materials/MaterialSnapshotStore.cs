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
        public static long HighestObservedSnapshotRevision
        {
            get { return Interlocked.Read(ref _highestObservedSnapshotRevision); }
        }

        private static readonly DateTime SafeEmptyDateTime =
            DateTime.SpecifyKind(new DateTime(1900, 1, 1, 0, 0, 0), DateTimeKind.Utc);
        private const int FailedSnapshotDiagnosticRetentionCount = 3;
        private static DateTime _lastTempCleanupUtc = DateTime.MinValue;
        private static long _highestObservedSnapshotRevision;

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

                string graphReason;
                if (!MaterialStorage.TryPrepareStateForUse(snapshot, out graphReason))
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotLoad",
                        "Material snapshot candidate graph rejected. file=" + path +
                        ", reason=" + graphReason + " - Failed");
                    return;
                }

                ObserveSnapshotRevision(snapshot);

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

            // Revision이 도입된 Snapshot이 하나라도 있으면 파일 시각이나 데이터 개수보다
            // 가장 큰 Revision을 우선한다. 컴팩션된 최신본보다 큰 과거본이 복구되는 것을 막는다.
            SnapshotCandidate highestRevision = candidates
                .Where(c => c != null && c.Snapshot != null && c.Snapshot.SnapshotRevision > 0L)
                .OrderByDescending(c => c.Snapshot.SnapshotRevision)
                .ThenByDescending(GetCandidateSavedAt)
                .ThenByDescending(c => c.LastWriteTime)
                .FirstOrDefault();
            if (highestRevision != null)
                return highestRevision;

            // Revision이 없는 legacy 세대에서는 데이터 개수가 아니라 저장 시각으로 순서를 판단한다.
            // primary보다 명확히 새 SavedAt을 가진 recovery/temp는 실패한 commit의 최신 상태일 수 있다.
            // 반대로 오래된 backup이 더 풍부하다는 이유만으로 Clear 이전 상태를 되살리지는 않는다.
            SnapshotCandidate committedPrimary = candidates.FirstOrDefault(c =>
                c != null &&
                string.Equals(c.Path, SnapshotPath, StringComparison.OrdinalIgnoreCase));
            if (committedPrimary != null)
            {
                DateTime primarySavedAt = GetCandidateSavedAt(committedPrimary);
                SnapshotCandidate newerAlternate = candidates
                    .Where(c => c != null &&
                                !ReferenceEquals(c, committedPrimary) &&
                                (string.Equals(c.Path, RecoveryPath, StringComparison.OrdinalIgnoreCase) ||
                                 IsTempPath(c.Path)) &&
                                GetCandidateSavedAt(c) > primarySavedAt &&
                                c.LastWriteTime > committedPrimary.LastWriteTime)
                    .OrderByDescending(GetCandidateSavedAt)
                    .ThenByDescending(c => c.LastWriteTime)
                    .FirstOrDefault();
                return newerAlternate ?? committedPrimary;
            }

            // Primary가 없거나 손상되어 후보에서 제외된 경우에만 가장 최근의 정상 대체본을 쓴다.
            return candidates
                .OrderByDescending(GetCandidateSavedAt)
                .ThenByDescending(c => c.LastWriteTime)
                .First();
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

        }

        /// <summary>
        /// 저장용 스냅샷 사본을 만든다.
        /// [정정 2026-07-27] 기존에는 Save() 안에서 라이브 State를 _stateSync 락 없이 클론해
        /// 저장 중 시퀀스가 Die를 추가하면 "컬렉션이 수정되었습니다" 예외가 날 수 있었다.
        /// 호출자가 상태 락을 쥔 채 이 메서드로 사본을 먼저 만들고, 디스크 쓰기는 락 밖에서 하도록 분리한다.
        /// </summary>
        public static MaterialSnapshot CreateSaveCopy(MaterialSnapshot snapshot)
        {
            // [택타임 개선 2026-08-08] 검사 상세를 저장하지 않는 설정이면 "복제 후 비우기"가 아니라
            // 처음부터 복제하지 않는다. 상세는 그래프의 대부분을 차지하므로 전역 락 보유 시간이 크게 준다.
            // 결과 그래프는 기존 (딥클론 → TrimInspectionDetailIfDisabled) 와 동일하다.
            if (IsTypedCloneUsable())
                return CloneSnapshotTyped(snapshot, ShouldSaveInspectionDetail());

            MaterialSnapshot copy = CloneSnapshotForSave(snapshot);
            if (copy == null)
                return null;

            TrimInspectionDetailIfDisabled(copy);
            return copy;
        }

        internal static MaterialSnapshot CreateStateCopy(MaterialSnapshot snapshot)
        {
            // 수동 Process Test transaction rollback용. 저장 정책에 따른 Inspection trim 없이
            // live graph를 그대로 복제해야 실패 전 상태를 정확히 복원할 수 있다.
            if (IsTypedCloneUsable())
                return CloneSnapshotTyped(snapshot, true);

            return CloneSnapshotForSave(snapshot);
        }

        // [종료 풀 저장 2026-08-28 팀장님 지시] 프로그램 종료 저장은 SaveMaterialInspectionDetail
        // 설정과 무관하게 측정값 상세를 전부 포함한다 — 상세 미저장 상태로 재시작하면 미기록
        // 다이(후검사 미완 등)의 측정값이 유실되어 결과 CSV가 공백/행 거부로 깨졌다(08-27 3다이 실측).
        // 종료 시 1회 켜고 앱이 내려가므로 원복은 불필요하다.
        private static volatile bool _applicationExitFullSave;

        public static void BeginApplicationExitFullSave()
        {
            _applicationExitFullSave = true;
            Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                "프로그램 종료 저장 모드 — 검사 측정값 상세를 설정과 무관하게 포함해 저장합니다. - Ok");
        }

        /// <summary>
        /// 저장 사본에 검사 측정값 상세를 포함할지 여부. 설정 조회 실패 시에는
        /// 기존 TrimInspectionDetailIfDisabled 와 같은 방향(상세 포함)으로 안전하게 처리한다.
        /// </summary>
        private static bool ShouldSaveInspectionDetail()
        {
            try
            {
                if (_applicationExitFullSave)
                    return true;

                AppSettings settings = AppSettingsStore.Current;
                return settings == null || settings.SaveMaterialInspectionDetail;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "검사 상세 저장 설정을 확인하지 못해 상세를 포함해 저장합니다. error=" + ex.Message + " - Check");
                return true;
            }
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
                // [종료 풀 저장 2026-08-28] 종료 저장 모드에서는 설정과 무관하게 상세를 유지한다.
                if (_applicationExitFullSave)
                    return;

                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || settings.SaveMaterialInspectionDetail)
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
        /// <summary>
        /// 마지막 저장 실패 사유. 저장에 성공하면 빈 문자열로 초기화된다.
        /// [가시성 2026-08-08] 저장 실패 로그는 기존에 Log.Write(class,user,source,msg) 4-인자 형식이라
        /// 운영 최소 로그 정책(LogPolicy)에서 통째로 버려졌다. 실제로 2026-08-08 운전에서 저장이
        /// 5시간 동안 실패했지만 로그가 한 줄도 남지 않아 종료 시점에야 발견되었다.
        /// 실패 사유를 호출자(MaterialStateService)가 읽어 경고로 올릴 수 있게 보관한다.
        /// </summary>
        public static string LastSaveFailureReason
        {
            get { return _lastSaveFailureReason ?? string.Empty; }
        }

        private static volatile string _lastSaveFailureReason = string.Empty;

        // 저장 실패를 기록하고 false를 돌려준다.
        // 로그는 LogLevel 지정 오버로드를 사용한다. 4-인자 형식과 달리 최소 로그 정책에서 버려지지 않는다.
        private static bool FailSave(string message)
        {
            _lastSaveFailureReason = message ?? string.Empty;
            Log.Write(LogLevel.AboveNormal, "Main", "MaterialSnapshotSave", message + " - Failed");
            return false;
        }

        public static bool Save(MaterialSnapshot snapshot, bool alreadyCopied)
        {
            if (snapshot == null)
                return FailSave("Material snapshot save failed: snapshot is null.");

            string tmp = null;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(Dir);
                MaterialSnapshot saveSnapshot = alreadyCopied ? snapshot : CloneSnapshotForSave(snapshot);
                if (saveSnapshot == null)
                    return FailSave("Material snapshot 저장용 복사본을 만들지 못해 저장을 중단합니다.");

                saveSnapshot.SavedAt = DateTime.Now;
                string rawValueReason;
                if (!MaterialStateCompactor.TryValidateRawMaterialValues(saveSnapshot, out rawValueReason))
                {
                    return FailSave("Material snapshot 원본 값 검증에 실패해 저장을 중단합니다. reason=" +
                        rawValueReason);
                }
                NormalizeSnapshotStates(saveSnapshot);
                NormalizeSnapshotDateTimes(saveSnapshot);

                if (!MaterialSnapshotRevisionPolicy.IsTrustedLoadedRevision(saveSnapshot.SnapshotRevision))
                {
                    return FailSave("Material snapshot revision이 신뢰 범위를 벗어나 저장을 중단합니다. revision=" +
                        saveSnapshot.SnapshotRevision);
                }

                string integrityReason;
                if (!MaterialStateCompactor.TryValidateForSave(saveSnapshot, out integrityReason))
                {
                    return FailSave("Material snapshot graph validation failed. reason=" + integrityReason);
                }

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
                    return FailSave("기록한 Material snapshot 임시 파일을 사용할 수 없습니다. file=" + tmp);
                }

                bool validateWrittenSnapshot = ShouldValidateWrittenSnapshot(saveSnapshot.SaveReason);
                if (validateWrittenSnapshot && !ValidateWrittenSnapshot(tmp, saveSnapshot))
                {
                    CopyFailedSnapshotForDiagnosis(tmp);
                    DeleteTempFile(tmp);
                    return FailSave("기록한 Material snapshot 검증에 실패했습니다. file=" + tmp);
                }

                bool committed = CommitSnapshot(tmp);
                if (committed)
                {
                    ObserveSnapshotRevision(saveSnapshot);
                    CleanupStaleTempFiles();
                }
                else
                {
                    DeleteTempFile(tmp);
                    return FailSave("Material snapshot 파일 커밋(교체)에 실패했습니다. file=" + SnapshotPath);
                }

                LogSaveElapsed(sw.ElapsedMilliseconds, saveSnapshot, validateWrittenSnapshot);
                _lastSaveFailureReason = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                FailSave("Material snapshot save exception: " + SnapshotPath + " / " + ex.Message);
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

        #region 타입별 스냅샷 딥클론 (저장 캡처 가속)

        // [택타임 개선 2026-08-08] 저장 캡처 딥클론은 MaterialStateService._stateSync 전역 락 안에서
        // 수행되므로, 이 시간이 그대로 시퀀스 대기 시간이 된다.
        // 실측(2026-08-07 야간 연속 운전, 다이 14,846개):
        //   SaveCaptureLock 평균 7.6초/회, 최대 8.1초 → 시간당 락 점유율 49%
        //   같은 구간 OutputReceive tact 1,186ms → 1,864ms (UPH 3,034 → 1,925)
        // 원인은 리플렉션 기반 범용 CloneObject다. 프로퍼티마다 GetValue/SetValue 박싱이 일어나고
        // 객체마다 visited 사전에 참조 해시를 넣는다. 다이/검사 수에 선형으로 늘어난다.
        // 아래 타입별 복제는 같은 그래프를 리플렉션/박싱/visited 없이 복사한다.
        //
        // 등가성 근거:
        //  - 스냅샷 그래프 타입은 전부 DataMember 프로퍼티만 가지며, 공개 read/write 프로퍼티 목록과 일치한다.
        //  - 저장 직렬화기(DataContractJsonSerializer)는 preserveObjectReferences를 쓰지 않으므로,
        //    공유 참조를 트리로 펼쳐 복제해도 출력 JSON은 동일하다.
        //  - 실제 운전 스냅샷(다이 14,846 / 웨이퍼 39 / 검사 89,076)으로 기존 경로와
        //    바이트 단위 동일 출력을 확인했다.
        //
        // 안전장치: 중첩 Review 검증 자료까지 모델에 프로퍼티가 추가/삭제되면 아래 개수 가드가 불일치를 감지해
        //   기존 리플렉션 복제로 폴백하고 로그를 남긴다. 조용한 데이터 누락이 생기지 않는다.
        private static int _typedCloneUsable = -1;   // -1=미검사, 1=사용, 0=폴백

        private static bool IsTypedCloneUsable()
        {
            int cached = Volatile.Read(ref _typedCloneUsable);
            if (cached >= 0)
                return cached == 1;

            bool usable;
            try
            {
                usable =
                    MatchesTypedCloneShape(typeof(MaterialSnapshot), 11) &&
                    MatchesTypedCloneShape(typeof(CassetteMaterial), 10) &&
                    MatchesTypedCloneShape(typeof(CassetteSlotMaterial), 4) &&
                    MatchesTypedCloneShape(typeof(WaferMaterial), 87) &&
                    MatchesTypedCloneShape(typeof(InputStageReviewSavedVerification), 4) &&
                    MatchesTypedCloneShape(typeof(InputStageReviewGeometryContext), 14) &&
                    MatchesTypedCloneShape(typeof(InputStageReviewGeometryTolerance), 5) &&
                    MatchesTypedCloneShape(typeof(InputStageReviewMeasurement), 12) &&
                    MatchesTypedCloneShape(typeof(OutputReceiveSlotMaterial), 22) &&
                    MatchesTypedCloneShape(typeof(DieMaterial), 30) &&
                    MatchesTypedCloneShape(typeof(DieInspectionRecord), 8) &&
                    MatchesTypedCloneShape(typeof(InspectionMeasurement), 7) &&
                    MatchesTypedCloneShape(typeof(InspectionAlignmentSnapshot), 11) &&
                    MatchesTypedCloneShape(typeof(MaterialLocation), 4) &&
                    MatchesTypedCloneShape(typeof(VisionOffset), 4);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "스냅샷 타입별 복제 가능 여부 확인에 실패해 기존 복제를 사용합니다. error=" +
                    ex.Message + " - Check");
                usable = false;
            }

            Volatile.Write(ref _typedCloneUsable, usable ? 1 : 0);
            return usable;
        }

        private static bool MatchesTypedCloneShape(Type type, int expectedPropertyCount)
        {
            int actual = GetSnapshotProperties(type).Length;
            if (actual == expectedPropertyCount)
                return true;

            Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                "스냅샷 모델이 변경되어 타입별 복제를 사용할 수 없습니다(기존 리플렉션 복제로 폴백). type=" +
                type.Name + ", expected=" + expectedPropertyCount + ", actual=" + actual +
                ". 복제 코드에 신규 프로퍼티를 반영한 뒤 기대값을 갱신하십시오. - Check");
            return false;
        }

        private static List<int> CloneIntList(List<int> source)
        {
            if (source == null)
                return null;

            var clone = new List<int>(source.Count);
            for (int i = 0; i < source.Count; i++)
                clone.Add(source[i]);
            return clone;
        }

        private static List<string> CloneStringList(List<string> source)
        {
            if (source == null)
                return null;

            var clone = new List<string>(source.Count);
            for (int i = 0; i < source.Count; i++)
                clone.Add(source[i]);
            return clone;
        }

        private static MaterialLocation CloneLocation(MaterialLocation source)
        {
            if (source == null)
                return null;

            return new MaterialLocation
            {
                Kind = source.Kind,
                CassetteRole = source.CassetteRole,
                SlotNumber = source.SlotNumber,
                PickerNo = source.PickerNo
            };
        }

        private static VisionOffset CloneVisionOffset(VisionOffset source)
        {
            if (source == null)
                return null;

            return new VisionOffset
            {
                X = source.X,
                Y = source.Y,
                R = source.R,
                IsValid = source.IsValid
            };
        }

        private static InspectionMeasurement CloneMeasurement(InspectionMeasurement source)
        {
            if (source == null)
                return null;

            return new InspectionMeasurement
            {
                Name = source.Name,
                Value = source.Value,
                Unit = source.Unit,
                LowerLimit = source.LowerLimit,
                UpperLimit = source.UpperLimit,
                RawValue = source.RawValue,
                Result = source.Result
            };
        }

        private static InspectionAlignmentSnapshot CloneAlignment(InspectionAlignmentSnapshot source)
        {
            if (source == null)
                return null;

            return new InspectionAlignmentSnapshot
            {
                Name = source.Name,
                X = source.X,
                Y = source.Y,
                T = source.T,
                Z = source.Z,
                XAxisName = source.XAxisName,
                YAxisName = source.YAxisName,
                TAxisName = source.TAxisName,
                ZAxisName = source.ZAxisName,
                Offset = CloneVisionOffset(source.Offset),
                IsValid = source.IsValid
            };
        }

        // includeInspectionDetail=false 는 기존 TrimInspectionDetailIfDisabled 와 동일한 결과를 만든다.
        // (원본이 null 이면 null 유지, 그 외에는 빈 목록) 다만 상세를 복제한 뒤 버리지 않고 처음부터 만들지 않는다.
        private static DieInspectionRecord CloneInspectionRecord(
            DieInspectionRecord source,
            bool includeInspectionDetail)
        {
            if (source == null)
                return null;

            var clone = new DieInspectionRecord
            {
                InspectionType = source.InspectionType,
                Result = source.Result,
                NgCodes = CloneStringList(source.NgCodes),
                Offset = CloneVisionOffset(source.Offset),
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

            if (source.Measurements == null)
            {
                clone.Measurements = null;
            }
            else if (!includeInspectionDetail)
            {
                clone.Measurements = new List<InspectionMeasurement>();
            }
            else
            {
                var measurements = new List<InspectionMeasurement>(source.Measurements.Count);
                for (int i = 0; i < source.Measurements.Count; i++)
                    measurements.Add(CloneMeasurement(source.Measurements[i]));
                clone.Measurements = measurements;
            }

            if (source.Alignments == null)
            {
                clone.Alignments = null;
            }
            else if (!includeInspectionDetail)
            {
                clone.Alignments = new List<InspectionAlignmentSnapshot>();
            }
            else
            {
                var alignments = new List<InspectionAlignmentSnapshot>(source.Alignments.Count);
                for (int i = 0; i < source.Alignments.Count; i++)
                    alignments.Add(CloneAlignment(source.Alignments[i]));
                clone.Alignments = alignments;
            }

            return clone;
        }

        private static DieMaterial CloneDie(DieMaterial source, bool includeInspectionDetail)
        {
            if (source == null)
                return null;

            var clone = new DieMaterial
            {
                DieId = source.DieId,
                WaferID_Input = source.WaferID_Input,
                InputWaferInstanceId = source.InputWaferInstanceId,
                InputResultFileSessionStartedAt = source.InputResultFileSessionStartedAt,
                WaferID_Output = source.WaferID_Output,
                OutputWaferInstanceId = source.OutputWaferInstanceId,
                OutputResultFileSessionStartedAt = source.OutputResultFileSessionStartedAt,
                Input_BinCode = source.Input_BinCode,
                IsInputTarget = source.IsInputTarget,
                Output_BinCode = source.Output_BinCode,
                Wafer_IndexX = source.Wafer_IndexX,
                Wafer_IndexY = source.Wafer_IndexY,
                Wafer_OriginalIndexX = source.Wafer_OriginalIndexX,
                Wafer_OriginalIndexY = source.Wafer_OriginalIndexY,
                InputSequenceNo = source.InputSequenceNo,
                Bin_IndexX = source.Bin_IndexX,
                Bin_IndexY = source.Bin_IndexY,
                CurrentLocation = CloneLocation(source.CurrentLocation),
                ReservedPickerLocation = source.ReservedPickerLocation,
                ReservedPickerNo = source.ReservedPickerNo,
                PickedPickerLocation = source.PickedPickerLocation,
                PickedPickerNo = source.PickedPickerNo,
                PickedAt = source.PickedAt,
                Result = source.Result,
                NgCodes = CloneStringList(source.NgCodes),
                WaferOffset = CloneVisionOffset(source.WaferOffset),
                BinOffset = CloneVisionOffset(source.BinOffset),
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

            if (source.Inspections == null)
            {
                clone.Inspections = null;
            }
            else
            {
                var inspections = new List<DieInspectionRecord>(source.Inspections.Count);
                for (int i = 0; i < source.Inspections.Count; i++)
                    inspections.Add(CloneInspectionRecord(source.Inspections[i], includeInspectionDetail));
                clone.Inspections = inspections;
            }

            return clone;
        }

        private static CassetteSlotMaterial CloneCassetteSlot(CassetteSlotMaterial source)
        {
            if (source == null)
                return null;

            return new CassetteSlotMaterial
            {
                SlotNumber = source.SlotNumber,
                WaferId = source.WaferId,
                WaferInstanceId = source.WaferInstanceId,
                HasWafer = source.HasWafer
            };
        }

        private static CassetteMaterial CloneCassette(CassetteMaterial source)
        {
            if (source == null)
                return null;

            var clone = new CassetteMaterial
            {
                CassetteId = source.CassetteId,
                CassetteLotId = source.CassetteLotId,
                Role = source.Role,
                Level = source.Level,
                SlotCount = source.SlotCount,
                IsEnabled = source.IsEnabled,
                IsPresent = source.IsPresent,
                IsMapped = source.IsMapped,
                LastScanTime = source.LastScanTime
            };

            if (source.Slots == null)
            {
                clone.Slots = null;
            }
            else
            {
                var slots = new List<CassetteSlotMaterial>(source.Slots.Count);
                for (int i = 0; i < source.Slots.Count; i++)
                    slots.Add(CloneCassetteSlot(source.Slots[i]));
                clone.Slots = slots;
            }

            return clone;
        }

        private static OutputReceiveSlotMaterial CloneOutputReceiveSlot(OutputReceiveSlotMaterial source)
        {
            if (source == null)
                return null;

            return new OutputReceiveSlotMaterial
            {
                OrderIndex = source.OrderIndex,
                SequenceNo = source.SequenceNo,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                OriginalMapX = source.OriginalMapX,
                OriginalMapY = source.OriginalMapY,
                IsTarget = source.IsTarget,
                Result = source.Result,
                BinCode = source.BinCode,
                PosX = source.PosX,
                PosY = source.PosY,
                DieUid = source.DieUid,
                SourceDieUid = source.SourceDieUid,
                PlacementUid = source.PlacementUid,
                LegacyDieUid = source.LegacyDieUid,
                IdentityRecoveryNote = source.IdentityRecoveryNote,
                IsOutputInspectionDone = source.IsOutputInspectionDone,
                IsOutputInspectionOk = source.IsOutputInspectionOk,
                OutputInspectionOffsetX = source.OutputInspectionOffsetX,
                OutputInspectionOffsetY = source.OutputInspectionOffsetY,
                OutputInspectionOffsetT = source.OutputInspectionOffsetT,
                OutputInspectionRaw = source.OutputInspectionRaw
            };
        }

        private static WaferMaterial CloneWafer(WaferMaterial source)
        {
            if (source == null)
                return null;

            var clone = new WaferMaterial
            {
                WaferId = source.WaferId,
                OriginalWaferId = source.OriginalWaferId,
                BarcodeId = source.BarcodeId,
                BarcodeConfirmed = source.BarcodeConfirmed,
                BarcodeSource = source.BarcodeSource,
                BarcodeUpdatedAt = source.BarcodeUpdatedAt,
                BarcodeAttemptCount = source.BarcodeAttemptCount,
                BarcodeSequencePerformed = source.BarcodeSequencePerformed,
                WaferInstanceId = source.WaferInstanceId,
                InputResultFileSessionStartedAt = source.InputResultFileSessionStartedAt,
                OutputResultFileSessionStartedAt = source.OutputResultFileSessionStartedAt,
                CassetteLotId = source.CassetteLotId,
                SourceCassetteId = source.SourceCassetteId,
                SourceCassetteRole = source.SourceCassetteRole,
                SourceSlotNumber = source.SourceSlotNumber,
                SourceCassetteSlotPosition = source.SourceCassetteSlotPosition,
                OutputCassetteId = source.OutputCassetteId,
                OutputCassetteRole = source.OutputCassetteRole,
                OutputSlotNumber = source.OutputSlotNumber,
                CurrentCassetteSlotPosition = source.CurrentCassetteSlotPosition,
                OutputGrade = source.OutputGrade,
                CurrentLocation = CloneLocation(source.CurrentLocation),
                State = source.State,
                TapeFrameSpecName = source.TapeFrameSpecName,
                DieMapFrameObjId = source.DieMapFrameObjId,
                HasInputStageAlignResult = source.HasInputStageAlignResult,
                InputStageAlignResultMode = source.InputStageAlignResultMode,
                InputStageAlignResultRunId = source.InputStageAlignResultRunId,
                InputStageAlignOriginX = source.InputStageAlignOriginX,
                InputStageAlignOriginY = source.InputStageAlignOriginY,
                InputStageAlignPitchX = source.InputStageAlignPitchX,
                InputStageAlignPitchY = source.InputStageAlignPitchY,
                InputStageDieSizeX = source.InputStageDieSizeX,
                InputStageDieSizeY = source.InputStageDieSizeY,
                InputStageOuterDiameterMm = source.InputStageOuterDiameterMm,
                InputStageAlignOffsetX = source.InputStageAlignOffsetX,
                InputStageAlignOffsetY = source.InputStageAlignOffsetY,
                HasInputStageThetaAlignResult = source.HasInputStageThetaAlignResult,
                InputStageAlignReferenceT = source.InputStageAlignReferenceT,
                InputStageAlignCorrectedT = source.InputStageAlignCorrectedT,
                InputStageAlignOffsetT = source.InputStageAlignOffsetT,
                InputStageAlignManualFallback = source.InputStageAlignManualFallback,
                InputStageAlignManualFallbackThetaDone = source.InputStageAlignManualFallbackThetaDone,
                HasInputStageDieMappingResult = source.HasInputStageDieMappingResult,
                InputStageDieMappingResultMode = source.InputStageDieMappingResultMode,
                InputStageDieMappingAlignRunId = source.InputStageDieMappingAlignRunId,
                InputStageDieMappingOffsetX = source.InputStageDieMappingOffsetX,
                InputStageDieMappingOffsetY = source.InputStageDieMappingOffsetY,
                HasInputStageDieMappingOrigin = source.HasInputStageDieMappingOrigin,
                InputStageDieMappingOriginX = source.InputStageDieMappingOriginX,
                InputStageDieMappingOriginY = source.InputStageDieMappingOriginY,
                HasInputStageDieMappingThetaSnapshot = source.HasInputStageDieMappingThetaSnapshot,
                InputStageDieMappingCorrectedT = source.InputStageDieMappingCorrectedT,
                InputStageDieMappingInvalidatedByAlignChange = source.InputStageDieMappingInvalidatedByAlignChange,
                InputMapApprovalHashAtMapping = source.InputMapApprovalHashAtMapping,
                HasInputStageRunReviewApproval = source.HasInputStageRunReviewApproval,
                InputStageRunReviewStartDieIndex = source.InputStageRunReviewStartDieIndex,
                InputStageRunReviewStartDieUid = source.InputStageRunReviewStartDieUid,
                InputStageRunReviewOrderedDieIds = CloneStringList(source.InputStageRunReviewOrderedDieIds),
                InputStageRunReviewMappingRevision = source.InputStageRunReviewMappingRevision,
                HasInputStageReviewBaseline = source.HasInputStageReviewBaseline,
                InputStageReviewBaselineMappingRevision = source.InputStageReviewBaselineMappingRevision,
                InputStageReviewBaselineOriginX = source.InputStageReviewBaselineOriginX,
                InputStageReviewBaselineOriginY = source.InputStageReviewBaselineOriginY,
                InputStageReviewVerification = source.InputStageReviewVerification != null ? source.InputStageReviewVerification.Clone() : null,
                InputStageProcessingGeneration = source.InputStageProcessingGeneration,
                OutputReceiveSourceWaferId = source.OutputReceiveSourceWaferId,
                OutputReceiveSourceWaferInstanceId = source.OutputReceiveSourceWaferInstanceId,
                OutputReceiveDieMapX = source.OutputReceiveDieMapX,
                OutputReceiveDieMapY = source.OutputReceiveDieMapY,
                OutputReceivePitchX = source.OutputReceivePitchX,
                OutputReceivePitchY = source.OutputReceivePitchY,
                OutputReceiveDieSizeX = source.OutputReceiveDieSizeX,
                OutputReceiveDieSizeY = source.OutputReceiveDieSizeY,
                OutputReceiveOuterDiameterMm = source.OutputReceiveOuterDiameterMm,
                OutputReceiveOriginX = source.OutputReceiveOriginX,
                OutputReceiveOriginY = source.OutputReceiveOriginY,
                OutputReceiveNextIndex = source.OutputReceiveNextIndex,
                OutputReceiveTotalCount = source.OutputReceiveTotalCount,
                OutputReceiveTargetCount = source.OutputReceiveTargetCount,
                OutputReceiveStartCorner = source.OutputReceiveStartCorner,
                OutputReceiveDirection = source.OutputReceiveDirection,
                OutputReceivePattern = source.OutputReceivePattern,
                DieIds = CloneStringList(source.DieIds),
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

            if (source.OutputReceiveSlots == null)
            {
                clone.OutputReceiveSlots = null;
            }
            else
            {
                var slots = new List<OutputReceiveSlotMaterial>(source.OutputReceiveSlots.Count);
                for (int i = 0; i < source.OutputReceiveSlots.Count; i++)
                    slots.Add(CloneOutputReceiveSlot(source.OutputReceiveSlots[i]));
                clone.OutputReceiveSlots = slots;
            }

            return clone;
        }

        private static MaterialSnapshot CloneSnapshotTyped(
            MaterialSnapshot source,
            bool includeInspectionDetail)
        {
            if (source == null)
                return null;

            var clone = new MaterialSnapshot
            {
                Version = source.Version,
                SnapshotRevision = source.SnapshotRevision,
                SavedAt = source.SavedAt,
                SaveReason = source.SaveReason,
                RecipeName = source.RecipeName,
                LotId = source.LotId,
                PickupBinMode = source.PickupBinMode,
                PickupBinNumbers = CloneIntList(source.PickupBinNumbers)
            };

            if (source.Cassettes == null)
            {
                clone.Cassettes = null;
            }
            else
            {
                var cassettes = new List<CassetteMaterial>(source.Cassettes.Count);
                for (int i = 0; i < source.Cassettes.Count; i++)
                    cassettes.Add(CloneCassette(source.Cassettes[i]));
                clone.Cassettes = cassettes;
            }

            if (source.Wafers == null)
            {
                clone.Wafers = null;
            }
            else
            {
                var wafers = new List<WaferMaterial>(source.Wafers.Count);
                for (int i = 0; i < source.Wafers.Count; i++)
                    wafers.Add(CloneWafer(source.Wafers[i]));
                clone.Wafers = wafers;
            }

            if (source.Dies == null)
            {
                clone.Dies = null;
            }
            else
            {
                var dies = new List<DieMaterial>(source.Dies.Count);
                for (int i = 0; i < source.Dies.Count; i++)
                    dies.Add(CloneDie(source.Dies[i], includeInspectionDetail));
                clone.Dies = dies;
            }

            return clone;
        }

        #endregion

        private static MaterialSnapshot CloneSnapshotForSave(MaterialSnapshot source)
        {
            try
            {
                MaterialSnapshot clone = CloneObject(source) as MaterialSnapshot;
                if (clone == null)
                {
                    Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                        "Material snapshot 저장용 복사 결과가 null입니다. - Failed");
                }
                return clone;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot 저장용 복사에 실패했습니다. error=" + ex.Message + " - Failed");
                return null;
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

        // [방어 강화 2026-08-05] 기존 CloneObject는 (a) 배열에서 Activator.CreateInstance 예외,
        // (b) Dictionary를 "예외 없이 빈 사전"으로 복제(무성 데이터 소실), (c) 순환 참조 시
        // StackOverflow, (d) 같은 인스턴스가 그래프에 2회 나타나면 물리 복제 — 네 가지 함정이 있었다.
        // visited 맵(참조 동일성)으로 순환/공유 참조를 보존하고, 배열/사전을 정확히 복제한다.
        // ReferenceEqualityComparer는 DateTime 정규화(NormalizeSnapshotObjectGraphDateTimes)와 공용이다.
        private static object CloneObject(object value)
        {
            return CloneObject(value, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));
        }

        private static object CloneObject(object value, Dictionary<object, object> visited)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
                return value;

            object existingClone;
            if (visited.TryGetValue(value, out existingClone))
                return existingClone;

            Array sourceArray = value as Array;
            if (sourceArray != null)
            {
                if (sourceArray.Rank != 1)
                    throw new NotSupportedException(
                        "Material snapshot 그래프에 다차원 배열은 지원하지 않습니다. type=" + type.FullName);

                Array clonedArray = Array.CreateInstance(type.GetElementType(), sourceArray.Length);
                visited[value] = clonedArray;
                for (int i = 0; i < sourceArray.Length; i++)
                    clonedArray.SetValue(CloneObject(sourceArray.GetValue(i), visited), i);
                return clonedArray;
            }

            IDictionary sourceDictionary = value as IDictionary;
            if (sourceDictionary != null)
            {
                IDictionary clonedDictionary = (IDictionary)Activator.CreateInstance(type);
                visited[value] = clonedDictionary;
                foreach (DictionaryEntry entry in sourceDictionary)
                    clonedDictionary[CloneObject(entry.Key, visited)] = CloneObject(entry.Value, visited);
                return clonedDictionary;
            }

            IList sourceList = value as IList;
            if (sourceList != null)
            {
                IList clonedList = (IList)Activator.CreateInstance(type);
                visited[value] = clonedList;
                foreach (object item in sourceList)
                    clonedList.Add(CloneObject(item, visited));
                return clonedList;
            }

            object clone = Activator.CreateInstance(type);
            visited[value] = clone;
            foreach (PropertyInfo property in GetSnapshotProperties(type))
            {
                object propertyValue = property.GetValue(value, null);
                property.SetValue(clone, CloneObject(propertyValue, visited), null);
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

            if (expected.SnapshotRevision != loaded.SnapshotRevision)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot validation failed. revision mismatch. file=" + path +
                    ", expectedRevision=" + expected.SnapshotRevision +
                    ", loadedRevision=" + loaded.SnapshotRevision + " - Failed");
                return false;
            }

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

        private static void ObserveSnapshotRevision(MaterialSnapshot snapshot)
        {
            long revision = snapshot != null ? snapshot.SnapshotRevision : 0L;
            if (revision <= 0L || !MaterialSnapshotRevisionPolicy.IsTrustedLoadedRevision(revision))
                return;

            while (true)
            {
                long current = Interlocked.Read(ref _highestObservedSnapshotRevision);
                if (revision <= current)
                    return;

                if (Interlocked.CompareExchange(
                        ref _highestObservedSnapshotRevision,
                        revision,
                        current) == current)
                {
                    return;
                }
            }
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
                CleanupFailedSnapshotDiagnostics();
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
                CleanupFailedSnapshotDiagnostics();
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

        private static void CleanupFailedSnapshotDiagnostics()
        {
            try
            {
                if (!Directory.Exists(Dir))
                    return;

                FileInfo[] failedFiles = Directory
                    .GetFiles(Dir, "material_state.failed.*.json")
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                foreach (FileInfo stale in failedFiles.Skip(FailedSnapshotDiagnosticRetentionCount))
                {
                    try { stale.Delete(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSnapshotSave",
                    "Material snapshot failed diagnostic cleanup failed: " + ex.Message + " - Check");
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
