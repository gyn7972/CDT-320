using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputCameraMarkInspectionSequence : PickerSequenceBase<InputCameraMarkInspectionStep>
    {
        private readonly List<int> _enabledPickerIndexes = new List<int>();
        private readonly List<InputDieVisionPreparedItem> _inspectedItems = new List<InputDieVisionPreparedItem>();
        private SequenceResourceLease _inputStageLease;
        private AutoSequenceCameraWorkZoneLease _cameraWorkLease;

        public InputCameraMarkInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.PickUp, side == PickerSequenceSide.Front ? "FrontInputCameraMarkInspectionSequence" : "RearInputCameraMarkInspectionSequence")
        {
            CurrentStep = InputCameraMarkInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == InputCameraMarkInspectionStep.Complete; }
        }

        public IList<InputDieVisionPreparedItem> InspectedItems
        {
            get { return _inspectedItems.AsReadOnly(); }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                while (CurrentStep != InputCameraMarkInspectionStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        CurrentStep = InputCameraMarkInspectionStep.Error;
                        return result;
                    }
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
                CurrentStep = InputCameraMarkInspectionStep.Error;
                return Fail("INPUT-CAMERA-MARK-INSPECTION-EX", Name,
                    "Input camera mark inspection failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                ReleaseInputCameraWorkZone();
                ReleaseInputStageArea();
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                case InputCameraMarkInspectionStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                case InputCameraMarkInspectionStep.BuildEnabledPickerList:
                    return Task.FromResult(BuildEnabledPickerList());

                case InputCameraMarkInspectionStep.RunInputCameraMarkInspection:
                    return RunInputCameraMarkInspectionAsync(ct);

                case InputCameraMarkInspectionStep.MoveInputVisionXToAvoid:
                    return MoveInputVisionXToAvoidAsync(_inspectedItems, ct);

                case InputCameraMarkInspectionStep.GrantPickUpPermission:
                    return Task.FromResult(GrantPickUpPermission());

                default:
                    return Task.FromResult(Fail("INPUT-CAMERA-MARK-INSPECTION-STEP", Name,
                        "Unsupported input camera mark inspection step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (!IsPickerSideEnabled())
                {
                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " picker side is disabled. Skip input camera mark inspection. side=" + Side + " - Check");
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                if (Context == null || Context.Machine == null || Context.Machine.InputStageUnit == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-MISSING", "InputStageUnit",
                        "InputStageUnit is missing. Input camera mark inspection cannot run.");

                CurrentStep = InputCameraMarkInspectionStep.BuildEnabledPickerList;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-CHECK-EX", Name,
                    "Input camera mark inspection check failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int BuildEnabledPickerList()
        {
            try
            {
                _enabledPickerIndexes.Clear();
                _inspectedItems.Clear();

                List<int> enabled = BuildEnabledPickerIndexes();
                for (int i = 0; i < enabled.Count; i++)
                {
                    int pickerIndex = enabled[i];
                    int pickerNo = ToPickerNo(pickerIndex);

                    if (Options != null &&
                        Options.RestrictToPickerNo > 0 &&
                        Options.RestrictToPickerNo != pickerNo)
                    {
                        continue;
                    }

                    _enabledPickerIndexes.Add(pickerIndex);
                }

                if (_enabledPickerIndexes.Count == 0)
                {
                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " no enabled picker for input camera mark inspection. side=" + Side + " - Check");
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " enabled picker list built for input camera mark inspection. count=" +
                    _enabledPickerIndexes.Count + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.RunInputCameraMarkInspection;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-PICKER-LIST-EX", Name,
                    "Input camera mark inspection picker list build failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunInputCameraMarkInspectionAsync(CancellationToken ct)
        {
            try
            {
                if (!IsInputCameraPreInspectionMode())
                {
                    int acquireResult = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (acquireResult != 0)
                        return acquireResult;
                }
                else
                {
                    int cameraZoneResult = await AcquireInputCameraWorkZoneAsync(ct).ConfigureAwait(false);
                    if (cameraZoneResult != 0)
                        return cameraZoneResult;

                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " InputCamera 선행검사 모드: Picker/Input 영역 대기 중에는 InputStageArea를 점유하지 않고 실제 Stage/Vision 이동 시점에만 점유합니다. side=" +
                        Side + " - Check");
                }

                InputDieVisionPrepareSequence prepareSequence = new InputDieVisionPrepareSequence(
                    Context,
                    Side,
                    _enabledPickerIndexes);

                int result = await prepareSequence
                    .RunAsync(ct, Options ?? PickerSequenceOptions.Default())
                    .ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (prepareSequence.PreparedItems.Count == 0)
                {
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                // 기존 조건: Task.WhenAll(CollectVisionResultsAsync, MoveInputVisionXToAvoidAsync)로
                //           RESULT 4건을 전부 회수한 뒤에야 허가를 발행했다 — RESULT 처리 시간이 픽업 허가를 지연.
                // 현재 기준: RESULT를 기다리지 않는다. 촬영(EPD)과 VisionX Avoid 완료만 확인하고 즉시 허가를
                //           발행하며, RESULT는 픽업 시퀀스가 CalculatePickTargets에서 회수한다 (조기 허가).
                int visionAvoidResult;
                try
                {
                    visionAvoidResult = await MoveInputVisionXToAvoidAsync(
                        prepareSequence.PreparedItems, ct).ConfigureAwait(false);
                }
                catch
                {
                    await prepareSequence.DrainPreparedResultsAfterFailureAsync(CancellationToken.None).ConfigureAwait(false);
                    prepareSequence.ReleasePreparedReservations();
                    throw;
                }

                if (visionAvoidResult != 0)
                {
                    // 허가 발행 전 실패: EPD 완료·RESULT 미회수 핸들을 드레인하고 예약을 해제한다.
                    await prepareSequence.DrainPreparedResultsAfterFailureAsync(ct).ConfigureAwait(false);
                    prepareSequence.ReleasePreparedReservations();
                    return visionAvoidResult;
                }

                _inspectedItems.Clear();
                IList<InputDieVisionPreparedItem> preparedItems = prepareSequence.PreparedItems;
                for (int i = 0; i < preparedItems.Count; i++)
                    _inspectedItems.Add(preparedItems[i]);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " input camera mark inspection EPD 단계 완료 — RESULT는 PickUp CalculatePickTargets에서 회수합니다. " +
                    "inspectedCount=" + _inspectedItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = _inspectedItems.Count > 0
                    ? InputCameraMarkInspectionStep.GrantPickUpPermission
                    : InputCameraMarkInspectionStep.Complete;
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-RUN-EX", Name,
                    "Input camera mark inspection run failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AcquireInputCameraWorkZoneAsync(CancellationToken ct)
        {
            try
            {
                if (!IsInputCameraPreInspectionMode())
                    return 0;

                if (_cameraWorkLease != null && !_cameraWorkLease.IsDisposed)
                    return 0;

                if (Context == null || Context.AutoSequenceGate == null)
                    return 0;

                // 진짜 FIFO 데드락 절단: 카메라 존을 '잡기 전'(어떤 배타 자원도 안 쥔 상태)에서
                // 전역 진입 큐의 head가 될 때까지 대기한다. head가 아니면 존을 획득하지 않으므로
                // 앞선 피커가 Input에 진입해 진행할 수 있어 hold-and-wait 순환이 성립하지 않는다.
                int fifoHeadResult = await WaitUntilFifoHeadForCameraZoneAsync(ct).ConfigureAwait(false);
                if (fifoHeadResult != 0)
                    return fifoHeadResult;

                _cameraWorkLease = await Context.AutoSequenceGate
                    .BeginInputCameraWorkAsync(Name + ":InputCameraPreInspection:" + Side, ct, Side)
                    .ConfigureAwait(false);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone approved. " +
                    "AutoSequenceCoordinator now owns InputCamera/Picker Input zone arbitration. side=" +
                    Side + " - Ok");
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-ZONE-EX", Name,
                    "InputCamera pre-inspection Input zone approval failed. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        // 진짜 FIFO 데드락 절단: 전역 진입 큐에서 내가 head(최소 티켓)가 될 때까지 카메라 존을
        // 획득하지 않은 채 대기한다. head는 정확히 1개뿐이므로 두 선행검사가 동시에 '상대가 앞섰다'로
        // 서로 양보하는 라이브락이 구조적으로 불가능하다(과거 실패: side별 두 seq 비교 → mine=none이면
        // 양쪽 다 상대가 foreign). 티켓은 선행검사 시작 시(EnsureStarted) 이미 발급돼 있어 대기 중
        // 항상 자기 티켓을 보유한다. bounded timeout(ResolveTimeout) 초과 시 무언정지 대신 알람 Fail.
        private async Task<int> WaitUntilFifoHeadForCameraZoneAsync(CancellationToken ct)
        {
            try
            {
                string headDetail;
                if (InputEntryQueue.IsHead(Side, out headDetail))
                    return 0;

                // 기존 조건: ResolveTimeout()(기본 30초) — 상대 픽커가 티켓을 쥔 채 잔여 공정
                // (Place/검사)을 도는 정상 대기가 저속 스케일에서 30초를 넘겨 오탐 타임아웃이
                // 발생했다(실장비 2026-07-26 03:53, elapsedMs=30013).
                // 현재 기준(사용자 지시 2026-07-26): 기본 100초로 확대하고 속도 스케일을 반영한다
                // (100%에서 100초, 5% 스케일에서는 상한 600초까지 자동 연장).
                int timeoutMs = QMC.Common.Motion.MotionSpeedScale.ScaleDefaultTimeoutMs(100000);
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".InputCameraFifoHeadWait");

                    if (InputEntryQueue.IsHead(Side, out headDetail))
                    {
                        if (waitLogged)
                        {
                            WriteLog("InputCameraMarkInspectionSequence",
                                Name + " 선행검사 카메라 존 진입 순번(FIFO head) 도달. side=" +
                                Side + ", " + headDetail + " - Ok");
                        }

                        return 0;
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputCameraMarkInspectionSequence",
                            Name + " 선행검사 카메라 존 진입 순번 대기(FIFO). 앞선 진입 티켓이 있어 카메라 존을 잡지 않고 " +
                            "먼저 등록된 피커의 진입을 기다립니다. " + headDetail + ", side=" + Side + " - Wait");
                        waitLogged = true;
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        return Fail("INPUT-CAMERA-MARK-INSPECTION-FIFO-HEAD-TIMEOUT", Name,
                            "선행검사 카메라 존 진입 순번(FIFO) 대기가 제한 시간을 초과했습니다. 앞선 진입 티켓이 해소되지 않았습니다. " +
                            "side=" + Side +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", " + headDetail +
                            ", queue=" + InputEntryQueue.Describe());
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-FIFO-HEAD-EX", Name,
                    "선행검사 카메라 존 진입 순번 대기 중 예외가 발생했습니다. side=" + Side + ", error=" + ex.Message);
            }
        }

        private void ReleaseInputCameraWorkZone()
        {
            try
            {
                if (_cameraWorkLease == null)
                    return;

                _cameraWorkLease.Dispose();
                _cameraWorkLease = null;
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone released. side=" + Side + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone release failed. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> AcquireInputStageAreaAsync(CancellationToken ct)
        {
            try
            {
                if (_inputStageLease != null)
                    return 0;

                _inputStageLease = await AcquireResourceAsync(
                    SequenceResourceKind.InputStageArea,
                    Name + ":InputCameraMarkInspection",
                    ct).ConfigureAwait(false);

                return _inputStageLease != null ? 0 : -1;
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-RESOURCE-EX", Name,
                    "InputStageArea resource acquire failed. error=" + ex.Message);
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
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputStageArea lease release failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXToAvoidAsync(
            IList<InputDieVisionPreparedItem> preparedItems,
            CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = Context != null && Context.Machine != null
                    ? Context.Machine.InputStageUnit
                    : null;
                if (stage == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-MISSING", "InputStageUnit",
                        "InputStageUnit is missing. InputVisionX avoid cannot run.");

                if (stage.Recipe == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-RECIPE", stage.Name,
                        "InputStage recipe is missing. InputVisionX avoid cannot run.");

                // 기존 조건(사용자 지시 2026-07-25): Auto+Conti에서는 회피를 픽업 이연·팔로잉으로
                //           위임(이동 생략)했다 — 실측(2026-07-25 런) 결과 8사이클 전부 촬영 위치 홀드
                //           2.1~36.0초 + 픽커 진입 명령과 동일 ms에야 회피 시작 + 픽커가 진입 후 회피
                //           완료를 5.7~6.7초 추가 대기(픽업 크리티컬 패스 포함)로 이연 설계가 손해였다.
                // 현재 기준(사용자 지시 2026-07-26): 배치 마지막 EPD 완료 "즉시" 이 시퀀스가 독립
                //           비동기 회피를 시작하고(허가 발행은 회피 완료를 기다리지 않음), 픽업은
                //           VisionIndependentRetreatCoordinator를 통해 회피 Task를 인수해 3단계
                //           (회피완료=즉시 진입/회피중=팔로잉/촬영중=EPD까지 대기) 진입한다.
                stage.Recipe.EnsurePositionObjects();
                double avoid = stage.Recipe.VisionX.AvoidPosition;
                double tolerance = stage.CameraX != null && stage.CameraX.Config != null && stage.CameraX.Config.InPositionTolerance > 0.0
                    ? stage.CameraX.Config.InPositionTolerance
                    : 0.01;

                if (IsMinimalRetreatGateSatisfied())
                {
                    // 선행검사 촬영 종료 직후 독립 회피: 부호 인지 최소 회피를 적용한다.
                    // 이 시점에는 pick 좌표(CalculatePickTargets)가 아직 없으므로 planned는 근사값으로 구성한다:
                    // 근사 피커X = PickTarget.TargetX(비전 기준 die X) + InputVisionToPicker X 오프셋.
                    // 근사여도 안전하다 — 이후 픽업 시퀀스(MoveInputVisionToAvoidForPickerMoveAsync)가 정확한
                    // 좌표로 재검증(부족 시 연장 회피)하고, Extra 40mm 버퍼 + 피커 진입 인터락(SafetyDistance)은
                    // 그대로 살아있다. 근사 실패 시 전체 Avoid 폴백.
                    double independentTarget = avoid;
                    string independentMode = "fullAvoid";
                    string independentDetail = "근사 산출 불가로 전체 Avoid를 사용합니다.";
                    SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                        Context != null ? Context.Machine : null);
                    List<double> approxPickerTargets = null;
                    string approxReason = string.Empty;
                    if (service != null &&
                        TryBuildApproxPlannedPickerTargets(preparedItems, out approxPickerTargets, out approxReason))
                    {
                        var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                        SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                            ? SharedRailXAxis.FrontPickerX
                            : SharedRailXAxis.RearPickerX;
                        planned[pickerRailAxis] = approxPickerTargets;

                        double dynamicTarget;
                        string dynamicDetail;
                        // 회피 목표 마진(사용자 승인 2026-07-26, 4번): Extra에 +1mm — 최심 픽 목표와의
                        // 최종 간격이 팔로잉 safetyGap과 정확히 같아지는 경계치 해소(팔로잉 gap은 무변경).
                        if (service.TryResolveMinimalVisionRetreatTarget(
                            stage.CameraX,
                            avoid,
                            planned,
                            (service.Config != null ? service.Config.InputVisionRetreatExtraClearance : 40.0) +
                            VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                            out dynamicTarget,
                            out dynamicDetail))
                        {
                            independentTarget = dynamicTarget;
                            independentMode = "minimal";
                            independentDetail = dynamicDetail;
                        }
                        else
                        {
                            independentDetail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                        }
                    }
                    else
                    {
                        independentDetail = "근사 피커X 구성 실패로 전체 Avoid를 사용합니다. reason=" + approxReason;
                    }

                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " InputVisionX 선행검사 후 회피 좌표를 확정했습니다. mode=" + independentMode +
                        ", target=" + independentTarget.ToString("F6") +
                        ", fullAvoid=" + avoid.ToString("F6") +
                        ", detail=" + independentDetail + " - Check");

                    if (stage.CameraX != null && stage.CameraX.IsAtTargetPosition(independentTarget, tolerance))
                    {
                        WriteLog("InputCameraMarkInspectionSequence",
                            Name + " InputVisionX가 이미 회피 목표에 있어 독립 회피 이동을 생략합니다. " +
                            "target=" + independentTarget.ToString("F6") + ", side=" + Side + " - Ok");
                        CurrentStep = InputCameraMarkInspectionStep.GrantPickUpPermission;
                        return 0;
                    }

                    // 배치 마지막 die EPD 완료 "즉시" 회피를 비동기 시작한다(fire-and-forget).
                    // 이동 명령 경로는 기존 동기 경로와 동일(stage.MoveInputStageAxis →
                    // SharedRailXMotionRuntime.MoveAxisAsync, MotionGuard/SharedRailX 검증 + 속도
                    // 스케일 포함). InputStageArea lease는 잡지 않는다 — 퇴장 방향
                    // 이동은 MotionGuard/공유레일 페어 검증이 담당하고, lease를 잡으면 픽업의
                    // 영역 획득이 회피 완료까지 직렬화되어 본 설계 목적(오버랩)이 무산된다.
                    // 보강(사용자 승인 2026-07-26, 3번): 이동 명령 완료를 신뢰하지 않고 "실위치
                    // 도착 대기"까지 합성해 등록한다 — 세션 Task의 완료가 곧 도착을 의미하게 한다.
                    Task<int> independentRetreatTask = RunIndependentInputVisionRetreatAsync(
                        stage,
                        independentTarget);
                    VisionIndependentRetreatCoordinator.RegisterInput(
                        Side,
                        independentRetreatTask,
                        independentTarget,
                        Name + ":PostEpdIndependentRetreat");
                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " InputVisionX 독립 회피 이동을 비동기 시작했습니다(배치 마지막 EPD 직후, 허가 발행은 대기하지 않음). " +
                        "mode=" + independentMode +
                        ", target=" + independentTarget.ToString("F6") +
                        ", actual=" + (stage.CameraX != null ? stage.CameraX.ActualPosition.ToString("F6") : "-") +
                        ", side=" + Side + " - Start");

                    CurrentStep = InputCameraMarkInspectionStep.GrantPickUpPermission;
                    return 0;
                }

                if (IsInputCameraPreInspectionMode())
                {
                    int acquireResult = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (acquireResult != 0)
                        return acquireResult;
                }

                double target = avoid;
                string retreatMode = "legacy";
                string retreatDetail = "전체 Avoid 사용";

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputVisionX 선행검사 후 회피 좌표를 확정했습니다. mode=" + retreatMode +
                    ", target=" + target.ToString("F6") +
                    ", fullAvoid=" + avoid.ToString("F6") +
                    ", detail=" + retreatDetail + " - Check");

                // 현장(F3 선행검사): 소수 3자리 스킵 판정 유지. 일반 경로는 AxisMoveWaiter 제거에 따라
                // BaseAxis.IsAtTargetPosition(동일 공식)으로 통일(R2).
                bool preInspectionF3Move = IsInputCameraPreInspectionMode();
                bool canSkipMove = preInspectionF3Move
                    ? PickerInputStageMoveHelper.CanSkipInputCameraPreInspectionMoveAtThreeDecimals(
                        stage.CameraX,
                        target,
                        tolerance)
                    : stage.CameraX.IsAtTargetPosition(target, tolerance);
                if (!canSkipMove)
                {
                    int moveResult = await stage.MoveInputStageAxis(
                        WaferStageAxis.VisionX,
                        target,
                        Options != null && Options.FineMove,
                        preInspectionF3Move).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-MOVE", stage.Name,
                            "InputVisionX avoid move command failed. target=" + target +
                            ", result=" + moveResult);

                    if (!preInspectionF3Move)
                    {
                        int waitResult = await stage.WaitInputStageAxisInPosition(
                            WaferStageAxis.VisionX,
                            target,
                            ResolveTimeout(),
                            ct).ConfigureAwait(false);
                        if (waitResult != 0)
                            return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-WAIT", stage.Name,
                                "InputVisionX avoid wait failed. target=" + target +
                                ", result=" + waitResult);
                    }
                }

                // 기존 조건: AxisMoveWaiter.IsMoveCompletedAtTarget/BuildAxisState —
                // 현재 기준: 동일 공식의 BaseAxis.IsAtTargetPosition + 인라인 상태 문자열(R2/[E]).
                if (preInspectionF3Move &&
                    !stage.CameraX.IsAtTargetPosition(target, tolerance))
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-CHECK", stage.Name,
                        "InputVisionX strong completion check failed after mark inspection. " +
                        "axisState=[actual=" + stage.CameraX.ActualPosition.ToString("F6") +
                        ", command=" + stage.CameraX.CommandPosition.ToString("F6") +
                        ", target=" + target.ToString("F6") +
                        ", tolerance=" + tolerance.ToString("F6") +
                        ", moving=" + stage.CameraX.IsMoving +
                        ", servo=" + stage.CameraX.IsServoOn +
                        ", alarm=" + stage.CameraX.IsAlarm + "]");

                // 최소 회피 경로는 목표 좌표 기준으로 확인한다 (IsVisionXInAvoidPosition은 전체 Avoid 기준).
                bool finalPositionOk = retreatMode == "minimal"
                    ? stage.CameraX.IsAtTargetPosition(target, tolerance)
                    : stage.IsVisionXInAvoidPosition();
                if (!finalPositionOk)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-CHECK", stage.Name,
                        "InputVisionX is not in avoid position after mark inspection. mode=" + retreatMode +
                        ", target=" + target.ToString("F6"));

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputVisionX avoid confirmed after input camera mark inspection. mode=" + retreatMode +
                    ", target=" + target.ToString("F6") + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.GrantPickUpPermission;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-EX", Name,
                    "InputVisionX avoid after mark inspection failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // 독립 회피 실행 본체(사용자 승인 2026-07-26, 3번): 이동 명령(완료 보장형) 후 "실위치
        // 도착 대기"까지 합성한다 — WaitUntilMoveDone류 완료 신호를 신뢰하지 않고, 이 Task의
        // 완료가 곧 축 도착(또는 명시적 실패)을 의미하게 한다. 도착 대기는 CancellationToken을
        // 걸지 않는다(시퀀스 종료 후 코디네이터 linked CTS가 dispose되어도 안전) — 타임아웃은
        // 속도 스케일을 반영해 상한을 잡는다. CycleStop 시 축 정지 → 대기 타임아웃 → 실패 종료.
        private async Task<int> RunIndependentInputVisionRetreatAsync(InputStageUnit stage, double target)
        {
            int moveResult = await stage.MoveInputStageAxis(
                WaferStageAxis.VisionX,
                target,
                Options != null && Options.FineMove).ConfigureAwait(false);
            if (moveResult != 0)
                return moveResult;

            int arrivalTimeoutMs = QMC.Common.Motion.MotionSpeedScale.ScaleDefaultTimeoutMs(ResolveTimeout());
            return await stage.WaitInputStageAxisInPositionResult(
                WaferStageAxis.VisionX,
                target,
                arrivalTimeoutMs).ConfigureAwait(false);
        }

        // Conti 게이트(픽업 계열): Auto + TransferMotionMode가 ContiSegmentedPickUp일 때만
        // 최소 회피를 적용한다. 미충족 시 기존 전체 Avoid 경로 그대로.
        private bool IsMinimalRetreatGateSatisfied()
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

            return config.TransferMotionMode == PickerPickUpTransferMotionMode.ContiSegmentedPickUp;
        }

        // 근사 planned 구성: 각 배치 아이템의 PickTarget.TargetX(비전 기준 die X)에
        // InputVisionToPicker X 오프셋을 더한 근사 피커X 목록. 하나라도 실패하면 false(전체 Avoid 폴백).
        private bool TryBuildApproxPlannedPickerTargets(
            IList<InputDieVisionPreparedItem> preparedItems,
            out List<double> pickerTargets,
            out string reason)
        {
            pickerTargets = new List<double>();
            reason = string.Empty;

            if (preparedItems == null || preparedItems.Count == 0)
            {
                reason = "준비된 배치 아이템이 없습니다.";
                return false;
            }

            for (int i = 0; i < preparedItems.Count; i++)
            {
                InputDieVisionPreparedItem item = preparedItems[i];
                if (item == null || item.PickTarget == null)
                {
                    reason = "배치 아이템 PickTarget이 없습니다. index=" + i;
                    return false;
                }

                double offsetX;
                double offsetY;
                string offsetReason;
                if (!TryResolveInputVisionToPickerOffsets(item.PickerIndex, out offsetX, out offsetY, out offsetReason))
                {
                    reason = "InputVisionToPicker 오프셋 확인 실패. pickerIndex=" + item.PickerIndex +
                             ", reason=" + offsetReason;
                    return false;
                }

                pickerTargets.Add(item.PickTarget.TargetX + offsetX);
            }

            return pickerTargets.Count > 0;
        }

        private int GrantPickUpPermission()
        {
            try
            {
                if (_inspectedItems.Count == 0)
                {
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                InputCameraPickUpPermissionStore.Grant(Side, _inspectedItems);
                // [동적 선행 대기점 2026-07-27, 검증 F4] 배치가 허가로 소비되면 공개 좌표도 제거 —
                // 다음 촬영 세션 초기 창(Running 등록~새 배치 Publish 사이)에서 이전 배치 좌표가
                // 재사용되는 것을 차단한다. 좌표 부재 시 모니터는 fail-safe로 무동작.
                InputDieVisionBatchCoordinateStore.Clear(Side);
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " pickup permission granted after input camera mark inspection. count=" +
                    _inspectedItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-GRANT-EX", Name,
                    "Input camera mark inspection pickup permission grant failed. error=" + ex.Message);
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
    }
}
