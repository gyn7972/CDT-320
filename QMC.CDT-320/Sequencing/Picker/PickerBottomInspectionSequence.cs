using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerBottomInspectionSequence : PickerSequenceBase<PickerBottomInspectionStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

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
                case PickerBottomInspectionStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                case PickerBottomInspectionStep.BuildPickedPickerList:
                    return Task.FromResult(BuildPickedPickerList());

                case PickerBottomInspectionStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                case PickerBottomInspectionStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                case PickerBottomInspectionStep.MoveBottomXY:
                    return MoveBottomXYAsync(ct);

                case PickerBottomInspectionStep.MoveBottomZ:
                    return MoveBottomZAsync(ct);

                case PickerBottomInspectionStep.MoveBottomT:
                    return MoveBottomTAsync(ct);

                case PickerBottomInspectionStep.RequestBottomInspection:
                    return RequestBottomInspectionAsync(ct);

                case PickerBottomInspectionStep.ApplyBottomInspectionResult:
                    return Task.FromResult(ApplyBottomInspectionResult());

                case PickerBottomInspectionStep.MoveBottomZToAvoid:
                    return MoveBottomZToAvoidAsync(ct);

                case PickerBottomInspectionStep.MoveBottomTToSafe:
                    return MoveBottomTToSafeAsync(ct);

                case PickerBottomInspectionStep.MoveBottomXYToAvoid:
                    return MoveBottomXYToAvoidAsync(ct);

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
                WriteLog("PickerBottomInspectionSequence", Name + " skipped because no die exists on picker. - Check");
                CurrentStep = PickerBottomInspectionStep.Complete;
                return 0;
            }

            CurrentStep = PickerBottomInspectionStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
            if (_inspectionAreaLease == null)
            {
                _inspectionAreaLease = await AcquireResourceAsync(SequenceResourceKind.InspectionArea, Name + ":Bottom", ct).ConfigureAwait(false);
                if (_inspectionAreaLease == null)
                    return -1;
            }

            int result = await MoveAllPickerZToAvoidAndVerifyAsync("bottom inspection pre all picker Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

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

            CurrentStep = PickerBottomInspectionStep.MoveBottomXY;
            return 0;
        }

        private async Task<int> MoveBottomXYAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            int result = await MovePickerAxesAndVerifyAsync(targets, "bottom inspection XY", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomZ;
            return 0;
        }

        private async Task<int> MoveBottomZAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(GetPickerZAxis(_currentPickerIndex), _targetPickerZ, "bottom inspection Z", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomT;
            return 0;
        }

        private async Task<int> MoveBottomTAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(GetPickerTAxis(_currentPickerIndex), _targetPickerT, "bottom inspection T", ct).ConfigureAwait(false);
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
                    IsValid = true
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
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "bottom inspection Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomTToSafe;
            return 0;
        }

        private async Task<int> MoveBottomTToSafeAsync(CancellationToken ct)
        {
            PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
            double target = GetPickerTeachingPosition(tAxis, "PickPosition") + ResolvePickerAlignOffsetT(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(tAxis, target, "bottom inspection T safe", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerBottomInspectionStep.MoveBottomXYToAvoid;
            return 0;
        }

        private async Task<int> MoveBottomXYToAvoidAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");
            targets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

            int result = await MovePickerAxesAndVerifyAsync(targets, "bottom inspection XY avoid", ct).ConfigureAwait(false);
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
                result = await FrontPicker.RequestBottomInspectionAsync(_currentPickerNo, timeoutMs).ConfigureAwait(false);
            }
            else
            {
                result = await RearPicker.RequestBottomInspectionAsync(_currentPickerNo, timeoutMs).ConfigureAwait(false);
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
                bool ok = !Options.SimulateVisionResult || SimVisionRandom.Next(0, 20) != 0;
                return new BottomVisionOffset
                {
                    PickerNo = _currentPickerNo,
                    OffsetX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    OffsetY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    IsOk = ok
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
                WriteLog("PickerBottomInspectionSequence", "InspectionArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
