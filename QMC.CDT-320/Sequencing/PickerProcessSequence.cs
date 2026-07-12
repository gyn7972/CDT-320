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
        private PickerPickUpSequence _pickUpSequence;
        private PickerBottomInspectionSequence _bottomInspectionSequence;
        private PickerSideInspectionSequence _sideInspectionSequence;
        private PickerBottomAndSideInspectionSequence _bottomAndSideInspectionSequence;
        private PickerPlaceSequence _placeSequence;
        private PickerPhaseLease _phaseLease;
        private AutoSequencePickerWorkZoneLease _workZoneLease;
        private bool _bottomInspectionCompletedInCurrentRun;
        private bool _forceBottomInspectionBeforeSideResume;
        private bool _forceSafeYBeforePlaceResume;
        private bool _keepPickerYForwardForContinuousPlace;
        private bool _resumePartialPickUpWithoutMarkPermission;
        private bool _firstForwardTurnHandled;
        private bool _resumeDrainWaitHandled;
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
                using (SequenceResourceLease pickerLease = await AcquirePickerProcessResourceAsync(ct).ConfigureAwait(false))
                {
                    if (pickerLease == null)
                        return Fail("PICKER-RESOURCE", Name, "Picker 리소스 점유 실패. resource=" + PickerResourceKind);

                    if (IsStepRunMode())
                    {
                        return await ExecuteSingleProcessStepAsync(ct).ConfigureAwait(false);
                    }

                    return await ExecuteProcessUntilCompleteAsync(ct).ConfigureAwait(false);
                }
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
                bool held = await PickerFirstForwardSequencer.WaitResumeDrainTurnAsync(
                    Side,
                    Context,
                    msg => WriteLog("PickerResumeDrainSequencer", Name + " " + msg + " - Wait"),
                    ct).ConfigureAwait(false);

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

        private async Task<int> RunInputCameraMarkInspectionAsync(CancellationToken ct)
        {
            try
            {
                int readyResult = await EnterOrTransitionPickerPhaseAsync(
                    PickerProcessPhase.PickUp,
                    "InputCameraMarkInspection",
                    ct).ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;

                InputCameraPreInspectionWaitResult waitResult =
                    await InputCameraPreInspectionCoordinator.WaitForPermissionOrCompletionAsync(
                        Context,
                        Side,
                        BuildChildSequenceOptions(),
                        ct,
                        Name + ":PickUpReady").ConfigureAwait(false);

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
                    _pickUpSequence = null;
                    _resumePartialPickUpWithoutMarkPermission = false;
                    // PickUp 완료 시 PickerZ가 Stage Safe 높이를 통과했으므로 다음 Bottom 진입에서 1회 사용한다.
                    _pickerZStageSafeConfirmedByPickUp = true;
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
                    Name + ":PickUpCompleteToBottom");

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

        private bool CanStartSafeInputCameraPreInspection(PickerSequenceSide targetSide, string reason, out string blockReason)
        {
            blockReason = string.Empty;

            try
            {
                if (Context == null || Context.Machine == null)
                {
                    blockReason = "Machine context 없음";
                    return false;
                }

                string pendingPermissionDetail;
                if (InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail))
                {
                    blockReason = "PickUp 허가가 이미 발급되어 InputVisionX Avoid 유지 필요. pendingPermission=" +
                        pendingPermissionDetail;
                    return false;
                }

                string frontDetail;
                bool frontBlocking = IsPickerBlockingInputCameraPreInspection(true, out frontDetail);
                string rearDetail;
                bool rearBlocking = IsPickerBlockingInputCameraPreInspection(false, out rearDetail);

                string railContentionDetail;
                bool exactTargetEvaluated;
                bool railContended = IsSharedRailContendedForInputVisionStart(
                    out railContentionDetail,
                    out exactTargetEvaluated);
                if (railContended)
                {
                    blockReason = "InputVisionX가 가장 가까운 검사 대기 다이까지 이동할 때 공용 레일 또는 모션 가드가 차단됩니다. " +
                        railContentionDetail;
                    return false;
                }

                if ((frontBlocking || rearBlocking) && !exactTargetEvaluated)
                {
                    blockReason = "InputCamera 선행검사 시작 전 Picker Input 영역이 안전하게 비어있고, 실제 검사 다이 좌표까지 안전한지 확인되어야 합니다. " +
                        "frontBlocking=" + frontBlocking +
                        ", frontDetail=" + frontDetail +
                        ", rearBlocking=" + rearBlocking +
                        ", rearDetail=" + rearDetail +
                        ", targetDetail=" + railContentionDetail;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                blockReason = "PickerProcess 선행검사 시작 조건 확인 중 예외. targetSide=" + targetSide +
                    ", reason=" + reason +
                    ", error=" + ex.Message;
                return false;
            }
        }

        private bool IsPickerBlockingInputCameraPreInspection(bool isFront, out string detail)
        {
            detail = string.Empty;

            try
            {
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    Context != null ? Context.Machine : null,
                    isFront,
                    PickerWorkZone.Input,
                    null,
                    "PickerProcess InputCamera 선행검사 시작 판단");

                bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
                bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
                bool inputRelated =
                    state != null &&
                    (state.CurrentZone == PickerWorkZone.Input ||
                     state.TargetZone == PickerWorkZone.Input ||
                     state.BlocksTransport ||
                     state.UnknownUnsafe);
                bool movingInputRisk = IsPickerInputZoneMotionRiskForProcess(state, xMoving, yMoving);
                bool busy = inputRelated || movingInputRisk;

                detail = (isFront ? "FrontPicker" : "RearPicker") +
                    ", busy=" + busy +
                    ", movingX=" + xMoving +
                    ", movingY=" + yMoving +
                    ", movingInputRisk=" + movingInputRisk +
                    ", " + (state != null ? state.Describe() : "state=null");

                return busy;
            }
            catch (Exception ex)
            {
                detail = (isFront ? "FrontPicker" : "RearPicker") +
                    " InputCamera 선행검사 시작 조건 확인 실패. error=" + ex.Message;
                return true;
            }
        }

        // FIX-D: InputVisionX가 작업(die)까지 전진할 때 공용 레일에서 상대 PickerX 실좌표와 겹치는지 dry-run으로 확인한다.
        // 실제 이동 인터락과 동일한 SharedRailX 충돌 판정을 사용하므로 zone 논리가 놓치는 물리 경합(예: Bottom 중 x=445.999)을 잡는다.
        private bool IsSharedRailContendedForInputVisionStart(out string detail, out bool exactTargetEvaluated)
        {
            detail = string.Empty;
            exactTargetEvaluated = false;

            try
            {
                CDT320_Machine machine = Context != null ? Context.Machine : null;
                if (machine == null)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(machine);
                if (service == null)
                {
                    detail = "SharedRailX 서비스가 없어 실제 검사 다이 좌표 안전 확인을 수행할 수 없습니다.";
                    return false;
                }

                BaseAxis inputVisionX = machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
                if (inputVisionX == null || !service.IsSharedRailAxis(inputVisionX))
                {
                    detail = "InputVisionX가 없거나 SharedRailX 축으로 등록되지 않아 실제 검사 다이 좌표 안전 확인을 수행할 수 없습니다.";
                    return false;
                }

                List<InputStagePickTargetCandidate> candidates =
                    MaterialStateService.GetReadyInputStagePickTargetCandidates();
                if (candidates == null || candidates.Count == 0)
                {
                    detail = "검사 대기 다이가 없습니다.";
                    return false;
                }

                BaseAxis frontPickerX = machine.PickerFrontUnit != null
                    ? machine.PickerFrontUnit.PickerX
                    : null;
                BaseAxis rearPickerX = machine.PickerRearUnit != null
                    ? machine.PickerRearUnit.PickerX
                    : null;

                InputStagePickTargetCandidate nearestCandidate = null;
                string nearestPicker = string.Empty;
                double nearestPickerReference = 0.0;
                double nearestDistance = double.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    InputStagePickTargetCandidate candidate = candidates[i];
                    if (candidate == null)
                        continue;

                    UpdateNearestInputVisionCandidate(
                        candidate,
                        frontPickerX,
                        "FrontPickerX",
                        ref nearestCandidate,
                        ref nearestPicker,
                        ref nearestPickerReference,
                        ref nearestDistance);
                    UpdateNearestInputVisionCandidate(
                        candidate,
                        rearPickerX,
                        "RearPickerX",
                        ref nearestCandidate,
                        ref nearestPicker,
                        ref nearestPickerReference,
                        ref nearestDistance);
                }

                if (nearestCandidate == null)
                {
                    detail = "PickerX와 비교할 수 있는 검사 대기 다이를 찾지 못했습니다.";
                    return false;
                }

                double probeTarget = nearestCandidate.TargetX;
                exactTargetEvaluated = true;

                string reason;
                if (!service.VerifySingleAxisMove(inputVisionX, probeTarget, out reason))
                {
                    detail = BuildNearestInputVisionTargetDetail(
                        nearestCandidate,
                        nearestPicker,
                        nearestPickerReference,
                        nearestDistance,
                        probeTarget,
                        inputVisionX,
                        "SharedRailX: " + reason);
                    return true;
                }

                string motionGuardReason;
                if (!MotionGuardRuntime.CanAxisTeachingMove(
                    inputVisionX,
                    probeTarget,
                    "InputCameraPreInspectionNearestDie;Die=" + nearestCandidate.DieId,
                    out motionGuardReason))
                {
                    detail = BuildNearestInputVisionTargetDetail(
                        nearestCandidate,
                        nearestPicker,
                        nearestPickerReference,
                        nearestDistance,
                        probeTarget,
                        inputVisionX,
                        "MotionGuard: " + motionGuardReason);
                    return true;
                }

                detail = BuildNearestInputVisionTargetDetail(
                    nearestCandidate,
                    nearestPicker,
                    nearestPickerReference,
                    nearestDistance,
                    probeTarget,
                    inputVisionX,
                    "Clear");
                return false;
            }
            catch (Exception ex)
            {
                detail = "공용 레일 경합 확인 중 예외. error=" + ex.Message;
                return true;
            }
        }

        private static string BuildNearestInputVisionTargetDetail(
            InputStagePickTargetCandidate nearestCandidate,
            string nearestPicker,
            double nearestPickerReference,
            double nearestDistance,
            double probeTarget,
            BaseAxis inputVisionX,
            string result)
        {
            return "die=" + (nearestCandidate != null ? nearestCandidate.DieId : "-") +
                    ", grid=(" + nearestCandidate.DieMapX + "," + nearestCandidate.DieMapY + ")" +
                    ", nearestPicker=" + nearestPicker +
                    ", pickerReferenceX=" + nearestPickerReference.ToString("F3") +
                    ", distance=" + nearestDistance.ToString("F3") +
                    ", probeTarget=" + probeTarget.ToString("F3") +
                    ", inputVisionXActual=" + (inputVisionX != null ? inputVisionX.ActualPosition.ToString("F3") : "-") +
                    ", result=" + result;
        }

        private static bool IsPickerInputZoneMotionRiskForProcess(PickerZoneTransportState state, bool xMoving, bool yMoving)
        {
            if (!xMoving && !yMoving)
                return false;

            if (state == null)
                return true;

            return state.CurrentZone == PickerWorkZone.Input ||
                   state.TargetZone == PickerWorkZone.Input ||
                   state.CurrentZone == PickerWorkZone.Unknown ||
                   state.TargetZone == PickerWorkZone.Unknown ||
                   state.UnknownUnsafe;
        }

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
                    _bottomAndSideInspectionSequence = null;
                    _bottomInspectionCompletedInCurrentRun = false;
                    _forceBottomInspectionBeforeSideResume = false;

                    int nextPhaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.Place, "BottomAndSideInspectionToPlace", ct).ConfigureAwait(false);
                    if (nextPhaseResult != 0)
                        return nextPhaseResult;

                    CurrentStep = PickerProcessStep.RunPlace;
                    EnableContinuousPlaceEntryFromInspection("BottomAndSideInspectionToPlace");
                    EnableSafePlaceEntryIfResumeDrain("BottomAndSideInspectionToPlace");
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

        private static void UpdateNearestInputVisionCandidate(
            InputStagePickTargetCandidate candidate,
            BaseAxis pickerX,
            string pickerName,
            ref InputStagePickTargetCandidate nearestCandidate,
            ref string nearestPicker,
            ref double nearestPickerReference,
            ref double nearestDistance)
        {
            if (candidate == null || pickerX == null)
                return;

            double pickerReference = pickerX.IsMoving
                ? pickerX.CommandPosition
                : pickerX.ActualPosition;
            double distance = Math.Abs(candidate.TargetX - pickerReference);
            if (distance >= nearestDistance)
                return;

            nearestCandidate = candidate;
            nearestPicker = pickerName;
            nearestPickerReference = pickerReference;
            nearestDistance = distance;
        }

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

                    // 현재 기준: CycleStop 후에는 새 작업 zone 진입 대기를 drain하지 않고 정지 경계로 종료한다.
                    Context.StopIfCycleStopRequested(
                        Name + ".WaitOppositePickerPhysicalClear:" + normalizedRequestedZone);

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
    }
}

