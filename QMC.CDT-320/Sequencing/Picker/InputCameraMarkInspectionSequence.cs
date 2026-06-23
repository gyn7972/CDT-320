using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputCameraMarkInspectionSequence : PickerSequenceBase<InputCameraMarkInspectionStep>
    {
        private readonly List<int> _enabledPickerIndexes = new List<int>();
        private readonly List<InputDieVisionPreparedItem> _inspectedItems = new List<InputDieVisionPreparedItem>();
        private SequenceResourceLease _inputStageLease;

        public InputCameraMarkInspectionSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.PickUp, side == PickerSequenceSide.Front ? "FrontInputCameraMarkInspectionSequence" : "RearInputCameraMarkInspectionSequence")
        {
            CurrentStep = InputCameraMarkInspectionStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == InputCameraMarkInspectionStep.Complete; }
        }

        public IList<InputDieVisionPreparedItem> InspectedItems
        {
            get { return _inspectedItems.AsReadOnly(); }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                while (CurrentStep != InputCameraMarkInspectionStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        CurrentStep = InputCameraMarkInspectionStep.Error;
                        return result;
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CurrentStep = InputCameraMarkInspectionStep.Error;
                return Fail("INPUT-CAMERA-MARK-INSPECTION-EX", Name,
                    "Input camera mark inspection failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                ReleaseInputStageArea();
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                case InputCameraMarkInspectionStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                case InputCameraMarkInspectionStep.BuildEnabledPickerList:
                    return Task.FromResult(BuildEnabledPickerList());

                case InputCameraMarkInspectionStep.RunInputCameraMarkInspection:
                    return RunInputCameraMarkInspectionAsync(ct);

                case InputCameraMarkInspectionStep.MoveInputVisionXToAvoid:
                    return MoveInputVisionXToAvoidAsync(ct);

                case InputCameraMarkInspectionStep.GrantPickUpPermission:
                    return Task.FromResult(GrantPickUpPermission());

                default:
                    return Task.FromResult(Fail("INPUT-CAMERA-MARK-INSPECTION-STEP", Name,
                        "Unsupported input camera mark inspection step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (!IsPickerSideEnabled())
                {
                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " picker side is disabled. Skip input camera mark inspection. side=" + Side + " - Check");
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                if (Context == null || Context.Machine == null || Context.Machine.InputStageUnit == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-MISSING", "InputStageUnit",
                        "InputStageUnit is missing. Input camera mark inspection cannot run.");

                CurrentStep = InputCameraMarkInspectionStep.BuildEnabledPickerList;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-CHECK-EX", Name,
                    "Input camera mark inspection check failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int BuildEnabledPickerList()
        {
            try
            {
                _enabledPickerIndexes.Clear();
                _inspectedItems.Clear();

                List<int> enabled = BuildEnabledPickerIndexes();
                for (int i = 0; i < enabled.Count; i++)
                {
                    int pickerIndex = enabled[i];
                    int pickerNo = ToPickerNo(pickerIndex);

                    if (Options != null &&
                        Options.RestrictToPickerNo > 0 &&
                        Options.RestrictToPickerNo != pickerNo)
                    {
                        continue;
                    }

                    _enabledPickerIndexes.Add(pickerIndex);
                }

                if (_enabledPickerIndexes.Count == 0)
                {
                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " no enabled picker for input camera mark inspection. side=" + Side + " - Check");
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " enabled picker list built for input camera mark inspection. count=" +
                    _enabledPickerIndexes.Count + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.RunInputCameraMarkInspection;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-PICKER-LIST-EX", Name,
                    "Input camera mark inspection picker list build failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunInputCameraMarkInspectionAsync(CancellationToken ct)
        {
            try
            {
                int acquireResult = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                if (acquireResult != 0)
                    return acquireResult;

                InputDieVisionPrepareSequence prepareSequence = new InputDieVisionPrepareSequence(
                    Context,
                    Side,
                    _enabledPickerIndexes);

                int result = await prepareSequence
                    .RunAsync(ct, Options ?? PickerSequenceOptions.Default())
                    .ConfigureAwait(false);
                if (result != 0)
                    return result;

                _inspectedItems.Clear();
                IList<InputDieVisionPreparedItem> preparedItems = prepareSequence.PreparedItems;
                for (int i = 0; i < preparedItems.Count; i++)
                    _inspectedItems.Add(preparedItems[i]);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " input camera mark inspection complete. inspectedCount=" +
                    _inspectedItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = _inspectedItems.Count > 0
                    ? InputCameraMarkInspectionStep.MoveInputVisionXToAvoid
                    : InputCameraMarkInspectionStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-RUN-EX", Name,
                    "Input camera mark inspection run failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AcquireInputStageAreaAsync(CancellationToken ct)
        {
            try
            {
                if (_inputStageLease != null)
                    return 0;

                _inputStageLease = await AcquireResourceAsync(
                    SequenceResourceKind.InputStageArea,
                    Name + ":InputCameraMarkInspection",
                    ct).ConfigureAwait(false);

                return _inputStageLease != null ? 0 : -1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-RESOURCE-EX", Name,
                    "InputStageArea resource acquire failed. error=" + ex.Message);
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
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputStageArea lease release failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputVisionXToAvoidAsync(CancellationToken ct)
        {
            try
            {
                InputStageUnit stage = Context != null && Context.Machine != null
                    ? Context.Machine.InputStageUnit
                    : null;
                if (stage == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-MISSING", "InputStageUnit",
                        "InputStageUnit is missing. InputVisionX avoid cannot run.");

                if (stage.Recipe == null)
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-STAGE-RECIPE", stage.Name,
                        "InputStage recipe is missing. InputVisionX avoid cannot run.");

                stage.Recipe.EnsurePositionObjects();
                double avoid = stage.Recipe.VisionX.AvoidPosition;
                if (!stage.IsVisionXInAvoidPosition())
                {
                    int moveResult = await stage.MoveInputStageAxis(
                        WaferStageAxis.VisionX,
                        avoid,
                        Options != null && Options.FineMove).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-MOVE", stage.Name,
                            "InputVisionX avoid move command failed. target=" + avoid +
                            ", result=" + moveResult);

                    int waitResult = await stage.WaitInputStageAxisInPosition(
                        WaferStageAxis.VisionX,
                        avoid,
                        ResolveTimeout(),
                        ct).ConfigureAwait(false);
                    if (waitResult != 0)
                        return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-WAIT", stage.Name,
                            "InputVisionX avoid wait failed. target=" + avoid +
                            ", result=" + waitResult);
                }

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-CHECK", stage.Name,
                        "InputVisionX is not in avoid position after mark inspection. target=" + avoid);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputVisionX avoid confirmed after input camera mark inspection. target=" +
                    avoid + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.GrantPickUpPermission;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-VISIONX-AVOID-EX", Name,
                    "InputVisionX avoid after mark inspection failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int GrantPickUpPermission()
        {
            try
            {
                if (_inspectedItems.Count == 0)
                {
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                InputCameraPickUpPermissionStore.Grant(Side, _inspectedItems);
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " pickup permission granted after input camera mark inspection. count=" +
                    _inspectedItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = InputCameraMarkInspectionStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("INPUT-CAMERA-MARK-INSPECTION-GRANT-EX", Name,
                    "Input camera mark inspection pickup permission grant failed. error=" + ex.Message);
            }
            finally
            {
            }
        }
    }
}
