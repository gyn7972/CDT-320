using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerProcessSequence : PickerSequenceBase<PickerProcessStep>
    {
        private enum OppositePickerMaterialPhase
        {
            Empty,
            NeedBottom,
            NeedSide,
            ReadyToPlace,
            Mixed,
            Unknown
        }

        private PickerPickUpSequence _pickUpSequence;
        private PickerBottomInspectionSequence _bottomInspectionSequence;
        private PickerSideInspectionSequence _sideInspectionSequence;
        private PickerPlaceSequence _placeSequence;

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

                if (_pickUpSequence != null)
                    _pickUpSequence.Abort();
                if (_bottomInspectionSequence != null)
                    _bottomInspectionSequence.Abort();
                if (_sideInspectionSequence != null)
                    _sideInspectionSequence.Abort();
                if (_placeSequence != null)
                    _placeSequence.Abort();

                _pickUpSequence = null;
                _bottomInspectionSequence = null;
                _sideInspectionSequence = null;
                _placeSequence = null;
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

                // 픽업 업 실행
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

                    placeReadyCount++;
                }

                if (occupiedCount == 0)
                {
                    CurrentStep = PickerProcessStep.RunPickUp;
                    WriteLog("PickerProcessSequence",
                        Name + " Picker에 Die가 없어 PickUp부터 시작합니다. side=" + Side +
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

        private async Task<int> RunPickUpAsync(CancellationToken ct)
        {
            try
            {
                if (_pickUpSequence == null || _pickUpSequence.IsComplete)
                {
                    int readyResult = await WaitForOppositePickerPhaseBeforePickUpAsync(ct).ConfigureAwait(false);
                    if (readyResult != 0)
                        return readyResult;

                    _pickUpSequence = new PickerPickUpSequence(Context, Side);
                }

                int result = await _pickUpSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (_pickUpSequence.IsComplete)
                {
                    _pickUpSequence = null;
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

        private async Task<int> RunBottomInspectionAsync(CancellationToken ct)
        {
            bool keepPhaseSignal = false;

            try
            {
                SetPickerPhaseSignal(GetOwnBottomInspectionSignal(), "Bottom");

                if (_bottomInspectionSequence == null || _bottomInspectionSequence.IsComplete)
                    _bottomInspectionSequence = new PickerBottomInspectionSequence(Context, Side);

                int result = await _bottomInspectionSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (_bottomInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnBottomInspectionCompleteSignal(), "BottomComplete");
                    _bottomInspectionSequence = null;
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

        private async Task<int> RunSideInspectionAsync(CancellationToken ct)
        {
            bool keepPhaseSignal = false;

            try
            {
                SetPickerPhaseSignal(GetOwnSideInspectionSignal(), "Side");

                if (_sideInspectionSequence == null || _sideInspectionSequence.IsComplete)
                    _sideInspectionSequence = new PickerSideInspectionSequence(Context, Side);

                int result = await _sideInspectionSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (_sideInspectionSequence.IsComplete)
                {
                    SetPickerPhaseSignal(GetOwnSideInspectionCompleteSignal(), "SideComplete");
                    _sideInspectionSequence = null;
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
                    int readyResult = await WaitForOppositePickerPhaseBeforePlaceAsync(ct).ConfigureAwait(false);
                    if (readyResult != 0)
                        return readyResult;

                    _placeSequence = new PickerPlaceSequence(Context, Side);
                }

                int result = await _placeSequence
                    .RunAsync(ct, BuildChildSequenceOptions())
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                if (_placeSequence.IsComplete)
                {
                    ResetPickerPhaseSignals();
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

        private async Task<int> WaitForOppositePickerPhaseBeforePickUpAsync(CancellationToken ct)
        {
            try
            {
                return await WaitForOppositePickerPhaseAsync(
                    GetOppositeSideInspectionSignal(),
                    GetOppositeSideInspectionCompleteSignal(),
                    "PickUp",
                    "상대 Picker가 Side 검사 단계가 아니어서 PickUp을 시작할 수 없습니다.",
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
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-PICKUP-GATE-EX", Name,
                    "PickUp 진입 조건 확인 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitForOppositePickerPhaseBeforePlaceAsync(CancellationToken ct)
        {
            try
            {
                return await WaitForOppositePickerPhaseAsync(
                    GetOppositeBottomInspectionSignal(),
                    GetOppositeBottomInspectionCompleteSignal(),
                    "Place",
                    "상대 Picker가 Bottom 검사 단계가 아니어서 Place를 시작할 수 없습니다.",
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
            catch (Exception ex)
            {
                return Fail("PICKER-PROCESS-PLACE-GATE-EX", Name,
                    "Place 진입 조건 확인 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitForOppositePickerPhaseAsync(
            string requiredSignal,
            string requiredCompletedSignal,
            string actionName,
            string blockedMessage,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string stateDetail;
                OppositePickerMaterialPhase materialPhase = ResolveOppositePickerMaterialPhase(out stateDetail);
                bool busReady = IsOppositePickerPhaseReady(requiredSignal, requiredCompletedSignal);
                bool materialReady = IsOppositeMaterialReadyForAction(actionName, materialPhase);

                if (materialPhase == OppositePickerMaterialPhase.Empty)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " " + actionName + " 진입 허용. 상대 Picker에 Die가 없습니다. side=" +
                        Side + ", " + BuildOppositePickerWorkState() + " - Check");
                    return 0;
                }

                if (!IsOppositePickerSideEnabled())
                {
                    return Fail("PICKER-PROCESS-OPPOSITE-DISABLED", Name,
                        blockedMessage + " 상대 Picker가 Die를 가지고 있지만 사용 안 함 상태입니다. side=" +
                        Side + ", opposite=" + GetOppositeSideName());
                }

                if (busReady || materialReady)
                {
                    WriteLog("PickerProcessSequence",
                        Name + " " + actionName + " 진입 허용. 상대 Picker 상태 확인. " +
                        "busReady=" + busReady +
                        ", materialReady=" + materialReady +
                        ", materialPhase=" + materialPhase +
                        ", " + stateDetail +
                        ", " + BuildOppositePickerWorkState() +
                        ", requiredSignal=" + requiredSignal +
                        ", requiredCompleteSignal=" + requiredCompletedSignal + " - Check");
                    return 0;
                }

                if (IsStepRunMode())
                {
                    return Fail("PICKER-PROCESS-PHASE-BLOCK", Name,
                        blockedMessage + " 수동/Step 모드에서는 대기하지 않습니다. " +
                        "상대 Picker 상태를 확인한 뒤 다시 실행하세요. side=" + Side +
                        ", materialPhase=" + materialPhase +
                        ", " + stateDetail +
                        ", " + BuildOppositePickerWorkState() +
                        ", requiredSignal=" + requiredSignal +
                        ", requiredCompleteSignal=" + requiredCompletedSignal);
                }

                WriteLog("PickerProcessSequence",
                    Name + " " + actionName + " 진입 대기. 상대 Picker의 Bus/Material/작업영역 상태를 기다립니다. " +
                    "side=" + Side +
                    ", materialPhase=" + materialPhase +
                    ", " + stateDetail +
                    ", " + BuildOppositePickerWorkState() +
                    ", requiredSignal=" + requiredSignal +
                    ", requiredCompleteSignal=" + requiredCompletedSignal + " - Wait");

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + "." + actionName + ".WaitOppositePickerPhase");

                    materialPhase = ResolveOppositePickerMaterialPhase(out stateDetail);
                    if (materialPhase == OppositePickerMaterialPhase.Empty)
                        break;

                    busReady = IsOppositePickerPhaseReady(requiredSignal, requiredCompletedSignal);
                    materialReady = IsOppositeMaterialReadyForAction(actionName, materialPhase);
                    if (busReady || materialReady)
                        break;

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
                WriteLog("PickerProcessSequence",
                    Name + " " + actionName + " 진입 허용. 상대 Picker 진입 조건 충족. " +
                    "side=" + Side +
                    ", materialPhase=" + materialPhase +
                    ", " + stateDetail +
                    ", " + BuildOppositePickerWorkState() +
                    ", requiredSignal=" + requiredSignal +
                    ", requiredCompleteSignal=" + requiredCompletedSignal + " - Ok");
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
                return Fail("PICKER-PROCESS-PHASE-GATE-EX", Name,
                    actionName + " 진입 대기 중 예외가 발생했습니다. side=" + Side +
                    ", requiredSignal=" + requiredSignal +
                    ", requiredCompleteSignal=" + requiredCompletedSignal +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private OppositePickerMaterialPhase ResolveOppositePickerMaterialPhase(out string detail)
        {
            detail = string.Empty;

            try
            {
                MaterialLocationKind location = Side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerRear
                    : MaterialLocationKind.PickerFront;

                int occupiedCount = 0;
                int needBottomCount = 0;
                int needSideCount = 0;
                int readyToPlaceCount = 0;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                    if (die == null)
                        continue;

                    occupiedCount++;

                    bool bottomDone = HasInspectionResult(die, "Bottom");
                    bool side0Done = HasInspectionResult(die, "Side0");
                    bool side90Done = HasInspectionResult(die, "Side90");

                    if (!bottomDone)
                    {
                        needBottomCount++;
                        continue;
                    }

                    if (!side0Done || !side90Done)
                    {
                        needSideCount++;
                        continue;
                    }

                    readyToPlaceCount++;
                }

                detail =
                    "opposite=" + GetOppositeSideName() +
                    ", occupied=" + occupiedCount +
                    ", needBottom=" + needBottomCount +
                    ", needSide=" + needSideCount +
                    ", readyToPlace=" + readyToPlaceCount;

                if (occupiedCount == 0)
                    return OppositePickerMaterialPhase.Empty;

                int activePhaseCount = 0;
                if (needBottomCount > 0)
                    activePhaseCount++;
                if (needSideCount > 0)
                    activePhaseCount++;
                if (readyToPlaceCount > 0)
                    activePhaseCount++;

                if (activePhaseCount > 1)
                    return OppositePickerMaterialPhase.Mixed;

                if (needBottomCount > 0)
                    return OppositePickerMaterialPhase.NeedBottom;

                if (needSideCount > 0)
                    return OppositePickerMaterialPhase.NeedSide;

                if (readyToPlaceCount > 0)
                    return OppositePickerMaterialPhase.ReadyToPlace;

                return OppositePickerMaterialPhase.Unknown;
            }
            catch (Exception ex)
            {
                detail = "opposite=" + GetOppositeSideName() + ", error=" + ex.Message;
                WriteLog("PickerProcessSequence",
                    Name + " 상대 Picker Material 단계 확인 실패. side=" + Side +
                    ", " + detail + " - Failed");
                return OppositePickerMaterialPhase.Unknown;
            }
            finally
            {
            }
        }

        private bool IsOppositeMaterialReadyForAction(string actionName, OppositePickerMaterialPhase phase)
        {
            try
            {
                if (phase == OppositePickerMaterialPhase.Empty)
                    return true;

                if (phase == OppositePickerMaterialPhase.NeedSide ||
                    phase == OppositePickerMaterialPhase.ReadyToPlace)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence",
                    Name + " 상대 Picker Material 진입 허용 판단 실패. action=" + actionName +
                    ", phase=" + phase +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private string BuildOppositePickerWorkState()
        {
            try
            {
                PickerWorkZone workZone;
                string owner;
                bool active = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    Side != PickerSequenceSide.Front,
                    out workZone,
                    out owner);

                string resourceState = string.Empty;
                if (Context != null && Context.Resources != null)
                {
                    resourceState =
                        ", resources=inputStage[" + Context.Resources.GetHolder(SequenceResourceKind.InputStageArea) + "]" +
                        ", inspection[" + Context.Resources.GetHolder(SequenceResourceKind.InspectionArea) + "]" +
                        ", outputPlace[" + Context.Resources.GetHolder(SequenceResourceKind.OutputPlaceArea) + "]";
                }

                return "oppositeWorkActive=" + active +
                       ", oppositeWorkZone=" + workZone +
                       ", oppositeWorkOwner=" + (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                       resourceState;
            }
            catch (Exception ex)
            {
                return "oppositeWorkStateError=" + ex.Message;
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

        private PickerSequenceOptions BuildChildSequenceOptions()
        {
            PickerSequenceOptions source = Options ?? PickerSequenceOptions.Default();
            return new PickerSequenceOptions
            {
                RunMode = source.RunMode,
                StartMode = source.StartMode,
                FineMove = source.FineMove,
                MoveTimeoutMs = source.MoveTimeoutMs,
                ResourceTimeoutMs = source.ResourceTimeoutMs,
                PickerNo = source.PickerNo,
                VisionRetryCount = source.VisionRetryCount,
                SimulateVisionResult = source.SimulateVisionResult
            };
        }
    }
}

