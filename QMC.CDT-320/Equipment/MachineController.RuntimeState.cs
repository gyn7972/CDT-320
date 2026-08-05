using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        public void ApplyStartupMachineRuntimeState(AppSettings settings)
        {
            try
            {
                settings = settings ?? AppSettingsStore.Current ?? AppSettingsStore.Load();
                _isDeveloperReadyRestored = false;
                var state = MachineRuntimeStateStore.Load();
                if (state != null)
                {
                    RestorePickerOffsetRuntimeState(state);
                    RestorePickerWorkCounterRuntimeState(state);
                }

                if (!settings.DeveloperMode)
                {
                    RestoreCylinderRuntimeState(state, settings);
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "StartupNormalMode", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Machine initialized state is false on normal startup. - Ok");
                    return;
                }

                if (settings.BypassHardware && state != null)
                    RestoreBypassAxisRuntimeState(state);
                else if (!settings.BypassHardware && state != null)
                    RestoreRealAxisHomeDoneState(state);
                if (state != null)
                    RestoreCylinderRuntimeState(state, settings);
                if (state != null)
                    RestoreAxisInitializeStepRuntimeState(state);

                if (state == null || !state.IsMachineInitialized)
                {
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "DeveloperModeNoSavedReady", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Developer mode is on, but saved initialized state does not exist. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-NO-STATE", "MachineController",
                        "Developer Mode: 저장된 장비 초기화 상태가 없어 INIT이 필요합니다.");
                    return;
                }

                string reason;
                if (!ValidateDeveloperRuntimeState(settings, state, out reason))
                {
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "DeveloperModeRestoreFailed", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Developer mode initialized state restore failed: " + reason + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-FAIL", "MachineController",
                        "Developer Mode: 장비 초기화 상태 복구 실패. " + reason);
                    return;
                }

                _isDeveloperReadyRestored = true;
                SetMachineInitialized(true, "DeveloperModeRestore", true);
                if (_status == EquipmentStatus.Idle || _status == EquipmentStatus.Stopped)
                    SetStatus(EquipmentStatus.Ready);

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Developer mode initialized state restored. file=" + MachineRuntimeStateStore.StatePath + " - Ok");
            }
            catch (Exception ex)
            {
                RestoreAxisInitializeStepRuntimeState(MachineRuntimeStateStore.Load());
                SetMachineInitialized(false, "StartupRestoreException", true);
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Machine initialized state restore failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-EX", "MachineController",
                    "장비 초기화 상태 복구 중 오류가 발생했습니다. " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ValidateDeveloperRuntimeState(
            AppSettings settings,
            MachineRuntimeState state,
            out string reason)
        {
            reason = "";
            try
            {
                if (settings == null)
                {
                    reason = "settings is null";
                    return false;
                }

                if (state == null)
                {
                    reason = "saved state is null";
                    return false;
                }

                if (!state.DeveloperMode)
                {
                    reason = "saved state was not created in Developer Mode";
                    return false;
                }

                if (state.Axes == null || state.Axes.Count == 0)
                {
                    reason = "saved axis state is empty";
                    return false;
                }

                foreach (var ax in EnumerateAxes())
                {
                    try { ax.UpdateStatus(); } catch { }

                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                    {
                        reason = "axis state is missing: " + ax.Name;
                        return false;
                    }

                    if (settings.BypassHardware)
                    {
                        if (!saved.IsServoOn || saved.IsAlarm || !saved.IsHomeDone)
                        {
                            reason = "saved axis is not ready: " + ax.Name;
                            return false;
                        }

                        continue;
                    }

                    if (!ax.IsServoOn)
                    {
                        reason = "axis servo is off: " + ax.Name;
                        return false;
                    }

                    if (ax.IsAlarm)
                    {
                        reason = "axis alarm is active: " + ax.Name;
                        return false;
                    }

                    if (!ax.IsHomeDone)
                    {
                        reason = "axis home is not done: " + ax.Name;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void SetMachineInitialized(bool initialized, string reason, bool saveState)
        {
            try
            {
                // 활성 Alarm 중 완료 콜백이 늦게 도착해도 초기화 완료 상태를 저장하지 않습니다.
                if (initialized &&
                    (_status == EquipmentStatus.Alarm || AlarmManager.HasActive))
                {
                    initialized = false;
                    reason = (reason ?? string.Empty) + ":BlockedByAlarm";
                }

                bool changed = _isMachineInitialized != initialized;
                _isMachineInitialized = initialized;
                if (initialized)
                    MachineInitializedAt = DateTime.Now;
                else
                    MachineInitializedAt = DateTime.MinValue;

                Log("[INIT-STATE] MachineInitialized=" + initialized + " reason=" + reason);
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitialized",
                    "Machine initialized=" + initialized + ", reason=" + reason + " - Ok");

                if (saveState)
                    SaveMachineRuntimeState(reason);

                if (changed)
                {
                    var h = MachineInitializedChanged;
                    if (h != null) try { h(initialized); } catch { }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitialized",
                    "Machine initialized state update failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public bool RequestMachineRuntimeStateSave(string reason)
        {
            bool startWorker = false;
            try
            {
                lock (_machineRuntimeStateSaveRequestLock)
                {
                    if (_machineRuntimeStateDeferredSaveClosed)
                        return false;

                    _machineRuntimeStateSaveRequestVersion++;
                    _pendingMachineRuntimeStateSaveVersion = _machineRuntimeStateSaveRequestVersion;
                    _pendingMachineRuntimeStateSaveReason = reason ?? string.Empty;
                    _pendingMachineRuntimeStateSaveRequestCount =
                        AddMachineRuntimeStateSaveRequestCount(_pendingMachineRuntimeStateSaveRequestCount, 1);
                    _machineRuntimeStateSaveRequested = true;

                    if (!_machineRuntimeStateSaveWorkerRunning)
                    {
                        _machineRuntimeStateSaveWorkerRunning = true;
                        startWorker = true;
                    }
                }

                return !startWorker || StartMachineRuntimeStateSaveWorker();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "장비 Runtime State 백그라운드 저장 요청에 실패했습니다. reason=" +
                    (reason ?? string.Empty) + ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        // [머지 복원 2026-07-27] 원격(f9210867)과 로컬 지연저장 리팩토링이 같은 구간을 수정하면서
        // 머지 결과에서 이 동기 저장 메서드 정의가 유실되어 빌드가 깨졌다(호출부 20여 곳 CS0103).
        // 서버 호출부는 건드리지 않고, 지연저장 워커와 버전 일관성을 유지하는 로컬 구현을 복원한다.
        // (동기 저장이 pending 요청 버전을 흡수하므로 순서/일관성은 서버의 동기 저장과 동일하다.)
        public bool SaveMachineRuntimeState(string reason)
        {
            long coveredRequestVersion = PrepareSynchronousMachineRuntimeStateSave();
            bool saved = SaveMachineRuntimeStateCore(
                reason,
                0,
                0L,
                coveredRequestVersion);

            if (!saved)
                RequestMachineRuntimeStateSave("SynchronousSaveRetry:" + (reason ?? string.Empty));

            return saved;
        }

        // [사용자 지시 2026-07-27] 픽업 핫패스의 die당 상태 저장(~10ms 디스크 I/O) 비동기화.
        // 저장 자체는 기존 SaveMachineRuntimeState의 lock으로 직렬화되고, 상태 캡처도 저장
        // 시점(락 안)에 이뤄지므로 순서/일관성은 동기 호출과 동일 — 호출자만 기다리지 않는다.
        public void SaveMachineRuntimeStateAsync(string reason)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    SaveMachineRuntimeState(reason);
                }
                catch
                {
                    // SaveMachineRuntimeState가 자체 로깅 — 백그라운드 예외 전파만 차단.
                }
            });
        }

        public void SaveMachineRuntimeStateForApplicationClosing()
        {
            try
            {
                CloseDeferredMachineRuntimeStateSaves();

                var settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                if (settings != null && settings.DeveloperMode)
                {
                    SaveMachineRuntimeState("ApplicationClosingDeveloperMode");
                    return;
                }

                SetMachineInitialized(false, "ApplicationClosingNormalMode", true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateClose",
                    "Machine runtime state close save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private bool StartMachineRuntimeStateSaveWorker()
        {
            try
            {
                Task.Run(() => ProcessMachineRuntimeStateSaveRequestsAsync());
                return true;
            }
            catch (Exception ex)
            {
                lock (_machineRuntimeStateSaveRequestLock)
                {
                    _machineRuntimeStateSaveWorkerRunning = false;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "장비 Runtime State 백그라운드 저장 작업을 시작하지 못했습니다. error=" +
                    ex.Message + " - Failed");
                return false;
            }
        }

        private async Task ProcessMachineRuntimeStateSaveRequestsAsync()
        {
            int nextDelayMs = MachineRuntimeStateSaveMergeIntervalMs;
            try
            {
                while (true)
                {
                    await Task.Delay(nextDelayMs).ConfigureAwait(false);

                    string reason;
                    int mergedRequestCount;
                    long requestVersion;
                    lock (_machineRuntimeStateSaveRequestLock)
                    {
                        if (_machineRuntimeStateDeferredSaveClosed ||
                            !_machineRuntimeStateSaveRequested)
                        {
                            return;
                        }

                        reason = _pendingMachineRuntimeStateSaveReason;
                        mergedRequestCount = _pendingMachineRuntimeStateSaveRequestCount;
                        requestVersion = _pendingMachineRuntimeStateSaveVersion;
                        _machineRuntimeStateSaveRequested = false;
                        _pendingMachineRuntimeStateSaveReason = string.Empty;
                        _pendingMachineRuntimeStateSaveRequestCount = 0;
                        _pendingMachineRuntimeStateSaveVersion = 0L;
                    }

                    bool saved = SaveMachineRuntimeStateCore(
                        reason,
                        mergedRequestCount,
                        requestVersion,
                        0L);
                    if (saved)
                    {
                        nextDelayMs = MachineRuntimeStateSaveMergeIntervalMs;
                        continue;
                    }

                    RequeueFailedMachineRuntimeStateSave(
                        reason,
                        mergedRequestCount,
                        requestVersion);
                    nextDelayMs = MachineRuntimeStateSaveFailureRetryMs;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "장비 Runtime State 백그라운드 저장 작업이 실패했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
                bool restartWorker = false;
                lock (_machineRuntimeStateSaveRequestLock)
                {
                    _machineRuntimeStateSaveWorkerRunning = false;
                    if (!_machineRuntimeStateDeferredSaveClosed &&
                        _machineRuntimeStateSaveRequested)
                    {
                        _machineRuntimeStateSaveWorkerRunning = true;
                        restartWorker = true;
                    }
                }

                if (restartWorker)
                    StartMachineRuntimeStateSaveWorker();
            }
        }

        private bool SaveMachineRuntimeStateCore(
            string reason,
            int mergedRequestCount,
            long deferredRequestVersion,
            long authoritativeRequestVersion)
        {
            try
            {
                lock (_machineRuntimeStateSaveLock)
                {
                    if (deferredRequestVersion > 0L &&
                        IsDeferredMachineRuntimeStateSaveSuperseded(deferredRequestVersion))
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                            "더 최신 동기 저장이 완료되어 이전 PickUp Runtime State 저장을 생략합니다. " +
                            "requestVersion=" + deferredRequestVersion + " - Skipped");
                        return true;
                    }

                    var state = CaptureMachineRuntimeState(reason);
                    bool ok = MachineRuntimeStateStore.Save(state);
                    if (ok && authoritativeRequestVersion > 0L)
                        MarkMachineRuntimeStateSaveAuthoritative(authoritativeRequestVersion);

                    string mergedText = mergedRequestCount > 0
                        ? ", deferredMergedRequests=" + mergedRequestCount
                        : string.Empty;
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                        "Machine runtime state save. reason=" + reason +
                        ", file=" + MachineRuntimeStateStore.StatePath +
                        mergedText +
                        (ok ? " - Ok" : " - Failed"));
                    return ok;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Machine runtime state save failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private long PrepareSynchronousMachineRuntimeStateSave()
        {
            lock (_machineRuntimeStateSaveRequestLock)
            {
                long coveredRequestVersion = _machineRuntimeStateSaveRequestVersion;
                _machineRuntimeStateSaveRequested = false;
                _pendingMachineRuntimeStateSaveReason = string.Empty;
                _pendingMachineRuntimeStateSaveRequestCount = 0;
                _pendingMachineRuntimeStateSaveVersion = 0L;
                return coveredRequestVersion;
            }
        }

        private void CloseDeferredMachineRuntimeStateSaves()
        {
            lock (_machineRuntimeStateSaveRequestLock)
            {
                _machineRuntimeStateDeferredSaveClosed = true;
            }
        }

        private bool IsDeferredMachineRuntimeStateSaveSuperseded(long requestVersion)
        {
            lock (_machineRuntimeStateSaveRequestLock)
            {
                return _machineRuntimeStateDeferredSaveClosed ||
                       requestVersion <= _machineRuntimeStateAuthoritativeVersion;
            }
        }

        private void MarkMachineRuntimeStateSaveAuthoritative(long requestVersion)
        {
            lock (_machineRuntimeStateSaveRequestLock)
            {
                if (requestVersion > _machineRuntimeStateAuthoritativeVersion)
                    _machineRuntimeStateAuthoritativeVersion = requestVersion;
            }
        }

        private void RequeueFailedMachineRuntimeStateSave(
            string reason,
            int mergedRequestCount,
            long requestVersion)
        {
            lock (_machineRuntimeStateSaveRequestLock)
            {
                if (_machineRuntimeStateDeferredSaveClosed ||
                    requestVersion <= _machineRuntimeStateAuthoritativeVersion)
                {
                    return;
                }

                if (!_machineRuntimeStateSaveRequested ||
                    requestVersion > _pendingMachineRuntimeStateSaveVersion)
                {
                    _pendingMachineRuntimeStateSaveReason = reason ?? string.Empty;
                    _pendingMachineRuntimeStateSaveVersion = requestVersion;
                }

                _pendingMachineRuntimeStateSaveRequestCount =
                    AddMachineRuntimeStateSaveRequestCount(
                        _pendingMachineRuntimeStateSaveRequestCount,
                        mergedRequestCount);
                _machineRuntimeStateSaveRequested = true;
            }
        }

        private static int AddMachineRuntimeStateSaveRequestCount(int current, int additional)
        {
            if (current >= int.MaxValue - additional)
                return int.MaxValue;

            return current + additional;
        }

        private MachineRuntimeState CaptureMachineRuntimeState(string reason)
        {
            var state = new MachineRuntimeState
            {
                IsMachineInitialized = _isMachineInitialized,
                DeveloperMode = AppSettingsStore.Current != null && AppSettingsStore.Current.DeveloperMode,
                SavedAt = DateTime.Now,
                SaveReason = reason ?? "",
                Status = _status.ToString(),
                MaterialSnapshotPath = QMC.CDT320.Materials.MaterialSnapshotStore.SnapshotPath,
                Axes = new List<MachineAxisRuntimeState>(),
                Cylinders = new List<MachineCylinderRuntimeState>(),
                PickerOffsets = CapturePickerOffsetRuntimeStates(),
                PickerWorkCounters = CapturePickerWorkCounterRuntimeStates(),
                InitializeSteps = CaptureAxisInitializeStepRuntimeStates()
            };

            foreach (var ax in EnumerateAxes())
            {
                try { ax.UpdateStatus(); } catch { }
                bool ignoreAxisAlarmForRuntimeState =
                    AppSettingsStore.Current != null &&
                    AppSettingsStore.Current.SimulationMode &&
                    ax.Config != null &&
                    ax.Config.IsSimulationMode;
                state.Axes.Add(new MachineAxisRuntimeState
                {
                    Name = ax.Name,
                    IsServoOn = ax.IsServoOn,
                    IsAlarm = ignoreAxisAlarmForRuntimeState ? false : ax.IsAlarm,
                    IsHomeDone = ax.IsHomeDone,
                    IsInPosition = ignoreAxisAlarmForRuntimeState ? true : ax.IsInPosition,
                    ActualPosition = ax.ActualPosition,
                    CommandPosition = ax.CommandPosition,
                    AlarmCode = ignoreAxisAlarmForRuntimeState ? 0 : ax.AlarmCode
                });
            }

            foreach (var cylinder in QMC.CDT320.Ajin.CylinderManager.Items.Values)
            {
                if (cylinder == null)
                    continue;

                try { cylinder.InFwd.UpdateStatus(); } catch { }
                try { cylinder.InBwd.UpdateStatus(); } catch { }
                try { cylinder.OutFwd.UpdateStatus(); } catch { }
                try { cylinder.OutBwd.UpdateStatus(); } catch { }

                state.Cylinders.Add(new MachineCylinderRuntimeState
                {
                    Name = cylinder.Name,
                    IsFwd = cylinder.IsFwd,
                    IsBwd = cylinder.IsBwd,
                    InFwdOn = cylinder.InFwd != null && cylinder.InFwd.IsOn,
                    InBwdOn = cylinder.InBwd != null && cylinder.InBwd.IsOn,
                    OutFwdOn = cylinder.OutFwd != null && cylinder.OutFwd.IsOn,
                    OutBwdOn = cylinder.OutBwd != null && cylinder.OutBwd.IsOn,
                    IsSingleSolenoid = cylinder.Setup != null && cylinder.Setup.IsSingleSolenoid,
                    IsSimulationMode = cylinder.Config != null && cylinder.Config.IsSimulationMode
                });
            }

            return state;
        }

        private List<MachinePickerOffsetRuntimeState> CapturePickerOffsetRuntimeStates()
        {
            var items = new List<MachinePickerOffsetRuntimeState>();
            try
            {
                CapturePickerOffsetRuntimeStates(items, "Front", _machine != null ? _machine.PickerFrontUnit : null);
                CapturePickerOffsetRuntimeStates(items, "Rear", _machine != null ? _machine.PickerRearUnit : null);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Picker offset runtime state capture failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return items;
        }

        private static void CapturePickerOffsetRuntimeStates(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            PickerFrontUnit unit)
        {
            if (items == null || unit == null)
                return;

            unit.EnsureRuntimePickerOffsets();
            unit.EnsureRuntimeSideInspectionCorrections();
            for (int i = 0; i < PickerFrontUnit.MaxPickerCount; i++)
                AddPickerOffsetRuntimeState(items, side, i, unit.GetRuntimePickerOffset(i), unit.GetRuntimeSideInspectionCorrection(i));
        }

        private static void CapturePickerOffsetRuntimeStates(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            PickerRearUnit unit)
        {
            if (items == null || unit == null)
                return;

            unit.EnsureRuntimePickerOffsets();
            unit.EnsureRuntimeSideInspectionCorrections();
            for (int i = 0; i < PickerRearUnit.MaxPickerCount; i++)
                AddPickerOffsetRuntimeState(items, side, i, unit.GetRuntimePickerOffset(i), unit.GetRuntimeSideInspectionCorrection(i));
        }

        private static void AddPickerOffsetRuntimeState(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            int pickerIndex,
            PickerAlignOffset offset,
            PickerSideInspectionCorrection sideCorrection)
        {
            if (items == null || offset == null)
                return;

            items.Add(new MachinePickerOffsetRuntimeState
            {
                Side = side,
                PickerIndex = pickerIndex,
                AlignOffsetX = offset.AlignOffsetX,
                AlignOffsetY = offset.AlignOffsetY,
                AlignOffsetT = offset.AlignOffsetT,
                SideInspectionCorrectionValid = sideCorrection != null && sideCorrection.IsValid,
                BottomOffsetXmm = sideCorrection != null ? sideCorrection.BottomOffsetXmm : 0.0,
                BottomOffsetYmm = sideCorrection != null ? sideCorrection.BottomOffsetYmm : 0.0,
                PickerZOffset = sideCorrection != null ? sideCorrection.PickerZOffset : 0.0,
                SideInspectionSourceDieId = sideCorrection != null ? sideCorrection.SourceDieId : string.Empty,
                SideInspectionUpdatedAt = NormalizeOptionalRuntimeDateTime(
                    sideCorrection != null ? sideCorrection.UpdatedAt : DateTime.MinValue,
                    DateTime.Now)
            });
        }

        private static DateTime NormalizeOptionalRuntimeDateTime(DateTime value, DateTime fallback)
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

        private void RestorePickerOffsetRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.PickerOffsets == null || state.PickerOffsets.Count == 0)
                    return;

                int restored = 0;
                foreach (MachinePickerOffsetRuntimeState saved in state.PickerOffsets)
                {
                    if (saved == null)
                        continue;

                    PickerAlignOffset offset = new PickerAlignOffset
                    {
                        AlignOffsetX = saved.AlignOffsetX,
                        AlignOffsetY = saved.AlignOffsetY,
                        AlignOffsetT = saved.AlignOffsetT
                    };
                    PickerSideInspectionCorrection sideCorrection = new PickerSideInspectionCorrection
                    {
                        IsValid = saved.SideInspectionCorrectionValid,
                        BottomOffsetXmm = saved.BottomOffsetXmm,
                        BottomOffsetYmm = saved.BottomOffsetYmm,
                        PickerZOffset = saved.PickerZOffset,
                        SourceDieId = saved.SideInspectionSourceDieId,
                        UpdatedAt = saved.SideInspectionUpdatedAt
                    };

                    if (string.Equals(saved.Side, "Front", StringComparison.OrdinalIgnoreCase) &&
                        _machine != null && _machine.PickerFrontUnit != null)
                    {
                        _machine.PickerFrontUnit.RestoreRuntimePickerOffset(saved.PickerIndex, offset);
                        _machine.PickerFrontUnit.RestoreRuntimeSideInspectionCorrection(saved.PickerIndex, sideCorrection);
                        restored++;
                    }
                    else if (string.Equals(saved.Side, "Rear", StringComparison.OrdinalIgnoreCase) &&
                             _machine != null && _machine.PickerRearUnit != null)
                    {
                        _machine.PickerRearUnit.RestoreRuntimePickerOffset(saved.PickerIndex, offset);
                        _machine.PickerRearUnit.RestoreRuntimeSideInspectionCorrection(saved.PickerIndex, sideCorrection);
                        restored++;
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker offset runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker offset runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private List<MachinePickerWorkCounterRuntimeState> CapturePickerWorkCounterRuntimeStates()
        {
            var items = new List<MachinePickerWorkCounterRuntimeState>();
            try
            {
                CapturePickerWorkCounterRuntimeState(items, "Front", _machine != null ? _machine.PickerFrontUnit : null);
                CapturePickerWorkCounterRuntimeState(items, "Rear", _machine != null ? _machine.PickerRearUnit : null);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Picker work counter runtime state capture failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return items;
        }

        private static void CapturePickerWorkCounterRuntimeState(
            List<MachinePickerWorkCounterRuntimeState> items,
            string side,
            PickerFrontUnit unit)
        {
            if (items == null || unit == null)
                return;

            items.Add(new MachinePickerWorkCounterRuntimeState
            {
                Side = side,
                ColletUseCounts = CopyCounterArray(unit.ColletUseCounts, PickerFrontUnit.MaxPickerCount),
                PickFailCount = unit.PickFailCount,
                PlaceFailCount = unit.PlaceFailCount
            });
        }

        private static void CapturePickerWorkCounterRuntimeState(
            List<MachinePickerWorkCounterRuntimeState> items,
            string side,
            PickerRearUnit unit)
        {
            if (items == null || unit == null)
                return;

            items.Add(new MachinePickerWorkCounterRuntimeState
            {
                Side = side,
                ColletUseCounts = CopyCounterArray(unit.ColletUseCounts, PickerRearUnit.MaxPickerCount),
                PickFailCount = unit.PickFailCount,
                PlaceFailCount = unit.PlaceFailCount
            });
        }

        private static int[] CopyCounterArray(int[] source, int count)
        {
            int[] result = new int[count];
            if (source == null)
                return result;

            int copyCount = Math.Min(count, source.Length);
            for (int i = 0; i < copyCount; i++)
                result[i] = Math.Max(0, source[i]);

            return result;
        }

        private void RestorePickerWorkCounterRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.PickerWorkCounters == null || state.PickerWorkCounters.Count == 0)
                    return;

                int restored = 0;
                foreach (MachinePickerWorkCounterRuntimeState saved in state.PickerWorkCounters)
                {
                    if (saved == null)
                        continue;

                    if (string.Equals(saved.Side, "Front", StringComparison.OrdinalIgnoreCase) &&
                        _machine != null && _machine.PickerFrontUnit != null)
                    {
                        _machine.PickerFrontUnit.RestoreWorkCounters(
                            saved.ColletUseCounts,
                            saved.PickFailCount,
                            saved.PlaceFailCount);
                        restored++;
                    }
                    else if (string.Equals(saved.Side, "Rear", StringComparison.OrdinalIgnoreCase) &&
                             _machine != null && _machine.PickerRearUnit != null)
                    {
                        _machine.PickerRearUnit.RestoreWorkCounters(
                            saved.ColletUseCounts,
                            saved.PickFailCount,
                            saved.PlaceFailCount);
                        restored++;
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker work counter runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker work counter runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreBypassAxisRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.Axes == null)
                    return;

                int restored = 0;
                foreach (var ax in EnumerateAxes())
                {
                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                        continue;

                    ax.RestoreRuntimeState(
                        saved.ActualPosition,
                        saved.CommandPosition,
                        saved.IsServoOn,
                        saved.IsHomeDone,
                        saved.IsInPosition,
                        saved.IsAlarm,
                        saved.AlarmCode);
                    restored++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Bypass axis runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Bypass axis runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreRealAxisHomeDoneState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.Axes == null)
                    return;

                int restored = 0;
                foreach (var ax in EnumerateAxes())
                {
                    QMC.CDT320.Ajin.AjinAxis ajin = ax as QMC.CDT320.Ajin.AjinAxis;
                    if (ajin == null)
                        continue;

                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                        continue;

                    // 실장비: 보드 HomeDone=off 보고를 무시하도록 저장된 초기화 신호를 latch 복원한다.
                    // 위치/서보는 보드 값을 그대로 따른다.
                    ajin.RestoreHomeDoneSignal(saved.IsHomeDone, saved.IsAlarm);
                    if (saved.IsHomeDone && !saved.IsAlarm)
                        restored++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Real axis HomeDone signal restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Real axis HomeDone signal restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreCylinderRuntimeState(MachineRuntimeState state, AppSettings settings)
        {
            try
            {
                if (QMC.CDT320.Ajin.CylinderManager.Items == null || QMC.CDT320.Ajin.CylinderManager.Items.Count == 0)
                    return;

                bool hasSavedState = state != null && state.Cylinders != null && state.Cylinders.Count > 0;
                int restored = 0;
                int defaulted = 0;
                foreach (var cylinder in QMC.CDT320.Ajin.CylinderManager.Items.Values)
                {
                    if (cylinder == null)
                        continue;

                    if (!CanRestoreSimulationCylinder(cylinder))
                        continue;

                    MachineCylinderRuntimeState saved = null;
                    if (hasSavedState)
                    {
                        foreach (var item in state.Cylinders)
                        {
                            if (item != null && string.Equals(item.Name, cylinder.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                saved = item;
                                break;
                            }
                        }
                    }

                    if (saved != null)
                    {
                        RestoreCylinderIoState(cylinder, saved);
                        restored++;
                    }

                    if (EnsureSimulationCylinderDisplayState(cylinder))
                        defaulted++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Cylinder runtime state restored. count=" + restored + ", defaulted=" + defaulted + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Cylinder runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool CanRestoreSimulationCylinder(QMC.Common.IO.BaseCylinder cylinder)
        {
            if (cylinder == null || cylinder.Config == null || !cylinder.Config.IsSimulationMode)
                return false;

            return IsSimulationInput(cylinder.InFwd)
                && IsSimulationInput(cylinder.InBwd)
                && IsSimulationOutput(cylinder.OutFwd)
                && IsSimulationOutput(cylinder.OutBwd);
        }

        private static bool IsSimulationInput(QMC.Common.IO.BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IsSimulationMode;
        }

        private static bool IsSimulationOutput(QMC.Common.IO.BaseDigitalOutput output)
        {
            return output != null && output.Config != null && output.Config.IsSimulationMode;
        }

        private static void RestoreCylinderIoState(QMC.Common.IO.BaseCylinder cylinder, MachineCylinderRuntimeState saved)
        {
            if (cylinder == null || saved == null)
                return;

            if (cylinder.OutFwd != null)
                cylinder.OutFwd.Write(saved.OutFwdOn);
            if (cylinder.OutBwd != null)
                cylinder.OutBwd.Write(saved.OutBwdOn);

            if (cylinder.InFwd != null)
                cylinder.InFwd.SimulateInput(saved.InFwdOn);
            if (cylinder.InBwd != null)
                cylinder.InBwd.SimulateInput(saved.InBwdOn);
        }

        private static bool EnsureSimulationCylinderDisplayState(QMC.Common.IO.BaseCylinder cylinder)
        {
            if (cylinder == null || !CanRestoreSimulationCylinder(cylinder))
                return false;

            bool inFwdOn = cylinder.InFwd != null && cylinder.InFwd.IsOn;
            bool inBwdOn = cylinder.InBwd != null && cylinder.InBwd.IsOn;
            if (inFwdOn != inBwdOn)
                return false;

            if (cylinder.OutFwd != null)
                cylinder.OutFwd.Write(false);
            if (cylinder.OutBwd != null)
                cylinder.OutBwd.Write(false);
            if (cylinder.InFwd != null)
                cylinder.InFwd.SimulateInput(false);
            if (cylinder.InBwd != null)
                cylinder.InBwd.SimulateInput(true);

            return true;
        }

    }
}
