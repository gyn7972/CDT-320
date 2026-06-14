using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerPickUpSequence : PickerSequenceBase<PickerPickUpStep>
    {
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

        private readonly List<int> _enabledPickerIndexes = new List<int>();
        private int _pickerCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private string _currentDieId = "";
        private InputStagePickTarget _pickTarget;
        private VisionAlignResult _visionOffset;
        private double _targetStageY;
        private double _targetPickerX;
        private double _targetPickerT;
        private double _targetPickerZ;
        private bool _diePicked;
        private SequenceResourceLease _inputStageLease;

        public PickerPickUpSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.PickUp, side == PickerSequenceSide.Front ? "FrontPickerPickUpSequence" : "RearPickerPickUpSequence")
        {
            CurrentStep = PickerPickUpStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerPickUpStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ReleaseInputReservationIfNeeded();
                ReleaseInputStageArea();
                CurrentStep = PickerPickUpStep.Complete;
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
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerPickUpStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerPickUpStep.Complete)
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
                return Fail("PICKER-PICKUP-EX", Name, "Picker pickup failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                {
                    ReleaseInputReservationIfNeeded();
                    ReleaseInputStageArea();
                }
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                case PickerPickUpStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                case PickerPickUpStep.CheckPickerSideEnabled:
                    return Task.FromResult(CheckPickerSideEnabled());

                case PickerPickUpStep.BuildEnabledPickerList:
                    return Task.FromResult(BuildEnabledPickerList());

                case PickerPickUpStep.CheckInputStageReady:
                    return Task.FromResult(CheckInputStageReady());

                case PickerPickUpStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                case PickerPickUpStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                case PickerPickUpStep.ReserveNextInputDie:
                    return Task.FromResult(ReserveNextInputDie());

                case PickerPickUpStep.MoveInputStageAndVisionToDie:
                    return MoveInputStageAndVisionToDieAsync(ct);

                case PickerPickUpStep.RequestInputDieVisionInspection:
                    return RequestInputDieVisionInspectionAsync(ct);

                case PickerPickUpStep.ApplyInputDieVisionOffset:
                    return Task.FromResult(ApplyInputDieVisionOffset());

                case PickerPickUpStep.CalculatePickTarget:
                    return Task.FromResult(CalculatePickTarget());

                case PickerPickUpStep.MovePickerXStageYPickerT:
                    return MovePickerXStageYPickerTAsync(ct);

                case PickerPickUpStep.VerifyPickTarget:
                    return Task.FromResult(VerifyPickTarget());

                case PickerPickUpStep.MovePickerZPick:
                    return MovePickerZPickAsync(ct);

                case PickerPickUpStep.VacuumOn:
                    return VacuumOnAsync(ct);

                case PickerPickUpStep.VerifyDiePicked:
                    return Task.FromResult(VerifyDiePicked());

                case PickerPickUpStep.MovePickerZToAvoid:
                    return MovePickerZToAvoidAsync(ct);

                case PickerPickUpStep.UpdateMaterialToPicker:
                    return Task.FromResult(UpdateMaterialToPicker());

                case PickerPickUpStep.SelectNextPickerOrComplete:
                    return Task.FromResult(SelectNextPickerOrComplete());

                default:
                    return Task.FromResult(Fail("PICKER-PICKUP-STEP", Name, "Unsupported picker pickup step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker == null)
                return Fail("PICKER-PICKUP-FRONT-NO-UNIT", Name, "FrontPickerUnit is null.");

            if (Side == PickerSequenceSide.Rear && RearPicker == null)
                return Fail("PICKER-PICKUP-REAR-NO-UNIT", Name, "RearPickerUnit is null.");

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-PICKUP-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            CurrentStep = PickerPickUpStep.CheckPickerSideEnabled;
            return 0;
        }

        private int CheckPickerSideEnabled()
        {
            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerPickUpSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerPickUpStep.Complete;
                return 0;
            }

            CurrentStep = PickerPickUpStep.BuildEnabledPickerList;
            return 0;
        }

        private int BuildEnabledPickerList()
        {
            _enabledPickerIndexes.Clear();
            _enabledPickerIndexes.AddRange(BuildEnabledPickerIndexes());
            _pickerCursor = 0;

            if (_enabledPickerIndexes.Count == 0)
                return Fail("PICKER-PICKUP-NO-PICKER", Name, "No enabled picker was found. side=" + Side);

            WriteLog("PickerPickUpSequence",
                Name + " enabled picker order=" + string.Join(",", _enabledPickerIndexes.ConvertAll(i => i.ToString()).ToArray()) + " - Ok");

            CurrentStep = PickerPickUpStep.CheckInputStageReady;
            return 0;
        }

        private int CheckInputStageReady()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (wafer == null)
                return Fail("PICKER-PICKUP-NO-WAFER", "Material", "InputStage wafer material is not available.");

            if (wafer.DieIds == null || wafer.DieIds.Count == 0)
                return Fail("PICKER-PICKUP-NO-DIE", "Material", "InputStage wafer has no die data. waferId=" + wafer.WaferId);

            if (!MaterialStateService.HasReadyInputStagePickTarget())
            {
                WriteLog("PickerPickUpSequence", Name + " has no ready input die target. waferId=" + wafer.WaferId + " - Check");
                CurrentStep = PickerPickUpStep.Complete;
                return 0;
            }

            CurrentStep = PickerPickUpStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
            if (_inputStageLease == null)
            {
                _inputStageLease = await AcquireResourceAsync(SequenceResourceKind.InputStageArea, Name + ":PickUp", ct).ConfigureAwait(false);
                if (_inputStageLease == null)
                    return -1;
            }

            int result = await MoveAllPickerZToAvoidAndVerifyAsync("pickup pre all picker Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.SelectNextPicker;
            return 0;
        }

        private int SelectNextPicker()
        {
            if (_pickerCursor >= _enabledPickerIndexes.Count)
            {
                CurrentStep = PickerPickUpStep.Complete;
                return 0;
            }

            _currentPickerIndex = _enabledPickerIndexes[_pickerCursor];
            _currentPickerNo = ToPickerNo(_currentPickerIndex);
            _currentDieId = "";
            _pickTarget = null;
            _visionOffset = null;
            _diePicked = false;

            CurrentStep = PickerPickUpStep.ReserveNextInputDie;
            return 0;
        }

        private int ReserveNextInputDie()
        {
            _pickTarget = MaterialStateService.ReserveNextInputStagePickTarget(PickerLocationKind, _currentPickerNo);
            _currentDieId = _pickTarget != null ? _pickTarget.DieId : "";

            if (string.IsNullOrWhiteSpace(_currentDieId))
            {
                CurrentStep = PickerPickUpStep.SelectNextPickerOrComplete;
                return 0;
            }

            WriteLog("PickerPickUpSequence",
                Name + " reserved input die. die=" + _currentDieId +
                ", pickerNo=" + _currentPickerNo +
                ", grid=(" + _pickTarget.GridX + "," + _pickTarget.GridY + ")" +
                ", inputVisionX=" + _pickTarget.TargetX +
                ", inputStageY=" + _pickTarget.TargetY + " - Ok");

            CurrentStep = PickerPickUpStep.MoveInputStageAndVisionToDie;
            return 0;
        }

        private async Task<int> MoveInputStageAndVisionToDieAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                double targetY = _pickTarget.TargetY;
                double targetX = _pickTarget.TargetX;

                Task<int> moveY = MoveInputStageAxisCommandAsync(stage, WaferStageAxis.WaferY, targetY, "input die StageY", ct);
                Task<int> moveX = MoveInputStageAxisCommandAsync(stage, WaferStageAxis.VisionX, targetX, "input die VisionX", ct);
                int[] moveResults = await Task.WhenAll(moveY, moveX).ConfigureAwait(false);

                if (moveResults[0] != 0)
                    return moveResults[0];
                if (moveResults[1] != 0)
                    return moveResults[1];

                Task<int> waitY = WaitInputStageAxisInPositionResultAsync(stage, WaferStageAxis.WaferY, targetY, "input die StageY", ct);
                Task<int> waitX = WaitInputStageAxisInPositionResultAsync(stage, WaferStageAxis.VisionX, targetX, "input die VisionX", ct);
                int[] waitResults = await Task.WhenAll(waitY, waitX).ConfigureAwait(false);

                if (waitResults[0] != 0)
                    return waitResults[0];
                if (waitResults[1] != 0)
                    return waitResults[1];

                CurrentStep = PickerPickUpStep.RequestInputDieVisionInspection;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", Name, "Input die move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RequestInputDieVisionInspectionAsync(CancellationToken ct)
        {
            try
            {
                int retryCount = Options != null && Options.VisionRetryCount > 0 ? Options.VisionRetryCount : 3;
                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    _visionOffset = await RequestInputVisionOffsetAsync(ct).ConfigureAwait(false);
                    if (_visionOffset != null)
                    {
                        WriteLog("PickerPickUpSequence",
                            Name + " input die vision offset ok. die=" + _currentDieId +
                            ", pickerNo=" + _currentPickerNo +
                            ", attempt=" + attempt +
                            ", dx=" + _visionOffset.DeltaX +
                            ", dy=" + _visionOffset.DeltaY +
                            ", dt=" + _visionOffset.DeltaTheta + " - Ok");

                        CurrentStep = PickerPickUpStep.ApplyInputDieVisionOffset;
                        return 0;
                    }

                    WriteLog("PickerPickUpSequence",
                        Name + " input die vision offset retry. die=" + _currentDieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", attempt=" + attempt + " - Check");
                }

                return Fail("PICKER-PICKUP-VISION-NG", "Vision", "Input die vision inspection failed. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VISION-EX", "Vision", "Input die vision inspection exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyInputDieVisionOffset()
        {
            if (_visionOffset == null)
                return Fail("PICKER-PICKUP-VISION-OFFSET", "Vision", "Input die vision offset is missing.");

            MaterialStateService.UpsertInspection(_currentDieId, new DieInspectionRecord
            {
                InspectionType = "InputPickVision",
                Result = MaterialInspectionResult.Ok,
                Offset = new VisionOffset
                {
                    X = _visionOffset.DeltaX,
                    Y = _visionOffset.DeltaY,
                    R = _visionOffset.DeltaTheta,
                    IsValid = true
                }
            });

            CurrentStep = PickerPickUpStep.CalculatePickTarget;
            return 0;
        }

        private int CalculatePickTarget()
        {
            try
            {
                _targetStageY = _pickTarget.TargetY + _visionOffset.DeltaY;
                _targetPickerX = _pickTarget.TargetX +
                    ResolveInputVisionToPickerXOffset(_currentPickerIndex) +
                    ResolvePickerAlignOffsetX(_currentPickerIndex) +
                    _visionOffset.DeltaX;
                _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PickPosition") +
                    ResolvePickerAlignOffsetT(_currentPickerIndex) +
                    _visionOffset.DeltaTheta;
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PickPosition");

                WriteLog("PickerPickUpSequence",
                    Name + " calculated pick target. die=" + _currentDieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", stageY=" + _targetStageY +
                    ", pickerX=" + _targetPickerX +
                    ", pickerT=" + _targetPickerT +
                    ", pickerZ=" + _targetPickerZ + " - Ok");

                CurrentStep = PickerPickUpStep.MovePickerXStageYPickerT;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-TARGET-EX", Name, "Pick target calculation failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerXStageYPickerTAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = ResolveInputStage();
                if (stage == null)
                    return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);

                Task<int> moveStageY = MoveInputStageAxisCommandAsync(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY", ct);
                Task<int> movePickerX = MovePickerAxisCommandResultAsync(PickerAxis.PickerX, _targetPickerX, "pick corrected PickerX", ct);
                Task<int> movePickerT = MovePickerAxisCommandResultAsync(tAxis, _targetPickerT, "pick corrected PickerT", ct);
                int[] moveResults = await Task.WhenAll(moveStageY, movePickerX, movePickerT).ConfigureAwait(false);

                if (moveResults[0] != 0)
                    return moveResults[0];
                if (moveResults[1] != 0)
                    return moveResults[1];
                if (moveResults[2] != 0)
                    return moveResults[2];

                Task<int> waitStageY = WaitInputStageAxisInPositionResultAsync(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY", ct);
                Task<int> waitPickerX = WaitPickerAxisInPositionResultAsync(PickerAxis.PickerX, _targetPickerX, "pick corrected PickerX", ct);
                Task<int> waitPickerT = WaitPickerAxisInPositionResultAsync(tAxis, _targetPickerT, "pick corrected PickerT", ct);
                int[] waitResults = await Task.WhenAll(waitStageY, waitPickerX, waitPickerT).ConfigureAwait(false);

                if (waitResults[0] != 0)
                    return waitResults[0];
                if (waitResults[1] != 0)
                    return waitResults[1];
                if (waitResults[2] != 0)
                    return waitResults[2];

                CurrentStep = PickerPickUpStep.VerifyPickTarget;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-XYT-MOVE-EX", Name, "Pick XYT move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyPickTarget()
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return Fail("PICKER-PICKUP-STAGE-NO-UNIT", "InputStageUnit", "InputStageUnit is null.");

            int result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, _targetStageY, "pick corrected StageY");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX, "pick corrected PickerX");
            if (result != 0)
                return result;

            result = CheckPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT, "pick corrected PickerT");
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.MovePickerZPick;
            return 0;
        }

        private async Task<int> MovePickerZPickAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            int result = await MovePickerAxisAndVerifyAsync(zAxis, _targetPickerZ, "pick Z down", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.VacuumOn;
            return 0;
        }

        private async Task<int> VacuumOnAsync(CancellationToken ct)
        {
            try
            {
                SetPickerVacuum(_currentPickerNo, true);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);

                CurrentStep = PickerPickUpStep.VerifyDiePicked;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-VACUUM-EX", Name, "Picker vacuum on failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyDiePicked()
        {
            _diePicked = true;
            CurrentStep = PickerPickUpStep.MovePickerZToAvoid;
            return 0;
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "pick Z avoid after pickup", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPickUpStep.UpdateMaterialToPicker;
            return 0;
        }

        private int UpdateMaterialToPicker()
        {
            MaterialStateService.MoveDie(_currentDieId, MaterialLocation.Picker(PickerLocationKind, _currentPickerNo));
            RecordColletUse(_currentPickerNo);
            WriteLog("PickerPickUpSequence", Name + " picked die. die=" + _currentDieId + ", pickerNo=" + _currentPickerNo + " - Ok");

            _currentDieId = "";
            _pickTarget = null;
            _diePicked = false;
            CurrentStep = PickerPickUpStep.SelectNextPickerOrComplete;
            return 0;
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _enabledPickerIndexes.Count ||
                !MaterialStateService.HasReadyInputStagePickTarget())
            {
                CurrentStep = PickerPickUpStep.Complete;
                ReleaseInputStageArea();
                return 0;
            }

            CurrentStep = PickerPickUpStep.SelectNextPicker;
            return 0;
        }

        private async Task<VisionAlignResult> RequestInputVisionOffsetAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveInputStage();
            if (stage == null)
                return null;

            if (IsSimulationOrDryRun(stage))
                return SimulateInputVisionOffset();

            if (stage.Vision == null)
                return null;

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync("InputPickDie").ConfigureAwait(false);
        }

        private VisionAlignResult SimulateInputVisionOffset()
        {
            lock (SimVisionRandomLock)
            {
                if (Options != null && Options.SimulateVisionResult && SimVisionRandom.Next(0, 20) == 0)
                    return null;

                return new VisionAlignResult
                {
                    DeltaX = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaY = (SimVisionRandom.NextDouble() - 0.5) * 0.002,
                    DeltaTheta = (SimVisionRandom.NextDouble() - 0.5) * 0.02
                };
            }
        }

        private bool IsSimulationOrDryRun(InputStageUnit stage)
        {
            if (Options != null && Options.SimulateVisionResult)
                return true;

            if (stage != null && stage.IsInputStageSimulationOrDryRun())
                return true;

            return IsPickerSimulationOrDryRun();
        }

        private int ResolveVacuumSettleMs()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Recipe != null)
                return FrontPicker.Recipe.VacuumSettleMs;

            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Recipe != null)
                return RearPicker.Recipe.VacuumSettleMs;

            return 50;
        }

        private void RecordColletUse(int pickerNo)
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                FrontPicker.RecordColletUse(pickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                RearPicker.RecordColletUse(pickerNo);
        }

        private InputStageUnit ResolveInputStage()
        {
            return Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
        }

        private async Task<int> MoveInputStageAxisCommandAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(
                    stage.MoveInputStageAxis(axis, target, Options != null && Options.FineMove),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-PICKUP-STAGE-MOVE", stage.Name, description + " move command failed. result=" + result + ", " + BuildInputStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitInputStageAxisInPositionResultAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                AxisMoveWaitResult waitResult = await AwaitStepWithCancellationAsync(
                    stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout()),
                    ct).ConfigureAwait(false);

                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-STAGE", waitResult), stage.Name,
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, BuildInputStageAxisState(stage, axis, target)));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStageUnit", description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckInputStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            try
            {
                QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
                if (item == null)
                    return Fail("PICKER-PICKUP-STAGE-AXIS", stage != null ? stage.Name : "InputStageUnit", description + " axis is not available. " + BuildInputStageAxisState(stage, axis, target));

                if (item.IsMoving || item.IsAlarm || !IsAxisInPosition(item, target))
                    return Fail("PICKER-PICKUP-STAGE-POSITION", stage.Name, description + " final position check failed. " + BuildInputStageAxisState(stage, axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-STAGE-POSITION-EX", stage != null ? stage.Name : "InputStageUnit", description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerAxisCommandResultAsync(PickerAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MovePickerAxisCommandAsync(axis, target).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-PICKUP-MOVE-CMD", Name, description + " move command failed. result=" + result + ", " + BuildPickerAxisState(axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-CMD-EX", Name, description + " move command exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitPickerAxisInPositionResultAsync(PickerAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                AxisMoveWaitResult waitResult = await WaitPickerAxisMoveDoneAsync(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PICKUP-MOVE", waitResult), Name,
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, BuildPickerAxisState(axis, target)));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PICKUP-MOVE-WAIT-EX", Name, description + " move wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckPickerAxisInPosition(PickerAxis axis, double target, string description)
        {
            if (!IsPickerAxisInPosition(axis, target))
                return Fail("PICKER-PICKUP-MOVE-CHECK", Name, description + " final position check failed. " + BuildPickerAxisState(axis, target));

            return 0;
        }

        private static QMC.Common.Motion.BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                default:
                    return null;
            }
        }

        private static string BuildInputStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveInputStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance;
        }

        private static bool IsAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private static async Task<TResult> AwaitStepWithCancellationAsync<TResult>(Task<TResult> task, CancellationToken ct)
        {
            Task cancelTask = Task.Delay(Timeout.Infinite, ct);
            Task completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (completed == cancelTask)
                ct.ThrowIfCancellationRequested();

            return await task.ConfigureAwait(false);
        }

        private void ReleaseInputReservationIfNeeded()
        {
            try
            {
                if (_diePicked || string.IsNullOrWhiteSpace(_currentDieId))
                    return;

                MaterialStateService.ReleaseInputStagePickReservation(
                    _currentDieId,
                    PickerLocationKind,
                    _currentPickerNo);

                WriteLog("PickerPickUpSequence", Name + " released input die reservation. die=" + _currentDieId + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "Input die reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseInputStageArea()
        {
            try
            {
                if (_inputStageLease == null)
                    return;

                _inputStageLease.Dispose();
                _inputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPickUpSequence", "InputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
