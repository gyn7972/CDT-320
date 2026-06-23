using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerSideInspectionSequence : PickerSequenceBase<PickerSideInspectionStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const int SideInspectionTurnSettleDelayMs = 100;
        private const int VisionInspectionSettleDelayMs = 100;

        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private int _pickerCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private DieMaterial _currentDie;
        private SideVisionResult _side0Result;
        private SideVisionResult _side90Result;
        private double _targetPickerX;
        private double _targetPickerY;
        private double _targetPickerZ;
        private double _targetPickerT0;
        private double _targetPickerT90;
        private bool _inspectionYPositionReady;
        private SequenceResourceLease _inspectionAreaLease;
        private readonly List<PendingT0Return> _pendingT0Returns = new List<PendingT0Return>();

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
                    int stepResult = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerSideInspectionStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerSideInspectionStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
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
                return Fail("PICKER-SIDE-EX", Name, "Picker side inspection failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                    ReleaseInspectionArea();
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
                    return Task.FromResult(ApplySideInspectionResult());

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

            if (_currentDie == null)
            {
                CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
                return 0;
            }

            _targetPickerX = ResolvePickerZoneX("DieSidePosition", _currentPickerIndex);
            _targetPickerY = ResolvePickerZoneY("DieSidePosition", _currentPickerIndex);
            _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "SidePosition");
            _targetPickerT0 = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "SidePosition") +
                ResolvePickerAlignOffsetT(_currentPickerIndex);
            _targetPickerT90 = _targetPickerT0 + 90.0;

            bool xPositionReady = IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX);
            _inspectionYPositionReady = IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY);

            if (xPositionReady)
            {
                CurrentStep = _inspectionYPositionReady
                    ? PickerSideInspectionStep.MoveSideZ
                    : PickerSideInspectionStep.MoveSideYToInspection;
                return 0;
            }

            if (!IsCurrentPickerXInSideZone() &&
                !IsPickerYAtXZoneMoveSafePosition() &&
                !IsEnterSideFromBottomInspection())
            {
                CurrentStep = PickerSideInspectionStep.MoveSideEntryYToAvoid;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.MoveSideXToInspection;
            return 0;
        }

        private async Task<int> MoveSideEntryYToAvoidAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
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
            bool enterFromBottom = IsEnterSideFromBottomInspection();
            if (!currentXInSideZone &&
                !IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX) &&
                !IsPickerYAtXZoneMoveSafePosition() &&
                !enterFromBottom)
            {
                return await MoveSideEntryYToAvoidAsync(ct).ConfigureAwait(false);
            }

            bool canKeepInspectionY = enterFromBottom || currentXInSideZone || IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX);
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            if (canKeepInspectionY && (!_inspectionYPositionReady || !IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY)))
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

        private string BuildSideMoveTargetName()
        {
            string targetName = "DieSidePosition[" + _currentPickerIndex + "]";
            if (!IsEnterSideFromBottomInspection() &&
                (Options == null || !Options.KeepZUntilSideInspectionComplete))
                return targetName;

            string phase = ";PickerPhase=InspectionZHold";
            if (IsEnterSideFromBottomInspection())
                phase += ";InspectionContinuous;From=Bottom;To=Side";

            return targetName + phase;
        }

        private async Task<int> MoveSideXAndVision0PositionAsync(
            IDictionary<PickerAxis, double> pickerTargets,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                Task<int> pickerTask = MovePickerAxesAndVerifyAsync(
                    pickerTargets,
                    "side inspection X/Y",
                    ct,
                    BuildSideMoveTargetName());

                Task<int> visionTask = IsSideVisionProcessPositionReady(0)
                    ? Task.FromResult(0)
                    : MoveSideVisionProcess0PositionAsync(ct);

                int[] results = await Task.WhenAll(pickerTask, visionTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                {
                    return Fail(
                        "PICKER-SIDE-X-VISION0-PARALLEL",
                        Name,
                        "Side 0도 진입 X/Y와 SideVisionY 0도 병렬 이동 실패. " +
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
                    "PICKER-SIDE-X-VISION0-PARALLEL-EX",
                    Name,
                    "Side 0도 진입 X/Y와 SideVisionY 0도 병렬 이동 중 예외가 발생했습니다. error=" + ex.Message);
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

            if (IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX))
            {
                CurrentStep = PickerSideInspectionStep.MoveSideZ;
                return 0;
            }

            _inspectionYPositionReady = false;
            CurrentStep = PickerSideInspectionStep.MoveSideEntryYToAvoid;
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

            if (!IsSideVisionProcessPositionReady(0))
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
            _side0Result = await RequestSideResultAsync(0, ct).ConfigureAwait(false);
            if (_side0Result == null)
            {
                return Fail("PICKER-SIDE-VISION0-FAIL", "Vision",
                    "Side 0deg inspection communication/result failed after retry. die=" +
                    _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
            }

            await Task.Delay(SideInspectionTurnSettleDelayMs, ct).ConfigureAwait(false);
            CurrentStep = PickerSideInspectionStep.MoveSideT90;
            return 0;
        }

        private async Task<int> MoveSideT90Async(CancellationToken ct)
        {
            int result = await MoveSideT90AndVision90PositionAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

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

                Task<int> visionTask = IsSideVisionProcessPositionReady(90)
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
            _side90Result = await RequestSideResultAsync(90, ct).ConfigureAwait(false);
            if (_side90Result == null)
            {
                return Fail("PICKER-SIDE-VISION90-FAIL", "Vision",
                    "Side 90deg inspection communication/result failed after retry. die=" +
                    _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
            }

            await Task.Delay(SideInspectionTurnSettleDelayMs, ct).ConfigureAwait(false);
            CurrentStep = PickerSideInspectionStep.ApplySideInspectionResult;
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

                int result = angleDeg == 90
                    ? await vision.MoveBothSideVisionProcess90PositionAsync(Options != null && Options.FineMove).ConfigureAwait(false)
                    : await vision.MoveBothSideVisionProcess0PositionAsync(Options != null && Options.FineMove).ConfigureAwait(false);

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

        private int ApplySideInspectionResult()
        {
            bool ok0 = _side0Result != null && _side0Result.IsAllOk;
            bool ok90 = _side90Result != null && _side90Result.IsAllOk;
            bool ok = ok0 && ok90 && _currentDie.Result != DieResult.NG;

            MaterialStateService.UpsertInspection(_currentDie.DieId, new DieInspectionRecord
            {
                InspectionType = "Side0",
                Result = ok0 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                NgCodes = ok0 ? new List<string>() : new List<string> { "SIDE0_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Side0",
                        _currentPickerIndex,
                        _targetPickerX,
                        _targetPickerY,
                        _targetPickerT0,
                        _targetPickerZ,
                        new VisionOffset())
                },
                Measurements = BuildSideMeasurements(_side0Result, "Side0")
            });

            MaterialStateService.UpsertInspection(_currentDie.DieId, new DieInspectionRecord
            {
                InspectionType = "Side90",
                Result = ok90 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng,
                NgCodes = ok90 ? new List<string>() : new List<string> { "SIDE90_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Side90",
                        _currentPickerIndex,
                        _targetPickerX,
                        _targetPickerY,
                        _targetPickerT90,
                        _targetPickerZ,
                        new VisionOffset())
                },
                Measurements = BuildSideMeasurements(_side90Result, "Side90")
            });

            MaterialStateService.ApplyDieInspectionResult(
                _currentDie.DieId,
                ok ? DieResult.Good : DieResult.NG,
                ok ? "" : "SIDE_NG",
                "SideInspection");

            WriteLog("PickerSideInspectionSequence",
                Name + " side inspection result. die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", ok0=" + ok0 +
                ", ok90=" + ok90 + " - Ok");

            if (Options != null && Options.KeepZUntilSideInspectionComplete)
            {
                WriteLog("PickerSideInspectionSequence",
                    Name + " Auto 연속 검사를 위해 Side 완료 후 PickerZ를 유지합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId + " - Ok");
                QueuePendingT0Return(_currentPickerIndex, _targetPickerT0);
                CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.MoveSideZToAvoid;
            return 0;
        }

        private List<InspectionMeasurement> BuildSideMeasurements(SideVisionResult result, string prefix)
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

        private async Task<int> MoveSideZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double zAvoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");

            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                zAvoid,
                "사이드 검사 종료 후 PickerZ 어보이드",
                ct,
                "DieSideExit[" + _currentPickerIndex + "]").ConfigureAwait(false);
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
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "Side 검사 전체 완료 후 PickerZ 전체 Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.Complete;
            return 0;
        }

        private async Task<int> MoveSideYToAvoidAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
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
                if (IsPickerAxisInPosition(tAxis, pending.Target))
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
                    "DieSideT0ReturnDeferred[" + pending.PickerIndex + "]");

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
                if (IsPickerAxisInPosition(tAxis, pending.Target))
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
                    "DieSideT0ReturnFinal[" + pending.PickerIndex + "]").ConfigureAwait(false);
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

                if (Options != null && Options.KeepZUntilSideInspectionComplete)
                {
                    CurrentStep = PickerSideInspectionStep.MoveAllPickerZToAvoidAfterInspection;
                    return 0;
                }

                CurrentStep = PickerSideInspectionStep.Complete;
                return 0;
            }

            CurrentStep = PickerSideInspectionStep.SelectNextPicker;
            return 0;
        }

        private async Task<SideVisionResult> RequestSideResultAsync(int angleDeg, CancellationToken ct)
        {
            try
            {
                int retryCount = Options != null && Options.VisionRetryCount > 0 ? Options.VisionRetryCount : 3;
                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    SideVisionResult result = await RequestSideResultCoreAsync(angleDeg, ct).ConfigureAwait(false);
                    if (result != null)
                        return result;

                    WriteLog("PickerSideInspectionSequence",
                        Name + " side inspection retry. die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", angle=" + angleDeg +
                        ", attempt=" + attempt + " - Check");
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("PICKER-SIDE-VISION-EX", "Vision", "Side inspection exception. angle=" + angleDeg + ", error=" + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private async Task<SideVisionResult> RequestSideResultCoreAsync(int angleDeg, CancellationToken ct)
        {
            await Task.Delay(VisionInspectionSettleDelayMs, ct).ConfigureAwait(false);

            if (IsSimulationOrDryRun())
                return SimulateSideResult();

            ct.ThrowIfCancellationRequested();

            int timeoutMs = ResolveTimeout();
            WriteLog("PickerSideInspectionSequence",
                Name + " request side vision. die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", angleDeg=" + angleDeg +
                ", pickerX=" + _targetPickerX +
                ", pickerY=" + _targetPickerY +
                ", pickerZ=" + _targetPickerZ +
                ", pickerT=" + (angleDeg == 90 ? _targetPickerT90 : _targetPickerT0) +
                ", timeoutMs=" + timeoutMs + " - Start");

            SideVisionResult result;
            if (Side == PickerSequenceSide.Front)
            {
                result = await FrontPicker.RequestSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);
            }
            else
            {
                result = await RearPicker.RequestSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs, ct).ConfigureAwait(false);
            }

            if (result == null)
                return null;

            if (!result.IsAllOk)
            {
                WriteLog("PickerSideInspectionSequence",
                    Name + " side vision returned NG. die=" + _currentDie.DieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", angleDeg=" + angleDeg +
                    ", side1=" + result.Side1Ok +
                    ", side2=" + result.Side2Ok +
                    ", side3=" + result.Side3Ok +
                    ", side4=" + result.Side4Ok + " - Check");
            }

            return result;
        }

        private SideVisionResult SimulateSideResult()
        {
            lock (SimVisionRandomLock)
            {
                return new SideVisionResult
                {
                    PickerNo = _currentPickerNo,
                    Side1Ok = true,
                    Side2Ok = true,
                    Side3Ok = true,
                    Side4Ok = true
                };
            }
        }

        private bool IsSimulationOrDryRun()
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            return IsPickerSimulationOrDryRun();
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

