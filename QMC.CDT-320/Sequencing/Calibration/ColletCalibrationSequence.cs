using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.VisionComm;
using QMC.Common.Motion;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal sealed class ColletCalibrationSequence : PickerSequenceBase<ColletCalibrationStep>
    {
        private const string BottomFinderTargetName = "ColletCalibration;PickerZone=Bottom";
        private const string FineAlignTargetName = "ColletCalibrationFineAlign;PickerZone=Bottom";
        private const double SimColletMaxPixelOffset = 5.0;
        private const double SimColletMaxAngleDeg = 0.03;
        private const double SimColletNoisePixel = 0.05;
        private const double SimColletNoiseAngleDeg = 0.0002;
        private static readonly object SimColletRandomLock = new object();
        private static readonly Random SimColletRandom = new Random();

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly int _colletNo;
        private readonly int _colletIndex;
        private SequenceResourceLease _inspectionAreaLease;
        private ColletCalibrationSettings _settings;
        private MatchResultDto _firstMatch;
        private MatchResultDto _finalMatch;
        private double _targetPickerX;
        private double _targetPickerY;
        private double _targetPickerZ;
        private double _nominalPickerX;
        private double _nominalPickerY;
        private double _basePickerT;
        private double _measuredTPosition;
        private double? _simPickerX;
        private double? _simPickerY;
        private double? _simPickerT;
        private double? _simColletCenterPickerX;
        private double? _simColletCenterPickerY;
        private double? _simColletZeroPickerT;
        private ColletCalibrationRecord _calculatedRecord;

        public ColletCalibrationSequence(MachineSequenceContext context, VisionFocusPickerSide side, int colletNo)
            : base(
                  context,
                  side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                  PickerSequenceKind.Inspect,
                  side == VisionFocusPickerSide.Front ? "FrontColletCalibrationSequence" : "RearColletCalibrationSequence")
        {
            _calibrationSide = side;
            _colletNo = colletNo < 1 ? 1 : colletNo > 4 ? 4 : colletNo;
            _colletIndex = _colletNo - 1;
            CurrentStep = ColletCalibrationStep.CheckUnit;
        }

        public ColletCalibrationRecord ResultRecord { get; private set; }

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
            return motion;
        }

        private sealed class MatchStepResult
        {
            public int Result;
            public MatchResultDto Match;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                while (CurrentStep != ColletCalibrationStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();
                    int result = await ExecuteCurrentStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        CurrentStep = ColletCalibrationStep.Error;
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
                return Fail("COLLET-CAL-EX", Name,
                    "Collet Calibration 실행 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", step=" + CurrentStep +
                    ", error=" + ex.Message);
            }
            finally
            {
                ReleaseCalibrationArea();
            }
        }

        private Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            switch (CurrentStep)
            {
                case ColletCalibrationStep.CheckUnit:
                    return Task.FromResult(CheckUnit());
                case ColletCalibrationStep.MoveColletToBottomView:
                    return MoveColletToBottomViewAsync(ct);
                case ColletCalibrationStep.FindCollet:
                    return FindColletAsync(false, ct);
                case ColletCalibrationStep.AdjustThetaToZero:
                    return AdjustThetaToZeroAsync(ct);
                case ColletCalibrationStep.AdjustXyToCenter:
                    return AdjustXyToCenterAsync(ct);
                case ColletCalibrationStep.FindColletAgain:
                    return FindColletAsync(true, ct);
                case ColletCalibrationStep.CalculateOffset:
                    return Task.FromResult(CalculateOffset());
                case ColletCalibrationStep.SaveColletCalibration:
                    return Task.FromResult(SaveColletCalibration());
                default:
                    CurrentStep = ColletCalibrationStep.Complete;
                    return Task.FromResult(0);
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (Context == null || Context.Machine == null)
                    return Fail("COLLET-CAL-NO-MACHINE", Name, "장비 객체가 없어 Collet Calibration을 실행할 수 없습니다.");
                if (Context.Machine.VisionUnit == null || Context.Machine.VisionUnit.Config == null)
                    return Fail("COLLET-CAL-NO-VISION", Name, "VisionUnit이 없어 Collet Calibration을 실행할 수 없습니다.");
                if (_calibrationSide == VisionFocusPickerSide.Front && FrontPicker == null)
                    return Fail("COLLET-CAL-NO-FRONT", Name, "Front Picker Unit이 없어 Collet Calibration을 실행할 수 없습니다.");
                if (_calibrationSide == VisionFocusPickerSide.Rear && RearPicker == null)
                    return Fail("COLLET-CAL-NO-REAR", Name, "Rear Picker Unit이 없어 Collet Calibration을 실행할 수 없습니다.");

                string axisReason = BuildRequiredPickerAxesReason();
                if (!string.IsNullOrWhiteSpace(axisReason))
                    return Fail("COLLET-CAL-AXIS-NOT-READY", Name,
                        "Collet Calibration 축 상태가 준비되지 않았습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", reason=" + axisReason);

                Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                Context.Machine.VisionUnit.Config.CalibrationData.Collet.EnsureObjects();
                _settings = Context.Machine.VisionUnit.Config.CalibrationData.Collet.Settings;
                _settings.EnsureDefaults();
                SetCalibrationMotion(_settings.Motion);
                string referenceReason;
                if (!IsReferenceCollet() && !IsReferenceColletCalibrationReady(out referenceReason))
                    return Fail("COLLET-CAL-REFERENCE-NOT-READY", Name,
                        "Collet Calibration은 4번 Collet을 Bottom 기준 티칭으로 먼저 잡아야 합니다. side=" +
                        _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", referenceColletNo=4" +
                        ", reason=" + referenceReason);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSettings",
                    "Collet Calibration 설정 확인. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", finder=" + _settings.BottomFinderName +
                    ", thetaTolDeg=" + _settings.ThetaToleranceDeg.ToString("F6") +
                    ", thetaRetry=" + _settings.MaxThetaIterations +
                    ", thetaGain=" + _settings.ThetaMoveGain.ToString("F6") +
                    ", xyTolMm=" + _settings.XyToleranceMm.ToString("F6") +
                    ", xyRetry=" + _settings.MaxXyIterations +
                    ", xyGainX=" + _settings.XyMoveGainX.ToString("F6") +
                    ", xyGainY=" + _settings.XyMoveGainY.ToString("F6") +
                    ", xyTolMode=" + (_settings.UseDiagonalXyTolerance ? "Diagonal" : "Axis") +
                    ", fineAlignMaxMm=" + _settings.FineAlignMaxXyMoveMm.ToString("F6") +
                    ", scoreMin=" + _settings.ScoreThreshold.ToString("F6") +
                    ", visionTimeoutMs=" + _settings.VisionTimeoutMs +
                    ", autoFocus=" + _settings.RunAutoFocusAfterTheta +
                    ", moveVelocity=" + _settings.Motion.MoveVelocity.ToString("F6") +
                    ", moveAcceleration=" + _settings.Motion.MoveAcceleration.ToString("F6") +
                    ", moveDeceleration=" + _settings.Motion.MoveDeceleration.ToString("F6") +
                    ", moveTimeoutMs=" + _settings.Motion.MoveTimeoutMs);

                CurrentStep = ColletCalibrationStep.MoveColletToBottomView;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-CHECK-EX", Name, "Collet Calibration 조건 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveColletToBottomViewAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int result = await AcquireCalibrationAreaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalMove",
                    "Collet Calibration Bottom 진입 전 Z 안전 위치 확인. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", z0Actual=" + (GetPickerAxis(PickerAxis.PickerZ0) != null ? GetPickerAxis(PickerAxis.PickerZ0).ActualPosition.ToString("F6") : "null") +
                    ", z1Actual=" + (GetPickerAxis(PickerAxis.PickerZ1) != null ? GetPickerAxis(PickerAxis.PickerZ1).ActualPosition.ToString("F6") : "null") +
                    ", z2Actual=" + (GetPickerAxis(PickerAxis.PickerZ2) != null ? GetPickerAxis(PickerAxis.PickerZ2).ActualPosition.ToString("F6") : "null") +
                    ", z3Actual=" + (GetPickerAxis(PickerAxis.PickerZ3) != null ? GetPickerAxis(PickerAxis.PickerZ3).ActualPosition.ToString("F6") : "null") +
                    ", z0Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition").ToString("F6") +
                    ", z1Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition").ToString("F6") +
                    ", z2Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition").ToString("F6") +
                    ", z3Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition").ToString("F6"));

                result = await MoveCurrentPickerZAndYToAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToOutsideForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "ColletCalibration");

                PickerCalibratedZoneTarget nominalTarget =
                    CalibrationCoordinateService.ResolvePickerZoneTarget(
                        Context.Machine,
                        _calibrationSide,
                        "DieBottomPosition",
                        _colletIndex,
                        null,
                        false,
                        false);
                PickerCalibratedZoneTarget startTarget =
                    CalibrationCoordinateService.ResolvePickerZoneTarget(
                        Context.Machine,
                        _calibrationSide,
                        "DieBottomPosition",
                        _colletIndex,
                        null,
                        false,
                        !IsReferenceCollet());
                double baseBottomX = nominalTarget.TeachingX;
                double baseBottomY = nominalTarget.TeachingY;
                double pitchOffsetX = nominalTarget.PitchOffsetX;
                _nominalPickerX = nominalTarget.X;
                _nominalPickerY = nominalTarget.Y;
                double existingColletOffsetX = startTarget.ColletOffsetX;
                double existingColletOffsetY = startTarget.ColletOffsetY;
                ColletCalibrationRecord existingRecord = ResolveExistingColletCalibrationRecord();

                _targetPickerX = startTarget.X;
                _targetPickerY = startTarget.Y;
                double bottomTeachingZ = GetPickerTeachingPosition(GetPickerZAxis(_colletIndex), "BottomPosition");
                string focusStartReason;
                if (!TryResolveBottomColletFocusStartPosition(out _targetPickerZ, out focusStartReason))
                    return Fail("COLLET-CAL-NO-FOCUS-POS", Name,
                        "Collet Calibration 시작 Z 위치로 사용할 Bottom Collet Focus Cal 등록값이 없습니다. side=" +
                        _calibrationSide + ", colletNo=" + _colletNo +
                        ", " + focusStartReason +
                        ", Vision Focus Cal에서 Bottom Collet Best Focus를 Apply/Save 후 다시 실행하세요.");
                _basePickerT = startTarget.T;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalMove",
                    "Collet Calibration Bottom 목표 좌표 계산. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", formulaX=bottomX+pitchOffsetX+savedColletOffsetX=" + baseBottomX.ToString("F6") + "+" + pitchOffsetX.ToString("F6") + "+" + existingColletOffsetX.ToString("F6") + "=" + _targetPickerX.ToString("F6") +
                    ", formulaY=bottomY+savedColletOffsetY=" + baseBottomY.ToString("F6") + "+" + existingColletOffsetY.ToString("F6") + "=" + _targetPickerY.ToString("F6") +
                    ", nominalPicker=(" + _nominalPickerX.ToString("F6") + "," + _nominalPickerY.ToString("F6") + ")" +
                    ", savedColletOffset=(" + existingColletOffsetX.ToString("F6") + "," + existingColletOffsetY.ToString("F6") + ")" +
                    ", savedColletValid=" + (existingRecord != null && existingRecord.Valid) +
                    ", formulaZ=bottomColletFocusDefaultZ=" + _targetPickerZ.ToString("F6") +
                    ", bottomTeachingZ=" + bottomTeachingZ.ToString("F6") +
                    ", formulaT=" + startTarget.Formula +
                    ", xAxis=" + PickerAxis.PickerX +
                    ", yAxis=" + PickerAxis.PickerY +
                    ", zAxis=" + GetPickerZAxis(_colletIndex) +
                    ", tAxis=" + GetPickerTAxis(_colletIndex));

                var xyTargets = new Dictionary<PickerAxis, double>();
                xyTargets[PickerAxis.PickerX] = _targetPickerX;
                xyTargets[PickerAxis.PickerY] = _targetPickerY;
                result = await MovePickerXTThenYAndVerifyAsync(
                    xyTargets,
                    "Collet Calibration Bottom X/Y",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;
                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerX, _targetPickerX);
                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerY, _targetPickerY);
                UpdateSimulatedPickerPosition(PickerAxis.PickerX, _targetPickerX);
                UpdateSimulatedPickerPosition(PickerAxis.PickerY, _targetPickerY);

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerTAxis(_colletIndex),
                    _basePickerT,
                    "Collet Calibration T 기준 위치",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;
                ApplyPickerAxisPositionForSimulation(GetPickerTAxis(_colletIndex), _basePickerT);
                UpdateSimulatedPickerPosition(GetPickerTAxis(_colletIndex), _basePickerT);

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerZAxis(_colletIndex),
                    _targetPickerZ,
                    "Collet Calibration Bottom Z",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;
                ApplyPickerAxisPositionForSimulation(GetPickerZAxis(_colletIndex), _targetPickerZ);

                result = await RunAutoFocusIfNeededAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = ColletCalibrationStep.FindCollet;
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
                return Fail("COLLET-CAL-MOVE-EX", Name,
                    "Collet을 Bottom Camera 위치로 이동 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputOutputVisionAvoidForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureInputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await EnsureOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-CAMERA-AVOID-EX", Name,
                    "Collet Calibration 시작 전 Input/Output VisionX Avoid 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveCurrentPickerZAndYToAvoidForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Collet Calibration 시작 전 선택 Picker Z축 Avoid",
                    ct,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerZ0, GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition"));
                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerZ1, GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition"));
                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerZ2, GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition"));
                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerZ3, GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition"));

                double avoidY = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    avoidY,
                    "Collet Calibration 시작 전 선택 PickerY Avoid",
                    ct,
                    "ColletCalibrationStart;PickerPhase=SafeY;PickerZone=Avoid",
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerY, avoidY);
                UpdateSimulatedPickerPosition(PickerAxis.PickerY, avoidY);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 선택 Picker Z/Y 안전 위치 완료. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", pickerXActual=" + (GetPickerAxis(PickerAxis.PickerX) != null ? GetPickerAxis(PickerAxis.PickerX).ActualPosition.ToString("F6") : "null") +
                    ", pickerYTarget=" + avoidY.ToString("F6"));

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
                return Fail("COLLET-CAL-PICKER-ZY-AVOID-EX", Name,
                    "Collet Calibration 시작 전 선택 Picker Z/Y Avoid 이동 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputVisionAvoidForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                    return Fail("COLLET-CAL-INPUT-CAMERA-MISSING", "InputStageUnit",
                        "Collet Calibration 시작 전 InputVisionX Avoid 이동에 필요한 축/Recipe가 없습니다.");

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                double target = stage.Recipe.VisionX.AvoidPosition;
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 InputVisionX Avoid 이동. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", actual=" + stage.CameraX.ActualPosition.ToString("F6") +
                    ", target=" + target.ToString("F6"));

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                int result = await stage.MoveInputStageAxisCommandWithMotion(
                    WaferStageAxis.VisionX,
                    target,
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-INPUT-CAMERA-MOVE", "InputStageUnit",
                        "Collet Calibration 시작 전 InputVisionX Avoid 이동 명령이 실패했습니다. result=" + result +
                        ", target=" + target.ToString("F3"));

                result = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, ResolveMoveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-INPUT-CAMERA-WAIT", "InputStageUnit",
                        "Collet Calibration 시작 전 InputVisionX Avoid 위치 대기가 실패했습니다. result=" + result +
                        ", target=" + target.ToString("F3"));

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("COLLET-CAL-INPUT-CAMERA-CHECK", "InputStageUnit",
                        "Collet Calibration 시작 전 InputVisionX Avoid 최종 위치 확인이 실패했습니다. actual=" +
                        stage.CameraX.ActualPosition.ToString("F3") +
                        ", target=" + target.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-INPUT-CAMERA-EX", "InputStageUnit",
                    "Collet Calibration 시작 전 InputVisionX Avoid 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureOutputVisionAvoidForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
                if (stage == null || stage.OutputCameraX == null)
                    return Fail("COLLET-CAL-OUTPUT-CAMERA-MISSING", "OutputStageUnit",
                        "Collet Calibration 시작 전 OutputVisionX Avoid 이동에 필요한 축이 없습니다.");

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 OutputVisionX Avoid 이동. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", actual=" + stage.OutputCameraX.ActualPosition.ToString("F6"));

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                    ResolveMoveTimeout(),
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-OUTPUT-CAMERA-MOVE", "OutputStageUnit",
                        "Collet Calibration 시작 전 OutputVisionX Avoid 이동이 실패했습니다. result=" + result);

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("COLLET-CAL-OUTPUT-CAMERA-CHECK", "OutputStageUnit",
                        "Collet Calibration 시작 전 OutputVisionX Avoid 최종 위치 확인이 실패했습니다. actual=" +
                        stage.OutputCameraX.ActualPosition.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-OUTPUT-CAMERA-EX", "OutputStageUnit",
                    "Collet Calibration 시작 전 OutputVisionX Avoid 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositePickerToOutsideForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (Side == PickerSequenceSide.Front)
                    return await MoveRearPickerToOutsideForStartAsync(ct).ConfigureAwait(false);

                return await MoveFrontPickerToOutsideForStartAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-OPPOSITE-PICKER-OUTSIDE-EX", Name,
                    "Collet Calibration 시작 전 상대 Picker Output-side Avoid 이동 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerToOutsideForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (FrontPicker == null)
                    return 0;

                if (FrontPicker.IsPickerInOutputSideAvoidPosition())
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration start FrontPicker Outside(Output-side Avoid) move for opposite picker. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo);

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                int result = await FrontPicker.MoveToOutputSideAvoidPosition(JogSpeedType.Custom, motion.MoveVelocity).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-FRONT-OUTSIDE", "PickerFrontUnit",
                        "Collet Calibration start FrontPicker Outside(Output-side Avoid) move failed. result=" + result);

                if (!FrontPicker.IsPickerInOutputSideAvoidPosition())
                    return Fail("COLLET-CAL-FRONT-OUTSIDE-CHECK", "PickerFrontUnit",
                        "Collet Calibration start FrontPicker Outside(Output-side Avoid) final check failed.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-FRONT-OUTSIDE-EX", "PickerFrontUnit",
                    "Collet Calibration start FrontPicker Outside(Output-side Avoid) exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRearPickerToOutsideForStartAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (RearPicker == null)
                    return 0;

                if (RearPicker.IsPickerInOutputSideAvoidPosition())
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration start RearPicker Outside(Output-side Avoid) move for opposite picker. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo);

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                int result = await RearPicker.MoveToOutputSideAvoidPosition(JogSpeedType.Custom, motion.MoveVelocity).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-REAR-OUTSIDE", "PickerRearUnit",
                        "Collet Calibration start RearPicker Outside(Output-side Avoid) move failed. result=" + result);

                if (!RearPicker.IsPickerInOutputSideAvoidPosition())
                    return Fail("COLLET-CAL-REAR-OUTSIDE-CHECK", "PickerRearUnit",
                        "Collet Calibration start RearPicker Outside(Output-side Avoid) final check failed.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-REAR-OUTSIDE-EX", "PickerRearUnit",
                    "Collet Calibration start RearPicker Outside(Output-side Avoid) exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> FindColletAsync(bool finalFind, CancellationToken ct)
        {
            try
            {
                MatchResultDto match = await RequestColletMatchAsync(ct).ConfigureAwait(false);
                if (match == null || !match.Success)
                {
                    return Fail("COLLET-CAL-FIND", "Vision",
                        "Bottom Camera에서 Collet을 찾지 못했습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", finalFind=" + finalFind +
                        ", raw=" + (match != null ? match.RawError : "null"));
                }

                if (finalFind)
                {
                    _finalMatch = match;
                    CurrentStep = ColletCalibrationStep.CalculateOffset;
                }
                else
                {
                    _firstMatch = match;
                    CurrentStep = ColletCalibrationStep.AdjustThetaToZero;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-FIND-EX", "Vision",
                    "Bottom Camera Collet 찾기 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AdjustThetaToZeroAsync(CancellationToken ct)
        {
            try
            {
                MatchResultDto match = _firstMatch;
                for (int i = 0; i < _settings.MaxThetaIterations; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (match == null || !match.Success)
                        return Fail("COLLET-CAL-THETA-NO-MATCH", "Vision", "Collet T 보정에 사용할 Vision 결과가 없습니다.");

                    double theta = match.AngleDeg;
                    if (Math.Abs(theta) <= _settings.ThetaToleranceDeg)
                    {
                        CurrentStep = ColletCalibrationStep.AdjustXyToCenter;
                        return 0;
                    }

                    BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
                    double actual = ReadPickerActual(GetPickerTAxis(_colletIndex), tAxis, _basePickerT);
                    double target = actual + theta * _settings.ThetaMoveGain;
                    int moveResult = await MovePickerAxisAndVerifyAsync(
                        GetPickerTAxis(_colletIndex),
                        target,
                        "Collet Calibration T 0도 보정",
                        ct,
                        BottomFinderTargetName,
                        true).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;
                    ApplyPickerAxisPositionForSimulation(GetPickerTAxis(_colletIndex), target);
                    UpdateSimulatedPickerPosition(GetPickerTAxis(_colletIndex), target);

                    match = await RequestColletMatchAsync(ct).ConfigureAwait(false);
                    _firstMatch = match;
                }

                return Fail("COLLET-CAL-THETA-NOT-CONVERGED", Name,
                    "Collet T 보정이 허용오차 안으로 수렴하지 않았습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", toleranceDeg=" + _settings.ThetaToleranceDeg +
                    ", maxIteration=" + _settings.MaxThetaIterations +
                    ", lastTheta=" + (match != null ? match.AngleDeg.ToString("F6") : "null"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-THETA-EX", Name,
                    "Collet T 보정 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> AdjustXyToCenterAsync(CancellationToken ct)
        {
            try
            {
                MatchResultDto match = await RequestColletMatchAsync(ct).ConfigureAwait(false);
                MatchStepResult xyResult = await RefineXyToCenterAsync(match, "1차 XY 중심 보정", ct).ConfigureAwait(false);
                if (xyResult.Result != 0)
                    return xyResult.Result;

                MatchStepResult thetaResult = await RefineThetaToZeroAgainAsync(xyResult.Match, "최종 T 보정", ct).ConfigureAwait(false);
                if (thetaResult.Result != 0)
                    return thetaResult.Result;

                xyResult = await RefineXyToCenterAsync(thetaResult.Match, "최종 XY 중심 보정", ct).ConfigureAwait(false);
                if (xyResult.Result != 0)
                    return xyResult.Result;

                _finalMatch = xyResult.Match;
                CurrentStep = ColletCalibrationStep.FindColletAgain;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-XY-EX", Name,
                    "Collet XY 중심 보정 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<MatchStepResult> RefineThetaToZeroAgainAsync(MatchResultDto startMatch, string label, CancellationToken ct)
        {
            MatchResultDto lastMatch = startMatch;
            try
            {
                MatchResultDto match = startMatch;
                for (int i = 0; i < _settings.MaxThetaIterations; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (match == null || !match.Success)
                        return new MatchStepResult { Result = Fail("COLLET-CAL-THETA-NO-MATCH", "Vision", "Collet T 보정에 사용할 Vision 결과가 없습니다."), Match = lastMatch };

                    double theta = match.AngleDeg;
                    BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
                    double actual = ReadPickerActual(GetPickerTAxis(_colletIndex), tAxis, _basePickerT);
                    if (Math.Abs(theta) <= _settings.ThetaToleranceDeg)
                    {
                        QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalTheta",
                            label + " 완료. side=" + _calibrationSide +
                            ", colletNo=" + _colletNo +
                            ", iteration=" + i +
                            ", theta=" + theta.ToString("F6") +
                            ", toleranceDeg=" + _settings.ThetaToleranceDeg.ToString("F6") +
                            ", actualT=" + actual.ToString("F6") + " - Ok");
                        lastMatch = match;
                        return new MatchStepResult { Result = 0, Match = lastMatch };
                    }

                    double target = actual + theta * _settings.ThetaMoveGain;
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalTheta",
                        label + " 이동. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", iteration=" + i +
                        ", formula=targetT=actualT+theta*gain=" + actual.ToString("F6") + "+" + theta.ToString("F6") + "*" + _settings.ThetaMoveGain.ToString("F6") +
                        "=" + target.ToString("F6"));

                    int moveResult = await MovePickerAxisAndVerifyAsync(
                        GetPickerTAxis(_colletIndex),
                        target,
                        "Collet Calibration T 0도 보정",
                        ct,
                        BottomFinderTargetName,
                        true).ConfigureAwait(false);
                    if (moveResult != 0)
                        return new MatchStepResult { Result = moveResult, Match = lastMatch };
                    ApplyPickerAxisPositionForSimulation(GetPickerTAxis(_colletIndex), target);
                    UpdateSimulatedPickerPosition(GetPickerTAxis(_colletIndex), target);

                    match = await RequestColletMatchAsync(ct).ConfigureAwait(false);
                    lastMatch = match;
                }

                return new MatchStepResult { Result = Fail("COLLET-CAL-THETA-NOT-CONVERGED", Name,
                    "Collet T 보정이 허용오차 안으로 수렴하지 않았습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", toleranceDeg=" + _settings.ThetaToleranceDeg +
                    ", maxIteration=" + _settings.MaxThetaIterations +
                    ", lastTheta=" + (lastMatch != null ? lastMatch.AngleDeg.ToString("F6") : "null")), Match = lastMatch };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<MatchStepResult> RefineXyToCenterAsync(MatchResultDto startMatch, string label, CancellationToken ct)
        {
            MatchResultDto lastMatch = startMatch;
            try
            {
                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                    Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                    AutoVisionChannel.BottomInspection);

                MatchResultDto match = startMatch;
                for (int i = 0; i < _settings.MaxXyIterations; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (match == null || !match.Success)
                        return new MatchStepResult { Result = Fail("COLLET-CAL-XY-NO-MATCH", "Vision", "Collet XY 보정에 사용할 Vision 결과가 없습니다."), Match = lastMatch };

                    double offsetMmX;
                    double offsetMmY;
                    double diagonal;
                    bool inTolerance;
                    if (!TryEvaluateXyMatchTolerance(camera, match, out offsetMmX, out offsetMmY, out diagonal, out inTolerance))
                        return new MatchStepResult { Result = Fail("COLLET-CAL-XY-NO-MATCH", "Vision", "Collet XY 보정에 사용할 Vision 결과가 없습니다."), Match = lastMatch };

                    BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
                    BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
                    double actualX = ReadPickerActual(PickerAxis.PickerX, xAxis, _targetPickerX);
                    double actualY = ReadPickerActual(PickerAxis.PickerY, yAxis, _targetPickerY);

                    if (inTolerance)
                    {
                        QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXy",
                            label + " 완료. side=" + _calibrationSide +
                            ", colletNo=" + _colletNo +
                            ", iteration=" + i +
                            ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                            ", diagonal=" + diagonal.ToString("F6") +
                            ", toleranceMm=" + _settings.XyToleranceMm.ToString("F6") +
                            ", mode=" + (_settings.UseDiagonalXyTolerance ? "Diagonal" : "Axis") +
                            ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + ") - Ok");
                        lastMatch = match;
                        return new MatchStepResult { Result = 0, Match = lastMatch };
                    }

                    double targetX = actualX + offsetMmX * _settings.XyMoveGainX;
                    double targetY = actualY + offsetMmY * _settings.XyMoveGainY;
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXy",
                        label + " 이동. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", iteration=" + i +
                        ", pixel=(" + match.X.ToString("F3") + "," + match.Y.ToString("F3") + ")" +
                        ", center=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")" +
                        ", scale=(" + camera.PixelToMmX.ToString("F9") + "," + camera.PixelToMmY.ToString("F9") + ")" +
                        ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                        ", formulaX=targetX=actualX+offsetMmX*gainX=" + actualX.ToString("F6") + "+" + offsetMmX.ToString("F6") + "*" + _settings.XyMoveGainX.ToString("F6") + "=" + targetX.ToString("F6") +
                        ", formulaY=targetY=actualY+offsetMmY*gainY=" + actualY.ToString("F6") + "+" + offsetMmY.ToString("F6") + "*" + _settings.XyMoveGainY.ToString("F6") + "=" + targetY.ToString("F6"));

                    var xyTargets = new Dictionary<PickerAxis, double>();
                    xyTargets[PickerAxis.PickerX] = targetX;
                    xyTargets[PickerAxis.PickerY] = targetY;
                    double fineAlignDx = Math.Abs(targetX - actualX);
                    double fineAlignDy = Math.Abs(targetY - actualY);
                    bool fineAlign = IsFineAlignXyMove(actualX, actualY, targetX, targetY);
                    string xyMoveTargetName = fineAlign ? FineAlignTargetName : BottomFinderTargetName;
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalFineAlignDecision",
                        label + " FineAlign 판정. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", dx=" + fineAlignDx.ToString("F6") +
                        ", dy=" + fineAlignDy.ToString("F6") +
                        ", fineAlignMaxMm=" + _settings.FineAlignMaxXyMoveMm.ToString("F6") +
                        ", forceMove=True" +
                        ", formula=fineAlign=(dx<=max && dy<=max)=(" +
                        fineAlignDx.ToString("F6") + "<=" + _settings.FineAlignMaxXyMoveMm.ToString("F6") +
                        " && " + fineAlignDy.ToString("F6") + "<=" + _settings.FineAlignMaxXyMoveMm.ToString("F6") + ")=" + fineAlign +
                        ", moveTargetName=" + xyMoveTargetName);

                    if (!fineAlign)
                    {
                        int zAvoidResult = await MoveSelectedColletZToAvoidForXyAsync(label, ct).ConfigureAwait(false);
                        if (zAvoidResult != 0)
                            return new MatchStepResult { Result = zAvoidResult, Match = lastMatch };

                        int yAvoidResult = await MovePickerYToAvoidForLargeXyAsync(label, ct).ConfigureAwait(false);
                        if (yAvoidResult != 0)
                            return new MatchStepResult { Result = yAvoidResult, Match = lastMatch };
                    }
                    else
                    {
                        QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalFineAlign",
                            label + " Z Down 상태 XY 미세 이동 적용. side=" + _calibrationSide +
                            ", colletNo=" + _colletNo +
                            ", dx=" + Math.Abs(targetX - actualX).ToString("F6") +
                            ", dy=" + Math.Abs(targetY - actualY).ToString("F6") +
                            ", max=" + _settings.FineAlignMaxXyMoveMm.ToString("F6"));
                    }

                    int moveResult = await MovePickerXTThenYAndVerifyAsync(
                        xyTargets,
                        "Collet Calibration XY 중심 보정",
                        ct,
                        xyMoveTargetName,
                        true).ConfigureAwait(false);
                    if (moveResult != 0)
                        return new MatchStepResult { Result = moveResult, Match = lastMatch };
                    ApplyPickerAxisPositionForSimulation(PickerAxis.PickerX, targetX);
                    ApplyPickerAxisPositionForSimulation(PickerAxis.PickerY, targetY);
                    UpdateSimulatedPickerPosition(PickerAxis.PickerX, targetX);
                    UpdateSimulatedPickerPosition(PickerAxis.PickerY, targetY);

                    _targetPickerX = targetX;
                    _targetPickerY = targetY;

                    if (!fineAlign)
                    {
                        int zReturnResult = await MoveSelectedColletZToInspectionForXyAsync(label, ct).ConfigureAwait(false);
                        if (zReturnResult != 0)
                            return new MatchStepResult { Result = zReturnResult, Match = lastMatch };
                    }

                    match = await RequestColletMatchAsync(ct).ConfigureAwait(false);
                    lastMatch = match;
                }

                double finalOffsetMmX;
                double finalOffsetMmY;
                double finalDiagonal;
                bool finalInTolerance;
                if (TryEvaluateXyMatchTolerance(camera, lastMatch, out finalOffsetMmX, out finalOffsetMmY, out finalDiagonal, out finalInTolerance) &&
                    finalInTolerance)
                {
                    BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
                    BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
                    double actualX = ReadPickerActual(PickerAxis.PickerX, xAxis, _targetPickerX);
                    double actualY = ReadPickerActual(PickerAxis.PickerY, yAxis, _targetPickerY);

                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXy",
                        label + " 최종 측정 완료. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", iteration=" + _settings.MaxXyIterations +
                        ", offsetMm=(" + finalOffsetMmX.ToString("F6") + "," + finalOffsetMmY.ToString("F6") + ")" +
                        ", diagonal=" + finalDiagonal.ToString("F6") +
                        ", toleranceMm=" + _settings.XyToleranceMm.ToString("F6") +
                        ", mode=" + (_settings.UseDiagonalXyTolerance ? "Diagonal" : "Axis") +
                        ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + ") - Ok");

                    return new MatchStepResult { Result = 0, Match = lastMatch };
                }

                return new MatchStepResult { Result = Fail("COLLET-CAL-XY-NOT-CONVERGED", Name,
                    "Collet XY 중심 보정이 허용오차 안으로 수렴하지 않았습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", toleranceMm=" + _settings.XyToleranceMm +
                    ", maxIteration=" + _settings.MaxXyIterations +
                    ", mode=" + (_settings.UseDiagonalXyTolerance ? "Diagonal" : "Axis") +
                    ", lastOffset=(" +
                    (lastMatch != null && lastMatch.Success ? finalOffsetMmX.ToString("F6") : "null") + "," +
                    (lastMatch != null && lastMatch.Success ? finalOffsetMmY.ToString("F6") : "null") + ")" +
                    ", lastDiagonal=" + (lastMatch != null && lastMatch.Success ? finalDiagonal.ToString("F6") : "null")), Match = lastMatch };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private bool TryEvaluateXyMatchTolerance(
            VisionCameraPixelCalibration camera,
            MatchResultDto match,
            out double offsetMmX,
            out double offsetMmY,
            out double diagonal,
            out bool inTolerance)
        {
            offsetMmX = 0.0;
            offsetMmY = 0.0;
            diagonal = 0.0;
            inTolerance = false;

            if (camera == null || match == null || !match.Success)
                return false;

            offsetMmX = camera.PixelToMmOffsetX(match.X);
            offsetMmY = camera.PixelToMmOffsetY(match.Y);
            diagonal = Math.Sqrt((offsetMmX * offsetMmX) + (offsetMmY * offsetMmY));
            inTolerance = _settings.UseDiagonalXyTolerance
                ? diagonal <= _settings.XyToleranceMm
                : Math.Abs(offsetMmX) <= _settings.XyToleranceMm && Math.Abs(offsetMmY) <= _settings.XyToleranceMm;
            return true;
        }

        private bool IsFineAlignXyMove(double actualX, double actualY, double targetX, double targetY)
        {
            try
            {
                double maxMove = _settings != null ? _settings.FineAlignMaxXyMoveMm : 0.2;
                if (maxMove <= 0.0)
                    return false;

                double dx = Math.Abs(targetX - actualX);
                double dy = Math.Abs(targetY - actualY);
                return dx <= maxMove && dy <= maxMove;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerYToAvoidForLargeXyAsync(string label, CancellationToken ct)
        {
            try
            {
                double avoidY = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXyY",
                    label + " XY 보정량이 FineAlign 범위를 초과하여 PickerY Avoid 후 X 이동을 진행합니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", targetY=" + avoidY.ToString("F6") +
                    ", fineAlignMax=" + (_settings != null ? _settings.FineAlignMaxXyMoveMm.ToString("F6") : "0.200000"));

                int result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    avoidY,
                    "Collet Calibration 큰 XY 보정 전 PickerY Avoid",
                    ct,
                    "ColletCalibration;PickerZone=Avoid",
                    true).ConfigureAwait(false);
                if (result == 0)
                {
                    ApplyPickerAxisPositionForSimulation(PickerAxis.PickerY, avoidY);
                    UpdateSimulatedPickerPosition(PickerAxis.PickerY, avoidY);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-XY-Y-AVOID-EX", Name,
                    "Collet 큰 XY 보정 전 PickerY Avoid 이동 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSelectedColletZToAvoidForXyAsync(string label, CancellationToken ct)
        {
            try
            {
                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                double avoidZ = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXyZ",
                    label + " XY 이동 전 선택 Collet Z Avoid 이동. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", zAxis=" + zAxis +
                    ", targetZ=" + avoidZ.ToString("F6"));

                int result = await MovePickerAxisAndVerifyAsync(
                    zAxis,
                    avoidZ,
                    "Collet Calibration XY 이동 전 Z Avoid",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result == 0)
                    ApplyPickerAxisPositionForSimulation(zAxis, avoidZ);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-XY-Z-AVOID-EX", Name,
                    "Collet XY 이동 전 선택 Collet Z Avoid 이동 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSelectedColletZToInspectionForXyAsync(string label, CancellationToken ct)
        {
            try
            {
                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXyZ",
                    label + " XY 이동 후 선택 Collet Z 검사 위치 복귀. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", zAxis=" + zAxis +
                    ", targetZ=" + _targetPickerZ.ToString("F6"));

                int result = await MovePickerAxisAndVerifyAsync(
                    zAxis,
                    _targetPickerZ,
                    "Collet Calibration XY 이동 후 Z 검사 위치",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result == 0)
                    ApplyPickerAxisPositionForSimulation(zAxis, _targetPickerZ);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-XY-Z-RETURN-EX", Name,
                    "Collet XY 이동 후 선택 Collet Z 검사 위치 복귀 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", targetZ=" + _targetPickerZ.ToString("F6") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RunAutoFocusIfNeededAsync(CancellationToken ct)
        {
            try
            {
                if (_settings == null || !_settings.RunAutoFocusAfterTheta)
                    return 0;

                VisionFocusCalibrationData focusData = Context.Machine.VisionUnit.Config.FocusCalibration;
                focusData.EnsureObjects();
                VisionFocusScanSettings focusSettings = focusData.BottomColletScan;
                focusSettings.EnsureDefaults();

                var request = new VisionFocusScanRequest
                {
                    Kind = VisionFocusScanKind.BottomCollet,
                    PickerSide = _calibrationSide,
                    PickerNo = _colletNo,
                    DefaultPosition = _targetPickerZ,
                    MinusRange = focusSettings.MinusRange,
                    PlusRange = focusSettings.PlusRange,
                    Step = focusSettings.Step,
                    FineMinusRange = focusSettings.FineMinusRange,
                    FinePlusRange = focusSettings.FinePlusRange,
                    FineStep = focusSettings.FineStep,
                    RepeatCount = focusSettings.RepeatCount,
                    MoveVelocity = focusSettings.MoveVelocity,
                    MoveAcceleration = focusSettings.MoveAcceleration,
                    MoveDeceleration = focusSettings.MoveDeceleration,
                    SettleDelayMs = focusSettings.SettleDelayMs,
                    MotionTimeoutMs = focusSettings.MotionTimeoutMs,
                    VisionTimeoutMs = focusSettings.VisionTimeoutMs,
                    VisionBestTimeoutMs = focusSettings.VisionBestTimeoutMs,
                    FocusValueReceiveMode = focusSettings.FocusValueReceiveMode,
                    ReturnToDefaultAfterScan = false,
                    UpdatedBy = "ColletCalibration"
                };

                var focus = new VisionFocusScanSequence(Context.Machine, request);
                int result = await focus.RunAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-AUTO-FOCUS", Name,
                        "Collet Calibration 시작 AutoFocus 실행에 실패했습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", message=" + focus.Result.Message);

                _targetPickerZ = focus.Result.BestPosition;
                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                result = await MovePickerAxisAndVerifyAsync(
                    zAxis,
                    _targetPickerZ,
                    "Collet Calibration AutoFocus Best Z",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ApplyPickerAxisPositionForSimulation(zAxis, _targetPickerZ);
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalAutoFocus",
                    "Collet Calibration AutoFocus Best Z 적용. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", zAxis=" + zAxis +
                    ", bestZ=" + _targetPickerZ.ToString("F6") +
                    ", score=" + focus.Result.BestScore.ToString("F6") +
                    ", sampleCount=" + focus.Result.SampleCount);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-AUTO-FOCUS-EX", Name,
                    "Collet AutoFocus 실행 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool TryResolveBottomColletFocusStartPosition(out double position, out string reason)
        {
            position = 0.0;
            reason = string.Empty;

            if (Context == null || Context.Machine == null ||
                Context.Machine.VisionUnit == null ||
                Context.Machine.VisionUnit.Config == null)
            {
                reason = "Vision Focus Cal 설정 객체가 없습니다.";
                return false;
            }

            VisionFocusCalibrationData focusData = Context.Machine.VisionUnit.Config.FocusCalibration;
            if (focusData == null)
            {
                reason = "FocusCalibration 설정이 없습니다.";
                return false;
            }

            focusData.EnsureObjects();
            VisionFocusPositionRecord record = focusData.GetColletRecord(_calibrationSide, _colletNo);
            if (!HasRegisteredFocusDefaultPosition(record))
            {
                reason = "recordValid=" + (record != null && record.Valid) +
                         ", default=" + (record != null ? record.DefaultPosition.ToString("F6") : "null") +
                         ", best=" + (record != null ? record.BestPosition.ToString("F6") : "null");
                return false;
            }

            position = record.DefaultPosition;
            return true;
        }

        private static bool HasRegisteredFocusDefaultPosition(VisionFocusPositionRecord record)
        {
            if (record == null)
                return false;

            return record.Valid || Math.Abs(record.DefaultPosition) > 0.0000001;
        }

        private int CalculateOffset()
        {
            try
            {
                if (_finalMatch == null || !_finalMatch.Success)
                    return Fail("COLLET-CAL-CALC-NO-MATCH", Name, "Collet 보정값 계산에 사용할 최종 Vision 결과가 없습니다.");

                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                    Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                    AutoVisionChannel.BottomInspection);
                double centerMmX = camera.PixelToMmOffsetX(_finalMatch.X);
                double centerMmY = camera.PixelToMmOffsetY(_finalMatch.Y);
                BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
                BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
                BaseAxis zAxis = GetPickerAxis(GetPickerZAxis(_colletIndex));
                _measuredTPosition = tAxis != null ? tAxis.ActualPosition : _basePickerT;
                double finalPickerX = xAxis != null ? xAxis.ActualPosition : _targetPickerX;
                double finalPickerY = yAxis != null ? yAxis.ActualPosition : _targetPickerY;
                double finalPickerZ = zAxis != null ? zAxis.ActualPosition : _targetPickerZ;
                double colletOffsetX = IsReferenceCollet() ? 0.0 : finalPickerX - _nominalPickerX;
                double colletOffsetY = IsReferenceCollet() ? 0.0 : finalPickerY - _nominalPickerY;
                double referenceTeachingShiftX = IsReferenceCollet() ? finalPickerX - _nominalPickerX : 0.0;
                double referenceTeachingShiftY = IsReferenceCollet() ? finalPickerY - _nominalPickerY : 0.0;
                double activeTPcHomeOffset = ResolvePickerTPcHomeOffset(tAxis);
                double tZeroResidual = _measuredTPosition - _basePickerT;
                double tZeroHomeOffset = activeTPcHomeOffset + tZeroResidual;

                _calculatedRecord = new ColletCalibrationRecord
                {
                    Side = _calibrationSide,
                    ColletNo = _colletNo,
                    CenterPixelX = _finalMatch.X,
                    CenterPixelY = _finalMatch.Y,
                    CenterMmX = centerMmX,
                    CenterMmY = centerMmY,
                    OffsetX = colletOffsetX,
                    OffsetY = colletOffsetY,
                    ThetaOffset = _finalMatch.AngleDeg,
                    TZeroHomeOffset = tZeroHomeOffset,
                    MeasuredTPosition = _measuredTPosition,
                    FinalPickerX = finalPickerX,
                    FinalPickerY = finalPickerY,
                    FinalPickerZ = finalPickerZ,
                    FinalPickerT = _measuredTPosition,
                    Valid = true,
                    UpdatedAt = DateTime.Now
                };

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalResult",
                    "Collet Calibration 계산 완료. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", centerPixel=(" + _finalMatch.X.ToString("F3") + "," + _finalMatch.Y.ToString("F3") + ")" +
                    ", centerMm=(" + centerMmX.ToString("F6") + "," + centerMmY.ToString("F6") + ")" +
                    ", formulaCenterMmX=(pixelX-centerX)*scaleX=(" + _finalMatch.X.ToString("F3") + "-" + camera.ImageCenterPixelX.ToString("F3") + ")*" + camera.PixelToMmX.ToString("F9") + "=" + centerMmX.ToString("F6") +
                    ", formulaCenterMmY=(pixelY-centerY)*scaleY=(" + _finalMatch.Y.ToString("F3") + "-" + camera.ImageCenterPixelY.ToString("F3") + ")*" + camera.PixelToMmY.ToString("F9") + "=" + centerMmY.ToString("F6") +
                    ", offset=(" + _calculatedRecord.OffsetX.ToString("F6") + "," + _calculatedRecord.OffsetY.ToString("F6") + ")" +
                    ", formulaOffsetX=" + (IsReferenceCollet() ? "referenceCollet=0" : "finalPickerX-nominalPickerX=" + finalPickerX.ToString("F6") + "-" + _nominalPickerX.ToString("F6") + "=" + colletOffsetX.ToString("F6")) +
                    ", formulaOffsetY=" + (IsReferenceCollet() ? "referenceCollet=0" : "finalPickerY-nominalPickerY=" + finalPickerY.ToString("F6") + "-" + _nominalPickerY.ToString("F6") + "=" + colletOffsetY.ToString("F6")) +
                    ", nominalPicker=(" + _nominalPickerX.ToString("F6") + "," + _nominalPickerY.ToString("F6") + ")" +
                    ", referenceTeachingShift=(" + referenceTeachingShiftX.ToString("F6") + "," + referenceTeachingShiftY.ToString("F6") + ")" +
                    ", thetaOffset=" + _calculatedRecord.ThetaOffset.ToString("F6") +
                    ", finalPicker=(" + _calculatedRecord.FinalPickerX.ToString("F6") + "," + _calculatedRecord.FinalPickerY.ToString("F6") + "," + _calculatedRecord.FinalPickerZ.ToString("F6") + "," + _calculatedRecord.FinalPickerT.ToString("F6") + ")" +
                    ", activeTPcHomeOffset=" + activeTPcHomeOffset.ToString("F6") +
                    ", tZeroResidual=measuredT-baseT=" + _measuredTPosition.ToString("F6") + "-" + _basePickerT.ToString("F6") + "=" + tZeroResidual.ToString("F6") +
                    ", tZeroHomeOffset=activePcOffset+residual=" + activeTPcHomeOffset.ToString("F6") + "+" + tZeroResidual.ToString("F6") + "=" + _calculatedRecord.TZeroHomeOffset.ToString("F6"));

                CurrentStep = ColletCalibrationStep.SaveColletCalibration;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-CALC-EX", Name, "Collet 보정값 계산 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int SaveColletCalibration()
        {
            try
            {
                ColletCalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData.Collet;
                data.EnsureObjects();
                ColletCalibrationRecord target = data.GetRecord(_calibrationSide, _colletNo);
                CopyRecord(_calculatedRecord, target);
                Context.Machine.VisionUnit.Config.CalibrationData.Touch("ColletCalibration");
                int referenceTeachingResult = SaveReferenceColletBottomTeachingIfNeeded(target);
                if (referenceTeachingResult != 0)
                    return referenceTeachingResult;

                string offsetSummary;
                PickerVisionOffsetCalibrationService.TryApplyAvailableOffsets(Context.Machine, "ColletCalibration", out offsetSummary);
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalPickerVisionOffset", offsetSummary);

                if (!Context.Machine.SaveSettings())
                    return Fail("COLLET-CAL-SAVE", Name, "Collet Calibration 결과를 CalibrationData 파일에 저장하지 못했습니다.");
                ResultRecord = target;
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSave",
                    "Collet Calibration 결과 저장 완료. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", centerPixel=(" + target.CenterPixelX.ToString("F3") + "," + target.CenterPixelY.ToString("F3") + ")" +
                    ", centerMm=(" + target.CenterMmX.ToString("F6") + "," + target.CenterMmY.ToString("F6") + ")" +
                    ", offset=(" + target.OffsetX.ToString("F6") + "," + target.OffsetY.ToString("F6") + ")" +
                    ", thetaOffset=" + target.ThetaOffset.ToString("F6") +
                    ", tZeroHomeOffset=" + target.TZeroHomeOffset.ToString("F6") +
                    ", measuredT=" + target.MeasuredTPosition.ToString("F6") +
                    ", finalPicker=(" + target.FinalPickerX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + "," + target.FinalPickerZ.ToString("F6") + "," + target.FinalPickerT.ToString("F6") + ")" +
                    ", valid=" + target.Valid +
                    ", updatedAt=" + target.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                WriteLog("ColletCalibrationSequence",
                    Name + " Collet Calibration 저장 완료. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", offsetX=" + target.OffsetX.ToString("F6") +
                    ", offsetY=" + target.OffsetY.ToString("F6") +
                    ", thetaOffset=" + target.ThetaOffset.ToString("F6") +
                    ", tZeroHomeOffset=" + target.TZeroHomeOffset.ToString("F6") + " - Ok");

                CurrentStep = ColletCalibrationStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-SAVE-EX", Name, "Collet Calibration 저장 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<MatchResultDto> RequestColletMatchAsync(CancellationToken ct)
        {
            if (IsVisionResultSimulationAllowed())
                return BuildSimulatedColletMatch();

            MatchResultDto match = await AutoVisionRequestService.MatchAsync(
                AutoVisionChannel.BottomInspection,
                _settings.BottomFinderName,
                _colletNo,
                _settings.VisionTimeoutMs,
                ct).ConfigureAwait(false);

            if ((match == null || !match.Success) && IsVisionResultSimulationAllowed())
                return BuildSimulatedColletMatch();

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalVision",
                "Collet Calibration Vision 결과 수신. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", success=" + (match != null && match.Success) +
                ", pixel=(" + (match != null ? match.X.ToString("F3") : "null") + "," + (match != null ? match.Y.ToString("F3") : "null") + ")" +
                ", angleDeg=" + (match != null ? match.AngleDeg.ToString("F6") : "null") +
                ", score=" + (match != null ? match.Score.ToString("F6") : "null") +
                ", threshold=" + (_settings != null ? _settings.ScoreThreshold.ToString("F6") : "null") +
                ", raw=" + (match != null ? match.RawError : "null"));

            if (match != null && match.Success && _settings.ScoreThreshold > 0.0 && match.Score < _settings.ScoreThreshold)
            {
                match.Success = false;
                match.RawError = "Score가 기준보다 낮습니다. score=" + match.Score.ToString("F6") +
                                 ", threshold=" + _settings.ScoreThreshold.ToString("F6");
            }

            if (match != null && !match.Success && _settings != null && _settings.ScoreThreshold > 0.0)
            {
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalVision",
                    "Collet Calibration Vision Score 기준 확인. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", score=" + match.Score.ToString("F6") +
                    ", threshold=" + _settings.ScoreThreshold.ToString("F6") +
                    ", formula=score>=threshold=" + match.Score.ToString("F6") + ">=" + _settings.ScoreThreshold.ToString("F6") + "=" + (match.Score >= _settings.ScoreThreshold));
            }

            return match;
        }

        private bool IsVisionResultSimulationAllowed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && (!settings.UseVision || settings.DryRunMode || settings.SimulationMode || settings.BypassHardware);
        }

        private bool IsAxisRuntimeSimulationAllowed()
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null)
            {
                if (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin)
                    return true;

                if (settings.DryRunMode)
                    return false;
            }

            if (Side == PickerSequenceSide.Front)
            {
                return FrontPicker != null &&
                       ((FrontPicker.Setup != null && FrontPicker.Setup.IsSimulationMode) ||
                        (FrontPicker.Config != null && FrontPicker.Config.IsSimulationMode));
            }

            if (Side == PickerSequenceSide.Rear)
            {
                return RearPicker != null &&
                       ((RearPicker.Setup != null && RearPicker.Setup.IsSimulationMode) ||
                        (RearPicker.Config != null && RearPicker.Config.IsSimulationMode));
            }

            return false;
        }

        private MatchResultDto BuildSimulatedColletMatch()
        {
            VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                AutoVisionChannel.BottomInspection);
            camera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);

            if (IsDryRunWithVisionDisabled())
            {
                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SIM-VISION-ZERO",
                    "드라이런 Vision 미사용 상태라 Collet Calibration Vision 보정값을 0으로 처리합니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")");

                return new MatchResultDto
                {
                    Success = true,
                    X = camera.ImageCenterPixelX,
                    Y = camera.ImageCenterPixelY,
                    AngleDeg = 0.0,
                    Score = 1.0,
                    ImageWidthPixel = camera.ImageWidthPixel,
                    ImageHeightPixel = camera.ImageHeightPixel,
                    HasImageSize = true,
                    RawError = "SIMULATION:ColletCalibration:ZeroOffset"
                };
            }

            BaseAxis xAxis = GetPickerAxis(PickerAxis.PickerX);
            BaseAxis yAxis = GetPickerAxis(PickerAxis.PickerY);
            BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
            double actualX = ReadPickerActual(PickerAxis.PickerX, xAxis, _targetPickerX);
            double actualY = ReadPickerActual(PickerAxis.PickerY, yAxis, _targetPickerY);
            double actualT = ReadPickerActual(GetPickerTAxis(_colletIndex), tAxis, _basePickerT);

            EnsureSimulatedColletTarget(actualX, actualY, actualT, camera);

            double offsetMmX = _simColletCenterPickerX.Value - actualX;
            double offsetMmY = _simColletCenterPickerY.Value - actualY;
            double theta = _simColletZeroPickerT.Value - actualT;
            double pixelX = camera.ImageCenterPixelX + SafeMmToPixel(offsetMmX, camera.PixelToMmX) + NextSimulatedValue(0.0, SimColletNoisePixel);
            double pixelY = camera.ImageCenterPixelY + SafeMmToPixel(offsetMmY, camera.PixelToMmY) + NextSimulatedValue(0.0, SimColletNoisePixel);
            double angle = theta + NextSimulatedValue(0.0, SimColletNoiseAngleDeg);
            double score = NextSimulatedScore();

            string simulationMessage =
                "Collet Calibration Vision 결과를 작은 랜덤 오차로 시뮬레이션합니다. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + "," + pixelY.ToString("F3") + ")" +
                ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                ", formulaPixelX=centerX+(offsetMmX/scaleX)+noise=" + camera.ImageCenterPixelX.ToString("F3") + "+(" + offsetMmX.ToString("F6") + "/" + camera.PixelToMmX.ToString("F9") + ")+noise=" + pixelX.ToString("F3") +
                ", formulaPixelY=centerY+(offsetMmY/scaleY)+noise=" + camera.ImageCenterPixelY.ToString("F3") + "+(" + offsetMmY.ToString("F6") + "/" + camera.PixelToMmY.ToString("F9") + ")+noise=" + pixelY.ToString("F3") +
                ", formulaAngle=zeroT-actualT+noise=" + _simColletZeroPickerT.Value.ToString("F6") + "-" + actualT.ToString("F6") + "+noise=" + angle.ToString("F6") +
                ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + "," + actualT.ToString("F6") + ")" +
                ", virtualTarget=(" + _simColletCenterPickerX.Value.ToString("F6") + "," + _simColletCenterPickerY.Value.ToString("F6") + "," + _simColletZeroPickerT.Value.ToString("F6") + ")" +
                ", scale=(" + camera.PixelToMmX.ToString("F9") + "," + camera.PixelToMmY.ToString("F9") + ")" +
                ", score=" + score.ToString("F6");

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSimVision", simulationMessage);

            EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-SIM-VISION",
                "Collet Calibration Vision 결과를 작은 랜덤 오차로 시뮬레이션합니다. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + "," + pixelY.ToString("F3") + ")" +
                ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                ", angle=" + angle.ToString("F6") +
                ", actual=(" + actualX.ToString("F6") + "," + actualY.ToString("F6") + "," + actualT.ToString("F6") + ")" +
                ", virtualTarget=(" + _simColletCenterPickerX.Value.ToString("F6") + "," + _simColletCenterPickerY.Value.ToString("F6") + "," + _simColletZeroPickerT.Value.ToString("F6") + ")" +
                ", scale=(" + camera.PixelToMmX.ToString("F9") + "," + camera.PixelToMmY.ToString("F9") + ")" +
                ", score=" + score.ToString("F6"));

            return new MatchResultDto
            {
                Success = true,
                X = pixelX,
                Y = pixelY,
                AngleDeg = angle,
                Score = score,
                ImageWidthPixel = camera.ImageWidthPixel,
                ImageHeightPixel = camera.ImageHeightPixel,
                HasImageSize = true,
                RawError = "SIMULATION:ColletCalibration"
            };
        }

        private void EnsureSimulatedColletTarget(double actualX, double actualY, double actualT, VisionCameraPixelCalibration camera)
        {
            if (_simColletCenterPickerX.HasValue && _simColletCenterPickerY.HasValue && _simColletZeroPickerT.HasValue)
                return;

            double initialOffsetMmX = NextSimulatedValue(0.0, SimColletMaxPixelOffset) * camera.PixelToMmX;
            double initialOffsetMmY = NextSimulatedValue(0.0, SimColletMaxPixelOffset) * camera.PixelToMmY;
            double initialTheta = NextSimulatedValue(0.0, SimColletMaxAngleDeg);
            _simColletCenterPickerX = actualX + initialOffsetMmX;
            _simColletCenterPickerY = actualY + initialOffsetMmY;
            _simColletZeroPickerT = actualT + initialTheta;
        }

        private static double SafeMmToPixel(double mm, double mmPerPixel)
        {
            if (Math.Abs(mmPerPixel) <= double.Epsilon)
                return 0.0;

            return mm / mmPerPixel;
        }

        private static double NextSimulatedValue(double center, double maxAbsOffset)
        {
            lock (SimColletRandomLock)
            {
                return center + ((SimColletRandom.NextDouble() * 2.0) - 1.0) * maxAbsOffset;
            }
        }

        private static double NextSimulatedScore()
        {
            return 1.0;
        }

        private double ReadPickerActual(PickerAxis axis, BaseAxis axisObject, double fallback)
        {
            if (IsAxisRuntimeSimulationAllowed())
            {
                if (axis == PickerAxis.PickerX && _simPickerX.HasValue)
                    return _simPickerX.Value;
                if (axis == PickerAxis.PickerY && _simPickerY.HasValue)
                    return _simPickerY.Value;
                if (IsPickerThetaAxis(axis) && _simPickerT.HasValue)
                    return _simPickerT.Value;
            }

            return axisObject != null ? axisObject.ActualPosition : fallback;
        }

        private void UpdateSimulatedPickerPosition(PickerAxis axis, double target)
        {
            if (!IsAxisRuntimeSimulationAllowed())
                return;

            if (axis == PickerAxis.PickerX)
            {
                _simPickerX = target;
                return;
            }

            if (axis == PickerAxis.PickerY)
            {
                _simPickerY = target;
                return;
            }

            if (IsPickerThetaAxis(axis))
                _simPickerT = target;
        }

        private void ApplyCurrentPickerAvoidPositionForSimulation()
        {
            PickerAxis[] axes =
            {
                PickerAxis.PickerX,
                PickerAxis.PickerY,
                PickerAxis.PickerT0,
                PickerAxis.PickerT1,
                PickerAxis.PickerT2,
                PickerAxis.PickerT3,
                PickerAxis.PickerZ0,
                PickerAxis.PickerZ1,
                PickerAxis.PickerZ2,
                PickerAxis.PickerZ3
            };

            for (int i = 0; i < axes.Length; i++)
            {
                PickerAxis axis = axes[i];
                double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                ApplyPickerAxisPositionForSimulation(axis, target);
                if (axis == PickerAxis.PickerX ||
                    axis == PickerAxis.PickerY ||
                    axis == GetPickerTAxis(_colletIndex))
                    UpdateSimulatedPickerPosition(axis, target);
            }
        }

        private void ApplyPickerAxisPositionForSimulation(PickerAxis axis, double target)
        {
            if (!IsAxisRuntimeSimulationAllowed())
                return;

            BaseAxis axisObject = GetPickerAxis(axis);
            if (axisObject == null)
                return;

            axisObject.SetPosition(target);
        }

        private static bool IsPickerThetaAxis(PickerAxis axis)
        {
            return axis == PickerAxis.PickerT0 ||
                   axis == PickerAxis.PickerT1 ||
                   axis == PickerAxis.PickerT2 ||
                   axis == PickerAxis.PickerT3;
        }

        private static double ResolvePickerTPcHomeOffset(BaseAxis axis)
        {
            try
            {
                return axis != null && axis.Setup != null ? axis.Setup.HomeOffset : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private async Task<int> AcquireCalibrationAreaAsync(CancellationToken ct)
        {
            if (_inspectionAreaLease != null)
                return 0;

            _inspectionAreaLease = await AcquireResourceAsync(
                SequenceResourceKind.InspectionArea,
                Name + ":ColletCalibration",
                ct).ConfigureAwait(false);
            return _inspectionAreaLease != null ? 0 : -1;
        }

        private bool IsReferenceCollet()
        {
            return _colletNo == 4;
        }

        private ColletCalibrationRecord ResolveExistingColletCalibrationRecord()
        {
            try
            {
                if (Context == null ||
                    Context.Machine == null ||
                    Context.Machine.VisionUnit == null ||
                    Context.Machine.VisionUnit.Config == null ||
                    Context.Machine.VisionUnit.Config.CalibrationData == null ||
                    Context.Machine.VisionUnit.Config.CalibrationData.Collet == null)
                    return null;

                ColletCalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData.Collet;
                data.EnsureObjects();
                ColletCalibrationRecord record = data.GetRecord(_calibrationSide, _colletNo);
                return record != null && record.Valid ? record : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private bool IsReferenceColletCalibrationReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (Context == null || Context.Machine == null)
                {
                    reason = "Machine context가 없습니다.";
                    return false;
                }

                if (Context.Machine.VisionUnit == null ||
                    Context.Machine.VisionUnit.Config == null ||
                    Context.Machine.VisionUnit.Config.CalibrationData == null ||
                    Context.Machine.VisionUnit.Config.CalibrationData.Collet == null)
                {
                    reason = "Collet CalibrationData가 없습니다.";
                    return false;
                }

                ColletCalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData.Collet;
                data.EnsureObjects();
                ColletCalibrationRecord reference = data.GetRecord(_calibrationSide, 4);
                if (reference == null || !reference.Valid)
                {
                    reason = "4번 Collet Calibration 결과가 유효하지 않습니다.";
                    return false;
                }

                double bottomX = GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition");
                double bottomY = GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition");
                double dx = Math.Abs(reference.FinalPickerX - bottomX);
                double dy = Math.Abs(reference.FinalPickerY - bottomY);
                const double referenceTeachingToleranceMm = 0.001;
                if (dx > referenceTeachingToleranceMm || dy > referenceTeachingToleranceMm)
                {
                    reason =
                        "4번 Collet 최종 OK 위치와 현재 Bottom X/Y 티칭값이 다릅니다. " +
                        "referenceFinal=(" + reference.FinalPickerX.ToString("F6") + "," + reference.FinalPickerY.ToString("F6") + ")" +
                        ", bottomTeaching=(" + bottomX.ToString("F6") + "," + bottomY.ToString("F6") + ")" +
                        ", delta=(" + dx.ToString("F6") + "," + dy.ToString("F6") + ")" +
                        ", toleranceMm=" + referenceTeachingToleranceMm.ToString("F6") +
                        ". 4번 Collet Calibration을 먼저 완료해서 Bottom 기준 티칭을 저장하세요.";
                    return false;
                }

                reason = "OK";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private int SaveReferenceColletBottomTeachingIfNeeded(ColletCalibrationRecord target)
        {
            try
            {
                if (!IsReferenceCollet())
                    return 0;

                if (target == null || !target.Valid)
                    return Fail("COLLET-CAL-REFERENCE-NO-RESULT", Name,
                        "4번 Collet 기준 Bottom X/Y 티칭으로 저장할 유효한 Calibration 결과가 없습니다. side=" + _calibrationSide);

                if (Context == null || Context.Controller == null || Context.Machine == null)
                    return Fail("COLLET-CAL-REFERENCE-NO-CONTEXT", Name,
                        "4번 Collet 기준 Bottom X/Y 티칭을 저장할 Machine context가 없습니다. side=" + _calibrationSide);

                string recipeName = Context.Controller.ActiveRecipeName;
                if (string.IsNullOrWhiteSpace(recipeName))
                    return Fail("COLLET-CAL-REFERENCE-NO-RECIPE", Name,
                        "4번 Collet 기준 Bottom X/Y 티칭을 저장할 활성 Recipe가 없습니다. side=" + _calibrationSide);

                double oldBottomX = GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition");
                double oldBottomY = GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition");
                SetPickerBottomTeachingPosition(PickerAxis.PickerX, target.FinalPickerX);
                SetPickerBottomTeachingPosition(PickerAxis.PickerY, target.FinalPickerY);

                if (!Context.Machine.SaveRecipe(recipeName))
                    return Fail("COLLET-CAL-REFERENCE-RECIPE-SAVE", Name,
                        "4번 Collet 기준 Bottom X/Y 티칭 Recipe 저장에 실패했습니다. side=" + _calibrationSide +
                        ", recipe=" + recipeName);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalReferenceTeach",
                    "4번 Collet 최종 OK 위치를 Bottom 기준 X/Y 티칭으로 저장했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", oldBottom=(" + oldBottomX.ToString("F6") + "," + oldBottomY.ToString("F6") + ")" +
                    ", newBottom=(" + target.FinalPickerX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + ")" +
                    ", recipe=" + recipeName);

                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-REFERENCE-TEACH",
                    "4번 Collet 최종 OK 위치를 Bottom 기준 X/Y 티칭으로 저장했습니다. side=" + _calibrationSide +
                    ", oldBottom=(" + oldBottomX.ToString("F6") + "," + oldBottomY.ToString("F6") + ")" +
                    ", newBottom=(" + target.FinalPickerX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + ")" +
                    ", recipe=" + recipeName);

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-REFERENCE-TEACH-EX", Name,
                    "4번 Collet 기준 Bottom X/Y 티칭 저장 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private void SetPickerBottomTeachingPosition(PickerAxis axis, double position)
        {
            if (_calibrationSide == VisionFocusPickerSide.Front)
            {
                if (FrontPicker == null)
                    throw new InvalidOperationException("Front Picker Unit이 없습니다.");

                FrontPicker.SetPickerAxisTeachingPosition(axis, "BottomPosition", position);
                return;
            }

            if (RearPicker == null)
                throw new InvalidOperationException("Rear Picker Unit이 없습니다.");

            RearPicker.SetPickerAxisTeachingPosition(axis, "BottomPosition", position);
        }

        private static void CopyRecord(ColletCalibrationRecord source, ColletCalibrationRecord target)
        {
            if (source == null || target == null)
                return;

            target.Side = source.Side;
            target.ColletNo = source.ColletNo;
            target.CenterPixelX = source.CenterPixelX;
            target.CenterPixelY = source.CenterPixelY;
            target.CenterMmX = source.CenterMmX;
            target.CenterMmY = source.CenterMmY;
            target.OffsetX = source.OffsetX;
            target.OffsetY = source.OffsetY;
            target.ThetaOffset = source.ThetaOffset;
            target.TZeroHomeOffset = source.TZeroHomeOffset;
            target.MeasuredTPosition = source.MeasuredTPosition;
            target.FinalPickerX = source.FinalPickerX;
            target.FinalPickerY = source.FinalPickerY;
            target.FinalPickerZ = source.FinalPickerZ;
            target.FinalPickerT = source.FinalPickerT;
            target.Valid = source.Valid;
            target.UpdatedAt = source.UpdatedAt;
        }

        private void ReleaseCalibrationArea()
        {
            try
            {
                ReleasePickerWorkArea();
                if (_inspectionAreaLease != null)
                {
                    _inspectionAreaLease.Dispose();
                    _inspectionAreaLease = null;
                }
            }
            catch (Exception ex)
            {
                WriteLog("ColletCalibrationSequence", "Collet Calibration 리소스 해제 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
