using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomInspectionSequence : PickerSequenceBase<PickerBottomInspectionStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();
        private const int VisionInspectionSettleDelayMs = 100;

        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private int _pickerCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private DieMaterial _currentDie;
        private BottomVisionOffset _bottomResult;
        private double _targetPickerX;
        private double _targetPickerY;
        private double _targetPickerZ;
        private double _targetPickerT;
        private bool _inspectionYPositionReady;
        private SequenceResourceLease _inspectionAreaLease;

        public PickerBottomInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Inspect, side == PickerSequenceSide.Front ? "FrontPickerBottomInspectionSequence" : "RearPickerBottomInspectionSequence")
        {
            CurrentStep = PickerBottomInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerBottomInspectionStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ReleaseInspectionArea();
                CurrentStep = PickerBottomInspectionStep.Complete;
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
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerBottomInspectionStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerBottomInspectionStep.Complete)
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
                return Fail("PICKER-BOTTOM-EX", Name, "Picker bottom inspection failed. step=" + CurrentStep + ", error=" + ex.Message);
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
                case PickerBottomInspectionStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // 픽업 피커 목록 생성
                case PickerBottomInspectionStep.BuildPickedPickerList:
                    return Task.FromResult(BuildPickedPickerList());

                // 전체 피커 Z로 어보이드 이동
                case PickerBottomInspectionStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                // Bottom 검사 진입 전 상대 피커 간섭 상태 확인
                case PickerBottomInspectionStep.MoveOppositePickerToAvoidBeforeInspection:
                    return MoveOppositePickerToAvoidBeforeInspectionAsync(ct);

                // 다음 피커 선택
                case PickerBottomInspectionStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                // Bottom 검사 영역 진입 전 Y축 어보이드 확인
                case PickerBottomInspectionStep.MoveBottomYToAvoidBeforeInspection:
                    return MoveBottomYToAvoidBeforeInspectionAsync(ct);

                // 하단 XY 이동
                case PickerBottomInspectionStep.MoveBottomXToInspection:
                    return MoveBottomXToInspectionAsync(ct);

                case PickerBottomInspectionStep.MoveBottomYToInspection:
                    return MoveBottomYToInspectionAsync(ct);

                // 하단 Z 이동
                case PickerBottomInspectionStep.MoveBottomZ:
                    return MoveBottomZAsync(ct);

                // 하단 T 이동
                case PickerBottomInspectionStep.MoveBottomT:
                    return MoveBottomTAsync(ct);

                // 하단 검사 요청
                case PickerBottomInspectionStep.RequestBottomInspection:
                    return RequestBottomInspectionAsync(ct);

                // 하단 검사 결과 적용
                case PickerBottomInspectionStep.ApplyBottomInspectionResult:
                    return Task.FromResult(ApplyBottomInspectionResult());

                // 하단 Z로 어보이드 이동
                case PickerBottomInspectionStep.MoveBottomZToAvoid:
                    return MoveBottomZToAvoidAsync(ct);

                // 하단 T로 안전 이동
                case PickerBottomInspectionStep.MoveBottomTToSafe:
                    return MoveBottomTToSafeAsync(ct);

                // 하단 XY로 어보이드 이동
                case PickerBottomInspectionStep.MoveBottomYToAvoid:
                    return MoveBottomYToAvoidAsync(ct);

                case PickerBottomInspectionStep.MoveBottomXToAvoid:
                    return MoveBottomXToAvoidAsync(ct);

                // 다음 피커 또는 완료 선택
                case PickerBottomInspectionStep.SelectNextPickerOrComplete:
                    return Task.FromResult(SelectNextPickerOrComplete());

                default:
                    return Task.FromResult(Fail("PICKER-BOTTOM-STEP", Name, "Unsupported picker bottom inspection step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerBottomInspectionSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerBottomInspectionStep.Complete;
                return 0;
            }

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-BOTTOM-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            CurrentStep = PickerBottomInspectionStep.BuildPickedPickerList;
            return 0;
        }

        private int BuildPickedPickerList()
        {
            _pickedPickerIndexes.Clear();
            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerBottomInspectionSequence"));

            _pickerCursor = 0;
            _inspectionYPositionReady = false;

            if (_pickedPickerIndexes.Count == 0)
            {
                WriteLog("PickerBottomInspectionSequence", Name + " skipped because no die exists on picker. - Check");
                CurrentStep = PickerBottomInspectionStep.Complete;
                return 0;
            }

            CurrentStep = PickerBottomInspectionStep.MoveAllPickerZToAvoid;
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

                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "BottomInspection");

                result = await MoveAllPickerZToAvoidAndVerifyAsync("bottom inspection pre all picker Z avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = PickerBottomInspectionStep.MoveOppositePickerToAvoidBeforeInspection;
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
                    "PICKER-BOTTOM-RESOURCE-EX",
                    Name,
                    "바텀 검사 리소스 점유 또는 진입 준비 중 예외가 발생했습니다. error=" + ex.Message);
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
                        Name + ":Bottom",
                        ct).ConfigureAwait(false);
                    if (_inspectionAreaLease == null)
                        return -1;
                }

                // Bottom 검사는 InspectionArea만 점유한다.
                // Output Place와는 다른 작업 존이므로 OutputPlaceArea를 함께 잡으면
                // Rear Place 중 Front Bottom 진입 같은 정상 병렬 동작이 막힌다.
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
                    "PICKER-BOTTOM-RESOURCE",
                    Name,
                    "바텀 검사 리소스 점유 실패. InspectionArea를 확인하세요. error=" + ex.Message);
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
                    "바텀 검사 진입 전 상대 Picker 상태 확인",
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
                    "PICKER-BOTTOM-OPPOSITE-AVOID-EX",
                    Name,
                    "바텀 검사 진입 전 상대 Picker 상태 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }

            CurrentStep = PickerBottomInspectionStep.SelectNextPicker;
            return 0;
        }

        private int SelectNextPicker()
        {
            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                CurrentStep = PickerBottomInspectionStep.Complete;
                ReleaseInspectionArea();
                return 0;
            }

            _currentPickerIndex = _pickedPickerIndexes[_pickerCursor];
            _currentPickerNo = ToPickerNo(_currentPickerIndex);
            _currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            _bottomResult = null;

            if (_currentDie == null)
            {
                CurrentStep = PickerBottomInspectionStep.SelectNextPickerOrComplete;
                return 0;
            }

            _targetPickerX = ResolvePickerZoneX("DieBottomPosition", _currentPickerIndex);
            _targetPickerY = ResolvePickerZoneY("DieBottomPosition", _currentPickerIndex);
            _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "BottomPosition");
            _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "BottomPosition") +
                ResolvePickerAlignOffsetT(_currentPickerIndex);

            if (!_inspectionYPositionReady &&
                !IsPickerAxisInPosition(PickerAxis.PickerY, GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition")))
            {
                CurrentStep = PickerBottomInspectionStep.MoveBottomYToAvoidBeforeInspection;
                return 0;
            }

            CurrentStep = PickerBottomInspectionStep.MoveBottomXToInspection;
            return 0;
        }

        private async Task<int> MoveBottomYToAvoidBeforeInspectionAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                target,
                "bottom inspection entry Y avoid",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomXToInspection;
            return 0;
        }

        private async Task<int> MoveBottomXToInspectionAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                "bottom inspection X",
                ct,
                "DieBottomPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            if (_inspectionYPositionReady && IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY))
            {
                CurrentStep = PickerBottomInspectionStep.MoveBottomZ;
                return 0;
            }

            // 피커 간 이동에서는 Y Avoid 복귀 없이 현재 피커의 AlignOffsetY가 반영된 Y 위치만 보정한다.
            CurrentStep = PickerBottomInspectionStep.MoveBottomYToInspection;
            return 0;
        }

        private async Task<int> MoveBottomYToInspectionAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "bottom inspection Y",
                ct,
                "DieBottomPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            _inspectionYPositionReady = true;

            CurrentStep = PickerBottomInspectionStep.MoveBottomZ;
            return 0;
        }

        private async Task<int> MoveBottomZAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "bottom inspection Z",
                ct,
                "DieBottomPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomT;
            return 0;
        }

        private async Task<int> MoveBottomTAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(_currentPickerIndex),
                _targetPickerT,
                "bottom inspection T",
                ct,
                "DieBottomPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.RequestBottomInspection;
            return 0;
        }

        private async Task<int> RequestBottomInspectionAsync(CancellationToken ct)
        {
            try
            {
                int retryCount = Options != null && Options.VisionRetryCount > 0 ? Options.VisionRetryCount : 3;
                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    _bottomResult = await RequestBottomResultAsync(ct).ConfigureAwait(false);
                    if (_bottomResult != null)
                    {
                        CurrentStep = PickerBottomInspectionStep.ApplyBottomInspectionResult;
                        return 0;
                    }

                    WriteLog("PickerBottomInspectionSequence",
                        Name + " bottom inspection retry. die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", attempt=" + attempt + " - Check");
                }

                return Fail("PICKER-BOTTOM-VISION-FAIL", "Vision",
                    "Bottom inspection communication/result failed after retry. die=" +
                    _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-BOTTOM-VISION-EX", "Vision", "Bottom inspection exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyBottomInspectionResult()
        {
            MaterialInspectionResult inspectionResult = _bottomResult.IsOk
                ? MaterialInspectionResult.Ok
                : MaterialInspectionResult.Ng;

            DieResult dieResult = _bottomResult.IsOk && _currentDie.Result != DieResult.NG
                ? DieResult.Good
                : DieResult.NG;

            MaterialStateService.UpsertInspection(_currentDie.DieId, new DieInspectionRecord
            {
                InspectionType = "Bottom",
                Result = inspectionResult,
                Offset = new VisionOffset
                {
                    X = _bottomResult.OffsetX,
                    Y = _bottomResult.OffsetY,
                    R = _bottomResult.OffsetT,
                    IsValid = true
                },
                NgCodes = _bottomResult.IsOk
                    ? new List<string>()
                    : new List<string> { "BOTTOM_NG" },
                Alignments = new List<InspectionAlignmentSnapshot>
                {
                    BuildPickerAlignmentSnapshot(
                        "Bottom",
                        _currentPickerIndex,
                        _targetPickerX,
                        _targetPickerY,
                        _targetPickerT,
                        _targetPickerZ,
                        new VisionOffset
                        {
                            X = _bottomResult.OffsetX,
                            Y = _bottomResult.OffsetY,
                            R = _bottomResult.OffsetT,
                            IsValid = true
                        })
                },
                Measurements = new List<InspectionMeasurement>
                {
                    BuildMeasurement("BottomAlignOffsetX", _bottomResult.OffsetX, "mm", inspectionResult),
                    BuildMeasurement("BottomAlignOffsetY", _bottomResult.OffsetY, "mm", inspectionResult),
                    BuildMeasurement("BottomAlignOffsetT", _bottomResult.OffsetT, "deg", inspectionResult),
                    BuildBooleanMeasurement("BottomInspectionResult", _bottomResult.IsOk)
                }
            });

            MaterialStateService.ApplyDieInspectionResult(
                _currentDie.DieId,
                dieResult,
                _bottomResult.IsOk ? "" : "BOTTOM_NG",
                "BottomInspection");

            WriteLog("PickerBottomInspectionSequence",
                Name + " bottom inspection result. die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", ok=" + _bottomResult.IsOk +
                ", offsetX=" + _bottomResult.OffsetX +
                ", offsetY=" + _bottomResult.OffsetY + " - Ok");

            CurrentStep = PickerBottomInspectionStep.MoveBottomZToAvoid;
            return 0;
        }

        private async Task<int> MoveBottomZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "bottom inspection Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private async Task<int> MoveBottomTToSafeAsync(CancellationToken ct)
        {
            PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
            double target = GetPickerTeachingPosition(tAxis, "PickPosition") + ResolvePickerAlignOffsetT(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(tAxis, target, "bottom inspection T safe", ct, "DiePickPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomYToAvoid;
            return 0;
        }

        private async Task<int> MoveBottomYToAvoidAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                target,
                "bottom inspection Y avoid",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomXToAvoid;
            return 0;
        }

        private async Task<int> MoveBottomXToAvoidAsync(CancellationToken ct)
        {
            double target = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                target,
                "bottom inspection X avoid",
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.SelectNextPickerOrComplete;
            return 0;
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                CurrentStep = PickerBottomInspectionStep.Complete;
                return 0;
            }

            CurrentStep = PickerBottomInspectionStep.SelectNextPicker;
            return 0;
        }

        private async Task<BottomVisionOffset> RequestBottomResultAsync(CancellationToken ct)
        {
            await Task.Delay(VisionInspectionSettleDelayMs, ct).ConfigureAwait(false);

            if (IsSimulationOrDryRun())
                return SimulateBottomResult();

            ct.ThrowIfCancellationRequested();

            int timeoutMs = ResolveTimeout();
            WriteLog("PickerBottomInspectionSequence",
                Name + " request bottom vision. die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", pickerX=" + _targetPickerX +
                ", pickerY=" + _targetPickerY +
                ", pickerZ=" + _targetPickerZ +
                ", pickerT=" + _targetPickerT +
                ", timeoutMs=" + timeoutMs + " - Start");

            BottomVisionOffset result;
            if (Side == PickerSequenceSide.Front)
            {
                result = await FrontPicker.RequestBottomInspectionAsync(_currentPickerNo, timeoutMs, ct).ConfigureAwait(false);
            }
            else
            {
                result = await RearPicker.RequestBottomInspectionAsync(_currentPickerNo, timeoutMs, ct).ConfigureAwait(false);
            }

            if (result == null)
                return null;

            if (!result.IsOk)
            {
                WriteLog("PickerBottomInspectionSequence",
                    Name + " bottom vision returned NG. die=" + _currentDie.DieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", offsetX=" + result.OffsetX +
                    ", offsetY=" + result.OffsetY +
                    ", offsetT=" + result.OffsetT + " - Check");
            }

            return result;
        }

        private BottomVisionOffset SimulateBottomResult()
        {
            lock (SimVisionRandomLock)
            {
                return new BottomVisionOffset
                {
                    PickerNo = _currentPickerNo,
                    OffsetX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    OffsetY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    IsOk = true
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
                WriteLog("PickerBottomInspectionSequence", "InspectionArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}

