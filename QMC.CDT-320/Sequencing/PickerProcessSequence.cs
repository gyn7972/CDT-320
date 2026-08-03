using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerProcessSequence : PickerSequenceBase<PickerProcessStep>
    {
        #region 실행 상태·Abort·Process 진입

        private const int OutputStageExchangeStallTimeoutMs = 120000;
        private PickerPickUpSequence _pickUpSequence;
        private PickerBottomInspectionSequence _bottomInspectionSequence;
        private PickerSideInspectionSequence _sideInspectionSequence;
        private PickerBottomAndSideInspectionSequence _bottomAndSideInspectionSequence;
        private PickerPlaceSequence _placeSequence;
        private SequenceResourceLease _pickerProcessLease;
        private PickerPhaseLease _phaseLease;
        private AutoSequencePickerWorkZoneLease _workZoneLease;
        private bool _bottomInspectionCompletedInCurrentRun;
        private bool _forceBottomInspectionBeforeSideResume;
        private bool _forceSafeYBeforePlaceResume;
        private bool _keepPickerYForwardForContinuousPlace;
        private bool _resumePartialPickUpWithoutMarkPermission;
        private bool _firstForwardTurnHandled;
        private bool _resumeDrainWaitHandled;

        // [동적 선행 대기점, 지시서 2026-07-27] 촬영(선행검사) 진행 중 대기 픽커 X를
        // "배치 maxVisionX + 팔로잉 클리어런스(+여유)"까지 선행 접근시키는 기능의 이동 Task/목표.
        // 허가 대기 세션당 1회만 발동(재명령 금지 — AXM 0x1038 선례), 세션 시작 시 초기화.
        private Task<int> _dynamicWaitAdvanceMoveTask;
        private double _dynamicWaitAdvanceTarget;
        private bool _resumeDrainTurnHeld;
        private bool _pickerZStageSafeConfirmedByPickUp;

        public PickerProcessSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Process, side == PickerSequenceSide.Front ? "FrontPickerSequence" : "RearPickerSequence")
        {
            CurrentStep = PickerProcessStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerProcessStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ResetPickerPhaseSignals();
                ReleasePickerProcessPhase("Abort");
                ReleasePickerWorkZone("Abort");
                InputCameraPreInspectionCoordinator.Clear(Side);
                InputVisionXPrePositionCoordinator.Cancel(Side);
                // C3(2026-07-26): 선행검사 EPD 직후 시작된 독립 회피 세션 잔여분 정리 —
                // 미인수 Task를 관찰(observe)해 다음 배치와의 겹침/unobserved 예외를 막는다.
                VisionIndependentRetreatCoordinator.CancelInput(Side, "PickerProcessSequence.Abort");

                if (_pickUpSequence != null)
                    _pickUpSequence.Abort();
                if (_bottomInspectionSequence != null)
                    _bottomInspectionSequence.Abort();
                if (_sideInspectionSequence != null)
                    _sideInspectionSequence.Abort();
                if (_bottomAndSideInspectionSequence != null)
                    _bottomAndSideInspectionSequence.Abort();
                if (_placeSequence != null)
                    _placeSequence.Abort();

                _pickUpSequence = null;
                _bottomInspectionSequence = null;
                _sideInspectionSequence = null;
                _bottomAndSideInspectionSequence = null;
                _placeSequence = null;
                _bottomInspectionCompletedInCurrentRun = false;
                _forceBottomInspectionBeforeSideResume = false;
                _forceSafeYBeforePlaceResume = false;
                _keepPickerYForwardForContinuousPlace = false;
                _resumePartialPickUpWithoutMarkPermission = false;
                _firstForwardTurnHandled = false;
                _resumeDrainWaitHandled = false;
                _resumeDrainTurnHeld = false;
                _pickerZStageSafeConfirmedByPickUp = false;
                CurrentStep = PickerProcessStep.Complete;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " picker process abort failed. step=" + CurrentStep +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                int acquireResult = await AcquireActivePickerProcessResourceAsync(
                    "ProcessStart",
                    ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                if (IsStepRunMode())
                    return await ExecuteSingleProcessStepAsync(ct).ConfigureAwait(false);

                return await ExecuteProcessUntilCompleteAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                string pendingPermissionDetail;
                bool hasAnyPermission = InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail);
                WriteLog("PickerProcessSequence",
                    Name + " Picker 공정 취소. side=" + Side +
                    ", step=" + CurrentStep +
                    ", tokenCanceled=" + ct.IsCancellationRequested +
                    ", cycleStopRequested=" + (Context != null && Context.IsCycleStopRequested) +
                    ", hasOwnPermission=" + InputCameraPickUpPermissionStore.HasPermission(Side) +
                    ", hasAnyPermission=" + hasAnyPermission +
                    ", permissionDetail=" + (string.IsNullOrWhiteSpace(pendingPermissionDetail) ? "-" : pendingPermissionDetail) +
                    " - Canceled");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-EX", Name, "Picker 공정 시퀀스 실패. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                ReleaseActivePickerProcessResource("ProcessFinally");
                // [사용자 지시 2026-07-27] Cycle Stop/종료 정리 전, place의 백그라운드 Z Avoid
                // 상승이 진행 중이면 완주를 기다린다 — 상승이 잘리며 -5 알람으로 승격 방지.
                if (_placeSequence != null)
                {
                    try
                    {
                        await _placeSequence.WaitPendingPickerZAvoidRiseBeforeStopAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
                await EnsureCycleStopSafePoseAsync(ct).ConfigureAwait(false);
                ReleasePickerProcessPhase("ProcessFinally");
                ResetPickerPhaseSignals();
            }
        }

        private async Task EnsureCycleStopSafePoseAsync(CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return;

                if (Context == null || !Context.IsCycleStopRequested)
                    return;

                if (ct.IsCancellationRequested || IsAlarmStopActive())
                {
                    WriteLog("PickerProcessSequence",
                        Name + " Cycle Stop 안전 정리를 건너뜁니다. " +
                        "tokenCanceled=" + ct.IsCancellationRequested +
                        ", alarmActive=" + IsAlarmStopActive() +
                        ", side=" + Side + " - Check");
                    return;
                }

                // 현재 기준: 정상 Cycle Stop 최종 자세는 X 이동 없이 Picker Z 상승 후 Picker Y Avoid로 정리한다.
                int result = await EnsureSelfSafeAsync(
                    "Cycle Stop 최종 안전 자세",
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " Cycle Stop 최종 안전 자세 정리 실패. result=" + result +
                        ", side=" + Side + " - Failed");
                    return;
                }

                WriteLog("PickerProcessSequence",
                    Name + " Cycle Stop 최종 안전 자세 정리 완료. PickerZ=Avoid, PickerY=Avoid, X 이동 없음. side=" +
                    Side + " - Ok");
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
                WriteLog("PickerProcessSequence",
                    Name + " Cycle Stop 최종 안전 자세 정리 중 예외 발생. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        #endregion

        #region 리소스 점유·Step 실행·Resume Drain

        private Task<SequenceResourceLease> AcquirePickerProcessResourceAsync(CancellationToken ct)
        {
            if (Options != null &&
                Options.RunMode == SequenceRunMode.Auto &&
                Context != null &&
                Context.AutoSequenceGate != null)
            {
                return Context.AutoSequenceGate.BeginPickerProcessAsync(Side, Name + ":Process", ct);
            }

            return AcquireResourceAsync(PickerResourceKind, Name + ":Process", ct);
        }

        private async Task<int> AcquireActivePickerProcessResourceAsync(
            string description,
            CancellationToken ct)
        {
            if (_pickerProcessLease != null)
                return 0;

            _pickerProcessLease = await AcquirePickerProcessResourceAsync(ct).ConfigureAwait(false);
            if (_pickerProcessLease == null)
            {
                return Fail("PICKER-RESOURCE", Name,
                    "Picker 리소스 점유 실패. resource=" + PickerResourceKind +
                    ", description=" + (description ?? "-"));
            }

            WriteLog("PickerProcessResource",
                Name + " Picker Process 리소스 점유 완료. side=" + Side +
                ", resource=" + PickerResourceKind +
                ", description=" + (description ?? "-") + " - Ok");
            return 0;
        }

        private void ReleaseActivePickerProcessResource(string description)
        {
            SequenceResourceLease lease = _pickerProcessLease;
            _pickerProcessLease = null;
            if (lease == null)
                return;

            lease.Dispose();
            WriteLog("PickerProcessResource",
                Name + " Picker Process 리소스를 안전 대기/종료 경계에서 반환했습니다. side=" + Side +
                ", resource=" + PickerResourceKind +
                ", description=" + (description ?? "-") + " - Ok");
        }

        private int VerifyPickerSafeAfterProcessResourceAcquire(string description)
        {
            string safeDetail;
            if (VerifySafeStartConfig(out safeDetail))
                return 0;

            return Fail("PICKER-RESOURCE-REACQUIRE-UNSAFE", Name,
                "Picker Process 리소스 재점유 후 실제 Avoid 안전상태 확인에 실패했습니다. " +
                "side=" + Side +
                ", description=" + (description ?? "-") +
                ", detail=" + safeDetail);
        }

        private bool IsStepRunMode()
        {
            return Options != null && Options.RunMode != SequenceRunMode.Auto;
        }

        private async Task<int> ExecuteSingleProcessStepAsync(CancellationToken ct)
        {
            if (CurrentStep == PickerProcessStep.Complete)
                return 0;

            ct.ThrowIfCancellationRequested();
            return await ExecuteStepAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> ExecuteProcessUntilCompleteAsync(CancellationToken ct)
        {
            while (CurrentStep != PickerProcessStep.Complete)
            {
                ct.ThrowIfCancellationRequested();

                // §4: run의 첫 전진 스텝만 크로스-픽커 우선순위 게이트를 통과한다(완료 가까운 쪽 먼저, 한 번에 한 픽커).
                // 이후 스텝/다음 die는 게이트를 통과하지 않으며 기존 상대 PickerY Avoid 대기 + supervisor가 담당한다.
                int resumeDrainWaitResult = await EnsureResumeDrainTurnBeforeProcessStepAsync(ct).ConfigureAwait(false);
                if (resumeDrainWaitResult != 0)
                    return resumeDrainWaitResult;

                bool gateThisStep = !_firstForwardTurnHandled &&
                                    (Options == null || Options.RunMode == SequenceRunMode.Auto) &&
                                    IsFirstForwardGatedStep(CurrentStep);
                if (gateThisStep)
                {
                    await PickerFirstForwardSequencer.AcquireAsync(
                        Side,
                        MapStepToFirstForwardRank(CurrentStep),
                        Context,
                        msg => WriteLog("PickerFirstForwardSequencer", Name + " " + msg + " - Wait"),
                        ct).ConfigureAwait(false);
                }

                int result;
                try
                {
                    result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    if (gateThisStep)
                    {
                        _firstForwardTurnHandled = true;
                        PickerFirstForwardSequencer.Complete(Side);
                    }
                }

                if (result != 0)
                    return result;
            }

            if (!_firstForwardTurnHandled &&
                (Options == null || Options.RunMode == SequenceRunMode.Auto))
            {
                PickerFirstForwardSequencer.Complete(Side);
                _firstForwardTurnHandled = true;
                WriteLog("PickerFirstForwardSequencer",
                    Name + " 이번 run에서 첫 전진 대상 작업이 없어 no-work/done 상태를 보고합니다. side=" +
                    Side + " - Check");
            }

            return await CompleteResumeDrainAfterProcessAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> EnsureResumeDrainTurnBeforeProcessStepAsync(CancellationToken ct)
        {
            if (_resumeDrainWaitHandled)
                return 0;

            if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                return 0;

            if (CurrentStep == PickerProcessStep.CheckUnit ||
                CurrentStep == PickerProcessStep.Complete ||
                CurrentStep == PickerProcessStep.Error ||
                CurrentStep == PickerProcessStep.Idle)
            {
                return 0;
            }

            try
            {
                // CheckUnit에서 실제 Picker 전체 Avoid를 확인한 뒤의 비모션 대기다.
                // 재시작 우선순위를 기다리는 동안 Process 리소스를 보유하면 Output Loader가
                // 양쪽 Picker gate를 확보할 수 없으므로, 안전 위치에서만 일시 반환한다.
                if (_pickerProcessLease == null)
                {
                    return Fail("PICKER-RESUME-DRAIN-RESOURCE", Name,
                        "재시작 드레인 순번 대기 전 Picker Process 리소스가 없습니다. " +
                        "side=" + Side + ", step=" + CurrentStep);
                }

                ReleaseActivePickerProcessResource("ResumeDrainTurnWait");

                bool held = await PickerFirstForwardSequencer.WaitResumeDrainTurnAsync(
                    Side,
                    Context,
                    msg => WriteLog("PickerResumeDrainSequencer", Name + " " + msg + " - Wait"),
                    ct).ConfigureAwait(false);

                int acquireResult = await AcquireActivePickerProcessResourceAsync(
                    "ResumeDrainTurnAcquired",
                    ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                int safeResult = VerifyPickerSafeAfterProcessResourceAcquire("ResumeDrainTurnAcquired");
                if (safeResult != 0)
                    return safeResult;

                _resumeDrainWaitHandled = true;
                _resumeDrainTurnHeld = held;

                if (held)
                {
                    WriteLog("PickerResumeDrainSequencer",
                        Name + " 재시작 드레인 턴 확보. 이 Picker가 남은 공정을 Place/Avoid/Output검사 완료까지 진행하는 동안 상대 Picker 공정 진입을 보류합니다. " +
                        "side=" + Side +
                        ", step=" + CurrentStep + " - Ok");
                }
                else
                {
                    WriteLog("PickerResumeDrainSequencer",
                        Name + " 재시작 드레인 대기 완료. 선행 Picker의 Place/Avoid/Output검사 완료 후 공정 진입을 허용합니다. " +
                        "side=" + Side +
                        ", step=" + CurrentStep + " - Ok");
                }

                return 0;
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
                return Fail("PICKER-RESUME-DRAIN-WAIT-EX", Name,
                    "재시작 Picker 드레인 턴 대기 중 예외가 발생했습니다. side=" + Side +
                    ", step=" + CurrentStep +
                    ", error=" + ex.Message);
            }
        }

        private async Task<int> CompleteResumeDrainAfterProcessAsync(CancellationToken ct)
        {
            if (!_resumeDrainTurnHeld)
                return 0;

            try
            {
                if (Context != null && Context.OutputPostPlaceInspections != null)
                {
                    int idleResult = await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                        Name + ":ResumeDrainComplete",
                        0,
                        ct).ConfigureAwait(false);
                    if (idleResult != 0)
                        return idleResult;
                }

                PickerFirstForwardSequencer.CompleteResumeDrain(Side);
                _resumeDrainTurnHeld = false;

                WriteLog("PickerResumeDrainSequencer",
                    Name + " 재시작 드레인 완료. Place 완료, Picker Avoid 복귀, Output camera 후검사 idle 확인 후 상대 Picker 공정 진입을 허용합니다. " +
                    "side=" + Side + " - Ok");
                return 0;
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
                return Fail("PICKER-RESUME-DRAIN-COMPLETE-EX", Name,
                    "재시작 Picker 드레인 완료 확인 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
        }

        private static bool IsFirstForwardGatedStep(PickerProcessStep step)
        {
            return step == PickerProcessStep.RunPickUp ||
                   step == PickerProcessStep.RunBottomInspection ||
                   step == PickerProcessStep.RunSideInspection ||
                   step == PickerProcessStep.RunPlace;
        }

        // 완료 가까운 쪽 우선. Bottom/Side는 하나로 취급한다(사용자 정책).
        private static int MapStepToFirstForwardRank(PickerProcessStep step)
        {
            switch (step)
            {
                case PickerProcessStep.RunPlace:
                    return PickerFirstForwardSequencer.RankPlace;
                case PickerProcessStep.RunBottomInspection:
                case PickerProcessStep.RunSideInspection:
                    return PickerFirstForwardSequencer.RankBottomSide;
                default:
                    return PickerFirstForwardSequencer.RankPickUp;
            }
        }

        // SAFETY CONTRACT:
        // - Auto CycleStop은 활성 PickUp/검사/Place child 또는 Picker가 보유한 InputTarget Die가 있으면
        //   즉시 공정 중단이 아니라 안전 배출 경계까지 드레인할 수 있다.
        // - Alarm 활성 시에는 드레인을 허용하지 않으며 InputCamera/Check/Idle/Complete 경계는 즉시 Stop 판단을 따른다.
        // - 이 메서드의 true를 "즉시 축 정지"로 해석하지 않는다.
        private bool ShouldDeferCycleStopForActivePickerDrain()
        {
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return false;
                if (Context == null || !Context.IsCycleStopRequested)
                    return false;
                if (IsAlarmStopActive())
                    return false;

                if (CurrentStep == PickerProcessStep.RunInputCameraMarkInspection ||
                    CurrentStep == PickerProcessStep.CheckUnit ||
                    CurrentStep == PickerProcessStep.Idle ||
                    CurrentStep == PickerProcessStep.Complete ||
                    CurrentStep == PickerProcessStep.Error)
                {
                    return false;
                }

                if (_pickUpSequence != null ||
                    _bottomInspectionSequence != null ||
                    _sideInspectionSequence != null ||
                    _bottomAndSideInspectionSequence != null ||
                    _placeSequence != null)
                {
                    return true;
                }

                return HasTargetDieOnThisPicker();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool HasTargetDieOnThisPicker()
        {
            try
            {
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die != null && die.IsInputTarget)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        #endregion

        #region Step Dispatch·Material 기반 다음 단계

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                // 유닛 확인
                case PickerProcessStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // InputCamera Mark 검사 실행
                case PickerProcessStep.RunInputCameraMarkInspection:
                    return Context.Tact.ProcessAsync(this, "InputCamera Mark 검사", ct, () => RunInputCameraMarkInspectionAsync(ct));

                // 픽업 실행
                case PickerProcessStep.RunPickUp:
                    return Context.Tact.ProcessAsync(this, "PickUp", ct, () => RunPickUpAsync(ct));

                // 하단 검사 실행
                case PickerProcessStep.RunBottomInspection:
                    return Context.Tact.ProcessAsync(this, "Bottom 검사", ct, () => RunBottomInspectionAsync(ct));

                // 사이드 검사 실행
                case PickerProcessStep.RunSideInspection:
                    return Context.Tact.ProcessAsync(this, "Side 검사", ct, () => RunSideInspectionAsync(ct));

                // 플레이스 실행
                case PickerProcessStep.RunPlace:
                    return Context.Tact.ProcessAsync(this, "Place", ct, () => RunPlaceAsync(ct));

                default:
                    return Task.FromResult(Fail("PICKER-PROCESS-STEP", Name, "지원하지 않는 Picker 공정 스텝입니다. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            ResetPickerPhaseSignals();

            if (Side == PickerSequenceSide.Front && FrontPicker == null)
                return Fail("PICKER-FRONT-NO-UNIT", Name, "FrontPickerUnit을 찾을 수 없습니다.");

            if (Side == PickerSequenceSide.Rear && RearPicker == null)
                return Fail("PICKER-REAR-NO-UNIT", Name, "RearPickerUnit을 찾을 수 없습니다.");

            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerProcessSequence", Name + " Picker 사용 설정이 OFF라 공정을 완료 처리합니다. side=" + Side + " - Check");
                CurrentStep = PickerProcessStep.Complete;
                return 0;
            }

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-AXIS-NOT-READY", Name, "Picker 축 준비 상태가 아닙니다. side=" + Side + ", reason=" + axisReason);

            // INV-7: 시작/재개 첫 이동 전, 자기 픽커가 Avoid 안전 배치인지 확인한다(이동 없음).
            // 정책: 어긋나면 자동 후퇴하지 않고 현재 상태에서 정지(알람)한다. 작업자가 START를 다시 누르면
            //       StartAsync가 Ready 시퀀스로 상부축(픽커/카메라)을 전부 Avoid로 정렬한 뒤 공정을 다시 시작한다.
            //       Start 전 Ready가 항상 선행되므로 정상 시작/재개에서는 이 검사가 통과한다.
            string startSafeDetail;
            if (!VerifySafeStartConfig(out startSafeDetail))
            {
                return Fail("PICKER-START-NOT-SAFE", Name,
                    "시작/재개 전 안전 배치 확인 실패. 상부 Picker가 Avoid 위치가 아닙니다. " +
                    "정지 후 START를 다시 누르면 Ready 시퀀스가 상부축을 Avoid로 정렬한 뒤 진행합니다. " +
                    "side=" + Side + ", detail=" + startSafeDetail);
            }

            WriteLog("PickerProcessSequence",
                Name + " 시작/재개 안전 배치 확인 완료. 자기 Picker가 Avoid 위치입니다. side=" + Side + " - Ok");

            return ResolveNextProcessStepFromMaterial();
        }

        private int ResolveNextProcessStepFromMaterial()
        {
            try
            {
                List<int> enabled = BuildEnabledPickerIndexes();
                if (enabled == null || enabled.Count == 0)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 사용 설정된 Picker가 없어 공정을 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerProcessStep.Complete;
                    return 0;
                }

                int occupiedCount = 0;
                int targetPickerDieCount = 0;
                int nonTargetPickerDieCount = 0;
                int bottomRequiredCount = 0;
                int sideRequiredCount = 0;
                int placeReadyCount = 0;

                for (int i = 0; i < enabled.Count; i++)
                {
                    int pickerIndex = enabled[i];
                    int pickerNo = ToPickerNo(pickerIndex);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                        continue;

                    occupiedCount++;

                    if (!die.IsInputTarget)
                    {
                        nonTargetPickerDieCount++;
                        continue;
                    }

                    targetPickerDieCount++;

                    bool bottomDone = HasInspectionResult(die, "Bottom");
                    bool side0Done = HasInspectionResult(die, "Side0");
                    bool side90Done = HasInspectionResult(die, "Side90");

                    if (!bottomDone)
                    {
                        bottomRequiredCount++;
                        continue;
                    }

                    if (!side0Done || !side90Done)
                    {
                        sideRequiredCount++;
                        continue;
                    }

                    if (!IsPlaceResultReady(die))
                    {
                        sideRequiredCount++;
                        WriteLog("PickerProcessSequence",
                            Name + " Side 검사 record는 있으나 Die 최종 판정이 없어 Side 검사부터 재개합니다. " +
                            "side=" + Side +
                            ", pickerNo=" + pickerNo +
                            ", die=" + die.DieId +
                            ", result=" + die.Result + " - Check");
                        continue;
                    }

                    placeReadyCount++;
                }

                bool hasReadyInputPickTarget = MaterialStateService.HasReadyInputStagePickTarget();
                bool hasInputPickReservationForSide =
                    MaterialStateService.HasInputStagePickReservationForPickerLocation(PickerLocationKind);
                bool hasRemainingInputPickWork = hasReadyInputPickTarget || hasInputPickReservationForSide;
                int emptyEnabledPickerCount = enabled.Count - occupiedCount;
                WriteLog("PickerProcessSequence",
                    Name + " Picker 공정 시작 스텝 판단. side=" + Side +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "null") +
                    ", enabledPickerCount=" + enabled.Count +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", emptyEnabledPickerCount=" + emptyEnabledPickerCount +
                    ", targetPickerDieCount=" + targetPickerDieCount +
                    ", nonTargetPickerDieCount=" + nonTargetPickerDieCount +
                    ", bottomRequiredCount=" + bottomRequiredCount +
                    ", sideRequiredCount=" + sideRequiredCount +
                    ", placeReadyCount=" + placeReadyCount +
                    ", hasReadyInputPickTarget=" + hasReadyInputPickTarget +
                    ", hasInputPickReservationForSide=" + hasInputPickReservationForSide +
                    ", hasRemainingInputPickWork=" + hasRemainingInputPickWork +
                    " - Check");

                if (occupiedCount == 0)
                {
                    _resumePartialPickUpWithoutMarkPermission = false;
                    CurrentStep = PickerProcessStep.RunInputCameraMarkInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker에 Die가 없어 InputCamera Mark 검사부터 시작합니다. side=" + Side +
                        ", enabledPickerCount=" + enabled.Count + " - Check");
                    return 0;
                }

                if (targetPickerDieCount <= 0)
                {
                    return Fail("PICKER-PROCESS-NON-TARGET-DIE-ON-PICKER", Name,
                        "Picker 위에 target Die가 없어 자동 공정을 계속할 수 없습니다. " +
                        "Skip/비대상 Die는 검사/Place 대상으로 사용하지 않습니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", nonTargetPickerDieCount=" + nonTargetPickerDieCount);
                }

                if (placeReadyCount > 0 &&
                    placeReadyCount == targetPickerDieCount &&
                    bottomRequiredCount == 0 &&
                    sideRequiredCount == 0)
                {
                    _forceBottomInspectionBeforeSideResume = false;
                    _forceSafeYBeforePlaceResume = true;
                    _keepPickerYForwardForContinuousPlace = false;
                    _resumePartialPickUpWithoutMarkPermission = false;
                    CurrentStep = PickerProcessStep.RunPlace;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker 위 target Die가 모두 Place 가능 상태라 Place부터 재개합니다. " +
                        "Place 도중 알람/정지 후 재시작 케이스로 판단하여 Bottom/Side 재검사를 생략하고, " +
                        "PickerY Avoid 정리 후 Picker X/T를 Place 위치로 먼저 이동한 다음 PickerY 전진을 허용합니다. " +
                        "side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", targetPickerDieCount=" + targetPickerDieCount +
                        ", nonTargetPickerDieCount=" + nonTargetPickerDieCount +
                        ", bottomRequiredCount=" + bottomRequiredCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount +
                        ", forceSafeYBeforePlaceResume=" + _forceSafeYBeforePlaceResume + " - Check");
                    return 0;
                }

                bool partialPickUpResumeCandidate =
                    occupiedCount > 0 &&
                    occupiedCount < enabled.Count &&
                    targetPickerDieCount == occupiedCount &&
                    nonTargetPickerDieCount == 0 &&
                    bottomRequiredCount == targetPickerDieCount &&
                    sideRequiredCount == 0 &&
                    placeReadyCount == 0 &&
                    hasRemainingInputPickWork;
                if (partialPickUpResumeCandidate)
                {
                    _resumePartialPickUpWithoutMarkPermission = false;
                    WriteLog("PickerProcessSequence",
                        Name + " 부분 PickUp 재개 후보 상태이지만 Picker가 이미 Die를 가지고 있어 빈 Picker PickUp보다 보유 Die 검사를 우선합니다. " +
                        "이미 들고 있는 Die를 Bottom/Side/Place로 먼저 드레인한 뒤 다음 PickUp batch를 시작합니다. side=" + Side +
                        ", enabledPickerCount=" + enabled.Count +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", emptyEnabledPickerCount=" + emptyEnabledPickerCount +
                        ", targetPickerDieCount=" + targetPickerDieCount +
                        ", bottomRequiredCount=" + bottomRequiredCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount +
                        ", hasReadyInputPickTarget=" + hasReadyInputPickTarget +
                        ", hasInputPickReservationForSide=" + hasInputPickReservationForSide +
                        ", hasRemainingInputPickWork=" + hasRemainingInputPickWork +
                        ", resumePartialPickUpWithoutMarkPermission=" + _resumePartialPickUpWithoutMarkPermission +
                        " - Check");
                }

                if (occupiedCount > 0 &&
                    occupiedCount < enabled.Count &&
                    targetPickerDieCount == occupiedCount &&
                    nonTargetPickerDieCount == 0 &&
                    bottomRequiredCount == targetPickerDieCount &&
                    sideRequiredCount == 0 &&
                    placeReadyCount == 0 &&
                    !hasRemainingInputPickWork)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 부분 로드 상태이지만 남은 Input Pick 대상이 없어 PickUp 재개로 판단하지 않습니다. " +
                        "Bottom/Side 진입 후 정지했거나 마지막 부분 배치일 수 있으므로 기존 드레인 정책을 유지합니다. side=" + Side +
                        ", enabledPickerCount=" + enabled.Count +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", emptyEnabledPickerCount=" + emptyEnabledPickerCount +
                        ", targetPickerDieCount=" + targetPickerDieCount +
                        ", hasReadyInputPickTarget=" + hasReadyInputPickTarget +
                        ", hasInputPickReservationForSide=" + hasInputPickReservationForSide +
                        ", hasRemainingInputPickWork=" + hasRemainingInputPickWork +
                        " - Check");
                }

                _forceBottomInspectionBeforeSideResume = false;
                _resumePartialPickUpWithoutMarkPermission = false;
                bool forceBottomAndSideFromFirst =
                    (Options == null || Options.RunMode == SequenceRunMode.Auto) &&
                    IsBottomAndSidePipelineModeEnabled();

                _forceBottomInspectionBeforeSideResume = forceBottomAndSideFromFirst;
                CurrentStep = PickerProcessStep.RunBottomInspection;
                WriteLog("PickerProcessSequence",
                    Name + " Picker가 Die를 가지고 있어 기존 Good/NG/검사 record와 관계없이 Bottom 검사부터 재개합니다. " +
                    "Skip/비대상 Die만 제외하고, Picker 위 target Die는 Bottom/Side를 다시 측정한 뒤 Place합니다. side=" + Side +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", targetPickerDieCount=" + targetPickerDieCount +
                    ", nonTargetPickerDieCount=" + nonTargetPickerDieCount +
                    ", bottomRequiredCount=" + bottomRequiredCount +
                    ", sideRequiredCount=" + sideRequiredCount +
                    ", placeReadyCount=" + placeReadyCount +
                    ", forceBottomAndSideFromFirst=" + forceBottomAndSideFromFirst + " - Check");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-ROUTE", Name,
                    "Picker 공정 시작 스텝 판단 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool HasInspectionResult(DieMaterial die, string inspectionType)
        {
            try
            {
                if (die == null || die.Inspections == null || string.IsNullOrWhiteSpace(inspectionType))
                    return false;

                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record == null)
                        continue;

                    if (!string.Equals(record.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return record.Result != MaterialInspectionResult.Unknown;
                }

                return false;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerProcessSequence",
                    "Picker inspection result check failed. inspectionType=" + inspectionType +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsPlaceResultReady(DieMaterial die)
        {
            return die != null &&
                   (die.Result == DieResult.Good || die.Result == DieResult.NG);
        }

        #endregion

        #region Input Vision·PickUp·동적 선행 이동

        private async Task<int> RunInputCameraMarkInspectionAsync(CancellationToken ct)
        {
            try
            {
                if (ShouldBlockNewPickForWaferCompletion())
                {
                    CompleteProcessWithoutNewPick("BeforeInputCameraMarkInspection");
                    return 0;
                }

                int readyResult = await EnterOrTransitionPickerPhaseAsync(
                    PickerProcessPhase.PickUp,
                    "InputCameraMarkInspection",
                    ct).ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;

                // [동적 선행 대기점 2026-07-27] 허가 대기 동안만 모니터를 돌린다 — 게이트 4종
                // (촬영 중/허가 대기/Auto+Conti/스위치 On) 충족 시 1회 선행 이동 발행.
                // 이동 명령은 시퀀스 ct를 쓰고(허가 도착으로 모니터가 꺼져도 이동은 완주),
                // 대기 종료 시 반드시 join해 픽업 X 진입과의 재명령 충돌(0x1038)을 차단한다.
                _dynamicWaitAdvanceMoveTask = null;
                _dynamicWaitAdvanceTarget = 0.0;
                InputCameraPreInspectionWaitResult waitResult;
                using (CancellationTokenSource dynamicWaitCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    Task dynamicWaitMonitor = RunDynamicPickUpWaitAdvanceMonitorAsync(dynamicWaitCts.Token, ct);
                    try
                    {
                        waitResult =
                            await InputCameraPreInspectionCoordinator.WaitForPermissionOrCompletionAsync(
                                Context,
                                Side,
                                BuildChildSequenceOptions(),
                                ct,
                                Name + ":PickUpReady").ConfigureAwait(false);
                    }
                    finally
                    {
                        dynamicWaitCts.Cancel();
                        try { await dynamicWaitMonitor.ConfigureAwait(false); } catch { }
                        await JoinDynamicPickUpWaitAdvanceAsync("허가 대기 종료").ConfigureAwait(false);
                    }
                }

                if (ShouldBlockNewPickForWaferCompletion())
                {
                    CompleteProcessWithoutNewPick("AfterInputCameraMarkInspection");
                    return 0;
                }

                if (waitResult.Status == InputCameraPreInspectionWaitStatus.Failed)
                {
                    ReleasePickerProcessPhase("InputCameraMarkInspectionFailed");
                    return Fail("PICKER-PROCESS-INPUT-CAMERA-PRE-INSPECTION", Name,
                        "InputCamera 선행검사 실패. side=" + Side +
                        ", result=" + waitResult.ResultCode +
                        ", message=" + waitResult.Message);
                }

                if (waitResult.Status == InputCameraPreInspectionWaitStatus.NoTarget)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " InputCamera 선행검사 대상이 없어 Picker 공정을 완료 처리합니다. side=" +
                        Side + " - Check");
                    ReleasePickerProcessPhase("InputCameraPreInspectionNoTarget");
                    CurrentStep = PickerProcessStep.Complete;
                    return 0;
                }

                CurrentStep = PickerProcessStep.RunPickUp;
                return 0;
            }
            catch (OperationCanceledException)
            {
                string pendingPermissionDetail;
                bool hasAnyPermission = InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail);
                WriteLog("PickerProcessSequence",
                    Name + " InputCamera Mark 검사 취소. side=" + Side +
                    ", tokenCanceled=" + ct.IsCancellationRequested +
                    ", cycleStopRequested=" + (Context != null && Context.IsCycleStopRequested) +
                    ", hasOwnPermission=" + InputCameraPickUpPermissionStore.HasPermission(Side) +
                    ", hasAnyPermission=" + hasAnyPermission +
                    ", permissionDetail=" + (string.IsNullOrWhiteSpace(pendingPermissionDetail) ? "-" : pendingPermissionDetail) +
                    " - Canceled");
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-INPUT-CAMERA-MARK-EX", Name,
                    "InputCamera Mark 검사 공정 실행 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunPickUpAsync(CancellationToken ct)
        {
            try
            {
                if ((_pickUpSequence == null || _pickUpSequence.IsComplete) &&
                    ShouldBlockNewPickForWaferCompletion())
                {
                    CompleteProcessWithoutNewPick("BeforePickUpSequence");
                    return 0;
                }

                if (_pickUpSequence == null || _pickUpSequence.IsComplete)
                {
                    int readyResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.PickUp, "PickUp", ct).ConfigureAwait(false);
                    if (readyResult != 0)
                        return readyResult;

                    _pickUpSequence = new PickerPickUpSequence(Context, Side);
                }

                PickerSequenceOptions pickUpOptions = BuildChildSequenceOptions();
                if (_resumePartialPickUpWithoutMarkPermission)
                {
                    pickUpOptions.RequireInputCameraMarkInspectionPermission = false;
                    pickUpOptions.InputCameraPreInspectionMode = false;
                    WriteLog("PickerProcessSequence",
                        Name + " 부분 PickUp 재개로 PickUp 내부 Input die vision 검사 흐름을 허용합니다. " +
                        "이미 들고 있는 Picker는 준비 배치에서 제외되고, 빈 Picker만 예약/검사/픽업합니다. side=" + Side +
                        ", requireInputCameraMarkInspectionPermission=" +
                        pickUpOptions.RequireInputCameraMarkInspectionPermission +
                        ", inputCameraPreInspectionMode=" + pickUpOptions.InputCameraPreInspectionMode + " - Check");
                }

                int result = await SequenceTrace.ChildAsync("PickerPickUpSequence", "PickUp",
                    () => _pickUpSequence.RunAsync(ct, pickUpOptions),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("PickUpFailed");
                    return result;
                }

                if (_pickUpSequence.IsComplete)
                {
                    // [검증 FAIL S5 수정 2026-07-26] 드레인 경계가 PickUpZHold를 남긴 채 종료했으면
                    // Bottom 진입 Full-Avoid 생략을 하지 않는다 — Bottom 첫 스텝의 전 Z Avoid 강제가
                    // PrePick 잔류 Z를 정상 경로로 회수한다.
                    bool pickUpLeftZHoldUnsafe = _pickUpSequence != null && _pickUpSequence.DrainLeftPickerZHoldUnsafe;
                    _pickUpSequence = null;
                    _resumePartialPickUpWithoutMarkPermission = false;

                    if (ShouldBlockNewPickForWaferCompletion() && !HasTargetDieOnThisPicker())
                    {
                        CompleteProcessWithoutNewPick("PickUpCompletedWithoutDie");
                        return 0;
                    }

                    // PickUp 완료 시 PickerZ가 Stage Safe 높이를 통과했으므로 다음 Bottom 진입에서 1회 사용한다.
                    _pickerZStageSafeConfirmedByPickUp = !pickUpLeftZHoldUnsafe;
                    if (pickUpLeftZHoldUnsafe)
                    {
                        WriteLog("PickerProcessSequence",
                            Name + " 드레인 경계 PickUpZHold 잔류로 Bottom 진입 Full-Avoid 생략을 해제합니다. " +
                            "side=" + Side + " - Check");
                    }
                    StartInputVisionXPrePositionAfterPickUpComplete(ct);
                    WriteLog("PickerProcessSequence",
                        Name + " PickUp 완료 후 PickerProcessSequence가 비침습 InputCamera 선행검사를 예약합니다. " +
                        "선행검사는 Picker X/Y를 직접 이동하지 않고 Input 영역 이탈 확인 후 InputStage/InputVision만 사용합니다. side=" +
                        Side + " - Check");
                    StartSafeInputCameraPreInspectionsAfterPickUpComplete(ct, "PickUpComplete");

                    int oppositeSideWaitResult = await WaitOppositePendingSideInspectionBeforeBottomAsync(
                        "PickUpToBottomInspection",
                        ct).ConfigureAwait(false);
                    if (oppositeSideWaitResult != 0)
                        return oppositeSideWaitResult;

                    int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.BottomInspection, "PickUpToBottomInspection", ct).ConfigureAwait(false);
                    if (phaseResult != 0)
                        return phaseResult;

                    CurrentStep = PickerProcessStep.RunBottomInspection;
                }

                return 0;
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
                return Fail("PICKER-PROCESS-PICKUP-EX", Name,
                    "PickUp 공정 실행 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool ShouldBlockNewPickForWaferCompletion()
        {
            if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                return false;

            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            return completion.IsDrainRequested;
        }

        // ===== [동적 선행 대기점, 지시서 2026-07-27] =====
        // 촬영(선행검사) 진행 중 대기 픽커 X를 "배치 maxVisionX + FollowMove 클리어런스(+여유)"
        // 위치로 미리 접근시켜 허가 후 팔로잉 시작 거리를 줄인다.
        //   게이트 4종: ①자기 측 촬영 진행 중 ②허가 대기 상태(모니터 수명으로 보장)
        //              ③Auto+ContiSegmentedPickUp ④PickUpDynamicWaitMode On(기본 Off)
        //   원칙: 전진만 허용(후퇴 금지), 산식·좌표 변환은 기존 것 재사용(신규 산식 금지),
        //         발행 전 MotionGuard dry-run(차단이면 알람 없이 스킵), 허가/선행검사 흐름 무변경.

        // 허가 대기 세션 동안 200ms 주기로 게이트를 판정하고 충족 시 1회 선행 이동을 발행한다.
        // monitorCt = 대기 종료 시 취소(모니터 전용), moveCt = 시퀀스 토큰(이동은 완주 허용).
        private async Task RunDynamicPickUpWaitAdvanceMonitorAsync(CancellationToken monitorCt, CancellationToken moveCt)
        {
            HashSet<string> loggedReasons = new HashSet<string>();
            try
            {
                while (!monitorCt.IsCancellationRequested)
                {
                    if (_dynamicWaitAdvanceMoveTask != null)
                        return;

                    PickerPickUpMotionConfig config;
                    string gateDetail;
                    if (IsDynamicPickUpWaitGateSatisfied(out config, out gateDetail))
                    {
                        TryIssueDynamicPickUpWaitAdvance(config, loggedReasons, moveCt);
                        if (_dynamicWaitAdvanceMoveTask != null)
                            return;
                    }
                    else if (!string.Equals(gateDetail, "switchOff", StringComparison.Ordinal) &&
                             !string.Equals(gateDetail, "noConfig", StringComparison.Ordinal))
                    {
                        // 스위치 Off/설정 부재는 로그 없이 무동작(R3·검증 F1: Off = 로그 포함
                        // 기존과 완전 동일). 그 외 게이트 미충족 사유는 세션당 1회만 기록.
                        LogDynamicWaitSkipOnce(loggedReasons, "gate:" + gateDetail, gateDetail);
                    }

                    await Task.Delay(200, monitorCt).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 동적 선행 대기점 모니터 예외(기능 중단, 기존 대기 유지). error=" + ex.Message + " - Check");
            }
        }

        // 게이트 ③④ + 기본 참조 확인. 촬영 진행 중(①)은 코디네이터 조회, 대기 상태(②)는
        // 모니터가 허가 대기 await 동안만 살아 있는 구조 자체로 보장된다.
        private bool IsDynamicPickUpWaitGateSatisfied(out PickerPickUpMotionConfig config, out string detail)
        {
            // 판정 순서(검증 F1): 스위치 Off를 최우선 무로그 차단해 Off 상태에서는 실행 모드와
            // 무관하게 로그 포함 기존 동작과 완전히 동일하게 만든다.
            config = Side == PickerSequenceSide.Front
                ? (FrontPicker != null && FrontPicker.Config != null ? FrontPicker.Config.PickUp : null)
                : (RearPicker != null && RearPicker.Config != null ? RearPicker.Config.PickUp : null);
            if (config == null)
            {
                detail = "noConfig";
                return false;
            }
            config.Ensure();

            if (!config.PickUpDynamicWaitMode)
            {
                detail = "switchOff";
                return false;
            }

            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
            {
                detail = "notAuto";
                return false;
            }

            // 검증 A7: Motion Only Test에서도 선행검사 Task/배치 Publish는 수행되므로 명시 차단
            // (지시서 A7 '자연 비활성' 전제가 성립하지 않음 — 무변경 원칙을 명시 게이트로 보장).
            if (Options.PickerMotionOnlyTestMode)
            {
                detail = "motionOnlyTest";
                return false;
            }

            if (config.TransferMotionMode != PickerPickUpTransferMotionMode.ContiSegmentedPickUp)
            {
                detail = "notConti";
                return false;
            }

            if (!InputCameraPreInspectionCoordinator.IsInspectionRunning(Side))
            {
                detail = "inspectionNotRunning";
                return false;
            }

            detail = "ok";
            return true;
        }

        private void TryIssueDynamicPickUpWaitAdvance(
            PickerPickUpMotionConfig config,
            HashSet<string> loggedReasons,
            CancellationToken moveCt)
        {
            // 산출 코어는 베이스 공용 리졸버 재사용(Place 복귀 직행과 동일 산식 공유 — 중복 금지).
            DynamicPickUpWaitTarget resolved;
            string failReasonKey;
            string failDetail;
            if (!TryResolveDynamicPickUpWaitTargetX(config, out resolved, out failReasonKey, out failDetail))
            {
                LogDynamicWaitSkipOnce(loggedReasons, failReasonKey, failDetail);
                return;
            }

            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            if (pickerX == null)
            {
                LogDynamicWaitSkipOnce(loggedReasons, "noAxis", "PickerX 축 참조 없음");
                return;
            }

            double waitX = resolved.WaitX;
            double currentX = pickerX.ActualPosition;
            bool forward = resolved.Direction > 0 ? waitX > currentX + 0.5 : waitX < currentX - 0.5;
            if (!forward)
            {
                LogDynamicWaitSkipOnce(loggedReasons, "noForwardGain",
                    "후퇴/무이득 — waitX=" + waitX.ToString("F3") + ", currentX=" + currentX.ToString("F3"));
                return;
            }

            // MotionGuard 전 규칙 dry-run(알람 없는 판정) — 차단 사유가 있으면 발행하지 않는다.
            // targetName은 유닛 가드 조립 형식과 동일하게 구성(명시 PickerZone 토큰은 재부착 안 됨).
            string moveTargetName = "PickUpDynamicWait;PickerZone=Input";
            string dryRunName = (Side == PickerSequenceSide.Front ? "FrontPicker" : "RearPicker") +
                                ";PickerX;" + moveTargetName;
            string guardReason;
            if (!MotionGuardRuntime.CanAxisTeachingMove(pickerX, waitX, dryRunName, out guardReason))
            {
                LogDynamicWaitSkipOnce(loggedReasons, "guardDryRun", guardReason);
                return;
            }

            _dynamicWaitAdvanceTarget = waitX;
            _dynamicWaitAdvanceMoveTask = MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                waitX,
                "PickUp 동적 선행 대기점",
                moveCt,
                moveTargetName);

            WriteLog("PickerProcessSequence",
                Name + " PickUp 동적 선행 대기점 이동 발행. constraintVisionX=" + resolved.ConstraintVisionX.ToString("F3") +
                ", batchVisionXRange=" + resolved.MinVisionX.ToString("F3") + "~" + resolved.MaxVisionX.ToString("F3") +
                ", dieCount=" + resolved.DieCount +
                ", homeGap=" + resolved.HomeGap.ToString("F3") +
                ", safetyGap=" + resolved.SafetyGap.ToString("F3") +
                ", extraMargin=" + resolved.ExtraMargin.ToString("F3") +
                ", direction=" + resolved.Direction +
                ", waitX=" + waitX.ToString("F3") +
                ", currentX=" + currentX.ToString("F3") +
                ", advance=" + Math.Abs(currentX - waitX).ToString("F3") + " - Start");
        }

        private void LogDynamicWaitSkipOnce(HashSet<string> loggedReasons, string key, string detail)
        {
            if (loggedReasons == null || !loggedReasons.Add(key))
                return;

            WriteLog("PickerProcessSequence",
                Name + " PickUp 동적 선행 대기점 스킵. reason=" + key +
                ", detail=" + (detail ?? "-") + " - Check");
        }

        // 대기 종료(허가/실패/정지) 시 선행 이동을 join한다 — 픽업 X 진입/정지 경로와의
        // 재명령 충돌(AXM 0x1038) 차단. 실패는 알람 재승격 없이 로그만(가드 차단은 dry-run이
        // 사전에 거르고, 차단이 실제 발생했다면 가드 레이어가 이미 알람을 올렸다).
        private async Task JoinDynamicPickUpWaitAdvanceAsync(string reason)
        {
            Task<int> move = _dynamicWaitAdvanceMoveTask;
            if (move == null)
                return;
            _dynamicWaitAdvanceMoveTask = null;

            try
            {
                // 검증 R5/공정 속도 규칙: 이동 시간은 MotionSpeedScale에 비례하므로 join 캡도 스케일한다.
                int joinTimeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(15000);
                Task completed = await Task.WhenAny(move, Task.Delay(joinTimeoutMs)).ConfigureAwait(false);
                if (completed != move)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 동적 선행 대기점 이동 join 타임아웃 — 백그라운드 관찰로 전환합니다. " +
                        "timeoutMs=" + joinTimeoutMs +
                        ", reason=" + reason +
                        ", target=" + _dynamicWaitAdvanceTarget.ToString("F3") + " - Check");
                    move.ContinueWith(
                        t =>
                        {
                            if (t.IsFaulted && t.Exception != null)
                                t.Exception.Flatten();
                        },
                        TaskScheduler.Default);
                    return;
                }

                int result = move.Status == TaskStatus.RanToCompletion ? move.Result : -1;
                if (move.IsFaulted && move.Exception != null)
                    move.Exception.Flatten();

                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                double actual = pickerX != null ? pickerX.ActualPosition : double.NaN;
                WriteLog("PickerProcessSequence",
                    Name + " 동적 선행 대기점 이동 join 완료. reason=" + reason +
                    ", result=" + result +
                    ", target=" + _dynamicWaitAdvanceTarget.ToString("F3") +
                    ", actual=" + (double.IsNaN(actual) ? "-" : actual.ToString("F3")) +
                    " - " + (result == 0 ? "Ok" : "Check"));
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 동적 선행 대기점 이동 join 중 예외(무시). reason=" + reason +
                    ", error=" + ex.Message + " - Check");
            }
        }

        private void CompleteProcessWithoutNewPick(string boundary)
        {
            InputCameraPickUpPermissionStore.Clear(Side);
            ReleasePickerProcessPhase("StopAfterDrain:" + (boundary ?? "-"));
            CurrentStep = PickerProcessStep.Complete;
            WriteLog("WaferCompletionRun",
                Name + " Stop After Drain 요청으로 신규 Pick 공정을 시작하지 않습니다. " +
                "side=" + Side + ", boundary=" + (boundary ?? "-") + " - Ok");
        }

        private void StartInputVisionXPrePositionAfterPickUpComplete(CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return;
                if (!IsBottomAndSidePipelineModeEnabled())
                    return;

                List<int> loadedPickerIndexes = BuildLoadedPickerIndexesInRunOrder("InputVisionXPrePosition");
                if (loadedPickerIndexes == null || loadedPickerIndexes.Count == 0)
                {
                    WriteLog("InputVisionXPrePosition",
                        Name + " PickUp 완료 후 InputVisionX 선행이동을 생략합니다. Picker에 제품이 없습니다. side=" + Side + " - Check");
                    return;
                }

                bool started = InputVisionXPrePositionCoordinator.EnsureStarted(
                    Context,
                    Side,
                    Options,
                    ct,
                    Name + ":PickUpCompleteToBottom",
                    result => OnInputVisionXPrePositionArrived(result, ct));

                WriteLog("InputVisionXPrePosition",
                    Name + " PickUp 완료 후 InputVisionX 선행이동 요청 결과. " +
                    "side=" + Side +
                    ", loadedPickerCount=" + loadedPickerIndexes.Count +
                    ", started=" + started + " - Check");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputVisionXPrePosition",
                    Name + " PickUp 완료 후 InputVisionX 선행이동 요청 중 예외가 발생했습니다. " +
                    "side=" + Side + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        // [A안, 사용자 승인 2026-07-27] 선행이동 세션이 최종 검사 위치 도착으로 정상 종료되면
        // 비침습 InputCamera 선행검사 시작을 1회 재시도한다 — PickUp 완료 시점의 시도가 레일 간격
        // 사전검사로 거절된 뒤 다음 시퀀스 이벤트(Place 완료 등)까지 검사가 기동되지 않던 공백
        // (실장비 2026-07-27 06:02:10.4→12.78, 약 2.4초) 제거. 시작 가능 판정
        // (CanStartSafeInputCameraPreInspection)과 중복 기동 방지(코디네이터)는 기존 그대로다.
        private void OnInputVisionXPrePositionArrived(int sessionResult, CancellationToken ct)
        {
            try
            {
                if (sessionResult != 0)
                    return;
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return;

                WriteLog("PickerProcessSequence",
                    Name + " InputVisionX 선행이동 도착 — 비침습 InputCamera 선행검사 시작을 재시도합니다. " +
                    "side=" + Side + " - Check");
                StartSafeInputCameraPreInspectionsAfterPickUpComplete(ct, "VisionPrePositionArrived");
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 선행이동 도착 후 선행검사 재시도 중 예외(무시). " +
                    "side=" + Side + ", error=" + ex.Message + " - Check");
            }
        }

        private void StartSafeInputCameraPreInspectionsAfterPickUpComplete(CancellationToken ct, string reason)
        {
            if (_resumeDrainTurnHeld)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 재시작 드레인 중이라 PickUp 완료 후 InputCamera 선행검사 예약을 보류합니다. " +
                    "Place/Avoid/Output검사 완료 후 다음 run/공정 경계에서 다시 판단합니다. side=" +
                    Side + ", reason=" + reason + " - Check");
                return;
            }

            PickerSequenceSide oppositeSide = Side == PickerSequenceSide.Front
                ? PickerSequenceSide.Rear
                : PickerSequenceSide.Front;

            StartSafeInputCameraPreInspectionForSideIfNeeded(oppositeSide, ct, reason + ":OppositeCandidate");
            StartSafeInputCameraPreInspectionForSideIfNeeded(Side, ct, reason);
        }

        private void StartSafeInputCameraPreInspectionForSideIfNeeded(PickerSequenceSide targetSide, CancellationToken ct, string reason)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return;

                if (!IsPickerSideEnabled(targetSide))
                    return;

                string blockReason;
                if (!CanStartSafeInputCameraPreInspection(targetSide, reason, out blockReason))
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 비침습 InputCamera 선행검사를 시작하지 않습니다. " +
                        "PickerProcessSequence가 Front/Rear 상태를 확인한 결과 아직 안전 조건이 아닙니다. " +
                        "targetSide=" + targetSide +
                        ", reason=" + reason +
                        ", detail=" + blockReason + " - Wait");
                    return;
                }

                PickerSequenceOptions runOptions = BuildChildSequenceOptions();
                runOptions.InputCameraPreInspectionMode = true;
                runOptions.RequireInputCameraMarkInspectionPermission = false;

                if (targetSide != Side)
                {
                    runOptions.PickerNo = 0;
                    runOptions.RestrictToPickerNo = 0;
                }

                bool started = InputCameraPreInspectionCoordinator.EnsureStarted(
                    Context,
                    targetSide,
                    runOptions,
                    ct,
                    Name + ":" + reason + ":SafeNonPickerMotion:Target=" + targetSide);

                if (started)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " 비침습 InputCamera 선행검사를 시작했습니다. " +
                        "targetSide=" + targetSide +
                        ", reason=" + reason +
                        ", ownerSide=" + Side + " - Start");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 비침습 InputCamera 선행검사 시작 요청 중 예외가 발생했습니다. ownerSide=" + Side +
                    ", targetSide=" + targetSide +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        // 기존 조건: 안전 판정(허가/픽커 Input 존/공용 레일 dry-run) 구현이 이 클래스의 private 메서드였다.
        // 현재 기준: InputVisionPrefetchRunner(주기 재시도 실행자)와 공유하기 위해
        //           InputCameraPreInspectionStartGate로 기계적으로 추출하고 여기서는 위임한다.
        private bool CanStartSafeInputCameraPreInspection(PickerSequenceSide targetSide, string reason, out string blockReason)
        {
            return InputCameraPreInspectionStartGate.CanStart(Context, out blockReason);
        }

        // 기존 조건: FIX-D 공용 레일 dry-run 판정(IsSharedRailContendedForInputVisionStart)과
        //           보조 헬퍼(BuildNearestInputVisionTargetDetail/IsPickerInputZoneMotionRiskForProcess/
        //           IsPickerBlockingInputCameraPreInspection)가 이 클래스에 있었다.
        // 현재 기준: InputCameraPreInspectionStartGate로 기계적으로 이동 (러너와 공용).

        private bool IsPickerSideEnabled(PickerSequenceSide targetSide)
        {
            try
            {
                if (targetSide == PickerSequenceSide.Front)
                    return FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;

                return RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Bottom·Side Inspection

        private async Task<int> RunBottomInspectionAsync(CancellationToken ct)
        {
            if ((Options == null || Options.RunMode == SequenceRunMode.Auto) &&
                IsBottomAndSidePipelineModeEnabled())
            {
                return await RunBottomAndSideInspectionAsync(ct).ConfigureAwait(false);
            }

            bool keepPhaseSignal = false;

            try
            {
                int oppositeSideWaitResult = await WaitOppositePendingSideInspectionBeforeBottomAsync(
                    "BottomInspection",
                    ct).ConfigureAwait(false);
                if (oppositeSideWaitResult != 0)
                    return oppositeSideWaitResult;

                int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.BottomInspection, "BottomInspection", ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                SetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "Bottom");

                if (_bottomInspectionSequence == null || _bottomInspectionSequence.IsComplete)
                    _bottomInspectionSequence = new PickerBottomInspectionSequence(Context, Side);

                int result = await SequenceTrace.ChildAsync("PickerBottomInspectionSequence", "BottomInspection",
                    () => _bottomInspectionSequence.RunAsync(ct, BuildChildSequenceOptions(
                        Options == null || Options.RunMode == SequenceRunMode.Auto,
                        false,
                        false)),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("BottomInspectionFailed");
                    return result;
                }

                if (_bottomInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnBottomInspectionCompleteSignal(), "BottomComplete");
                    _bottomInspectionSequence = null;
                    _bottomInspectionCompletedInCurrentRun = true;
                    int nextPhaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.SideInspection, "BottomToSideInspection", ct).ConfigureAwait(false);
                    if (nextPhaseResult != 0)
                        return nextPhaseResult;

                    CurrentStep = PickerProcessStep.RunSideInspection;
                }
                else
                {
                    keepPhaseSignal = true;
                }

                return 0;
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
                return Fail("PICKER-PROCESS-BOTTOM-EX", Name,
                    "Bottom 검사 공정 실행 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepPhaseSignal)
                    ResetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "Bottom");
            }
        }

        private bool IsBottomAndSidePipelineModeEnabled()
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null || vision.Config == null)
                    return true;

                if (vision.Config.PickerInspectionMode == PickerInspectionPipelineMode.BottomAndSidePipeline)
                    return true;

                if (Options == null || Options.RunMode == SequenceRunMode.Auto)
                {
                    // 현재 기준: Auto에서는 Bottom 후 Y/X Avoid 복귀 없이 Bottom/Side 통합 검사만 사용한다.
                    WriteLog("PickerProcessSequence",
                        Name + " Auto Bottom/Side 검사는 통합 파이프라인으로 강제합니다. " +
                        "기존 조건: VisionConfig.PickerInspectionMode=" + vision.Config.PickerInspectionMode +
                        "이면 Serial Bottom 후 Side 경로를 사용할 수 있었습니다. " +
                        "현재 필요 여부: 실장비 안전을 위해 Auto에서는 사용하지 않습니다. side=" + Side + " - Check");
                    return true;
                }

                return false;
            }
            catch
            {
                // 현재 기준: 모드 판정 실패 시 Auto 공정은 Serial 구경로보다 통합 파이프라인을 우선한다.
                return true;
            }
            finally
            {
            }
        }

        private async Task<int> RunBottomAndSideInspectionAsync(CancellationToken ct)
        {
            bool keepBottomSignal = false;
            bool keepSideSignal = false;

            try
            {
                int oppositeSideWaitResult = await WaitOppositePendingSideInspectionBeforeBottomAsync(
                    "BottomAndSideInspection",
                    ct).ConfigureAwait(false);
                if (oppositeSideWaitResult != 0)
                    return oppositeSideWaitResult;

                int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.BottomInspection, "BottomAndSideInspection", ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                SetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "BottomAndSide-Bottom");
                SetPickerPhaseSignal(GetOwnSideInspectionSignal(), "BottomAndSide-Side");

                if (_bottomAndSideInspectionSequence == null || _bottomAndSideInspectionSequence.IsComplete)
                    _bottomAndSideInspectionSequence = new PickerBottomAndSideInspectionSequence(Context, Side);

                _bottomAndSideInspectionSequence.ForceBottomInspectionBeforeSideResume = _forceBottomInspectionBeforeSideResume;
                _bottomAndSideInspectionSequence.PickerZStageSafeConfirmedByPickUp =
                    _pickerZStageSafeConfirmedByPickUp;
                _pickerZStageSafeConfirmedByPickUp = false;
                if (_forceBottomInspectionBeforeSideResume)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " Bottom/Side 통합 검사에 Side 단독 재개 금지 옵션을 전달했습니다. " +
                        "기존 Bottom 결과가 있어도 Bottom shot부터 다시 진행합니다. side=" + Side + " - Check");
                }

                int result = await SequenceTrace.ChildAsync("PickerBottomAndSideInspectionSequence", "BottomAndSideInspection",
                    () => _bottomAndSideInspectionSequence.RunAsync(ct, BuildChildSequenceOptions(
                        true,
                        true,
                        true)),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    if (_bottomAndSideInspectionSequence != null)
                    {
                        _bottomAndSideInspectionSequence.Abort();
                        _bottomAndSideInspectionSequence = null;
                    }

                    ReleasePickerProcessPhase("BottomAndSideInspectionFailed");
                    return result;
                }

                if (_bottomAndSideInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnBottomInspectionCompleteSignal(), "BottomComplete");
                    SetPickerPhaseSignal(GetOwnSideInspectionCompleteSignal(), "SideComplete");
                    _bottomInspectionCompletedInCurrentRun = false;
                    _forceBottomInspectionBeforeSideResume = false;

                    int nextPhaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.Place, "BottomAndSideInspectionToPlace", ct).ConfigureAwait(false);
                    if (nextPhaseResult != 0)
                        return nextPhaseResult;

                    CurrentStep = PickerProcessStep.RunPlace;
                    EnableContinuousPlaceEntryFromInspection("BottomAndSideInspectionToPlace");
                    EnableSafePlaceEntryIfResumeDrain("BottomAndSideInspectionToPlace");
                    WriteLog("PickerProcessSequence",
                        Name + " Bottom/Side EPD 완료 후 Place 이동을 시작합니다. " +
                        "최종 RESULT 수집 객체는 Picker별 Place Z 하강 배리어까지 유지합니다. side=" + Side + " - Ok");
                }
                else
                {
                    keepBottomSignal = true;
                    keepSideSignal = true;
                }

                return 0;
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
                return Fail("PICKER-PROCESS-BOTTOM-SIDE-EX", Name,
                    "Bottom/Side 통합 검사 공정 실행 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepBottomSignal)
                    ResetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "BottomAndSide-Bottom");
                if (!keepSideSignal)
                    ResetPickerPhaseSignal(GetOwnSideInspectionSignal(), "BottomAndSide-Side");
            }
        }

        private async Task<int> WaitOppositePendingSideInspectionBeforeBottomAsync(
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                {
                    string manualBlockDetail;
                    if (IsOppositePendingSideInspectionBottomEntryBlocked(out manualBlockDetail))
                    {
                        return Fail("PICKER-BOTTOM-SIDE-PENDING-BLOCK", Name,
                            "상대 Picker가 Side 검사 대기 상태라 Bottom 검사 진입이 차단되었습니다. " +
                            "수동/Step 모드에서는 대기하지 않습니다. side=" + Side +
                            ", description=" + description +
                            ", detail=" + manualBlockDetail);
                    }

                    return 0;
                }

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitOppositePendingSideBeforeBottom",
                            ShouldDeferCycleStopForActivePickerDrain(),
                            "Picker target die/process drain");

                    string detail;
                    if (!IsOppositePendingSideInspectionBottomEntryBlocked(out detail))
                        break;

                    if (!waitLogged)
                    {
                        WriteLog("PickerSidePendingGate",
                            Name + " Bottom 검사 진입 대기. 상대 Picker가 Side 검사 대기 상태이고 " +
                            "Front/Rear PickerY 돌출 및 PickerX 엔코더 안전거리 조건이 해제될 때까지 기다립니다. " +
                            "side=" + Side +
                            ", description=" + description +
                            ", detail=" + detail + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerSidePendingGate",
                        Name + " Bottom 검사 진입 대기 완료. 상대 Picker Side 검사 대기/엔코더 간섭 조건 해제 확인. " +
                        "side=" + Side +
                        ", description=" + description + " - Ok");
                }

                return 0;
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
                return Fail("PICKER-BOTTOM-SIDE-PENDING-EX", Name,
                    "상대 Picker Side 검사 대기 상태 확인 중 예외가 발생했습니다. " +
                    "side=" + Side +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOppositePendingSideInspectionBottomEntryBlocked(out string detail)
        {
            detail = string.Empty;

            try
            {
                string pendingDetail;
                if (!HasOppositePickerPendingSideInspection(out pendingDetail))
                    return false;

                List<int> loadedPickerIndexes = BuildLoadedPickerIndexesInRunOrder("OppositePendingSideGate");
                if (loadedPickerIndexes == null || loadedPickerIndexes.Count == 0)
                {
                    detail = "내 Picker에 Bottom 검사 대상 제품이 없습니다. " + pendingDetail;
                    return false;
                }

                int pickerIndex = loadedPickerIndexes[0];
                double bottomX = ResolvePickerZoneX("DieBottomPosition", pickerIndex);
                double bottomY = ResolvePickerZoneY("DieBottomPosition", pickerIndex);

                string encoderDetail;
                bool canEnter = PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock(
                    Context != null ? Context.Machine : null,
                    Side == PickerSequenceSide.Front,
                    PickerAxis.PickerY,
                    bottomY,
                    BuildPickerTargetName("DieBottomPosition", pickerIndex) + ";PickerPhase=BottomEntry;OppositeSidePending",
                    bottomX,
                    null,
                    out encoderDetail);

                if (canEnter)
                    return false;

                detail = pendingDetail +
                         ", pickerNo=" + ToPickerNo(pickerIndex) +
                         ", bottomX=" + bottomX.ToString("0.###") +
                         ", bottomY=" + bottomY.ToString("0.###") +
                         ", encoder=" + encoderDetail;
                return true;
            }
            catch (Exception ex)
            {
                detail = "상대 Picker Side 검사 대기/엔코더 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private bool HasOppositePickerPendingSideInspection(out string detail)
        {
            detail = string.Empty;

            try
            {
                MaterialLocationKind oppositeLocation = Side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerRear
                    : MaterialLocationKind.PickerFront;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(oppositeLocation, pickerNo);
                    if (die == null)
                        continue;

                    bool bottomDone = HasInspectionResult(die, "Bottom");
                    bool side0Done = HasInspectionResult(die, "Side0");
                    bool side90Done = HasInspectionResult(die, "Side90");
                    bool placeReady = IsPlaceResultReady(die);

                    if (!bottomDone)
                        continue;

                    if (side0Done && side90Done && placeReady)
                        continue;

                    detail = "상대 Picker가 Side 검사 대기 상태입니다. " +
                             "oppositeLocation=" + oppositeLocation +
                             ", oppositePickerNo=" + pickerNo +
                             ", die=" + die.DieId +
                             ", bottomDone=" + bottomDone +
                             ", side0Done=" + side0Done +
                             ", side90Done=" + side90Done +
                             ", placeReady=" + placeReady;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                detail = "상대 Picker Side 검사 대기 제품 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private async Task<int> PrepareBottomAndSideInspectionXWaitPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (!IsBottomAndSidePipelineModeEnabled())
                    return 0;

                string oppositePhaseReason;
                if (!IsOppositePickerProcessActiveForBottomAndSideWait(out oppositePhaseReason))
                {
                    WriteLog("BottomSideXWaitPrepare",
                        Name + " Bottom/Side 통합 검사 X 대기 위치 준비 생략. 상대 Picker 프로세스가 완료 상태입니다. " +
                        "side=" + Side + ", " + oppositePhaseReason + " - Check");
                    return 0;
                }

                if (_bottomAndSideInspectionSequence != null && !_bottomAndSideInspectionSequence.IsComplete)
                    return 0;

                List<int> loadedPickerIndexes = BuildLoadedPickerIndexesInRunOrder("BottomSideXWaitPrepare");
                if (loadedPickerIndexes == null || loadedPickerIndexes.Count == 0)
                {
                    WriteLog("BottomSideXWaitPrepare",
                        Name + " Bottom/Side 통합 검사 X 대기 위치 준비 생략. Picker에 제품이 없습니다. side=" + Side + " - Check");
                    return 0;
                }

                int pickerIndex = loadedPickerIndexes[0];
                int pickerNo = ToPickerNo(pickerIndex);

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Bottom/Side 통합 검사 X 대기 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    yAvoid,
                    "Bottom/Side 통합 검사 X 대기 전 PickerY Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=BottomSideXWait").ConfigureAwait(false);
                if (result != 0)
                    return result;

                double bottomX = ResolvePickerZoneX("DieBottomPosition", pickerIndex);
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    bottomX,
                    "Bottom/Side 통합 검사 X 대기 위치 이동",
                    ct,
                    BuildPickerTargetName("DieBottomPosition", pickerIndex) + ";PickerPhase=BottomSideXWait;YHold").ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("BottomSideXWaitPrepare",
                    Name + " Bottom/Side 통합 검사 대기 위치 준비 완료. " +
                    "PickerY는 Avoid를 유지하고 PickerX만 Bottom 위치로 선행 이동했습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", bottomX=" + bottomX + " - Ok");

                return 0;
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
                return Fail("PICKER-BOTTOM-SIDE-X-WAIT-EX", Name,
                    "Bottom/Side 통합 검사 X 대기 위치 준비 중 예외가 발생했습니다. " +
                    "side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOppositePickerProcessActiveForBottomAndSideWait(out string reason)
        {
            reason = string.Empty;

            try
            {
                if (Context == null || Context.PickerPhases == null)
                {
                    reason = "PickerPhaseCoordinator=null";
                    return false;
                }

                PickerPhaseSnapshot snapshot = Context.PickerPhases.GetSnapshot();
                PickerPhaseState opposite = Side == PickerSequenceSide.Front ? snapshot.Rear : snapshot.Front;
                bool active = opposite.Phase != PickerProcessPhase.Idle;

                reason = "opposite=" + opposite + ", active=" + active;
                return active;
            }
            catch (Exception ex)
            {
                reason = "상대 Picker phase 확인 중 예외가 발생했습니다. error=" + ex.Message;
                WriteLog("BottomSideXWaitPrepare",
                    Name + " " + reason + " 안전을 위해 X 대기 준비 조건을 활성으로 판단합니다. side=" + Side + " - Failed");
                return true;
            }
            finally
            {
            }
        }

        private async Task<int> RunSideInspectionAsync(CancellationToken ct)
        {
            bool keepPhaseSignal = false;

            try
            {
                int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.SideInspection, "SideInspection", ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                SetPickerPhaseSignal(GetOwnSideInspectionSignal(), "Side");

                if (_sideInspectionSequence == null || _sideInspectionSequence.IsComplete)
                    _sideInspectionSequence = new PickerSideInspectionSequence(Context, Side);

                int result = await SequenceTrace.ChildAsync("PickerSideInspectionSequence", "SideInspection",
                    () => _sideInspectionSequence.RunAsync(ct, BuildChildSequenceOptions(
                        false,
                        _bottomInspectionCompletedInCurrentRun,
                        Options == null || Options.RunMode == SequenceRunMode.Auto)),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("SideInspectionFailed");
                    return result;
                }

                if (_sideInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnSideInspectionCompleteSignal(), "SideComplete");
                    _sideInspectionSequence = null;
                    _bottomInspectionCompletedInCurrentRun = false;

                    int nextPhaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.Place, "SideInspectionToPlace", ct).ConfigureAwait(false);
                    if (nextPhaseResult != 0)
                        return nextPhaseResult;

                    CurrentStep = PickerProcessStep.RunPlace;
                    EnableContinuousPlaceEntryFromInspection("SideInspectionToPlace");
                    EnableSafePlaceEntryIfResumeDrain("SideInspectionToPlace");
                }
                else
                {
                    keepPhaseSignal = true;
                }

                return 0;
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
                return Fail("PICKER-PROCESS-SIDE-EX", Name,
                    "Side 검사 공정 실행 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepPhaseSignal)
                    ResetPickerPhaseSignal(GetOwnSideInspectionSignal(), "Side");
            }
        }

        #endregion

        #region Place 및 Output Stage 대기

        private async Task<int> RunPlaceAsync(CancellationToken ct)
        {
            try
            {
                if (_placeSequence == null || _placeSequence.IsComplete)
                {
                    int readyResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.Place, "Place", ct).ConfigureAwait(false);
                    if (readyResult != 0)
                        return readyResult;

                    _placeSequence = new PickerPlaceSequence(Context, Side);
                    _placeSequence.ForceSafeYBeforeFirstPlaceMove = _forceSafeYBeforePlaceResume;
                    _placeSequence.KeepPickerYForwardDuringPlaceReadyWait = _keepPickerYForwardForContinuousPlace;
                    // SAFETY CONTRACT:
                    // - Bottom/Side EPD 완료와 최종 판정 완료는 같은 시점이 아닐 수 있다.
                    // - Bottom FINAL은 Place XY/보정 이동 전, Bottom+Side FINAL은 PickerZ의 최종
                    //   Place 접촉 하강 전에 각각 별도 배리어로 기다린다.
                    // - 이 callback 연결을 Place 완료 전 해제하거나 하나의 대기로 합치지 않는다.
                    if (_bottomAndSideInspectionSequence != null &&
                        _bottomAndSideInspectionSequence.IsComplete)
                    {
                        _placeSequence.WaitBottomFinalBeforePlaceMoveAsync =
                            _bottomAndSideInspectionSequence.WaitBottomFinalBeforePlaceMoveAsync;
                        _placeSequence.GetValidatedBottomPlaceResult =
                            _bottomAndSideInspectionSequence.GetValidatedBottomPlaceResult;
                        _placeSequence.WaitInspectionResultsBeforePlaceDownAsync =
                            _bottomAndSideInspectionSequence.WaitFinalResultsBeforePlaceDownAsync;
                    }
                    _placeSequence.ReleaseParentOutputWorkZoneAfterSafeAvoid = delegate(string description)
                    {
                        ReleasePickerWorkZone("PlaceSafeAvoid:" + (description ?? "-"));
                        return _workZoneLease == null;
                    };
                    _placeSequence.WaitForOutputStageExchangeWithProcessHandoffAsync =
                        WaitForOutputStageExchangeWithProcessHandoffAsync;
                    if (_forceSafeYBeforePlaceResume)
                    {
                        WriteLog("PickerProcessSequence",
                            Name + " Place 재시작 안전 진입 옵션을 전달했습니다. " +
                            "Place 가능 전에는 PickerY Avoid에서 대기하고, X/T 위치 이동 후 Y를 전진합니다. side=" + Side + " - Check");
                    }
                    else if (_keepPickerYForwardForContinuousPlace)
                    {
                        WriteLog("PickerProcessSequence",
                            Name + " 정상 연속 검사 후 Place 옵션을 전달했습니다. " +
                            "OutputStage 준비 대기 중 PickerY Avoid 복귀를 생략하고, 준비가 열리면 현재 Y 위치에서 Place 목표 Y로 직접 이동합니다. " +
                            "side=" + Side + " - Check");
                    }
                }

                int result = await SequenceTrace.ChildAsync("PickerPlaceSequence", "Place",
                    () => _placeSequence.RunAsync(ct, BuildChildSequenceOptions()),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("PlaceFailed");
                    return result;
                }

                if (_placeSequence.IsComplete)
                {
                    if (_bottomAndSideInspectionSequence != null)
                    {
                        if (_bottomAndSideInspectionSequence.HasPendingFinalResults)
                        {
                            return Fail("PICKER-PROCESS-PLACE-RESULT-PENDING", Name,
                                "Place 완료 시점에도 Picker별 Bottom/Side 최종 RESULT 적용 항목이 남아 있습니다. " +
                                "Place Z 하강 배리어 우회 여부를 확인하세요. side=" + Side + ".");
                        }

                        _bottomAndSideInspectionSequence.ReleaseDeferredFinalResultResources();
                        _bottomAndSideInspectionSequence = null;
                    }

                    ResetPickerPhaseSignals();
                    ReleasePickerProcessPhase("PlaceComplete");
                    _placeSequence = null;
                    _forceSafeYBeforePlaceResume = false;
                    _keepPickerYForwardForContinuousPlace = false;
                    WriteLog("PickerProcessSequence",
                        Name + " Place 완료 후 PickerProcessSequence가 다음 PickUp용 비침습 InputCamera 선행검사를 예약합니다. " +
                        "선행검사는 Picker 축을 직접 이동하지 않는 경로만 사용합니다. side=" +
                        Side + " - Check");
                    if (_resumeDrainTurnHeld)
                    {
                        WriteLog("PickerProcessSequence",
                            Name + " 재시작 드레인 중이라 Place 완료 직후 InputCamera 선행검사 예약을 보류합니다. " +
                            "Output camera 후검사 idle 확인 후 상대 Picker가 남은 작업을 처리하는 정책을 우선합니다. side=" +
                            Side + " - Check");
                    }
                    else
                    {
                        StartSafeInputCameraPreInspectionForSideIfNeeded(Side, ct, "PlaceComplete");
                    }

                    CurrentStep = PickerProcessStep.Complete;
                }

                return 0;
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
                return Fail("PICKER-PROCESS-PLACE-EX", Name,
                    "Place 공정 실행 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        // SAFETY CONTRACT:
        // - OutputStage 교체 handoff는 Picker 전체 Safe와 Output 작업영역 해제를 확인한 뒤
        //   phase/work-zone을 해제하고 Picker process resource를 반환해 Loader가 획득할 수 있게 한다.
        // - 보유 Die, cursor, 지연된 검사 RESULT 상태는 같은 sequence 인스턴스에 유지한다.
        // - 새 Stage Material Ready, 해당 Ready signal, Loader 비활성 조건 전에는 Place 리소스를 재획득해 진입하지 않는다.
        private async Task<int> WaitForOutputStageExchangeWithProcessHandoffAsync(
            BinSide outputSide,
            int pickerNo,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-MODE", Name,
                        "OutputStage 교체용 Picker Process 리소스 양도는 Auto 모드에서만 허용됩니다. " +
                        "side=" + Side + ", outputSide=" + outputSide + ", pickerNo=" + pickerNo);
                }

                if (Context == null ||
                    Context.AutoSequenceGate == null ||
                    !Context.AutoSequenceGate.IsOutputLoaderConfiguredForAutoRun)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-LOADER-DISABLED", Name,
                        "OutputStage 교체가 필요하지만 이번 Auto run에 OutputUnloader가 활성화되지 않았습니다. " +
                        "Picker는 전체 Avoid 상태에서 교체 대기를 시작하지 않습니다. " +
                        "side=" + Side + ", outputSide=" + outputSide + ", pickerNo=" + pickerNo);
                }

                if (_pickerProcessLease == null)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-RESOURCE", Name,
                        "OutputStage 교체용 Picker Process 리소스를 양도할 수 없습니다. 현재 리소스가 없습니다. " +
                        "side=" + Side + ", outputSide=" + outputSide + ", pickerNo=" + pickerNo);
                }

                if (_workZoneLease != null)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-WORK-ZONE", Name,
                        "OutputStage 교체용 Picker Process 리소스 양도 전에 Output 작업영역이 해제되지 않았습니다. " +
                        "side=" + Side + ", outputSide=" + outputSide + ", pickerNo=" + pickerNo);
                }

                int safeResult = VerifyPickerSafeAfterProcessResourceAcquire("OutputStageExchangeHandoffBeforeRelease");
                if (safeResult != 0)
                    return safeResult;

                // 물리 Safe와 child/work-zone 해제를 확인한 뒤 phase와 Process 리소스를 반환한다.
                // 동일 PickerProcessSequence 인스턴스는 유지하므로 보유 Die, Picker cursor,
                // Bottom/Side 최종 RESULT 대기 객체와 resume-drain 순서는 그대로 보존된다.
                ReleasePickerProcessPhase("OutputStageExchangeHandoff");
                if (_phaseLease != null || _workZoneLease != null)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-PHASE", Name,
                        "OutputStage 교체용 Picker phase/work-zone 해제에 실패했습니다. " +
                        "side=" + Side + ", outputSide=" + outputSide + ", pickerNo=" + pickerNo);
                }

                WriteLog("PickerOutputLoaderHandoff",
                    Name + " OutputStage Full 안전 대기에서 Picker Process 리소스를 Loader에 양도합니다. " +
                    "side=" + Side + ", outputSide=" + outputSide +
                    ", pickerNo=" + pickerNo +
                    ", description=" + (description ?? "-") + " - Start");

                ReleaseActivePickerProcessResource("OutputStageExchangeHandoff");

                int readyResult = await WaitForOutputStageReadyAfterProcessHandoffAsync(
                    outputSide,
                    pickerNo,
                    ct).ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;

                // Coordinator는 다른 OutputStage Full/완료 신호와 LoaderActive가 남아 있으면
                // 재점유를 계속 차단한다. Store/Supply 복구 작업도 Picker 리소스 없이 완료할 수 있다.
                int acquireResult = await AcquireActivePickerProcessResourceAsync(
                    "OutputStageExchangeHandoffComplete",
                    ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                safeResult = VerifyPickerSafeAfterProcessResourceAcquire("OutputStageExchangeHandoffComplete");
                if (safeResult != 0)
                    return safeResult;

                int phaseResult = await EnterOrTransitionPickerPhaseAsync(
                    PickerProcessPhase.Place,
                    "OutputStageExchangeHandoffResume",
                    ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                WriteLog("PickerOutputLoaderHandoff",
                    Name + " OutputStage 교체 완료 후 같은 Picker Process/Die 상태로 Place 재개 승인을 받았습니다. " +
                    "side=" + Side + ", outputSide=" + outputSide +
                    ", pickerNo=" + pickerNo +
                    ", description=" + (description ?? "-") + " - Ok");
                return 0;
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
                return Fail("PICKER-OUTPUT-HANDOFF-EX", Name,
                    "OutputStage 교체용 Picker Process 리소스 양도/재점유 중 예외가 발생했습니다. " +
                    "side=" + Side + ", outputSide=" + outputSide +
                    ", pickerNo=" + pickerNo + ", error=" + ex.Message);
            }
        }

        private async Task<int> WaitForOutputStageReadyAfterProcessHandoffAsync(
            BinSide outputSide,
            int pickerNo,
            CancellationToken ct)
        {
            DateTime waitStarted = DateTime.UtcNow;
            DateTime lastProgress = waitStarted;
            string lastWaitState = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested(
                    Name + ".OutputStageExchangeHandoffWait",
                    ShouldDeferCycleStopForActivePickerDrain(),
                    "Picker held die output stage exchange");

                string materialReason;
                bool materialReady = MaterialStateService.IsOutputStageReceiveAvailable(
                    outputSide,
                    out materialReason);
                string readySignal = outputSide == BinSide.Ng
                    ? "OutputNgStageReady"
                    : "OutputGoodStageReady";
                bool signalReady = Context != null && Context.Bus != null && Context.Bus.IsSet(readySignal);
                bool outputLoaderActive = Context != null &&
                                          Context.Bus != null &&
                                          Context.Bus.IsSet("OutputLoaderActive");
                DateTime now = DateTime.UtcNow;
                string waitState = "materialReady=" + materialReady +
                                   ", signalReady=" + signalReady +
                                   ", outputLoaderActive=" + outputLoaderActive +
                                   ", materialReason=" + (string.IsNullOrWhiteSpace(materialReason) ? "-" : materialReason);

                // 다른 side를 포함해 Loader가 실제 교체 작업 중이면 절대 경과시간으로 실패시키지 않는다.
                // Loader가 inactive인 상태에서는 Material/Ready 상태가 바뀐 시점부터 stall 시간을 다시 센다.
                if (outputLoaderActive || !string.Equals(lastWaitState, waitState, StringComparison.Ordinal))
                {
                    lastProgress = now;
                    lastWaitState = waitState;
                }

                if (materialReady && signalReady && !outputLoaderActive)
                {
                    WriteLog("PickerOutputLoaderHandoff",
                        Name + " OutputStage 교체 후 Material/Ready/Loader 완료를 확인했습니다. " +
                         "side=" + Side + ", outputSide=" + outputSide +
                         ", pickerNo=" + pickerNo +
                         ", signal=" + readySignal +
                         ", elapsedMs=" + (int)(now - waitStarted).TotalMilliseconds + " - Ok");
                    return 0;
                }

                if ((now - lastProgress).TotalMilliseconds >= OutputStageExchangeStallTimeoutMs)
                {
                    return Fail("PICKER-OUTPUT-HANDOFF-TIMEOUT", Name,
                        "OutputStage 교체 후 새 Stage 준비 상태가 제한시간 동안 진행되지 않았습니다. " +
                        "Picker는 Process 리소스를 반환하고 전체 Avoid 상태를 유지합니다. " +
                        "side=" + Side + ", outputSide=" + outputSide +
                        ", pickerNo=" + pickerNo +
                        ", stallTimeoutMs=" + OutputStageExchangeStallTimeoutMs +
                        ", totalElapsedMs=" + (int)(now - waitStarted).TotalMilliseconds +
                        ", state=" + waitState);
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }

        private void EnableSafePlaceEntryIfResumeDrain(string reason)
        {
            if (!_resumeDrainTurnHeld)
                return;

            _forceSafeYBeforePlaceResume = true;
            _keepPickerYForwardForContinuousPlace = false;
            WriteLog("PickerProcessSequence",
                Name + " 재시작 드레인 중 Place 진입이 예정되어 Place 첫 접근 안전 옵션을 강제합니다. " +
                "PickerY Avoid 정리 후 Picker X/T를 Place 티칭값으로 먼저 이동하고, 그 다음 PickerY 전진을 허용합니다. " +
                "side=" + Side +
                ", reason=" + reason +
                ", forceSafeYBeforePlaceResume=" + _forceSafeYBeforePlaceResume + " - Check");
        }

        // 기존 조건: UpdateNearestInputVisionCandidate가 이 클래스에 있었다.
        // 현재 기준: InputCameraPreInspectionStartGate로 기계적으로 이동 (러너와 공용).

        private void EnableContinuousPlaceEntryFromInspection(string reason)
        {
            if (_resumeDrainTurnHeld)
                return;

            _keepPickerYForwardForContinuousPlace = true;
            WriteLog("PickerProcessSequence",
                Name + " 정상 검사 완료 후 Place 진입으로 PickerY Forward 유지 옵션을 설정합니다. " +
                "Side 검사 종료 위치에서 PickerY Avoid 복귀 없이 Place 목표 Y로 직접 이동합니다. " +
                "side=" + Side +
                ", runMode=" + (Options != null ? Options.RunMode.ToString() : "-") +
                ", reason=" + reason +
                ", keepPickerYForwardForContinuousPlace=" + _keepPickerYForwardForContinuousPlace + " - Check");
        }

        #endregion

        #region Picker Phase·Work Zone

        private async Task<int> EnterOrTransitionPickerPhaseAsync(
            PickerProcessPhase requestedPhase,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Context == null || Context.PickerPhases == null)
                    return 0;

                if (_phaseLease != null && !_phaseLease.IsDisposed)
                {
                    string transitionReason;
                    if (Context.PickerPhases.TryTransition(_phaseLease, requestedPhase, out transitionReason))
                    {
                        int zoneResult = await EnterOrTransitionPickerWorkZoneAsync(requestedPhase, description, ct).ConfigureAwait(false);
                        if (zoneResult != 0)
                            return zoneResult;

                        WriteLog("PickerPhase",
                            Name + " Picker phase 전환 완료. side=" + Side +
                            ", phase=" + requestedPhase +
                            ", description=" + description + " - Ok");
                        return 0;
                    }

                    return await WaitPickerPhaseTransitionAsync(requestedPhase, description, transitionReason, ct).ConfigureAwait(false);
                }

                string enterReason;
                PickerPhaseLease lease;
                if (Context.PickerPhases.TryEnter(Side, requestedPhase, Name + ":" + description, out lease, out enterReason))
                {
                    _phaseLease = lease;
                    int zoneResult = await EnterOrTransitionPickerWorkZoneAsync(requestedPhase, description, ct).ConfigureAwait(false);
                    if (zoneResult != 0)
                        return zoneResult;

                    WriteLog("PickerPhase",
                        Name + " Picker phase 점유 완료. side=" + Side +
                        ", phase=" + requestedPhase +
                        ", description=" + description + " - Ok");
                    return 0;
                }

                return await WaitPickerPhaseEnterAsync(requestedPhase, description, enterReason, ct).ConfigureAwait(false);
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
                return Fail("PICKER-PHASE-EX", Name,
                    "Picker phase 진입/전환 중 예외가 발생했습니다. side=" + Side +
                    ", phase=" + requestedPhase +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerPhaseEnterAsync(
            PickerProcessPhase requestedPhase,
            string description,
            string firstReason,
            CancellationToken ct)
        {
            if (IsStepRunMode())
            {
                return Fail("PICKER-PHASE-BLOCK", Name,
                    "Picker phase 진입이 차단되었습니다. 수동/Step 모드에서는 대기하지 않습니다. " +
                    "side=" + Side +
                    ", phase=" + requestedPhase +
                    ", description=" + description +
                    ", reason=" + firstReason);
            }

            WriteLog("PickerPhase",
                Name + " Picker phase 진입 대기. side=" + Side +
                ", phase=" + requestedPhase +
                ", description=" + description +
                ", reason=" + firstReason + " - Wait");

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested(
                    Name + ".PickerPhaseEnter:" + requestedPhase,
                    ShouldDeferCycleStopForActivePickerDrain(),
                    "Picker target die/process drain");

                string reason;
                PickerPhaseLease lease;
                if (Context.PickerPhases.TryEnter(Side, requestedPhase, Name + ":" + description, out lease, out reason))
                {
                    _phaseLease = lease;
                    int zoneResult = await EnterOrTransitionPickerWorkZoneAsync(requestedPhase, description, ct).ConfigureAwait(false);
                    if (zoneResult != 0)
                        return zoneResult;

                    WriteLog("PickerPhase",
                        Name + " Picker phase 진입 대기 완료. side=" + Side +
                        ", phase=" + requestedPhase +
                        ", description=" + description + " - Ok");
                    return 0;
                }

                await Task.Delay(1, ct).ConfigureAwait(false);
            }
        }

        private async Task<int> WaitPickerPhaseTransitionAsync(
            PickerProcessPhase requestedPhase,
            string description,
            string firstReason,
            CancellationToken ct)
        {
            if (IsStepRunMode())
            {
                return Fail("PICKER-PHASE-TRANSITION-BLOCK", Name,
                    "Picker phase 전환이 차단되었습니다. 수동/Step 모드에서는 대기하지 않습니다. " +
                    "side=" + Side +
                    ", phase=" + requestedPhase +
                    ", description=" + description +
                    ", reason=" + firstReason);
            }

            WriteLog("PickerPhase",
                Name + " Picker phase 전환 대기. 현재 phase는 유지한 상태로 상대 Picker 조건을 기다립니다. " +
                "side=" + Side +
                ", phase=" + requestedPhase +
                ", description=" + description +
                ", reason=" + firstReason + " - Wait");

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested(
                    Name + ".PickerPhaseTransition:" + requestedPhase,
                    ShouldDeferCycleStopForActivePickerDrain(),
                    "Picker target die/process drain");

                string reason;
                if (Context.PickerPhases.TryTransition(_phaseLease, requestedPhase, out reason))
                {
                    int zoneResult = await EnterOrTransitionPickerWorkZoneAsync(requestedPhase, description, ct).ConfigureAwait(false);
                    if (zoneResult != 0)
                        return zoneResult;

                    WriteLog("PickerPhase",
                        Name + " Picker phase 전환 대기 완료. side=" + Side +
                        ", phase=" + requestedPhase +
                        ", description=" + description + " - Ok");
                    return 0;
                }

                await Task.Delay(1, ct).ConfigureAwait(false);
            }
        }

        private void ReleasePickerProcessPhase(string description)
        {
            try
            {
                ReleasePickerWorkZone(description);

                if (_phaseLease == null)
                    return;

                _phaseLease.Dispose();
                _phaseLease = null;
                WriteLog("PickerPhase",
                    Name + " Picker phase 해제. side=" + Side +
                    ", description=" + description + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPhase",
                    Name + " Picker phase 해제 실패. side=" + Side +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> EnterOrTransitionPickerWorkZoneAsync(
            PickerProcessPhase requestedPhase,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (Context == null || Context.AutoSequenceGate == null)
                    return 0;

                PickerWorkZone requestedZone = ResolvePickerWorkZoneForPhase(requestedPhase);
                if (requestedZone == PickerWorkZone.Unknown || requestedZone == PickerWorkZone.Avoid)
                {
                    ReleasePickerWorkZone(description + ":NoWorkZone");
                    return 0;
                }

                if (IsInputCameraOnlyPhase(requestedPhase, description))
                {
                    ReleasePickerWorkZone(description + ":InputCameraOnly");
                    WriteLog("PickerWorkZone",
                        Name + " InputCamera 선행검사 대기 단계는 Picker Input work zone을 점유하지 않습니다. " +
                        "InputCamera 자체가 중앙 Coordinator의 Input camera zone 승인을 받은 뒤 이동하고, 실제 PickUp 단계에서 Picker Input zone을 다시 승인받습니다. " +
                        "side=" + Side +
                        ", phase=" + requestedPhase +
                        ", description=" + description + " - Check");
                    return 0;
                }

                int physicalClearResult = await WaitOppositePickerPhysicalClearForWorkZoneAsync(
                    requestedZone,
                    requestedPhase,
                    description,
                    ct).ConfigureAwait(false);
                if (physicalClearResult != 0)
                    return physicalClearResult;

                string holder = Name + ":" + description + ":" + requestedPhase;
                if (_workZoneLease == null || _workZoneLease.IsDisposed)
                {
                    _workZoneLease = await Context.AutoSequenceGate
                        .BeginPickerWorkZoneAsync(Side, requestedZone, holder, ct)
                        .ConfigureAwait(false);
                }
                else
                {
                    _workZoneLease = await Context.AutoSequenceGate
                        .TransitionPickerWorkZoneAsync(_workZoneLease, requestedZone, holder, ct)
                        .ConfigureAwait(false);
                }

                WriteLog("PickerWorkZone",
                    Name + " Picker work zone 승인 완료. side=" + Side +
                    ", phase=" + requestedPhase +
                    ", zone=" + requestedZone +
                    ", description=" + description + " - Ok");
                return 0;
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
                return Fail("PICKER-WORK-ZONE-EX", Name,
                    "Picker work zone 승인 중 예외가 발생했습니다. side=" + Side +
                    ", phase=" + requestedPhase +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsInputCameraOnlyPhase(PickerProcessPhase requestedPhase, string description)
        {
            return requestedPhase == PickerProcessPhase.PickUp &&
                   !string.IsNullOrWhiteSpace(description) &&
                   description.IndexOf("InputCameraMarkInspection", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<int> WaitOppositePickerPhysicalClearForWorkZoneAsync(
            PickerWorkZone requestedZone,
            PickerProcessPhase requestedPhase,
            string description,
            CancellationToken ct)
        {
            try
            {
                PickerWorkZone normalizedRequestedZone =
                    PickerZoneInterlockRules.NormalizeInterlockZone(requestedZone);
                if (normalizedRequestedZone == PickerWorkZone.Unknown ||
                    normalizedRequestedZone == PickerWorkZone.Avoid)
                {
                    return 0;
                }

                if (Context == null || Context.Machine == null)
                    return 0;

                bool waitLogged = false;
                DateTime lastWaitLog = DateTime.MinValue;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    // 보유 Die가 없으면 CycleStop 경계에서 종료하고, 보유 Die Place drain 중이면
                    // 상대 Picker가 물리 zone을 비울 때까지 기존 phase/work-zone 정책과 동일하게 기다린다.
                    Context.StopIfCycleStopRequested(
                        Name + ".WaitOppositePickerPhysicalClear:" + normalizedRequestedZone,
                        ShouldDeferCycleStopForActivePickerDrain(),
                        "Picker target die/process drain");

                    string blockReason;
                    if (!IsOppositePickerPhysicallyBlockingWorkZone(
                        normalizedRequestedZone,
                        requestedPhase,
                        description,
                        out blockReason))
                    {
                        if (waitLogged)
                        {
                            WriteLog("PickerWorkZone",
                                Name + " 상대 Picker 물리 zone 대기 완료. 요청 zone 진입을 허용합니다. " +
                                "side=" + Side +
                                ", phase=" + requestedPhase +
                                ", zone=" + normalizedRequestedZone +
                                ", description=" + description + " - Ok");
                        }

                        return 0;
                    }

                    if ((DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= 1000.0)
                    {
                        lastWaitLog = DateTime.UtcNow;
                        waitLogged = true;
                        WriteLog("PickerWorkZone",
                            Name + " 상대 Picker 물리 zone 이탈 대기. phase/zone 승인 전에 실제 축 위치 기준으로 같은 작업 zone 침범을 보류합니다. " +
                            "side=" + Side +
                            ", phase=" + requestedPhase +
                            ", zone=" + normalizedRequestedZone +
                            ", description=" + description +
                            ", reason=" + blockReason + " - Wait");
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
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
                return Fail("PICKER-WORK-ZONE-PHYSICAL-GATE-EX", Name,
                    "상대 Picker 물리 zone 확인 중 예외가 발생했습니다. side=" + Side +
                    ", phase=" + requestedPhase +
                    ", zone=" + requestedZone +
                    ", description=" + description +
                    ", error=" + ex.Message);
            }
        }

        private bool IsOppositePickerPhysicallyBlockingWorkZone(
            PickerWorkZone requestedZone,
            PickerProcessPhase requestedPhase,
            string description,
            out string reason)
        {
            reason = string.Empty;

            bool oppositeIsFront = Side == PickerSequenceSide.Rear;
            PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                Context != null ? Context.Machine : null,
                oppositeIsFront,
                requestedZone,
                null,
                "PickerProcess physical zone gate; requester=" + Side +
                ";phase=" + requestedPhase +
                ";description=" + description);

            if (state == null)
                return false;

            PickerWorkZone currentZone = PickerZoneInterlockRules.NormalizeInterlockZone(state.CurrentZone);
            PickerWorkZone targetZone = PickerZoneInterlockRules.NormalizeInterlockZone(state.TargetZone);
            bool currentSame = PickerZoneInterlockRules.IsSameInterlockZone(currentZone, requestedZone);
            bool targetSame = PickerZoneInterlockRules.IsSameInterlockZone(targetZone, requestedZone);
            bool moving = (state.PickerX != null && state.PickerX.IsMoving) ||
                          (state.PickerY != null && state.PickerY.IsMoving);
            bool block = currentSame ||
                         targetSame ||
                         (state.BlocksTransport && (currentSame || targetSame)) ||
                         (state.UnknownUnsafe && moving);

            if (!block)
                return false;

            reason = "opposite=" + (oppositeIsFront ? "FrontPicker" : "RearPicker") +
                ", requestedZone=" + requestedZone +
                ", currentSame=" + currentSame +
                ", targetSame=" + targetSame +
                ", moving=" + moving +
                ", state=" + state.Describe();
            return true;
        }

        private static PickerWorkZone ResolvePickerWorkZoneForPhase(PickerProcessPhase phase)
        {
            switch (phase)
            {
                case PickerProcessPhase.PickUp:
                    return PickerWorkZone.Input;
                case PickerProcessPhase.BottomInspection:
                case PickerProcessPhase.SideInspection:
                    return PickerWorkZone.Bottom;
                case PickerProcessPhase.Place:
                    return PickerWorkZone.Output;
                default:
                    return PickerWorkZone.Unknown;
            }
        }

        private void ReleasePickerWorkZone(string description)
        {
            try
            {
                if (_workZoneLease == null)
                    return;

                _workZoneLease.Dispose();
                _workZoneLease = null;
                WriteLog("PickerWorkZone",
                    Name + " Picker work zone 해제. side=" + Side +
                    ", description=" + description + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkZone",
                    Name + " Picker work zone 해제 실패. side=" + Side +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        #endregion

        #region Signal·상대 Picker 상태

        private string GetOwnBottomInspectionSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "FrontPickerInBottomInspection"
                : "RearPickerInBottomInspection";
        }

        private string GetOwnSideInspectionSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "FrontPickerInSideInspection"
                : "RearPickerInSideInspection";
        }

        private string GetOwnBottomInspectionCompleteSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "FrontPickerBottomInspectionComplete"
                : "RearPickerBottomInspectionComplete";
        }

        private string GetOwnSideInspectionCompleteSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "FrontPickerSideInspectionComplete"
                : "RearPickerSideInspectionComplete";
        }

        private string GetOppositeBottomInspectionSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "RearPickerInBottomInspection"
                : "FrontPickerInBottomInspection";
        }

        private string GetOppositeSideInspectionSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "RearPickerInSideInspection"
                : "FrontPickerInSideInspection";
        }

        private string GetOppositeBottomInspectionCompleteSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "RearPickerBottomInspectionComplete"
                : "FrontPickerBottomInspectionComplete";
        }

        private string GetOppositeSideInspectionCompleteSignal()
        {
            return Side == PickerSequenceSide.Front
                ? "RearPickerSideInspectionComplete"
                : "FrontPickerSideInspectionComplete";
        }

        private string GetOppositeSideName()
        {
            return Side == PickerSequenceSide.Front ? "Rear" : "Front";
        }

        private bool IsSignalSet(string signalName)
        {
            try
            {
                return Context != null &&
                       Context.Bus != null &&
                       Context.Bus.IsSet(signalName);
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " signal state check failed. signal=" + signalName +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerPhaseReady(string runningSignal, string completedSignal)
        {
            try
            {
                return IsSignalSet(runningSignal) || IsSignalSet(completedSignal);
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " opposite picker phase state check failed. runningSignal=" + runningSignal +
                    ", completedSignal=" + completedSignal +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerSideEnabled()
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                    return RearPicker != null && RearPicker.Config != null && RearPicker.Config.UseUnit;

                return FrontPicker != null && FrontPicker.Config != null && FrontPicker.Config.UseUnit;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " opposite picker enable state check failed. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool HasLoadedDieOnOppositePicker()
        {
            try
            {
                MaterialLocationKind location = Side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerRear
                    : MaterialLocationKind.PickerFront;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (MaterialStateService.GetDieAtPicker(location, pickerNo) != null)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " opposite picker die state check failed. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
                return true;
            }
            finally
            {
            }
        }

        private void SetPickerPhaseSignal(string signalName, string phaseName)
        {
            try
            {
                if (Context == null || Context.Bus == null)
                    return;

                if (Context.Bus.IsSet(signalName))
                    return;

                Context.Bus.Set(signalName);
                WriteLog("PickerProcessSequence",
                    Name + " " + phaseName + " 검사 상태 신호를 설정했습니다. signal=" + signalName + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " " + phaseName + " 검사 상태 신호 설정 실패. signal=" + signalName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ResetPickerPhaseSignal(string signalName, string phaseName)
        {
            try
            {
                if (Context == null || Context.Bus == null)
                    return;

                if (!Context.Bus.IsSet(signalName))
                    return;

                Context.Bus.Reset(signalName);
                WriteLog("PickerProcessSequence",
                    Name + " " + phaseName + " 검사 상태 신호를 해제했습니다. signal=" + signalName + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " " + phaseName + " 검사 상태 신호 해제 실패. signal=" + signalName +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ResetPickerPhaseSignals()
        {
            try
            {
                ResetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "Bottom");
                ResetPickerPhaseSignal(GetOwnSideInspectionSignal(), "Side");
                ResetPickerPhaseSignal(GetOwnBottomInspectionCompleteSignal(), "BottomComplete");
                ResetPickerPhaseSignal(GetOwnSideInspectionCompleteSignal(), "SideComplete");
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " Picker 공정 상태 신호 초기화 실패. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        #endregion

        #region 하위 Picker Options

        private PickerSequenceOptions BuildChildSequenceOptions(
            bool keepZAfterBottomInspection = false,
            bool enterSideFromBottomInspection = false,
            bool keepZUntilSideInspectionComplete = false)
        {
            PickerSequenceOptions source = Options ?? PickerSequenceOptions.Default();
            bool isAuto = source.RunMode == SequenceRunMode.Auto;
            return new PickerSequenceOptions
            {
                RunMode = source.RunMode,
                StartMode = source.StartMode,
                FineMove = source.FineMove,
                MoveTimeoutMs = source.MoveTimeoutMs,
                ResourceTimeoutMs = source.ResourceTimeoutMs,
                PickerNo = source.PickerNo,
                RestrictToPickerNo = source.RestrictToPickerNo,
                VisionRetryCount = source.VisionRetryCount,
                InputDieVisionFailureAction = source.InputDieVisionFailureAction,
                SimulateVisionResult = source.SimulateVisionResult,
                PickerMotionOnlyTestMode = source.PickerMotionOnlyTestMode,
                RequireInputCameraMarkInspectionPermission = source.RequireInputCameraMarkInspectionPermission,
                InputCameraPreInspectionMode = source.InputCameraPreInspectionMode,
                KeepZAfterBottomInspection = source.KeepZAfterBottomInspection || (isAuto && keepZAfterBottomInspection),
                EnterSideFromBottomInspection = source.EnterSideFromBottomInspection || (isAuto && enterSideFromBottomInspection),
                KeepZUntilSideInspectionComplete = source.KeepZUntilSideInspectionComplete || (isAuto && keepZUntilSideInspectionComplete)
            };
        }

        #endregion
    }
}

