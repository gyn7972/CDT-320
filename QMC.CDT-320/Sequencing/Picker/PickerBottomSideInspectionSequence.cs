using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomSideInspectionSequence : PickerSequenceBase<PickerBottomSideInspectionStep>
    {
        private const int VisionInspectionSettleDelayMs = 100;
        private const int SideInspectionTurnSettleDelayMs = 100;

        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private readonly List<BottomShot> _pendingBottomShots = new List<BottomShot>();
        private readonly List<int> _sideReadyPickerIndexes = new List<int>();
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();

        private bool _bottomInspectionYReady;
        private bool _sideInspectionYReady;
        private SequenceResourceLease _inspectionAreaLease;

        private sealed class InspectionTarget
        {
            public int PickerIndex;
            public int PickerNo;
            public DieMaterial Die;
            public double X;
            public double Y;
            public double Z;
            public double T0;
            public double T90;
        }

        private sealed class BottomShot
        {
            public InspectionTarget Target;
            public bool Applied;
        }

        private sealed class PendingT0Return
        {
            public int PickerIndex;
            public double Target;
            public Task<int> MoveTask;
        }

        public PickerBottomSideInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Inspect, side == PickerSequenceSide.Front ? "FrontPickerBottomSideInspectionSequence" : "RearPickerBottomSideInspectionSequence")
        {
            CurrentStep = PickerBottomSideInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerBottomSideInspectionStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ReleaseInspectionArea();
                _pendingBottomShots.Clear();
                _sideReadyPickerIndexes.Clear();
                _pendingT0Returns.Clear();
                CurrentStep = PickerBottomSideInspectionStep.Complete;
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
            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return Fail("PICKER-BOTTOM-SIDE-MANUAL-NOT-SUPPORTED", Name, "Bottom/Side 통합 검사는 Auto 전용 시퀀스입니다. 기존 Bottom 또는 Side 메뉴얼 시퀀스를 사용하세요.");

                CurrentStep = PickerBottomSideInspectionStep.CheckUnit;
                int result = CheckUnit();
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.BuildPickedPickerList;
                result = BuildPickedPickerList();
                if (result != 0 || CurrentStep == PickerBottomSideInspectionStep.Complete)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.AcquireInspectionArea;
                result = await AcquireInspectionAreaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.MoveOppositePickerToAvoidBeforeInspection;
                result = await MoveOppositePickerToAvoidAndVerifyAsync("Bottom/Side 통합 검사 진입 전 상대 Picker 상태 확인", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.RunBottomPipeline;
                result = await RunBottomPipelineAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.RunSidePipeline;
                result = await RunSidePipelineAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.MoveFinalZToAvoid;
                result = await MoveAllPickerZToAvoidAndVerifyAsync("Bottom/Side 통합 검사 완료 후 PickerZ 전체 Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.CompletePendingT0Return;
                result = await CompletePendingT0ReturnAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.MoveFinalYToAvoid;
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    "Bottom/Side 통합 검사 완료 후 PickerY Avoid",
                    ct,
                    "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.MoveFinalXToAvoid;
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    "Bottom/Side 통합 검사 완료 후 PickerX Avoid",
                    ct,
                    "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomSideInspectionStep.Complete;
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
                return Fail("PICKER-BOTTOM-SIDE-EX", Name, "Bottom/Side 통합 검사 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (CurrentStep == PickerBottomSideInspectionStep.Complete)
                    ReleaseInspectionArea();
            }
        }

        private int CheckUnit()
        {
            if (!IsPickerSideEnabled())
            {
                CurrentStep = PickerBottomSideInspectionStep.Complete;
                WriteLog("PickerBottomSideInspectionSequence", Name + " Picker 사용 설정이 꺼져 있어 Bottom/Side 통합 검사를 완료 처리합니다. side=" + Side + " - Check");
                return 0;
            }

            CurrentStep = PickerBottomSideInspectionStep.BuildPickedPickerList;
            return 0;
        }

        private int BuildPickedPickerList()
        {
            _pickedPickerIndexes.Clear();
            _pendingBottomShots.Clear();
            _sideReadyPickerIndexes.Clear();
            _pendingT0Returns.Clear();
            _bottomInspectionYReady = false;
            _sideInspectionYReady = false;

            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerBottomSideInspectionSequence"));
            if (_pickedPickerIndexes.Count == 0)
            {
                CurrentStep = PickerBottomSideInspectionStep.Complete;
                return 0;
            }

            WriteLog("PickerBottomSideInspectionSequence",
                Name + " Bottom/Side 통합 검사 대상 구성 완료. count=" + _pickedPickerIndexes.Count + " - Ok");
            CurrentStep = PickerBottomSideInspectionStep.AcquireInspectionArea;
            return 0;
        }

        private async Task<int> AcquireInspectionAreaAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_inspectionAreaLease == null)
                {
                    _inspectionAreaLease = await AcquireResourceAsync(
                        SequenceResourceKind.InspectionArea,
                        Name + ":BottomSide",
                        ct).ConfigureAwait(false);
                    if (_inspectionAreaLease == null)
                        return -1;
                }

                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "BottomSideInspection");

                int result = await MoveAllPickerZToAvoidAndVerifyAsync("Bottom/Side 통합 검사 진입 전 PickerZ 전체 Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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
                return Fail("PICKER-BOTTOM-SIDE-RESOURCE", Name, "Bottom/Side 통합 검사 리소스 점유 실패. InspectionArea를 확인하세요. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunBottomPipelineAsync(CancellationToken ct)
        {
            EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "BottomSideInspection:Bottom");

            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                InspectionTarget target = BuildBottomTarget(_pickedPickerIndexes[i]);
                if (target == null || target.Die == null)
                    continue;

                if (HasInspectionResult(target.Die, "Bottom"))
                {
                    if (!_sideReadyPickerIndexes.Contains(target.PickerIndex))
                        _sideReadyPickerIndexes.Add(target.PickerIndex);

                    WriteLog("PickerBottomSideInspectionSequence",
                        Name + " 기존 Bottom 검사 결과가 있어 Bottom shot을 생략하고 SideReady로 등록합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                    continue;
                }

                int result = await MoveBottomTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await TriggerBottomInspectionAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                _pendingBottomShots.Add(new BottomShot { Target = target });

                // Vision 결과 대기 시간을 뒤로 밀기 위해 최소 3번째 shot 이후부터 앞쪽 결과를 회수한다.
                if (_pendingBottomShots.Count >= 3)
                {
                    result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
            }

            WriteLog("PickerBottomSideInspectionSequence",
                Name + " Bottom shot 전체 완료. pendingResult=" + CountPendingBottomResults() + " - Ok");
            return 0;
        }

        private InspectionTarget BuildBottomTarget(int pickerIndex)
        {
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
                return null;

            return new InspectionTarget
            {
                PickerIndex = pickerIndex,
                PickerNo = pickerNo,
                Die = die,
                X = ResolvePickerZoneX("DieBottomPosition", pickerIndex),
                Y = ResolvePickerZoneY("DieBottomPosition", pickerIndex),
                Z = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "BottomPosition"),
                T0 = GetPickerTeachingPosition(GetPickerTAxis(pickerIndex), "BottomPosition") + ResolvePickerAlignOffsetT(pickerIndex)
            };
        }

        private async Task<int> MoveBottomTargetAsync(InspectionTarget target, CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = target.X;
            if (!_bottomInspectionYReady || !IsPickerAxisInPosition(PickerAxis.PickerY, target.Y))
                targets[PickerAxis.PickerY] = target.Y;

            int result = await MovePickerXTThenYAndVerifyAsync(
                targets,
                "Bottom/Side 통합 Bottom X/Y",
                ct,
                BuildBottomTargetName(target)).ConfigureAwait(false);
            if (result != 0)
                return result;

            _bottomInspectionYReady = IsPickerAxisInPosition(PickerAxis.PickerY, target.Y);
            if (!_bottomInspectionYReady)
            {
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    target.Y,
                    "Bottom/Side 통합 Bottom Y",
                    ct,
                    BuildBottomTargetName(target)).ConfigureAwait(false);
                if (result != 0)
                    return result;
                _bottomInspectionYReady = true;
            }

            result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(target.PickerIndex),
                target.Z,
                "Bottom/Side 통합 Bottom Z",
                ct,
                BuildBottomTargetName(target)).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(target.PickerIndex),
                target.T0,
                "Bottom/Side 통합 Bottom T",
                ct,
                BuildBottomTargetName(target)).ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private string BuildBottomTargetName(InspectionTarget target)
        {
            return "DieBottomPosition[" + target.PickerIndex + "];PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Side";
        }

        private async Task<int> TriggerBottomInspectionAsync(InspectionTarget target, CancellationToken ct)
        {
            await Task.Delay(VisionInspectionSettleDelayMs, ct).ConfigureAwait(false);

            int timeoutMs = ResolveTimeout();
            bool triggered = Side == PickerSequenceSide.Front
                ? await FrontPicker.TriggerBottomInspectionExposeAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.TriggerBottomInspectionExposeAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (!triggered)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-TRIGGER", "Vision",
                    "Bottom 검사 노출 요청 실패. die=" + target.Die.DieId +
                    ", pickerNo=" + target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            WriteLog("PickerBottomSideInspectionSequence",
                Name + " Bottom 검사 노출 완료. die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", pendingResult=" + (_pendingBottomShots.Count + 1) + " - Ok");
            return 0;
        }

        private async Task<int> ApplyOldestBottomResultIfNeededAsync(CancellationToken ct)
        {
            for (int i = 0; i < _pendingBottomShots.Count; i++)
            {
                if (!_pendingBottomShots[i].Applied)
                    return await ApplyBottomResultAsync(_pendingBottomShots[i], ct).ConfigureAwait(false);
            }

            return 0;
        }

        private async Task<int> EnsureBottomReadyForSideAsync(int pickerIndex, CancellationToken ct)
        {
            while (!_sideReadyPickerIndexes.Contains(pickerIndex))
            {
                ct.ThrowIfCancellationRequested();

                int result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> ApplyBottomResultAsync(BottomShot shot, CancellationToken ct)
        {
            if (shot == null || shot.Target == null || shot.Applied)
                return 0;

            int timeoutMs = ResolveTimeout();
            BottomVisionOffset result = Side == PickerSequenceSide.Front
                ? await FrontPicker.GetBottomInspectionResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.GetBottomInspectionResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (result == null)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-RESULT", "Vision",
                    "Bottom 검사 결과 수신 실패. die=" + shot.Target.Die.DieId +
                    ", pickerNo=" + shot.Target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            ApplyBottomInspectionResult(shot.Target, result);
            shot.Applied = true;

            if (!_sideReadyPickerIndexes.Contains(shot.Target.PickerIndex))
                _sideReadyPickerIndexes.Add(shot.Target.PickerIndex);

            WriteLog("PickerBottomSideInspectionSequence",
                Name + " Bottom 결과 적용 및 SideReady 등록 완료. die=" + shot.Target.Die.DieId +
                ", pickerNo=" + shot.Target.PickerNo +
                ", sideReadyCount=" + _sideReadyPickerIndexes.Count + " - Ok");
            return 0;
        }

        private void ApplyBottomInspectionResult(InspectionTarget target, BottomVisionOffset result)
        {
            MaterialInspectionResult inspectionResult = result.IsOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;
            DieResult dieResult = result.IsOk && target.Die.Result != DieResult.NG ? DieResult.Good : DieResult.NG;

            MaterialStateService.UpsertInspection(target.Die.DieId, new DieInspectionRecord
            {
                InspectionType = "Bottom",
                Result = inspectionResult,
                Offset = new VisionOffset
                {
                    X = result.OffsetX,
                    Y = result.OffsetY,
                    R = result.OffsetT,
                    IsValid = true
                },
                NgCodes = result.IsOk ? new List<string>() : new List<string> { "BOTTOM_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Bottom",
                        target.PickerIndex,
                        target.X,
                        target.Y,
                        target.T0,
                        target.Z,
                        new VisionOffset
                        {
                            X = result.OffsetX,
                            Y = result.OffsetY,
                            R = result.OffsetT,
                            IsValid = true
                        })
                },
                Measurements = new List<InspectionMeasurement>
                {
                    BuildMeasurement("BottomAlignOffsetX", result.OffsetX, "mm", inspectionResult),
                    BuildMeasurement("BottomAlignOffsetY", result.OffsetY, "mm", inspectionResult),
                    BuildMeasurement("BottomAlignOffsetT", result.OffsetT, "deg", inspectionResult),
                    BuildBooleanMeasurement("BottomInspectionResult", result.IsOk)
                }
            });

            MaterialStateService.ApplyDieInspectionResult(
                target.Die.DieId,
                dieResult,
                result.IsOk ? "" : "BOTTOM_NG",
                "BottomInspection");
        }

        private async Task<int> RunSidePipelineAsync(CancellationToken ct)
        {
            EnsurePickerWorkAreaReserved(PickerWorkZone.Side, "BottomSideInspection:Side");
            _sideInspectionYReady = false;

            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                int pickerIndex = _pickedPickerIndexes[i];
                int result = await EnsureBottomReadyForSideAsync(pickerIndex, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                InspectionTarget target = BuildSideTarget(pickerIndex);
                if (target == null || target.Die == null)
                    continue;

                if (HasInspectionResult(target.Die, "Side0") &&
                    HasInspectionResult(target.Die, "Side90"))
                {
                    WriteLog("PickerBottomSideInspectionSequence",
                        Name + " 기존 Side 검사 결과가 있어 Side shot을 생략합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                    continue;
                }

                result = await InspectSideTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private InspectionTarget BuildSideTarget(int pickerIndex)
        {
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
                return null;

            double t0 = GetPickerTeachingPosition(GetPickerTAxis(pickerIndex), "SidePosition") + ResolvePickerAlignOffsetT(pickerIndex);
            return new InspectionTarget
            {
                PickerIndex = pickerIndex,
                PickerNo = pickerNo,
                Die = die,
                X = ResolvePickerZoneX("DieSidePosition", pickerIndex),
                Y = ResolvePickerZoneY("DieSidePosition", pickerIndex),
                Z = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "SidePosition"),
                T0 = t0,
                T90 = t0 + 90.0
            };
        }

        private async Task<int> InspectSideTargetAsync(InspectionTarget target, CancellationToken ct)
        {
            int result = await MoveSideXAndVision0PositionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(target.PickerIndex),
                target.Z,
                "Bottom/Side 통합 Side Z",
                ct,
                BuildSideTargetName(target)).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(target.PickerIndex),
                target.T0,
                "Bottom/Side 통합 Side T 0도",
                ct,
                BuildSideTargetName(target)).ConfigureAwait(false);
            if (result != 0)
                return result;

            SideVisionResult side0Result = await TriggerAndGetSideResultAsync(target, 0, ct).ConfigureAwait(false);
            if (side0Result == null)
                return Fail("PICKER-BOTTOM-SIDE-SIDE0-RESULT", "Vision", "Side 0도 검사 결과 수신 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);

            await Task.Delay(SideInspectionTurnSettleDelayMs, ct).ConfigureAwait(false);

            result = await MoveSideT90AndVision90PositionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            SideVisionResult side90Result = await TriggerAndGetSideResultAsync(target, 90, ct).ConfigureAwait(false);
            if (side90Result == null)
                return Fail("PICKER-BOTTOM-SIDE-SIDE90-RESULT", "Vision", "Side 90도 검사 결과 수신 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);

            await Task.Delay(SideInspectionTurnSettleDelayMs, ct).ConfigureAwait(false);

            ApplySideInspectionResult(target, side0Result, side90Result);
            QueuePendingT0Return(target.PickerIndex, target.T0);
            StartPendingT0ReturnCommandAsync("다음 Side 검사 중 이전 PickerT 0도 복귀", ct);
            return 0;
        }

        private async Task<int> MoveSideXAndVision0PositionAsync(InspectionTarget target, CancellationToken ct)
        {
            var pickerTargets = new Dictionary<PickerAxis, double>();
            pickerTargets[PickerAxis.PickerX] = target.X;
            if (!_sideInspectionYReady || !IsPickerAxisInPosition(PickerAxis.PickerY, target.Y))
                pickerTargets[PickerAxis.PickerY] = target.Y;

            Task<int> pickerTask = MovePickerXTThenYAndVerifyAsync(
                pickerTargets,
                "Bottom/Side 통합 Side X/Y",
                ct,
                BuildSideTargetName(target));

            Task<int> visionTask = IsSideVisionProcessPositionReady(0)
                ? Task.FromResult(0)
                : MoveSideVisionProcessPositionAsync(target, 0, ct);

            int[] results = await Task.WhenAll(pickerTask, visionTask).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-BOTTOM-SIDE-SIDE-X-VISION0", Name,
                    "Side 0도 진입 X/Y와 SideVisionY 0도 병렬 이동 실패. pickerResult=" + results[0] +
                    ", visionResult=" + results[1] +
                    ", pickerNo=" + target.PickerNo);
            }

            _sideInspectionYReady = IsPickerAxisInPosition(PickerAxis.PickerY, target.Y);
            return 0;
        }

        private async Task<int> MoveSideT90AndVision90PositionAsync(InspectionTarget target, CancellationToken ct)
        {
            Task<int> pickerTask = MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(target.PickerIndex),
                target.T90,
                "Bottom/Side 통합 Side T 90도",
                ct,
                BuildSideTargetName(target));

            Task<int> visionTask = IsSideVisionProcessPositionReady(90)
                ? Task.FromResult(0)
                : MoveSideVisionProcessPositionAsync(target, 90, ct);

            int[] results = await Task.WhenAll(pickerTask, visionTask).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-BOTTOM-SIDE-SIDE-T90-VISION90", Name,
                    "Side 90도 PickerT와 SideVisionY 90도 병렬 이동 실패. pickerResult=" + results[0] +
                    ", visionResult=" + results[1] +
                    ", pickerNo=" + target.PickerNo);
            }

            return 0;
        }

        private async Task<int> MoveSideVisionProcessPositionAsync(InspectionTarget target, int angleDeg, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return Fail("PICKER-BOTTOM-SIDE-VISION-UNIT", "Vision", "Side 검사 카메라 이동 실패. VisionUnit을 찾을 수 없습니다. angle=" + angleDeg + ", pickerNo=" + target.PickerNo);

                int result = angleDeg == 90
                    ? await vision.MoveBothSideVisionProcess90PositionAsync(Options != null && Options.FineMove).ConfigureAwait(false)
                    : await vision.MoveBothSideVisionProcess0PositionAsync(Options != null && Options.FineMove).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-BOTTOM-SIDE-VISION-POSITION", "Vision", "Side 검사 카메라 " + angleDeg + "도 티칭 위치 이동 실패. result=" + result + ", pickerNo=" + target.PickerNo);

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BOTTOM-SIDE-VISION-POSITION-EX", "Vision", "Side 검사 카메라 " + angleDeg + "도 티칭 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsSideVisionProcessPositionReady(int angleDeg)
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return false;

                string positionName = angleDeg == 90 ? "Process90Position" : "Process0Position";
                return vision.IsVisionAxisInTeachingPosition(VisionAxis.FrontSideVisionY, positionName) &&
                       vision.IsVisionAxisInTeachingPosition(VisionAxis.RearSideVisionY, positionName);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<SideVisionResult> TriggerAndGetSideResultAsync(InspectionTarget target, int angleDeg, CancellationToken ct)
        {
            await Task.Delay(VisionInspectionSettleDelayMs, ct).ConfigureAwait(false);

            int timeoutMs = ResolveTimeout();
            bool triggered = Side == PickerSequenceSide.Front
                ? await FrontPicker.TriggerSideInspectionExposeAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.TriggerSideInspectionExposeAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);

            if (!triggered)
                return null;

            return Side == PickerSequenceSide.Front
                ? await FrontPicker.GetSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.GetSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);
        }

        private string BuildSideTargetName(InspectionTarget target)
        {
            return "DieSidePosition[" + target.PickerIndex + "];PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Side";
        }

        private void ApplySideInspectionResult(InspectionTarget target, SideVisionResult side0Result, SideVisionResult side90Result)
        {
            bool ok0 = side0Result != null && side0Result.IsAllOk;
            bool ok90 = side90Result != null && side90Result.IsAllOk;
            bool ok = ok0 && ok90 && target.Die.Result != DieResult.NG;

            MaterialStateService.UpsertInspection(target.Die.DieId, new DieInspectionRecord
            {
                InspectionType = "Side0",
                Result = ok0 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                Offset = new VisionOffset { IsValid = false },
                NgCodes = ok0 ? new List<string>() : new List<string> { "SIDE0_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot("Side0", target.PickerIndex, target.X, target.Y, target.T0, target.Z, new VisionOffset { IsValid = false })
                },
                Measurements = BuildSideMeasurements(side0Result, "Side0")
            });

            MaterialStateService.UpsertInspection(target.Die.DieId, new DieInspectionRecord
            {
                InspectionType = "Side90",
                Result = ok90 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                Offset = new VisionOffset { IsValid = false },
                NgCodes = ok90 ? new List<string>() : new List<string> { "SIDE90_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot("Side90", target.PickerIndex, target.X, target.Y, target.T90, target.Z, new VisionOffset { IsValid = false })
                },
                Measurements = BuildSideMeasurements(side90Result, "Side90")
            });

            MaterialStateService.ApplyDieInspectionResult(
                target.Die.DieId,
                ok ? DieResult.Good : DieResult.NG,
                ok ? "" : "SIDE_NG",
                "SideInspection");

            WriteLog("PickerBottomSideInspectionSequence",
                Name + " Side 결과 적용 완료. die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", ok0=" + ok0 +
                ", ok90=" + ok90 + " - Ok");
        }

        private static List<InspectionMeasurement> BuildSideMeasurements(SideVisionResult result, string prefix)
        {
            bool side1Ok = result != null && result.Side1Ok;
            bool side2Ok = result != null && result.Side2Ok;
            bool side3Ok = result != null && result.Side3Ok;
            bool side4Ok = result != null && result.Side4Ok;

            return new List<InspectionMeasurement>
            {
                BuildBooleanMeasurement(prefix + "Side1", side1Ok),
                BuildBooleanMeasurement(prefix + "Side2", side2Ok),
                BuildBooleanMeasurement(prefix + "Side3", side3Ok),
                BuildBooleanMeasurement(prefix + "Side4", side4Ok),
                BuildBooleanMeasurement(prefix + "InspectionResult", result != null && result.IsAllOk)
            };
        }

        private void QueuePendingT0Return(int pickerIndex, double target)
        {
            _pendingT0Returns.Add(new PendingT0Return
            {
                PickerIndex = pickerIndex,
                Target = target
            });
        }

        private void StartPendingT0ReturnCommandAsync(string description, CancellationToken ct)
        {
            for (int i = _pendingT0Returns.Count - 1; i >= 0; i--)
            {
                PendingT0Return pending = _pendingT0Returns[i];
                if (pending.MoveTask != null)
                    continue;

                if (IsPickerAxisInPosition(GetPickerTAxis(pending.PickerIndex), pending.Target))
                {
                    _pendingT0Returns.RemoveAt(i);
                    continue;
                }

                pending.MoveTask = MovePickerAxisCommandAsync(
                    GetPickerTAxis(pending.PickerIndex),
                    pending.Target,
                    "DieSideT0ReturnDeferred[" + pending.PickerIndex + "]");

                WriteLog("PickerBottomSideInspectionSequence",
                    Name + " " + description + " 명령 시작. pickerNo=" + ToPickerNo(pending.PickerIndex) + " - Ok");
            }
        }

        private async Task<int> CompletePendingT0ReturnAsync(CancellationToken ct)
        {
            StartPendingT0ReturnCommandAsync("Bottom/Side 통합 검사 완료 후 PickerT 0도 복귀", ct);

            for (int i = _pendingT0Returns.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                PendingT0Return pending = _pendingT0Returns[i];
                PickerAxis axis = GetPickerTAxis(pending.PickerIndex);
                if (pending.MoveTask != null)
                {
                    int commandResult = await pending.MoveTask.ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-T0-CMD", Name, "예약된 PickerT 0도 복귀 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }

                var waitResult = await WaitPickerAxisMoveDoneAsync(axis, pending.Target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail("PICKER-BOTTOM-SIDE-T0-WAIT", Name, "예약된 PickerT 0도 복귀 완료 대기 실패. " + FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, pending.Target)));

                if (!IsPickerAxisInPosition(axis, pending.Target))
                    return Fail("PICKER-BOTTOM-SIDE-T0-POS", Name, "예약된 PickerT 0도 복귀 최종 위치 확인 실패. " + BuildPickerAxisState(axis, pending.Target));

                _pendingT0Returns.RemoveAt(i);
            }

            return 0;
        }

        private int CountPendingBottomResults()
        {
            int count = 0;
            for (int i = 0; i < _pendingBottomShots.Count; i++)
            {
                if (!_pendingBottomShots[i].Applied)
                    count++;
            }
            return count;
        }

        private static bool HasInspectionResult(DieMaterial die, string inspectionType)
        {
            try
            {
                if (die == null || die.Inspections == null || string.IsNullOrWhiteSpace(inspectionType))
                    return false;

                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record == null)
                        continue;

                    if (!string.Equals(record.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return record.Result != MaterialInspectionResult.Unknown;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void ReleaseInspectionArea()
        {
            try
            {
                ReleasePickerWorkArea();

                if (_inspectionAreaLease == null)
                    return;

                _inspectionAreaLease.Dispose();
                _inspectionAreaLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomSideInspectionSequence", "InspectionArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
