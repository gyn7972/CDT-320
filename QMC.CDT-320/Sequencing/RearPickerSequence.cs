using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    public sealed class RearPickerSequence : UnitSequenceBase
    {
        private static readonly TimeSpan FrontPickupYieldLogInterval = TimeSpan.FromSeconds(2);
        private PickerProcessSequence _stepSequence;
        private DateTime _lastFrontPickupYieldLogTime = DateTime.MinValue;
        private bool _outputGoodPickupCapBlocked;

        public RearPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.PickerRear, "RearPicker")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await WaitForPickerWorkAsync(ct).ConfigureAwait(false);
                    if (IsWaferCompletionRunComplete())
                        return;

                    await WaitForLoaderInactiveBeforePickerProcessAsync(ct).ConfigureAwait(false);
                    if (IsWaferCompletionRunComplete())
                        return;

                    if (await YieldInputPickupPriorityToFrontAsync(ct).ConfigureAwait(false))
                        continue;

                    PickerSequenceOptions options = BuildSequenceOptions();
                    PickerProcessSequence processSequence = new PickerProcessSequence(Context, PickerSequenceSide.Rear);
                    int result = await SequenceTrace.ChildAsync("PickerProcessSequence", "Process",
                        () => processSequence.RunAsync(ct, options),
                        "side=Rear").ConfigureAwait(false);
                    if (result != 0)
                        throw new InvalidOperationException(
                            SequenceFailureStore.AppendRecentDetail(
                                "RearPicker 자동 시퀀스 실패. result=" + result,
                                "RearPicker",
                                "REAR-PICKER-SEQUENCE"));

                    Context.StopIfCycleStopRequested("RearPickerSequence.ProcessComplete");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "RearPicker 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("ExecuteAutoAsync", "RearPicker 자동 시퀀스 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                EnsureLoaderInactiveForManualStep();

                if (_stepSequence == null || _stepSequence.IsComplete)
                    _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Rear);

                PickerSequenceOptions options = BuildSequenceOptions();
                await RunStepSequenceAsync(ct, options).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "RearPicker 수동/스텝 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("ExecuteStepAsync", "RearPicker 수동/스텝 시퀀스 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private PickerSequenceOptions BuildSequenceOptions()
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = Mode;
            options.SimulateVisionResult = ShouldSimulateVisionResult();
            options.PickerMotionOnlyTestMode = Mode == SequenceRunMode.Auto && IsPickerMotionOnlyTestModeEnabled();
            options.RequireInputCameraMarkInspectionPermission = Mode == SequenceRunMode.Auto;
            options.ApplyInputStageVisionPolicy(Context != null ? Context.Machine : null);
            return options;
        }

        private bool IsPickerMotionOnlyTestModeEnabled()
        {
            try
            {
                return QMC.CDT320.AppSettingsStore.Current != null &&
                       QMC.CDT320.AppSettingsStore.Current.PickerMotionOnlyTestMode;
            }
            catch (Exception ex)
            {
                WriteLog("IsPickerMotionOnlyTestModeEnabled", "RearPicker Picker Motion Only Test 설정 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool ShouldSimulateVisionResult()
        {
            try
            {
                if (QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                    return false;

                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin))
                    return true;

                if (Context != null && Context.Controller != null && Context.Controller.GlobalDryRun)
                    return false;

                if (Context != null &&
                    Context.Machine != null &&
                    Context.Machine.PickerRearUnit != null &&
                    ((Context.Machine.PickerRearUnit.Setup != null && Context.Machine.PickerRearUnit.Setup.IsSimulationMode) ||
                     (Context.Machine.PickerRearUnit.Config != null && Context.Machine.PickerRearUnit.Config.IsSimulationMode)))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("ShouldSimulateVisionResult", "RearPicker Vision 시뮬레이션 조건 확인 실패: " + ex.Message + " - Failed");
                return true;
            }
            finally
            {
            }
        }

        private async Task RunStepSequenceAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            try
            {
                int result = await SequenceTrace.ChildAsync("PickerProcessSequence", "StepProcess",
                    () => _stepSequence.RunAsync(ct, options),
                    "side=Rear").ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "RearPicker 수동/스텝 시퀀스 실패. result=" + result,
                            "RearPicker",
                            "REAR-PICKER-STEP"));

                if (_stepSequence.IsComplete)
                    _stepSequence = null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task WaitForPickerWorkAsync(CancellationToken ct)
        {
            try
            {
                while (!HasPickerWork())
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested("RearPickerSequence.WaitForWork");

                    await EnsureIdlePickerAvoidAsync(ct).ConfigureAwait(false);
                    TryPublishInputStageExchangeReadyWithoutPickTarget();
                    ObserveWaferCompletionRun();
                    if (IsWaferCompletionRunComplete())
                        return;

                    // 유휴 폴 주기: 1ms 폴은 CPU/축 상태 조회 폭주를 유발했다. AutoSequenceGate와 동일한 20ms를 기본으로 사용한다.
                    // 대기 루프의 부수 임무(교체 준비 publish/Avoid 유지/CycleStop 관찰)는 그대로 유지된다.
                    await Task.Delay(PickerSequenceIdlePolicy.IdlePollDelayMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("WaitForPickerWorkAsync", "RearPicker 작업 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("WaitForPickerWorkAsync", "RearPicker 작업 대기 중 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private async Task WaitForLoaderInactiveBeforePickerProcessAsync(CancellationToken ct)
        {
            bool waitLogged = false;
            while (IsInputOrOutputLoaderActive())
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested(
                    "RearPickerSequence.WaitLoaderInactive",
                    HasLoadedDieOnPicker(),
                    "RearPicker loaded die drain");

                // 현재 기준: Input/Output 로더 동작 중에는 Picker가 Avoid에서 신규 공정 진입을 기다린다.
                await EnsureIdlePickerAvoidAsync(ct).ConfigureAwait(false);
                if (!waitLogged)
                {
                    WriteLog("WaitForLoaderInactiveBeforePickerProcessAsync",
                        "RearPicker 신규 공정 대기: Input/Output 로더가 동작 중입니다. - Wait");
                    waitLogged = true;
                }

                await Task.Delay(100, ct).ConfigureAwait(false);
            }

            if (waitLogged)
            {
                WriteLog("WaitForLoaderInactiveBeforePickerProcessAsync",
                    "RearPicker 신규 공정 대기 해제: Input/Output 로더 동작 종료. - Ok");
            }
        }

        private void EnsureLoaderInactiveForManualStep()
        {
            if (!IsInputOrOutputLoaderActive())
                return;

            throw new InvalidOperationException("Input/Output 로더 동작 중이므로 RearPicker 수동/스텝 공정 진입이 차단되었습니다.");
        }

        private bool IsInputOrOutputLoaderActive()
        {
            return Context != null &&
                   Context.Bus != null &&
                   (Context.Bus.IsSet("InputLoaderActive") || Context.Bus.IsSet("OutputLoaderActive"));
        }

        private async Task EnsureIdlePickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                ThrowIfAlarmStopActive(ct, "RearPicker Idle Avoid 시작 전");

                if (Mode != SequenceRunMode.Auto || HasLoadedDieOnPicker() || !IsRearPickerEnabled())
                    return;

                PickerRearUnit rear = Context != null && Context.Machine != null
                    ? Context.Machine.PickerRearUnit
                    : null;
                if (rear == null || CanSkipIdlePickerAvoidMove(rear))
                    return;

                string safeReason;
                if (!CanMoveIdlePickerToAvoid(rear, out safeReason))
                {
                    WriteLog("EnsureIdlePickerAvoidAsync",
                        "RearPicker 작업 대기 중 Idle Avoid 이동을 생략합니다. " +
                        "현재 기준: 실제 Picker가 Process 존이거나 phase/workArea가 남아 있으면 자동 Avoid 이동하지 않습니다. " +
                        "reason=" + safeReason + " - Check");
                    return;
                }

                using (SequenceResourceLease pickerLease = await Context.Resources
                    .AcquireAsync(SequenceResourceKind.RearPicker, "RearPicker:IdleAvoid", 200, ct, false)
                    .ConfigureAwait(false))
                {
                    if (pickerLease == null)
                        return;

                    if (CanSkipIdlePickerAvoidMove(rear))
                        return;

                    if (!CanMoveIdlePickerToAvoid(rear, out safeReason))
                    {
                        WriteLog("EnsureIdlePickerAvoidAsync",
                            "RearPicker Idle Avoid 이동 직전 조건이 바뀌어 이동을 생략합니다. " +
                            "reason=" + safeReason + " - Check");
                        return;
                    }

                    ThrowIfAlarmStopActive(ct, "RearPicker Idle Avoid 이동 직전");
                    WriteLog("EnsureIdlePickerAvoidAsync", "RearPicker 작업 대기 중이므로 Avoid 위치로 이동합니다. - Start");
                    int result = await rear.MoveToRearPickerAvoidPosition(false).ConfigureAwait(false);
                    if (result != 0 || !rear.IsRearPickerInAvoidPosition())
                    {
                        ThrowIfAlarmStopActive(ct, "RearPicker Idle Avoid 이동 중");
                        throw new InvalidOperationException(
                            "RearPicker 작업 대기 중 Avoid 이동 실패. result=" + result +
                            ", finalAvoid=" + rear.IsRearPickerInAvoidPosition());
                    }
                }

                WriteLog("EnsureIdlePickerAvoidAsync", "RearPicker 작업 대기 중 Avoid 위치 이동 완료. - Ok");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("EnsureIdlePickerAvoidAsync", "RearPicker 작업 대기 중 Avoid 이동 예외 발생: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private void ThrowIfAlarmStopActive(CancellationToken ct, string phase)
        {
            if (!IsAlarmStopActive())
                return;

            throw new OperationCanceledException(
                phase + " 활성 알람으로 이동을 시작하지 않고 시퀀스를 취소합니다.",
                ct);
        }

        private bool IsAlarmStopActive()
        {
            try
            {
                if (AlarmManager.HasActive)
                    return true;

                return Context != null &&
                       Context.Controller != null &&
                       Context.Controller.Status == EquipmentStatus.Alarm;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static bool CanSkipIdlePickerAvoidMove(PickerRearUnit rear)
        {
            if (rear == null)
                return false;

            foreach (var pair in rear.Axes)
            {
                if (pair.Value == null)
                    return false;

                pair.Value.UpdateStatus();
                double target = rear.GetPickerTeachingPosition(pair.Key, "AvoidPosition");
                if (!pair.Value.IsAtTargetPosition(target, 0.0))
                    return false;
            }

            return true;
        }

        private bool CanMoveIdlePickerToAvoid(PickerRearUnit rear, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (rear == null)
                {
                    reason = "RearPickerUnit=null";
                    return false;
                }

                if (Context != null && Context.PickerPhases != null)
                {
                    PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                    if (snapshot.Rear.Phase != PickerProcessPhase.Idle)
                    {
                        reason = "RearPicker phase가 Idle이 아닙니다. phase=" + snapshot.Rear;
                        return false;
                    }
                }

                PickerWorkZone workZone;
                string owner;
                if (PickerZoneInterlockRules.TryGetPickerWorkArea(false, out workZone, out owner))
                {
                    reason = "RearPicker workArea가 남아 있습니다. zone=" + workZone + ", owner=" + owner;
                    return false;
                }

                PickerWorkZone xZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    false);
                if (PickerZoneInterlockRules.IsProcessZone(xZone))
                {
                    reason = "RearPickerX가 Process 존에 있습니다. xZone=" + xZone;
                    return false;
                }

                if (xZone == PickerWorkZone.Unknown &&
                    !rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    reason = "RearPickerX 존을 확정할 수 없고 RearPickerY가 Avoid가 아닙니다.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "Idle Avoid 안전 조건 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool HasPickerWork()
        {
            try
            {
                if (HasLoadedDieOnPicker())
                    return true;

                if (ShouldBlockNewPickForWaferCompletion())
                    return false;

                if (!IsRearPickerEnabled())
                    return false;

                if (!HasEnabledRearPickerHead())
                {
                    ClearDisabledPickerReservations();
                    return false;
                }

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");

                if (!inputStageReady)
                    return false;

                // 전역 target 존재 여부가 아니라 "이 side가 실제 처리 가능한" target(미예약 또는 Rear 예약)으로 판정한다.
                // 전역 판정은 상대 픽커 예약 die에도 true를 반환해 빈 PickerProcess 무한 재진입(busy loop)을 유발했다.
                bool actionable = MaterialStateService.HasActionableInputStagePickTarget(
                    MaterialLocationKind.PickerRear);
                if (!actionable)
                    return false;

                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || !settings.UseOutputGoodPickupCap)
                {
                    _outputGoodPickupCapBlocked = false;
                    return true;
                }

                bool hasOwnReservation = MaterialStateService.HasInputStagePickReservationForPickerLocation(
                    MaterialLocationKind.PickerRear);
                int pending;
                int held;
                int reserved;
                int allowance = MaterialStateService.GetOutputGoodNewPickAllowance(
                    out pending,
                    out held,
                    out reserved);
                bool blocked = !hasOwnReservation && allowance <= 0;
                if (blocked == _outputGoodPickupCapBlocked)
                    return !blocked;

                _outputGoodPickupCapBlocked = blocked;
                int ownReservationCount = MaterialStateService.CountInputStagePickReservationsForPickerLocation(
                    MaterialLocationKind.PickerRear);
                WriteLog("HasPickerWork",
                    "RearPicker GOOD 배출 픽업 캡 진입 차단 " + (blocked ? "시작" : "해제") + ". " +
                    "allowance=" + allowance +
                    ", pending=" + pending +
                    ", held=" + held +
                    ", reserved=" + reserved +
                    ", ownReserved=" + ownReservationCount + " - " + (blocked ? "Wait" : "Ok"));

                // [사용자 승인 2026-08-17] 위 WriteLog는 4-인자 Log.Write라 LogPolicy에서 폐기된다.
                // 이 캡 차단이 무언정지의 직접 원인이므로 전이 시점만 Warning으로 올려 디스크에 남긴다.
                LogPickupCapTransition(blocked, allowance, pending, held, reserved, ownReservationCount);
                return !blocked;
            }
            catch (Exception ex)
            {
                WriteLog("HasPickerWork", "RearPicker 작업 조건 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// GOOD 배출 픽업 캡 차단/해제 전이를 최소 로그 정책에서도 보존한다(전이 시점 1회, 폴링 아님).
        /// allowance=0은 출력 GOOD wafer의 수령 슬롯이 0이라는 뜻이며, 이 상태에서는 픽커가
        /// 알람 없이 무한 대기한다(2026-08-17 실측). 사유 추적에 필요한 계측값을 함께 남긴다.
        /// </summary>
        private void LogPickupCapTransition(
            bool blocked,
            int allowance,
            int pending,
            int held,
            int reserved,
            int ownReservationCount)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "SYSTEM",
                    blocked ? "PICK-CAP-BLOCKED" : "PICK-CAP-CLEARED",
                    "RearPicker",
                    "GOOD 배출 픽업 캡 " + (blocked ? "차단 — 픽커가 신규 픽업을 시작하지 않습니다." : "해제") +
                    " allowance=" + allowance +
                    ", pending=" + pending +
                    ", held=" + held +
                    ", reserved=" + reserved +
                    ", ownReserved=" + ownReservationCount);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void TryPublishInputStageExchangeReadyWithoutPickTarget()
        {
            try
            {
                if (Mode != SequenceRunMode.Auto || Context == null || Context.Bus == null)
                    return;
                if (!Context.Bus.IsSet("InputStageReady") || Context.Bus.IsSet("InputStageDieComplete"))
                    return;
                if (!MaterialStateService.IsInputStagePickComplete() || MaterialStateService.HasReadyInputStagePickTarget())
                    return;

                PickerRearUnit rear = Context.Machine != null ? Context.Machine.PickerRearUnit : null;
                if (!IsRearPickerEnabled() || rear == null || !rear.IsRearPickerInAvoidPosition())
                    return;

                Context.Bus.Set("InputStageDieComplete");
                WriteLog("InputStageExchangeReady",
                    "RearPicker가 InputStage Pick 대상 없음과 자기 Picker 전체 Avoid를 확인하고 교체 준비 신호를 발행했습니다. " +
                    "signal=InputStageDieComplete - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputStageExchangeReady",
                    "RearPicker InputStage 교체 준비 신호 판단 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
        }

        private void ObserveWaferCompletionRun()
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion != null && completion.Enabled)
                completion.ObserveCompletionSignals();
        }

        private bool ShouldBlockNewPickForWaferCompletion()
        {
            ObserveWaferCompletionRun();
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            return completion != null && completion.Enabled && completion.IsDrainRequested;
        }

        private bool IsWaferCompletionRunComplete()
        {
            ObserveWaferCompletionRun();
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            return completion != null && completion.Enabled && completion.IsRunComplete;
        }

        private bool HasEnabledRearPickerHead()
        {
            try
            {
                PickerRearUnit rear = Context != null && Context.Machine != null
                    ? Context.Machine.PickerRearUnit
                    : null;
                bool[] usePicker = rear != null && rear.Config != null ? rear.Config.UsePicker : null;
                if (usePicker == null || usePicker.Length == 0)
                    return false;

                for (int i = 0; i < usePicker.Length; i++)
                {
                    if (usePicker[i])
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("HasEnabledRearPickerHead",
                    "RearPicker 사용 Head 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private void ClearDisabledPickerReservations()
        {
            try
            {
                InputCameraPickUpPermissionStore.Clear(PickerSequenceSide.Rear);
                int releaseCount = MaterialStateService.ReleaseInputStagePickReservationsForPickerLocation(
                    MaterialLocationKind.PickerRear,
                    "RearPicker 사용 가능한 Head가 없습니다.");
                if (releaseCount > 0)
                    WriteLog("ClearDisabledPickerReservations",
                        "RearPicker 사용 가능한 Head가 없어 남은 Pick 예약을 해제했습니다. count=" + releaseCount + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ClearDisabledPickerReservations",
                    "RearPicker 사용 불가 예약 해제 중 예외 발생: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<bool> YieldInputPickupPriorityToFrontAsync(CancellationToken ct)
        {
            try
            {
                if (Mode != SequenceRunMode.Auto)
                    return false;

                if (PickerFirstForwardSequencer.IsResumeDrainRequired(PickerSequenceSide.Rear))
                {
                    WriteLog("YieldInputPickupPriorityToFrontAsync",
                        "RearPicker 재시작 드레인이 남아 있어 FrontPicker PickUp 우선권 양보를 건너뜁니다. " +
                        "정지/알람 복구 중 남은 RearPicker 작업을 먼저 완료합니다. - Check");
                    return false;
                }

                if (HasLoadedDieOnPicker())
                    return false;

                if (MaterialStateService.HasInputStagePickReservationForPickerLocation(MaterialLocationKind.PickerRear))
                {
                    WriteLog("YieldInputPickupPriorityToFrontAsync",
                        "RearPicker에 복구/선행검사 Pick 예약이 남아 있어 FrontPicker 우선권 양보를 건너뜁니다. - Check");
                    return false;
                }

                if (!IsFrontPickerEnabled())
                    return false;

                if (HasLoadedDieOnFrontPicker())
                    return false;

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");
                if (!inputStageReady || !MaterialStateService.HasReadyInputStagePickTarget())
                    return false;

                Context.StopIfCycleStopRequested("RearPickerSequence.YieldInputPickupPriorityToFront");
                string firstForwardYieldDetail;
                if (!PickerFirstForwardSequencer.TryYieldExpectedSideToPriority(
                    PickerSequenceSide.Rear,
                    PickerSequenceSide.Front,
                    out firstForwardYieldDetail))
                {
                    WriteLog("YieldInputPickupPriorityToFrontAsync",
                        "RearPicker Front 우선권 양보를 첫 전진 게이트에 반영할 수 없어 RearPicker 공정 진입을 허용합니다. " +
                        firstForwardYieldDetail + " - Check");
                    return false;
                }

                WriteFrontPickupYieldWaitLog(firstForwardYieldDetail);

                // Front 양보 대기도 1ms 폴 대신 공통 유휴 폴 주기를 사용한다(양보 판정 자체는 매 폴 재평가).
                await Task.Delay(PickerSequenceIdlePolicy.IdlePollDelayMs, ct).ConfigureAwait(false);
                Context.StopIfCycleStopRequested("RearPickerSequence.YieldInputPickupPriorityToFront");
                return true;
            }
            catch (OperationCanceledException)
            {
                WriteLog("YieldInputPickupPriorityToFrontAsync",
                    "RearPicker Front 우선순위 양보 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("YieldInputPickupPriorityToFrontAsync",
                    "RearPicker Front 우선순위 양보 확인 중 예외 발생: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private void WriteFrontPickupYieldWaitLog(string firstForwardYieldDetail)
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now - _lastFrontPickupYieldLogTime < FrontPickupYieldLogInterval)
                    return;

                _lastFrontPickupYieldLogTime = now;
                WriteLog("YieldInputPickupPriorityToFrontAsync",
                    "RearPicker가 FrontPicker PickUp 우선권을 위해 대기합니다. " +
                    "FrontPicker가 비어 있고 InputStage에 Pick 대상이 남아 있습니다. " +
                    "firstForwardGate=" + firstForwardYieldDetail + " - Wait");
            }
            catch (Exception ex)
            {
                WriteLog("YieldInputPickupPriorityToFrontAsync",
                    "RearPicker Front 우선권 대기 로그 처리 중 예외 발생: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private bool IsRearPickerEnabled()
        {
            try
            {
                return Context != null &&
                       Context.Machine != null &&
                       Context.Machine.PickerRearUnit != null &&
                       Context.Machine.PickerRearUnit.Config != null &&
                       Context.Machine.PickerRearUnit.Config.UseUnit;
            }
            catch (Exception ex)
            {
                WriteLog("IsRearPickerEnabled",
                    "RearPicker 사용 여부 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool HasLoadedDieOnPicker()
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerRear, pickerNo) != null)
                    return true;
            }

            return false;
        }

        private bool IsFrontPickerEnabled()
        {
            try
            {
                return Context != null &&
                       Context.Machine != null &&
                       Context.Machine.PickerFrontUnit != null &&
                       Context.Machine.PickerFrontUnit.Config != null &&
                       Context.Machine.PickerFrontUnit.Config.UseUnit;
            }
            catch (Exception ex)
            {
                WriteLog("IsFrontPickerEnabled",
                    "FrontPicker 사용 여부 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool HasLoadedDieOnFrontPicker()
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerFront, pickerNo) != null)
                    return true;
            }

            return false;
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RearPickerSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.RearHeadSeq, source, message);
        }
    }
}
