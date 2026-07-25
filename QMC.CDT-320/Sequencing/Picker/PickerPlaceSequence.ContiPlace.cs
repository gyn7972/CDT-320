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
        private async Task<int> MoveOutputStageYPickerXAndPickerZByContiSegmentedPlaceAsync(
            BinStageAxis yAxis,
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            BaseAxis pickerZ,
            PickerPlaceMotionConfig placeConfig,
            CancellationToken ct)
        {
            string guardReason;
            if (!CanUseContiSegmentedPlaceFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 이동 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide +
                    ", maxTravel=" + placeConfig.ContiMaxTravelDistance.ToString("F3") +
                    ", stageYTravel=" + FormatTravel(stageY, _targetOutputStageY) +
                    ", pickerXTravel=" + FormatTravel(pickerX, _targetPickerX) +
                    ", previousPickerZTravel=" + FormatTravel(previousPickerZ, previousPickerZAvoid) +
                    " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            IList<PickerPlaceContiNode> nodes = BuildContiSegmentedPlaceNodes(stageY, pickerX, previousPickerZ, pickerZ, placeConfig);
            if (nodes == null || nodes.Count < 2)
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 노드 생성 실패로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 생성 실패로 기존 이동 방식으로 접근합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (!CanUseContiSegmentedNodesFromCurrentPosition(stageY, pickerX, previousPickerZ, pickerZ, nodes, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 노드 거리 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 거리 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            int preMove = await MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync(ct).ConfigureAwait(false);
            if (preMove != 0)
                return preMove;

            int zReady = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
            if (zReady != 0)
                return zReady;

            double prePlacePickerZ = nodes[nodes.Count - 2].PickerZ;
            double finalPickerZ = nodes[nodes.Count - 1].PickerZ;
            double originalPickerZTarget = _targetPickerZ;
            _targetPickerZ = prePlacePickerZ;

            int previousRetreatResult = await CompletePendingContiRetreatIfNeededAsync(
                "Place 비동기 접근 전 이전 PickerZ Avoid 복귀",
                ct).ConfigureAwait(false);
            if (previousRetreatResult != 0)
            {
                _targetPickerZ = originalPickerZTarget;
                return previousRetreatResult;
            }

            double stageYTarget = _targetOutputStageY;
            double pickerXTarget = _targetPickerX;
            if (stageY != null)
                stageY.UpdateStatus();
            if (pickerX != null)
                pickerX.UpdateStatus();

            if (stageY == null || pickerX == null ||
                !IsFinitePosition(stageYTarget) || !IsFinitePosition(pickerXTarget) ||
                !IsFinitePosition(stageY.ActualPosition) || !IsFinitePosition(stageY.CommandPosition) ||
                !IsFinitePosition(pickerX.ActualPosition) || !IsFinitePosition(pickerX.CommandPosition) ||
                !stageY.IsServoOn || stageY.IsAlarm || stageY.IsMoving ||
                !pickerX.IsServoOn || pickerX.IsAlarm || pickerX.IsMoving)
            {
                return Fail("PICKER-PLACE-CONTI-XY-START", Name,
                    "Place ContiNode XY 이동 시작 전 축 상태가 비정상입니다. " +
                    "stageY={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, true) + "}" +
                    ", pickerX={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, true) + "}");
            }

            bool stageYForceMove = RequiresContiPlaceForceMove(stageY, stageYTarget);
            bool pickerXForceMove = RequiresContiPlaceForceMove(pickerX, pickerXTarget);
            double stageYStart = stageY != null ? stageY.ActualPosition : stageYTarget;
            double pickerXStart = pickerX != null ? pickerX.ActualPosition : pickerXTarget;
            PickerAxis currentPickerZAxis = GetPickerZAxis(_currentPickerIndex);
            string placeTargetName = BuildPlaceMoveTargetName();

            WriteLog("PickerPlaceSequence",
                Name + " Place ContiNode XY 이동 명령 판정. " +
                "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", stageY={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove) + "}" +
                ", pickerX={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove) + "}" +
                " - Check");

            // O-1-A(2026-07-25): 기존 조건 — Conti 본경로(배치 2번째 Place 이후)에는 follow 분기가
            //   아예 없어 Conti 정상 운전에서 팔로잉이 전혀 걸리지 않았다(XYT 폴백 경로에만 존재).
            //   현재 기준 — 비전 회피가 이연/진행 중이면 X만 follow로 진입한다(Input Conti 경로 미러).
            //   forceMove가 필요한 경우는 follow가 forceMove 의미를 보장하지 못하므로 기존 일반 이동.
            // O-2-F: follow를 쓰지 않는 쪽은 이동 Task 생성 전에 이연 회피를 완료한다.
            bool useContiVisionFollowEntry = !pickerXForceMove && ShouldFollowOutputVisionRetreatForPickerEntry();
            if (!useContiVisionFollowEntry)
            {
                int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                {
                    _targetPickerZ = originalPickerZTarget;
                    return deferredJoin;
                }
            }

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                stageYTarget,
                "Place ContiNode OutputStageY 비동기 이동",
                ct,
                BuildOutputStagePlaceMoveTargetName("AsyncReceiveY"),
                stageYForceMove);
            Task<int> pickerXMove = useContiVisionFollowEntry
                ? MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
                    "Place ContiNode PickerX 팔로잉 진입",
                    placeTargetName,
                    ct)
                : MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    pickerXTarget,
                    "Place ContiNode PickerX 비동기 이동",
                    ct,
                    placeTargetName,
                    pickerXForceMove);
            Task<int> pickerZPrePlaceMove = MovePickerZPlaceAfterContiProgressAsync(
                stageY,
                pickerX,
                stageYStart,
                stageYTarget,
                pickerXStart,
                pickerXTarget,
                stageYMove,
                pickerXMove,
                currentPickerZAxis,
                prePlacePickerZ,
                placeConfig,
                stageYForceMove,
                pickerXForceMove,
                ct);

            int[] moveResults = await Task.WhenAll(stageYMove, pickerXMove, pickerZPrePlaceMove).ConfigureAwait(false);
            if (moveResults[0] != 0 || moveResults[1] != 0 || moveResults[2] != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return Fail("PICKER-PLACE-CONTI-ASYNC-MOVE", Name,
                    "Place ContiNode 비동기 PrePlace 이동 실패. " +
                    "stageYResult=" + moveResults[0] +
                    ", pickerXResult=" + moveResults[1] +
                    ", pickerZPrePlaceResult=" + moveResults[2] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            int finalWait = await WaitContiSegmentedPlaceFinalPositionAsync(
                yAxis,
                stageYTarget,
                pickerXTarget,
                null,
                previousPickerZAvoid,
                currentPickerZAxis,
                Math.Max(placeConfig.ContiTimeoutMs, ResolveTimeout()),
                ct).ConfigureAwait(false);
            if (finalWait != 0)
                return finalWait;

            // StageY와 PickerX 최종 도착 확인 후에만 PickerZ를 최종 Place 접촉 위치로 이동합니다.
            int inspectionGateResult = await EnsureInspectionResultsReadyBeforePlaceDownAsync(ct).ConfigureAwait(false);
            if (inspectionGateResult != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return inspectionGateResult;
            }

            _targetPickerZ = finalPickerZ;
            int finalPlaceZResult = await MovePickerAxisAndVerifyAsync(
                currentPickerZAxis,
                finalPickerZ,
                "Place ContiNode PickerZ PrePlace 후 최종 Place 하강",
                ct,
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
            if (finalPlaceZResult != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return finalPlaceZResult;
            }

            WriteLog("PickerPlaceSequence",
                Name + " Place ContiNode 비동기 이동 완료. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", basePickerZ=" + originalPickerZTarget.ToString("F3") +
                ", prePlacePickerZ=" + prePlacePickerZ.ToString("F3") +
                ", finalPickerZ=" + finalPickerZ.ToString("F3") +
                ", stageYStart=" + stageYStart.ToString("F3") +
                ", stageYTarget=" + stageYTarget.ToString("F3") +
                ", pickerXStart=" + pickerXStart.ToString("F3") +
                ", pickerXTarget=" + pickerXTarget.ToString("F3") +
                ", triggerRatio=" + placeConfig.ContiXYMidRatio.ToString("F3") +
                " - Ok");
            _pickerZPlacedByContiSegmentedPlace = true;
            return 0;
        }

        private IList<PickerPlaceContiNode> BuildContiSegmentedPlaceNodes(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            BaseAxis pickerZ,
            PickerPlaceMotionConfig placeConfig)
        {
            if (stageY == null || pickerX == null || previousPickerZ == null || pickerZ == null || placeConfig == null)
                return new List<PickerPlaceContiNode>();

            PickerAxis previousZAxis = GetPickerZAxis(_pendingContiRetreatPickerIndex);
            PickerAxis currentZAxis = GetPickerZAxis(_currentPickerIndex);

            double previousPlaceBase = GetPickerTeachingPosition(previousZAxis, "PlacePosition");
            double previousAvoid = GetPickerTeachingPosition(previousZAxis, "AvoidPosition");
            double currentPlaceBase = _targetPickerZ;
            double currentAvoid = GetPickerTeachingPosition(currentZAxis, "AvoidPosition");
            double tapeThickness = ResolveTapeThicknessMm(placeConfig);
            double dieThickness = ResolveDieThicknessMm(placeConfig);
            double materialOffset = tapeThickness + dieThickness;
            double previousMaterialBase = previousPlaceBase + materialOffset;
            double currentMaterialBase = currentPlaceBase + materialOffset;

            double previousZNode0 = previousMaterialBase + placeConfig.ContiZ1Step1Clearance;
            double previousZNode1 = previousMaterialBase + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double previousZNearAvoid = ResolveNearAvoidPosition(previousAvoid, previousPlaceBase, placeConfig.ContiNearAvoidDistance);
            double currentZNearAvoid = ResolveNearAvoidPosition(currentAvoid, currentPlaceBase, placeConfig.ContiNearAvoidDistance);
            double currentZNode3 = currentMaterialBase + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double currentZFinal = currentPlaceBase - placeConfig.ContiOverDrive;

            double ratio = placeConfig.ContiXYMidRatio;
            double stageYMid = stageY.ActualPosition + ((_targetOutputStageY - stageY.ActualPosition) * ratio);
            double pickerXMid = pickerX.ActualPosition + ((_targetPickerX - pickerX.ActualPosition) * ratio);

            var nodes = new List<PickerPlaceContiNode>();
            nodes.Add(new PickerPlaceContiNode(0, stageY.ActualPosition, pickerX.ActualPosition, previousZNode0, pickerZ.ActualPosition));
            nodes.Add(new PickerPlaceContiNode(1, stageY.ActualPosition, pickerX.ActualPosition, previousZNode1, pickerZ.ActualPosition));
            nodes.Add(new PickerPlaceContiNode(2, stageYMid, pickerXMid, previousZNearAvoid, currentZNearAvoid));
            nodes.Add(new PickerPlaceContiNode(3, _targetOutputStageY, _targetPickerX, previousZNearAvoid, currentZNode3));
            nodes.Add(new PickerPlaceContiNode(4, _targetOutputStageY, _targetPickerX, previousAvoid, currentZFinal));
            return nodes;
        }

        private bool CanUseContiSegmentedPlaceFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis pickerZ,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = string.Empty;

            if (!HasPendingContiRetreat())
            {
                reason = "이전 PickerZ 복귀 지연 대상이 없어 ContiNode에 포함할 Z1 축이 없습니다.";
                return false;
            }

            if (!CanUseContiSegmentedPlaceBaseFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out reason))
                return false;

            return true;
        }

        private bool CanUseContiSegmentedNodesFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            BaseAxis pickerZ,
            IList<PickerPlaceContiNode> nodes,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = string.Empty;

            if (nodes == null || nodes.Count == 0)
            {
                reason = "ContiNode 노드가 없습니다.";
                return false;
            }

            double maxTravel = placeConfig != null ? placeConfig.ContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

            foreach (PickerPlaceContiNode node in nodes)
            {
                if (Math.Abs(node.StageY - stageY.ActualPosition) > maxTravel ||
                    Math.Abs(node.PickerX - pickerX.ActualPosition) > maxTravel ||
                    Math.Abs(node.PreviousPickerZ - previousPickerZ.ActualPosition) > maxTravel ||
                    Math.Abs(node.PickerZ - pickerZ.ActualPosition) > maxTravel)
                {
                    reason = "node" + node.Index + " 목표 위치가 연속 Place ContiNode 허용 거리 밖입니다.";
                    return false;
                }
            }

            return true;
        }

        private static double ResolveNearAvoidPosition(double avoidPosition, double placePosition, double distanceFromAvoid)
        {
            if (distanceFromAvoid <= 0.0)
                return avoidPosition;

            double directionToPlace = placePosition >= avoidPosition ? 1.0 : -1.0;
            return avoidPosition + (directionToPlace * distanceFromAvoid);
        }

        private double ResolveTapeThicknessMm(PickerPlaceMotionConfig placeConfig)
        {
            double fallback = placeConfig != null ? placeConfig.ContiTapeThicknessFallback : 0.10;

            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project != null)
                    return NormalizeThicknessMm(project.TapeThickness, fallback);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode Tape 두께 로드 실패. fallback=" + fallback.ToString("F3") +
                    ", error=" + ex.Message + " - Check");
            }

            return fallback;
        }

        private double ResolveDieThicknessMm(PickerPlaceMotionConfig placeConfig)
        {
            double fallback = placeConfig != null ? placeConfig.ContiDieThicknessFallback : 0.15;

            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project != null)
                {
                    if (project.Die != null && project.Die.ThicknessMm > 0.0)
                        return NormalizeThicknessMm(project.Die.ThicknessMm, fallback);

                    return NormalizeThicknessMm(project.ChipThickness, fallback);
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode Die 두께 로드 실패. fallback=" + fallback.ToString("F3") +
                    ", error=" + ex.Message + " - Check");
            }

            return fallback;
        }

        private static double NormalizeThicknessMm(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                return fallback;

            if (value > 5.0)
                return value / 1000.0;

            return value;
        }

        private async Task<int> WaitContiSegmentedPlaceFinalPositionAsync(
            BinStageAxis yAxis,
            double stageYTarget,
            double pickerXTarget,
            PickerAxis? previousPickerZAxis,
            double previousPickerZAvoid,
            PickerAxis pickerZAxis,
            int timeoutMs,
            CancellationToken ct)
        {
            int stageYWait = await OutputStage.WaitStageAxisMoveDoneInPosition(
                yAxis,
                stageYTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (stageYWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-STAGE-Y", "OutputStage",
                    "Place ContiNode 이동 후 OutputStageY 최종 위치 대기 실패. waitCode=" + stageYWait +
                    ". " + OutputStage.BuildStageAxisState(yAxis, stageYTarget));
            }

            int pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                pickerXTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-PICKER-X", Name,
                    "Place ContiNode 이동 후 PickerX 최종 위치 대기 실패. waitCode=" + pickerXWait +
                    ". " + BuildPickerAxisState(PickerAxis.PickerX, pickerXTarget));
            }

            if (previousPickerZAxis.HasValue)
            {
                int previousPickerZWait = await WaitPickerAxisMoveDoneAsync(
                    previousPickerZAxis.Value,
                    previousPickerZAvoid,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                if (previousPickerZWait != 0)
                {
                    return Fail("PICKER-PLACE-CONTI-PREV-PICKER-Z", Name,
                        "Place ContiNode 이동 후 이전 PickerZ Avoid 최종 위치 대기 실패. pickerNo=" + _pendingContiRetreatPickerNo +
                        ", waitCode=" + previousPickerZWait +
                        ". " + BuildPickerAxisState(previousPickerZAxis.Value, previousPickerZAvoid));
                }
            }

            int pickerZWait = await WaitPickerAxisMoveDoneAsync(
                pickerZAxis,
                _targetPickerZ,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerZWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-PICKER-Z", Name,
                    "Place ContiNode 이동 후 PickerZ 최종 위치 대기 실패. pickerNo=" + _currentPickerNo +
                    ", waitCode=" + pickerZWait +
                    ". " + BuildPickerAxisState(pickerZAxis, _targetPickerZ));
            }

            return 0;
        }

        private bool IsFirstPlaceMoveInBatch()
        {
            return _pickerCursor <= 0;
        }

        private bool CanUseContiSegmentedPlaceBaseFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis pickerZ,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = "";

            if (stageY == null)
            {
                reason = "OutputStageY 축을 찾을 수 없습니다.";
                return false;
            }

            if (pickerX == null)
            {
                reason = "PickerX 축을 찾을 수 없습니다.";
                return false;
            }

            if (pickerZ == null)
            {
                reason = "PickerZ 축을 찾을 수 없습니다.";
                return false;
            }

            if (HasPendingContiRetreat() && previousPickerZ == null)
            {
                reason = "이전 PickerZ 복귀 대상 축을 찾을 수 없습니다.";
                return false;
            }

            double maxTravel = placeConfig != null ? placeConfig.ContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

            double stageYTravel = Math.Abs(_targetOutputStageY - stageY.ActualPosition);
            double pickerXTravel = Math.Abs(_targetPickerX - pickerX.ActualPosition);
            double pickerZTravel = Math.Abs(_targetPickerZ - pickerZ.ActualPosition);
            double previousPickerZTravel = previousPickerZ != null ? Math.Abs(previousPickerZAvoid - previousPickerZ.ActualPosition) : 0.0;

            if (IsPickerXAxisAtAnyAvoidPosition())
            {
                reason = "PickerX가 Avoid 계열 위치에 있어 Place 첫 접근으로 판단됩니다.";
                return false;
            }

            if (stageYTravel > maxTravel || pickerXTravel > maxTravel || pickerZTravel > maxTravel || previousPickerZTravel > maxTravel)
            {
                reason = "현재 위치가 연속 Place ContiNode 허용 거리 밖입니다.";
                return false;
            }

            return true;
        }

        private bool IsPickerXAxisAtAnyAvoidPosition()
        {
            return IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition")) ||
                IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition")) ||
                IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "OutputAvoidPosition"));
        }

        private bool ShouldDelayCurrentPickerZRetreatForNextContiPlace()
        {
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
                return false;

            if (WillCurrentPlaceCompleteOutputStage())
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place이므로 PickerZ Avoid 복귀를 다음 Conti Place로 지연하지 않습니다. " +
                    "side=" + Side + ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") + " - Check");
                return false;
            }

            int nextCursor = _pickerCursor + 1;
            if (nextCursor >= _pickedPickerIndexes.Count)
                return false;

            int nextPickerIndex = _pickedPickerIndexes[nextCursor];
            int nextPickerNo = ToPickerNo(nextPickerIndex);
            DieMaterial nextDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, nextPickerNo);
            if (nextDie == null)
                return false;

            BinSide nextSide;
            if (!TryResolveOutputSide(nextDie, out nextSide))
                return false;

            return nextSide == _currentOutputSide;
        }

        private bool WillCurrentPlaceCompleteOutputStage()
        {
            try
            {
                if (_receiveTarget == null || _receiveTarget.OrderIndex < 0)
                    return false;

                WaferMaterial outputWafer = MaterialStateService.GetWaferAtLocation(_receiveTarget.StageLocation);
                if (outputWafer == null || outputWafer.OutputReceiveTotalCount <= 0)
                    return false;

                if (!string.IsNullOrWhiteSpace(_receiveTarget.OutputWaferId) &&
                    !string.Equals(outputWafer.WaferId, _receiveTarget.OutputWaferId, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (outputWafer.OutputReceiveSlots != null && outputWafer.OutputReceiveSlots.Count > 0)
                {
                    int pendingTargetCount = 0;
                    bool currentTargetIsPending = false;
                    for (int i = 0; i < outputWafer.OutputReceiveSlots.Count; i++)
                    {
                        OutputReceiveSlotMaterial slot = outputWafer.OutputReceiveSlots[i];
                        bool pending = slot != null &&
                                       slot.IsTarget &&
                                       slot.Result == DieResult.Unknown &&
                                       string.IsNullOrWhiteSpace(slot.DieUid);
                        if (!pending)
                            continue;

                        pendingTargetCount++;
                        if (slot.OrderIndex == _receiveTarget.OrderIndex)
                            currentTargetIsPending = true;
                    }

                    if (currentTargetIsPending)
                        return pendingTargetCount == 1;
                }

                return _receiveTarget.OrderIndex >= outputWafer.OutputReceiveTotalCount - 1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place 판단 중 예외가 발생하여 Z 복귀 지연을 금지합니다. " +
                    "error=" + ex.Message + " - Check");
                return true;
            }
        }

        private bool TryResolveOutputSide(DieMaterial die, out BinSide side)
        {
            side = BinSide.Good;
            if (die == null)
                return false;

            OutputStageResultRoutingMode routingMode = ResolveOutputStageResultRoutingMode();
            if (routingMode == OutputStageResultRoutingMode.ForceGoodStage)
            {
                side = BinSide.Good;
                return true;
            }

            if (routingMode != OutputStageResultRoutingMode.RouteByInspectionResult ||
                !IsInspectionFlowComplete(die))
            {
                return false;
            }

            if (die.Result == DieResult.Good)
            {
                side = BinSide.Good;
                return true;
            }

            if (die.Result == DieResult.NG)
            {
                side = BinSide.Ng;
                return true;
            }

            return false;
        }

        private bool HasPendingContiRetreat()
        {
            return _pendingContiRetreatPickerIndex >= 0;
        }

        private void SetPendingContiRetreat(int pickerIndex, int pickerNo)
        {
            _pendingContiRetreatPickerIndex = pickerIndex;
            _pendingContiRetreatPickerNo = pickerNo;
        }

        private void ClearPendingContiRetreat()
        {
            _pendingContiRetreatPickerIndex = -1;
            _pendingContiRetreatPickerNo = 0;
        }

        private async Task<int> CompletePendingContiRetreatIfNeededAsync(string description, CancellationToken ct)
        {
            if (!HasPendingContiRetreat())
                return 0;

            PickerAxis zAxis = GetPickerZAxis(_pendingContiRetreatPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                avoid,
                description,
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            ClearPendingContiRetreat();
            return 0;
        }

        private static string FormatTravel(BaseAxis axis, double target)
        {
            if (axis == null)
                return "axis=null";

            return Math.Abs(target - axis.ActualPosition).ToString("F3");
        }

        private async Task<int> MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerY] = _targetPickerY;
            AddLoadedPickerTPlaceTargets(targets);

            return await MovePickerAxesAndVerifyAsync(
                targets,
                "place picker Y/T before synchronized arrival",
                ct,
                BuildPlaceMoveTargetName()).ConfigureAwait(false);
        }

        private static bool RequiresContiPlaceForceMove(BaseAxis axis, double target)
        {
            if (axis == null)
                return true;

            axis.UpdateStatus();
            return !IsContiPlaceAxisSkipEligible(axis, target);
        }

        private static bool IsContiPlaceAxisSkipEligible(BaseAxis axis, double target)
        {
            if (!IsContiPlaceAxisStrongAtTarget(axis, target))
                return false;

            return IsSameF3(axis.ActualPosition, target) &&
                   IsSameF3(axis.CommandPosition, target);
        }

        private static bool IsContiPlaceAxisStrongAtTarget(BaseAxis axis, double target)
        {
            if (axis == null ||
                !IsFinitePosition(target) ||
                !IsFinitePosition(axis.ActualPosition) ||
                !IsFinitePosition(axis.CommandPosition))
            {
                return false;
            }

            double tolerance = ResolveContiPlaceAxisTolerance(axis);
            return axis.IsServoOn &&
                   !axis.IsAlarm &&
                   !axis.IsMoving &&
                   axis.IsInPosition &&
                   Math.Abs(axis.ActualPosition - target) <= tolerance &&
                   Math.Abs(axis.CommandPosition - target) <= tolerance;
        }

        private static bool IsSameF3(double left, double right)
        {
            if (!IsFinitePosition(left) || !IsFinitePosition(right))
                return false;

            return Math.Round(left, 3, MidpointRounding.AwayFromZero) ==
                   Math.Round(right, 3, MidpointRounding.AwayFromZero);
        }

        private static bool IsFinitePosition(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double ResolveContiPlaceAxisTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.001;
        }

        private static string BuildContiPlaceAxisDecision(BaseAxis axis, double target, bool forceMove)
        {
            if (axis == null)
                return "axis=null, target=" + target.ToString("F6") + ", forceMove=" + forceMove;

            double tolerance = ResolveContiPlaceAxisTolerance(axis);
            return "name=" + axis.Name +
                   ", actual=" + axis.ActualPosition.ToString("F6") +
                   ", command=" + axis.CommandPosition.ToString("F6") +
                   ", target=" + target.ToString("F6") +
                   ", actualF3=" + axis.ActualPosition.ToString("F3") +
                   ", commandF3=" + axis.CommandPosition.ToString("F3") +
                   ", targetF3=" + target.ToString("F3") +
                   ", actualF3Match=" + IsSameF3(axis.ActualPosition, target) +
                   ", commandF3Match=" + IsSameF3(axis.CommandPosition, target) +
                   ", tolerance=" + tolerance.ToString("F6") +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm +
                   ", moving=" + axis.IsMoving +
                   ", inPosition=" + axis.IsInPosition +
                   ", forceMove=" + forceMove;
        }

        private static double ResolveContiAsyncPlaceTriggerPosition(double start, double target, PickerPlaceMotionConfig placeConfig)
        {
            double ratio = placeConfig != null ? placeConfig.ContiXYMidRatio : 0.5;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0.5;
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            return start + ((target - start) * ratio);
        }

        private static bool IsContiAsyncPlaceTriggerReached(double start, double target, double trigger, double actual)
        {
            double travel = target - start;
            if (Math.Abs(travel) <= 0.000001)
                return true;

            return travel > 0.0
                 ? actual >= trigger
                 : actual <= trigger;
        }

        private static bool IsContiPlaceAxisThresholdReady(
            BaseAxis axis,
            double start,
            double target,
            double trigger,
            bool triggerReached,
            bool moveSucceeded)
        {
            if (!triggerReached ||
                axis == null ||
                !axis.IsServoOn ||
                axis.IsAlarm ||
                !IsFinitePosition(axis.ActualPosition) ||
                !IsFinitePosition(axis.CommandPosition) ||
                !IsFinitePosition(target))
            {
                return false;
            }

            // ratio=0 또는 실질 이동량이 없는 경우에는 명령 완료 전 즉시 Z 하강하지 않습니다.
            if (Math.Abs(trigger - start) <= 0.000001)
                return false;

            // 이 Task가 바로 위에서 해당 target으로 발행한 이동을 소유합니다.
            // 실장비 CommandPosition은 이동 중 궤적 위치이므로 target 일치 조건은 최종 완료 판정에서만 적용합니다.
            return axis.IsMoving ||
                   (moveSucceeded && IsContiPlaceAxisStrongAtTarget(axis, target));
        }

        private async Task<int> MovePickerZPlaceAfterContiProgressAsync(
            BaseAxis stageY,
            BaseAxis pickerX,
            double stageYStart,
            double stageYTarget,
            double pickerXStart,
            double pickerXTarget,
            Task<int> stageYMove,
            Task<int> pickerXMove,
            PickerAxis pickerZAxis,
            double pickerZTarget,
            PickerPlaceMotionConfig placeConfig,
            bool stageYForceMove,
            bool pickerXForceMove,
            CancellationToken ct)
        {
            CancellationTokenSource inspectionGateCancellation = null;
            Task<int> inspectionGateTask = null;
            bool inspectionGateObserved = false;

            try
            {
                ct.ThrowIfCancellationRequested();

                double stageYTrigger = ResolveContiAsyncPlaceTriggerPosition(stageYStart, stageYTarget, placeConfig);
                double pickerXTrigger = ResolveContiAsyncPlaceTriggerPosition(pickerXStart, pickerXTarget, placeConfig);
                int timeoutMs = Math.Max(1000, Math.Max(placeConfig != null ? placeConfig.ContiTimeoutMs : 0, ResolveTimeout()));
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                bool stageYMoveSucceeded = false;
                bool pickerXMoveSucceeded = false;
                bool stageYReachedByThreshold = false;
                bool pickerXReachedByThreshold = false;
                bool stageYReachedByCompletion = false;
                bool pickerXReachedByCompletion = false;
                bool inspectionGateSucceeded = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (stageYMove != null && stageYMove.IsCompleted)
                    {
                        int stageResult = await stageYMove.ConfigureAwait(false);
                        if (stageResult != 0)
                            return stageResult;
                        stageYMoveSucceeded = true;
                    }

                    if (pickerXMove != null && pickerXMove.IsCompleted)
                    {
                        int pickerXResult = await pickerXMove.ConfigureAwait(false);
                        if (pickerXResult != 0)
                            return pickerXResult;
                        pickerXMoveSucceeded = true;
                    }

                    if (stageY != null)
                        stageY.UpdateStatus();
                    if (pickerX != null)
                        pickerX.UpdateStatus();

                    if (stageY == null || pickerX == null)
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 확인 대상 축을 찾을 수 없습니다. " +
                            "stageY=" + (stageY != null) +
                            ", pickerX=" + (pickerX != null));
                    }

                    if (!stageY.IsServoOn || stageY.IsAlarm ||
                        !IsFinitePosition(stageY.ActualPosition) ||
                        !IsFinitePosition(stageY.CommandPosition))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 OutputStageY 상태가 비정상입니다. " +
                            BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove));
                    }

                    if (!pickerX.IsServoOn || pickerX.IsAlarm ||
                        !IsFinitePosition(pickerX.ActualPosition) ||
                        !IsFinitePosition(pickerX.CommandPosition))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 PickerX 상태가 비정상입니다. " +
                            BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove));
                    }

                    double stageYActual = stageY != null ? stageY.ActualPosition : stageYStart;
                    double pickerXActual = pickerX != null ? pickerX.ActualPosition : pickerXStart;
                    bool stageYTriggerReached = IsContiAsyncPlaceTriggerReached(stageYStart, stageYTarget, stageYTrigger, stageYActual);
                    bool pickerXTriggerReached = IsContiAsyncPlaceTriggerReached(pickerXStart, pickerXTarget, pickerXTrigger, pickerXActual);
                    bool thresholdWindowOpen = DateTime.UtcNow < deadline;
                    stageYReachedByThreshold = thresholdWindowOpen && IsContiPlaceAxisThresholdReady(
                        stageY,
                        stageYStart,
                        stageYTarget,
                        stageYTrigger,
                        stageYTriggerReached,
                        stageYMoveSucceeded);
                    pickerXReachedByThreshold = thresholdWindowOpen && IsContiPlaceAxisThresholdReady(
                        pickerX,
                        pickerXStart,
                        pickerXTarget,
                        pickerXTrigger,
                        pickerXTriggerReached,
                        pickerXMoveSucceeded);
                    stageYReachedByCompletion = stageYMoveSucceeded && IsContiPlaceAxisStrongAtTarget(stageY, stageYTarget);
                    pickerXReachedByCompletion = pickerXMoveSucceeded && IsContiPlaceAxisStrongAtTarget(pickerX, pickerXTarget);
                    bool stageYReached = stageYReachedByThreshold || stageYReachedByCompletion;
                    bool pickerXReached = pickerXReachedByThreshold || pickerXReachedByCompletion;

                    if (stageYReached && pickerXReached && inspectionGateTask == null)
                    {
                        WriteLog("PickerPlaceSequence",
                            Name + " Place ContiNode XY 트리거 도달, 검사 결과 게이트 확인을 시작합니다. " +
                            "stageYReachedBy=" + (stageYReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                            ", pickerXReachedBy=" + (pickerXReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                            " - Check");
                        inspectionGateCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        inspectionGateTask = EnsureInspectionResultsReadyBeforePlaceDownAsync(
                            inspectionGateCancellation.Token);
                    }

                    if (!inspectionGateSucceeded &&
                        inspectionGateTask != null &&
                        inspectionGateTask.IsCompleted)
                    {
                        int inspectionGateResult;
                        try
                        {
                            inspectionGateResult = await inspectionGateTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            inspectionGateObserved = true;
                        }

                        if (inspectionGateResult != 0)
                            return inspectionGateResult;
                        inspectionGateSucceeded = true;
                    }

                    // 검사 결과 대기 중에도 XY 상태를 매 주기 재확인하고,
                    // 검사 완료와 같은 상태 스냅샷에서 두 축이 모두 안전할 때만 Z 하강을 허용합니다.
                    if (stageYReached && pickerXReached && inspectionGateSucceeded)
                        break;

                    if (DateTime.UtcNow >= deadline && (!stageYReached || !pickerXReached))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER-TIMEOUT", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 시간이 초과되었습니다. " +
                            "timeoutMs=" + timeoutMs +
                            ", stageYStart=" + stageYStart.ToString("F6") +
                            ", stageYTrigger=" + stageYTrigger.ToString("F6") +
                            ", stageYActual=" + stageYActual.ToString("F6") +
                            ", stageYTarget=" + stageYTarget.ToString("F6") +
                            ", stageYMoveSucceeded=" + stageYMoveSucceeded +
                            ", stageYReachedByThreshold=" + stageYReachedByThreshold +
                            ", stageYReachedByCompletion=" + stageYReachedByCompletion +
                            ", stageYState={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove) + "}" +
                            ", pickerXStart=" + pickerXStart.ToString("F6") +
                            ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                            ", pickerXActual=" + pickerXActual.ToString("F6") +
                            ", pickerXTarget=" + pickerXTarget.ToString("F6") +
                            ", pickerXMoveSucceeded=" + pickerXMoveSucceeded +
                            ", pickerXReachedByThreshold=" + pickerXReachedByThreshold +
                            ", pickerXReachedByCompletion=" + pickerXReachedByCompletion +
                            ", pickerXState={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove) + "}");
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode PickerZ 하강 트리거 도달. " +
                    "pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", stageYStart=" + stageYStart.ToString("F6") +
                    ", stageYTrigger=" + stageYTrigger.ToString("F6") +
                    ", stageYActual=" + (stageY != null ? stageY.ActualPosition.ToString("F6") : "-") +
                    ", stageYTarget=" + stageYTarget.ToString("F6") +
                    ", stageYReachedBy=" + (stageYReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                    ", pickerXStart=" + pickerXStart.ToString("F6") +
                    ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                    ", pickerXActual=" + (pickerX != null ? pickerX.ActualPosition.ToString("F6") : "-") +
                    ", pickerXTarget=" + pickerXTarget.ToString("F6") +
                    ", pickerXReachedBy=" + (pickerXReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                    ", pickerZTarget=" + pickerZTarget.ToString("F6") +
                    " - Start");

                return await MovePickerAxisAndVerifyAsync(
                    pickerZAxis,
                    pickerZTarget,
                    "Place ContiNode PickerZ 비동기 하강",
                    ct,
                    BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-CONTI-ASYNC-Z-EX", Name,
                    "Place ContiNode PickerZ 비동기 하강 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (inspectionGateCancellation != null)
                {
                    try
                    {
                        if (!inspectionGateObserved && inspectionGateTask != null)
                        {
                            inspectionGateCancellation.Cancel();
                            await DrainGateTaskAfterCancellationAsync(
                                inspectionGateTask,
                                "Place ContiNode PickerZ 하강 검사 결과 게이트").ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        inspectionGateCancellation.Dispose();
                    }
                }
            }
        }

    }
}
