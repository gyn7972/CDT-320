using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.VisionComm;
using QMC.Common.Diagnostics.TactTime;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomAndSideInspectionSequence : PickerSequenceBase<PickerBottomAndSideInspectionStep>
    {
        private const double MaxSideVisionCenterCorrectionMm = 7.0;
        private const double DefaultBottomPlaceCorrectionLimitMm = 1.0;

        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private readonly List<BottomShot> _pendingBottomShots = new List<BottomShot>();
        private readonly List<int> _sideReadyPickerIndexes = new List<int>();
        private readonly List<InspectionTarget> _pendingSideResults = new List<InspectionTarget>();
        private readonly List<int> _sideCapturedPickerIndexes = new List<int>();
        private readonly List<int> _sideCompletedPickerIndexes = new List<int>();
        private readonly Dictionary<int, Task<SideVisionResult>> _sideFinalResultTasks = new Dictionary<int, Task<SideVisionResult>>();
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();
        private readonly List<PendingZAvoid> _pendingZAvoids = new List<PendingZAvoid>();
        private readonly List<PendingBottomZDown> _pendingBottomZDowns = new List<PendingBottomZDown>();
        private readonly Dictionary<int, SideTargetPosition> _bottomReferencePositions = new Dictionary<int, SideTargetPosition>();
        private readonly Dictionary<int, SideTargetPosition> _sideTargetPositions = new Dictionary<int, SideTargetPosition>();

        private bool _bottomInspectionYReady;
        private bool _sidePipelineEnabled;
        private bool _parallelFirstSideEnabled;
        private bool _inspectionFixedYConfigured;
        private double _inspectionFixedY;
        private CancellationTokenSource _deferredResultCancellation;
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
            // Bottom 촬영 명령 시점의 PickerY CommandPosition (Pick 런타임 보정 Y 전처리용 캡처값).
            public double BottomShotPickerYCommand;
            public bool SideCorrectionValid;
            public double SideBottomOffsetXmm;
            public double SideBottomOffsetYmm;
            public double SidePickerZBase;
            public double SidePickerZOffset;
            public SideVisionPositionTarget FrontSideVision;
            public SideVisionPositionTarget RearSideVision;
            public string SideCorrectionSourceDieId;
            // Side 비전 Y 절대식 계산 결과(항별 값 로그·이동에 공용 사용).
            public SideVisionYTargetResult SideVisionCalc;
        }

        private sealed class SideVisionPositionTarget
        {
            public PickerSequenceSide CameraSide;
            public VisionAxis Axis;
            public double Process0Y;
            public double Process90Y;
        }

        private sealed class BottomShot
        {
            public InspectionTarget Target;
            public bool MResultApplied;
            public bool FinalResultApplied;
            public BottomVisionOffset MResult;
            public Task<int> MResultTask;
            public Task<BottomVisionOffset> FinalResultTask;
            public BottomVisionOffset ValidatedPlaceResult;
            public double ValidatedPlaceCorrectionLimitMm;
            public bool FinalGateTactRecorded;
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
            // Z+T 선행(사용자 승인 2026-07-26): Z만 선행하던 구조에 T 회전을 추가한다.
            // HasT=false면 기존 Z 단독 선행과 완전 동일 동작.
            public bool HasT;
            public double TTarget;
            public Task<int> TMoveTask;
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

        public bool HasPendingFinalResults
        {
            get
            {
                for (int i = 0; i < _pendingBottomShots.Count; i++)
                {
                    if (!_pendingBottomShots[i].FinalResultApplied)
                        return true;
                }

                return _sideCompletedPickerIndexes.Count < _pendingSideResults.Count;
            }
        }

        public bool ForceBottomInspectionBeforeSideResume { get; set; }

        public bool PickerZStageSafeConfirmedByPickUp { get; set; }

        public void Abort()
        {
            try
            {
                CancelDeferredFinalResultTasks();
                AbandonPendingVisionInspections("Bottom/Side 통합 검사 Abort로 미완료 요청을 정리합니다.");
                ReleaseInspectionArea();
                ReleaseBottomSideProcessArea();
                _pendingBottomShots.Clear();
                _sideReadyPickerIndexes.Clear();
                _pendingSideResults.Clear();
                _sideFinalResultTasks.Clear();
                _sideCapturedPickerIndexes.Clear();
                _sideCompletedPickerIndexes.Clear();
                _pendingT0Returns.Clear();
                _pendingZAvoids.Clear();
                _pendingBottomZDowns.Clear();
                _bottomReferencePositions.Clear();
                _sideTargetPositions.Clear();
                _sidePipelineEnabled = false;
                _parallelFirstSideEnabled = false;
                _inspectionFixedYConfigured = false;
                _inspectionFixedY = 0.0;
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
                if (CurrentStep != PickerBottomAndSideInspectionStep.Complete)
                {
                    try
                    {
                        CancelDeferredFinalResultTasks();
                        await DrainCapturedSideResultsAfterFailureAsync(ct).ConfigureAwait(false);
                        AbandonPendingVisionInspections("Bottom/Side 통합 검사 실패/중단으로 미완료 요청을 정리합니다.");
                    }
                    catch (Exception ex)
                    {
                        WriteLog("PickerBottomAndSideInspectionSequence",
                            Name + " 실패 후 Vision Handle 정리 예외. error=" + ex.Message + " - Check");
                    }
                }
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
            CancelDeferredFinalResultTasks();
            _pickedPickerIndexes.Clear();
            _pendingBottomShots.Clear();
            _sideReadyPickerIndexes.Clear();
            _pendingSideResults.Clear();
            _sideFinalResultTasks.Clear();
            _sideCapturedPickerIndexes.Clear();
            _sideCompletedPickerIndexes.Clear();
            _pendingT0Returns.Clear();
            _pendingZAvoids.Clear();
            _pendingBottomZDowns.Clear();
            _bottomReferencePositions.Clear();
            _sideTargetPositions.Clear();
            _bottomInspectionYReady = false;
            _sidePipelineEnabled = false;
            _parallelFirstSideEnabled = false;
            _inspectionFixedYConfigured = false;
            _inspectionFixedY = 0.0;

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

            _sidePipelineEnabled = true;
            // 기존 조건: 특수 Bottom P1+Side P4 오버랩은 하드코딩 false로 잠겨 있었다.
            // 현재 기준(사용자 승인 2026-07-28): 유닛 Config(ParallelFirstSideOverlap, 기본 Off)로
            //   노출 — 레시피 화면 BOTTOM MOTION SETTING 그룹에서 켜고 끈다.
            _parallelFirstSideEnabled = ResolveBottomInspectionMotionConfig().ParallelFirstSideOverlap;

            // 기존 조건: 고정 PickerY 기준을 "보유 중 최상위 피커"(_pickedPickerIndexes[0])의
            //   DieBottomPosition Y로 잡았다 — 보유 조합에 따라 기준이 바뀌어, 같은 다이라도
            //   4개 배치(P4 기준)와 1개 배치(P1/P2 기준)에서 촬영 Y가 달라졌다
            //   (실측 2026-07-28: Front P4=36.264 / P1=36.497 → 0.233mm, Rear P2=-32.449 → 0.550mm).
            // 현재 기준(사용자 승인 2026-07-28): 보유 조합과 무관하게 항상 P4의 Y를 기준으로 고정한다.
            //   PickerY는 4헤드 공용 축이므로 P4 미보유·비활성 조합에서도 좌표 자체는 유효하다.
            int firstPickerIndex = _pickedPickerIndexes[0];
            int fixedYReferenceIndex = ToPickerIndex(4);
            _inspectionFixedY = ResolvePickerZoneY("DieBottomPosition", fixedYReferenceIndex);
            if (!IsExpectedProcessYDirection(_inspectionFixedY))
            {
                return Fail("PICKER-BOTTOM-SIDE-FIXED-Y", Name,
                    "Bottom P4부터 Side 종료까지 유지할 PickerY 기준값이 올바르지 않습니다. " +
                    "side=" + Side +
                    ", fixedYSource=P" + ToPickerNo(fixedYReferenceIndex) + "(고정)" +
                    ", firstPickerNo=" + ToPickerNo(firstPickerIndex) +
                    ", fixedY=" + _inspectionFixedY.ToString("F6") +
                    ", expectedDirection=" + ResolveProcessYDirectionName() + ".");
            }
            _inspectionFixedYConfigured = true;

            int planResult = BuildSideTargetPlan();
            if (planResult != 0)
                return planResult;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom P4 진입 시 확정한 PickerY를 Side 종료까지 고정합니다. " +
                "Bottom은 EPD까지만 받고, Side 0도 직전에 해당 Picker MRESULT를 확인합니다. " +
                "loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) +
                ", fixedY=" + _inspectionFixedY.ToString("F6") +
                ", fixedYSource=P" + ToPickerNo(fixedYReferenceIndex) + "(고정, 보유 조합 무관)" +
                ", firstPickerNo=" + ToPickerNo(firstPickerIndex) +
                ", specialBottomP1SideP4Overlap=" + _parallelFirstSideEnabled + " - Check");

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

        private bool HasValidFourPickerOverlapContext(out string reason)
        {
            reason = string.Empty;
            if (_pickedPickerIndexes.Count != 4)
            {
                reason = "loadedPickerCount=" + _pickedPickerIndexes.Count;
                return false;
            }

            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                string pickerReason;
                if (!HasValidAutoVisionContext(_pickedPickerIndexes[i], out pickerReason))
                {
                    reason = "pickerNo=" + ToPickerNo(_pickedPickerIndexes[i]) + ", " + pickerReason;
                    return false;
                }
            }
            return true;
        }

        private bool HasValidAutoVisionContext(int pickerIndex, out string reason)
        {
            reason = string.Empty;
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
            {
                reason = "현재 Picker에 제품이 없습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(die.DieId) || die.InputSequenceNo < 0 || die.InputSequenceNo > 9999)
            {
                reason = "DieId 또는 DIE_INDEX가 유효하지 않습니다. die=" + (die.DieId ?? string.Empty) +
                         ", dieIndex=" + die.InputSequenceNo;
                return false;
            }

            int fb = Side == PickerSequenceSide.Front ? 0 : 1;
            VisionInspectionRequestContext context = VisionInspectionContextFactory.CreateAuto(
                AutoVisionChannel.BottomInspection,
                VisionToolIds.BottomInspection.SurfaceInspector,
                fb,
                pickerNo,
                die.InputSequenceNo,
                die.Wafer_IndexX,
                die.Wafer_IndexY,
                0,
                die.DieId,
                die.WaferID_Input,
                VisionInspectionOperations.Inspect,
                VisionResultTimings.Deferred,
                string.Empty);
            VisionInspectionEnvelope envelope = VisionInspectionEnvelope.Create(context);
            return envelope.Validate(out reason);
        }

        private bool IsSameCurrentPickerProduct(InspectionTarget target, out string reason)
        {
            reason = string.Empty;
            if (target == null || target.Die == null)
            {
                reason = "검사 대상이 없습니다.";
                return false;
            }
            DieMaterial current = MaterialStateService.GetDieAtPicker(PickerLocationKind, target.PickerNo);
            if (current == null)
            {
                reason = "pickerNo=" + target.PickerNo + " 제품이 중첩 직전에 제거되었습니다.";
                return false;
            }
            if (!string.Equals(current.DieId, target.Die.DieId, StringComparison.OrdinalIgnoreCase))
            {
                reason = "pickerNo=" + target.PickerNo + " 제품이 변경되었습니다. expected=" +
                         target.Die.DieId + ", current=" + current.DieId;
                return false;
            }
            return HasValidAutoVisionContext(target.PickerIndex, out reason);
        }

        private bool CanStartSpecialBottomSideOverlap(
            InspectionTarget bottomTarget,
            InspectionTarget sideTarget,
            out string reason)
        {
            reason = string.Empty;
            if (!_parallelFirstSideEnabled)
            {
                reason = "특수 중첩 플래그가 비활성화되었습니다.";
                return false;
            }
            if (bottomTarget == null || bottomTarget.PickerNo != 1 ||
                sideTarget == null || sideTarget.PickerNo != 4)
            {
                reason = "중첩 대상은 Bottom P1과 Side P4여야 합니다.";
                return false;
            }
            if (!IsSameCurrentPickerProduct(bottomTarget, out reason))
                return false;
            return IsSameCurrentPickerProduct(sideTarget, out reason);
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
                        Y = _inspectionFixedY
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
                        Y = _inspectionFixedY
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
                        ", targetY=" + _inspectionFixedY.ToString("0.###") +
                        ", mapping=BottomReference" +
                        ", pickerYMode=FixedFromFirstBottom" +
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
            EnsureDeferredFinalResultLifetime(ct);

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

                // 기존 조건(~2026-07-26): 선행 Z 하강 join을 X 이동 "전"에 수행해 X 이동과
                //   Z 하강의 오버랩 이득이 상쇄됐다. 현재 기준(사용자 승인 2026-07-26, 1-B(b)):
                //   join은 MoveBottomTargetAsync 내부의 X 도착 후·Z/T 최종 확인 앞으로 이동 —
                //   X 이동과 선행 Z(+T)가 실제로 겹친다.
                result = await MoveBottomTargetAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // 다음 PickerZ+T는 현재 Bottom 검사와 겹쳐 선행 구동합니다.
                // 완료 확인은 다음 Picker 검사 진입 시(X 도착 후) 수행하여 현재 검사를 지연시키지 않습니다.
                StartNextBottomZDownCommand(i + 1);

                result = await RunAutoFocusBeforeBottomInspectionAsync(target, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                bool specialBottomDelayCompleted = false;
                if (sideFirstTarget != null)
                {
                    string overlapReason;
                    if (!CanStartSpecialBottomSideOverlap(target, sideFirstTarget, out overlapReason))
                    {
                        DisableSpecialBottomSideOverlap(overlapReason);
                        sideFirstTarget = null;
                    }
                    else
                    {
                        result = await PrepareSideTargetForInspectionAsync(sideFirstTarget, ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;

                        // 검사 전 대기까지 끝낸 다음 실제 Bottom/Side 요청을 병렬 송신하기 직전에
                        // P1/P4 제품과 자재 문맥을 다시 확인한다.
                        await DelayBeforeBottomVisionInspectionAsync(target.PickerNo, ct).ConfigureAwait(false);
                        specialBottomDelayCompleted = true;
                        if (!CanStartSpecialBottomSideOverlap(target, sideFirstTarget, out overlapReason))
                        {
                            DisableSpecialBottomSideOverlap(overlapReason);
                            sideFirstTarget = null;
                        }
                    }
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

                    var bottomShot = new BottomShot { Target = target, InspectStartedAt = bottomInspectStartedAt };
                    _pendingBottomShots.Add(bottomShot);

                    result = triggerResults[1];
                    if (result != 0)
                    {
                        int bottomCompletionResult = await ApplyBottomMResultAsync(bottomShot, ct).ConfigureAwait(false);
                        if (bottomCompletionResult != 0)
                            return bottomCompletionResult;
                        return result;
                    }

                    // 4-Picker 풀 적재에서만 Bottom P1의 MRESULT/RESULT 회수와
                    // Side P4의 나머지(90도) 촬영을 중첩한다. 두 작업이 모두 끝나야 다음 단계로 진행한다.
                    Task<int> bottomCompletionTask = ApplyBottomMResultAsync(bottomShot, ct);
                    Task<int> sideCaptureTask = CompletePreparedFirstSideInspectionDuringLastBottomAsync(sideFirstTarget, ct);
                    int[] completionResults = await Task.WhenAll(bottomCompletionTask, sideCaptureTask).ConfigureAwait(false);
                    if (completionResults[0] != 0)
                        return completionResults[0];
                    if (completionResults[1] != 0)
                        return completionResults[1];
                }
                else
                {
                    result = await StartBottomInspectionAsync(target, ct, specialBottomDelayCompleted).ConfigureAwait(false);
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

                    var bottomShot = new BottomShot { Target = target, InspectStartedAt = bottomInspectStartedAt };
                    _pendingBottomShots.Add(bottomShot);

                    // Bottom은 REQ -> EPD까지 확인하면 즉시 MRESULT 요청을 백그라운드로 시작하고(B방식),
                    // 다음 Picker 물류/모션은 그대로 진행한다. 결과 검증/소비는 Side 0도 진입 직전에 수행하며,
                    // MRESULT 수신 즉시 최종 RESULT 요청도 ApplyBottomMResultAsync 내부에서 바로 등록된다.
                    StartBottomMResultCollection(bottomShot, ct);
                }
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom shot 전체 완료. pendingResult=" + CountPendingBottomResults() + " - Ok");

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
                Y = _inspectionFixedYConfigured
                    ? _inspectionFixedY
                    : ResolvePickerZoneY("DieBottomPosition", pickerIndex),
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
                    bool fixedYWasEstablished = _bottomInspectionYReady;
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

                    if (!fixedYWasEstablished)
                        targets[PickerAxis.PickerY] = target.Y;
                    else
                    {
                        int fixedYResult = VerifyInspectionFixedY("Bottom 후속 PickerX 이동 전", target);
                        if (fixedYResult != 0)
                            return fixedYResult;
                    }

                    // 1-A 접근 구간 Z+T 선행(사용자 승인 2026-07-26): 게이트 3종 —
                    //   (1) fixed-Y 확립(Y가 이미 촬영 위치 — Y가 Avoid면 Z 하강이
                    //       VerifyPickerYAvoidBlocksZDown에 차단되므로 이 게이트가 안전 근거),
                    //   (2) Auto 모드(일반/수동 경로 제외),
                    //   (3) ApproachPreMotionDistanceMm > 0.
                    //   추가로 Z가 Avoid 또는 목표 위치일 때만(기존 선행 하강과 동일 자격) 발동.
                    //   미충족 시 기존 동기 경로 그대로(동작 무변경).
                    PickerBottomInspectionMotionConfig approachConfig = ResolveBottomInspectionMotionConfig();
                    PickerAxis approachZAxis = GetPickerZAxis(target.PickerIndex);
                    bool approachPreMotionEligible =
                        fixedYWasEstablished &&
                        !pickerXAlreadyInBottomPosition &&
                        Options != null && Options.RunMode == SequenceRunMode.Auto &&
                        approachConfig.ApproachPreMotionDistanceMm > 0.0 &&
                        (IsPickerAxisInPosition(approachZAxis, target.Z) ||
                         IsPickerAxisInPosition(approachZAxis, GetPickerTeachingPosition(approachZAxis, "AvoidPosition")));

                    int result;
                    if (approachPreMotionEligible)
                    {
                        // X 이동을 기존 헬퍼 그대로 비동기 보관(내부 인터락 대기 3종 + 속도 스케일
                        // 자동 상속 — 신규 저수준 발행 없음). 실측 폴링으로 잔여 거리를 감시한다.
                        Task<int> xMoveTask = MovePickerXTThenYAndVerifyAsync(
                            targets,
                            "Bottom/Side 통합 Bottom X/Y",
                            ct,
                            BuildBottomTargetName(target));
                        BaseAxis xAxisObject = GetPickerAxis(PickerAxis.PickerX);
                        bool preMotionStarted = false;
                        DateTime approachPollStart = DateTime.UtcNow;
                        while (!xMoveTask.IsCompleted)
                        {
                            ct.ThrowIfCancellationRequested();

                            double remaining = xAxisObject != null
                                ? Math.Abs(xAxisObject.ActualPosition - target.X)
                                : double.MaxValue;
                            if (remaining <= approachConfig.ApproachPreMotionDistanceMm)
                            {
                                QueuePendingBottomZTDown(target.PickerIndex, target.Z, target.T0);
                                StartPendingBottomZDownCommand(
                                    "Bottom 접근 구간 현재 피커 Z+T 선행 구동",
                                    target.PickerIndex);
                                preMotionStarted = true;
                                WriteLog("PickerBottomAndSideInspectionSequence",
                                    Name + " Bottom 접근 구간 Z+T 선행을 발동했습니다. " +
                                    "pickerNo=" + target.PickerNo +
                                    ", remainingX=" + remaining.ToString("0.###") +
                                    ", thresholdMm=" + approachConfig.ApproachPreMotionDistanceMm.ToString("0.###") +
                                    ", elapsedMs=" + (DateTime.UtcNow - approachPollStart).TotalMilliseconds.ToString("0") + " - Ok");
                                break;
                            }

                            await Task.Delay(10, ct).ConfigureAwait(false);
                        }

                        result = await xMoveTask.ConfigureAwait(false);
                        if (result != 0 && preMotionStarted)
                            ObservePendingBottomZDownTasks("접근 구간 X 이동 실패로 선행 Z+T Task를 관찰 정리합니다.");
                    }
                    else
                    {
                        result = await MovePickerXTThenYAndVerifyAsync(
                            targets,
                            "Bottom/Side 통합 Bottom X/Y",
                            ct,
                            BuildBottomTargetName(target)).ConfigureAwait(false);
                    }

                    if (result != 0)
                    {
                        tactScope.Fail("PICKER-BOTTOM-SIDE-BOTTOM-XY", BuildTactDetail(target, "Bottom pitch X/Y move failed. result=" + result));
                        return result;
                    }

                    _bottomInspectionYReady = IsPickerAxisInPosition(PickerAxis.PickerY, target.Y);
                    if (!_bottomInspectionYReady)
                    {
                        if (fixedYWasEstablished)
                            return VerifyInspectionFixedY("Bottom 후속 PickerX 이동 후", target);

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

                    // join(사용자 승인 2026-07-26, 1-B(b)): 선행 Z(+T)의 완료 확인을 X 도착 후
                    // 이 지점에서 수행한다 — X 이동과 선행 하강이 겹치고, 미완료분은 여기서만 대기.
                    // 아래 Z/T 최종 단계는 CanSkip으로 자연 수렴(선행 완료 시 무명령).
                    DateTime pendingJoinStart = DateTime.UtcNow;
                    bool hadPendingJoin = HasPendingBottomZDown(target.PickerIndex);
                    result = await CompletePendingBottomZDownForPickerAsync(target.PickerIndex, ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        tactScope.Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-JOIN", BuildTactDetail(target, "Bottom pre-down join failed. result=" + result));
                        return result;
                    }
                    if (hadPendingJoin)
                    {
                        WriteLog("PickerBottomAndSideInspectionSequence",
                            Name + " Bottom 선행 Z+T join 완료. pickerNo=" + target.PickerNo +
                            ", joinWaitMs=" + (DateTime.UtcNow - pendingJoinStart).TotalMilliseconds.ToString("0") + " - Ok");
                    }

                    PickerAxis zAxis = GetPickerZAxis(target.PickerIndex);
                    if (!CanSkipPickerMoveCommand(zAxis, target.Z))
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

                    result = VerifyInspectionFixedY("Bottom 촬영 위치 준비 완료", target);
                    if (result != 0)
                    {
                        tactScope.Fail("PICKER-BOTTOM-SIDE-FIXED-Y-DRIFT", BuildTactDetail(target, "PickerY fixed-position verification failed before Bottom shot."));
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
                if (CanSkipPickerMoveCommand(PickerAxis.PickerY, yAvoid))
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

            int fixedYResult = VerifyInspectionFixedY("Bottom REQ 직전", target);
            if (fixedYResult != 0)
                return fixedYResult;

            // Bottom 촬영 명령 시점의 PickerY 지령 위치를 캡처한다 (결과 도착 시 Y 전처리에 사용).
            BaseAxis bottomShotPickerY = GetPickerAxis(PickerAxis.PickerY);
            target.BottomShotPickerYCommand = bottomShotPickerY != null ? bottomShotPickerY.CommandPosition : 0.0;

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
                ", pendingResult=" + (CountPendingBottomResults() + 1) + " - Ok");
            await DelayAfterRuntimeAutoFocusBottomGrabAckAsync(target.PickerNo, ct).ConfigureAwait(false);
            return 0;
        }

        private async Task<int[]> StartBottomAndPreparedSide0InspectionAsync(InspectionTarget bottomTarget, InspectionTarget sideTarget, CancellationToken ct)
        {
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

        private BottomShot FindBottomShot(int pickerIndex)
        {
            for (int i = 0; i < _pendingBottomShots.Count; i++)
            {
                BottomShot shot = _pendingBottomShots[i];
                if (shot != null && shot.Target != null && shot.Target.PickerIndex == pickerIndex)
                    return shot;
            }

            return null;
        }

        private async Task<int> EnsureBottomReadyForSideAsync(int pickerIndex, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!_sideReadyPickerIndexes.Contains(pickerIndex))
            {
                BottomShot shot = FindBottomShot(pickerIndex);
                if (shot == null)
                {
                    return Fail("PICKER-BOTTOM-SIDE-BOTTOM-MRESULT-NOT-PENDING", "Vision",
                        "Side 0도 검사 전에 받아야 할 동일 Picker의 Bottom MRESULT 항목이 없습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + ToPickerNo(pickerIndex) + ".");
                }

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side 0도 진입 직전 동일 Picker Bottom MRESULT를 확인합니다. " +
                    "die=" + (shot.Target.Die != null ? shot.Target.Die.DieId : "-") +
                    ", pickerNo=" + shot.Target.PickerNo +
                    ", deferredTask=" + (shot.MResultTask != null) + " - Start");

                int result = shot.MResultTask != null
                    ? await shot.MResultTask.ConfigureAwait(false)
                    : await ApplyBottomMResultAsync(shot, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return ValidateRuntimeSideInspectionCorrection(pickerIndex);
        }

        private void StartBottomMResultCollection(BottomShot shot, CancellationToken ct)
        {
            if (shot == null || shot.Target == null || shot.MResultTask != null)
                return;

            shot.MResultTask = ApplyBottomMResultAsync(shot, ct);
            // Abort 등으로 끝까지 await되지 않아도 UnobservedTaskException이 발생하지 않도록 예외를 관찰한다.
            shot.MResultTask.ContinueWith(
                t => { var _ = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom EPD 직후 MRESULT 요청을 백그라운드로 시작했습니다. " +
                "die=" + (shot.Target.Die != null ? shot.Target.Die.DieId : "-") +
                ", pickerNo=" + shot.Target.PickerNo + " - Start");
        }

        private async Task<int> ApplyBottomMResultAsync(BottomShot shot, CancellationToken ct)
        {
            if (shot == null || shot.Target == null || shot.MResultApplied)
                return 0;

            int timeoutMs = ResolveVisionInspectionTimeout();
            // Side 위치에 필요한 OffsetY만 MRESULT에서 먼저 받고, 최종 판정 RESULT는 Place 접근과 병렬 수집한다.
            BottomVisionOffset result = Side == PickerSequenceSide.Front
                ? await FrontPicker.WaitBottomInspectionMResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitBottomInspectionMResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

            if (result == null)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-MRESULT", "Vision",
                    "Bottom MRESULT 수신 실패. 최종 RESULT 미수신을 NG로 가장하지 않고 Side 진입을 차단합니다. die=" + shot.Target.Die.DieId +
                    ", pickerNo=" + shot.Target.PickerNo +
                    ", timeoutMs=" + timeoutMs);
            }

            if (result.PickerNo != shot.Target.PickerNo)
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-MRESULT-PICKER-MISMATCH", "Vision",
                    "Bottom MRESULT의 Picker 번호가 요청 대상과 일치하지 않습니다. " +
                    "side=" + Side +
                    ", requestedPickerNo=" + shot.Target.PickerNo +
                    ", resultPickerNo=" + result.PickerNo +
                    ", die=" + shot.Target.Die.DieId + ".");
            }

            double sideVisionOffsetX;
            double sideVisionOffsetY;
            string offsetReason;
            if (!TryResolveBottomMResultSideVisionOffset(result, out sideVisionOffsetX, out sideVisionOffsetY, out offsetReason))
            {
                return Fail("PICKER-BOTTOM-SIDE-BOTTOM-MRESULT-OFFSET", "Vision",
                    "Bottom MRESULT Offset(X,Y)가 Side Vision Y 절대식 허용 조건을 만족하지 않습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + shot.Target.PickerNo +
                    ", die=" + shot.Target.Die.DieId +
                    ", reason=" + offsetReason + ".");
            }

            result.OffsetX = sideVisionOffsetX;
            result.OffsetY = sideVisionOffsetY;
            shot.MResult = result;
            StoreRuntimeSideInspectionCorrection(shot.Target, result);

            int correctionResult = ValidateRuntimeSideInspectionCorrection(shot.Target.PickerIndex);
            if (correctionResult != 0)
                return correctionResult;

            shot.MResultApplied = true;

            if (!_sideReadyPickerIndexes.Contains(shot.Target.PickerIndex))
                _sideReadyPickerIndexes.Add(shot.Target.PickerIndex);

            RecordInspectionCheckpointForTact(
                "BottomMResult",
                "Bottom MRESULT Interval",
                "MRESULT",
                shot.Target,
                "offsetX=" + result.OffsetX.ToString("F6") +
                ",offsetY=" + result.OffsetY.ToString("F6") + ",finalResultPending=True");

            EnsureDeferredFinalResultLifetime(ct);
            if (shot.FinalResultTask == null)
                shot.FinalResultTask = ReceiveBottomFinalResultAsync(shot, _deferredResultCancellation.Token);

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom MRESULT Offset(X,Y) 저장 및 SideReady 등록 완료. 최종 RESULT는 병렬 수집합니다. die=" + shot.Target.Die.DieId +
                ", pickerNo=" + shot.Target.PickerNo +
                ", offsetXmm=" + result.OffsetX.ToString("F6") +
                ", offsetYmm=" + result.OffsetY.ToString("F6") +
                ", sideReadyCount=" + _sideReadyPickerIndexes.Count + " - Ok");
            return 0;
        }

        private void ApplyBottomInspectionResult(InspectionTarget target, BottomVisionOffset result)
        {
            MaterialInspectionResult inspectionResult = result.IsOk ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng;
            DieResult dieResult = result.IsOk ? DieResult.Good : DieResult.NG;
            MaterialInspectionResult itemOffsetXResult =
                result.HasMeasureValid && result.MeasureValid &&
                result.HasBottomItemOffsetX && result.HasBottomItemOffsetXPass && result.BottomItemOffsetXPass
                    ? MaterialInspectionResult.Ok
                    : MaterialInspectionResult.Ng;
            MaterialInspectionResult itemOffsetYResult =
                result.HasMeasureValid && result.MeasureValid &&
                result.HasBottomItemOffsetY && result.HasBottomItemOffsetYPass && result.BottomItemOffsetYPass
                    ? MaterialInspectionResult.Ok
                    : MaterialInspectionResult.Ng;
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
                BuildBooleanMeasurement("BottomInspectionResult", result.IsOk),
                BuildMeasurement("BottomItemOffsetX", result.BottomItemOffsetX, "mm", itemOffsetXResult),
                BuildMeasurement("BottomItemOffsetY", result.BottomItemOffsetY, "mm", itemOffsetYResult),
                BuildBooleanMeasurement("BottomItemOffsetXPresent", result.HasBottomItemOffsetX),
                BuildBooleanMeasurement("BottomItemOffsetYPresent", result.HasBottomItemOffsetY),
                BuildBooleanMeasurement("BottomItemOffsetXPassPresent", result.HasBottomItemOffsetXPass),
                BuildBooleanMeasurement("BottomItemOffsetYPassPresent", result.HasBottomItemOffsetYPass),
                BuildBooleanMeasurement("BottomItemOffsetXPass", result.BottomItemOffsetXPass),
                BuildBooleanMeasurement("BottomItemOffsetYPass", result.BottomItemOffsetYPass),
                BuildBooleanMeasurement("BottomMeasureValidPresent", result.HasMeasureValid),
                BuildBooleanMeasurement("BottomMeasureValid", result.MeasureValid),
                BuildRawTextMeasurement("BottomRequestId", result.RequestId, inspectionResult),
                BuildRawTextMeasurement("BottomGroupId", result.GroupId, inspectionResult),
                BuildRawTextMeasurement("BottomDieId", result.DieId, inspectionResult),
                BuildMeasurement("BottomDieIndex", result.DieIndex, "index", inspectionResult),
                BuildMeasurement("BottomPickerNo", target.PickerNo, "index", inspectionResult),
                BuildRawTextMeasurement(
                    "BottomPickedAtTicks",
                    target.Die.PickedAt.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    inspectionResult)
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

            UpdatePickRuntimeOffsetFilter(target, result);
        }

        private static InspectionMeasurement BuildRawTextMeasurement(
            string name,
            string value,
            MaterialInspectionResult result)
        {
            return new InspectionMeasurement
            {
                Name = name ?? string.Empty,
                Value = 0.0,
                Unit = "raw",
                RawValue = value ?? string.Empty,
                Result = result
            };
        }

        /// <summary>
        /// Bottom 검사 Pass 결과를 Pick 런타임 오프셋 필터에 반영한다 (학습 전용 — Material 반영과 무관).
        /// 콜렛Cal Y 기준값은 ColletCalibrationRecord.FinalPickerY를 사용한다
        /// (ColletCalibrationSequence가 최종 Vision 매치 시점의 PickerY ActualPosition을 저장하는 필드).
        /// </summary>
        private void UpdatePickRuntimeOffsetFilter(InspectionTarget target, BottomVisionOffset result)
        {
            try
            {
                if (target == null || result == null || !result.IsOk)
                    return;   // NG 측정은 신뢰할 수 없으므로 학습에서 제외한다.

                ColletCalibrationRecord collet = CalibrationCoordinateService.ResolveCollet(
                    Context != null ? Context.Machine : null,
                    Side == PickerSequenceSide.Front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                    target.PickerIndex);

                // P4 기준 환산용(사용자 지시 2026-07-30): 촬영 Y가 P4 고정(07-28 승인)이므로
                // 4번 픽커 콜렛Cal Y를 기준값으로 함께 전달한다.
                ColletCalibrationRecord basePicker4Collet = CalibrationCoordinateService.ResolveCollet(
                    Context != null ? Context.Machine : null,
                    Side == PickerSequenceSide.Front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                    ToPickerIndex(4));

                PickRuntimeOffsetService.OnBottomInspectionOffset(
                    Side,
                    target.PickerNo,
                    result.OffsetX,
                    result.OffsetY,
                    result.OffsetT,
                    target.BottomShotPickerYCommand,
                    collet != null ? collet.FinalPickerY : 0.0,
                    collet != null && collet.Valid,
                    basePicker4Collet != null ? basePicker4Collet.FinalPickerY : 0.0,
                    basePicker4Collet != null && basePicker4Collet.Valid,
                    target.Die != null ? target.Die.DieId : string.Empty);
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Pick 런타임 오프셋 필터 갱신 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
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

            // Bottom MRESULT의 Offset(X,Y) 벡터를 Side Vision Y 절대식 입력으로 저장한다.
            // Picker X/Y/Z/T 보정에는 전달하지 않는다. 0도는 OffsetY, 90도는 OffsetX(COC 중심 CW 회전)로 소비된다.
            double bottomOffsetXmm = result.OffsetX;
            double bottomOffsetYmm = result.OffsetY;
            double pickerZOffset = 0.0;
            bool valid = IsValidSideVisionCenterCorrection(bottomOffsetXmm) &&
                         IsValidSideVisionCenterCorrection(bottomOffsetYmm);
            string sourceDieId = target.Die != null ? target.Die.DieId : string.Empty;

            if (Side == PickerSequenceSide.Front)
            {
                if (FrontPicker != null)
                    FrontPicker.SetRuntimeSideInspectionCorrection(
                        target.PickerIndex,
                        bottomOffsetXmm,
                        bottomOffsetYmm,
                        pickerZOffset,
                        valid,
                        sourceDieId);
            }
            else
            {
                if (RearPicker != null)
                    RearPicker.SetRuntimeSideInspectionCorrection(
                        target.PickerIndex,
                        bottomOffsetXmm,
                        bottomOffsetYmm,
                        pickerZOffset,
                        valid,
                        sourceDieId);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom MRESULT Offset(X,Y)를 Side Vision Y 절대식 입력으로 저장합니다. " +
                "side=" + Side +
                ", pickerNo=" + target.PickerNo +
                ", die=" + sourceDieId +
                ", bottomInspectionOk=" + result.IsOk +
                ", valid=" + valid +
                ", bottomOffsetXmm=" + bottomOffsetXmm.ToString("F6") +
                ", bottomOffsetYmm=" + bottomOffsetYmm.ToString("F6") +
                ", PickerZ.offset=" + pickerZOffset.ToString("F6") +
                ", pickerAxisOffsetApplied=false - Ok");
        }

        private int ValidateRuntimeSideInspectionCorrection(int pickerIndex)
        {
            int pickerNo = ToPickerNo(pickerIndex);
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (die == null)
            {
                return Fail("PICKER-BOTTOM-SIDE-MRESULT-OFFSET-DIE", Name,
                    "Bottom MRESULT OffsetY 확인 실패. 해당 Picker의 제품 정보를 찾을 수 없습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ".");
            }

            PickerSideInspectionCorrection correction = ResolveRuntimeSideInspectionCorrection(pickerIndex);
            if (correction == null || !correction.IsValid)
            {
                return Fail("PICKER-BOTTOM-SIDE-MRESULT-OFFSET-MISSING", "Vision",
                    "Side 검사에 필요한 Bottom MRESULT OffsetY(mm)가 없습니다. " +
                    "Bottom MRESULT의 bottom_offset_y_mm 수신 상태를 확인하세요. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + die.DieId + ".");
            }

            if (string.IsNullOrWhiteSpace(die.DieId) ||
                string.IsNullOrWhiteSpace(correction.SourceDieId) ||
                !string.Equals(correction.SourceDieId, die.DieId, StringComparison.Ordinal))
            {
                return Fail("PICKER-BOTTOM-SIDE-MRESULT-OFFSET-DIE-MISMATCH", "Vision",
                    "Bottom MRESULT OffsetY의 제품과 Side 검사 제품이 일치하지 않습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", sideDie=" + die.DieId +
                    ", bottomResultDie=" + (correction.SourceDieId ?? string.Empty) + ".");
            }

            if (!IsValidSideVisionCenterCorrection(correction.BottomOffsetXmm) ||
                !IsValidSideVisionCenterCorrection(correction.BottomOffsetYmm))
            {
                return Fail("PICKER-BOTTOM-SIDE-MRESULT-OFFSET-RANGE", "Vision",
                    "Bottom MRESULT Offset(X,Y)가 Side Vision Y 절대식 허용범위를 벗어났습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + die.DieId +
                    ", bottomOffsetXmm=" + correction.BottomOffsetXmm.ToString("F6") +
                    ", bottomOffsetYmm=" + correction.BottomOffsetYmm.ToString("F6") +
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

        private static bool TryResolveBottomMResultSideVisionOffset(
            BottomVisionOffset result,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            offsetX = 0.0;
            offsetY = 0.0;
            reason = string.Empty;

            if (result == null || result.Values == null)
            {
                reason = "MRESULT values 없음";
                return false;
            }

            string rawX;
            if (!result.Values.TryGetValue("bottom_offset_x_mm", out rawX) ||
                !VisionProtocolResponse.TryParseDouble(rawX, out offsetX) ||
                !IsFiniteCorrectionValue(offsetX))
            {
                reason = "bottom_offset_x_mm 누락 또는 숫자 형식 오류, raw=" + (rawX ?? string.Empty);
                return false;
            }

            string rawY;
            if (!result.Values.TryGetValue("bottom_offset_y_mm", out rawY) ||
                !VisionProtocolResponse.TryParseDouble(rawY, out offsetY) ||
                !IsFiniteCorrectionValue(offsetY))
            {
                reason = "bottom_offset_y_mm 누락 또는 숫자 형식 오류, raw=" + (rawY ?? string.Empty);
                return false;
            }

            if (!IsValidSideVisionCenterCorrection(offsetX) ||
                !IsValidSideVisionCenterCorrection(offsetY))
            {
                reason = "Offset 허용 범위 초과, value=(" + offsetX.ToString("F6") +
                         "," + offsetY.ToString("F6") + ")" +
                         ", allowedAbsMaxMm=" + MaxSideVisionCenterCorrectionMm.ToString("F3");
                return false;
            }

            return true;
        }

        private int VerifyInspectionFixedY(string stage, InspectionTarget target)
        {
            double fixedY = target != null ? target.Y : _inspectionFixedY;
            int pickerNo = target != null ? target.PickerNo : 0;
            if (!_inspectionFixedYConfigured || !IsPickerAxisInPosition(PickerAxis.PickerY, fixedY))
            {
                return Fail("PICKER-BOTTOM-SIDE-FIXED-Y-DRIFT", Name,
                    "Bottom P4 진입 후 Side 종료까지 고정해야 할 PickerY가 기준 위치를 벗어났습니다. " +
                    "자동 재이동하지 않고 검사를 차단합니다. " +
                    "side=" + Side +
                    ", stage=" + (stage ?? string.Empty) +
                    ", pickerNo=" + pickerNo +
                    ", fixedY=" + fixedY.ToString("F6") +
                    ", configured=" + _inspectionFixedYConfigured +
                    ", " + BuildPickerAxisState(PickerAxis.PickerY, fixedY));
            }

            return 0;
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
                if (_sideCapturedPickerIndexes.Contains(pickerIndex))
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 현재 실행에서 이미 촬영 완료된 Side 대상은 재촬영을 생략하고 최종 RESULT Collection에서 회수합니다. " +
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
            }

            if (_pendingSideResults.Count != _pickedPickerIndexes.Count)
            {
                return Fail("PICKER-BOTTOM-SIDE-SIDE-CAPTURE-COUNT", "Vision",
                    "Side 최종 RESULT 병렬 수집 전 전체 EPD 완료 수가 일치하지 않습니다. " +
                    "side=" + Side +
                    ", capturedCount=" + _pendingSideResults.Count +
                    ", loadedCount=" + _pickedPickerIndexes.Count + ".");
            }

            int fixedYResult = VerifyInspectionFixedY("Side 전체 EPD 완료", null);
            if (fixedYResult != 0)
                return fixedYResult;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side 전체 EPD 완료. Bottom/Side 최종 RESULT Task를 유지한 채 Place 공정 이동을 시작합니다. " +
                "capturedCount=" + _pendingSideResults.Count +
                ", loadedPickers=" + BuildPickerNoListText(_pickedPickerIndexes) + " - Ok");
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
                                   IsValidSideVisionCenterCorrection(correction.BottomOffsetXmm) &&
                                   IsValidSideVisionCenterCorrection(correction.BottomOffsetYmm) &&
                                   !string.IsNullOrWhiteSpace(die.DieId) &&
                                   !string.IsNullOrWhiteSpace(correction.SourceDieId) &&
                                   string.Equals(correction.SourceDieId, die.DieId, StringComparison.Ordinal);
            double baseZ = ResolveSidePickerZBase(pickerIndex, pickerNo);
            double zOffset = 0.0;
            double bottomOffsetXmm = correctionValid ? correction.BottomOffsetXmm : 0.0;
            double bottomOffsetYmm = correctionValid ? correction.BottomOffsetYmm : 0.0;

            SideVisionYTargetResult sideVisionCalc;
            string sideVisionCalcFailReason;
            if (!SideVisionYTargetCalculator.TryBuild(
                Context != null ? Context.Machine : null,
                Context != null ? Context.Controller : null,
                Side == PickerSequenceSide.Front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                pickerNo,
                bottomOffsetXmm,
                bottomOffsetYmm,
                correctionValid,
                out sideVisionCalc,
                out sideVisionCalcFailReason))
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side Vision Y 절대식 계산 실패로 Side 목표를 만들 수 없습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + die.DieId +
                    ", offsetApplied=" + correctionValid +
                    ", reason=" + sideVisionCalcFailReason + " - Failed");
                return null;
            }

            SideVisionPositionTarget frontSideVision = new SideVisionPositionTarget
            {
                CameraSide = PickerSequenceSide.Front,
                Axis = VisionAxis.FrontSideVisionY,
                Process0Y = sideVisionCalc.Front0Y,
                Process90Y = sideVisionCalc.Front90Y
            };
            SideVisionPositionTarget rearSideVision = new SideVisionPositionTarget
            {
                CameraSide = PickerSequenceSide.Rear,
                Axis = VisionAxis.RearSideVisionY,
                Process0Y = sideVisionCalc.Rear0Y,
                Process90Y = sideVisionCalc.Rear90Y
            };
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
                SideBottomOffsetXmm = bottomOffsetXmm,
                SideBottomOffsetYmm = bottomOffsetYmm,
                SidePickerZBase = baseZ,
                SidePickerZOffset = zOffset,
                FrontSideVision = frontSideVision,
                RearSideVision = rearSideVision,
                SideCorrectionSourceDieId = correction != null ? correction.SourceDieId : string.Empty,
                SideVisionCalc = sideVisionCalc
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
                // 절대식 계산 결과가 없는 비정상 경로 — 로그 표기용 안전 폴백(티칭값)만 반환한다.
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                return vision != null
                    ? vision.GetVisionTeachingPosition(ResolveSideVisionAxis(cameraSide), "Process0Position")
                    : 0.0;
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
                    DisableSpecialBottomSideOverlap(
                        "Bottom 마지막 대상이 P1이 아닙니다. lastBottomPickerNo=" + lastBottomTarget.PickerNo);
                    return 0;
                }

                int sideFirstPickerIndex = ToPickerIndex(4);
                if (!_pickedPickerIndexes.Contains(sideFirstPickerIndex))
                {
                    DisableSpecialBottomSideOverlap("P4가 현재 검사 대상 목록에 없습니다.");
                    return 0;
                }

                if (_sideCapturedPickerIndexes.Contains(sideFirstPickerIndex) ||
                    _sideCompletedPickerIndexes.Contains(sideFirstPickerIndex))
                    return 0;

                sideTarget = BuildSideTarget(sideFirstPickerIndex);
                if (sideTarget == null || sideTarget.Die == null)
                {
                    sideTarget = null;
                    DisableSpecialBottomSideOverlap("P4 Side 검사 목표 또는 제품 문맥을 만들 수 없습니다.");
                    return 0;
                }

                string overlapReason;
                if (!CanStartSpecialBottomSideOverlap(lastBottomTarget, sideTarget, out overlapReason))
                {
                    sideTarget = null;
                    DisableSpecialBottomSideOverlap(overlapReason);
                    return 0;
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
                DisableSpecialBottomSideOverlap(
                    "Bottom P1/Side P4 공유 위치 준비 중 예외가 발생했습니다. error=" + ex.Message);
                return 0;
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

                return await CompleteSideInspectionAfterSide0Async(target, ct).ConfigureAwait(false);
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
            // 동일 Picker Bottom MRESULT 확인은 EnsureBottomReadyForSideAsync에서 끝났다.
            // 여기서는 Bottom XYT push나 최종 RESULT를 추가로 기다리지 않고 Side 0도 위치 준비를 시작한다.
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

            return VerifyInspectionFixedY("Side 0도 촬영 위치 준비 완료", target);
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

                    // Side RESULT는 모든 Picker의 Front0/Rear0/Front90/Rear90 EPD가 끝난 뒤
                    // RunSidePipeline의 마지막 Result Collection 단계에서만 회수한다.
                    side90TactScope.Complete(BuildTactDetail(target, "Side 90deg inspection EPD 수신 완료. RESULT는 전체 촬영 후 회수합니다."));
                    RecordInspectionCheckpointForTact(
                        "Side90Inspection",
                        "Side 90deg Inspect Interval",
                        "90deg",
                        target,
                        "started=True,resultPending=True");
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

            RegisterPendingSideResult(target, ct);
            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);

            QueuePendingZAvoid(target.PickerIndex);
            QueuePendingT0Return(target.PickerIndex, target.T0);
            StartPendingT0ReturnCommandAsync("다음 Side 검사 중 이전 PickerT 0도 복귀", ct);
            return 0;
        }

        private void DisableSpecialBottomSideOverlap(string reason)
        {
            _parallelFirstSideEnabled = false;
            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " 4-Picker 특수 중첩 조건이 중첩 직전에 해제되었습니다. " +
                "Bottom P1은 일반 완료하고 Side는 일반 순차 처리합니다. " +
                "reason=" + (reason ?? string.Empty) + " - Check");
        }

        private void RegisterPendingSideResult(InspectionTarget target, CancellationToken ct)
        {
            if (target == null || target.Die == null)
                return;

            if (_sideCapturedPickerIndexes.Contains(target.PickerIndex))
                return;

            _sideCapturedPickerIndexes.Add(target.PickerIndex);
            _pendingSideResults.Add(target);
            EnsureDeferredFinalResultLifetime(ct);
            if (!_sideFinalResultTasks.ContainsKey(target.PickerIndex))
            {
                _sideFinalResultTasks[target.PickerIndex] = ReceiveSideFinalResultAsync(
                    target,
                    _deferredResultCancellation.Token);
            }
            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Side Front0/Rear0/Front90/Rear90 EPD 전체 완료. 최종 RESULT 병렬 수집 Task 등록. " +
                "die=" + target.Die.DieId +
                ", pickerNo=" + target.PickerNo +
                ", capturedCount=" + _sideCapturedPickerIndexes.Count +
                ", loadedCount=" + _pickedPickerIndexes.Count + " - Ok");
        }

        private async Task DrainCapturedSideResultsAfterFailureAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested || IsDryRunMode())
                return;

            for (int i = 0; i < _pendingSideResults.Count; i++)
            {
                InspectionTarget target = _pendingSideResults[i];
                if (target == null || target.Die == null ||
                    _sideCompletedPickerIndexes.Contains(target.PickerIndex))
                    continue;

                try
                {
                    Task<SideVisionResult> pendingTask;
                    if (_sideFinalResultTasks.TryGetValue(target.PickerIndex, out pendingTask) && pendingTask != null)
                        await pendingTask.ConfigureAwait(false);
                    else
                        await WaitSideInspectionResultAsync(target, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " 실패 정리 중 Side RESULT 회수 예외. pickerNo=" + target.PickerNo +
                        ", error=" + ex.Message + " - Check");
                }
            }
        }

        private void AbandonPendingVisionInspections(string reason)
        {
            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
            {
                int pickerNo = ToPickerNo(_pickedPickerIndexes[i]);
                if (Side == PickerSequenceSide.Front)
                    FrontPicker.AbandonPendingVisionInspection(pickerNo, reason);
                else
                    RearPicker.AbandonPendingVisionInspection(pickerNo, reason);
            }
        }

        private async Task<int> MoveSideXAndVision0PositionAsync(InspectionTarget target, CancellationToken ct)
        {
            int fixedYResult = VerifyInspectionFixedY("Side PickerX 이동 전", target);
            if (fixedYResult != 0)
                return fixedYResult;

            var pickerTargets = new Dictionary<PickerAxis, double>();
            pickerTargets[PickerAxis.PickerX] = target.X;

            Task<int> pickerTask = MovePickerXTThenYAndVerifyAsync(
                pickerTargets,
                "Bottom/Side 통합 Side X (PickerY 고정)",
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

            fixedYResult = VerifyInspectionFixedY("Side PickerX 및 VisionY 0도 이동 후", target);
            if (fixedYResult != 0)
                return fixedYResult;

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

            return VerifyInspectionFixedY("Side T 90도 및 VisionY 90도 이동 후", target);
        }

        private async Task<int> MoveSideVisionProcessPositionAsync(InspectionTarget target, int angleDeg, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return Fail("PICKER-BOTTOM-SIDE-VISION-UNIT", "Vision", "Side 검사 카메라 이동 실패. VisionUnit을 찾을 수 없습니다. angle=" + angleDeg + ", pickerNo=" + target.PickerNo);

                Task<int> frontTask = MoveSideVisionAxisProcessPositionAsync(
                    vision, target, target.FrontSideVision, angleDeg);
                Task<int> rearTask = MoveSideVisionAxisProcessPositionAsync(
                    vision, target, target.RearSideVision, angleDeg);

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
            int angleDeg)
        {
            if (vision == null || target == null || cameraTarget == null)
                return -1;

            double targetY = angleDeg == 90 ? cameraTarget.Process90Y : cameraTarget.Process0Y;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " SideVisionY 절대식 이동. " +
                "pickerSide=" + Side +
                ", cameraSide=" + cameraTarget.CameraSide +
                ", axis=" + cameraTarget.Axis +
                ", pickerNo=" + target.PickerNo +
                ", die=" + target.Die.DieId +
                ", angle=" + angleDeg +
                ", finalY=" + targetY.ToString("F6") +
                ", offsetApplied=" + target.SideCorrectionValid +
                ", sourceDie=" + (target.SideCorrectionSourceDieId ?? string.Empty) +
                ", " + (target.SideVisionCalc != null ? target.SideVisionCalc.BuildTermLogText() : "calc=null") +
                " - Start");

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
            return item.IsAtTargetPosition(targetY, tolerance);
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

            int fixedYResult = VerifyInspectionFixedY("Side " + angleDeg + "도 REQ 직전", target);
            if (fixedYResult != 0)
                return false;

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
        private bool IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel channel)
        {
            return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(channel);
        }

        private bool IsDisconnectedSyntheticBottomResult(BottomVisionOffset result)
        {
            if (result == null ||
                IsVisionConnected(QMC.CDT320.VisionComm.AutoVisionChannel.BottomInspection) ||
                QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive())
            {
                return false;
            }

            AppSettings settings = AppSettingsStore.Current;
            bool simulationOrDryRun = settings != null &&
                                      (settings.SimulationMode ||
                                       settings.BypassHardware ||
                                       settings.DryRunMode);
            if (!simulationOrDryRun)
                return false;

            string raw = result.Raw ?? string.Empty;
            return raw.StartsWith("SIMULATION:", StringComparison.OrdinalIgnoreCase) ||
                   raw.StartsWith("BYPASS:", StringComparison.OrdinalIgnoreCase);
        }

        private void BindDisconnectedSyntheticBottomResultContext(
            BottomShot bottomShot,
            BottomVisionOffset bottomResult)
        {
            if (bottomShot == null ||
                bottomShot.Target == null ||
                bottomShot.Target.Die == null ||
                !IsDisconnectedSyntheticBottomResult(bottomResult))
            {
                return;
            }

            InspectionTarget target = bottomShot.Target;
            string targetDieId = target.Die.DieId ?? string.Empty;
            bool alreadyBound =
                bottomResult.PickerNo == target.PickerNo &&
                string.Equals(bottomResult.DieId, targetDieId, StringComparison.Ordinal) &&
                bottomResult.DieIndex == target.Die.InputSequenceNo &&
                !string.IsNullOrWhiteSpace(bottomResult.RequestId) &&
                !string.IsNullOrWhiteSpace(bottomResult.GroupId);
            if (alreadyBound)
                return;

            string correlationSuffix = Guid.NewGuid().ToString("N");
            bottomResult.PickerNo = target.PickerNo;
            bottomResult.DieId = targetDieId;
            bottomResult.DieIndex = target.Die.InputSequenceNo;
            bottomResult.RequestId = "SIM-BOTTOM-REQ-" + correlationSuffix;
            bottomResult.GroupId = "SIM-BOTTOM-GROUP-" + correlationSuffix;

            if (bottomResult.Values == null)
                bottomResult.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bottomResult.Values["request_id"] = bottomResult.RequestId;
            bottomResult.Values["group_id"] = bottomResult.GroupId;
            bottomResult.Values["die_id"] = bottomResult.DieId;
            bottomResult.Values["die_index"] = bottomResult.DieIndex.ToString();

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom Vision 미연결 합성 RESULT에 요청 Material 상관관계를 연결합니다. " +
                "side=" + Side +
                ", pickerNo=" + target.PickerNo +
                ", die=" + targetDieId +
                ", dieIndex=" + target.Die.InputSequenceNo +
                ", requestId=" + bottomResult.RequestId +
                ", groupId=" + bottomResult.GroupId + " - Check");
        }

        // 현재 기준: Side 결과를 timeout까지 기다리며, null이면 상위에서 알람 정지한다.
        private async Task<SideVisionResult> WaitSideInspectionResultAsync(InspectionTarget target, CancellationToken ct)
        {
            int timeoutMs = ResolveVisionInspectionTimeout();
            return Side == PickerSequenceSide.Front
                ? await FrontPicker.WaitSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitSideInspectionResultAsync(target.PickerNo, timeoutMs, ct).ConfigureAwait(false);
        }

        private void EnsureDeferredFinalResultLifetime(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_deferredResultCancellation == null)
                _deferredResultCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        }

        private void CancelDeferredFinalResultTasks()
        {
            CancellationTokenSource source = _deferredResultCancellation;
            _deferredResultCancellation = null;
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                source.Dispose();
            }
        }

        public void ReleaseDeferredFinalResultResources()
        {
            CancelDeferredFinalResultTasks();
        }

        private async Task<BottomVisionOffset> ReceiveBottomFinalResultAsync(BottomShot shot, CancellationToken ct)
        {
            try
            {
                if (shot == null || shot.Target == null)
                    return null;

                int timeoutMs = ResolveVisionInspectionTimeout();
                BottomVisionOffset result = Side == PickerSequenceSide.Front
                    ? await FrontPicker.WaitBottomInspectionFinalResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false)
                    : await RearPicker.WaitBottomInspectionFinalResultAsync(shot.Target.PickerNo, timeoutMs, ct).ConfigureAwait(false);

                ApplyBypassBottomFinalResultContext(shot, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom 최종 RESULT 병렬 수집 중 예외가 발생했습니다. " +
                    "pickerNo=" + (shot != null && shot.Target != null ? shot.Target.PickerNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                return null;
            }
        }

        // 비전 바이패스(UseVision=false) 경로의 시뮬 결과는 요청 문맥 스탬핑(ApplyBottomRequestContext)을
        // 거치지 않아 DieId=""/DieIndex=-1/RequestId·GroupId 빈 값으로 온다(ToBottomVisionOffset 하드코딩).
        // place 최종 검증(PICKER-PLACE-BOTTOM-DIE-MISMATCH 등)이 시뮬 런에서 오탐하지 않도록
        // 바이패스 모드에서만 shot의 촬영 대상 문맥으로 채운다. 실비전 결과(DieId 보유)는 손대지
        // 않으므로 place 대상과 촬영 대상이 실제로 다른 경우의 검출력은 그대로 유지된다.
        private void ApplyBypassBottomFinalResultContext(BottomShot shot, BottomVisionOffset result)
        {
            if (result == null || shot == null || shot.Target == null || shot.Target.Die == null)
                return;
            if (!string.IsNullOrWhiteSpace(result.DieId))
                return;

            AppSettings settings = AppSettingsStore.Current;
            if (settings == null || settings.UseVision)
                return;

            result.DieId = shot.Target.Die.DieId ?? string.Empty;
            result.DieIndex = shot.Target.Die.InputSequenceNo;
            if (string.IsNullOrWhiteSpace(result.RequestId))
                result.RequestId = "SIM-BOTTOM-P" + shot.Target.PickerNo + "-" + result.DieId;
            if (string.IsNullOrWhiteSpace(result.GroupId))
                result.GroupId = result.RequestId;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " 비전 바이패스 모드: Bottom 최종 RESULT에 촬영 대상 문맥을 채웠습니다. " +
                "pickerNo=" + shot.Target.PickerNo +
                ", die=" + result.DieId +
                ", dieIndex=" + result.DieIndex +
                ", requestId=" + result.RequestId + " - Check");
        }

        public async Task<int> WaitBottomFinalBeforePlaceMoveAsync(
            int pickerNo,
            string dieId,
            double correctionLimitMm,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int pickerIndex = ToPickerIndex(pickerNo);
            BottomShot bottomShot = FindBottomShot(pickerIndex);
            if (bottomShot == null || bottomShot.Target == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-RESULT-HANDLE", "Vision",
                    "Place 이동 전에 확인할 Bottom 최종 RESULT 항목이 없습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + ".");
            }
            if (!bottomShot.MResultApplied || bottomShot.FinalResultTask == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-RESULT-TASK", "Vision",
                    "Place 이동 전에 기다릴 Bottom 최종 RESULT Task가 준비되지 않았습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + ".");
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Place 목표 이동 전 해당 Picker Bottom 최종 RESULT와 보정값을 확인합니다. " +
                "pickerNo=" + pickerNo +
                ", die=" + (dieId ?? string.Empty) +
                ", correctionLimitMm=" + correctionLimitMm.ToString("F6") +
                ", bottomReady=" + bottomShot.FinalResultTask.IsCompleted + " - Start");

            BottomVisionOffset bottomResult = await AwaitSharedResultWithCancellationAsync(
                bottomShot.FinalResultTask,
                ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            int result = ValidateAndApplyBottomFinalResult(
                bottomShot,
                bottomResult,
                pickerNo,
                dieId,
                correctionLimitMm);
            if (result != 0)
                return result;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Place 목표 이동 전 Bottom 보정값 확인 완료. " +
                "pickerNo=" + pickerNo +
                ", die=" + dieId +
                ", itemOffsetX=" + bottomResult.BottomItemOffsetX.ToString("F6") +
                ", itemOffsetY=" + bottomResult.BottomItemOffsetY.ToString("F6") +
                ", bottomJudgmentOk=" + bottomResult.IsOk + " - Ok");
            return 0;
        }

        public BottomVisionOffset GetValidatedBottomPlaceResult(int pickerNo, string dieId)
        {
            BottomShot bottomShot = FindBottomShot(ToPickerIndex(pickerNo));
            BottomVisionOffset bottomResult = bottomShot != null ? bottomShot.ValidatedPlaceResult : null;
            if (bottomShot == null || bottomShot.Target == null || bottomResult == null ||
                !bottomShot.FinalResultApplied)
                return null;
            if (bottomShot.Target.Die == null ||
                !string.Equals(bottomShot.Target.Die.DieId, dieId, StringComparison.Ordinal) ||
                !string.Equals(bottomResult.DieId, dieId, StringComparison.Ordinal) ||
                bottomResult.PickerNo != pickerNo)
                return null;

            return bottomResult;
        }

        private int ValidateAndApplyBottomFinalResult(
            BottomShot bottomShot,
            BottomVisionOffset bottomResult,
            int pickerNo,
            string dieId,
            double maxAbsCorrectionMm)
        {
            if (bottomShot == null || bottomShot.Target == null || bottomShot.Target.Die == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-RESULT-TARGET", "Material",
                    "Bottom 최종 RESULT의 요청 대상 정보가 없습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + ".");
            }
            if (bottomResult == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-RESULT-MISSING", "Vision",
                    "Place 이동을 차단합니다. Bottom 최종 RESULT가 수신되지 않았습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + ".");
            }

            BindDisconnectedSyntheticBottomResultContext(bottomShot, bottomResult);

            if (!IsValidBottomPlaceCorrectionLimit(maxAbsCorrectionMm))
            {
                return Fail("PICKER-PLACE-BOTTOM-OFFSET-LIMIT", "Vision",
                    "Bottom Place 보정 허용 범위가 올바르지 않습니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + (dieId ?? string.Empty) +
                    ", maxAbsCorrectionMm=" + maxAbsCorrectionMm.ToString("F6") + ".");
            }

            string targetDieId = bottomShot.Target.Die.DieId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dieId) ||
                !string.Equals(targetDieId, dieId, StringComparison.Ordinal) ||
                !string.Equals(bottomResult.DieId, dieId, StringComparison.Ordinal))
            {
                return Fail("PICKER-PLACE-BOTTOM-DIE-MISMATCH", "Material",
                    "Place 대상과 Bottom 최종 RESULT 대상 Die가 일치하지 않습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", placeDie=" + (dieId ?? string.Empty) +
                    ", bottomTargetDie=" + targetDieId +
                    ", bottomResultDie=" + (bottomResult.DieId ?? string.Empty) + ".");
            }
            if (bottomResult.PickerNo != pickerNo)
            {
                return Fail("PICKER-PLACE-BOTTOM-PICKER-MISMATCH", "Vision",
                    "Place 대상 Picker와 Bottom 최종 RESULT Picker가 일치하지 않습니다. " +
                    "requestedPickerNo=" + pickerNo +
                    ", resultPickerNo=" + bottomResult.PickerNo +
                    ", die=" + dieId + ".");
            }
            if (bottomResult.DieIndex != bottomShot.Target.Die.InputSequenceNo)
            {
                return Fail("PICKER-PLACE-BOTTOM-DIE-INDEX-MISMATCH", "Vision",
                    "Bottom 최종 RESULT DieIndex가 요청 Material과 일치하지 않습니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", expectedDieIndex=" + bottomShot.Target.Die.InputSequenceNo +
                    ", actualDieIndex=" + bottomResult.DieIndex + ".");
            }
            if (string.IsNullOrWhiteSpace(bottomResult.RequestId) ||
                string.IsNullOrWhiteSpace(bottomResult.GroupId))
            {
                return Fail("PICKER-PLACE-BOTTOM-CORRELATION", "Vision",
                    "Bottom 최종 RESULT 상관관계 ID가 없습니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", requestId=" + (bottomResult.RequestId ?? string.Empty) +
                    ", groupId=" + (bottomResult.GroupId ?? string.Empty) + ".");
            }
            if (!bottomResult.HasMeasureValid || !bottomResult.MeasureValid)
            {
                return Fail("PICKER-PLACE-BOTTOM-MEASURE-INVALID", "Vision",
                    "Bottom 최종 RESULT measure_valid 조건을 만족하지 않아 Place 이동을 차단합니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", present=" + bottomResult.HasMeasureValid +
                    ", value=" + bottomResult.MeasureValid + ".");
            }
            if (!bottomResult.HasBottomItemOffsetX ||
                !bottomResult.HasBottomItemOffsetY ||
                !bottomResult.HasBottomItemOffsetXPass ||
                !bottomResult.HasBottomItemOffsetYPass ||
                !bottomResult.BottomItemOffsetXPass ||
                !bottomResult.BottomItemOffsetYPass ||
                !IsFiniteCorrectionValue(bottomResult.BottomItemOffsetX) ||
                !IsFiniteCorrectionValue(bottomResult.BottomItemOffsetY))
            {
                return Fail("PICKER-PLACE-BOTTOM-ITEM-OFFSET", "Vision",
                    "Bottom 최종 RESULT item offset 값 또는 pass 조건을 만족하지 않아 Place 이동을 차단합니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", hasX=" + bottomResult.HasBottomItemOffsetX +
                    ", hasY=" + bottomResult.HasBottomItemOffsetY +
                    ", hasXPass=" + bottomResult.HasBottomItemOffsetXPass +
                    ", hasYPass=" + bottomResult.HasBottomItemOffsetYPass +
                    ", xPass=" + bottomResult.BottomItemOffsetXPass +
                    ", yPass=" + bottomResult.BottomItemOffsetYPass +
                    ", offsetX=" + bottomResult.BottomItemOffsetX.ToString("F6") +
                    ", offsetY=" + bottomResult.BottomItemOffsetY.ToString("F6") + ".");
            }
            if (Math.Abs(bottomResult.BottomItemOffsetX) > maxAbsCorrectionMm ||
                Math.Abs(bottomResult.BottomItemOffsetY) > maxAbsCorrectionMm)
            {
                return Fail("PICKER-PLACE-BOTTOM-ITEM-OFFSET-RANGE", "Vision",
                    "Bottom 최종 RESULT item offset이 Place 보정 허용 범위를 벗어나 이동을 차단합니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", offsetX=" + bottomResult.BottomItemOffsetX.ToString("F6") +
                    ", offsetY=" + bottomResult.BottomItemOffsetY.ToString("F6") +
                    ", allowedAbsMaxMm=" + maxAbsCorrectionMm.ToString("F6") + ".");
            }

            DieMaterial currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            if (currentDie == null || !string.Equals(currentDie.DieId, dieId, StringComparison.Ordinal))
            {
                return Fail("PICKER-PLACE-BOTTOM-CURRENT-DIE", "Material",
                    "Bottom 최종 RESULT 적용 직전 Picker 제품이 변경되었습니다. " +
                    "pickerNo=" + pickerNo +
                    ", expectedDie=" + dieId +
                    ", currentDie=" + (currentDie != null ? currentDie.DieId : string.Empty) + ".");
            }

            if (!bottomShot.FinalResultApplied)
            {
                ApplyBottomInspectionResult(bottomShot.Target, bottomResult);
                bottomShot.FinalResultApplied = true;
            }

            bottomShot.ValidatedPlaceResult = bottomResult;
            bottomShot.ValidatedPlaceCorrectionLimitMm = maxAbsCorrectionMm;
            return 0;
        }

        private static bool IsValidBottomPlaceCorrectionLimit(double value)
        {
            return IsFiniteCorrectionValue(value) && value >= 0.0;
        }

        private static async Task<T> AwaitSharedResultWithCancellationAsync<T>(
            Task<T> sharedTask,
            CancellationToken ct)
        {
            if (sharedTask == null)
                return default(T);
            if (sharedTask.IsCompleted || !ct.CanBeCanceled)
                return await sharedTask.ConfigureAwait(false);

            var cancellationSignal = new TaskCompletionSource<bool>();
            using (ct.Register(
                state => ((TaskCompletionSource<bool>)state).TrySetResult(true),
                cancellationSignal))
            {
                Task completedTask = await Task.WhenAny(
                    sharedTask,
                    cancellationSignal.Task).ConfigureAwait(false);
                if (!object.ReferenceEquals(completedTask, sharedTask))
                    ct.ThrowIfCancellationRequested();
            }

            return await sharedTask.ConfigureAwait(false);
        }

        private async Task<SideVisionResult> ReceiveSideFinalResultAsync(InspectionTarget target, CancellationToken ct)
        {
            try
            {
                if (target == null)
                    return null;

                return await WaitSideInspectionResultAsync(target, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Side 최종 RESULT 병렬 수집 중 예외가 발생했습니다. " +
                    "pickerNo=" + (target != null ? target.PickerNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                return null;
            }
        }

        private InspectionTarget FindPendingSideTarget(int pickerIndex)
        {
            for (int i = 0; i < _pendingSideResults.Count; i++)
            {
                InspectionTarget target = _pendingSideResults[i];
                if (target != null && target.PickerIndex == pickerIndex)
                    return target;
            }

            return null;
        }

        public async Task<int> WaitFinalResultsBeforePlaceDownAsync(
            int pickerNo,
            string dieId,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int pickerIndex = ToPickerIndex(pickerNo);
            BottomShot bottomShot = FindBottomShot(pickerIndex);
            InspectionTarget sideTarget = FindPendingSideTarget(pickerIndex);
            if (bottomShot == null || bottomShot.Target == null || sideTarget == null)
            {
                return Fail("PICKER-PLACE-INSPECTION-RESULT-HANDLE", "Vision",
                    "Place 하강 전에 확인할 Bottom/Side 결과 항목이 없습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + (dieId ?? string.Empty) + ".");
            }

            string expectedDieId = bottomShot.Target.Die != null ? bottomShot.Target.Die.DieId : string.Empty;
            if (string.IsNullOrWhiteSpace(dieId) ||
                !string.Equals(expectedDieId, dieId, StringComparison.Ordinal) ||
                sideTarget.Die == null ||
                !string.Equals(sideTarget.Die.DieId, dieId, StringComparison.Ordinal))
            {
                return Fail("PICKER-PLACE-INSPECTION-DIE-MISMATCH", "Material",
                    "Place 대상과 지연 Bottom/Side RESULT 대상이 일치하지 않습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", placeDie=" + (dieId ?? string.Empty) +
                    ", bottomDie=" + expectedDieId +
                    ", sideDie=" + (sideTarget.Die != null ? sideTarget.Die.DieId : string.Empty) + ".");
            }

            Task<SideVisionResult> sideFinalTask;
            if (!bottomShot.MResultApplied || bottomShot.FinalResultTask == null ||
                !_sideFinalResultTasks.TryGetValue(pickerIndex, out sideFinalTask) || sideFinalTask == null)
            {
                return Fail("PICKER-PLACE-INSPECTION-RESULT-TASK", "Vision",
                    "Place 하강 전에 기다릴 Picker별 Bottom/Side 최종 RESULT Task가 준비되지 않았습니다. " +
                    "side=" + Side + ", pickerNo=" + pickerNo + ", die=" + dieId + ".");
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Place PickerZ 하강 직전 해당 Picker Bottom/Side 최종 RESULT 배리어 확인. " +
                "pickerNo=" + pickerNo + ", die=" + dieId +
                ", bottomReady=" + bottomShot.FinalResultTask.IsCompleted +
                ", sideReady=" + sideFinalTask.IsCompleted + " - Start");

            BottomVisionOffset bottomResult = await AwaitSharedResultWithCancellationAsync(
                bottomShot.FinalResultTask,
                ct).ConfigureAwait(false);
            SideVisionResult sideResult = await AwaitSharedResultWithCancellationAsync(
                sideFinalTask,
                ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            if (bottomResult == null || sideResult == null)
            {
                return Fail("PICKER-PLACE-INSPECTION-RESULT-MISSING", "Vision",
                    "Place PickerZ 하강을 차단합니다. Bottom/Side 최종 RESULT가 모두 수신되지 않았습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", bottomReceived=" + (bottomResult != null) +
                    ", sideReceived=" + (sideResult != null) + ".");
            }

            if (bottomResult.PickerNo != pickerNo ||
                (sideResult.PickerNo != 0 && sideResult.PickerNo != pickerNo))
            {
                return Fail("PICKER-PLACE-INSPECTION-RESULT-PICKER-MISMATCH", "Vision",
                    "Place 대상 Picker와 Bottom/Side 최종 RESULT Picker가 일치하지 않습니다. " +
                    "requestedPickerNo=" + pickerNo +
                    ", bottomPickerNo=" + bottomResult.PickerNo +
                    ", sidePickerNo=" + sideResult.PickerNo +
                    ", die=" + dieId + ".");
            }

            // 두 RESULT를 모두 확보한 뒤 단일 경로에서 Bottom -> Side 순으로 적용해
            // Side NG 또는 Bottom NG가 나중의 Good 결과로 덮이지 않게 한다.
            double bottomCorrectionLimit = bottomShot.ValidatedPlaceResult != null
                ? bottomShot.ValidatedPlaceCorrectionLimitMm
                : DefaultBottomPlaceCorrectionLimitMm;
            int bottomValidationResult = ValidateAndApplyBottomFinalResult(
                bottomShot,
                bottomResult,
                pickerNo,
                dieId,
                bottomCorrectionLimit);
            if (bottomValidationResult != 0)
                return bottomValidationResult;

            if (!_sideCompletedPickerIndexes.Contains(pickerIndex))
            {
                ApplySideInspectionResult(sideTarget, sideResult, sideResult);
                _sideCompletedPickerIndexes.Add(pickerIndex);
            }

            DieMaterial currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
            DieResult expectedDieResult = bottomResult.IsOk && sideResult.IsAllOk
                ? DieResult.Good
                : DieResult.NG;
            if (currentDie == null ||
                !string.Equals(currentDie.DieId, dieId, StringComparison.Ordinal) ||
                !HasInspectionResult(currentDie, "Bottom") ||
                !HasInspectionResult(currentDie, "Side0") ||
                !HasInspectionResult(currentDie, "Side90") ||
                currentDie.Result != expectedDieResult)
            {
                return Fail("PICKER-PLACE-INSPECTION-RESULT-APPLY", "Material",
                    "Bottom/Side 최종 RESULT를 수신했지만 Material 판정 반영 검증에 실패했습니다. " +
                    "pickerNo=" + pickerNo +
                    ", die=" + dieId +
                    ", expectedResult=" + expectedDieResult +
                    ", actualDie=" + (currentDie != null ? currentDie.DieId : string.Empty) +
                    ", actualResult=" + (currentDie != null ? currentDie.Result.ToString() : "null") +
                    ", bottomDone=" + HasInspectionResult(currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(currentDie, "Side90") + ".");
            }

            EnqueueFinalBottomAndSideResult(currentDie);

            if (!bottomShot.FinalGateTactRecorded)
            {
                RecordDetailedTactRecord(
                    TactTimeCategory.Vision,
                    "Bottom Camera Inspect",
                    "RESULT",
                    bottomShot.Target,
                    bottomShot.InspectStartedAt,
                    TactTimeResult.Ok,
                    string.Empty,
                    "Bottom/Side final results completed before place down. bottomOk=" + bottomResult.IsOk +
                    ", sideOk=" + sideResult.IsAllOk);
                bottomShot.FinalGateTactRecorded = true;
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Place PickerZ 하강 허용. 해당 Picker Bottom/Side 최종 RESULT 수신 및 판정 반영 완료. " +
                "pickerNo=" + pickerNo +
                ", die=" + dieId +
                ", bottomOk=" + bottomResult.IsOk +
                ", sideOk=" + sideResult.IsAllOk + " - Ok");
            return 0;
        }

        private void EnqueueFinalBottomAndSideResult(DieMaterial die)
        {
            try
            {
                DieInspectionRecord bottomRecord = FindInspectionRecord(die, "Bottom");
                if (bottomRecord == null)
                {
                    WriteLog("PickerBottomAndSideInspectionSequence",
                        Name + " Bottom/Side 최종 RESULT 저장을 생략했습니다. Bottom Material record가 없습니다. die=" +
                        (die != null ? die.DieId : string.Empty) + " - Failed");
                    return;
                }

                MaterialSnapshot state = MaterialStateService.State;
                VisionInspectionResultFileWriter.EnqueueBottomResult(
                    state != null ? state.RecipeName : string.Empty,
                    MaterialStateService.GetProductionLotId(),
                    die,
                    bottomRecord);
            }
            catch (Exception ex)
            {
                // 고객 결과 파일 저장 실패는 검사/Place 안전 배리어를 해제하거나 모션 결과를 바꾸지 않는다.
                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " Bottom/Side 최종 RESULT 저장 enqueue 실패. die=" +
                    (die != null ? die.DieId : string.Empty) + ", error=" + ex.Message + " - Failed");
            }
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
                    target.Die.DieId,
                    target.Die.WaferID_Input);
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
                " | SideVisionCalc: " +
                (target.SideVisionCalc != null ? target.SideVisionCalc.BuildTermLogText() : "null") +
                " | FrontSideVisionY0.final=" + target.FrontSideVision.Process0Y.ToString("F6") +
                ", FrontSideVisionY90.final=" + target.FrontSideVision.Process90Y.ToString("F6") +
                ", RearSideVisionY0.final=" + target.RearSideVision.Process0Y.ToString("F6") +
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
                // Z+T 확장(사용자 승인 2026-07-26, 1-B(a)): T 회전도 같은 pending으로 선행한다.
                QueuePendingBottomZTDown(nextPickerIndex, zTarget, nextTarget.T0);
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
            QueuePendingBottomZTDown(pickerIndex, target, double.NaN);
        }

        // Z+T 선행(사용자 승인 2026-07-26): tTarget이 NaN이면 기존 Z 단독 선행과 동일.
        private void QueuePendingBottomZTDown(int pickerIndex, double target, double tTarget)
        {
            for (int i = 0; i < _pendingBottomZDowns.Count; i++)
            {
                if (_pendingBottomZDowns[i].PickerIndex == pickerIndex)
                    return;
            }

            _pendingBottomZDowns.Add(new PendingBottomZDown
            {
                PickerIndex = pickerIndex,
                Target = target,
                HasT = !double.IsNaN(tTarget),
                TTarget = double.IsNaN(tTarget) ? 0.0 : tTarget
            });
        }

        private bool HasPendingBottomZDown(int pickerIndex)
        {
            for (int i = 0; i < _pendingBottomZDowns.Count; i++)
            {
                if (_pendingBottomZDowns[i].PickerIndex == pickerIndex)
                    return true;
            }

            return false;
        }

        // 실패 경로 정리: 미소비 선행 Z/T Task를 관찰(observe)만 하고 목록을 비운다 —
        // 미관찰 Task 예외 전파 방지. 축 정지/복귀는 기존 실패 처리(Z Avoid 복귀 경로)가 담당.
        private void ObservePendingBottomZDownTasks(string reason)
        {
            for (int i = _pendingBottomZDowns.Count - 1; i >= 0; i--)
            {
                PendingBottomZDown pending = _pendingBottomZDowns[i];
                ObservePendingMoveTask(pending.MoveTask);
                ObservePendingMoveTask(pending.TMoveTask);
                _pendingBottomZDowns.RemoveAt(i);
            }

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " 선행 Z+T pending 정리. reason=" + (reason ?? "-") + " - Check");
        }

        private static void ObservePendingMoveTask(Task<int> moveTask)
        {
            if (moveTask == null || moveTask.IsCompleted)
                return;

            moveTask.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
        }

        private void StartPendingBottomZDownCommand(string description, int pickerIndex)
        {
            for (int i = _pendingBottomZDowns.Count - 1; i >= 0; i--)
            {
                PendingBottomZDown pending = _pendingBottomZDowns[i];
                if (pending.PickerIndex != pickerIndex || pending.MoveTask != null || pending.TMoveTask != null)
                    continue;

                PickerAxis axis = GetPickerZAxis(pending.PickerIndex);
                bool zSkip = CanSkipPickerMoveCommand(axis, pending.Target);
                bool tSkip = true;
                if (pending.HasT)
                {
                    PickerAxis tAxis = GetPickerTAxis(pending.PickerIndex);
                    tSkip = CanSkipPickerMoveCommand(tAxis, pending.TTarget);
                    if (!tSkip)
                    {
                        pending.TMoveTask = MovePickerAxisCommandAsync(
                            tAxis,
                            pending.TTarget,
                            BuildPickerTargetName("DieBottomTPreMove", pending.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom");
                    }
                }

                if (zSkip && tSkip)
                {
                    _pendingBottomZDowns.RemoveAt(i);
                    continue;
                }

                if (!zSkip)
                {
                    pending.MoveTask = MovePickerAxisCommandAsync(
                        axis,
                        pending.Target,
                        BuildPickerTargetName("DieBottomZPreDown", pending.PickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom");
                }

                WriteLog("PickerBottomAndSideInspectionSequence",
                    Name + " " + description + " 명령 시작. pickerNo=" + ToPickerNo(pending.PickerIndex) +
                    ", target=" + pending.Target.ToString("0.###") +
                    ", zSkip=" + zSkip +
                    ", t=" + (pending.HasT ? pending.TTarget.ToString("0.###") : "-") +
                    ", tSkip=" + tSkip + " - Ok");
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

                // Z join — 실패 시 동기 1회 재시도(사용자 승인 2026-07-26) 후 기존 Fail 코드.
                PickerAxis axis = GetPickerZAxis(pending.PickerIndex);
                int zResult = await JoinPendingAxisMoveAsync(
                    axis,
                    pending.Target,
                    pending.MoveTask,
                    "DieBottomZPreDown",
                    pending.PickerIndex,
                    ct).ConfigureAwait(false);
                if (zResult != 0)
                    return zResult;

                // T join(Z+T 확장) — 동일한 재시도 정책. 기존 Z-PREDOWN Fail 코드 재사용(신규 코드 금지).
                if (pending.HasT)
                {
                    PickerAxis tAxis = GetPickerTAxis(pending.PickerIndex);
                    int tResult = await JoinPendingAxisMoveAsync(
                        tAxis,
                        pending.TTarget,
                        pending.TMoveTask,
                        "DieBottomTPreMove",
                        pending.PickerIndex,
                        ct).ConfigureAwait(false);
                    if (tResult != 0)
                        return tResult;
                }

                _pendingBottomZDowns.RemoveAt(i);
            }

            return 0;
        }

        // 선행 축 1개의 join: 비동기 명령 결과 확인 → (미발행 시 발행) → 완료 대기 → 위치 확인.
        // 어느 단계든 실패하면 동기 1회 재시도(MovePickerAxisAndVerifyAsync — 인터락 대기+스케일 포함)
        // 후에도 실패면 기존 PICKER-BOTTOM-SIDE-Z-PREDOWN-* Fail 코드로 종료한다.
        private async Task<int> JoinPendingAxisMoveAsync(
            PickerAxis axis,
            double target,
            Task<int> moveTask,
            string targetNamePrefix,
            int pickerIndex,
            CancellationToken ct)
        {
            string retryReason = null;

            if (moveTask != null)
            {
                int commandResult = await SequenceAwaiter.AwaitAsync(moveTask, -1, ct).ConfigureAwait(false);
                if (commandResult != 0)
                    retryReason = "명령 실패 result=" + commandResult;
            }
            else if (!CanSkipPickerMoveCommand(axis, target))
            {
                int commandResult = await SequenceAwaiter.AwaitAsync(
                    MovePickerAxisCommandAsync(
                        axis,
                        target,
                        BuildPickerTargetName(targetNamePrefix, pickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom"),
                    -1,
                    ct).ConfigureAwait(false);
                if (commandResult != 0)
                    retryReason = "지연 발행 명령 실패 result=" + commandResult;
            }

            if (retryReason == null)
            {
                int waitCode = await WaitPickerAxisMoveDoneAsync(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    retryReason = "완료 대기 실패 waitCode=" + waitCode;
                else if (!IsPickerAxisInPosition(axis, target))
                    retryReason = "최종 위치 확인 실패";
            }

            if (retryReason == null)
                return 0;

            WriteLog("PickerBottomAndSideInspectionSequence",
                Name + " Bottom 선행 구동 join 실패 — 동기 1회 재시도합니다. axis=" + axis +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", target=" + target.ToString("0.###") +
                ", reason=" + retryReason + " - Check");

            int retryResult = await MovePickerAxisAndVerifyAsync(
                axis,
                target,
                "Bottom 선행 구동 동기 재시도",
                ct,
                BuildPickerTargetName(targetNamePrefix, pickerIndex) + ";PickerProcess=BottomSide;PickerPhase=InspectionZHold;InspectionContinuous;From=Bottom;To=Bottom").ConfigureAwait(false);
            if (retryResult == 0 && IsPickerAxisInPosition(axis, target))
                return 0;

            return Fail("PICKER-BOTTOM-SIDE-Z-PREDOWN-CMD", Name,
                "예약된 Bottom 선행 구동 실패(동기 재시도 포함). axis=" + axis +
                ", retryResult=" + retryResult +
                ", reason=" + retryReason +
                ", pickerNo=" + ToPickerNo(pickerIndex) + ". " + BuildPickerAxisState(axis, target));
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

                if (CanSkipPickerMoveCommand(GetPickerZAxis(pending.PickerIndex), pending.Target))
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
                else if (!CanSkipPickerMoveCommand(axis, pending.Target))
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

                int waitCode = await WaitPickerAxisMoveDoneAsync(axis, pending.Target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("PICKER-BOTTOM-SIDE-Z-AVOID-WAIT", Name, "예약된 PickerZ Avoid 완료 대기 실패. waitCode=" + waitCode + ". " + BuildPickerAxisState(axis, pending.Target));

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

                if (CanSkipPickerMoveCommand(GetPickerTAxis(pending.PickerIndex), pending.Target))
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

                int waitCode = await WaitPickerAxisMoveDoneAsync(axis, pending.Target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("PICKER-BOTTOM-SIDE-T0-WAIT", Name, "예약된 PickerT 0도 복귀 완료 대기 실패. waitCode=" + waitCode + ". " + BuildPickerAxisState(axis, pending.Target));

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
                if (!_pendingBottomShots[i].MResultApplied)
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

        private static DieInspectionRecord FindInspectionRecord(DieMaterial die, string inspectionType)
        {
            if (die == null || die.Inspections == null || string.IsNullOrWhiteSpace(inspectionType))
                return null;

            for (int i = 0; i < die.Inspections.Count; i++)
            {
                DieInspectionRecord record = die.Inspections[i];
                if (record != null &&
                    string.Equals(record.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return record;
                }
            }

            return null;
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
