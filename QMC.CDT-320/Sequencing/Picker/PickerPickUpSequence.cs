using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public enum PickerPickUpZManualStep
    {
        PrepareNeedlePinZ = 0,
        VacuumOnBeforePick = 1,
        MovePickerZPrePick = 2,
        MovePickerZSlowToContact = 3,
        MoveNeedlePickerZSyncLift = 4,
        SeparateNeedlePickerZ = 5,
        VerifyDiePicked = 6,
        MoveZToSafeAfterPick = 7,
        UpdateMaterialToPicker = 8
    }

    internal sealed class PickerPickUpSequence : PickerSequenceBase<PickerPickUpStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const int VisionInspectionSettleDelayMs = 100;

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
        private double _targetPickerY;
        private double _targetPickerT;
        private double _targetPickerZ;
        private double _targetNeedleX;
        private double _targetNeedleZ;
        private double _targetEjectPinZ;
        private bool _diePicked;
        private SequenceResourceLease _inputStageLease;
        private PickUpBatchItem _currentBatchItem;
        private PickUpZTargets _lastPickUpZTargets;

        private sealed class PickUpBatchItem
        {
            public int PickerIndex;
            public int PickerNo;
            public string DieId;
            public InputStagePickTarget PickTarget;
            public VisionAlignResult VisionOffset;
            public double TargetStageY;
            public double TargetPickerX;
            public double TargetPickerY;
            public double TargetPickerT;
            public double TargetPickerZ;
            public double TargetNeedleX;
            public double TargetNeedleZ;
            public double TargetEjectPinZ;
            public bool DiePicked;
        }

        private sealed class PickUpZTargets
        {
            public double PickerZ;
            public double NeedleZ;
            public double EjectPinZ;
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
            catch (SequenceStopException)
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

                // Input die vision 준비
                case PickerPickUpStep.BuildPickBatch:
                    return PrepareInputDieVisionBatchAsync(ct);

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
                        await Task.Delay(1, ct).ConfigureAwait(false);
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

        private async Task<int> PrepareInputDieVisionBatchAsync(CancellationToken ct)
        {
            try
            {
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;
                ClearCurrentPickContext();

                bool permitLoaded;
                int permitResult = TryLoadInputCameraMarkInspectionPermission(out permitLoaded);
                if (permitLoaded || permitResult != 0)
                    return permitResult;

                InputDieVisionPrepareSequence prepareSequence = new InputDieVisionPrepareSequence(
                    Context,
                    Side,
                    _enabledPickerIndexes);

                int result = await prepareSequence.RunAsync(ct, Options).ConfigureAwait(false);
                if (result != 0)
                    return result;

                IList<InputDieVisionPreparedItem> preparedItems = prepareSequence.PreparedItems;
                for (int i = 0; i < preparedItems.Count; i++)
                {
                    InputDieVisionPreparedItem prepared = preparedItems[i];
                    if (prepared == null)
                        continue;

                    _pickBatchItems.Add(new PickUpBatchItem
                    {
                        PickerIndex = prepared.PickerIndex,
                        PickerNo = prepared.PickerNo,
                        DieId = prepared.DieId,
                        PickTarget = prepared.PickTarget,
                        VisionOffset = prepared.VisionOffset,
                        DiePicked = prepared.DiePicked
                    });
                }

                if (_pickBatchItems.Count == 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " Input die vision 준비 결과가 없어 PickUp을 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerPickUpStep.Complete;
                    ReleaseInputStageArea();
                    return 0;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " Input die vision 준비 결과를 PickUp 배치로 받았습니다. count=" + _pickBatchItems.Count +
                    ", enabledPickerCount=" + _enabledPickerIndexes.Count + " - Ok");

                CurrentStep = PickerPickUpStep.MoveInputVisionToAvoidForPickerMove;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-PREPARE-EX", Name,
                    "Input die vision 준비 시퀀스 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int TryLoadInputCameraMarkInspectionPermission(out bool loaded)
        {
            loaded = false;

            try
            {
                List<InputDieVisionPreparedItem> permittedItems;
                string permissionReason;
                if (!InputCameraPickUpPermissionStore.TryConsume(Side, out permittedItems, out permissionReason))
                {
                    if (Options != null && Options.RequireInputCameraMarkInspectionPermission)
                    {
                        return Fail("PICKER-PICKUP-INPUT-CAMERA-MARK-PERMISSION", Name,
                            "Auto PickUp 전에 InputCamera Mark 검사 허가가 없습니다. " +
                            "PickerProcessSequence에서 InputCameraMarkInspectionSequence가 먼저 완료되어야 합니다. " +
                            "side=" + Side + ", reason=" + permissionReason);
                    }

                    WriteLog("PickerPickUpSequence",
                        Name + " InputCamera Mark 검사 PickUp 허가가 없어 기존 PickUp 내부 검사 흐름으로 진행합니다. " +
                        "side=" + Side + ", reason=" + permissionReason + " - Check");
                    return 0;
                }

                loaded = true;
                WriteLog("PickerPickUpSequence",
                    Name + " InputCamera Mark 검사 PickUp 허가를 수신했습니다. " +
                    "side=" + Side + ", " + permissionReason + " - Ok");

                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-PERMISSION-STAGE-NO-UNIT", "InputStageUnit",
                        "InputCamera Mark 검사 허가를 받았지만 InputStageUnit이 없습니다.");

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID", stage.Name,
                        "InputCamera Mark 검사 허가를 받았지만 InputVisionX가 Avoid 위치가 아닙니다. side=" + Side);

                if (permittedItems == null || permittedItems.Count == 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " InputCamera Mark 검사 허가 batch가 비어 있어 PickUp을 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerPickUpStep.Complete;
                    ReleaseInputStageArea();
                    return 0;
                }

                for (int i = 0; i < permittedItems.Count; i++)
                {
                    InputDieVisionPreparedItem permitted = permittedItems[i];
                    if (permitted == null)
                        continue;

                    var batchItem = new PickUpBatchItem
                    {
                        PickerIndex = permitted.PickerIndex,
                        PickerNo = permitted.PickerNo,
                        DieId = permitted.DieId,
                        PickTarget = permitted.PickTarget,
                        VisionOffset = permitted.VisionOffset,
                        DiePicked = permitted.DiePicked
                    };
                    _pickBatchItems.Add(batchItem);

                    string reason;
                    if (!MaterialStateService.ValidateInputStagePickTarget(
                        permitted.DieId,
                        PickerLocationKind,
                        permitted.PickerNo,
                        out reason))
                    {
                        return Fail("PICKER-PICKUP-PERMISSION-DIE-NOT-PICKABLE", "Material",
                            "InputCamera Mark 검사 허가 Die가 현재 PickUp 가능한 상태가 아닙니다. die=" +
                            permitted.DieId + ", pickerNo=" + permitted.PickerNo +
                            ", reason=" + reason);
                    }

                    if (permitted.PickTarget == null)
                    {
                        return Fail("PICKER-PICKUP-PERMISSION-TARGET-MISSING", "Material",
                            "InputCamera Mark 검사 허가에 PickTarget이 없습니다. die=" +
                            permitted.DieId + ", pickerNo=" + permitted.PickerNo);
                    }

                    if (permitted.VisionOffset == null)
                    {
                        return Fail("PICKER-PICKUP-PERMISSION-OFFSET-MISSING", "Vision",
                            "InputCamera Mark 검사 허가에 VisionOffset이 없습니다. die=" +
                            permitted.DieId + ", pickerNo=" + permitted.PickerNo);
                    }
                }

                if (_pickBatchItems.Count == 0)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " InputCamera Mark 검사 허가에서 PickUp 대상이 없어 완료 처리합니다. side=" + Side + " - Check");
                    CurrentStep = PickerPickUpStep.Complete;
                    ReleaseInputStageArea();
                    return 0;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " InputCamera Mark 검사 완료 batch로 PickUp을 허가합니다. count=" +
                    _pickBatchItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = PickerPickUpStep.CalculatePickTargets;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PERMISSION-EX", Name,
                    "InputCamera Mark 검사 PickUp 허가 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
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
                if (IsPickerMotionOnlyTestMode())
                {
                    if (!stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    {
                        return Fail("PICKER-PICKUP-STAGE-WORK-AREA", stage.Name,
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
                    WriteLog("PickerPickUpSequence",
                        Name + " Picker Motion Only Test 모드: InputVisionX 이동과 비전 검사를 생략하고 보정값 0으로 진행합니다. die=" +
                        _currentDieId + ", pickerNo=" + _currentPickerNo + " - Check");
                    CurrentStep = PickerPickUpStep.ApplyInputDieVisionOffset;
                    return 0;
                }

                if (!stage.IsInputStageWorkPointInArea(targetX, targetY, out areaReason))
                    return Fail("PICKER-PICKUP-STAGE-WORK-AREA", stage.Name,
                        "Input die target is outside input stage work area. " + areaReason);

                int result = await EnsureNeedleZSafeForCurrentStageTravelAsync(
                    stage,
                    "PickUp 비전 준비",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputStageZProcessForVisionAsync(
                    stage,
                    "PickUp 비전 검사",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageVisionPointForPickerAsync(
                    stage,
                    targetX,
                    targetY,
                    "input die",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureNeedleZProcessForVisionAsync(
                    stage,
                    "PickUp 비전 검사",
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
                _targetPickerY = GetPickerTeachingPosition(PickerAxis.PickerY, "PickPosition");
                _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PickPosition") +
                    ResolvePickerAlignOffsetT(_currentPickerIndex) +
                    _visionOffset.DeltaTheta;
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PickPosition");
                _targetNeedleX = ResolveNeedleXForVisionX(_pickTarget.TargetX, _visionOffset.DeltaX);
                _targetNeedleZ = ResolveNeedleZPickTarget();
                _targetEjectPinZ = ResolveEjectPinZPickTarget();

                WriteLog("PickerPickUpSequence",
                    Name + " calculated pick target. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerY=" + _targetPickerY +
                    ", pickerT=" + _targetPickerT +
                    ", pickerZ=" + _targetPickerZ +
                    ", needleX=" + _targetNeedleX +
                    ", needleZ=" + _targetNeedleZ +
                    ", ejectPinZ=" + _targetEjectPinZ +
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

        private Task<int> MoveOppositePickerToAvoidForPickerMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = VerifyOppositePickerNotInInputPickArea(
                    "Pick 위치 이동 전 상대 Picker Input 영역 확인");
                if (result != 0)
                    return Task.FromResult(result);

                CurrentStep = PickerPickUpStep.MovePickerXStageYPickerT;
                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("PICKER-PICKUP-OPPOSITE-CHECK-EX", Name,
                    "Pick 위치 이동 전 상대 Picker Input 영역 확인 중 예외가 발생했습니다. error=" + ex.Message));
            }
            finally
            {
            }
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

                string needleAreaReason;
                if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, _targetStageY, out needleAreaReason))
                {
                    return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
                        "PickUp 보정 Needle 목표 위치가 작업 가능 영역을 벗어났습니다. " +
                        "die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", needleX=" + _targetNeedleX.ToString("F6") +
                        ", stageY=" + _targetStageY.ToString("F6") +
                        ", reason=" + needleAreaReason);
                }

                int result = await EnsurePickerYAtAvoidBeforePickMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                var pickerTargets = new Dictionary<PickerAxis, double>();
                pickerTargets[PickerAxis.PickerX] = _targetPickerX;
                pickerTargets[PickerAxis.PickerY] = _targetPickerY;
                pickerTargets[tAxis] = _targetPickerT;

                Task<int> stageYMove = MoveInputStageYAndVerifyAsync(
                    stage,
                    _pickTarget.TargetX,
                    _targetStageY,
                    "pick corrected StageY",
                    ct);
                Task<int> needleXMove = MoveNeedleXAndVerifyAsync(
                    stage,
                    _targetNeedleX,
                    "pick corrected NeedleX",
                    ct);
                Task<int> pickerMove = MovePickerAxesAndVerifyAsync(
                    pickerTargets,
                    "pick corrected Picker X/Y/T",
                    ct,
                    targetName);

                int[] results = await Task.WhenAll(stageYMove, needleXMove, pickerMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-PARALLEL-MOVE", Name,
                        "PickUp StageY/NeedleX/Picker X/Y/T 동시 이동 실패. " +
                        "stageYResult=" + results[0] +
                        ", needleXResult=" + results[1] +
                        ", pickerResult=" + results[2] +
                        ", die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);
                }

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

        private async Task<int> EnsurePickerYAtAvoidBeforePickMoveAsync(CancellationToken ct)
        {
            try
            {
                double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, avoid))
                    return 0;

                int result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    avoid,
                    "pick picker Y avoid before X/T",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Y-AVOID-EX", Name,
                    "PickUp 전 PickerY Avoid 이동 중 예외 발생: " + ex.Message);
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

            result = CheckPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY, "pick PickerY teaching position");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT, "pick corrected PickerT");
            if (result != 0)
                return result;

            string needleAreaReason;
            if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, _targetStageY, out needleAreaReason))
            {
                return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
                    "PickUp 전 Needle 목표 위치가 작업 가능 영역을 벗어났습니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", needleX=" + _targetNeedleX.ToString("F6") +
                    ", stageY=" + _targetStageY.ToString("F6") +
                    ", reason=" + needleAreaReason);
            }

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, _targetNeedleX, "pick NeedleX");
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.MovePickerZPick;
            return 0;
        }

        private async Task<int> MovePickerZPickAsync(CancellationToken ct)
        {
            try
            {
                int result = await RunPickupZMotionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                {
                    await TryMovePickerNeedleAndEjectPinZToAvoidAsync("PickUp Z 세부 모션 실패 후 Z축 안전 복귀", ct).ConfigureAwait(false);
                    return result;
                }

                CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-MOTION-EX", Name,
                    "PickUp Z 세부 모션 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        internal async Task<int> RunPickupZMotionAsync(CancellationToken ct)
        {
            return await RunPickupZMotionAsync(true, ct).ConfigureAwait(false);
        }

        internal async Task<int> RunManualZMotionOnlyAsync(int pickerNo, CancellationToken ct, PickerSequenceOptions options)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);

                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-Z-TEST-STAGE", "InputStageUnit", "PickUp Z 단독 테스트 실패. InputStageUnit is null.");

                int normalizedPickerNo = pickerNo;
                if (normalizedPickerNo < 1)
                    normalizedPickerNo = 1;
                if (normalizedPickerNo > 4)
                    normalizedPickerNo = 4;

                _currentPickerNo = normalizedPickerNo;
                _currentPickerIndex = normalizedPickerNo - 1;
                _currentDieId = "ManualPickUpZTest";
                _pickTarget = null;
                _visionOffset = null;
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PickPosition");
                _targetNeedleZ = stage.Recipe != null && stage.Recipe.NeedleZ != null
                    ? stage.Recipe.NeedleZ.ProcessPosition
                    : 0.0;
                _targetEjectPinZ = ResolveEjectPinZPickTarget();
                _lastPickUpZTargets = null;

                int result = await RunPickupZMotionAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp Z 단독 테스트 완료. pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-TEST-EX", Name,
                    "PickUp Z 단독 테스트 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        internal async Task<int> RunManualSelectedDiePickUpAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool picked = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                if (string.IsNullOrWhiteSpace(dieId))
                    return Fail("PICKER-PICKUP-SELECT-DIE", "Material", "PickUp 테스트 대상 Die가 선택되지 않았습니다.");

                int normalizedPickerNo = pickerNo;
                if (normalizedPickerNo < 1)
                    normalizedPickerNo = 1;
                if (normalizedPickerNo > 4)
                    normalizedPickerNo = 4;

                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                string finishReason;
                if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                    return Fail("PICKER-PICKUP-STAGE-NOT-FINISH", "InputStage",
                        "선택 Die PickUp 테스트 전에 InputStage Align/DieMapping/Finish가 완료되어야 합니다. " + finishReason);

                DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, normalizedPickerNo);
                if (loadedDie != null)
                {
                    return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                        "선택 Die PickUp 테스트 불가: Picker가 이미 Die를 가지고 있습니다. " +
                        "pickerNo=" + normalizedPickerNo +
                        ", loadedDie=" + loadedDie.DieId +
                        ", selectedDie=" + dieId +
                        ", side=" + Side);
                }

                int acquireResult = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "ManualSelectedPickUp");

                InputStagePickTarget target = MaterialStateService.ReserveInputStagePickTargetByDieId(
                    PickerLocationKind,
                    normalizedPickerNo,
                    dieId);
                if (target == null)
                    return Fail("PICKER-PICKUP-SELECT-DIE-RESERVE", "Material",
                        "선택한 Die를 PickUp 대상으로 예약할 수 없습니다. die=" + dieId +
                        ", pickerNo=" + normalizedPickerNo +
                        ", side=" + Side);

                var item = new PickUpBatchItem
                {
                    PickerIndex = normalizedPickerNo - 1,
                    PickerNo = normalizedPickerNo,
                    DieId = target.DieId,
                    PickTarget = target
                };

                _pickBatchItems.Add(item);
                SetCurrentBatchItem(item);

                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 테스트 시작. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo +
                    ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                    ", inputVisionX=" + target.TargetX +
                    ", inputStageY=" + target.TargetY + " - Start");

                int result = await MoveAllPickerZToAvoidAndVerifyAsync("선택 Die PickUp 테스트 전 Picker Z Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyReservedInputDie();
                if (result != 0)
                    return result;

                result = await MovePickersToAvoidForInputVisionMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAndVisionToDieAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await RequestInputDieVisionInspectionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = ApplyInputDieVisionOffset();
                if (result != 0)
                    return result;

                result = await MoveInputVisionToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CalculatePickTargets();
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerXStageYPickerTAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await MovePickerZPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = UpdateMaterialToPicker();
                if (result != 0)
                    return result;

                picked = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 테스트 완료. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SELECT-DIE-EX", Name,
                    "선택 Die PickUp 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!picked)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualSelectedDiePrepareAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool prepared = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, true);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAllPickerZToAvoidAndVerifyAsync("선택 Die PickUp 준비 전 Picker Z Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyReservedInputDie();
                if (result != 0)
                    return result;

                result = await MovePickersToAvoidForInputVisionMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputStageAndVisionToDieAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await RequestInputDieVisionInspectionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = ApplyInputDieVisionOffset();
                if (result != 0)
                    return result;

                result = await MoveInputVisionToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CalculatePickTargets();
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidForPickerMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerXStageYPickerTAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                prepared = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 선택 Die PickUp 준비 완료. die=" + dieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerY=" + _targetPickerY +
                    ", pickerT=" + _targetPickerT + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PREPARE-EX", Name,
                    "선택 Die PickUp 준비 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!prepared)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualPreparedDiePickZAsync(
            string dieId,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool picked = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, false);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                VisionOffset offset;
                if (!MaterialStateService.TryGetLatestInputPickVisionOffset(dieId, out offset) || offset == null || !offset.IsValid)
                {
                    return Fail("PICKER-PICKUP-PREPARED-OFFSET", "Material",
                        "선택 Die Pick Z 테스트 전에 Input Vision 검사/Align 준비가 필요합니다. die=" + dieId +
                        ", pickerNo=" + pickerNo);
                }

                _visionOffset = new VisionAlignResult
                {
                    DeltaX = offset.X,
                    DeltaY = offset.Y,
                    DeltaTheta = offset.R
                };
                SaveCurrentStateToBatchItem();

                result = CalculatePickTargets();
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await MovePickerZPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = UpdateMaterialToPicker();
                if (result != 0)
                    return result;

                picked = true;
                WriteLog("PickerPickUpSequence",
                    Name + " 준비 Die Pick Z 테스트 완료. die=" + dieId +
                    ", pickerNo=" + pickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PREPARED-Z-EX", Name,
                    "준비 Die Pick Z 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!picked)
                    ReleaseInputReservationIfNeeded();

                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        internal async Task<int> RunManualPreparedDiePickZStepAsync(
            string dieId,
            int pickerNo,
            PickerPickUpZManualStep step,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                SetOptionsForManualOperation(options);
                ClearCurrentPickContext();
                _pickBatchItems.Clear();
                _inspectionCursor = 0;
                _pickCursor = 0;

                int result = PrepareManualSelectedDieContext(dieId, pickerNo, false);
                if (result != 0)
                    return result;

                result = await AcquireInputStageAreaForPickUpAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                VisionOffset offset;
                if (!MaterialStateService.TryGetLatestInputPickVisionOffset(dieId, out offset) || offset == null || !offset.IsValid)
                {
                    return Fail("PICKER-PICKUP-MANUAL-STEP-OFFSET", "Material",
                        "Pick Z Step 테스트 전에 Input Vision 검사/Align 준비가 필요합니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", step=" + step);
                }

                _visionOffset = new VisionAlignResult
                {
                    DeltaX = offset.X,
                    DeltaY = offset.Y,
                    DeltaTheta = offset.R
                };
                SaveCurrentStateToBatchItem();

                result = CalculatePickTargets();
                if (result != 0)
                    return result;

                result = SelectNextPickTarget();
                if (result != 0)
                    return result;

                result = VerifyPickTarget();
                if (result != 0)
                    return result;

                result = await RunPickupZManualStepAsync(step, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " 준비 Die Pick Z Step 테스트 완료. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", step=" + step + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MANUAL-STEP-EX", Name,
                    "Pick Z Step 테스트 중 예외가 발생했습니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", step=" + step +
                    ", error=" + ex.Message);
            }
            finally
            {
                ReleasePickerWorkArea();
                ReleaseInputStageArea();
            }
        }

        private int PrepareManualSelectedDieContext(string dieId, int pickerNo, bool reserveIfNeeded)
        {
            if (string.IsNullOrWhiteSpace(dieId))
                return Fail("PICKER-PICKUP-SELECT-DIE", "Material", "PickUp 테스트 대상 Die가 선택되지 않았습니다.");

            int normalizedPickerNo = pickerNo;
            if (normalizedPickerNo < 1)
                normalizedPickerNo = 1;
            if (normalizedPickerNo > 4)
                normalizedPickerNo = 4;

            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            string finishReason;
            if (!MaterialStateService.IsInputStageFinishComplete(out finishReason))
                return Fail("PICKER-PICKUP-STAGE-NOT-FINISH", "InputStage",
                    "선택 Die PickUp 테스트 전에 InputStage Align/DieMapping/Finish가 완료되어야 합니다. " + finishReason);

            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, normalizedPickerNo);
            if (loadedDie != null)
            {
                return Fail("PICKER-PICKUP-PICKER-OCCUPIED", "Material",
                    "선택 Die PickUp 테스트 불가: Picker가 이미 Die를 가지고 있습니다. " +
                    "pickerNo=" + normalizedPickerNo +
                    ", loadedDie=" + loadedDie.DieId +
                    ", selectedDie=" + dieId +
                    ", side=" + Side);
            }

            InputStagePickTarget target = reserveIfNeeded
                ? MaterialStateService.ReserveInputStagePickTargetByDieId(PickerLocationKind, normalizedPickerNo, dieId)
                : MaterialStateService.GetReservedInputStagePickTarget(PickerLocationKind, normalizedPickerNo, dieId);
            if (target == null)
            {
                return Fail("PICKER-PICKUP-SELECT-DIE-RESERVE", "Material",
                    "선택한 Die를 PickUp 대상으로 확인할 수 없습니다. die=" + dieId +
                    ", pickerNo=" + normalizedPickerNo +
                    ", side=" + Side +
                    ", reserveIfNeeded=" + reserveIfNeeded);
            }

            var item = new PickUpBatchItem
            {
                PickerIndex = normalizedPickerNo - 1,
                PickerNo = normalizedPickerNo,
                DieId = target.DieId,
                PickTarget = target
            };

            _pickBatchItems.Add(item);
            SetCurrentBatchItem(item);
            EnsurePickerWorkAreaReserved(PickerWorkZone.Input, reserveIfNeeded ? "ManualSelectedPickUpPrepare" : "ManualPreparedPickZ");

            WriteLog("PickerPickUpSequence",
                Name + " 선택 Die PickUp 테스트 Context 설정. die=" + dieId +
                ", pickerNo=" + normalizedPickerNo +
                ", grid=(" + target.DieMapX + "," + target.DieMapY + ")" +
                ", inputVisionX=" + target.TargetX +
                ", inputStageY=" + target.TargetY +
                ", reserveIfNeeded=" + reserveIfNeeded + " - Ok");
            return 0;
        }

        private async Task<int> RunPickupZMotionAsync(bool updateMaterialInspection, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");

                if (config.MotionMode == PickerPickUpZMotionMode.SimpleZDownVacuumUp)
                    return await RunSimplePickupZMotionAsync(config, pickerZ, pickerZAvoid, updateMaterialInspection, ct).ConfigureAwait(false);

                int result = await PrepareNeedlePinZForPickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOnBeforePickAsync(config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZPrePickAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZSlowToContactAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveNeedlePickerZSyncLiftAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await SeparateNeedlePickerZAsync(pickerZ, pickerZAvoid, _lastPickUpZTargets, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (config.PickSettleMs > 0)
                    await Task.Delay(config.PickSettleMs, ct).ConfigureAwait(false);

                result = await VerifyDiePickedAfterZMotionAsync(updateMaterialInspection, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveZToSafeAfterPickAsync(pickerZ, pickerZAvoid, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-RUN-EX", Name,
                    "PickUp Z 세부 모션 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunSimplePickupZMotionAsync(
            PickerPickUpMotionConfig config,
            PickerAxis pickerZ,
            double pickerZAvoid,
            bool updateMaterialInspection,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    "PickUp 단순 PickerZ 하강",
                    ct,
                    "DiePickPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOnForSimplePickAsync(config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    pickerZAvoid,
                    "PickUp 단순 PickerZ 상승",
                    ct,
                    "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (config.PickSettleMs > 0)
                    await Task.Delay(config.PickSettleMs, ct).ConfigureAwait(false);

                return await VerifyDiePickedAfterZMotionAsync(updateMaterialInspection, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SIMPLE-Z-RUN-EX", Name,
                    "PickUp 단순 Z 모션 실행 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunPickupZManualStepAsync(PickerPickUpZManualStep step, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");

                switch (step)
                {
                    case PickerPickUpZManualStep.PrepareNeedlePinZ:
                        return await PrepareNeedlePinZForPickAsync(ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.VacuumOnBeforePick:
                        return await VacuumOnBeforePickAsync(config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MovePickerZPrePick:
                        return await MovePickerZPrePickAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MovePickerZSlowToContact:
                        return await MovePickerZSlowToContactAsync(pickerZ, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MoveNeedlePickerZSyncLift:
                        return await MoveNeedlePickerZSyncLiftAsync(pickerZ, pickerZAvoid, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.SeparateNeedlePickerZ:
                        return await SeparateNeedlePickerZAsync(pickerZ, pickerZAvoid, _lastPickUpZTargets, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.VerifyDiePicked:
                        return await VerifyDiePickedAfterZMotionAsync(true, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MoveZToSafeAfterPick:
                        return await MoveZToSafeAfterPickAsync(pickerZ, pickerZAvoid, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.UpdateMaterialToPicker:
                        return UpdateMaterialToPicker();
                    default:
                        return Fail("PICKER-PICKUP-MANUAL-STEP-UNKNOWN", Name,
                            "알 수 없는 Pick Z Step입니다. step=" + step);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MANUAL-STEP-RUN-EX", Name,
                    "Pick Z Step 실행 중 예외가 발생했습니다. step=" + step +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareNeedlePinZForPickAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    _targetNeedleZ,
                    "PickUp NeedleZ 픽업 준비 위치",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    _targetEjectPinZ,
                    "PickUp EjectPinZ 픽업 준비 위치",
                    ct);

                int[] results = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-NEEDLE-PIN-READY", Name,
                        "PickUp Needle/EjectPin 준비 위치 이동 실패. " +
                        "needleZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ));
                }

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp NeedleZ 픽업 준비 위치");
                if (check != 0)
                    return check;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp EjectPinZ 픽업 준비 위치");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-READY-EX", Name,
                    "PickUp Needle/EjectPin 준비 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> VacuumOnBeforePickAsync(PickerPickUpMotionConfig config, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                SetPickerVacuum(_currentPickerNo, true);

                int delayMs = Math.Max(ResolveVacuumSettleMs(), config.VacuumOnBeforePickDelayMs);
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct).ConfigureAwait(false);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VACUUM-BEFORE-EX", Name,
                    "PickUp 하강 전 Vacuum ON 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> VacuumOnForSimplePickAsync(PickerPickUpMotionConfig config, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                SetPickerVacuum(_currentPickerNo, true);

                int delayMs = Math.Max(ResolveVacuumSettleMs(), config.VacuumOnBeforePickDelayMs);
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct).ConfigureAwait(false);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SIMPLE-VACUUM-EX", Name,
                    "PickUp 단순 Z 모션 Vacuum ON 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZPrePickAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (config.PickerZPrePickDistance <= 0.0)
                    return 0;

                double target = ResolveTargetToward(_targetPickerZ, pickerZAvoid, config.PickerZPrePickDistance);
                return await MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    target,
                    "PickUp PickerZ PrePick 위치",
                    ct,
                    "PickUpPrePick").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-PICK-EX", Name,
                    "PickUp PickerZ PrePick 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZSlowToContactAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double velocity = ResolvePickerAxisVelocityByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent);
                double acceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent, true);
                double deceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSlowApproachSpeedPercent, false);
                return await MovePickerAxisWithMotionAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    velocity,
                    acceleration,
                    deceleration,
                    "PickUp PickerZ 저속 Contact 위치",
                    "DiePickPosition[" + _currentPickerIndex + "]",
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-SLOW-CONTACT-EX", Name,
                    "PickUp PickerZ 저속 Contact 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedlePickerZSyncLiftAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            PickUpZTargets syncTargets = new PickUpZTargets();
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                stage.Config.EnsurePickUpMotionDefaults();
                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                syncTargets.PickerZ = ResolveTargetToward(_targetPickerZ, pickerZAvoid, config.PickerZSyncLiftDistance);
                syncTargets.NeedleZ = ResolveTargetToward(_targetNeedleZ, needleZAvoid, stage.Config.PickUpNeedleSyncLiftDistance);
                syncTargets.EjectPinZ = ResolveTargetToward(_targetEjectPinZ, ejectPinZAvoid, stage.Config.PickUpNeedleSyncLiftDistance);
                _lastPickUpZTargets = syncTargets;

                if (config.PickerZSyncLiftDistance <= 0.0 && stage.Config.PickUpNeedleSyncLiftDistance <= 0.0)
                    return 0;

                Task<int> pickerMove = MovePickerAxisWithMotionAndVerifyAsync(
                    pickerZ,
                    syncTargets.PickerZ,
                    config.PickerZSyncLiftVelocity,
                    config.PickerZSyncLiftAcceleration,
                    config.PickerZSyncLiftDeceleration,
                    "PickUp PickerZ/NeedlePinZ 동기 상승 PickerZ",
                    "PickUpSyncLift",
                    ct);
                Task<int> needleMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    syncTargets.NeedleZ,
                    stage.Config.PickUpNeedleSyncLiftVelocity,
                    stage.Config.PickUpNeedleSyncLiftAcc,
                    stage.Config.PickUpNeedleSyncLiftDec,
                    "PickUp PickerZ/NeedlePinZ 동기 상승 NeedleZ",
                    ct);
                Task<int> ejectMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    syncTargets.EjectPinZ,
                    stage.Config.PickUpNeedleSyncLiftVelocity,
                    stage.Config.PickUpNeedleSyncLiftAcc,
                    stage.Config.PickUpNeedleSyncLiftDec,
                    "PickUp PickerZ/NeedlePinZ 동기 상승 EjectPinZ",
                    ct);

                int[] results = await Task.WhenAll(pickerMove, needleMove, ejectMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-SYNC-LIFT", Name,
                        "PickUp PickerZ/NeedlePinZ 동기 상승 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needleZResult=" + results[1] +
                        ", ejectPinZResult=" + results[2] +
                        ", " + BuildPickerAxisState(pickerZ, syncTargets.PickerZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, syncTargets.EjectPinZ));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-EX", Name,
                    "PickUp PickerZ/NeedlePinZ 동기 상승 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> SeparateNeedlePickerZAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickUpZTargets syncTargets,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                stage.Config.EnsurePickUpMotionDefaults();
                if (config.PickerZSeparateDistance <= 0.0 && stage.Config.PickUpNeedleSeparateDistance <= 0.0)
                    return 0;

                if (syncTargets == null)
                {
                    syncTargets = new PickUpZTargets
                    {
                        PickerZ = _targetPickerZ,
                        NeedleZ = _targetNeedleZ,
                        EjectPinZ = _targetEjectPinZ
                    };
                }

                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                double pickerTarget = ResolveTargetToward(syncTargets.PickerZ, pickerZAvoid, config.PickerZSeparateDistance);
                double needleTarget = ResolveTargetToward(syncTargets.NeedleZ, needleZAvoid, stage.Config.PickUpNeedleSeparateDistance);
                double ejectTarget = ResolveTargetToward(syncTargets.EjectPinZ, ejectPinZAvoid, stage.Config.PickUpNeedleSeparateDistance);
                double pickerVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, config.PickerZSeparateSpeedPercent);
                double pickerAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSeparateSpeedPercent, true);
                double pickerDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, config.PickerZSeparateSpeedPercent, false);

                if (config.SeparateMode == PickerPickUpSeparateMode.NeedleFirst)
                {
                    int needleResult = await MoveNeedlePinZSeparateAsync(stage, needleTarget, ejectTarget, ct).ConfigureAwait(false);
                    if (needleResult != 0)
                        return needleResult;

                    return await MovePickerAxisWithMotionAndVerifyAsync(
                        pickerZ,
                        pickerTarget,
                        pickerVelocity,
                        pickerAcceleration,
                        pickerDeceleration,
                        "PickUp 분리 PickerZ 상승",
                        "PickUpSeparate",
                        ct).ConfigureAwait(false);
                }

                if (config.SeparateMode == PickerPickUpSeparateMode.PickerFirst)
                {
                    int pickerResult = await MovePickerAxisWithMotionAndVerifyAsync(
                        pickerZ,
                        pickerTarget,
                        pickerVelocity,
                        pickerAcceleration,
                        pickerDeceleration,
                        "PickUp 분리 PickerZ 상승",
                        "PickUpSeparate",
                        ct).ConfigureAwait(false);
                    if (pickerResult != 0)
                        return pickerResult;

                    return await MoveNeedlePinZSeparateAsync(stage, needleTarget, ejectTarget, ct).ConfigureAwait(false);
                }

                Task<int> pickerMove = MovePickerAxisWithMotionAndVerifyAsync(
                    pickerZ,
                    pickerTarget,
                    pickerVelocity,
                    pickerAcceleration,
                    pickerDeceleration,
                    "PickUp 동시 분리 PickerZ 상승",
                    "PickUpSeparate",
                    ct);
                Task<int> needleMove = MoveNeedlePinZSeparateAsync(stage, needleTarget, ejectTarget, ct);
                int[] results = await Task.WhenAll(pickerMove, needleMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-SEPARATE", Name,
                        "PickUp PickerZ/NeedlePinZ 분리 이동 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needlePinResult=" + results[1] +
                        ", " + BuildPickerAxisState(pickerZ, pickerTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectTarget));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SEPARATE-EX", Name,
                    "PickUp PickerZ/NeedlePinZ 분리 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedlePinZSeparateAsync(
            InputStageUnit stage,
            double needleTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                double needleVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent);
                double needleAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent, true);
                double needleDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleZ, stage.Config.PickUpNeedleSeparateSpeedPercent, false);
                double ejectVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent);
                double ejectAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent, true);
                double ejectDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.EjectPinZ, stage.Config.PickUpNeedleSeparateSpeedPercent, false);
                Task<int> needleMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    needleVelocity,
                    needleAcceleration,
                    needleDeceleration,
                    "PickUp 분리 NeedleZ 하강",
                    ct);
                Task<int> ejectMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    ejectVelocity,
                    ejectAcceleration,
                    ejectDeceleration,
                    "PickUp 분리 EjectPinZ 하강",
                    ct);

                int[] results = await Task.WhenAll(needleMove, ejectMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-NEEDLE-PIN-SEPARATE", Name,
                        "PickUp Needle/EjectPin 분리 이동 실패. " +
                        "needleZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTarget) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectTarget));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-SEPARATE-EX", Name,
                    "PickUp Needle/EjectPin 분리 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> VerifyDiePickedAfterZMotionAsync(bool updateMaterialInspection, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int flowResult = await VerifyPickerFlowStateAsync(
                    _currentPickerNo,
                    true,
                    "PickUp Z 모션 완료 후 흡착 Flow 확인",
                    ct).ConfigureAwait(false);
                if (flowResult != 0)
                    return flowResult;

                if (!updateMaterialInspection)
                    return 0;

                return VerifyDiePicked();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VERIFY-PICKED-EX", Name,
                    "PickUp 흡착 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveZToSafeAfterPickAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            CancellationToken ct)
        {
            return await MovePickerNeedleEjectZToAvoidAsync(
                pickerZ,
                pickerZAvoid,
                "PickUp 완료 후 PickerZ/NeedleZ/EjectPinZ 안전 복귀",
                ct).ConfigureAwait(false);
        }

        private PickerPickUpMotionConfig ResolvePickUpMotionConfig()
        {
            PickerPickUpMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                config = FrontPicker.Config.PickUp;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                config = RearPicker.Config.PickUp;

            if (config == null)
                config = new PickerPickUpMotionConfig();

            config.Ensure();
            return config;
        }

        private async Task<int> MovePickerAxisWithMotionAndVerifyAsync(
            PickerAxis axis,
            double target,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            string targetName,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int commandResult = await MovePickerAxisCommandWithMotionAsync(axis, target, velocity, acceleration, deceleration, targetName).ConfigureAwait(false);
                if (commandResult != 0)
                    return Fail("PICKER-PICKUP-MOVE-CMD", Name,
                        description + " 이동 명령 실패. result=" + commandResult +
                        ", velocity=" + velocity +
                        ", acc=" + acceleration +
                        ", dec=" + deceleration +
                        ", " + BuildPickerAxisState(axis, target));

                int waitResult = await WaitPickerAxisInPositionResultAsync(axis, target, description, ct).ConfigureAwait(false);
                if (waitResult != 0)
                    return waitResult;

                return CheckPickerAxisInPosition(axis, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-VEL-EX", Name,
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisWithMotionAndVerifyAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int commandResult = await AwaitStepWithCancellationAsync(
                    stage.MoveInputStageAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration),
                    ct).ConfigureAwait(false);
                if (commandResult != 0)
                {
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name,
                        description + " 이동 명령 실패. result=" + commandResult +
                        ", velocity=" + velocity +
                        ", acc=" + acceleration +
                        ", dec=" + deceleration +
                        ", " + BuildInputStageAxisState(stage, axis, target) +
                        PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));
                }

                int waitResult = await WaitInputStageAxisInPositionResultAsync(stage, axis, target, description, ct).ConfigureAwait(false);
                if (waitResult != 0)
                    return waitResult;

                return CheckInputStageAxisInPosition(stage, axis, target, description);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-VEL-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private double ResolvePickerAxisVelocityByPercent(PickerAxis axis, double percent)
        {
            QMC.Common.Motion.BaseAxis item = GetPickerAxis(axis);
            return ResolveAxisVelocityByPercent(item, percent);
        }

        private double ResolvePickerAxisAccelerationByPercent(PickerAxis axis, double percent, bool acceleration)
        {
            QMC.Common.Motion.BaseAxis item = GetPickerAxis(axis);
            return ResolveAxisAccelerationByPercent(item, percent, acceleration);
        }

        private static double ResolveInputStageAxisVelocityByPercent(InputStageUnit stage, WaferStageAxis axis, double percent)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            return ResolveAxisVelocityByPercent(item, percent);
        }

        private static double ResolveInputStageAxisAccelerationByPercent(InputStageUnit stage, WaferStageAxis axis, double percent, bool acceleration)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            return ResolveAxisAccelerationByPercent(item, percent, acceleration);
        }

        private static double ResolveAxisVelocityByPercent(QMC.Common.Motion.BaseAxis axis, double percent)
        {
            double normalizedPercent = PickerPickUpMotionConfig.NormalizePercent(percent, 1.0);
            double baseVelocity = 1.0;
            if (axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0)
                baseVelocity = axis.Config.DefaultVelocity;

            return Math.Max(0.001, baseVelocity * normalizedPercent / 100.0);
        }

        private static double ResolveAxisAccelerationByPercent(QMC.Common.Motion.BaseAxis axis, double percent, bool acceleration)
        {
            double normalizedPercent = PickerPickUpMotionConfig.NormalizePercent(percent, 1.0);
            double baseAcceleration = 1.0;
            if (axis != null && axis.Config != null)
            {
                double configured = acceleration ? axis.Config.Acceleration : axis.Config.Deceleration;
                if (configured > 0.0)
                    baseAcceleration = configured;
            }

            return Math.Max(0.001, baseAcceleration * normalizedPercent / 100.0);
        }

        private static double ResolveTargetToward(double fromTarget, double towardTarget, double distance)
        {
            if (distance <= 0.0)
                return fromTarget;

            double delta = towardTarget - fromTarget;
            if (Math.Abs(delta) <= distance)
                return towardTarget;

            return fromTarget + Math.Sign(delta) * distance;
        }

        private async Task<int> MovePickerNeedleEjectZToPickAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                Task<int> pickerZMove = MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    _targetPickerZ,
                    "pick Z down",
                    ct,
                    "DiePickPosition[" + _currentPickerIndex + "]");
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    _targetNeedleZ,
                    "픽업 NeedleZ 상승",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    _targetEjectPinZ,
                    "픽업 EjectPinZ 상승",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-SYNC", Name,
                        "PickUp Z 동기 이동 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needleZResult=" + results[1] +
                        ", ejectPinZResult=" + results[2] +
                        ", " + BuildPickerAxisState(pickerZ, _targetPickerZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ));
                }

                int check = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "pick Z down");
                if (check != 0)
                    return check;

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "픽업 NeedleZ 상승");
                if (check != 0)
                    return check;

                return CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "픽업 EjectPinZ 상승");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-EX", Name,
                    "PickUp Z 동기 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerNeedleEjectZToAvoidAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            string description,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                Task<int> pickerZMove = MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    pickerZAvoid,
                    "pick Z avoid after pickup",
                    ct,
                    "AvoidPosition");
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleZAvoid,
                    "픽업 후 NeedleZ 안전 위치 복귀",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    "픽업 후 EjectPinZ 안전 위치 복귀",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-AVOID-SYNC", Name,
                        description + " 실패. " +
                        "pickerZResult=" + results[0] +
                        ", needleZResult=" + results[1] +
                        ", ejectPinZResult=" + results[2] +
                        ", " + BuildPickerAxisState(pickerZ, pickerZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-AVOID-SYNC-EX", Name,
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> TryMovePickerNeedleAndEjectPinZToAvoidAsync(string description, CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return -1;

                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");
                double needleZAvoid = ResolveNeedleZAvoidTarget(stage);
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                Task<int> pickerZMove = MovePickerAxisAndVerifyAsync(
                    pickerZ,
                    pickerZAvoid,
                    description + " PickerZ",
                    ct,
                    "AvoidPosition");
                Task<int> needleZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleZAvoid,
                    description + " NeedleZ",
                    ct);
                Task<int> ejectPinZMove = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " EjectPinZ",
                    ct);

                int[] results = await Task.WhenAll(pickerZMove, needleZMove, ejectPinZMove).ConfigureAwait(false);
                WriteLog("PickerPickUpSequence",
                    Name + " " + description +
                    ". pickerZResult=" + results[0] +
                    ", needleZResult=" + results[1] +
                    ", ejectPinZResult=" + results[2] + " - Check");
                return results[0] == 0 && results[1] == 0 && results[2] == 0 ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> VacuumOnAsync(CancellationToken ct)
        {
            try
            {
                SetPickerVacuum(_currentPickerNo, true);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);

                int flowResult = await VerifyPickerFlowStateAsync(
                    _currentPickerNo,
                    true,
                    "PickUp Vacuum ON 후 흡착 Flow 확인",
                    ct).ConfigureAwait(false);
                if (flowResult != 0)
                    return flowResult;

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
                    BuildBooleanMeasurement("FlowCheckOn", true),
                    BuildMeasurement("PickerNo", _currentPickerNo, "no", MaterialInspectionResult.Ok),
                    BuildMeasurement("NeedleX", _targetNeedleX, "mm", MaterialInspectionResult.Ok),
                    BuildMeasurement("NeedleZ", _targetNeedleZ, "mm", MaterialInspectionResult.Ok),
                    BuildMeasurement("EjectPinZ", _targetEjectPinZ, "mm", MaterialInspectionResult.Ok)
                }
            });

            CurrentStep = PickerPickUpStep.MovePickerZToAvoid;
            return 0;
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerNeedleEjectZToAvoidAsync(
                zAxis,
                avoid,
                "PickUp 완료 후 PickerZ/NeedleZ/EjectPinZ 안전 복귀",
                ct).ConfigureAwait(false);
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
            _targetPickerY = item != null ? item.TargetPickerY : 0.0;
            _targetPickerT = item != null ? item.TargetPickerT : 0.0;
            _targetPickerZ = item != null ? item.TargetPickerZ : 0.0;
            _targetNeedleX = item != null ? item.TargetNeedleX : 0.0;
            _targetNeedleZ = item != null ? item.TargetNeedleZ : 0.0;
            _targetEjectPinZ = item != null ? item.TargetEjectPinZ : 0.0;
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
            _currentBatchItem.TargetPickerY = _targetPickerY;
            _currentBatchItem.TargetPickerT = _targetPickerT;
            _currentBatchItem.TargetPickerZ = _targetPickerZ;
            _currentBatchItem.TargetNeedleX = _targetNeedleX;
            _currentBatchItem.TargetNeedleZ = _targetNeedleZ;
            _currentBatchItem.TargetEjectPinZ = _targetEjectPinZ;
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
            _targetPickerY = 0.0;
            _targetPickerT = 0.0;
            _targetPickerZ = 0.0;
            _targetNeedleX = 0.0;
            _targetNeedleZ = 0.0;
            _targetEjectPinZ = 0.0;
            _diePicked = false;
        }

        private double ResolveNeedleXForVisionX(double visionX, double visionOffsetX = 0.0)
        {
            InputStageUnit stage = ResolveInputStage();
            double offset = stage != null && stage.Setup != null
                ? stage.Setup.NeedleXToVisionXOffset
                : 0.0;
            return visionX + visionOffsetX - offset;
        }

        private double ResolveNeedleZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.NeedleZ.ProcessPosition;
        }

        private double ResolveEjectPinZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            double offset = stage.Config != null ? stage.Config.PickUpEjectPinOffset : 0.0;
            return stage.Recipe.EjectPinZ.ProcessPosition + offset;
        }

        private static double ResolveNeedleZAvoidTarget(InputStageUnit stage)
        {
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.NeedleZ.AvoidPosition;
        }

        private static double ResolveEjectPinZAvoidTarget(InputStageUnit stage)
        {
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.EjectPinZ.AvoidPosition;
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
                return Fail("PICKER-PICKUP-MOTION-ONLY-STAGEY-EX", stage != null ? stage.Name : "InputStageUnit",
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

            return 5; //50
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

        private async Task<int> EnsureInputStageZProcessForVisionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.WaferZ == null)
                    return Fail("PICKER-PICKUP-STAGEZ-RECIPE", stage.Name,
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
                return Fail("PICKER-PICKUP-STAGEZ-PROCESS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 InputStage Z Process 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.NeedleZ == null)
                    return Fail("PICKER-PICKUP-NEEDLEZ-RECIPE", stage.Name,
                        description + " 전 NeedleZ Process 위치 정보가 없습니다.");

                double needleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : stage.Recipe.NeedleX.ProcessPosition;
                double stageY = stage.StageY != null ? stage.StageY.ActualPosition : stage.ResolveWorkAreaCenterY();
                string areaReason;
                if (!stage.IsNeedleWorkPointInArea(needleX, stageY, out areaReason))
                    return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
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
                return Fail("PICKER-PICKUP-NEEDLEZ-PROCESS-EX", stage != null ? stage.Name : "InputStageUnit",
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
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");
                if (stage.Recipe == null || stage.Recipe.NeedleZ == null)
                    return Fail("PICKER-PICKUP-NEEDLEZ-RECIPE", stage.Name,
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
                return Fail("PICKER-PICKUP-NEEDLEZ-AVOID-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 전 NeedleZ Avoid 위치 확인 중 예외가 발생했습니다. error=" + ex.Message);
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
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                double currentX = stage.CameraX != null ? stage.CameraX.ActualPosition : targetX;
                double currentY = stage.StageY != null ? stage.StageY.ActualPosition : targetY;
                double targetNeedleX = ResolveNeedleXForVisionX(targetX);

                string needleAreaReason;
                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out needleAreaReason))
                {
                    return Fail("PICKER-PICKUP-NEEDLE-WORK-AREA", stage.Name,
                        "니들 목표 위치가 작업 가능 영역을 벗어났습니다. " +
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

                return Fail("PICKER-PICKUP-STAGE-PATH", stage.Name,
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
                return Fail("PICKER-PICKUP-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
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
                            return Fail("PICKER-PICKUP-STAGE-XY-PARALLEL", stage != null ? stage.Name : "InputStageUnit",
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
                return Fail("PICKER-PICKUP-STAGE-XY-PARALLEL-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 동시 이동 중 예외가 발생했습니다. error=" + ex.Message);
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
                    return Fail("PICKER-PICKUP-VISION-NEEDLE-X", stage != null ? stage.Name : "InputStageUnit",
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
                return Fail("PICKER-PICKUP-VISION-NEEDLE-X-EX", stage != null ? stage.Name : "InputStageUnit",
                    "InputVisionX와 NeedleX 동시 이동 중 예외가 발생했습니다. error=" + ex.Message);
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
                return Fail("PICKER-PICKUP-NEEDLEX-MOVE-EX", stage != null ? stage.Name : "InputStageUnit",
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

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, targetY, description + " StageY");
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(
                stage,
                WaferStageAxis.NeedleX,
                ResolveNeedleXForVisionX(targetX),
                description + " NeedleX");
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

                int result;
                if (axis == WaferStageAxis.VisionX)
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(stage.CameraX, target, "PickerPickUp;Side=" + Side + ";InputVisionX;" + description))
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
                case WaferStageAxis.WaferExpandingZ:
                    return stage.ExpanderZ;
                // 비전 X축 반환
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

