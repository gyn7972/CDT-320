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
    internal sealed class PickerSideInspectionSequence : PickerSequenceBase<PickerSideInspectionStep>
    {
        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private int _pickerCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private DieMaterial _currentDie;
        private SideVisionResult _side0Result;
        private SideVisionResult _side90Result;
        private bool _side0Started;
        private bool _side90Started;
        private double _targetPickerX;
        private double _targetPickerY;
        private double _targetPickerZ;
        private double _targetPickerT0;
        private double _targetPickerT90;
        private bool _inspectionYPositionReady;
        private SequenceResourceLease _inspectionAreaLease;
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();
        private readonly List<CapturedSideInspection> _capturedSideInspections = new List<CapturedSideInspection>();

        private sealed class CapturedSideInspection
        {
            public int PickerIndex;
            public int PickerNo;
            public DieMaterial Die;
            public SideVisionResult Side0Result;
            public SideVisionResult Side90Result;
            public bool Side0Started;
            public bool Side90Started;
            public double TargetPickerX;
            public double TargetPickerY;
            public double TargetPickerZ;
            public double TargetPickerT0;
            public double TargetPickerT90;
            public bool ResultApplied;
        }

        private sealed class PendingT0Return
        {
            public int PickerIndex;
            public double Target;
            public Task<int> MoveTask;
        }

        public PickerSideInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Inspect, side == PickerSequenceSide.Front ? "FrontPickerSideInspectionSequence" : "RearPickerSideInspectionSequence")
        {
            CurrentStep = PickerSideInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerSideInspectionStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                AbandonPendingVisionInspections("Picker Side 검사 Abort로 미완료 요청을 정리합니다.");
                ReleaseInspectionArea();
                ClearPendingT0Return();
                CurrentStep = PickerSideInspectionStep.Complete;
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
            bool keepCurrentState = false;

            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                {
                    ct.ThrowIfCancellationRequested();
                    int stepResult = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerSideInspectionStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerSideInspectionStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
                    if (result != 0)
                        return result;
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
                return Fail("PICKER-SIDE-EX", Name, "Picker side inspection failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                {
                    if (CurrentStep != PickerSideInspectionStep.Complete)
                    {
                        try
                        {
                            await DrainCapturedSideResultsAfterFailureAsync(ct).ConfigureAwait(false);
                            AbandonPendingVisionInspections("Picker Side 검사 실패/중단으로 미완료 요청을 정리합니다.");
                        }
                        catch (Exception ex)
                        {
                            WriteLog("PickerSideInspectionSequence",
                                Name + " 실패 후 Vision Handle 정리 예외. error=" + ex.Message + " - Check");
                        }
                    }
                    ReleaseInspectionArea();
                }
            }
        }

        private async Task DrainCapturedSideResultsAfterFailureAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested || IsVisionBypassed() || IsDryRunMode())
                return;

            for (int i = 0; i < _capturedSideInspections.Count; i++)
            {
                CapturedSideInspection captured = _capturedSideInspections[i];
                if (captured == null || captured.ResultApplied ||
                    (!captured.Side0Started && !captured.Side90Started) ||
                    (captured.Side0Result != null && captured.Side90Result != null))
                    continue;

                try
                {
                    SideVisionResult result = await WaitSideInspectionResultAsync(captured.PickerNo, ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        if (captured.Side0Result == null)
                            captured.Side0Result = result;
                        if (captured.Side90Result == null)
                            captured.Side90Result = result;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    WriteLog("PickerSideInspectionSequence",
                        Name + " 실패 정리 중 Side RESULT 회수 예외. pickerNo=" + captured.PickerNo +
                        ", error=" + ex.Message + " - Check");
                }
            }
        }

        private void AbandonPendingVisionInspections(string reason)
        {
            var pickerNos = new HashSet<int>();
            for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                pickerNos.Add(ToPickerNo(_pickedPickerIndexes[i]));
            if (_currentPickerNo >= 1 && _currentPickerNo <= 4)
                pickerNos.Add(_currentPickerNo);

            foreach (int pickerNo in pickerNos)
            {
                if (Side == PickerSequenceSide.Front)
                    FrontPicker.AbandonPendingVisionInspection(pickerNo, reason);
                else
                    RearPicker.AbandonPendingVisionInspection(pickerNo, reason);
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                // 유닛 확인
                case PickerSideInspectionStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // 픽업 피커 목록 생성
                case PickerSideInspectionStep.BuildPickedPickerList:
                    return Task.FromResult(BuildPickedPickerList());

                // 전체 피커 Z로 어보이드 이동
                case PickerSideInspectionStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                // Side 검사 진입 전 상대 피커 간섭 상태 확인
                case PickerSideInspectionStep.MoveOppositePickerToAvoidBeforeInspection:
                    return MoveOppositePickerToAvoidBeforeInspectionAsync(ct);

                // 다음 피커 선택
                case PickerSideInspectionStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                // Bottom/다른 존에서 Side 존으로 들어가기 전 Y 어보이드 이동
                case PickerSideInspectionStep.MoveSideEntryYToAvoid:
                    return MoveSideEntryYToAvoidAsync(ct);

                // 사이드 XY 이동
                case PickerSideInspectionStep.MoveSideXToInspection:
                    return MoveSideXToInspectionAsync(ct);

                case PickerSideInspectionStep.MoveSideYToInspection:
                    return MoveSideYToInspectionAsync(ct);

                // 사이드 Z 이동
                case PickerSideInspectionStep.MoveSideZ:
                    return MoveSideZAsync(ct);

                // 사이드 T 0 이동
                case PickerSideInspectionStep.MoveSideT0:
                    return MoveSideT0Async(ct);

                // 사이드 0 검사 요청
                case PickerSideInspectionStep.RequestSide0Inspection:
                    return RequestSide0InspectionAsync(ct);

                // 사이드 T 90 이동
                case PickerSideInspectionStep.MoveSideT90:
                    return MoveSideT90Async(ct);

                // 사이드 90 검사 요청
                case PickerSideInspectionStep.RequestSide90Inspection:
                    return RequestSide90InspectionAsync(ct);

                // 사이드 검사 결과 적용
                case PickerSideInspectionStep.ApplySideInspectionResult:
                    return ApplySideInspectionResultAsync(ct);

                // 사이드 Z 어보이드 후 T 0도 복귀를 다음 검사 동작과 겹치도록 예약
                case PickerSideInspectionStep.MoveSideZToAvoid:
                    return MoveSideZToAvoidAsync(ct);

                // 사이드 검사 종료 후 T를 0도 기준으로 복귀
                case PickerSideInspectionStep.MoveSideTToSafe:
                    return MoveSideTToSafeAsync(ct);

                // Side 전체 검사 완료 후 모든 PickerZ 어보이드 이동
                case PickerSideInspectionStep.MoveAllPickerZToAvoidAfterInspection:
                    return MoveAllPickerZToAvoidAfterInspectionAsync(ct);

                // 사이드 XY로 어보이드 이동
                case PickerSideInspectionStep.MoveSideYToAvoid:
                    return MoveSideYToAvoidAsync(ct);

                case PickerSideInspectionStep.MoveSideXToAvoid:
                    return MoveSideXToAvoidAsync(ct);

                // 다음 피커 또는 완료 선택
                case PickerSideInspectionStep.SelectNextPickerOrComplete:
                    return Task.FromResult(SelectNextPickerOrComplete());

                default:
                    return Task.FromResult(Fail("PICKER-SIDE-STEP", Name, "Unsupported picker side inspection step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerSideInspectionSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerSideInspectionStep.Complete;
                return 0;
            }

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-SIDE-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            CurrentStep = PickerSideInspectionStep.BuildPickedPickerList;
            return 0;
        }

        private int BuildPickedPickerList()
        {
            _pickedPickerIndexes.Clear();
            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerSideInspectionSequence"));

            _pickerCursor = 0;
            _inspectionYPositionReady = false;
            ClearPendingT0Return();
            _capturedSideInspections.Clear();

            if (_pickedPickerIndexes.Count == 0)
            {
                WriteLog("PickerSideInspectionSequence", Name + " skipped because no die exists on picker. - Check");
                CurrentStep = PickerSideInspectionStep.Complete;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AcquireInspectionResourcesAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Side, "SideInspection");

                if (Options != null &&
                    (Options.EnterSideFromBottomInspection || Options.KeepZUntilSideInspectionComplete))
                {
                    WriteLog("PickerSideInspectionSequence",
                        Name + " Bottom->Side 연속 검사를 위해 Side 진입 전 PickerZ 전체 Avoid 이동을 생략합니다. " +
                        "enterFromBottom=" + Options.EnterSideFromBottomInspection +
                        ", keepZUntilSideComplete=" + Options.KeepZUntilSideInspectionComplete + " - Ok");
                    CurrentStep = PickerSideInspectionStep.MoveOppositePickerToAvoidBeforeInspection;
                    return 0;
                }

                result = await MoveAllPickerZToAvoidAndVerifyAsync("side inspection pre all picker Z avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerSideInspectionStep.MoveOppositePickerToAvoidBeforeInspection;
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
                return Fail(
                    "PICKER-SIDE-RESOURCE-EX",
                    Name,
                    "사이드 검사 리소스 점유 또는 진입 준비 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AcquireInspectionResourcesAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_inspectionAreaLease == null)
                {
                    _inspectionAreaLease = await AcquireResourceAsync(
                        SequenceResourceKind.InspectionArea,
                        Name + ":Side",
                        ct).ConfigureAwait(false);
                    if (_inspectionAreaLease == null)
                        return -1;
                }

                // Side 검사는 InspectionArea만 점유한다.
                // Output Place와는 다른 작업 존이므로 OutputPlaceArea를 함께 잡으면
                // 한 Picker가 Place하는 동안 다른 Picker가 다음 검사로 넘어가지 못한다.
                // 실제 축 간섭은 Picker phase gate, Picker Y zone gate, SharedRailX/Encoder 인터락이 최종 방어한다.
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
                return Fail(
                    "PICKER-SIDE-RESOURCE",
                    Name,
                    "사이드 검사 리소스 점유 실패. InspectionArea를 확인하세요. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositePickerToAvoidBeforeInspectionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveOppositePickerToAvoidAndVerifyAsync(
                    "사이드 검사 진입 전 상대 Picker 상태 확인",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "PICKER-SIDE-OPPOSITE-AVOID-EX",
                    Name,
                    "사이드 검사 진입 전 상대 Picker 상태 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }

            CurrentStep = PickerSideInspectionStep.SelectNextPicker;
            return 0;
        }

        private int SelectNextPicker()
        {
            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                if (HasPendingT0Return())
                {
                    CurrentStep = PickerSideInspectionStep.MoveSideTToSafe;
                    return 0;
                }

                CurrentStep = PickerSideInspectionStep.Complete;
                ReleaseInspectionArea();
                return 0;
            }

            _currentPickerIndex = _pickedPickerIndexes[_pickerCursor];
            _currentPickerNo = ToPickerNo(_currentPickerIndex);
            _currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            _side0Result = null;
            _side90Result = null;
            _side0Started = false;
            _side90Started = false;

            if (_currentDie == null)
            {
                return Fail("PICKER-SIDE-MATERIAL-CONTEXT", Name,
                    "Side 검사 대상으로 확정된 Picker의 자재 문맥을 찾을 수 없습니다. " +
                    "pickerIndex=" + _currentPickerIndex +
                    ", pickerNo=" + _currentPickerNo);
            }

            _targetPickerX = ResolvePickerZoneX("DieSidePosition", _currentPickerIndex);
            _targetPickerY = ResolvePickerZoneY("DieSidePosition", _currentPickerIndex);
            _targetPickerZ = ResolveSideInspectionPickerZFromBottomBest(
                _currentPickerIndex,
                _currentPickerNo,
                "PickerSideInspectionSequence");
            _targetPickerT0 = ResolvePickerZoneT("DieSidePosition", _currentPickerIndex);
            _targetPickerT90 = _targetPickerT0 + 90.0;

            _inspectionYPositionReady = IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY);

            bool continuousSideEntry = IsContinuousSideInspectionEntry();
            if (!IsCurrentPickerXInSideZone() &&
                !IsPickerYAtXZoneMoveSafePosition() &&
                !continuousSideEntry)
            {
                CurrentStep = PickerSideInspectionStep.MoveSideEntryYToAvoid;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.MoveSideXToInspection;
            return 0;
        }

        private async Task<int> MoveSideEntryYToAvoidAsync(CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "사이드 진입 전 PickerY 어보이드 전 PickerZ 전체 Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                target,
                "사이드 진입 전 PickerY 어보이드",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            _inspectionYPositionReady = false;
            CurrentStep = PickerSideInspectionStep.MoveSideXToInspection;
            return 0;
        }

        private async Task<int> MoveSideXToInspectionAsync(CancellationToken ct)
        {
            bool currentXInSideZone = IsCurrentPickerXInSideZone();
            bool continuousSideEntry = IsContinuousSideInspectionEntry();
            if (!currentXInSideZone &&
                !IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX) &&
                !IsPickerYAtXZoneMoveSafePosition() &&
                !continuousSideEntry)
            {
                return await MoveSideEntryYToAvoidAsync(ct).ConfigureAwait(false);
            }

            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            if (!_inspectionYPositionReady || !CanSkipPickerMoveCommand(PickerAxis.PickerY, _targetPickerY))
                targets[PickerAxis.PickerY] = _targetPickerY;

            int result = await MoveSideXAndVision0PositionAsync(targets, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            _inspectionYPositionReady = IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY);
            CurrentStep = _inspectionYPositionReady
                ? PickerSideInspectionStep.MoveSideZ
                : PickerSideInspectionStep.MoveSideYToInspection;
            return 0;
        }

        private bool IsEnterSideFromBottomInspection()
        {
            return Options != null && Options.EnterSideFromBottomInspection;
        }

        private bool IsContinuousSideInspectionEntry()
        {
            return Options != null &&
                   (Options.EnterSideFromBottomInspection ||
                    Options.KeepZUntilSideInspectionComplete);
        }

        private string BuildSideMoveTargetName()
        {
            string targetName = BuildPickerTargetName("DieSidePosition", _currentPickerIndex);
            if (!IsEnterSideFromBottomInspection() &&
                (Options == null || !Options.KeepZUntilSideInspectionComplete))
                return AppendAutoProcessCorrectionTargetTag(targetName);

            string phase = ";PickerPhase=InspectionZHold";
            if (IsEnterSideFromBottomInspection())
                phase += ";InspectionContinuous;From=Bottom;To=Side";
            else if (Options != null && Options.KeepZUntilSideInspectionComplete)
                phase += ";InspectionContinuous;From=Side;To=Side";

            return AppendAutoProcessCorrectionTargetTag(targetName + phase);
        }

        private async Task<int> MoveSideXAndVision0PositionAsync(
            IDictionary<PickerAxis, double> pickerTargets,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> pickerTask = MovePickerXTThenYAndVerifyAsync(
                    pickerTargets,
                    "side inspection X/Y",
                    ct,
                    BuildSideMoveTargetName());

                Task<int> visionTask = CanSkipSideVisionProcessMove(0)
                    ? Task.FromResult(0)
                    : MoveSideVisionProcess0PositionAsync(ct);

                Task<int> previousZTask = MovePreviousInspectedPickerZToAvoidForCurrentInspectionAsync(ct);

                int[] results = await Task.WhenAll(pickerTask, visionTask, previousZTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0 || results[2] != 0)
                {
                    return Fail(
                        "PICKER-SIDE-X-VISION0-PARALLEL",
                        Name,
                        "Side 0도 진입 X/Y, SideVisionY 0도, 이전 PickerZ Avoid 병렬 이동 실패. " +
                        "pickerResult=" + results[0] +
                        ", visionResult=" + results[1] +
                        ", previousZResult=" + results[2] +
                        ", pickerNo=" + _currentPickerNo);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "PICKER-SIDE-X-VISION0-PARALLEL-EX",
                    Name,
                    "Side 0도 진입 X/Y와 SideVisionY 0도 병렬 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePreviousInspectedPickerZToAvoidForCurrentInspectionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options == null || !Options.KeepZUntilSideInspectionComplete)
                    return 0;

                int previousCursor = _pickerCursor - 1;
                if (previousCursor < 0 || previousCursor >= _pickedPickerIndexes.Count)
                    return 0;

                int previousPickerIndex = _pickedPickerIndexes[previousCursor];
                if (previousPickerIndex == _currentPickerIndex)
                    return 0;

                PickerAxis previousZAxis = GetPickerZAxis(previousPickerIndex);
                double previousZAvoid = GetPickerTeachingPosition(previousZAxis, "AvoidPosition");
                if (CanSkipPickerMoveCommand(previousZAxis, previousZAvoid))
                {
                    WriteLog("PickerSideInspectionSequence",
                        Name + " 다음 Side 검사 진입 중 이전 PickerZ가 이미 Avoid 위치입니다. " +
                        "previousPickerNo=" + ToPickerNo(previousPickerIndex) +
                        ", currentPickerNo=" + _currentPickerNo + " - Check");
                    return 0;
                }

                int result = await MovePickerAxisAndVerifyAsync(
                    previousZAxis,
                    previousZAvoid,
                    "다음 Side 검사 진입 중 이전 PickerZ Avoid",
                    ct,
                    BuildPickerTargetName("DieSideZAvoidDeferred", previousPickerIndex) + ";PickerPhase=InspectionZHold;InspectionContinuous;From=Side;To=Side").ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerSideInspectionSequence",
                    Name + " 다음 Side 검사 진입 중 이전 PickerZ Avoid 완료. " +
                    "previousPickerNo=" + ToPickerNo(previousPickerIndex) +
                    ", currentPickerNo=" + _currentPickerNo + " - Ok");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-SIDE-PREV-Z-AVOID-EX", Name,
                    "다음 Side 검사 시작 중 이전 PickerZ Avoid 이동 예외 발생. currentPickerNo=" +
                    _currentPickerNo + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSideYToInspectionAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "side inspection Y",
                ct,
                BuildSideMoveTargetName()).ConfigureAwait(false);
            if (result != 0)
                return result;

            _inspectionYPositionReady = true;

            if (CanSkipPickerMoveCommand(PickerAxis.PickerX, _targetPickerX))
            {
                CurrentStep = PickerSideInspectionStep.MoveSideZ;
                return 0;
            }

            _inspectionYPositionReady = false;
            CurrentStep = IsContinuousSideInspectionEntry()
                ? PickerSideInspectionStep.MoveSideXToInspection
                : PickerSideInspectionStep.MoveSideEntryYToAvoid;
            return 0;
        }

        private bool IsPickerYAtAvoidPosition()
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            return IsPickerAxisInPosition(PickerAxis.PickerY, target);
        }

        private bool IsPickerYAtXZoneMoveSafePosition()
        {
            return IsPickerYAtAvoidPosition() || IsPickerAxisInPosition(PickerAxis.PickerY, 0.0);
        }

        private bool IsCurrentPickerXInSideZone()
        {
            return PickerZoneInterlockRules.GetPickerCurrentXZone(Context != null ? Context.Machine : null, Side == PickerSequenceSide.Front) ==
                PickerWorkZone.Side;
        }

        private async Task<int> MoveSideZAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "side inspection Z",
                ct,
                BuildSideMoveTargetName()).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideT0;
            return 0;
        }

        private async Task<int> MoveSideT0Async(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(_currentPickerIndex),
                _targetPickerT0,
                "side inspection T 0deg",
                ct,
                BuildSideMoveTargetName()).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!CanSkipSideVisionProcessMove(0))
            {
                result = await MoveSideVisionProcess0PositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            CurrentStep = PickerSideInspectionStep.RequestSide0Inspection;
            return 0;
        }

        private async Task<int> RequestSide0InspectionAsync(CancellationToken ct)
        {
            using (TactTimeScope scope = BeginDetailedTactScope(
                TactTimeCategory.Vision,
                "Side 0deg Inspect",
                "0deg"))
            {
                try
                {
                    bool started = await StartSideInspectionRequestAsync(0, ct).ConfigureAwait(false);
                    if (!started)
                    {
                        scope.Fail("PICKER-SIDE-VISION0-FAIL", BuildSideTactDetail(0, "Side 0도 검사 실패."));
                        return Fail("PICKER-SIDE-VISION0-FAIL", "Vision",
                            "Side 0deg inspection EPD timeout. die=" +
                            _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
                    }

                    scope.Complete(BuildSideTactDetail(0, "Side 0도 검사 EPD 수신."));
                    RecordInspectionCheckpointForTact(
                        "Side0Inspection",
                        "Side 0deg Inspect Interval",
                        "0deg",
                        "started=True");
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel(BuildSideTactDetail(0, "Side 0도 검사가 취소되었습니다."));
                    throw;
                }
                catch (Exception ex)
                {
                    scope.Fail("PICKER-SIDE-VISION0-EX", BuildSideTactDetail(0, "Side 0도 검사 중 예외가 발생했습니다. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);
            CurrentStep = PickerSideInspectionStep.MoveSideT90;
            return 0;
        }

        private async Task<int> MoveSideT90Async(CancellationToken ct)
        {
            using (TactTimeScope scope = BeginDetailedTactScope(
                TactTimeCategory.Motion,
                "Side 0deg To 90deg Motion",
                "0deg->90deg"))
            {
                try
                {
                    int result = await MoveSideT90AndVision90PositionAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        scope.Fail("PICKER-SIDE-T90-MOVE", BuildSideTactDetail(90, "Side 0도에서 90도 전환 모션 실패. result=" + result));
                        return result;
                    }

                    scope.Complete(BuildSideTactDetail(90, "Side 0도에서 90도 전환 모션 완료."));
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel(BuildSideTactDetail(90, "Side 0도에서 90도 전환 모션이 취소되었습니다."));
                    throw;
                }
                catch (Exception ex)
                {
                    scope.Fail("PICKER-SIDE-T90-MOVE-EX", BuildSideTactDetail(90, "Side 0도에서 90도 전환 모션 중 예외가 발생했습니다. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

            CurrentStep = PickerSideInspectionStep.RequestSide90Inspection;
            return 0;
        }

        private async Task<int> MoveSideT90AndVision90PositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> pickerTask = MovePickerAxisAndVerifyAsync(
                    GetPickerTAxis(_currentPickerIndex),
                    _targetPickerT90,
                    "side inspection T 90deg",
                    ct,
                    BuildSideMoveTargetName());

                Task<int> visionTask = CanSkipSideVisionProcessMove(90)
                    ? Task.FromResult(0)
                    : MoveSideVisionProcess90PositionAsync(ct);

                int[] results = await Task.WhenAll(pickerTask, visionTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail(
                        "PICKER-SIDE-T90-VISION90-PARALLEL",
                        Name,
                        "Side 90도 PickerT와 SideVisionY 90도 병렬 이동 실패. " +
                        "pickerResult=" + results[0] +
                        ", visionResult=" + results[1] +
                        ", pickerNo=" + _currentPickerNo);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "PICKER-SIDE-T90-VISION90-PARALLEL-EX",
                    Name,
                    "Side 90도 PickerT와 SideVisionY 90도 병렬 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RequestSide90InspectionAsync(CancellationToken ct)
        {
            using (TactTimeScope scope = BeginDetailedTactScope(
                TactTimeCategory.Vision,
                "Side 90deg Inspect",
                "90deg"))
            {
                try
                {
                    bool started = await StartSideInspectionRequestAsync(90, ct).ConfigureAwait(false);
                    if (!started)
                    {
                        scope.Fail("PICKER-SIDE-VISION90-FAIL", BuildSideTactDetail(90, "Side 90도 검사 실패."));
                        return Fail("PICKER-SIDE-VISION90-FAIL", "Vision",
                            "Side 90deg inspection EPD timeout. die=" +
                            _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
                    }

                    scope.Complete(BuildSideTactDetail(90, "Side 90도 검사 EPD 수신."));
                    RecordInspectionCheckpointForTact(
                        "Side90Inspection",
                        "Side 90deg Inspect Interval",
                        "90deg",
                        "started=True");
                }
                catch (OperationCanceledException)
                {
                    scope.Cancel(BuildSideTactDetail(90, "Side 90도 검사가 취소되었습니다."));
                    throw;
                }
                catch (Exception ex)
                {
                    scope.Fail("PICKER-SIDE-VISION90-EX", BuildSideTactDetail(90, "Side 90도 검사 중 예외가 발생했습니다. error=" + ex.Message));
                    throw;
                }
                finally
                {
                }
            }

            await DelaySideInspectionTurnSettleAsync(ct).ConfigureAwait(false);
            return QueueCapturedSideInspection();
        }

        private int QueueCapturedSideInspection()
        {
            if (_currentDie == null)
            {
                return Fail("PICKER-SIDE-CAPTURE-CONTEXT", "Vision",
                    "Side 검사 촬영 완료 정보를 저장할 자재 문맥이 없습니다. pickerNo=" + _currentPickerNo);
            }

            bool side0Ready = _side0Started || _side0Result != null;
            bool side90Ready = _side90Started || _side90Result != null;
            if (!side0Ready || !side90Ready)
            {
                return Fail("PICKER-SIDE-CAPTURE-INCOMPLETE", "Vision",
                    "Side RESULT 수집 전 0도/90도 촬영 EPD가 모두 완료되지 않았습니다. die=" +
                    _currentDie.DieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", side0Ready=" + side0Ready +
                    ", side90Ready=" + side90Ready);
            }

            _capturedSideInspections.Add(new CapturedSideInspection
            {
                PickerIndex = _currentPickerIndex,
                PickerNo = _currentPickerNo,
                Die = _currentDie,
                Side0Result = _side0Result,
                Side90Result = _side90Result,
                Side0Started = _side0Started,
                Side90Started = _side90Started,
                TargetPickerX = _targetPickerX,
                TargetPickerY = _targetPickerY,
                TargetPickerZ = _targetPickerZ,
                TargetPickerT0 = _targetPickerT0,
                TargetPickerT90 = _targetPickerT90
            });

            WriteLog("PickerSideInspectionSequence",
                Name + " Side 0도/90도 촬영 EPD 완료 정보를 RESULT Collection에 등록했습니다. " +
                "die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", capturedCount=" + _capturedSideInspections.Count +
                ", loadedCount=" + _pickedPickerIndexes.Count + " - Ok");

            if (Options != null && Options.KeepZUntilSideInspectionComplete)
            {
                WriteLog("PickerSideInspectionSequence",
                    Name + " Auto 연속 검사를 위해 Side 촬영 완료 후 PickerZ를 유지합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId + " - Ok");
                QueuePendingT0Return(_currentPickerIndex, _targetPickerT0);
                CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.MoveSideZToAvoid;
            return 0;
        }

        private async Task<int> MoveSideVisionProcess0PositionAsync(CancellationToken ct)
        {
            return await MoveSideVisionProcessPositionAsync(0, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveSideVisionProcess90PositionAsync(CancellationToken ct)
        {
            return await MoveSideVisionProcessPositionAsync(90, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveSideVisionProcessPositionAsync(int angleDeg, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                {
                    return Fail("PICKER-SIDE-VISION-UNIT-MISSING", "Vision",
                        "Side 검사 카메라 이동 실패. VisionUnit을 찾을 수 없습니다. angle=" + angleDeg +
                        ", pickerNo=" + _currentPickerNo);
                }

                // Side 0/90도 모두 동일한 카메라 초점 기준 위치를 사용한다.
                int result = await vision.MoveBothSideVisionProcess0PositionAsync(
                    Options != null && Options.FineMove).ConfigureAwait(false);

                if (result != 0)
                {
                    return Fail("PICKER-SIDE-VISION-POSITION", "Vision",
                        "Side 검사 카메라 " + angleDeg + "도 티칭 위치 이동 실패. result=" + result +
                        ", pickerNo=" + _currentPickerNo);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-SIDE-VISION-POSITION-EX", "Vision",
                    "Side 검사 카메라 " + angleDeg + "도 티칭 위치 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool CanSkipSideVisionProcessMove(int angleDeg)
        {
            try
            {
                VisionUnit vision = Context != null && Context.Machine != null ? Context.Machine.VisionUnit : null;
                if (vision == null)
                    return false;

                BaseAxis front = vision.ResolveVisionAxis(VisionAxis.FrontSideVisionY);
                BaseAxis rear = vision.ResolveVisionAxis(VisionAxis.RearSideVisionY);
                double frontTarget = vision.GetVisionTeachingPosition(VisionAxis.FrontSideVisionY, "Process0Position");
                double rearTarget = vision.GetVisionTeachingPosition(VisionAxis.RearSideVisionY, "Process0Position");
                double frontTolerance = front != null && front.Config != null && front.Config.InPositionTolerance > 0.0
                    ? front.Config.InPositionTolerance
                    : 0.01;
                double rearTolerance = rear != null && rear.Config != null && rear.Config.InPositionTolerance > 0.0
                    ? rear.Config.InPositionTolerance
                    : 0.01;
                return AxisMoveWaiter.CanSkipMoveCommandAtTarget(front, frontTarget, frontTolerance) &&
                       AxisMoveWaiter.CanSkipMoveCommandAtTarget(rear, rearTarget, rearTolerance);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> ApplySideInspectionResultAsync(CancellationToken ct)
        {
            if (_capturedSideInspections.Count != _pickedPickerIndexes.Count)
            {
                return Fail("PICKER-SIDE-CAPTURE-COUNT", "Vision",
                    "Side RESULT Collection 전에 실제 적재 Picker의 촬영 완료 수가 일치하지 않습니다. " +
                    "capturedCount=" + _capturedSideInspections.Count +
                    ", loadedCount=" + _pickedPickerIndexes.Count);
            }

            var failures = new List<string>();

            for (int i = 0; i < _capturedSideInspections.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                CapturedSideInspection captured = _capturedSideInspections[i];
                if (captured == null || captured.ResultApplied)
                    continue;

                try
                {
                    if ((captured.Side0Started || captured.Side90Started) &&
                        (captured.Side0Result == null || captured.Side90Result == null))
                    {
                        SideVisionResult result = await WaitSideInspectionResultAsync(
                            captured.PickerNo,
                            ct).ConfigureAwait(false);
                        if (result == null)
                        {
                            string failure = "die=" + (captured.Die != null ? captured.Die.DieId : "-") +
                                             ", pickerNo=" + captured.PickerNo +
                                             ", reason=RESULT missing";
                            failures.Add(failure);
                            WriteLog("PickerSideInspectionSequence",
                                Name + " Side 최종 RESULT Collection 중 결과 수신 실패. " + failure + " - Failed");
                            continue;
                        }

                        if (captured.Side0Result == null)
                            captured.Side0Result = result;
                        if (captured.Side90Result == null)
                            captured.Side90Result = result;
                    }

                    if (captured.Side0Result == null || captured.Side90Result == null)
                    {
                        string failure = "die=" + (captured.Die != null ? captured.Die.DieId : "-") +
                                         ", pickerNo=" + captured.PickerNo +
                                         ", reason=incomplete RESULT";
                        failures.Add(failure);
                        WriteLog("PickerSideInspectionSequence",
                            Name + " Side 최종 RESULT Collection 결과가 불완전합니다. " + failure + " - Failed");
                        continue;
                    }

                    ApplySideInspectionResult(captured);
                    captured.ResultApplied = true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string failure = "die=" + (captured.Die != null ? captured.Die.DieId : "-") +
                                     ", pickerNo=" + captured.PickerNo +
                                     ", reason=" + ex.Message;
                    failures.Add(failure);
                    WriteLog("PickerSideInspectionSequence",
                        Name + " Side 최종 RESULT Collection 또는 적용 중 예외가 발생했습니다. " +
                        failure + " - Failed");
                }
            }

            if (failures.Count > 0)
            {
                return Fail("PICKER-SIDE-VISION-RESULT", "Vision",
                    "모든 Side 촬영 완료 후 RESULT Collection에 실패 항목이 있습니다. " +
                    "failureCount=" + failures.Count +
                    ", capturedCount=" + _capturedSideInspections.Count +
                    ", failures=" + string.Join(" / ", failures.ToArray()));
            }

            CurrentStep = Options != null && Options.KeepZUntilSideInspectionComplete
                ? PickerSideInspectionStep.MoveAllPickerZToAvoidAfterInspection
                : PickerSideInspectionStep.Complete;
            return 0;
        }

        private void ApplySideInspectionResult(CapturedSideInspection captured)
        {
            if (captured == null || captured.Die == null)
                throw new InvalidOperationException("Side 검사 결과를 적용할 자재 문맥이 없습니다.");

            bool ok0 = captured.Side0Result != null && captured.Side0Result.IsAllOk;
            bool ok90 = captured.Side90Result != null && captured.Side90Result.IsAllOk;
            bool ok = ok0 && ok90 && captured.Die.Result != DieResult.NG;

            MaterialStateService.UpsertInspection(captured.Die.DieId, new DieInspectionRecord
            {
                InspectionType = "Side0",
                Result = ok0 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                NgCodes = ok0 ? new List<string>() : new List<string> { "SIDE0_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Side0",
                        captured.PickerIndex,
                        captured.TargetPickerX,
                        captured.TargetPickerY,
                        captured.TargetPickerT0,
                        captured.TargetPickerZ,
                        new VisionOffset())
                },
                Measurements = BuildSideMeasurements(captured.Side0Result, "Side0")
            });

            MaterialStateService.UpsertInspection(captured.Die.DieId, new DieInspectionRecord
            {
                InspectionType = "Side90",
                Result = ok90 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                NgCodes = ok90 ? new List<string>() : new List<string> { "SIDE90_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Side90",
                        captured.PickerIndex,
                        captured.TargetPickerX,
                        captured.TargetPickerY,
                        captured.TargetPickerT90,
                        captured.TargetPickerZ,
                        new VisionOffset())
                },
                Measurements = BuildSideMeasurements(captured.Side90Result, "Side90")
            });

            MaterialStateService.ApplyDieInspectionResult(
                captured.Die.DieId,
                ok ? DieResult.Good : DieResult.NG,
                ok ? "" : "SIDE_NG",
                "SideInspection");

            WriteLog("PickerSideInspectionSequence",
                Name + " side inspection result. die=" + captured.Die.DieId +
                ", pickerNo=" + captured.PickerNo +
                ", ok0=" + ok0 +
                ", ok90=" + ok90 + " - Ok");
        }

        private List<InspectionMeasurement> BuildSideMeasurements(SideVisionResult result, string prefix)
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

        private async Task<int> MoveSideZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double zAvoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");

            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                zAvoid,
                "사이드 검사 종료 후 PickerZ 어보이드",
                ct,
                BuildPickerTargetName("DieSideExit", _currentPickerIndex)).ConfigureAwait(false);
            if (result != 0)
                return result;

            QueuePendingT0Return(_currentPickerIndex, _targetPickerT0);
            CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private async Task<int> MoveSideTToSafeAsync(CancellationToken ct)
        {
            int result = await CompletePendingT0ReturnAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAfterInspectionAsync(CancellationToken ct)
        {
            int result = await MoveRemainingInspectedPickerZToAvoidAfterInspectionAsync(
                "Side 검사 전체 완료 후 남은 PickerZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.Complete;
            return 0;
        }

        private async Task<int> MoveRemainingInspectedPickerZToAvoidAfterInspectionAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var targets = new Dictionary<PickerAxis, double>();
                for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                {
                    int pickerIndex = _pickedPickerIndexes[i];
                    PickerAxis zAxis = GetPickerZAxis(pickerIndex);
                    double zAvoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                    if (!CanSkipPickerMoveCommand(zAxis, zAvoid))
                        targets[zAxis] = zAvoid;
                }

                if (targets.Count == 0)
                    return 0;

                foreach (KeyValuePair<PickerAxis, double> pair in targets)
                {
                    int result = await MovePickerAxisAndVerifyAsync(
                        pair.Key,
                        pair.Value,
                        description,
                        ct,
                        "DieSideFinalZAvoid").ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-SIDE-FINAL-Z-AVOID-EX", Name,
                    description + " 중 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSideYToAvoidAsync(CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "side inspection Y avoid 전 PickerZ 전체 Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                target,
                "side inspection Y avoid",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideXToAvoid;
            return 0;
        }

        private async Task<int> MoveSideXToAvoidAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                target,
                "side inspection X avoid",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private void QueuePendingT0Return(int pickerIndex, double target)
        {
            _pendingT0Returns.Add(new PendingT0Return
            {
                PickerIndex = pickerIndex,
                Target = target
            });

            WriteLog("PickerSideInspectionSequence",
                Name + " Side 검사 완료 후 PickerT 0도 복귀를 생략합니다. " +
                "Side 90도 위치를 유지합니다. pickerNo=" + ToPickerNo(pickerIndex) +
                ", target0=" + target.ToString("F3") + " - Ok");
        }

        private bool HasPendingT0Return()
        {
            return _pendingT0Returns.Count > 0;
        }

        private void ClearPendingT0Return()
        {
            _pendingT0Returns.Clear();
        }

        private async Task<int> StartPendingT0ReturnCommandAsync(string description, CancellationToken ct)
        {
            if (!HasPendingT0Return())
                return 0;

            ct.ThrowIfCancellationRequested();

            for (int i = _pendingT0Returns.Count - 1; i >= 0; i--)
            {
                PendingT0Return pending = _pendingT0Returns[i];
                PickerAxis tAxis = GetPickerTAxis(pending.PickerIndex);
                if (CanSkipPickerMoveCommand(tAxis, pending.Target))
                {
                    _pendingT0Returns.RemoveAt(i);
                    continue;
                }

                if (pending.MoveTask != null)
                {
                    if (!pending.MoveTask.IsCompleted)
                        continue;

                    int completedResult = await pending.MoveTask.ConfigureAwait(false);
                    if (completedResult != 0)
                    {
                        return Fail("PICKER-SIDE-T0-DEFER-CMD", Name,
                            description + " 완료된 복귀 명령 결과 실패. result=" + completedResult +
                            ", pickerNo=" + ToPickerNo(pending.PickerIndex) +
                            ", " + BuildPickerAxisState(tAxis, pending.Target));
                    }

                    if (!IsPickerAxisInPosition(tAxis, pending.Target))
                    {
                        return Fail("PICKER-SIDE-T0-DEFER-FINAL-POS", Name,
                            description + " 완료된 PickerT 0도 복귀 최종 위치 확인 실패. " +
                            BuildPickerAxisState(tAxis, pending.Target));
                    }

                    _pendingT0Returns.RemoveAt(i);
                    continue;
                }

                pending.MoveTask = MovePickerAxisCommandAsync(
                    tAxis,
                    pending.Target,
                    BuildPickerTargetName("DieSideT0ReturnDeferred", pending.PickerIndex));

                WriteLog("PickerSideInspectionSequence",
                    Name + " 이전 PickerT 0도 복귀 명령을 백그라운드로 시작했습니다. description=" + description +
                    ", pickerNo=" + ToPickerNo(pending.PickerIndex) +
                    ", " + BuildPickerAxisState(tAxis, pending.Target) + " - Ok");
            }

            return 0;
        }

        private async Task<int> CompletePendingT0ReturnAsync(CancellationToken ct)
        {
            if (!HasPendingT0Return())
                return 0;

            // 다음 검사 동작이 없는 마지막 Picker는 여기서 Z 상승 후 T 0도 복귀를 직접 완료한다.
            for (int i = _pendingT0Returns.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                PendingT0Return pending = _pendingT0Returns[i];
                PickerAxis tAxis = GetPickerTAxis(pending.PickerIndex);
                if (CanSkipPickerMoveCommand(tAxis, pending.Target))
                {
                    _pendingT0Returns.RemoveAt(i);
                    continue;
                }

                if (pending.MoveTask != null)
                    continue;

                int moveResult = await MovePickerAxisAndVerifyAsync(
                    tAxis,
                    pending.Target,
                    "마지막 Side PickerT 0도 복귀",
                    ct,
                    BuildPickerTargetName("DieSideT0ReturnFinal", pending.PickerIndex)).ConfigureAwait(false);
                if (moveResult != 0)
                    return moveResult;

                if (!IsPickerAxisInPosition(tAxis, pending.Target))
                {
                    return Fail("PICKER-SIDE-T0-DEFER-FINAL-POS", Name,
                        "예약된 PickerT 0도 복귀 최종 위치 확인 실패. " +
                        BuildPickerAxisState(tAxis, pending.Target));
                }

                _pendingT0Returns.RemoveAt(i);
            }

            // 이전 Picker들은 다음 Picker 검사 중 백그라운드로 복귀 중이어야 하므로, 완료 시점에 결과만 확인한다.
            for (int i = _pendingT0Returns.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                PendingT0Return pending = _pendingT0Returns[i];
                PickerAxis tAxis = GetPickerTAxis(pending.PickerIndex);
                if (IsPickerAxisInPosition(tAxis, pending.Target))
                {
                    _pendingT0Returns.RemoveAt(i);
                    continue;
                }

                int moveResult = await pending.MoveTask.ConfigureAwait(false);
                if (moveResult != 0)
                {
                    return Fail("PICKER-SIDE-T0-DEFER-CMD", Name,
                        "예약된 PickerT 0도 복귀 명령 결과 실패. result=" + moveResult +
                        ", pickerNo=" + ToPickerNo(pending.PickerIndex) +
                        ", " + BuildPickerAxisState(tAxis, pending.Target));
                }

                var waitResult = await WaitPickerAxisMoveDoneAsync(
                    tAxis,
                    pending.Target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-SIDE-T0-DEFER", waitResult), Name,
                        "예약된 PickerT 0도 복귀 완료 대기 실패. " +
                        FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(tAxis, pending.Target)));
                }

                if (!IsPickerAxisInPosition(tAxis, pending.Target))
                {
                    return Fail("PICKER-SIDE-T0-DEFER-FINAL-POS", Name,
                        "예약된 PickerT 0도 복귀 최종 위치 확인 실패. " +
                        BuildPickerAxisState(tAxis, pending.Target));
                }

                _pendingT0Returns.RemoveAt(i);
            }

            return 0;
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                if (HasPendingT0Return())
                    ClearPendingT0Return();

                CurrentStep = PickerSideInspectionStep.ApplySideInspectionResult;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.SelectNextPicker;
            return 0;
        }

        private async Task<bool> StartSideInspectionRequestAsync(int angleDeg, CancellationToken ct)
        {
            try
            {
                int retryCount = Options != null && Options.VisionRetryCount > 0 ? Options.VisionRetryCount : 3;
                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    bool started = await StartSideInspectionRequestCoreAsync(angleDeg, ct).ConfigureAwait(false);
                    if (started)
                        return true;

                    WriteLog("PickerSideInspectionSequence",
                        Name + " side inspection retry. die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", angle=" + angleDeg +
                        ", attempt=" + attempt + " - Check");
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("PICKER-SIDE-VISION-EX", "Vision", "Side inspection exception. angle=" + angleDeg + ", error=" + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private async Task<bool> StartSideInspectionRequestCoreAsync(int angleDeg, CancellationToken ct)
        {
            if (IsVisionBypassed())
            {
                if (angleDeg == 90)
                    _side90Result = SimulateSideResult();
                else
                    _side0Result = SimulateSideResult();
                return true;
            }

            await DelayBeforeVisionInspectionAsync(ct).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();

            int timeoutMs = ResolveVisionInspectionTimeout();
            WriteLog("PickerSideInspectionSequence",
                Name + " request side vision. die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", angleDeg=" + angleDeg +
                ", pickerX=" + _targetPickerX +
                ", pickerY=" + _targetPickerY +
                ", pickerZ=" + _targetPickerZ +
                ", pickerT=" + (angleDeg == 90 ? _targetPickerT90 : _targetPickerT0) +
                ", timeoutMs=" + timeoutMs + " - Start");

            if (IsDryRunMode())
            {
                await TriggerDryRunSideGrabIfConnectedAsync(angleDeg, timeoutMs, ct).ConfigureAwait(false);
                if (angleDeg == 90)
                    _side90Result = BuildSideResultWaitSkipped(angleDeg);
                else
                    _side0Result = BuildSideResultWaitSkipped(angleDeg);
                return true;
            }

            bool started;
            if (Side == PickerSequenceSide.Front)
            {
                started = await FrontPicker.StartSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);
            }
            else
            {
                started = await RearPicker.StartSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);
            }

            if (!started)
                return false;

            if (angleDeg == 90)
                _side90Started = true;
            else
                _side0Started = true;

            return true;
        }

        private async Task<SideVisionResult> WaitSideInspectionResultAsync(int pickerNo, CancellationToken ct)
        {
            if (IsVisionBypassed() || IsDryRunMode())
                return SimulateSideResult();

            ct.ThrowIfCancellationRequested();

            int timeoutMs = ResolveVisionInspectionTimeout();
            return Side == PickerSequenceSide.Front
                ? await FrontPicker.WaitSideInspectionResultAsync(pickerNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitSideInspectionResultAsync(pickerNo, timeoutMs, ct).ConfigureAwait(false);
        }

        private async Task TriggerDryRunSideGrabIfConnectedAsync(int angleDeg, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsSideVisionConnected())
            {
                WriteLog("PickerSideInspectionSequence",
                    Name + " DryRun Side GRAB skipped. Vision is not connected. side=" + Side +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : string.Empty) +
                    ", pickerNo=" + _currentPickerNo +
                    ", angleDeg=" + angleDeg + " - Check");
                return;
            }

            bool grabbed = Side == PickerSequenceSide.Front
                ? await FrontPicker.TriggerSideInspectionExposeAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.TriggerSideInspectionExposeAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);

            WriteLog("PickerSideInspectionSequence",
                Name + " DryRun Side GRAB " + (grabbed ? "completed" : "failed") +
                ". side=" + Side +
                ", die=" + (_currentDie != null ? _currentDie.DieId : string.Empty) +
                ", pickerNo=" + _currentPickerNo +
                ", angleDeg=" + angleDeg +
                ", timeoutMs=" + timeoutMs + " - Check");
        }

        private SideVisionResult BuildSideResultWaitSkipped(int angleDeg)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values["ResultWaitSkipped"] = "True";
            values["AngleDeg"] = angleDeg.ToString();
            values["DieId"] = _currentDie != null ? _currentDie.DieId : string.Empty;

            WriteLog("PickerSideInspectionSequence",
                Name + " Side 검사 EPD 수신 이후 결과 수신 대기를 생략하고 진행합니다. " +
                "side=" + Side +
                ", die=" + (_currentDie != null ? _currentDie.DieId : string.Empty) +
                ", pickerNo=" + _currentPickerNo +
                ", angleDeg=" + angleDeg + " - Check");

            return new SideVisionResult
            {
                PickerNo = _currentPickerNo,
                Side1Ok = true,
                Side2Ok = true,
                Side3Ok = true,
                Side4Ok = true,
                Raw = "SIDE_RESULT_WAIT_SKIPPED",
                Values = values
            };
        }

        private SideVisionResult SimulateSideResult()
        {
            QMC.CDT320.VisionComm.AutoVisionChannel channel = Side == PickerSequenceSide.Front
                ? QMC.CDT320.VisionComm.AutoVisionChannel.FrontSide
                : QMC.CDT320.VisionComm.AutoVisionChannel.RearSide;
            string inspector = Side == PickerSequenceSide.Front
                ? QMC.CDT320.VisionComm.VisionToolIds.FrontSide.SurfaceInspector
                : QMC.CDT320.VisionComm.VisionToolIds.RearSide.SurfaceInspector;
            QMC.CDT320.VisionComm.InspectionResultDto inspection =
                QMC.CDT320.VisionComm.AutoVisionRequestService.BuildSimulationInspectionResult(
                    channel,
                    inspector,
                    _currentPickerNo);
            bool pass = inspection != null && inspection.IsPass;
            return new SideVisionResult
            {
                PickerNo = _currentPickerNo,
                Side1Ok = pass,
                Side2Ok = pass,
                Side3Ok = true,
                Side4Ok = true,
                Raw = inspection != null ? inspection.Raw : "",
                Values = inspection != null && inspection.Values != null
                    ? new Dictionary<string, string>(inspection.Values, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private bool IsSimulationOrDryRun()
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            return IsPickerSimulationOrDryRun();
        }
        private bool IsVisionBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }

        private bool IsDryRunMode()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null &&
                   settings.DryRunMode &&
                   !QMC.CDT320.VisionComm.AutoVisionRequestService.IsRealVisionInSimulationActive();
        }

        private bool IsSideVisionConnected()
        {
            return QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                       QMC.CDT320.VisionComm.AutoVisionChannel.FrontSide) &&
                   QMC.CDT320.VisionComm.VisionCommandService.IsConnected(
                       QMC.CDT320.VisionComm.AutoVisionChannel.RearSide);
        }

        private TactTimeScope BeginDetailedTactScope(TactTimeCategory category, string processName, string stepName)
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
                BuildSideTactDetail(stepName == "90deg" || stepName == "0deg->90deg" ? 90 : 0, "Start"));
        }

        private void RecordInspectionCheckpointForTact(string key, string processName, string stepName, string detail)
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
                    _currentDie != null ? _currentDie.DieId : "",
                    _currentPickerNo,
                    detail);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string BuildSideTactDetail(int angleDeg, string detail)
        {
            return "side=" + Side +
                   ", angleDeg=" + angleDeg +
                   ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                   ", pickerNo=" + _currentPickerNo +
                   ", " + (detail ?? "");
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
                WriteLog("PickerSideInspectionSequence", "InspectionArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}

