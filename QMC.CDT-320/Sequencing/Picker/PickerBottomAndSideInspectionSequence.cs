using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.Diagnostics.TactTime;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomAndSideInspectionSequence : PickerSequenceBase<PickerBottomAndSideInspectionStep>
    {
        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private readonly List<BottomShot> _pendingBottomShots = new List<BottomShot>();
        private readonly List<int> _sideReadyPickerIndexes = new List<int>();
        private readonly List<int> _sideCompletedPickerIndexes = new List<int>();
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();
        private readonly List<PendingZAvoid> _pendingZAvoids = new List<PendingZAvoid>();
        private readonly List<PendingBottomZDown> _pendingBottomZDowns = new List<PendingBottomZDown>();
        private readonly Dictionary<int, BottomReferencePosition> _bottomReferencePositions = new Dictionary<int, BottomReferencePosition>();

        private bool _bottomInspectionYReady;
        private bool _sideInspectionYReady;
        private bool _sidePipelineEnabled;
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
            public bool SideCorrectionValid;
            public double SideVisionYOffset;
            public double SidePickerZBase;
            public double SidePickerZOffset;
            public double SideVisionProcess0BaseY;
            public double SideVisionProcess90BaseY;
            public double SideVisionProcess0Y;
            public double SideVisionProcess90Y;
            public string SideCorrectionSourceDieId;
        }

        private sealed class BottomShot
        {
            public InspectionTarget Target;
            public bool Applied;
            public DateTime InspectStartedAt;
        }

        private sealed class BottomReferencePosition
        {
            public double X;
            public double Y;
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
                _sideCompletedPickerIndexes.Clear();
                _pendingT0Returns.Clear();
                _pendingZAvoids.Clear();
                _pendingBottomZDowns.Clear();
                _bottomReferencePositions.Clear();
                _sidePipelineEnabled = false;
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

                CurrentStep = PickerBottomAndSideInspectionStep.VerifyPickedPickerFlow;
                result = await VerifyPickedPickerFlowBeforeInspectionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ClearRuntimeSideInspectionCorrections();

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
            _sideCompletedPickerIndexes.Clear();
            _pendingT0Returns.Clear();
            _pendingZAvoids.Clear();
            _pendingBottomZDowns.Clear();
            _bottomReferencePositions.Clear();
            _bottomInspectionYReady = false;
            _sideInspectionYReady = false;
            _sidePipelineEnabled = false;

            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerBottomAndSideInspectionSequence"));
            RemoveSkippedPickerTargets();
            if (_pickedPickerIndexes.Count == 0)
            {
                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
                return 0;
            }

            if (_pickedPickerIndexes.Count == 4)
            {
                string orderReason;
                if (!IsBottomSideFullBatchOrderValid(out orderReason))
                {
                    return Fail("PICKER-BOTTOM-SIDE-RUN-ORDER", Name,
                        "Bottom/Side 통합 검사는 4->3->2->1 순서만 허용합니다. " + orderReason);
                }

                _sidePipelineEnabled = true;
            }
            else
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom/Side 통합 Side 검사를 생략합니다. " +
                    "4개 풀 배치가 아니면 Side 첫 검사가 잘못 선행될 수 있어 현재 배치는 Bottom만 진행합니다. " +
                    "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) +
                    ", count=" + _pickedPickerIndexes.Count + " - Check");
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom/Side 통합 검사 대상 구성 완료. count=" + _pickedPickerIndexes.Count +
                ", sidePipelineEnabled=" + _sidePipelineEnabled + " - Ok");
            CurrentStep = PickerBottomAndSideInspectionStep.AcquireInspectionArea;
            return 0;
        }

        private bool IsBottomSideFullBatchOrderValid(out string reason)
        {
            reason = string.Empty;
            if (_pickedPickerIndexes.Count != 4)
            {
                reason = "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".";
                return false;
            }

            int[] expected = new int[] { ToPickerIndex(4), ToPickerIndex(3), ToPickerIndex(2), ToPickerIndex(1) };
            for (int i = 0; i < expected.Length; i++)
            {
                if (_pickedPickerIndexes[i] == expected[i])
                    continue;

                reason = "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) +
                    ", expected=4->3->2->1.";
                return false;
            }

            return true;
        }

        private async Task<int> VerifyPickedPickerFlowBeforeInspectionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsBottomSideProductCheckBypassed())
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom/Side 시작 전 제품 흡착 Flow 확인은 Simulation/DryRun 조건으로 통과합니다. side=" + Side +
                        ", targetCount=" + _pickedPickerIndexes.Count + " - Bypass");
                    return 0;
                }

                for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    int pickerIndex = _pickedPickerIndexes[i];
                    int pickerNo = ToPickerNo(pickerIndex);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                        continue;

                    int timeoutMs = ResolvePickerIoTimeoutMs(pickerNo);
                    DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
                    bool flowOn = ReadPickerFlowState(pickerNo);
                    while (!flowOn && DateTime.Now <= deadline)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(1, ct).ConfigureAwait(false);
                        flowOn = ReadPickerFlowState(pickerNo);
                    }

                    if (!flowOn)
                    {
                        return Fail("PICKER-BOTTOM-SIDE-FLOW-NOT-DETECTED", Name,
                            "Bottom/Side 검사 시작 전 제품 흡착 Flow 확인 실패. " +
                            "데이터상 Picker에 Die가 있지만 실제 Flow 신호가 ON이 아닙니다. " +
                            "side=" + Side +
                            ", pickerNo=" + pickerNo +
                            ", die=" + die.DieId +
                            ", timeoutMs=" + timeoutMs +
                            ", expectedFlow=ON, actualFlow=OFF");
                    }

                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom/Side 시작 전 제품 흡착 Flow 확인 완료. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + die.DieId +
                        ", flow=ON - Ok");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BOTTOM-SIDE-FLOW-CHECK-EX", Name,
                    "Bottom/Side 검사 시작 전 제품 흡착 Flow 확인 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
        }

        private bool IsBottomSideProductCheckBypassed()
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

        private void RemoveSkippedPickerTargets()
        {
            for (int i = _pickedPickerIndexes.Count - 1; i >= 0; i--)
            {
                int pickerIndex = _pickedPickerIndexes[i];
                int pickerNo = ToPickerNo(pickerIndex);
                DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                if (die == null || die.IsInputTarget)
                    continue;

                _pickedPickerIndexes.RemoveAt(i);
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Skip die는 Bottom/Side 검사를 생략합니다. " +
                    "die=" + die.DieId +
                    ", pickerNo=" + pickerNo +
                    ", result=" + die.Result + " - Skip");
            }
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

                StoreBottomReferencePosition(target);

                if (HasInspectionResult(target.Die, "Bottom"))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 기존 Bottom 검사 결과가 있어도 현재 PickUp 배치는 Bottom shot을 다시 진행합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                }

                bool isLastBottomShot = i >= _pickedPickerIndexes.Count - 1;
                InspectionTarget sideFirstTarget = isLastBottomShot
                    ? PrepareAutoFirstSideTargetForLastBottom(target)
                    : null;

                int result = await CompletePendingBottomZDownForPickerAsync(target.PickerIndex, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveBottomTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 현재 기준: Z Down Mode 선행 Z 명령은 AutoFocus보다 먼저 수행되어야 하므로 위치를 바꾸지 않는다.
                StartNextBottomZDownCommand(i + 1);

                result = await RunAutoFocusBeforeBottomInspectionAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (sideFirstTarget != null)
                {
                    result = await PrepareSideTargetForInspectionAsync(sideFirstTarget, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                DateTime bottomInspectStartedAt = DateTime.Now;
                if (sideFirstTarget != null)
                {
                    int[] triggerResults = await StartBottomAndPreparedSide0InspectionAsync(target, sideFirstTarget, ct).ConfigureAwait(false);
                    result = triggerResults[0];
                    if (result != 0)
                    {
                        RecordDetailedTactRecord(
                            TactTimeCategory.Vision,
                            "Bottom Camera Inspect",
                            "Bottom",
                            target,
                            bottomInspectStartedAt,
                            TactTimeResult.Failed,
                            "PICKER-BOTTOM-SIDE-BOTTOM-TRIGGER",
                            "Bottom inspection trigger failed. result=" + result);
                        return result;
                    }

                    _pendingBottomShots.Add(new BottomShot { Target = target, InspectStartedAt = bottomInspectStartedAt });

                    result = triggerResults[1];
                    if (result != 0)
                        return result;

                    result = await CompletePreparedFirstSideInspectionDuringLastBottomAsync(sideFirstTarget, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
                else
                {
                    result = await StartBottomInspectionAsync(target, ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        RecordDetailedTactRecord(
                            TactTimeCategory.Vision,
                            "Bottom Camera Inspect",
                            "Bottom",
                            target,
                            bottomInspectStartedAt,
                            TactTimeResult.Failed,
                            "PICKER-BOTTOM-SIDE-BOTTOM-TRIGGER",
                            "Bottom inspection trigger failed. result=" + result);
                        return result;
                    }

                    _pendingBottomShots.Add(new BottomShot { Target = target, InspectStartedAt = bottomInspectStartedAt });
                }

                // Vision 결과 대기 시간을 뒤로 밀기 위해 최소 3번째 shot 이후부터 앞쪽 결과를 회수한다.
                // 마지막 Bottom shot 이후에는 Side #4가 즉시 진입해야 하므로 추가 결과 회수로 시간을 쓰지 않는다.
                if (!isLastBottomShot && _pendingBottomShots.Count >= 3)
                {
                    result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom shot 전체 완료. pendingResult=" + CountPendingBottomResults() + " - Ok");

            if (!_sidePipelineEnabled)
            {
                int result = await ApplyAllPendingBottomResultsAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> RunAutoFocusBeforeBottomInspectionAsync(InspectionTarget target, CancellationToken ct)
        {
            if (target == null)
                return 0;

            int result = await RunBottomRuntimeAutoFocusIfNeededAsync(
                target.PickerIndex,
                target.PickerNo,
                target.Die,
                target.Z,
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            target.Z = GetPickerTeachingPosition(GetPickerZAxis(target.PickerIndex), "BottomPosition");
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

        private void StoreBottomReferencePosition(InspectionTarget target)
        {
            if (target == null)
                return;

            _bottomReferencePositions[target.PickerIndex] = new BottomReferencePosition
            {
                X = target.X,
                Y = target.Y
            };

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 공정 X/Y를 Side 기준 위치로 저장합니다. " +
                "bottomPickerNo=" + target.PickerNo +
                ", x=" + target.X.ToString("0.###") +
                ", y=" + target.Y.ToString("0.###") + " - Check");
        }

        private bool TryResolveBottomReferenceForSideTarget(
            int sidePickerIndex,
            out double x,
            out double y,
            out int bottomReferencePickerNo,
            out bool stored)
        {
            x = 0.0;
            y = 0.0;
            stored = false;
            bottomReferencePickerNo = ResolveBottomReferencePickerNoForSide(sidePickerIndex);
            if (bottomReferencePickerNo < 1 || bottomReferencePickerNo > 4)
                return false;

            int bottomReferencePickerIndex = ToPickerIndex(bottomReferencePickerNo);
            BottomReferencePosition reference;
            if (_bottomReferencePositions.TryGetValue(bottomReferencePickerIndex, out reference))
            {
                x = reference.X;
                y = reference.Y;
                stored = true;
                return true;
            }

            if (!_sidePipelineEnabled)
                return false;

            // 현재 기준: Side는 Bottom 공정 X/Y만 사용한다. 런타임 저장값이 없으면 Bottom 티칭값으로만 대체한다.
            x = ResolvePickerZoneX("DieBottomPosition", bottomReferencePickerIndex);
            y = ResolvePickerZoneY("DieBottomPosition", bottomReferencePickerIndex);
            return true;
        }

        private int ResolveBottomReferencePickerNoForSide(int sidePickerIndex)
        {
            int sidePickerNo = ToPickerNo(sidePickerIndex);
            return 5 - sidePickerNo;
        }

        private async Task<int> MoveBottomTargetAsync(InspectionTarget target, CancellationToken ct)
        {
            using (TactTimeScope tactScope = BeginDetailedTactScope(
                TactTimeCategory.Motion,
                "Bottom Vision To Pitch Move",
                "Vision->Pitch",
                target,
                "Bottom pitch move start."))
            {
                try
                {
                    var targets = new Dictionary<PickerAxis, double>();
                    targets[PickerAxis.PickerX] = target.X;
                    bool pickerXAlreadyInBottomPosition = IsPickerAxisInPosition(PickerAxis.PickerX, target.X);
                    if (pickerXAlreadyInBottomPosition)
                    {
                        WriteLog("PickerBottomAndSideInspectionSequence",
                            Name + " Bottom X 이동 전에 PickerX가 이미 Bottom 목표 위치입니다. " +
                            "이 상태에서만 PickerY 전진을 허용합니다. " +
                            "die=" + (target.Die != null ? target.Die.DieId : "-") +
                            ", pickerNo=" + target.PickerNo +
                            ", targetX=" + target.X +
                            ", " + BuildPickerAxisState(PickerAxis.PickerX, target.X) +
                            " - Check");
                    }
                    else
                    {
                        WriteLog("PickerBottomAndSideInspectionSequence",
                            Name + " Bottom X 이동 후 PickerY 전진 순서로 진행합니다. " +
                            "die=" + (target.Die != null ? target.Die.DieId : "-") +
                            ", pickerNo=" + target.PickerNo +
                            ", targetX=" + target.X +
                            ", " + BuildPickerAxisState(PickerAxis.PickerX, target.X) +
                            " - Check");
                    }

                    if (!_bottomInspectionYReady || !IsPickerAxisInPosition(PickerAxis.PickerY, target.Y))
                        targets[PickerAxis.PickerY] = target.Y;

                    int result = await MovePickerXTThenYAndVerifyAsync(
                        targets,
                        "Bottom/Side 통합 Bottom X/Y",
                        ct,
                        BuildBottomTargetName(target)).ConfigureAwait(false);
                    if (result != 0)
                    {
                        tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-XY", BuildTactDetail(target, "Bottom pitch X/Y move failed. result=" + result));
                        return result;
                    }

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
                        {
                            tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-Y", BuildTactDetail(target, "Bottom pitch Y move failed. result=" + result));
                            return result;
                        }
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
                        {
                            tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-Z", BuildTactDetail(target, "Bottom pitch Z move failed. result=" + result));
                            return result;
                        }
                    }

                    result = await MovePickerAxisAndVerifyAsync(
                        GetPickerTAxis(target.PickerIndex),
                        target.T0,
                        "Bottom/Side 통합 Bottom T",
                        ct,
                        BuildBottomTargetName(target)).ConfigureAwait(false);
                    if (result != 0)
                    {
                        tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-T", BuildTactDetail(target, "Bottom pitch T move failed. result=" + result));
                        return result;
                    }

                    tactScope.Complete(BuildTactDetail(target, "Bottom pitch move completed."));
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    tactScope.Cancel(BuildTactDetail(target, "Bottom pitch move canceled."));
                    throw;
                }
                catch (SequenceStopException)
                {
                    tactScope.Stop("SEQUENCE-STOP", BuildTactDetail(target, "Bottom pitch move stopped."));
                    throw;
                }
                catch (Exception ex)
                {
                    tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-MOVE-EX", BuildTactDetail(target, "Bottom pitch move exception. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }
        }

        private TactTimeScope BeginDetailedTactScope(
            TactTimeCategory category,
            string processName,
            string stepName,
            InspectionTarget target,
            string detail)
        {
            TactTimeRecorder recorder = Context != null && Context.Tact != null
                ? Context.Tact
                : NullTactTimeRecorder.Instance;

            return recorder.BeginScope(
                category,
                Side == PickerSequenceSide.Front ? "FrontPicker" : "RearPicker",
                Name,
                processName,
                stepName,
                null,
                BuildTactDetail(target, detail));
        }

        private string BuildTactDetail(InspectionTarget target, string detail)
        {
            return "side=" + Side +
                   ", die=" + (target != null && target.Die != null ? target.Die.DieId : "-") +
                   ", pickerNo=" + (target != null ? target.PickerNo.ToString() : "-") +
                   ", " + (detail ?? "");
        }

        private string BuildPickerNoListText(List<int> pickerIndexes)
        {
            if (pickerIndexes == null || pickerIndexes.Count == 0)
                return "-";

            var pickerNos = new List<string>();
            for (int i = 0; i < pickerIndexes.Count; i++)
                pickerNos.Add(ToPickerNo(pickerIndexes[i]).ToString());

            return string.Join(",", pickerNos.ToArray());
        }

        private void RecordDetailedTactRecord(
            TactTimeCategory category,
            string processName,
            string stepName,
            InspectionTarget target,
            DateTime startedAt,
            TactTimeResult result,
            string alarmCode,
            string detail)
        {
            try
            {
                TactTimeRecorder recorder = Context != null && Context.Tact != null
                    ? Context.Tact
                    : NullTactTimeRecorder.Instance;

                DateTime endAt = DateTime.Now;
                if (startedAt == DateTime.MinValue)
                    startedAt = endAt;

                recorder.Record(new TactTimeRecord
                {
                    UnitName = Side == PickerSequenceSide.Front ? "FrontPicker" : "RearPicker",
                    SequenceName = Name,
                    ProcessName = processName ?? "",
                    StepName = stepName ?? "",
                    Category = category,
                    StartedAt = startedAt,
                    EndedAt = endAt,
                    ElapsedMs = Math.Max(0, (long)(endAt - startedAt).TotalMilliseconds),
                    Result = result,
                    AlarmCode = alarmCode ?? "",
                    Detail = BuildTactDetail(target, detail)
                });
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void RecordInspectionCheckpointForTact(
            string key,
            string processName,
            string stepName,
            InspectionTarget target,
            string detail)
        {
            try
            {
                if (Context == null || Context.Controller == null)
                    return;

                Context.Controller.RecordInspectionCheckpointForTact(
                    key,
                    processName,
                    stepName,
                    Side.ToString(),
                    target != null && target.Die != null ? target.Die.DieId : "",
                    target != null ? target.PickerNo : 0,
                    detail);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task<int> MoveOwnPickerYToAvoidBeforeForcedBottomResumeAsync(CancellationToken ct)
        {
            try
            {
                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisInPosition(PickerAxis.PickerY, yAvoid))
                    return 0;

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Bottom/Side 재시작 전 PickerY Avoid 이동 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
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
            return AppendAutoProcessCorrectionTargetTag(BuildPickerTargetName("DieBottomPosition", target.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Input;To=Bottom");
        }

        private async Task<int> StartBottomInspectionAsync(InspectionTarget target, CancellationToken ct, bool skipDelay = false)
        {
            if (!skipDelay)
                await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            RegisterVisionDieAddress(target);   // 신형 와이어(die_index/gridx;gridy) 구성용 — 어댑터가 조회

            int timeoutMs = ResolveVisionInspectionTimeout();
            if (IsDryRunMode())
            {
                await TriggerDryRunBottomGrabIfConnectedAsync(target, timeoutMs, ct).ConfigureAwait(false);
                return 0;
            }

            bool started = Side == PickerSequenceSide.Front
                ? await FrontPicker.StartBottomInspectionAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.StartBottomInspectionAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (!started)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-TRIGGER", "Vision",
                    "Bottom 검사 시작 ACK 수신 실패. die=" + target.Die.DieId +
                    ", pickerNo=" + target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 검사 시작 ACK 수신 완료. die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", pendingResult=" + (_pendingBottomShots.Count + 1) + " - Ok");
            return 0;
        }

        private async Task<int[]> StartBottomAndPreparedSide0InspectionAsync(InspectionTarget bottomTarget, InspectionTarget sideTarget, CancellationToken ct)
        {
            await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom #1 / Side #4 검사 시작 명령을 같은 대기 조건에서 연속 발행합니다. " +
                "bottomPickerNo=" + (bottomTarget != null ? bottomTarget.PickerNo : 0) +
                ", sidePickerNo=" + (sideTarget != null ? sideTarget.PickerNo : 0) +
                ", sharedX=" + (sideTarget != null ? sideTarget.X.ToString("0.###") : "-") +
                ", sharedY=" + (sideTarget != null ? sideTarget.Y.ToString("0.###") : "-") + " - Start");

            Task<int> side0TriggerTask = StartSide0InspectionAsync(sideTarget, ct, true);
            Task<int> bottomTriggerTask = StartBottomInspectionAsync(bottomTarget, ct, true);
            int[] results = await Task.WhenAll(bottomTriggerTask, side0TriggerTask).ConfigureAwait(false);
            return results;
        }

        private async Task TriggerDryRunBottomGrabIfConnectedAsync(InspectionTarget target, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel.BottomInspection))
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " DryRun Bottom GRAB skipped. BottomInspection Vision is not connected. die=" +
                    (target != null && target.Die != null ? target.Die.DieId : "") +
                    ", pickerNo=" + (target != null ? target.PickerNo : 0) + " - Check");
                return;
            }

            bool grabbed = Side == PickerSequenceSide.Front
                ? await FrontPicker.TriggerBottomInspectionExposeAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.TriggerBottomInspectionExposeAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " DryRun Bottom GRAB " + (grabbed ? "completed" : "failed") +
                ". die=" + (target != null && target.Die != null ? target.Die.DieId : "") +
                ", pickerNo=" + (target != null ? target.PickerNo : 0) +
                ", timeoutMs=" + timeoutMs + " - Check");
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

        private async Task<int> ApplyAllPendingBottomResultsAsync(CancellationToken ct)
        {
            while (CountPendingBottomResults() > 0)
            {
                ct.ThrowIfCancellationRequested();

                int result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
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

            int timeoutMs = ResolveVisionInspectionTimeout();
            // 현재 기준: Bottom 결과는 반드시 받아야 하며, 미수신 시 timeout 후 알람 정지한다.
            BottomVisionOffset result = Side == PickerSequenceSide.Front
                ? await FrontPicker.WaitBottomInspectionResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitBottomInspectionResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (result == null)
            {
                MarkInspectionResultReceiveFailureAsNg(
                    shot.Target,
                    "Bottom",
                    "BOTTOM_RESULT_MISSING",
                    "Bottom 검사 결과 ACK/RESULT 미수신");

                RecordDetailedTactRecord(
                    TactTimeCategory.Vision,
                    "Bottom Camera Inspect",
                    "Bottom",
                    shot.Target,
                    shot.InspectStartedAt,
                    TactTimeResult.Failed,
                    "PICKER-BOTTOM-SIDE-BOTTOM-RESULT",
                    "Bottom inspection result receive failed. timeoutMs=" + timeoutMs);

                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-RESULT", "Vision",
                    "Bottom 검사 결과 수신 실패. die=" + shot.Target.Die.DieId +
                    ", pickerNo=" + shot.Target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            ApplyBottomInspectionResult(shot.Target, result);
            shot.Applied = true;

            if (!_sideReadyPickerIndexes.Contains(shot.Target.PickerIndex))
                _sideReadyPickerIndexes.Add(shot.Target.PickerIndex);

            RecordDetailedTactRecord(
                TactTimeCategory.Vision,
                "Bottom Camera Inspect",
                "Bottom",
                shot.Target,
                shot.InspectStartedAt,
                TactTimeResult.Ok,
                "",
                "Bottom inspection completed. ok=" + result.IsOk);
            RecordInspectionCheckpointForTact(
                "BottomCameraInspection",
                "Bottom Camera Inspect Interval",
                "Bottom",
                shot.Target,
                "ok=" + result.IsOk);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 결과 적용 및 SideReady 등록 완료. die=" + shot.Target.Die.DieId +
                ", pickerNo=" + shot.Target.PickerNo +
                ", sideReadyCount=" + _sideReadyPickerIndexes.Count + " - Ok");
            return 0;
        }

        private void ApplyBottomInspectionResult(InspectionTarget target, BottomVisionOffset result)
        {
            MaterialInspectionResult inspectionResult = result.IsOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;
            DieResult dieResult = result.IsOk ? DieResult.Good : DieResult.NG;
            var measurements = new List<InspectionMeasurement>
            {
                BuildMeasurement("BottomAlignOffsetX", result.OffsetX, "mm", inspectionResult),
                BuildMeasurement("BottomAlignOffsetY", result.OffsetY, "mm", inspectionResult),
                BuildMeasurement("BottomAlignOffsetT", result.OffsetT, "deg", inspectionResult),
                BuildMeasurement("SideVisionYOffset", result.SideVisionYOffset, "mm", inspectionResult),
                BuildMeasurement("SidePickerZOffset", result.PickerZOffset, "mm", inspectionResult),
                BuildBooleanMeasurement("BottomInspectionResult", result.IsOk)
            };
            AppendVisionRawMeasurements(measurements, result, "Bottom", inspectionResult);

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
                Measurements = measurements
            });

            MaterialStateService.ApplyDieInspectionResult(
                target.Die.DieId,
                dieResult,
                result.IsOk ? "" : "BOTTOM_NG",
                "BottomInspection");

            StoreRuntimeSideInspectionCorrection(target, result);
        }

        private void ClearRuntimeSideInspectionCorrections()
        {
            if (Side == PickerSequenceSide.Front)
            {
                if (FrontPicker != null)
                    FrontPicker.ClearRuntimeSideInspectionCorrections();
            }
            else
            {
                if (RearPicker != null)
                    RearPicker.ClearRuntimeSideInspectionCorrections();
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 검사 런타임 보정값 초기화. side=" + Side + " - Ok");
        }

        private PickerSideInspectionCorrection ResolveRuntimeSideInspectionCorrection(int pickerIndex)
        {
            if (Side == PickerSequenceSide.Front)
                return FrontPicker != null ? FrontPicker.GetRuntimeSideInspectionCorrection(pickerIndex) : null;

            return RearPicker != null ? RearPicker.GetRuntimeSideInspectionCorrection(pickerIndex) : null;
        }

        private void StoreRuntimeSideInspectionCorrection(InspectionTarget target, BottomVisionOffset result)
        {
            if (target == null || result == null)
                return;

            bool valid = result.IsOk && result.HasSideInspectionCorrection;
            double sideVisionYOffset = valid ? result.SideVisionYOffset : 0.0;
            double pickerZOffset = valid ? result.PickerZOffset : 0.0;
            string sourceDieId = target.Die != null ? target.Die.DieId : string.Empty;

            if (Side == PickerSequenceSide.Front)
            {
                if (FrontPicker != null)
                    FrontPicker.SetRuntimeSideInspectionCorrection(target.PickerIndex, sideVisionYOffset, pickerZOffset, valid, sourceDieId);
            }
            else
            {
                if (RearPicker != null)
                    RearPicker.SetRuntimeSideInspectionCorrection(target.PickerIndex, sideVisionYOffset, pickerZOffset, valid, sourceDieId);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 결과 기반 Side 검사 보정 저장. " +
                "side=" + Side +
                ", pickerNo=" + target.PickerNo +
                ", die=" + sourceDieId +
                ", valid=" + valid +
                ", SideVisionY.offset=" + sideVisionYOffset.ToString("F6") +
                ", PickerZ.offset=" + pickerZOffset.ToString("F6") +
                ", bottomOffsetX=" + result.OffsetX.ToString("F6") +
                ", bottomOffsetY=" + result.OffsetY.ToString("F6") +
                ", bottomOffsetT=" + result.OffsetT.ToString("F6") + " - Ok");
        }

        private async Task<int> RunSidePipelineAsync(CancellationToken ct)
        {
            if (!_sidePipelineEnabled)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side Pipeline 생략. 4개 풀 배치가 아니므로 Bottom 검사 결과만 반영하고 Side 검사는 수행하지 않습니다. " +
                    "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + " - Check");
                return 0;
            }

            EnsureBottomSideProcessAreaReserved("BottomAndSideInspection:Side");

            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                int pickerIndex = _pickedPickerIndexes[i];
                if (_sideCompletedPickerIndexes.Contains(pickerIndex))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom 마지막 검사 중 병렬 완료된 Side 대상은 재검사를 생략합니다. " +
                        "pickerNo=" + ToPickerNo(pickerIndex) + " - Check");
                    continue;
                }

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
                        Name + " 기존 Side 검사 결과가 있어도 현재 PickUp 배치는 Side shot을 다시 진행합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                }

                result = await InspectSideTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!_sideCompletedPickerIndexes.Contains(pickerIndex))
                    _sideCompletedPickerIndexes.Add(pickerIndex);
            }

            return 0;
        }

        private InspectionTarget BuildSideTarget(int pickerIndex)
        {
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
                return null;

            PickerSideInspectionCorrection correction = ResolveRuntimeSideInspectionCorrection(pickerIndex);
            double baseZ = GetPickerTeachingPosition(GetPickerZAxis(pickerIndex), "SidePosition");
            double zOffset = correction != null && correction.IsValid ? correction.PickerZOffset : 0.0;
            double sideYOffset = correction != null && correction.IsValid ? correction.SideVisionYOffset : 0.0;
            double process0BaseY = ResolveSideVisionBasePosition(0);
            double process90BaseY = ResolveSideVisionBasePosition(90);
            double t0 = ResolvePickerZoneT("DieSidePosition", pickerIndex);
            double sideTeachingX = ResolvePickerZoneX("DieSidePosition", pickerIndex);
            double sideTeachingY = ResolvePickerZoneY("DieSidePosition", pickerIndex);
            double targetX = sideTeachingX;
            double targetY = sideTeachingY;
            int bottomReferencePickerNo;
            bool bottomReferenceStored;
            if (TryResolveBottomReferenceForSideTarget(
                pickerIndex,
                out targetX,
                out targetY,
                out bottomReferencePickerNo,
                out bottomReferenceStored))
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side X/Y 목표를 Side 티칭값 대신 Bottom 공정 X/Y 기준으로 적용합니다. " +
                    "sidePickerNo=" + pickerNo +
                    ", bottomReferencePickerNo=" + bottomReferencePickerNo +
                    ", targetX=" + targetX.ToString("0.###") +
                    ", targetY=" + targetY.ToString("0.###") +
                    ", sideTeachingXIgnored=" + sideTeachingX.ToString("0.###") +
                    ", sideTeachingYIgnored=" + sideTeachingY.ToString("0.###") +
                    ", source=" + (bottomReferenceStored ? "RuntimeBottomTarget" : "BottomTeachingFallback") + " - Check");
            }
            return new InspectionTarget
            {
                PickerIndex = pickerIndex,
                PickerNo = pickerNo,
                Die = die,
                X = targetX,
                Y = targetY,
                Z = baseZ + zOffset,
                T0 = t0,
                T90 = t0 + 90.0,
                SideCorrectionValid = correction != null && correction.IsValid,
                SideVisionYOffset = sideYOffset,
                SidePickerZBase = baseZ,
                SidePickerZOffset = zOffset,
                SideVisionProcess0BaseY = process0BaseY,
                SideVisionProcess90BaseY = process90BaseY,
                SideVisionProcess0Y = process0BaseY + sideYOffset,
                SideVisionProcess90Y = process90BaseY + sideYOffset,
                SideCorrectionSourceDieId = correction != null ? correction.SourceDieId : string.Empty
            };
        }

        private VisionAxis ResolveSideVisionAxis()
        {
            return Side == PickerSequenceSide.Front ? VisionAxis.FrontSideVisionY : VisionAxis.RearSideVisionY;
        }

        private double ResolveSideVisionBasePosition(int angleDeg)
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return 0.0;

                string positionName = angleDeg == 90 ? "Process90Position" : "Process0Position";
                return vision.GetVisionTeachingPosition(ResolveSideVisionAxis(), positionName);
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private double ResolveSideVisionTargetY(InspectionTarget target, int angleDeg)
        {
            if (target == null)
                return ResolveSideVisionBasePosition(angleDeg);

            if (angleDeg == 90)
                return target.SideVisionProcess90Y;

            return target.SideVisionProcess0Y;
        }

        private InspectionTarget PrepareAutoFirstSideTargetForLastBottom(InspectionTarget lastBottomTarget)
        {
            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return null;

                if (lastBottomTarget == null || _pickedPickerIndexes.Count == 0)
                    return null;

                if (!_sidePipelineEnabled)
                    return null;

                if (lastBottomTarget.PickerNo != 1)
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Auto Bottom 마지막/Side 첫 동시 검사를 생략합니다. " +
                        "Bottom 마지막 대상이 pickerNo=1이 아닙니다. " +
                        "lastBottomPickerNo=" + lastBottomTarget.PickerNo +
                        ", loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + " - Check");
                    return null;
                }

                int sideFirstPickerIndex = ToPickerIndex(4);
                if (!_pickedPickerIndexes.Contains(sideFirstPickerIndex))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Auto Bottom 마지막/Side 첫 동시 검사를 생략합니다. " +
                        "pickerNo=4에 제품이 없습니다. 현재 배치는 Bottom만 진행합니다. " +
                        "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + " - Check");
                    return null;
                }

                if (_sideCompletedPickerIndexes.Contains(sideFirstPickerIndex))
                    return null;

                InspectionTarget sideTarget = BuildSideTarget(sideFirstPickerIndex);
                if (sideTarget == null || sideTarget.Die == null)
                    return null;

                double bottomX = lastBottomTarget.X;
                double bottomY = lastBottomTarget.Y;
                double sharedX = bottomX;
                double preparedSideX = sideTarget.X;
                double preparedSideY = sideTarget.Y;

                sideTarget.X = sharedX;
                sideTarget.Y = bottomY;

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Auto Bottom 마지막 검사와 Side 첫 검사를 같은 XY 위치에서 동시 진행하도록 공유 위치를 적용합니다. " +
                    "bottomPickerNo=" + lastBottomTarget.PickerNo +
                    ", sidePickerNo=" + sideTarget.PickerNo +
                    ", sharedX=" + sharedX.ToString("0.###") +
                    ", sharedY=" + bottomY.ToString("0.###") +
                    ", preparedSideX=" + preparedSideX.ToString("0.###") +
                    ", preparedSideY=" + preparedSideY.ToString("0.###") + " - Check");

                return sideTarget;
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Auto Bottom 마지막/Side 첫 동시 검사 공유 위치 준비 실패. error=" + ex.Message + " - Failed");
                return null;
            }
            finally
            {
            }
        }

        private async Task<int> CompletePreparedFirstSideInspectionDuringLastBottomAsync(InspectionTarget target, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (target == null || target.Die == null)
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom 마지막 검사 중 병렬 시작할 Side 첫 대상이 없습니다. - Check");
                    return 0;
                }

                if (HasInspectionResult(target.Die, "Side0") &&
                    HasInspectionResult(target.Die, "Side90"))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom 마지막 검사 중 Side 병렬 시작 대상에 기존 Side 결과가 있어도 Side shot을 다시 진행합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                }

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom 마지막 검사와 함께 시작한 Side 첫 검사의 90도 검사를 진행합니다. " +
                    "die=" + target.Die.DieId +
                    ", pickerNo=" + target.PickerNo +
                    ", sharedX=" + target.X.ToString("0.###") +
                    ", sharedY=" + target.Y.ToString("0.###") +
                    ", sideReadyBypass=True - Start");

                int result = await CompleteSideInspectionAfterSide0Async(target, ct).ConfigureAwait(false);
                if (result == 0 && !_sideCompletedPickerIndexes.Contains(target.PickerIndex))
                    _sideCompletedPickerIndexes.Add(target.PickerIndex);

                return result;
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
            int result = await PrepareSideTargetForInspectionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await StartSide0InspectionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await CompleteSideInspectionAfterSide0Async(target, ct).ConfigureAwait(false);
        }

        private async Task<int> PrepareSideTargetForInspectionAsync(InspectionTarget target, CancellationToken ct)
        {
            // Bottom 외곽 XYT 푸시(EventSearchDieEnd) 조회 — Side 에서 해당 콜렛 다이의 X/Y/T 사용 근거.
            // 현재는 가용성 확인/로그만 수행(보정 반영 방식은 공정 담당 확정 후 적용 — TODO).
            LogBottomXytForSide(target);
            LogSideCorrectionTarget(target);

            int result = await MoveSideXAndVision0PositionAsync(target, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side PickerZ 보정 이동. " +
                "side=" + Side +
                ", axis=PickerZ" + target.PickerNo +
                ", pickerNo=" + target.PickerNo +
                ", die=" + target.Die.DieId +
                ", baseZ=" + target.SidePickerZBase.ToString("F6") +
                ", offsetZ=" + target.SidePickerZOffset.ToString("F6") +
                ", finalZ=" + target.Z.ToString("F6") +
                ", correctionValid=" + target.SideCorrectionValid + " - Start");

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

            return 0;
        }

        private async Task<int> StartSide0InspectionAsync(InspectionTarget target, CancellationToken ct, bool skipDelay = false)
        {
            using (TactTimeScope side0TactScope = BeginDetailedTactScope(
                TactTimeCategory.Vision,
                "Side 0deg Inspect",
                "0deg",
                target,
                "Side 0deg inspect start."))
            {
                try
                {
                    bool side0Started = await TriggerSideInspectionStartAsync(target, 0, ct, skipDelay).ConfigureAwait(false);
                    if (!side0Started)
                    {
                        MarkInspectionResultReceiveFailureAsNg(
                            target,
                            "Side0",
                            "SIDE0_RESULT_MISSING",
                            "Side 0도 검사 결과 ACK/RESULT 미수신");

                        side0TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE0-RESULT", BuildTactDetail(target, "Side 0deg inspection result receive failed."));
                        return Fail("PICKER-BOTTOM-SIDE-SIDE0-RESULT", "Vision", "Side 0도 검사 시작 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);
                    }

                    // 현재 기준: Side 0도는 시작 ACK만 확인하고, 결과는 90도 검사 후 통합 결과로 받는다.
                    side0TactScope.Complete(BuildTactDetail(target, "Side 0deg inspection started. RESULT는 Side 90deg 시작 후 회수합니다."));
                    RecordInspectionCheckpointForTact(
                        "Side0Inspection",
                        "Side 0deg Inspect Interval",
                        "0deg",
                        target,
                        "started=True,resultPending=True");
                }
                catch (OperationCanceledException)
                {
                    side0TactScope.Cancel(BuildTactDetail(target, "Side 0deg inspection canceled."));
                    throw;
                }
                catch (SequenceStopException)
                {
                    side0TactScope.Stop("SEQUENCE-STOP", BuildTactDetail(target, "Side 0deg inspection stopped."));
                    throw;
                }
                catch (Exception ex)
                {
                    side0TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE0-EX", BuildTactDetail(target, "Side 0deg inspection exception. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

            return 0;
        }

        private async Task<int> CompleteSideInspectionAfterSide0Async(InspectionTarget target, CancellationToken ct)
        {
            int result;
            SideVisionResult side0Result = null;

            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);

            using (TactTimeScope side90MoveTactScope = BeginDetailedTactScope(
                TactTimeCategory.Motion,
                "Side 0deg To 90deg Motion",
                "0deg->90deg",
                target,
                "Side 0deg to 90deg motion start."))
            {
                try
                {
                    result = await MoveSideT90AndVision90PositionAsync(target, ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        side90MoveTactScope.Fail("PICKER-BOTTOM-SIDE-SIDE-T90-VISION90", BuildTactDetail(target, "Side 0deg to 90deg motion failed. result=" + result));
                        return result;
                    }

                    side90MoveTactScope.Complete(BuildTactDetail(target, "Side 0deg to 90deg motion completed."));
                }
                catch (OperationCanceledException)
                {
                    side90MoveTactScope.Cancel(BuildTactDetail(target, "Side 0deg to 90deg motion canceled."));
                    throw;
                }
                catch (SequenceStopException)
                {
                    side90MoveTactScope.Stop("SEQUENCE-STOP", BuildTactDetail(target, "Side 0deg to 90deg motion stopped."));
                    throw;
                }
                catch (Exception ex)
                {
                    side90MoveTactScope.Fail("PICKER-BOTTOM-SIDE-SIDE-T90-EX", BuildTactDetail(target, "Side 0deg to 90deg motion exception. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

            SideVisionResult side90Result;
            using (TactTimeScope side90TactScope = BeginDetailedTactScope(
                TactTimeCategory.Vision,
                "Side 90deg Inspect",
                "90deg",
                target,
                "Side 90deg inspect start."))
            {
                try
                {
                    bool side90Started = await TriggerSideInspectionStartAsync(target, 90, ct).ConfigureAwait(false);
                    if (!side90Started)
                    {
                        MarkInspectionResultReceiveFailureAsNg(
                            target,
                            "Side90",
                            "SIDE90_RESULT_MISSING",
                            "Side 90도 검사 시작 ACK 미수신");

                        side90TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE90-RESULT", BuildTactDetail(target, "Side 90deg inspection result receive failed."));
                        return Fail("PICKER-BOTTOM-SIDE-SIDE90-RESULT", "Vision", "Side 90도 검사 시작 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);
                    }

                    // 수정 포인트: Side RESULT 미수신을 임시 통과시키려면 아래 Wait 대신 BuildSideResultWaitSkipped(target, 90)를 사용한다.
                    SideVisionResult sideResult = await WaitSideInspectionResultAsync(target, ct).ConfigureAwait(false);
                    if (sideResult == null)
                    {
                        MarkInspectionResultReceiveFailureAsNg(
                            target,
                            "Side",
                            "SIDE_RESULT_MISSING",
                            "Side 검사 결과 ACK/RESULT 미수신");

                        side90TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE-RESULT", BuildTactDetail(target, "Side inspection result receive failed."));
                        return Fail("PICKER-BOTTOM-SIDE-SIDE-RESULT", "Vision", "Side 검사 결과 수신 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);
                    }

                    side0Result = sideResult;
                    side90Result = sideResult;
                    side90TactScope.Complete(BuildTactDetail(target, "Side 90deg inspection RESULT 수신 완료. ok=" + side90Result.IsAllOk));
                    RecordInspectionCheckpointForTact(
                        "Side90Inspection",
                        "Side 90deg Inspect Interval",
                        "90deg",
                        target,
                        "started=True,resultReceived=True,ok=" + side90Result.IsAllOk);
                }
                catch (OperationCanceledException)
                {
                    side90TactScope.Cancel(BuildTactDetail(target, "Side 90deg inspection canceled."));
                    throw;
                }
                catch (SequenceStopException)
                {
                    side90TactScope.Stop("SEQUENCE-STOP", BuildTactDetail(target, "Side 90deg inspection stopped."));
                    throw;
                }
                catch (Exception ex)
                {
                    side90TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE90-EX", BuildTactDetail(target, "Side 90deg inspection exception. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

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

            Task<int> visionTask = IsSideVisionProcessPositionReady(target, 0)
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

            Task<int> visionTask = IsSideVisionProcessPositionReady(target, 90)
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

                VisionAxis axis = ResolveSideVisionAxis();
                string positionName = angleDeg == 90 ? "Process90Position" : "Process0Position";
                double baseY = vision.GetVisionTeachingPosition(axis, positionName);
                double targetY = ResolveSideVisionTargetY(target, angleDeg);

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " SideVisionY 보정 이동. " +
                    "side=" + Side +
                    ", axis=" + axis +
                    ", pickerNo=" + target.PickerNo +
                    ", die=" + target.Die.DieId +
                    ", angle=" + angleDeg +
                    ", baseY=" + baseY.ToString("F6") +
                    ", offsetY=" + target.SideVisionYOffset.ToString("F6") +
                    ", finalY=" + targetY.ToString("F6") +
                    ", correctionValid=" + target.SideCorrectionValid +
                    ", sourceDie=" + (target.SideCorrectionSourceDieId ?? string.Empty) + " - Start");

                int result = await vision.MoveVisionAxis(axis, targetY, Options != null && Options.FineMove).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-BOTTOM-SIDE-VISION-POSITION", "Vision",
                        "Side 검사 카메라 " + angleDeg + "도 보정 위치 이동 실패. " +
                        "result=" + result +
                        ", side=" + Side +
                        ", axis=" + axis +
                        ", pickerNo=" + target.PickerNo +
                        ", baseY=" + baseY.ToString("F6") +
                        ", offsetY=" + target.SideVisionYOffset.ToString("F6") +
                        ", finalY=" + targetY.ToString("F6"));

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

        private bool IsSideVisionProcessPositionReady(InspectionTarget target, int angleDeg)
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return false;

                VisionAxis axis = ResolveSideVisionAxis();
                BaseAxis item = vision.ResolveVisionAxis(axis);
                double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                    ? item.Config.InPositionTolerance
                    : 0.05;
                double targetY = ResolveSideVisionTargetY(target, angleDeg);
                return vision.IsVisionAxisInPosition(axis, targetY, tolerance);
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
            bool started = await TriggerSideInspectionStartAsync(target, angleDeg, ct).ConfigureAwait(false);
            if (!started)
                return null;

            return await WaitSideInspectionResultAsync(target, ct).ConfigureAwait(false);
        }

        // 임시 우회용: Side RESULT 대기를 스킵하고 OK 결과를 만든다. 현재 통합 시퀀스에서는 직접 호출하지 않는다.
        private SideVisionResult BuildSideResultWaitSkipped(InspectionTarget target, int angleDeg)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values["ResultWaitSkipped"] = "True";
            values["AngleDeg"] = angleDeg.ToString();
            values["DieId"] = target != null && target.Die != null ? target.Die.DieId : string.Empty;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 검사 시작 ACK 이후 결과 수신 대기를 생략하고 진행합니다. " +
                "side=" + Side +
                ", die=" + (target != null && target.Die != null ? target.Die.DieId : string.Empty) +
                ", pickerNo=" + (target != null ? target.PickerNo : 0) +
                ", angleDeg=" + angleDeg + " - Check");

            return new SideVisionResult
            {
                PickerNo = target != null ? target.PickerNo : 0,
                Side1Ok = true,
                Side2Ok = true,
                Side3Ok = true,
                Side4Ok = true,
                Raw = "SIDE_RESULT_WAIT_SKIPPED",
                Values = values
            };
        }

        private async Task<bool> TriggerSideInspectionStartAsync(InspectionTarget target, int angleDeg, CancellationToken ct, bool skipDelay = false)
        {
            if (!skipDelay)
                await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            RegisterVisionDieAddress(target);   // 신형 와이어(die_index/gridx;gridy) 구성용 — 어댑터가 조회

            int timeoutMs = ResolveVisionInspectionTimeout();
            if (IsDryRunMode())
                return await TriggerDryRunSideGrabIfConnectedAsync(target, angleDeg, timeoutMs, ct).ConfigureAwait(false);

            bool triggered = Side == PickerSequenceSide.Front
                ? await FrontPicker.StartSideInspectionAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.StartSideInspectionAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);

            if (!triggered)
                return false;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 검사 시작 ACK 수신 후 Vision 백그라운드 결과 대기 상태로 진행합니다. " +
                "side=" + Side +
                ", die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", angleDeg=" + angleDeg +
                ", timeoutMs=" + timeoutMs + " - Ok");
            return true;
        }

        private async Task<bool> TriggerDryRunSideGrabIfConnectedAsync(InspectionTarget target, int angleDeg, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            QMC.CDT320.VisionComm.AutoVisionChannel channel = Side == PickerSequenceSide.Front
                ? QMC.CDT320.VisionComm.AutoVisionChannel.FrontSide
                : QMC.CDT320.VisionComm.AutoVisionChannel.RearSide;
            if (!IsVisionConnected(channel))
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " DryRun Side GRAB skipped. Vision is not connected. channel=" + channel +
                    ", die=" + (target != null && target.Die != null ? target.Die.DieId : "") +
                    ", pickerNo=" + (target != null ? target.PickerNo : 0) +
                    ", angleDeg=" + angleDeg + " - Check");
                return true;
            }

            bool grabbed = Side == PickerSequenceSide.Front
                ? await FrontPicker.TriggerSideInspectionExposeAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.TriggerSideInspectionExposeAsync(target.PickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " DryRun Side GRAB " + (grabbed ? "completed" : "failed") +
                ". channel=" + channel +
                ", die=" + (target != null && target.Die != null ? target.Die.DieId : "") +
                ", pickerNo=" + (target != null ? target.PickerNo : 0) +
                ", angleDeg=" + angleDeg +
                ", timeoutMs=" + timeoutMs + " - Check");
            return true;
        }

        private bool IsDryRunMode()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode;
        }

        private bool IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel channel)
        {
            return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(channel);
        }

        // 현재 기준: Side 결과를 timeout까지 기다리며, null이면 상위에서 알람 정지한다.
        private async Task<SideVisionResult> WaitSideInspectionResultAsync(InspectionTarget target, CancellationToken ct)
        {
            int timeoutMs = ResolveVisionInspectionTimeout();
            return Side == PickerSequenceSide.Front
                ? await FrontPicker.WaitSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);
        }

        /// <summary>현재 콜렛의 다이 주소(die_index=InputSequenceNo, grid=Wafer_IndexX/Y)를
        /// <see cref="QMC.CDT320.VisionComm.VisionDieAddressStore"/> 에 기록 — 어댑터가 신형 와이어
        /// "tool|fb|collet|die_index|channel|gridx;gridy"(chip_uid 폐기, 2026-07-06)를 구성할 때 사용.
        /// 다이 정보가 없으면 기록 생략(어댑터가 콜렛별 음수 합성키로 대체).</summary>
        private void RegisterVisionDieAddress(InspectionTarget target)
        {
            try
            {
                if (target == null || target.Die == null)
                    return;

                int fb = Side == PickerSequenceSide.Front ? 0 : 1;
                QMC.CDT320.VisionComm.VisionDieAddressStore.Set(
                    fb,
                    target.PickerNo,
                    target.Die.InputSequenceNo,
                    target.Die.Wafer_IndexX,
                    target.Die.Wafer_IndexY,
                    target.Die.DieId);
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " 비전 다이 주소 기록 실패(진행에는 영향 없음). error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        /// <summary>Side 진입 시 Bottom XYT 푸시 가용성 확인 — (fb=자기 그룹, collet=pickerNo) 최신값 로그.</summary>
        private void LogBottomXytForSide(InspectionTarget target)
        {
            try
            {
                if (target == null)
                    return;

                int fb = Side == PickerSequenceSide.Front ? 0 : 1;
                QMC.CDT320.VisionComm.BottomXytPush xyt;
                if (QMC.CDT320.VisionComm.BottomXytStore.TryGet(fb, target.PickerNo, out xyt))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Side 진입 — Bottom XYT 푸시 확인. die=" + (target.Die != null ? target.Die.DieId : "-") +
                        ", fb=" + fb +
                        ", collet=" + target.PickerNo +
                        ", dieIndex=" + xyt.DieIndex +
                        ", x=" + xyt.X.ToString("F3") +
                        ", y=" + xyt.Y.ToString("F3") +
                        ", t=" + xyt.T.ToString("F4") +
                        ", valid=" + xyt.IsValid +
                        ", age=" + (DateTime.Now - xyt.ReceivedAt).TotalMilliseconds.ToString("F0") + "ms - Check");
                }
                else
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Side 진입 — Bottom XYT 푸시 미수신(스토어 없음). fb=" + fb +
                        ", collet=" + target.PickerNo + " - Check");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom XYT 조회 실패(진행에는 영향 없음). error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        private void LogSideCorrectionTarget(InspectionTarget target)
        {
            if (target == null || target.Die == null)
                return;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 검사 보정 목표 계산. " +
                "side=" + Side +
                ", pickerNo=" + target.PickerNo +
                ", die=" + target.Die.DieId +
                ", correctionValid=" + target.SideCorrectionValid +
                ", sourceDie=" + (target.SideCorrectionSourceDieId ?? string.Empty) +
                " | PickerX.final=" + target.X.ToString("F6") +
                " | PickerY.final=" + target.Y.ToString("F6") +
                " | PickerZ.base=" + target.SidePickerZBase.ToString("F6") +
                ", PickerZ.offset=" + target.SidePickerZOffset.ToString("F6") +
                ", PickerZ.final=" + target.Z.ToString("F6") +
                " | PickerT0.final=" + target.T0.ToString("F6") +
                ", PickerT90.final=" + target.T90.ToString("F6") +
                " | SideVisionY0.base=" + target.SideVisionProcess0BaseY.ToString("F6") +
                ", SideVisionY0.offset=" + target.SideVisionYOffset.ToString("F6") +
                ", SideVisionY0.final=" + target.SideVisionProcess0Y.ToString("F6") +
                " | SideVisionY90.base=" + target.SideVisionProcess90BaseY.ToString("F6") +
                ", SideVisionY90.offset=" + target.SideVisionYOffset.ToString("F6") +
                ", SideVisionY90.final=" + target.SideVisionProcess90Y.ToString("F6") +
                " - Check");
        }

        private string BuildSideTargetName(InspectionTarget target)
        {
            return AppendAutoProcessCorrectionTargetTag(BuildPickerTargetName("DieSidePosition", target.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Side");
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

        private void StartNextBottomZDownCommand(int nextCursor)
        {
            try
            {
                if (nextCursor < 0 || nextCursor >= _pickedPickerIndexes.Count)
                    return;

                PickerBottomInspectionMotionConfig config = ResolveBottomInspectionMotionConfig();
                if (config.FlyingZDownMode == PickerBottomFlyingZDownMode.Off)
                    return;

                int nextPickerIndex = _pickedPickerIndexes[nextCursor];
                InspectionTarget nextTarget = BuildBottomTarget(nextPickerIndex);
                if (nextTarget == null || nextTarget.Die == null)
                    return;

                PickerAxis zAxis = GetPickerZAxis(nextPickerIndex);
                double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                double zTarget = config.ResolveFlyingZDownTarget(avoid, nextTarget.Z);
                if (Math.Abs(zTarget - avoid) <= 0.0001)
                    return;

                if (!IsPickerAxisInPosition(zAxis, nextTarget.Z) &&
                    !IsPickerAxisInPosition(zAxis, avoid))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom 검사 중 다음 PickerZ 선행 하강을 생략합니다. PickerZ가 Avoid 위치가 아닙니다. " +
                        "pickerNo=" + ToPickerNo(nextPickerIndex) +
                        ", axis=" + zAxis +
                        ", avoid=" + avoid.ToString("0.###") +
                        ", target=" + zTarget.ToString("0.###") +
                        ", mode=" + config.FlyingZDownMode + " - Check");
                    return;
                }

                // 현재 기준: 통합 Bottom/Side 선행 Z는 FlyingZDownMode/Distance를 따른다.
                QueuePendingBottomZDown(nextPickerIndex, zTarget);
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
                    BuildPickerTargetName("DieBottomZPreDown", pending.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom");

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " " + description + " 명령 시작. pickerNo=" + ToPickerNo(pending.PickerIndex) +
                    ", target=" + pending.Target.ToString("0.###") + " - Ok");
            }
        }

        private PickerBottomInspectionMotionConfig ResolveBottomInspectionMotionConfig()
        {
            PickerBottomInspectionMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
            {
                if (FrontPicker.Config.BottomInspection == null)
                    FrontPicker.Config.BottomInspection = new PickerBottomInspectionMotionConfig();
                config = FrontPicker.Config.BottomInspection;
            }
            else if (RearPicker != null && RearPicker.Config != null)
            {
                if (RearPicker.Config.BottomInspection == null)
                    RearPicker.Config.BottomInspection = new PickerBottomInspectionMotionConfig();
                config = RearPicker.Config.BottomInspection;
            }

            if (config == null)
                config = new PickerBottomInspectionMotionConfig();

            config.Ensure();
            return config;
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
                    int commandResult = await SequenceAwaiter.AwaitAsync(
                        pending.MoveTask,
                        -1,
                        ct).ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-CMD", Name, "예약된 Bottom PickerZ 선행 하강 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }
                else if (!IsPickerAxisInPosition(axis, pending.Target))
                {
                    int commandResult = await SequenceAwaiter.AwaitAsync(
                        MovePickerAxisCommandAsync(
                            axis,
                            pending.Target,
                            BuildPickerTargetName("DieBottomZPreDown", pending.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom"),
                        -1,
                        ct).ConfigureAwait(false);
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
            MaterialInspectionResult inspectionResult = result != null && result.IsAllOk
                ? MaterialInspectionResult.Ok
                : MaterialInspectionResult.Ng;

            var measurements = new List<InspectionMeasurement>
            {
                BuildBooleanMeasurement(prefix + "Side1", side1Ok),
                BuildBooleanMeasurement(prefix + "Side2", side2Ok),
                BuildBooleanMeasurement(prefix + "Side3", side3Ok),
                BuildBooleanMeasurement(prefix + "Side4", side4Ok),
                BuildBooleanMeasurement(prefix + "InspectionResult", result != null && result.IsAllOk)
            };

            AppendVisionRawMeasurements(measurements, result, prefix, inspectionResult);
            return measurements;
        }

        private void MarkInspectionResultReceiveFailureAsNg(
            InspectionTarget target,
            string inspectionType,
            string ngCode,
            string reason)
        {
            try
            {
                if (target == null || target.Die == null)
                    return;

                double t = target.T0;
                if (string.Equals(inspectionType, "Side90", StringComparison.OrdinalIgnoreCase))
                    t = target.T90;

                MaterialStateService.UpsertInspection(target.Die.DieId, new DieInspectionRecord
                {
                    InspectionType = inspectionType,
                    Result = MaterialInspectionResult.Ng,
                    Offset = new VisionOffset { IsValid = false },
                    NgCodes = new List<string> { ngCode },
                    Alignments = new List<InspectionAlignmentSnapshot>
                    {
                        BuildPickerAlignmentSnapshot(inspectionType, target.PickerIndex, target.X, target.Y, t, target.Z, new VisionOffset { IsValid = false })
                    },
                    Measurements = new List<InspectionMeasurement>
                    {
                        BuildBooleanMeasurement(inspectionType + "ResultReceived", false),
                        BuildBooleanMeasurement(inspectionType + "InspectionResult", false)
                    }
                });

                MaterialStateService.ApplyDieInspectionResult(
                    target.Die.DieId,
                    DieResult.NG,
                    ngCode,
                    inspectionType + "ResultMissing");

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " 검사 결과 미수신으로 Die를 NG 처리합니다. " +
                    "inspectionType=" + inspectionType +
                    ", die=" + target.Die.DieId +
                    ", pickerNo=" + target.PickerNo +
                    ", ngCode=" + ngCode +
                    ", reason=" + reason + " - Failed");
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " 검사 결과 미수신 NG 처리 중 예외 발생. " +
                    "inspectionType=" + inspectionType +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
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
                    BuildPickerTargetName("DieSideZAvoidDeferred", pending.PickerIndex));

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
                    int commandResult = await SequenceAwaiter.AwaitAsync(
                        pending.MoveTask,
                        -1,
                        ct).ConfigureAwait(false);
                    if (commandResult != 0)
                        return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-CMD", Name, "예약된 PickerZ Avoid 명령 실패. result=" + commandResult + ", pickerNo=" + ToPickerNo(pending.PickerIndex));
                }
                else if (!IsPickerAxisInPosition(axis, pending.Target))
                {
                    int commandResult = await SequenceAwaiter.AwaitAsync(
                        MovePickerAxisCommandAsync(
                            axis,
                            pending.Target,
                            BuildPickerTargetName("DieSideZAvoidDeferred", pending.PickerIndex)),
                        -1,
                        ct).ConfigureAwait(false);
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
                    BuildPickerTargetName("DieSideT0ReturnDeferred", pending.PickerIndex));

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
