using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPlaceSequence
    {
        private PickerPlaceMotionConfig ResolvePlaceMotionConfig()
        {
            PickerPlaceMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                config = FrontPicker.Config.Place;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                config = RearPicker.Config.Place;

            if (config == null)
                config = new PickerPlaceMotionConfig();

            config.Ensure();
            return config;
        }

        private OutputStageResultRoutingMode ResolveOutputStageResultRoutingMode()
        {
            if (_resultRoutingModeCaptured)
                return _resultRoutingModeSnapshot;

            if (OutputStage == null || OutputStage.Config == null)
                return OutputStageResultRoutingMode.ForceGoodStage;

            return OutputStage.Config.ResultRoutingMode;
        }

        private double ResolvePlaceZOverDrive()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? config.PlaceZOverDrive : 0.0;
        }

        private int ResolvePlaceReleaseDwellMs()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? Math.Max(0, config.PlaceReleaseDwellMs) : 0;
        }

        private int ResolvePlaceBlowDelayMs()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? Math.Max(0, config.PlaceBlowDelayMs) : 0;
        }

        private void TurnPlaceBlowOff(string reason, bool force = false)
        {
            if (!_placeBlowHoldUntilAvoid && !force)
                return;

            try
            {
                SetPickerBlow(_currentPickerNo, false);
                WriteLog("PickerPlaceSequence",
                    Name + " Place Blow OFF. " +
                    "pickerNo=" + _currentPickerNo +
                    ", reason=" + reason + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place Blow OFF 정리 실패. " +
                    "pickerNo=" + _currentPickerNo +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
                _placeBlowHoldUntilAvoid = false;
            }
        }

        private BaseAxis ResolveOutputStageYAxis(BinStageAxis yAxis)
        {
            if (OutputStage == null)
                return null;

            if (yAxis == BinStageAxis.GoodBinY)
                return OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;

            if (yAxis == BinStageAxis.NgBinY)
                return OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;

            return null;
        }

        private string BuildPlaceMoveTargetName()
        {
            return AppendAutoProcessCorrectionTargetTag(BuildPickerTargetName("DiePlacePosition", _currentPickerIndex) + ";PickerPhase=InspectionZHold;InspectionContinuous;From=Side;To=Place");
        }

        private async Task<int> EnsureOutputStageZReadyForPlaceAsync(CancellationToken ct)
        {
            if (_currentOutputSide != BinSide.Good)
                return 0;

            if (OutputStage == null || OutputStage.Recipe == null)
                return Fail("PICKER-PLACE-GOOD-Z-UNIT", "OutputStage",
                    "Good Stage Place 전 OutputStage 또는 Recipe를 찾을 수 없습니다. die=" +
                    (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo);

            OutputStage.Recipe.EnsurePositionObjects();

            int ngAvoidResult = await AwaitStepWithCancellationAsync(
                OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);
            if (ngAvoidResult != 0)
                return Fail("PICKER-PLACE-GOOD-Z-NG-AVOID", "OutputStage",
                    "Good Stage Z Process 이동 전 NG Stage Avoid 이동 실패. result=" + ngAvoidResult +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            if (!OutputStage.IsNgStageInAvoidPosition())
                return Fail("PICKER-PLACE-GOOD-Z-NG-AVOID-CHECK", "OutputStage",
                    "Good Stage Z Process 이동 전 NG Stage가 Avoid 위치가 아닙니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            double target = OutputStage.Recipe.GoodStageZ.ProcessPosition;
            int result = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.GoodBinZ,
                target,
                "Good Stage Z place process",
                ct,
                BuildOutputStagePlaceMoveTargetName("GoodZProcess")).ConfigureAwait(false);

            if (result != 0)
                return Fail("PICKER-PLACE-GOOD-Z-PROCESS", "OutputStage",
                    "Good Stage Place 전 Z축 Process 위치 이동 실패. result=" + result +
                    ", target=" + target +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            return 0;
        }

        private void AddLoadedPickerTPlaceTargets(IDictionary<PickerAxis, double> targets)
        {
            foreach (int pickerIndex in _pickedPickerIndexes)
            {
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                double target = ResolvePlacePickerTTarget(pickerIndex);

                if (!CanSkipPickerMoveCommand(tAxis, target))
                    targets[tAxis] = target;
            }
        }

        private double ResolvePlacePickerTTarget(int pickerIndex)
        {
            // 현재 Place 대상 PickerT에는 Bottom 검사 회전 보정값이 적용된 최종 목표를 사용한다.
            if (pickerIndex == _currentPickerIndex)
                return _targetPickerT;

            return GetPickerTeachingPosition(GetPickerTAxis(pickerIndex), "PlacePosition");
        }

        private int VerifyPlaceTarget()
        {
            if (!IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerX final position check failed before place. " +
                    BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
            }

            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            if (!OutputStage.IsStageAxisInPosition(yAxis, _targetOutputStageY, ResolveOutputStageAxisTolerance(yAxis)))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "OutputStageY final position check failed before place. " +
                    OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY));
            }

            if (!IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerY final teaching position check failed before place. " +
                    BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY));
            }

            if (!IsPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT))
            {
                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerT final position check failed before place. pickerNo=" + _currentPickerNo +
                    ", " + BuildPickerAxisState(tAxis, _targetPickerT));
            }

            PickerAxis currentTAxis = GetPickerTAxis(_currentPickerIndex);
            WriteLog("PickerPlaceTargetVerify",
                Name + " place target verified after XYT move. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", formula=" + (_targetFormula ?? "") +
                ", outputStageYState=" + OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY) +
                ", pickerXState=" + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX) +
                ", pickerYState=" + BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                ", pickerTState=" + BuildPickerAxisState(currentTAxis, _targetPickerT) +
                ", pickerZTarget=" + _targetPickerZ.ToString("F6") +
                " - Ok");

            if (_pickerZPlacedByContiSegmentedPlace)
            {
                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                if (!IsPickerAxisInPosition(zAxis, _targetPickerZ))
                {
                    return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                        "Place ContiNode 이동 후 PickerZ 최종 위치 확인 실패. pickerNo=" + _currentPickerNo +
                        ", " + BuildPickerAxisState(zAxis, _targetPickerZ));
                }

                WriteLog("PickerPlaceTargetVerify",
                    Name + " place synchronized Z target verified after move. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", formula=" + (_targetFormula ?? "") +
                    ", pickerZState=" + BuildPickerAxisState(zAxis, _targetPickerZ) +
                    " - Ok");

                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

            CurrentStep = PickerPlaceStep.MovePickerZPlace;
            return 0;
        }

        private async Task<int> EnsureInspectionResultsReadyBeforePlaceDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (WaitInspectionResultsBeforePlaceDownAsync == null)
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (_currentDie == null ||
                    !IsInspectionFlowComplete(_currentDie) ||
                    (_currentDie.Result != DieResult.Good && _currentDie.Result != DieResult.NG))
                {
                    return Fail("PICKER-PLACE-INSPECTION-RESUME-GATE", "Material",
                        "Place 재개 시 Vision RESULT Task는 없지만 저장된 Bottom/Side 판정이 완전하지 않아 PickerZ 하강을 차단합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", result=" + (_currentDie != null ? _currentDie.Result.ToString() : "null") +
                        ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                        ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                        ", side90Done=" + HasInspectionResult(_currentDie, "Side90") + ".");
                }

                WriteLog("PickerPlaceSequence",
                    Name + " Place 재개 PickerZ 하강 전 저장된 Bottom/Side 판정을 재확인했습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + _currentDie.Result + " - Ok");
                return 0;
            }

            if (_currentDie == null || string.IsNullOrWhiteSpace(_currentDie.DieId))
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE-DIE", "Material",
                    "PickerZ 하강 전 Bottom/Side 최종 RESULT와 연결할 Place 제품이 없습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo + ".");
            }

            WriteLog("PickerPlaceSequence",
                Name + " PickerZ 하강 직전 해당 Picker Bottom/Side 최종 RESULT를 기다립니다. " +
                "다른 Picker 결과는 기다리지 않습니다. pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId + " - Start");

            int result = await WaitInspectionResultsBeforePlaceDownAsync(
                _currentPickerNo,
                _currentDie.DieId,
                ct).ConfigureAwait(false);
            if (result != 0)
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE", "Vision",
                    "Bottom/Side 최종 RESULT 미수신 또는 불일치로 PickerZ 하강을 차단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + result);
            }

            if (!IsInspectionFlowComplete(_currentDie) ||
                (_currentDie.Result != DieResult.Good && _currentDie.Result != DieResult.NG))
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE-STATE", "Material",
                    "Bottom/Side 최종 RESULT Task는 완료됐지만 Material 판정 반영이 완료되지 않았습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + _currentDie.Result +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

            WriteLog("PickerPlaceSequence",
                Name + " PickerZ 하강 직전 Bottom/Side 최종 RESULT 확인 완료. " +
                "pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId +
                ", inspectionResult=" + _currentDie.Result + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerZPlaceAsync(CancellationToken ct)
        {
            int inspectionGateResult = await EnsureInspectionResultsReadyBeforePlaceDownAsync(ct).ConfigureAwait(false);
            if (inspectionGateResult != 0)
                return inspectionGateResult;

            if (_pickerZPlacedByContiSegmentedPlace)
            {
                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

            // 1-A join(사용자 승인 2026-07-26): 진입 시 선행 발행한 Z PrePlace 하강을 여기서
            // 합류한다 — 완료 대기 후 아래 기존 최종 하강이 잔여 구간을 이어간다(동기 재시도 겸용).
            // 선행 실패는 로그만 남기고 기존 하강이 그대로 흡수한다(기존 Fail 처리 경로 유지).
            Task<int> entryPreDown = _placeEntryZPreDownTask;
            if (entryPreDown != null)
            {
                _placeEntryZPreDownTask = null;
                double preDownTarget = _placeEntryZPreDownTarget;
                _placeEntryZPreDownTarget = double.NaN;
                DateTime joinStart = DateTime.UtcNow;

                int preDownCommandResult = await SequenceAwaiter.AwaitAsync(entryPreDown, -1, ct).ConfigureAwait(false);
                int preDownWaitResult = preDownCommandResult == 0 && !double.IsNaN(preDownTarget)
                    ? await WaitPickerAxisMoveDoneAsync(
                        GetPickerZAxis(_currentPickerIndex),
                        preDownTarget,
                        ResolveTimeout(),
                        ct).ConfigureAwait(false)
                    : 0;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 진입 Z 선행 join. pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", commandResult=" + preDownCommandResult +
                    ", waitResult=" + preDownWaitResult +
                    ", joinWaitMs=" + (DateTime.UtcNow - joinStart).TotalMilliseconds.ToString("0") +
                    " - " + (preDownCommandResult == 0 && preDownWaitResult == 0 ? "Ok" : "Check"));
            }

            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "place picker Z",
                ct,
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VacuumOff;
            return 0;
        }

        private async Task<int> VacuumOffAsync(CancellationToken ct)
        {
            try
            {
                SetPickerVacuum(_currentPickerNo, false);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);
                CurrentStep = PickerPlaceStep.BlowOff;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-VACUUM-OFF", Name, "Picker vacuum off failed. pickerNo=" + _currentPickerNo + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> BlowOffAsync(CancellationToken ct)
        {
            try
            {
                int releaseDwellMs = ResolvePlaceReleaseDwellMs();
                int blowDelayMs = ResolvePlaceBlowDelayMs();
                int totalDwellMs = Math.Max(releaseDwellMs, blowDelayMs);

                if (blowDelayMs > 0)
                {
                    SetPickerBlow(_currentPickerNo, true);
                    _placeBlowHoldUntilAvoid = true;

                    WriteLog("PickerPlaceSequence",
                        Name + " Place Blow ON. Place 위치에서 Blow Delay 동안만 Blow를 유지합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", outputSide=" + _currentOutputSide +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", totalDwellMs=" + totalDwellMs + " - Start");

                    await Task.Delay(blowDelayMs, ct).ConfigureAwait(false);
                    TurnPlaceBlowOff("Place Blow Delay 완료");
                }
                else
                {
                    TurnPlaceBlowOff("Place Blow Delay 0ms", true);
                    WriteLog("PickerPlaceSequence",
                        Name + " Place Blow Delay가 0ms라 Blow ON을 생략합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", outputSide=" + _currentOutputSide +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", totalDwellMs=" + totalDwellMs + " - Check");
                }

                int remainDwellMs = totalDwellMs - blowDelayMs;
                if (remainDwellMs > 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place Release Dwell 잔여 대기 시작. " +
                        "Blow는 OFF 상태로 유지합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", remainDwellMs=" + remainDwellMs + " - Start");
                    await Task.Delay(remainDwellMs, ct).ConfigureAwait(false);
                }

                // [정정 2026-07-26, 사용자 승인] PrePlace 파킹/지연 폐지 — Auto+Conti(스위치 On)면
                // Z를 즉시 Avoid까지 명령하고, place 높이에서 PLACE CONTI NEAR AVOID 거리만큼
                // 이탈하는 순간 시퀀스는 다음으로 진행한다(잔여 상승 백그라운드, 도착 보장은
                // pending join(다음 노드)과 배치 종료 정리(Z 전체 Avoid)가 담당).
                if (ShouldEarlyProceedPlaceZRetreat())
                {
                    int earlyResult = await StartPickerZAvoidRiseAndWaitNearAvoidDepartureAsync(ct).ConfigureAwait(false);
                    if (earlyResult != 0)
                        return earlyResult;

                    CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
                    return 0;
                }

                CurrentStep = PickerPlaceStep.MovePickerZToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                TurnPlaceBlowOff("Place Blow 유지 중 취소");
                throw;
            }
            catch (Exception ex)
            {
                TurnPlaceBlowOff("Place Blow 유지 중 예외");
                return Fail("PICKER-PLACE-BLOW", Name,
                    "Place Blow 유지 동작 중 예외가 발생했습니다. pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        // [정정 2026-07-26, 사용자 승인] 조기 진행 발동 조건: 스위치 On + Auto + Conti 모드.
        // 마지막/중간 die 구분 없음 — 도착 보장은 pending join과 배치 종료 정리가 담당한다.
        private bool ShouldEarlyProceedPlaceZRetreat()
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return false;

                PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
                if (placeConfig == null || !placeConfig.PlaceEntryZPreDownMode ||
                    !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        // [사용자 승인 2026-07-26] Z→Avoid 풀 명령 후 place 높이에서 ContiNearAvoidDistance
        // 만큼 이탈할 때까지만 대기(10ms 폴링)한다. 임계가 Avoid까지 거리 이상이면 도착 대기와
        // 동일하게 수렴하고, 잔여 상승은 백그라운드 — 도착 join은 pending 소비점이 담당한다.
        private async Task<int> StartPickerZAvoidRiseAndWaitNearAvoidDepartureAsync(CancellationToken ct)
        {
            PickerAxis zAxisKind = GetPickerZAxis(_currentPickerIndex);
            BaseAxis zAxis = GetPickerAxis(zAxisKind);
            if (zAxis == null)
            {
                TurnPlaceBlowOff("Z Avoid 조기 진행 실패(축 없음)");
                return Fail("PICKER-PLACE-Z-EARLY-NO-AXIS", Name,
                    "Place Z Avoid 조기 진행 실패: PickerZ 축이 없습니다. pickerNo=" + _currentPickerNo);
            }

            double avoid = GetPickerTeachingPosition(zAxisKind, "AvoidPosition");
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            double nearAvoidDistance = placeConfig != null ? Math.Max(0.0, placeConfig.ContiNearAvoidDistance) : 0.0;
            double startZ = zAxis.ActualPosition;
            double requiredTravel = Math.Min(Math.Abs(avoid - startZ), nearAvoidDistance);

            Task<int> riseTask = MovePickerAxisCommandAsync(zAxisKind, avoid, "AvoidPosition");
            ObservePlaceBackgroundResultTask(riseTask);

            DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(ResolveTimeout());
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (riseTask.IsCompleted)
                {
                    int riseResult = await riseTask.ConfigureAwait(false);
                    if (riseResult != 0)
                    {
                        TurnPlaceBlowOff("Z Avoid 조기 진행 상승 실패");
                        return riseResult;
                    }
                    break;
                }

                if (Math.Abs(zAxis.ActualPosition - startZ) >= requiredTravel)
                    break;

                if (DateTime.UtcNow > timeoutAt)
                {
                    TurnPlaceBlowOff("Z Avoid 조기 진행 임계 대기 타임아웃");
                    return Fail("PICKER-PLACE-Z-EARLY-TIMEOUT", Name,
                        "Place Z Avoid 조기 진행 실패: NEAR AVOID 이탈 대기 타임아웃. " +
                        "pickerNo=" + _currentPickerNo +
                        ", startZ=" + startZ.ToString("F3") +
                        ", actual=" + zAxis.ActualPosition.ToString("F3") +
                        ", requiredTravel=" + requiredTravel.ToString("F3") +
                        ", avoid=" + avoid.ToString("F3"));
                }

                await Task.Delay(10, ct).ConfigureAwait(false);
            }

            TurnPlaceBlowOff("Z Avoid 조기 진행 — NEAR AVOID 이탈");
            SetPendingContiRetreat(_currentPickerIndex, _currentPickerNo);
            WriteLog("PickerPlaceSequence",
                Name + " Place Z를 Avoid로 명령하고 NEAR AVOID 이탈 확인 — 시퀀스 조기 진행" +
                "(잔여 상승 백그라운드, 도착 join은 pending 소비점/종료 정리 담당). " +
                "pickerNo=" + _currentPickerNo +
                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", startZ=" + startZ.ToString("F3") +
                ", requiredTravel=" + requiredTravel.ToString("F3") +
                ", avoid=" + avoid.ToString("F3") +
                ", outputSide=" + _currentOutputSide + " - Ok");
            return 0;
        }

        // 백그라운드 결과 태스크의 예외 관찰(미회수 예외 방지) — 결과 회수는 join 지점에서.
        private static void ObservePlaceBackgroundResultTask(Task<int> task)
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

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "place picker Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                {
                    TurnPlaceBlowOff("PickerZ Avoid 복귀 실패");
                    return result;
                }

                TurnPlaceBlowOff("PickerZ Avoid 복귀 완료");
                ClearPendingContiRetreat();
                _currentPlaceZSafeReturnCompleted = true;
                CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
                return 0;
            }
            catch (OperationCanceledException)
            {
                TurnPlaceBlowOff("PickerZ Avoid 복귀 중 취소");
                throw;
            }
            catch (Exception ex)
            {
                TurnPlaceBlowOff("PickerZ Avoid 복귀 중 예외");
                return Fail("PICKER-PLACE-Z-AVOID-EX", Name,
                    "Place 후 PickerZ Avoid 복귀 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int UpdateMaterialToOutputStage(CancellationToken ct)
        {
            bool preserveInspectionResult = IsInspectionFlowComplete(_currentDie) &&
                                            (_currentDie.Result == DieResult.Good || _currentDie.Result == DieResult.NG);
            if (!MaterialStateService.MoveDieToOutputStage(
                _currentDie.DieId,
                _currentOutputSide,
                _receiveTarget,
                preserveInspectionResult))
            {
                return Fail("PICKER-PLACE-MATERIAL", "Material",
                    "Move die to output stage failed. die=" + _currentDie.DieId +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", preserveInspectionResult=" + preserveInspectionResult);
            }

            WriteLog("PickerPlaceSequence",
                Name + " place complete. die=" + _currentDie.DieId +
                ", side=" + _currentOutputSide +
                ", pickerNo=" + _currentPickerNo +
                ", outputWafer=" + (_receiveTarget != null ? _receiveTarget.OutputWaferId : "-") +
                ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") +
                ", inspectionResult=" + _currentDie.Result +
                ", preserveInspectionResult=" + preserveInspectionResult + " - Ok");

            if (Context != null && Context.Controller != null)
            {
                Context.Controller.RecordAutoDiePlacedForStats(_currentOutputSide);
                Context.Controller.RecordOutputStageProductReceivedForTact(
                    _currentOutputSide,
                    _currentDie.DieId,
                    _currentPickerNo,
                    _receiveTarget);
            }

            _placedDieId = _currentDie.DieId;
            _placedOutputSide = _currentOutputSide;
            _placedReceiveTarget = _receiveTarget;

            if (_suppressOutputPostPlaceInspection)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " manual Place: Output camera 후검사 큐 등록을 생략합니다. die=" +
                    _placedDieId + ", side=" + _placedOutputSide +
                    ", pickerNo=" + _currentPickerNo + " - Check");
                CurrentStep = PickerPlaceStep.RecoverOutputStageAfterPlace;
                return 0;
            }

            int inspectQueueResult = RegisterOutputPostPlaceInspection(ct);
            if (inspectQueueResult != 0)
                return inspectQueueResult;

            CurrentStep = PickerPlaceStep.RecoverOutputStageAfterPlace;
            return 0;
        }

        private int RegisterOutputPostPlaceInspection(CancellationToken ct)
        {
            try
            {
                if (Context == null || Context.OutputPostPlaceInspections == null)
                {
                    return Fail("PICKER-PLACE-OUTPUT-INSPECT-QUEUE", Name,
                        "Output camera 후검사 큐가 없어 요청을 등록하지 못했습니다. die=" +
                        _placedDieId + ", side=" + _placedOutputSide);
                }

                int result = Context.OutputPostPlaceInspections.Enqueue(
                    new OutputPostPlaceInspectionRequest
                    {
                        DieId = _placedDieId,
                        OutputSide = _placedOutputSide,
                        ReceiveTarget = _placedReceiveTarget,
                        HasPlacedDieCameraTarget = true,
                        PickerNo = _currentPickerNo,
                        PickerSide = Side,
                        HasPickerContext = true,
                        // Auto+Conti 플레이스 등록에서만 최소 회피 허용 (복원/기타 경로는 기본 false).
                        MinimalRetreatEligible =
                            Options != null && Options.RunMode == SequenceRunMode.Auto &&
                            IsCoordinatedPlaceMotionMode(ResolvePlaceMotionConfig().MotionMode),
                        PlacedStageY = _targetOutputStageY,
                        PlacedPickerY = _targetPickerY,
                        OutputVisionToPickerY = _outputVisionToPickerY,
                        FineMove = Options != null && Options.FineMove,
                        MoveTimeoutMs = ResolveTimeout(),
                        Owner = Name,
                        SkipInspection = IsPickerMotionOnlyTestMode()
                    },
                    ct);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 요청 등록 완료. die=" + _placedDieId +
                    ", side=" + _placedOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", placedStageY=" + _targetOutputStageY.ToString("F6") +
                    ", placedPickerY=" + _targetPickerY.ToString("F6") +
                    ", outputVisionToPickerY=" + _outputVisionToPickerY.ToString("F6") + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-INSPECT-QUEUE-EX", Name,
                    "Output camera 후검사 요청 등록 중 예외가 발생했습니다. die=" +
                    _placedDieId + ", side=" + _placedOutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RecoverOutputStageAfterPlaceAsync(CancellationToken ct)
        {
            try
            {
                if (MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                {
                    int handoffResult = await CompleteOutputStageExchangeHandoffAsync(ct).ConfigureAwait(false);
                    if (handoffResult != 0)
                        return handoffResult;

                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

                if (HasPendingContiRetreat())
                {
                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

                if (_currentOutputSide != BinSide.Ng)
                {
                    ReleaseOutputStageArea();
                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

                int result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-PLACE-NG-STAGE-AVOID-AFTER-PLACE", "OutputStage",
                        "NG Place 완료 후 NG Stage Avoid 이동 실패. result=" + result +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

                result = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.GoodBinY,
                    OutputStage.Recipe.GoodStageY.ProcessPosition,
                    "NG Place 완료 후 Good Stage Y 다음 수령 기준 위치 복귀",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.GoodBinZ,
                    OutputStage.Recipe.GoodStageZ.ProcessPosition,
                    "NG Place 완료 후 Good Stage Z 다음 수령 높이 복귀",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ReleaseOutputStageArea();
                CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-RECOVER-EX", "OutputStage",
                    "Place 완료 후 OutputStage 복귀 중 예외가 발생했습니다. side=" + _currentOutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> CompleteOutputStageExchangeHandoffAsync(CancellationToken ct)
        {
            int avoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                "OutputStage 마지막 Place 후 교체 준비 Picker 전체 Avoid",
                ct).ConfigureAwait(false);
            if (avoidResult != 0)
                return avoidResult;

            _currentPlaceZSafeReturnCompleted = true;
            ClearPendingContiRetreat();
            ForceSafeYBeforeFirstPlaceMove = true;
            KeepPickerYForwardDuringPlaceReadyWait = false;

            ReleaseOutputPlaceArea();
            ReleaseOutputStageArea();
            ReleaseOutputFeederArea();
            EndOutputPostPlaceInspectionBatch();

            int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                "OutputStage 마지막 Place 후 교체 준비");
            if (workZoneReleaseResult != 0)
                return workZoneReleaseResult;

            return await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> WaitOutputPostPlaceInspectionIdleAsync(CancellationToken ct)
        {
            try
            {
                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return 0;

                int idleTimeoutMs = Options != null && Options.RunMode == SequenceRunMode.Auto
                    ? 0
                    : ResolveTimeout();

                // F8(2026-07-26): 완전 유휴 대기 → Place 진입 전용 대기로 전환 — 현재 배치가
                // "EPD 완료 + 회피 시작 + lease 조기 반환"에 도달하면 RESULT 수집 완료를 기다리지
                // 않고 진입한다(다음 배치가 등록돼 있으면 그 배치 EPD까지 대기 — 3단계-③ 미러).
                return await Context.OutputPostPlaceInspections.WaitUntilPlaceEntryClearAsync(
                    Name + " Place 진입",
                    idleTimeoutMs,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-INSPECT-WAIT-EX", Name,
                    "Place 진입 전 Output camera 후검사 완료 대기 중 예외가 발생했습니다. side=" +
                    _currentOutputSide + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

    }
}
