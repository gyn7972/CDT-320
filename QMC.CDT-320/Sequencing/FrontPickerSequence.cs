using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    public sealed class FrontPickerSequence : UnitSequenceBase
    {
        private PickerProcessSequence _stepSequence;

        public FrontPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.PickerFront, "FrontPicker")
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

                    PickerSequenceOptions options = BuildSequenceOptions();
                    PickerProcessSequence processSequence = new PickerProcessSequence(Context, PickerSequenceSide.Front);
                    int result = await SequenceTrace.ChildAsync("PickerProcessSequence", "Process",
                        () => processSequence.RunAsync(ct, options),
                        "side=Front").ConfigureAwait(false);
                    if (result != 0)
                        throw new System.InvalidOperationException(
                            SequenceFailureStore.AppendRecentDetail(
                                "FrontPicker 자동 시퀀스 실패. result=" + result,
                                "FrontPicker",
                                "FRONT-PICKER-SEQUENCE"));

                    Context.StopIfCycleStopRequested("FrontPickerSequence.ProcessComplete");
                }
            }
            catch (System.OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "FrontPicker 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("ExecuteAutoAsync", "FrontPicker 자동 시퀀스 예외 발생: " + ex.Message + " - Failed");
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
                    _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Front);

                PickerSequenceOptions options = BuildSequenceOptions();
                await RunStepSequenceAsync(ct, options).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "FrontPicker 수동/스텝 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("ExecuteStepAsync", "FrontPicker 수동/스텝 시퀀스 예외 발생: " + ex.Message + " - Failed");
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
            catch (System.Exception ex)
            {
                WriteLog("IsPickerMotionOnlyTestModeEnabled", "FrontPicker Picker Motion Only Test 설정 확인 실패: " + ex.Message + " - Failed");
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
                    Context.Machine.PickerFrontUnit != null &&
                    ((Context.Machine.PickerFrontUnit.Setup != null && Context.Machine.PickerFrontUnit.Setup.IsSimulationMode) ||
                     (Context.Machine.PickerFrontUnit.Config != null && Context.Machine.PickerFrontUnit.Config.IsSimulationMode)))
                    return true;

                return false;
            }
            catch (System.Exception ex)
            {
                WriteLog("ShouldSimulateVisionResult", "FrontPicker Vision 시뮬레이션 조건 확인 실패: " + ex.Message + " - Failed");
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
                    "side=Front").ConfigureAwait(false);
                if (result != 0)
                    throw new System.InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "FrontPicker 수동/스텝 시퀀스 실패. result=" + result,
                            "FrontPicker",
                            "FRONT-PICKER-STEP"));

                if (_stepSequence.IsComplete)
                    _stepSequence = null;
            }
            catch (System.OperationCanceledException)
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
                    Context.StopIfCycleStopRequested("FrontPickerSequence.WaitForWork");

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
            catch (System.OperationCanceledException)
            {
                WriteLog("WaitForPickerWorkAsync", "FrontPicker 작업 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                WriteLog("WaitForPickerWorkAsync", "FrontPicker 작업 대기 중 예외 발생: " + ex.Message + " - Failed");
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
                    "FrontPickerSequence.WaitLoaderInactive",
                    HasLoadedDieOnPicker(),
                    "FrontPicker loaded die drain");

                // 현재 기준: Input/Output 로더 동작 중에는 Picker가 Avoid에서 신규 공정 진입을 기다린다.
                await EnsureIdlePickerAvoidAsync(ct).ConfigureAwait(false);
                if (!waitLogged)
                {
                    WriteLog("WaitForLoaderInactiveBeforePickerProcessAsync",
                        "FrontPicker 신규 공정 대기: Input/Output 로더가 동작 중입니다. - Wait");
                    waitLogged = true;
                }

                await Task.Delay(100, ct).ConfigureAwait(false);
            }

            if (waitLogged)
            {
                WriteLog("WaitForLoaderInactiveBeforePickerProcessAsync",
                    "FrontPicker 신규 공정 대기 해제: Input/Output 로더 동작 종료. - Ok");
            }
        }

        private void EnsureLoaderInactiveForManualStep()
        {
            if (!IsInputOrOutputLoaderActive())
                return;

            throw new InvalidOperationException("Input/Output 로더 동작 중이므로 FrontPicker 수동/스텝 공정 진입이 차단되었습니다.");
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
                ThrowIfAlarmStopActive(ct, "FrontPicker Idle Avoid 시작 전");

                if (Mode != SequenceRunMode.Auto || HasLoadedDieOnPicker() || !IsFrontPickerEnabled())
                    return;

                PickerFrontUnit front = Context != null && Context.Machine != null
                    ? Context.Machine.PickerFrontUnit
                    : null;
                if (front == null || CanSkipIdlePickerAvoidMove(front))
                    return;

                string safeReason;
                if (!CanMoveIdlePickerToAvoid(front, out safeReason))
                {
                    WriteLog("EnsureIdlePickerAvoidAsync",
                        "FrontPicker 작업 대기 중 Idle Avoid 이동을 생략합니다. " +
                        "현재 기준: 실제 Picker가 Process 존이거나 phase/workArea가 남아 있으면 자동 Avoid 이동하지 않습니다. " +
                        "reason=" + safeReason + " - Check");
                    return;
                }

                using (SequenceResourceLease pickerLease = await Context.Resources
                    .AcquireAsync(SequenceResourceKind.FrontPicker, "FrontPicker:IdleAvoid", 200, ct, false)
                    .ConfigureAwait(false))
                {
                    if (pickerLease == null)
                        return;

                    if (CanSkipIdlePickerAvoidMove(front))
                        return;

                    if (!CanMoveIdlePickerToAvoid(front, out safeReason))
                    {
                        WriteLog("EnsureIdlePickerAvoidAsync",
                            "FrontPicker Idle Avoid 이동 직전 조건이 바뀌어 이동을 생략합니다. " +
                            "reason=" + safeReason + " - Check");
                        return;
                    }

                    ThrowIfAlarmStopActive(ct, "FrontPicker Idle Avoid 이동 직전");
                    WriteLog("EnsureIdlePickerAvoidAsync", "FrontPicker 작업 대기 중이므로 Avoid 위치로 이동합니다. - Start");
                    int result = await front.MoveToFrontPickerAvoidPosition(false).ConfigureAwait(false);
                    if (result != 0 || !front.IsFrontPickerInAvoidPosition())
                    {
                        ThrowIfAlarmStopActive(ct, "FrontPicker Idle Avoid 이동 중");
                        throw new InvalidOperationException(
                            "FrontPicker 작업 대기 중 Avoid 이동 실패. result=" + result +
                            ", finalAvoid=" + front.IsFrontPickerInAvoidPosition());
                    }
                }

                WriteLog("EnsureIdlePickerAvoidAsync", "FrontPicker 작업 대기 중 Avoid 위치 이동 완료. - Ok");
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
                WriteLog("EnsureIdlePickerAvoidAsync", "FrontPicker 작업 대기 중 Avoid 이동 예외 발생: " + ex.Message + " - Failed");
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

        private static bool CanSkipIdlePickerAvoidMove(PickerFrontUnit front)
        {
            if (front == null)
                return false;

            foreach (var pair in front.Axes)
            {
                if (pair.Value == null)
                    return false;

                pair.Value.UpdateStatus();
                double target = front.GetPickerTeachingPosition(pair.Key, "AvoidPosition");
                if (!pair.Value.IsAtTargetPosition(target, 0.0))
                    return false;
            }

            return true;
        }

        private bool CanMoveIdlePickerToAvoid(PickerFrontUnit front, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (front == null)
                {
                    reason = "FrontPickerUnit=null";
                    return false;
                }

                if (Context != null && Context.PickerPhases != null)
                {
                    PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                    if (snapshot.Front.Phase != PickerProcessPhase.Idle)
                    {
                        reason = "FrontPicker phase가 Idle이 아닙니다. phase=" + snapshot.Front;
                        return false;
                    }
                }

                PickerWorkZone workZone;
                string owner;
                if (PickerZoneInterlockRules.TryGetPickerWorkArea(true, out workZone, out owner))
                {
                    reason = "FrontPicker workArea가 남아 있습니다. zone=" + workZone + ", owner=" + owner;
                    return false;
                }

                PickerWorkZone xZone = PickerZoneInterlockRules.GetPickerCurrentXZone(
                    Context != null ? Context.Machine : null,
                    true);
                if (PickerZoneInterlockRules.IsProcessZone(xZone))
                {
                    reason = "FrontPickerX가 Process 존에 있습니다. xZone=" + xZone;
                    return false;
                }

                if (xZone == PickerWorkZone.Unknown &&
                    !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                {
                    reason = "FrontPickerX 존을 확정할 수 없고 FrontPickerY가 Avoid가 아닙니다.";
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

                if (!IsFrontPickerEnabled())
                    return false;

                if (!HasEnabledFrontPickerHead())
                {
                    ClearDisabledPickerReservations();
                    return false;
                }

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");

                if (!inputStageReady)
                    return false;

                // 전역 target 존재 여부가 아니라 "이 side가 실제 처리 가능한" target(미예약 또는 Front 예약)으로 판정한다.
                // 전역 판정은 상대 픽커 예약 die에도 true를 반환해 빈 PickerProcess 무한 재진입(busy loop)을 유발했다.
                return MaterialStateService.HasActionableInputStagePickTarget(MaterialLocationKind.PickerFront);
            }
            catch (System.Exception ex)
            {
                WriteLog("HasPickerWork", "FrontPicker 작업 조건 확인 실패: " + ex.Message + " - Failed");
                return false;
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

                PickerFrontUnit front = Context.Machine != null ? Context.Machine.PickerFrontUnit : null;
                if (!IsFrontPickerEnabled() || front == null || !front.IsFrontPickerInAvoidPosition())
                    return;

                Context.Bus.Set("InputStageDieComplete");
                WriteLog("InputStageExchangeReady",
                    "FrontPicker가 InputStage Pick 대상 없음과 자기 Picker 전체 Avoid를 확인하고 교체 준비 신호를 발행했습니다. " +
                    "signal=InputStageDieComplete - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputStageExchangeReady",
                    "FrontPicker InputStage 교체 준비 신호 판단 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
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
            catch (System.Exception ex)
            {
                WriteLog("IsFrontPickerEnabled", "FrontPicker 사용 여부 확인 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool HasEnabledFrontPickerHead()
        {
            try
            {
                PickerFrontUnit front = Context != null && Context.Machine != null
                    ? Context.Machine.PickerFrontUnit
                    : null;
                bool[] usePicker = front != null && front.Config != null ? front.Config.UsePicker : null;
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
                WriteLog("HasEnabledFrontPickerHead", "FrontPicker 사용 Head 확인 실패: " + ex.Message + " - Failed");
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
                InputCameraPickUpPermissionStore.Clear(PickerSequenceSide.Front);
                int releaseCount = MaterialStateService.ReleaseInputStagePickReservationsForPickerLocation(
                    MaterialLocationKind.PickerFront,
                    "FrontPicker 사용 가능한 Head가 없습니다.");
                if (releaseCount > 0)
                    WriteLog("ClearDisabledPickerReservations",
                        "FrontPicker 사용 가능한 Head가 없어 남은 Pick 예약을 해제했습니다. count=" + releaseCount + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ClearDisabledPickerReservations",
                    "FrontPicker 사용 불가 예약 해제 중 예외 발생: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool HasLoadedDieOnPicker()
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
                System.Diagnostics.Debug.WriteLine("FrontPickerSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.FrontHeadSeq, source, message);
        }
    }
}
