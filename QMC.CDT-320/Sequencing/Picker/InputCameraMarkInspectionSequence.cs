using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class InputCameraMarkInspectionSequence : PickerSequenceBase<InputCameraMarkInspectionStep>
    {
        private readonly List<int> _enabledPickerIndexes = new List<int>();
        private readonly List<InputDieVisionPreparedItem> _inspectedItems = new List<InputDieVisionPreparedItem>();
        private SequenceResourceLease _inputStageLease;
        private AutoSequenceCameraWorkZoneLease _cameraWorkLease;

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
            catch (SequenceStopException)
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
                ReleaseInputCameraWorkZone();
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
                if (!IsInputCameraPreInspectionMode())
                {
                    int acquireResult = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (acquireResult != 0)
                        return acquireResult;
                }
                else
                {
                    int cameraZoneResult = await AcquireInputCameraWorkZoneAsync(ct).ConfigureAwait(false);
                    if (cameraZoneResult != 0)
                        return cameraZoneResult;

                    WriteLog("InputCameraMarkInspectionSequence",
                        Name + " InputCamera 선행검사 모드: Picker/Input 영역 대기 중에는 InputStageArea를 점유하지 않고 실제 Stage/Vision 이동 시점에만 점유합니다. side=" +
                        Side + " - Check");
                }

                InputDieVisionPrepareSequence prepareSequence = new InputDieVisionPrepareSequence(
                    Context,
                    Side,
                    _enabledPickerIndexes);

                int result = await prepareSequence
                    .RunAsync(ct, Options ?? PickerSequenceOptions.Default())
                    .ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (prepareSequence.PreparedItems.Count == 0)
                {
                    CurrentStep = InputCameraMarkInspectionStep.Complete;
                    return 0;
                }

                // 기존 조건: Task.WhenAll(CollectVisionResultsAsync, MoveInputVisionXToAvoidAsync)로
                //           RESULT 4건을 전부 회수한 뒤에야 허가를 발행했다 — RESULT 처리 시간이 픽업 허가를 지연.
                // 현재 기준: RESULT를 기다리지 않는다. 촬영(EPD)과 VisionX Avoid 완료만 확인하고 즉시 허가를
                //           발행하며, RESULT는 픽업 시퀀스가 CalculatePickTargets에서 회수한다 (조기 허가).
                int visionAvoidResult;
                try
                {
                    visionAvoidResult = await MoveInputVisionXToAvoidAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await prepareSequence.DrainPreparedResultsAfterFailureAsync(CancellationToken.None).ConfigureAwait(false);
                    prepareSequence.ReleasePreparedReservations();
                    throw;
                }

                if (visionAvoidResult != 0)
                {
                    // 허가 발행 전 실패: EPD 완료·RESULT 미회수 핸들을 드레인하고 예약을 해제한다.
                    await prepareSequence.DrainPreparedResultsAfterFailureAsync(ct).ConfigureAwait(false);
                    prepareSequence.ReleasePreparedReservations();
                    return visionAvoidResult;
                }

                _inspectedItems.Clear();
                IList<InputDieVisionPreparedItem> preparedItems = prepareSequence.PreparedItems;
                for (int i = 0; i < preparedItems.Count; i++)
                    _inspectedItems.Add(preparedItems[i]);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " input camera mark inspection EPD 단계 완료 — RESULT는 PickUp CalculatePickTargets에서 회수합니다. " +
                    "inspectedCount=" + _inspectedItems.Count + ", side=" + Side + " - Ok");

                CurrentStep = _inspectedItems.Count > 0
                    ? InputCameraMarkInspectionStep.GrantPickUpPermission
                    : InputCameraMarkInspectionStep.Complete;
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-RUN-EX", Name,
                    "Input camera mark inspection run failed. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AcquireInputCameraWorkZoneAsync(CancellationToken ct)
        {
            try
            {
                if (!IsInputCameraPreInspectionMode())
                    return 0;

                if (_cameraWorkLease != null && !_cameraWorkLease.IsDisposed)
                    return 0;

                if (Context == null || Context.AutoSequenceGate == null)
                    return 0;

                _cameraWorkLease = await Context.AutoSequenceGate
                    .BeginInputCameraWorkAsync(Name + ":InputCameraPreInspection:" + Side, ct)
                    .ConfigureAwait(false);

                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone approved. " +
                    "AutoSequenceCoordinator now owns InputCamera/Picker Input zone arbitration. side=" +
                    Side + " - Ok");
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
                return Fail("INPUT-CAMERA-MARK-INSPECTION-ZONE-EX", Name,
                    "InputCamera pre-inspection Input zone approval failed. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void ReleaseInputCameraWorkZone()
        {
            try
            {
                if (_cameraWorkLease == null)
                    return;

                _cameraWorkLease.Dispose();
                _cameraWorkLease = null;
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone released. side=" + Side + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("InputCameraMarkInspectionSequence",
                    Name + " InputCamera pre-inspection Input zone release failed. side=" + Side +
                    ", error=" + ex.Message + " - Failed");
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
            catch (SequenceStopException)
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

                if (IsInputCameraPreInspectionMode())
                {
                    int acquireResult = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (acquireResult != 0)
                        return acquireResult;
                }

                stage.Recipe.EnsurePositionObjects();
                double avoid = stage.Recipe.VisionX.AvoidPosition;
                double tolerance = stage.CameraX != null && stage.CameraX.Config != null && stage.CameraX.Config.InPositionTolerance > 0.0
                    ? stage.CameraX.Config.InPositionTolerance
                    : 0.01;
                if (!stage.CameraX.IsAtTargetPosition(avoid, tolerance))
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

        private bool IsInputCameraPreInspectionMode()
        {
            return Options != null &&
                   Options.RunMode == SequenceRunMode.Auto &&
                   Options.InputCameraPreInspectionMode;
        }
    }
}
