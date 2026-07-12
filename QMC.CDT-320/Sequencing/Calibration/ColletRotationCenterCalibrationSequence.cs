using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.VisionComm;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal sealed class ColletRotationCenterCalibrationSequence
        : PickerSequenceBase<ColletRotationCenterCalibrationStep>
    {
        private const double RotationDegrees = 360.0;
        private const double PositionToleranceMm = 0.05;
        private const int MinimumRotationTimeoutMs = 20000;
        private const int RotationTimeoutMarginMs = 5000;
        private const int CocStartVisionTimeoutMs = 30000;
        private const int CocEndVisionTimeoutMs = 30000;

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly int _colletNo;
        private readonly int _colletIndex;
        private SequenceResourceLease _inspectionAreaLease;
        private ColletCalibrationSettings _settings;
        private ColletCalibrationRecord _record;
        private bool _cocStarted;
        private bool _cocEnded;

        public ColletRotationCenterCalibrationSequence(
            MachineSequenceContext context,
            VisionFocusPickerSide side,
            int colletNo)
            : base(
                context,
                side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                PickerSequenceKind.Inspect,
                side == VisionFocusPickerSide.Front
                    ? "FrontColletRotationCenterCalibrationSequence"
                    : "RearColletRotationCenterCalibrationSequence")
        {
            _calibrationSide = side;
            _colletNo = Math.Max(1, Math.Min(4, colletNo));
            _colletIndex = _colletNo - 1;
            CurrentStep = ColletRotationCenterCalibrationStep.CheckReady;
        }

        public VisionCocResult Result { get; private set; }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                int result = CheckReady();
                if (result != 0)
                    return result;

                result = await AcquireInspectionAreaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = ColletRotationCenterCalibrationStep.StartCoc;
                result = await StartCocAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = ColletRotationCenterCalibrationStep.RotateTheta;
                result = await RotateThetaAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = ColletRotationCenterCalibrationStep.EndCoc;
                result = await EndCocAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = ColletRotationCenterCalibrationStep.SaveResult;
                result = SaveResult();
                if (result != 0)
                    return result;

                CurrentStep = ColletRotationCenterCalibrationStep.Complete;
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
                return Fail("COLLET-COC-EX", Name,
                    "콜렛 회전 중심 캘리브레이션 중 예외가 발생했습니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", step=" + CurrentStep +
                    ", error=" + ex.Message);
            }
            finally
            {
                await EndCocForCleanupAsync().ConfigureAwait(false);
                if (_inspectionAreaLease != null)
                {
                    _inspectionAreaLease.Dispose();
                    _inspectionAreaLease = null;
                }
            }
        }

        private int CheckReady()
        {
            if (Context == null || Context.Machine == null || Context.Machine.VisionUnit == null)
                return Fail("COLLET-COC-NO-MACHINE", Name, "장비 또는 VisionUnit이 없어 COC를 시작할 수 없습니다.");

            Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
            ColletCalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData.Collet;
            data.EnsureObjects();
            _settings = data.Settings;
            _settings.EnsureDefaults();
            _record = data.GetRecord(_calibrationSide, _colletNo);
            if (_record == null || !_record.Valid)
            {
                return Fail("COLLET-COC-CAL-NOT-READY", Name,
                    "COC 시작 전 선택 콜렛의 Collet Calibration을 먼저 완료해야 합니다. side=" +
                    _calibrationSide + ", colletNo=" + _colletNo);
            }

            BaseAxis x = GetPickerAxis(PickerAxis.PickerX);
            BaseAxis y = GetPickerAxis(PickerAxis.PickerY);
            BaseAxis z = GetPickerAxis(GetPickerZAxis(_colletIndex));
            BaseAxis t = GetPickerAxis(GetPickerTAxis(_colletIndex));
            string notReady = BuildAxisNotReadyReason(x, "PickerX") +
                              BuildAxisNotReadyReason(y, "PickerY") +
                              BuildAxisNotReadyReason(z, "PickerZ") +
                              BuildAxisNotReadyReason(t, "PickerT");
            if (!string.IsNullOrWhiteSpace(notReady))
                return Fail("COLLET-COC-AXIS-NOT-READY", Name, "COC 축 준비 상태가 아닙니다. " + notReady);

            if (!IsNear(x, _record.FinalPickerX) ||
                !IsNear(y, _record.FinalPickerY) ||
                !IsNear(z, _record.FinalPickerZ))
            {
                return Fail("COLLET-COC-POSITION", Name,
                    "COC는 기존 Collet Calibration 완료 X/Y/Z 위치에서만 시작할 수 있습니다. " +
                    "side=" + _calibrationSide + ", colletNo=" + _colletNo +
                    ", current=(" + x.ActualPosition.ToString("F6") + "," + y.ActualPosition.ToString("F6") + "," + z.ActualPosition.ToString("F6") + ")" +
                    ", expected=(" + _record.FinalPickerX.ToString("F6") + "," + _record.FinalPickerY.ToString("F6") + "," + _record.FinalPickerZ.ToString("F6") + ")" +
                    ", toleranceMm=" + PositionToleranceMm.ToString("F3"));
            }

            t.Setup.SoftLimitMinus = -720.0;
            t.Setup.SoftLimitPlus = 720.0;
            t.Setup.SoftLimitEnabled = true;

            if (!IsPickerSimulationOrDryRun() &&
                !VisionCommandService.IsConnected(AutoVisionChannel.BottomInspection))
            {
                return Fail("COLLET-COC-VISION-DISCONNECTED", "Vision",
                    "Bottom Vision 통신이 연결되지 않아 COC를 시작할 수 없습니다.");
            }

            WriteLog("ColletCOC",
                "COC 시작 조건 확인 완료. side=" + _calibrationSide +
                ", colletNo=" + _colletNo +
                ", position=(" + x.ActualPosition.ToString("F6") + "," + y.ActualPosition.ToString("F6") + "," + z.ActualPosition.ToString("F6") + ")" +
                ", tActual=" + t.ActualPosition.ToString("F6") +
                ", cocVelocityDegPerSec=" + _settings.CocRotationVelocityDegPerSec.ToString("F3") +
                ", tSoftLimit=-720~720 - Ok");
            return 0;
        }

        private async Task<int> AcquireInspectionAreaAsync(CancellationToken ct)
        {
            _inspectionAreaLease = await AcquireResourceAsync(
                SequenceResourceKind.InspectionArea,
                Name + ":COC",
                ct).ConfigureAwait(false);
            return _inspectionAreaLease != null ? 0 : -1;
        }

        private async Task<int> StartCocAsync(CancellationToken ct)
        {
            if (IsPickerSimulationOrDryRun())
            {
                _cocStarted = true;
                return 0;
            }

            VisionCocResult start = await VisionCommandService.StartColletRotationCenterAsync(
                AutoVisionChannel.BottomInspection,
                _calibrationSide == VisionFocusPickerSide.Front ? "F" : "R",
                _colletNo,
                CocStartVisionTimeoutMs,
                ct).ConfigureAwait(false);
            if (start == null || !start.Started)
                return Fail("COLLET-COC-START-ACK", "Vision", "COC START ACK를 받지 못했습니다. raw=" + (start != null ? start.Raw : "null"));

            _cocStarted = true;
            WriteLog("ColletCOC", "Vision COC START ACK 수신. raw=" + start.Raw + " - Ok");
            return 0;
        }

        private async Task<int> RotateThetaAsync(CancellationToken ct)
        {
            PickerAxis tAxis = GetPickerTAxis(_colletIndex);
            BaseAxis axis = GetPickerAxis(tAxis);
            double start = axis.ActualPosition;
            double delta = start + RotationDegrees <= 720.0 ? RotationDegrees : -RotationDegrees;
            double target = start + delta;
            double rotationVelocity = _settings.CocRotationVelocityDegPerSec;
            int rotationTimeoutMs = Math.Max(
                MinimumRotationTimeoutMs,
                (int)Math.Ceiling((RotationDegrees / rotationVelocity) * 1000.0) + RotationTimeoutMarginMs);

            CalibrationMotionSettings original = CalibrationMotion;
            var rotationMotion = new CalibrationMotionSettings
            {
                MoveVelocity = rotationVelocity,
                MoveAcceleration = original != null ? original.MoveAcceleration : axis.Config.Acceleration,
                MoveDeceleration = original != null ? original.MoveDeceleration : axis.Config.Deceleration,
                MoveTimeoutMs = rotationTimeoutMs
            };
            rotationMotion.EnsureDefaults();
            SetCalibrationMotion(rotationMotion);
            try
            {
                WriteLog("ColletCOC",
                    "콜렛 T축 360도 회전을 시작합니다. side=" + _calibrationSide +
                    ", colletNo=" + _colletNo +
                    ", startT=" + start.ToString("F6") +
                    ", targetT=" + target.ToString("F6") +
                    ", delta=" + delta.ToString("F6") +
                    ", velocityDegPerSec=" + rotationVelocity.ToString("F3") +
                    ", timeoutMs=" + rotationTimeoutMs + " - Start");

                return await MovePickerAxisAndVerifyAsync(
                    tAxis,
                    target,
                    "COC 콜렛 T축 360도 회전",
                    ct,
                    "ColletCOC;PickerZone=Bottom",
                    true).ConfigureAwait(false);
            }
            finally
            {
                SetCalibrationMotion(original);
            }
        }

        private async Task<int> EndCocAsync(CancellationToken ct)
        {
            if (IsPickerSimulationOrDryRun())
            {
                Result = new VisionCocResult
                {
                    Success = true,
                    CenterPixelX = _record.CenterPixelX,
                    CenterPixelY = _record.CenterPixelY,
                    SampleCount = 1,
                    FrameCount = 1,
                    Raw = "SIMULATION:COC"
                };
                _cocEnded = true;
                return 0;
            }

            Result = await VisionCommandService.EndColletRotationCenterAsync(
                AutoVisionChannel.BottomInspection,
                _calibrationSide == VisionFocusPickerSide.Front ? "F" : "R",
                _colletNo,
                CocEndVisionTimeoutMs,
                ct).ConfigureAwait(false);
            _cocEnded = true;
            if (Result == null || !Result.Success)
                return Fail("COLLET-COC-END-RESULT", "Vision", "COC END 중심 결과를 받지 못했습니다. raw=" + (Result != null ? Result.Raw : "null"));

            WriteLog("ColletCOC",
                "Vision COC END 픽셀 결과 수신. centerPixel=(" + Result.CenterPixelX.ToString("F6") + "," + Result.CenterPixelY.ToString("F6") + ")" +
                ", frames=" + Result.FrameCount +
                ", raw=" + Result.Raw + " - Ok");
            return 0;
        }

        private int SaveResult()
        {
            _record.RotationCenterPixelX = Result.CenterPixelX;
            _record.RotationCenterPixelY = Result.CenterPixelY;
            _record.RotationCenterRadiusPixel = Result.RadiusPixel;
            _record.RotationCenterSampleCount = Result.FrameCount;
            _record.RotationCenterValid = true;
            _record.RotationCenterUpdatedAt = DateTime.Now;
            bool saved = Context.Machine.VisionUnit.SaveSettings();
            if (!saved)
                return Fail("COLLET-COC-SAVE", "VisionUnit", "COC 회전 중심 픽셀 결과 저장에 실패했습니다.");
            return 0;
        }

        private async Task EndCocForCleanupAsync()
        {
            if (!_cocStarted || _cocEnded || IsPickerSimulationOrDryRun())
                return;
            try
            {
                VisionCocResult cleanup = await VisionCommandService.EndColletRotationCenterAsync(
                    AutoVisionChannel.BottomInspection,
                    _calibrationSide == VisionFocusPickerSide.Front ? "F" : "R",
                    _colletNo,
                    CocEndVisionTimeoutMs,
                    CancellationToken.None).ConfigureAwait(false);
                _cocEnded = true;
                if (cleanup != null && (cleanup.Success || cleanup.Started))
                {
                    WriteLog("ColletCOC",
                        "COC 비정상 종료 정리를 위해 END 명령을 전송했습니다. raw=" + cleanup.Raw + " - Check");
                }
                else
                {
                    WriteLog("ColletCOC",
                        "COC 비정상 종료 END 응답을 확인하지 못했습니다. Vision 연결 종료 시 Vision 측 Abort로 Live를 정리합니다. raw=" +
                        (cleanup != null ? cleanup.Raw : "null") + " - Failed");
                }
            }
            catch (Exception ex)
            {
                WriteLog("ColletCOC", "COC 종료 정리 명령 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
        }

        private static bool IsNear(BaseAxis axis, double target)
        {
            return axis != null && !axis.IsMoving && Math.Abs(axis.ActualPosition - target) <= PositionToleranceMm;
        }

        private static string BuildAxisNotReadyReason(BaseAxis axis, string name)
        {
            if (axis == null) return name + "=null; ";
            if (!axis.IsServoOn) return name + " servo=OFF; ";
            if (axis.IsAlarm) return name + " alarm=ON; ";
            if (axis.IsMoving) return name + " moving=ON; ";
            return string.Empty;
        }
    }

    internal enum ColletRotationCenterCalibrationStep
    {
        CheckReady,
        StartCoc,
        RotateTheta,
        EndCoc,
        SaveResult,
        Complete
    }
}
