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
        private bool _bottomInspectionCompletedInCurrentRun;
        private bool _forceBottomInspectionBeforeSideResume;
        private bool _forceSafeYBeforePlaceResume;

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
                InputCameraPreInspectionCoordinator.Clear(Side);

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
                using (SequenceResourceLease pickerLease = await AcquireResourceAsync(PickerResourceKind, Name + ":Process", ct).ConfigureAwait(false))
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
            }
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

                int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
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

            // INV-7: 시작/재개 첫 이동 전, 양 픽커가 안전 배치(양쪽 Avoid = Y·Z Avoid, X 비대면)인지 확인만 한다(이동 없음).
            // 정상 시작에서는 상부축이 항상 Avoid이므로 Ok가 기대된다. 어긋나면 재개 첫 이동 전 정규화가 필요함을 로그로 남긴다.
            string startSafeDetail;
            if (VerifySafeStartConfig(out startSafeDetail))
            {
                WriteLog("PickerProcessSequence",
                    Name + " 시작/재개 안전 배치 확인 완료. 양 픽커가 Avoid 위치입니다. side=" + Side + " - Ok");
            }
            else
            {
                WriteLog("PickerProcessSequence",
                    Name + " 시작/재개 안전 배치 확인: 양 픽커가 Avoid 위치가 아닙니다. 재개 첫 전진 전 Avoid 정규화가 필요합니다. " +
                    "side=" + Side + ", detail=" + startSafeDetail + " - Check");
            }

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
                WriteLog("PickerProcessSequence",
                    Name + " Picker 공정 시작 스텝 판단. side=" + Side +
                    ", runMode=" + (Options != null ? Options.RunMode.ToString() : "null") +
                    ", enabledPickerCount=" + enabled.Count +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", bottomRequiredCount=" + bottomRequiredCount +
                    ", sideRequiredCount=" + sideRequiredCount +
                    ", placeReadyCount=" + placeReadyCount +
                    ", hasReadyInputPickTarget=" + hasReadyInputPickTarget +
                    " - Check");

                if (occupiedCount == 0)
                {
                    CurrentStep = PickerProcessStep.RunInputCameraMarkInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker에 Die가 없어 InputCamera Mark 검사부터 시작합니다. side=" + Side +
                        ", enabledPickerCount=" + enabled.Count + " - Check");
                    return 0;
                }

                _forceBottomInspectionBeforeSideResume = false;
                bool forceBottomAndSideFromFirst =
                    (Options == null || Options.RunMode == SequenceRunMode.Auto) &&
                    IsBottomAndSidePipelineModeEnabled() &&
                    (bottomRequiredCount > 0 || sideRequiredCount > 0);

                if (bottomRequiredCount > 0)
                {
                    _forceBottomInspectionBeforeSideResume = forceBottomAndSideFromFirst;
                    CurrentStep = PickerProcessStep.RunBottomInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker가 Die를 가지고 있어 PickUp을 건너뛰고 Bottom 검사부터 재개합니다. " +
                        "Bottom/Side 파이프라인 모드에서는 중간 정지 후 안전을 위해 1번부터 Bottom/Side를 다시 진행합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", bottomRequiredCount=" + bottomRequiredCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount +
                        ", forceBottomAndSideFromFirst=" + forceBottomAndSideFromFirst + " - Check");
                    return 0;
                }

                if (sideRequiredCount > 0)
                {
                    if (forceBottomAndSideFromFirst)
                    {
                        _forceBottomInspectionBeforeSideResume = true;
                        CurrentStep = PickerProcessStep.RunBottomInspection;
                        WriteLog("PickerProcessSequence",
                            Name + " Side 검사만 남은 재시작 상태라 Bottom/Side 통합 검사를 Bottom부터 다시 시작합니다. " +
                            "상대 Picker Place 진입과 Side 진입이 겹치지 않도록 Side 단독 재개를 사용하지 않습니다. side=" + Side +
                            ", occupiedPickerCount=" + occupiedCount +
                            ", sideRequiredCount=" + sideRequiredCount +
                            ", placeReadyCount=" + placeReadyCount + " - Check");
                        return 0;
                    }

                    CurrentStep = PickerProcessStep.RunSideInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Bottom 검사가 완료된 Die가 있어 Side 검사부터 재개합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount + " - Check");
                    return 0;
                }

                CurrentStep = PickerProcessStep.RunPlace;
                _forceSafeYBeforePlaceResume = true;
                WriteLog("PickerProcessSequence",
                    Name + " Picker Die 검사 상태가 Place 가능 상태라 Place부터 재개합니다. " +
                    "Place 재개 시에는 모드와 관계없이 PickerY를 Avoid로 정리한 뒤 X/T Place 위치 이동 후 Y를 전진합니다. side=" + Side +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", placeReadyCount=" + placeReadyCount +
                    ", forceSafeYBeforePlaceResume=" + _forceSafeYBeforePlaceResume + " - Check");
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

                int result = await SequenceTrace.ChildAsync("PickerPickUpSequence", "PickUp",
                    () => _pickUpSequence.RunAsync(ct, BuildChildSequenceOptions()),
                    "side=" + Side).ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("PickUpFailed");
                    return result;
                }

                if (_pickUpSequence.IsComplete)
                {
                    _pickUpSequence = null;
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

        private void StartSafeInputCameraPreInspectionsAfterPickUpComplete(CancellationToken ct, string reason)
        {
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
                if (frontBlocking || rearBlocking)
                {
                    blockReason = "InputCamera 선행검사 시작 전 Picker Input 영역이 안전하게 비어있지 않습니다. " +
                        "frontBlocking=" + frontBlocking +
                        ", frontDetail=" + frontDetail +
                        ", rearBlocking=" + rearBlocking +
                        ", rearDetail=" + rearDetail;
                    return false;
                }

                // FIX-D: zone/workArea 논리뿐 아니라 실제 PickerX 좌표 기준으로 공용 레일 경합을 확인한다.
                // Rear가 Bottom workArea여도 물리 X가 InputVisionX 작업 전진 경로와 겹치면 시작을 보류한다.
                string railContentionDetail;
                if (IsSharedRailContendedForInputVisionStart(out railContentionDetail))
                {
                    blockReason = "InputVisionX 작업 전진 경로가 공용 레일에서 상대 PickerX 실좌표와 겹쳐 선행검사 시작을 보류합니다. " +
                        railContentionDetail;
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
        private bool IsSharedRailContendedForInputVisionStart(out string detail)
        {
            detail = string.Empty;

            try
            {
                CDT320_Machine machine = Context != null ? Context.Machine : null;
                if (machine == null)
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(machine);
                if (service == null)
                    return false;

                BaseAxis inputVisionX = machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
                if (inputVisionX == null || !service.IsSharedRailAxis(inputVisionX))
                    return false;

                // InputVisionX가 작업 범위 끝(soft limit)까지 전진 가능한지 상대 PickerX 현재 좌표 기준으로 검사한다.
                // 전진 여지가 없으면(전진 방향이 아니면) 판단을 보류하고 기존 zone 게이트에 위임한다.
                double probeTarget = inputVisionX.Setup != null ? inputVisionX.Setup.SoftLimitPlus : 0.0;
                if (probeTarget <= inputVisionX.ActualPosition)
                    return false;

                string reason;
                if (service.VerifySingleAxisMove(inputVisionX, probeTarget, out reason))
                    return false;

                detail = "probeTarget=" + probeTarget.ToString("F3") +
                    ", inputVisionXActual=" + inputVisionX.ActualPosition.ToString("F3") +
                    ", reason=" + reason;
                return true;
            }
            catch (Exception ex)
            {
                // 안전측: 확인 실패 시 시작을 보류한다. 본 공정은 FIX-A/B로 계속 진행된다.
                detail = "공용 레일 경합 확인 중 예외. error=" + ex.Message;
                return true;
            }
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
                return vision != null &&
                       vision.Config != null &&
                       vision.Config.PickerInspectionMode == PickerInspectionPipelineMode.BottomAndSidePipeline;
            }
            catch
            {
                return false;
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
                        Context.StopIfCycleStopRequested(Name + ".WaitOppositePendingSideBeforeBottom");

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
                    "DieBottomPosition[" + pickerIndex + "];PickerPhase=BottomEntry;OppositeSidePending",
                    bottomX,
                    null,
                    out encoderDetail);

                if (canEnter)
                    return false;

                detail = pendingDetail +
                         ", pickerIndex=" + pickerIndex +
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
                    "DieBottomPosition[" + pickerIndex + "];PickerPhase=BottomSideXWait;YHold").ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("BottomSideXWaitPrepare",
                    Name + " Bottom/Side 통합 검사 대기 위치 준비 완료. " +
                    "PickerY는 Avoid를 유지하고 PickerX만 Bottom 위치로 선행 이동했습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", pickerIndex=" + pickerIndex +
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
                    if (_forceSafeYBeforePlaceResume)
                    {
                        WriteLog("PickerProcessSequence",
                            Name + " Place 재시작 안전 진입 옵션을 전달했습니다. " +
                            "Place 가능 전에는 PickerY Avoid에서 대기하고, X/T 위치 이동 후 Y를 전진합니다. side=" + Side + " - Check");
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
                    WriteLog("PickerProcessSequence",
                        Name + " Place 완료 후 PickerProcessSequence가 다음 PickUp용 비침습 InputCamera 선행검사를 예약합니다. " +
                        "선행검사는 Picker 축을 직접 이동하지 않는 경로만 사용합니다. side=" +
                        Side + " - Check");
                    StartSafeInputCameraPreInspectionForSideIfNeeded(Side, ct, "PlaceComplete");
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
                Context.StopIfCycleStopRequested(Name + ".PickerPhaseEnter:" + requestedPhase);

                string reason;
                PickerPhaseLease lease;
                if (Context.PickerPhases.TryEnter(Side, requestedPhase, Name + ":" + description, out lease, out reason))
                {
                    _phaseLease = lease;
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
                Context.StopIfCycleStopRequested(Name + ".PickerPhaseTransition:" + requestedPhase);

                string reason;
                if (Context.PickerPhases.TryTransition(_phaseLease, requestedPhase, out reason))
                {
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

