using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputDieVisionPrepareSequence : PickerSequenceBase<InputDieVisionPrepareStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const int VisionInspectionSettleDelayMs = 100;

        private readonly List<int> _enabledPickerIndexes;
        private readonly List<InputDieVisionPreparedItem> _preparedItems = new List<InputDieVisionPreparedItem>();
        private readonly HashSet<int> _preInspectionOccupiedPickerNos = new HashSet<int>();
        private int _inspectionCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private string _currentDieId = "";
        private InputStagePickTarget _pickTarget;
        private VisionAlignResult _visionOffset;
        private InputDieVisionPreparedItem _currentItem;
        private bool _completedSuccessfully;

        public InputDieVisionPrepareSequence(
            MachineSequenceContext context,
            PickerSequenceSide side,
            IEnumerable<int> enabledPickerIndexes)
            : base(context, side, PickerSequenceKind.PickUp, side == PickerSequenceSide.Front ? "FrontInputDieVisionPrepareSequence" : "RearInputDieVisionPrepareSequence")
        {
            _enabledPickerIndexes = enabledPickerIndexes != null
                ? new List<int>(enabledPickerIndexes)
                : new List<int>();
            CurrentStep = InputDieVisionPrepareStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == InputDieVisionPrepareStep.Complete; }
        }

        public IList<InputDieVisionPreparedItem> PreparedItems
        {
            get { return _preparedItems.AsReadOnly(); }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                while (CurrentStep != InputDieVisionPrepareStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();
                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                _completedSuccessfully = true;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-EX", Name,
                    "Input die vision 준비 중 예외가 발생했습니다. step=" + CurrentStep +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!_completedSuccessfully)
                    ReleasePreparedReservationsIfNeeded();
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                case InputDieVisionPrepareStep.CheckUnit:
                    return Task.FromResult(CheckUnit());
                case InputDieVisionPrepareStep.BuildPickBatch:
                    return Task.FromResult(BuildPickBatch());
                case InputDieVisionPrepareStep.SelectNextInspectionTarget:
                    return Task.FromResult(SelectNextInspectionTarget());
                case InputDieVisionPrepareStep.VerifyReservedInputDie:
                    return Task.FromResult(VerifyReservedInputDie());
                case InputDieVisionPrepareStep.MovePickersToAvoidForInputVisionMove:
                    return MovePickersToAvoidForInputVisionMoveAsync(ct);
                case InputDieVisionPrepareStep.MoveInputStageAndVisionToDie:
                    return MoveInputStageAndVisionToDieAsync(ct);
                case InputDieVisionPrepareStep.RequestInputDieVisionInspection:
                    return RequestInputDieVisionInspectionAsync(ct);
                case InputDieVisionPrepareStep.ApplyInputDieVisionOffset:
                    return Task.FromResult(ApplyInputDieVisionOffset());
                default:
                    return Task.FromResult(Fail("INPUT-DIE-VISION-PREPARE-STEP", Name,
                        "지원하지 않는 Input die vision 준비 단계입니다. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (!IsPickerSideEnabled())
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Picker 사용 설정이 OFF라서 Input die vision 준비를 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = InputDieVisionPrepareStep.Complete;
                    return 0;
                }

                if (ResolveInputStage() == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit",
                        "InputStageUnit이 없어 Input die vision 준비를 진행할 수 없습니다.");

                if (_enabledPickerIndexes.Count == 0)
                    return Fail("INPUT-DIE-VISION-PREPARE-NO-PICKER", Name,
                        "Input die vision 준비 대상 Picker가 없습니다. side=" + Side);

                CurrentStep = InputDieVisionPrepareStep.BuildPickBatch;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-CHECK-EX", Name,
                    "Input die vision 준비 조건 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int BuildPickBatch()
        {
            try
            {
                _preparedItems.Clear();
                _preInspectionOccupiedPickerNos.Clear();
                _inspectionCursor = 0;
                ClearCurrentContext();

                int occupiedPickerCount = 0;
                for (int i = 0; i < _enabledPickerIndexes.Count; i++)
                {
                    int pickerIndex = _enabledPickerIndexes[i];
                    int pickerNo = ToPickerNo(pickerIndex);

                    DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (loadedDie != null && IsInputCameraPreInspectionMode())
                    {
                        _preInspectionOccupiedPickerNos.Add(pickerNo);
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputCamera 선행검사 모드: Picker가 Die를 들고 있지만 다음 PickUp 대상 예약을 허용합니다. " +
                            "pickerNo=" + pickerNo +
                            ", pickerIndex=" + pickerIndex +
                            ", loadedDie=" + loadedDie.DieId + " - Check");
                        loadedDie = null;
                    }
                    if (loadedDie != null)
                    {
                        occupiedPickerCount++;
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " Picker가 이미 Die를 가지고 있어 Input die vision 예약에서 제외합니다. " +
                            "pickerNo=" + pickerNo +
                            ", pickerIndex=" + pickerIndex +
                            ", loadedDie=" + loadedDie.DieId + " - Check");
                        continue;
                    }

                    InputStagePickTarget target = MaterialStateService.ReserveNextInputStagePickTarget(PickerLocationKind, pickerNo);
                    string dieId = target != null ? target.DieId : "";
                    if (string.IsNullOrWhiteSpace(dieId))
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " Input die vision 준비 대상이 더 없습니다. pickerNo=" + pickerNo + " - Check");
                        continue;
                    }

                    _preparedItems.Add(new InputDieVisionPreparedItem
                    {
                        PickerIndex = pickerIndex,
                        PickerNo = pickerNo,
                        DieId = dieId,
                        PickTarget = target
                    });

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Input die vision 준비용 Die를 예약했습니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", pickerIndex=" + pickerIndex +
                        ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                        ", inputVisionX=" + target.TargetX +
                        ", inputStageY=" + target.TargetY + " - Ok");
                }

                if (_preparedItems.Count == 0)
                {
                    if (occupiedPickerCount > 0 && occupiedPickerCount >= _enabledPickerIndexes.Count)
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-PICKER-OCCUPIED", "Material",
                            "사용 설정된 모든 Picker가 이미 Die를 가지고 있어 Input die vision 준비를 시작할 수 없습니다. " +
                            "먼저 검사/Place/Recover를 진행해 Picker를 비운 뒤 다시 시작하세요. " +
                            "occupiedPickerCount=" + occupiedPickerCount +
                            ", enabledPickerCount=" + _enabledPickerIndexes.Count +
                            ", side=" + Side);
                    }

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Input die vision 준비 대상이 없습니다. side=" + Side + " - Check");
                    CurrentStep = InputDieVisionPrepareStep.Complete;
                    return 0;
                }

                WriteLog("InputDieVisionPrepareSequence",
                    Name + " Input die vision 준비 배치를 생성했습니다. count=" + _preparedItems.Count +
                    ", enabledPickerCount=" + _enabledPickerIndexes.Count + " - Ok");

                CurrentStep = InputDieVisionPrepareStep.SelectNextInspectionTarget;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-BATCH-EX", Name,
                    "Input die vision 준비 배치 생성 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int SelectNextInspectionTarget()
        {
            if (_inspectionCursor >= _preparedItems.Count)
            {
                WriteLog("InputDieVisionPrepareSequence",
                    Name + " Input die vision 준비가 완료되었습니다. count=" + _preparedItems.Count + " - Ok");
                CurrentStep = InputDieVisionPrepareStep.Complete;
                return 0;
            }

            SetCurrentItem(_preparedItems[_inspectionCursor]);
            WriteLog("InputDieVisionPrepareSequence",
                Name + " Input die vision 검사 대상을 선택했습니다. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", inspectIndex=" + (_inspectionCursor + 1) +
                "/" + _preparedItems.Count + " - Ok");

            CurrentStep = InputDieVisionPrepareStep.VerifyReservedInputDie;
            return 0;
        }

        private int VerifyReservedInputDie()
        {
            try
            {
                DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
                if (loadedDie != null && IsInputCameraPreInspectionMode())
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " InputCamera 선행검사 모드: Picker 적재 상태를 유지하고 예약 Die 검사를 계속합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", loadedDie=" + loadedDie.DieId +
                        ", reservedDie=" + _currentDieId +
                        ", side=" + Side + " - Check");
                    loadedDie = null;
                }
                if (loadedDie != null)
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-PICKER-OCCUPIED", "Material",
                        "Picker가 이미 Die를 가지고 있어 예약된 Die의 Input vision 검사를 진행할 수 없습니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", loadedDie=" + loadedDie.DieId +
                        ", reservedDie=" + _currentDieId +
                        ", side=" + Side);
                }

                string reason;
                if (!MaterialStateService.ValidateInputStagePickTarget(
                    _currentDieId,
                    PickerLocationKind,
                    _currentPickerNo,
                    out reason))
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-DIE-NOT-PICKABLE", "Material",
                        "예약된 Input die가 Pick 가능한 상태가 아닙니다. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", reason=" + reason);
                }

                CurrentStep = InputDieVisionPrepareStep.MovePickersToAvoidForInputVisionMove;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VERIFY-EX", Name,
                    "예약된 Input die 상태 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickersToAvoidForInputVisionMoveAsync(CancellationToken ct)
        {
            try
            {
                if (IsPickerMotionOnlyTestMode())
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Picker Motion Only Test 모드: InputVisionX 이동 전 Picker Avoid 동작을 생략합니다. die=" +
                        _currentDieId + ", pickerNo=" + _currentPickerNo + " - Check");
                    CurrentStep = InputDieVisionPrepareStep.MoveInputStageAndVisionToDie;
                    return 0;
                }

                if (IsInputCameraPreInspectionMode())
                {
                    if (_preInspectionOccupiedPickerNos.Contains(_currentPickerNo))
                    {
                        int waitResult = await WaitPickerInputZonesClearForPreInspectionAsync(ct).ConfigureAwait(false);
                        if (waitResult != 0)
                            return waitResult;

                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputCamera 선행검사 모드: Die를 들고 있는 picker는 이동하지 않고 InputVision 검사만 진행합니다. " +
                            "pickerNo=" + _currentPickerNo + ", die=" + _currentDieId + " - Check");
                    }
                    else
                    {
                        int avoidResult = await MoveCurrentPickerToAvoidAndVerifyAsync(
                            "InputCamera 선행검사 전 빈 Picker Avoid",
                            ct).ConfigureAwait(false);
                        if (avoidResult != 0)
                            return avoidResult;

                        avoidResult = await WaitOppositePickerInputZoneClearAsync(
                            "InputCamera 선행검사 전 상대 Picker Input 영역 확인",
                            ct).ConfigureAwait(false);
                        if (avoidResult != 0)
                            return avoidResult;
                    }

                    CurrentStep = InputDieVisionPrepareStep.MoveInputStageAndVisionToDie;
                    return 0;
                }

                int result = await MoveCurrentPickerToAvoidAndVerifyAsync(
                    "InputVisionX 이동 전 Picker Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitOppositePickerInputZoneClearAsync(
                    "InputVisionX 이동 전 상대 Picker Input 영역 확인",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = InputDieVisionPrepareStep.MoveInputStageAndVisionToDie;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-PICKER-AVOID-EX", Name,
                    "InputVisionX 이동 전 Picker Avoid 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAndVisionToDieAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");

                if (_pickTarget == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-DIE-TARGET", "Material",
                        "Input die vision 준비 대상 좌표가 없습니다. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                double targetY = _pickTarget.TargetY;
                double targetX = _pickTarget.TargetX;

                string areaReason;
                if (IsPickerMotionOnlyTestMode())
                {
                    if (!stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-STAGE-WORK-AREA", stage.Name,
                            "Picker Motion Only Test 목표 위치가 InputStage 작업 가능 영역을 벗어났습니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", reason=" + areaReason);
                    }

                    int stageOnlyResult = await MoveInputStageToDiePositionForPickerMotionOnlyAsync(
                        stage,
                        targetX,
                        targetY,
                        ct).ConfigureAwait(false);
                    if (stageOnlyResult != 0)
                        return stageOnlyResult;

                    _visionOffset = CreateZeroInputVisionOffset();
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Picker Motion Only Test 모드: InputVisionX 이동과 비전 검사를 생략하고 보정값 0으로 진행합니다. die=" +
                        _currentDieId + ", pickerNo=" + _currentPickerNo + " - Check");
                    CurrentStep = InputDieVisionPrepareStep.ApplyInputDieVisionOffset;
                    return 0;
                }

                if (!stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-WORK-AREA", stage.Name,
                        "Input die vision 목표 위치가 InputStage 작업 가능 영역을 벗어났습니다. " +
                        "die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", reason=" + areaReason);

                int result = await EnsureNeedleZSafeForCurrentStageTravelAsync(stage, "Input die vision 준비", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputStageZProcessForVisionAsync(stage, "Input die vision 검사", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageVisionPointForPickerAsync(
                    stage,
                    targetX,
                    targetY,
                    "Input die vision 준비",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureNeedleZProcessForVisionAsync(stage, "Input die vision 검사", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = InputDieVisionPrepareStep.RequestInputDieVisionInspection;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE-EX", Name,
                    "Input die vision 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RequestInputDieVisionInspectionAsync(CancellationToken ct)
        {
            try
            {
                int retryCount = Options != null && Options.VisionRetryCount > 0 ? Options.VisionRetryCount : 3;
                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    _visionOffset = await RequestInputVisionOffsetAsync(ct).ConfigureAwait(false);
                    if (_visionOffset != null)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " Input die vision offset 확인 완료. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", attempt=" + attempt +
                            ", dx=" + _visionOffset.DeltaX +
                            ", dy=" + _visionOffset.DeltaY +
                            ", dt=" + _visionOffset.DeltaTheta + " - Ok");

                        CurrentStep = InputDieVisionPrepareStep.ApplyInputDieVisionOffset;
                        return 0;
                    }

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Input die vision offset 재시도. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", attempt=" + attempt + " - Check");
                }

                return Fail("INPUT-DIE-VISION-PREPARE-VISION-NG", "Vision",
                    "Input die vision 검사에 실패했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VISION-EX", "Vision",
                    "Input die vision 검사 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyInputDieVisionOffset()
        {
            try
            {
                if (_visionOffset == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-VISION-OFFSET", "Vision",
                        "Input die vision offset 결과가 없습니다. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                VisionOffset offset = new VisionOffset
                {
                    X = _visionOffset.DeltaX,
                    Y = _visionOffset.DeltaY,
                    R = _visionOffset.DeltaTheta,
                    IsValid = true
                };

                InputStageUnit stage = ResolveInputStage();
                MaterialStateService.UpsertInspection(_currentDieId, new DieInspectionRecord
                {
                    InspectionType = "InputPickVision",
                    Result = MaterialInspectionResult.Ok,
                    Offset = offset,
                    Alignments = new List<InspectionAlignmentSnapshot>
                    {
                        BuildInputStageAlignmentSnapshot(stage, "Input", offset)
                    },
                    Measurements = new List<InspectionMeasurement>
                    {
                        BuildMeasurement("InputAlignOffsetX", _visionOffset.DeltaX, "mm", MaterialInspectionResult.Ok),
                        BuildMeasurement("InputAlignOffsetY", _visionOffset.DeltaY, "mm", MaterialInspectionResult.Ok),
                        BuildMeasurement("InputAlignOffsetT", _visionOffset.DeltaTheta, "deg", MaterialInspectionResult.Ok),
                        BuildBooleanMeasurement("InputVisionResult", true)
                    }
                });

                SaveCurrentStateToItem();
                _inspectionCursor++;
                CurrentStep = InputDieVisionPrepareStep.SelectNextInspectionTarget;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-APPLY-EX", Name,
                    "Input die vision offset 저장 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerInputZonesClearForPreInspectionAsync(CancellationToken ct)
        {
            try
            {
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".PreInspectionInputZoneWait");

                    string ownDetail;
                    bool ownBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        Side == PickerSequenceSide.Front,
                        PickerWorkZone.Input,
                        out ownDetail);

                    string oppositeDetail;
                    bool oppositeBlocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        Side != PickerSequenceSide.Front,
                        PickerWorkZone.Input,
                        out oppositeDetail);

                    if (!ownBlocking && !oppositeBlocking)
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputCamera 선행검사 Input 영역 대기 완료. side=" + Side + " - Ok");
                        }

                        return 0;
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputCamera 선행검사 대기. Picker Input 영역이 비워질 때까지 기다립니다. " +
                            "side=" + Side +
                            ", ownBlocking=" + ownBlocking +
                            ", ownDetail=" + ownDetail +
                            ", oppositeBlocking=" + oppositeBlocking +
                            ", oppositeDetail=" + oppositeDetail + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(50, ct).ConfigureAwait(false);
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
                return Fail("INPUT-DIE-VISION-PREPARE-PRE-INSPECTION-WAIT-EX", Name,
                    "InputCamera 선행검사 Input 영역 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private async Task<int> WaitOppositePickerInputZoneClearAsync(string description, CancellationToken ct)
        {
            try
            {
                bool oppositeFront = Side != PickerSequenceSide.Front;
                if ((oppositeFront && FrontPicker == null) ||
                    (!oppositeFront && RearPicker == null))
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " 상대 Picker Input 영역 확인 생략. unit 없음. description=" +
                        description + " - Check");
                    return 0;
                }

                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                while (DateTime.UtcNow >= DateTime.MinValue)
                {
                    string detail;
                    bool blocking = PickerZoneInterlockRules.IsPickerBlockingZoneTransport(
                        Context != null ? Context.Machine : null,
                        oppositeFront,
                        PickerWorkZone.Input,
                        out detail);

                    if (!blocking)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " 상대 Picker Input 영역 확인 완료. description=" + description + " - Ok");
                        return 0;
                    }

                    string alarmState = BuildOppositePickerAlarmState(oppositeFront);
                    if (!string.IsNullOrEmpty(alarmState))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-OPPOSITE-PICKER-ALARM", Name,
                            description + " 대기 불가: 상대 Picker 축 알람 상태입니다. " +
                            "detail=" + detail + ", " + alarmState);
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-OPPOSITE-INPUT-ZONE-TIMEOUT", Name,
                            description + " 대기 시간 초과: 상대 Picker가 Input 영역에서 벗어나지 않았습니다. " +
                            "timeoutMs=" + timeoutMs + ", detail=" + detail);
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " 상대 Picker Input 영역 해제 대기. description=" + description +
                            ", detail=" + detail + ", timeoutMs=" + timeoutMs + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(50, ct).ConfigureAwait(false);
                }

                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " 상대 Picker Input 영역 확인 생략. RearPickerUnit 없음. description=" +
                            description + " - Check");
                        return 0;
                    }

                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                    {
                        if (!RearPicker.IsRearPickerInDiePickPosition(pickerNo))
                            continue;

                        return Fail("INPUT-DIE-VISION-PREPARE-OPPOSITE-INPUT-ZONE", "RearPickerUnit",
                            description + " 실패. RearPicker가 Input Pick 영역에 있습니다. " +
                            "RearPicker를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo);
                    }
                }
                else
                {
                    if (FrontPicker == null)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " 상대 Picker Input 영역 확인 생략. FrontPickerUnit 없음. description=" +
                            description + " - Check");
                        return 0;
                    }

                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                    {
                        if (!FrontPicker.IsFrontPickerInDiePickPosition(pickerNo))
                            continue;

                        return Fail("INPUT-DIE-VISION-PREPARE-OPPOSITE-INPUT-ZONE", "FrontPickerUnit",
                            description + " 실패. FrontPicker가 Input Pick 영역에 있습니다. " +
                            "FrontPicker를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo);
                    }
                }

                WriteLog("InputDieVisionPrepareSequence",
                    Name + " 상대 Picker Input 영역 확인 완료. description=" + description + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-OPPOSITE-INPUT-ZONE-EX", Name,
                    description + " 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildOppositePickerAlarmState(bool oppositeFront)
        {
            QMC.Common.Motion.BaseAxis x = oppositeFront && FrontPicker != null
                ? FrontPicker.PickerX
                : (!oppositeFront && RearPicker != null ? RearPicker.PickerX : null);
            QMC.Common.Motion.BaseAxis y = oppositeFront && FrontPicker != null
                ? FrontPicker.PickerY
                : (!oppositeFront && RearPicker != null ? RearPicker.PickerY : null);

            string state = string.Empty;
            AppendAxisAlarmState(ref state, oppositeFront ? "FrontPickerX" : "RearPickerX", x);
            AppendAxisAlarmState(ref state, oppositeFront ? "FrontPickerY" : "RearPickerY", y);
            return state;
        }

        private static void AppendAxisAlarmState(ref string state, string name, QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null || !axis.IsAlarm)
                return;

            if (state.Length > 0)
                state += " ";

            state += name +
                "(servo=" + axis.IsServoOn +
                ", alarm=" + axis.IsAlarm +
                ", moving=" + axis.IsMoving +
                ", actual=" + axis.ActualPosition + ");";
        }

        private async Task<VisionAlignResult> RequestInputVisionOffsetAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return null;

            await Task.Delay(VisionInspectionSettleDelayMs, ct).ConfigureAwait(false);

            if (IsSimulationOrDryRun(stage))
                return SimulateInputVisionOffset();

            if (stage.Vision == null)
                return null;

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync("InputPickDie").ConfigureAwait(false);
        }

        private async Task<int> EnsureInputStageZProcessForVisionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.WaferZ == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGEZ-RECIPE", stage.Name,
                        description + " 전 InputStage Z Process 위치 정보가 없습니다.");

                double target = stage.Recipe.WaferZ.ProcessPosition;
                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferExpandingZ, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.WaferExpandingZ,
                        target,
                        description + " StageZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.WaferExpandingZ,
                        target,
                        description + " StageZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.WaferExpandingZ,
                    target,
                    description + " StageZ process");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGEZ-PROCESS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 InputStage Z Process 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureNeedleZProcessForVisionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.NeedleZ == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLEZ-RECIPE", stage.Name,
                        description + " 전 NeedleZ Process 위치 정보가 없습니다.");

                double needleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : stage.Recipe.NeedleX.ProcessPosition;
                double stageY = stage.StageY != null ? stage.StageY.ActualPosition : stage.ResolveWorkAreaCenterY();
                string areaReason;
                if (!stage.IsNeedleWorkPointInArea(needleX, stageY, out areaReason))
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-WORK-AREA", stage.Name,
                        description + " 전 NeedleZ를 Process 위치로 올릴 수 없습니다. Needle 작업 원을 벗어났습니다. " +
                        "needleX=" + needleX.ToString("F6") +
                        ", stageY=" + stageY.ToString("F6") +
                        ", reason=" + areaReason);

                double target = stage.Recipe.NeedleZ.ProcessPosition;
                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleZ, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.NeedleZ,
                        target,
                        description + " NeedleZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.NeedleZ,
                        target,
                        description + " NeedleZ process",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.NeedleZ,
                    target,
                    description + " NeedleZ process");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-NEEDLEZ-PROCESS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 NeedleZ Process 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private async Task<int> EnsureNeedleZSafeForCurrentStageTravelAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.NeedleZ == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLEZ-RECIPE", stage.Name,
                        description + " 전 NeedleZ Avoid 위치 정보가 없습니다.");

                double currentX = stage.CameraX != null ? stage.CameraX.ActualPosition : stage.ResolveWorkAreaCenterX();
                double currentY = stage.StageY != null ? stage.StageY.ActualPosition : stage.ResolveWorkAreaCenterY();
                string areaReason;
                if (stage.IsInputStageWorkPointInArea(currentX, currentY, out areaReason))
                    return 0;
                if (stage.IsNeedleZInSafePosition())
                    return 0;

                double target = stage.Recipe.NeedleZ.AvoidPosition;
                int result = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    target,
                    description + " NeedleZ avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    target,
                    description + " NeedleZ avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.NeedleZ,
                    target,
                    description + " NeedleZ avoid");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-NEEDLEZ-AVOID-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 NeedleZ Avoid 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private VisionAlignResult SimulateInputVisionOffset()
        {
            lock (SimVisionRandomLock)
            {
                return new VisionAlignResult
                {
                    DeltaX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaTheta = (SimVisionRandom.NextDouble() - 0.5) * 0.02
                };
            }
        }

        private VisionAlignResult CreateZeroInputVisionOffset()
        {
            return new VisionAlignResult
            {
                DeltaX = 0.0,
                DeltaY = 0.0,
                DeltaTheta = 0.0
            };
        }

        private async Task<int> MoveInputStageToDiePositionForPickerMotionOnlyAsync(
            InputStageUnit stage,
            double targetX,
            double targetY,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureNeedleZSafeForCurrentStageTravelAsync(
                    stage,
                    "Picker Motion Only Test StageY 이동",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageYAndVerifyAsync(
                    stage,
                    targetX,
                    targetY,
                    "Picker Motion Only Test StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.WaferY,
                    targetY,
                    "Picker Motion Only Test StageY 최종 위치 확인");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-MOTION-ONLY-STAGEY-EX", stage != null ? stage.Name : "InputStageUnit",
                    "Picker Motion Only Test StageY 이동 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsSimulationOrDryRun(InputStageUnit stage)
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            if (stage != null && stage.IsInputStageSimulationOrDryRun())
                return true;

            return IsPickerSimulationOrDryRun();
        }

        private bool IsInputCameraPreInspectionMode()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Auto &&
                   Options.InputCameraPreInspectionMode;
        }

        private async Task<int> MoveInputStageVisionPointForPickerAsync(
            InputStageUnit stage,
            double targetX,
            double targetY,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");

                double currentX = stage.CameraX != null ? stage.CameraX.ActualPosition : targetX;
                double currentY = stage.StageY != null ? stage.StageY.ActualPosition : targetY;
                double targetNeedleX = ResolveNeedleXForVisionX(stage, targetX);

                string needleAreaReason;
                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out needleAreaReason))
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-WORK-AREA", stage.Name,
                        "Input die vision 준비 중 Needle 목표 위치가 작업 가능 영역을 벗어났습니다. " +
                        "description=" + description +
                        ", visionX=" + targetX.ToString("F6") +
                        ", needleX=" + targetNeedleX.ToString("F6") +
                        ", stageY=" + targetY.ToString("F6") +
                        ", reason=" + needleAreaReason);
                }

                bool visionXInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, targetX);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, targetY);
                if (visionXInPosition && stageYInPosition)
                {
                    int needleResult = await MoveNeedleXAndVerifyAsync(
                        stage,
                        targetNeedleX,
                        description + " NeedleX",
                        ct).ConfigureAwait(false);
                    if (needleResult != 0)
                        return needleResult;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                if (!visionXInPosition && stageYInPosition)
                {
                    int result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                if (visionXInPosition && !stageYInPosition)
                {
                    int result = await MoveInputStageYAndVerifyAsync(stage, targetX, targetY, description + " StageY", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveNeedleXAndVerifyAsync(
                        stage,
                        targetNeedleX,
                        description + " NeedleX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string xFirstReason;
                bool canMoveXFirst = stage.IsInputStageWorkPointInArea(targetX, currentY, out xFirstReason);
                if (canMoveXFirst)
                {
                    int result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputStageYAndVerifyAsync(stage, targetX, targetY, description + " StageY", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string yFirstReason;
                bool canMoveYFirst = stage.IsInputStageWorkPointInArea(currentX, targetY, out yFirstReason);
                if (canMoveYFirst)
                {
                    int result = await MoveInputStageYAndVerifyAsync(stage, currentX, targetY, description + " StageY", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string finalTargetReason;
                if (stage.IsInputStageWorkPointInArea(targetX, targetY, out finalTargetReason))
                {
                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY 안전 진입",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description + " VisionX/NeedleX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-PATH", stage.Name,
                    description + " 위치로 이동할 안전한 X/Y 순서를 찾지 못했습니다. " +
                    "currentX=" + currentX.ToString("F6") +
                    ", currentY=" + currentY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") +
                    ", xFirst=" + xFirstReason +
                    ", yFirst=" + yFirstReason);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " X/Y 이동 순서 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXStageYAndNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double visionTarget,
            double stageYTarget,
            double needleTarget,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var commandTasks = new List<Task<int>>();
                var commandAxes = new List<Tuple<WaferStageAxis, double, string>>();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, visionTarget))
                {
                    commandAxes.Add(Tuple.Create(WaferStageAxis.VisionX, visionTarget, description + " VisionX"));
                    commandTasks.Add(MoveInputStageAxisCommandAsync(stage, WaferStageAxis.VisionX, visionTarget, description + " VisionX", ct));
                }

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, stageYTarget))
                {
                    commandAxes.Add(Tuple.Create(WaferStageAxis.WaferY, stageYTarget, description + " StageY"));
                    commandTasks.Add(MoveInputStageYForPickerWorkPointCommandAsync(stage, visionTarget, stageYTarget, description + " StageY", ct));
                }

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, needleTarget))
                {
                    commandAxes.Add(Tuple.Create(WaferStageAxis.NeedleX, needleTarget, description + " NeedleX"));
                    commandTasks.Add(MoveInputStageAxisCommandAsync(stage, WaferStageAxis.NeedleX, needleTarget, description + " NeedleX", ct));
                }

                if (commandTasks.Count > 0)
                {
                    int[] commandResults = await Task.WhenAll(commandTasks).ConfigureAwait(false);
                    for (int i = 0; i < commandResults.Length; i++)
                    {
                        if (commandResults[i] != 0)
                        {
                            Tuple<WaferStageAxis, double, string> axis = commandAxes[i];
                            return Fail("INPUT-DIE-VISION-PREPARE-STAGE-XY-PARALLEL", stage != null ? stage.Name : "InputStageUnit",
                                axis.Item3 + " 동시 이동 명령 실패. result=" + commandResults[i] +
                                ", " + BuildInputStageAxisState(stage, axis.Item1, axis.Item2) +
                                PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));
                        }
                    }

                    var waitTasks = new List<Task<int>>();
                    foreach (Tuple<WaferStageAxis, double, string> axis in commandAxes)
                        waitTasks.Add(WaitInputStageAxisInPositionResultAsync(stage, axis.Item1, axis.Item2, axis.Item3, ct));

                    int[] waitResults = await Task.WhenAll(waitTasks).ConfigureAwait(false);
                    for (int i = 0; i < waitResults.Length; i++)
                    {
                        if (waitResults[i] != 0)
                            return waitResults[i];
                    }
                }

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-XY-PARALLEL-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 동시 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXAndNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double visionTarget,
            double needleTarget,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> visionMove = MoveInputVisionXAndVerifyAsync(
                    stage,
                    visionTarget,
                    description + " VisionX",
                    ct);
                Task<int> needleMove = MoveNeedleXAndVerifyAsync(
                    stage,
                    needleTarget,
                    description + " NeedleX",
                    ct);

                int[] results = await Task.WhenAll(visionMove, needleMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-VISION-NEEDLE-X", stage != null ? stage.Name : "InputStageUnit",
                        "InputVisionX와 NeedleX 동시 이동 실패. " +
                        "visionResult=" + results[0] +
                        ", needleResult=" + results[1] +
                        ", visionTarget=" + visionTarget.ToString("F6") +
                        ", needleTarget=" + needleTarget.ToString("F6") +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.VisionX, visionTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, needleTarget));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VISION-NEEDLE-X-EX", stage != null ? stage.Name : "InputStageUnit",
                    "InputVisionX와 NeedleX 동시 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXAndVerifyAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VISIONX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXAndVerifyAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, target))
                    return CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, target, description);

                int result = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    target,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    target,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-NEEDLEX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    "NeedleX 이동 중 예외가 발생했습니다. description=" + description +
                    ", target=" + target.ToString("F6") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageYAndVerifyAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, target))
                {
                    int result = await MoveInputStageYForPickerWorkPointCommandAsync(
                        stage,
                        workAreaVisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.WaferY,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGEY-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageYForPickerWorkPointCommandAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(
                    PickerInputStageMoveHelper.MoveStageYForPickerWorkPointCommandAsync(
                        stage,
                        workAreaVisionX,
                        target,
                        Options != null && Options.FineMove,
                        "InputDieVisionPrepare"),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE", stage.Name,
                        description + " 이동 명령 실패. result=" + result +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisCommandAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result;
                if (axis == WaferStageAxis.VisionX)
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(stage.CameraX, target, "AutoInputDieVisionPrepare;Side=" + Side + ";InputVisionX;" + description))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                            ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    result = await AwaitStepWithCancellationAsync(
                        stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                        ct).ConfigureAwait(false);
                }

                if (result != 0)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE", stage.Name,
                        description + " 이동 명령 실패. result=" + result +
                        ", " + BuildInputStageAxisState(stage, axis, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitInputStageAxisInPositionResultAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);

                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("INPUT-DIE-VISION-PREPARE-STAGE", waitResult), stage.Name,
                        description + " 이동/InPosition 대기 실패. " +
                        FormatAxisMoveWaitResult(waitResult, BuildInputStageAxisState(stage, axis, target)));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputStageVisionPointFinalPosition(
            InputStageUnit stage,
            double targetX,
            double targetY,
            string description)
        {
            int result = CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, targetX, description + " VisionX");
            if (result != 0)
                return result;

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, targetY, description + " StageY");
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.NeedleX,
                ResolveNeedleXForVisionX(stage, targetX),
                description + " NeedleX");
        }

        private int CheckInputStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            try
            {
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-AXIS", stage != null ? stage.Name : "InputStageUnit",
                        description + " 축을 찾을 수 없습니다. " + BuildInputStageAxisState(stage, axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-POSITION", stage.Name,
                        description + " 최종 위치 확인 실패. " + BuildInputStageAxisState(stage, axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-POSITION-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 최종 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void SetCurrentItem(InputDieVisionPreparedItem item)
        {
            _currentItem = item;
            _currentPickerIndex = item != null ? item.PickerIndex : -1;
            _currentPickerNo = item != null ? item.PickerNo : 0;
            _currentDieId = item != null ? item.DieId : "";
            _pickTarget = item != null ? item.PickTarget : null;
            _visionOffset = item != null ? item.VisionOffset : null;
        }

        private void SaveCurrentStateToItem()
        {
            if (_currentItem == null)
                return;

            _currentItem.PickerIndex = _currentPickerIndex;
            _currentItem.PickerNo = _currentPickerNo;
            _currentItem.DieId = _currentDieId;
            _currentItem.PickTarget = _pickTarget;
            _currentItem.VisionOffset = _visionOffset;
        }

        private void ClearCurrentContext()
        {
            _currentItem = null;
            _currentPickerIndex = -1;
            _currentPickerNo = 0;
            _currentDieId = "";
            _pickTarget = null;
            _visionOffset = null;
        }

        private void ReleasePreparedReservationsIfNeeded()
        {
            try
            {
                for (int i = 0; i < _preparedItems.Count; i++)
                {
                    InputDieVisionPreparedItem item = _preparedItems[i];
                    if (item == null || item.DiePicked || string.IsNullOrWhiteSpace(item.DieId))
                        continue;

                    MaterialStateService.ReleaseInputStagePickReservation(
                        item.DieId,
                        PickerLocationKind,
                        item.PickerNo);

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Input die vision 준비 예약을 해제했습니다. die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("InputDieVisionPrepareSequence",
                    "Input die vision 준비 예약 해제 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private double ResolveNeedleXForVisionX(InputStageUnit stage, double visionX, double visionOffsetX = 0.0)
        {
            double offset = stage != null && stage.Setup != null
                ? stage.Setup.NeedleXToVisionXOffset
                : 0.0;
            return visionX + visionOffsetX - offset;
        }

        private InputStageUnit ResolveInputStage()
        {
            return Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
        }

        private static BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                case WaferStageAxis.WaferExpandingZ:
                    return stage.ExpanderZ;
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                case WaferStageAxis.NeedleX:
                    return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ:
                    return stage.NeedleZ;
                case WaferStageAxis.EjectPinZ:
                    return stage.EjectPinZ;
                default:
                    return null;
            }
        }

        private static string BuildInputStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            BaseAxis item = ResolveInputStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance;
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private bool IsInputStageAxisAlreadyInPosition(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            try
            {
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                return item != null &&
                       !item.IsMoving &&
                       !item.IsAlarm &&
                       IsAxisInPosition(item, target);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static async Task<TResult> AwaitStepWithCancellationAsync<TResult>(Task<TResult> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, default(TResult), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
