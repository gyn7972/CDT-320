using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.VisionComm;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputDieVisionPrepareSequence : PickerSequenceBase<InputDieVisionPrepareStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const int InputVisionTimeoutMs = 5000;
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
        private SequenceResourceLease _preInspectionInputStageLease;

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

        public void ReleasePreparedReservations()
        {
            ReleasePreparedReservationsIfNeeded();
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
            catch (SequenceStopException)
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
                ReleasePreInspectionInputStageArea();
                if (!_completedSuccessfully)
                {
                    // CycleTime ERR 초크 포인트 — 실패/취소 종료 시 활성 촬영 사이클 일괄 ERR.
                    QMC.CDT320.Diagnostics.HandlerTactLog.CycleErrorAll(
                        "INPUTVISION|" + (Side == PickerSequenceSide.Front ? "F" : "R"));
                    await DrainOutstandingWaferResultsAfterFailureAsync(ct).ConfigureAwait(false);
                    ReleasePreparedReservationsIfNeeded();
                }
            }
        }

        /// <summary>CycleTime 계측 키 — 촬영 사이클 동안 불변(피커+다이).</summary>
        private string TactRequestId()
        {
            return (Side == PickerSequenceSide.Front ? "F" : "R") + _currentPickerNo + "-" +
                (string.IsNullOrEmpty(_currentDieId) ? ("c" + _inspectionCursor) : _currentDieId);
        }

        private async Task DrainOutstandingWaferResultsAfterFailureAsync(CancellationToken ct)
        {
            for (int i = 0; i < _preparedItems.Count; i++)
            {
                InputDieVisionPreparedItem item = _preparedItems[i];
                bool canceled = await DrainInputVisionRequestHandleAsync(
                    item != null ? item.VisionRequest : null,
                    "Input die vision 실패 정리",
                    Name + " die=" + (item != null && item.DieId != null ? item.DieId : string.Empty) +
                    ", pickerNo=" + (item != null ? item.PickerNo : 0),
                    ct).ConfigureAwait(false);
                if (canceled)
                    break;
            }
        }

        /// <summary>실패/폐기 경로 핸들 정리 공개 진입점 — 조기 허가 경로(mark 시퀀스 실패 시)에서 사용.</summary>
        public Task DrainPreparedResultsAfterFailureAsync(CancellationToken ct)
        {
            return DrainOutstandingWaferResultsAfterFailureAsync(ct);
        }

        /// <summary>
        /// EPD 완료·RESULT 미회수 핸들 1건 드레인 코어 — 회수 시도 후 실패 시 MarkError.
        /// 이미 회수/에러 처리된 핸들은 그대로 통과. 반환값: 취소로 중단되었으면 true.
        /// 조기 허가(EPD 시점 허가) 폐기 경로들(허가 스토어/픽업 배치 정리)과 공용.
        /// </summary>
        internal static async Task<bool> DrainInputVisionRequestHandleAsync(
            VisionRequestHandle handle,
            string reason,
            string logOwner,
            CancellationToken ct)
        {
            if (handle == null || handle.IsResultDone || !string.IsNullOrWhiteSpace(handle.Error))
                return false;

            if (ct.IsCancellationRequested)
            {
                handle.MarkError(reason + " 취소로 RESULT를 회수하지 못했습니다.");
                return true;
            }

            try
            {
                VisionInspectionResult result = await AutoVisionRequestService.WaitInspectionStageAsync(
                    handle,
                    VisionInspectionCommands.Result,
                    InputVisionTimeoutMs,
                    ct).ConfigureAwait(false);
                if (result == null && string.IsNullOrWhiteSpace(handle.Error))
                    handle.MarkError(reason + " 중 RESULT를 회수하지 못했습니다.");
                QMC.Common.Log.Write("Main", "SYSTEM", "InputDieVisionPrepareSequence",
                    (logOwner ?? "-") + " EPD 완료 WAFER RESULT 정리. reason=" + reason +
                    ", groupId=" + handle.Request.GroupId +
                    ", received=" + (result != null) + " - Check");
                return false;
            }
            catch (OperationCanceledException)
            {
                handle.MarkError(reason + " 중 취소되었습니다.");
                return true;
            }
            catch (Exception ex)
            {
                handle.MarkError(reason + " 예외. " + ex.Message);
                QMC.Common.Log.Write("Main", "SYSTEM", "InputDieVisionPrepareSequence",
                    (logOwner ?? "-") + " WAFER RESULT 정리 예외. reason=" + reason +
                    ", groupId=" + handle.Request.GroupId +
                    ", error=" + ex.Message + " - Check");
                return false;
            }
        }

        /// <summary>
        /// RESULT 회수 코어 — WaitInspectionStageAsync(Result) → ToAlignResult(Wafer) 변환,
        /// 성공 시 WaferVisionResultStore.RecordAlign 기록 후 offset 반환 (실패는 null).
        /// CollectVisionResultsAsync(내부 경로)와 픽업 CalculatePickTargets(조기 허가 경로)가 공용.
        /// </summary>
        internal static async Task<VisionAlignResult> CollectInputDieVisionResultCoreAsync(
            VisionRequestHandle handle,
            CancellationToken ct)
        {
            VisionInspectionResult correlatedResult = await AutoVisionRequestService.WaitInspectionStageAsync(
                handle,
                VisionInspectionCommands.Result,
                InputVisionTimeoutMs,
                ct).ConfigureAwait(false);
            MatchResultDto match = correlatedResult != null ? correlatedResult.MatchResult : null;

            VisionAlignResult offset = VisionCameraCalibrationTransform.ToAlignResult(
                AutoVisionChannel.Wafer,
                match,
                0.0);

            if (offset != null)
            {
                QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordAlign(
                    VisionAlignTargetIds.InputPickDie,
                    offset);
            }

            return offset;
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
                case InputDieVisionPrepareStep.StartInputDieVisionInspection:
                    return StartInputDieVisionInspectionAsync(ct);
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
                            ", loadedDie=" + loadedDie.DieId + " - Check");
                        loadedDie = null;
                    }
                    if (loadedDie != null)
                    {
                        occupiedPickerCount++;
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " Picker가 이미 Die를 가지고 있어 Input die vision 예약에서 제외합니다. " +
                            "pickerNo=" + pickerNo +
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
                    // C1(return-follow): Auto Conti 게이트면 존 이탈 블로킹 대기를 생략한다 —
                    // VisionX 진입은 follow의 간격 제약+MotionGuard(페어 간격)가, StageY/NeedleX는
                    // 스테이지 이동 스텝의 MotionGuard dry-run 대기가 각각 대기를 대신한다.
                    if (IsVisionReturnFollowGateSatisfied())
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputCamera 선행검사 모드(팔로잉 게이트): Picker Input 영역 이탈 대기를 생략하고 " +
                            "VisionX 팔로잉 진입으로 대체합니다. pickerNo=" + _currentPickerNo +
                            ", die=" + _currentDieId + ", side=" + Side + " - Check");
                        CurrentStep = InputDieVisionPrepareStep.MoveInputStageAndVisionToDie;
                        return 0;
                    }

                    int waitResult = await WaitPickerInputZonesClearForPreInspectionAsync(ct).ConfigureAwait(false);
                    if (waitResult != 0)
                        return waitResult;

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " InputCamera 선행검사 모드: Picker X/Y를 직접 Avoid 이동하지 않고 Input 영역 이탈만 확인합니다. " +
                        "Place/Bottom/Side 진행 중인 같은 PickerX 명령을 덮어쓰지 않기 위한 안전 동작입니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", occupiedAtBatch=" + _preInspectionOccupiedPickerNos.Contains(_currentPickerNo) +
                        ", side=" + Side + " - Check");

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

                // C1(return-follow): Auto Conti 게이트면 존 클리어 블로킹 대기를 생략한다.
                // VisionX 진입의 X 간섭은 follow(간격 제약)+MotionGuard(페어 간격 제3 분기)가 담당하고,
                // StageY/NeedleX 등 스테이지 이동의 안전 전제(피커 하강/Unknown/EjectPinZ)는 아래
                // WaitInputStagePlaneGuardClearBeforeVisionOverlapAsync(MotionGuard dry-run)가 대기한다.
                bool useReturnFollowOverlap = IsVisionReturnFollowGateSatisfied();
                if (!useReturnFollowOverlap)
                {
                    int pickerClearResult = await WaitFrontRearPickerInputZonesClearBeforeVisionAsync(ct).ConfigureAwait(false);
                    if (pickerClearResult != 0)
                        return pickerClearResult;
                }
                else
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " 팔로잉 게이트: Picker Input 존 클리어 블로킹 대기를 생략합니다(오버랩 진입). " +
                        "die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Check");
                }

                int permissionClearResult = await WaitInputCameraPickUpPermissionsClearForPreInspectionAsync(ct).ConfigureAwait(false);
                if (permissionClearResult != 0)
                    return permissionClearResult;

                int acquireResult = await AcquirePreInspectionInputStageAreaIfNeededAsync(ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                permissionClearResult = await WaitInputCameraPickUpPermissionsClearForPreInspectionAsync(ct).ConfigureAwait(false);
                if (permissionClearResult != 0)
                    return permissionClearResult;

                if (_pickTarget == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-DIE-TARGET", "Material",
                        "Input die vision 준비 대상 좌표가 없습니다. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                double targetY = _pickTarget.TargetY;
                double targetX = _pickTarget.TargetX;
                double targetNeedleX = ResolveNeedleXForVisionX(stage, targetX);

                string areaReason;
                if (IsPickerMotionOnlyTestMode())
                {
                    if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out areaReason))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-STAGE-WORK-AREA", stage.Name,
                            "Picker Motion Only Test 목표 위치가 InputStage 작업 가능 영역을 벗어났습니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", needleX=" + targetNeedleX.ToString("F6") +
                            ", reason=" + areaReason);
                    }

                    int stageOnlyResult = await EnsureEjectPinZAvoidForStageTravelAsync(stage, "Input die vision 준비", ct).ConfigureAwait(false);
                    if (stageOnlyResult != 0)
                        return stageOnlyResult;

                    stageOnlyResult = await EnsureWaferAlignThetaPositionAsync(stage, "Input die vision 준비 전 StageT 보정 위치", ct).ConfigureAwait(false);
                    if (stageOnlyResult != 0)
                        return stageOnlyResult;

                    stageOnlyResult = await MoveInputStageToDiePositionForPickerMotionOnlyAsync(
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

                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out areaReason))
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-WORK-AREA", stage.Name,
                        "Input die vision 목표 위치가 InputStage 작업 가능 영역을 벗어났습니다. " +
                        "die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", needleX=" + targetNeedleX.ToString("F6") +
                        ", reason=" + areaReason);

                // CycleTime 계측 시작 (Auto 운전만) — 다이 1개 촬영 사이클(위치 이동~EPD).
                if (Options != null && Options.RunMode == SequenceRunMode.Auto)
                    QMC.CDT320.Diagnostics.HandlerTactLog.CycleStart(
                        "INPUTVISION",
                        TactRequestId(),
                        Side == PickerSequenceSide.Front ? "FRONT" : "REAR",
                        _currentPickerNo,
                        _pickTarget != null ? _pickTarget.OrderIndex : _inspectionCursor,
                        _currentDieId);

                // C1/R1-①-3(return-follow): 게이트 시 스테이지(StageY/NeedleX 등) 이동의 안전 전제를
                // MotionGuard dry-run으로 대기 — 기존 존 클리어 대기가 보호하던 비-X 대상
                // (VerifyPickerZAxesAvoidWhenInputRisk/EjectPinZ zero-avoid/작업영역/Unknown)을 동일한
                // fail-safe 의미(대기)로 유지한다.
                if (useReturnFollowOverlap)
                {
                    int stageGuardResult = await WaitInputStagePlaneGuardClearBeforeVisionOverlapAsync(
                        stage,
                        targetNeedleX,
                        targetY,
                        ct).ConfigureAwait(false);
                    if (stageGuardResult != 0)
                        return stageGuardResult;
                }

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("INPUTVISION", TactRequestId(), "EjectPinZ");
                int result = await EnsureEjectPinZAvoidForStageTravelAsync(stage, "Input die vision 준비", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("INPUTVISION", TactRequestId(), "EjectPinZ");

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("INPUTVISION", TactRequestId(), "StageT");
                result = await EnsureWaferAlignThetaPositionAsync(stage, "Input die vision 준비 전 StageT 보정 위치", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("INPUTVISION", TactRequestId(), "StageT");

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("INPUTVISION", TactRequestId(), "StageZ");
                result = await EnsureInputStageZProcessForVisionAsync(stage, "Input die vision 검사", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("INPUTVISION", TactRequestId(), "StageZ");

                QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("INPUTVISION", TactRequestId(), "Vision/StageXY");
                result = await MoveInputStageVisionPointForPickerAsync(
                    stage,
                    targetX,
                    targetY,
                    "Input die vision 준비",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("INPUTVISION", TactRequestId(), "Vision/StageXY");

                // 공정 중 NeedleZ는 이동하지 않는다. 픽업 준비 상승은 PickerPickUpSequence가 수행한다.
                CurrentStep = InputDieVisionPrepareStep.StartInputDieVisionInspection;
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

        private async Task<int> StartInputDieVisionInspectionAsync(CancellationToken ct)
        {
            try
            {
                if (_currentItem == null || _pickTarget == null)
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-VISION-TARGET", "Vision",
                        "Input die vision 촬영 대상 정보가 없습니다. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);
                }

                ct.ThrowIfCancellationRequested();
                await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

                int requestIndex = ResolveInputVisionRequestIndex(_currentItem);
                _currentItem.VisionRequestIndex = requestIndex;

                InputStageUnit stage = ResolveInputStage();
                bool useConnectedDryRunVision = IsDryRunWithWaferVisionConnected();
                if (IsSimulationOrDryRun(stage) && !useConnectedDryRunVision)
                {
                    _visionOffset = SimulateInputVisionOffset();
                    _currentItem.VisionOffset = _visionOffset;
                    _currentItem.ExposureCompleted = true;
                }
                else
                {
                    string materialReason;
                    if (!MaterialStateService.ValidateInputStagePickTarget(
                        _currentDieId,
                        PickerLocationKind,
                        _currentPickerNo,
                        out materialReason))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-CONTEXT-CHANGED", "Material",
                            "Vision 요청 직전 Input die 자재 문맥이 변경되었습니다. 전송을 차단합니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", reason=" + materialReason);
                    }

                    string finder = VisionAlignTargetIds.ResolveWaferFinder(VisionAlignTargetIds.InputPickDie);
                    int fb = Side == PickerSequenceSide.Front ? 0 : 1;
                    VisionInspectionRequestContext requestContext = VisionInspectionContextFactory.CreateAuto(
                        AutoVisionChannel.Wafer,
                        finder,
                        fb,
                        _currentPickerNo,
                        requestIndex,
                        _pickTarget.DieMapX,
                        _pickTarget.DieMapY,
                        0,
                        _currentDieId,
                        _pickTarget.WaferId,
                        VisionInspectionOperations.Match,
                        VisionResultTimings.Deferred,
                        string.Empty);
                    QMC.CDT320.Diagnostics.HandlerTactLog.MotionStart("INPUTVISION", TactRequestId(), "촬영(EPD)");
                    VisionRequestHandle requestHandle = await AutoVisionRequestService.StartInspectionRequestAsync(
                        requestContext,
                        InputVisionTimeoutMs,
                        ct).ConfigureAwait(false);
                    if (requestHandle == null)
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-EPD", "Vision",
                            "Input die vision 촬영 EPD를 받지 못했습니다. 다음 위치 이동을 차단합니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", requestIndex=" + requestIndex);
                    }
                    QMC.CDT320.Diagnostics.HandlerTactLog.MotionEnd("INPUTVISION", TactRequestId(), "촬영(EPD)");

                    _currentItem.VisionRequest = requestHandle;
                    _currentItem.ExposureCompleted = true;
                }

                WriteLog("InputDieVisionPrepareSequence",
                    Name + " Input die vision 촬영 EPD 확인 완료. Result를 기다리지 않고 다음 촬영으로 진행합니다. die=" +
                    _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", requestIndex=" + requestIndex + " - Ok");

                // CycleTime 정상 종결 — 촬영 사이클은 EPD 확인 시점까지 (RESULT 회수는 픽업 시점 별도).
                QMC.CDT320.Diagnostics.HandlerTactLog.CycleResult("INPUTVISION", TactRequestId());

                SaveCurrentStateToItem();
                _inspectionCursor++;
                ReleasePreInspectionInputStageArea();
                ClearCurrentContext();
                CurrentStep = InputDieVisionPrepareStep.SelectNextInspectionTarget;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VISION-EX", "Vision",
                    "Input die vision EPD 촬영 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> CollectVisionResultsAsync(CancellationToken ct)
        {
            bool completed = false;
            try
            {
                _inspectionCursor = 0;
                while (_inspectionCursor < _preparedItems.Count)
                {
                    ct.ThrowIfCancellationRequested();
                    SetCurrentItem(_preparedItems[_inspectionCursor]);

                    if (_currentItem == null || !_currentItem.ExposureCompleted)
                    {
                        int skipResult = SkipCurrentVisionFailedDieAndContinue(1);
                        if (skipResult != 0)
                            return skipResult;
                        continue;
                    }

                    if (_currentItem.VisionOffset == null)
                    {
                        // 현재 기준: 회수 코어를 조기 허가 경로(픽업 CalculatePickTargets)와 공용화했다 — 동작 불변.
                        _visionOffset = await CollectInputDieVisionResultCoreAsync(
                            _currentItem.VisionRequest,
                            ct).ConfigureAwait(false);
                        _currentItem.VisionOffset = _visionOffset;
                    }
                    else
                    {
                        _visionOffset = _currentItem.VisionOffset;
                    }

                    if (_visionOffset == null)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " Input die vision Result 실패로 Die를 SKIP 처리합니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", requestIndex=" + _currentItem.VisionRequestIndex + " - Skip");

                        int skipResult = SkipCurrentVisionFailedDieAndContinue(1);
                        if (skipResult != 0)
                            return skipResult;
                        continue;
                    }

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " Input die vision Result 회수 완료. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", requestIndex=" + _currentItem.VisionRequestIndex +
                        ", dx=" + _visionOffset.DeltaX +
                        ", dy=" + _visionOffset.DeltaY +
                        ", dt=" + _visionOffset.DeltaTheta + " - Ok");

                    _inspectionCursor++;
                }

                ClearCurrentContext();
                _inspectionCursor = 0;
                while (_inspectionCursor < _preparedItems.Count)
                {
                    ct.ThrowIfCancellationRequested();
                    SetCurrentItem(_preparedItems[_inspectionCursor]);
                    _visionOffset = _currentItem != null ? _currentItem.VisionOffset : null;

                    int applyResult = ApplyInputDieVisionOffset();
                    if (applyResult != 0)
                        return applyResult;
                }

                ClearCurrentContext();
                completed = true;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-RESULT-EX", "Vision",
                    "Input die vision Result 회수 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (!completed)
                    ReleasePreparedReservationsIfNeeded();
            }
        }

        private static int ResolveInputVisionRequestIndex(InputDieVisionPreparedItem item)
        {
            if (item != null && item.PickTarget != null && item.PickTarget.OrderIndex >= 0)
                return item.PickTarget.OrderIndex + 1;

            return item != null && item.PickerNo > 0 ? item.PickerNo : 1;
        }

        private int SkipCurrentVisionFailedDieAndContinue(int retryCount)
        {
            try
            {
                string dieId = _currentDieId ?? string.Empty;
                int pickerNo = _currentPickerNo;

                MaterialStateService.ReleaseInputStagePickReservation(dieId, PickerLocationKind, pickerNo);
                MaterialStateService.RemoveInspection(dieId, "InputPickVision");

                string message;
                bool syncOk = MaterialStateService.ApplyManualDieState(
                    dieId,
                    false,
                    DieResult.Unknown,
                    0,
                    "",
                    "InputDieVisionPrepareVisionNgSkip",
                    out message);
                if (!syncOk)
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-VISION-SKIP-FAIL", "Material",
                        "Input die vision 실패 Die SKIP 처리에 실패했습니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", message=" + message);
                }

                if (_currentItem != null)
                    _preparedItems.Remove(_currentItem);

                WriteLog("InputDieVisionPrepareSequence",
                    Name + " Input die vision 검사 실패 Die를 SKIP 처리하고 다음 Die로 진행합니다. " +
                    "die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", retryCount=" + retryCount + " - Ok");

                ReleasePreInspectionInputStageArea();
                ClearCurrentContext();
                CurrentStep = InputDieVisionPrepareStep.SelectNextInspectionTarget;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-VISION-SKIP-EX", "Material",
                    "Input die vision 실패 Die SKIP 처리 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
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

                bool lastPreparedDie = _inspectionCursor == _preparedItems.Count - 1;
                if (lastPreparedDie)
                {
                    // Wafer 채널 라이브 Delta는 카메라 순수 오프셋(raw)이므로 InputToBottomOffset 감산 없이 그대로 전파한다.
                    double pendingMapOffsetX = _visionOffset.DeltaX;
                    double pendingMapOffsetY = -_visionOffset.DeltaY;
                    string limitReason;
                    if (stage != null &&
                        !stage.IsManualDieDetectOffsetWithinLimit(pendingMapOffsetX, pendingMapOffsetY, out limitReason))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-PENDING-OFFSET-LIMIT", "Material",
                            "마지막 Input Vision 보정값이 허용 범위를 벗어나 미촬영 Die 좌표에 적용할 수 없습니다. " +
                            "referenceDie=" + _currentDieId +
                            ", offsetX=" + pendingMapOffsetX.ToString("F6") +
                            ", offsetY=" + pendingMapOffsetY.ToString("F6") +
                            ", reason=" + limitReason);
                    }

                    int updatedCount;
                    int skippedCount;
                    string updateDetail;
                    if (!MaterialStateService.TryApplyLastVisionOffsetToPendingInputDies(
                        _currentDieId,
                        pendingMapOffsetX,
                        pendingMapOffsetY,
                        "InputLastPreparedVisionOffset:" + _currentDieId,
                        out updatedCount,
                        out skippedCount,
                        out updateDetail))
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-PENDING-OFFSET-APPLY", "Material", updateDetail);
                    }

                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " 예약 배치 마지막 촬영 결과를 아직 촬영하지 않은 Die 좌표에 적용했습니다. " +
                        "referenceDie=" + _currentDieId +
                        ", visionDeltaX=" + _visionOffset.DeltaX.ToString("F6") +
                        ", visionDeltaY=" + _visionOffset.DeltaY.ToString("F6") +
                        ", appliedOffsetX=" + pendingMapOffsetX.ToString("F6") +
                        ", appliedOffsetY=" + pendingMapOffsetY.ToString("F6") +
                        ", updated=" + updatedCount +
                        ", skipped=" + skippedCount + " - Ok");
                }

                // 조기 허가 경로 구분용: 기록/전파까지 완료된 항목 표시 — 픽업 측 회수 루프가 중복 적용하지 않게 한다.
                if (_currentItem != null)
                    _currentItem.VisionOffsetApplied = true;

                SaveCurrentStateToItem();
                _inspectionCursor++;
                ReleasePreInspectionInputStageArea();
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

        private async Task<int> WaitFrontRearPickerInputZonesClearBeforeVisionAsync(CancellationToken ct)
        {
            try
            {
                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".InputVisionPickerZoneWait");

                    string frontDetail;
                    bool frontBlocking = IsPickerInputZoneBusyForVision(true, out frontDetail);
                    string rearDetail;
                    bool rearBlocking = IsPickerInputZoneBusyForVision(false, out rearDetail);

                    if (!frontBlocking && !rearBlocking)
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputVision 검사 전 Front/Rear Picker Input 영역 이탈 확인 완료. " +
                                "die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");
                        }

                        return 0;
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputVision 검사 전 Picker Input 영역 이탈 대기. " +
                            "die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", frontBlocking=" + frontBlocking +
                            ", frontDetail=" + frontDetail +
                            ", rearBlocking=" + rearBlocking +
                            ", rearDetail=" + rearDetail + " - Wait");
                        waitLogged = true;
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        if (IsInputCameraPreInspectionMode())
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputCamera 선행검사 모드: Picker가 Input 영역에서 이탈할 때까지 계속 대기합니다. " +
                                "die=" + _currentDieId +
                                ", pickerNo=" + _currentPickerNo +
                                ", elapsedMs=" + elapsedMs.ToString("0") +
                                ", timeoutMs=" + timeoutMs +
                                ", frontBlocking=" + frontBlocking +
                                ", frontDetail=" + frontDetail +
                                ", rearBlocking=" + rearBlocking +
                                ", rearDetail=" + rearDetail + " - Wait");
                            start = DateTime.UtcNow;
                            continue;
                        }

                        return Fail("INPUT-DIE-VISION-PREPARE-PICKER-INPUT-ZONE-TIMEOUT", Name,
                            "InputVision 검사 전 Picker가 Input 영역에서 완전히 이탈하지 못했습니다. " +
                            "die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", frontBlocking=" + frontBlocking +
                            ", frontDetail=" + frontDetail +
                            ", rearBlocking=" + rearBlocking +
                            ", rearDetail=" + rearDetail);
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
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
                return Fail("INPUT-DIE-VISION-PREPARE-PICKER-INPUT-ZONE-EX", Name,
                    "InputVision 검사 전 Picker Input 영역 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsPickerInputZoneBusyForVision(bool isFront, out string detail)
        {
            detail = string.Empty;

            try
            {
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    Context != null ? Context.Machine : null,
                    isFront,
                    PickerWorkZone.Input,
                    null,
                    "InputVision 검사 전 Picker Input 영역 확인");

                bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
                bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
                bool inputRelated =
                    state != null &&
                    (state.CurrentZone == PickerWorkZone.Input ||
                     state.TargetZone == PickerWorkZone.Input ||
                     state.BlocksTransport ||
                     state.UnknownUnsafe);

                bool movingInputRisk = IsPickerInputZoneMotionRisk(state, xMoving, yMoving);
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
                         " Input 영역 확인 실패. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private static bool IsPickerInputZoneMotionRisk(PickerZoneTransportState state, bool xMoving, bool yMoving)
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

                    string frontDetail;
                    bool frontBlocking = IsPickerInputZoneBusyForVision(true, out frontDetail);
                    string rearDetail;
                    bool rearBlocking = IsPickerInputZoneBusyForVision(false, out rearDetail);

                    if (!frontBlocking && !rearBlocking)
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
                            ", frontBlocking=" + frontBlocking +
                            ", frontDetail=" + frontDetail +
                            ", rearBlocking=" + rearBlocking +
                            ", rearDetail=" + rearDetail + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
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

        private async Task<int> WaitInputCameraPickUpPermissionsClearForPreInspectionAsync(CancellationToken ct)
        {
            if (!IsInputCameraPreInspectionMode())
                return 0;

            try
            {
                // FIFO/데드락 방어선: '아무 side나'(HasAnyPermission)가 아니라 '나보다 앞선 다른
                // side 허가'(HasForeignPermission)만 대기 조건으로 본다. 카메라 존 획득 게이트
                // (WaitUntilNoForeignPickUpPermissionAsync)가 이미 foreign을 배제하고 존을 잡았으므로
                // 정상 흐름에서 이 대기는 즉시 통과하는 belt-and-suspenders다. 무한 대기 대신
                // bounded timeout(ResolveTimeout)을 걸어 무언정지 대신 복구 가능한 알람으로 전환한다.
                bool waitLogged = false;
                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".InputCameraPickUpPermissionClear");

                    string pendingPermissionDetail;
                    if (!InputCameraPickUpPermissionStore.HasForeignPermission(Side, out pendingPermissionDetail))
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputCamera 선행검사 이동 대기 종료. 앞선 PickUp 허가가 소비되어 InputVisionX 이동이 가능합니다. " +
                                "die=" + _currentDieId +
                                ", pickerNo=" + _currentPickerNo +
                                ", side=" + Side + " - Ok");
                        }

                        return 0;
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputCamera 선행검사 이동을 보류합니다. 앞선 PickUp 허가가 발급되어 InputVisionX는 Avoid 위치를 유지해야 합니다. " +
                            "pendingPermission=" + pendingPermissionDetail +
                            ", die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", side=" + Side + " - Wait");
                        waitLogged = true;
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("INPUT-DIE-VISION-PREPARE-PERMISSION-CLEAR-TIMEOUT", Name,
                            "InputCamera 선행검사 이동 전 앞선 PickUp 허가 대기가 제한 시간을 초과했습니다. " +
                            "die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", side=" + Side +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", pendingPermission=" + pendingPermissionDetail);
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
                return Fail("INPUT-DIE-VISION-PREPARE-PERMISSION-CLEAR-EX", Name,
                    "InputCamera 선행검사 이동 전 PickUp 허가 상태 확인 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
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

                    await Task.Delay(1, ct).ConfigureAwait(false);
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

        private async Task<int> EnsureWaferAlignThetaPositionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string materialReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out materialReason))
                    return Fail("INPUT-DIE-VISION-PREPARE-THETA-ALIGN", stage.Name,
                        description + " 실패. " + materialReason);

                stage.ApplyWaferAlignThetaResult(
                    wafer.InputStageAlignReferenceT,
                    wafer.InputStageAlignCorrectedT,
                    wafer.InputStageAlignOffsetT);

                string readyReason;
                if (!stage.IsWaferAlignThetaResultReady(out readyReason))
                    return Fail("INPUT-DIE-VISION-PREPARE-THETA-ALIGN", stage.Name,
                        description + " 실패. " + readyReason);

                double targetT;
                if (!stage.TryResolveWaferAlignThetaTarget(out targetT))
                    return Fail("INPUT-DIE-VISION-PREPARE-THETA-TARGET", stage.Name,
                        description + " 실패. StageT 보정 목표값을 찾을 수 없습니다.");

                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferT, targetT))
                    return 0;

                int result = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    targetT,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    targetT,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferT, targetT, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-THETA-POS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 확인/복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureEjectPinZAvoidForStageTravelAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.EjectPinZ == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-EJECTPINZ-RECIPE", stage.Name,
                        description + " 전 EjectPinZ 대기(Avoid) 위치 정보가 없습니다.");

                // 공정 중 NeedleZ는 공정 위치를 유지하고, 스테이지 이동 전에는 EjectPinZ만 대기(Avoid) 위치로 내린다.
                double target = stage.Recipe.EjectPinZ.AvoidPosition;
                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.EjectPinZ, target))
                {
                    if (stage.NeedleVacuum != null && stage.NeedleVacuum.IsOn)
                    {
                        stage.NeedleVacuum.Off();
                        if (stage.NeedleVacuum.IsOn)
                            return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-VAC-OFF", stage.Name,
                                description + " 전 EjectPinZ 하강을 위해 Needle Vacuum OFF를 명령했지만 출력이 계속 ON 상태입니다.");

                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " " + description + " 전 EjectPinZ 하강을 위해 Needle Vacuum을 OFF 했습니다. - Ok");
                    }

                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        target,
                        description + " EjectPinZ avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        target,
                        description + " EjectPinZ avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    target,
                    description + " EjectPinZ avoid");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-EJECTPINZ-AVOID-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 EjectPinZ 대기(Avoid) 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private VisionAlignResult SimulateInputVisionOffset()
        {
            if (IsDryRunWithVisionDisabled())
                return CreateZeroInputVisionOffset();

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

                int result = await EnsureEjectPinZAvoidForStageTravelAsync(
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
            // 비전 미사용(UseVision=false) — Wafer/Input die 비전은 비전 작업이므로 합성 결과로 통과.
            if (QMC.CDT320.AppSettingsStore.Current != null && !QMC.CDT320.AppSettingsStore.Current.UseVision)
                return true;

            if (QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                return false;

            if (Options != null && Options.SimulateVisionResult)
                return true;

            if (stage != null && stage.IsInputStageSimulationOrDryRun())
                return true;

            return IsPickerSimulationOrDryRun();
        }

        private static bool IsDryRunWithWaferVisionConnected()
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                    QMC.CDT320.VisionComm.AutoVisionChannel.Wafer);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsInputCameraPreInspectionMode()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Auto &&
                   Options.InputCameraPreInspectionMode;
        }

        private bool IsInputCameraPreInspectionPrecisionAxis(WaferStageAxis axis)
        {
            if (!IsInputCameraPreInspectionMode())
                return false;

            return axis == WaferStageAxis.VisionX ||
                   axis == WaferStageAxis.NeedleX ||
                   axis == WaferStageAxis.WaferY;
        }

        private async Task<int> AcquirePreInspectionInputStageAreaIfNeededAsync(CancellationToken ct)
        {
            try
            {
                if (!IsInputCameraPreInspectionMode())
                    return 0;

                if (_preInspectionInputStageLease != null)
                    return 0;

                _preInspectionInputStageLease = await AcquireResourceAsync(
                    SequenceResourceKind.InputStageArea,
                    Name + ":InputCameraPreInspection:" + _currentDieId,
                    ct).ConfigureAwait(false);

                if (_preInspectionInputStageLease == null)
                    return -1;

                WriteLog("InputDieVisionPrepareSequence",
                    Name + " InputCamera 선행검사 실제 Stage/Vision 이동을 위해 InputStageArea를 점유했습니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", side=" + Side + " - Ok");
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
                return Fail("INPUT-DIE-VISION-PREPARE-PRE-INSPECTION-RESOURCE-EX", Name,
                    "InputCamera 선행검사 InputStageArea 점유 중 예외가 발생했습니다. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
        }

        private void ReleasePreInspectionInputStageArea()
        {
            try
            {
                if (_preInspectionInputStageLease == null)
                    return;

                _preInspectionInputStageLease.Dispose();
                _preInspectionInputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("InputDieVisionPrepareSequence",
                    Name + " InputCamera 선행검사 InputStageArea 해제 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
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
                double currentNeedleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : ResolveNeedleXForVisionX(stage, currentX);
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
                    int result = await MoveNeedleXAndStageYForVisionPrepareAsync(
                        stage,
                        targetNeedleX,
                        targetY,
                        targetX,
                        description + " NeedleX/StageY",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string xFirstReason;
                // 현재 기준: 경로 판단은 CameraX가 아니라 NeedleX/StageY 실축 조합으로 확인한다.
                bool canMoveXFirst = stage.IsNeedleWorkPointInArea(targetNeedleX, currentY, out xFirstReason);
                if (canMoveXFirst)
                {
                    int result = await MoveInputVisionXAndVerifyAsync(
                        stage,
                        targetX,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveNeedleXAndStageYForVisionPrepareAsync(
                        stage,
                        targetNeedleX,
                        targetY,
                        targetX,
                        description + " NeedleX/StageY",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string yFirstReason;
                bool canMoveYFirst = stage.IsNeedleWorkPointInArea(currentNeedleX, targetY, out yFirstReason);
                if (canMoveYFirst)
                {
                    int result = await MoveNeedleXAndStageYForVisionPrepareAsync(
                        stage,
                        targetNeedleX,
                        targetY,
                        currentX,
                        description + " NeedleX/StageY",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputVisionXAndVerifyAsync(
                        stage,
                        targetX,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                int entryResult = await MoveInputStageVisionPointViaWorkCenterAsync(
                    stage,
                    targetX,
                    targetY,
                    targetNeedleX,
                    description,
                    xFirstReason,
                    yFirstReason,
                    ct).ConfigureAwait(false);
                if (entryResult != int.MinValue)
                    return entryResult;

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

        private async Task<int> MoveInputStageVisionPointViaWorkCenterAsync(
            InputStageUnit stage,
            double targetX,
            double targetY,
            double targetNeedleX,
            string description,
            string xFirstReason,
            string yFirstReason,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return int.MinValue;

                double entryX = stage.ResolveWorkAreaCenterX();
                double entryY = stage.ResolveWorkAreaCenterY();

                string entryReason;
                if (!stage.IsInputStageWorkPointInArea(entryX, entryY, out entryReason))
                {
                    WriteLog("InputDieVisionPrepareSequence",
                        description + " work area center entry is not available. entryX=" + entryX.ToString("F6") +
                        ", entryY=" + entryY.ToString("F6") +
                        ", reason=" + entryReason +
                        ", xFirst=" + xFirstReason +
                        ", yFirst=" + yFirstReason + " - Skip");
                    return int.MinValue;
                }

                WriteLog("InputDieVisionPrepareSequence",
                    description + " has no direct L-path. Enter work center first. entryX=" + entryX.ToString("F6") +
                    ", entryY=" + entryY.ToString("F6") +
                    ", targetX=" + targetX.ToString("F6") +
                    ", targetY=" + targetY.ToString("F6") +
                    ", xFirst=" + xFirstReason +
                    ", yFirst=" + yFirstReason + " - Start");

                int result = await MoveInputStageYAndVerifyAsync(
                    stage,
                    entryX,
                    entryY,
                    description + " Entry StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputVisionXAndVerifyAsync(
                    stage,
                    targetX,
                    description + " Entry VisionX",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveNeedleXAndStageYForVisionPrepareAsync(
                    stage,
                    targetNeedleX,
                    targetY,
                    targetX,
                    description + " Entry NeedleX/StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-ENTRY-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 작업영역 진입 경유 이동 중 예외가 발생했습니다. error=" + ex.Message);
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

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, visionTarget))
                {
                    int visionResult = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        visionTarget,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (visionResult != 0)
                        return Fail("INPUT-DIE-VISION-PREPARE-STAGE-XY-VISION", stage != null ? stage.Name : "InputStageUnit",
                            description + " VisionX 이동 명령 실패. result=" + visionResult +
                            ", " + BuildInputStageAxisState(stage, WaferStageAxis.VisionX, visionTarget));

                    visionResult = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        visionTarget,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (visionResult != 0)
                        return visionResult;
                }

                int result = await MoveNeedleXAndStageYForVisionPrepareAsync(
                    stage,
                    needleTarget,
                    stageYTarget,
                    visionTarget,
                    description,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-XY-ORDER-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 순서 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXAndStageYForVisionPrepareAsync(
            InputStageUnit stage,
            double needleTarget,
            double stageYTarget,
            double workAreaVisionX,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-STAGE-NO-UNIT", "InputStageUnit",
                        description + " 이동 중 InputStageUnit이 없습니다.");

                bool needleInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, needleTarget);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, stageYTarget);
                if (needleInPosition && stageYInPosition)
                    return 0;

                bool moveNeedleXFirst;
                string reason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(needleTarget, stageYTarget, out moveNeedleXFirst, out reason))
                {
                    return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-STAGE-PATH", stage.Name,
                        description + " 이동 가능한 NeedleX/StageY 순서를 찾지 못했습니다. " + reason);
                }

                if (moveNeedleXFirst)
                {
                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXAndVerifyAsync(
                            stage,
                            needleTarget,
                            description + " NeedleX",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }

                    if (!stageYInPosition)
                    {
                        int result = await MoveInputStageYAndVerifyAsync(
                            stage,
                            workAreaVisionX,
                            stageYTarget,
                            description + " StageY",
                            ct,
                            needleTarget).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
                }
                else
                {
                    if (!stageYInPosition)
                    {
                        int result = await MoveInputStageYAndVerifyAsync(
                            stage,
                            workAreaVisionX,
                            stageYTarget,
                            description + " StageY",
                            ct,
                            needleTarget).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }

                    if (!needleInPosition)
                    {
                        int result = await MoveNeedleXAndVerifyAsync(
                            stage,
                            needleTarget,
                            description + " NeedleX",
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-DIE-VISION-PREPARE-NEEDLE-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " NeedleX/StageY 이동 순서 처리 중 예외가 발생했습니다. error=" + ex.Message);
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
                    // C1(return-follow): 게이트+제약(목표가 피커 페어 간격 미충족)이면 퇴장 피커를
                    // 선행축으로 추종 진입한다. 존이 이미 비어 제약이 없으면(2번째 이후 촬영 포함)
                    // follow 없이 기존 일반 이동. follow 실패 시 R5 폴백 — 기존 경로(공유레일/가드
                    // 클리어 대기 + 일반 이동)로 1회 재시도(아래 기존 코드가 그 재시도 경로다).
                    if (ShouldFollowPickerForInputVisionEntry(stage, target))
                    {
                        int followEntryResult = await TryFollowInputVisionXBehindPickerAsync(
                            stage,
                            target,
                            description,
                            ct).ConfigureAwait(false);
                        if (followEntryResult == 0)
                            return CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, target, description);

                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " " + description + " VisionX 팔로잉 진입이 실패해 기존 대기+일반 이동으로 재시도합니다. " +
                            "followResult=" + followEntryResult + " - Check");
                    }

                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    if (!IsInputCameraPreInspectionPrecisionAxis(WaferStageAxis.VisionX))
                    {
                        result = await WaitInputStageAxisInPositionResultAsync(
                            stage,
                            WaferStageAxis.VisionX,
                            target,
                            description,
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
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

        // C1 게이트(return-follow): Auto + PickUp TransferMotionMode가 Conti 계열일 때만
        // 존 대기 생략+VisionX 팔로잉 진입을 적용한다. 미충족 시 기존 경로 완전 무변경.
        // (InputCameraMarkInspectionSequence.IsMinimalRetreatGateSatisfied 선례 미러)
        private bool IsVisionReturnFollowGateSatisfied()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return false;

            PickerPickUpMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                config = FrontPicker.Config.PickUp;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                config = RearPicker.Config.PickUp;

            if (config == null)
                return false;

            return config.TransferMotionMode == PickerPickUpTransferMotionMode.ContiSegmentedPickUp ||
                   config.TransferMotionMode == PickerPickUpTransferMotionMode.FastContiSegmentedPickUp;
        }

        // C1/R1-①-3(return-follow): 스테이지 평면 이동(StageY/NeedleX) 목표에 대한 MotionGuard 전체
        // dry-run(CanAxisTeachingMove — VerifyPickerZAxesAvoidWhenInputRisk/EjectPinZ zero-avoid/
        // 작업영역 규칙 포함)을 폴링 대기한다. 기존 존 클리어 대기의 X존 소속 조건만 follow로
        // 이관하고, 나머지 보호 대상은 동일한 fail-safe(대기) 의미를 유지한다.
        // 타임아웃/선행검사 무한 대기 규칙은 기존 존 대기와 동일.
        private async Task<int> WaitInputStagePlaneGuardClearBeforeVisionOverlapAsync(
            InputStageUnit stage,
            double targetNeedleX,
            double targetY,
            CancellationToken ct)
        {
            try
            {
                BaseAxis stageY = stage != null ? stage.StageY : null;
                BaseAxis needleX = stage != null ? stage.NeedleBlockX : null;
                if (stageY == null && needleX == null)
                    return 0;

                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".InputVisionOverlapStageGuardWait");

                    string stageYReason = string.Empty;
                    string needleXReason = string.Empty;
                    bool stageYClear = stageY == null ||
                        MotionGuardRuntime.CanAxisTeachingMove(
                            stageY, targetY, "InputDieVisionPrepareOverlap;StageY", out stageYReason);
                    bool needleXClear = needleX == null ||
                        MotionGuardRuntime.CanAxisTeachingMove(
                            needleX, targetNeedleX, "InputDieVisionPrepareOverlap;NeedleX", out needleXReason);

                    if (stageYClear && needleXClear)
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " 팔로잉 게이트: StageY/NeedleX 이동 안전 전제 확인 완료. " +
                                "die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");
                        }

                        return 0;
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " 팔로잉 게이트: StageY/NeedleX 이동 안전 전제 대기. " +
                            "die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", stageYReason=" + stageYReason +
                            ", needleXReason=" + needleXReason + " - Wait");
                        waitLogged = true;
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        if (IsInputCameraPreInspectionMode())
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputCamera 선행검사 모드: StageY/NeedleX 안전 전제가 확보될 때까지 계속 대기합니다. " +
                                "die=" + _currentDieId +
                                ", elapsedMs=" + elapsedMs.ToString("0") +
                                ", stageYReason=" + stageYReason +
                                ", needleXReason=" + needleXReason + " - Wait");
                            start = DateTime.UtcNow;
                            continue;
                        }

                        return Fail("INPUT-DIE-VISION-PREPARE-STAGE-GUARD-TIMEOUT", Name,
                            "StageY/NeedleX 이동 안전 전제(MotionGuard dry-run)가 확보되지 않았습니다. " +
                            "die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", stageYReason=" + stageYReason +
                            ", needleXReason=" + needleXReason);
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
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
                return Fail("INPUT-DIE-VISION-PREPARE-STAGE-GUARD-EX", Name,
                    "StageY/NeedleX 이동 안전 전제 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        // C1(return-follow): 촬영 목표가 양 피커의 Actual/Command 페어 간격을 이미 만족하면
        // 존이 빈 상태 — follow 없이 기존 일반 이동을 쓴다(2번째 이후 촬영 자연 일반화).
        private bool ShouldFollowPickerForInputVisionEntry(InputStageUnit stage, double target)
        {
            try
            {
                if (!IsVisionReturnFollowGateSatisfied())
                    return false;
                if (stage == null || !(stage.CameraX is AjinAxis))
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    Context != null ? Context.Machine : null);
                if (service == null)
                    return false;

                BaseAxis frontX = Context.Machine.PickerFrontUnit != null ? Context.Machine.PickerFrontUnit.PickerX : null;
                BaseAxis rearX = Context.Machine.PickerRearUnit != null ? Context.Machine.PickerRearUnit.PickerX : null;
                string detail;
                bool frontClear = frontX == null ||
                    (service.IsPairClearanceSatisfied(frontX, frontX.ActualPosition, stage.CameraX, target, out detail) &&
                     service.IsPairClearanceSatisfied(frontX, frontX.CommandPosition, stage.CameraX, target, out detail));
                bool rearClear = rearX == null ||
                    (service.IsPairClearanceSatisfied(rearX, rearX.ActualPosition, stage.CameraX, target, out detail) &&
                     service.IsPairClearanceSatisfied(rearX, rearX.CommandPosition, stage.CameraX, target, out detail));

                return !(frontClear && rearClear);
            }
            catch
            {
                return false;
            }
        }

        // C1/R2(return-follow): 제약이 되는 선행 피커 선택 — Input(+방향 진입)은 페어 간격식
        // (피커 + HomeClearance − 비전)에서 X가 작은 피커가 여유가 작아 제약이 된다.
        private BaseAxis ResolveConstrainingPickerXForInputVisionEntry()
        {
            BaseAxis frontX = Context != null && Context.Machine != null && Context.Machine.PickerFrontUnit != null
                ? Context.Machine.PickerFrontUnit.PickerX
                : null;
            BaseAxis rearX = Context != null && Context.Machine != null && Context.Machine.PickerRearUnit != null
                ? Context.Machine.PickerRearUnit.PickerX
                : null;
            if (frontX == null)
                return rearX;
            if (rearX == null)
                return frontX;

            return frontX.ActualPosition <= rearX.ActualPosition ? frontX : rearX;
        }

        // C1/R2/R4(return-follow): InputVisionX(후행)가 퇴장하는 제약 피커X(선행)를 추종해 촬영
        // 위치로 진입한다. 판단 로직 없이 항상 follow 시도(정지 선행축 포함) — follow는 여유
        // (간격−safetyGap) ≤ 0이면 명령 없이 대기하고, 피커가 끝내 안 움직이면 타임아웃(-21) →
        // R5 폴백. 역방향 판단도 하지 않는다: 피커가 비전 쪽으로 접근하는 이동은 피커 자신의
        // 인터락(SafetyDistance)이 차단하고, follow는 후행축을 전진만 시키므로 간격 하한은
        // 인터락이 보장한다. 선택되지 않은 다른 피커와의 충돌은 follow 내부 이동/오버라이드가
        // MotionGuard(SharedRailX 페어 간격 포함)를 통과하며 검증된다 — 위반 시도는 -11 → R5 폴백.
        // 간격 공식 정합: direction=+1 → 거리 = (선행 + homeGap) − 후행 = 피커 + HomeClearance − 비전
        // (기존 페어 간격식과 동일). homeGap/safetyGap/direction/timeout은 설정 런타임 조회.
        private async Task<int> TryFollowInputVisionXBehindPickerAsync(
            InputStageUnit stage,
            double target,
            string description,
            CancellationToken ct)
        {
            AjinAxis followVisionX = stage != null ? stage.CameraX as AjinAxis : null;
            BaseAxis leadingPickerX = ResolveConstrainingPickerXForInputVisionEntry();
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (followVisionX == null || leadingPickerX == null || service == null)
                return -1;

            int direction;
            double homeGap;
            double safetyGap;
            string gapDetail;
            if (!service.TryGetFollowGapParameters(
                stage.CameraX,
                leadingPickerX,
                service.Config != null ? service.Config.InputVisionRetreatExtraClearance : 40.0,
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                WriteLog("InputDieVisionPrepareSequence",
                    Name + " VisionX 팔로잉 파라미터 조회에 실패해 기존 경로로 진행합니다. " +
                    "detail=" + gapDetail + " - Check");
                return -1;
            }

            int timeoutMs = service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000;
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                stage.CameraX.Config != null ? stage.CameraX.Config.DefaultVelocity : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.CameraX.Config != null ? stage.CameraX.Config.Acceleration : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.CameraX.Config != null ? stage.CameraX.Config.Deceleration : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.DefaultVelocity : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.Acceleration : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.Deceleration : 0.0);

            WriteLog("InputDieVisionPrepareSequence",
                Name + " " + description + " VisionX 팔로잉 진입을 시작합니다. leading=" + leadingPickerX.Name +
                ", leadingCommand=" + leadingPickerX.CommandPosition.ToString("F6") +
                ", visionTarget=" + target.ToString("F6") +
                ", " + gapDetail +
                ", timeoutMs=" + timeoutMs + " - Start");

            // 선행 목표는 퇴장 목표를 모르므로 현재 Command(정보용)를 사용한다(스펙 확정).
            return await followVisionX.FollowMoveAsync(
                leadingPickerX,
                leadingPickerX.CommandPosition,
                leadingVelocity,
                leadingAcceleration,
                leadingDeceleration,
                target,
                trailingVelocity,
                trailingAcceleration,
                trailingDeceleration,
                direction,
                safetyGap,
                homeGap,
                timeoutMs,
                ct).ConfigureAwait(false);
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

                if (!IsInputCameraPreInspectionPrecisionAxis(WaferStageAxis.NeedleX))
                {
                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.NeedleX,
                        target,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

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
            CancellationToken ct,
            double? workAreaNeedleX = null)
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
                        ct,
                        workAreaNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    if (!IsInputCameraPreInspectionPrecisionAxis(WaferStageAxis.WaferY))
                    {
                        result = await WaitInputStageAxisInPositionResultAsync(
                            stage,
                            WaferStageAxis.WaferY,
                            target,
                            description,
                            ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                    }
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
            CancellationToken ct,
            double? workAreaNeedleX = null)
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
                        "InputDieVisionPrepare",
                        workAreaNeedleX,
                        IsInputCameraPreInspectionPrecisionAxis(WaferStageAxis.WaferY)),
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
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                string guardTargetName = "AutoInputDieVisionPrepare;Side=" + Side + ";" + axis + ";" + description;
                bool forceMove = IsInputCameraPreInspectionPrecisionAxis(axis);
                if (axis == WaferStageAxis.VisionX)
                {
                    bool preInspection = IsInputCameraPreInspectionMode();

                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        // 현재 기준: InputVisionX 이동 전 SharedRailX 거리와 MotionGuard 조건을 먼저 확인한다.
                        int sharedRailWait = await WaitInputVisionXSharedRailClearAsync(
                            stage,
                            item,
                            target,
                            guardTargetName,
                            description,
                            ct).ConfigureAwait(false);
                        if (sharedRailWait != 0)
                            return sharedRailWait;

                        using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                        {
                            result = await AwaitStepWithCancellationAsync(
                                stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove, forceMove),
                                ct).ConfigureAwait(false);
                        }

                        // 현재 기준: SharedRailX/인터락 차단(-11)은 대기 조건이 아니므로 즉시 알람 처리한다.
                        if (result != -11)
                            break;

                        return Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE-INTERLOCK", stage.Name,
                            description + " 이동 명령이 인터락(-11)에 차단되었습니다. 대기 조건이 아니므로 알람 처리합니다. " +
                            "target=" + target.ToString("F6") +
                            ", description=" + description +
                            ", preInspection=" + preInspection +
                            ", " + BuildInputStageAxisState(stage, axis, target) +
                            PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

                        // 기존 조건 필요 여부: 사용하지 않음. 인터락(-11)을 재시도 대기로 처리하면 위험 명령이 알람 없이 반복될 수 있다.
                        // double interlockElapsedMs = (DateTime.UtcNow - interlockRetryStart).TotalMilliseconds;
                        // if (!preInspection && interlockElapsedMs >= interlockRetryTimeoutMs)
                        //     break;
                        // await Task.Delay(20, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove, forceMove),
                            ct).ConfigureAwait(false);
                    }
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

        private async Task<int> WaitInputVisionXSharedRailClearAsync(
            InputStageUnit stage,
            BaseAxis visionAxis,
            double target,
            string guardTargetName,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (stage == null || stage.CameraX == null)
                    return 0;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(Context != null ? Context.Machine : null);
                bool sharedRailApplicable = service != null && service.IsSharedRailAxis(stage.CameraX);
                BaseAxis guardAxis = visionAxis ?? stage.CameraX;
                bool preInspection = IsInputCameraPreInspectionMode();

                int timeoutMs = ResolveTimeout();
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                string reason = "";
                SequenceTrace.WaitStart("InputVisionXSharedRailClear",
                    "target=" + target.ToString("F3"),
                    "description=" + description,
                    BuildInputStageAxisState(stage, WaferStageAxis.VisionX, target));

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".InputVisionXSharedRailClear");

                    // 현재 기준: SharedRailX 거리 검사를 1순위로 보고, 명령 전 clear 될 때까지 대기한다.
                    string sharedRailReason = "";
                    bool sharedRailClear = !sharedRailApplicable ||
                        service.VerifySingleAxisMove(stage.CameraX, target, out sharedRailReason);

                    string guardReason = "";
                    bool guardClear = sharedRailClear &&
                        MotionGuardRuntime.CanAxisTeachingMove(guardAxis, target, guardTargetName, out guardReason);

                    if (sharedRailClear && guardClear)
                        break;

                    reason = !sharedRailClear
                        ? "SharedRailX: " + sharedRailReason
                        : "MotionGuard: " + guardReason;

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        // 현재 기준: 선행검사는 SharedRailX 거리와 MotionGuard 협조 조건이 clear 될 때까지 대기한다.
                        if (preInspection)
                        {
                            WriteLog("InputDieVisionPrepareSequence",
                                Name + " InputCamera 선행검사 모드: SharedRailX/MotionGuard 대기 조건이 풀릴 때까지 계속 대기합니다. " +
                                "target=" + target.ToString("F6") +
                                ", description=" + description +
                                ", elapsedMs=" + elapsedMs.ToString("0") +
                                ", timeoutMs=" + timeoutMs +
                                ", reason=" + reason + " - Wait");
                            start = DateTime.UtcNow;
                            waitLogged = true;
                            await Task.Delay(20, ct).ConfigureAwait(false);
                            continue;
                        }

                        SequenceTrace.WaitEnd("InputVisionXSharedRailClear",
                            -1,
                            "status=Timeout",
                            "elapsedMs=" + elapsedMs.ToString("0"),
                            "timeoutMs=" + timeoutMs,
                            "reason=" + reason);
                        string timeoutAlarmCode = !sharedRailClear
                            ? "INPUT-DIE-VISION-PREPARE-SHARED-RAIL-X-TIMEOUT"
                            : "INPUT-DIE-VISION-PREPARE-MOTION-GUARD-TIMEOUT";
                        return Fail(timeoutAlarmCode, stage.Name,
                            description + " InputVisionX 이동 전 SharedRailX/MotionGuard 대기 시간 초과. " +
                            "target=" + target.ToString("F6") +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", reason=" + reason);
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputDieVisionPrepareSequence",
                            Name + " InputVisionX 이동 전 SharedRailX/MotionGuard 대기. " +
                            "target=" + target.ToString("F6") +
                            ", description=" + description +
                            ", reason=" + reason + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    WriteLog("InputDieVisionPrepareSequence",
                        Name + " InputVisionX 이동 전 SharedRailX/MotionGuard 대기 완료. " +
                        "target=" + target.ToString("F6") +
                        ", elapsedMs=" + elapsedMs.ToString("0") + " - Ok");
                }

                SequenceTrace.WaitEnd("InputVisionXSharedRailClear",
                    0,
                    "status=Clear",
                    "elapsedMs=" + ((DateTime.UtcNow - start).TotalMilliseconds).ToString("0"),
                    "target=" + target.ToString("F3"));
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
                return Fail("INPUT-DIE-VISION-PREPARE-SHARED-RAIL-X-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " SharedRailX clearance 확인 중 예외가 발생했습니다. error=" + ex.Message);
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

                int waitCode = await stage.WaitInputStageAxisInPositionResult(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);

                if (waitCode != 0)
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE", stage.Name,
                        description + " 이동/InPosition 대기 실패. waitCode=" + waitCode +
                        ". " + BuildInputStageAxisState(stage, axis, target));

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

                double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.05;
                // 기존 조건: AxisMoveWaiter.IsMoveCompletedAtTarget — 현재 기준: 동일 공식의
                // BaseAxis.IsAtTargetPosition(정지+무알람+서보ON+Actual/Command 톨러런스)로 통일(R2).
                if (IsInputCameraPreInspectionPrecisionAxis(axis) &&
                    !item.IsAtTargetPosition(target, tolerance))
                    return Fail("INPUT-DIE-VISION-PREPARE-STAGE-POSITION", stage.Name,
                        description + " 최종 강한 완료 확인 실패. " + BuildInputStageAxisState(stage, axis, target));

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
            double offset = ResolveNeedleCalibrationOffsetX();
            return visionX + visionOffsetX - offset;
        }

        private double ResolveNeedleCalibrationOffsetX()
        {
            if (Context == null ||
                Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null ||
                Context.Machine.VisionUnit.Config.CalibrationData == null ||
                Context.Machine.VisionUnit.Config.CalibrationData.Needle == null ||
                !Context.Machine.VisionUnit.Config.CalibrationData.Needle.Valid)
                return 0.0;

            return Context.Machine.VisionUnit.Config.CalibrationData.Needle.NeedleXToVisionXOffset;
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
                case WaferStageAxis.WaferT:
                    return stage.StageT;
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
                double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.05;

                // 현장(F3 선행검사 정밀축): 소수 3자리 스킵 판정 유지. 일반 경로는 AxisMoveWaiter 제거에 따라
                // BaseAxis.IsAtTargetPosition(동일 공식)으로 통일(R2).
                if (IsInputCameraPreInspectionPrecisionAxis(axis))
                    return PickerInputStageMoveHelper.CanSkipInputCameraPreInspectionMoveAtThreeDecimals(
                        item,
                        target,
                        tolerance);

                return item != null && item.IsAtTargetPosition(target, tolerance);
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
