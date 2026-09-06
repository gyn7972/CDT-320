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
                // [내구성 워터마크 2026-08-18] 자료 변경 세대를 여기서 한 번만 올린다.
                // 이 함수는 "변경이 끝난 뒤" 호출되는 공식 통지 경로이므로, 세대 증가가 항상 변경 이후다.
                // (변경보다 먼저 올리면 변경 전 스냅샷이 새 세대를 주장해 유실로 이어진다 — 순서가 중요.)
                // _stateSync 안에서 올려야 저장 캡처가 (자료, 세대)를 짝이 맞게 집는다.
                lock (_stateSync)
                {
                    _stateVersion++;
                    // UI StateChanged는 지연되므로 복구 승인 만료는 Material 변경 통지 안에서 즉시 정리한다.
                    PrunePickerFlowRecoveriesNoLock("Material 변경: " + reason);
                }

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
                // [내구성 워터마크 2026-08-18] 이 호출이 보장해야 하는 것은
                // "호출 시점의 자료가 디스크에 있다"이다. 그 기준 세대를 먼저 고정한다.
                long targetVersion;
                lock (_stateSync)
                {
                    targetVersion = _stateVersion;
                }

                long requestVersion;
                lock (_saveRequestSync)
                {
                    _pendingSaveReason = reason ?? "";
                    requestVersion = _saveRequestVersion;
                }

                RequestStateChanged();

                // 이미 이 세대(또는 그 이후)가 디스크에 확정돼 있으면 쓸 이유가 없다.
                // 다른 스레드가 더 새 스냅샷을 저장했어도 그 안에 내 자료가 들어 있으므로 성공이다.
                if (IsStateVersionDurable(targetVersion))
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
                        (reason ?? "") +
                        ", targetVersion=" + targetVersion +
                        ", durableVersion=" + Interlocked.Read(ref _durableStateVersion) + " - Ok");
                    return true;
                }

                SaveCurrentSnapshot(reason);

                // 내 쓰기가 밀렸더라도(동시 저장이 더 새 세대를 먼저 확정) 목표 세대가 디스크에 있으면 성공이다.
                // 경합을 기다리거나 재시도하지 않는다 — 판정 기준 자체가 경합에 영향받지 않는다.
                bool durable = IsStateVersionDurable(targetVersion);
                lock (_saveRequestSync)
                {
                    if (durable && _saveRequestVersion == requestVersion)
                    {
                        _saveRequested = false;
                        _pendingSaveReason = "";
                    }
                }
                if (!durable)
                    RequestBackgroundSave(reason);
                return durable;
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

        /// <summary>
        /// 지정 자료 세대가 디스크에 확정됐는지 판정한다(단조 워터마크 비교).
        /// 동시 저장이 더 새 세대를 확정했다면 그 스냅샷이 이 세대의 자료를 포함하므로 참이다.
        /// </summary>
        private static bool IsStateVersionDurable(long targetVersion)
        {
            return Interlocked.Read(ref _durableStateVersion) >= targetVersion;
        }

        /// <summary>디스크 확정 세대를 단조 증가로 게시한다(_saveIoSync 안에서 호출).</summary>
        private static void PublishDurableStateVersionNoLock(long capturedVersion)
        {
            if (capturedVersion > _durableStateVersion)
                _durableStateVersion = capturedVersion;
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
                // [내구성 워터마크 2026-08-18] 딥카피와 같은 락 안에서 자료 세대를 함께 집는다.
                // 이렇게 해야 "이 스냅샷이 담은 자료 세대"가 정확히 확정된다(사후에 읽으면 어긋난다).
                long capturedStateVersion;
                // 저장 캡처(딥클론) 동안의 전역 락 "보유" 시간 — 기준선 계측 (락 대기 시간은 제외).
                // 예외 경로의 최악 샘플도 통계에 남도록 try/finally로 감싼다.
                long captureProbeToken = 0;
                try
                {
                    lock (_stateSync)
                    {
                        captureProbeToken = MaterialPerfProbe.BeginSample();
                        capturedStateVersion = _stateVersion;
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
                        {
                            _lastCommittedSnapshotRevision = saveCopy.SnapshotRevision;
                            // 실제로 디스크에 쓴 경우에만 워터마크를 올린다.
                            // superseded(더 새 스냅샷이 이미 커밋됨) 경로는 그 저장 주체가 자기 세대를
                            // 게시하므로 여기서 올리지 않는다 — 쓰지도 않은 세대를 확정으로 주장하지 않는다.
                            PublishDurableStateVersionNoLock(capturedStateVersion);
                        }
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

                    // [내구성 워터마크 2026-08-18] 완료 판정을 "내가 담은 자료 세대가 디스크에 있는가"로 한다.
                    // 기존 판정(committedRevision >= latestIssuedRevision)은 옆 스레드가 저장을 시작만 해도
                    // 거짓이 되어, 내 쓰기가 성공했는데도 실패로 뒤집혔다(2026-08-17 오탐).
                    // 세대 기준은 동시 저장에 영향받지 않는다 — 더 새 스냅샷은 내 자료를 포함하므로 성공이다.
                    latestRevisionIsDurable = IsStateVersionDurable(capturedStateVersion);

                    lock (_saveRequestSync)
                    {
                        if (latestRevisionIsDurable)
                            _lastSaveCompletedUtc = DateTime.UtcNow;
                        _lastSaveSucceeded = latestRevisionIsDurable;
                    }
                }

                // [가시성 2026-08-08] 저장 성공/실패 상태 전환을 항상 남는 경고 채널로 알린다.
                ReportMaterialSaveOutcome(latestRevisionIsDurable, saveCopy);

                // [자가 복구 2026-08-09] 저장이 그래프 정합성 검증에서 거부되면 컴팩션이 고칠 수 있는
                // 종류인지 한 번 시도한다. 컴팩션은 Clear/재매핑/이동/로드 경로에만 있어서, 그 밖의
                // 경로로 정합성이 깨지면 재기동 전까지 저장이 영구 거부되었다(2026-08-08 5시간, 08-09 5.7시간).
                if (!latestRevisionIsDurable)
                    TryRecoverSaveFailureByCompaction();

                if (!latestRevisionIsDurable)
                {
                    int waferCount = saveCopy.Wafers != null ? saveCopy.Wafers.Count : 0;
                    int dieCount = saveCopy.Dies != null ? saveCopy.Dies.Count : 0;
                    int cassetteCount = saveCopy.Cassettes != null ? saveCopy.Cassettes.Count : 0;
                    Log.Write("Main", "SYSTEM", "MaterialStateSave",
                        "최신 Material snapshot이 아직 디스크에 반영되지 않았습니다. reason=" +
                        (saveCopy.SaveReason ?? "") +
                        ", capturedStateVersion=" + capturedStateVersion +
                        ", durableStateVersion=" + Interlocked.Read(ref _durableStateVersion) +
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

        // [자가 복구 2026-08-09] 저장 실패가 이어질 때 컴팩션으로 치유 가능한지 주기적으로 한 번씩 시도한다.
        // 컴팩션은 그래프 전체 순회라 비용이 있으므로, 실패 상황에서만 그리고 최소 간격을 두고 실행한다.
        // 정상 저장 경로에는 추가 비용이 없다.
        private const int MaterialSaveRecoveryIntervalMs = 60000;
        private static DateTime _lastSaveRecoveryAttemptAt = DateTime.MinValue;

        private static void TryRecoverSaveFailureByCompaction()
        {
            try
            {
                lock (_saveFailureNotifySync)
                {
                    if (_lastSaveRecoveryAttemptAt != DateTime.MinValue &&
                        (DateTime.Now - _lastSaveRecoveryAttemptAt).TotalMilliseconds < MaterialSaveRecoveryIntervalMs)
                    {
                        return;
                    }

                    _lastSaveRecoveryAttemptAt = DateTime.Now;
                }

                MaterialCompactionResult outcome;
                lock (_stateSync)
                {
                    outcome = CompactMaterialStateNoLock();
                }

                if (outcome == null)
                    return;

                if (outcome.Changed || outcome.WarningCount > 0)
                {
                    EventLogger.Write(
                        EventKind.Warning,
                        "SYSTEM",
                        "MATERIAL-SAVE-RECOVERY-TRY",
                        "저장 실패가 계속되어 Material 그래프 정리를 시도했습니다. " +
                        "wafers=" + outcome.BeforeWaferCount + "->" + outcome.AfterWaferCount +
                        ", dies=" + outcome.BeforeDieCount + "->" + outcome.AfterDieCount +
                        ", warnings=" + outcome.WarningCount +
                        (outcome.WarningCount > 0 ? ", firstWarning=" + outcome.FirstWarning : ""));
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.AboveNormal, "Main", "MaterialStateSave",
                    "저장 실패 자가 복구 시도 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
