using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.Diagnostics.TactTime;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomAndSideInspectionSequence : PickerSequenceBase<PickerBottomAndSideInspectionStep>
    {
        private const double MaxSideVisionCenterCorrectionMm = 2.0;

        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private readonly List<BottomShot> _pendingBottomShots = new List<BottomShot>();
        private readonly List<int> _sideReadyPickerIndexes = new List<int>();
        private readonly List<int> _sideCompletedPickerIndexes = new List<int>();
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();
        private readonly List<PendingZAvoid> _pendingZAvoids = new List<PendingZAvoid>();
        private readonly List<PendingBottomZDown> _pendingBottomZDowns = new List<PendingBottomZDown>();
        private readonly Dictionary<int, SideTargetPosition> _bottomReferencePositions = new Dictionary<int, SideTargetPosition>();
        private readonly Dictionary<int, SideTargetPosition> _sideTargetPositions = new Dictionary<int, SideTargetPosition>();

        private bool _bottomInspectionYReady;
        private bool _sideInspectionYReady;
        private bool _sidePipelineEnabled;
        private bool _parallelFirstSideEnabled;
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
            public double SideVisionProcess0YOffset;
            public double SideVisionProcess90YOffset;
            public double SidePickerZBase;
            public double SidePickerZOffset;
            public SideVisionPositionTarget FrontSideVision;
            public SideVisionPositionTarget RearSideVision;
            public string SideCorrectionSourceDieId;
        }

        private sealed class SideVisionPositionTarget
        {
            public PickerSequenceSide CameraSide;
            public VisionAxis Axis;
            public double Process0BaseY;
            public double Process90BaseY;
            public double Process0Y;
            public double Process90Y;
            public bool Focus0CalibrationValid;
            public bool Focus90CalibrationValid;
        }

        private sealed class BottomShot
        {
            public InspectionTarget Target;
            public bool Applied;
            public DateTime InspectStartedAt;
        }

        private sealed class SideTargetPosition
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

        public bool PickerZStageSafeConfirmedByPickUp { get; set; }

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
                _sideTargetPositions.Clear();
                _sidePipelineEnabled = false;
                _parallelFirstSideEnabled = false;
                ForceBottomInspectionBeforeSideResume = false;
                PickerZStageSafeConfirmedByPickUp = false;
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
                    ct,
                    true).ConfigureAwait(false);
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
            _sideTargetPositions.Clear();
            _bottomInspectionYReady = false;
            _sideInspectionYReady = false;
            _sidePipelineEnabled = false;
            _parallelFirstSideEnabled = false;

            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerBottomAndSideInspectionSequence"));
            RemoveSkippedPickerTargets();
            if (_pickedPickerIndexes.Count == 0)
            {
                CurrentStep = PickerBottomAndSideInspectionStep.Complete;
                return 0;
            }

            string orderReason;
            if (!IsBottomSideRunOrderValid(out orderReason))
            {
                return Fail("PICKER-BOTTOM-SIDE-RUN-ORDER", Name,
                    "Bottom/Side 통합 검사는 보유 Picker를 4->3->2->1 순서로만 처리합니다. " + orderReason);
            }

            // 풀 배치에서만 Bottom1/Side4 병렬을 허용한다. Side 좌표는 Bottom 반대 번호 기준으로 잡는다.
            _sidePipelineEnabled = true;
            _parallelFirstSideEnabled = _pickedPickerIndexes.Count == 4;

            int planResult = BuildSideTargetPlan();
            if (planResult != 0)
                return planResult;

            if (_parallelFirstSideEnabled)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " 4개 풀 배치이므로 Bottom1 검사와 Side4 첫 검사를 병렬 수행합니다. " +
                    "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + " - Check");
            }
            else
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " 부분 배치이므로 Side 병렬 동작만 생략합니다. " +
                    "보유 Picker의 Bottom 검사를 모두 완료한 뒤 Side 검사를 순차 수행합니다. " +
                    "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) +
                    ", count=" + _pickedPickerIndexes.Count + " - Check");
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom/Side 통합 검사 대상 구성 완료. count=" + _pickedPickerIndexes.Count +
                ", sidePipelineEnabled=" + _sidePipelineEnabled +
                ", parallelFirstSideEnabled=" + _parallelFirstSideEnabled + " - Ok");
            CurrentStep = PickerBottomAndSideInspectionStep.AcquireInspectionArea;
            return 0;
        }

        private bool IsBottomSideRunOrderValid(out string reason)
        {
            reason = string.Empty;
            if (_pickedPickerIndexes.Count <= 0)
            {
                reason = "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".";
                return false;
            }

            for (int i = 1; i < _pickedPickerIndexes.Count; i++)
            {
                if (_pickedPickerIndexes[i - 1] > _pickedPickerIndexes[i])
                    continue;

                reason = "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) +
                    ", expectedOrder=4->3->2->1 subset.";
                return false;
            }

            return true;
        }

        private int BuildSideTargetPlan()
        {
            _sideTargetPositions.Clear();

            try
            {
                var bottomTargetPositions = new Dictionary<int, SideTargetPosition>();
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    int pickerIndex = ToPickerIndex(pickerNo);
                    var bottomTarget = ResolvePickerZoneTarget("DieBottomPosition", pickerIndex);

                    if (bottomTarget == null ||
                        !IsValidSidePlanCoordinate(bottomTarget.X) ||
                        !IsExpectedProcessYDirection(bottomTarget.Y))
                    {
                        return Fail("PICKER-BOTTOM-SIDE-TARGET-PLAN", Name,
                            "Bottom/Side 검사 X/Y 사전 검증 실패. Bottom 티칭 좌표와 Y 공정 방향을 확인하세요. " +
                            "side=" + Side +
                            ", pickerNo=" + pickerNo +
                            ", expectedYDirection=" + ResolveProcessYDirectionName() +
                            ", bottomX=" + (bottomTarget != null ? bottomTarget.X.ToString("0.###") : "null") +
                            ", bottomY=" + (bottomTarget != null ? bottomTarget.Y.ToString("0.###") : "null") + ".");
                    }

                    bottomTargetPositions[pickerIndex] = new SideTargetPosition
                    {
                        X = bottomTarget.X,
                        Y = bottomTarget.Y
                    };
                }

                int orderResult = ValidatePickerXRunOrder(bottomTargetPositions, "Bottom");
                if (orderResult != 0)
                    return orderResult;

                int side4BottomReferencePickerNo = ResolveBottomReferencePickerNoForSide(ToPickerIndex(4));
                SideTargetPosition sideXAnchorBottomTarget;
                if (!bottomTargetPositions.TryGetValue(ToPickerIndex(side4BottomReferencePickerNo), out sideXAnchorBottomTarget) ||
                    sideXAnchorBottomTarget == null ||
                    !IsValidSidePlanCoordinate(sideXAnchorBottomTarget.X))
                {
                    return Fail("PICKER-BOTTOM-SIDE-TARGET-PLAN", Name,
                        "Side 검사 X 앵커 계산 실패. Bottom1 공정 좌표를 확인하세요. " +
                        "side=" + Side +
                        ", side4BottomReferencePickerNo=" + side4BottomReferencePickerNo + ".");
                }

                double sideXAnchor = sideXAnchorBottomTarget.X;
                for (int sidePickerNo = 1; sidePickerNo <= 4; sidePickerNo++)
                {
                    int sidePickerIndex = ToPickerIndex(sidePickerNo);
                    int bottomReferencePickerNo = ResolveBottomReferencePickerNoForSide(sidePickerIndex);
                    SideTargetPosition bottomTarget;
                    if (!bottomTargetPositions.TryGetValue(ToPickerIndex(bottomReferencePickerNo), out bottomTarget) ||
                        bottomTarget == null ||
                        !IsValidSidePlanCoordinate(bottomTarget.X) ||
                        !IsExpectedProcessYDirection(bottomTarget.Y))
                    {
                        return Fail("PICKER-BOTTOM-SIDE-TARGET-PLAN", Name,
                            "Side 검사 X/Y 사전 할당 실패. Bottom 공정 좌표 설정을 확인하세요. " +
                            "side=" + Side +
                            ", sidePickerNo=" + sidePickerNo +
                            ", bottomReferencePickerNo=" + bottomReferencePickerNo +
                            ", targetX=" + (bottomTarget != null ? bottomTarget.X.ToString("0.###") : "null") +
                            ", targetY=" + (bottomTarget != null ? bottomTarget.Y.ToString("0.###") : "null") + ".");
                    }

                    double targetX = sideXAnchor + (sideXAnchor - bottomTarget.X);

                    _sideTargetPositions[sidePickerIndex] = new SideTargetPosition
                    {
                        X = targetX,
                        Y = bottomTarget.Y
                    };

                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Side 검사 X/Y 사전 할당 완료. " +
                        "side=" + Side +
                        ", sidePickerNo=" + sidePickerNo +
                        ", bottomReferencePickerNo=" + bottomReferencePickerNo +
                        ", bottomReferenceX=" + bottomTarget.X.ToString("0.###") +
                        ", bottomReferenceY=" + bottomTarget.Y.ToString("0.###") +
                        ", sideXAnchor=" + sideXAnchor.ToString("0.###") +
                        ", targetX=" + targetX.ToString("0.###") +
                        ", targetY=" + bottomTarget.Y.ToString("0.###") +
                        ", mapping=BottomReference" +
                        ", xCoordinate=SideCameraForwardFromBottom1Anchor" +
                        ", yProcessDirection=" + ResolveProcessYDirectionName() +
                        ", source=BottomProcessPlan - Check");
                }

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BOTTOM-SIDE-TARGET-PLAN", Name,
                    "Bottom/Side 검사 X/Y 사전 할당 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private static bool IsValidSidePlanCoordinate(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   Math.Abs(value) > 0.000001;
        }

        private bool IsExpectedProcessYDirection(double value)
        {
            if (!IsValidSidePlanCoordinate(value))
                return false;

            return Side == PickerSequenceSide.Front ? value > 0.0 : value < 0.0;
        }

        private string ResolveProcessYDirectionName()
        {
            return Side == PickerSequenceSide.Front ? "FrontPositive" : "RearNegative";
        }

        private int ValidatePickerXRunOrder(
            Dictionary<int, SideTargetPosition> targetPositions,
            string processName)
        {
            for (int pickerNo = 4; pickerNo > 1; pickerNo--)
            {
                SideTargetPosition current;
                SideTargetPosition next;
                if (!targetPositions.TryGetValue(ToPickerIndex(pickerNo), out current) || current == null ||
                    !targetPositions.TryGetValue(ToPickerIndex(pickerNo - 1), out next) || next == null)
                {
                    return Fail("PICKER-BOTTOM-SIDE-X-ORDER-TARGET", Name,
                        processName + " 검사 X 순서 확인에 필요한 Picker 좌표가 없습니다. " +
                        "side=" + Side + ", pickerNo=" + pickerNo + ".");
                }

                if (current.X < next.X)
                    continue;

                return Fail("PICKER-BOTTOM-SIDE-X-ORDER", Name,
                    processName + " 검사 X 좌표는 Picker4->3->2->1 순서로 증가해야 합니다. " +
                    "side=" + Side +
                    ", picker" + pickerNo + "X=" + current.X.ToString("0.###") +
                    ", picker" + (pickerNo - 1) + "X=" + next.X.ToString("0.###") + ".");
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " " + processName + " 검사 X 증가 순서 확인 완료. " +
                "side=" + Side +
                ", order=Picker4->3->2->1" +
                ", xDirection=Increasing - Ok");
            return 0;
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

                bool skipFullAvoidWait =
                    PickerZStageSafeConfirmedByPickUp &&
                    !ForceBottomInspectionBeforeSideResume;
                PickerZStageSafeConfirmedByPickUp = false;

                if (!skipFullAvoidWait)
                {
                    // PickUp의 안전 상승을 확인하지 못한 신규/재개 경로는 Full Avoid를 확인한다.
                    int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                        "Bottom/Side 통합 검사 진입 전 PickerZ 전체 Avoid",
                        ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
                else
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 직전 PickUp의 PickerZ Stage Safe 높이 확인을 사용하고 Full Avoid 완료 대기를 생략합니다. " +
                        "PickerZ는 기존 Avoid 목표로 계속 이동하며 Bottom 검사 이동을 이어갑니다. side=" + Side + " - Check");
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

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom 동일 Picker 티칭 위치를 적용합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + target.PickerNo +
                    ", targetX=" + target.X.ToString("0.###") +
                    ", targetY=" + target.Y.ToString("0.###") +
                    ", xDirection=Picker4To1Increasing" +
                    ", yProcessDirection=" + ResolveProcessYDirectionName() + " - Check");

                if (HasInspectionResult(target.Die, "Bottom"))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 기존 Bottom 검사 결과가 있어도 현재 PickUp 배치는 Bottom shot을 다시 진행합니다. " +
                        "die=" + target.Die.DieId +
                        ", pickerNo=" + target.PickerNo + " - Check");
                }

                bool isLastBottomShot = i >= _pickedPickerIndexes.Count - 1;
                InspectionTarget sideFirstTarget = null;
                int result = 0;
                if (isLastBottomShot)
                {
                    result = PrepareAutoFirstSideTargetForLastBottom(target, out sideFirstTarget);
                    if (result != 0)
                        return result;
                }

                result = await CompletePendingBottomZDownForPickerAsync(target.PickerIndex, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveBottomTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 다음 PickerZ는 현재 Bottom 검사와 겹쳐 선행 하강합니다.
                // 완료 확인은 다음 Picker 검사 진입 시 수행하여 현재 검사를 지연시키지 않습니다.
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

            _bottomReferencePositions[target.PickerIndex] = new SideTargetPosition
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

            SideTargetPosition plannedTarget;
            if (!_sideTargetPositions.TryGetValue(sidePickerIndex, out plannedTarget) ||
                plannedTarget == null ||
                !IsValidSidePlanCoordinate(plannedTarget.X) ||
                !IsExpectedProcessYDirection(plannedTarget.Y))
            {
                return false;
            }

            x = plannedTarget.X;

            int bottomReferencePickerIndex = ToPickerIndex(bottomReferencePickerNo);
            SideTargetPosition reference;
            if (_bottomReferencePositions.TryGetValue(bottomReferencePickerIndex, out reference) &&
                reference != null &&
                IsExpectedProcessYDirection(reference.Y))
            {
                y = reference.Y;
                stored = true;
                return true;
            }

            y = plannedTarget.Y;
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
                await DelayBeforeBottomVisionInspectionAsync(target.PickerNo, ct).ConfigureAwait(false);

            RegisterVisionDieAddress(target);   // 신형 와이어(die_index/gridx;gridy) 구성용 — 어댑터가 조회

            int timeoutMs = ResolveVisionInspectionTimeout();
            if (IsDryRunMode())
            {
                await TriggerDryRunBottomGrabIfConnectedAsync(target, timeoutMs, ct).ConfigureAwait(false);
                await DelayAfterRuntimeAutoFocusBottomGrabAckAsync(target.PickerNo, ct).ConfigureAwait(false);
                return 0;
            }

            bool started = Side == PickerSequenceSide.Front
                ? await FrontPicker.StartBottomInspectionAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.StartBottomInspectionAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (!started)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-TRIGGER", "Vision",
                    "Bottom 검사 EPD 수신 실패. die=" + target.Die.DieId +
                    ", pickerNo=" + target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 검사 EPD 수신 완료. die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", pendingResult=" + (_pendingBottomShots.Count + 1) + " - Ok");
            await DelayAfterRuntimeAutoFocusBottomGrabAckAsync(target.PickerNo, ct).ConfigureAwait(false);
            return 0;
        }

        private async Task<int[]> StartBottomAndPreparedSide0InspectionAsync(InspectionTarget bottomTarget, InspectionTarget sideTarget, CancellationToken ct)
        {
            await DelayBeforeBottomVisionInspectionAsync(bottomTarget.PickerNo, ct).ConfigureAwait(false);

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

                if (CountPendingBottomResults() <= 0)
                {
                    return Fail("PICKER-BOTTOM-SIDE-BOTTOM-RESULT-NOT-PENDING", "Vision",
                        "Side 검사 전에 받아야 할 동일 Picker의 Bottom 결과 대기 항목이 없습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + ToPickerNo(pickerIndex) + ".");
                }

                int result = await ApplyOldestBottomResultIfNeededAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return ValidateRuntimeSideInspectionCorrection(pickerIndex);
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

            if (result.PickerNo != shot.Target.PickerNo)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-RESULT-PICKER-MISMATCH", "Vision",
                    "Bottom 검사 결과의 Picker 번호가 요청 대상과 일치하지 않습니다. " +
                    "side=" + Side +
                    ", requestedPickerNo=" + shot.Target.PickerNo +
                    ", resultPickerNo=" + result.PickerNo +
                    ", die=" + shot.Target.Die.DieId + ".");
            }

            ApplyBottomInspectionResult(shot.Target, result);

            int correctionResult = ValidateRuntimeSideInspectionCorrection(shot.Target.PickerIndex);
            if (correctionResult != 0)
                return correctionResult;

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
                ", centerOffsetXmm=" + result.BottomCenterOffsetX.ToString("F6") +
                ", centerOffsetYmm=" + result.BottomCenterOffsetY.ToString("F6") +
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
                BuildMeasurement("BottomCenterOffsetX", result.BottomCenterOffsetX, "mm", inspectionResult),
                BuildMeasurement("BottomCenterOffsetY", result.BottomCenterOffsetY, "mm", inspectionResult),
                BuildBooleanMeasurement("BottomCenterOffsetValid", result.HasBottomCenterOffset),
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

            // 현재 기준: Bottom 검사 결과(BottomCenterOffsetX/Y)는 Side 위치 보정에 적용하지 않는다.
            // Side 위치는 콜렛별 Side AF 저장값(BestPosition)과 티칭으로만 결정한다(Bottom/Side AutoFocus 간섭 금지).
            // Bottom 중심 값은 Vision ROI 공칭 중심 기준이라 die 주차 위치와의 상수 편차(수 mm)가 섞여 Side 보정으로 쓸 수 없다.
            bool valid = true;
            double sideVisionProcess0YOffset = 0.0;
            double sideVisionProcess90YOffset = 0.0;
            double pickerZOffset = result.HasSideInspectionCorrection && IsFiniteCorrectionValue(result.PickerZOffset)
                ? result.PickerZOffset
                : 0.0;
            string sourceDieId = target.Die != null ? target.Die.DieId : string.Empty;

            if (Side == PickerSequenceSide.Front)
            {
                if (FrontPicker != null)
                    FrontPicker.SetRuntimeSideInspectionCorrection(
                        target.PickerIndex,
                        sideVisionProcess0YOffset,
                        sideVisionProcess90YOffset,
                        pickerZOffset,
                        valid,
                        sourceDieId);
            }
            else
            {
                if (RearPicker != null)
                    RearPicker.SetRuntimeSideInspectionCorrection(
                        target.PickerIndex,
                        sideVisionProcess0YOffset,
                        sideVisionProcess90YOffset,
                        pickerZOffset,
                        valid,
                        sourceDieId);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 결과 확인. Side 위치 보정에는 적용하지 않습니다(Bottom/Side AF 간섭 금지). " +
                "side=" + Side +
                ", pickerNo=" + target.PickerNo +
                ", die=" + sourceDieId +
                ", bottomInspectionOk=" + result.IsOk +
                ", valid=" + valid +
                ", appliedSideVisionY0Offset=" + sideVisionProcess0YOffset.ToString("F6") +
                ", appliedSideVisionY90Offset=" + sideVisionProcess90YOffset.ToString("F6") +
                ", PickerZ.offset=" + pickerZOffset.ToString("F6") +
                ", bottomCenterOffsetX(참고)=" + result.BottomCenterOffsetX.ToString("F6") +
                ", bottomCenterOffsetY(참고)=" + result.BottomCenterOffsetY.ToString("F6") +
                ", bottomOffsetT=" + result.OffsetT.ToString("F6") + " - Ok");
        }

        private int ValidateRuntimeSideInspectionCorrection(int pickerIndex)
        {
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
            {
                return Fail("PICKER-BOTTOM-SIDE-CENTER-OFFSET-DIE", Name,
                    "Bottom Center Offset 확인 실패. 해당 Picker의 제품 정보를 찾을 수 없습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ".");
            }

            PickerSideInspectionCorrection correction = ResolveRuntimeSideInspectionCorrection(pickerIndex);
            if (correction == null || !correction.IsValid)
            {
                return Fail("PICKER-BOTTOM-SIDE-CENTER-OFFSET-MISSING", "Vision",
                    "Side 검사에 필요한 Bottom Center X/Y Offset(mm)이 없습니다. " +
                    "Bottom 결과의 bottom_offset_x_mm/bottom_offset_y_mm 수신 상태를 확인하세요. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + die.DieId + ".");
            }

            if (string.IsNullOrWhiteSpace(die.DieId) ||
                string.IsNullOrWhiteSpace(correction.SourceDieId) ||
                !string.Equals(correction.SourceDieId, die.DieId, StringComparison.Ordinal))
            {
                return Fail("PICKER-BOTTOM-SIDE-CENTER-OFFSET-DIE-MISMATCH", "Vision",
                    "Bottom Center Offset의 제품과 Side 검사 제품이 일치하지 않습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", sideDie=" + die.DieId +
                    ", bottomResultDie=" + (correction.SourceDieId ?? string.Empty) + ".");
            }

            if (!IsValidSideVisionCenterCorrection(correction.SideVisionProcess0YOffset) ||
                !IsValidSideVisionCenterCorrection(correction.SideVisionProcess90YOffset))
            {
                return Fail("PICKER-BOTTOM-SIDE-CENTER-OFFSET-RANGE", "Vision",
                    "Bottom Center Offset이 SideVision 보정 허용범위를 벗어났습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + die.DieId +
                    ", offsetXFor0Deg=" + correction.SideVisionProcess0YOffset.ToString("F6") +
                    ", offsetYFor90Deg=" + correction.SideVisionProcess90YOffset.ToString("F6") +
                    ", allowedAbsMaxMm=" + MaxSideVisionCenterCorrectionMm.ToString("F3") + ".");
            }

            return 0;
        }

        private static bool IsFiniteCorrectionValue(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidSideVisionCenterCorrection(double value)
        {
            return IsFiniteCorrectionValue(value) && Math.Abs(value) <= MaxSideVisionCenterCorrectionMm;
        }

        private async Task<int> RunSidePipelineAsync(CancellationToken ct)
        {
            if (!_sidePipelineEnabled)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side Pipeline 비활성 상태입니다. 보유 Picker가 없을 때만 허용됩니다. " +
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
                        Name + " 현재 실행에서 이미 완료된 Side 대상은 재검사를 생략합니다. " +
                        "pickerNo=" + ToPickerNo(pickerIndex) + " - Check");
                    continue;
                }

                int result = await EnsureBottomReadyForSideAsync(pickerIndex, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                InspectionTarget target = BuildSideTarget(pickerIndex);
                if (target == null || target.Die == null)
                {
                    return Fail("PICKER-BOTTOM-SIDE-SIDE-TARGET", Name,
                        "Side 검사 목표 생성 실패. Side 목표 좌표 또는 Picker 제품 상태를 확인하세요. " +
                        "pickerNo=" + ToPickerNo(pickerIndex) +
                        ", loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".");
                }

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
            bool correctionValid = correction != null &&
                                   correction.IsValid &&
                                   IsValidSideVisionCenterCorrection(correction.SideVisionProcess0YOffset) &&
                                   IsValidSideVisionCenterCorrection(correction.SideVisionProcess90YOffset) &&
                                   !string.IsNullOrWhiteSpace(die.DieId) &&
                                   !string.IsNullOrWhiteSpace(correction.SourceDieId) &&
                                   string.Equals(correction.SourceDieId, die.DieId, StringComparison.Ordinal);
            double baseZ = ResolveSidePickerZBase(pickerIndex, pickerNo);
            double zOffset = 0.0;
            double sideVisionProcess0YOffset = correctionValid ? correction.SideVisionProcess0YOffset : 0.0;
            double sideVisionProcess90YOffset = correctionValid ? correction.SideVisionProcess90YOffset : 0.0;
            SideVisionPositionTarget frontSideVision = BuildSideVisionPositionTarget(
                PickerSequenceSide.Front,
                pickerNo,
                sideVisionProcess0YOffset,
                sideVisionProcess90YOffset);
            SideVisionPositionTarget rearSideVision = BuildSideVisionPositionTarget(
                PickerSequenceSide.Rear,
                pickerNo,
                sideVisionProcess0YOffset,
                sideVisionProcess90YOffset);
            double t0 = ResolvePickerZoneT("DieSidePosition", pickerIndex);
            double sideTeachingX = ResolvePickerZoneX("DieSidePosition", pickerIndex);
            double sideTeachingY = ResolvePickerZoneY("DieSidePosition", pickerIndex);
            double targetX = sideTeachingX;
            double targetY = sideTeachingY;
            int bottomReferencePickerNo;
            bool bottomReferenceStored;
            if (!TryResolveBottomReferenceForSideTarget(
                pickerIndex,
                out targetX,
                out targetY,
                out bottomReferencePickerNo,
                out bottomReferenceStored))
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side X/Y 목표를 Bottom 공정 기준으로 확정할 수 없습니다. " +
                    "side=" + Side +
                    ", sidePickerNo=" + pickerNo +
                    ", bottomReferencePickerNo=" + bottomReferencePickerNo +
                    ", sideTeachingXNotUsed=" + sideTeachingX.ToString("0.###") +
                    ", sideTeachingYNotUsed=" + sideTeachingY.ToString("0.###") +
                    ", expectedYDirection=" + ResolveProcessYDirectionName() + " - Failed");
                return null;
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side X/Y 목표를 Bottom 공정 X/Y 기준으로 적용합니다. " +
                "side=" + Side +
                ", sidePickerNo=" + pickerNo +
                ", bottomReferencePickerNo=" + bottomReferencePickerNo +
                ", targetX=" + targetX.ToString("0.###") +
                ", targetY=" + targetY.ToString("0.###") +
                ", sideTeachingXIgnored=" + sideTeachingX.ToString("0.###") +
                ", sideTeachingYIgnored=" + sideTeachingY.ToString("0.###") +
                ", mapping=BottomReference" +
                ", yProcessDirection=" + ResolveProcessYDirectionName() +
                ", source=" + (bottomReferenceStored ? "RuntimeBottomTarget" : "PrecalculatedBottomPlan") + " - Check");

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
                SideCorrectionValid = correctionValid,
                SideVisionProcess0YOffset = sideVisionProcess0YOffset,
                SideVisionProcess90YOffset = sideVisionProcess90YOffset,
                SidePickerZBase = baseZ,
                SidePickerZOffset = zOffset,
                FrontSideVision = frontSideVision,
                RearSideVision = rearSideVision,
                SideCorrectionSourceDieId = correction != null ? correction.SourceDieId : string.Empty
            };
        }

        private SideVisionPositionTarget BuildSideVisionPositionTarget(
            PickerSequenceSide cameraSide,
            int pickerNo,
            double process0Offset,
            double process90Offset)
        {
            bool focus0Valid;
            bool focus90Valid;
            double process0BaseY = ResolveSideVisionBasePosition(cameraSide, 0, pickerNo, out focus0Valid);
            double process90BaseY = ResolveSideVisionBasePosition(cameraSide, 90, pickerNo, out focus90Valid);
            return new SideVisionPositionTarget
            {
                CameraSide = cameraSide,
                Axis = ResolveSideVisionAxis(cameraSide),
                Process0BaseY = process0BaseY,
                Process90BaseY = process90BaseY,
                Process0Y = process0BaseY + process0Offset,
                Process90Y = process90BaseY + process90Offset,
                Focus0CalibrationValid = focus0Valid,
                Focus90CalibrationValid = focus90Valid
            };
        }

        private static VisionAxis ResolveSideVisionAxis(PickerSequenceSide cameraSide)
        {
            return cameraSide == PickerSequenceSide.Front
                ? VisionAxis.FrontSideVisionY
                : VisionAxis.RearSideVisionY;
        }

        private double ResolveSidePickerZBase(int pickerIndex, int pickerNo)
        {
            return ResolveSideInspectionPickerZFromBottomBest(
                pickerIndex,
                pickerNo,
                "PickerBottomAndSideInspectionSequence");
        }

        private double ResolveSideVisionBasePosition(
            PickerSequenceSide cameraSide,
            int angleDeg,
            int pickerNo,
            out bool focusCalibrationValid)
        {
            focusCalibrationValid = false;
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return 0.0;

                // Side 0/90도 저장 AF가 없을 때는 공통 Process0 티칭 위치를 기준으로 사용한다.
                VisionAxis axis = ResolveSideVisionAxis(cameraSide);
                double teachingY = vision.GetVisionTeachingPosition(axis, "Process0Position");
                VisionFocusCalibrationData focusData = vision.Config != null ? vision.Config.FocusCalibration : null;
                if (focusData == null)
                    return teachingY;

                focusData.EnsureObjects();
                VisionFocusScanKind kind;
                if (cameraSide == PickerSequenceSide.Front)
                    kind = angleDeg == 90 ? VisionFocusScanKind.FrontSide90 : VisionFocusScanKind.FrontSide0;
                else
                    kind = angleDeg == 90 ? VisionFocusScanKind.RearSide90 : VisionFocusScanKind.RearSide0;

                VisionFocusPositionRecord record = focusData.GetSideRecord(kind, pickerNo);
                if (record == null || !record.Valid ||
                    double.IsNaN(record.BestPosition) || double.IsInfinity(record.BestPosition))
                {
                    double fallbackCorrection;
                    if (!TryResolveSideFocusFallbackCorrection(cameraSide, angleDeg, pickerNo, focusData, out fallbackCorrection))
                        return teachingY;

                    double axisSign = cameraSide == PickerSequenceSide.Front ? 1.0 : -1.0;
                    double fallbackY = teachingY + axisSign * fallbackCorrection;
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Side AF 저장값이 없어 Process0+COC/DieSize 폴백을 적용합니다. " +
                        "pickerSide=" + Side +
                        ", cameraSide=" + cameraSide +
                        ", axis=" + axis +
                        ", pickerNo=" + pickerNo +
                        ", angle=" + angleDeg +
                        ", process0Y=" + teachingY.ToString("F6") +
                        ", axisSign=" + axisSign.ToString("F1") +
                        ", correction=" + fallbackCorrection.ToString("F6") +
                        ", fallbackY=" + fallbackY.ToString("F6") + " - Check");
                    return fallbackY;
                }

                focusCalibrationValid = true;
                return record.BestPosition;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private bool TryResolveSideFocusFallbackCorrection(
            PickerSequenceSide cameraSide,
            int angleDeg,
            int pickerNo,
            VisionFocusCalibrationData focusData,
            out double correction)
        {
            correction = 0.0;
            try
            {
                if (Context == null || Context.Machine == null || Context.Machine.VisionUnit == null ||
                    Context.Machine.VisionUnit.Config == null ||
                    Context.Machine.VisionUnit.Config.CalibrationData == null)
                    return false;

                VisionFocusPickerSide focusSide = Side == PickerSequenceSide.Front
                    ? VisionFocusPickerSide.Front
                    : VisionFocusPickerSide.Rear;
                ColletCalibrationData colletData = Context.Machine.VisionUnit.Config.CalibrationData.Collet;
                if (colletData == null)
                    return false;

                ColletCalibrationRecord record = colletData.GetRecord(focusSide, pickerNo);
                if (record == null || !record.RotationCenterValid)
                    return false;

                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                    Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                    QMC.CDT320.VisionComm.AutoVisionChannel.BottomInspection);
                double cocXmm = camera.PixelToMmOffsetX(record.RotationCenterPixelX);
                double cocYmm = camera.PixelToMmOffsetY(record.RotationCenterPixelY);
                if (double.IsNaN(cocXmm) || double.IsInfinity(cocXmm) ||
                    double.IsNaN(cocYmm) || double.IsInfinity(cocYmm))
                    return false;

                bool frontCamera = cameraSide == PickerSequenceSide.Front;
                if (angleDeg != 90)
                {
                    double coc0Sign = frontCamera ? focusData.SideFocusCoc0SignFront : focusData.SideFocusCoc0SignRear;
                    correction = coc0Sign * cocYmm;
                    return true;
                }

                double dieSizeX = 0.0;
                double dieSizeY = 0.0;
                QMC.CDT320.Recipes.RecipeProject recipe = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                QMC.CDT320.Recipes.TapeFrameSubset frame = recipe != null
                    ? (recipe.InputFrame ?? recipe.Frame)
                    : null;
                if (frame != null)
                {
                    dieSizeX = frame.DieSizeX;
                    dieSizeY = frame.DieSizeY;
                }
                if ((dieSizeX <= 0.0 || dieSizeY <= 0.0) && Context.Controller != null)
                {
                    dieSizeX = Context.Controller.DieSizeXMm;
                    dieSizeY = Context.Controller.DieSizeYMm;
                }
                if (dieSizeX <= 0.0 || dieSizeY <= 0.0)
                    return false;

                double sizeTerm90 = (dieSizeX - dieSizeY) / 2.0;
                double size90Sign = frontCamera ? focusData.SideFocusSize90SignFront : focusData.SideFocusSize90SignRear;
                double coc90Sign = frontCamera ? focusData.SideFocusCoc90SignFront : focusData.SideFocusCoc90SignRear;
                correction = size90Sign * sizeTerm90 + coc90Sign * cocXmm;
                return true;
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side Process0+COC/DieSize 폴백 계산 중 예외가 발생했습니다. " +
                    "pickerSide=" + Side + ", cameraSide=" + cameraSide +
                    ", pickerNo=" + pickerNo + ", angle=" + angleDeg +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
        }

        private SideVisionPositionTarget ResolveSideVisionPositionTarget(
            InspectionTarget target,
            PickerSequenceSide cameraSide)
        {
            if (target == null)
                return null;

            return cameraSide == PickerSequenceSide.Front
                ? target.FrontSideVision
                : target.RearSideVision;
        }

        private double ResolveSideVisionTargetY(
            InspectionTarget target,
            PickerSequenceSide cameraSide,
            int angleDeg)
        {
            SideVisionPositionTarget cameraTarget = ResolveSideVisionPositionTarget(target, cameraSide);
            if (cameraTarget == null)
            {
                bool focusCalibrationValid;
                return ResolveSideVisionBasePosition(cameraSide, angleDeg, 1, out focusCalibrationValid);
            }

            return angleDeg == 90 ? cameraTarget.Process90Y : cameraTarget.Process0Y;
        }

        private int PrepareAutoFirstSideTargetForLastBottom(
            InspectionTarget lastBottomTarget,
            out InspectionTarget sideTarget)
        {
            sideTarget = null;

            try
            {
                if (Options == null || Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (lastBottomTarget == null || _pickedPickerIndexes.Count == 0)
                    return 0;

                if (!_sidePipelineEnabled)
                    return 0;

                if (!_parallelFirstSideEnabled)
                    return 0;

                if (lastBottomTarget.PickerNo != 1)
                {
                    return Fail("PICKER-BOTTOM-SIDE-PARALLEL-TARGET", Name,
                        "4개 풀 배치 Bottom1/Side4 병렬 검사 준비 실패. " +
                        "Bottom 마지막 대상이 pickerNo=1이 아닙니다. " +
                        "lastBottomPickerNo=" + lastBottomTarget.PickerNo +
                        ", loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".");
                }

                int sideFirstPickerIndex = ToPickerIndex(4);
                if (!_pickedPickerIndexes.Contains(sideFirstPickerIndex))
                {
                    return Fail("PICKER-BOTTOM-SIDE-PARALLEL-TARGET", Name,
                        "4개 풀 배치 Bottom1/Side4 병렬 검사 준비 실패. " +
                        "pickerNo=4에 제품이 없습니다. " +
                        "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".");
                }

                if (_sideCompletedPickerIndexes.Contains(sideFirstPickerIndex))
                    return 0;

                sideTarget = BuildSideTarget(sideFirstPickerIndex);
                if (sideTarget == null || sideTarget.Die == null)
                {
                    return Fail("PICKER-BOTTOM-SIDE-PARALLEL-TARGET", Name,
                        "4개 풀 배치 Bottom1/Side4 병렬 검사 목표 생성 실패. " +
                        "sidePickerNo=4, loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + ".");
                }

                double preparedSideX = sideTarget.X;
                double preparedSideY = sideTarget.Y;
                sideTarget.X = lastBottomTarget.X;
                sideTarget.Y = lastBottomTarget.Y;

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Auto Bottom 마지막 검사와 Side 첫 검사를 같은 XY 위치에서 동시 진행하도록 공유 위치를 적용합니다. " +
                    "bottomPickerNo=" + lastBottomTarget.PickerNo +
                    ", sidePickerNo=" + sideTarget.PickerNo +
                    ", sharedX=" + sideTarget.X.ToString("0.###") +
                    ", sharedY=" + sideTarget.Y.ToString("0.###") +
                    ", preparedSideX=" + preparedSideX.ToString("0.###") +
                    ", preparedSideY=" + preparedSideY.ToString("0.###") + " - Check");

                return 0;
            }
            catch (Exception ex)
            {
                sideTarget = null;
                return Fail("PICKER-BOTTOM-SIDE-PARALLEL-TARGET", Name,
                    "Auto Bottom1/Side4 병렬 검사 공유 위치 준비 중 예외가 발생했습니다. error=" + ex.Message);
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
            // Bottom 외곽 XYT+W/H 푸시(EventSearchDieEnd) 도착 게이트(2026-07-12 확정 정책):
            // 데이터가 도착하지 않았으면 Side 촬영을 시작하지 않는다 — 도착까지 대기, 타임아웃 시 알람 정지.
            // 참고(병합): 조기 푸시의 X/Y는 px 진단용이며, 실제 Side 위치 보정은 Bottom 완료 결과의
            // mm Center X/Y 를 사용한다(보정 값 자체는 InspectionTarget 구성 시 반영).
            int gate = await WaitBottomXytForSideAsync(target, ct).ConfigureAwait(false);
            if (gate != 0)
                return gate;
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
                            "Side 0도 검사 INSPECTRESULT 미수신");

                        side0TactScope.Fail("PICKER-BOTTOM-SIDE-SIDE0-RESULT", BuildTactDetail(target, "Side 0deg inspection result receive failed."));
                        return Fail("PICKER-BOTTOM-SIDE-SIDE0-RESULT", "Vision", "Side 0도 검사 시작 실패. die=" + target.Die.DieId + ", pickerNo=" + target.PickerNo);
                    }

                    // Side 0도는 EPD 수신 후 진행하고, 최종 결과는 90도 검사 후 통합 INSPECTRESULT로 받는다.
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
                            "Side 90도 검사 EPD 미수신");

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
                            "Side 검사 INSPECTRESULT 미수신");

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

                double correctionYOffset = angleDeg == 90
                    ? target.SideVisionProcess90YOffset
                    : target.SideVisionProcess0YOffset;
                string correctionSource = angleDeg == 90 ? "BottomCenterY" : "BottomCenterX";

                Task<int> frontTask = MoveSideVisionAxisProcessPositionAsync(
                    vision, target, target.FrontSideVision, angleDeg,
                    correctionYOffset, correctionSource);
                Task<int> rearTask = MoveSideVisionAxisProcessPositionAsync(
                    vision, target, target.RearSideVision, angleDeg,
                    correctionYOffset, correctionSource);

                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (results.Length != 2 || results[0] != 0 || results[1] != 0)
                {
                    return Fail("PICKER-BOTTOM-SIDE-VISION-POSITION", "Vision",
                        "Side 검사 양쪽 카메라 " + angleDeg + "도 보정 위치 이동 실패. " +
                        "frontResult=" + (results.Length > 0 ? results[0] : -1) +
                        ", rearResult=" + (results.Length > 1 ? results[1] : -1) +
                        ", pickerSide=" + Side +
                        ", pickerNo=" + target.PickerNo +
                        ", frontTargetY=" + ResolveSideVisionTargetY(target, PickerSequenceSide.Front, angleDeg).ToString("F6") +
                        ", rearTargetY=" + ResolveSideVisionTargetY(target, PickerSequenceSide.Rear, angleDeg).ToString("F6"));
                }

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

        private async Task<int> MoveSideVisionAxisProcessPositionAsync(
            VisionUnit vision,
            InspectionTarget target,
            SideVisionPositionTarget cameraTarget,
            int angleDeg,
            double correctionYOffset,
            string correctionSource)
        {
            if (vision == null || target == null || cameraTarget == null)
                return -1;

            double baseY = vision.GetVisionTeachingPosition(cameraTarget.Axis, "Process0Position");
            double focusCalBaseY = angleDeg == 90 ? cameraTarget.Process90BaseY : cameraTarget.Process0BaseY;
            bool focusCalValid = angleDeg == 90
                ? cameraTarget.Focus90CalibrationValid
                : cameraTarget.Focus0CalibrationValid;
            double targetY = angleDeg == 90 ? cameraTarget.Process90Y : cameraTarget.Process0Y;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " SideVisionY 보정 이동. " +
                "pickerSide=" + Side +
                ", cameraSide=" + cameraTarget.CameraSide +
                ", axis=" + cameraTarget.Axis +
                ", pickerNo=" + target.PickerNo +
                ", die=" + target.Die.DieId +
                ", angle=" + angleDeg +
                ", process0TeachingY=" + baseY.ToString("F6") +
                ", focusCalBaseY=" + focusCalBaseY.ToString("F6") +
                ", focusCalValid=" + focusCalValid +
                ", offsetY=" + correctionYOffset.ToString("F6") +
                ", offsetSource=" + correctionSource +
                ", finalY=" + targetY.ToString("F6") +
                ", correctionValid=" + target.SideCorrectionValid +
                ", sourceDie=" + (target.SideCorrectionSourceDieId ?? string.Empty) + " - Start");

            return await vision.MoveVisionAxis(
                cameraTarget.Axis,
                targetY,
                Options != null && Options.FineMove).ConfigureAwait(false);
        }

        private bool IsSideVisionProcessPositionReady(InspectionTarget target, int angleDeg)
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return false;

                return IsSideVisionAxisProcessPositionReady(
                           vision, target, target.FrontSideVision, angleDeg) &&
                       IsSideVisionAxisProcessPositionReady(
                           vision, target, target.RearSideVision, angleDeg);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsSideVisionAxisProcessPositionReady(
            VisionUnit vision,
            InspectionTarget target,
            SideVisionPositionTarget cameraTarget,
            int angleDeg)
        {
            if (vision == null || target == null || cameraTarget == null)
                return false;

            BaseAxis item = vision.ResolveVisionAxis(cameraTarget.Axis);
            double tolerance = item != null && item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            double targetY = angleDeg == 90 ? cameraTarget.Process90Y : cameraTarget.Process0Y;
            return vision.IsVisionAxisInPosition(cameraTarget.Axis, targetY, tolerance);
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
                Name + " Side 검사 EPD 수신 이후 결과 수신 대기를 생략하고 진행합니다. " +
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
                Name + " Side 검사 EPD 수신 후 Vision 백그라운드 결과 대기 상태로 진행합니다. " +
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

            bool frontConnected = IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel.FrontSide);
            bool rearConnected = IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel.RearSide);
            if (!frontConnected || !rearConnected)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " DryRun Side GRAB skipped. 양쪽 Side Vision 연결이 완료되지 않았습니다. " +
                    "frontConnected=" + frontConnected +
                    ", rearConnected=" + rearConnected +
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
                ". channels=FrontSide,RearSide" +
                ", die=" + (target != null && target.Die != null ? target.Die.DieId : "") +
                ", pickerNo=" + (target != null ? target.PickerNo : 0) +
                ", angleDeg=" + angleDeg +
                ", timeoutMs=" + timeoutMs + " - Check");
            return true;
        }

        private bool IsDryRunMode()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null &&
                   settings.DryRunMode &&
                   !QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive();
        }

        // 시뮬/드라이런 합성 비전에서는 Bottom XYT push가 없을 수 있으므로 Side 게이트만 통과시킨다.
        // Simulation 실제 Vision 사용 시에는 실장비와 동일하게 Bottom XYT push를 진행 조건으로 사용한다.
        private bool ShouldBypassBottomXytGateForSimulation()
        {
            if (QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
                return false;

            if (Options != null && Options.SimulateVisionResult)
                return true;

            AppSettings settings = AppSettingsStore.Current;
            if (settings != null &&
                (settings.SimulationMode ||
                 settings.DryRunMode ||
                 settings.BypassHardware ||
                 !settings.UseVision ||
                 !settings.UseAjin))
                return true;

            return IsPickerSimulationOrDryRun();
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

        /// <summary>Side 촬영 전 Bottom XYT+W/H 푸시 도착 게이트(2026-07-12 확정 정책).
        /// <para>Bottom 외곽 확정 즉시 오는 푸시가 이 콜렛에 도착할 때까지 대기한다 — 미도착 상태로
        /// 촬영을 시작하지 않는다. 배치 흐름상 Bottom 촬영 → Side 진입 순서라 (fb, collet) 최신 1건 =
        /// 현재 다이의 푸시다. 타임아웃(비전 검사 타임아웃과 동일) 시 알람 정지 — 촬영 미시작.
        /// valid=0(외곽 미검출)은 '도착'으로 간주하고 진행한다(보정 0 적용, 기존 미검출 정책과 동일).</para>
        /// <para>병합 주(3d0788e7): 푸시의 X/Y는 px 진단용 — 실제 Side 위치 보정은 Bottom 완료 결과의
        /// mm Center X/Y 를 사용한다(InspectionTarget 구성 시 반영). 이 게이트는 도착 보장 + W/H(mm) 확보용.</para></summary>
        private async Task<int> WaitBottomXytForSideAsync(InspectionTarget target, CancellationToken ct)
        {
            if (target == null)
                return 0;

            int fb = Side == PickerSequenceSide.Front ? 0 : 1;
            int timeoutMs = ResolveVisionInspectionTimeout();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (ShouldBypassBottomXytGateForSimulation())
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side 진입 — 시뮬/드라이런 조건으로 Bottom XYT push 게이트를 통과합니다. " +
                    "실장비/비전 사용 조건에서는 push 미도착 시 알람 정지합니다. " +
                    "die=" + (target.Die != null ? target.Die.DieId : "-") +
                    ", fb=" + fb +
                    ", collet=" + target.PickerNo +
                    ", timeoutMs=" + timeoutMs + " - Check");
                return 0;
            }

            QMC.CDT320.VisionComm.BottomXytPush xyt;
            while (!QMC.CDT320.VisionComm.BottomXytStore.TryGet(fb, target.PickerNo, out xyt))
            {
                ct.ThrowIfCancellationRequested();
                if (sw.ElapsedMilliseconds >= timeoutMs)
                {
                    return Fail("PICKER-SIDE-XYT-TIMEOUT", "Vision",
                        "Side 촬영 전 Bottom XYT 푸시 미도착 — 촬영을 시작하지 않습니다. " +
                        "die=" + (target.Die != null ? target.Die.DieId : "-") +
                        ", fb=" + fb +
                        ", collet=" + target.PickerNo +
                        ", timeoutMs=" + timeoutMs);
                }
                await Task.Delay(5, ct).ConfigureAwait(false);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 진입 — Bottom XYT 푸시 확인(게이트 통과, xPx/yPx=진단용·모션 미사용). die=" + (target.Die != null ? target.Die.DieId : "-") +
                ", fb=" + fb +
                ", collet=" + target.PickerNo +
                ", dieIndex=" + xyt.DieIndex +
                ", xPx=" + xyt.X.ToString("F3") +
                ", yPx=" + xyt.Y.ToString("F3") +
                ", t=" + xyt.T.ToString("F4") +
                ", w=" + xyt.W.ToString("F4") + "mm" +
                ", h=" + xyt.H.ToString("F4") + "mm" +
                ", valid=" + xyt.IsValid +
                ", waited=" + sw.ElapsedMilliseconds + "ms" +
                ", age=" + (DateTime.Now - xyt.ReceivedAt).TotalMilliseconds.ToString("F0") + "ms - Check");
            return 0;
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
                " | FrontSideVisionY0.base=" + target.FrontSideVision.Process0BaseY.ToString("F6") +
                ", FrontSideVisionY0.offset=" + target.SideVisionProcess0YOffset.ToString("F6") +
                ", FrontSideVisionY0.final=" + target.FrontSideVision.Process0Y.ToString("F6") +
                " | FrontSideVisionY90.base=" + target.FrontSideVision.Process90BaseY.ToString("F6") +
                ", FrontSideVisionY90.offset=" + target.SideVisionProcess90YOffset.ToString("F6") +
                ", FrontSideVisionY90.final=" + target.FrontSideVision.Process90Y.ToString("F6") +
                " | RearSideVisionY0.base=" + target.RearSideVision.Process0BaseY.ToString("F6") +
                ", RearSideVisionY0.offset=" + target.SideVisionProcess0YOffset.ToString("F6") +
                ", RearSideVisionY0.final=" + target.RearSideVision.Process0Y.ToString("F6") +
                " | RearSideVisionY90.base=" + target.RearSideVision.Process90BaseY.ToString("F6") +
                ", RearSideVisionY90.offset=" + target.SideVisionProcess90YOffset.ToString("F6") +
                ", RearSideVisionY90.final=" + target.RearSideVision.Process90Y.ToString("F6") +
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
