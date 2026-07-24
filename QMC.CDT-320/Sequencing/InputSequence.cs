using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    // Input 자동 시퀀스의 큰 흐름:
    // Mapping -> 작업 슬롯 결정 -> Stage 로드 준비 -> Cassette에서 Feeder로 로드
    // -> Feeder에서 Stage로 로드 -> Feeder 복귀 -> Stage Align -> Die Mapping -> 사용자 확인 -> Picker 작업 대기.
    internal enum InputSequenceAutoStep
    {
        // Input Cassette의 wafer map을 먼저 갱신한다.
        Mapping,
        // 현재 Processing 슬롯이 있으면 이어서 사용하고, 없으면 다음 Ready 슬롯을 찾는다.
        ResolveSlot,
        // InputStage가 wafer를 받을 수 있도록 위치/상태를 준비한다.
        PrepareStageLoad,
        // 결정된 cassette slot에서 InputFeeder로 wafer를 꺼낸다.
        LoadFeederFromCassette,
        // InputFeeder가 들고 있는 wafer를 InputStage로 이송한다.
        LoadFeederToStage,
        // Stage 로드 후 InputFeeder를 안전 위치/대기 상태로 복귀시킨다.
        RecoverFeeder,
        // InputStage에서 wafer align을 수행한다.
        AlignStage,
        // Align 결과를 기준으로 die map을 생성한다.
        DieMapping,
        // Align/Die Mapping 결과를 작업자가 확인할 때까지 Picker Ready 발행을 보류한다.
        ReviewStage,
        // Stage가 Picker PickUp 가능한 상태까지 준비된 상태이다.
        Complete
    }

    public class InputSequence : UnitSequenceBase
    {
        private const string InputLoaderActiveSignal = "InputLoaderActive";
        private const string InputStageAlignSequenceStateName = "InputStageSequence.Align";
        private const string InputStageDieMappingSequenceStateName = "InputStageSequence.DieMapping";
        // 현재 자동/스텝 실행 위치. 장비 상태 복원 시 Runtime Material 위치를 보고 재설정된다.
        private InputSequenceAutoStep _autoStep = InputSequenceAutoStep.Mapping;
        // 현재 처리 중인 Input Cassette slot index. -1이면 아직 slot이 확정되지 않은 상태이다.
        private int _autoSlotIndex = -1;
        // To do: C4 - 현재 처리 중인 Input Cassette 레벨(1단=Input1 / 2단=Input2). 1단 소진 후 2단으로 전환된다.
        private CassetteMaterialRole _autoCassetteRole = CassetteMaterialRole.Input1;
        // 로그와 Stage option 전달용 wafer id. 기본 규칙은 INPUT-SLOT-xx이다.
        private string _autoWaferId = "";
        // Input loader active signal 중복 Set/Reset을 막기 위한 상태입니다.
        private bool _inputLoaderActivePublished;
        // 리뷰 취소 후 Align을 저장 재개점이 아닌 최초 단계부터 다시 실행하기 위한 1회성 플래그입니다.
        private bool _restartAlignFromReview;
        // 리뷰에서 Mapping 재실행을 선택한 경우 저장 재개점이 아닌 최초 단계부터 실행하기 위한 1회성 플래그입니다.
        private bool _restartDieMappingFromReview;
        // 재개 시 Feeder에 남은 "이미 스테이지를 거친(언로드 진행 중)" wafer를 Stage로 전진시키지 않고
        // 원래 Cassette로 되돌리기 위한 1회성 플래그입니다. (언로드 중단 후 재시작의 역주행 방지)
        private bool _resumeFeederUnloadToCassette;

        public InputSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "Input")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            try
            {
                // Auto 모드는 한 wafer cycle을 끝낼 때마다 CycleStop 요청을 확인하고,
                // 정지 요청이 없으면 다음 Ready wafer cycle로 반복 진입한다.
                while (!ct.IsCancellationRequested)
                {
                    if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                        "InputSequence.AutoLoop",
                        ct).ConfigureAwait(false))
                    {
                        return;
                    }

                    await ExecuteInputAutoCycleAsync(ct).ConfigureAwait(false);
                    Context.StopIfCycleStopRequested("InputSequence.AutoCycleComplete");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoAsync", "Input 자동 시퀀스가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-EX", "InputSequence", "Input 자동 시퀀스 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task ExecuteInputAutoCycleAsync(CancellationToken ct)
        {
            try
            {
                if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                    "InputSequence.AutoCycleStart",
                    ct).ConfigureAwait(false))
                {
                    return;
                }

                // 이전 실행 중 Stage/Feeder에 남은 wafer가 있으면 해당 위치부터 재개한다.
                RestoreInputStepSessionFromRuntimeState();

                // 언로드(Stage->Feeder->Cassette) 도중 중단 후 재개: Feeder에 남은 완료 wafer를 Stage로 전진시키지 않고
                // 원래 Cassette로 되돌리는 언로드를 먼저 마친 뒤 다음 slot부터 정상 cycle을 재개한다.
                if (_resumeFeederUnloadToCassette)
                {
                    _resumeFeederUnloadToCassette = false;
                    using (AutoSequenceLoaderWorkLease resumeUnloadLease = await Context.AutoLoaderGate
                        .BeginInputWorkAsync(
                            "InputResumeFeederUnloadCycle",
                            ct,
                            EnsureInputPickersEmptyAvoidAndStoppedAsync,
                            AreInputPickersEmptyAvoidAndStopped)
                        .ConfigureAwait(false))
                    {
                        await ExecuteResumeFeederUnloadToCassetteAsync(ct).ConfigureAwait(false);
                    }

                    ResetInputAutoCycle();
                    Context.StopIfCycleStopRequested("InputSequence.AfterResumeFeederUnload");
                    return;
                }

                bool readySignalPublishedFromRestore = TryPublishRestoredInputStageReadySignals();

                WaferMaterial stageWafer = null;

                // Mapping부터 DieMapping까지 한 번 승인된 Input loader 작업으로 완료한다.
                if (_autoStep != InputSequenceAutoStep.Complete || !readySignalPublishedFromRestore)
                {
                    using (AutoSequenceLoaderWorkLease loaderLease = await Context.AutoLoaderGate
                        .BeginInputWorkAsync(
                            "InputStageReadyCycle",
                            ct,
                            EnsureInputPickersEmptyAvoidAndStoppedAsync,
                            AreInputPickersEmptyAvoidAndStopped)
                        .ConfigureAwait(false))
                    {
                        await ExecuteInputLoadingStepsUntilStageReadyAsync(ct).ConfigureAwait(false);

                        // Complete 복구 판정과 누락 step 재실행도 InputLoaderActive lease 안에서만 수행한다.
                        stageWafer = ResolveStageWaferFromRuntimeState();
                        if (stageWafer != null)
                        {
                            stageWafer = await EnsureInputStageFinishBeforePickerReadyAsync(
                                stageWafer,
                                ct).ConfigureAwait(false);
                        }
                    }
                }

                // Stage에 wafer가 없으면 아직 다음 cycle을 진행할 조건이 아니므로 짧게 대기 후 반환한다.
                if (stageWafer == null)
                    stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer == null)
                {
                    Context.StopIfCycleStopRequested("InputSequence.WaitStageWafer");
                    await Task.Delay(100, ct).ConfigureAwait(false);
                    return;
                }

                // Loader lease 밖에서는 복구 동작을 시작하지 않고 최종 완료 상태만 fail-closed로 확인한다.
                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException(
                        "InputLoader gate 종료 후 InputStage PickUp 준비 상태가 유효하지 않습니다. " + finishReason);

                // Picker 쪽에서 볼 수 있는 ready bus를 올린 뒤 die pick 완료를 기다린다.
                if (!readySignalPublishedFromRestore ||
                    Context == null ||
                    Context.Bus == null ||
                    !Context.Bus.IsSet("InputStageReady"))
                {
                    PublishInputStageReadySignals(stageWafer);
                }

                await WaitPickerToCompleteInputStageDiesAsync(
                    stageWafer,
                    readySignalPublishedFromRestore,
                    ct).ConfigureAwait(false);

                if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                    "InputSequence.BeforeStageUnload",
                    ct).ConfigureAwait(false))
                {
                    return;
                }

                // Picker가 해당 Stage wafer의 die pick을 완료하면 별도 승인된 Input loader 작업으로 Stage wafer를 cassette로 되돌린다.
                using (AutoSequenceLoaderWorkLease unloadLease = await Context.AutoLoaderGate
                    .BeginInputWorkAsync(
                        "InputStageUnloadCycle",
                        ct,
                        EnsureInputPickersEmptyAvoidAndStoppedAsync,
                        AreInputPickersEmptyAvoidAndStopped)
                    .ConfigureAwait(false))
                {
                    await UnloadInputStageWaferIfPresentAsync(ct).ConfigureAwait(false);
                }

                // 모든 Input Cassette slot 처리가 끝났으면 알람/메시지를 띄우고 Auto를 정지한다.
                int completeResult = StopAutoSequenceIfInputCassetteComplete();
                if (completeResult != 0)
                    return;

                // 다음 wafer cycle은 Mapping을 다시 하지 않고 다음 slot 탐색부터 시작한다.
                ResetInputAutoCycle();
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteInputAutoCycleAsync", "Input 자동 사이클이 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-CYCLE", "InputSequence", "Input 자동 사이클 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private async Task<WaferMaterial> EnsureInputStageFinishBeforePickerReadyAsync(WaferMaterial stageWafer, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stageWafer == null)
                    return null;

                string finishReason;
                if (MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    return stageWafer;

                // Stage에는 wafer가 있는데 finish 조건이 아니면 Align/DieMapping 중 부족한 step을 계산한다.
                InputSequenceAutoStep resumeStep = ResolveStageWaferResumeStep(stageWafer);
                if (resumeStep == InputSequenceAutoStep.Complete)
                    resumeStep = InputSequenceAutoStep.DieMapping;

                _autoSlotIndex = ResolveSlotIndexFromWafer(stageWafer);
                _autoWaferId = stageWafer.WaferId ?? "";
                _autoStep = resumeStep;

                ResetInputStageReadySignals();
                WriteLog("EnsureInputStageFinishBeforePickerReady",
                    "InputStage가 아직 PickUp 준비 완료 상태가 아니어서 누락된 준비 step부터 재개합니다. " +
                    "wafer=" + _autoWaferId +
                    ", slot=" + _autoSlotIndex +
                    ", resumeStep=" + _autoStep +
                    ", reason=" + finishReason + " - Check");

                // 계산된 재개 step부터 다시 실행하여 Stage finish 상태를 보장한다.
                await ExecuteInputLoadingStepsUntilStageReadyAsync(ct).ConfigureAwait(false);

                WaferMaterial refreshedWafer = ResolveStageWaferFromRuntimeState();
                if (refreshedWafer == null)
                    throw new InvalidOperationException("InputStage 준비 재개 후 웨이퍼 Material 정보가 없습니다.");

                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage 준비 재개 후에도 Picker PickUp 가능 상태가 아닙니다. " + finishReason);

                return refreshedWafer;
            }
            catch (OperationCanceledException)
            {
                WriteLog("EnsureInputStageFinishBeforePickerReady",
                    "InputStage PickUp 준비 재확인 중 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STAGE-FINISH-RECOVER", "InputSequence",
                    "InputStage PickUp 준비 복구 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private bool TryPublishRestoredInputStageReadySignals()
        {
            try
            {
                if (_autoStep != InputSequenceAutoStep.Complete)
                    return false;

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer == null)
                    return false;

                string approvalReason;
                if (!MaterialStateService.IsInputStageRunReviewApprovalUsable(
                    stageWafer,
                    out approvalReason))
                {
                    WriteLog("TryPublishRestoredInputStageReadySignals",
                        "Restored InputStage Review approval is not usable. wafer=" +
                        (stageWafer.WaferId ?? "") + ", reason=" + approvalReason + " - Check");
                    return false;
                }

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    return false;

                // 재시작 복구 시 Stage가 이미 완료 상태면 Picker 재개보다 먼저 Ready 신호를 복구한다.
                PublishInputStageReadySignals(stageWafer);
                WriteLog("TryPublishRestoredInputStageReadySignals",
                    "Restored InputStage is already ready for PickUp. InputStageReady was published before InputLoader gate. " +
                    "wafer=" + stageWafer.WaferId +
                    ", slot=" + _autoSlotIndex +
                    ", step=" + _autoStep +
                    " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                WriteLog("TryPublishRestoredInputStageReadySignals",
                    "Restored InputStage ready signal publish skipped/failed: " + ex.Message + " - Check");
                return false;
            }
            finally
            {
            }
        }

        private async Task ExecuteInputLoadingStepsUntilStageReadyAsync(CancellationToken ct)
        {
            // _autoStep이 Complete가 될 때까지 한 step씩 실행한다.
            // 각 step 성공 시 ExecuteCurrentInputStepAsync 내부에서 다음 step으로 전환된다.
            while (_autoStep != InputSequenceAutoStep.Complete)
            {
                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input 자동 시퀀스 실패. step=" + _autoStep + ", result=" + result);
            }
        }

        private async Task WaitPickerToCompleteInputStageDiesAsync(
            WaferMaterial stageWafer,
            bool allowSafeCompletionSignalRecovery,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stageWafer == null)
                    return;

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage Finish 상태가 완료가 아닙니다. " + finishReason);

                if (Context.Bus.IsSet("InputStageDieComplete"))
                {
                    WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                        "Input stage 마지막 Pick 안전 복귀 완료 신호가 이미 발행되어 있습니다. wafer=" +
                        (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
                    return;
                }

                if (TryCompleteInputStageWithNoApprovedPickTargets(stageWafer))
                    return;

                if (allowSafeCompletionSignalRecovery &&
                    TryRestoreInputStageCompletionSignalAfterPickerAvoid(stageWafer))
                    return;

                Context.Bus.Reset("InputStageDieComplete");
                PublishInputStageReadySignals(stageWafer);

                // 정상 운전은 Picker Sequence가 마지막 Pick 안전 복귀 후 완료 신호를 발행한다.
                // Material 상태 기반 완료 신호 복구는 Ready 상태를 복원한 재시작 경로에서만 허용한다.
                while (!ct.IsCancellationRequested)
                {
                    if (await WaitForStopAfterDrainCompletionIfRequestedAsync(
                        "InputSequence.WaitInputStageDieComplete",
                        ct).ConfigureAwait(false))
                    {
                        return;
                    }

                    Context.StopIfCycleStopRequested("InputSequence.WaitInputStageDieComplete");

                    if (Context.Bus.IsSet("InputStageDieComplete"))
                    {
                        WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                            "Input stage die pick complete signal received. wafer=" +
                            (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
                        return;
                    }

                    if (TryCompleteInputStageWithNoApprovedPickTargets(stageWafer))
                        return;

                    if (allowSafeCompletionSignalRecovery &&
                        TryRestoreInputStageCompletionSignalAfterPickerAvoid(stageWafer))
                        return;

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                    "InputStage Die Pick 완료 대기가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-PICK-WAIT", "InputSequence",
                    "InputStage Die Pick 완료 대기 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private bool TryCompleteInputStageWithNoApprovedPickTargets(WaferMaterial stageWafer)
        {
            if (stageWafer == null ||
                !stageWafer.HasInputStageRunReviewApproval ||
                stageWafer.InputStageRunReviewOrderedDieIds == null ||
                stageWafer.InputStageRunReviewOrderedDieIds.Count != 0)
            {
                return false;
            }

            string approvalReason;
            if (!MaterialStateService.IsInputStageRunReviewApprovalUsable(stageWafer, out approvalReason))
                return false;

            string pickerReason;
            if (!AreInputPickersEmptyAvoidAndStopped(out pickerReason))
                return false;

            Context.Bus.Set("InputStageDieComplete");
            WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                "Review 승인 PickUp 대상이 0개이고 Front/Rear Picker Empty/Idle/full Avoid를 확인하여 " +
                "InputStage 완료 신호를 확정했습니다. wafer=" + (stageWafer.WaferId ?? "") + " - Ok");
            return true;
        }

        private async Task<bool> WaitForStopAfterDrainCompletionIfRequestedAsync(
            string boundary,
            CancellationToken ct)
        {
            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            if (!completion.IsDrainRequested)
                return false;

            WriteLog("WaferCompletionRun",
                "Stop After Drain 요청으로 Input 신규 Pick/교체를 중단하고 안전 배출 완료를 기다립니다. " +
                "boundary=" + (boundary ?? "-") + " - Wait");
            await completion.WaitForCompletionAsync(ct).ConfigureAwait(false);
            WriteLog("WaferCompletionRun",
                "Stop After Drain 안전 배출 완료를 확인하여 Input 자동 시퀀스를 종료합니다. " +
                "boundary=" + (boundary ?? "-") + " - Ok");
            return true;
        }

        private bool TryRestoreInputStageCompletionSignalAfterPickerAvoid(WaferMaterial stageWafer)
        {
            if (!MaterialStateService.IsInputStagePickComplete())
                return false;

            string reason;
            if (!AreInputPickersAvoidAndStopped(out reason))
                return false;

            Context.Bus.Set("InputStageDieComplete");
            WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                "InputStage Material Pick 완료 복구 시 Front/Rear Picker 전체 Avoid 및 정지를 확인한 후 완료 신호를 복구했습니다. wafer=" +
                (stageWafer != null ? stageWafer.WaferId : "-") + " - Ok");
            return true;
        }

        private async Task UnloadInputStageWaferIfPresentAsync(CancellationToken ct)
        {
            try
            {
                // Stage를 움직이기 전에 Picker가 다시 접근하지 못하도록 ready 신호를 먼저 내린다.
                ResetInputStageReadySignals();

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer == null)
                    return;

                // Source slot 정보를 기준으로 Stage -> Feeder -> Cassette 언로딩을 수행한다.
                int slotIndex = ResolveSlotIndexFromWafer(stageWafer);
                int result = await ExecuteWaferUnloadingAsync(
                    ct,
                    slotIndex,
                    false,
                    0,
                    SequenceStartMode.Resume).ConfigureAwait(false);

                if (result != 0)
                    throw new InvalidOperationException("InputStage 웨이퍼 자동 언로딩 실패. slot=" + slotIndex + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("UnloadInputStageWaferIfPresentAsync", "InputStage 웨이퍼 언로딩이 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-UNLOAD", "InputSequence", "Input 자동 웨이퍼 언로딩 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        // 재개 방향 판별: Feeder에 있는 wafer가 이미 InputStage Align을 마친(=스테이지를 거친) wafer이면
        // Cassette->Feeder 로딩 중이 아니라 Stage->Feeder->Cassette 언로딩 진행 중으로 판단한다.
        // 새 wafer 로딩 경로는 ResetInputStageWaferProcessingState로 Align 결과가 초기화되므로, 로드 방향 Feeder wafer는 Align 결과가 없다.
        private static bool IsFeederWaferMidUnload(WaferMaterial feederWafer)
        {
            return feederWafer != null && feederWafer.HasInputStageAlignResult;
        }

        // Stage 모션(Align/DieMapping 등) 전에 InputFeeder가 안전하게 후퇴(Avoid)되어 정지해 있는지 확인한다.
        // 실장비는 X090 Dog(Picker/Stage 간섭 안전), 순수 시뮬은 엔코더 위치로 판정한다.
        // 확인할 수 없으면(피더 참조/축 없음, 이동 중) 안전측으로 '후퇴 안 됨'(false)으로 본다.
        private bool IsInputFeederRetractedForStageMotion()
        {
            try
            {
                var feeder = Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null;
                if (feeder == null || feeder.FeederY == null)
                    return false;
                if (feeder.FeederY.IsMoving)
                    return false;
                return feeder.IsWaferFeederAvoidPositionCheck();
            }
            catch (Exception ex)
            {
                WriteLog("IsInputFeederRetractedForStageMotion",
                    "InputFeeder 후퇴 상태 확인 실패. 안전측으로 미후퇴 처리합니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        // 언로드(Stage->Feeder->Cassette) 도중 중단되어 Feeder에 남은 완료 wafer를,
        // Stage로 전진시키지 않고 원래 Cassette로 되돌리는 언로드 잔여 구간(Feeder->Cassette, Stage Avoid 복귀)을 이어서 수행한다.
        // 정상 언로드 경로(ExecuteWaferUnloadingAsync)와 동일한 Picker Avoid 게이트/InputStageArea 락을 사용한다.
        private async Task ExecuteResumeFeederUnloadToCassetteAsync(CancellationToken ct)
        {
            bool loaderActive = true;
            try
            {
                ct.ThrowIfCancellationRequested();

                // 이송 중에는 Picker가 Stage에 접근하지 못하도록 ready 신호를 먼저 내린다.
                ResetInputStageReadySignals();

                WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
                if (feederWafer == null)
                {
                    // 재개 판정 이후 Feeder가 비었으면(이미 처리됨) 추가 동작 없이 종료한다.
                    WriteLog("ExecuteResumeFeederUnloadToCassette",
                        "Feeder에 남은 언로드 대상 wafer가 없어 재개 언로드를 건너뜁니다. - Ok");
                    return;
                }

                int slotIndex = ResolveSlotIndexFromWafer(feederWafer);
                if (slotIndex < 0)
                    throw new InvalidOperationException(
                        "Feeder 잔류 언로드 재개 대상 슬롯을 확인할 수 없습니다. wafer=" + (feederWafer.WaferId ?? ""));

                LogPublic("[UNIT-INPUT] Resume feeder unloading to cassette. slot=" + slotIndex);
                WriteLog("ExecuteResumeFeederUnloadToCassette",
                    "Feeder 잔류 완료 wafer의 Cassette 복귀 재개 시작. slot=" + slotIndex +
                    ", wafer=" + (feederWafer.WaferId ?? "") + " - Start");
                SetInputLoaderActive(loaderActive, "ResumeFeederUnloadToCassette");

                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions =
                    BuildFeederSequenceOptions(slotIndex, slotIndex, false, 0, SequenceStartMode.Resume);

                // Feeder -> Cassette 복귀 구간.
                int result = await ExecuteWithInputPickerAvoidGateAsync("InputResumeUnloadToCassette", ct, () =>
                    SequenceTrace.ChildAsync("InputFeederSequence", "UnloadToCassette",
                        () => feederSequence.RunUnloadToCassetteAsync(ct, feederOptions),
                        "slot=" + slotIndex)).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException(
                        "Feeder 잔류 wafer의 카세트 복귀 재개 실패. slot=" + slotIndex + ", result=" + result);

                // 빈 InputStage를 Avoid로 복귀시킨다.
                result = await ExecuteWithInputPickerAvoidGateAsync("InputResumeStageMoveAvoid", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputResumeStageMoveAvoid", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence",
                                "Feeder 잔류 언로드 재개 후 InputStage Avoid 복귀 중 InputStageArea 리소스 점유에 실패했습니다.");

                        var stageSequence = new InputStageSequence(Context);
                        int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "MoveAvoidAfterUnload",
                            () => stageSequence.RunMoveAvoidAsync(ct, BuildStageSequenceOptions(false, SequenceStartMode.Resume, false, ResolveInputWaferId(slotIndex), false)),
                            "slot=" + slotIndex).ConfigureAwait(false);
                        if (stageResult != 0)
                            return Fail("SEQ-IN-STAGE-AVOID", "InputStage",
                                "Feeder 잔류 언로드 재개 후 InputStage Avoid 복귀 실패. result=" + stageResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException(
                        "Feeder 잔류 언로드 재개 후 InputStage Avoid 복귀 실패. slot=" + slotIndex + ", result=" + result);

                // slot을 Done으로 표시하고 Stage runtime을 비워 다음 cycle과 섞이지 않게 한다.
                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Done);
                ClearInputStageRuntime();
                Context.Bus.Set("InputWaferUnloaded");
                LogPublic("[UNIT-INPUT] Resume feeder unloading complete. slot=" + slotIndex);
                WriteLog("ExecuteResumeFeederUnloadToCassette",
                    "Feeder 잔류 완료 wafer를 원래 Cassette로 안전 복귀했습니다(재개). slot=" + slotIndex +
                    ", wafer=" + (feederWafer.WaferId ?? "") + " - Ok");
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteResumeFeederUnloadToCassette", "Feeder 잔류 언로드 재개가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-RESUME-UNLOAD-EX", "InputSequence", "Feeder 잔류 언로드 재개 실패: " + ex.Message);
                throw;
            }
            finally
            {
                ResetInputLoaderActive(loaderActive, "ResumeFeederUnloadToCassette");
            }
        }

        private void ResetInputAutoCycle()
        {
            // 한 wafer cycle 완료 후 Picker/Stage 완료 신호와 slot 세션 정보를 초기화한다.
            ResetInputStageCycleSignals();
            _autoSlotIndex = -1;
            _autoWaferId = "";
            _restartAlignFromReview = false;
            _restartDieMappingFromReview = false;
            _resumeFeederUnloadToCassette = false;
            _autoStep = InputSequenceAutoStep.ResolveSlot;
        }

        private void PublishInputStageReadySignals(WaferMaterial stageWafer)
        {
            try
            {
                if (stageWafer == null)
                    throw new InvalidOperationException("InputStage 웨이퍼 Material 정보가 없습니다.");

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    throw new InvalidOperationException("InputStage가 Picker PickUp 가능 상태가 아닙니다. " + finishReason);

                // Picker Sequence가 InputStage 작업 가능 여부를 판단하는 주요 bus 신호들이다.
                Context.Bus.Set("InputWaferLoaded");
                Context.Bus.Set("InputStageDieMapped");
                Context.Bus.Set("InputStageFinishComplete");
                Context.Bus.Set("InputStageReady");

                int fullDieCount = stageWafer.DieIds != null ? stageWafer.DieIds.Count : 0;
                int targetDieCount = CountInputStageTargetDies(stageWafer);

                WriteLog("PublishInputStageReadySignals",
                    "Input stage ready signals published. wafer=" +
                    stageWafer.WaferId +
                    ", slot=" + stageWafer.SourceSlotNumber +
                    ", targetDieCount=" + targetDieCount +
                    ", fullDieCount=" + fullDieCount +
                    " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PublishInputStageReadySignals",
                    "Input stage ready signal publish failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private static int CountInputStageTargetDies(WaferMaterial stageWafer)
        {
            try
            {
                if (stageWafer == null || stageWafer.DieIds == null)
                    return 0;

                int count = 0;
                foreach (string dieId in stageWafer.DieIds)
                {
                    DieMaterial die = MaterialStateService.GetDieMaterial(dieId);
                    if (die != null &&
                        die.IsInputTarget &&
                        string.Equals(die.WaferID_Input, stageWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return stageWafer != null && stageWafer.DieIds != null ? stageWafer.DieIds.Count : 0;
            }
            finally
            {
            }
        }

        private void ResetInputStageReadySignals()
        {
            try
            {
                // Feeder/Stage 이송 중에는 Picker가 Stage에 접근하지 않도록 ready 신호만 우선 차단한다.
                Context.Bus.Reset("InputStageReady");
                WriteLog("ResetInputStageReadySignals",
                    "Input stage ready signal reset. Picker pickup is blocked for wafer transfer. - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ResetInputStageReadySignals",
                    "Input stage ready signal reset failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private void ResetInputStageCycleSignals()
        {
            try
            {
                // 다음 wafer cycle과 신호가 섞이지 않도록 Stage 관련 완료/ready bus를 모두 내린다.
                Context.Bus.Reset("InputStageDieComplete");
                Context.Bus.Reset("InputStageReady");
                Context.Bus.Reset("InputStageDieMapped");
                Context.Bus.Reset("InputStageFinishComplete");
                WriteLog("ResetInputStageCycleSignals", "Input stage cycle signals reset. - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("ResetInputStageCycleSignals",
                    "Input stage cycle signal reset failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private int StopAutoSequenceIfInputCassetteComplete()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null
                    ? Context.Machine.InputCassetteUnit
                    : null;
                if (cassette == null)
                    return 0;

                if (!cassette.IsInputCassetteProcessComplete())
                    return 0;

                cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                NotifyInputCassetteReplacementRequired();
                return StopAutoSequence("입력 카세트의 모든 웨이퍼 작업이 완료되었습니다. 카세트를 교체하세요.");
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-COMPLETE-CHECK", "InputSequence",
                    "입력 카세트 완료 상태 확인 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private int StopInputNoReadyWafer()
        {
            try
            {
                int completeResult = StopAutoSequenceIfInputCassetteComplete();
                if (completeResult != 0)
                    return completeResult;

                string reason = "입력 카세트에서 작업 가능한 Ready 웨이퍼 슬롯을 찾을 수 없습니다. 카세트 매핑 상태와 슬롯의 Process 상태를 확인하세요.";
                var cassette = Context != null && Context.Machine != null
                    ? Context.Machine.InputCassetteUnit
                    : null;
                if (cassette != null)
                    reason += " detail=" + cassette.BuildProcessWaferAvailabilitySummary();
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-IN-NO-READY-WAFER", "InputSequence", reason);
                return StopAutoSequence(reason);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-NO-READY-WAFER-CHECK", "InputSequence",
                    "입력 카세트 Ready 웨이퍼 확인 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            try
            {
                // Step 실행에서 Complete 상태로 남아 있으면 실제 Runtime 위치를 보고 재개 위치를 다시 판단한다.
                if (_autoStep == InputSequenceAutoStep.Complete)
                    RestoreInputStepSessionFromRuntimeState();

                // 수동 Step은 현재 _autoStep 한 단계만 실행하고 다음 step으로 이동한다.
                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    throw new InvalidOperationException("Input 수동/스텝 시퀀스 실패. step=" + _autoStep + ", result=" + result);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteStepAsync", "Input step sequence canceled. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STEP-EX", "InputSequence", "Input 수동/스텝 시퀀스 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void RestoreInputStepSessionFromRuntimeState()
        {
            try
            {
                _autoSlotIndex = -1;
                _autoWaferId = "";

                // Todo: GYN 2026.07.03 - 여기서 순번대로 재개할때 항상 인터락 확인 후에 재개하도록 해야 한다. (Feeder/Stage/Picker)
                // 재개 Step시에 필요한 인터락 / 안전 상태 확인 후에 작업을 재개하는데 만약 안전 상태로 모션이 가능하면
                // 안전상태로 모션 시키고 재개하고 그렇지 않으면 알람 발생 후 장비를 멈춘다.

                // 1순위: Stage에 wafer가 있으면 Stage 처리 상태(Align/DieMapping/Complete)를 기준으로 재개한다.
                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(stageWafer);
                    _autoWaferId = stageWafer.WaferId ?? "";
                    RestoreActiveInputWaferSourceSlotProjection(stageWafer, "InputStageRestore");
                    _autoStep = ResolveStageWaferResumeStep(stageWafer);

                    // 재개 안전(Align-into-Feeder 충돌 방지):
                    // LoadFeederToStage는 자재를 스테이지로 옮긴 뒤(MoveMaterialDataToStage) 피더를 Avoid로 후퇴시킨다.
                    // 후퇴 꼬리(LiftUp/Avoid) 도중 정지하면 자재는 스테이지에 있으나 피더가 아직 전진 상태로 남는다.
                    // 이 상태에서 AlignStage로 바로 진입하면 Stage Y/T/Camera 이동이 후퇴하지 않은 피더와 충돌하므로,
                    // 피더가 Avoid 안전 위치가 아니면 RecoverFeeder(피더 안전 후퇴)부터 재개하도록 보정한다.
                    if (_autoStep == InputSequenceAutoStep.AlignStage &&
                        !IsInputFeederRetractedForStageMotion())
                    {
                        _autoStep = InputSequenceAutoStep.RecoverFeeder;
                        WriteLog("RestoreInputStepSession",
                            "InputStage wafer 재개 시 InputFeeder가 Avoid 후퇴 상태가 아니어서 RecoverFeeder부터 재개합니다. " +
                            "(Align 전 피더 안전 후퇴, Align-into-Feeder 충돌 방지) wafer=" + _autoWaferId +
                            ", slot=" + _autoSlotIndex +
                            ", positions=" + BuildAutoResumePositionSummary() +
                            " - Check");
                    }

                    WriteLog("RestoreInputStepSession",
                        "Input sequence restored from InputStage wafer. wafer=" + _autoWaferId +
                        ", slot=" + _autoSlotIndex +
                        ", step=" + _autoStep +
                        ", positions=" + BuildAutoResumePositionSummary() +
                        " - Ok");
                    return;
                }

                // 2순위: Feeder에 wafer가 있으면 이송 방향을 판별해 재개한다.
                //  - 로드(Cassette->Feeder->Stage) 진행 중: 아직 스테이지를 거치지 않아 Align 결과가 없다 -> Stage로 전진.
                //  - 언로드(Stage->Feeder->Cassette) 진행 중: 이미 스테이지에서 처리(Align 완료)된 wafer가 되돌아오는 중이다
                //    -> Stage로 전진시키면 완료 wafer를 재처리/역주행하므로, 원래 Cassette로 되돌리는 언로드를 이어서 수행한다.
                WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
                if (feederWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(feederWafer);
                    _autoWaferId = feederWafer.WaferId ?? "";
                    RestoreActiveInputWaferSourceSlotProjection(feederWafer, "InputFeederRestore");

                    if (IsFeederWaferMidUnload(feederWafer))
                    {
                        _resumeFeederUnloadToCassette = true;
                        // 로드 상태머신이 실행되지 않도록 안전한 값으로 둔다(실제 처리는 cycle 초입의 언로드 재개 분기에서 수행).
                        _autoStep = InputSequenceAutoStep.ResolveSlot;
                        WriteLog("RestoreInputStepSession",
                            "Input sequence restored from InputFeeder wafer (UNLOAD 진행 중으로 판단). Cassette 복귀를 이어서 수행합니다. wafer=" + _autoWaferId +
                            ", slot=" + _autoSlotIndex +
                            ", alignResult=" + feederWafer.HasInputStageAlignResult +
                            ", positions=" + BuildAutoResumePositionSummary() +
                            " - Ok");
                        return;
                    }

                    _autoStep = InputSequenceAutoStep.LoadFeederToStage;
                    WriteLog("RestoreInputStepSession",
                        "Input sequence restored from InputFeeder wafer (LOAD 진행 중으로 판단). wafer=" + _autoWaferId +
                        ", slot=" + _autoSlotIndex +
                        ", step=" + _autoStep +
                        ", positions=" + BuildAutoResumePositionSummary() +
                        " - Ok");
                    return;
                }

                // 3순위: Stage/Feeder가 비어 있으면 cassette mapping 유무에 따라 Mapping 또는 Slot 결정부터 시작한다.
                _autoStep = IsInputCassetteMappedInRuntimeState()
                    ? InputSequenceAutoStep.ResolveSlot
                    : InputSequenceAutoStep.Mapping;

                WriteLog("RestoreInputStepSession",
                    "Input sequence restored from cassette state. step=" + _autoStep +
                    ", positions=" + BuildAutoResumePositionSummary() +
                    " - Ok");
            }
            catch (Exception ex)
            {
                _autoStep = InputSequenceAutoStep.Mapping;
                _autoSlotIndex = -1;
                _autoWaferId = "";
                WriteLog("RestoreInputStepSession",
                    "Input sequence runtime restore failed: " + ex.Message + ". Restart from mapping. - Failed");
            }
            finally
            {
            }
        }

        private string BuildAutoResumePositionSummary()
        {
            try
            {
                CDT320_Machine machine = Context != null ? Context.Machine : null;
                if (machine == null)
                    return "machine=null";

                InputStageUnit stage = machine.InputStageUnit;
                InputFeederUnit inputFeeder = machine.InputFeederUnit;
                PickerFrontUnit front = machine.PickerFrontUnit;
                PickerRearUnit rear = machine.PickerRearUnit;
                OutputStageUnit outputStage = machine.OutputStageUnit;
                OutputFeederUnit outputFeeder = machine.OutputFeederUnit;

                return
                    FormatAxis("InputFeederY", inputFeeder != null ? inputFeeder.FeederY : null) + "; " +
                    "InputFeederDown=" + SafeBool(inputFeeder != null ? (bool?)inputFeeder.IsWaferFeederDown() : null) + "; " +
                    FormatAxis("InputStageY", stage != null ? stage.StageY : null) + "; " +
                    FormatAxis("InputStageT", stage != null ? stage.StageT : null) + "; " +
                    FormatAxis("ExpanderZ", stage != null ? stage.ExpanderZ : null) + "; " +
                    FormatAxis("InputVisionX", stage != null ? stage.CameraX : null) + "; " +
                    FormatAxis("NeedleX", stage != null ? stage.NeedleBlockX : null) + "; " +
                    FormatAxis("NeedleZ", stage != null ? stage.NeedleZ : null) + "; " +
                    FormatAxis("EjectPinZ", stage != null ? stage.EjectPinZ : null) + "; " +
                    FormatAxis("FrontPickerX", front != null ? front.PickerX : null) + "; " +
                    FormatAxis("FrontPickerY", front != null ? front.PickerY : null) + "; " +
                    FormatAxis("FrontPickerZ1", front != null ? front.PickerZ0 : null) + "; " +
                    FormatAxis("RearPickerX", rear != null ? rear.PickerX : null) + "; " +
                    FormatAxis("RearPickerY", rear != null ? rear.PickerY : null) + "; " +
                    FormatAxis("RearPickerZ1", rear != null ? rear.PickerZ0 : null) + "; " +
                    FormatAxis("OutputVisionX", outputStage != null ? outputStage.OutputCameraX : null) + "; " +
                    FormatAxis("GoodStageY", outputStage != null && outputStage.GoodStage != null ? outputStage.GoodStage.StageY : null) + "; " +
                    FormatAxis("GoodStageZ", outputStage != null && outputStage.GoodStage != null ? outputStage.GoodStage.StageZ : null) + "; " +
                    FormatAxis("OutputFeederY", outputFeeder != null ? outputFeeder.FeederY : null) + "; " +
                    "OutputFeederDown=" + SafeBool(outputFeeder != null ? (bool?)outputFeeder.IsBinFeederDown() : null);
            }
            catch (Exception ex)
            {
                return "positionSummaryFailed=" + ex.Message;
            }
            finally
            {
            }
        }

        private static string FormatAxis(string label, QMC.Common.Motion.BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return label + "[null]";

                return label +
                    "[actual=" + axis.ActualPosition.ToString("F6") +
                    ", command=" + axis.CommandPosition.ToString("F6") +
                    ", moving=" + (axis.IsMoving ? "Y" : "N") +
                    ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                    ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                    ", homeDone=" + (axis.IsHomeDone ? "Y" : "N") +
                    "]";
            }
            catch (Exception ex)
            {
                return label + "[stateFailed=" + ex.Message + "]";
            }
            finally
            {
            }
        }

        private static string SafeBool(bool? value)
        {
            return value.HasValue ? (value.Value ? "Y" : "N") : "-";
        }

        private WaferMaterial ResolveStageWaferFromRuntimeState()
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                WaferMaterial wafer = stage != null ? stage.GetCurrentStageWaferMaterial() : null;
                if (wafer == null)
                    wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);

                if (wafer != null && stage != null && stage.CurrentWaferMaterial == null)
                    stage.SetCurrentWaferMaterial(wafer);
                return wafer;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStageWaferFromRuntimeState", "Stage wafer runtime resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private WaferMaterial ResolveFeederWaferFromRuntimeState()
        {
            try
            {
                var feeder = Context != null && Context.Machine != null ? Context.Machine.InputFeederUnit : null;
                WaferMaterial wafer = feeder != null ? feeder.CurrentWaferMaterial : null;
                if (wafer == null)
                    wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                if (wafer != null && feeder != null && feeder.CurrentWaferMaterial == null)
                    feeder.SetCurrentWaferMaterial(wafer);
                return wafer;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveFeederWaferFromRuntimeState", "Feeder wafer runtime resolve failed: " + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private InputSequenceAutoStep ResolveStageWaferResumeStep(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null)
                    return InputSequenceAutoStep.AlignStage;

                string resultModeReason;
                if (wafer.HasInputStageAlignResult &&
                    !MaterialStateService.IsStoredInputStageResultModeUsable(
                        wafer,
                        false,
                        out resultModeReason))
                {
                    WriteLog("ResolveStageWaferResumeStep",
                        "Saved InputStage align result cannot be resumed. wafer=" + (wafer.WaferId ?? "") +
                        ", reason=" + resultModeReason + " - Check");
                    return InputSequenceAutoStep.AlignStage;
                }

                // Align 결과, die mapping 결과와 die id가 있으면 사용자 승인 여부를 확인한다.
                // legacy 빈 frame id는 MaterialStateService가 map/wafer id 순서로 revision을 해석한다.
                if (wafer.HasInputStageAlignResult &&
                    wafer.HasInputStageDieMappingResult &&
                    wafer.DieIds != null &&
                    wafer.DieIds.Count > 0 &&
                    MaterialStateService.IsStoredInputStageResultModeUsable(
                        wafer,
                        true,
                        out resultModeReason))
                {
                    string approvalReason;
                    if (MaterialStateService.IsInputStageRunReviewApprovalUsable(
                        wafer,
                        out approvalReason))
                    {
                        return InputSequenceAutoStep.Complete;
                    }

                    if (wafer.HasInputStageRunReviewApproval)
                    {
                        WriteLog("ResolveStageWaferResumeStep",
                            "Saved InputStage Review approval is not usable. wafer=" +
                            (wafer.WaferId ?? "") + ", reason=" + approvalReason +
                            ". ReviewStage부터 다시 확인합니다. - Check");
                    }
                    return InputSequenceAutoStep.ReviewStage;
                }

                // Align은 끝났지만 die map 정보가 부족하면 DieMapping부터 재개한다.
                if (wafer.HasInputStageAlignResult)
                    return InputSequenceAutoStep.DieMapping;

                // Stage wafer가 있지만 Align 결과가 없으면 Align부터 재개한다.
                return InputSequenceAutoStep.AlignStage;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveStageWaferResumeStep", "Stage wafer resume step resolve failed: " + ex.Message + " - Failed");
                return InputSequenceAutoStep.AlignStage;
            }
            finally
            {
            }
        }

        private int ResolveSlotIndexFromWafer(WaferMaterial wafer)
        {
            try
            {
                if (wafer == null)
                    return -1;

                // To do: C4 - 재개/언로드 시 웨이퍼의 원본 레벨(Input1/Input2)로 현재 처리 레벨을 복원한다.
                if (wafer.SourceCassetteRole == CassetteMaterialRole.Input1 || wafer.SourceCassetteRole == CassetteMaterialRole.Input2)
                    _autoCassetteRole = wafer.SourceCassetteRole;

                if (wafer.SourceSlotNumber >= 0)
                    return wafer.SourceSlotNumber;

                if (wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                    wafer.CurrentLocation.SlotNumber >= 0)
                {
                    return wafer.CurrentLocation.SlotNumber;
                }
            }
            catch (Exception ex)
            {
                WriteLog("ResolveSlotIndexFromWafer", "Input slot resolve from wafer failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private void RestoreActiveInputWaferSourceSlotProjection(WaferMaterial wafer, string restoreContext)
        {
            try
            {
                if (wafer == null ||
                    wafer.CurrentLocation == null ||
                    (wafer.CurrentLocation.Kind != MaterialLocationKind.InputStage &&
                     wafer.CurrentLocation.Kind != MaterialLocationKind.InputFeeder) ||
                    (wafer.SourceCassetteRole != CassetteMaterialRole.Input1 &&
                     wafer.SourceCassetteRole != CassetteMaterialRole.Input2) ||
                    wafer.SourceSlotNumber < 0 ||
                    WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                {
                    return;
                }

                CassetteMaterial cassetteMaterial = MaterialStateService.State != null &&
                                                     MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c =>
                        c != null && c.Role == wafer.SourceCassetteRole)
                    : null;
                if (cassetteMaterial == null ||
                    !cassetteMaterial.IsEnabled ||
                    !cassetteMaterial.IsPresent ||
                    !cassetteMaterial.IsMapped)
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore skipped because cassette Material mapping is not valid. context=" +
                        (restoreContext ?? "") +
                        ", wafer=" + (wafer.WaferId ?? "") +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber + " - Check");
                    return;
                }

                cassetteMaterial.EnsureSlots();
                if (wafer.SourceSlotNumber >= cassetteMaterial.Slots.Count)
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore skipped because source slot is out of range. context=" +
                        (restoreContext ?? "") +
                        ", wafer=" + (wafer.WaferId ?? "") +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber +
                        ", slotCount=" + cassetteMaterial.Slots.Count + " - Failed");
                    return;
                }

                CassetteSlotMaterial sourceSlot = cassetteMaterial.Slots[wafer.SourceSlotNumber];
                bool sourceSlotOccupied =
                    sourceSlot != null &&
                    (sourceSlot.HasWafer || !string.IsNullOrWhiteSpace(sourceSlot.WaferId));
                WaferMaterial cassetteWafer = MaterialStateService.GetWaferInCassette(
                    wafer.SourceCassetteRole,
                    wafer.SourceSlotNumber);
                if (sourceSlotOccupied ||
                    (cassetteWafer != null &&
                     WaferMaterialStateText.Normalize(cassetteWafer.State) != WaferMaterialState.Empty))
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore blocked because cassette Material slot is occupied. context=" +
                        (restoreContext ?? "") +
                        ", activeWafer=" + (wafer.WaferId ?? "") +
                        ", cassetteWafer=" + (cassetteWafer != null ? cassetteWafer.WaferId : "") +
                        ", slotWaferId=" + (sourceSlot != null ? sourceSlot.WaferId : "") +
                        ", slotHasWafer=" + (sourceSlot != null && sourceSlot.HasWafer) +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber + " - Failed");
                    return;
                }

                InputCassetteUnit cassette = Context != null && Context.Machine != null
                    ? Context.Machine.InputCassetteUnit
                    : null;
                if (cassette == null ||
                    cassette.Config == null ||
                    wafer.SourceSlotNumber >= cassette.Config.SlotCount)
                {
                    return;
                }

                int cassetteLevel = InputCassetteUnit.ResolveCassetteLevel(wafer.SourceCassetteRole);
                WaferCassetteMaterial unitMaterial = cassette.GetWaferMaterialCassette(cassetteLevel);
                WaferSlotState previousState =
                    unitMaterial != null &&
                    unitMaterial.Slots != null &&
                    wafer.SourceSlotNumber < unitMaterial.Slots.Count
                        ? unitMaterial.Slots[wafer.SourceSlotNumber]
                        : null;

                if (previousState != null &&
                    previousState.Presence == SlotPresence.Exist &&
                    previousState.Process == ProcessState.Processing)
                {
                    return;
                }

                if (previousState != null &&
                    previousState.Presence != SlotPresence.Empty)
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore blocked by non-empty unit slot state. context=" +
                        (restoreContext ?? "") +
                        ", wafer=" + (wafer.WaferId ?? "") +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber +
                        ", presence=" + previousState.Presence +
                        ", process=" + previousState.Process + " - Failed");
                    return;
                }

                // 앱 재시작 시 Unit의 휘발성 slot projection은 사라지지만, 저장된 active Material과
                // 비어 있는 원본 Material slot은 유지된다. Unit projection도 Empty일 때만 정상 로드 완료
                // 상태를 복원하며, Exist/Unknown은 실제 점유 불일치 가능성이 있으므로 안전 실패한다.
                cassette.UpdateWaferCassetteSlotState(
                    cassetteLevel,
                    wafer.SourceSlotNumber,
                    SlotPresence.Exist,
                    ProcessState.Processing);
                WriteLog("RestoreInputSourceSlot",
                    "Active wafer source slot projection restored from Material snapshot. context=" +
                    (restoreContext ?? "") +
                    ", wafer=" + (wafer.WaferId ?? "") +
                    ", location=" + wafer.CurrentLocation.Kind +
                    ", role=" + wafer.SourceCassetteRole +
                    ", level=" + cassetteLevel +
                    ", slot=" + wafer.SourceSlotNumber +
                    ", previousPresence=" + (previousState != null ? previousState.Presence.ToString() : "null") +
                    ", previousProcess=" + (previousState != null ? previousState.Process.ToString() : "null") +
                    ", restoredProcess=" + ProcessState.Processing + " - Check");
            }
            catch (Exception ex)
            {
                WriteLog("RestoreInputSourceSlot",
                    "Active wafer source slot projection restore failed. context=" + (restoreContext ?? "") +
                    ", wafer=" + (wafer != null ? wafer.WaferId : "") +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private bool IsInputCassetteMappedInRuntimeState()
        {
            try
            {
                CassetteMaterial inputCassetteState = null;
                if (MaterialStateService.State != null && MaterialStateService.State.Cassettes != null)
                {
                    foreach (var cassette in MaterialStateService.State.Cassettes)
                    {
                        if (cassette != null &&
                            cassette.Role == CassetteMaterialRole.Input1)
                        {
                            inputCassetteState = cassette;
                            break;
                        }
                    }
                }

                // Material cassette가 존재하면 그 상태를 단일 기준으로 사용한다.
                // Clear All로 IsMapped가 내려간 뒤 WaferMap의 고정 slot 개수만 보고
                // mapping 완료로 오인하지 않도록 한다.
                if (inputCassetteState != null)
                {
                    return inputCassetteState.IsEnabled &&
                           inputCassetteState.IsPresent &&
                           inputCassetteState.IsMapped;
                }

                var inputCassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                return inputCassette != null &&
                       inputCassette.WaferMap != null &&
                       inputCassette.WaferMap.Count > 0;
            }
            catch (Exception ex)
            {
                WriteLog("IsInputCassetteMappedInRuntimeState", "Input cassette mapped state resolve failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCurrentInputStepAsync(CancellationToken ct, bool requireVisionAlign)
        {
            InputSequenceAutoStep executingStep = _autoStep;
            bool loaderActiveStep = IsInputLoaderActiveAutoStep(executingStep);
            bool stepSucceeded = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step start. step=" + _autoStep + " - Start");
                if (Mode != SequenceRunMode.Auto)
                    SetInputLoaderActive(loaderActiveStep, executingStep.ToString());

                int result;
                switch (_autoStep)
                {
                    // [1] Mapping: Input Cassette의 slot map/material 상태를 갱신한다.
                    case InputSequenceAutoStep.Mapping:
                        result = await ExecuteMappingAsync(ct, false, 0, SequenceStartMode.Resume).ConfigureAwait(false);
                        if (result != 0)
                        return Fail("SEQ-IN-STEP-MAP", "InputSequence", "Input cassette 매핑 실패. result=" + result);
                        // Mapping 완료 후 다른 sequence가 확인할 수 있도록 cassette mapped bus를 올린다.
                        Context.Bus.Set("InputCassetteMapped");
                        _autoStep = InputSequenceAutoStep.ResolveSlot;
                        break;

                    // [2] ResolveSlot: Processing 중인 slot을 우선 사용하고, 없으면 다음 Ready slot을 선택한다.
                    case InputSequenceAutoStep.ResolveSlot:
                        _autoSlotIndex = ResolveCurrentOrNextInputSlot();
                        if (_autoSlotIndex < 0)
                            return StopInputNoReadyWafer();
                        // wafer id는 Stage/Feeder 하위 sequence option과 로그 추적에 사용된다.
                        _autoWaferId = ResolveInputWaferId(_autoSlotIndex);
                        _autoStep = InputSequenceAutoStep.PrepareStageLoad;
                        break;

                    // [3] PrepareStageLoad: Picker가 Avoid로 빠진 상태에서 Stage 로드 준비 위치를 만든다.
                    case InputSequenceAutoStep.PrepareStageLoad:
                    {
                        result = await ExecuteWithInputPickerAvoidGateAsync("InputPrepareLoad", ct, async () =>
                        {
                            using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareLoad", ct).ConfigureAwait(false))
                            {
                                if (lease == null)
                                    return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Load 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                                // InputStageSequence가 실제 Stage 준비 동작을 담당한다.
                                var stageSequence = new InputStageSequence(Context);
                                int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "PrepareLoad",
                                    () => stageSequence.RunPrepareLoadAsync(
                                        ct,
                                        BuildStageSequenceOptions(false, SequenceStartMode.Resume, false, _autoWaferId, false)),
                                    "wafer=" + _autoWaferId).ConfigureAwait(false);
                                if (stageResult != 0)
                                    return Fail("SEQ-IN-STEP-STAGE-PREP", "InputStage",
                                        "InputStage Load 준비 실패. result=" + stageResult);
                            }

                            return 0;
                        }).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        _autoStep = InputSequenceAutoStep.LoadFeederFromCassette;
                        break;
                    }

                    // [4] LoadFeederFromCassette: 선택된 cassette slot의 wafer를 InputFeeder로 로드한다.
                    case InputSequenceAutoStep.LoadFeederFromCassette:
                    {
                        // 재개 상황에서 slot 정보가 비어 있으면 cassette 상태에서 다시 확인한다.
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveCurrentOrNextInputSlot();
                        if (_autoSlotIndex < 0)
                            return Fail("SEQ-IN-STEP-SLOT", "InputSequence", "Feeder 카세트 로딩 전에 Input Slot이 결정되지 않았습니다.");

                        // InputFeederSequence가 cassette slot 접근과 feeder 적재 동작을 수행한다.
                        var feederSequence = new InputFeederSequence(Context);
                        InputFeederSequenceOptions feederOptions =
                            BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                        result = await ExecuteWithInputPickerAvoidGateAsync("InputLoadFromCassette", ct, () =>
                            SequenceTrace.ChildAsync("InputFeederSequence", "LoadFromCassette",
                                () => feederSequence.RunLoadFromCassetteAsync(ct, feederOptions),
                                "slot=" + _autoSlotIndex)).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-FEEDER-CST", "InputFeeder",
                                "InputFeeder cassette loading 실패. result=" + result);
                        // slot은 이제 작업 중인 wafer로 표시해서 중복 선택을 막는다.
                        UpdateInputSlotState(_autoSlotIndex, SlotPresence.Exist, ProcessState.Processing);
                        _autoStep = InputSequenceAutoStep.LoadFeederToStage;
                        break;
                    }

                    // [5] LoadFeederToStage: InputFeeder의 wafer를 InputStage로 넘긴다.
                    case InputSequenceAutoStep.LoadFeederToStage:
                    {
                        // Feeder에 wafer만 남은 재개 상황이면 wafer의 source slot에서 slot index를 복원한다.
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveFeederWaferFromRuntimeState());

                        result = await ExecuteWithInputPickerAvoidGateAsync("InputFeederToStage", ct, async () =>
                        {
                            using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputFeederToStage", ct).ConfigureAwait(false))
                            {
                                if (lease == null)
                                    return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Feeder -> Stage 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                                var feederSequence = new InputFeederSequence(Context);
                                InputFeederSequenceOptions feederOptions =
                                    BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                                int feederResult = await SequenceTrace.ChildAsync("InputFeederSequence", "LoadToStage",
                                    () => feederSequence.RunLoadToStageAsync(ct, feederOptions),
                                    "slot=" + _autoSlotIndex).ConfigureAwait(false);
                                if (feederResult != 0)
                                    return Fail("SEQ-IN-STEP-FEEDER-STAGE", "InputFeeder",
                                        "InputFeeder -> InputStage loading 실패. result=" + feederResult);
                            }

                            return 0;
                        }).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        _autoStep = InputSequenceAutoStep.RecoverFeeder;
                        break;
                    }

                    // [6] RecoverFeeder: wafer 전달 후 Feeder를 후속 동작 가능한 상태로 복귀시킨다.
                    case InputSequenceAutoStep.RecoverFeeder:
                    {
                        // Stage에 올라간 wafer 기준으로 slot 정보를 복원할 수 있다.
                        if (_autoSlotIndex < 0)
                            _autoSlotIndex = ResolveSlotIndexFromWafer(ResolveStageWaferFromRuntimeState());

                        var feederSequence = new InputFeederSequence(Context);
                        InputFeederSequenceOptions feederOptions =
                            BuildFeederSequenceOptions(_autoSlotIndex, _autoSlotIndex, false, 0, SequenceStartMode.Resume);
                        result = await ExecuteWithInputPickerAvoidGateAsync("InputFeederRecover", ct, () =>
                            SequenceTrace.ChildAsync("InputFeederSequence", "Recover",
                                () => feederSequence.RunRecoverAsync(ct, feederOptions),
                                "slot=" + _autoSlotIndex)).ConfigureAwait(false);
                        if (result != 0)
                            return Fail("SEQ-IN-STEP-FEEDER-RECOVER", "InputFeeder",
                                "InputFeeder recover 실패. result=" + result);
                        _autoStep = InputSequenceAutoStep.AlignStage;
                        break;
                    }

                    // [7] AlignStage: InputStageArea를 점유하고 wafer align을 수행한다.
                    case InputSequenceAutoStep.AlignStage:
                    {
                        SequenceStartMode alignStartMode = _restartAlignFromReview
                            ? SequenceStartMode.Restart
                            : SequenceStartMode.Resume;
                        result = await ExecuteWithInputPickerAvoidGateAsync("InputAlign", ct, async () =>
                        {
                            using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputAlign", ct).ConfigureAwait(false))
                            {
                                if (lease == null)
                                    return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                                // requireVisionAlign이 true이면 StageSequence 내부에서 vision align 조건을 함께 요구한다.
                                var stageSequence = new InputStageSequence(Context);
                                int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "Align",
                                    () => stageSequence.RunAlignAsync(
                                        ct,
                                        BuildStageSequenceOptions(false, alignStartMode, requireVisionAlign, _autoWaferId, false)),
                                    "wafer=" + _autoWaferId,
                                    "requireVisionAlign=" + requireVisionAlign,
                                    "startMode=" + alignStartMode).ConfigureAwait(false);
                                if (stageResult != 0)
                                    return Fail("SEQ-IN-STEP-STAGE-ALIGN", "InputStage",
                                        "InputStage align 실패. result=" + stageResult);
                            }

                            return 0;
                        }).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        _restartAlignFromReview = false;
                        _autoStep = InputSequenceAutoStep.DieMapping;
                        break;
                    }

                    // [8] DieMapping: Align 결과를 기반으로 Stage wafer의 die map 정보를 생성한다.
                    case InputSequenceAutoStep.DieMapping:
                    {
                        SequenceStartMode mappingStartMode = _restartDieMappingFromReview
                            ? SequenceStartMode.Restart
                            : SequenceStartMode.Resume;
                        string dieMappingResumeStep;
                        if (mappingStartMode == SequenceStartMode.Resume &&
                            ShouldRestartWaferAlignForDieMappingResume(out dieMappingResumeStep))
                        {
                            RestartWaferAlignAfterMissingDieMapPoints(dieMappingResumeStep);
                            break;
                        }

                        result = await ExecuteWithInputPickerAvoidGateAsync("InputDieMapping", ct, async () =>
                        {
                            using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputDieMapping", ct).ConfigureAwait(false))
                            {
                                if (lease == null)
                                    return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Die mapping 중 InputStageArea 리소스 점유에 실패했습니다.");

                                InputStageSequenceOptions mappingOptions =
                                    BuildStageSequenceOptions(false, mappingStartMode, requireVisionAlign, _autoWaferId, false);
                                // Auto에서는 사용자 리뷰 승인 전 Picker Ready 신호를 발행하지 않는다.
                                mappingOptions.PublishReadySignals = false;

                                // DieMapping이 끝나면 MaterialStateService의 Stage finish 조건이 만족되어야 한다.
                                var stageSequence = new InputStageSequence(Context);
                                int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "DieMapping",
                                    () => stageSequence.RunDieMappingAsync(
                                        ct,
                                        mappingOptions),
                                    "wafer=" + _autoWaferId,
                                    "requireVisionAlign=" + requireVisionAlign).ConfigureAwait(false);
                                if (stageResult != 0)
                                    return Fail("SEQ-IN-STEP-STAGE-DIEMAP", "InputStage",
                                        "InputStage die mapping 실패. result=" + stageResult);
                            }

                            return 0;
                        }).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        _restartDieMappingFromReview = false;
                        // Picker Ready는 사용자 리뷰 승인 후에만 발행한다.
                        _autoStep = InputSequenceAutoStep.ReviewStage;
                        break;
                    }

                    // [9] ReviewStage: Align/Die Mapping 결과를 표시하고 작업자의 진행/재실행 결정을 기다린다.
                    case InputSequenceAutoStep.ReviewStage:
                    {
                        InputStageUnit stage = Context != null && Context.Machine != null
                            ? Context.Machine.InputStageUnit
                            : null;
                        MachineController controller = Context != null ? Context.Controller : null;
                        WaferMaterial reviewWafer = ResolveStageWaferFromRuntimeState();
                        if (stage == null || controller == null || reviewWafer == null)
                            return Fail("SEQ-IN-REVIEW-MATERIAL", "InputSequence",
                                "InputStage 리뷰 대상 장비 또는 Wafer Material이 없습니다.");

                        if (!reviewWafer.HasInputStageAlignResult ||
                            !reviewWafer.HasInputStageThetaAlignResult ||
                            !reviewWafer.HasInputStageDieMappingResult ||
                            reviewWafer.InputStageDieMappingInvalidatedByAlignChange)
                        {
                            return Fail("SEQ-IN-REVIEW-STATE", "InputSequence",
                                "InputStage 리뷰 전 Align/T Align/Die Mapping 상태가 유효하지 않습니다. wafer=" +
                                (reviewWafer.WaferId ?? ""));
                        }

                        ResetInputStageReadySignals();
                        string pickerReason;
                        if (!controller.AreInputStageRunReviewPickersSafe(out pickerReason))
                        {
                            return Fail("SEQ-IN-REVIEW-PICKER-STATE", "InputSequence",
                                "InputStage Review 진입 전 통합 Picker 안전 조건이 유효하지 않습니다. " +
                                pickerReason);
                        }

                        string sessionReason;
                        if (!controller.TryEnterInputStageRunReviewManual(
                            Context,
                            reviewWafer.WaferId,
                            out sessionReason))
                        {
                            return Fail("SEQ-IN-REVIEW-MANUAL-ENTER", "InputSequence", sessionReason);
                        }

                        WriteLog("InputStageRunReview",
                            "Align/Die Mapping 사용자 확인을 Manual 상태에서 기다립니다. wafer=" +
                            (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex +
                            ", outputSequenceMaintained=True - Wait");

                        while (controller.IsInputStageRunReviewManualActive)
                        {
                            UserConfirmResult reviewResult = await stage.WaitForUserConfirmAsync(ct).ConfigureAwait(false);
                            InputStageRunReviewDecision decision = reviewResult != null && reviewResult.IsConfirmed
                                ? InputStageRunReviewDecision.ConfirmAndContinue
                                : (reviewResult != null ? reviewResult.Decision : InputStageRunReviewDecision.RetryAlign);

                            if (decision == InputStageRunReviewDecision.ConfirmAndContinue)
                            {
                                controller.CancelInputStageRunReviewAction();
                                if (!await WaitForInputStageRunReviewActionStopAsync(controller, ct).ConfigureAwait(false))
                                {
                                    stage.NotifyUserConfirmProcessingFailed(
                                        "Review 수동 동작 또는 Jog가 아직 진행 중입니다. STOP 후 다시 확인하세요.");
                                    continue;
                                }

                                int avoidResult = await MoveInputCameraXToAvoidAfterReviewAsync(stage, ct).ConfigureAwait(false);
                                if (avoidResult != 0)
                                {
                                    stage.NotifyUserConfirmProcessingFailed(
                                        "Input Camera X를 Avoid 위치로 복귀하지 못했습니다. Alarm/인터락을 확인한 뒤 다시 시도하세요.");
                                    continue;
                                }

                                string approvalReason;
                                if (!MaterialStateService.CommitInputStageRunReview(
                                    reviewWafer,
                                    reviewResult,
                                    out approvalReason))
                                {
                                    stage.NotifyUserConfirmProcessingFailed(approvalReason);
                                    continue;
                                }

                                if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                                {
                                    string resetReason;
                                    WaferMaterial approvalWafer = ResolveStageWaferFromRuntimeState() ?? reviewWafer;
                                    bool approvalCleared = MaterialStateService.SetInputStageRunReviewApproval(
                                        approvalWafer,
                                        false,
                                        0,
                                        out resetReason);
                                    if (!approvalCleared)
                                    {
                                        return Fail("SEQ-IN-REVIEW-AUTO-RESUME-RESET", "InputSequence",
                                            sessionReason + " Auto 복귀 실패 후 Review 승인을 해제하지 못했습니다. " +
                                            resetReason);
                                    }

                                    stage.NotifyUserConfirmProcessingFailed(
                                        sessionReason + " Auto 복귀가 완료되지 않아 Review 승인을 해제했습니다. 다시 확인하세요.");
                                    WriteLog("InputStageRunReview",
                                        "Review Commit 후 Auto 복귀가 실패하여 승인을 fail-closed 해제했습니다. wafer=" +
                                        (reviewWafer.WaferId ?? "") + ", reason=" + sessionReason +
                                        ", reset=" + resetReason + " - Reset");
                                    continue;
                                }

                                _autoStep = InputSequenceAutoStep.Complete;
                                WriteLog("InputStageRunReview",
                                    approvalReason + ", CameraXAvoid=True, sameCoordinator=True - Ok");
                                break;
                            }

                            if (decision == InputStageRunReviewDecision.RetryMapping)
                            {
                                string resetReason;
                                MaterialStateService.SetInputStageRunReviewApproval(
                                    reviewWafer,
                                    false,
                                    0,
                                    out resetReason);
                                SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                                _restartDieMappingFromReview = true;
                                _autoStep = InputSequenceAutoStep.DieMapping;

                                if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                                {
                                    stage.NotifyUserConfirmProcessingFailed(sessionReason);
                                    continue;
                                }

                                WriteLog("InputStageRunReview",
                                    "사용자가 Die Mapping 개별 재실행을 선택했습니다. wafer=" +
                                    (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex + " - Restart");
                                break;
                            }

                            if (decision == InputStageRunReviewDecision.Stop)
                            {
                                string resetReason;
                                MaterialStateService.SetInputStageRunReviewApproval(
                                    reviewWafer,
                                    false,
                                    0,
                                    out resetReason);
                                controller.CancelInputStageRunReviewAction();
                                controller.TryExitInputStageRunReviewManual(Context, false, out sessionReason);
                                Context.RequestCycleStop();
                                throw new SequenceStopException(
                                    "InputStage Review 중 STOP 요청으로 동일 Auto Coordinator를 안전 정지합니다.");
                            }

                            string alignResetReason;
                            MaterialStateService.SetInputStageRunReviewApproval(
                                reviewWafer,
                                false,
                                0,
                                out alignResetReason);
                            SequenceResumeStore.Clear(InputStageAlignSequenceStateName);
                            SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                            _restartAlignFromReview = true;
                            _restartDieMappingFromReview = true;
                            _autoStep = InputSequenceAutoStep.AlignStage;

                            if (!controller.TryExitInputStageRunReviewManual(Context, true, out sessionReason))
                            {
                                stage.NotifyUserConfirmProcessingFailed(sessionReason);
                                continue;
                            }

                            WriteLog("InputStageRunReview",
                                "사용자가 Align 재실행을 선택하여 센터 검출/T Align과 종속 Die Mapping을 다시 실행합니다. wafer=" +
                                (reviewWafer.WaferId ?? "") + ", slot=" + _autoSlotIndex + " - Restart");
                            break;
                        }
                        break;
                    }

                    // [10] Complete: 로딩/정렬/맵핑/사용자 확인이 끝났고, 상위 cycle에서 Picker 완료를 기다리는 상태이다.
                    case InputSequenceAutoStep.Complete:
                        LogPublic("[UNIT-INPUT] Input sequence already complete slot=" + _autoSlotIndex);
                        break;

                    default:
                        return Fail("SEQ-IN-STEP-UNKNOWN", "InputSequence", "알 수 없는 Input 시퀀스 스텝입니다. step=" + _autoStep);
                }

                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step complete. nextStep=" + _autoStep + " - Ok");
                stepSucceeded = true;
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step canceled. step=" + _autoStep + " - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-STEP-EX", "InputSequence", "Input 시퀀스 스텝 실패. step=" + _autoStep + ", error=" + ex.Message);
            }
            finally
            {
                if (loaderActiveStep && (!stepSucceeded || !IsInputLoaderActiveAutoStep(_autoStep)))
                    if (Mode != SequenceRunMode.Auto)
                        ResetInputLoaderActive(true, executingStep.ToString());
            }
        }

        public async Task<int> ExecuteMappingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                // 단독 Mapping 요청: InputCassetteSequence에 위임해서 cassette slot map을 갱신한다.
                var sequence = new InputCassetteSequence(Context);
                return await SequenceTrace.ChildAsync("InputCassetteSequence", "Mapping",
                    () => sequence.RunMappingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode))).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingAsync", "Input cassette mapping sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAP-EX", "InputSequence", "Input cassette mapping 시퀀스 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteLoadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                // Cassette 자체 로딩 동작은 InputCassetteSequence에서 처리한다.
                var sequence = new InputCassetteSequence(Context);
                return await SequenceTrace.ChildAsync("InputCassetteSequence", "Loading",
                    () => sequence.RunLoadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode))).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteLoadingAsync", "Input cassette loading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-LOAD-EX", "InputSequence", "Input cassette loading 시퀀스 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteCassetteUnloadingAsync(CancellationToken ct, bool bFine = false, int moveTimeoutMs = 0, SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                // Cassette 자체 언로딩 동작은 InputCassetteSequence에서 처리한다.
                var sequence = new InputCassetteSequence(Context);
                return await SequenceTrace.ChildAsync("InputCassetteSequence", "Unloading",
                    () => sequence.RunUnloadingAsync(ct, BuildCassetteSequenceOptions(bFine, moveTimeoutMs, startMode))).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCassetteUnloadingAsync", "Input cassette unloading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-CST-UNLOAD-EX", "InputSequence", "Input cassette unloading 시퀀스 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteWaferLoadingAsync(
            CancellationToken ct,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume,
            bool requireVisionAlign = false)
        {
            bool loaderActive = true;
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT] Wafer loading start");
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence start. - Start");
                SetInputLoaderActive(loaderActive, "ManualWaferLoading");

                // 수동 Wafer Loading도 먼저 cassette mapping을 수행해서 Ready slot 판단 기준을 최신화한다.
                int result = await ExecuteMappingAsync(ct, bFine, moveTimeoutMs, startMode).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-WAFER-MAP", "InputSequence", "웨이퍼 로딩 전 Input cassette mapping 실패. result=" + result);

                // 다음 작업 가능한 Ready slot을 찾고, 없으면 cassette 완료/No Ready 조건으로 정지한다.
                int slotIndex = ResolveNextInputSlot();
                if (slotIndex < 0)
                    return StopInputNoReadyWafer();

                string waferId = ResolveInputWaferId(slotIndex);
                // Stage 로드 준비 구간: Picker가 이미 Avoid일 때만 Stage를 load 받을 위치로 준비한다.
                result = await ExecuteWithInputPickerAvoidGateAsync("ManualInputPrepareLoad", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputPrepareLoad", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 웨이퍼 로딩 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                        var stageSequence = new InputStageSequence(Context);
                        int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "PrepareLoad",
                            () => stageSequence.RunPrepareLoadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, waferId, false)),
                            "wafer=" + waferId).ConfigureAwait(false);
                        if (stageResult != 0)
                            return Fail("SEQ-IN-STAGE-PREP", "InputStage",
                                "InputStage load 준비 실패. result=" + stageResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Cassette -> Feeder 로딩 구간.
                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await ExecuteWithInputPickerAvoidGateAsync("ManualInputLoadFromCassette", ct, () =>
                    SequenceTrace.ChildAsync("InputFeederSequence", "LoadFromCassette",
                        () => feederSequence.RunLoadFromCassetteAsync(ct, feederOptions),
                        "slot=" + slotIndex)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST", "InputSequence", "InputFeeder cassette loading 실패. result=" + result);

                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Processing);

                // Feeder -> Stage 이송 구간. 자동 step의 LoadFeederToStage와 같은 안전 조건을 사용한다.
                result = await ExecuteWithInputPickerAvoidGateAsync("ManualInputFeederToStage", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputFeederToStage", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 Feeder -> Stage 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                        int feederResult = await SequenceTrace.ChildAsync("InputFeederSequence", "LoadToStage",
                            () => feederSequence.RunLoadToStageAsync(ct, feederOptions),
                            "slot=" + slotIndex).ConfigureAwait(false);
                        if (feederResult != 0)
                            return Fail("SEQ-IN-FEEDER-STAGE", "InputSequence", "InputFeeder -> InputStage 로딩 실패. result=" + feederResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Stage로 넘긴 뒤 Feeder를 복귀시킨다.
                result = await ExecuteWithInputPickerAvoidGateAsync("ManualInputFeederRecover", ct, () =>
                    SequenceTrace.ChildAsync("InputFeederSequence", "Recover",
                        () => feederSequence.RunRecoverAsync(ct, feederOptions),
                        "slot=" + slotIndex)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-RECOVER", "InputSequence", "Stage loading 후 InputFeeder recover 실패. result=" + result);

                // 수동 Loading은 Align까지 수행하고 종료한다. DieMapping/Picker ready는 자동 step 흐름과 별도로 호출된다.
                result = await ExecuteWithInputPickerAvoidGateAsync("ManualInputAlign", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualInputAlign", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "수동 Align 중 InputStageArea 리소스 점유에 실패했습니다.");

                        var stageSequence = new InputStageSequence(Context);
                        int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "Align",
                            () => stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, waferId, false)),
                            "wafer=" + waferId,
                            "requireVisionAlign=" + requireVisionAlign).ConfigureAwait(false);
                        if (stageResult != 0)
                            return Fail("SEQ-IN-STAGE-ALIGN", "InputSequence", "InputStage align 실패. result=" + stageResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Loading 완료 bus는 올리지만, Picker 접근을 허용하는 StageReady는 올리지 않는다.
                Context.Bus.Set("InputWaferLoaded");
                ResetInputStageReadySignals();
                LogPublic("[UNIT-INPUT] Wafer loading complete slot=" + slotIndex);
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence completed. slot=" + slotIndex + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferLoadingAsync", "Input wafer loading sequence canceled. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-LOAD-EX", "InputSequence", "Input 웨이퍼 로딩 시퀀스 실패: " + ex.Message);
            }
            finally
            {
                ResetInputLoaderActive(loaderActive, "ManualWaferLoading");
            }
        }

        public async Task<int> ExecuteWaferUnloadingAsync(
            CancellationToken ct,
            int slotIndex,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            bool loaderActive = true;
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT] Wafer unloading start slot=" + slotIndex);
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence start. slot=" + slotIndex + " - Start");
                SetInputLoaderActive(loaderActive, "ManualWaferUnloading");

                int result;
                // Stage unload 준비 구간: Picker가 Avoid일 때만 Stage를 unload 가능한 상태로 만든다.
                result = await ExecuteWithInputPickerAvoidGateAsync("InputPrepareUnload", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputPrepareUnload", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Unload 준비 중 InputStageArea 리소스 점유에 실패했습니다.");

                        var stageSequence = new InputStageSequence(Context);
                        int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "PrepareUnload",
                            () => stageSequence.RunPrepareUnloadAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, ResolveInputWaferId(slotIndex), false)),
                            "slot=" + slotIndex).ConfigureAwait(false);
                        if (stageResult != 0)
                            return Fail("SEQ-IN-STAGE-UNLOAD-PREP", "InputStage",
                                "InputStage unload 준비 실패. result=" + stageResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Stage -> Feeder 언로딩 구간.
                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(slotIndex, slotIndex, bFine, moveTimeoutMs, startMode);

                result = await ExecuteWithInputPickerAvoidGateAsync("InputStageToFeeder", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputStageToFeeder", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Stage -> Feeder 이송 중 InputStageArea 리소스 점유에 실패했습니다.");

                        int feederResult = await SequenceTrace.ChildAsync("InputFeederSequence", "UnloadFromStage",
                            () => feederSequence.RunUnloadFromStageAsync(ct, feederOptions),
                            "slot=" + slotIndex).ConfigureAwait(false);
                        if (feederResult != 0)
                            return Fail("SEQ-IN-FEEDER-STAGE-UNLOAD", "InputSequence", "InputStage -> InputFeeder 언로딩 실패. result=" + feederResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Feeder -> Cassette 복귀 구간.
                result = await ExecuteWithInputPickerAvoidGateAsync("InputUnloadToCassette", ct, () =>
                    SequenceTrace.ChildAsync("InputFeederSequence", "UnloadToCassette",
                        () => feederSequence.RunUnloadToCassetteAsync(ct, feederOptions),
                        "slot=" + slotIndex)).ConfigureAwait(false);
                if (result != 0)
                    return Fail("SEQ-IN-FEEDER-CST-UNLOAD", "InputSequence", "InputFeeder -> 카세트 언로딩 실패. result=" + result);

                // Feeder가 카세트 이송을 마치고 Avoid로 복귀한 뒤에만 빈 InputStage를 Avoid로 복귀시킨다.
                result = await ExecuteWithInputPickerAvoidGateAsync("InputStageMoveAvoidAfterUnload", ct, async () =>
                {
                    using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("InputStageMoveAvoidAfterUnload", ct).ConfigureAwait(false))
                    {
                        if (lease == null)
                            return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Unload 완료 후 InputStage Avoid 복귀 중 InputStageArea 리소스 점유에 실패했습니다.");

                        var stageSequence = new InputStageSequence(Context);
                        int stageResult = await SequenceTrace.ChildAsync("InputStageSequence", "MoveAvoidAfterUnload",
                            () => stageSequence.RunMoveAvoidAsync(ct, BuildStageSequenceOptions(bFine, startMode, false, ResolveInputWaferId(slotIndex), false)),
                            "slot=" + slotIndex).ConfigureAwait(false);
                        if (stageResult != 0)
                            return Fail("SEQ-IN-STAGE-AVOID", "InputStage",
                                "InputFeeder 안전 복귀 후 InputStage Avoid 복귀 실패. result=" + stageResult);
                    }

                    return 0;
                }).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // slot을 Done으로 표시하고 Stage runtime 정보를 비워 다음 cycle과 섞이지 않게 한다.
                UpdateInputSlotState(slotIndex, SlotPresence.Exist, ProcessState.Done);
                ClearInputStageRuntime();
                Context.Bus.Set("InputWaferUnloaded");
                LogPublic("[UNIT-INPUT] Wafer unloading complete slot=" + slotIndex);
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence completed. slot=" + slotIndex + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferUnloadingAsync", "Input wafer unloading sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-UNLOAD-EX", "InputSequence", "Input 웨이퍼 언로딩 시퀀스 실패: " + ex.Message);
            }
            finally
            {
                ResetInputLoaderActive(loaderActive, "ManualWaferUnloading");
            }
        }

        public async Task<int> ExecuteCurrentWaferUnloadingAsync(
            CancellationToken ct,
            bool bFine = false,
            int moveTimeoutMs = 0,
            SequenceStartMode startMode = SequenceStartMode.Resume)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // 현재 Stage wafer의 source slot을 우선 사용하고, 없으면 cassette의 Processing slot을 fallback으로 사용한다.
                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                int slotIndex = ResolveSlotIndexFromWafer(stageWafer);
                if (slotIndex < 0)
                    slotIndex = ResolveProcessingInputSlot();

                if (slotIndex < 0)
                    return Fail("SEQ-IN-MANUAL-UNLOAD-SLOT", "InputSequence", "Input UNLOAD 대상 슬롯을 확인할 수 없습니다. InputStage 자재와 카세트 Processing 슬롯 상태를 확인하세요.");

                return await ExecuteWaferUnloadingAsync(
                    ct,
                    slotIndex,
                    bFine,
                    moveTimeoutMs,
                    startMode).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteCurrentWaferUnloadingAsync", "Input Manual UNLOAD가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MANUAL-UNLOAD-EX", "InputSequence", "Input Manual UNLOAD 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> ExecuteWaferAlignAsync(
            CancellationToken ct,
            bool bFine = false,
            SequenceStartMode startMode = SequenceStartMode.Resume,
            bool requireVisionAlign = false)
        {
            try
            {
                // 단독 Align 요청: StageArea를 점유한 뒤 InputStageSequence Align만 실행한다.
                using (SequenceResourceLease lease = await AcquireInputStageAreaAsync("ManualWaferAlign", ct).ConfigureAwait(false))
                {
                    if (lease == null)
                        return Fail("SEQ-IN-RESOURCE-STAGE", "InputSequence", "Wafer align 중 InputStageArea 리소스 점유에 실패했습니다.");

                    var stageSequence = new InputStageSequence(Context);
                    return await SequenceTrace.ChildAsync("InputStageSequence", "Align",
                        () => stageSequence.RunAlignAsync(ct, BuildStageSequenceOptions(bFine, startMode, requireVisionAlign, "", false)),
                        "requireVisionAlign=" + requireVisionAlign).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteWaferAlignAsync", "Input wafer align sequence canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-WAFER-ALIGN-EX", "InputSequence", "Input 웨이퍼 Align 시퀀스 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteMappingFirstAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping start");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping requested as first input sequence step. - Start");

                // Loader 단위에서 Mapping만 먼저 수행할 때 사용하는 helper이다.
                int result = await ExecuteMappingAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (result != 0)
                    return Fail("SEQ-IN-MAPPING", "InputSequence", "Input cassette mapping 실패. result=" + result);

                Context.Bus.Set("InputCassetteMapped");
                LogPublic("[UNIT-INPUT-LOADER] Input cassette mapping complete");
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping completed as first input sequence step. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteMappingFirstAsync", "Input cassette mapping canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-MAPPING-EX", "InputSequence", "Input cassette mapping exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteLoadOnceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer start");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer requested. - Start");

                // Controller의 LoadNextWaferAsync를 직접 호출하는 단발 로딩 helper이다.
                bool ok = await Context.Controller.LoadNextWaferAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (!ok)
                return Fail("SEQ-INLOAD", "InputSequence", "다음 Input wafer loading에 실패했습니다.");

                Context.Bus.Set("InputWaferLoaded");
                ResetInputStageReadySignals();
                LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer complete");
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer completed. - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteLoadOnceAsync", "LoadNextWafer canceled. - Failed");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-INLOAD-EX", "InputSequence", "다음 웨이퍼 로딩 실행 중 예외가 발생했습니다: " + ex.Message);
            }
            finally
            {
            }
        }

        private InputCassetteSequenceOptions BuildCassetteSequenceOptions(bool bFine, int moveTimeoutMs, SequenceStartMode startMode)
        {
            try
            {
                var options = InputCassetteSequenceOptions.Default();
                options.FineMove = bFine;
                options.MoveTimeoutMs = moveTimeoutMs;
                options.RunMode = Mode;
                options.StartMode = startMode;
                return options;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<SequenceResourceLease> AcquireInputStageAreaAsync(string holder, CancellationToken ct)
        {
            try
            {
                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;
                return await AcquireResourceForRunAsync(
                    SequenceResourceKind.InputStageArea,
                    safeHolder,
                    30000,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsInputLoaderActiveAutoStep(InputSequenceAutoStep step)
        {
            return step == InputSequenceAutoStep.PrepareStageLoad ||
                   step == InputSequenceAutoStep.LoadFeederFromCassette ||
                   step == InputSequenceAutoStep.LoadFeederToStage ||
                   step == InputSequenceAutoStep.RecoverFeeder ||
                   step == InputSequenceAutoStep.AlignStage ||
                   step == InputSequenceAutoStep.DieMapping ||
                   step == InputSequenceAutoStep.ReviewStage;
        }

        private bool ShouldRestartWaferAlignForDieMappingResume(out string resumeStep)
        {
            resumeStep = "";

            try
            {
                resumeStep = SequenceResumeStore.ResolveStartStep(InputStageDieMappingSequenceStateName, "");
                if (string.IsNullOrWhiteSpace(resumeStep))
                    return false;

                // DieMapping mark point 결과는 시퀀스 메모리에만 있으므로, 중간/계산 단계 재개 시 Align부터 다시 수행한다.
                if (string.Equals(resumeStep, "FindTopPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveBottomPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "FindBottomPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveLeftPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "FindLeftPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveRightPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "FindRightPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "CalculateDieMap", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "ApplyDieMap", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveVisionXAvoidAfterManual", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteLog("ShouldRestartWaferAlignForDieMappingResume",
                    "DieMapping resume step 확인 실패. Align부터 재시작합니다. error=" + ex.Message + " - Failed");
                return true;
            }
            finally
            {
            }

            return false;
        }

        private void RestartWaferAlignAfterMissingDieMapPoints(string dieMappingResumeStep)
        {
            try
            {
                SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                SequenceResumeStore.Clear(InputStageAlignSequenceStateName);
                _autoStep = InputSequenceAutoStep.AlignStage;

                WriteLog("ExecuteCurrentInputStepAsync",
                    "DieMapping resume step=" + dieMappingResumeStep +
                    " 은/는 재시작 후 맵포인트가 복원되지 않는 단계입니다. 웨이퍼 얼라인부터 다시 시작합니다. wafer=" +
                    _autoWaferId + ", slot=" + _autoSlotIndex + " - Restart");
            }
            catch (Exception ex)
            {
                _autoStep = InputSequenceAutoStep.AlignStage;
                WriteLog("ExecuteCurrentInputStepAsync",
                    "DieMapping resume 상태 초기화 중 예외가 발생했지만 웨이퍼 얼라인부터 다시 시작합니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void SetInputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            if (_inputLoaderActivePublished)
                return;

            // 현재 기준: Input 로더 동작 중에는 Picker 공정 신규 진입을 막는다.
            Context.Bus.Set(InputLoaderActiveSignal);
            _inputLoaderActivePublished = true;
            WriteLog("InputLoaderActive", holder + " 시작: Picker 신규 공정 진입을 대기시킵니다. - Set");
        }

        private void ResetInputLoaderActive(bool active, string holder)
        {
            if (Mode == SequenceRunMode.Auto)
                return;

            if (!active || Context == null || Context.Bus == null)
                return;

            if (!_inputLoaderActivePublished)
                return;

            Context.Bus.Reset(InputLoaderActiveSignal);
            _inputLoaderActivePublished = false;
            WriteLog("InputLoaderActive", holder + " 종료: Picker 신규 공정 진입 대기를 해제합니다. - Reset");
        }

        private async Task<int> ExecuteWithInputPickerAvoidGateAsync(string holder, CancellationToken ct, Func<Task<int>> action)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;

            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureInputPickersAvoidBeforeFeederMoveAsync(safeHolder, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                string reason;
                if (!AreInputPickersAvoidAndStopped(out reason))
                    return Fail("SEQ-IN-PICKER-AVOID-STATE", "InputSequence",
                        safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                return await action().ConfigureAwait(false);
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
                return Fail("SEQ-IN-PICKER-GATE-EX", "InputSequence",
                    safeHolder + " Picker Avoid gate 처리 중 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputPickersAvoidBeforeFeederMoveAsync(string holder, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested("InputSequence.PickerAvoidGate:" + safeHolder);

                    string reason;
                    if (AreInputPickersAvoidAndStopped(out reason))
                    {
                        if (waitLogged)
                            WriteLog("InputPickerAvoidGate", safeHolder + " 전 Picker Avoid 대기 완료. - Ok");
                        return 0;
                    }

                    // 현재 기준: 로더는 Picker를 직접 이동하지 않고 PickerSequence가 Avoid로 빠질 때까지 대기한다.
                    if (Mode != SequenceRunMode.Auto)
                        return Fail("SEQ-IN-PICKER-AVOID-STATE", "InputSequence",
                            safeHolder + " 불가: Front/Rear Picker가 Avoid 정지 상태가 아닙니다. " + reason);

                    if (!waitLogged)
                    {
                        WriteLog("InputPickerAvoidGate",
                            safeHolder + " 전 Picker Avoid 대기 중입니다. reason=" + reason + " - Wait");
                        LogPublic("[UNIT-INPUT-LOADER] WAIT Picker Avoid before " + safeHolder + ". " + reason);
                        waitLogged = true;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }
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
                return Fail("SEQ-IN-PICKER-AVOID-EX", "InputSequence",
                    "Input feeder/stage 이동 전 Picker Avoid 처리 중 예외 발생. holder=" + holder +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<bool> WaitForInputStageRunReviewActionStopAsync(
            MachineController controller,
            CancellationToken ct)
        {
            if (controller == null)
                return false;

            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            while (controller.IsInputStageRunReviewActionBusy)
            {
                ct.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline)
                    return false;
                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            return true;
        }

        private async Task<int> MoveInputCameraXToAvoidAfterReviewAsync(
            InputStageUnit stage,
            CancellationToken ct)
        {
            if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
            {
                return Fail("SEQ-IN-REVIEW-CAMERA-AVOID", "InputStage",
                    "Review 확인 후 Input Camera X Avoid 복귀에 필요한 Axis/Recipe 정보가 없습니다.");
            }

            MachineController controller = Context != null ? Context.Controller : null;
            if (controller == null)
            {
                return Fail("SEQ-IN-REVIEW-CAMERA-CONTROLLER", "InputStage",
                    "Review 확인 후 통합 Picker 안전 조건을 확인할 MachineController가 없습니다.");
            }

            string pickerReason;
            if (!controller.AreInputStageRunReviewPickersSafe(out pickerReason))
            {
                return Fail("SEQ-IN-REVIEW-CAMERA-PICKER", "InputStage",
                    "Input Camera X Avoid 복귀 전 통합 Picker 안전 조건이 유효하지 않습니다. " +
                    pickerReason);
            }

            DateTime axisStopDeadline = DateTime.UtcNow.AddSeconds(3);
            while ((stage.CameraX.IsMoving ||
                    (stage.StageY != null && stage.StageY.IsMoving) ||
                    (stage.StageT != null && stage.StageT.IsMoving)) &&
                   DateTime.UtcNow < axisStopDeadline)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            if (stage.CameraX.IsMoving ||
                (stage.StageY != null && stage.StageY.IsMoving) ||
                (stage.StageT != null && stage.StageT.IsMoving))
            {
                return Fail("SEQ-IN-REVIEW-CAMERA-MOVING", "InputStage",
                    "Review 수동 동작 축이 완전히 정지하지 않아 Input Camera X Avoid 복귀를 시작할 수 없습니다.");
            }

            using (SequenceResourceLease stageLease = await AcquireInputStageAreaAsync(
                "InputStageRunReview:CameraXAvoid",
                ct).ConfigureAwait(false))
            {
                if (stageLease == null)
                {
                    return Fail("SEQ-IN-REVIEW-CAMERA-RESOURCE", "InputStage",
                        "Review 확인 후 InputStageArea 리소스 점유에 실패했습니다.");
                }

                using (AutoSequenceCameraWorkZoneLease cameraLease = await Context.AutoLoaderGate
                    .BeginInputCameraWorkAsync("InputStageRunReview:CameraXAvoid", ct)
                    .ConfigureAwait(false))
                {
                    if (!controller.AreInputStageRunReviewPickersSafe(out pickerReason))
                    {
                        return Fail("SEQ-IN-REVIEW-CAMERA-PICKER-RECHECK", "InputStage",
                            "Input Camera X Avoid 명령 직전 Picker 안전 조건 재확인에 실패했습니다. " + pickerReason);
                    }

                    double target = stage.Recipe.VisionX.AvoidPosition;
                    int result = await stage.MoveInputStageAxis(
                        WaferStageAxis.VisionX,
                        target,
                        false,
                        true).ConfigureAwait(false);
                    if (result != 0)
                    {
                        return Fail("SEQ-IN-REVIEW-CAMERA-AVOID-MOVE", "InputStage",
                            "Review 확인 후 Input Camera X Avoid 이동에 실패했습니다. result=" + result +
                            ", target=" + target.ToString("F6") +
                            ", actual=" + stage.CameraX.ActualPosition.ToString("F6"));
                    }

                    if (!stage.IsVisionXInAvoidPosition())
                    {
                        return Fail("SEQ-IN-REVIEW-CAMERA-AVOID-CHECK", "InputStage",
                            "Input Camera X 이동 완료 후 Avoid 위치 최종 확인에 실패했습니다. target=" +
                            target.ToString("F6") + ", actual=" + stage.CameraX.ActualPosition.ToString("F6"));
                    }

                    if (!controller.AreInputStageRunReviewPickersSafe(out pickerReason))
                    {
                        return Fail("SEQ-IN-REVIEW-CAMERA-PICKER-FINAL", "InputStage",
                            "Input Camera X Avoid 완료 후 통합 Picker 안전 조건 최종 확인에 실패했습니다. " +
                            pickerReason);
                    }

                    WriteLog("InputStageRunReview",
                        "확인 후 Input Camera X를 Avoid 위치로 복귀하고 최종 위치를 확인했습니다. target=" +
                        target.ToString("F6") + ", actual=" + stage.CameraX.ActualPosition.ToString("F6") + " - Ok");
                    return 0;
                }
            }
        }

        private async Task<int> EnsureInputPickersEmptyAvoidAndStoppedAsync(string holder, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string safeHolder = string.IsNullOrWhiteSpace(holder) ? "InputSequence" : holder;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested("InputSequence.PickerDrainGate:" + safeHolder);

                    string reason;
                    if (AreInputPickersEmptyAvoidAndStopped(out reason))
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputPickerDrainGate",
                                safeHolder + " 전 Front/Rear Picker 제품 배출 및 Avoid 정지 확인 완료. - Ok");
                        }
                        return 0;
                    }

                    if (Mode != SequenceRunMode.Auto)
                    {
                        return Fail("SEQ-IN-PICKER-DRAIN-STATE", "InputSequence",
                            safeHolder + " 불가: Front/Rear Picker가 Empty/Avoid/Stopped 상태가 아닙니다. " + reason);
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputPickerDrainGate",
                            safeHolder + " 전 기존 Picker 제품의 Output 배출과 Avoid 복귀를 기다립니다. reason=" +
                            reason + " - Wait");
                        LogPublic("[UNIT-INPUT-LOADER] WAIT Picker drain before " + safeHolder + ". " + reason);
                        waitLogged = true;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }
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
                return Fail("SEQ-IN-PICKER-DRAIN-EX", "InputSequence",
                    "Input loader 진입 전 Picker 제품 배출 확인 중 예외 발생. holder=" + holder +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool AreInputPickersEmptyAvoidAndStopped(out string reason)
        {
            if (!AreInputPickersAvoidAndStopped(out reason))
                return false;

            if (Context != null && Context.PickerPhases != null)
            {
                PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                if (snapshot.Front.Phase != PickerProcessPhase.Idle ||
                    snapshot.Rear.Phase != PickerProcessPhase.Idle)
                {
                    reason = "Picker phase가 Idle이 아닙니다. " + snapshot;
                    return false;
                }
            }

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial frontDie = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerFront,
                    pickerNo);
                if (frontDie != null)
                {
                    reason = "FrontPicker가 제품을 보유 중입니다. picker=" + pickerNo +
                             ", die=" + (frontDie.DieId ?? "");
                    return false;
                }

                DieMaterial rearDie = MaterialStateService.GetDieAtPicker(
                    MaterialLocationKind.PickerRear,
                    pickerNo);
                if (rearDie != null)
                {
                    reason = "RearPicker가 제품을 보유 중입니다. picker=" + pickerNo +
                             ", die=" + (rearDie.DieId ?? "");
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private bool AreInputPickersAvoidAndStopped(out string reason)
        {
            reason = string.Empty;

            var machine = Context != null ? Context.Machine : null;
            var front = machine != null ? machine.PickerFrontUnit : null;
            var rear = machine != null ? machine.PickerRearUnit : null;

            if (IsFrontPickerEnabled(front))
            {
                if (!front.IsFrontPickerInAvoidPosition())
                {
                    reason = "FrontPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(front.Axes, "FrontPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            if (IsRearPickerEnabled(rear))
            {
                if (!rear.IsRearPickerInAvoidPosition())
                {
                    reason = "RearPicker가 Avoid 위치가 아닙니다.";
                    return false;
                }

                string movingReason;
                if (TryDescribeMovingPickerAxes(rear.Axes, "RearPicker", out movingReason))
                {
                    reason = movingReason;
                    return false;
                }
            }

            return true;
        }

        private static bool IsFrontPickerEnabled(PickerFrontUnit front)
        {
            return front != null && front.Config != null && front.Config.UseUnit;
        }

        private static bool IsRearPickerEnabled(PickerRearUnit rear)
        {
            return rear != null && rear.Config != null && rear.Config.UseUnit;
        }

        private static bool TryDescribeMovingPickerAxes(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<PickerAxis, QMC.Common.Motion.BaseAxis>> axes, string pickerName, out string reason)
        {
            reason = string.Empty;
            if (axes == null)
                return false;

            foreach (var pair in axes)
            {
                if (pair.Value != null && pair.Value.IsMoving)
                {
                    reason = pickerName + " " + pair.Key + " 축이 이동 중입니다.";
                    return true;
                }
            }

            return false;
        }

        private InputFeederSequenceOptions BuildFeederSequenceOptions(
            int slotIndex,
            int nextSlotIndex,
            bool bFine,
            int moveTimeoutMs,
            SequenceStartMode startMode)
        {
            var options = InputFeederSequenceOptions.Default();
            options.SlotIndex = slotIndex;
            options.NextSlotIndex = nextSlotIndex;
            // To do: C4 - 현재 처리 레벨(_autoCassetteRole)을 우선 사용한다. 미확정(Input1 기본)일 때만 슬롯 기반 추정으로 보완.
            options.CassetteRole = _autoCassetteRole != CassetteMaterialRole.Input1
                ? _autoCassetteRole
                : ResolveInputCassetteRole(slotIndex);
            options.ExpectedWaferId = ResolveInputWaferId(slotIndex);
            options.WaferSize = ResolveInputWaferSize();
            options.FineMove = bFine;
            options.MoveTimeoutMs = moveTimeoutMs > 0 ? moveTimeoutMs : options.MoveTimeoutMs;
            options.RunMode = Mode;
            options.StartMode = startMode;
            return options;
        }

        private InputStageSequenceOptions BuildStageSequenceOptions(
            bool bFine,
            SequenceStartMode startMode,
            bool requireVisionAlign,
            string waferId,
            bool requireMapData)
        {
            var options = InputStageSequenceOptions.Default();
            options.FineMove = bFine;
            options.RunMode = Mode;
            options.StartMode = startMode;
            options.RequireVisionAlign = requireVisionAlign;
            options.WaferId = waferId ?? "";
            options.RequireMapData = requireMapData;
            ApplyInputStageUnitParameters(options);
            return options;
        }

        private void ApplyInputStageUnitParameters(InputStageSequenceOptions options)
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                if (stage == null || stage.Config == null || options == null)
                    return;

                if (stage.Config.SequenceMoveTimeoutMs > 0)
                    options.MoveTimeoutMs = stage.Config.SequenceMoveTimeoutMs;
                if (stage.Config.AlignConvergenceThresholdDeg > 0.0)
                    options.AlignThetaToleranceDeg = stage.Config.AlignConvergenceThresholdDeg;
                if (stage.Config.AlignThetaCorrectionLimitDeg > 0.0)
                    options.AlignThetaCorrectionLimitDeg = stage.Config.AlignThetaCorrectionLimitDeg;
                if (stage.Config.MaxAlignIterations > 0)
                    options.AlignRetryCount = stage.Config.MaxAlignIterations;
            }
            catch (Exception ex)
            {
                WriteLog("BuildStageSequenceOptions", "InputStage sequence option parameter apply failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private string ResolveInputWaferId(int slotIndex)
        {
            WaferMaterial wafer = ResolveFeederWaferFromRuntimeState();
            if (wafer == null)
                wafer = ResolveStageWaferFromRuntimeState();
            if (wafer != null && wafer.SourceSlotNumber == slotIndex)
                return wafer.WaferId ?? "";

            wafer = MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input1, slotIndex);
            if (wafer == null)
                wafer = MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input2, slotIndex);
            return wafer != null ? (wafer.WaferId ?? "") : "";
        }

        private CassetteMaterialRole ResolveInputCassetteRole(int slotIndex)
        {
            WaferMaterial wafer = ResolveFeederWaferFromRuntimeState();
            if (wafer == null)
                wafer = ResolveStageWaferFromRuntimeState();
            if (wafer != null &&
                wafer.SourceSlotNumber == slotIndex &&
                (wafer.SourceCassetteRole == CassetteMaterialRole.Input1 || wafer.SourceCassetteRole == CassetteMaterialRole.Input2))
                return wafer.SourceCassetteRole;

            if (MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input1, slotIndex) != null)
                return CassetteMaterialRole.Input1;
            if (MaterialStateService.GetWaferInCassette(CassetteMaterialRole.Input2, slotIndex) != null)
                return CassetteMaterialRole.Input2;
            return CassetteMaterialRole.Input1;
        }

        private void UpdateInputSlotState(int slotIndex, SlotPresence presence, ProcessState state)
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette != null && slotIndex >= 0)
                    // To do: C4 - 현재 처리 레벨(_autoCassetteRole)의 슬롯 상태를 갱신한다.
                    cassette.UpdateWaferCassetteSlotState(InputCassetteUnit.ResolveCassetteLevel(_autoCassetteRole), slotIndex, presence, state);
            }
            catch (Exception ex)
            {
                WriteLog("UpdateInputSlotState", "Input slot state update failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ClearInputStageRuntime()
        {
            try
            {
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                if (stage != null)
                {
                    stage.ClearCurrentWaferMaterial();
                    stage.ClearCurrentWaferMap();
                }

                // 현재 기준: InputStage가 비워질 때 이전 wafer active map도 같이 비워 다음 wafer와 섞이지 않게 한다.
                if (Context != null && Context.Controller != null)
                    Context.Controller.ClearInputDieMap("InputSequence.ClearInputStageRuntime");
                else
                    LotStorage.ActiveInputDieMap = null;
            }
            catch (Exception ex)
            {
                WriteLog("ClearInputStageRuntime", "Input stage runtime clear failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private int ResolveNextInputSlot()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null)
                    return -1;

                // To do: C4 - 1단 소진 후 2단. 선택된 슬롯의 레벨(role)을 함께 확정한다.
                CassetteMaterialRole role;
                int slotIndex = cassette.FindNextProcessWaferSlot(out role);
                if (slotIndex >= 0)
                    _autoCassetteRole = role;
                if (slotIndex < 0 && cassette.IsInputCassetteProcessComplete())
                {
                    cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                    NotifyInputCassetteReplacementRequired();
                }

                return slotIndex;
            }
            catch (Exception ex)
            {
                WriteLog("ResolveNextInputSlot", "Input next slot resolve failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private int ResolveCurrentOrNextInputSlot()
        {
            try
            {
                int slotIndex = ResolveProcessingInputSlot();
                if (slotIndex >= 0)
                    return slotIndex;

                return ResolveNextInputSlot();
            }
            catch (Exception ex)
            {
                WriteLog("ResolveCurrentOrNextInputSlot", "Input current/next slot resolve failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private int ResolveProcessingInputSlot()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null)
                    return -1;

                // To do: [맵핑 재설계] Processing 슬롯을 최상위 레벨(2단)부터 탐색한다(처리 순서 = 맨 위에서 아래로).
                int levelCount = cassette.ResolveCassetteLevelCount();
                for (int level = levelCount; level >= 1; level--)
                {
                    WaferCassetteMaterial material = cassette.GetWaferMaterialCassette(level);
                    if (material == null || material.Slots == null)
                        continue;

                    for (int i = 0; i < material.Slots.Count; i++)
                    {
                        WaferSlotState state = material.Slots[i];
                        if (state != null &&
                            state.Presence == SlotPresence.Exist &&
                            state.Process == ProcessState.Processing)
                        {
                            _autoCassetteRole = InputCassetteUnit.ResolveCassetteRole(level);
                            return i;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("ResolveProcessingInputSlot", "Input processing slot resolve failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private int ResolveInputWaferSize()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette != null && cassette.Config != null)
                    return MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect);
            }
            catch (Exception ex)
            {
                WriteLog("ResolveInputWaferSize", "Input wafer size 확인 중 예외가 발생했습니다: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return 12;
        }

        private int Fail(string alarmCode, string source, string message)
        {
            try
            {
                if (SequenceStopException.IsCycleStopMessage(message))
                {
                    WriteLog(source, message + " - Stopped");
                    LogPublic("[UNIT-INPUT-LOADER] STOP " + message);
                    throw new SequenceStopException(message);
                }

                message = SequenceFailureStore.AppendRecentDetail(message, "InputSequence", alarmCode);
                SequenceFailureStore.Record("InputSequence", Kind.ToString(), "", alarmCode, source, message);
                SequenceTrace.StepFail("InputSequence", _autoStep.ToString(), -1,
                    "alarm=" + alarmCode,
                    "source=" + source,
                    "message=" + message);
                WriteLog(source, message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
                LogPublic("[UNIT-INPUT-LOADER] FAIL " + alarmCode + " - " + message);
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(source, "실패 처리 중 예외가 발생했습니다: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return -1;
        }

        private int StopAutoSequence(string reason)
        {
            try
            {
                WriteLog("InputSequence", "Input 시퀀스 정지: " + reason + " - Stopped");
                LogPublic("[UNIT-INPUT-LOADER] STOP " + reason);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence StopAutoSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            throw new SequenceStopException(reason);
        }

        private void NotifyInputCassetteReplacementRequired()
        {
            try
            {
                Context.RequestOperatorMessage(
                    "입력 카세트 교체",
                    "입력 카세트의 모든 웨이퍼 작업이 완료되었습니다.\r\n카세트를 교체한 뒤 필요한 작업을 진행하세요.");
            }
            catch (Exception ex)
            {
                WriteLog("NotifyInputCassetteReplacementRequired",
                    "입력 카세트 교체 메시지 표시 요청 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void LogPublic(string message)
        {
            try
            {
                Context.LogPublic(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence public log failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                Log.Write("Main", "SYSTEM", source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InputSequence log failed: " + ex.Message);
            }
            finally
            {
            }

            // 시퀀스 로그를 이력(EventLogger)에도 분류 기록(스코프 Kind 또는 메시지 접두어 라우팅).
            SequenceLog.EmitTrace(QMC.Common.Logging.EventKind.InputSeq, source, message);
        }
    }
}

