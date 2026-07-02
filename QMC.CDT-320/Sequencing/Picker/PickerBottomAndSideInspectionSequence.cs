using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomAndSideInspectionSequence : PickerSequenceBase<PickerBottomAndSideInspectionStep>
    {
        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private readonly List<BottomShot> _pendingBottomShots = new List<BottomShot>();
        private readonly List<int> _sideReadyPickerIndexes = new List<int>();
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();
        private readonly List<PendingZAvoid> _pendingZAvoids = new List<PendingZAvoid>();
        private readonly List<PendingBottomZDown> _pendingBottomZDowns = new List<PendingBottomZDown>();

        private bool _bottomInspectionYReady;
        private bool _sideInspectionYReady;
        private bool _hasPreviousBottomX;
        private bool _hasLastBottomX;
        private bool _hasSidePitchStepX;
        private bool _sidePitchTargetReady;
        private double _previousBottomX;
        private double _lastBottomX;
        private double _sidePitchStepX;
        private double _lastSidePitchX;
        private SequenceResourceLease _inspectionAreaLease;
        private IDisposable _bottomProcessAreaScope;
        private IDisposable _sideProcessAreaScope;

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

        private sealed class PendingZAvoid
        {
            public int PickerIndex;
            public double Target;
            public Task<int> MoveTask;
        }

        private sealed class PendingBottomZDown
        {
            public int PickerIndex;
            public double Target;
            public Task<int> MoveTask;
        }

        public PickerBottomAndSideInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Inspect, side == PickerSequenceSide.Front ? "FrontPickerBottomAndSideInspectionSequence" : "RearPickerBottomAndSideInspectionSequence")
        {
            CurrentStep = PickerBottomAndSideInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerBottomAndSideInspectionStep.Complete; }
        }

        public bool ForceBottomInspectionBeforeSideResume { get; set; }

        public void Abort()
        {
            try
            {
                ReleaseInspectionArea();
                ReleaseBottomSideProcessArea();
                _pendingBottomShots.Clear();
                _sideReadyPickerIndexes.Clear();
                _pendingT0Returns.Clear();
                _pendingZAvoids.Clear();
                _pendingBottomZDowns.Clear();
                ForceBottomInspectionBeforeSideResume = false;
                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
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

                CurrentStep = PickerBottomAndSideInspectionStep.CheckUnit;
                int result = CheckUnit();
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.BuildPickedPickerList;
                result = BuildPickedPickerList();
                if (result != 0 || CurrentStep == PickerBottomAndSideInspectionStep.Complete)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.AcquireInspectionArea;
                result = await AcquireInspectionAreaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.MoveOppositePickerToAvoidBeforeInspection;
                result = await MoveOppositePickerToAvoidAndVerifyAsync(
                    "Bottom/Side 통합 검사 진입 전 상대 Picker Avoid 확인",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (ForceBottomInspectionBeforeSideResume)
                {
                    result = await MoveOwnPickerYToAvoidBeforeForcedBottomResumeAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                CurrentStep = PickerBottomAndSideInspectionStep.RunBottomPipeline;
                result = await RunBottomPipelineAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.RunSidePipeline;
                result = await RunSidePipelineAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.MoveFinalZToAvoid;
                result = await CompletePendingZAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveAllPickerZToAvoidAndVerifyAsync("Bottom/Side 통합 검사 완료 후 PickerZ 전체 Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomAndSideInspectionStep.CompletePendingT0Return;
                result = await CompletePendingT0ReturnAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom/Side 통합 검사 완료 후 PickerY/X Avoid 복귀를 생략합니다. " +
                    "Place 공정이 Side 검사 종료 위치에서 직접 이어서 이동합니다. side=" + Side + " - Ok");

                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
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
                if (CurrentStep == PickerBottomAndSideInspectionStep.Complete)
                {
                    ReleaseInspectionArea();
                    ReleaseBottomSideProcessArea();
                }
            }
        }

        private int CheckUnit()
        {
            if (!IsPickerSideEnabled())
            {
                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
                WriteLog("PickerBottomAndSideInspectionSequence", Name + " Picker 사용 설정이 꺼져 있어 Bottom/Side 통합 검사를 완료 처리합니다. side=" + Side + " - Check");
                return 0;
            }

            CurrentStep = PickerBottomAndSideInspectionStep.BuildPickedPickerList;
            return 0;
        }

        private int BuildPickedPickerList()
        {
            _pickedPickerIndexes.Clear();
            _pendingBottomShots.Clear();
            _sideReadyPickerIndexes.Clear();
            _pendingT0Returns.Clear();
            _pendingZAvoids.Clear();
            _pendingBottomZDowns.Clear();
            _bottomInspectionYReady = false;
            _sideInspectionYReady = false;
            _hasPreviousBottomX = false;
            _hasLastBottomX = false;
            _hasSidePitchStepX = false;
            _sidePitchTargetReady = false;
            _previousBottomX = 0.0;
            _lastBottomX = 0.0;
            _sidePitchStepX = 0.0;
            _lastSidePitchX = 0.0;

            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerBottomAndSideInspectionSequence"));
            if (_pickedPickerIndexes.Count == 0)
            {
                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
                return 0;
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom/Side 통합 검사 대상 구성 완료. count=" + _pickedPickerIndexes.Count + " - Ok");
            CurrentStep = PickerBottomAndSideInspectionStep.AcquireInspectionArea;
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
                        Name + ":RearSide",
                        ct).ConfigureAwait(false);
                    if (_inspectionAreaLease == null)
                        return -1;
                }

                EnsureBottomSideProcessAreaReserved("BottomAndSideInspection");

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
            EnsureBottomSideProcessAreaReserved("BottomAndSideInspection:Bottom");

            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                InspectionTarget target = BuildBottomTarget(_pickedPickerIndexes[i]);
                if (target == null || target.Die == null)
                    continue;

                if (HasInspectionResult(target.Die, "Bottom") && !ForceBottomInspectionBeforeSideResume)
                {
                    if (!_sideReadyPickerIndexes.Contains(target.PickerIndex))
                        _sideReadyPickerIndexes.Add(target.PickerIndex);

                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 기존 Bottom 검사 결과가 있어 Bottom shot을 생략하고 SideReady로 등록합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                    continue;
                }

                if (HasInspectionResult(target.Die, "Bottom") && ForceBottomInspectionBeforeSideResume)
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Side 단독 재개 방지 모드라 기존 Bottom 결과가 있어도 Bottom shot을 다시 진행합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                }

                int result = await CompletePendingBottomZDownForPickerAsync(target.PickerIndex, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveBottomTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                UpdateBottomPitchReference(target);
                StartNextBottomZDownCommand(i + 1);

                bool isLastBottomShot = i >= _pickedPickerIndexes.Count - 1;
                Task<int> sideFirstTask = null;
                Task<int> bottomTriggerTask = TriggerBottomInspectionAsync(target, ct);
                if (isLastBottomShot)
                    sideFirstTask = StartFirstReadySideInspectionDuringLastBottomAsync(ct);

                result = await bottomTriggerTask.ConfigureAwait(false);
                if (result != 0)
                    return result;

                _pendingBottomShots.Add(new BottomShot { Target = target });

                if (sideFirstTask != null)
                {
                    result = await sideFirstTask.ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                // Vision 결과 대기 시간을 뒤로 밀기 위해 최소 3번째 shot 이후부터 앞쪽 결과를 회수한다.
                // 마지막 Bottom shot 이후에는 Side #1이 즉시 진입해야 하므로 추가 결과 회수로 시간을 쓰지 않는다.
                if (!isLastBottomShot && _pendingBottomShots.Count >= 3)
                {
                    result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
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
                T0 = ResolvePickerZoneT("DieBottomPosition", pickerIndex)
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

            PickerAxis zAxis = GetPickerZAxis(target.PickerIndex);
            if (!IsPickerAxisInPosition(zAxis, target.Z))
            {
                result = await MovePickerAxisAndVerifyAsync(
                    zAxis,
                    target.Z,
                    "Bottom/Side 통합 Bottom Z",
                    ct,
                    BuildBottomTargetName(target)).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

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

        private async Task<int> MoveOwnPickerYToAvoidBeforeForcedBottomResumeAsync(CancellationToken ct)
        {
            try
            {
                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisInPosition(PickerAxis.PickerY, yAvoid))
                    return 0;

                int result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    yAvoid,
                    "Bottom/Side 재시작 전 PickerY Avoid",
                    ct,
                    "AvoidPosition;PickerProcess=BottomSide;PickerPhase=ForcedBottomResumeSafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                _bottomInspectionYReady = false;
                _sideInspectionYReady = false;
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom/Side 재시작 안전 진입: Bottom X 이동 전에 PickerY를 Avoid로 정리했습니다. " +
                    "side=" + Side + " - Ok");
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
                return Fail("PICKER-BOTTOM-SIDE-RESUME-Y-AVOID-EX", Name,
                    "Bottom/Side 재시작 전 PickerY Avoid 이동 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private string BuildBottomTargetName(InspectionTarget target)
        {
            return AppendAutoProcessCorrectionTargetTag("DieBottomPosition[" + target.PickerIndex + "];PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Input;To=Bottom");
        }

        private async Task<int> TriggerBottomInspectionAsync(InspectionTarget target, CancellationToken ct)
        {
            await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

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

            WriteLog("PickerBottomAndSideInspectionSequence",
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

            WriteLog("PickerBottomAndSideInspectionSequence",
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
            EnsureBottomSideProcessAreaReserved("BottomAndSideInspection:Side");

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
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 기존 Side 검사 결과가 있어 Side shot을 생략합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                    continue;
                }

                ApplySidePitchTargetIfReady(target);

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

            double t0 = ResolvePickerZoneT("DieSidePosition", pickerIndex);
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

        private async Task<int> StartFirstReadySideInspectionDuringLastBottomAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                {
                    int pickerIndex = _pickedPickerIndexes[i];
                    if (!_sideReadyPickerIndexes.Contains(pickerIndex))
                        continue;

                    InspectionTarget target = BuildSideTarget(pickerIndex);
                    if (target == null || target.Die == null)
                        continue;

                    if (HasInspectionResult(target.Die, "Side0") &&
                        HasInspectionResult(target.Die, "Side90"))
                    {
                        WriteLog("PickerBottomAndSideInspectionSequence",
                            Name + " Bottom 마지막 검사 중 Side 병렬 시작 대상이 이미 검사 완료 상태입니다. " +
                            "die=" + target.Die.DieId +
                            ", pickerNo=" + target.PickerNo + " - Check");
                        return 0;
                    }

                    ApplySidePitchTargetIfReady(target);

                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom 마지막 검사와 Side 첫 검사를 병렬 시작합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo +
                        ", sideReadyCount=" + _sideReadyPickerIndexes.Count + " - Start");

                    return await InspectSideTargetAsync(target, ct).ConfigureAwait(false);
                }

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom 마지막 검사 중 병렬 시작 가능한 SideReady 대상이 없습니다. - Check");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BOTTOM-SIDE-PARALLEL-SIDE1-EX", Name,
                    "Bottom 마지막 검사 중 Side 첫 검사 병렬 시작 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
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

            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);

            result = await MoveSideT90AndVision90PositionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            SideVisionResult side90Result = await TriggerAndGetSideResultAsync(target, 90, ct).ConfigureAwait(false);
            if (side90Result == null)
                return Fail("PICKER-BOTTOM-SIDE-SIDE90-RESULT", "Vision", "Side 90도 검사 결과 수신 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);

            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);

            ApplySideInspectionResult(target, side0Result, side90Result);
            QueuePendingZAvoid(target.PickerIndex);
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

            Task<int> previousZTask = StartAndCompletePendingZAvoidAsync(
                "다음 Side 검사 진입 중 이전 PickerZ Avoid",
                ct);

            int[] results = await Task.WhenAll(pickerTask, visionTask, previousZTask).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0 || results[2] != 0)
            {
                return Fail("PICKER-BOTTOM-SIDE-SIDE-X-VISION0", Name,
                    "Side 0도 진입 X/Y와 SideVisionY 0도 병렬 이동 실패. pickerResult=" + results[0] +
                    ", visionResult=" + results[1] +
                    ", previousZResult=" + results[2] +
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
            await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

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
            return AppendAutoProcessCorrectionTargetTag("DieSidePosition[" + target.PickerIndex + "];PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Side");
        }

        private void EnsureBottomSideProcessAreaReserved(string description)
        {
            try
            {
                bool isFront = Side == PickerSequenceSide.Front;
                if (_bottomProcessAreaScope == null)
                {
                    _bottomProcessAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                        isFront,
                        PickerWorkZone.Bottom,
                        Name + ":" + description + ":ProcessBottom");
                    WriteLog("PickerWorkArea",
                        Name + " Bottom/Side 통합 검사 Process 영역 Bottom 점유. side=" + Side +
                        ", description=" + description + " - Ok");
                }

                if (_sideProcessAreaScope == null)
                {
                    _sideProcessAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                        isFront,
                        PickerWorkZone.Side,
                        Name + ":" + description + ":ProcessSide");
                    WriteLog("PickerWorkArea",
                        Name + " Bottom/Side 통합 검사 Process 영역 Side 점유. side=" + Side +
                        ", description=" + description + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " Bottom/Side 통합 검사 Process 영역 점유 실패. side=" + Side +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseBottomSideProcessArea()
        {
            try
            {
                if (_sideProcessAreaScope != null)
                {
                    _sideProcessAreaScope.Dispose();
                    _sideProcessAreaScope = null;
                    WriteLog("PickerWorkArea",
                        Name + " Bottom/Side 통합 검사 Process 영역 Side 점유 해제. side=" + Side + " - Ok");
                }

                if (_bottomProcessAreaScope != null)
                {
                    _bottomProcessAreaScope.Dispose();
                    _bottomProcessAreaScope = null;
                    WriteLog("PickerWorkArea",
                        Name + " Bottom/Side 통합 검사 Process 영역 Bottom 점유 해제. side=" + Side + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerWorkArea",
                    Name + " Bottom/Side 통합 검사 Process 영역 해제 실패. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void UpdateBottomPitchReference(InspectionTarget target)
        {
            if (target == null)
                return;

            if (_hasPreviousBottomX)
            {
                double rawStep = target.X - _previousBottomX;
                if (Math.Abs(rawStep) > 0.000001)
                {
                    double pitch = ResolvePickerPitchXMagnitude();
                    _sidePitchStepX = (pitch > 0.000001 ? pitch : Math.Abs(rawStep)) * Math.Sign(rawStep);
                    _hasSidePitchStepX = true;
                }
            }

            _previousBottomX = target.X;
            _lastBottomX = target.X;
            _hasPreviousBottomX = true;
            _hasLastBottomX = true;
        }

        private void ApplySidePitchTargetIfReady(InspectionTarget target)
        {
            if (target == null || !_hasLastBottomX)
                return;

            double pitchTargetX;
            if (!_sidePitchTargetReady)
            {
                pitchTargetX = _lastBottomX;
                _sidePitchTargetReady = true;
            }
            else
            {
                if (!_hasSidePitchStepX)
                    return;

                pitchTargetX = _lastSidePitchX + _sidePitchStepX;
            }

            double absoluteX = target.X;
            target.X = pitchTargetX;
            _lastSidePitchX = pitchTargetX;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side X 목표를 절대 포지션 대신 Bottom 진행 피치 기준으로 계산합니다. " +
                "pickerNo=" + target.PickerNo +
                ", absoluteX=" + absoluteX.ToString("0.###") +
                ", pitchTargetX=" + pitchTargetX.ToString("0.###") +
                ", pitchStep=" + _sidePitchStepX.ToString("0.###") + " - Check");
        }

        private double ResolvePickerPitchXMagnitude()
        {
            try
            {
                double pitch = 0.0;
                if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Setup != null)
                    pitch = FrontPicker.Setup.PickerPitchX;
                else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Setup != null)
                    pitch = RearPicker.Setup.PickerPitchX;

                return Math.Abs(pitch);
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private void StartNextBottomZDownCommand(int nextCursor)
        {
            try
            {
                if (nextCursor < 0 || nextCursor >= _pickedPickerIndexes.Count)
                    return;

                int nextPickerIndex = _pickedPickerIndexes[nextCursor];
                InspectionTarget nextTarget = BuildBottomTarget(nextPickerIndex);
                if (nextTarget == null || nextTarget.Die == null)
                    return;

                QueuePendingBottomZDown(nextPickerIndex, nextTarget.Z);
                StartPendingBottomZDownCommand(
                    "Bottom 검사 중 다음 PickerZ 선행 하강",
                    nextPickerIndex);
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom 검사 중 다음 PickerZ 선행 하강 준비 예외. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void QueuePendingBottomZDown(int pickerIndex, double target)
        {
            for (int i = 0; i < _pendingBottomZDowns.Count; i++)
            {
                if (_pendingBottomZDowns[i].PickerIndex == pickerIndex)
                    return;
            }

            _pendingBottomZDowns.Add(new PendingBottomZDown
            {
                PickerIndex = pickerIndex,
                Target = target
            });
        }

        private void StartPendingBottomZDownCommand(string description, int pickerIndex)
        {
            for (int i = _pendingBottomZDowns.Count - 1; i >= 0; i--)
            {
                PendingBottomZDown pending = _pendingBottomZDowns[i];
                if (pending.PickerIndex != pickerIndex || pending.MoveTask != null)
                    continue;

                PickerAxis axis = GetPickerZAxis(pending.PickerIndex);
                if (IsPickerAxisInPosition(axis, pending.Target))
                {
                    _pendingBottomZDowns.RemoveAt(i);
                    continue;
                }

                pending.MoveTask = MovePickerAxisCommandAsync(
                    axis,
                    pending.Target,
                    "DieBottomZPreDown[" + pending.PickerIndex + "];PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom");

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " " + description + " 명령 시작. pickerNo=" + ToPickerNo(pending.PickerIndex) +
                    ", target=" + pending.Target.ToString("0.###") + " - Ok");
            }
        }

        private async Task<int> CompletePendingBottomZDownForPickerAsync(int pickerIndex, CancellationToken ct)
        {
            for (int i = _pendingBottomZDowns.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                PendingBottomZDown pending = _pendingBottomZDowns[i];
                if (pending.PickerIndex != pickerIndex)
                    continue;

                PickerAxis axis = GetPickerZAxis(pending.PickerIndex);
                if (pending.MoveTask != null)
                {
                    int commandResult = await pending.MoveTask.ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-CMD", Name, "예약된 Bottom PickerZ 선행 하강 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }
                else if (!IsPickerAxisInPosition(axis, pending.Target))
                {
                    int commandResult = await MovePickerAxisCommandAsync(
                        axis,
                        pending.Target,
                        "DieBottomZPreDown[" + pending.PickerIndex + "];PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom").ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-CMD", Name, "예약된 Bottom PickerZ 선행 하강 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }

                var waitResult = await WaitPickerAxisMoveDoneAsync(axis, pending.Target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-WAIT", Name, "예약된 Bottom PickerZ 선행 하강 완료 대기 실패. " + FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, pending.Target)));

                if (!IsPickerAxisInPosition(axis, pending.Target))
                    return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-POS", Name, "예약된 Bottom PickerZ 선행 하강 최종 위치 확인 실패. " + BuildPickerAxisState(axis, pending.Target));

                _pendingBottomZDowns.RemoveAt(i);
            }

            return 0;
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

            WriteLog("PickerBottomAndSideInspectionSequence",
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

        private void QueuePendingZAvoid(int pickerIndex)
        {
            for (int i = 0; i < _pendingZAvoids.Count; i++)
            {
                if (_pendingZAvoids[i].PickerIndex == pickerIndex)
                    return;
            }

            _pendingZAvoids.Add(new PendingZAvoid
            {
                PickerIndex = pickerIndex,
                Target = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "AvoidPosition")
            });
        }

        private async Task<int> StartAndCompletePendingZAvoidAsync(string description, CancellationToken ct)
        {
            StartPendingZAvoidCommandAsync(description);
            return await CompletePendingZAvoidAsync(ct).ConfigureAwait(false);
        }

        private void StartPendingZAvoidCommandAsync(string description)
        {
            for (int i = _pendingZAvoids.Count - 1; i >= 0; i--)
            {
                PendingZAvoid pending = _pendingZAvoids[i];
                if (pending.MoveTask != null)
                    continue;

                if (IsPickerAxisInPosition(GetPickerZAxis(pending.PickerIndex), pending.Target))
                {
                    _pendingZAvoids.RemoveAt(i);
                    continue;
                }

                pending.MoveTask = MovePickerAxisCommandAsync(
                    GetPickerZAxis(pending.PickerIndex),
                    pending.Target,
                    "DieSideZAvoidDeferred[" + pending.PickerIndex + "]");

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " " + description + " 명령 시작. pickerNo=" + ToPickerNo(pending.PickerIndex) + " - Ok");
            }
        }

        private async Task<int> CompletePendingZAvoidAsync(CancellationToken ct)
        {
            for (int i = _pendingZAvoids.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                PendingZAvoid pending = _pendingZAvoids[i];
                PickerAxis axis = GetPickerZAxis(pending.PickerIndex);
                if (pending.MoveTask != null)
                {
                    int commandResult = await pending.MoveTask.ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-CMD", Name, "예약된 PickerZ Avoid 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }
                else if (!IsPickerAxisInPosition(axis, pending.Target))
                {
                    int commandResult = await MovePickerAxisCommandAsync(
                        axis,
                        pending.Target,
                        "DieSideZAvoidDeferred[" + pending.PickerIndex + "]").ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-CMD", Name, "예약된 PickerZ Avoid 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }

                var waitResult = await WaitPickerAxisMoveDoneAsync(axis, pending.Target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-WAIT", Name, "예약된 PickerZ Avoid 완료 대기 실패. " + FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, pending.Target)));

                if (!IsPickerAxisInPosition(axis, pending.Target))
                    return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-POS", Name, "예약된 PickerZ Avoid 최종 위치 확인 실패. " + BuildPickerAxisState(axis, pending.Target));

                _pendingZAvoids.RemoveAt(i);
            }

            return 0;
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

                WriteLog("PickerBottomAndSideInspectionSequence",
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
                WriteLog("PickerBottomAndSideInspectionSequence", "InspectionArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
