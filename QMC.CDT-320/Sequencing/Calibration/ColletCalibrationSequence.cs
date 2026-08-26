using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
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
        private readonly bool? _runPostCalibrationPipelineOverride;
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
            : this(context, side, colletNo, null)
        {
        }

        public ColletCalibrationSequence(
            MachineSequenceContext context,
            VisionFocusPickerSide side,
            int colletNo,
            bool runPostCalibrationPipeline)
            : this(context, side, colletNo, (bool?)runPostCalibrationPipeline)
        {
        }

        private ColletCalibrationSequence(
            MachineSequenceContext context,
            VisionFocusPickerSide side,
            int colletNo,
            bool? runPostCalibrationPipelineOverride)
            : base(
                  context,
                  side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                  PickerSequenceKind.Inspect,
                  side == VisionFocusPickerSide.Front ? "FrontColletCalibrationSequence" : "RearColletCalibrationSequence")
        {
            _calibrationSide = side;
            _colletNo = colletNo < 1 ? 1 : colletNo > 4 ? 4 : colletNo;
            _colletIndex = _colletNo - 1;
            _runPostCalibrationPipelineOverride = runPostCalibrationPipelineOverride;
            CurrentStep = ColletCalibrationStep.CheckUnit;
        }

        public ColletCalibrationRecord ResultRecord { get; private set; }

        // Manual Collet Calibration Start에서만 사용한다. 상대 Picker를 자동으로
        // 공용 Avoid 위치로 이동하지 않고, 이미 전체 Avoid에 있는지 확인하여
        // 선택 Picker의 내부 안전 위치 흐름만 사용한다.
        internal bool RequireOppositePickerAlreadyAtAvoid { get; set; }

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

        private sealed class ColletInspectionZInfo
        {
            public double TeachingZ;
            public double DieThickness;
            public double FilmThickness;
            public double ColletOffset;
            public ColletShapeType ColletType;
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
                case ColletCalibrationStep.RunCocAndSideAutoFocus:
                    return RunCocAndSideAutoFocusAsync(ct);
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
                    ", moveTimeoutMs=" + _settings.Motion.MoveTimeoutMs +
                    ", speedScalePercent=" + QMC.Common.Motion.MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + QMC.Common.Motion.MotionSpeedScale.EffectiveScaleFactor.ToString("F6") +
                    ", explicitVelocityNotDefaultScaled=True");

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
                    ", z1Actual=" + (GetPickerAxis(PickerAxis.PickerZ0) != null ? GetPickerAxis(PickerAxis.PickerZ0).ActualPosition.ToString("F6") : "null") +
                    ", z2Actual=" + (GetPickerAxis(PickerAxis.PickerZ1) != null ? GetPickerAxis(PickerAxis.PickerZ1).ActualPosition.ToString("F6") : "null") +
                    ", z3Actual=" + (GetPickerAxis(PickerAxis.PickerZ2) != null ? GetPickerAxis(PickerAxis.PickerZ2).ActualPosition.ToString("F6") : "null") +
                    ", z4Actual=" + (GetPickerAxis(PickerAxis.PickerZ3) != null ? GetPickerAxis(PickerAxis.PickerZ3).ActualPosition.ToString("F6") : "null") +
                    ", z1Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ0, "AvoidPosition").ToString("F6") +
                    ", z2Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ1, "AvoidPosition").ToString("F6") +
                    ", z3Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ2, "AvoidPosition").ToString("F6") +
                    ", z4Avoid=" + GetPickerTeachingPosition(PickerAxis.PickerZ3, "AvoidPosition").ToString("F6"));

                result = await MoveCurrentPickerZAndYToAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureInputOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // VisionX Avoid는 상대 픽커 이동 "전"에 확보되어야 하며(간섭 방지), 위 1회 호출로 충분하다.
                // Collet Cal의 상대 Picker 안전위치는 공용 전체 AVOID가 아니라 Output-side Avoid(OutSide-Avoid)다.
                // 이미 Output-side Avoid면 헬퍼가 재이동 없이 통과하므로 공용 AVOID↔OutSide-Avoid 왕복이 생기지 않는다.
                result = await MoveOppositePickerToOutsideForStartAsync(ct).ConfigureAwait(false);
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
                // 접근(X/Y) 이동은 안전이동 속도(SafeMovePercent)로 수행한다. 측정 속도는 Z 하강부터 적용.
                result = await MovePickerXTThenYAndVerifyAsync(
                    xyTargets,
                    "Collet Calibration Bottom X/Y",
                    ct,
                    BottomFinderTargetName,
                    true,
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
                    true,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;
                ApplyPickerAxisPositionForSimulation(GetPickerTAxis(_colletIndex), _basePickerT);
                UpdateSimulatedPickerPosition(GetPickerTAxis(_colletIndex), _basePickerT);

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerZAxis(_colletIndex),
                    _targetPickerZ,
                    // [안전이동 적용 2026-08-06] Bottom 카메라 초점 높이로 가는 위치 이동이다.
                    // Collet 1:1 캘의 측정은 Bottom 비전이 담당하고 Z 탐색 스트로크가 없으므로
                    // 이 파일의 Z/Y 이동은 전부 안전이동 대상이다(T축은 결정에 따라 현재 유지).
                    "Collet Calibration Bottom Z",
                    ct,
                    BottomFinderTargetName,
                    true,
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

                // 시작 안전이동은 forceMove를 쓰지 않는다: 이미 Avoid(정지+무알람+톨러런스)면 확인만 하고 통과한다.
                // (CanSkipPickerMoveCommand — 이동 중/알람/서보OFF면 스킵되지 않는 fail-closed 판정)
                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Collet Calibration 시작 전 선택 Picker Z축 Avoid",
                    ct).ConfigureAwait(false);
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
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ApplyPickerAxisPositionForSimulation(PickerAxis.PickerY, avoidY);
                UpdateSimulatedPickerPosition(PickerAxis.PickerY, avoidY);

                result = await MoveAllPickerTToAvoidAndVerifyAsync(
                    "Collet Calibration 시작 전 선택 Picker T축 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 선택 Picker Z/Y/T 안전 위치 완료. side=" + _calibrationSide +
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

        private int VerifyOppositePickerAlreadyAtAvoidForStart()
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                {
                    if (RearPicker == null || (RearPicker.Config != null && !RearPicker.Config.UseUnit))
                        return 0;

                    return VerifyPickerAlreadyAtAvoidForStart(
                        "RearPickerUnit",
                        RearPicker.IsRearPickerInAvoidPosition(),
                        RearPicker.Axes.Values);
                }

                if (FrontPicker == null || (FrontPicker.Config != null && !FrontPicker.Config.UseUnit))
                    return 0;

                return VerifyPickerAlreadyAtAvoidForStart(
                    "FrontPickerUnit",
                    FrontPicker.IsFrontPickerInAvoidPosition(),
                    FrontPicker.Axes.Values);
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-OPPOSITE-AVOID-CHECK-EX", Name,
                    "Collet Calibration 시작 전 상대 Picker Avoid 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyPickerAlreadyAtAvoidForStart(
            string pickerUnitName,
            bool isAtAvoid,
            IEnumerable<BaseAxis> axes)
        {
            if (!isAtAvoid)
            {
                return Fail("COLLET-CAL-OPPOSITE-AVOID-REQUIRED", pickerUnitName,
                    "Collet Calibration 내부 안전 위치 모드에서는 상대 Picker를 자동 이동하지 않습니다. " +
                    "상대 Picker X/Y/Z/T 전체를 Avoid 위치로 이동한 뒤 다시 시작하세요.");
            }

            if (axes != null)
            {
                foreach (BaseAxis axis in axes)
                {
                    if (axis == null)
                        continue;

                    if (!axis.IsServoOn || axis.IsAlarm || axis.IsMoving || !axis.IsInPosition)
                    {
                        return Fail("COLLET-CAL-OPPOSITE-AXIS-NOT-READY", pickerUnitName,
                            "Collet Calibration 시작 전 상대 Picker 축이 안전 완료 상태가 아닙니다. axis=" +
                            axis.Name + ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                            ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                            ", moving=" + (axis.IsMoving ? "Y" : "N") +
                            ", inPosition=" + (axis.IsInPosition ? "Y" : "N"));
                    }
                }
            }

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                "Collet Calibration 내부 안전 위치 모드: 상대 Picker 자동 Avoid 이동을 생략하고 " +
                "기존 Avoid 상태를 확인했습니다. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", opposite=" + pickerUnitName);
            return 0;
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

                double target = stage.Recipe.VisionX.AvoidPosition;
                if (stage.CameraX.IsAtTargetPosition(target, 0.0))
                    return 0;

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 InputVisionX Avoid 이동. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", actual=" + stage.CameraX.ActualPosition.ToString("F6") +
                    ", target=" + target.ToString("F6") +
                    ", velocity=" + motion.MoveVelocity.ToString("F6") +
                    ", acceleration=" + motion.MoveAcceleration.ToString("F6") +
                    ", deceleration=" + motion.MoveDeceleration.ToString("F6") +
                    ", timeoutMs=" + motion.MoveTimeoutMs +
                    ", speedScalePercent=" + QMC.Common.Motion.MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + QMC.Common.Motion.MotionSpeedScale.EffectiveScaleFactor.ToString("F6"));

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
                if (stage == null ||
                    stage.OutputCameraX == null ||
                    stage.Recipe == null ||
                    stage.Recipe.VisionX == null)
                    return Fail("COLLET-CAL-OUTPUT-CAMERA-MISSING", "OutputStageUnit",
                        "Collet Calibration 시작 전 OutputVisionX Avoid 이동에 필요한 축이 없습니다.");

                stage.Recipe.EnsurePositionObjects();
                double target = stage.Recipe.VisionX.AvoidPosition;
                if (stage.OutputCameraX.IsAtTargetPosition(target, 0.0))
                    return 0;

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration 시작 전 OutputVisionX Avoid 이동. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", actual=" + stage.OutputCameraX.ActualPosition.ToString("F6") +
                    ", velocity=" + motion.MoveVelocity.ToString("F6") +
                    ", acceleration=" + motion.MoveAcceleration.ToString("F6") +
                    ", deceleration=" + motion.MoveDeceleration.ToString("F6") +
                    ", timeoutMs=" + motion.MoveTimeoutMs +
                    ", speedScalePercent=" + QMC.Common.Motion.MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + QMC.Common.Motion.MotionSpeedScale.EffectiveScaleFactor.ToString("F6"));

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

                // 이미 Output-side Avoid면 재이동하지 않고 그대로 대기(공용 AVOID로 갔다가 다시 오는 왕복 방지).
                if (FrontPicker.IsPickerInOutputSideAvoidPosition())
                {
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                        "Collet Calibration start FrontPicker already at Output-side Avoid. move skipped. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo + " - Ok");
                    return 0;
                }

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
                double safePercent = ResolveCalibrationSafeMovePercent();
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration start FrontPicker Outside(Output-side Avoid) move for opposite picker. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", safeMovePercent=" + safePercent.ToString("F3") +
                    ", fallbackVelocity=" + motion.MoveVelocity.ToString("F6") +
                    ", note=axis Config.Default x SafeMovePercent applied to velocity/acceleration/deceleration");

                int result = await FrontPicker.MoveToOutputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity).ConfigureAwait(false);
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

                // 이미 Output-side Avoid면 재이동하지 않고 그대로 대기(공용 AVOID로 갔다가 다시 오는 왕복 방지).
                if (RearPicker.IsPickerInOutputSideAvoidPosition())
                {
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                        "Collet Calibration start RearPicker already at Output-side Avoid. move skipped. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo + " - Ok");
                    return 0;
                }

                CalibrationMotionSettings motion = ResolveCalibrationMotion();
                // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
                double safePercent = ResolveCalibrationSafeMovePercent();
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalStartSafe",
                    "Collet Calibration start RearPicker Outside(Output-side Avoid) move for opposite picker. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", safeMovePercent=" + safePercent.ToString("F3") +
                    ", fallbackVelocity=" + motion.MoveVelocity.ToString("F6") +
                    ", note=axis Config.Default x SafeMovePercent applied to velocity/acceleration/deceleration");

                int result = await RearPicker.MoveToOutputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity).ConfigureAwait(false);
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
                    double target = CalculateThetaMoveTarget(actual, theta);
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

                    double target = CalculateThetaMoveTarget(actual, theta);
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalTheta",
                        label + " 이동. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", iteration=" + i +
                        ", formula=targetT=actualT-theta*gain=" + actual.ToString("F6") + "-" + theta.ToString("F6") + "*" + _settings.ThetaMoveGain.ToString("F6") +
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

        private double CalculateThetaMoveTarget(double actualT, double thetaDeg)
        {
            return actualT - thetaDeg * _settings.ThetaMoveGain;
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

                    double targetX = actualX - offsetMmX * _settings.XyMoveGainX;
                    double targetY = actualY - offsetMmY * _settings.XyMoveGainY;
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalXy",
                        label + " 이동. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", iteration=" + i +
                        ", pixel=(" + match.X.ToString("F3") + "," + match.Y.ToString("F3") + ")" +
                        ", center=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")" +
                        ", scale=(" + camera.PixelToMmX.ToString("F9") + "," + camera.PixelToMmY.ToString("F9") + ")" +
                        ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                        ", formulaX=targetX=actualX-offsetMmX*gainX=" + actualX.ToString("F6") + "-" + offsetMmX.ToString("F6") + "*" + _settings.XyMoveGainX.ToString("F6") + "=" + targetX.ToString("F6") +
                        ", formulaY=targetY=actualY+offsetMmY*gainY=" + actualY.ToString("F6") + "-" + offsetMmY.ToString("F6") + "*" + _settings.XyMoveGainY.ToString("F6") + "=" + targetY.ToString("F6"));

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
                    true,
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
                    true,
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
                    true,
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
                    true,
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
                    ", formulaCenterMmY=(centerY-pixelY)*scaleY=(" + camera.ImageCenterPixelY.ToString("F3") + "-" + _finalMatch.Y.ToString("F3") + ")*" + camera.PixelToMmY.ToString("F9") + "=" + centerMmY.ToString("F6") +
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

        // 콜렛 AF 절대 산식(승인 2026-07-29): PickPosition = AF BestZ + ColletOffset(Rim/Flat) + DieThickness + BottomToPickMm.
        // Film은 Pick 산식에 포함하지 않는다(검사티칭Z 산식과 다름). 메모리 변이만 — 파일 영속은
        // SaveColletCalibration의 티칭 동기화 SaveRecipe에 원자적으로 실린다. 한계 초과는 fail-closed(공용 메서드).
        private int ComputeAndApplyAfDerivedPickZ(ColletCalibrationRecord target, out double previousPickZ, out bool pickApplied)
        {
            previousPickZ = 0.0;
            pickApplied = false;
            try
            {
                string recipeName = Context != null && Context.Controller != null ? Context.Controller.ActiveRecipeName : null;
                if (string.IsNullOrWhiteSpace(recipeName))
                {
                    QMC.Common.Log.Write("Calibration", "SYSTEM", "AfProcessZ",
                        "활성 Recipe가 없어 콜렛 AF 기반 PickPosition을 갱신하지 않습니다. side=" + _calibrationSide + ", colletNo=" + _colletNo);
                    return 0;
                }

                ColletInspectionZInfo zInfo = ResolveColletInspectionTeachingZ(recipeName, target.FinalPickerZ);
                double bottomToPickMm = ResolveBottomToPickMm();
                double newPickZ = target.FinalPickerZ + zInfo.ColletOffset + zInfo.DieThickness + bottomToPickMm;

                return ApplyAfDerivedZTeaching(
                    _colletIndex,
                    "PickPosition",
                    newPickZ,
                    "formulaPickZ=afBestZ+colletOffset+dieThickness+bottomToPick=" +
                    target.FinalPickerZ.ToString("F6") + "+" + zInfo.ColletOffset.ToString("F6") + "+" +
                    zInfo.DieThickness.ToString("F6") + "+" + bottomToPickMm.ToString("F6") + "=" + newPickZ.ToString("F6") +
                    ", colletType=" + zInfo.ColletType +
                    ", filmThickness=" + zInfo.FilmThickness.ToString("F6") + "(Pick 산식 미포함)" +
                    ", recipe=" + recipeName,
                    "ColletCalibrationAF",
                    out previousPickZ,
                    out pickApplied);
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-AF-PICKZ-EX", Name,
                    "콜렛 AF 기반 PickPosition 계산/적용 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo + ", error=" + ex.Message);
            }
        }

        // 티칭 동기화 실패 롤백용 스냅샷: SaveReferenceColletBottomTeachingIfNeeded /
        // SaveColletInspectionZTeachingIfNeeded / ComputeAndApplyAfDerivedPickZ 가 변이시키는 티칭 전체
        // (기준/비기준 슈퍼셋 14개 + PickPosition)를 캡처한다. 동기화가 티칭 변이 후 SaveRecipe에 실패해도
        // 메모리를 캘 이전 정합 상태로 되돌려 "신 PickZ + 구 검사티칭" 부정합이 남는 것을 막는다.
        private double[] CaptureColletTeachingSnapshot()
        {
            PickerAxis zAxis = GetPickerZAxis(_colletIndex);
            string dieBottom = BuildIndexedPositionName("DieBottomPosition");
            string dieSide = BuildIndexedPositionName("DieSidePosition");
            string diePick = BuildIndexedPositionName("DiePickPosition");
            string diePlace = BuildIndexedPositionName("DiePlacePosition");
            return new[]
            {
                GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition"),
                GetPickerTeachingPosition(PickerAxis.PickerX, "SidePosition"),
                GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition"),
                GetPickerTeachingPosition(PickerAxis.PickerY, "SidePosition"),
                GetPickerTeachingPosition(zAxis, "BottomPosition"),
                GetPickerTeachingPosition(zAxis, "SidePosition"),
                GetPickerTeachingPosition(PickerAxis.PickerX, dieBottom),
                GetPickerTeachingPosition(PickerAxis.PickerX, dieSide),
                GetPickerTeachingPosition(PickerAxis.PickerY, diePick),
                GetPickerTeachingPosition(PickerAxis.PickerY, dieBottom),
                GetPickerTeachingPosition(PickerAxis.PickerY, dieSide),
                GetPickerTeachingPosition(PickerAxis.PickerY, diePlace),
                GetPickerTeachingPosition(zAxis, dieBottom),
                GetPickerTeachingPosition(zAxis, dieSide),
                GetPickerTeachingPosition(zAxis, "PickPosition")
            };
        }

        private void RestoreColletTeachingSnapshot(double[] snapshot)
        {
            if (snapshot == null || snapshot.Length < 15)
                return;

            try
            {
                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                SetPickerBottomTeachingPosition(PickerAxis.PickerX, snapshot[0]);
                SetPickerSideTeachingPosition(PickerAxis.PickerX, snapshot[1]);
                SetPickerBottomTeachingPosition(PickerAxis.PickerY, snapshot[2]);
                SetPickerSideTeachingPosition(PickerAxis.PickerY, snapshot[3]);
                SetPickerBottomTeachingPosition(zAxis, snapshot[4]);
                SetPickerSideTeachingPosition(zAxis, snapshot[5]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerX, "DieBottomPosition", snapshot[6]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerX, "DieSidePosition", snapshot[7]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DiePickPosition", snapshot[8]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DieBottomPosition", snapshot[9]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DieSidePosition", snapshot[10]);
                SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DiePlacePosition", snapshot[11]);
                SetPickerIndexedTeachingPosition(zAxis, "DieBottomPosition", snapshot[12]);
                SetPickerIndexedTeachingPosition(zAxis, "DieSidePosition", snapshot[13]);
                SetPickerPickTeachingPosition(zAxis, snapshot[14]);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "AfProcessZ",
                    "티칭 동기화 실패로 콜렛 티칭 스냅샷을 원복했습니다(메모리=캘 이전 상태). side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", bottomZRestored=" + snapshot[4].ToString("F6") +
                    ", sideZRestored=" + snapshot[5].ToString("F6") +
                    ", pickZRestored=" + snapshot[14].ToString("F6"));
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Calibration", "SYSTEM", "AfProcessZ",
                    "콜렛 티칭 스냅샷 원복 중 예외. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo + ", error=" + ex.Message);
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
                // 콜렛 AF 기반 PickPosition 갱신(메모리 변이만) — 파일 영속은 아래 티칭 동기화 SaveRecipe와
                // 원자적으로 수행한다. 스냅샷(PickPosition 포함)을 갱신 "전"에 떠서 후속 티칭 동기화 실패 시
                // "신 PickZ + 구 검사티칭" 부정합 없이 전체를 캘 이전 상태로 원복한다.
                double[] teachingSnapshot = CaptureColletTeachingSnapshot();
                double afPreviousPickZ;
                bool afPickApplied;
                int afPickZResult = ComputeAndApplyAfDerivedPickZ(target, out afPreviousPickZ, out afPickApplied);
                if (afPickZResult != 0)
                    return afPickZResult;
                int referenceTeachingResult = SaveReferenceColletBottomTeachingIfNeeded(target);
                if (referenceTeachingResult != 0)
                {
                    RestoreColletTeachingSnapshot(teachingSnapshot);
                    return referenceTeachingResult;
                }
                int inspectionZTeachingResult = SaveColletInspectionZTeachingIfNeeded(target);
                if (inspectionZTeachingResult != 0)
                {
                    RestoreColletTeachingSnapshot(teachingSnapshot);
                    return inspectionZTeachingResult;
                }
                // PickPosition 영속 보장: 콜렛 1~3은 inspection 동기화, 콜렛 4는 reference 동기화가 각각 SaveRecipe로
                // 전체 레시피(PickPosition 포함)를 이미 영속했다. 이 저장은 동기화가 생략된 경로 대비 안전망이며,
                // 실패해도 롤백하지 않는다 — 파일이 이미 신 상태(티칭 동기화 저장)일 수 있어 롤백하면
                // 올바른 영속 상태를 구 값으로 후퇴시킨다. 메모리는 일관된 신 상태로 유지하고 실패만 알린다.
                if (afPickApplied)
                {
                    string activeRecipeName = Context.Controller != null ? Context.Controller.ActiveRecipeName : null;
                    if (!string.IsNullOrWhiteSpace(activeRecipeName) && !Context.Machine.SaveRecipe(activeRecipeName))
                    {
                        return Fail("COLLET-CAL-AF-PICKZ-SAVE", Name,
                            "콜렛 AF 기반 PickPosition Recipe 저장에 실패했습니다(메모리 상태는 신값 유지). side=" + _calibrationSide +
                            ", colletNo=" + _colletNo + ", recipe=" + activeRecipeName);
                    }
                }

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

                bool runPostCalibrationPipeline = _runPostCalibrationPipelineOverride ??
                                                  (_settings != null && _settings.RunAutoFocusAfterTheta);
                CurrentStep = runPostCalibrationPipeline
                    ? ColletCalibrationStep.RunCocAndSideAutoFocus
                    : ColletCalibrationStep.Complete;
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

        private async Task<int> RunCocAndSideAutoFocusAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_settings == null || !_settings.RunAutoFocusAfterTheta)
                {
                    CurrentStep = ColletCalibrationStep.Complete;
                    return 0;
                }

                // COC 하위 시퀀스가 동일 검사영역 리소스를 사용하므로 Collet Cal 점유를 먼저 해제한다.
                ReleaseCalibrationArea();

                WriteLog("ColletCalibrationSequence",
                    Name + " Bottom AF/Collet 보정 완료 후 COC 및 Side AF를 시작합니다. side=" +
                    _calibrationSide + ", colletNo=" + _colletNo + " - Start");

                var coc = new ColletRotationCenterCalibrationSequence(
                    Context,
                    _calibrationSide,
                    _colletNo,
                    false);
                int result = await coc.RunAsync(
                    ct,
                    Options ?? PickerSequenceOptions.Default()).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-COC", Name,
                        "Collet Calibration 후 COC 회전 중심 검출에 실패했습니다. side=" +
                        _calibrationSide + ", colletNo=" + _colletNo + ", result=" + result);

                // COC 하위 시퀀스가 해제한 Bottom 작업영역을 다시 점유한 뒤 회전 중심 FineAlign 이동을 수행합니다.
                result = await AcquireCalibrationAreaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-COC-AREA", Name,
                        "COC 완료 후 회전 중심 이동을 위한 InspectionArea 재점유에 실패했습니다. side=" +
                        _calibrationSide + ", colletNo=" + _colletNo);
                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "ColletCalibration");

                // 회전 중심이 현재 위치에서 FineAlign 허용 이동량을 넘으면 이상치로 판정한다.
                // (이 이동은 Z 하강 상태에서 수행되므로 MotionGuard가 어차피 차단한다 —
                //  레시피에 이상 COC 값이 먼저 저장되는 것을 막기 위해 저장 전에 확인한다.)
                BaseAxis cocXAxis = GetPickerAxis(PickerAxis.PickerX);
                BaseAxis cocYAxis = GetPickerAxis(PickerAxis.PickerY);
                if (cocXAxis != null && cocYAxis != null &&
                    !IsFineAlignXyMove(
                        cocXAxis.ActualPosition,
                        cocYAxis.ActualPosition,
                        coc.RotationCenterMachineX,
                        coc.RotationCenterMachineY))
                {
                    return Fail("COLLET-CAL-COC-CENTER-RANGE", Name,
                        "COC 회전 중심이 현재 위치에서 FineAlign 허용 범위를 초과해 이상치로 판정했습니다(레시피 미저장). side=" +
                        _calibrationSide + ", colletNo=" + _colletNo +
                        ", center=(" + coc.RotationCenterMachineX.ToString("F6") + "," +
                        coc.RotationCenterMachineY.ToString("F6") + ")" +
                        ", actual=(" + cocXAxis.ActualPosition.ToString("F6") + "," +
                        cocYAxis.ActualPosition.ToString("F6") + ")" +
                        ", fineAlignMaxMm=" + (_settings != null ? _settings.FineAlignMaxXyMoveMm.ToString("F6") : "0.200000"));
                }

                result = SaveAndApplyRotationCenter(coc.RotationCenterMachineX, coc.RotationCenterMachineY);
                if (result != 0)
                    return result;

                var centerTargets = new Dictionary<PickerAxis, double>
                {
                    { PickerAxis.PickerX, coc.RotationCenterMachineX },
                    { PickerAxis.PickerY, coc.RotationCenterMachineY }
                };
                result = await MovePickerXTThenYAndVerifyAsync(
                    centerTargets,
                    "COC 회전 중심 XY 적용",
                    ct,
                    FineAlignTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerTAxis(_colletIndex),
                    ResultRecord.FinalPickerT,
                    "COC 후 Bottom T 기준 복귀",
                    ct,
                    BottomFinderTargetName,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                // Side AutoFocus는 파라미터(RunSideAutoFocusAfterCoc)로 on/off. COC(회전중심)는 위에서 이미 수행/적용됐고,
                // 이 값이 false면 Side 0°/90° AutoFocus만 건너뛴다.
                bool runSideAf = _settings != null && _settings.RunSideAutoFocusAfterCoc;
                string sideFocusLog = "sideAutoFocus=skipped";
                if (runSideAf)
                {
                    // 현재 기준: Side 초점 보정은 Bottom 재측정 값을 쓰지 않고 COC(회전 중심 편차) + 레시피 다이 사이즈로만 계산한다.
                    // (Bottom 검사 결과가 Side 위치에 영향을 주지 않도록 하는 정책과 동일 계약)
                    SideFocusCorrection correction;
                    if (!TryBuildSideFocusCorrectionFromCoc(out correction))
                        return Fail("COLLET-CAL-SIDE-AF-CORRECTION", Name,
                            "COC/Die Size로 Side Focus 보정량을 계산하지 못했습니다. side=" +
                            _calibrationSide + ", colletNo=" + _colletNo);

                    result = await RunSideAutoFocusAsync(0, correction.Focus0, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await RunSideAutoFocusAsync(90, correction.Focus90, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MovePickerAxisAndVerifyAsync(
                        GetPickerTAxis(_colletIndex),
                        ResultRecord.FinalPickerT,
                        "Side AF 후 Bottom T 복귀",
                        ct,
                        BottomFinderTargetName,
                        true).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MovePickerAxisAndVerifyAsync(
                        GetPickerZAxis(_colletIndex),
                        ResultRecord.FinalPickerZ,
                        "Side AF 후 Bottom Z 복귀",
                        ct,
                        BottomFinderTargetName,
                        true,
                        true).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    sideFocusLog = "focusCorrection0=" + correction.Focus0.ToString("F6") +
                        ", focusCorrection90=" + correction.Focus90.ToString("F6");
                }
                else
                {
                    WriteLog("ColletCalibrationSequence",
                        Name + " Side AutoFocus 파라미터 off로 Side 0/90 AutoFocus를 건너뜁니다. side=" +
                        _calibrationSide + ", colletNo=" + _colletNo + " - Check");
                }

                if (!Context.Machine.VisionUnit.SaveSettings())
                    return Fail("COLLET-CAL-SIDE-AF-SAVE", Name,
                        "COC 및 Side AutoFocus 결과를 설정 파일에 저장하지 못했습니다. side=" +
                        _calibrationSide + ", colletNo=" + _colletNo);

                CurrentStep = ColletCalibrationStep.Complete;
                WriteLog("ColletCalibrationSequence",
                    Name + " COC 적용" + (runSideAf ? " 및 Side 0/90 AutoFocus" : "(Side AF 생략)") + " 완료. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", center=(" + coc.RotationCenterMachineX.ToString("F6") + "," +
                    coc.RotationCenterMachineY.ToString("F6") + ")" +
                    ", " + sideFocusLog + " - Ok");
                // 의도된 잔류: 완료 후 픽커를 Bottom 측정 위치(Z 하강)에 그대로 둔다.
                // 기준콜렛 검증(IsReferenceColletCalibrationReady)이 최종 OK 위치=티칭 일치를 요구하기 때문이다.
                WriteLog("ColletCalibrationSequence",
                    Name + " 완료 후 픽커를 Bottom 측정 위치(Z 하강)에 의도적으로 잔류시킵니다. " +
                    "후속 수동 조작 전 Z Avoid 복귀를 먼저 수행하세요. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo + " - Check");
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
                return Fail("COLLET-CAL-COC-SIDE-AF-EX", Name,
                    "COC 및 Side AutoFocus 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo + ", error=" + ex.Message);
            }
        }

        private int SaveAndApplyRotationCenter(double centerX, double centerY)
        {
            if (Context == null || Context.Machine == null || Context.Controller == null)
                return Fail("COLLET-CAL-COC-NO-CONTEXT", Name, "COC 회전 중심을 저장할 장비 Context가 없습니다.");

            if (_calibrationSide == VisionFocusPickerSide.Front)
            {
                FrontPicker.Recipe.EnsurePositionObjects();
                FrontPicker.Config.ColletRotationCenterX[_colletIndex] = centerX;
                FrontPicker.Config.ColletRotationCenterY[_colletIndex] = centerY;
                FrontPicker.Config.ColletRotationCenterValid[_colletIndex] = true;
            }
            else
            {
                RearPicker.Recipe.EnsurePositionObjects();
                RearPicker.Config.ColletRotationCenterX[_colletIndex] = centerX;
                RearPicker.Config.ColletRotationCenterY[_colletIndex] = centerY;
                RearPicker.Config.ColletRotationCenterValid[_colletIndex] = true;
            }

            string recipeName = Context.Controller.ActiveRecipeName;
            if (string.IsNullOrWhiteSpace(recipeName) || !Context.Machine.SaveRecipe(recipeName))
                return Fail("COLLET-CAL-COC-RECIPE-SAVE", Name,
                    "COC 회전 중심 Recipe 저장에 실패했습니다. recipe=" + (recipeName ?? string.Empty));

            // 회전중심 실사용 저장소는 픽커 유닛 Config인데 SaveRecipe는 Recipe만 영속한다 —
            // 앱 재시작 직후 소비자(Side 90° Y 절대식, 바텀 Offset 촬영각 프레임 변환)가 구값을
            // 읽지 않도록 장비 설정도 함께 저장한다(2026-08-25 팀장님 지시).
            if (!Context.Machine.SaveSettings())
                return Fail("COLLET-CAL-COC-SETTINGS-SAVE", Name,
                    "COC 회전 중심 장비 설정(SaveSettings) 저장에 실패했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo);

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalCocCenter",
                "Collet Calibration COC 회전 중심 저장/적용. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", centerX=" + centerX.ToString("F6") +
                ", centerY=" + centerY.ToString("F6") +
                ", recipe=" + recipeName +
                ", settingsSaved=true");
            return 0;
        }

        private async Task<BottomVisionOffset> InspectBottomDieForSideFocusAsync(CancellationToken ct)
        {
            DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, _colletNo);
            if (die == null && !IsPickerSimulationOrDryRun())
                return null;

            int fb = _calibrationSide == VisionFocusPickerSide.Front ? 0 : 1;
            int dieIndex = die != null ? die.InputSequenceNo : _colletNo;
            int gridX = die != null ? die.Wafer_IndexX : 0;
            int gridY = die != null ? die.Wafer_IndexY : 0;
            string dieId = die != null ? die.DieId : "SIM-C" + _colletNo;
            VisionDieAddressStore.Set(
                fb,
                _colletNo,
                dieIndex,
                gridX,
                gridY,
                dieId,
                die != null ? die.WaferID_Input : string.Empty);

            int timeoutMs = _settings != null ? _settings.VisionTimeoutMs : 5000;
            bool started = _calibrationSide == VisionFocusPickerSide.Front
                ? await FrontPicker.StartBottomInspectionAsync(_colletNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.StartBottomInspectionAsync(_colletNo, timeoutMs, ct).ConfigureAwait(false);
            if (!started)
                return null;

            BottomVisionOffset result = _calibrationSide == VisionFocusPickerSide.Front
                ? await FrontPicker.WaitBottomInspectionResultAsync(_colletNo, timeoutMs, ct).ConfigureAwait(false)
                : await RearPicker.WaitBottomInspectionResultAsync(_colletNo, timeoutMs, ct).ConfigureAwait(false);

            WriteLog("ColletCalibrationSequence",
                Name + " Side AF용 Bottom Die 결과 수신. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", die=" + dieId +
                ", ok=" + (result != null && result.IsOk) +
                ", bottomCenterValid=" + (result != null && result.HasBottomCenterOffset) +
                ", bottomCenter=(" + (result != null ? result.BottomCenterOffsetX.ToString("F6") : "null") + "," +
                (result != null ? result.BottomCenterOffsetY.ToString("F6") : "null") + ") - Check");
            return result;
        }

        private sealed class SideFocusCorrection
        {
            public double Focus0;
            public double Focus90;
        }

        private bool TryBuildSideFocusCorrectionFromCoc(out SideFocusCorrection correction)
        {
            correction = null;
            try
            {
                if (ResultRecord == null || !ResultRecord.RotationCenterValid)
                    return false;

                VisionFocusCalibrationData focusData = Context.Machine.VisionUnit.Config.FocusCalibration;
                focusData.EnsureObjects();

                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                    Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                    AutoVisionChannel.BottomInspection);
                // COC 편차(mm): 회전 중심이 콜렛(영상 기준 중심) 대비 얼마나 밀려 있는지.
                double cocXmm = camera.PixelToMmOffsetX(ResultRecord.RotationCenterPixelX);
                double cocYmm = camera.PixelToMmOffsetY(ResultRecord.RotationCenterPixelY);
                if (double.IsNaN(cocXmm) || double.IsInfinity(cocXmm) ||
                    double.IsNaN(cocYmm) || double.IsInfinity(cocYmm))
                    return false;

                // 다이 사이즈는 레시피(Input Frame) 값을 우선 사용한다. 없으면 Controller 기본값 폴백.
                double dieSizeX = 0.0;
                double dieSizeY = 0.0;
                string dieSizeSource = "None";
                QMC.CDT320.Recipes.RecipeProject recipe = QMC.CDT320.Recipes.RecipeStore.LoadLastOrDefault();
                QMC.CDT320.Recipes.TapeFrameSubset frame = recipe != null
                    ? (recipe.InputFrame ?? recipe.Frame)
                    : null;
                if (frame != null && frame.DieSizeX > 0.0 && frame.DieSizeY > 0.0)
                {
                    dieSizeX = frame.DieSizeX;
                    dieSizeY = frame.DieSizeY;
                    dieSizeSource = "RecipeInputFrame";
                }
                else if (Context.Controller != null)
                {
                    dieSizeX = Context.Controller.DieSizeXMm;
                    dieSizeY = Context.Controller.DieSizeYMm;
                    dieSizeSource = "ControllerDefault";
                }
                if (dieSizeX <= 0.0 || dieSizeY <= 0.0)
                    return false;
                // 90도 회전 시 촬영면-중심 거리 변화량: (가로-세로)/2.
                double sizeTerm90 = (dieSizeX - dieSizeY) / 2.0;

                bool front = _calibrationSide == VisionFocusPickerSide.Front;
                double size90Sign = front ? focusData.SideFocusSize90SignFront : focusData.SideFocusSize90SignRear;
                double coc0Sign = front ? focusData.SideFocusCoc0SignFront : focusData.SideFocusCoc0SignRear;
                double coc90Sign = front ? focusData.SideFocusCoc90SignFront : focusData.SideFocusCoc90SignRear;

                // 0도: COC의 카메라축 성분(cocY)만 영향. 90도: 회전 매트릭스로 cocX가 카메라축으로 오고 다이 사이즈 항이 추가.
                correction = new SideFocusCorrection
                {
                    Focus0 = coc0Sign * cocYmm,
                    Focus90 = size90Sign * sizeTerm90 + coc90Sign * cocXmm
                };

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSideFocusFormula",
                    "Side AF 보정 계산(COC/DieSize). side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", cocOffsetMm=(" + cocXmm.ToString("F6") + "," + cocYmm.ToString("F6") + ")" +
                    ", dieSize=(" + dieSizeX.ToString("F6") + "," + dieSizeY.ToString("F6") + ", source=" + dieSizeSource + ")" +
                    ", sizeTerm90=(X-Y)/2=" + sizeTerm90.ToString("F6") +
                    ", signs(size90=" + size90Sign.ToString("F1") +
                    ", coc0=" + coc0Sign.ToString("F1") +
                    ", coc90=" + coc90Sign.ToString("F1") + ")" +
                    ", focus0=" + correction.Focus0.ToString("F6") +
                    ", focus90=" + correction.Focus90.ToString("F6"));
                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSideFocusFormula",
                    "Side AF 보정 계산(COC/DieSize) 예외. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", error=" + ex.Message);
                correction = null;
                return false;
            }
        }

        private bool TryBuildSideFocusCorrection(BottomVisionOffset bottom, out SideFocusCorrection correction)
        {
            correction = null;
            if (bottom == null || ResultRecord == null || !ResultRecord.RotationCenterValid)
                return false;

            double width;
            double height;
            if (!TryReadBottomValue(bottom, out width, "bottom_width_mm", "bottom_item_width") ||
                !TryReadBottomValue(bottom, out height, "bottom_height_mm", "bottom_item_height"))
                return false;

            if (!bottom.HasBottomCenterOffset ||
                double.IsNaN(bottom.BottomCenterOffsetX) || double.IsInfinity(bottom.BottomCenterOffsetX) ||
                double.IsNaN(bottom.BottomCenterOffsetY) || double.IsInfinity(bottom.BottomCenterOffsetY))
                return false;

            double referenceWidth = Context.Controller != null && Context.Controller.DieSizeXMm > 0.0
                ? Context.Controller.DieSizeXMm
                : width;
            double referenceHeight = Context.Controller != null && Context.Controller.DieSizeYMm > 0.0
                ? Context.Controller.DieSizeYMm
                : height;
            double normalError = Math.Abs(width - referenceWidth) + Math.Abs(height - referenceHeight);
            double swappedError = Math.Abs(width - referenceHeight) + Math.Abs(height - referenceWidth);
            if (swappedError < normalError)
            {
                double swap = width;
                width = height;
                height = swap;
            }

            VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                Context.Machine.VisionUnit.Config.CalibrationData.Camera,
                AutoVisionChannel.BottomInspection);
            double cocResidualX = camera.PixelToMmOffsetX(ResultRecord.RotationCenterPixelX);
            double cocResidualY = camera.PixelToMmOffsetY(ResultRecord.RotationCenterPixelY);
            double dieX = bottom.BottomCenterOffsetX;
            double dieY = bottom.BottomCenterOffsetY;

            // COC 기계 중심으로 XY 이동한 뒤 다시 측정했으므로 영상상의 회전 중심은 (0,0)으로 적용합니다.
            double rotatedY = dieX;
            double size0 = (height - referenceHeight) / 2.0;
            double size90 = (width - referenceWidth) / 2.0;
            correction = new SideFocusCorrection
            {
                Focus0 = dieY + size0,
                Focus90 = rotatedY + size90
            };

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSideFocusFormula",
                "Side AF 보정 계산. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", dieOffset=(" + dieX.ToString("F6") + "," + dieY.ToString("F6") + ")" +
                ", cocResidualBeforeMove=(" + cocResidualX.ToString("F6") + "," + cocResidualY.ToString("F6") + ")" +
                ", cocCenterAfterMove=(0.000000,0.000000)" +
                ", measuredSize=(" + width.ToString("F6") + "," + height.ToString("F6") + ")" +
                ", referenceSize=(" + referenceWidth.ToString("F6") + "," + referenceHeight.ToString("F6") + ")" +
                ", rotatedY=Dx=" + rotatedY.ToString("F6") +
                ", focus0=Dy+(H-Href)/2=" + correction.Focus0.ToString("F6") +
                ", focus90=RotY+(W-Wref)/2=" + correction.Focus90.ToString("F6"));
            return true;
        }

        private static bool TryReadBottomValue(BottomVisionOffset bottom, out double value, params string[] keys)
        {
            value = 0.0;
            if (bottom == null || bottom.Values == null || keys == null)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                string raw;
                if (bottom.Values.TryGetValue(keys[i], out raw) &&
                    double.TryParse(raw, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out value))
                    return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
            }

            return false;
        }

        private async Task<int> RunSideAutoFocusAsync(int angleDeg, double focusCorrection, CancellationToken ct)
        {
            VisionFocusCalibrationData focusData = Context.Machine.VisionUnit.Config.FocusCalibration;
            focusData.EnsureObjects();
            VisionFocusScanSettings settings = focusData.SideVisionScan;
            settings.EnsureDefaults();

            VisionFocusScanKind kind;
            if (_calibrationSide == VisionFocusPickerSide.Front)
                kind = angleDeg == 90 ? VisionFocusScanKind.FrontSide90 : VisionFocusScanKind.FrontSide0;
            else
                kind = angleDeg == 90 ? VisionFocusScanKind.RearSide90 : VisionFocusScanKind.RearSide0;

            PickerAxis zAxis = GetPickerZAxis(_colletIndex);
            PickerAxis tAxis = GetPickerTAxis(_colletIndex);
            double sideT0 = ResolvePickerZoneT("DieSidePosition", _colletIndex);
            double targetT = angleDeg == 90 ? sideT0 + 90.0 : sideT0;
            VisionAxis visionAxis = _calibrationSide == VisionFocusPickerSide.Front
                ? VisionAxis.FrontSideVisionY
                : VisionAxis.RearSideVisionY;
            // Side 0/90도는 동일한 카메라 초점 기준을 사용하고 각도별 COC/Die Size 보정만 적용한다.
            const string positionName = "Process0Position";
            double teachingY = Context.Machine.VisionUnit.GetVisionTeachingPosition(visionAxis, positionName);
            double axisSign = _calibrationSide == VisionFocusPickerSide.Front ? 1.0 : -1.0;

            // PickerZ 우선순위: (1) Bottom AF Best Z + 공용 Z옵셋(사용 설정 시) (2) 저장된 Side AF PickerZ (3) SidePosition 티칭.
            VisionFocusPositionRecord savedRecord = focusData.GetSideRecord(kind, _colletNo);
            // 초점 신호가 없던 스캔(score<=0)의 저장값은 시작 위치로 쓰지 않는다.
            bool savedFocusMeaningful = savedRecord != null && savedRecord.BestScore > 0.0;
            bool useSavedZ = savedFocusMeaningful && savedRecord.PickerZValid &&
                             !double.IsNaN(savedRecord.PickerZPosition) &&
                             !double.IsInfinity(savedRecord.PickerZPosition);
            // Z옵셋 기준 Z: 생산과 동일 기준을 위해 콜렛별 Bottom Die AF Best를 우선 사용하고,
            // 없으면 이번 캘리브레이션의 Bottom AF 최종 Z(FinalPickerZ)를 사용한다.
            bool useBottomZOffset = false;
            double bottomBestZ = 0.0;
            string bottomBestZSource = "None";
            if (focusData.UseBottomToSideZOffset)
            {
                VisionFocusPositionRecord bottomDieRecord = focusData.GetBottomRecord(
                    VisionFocusScanKind.BottomDie, _calibrationSide, _colletNo);
                if (bottomDieRecord != null && bottomDieRecord.Valid && bottomDieRecord.BestScore > 0.0 &&
                    !double.IsNaN(bottomDieRecord.BestPosition) && !double.IsInfinity(bottomDieRecord.BestPosition))
                {
                    bottomBestZ = bottomDieRecord.BestPosition;
                    bottomBestZSource = "BottomDieAfRecord";
                    useBottomZOffset = true;
                }
                else if (ResultRecord != null &&
                         !double.IsNaN(ResultRecord.FinalPickerZ) &&
                         !double.IsInfinity(ResultRecord.FinalPickerZ))
                {
                    bottomBestZ = ResultRecord.FinalPickerZ;
                    bottomBestZSource = "ColletCalFinalZ";
                    useBottomZOffset = true;
                }
            }
            double sideZ;
            string sideZSource;
            if (useBottomZOffset)
            {
                // Bottom AF가 찾은 초점 Z에 Bottom↔Side 기계 옵셋을 더해 Side 촬영 Z를 만든다.
                sideZ = bottomBestZ + focusData.BottomToSideZOffsetMm;
                sideZSource = "BottomBestZ+Offset(" + bottomBestZSource + ")";
            }
            else if (useSavedZ)
            {
                sideZ = savedRecord.PickerZPosition;
                sideZSource = "SavedRecord";
            }
            else
            {
                sideZ = GetPickerTeachingPosition(zAxis, "SidePosition");
                sideZSource = "SidePositionTeaching";
            }

            // Side 카메라 Y 시작 = Process 티칭 + COC/다이사이즈 보정(부호는 설정값, 실장비 테스트로 확정).
            double defaultY = teachingY + axisSign * focusCorrection;

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSideAutoFocus",
                "Side AF 시작 위치 결정. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", angle=" + angleDeg +
                ", pickerZSource=" + sideZSource +
                ", pickerZ=" + sideZ.ToString("F6") +
                ", bottomBestZ=" + bottomBestZ.ToString("F6") + "(" + bottomBestZSource + ")" +
                ", zOffsetMm=" + focusData.BottomToSideZOffsetMm.ToString("F6") +
                ", zOffsetUse=" + focusData.UseBottomToSideZOffset +
                ", visionYSource=TeachingPlusCocCorrection" +
                ", defaultY=" + defaultY.ToString("F6") +
                ", teachingY=" + teachingY.ToString("F6") +
                ", focusCorrection=" + focusCorrection.ToString("F6"));

            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                sideZ,
                "Side AF PickerZ 위치",
                ct,
                "ColletCalibration;PickerZone=Bottom",
                true,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                tAxis,
                targetT,
                "Side AF PickerT " + angleDeg + "도",
                ct,
                "ColletCalibration;PickerZone=Bottom",
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            var request = new VisionFocusScanRequest
            {
                Kind = kind,
                PickerSide = _calibrationSide,
                PickerNo = _colletNo,
                DefaultPosition = defaultY,
                MinusRange = settings.MinusRange,
                PlusRange = settings.PlusRange,
                Step = settings.Step,
                FineMinusRange = settings.FineMinusRange,
                FinePlusRange = settings.FinePlusRange,
                FineStep = settings.FineStep,
                RepeatCount = settings.RepeatCount,
                MoveVelocity = settings.MoveVelocity,
                MoveAcceleration = settings.MoveAcceleration,
                MoveDeceleration = settings.MoveDeceleration,
                SettleDelayMs = settings.SettleDelayMs,
                MotionTimeoutMs = settings.MotionTimeoutMs,
                VisionTimeoutMs = settings.VisionTimeoutMs,
                VisionBestTimeoutMs = settings.VisionBestTimeoutMs,
                FocusValueReceiveMode = settings.FocusValueReceiveMode,
                ReturnToDefaultAfterScan = false,
                UpdatedBy = "ColletCalibrationSideAutoFocus"
            };

            var focus = new VisionFocusScanSequence(Context.Machine, request);
            result = await focus.RunAsync(
                ct,
                Options != null ? Options.RunMode : SequenceRunMode.Manual).ConfigureAwait(false);
            if (result != 0)
                return Fail("COLLET-CAL-SIDE-AUTO-FOCUS", Name,
                    "Side AutoFocus 실행에 실패했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", angle=" + angleDeg +
                    ", message=" + focus.Result.Message);

            QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalSideAutoFocus",
                "Side AutoFocus Best 적용. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", angle=" + angleDeg +
                ", teachingY=" + teachingY.ToString("F6") +
                ", axisSign=" + axisSign.ToString("F0") +
                ", focusCorrection=" + focusCorrection.ToString("F6") +
                ", defaultY=" + defaultY.ToString("F6") +
                ", bestY=" + focus.Result.BestPosition.ToString("F6") +
                ", score=" + focus.Result.BestScore.ToString("F6") +
                ", sample=" + focus.Result.SampleCount);
            return 0;
        }

        private async Task<MatchResultDto> RequestColletMatchAsync(CancellationToken ct)
        {
            if (IsDryRunWithBottomVisionConnected())
            {
                await AutoVisionRequestService.GrabAsync(
                    AutoVisionChannel.BottomInspection,
                    _colletNo,
                    _settings != null ? _settings.VisionTimeoutMs : 5000,
                    ct).ConfigureAwait(false);

                return BuildSimulatedColletMatch();
            }

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

        private static bool IsDryRunWithBottomVisionConnected()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                return VisionCommandService.IsConnected(AutoVisionChannel.BottomInspection);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
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

            double offsetMmX = actualX - _simColletCenterPickerX.Value;
            double offsetMmY = _simColletCenterPickerY.Value - actualY;
            double theta = actualT - _simColletZeroPickerT.Value;
            double pixelX = camera.ImageCenterPixelX + SafeMmToPixel(offsetMmX, camera.PixelToMmX) + NextSimulatedValue(0.0, SimColletNoisePixel);
            double pixelY = camera.ImageCenterPixelY - SafeMmToPixel(offsetMmY, camera.PixelToMmY) + NextSimulatedValue(0.0, SimColletNoisePixel);
            double angle = theta + NextSimulatedValue(0.0, SimColletNoiseAngleDeg);
            double score = NextSimulatedScore();

            string simulationMessage =
                "Collet Calibration Vision 결과를 작은 랜덤 오차로 시뮬레이션합니다. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + "," + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + "," + pixelY.ToString("F3") + ")" +
                ", offsetMm=(" + offsetMmX.ToString("F6") + "," + offsetMmY.ToString("F6") + ")" +
                ", formulaPixelX=centerX+(offsetMmX/scaleX)+noise=" + camera.ImageCenterPixelX.ToString("F3") + "+(" + offsetMmX.ToString("F6") + "/" + camera.PixelToMmX.ToString("F9") + ")+noise=" + pixelX.ToString("F3") +
                ", formulaPixelY=centerY-(offsetMmY/scaleY)+noise=" + camera.ImageCenterPixelY.ToString("F3") + "-(" + offsetMmY.ToString("F6") + "/" + camera.PixelToMmY.ToString("F9") + ")+noise=" + pixelY.ToString("F3") +
                ", formulaAngle=actualT-zeroT+noise=" + actualT.ToString("F6") + "-" + _simColletZeroPickerT.Value.ToString("F6") + "+noise=" + angle.ToString("F6") +
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
                double oldSideX = GetPickerTeachingPosition(PickerAxis.PickerX, "SidePosition");
                double oldSideY = GetPickerTeachingPosition(PickerAxis.PickerY, "SidePosition");
                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                double oldBottomZ = GetPickerTeachingPosition(zAxis, "BottomPosition");
                double oldSideZ = GetPickerTeachingPosition(zAxis, "SidePosition");
                double oldDieBottomX = GetPickerTeachingPosition(PickerAxis.PickerX, BuildIndexedPositionName("DieBottomPosition"));
                double oldDieSideX = GetPickerTeachingPosition(PickerAxis.PickerX, BuildIndexedPositionName("DieSidePosition"));
                double oldDiePickY = GetPickerTeachingPosition(PickerAxis.PickerY, BuildIndexedPositionName("DiePickPosition"));
                double oldDieBottomY = GetPickerTeachingPosition(PickerAxis.PickerY, BuildIndexedPositionName("DieBottomPosition"));
                double oldDieSideY = GetPickerTeachingPosition(PickerAxis.PickerY, BuildIndexedPositionName("DieSidePosition"));
                double oldDiePlaceY = GetPickerTeachingPosition(PickerAxis.PickerY, BuildIndexedPositionName("DiePlacePosition"));
                double oldDieBottomZ = GetPickerTeachingPosition(zAxis, BuildIndexedPositionName("DieBottomPosition"));
                double oldDieSideZ = GetPickerTeachingPosition(zAxis, BuildIndexedPositionName("DieSidePosition"));
                double pickerPitchX = ResolvePickerPitchXMagnitude();
                double bottomPicker1X = target.FinalPickerX + (pickerPitchX * 3.0);
                double sideTeachingX = bottomPicker1X + pickerPitchX;
                ColletInspectionZInfo inspectionZ = ResolveColletInspectionTeachingZ(recipeName, target.FinalPickerZ);
                SetPickerBottomTeachingPosition(PickerAxis.PickerX, target.FinalPickerX);
                SetPickerBottomTeachingPosition(PickerAxis.PickerY, target.FinalPickerY);
                SetPickerBottomTeachingPosition(zAxis, inspectionZ.TeachingZ);
                SetPickerSideTeachingPosition(PickerAxis.PickerX, sideTeachingX);
                SetPickerSideTeachingPosition(PickerAxis.PickerY, target.FinalPickerY);
                SetPickerSideTeachingPosition(zAxis, inspectionZ.TeachingZ);
                SyncReferenceColletDieTeachingPositions(target, zAxis, sideTeachingX, inspectionZ.TeachingZ);

                if (!Context.Machine.SaveRecipe(recipeName))
                    return Fail("COLLET-CAL-REFERENCE-RECIPE-SAVE", Name,
                        "4번 Collet 기준 Bottom X/Y 티칭 Recipe 저장에 실패했습니다. side=" + _calibrationSide +
                        ", recipe=" + recipeName);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalReferenceTeach",
                    "4번 Collet 최종 OK 위치를 Bottom 기준 X/Y 티칭으로 저장했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", oldBottom=(" + oldBottomX.ToString("F6") + "," + oldBottomY.ToString("F6") + "," + oldBottomZ.ToString("F6") + ")" +
                    ", newBottom=(" + target.FinalPickerX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + "," + inspectionZ.TeachingZ.ToString("F6") + ")" +
                    ", oldSide=(" + oldSideX.ToString("F6") + "," + oldSideY.ToString("F6") + "," + oldSideZ.ToString("F6") + ")" +
                    ", newSide=(" + sideTeachingX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + "," + inspectionZ.TeachingZ.ToString("F6") + ")" +
                    ", measuredBestZ=" + target.FinalPickerZ.ToString("F6") +
                    ", inspectionTeachingZ=measuredBestZ+dieThickness+filmThickness+colletOffset=" + target.FinalPickerZ.ToString("F6") + "+" + inspectionZ.DieThickness.ToString("F6") + "+" + inspectionZ.FilmThickness.ToString("F6") + "+" + inspectionZ.ColletOffset.ToString("F6") + "=" + inspectionZ.TeachingZ.ToString("F6") +
                    ", colletType=" + inspectionZ.ColletType +
                    ", bottomPicker1X=" + bottomPicker1X.ToString("F6") +
                    ", pickerPitchX=" + pickerPitchX.ToString("F6") +
                    ", oldDieBottomX[" + _colletIndex + "]=" + oldDieBottomX.ToString("F6") +
                    ", oldDieSideX[" + _colletIndex + "]=" + oldDieSideX.ToString("F6") +
                    ", oldDieY[pick,bottom,side,place]=(" + oldDiePickY.ToString("F6") + "," + oldDieBottomY.ToString("F6") + "," + oldDieSideY.ToString("F6") + "," + oldDiePlaceY.ToString("F6") + ")" +
                    ", oldDieBottomZ[" + _colletIndex + "]=" + oldDieBottomZ.ToString("F6") +
                    ", oldDieSideZ[" + _colletIndex + "]=" + oldDieSideZ.ToString("F6") +
                    ", recipe=" + recipeName);

                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-REFERENCE-TEACH",
                    "4번 Collet 최종 OK 위치를 Bottom 기준 X/Y 티칭으로 저장했습니다. side=" + _calibrationSide +
                    ", oldBottom=(" + oldBottomX.ToString("F6") + "," + oldBottomY.ToString("F6") + "," + oldBottomZ.ToString("F6") + ")" +
                    ", newBottom=(" + target.FinalPickerX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + "," + inspectionZ.TeachingZ.ToString("F6") + ")" +
                    ", sideTeaching=(" + sideTeachingX.ToString("F6") + "," + target.FinalPickerY.ToString("F6") + "," + inspectionZ.TeachingZ.ToString("F6") + ")" +
                    ", measuredBestZ=" + target.FinalPickerZ.ToString("F6") +
                    ", dieThickness=" + inspectionZ.DieThickness.ToString("F6") +
                    ", filmThickness=" + inspectionZ.FilmThickness.ToString("F6") +
                    ", colletOffset=" + inspectionZ.ColletOffset.ToString("F6") +
                    ", colletType=" + inspectionZ.ColletType +
                    ", bottomPicker1X=" + bottomPicker1X.ToString("F6") +
                    ", pickerPitchX=" + pickerPitchX.ToString("F6") +
                    ", dieY[pick,bottom,side,place]=" + target.FinalPickerY.ToString("F6") +
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

        private int SaveColletInspectionZTeachingIfNeeded(ColletCalibrationRecord target)
        {
            try
            {
                if (IsReferenceCollet())
                    return 0;

                if (target == null || !target.Valid)
                    return Fail("COLLET-CAL-Z-TEACH-NO-RESULT", Name,
                        "Collet 검사 Z 티칭으로 저장할 유효한 Calibration 결과가 없습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo);

                if (Context == null || Context.Controller == null || Context.Machine == null)
                    return Fail("COLLET-CAL-Z-TEACH-NO-CONTEXT", Name,
                        "Collet 검사 Z 티칭을 저장할 Machine context가 없습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo);

                string recipeName = Context.Controller.ActiveRecipeName;
                if (string.IsNullOrWhiteSpace(recipeName))
                    return Fail("COLLET-CAL-Z-TEACH-NO-RECIPE", Name,
                        "Collet 검사 Z 티칭을 저장할 활성 Recipe가 없습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo);

                PickerAxis zAxis = GetPickerZAxis(_colletIndex);
                double oldBottomZ = GetPickerTeachingPosition(zAxis, "BottomPosition");
                double oldSideZ = GetPickerTeachingPosition(zAxis, "SidePosition");
                double oldDieBottomZ = GetPickerTeachingPosition(zAxis, BuildIndexedPositionName("DieBottomPosition"));
                double oldDieSideZ = GetPickerTeachingPosition(zAxis, BuildIndexedPositionName("DieSidePosition"));
                ColletInspectionZInfo inspectionZ = ResolveColletInspectionTeachingZ(recipeName, target.FinalPickerZ);

                SetPickerBottomTeachingPosition(zAxis, inspectionZ.TeachingZ);
                SetPickerSideTeachingPosition(zAxis, inspectionZ.TeachingZ);
                SetPickerIndexedTeachingPosition(zAxis, "DieBottomPosition", inspectionZ.TeachingZ);
                SetPickerIndexedTeachingPosition(zAxis, "DieSidePosition", inspectionZ.TeachingZ);

                if (!Context.Machine.SaveRecipe(recipeName))
                    return Fail("COLLET-CAL-Z-TEACH-RECIPE-SAVE", Name,
                        "Collet 검사 Z 티칭 Recipe 저장에 실패했습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", recipe=" + recipeName);

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalInspectionZTeach",
                    "Collet Cal 결과로 Bottom/Side 검사 Z 티칭을 저장했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", zAxis=" + zAxis +
                    ", measuredBestZ=" + target.FinalPickerZ.ToString("F6") +
                    ", inspectionTeachingZ=measuredBestZ+dieThickness+filmThickness+colletOffset=" + target.FinalPickerZ.ToString("F6") + "+" + inspectionZ.DieThickness.ToString("F6") + "+" + inspectionZ.FilmThickness.ToString("F6") + "+" + inspectionZ.ColletOffset.ToString("F6") + "=" + inspectionZ.TeachingZ.ToString("F6") +
                    ", colletType=" + inspectionZ.ColletType +
                    ", oldBottomZ=" + oldBottomZ.ToString("F6") +
                    ", oldSideZ=" + oldSideZ.ToString("F6") +
                    ", oldDieBottomZ[" + _colletIndex + "]=" + oldDieBottomZ.ToString("F6") +
                    ", oldDieSideZ[" + _colletIndex + "]=" + oldDieSideZ.ToString("F6") +
                    ", recipe=" + recipeName);

                EventLogger.Write(EventKind.Event, "CAL", "COLLET-CAL-Z-TEACH",
                    "Collet Cal 결과로 Bottom/Side 검사 Z 티칭을 저장했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", measuredBestZ=" + target.FinalPickerZ.ToString("F6") +
                    ", inspectionTeachingZ=" + inspectionZ.TeachingZ.ToString("F6") +
                    ", dieThickness=" + inspectionZ.DieThickness.ToString("F6") +
                    ", filmThickness=" + inspectionZ.FilmThickness.ToString("F6") +
                    ", colletOffset=" + inspectionZ.ColletOffset.ToString("F6") +
                    ", colletType=" + inspectionZ.ColletType +
                    ", recipe=" + recipeName);

                return 0;
            }
            catch (Exception ex)
            {
                return Fail("COLLET-CAL-Z-TEACH-EX", Name,
                    "Collet 검사 Z 티칭 저장 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
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

        private void SetPickerSideTeachingPosition(PickerAxis axis, double position)
        {
            if (_calibrationSide == VisionFocusPickerSide.Front)
            {
                if (FrontPicker == null)
                    throw new InvalidOperationException("Front Picker Unit이 없습니다.");

                FrontPicker.SetPickerAxisTeachingPosition(axis, "SidePosition", position);
                return;
            }

            if (RearPicker == null)
                throw new InvalidOperationException("Rear Picker Unit이 없습니다.");

            RearPicker.SetPickerAxisTeachingPosition(axis, "SidePosition", position);
        }

        private void SetPickerPickTeachingPosition(PickerAxis axis, double position)
        {
            if (_calibrationSide == VisionFocusPickerSide.Front)
            {
                if (FrontPicker == null)
                    throw new InvalidOperationException("Front Picker Unit이 없습니다.");

                FrontPicker.SetPickerAxisTeachingPosition(axis, "PickPosition", position);
                return;
            }

            if (RearPicker == null)
                throw new InvalidOperationException("Rear Picker Unit이 없습니다.");

            RearPicker.SetPickerAxisTeachingPosition(axis, "PickPosition", position);
        }

        private void SyncReferenceColletDieTeachingPositions(ColletCalibrationRecord target, PickerAxis zAxis, double sideTeachingX, double inspectionTeachingZ)
        {
            if (target == null)
                return;

            // Reference collet defines common forward Y. Side #4 X starts at Bottom #1 X plus one picker pitch.
            SetPickerIndexedTeachingPosition(PickerAxis.PickerX, "DieBottomPosition", target.FinalPickerX);
            SetPickerIndexedTeachingPosition(PickerAxis.PickerX, "DieSidePosition", sideTeachingX);
            SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DiePickPosition", target.FinalPickerY);
            SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DieBottomPosition", target.FinalPickerY);
            SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DieSidePosition", target.FinalPickerY);
            SetPickerIndexedTeachingPosition(PickerAxis.PickerY, "DiePlacePosition", target.FinalPickerY);
            SetPickerIndexedTeachingPosition(zAxis, "DieBottomPosition", inspectionTeachingZ);
            SetPickerIndexedTeachingPosition(zAxis, "DieSidePosition", inspectionTeachingZ);
        }

        private ColletInspectionZInfo ResolveColletInspectionTeachingZ(string recipeName, double measuredBestZ)
        {
            var info = new ColletInspectionZInfo
            {
                TeachingZ = measuredBestZ,
                ColletType = ColletShapeType.Flat
            };

            try
            {
                RecipeProject project = null;
                if (!string.IsNullOrWhiteSpace(recipeName))
                    project = RecipeStore.Load(recipeName);
                if (project == null)
                    project = RecipeStore.LoadLastOrDefault();
                if (project == null)
                    return info;

                if (project.ColletZ == null)
                    project.ColletZ = new ColletZConfigSubset();
                project.ColletZ.Ensure();

                info.ColletType = project.ColletZ.ColletType;
                info.DieThickness = Math.Max(0.0, project.ColletZ.DieCalThicknessMm);
                info.FilmThickness = Math.Max(0.0, project.ColletZ.FilmThicknessMm);
                info.ColletOffset = project.ColletZ.ColletType == ColletShapeType.Rim
                    ? project.ColletZ.RimOffsetFromFlatMm
                    : project.ColletZ.FlatZOffsetMm;
                info.TeachingZ = measuredBestZ + info.DieThickness + info.FilmThickness + info.ColletOffset;
                return info;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalInspectionZ",
                    "Collet 검사 Z 보정값을 읽지 못해 측정 Z를 그대로 사용합니다. recipe=" + recipeName +
                    ", measuredBestZ=" + measuredBestZ.ToString("F6") +
                    ", error=" + ex.Message);
                return info;
            }
            finally
            {
            }
        }

        private double ResolvePickerPitchXMagnitude()
        {
            try
            {
                double pitch = 0.0;
                if (_calibrationSide == VisionFocusPickerSide.Front && FrontPicker != null && FrontPicker.Setup != null)
                    pitch = FrontPicker.Setup.PickerPitchX;
                else if (_calibrationSide == VisionFocusPickerSide.Rear && RearPicker != null && RearPicker.Setup != null)
                    pitch = RearPicker.Setup.PickerPitchX;

                return Math.Abs(pitch);
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private void SetPickerIndexedTeachingPosition(PickerAxis axis, string positionArrayName, double position)
        {
            string positionName = BuildIndexedPositionName(positionArrayName);
            if (_calibrationSide == VisionFocusPickerSide.Front)
            {
                if (FrontPicker == null)
                    throw new InvalidOperationException("Front Picker Unit이 없습니다.");

                FrontPicker.SetPickerAxisTeachingPosition(axis, positionName, position);
                return;
            }

            if (RearPicker == null)
                throw new InvalidOperationException("Rear Picker Unit이 없습니다.");

            RearPicker.SetPickerAxisTeachingPosition(axis, positionName, position);
        }

        private string BuildIndexedPositionName(string positionArrayName)
        {
            return positionArrayName + "[" + _colletIndex + "]";
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
