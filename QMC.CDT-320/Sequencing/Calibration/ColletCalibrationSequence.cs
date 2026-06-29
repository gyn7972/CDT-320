using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.VisionComm;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal sealed class ColletCalibrationSequence : PickerSequenceBase<ColletCalibrationStep>
    {
        private const string BottomFinderTargetName = "ColletCalibration;PickerZone=Bottom";

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
        private double _basePickerT;
        private double _measuredTPosition;
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
                if (Context.Machine.VisionUnit.Config.ColletCalibration == null)
                    Context.Machine.VisionUnit.Config.ColletCalibration = new ColletCalibrationData();
                Context.Machine.VisionUnit.Config.ColletCalibration.EnsureObjects();
                _settings = Context.Machine.VisionUnit.Config.ColletCalibration.Settings;
                _settings.EnsureDefaults();

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

                EnsurePickerWorkAreaReserved(PickerWorkZone.Bottom, "ColletCalibration");

                result = await MoveAllPickerZToAvoidAndVerifyAsync("Collet Calibration 전 PickerZ 전체 Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOppositePickerToAvoidAndVerifyAsync("Collet Calibration 진입 전 상대 Picker Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                _targetPickerX = GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition") +
                                 ResolvePickerPitchXOffset("DieBottomPosition", _colletIndex);
                _targetPickerY = GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition");
                _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_colletIndex), "BottomPosition");
                _basePickerT = GetPickerTeachingPosition(GetPickerTAxis(_colletIndex), "BottomPosition");

                var xyTargets = new Dictionary<PickerAxis, double>();
                xyTargets[PickerAxis.PickerX] = _targetPickerX;
                xyTargets[PickerAxis.PickerY] = _targetPickerY;
                result = await MovePickerXTThenYAndVerifyAsync(
                    xyTargets,
                    "Collet Calibration Bottom X/Y",
                    ct,
                    BottomFinderTargetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerTAxis(_colletIndex),
                    _basePickerT,
                    "Collet Calibration T 기준 위치",
                    ct,
                    BottomFinderTargetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    GetPickerZAxis(_colletIndex),
                    _targetPickerZ,
                    "Collet Calibration Bottom Z",
                    ct,
                    BottomFinderTargetName).ConfigureAwait(false);
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
                        int focusResult = await RunAutoFocusIfNeededAsync(ct).ConfigureAwait(false);
                        if (focusResult != 0)
                            return focusResult;

                        CurrentStep = ColletCalibrationStep.FindColletAgain;
                        return 0;
                    }

                    BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
                    double actual = tAxis != null ? tAxis.ActualPosition : _basePickerT;
                    double target = actual + theta * _settings.ThetaMoveGain;
                    int moveResult = await MovePickerAxisAndVerifyAsync(
                        GetPickerTAxis(_colletIndex),
                        target,
                        "Collet Calibration T 0도 보정",
                        ct,
                        BottomFinderTargetName).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

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
                    RepeatCount = focusSettings.RepeatCount,
                    MoveVelocity = focusSettings.MoveVelocity,
                    MoveAcceleration = focusSettings.MoveAcceleration,
                    MoveDeceleration = focusSettings.MoveDeceleration,
                    SettleDelayMs = focusSettings.SettleDelayMs,
                    MotionTimeoutMs = focusSettings.MotionTimeoutMs,
                    VisionTimeoutMs = focusSettings.VisionTimeoutMs,
                    ReturnToDefaultAfterScan = false,
                    UpdatedBy = "ColletCalibration"
                };

                var focus = new VisionFocusScanSequence(Context.Machine, request);
                int result = await focus.RunAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("COLLET-CAL-AUTO-FOCUS", Name,
                        "Collet T 보정 후 AutoFocus 실행에 실패했습니다. side=" + _calibrationSide +
                        ", colletNo=" + _colletNo +
                        ", message=" + focus.Result.Message);

                _targetPickerZ = focus.Result.BestPosition;
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

        private int CalculateOffset()
        {
            try
            {
                if (_finalMatch == null || !_finalMatch.Success)
                    return Fail("COLLET-CAL-CALC-NO-MATCH", Name, "Collet 보정값 계산에 사용할 최종 Vision 결과가 없습니다.");

                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(
                    Context.Machine.VisionUnit.Config.CameraCalibration,
                    AutoVisionChannel.BottomInspection);
                double centerMmX = camera.PixelToMmOffsetX(_finalMatch.X);
                double centerMmY = camera.PixelToMmOffsetY(_finalMatch.Y);
                BaseAxis tAxis = GetPickerAxis(GetPickerTAxis(_colletIndex));
                _measuredTPosition = tAxis != null ? tAxis.ActualPosition : _basePickerT;

                _calculatedRecord = new ColletCalibrationRecord
                {
                    Side = _calibrationSide,
                    ColletNo = _colletNo,
                    CenterPixelX = _finalMatch.X,
                    CenterPixelY = _finalMatch.Y,
                    CenterMmX = centerMmX,
                    CenterMmY = centerMmY,
                    OffsetX = centerMmX,
                    OffsetY = centerMmY,
                    ThetaOffset = _finalMatch.AngleDeg,
                    TZeroHomeOffset = _measuredTPosition - _basePickerT,
                    MeasuredTPosition = _measuredTPosition,
                    Valid = true,
                    UpdatedAt = DateTime.Now
                };

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
                ColletCalibrationData data = Context.Machine.VisionUnit.Config.ColletCalibration;
                data.EnsureObjects();
                ColletCalibrationRecord target = data.GetRecord(_calibrationSide, _colletNo);
                CopyRecord(_calculatedRecord, target);
                ResultRecord = target;
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
            if (IsVisionBypassed())
            {
                return new MatchResultDto
                {
                    Success = true,
                    X = 320.0,
                    Y = 240.0,
                    AngleDeg = 0.0,
                    Score = 1.0,
                    ImageWidthPixel = 640.0,
                    ImageHeightPixel = 480.0,
                    HasImageSize = true
                };
            }

            MatchResultDto match = await AutoVisionRequestService.MatchAsync(
                AutoVisionChannel.BottomInspection,
                _settings.BottomFinderName,
                _colletNo,
                _settings.VisionTimeoutMs,
                ct).ConfigureAwait(false);

            if (match != null && match.Success && _settings.ScoreThreshold > 0.0 && match.Score < _settings.ScoreThreshold)
            {
                match.Success = false;
                match.RawError = "Score가 기준보다 낮습니다. score=" + match.Score.ToString("F6") +
                                 ", threshold=" + _settings.ScoreThreshold.ToString("F6");
            }

            return match;
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

        private bool IsVisionBypassed()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
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
