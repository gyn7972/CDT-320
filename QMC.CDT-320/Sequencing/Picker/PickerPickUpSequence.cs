using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    public enum PickerPickUpZManualStep
    {
        PrepareNeedlePinZ = 0,
        VacuumOnBeforePick = 1,
        MovePickerZPrePick = 2,
        MovePickerZSlowToContact = 3,
        MoveEjectPinPickerZSyncLift = 4,
        SeparateNeedlePickerZ = 5,
        VerifyDiePicked = 6,
        MoveZToSafeAfterPick = 7,
        UpdateMaterialToPicker = 8
    }

    internal sealed class PickerPickUpSequence : PickerSequenceBase<PickerPickUpStep>
    {
        private const double ContinuousPickMaxDeltaX = 45.0;
        private const double ContinuousPickMaxDeltaY = 1.5;
        private const double ContinuousPickMaxDeltaT = 0.2;
        private const double ContinuousPickFacingPrecheckClearance = 180.0;
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
        private double _targetPickerY;
        private double _targetPickerT;
        private double _targetPickerZ;
        private double _targetNeedleX;
        private double _targetNeedleZ;
        private double _targetEjectPinZ;
        // Formula from the central pick target resolver; kept until final verify logging.
        private string _targetFormula = "";
        private bool _diePicked;
        private bool _pickerZContactedByContiPickUp;
        private bool _currentPickSafeReturnCompleted;
        private SequenceResourceLease _inputStageLease;
        private PickUpBatchItem _currentBatchItem;
        private PickUpZTargets _lastPickUpZTargets;
        private double _inputVisionPickerEntryTarget;
        private bool _inputVisionPickerEntryTargetPrepared;
        private DateTime _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;

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
            // Formula snapshot for this picker item so batch cursor changes do not hide the original calculation.
            public string TargetFormula;
            public bool DiePicked;
        }

        private sealed class PickUpZTargets
        {
            public double PickerZ;
            public double NeedleZ;
            public double EjectPinZ;
            public double EjectPinSyncLiftOffset;
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
                    int stepResult = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerPickUpStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerPickUpStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
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
                    return Task.FromResult(CalculatePickTargets(true));

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

                // 픽업 전 Picker 제품 유/무 확인
                case PickerPickUpStep.VerifyPickerEmptyBeforePick:
                    return VerifyPickerEmptyBeforePickAsync(ct);

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
                Name + " enabled picker order=" + string.Join(",", _enabledPickerIndexes.ConvertAll(i => ToPickerNo(i).ToString()).ToArray()) + " - Ok");

            CurrentStep = PickerPickUpStep.CheckInputStageReady;
            return 0;
        }

        private int CheckInputStageReady()
        {
            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("CheckInputStageReady");

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
                        Context.StopIfCycleStopRequested(
                            Name + ".AcquireInputStageArea",
                            ShouldDeferCycleStopForPickUpDrain(),
                            "PickUp batch drain");

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
                _inputVisionPickerEntryTargetPrepared = false;
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

                result = await prepareSequence.CollectVisionResultsAsync(ct).ConfigureAwait(false);
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

                CurrentStep = PickerPickUpStep.CalculatePickTargets;
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
                {
                    InputCameraPickUpPermissionStore.Grant(Side, permittedItems);
                    return Fail("PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID", stage.Name,
                        "InputCamera Mark 검사 허가를 받았지만 InputVisionX가 Avoid 위치가 아닙니다. " +
                        "허가는 복구했으며, 다른 InputCamera 선행검사가 PickUp 허가 이후 InputVisionX를 이동했는지 확인해야 합니다. side=" + Side);
                }

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
                CurrentStep = PickerPickUpStep.CalculatePickTargets;
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
                double targetNeedleX = ResolveNeedleXForVisionX(targetX);

                string areaReason;
                if (IsPickerMotionOnlyTestMode())
                {
                    if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out areaReason))
                    {
                        return Fail("PICKER-PICKUP-STAGE-WORK-AREA", stage.Name,
                            "Picker Motion Only Test 목표 위치가 InputStage 작업 가능 영역을 벗어났습니다. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", needleX=" + targetNeedleX.ToString("F6") +
                            ", reason=" + areaReason);
                    }

                    int stageOnlyResult = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                        stage,
                        "PickUp 비전 준비",
                        ct).ConfigureAwait(false);
                    if (stageOnlyResult != 0)
                        return stageOnlyResult;

                    stageOnlyResult = await EnsureWaferAlignThetaPositionAsync(
                        stage,
                        "PickUp 비전 준비 전 StageT 보정 위치",
                        ct).ConfigureAwait(false);
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
                    WriteLog("PickerPickUpSequence",
                        Name + " Picker Motion Only Test 모드: InputVisionX 이동과 비전 검사를 생략하고 보정값 0으로 진행합니다. die=" +
                        _currentDieId + ", pickerNo=" + _currentPickerNo + " - Check");
                    CurrentStep = PickerPickUpStep.ApplyInputDieVisionOffset;
                    return 0;
                }

                if (!stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out areaReason))
                    return Fail("PICKER-PICKUP-STAGE-WORK-AREA", stage.Name,
                        "Input die target is outside input stage needle work area. " +
                        "needleX=" + targetNeedleX.ToString("F6") + ". " + areaReason);

                int result = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                    stage,
                    "PickUp 비전 준비",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureWaferAlignThetaPositionAsync(
                    stage,
                    "PickUp 비전 준비 전 StageT 보정 위치",
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

                // 공정 중 NeedleZ는 이동하지 않는다. 픽업 준비 상승은 PrepareNeedlePinZForPickAsync가 수행한다.
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

                    _visionOffset = await RequestInputVisionOffsetAsync(ct, attempt == 1).ConfigureAwait(false);
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

                if (Options != null &&
                    Options.InputDieVisionFailureAction == InputDieVisionFailureAction.Alarm)
                {
                    return Fail("PICKER-PICKUP-VISION-NG", "Vision",
                        "Input die vision inspection failed. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", retryCount=" + retryCount);
                }

                return SkipCurrentVisionFailedDieAndContinue(retryCount);
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
                    "PickerPickUpVisionNgSkip",
                    out message);
                if (!syncOk)
                {
                    return Fail("PICKER-PICKUP-VISION-SKIP-FAIL", "Material",
                        "Input die vision 실패 Die SKIP 처리에 실패했습니다. die=" + dieId +
                        ", pickerNo=" + pickerNo +
                        ", message=" + message);
                }

                if (_currentBatchItem != null)
                    _pickBatchItems.Remove(_currentBatchItem);

                WriteLog("PickerPickUpSequence",
                    Name + " input die vision 실패 Die를 SKIP 처리하고 다음 Die로 진행합니다. die=" + dieId +
                    ", pickerNo=" + pickerNo +
                    ", retryCount=" + retryCount + " - Ok");

                ClearCurrentPickContext();
                if (_pickBatchItems.Count == 0)
                {
                    CurrentStep = PickerPickUpStep.Complete;
                    ReleaseInputStageArea();
                }
                else if (_inspectionCursor >= _pickBatchItems.Count)
                {
                    CurrentStep = PickerPickUpStep.CalculatePickTargets;
                }
                else
                {
                    CurrentStep = PickerPickUpStep.SelectNextInspectionTarget;
                }

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-SKIP-EX", "Material",
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

            CurrentStep = PickerPickUpStep.CalculatePickTargets;
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
                double target = avoid;
                string retreatDetail = "PickUp 목표 좌표가 없어 전체 Avoid를 사용합니다.";
                bool targetsCalculated = ArePickBatchTargetsCalculated();

                if (targetsCalculated)
                {
                    SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                        Context != null ? Context.Machine : null);
                    if (service != null)
                    {
                        var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                        SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                            ? SharedRailXAxis.FrontPickerX
                            : SharedRailXAxis.RearPickerX;
                        var pickerTargets = new List<double>();
                        for (int i = 0; i < _pickBatchItems.Count; i++)
                            pickerTargets.Add(_pickBatchItems[i].TargetPickerX);
                        planned[pickerRailAxis] = pickerTargets;

                        double dynamicTarget;
                        string dynamicDetail;
                        if (service.TryResolveNearestVisionRetreatTarget(
                            stage.CameraX,
                            avoid,
                            -0.1,
                            planned,
                            1.0,
                            out dynamicTarget,
                            out dynamicDetail))
                        {
                            target = dynamicTarget;
                            retreatDetail = dynamicDetail;
                        }
                        else
                        {
                            retreatDetail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                        }
                    }
                }

                _inputVisionPickerEntryTarget = target;
                _inputVisionPickerEntryTargetPrepared = true;
                WriteLog("PickerPickUpSequence",
                    Name + " InputVisionX 피커 진입 회피 좌표를 확정했습니다. " +
                    "target=" + target.ToString("F6") +
                    ", fullAvoid=" + avoid.ToString("F6") +
                    ", batchPickerX=" + string.Join(",", _pickBatchItems.ConvertAll(x => x.TargetPickerX.ToString("F6")).ToArray()) +
                    ", detail=" + retreatDetail + " - Check");

                if (!IsAxisInTarget(stage.CameraX, target))
                {
                    int result = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        "InputVisionX 최소 회피 위치 이동",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await WaitInputStageAxisInPositionResultAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        target,
                        "InputVisionX 최소 회피 위치 이동",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                int checkResult = CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.VisionX,
                    target,
                    "InputVisionX 최소 회피 위치 이동");
                if (checkResult != 0)
                    return checkResult;

                CurrentStep = targetsCalculated
                    ? PickerPickUpStep.SelectNextPickTarget
                    : PickerPickUpStep.CalculatePickTargets;
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

        private int CalculatePickTargets(bool moveVisionAfterCalculation = false)
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

                CurrentStep = moveVisionAfterCalculation
                    ? PickerPickUpStep.MoveInputVisionToAvoidForPickerMove
                    : PickerPickUpStep.SelectNextPickTarget;
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

                // Input Vision 검사에서 받은 X/Y/T 보정값을 Pick 이동식에 직접 적용한다.
                double alignOffsetX = _visionOffset.DeltaX;
                double alignOffsetY = _visionOffset.DeltaY;
                double alignOffsetT = _visionOffset.DeltaTheta;

                PickCoordinateResult coordinate;
                string coordinateReason;
                if (!PickerMotionTargetResolver.TryCalculateInputPickTarget(
                    Context != null ? Context.Machine : null,
                    Side,
                    _currentPickerIndex,
                    Name,
                    _currentDieId,
                    _pickTarget.TargetX,
                    _pickTarget.TargetY,
                    alignOffsetX,
                    alignOffsetY,
                    alignOffsetT,
                    true,
                    out coordinate,
                    out coordinateReason))
                {
                    return Fail("PICKER-PICKUP-COORD-OFFSET", Name,
                        "Input pick coordinate target resolve failed. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", reason=" + coordinateReason);
                }

                _targetStageY = coordinate.StageY;
                _targetPickerX = coordinate.PickerX;
                _targetPickerY = coordinate.PickerY;
                _targetPickerT = coordinate.PickerT;
                _targetPickerZ = coordinate.PickerZ;
                _targetNeedleX = coordinate.NeedleX;
                _targetNeedleZ = coordinate.NeedleZ;
                _targetEjectPinZ = coordinate.EjectPinZ;
                _targetFormula = coordinate.Formula;

                double cameraOffsetX;
                double cameraOffsetY;
                InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(
                    Context != null ? Context.Machine : null,
                    out cameraOffsetX,
                    out cameraOffsetY);

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
                    ", formula=" + coordinate.Formula +
                    ", cameraOffsetX=" + cameraOffsetX +
                    ", cameraOffsetY=" + cameraOffsetY +
                    ", cameraOffsetIncludedInInputVisionToPicker=True" +
                    ", alignOffsetX=" + alignOffsetX +
                    ", alignOffsetY=" + alignOffsetY +
                    ", visionTotalOffsetX=" + _visionOffset.DeltaX +
                    ", visionTotalOffsetY=" + _visionOffset.DeltaY +
                    ", visionOffsetXAppliedToPickerAndNeedle=True" +
                    ", visionOffsetYAppliedToStage=True(OppositeSign)" +
                    ", visionOffsetYAppliedToPicker=False(FixedPickY)" +
                    ", needleYToVisionYOffset=" + ResolveNeedleCalibrationOffsetY() +
                    ", alignOffsetT=" + alignOffsetT + " - Ok");

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
            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("SelectNextPickTarget");

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
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await WaitOppositePickerNotInInputPickAreaAsync(
                    "Pick 위치 이동 전 상대 Picker Input 영역 확인",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerPickUpStep.MovePickerXStageYPickerT;
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
                return Fail("PICKER-PICKUP-OPPOSITE-CHECK-EX", Name,
                    "Pick 위치 이동 전 상대 Picker Input 영역 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitOppositePickerNotInInputPickAreaAsync(
            string description,
            CancellationToken ct)
        {
            try
            {
                string oppositeUnitName;
                string blockReason;
                if (!TryBuildOppositePickerInputBlockReason(description, out oppositeUnitName, out blockReason))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 상대 Picker Input 영역 확인 완료. description=" + description + " - Ok");
                    return 0;
                }

                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    return Fail("PICKER-OPPOSITE-INPUT-ZONE", oppositeUnitName, blockReason);
                }

                bool waitLogged = false;
                DateTime lastWaitLog = DateTime.MinValue;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitOppositePickerInputClearBeforePick",
                            ShouldDeferCycleStopForPickUpDrain(),
                            "PickUp batch drain");

                    if (!TryBuildOppositePickerInputBlockReason(description, out oppositeUnitName, out blockReason))
                    {
                        if (waitLogged)
                        {
                            WriteLog("PickerPickUpSequence",
                                Name + " 상대 Picker Input 영역 대기 완료. 상대 Picker가 PickUp Input 영역을 물리적으로 이탈한 뒤 Pick 위치 이동을 허용합니다. " +
                                "description=" + description + " - Ok");
                        }
                        else
                        {
                            WriteLog("PickerPickUpSequence",
                                Name + " 상대 Picker Input 영역 확인 완료. description=" + description + " - Ok");
                        }

                        return 0;
                    }

                    if ((DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= 1000.0)
                    {
                        lastWaitLog = DateTime.UtcNow;
                        waitLogged = true;
                        WriteLog("PickerPickUpSequence",
                            Name + " Pick 위치 이동 전 상대 Picker Input 영역 이탈 대기. " +
                            "상대 Picker가 PickUp 중이거나 Pick 위치에 남아 있어 현재 Picker의 Input 진입을 보류합니다. " +
                            "side=" + Side +
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
                return Fail("PICKER-OPPOSITE-INPUT-ZONE-EX", Name,
                    description + " 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyOppositePickerNotInInputPickArea(string description)
        {
            try
            {
                string oppositeUnitName;
                string blockReason;
                if (TryBuildOppositePickerInputBlockReason(description, out oppositeUnitName, out blockReason))
                    return Fail("PICKER-OPPOSITE-INPUT-ZONE", oppositeUnitName, blockReason);

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

        private bool TryBuildOppositePickerInputBlockReason(
            string description,
            out string oppositeUnitName,
            out string blockReason)
        {
            oppositeUnitName = Side == PickerSequenceSide.Rear ? "FrontPickerUnit" : "RearPickerUnit";
            blockReason = string.Empty;

            bool oppositeIsFront = Side == PickerSequenceSide.Rear;
            string oppositePickerName = oppositeIsFront ? "FrontPicker" : "RearPicker";

            if (!IsOppositePickerUnitAvailable())
            {
                WriteLog("PickerPickUpSequence",
                    Name + " 상대 Picker Input 영역 확인 생략. " + oppositeUnitName +
                    " 없음. description=" + description + " - Check");
                return false;
            }

            if (oppositeIsFront)
            {
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (!FrontPicker.IsFrontPickerInDiePickPosition(pickerNo))
                        continue;

                    blockReason = description + " 실패. " + oppositePickerName + "가 Input Pick 영역에 있습니다. " +
                        oppositePickerName + "를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo;
                    return true;
                }
            }
            else
            {
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (!RearPicker.IsRearPickerInDiePickPosition(pickerNo))
                        continue;

                    blockReason = description + " 실패. " + oppositePickerName + "가 Input Pick 영역에 있습니다. " +
                        oppositePickerName + "를 먼저 Input 영역 밖으로 이동해야 합니다. pickerNo=" + pickerNo;
                    return true;
                }
            }

            string inputBlockReason;
            if (IsOppositePickerInputInterferenceActive(out inputBlockReason))
            {
                blockReason = description + " 실패. " + oppositePickerName +
                    "가 Input 영역을 점유하거나 진입/이탈 중입니다. " + inputBlockReason;
                return true;
            }

            return false;
        }

        private async Task<int> MovePickerXStageYPickerTAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                string targetName = BuildPickMoveTargetName();
                PickerPickUpMotionConfig pickUpConfig = ResolvePickUpMotionConfig();
                bool useContiTransfer = IsCoordinatedPickUpTransferMotionMode(pickUpConfig.TransferMotionMode);

                // 기존 조건: CameraX/StageY 기준 체크는 실제 간섭축 기준이 아니라서 PickUp 보정 이동 차단 조건으로 쓰지 않는다.
                // string areaReason;
                // if (!stage.IsInputStageWorkPointInArea(_pickTarget.TargetX, _targetStageY, out areaReason)) ...

                int result = await EnsureZAxesAtAvoidBeforePickerMoveAsync(
                    stage,
                    "PickUp 피커 이동 전 Z축 안전 복귀",
                    useContiTransfer,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

                result = await EnsureWaferAlignThetaPositionAsync(
                    stage,
                    "PickUp 피커 접근 전 StageT 보정 위치",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (useContiTransfer)
                {
                    return await MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync(
                        stage,
                        tAxis,
                        targetName,
                        pickUpConfig,
                        ct).ConfigureAwait(false);
                }

                return await MovePickerXStageYPickerTByDefaultAsync(
                    stage,
                    tAxis,
                    targetName,
                    ct).ConfigureAwait(false);
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

        private async Task<int> MovePickerXStageYPickerTByDefaultAsync(
            InputStageUnit stage,
            PickerAxis tAxis,
            string targetName,
            CancellationToken ct)
        {
            try
            {
                int result = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                    stage,
                    "PickUp default transfer before NeedleX/StageY move",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsurePickerYAtAvoidBeforePickMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Input 로딩/언로딩이 우선이다.
                // PickUp은 InputStageArea를 잡고 비전/스테이지 준비를 진행하되,
                // Picker가 실제 Pick 위치로 진입하기 직전에만 Input work area를 점유한다.
                EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUp");

                var pickerTargets = new Dictionary<PickerAxis, double>();
                pickerTargets[PickerAxis.PickerX] = _targetPickerX;
                pickerTargets[tAxis] = _targetPickerT;

                Task<int> pickerMove = MovePickerAxesAndVerifyAsync(
                    pickerTargets,
                    "pick corrected Picker X/T",
                    ct,
                    targetName);
                Task<int> needleStageMove = MoveNeedleXAndStageYForPickAsync(
                    stage,
                    _targetNeedleX,
                    _targetStageY,
                    _pickTarget.TargetX,
                    "pick corrected NeedleX/StageY",
                    ct);

                int[] results = await Task.WhenAll(needleStageMove, pickerMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-SAFE-MOVE", Name,
                        "PickUp NeedleX/StageY 안전 순서 이동 또는 Picker X/T 이동 실패. " +
                        "needleStageResult=" + results[0] +
                        ", pickerResult=" + results[1] +
                        ", die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo);
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp Picker X/T 및 NeedleX/StageY 목표 이동 완료 후 PickerY 전진을 시작합니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", targetY=" + _targetPickerY +
                    ", targetName=" + targetName + " - Check");

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    _targetPickerY,
                    "pick corrected PickerY",
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
                return Fail("PICKER-PICKUP-XYT-MOVE-DEFAULT-EX", Name, "Pick default XYT move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsCoordinatedPickUpTransferMotionMode(PickerPickUpTransferMotionMode mode)
        {
            return mode == PickerPickUpTransferMotionMode.ContiSegmentedPickUp;
        }

        private async Task<int> MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync(
            InputStageUnit stage,
            PickerAxis tAxis,
            string targetName,
            PickerPickUpMotionConfig pickUpConfig,
            CancellationToken ct)
        {
            try
            {
                if (pickUpConfig == null)
                    pickUpConfig = new PickerPickUpMotionConfig();
                pickUpConfig.Ensure();

                PickerAxis pickerZAxis = GetPickerZAxis(_currentPickerIndex);
                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis needleX = ResolveInputStageAxis(stage, WaferStageAxis.NeedleX);
                BaseAxis stageY = ResolveInputStageAxis(stage, WaferStageAxis.WaferY);
                BaseAxis pickerZ = GetPickerAxis(pickerZAxis);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZAxis, "AvoidPosition");
                double prePickTarget = ResolveTargetToward(_targetPickerZ, pickerZAvoid, pickUpConfig.PickerZPrePickDistance);

                string guardReason;
                if (!CanUseContiSegmentedPickUpFromCurrentPosition(
                    stage,
                    pickerX,
                    needleX,
                    stageY,
                    pickerZ,
                    tAxis,
                    prePickTarget,
                    pickUpConfig,
                    out guardReason))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp ContiNode condition rejected. Use default PickUp transfer. " +
                        "reason=" + guardReason +
                        ", pickerNo=" + _currentPickerNo +
                        ", pickIndex=" + (_pickCursor + 1) +
                        "/" + _pickBatchItems.Count +
                        ", die=" + _currentDieId + " - Check");
                    return await MovePickerXStageYPickerTByDefaultAsync(stage, tAxis, targetName, ct).ConfigureAwait(false);
                }

                int preMove = await MovePickerYPickerTAndEjectPinZBeforeContiPickUpAsync(
                    stage,
                    tAxis,
                    targetName,
                    ct).ConfigureAwait(false);
                if (preMove != 0)
                    return preMove;

                string inputZDetail;
                if (!AreInputPickZAxesSafeBeforeContinuousXYT(stage, out inputZDetail))
                {
                    return Fail("PICKER-PICKUP-CONTI-Z-SAFE", stage.Name,
                        "PickUp ContiNode before StageY move, Input Z axes are not safe. " +
                        inputZDetail +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ResolveEjectPinZAvoidTarget(stage)) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ));
                }

                IList<PickerPickUpContiNode> nodes = BuildContiSegmentedPickUpNodes(
                    pickerX,
                    needleX,
                    stageY,
                    pickerZ,
                    prePickTarget,
                    pickUpConfig);

                if (!CanUseContiSegmentedPickUpNodesFromCurrentPosition(
                    stage,
                    pickerX,
                    needleX,
                    stageY,
                    pickerZ,
                    nodes,
                    pickUpConfig,
                    out guardReason))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp ContiNode node condition rejected. Use default PickUp transfer. " +
                        "reason=" + guardReason +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId + " - Check");
                    return await MovePickerXStageYPickerTByDefaultAsync(stage, tAxis, targetName, ct).ConfigureAwait(false);
                }

                // 니들 진공은 XY 게이트에서 OFF되므로 conti에서는 Contact 직전에 ON한다.
                // 여기서는 접촉 시 die를 잡기 위한 Picker Vacuum만 미리 ON한다.
                SetPickerVacuum(_currentPickerNo, true);
                WriteLog("PickerPickUpZ",
                    Name + " PickUp ContiNode Picker Vacuum ON before transfer. pickerNo=" + _currentPickerNo + " - Ok");

                EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUp ContiNode");

                double pickerXStart = pickerX.ActualPosition;
                double pickerXPrePickTrigger = ResolveContiAsyncPrePickTriggerPosition(
                    pickerXStart,
                    _targetPickerX,
                    pickUpConfig);
                double transferVelocity = pickUpConfig.GetTransferContiNodeVelocity(2);
                double transferAcceleration = pickUpConfig.GetTransferContiNodeAcceleration(2);
                double transferDeceleration = pickUpConfig.GetTransferContiNodeDeceleration(2);

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode async transfer start. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerXStart=" + pickerXStart.ToString("F6") +
                    ", pickerXTarget=" + _targetPickerX.ToString("F6") +
                    ", prePickTrigger=" + pickerXPrePickTrigger.ToString("F6") +
                    ", prePickZ=" + prePickTarget.ToString("F6") +
                    ", velocity=" + transferVelocity.ToString("F6") +
                    ", acc=" + transferAcceleration.ToString("F6") +
                    ", dec=" + transferDeceleration.ToString("F6") + " - Start");

                Task<int> pickerXMoveTask = MovePickerAxisWithMotionAndVerifyAsync(
                    PickerAxis.PickerX,
                    _targetPickerX,
                    transferVelocity,
                    transferAcceleration,
                    transferDeceleration,
                    "PickUp ContiNode PickerX async target",
                    targetName,
                    ct);
                Task<int> pickerZPrePickTask = MovePickerZPrePickAfterPickerXProgressAsync(
                    pickerX,
                    pickerZAxis,
                    pickerZAvoid,
                    pickerXStart,
                    pickerXPrePickTrigger,
                    pickUpConfig,
                    ct);

                int needleSafetyResult = await EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync(
                    stage,
                    "PickUp ContiNode StageY/NeedleX 이동 전 EjectPinZ 대기(Avoid)/Vacuum OFF 확인",
                    ct).ConfigureAwait(false);
                if (needleSafetyResult != 0)
                {
                    int[] pickerOnlyResults = await Task.WhenAll(
                        pickerXMoveTask,
                        pickerZPrePickTask).ConfigureAwait(false);
                    WriteLog("PickerPickUpSequence",
                        Name + " EjectPinZ 대기(Avoid)/Vacuum OFF 조건 실패로 StageY/NeedleX 이동은 시작하지 않았습니다. " +
                        "PickerX/PickerZ 선행 이동 완료 후 시퀀스를 중단합니다. " +
                        "needleSafetyResult=" + needleSafetyResult +
                        ", pickerXResult=" + pickerOnlyResults[0] +
                        ", pickerZPrePickResult=" + pickerOnlyResults[1] + " - Failed");
                    return needleSafetyResult;
                }

                Task<int> stageYMoveTask = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferY,
                    _targetStageY,
                    transferVelocity,
                    transferAcceleration,
                    transferDeceleration,
                    "PickUp ContiNode StageY async target",
                    ct,
                    BuildPickUpInputStageMoveTargetName(WaferStageAxis.WaferY, "PickUpContiNodeStageY"));
                Task<int> needleXMoveTask = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    _targetNeedleX,
                    transferVelocity,
                    transferAcceleration,
                    transferDeceleration,
                    "PickUp ContiNode NeedleX async target",
                    ct,
                    BuildPickUpInputStageMoveTargetName(WaferStageAxis.NeedleX, "PickUpContiNodeNeedleX"));

                int[] transferResults = await Task.WhenAll(
                    pickerXMoveTask,
                    stageYMoveTask,
                    needleXMoveTask,
                    pickerZPrePickTask).ConfigureAwait(false);
                if (transferResults[0] != 0 ||
                    transferResults[1] != 0 ||
                    transferResults[2] != 0 ||
                    transferResults[3] != 0)
                {
                    return Fail("PICKER-PICKUP-CONTI-ASYNC-MOVE", Name,
                        "PickUp ContiNode async transfer failed. " +
                        "pickerXResult=" + transferResults[0] +
                        ", stageYResult=" + transferResults[1] +
                        ", needleXResult=" + transferResults[2] +
                        ", pickerZPrePickResult=" + transferResults[3] +
                        ", " + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, _targetStageY) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, _targetNeedleX) +
                        ", " + BuildPickerAxisState(pickerZAxis, prePickTarget));
                }

                // XY 이동 게이트에서 Needle Vacuum을 OFF했으므로 Contact/EjectPinZ 상승 전에 다시 ON한다.
                int vacuumOnResult = EnsureNeedleVacuumOnForPick(stage, "PickUp ContiNode Contact 전");
                if (vacuumOnResult != 0)
                    return vacuumOnResult;

                Task<int> pickerZContactTask = MovePickerZSlowToContactAndSettleAsync(
                    pickerZAxis,
                    pickUpConfig,
                    ct);
                Task<int> ejectPinZReadyTask = MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    _targetEjectPinZ,
                    "PickUp ContiNode EjectPinZ pick ready async",
                    ct);

                int[] contactResults = await Task.WhenAll(
                    pickerZContactTask,
                    ejectPinZReadyTask).ConfigureAwait(false);
                if (contactResults[0] != 0 || contactResults[1] != 0)
                {
                    return Fail("PICKER-PICKUP-CONTI-CONTACT", Name,
                        "PickUp ContiNode contact/eject ready failed. " +
                        "pickerZContactResult=" + contactResults[0] +
                        ", ejectPinZReadyResult=" + contactResults[1] +
                        ", " + BuildPickerAxisState(pickerZAxis, _targetPickerZ) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ));
                }

                int finalWait = await WaitContiSegmentedPickUpFinalPositionAsync(
                    stage,
                    pickerZAxis,
                    _targetPickerZ,
                    Math.Max(pickUpConfig.TransferContiTimeoutMs, ResolveTimeout()),
                    ct).ConfigureAwait(false);
                if (finalWait != 0)
                    return finalWait;

                // MoveInputStageAxisCommandAsync already completes the move/in-position wait.
                // Bypass this immediate duplicate snapshot check to avoid encoder-jitter false alarms.
                // int ejectCheck = CheckInputStageAxisInPosition(
                //     stage,
                //     WaferStageAxis.EjectPinZ,
                //     _targetEjectPinZ,
                //     "PickUp ContiNode EjectPinZ pick ready final");
                // if (ejectCheck != 0)
                //     return ejectCheck;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode async transfer/contact complete. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", targetPickerZ=" + _targetPickerZ.ToString("F3") +
                    ", prePickZ=" + prePickTarget.ToString("F3") +
                    ", ejectPinZ=" + _targetEjectPinZ.ToString("F3") +
                    " - Ok");

                _pickerZContactedByContiPickUp = true;
                CurrentStep = PickerPickUpStep.VerifyPickTarget;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-CONTI-EX", Name,
                    "PickUp ContiNode transfer exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerYPickerTAndEjectPinZBeforeContiPickUpAsync(
            InputStageUnit stage,
            PickerAxis tAxis,
            string targetName,
            CancellationToken ct)
        {
            try
            {
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                var pickerTargets = new Dictionary<PickerAxis, double>();
                pickerTargets[PickerAxis.PickerY] = _targetPickerY;
                pickerTargets[tAxis] = _targetPickerT;

                Task<int> ejectPinZMove = MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    "PickUp ContiNode PickerY pre-correction with EjectPinZ Avoid",
                    ct);
                Task<int> pickerPreMove = MovePickerAxesAndVerifyAsync(
                    pickerTargets,
                    "PickUp ContiNode PickerY/T pre-correction",
                    ct,
                    targetName);

                int[] results = await Task.WhenAll(pickerPreMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-CONTI-PRE-MOVE", Name,
                        "PickUp ContiNode pre-correction failed. " +
                        "pickerResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                        ", " + BuildPickerAxisState(tAxis, _targetPickerT) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid));
                }

                int check = CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    "PickUp ContiNode EjectPinZ Avoid before StageY move");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode pre-correction complete. PickerY/T and EjectPinZ Avoid ready. " +
                    BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                    ", " + BuildPickerAxisState(tAxis, _targetPickerT) +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                    " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-CONTI-PRE-MOVE-EX", Name,
                    "PickUp ContiNode pre-correction exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
            int vacuumOffResult = EnsureNeedleVacuumOffForPick(stage, description + " - EjectPinZ Avoid 이동 전");
            if (vacuumOffResult != 0)
                return vacuumOffResult;

            int result = await MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                stage,
                WaferStageAxis.EjectPinZ,
                ejectPinZAvoid,
                description + " - EjectPinZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PickerPickUpZ",
                Name + " Needle Vacuum OFF 후 EjectPinZ Avoid 완료 확인. " +
                "description=" + description +
                ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                " - Ok");

            return 0;
        }

        private bool CanUseContiSegmentedPickUpFromCurrentPosition(
            InputStageUnit stage,
            BaseAxis pickerX,
            BaseAxis needleX,
            BaseAxis stageY,
            BaseAxis pickerZ,
            PickerAxis tAxis,
            double prePickTarget,
            PickerPickUpMotionConfig pickUpConfig,
            out string reason)
        {
            reason = string.Empty;

            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
            {
                reason = "runMode is not Auto.";
                return false;
            }

            if (_pickCursor <= 0)
            {
                reason = "first pick in batch.";
                return false;
            }

            if (stage == null || pickerX == null || needleX == null || stageY == null || pickerZ == null)
            {
                reason = "required axis missing. pickerX=" + FormatAxisForContinuousCheck(pickerX) +
                    ", needleX=" + FormatAxisForContinuousCheck(needleX) +
                    ", stageY=" + FormatAxisForContinuousCheck(stageY) +
                    ", pickerZ=" + FormatAxisForContinuousCheck(pickerZ);
                return false;
            }

            if (!IsAxisReadyForContiPickUp(pickerX, "PickerX", out reason) ||
                !IsAxisReadyForContiPickUp(needleX, "NeedleX", out reason) ||
                !IsAxisReadyForContiPickUp(stageY, "StageY", out reason) ||
                !IsAxisReadyForContiPickUp(pickerZ, "PickerZ", out reason))
            {
                return false;
            }

            BaseAxis pickerY = GetPickerAxis(PickerAxis.PickerY);
            BaseAxis pickerT = GetPickerAxis(tAxis);
            if (!IsAxisReadyForContiPickUp(pickerY, "PickerY", out reason) ||
                !IsAxisReadyForContiPickUp(pickerT, "PickerT", out reason))
            {
                return false;
            }

            double pickerYAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, pickerYAvoid))
            {
                reason = "PickerY is at Avoid. Use default safe approach.";
                return false;
            }

            double maxTravel = pickUpConfig != null ? pickUpConfig.TransferContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

            if (pickUpConfig == null || pickUpConfig.PickerZPrePickDistance <= 0.0)
            {
                reason = "PickerZ PrePick distance is disabled. Use default so contact Z starts only after XYT final.";
                return false;
            }

            double yMax = pickUpConfig != null ? pickUpConfig.TransferContiPickerYMaxCorrectionDistance : ContinuousPickMaxDeltaY;
            if (yMax <= 0.0)
                yMax = ContinuousPickMaxDeltaY;

            double deltaY = Math.Abs(_targetPickerY - pickerY.ActualPosition);
            double deltaT = Math.Abs(_targetPickerT - pickerT.ActualPosition);
            if (deltaY > yMax || deltaT > ContinuousPickMaxDeltaT)
            {
                reason = "PickerY/T pre-correction limit exceeded. deltaY=" + deltaY.ToString("0.###") +
                    "/" + yMax.ToString("0.###") +
                    ", deltaT=" + deltaT.ToString("0.###") +
                    "/" + ContinuousPickMaxDeltaT.ToString("0.###");
                return false;
            }

            if (Math.Abs(_targetPickerX - pickerX.ActualPosition) > maxTravel ||
                Math.Abs(_targetNeedleX - needleX.ActualPosition) > maxTravel ||
                Math.Abs(_targetStageY - stageY.ActualPosition) > maxTravel ||
                Math.Abs(prePickTarget - pickerZ.ActualPosition) > maxTravel)
            {
                reason = "target travel exceeds PickUp ContiNode max travel. max=" + maxTravel.ToString("0.###") +
                    ", pickerX=" + FormatTravel(pickerX, _targetPickerX) +
                    ", needleX=" + FormatTravel(needleX, _targetNeedleX) +
                    ", stageY=" + FormatTravel(stageY, _targetStageY) +
                    ", pickerZ=" + FormatTravel(pickerZ, prePickTarget);
                return false;
            }

            string zDetail;
            if (!ArePickerZAxesSafeForContinuousPick(out zDetail))
            {
                reason = "PickerZ is not safe. " + zDetail;
                return false;
            }

            string visionDetail;
            if (!IsInputVisionXSafeForContinuousPick(stage, out visionDetail))
            {
                reason = "InputVisionX is not safe. " + visionDetail;
                return false;
            }

            string oppositeDetail;
            if (IsOppositePickerInputInterferenceActive(out oppositeDetail))
            {
                reason = "opposite picker blocks Input. " + oppositeDetail;
                return false;
            }

            string facingDetail;
            if (!IsFrontRearPickerXFacingPrecheckClear(_targetPickerX, out facingDetail))
            {
                reason = "Front/Rear PickerX facing precheck blocked. " + facingDetail;
                return false;
            }

            string workAreaReason;
            if (!IsNeedleWorkPathInAreaForContiPickUp(stage, needleX.ActualPosition, stageY.ActualPosition, _targetNeedleX, _targetStageY, out workAreaReason))
            {
                reason = workAreaReason;
                return false;
            }

            bool moveNeedleXFirst;
            string orderReason;
            if (!stage.TryResolveNeedleWorkPointMoveOrder(_targetNeedleX, _targetStageY, out moveNeedleXFirst, out orderReason))
            {
                reason = "NeedleX/StageY safe order not found. " + orderReason;
                return false;
            }

            // 공정 중 NeedleZ 무이동 정책: conti는 NeedleZ를 픽업 목표 높이로 유지한 채 StageY/NeedleX를
            // 동시에 이동시키므로, 목표 높이 일치와 양쪽 L코너 작업 원 조건을 만족하지 못하면
            // 여기서 부적격 처리해 default 순차 경로로 폴백한다(이송 인터락 알람 방지).
            string needleZKeptReason;
            if (!CanRunContiConcurrentXyWithNeedleZKept(stage, out needleZKeptReason))
            {
                reason = "NeedleZ-kept concurrent XY precondition not met. " + needleZKeptReason;
                return false;
            }

            reason = "Ok. pickerYDelta=" + deltaY.ToString("0.###") +
                ", tDelta=" + deltaT.ToString("0.###") +
                ", workArea=" + workAreaReason +
                ", order=" + (moveNeedleXFirst ? "NeedleX->StageY" : "StageY->NeedleX") +
                ", facing=" + facingDetail;
            return true;
        }

        private bool CanRunContiConcurrentXyWithNeedleZKept(InputStageUnit stage, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (stage == null || stage.NeedleZ == null)
                    return true;

                if (stage.NeedleZ.IsMoving)
                {
                    reason = "NeedleZ is moving. actual=" + stage.NeedleZ.ActualPosition.ToString("0.###");
                    return false;
                }

                // conti에는 NeedleZ 상승 단계가 없으므로 이미 픽업 목표 높이에 있어야 한다.
                double tolerance = stage.NeedleZ.Config != null && stage.NeedleZ.Config.InPositionTolerance > 0.0
                    ? stage.NeedleZ.Config.InPositionTolerance
                    : 0.01;
                if (double.IsNaN(_targetNeedleZ) ||
                    double.IsInfinity(_targetNeedleZ) ||
                    Math.Abs(stage.NeedleZ.ActualPosition - _targetNeedleZ) > tolerance)
                {
                    reason = "NeedleZ is not at pick target. actual=" + stage.NeedleZ.ActualPosition.ToString("0.###") +
                        ", pickTarget=" + _targetNeedleZ.ToString("0.###") +
                        ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }

                double currentNeedleX = stage.NeedleBlockX != null
                    ? stage.NeedleBlockX.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterX();
                double currentStageY = stage.StageY != null
                    ? stage.StageY.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterY();

                // 동시 이동은 축별 인터락이 (목표X,현재Y)/(현재X,목표Y) 코너를 각각 검사하므로 둘 다 원 안이어야 한다.
                string cornerXFirstReason;
                if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, currentStageY, out cornerXFirstReason))
                {
                    reason = "NeedleX-first corner is outside area. needleX=" + _targetNeedleX.ToString("0.###") +
                        ", stageY=" + currentStageY.ToString("0.###") +
                        ", reason=" + cornerXFirstReason;
                    return false;
                }

                string cornerYFirstReason;
                if (!stage.IsNeedleWorkPointInArea(currentNeedleX, _targetStageY, out cornerYFirstReason))
                {
                    reason = "StageY-first corner is outside area. needleX=" + currentNeedleX.ToString("0.###") +
                        ", stageY=" + _targetStageY.ToString("0.###") +
                        ", reason=" + cornerYFirstReason;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "NeedleZ-kept concurrent XY precheck failed. error=" + ex.Message;
                return false;
            }
        }

        private IList<PickerPickUpContiNode> BuildContiSegmentedPickUpNodes(
            BaseAxis pickerX,
            BaseAxis needleX,
            BaseAxis stageY,
            BaseAxis pickerZ,
            double prePickTarget,
            PickerPickUpMotionConfig pickUpConfig)
        {
            var nodes = new List<PickerPickUpContiNode>();
            if (pickerX == null || needleX == null || stageY == null || pickerZ == null || pickUpConfig == null)
                return nodes;

            double firstRatio = Math.Max(0.0, Math.Min(1.0, pickUpConfig.TransferContiXYMidRatio));
            double secondRatio = Math.Max(firstRatio, Math.Min(1.0, firstRatio * 2.0));

            double pickerXNode0 = pickerX.ActualPosition + ((_targetPickerX - pickerX.ActualPosition) * firstRatio);
            double needleXNode0 = needleX.ActualPosition + ((_targetNeedleX - needleX.ActualPosition) * firstRatio);
            double stageYNode0 = stageY.ActualPosition + ((_targetStageY - stageY.ActualPosition) * firstRatio);

            double pickerXNode1 = pickerX.ActualPosition + ((_targetPickerX - pickerX.ActualPosition) * secondRatio);
            double needleXNode1 = needleX.ActualPosition + ((_targetNeedleX - needleX.ActualPosition) * secondRatio);
            double stageYNode1 = stageY.ActualPosition + ((_targetStageY - stageY.ActualPosition) * secondRatio);
            double pickerZNode1 = pickerZ.ActualPosition + ((_targetPickerZ - pickerZ.ActualPosition) * firstRatio);

            nodes.Add(new PickerPickUpContiNode(0, pickerXNode0, needleXNode0, stageYNode0, pickerZ.ActualPosition));
            nodes.Add(new PickerPickUpContiNode(1, pickerXNode1, needleXNode1, stageYNode1, pickerZNode1));
            nodes.Add(new PickerPickUpContiNode(2, _targetPickerX, _targetNeedleX, _targetStageY, prePickTarget));
            nodes.Add(new PickerPickUpContiNode(3, _targetPickerX, _targetNeedleX, _targetStageY, _targetPickerZ));
            return nodes;
        }

        private bool CanUseContiSegmentedPickUpNodesFromCurrentPosition(
            InputStageUnit stage,
            BaseAxis pickerX,
            BaseAxis needleX,
            BaseAxis stageY,
            BaseAxis pickerZ,
            IList<PickerPickUpContiNode> nodes,
            PickerPickUpMotionConfig pickUpConfig,
            out string reason)
        {
            reason = string.Empty;

            if (nodes == null || nodes.Count == 0)
            {
                reason = "node list is empty.";
                return false;
            }

            double maxTravel = pickUpConfig != null ? pickUpConfig.TransferContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

            foreach (PickerPickUpContiNode node in nodes)
            {
                if (Math.Abs(node.PickerX - pickerX.ActualPosition) > maxTravel ||
                    Math.Abs(node.NeedleX - needleX.ActualPosition) > maxTravel ||
                    Math.Abs(node.StageY - stageY.ActualPosition) > maxTravel ||
                    Math.Abs(node.PickerZ - pickerZ.ActualPosition) > maxTravel)
                {
                    reason = "node" + node.Index + " target exceeds max travel.";
                    return false;
                }

                string areaReason;
                if (!stage.IsNeedleWorkPointInArea(node.NeedleX, node.StageY, out areaReason))
                {
                    reason = "node" + node.Index + " Needle work point is outside area. " + areaReason;
                    return false;
                }
            }

            return true;
        }

        private static double ResolveContiAsyncPrePickTriggerPosition(
            double pickerXStart,
            double pickerXTarget,
            PickerPickUpMotionConfig pickUpConfig)
        {
            double ratio = pickUpConfig != null ? pickUpConfig.TransferContiXYMidRatio : 0.5;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0.5;
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            return pickerXStart + ((pickerXTarget - pickerXStart) * ratio);
        }

        private static bool IsContiAsyncPrePickTriggerReached(
            double pickerXStart,
            double pickerXTarget,
            double pickerXTrigger,
            double pickerXActual)
        {
            double travel = pickerXTarget - pickerXStart;
            if (Math.Abs(travel) <= 0.000001)
                return true;

            return travel > 0.0
                ? pickerXActual >= pickerXTrigger
                : pickerXActual <= pickerXTrigger;
        }

        private async Task<int> MovePickerZPrePickAfterPickerXProgressAsync(
            BaseAxis pickerX,
            PickerAxis pickerZ,
            double pickerZAvoid,
            double pickerXStart,
            double pickerXTrigger,
            PickerPickUpMotionConfig pickUpConfig,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (pickUpConfig == null || pickUpConfig.PickerZPrePickDistance <= 0.0)
                    return 0;

                int timeoutMs = Math.Max(1000, Math.Max(pickUpConfig.TransferContiTimeoutMs, ResolveTimeout()));
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                DateTime stoppedCheckDeadline = DateTime.UtcNow.AddMilliseconds(200);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    double pickerXActual = pickerX != null ? pickerX.ActualPosition : pickerXStart;
                    if (IsContiAsyncPrePickTriggerReached(pickerXStart, _targetPickerX, pickerXTrigger, pickerXActual))
                        break;

                    if (pickerX != null && pickerX.IsAlarm)
                    {
                        return Fail("PICKER-PICKUP-CONTI-PREPICK-TRIGGER", Name,
                            "PickUp ContiNode PickerZ PrePick trigger wait failed. PickerX alarm is ON. " +
                            BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
                    }

                    if (DateTime.UtcNow >= stoppedCheckDeadline &&
                        pickerX != null &&
                        !pickerX.IsMoving &&
                        !IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX))
                    {
                        return Fail("PICKER-PICKUP-CONTI-PREPICK-TRIGGER", Name,
                            "PickUp ContiNode PickerZ PrePick trigger wait failed. PickerX stopped before trigger. " +
                            "start=" + pickerXStart.ToString("F6") +
                            ", trigger=" + pickerXTrigger.ToString("F6") +
                            ", actual=" + pickerXActual.ToString("F6") +
                            ", " + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
                    }

                    if (DateTime.UtcNow >= deadline)
                    {
                        return Fail("PICKER-PICKUP-CONTI-PREPICK-TRIGGER-TIMEOUT", Name,
                            "PickUp ContiNode PickerZ PrePick trigger timeout. " +
                            "timeoutMs=" + timeoutMs +
                            ", start=" + pickerXStart.ToString("F6") +
                            ", trigger=" + pickerXTrigger.ToString("F6") +
                            ", actual=" + pickerXActual.ToString("F6") +
                            ", " + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode PickerZ PrePick trigger reached. " +
                    "pickerNo=" + _currentPickerNo +
                    ", start=" + pickerXStart.ToString("F6") +
                    ", trigger=" + pickerXTrigger.ToString("F6") +
                    ", actual=" + (pickerX != null ? pickerX.ActualPosition.ToString("F6") : "-") +
                    " - Start");

                return await MovePickerZPrePickAsync(pickerZ, pickerZAvoid, pickUpConfig, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-CONTI-PREPICK-EX", Name,
                    "PickUp ContiNode PickerZ PrePick trigger move exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool ArePickBatchTargetsCalculated()
        {
            if (_pickBatchItems.Count == 0)
                return false;

            for (int i = 0; i < _pickBatchItems.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_pickBatchItems[i].TargetFormula))
                    return false;
            }

            return true;
        }

        private static bool IsAxisInTarget(BaseAxis axis, double target)
        {
            if (axis == null || axis.IsMoving)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private async Task<int> WaitContiSegmentedPickUpFinalPositionAsync(
            InputStageUnit stage,
            PickerAxis pickerZAxis,
            double pickerZFinalTarget,
            int timeoutMs,
            CancellationToken ct)
        {
            AxisMoveWaitResult pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait == null || !pickerXWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-CONTI-PICKER-X", pickerXWait), Name,
                    "PickUp ContiNode PickerX final wait failed. " +
                    FormatAxisMoveWaitResult(pickerXWait, BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX)));
            }

            int result = await WaitInputStageAxisInPositionResultAsync(
                stage,
                WaferStageAxis.NeedleX,
                _targetNeedleX,
                "PickUp ContiNode NeedleX final",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitInputStageAxisInPositionResultAsync(
                stage,
                WaferStageAxis.WaferY,
                _targetStageY,
                "PickUp ContiNode StageY final",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            AxisMoveWaitResult pickerZWait = await WaitPickerAxisMoveDoneAsync(
                pickerZAxis,
                pickerZFinalTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerZWait == null || !pickerZWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-CONTI-PICKER-Z", pickerZWait), Name,
                    "PickUp ContiNode PickerZ final wait failed. " +
                    FormatAxisMoveWaitResult(pickerZWait, BuildPickerAxisState(pickerZAxis, pickerZFinalTarget)));
            }

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleX, _targetNeedleX, "PickUp ContiNode NeedleX final");
            if (result != 0)
                return result;

            result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, _targetStageY, "PickUp ContiNode StageY final");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX, "PickUp ContiNode PickerX final");
            if (result != 0)
                return result;

            return CheckPickerAxisInPosition(pickerZAxis, pickerZFinalTarget, "PickUp ContiNode PickerZ final");
        }

        private static bool IsAxisReadyForContiPickUp(BaseAxis axis, string name, out string reason)
        {
            reason = string.Empty;

            if (axis == null)
            {
                reason = name + " axis is null.";
                return false;
            }

            if (axis.IsMoving)
            {
                reason = name + " is moving. " + FormatAxisForContinuousCheck(axis);
                return false;
            }

            if (!axis.IsServoOn)
            {
                reason = name + " servo is off. " + FormatAxisForContinuousCheck(axis);
                return false;
            }

            if (axis.IsAlarm)
            {
                reason = name + " alarm is on. " + FormatAxisForContinuousCheck(axis);
                return false;
            }

            if (axis.Setup == null || axis.Setup.AxisNo < 0)
            {
                reason = name + " axis number is not configured.";
                return false;
            }

            return true;
        }

        private bool IsNeedleWorkPathInAreaForContiPickUp(
            InputStageUnit stage,
            double startNeedleX,
            double startStageY,
            double targetNeedleX,
            double targetStageY,
            out string reason)
        {
            reason = string.Empty;

            if (stage == null)
            {
                reason = "InputStageUnit is null.";
                return false;
            }

            for (int i = 0; i <= 8; i++)
            {
                double ratio = i / 8.0;
                double x = startNeedleX + ((targetNeedleX - startNeedleX) * ratio);
                double y = startStageY + ((targetStageY - startStageY) * ratio);
                string areaReason;
                if (!stage.IsNeedleWorkPointInArea(x, y, out areaReason))
                {
                    reason = "Needle work path sample is outside area. sample=" + i +
                        ", x=" + x.ToString("0.###") +
                        ", y=" + y.ToString("0.###") +
                        ", reason=" + areaReason;
                    return false;
                }
            }

            reason = "Needle work path samples are inside area.";
            return true;
        }

        private static string FormatTravel(BaseAxis axis, double target)
        {
            if (axis == null)
                return "-";

            return Math.Abs(target - axis.ActualPosition).ToString("0.###");
        }

        private string BuildPickMoveTargetName()
        {
            string targetName = BuildPickerTargetName("DiePickPosition", _currentPickerIndex);
            if (Options != null && Options.RunMode == SequenceRunMode.Auto && _pickCursor > 0)
                return AppendAutoProcessCorrectionTargetTag(targetName + ";PickerPhase=InspectionZHold;InspectionContinuous;From=Input;To=Input");

            return AppendAutoProcessCorrectionTargetTag(targetName);
        }

        private string BuildPickUpInputStageMoveTargetPrefix()
        {
            return "PickerPickUp;Side=" + Side + ";PickerZone=Input;Owner=" + Name;
        }

        private string BuildPickUpInputStageMoveTargetName(WaferStageAxis axis, string phase)
        {
            return BuildPickUpInputStageMoveTargetPrefix() +
                ";StageAxis=" + axis +
                ";Phase=" + (phase ?? string.Empty);
        }

        private async Task<int> EnsurePickerYAtAvoidBeforePickMoveAsync(CancellationToken ct)
        {
            try
            {
                string continuousDetail;
                if (CanKeepPickerYForwardForContinuousPick(out continuousDetail))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " 연속 PickUp X/Y/T 제한 및 외부 간섭 확인 완료. PickerY Avoid 복귀를 생략합니다. " +
                        continuousDetail + " - Ok");
                    return 0;
                }

                double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, avoid))
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 안전 진입: PickerY가 이미 Avoid 위치입니다. " +
                        "Picker X/T 이동 완료 후에만 PickerY 전진을 시작합니다. " +
                        "pickIndex=" + (_pickCursor + 1) +
                        "/" + _pickBatchItems.Count +
                        ", pickerNo=" + _currentPickerNo + " - Check");
                    return 0;
                }

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "PickUp 안전 진입 PickerY Avoid 이동 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    avoid,
                    "pick picker Y avoid before X/T",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 안전 진입: Picker X/T 이동 전에 PickerY를 Avoid로 정리했습니다. " +
                    "pickIndex=" + (_pickCursor + 1) +
                    "/" + _pickBatchItems.Count +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
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

        private bool CanKeepPickerYForwardForContinuousPick(out string detail)
        {
            detail = string.Empty;

            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    detail = "runMode is not Auto.";
                    return false;
                }

                if (_pickCursor <= 0)
                {
                    detail = "first pick in batch.";
                    return false;
                }

                BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis pickerY = GetPickerAxis(PickerAxis.PickerY);
                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                BaseAxis pickerT = GetPickerAxis(tAxis);
                if (pickerX == null || pickerY == null || pickerT == null)
                {
                    detail = "picker axis missing. pickerX=" + FormatAxisForContinuousCheck(pickerX) +
                        ", pickerY=" + FormatAxisForContinuousCheck(pickerY) +
                        ", pickerT=" + FormatAxisForContinuousCheck(pickerT);
                    return false;
                }

                if (pickerX.IsMoving || pickerY.IsMoving || pickerT.IsMoving)
                {
                    detail = "picker X/Y/T is moving. pickerX=" + FormatAxisForContinuousCheck(pickerX) +
                        ", pickerY=" + FormatAxisForContinuousCheck(pickerY) +
                        ", pickerT=" + FormatAxisForContinuousCheck(pickerT);
                    return false;
                }

                double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, avoid))
                {
                    detail = "pickerY is already Avoid.";
                    return false;
                }

                double deltaX = Math.Abs(_targetPickerX - pickerX.ActualPosition);
                double deltaY = Math.Abs(_targetPickerY - pickerY.ActualPosition);
                double deltaT = Math.Abs(_targetPickerT - pickerT.ActualPosition);
                if (deltaX > ContinuousPickMaxDeltaX ||
                    deltaY > ContinuousPickMaxDeltaY ||
                    deltaT > ContinuousPickMaxDeltaT)
                {
                    detail = "continuous delta limit exceeded. deltaX=" + deltaX.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaX.ToString("0.###") +
                        ", deltaY=" + deltaY.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaY.ToString("0.###") +
                        ", deltaT=" + deltaT.ToString("0.###") +
                        "/" + ContinuousPickMaxDeltaT.ToString("0.###");
                    return false;
                }

                string zDetail;
                if (!ArePickerZAxesSafeForContinuousPick(out zDetail))
                {
                    detail = "PickerZ is not safe. " + zDetail;
                    return false;
                }

                InputStageUnit stage = ResolveInputStage();
                string visionDetail;
                if (!IsInputVisionXSafeForContinuousPick(stage, out visionDetail))
                {
                    detail = "InputVisionX is not safe. " + visionDetail;
                    return false;
                }

                string inputZDetail;
                if (!AreInputPickZAxesSafeBeforeContinuousXYT(stage, out inputZDetail))
                {
                    detail = "Input pick Z axes are not safe. " + inputZDetail;
                    return false;
                }

                string oppositeDetail;
                if (IsOppositePickerInputInterferenceActive(out oppositeDetail))
                {
                    detail = "opposite picker blocks Input. " + oppositeDetail;
                    return false;
                }

                string facingDetail;
                if (!IsFrontRearPickerXFacingPrecheckClear(_targetPickerX, out facingDetail))
                {
                    detail = "Front/Rear PickerX facing precheck blocked. " + facingDetail;
                    return false;
                }

                detail = "pickIndex=" + (_pickCursor + 1) +
                    "/" + _pickBatchItems.Count +
                    ", pickerNo=" + _currentPickerNo +
                    ", deltaX=" + deltaX.ToString("0.###") +
                    ", deltaY=" + deltaY.ToString("0.###") +
                    ", deltaT=" + deltaT.ToString("0.###") +
                    ", facing=" + facingDetail;
                return true;
            }
            catch (Exception ex)
            {
                detail = "continuous pick check exception. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerInputInterferenceActive(out string detail)
        {
            detail = string.Empty;

            try
            {
                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                if (!IsOppositePickerUnitAvailable())
                    return false;

                PickerWorkZone workZone;
                string owner;
                bool workAreaActive = PickerZoneInterlockRules.TryGetPickerWorkArea(
                    oppositeIsFront,
                    out workZone,
                    out owner);
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    Context != null ? Context.Machine : null,
                    oppositeIsFront,
                    PickerWorkZone.Input,
                    null,
                    "PickUp continuous input interference check");

                bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
                bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
                bool activeYInput = PickerZoneInterlockRules.GetPickerYActiveTargetZone(oppositeIsFront) == PickerWorkZone.Input;
                bool inputRelated = state != null &&
                    (state.CurrentZone == PickerWorkZone.Input ||
                     state.TargetZone == PickerWorkZone.Input ||
                     state.UnknownUnsafe ||
                     state.BlocksTransport);
                bool movingInputRisk = (xMoving || yMoving) && inputRelated;
                bool inputWorkArea = workAreaActive && workZone == PickerWorkZone.Input;
                bool blocks = inputWorkArea || activeYInput || inputRelated || movingInputRisk;

                detail = "opposite=" + (oppositeIsFront ? "FrontPicker" : "RearPicker") +
                    ", blocks=" + blocks +
                    ", inputWorkArea=" + inputWorkArea +
                    ", workArea=" + (workAreaActive ? workZone.ToString() : "None") +
                    ", owner=" + (workAreaActive ? owner : "-") +
                    ", activeYInput=" + activeYInput +
                    ", movingX=" + xMoving +
                    ", movingY=" + yMoving +
                    ", inputRelated=" + inputRelated +
                    ", movingInputRisk=" + movingInputRisk +
                    ", state=" + (state != null ? state.Describe() : "null");

                return blocks;
            }
            catch (Exception ex)
            {
                detail = "opposite picker Input interference check failed. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerUnitAvailable()
        {
            return Side == PickerSequenceSide.Front ? RearPicker != null : FrontPicker != null;
        }

        private bool ArePickerZAxesSafeForContinuousPick(out string detail)
        {
            detail = string.Empty;

            PickerAxis[] zAxes =
            {
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = GetPickerAxis(zAxis);
                if (axis == null)
                    continue;

                if (axis.IsMoving)
                {
                    detail = zAxis + " is moving. " + FormatAxisForContinuousCheck(axis);
                    return false;
                }

                double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;
                bool homeOrAbove = axis.ActualPosition >= -tolerance;
                bool atAvoid = Math.Abs(axis.ActualPosition - avoid) <= tolerance;
                if (!homeOrAbove && !atAvoid)
                {
                    detail = zAxis + " is not at home/avoid. actual=" +
                        axis.ActualPosition.ToString("0.###") +
                        ", avoid=" + avoid.ToString("0.###") +
                        ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }
            }

            detail = "PickerZ1~4 home/avoid.";
            return true;
        }

        private bool IsInputVisionXSafeForContinuousPick(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (stage == null || stage.CameraX == null)
                    return true;

                if (stage.CameraX.IsMoving)
                {
                    detail = "InputVisionX is moving. actual=" +
                        stage.CameraX.ActualPosition.ToString("0.###") +
                        ", command=" + stage.CameraX.CommandPosition.ToString("0.###");
                    return false;
                }

                if (!_inputVisionPickerEntryTargetPrepared ||
                    !IsAxisInTarget(stage.CameraX, _inputVisionPickerEntryTarget) ||
                    stage.CameraX.ActualPosition > 0.0)
                {
                    detail = "InputVisionX가 확정된 피커 진입 회피 위치가 아닙니다. actual=" +
                        stage.CameraX.ActualPosition.ToString("0.###") +
                        ", target=" + (_inputVisionPickerEntryTargetPrepared
                            ? _inputVisionPickerEntryTarget.ToString("0.###")
                            : "미확정") +
                        ", entryLimit=0";
                    return false;
                }

                detail = "InputVisionX 피커 진입 회피 위치 확인 완료.";
                return true;
            }
            catch (Exception ex)
            {
                detail = "InputVisionX safety check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool AreInputPickZAxesSafeBeforeContinuousXYT(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (stage == null)
                    return true;

                if (stage.NeedleZ != null && stage.NeedleZ.IsMoving)
                {
                    detail = "NeedleZ is moving. actual=" + stage.NeedleZ.ActualPosition.ToString("0.###");
                    return false;
                }

                if (stage.EjectPinZ != null && stage.EjectPinZ.IsMoving)
                {
                    detail = "EjectPinZ is moving. actual=" + stage.EjectPinZ.ActualPosition.ToString("0.###");
                    return false;
                }

                string needleZTeachingDetail = string.Empty;
                if (!stage.IsNeedleZInHomeOrSafePosition() &&
                    !CanKeepNeedleZAtTeachingPositionForPickMove(stage, out needleZTeachingDetail))
                {
                    detail = "NeedleZ is not home/safe. actual=" +
                        (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("0.###") : "null") +
                        ", teachingCheck=" + needleZTeachingDetail;
                    return false;
                }

                double ejectAvoid = stage.Recipe != null && stage.Recipe.EjectPinZ != null
                    ? stage.Recipe.EjectPinZ.AvoidPosition
                    : 0.0;
                if (stage.EjectPinZ != null)
                {
                    double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                        ? stage.EjectPinZ.Config.InPositionTolerance
                        : 0.01;
                    bool atAvoid = Math.Abs(stage.EjectPinZ.ActualPosition - ejectAvoid) <= tolerance;
                    bool homeOrBelow = stage.EjectPinZ.ActualPosition <= tolerance;
                    if (!atAvoid && !homeOrBelow)
                    {
                        detail = "EjectPinZ is not avoid/home. actual=" +
                            stage.EjectPinZ.ActualPosition.ToString("0.###") +
                            ", avoid=" + ejectAvoid.ToString("0.###") +
                            ", tolerance=" + tolerance.ToString("0.###");
                        return false;
                    }
                }

                detail = "NeedleZ/EjectPinZ safe. " + needleZTeachingDetail;
                return true;
            }
            catch (Exception ex)
            {
                detail = "Input pick Z safety check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool CanKeepNeedleZAtTeachingPositionForPickMove(InputStageUnit stage, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    detail = "runMode is not Auto.";
                    return false;
                }

                if (stage == null)
                {
                    detail = "InputStageUnit is null.";
                    return false;
                }

                BaseAxis needleZ = stage.NeedleZ;
                if (needleZ == null)
                {
                    detail = "NeedleZ axis is null.";
                    return true;
                }

                if (needleZ.IsMoving)
                {
                    detail = "NeedleZ is moving. actual=" + needleZ.ActualPosition.ToString("0.###");
                    return false;
                }

                double tolerance = needleZ.Config != null && needleZ.Config.InPositionTolerance > 0.0
                    ? needleZ.Config.InPositionTolerance
                    : 0.01;

                string teachingName;
                if (!IsNeedleZAtAutoPickTeachingPosition(stage, needleZ.ActualPosition, tolerance, out teachingName))
                {
                    detail = "NeedleZ is not at known Auto Pick teaching position. actual=" +
                        needleZ.ActualPosition.ToString("0.###") +
                        ", tolerance=" + tolerance.ToString("0.###");
                    return false;
                }

                double currentNeedleX = stage.NeedleBlockX != null
                    ? stage.NeedleBlockX.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterX();
                double currentStageY = stage.StageY != null
                    ? stage.StageY.ActualPosition
                    : stage.ResolveNeedleWorkAreaCenterY();

                string currentAreaReason;
                if (!stage.IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out currentAreaReason))
                {
                    detail = "current Needle work point is outside area. needleX=" +
                        currentNeedleX.ToString("0.###") +
                        ", stageY=" + currentStageY.ToString("0.###") +
                        ", reason=" + currentAreaReason;
                    return false;
                }

                string targetAreaReason;
                if (!stage.IsNeedleWorkPointInArea(_targetNeedleX, _targetStageY, out targetAreaReason))
                {
                    detail = "target Needle work point is outside area. needleX=" +
                        _targetNeedleX.ToString("0.###") +
                        ", stageY=" + _targetStageY.ToString("0.###") +
                        ", reason=" + targetAreaReason;
                    return false;
                }

                bool moveNeedleXFirst;
                string orderReason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(
                    _targetNeedleX,
                    _targetStageY,
                    out moveNeedleXFirst,
                    out orderReason))
                {
                    detail = "NeedleX/StageY safe order not found. " + orderReason;
                    return false;
                }

                detail = "NeedleZ keep allowed. teaching=" + teachingName +
                    ", actual=" + needleZ.ActualPosition.ToString("0.###") +
                    ", currentNeedleX=" + currentNeedleX.ToString("0.###") +
                    ", currentStageY=" + currentStageY.ToString("0.###") +
                    ", targetNeedleX=" + _targetNeedleX.ToString("0.###") +
                    ", targetStageY=" + _targetStageY.ToString("0.###") +
                    ", order=" + (moveNeedleXFirst ? "NeedleX->StageY" : "StageY->NeedleX") +
                    ", reason=" + orderReason;
                return true;
            }
            catch (Exception ex)
            {
                detail = "NeedleZ teaching keep check failed. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool IsNeedleZAtAutoPickTeachingPosition(
            InputStageUnit stage,
            double actual,
            double tolerance,
            out string teachingName)
        {
            teachingName = string.Empty;

            if (actual <= 0.0 + tolerance)
            {
                teachingName = "Home";
                return true;
            }

            if (stage == null || stage.Recipe == null || stage.Recipe.NeedleZ == null)
                return false;

            stage.Recipe.EnsurePositionObjects();
            StageAxisPositions needleZ = stage.Recipe.NeedleZ;

            if (IsPositionNear(actual, needleZ.AvoidPosition, tolerance))
            {
                teachingName = "AvoidPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.ProcessPosition, tolerance))
            {
                teachingName = "ProcessPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.ReadyPosition, tolerance))
            {
                teachingName = "ReadyPosition";
                return true;
            }

            if (IsPositionNear(actual, needleZ.NeedlePinCalPosition, tolerance))
            {
                teachingName = "NeedlePinCalPosition";
                return true;
            }

            if (!double.IsNaN(_targetNeedleZ) &&
                !double.IsInfinity(_targetNeedleZ) &&
                IsPositionNear(actual, _targetNeedleZ, tolerance))
            {
                teachingName = "CurrentPickTarget";
                return true;
            }

            return false;
        }

        private static bool IsPositionNear(double actual, double target, double tolerance)
        {
            return Math.Abs(actual - target) <= tolerance;
        }

        private bool IsFrontRearPickerXFacingPrecheckClear(double ownTargetX, out string detail)
        {
            detail = string.Empty;

            try
            {
                CDT320_Machine machine = Context != null ? Context.Machine : null;
                if (machine == null)
                    return true;

                BaseAxis ownX = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis ownY = GetPickerAxis(PickerAxis.PickerY);
                BaseAxis otherX = GetOppositePickerAxis(PickerAxis.PickerX);
                BaseAxis otherY = GetOppositePickerAxis(PickerAxis.PickerY);
                if (ownX == null || otherX == null)
                    return true;

                bool ownYOut = ownY != null && !IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"));
                bool otherYOut = IsOppositePickerYOut(otherY);
                if (!ownYOut || !otherYOut)
                {
                    detail = "one picker Y is safe. ownYOut=" + ownYOut + ", otherYOut=" + otherYOut;
                    return true;
                }

                double otherTargetX = otherX.IsMoving ? otherX.CommandPosition : otherX.ActualPosition;
                double ownMin = Math.Min(ownX.ActualPosition, ownTargetX) - ContinuousPickFacingPrecheckClearance;
                double ownMax = Math.Max(ownX.ActualPosition, ownTargetX) + ContinuousPickFacingPrecheckClearance;
                double otherMin = Math.Min(otherX.ActualPosition, otherTargetX);
                double otherMax = Math.Max(otherX.ActualPosition, otherTargetX);
                bool overlap = otherMax >= ownMin && otherMin <= ownMax;

                detail = "clearance=" + ContinuousPickFacingPrecheckClearance.ToString("0.###") +
                    ", ownX=" + ownX.ActualPosition.ToString("0.###") +
                    "->" + ownTargetX.ToString("0.###") +
                    ", otherX=" + otherX.ActualPosition.ToString("0.###") +
                    "->" + otherTargetX.ToString("0.###") +
                    ", ownY=" + FormatAxisForContinuousCheck(ownY) +
                    ", otherY=" + FormatAxisForContinuousCheck(otherY);
                return !overlap;
            }
            catch (Exception ex)
            {
                detail = "facing precheck exception. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private BaseAxis GetOppositePickerAxis(PickerAxis axis)
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    BaseAxis item;
                    if (RearPicker != null && RearPicker.Axes != null && RearPicker.Axes.TryGetValue(axis, out item))
                        return item;
                    return null;
                }

                BaseAxis frontItem;
                if (FrontPicker != null && FrontPicker.Axes != null && FrontPicker.Axes.TryGetValue(axis, out frontItem))
                    return frontItem;
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private bool IsOppositePickerYOut(BaseAxis otherY)
        {
            try
            {
                if (otherY == null)
                    return false;

                if (Math.Abs(otherY.ActualPosition) <= 0.05)
                    return false;

                bool oppositeIsFront = Side == PickerSequenceSide.Rear;
                if (oppositeIsFront)
                    return FrontPicker == null || !FrontPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

                return RearPicker == null || !RearPicker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private static string FormatAxisForContinuousCheck(BaseAxis axis)
        {
            if (axis == null)
                return "<null>";

            return axis.Name +
                "(actual=" + axis.ActualPosition.ToString("0.###") +
                ", command=" + axis.CommandPosition.ToString("0.###") +
                ", moving=" + (axis.IsMoving ? "Y" : "N") +
                ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") + ")";
        }

        private async Task<int> EnsureZAxesAtAvoidBeforePickerMoveAsync(
            InputStageUnit stage,
            string description,
            bool skipEjectPinZAvoid,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    description + " - 모든 PickerZ Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPickUpSequence",
                    Name + " " + description +
                    " - 공정 중 NeedleZ는 현재 위치를 유지하고, EjectPinZ 대기(Avoid)/Vacuum OFF 안정화는 StageY/NeedleX 이동 직전 전용 게이트에서 확인합니다. " +
                    "needleZActual=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") + " - Check");

                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                if (skipEjectPinZAvoid)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " " + description +
                        " - EjectPinZ Avoid is deferred to PickUp transfer pre-correction. " +
                        BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                        " - Check");
                }
                else
                {
                    result = await MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        ejectPinZAvoid,
                        description + " - EjectPinZ Avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 완료. " +
                    "needleZActual(유지)=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") +
                    ", " +
                    BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                    " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-MOVE-Z-AVOID-EX", Name,
                    description + " 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            if (IsInputStageAxisAlreadyInPosition(stage, axis, target))
                return 0;

            int result = await MoveInputStageAxisCommandAsync(
                stage,
                axis,
                target,
                description,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitInputStageAxisInPositionResultAsync(
                stage,
                axis,
                target,
                description,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return CheckInputStageAxisInPosition(stage, axis, target, description);
        }

        private int VerifyPickTarget()
        {
            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("VerifyPickTarget");

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

            WriteLog("PickerPickTargetVerify",
                Name + " pick target verified after move. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", formula=" + (_targetFormula ?? "") +
                ", stageYState=" + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, _targetStageY) +
                ", needleXState=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, _targetNeedleX) +
                ", pickerXState=" + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX) +
                ", pickerYState=" + BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                ", pickerTState=" + BuildPickerAxisState(GetPickerTAxis(_currentPickerIndex), _targetPickerT) +
                " - Ok");

            if (_pickerZContactedByContiPickUp)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode Contact 완료 상태이므로 Picker Empty 사전 Flow 확인을 생략합니다. " +
                    "Vacuum은 Contact 전에 이미 ON 처리되었습니다. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo + " - Check");
                CurrentStep = PickerPickUpStep.MovePickerZPick;
                return 0;
            }

            CurrentStep = PickerPickUpStep.VerifyPickerEmptyBeforePick;
            return 0;
        }

        private async Task<int> VerifyPickerEmptyBeforePickAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (ShouldBlockNewPickForWaferCompletion())
                    return StopRemainingPickBatchForWaferCompletion("VerifyPickerEmptyBeforePick");

                await Task.CompletedTask.ConfigureAwait(false);

                if (IsPickUpProductPrecheckBypassed())
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 시작 전 Picker 제품 유/무 확인은 Simulation/DryRun 조건으로 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId + " - Bypass");
                    CurrentStep = PickerPickUpStep.MovePickerZPick;
                    return 0;
                }

                bool vacuumOn;
                string vacuumStateReason;
                if (!TryReadPickerVacuumOutputOn(_currentPickerNo, out vacuumOn, out vacuumStateReason))
                {
                    return Fail("PICKER-PICKUP-PRE-VACUUM-STATE", Name,
                        "PickUp 시작 전 Picker Vacuum 출력 상태 확인 실패. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", reason=" + vacuumStateReason);
                }

                if (!vacuumOn)
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 시작 전 Picker 제품 유/무 확인 생략. " +
                        "Vacuum 출력이 OFF이므로 Flow 사전 확인을 하지 않습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", vacuum=OFF" +
                        ", flowCheck=Skipped - Ok");

                    CurrentStep = PickerPickUpStep.MovePickerZPick;
                    return 0;
                }

                bool flowOn = ReadPickerFlowState(_currentPickerNo);
                if (flowOn)
                {
                    return Fail("PICKER-PICKUP-PRE-FLOW-DETECTED", Name,
                        "PickUp 시작 전 Picker 제품 유/무 확인 실패. " +
                        "기존 Vacuum ON 상태에서 Flow 신호가 ON입니다. Picker가 이미 제품을 가지고 있으므로 PickUp을 진행하지 않습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDieId +
                        ", vacuum=ON" +
                        ", expectedFlow=OFF, actualFlow=ON");
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 시작 전 Picker 제품 유/무 확인 완료. " +
                    "기존 Vacuum ON 상태에서 Flow 신호가 OFF이므로 Picker가 비어 있다고 판단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", vacuum=ON" +
                    ", flow=OFF - Ok");

                CurrentStep = PickerPickUpStep.MovePickerZPick;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-PRE-FLOW-CHECK-EX", Name,
                    "PickUp 시작 전 Picker 제품 유/무 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsPickUpProductPrecheckBypassed()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && (settings.BypassHardware || settings.SimulationMode || settings.DryRunMode))
                    return true;

                return Context != null && Context.Controller != null && Context.Controller.GlobalDryRun;
            }
            catch
            {
                return false;
            }
        }

        private bool TryReadPickerVacuumOutputOn(int pickerNo, out bool vacuumOn, out string reason)
        {
            vacuumOn = false;
            reason = string.Empty;

            try
            {
                QMC.Common.IO.BaseDigitalOutput[] outputs = Side == PickerSequenceSide.Front && FrontPicker != null
                    ? FrontPicker.Vacuums
                    : Side == PickerSequenceSide.Rear && RearPicker != null
                        ? RearPicker.Vacuums
                        : null;

                int index = pickerNo - 1;
                if (outputs == null || index < 0 || index >= outputs.Length || outputs[index] == null)
                {
                    reason = "Picker Vacuum output is not configured. side=" + Side +
                             ", pickerNo=" + pickerNo +
                             ", index=" + index;
                    return false;
                }

                vacuumOn = outputs[index].IsOn;
                return true;
            }
            catch (Exception ex)
            {
                reason = "Exception occurred while reading Picker Vacuum output state. " + ex.Message;
                return false;
            }
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

                _currentPickSafeReturnCompleted = true;
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
                if (CurrentStep == PickerPickUpStep.Complete)
                    return 0;

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

                result = await VerifyPickerEmptyBeforePickAsync(ct).ConfigureAwait(false);
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
                if (CurrentStep == PickerPickUpStep.Complete)
                    return 0;

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

                result = await VerifyPickerEmptyBeforePickAsync(ct).ConfigureAwait(false);
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
            bool contiContactFlow = _pickerZContactedByContiPickUp;
            try
            {
                ct.ThrowIfCancellationRequested();

                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                PickerAxis pickerZ = GetPickerZAxis(_currentPickerIndex);
                double pickerZAvoid = GetPickerTeachingPosition(pickerZ, "AvoidPosition");
                string syncLiftSettleSource;
                int syncLiftSettleMs = ResolvePickUpSyncLiftSettleMs(config, out syncLiftSettleSource);

                WriteLog("PickerPickUpZ",
                    Name + " PickUp Z motion mode. mode=" + config.MotionMode +
                    ", syncLiftSettleMs=" + syncLiftSettleMs +
                    ", syncLiftSettleSource=" + syncLiftSettleSource +
                    ", pickSettleMs=" + config.PickSettleMs + " - Check");

                if (contiContactFlow)
                {
                    return await RunPickupZMotionAfterContiContactAsync(
                        pickerZ,
                        pickerZAvoid,
                        config,
                        updateMaterialInspection,
                        ct).ConfigureAwait(false);
                }

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

                result = await MovePickerZSlowToContactAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
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
                if (contiContactFlow)
                    _pickerZContactedByContiPickUp = false;
            }
        }

        private async Task<int> RunPickupZMotionAfterContiContactAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            PickerPickUpMotionConfig config,
            bool updateMaterialInspection,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int check = CheckPickerAxisInPosition(
                    pickerZ,
                    _targetPickerZ,
                    "PickUp ContiNode Contact 완료 PickerZ");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " PickUp ContiNode Contact 이후 Z 세부 모션 시작. " +
                    "Contact/EjectPinZ Ready 완료 후 SyncLift/Separate/Verify/Safe만 실행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", targetPickerZ=" + _targetPickerZ.ToString("F6") +
                    ", pickerZAvoid=" + pickerZAvoid.ToString("F6") + " - Start");

                int result = MoveEjectPinPickerZStartPositionCheck(pickerZ);
                if (result != 0)
                    return result;

                result = await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
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
                return Fail("PICKER-PICKUP-CONTI-CONTACT-Z-RUN-EX", Name,
                    "PickUp ContiNode Contact 이후 Z 세부 모션 중 예외가 발생했습니다. error=" + ex.Message);
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
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex)).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOnForSimplePickAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await WaitAfterPickerZContactSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                InputStageUnit stage = ResolveInputStage();
                result = EnsureNeedleVacuumOffForPick(stage, "PickUp 단순 PickerZ AVOID 이동 전");
                if (result != 0)
                    return result;

                double pickerAvoidSpeedPercent = config != null ? config.PickerZAvoidReturnSpeedPercent : 10.0;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;

                result = await MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "PickUp 단순 PickerZ 상승",
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
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

        private int MoveEjectPinPickerZStartPositionCheck(PickerAxis pickerZ)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            int check = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "PickUp ContiNode Sync Lift 시작 PickerZ Contact 위치");
            if (check != 0)
                return check;

            check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp ContiNode Sync Lift 시작 NeedleZ 티칭 위치");
            if (check != 0)
                return check;

            return CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp ContiNode Sync Lift 시작 EjectPinZ 픽업 준비 위치");
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
                        return await MovePickerZSlowToContactAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
                    case PickerPickUpZManualStep.MoveEjectPinPickerZSyncLift:
                        return await MoveEjectPinPickerZSyncLiftAndSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
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

                Task<int> needleZMove;
                if (IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ))
                {
                    // 현재 기준: 공정 중 NeedleZ는 Pick teaching 위치를 유지하고 EjectPinZ만 왕복한다.
                    WriteLog("PickerPickUpZ",
                        Name + " PickUp NeedleZ teaching 유지. " +
                        BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, _targetNeedleZ) +
                        " - Ok");
                    needleZMove = Task.FromResult(0);
                }
                else
                {
                    needleZMove = MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.NeedleZ,
                        _targetNeedleZ,
                        "PickUp NeedleZ 픽업 준비 위치",
                        ct);
                }
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

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp EjectPinZ 픽업 준비 위치");
                if (check != 0)
                    return check;

                return EnsureNeedleVacuumOnForPick(stage, "PickUp Needle/EjectPin 준비 완료 후");
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

                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Vacuum ON Step");
                if (needleVacuumResult != 0)
                    return needleVacuumResult;

                SetPickerVacuum(_currentPickerNo, true);

                int contactSettleMs = ResolvePickerContactSettleMs(config);
                WriteLog("PickerPickUpZ",
                    Name + " PickUp Vacuum ON before contact. contactSettleMs=" + contactSettleMs +
                    ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                    ", pickerNo=" + _currentPickerNo +
                    " - Ok");

                await Task.CompletedTask.ConfigureAwait(false);
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

        private void RestorePickerVacuumBeforeDefaultFallbackIfNeeded(bool stateKnown, bool wasOn)
        {
            if (!stateKnown || wasOn)
                return;

            try
            {
                SetPickerVacuum(_currentPickerNo, false);
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode fallback 전 Picker Vacuum을 기존 OFF 상태로 복구했습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode fallback 전 Picker Vacuum OFF 복구 중 예외. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDieId +
                    ", error=" + ex.Message + " - Check");
            }
        }

        private int EnsureNeedleVacuumOnForPick(InputStageUnit stage, string reason)
        {
            if (stage == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-NO-STAGE", "InputStageUnit",
                    reason + " Needle Vacuum ON 실패: InputStageUnit is null.");

            if (stage.NeedleVacuum == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-NO-OUTPUT", "InputStageUnit",
                    reason + " Needle Vacuum 출력이 없습니다.");

            stage.NeedleVacuum.On();
            _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
            WriteLog("PickerPickUpZ",
                reason + " Needle Vacuum ON. outputOn=" + stage.NeedleVacuum.IsOn);

            return 0;
        }

        private int EnsureNeedleVacuumOffForPick(InputStageUnit stage, string reason)
        {
            if (stage == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-NO-STAGE", "InputStageUnit",
                    reason + " Needle Vacuum OFF 실패: InputStageUnit is null.");

            if (stage.NeedleVacuum == null)
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-NO-OUTPUT", "InputStageUnit",
                    reason + " Needle Vacuum 출력이 없습니다.");

            stage.NeedleVacuum.Off();
            if (stage.NeedleVacuum.IsOn)
            {
                _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
                return Fail("PICKER-PICKUP-NEEDLE-VAC-OFF-STATE", "InputStageUnit",
                    reason + " Needle Vacuum OFF 명령 후 출력이 계속 ON 상태입니다.");
            }

            _needleVacuumOffConfirmedAtUtc = DateTime.UtcNow;
            WriteLog("PickerPickUpZ",
                reason + " Needle Vacuum OFF. outputOn=" + stage.NeedleVacuum.IsOn);

            return 0;
        }

        private void TryNeedleVacuumOffForPick(InputStageUnit stage, string reason)
        {
            try
            {
                if (stage == null || stage.NeedleVacuum == null)
                {
                    WriteLog("PickerPickUpZ", reason + " Needle Vacuum OFF skip. output is null.");
                    return;
                }

                stage.NeedleVacuum.Off();
                _needleVacuumOffConfirmedAtUtc = stage.NeedleVacuum.IsOn
                    ? DateTime.MinValue
                    : DateTime.UtcNow;
                WriteLog("PickerPickUpZ",
                    reason + " Needle Vacuum OFF. outputOn=" + stage.NeedleVacuum.IsOn);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpZ", reason + " Needle Vacuum OFF 중 예외. error=" + ex.Message);
            }
        }

        private async Task<int> EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-STAGE", "InputStageUnit",
                        description + " 실패: InputStageUnit이 없습니다.");
                }
                if (stage.NeedleVacuum == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-VAC", "InputStageUnit",
                        description + " 실패: Needle Vacuum 출력이 없습니다.");
                }
                if (stage.Recipe == null || stage.Recipe.EjectPinZ == null)
                {
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-NO-RECIPE", stage.Name,
                        description + " 실패: EjectPinZ 대기(Avoid) 위치 정보가 없습니다.");
                }

                if (stage.NeedleVacuum.IsOn || _needleVacuumOffConfirmedAtUtc == DateTime.MinValue)
                {
                    int vacuumResult = EnsureNeedleVacuumOffForPick(stage, description);
                    if (vacuumResult != 0)
                        return vacuumResult;
                }

                // 공정 중 NeedleZ는 현재 위치를 유지하고, XY 이동 전에는 EjectPinZ만 대기(Avoid) 위치로 내린다.
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                int requiredVacuumOffMs = ResolveNeedleVacuumOffSettleBeforeXYMs();
                int result = await MoveInputStageAxisToAvoidAndVerifyIfNeededAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " - EjectPinZ Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CheckInputStageAxisInPosition(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " - EjectPinZ Avoid 최종 확인");
                if (result != 0)
                    return result;

                double elapsedMs = _needleVacuumOffConfirmedAtUtc == DateTime.MinValue
                    ? 0.0
                    : (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                int remainingMs = Math.Max(
                    0,
                    requiredVacuumOffMs - (int)Math.Floor(elapsedMs));
                if (remainingMs > 0)
                    await Task.Delay(remainingMs, ct).ConfigureAwait(false);

                if (stage.NeedleVacuum.IsOn)
                {
                    _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;
                    return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-VAC-ON", stage.Name,
                        description + " 실패: StageY/NeedleX 이동 직전 Needle Vacuum 출력이 ON 상태입니다.");
                }

                elapsedMs = _needleVacuumOffConfirmedAtUtc == DateTime.MinValue
                    ? 0.0
                    : (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                if (elapsedMs < requiredVacuumOffMs)
                {
                    int finalRemainingMs = Math.Max(
                        1,
                        requiredVacuumOffMs - (int)Math.Floor(elapsedMs));
                    await Task.Delay(finalRemainingMs, ct).ConfigureAwait(false);
                    elapsedMs = (DateTime.UtcNow - _needleVacuumOffConfirmedAtUtc).TotalMilliseconds;
                }

                WriteLog("PickerPickUpSequence",
                    Name + " " + description + " 완료. StageY/NeedleX 이동을 허용합니다. " +
                    "ejectPinZAvoid=" + ejectPinZAvoid.ToString("F6") +
                    ", ejectPinZActual=" + (stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition.ToString("F6") : "null") +
                    ", needleZActual(유지)=" + (stage.NeedleZ != null ? stage.NeedleZ.ActualPosition.ToString("F6") : "null") +
                    ", needleVacuumOn=" + stage.NeedleVacuum.IsOn +
                    ", vacuumOffElapsedMs=" + elapsedMs.ToString("F1") +
                    ", requiredMs=" + requiredVacuumOffMs +
                    ", delaySource=PickUp.NeedleVacuumOffSettleBeforeXYMs - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-EJECTPIN-XY-SAFE-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> VacuumOnForSimplePickAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Simple Vacuum ON Step");
                if (needleVacuumResult != 0)
                    return Task.FromResult(needleVacuumResult);

                SetPickerVacuum(_currentPickerNo, true);

                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("PICKER-PICKUP-SIMPLE-VACUUM-EX", Name,
                    "PickUp 단순 Z 모션 Vacuum ON 중 예외가 발생했습니다. error=" + ex.Message));
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
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex),
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

        private async Task<int> MoveEjectPinPickerZSyncLiftAsync(
            PickerAxis pickerZ,
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
                double syncLiftDistance = stage.Config.PickUpNeedleSyncLiftDistance;
                double ejectPinSyncLiftOffset = ResolveEjectPinZSyncLiftOffset(stage);

                syncTargets.PickerZ = _targetPickerZ + syncLiftDistance;
                syncTargets.NeedleZ = _targetNeedleZ;
                syncTargets.EjectPinZ = _targetEjectPinZ + syncLiftDistance + ejectPinSyncLiftOffset;
                syncTargets.EjectPinSyncLiftOffset = ejectPinSyncLiftOffset;
                _lastPickUpZTargets = syncTargets;

                if (syncLiftDistance <= 0.0)
                    return 0;

                int startCheck = CheckPickerAxisInPosition(pickerZ, _targetPickerZ, "PickUp Sync Lift 시작 PickerZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                startCheck = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, _targetNeedleZ, "PickUp Sync Lift 시작 NeedleZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                startCheck = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, _targetEjectPinZ, "PickUp Sync Lift 시작 EjectPinZ 티칭 위치");
                if (startCheck != 0)
                    return startCheck;

                return await MovePickerNeedleZSyncLiftFallbackAsync(
                    pickerZ,
                    stage,
                    syncTargets,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-EX", Name,
                    "PickUp PickerZ/EjectPinZ 개별 비동기 상승 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveEjectPinPickerZSyncLiftAndSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int result = await MoveEjectPinPickerZSyncLiftAsync(pickerZ, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            await WaitAfterSyncLiftSettleAsync(config, ct).ConfigureAwait(false);
            // Sync Lift는 이동 완료/InPosition 대기 결과를 사용하고 동일 목표의 최종 중복 체크는 수행하지 않는다.
            return 0;
        }

        private async Task WaitAfterSyncLiftSettleAsync(PickerPickUpMotionConfig config, CancellationToken ct)
        {
            string source;
            int waitMs = ResolvePickUpSyncLiftSettleMs(config, out source);
            if (waitMs <= 0)
                return;

            // 현재 기준: Sync Lift 직후 Separate 전에 필요한 안정화 대기만 적용한다.
            WriteLog("PickerPickUpZ",
                Name + " PickUp Sync Lift settle wait start. waitMs=" + waitMs +
                ", source=" + source + " - Wait");
            await Task.Delay(waitMs, ct).ConfigureAwait(false);
            WriteLog("PickerPickUpZ",
                Name + " PickUp Sync Lift settle wait complete. waitMs=" + waitMs +
                ", source=" + source + " - Ok");
        }

        private int ResolvePickUpSyncLiftSettleMs(PickerPickUpMotionConfig config, out string source)
        {
            source = "None";
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage != null && stage.Config != null)
                {
                    stage.Config.EnsurePickUpMotionDefaults();
                    int inputStageWaitMs = Math.Max(0, stage.Config.PickUpNeedleSyncLiftSettleMs);
                    source = "InputStage.PickUpNeedleSyncLiftSettleMs";
                    return inputStageWaitMs;
                }

                int pickerWaitMs = config != null ? Math.Max(0, config.SyncLiftSettleMs) : 0;
                if (pickerWaitMs > 0)
                    source = "Picker.PickUp.SyncLiftSettleMs";

                return pickerWaitMs;
            }
            catch
            {
                source = "Error";
                return config != null ? Math.Max(0, config.SyncLiftSettleMs) : 0;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZSlowToContactAndSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int result = await MovePickerZSlowToContactAsync(pickerZ, config, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await WaitAfterPickerZContactSettleAsync(pickerZ, config, ct).ConfigureAwait(false);
        }

        private async Task<int> WaitAfterPickerZContactSettleAsync(
            PickerAxis pickerZ,
            PickerPickUpMotionConfig config,
            CancellationToken ct)
        {
            int check = CheckPickerAxisInPosition(
                pickerZ,
                _targetPickerZ,
                "PickUp PickerZ Die Touch 위치 완료 확인");
            if (check != 0)
                return check;

            int contactSettleMs = ResolvePickerContactSettleMs(config);
            if (contactSettleMs <= 0)
                return 0;

            WriteLog("PickerPickUpZ",
                Name + " PickUp PickerZ contact settle wait start. waitMs=" + contactSettleMs +
                ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                ", pickerNo=" + _currentPickerNo +
                ", targetPickerZ=" + _targetPickerZ + " - Wait");
            await Task.Delay(contactSettleMs, ct).ConfigureAwait(false);
            WriteLog("PickerPickUpZ",
                Name + " PickUp PickerZ contact settle wait complete. waitMs=" + contactSettleMs +
                ", delaySource=PickUp.VacuumOnBeforePickDelayMs" +
                ", pickerNo=" + _currentPickerNo +
                ", targetPickerZ=" + _targetPickerZ + " - Ok");

            return 0;
        }

        private static int ResolvePickerContactSettleMs(PickerPickUpMotionConfig config)
        {
            return config != null ? Math.Max(0, config.VacuumOnBeforePickDelayMs) : 0;
        }

        private async Task<int> MovePickerNeedleZSyncLiftFallbackAsync(
            PickerAxis pickerZ,
            InputStageUnit stage,
            PickUpZTargets syncTargets,
            CancellationToken ct)
        {
            Task<int> pickerMove = MovePickerAxisWithMotionAndVerifyAsync(
                pickerZ,
                syncTargets.PickerZ,
                stage.Config.PickUpNeedleSyncLiftVelocity,
                stage.Config.PickUpNeedleSyncLiftAcc,
                stage.Config.PickUpNeedleSyncLiftDec,
                "PickUp PickerZ/EjectPinZ 개별 비동기 상승 PickerZ",
                "PickUpSyncLift",
                ct,
                true);
            Task<int> ejectPinMove = MoveInputStageAxisWithMotionAndVerifyAsync(
                stage,
                WaferStageAxis.EjectPinZ,
                syncTargets.EjectPinZ,
                stage.Config.PickUpNeedleSyncLiftVelocity,
                stage.Config.PickUpNeedleSyncLiftAcc,
                stage.Config.PickUpNeedleSyncLiftDec,
                "PickUp PickerZ/EjectPinZ 개별 비동기 상승 EjectPinZ",
                ct,
                null,
                true);

            int[] results = await Task.WhenAll(pickerMove, ejectPinMove).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT", Name,
                    "PickUp PickerZ/EjectPinZ 개별 비동기 상승 실패. " +
                    "pickerZResult=" + results[0] +
                    ", ejectPinZResult=" + results[1] +
                    ", " + BuildPickerAxisState(pickerZ, syncTargets.PickerZ) +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, syncTargets.EjectPinZ) +
                    ", NeedleZHold=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ));
            }

            int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ, "PickUp Sync Lift NeedleZ 티칭 위치 유지");
            if (check != 0)
                return check;

            WriteLog("PickerPickUpSyncLift",
                Name + " PickUp PickerZ/EjectPinZ async lift move wait complete. pickerNo=" + _currentPickerNo +
                ", ejectPinSyncLiftOffset=" + syncTargets.EjectPinSyncLiftOffset.ToString("F6") +
                ", pickerZState=" + BuildPickerAxisState(pickerZ, syncTargets.PickerZ) +
                ", ejectPinZState=" + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, syncTargets.EjectPinZ) +
                ", needleZHoldState=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, syncTargets.NeedleZ) +
                " - WaitSettle");

            return 0;
        }

        private static bool ShouldUseSimulatedSyncLiftFallback()
        {
            QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
            return settings == null ||
                   settings.SimulationMode ||
                   settings.DryRunMode ||
                   settings.BypassHardware ||
                   !settings.UseAjin ||
                   !QMC.CDT320.Ajin.AjinFactory.IsRealBoardReady;
        }

        private int CheckAxisReadyForInterpolatedSyncLift(QMC.Common.Motion.BaseAxis axis, string axisName)
        {
            if (axis == null)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-AXIS", Name, axisName + " 축을 찾을 수 없습니다.");
            if (axis.Setup == null || axis.Setup.AxisNo < 0)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-AXIS", Name, axisName + " 축 번호가 설정되지 않았습니다.");
            if (!axis.IsServoOn)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 서보가 OFF 상태입니다.");
            if (axis.IsAlarm)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 알람이 ON 상태입니다.");
            if (axis.IsMoving)
                return Fail("PICKER-PICKUP-Z-SYNC-LIFT-READY", Name, axisName + " 축이 이미 이동 중입니다.");

            return 0;
        }

        private static void StopSyncLiftAxes(params QMC.Common.Motion.BaseAxis[] axes)
        {
            if (axes == null)
                return;

            for (int i = 0; i < axes.Length; i++)
            {
                try
                {
                    if (axes[i] != null)
                        axes[i].Stop();
                }
                catch
                {
                }
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
                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);

                double pickerSeparateSpeedPercent = config != null ? config.PickerZSeparateSpeedPercent : 1.0;
                double pickerAvoidSpeedPercent = config != null ? config.PickerZAvoidReturnSpeedPercent : 10.0;
                double pickerSeparateDistance = config != null ? Math.Max(0.0, config.PickerZSeparateDistance) : 0.0;
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;
                double pickerSeparateStart = syncTargets != null ? syncTargets.PickerZ : GetPickerAxis(pickerZ).ActualPosition;
                double pickerSeparateTarget = ResolveTargetToward(pickerSeparateStart, pickerZAvoid, pickerSeparateDistance);
                double pickerVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerSeparateSpeedPercent);
                double pickerAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerSeparateSpeedPercent, true);
                double pickerDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerSeparateSpeedPercent, false);
                // 현재 기준: Separate 저속 구간 이후 PickerZ Avoid 최종 상승은 별도 속도로 복귀한다.
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                WriteLog("PickerPickUpZ",
                    "PickerZ separate speed resolved. axis=" + pickerZ +
                    ", start=" + pickerSeparateStart.ToString("0.###") +
                    ", target=" + pickerSeparateTarget.ToString("0.###") +
                    ", avoid=" + pickerZAvoid.ToString("0.###") +
                    ", distance=" + pickerSeparateDistance.ToString("0.###") +
                    ", percent=" + pickerSeparateSpeedPercent.ToString("0.###") +
                    ", velocity=" + pickerVelocity.ToString("0.###") +
                    ", acceleration=" + pickerAcceleration.ToString("0.###") +
                    ", deceleration=" + pickerDeceleration.ToString("0.###") +
                    ", avoidPercent=" + pickerAvoidSpeedPercent.ToString("0.###") +
                    ", avoidVelocity=" + pickerAvoidVelocity.ToString("0.###") +
                    ", avoidAcceleration=" + pickerAvoidAcceleration.ToString("0.###") +
                    ", avoidDeceleration=" + pickerAvoidDeceleration.ToString("0.###") +
                    ", pickerSafeForWaferStageDistance=" + pickerSafeForWaferStageDistance.ToString("0.###"));

                int pickerResult = await MovePickerAxisWithMotionAndVerifyAsync(
                    pickerZ,
                    pickerSeparateTarget,
                    pickerVelocity,
                    pickerAcceleration,
                    pickerDeceleration,
                    "PickUp Sync Lift 후 PickerZ Separate 이동",
                    "PickUpSeparateDistance",
                    ct).ConfigureAwait(false);
                if (pickerResult != 0)
                    return pickerResult;

                pickerResult = await MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "PickUp Sync Lift 후 PickerZ Avoid 최종 이동",
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
                if (pickerResult != 0)
                    return pickerResult;

                WriteLog("PickerPickUpZ",
                    Name + " PickerZ Stage Safe 도달 후 Needle Vacuum OFF 및 EjectPinZ Avoid 이동을 시작합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", safeDistance=" + pickerSafeForWaferStageDistance.ToString("0.###") +
                    ", ejectPinZAvoid=" + ejectPinZAvoid.ToString("0.###") + " - Start");

                return await MoveEjectPinZToAvoidKeepNeedleZAsync(
                    stage,
                    _targetNeedleZ,
                    ejectPinZAvoid,
                    ct).ConfigureAwait(false);
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

        private async Task<int> MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            double pickerTouchZ,
            double pickerSafeForWaferStageDistance,
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

                BaseAxis axis = GetPickerAxis(pickerZ);
                if (axis == null)
                    return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                        description + " 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);

                double safeDistance = PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(pickerSafeForWaferStageDistance);
                double safeTarget = ResolveTargetToward(pickerTouchZ, pickerZAvoid, safeDistance);
                double tolerance = ResolveAxisTolerance(axis);
                bool fullAvoidRequired = Math.Abs(safeTarget - pickerZAvoid) <= tolerance;
                bool alreadyMovingToAvoid = axis.IsMoving && Math.Abs(axis.CommandPosition - pickerZAvoid) <= tolerance;

                WriteLog("PickerPickUpZ",
                    description + " command/wait safe. axis=" + pickerZ +
                    ", touchZ=" + pickerTouchZ.ToString("0.###") +
                    ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                    ", safeDistance=" + safeDistance.ToString("0.###") +
                    ", safeTarget=" + safeTarget.ToString("0.###") +
                    ", fullAvoidRequired=" + fullAvoidRequired +
                    ", alreadyMovingToAvoid=" + alreadyMovingToAvoid +
                    ", velocity=" + velocity.ToString("0.###") +
                    ", acceleration=" + acceleration.ToString("0.###") +
                    ", deceleration=" + deceleration.ToString("0.###"));

                if (!alreadyMovingToAvoid)
                {
                    int commandResult = await MovePickerAxisCommandWithMotionAsync(
                        pickerZ,
                        pickerZAvoid,
                        velocity,
                        acceleration,
                        deceleration,
                        targetName).ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-PICKUP-Z-SAFE-CMD", Name,
                            description + " 이동 명령 실패. result=" + commandResult +
                            ", velocity=" + velocity +
                            ", acc=" + acceleration +
                            ", dec=" + deceleration +
                            ", " + BuildPickerAxisState(pickerZ, pickerZAvoid));
                }

                if (fullAvoidRequired)
                {
                    int waitResult = await WaitPickerAxisInPositionResultAsync(pickerZ, pickerZAvoid, description, ct).ConfigureAwait(false);
                    if (waitResult != 0)
                        return waitResult;

                    return CheckPickerAxisInPosition(pickerZ, pickerZAvoid, description);
                }

                return await WaitPickerZSafeForWaferStageAsync(
                    pickerZ,
                    pickerTouchZ,
                    pickerZAvoid,
                    safeTarget,
                    tolerance,
                    description,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-SAFE-EX", Name,
                    description + " 안전 상승 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerZSafeForWaferStageAsync(
            PickerAxis pickerZ,
            double pickerTouchZ,
            double pickerZAvoid,
            double safeTarget,
            double tolerance,
            string description,
            CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(pickerZ);
            if (axis == null)
                return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                    description + " 안전 상승 확인 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);

            double direction = Math.Sign(pickerZAvoid - pickerTouchZ);
            if (direction == 0.0)
                return 0;

            DateTime startedAt = DateTime.UtcNow;
            DateTime moveStartGraceUntil = startedAt.AddMilliseconds(250.0);
            bool sawMoving = axis.IsMoving;
            int timeoutMs = ResolveTimeout();

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                axis = GetPickerAxis(pickerZ);
                if (axis == null)
                    return Fail("PICKER-PICKUP-Z-SAFE-AXIS", Name,
                        description + " 안전 상승 확인 실패. PickerZ 축을 찾을 수 없습니다. axis=" + pickerZ);
                if (!axis.IsServoOn)
                    return Fail("PICKER-PICKUP-Z-SAFE-SERVO", Name,
                        description + " 안전 상승 확인 실패. PickerZ 서보가 OFF입니다. " + BuildPickerAxisState(pickerZ, safeTarget));
                if (axis.IsAlarm)
                    return Fail("PICKER-PICKUP-Z-SAFE-ALARM", Name,
                        description + " 안전 상승 확인 실패. PickerZ 알람이 ON입니다. " + BuildPickerAxisState(pickerZ, safeTarget));

                if (axis.IsMoving)
                    sawMoving = true;

                double actual = axis.ActualPosition;
                bool reached = direction > 0.0
                    ? actual >= safeTarget - tolerance
                    : actual <= safeTarget + tolerance;
                if (reached)
                {
                    WriteLog("PickerPickUpZ",
                        description + " safe height reached. axis=" + pickerZ +
                        ", touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", actual=" + actual.ToString("0.###") +
                        ", command=" + axis.CommandPosition.ToString("0.###") +
                        ", moving=" + axis.IsMoving + " - Ok");
                    return 0;
                }

                if (!axis.IsMoving && (sawMoving || DateTime.UtcNow >= moveStartGraceUntil))
                {
                    return Fail("PICKER-PICKUP-Z-SAFE-NOT-REACHED", Name,
                        description + " 안전 상승 거리 도달 전 PickerZ가 정지했습니다. " +
                        "touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", " + BuildPickerAxisState(pickerZ, safeTarget));
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
                {
                    return Fail("PICKER-PICKUP-Z-SAFE-TIMEOUT", Name,
                        description + " 안전 상승 거리 확인 timeout. " +
                        "timeoutMs=" + timeoutMs +
                        ", touchZ=" + pickerTouchZ.ToString("0.###") +
                        ", avoidZ=" + pickerZAvoid.ToString("0.###") +
                        ", safeTarget=" + safeTarget.ToString("0.###") +
                        ", " + BuildPickerAxisState(pickerZ, safeTarget));
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }
        }

        private static double ResolveAxisTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.001;
        }

        // 기존 안전 복구용: 실패/비상 상황에서는 NeedleZ까지 Avoid 복귀할 수 있게 유지한다.
        private async Task<int> MoveNeedlePinZToAvoidAndVacuumOffAsync(
            InputStageUnit stage,
            double needleTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, "PickUp 후 NeedlePinZ/NeedleZ AVOID 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                int ejectResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                int needleResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    "PickUp 후 NeedleZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (needleResult != 0)
                    return needleResult;

                ejectResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                needleResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    needleTarget,
                    "PickUp 후 NeedleZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (needleResult != 0)
                    return needleResult;

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectTarget, "PickUp 후 NeedlePinZ(EjectPinZ) Avoid 이동");
                if (check != 0)
                    return check;

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTarget, "PickUp 후 NeedleZ Avoid 이동");
                if (check != 0)
                    return check;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-NEEDLE-PIN-AVOID-VAC-OFF-EX", Name,
                    "PickUp 후 NeedlePinZ Avoid 및 Needle Vacuum OFF 중 예외가 발생했습니다. error=" + ex.Message);
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
            return await MovePickerEjectPinZToAvoidKeepNeedleZAsync(
                pickerZ,
                pickerZAvoid,
                _targetNeedleZ,
                "PickUp 완료 후 PickerZ/EjectPinZ 안전 복귀 및 NeedleZ teaching 유지",
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

        private int ResolveNeedleVacuumOffSettleBeforeXYMs()
        {
            PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
            if (config == null)
                return 100;

            return Math.Max(0, Math.Min(60000, config.NeedleVacuumOffSettleBeforeXYMs));
        }

        private async Task<int> MovePickerEjectPinZToAvoidKeepNeedleZAsync(
            PickerAxis pickerZ,
            double pickerZAvoid,
            double needleTeachingTarget,
            string description,
            CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            try
            {
                ct.ThrowIfCancellationRequested();

                double ejectPinZAvoid = ResolveEjectPinZAvoidTarget(stage);
                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                double pickerAvoidSpeedPercent = config != null ? config.PickerZAvoidReturnSpeedPercent : 10.0;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, description + " 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                // 현재 기준: 정상 PickUp 루프에서는 NeedleZ를 teaching 위치에 고정하고 PickerZ/EjectPinZ만 복귀한다.
                Task<int> pickerZMove = MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    description + " PickerZ",
                    "AvoidPosition",
                    ct);

                Task<int> ejectPinZMove = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid)
                    ? Task.FromResult(0)
                    : MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.EjectPinZ,
                        ejectPinZAvoid,
                        description + " EjectPinZ",
                        ct);

                int[] results = await Task.WhenAll(pickerZMove, ejectPinZMove).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-PICKUP-Z-EJECT-AVOID-KEEP-NEEDLE", Name,
                        description + " 실패. " +
                        "pickerZResult=" + results[0] +
                        ", ejectPinZResult=" + results[1] +
                        ", " + BuildPickerAxisState(pickerZ, pickerZAvoid) +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                        ", needleKeep=" + BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget));
                }

                int ejectResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectPinZAvoid,
                    description + " EjectPinZ",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid, description + " EjectPinZ");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " Needle Vacuum OFF 후 EjectPinZ Avoid 완료 확인. " +
                    "description=" + description +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectPinZAvoid) +
                    " - Ok");

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTeachingTarget, description + " NeedleZ teaching 유지");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " " + description + ". NeedleZ teaching 유지, " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget) +
                    " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-Z-EJECT-AVOID-KEEP-NEEDLE-EX", Name,
                    description + " 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerAxisWithMotionAndVerifyAsync(
            PickerAxis axis,
            double target,
            double velocity,
            double acceleration,
            double deceleration,
            string description,
            string targetName,
            CancellationToken ct,
            bool deferFinalPositionCheck = false)
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

                if (deferFinalPositionCheck)
                    return 0;

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
            CancellationToken ct,
            string guardTargetName = null,
            bool deferFinalPositionCheck = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int commandResult;
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item != null && !string.IsNullOrWhiteSpace(guardTargetName))
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        commandResult = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration),
                            ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    commandResult = await AwaitStepWithCancellationAsync(
                        stage.MoveInputStageAxisCommandWithMotion(axis, target, velocity, acceleration, deceleration),
                        ct).ConfigureAwait(false);
                }

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

                if (deferFinalPositionCheck)
                    return 0;

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
                    BuildPickerTargetName("DiePickPosition", _currentPickerIndex));
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
                PickerPickUpMotionConfig config = ResolvePickUpMotionConfig();
                double pickerAvoidSpeedPercent = config != null ? config.PickerZAvoidReturnSpeedPercent : 10.0;
                double pickerAvoidVelocity = ResolvePickerAxisVelocityByPercent(pickerZ, pickerAvoidSpeedPercent);
                double pickerAvoidAcceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, true);
                double pickerAvoidDeceleration = ResolvePickerAxisAccelerationByPercent(pickerZ, pickerAvoidSpeedPercent, false);
                double pickerSafeForWaferStageDistance = config != null
                    ? PickerPickUpMotionConfig.NormalizePickerSafeForWaferStageDistance(config.PickerSafeForWaferStageDistance)
                    : PickerPickUpMotionConfig.MinimumPickerSafeForWaferStageDistance;
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, description + " 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                Task<int> pickerZMove = MovePickerZToAvoidAndWaitSafeForWaferStageAsync(
                    pickerZ,
                    pickerZAvoid,
                    _targetPickerZ,
                    pickerSafeForWaferStageDistance,
                    pickerAvoidVelocity,
                    pickerAvoidAcceleration,
                    pickerAvoidDeceleration,
                    "pick Z avoid after pickup",
                    "AvoidPosition",
                    ct);
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
                TryNeedleVacuumOffForPick(stage, description + " 이동 전");

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
                InputStageUnit stage = ResolveInputStage();
                int needleVacuumResult = EnsureNeedleVacuumOnForPick(stage, "PickUp Vacuum ON");
                if (needleVacuumResult != 0)
                    return needleVacuumResult;

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

            _currentPickSafeReturnCompleted = true;
            CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
            return 0;
        }

        private int UpdateMaterialToPicker()
        {
            string pickedDieId = _currentDieId;
            int pickedPickerNo = _currentPickerNo;
            int pickedPickerIndex = _currentPickerIndex;

            bool materialUpdated = MaterialStateService.MarkDiePickedByPicker(_currentDieId, PickerLocationKind, _currentPickerNo);
            if (!materialUpdated)
                return Fail("PICKER-PICKUP-MATERIAL", Name, "Picked die material state update failed. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo);

            int verifyResult = VerifyPickerHasDieDataAndFlowAfterPick(pickedDieId, pickedPickerNo, pickedPickerIndex);
            if (verifyResult != 0)
                return verifyResult;

            RecordColletUse(_currentPickerNo);
            RecordBottomAutoFocusPickCount(_currentPickerNo, MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo));
            SaveRuntimeState(Name + ":PickUp:ColletUse:" + _currentPickerNo);
            WriteLog("PickerPickUpSequence", Name + " picked die. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");

            int completionResult = PublishInputStageCompletionAfterSafePickReturn();
            if (completionResult != 0)
                return completionResult;

            if (_currentBatchItem != null)
                _currentBatchItem.DiePicked = true;

            _currentDieId = "";
            _pickTarget = null;
            _diePicked = false;
            CurrentStep = PickerPickUpStep.SelectNextPickTargetOrComplete;
            return 0;
        }

        private int PublishInputStageCompletionAfterSafePickReturn()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return 0;

            if (!MaterialStateService.IsInputStagePickComplete())
                return 0;

            if (!_currentPickSafeReturnCompleted)
            {
                return Fail("PICKER-PICKUP-STAGE-COMPLETE-UNSAFE", Name,
                    "InputStage 마지막 Pick 완료 신호를 발행할 수 없습니다. " +
                    "PickerZ/NeedleZ/EjectPinZ 안전 복귀가 완료되지 않았습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo);
            }

            if (Context == null || Context.Bus == null)
            {
                return Fail("PICKER-PICKUP-STAGE-COMPLETE-BUS", Name,
                    "InputStage 마지막 Pick 안전 복귀 완료 신호를 발행할 Bus가 없습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo);
            }

            Context.Bus.Set("InputStageDieComplete");
            WriteLog("PickerPickUpSequence",
                Name + " InputStage 마지막 Pick 안전 복귀 완료 후 완료 신호를 발행했습니다. " +
                "signal=InputStageDieComplete, side=" + Side +
                ", pickerNo=" + _currentPickerNo + " - Ok");
            return 0;
        }

        private int VerifyPickerHasDieDataAndFlowAfterPick(string dieId, int pickerNo, int pickerIndex)
        {
            try
            {
                DieMaterial dieOnPicker = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                if (dieOnPicker == null || !string.Equals(dieOnPicker.DieId, dieId, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("PICKER-PICKUP-MATERIAL-FLOW-MISMATCH", Name,
                        "PickUp 완료 후 제품 보유 데이터 확인 실패. " +
                        "Flow 확인 전에 Material 데이터가 Picker 위치와 일치해야 합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", expectedDie=" + dieId +
                        ", actualDie=" + (dieOnPicker != null ? dieOnPicker.DieId : "null"));
                }

                if (IsPickUpProductPrecheckBypassed())
                {
                    WriteLog("PickerPickUpSequence",
                        Name + " PickUp 완료 후 제품 보유 Flow/Data 확인은 Simulation/DryRun 조건으로 Flow 확인을 통과합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + dieId +
                        ", data=OK - Bypass");
                    return 0;
                }

                bool flowOn = ReadPickerFlowState(pickerNo);
                if (!flowOn)
                {
                    return Fail("PICKER-PICKUP-COMPLETE-FLOW-NOT-DETECTED", Name,
                        "PickUp 완료 후 제품 보유 Flow/Data 확인 실패. " +
                        "Material 데이터는 Picker에 있지만 실제 Flow 신호가 ON이 아닙니다. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + dieId +
                        ", expectedFlow=ON, actualFlow=OFF");
                }

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 완료 후 제품 보유 Flow/Data 확인 완료. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", data=OK, flow=ON - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-COMPLETE-FLOW-DATA-EX", Name,
                    "PickUp 완료 후 제품 보유 Flow/Data 확인 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", error=" + ex.Message);
            }
        }

        private int SelectNextPickTargetOrComplete()
        {
            _pickCursor++;

            if (ShouldBlockNewPickForWaferCompletion())
                return StopRemainingPickBatchForWaferCompletion("SelectNextPickTargetOrComplete");

            if (_pickCursor >= _pickBatchItems.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            CurrentStep = PickerPickUpStep.SelectNextPickTarget;
            return 0;
        }

        private bool ShouldBlockNewPickForWaferCompletion()
        {
            if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                return false;

            WaferCompletionRunCoordinator completion = Context != null ? Context.WaferCompletion : null;
            if (completion == null || !completion.Enabled)
                return false;

            completion.ObserveCompletionSignals();
            return completion.IsDrainRequested;
        }

        private int StopRemainingPickBatchForWaferCompletion(string boundary)
        {
            WriteLog("WaferCompletionRun",
                Name + " Stop After Drain 요청으로 현재 안전 경계에서 남은 신규 Pick 대상을 해제합니다. " +
                "side=" + Side +
                ", boundary=" + (boundary ?? "-") +
                ", currentPickerNo=" + _currentPickerNo + " - Ok");
            InputCameraPickUpPermissionStore.Clear(Side);
            ReleaseInputReservationIfNeeded();
            ReleaseInputStageArea();
            CurrentStep = PickerPickUpStep.Complete;
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
            _targetFormula = item != null ? item.TargetFormula ?? "" : "";
            _diePicked = item != null && item.DiePicked;
            _pickerZContactedByContiPickUp = false;
            _currentPickSafeReturnCompleted = false;
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
            _currentBatchItem.TargetFormula = _targetFormula;
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
            _targetFormula = "";
            _diePicked = false;
            _pickerZContactedByContiPickUp = false;
        }

        private double ResolveNeedleXForVisionX(double visionX, double visionOffsetX = 0.0)
        {
            double offset = ResolveNeedleCalibrationOffsetX();
            return visionX + visionOffsetX - offset;
        }

        private double ResolveNeedleYForVisionYOffset()
        {
            return ResolveNeedleCalibrationOffsetY();
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

        private double ResolveNeedleCalibrationOffsetY()
        {
            if (Context == null ||
                Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null ||
                Context.Machine.VisionUnit.Config.CalibrationData == null ||
                Context.Machine.VisionUnit.Config.CalibrationData.Needle == null ||
                !Context.Machine.VisionUnit.Config.CalibrationData.Needle.Valid)
                return 0.0;

            return Context.Machine.VisionUnit.Config.CalibrationData.Needle.NeedleYToVisionYOffset;
        }

        private double ResolveNeedleZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            try
            {
                CalibrationData data = Context != null &&
                                       Context.Machine != null &&
                                       Context.Machine.VisionUnit != null &&
                                       Context.Machine.VisionUnit.Config != null
                    ? Context.Machine.VisionUnit.Config.CalibrationData
                    : null;
                if (data != null)
                {
                    data.EnsureObjects();
                    if (data.Needle != null &&
                        data.Needle.NeedleZCalibrationValid)
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " NeedleZ target uses NeedleCalibrationData.NeedlePinReadyPosition=" +
                            data.Needle.NeedlePinReadyPosition.ToString("F6") + " - Check");
                        return data.Needle.NeedlePinReadyPosition;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " NeedleZ calibration target resolve failed, recipe fallback used. error=" +
                    ex.Message + " - Check");
            }

            return stage.Recipe.NeedleZ.ProcessPosition;
        }

        private double ResolveEjectPinZPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            return stage.Recipe.EjectPinZ.ProcessPosition;
        }

        private static double ResolveEjectPinZSyncLiftOffset(InputStageUnit stage)
        {
            return stage != null && stage.Config != null
                ? stage.Config.PickUpEjectPinOffset
                : 0.0;
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

        private async Task<VisionAlignResult> RequestInputVisionOffsetAsync(CancellationToken ct, bool applySettleDelay)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return null;

            if (IsSimulationOrDryRun(stage))
            {
                VisionAlignResult dryRunVisionResult = await RequestDryRunInputVisionOffsetAsync(stage, ct, applySettleDelay).ConfigureAwait(false);
                if (dryRunVisionResult != null)
                    return dryRunVisionResult;

                return SimulateInputVisionOffset();
            }

            if (applySettleDelay)
                await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            if (stage.Vision == null)
                return null;

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync(VisionAlignTargetIds.InputPickDie).ConfigureAwait(false);
        }

        private async Task<VisionAlignResult> RequestDryRunInputVisionOffsetAsync(
            InputStageUnit stage,
            CancellationToken ct,
            bool applySettleDelay)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!IsDryRunWithWaferVisionConnected())
                    return null;

                if (stage == null || stage.Vision == null)
                    return null;

                if (applySettleDelay)
                    await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

                VisionAlignResult result = await stage.Vision.TriggerAlignAsync(VisionAlignTargetIds.InputPickDie).ConfigureAwait(false);
                WriteLog(Name,
                    "DryRun " + VisionAlignTargetIds.InputPickDie + " Vision GRAB request completed. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", result=" + (result != null ? "OK" : "NG"));
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "DryRun " + VisionAlignTargetIds.InputPickDie + " Vision GRAB request exception. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message + " - SimFallback");
                return null;
            }
            finally
            {
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

                int result = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
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

        private async Task<int> EnsureWaferAlignThetaPositionAsync(
            InputStageUnit stage,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit이 없습니다.");

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                string materialReason;
                if (!MaterialStateService.IsInputStageThetaAlignComplete(wafer, out materialReason))
                    return Fail("PICKER-PICKUP-THETA-ALIGN", stage.Name,
                        description + " 실패. " + materialReason);

                stage.ApplyWaferAlignThetaResult(
                    wafer.InputStageAlignReferenceT,
                    wafer.InputStageAlignCorrectedT,
                    wafer.InputStageAlignOffsetT);

                string readyReason;
                if (!stage.IsWaferAlignThetaResultReady(out readyReason))
                    return Fail("PICKER-PICKUP-THETA-ALIGN", stage.Name,
                        description + " 실패. " + readyReason);

                double targetT;
                if (!stage.TryResolveWaferAlignThetaTarget(out targetT))
                    return Fail("PICKER-PICKUP-THETA-TARGET", stage.Name,
                        description + " 실패. StageT 보정 목표값을 찾을 수 없습니다.");

                if (stage.IsWaferAlignThetaInPosition())
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
                return Fail("PICKER-PICKUP-THETA-POS-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 확인/복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
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
                double currentNeedleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : ResolveNeedleXForVisionX(currentX);
                double targetNeedleX = ResolveNeedleXForVisionX(targetX);

                WriteLog("PickerPickUpStagePath",
                    Name + " InputStage vision point path evaluate. description=" + description +
                    ", currentVisionX=" + currentX.ToString("F6") +
                    ", currentStageY=" + currentY.ToString("F6") +
                    ", currentNeedleX=" + currentNeedleX.ToString("F6") +
                    ", targetVisionX=" + targetX.ToString("F6") +
                    ", targetStageY=" + targetY.ToString("F6") +
                    ", targetNeedleX=" + targetNeedleX.ToString("F6") +
                    " - Check");

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
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX/StageY already in position, move/check NeedleX only. description=" + description + " - Check");

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
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY in position, move VisionX+NeedleX. description=" + description + " - Check");

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
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX in position, move StageY then NeedleX. description=" + description + " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
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
                // 현재 기준: 경로 판단은 CameraX가 아니라 NeedleX/StageY 실축 조합으로 확인한다.
                bool canMoveXFirst = stage.IsNeedleWorkPointInArea(targetNeedleX, currentY, out xFirstReason);
                if (canMoveXFirst)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: VisionX/NeedleX first then StageY. description=" + description +
                        ", reason=" + xFirstReason + " - Check");

                    int result = await MoveInputVisionXAndNeedleXAndVerifyAsync(
                        stage,
                        targetX,
                        targetNeedleX,
                        description,
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    return CheckInputStageVisionPointFinalPosition(stage, targetX, targetY, description);
                }

                string yFirstReason;
                bool canMoveYFirst = stage.IsNeedleWorkPointInArea(currentNeedleX, targetY, out yFirstReason);
                if (canMoveYFirst)
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY first then VisionX/NeedleX. description=" + description +
                        ", reason=" + yFirstReason + " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        currentX,
                        targetY,
                        description + " StageY",
                        ct,
                        currentNeedleX).ConfigureAwait(false);
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
                if (stage.IsNeedleWorkPointInArea(targetNeedleX, targetY, out finalTargetReason))
                {
                    WriteLog("PickerPickUpStagePath",
                        Name + " InputStage path selected: StageY safe enter then VisionX/NeedleX. description=" + description +
                        ", xFirstBlocked=" + xFirstReason +
                        ", yFirstBlocked=" + yFirstReason +
                        ", finalTargetReason=" + finalTargetReason +
                        " - Check");

                    int result = await MoveInputStageYAndVerifyAsync(
                        stage,
                        targetX,
                        targetY,
                        description + " StageY 안전 진입",
                        ct,
                        targetNeedleX).ConfigureAwait(false);
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

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.VisionX, visionTarget))
                {
                    int visionResult = await MoveInputStageAxisCommandAsync(
                        stage,
                        WaferStageAxis.VisionX,
                        visionTarget,
                        description + " VisionX",
                        ct).ConfigureAwait(false);
                    if (visionResult != 0)
                        return Fail("PICKER-PICKUP-STAGE-XY-VISION", stage != null ? stage.Name : "InputStageUnit",
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

                int result = await MoveNeedleXAndStageYForPickAsync(
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
                return Fail("PICKER-PICKUP-STAGE-XY-ORDER-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " 순서 이동 중 예외가 발생했습니다. error=" + ex.Message);
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

        private async Task<int> MoveNeedleXAndStageYForPickAsync(
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
                    return Fail("PICKER-PICKUP-NEEDLE-STAGE-NO-UNIT", "InputStageUnit",
                        description + " 이동 중 InputStageUnit이 없습니다.");

                bool needleInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.NeedleX, needleTarget);
                bool stageYInPosition = IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, stageYTarget);
                if (needleInPosition && stageYInPosition)
                    return 0;

                int safetyResult = await EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync(
                    stage,
                    description + " 이동 전 EjectPinZ 대기(Avoid)/Vacuum OFF 확인",
                    ct).ConfigureAwait(false);
                if (safetyResult != 0)
                    return safetyResult;

                bool moveNeedleXFirst;
                string reason;
                if (!stage.TryResolveNeedleWorkPointMoveOrder(needleTarget, stageYTarget, out moveNeedleXFirst, out reason))
                {
                    return Fail("PICKER-PICKUP-NEEDLE-STAGE-PATH", stage.Name,
                        description + " 이동 가능한 NeedleX/StageY 순서를 찾지 못했습니다. " + reason);
                }

                WriteLog("PickerPickUpStagePath",
                    Name + " NeedleX/StageY pick path selected. description=" + description +
                    ", order=" + (moveNeedleXFirst ? "NeedleX->StageY" : "StageY->NeedleX") +
                    ", reason=" + reason +
                    ", needleInPosition=" + needleInPosition +
                    ", stageYInPosition=" + stageYInPosition +
                    ", targetNeedleX=" + needleTarget.ToString("F6") +
                    ", targetStageY=" + stageYTarget.ToString("F6") +
                    ", workAreaVisionX=" + workAreaVisionX.ToString("F6") +
                    " - Check");

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
                return Fail("PICKER-PICKUP-NEEDLE-STAGE-PATH-EX", stage != null ? stage.Name : "InputStageUnit",
                    description + " NeedleX/StageY 이동 순서 처리 중 예외가 발생했습니다. error=" + ex.Message);
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
            CancellationToken ct,
            double? workAreaNeedleX = null)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!IsInputStageAxisAlreadyInPosition(stage, WaferStageAxis.WaferY, target))
                {
                    int safeResult = await EnsureEjectPinZAtAvoidBeforePickStageMoveAsync(
                        stage,
                        description + " StageY 이동 전",
                        ct).ConfigureAwait(false);
                    if (safeResult != 0)
                        return safeResult;

                    int result = await MoveInputStageYForPickerWorkPointCommandAsync(
                        stage,
                        workAreaVisionX,
                        target,
                        description,
                        ct,
                        workAreaNeedleX).ConfigureAwait(false);
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
                        BuildPickUpInputStageMoveTargetPrefix(),
                        workAreaNeedleX),
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
                BaseAxis item = ResolveInputStageAxis(stage, axis);
                string guardTargetName = "PickerPickUp;Side=" + Side + ";" + axis + ";" + description;
                if (axis == WaferStageAxis.VisionX)
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                            ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    using (MotionGuardRuntime.BeginAxisTeachingMove(item, target, guardTargetName))
                    {
                        result = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                            ct).ConfigureAwait(false);
                    }
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
                WriteLog("PickerPickUpStageMove",
                    Name + " InputStage axis wait complete. description=" + description +
                    ", " + BuildInputStageAxisState(stage, axis, target) +
                    " - Ok");
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
                case WaferStageAxis.WaferT:
                    return stage.StageT;
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

        private async Task<int> MoveEjectPinZToAvoidKeepNeedleZAsync(
            InputStageUnit stage,
            double needleTeachingTarget,
            double ejectTarget,
            CancellationToken ct)
        {
            try
            {
                int needleVacuumOffResult = EnsureNeedleVacuumOffForPick(stage, "PickerZ Stage Safe 도달 후 EjectPinZ AVOID 이동 전");
                if (needleVacuumOffResult != 0)
                    return needleVacuumOffResult;

                // 현재 기준: 정상 PickUp 완료 후 NeedleZ는 Pick teaching 위치를 유지하고 EjectPinZ만 복귀한다.
                int ejectResult = await MoveInputStageAxisCommandAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 EjectPinZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                ejectResult = await WaitInputStageAxisInPositionResultAsync(
                    stage,
                    WaferStageAxis.EjectPinZ,
                    ejectTarget,
                    "PickUp 후 EjectPinZ Avoid 이동",
                    ct).ConfigureAwait(false);
                if (ejectResult != 0)
                    return ejectResult;

                int check = CheckInputStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, ejectTarget, "PickUp 후 EjectPinZ Avoid 이동");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " Needle Vacuum OFF 후 EjectPinZ Avoid 완료 확인. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.EjectPinZ, ejectTarget) +
                    " - Ok");

                check = CheckInputStageAxisInPosition(stage, WaferStageAxis.NeedleZ, needleTeachingTarget, "PickUp 후 NeedleZ teaching 유지");
                if (check != 0)
                    return check;

                WriteLog("PickerPickUpZ",
                    Name + " PickUp 후 NeedleZ teaching 유지. " +
                    BuildInputStageAxisState(stage, WaferStageAxis.NeedleZ, needleTeachingTarget) +
                    " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-EJECT-PIN-AVOID-KEEP-NEEDLE-EX", Name,
                    "PickUp 후 EjectPinZ Avoid 및 NeedleZ teaching 유지 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool ShouldDeferCycleStopForPickUpDrain()
        {
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return false;
                if (Context == null || !Context.IsCycleStopRequested)
                    return false;
                if (IsAlarmStopActive())
                    return false;

                return CurrentStep != PickerPickUpStep.CheckUnit &&
                       CurrentStep != PickerPickUpStep.CheckPickerSideEnabled &&
                       CurrentStep != PickerPickUpStep.BuildEnabledPickerList &&
                       CurrentStep != PickerPickUpStep.CheckInputStageReady &&
                       CurrentStep != PickerPickUpStep.Complete &&
                       CurrentStep != PickerPickUpStep.Error;
            }
            catch
            {
                return false;
            }
            finally
            {
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

