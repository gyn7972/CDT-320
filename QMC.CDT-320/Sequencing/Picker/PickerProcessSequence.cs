using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerProcessSequence : PickerSequenceBase<PickerProcessStep>
    {
        private PickerPickUpSequence _pickUpSequence;
        private PickerBottomInspectionSequence _bottomInspectionSequence;
        private PickerSideInspectionSequence _sideInspectionSequence;
        private PickerBottomSideInspectionSequence _bottomSideInspectionSequence;
        private PickerPlaceSequence _placeSequence;
        private PickerPhaseLease _phaseLease;
        private bool _bottomInspectionCompletedInCurrentRun;

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
                if (_bottomSideInspectionSequence != null)
                    _bottomSideInspectionSequence.Abort();
                if (_placeSequence != null)
                    _placeSequence.Abort();

                _pickUpSequence = null;
                _bottomInspectionSequence = null;
                _sideInspectionSequence = null;
                _bottomSideInspectionSequence = null;
                _placeSequence = null;
                _bottomInspectionCompletedInCurrentRun = false;
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
                if (IsStepRunMode())
                {
                    return await ExecuteSingleProcessStepAsync(ct).ConfigureAwait(false);
                }

                return await ExecuteProcessUntilCompleteAsync(ct).ConfigureAwait(false);
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
                    return RunInputCameraMarkInspectionAsync(ct);

                // 픽업 실행
                case PickerProcessStep.RunPickUp:
                    return RunPickUpAsync(ct);

                // 하단 검사 실행
                case PickerProcessStep.RunBottomInspection:
                    return RunBottomInspectionAsync(ct);

                // 사이드 검사 실행
                case PickerProcessStep.RunSideInspection:
                    return RunSideInspectionAsync(ct);

                // 플레이스 실행
                case PickerProcessStep.RunPlace:
                    return RunPlaceAsync(ct);

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

                if (bottomRequiredCount > 0)
                {
                    CurrentStep = PickerProcessStep.RunBottomInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker가 Die를 가지고 있어 PickUp을 건너뛰고 Bottom 검사부터 재개합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", bottomRequiredCount=" + bottomRequiredCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount + " - Check");
                    return 0;
                }

                if (sideRequiredCount > 0)
                {
                    CurrentStep = PickerProcessStep.RunSideInspection;
                    WriteLog("PickerProcessSequence",
                        Name + " Bottom 검사가 완료된 Die가 있어 Side 검사부터 재개합니다. side=" + Side +
                        ", occupiedPickerCount=" + occupiedCount +
                        ", sideRequiredCount=" + sideRequiredCount +
                        ", placeReadyCount=" + placeReadyCount + " - Check");
                    return 0;
                }

                CurrentStep = PickerProcessStep.RunPlace;
                WriteLog("PickerProcessSequence",
                    Name + " Picker Die 검사 상태가 Place 가능 상태라 Place부터 재개합니다. side=" + Side +
                    ", occupiedPickerCount=" + occupiedCount +
                    ", placeReadyCount=" + placeReadyCount + " - Check");
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

                int result = await _pickUpSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("PickUpFailed");
                    return result;
                }

                if (_pickUpSequence.IsComplete)
                {
                    _pickUpSequence = null;
                    StartNextInputCameraPreInspectionIfNeeded(ct, "PickUpComplete");
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

        private void StartNextInputCameraPreInspectionIfNeeded(CancellationToken ct, string reason)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return;

                InputCameraPreInspectionCoordinator.EnsureStarted(
                    Context,
                    Side,
                    BuildChildSequenceOptions(),
                    ct,
                    Name + ":" + reason);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " InputCamera 선행검사 시작 요청 중 예외가 발생했습니다. side=" + Side +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        private async Task<int> RunBottomInspectionAsync(CancellationToken ct)
        {
            if ((Options == null || Options.RunMode == SequenceRunMode.Auto) &&
                IsBottomSidePipelineModeEnabled())
            {
                return await RunBottomSideInspectionAsync(ct).ConfigureAwait(false);
            }

            bool keepPhaseSignal = false;

            try
            {
                int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.BottomInspection, "BottomInspection", ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                SetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "Bottom");

                if (_bottomInspectionSequence == null || _bottomInspectionSequence.IsComplete)
                    _bottomInspectionSequence = new PickerBottomInspectionSequence(Context, Side);

                int result = await _bottomInspectionSequence
                    .RunAsync(ct, BuildChildSequenceOptions(
                        Options == null || Options.RunMode == SequenceRunMode.Auto,
                        false,
                        false))
                    .ConfigureAwait(false);

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

        private bool IsBottomSidePipelineModeEnabled()
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                return vision != null &&
                       vision.Config != null &&
                       vision.Config.PickerInspectionMode == PickerInspectionPipelineMode.BottomSidePipeline;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> RunBottomSideInspectionAsync(CancellationToken ct)
        {
            bool keepBottomSignal = false;
            bool keepSideSignal = false;

            try
            {
                int phaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.BottomInspection, "BottomSideInspection", ct).ConfigureAwait(false);
                if (phaseResult != 0)
                    return phaseResult;

                SetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "BottomSide-Bottom");
                SetPickerPhaseSignal(GetOwnSideInspectionSignal(), "BottomSide-Side");

                if (_bottomSideInspectionSequence == null || _bottomSideInspectionSequence.IsComplete)
                    _bottomSideInspectionSequence = new PickerBottomSideInspectionSequence(Context, Side);

                int result = await _bottomSideInspectionSequence
                    .RunAsync(ct, BuildChildSequenceOptions(
                        true,
                        true,
                        true))
                    .ConfigureAwait(false);

                if (result != 0)
                {
                    ReleasePickerProcessPhase("BottomSideInspectionFailed");
                    return result;
                }

                if (_bottomSideInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnBottomInspectionCompleteSignal(), "BottomComplete");
                    SetPickerPhaseSignal(GetOwnSideInspectionCompleteSignal(), "SideComplete");
                    _bottomSideInspectionSequence = null;
                    _bottomInspectionCompletedInCurrentRun = false;

                    int nextPhaseResult = await EnterOrTransitionPickerPhaseAsync(PickerProcessPhase.Place, "BottomSideInspectionToPlace", ct).ConfigureAwait(false);
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
                    ResetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "BottomSide-Bottom");
                if (!keepSideSignal)
                    ResetPickerPhaseSignal(GetOwnSideInspectionSignal(), "BottomSide-Side");
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

                int result = await _sideInspectionSequence
                    .RunAsync(ct, BuildChildSequenceOptions(
                        false,
                        _bottomInspectionCompletedInCurrentRun,
                        Options == null || Options.RunMode == SequenceRunMode.Auto))
                    .ConfigureAwait(false);

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
                }

                int result = await _placeSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

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

