using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing.Calibration;
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

    // 스텝 본문은 partial 파일로 분리되어 있다(순수 추출, 동작 변경 없음. 2026-07-27):
    //   InputSequence.Steps.Load.cs    Mapping / ResolveSlot / PrepareStageLoad / LoadFeeder* / RecoverFeeder
    //   InputSequence.Steps.Align.cs   AlignStage / DieMapping
    //   InputSequence.Steps.Review.cs  ReviewStage / Complete
    public partial class InputSequence : UnitSequenceBase
    {
        // 하위 시퀀스/step에서 이미 Fail()로 Alarm을 발생시킨 실패를 상위 계층이 중복 Alarm 없이
        // 전파하기 위한 내부 예외입니다. 동일 실패가 step -> cycle -> auto 순서로 세 번 Alarm되던
        // cascade를 막는다. (규칙: 동일 실패의 중복 Alarm 금지)
        private sealed class StepAlreadyAlarmedException : Exception
        {
            public StepAlreadyAlarmedException(string message)
                : base(message)
            {
            }

            public StepAlreadyAlarmedException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private const string InputLoaderActiveSignal = "InputLoaderActive";
        // 무한 대기 진단용: Auto 대기 루프가 무언정지처럼 보이지 않도록 주기적으로 상태를 남기는 간격.
        private const int AutoWaitStatusLogIntervalMs = 30000;
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
            catch (StepAlreadyAlarmedException ex)
            {
                // 하위 step/사이클에서 이미 Alarm을 발생시킨 실패이므로 중복 Alarm 없이 전파만 한다.
                WriteLog("ExecuteAutoAsync", "Input 자동 시퀀스가 하위 실패로 중단되었습니다. " + ex.Message + " - Failed");
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

                        // 자동 콜렛 클리닝 실행 창: "새 웨이퍼 로딩 완료 후, 첫 Pick 전".
                        // 이 lease를 쥐고 있는 동안에는 Picker 신규 공정이 진입하지 못하고 NG Bin이 Stage에 있으므로
                        // 클리닝이 안전하게 수행될 수 있는 유일한 구간이다.
                        if (stageWafer != null && Mode == SequenceRunMode.Auto)
                        {
                            ColletCleaningTriggerService.NotifyWaferExchanged();
                            int cleaningResult = await ColletCleaningTriggerService
                                .RunIfTriggeredAsync(Context, ct)
                                .ConfigureAwait(false);
                            if (cleaningResult != 0)
                                // 클리닝 시퀀스 내부 Fail()이 이미 Alarm을 발생시켰으므로 중복 없이 전파한다.
                                throw new StepAlreadyAlarmedException(
                                    "자동 콜렛 클리닝 실패. result=" + cleaningResult);
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
                bool chainedNextCassetteLoad = false;
                using (AutoSequenceLoaderWorkLease unloadLease = await Context.AutoLoaderGate
                    .BeginInputWorkAsync(
                        "InputStageUnloadCycle",
                        ct,
                        EnsureInputPickersEmptyAvoidAndStoppedAsync,
                        AreInputPickersEmptyAvoidAndStopped)
                    .ConfigureAwait(false))
                {
                    // 연속 이송: 언로드 직후 다음 슬롯 로딩이 이어질 수 있으면 리프터를 Avoid로 되돌리지 않고
                    // 같은 lease 안에서 슬롯 -> 슬롯 직행으로 진행한다.
                    bool canChain = CanChainNextInputCassetteLoad();
                    await UnloadInputStageWaferIfPresentAsync(ct, canChain).ConfigureAwait(false);

                    if (canChain)
                        chainedNextCassetteLoad = await TryChainNextInputCassetteLoadAsync(ct).ConfigureAwait(false);
                }

                // 모든 Input Cassette slot 처리가 끝났으면 알람/메시지를 띄우고 Auto를 정지한다.
                int completeResult = StopAutoSequenceIfInputCassetteComplete();
                if (completeResult != 0)
                    return;

                // 연속 로딩으로 이미 다음 wafer의 카세트 이송까지 진행했으면 진행 상태를 유지한다.
                // (다음 cycle 진입 시 RestoreInputStepSessionFromRuntimeState가 남은 step부터 재개한다.)
                if (chainedNextCassetteLoad)
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
            catch (StepAlreadyAlarmedException ex)
            {
                // 하위 step에서 이미 Alarm을 발생시킨 실패이므로 로그만 남기고 전파한다.
                WriteLog("ExecuteInputAutoCycleAsync",
                    "Input 자동 사이클이 하위 step 실패로 중단되었습니다. " + ex.Message + " - Failed");
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-CYCLE", "InputSequence", "Input 자동 사이클 실패: " + ex.Message);
                // 이 계층에서 Alarm을 확정했으므로 상위(ExecuteAutoAsync)에서는 중복 Alarm 없이 전파만 한다.
                throw new StepAlreadyAlarmedException("Input 자동 사이클 실패: " + ex.Message, ex);
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
            catch (StepAlreadyAlarmedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-STAGE-FINISH-RECOVER", "InputSequence",
                    "InputStage PickUp 준비 복구 실패: " + ex.Message);
                throw new StepAlreadyAlarmedException("InputStage PickUp 준비 복구 실패: " + ex.Message, ex);
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
                    // 실패 step 내부의 Fail()이 이미 Alarm을 발생시켰으므로 상위에는 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException("Input 자동 시퀀스 실패. step=" + _autoStep + ", result=" + result);
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

                // 주의: 여기서 InputStageDieComplete를 Reset하지 않는다.
                // 위의 IsSet 확인과 Reset 사이에 Picker가 마지막 Pick 완료 신호를 올리면
                // Reset이 유효한 완료 신호를 지워 영구 대기(무언정지)가 되는 race가 있었다.
                // 이전 wafer의 잔류 신호는 사이클 종료(ResetInputAutoCycle)와
                // 언로드 완료 시점의 ResetInputStageCycleSignals로 차단한다.
                PublishInputStageReadySignals(stageWafer);

                // 정상 운전은 Picker Sequence가 마지막 Pick 안전 복귀 후 완료 신호를 발행한다.
                // Material 상태 기반 완료 신호 복구는 Ready 상태를 복원한 재시작 경로에서만 허용한다.
                // wafer 전체 die pick은 생산 길이 대기이므로 고정 timeout 대신 주기 상태 로그로 무언정지를 진단한다.
                int waitStatusTick = Environment.TickCount;
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

                    if (unchecked(Environment.TickCount - waitStatusTick) >= AutoWaitStatusLogIntervalMs)
                    {
                        waitStatusTick = Environment.TickCount;
                        WriteLog("WaitPickerToCompleteInputStageDiesAsync",
                            "InputStage Die Pick 완료 신호를 대기 중입니다. wafer=" +
                            (stageWafer != null ? stageWafer.WaferId : "-") +
                            ", pickerPhases=" + DescribePickerPhases() + " - Wait");
                    }

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
            catch (StepAlreadyAlarmedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-PICK-WAIT", "InputSequence",
                    "InputStage Die Pick 완료 대기 실패: " + ex.Message);
                throw new StepAlreadyAlarmedException("InputStage Die Pick 완료 대기 실패: " + ex.Message, ex);
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

        // 완료 대기 주기 로그용: 현재 Front/Rear Picker phase 상태를 문자열로 요약한다.
        private string DescribePickerPhases()
        {
            try
            {
                if (Context == null || Context.PickerPhases == null)
                    return "-";

                return Context.PickerPhases.GetSnapshot().ToString();
            }
            catch (Exception ex)
            {
                return "phaseResolveFailed=" + ex.Message;
            }
            finally
            {
            }
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

        private async Task UnloadInputStageWaferIfPresentAsync(CancellationToken ct, bool keepCassetteAtSlotForNextAccess = false)
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
                    SequenceStartMode.Resume,
                    keepCassetteAtSlotForNextAccess).ConfigureAwait(false);

                if (result != 0)
                    // 언로드 하위 시퀀스의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException("InputStage 웨이퍼 자동 언로딩 실패. slot=" + slotIndex + ", result=" + result);
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
            catch (StepAlreadyAlarmedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-AUTO-UNLOAD", "InputSequence", "Input 자동 웨이퍼 언로딩 실패: " + ex.Message);
                throw new StepAlreadyAlarmedException("Input 자동 웨이퍼 언로딩 실패: " + ex.Message, ex);
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
                    // 하위 시퀀스의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException(
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
                    // 하위 시퀀스의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException(
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
            catch (StepAlreadyAlarmedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("SEQ-IN-RESUME-UNLOAD-EX", "InputSequence", "Feeder 잔류 언로드 재개 실패: " + ex.Message);
                throw new StepAlreadyAlarmedException("Feeder 잔류 언로드 재개 실패: " + ex.Message, ex);
            }
            finally
            {
                ResetInputLoaderActive(loaderActive, "ResumeFeederUnloadToCassette");
            }
        }

        // 연속 이송 가능 판정: 언로드 직후 같은 Loader lease 안에서 다음 슬롯 로딩을 이어갈 수 있는지 본다.
        // 조건을 하나라도 만족하지 못하면 기존처럼 리프터를 Avoid로 되돌리고 lease를 정상 종료한다.
        private bool CanChainNextInputCassetteLoad()
        {
            try
            {
                // Auto 연속 운전에서만 사용한다. Manual/Step은 한 동작 단위로 끝나야 하므로 항상 Avoid 복귀.
                if (Mode != SequenceRunMode.Auto)
                    return false;

                // 정지 요청/배출 완료 요청 중이면 다음 wafer를 새로 꺼내지 않는다.
                if (Context == null || Context.IsCycleStopRequested)
                    return false;

                WaferCompletionRunCoordinator completion = Context.WaferCompletion;
                if (completion != null && completion.Enabled)
                {
                    completion.ObserveCompletionSignals();
                    if (completion.IsDrainRequested)
                        return false;
                }

                // 다음에 처리할 Ready 슬롯이 실제로 있어야 한다. (부작용 없는 조회만 사용)
                var cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null || !cassette.HasMoreProcessWafer())
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                WriteLog("CanChainNextInputCassetteLoad",
                    "연속 이송 가능 판정 중 예외가 발생해 기존 Avoid 복귀 경로를 사용합니다. error=" + ex.Message + " - Check");
                return false;
            }
            finally
            {
            }
        }

        // 같은 Loader lease 안에서 다음 슬롯의 카세트 이송 구간만 이어서 수행한다.
        // ResolveSlot -> PrepareStageLoad -> LoadFeederFromCassette -> LoadFeederToStage 까지 진행하며,
        // LoadFeederToStage가 끝나면 리프터가 Avoid로 복귀하므로 lease를 놓아도 Picker X 이동이 안전하다.
        // 이후 남은 step(RecoverFeeder/Align/DieMapping/Review)은 다음 cycle에서 재개한다.
        private async Task<bool> TryChainNextInputCassetteLoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            ResetInputAutoCycle();
            WriteLog("TryChainNextInputCassetteLoadAsync",
                "언로드 직후 같은 Loader 승인 안에서 다음 슬롯 로딩을 이어서 수행합니다. " +
                "(카세트 리프터 Avoid 복귀 생략, 슬롯 -> 슬롯 직행) - Start");

            while (IsInputCassetteTransferStep(_autoStep))
            {
                ct.ThrowIfCancellationRequested();
                int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                if (result != 0)
                    // 실패 step 내부의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException(
                        "연속 로딩 실패. step=" + _autoStep + ", result=" + result);
            }

            // 카세트 이송 구간이 끝났으면 리프터는 LoadFeederToStage에서 Avoid로 복귀한 상태다.
            string cassetteState = DescribeInputLifterAvoidState();
            WriteLog("TryChainNextInputCassetteLoadAsync",
                "연속 로딩의 카세트 이송 구간을 완료했습니다. nextStep=" + _autoStep +
                ", slot=" + _autoSlotIndex + ", wafer=" + _autoWaferId +
                ", " + cassetteState + " - Ok");
            return true;
        }

        private static bool IsInputCassetteTransferStep(InputSequenceAutoStep step)
        {
            return step == InputSequenceAutoStep.ResolveSlot ||
                   step == InputSequenceAutoStep.PrepareStageLoad ||
                   step == InputSequenceAutoStep.LoadFeederFromCassette ||
                   step == InputSequenceAutoStep.LoadFeederToStage;
        }

        private string DescribeInputLifterAvoidState()
        {
            try
            {
                var cassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                if (cassette == null || cassette.InputLifterZ == null)
                    return "InputLifterZ=null";

                return "InputLifterZ[actual=" + cassette.InputLifterZ.ActualPosition.ToString("F3") +
                       ", atAvoid=" + (cassette.IsWaferLifterZInAvoidPosition() ? "Y" : "N") + "]";
            }
            catch (Exception ex)
            {
                return "InputLifterZ[stateFailed=" + ex.Message + "]";
            }
            finally
            {
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
                    // 실패 step 내부의 Fail()이 이미 Alarm을 발생시켰으므로 중복 Alarm 없이 전파한다.
                    throw new StepAlreadyAlarmedException("Input 수동/스텝 시퀀스 실패. step=" + _autoStep + ", result=" + result);
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
            catch (StepAlreadyAlarmedException ex)
            {
                WriteLog("ExecuteStepAsync", "Input 수동/스텝 시퀀스가 하위 step 실패로 중단되었습니다. " + ex.Message + " - Failed");
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

                // GYN 2026.07.03 TODO 반영: 여기서는 재개 위치(step) 판정만 수행한다.
                // 재개 Step에 필요한 인터락/축 상태/자재 정합성 재확인은 각 Step 실행 직전
                // CheckInputStepInterlocksBeforeExecute에서 공통 수행하며, 안전 이동으로 해결 가능한
                // 경우(빈 피더 미후퇴)는 RecoverFeeder로 보정하고 그 외에는 알람 발생 후 정지한다.
                // Picker 안전 조건은 각 Step의 ExecuteWithInputPickerAvoidGateAsync/드레인 게이트가 확인한다.

                // 1순위: Stage에 wafer가 있으면 Stage 처리 상태(Align/DieMapping/Complete)를 기준으로 재개한다.
                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(stageWafer);
                    _autoWaferId = stageWafer.WaferId ?? "";
                    string sourceRestoreReason;
                    if (!TryRestoreActiveInputWaferSourceSlotProjection(
                        stageWafer,
                        "InputStageRestore",
                        out sourceRestoreReason))
                    {
                        Fail(
                            "SEQ-IN-ACTIVE-WAFER-SOURCE",
                            "InputSequence",
                            sourceRestoreReason);
                        throw new StepAlreadyAlarmedException(sourceRestoreReason);
                    }
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

                //Todo : Feeder에 wafer가 있으면...Ready 상태인지 확인하고, Ready 상태가 아니면 RecoverFeeder부터 재개하도록 보정한다.
                // Feeder에 wafer 가지고 있으면 사용자가 조치 후 장비 런일텐데...

                // 2순위: Feeder에 wafer가 있으면 이송 방향을 판별해 재개한다.
                //  - 로드(Cassette->Feeder->Stage) 진행 중: 아직 스테이지를 거치지 않아 Align 결과가 없다 -> Stage로 전진.
                //  - 언로드(Stage->Feeder->Cassette) 진행 중: 이미 스테이지에서 처리(Align 완료)된 wafer가 되돌아오는 중이다
                //    -> Stage로 전진시키면 완료 wafer를 재처리/역주행하므로, 원래 Cassette로 되돌리는 언로드를 이어서 수행한다.
                WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
                if (feederWafer != null)
                {
                    _autoSlotIndex = ResolveSlotIndexFromWafer(feederWafer);
                    _autoWaferId = feederWafer.WaferId ?? "";
                    string sourceRestoreReason;
                    if (!TryRestoreActiveInputWaferSourceSlotProjection(
                        feederWafer,
                        "InputFeederRestore",
                        out sourceRestoreReason))
                    {
                        Fail(
                            "SEQ-IN-ACTIVE-WAFER-SOURCE",
                            "InputSequence",
                            sourceRestoreReason);
                        throw new StepAlreadyAlarmedException(sourceRestoreReason);
                    }

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
            catch (StepAlreadyAlarmedException)
            {
                throw;
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

        private bool TryRestoreActiveInputWaferSourceSlotProjection(
            WaferMaterial wafer,
            string restoreContext,
            out string reason)
        {
            try
            {
                reason = "";
                string materialReason;
                if (!MaterialStateService.TryValidateActiveInputWaferSourceState(
                    wafer,
                    out materialReason))
                {
                    reason = "활성 Input wafer의 원본 Cassette/Slot 정보가 유효하지 않습니다. context=" +
                             (restoreContext ?? "") +
                             ", detail=" + materialReason;
                    WriteLog("RestoreInputSourceSlot", reason + " - Failed");
                    return false;
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
                        ", slot=" + wafer.SourceSlotNumber + " - Failed");
                    reason = "활성 Input wafer의 원본 Cassette Material mapping이 유효하지 않습니다. context=" +
                             (restoreContext ?? "") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", role=" + wafer.SourceCassetteRole +
                             ", slot=" + wafer.SourceSlotNumber;
                    return false;
                }

                if (cassetteMaterial.Slots == null ||
                    wafer.SourceSlotNumber >= cassetteMaterial.Slots.Count)
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore skipped because source slot is out of range. context=" +
                        (restoreContext ?? "") +
                        ", wafer=" + (wafer.WaferId ?? "") +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber +
                        ", slotCount=" +
                        (cassetteMaterial.Slots != null ? cassetteMaterial.Slots.Count : 0) +
                        " - Failed");
                    reason = "활성 Input wafer의 원본 Slot이 Material 범위를 벗어났습니다. context=" +
                             (restoreContext ?? "") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", role=" + wafer.SourceCassetteRole +
                             ", slot=" + wafer.SourceSlotNumber +
                             ", slotCount=" +
                             (cassetteMaterial.Slots != null ? cassetteMaterial.Slots.Count : 0);
                    return false;
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
                    reason = "활성 Input wafer의 원본 Slot이 다른 wafer 정보로 점유되어 있습니다. context=" +
                             (restoreContext ?? "") +
                             ", activeWafer=" + (wafer.WaferId ?? "") +
                             ", cassetteWafer=" + (cassetteWafer != null ? cassetteWafer.WaferId : "") +
                             ", role=" + wafer.SourceCassetteRole +
                             ", slot=" + wafer.SourceSlotNumber;
                    return false;
                }

                InputCassetteUnit cassette = Context != null && Context.Machine != null
                    ? Context.Machine.InputCassetteUnit
                    : null;
                if (cassette == null ||
                    cassette.Config == null ||
                    wafer.SourceSlotNumber >= cassette.Config.SlotCount)
                {
                    reason = "활성 Input wafer의 원본 Slot을 Unit projection에 복원할 수 없습니다. context=" +
                             (restoreContext ?? "") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", role=" + wafer.SourceCassetteRole +
                             ", slot=" + wafer.SourceSlotNumber +
                             ", unitNull=" + (cassette == null) +
                             ", configNull=" + (cassette == null || cassette.Config == null);
                    WriteLog("RestoreInputSourceSlot", reason + " - Failed");
                    return false;
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
                    return true;
                }

                // 기존 조건: Presence != Empty 이면 모두 차단 - 앱 재시작 직후 Unit projection의 초기값이
                //           바로 Unknown이라, "재시작 복구용" 이 함수가 정작 재시작 때는 항상 스킵되었다.
                //           그 결과 projection이 Unknown으로 남아 언로드 슬롯 검사(IsUnloadSlotEmpty)가
                //           빈 슬롯을 점유로 오판했다. (실장비 2026-07-25 21:42, CYCLE RUN INPUT UNLOAD)
                // 현재 기준: Unknown은 "점유"가 아니라 "정보 없음"이므로 Empty와 함께 복원 대상으로 본다.
                //           스캔으로 점유가 확인된 Exist는 실제 자재 불일치 가능성이 있어 계속 차단한다.
                //           (이 시점까지 Material 근거는 모두 확인됨: active wafer가 Stage/Feeder에 있고,
                //            원본 role/slot이 유효하며, 카세트가 Enabled/Present/Mapped이고,
                //            해당 Material slot과 cassette wafer가 비어 있음)
                if (previousState != null &&
                    previousState.Presence == SlotPresence.Exist)
                {
                    WriteLog("RestoreInputSourceSlot",
                        "Active wafer source slot projection restore blocked by occupied unit slot state. context=" +
                        (restoreContext ?? "") +
                        ", wafer=" + (wafer.WaferId ?? "") +
                        ", role=" + wafer.SourceCassetteRole +
                        ", slot=" + wafer.SourceSlotNumber +
                        ", presence=" + previousState.Presence +
                        ", process=" + previousState.Process + " - Failed");
                    reason = "활성 Input wafer의 원본 Unit Slot이 이미 점유되어 복원할 수 없습니다. context=" +
                             (restoreContext ?? "") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", role=" + wafer.SourceCassetteRole +
                             ", slot=" + wafer.SourceSlotNumber +
                             ", presence=" + previousState.Presence +
                             ", process=" + previousState.Process;
                    return false;
                }

                // 앱 재시작 시 Unit의 휘발성 slot projection은 사라지지만(Unknown), 저장된 active Material과
                // 비어 있는 원본 Material slot은 유지된다. Empty/Unknown일 때만 정상 로드 완료 상태를
                // 복원하며, Exist는 실제 점유 불일치 가능성이 있으므로 안전 실패한다.
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
                return true;
            }
            catch (Exception ex)
            {
                reason = "활성 Input wafer 원본 Slot projection 복원 중 예외가 발생했습니다. context=" +
                         (restoreContext ?? "") +
                         ", wafer=" + (wafer != null ? wafer.WaferId : "") +
                         ", error=" + ex.Message;
                WriteLog("RestoreInputSourceSlot", reason + " - Failed");
                return false;
            }
        }

        // To do: C4 반영 - 매핑 판정을 1단(Input1)/2단(Input2) 동일 기준으로 수행한다.
        // Mapping 시퀀스는 한 번의 실행으로 구성된 모든 레벨을 스캔/등록하므로
        // (RegisterMappingResult -> UpdateInputCassetteMapping level1/level2),
        // 사용(Enabled/Present) 레벨 중 하나라도 미맵핑이면 Mapping부터 다시 시작하게 false를 반환한다.
        private bool IsInputCassetteMappedInRuntimeState()
        {
            try
            {
                var inputCassette = Context != null && Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
                int levelCount = inputCassette != null ? inputCassette.ResolveCassetteLevelCount() : 1;
                if (levelCount < 1)
                    levelCount = 1;

                bool anyMaterialState = false;
                bool anyMappedUsableLevel = false;
                if (MaterialStateService.State != null && MaterialStateService.State.Cassettes != null)
                {
                    for (int level = 1; level <= levelCount; level++)
                    {
                        CassetteMaterialRole role = InputCassetteUnit.ResolveCassetteRole(level);
                        CassetteMaterial state = MaterialStateService.State.Cassettes
                            .FirstOrDefault(c => c != null && c.Role == role);
                        if (state == null)
                            continue;

                        anyMaterialState = true;

                        // 사용하지 않는(비활성/미장착) 레벨은 매핑을 요구하지 않는다.
                        if (!state.IsEnabled || !state.IsPresent)
                            continue;

                        // 사용 레벨 중 하나라도 미맵핑이면 Mapping부터 다시 시작한다.
                        if (!state.IsMapped)
                            return false;

                        anyMappedUsableLevel = true;
                    }
                }

                // Material cassette가 존재하면 그 상태를 단일 기준으로 사용한다.
                // Clear All로 IsMapped가 내려간 뒤 WaferMap의 고정 slot 개수만 보고
                // mapping 완료로 오인하지 않도록 한다.
                if (anyMaterialState)
                    return anyMappedUsableLevel;

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
            // 재개를 포함한 모든 실행에서 Step 시작 직전에 인터락/자재 정합성을 다시 확인한다.
            // 안전 이동으로 해결 가능한 경우(빈 피더 미후퇴)는 _autoStep을 RecoverFeeder로 보정하므로
            // executingStep 캡처보다 먼저 수행한다. (GYN 2026.07.03 재개 인터락 TODO 구현)
            int interlockResult = CheckInputStepInterlocksBeforeExecute();
            if (interlockResult != 0)
                return interlockResult;

            InputSequenceAutoStep executingStep = _autoStep;
            bool loaderActiveStep = IsInputLoaderActiveAutoStep(executingStep);
            bool stepSucceeded = false;
            try
            {
                ct.ThrowIfCancellationRequested();
                WriteLog("ExecuteCurrentInputStepAsync", "Input sequence step start. step=" + _autoStep + " - Start");
                if (Mode != SequenceRunMode.Auto)
                    SetInputLoaderActive(loaderActiveStep, executingStep.ToString());

                // 스텝 본문은 partial 파일(InputSequence.Steps.*.cs)로 분리되어 있다.
                int result = await DispatchInputStepAsync(ct, requireVisionAlign).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

        // 현재 스텝 하나를 실행한다. 0이면 정상 진행(다음 스텝은 각 스텝 메서드가 _autoStep으로 지정),
        // 0이 아니면 상위에서 그대로 반환해 사이클을 중단한다.
        // 각 스텝 본문은 아래 partial 파일에 있다.
        //   InputSequence.Steps.Load.cs    [1]~[6] Mapping ~ RecoverFeeder
        //   InputSequence.Steps.Align.cs   [7]~[8] AlignStage / DieMapping
        //   InputSequence.Steps.Review.cs  [9]~[10] ReviewStage / Complete
        private async Task<int> DispatchInputStepAsync(CancellationToken ct, bool requireVisionAlign)
        {
            switch (_autoStep)
            {
                case InputSequenceAutoStep.Mapping:
                    return await ExecuteStepMappingAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.ResolveSlot:
                    return ExecuteStepResolveSlot();

                case InputSequenceAutoStep.PrepareStageLoad:
                    return await ExecuteStepPrepareStageLoadAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.LoadFeederFromCassette:
                    return await ExecuteStepLoadFeederFromCassetteAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.LoadFeederToStage:
                    return await ExecuteStepLoadFeederToStageAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.RecoverFeeder:
                    return await ExecuteStepRecoverFeederAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.AlignStage:
                    return await ExecuteStepAlignStageAsync(ct, requireVisionAlign).ConfigureAwait(false);

                case InputSequenceAutoStep.DieMapping:
                    return await ExecuteStepDieMappingAsync(ct, requireVisionAlign).ConfigureAwait(false);

                case InputSequenceAutoStep.ReviewStage:
                    return await ExecuteStepReviewStageAsync(ct).ConfigureAwait(false);

                case InputSequenceAutoStep.Complete:
                    return ExecuteStepComplete();

                default:
                    return Fail("SEQ-IN-STEP-UNKNOWN", "InputSequence", "알 수 없는 Input 시퀀스 스텝입니다. step=" + _autoStep);
            }
        }

        // 재개/스텝 실행 직전 인터락 재확인 (GYN 2026.07.03 TODO 구현):
        // - 해당 Step이 사용하는 축의 Alarm/Servo/Home/이동 상태를 확인한다.
        // - Step이 전제하는 자재 배치(Feeder/Stage wafer 유무)를 영속 Material 기준으로 확인한다.
        // - 안전 이동으로 해결 가능한 경우(빈 피더가 후퇴하지 않은 상태에서 Stage 모션 Step 진입)는
        //   RecoverFeeder로 재라우팅해 안전 후퇴부터 수행하고, 그 외 불일치는 모션을 시작하지 않고
        //   알람으로 정지한다(fail-closed).
        private int CheckInputStepInterlocksBeforeExecute()
        {
            try
            {
                var machine = Context != null ? Context.Machine : null;
                var feeder = machine != null ? machine.InputFeederUnit : null;
                var stage = machine != null ? machine.InputStageUnit : null;
                var cassette = machine != null ? machine.InputCassetteUnit : null;

                var axes = new List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>>();
                string reason;
                WaferMaterial feederWafer;
                WaferMaterial stageWafer;

                switch (_autoStep)
                {
                    // 슬롯 결정은 계산만 수행하고, Complete는 모션이 없다.
                    case InputSequenceAutoStep.ResolveSlot:
                    case InputSequenceAutoStep.Complete:
                        return 0;

                    // Mapping: 카세트 리프터 축 상태만 확인한다. 자재/센서 조건은 하위 Mapping 시퀀스가 확인한다.
                    case InputSequenceAutoStep.Mapping:
                        AddAxisIfPresent(axes, "InputLifterZ", cassette != null ? cassette.InputLifterZ : null);
                        AddAxisIfPresent(axes, "InputFeederY", feeder != null ? feeder.FeederY : null);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                "Mapping 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        return 0;

                    // PrepareStageLoad: Stage가 새 wafer를 받는 준비이므로 Stage에 자재가 없어야 한다.
                    case InputSequenceAutoStep.PrepareStageLoad:
                        CollectInputStageAxes(axes, stage);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                "PrepareStageLoad 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        stageWafer = ResolveStageWaferFromRuntimeState();
                        if (stageWafer != null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                "PrepareStageLoad 시작 전 InputStage에 wafer가 남아 있습니다. wafer=" +
                                (stageWafer.WaferId ?? "") + ". 언로드 또는 자재 상태 복구 후 다시 시작하세요.");
                        return 0;

                    // LoadFeederFromCassette: Feeder/Stage 모두 비어 있어야 새 wafer를 꺼낼 수 있다.
                    case InputSequenceAutoStep.LoadFeederFromCassette:
                        AddAxisIfPresent(axes, "InputFeederY", feeder != null ? feeder.FeederY : null);
                        AddAxisIfPresent(axes, "InputLifterZ", cassette != null ? cassette.InputLifterZ : null);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                "LoadFeederFromCassette 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        feederWafer = ResolveFeederWaferFromRuntimeState();
                        if (feederWafer != null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                "LoadFeederFromCassette 시작 전 InputFeeder에 이미 wafer가 있습니다. wafer=" +
                                (feederWafer.WaferId ?? "") + ". 자재 상태 복구 후 다시 시작하세요.");
                        stageWafer = ResolveStageWaferFromRuntimeState();
                        if (stageWafer != null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                "LoadFeederFromCassette 시작 전 InputStage에 wafer가 남아 있습니다. wafer=" +
                                (stageWafer.WaferId ?? "") + ". 기존 wafer 언로드 후 다시 시작하세요.");
                        return 0;

                    // LoadFeederToStage: Feeder에 wafer가 있고 Stage는 비어 있어야 한다.
                    case InputSequenceAutoStep.LoadFeederToStage:
                        AddAxisIfPresent(axes, "InputFeederY", feeder != null ? feeder.FeederY : null);
                        CollectInputStageAxes(axes, stage);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                "LoadFeederToStage 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        feederWafer = ResolveFeederWaferFromRuntimeState();
                        if (feederWafer == null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                "LoadFeederToStage 시작 전 InputFeeder에 이송할 wafer가 없습니다. 자재 상태를 확인하세요.");
                        stageWafer = ResolveStageWaferFromRuntimeState();
                        if (stageWafer != null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                "LoadFeederToStage 시작 전 InputStage에 다른 wafer가 있습니다. stageWafer=" +
                                (stageWafer.WaferId ?? "") + ", feederWafer=" + (feederWafer.WaferId ?? "") +
                                ". 자재 상태 복구 후 다시 시작하세요.");
                        return 0;

                    // RecoverFeeder: 안전 후퇴 동작이므로 축 상태만 확인한다.
                    case InputSequenceAutoStep.RecoverFeeder:
                        AddAxisIfPresent(axes, "InputFeederY", feeder != null ? feeder.FeederY : null);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                "RecoverFeeder 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        return 0;

                    // AlignStage/DieMapping: Stage에 wafer가 있어야 하고, Stage Y/T/Camera 모션 전에
                    // InputFeeder가 후퇴(Avoid)해 있어야 한다(Align-into-Feeder 충돌 방지).
                    case InputSequenceAutoStep.AlignStage:
                    case InputSequenceAutoStep.DieMapping:
                        AddAxisIfPresent(axes, "InputFeederY", feeder != null ? feeder.FeederY : null);
                        CollectInputStageAxes(axes, stage);
                        if (!AreInputAxesReadyForStep(axes, out reason))
                            return Fail("SEQ-IN-ILK-AXIS", "InputSequence",
                                _autoStep + " 시작 전 축 인터락 조건이 맞지 않습니다. " + reason);
                        stageWafer = ResolveStageWaferFromRuntimeState();
                        if (stageWafer == null)
                            return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                _autoStep + " 시작 전 InputStage에 wafer가 없습니다. 자재 상태를 확인하세요.");
                        if (!IsInputFeederRetractedForStageMotion())
                        {
                            feederWafer = ResolveFeederWaferFromRuntimeState();
                            if (feederWafer != null)
                                return Fail("SEQ-IN-ILK-MATERIAL", "InputSequence",
                                    _autoStep + " 시작 전 InputFeeder가 wafer를 보유한 채 후퇴하지 않았습니다. " +
                                    "Stage/Feeder 자재 이중 배치 가능성이 있어 자동 진행하지 않습니다. " +
                                    "stageWafer=" + (stageWafer.WaferId ?? "") +
                                    ", feederWafer=" + (feederWafer.WaferId ?? "") +
                                    ". 자재 상태 복구 후 다시 시작하세요.");

                            // 빈 피더 미후퇴는 안전 이동(피더 후퇴)으로 해결 가능하므로 RecoverFeeder부터 수행한다.
                            WriteLog("CheckInputStepInterlocks",
                                _autoStep + " 시작 전 InputFeeder가 후퇴 상태가 아니어서 RecoverFeeder를 먼저 실행합니다. " +
                                "(Align-into-Feeder 충돌 방지) - Check");
                            _autoStep = InputSequenceAutoStep.RecoverFeeder;
                        }
                        return 0;

                    // ReviewStage: 모션 없는 사용자 확인 단계이며 자재/결과/픽커 조건은 스텝 본문이 확인한다.
                    case InputSequenceAutoStep.ReviewStage:
                        return 0;

                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-ILK-EX", "InputSequence",
                    "Step 시작 전 인터락 재확인 중 예외가 발생했습니다. step=" + _autoStep + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static void AddAxisIfPresent(
            List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>> axes,
            string name,
            QMC.Common.Motion.BaseAxis axis)
        {
            if (axes != null && axis != null)
                axes.Add(new KeyValuePair<string, QMC.Common.Motion.BaseAxis>(name, axis));
        }

        private static void CollectInputStageAxes(
            List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>> axes,
            InputStageUnit stage)
        {
            if (stage == null)
                return;

            AddAxisIfPresent(axes, "InputStageY", stage.StageY);
            AddAxisIfPresent(axes, "InputStageT", stage.StageT);
            AddAxisIfPresent(axes, "InputVisionX", stage.CameraX);
            AddAxisIfPresent(axes, "ExpanderZ", stage.ExpanderZ);
            AddAxisIfPresent(axes, "NeedleZ", stage.NeedleZ);
            AddAxisIfPresent(axes, "EjectPinZ", stage.EjectPinZ);
            AddAxisIfPresent(axes, "NeedleBlockX", stage.NeedleBlockX);
        }

        // Step에서 사용할 축의 공통 안전 조건: 알람 없음, 서보 ON, 원점 복귀 완료, 정지 상태.
        private static bool AreInputAxesReadyForStep(
            List<KeyValuePair<string, QMC.Common.Motion.BaseAxis>> axes,
            out string reason)
        {
            reason = string.Empty;
            if (axes == null)
                return true;

            foreach (var pair in axes)
            {
                QMC.Common.Motion.BaseAxis axis = pair.Value;
                if (axis == null)
                    continue;

                if (axis.IsAlarm)
                {
                    reason = pair.Key + " 축 알람이 ON 상태입니다.";
                    return false;
                }

                if (!axis.IsServoOn)
                {
                    reason = pair.Key + " 축 서보가 OFF 상태입니다.";
                    return false;
                }

                if (!axis.IsHomeDone)
                {
                    reason = pair.Key + " 축 원점 복귀(Home)가 완료되지 않았습니다.";
                    return false;
                }

                if (axis.IsMoving)
                {
                    reason = pair.Key + " 축이 아직 이동 중입니다.";
                    return false;
                }
            }

            return true;
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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
            SequenceStartMode startMode = SequenceStartMode.Resume,
            bool keepCassetteAtSlotForNextAccess = false)
        {
            bool loaderActive = true;
            try
            {
                ct.ThrowIfCancellationRequested();

                // 유효하지 않은 slot(-1 등)으로 카세트 접근 모션이 진행되지 않도록 진입 시점에 차단한다.
                if (slotIndex < 0)
                {
                    return Fail("SEQ-IN-WAFER-UNLOAD-SLOT", "InputSequence",
                        "Input 웨이퍼 언로딩 대상 슬롯이 유효하지 않습니다. slot=" + slotIndex +
                        ". InputStage 자재의 원본 슬롯(SourceSlotNumber) 정보를 확인하세요.");
                }

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
                // keepCassetteAtSlotForNextAccess=true이면 Feeder -> Cassette 이송 후 리프터를 Avoid로
                // 되돌리지 않고 곧바로 다음 슬롯 로딩으로 이어간다(같은 Loader lease 안에서만 사용).
                var feederSequence = new InputFeederSequence(Context);
                InputFeederSequenceOptions feederOptions = BuildFeederSequenceOptions(
                    slotIndex, slotIndex, bFine, moveTimeoutMs, startMode, keepCassetteAtSlotForNextAccess);

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
                // 언로드가 끝난 wafer의 완료/ready 잔류 신호가 다음 wafer의 완료로 오인되지 않도록
                // 언로드 완료 시점에 Stage cycle 신호를 함께 초기화한다. (수동 언로드 경로 포함)
                ResetInputStageCycleSignals();
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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

        // Work CYCLE RUN 수동 테스트(INPUT LOAD): Auto 사이클과 동일한 재개 판정과 스텝 상태머신으로
        // Input 로딩을 실행한다. 수동 전용 경로(ExecuteWaferLoadingAsync)와 달리 Auto 운전이 실제로 타는
        // 코드(RestoreInputStepSessionFromRuntimeState -> ExecuteCurrentInputStepAsync)를 그대로 검증한다.
        // ReviewStage(사용자 확인)와 Picker Ready 신호 발행은 Auto 운전에서 수행하므로 그 직전까지 진행한다.
        public Task<int> ExecuteAutoStepLoadingForTestAsync(CancellationToken ct)
        {
            // slot이 음수이면 자동 순번 로딩이므로 role은 사용되지 않는다.
            return ExecuteAutoStepLoadingForTestAsync(ct, CassetteMaterialRole.Input1, -1);
        }

        /// <summary>
        /// Manual Sequence INPUT LOAD. requestedSlotIndex가 0 이상이면 작업자가 지정한 Wafer를 로딩하고,
        /// 음수이면 Auto와 동일한 순번(ResolveSlot)으로 로딩한다.
        /// (UNLOAD는 원본 슬롯으로만 복귀하므로 지정 대상이 없다.)
        /// </summary>
        public async Task<int> ExecuteAutoStepLoadingForTestAsync(
            CancellationToken ct,
            CassetteMaterialRole requestedRole,
            int requestedSlotIndex)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                LogPublic("[UNIT-INPUT] CYCLE RUN INPUT LOAD (auto-step test) start");
                WriteLog("ExecuteAutoStepLoadingForTestAsync",
                    "CYCLE RUN INPUT LOAD: Auto 스텝 상태머신 로딩 테스트를 시작합니다. requestedSlot=" +
                    (requestedSlotIndex >= 0 ? (requestedRole + "/" + (requestedSlotIndex + 1).ToString("00")) : "auto") +
                    " - Start");

                // Auto 사이클과 동일하게 런타임 자재 상태로 재개 위치를 복원한다.
                RestoreInputStepSessionFromRuntimeState();

                // 언로드(Stage->Feeder->Cassette) 중단 잔류 wafer가 있으면 Auto와 동일하게
                // 카세트 복귀를 먼저 마친 뒤 재개 위치를 다시 판정한다.
                if (_resumeFeederUnloadToCassette)
                {
                    _resumeFeederUnloadToCassette = false;
                    await ExecuteResumeFeederUnloadToCassetteAsync(ct).ConfigureAwait(false);
                    RestoreInputStepSessionFromRuntimeState();
                }

                if (_autoStep == InputSequenceAutoStep.Complete)
                {
                    WriteLog("ExecuteAutoStepLoadingForTestAsync",
                        "InputStage가 이미 로딩 완료 상태여서 추가 동작 없이 종료합니다. step=" + _autoStep + " - Ok");
                    LogPublic("[UNIT-INPUT] CYCLE RUN INPUT LOAD already complete");
                    return 0;
                }

                // 작업자 지정 Wafer 로딩: 진행 중인 자재가 없을 때만 허용하고, 지정 슬롯을 검증한 뒤
                // ResolveSlot(자동 순번)을 건너뛰고 Stage 로드 준비부터 시작한다.
                if (requestedSlotIndex >= 0)
                {
                    int requestResult = PrepareRequestedInputSlotLoading(requestedRole, requestedSlotIndex);
                    if (requestResult != 0)
                        return requestResult;
                }

                // ReviewStage(사용자 확인) 직전(DieMapping 완료)까지 Auto와 동일한 스텝을 실행한다.
                while (_autoStep != InputSequenceAutoStep.Complete &&
                       _autoStep != InputSequenceAutoStep.ReviewStage)
                {
                    int result = await ExecuteCurrentInputStepAsync(ct, false).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                LogPublic("[UNIT-INPUT] CYCLE RUN INPUT LOAD (auto-step test) complete step=" + _autoStep);
                WriteLog("ExecuteAutoStepLoadingForTestAsync",
                    "CYCLE RUN INPUT LOAD: Auto 스텝 로딩 테스트 완료. step=" + _autoStep +
                    ", slot=" + _autoSlotIndex + ", wafer=" + _autoWaferId + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoStepLoadingForTestAsync", "CYCLE RUN INPUT LOAD 테스트가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException ex)
            {
                // 수동 테스트의 정지(예: Ready wafer 없음, 카세트 완료)는 이미 원인 알람/로그가 남아 있으므로
                // 상위에서 일반 고장으로 재분류하지 않도록 실패 코드로만 보고한다.
                WriteLog("ExecuteAutoStepLoadingForTestAsync",
                    "CYCLE RUN INPUT LOAD 테스트 정지: " + ex.Message + " - Stopped");
                return -1;
            }
            catch (StepAlreadyAlarmedException ex)
            {
                // 하위에서 이미 Alarm이 발생한 실패이므로 중복 Alarm 없이 실패 코드로만 보고한다.
                WriteLog("ExecuteAutoStepLoadingForTestAsync",
                    "CYCLE RUN INPUT LOAD 테스트 실패(하위 Alarm 처리됨): " + ex.Message + " - Failed");
                return -1;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-TEST-LOAD-EX", "InputSequence",
                    "CYCLE RUN INPUT LOAD 테스트 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        // 작업자가 지정한 Input Cassette 슬롯을 로딩 대상으로 확정한다.
        // 진행 중인 자재(Stage/Feeder 점유)가 있으면 지정 로딩을 허용하지 않는다(자재 혼선 방지).
        private int PrepareRequestedInputSlotLoading(CassetteMaterialRole requestedRole, int requestedSlotIndex)
        {
            if (requestedRole != CassetteMaterialRole.Input1 && requestedRole != CassetteMaterialRole.Input2)
                return Fail("SEQ-IN-TEST-LOAD-ROLE", "InputSequence",
                    "지정 로딩 대상 카세트가 Input 카세트가 아닙니다. role=" + requestedRole);

            WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
            if (stageWafer != null)
                return Fail("SEQ-IN-TEST-LOAD-STAGE-OCCUPIED", "InputSequence",
                    "InputStage에 이미 wafer가 있어 지정 로딩을 시작할 수 없습니다. wafer=" +
                    (stageWafer.WaferId ?? "") + ". INPUT UNLOAD로 먼저 배출하세요.");

            WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
            if (feederWafer != null)
                return Fail("SEQ-IN-TEST-LOAD-FEEDER-OCCUPIED", "InputSequence",
                    "InputFeeder에 이미 wafer가 있어 지정 로딩을 시작할 수 없습니다. wafer=" +
                    (feederWafer.WaferId ?? "") + ". 진행 중인 이송을 먼저 마치세요.");

            WaferMaterial requestedWafer = MaterialStateService.GetWaferInCassette(requestedRole, requestedSlotIndex);
            if (requestedWafer == null)
                return Fail("SEQ-IN-TEST-LOAD-SLOT-EMPTY", "InputSequence",
                    "지정한 슬롯에 로딩할 wafer Material이 없습니다. role=" + requestedRole +
                    ", slot=" + (requestedSlotIndex + 1).ToString("00"));

            WaferMaterialState state = WaferMaterialStateText.Normalize(requestedWafer.State);
            if (state != WaferMaterialState.Ready && state != WaferMaterialState.WorkReady)
                return Fail("SEQ-IN-TEST-LOAD-SLOT-STATE", "InputSequence",
                    "지정한 wafer가 로딩 가능한 상태가 아닙니다. wafer=" + (requestedWafer.WaferId ?? "") +
                    ", role=" + requestedRole + ", slot=" + (requestedSlotIndex + 1).ToString("00") +
                    ", state=" + state);

            _autoCassetteRole = requestedRole;
            _autoSlotIndex = requestedSlotIndex;
            _autoWaferId = requestedWafer.WaferId ?? "";
            _autoStep = InputSequenceAutoStep.PrepareStageLoad;

            LogPublic("[UNIT-INPUT] 지정 Wafer 로딩을 실행합니다. role=" + requestedRole +
                ", slot=" + (requestedSlotIndex + 1).ToString("00") + ", wafer=" + _autoWaferId);
            WriteLog("PrepareRequestedInputSlotLoading",
                "작업자 지정 로딩 대상을 확정했습니다. role=" + requestedRole +
                ", slot=" + (requestedSlotIndex + 1).ToString("00") +
                ", wafer=" + _autoWaferId + ", step=" + _autoStep + " - Ok");
            return 0;
        }

        // Work CYCLE RUN 수동 테스트(INPUT UNLOAD): Auto 사이클과 동일한 판정으로 Input 언로딩을 실행한다.
        // Feeder에 언로드 중단 잔류 wafer(Align 결과 보유)가 있으면 카세트 복귀를 이어서 수행하고,
        // 그렇지 않으면 Stage wafer를 Stage -> Feeder -> Cassette 순서로 언로드한다.
        public async Task<int> ExecuteAutoStepUnloadingForTestAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                LogPublic("[UNIT-INPUT] CYCLE RUN INPUT UNLOAD (auto-step test) start");
                WriteLog("ExecuteAutoStepUnloadingForTestAsync",
                    "CYCLE RUN INPUT UNLOAD: Auto 판정 언로딩 테스트를 시작합니다. - Start");

                RestoreInputStepSessionFromRuntimeState();

                // 언로드 중단 잔류 wafer의 카세트 복귀 재개(Auto 사이클 초입과 동일 경로).
                if (_resumeFeederUnloadToCassette)
                {
                    _resumeFeederUnloadToCassette = false;
                    await ExecuteResumeFeederUnloadToCassetteAsync(ct).ConfigureAwait(false);
                    LogPublic("[UNIT-INPUT] CYCLE RUN INPUT UNLOAD resume-to-cassette complete");
                    WriteLog("ExecuteAutoStepUnloadingForTestAsync",
                        "CYCLE RUN INPUT UNLOAD: Feeder 잔류 wafer 카세트 복귀 재개 완료. - Ok");
                    return 0;
                }

                // Stage wafer는 남아 있지만 빈 Feeder가 안전 후퇴 위치가 아니면 Restore가
                // RecoverFeeder를 선택한다. 기존 수동 Unload 경로는 이 판정을 무시하고 Stage
                // 준비부터 시작해 ExpanderZ 인터락 또는 저장된 중간 Step 재개로 이어졌다.
                if (_autoStep == InputSequenceAutoStep.RecoverFeeder)
                {
                    WriteLog(
                        "ExecuteAutoStepUnloadingForTestAsync",
                        "CYCLE RUN INPUT UNLOAD: Stage 준비 전에 빈 InputFeeder 안전 복구를 실행합니다. - Start");
                    int recoverResult = await ExecuteStepRecoverFeederAsync(ct).ConfigureAwait(false);
                    if (recoverResult != 0)
                        return recoverResult;

                    // 안전 복구로 실제 Feeder 위치/Lift 상태가 바뀌었으므로 이전 UnloadFromStage
                    // 실패 Step은 더 이상 유효하지 않다. Material 상태는 유지하고 해당 하위
                    // 시퀀스 재개 정보만 초기화한다.
                    SequenceResumeStore.Clear(InputFeederUnloadFromStageSequence.ResumeStateName);
                    WriteLog(
                        "ExecuteAutoStepUnloadingForTestAsync",
                        "CYCLE RUN INPUT UNLOAD: InputFeeder 안전 복구 완료 및 UnloadFromStage 재개 상태 초기화. - Ok");
                }

                WaferMaterial stageWafer = ResolveStageWaferFromRuntimeState();
                if (stageWafer == null)
                {
                    WaferMaterial feederWafer = ResolveFeederWaferFromRuntimeState();
                    if (feederWafer != null)
                    {
                        // 재개 판정상 로딩 진행 중(Align 결과 없음) wafer는 역방향 반납 경로가 없으므로 전진을 안내한다.
                        return Fail("SEQ-IN-TEST-UNLOAD-FEEDER-LOAD", "InputSequence",
                            "CYCLE RUN INPUT UNLOAD 불가: InputFeeder에 로딩 진행 중 wafer가 있습니다. " +
                            "INPUT LOAD로 Stage 로딩을 완료한 뒤 언로드하세요. wafer=" + (feederWafer.WaferId ?? ""));
                    }

                    return Fail("SEQ-IN-TEST-UNLOAD-EMPTY", "InputSequence",
                        "CYCLE RUN INPUT UNLOAD 불가: InputStage/InputFeeder에 언로드할 wafer가 없습니다.");
                }

                // Stage를 움직이기 전에 Picker 접근을 차단하고(Auto 언로드와 동일) source slot을 확정한다.
                ResetInputStageReadySignals();
                int slotIndex = ResolveSlotIndexFromWafer(stageWafer);
                if (slotIndex < 0)
                {
                    return Fail("SEQ-IN-TEST-UNLOAD-SLOT", "InputSequence",
                        "CYCLE RUN INPUT UNLOAD 불가: 언로드 대상 슬롯을 확인할 수 없습니다. wafer=" +
                        (stageWafer.WaferId ?? ""));
                }

                int result = await ExecuteWaferUnloadingAsync(
                    ct,
                    slotIndex,
                    false,
                    0,
                    SequenceStartMode.Resume).ConfigureAwait(false);
                if (result != 0)
                    return result;

                LogPublic("[UNIT-INPUT] CYCLE RUN INPUT UNLOAD (auto-step test) complete slot=" + slotIndex);
                WriteLog("ExecuteAutoStepUnloadingForTestAsync",
                    "CYCLE RUN INPUT UNLOAD: Auto 판정 언로딩 테스트 완료. slot=" + slotIndex + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteLog("ExecuteAutoStepUnloadingForTestAsync", "CYCLE RUN INPUT UNLOAD 테스트가 취소되었습니다. - Failed");
                throw;
            }
            catch (SequenceStopException ex)
            {
                WriteLog("ExecuteAutoStepUnloadingForTestAsync",
                    "CYCLE RUN INPUT UNLOAD 테스트 정지: " + ex.Message + " - Stopped");
                return -1;
            }
            catch (StepAlreadyAlarmedException ex)
            {
                // 하위에서 이미 Alarm이 발생한 실패이므로 중복 Alarm 없이 실패 코드로만 보고한다.
                WriteLog("ExecuteAutoStepUnloadingForTestAsync",
                    "CYCLE RUN INPUT UNLOAD 테스트 실패(하위 Alarm 처리됨): " + ex.Message + " - Failed");
                return -1;
            }
            catch (Exception ex)
            {
                return Fail("SEQ-IN-TEST-UNLOAD-EX", "InputSequence",
                    "CYCLE RUN INPUT UNLOAD 테스트 실패: " + ex.Message);
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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
            catch (SequenceStopException)
            {
                // 정상 정지(Cycle Stop 등)는 고장(Alarm)으로 재분류하지 않고 상위 제어기로 전파한다.
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

                // DieMapping의 Anchor/Source Map/mark point 결과는 시퀀스 메모리에만 있으므로,
                // 새 시퀀스 객체에서 중간 단계부터 재개하지 않고 Align부터 다시 수행한다.
                if (string.Equals(resumeStep, "MoveNeedleZSafeBeforeMapping", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveVisionProcessBeforeMapping", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveCenterPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "FindCenterPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveCenterDiePoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "MoveTopPoint", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(resumeStep, "FindTopPoint", StringComparison.OrdinalIgnoreCase) ||
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
                _restartAlignFromReview = true;
                _restartDieMappingFromReview = true;
                SequenceResumeStore.Clear(InputStageDieMappingSequenceStateName);
                SequenceResumeStore.Clear(InputStageAlignSequenceStateName);
                _autoStep = InputSequenceAutoStep.AlignStage;

                WriteLog("ExecuteCurrentInputStepAsync",
                    "DieMapping resume step=" + dieMappingResumeStep +
                    " 은/는 재시작 후 Anchor/맵포인트 런타임 정보가 복원되지 않는 단계입니다. 웨이퍼 얼라인부터 다시 시작합니다. wafer=" +
                    _autoWaferId + ", slot=" + _autoSlotIndex + " - Restart");
            }
            catch (Exception ex)
            {
                _restartAlignFromReview = true;
                _restartDieMappingFromReview = true;
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
                int waitStatusTick = Environment.TickCount;

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
                        waitStatusTick = Environment.TickCount;
                    }
                    else if (unchecked(Environment.TickCount - waitStatusTick) >= AutoWaitStatusLogIntervalMs)
                    {
                        // 무언정지 진단: 대기가 길어지면 현재 차단 사유를 주기적으로 남긴다.
                        waitStatusTick = Environment.TickCount;
                        WriteLog("InputPickerAvoidGate",
                            safeHolder + " 전 Picker Avoid 대기가 계속되고 있습니다. reason=" + reason + " - Wait");
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
                int waitStatusTick = Environment.TickCount;

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
                        waitStatusTick = Environment.TickCount;
                    }
                    else if (unchecked(Environment.TickCount - waitStatusTick) >= AutoWaitStatusLogIntervalMs)
                    {
                        // 무언정지 진단: 대기가 길어지면 현재 차단 사유를 주기적으로 남긴다.
                        waitStatusTick = Environment.TickCount;
                        WriteLog("InputPickerDrainGate",
                            safeHolder + " 전 Picker 제품 배출/Avoid 대기가 계속되고 있습니다. reason=" + reason + " - Wait");
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
            SequenceStartMode startMode,
            bool keepCassetteAtSlotForNextAccess = false)
        {
            var options = InputFeederSequenceOptions.Default();
            options.KeepCassetteAtSlotForNextAccess = keepCassetteAtSlotForNextAccess;
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

        // To do: C4 - 현재 처리 중인 카세트 레벨(_autoCassetteRole)을 우선 조회한다.
        // Input1 소진 후 Input2 처리 중에, 카세트로 복귀한 Input1의 완료(Done) wafer가
        // 같은 slot 번호로 잘못 조회되어 ExpectedWaferId가 어긋나던 문제를 막는다.
        private string ResolveInputWaferId(int slotIndex)
        {
            WaferMaterial wafer = ResolveFeederWaferFromRuntimeState();
            if (wafer == null)
                wafer = ResolveStageWaferFromRuntimeState();
            if (wafer != null && wafer.SourceSlotNumber == slotIndex)
                return wafer.WaferId ?? "";

            CassetteMaterialRole primaryRole = ResolvePrimaryInputCassetteRole();
            CassetteMaterialRole secondaryRole = primaryRole == CassetteMaterialRole.Input1
                ? CassetteMaterialRole.Input2
                : CassetteMaterialRole.Input1;

            wafer = MaterialStateService.GetWaferInCassette(primaryRole, slotIndex);
            if (wafer == null)
                wafer = MaterialStateService.GetWaferInCassette(secondaryRole, slotIndex);
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

            // To do: C4 - 현재 처리 레벨을 우선 확인해 완료 wafer가 남은 반대 레벨로 오판하지 않게 한다.
            CassetteMaterialRole primaryRole = ResolvePrimaryInputCassetteRole();
            CassetteMaterialRole secondaryRole = primaryRole == CassetteMaterialRole.Input1
                ? CassetteMaterialRole.Input2
                : CassetteMaterialRole.Input1;

            if (MaterialStateService.GetWaferInCassette(primaryRole, slotIndex) != null)
                return primaryRole;
            if (MaterialStateService.GetWaferInCassette(secondaryRole, slotIndex) != null)
                return secondaryRole;
            return primaryRole;
        }

        // 현재 처리 중인 Input 카세트 레벨을 조회 우선순위 기준으로 반환한다.
        private CassetteMaterialRole ResolvePrimaryInputCassetteRole()
        {
            return _autoCassetteRole == CassetteMaterialRole.Input2
                ? CassetteMaterialRole.Input2
                : CassetteMaterialRole.Input1;
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

