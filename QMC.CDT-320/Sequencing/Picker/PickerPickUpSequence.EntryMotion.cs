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
    internal sealed partial class PickerPickUpSequence
    {
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

        // 현재 기준(사용자 승인 2026-07-25, M8): Input 작업영역의 확인과 예약을 원자화한다.
        //   기존 조건: 물리 대기 게이트(WaitOppositePickerNotInInputPickAreaAsync, 스텝
        //             MoveOppositePickerToAvoidForPickerMove)와 논리 예약(EnsurePickerWorkAreaReserved,
        //             스텝 MovePickerXStageYPickerT)이 분리되어 있고 사이에 실제 축 이동이 여럿 있어
        //             양쪽이 동시에 게이트를 통과하면 둘 다 등록되고 이후 X 이동이 인터락 -11 →
        //             Critical로 라인을 세웠다.
        //   현재 기준: TryReservePickerWorkAreaExclusive가 성공할 때까지 대기한다. 성공 = 그 순간
        //             상대 미점유가 보장된 상태이므로 이후 X 진입이 점유 경합으로 차단되지 않는다.
        //   물리 대기 게이트는 상대의 *물리 위치*를 보는 게이트라 그대로 유지한다(이 예약은 *논리 점유*).
        // 계층 관계: 1차 직렬화는 InputStageArea 자원 lease와 PickerPhaseCoordinator의 PickUp/PickUp
        //   차단이며, 이 예약은 그 뒤의 방어 계층이다. 정상 흐름에서는 첫 시도에 성공해
        //   Wait 로그가 찍히지 않는 것이 정상이다(찍히면 상위 직렬화 구멍 신호 → 보고 대상).
        // 자기 재예약 호환: 수동 경로는 시퀀스 초입에서 EnsurePickerWorkAreaReserved로 선예약하므로
        //   여기서는 no-op(대기 없음)로 통과한다 — 수동 무대기 규약 유지.
        // 비Auto는 기존과 동일하게 즉시 실패(인터락 차단과 같은 결론, 대기 없음).
        private async Task<int> ReserveInputWorkAreaExclusiveWithWaitAsync(
            string description,
            CancellationToken ct)
        {
            try
            {
                string occupiedOwner;
                if (TryReservePickerWorkAreaExclusive(PickerWorkZone.Input, description, out occupiedOwner))
                    return 0;

                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                {
                    return Fail("PICKER-OPPOSITE-INPUT-ZONE", Name,
                        "Pick 진입 불가: 상대 Picker가 Input 작업영역을 점유 중입니다. " +
                        "description=" + description +
                        ", owner=" + occupiedOwner);
                }

                bool waitLogged = false;
                DateTime lastWaitLog = DateTime.MinValue;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(
                            Name + ".WaitInputWorkAreaExclusive",
                            ShouldDeferCycleStopForPickUpDrain(),
                            "PickUp batch drain");

                    if (TryReservePickerWorkAreaExclusive(PickerWorkZone.Input, description, out occupiedOwner))
                    {
                        if (waitLogged)
                        {
                            WriteLog("PickerPickUpSequence",
                                Name + " 상대 Picker Input 작업영역 대기 완료. Input 작업영역을 예약하고 Pick 진입을 진행합니다. " +
                                "side=" + Side +
                                ", description=" + description + " - Ok");
                        }

                        return 0;
                    }

                    if ((DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= 1000.0)
                    {
                        lastWaitLog = DateTime.UtcNow;
                        waitLogged = true;
                        WriteLog("PickerPickUpSequence",
                            Name + " Pick 진입 전 상대 Picker Input 작업영역 해제 대기. 동시 Pick은 허용되지 않습니다. " +
                            "side=" + Side +
                            ", description=" + description +
                            ", owner=" + occupiedOwner + " - Wait");
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
                    "상대 Picker Input 작업영역 원자 예약 대기 중 예외 발생: description=" + description +
                    ", error=" + ex.Message);
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

                // 기존 조건: 삭제된 고속 픽업 모드만 자체 PickerZ 안전 확인을 이유로 이 선행 복귀를
                //           건너뛰었다 — 현재 기준: 해당 모드 삭제(사용자 지시 2026-07-25)로 항상 수행한다.
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

                int branchResult;
                if (useContiTransfer)
                {
                    branchResult = await MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync(
                        stage,
                        tAxis,
                        targetName,
                        pickUpConfig,
                        ct).ConfigureAwait(false);
                }
                else
                {
                    branchResult = await MovePickerXStageYPickerTByDefaultAsync(
                        stage,
                        tAxis,
                        targetName,
                        ct).ConfigureAwait(false);
                }

                // R3(follow-entry): 비동기 비전 회피 Task는 첫 피커 처리 완료 전에 반드시 join.
                // 진입 실패 시에도 drain(observe)하고, 진입 성공 후 회피 실패면 시퀀스 Fail.
                int retreatJoinResult = await JoinInputVisionRetreatMoveTaskAsync(
                    "피커 X 진입 완료",
                    ct).ConfigureAwait(false);
                if (branchResult != 0)
                    return branchResult;
                if (retreatJoinResult != 0)
                {
                    return Fail("PICKER-PICKUP-VISION-AVOID-JOIN", Name,
                        "InputVisionX 비동기 최소 회피 이동이 실패했습니다. result=" + retreatJoinResult +
                        ", die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", " + BuildInputStageAxisState(stage, WaferStageAxis.VisionX, _inputVisionPickerEntryTarget));
                }

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
                result = await ReserveInputWorkAreaExclusiveWithWaitAsync("PickUp", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                Task<int> pickerMove;
                if (ShouldFollowInputVisionRetreatForPickerEntry(stage))
                {
                    // R3(follow-entry): X축만 follow로 분리하고 T는 기존 단독 이동 —
                    // NeedleX/StageY/PickerY 등 나머지 축의 병렬/순차 구조는 그대로 유지한다.
                    Task<int> pickerXEntryMove = MovePickerXEntryByVisionFollowOrFallbackAsync(
                        stage,
                        0.0,
                        "pick corrected PickerX 팔로잉 진입",
                        targetName,
                        ct);
                    Task<int> pickerTMove = MovePickerAxisAndVerifyAsync(
                        tAxis,
                        _targetPickerT,
                        "pick corrected PickerT",
                        ct,
                        targetName);
                    pickerMove = JoinPickerEntryMoveResultsAsync(pickerXEntryMove, pickerTMove);
                }
                else
                {
                    // 안전 불변식(사용자 지시 2026-07-25): X/T 묶음 일반 이동 전에 이연 회피가
                    // 남아 있으면 기동+완료 확인 — 미회피 비전으로 진입해 -11에 걸리는 것을 막는다.
                    int deferredJoin = await JoinDeferredInputVisionRetreatBeforePlainMoveAsync(stage, ct)
                        .ConfigureAwait(false);
                    if (deferredJoin != 0)
                        return deferredJoin;

                    var pickerTargets = new Dictionary<PickerAxis, double>();
                    pickerTargets[PickerAxis.PickerX] = _targetPickerX;
                    pickerTargets[tAxis] = _targetPickerT;

                    pickerMove = MovePickerAxesAndVerifyAsync(
                        pickerTargets,
                        "pick corrected Picker X/T",
                        ct,
                        targetName);
                }
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

                // 1-A(사용자 승인 2026-07-26): Y 전진과 동시에 Z PrePick 선행 하강.
                // 발행은 Y가 Avoid 공차를 벗어난 뒤(모니터) — Y-Avoid 중 Z 하강 차단룰 회피.
                // 이전 die가 발행~join 사이 Fail로 중단된 경우의 stale task 잔존 방지(재개 1회 지연 차단).
                ObservePickUpEntryZPreDownTask("새 die 진입 전 잔존 정리");
                Task<int> pickYMove = MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    _targetPickerY,
                    "pick corrected PickerY",
                    ct,
                    targetName);
                Task entryPreDownMonitor = StartPickUpEntryZPreDownWhenYDepartsAsync(pickYMove, targetName, ct);
                result = await pickYMove.ConfigureAwait(false);
                await entryPreDownMonitor.ConfigureAwait(false);
                if (result != 0)
                {
                    ObservePickUpEntryZPreDownTask("PickUp Y 전진 실패");
                    return result;
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

        // 1-A(사용자 승인 2026-07-26): PickUp 진입 Y 전진 ∥ 첫 피커 Z PrePick 선행 하강.
        // 반경 게이트(1-C): die 목표 NeedleX/StageY와 기존 Needle 작업영역 중심의 거리 ≤
        // min(설정 반경, 런타임 작업영역 반경)일 때만 발동 — 기존 반경 인터락은 무변경 유지.
        // join은 MovePickerZPickAsync(Z 세부 모션 직전). 이 경로는 default XYT(배치 첫 die 포함)
        // 전용 — Conti 이송은 X 명령 시점 Z 전축 Avoid 인터락 요구 때문에 선행 하강 불가(레포트 기재).
        private Task<int> _pickUpEntryZPreDownTask;
        private double _pickUpEntryZPreDownTarget = double.NaN;
        private const double PickUpEntryZPreDownYDepartureMm = 1.0;

        // [정정 2026-07-26, 사용자 승인] PickUpZHold 의미 변경 — PrePick 파킹이 아니라
        // "픽업 후 Avoid로 상승 명령된(safe 통과) 픽커 Z"를 추적한다. X 진입은 이 축의
        // Avoid "도착"을 기다리지 않고 진행하며, 인터락 면제는 targetName의
        // "PickUpZHold={pickerNo}" 토큰으로 해당 픽커 Z만(이동 중 포함) 적용된다.
        private int _pickUpZHoldPickerIndex = -1;
        private double _pickUpZHoldZTarget = double.NaN;
        private const double PickUpZHoldParkToleranceMm = 0.05;

        // [사용자 승인 2026-07-26, 병목 #1] 픽업 후 EjectPinZ Avoid 복귀 백그라운드 태스크 —
        // 다음 die XY 게이트(EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync)에서 join.
        private Task<int> _pickUpEjectPinAvoidTask;

        private bool HasActivePickUpZHold
        {
            get { return _pickUpZHoldPickerIndex >= 0 && !double.IsNaN(_pickUpZHoldZTarget); }
        }

        // 픽업 완료 직후 호출 — 해당 픽커 Z가 Avoid로 상승 중(safe 통과)임을 기록한다.
        // Auto+Conti에서만 기록: 그 외 모드는 인터락 면제(토큰/태그)가 성립하지 않으므로
        // 기존처럼 다음 진입에서 Avoid 도착을 기다린다(회귀 방지).
        private void MarkPickUpZRising(PickerAxis pickerZ, double pickerZAvoid)
        {
            PickerPickUpMotionConfig pickUpConfig = ResolvePickUpMotionConfig();
            if (Options == null || Options.RunMode != SequenceRunMode.Auto ||
                pickUpConfig == null || !IsCoordinatedPickUpTransferMotionMode(pickUpConfig.TransferMotionMode))
                return;

            _pickUpZHoldPickerIndex = _currentPickerIndex;
            _pickUpZHoldZTarget = pickerZAvoid;
            WriteLog("PickerPickUpSequence",
                Name + " PickUpZRising 기록 — PickerZ Avoid 상승 중 조기 진행. pickerNo=" + _currentPickerNo +
                ", axis=" + pickerZ +
                ", avoid=" + pickerZAvoid.ToString("F3") + " - Check");
        }

        // 백그라운드 결과 태스크의 예외 관찰(미회수 예외 방지). 결과 회수는 각 join 지점에서 한다.
        private static void ObserveBackgroundResultTask(Task<int> task)
        {
            if (task == null || task.IsCompleted)
                return;

            task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
        }

        // [검증 FAIL S5 수정 2026-07-26] 드레인 경계가 PickUpZHold를 남긴 채 종료했는지 —
        // PickerProcessSequence가 Bottom 진입 Full-Avoid 생략을 판단할 때 사용한다.
        public bool DrainLeftPickerZHoldUnsafe { get; private set; }

        private void ClearPickUpZHold(string reason)
        {
            if (!HasActivePickUpZHold)
            {
                _pickUpZHoldPickerIndex = -1;
                _pickUpZHoldZTarget = double.NaN;
                return;
            }

            WriteLog("PickerPickUpSequence",
                Name + " PickUpZHold 해제. pickerNo=" + ToPickerNo(_pickUpZHoldPickerIndex) +
                ", holdZ=" + _pickUpZHoldZTarget.ToString("F3") +
                ", reason=" + (reason ?? "-") + " - Check");
            _pickUpZHoldPickerIndex = -1;
            _pickUpZHoldZTarget = double.NaN;
        }

        // 반경 게이트(1-C) 공용 판정: 중심/반경은 기존 Needle 작업영역 정의 재사용,
        // limit = min(설정 반경, 런타임 작업영역 반경). die 목표 좌표 확정 판정.
        private bool IsPickTargetWithinPreDownRadius(
            double needleX,
            double stageY,
            PickerPickUpMotionConfig pickUpConfig,
            out string detail)
        {
            detail = string.Empty;
            InputStageUnit stage = ResolveInputStage();
            if (stage == null || pickUpConfig == null)
            {
                detail = "stageOrConfigNull";
                return false;
            }

            double centerX = stage.ResolveNeedleWorkAreaCenterX();
            double centerY = stage.ResolveNeedleWorkAreaCenterY();
            double areaRadius = stage.ResolveNeedleWorkAreaRadius();
            double limit = pickUpConfig.PreDownNeedleWorkRadiusMm;
            if (areaRadius > 0.0 && limit > areaRadius)
                limit = areaRadius;
            if (limit <= 0.0)
            {
                detail = "radiusOff";
                return false;
            }

            double dx = needleX - centerX;
            double dy = stageY - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            detail = "distance=" + distance.ToString("F3") +
                     ", limit=" + limit.ToString("F3") +
                     ", centerX=" + centerX.ToString("F3") +
                     ", centerY=" + centerY.ToString("F3");
            return distance <= limit;
        }

        private void ObservePickUpEntryZPreDownTask(string reason)
        {
            Task<int> task = _pickUpEntryZPreDownTask;
            _pickUpEntryZPreDownTask = null;
            _pickUpEntryZPreDownTarget = double.NaN;
            if (task == null || task.IsCompleted)
                return;

            task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
            WriteLog("PickerPickUpSequence",
                Name + " PickUp 진입 Z 선행 Task를 관찰 정리합니다. reason=" + (reason ?? "-") + " - Check");
        }

        private bool IsPickUpEntryZPreDownEligible(out double preDownTarget, out string detail)
        {
            preDownTarget = double.NaN;
            detail = string.Empty;
            try
            {
                PickerPickUpMotionConfig pickUpConfig = ResolvePickUpMotionConfig();
                if (pickUpConfig == null || !pickUpConfig.PickUpEntryZPreDownMode)
                {
                    detail = "switchOff";
                    return false;
                }

                if (Options == null || Options.RunMode != SequenceRunMode.Auto ||
                    !IsCoordinatedPickUpTransferMotionMode(pickUpConfig.TransferMotionMode))
                {
                    detail = "notAutoConti";
                    return false;
                }

                if (pickUpConfig.PickerZPrePickDistance <= 0.0)
                {
                    detail = "prePickDistanceOff";
                    return false;
                }

                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                double zAvoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                if (!IsPickerAxisInPosition(zAxis, zAvoid))
                {
                    detail = "zNotAvoid";
                    return false;
                }

                // 반경 게이트(1-C): 공용 판정 헬퍼(중심·반경 기존 정의 재사용, 신규 중심 금지).
                string radiusDetail;
                if (!IsPickTargetWithinPreDownRadius(_targetNeedleX, _targetStageY, pickUpConfig, out radiusDetail))
                {
                    detail = "radius " + radiusDetail;
                    return false;
                }
                detail = "radius " + radiusDetail;

                preDownTarget = ResolveTargetToward(
                    _targetPickerZ,
                    zAvoid,
                    pickUpConfig.PickerZPrePickDistance);
                if (Math.Abs(preDownTarget - zAvoid) <= 0.0001)
                {
                    detail += ", preDownEqualsAvoid";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                detail = "exception " + ex.Message;
                return false;
            }
        }

        private async Task StartPickUpEntryZPreDownWhenYDepartsAsync(
            Task<int> pickYMoveTask,
            string targetName,
            CancellationToken ct)
        {
            try
            {
                double preDownTarget;
                string gateDetail;
                if (!IsPickUpEntryZPreDownEligible(out preDownTarget, out gateDetail))
                    return;

                BaseAxis yAxisObject = GetPickerAxis(PickerAxis.PickerY);
                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                DateTime start = DateTime.UtcNow;
                while (!pickYMoveTask.IsCompleted)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    double yActual = yAxisObject != null ? yAxisObject.ActualPosition : yAvoid;
                    if (Math.Abs(yActual - yAvoid) > PickUpEntryZPreDownYDepartureMm)
                    {
                        // [정정 2026-07-26] 같은 픽커 Z에 하강 명령이 나가므로 rising 추적 종료.
                        if (HasActivePickUpZHold && _pickUpZHoldPickerIndex == _currentPickerIndex)
                            ClearPickUpZHold("1-A 진입 Z 선행 하강 시작 — rising 종료");

                        _pickUpEntryZPreDownTarget = preDownTarget;
                        _pickUpEntryZPreDownTask = MovePickerAxisCommandAsync(
                            GetPickerZAxis(_currentPickerIndex),
                            preDownTarget,
                            targetName);
                        WriteLog("PickerPickUpSequence",
                            Name + " PickUp 진입 Y 전진과 동시 Z PrePick 선행 하강을 시작했습니다. " +
                            "pickerNo=" + _currentPickerNo +
                            ", die=" + _currentDieId +
                            ", preDownZ=" + preDownTarget.ToString("F3") +
                            ", yActual=" + yActual.ToString("F3") +
                            ", gate=" + gateDetail +
                            ", elapsedMs=" + (DateTime.UtcNow - start).TotalMilliseconds.ToString("0") + " - Ok");
                        return;
                    }

                    await Task.Delay(10).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence",
                    Name + " PickUp 진입 Z 선행 모니터 예외(선행 생략, 기존 경로 유지). error=" + ex.Message + " - Check");
            }
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

                int inputReserveResult = await ReserveInputWorkAreaExclusiveWithWaitAsync("PickUp ContiNode", ct).ConfigureAwait(false);
                if (inputReserveResult != 0)
                    return inputReserveResult;

                double pickerXStart = pickerX.ActualPosition;
                double pickerXPrePickTrigger = ResolveContiAsyncPrePickTriggerPosition(
                    pickerXStart,
                    _targetPickerX,
                    pickUpConfig);
                // 기존 조건: 이송 4축이 PICKUP CONTI MAX VEL × NODE2%(단일값)를 공유 — 축별
                //   기본속도와 무관했고, 값이 축 기본속도×스케일과 우연히 일치하면 가감속 이중
                //   스케일(S²)까지 발생했다(실장비 Front PickerX acc 1/400 실측).
                // 현재 기준(사용자 확정 속도 모델 2026-07-26): 이송 각 축 = 자기 축
                //   DefaultVelocity × 전역 스케일 (CONTI MAX VEL/NODE% 설정 폐지).
                double pickerXTransferVelocity = ResolvePickerAxisVelocityByPercent(PickerAxis.PickerX, 100.0);
                double pickerXTransferAcceleration = ResolvePickerAxisAccelerationByPercent(PickerAxis.PickerX, 100.0, true);
                double pickerXTransferDeceleration = ResolvePickerAxisAccelerationByPercent(PickerAxis.PickerX, 100.0, false);
                double stageYTransferVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.WaferY, 100.0);
                double stageYTransferAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.WaferY, 100.0, true);
                double stageYTransferDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.WaferY, 100.0, false);
                double needleXTransferVelocity = ResolveInputStageAxisVelocityByPercent(stage, WaferStageAxis.NeedleX, 100.0);
                double needleXTransferAcceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleX, 100.0, true);
                double needleXTransferDeceleration = ResolveInputStageAxisAccelerationByPercent(stage, WaferStageAxis.NeedleX, 100.0, false);

                WriteLog("PickerPickUpSequence",
                    Name + " PickUp ContiNode async transfer start. " +
                    "die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerXStart=" + pickerXStart.ToString("F6") +
                    ", pickerXTarget=" + _targetPickerX.ToString("F6") +
                    ", prePickTrigger=" + pickerXPrePickTrigger.ToString("F6") +
                    ", prePickZ=" + prePickTarget.ToString("F6") +
                    ", pickerXVel=" + pickerXTransferVelocity.ToString("F6") +
                    ", stageYVel=" + stageYTransferVelocity.ToString("F6") +
                    ", needleXVel=" + needleXTransferVelocity.ToString("F6") +
                    ", speedModel=DefaultVelocity*GlobalScale - Start");

                // R3(follow-entry): 비전 회피가 진행 중이면 follow 진입(+R6 폴백) — 정지 상태면 기존 이동.
                Task<int> pickerXMoveTask = StartPickUpPickerXEntryMoveTask(
                    stage,
                    pickerXTransferVelocity,
                    pickerXTransferAcceleration,
                    pickerXTransferDeceleration,
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
                    stageYTransferVelocity,
                    stageYTransferAcceleration,
                    stageYTransferDeceleration,
                    "PickUp ContiNode StageY async target",
                    ct,
                    BuildPickUpInputStageMoveTargetName(WaferStageAxis.WaferY, "PickUpContiNodeStageY"));
                Task<int> needleXMoveTask = MoveInputStageAxisWithMotionAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    _targetNeedleX,
                    needleXTransferVelocity,
                    needleXTransferAcceleration,
                    needleXTransferDeceleration,
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
            int pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait != 0)
            {
                return Fail("PICKER-PICKUP-CONTI-PICKER-X", Name,
                    "PickUp ContiNode PickerX final wait failed. waitCode=" + pickerXWait +
                    ". " + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
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

            int pickerZWait = await WaitPickerAxisMoveDoneAsync(
                pickerZAxis,
                pickerZFinalTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerZWait != 0)
            {
                return Fail("PICKER-PICKUP-CONTI-PICKER-Z", Name,
                    "PickUp ContiNode PickerZ final wait failed. waitCode=" + pickerZWait +
                    ". " + BuildPickerAxisState(pickerZAxis, pickerZFinalTarget));
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
            {
                string tags = ";PickerPhase=InspectionZHold;InspectionContinuous;From=Input;To=Input";
                // PickUpZHold 면제 토큰(사용자 승인 2026-07-26): 유지 픽커 Z만 인터락 위치 요구 면제.
                if (HasActivePickUpZHold)
                    tags += ";PickUpZHold=" + ToPickerNo(_pickUpZHoldPickerIndex);
                return AppendAutoProcessCorrectionTargetTag(targetName + tags);
            }

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

    }
}
