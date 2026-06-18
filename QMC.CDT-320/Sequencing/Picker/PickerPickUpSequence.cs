using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerPickUpSequence : PickerSequenceBase<PickerPickUpStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

        private readonly List<int> _enabledPickerIndexes = new List<int>();
        private readonly List<PickUpBatchItem> _pickBatchItems = new List<PickUpBatchItem>();
        private int _inspectionCursor;
        private int _pickCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private string _currentDieId = "";
        private InputStagePickTarget _pickTarget;
        private VisionAlignResult _visionOffset;
        private double _targetStageY;
        private double _targetPickerX;
        private double _targetPickerT;
        private double _targetPickerZ;
        private bool _diePicked;
        private SequenceResourceLease _inputStageLease;
        private PickUpBatchItem _currentBatchItem;

        private sealed class PickUpBatchItem
        {
            public int PickerIndex;
            public int PickerNo;
            public string DieId;
            public InputStagePickTarget PickTarget;
            public VisionAlignResult VisionOffset;
            public double TargetStageY;
            public double TargetPickerX;
            public double TargetPickerT;
            public double TargetPickerZ;
            public bool DiePicked;
        }

        public PickerPickUpSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.PickUp, side == PickerSequenceSide.Front ? "FrontPickerPickUpSequence" : "RearPickerPickUpSequence")
        {
            CurrentStep = PickerPickUpStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerPickUpStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ReleaseInputReservationIfNeeded();
                ReleasePickerWorkArea();
                ReleaseInputStageArea();
                CurrentStep = PickerPickUpStep.Complete;
            }
            catch
            {
            }
            finally
            {
            }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool keepCurrentState = false;

            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                {
                    ct.ThrowIfCancellationRequested();
                    int stepResult = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerPickUpStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerPickUpStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-EX", Name, "Picker pickup failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                {
                    ReleaseInputReservationIfNeeded();
                    ReleasePickerWorkArea();
                    ReleaseInputStageArea();
                }
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                // 유닛 확인
                case PickerPickUpStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // 피커 사이드 사용 확인
                case PickerPickUpStep.CheckPickerSideEnabled:
                    return Task.FromResult(CheckPickerSideEnabled());

                // 사용 피커 목록 생성
                case PickerPickUpStep.BuildEnabledPickerList:
                    return Task.FromResult(BuildEnabledPickerList());

                // 인풋 스테이지 준비 확인
                case PickerPickUpStep.CheckInputStageReady:
                    return Task.FromResult(CheckInputStageReady());

                // 전체 피커 Z로 어보이드 이동
                case PickerPickUpStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                // 이번 PickUp 사이클에서 사용할 다이 배치 예약
                case PickerPickUpStep.BuildPickBatch:
                    return Task.FromResult(BuildPickBatch());

                // 다음 비전 검사 대상 선택
                case PickerPickUpStep.SelectNextInspectionTarget:
                    return Task.FromResult(SelectNextInspectionTarget());

                // 예약한 다이 상태 재확인
                case PickerPickUpStep.VerifyReservedInputDie:
                    return Task.FromResult(VerifyReservedInputDie());

                // InputVisionX 이동 전 Front/Rear 피커 회피
                case PickerPickUpStep.MovePickersToAvoidForInputVisionMove:
                    return MovePickersToAvoidForInputVisionMoveAsync(ct);

                // 인풋 스테이지와 비전을 다이 위치로 이동
                case PickerPickUpStep.MoveInputStageAndVisionToDie:
                    return MoveInputStageAndVisionToDieAsync(ct);

                // 인풋 다이 비전 검사 요청
                case PickerPickUpStep.RequestInputDieVisionInspection:
                    return RequestInputDieVisionInspectionAsync(ct);

                // 인풋 다이 비전 오프셋 적용
                case PickerPickUpStep.ApplyInputDieVisionOffset:
                    return Task.FromResult(ApplyInputDieVisionOffset());

                // 다음 검사 대상 또는 픽업 이동 단계 선택
                case PickerPickUpStep.SelectNextInspectionTargetOrPickerMove:
                    return Task.FromResult(SelectNextInspectionTargetOrPickerMove());

                // 피커 접근 전 InputVisionX 회피
                case PickerPickUpStep.MoveInputVisionToAvoidForPickerMove:
                    return MoveInputVisionToAvoidForPickerMoveAsync(ct);

                // 검사 완료된 배치의 픽업 대상 계산
                case PickerPickUpStep.CalculatePickTargets:
                    return Task.FromResult(CalculatePickTargets());

                // 다음 픽업 대상 선택
                case PickerPickUpStep.SelectNextPickTarget:
                    return Task.FromResult(SelectNextPickTarget());

                // 피커 접근 전 반대 피커 회피
                case PickerPickUpStep.MoveOppositePickerToAvoidForPickerMove:
                    return MoveOppositePickerToAvoidForPickerMoveAsync(ct);

                // 피커 X 스테이지 Y 피커 T 이동
                case PickerPickUpStep.MovePickerXStageYPickerT:
                    return MovePickerXStageYPickerTAsync(ct);

                // 픽업 대상 검증
                case PickerPickUpStep.VerifyPickTarget:
                    return Task.FromResult(VerifyPickTarget());

                // 피커 Z 픽업 이동
                case PickerPickUpStep.MovePickerZPick:
                    return MovePickerZPickAsync(ct);

                // 진공 ON 처리
                case PickerPickUpStep.VacuumOn:
                    return VacuumOnAsync(ct);

                // 다이 픽업 검증
                case PickerPickUpStep.VerifyDiePicked:
                    return Task.FromResult(VerifyDiePicked());

                // 피커 Z로 어보이드 이동
                case PickerPickUpStep.MovePickerZToAvoid:
                    return MovePickerZToAvoidAsync(ct);

                // 자재로 피커 갱신
                case PickerPickUpStep.UpdateMaterialToPicker:
                    return Task.FromResult(UpdateMaterialToPicker());

                // 다음 픽업 대상 또는 완료 선택
                case PickerPickUpStep.SelectNextPickTargetOrComplete:
                    return Task.FromResult(SelectNextPickTargetOrComplete());

                default:
                    return Task.FromResult(Fail("PICKER-PICKUP-STEP", Name, "Unsupported picker pickup step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker == null)
                return Fail("PICKER-PICKUP-FRONT-NO-UNIT", Name, "FrontPickerUnit is null.");

            if (Side == PickerSequenceSide.Rear && RearPicker == null)
                return Fail("PICKER-PICKUP-REAR-NO-UNIT", Name, "RearPickerUnit is null.");

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-PICKUP-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            CurrentStep = PickerPickUpStep.CheckPickerSideEnabled;
            return 0;
        }

        private int CheckPickerSideEnabled()
        {
            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerPickUpSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerPickUpStep.Complete;
                return 0;
            }

            CurrentStep = PickerPickUpStep.BuildEnabledPickerList;
            return 0;
        }

        private int BuildEnabledPickerList()
        {
            _enabledPickerIndexes.Clear();
            _enabledPickerIndexes.AddRange(BuildEnabledPickerIndexes());

            if (_enabledPickerIndexes.Count == 0)
                return Fail("PICKER-PICKUP-NO-PICKER", Name, "No enabled picker was found. side=" + Side);

            WriteLog("PickerPickUpSequence",
                Name + " enabled picker order=" + string.Join(",", _enabledPickerIndexes.ConvertAll(i => i.ToString()).ToArray()) + " - Ok");

            CurrentStep = PickerPickUpStep.CheckInputStageReady;
            return 0;
        }

        private int CheckInputStageReady()
        {
            bool inputStageReady = Context != null &&
                                   Context.Bus != null &&
                                   Context.Bus.IsSet("InputStageReady");
            if (!inputStageReady)
            {
                return Fail("PICKER-PICKUP-STAGE-READY-SIGNAL", "InputStage",
                    "InputStageReady 신호가 없어 PickUp을 시작할 수 없습니다. " +
                    "InputSequence가 웨이퍼 Align/DieMapping/Finish 완료 후 InputStageReady 신호를 설정해야 합니다.");
            }

            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (wafer == null)
                return Fail("PICKER-PICKUP-NO-WAFER", "Material", "InputStage 웨이퍼 Material 정보가 없습니다.");

            string finishReason;
            if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                return Fail("PICKER-PICKUP-STAGE-NOT-FINISH", "InputStage",
                    "Picker PickUp 전에 InputStage Finish 상태가 완료되어야 합니다. " + finishReason);

            if (wafer.DieIds == null || wafer.DieIds.Count == 0)
                return Fail("PICKER-PICKUP-NO-DIE", "Material", "InputStage 웨이퍼에 Die 데이터가 없습니다. waferId=" + wafer.WaferId);

            if (!MaterialStateService.HasReadyInputStagePickTarget())
            {
                WriteLog("PickerPickUpSequence", Name + " has no ready input die target. waferId=" + wafer.WaferId + " - Check");
                CurrentStep = PickerPickUpStep.Complete;
                return 0;
            }

            CurrentStep = PickerPickUpStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
            int acquireResult = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
            if (acquireResult != 0)
                return acquireResult;

            EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUp");

            int result = await MoveAllPickerZToAvoidAndVerifyAsync("pickup pre all picker Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.BuildPickBatch;
            return 0;
        }

        private async Task<int> AcquireInputStageAreaForPickUpAsync(CancellationToken ct)
        {
            bool waitLogged = false;
            string holder = Name + ":PickUp";

            try
            {
                if (_inputStageLease != null)
                    return 0;

                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    _inputStageLease = await AcquireResourceAsync(SequenceResourceKind.InputStageArea, holder, ct).ConfigureAwait(false);
                    return _inputStageLease != null ? 0 : -1;
                }

                while (_inputStageLease == null)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".AcquireInputStageArea");

                    string currentHolder = Context != null && Context.Resources != null
                        ? Context.Resources.GetHolder(SequenceResourceKind.InputStageArea)
                        : "";

                    if (!waitLogged && !string.IsNullOrWhiteSpace(currentHolder))
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " PickUp이 InputStageArea 리소스를 기다립니다. " +
                            "현재 점유=" + currentHolder + ", 요청=" + holder + " - Wait");
                        waitLogged = true;
                    }

                    _inputStageLease = await Context.Resources
                        .AcquireAsync(SequenceResourceKind.InputStageArea, holder, 200, ct, false)
                        .ConfigureAwait(false);

                    if (_inputStageLease == null)
                        await Task.Delay(100, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp이 InputStageArea 리소스를 획득했습니다. 요청=" + holder + " - Ok");
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
                return Fail("PICKER-RESOURCE", holder,
                    "PickUp InputStageArea 리소스 획득 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int BuildPickBatch()
        {
            _pickBatchItems.Clear();
            _inspectionCursor = 0;
            _pickCursor = 0;
            ClearCurrentPickContext();

            int occupiedPickerCount = 0;
            for (int i = 0; i < _enabledPickerIndexes.Count; i++)
            {
                int pickerIndex = _enabledPickerIndexes[i];
                int pickerNo = ToPickerNo(pickerIndex);

                DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                if (loadedDie != null)
                {
                    occupiedPickerCount++;
                    WriteLog("PickerPickUpSequence",
                        Name + " picker already has die, skip pickup reservation. " +
                        "이미 Die를 가지고 있어 PickUp 예약에서 제외합니다. " +
                        "pickerNo=" + pickerNo +
                        ", pickerIndex=" + pickerIndex +
                        ", loadedDie=" + loadedDie.DieId + " - Check");
                    continue;
                }

                InputStagePickTarget target = MaterialStateService.ReserveNextInputStagePickTarget(PickerLocationKind, pickerNo);
                string dieId = target != null ? target.DieId : "";

                if (string.IsNullOrWhiteSpace(dieId))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " has no more ready input die for batch. pickerNo=" + pickerNo + " - Check");
                    continue;
                }

                PickUpBatchItem item = new PickUpBatchItem
                {
                    PickerIndex = pickerIndex,
                    PickerNo = pickerNo,
                    DieId = dieId,
                    PickTarget = target
                };
                _pickBatchItems.Add(item);

                WriteLog("PickerPickUpSequence",
                    Name + " reserved input die for batch. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", pickerIndex=" + pickerIndex +
                    ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                    ", inputVisionX=" + target.TargetX +
                    ", inputStageY=" + target.TargetY + " - Ok");
            }

            if (_pickBatchItems.Count == 0)
            {
                if (occupiedPickerCount > 0 && occupiedPickerCount >= _enabledPickerIndexes.Count)
                {
                    return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                        "사용 설정된 모든 Picker가 이미 Die를 가지고 있어 PickUp을 시작할 수 없습니다. " +
                        "먼저 검사/Place/Recover를 진행해 Picker를 비운 뒤 다시 시작하세요. " +
                        "occupiedPickerCount=" + occupiedPickerCount +
                        ", enabledPickerCount=" + _enabledPickerIndexes.Count +
                        ", side=" + Side);
                }

                WriteLog("PickerPickUpSequence", Name + " has no input die batch target. side=" + Side + " - Check");
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            WriteLog("PickerPickUpSequence",
                Name + " pick batch created. count=" + _pickBatchItems.Count +
                ", enabledPickerCount=" + _enabledPickerIndexes.Count + " - Ok");

            CurrentStep = PickerPickUpStep.SelectNextInspectionTarget;
            return 0;
        }

        private int SelectNextInspectionTarget()
        {
            if (_inspectionCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.MoveInputVisionToAvoidForPickerMove;
                return 0;
            }

            SetCurrentBatchItem(_pickBatchItems[_inspectionCursor]);

            WriteLog("PickerPickUpSequence",
                Name + " selected input die vision target. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", inspectIndex=" + (_inspectionCursor + 1) +
                "/" + _pickBatchItems.Count + " - Ok");

            CurrentStep = PickerPickUpStep.VerifyReservedInputDie;
            return 0;
        }

        private int VerifyReservedInputDie()
        {
            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            if (loadedDie != null)
            {
                return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                    "Picker가 이미 Die를 가지고 있어 예약된 Die를 PickUp할 수 없습니다. " +
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
                return Fail("PICKER-PICKUP-DIE-NOT-PICKABLE", "Material",
                    "Reserved input die is not pickable. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + reason);
            }

            CurrentStep = PickerPickUpStep.MovePickersToAvoidForInputVisionMove;
            return 0;
        }

        private async Task<int> MovePickersToAvoidForInputVisionMoveAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveCurrentPickerToAvoidAndVerifyAsync(
                    "current picker avoid before InputVisionX move",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyOppositePickerNotInInputPickArea(
                    "InputVisionX 이동 전 상대 Picker Input 영역 확인");
                if (result != 0)
                    return result;

                CurrentStep = PickerPickUpStep.MoveInputStageAndVisionToDie;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PICKER-AVOID-EX", Name,
                    "Picker avoid before InputVisionX move failed: " + ex.Message);
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
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                if (_pickTarget == null)
                    return Fail("PICKER-PICKUP-DIE-TARGET", "Material",
                        "Input die pick target is missing. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                double targetY = _pickTarget.TargetY;
                double targetX = _pickTarget.TargetX;

                string areaReason;
                if (!stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    return Fail("PICKER-PICKUP-STAGE-WORK-AREA", stage.Name,
                        "Input die target is outside input stage work area. " + areaReason);

                int result = await MoveInputStageVisionPointForPickerAsync(
                    stage,
                    targetX,
                    targetY,
                    "input die",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerPickUpStep.RequestInputDieVisionInspection;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", Name, "Input die move failed: " + ex.Message);
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
                        WriteLog("PickerPickUpSequence",
                            Name + " input die vision offset ok. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", attempt=" + attempt +
                            ", dx=" + _visionOffset.DeltaX +
                            ", dy=" + _visionOffset.DeltaY +
                            ", dt=" + _visionOffset.DeltaTheta + " - Ok");

                        CurrentStep = PickerPickUpStep.ApplyInputDieVisionOffset;
                        return 0;
                    }

                    WriteLog("PickerPickUpSequence",
                        Name + " input die vision offset retry. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", attempt=" + attempt + " - Check");
                }

                return Fail("PICKER-PICKUP-VISION-NG", "Vision", "Input die vision inspection failed. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-EX", "Vision", "Input die vision inspection exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyInputDieVisionOffset()
        {
            if (_visionOffset == null)
                return Fail("PICKER-PICKUP-VISION-OFFSET", "Vision", "Input die vision offset is missing.");

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

            SaveCurrentStateToBatchItem();
            CurrentStep = PickerPickUpStep.SelectNextInspectionTargetOrPickerMove;
            return 0;
        }

        private int SelectNextInspectionTargetOrPickerMove()
        {
            _inspectionCursor++;

            if (_inspectionCursor < _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.SelectNextInspectionTarget;
                return 0;
            }

            WriteLog("PickerPickUpSequence",
                Name + " input die vision batch completed. count=" + _pickBatchItems.Count + " - Ok");

            CurrentStep = PickerPickUpStep.MoveInputVisionToAvoidForPickerMove;
            return 0;
        }

        private async Task<int> MoveInputVisionToAvoidForPickerMoveAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                if (stage.Recipe == null)
                    return Fail("PICKER-PICKUP-STAGE-RECIPE", stage.Name, "InputStage recipe is null.");

                stage.Recipe.EnsurePositionObjects();
                double avoid = stage.Recipe.VisionX.AvoidPosition;

                if (!stage.IsVisionXInAvoidPosition())
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        avoid,
                        "InputVisionX avoid before picker move",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        avoid,
                        "InputVisionX avoid before picker move",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                int checkResult = CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.VisionX,
                    avoid,
                    "InputVisionX avoid before picker move");
                if (checkResult != 0)
                    return checkResult;

                CurrentStep = PickerPickUpStep.CalculatePickTargets;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-AVOID-EX", Name,
                    "InputVisionX avoid before picker move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CalculatePickTargets()
        {
            try
            {
                _pickCursor = 0;

                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    SetCurrentBatchItem(_pickBatchItems[i]);

                    int result = CalculateCurrentPickTarget();
                    if (result != 0)
                        return result;

                    SaveCurrentStateToBatchItem();
                }

                CurrentStep = PickerPickUpStep.SelectNextPickTarget;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-TARGET-BATCH-EX", Name,
                    "Pick target batch calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CalculateCurrentPickTarget()
        {
            try
            {
                if (_pickTarget == null)
                    return Fail("PICKER-PICKUP-DIE-TARGET", "Material",
                        "Input die pick target is missing before target calculation. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                if (_visionOffset == null)
                    return Fail("PICKER-PICKUP-VISION-OFFSET", "Vision",
                        "Input die vision offset is missing before target calculation. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);

                double inputVisionToPickerX;
                double inputVisionToPickerY;
                string offsetReason;
                if (!TryResolveInputVisionToPickerOffsets(
                    _currentPickerIndex,
                    out inputVisionToPickerX,
                    out inputVisionToPickerY,
                    out offsetReason))
                {
                    return Fail("PICKER-PICKUP-COORD-OFFSET", Name,
                        "InputVision to picker coordinate offset resolve failed. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", pickerIndex=" + _currentPickerIndex +
                        ", die=" + _currentDieId +
                        ", reason=" + offsetReason);
                }

                _targetStageY = _pickTarget.TargetY +
                    inputVisionToPickerY +
                    _visionOffset.DeltaY;
                _targetPickerX = _pickTarget.TargetX +
                    inputVisionToPickerX +
                    ResolvePickerAlignOffsetX(_currentPickerIndex) +
                    _visionOffset.DeltaX;
                _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PickPosition") +
                    ResolvePickerAlignOffsetT(_currentPickerIndex) +
                    _visionOffset.DeltaTheta;
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PickPosition");

                WriteLog("PickerPickUpSequence",
                    Name + " calculated pick target. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerT=" + _targetPickerT +
                    ", pickerZ=" + _targetPickerZ +
                    ", inputVisionX=" + _pickTarget.TargetX +
                    ", inputStageY=" + _pickTarget.TargetY +
                    ", inputVisionToPickerOffsetX=" + inputVisionToPickerX +
                    ", inputVisionToPickerOffsetY=" + inputVisionToPickerY +
                    ", visionOffsetX=" + _visionOffset.DeltaX +
                    ", visionOffsetY=" + _visionOffset.DeltaY +
                    ", visionOffsetT=" + _visionOffset.DeltaTheta + " - Ok");

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-TARGET-EX", Name, "Pick target calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int SelectNextPickTarget()
        {
            if (_pickCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            SetCurrentBatchItem(_pickBatchItems[_pickCursor]);

            WriteLog("PickerPickUpSequence",
                Name + " selected pick target. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", pickIndex=" + (_pickCursor + 1) +
                "/" + _pickBatchItems.Count + " - Ok");

            CurrentStep = PickerPickUpStep.MoveOppositePickerToAvoidForPickerMove;
            return 0;
        }

        private async Task<int> MoveOppositePickerToAvoidForPickerMoveAsync(CancellationToken ct)
        {
            int result = VerifyOppositePickerNotInInputPickArea(
                "Pick 위치 이동 전 상대 Picker Input 영역 확인");
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.MovePickerXStageYPickerT;
            return 0;
        }

        private int VerifyOppositePickerNotInInputPickArea(string description)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null)
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " 상대 Picker Input 영역 확인 생략. RearPickerUnit 없음. description=" +
                            description + " - Check");
                        return 0;
                    }

                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                    {
                        if (!RearPicker.IsRearPickerInDiePickPosition(pickerNo))
                            continue;

                        return Fail("PICKER-OPPOSITE-INPUT-ZONE", "RearPickerUnit",
                            description + " 실패. RearPicker가 Input Pick 영역에 있습니다. " +
                            "RearPicker를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo);
                    }
                }
                else
                {
                    if (FrontPicker == null)
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " 상대 Picker Input 영역 확인 생략. FrontPickerUnit 없음. description=" +
                            description + " - Check");
                        return 0;
                    }

                    for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                    {
                        if (!FrontPicker.IsFrontPickerInDiePickPosition(pickerNo))
                            continue;

                        return Fail("PICKER-OPPOSITE-INPUT-ZONE", "FrontPickerUnit",
                            description + " 실패. FrontPicker가 Input Pick 영역에 있습니다. " +
                            "FrontPicker를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo);
                    }
                }

                WriteLog("PickerPickUpSequence",
                    Name + " 상대 Picker Input 영역 확인 완료. description=" + description + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OPPOSITE-INPUT-ZONE-EX", Name,
                    description + " 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerXStageYPickerTAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                string targetName = "DiePickPosition[" + _currentPickerIndex + "]";

                string areaReason;
                if (!stage.IsInputStageWorkPointInArea(_pickTarget.TargetX, _targetStageY, out areaReason))
                {
                    return Fail("PICKER-PICKUP-CORRECTED-STAGE-WORK-AREA", stage.Name,
                        "Pick corrected StageY target is outside input stage work area. " +
                        "die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", inputVisionX=" + _pickTarget.TargetX +
                        ", targetStageY=" + _targetStageY +
                        ", reason=" + areaReason);
                }

                int result = await MoveInputStageYForPickerWorkPointCommandAsync(
                    stage,
                    _pickTarget.TargetX,
                    _targetStageY,
                    "pick corrected StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.WaferY,
                    _targetStageY,
                    "pick corrected StageY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                int finalCheck = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY");
                if (finalCheck != 0)
                    return finalCheck;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    _targetPickerX,
                    "pick corrected PickerX",
                    ct,
                    targetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    tAxis,
                    _targetPickerT,
                    "pick corrected PickerT",
                    ct,
                    targetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerPickUpStep.VerifyPickTarget;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-XYT-MOVE-EX", Name, "Pick XYT move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            if (loadedDie != null)
            {
                return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                    "Picker가 이미 Die를 가지고 있어 Z축 Pick 동작을 진행할 수 없습니다. " +
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
                return Fail("PICKER-PICKUP-DIE-NOT-PICKABLE", "Material",
                    "Reserved input die changed before pick. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + reason);
            }

            int result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX, "pick corrected PickerX");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT, "pick corrected PickerT");
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.MovePickerZPick;
            return 0;
        }

        private async Task<int> MovePickerZPickAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                _targetPickerZ,
                "pick Z down",
                ct,
                "DiePickPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.VacuumOn;
            return 0;
        }

        private async Task<int> VacuumOnAsync(CancellationToken ct)
        {
            try
            {
                SetPickerVacuum(_currentPickerNo, true);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);

                CurrentStep = PickerPickUpStep.VerifyDiePicked;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VACUUM-EX", Name, "Picker vacuum on failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyDiePicked()
        {
            _diePicked = true;
            SaveCurrentStateToBatchItem();

            MaterialStateService.UpsertInspection(_currentDieId, new DieInspectionRecord
            {
                InspectionType = "PickUp",
                Result = MaterialInspectionResult.Ok,
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "PickUp",
                        _currentPickerIndex,
                        _targetPickerX,
                        _targetStageY,
                        _targetPickerT,
                        _targetPickerZ,
                        _visionOffset != null
                            ? new VisionOffset
                            {
                                X = _visionOffset.DeltaX,
                                Y = _visionOffset.DeltaY,
                                R = _visionOffset.DeltaTheta,
                                IsValid = true
                            }
                            : new VisionOffset())
                },
                Measurements = new List<InspectionMeasurement>
                {
                    BuildBooleanMeasurement("VacuumOn", true),
                    BuildMeasurement("PickerNo", _currentPickerNo, "no", MaterialInspectionResult.Ok)
                }
            });

            CurrentStep = PickerPickUpStep.MovePickerZToAvoid;
            return 0;
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "pick Z avoid after pickup", ct, "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
            return 0;
        }

        private int UpdateMaterialToPicker()
        {
            bool materialUpdated = MaterialStateService.MarkDiePickedByPicker(_currentDieId, PickerLocationKind, _currentPickerNo);
            if (!materialUpdated)
                return Fail("PICKER-PICKUP-MATERIAL", Name, "Picked die material state update failed. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo);

            RecordColletUse(_currentPickerNo);
            SaveRuntimeState(Name + ":PickUp:ColletUse:" + _currentPickerNo);
            WriteLog("PickerPickUpSequence", Name + " picked die. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");

            if (_currentBatchItem != null)
                _currentBatchItem.DiePicked = true;

            _currentDieId = "";
            _pickTarget = null;
            _diePicked = false;
            CurrentStep = PickerPickUpStep.SelectNextPickTargetOrComplete;
            return 0;
        }

        private int SelectNextPickTargetOrComplete()
        {
            _pickCursor++;

            if (_pickCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            CurrentStep = PickerPickUpStep.SelectNextPickTarget;
            return 0;
        }

        private void SetCurrentBatchItem(PickUpBatchItem item)
        {
            _currentBatchItem = item;
            _currentPickerIndex = item != null ? item.PickerIndex : -1;
            _currentPickerNo = item != null ? item.PickerNo : 0;
            _currentDieId = item != null ? item.DieId : "";
            _pickTarget = item != null ? item.PickTarget : null;
            _visionOffset = item != null ? item.VisionOffset : null;
            _targetStageY = item != null ? item.TargetStageY : 0.0;
            _targetPickerX = item != null ? item.TargetPickerX : 0.0;
            _targetPickerT = item != null ? item.TargetPickerT : 0.0;
            _targetPickerZ = item != null ? item.TargetPickerZ : 0.0;
            _diePicked = item != null && item.DiePicked;
        }

        private void SaveCurrentStateToBatchItem()
        {
            if (_currentBatchItem == null)
                return;

            _currentBatchItem.PickerIndex = _currentPickerIndex;
            _currentBatchItem.PickerNo = _currentPickerNo;
            _currentBatchItem.DieId = _currentDieId;
            _currentBatchItem.PickTarget = _pickTarget;
            _currentBatchItem.VisionOffset = _visionOffset;
            _currentBatchItem.TargetStageY = _targetStageY;
            _currentBatchItem.TargetPickerX = _targetPickerX;
            _currentBatchItem.TargetPickerT = _targetPickerT;
            _currentBatchItem.TargetPickerZ = _targetPickerZ;
            _currentBatchItem.DiePicked = _diePicked || _currentBatchItem.DiePicked;
        }

        private void ClearCurrentPickContext()
        {
            _currentBatchItem = null;
            _currentPickerIndex = -1;
            _currentPickerNo = 0;
            _currentDieId = "";
            _pickTarget = null;
            _visionOffset = null;
            _targetStageY = 0.0;
            _targetPickerX = 0.0;
            _targetPickerT = 0.0;
            _targetPickerZ = 0.0;
            _diePicked = false;
        }

        private async Task<VisionAlignResult> RequestInputVisionOffsetAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return null;

            if (IsSimulationOrDryRun(stage))
                return SimulateInputVisionOffset();

            if (stage.Vision == null)
                return null;

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync("InputPickDie").ConfigureAwait(false);
        }

        private VisionAlignResult SimulateInputVisionOffset()
        {
            lock (SimVisionRandomLock)
            {
                if (Options != null && Options.SimulateVisionResult && SimVisionRandom.Next(0, 20) == 0)
                    return null;

                return new VisionAlignResult
                {
                    DeltaX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaTheta = (SimVisionRandom.NextDouble() - 0.5) * 0.02
                };
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

        private int ResolveVacuumSettleMs()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            return 50;
        }

        private void RecordColletUse(int pickerNo)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                FrontPicker.RecordColletUse(pickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                RearPicker.RecordColletUse(pickerNo);
        }

        private InputStageUnit ResolveInputStage()
        {
            return Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
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
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                double currentX = stage.CameraX != null ? stage.CameraX.ActualPosition : targetX;
                double currentY = stage.StageY != null ? stage.StageY.ActualPosition : targetY;

                bool visionXInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, targetX);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, targetY);
                if (visionXInPosition && stageYInPosition)
                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);

                if (!visionXInPosition && stageYInPosition)
                {
                    int result = await MoveInputVisionXAndVerifyAsync(stage, targetX, description + " VisionX", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                if (visionXInPosition && !stageYInPosition)
                {
                    int result = await MoveInputStageYAndVerifyAsync(stage, targetX, targetY, description + " StageY", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string xFirstReason;
                bool canMoveXFirst = stage.IsInputStageWorkPointInArea(targetX, currentY, out xFirstReason);
                if (canMoveXFirst)
                {
                    int result = await MoveInputVisionXAndVerifyAsync(stage, targetX, description + " VisionX", ct).ConfigureAwait(false);
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

                    result = await MoveInputVisionXAndVerifyAsync(stage, targetX, description + " VisionX", ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                return Fail("PICKER-PICKUP-STAGE-PATH", stage.Name,
                    description + " has no safe X/Y move order inside input stage work area. " +
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
                return Fail("PICKER-PICKUP-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " X/Y move order exception: " + ex.Message);
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
                return Fail("PICKER-PICKUP-VISIONX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " move exception: " + ex.Message);
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
                return Fail("PICKER-PICKUP-STAGEY-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " move exception: " + ex.Message);
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

            return CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, targetY, description + " StageY");
        }

        private bool IsInputStageAxisAlreadyInPosition(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
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
                        "PickerPickUp"),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " move command failed. result=" + result +
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
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
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

                int result = await AwaitStepWithCancellationAsync(
                    stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " move command failed. result=" + result +
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
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
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
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-STAGE", waitResult), stage.Name,
                        description + " move/in-position wait failed. " +
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
                return Fail("PICKER-PICKUP-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStageUnit", description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item == null)
                    return Fail("PICKER-PICKUP-STAGE-AXIS", stage != null ? stage.Name : "InputStageUnit", description + " axis is not available. " + BuildInputStageAxisState(stage, axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("PICKER-PICKUP-STAGE-POSITION", stage.Name, description + " final position check failed. " + BuildInputStageAxisState(stage, axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-POSITION-EX", stage != null ? stage.Name : "InputStageUnit", description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerAxisCommandResultAsync(PickerAxis axis, double target, string description, CancellationToken ct, string targetName = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MovePickerAxisCommandAsync(axis, target, targetName).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-PICKUP-MOVE-CMD", Name, description + " move command failed. result=" + result + ", " + BuildPickerAxisState(axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-CMD-EX", Name, description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerAxisInPositionResultAsync(PickerAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult waitResult = await WaitPickerAxisMoveDoneAsync(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-MOVE", waitResult), Name,
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, target)));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-WAIT-EX", Name, description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckPickerAxisInPosition(PickerAxis axis, double target, string description)
        {
            if (!IsPickerAxisInPosition(axis, target))
                return Fail("PICKER-PICKUP-MOVE-CHECK", Name, description + " final position check failed. " + BuildPickerAxisState(axis, target));

            return 0;
        }

        private static QMC.Common.Motion.BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                // 비전 X축 반환
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                default:
                    return null;
            }
        }

        private static string BuildInputStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
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

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            return Math.Abs(axis.ActualPosition - target) <= tolerance;
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

        private void ReleaseInputReservationIfNeeded()
        {
            try
            {
                bool released = false;
                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    PickUpBatchItem item = _pickBatchItems[i];
                    if (item == null || item.DiePicked || string.IsNullOrWhiteSpace(item.DieId))
                        continue;

                    MaterialStateService.ReleaseInputStagePickReservation(
                        item.DieId,
                        PickerLocationKind,
                        item.PickerNo);

                    released = true;
                    WriteLog("PickerPickUpSequence",
                        Name + " released input die batch reservation. die=" + item.DieId +
                        ", pickerNo=" + item.PickerNo + " - Ok");
                }

                if (!released && !_diePicked && !string.IsNullOrWhiteSpace(_currentDieId))
                {
                    MaterialStateService.ReleaseInputStagePickReservation(
                        _currentDieId,
                        PickerLocationKind,
                        _currentPickerNo);

                    WriteLog("PickerPickUpSequence",
                        Name + " released input die reservation. die=" + _currentDieId + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "Input die reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;
                ClearCurrentPickContext();
            }
        }

        private void ReleaseInputStageArea()
        {
            try
            {
                if (_inputStageLease == null)
                    return;

                _inputStageLease.Dispose();
                _inputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "InputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

    }
}

