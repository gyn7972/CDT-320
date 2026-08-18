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
        private int CalculatePlaceTarget()
        {
            int result = PreparePlaceTargetValues();
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.MovePickerXYAndTToPlace;
            return 0;
        }

        private int PreparePlaceTargetValues()
        {
            if (_placeTargetPrepared)
                return 0;

            string offsetReason;
            if (!TryResolveOutputVisionToPickerOffsets(
                _currentOutputSide,
                _currentPickerIndex,
                out _outputVisionToPickerX,
                out _outputVisionToPickerY,
                out offsetReason))
            {
                return Fail("PICKER-PLACE-COORD-OFFSET", Name,
                    "OutputVision 기준 Picker 좌표 보정값 계산 실패. " +
                    "side=" + Side +
                    ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + offsetReason);
            }

            int calculateResult = CalculatePlaceTargetValues();
            if (calculateResult != 0)
                return calculateResult;

            _placeTargetPrepared = true;
            return 0;
        }

        private int CalculatePlaceTargetValues()
        {
            string dieId = _currentDie != null ? _currentDie.DieId : string.Empty;
            bool autoRun = Options != null && Options.RunMode == SequenceRunMode.Auto;
            if (autoRun && _currentBottomPlaceResult == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-CORRECTION-MISSING", "Vision",
                    "Bottom FINAL 보정값을 확보하기 전에 Place 목표 계산이 요청되었습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + dieId + ".");
            }

            // FINAL RESULT의 bottom_item_offset_x/y만 Place 보정에 정확히 한 번 사용합니다.
            // 기존 MRESULT OffsetY는 계속 Side Vision 전용이며 이 계산에 혼용하지 않습니다.
            VisionOffset bottomOffset = new VisionOffset
            {
                X = _currentBottomPlaceResult != null ? _currentBottomPlaceResult.BottomItemOffsetX : 0.0,
                Y = _currentBottomPlaceResult != null ? _currentBottomPlaceResult.BottomItemOffsetY : 0.0,
                R = 0.0,
                IsValid = true
            };
            string bottomOffsetReason = _currentBottomPlaceResult != null
                ? "BottomFinalItemOffsetAppliedOnce"
                : "ManualPlaceWithoutDeferredBottomCorrection";
            bool useBottomFinalItemOffsetYAsSoleColletYCorrection =
                autoRun && _currentBottomPlaceResult != null;

            double outputStageBaseY = _currentOutputSide == BinSide.Ng
                ? OutputStage.Recipe.NGStageY.ProcessPosition
                : OutputStage.Recipe.GoodStageY.ProcessPosition;
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            double placeMechanicalOffsetX = _placeCorrectionConfigCaptured
                ? _placeMechanicalOffsetXSnapshot
                : placeConfig.GetMechanicalOffsetX(_currentPickerIndex);
            double placeMechanicalOffsetY = _placeCorrectionConfigCaptured
                ? _placeMechanicalOffsetYSnapshot
                : placeConfig.GetMechanicalOffsetY(_currentPickerIndex);
            double placeMechanicalOffsetT = _placeCorrectionConfigCaptured
                ? _placeMechanicalOffsetTSnapshot
                : placeConfig.GetMechanicalOffsetT(_currentPickerIndex);
            double placeCorrectionX = -bottomOffset.X + placeMechanicalOffsetX;
            double placeCorrectionY = -bottomOffset.Y + placeMechanicalOffsetY;
            double placeCorrectionLimitMm = _placeCorrectionConfigCaptured
                ? _bottomPlaceCorrectionLimitSnapshot
                : placeConfig.BottomPlaceCorrectionLimitMm;
            if (Math.Abs(placeCorrectionX) > placeCorrectionLimitMm ||
                Math.Abs(placeCorrectionY) > placeCorrectionLimitMm)
            {
                return Fail("PICKER-PLACE-CORRECTION-RANGE", "Vision",
                    "Bottom FINAL과 기구 보정을 합산한 Place 보정값이 허용 범위를 벗어나 이동을 차단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + dieId +
                    ", correctionX=-bottomX+mechanicalX=" + placeCorrectionX.ToString("F6") +
                    ", correctionY=-bottomY+mechanicalY=" + placeCorrectionY.ToString("F6") +
                    ", allowedAbsMaxMm=" + placeCorrectionLimitMm.ToString("F3") + ".");
            }

            // Place 런타임 보정: Enable일 때만 필터 상태를 적용하고, Disable이면 0을 전달한다
            // (Disable이어도 필터 학습·저장은 Bin 후검사 경로에서 계속된다).
            // 부호 반영(X:-, Y:-, T:-)은 DieCoordinateTransformService.CalculatePlaceTarget이 담당한다.
            // (Y는 2026-07-29 사용자 실장비 확인으로 가산→감산 정정 — 전 채널 감산.)
            bool placeRuntimeEnabled = PlaceRuntimeOffsetService.IsEnabled;
            double placeRuntimeOffsetX = 0.0;
            double placeRuntimeOffsetY = 0.0;
            double placeRuntimeOffsetT = 0.0;
            if (placeRuntimeEnabled)
            {
                PlaceRuntimeOffsetService.GetOffset(
                    Side,
                    _currentPickerNo,
                    out placeRuntimeOffsetX,
                    out placeRuntimeOffsetY,
                    out placeRuntimeOffsetT);
            }

            PlaceCoordinateResult coordinate = PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                Context != null ? Context.Machine : null,
                Side,
                _currentPickerIndex,
                Name,
                dieId,
                _currentOutputSide,
                outputStageBaseY,
                _receiveTarget != null ? _receiveTarget.TargetX : 0.0,
                _receiveTarget != null ? _receiveTarget.TargetY : 0.0,
                OutputStage.Recipe.VisionX.ProcessPosition,
                _outputVisionToPickerX,
                _outputVisionToPickerY,
                bottomOffset.X,
                bottomOffset.Y,
                bottomOffset.R,
                placeRuntimeOffsetX,
                placeRuntimeOffsetY,
                placeRuntimeOffsetT,
                placeMechanicalOffsetX,
                placeMechanicalOffsetY,
                useBottomFinalItemOffsetYAsSoleColletYCorrection,
                placeMechanicalOffsetT);

            _targetOutputStageY = coordinate.OutputStageY;
            _targetPickerX = coordinate.PickerX;
            _targetPickerY = coordinate.PickerY;
            _targetPickerT = coordinate.PickerT;
            // 공정 Place Z 유일한 생성점(Conti 노드/하강/검증에 자동 전파). PlacePosition 티칭은 다이 AF가
            // bestZ+BottomToPlace 산식으로 갱신하며(승인 2026-07-29), 목표식은 티칭 + PlaceZOverDrive 그대로다.
            // PlaceZ 캘리브레이션은 이 함수를 지나지 않는다.
            double placeZOverDrive = ResolvePlaceZOverDrive();
            // [PickerZ 런타임 폐루프 2026-08-16] Side FrontSide ch0 필터값을 이동 목표에서만 감산.
            // Conti 노드/하강/검증은 이 값을 그대로 전파하므로 이중 적용 없음.
            double pickerZRuntimeOffset = CapturePickerZRuntimeOffset(
                _currentPickerNo, "PlaceZ", coordinate.PickerZ + placeZOverDrive);
            _targetPickerZ = coordinate.PickerZ + placeZOverDrive - pickerZRuntimeOffset;
            _targetFormula = coordinate.Formula +
                " / pickerZFinal = pickerZTeaching(" + coordinate.PickerZ.ToString("F6") +
                ") + placeZOverDrive(" + placeZOverDrive.ToString("F6") +
                ") - pickerZRuntimeOffset(" + pickerZRuntimeOffset.ToString("F6") +
                ") = " + _targetPickerZ.ToString("F6");

            WriteLog("PickerPlaceSequence",
                Name + " calculated place target. die=" + dieId +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", outputStageY=" + _targetOutputStageY +
                ", pickerX=" + _targetPickerX +
                ", pickerY=" + _targetPickerY +
                ", pickerT=" + _targetPickerT +
                ", pickerZ=" + _targetPickerZ +
                ", pickerZTeaching=" + coordinate.PickerZ +
                ", placeZOverDrive=" + placeZOverDrive +
                ", pickerZRuntimeOffset=" + pickerZRuntimeOffset.ToString("F6") +
                ", outputStageBaseY=" + outputStageBaseY +
                ", receiveTargetX=" + (_receiveTarget != null ? _receiveTarget.TargetX.ToString() : "-") +
                ", receiveTargetY=" + (_receiveTarget != null ? _receiveTarget.TargetY.ToString() : "-") +
                ", outputVisionToPickerOffsetX=" + _outputVisionToPickerX +
                ", outputVisionToPickerOffsetY=" + _outputVisionToPickerY +
                ", outputVisionToPickerYAppliedToOutputStageY=" +
                (!useBottomFinalItemOffsetYAsSoleColletYCorrection) +
                ", bottomOffsetX=" + bottomOffset.X +
                ", bottomOffsetY=" + bottomOffset.Y +
                ", bottomOffsetT=" + bottomOffset.R +
                ", bottomOffsetMode=" + bottomOffsetReason +
                ", placeRuntimeEnabled=" + placeRuntimeEnabled +
                ", placeRuntimeOffsetX=" + placeRuntimeOffsetX.ToString("F6") +
                ", placeRuntimeOffsetY=" + placeRuntimeOffsetY.ToString("F6") +
                ", placeRuntimeOffsetT=" + placeRuntimeOffsetT.ToString("F6") +
                ", placeMechanicalOffsetX=" + placeMechanicalOffsetX.ToString("F3") +
                ", placeMechanicalOffsetXAppliedToPickerX=True" +
                ", placeMechanicalOffsetY=" + placeMechanicalOffsetY.ToString("F3") +
                ", placeMechanicalOffsetYAppliedToOutputStageY=True" +
                ", placeMechanicalOffsetYAppliedToPickerY=False" +
                ", placeMechanicalOffsetT=" + placeMechanicalOffsetT.ToString("F3") +
                ", placeMechanicalOffsetTAppliedToPickerT=True" +
                ", combinedPlaceCorrectionX=" + placeCorrectionX.ToString("F6") +
                ", combinedPlaceCorrectionY=" + placeCorrectionY.ToString("F6") +
                ", combinedPlaceCorrectionLimitMm=" + placeCorrectionLimitMm.ToString("F3") +
                ", formula=" + _targetFormula + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerXYAndTToPlaceAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            AddLoadedPickerTPlaceTargets(targets);

            // O-2-F: 일반 이동 전 이연 회피 완료 보장(스텝 직접 진입 경로).
            int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
            if (deferredJoin != 0)
                return deferredJoin;

            int result = await MovePickerXTThenYAndVerifyAsync(
                targets,
                "place picker X/Y/T",
                ct,
                BuildPlaceMoveTargetName(),
                forceMove: true).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        // 1-A(사용자 승인 2026-07-26): Place 진입 Y 전진과 동시에 첫 피커 Z를 PrePlace 상당
        // 높이까지 비동기 선행 하강. 발행은 "Y가 Avoid 공차를 벗어난 뒤"에만 한다 —
        // Y가 Avoid 티칭 위치(공차 내)에 있는 동안 Z 하강은 VerifyPickerYAvoidBlocksZDown이
        // 차단하므로(ZHold 예외 없음), 순서로 결정론을 보장한다. join은 MovePickerZPlaceAsync.
        private Task<int> _placeEntryZPreDownTask;
        private double _placeEntryZPreDownTarget = double.NaN;
        private const double PlaceEntryZPreDownYDepartureMm = 1.0;

        private void ObservePlaceEntryZPreDownTask(string reason)
        {
            Task<int> task = _placeEntryZPreDownTask;
            _placeEntryZPreDownTask = null;
            _placeEntryZPreDownTarget = double.NaN;
            if (task == null || task.IsCompleted)
                return;

            task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
            WriteLog("PickerPlaceSequence",
                Name + " Place 진입 Z 선행 Task를 관찰 정리합니다. reason=" + (reason ?? "-") + " - Check");
        }

        // 발동 게이트: 스위치 On + Auto + Conti 모드 + 검사결과 이미 완료(비블로킹 선확인 —
        // 기존 하강 게이트의 재개 판정과 동일 기준) + Z가 Avoid 위치 + (Rear면 대상 Bin StageY
        // 실측 ≤ 임계값; Front는 무조건). 미충족 시 아무 것도 하지 않는다(기존 동작).
        private bool IsPlaceEntryZPreDownEligible(out double preDownTarget, out string detail)
        {
            preDownTarget = double.NaN;
            detail = string.Empty;
            try
            {
                PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
                if (placeConfig == null || !placeConfig.PlaceEntryZPreDownMode)
                {
                    detail = "switchOff";
                    return false;
                }

                if (Options == null || Options.RunMode != SequenceRunMode.Auto ||
                    !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
                {
                    detail = "notAutoConti";
                    return false;
                }

                if (_currentDie == null ||
                    !IsInspectionFlowComplete(_currentDie) ||
                    (_currentDie.Result != DieResult.Good && _currentDie.Result != DieResult.NG))
                {
                    detail = "inspectionNotReady";
                    return false;
                }

                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                if (!IsPickerAxisInPosition(zAxis, GetPickerTeachingPosition(zAxis, "AvoidPosition")))
                {
                    detail = "zNotAvoid";
                    return false;
                }

                if (Side == PickerSequenceSide.Rear)
                {
                    string rearDetail;
                    if (!IsRearEntryPreDownStageYWithinLimit(placeConfig, out rearDetail))
                    {
                        detail = rearDetail;
                        return false;
                    }

                    detail = rearDetail;
                }

                // PrePlace 상당 높이 = 최종 접촉 + 두께 폴백 + Step1/Step2 여유(Conti prePlace 산식과 동형).
                preDownTarget = _targetPickerZ +
                    placeConfig.ContiTapeThicknessFallback +
                    placeConfig.ContiDieThicknessFallback +
                    placeConfig.ContiZ1Step1Clearance +
                    placeConfig.ContiZ1Step2Clearance;
                return true;
            }
            catch (Exception ex)
            {
                detail = "exception " + ex.Message;
                return false;
            }
        }

        // [검증 FAIL 수정 2026-07-26, F2] Rear StageY 임계 판정 — "발행 직전" 재판정용.
        // 기존 결함: 게이트가 모니터 기동 시 1회만 실측을 읽었는데, 직전에 StageY 수령 이동이
        //   이미 발행돼 있어 실제 Z 발행 시점(Y 이탈 감지, 수 초 뒤)엔 StageY가 임계 밖일 수
        //   있었다 — 파라미터가 금지하려는 구간에서 하강하는 오동작 경로.
        // 현재 기준: (a) 실측을 판정 시점마다 UpdateStatus 후 재확인하고, (b) 진행 중인 수령
        //   이동의 "목표"(_targetOutputStageY)도 함께 판정한다 — 둘 다 안전측 AND.
        // 방향(사용자 실측 확인 2026-07-26): 실측·목표 모두 임계 "이상(≥)"일 때만 발동.
        private bool IsRearEntryPreDownStageYWithinLimit(PickerPlaceMotionConfig placeConfig, out string detail)
        {
            detail = string.Empty;
            double limit = placeConfig != null ? placeConfig.RearEntryPreDownStageYLimitMm : 0.0;
            if (limit <= 0.0)
            {
                detail = "rearStageYLimitOff";
                return false;
            }

            BaseAxis stageYAxis = ResolveOutputStageYAxis(
                _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY);
            if (stageYAxis == null)
            {
                detail = "rearStageYAxisNull";
                return false;
            }

            stageYAxis.UpdateStatus();
            double stageYActual = stageYAxis.ActualPosition;
            double stageYTarget = _targetOutputStageY;
            // 방향 정정(사용자 실측 확인 2026-07-26): 설정값 "이상(≥)"일 때만 발동한다 —
            // 리어 물리 간섭 구조물은 StageY가 작은 구간에 있다. 실측과 수령 이동 목표 모두
            // 임계 이상이어야 발동(이동 중 임계 아래로 진입하는 경합은 목표 검사가 차단).
            // limit<=0은 계속 "Rear 발동 안 함(Off)" — 안전측 기본값 유지.
            bool ok = stageYActual >= limit && stageYTarget >= limit;
            detail = (ok ? "rearStageYOk" : "rearStageYLimit") +
                     " actual=" + stageYActual.ToString("F3") +
                     ", target=" + stageYTarget.ToString("F3") +
                     ", limit=" + limit.ToString("F3") +
                     ", rule=actual>=limit&&target>=limit";
            return ok;
        }

        // Y 전진을 감시하다가 Avoid 공차 이탈이 확인되는 즉시 Z 선행 하강을 발행한다.
        // 이 모니터는 진입 이동을 실패시키지 않는다(선행 실패는 join에서 기존 하강이 흡수).
        private async Task StartPlaceEntryZPreDownWhenYDepartsAsync(Task<int> pickerMoveTask, CancellationToken ct)
        {
            try
            {
                double preDownTarget;
                string gateDetail;
                if (!IsPlaceEntryZPreDownEligible(out preDownTarget, out gateDetail))
                    return;

                BaseAxis yAxisObject = GetPickerAxis(PickerAxis.PickerY);
                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                DateTime start = DateTime.UtcNow;
                while (!pickerMoveTask.IsCompleted)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    double yActual = yAxisObject != null ? yAxisObject.ActualPosition : yAvoid;
                    if (Math.Abs(yActual - yAvoid) > PlaceEntryZPreDownYDepartureMm)
                    {
                        // [F2 수정] Rear는 발행 직전 실측+수령 목표를 재판정 — 초과면 발행 포기(기존 경로).
                        if (Side == PickerSequenceSide.Rear)
                        {
                            string rearRecheckDetail;
                            if (!IsRearEntryPreDownStageYWithinLimit(ResolvePlaceMotionConfig(), out rearRecheckDetail))
                            {
                                WriteLog("PickerPlaceSequence",
                                    Name + " Place 진입 Z 선행을 발행 직전 Rear StageY 재판정으로 생략합니다. " +
                                    "pickerNo=" + _currentPickerNo +
                                    ", detail=" + rearRecheckDetail + " - Check");
                                return;
                            }
                        }

                        _placeEntryZPreDownTarget = preDownTarget;
                        _placeEntryZPreDownTask = MovePickerAxisCommandAsync(
                            GetPickerZAxis(_currentPickerIndex),
                            preDownTarget,
                            BuildPickerTargetName("DiePlacePosition", _currentPickerIndex) +
                            ";PickerPhase=InspectionZHold;InspectionContinuous;From=Place;To=Place");
                        WriteLog("PickerPlaceSequence",
                            Name + " Place 진입 Y 전진과 동시 Z PrePlace 선행 하강을 시작했습니다. " +
                            "pickerNo=" + _currentPickerNo +
                            ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                            ", preDownZ=" + preDownTarget.ToString("F3") +
                            ", yActual=" + yActual.ToString("F3") +
                            ", side=" + Side +
                            ", gate=" + gateDetail +
                            ", elapsedMs=" + (DateTime.UtcNow - start).TotalMilliseconds.ToString("0") + " - Ok");
                        return;
                    }

                    await Task.Delay(10).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 진입 Z 선행 모니터 예외(선행 생략, 기존 경로 유지). error=" + ex.Message + " - Check");
            }
        }

        private async Task<int> MoveOutputStageYAndPickerXYTToPlaceAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            var pickerTargets = new Dictionary<PickerAxis, double>();
            pickerTargets[PickerAxis.PickerX] = _targetPickerX;
            pickerTargets[PickerAxis.PickerY] = _targetPickerY;
            AddLoadedPickerTPlaceTargets(pickerTargets);

            // R4(follow-entry): 비전 회피가 진행/이연 중이면 X만 follow로 분리(T 병렬 → Y 전진 구조 유지),
            // 정지 상태(2번째 픽커 포함)면 기존 X/T→Y 이동 그대로.
            // O-2-F: follow를 타지 않을 때만, 그리고 어떤 이동 Task보다 먼저 이연 회피를 완료한다
            //        (StageY Task 생성 전에 판정해야 조기 반환 시 고아 Task가 생기지 않는다.
            //         StageY 이동은 게이트 입력에 영향을 주지 않으므로 판정 시점 이동은 무해하다).
            bool useVisionFollowEntry = ShouldFollowOutputVisionRetreatForPickerEntry();
            if (!useVisionFollowEntry)
            {
                int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                    return deferredJoin;
            }

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "output stage receive Y",
                ct,
                BuildOutputStagePlaceMoveTargetName("ReceiveY"));
            Task<int> pickerMove = useVisionFollowEntry
                ? MovePlacePickerXTThenYWithVisionFollowAsync(
                    pickerTargets,
                    BuildPlaceMoveTargetName(),
                    ct)
                : MovePickerXTThenYAndVerifyAsync(
                    pickerTargets,
                    "place picker X/Y/T",
                    ct,
                    BuildPlaceMoveTargetName(),
                    forceMove: true);

            // 1-A: Y 전진 시작(Avoid 이탈) 감지 시 Z PrePlace 선행 — 게이트 미충족이면 즉시 종료.
            Task entryPreDownMonitor = StartPlaceEntryZPreDownWhenYDepartsAsync(pickerMove, ct);

            int[] results = await Task.WhenAll(stageYMove, pickerMove).ConfigureAwait(false);
            await entryPreDownMonitor.ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                ObservePlaceEntryZPreDownTask("Place 진입 XYT 이동 실패");
                return Fail("PICKER-PLACE-PARALLEL-MOVE", Name,
                    "Place OutputStageY와 Picker X/Y/T 동시 이동 실패. " +
                    "stageYResult=" + results[0] +
                    ", pickerResult=" + results[1] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            //await MovePickerAxesAndVerifyAsync(
            //    pickerTargets,
            //    "place picker X/Y/T",
            //    ct,
            //    BuildPickerTargetName("DiePlacePosition", _currentPickerIndex));

            //Log.Write("PickerPlaceSequence", Name + " MovePickerAxesAndVerifyAsync. side=" + Side + ", step=" + CurrentStep);
            //await MoveOutputStageAxisAndVerifyAsync(
            //    yAxis,
            //    _targetOutputStageY,
            //    "output stage receive Y",
            //    ct);
            //Log.Write("PickerPlaceSequence", Name + " MoveOutputStageAxisAndVerifyAsync. side=" + Side + ", step=" + CurrentStep);
            return 0;
        }

        private async Task<int> MoveOutputStageYAndPickerXTThenYToPlaceAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            var pickerXAndTTargets = new Dictionary<PickerAxis, double>();
            pickerXAndTTargets[PickerAxis.PickerX] = _targetPickerX;
            AddLoadedPickerTPlaceTargets(pickerXAndTTargets);

            // O-1-B: 이 경로는 ForceSafeYBeforeFirstPlaceMove(재시작 첫 접근)로만 진입하는 안전 우선
            //        경로다. 오버랩보다 확정된 순서(X/T 완료 후 Y 전진)가 우선이므로 follow를 심지
            //        않는다. 대신 O-2-F 방어만 넣어 일반 이동 전에 이연 회피가 반드시 완료되게 한다.
            int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
            if (deferredJoin != 0)
                return deferredJoin;

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "Place 재시작 OutputStageY 수령 위치",
                ct,
                BuildOutputStagePlaceMoveTargetName("ResumeReceiveY"));
            Task<int> pickerXAndTMove = MovePickerAxesAndVerifyAsync(
                pickerXAndTTargets,
                "Place 재시작 Picker X/T",
                ct,
                BuildPlaceMoveTargetName(),
                forceMove: true);

            int[] results = await Task.WhenAll(stageYMove, pickerXAndTMove).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-PLACE-RESUME-XT-STAGE-MOVE", Name,
                    "Place 재시작 OutputStageY와 Picker X/T 선행 이동 실패. " +
                    "stageYResult=" + results[0] +
                    ", pickerXTResult=" + results[1] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            // [사용자 지시 2026-08-18] 최종 목표 접근 Y는 허용치 내라도 스킵 없이 강제 발행한다.
            if (CanSkipPickerMoveCommand(PickerAxis.PickerY, _targetPickerY))
            {
                BaseAxis pickerYAxis = GetPickerAxis(PickerAxis.PickerY);
                double actualY = pickerYAxis != null ? pickerYAxis.ActualPosition : double.NaN;
                WriteLog("PickerPlaceSequence",
                    Name + " Place 재시작 PickerY를 허용치 내에서도 강제 발행합니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", targetY=" + _targetPickerY.ToString("F6") +
                    ", actual=" + actualY.ToString("F6") +
                    ", diff=" + (_targetPickerY - actualY).ToString("F6") + " - Check");
            }

            WriteLog("PickerPlaceSequence",
                Name + " Place 재시작 Picker X/T 목표 이동 완료 후 PickerY 전진을 시작합니다. " +
                "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", targetY=" + _targetPickerY + " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "Place 재시작 PickerY 전진",
                ct,
                BuildPlaceMoveTargetName(),
                forceMove: true).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            _pickerZPlacedByContiSegmentedPlace = false;

            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (ForceSafeYBeforeFirstPlaceMove)
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place 재시작 안전 진입으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                int safeYResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                if (safeYResult != 0)
                    return safeYResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 재시작 첫 접근은 PickerY Avoid 상태에서 ContiNode/선행 Y 전진을 사용하지 않고 X/T 이동 후 Y 전진 순서로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXTThenYToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 모드가 아니어서 이전 PickerZ를 먼저 Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (IsFirstPlaceMoveInBatch())
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place 첫 번째 접근 전 이전 PickerZ 안전 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 첫 번째 접근 이동은 ContiNode를 사용하지 않고 기존 이동 방식으로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            BaseAxis stageY = ResolveOutputStageYAxis(yAxis);
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            BaseAxis pickerZ = GetPickerAxis(GetPickerZAxis(_currentPickerIndex));
            BaseAxis previousPickerZ = HasPendingContiRetreat()
                ? GetPickerAxis(GetPickerZAxis(_pendingContiRetreatPickerIndex))
                : null;
            double previousPickerZAvoid = HasPendingContiRetreat()
                ? GetPickerTeachingPosition(GetPickerZAxis(_pendingContiRetreatPickerIndex), "AvoidPosition")
                : 0.0;

            return await MoveOutputStageYPickerXAndPickerZByContiSegmentedPlaceAsync(
                yAxis,
                stageY,
                pickerX,
                previousPickerZ,
                previousPickerZAvoid,
                pickerZ,
                placeConfig,
                ct).ConfigureAwait(false);
        }

        private static bool IsCoordinatedPlaceMotionMode(PickerPlaceMotionMode mode)
        {
            return mode == PickerPlaceMotionMode.ContiSegmentedPlace;
        }

    }
}
