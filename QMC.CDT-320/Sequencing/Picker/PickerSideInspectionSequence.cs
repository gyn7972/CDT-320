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

                // 사이드 Z로 어보이드 이동
                case PickerSideInspectionStep.MoveSideZToAvoid:
                    return MoveSideZToAvoidAsync(ct);

                // 사이드 T로 안전 이동
                case PickerSideInspectionStep.MoveSideTToSafe:
                    return MoveSideTToSafeAsync(ct);

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

            bool wasInInspectionZone = _inspectionYPositionReady;
            bool xPositionReady = IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX);
            _inspectionYPositionReady = IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY);

            if (xPositionReady)
            {
                CurrentStep = _inspectionYPositionReady
                    ? PickerSideInspectionStep.MoveSideZ
                    : PickerSideInspectionStep.MoveSideYToInspection;
                return 0;
            }

            if (_inspectionYPositionReady || wasInInspectionZone)
            {
                CurrentStep = PickerSideInspectionStep.MoveSideXToInspection;
                return 0;
            }

            if (!IsPickerYAtAvoidPosition())
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
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                "side inspection X",
                ct,
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            if (_inspectionYPositionReady && IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY))
            {
                CurrentStep = PickerSideInspectionStep.MoveSideZ;
                return 0;
            }

            // 피커 간 이동에서는 Y Avoid 복귀 없이 현재 피커의 AlignOffsetY가 반영된 Y 위치만 보정한다.
            CurrentStep = PickerSideInspectionStep.MoveSideYToInspection;
            return 0;
        }

        private async Task<int> MoveSideYToInspectionAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "side inspection Y",
                ct,
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
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

        private async Task<int> MoveSideZAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "side inspection Z",
                ct,
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
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
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveSideVisionProcess0PositionAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

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
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(_currentPickerIndex),
                _targetPickerT90,
                "side inspection T 90deg",
                ct,
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveSideVisionProcess90PositionAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.RequestSide90Inspection;
            return 0;
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
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "side inspection Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private async Task<int> MoveSideTToSafeAsync(CancellationToken ct)
        {
            PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
            double target = GetPickerTeachingPosition(tAxis, "PickPosition") + ResolvePickerAlignOffsetT(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(tAxis, target, "side inspection T safe", ct, "DiePickPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideYToAvoid;
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

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
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

