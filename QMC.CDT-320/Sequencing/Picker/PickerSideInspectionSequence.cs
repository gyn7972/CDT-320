using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerSideInspectionSequence : PickerSequenceBase<PickerSideInspectionStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

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

                // 다음 피커 선택
                case PickerSideInspectionStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                // 사이드 XY 이동
                case PickerSideInspectionStep.MoveSideXY:
                    return MoveSideXYAsync(ct);

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
                case PickerSideInspectionStep.MoveSideXYToAvoid:
                    return MoveSideXYToAvoidAsync(ct);

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

            List<int> enabled = BuildEnabledPickerIndexes();
            for (int i = 0; i < enabled.Count; i++)
            {
                int index = enabled[i];
                int pickerNo = ToPickerNo(index);
                DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                if (die != null)
                    _pickedPickerIndexes.Add(index);
            }

            _pickerCursor = 0;

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
            if (_inspectionAreaLease == null)
            {
                _inspectionAreaLease = await AcquireResourceAsync(SequenceResourceKind.InspectionArea, Name + ":Side", ct).ConfigureAwait(false);
                if (_inspectionAreaLease == null)
                    return -1;
            }

            int result = await MoveAllPickerZToAvoidAndVerifyAsync("side inspection pre all picker Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

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

            CurrentStep = PickerSideInspectionStep.MoveSideXY;
            return 0;
        }

        private async Task<int> MoveSideXYAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            int result = await MovePickerAxesAndVerifyAsync(
                targets,
                "side inspection XY",
                ct,
                "DieSidePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideZ;
            return 0;
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

            CurrentStep = PickerSideInspectionStep.ApplySideInspectionResult;
            return 0;
        }

        private int ApplySideInspectionResult()
        {
            bool ok0 = _side0Result != null && _side0Result.IsAllOk;
            bool ok90 = _side90Result != null && _side90Result.IsAllOk;
            bool ok = ok0 && ok90 && _currentDie.Result != DieResult.NG;

            MaterialStateService.UpsertInspection(_currentDie.DieId, new DieInspectionRecord
            {
                InspectionType = "Side0",
                Result = ok0 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
            });

            MaterialStateService.UpsertInspection(_currentDie.DieId, new DieInspectionRecord
            {
                InspectionType = "Side90",
                Result = ok90 ? MaterialInspectionResult.Ok : MaterialInspectionResult.Ng
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

        private async Task<int> MoveSideZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "side inspection Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideTToSafe;
            return 0;
        }

        private async Task<int> MoveSideTToSafeAsync(CancellationToken ct)
        {
            PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
            double target = GetPickerTeachingPosition(tAxis, "PickPosition") + ResolvePickerAlignOffsetT(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(tAxis, target, "side inspection T safe", ct, "DiePickPosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerSideInspectionStep.MoveSideXYToAvoid;
            return 0;
        }

        private async Task<int> MoveSideXYToAvoidAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            targets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

            int result = await MovePickerAxesAndVerifyAsync(targets, "side inspection XY avoid", ct, "AvoidPosition").ConfigureAwait(false);
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
                result = await FrontPicker.RequestSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs).ConfigureAwait(false);
            }
            else
            {
                result = await RearPicker.RequestSideInspectionAsync(_currentPickerNo, angleDeg, timeoutMs).ConfigureAwait(false);
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
                bool ok = !Options.SimulateVisionResult || SimVisionRandom.Next(0, 20) != 0;
                return new SideVisionResult
                {
                    PickerNo = _currentPickerNo,
                    Side1Ok = ok,
                    Side2Ok = ok,
                    Side3Ok = ok,
                    Side4Ok = ok
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

