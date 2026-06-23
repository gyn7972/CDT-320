using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class RearPickerSequence : UnitSequenceBase
    {
        private static readonly TimeSpan FrontPickupYieldLogInterval = TimeSpan.FromSeconds(2);
        private PickerProcessSequence _stepSequence;
        private DateTime _lastFrontPickupYieldLogTime = DateTime.MinValue;

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
                    if (await YieldInputPickupPriorityToFrontAsync(ct).ConfigureAwait(false))
                        continue;

                    PickerSequenceOptions options = BuildSequenceOptions();
                    int result = await new PickerProcessSequence(Context, PickerSequenceSide.Rear)
                        .RunAsync(ct, options)
                        .ConfigureAwait(false);
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
                int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
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
                    await Task.Delay(200, ct).ConfigureAwait(false);
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

        private async Task EnsureIdlePickerAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Mode != SequenceRunMode.Auto || HasLoadedDieOnPicker() || !IsRearPickerEnabled())
                    return;

                PickerRearUnit rear = Context != null && Context.Machine != null
                    ? Context.Machine.PickerRearUnit
                    : null;
                if (rear == null || rear.IsRearPickerInAvoidPosition())
                    return;

                WriteLog("EnsureIdlePickerAvoidAsync", "RearPicker 작업 대기 중이므로 Avoid 위치로 이동합니다. - Start");
                int result = await rear.MoveToRearPickerAvoidPosition(false).ConfigureAwait(false);
                if (result != 0 || !rear.IsRearPickerInAvoidPosition())
                    throw new InvalidOperationException(
                        "RearPicker 작업 대기 중 Avoid 이동 실패. result=" + result +
                        ", finalAvoid=" + rear.IsRearPickerInAvoidPosition());

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

        private bool HasPickerWork()
        {
            try
            {
                if (HasLoadedDieOnPicker())
                    return true;

                if (!IsRearPickerEnabled())
                    return false;

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");

                if (!inputStageReady)
                    return false;

                return MaterialStateService.HasReadyInputStagePickTarget();
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

        private async Task<bool> YieldInputPickupPriorityToFrontAsync(CancellationToken ct)
        {
            try
            {
                if (Mode != SequenceRunMode.Auto)
                    return false;

                if (HasLoadedDieOnPicker())
                    return false;

                if (!IsFrontPickerEnabled())
                    return false;

                if (HasLoadedDieOnFrontPicker())
                    return false;

                bool inputStageReady = Context != null &&
                                       Context.Bus != null &&
                                       Context.Bus.IsSet("InputStageReady");
                if (!inputStageReady || !MaterialStateService.HasReadyInputStagePickTarget())
                    return false;

                WriteFrontPickupYieldWaitLog();

                await Task.Delay(300, ct).ConfigureAwait(false);
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

        private void WriteFrontPickupYieldWaitLog()
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now - _lastFrontPickupYieldLogTime < FrontPickupYieldLogInterval)
                    return;

                _lastFrontPickupYieldLogTime = now;
                WriteLog("YieldInputPickupPriorityToFrontAsync",
                    "RearPicker가 FrontPicker PickUp 우선권을 위해 대기합니다. " +
                    "FrontPicker가 비어 있고 InputStage에 Pick 대상이 남아 있습니다. - Wait");
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
