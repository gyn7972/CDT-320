using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
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

    internal sealed partial class PickerPickUpSequence : PickerSequenceBase<PickerPickUpStep>
    {
        private const double ContinuousPickMaxDeltaX = 70.0;
        private const double ContinuousPickMaxDeltaY = 3.0;
        private const double ContinuousPickMaxDeltaT = 1.0;
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
        // 픽업 중 비전X 비동기 전진(사용자 지시 2026-07-27, 한 번에 수정): 발행한 전진 목표와 이동 Task.
        // Task는 "이동 중인 비전이 내 전진인지" 판정(ContiNode 비전 검사 통과 조건)에,
        // 목표는 이동 중 오버라이드 연장의 기준 비교에 사용한다. 배치 준비 시 초기화.
        private double _pickUpVisionAdvanceTarget;
        private Task<int> _pickUpVisionAdvanceTask;
        // R3(follow-entry): 비동기 시작한 InputVisionX 최소 회피 이동 Task.
        // 첫 피커 X 진입(MovePickerXStageYPickerT) 완료 전에 반드시 join(결과 0 확인)한다.
        // 기존 조건(사용자 지시 2026-07-25): 이동 명령 발행을 피커 X 진입 직전까지 이연했다
        //   (_inputVisionRetreatDeferred) — 실측 결과 비전이 촬영 위치에 홀드되어 손해였다.
        // 현재 기준(사용자 지시 2026-07-26): 이연 폐지. 선행검사가 배치 마지막 EPD 직후 시작한
        //   독립 회피(VisionIndependentRetreatCoordinator)를 인수하거나(목표 부족 시 연장 합성),
        //   외부 회피가 없으면 회피 좌표 확정 즉시 비동기 자체 기동한다.
        private Task<int> _inputVisionRetreatMoveTask;
        private DateTime _needleVacuumOffConfirmedAtUtc = DateTime.MinValue;

        private sealed class PickUpBatchItem
        {
            public int PickerIndex;
            public int PickerNo;
            public string DieId;
            public InputStagePickTarget PickTarget;
            public VisionAlignResult VisionOffset;
            // 조기 허가 경로(EPD 시점 허가): RESULT 미회수 핸들과 요청 인덱스,
            // 그리고 자재 기록/전파 적용 여부 — CalculatePickTargets에서 회수/적용에 사용.
            public QMC.CDT320.VisionComm.VisionRequestHandle VisionRequest;
            public int VisionRequestIndex;
            public bool VisionOffsetApplied;
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
                ObserveInputVisionRetreatMoveTaskOnAbort();
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
                    // CycleTime ERR 초크 포인트 — 실패/취소 종료 시 활성 계측 사이클 일괄 ERR.
                    // (정상 완료 시엔 RESULT로 이미 닫혀 활성이 없으므로 무해.)
                    QMC.CDT320.Diagnostics.HandlerTactLog.CycleErrorAll(
                        "PICKUP|" + (Side == PickerSequenceSide.Front ? "F" : "R"));
                    // 조기 허가 경로: CalculatePickTargets 전에 종료되면 배치에 RESULT 미회수 핸들이 남는다 — 드레인.
                    DrainPickBatchVisionHandles("PickUp 시퀀스 종료 정리");
                    ReleaseInputReservationIfNeeded();
                    ReleasePickerWorkArea();
                    ReleaseInputStageArea();
                }
            }
        }

        /// <summary>배치에 남은 RESULT 미회수 핸들을 백그라운드로 드레인 — 회수 완료/에러 핸들은 코어에서 자연 통과.</summary>
        private void DrainPickBatchVisionHandles(string reason)
        {
            try
            {
                List<QMC.CDT320.VisionComm.VisionRequestHandle> pendingHandles = null;
                for (int i = 0; i < _pickBatchItems.Count; i++)
                {
                    PickUpBatchItem item = _pickBatchItems[i];
                    if (item == null || item.VisionRequest == null || item.VisionOffset != null)
                        continue;

                    if (pendingHandles == null)
                        pendingHandles = new List<QMC.CDT320.VisionComm.VisionRequestHandle>();
                    pendingHandles.Add(item.VisionRequest);
                }

                if (pendingHandles == null)
                    return;

                List<QMC.CDT320.VisionComm.VisionRequestHandle> handles = pendingHandles;
                string owner = Name;
                Task.Run(async () =>
                {
                    for (int i = 0; i < handles.Count; i++)
                    {
                        try
                        {
                            await InputDieVisionPrepareSequence.DrainInputVisionRequestHandleAsync(
                                handles[i],
                                reason,
                                owner,
                                CancellationToken.None).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }
                });
            }
            catch
            {
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

                // 검사 완료된 배치의 픽업 대상 계산 (조기 허가 경로는 여기서 RESULT를 회수)
                case PickerPickUpStep.CalculatePickTargets:
                    return CalculatePickTargetsAsync(true, ct);

                // 다음 픽업 대상 선택 (A안: 선택 항목의 RESULT 미회수 시 여기서 확정)
                case PickerPickUpStep.SelectNextPickTarget:
                    return SelectNextPickTargetAsync(ct);

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
                _pickUpVisionAdvanceTask = null;
                _pickUpVisionAdvanceTarget = 0.0;
                // R3(follow-entry): 이전 배치의 비동기 회피 Task 잔여분 정리(drain).
                await JoinInputVisionRetreatMoveTaskAsync("배치 준비 초기화", ct).ConfigureAwait(false);
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

                // [동적 선행 대기점 2026-07-27, 검증 RV1] 허가 미경유 직접 prepare 경로(수동 픽업/
                // 부분 PickUp 재개)는 배치를 여기서 즉시 소비하므로 공개 좌표를 지금 제거한다 —
                // Grant 경로가 없어 스테일 좌표가 무기한 잔존하던 문제 차단. 스킵으로 축소된
                // 배치가 남는 문제도 결과 회수 완료 후 Clear라 함께 해소.
                InputDieVisionBatchCoordinateStore.Clear(Side);

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
                        VisionRequest = prepared.VisionRequest,
                        VisionRequestIndex = prepared.VisionRequestIndex,
                        VisionOffsetApplied = prepared.VisionOffsetApplied,
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

                // 기존 조건: 전체 Avoid 정위치만 허용 — 현재 기준(사용자 승인 2026-07-24): 최소 회피 주차도
                // 인정한다. 비전이 정지 상태이고 양쪽 피커 X의 현재 위치와 페어 간격(SafetyDistance,
                // RetreatExtra 미포함)을 만족하면 통과. 실제 진입 이동은 존 인터락 제3 분기가 재검증한다.
                if (!stage.IsVisionXInAvoidPosition() &&
                    !IsInputVisionParkedClearOfPickers(stage))
                {
                    InputCameraPickUpPermissionStore.Grant(Side, permittedItems);
                    return Fail("PICKER-PICKUP-PERMISSION-VISIONX-NOT-AVOID", stage.Name,
                        "InputCamera Mark 검사 허가를 받았지만 InputVisionX가 Avoid/최소 회피 위치가 아닙니다. " +
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
                        VisionRequest = permitted.VisionRequest,
                        VisionRequestIndex = permitted.VisionRequestIndex,
                        VisionOffsetApplied = permitted.VisionOffsetApplied,
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

                    // 기존 조건: VisionOffset == null이면 Fail — RESULT까지 회수된 허가만 유효했다.
                    // 현재 기준(조기 허가): EPD 완료 핸들만 보유한 허가도 유효 — RESULT는
                    //           CalculatePickTargets에서 회수한다.
                    if (permitted.VisionOffset == null &&
                        !(permitted.ExposureCompleted && permitted.VisionRequest != null))
                    {
                        return Fail("PICKER-PICKUP-PERMISSION-OFFSET-MISSING", "Vision",
                            "InputCamera Mark 검사 허가에 VisionOffset도 Vision 핸들도 없습니다. die=" +
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

    }
}
