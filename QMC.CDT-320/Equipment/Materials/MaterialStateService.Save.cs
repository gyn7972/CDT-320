using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Logging;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: 저장 파이프라인(디바운스 워커/Revision/Flush)과 StateChanged 이벤트 (원본 11243-11778)
    public static partial class MaterialStateService
    {
        public static void NotifyAndSave(string reason)
        {
            TryNotifyAndSave(reason);
        }

        public static bool TryNotifyAndSave(string reason)
        {
            try
            {
                RequestStateChanged();
                RequestBackgroundSave(reason);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save request failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public static bool TryFlushPendingSave(string reason)
        {
            try
            {
                bool shouldSave;
                long requestVersion;
                lock (_saveRequestSync)
                {
                    long lastIssuedRevision = Interlocked.Read(ref _lastIssuedSnapshotRevision);
                    long lastCommittedRevision = Interlocked.Read(ref _lastCommittedSnapshotRevision);
                    bool latestRevisionIsDurable =
                        lastIssuedRevision > 0L && lastCommittedRevision >= lastIssuedRevision;
                    shouldSave = _saveRequested ||
                                 _saveWorkerRunning ||
                                 !_lastSaveSucceeded ||
                                 !latestRevisionIsDurable ||
                                 _lastSaveCompletedUtc == DateTime.MinValue;
                    _pendingSaveReason = reason ?? "";
                    requestVersion = _saveRequestVersion;
                }

                RequestStateChanged();
                if (!shouldSave)
                {
                    lock (_saveRequestSync)
                    {
                        if (_saveRequestVersion == requestVersion)
                        {
                            _saveRequested = false;
                            _pendingSaveReason = "";
                        }
                    }
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "Material state flush skipped because latest snapshot is already saved. reason=" +
                        (reason ?? "") + " - Ok");
                    return true;
                }

                bool saved = SaveCurrentSnapshot(reason);
                lock (_saveRequestSync)
                {
                    if (saved && _saveRequestVersion == requestVersion)
                    {
                        _saveRequested = false;
                        _pendingSaveReason = "";
                    }
                }
                if (!saved)
                    RequestBackgroundSave(reason);
                return saved;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state flush failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void RequestBackgroundSave(string reason)
        {
            try
            {
                lock (_saveRequestSync)
                {
                    _pendingSaveReason = reason ?? "";
                    _saveRequested = true;
                    _saveRequestVersion = _saveRequestVersion == long.MaxValue
                        ? 1L
                        : _saveRequestVersion + 1L;

                    if (_saveWorkerRunning)
                        return;

                    _saveWorkerRunning = true;
                    Task.Run(() => MaterialSaveWorkerAsync());
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material save worker start failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static async Task MaterialSaveWorkerAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(MaterialSaveQuietMs).ConfigureAwait(false);

                    string reason;
                    int waitMs;
                    long requestVersion;
                    lock (_saveRequestSync)
                    {
                        if (!_saveRequested)
                        {
                            _saveWorkerRunning = false;
                            return;
                        }

                        reason = _pendingSaveReason;
                        requestVersion = _saveRequestVersion;
                        waitMs = ResolveBackgroundSaveWaitMs(reason);
                    }

                    if (waitMs > 0)
                    {
                        await Task.Delay(waitMs).ConfigureAwait(false);
                        continue;
                    }

                    bool saved = SaveCurrentSnapshot(reason);
                    lock (_saveRequestSync)
                    {
                        if (saved && _saveRequestVersion == requestVersion)
                        {
                            _saveRequested = false;
                            _pendingSaveReason = "";
                        }
                        else if (!saved)
                        {
                            // 일시적인 파일 잠금/IO 실패에서도 dirty 요청을 잃지 않는다.
                            _saveRequested = true;
                            if (string.IsNullOrWhiteSpace(_pendingSaveReason))
                                _pendingSaveReason = reason ?? "";
                        }
                    }
                    if (!saved)
                        await Task.Delay(MaterialSaveFailureRetryMs).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material save worker failed: " + ex.Message + " - Failed");
                lock (_saveRequestSync)
                {
                    _saveWorkerRunning = false;
                }
            }
            finally
            {
            }
        }

        private static int ResolveBackgroundSaveWaitMs(string reason)
        {
            try
            {
                if (IsImmediateBackgroundSaveReason(reason))
                    return 0;

                if (_lastSaveCompletedUtc == DateTime.MinValue)
                    return 0;

                double elapsedMs = (DateTime.UtcNow - _lastSaveCompletedUtc).TotalMilliseconds;
                if (elapsedMs >= MaterialSaveMinimumIntervalMs)
                    return 0;

                int waitMs = MaterialSaveMinimumIntervalMs - (int)elapsedMs;
                return waitMs < 0 ? 0 : waitMs;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        private static bool IsImmediateBackgroundSaveReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            if (reason.IndexOf("Initialize", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Manual", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Mapping", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("MapTransfer", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (reason.IndexOf("Dialog", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static long IssueNextSnapshotRevisionNoLock()
        {
            long stateRevision = State != null ? State.SnapshotRevision : 0L;
            long storeRevision = MaterialSnapshotStore.HighestObservedSnapshotRevision;
            _lastIssuedSnapshotRevision = MaterialSnapshotRevisionPolicy.IssueNext(
                stateRevision,
                storeRevision,
                _lastIssuedSnapshotRevision);
            return _lastIssuedSnapshotRevision;
        }

        private static void EnsureSnapshotMaterialIdentityNoLock()
        {
            if (State == null)
                return;

            if (State.Wafers != null)
            {
                foreach (WaferMaterial wafer in State.Wafers)
                {
                    if (wafer != null)
                        EnsureWaferInstanceIdNoLock(wafer);
                }
            }

            if (State.Cassettes == null)
                return;

            foreach (CassetteMaterial cassette in State.Cassettes)
            {
                if (cassette == null)
                    continue;

                cassette.EnsureSlots();
                foreach (CassetteSlotMaterial slot in cassette.Slots)
                {
                    if (slot == null ||
                        !string.IsNullOrWhiteSpace(slot.WaferInstanceId) ||
                        string.IsNullOrWhiteSpace(slot.WaferId))
                    {
                        continue;
                    }

                    if (slot.HasWafer)
                    {
                        string resolveReason;
                        ResolveCassetteSlotWaferNoLock(slot, out resolveReason);
                        continue;
                    }

                    List<WaferMaterial> legacyCandidates = State.Wafers
                        .Where(w =>
                            w != null &&
                            string.Equals(
                                w.WaferId ?? "",
                                slot.WaferId ?? "",
                                StringComparison.OrdinalIgnoreCase))
                        .Take(2)
                        .ToList();
                    if (legacyCandidates.Count == 1)
                        slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(legacyCandidates[0]);
                }
            }
        }

        private static MaterialCompactionResult CompactMaterialStateNoLock()
        {
            EnsureSnapshotMaterialIdentityNoLock();
            IEnumerable<string> activeInputMapRoots = null;
            WaferMaterial activeInputWafer = State != null && State.Wafers != null
                ? State.Wafers.FirstOrDefault(wafer =>
                    wafer != null &&
                    wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage)
                : null;
            DieMap activeInputMap = LotStorage.ActiveInputDieMap;
            if (activeInputWafer != null && activeInputMap != null && activeInputMap.Entries != null)
            {
                // ActiveInputDieMap은 Input Stage wafer가 실제로 존재하는 동안만 외부 root다.
                // Stage clear 뒤 남은 파생 캐시가 과거 Die를 영구 보존하지 않도록 물리 위치로 범위를 제한한다.
                activeInputMapRoots = activeInputMap.Entries
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.DieUid))
                    .Select(entry => entry.DieUid.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            MaterialCompactionResult compactionOutcome = MaterialStateCompactor.Compact(State, activeInputMapRoots);
            // 컴팩터가 State.Dies를 직접 제거하므로 파생 인덱스를 함께 무효화한다.
            if (compactionOutcome != null && compactionOutcome.Changed)
                InvalidateDieByIdIndexNoLock();
            return compactionOutcome;
        }

        private static void LogMaterialCompaction(string reason, MaterialCompactionResult result)
        {
            if (result == null || (!result.Changed && result.WarningCount == 0))
                return;

            Log.Write(
                "Main",
                "SYSTEM",
                "MaterialStateCompact",
                "Material 현재 작업 집합을 정리했습니다. reason=" + (reason ?? "") +
                ", wafers=" + result.BeforeWaferCount + "->" + result.AfterWaferCount +
                ", dies=" + result.BeforeDieCount + "->" + result.AfterDieCount +
                ", warnings=" + result.WarningCount +
                (result.WarningCount > 0 ? ", firstWarning=" + result.FirstWarning + " - Check" : " - Ok"));
        }

        private static bool SaveCurrentSnapshot(string reason)
        {
            bool saved = false;
            bool supersededByNewerCommit = false;
            bool latestRevisionIsDurable = false;
            try
            {
                // [정정 2026-07-27] 저장용 사본을 _stateSync 락 안에서 만든다.
                // 기존에는 Store.Save가 라이브 State를 락 없이 순회해, 저장 중 시퀀스가 Die를 추가하면
                // "컬렉션이 수정되었습니다" 예외로 저장이 실패할 수 있었다.
                // 무거운 직렬화/디스크 쓰기는 계속 락 밖에서 수행한다.
                MaterialSnapshot saveCopy;
                // 저장 캡처(딥클론) 동안의 전역 락 "보유" 시간 — 기준선 계측 (락 대기 시간은 제외).
                // 예외 경로의 최악 샘플도 통계에 남도록 try/finally로 감싼다.
                long captureProbeToken = 0;
                try
                {
                    lock (_stateSync)
                    {
                        captureProbeToken = MaterialPerfProbe.BeginSample();
                        State.SaveReason = reason ?? "";
                        State.SavedAt = DateTime.Now;
                        EnsureSnapshotMaterialIdentityNoLock();
                        State.SnapshotRevision = IssueNextSnapshotRevisionNoLock();
                        lock (_saveRequestSync)
                        {
                            // 새 Revision을 발급한 순간부터 해당 Revision의 커밋이 확인될 때까지
                            // "최신 상태 저장 완료"로 판단하면 안 된다.
                            _lastSaveSucceeded = false;
                        }
                        NormalizeSnapshotHeader(State);
                        VerifyDieByIdIndexConsistencyNoLock();
                        saveCopy = MaterialSnapshotStore.CreateSaveCopy(State);
                    }
                }
                finally
                {
                    if (captureProbeToken != 0)
                        MaterialPerfProbe.EndSample("SaveCaptureLock", captureProbeToken);
                }
                if (saveCopy != null)
                {
                    MaterialPerfProbe.SetGauge("StateDies", saveCopy.Dies != null ? saveCopy.Dies.Count : 0);
                    MaterialPerfProbe.SetGauge("StateWafers", saveCopy.Wafers != null ? saveCopy.Wafers.Count : 0);
                }

                if (saveCopy == null)
                    throw new InvalidOperationException("Material snapshot 저장용 복사본을 만들지 못했습니다.");

                lock (_saveIoSync)
                {
                    if (MaterialSnapshotRevisionPolicy.IsSuperseded(
                        saveCopy.SnapshotRevision,
                        _lastCommittedSnapshotRevision))
                    {
                        supersededByNewerCommit = true;
                        saved = true;
                    }
                    else
                    {
                        saved = MaterialSnapshotStore.Save(saveCopy, true);
                        if (saved)
                            _lastCommittedSnapshotRevision = saveCopy.SnapshotRevision;
                    }
                }

                if (supersededByNewerCommit)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "더 최신 Material snapshot이 이미 저장되어 이전 저장 요청을 건너뜁니다. revision=" +
                        saveCopy.SnapshotRevision +
                        ", committedRevision=" + _lastCommittedSnapshotRevision + " - Ok");
                }

                long latestIssuedRevision;
                long committedRevision;
                lock (_stateSync)
                {
                    latestIssuedRevision = _lastIssuedSnapshotRevision;
                    committedRevision = Interlocked.Read(ref _lastCommittedSnapshotRevision);
                    latestRevisionIsDurable =
                        latestIssuedRevision > 0L && committedRevision >= latestIssuedRevision;

                    lock (_saveRequestSync)
                    {
                        if (latestRevisionIsDurable)
                            _lastSaveCompletedUtc = DateTime.UtcNow;
                        _lastSaveSucceeded = latestRevisionIsDurable;
                    }
                }

                // [가시성 2026-08-08] 저장 성공/실패 상태 전환을 항상 남는 경고 채널로 알린다.
                ReportMaterialSaveOutcome(latestRevisionIsDurable, saveCopy);

                if (!latestRevisionIsDurable)
                {
                    int waferCount = saveCopy.Wafers != null ? saveCopy.Wafers.Count : 0;
                    int dieCount = saveCopy.Dies != null ? saveCopy.Dies.Count : 0;
                    int cassetteCount = saveCopy.Cassettes != null ? saveCopy.Cassettes.Count : 0;
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "최신 Material snapshot이 아직 디스크에 반영되지 않았습니다. reason=" +
                        (saveCopy.SaveReason ?? "") +
                        ", savedRevision=" + saveCopy.SnapshotRevision +
                        ", latestIssuedRevision=" + latestIssuedRevision +
                        ", committedRevision=" + committedRevision +
                        ", cassettes=" + cassetteCount +
                        ", wafers=" + waferCount +
                        ", dies=" + dieCount +
                        ", file=" + MaterialSnapshotStore.SnapshotPath + " - Failed");
                }

                return latestRevisionIsDurable;
            }
            catch (Exception ex)
            {
                lock (_saveRequestSync)
                {
                    _lastSaveSucceeded = false;
                }
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material state save failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        // [가시성 2026-08-08] Material 저장 실패를 즉시 알 수 있게 한다.
        //
        // 배경: 저장 실패 로그는 Log.Write(class,user,source,msg) 4-인자 형식이라 운영 최소 로그 정책에서
        //   전부 버려졌다. 2026-08-08 운전에서 카세트 데이터 Clear 이후 저장이 5시간 동안 계속 실패했으나
        //   로그가 한 줄도 남지 않아, 프로그램 종료 시 "Material 상태 저장에 실패했습니다" 대화상자로만
        //   문제를 알 수 있었다. 그 사이 진행한 작업 상태는 디스크에 반영되지 않았다.
        //
        // 정책:
        //  - EventKind.Warning 은 최소 로그 정책과 무관하게 Warning 로그에 남는다.
        //  - AlarmManager.Raise 는 사용하지 않는다. AlarmManager.HasActive 는 심각도와 무관하게
        //    시퀀스의 IsAlarmStopActive 판정에 쓰이므로, 저장 실패로 운전을 멈추는 동작 변경이 생긴다.
        //  - 실패가 이어질 때 로그가 폭주하지 않도록 최초 1회와 이후 주기적으로만 남긴다.
        private const int MaterialSaveFailureNotifyIntervalMs = 60000;
        private static readonly object _saveFailureNotifySync = new object();
        private static int _consecutiveSaveFailureCount;
        private static DateTime _firstSaveFailureAt = DateTime.MinValue;
        private static DateTime _lastSaveFailureNotifiedAt = DateTime.MinValue;

        private static void ReportMaterialSaveOutcome(bool durable, MaterialSnapshot saveCopy)
        {
            try
            {
                int failureCount;
                bool notify;
                bool recovered = false;
                TimeSpan failingFor = TimeSpan.Zero;

                lock (_saveFailureNotifySync)
                {
                    if (durable)
                    {
                        recovered = _consecutiveSaveFailureCount > 0;
                        failureCount = _consecutiveSaveFailureCount;
                        failingFor = recovered && _firstSaveFailureAt != DateTime.MinValue
                            ? DateTime.Now - _firstSaveFailureAt
                            : TimeSpan.Zero;
                        _consecutiveSaveFailureCount = 0;
                        _firstSaveFailureAt = DateTime.MinValue;
                        _lastSaveFailureNotifiedAt = DateTime.MinValue;
                        notify = recovered;
                    }
                    else
                    {
                        if (_consecutiveSaveFailureCount == 0)
                            _firstSaveFailureAt = DateTime.Now;
                        _consecutiveSaveFailureCount++;
                        failureCount = _consecutiveSaveFailureCount;
                        failingFor = DateTime.Now - _firstSaveFailureAt;

                        // 최초 실패는 즉시, 이후에는 주기적으로만 알린다.
                        notify = _lastSaveFailureNotifiedAt == DateTime.MinValue ||
                                 (DateTime.Now - _lastSaveFailureNotifiedAt).TotalMilliseconds >=
                                     MaterialSaveFailureNotifyIntervalMs;
                        if (notify)
                            _lastSaveFailureNotifiedAt = DateTime.Now;
                    }
                }

                if (!notify)
                    return;

                if (recovered)
                {
                    EventLogger.Write(
                        EventKind.Warning,
                        "SYSTEM",
                        "MATERIAL-SAVE-RECOVERED",
                        "Material 상태 저장이 정상 복구되었습니다. 실패 " + failureCount + "회, 지속 " +
                        ((int)failingFor.TotalSeconds) + "초. file=" + MaterialSnapshotStore.SnapshotPath);
                    return;
                }

                string failureReason = MaterialSnapshotStore.LastSaveFailureReason;
                if (string.IsNullOrWhiteSpace(failureReason))
                {
                    // Store 단계가 아니라 revision 내구성 판정에서 실패한 경우다.
                    failureReason = "최신 Material snapshot이 디스크에 반영되지 않았습니다(리비전 미커밋).";
                }

                EventLogger.Write(
                    EventKind.Warning,
                    "SYSTEM",
                    "MATERIAL-SAVE-FAIL",
                    "Material 상태 저장에 실패했습니다. 저장되지 않은 작업 정보가 손실될 수 있습니다. " +
                    "연속 실패=" + failureCount + "회, 지속=" + ((int)failingFor.TotalSeconds) + "초" +
                    ", wafers=" + (saveCopy != null && saveCopy.Wafers != null ? saveCopy.Wafers.Count : 0) +
                    ", dies=" + (saveCopy != null && saveCopy.Dies != null ? saveCopy.Dies.Count : 0) +
                    ", file=" + MaterialSnapshotStore.SnapshotPath +
                    ", reason=" + failureReason);
            }
            catch
            {
                // 알림 실패가 저장 경로에 영향을 주지 않게 한다.
            }
        }

        private static void RequestStateChanged()
        {
            try
            {
                int delayMs;
                lock (_stateChangedSync)
                {
                    if (_stateChangedQueued)
                        return;

                    TimeSpan elapsed = DateTime.Now - _lastStateChangedAt;
                    delayMs = elapsed.TotalMilliseconds >= MaterialStateChangedQuietMs
                        ? 0
                        : MaterialStateChangedQuietMs - (int)elapsed.TotalMilliseconds;
                    _stateChangedQueued = true;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        if (delayMs > 0)
                            await Task.Delay(delayMs).ConfigureAwait(false);

                        // [계약 보강 2026-08-07] 라이브 State를 넘기지 않는 시그널 전용 알림.
                        // 구독자별로 예외를 격리해 앞 구독자의 실패가 뒤 구독자의 알림을 막지 않게 한다.
                        Action handler = StateChanged;
                        if (handler != null)
                        {
                            foreach (Delegate subscriber in handler.GetInvocationList())
                            {
                                try
                                {
                                    ((Action)subscriber)();
                                }
                                catch (Exception subscriberEx)
                                {
                                    Log.Write("Main", "SYSTEM", "MaterialStateChanged",
                                        "Material state changed subscriber failed: " + subscriberEx.Message + " - Failed");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Main", "SYSTEM", "MaterialStateChanged",
                            "Material state changed event failed: " + ex.Message + " - Failed");
                    }
                    finally
                    {
                        lock (_stateChangedSync)
                        {
                            _lastStateChangedAt = DateTime.Now;
                            _stateChangedQueued = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateChanged",
                    "Material state changed event queue failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static void NormalizeSnapshotHeader(MaterialSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return;

                if (string.IsNullOrWhiteSpace(snapshot.RecipeName))
                {
                    var project = RecipeStore.LoadLastOrDefault();
                    if (project != null)
                        snapshot.RecipeName = project.FileName ?? "";
                }

                snapshot.LotId = string.IsNullOrWhiteSpace(snapshot.LotId)
                    ? ""
                    : snapshot.LotId.Trim();
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateSave", "Material snapshot header normalize failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

    }
}
