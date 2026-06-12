using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerProcessSequence : PickerSequenceBase<PickerProcessStep>
    {
        private struct PickerStepRunResult
        {
            public int Result;
            public bool ReleaseResources;
        }

        private SequenceResourceLease _inputAreaLease;
        private SequenceResourceLease _inspectionAreaLease;
        private SequenceResourceLease _outputAreaLease;
        private string _currentDieId = "";
        private InputStagePickTarget _pickTarget;
        private DieGrade _inspectionGrade = DieGrade.Good;
        private bool _diePhysicallyPicked;
        private bool _diePlaced;

        public PickerProcessSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.Process, side == PickerSequenceSide.Front ? "FrontPickerSequence" : "RearPickerSequence")
        {
            CurrentStep = PickerProcessStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerProcessStep.Complete; }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool releaseResources = true;

            try
            {
                if (IsStepRunMode())
                {
                    PickerStepRunResult stepResult = await ExecuteSinglePickerStepAsync(ct).ConfigureAwait(false);
                    releaseResources = stepResult.ReleaseResources;
                    return stepResult.Result;
                }

                return await ExecutePickerStepsUntilCompleteAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                return Fail("PICKER-PROCESS-EX", Name, "Picker process failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (releaseResources)
                {
                    ReleaseInputReservationIfNeeded();
                    ReleaseAreaLeases();
                }
            }
        }

        private bool IsStepRunMode()
        {
            return Options != null && Options.RunMode != SequenceRunMode.Auto;
        }

        private async Task<PickerStepRunResult> ExecuteSinglePickerStepAsync(CancellationToken ct)
        {
            var stepResult = new PickerStepRunResult
            {
                Result = 0,
                ReleaseResources = true
            };

            if (CurrentStep == PickerProcessStep.Complete)
                return stepResult;

            ct.ThrowIfCancellationRequested();
            int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
            stepResult.Result = result;
            if (result == 0 && CurrentStep != PickerProcessStep.Complete)
                stepResult.ReleaseResources = false;

            return stepResult;
        }

        private async Task<int> ExecutePickerStepsUntilCompleteAsync(CancellationToken ct)
        {
            while (CurrentStep != PickerProcessStep.Complete)
            {
                ct.ThrowIfCancellationRequested();
                int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                case PickerProcessStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                case PickerProcessStep.CheckPickerSafe:
                    return CheckPickerSafeAsync(ct);

                case PickerProcessStep.WaitInputReady:
                    return WaitInputReadyAsync(ct);

                case PickerProcessStep.AcquireInputArea:
                    return AcquireInputAreaAsync(ct);

                case PickerProcessStep.MoveInputStageToReservedDie:
                    return MoveInputStageToReservedDieAsync(ct);

                case PickerProcessStep.MoveInputPickPosition:
                    return MoveInputPickPositionAsync(ct);

                case PickerProcessStep.PickDieFromInput:
                    return PickDieFromInputAsync(ct);

                case PickerProcessStep.MoveInputAvoidPosition:
                    return MoveInputAvoidPositionAsync(ct);

                case PickerProcessStep.MoveInspectionPosition:
                    return MoveInspectionPositionAsync(ct);

                case PickerProcessStep.InspectDie:
                    return InspectDieAsync(ct);

                case PickerProcessStep.WaitOutputReady:
                    return WaitOutputReadyAsync(ct);

                case PickerProcessStep.ResolveOutputSide:
                    return ResolveOutputSideAsync(ct);

                case PickerProcessStep.PrepareOutputReceivePosition:
                    return PrepareOutputReceivePositionAsync(ct);

                case PickerProcessStep.AcquireOutputArea:
                    return AcquireOutputAreaAsync(ct);

                case PickerProcessStep.MoveOutputPlacePosition:
                    return MoveOutputPlacePositionAsync(ct);

                case PickerProcessStep.PlaceDieToOutput:
                    return PlaceDieToOutputAsync(ct);

                case PickerProcessStep.MoveOutputAvoidPosition:
                    return MoveOutputAvoidPositionAsync(ct);

                case PickerProcessStep.InspectOutputPlacement:
                    return InspectOutputPlacementAsync(ct);

                default:
                    return Task.FromResult(Fail("PICKER-STEP", Name, "Unsupported picker step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker == null)
                return Fail("PICKER-FRONT-NO-UNIT", Name, "FrontPickerUnit is null.");
            if (Side == PickerSequenceSide.Rear && RearPicker == null)
                return Fail("PICKER-REAR-NO-UNIT", Name, "RearPickerUnit is null.");

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            CurrentStep = PickerProcessStep.CheckPickerSafe;
            return 0;
        }

        private async Task<int> CheckPickerSafeAsync(CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "picker Z safe before process",
                ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            CurrentStep = PickerProcessStep.WaitInputReady;
            return 0;
        }

        private async Task<int> WaitInputReadyAsync(CancellationToken ct)
        {
            if (!IsInputStageReady())
                await Context.Bus.WaitAsync("InputStageReady", ct).ConfigureAwait(false);

            CurrentStep = PickerProcessStep.AcquireInputArea;
            return 0;
        }

        private async Task<int> AcquireInputAreaAsync(CancellationToken ct)
        {
            _inputAreaLease = await AcquireResourceAsync(SequenceResourceKind.InputStageArea, Name + ":InputPick", ct).ConfigureAwait(false);
            if (_inputAreaLease == null)
                return -1;

            _pickTarget = MaterialStateService.ReserveNextInputStagePickTarget(PickerLocationKind, ResolvePickerNo());
            _currentDieId = _pickTarget != null ? _pickTarget.DieId : "";
            if (string.IsNullOrWhiteSpace(_currentDieId))
            {
                ReleaseInputArea();
                Context.Bus.Reset("InputStageReady");
                if (MaterialStateService.IsInputStagePickComplete())
                {
                    Context.Bus.Set("InputStageDieComplete");
                    WriteLog("PickerProcessSequence", Name + " input stage pick complete. request wafer exchange. - Check");
                }
                else
                {
                    WriteLog("PickerProcessSequence", Name + " input stage has no ready die. waiting for other picker progress. - Check");
                }

                CurrentStep = PickerProcessStep.Complete;
                return 0;
            }

            WriteLog("PickerProcessSequence",
                Name + " reserved input die. die=" + _currentDieId +
                ", order=" + _pickTarget.OrderIndex +
                ", grid=(" + _pickTarget.GridX + "," + _pickTarget.GridY + ")" +
                ", pickerNo=" + ResolvePickerNo() + " - Ok");

            CurrentStep = PickerProcessStep.MoveInputStageToReservedDie;
            return 0;
        }

        private async Task<int> MoveInputStageToReservedDieAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = Context != null && Context.Machine != null
                    ? Context.Machine.InputStageUnit
                    : null;

                if (stage == null)
                    return Fail("PICKER-INPUT-STAGE-MISSING", "InputStageUnit", "InputStageUnit is null.");

                if (_pickTarget == null || string.IsNullOrWhiteSpace(_pickTarget.DieId))
                    return Fail("PICKER-INPUT-DIE-MISSING", "Material", "Reserved input die target is missing. picker=" + Name + ", location=" + PickerLocationKind);

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

                int result = CheckInputStageAxisInPosition(stage, WaferStageAxis.WaferY, targetY, "input die StageY");
                if (result != 0)
                    return result;

                result = CheckInputStageAxisInPosition(stage, WaferStageAxis.VisionX, targetX, "input die VisionX");
                if (result != 0)
                    return result;

                WriteLog("PickerProcessSequence",
                    Name + " input stage moved to reserved die. die=" + _currentDieId +
                    ", stageY=" + targetY +
                    ", visionX=" + targetX + " - Ok");

                CurrentStep = PickerProcessStep.MoveInputPickPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-INPUT-STAGE-MOVE-EX", Name, "Input stage die move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputPickPositionAsync(CancellationToken ct)
        {
            int result = await MovePickerToDiePositionAndVerifyAsync(
                "DiePickPosition",
                ResolvePickerNo(),
                "input die pick",
                ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            CurrentStep = PickerProcessStep.PickDieFromInput;
            return 0;
        }

        private async Task<int> PickDieFromInputAsync(CancellationToken ct)
        {
            try
            {
                int pickerNo = ResolvePickerNo();
                SetPickerVacuum(pickerNo, true);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);

                MaterialStateService.MoveDie(_currentDieId, MaterialLocation.Picker(PickerLocationKind, pickerNo));
                RecordColletUse(pickerNo);
                _diePhysicallyPicked = true;
                WriteLog("PickerProcessSequence", Name + " picked die. die=" + _currentDieId + ", pickerNo=" + pickerNo + " - Ok");

                CurrentStep = PickerProcessStep.MoveInputAvoidPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                RecordPickFail();
                return Fail("PICKER-PICK-EX", Name, "Input die pick failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "picker Z safe after input pick",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            ReleaseInputArea();

            CurrentStep = PickerProcessStep.MoveInspectionPosition;
            return 0;
        }

        private async Task<int> MoveInspectionPositionAsync(CancellationToken ct)
        {
            _inspectionAreaLease = await AcquireResourceAsync(SequenceResourceKind.InspectionArea, Name + ":Inspection", ct).ConfigureAwait(false);
            if (_inspectionAreaLease == null)
                return -1;

            int result = await MovePickerToDiePositionAndVerifyAsync(
                "DieBottomPosition",
                ResolvePickerNo(),
                "bottom inspection",
                ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            CurrentStep = PickerProcessStep.InspectDie;
            return 0;
        }

        private Task<int> InspectDieAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                _inspectionGrade = SimulateInspectionGrade();
                MaterialInspectionResult result = _inspectionGrade == DieGrade.Ng
                    ? MaterialInspectionResult.Ng
                    : MaterialInspectionResult.Ok;

                MaterialStateService.UpsertInspection(_currentDieId, new DieInspectionRecord
                {
                    InspectionType = "PickerProcess",
                    Result = result
                });

                WriteLog("PickerProcessSequence", Name + " inspection complete. die=" + _currentDieId + ", grade=" + _inspectionGrade + " - Ok");
                ReleaseInspectionArea();

                CurrentStep = PickerProcessStep.WaitOutputReady;
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("PICKER-INSPECT-EX", Name, "Picker inspection failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> WaitOutputReadyAsync(CancellationToken ct)
        {
            BinSide outputSide = _inspectionGrade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;
            string signalName = outputSide == BinSide.Ng ? "OutputNgStageReady" : "OutputGoodStageReady";

            while (!IsOutputStageReady(outputSide))
            {
                ct.ThrowIfCancellationRequested();
                await Context.Bus.WaitAsync(signalName, ct).ConfigureAwait(false);
                if (!IsOutputStageReady(outputSide))
                    await Task.Delay(100, ct).ConfigureAwait(false);
            }

            CurrentStep = PickerProcessStep.ResolveOutputSide;
            return 0;
        }

        private Task<int> ResolveOutputSideAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            CurrentStep = PickerProcessStep.AcquireOutputArea;
            return Task.FromResult(0);
        }

        private async Task<int> PrepareOutputReceivePositionAsync(CancellationToken ct)
        {
            try
            {
                OutputStageSequenceOptions options = OutputStageSequenceOptions.Default();
                options.Grade = _inspectionGrade;
                options.Side = _inspectionGrade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;
                options.FineMove = Options != null && Options.FineMove;
                options.MoveTimeoutMs = ResolveTimeout();
                options.RunMode = Options != null ? Options.RunMode : SequenceRunMode.Auto;
                options.StartMode = Options != null ? Options.StartMode : SequenceStartMode.Resume;

                int result = await new OutputStageSequence(Context)
                    .RunReceiveDieAsync(ct, options)
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = PickerProcessStep.MoveOutputPlacePosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OUTPUT-PREPARE-EX", Name, "Output receive prepare failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AcquireOutputAreaAsync(CancellationToken ct)
        {
            SequenceResourceKind resource = _inspectionGrade == DieGrade.Ng
                ? SequenceResourceKind.OutputNgStageArea
                : SequenceResourceKind.OutputGoodStageArea;

            _outputAreaLease = await AcquireResourceAsync(resource, Name + ":OutputPlace", ct).ConfigureAwait(false);
            if (_outputAreaLease == null)
                return -1;

            CurrentStep = PickerProcessStep.PrepareOutputReceivePosition;
            return 0;
        }

        private async Task<int> MoveOutputPlacePositionAsync(CancellationToken ct)
        {
            int result = await MovePickerToDiePositionAndVerifyAsync(
                "DiePlacePosition",
                ResolvePickerNo(),
                "output die place",
                ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            CurrentStep = PickerProcessStep.PlaceDieToOutput;
            return 0;
        }

        private async Task<int> PlaceDieToOutputAsync(CancellationToken ct)
        {
            try
            {
                int pickerNo = ResolvePickerNo();
                BinSide outputSide = _inspectionGrade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;

                SetPickerVacuum(pickerNo, false);
                await PickerBlowAsync(pickerNo, ct).ConfigureAwait(false);
                if (!MaterialStateService.MoveDieToOutputStage(_currentDieId, outputSide))
                {
                    RecordPlaceFail();
                    return Fail("PICKER-PLACE-DATA", "Material", "Output die material update failed. die=" + _currentDieId + ", side=" + outputSide);
                }

                _diePlaced = true;
                Context.Bus.Set("InputStageReady");

                WriteLog("PickerProcessSequence", Name + " placed die. die=" + _currentDieId + ", grade=" + _inspectionGrade + " - Ok");
                CurrentStep = PickerProcessStep.MoveOutputAvoidPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                RecordPlaceFail();
                return Fail("PICKER-PLACE-EX", Name, "Output die place failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                "picker Z safe after output place",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerProcessStep.InspectOutputPlacement;
            return 0;
        }

        private async Task<int> InspectOutputPlacementAsync(CancellationToken ct)
        {
            try
            {
                OutputStageSequenceOptions options = OutputStageSequenceOptions.Default();
                options.Grade = _inspectionGrade;
                options.Side = _inspectionGrade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;
                options.FineMove = Options != null && Options.FineMove;
                options.MoveTimeoutMs = ResolveTimeout();
                options.RunMode = Options != null ? Options.RunMode : SequenceRunMode.Auto;
                options.StartMode = Options != null ? Options.StartMode : SequenceStartMode.Resume;

                int result = await new OutputStageSequence(Context)
                    .RunInspectBinAsync(ct, options)
                    .ConfigureAwait(false);

                if (result != 0)
                    return result;

                BinSide outputSide = _inspectionGrade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;
                if (MaterialStateService.IsOutputStageReceiveComplete(outputSide))
                {
                    Context.Bus.Reset(outputSide == BinSide.Ng ? "OutputNgStageReady" : "OutputGoodStageReady");
                    Context.Bus.Set(outputSide == BinSide.Ng ? "OutputNgStageReceiveComplete" : "OutputGoodStageReceiveComplete");
                }
                else
                {
                    Context.Bus.Set(outputSide == BinSide.Ng ? "OutputNgStageReady" : "OutputGoodStageReady");
                }

                ReleaseOutputArea();

                CurrentStep = PickerProcessStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-OUTPUT-INSPECT-EX", Name, "Output placement inspect failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsInputStageReady()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            return wafer != null &&
                   wafer.DieIds != null &&
                   wafer.DieIds.Count > 0 &&
                   !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId) &&
                   MaterialStateService.HasReadyInputStagePickTarget();
        }

        private static bool IsOutputStageReady(BinSide side)
        {
            return MaterialStateService.IsOutputStageReceiveAvailable(side);
        }

        private DieGrade SimulateInspectionGrade()
        {
            try
            {
                int seed = Environment.TickCount ^ (Side == PickerSequenceSide.Front ? 17 : 31);
                return Math.Abs(seed % 10) == 0 ? DieGrade.Ng : DieGrade.Good;
            }
            catch
            {
                return DieGrade.Good;
            }
            finally
            {
            }
        }

        private PickerAxis ResolvePickerZAxis(int pickerNo)
        {
            int index = pickerNo - 1;
            if (index <= 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        private double ResolveSafeZPosition()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Recipe != null)
                return FrontPicker.Recipe.PickerZ0.AvoidPosition;
            if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Recipe != null)
                return RearPicker.Recipe.PickerZ0.AvoidPosition;
            return 0.0;
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

        private void RecordPickFail()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                FrontPicker.RecordPickFail();
            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                RearPicker.RecordPickFail();
        }

        private void RecordPlaceFail()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                FrontPicker.RecordPlaceFail();
            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                RearPicker.RecordPlaceFail();
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
                    return Fail("PICKER-INPUT-STAGE-MOVE", stage.Name, description + " move command failed. result=" + result + ", " + BuildInputStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-INPUT-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStageUnit", description + " move command exception: " + ex.Message);
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

                int result = await AwaitStepWithCancellationAsync(
                    stage.WaitInputStageAxisInPosition(axis, target, ResolveTimeout()),
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("PICKER-INPUT-STAGE-MOVE-TIMEOUT", stage.Name, description + " move done timeout. waitResult=" + result + ", " + BuildInputStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-INPUT-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStageUnit", description + " move wait exception: " + ex.Message);
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
                    return Fail("PICKER-INPUT-STAGE-AXIS", stage != null ? stage.Name : "InputStageUnit", description + " axis is not available. " + BuildInputStageAxisState(stage, axis, target));

                if (item.IsMoving || item.IsAlarm || !IsInputStageAxisInPosition(item, target))
                    return Fail("PICKER-INPUT-STAGE-POSITION", stage.Name, description + " final position check failed. " + BuildInputStageAxisState(stage, axis, target));

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-INPUT-STAGE-POSITION-EX", stage != null ? stage.Name : "InputStageUnit", description + " final position check exception: " + ex.Message);
            }
            finally
            {
            }
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

        private static bool IsInputStageAxisInPosition(QMC.Common.Motion.BaseAxis axis, double target)
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

        private void ReleaseAreaLeases()
        {
            ReleaseInputArea();
            ReleaseInspectionArea();
            ReleaseOutputArea();
        }

        private void ReleaseInputReservationIfNeeded()
        {
            try
            {
                if (_diePlaced || _diePhysicallyPicked || string.IsNullOrWhiteSpace(_currentDieId))
                    return;

                MaterialStateService.ReleaseInputStagePickReservation(
                    _currentDieId,
                    PickerLocationKind,
                    ResolvePickerNo());

                WriteLog("PickerProcessSequence", Name + " released input die reservation. die=" + _currentDieId + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerProcessSequence", "Input die reservation release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseInputArea()
        {
            if (_inputAreaLease != null)
            {
                _inputAreaLease.Dispose();
                _inputAreaLease = null;
            }
        }

        private void ReleaseInspectionArea()
        {
            if (_inspectionAreaLease != null)
            {
                _inspectionAreaLease.Dispose();
                _inspectionAreaLease = null;
            }
        }

        private void ReleaseOutputArea()
        {
            if (_outputAreaLease != null)
            {
                _outputAreaLease.Dispose();
                _outputAreaLease = null;
            }
        }
    }
}
