using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal enum PickUpZCalibrationStep
    {
        None,
        CheckReady,
        ReserveArea,
        MoveZSafe,
        MoveScanStart,
        VacuumOn,
        SearchFlow,
        SaveResult,
        MoveAvoid,
        Complete
    }

    internal sealed class PickUpZCalibrationResult
    {
        public bool Success { get; set; }
        public VisionFocusPickerSide Side { get; set; }
        public int PickerNo { get; set; }
        public double OldPickPosition { get; set; }
        public double ScanStartPosition { get; set; }
        public double SearchLimitPosition { get; set; }
        public double DetectedFlowPosition { get; set; }
        public double SavedPickPosition { get; set; }
        public int DetectElapsedMs { get; set; }
        public string Message { get; set; }
    }

    internal sealed class PickUpZFlowSearchResult
    {
        public int ResultCode { get; set; }
        public double DetectedPosition { get; set; }
        public int ElapsedMs { get; set; }

        public bool Success
        {
            get { return ResultCode == 0; }
        }
    }

    internal sealed class PickerPickUpZCalibrationSequence : PickerSequenceBase<PickUpZCalibrationStep>
    {
        private const string SearchTargetName = "PickUpZCalibration;PickerZone=Input";

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly int _pickerNo;
        private readonly int _pickerIndex;
        private SequenceResourceLease _pickerLease;
        private SequenceResourceLease _inputStageLease;
        private SequenceResourceLease _outputStageLease;
        private PickUpZCalibrationSettings _settings;
        private PickerAxis _pickerZAxis;
        private PickCoordinateResult _calibrationTarget;
        private double _calibrationInputVisionX;
        private double _calibrationInputStageY;
        private double _oldPickPosition;
        private double _scanStartPosition;
        private double _searchLimitPosition;
        private double _searchDirection;
        private double _detectedFlowPosition;
        private double _savedPickPosition;
        private int _detectElapsedMs;

        public PickerPickUpZCalibrationSequence(MachineSequenceContext context, VisionFocusPickerSide side, int pickerNo)
            : base(
                  context,
                  side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                  PickerSequenceKind.PickUp,
                  "PickUpZCalibration")
        {
            _calibrationSide = side;
            _pickerNo = NormalizePickerNo(pickerNo);
            _pickerIndex = _pickerNo - 1;
            Result = new PickUpZCalibrationResult
            {
                Side = side,
                PickerNo = _pickerNo,
                Message = string.Empty
            };
        }

        public PickUpZCalibrationResult Result { get; private set; }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool vacuumOn = false;
            try
            {
                CurrentStep = PickUpZCalibrationStep.CheckReady;
                int result = CheckReady();
                if (result != 0) return result;

                CurrentStep = PickUpZCalibrationStep.ReserveArea;
                result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUpZCalibration");

                CurrentStep = PickUpZCalibrationStep.MoveZSafe;
                result = await PrepareSafeStartPositionAsync("PickUpZ Calibration 시작 전 안전 위치 이동", ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = PickUpZCalibrationStep.MoveScanStart;
                result = await MovePickerAxisAndVerifyAsync(
                    _pickerZAxis,
                    _scanStartPosition,
                    "PickUpZ Calibration Scan Start",
                    ct,
                    SearchTargetName).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = PickUpZCalibrationStep.VacuumOn;
                SetPickerVacuum(_pickerNo, true);
                vacuumOn = true;
                if (_settings.VacuumOnDelayMs > 0)
                    await Task.Delay(_settings.VacuumOnDelayMs, ct).ConfigureAwait(false);

                CurrentStep = PickUpZCalibrationStep.SearchFlow;
                result = await SearchFlowPositionWithResetAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                SetPickerVacuum(_pickerNo, false);
                vacuumOn = false;

                CurrentStep = PickUpZCalibrationStep.SaveResult;
                result = SaveCalibrationResult();
                if (result != 0) return result;

                CurrentStep = PickUpZCalibrationStep.MoveAvoid;
                if (_settings.MoveAvoidAfterScan)
                {
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition"),
                        "PickUpZ Calibration 완료 후 PickerZ Avoid",
                        ct,
                        "AvoidPosition").ConfigureAwait(false);
                    if (result != 0) return result;
                }

                CurrentStep = PickUpZCalibrationStep.Complete;
                Result.Success = true;
                Result.Message = "PickUpZ Calibration complete.";
                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration 완료. side=" + _calibrationSide +
                    ", pickerNo=" + _pickerNo +
                    ", oldPickZ=" + _oldPickPosition.ToString("F6") +
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPickZ=" + _savedPickPosition.ToString("F6") +
                    ", dieThickness=" + (_settings != null ? _settings.DieThicknessMm.ToString("F6") : "0.000000") +
                    ", filmThickness=" + (_settings != null ? _settings.FilmThicknessMm.ToString("F6") : "0.000000") +
                    ", elapsedMs=" + _detectElapsedMs + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                StopPickerZAxis();
                Result.Message = "PickUpZ Calibration canceled.";
                throw;
            }
            catch (SequenceStopException)
            {
                StopPickerZAxis();
                throw;
            }
            catch (Exception ex)
            {
                StopPickerZAxis();
                return Fail("PICKUP-Z-CAL-EX", Name, "PickUpZ Calibration 예외 발생. error=" + ex.Message);
            }
            finally
            {
                if (vacuumOn)
                {
                    try { SetPickerVacuum(_pickerNo, false); }
                    catch { }
                }

                ReleasePickerWorkArea();
                ReleaseArea();
            }
        }

        public async Task<int> MoveScanStartOnlyAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            SetOptionsForManualOperation(options);
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options != null && Options.RunMode == SequenceRunMode.Auto,
                "PickerPickUpZCalibrationSequence.MoveScanStartOnlyAsync:" + (Options != null ? Options.RunMode.ToString() : "-")))
            {
                try
                {
                    CurrentStep = PickUpZCalibrationStep.CheckReady;
                    int result = CheckReady();
                    if (result != 0) return result;

                    CurrentStep = PickUpZCalibrationStep.ReserveArea;
                    result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUpZCalibrationMoveStart");

                    CurrentStep = PickUpZCalibrationStep.MoveZSafe;
                    result = await PrepareSafeStartPositionAsync("PickUpZ Calibration Start 이동 전 안전 위치 이동", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = PickUpZCalibrationStep.MoveScanStart;
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        _scanStartPosition,
                        "PickUpZ Calibration Scan Start",
                        ct,
                        SearchTargetName).ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "PickUpZ Calibration scan start move complete.";
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    StopPickerZAxis();
                    throw;
                }
                catch (Exception ex)
                {
                    StopPickerZAxis();
                    return Fail("PICKUP-Z-CAL-MOVE-START-EX", Name,
                        "PickUpZ Calibration Scan Start 이동 예외 발생. error=" + ex.Message);
                }
                finally
                {
                    ReleasePickerWorkArea();
                    ReleaseArea();
                }
            }
        }

        public async Task<int> MoveAvoidOnlyAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            SetOptionsForManualOperation(options);
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options != null && Options.RunMode == SequenceRunMode.Auto,
                "PickerPickUpZCalibrationSequence.MoveAvoidOnlyAsync:" + (Options != null ? Options.RunMode.ToString() : "-")))
            {
                try
                {
                    CurrentStep = PickUpZCalibrationStep.CheckReady;
                    int result = CheckReady();
                    if (result != 0) return result;

                    CurrentStep = PickUpZCalibrationStep.MoveAvoid;
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition"),
                        "PickUpZ Calibration PickerZ Avoid",
                        ct,
                        "AvoidPosition").ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "PickUpZ Calibration avoid move complete.";
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    StopPickerZAxis();
                    throw;
                }
                catch (Exception ex)
                {
                    StopPickerZAxis();
                    return Fail("PICKUP-Z-CAL-AVOID-EX", Name,
                        "PickUpZ Calibration PickerZ Avoid 이동 예외 발생. error=" + ex.Message);
                }
                finally
                {
                }
            }
        }

        private int CheckReady()
        {
            try
            {
                if (Context == null || Context.Machine == null)
                    return Fail("PICKUP-Z-CAL-MACHINE", Name, "Machine is null.");
                if (Context.Machine.VisionUnit == null || Context.Machine.VisionUnit.Config == null)
                    return Fail("PICKUP-Z-CAL-VISION", Name, "VisionUnit calibration config is null.");
                if (Side == PickerSequenceSide.Front && FrontPicker == null)
                    return Fail("PICKUP-Z-CAL-FRONT", Name, "FrontPickerUnit is null.");
                if (Side == PickerSequenceSide.Rear && RearPicker == null)
                    return Fail("PICKUP-Z-CAL-REAR", Name, "RearPickerUnit is null.");
                if (!IsPickerSideEnabled())
                    return Fail("PICKUP-Z-CAL-SIDE-DISABLED", Name, "Picker side is disabled. side=" + Side);
                if (!IsPickerIndexEnabled(_pickerIndex))
                    return Fail("PICKUP-Z-CAL-PICKER-DISABLED", Name, "Picker is disabled. pickerNo=" + _pickerNo);

                Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                Context.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                Context.Machine.VisionUnit.Config.CalibrationData.PickUpZ.EnsureObjects();
                _settings = Context.Machine.VisionUnit.Config.CalibrationData.PickUpZ.Settings;
                _settings.EnsureDefaults();
                SetCalibrationMotion(_settings.Motion);

                _pickerZAxis = GetPickerZAxis(_pickerIndex);
                _oldPickPosition = GetPickerTeachingPosition(_pickerZAxis, "PickPosition");
                double avoid = GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition");
                double downSign = Math.Sign(_oldPickPosition - avoid);
                if (downSign == 0.0)
                    return Fail("PICKUP-Z-CAL-Z-TEACH", Name,
                        "PickPosition과 AvoidPosition이 같아 하강 방향을 계산할 수 없습니다. " +
                        "side=" + Side + ", pickerNo=" + _pickerNo +
                        ", pick=" + _oldPickPosition.ToString("F6") +
                        ", avoid=" + avoid.ToString("F6"));

                InputStageUnit stage = Context.Machine.InputStageUnit;
                if (stage == null || stage.Recipe == null)
                    return Fail("PICKUP-Z-CAL-INPUT-STAGE", Name, "InputStageUnit or recipe is null.");
                stage.Recipe.EnsurePositionObjects();

                _calibrationInputVisionX = stage.Recipe.VisionX.ProcessPosition + _settings.PositionOffsetXmm;
                _calibrationInputStageY = stage.Recipe.WaferY.ProcessPosition + _settings.PositionOffsetYmm;

                string targetReason;
                if (!PickerMotionTargetResolver.TryCalculateInputPickTarget(
                    Context.Machine,
                    Side,
                    _pickerIndex,
                    "PickUpZCalibration",
                    "PICKUP-Z-CAL",
                    _calibrationInputVisionX,
                    _calibrationInputStageY,
                    0.0,
                    0.0,
                    0.0,
                    true,
                    out _calibrationTarget,
                    out targetReason))
                {
                    return Fail("PICKUP-Z-CAL-TARGET", Name,
                        "PickUpZ Calibration input pick target calculation failed. reason=" + targetReason);
                }

                string workAreaReason;
                if (!stage.IsNeedleWorkPointInArea(_calibrationTarget.NeedleX, _calibrationTarget.StageY, out workAreaReason))
                {
                    return Fail("PICKUP-Z-CAL-WORK-AREA", stage.Name,
                        "PickUpZ Calibration Offset X/Y 목표가 InputStage 작업 영역을 벗어났습니다. " +
                        "inputVisionX=" + _calibrationInputVisionX.ToString("F6") +
                        ", inputStageY=" + _calibrationInputStageY.ToString("F6") +
                        ", targetNeedleX=" + _calibrationTarget.NeedleX.ToString("F6") +
                        ", targetStageY=" + _calibrationTarget.StageY.ToString("F6") +
                        ", reason=" + workAreaReason);
                }

                _scanStartPosition = _settings.StartZMm;
                _searchDirection = downSign;
                _searchLimitPosition = _scanStartPosition + (downSign * Math.Abs(_settings.SearchMaxDistanceMm));

                Result.OldPickPosition = _oldPickPosition;
                Result.ScanStartPosition = _scanStartPosition;
                Result.SearchLimitPosition = _searchLimitPosition;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKUP-Z-CAL-CHECK-EX", Name, "PickUpZ Calibration 준비 확인 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ReserveAreaAsync(CancellationToken ct)
        {
            try
            {
                _pickerLease = await AcquireResourceAsync(PickerResourceKind, Name + ":" + Side, ct).ConfigureAwait(false);
                if (_pickerLease == null)
                    return Fail("PICKUP-Z-CAL-RESOURCE", Name, "Picker resource acquire failed. side=" + Side);

                _inputStageLease = await AcquireResourceAsync(SequenceResourceKind.InputStageArea, Name + ":InputStageArea", ct).ConfigureAwait(false);
                if (_inputStageLease == null)
                    return Fail("PICKUP-Z-CAL-RESOURCE", Name, "InputStageArea resource acquire failed.");

                _outputStageLease = await AcquireResourceAsync(SequenceResourceKind.OutputPlaceArea, Name + ":OutputVisionArea", ct).ConfigureAwait(false);
                if (_outputStageLease == null)
                    return Fail("PICKUP-Z-CAL-RESOURCE", Name, "OutputPlaceArea resource acquire failed.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKUP-Z-CAL-RESOURCE-EX", Name, "PickUpZ Calibration 리소스 점유 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareSafeStartPositionAsync(string description, CancellationToken ct)
        {
            return await PrepareSafeStartPositionCoreAsync(description, ct).ConfigureAwait(false);
        }

        private async Task<int> PrepareSafeStartPositionCoreAsync(string description, CancellationToken ct)
        {
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " - PickerZ all Avoid", ct, true).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                description + " - PickerY Avoid",
                ct,
                "AvoidPosition;PickerPhase=SafeY",
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveOppositePickerToAvoidAndVerifyAsync(description + " - Opposite Picker Avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await EnsureInputOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveInputStageToCalibrationProcessAsync(description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveCurrentPickerToCalibrationTargetAsync(description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PickUpZCalibration",
                Name + " safe start position ready. side=" + Side +
                ", pickerNo=" + _pickerNo +
                ", inputVisionXBase=" + _calibrationInputVisionX.ToString("F6") +
                ", inputStageYBase=" + _calibrationInputStageY.ToString("F6") +
                ", targetPickerX=" + (_calibrationTarget != null ? _calibrationTarget.PickerX.ToString("F6") : "-") +
                ", targetPickerY=" + (_calibrationTarget != null ? _calibrationTarget.PickerY.ToString("F6") : "-") +
                ", targetPickerT=" + (_calibrationTarget != null ? _calibrationTarget.PickerT.ToString("F6") : "-") +
                ", targetNeedleX=" + (_calibrationTarget != null ? _calibrationTarget.NeedleX.ToString("F6") : "-") +
                ", targetNeedleZ=" + (_calibrationTarget != null ? _calibrationTarget.NeedleZ.ToString("F6") : "-") +
                ", formula=" + (_calibrationTarget != null ? _calibrationTarget.Formula : "-") +
                " - Ok");
            return 0;
        }

        private async Task<int> EnsureInputOutputVisionAvoidForStartAsync(CancellationToken ct)
        {
            int result = await EnsureInputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await EnsureOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> EnsureInputVisionAvoidForStartAsync(CancellationToken ct)
        {
            InputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                return Fail("PICKUP-Z-CAL-INPUT-VISION-MISSING", "InputStageUnit",
                    "PickUpZ Calibration start InputVisionX Avoid move requires axis/recipe.");

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (stage.IsVisionXInAvoidPosition())
                return 0;

            return await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.VisionX,
                target,
                "PickUpZ Calibration InputVisionX Avoid",
                ct).ConfigureAwait(false);
        }

        private async Task<int> EnsureOutputVisionAvoidForStartAsync(CancellationToken ct)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
            if (stage == null || stage.OutputCameraX == null)
                return Fail("PICKUP-Z-CAL-OUTPUT-VISION-MISSING", "OutputStageUnit",
                    "PickUpZ Calibration start OutputVisionX Avoid move requires axis.");

            if (stage.IsVisionXInAvoidPosition())
                return 0;

            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveMoveTimeout(),
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKUP-Z-CAL-OUTPUT-VISION-AVOID", "OutputStageUnit",
                    "PickUpZ Calibration OutputVisionX Avoid move failed. result=" + result);

            return 0;
        }

        private async Task<int> MoveInputStageToCalibrationProcessAsync(string description, CancellationToken ct)
        {
            InputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null || stage.Recipe == null || _calibrationTarget == null)
                return Fail("PICKUP-Z-CAL-STAGE-MISSING", "InputStageUnit",
                    "PickUpZ Calibration InputStage process move requires axis/recipe/target.");

            stage.Recipe.EnsurePositionObjects();

            int result = await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.NeedleZ,
                stage.Recipe.NeedleZ.AvoidPosition,
                description + " NeedleZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.EjectPinZ,
                stage.Recipe.EjectPinZ.AvoidPosition,
                description + " EjectPinZ Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.WaferExpandingZ,
                stage.Recipe.WaferZ.ProcessPosition,
                description + " ExpanderZ Process",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            result = await stage.MoveNeedleWorkPointSafelyAsync(
                _calibrationTarget.NeedleX,
                _calibrationTarget.StageY,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                ResolveMoveTimeout(),
                "PickUpZCalibration.MoveNeedleWorkPoint").ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKUP-Z-CAL-NEEDLE-XY", stage.Name,
                    "PickUpZ Calibration NeedleX/StageY safe move failed. result=" + result +
                    ", targetNeedleX=" + _calibrationTarget.NeedleX.ToString("F6") +
                    ", targetStageY=" + _calibrationTarget.StageY.ToString("F6") +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.NeedleX, _calibrationTarget.NeedleX) +
                    ", " + BuildInputStageAxisState(stage, WaferStageAxis.WaferY, _calibrationTarget.StageY));

            return await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.NeedleZ,
                _calibrationTarget.NeedleZ,
                description + " NeedleZ Pick Ready",
                ct).ConfigureAwait(false);
        }

        private async Task<int> MoveCurrentPickerToCalibrationTargetAsync(string description, CancellationToken ct)
        {
            if (_calibrationTarget == null)
                return Fail("PICKUP-Z-CAL-PICKER-TARGET", Name, "PickUpZ Calibration picker target is null.");

            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _calibrationTarget.PickerX;
            targets[GetPickerTAxis(_pickerIndex)] = _calibrationTarget.PickerT;
            targets[PickerAxis.PickerY] = _calibrationTarget.PickerY;

            return await MovePickerXTThenYAndVerifyAsync(
                targets,
                description + " Picker Input Process",
                ct,
                SearchTargetName,
                true).ConfigureAwait(false);
        }

        private CalibrationMotionSettings ResolveCalibrationMotion()
        {
            if (CalibrationMotion != null)
                return CalibrationMotion;

            if (_settings != null)
            {
                _settings.EnsureDefaults();
                SetCalibrationMotion(_settings.Motion);
                return CalibrationMotion;
            }

            var motion = new CalibrationMotionSettings();
            motion.EnsureDefaults();
            SetCalibrationMotion(motion);
            return CalibrationMotion;
        }

        private async Task<int> MoveInputStageAxisWithCalibrationMotionAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            string description,
            CancellationToken ct)
        {
            if (stage == null)
                return Fail("PICKUP-Z-CAL-STAGE-NULL", "InputStageUnit", description + " stage is null.");

            ct.ThrowIfCancellationRequested();
            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            int result = await stage.MoveInputStageAxisCommandWithMotion(
                axis,
                target,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration).ConfigureAwait(false);
            if (result != 0)
                return Fail("PICKUP-Z-CAL-STAGE-MOVE", stage.Name,
                    description + " command failed. result=" + result +
                    ", " + BuildInputStageAxisState(stage, axis, target) +
                    PickerInputStageMoveHelper.BuildLastStageMoveFailure(stage));

            AxisMoveWaitResult waitResult = await stage.WaitInputStageAxisInPositionResult(
                axis,
                target,
                ResolveMoveTimeout(),
                ct).ConfigureAwait(false);
            if (waitResult == null || !waitResult.Success)
                return Fail("PICKUP-Z-CAL-STAGE-WAIT", stage.Name,
                    description + " final position check failed. " +
                    AxisMoveWaiter.FormatResult(waitResult, axis.ToString()) +
                    ", " + BuildInputStageAxisState(stage, axis, target));

            return 0;
        }

        private static BaseAxis ResolveInputStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                case WaferStageAxis.WaferY: return stage.StageY;
                case WaferStageAxis.WaferT: return stage.StageT;
                case WaferStageAxis.WaferExpandingZ: return stage.ExpanderZ;
                case WaferStageAxis.VisionX: return stage.CameraX;
                case WaferStageAxis.NeedleX: return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ: return stage.NeedleZ;
                case WaferStageAxis.EjectPinZ: return stage.EjectPinZ;
                default: return null;
            }
        }

        private static string BuildInputStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            BaseAxis item = ResolveInputStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target.ToString("F6") + ", state=null";

            item.UpdateStatus();
            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition.ToString("F6") +
                   ", target=" + target.ToString("F6");
        }

        private async Task<int> SearchFlowPositionWithResetAsync(CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
                return Fail("PICKUP-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);

            Stopwatch totalWatch = Stopwatch.StartNew();
            try
            {
                if (_settings.FailIfFlowAlreadyOn)
                {
                    int offResult = await WaitPickerFlowStateForCalibrationAsync(
                        false,
                        "PickUpZ Calibration before coarse search",
                        ct).ConfigureAwait(false);
                    if (offResult != 0)
                        return offResult;
                }

                PickUpZFlowSearchResult coarse = await SearchFlowPassAsync(
                    "Coarse",
                    _scanStartPosition,
                    _searchLimitPosition,
                    _settings.Motion.MoveVelocity,
                    ct).ConfigureAwait(false);
                if (!coarse.Success)
                    return coarse.ResultCode;

                var fineDetections = new List<double>();
                double lastDetected = coarse.DetectedPosition;
                int repeatCount = Math.Max(1, _settings.RepeatCount);

                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration fine correction starts in current aligned pose. " +
                    "Only selected PickerZ will move for BackOff/Fine Search. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", pickerZAxis=" + _pickerZAxis +
                    ", coarseFlow=" + coarse.DetectedPosition.ToString("F6") +
                    ", repeatCount=" + repeatCount +
                    ", fineVelocity=" + _settings.FineSearchVelocityMmPerSec.ToString("F6") + " - Check");

                for (int index = 1; index <= repeatCount; index++)
                {
                    double backOffTarget = CalculateBackOffTarget(lastDetected);
                    int resetResult = await RunBackOffBlowResetAsync(
                        lastDetected,
                        backOffTarget,
                        index,
                        ct).ConfigureAwait(false);
                    if (resetResult != 0)
                        return resetResult;

                    PickUpZFlowSearchResult fine = await SearchFlowPassAsync(
                        "Fine#" + index,
                        backOffTarget,
                        _searchLimitPosition,
                        _settings.FineSearchVelocityMmPerSec,
                        ct).ConfigureAwait(false);
                    if (!fine.Success)
                        return fine.ResultCode;

                    fineDetections.Add(fine.DetectedPosition);
                    lastDetected = fine.DetectedPosition;

                    string toleranceReason;
                    if (!AreFineDetectionsWithinTolerance(fineDetections, out toleranceReason))
                    {
                        return Fail("PICKUP-Z-CAL-REPEAT-TOLERANCE", Name,
                            "PickUpZ Calibration fine search repeat tolerance failed. " + toleranceReason +
                            ", tolerance=" + _settings.RepeatToleranceMm.ToString("F6") +
                            ", detections=" + FormatDetections(fineDetections));
                    }
                }

                _detectedFlowPosition = AverageDetections(fineDetections);
                _savedPickPosition = CalculateSavedPickPosition(_detectedFlowPosition);
                _detectElapsedMs = (int)Math.Min(int.MaxValue, totalWatch.ElapsedMilliseconds);
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPickPosition = _savedPickPosition;
                Result.DetectElapsedMs = _detectElapsedMs;

                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration search complete. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", coarseFlow=" + coarse.DetectedPosition.ToString("F6") +
                    ", fineFlows=" + FormatDetections(fineDetections) +
                    ", finalFlow=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPickZ=" + _savedPickPosition.ToString("F6") +
                    ", formula=flow+die+film=" + _detectedFlowPosition.ToString("F6") +
                    "+" + _settings.DieThicknessMm.ToString("F6") +
                    "+" + _settings.FilmThicknessMm.ToString("F6") +
                    ", elapsedMs=" + _detectElapsedMs + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                StopPickerZAxis();
                throw;
            }
            catch (SequenceStopException)
            {
                StopPickerZAxis();
                throw;
            }
            catch (Exception ex)
            {
                StopPickerZAxis();
                return Fail("PICKUP-Z-CAL-SEARCH-RESET-EX", Name,
                    "PickUpZ Calibration search with reset exception. error=" + ex.Message);
            }
            finally
            {
                totalWatch.Stop();
            }
        }

        private async Task<PickUpZFlowSearchResult> SearchFlowPassAsync(
            string passName,
            double searchStart,
            double searchLimit,
            double velocity,
            CancellationToken ct)
        {
            var result = new PickUpZFlowSearchResult();
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
            {
                result.ResultCode = Fail("PICKUP-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);
                return result;
            }

            if (IsPickerSimulationOrDryRun())
            {
                double simulatedFlow = ResolveSimulatedFlowPosition();
                if (!IsBetween(simulatedFlow, searchStart, searchLimit, 0.000001))
                {
                    result.ResultCode = Fail("PICKUP-Z-CAL-SIM-FLOW-RANGE", Name,
                        "PickUpZ Calibration simulation Flow position is outside search range. pass=" + passName +
                        ", simulatedFlow=" + simulatedFlow.ToString("F6") +
                        ", start=" + searchStart.ToString("F6") +
                        ", limit=" + searchLimit.ToString("F6"));
                    return result;
                }

                Stopwatch simWatch = Stopwatch.StartNew();
                int moveResult = await MovePickerAxisAndVerifyAsync(
                    _pickerZAxis,
                    simulatedFlow,
                    "PickUpZ Calibration " + passName + " simulated Flow Z-only",
                    ct,
                    SearchTargetName).ConfigureAwait(false);
                simWatch.Stop();
                if (moveResult != 0)
                {
                    result.ResultCode = moveResult;
                    return result;
                }

                axis.UpdateStatus();
                result.DetectedPosition = axis.ActualPosition;
                result.ElapsedMs = (int)Math.Min(int.MaxValue, simWatch.ElapsedMilliseconds);
                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration " + passName + " simulated Flow detected. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", start=" + searchStart.ToString("F6") +
                    ", limit=" + searchLimit.ToString("F6") +
                    ", flow=" + result.DetectedPosition.ToString("F6") + " - Ok");
                return result;
            }

            string interlockReason;
            if (!MotionGuardRuntime.VerifyAxisTeachingMove(axis, searchLimit, SearchTargetName, out interlockReason))
            {
                result.ResultCode = Fail("PICKUP-Z-CAL-SEARCH-INTERLOCK", Name,
                    "PickUpZ Calibration search move blocked. pass=" + passName +
                    ", side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", target=" + searchLimit.ToString("F6") +
                    ". " + interlockReason);
                return result;
            }

            Stopwatch watch = Stopwatch.StartNew();
            DateTime? stableSinceUtc = null;
            bool detected = false;

            Task<int> moveTask = MovePickerAxisCommandWithMotionAsync(
                _pickerZAxis,
                searchLimit,
                velocity,
                _settings.Motion.MoveAcceleration,
                _settings.Motion.MoveDeceleration,
                SearchTargetName);

            try
            {
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".SearchFlow." + passName);

                    if (ReadPickerFlowState(_pickerNo))
                    {
                        if (!stableSinceUtc.HasValue)
                            stableSinceUtc = DateTime.UtcNow;

                        if ((DateTime.UtcNow - stableSinceUtc.Value).TotalMilliseconds >= _settings.FlowStableMs)
                        {
                            StopPickerZAxis();
                            detected = true;
                            break;
                        }
                    }
                    else
                    {
                        stableSinceUtc = null;
                    }

                    if (watch.ElapsedMilliseconds > _settings.Motion.MoveTimeoutMs)
                    {
                        StopPickerZAxis();
                        break;
                    }

                    await Task.Delay(_settings.FlowPollIntervalMs, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                axis.UpdateStatus();

                if (!detected)
                {
                    if (ReadPickerFlowState(_pickerNo))
                    {
                        detected = true;
                    }
                    else if (moveResult != 0)
                    {
                        result.ResultCode = Fail("PICKUP-Z-CAL-Z-MOVE", Name,
                            "PickUpZ Calibration search move failed. pass=" + passName +
                            ", result=" + moveResult +
                            ", " + BuildPickerAxisState(_pickerZAxis, searchLimit));
                        return result;
                    }
                }

                if (!detected)
                {
                    result.ResultCode = Fail("PICKUP-Z-CAL-FLOW-NOT-DETECTED", Name,
                        "PickUpZ Calibration Flow was not detected. pass=" + passName +
                        ", side=" + Side +
                        ", pickerNo=" + _pickerNo +
                        ", start=" + searchStart.ToString("F6") +
                        ", limit=" + searchLimit.ToString("F6") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + _settings.Motion.MoveTimeoutMs);
                    return result;
                }

                result.DetectedPosition = axis.ActualPosition;
                result.ElapsedMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration " + passName + " Flow detected. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", start=" + searchStart.ToString("F6") +
                    ", limit=" + searchLimit.ToString("F6") +
                    ", velocity=" + velocity.ToString("F6") +
                    ", flow=" + result.DetectedPosition.ToString("F6") +
                    ", elapsedMs=" + result.ElapsedMs + " - Ok");
                return result;
            }
            catch
            {
                StopPickerZAxis();
                throw;
            }
            finally
            {
                watch.Stop();
            }
        }

        private async Task<int> RunBackOffBlowResetAsync(
            double detectedPosition,
            double backOffTarget,
            int repeatIndex,
            CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                _pickerZAxis,
                backOffTarget,
                "PickUpZ Calibration Z-only BackOff before fine search #" + repeatIndex,
                ct,
                SearchTargetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            SetPickerVacuum(_pickerNo, false);
            WriteLog("PickUpZCalibration",
                "PickUpZ Calibration BackOff complete. side=" + Side +
                ", pickerNo=" + _pickerNo +
                ", repeat=" + repeatIndex +
                ", detected=" + detectedPosition.ToString("F6") +
                ", backOffTarget=" + backOffTarget.ToString("F6") +
                ", vacuum=OFF");

            result = await PulsePickerBlowForCalibrationAsync(repeatIndex, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (_settings.BlowSettleTimeMs > 0)
                await Task.Delay(_settings.BlowSettleTimeMs, ct).ConfigureAwait(false);

            SetPickerVacuum(_pickerNo, true);
            if (_settings.VacuumReOnDelayMs > 0)
                await Task.Delay(_settings.VacuumReOnDelayMs, ct).ConfigureAwait(false);

            return await WaitPickerFlowStateForCalibrationAsync(
                false,
                "PickUpZ Calibration Flow OFF after BackOff/Blow repeat #" + repeatIndex,
                ct).ConfigureAwait(false);
        }

        private async Task<int> PulsePickerBlowForCalibrationAsync(int repeatIndex, CancellationToken ct)
        {
            try
            {
                SetPickerBlow(_pickerNo, true);
                if (_settings.BlowPulseTimeMs > 0)
                    await Task.Delay(_settings.BlowPulseTimeMs, ct).ConfigureAwait(false);

                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration Blow pulse complete. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", repeat=" + repeatIndex +
                    ", pulseMs=" + _settings.BlowPulseTimeMs + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKUP-Z-CAL-BLOW-EX", Name,
                    "PickUpZ Calibration Blow pulse exception. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", repeat=" + repeatIndex +
                    ", error=" + ex.Message);
            }
            finally
            {
                try { SetPickerBlow(_pickerNo, false); }
                catch (Exception ex)
                {
                    WriteLog("PickUpZCalibration",
                        "PickUpZ Calibration Blow OFF cleanup failed. side=" + Side +
                        ", pickerNo=" + _pickerNo +
                        ", error=" + ex.Message + " - Failed");
                }
            }
        }

        private async Task<int> WaitPickerFlowStateForCalibrationAsync(
            bool expected,
            string description,
            CancellationToken ct)
        {
            if (IsPickerSimulationOrDryRun())
            {
                WriteLog("PickUpZCalibration",
                    "PickUpZ Calibration Flow " + (expected ? "ON" : "OFF") +
                    " check bypassed in Simulation/DryRun. side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", description=" + description + " - Bypass");
                return 0;
            }

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(_settings.FlowOffConfirmTimeoutMs);
            bool actual = ReadPickerFlowState(_pickerNo);
            while (DateTime.UtcNow <= deadline)
            {
                ct.ThrowIfCancellationRequested();
                actual = ReadPickerFlowState(_pickerNo);
                if (actual == expected)
                    return 0;

                await Task.Delay(Math.Max(1, _settings.FlowPollIntervalMs), ct).ConfigureAwait(false);
            }

            actual = ReadPickerFlowState(_pickerNo);
            return Fail("PICKUP-Z-CAL-FLOW-STATE", Name,
                "PickUpZ Calibration Flow state check failed. expected=" + (expected ? "ON" : "OFF") +
                ", actual=" + (actual ? "ON" : "OFF") +
                ", timeoutMs=" + _settings.FlowOffConfirmTimeoutMs +
                ", side=" + Side +
                ", pickerNo=" + _pickerNo +
                ", description=" + description);
        }

        private double CalculateBackOffTarget(double detectedPosition)
        {
            double backOffTarget = detectedPosition - (_searchDirection * Math.Abs(_settings.BackOffDistanceMm));
            double avoid = GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition");
            if (_searchDirection > 0.0)
                return Math.Max(backOffTarget, avoid);
            if (_searchDirection < 0.0)
                return Math.Min(backOffTarget, avoid);

            return backOffTarget;
        }

        private double ResolveSimulatedFlowPosition()
        {
            double distance = Math.Abs(_settings.SearchMaxDistanceMm);
            double offset = Math.Max(0.001, distance * 0.5);
            return _scanStartPosition + (_searchDirection * offset);
        }

        private static bool IsBetween(double value, double start, double end, double tolerance)
        {
            double min = Math.Min(start, end) - tolerance;
            double max = Math.Max(start, end) + tolerance;
            return value >= min && value <= max;
        }

        private bool AreFineDetectionsWithinTolerance(List<double> detections, out string reason)
        {
            reason = string.Empty;
            if (detections == null || detections.Count <= 1)
                return true;

            double min = detections[0];
            double max = detections[0];
            for (int i = 1; i < detections.Count; i++)
            {
                if (detections[i] < min) min = detections[i];
                if (detections[i] > max) max = detections[i];
            }

            double range = max - min;
            if (range <= _settings.RepeatToleranceMm)
                return true;

            reason = "min=" + min.ToString("F6") +
                     ", max=" + max.ToString("F6") +
                     ", range=" + range.ToString("F6");
            return false;
        }

        private static double AverageDetections(List<double> detections)
        {
            if (detections == null || detections.Count == 0)
                return 0.0;

            double sum = 0.0;
            for (int i = 0; i < detections.Count; i++)
                sum += detections[i];

            return sum / detections.Count;
        }

        private static string FormatDetections(List<double> detections)
        {
            if (detections == null || detections.Count == 0)
                return "-";

            var parts = new string[detections.Count];
            for (int i = 0; i < detections.Count; i++)
                parts[i] = detections[i].ToString("F6");

            return string.Join(",", parts);
        }

        private async Task<int> SearchFlowPositionAsync(CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
                return Fail("PICKUP-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);

            if (IsPickerSimulationOrDryRun())
            {
                _detectedFlowPosition = _oldPickPosition;
                _savedPickPosition = CalculateSavedPickPosition(_detectedFlowPosition);
                _detectElapsedMs = 0;
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPickPosition = _savedPickPosition;
                Result.DetectElapsedMs = _detectElapsedMs;
                return 0;
            }

            if (_settings.FailIfFlowAlreadyOn && ReadPickerFlowState(_pickerNo))
                return Fail("PICKUP-Z-CAL-FLOW-ALREADY-ON", Name,
                    "PickUpZ Calibration 시작 전 Flow가 이미 ON입니다. Vacuum sensor 상태 또는 Picker 위치를 확인하세요. " +
                    "side=" + Side + ", pickerNo=" + _pickerNo);

            Stopwatch watch = Stopwatch.StartNew();
            DateTime? stableSinceUtc = null;
            bool detected = false;

            string interlockReason;
            if (!MotionGuardRuntime.VerifyAxisTeachingMove(axis, _searchLimitPosition, SearchTargetName, out interlockReason))
            {
                return Fail("PICKUP-Z-CAL-SEARCH-INTERLOCK", Name,
                    "PickUpZ Calibration 검색 이동 인터락 차단. " +
                    "side=" + Side +
                    ", pickerNo=" + _pickerNo +
                    ", target=" + _searchLimitPosition.ToString("F6") +
                    ". " + interlockReason);
            }

            Task<int> moveTask = MovePickerAxisCommandWithMotionAsync(
                _pickerZAxis,
                _searchLimitPosition,
                _settings.Motion.MoveVelocity,
                _settings.Motion.MoveAcceleration,
                _settings.Motion.MoveDeceleration,
                SearchTargetName);

            try
            {
                while (!moveTask.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Context != null)
                        Context.StopIfCycleStopRequested(Name + ".SearchFlow");

                    if (ReadPickerFlowState(_pickerNo))
                    {
                        if (!stableSinceUtc.HasValue)
                            stableSinceUtc = DateTime.UtcNow;

                        if ((DateTime.UtcNow - stableSinceUtc.Value).TotalMilliseconds >= _settings.FlowStableMs)
                        {
                            StopPickerZAxis();
                            detected = true;
                            break;
                        }
                    }
                    else
                    {
                        stableSinceUtc = null;
                    }

                    if (watch.ElapsedMilliseconds > _settings.Motion.MoveTimeoutMs)
                    {
                        StopPickerZAxis();
                        break;
                    }

                    await Task.Delay(_settings.FlowPollIntervalMs, ct).ConfigureAwait(false);
                }

                int moveResult = await moveTask.ConfigureAwait(false);
                axis.UpdateStatus();

                if (!detected)
                {
                    if (ReadPickerFlowState(_pickerNo))
                    {
                        detected = true;
                    }
                    else if (moveResult != 0)
                    {
                        return Fail("PICKUP-Z-CAL-Z-MOVE", Name,
                            "PickUpZ Calibration 검색 이동 실패. result=" + moveResult +
                            ", " + BuildPickerAxisState(_pickerZAxis, _searchLimitPosition));
                    }
                }

                if (!detected)
                {
                    return Fail("PICKUP-Z-CAL-FLOW-NOT-DETECTED", Name,
                        "PickUpZ Calibration Flow 감지 실패. 최대 하강 위치까지 Flow가 ON되지 않았습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _pickerNo +
                        ", start=" + _scanStartPosition.ToString("F6") +
                        ", limit=" + _searchLimitPosition.ToString("F6") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + _settings.Motion.MoveTimeoutMs);
                }

                _detectedFlowPosition = axis.ActualPosition;
                _savedPickPosition = CalculateSavedPickPosition(_detectedFlowPosition);
                _detectElapsedMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPickPosition = _savedPickPosition;
                Result.DetectElapsedMs = _detectElapsedMs;
                return 0;
            }
            catch (OperationCanceledException)
            {
                StopPickerZAxis();
                throw;
            }
            catch (SequenceStopException)
            {
                StopPickerZAxis();
                throw;
            }
            catch (Exception ex)
            {
                StopPickerZAxis();
                return Fail("PICKUP-Z-CAL-SEARCH-EX", Name,
                    "PickUpZ Calibration Flow 검색 예외 발생. error=" + ex.Message);
            }
            finally
            {
                watch.Stop();
            }
        }

        private int SaveCalibrationResult()
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                    FrontPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PickPosition", _savedPickPosition);
                else
                    RearPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PickPosition", _savedPickPosition);

                double recipePickZ = GetPickerTeachingPosition(_pickerZAxis, "PickPosition");
                if (Math.Abs(recipePickZ - _savedPickPosition) > 0.000001)
                {
                    return Fail("PICKUP-Z-CAL-RECIPE-Z-VERIFY", Name,
                        "PickUpZ Calibration saved PickPosition readback mismatch. " +
                        "side=" + _calibrationSide +
                        ", pickerNo=" + _pickerNo +
                        ", axis=" + _pickerZAxis +
                        ", savedPickZ=" + _savedPickPosition.ToString("F6") +
                        ", recipePickZ=" + recipePickZ.ToString("F6"));
                }

                CalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData;
                data.EnsureObjects();
                PickUpZCalibrationRecord record = data.PickUpZ.GetRecord(_calibrationSide, _pickerNo);
                record.OldPickPosition = _oldPickPosition;
                record.DetectedFlowPosition = _detectedFlowPosition;
                record.SavedPickPosition = _savedPickPosition;
                record.ContactOffsetMm = _settings.DieThicknessMm;
                record.StartZMm = _settings.StartZMm;
                record.DieThicknessMm = _settings.DieThicknessMm;
                record.FilmThicknessMm = _settings.FilmThicknessMm;
                record.PositionOffsetXmm = _settings.PositionOffsetXmm;
                record.PositionOffsetYmm = _settings.PositionOffsetYmm;
                record.DetectElapsedMs = _detectElapsedMs;
                record.Valid = true;
                record.UpdatedAt = DateTime.Now;
                record.UpdatedBy = ResolveUpdatedBy();
                record.Message = "OK";
                data.Touch("PickUpZCalibration");

                string saveReason;
                if (!CalibrationDataStore.Save(data, out saveReason))
                    return Fail("PICKUP-Z-CAL-DATA-SAVE", Name,
                        "PickUpZ CalibrationData 저장 실패. reason=" + saveReason);

                EventLogger.Write(EventKind.Event, "CAL", "PICKUP-Z-CAL-SAVE",
                    "PickUpZ Calibration 저장. side=" + _calibrationSide +
                    ", pickerNo=" + _pickerNo +
                    ", axis=" + _pickerZAxis +
                    ", oldPickZ=" + _oldPickPosition.ToString("F6") +
                    ", startZ=" + _settings.StartZMm.ToString("F6") +
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPickZ=" + _savedPickPosition.ToString("F6") +
                    ", recipePickZ=" + recipePickZ.ToString("F6") +
                    ", formula=savedPickZ=flowZ+dieThickness+filmThickness=" +
                    _detectedFlowPosition.ToString("F6") + "+" +
                    _settings.DieThicknessMm.ToString("F6") + "+" +
                    _settings.FilmThicknessMm.ToString("F6") + "=" +
                    _savedPickPosition.ToString("F6") +
                    ", offsetX=" + _settings.PositionOffsetXmm.ToString("F6") +
                    ", offsetY=" + _settings.PositionOffsetYmm.ToString("F6"));
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKUP-Z-CAL-SAVE-EX", Name,
                    "PickUpZ Calibration 결과 저장 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private double CalculateSavedPickPosition(double detectedFlowPosition)
        {
            double dieThickness = _settings != null ? _settings.DieThicknessMm : 0.0;
            double filmThickness = _settings != null ? _settings.FilmThicknessMm : 0.0;
            return detectedFlowPosition + dieThickness + filmThickness;
        }

        private void StopPickerZAxis()
        {
            try
            {
                BaseAxis axis = GetPickerAxis(_pickerZAxis);
                if (axis != null)
                    axis.StopJog();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ReleaseArea()
        {
            try
            {
                if (_inputStageLease != null)
                {
                    _inputStageLease.Dispose();
                    _inputStageLease = null;
                }

                if (_outputStageLease != null)
                {
                    _outputStageLease.Dispose();
                    _outputStageLease = null;
                }

                if (_pickerLease != null)
                {
                    _pickerLease.Dispose();
                    _pickerLease = null;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private string ResolveUpdatedBy()
        {
            try
            {
                return Environment.UserName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private static int NormalizePickerNo(int pickerNo)
        {
            if (pickerNo < 1)
                return 1;
            if (pickerNo > 4)
                return 4;
            return pickerNo;
        }
    }

}
