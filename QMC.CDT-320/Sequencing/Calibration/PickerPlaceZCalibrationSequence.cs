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
    internal enum PlaceZCalibrationStep
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

    internal sealed class PlaceZCalibrationResult
    {
        public bool Success { get; set; }
        public VisionFocusPickerSide Side { get; set; }
        public BinSide OutputSide { get; set; }
        public int PickerNo { get; set; }
        public double OldPlacePosition { get; set; }
        public double ScanStartPosition { get; set; }
        public double SearchLimitPosition { get; set; }
        public double DetectedFlowPosition { get; set; }
        public double SavedPlacePosition { get; set; }
        public int DetectElapsedMs { get; set; }
        public string Message { get; set; }
    }

    internal sealed class PlaceZFlowSearchResult
    {
        public int ResultCode { get; set; }
        public double DetectedPosition { get; set; }
        public double FirstFlowOnPosition { get; set; }
        public double StopPosition { get; set; }
        public int ElapsedMs { get; set; }

        public bool Success
        {
            get { return ResultCode == 0; }
        }
    }

    internal sealed class PickerPlaceZCalibrationSequence : PickerSequenceBase<PlaceZCalibrationStep>
    {
        private const string SearchTargetName = "PlaceZCalibration;PickerZone=Output";
        private const int MinInitialVacuumOnDelayMs = 500;

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly BinSide _targetOutputSide;
        private readonly int _pickerNo;
        private readonly int _pickerIndex;
        private SequenceResourceLease _pickerLease;
        private SequenceResourceLease _outputPlaceLease;
        private SequenceResourceLease _outputStageLease;
        private PlaceZCalibrationSettings _settings;
        private PickerAxis _pickerZAxis;
        private PlaceCoordinateResult _calibrationTarget;
        private double _outputStageBaseY;
        private double _outputVisionToPickerX;
        private double _outputVisionToPickerY;
        private double _oldPlacePosition;
        private double _scanStartPosition;
        private double _searchLimitPosition;
        private double _searchDirection;
        private double _detectedFlowPosition;
        private double _savedPlacePosition;
        private int _detectElapsedMs;
        private bool _preferCurrentPoseScanStart;
        private volatile bool _immediateStopRequested;

        public PickerPlaceZCalibrationSequence(MachineSequenceContext context, VisionFocusPickerSide side, int pickerNo)
            : this(context, side, pickerNo, BinSide.Good)
        {
        }

        public PickerPlaceZCalibrationSequence(MachineSequenceContext context, VisionFocusPickerSide side, int pickerNo, BinSide outputSide)
            : base(
                  context,
                  side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                  PickerSequenceKind.UnloadToOutput,
                  "PlaceZCalibration")
        {
            _calibrationSide = side;
            _targetOutputSide = outputSide == BinSide.Ng ? BinSide.Ng : BinSide.Good;
            _pickerNo = NormalizePickerNo(pickerNo);
            _pickerIndex = _pickerNo - 1;
            Result = new PlaceZCalibrationResult
            {
                Side = side,
                OutputSide = _targetOutputSide,
                PickerNo = _pickerNo,
                Message = string.Empty
            };
        }

        public PlaceZCalibrationResult Result { get; private set; }

        public void RequestImmediateStop(string reason)
        {
            _immediateStopRequested = true;
            WriteLog("PlaceZCalibration",
                "PlaceZ Calibration 즉시 정지 요청. " +
                "side=" + Side +
                ", outputSide=" + _targetOutputSide +
                ", pickerNo=" + _pickerNo +
                ", step=" + CurrentStep +
                ", reason=" + reason + " - Stop");
            StopPickerZAxis("즉시 정지 요청: " + reason);
        }

        public async Task<int> RunCurrentPoseTestOrDefaultAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            _preferCurrentPoseScanStart = true;
            try
            {
                return await RunAsync(ct, options).ConfigureAwait(false);
            }
            finally
            {
                _preferCurrentPoseScanStart = false;
            }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool vacuumOn = false;
            bool currentPoseScanStart = false;
            _immediateStopRequested = false;
            try
            {
                CurrentStep = PlaceZCalibrationStep.CheckReady;
                int result = CheckReady();
                if (result != 0) return result;

                CurrentStep = PlaceZCalibrationStep.ReserveArea;
                result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "PlaceZCalibration");

                string currentPoseReason = "현재 위치 반복 테스트 요청 없음.";
                if (_preferCurrentPoseScanStart && TryConfigureCurrentPoseScanStart(out currentPoseReason))
                {
                    currentPoseScanStart = true;
                    CurrentStep = PlaceZCalibrationStep.MoveScanStart;
                    WriteLog("PlaceZCalibration",
                        "PlaceZ Calibration 현재 위치 반복 테스트 조건 충족. 안전 시작 위치 이동과 Scan Start 이동을 생략합니다. " +
                        "side=" + Side +
                        ", outputSide=" + _targetOutputSide +
                        ", pickerNo=" + _pickerNo +
                        ", scanStartZ=" + _scanStartPosition.ToString("F6") +
                        ", searchLimitZ=" + _searchLimitPosition.ToString("F6") +
                        ", reason=" + currentPoseReason + " - Ok");
                }
                else
                {
                    if (_preferCurrentPoseScanStart)
                    {
                        WriteLog("PlaceZCalibration",
                            "PlaceZ Calibration 현재 위치 반복 테스트 조건 불만족. 기존 안전 시작 위치 이동으로 진행합니다. " +
                            "side=" + Side +
                            ", outputSide=" + _targetOutputSide +
                            ", pickerNo=" + _pickerNo +
                            ", reason=" + currentPoseReason + " - Check");
                    }

                    CurrentStep = PlaceZCalibrationStep.MoveZSafe;
                    result = await PrepareSafeStartPositionAsync("PlaceZ Calibration 시작 전 안전 위치 이동", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = PlaceZCalibrationStep.MoveScanStart;
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        _scanStartPosition,
                        "PlaceZ Calibration Scan Start",
                        ct,
                        SearchTargetName,
                        false,
                        true).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                CurrentStep = PlaceZCalibrationStep.VacuumOn;
                SetPickerVacuum(_pickerNo, true);
                vacuumOn = true;
                int vacuumOnDelayMs = ResolveInitialVacuumOnDelayMs();
                if (vacuumOnDelayMs > 0)
                {
                    WriteLog("PlaceZCalibration",
                        "PlaceZ Calibration 초기 Vacuum ON 안정화 대기. " +
                        "side=" + Side +
                        ", outputSide=" + _targetOutputSide +
                        ", pickerNo=" + _pickerNo +
                        ", configuredDelayMs=" + _settings.VacuumOnDelayMs +
                        ", appliedDelayMs=" + vacuumOnDelayMs + " - Check");
                    await Task.Delay(vacuumOnDelayMs, ct).ConfigureAwait(false);
                }

                CurrentStep = PlaceZCalibrationStep.SearchFlow;
                result = await SearchFlowPositionWithResetAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                SetPickerVacuum(_pickerNo, false);
                vacuumOn = false;

                CurrentStep = PlaceZCalibrationStep.SaveResult;
                result = SaveCalibrationResult();
                if (result != 0) return result;

                CurrentStep = PlaceZCalibrationStep.MoveAvoid;
                if (_settings.MoveAvoidAfterScan)
                {
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition"),
                        "PlaceZ Calibration 완료 후 PickerZ Avoid",
                        ct,
                        "AvoidPosition",
                        false,
                        true).ConfigureAwait(false);
                    if (result != 0) return result;
                }

                CurrentStep = PlaceZCalibrationStep.Complete;
                Result.Success = true;
                Result.Message = "PlaceZ Calibration complete.";
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 완료. side=" + _calibrationSide +
                    ", pickerNo=" + _pickerNo +
                    ", oldPlaceZ=" + _oldPlacePosition.ToString("F6") +
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPlaceZ=" + _savedPlacePosition.ToString("F6") +
                    ", dieThickness=" + (_settings != null ? _settings.DieThicknessMm.ToString("F6") : "0.000000") +
                    ", filmThickness=" + (_settings != null ? _settings.FilmThicknessMm.ToString("F6") : "0.000000") +
                    ", outputSide=" + _targetOutputSide +
                    ", currentPoseScanStart=" + currentPoseScanStart +
                    ", elapsedMs=" + _detectElapsedMs + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                StopPickerZAxis("정지 요청 취소");
                Result.Message = "PlaceZ Calibration canceled.";
                throw;
            }
            catch (SequenceStopException)
            {
                StopPickerZAxis("시퀀스 정지 요청");
                throw;
            }
            catch (Exception ex)
            {
                StopPickerZAxis("예외 발생");
                return Fail("PLACE-Z-CAL-EX", Name, "PlaceZ Calibration 예외 발생. error=" + ex.Message);
            }
            finally
            {
                if (vacuumOn)
                {
                    try { SetPickerVacuum(_pickerNo, false); }
                    catch { }
                }

                WarnIfPickerZLeftDown();
                ReleasePickerWorkArea();
                ReleaseArea();
            }
        }

        /// <summary>
        /// 작업영역/lease를 해제하기 전에 PickerZ가 하강 위치에 남아 있으면 운전자에게 명시적으로 알린다.
        /// 자동 StageY 인터락은 연속 Place 때문에 PickerZ 하강을 차단할 수 없으므로(생산이 동시 이동을 사용),
        /// 하강 잔류 상태를 조용히 넘기지 않는 것이 유일한 방어다. 수동 StageY 이동은 인터락이 차단한다.
        /// </summary>
        private void WarnIfPickerZLeftDown()
        {
            try
            {
                BaseAxis axis = GetPickerAxis(_pickerZAxis);
                if (axis == null)
                    return;

                axis.UpdateStatus();
                double avoid = GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition");
                // IsAtTargetPosition은 AjinAxis에서 보드 Command 기준으로 오버라이드되어 있어
                // (보드 미오픈 시 상시 false, 실제 하강 잔류를 놓칠 수도 있음) 실측 위치로 직접 판정한다.
                double tolerance = ResolveAxisPositionTolerance(axis);
                if (!axis.IsMoving && Math.Abs(axis.ActualPosition - avoid) <= tolerance)
                    return;

                EventLogger.Write(EventKind.Warning, "CAL", "PLACE-Z-CAL-PICKER-Z-DOWN",
                    "PlaceZ Calibration 종료 시 PickerZ가 Avoid 위치가 아닙니다. " +
                    "Output Stage를 움직이기 전에 PickerZ를 Avoid로 복귀시키세요. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", axis=" + _pickerZAxis +
                    ", actual=" + axis.ActualPosition.ToString("F6") +
                    ", avoid=" + avoid.ToString("F6"));
            }
            catch (Exception ex)
            {
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 종료 시 PickerZ 잔류 확인 예외. error=" + ex.Message + " - Check");
            }
        }

        private bool TryConfigureCurrentPoseScanStart(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_settings == null || _calibrationTarget == null)
                {
                    reason = "캘리브레이션 목표가 아직 계산되지 않았습니다.";
                    return false;
                }

                BaseAxis zAxis = GetPickerAxis(_pickerZAxis);
                if (!IsAxisReadyForCurrentPose(zAxis, out reason))
                    return false;

                if (!IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, _calibrationTarget.PickerX))
                {
                    reason = "PickerX가 PlaceZ 캘리브레이션 목표 위치가 아닙니다. " +
                             BuildPickerAxisState(PickerAxis.PickerX, _calibrationTarget.PickerX);
                    return false;
                }

                if (!IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, _calibrationTarget.PickerY))
                {
                    reason = "PickerY가 PlaceZ 캘리브레이션 목표 위치가 아닙니다. " +
                             BuildPickerAxisState(PickerAxis.PickerY, _calibrationTarget.PickerY);
                    return false;
                }

                PickerAxis tAxis = GetPickerTAxis(_pickerIndex);
                if (!IsPickerAxisAlreadyInPosition(tAxis, _calibrationTarget.PickerT))
                {
                    reason = "PickerT가 PlaceZ 캘리브레이션 목표 위치가 아닙니다. " +
                             BuildPickerAxisState(tAxis, _calibrationTarget.PickerT);
                    return false;
                }

                if (!IsInputVisionInAvoidForCurrentPose(out reason))
                    return false;

                if (!IsOutputVisionInAvoidForCurrentPose(out reason))
                    return false;

                if (!IsOppositePickerInOutputAvoidForCurrentPose(out reason))
                    return false;

                if (!IsOutputStageInCalibrationPosition(out reason))
                    return false;

                double currentZ = zAxis.ActualPosition;
                if (!IsBetween(currentZ, _settings.StartZMm, _searchLimitPosition, ResolveAxisPositionTolerance(zAxis)))
                {
                    reason = "현재 PickerZ가 설정된 PlaceZ 검색 범위 밖입니다. currentZ=" +
                             currentZ.ToString("F6") +
                             ", startZ=" + _settings.StartZMm.ToString("F6") +
                             ", searchLimitZ=" + _searchLimitPosition.ToString("F6");
                    return false;
                }

                _scanStartPosition = currentZ;
                Result.ScanStartPosition = _scanStartPosition;
                Result.SearchLimitPosition = _searchLimitPosition;
                reason = "현재 X/Y/T/Stage/Vision 조건이 유지되어 Z 현재 위치에서 반복 테스트합니다.";
                return true;
            }
            catch (Exception ex)
            {
                reason = "현재 위치 반복 테스트 조건 확인 예외 발생. error=" + ex.Message;
                return false;
            }
        }

        private bool IsAxisReadyForCurrentPose(BaseAxis axis, out string reason)
        {
            if (axis == null)
            {
                reason = "PickerZ 축을 찾을 수 없습니다.";
                return false;
            }

            axis.UpdateStatus();
            if (!axis.IsServoOn || axis.IsAlarm || axis.IsMoving)
            {
                reason = "PickerZ 축 상태가 현재 위치 반복 테스트 조건이 아닙니다. axis=" + axis.Name +
                         ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                         ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                         ", moving=" + (axis.IsMoving ? "Y" : "N") +
                         ", actual=" + axis.ActualPosition.ToString("F6");
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsInputVisionInAvoidForCurrentPose(out string reason)
        {
            InputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
            {
                reason = "InputVisionX 현재 위치 확인에 필요한 축/레시피가 없습니다.";
                return false;
            }

            if (!stage.IsVisionXInAvoidPosition())
            {
                reason = "InputVisionX가 Avoid 위치가 아닙니다.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsOutputVisionInAvoidForCurrentPose(out string reason)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
            if (stage == null || stage.OutputCameraX == null)
            {
                reason = "OutputVisionX 현재 위치 확인에 필요한 축이 없습니다.";
                return false;
            }

            if (!stage.IsVisionXInAvoidPosition())
            {
                reason = "OutputVisionX가 Avoid 위치가 아닙니다.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsOppositePickerInOutputAvoidForCurrentPose(out string reason)
        {
            if (Side == PickerSequenceSide.Front)
            {
                if (RearPicker == null || RearPicker.IsPickerInOutputSideAvoidPosition())
                {
                    reason = string.Empty;
                    return true;
                }

                reason = "Rear Picker가 Output Avoid 위치가 아닙니다.";
                return false;
            }

            if (FrontPicker == null || FrontPicker.IsPickerInOutputSideAvoidPosition())
            {
                reason = string.Empty;
                return true;
            }

            reason = "Front Picker가 Output Avoid 위치가 아닙니다.";
            return false;
        }

        private bool IsOutputStageInCalibrationPosition(out string reason)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
            if (stage == null || stage.Recipe == null || _calibrationTarget == null)
            {
                reason = "OutputStage 현재 위치 확인에 필요한 축/레시피/목표가 없습니다.";
                return false;
            }

            BinStageAxis yAxis = _targetOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            if (!stage.IsStageAxisInPosition(yAxis, _calibrationTarget.OutputStageY, 0.05))
            {
                reason = "OutputStageY가 PlaceZ 캘리브레이션 목표 위치가 아닙니다. " +
                         stage.BuildStageAxisState(yAxis, _calibrationTarget.OutputStageY);
                return false;
            }

            if (_targetOutputSide == BinSide.Good && stage.HasStageAxis(BinStageAxis.GoodBinZ))
            {
                double targetZ = stage.Recipe.GoodStageZ.ProcessPosition;
                if (!stage.IsStageAxisInPosition(BinStageAxis.GoodBinZ, targetZ, 0.05))
                {
                    reason = "GoodStageZ가 Process 위치가 아닙니다. " +
                             stage.BuildStageAxisState(BinStageAxis.GoodBinZ, targetZ);
                    return false;
                }

                if (stage.HasStageAxis(BinStageAxis.NgBinY))
                {
                    double ngAvoidY = stage.Recipe.NGStageY.AvoidPosition;
                    if (!stage.IsStageAxisInPosition(BinStageAxis.NgBinY, ngAvoidY, 0.05))
                    {
                        reason = "Good PlaceZ 캘리브레이션에서 NGStageY가 Avoid 위치가 아닙니다. " +
                                 stage.BuildStageAxisState(BinStageAxis.NgBinY, ngAvoidY);
                        return false;
                    }
                }
            }
            else if (_targetOutputSide == BinSide.Ng && stage.HasStageAxis(BinStageAxis.GoodBinZ))
            {
                double goodAvoidZ = stage.Recipe.GoodStageZ.AvoidPosition;
                if (!stage.IsStageAxisInPosition(BinStageAxis.GoodBinZ, goodAvoidZ, 0.05))
                {
                    reason = "NG PlaceZ 캘리브레이션에서 GoodStageZ가 Avoid 위치가 아닙니다. " +
                             stage.BuildStageAxisState(BinStageAxis.GoodBinZ, goodAvoidZ);
                    return false;
                }
            }

            reason = BuildOutputStageCalibrationPositionState(stage);
            return true;
        }

        private static double ResolveAxisPositionTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.01;
        }

        public async Task<int> MoveScanStartOnlyAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            SetOptionsForManualOperation(options);
            using (QMC.CDT320.Interlocks.MotionGuardRuntime.BeginSequenceProcessMove(
                Options != null && Options.RunMode == SequenceRunMode.Auto,
                "PickerPlaceZCalibrationSequence.MoveScanStartOnlyAsync:" + (Options != null ? Options.RunMode.ToString() : "-")))
            {
                try
                {
                    CurrentStep = PlaceZCalibrationStep.CheckReady;
                    int result = CheckReady();
                    if (result != 0) return result;

                    CurrentStep = PlaceZCalibrationStep.ReserveArea;
                    result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "PlaceZCalibrationMoveStart");

                    CurrentStep = PlaceZCalibrationStep.MoveZSafe;
                    result = await PrepareSafeStartPositionAsync("PlaceZ Calibration Start 이동 전 안전 위치 이동", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = PlaceZCalibrationStep.MoveScanStart;
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        _scanStartPosition,
                        "PlaceZ Calibration Scan Start",
                        ct,
                        SearchTargetName,
                        false,
                        true).ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "PlaceZ Calibration scan start move complete.";
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
                    return Fail("PLACE-Z-CAL-MOVE-START-EX", Name,
                        "PlaceZ Calibration Scan Start 이동 예외 발생. error=" + ex.Message);
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
                "PickerPlaceZCalibrationSequence.MoveAvoidOnlyAsync:" + (Options != null ? Options.RunMode.ToString() : "-")))
            {
                try
                {
                    CurrentStep = PlaceZCalibrationStep.CheckReady;
                    int result = CheckReady();
                    if (result != 0) return result;

                    CurrentStep = PlaceZCalibrationStep.MoveAvoid;
                    result = await MovePickerAxisAndVerifyAsync(
                        _pickerZAxis,
                        GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition"),
                        "PlaceZ Calibration PickerZ Avoid",
                        ct,
                        "AvoidPosition",
                        false,
                        true).ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "PlaceZ Calibration avoid move complete.";
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
                    return Fail("PLACE-Z-CAL-AVOID-EX", Name,
                        "PlaceZ Calibration PickerZ Avoid 이동 예외 발생. error=" + ex.Message);
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
                    return Fail("PLACE-Z-CAL-MACHINE", Name, "Machine is null.");
                if (Context.Machine.VisionUnit == null || Context.Machine.VisionUnit.Config == null)
                    return Fail("PLACE-Z-CAL-VISION", Name, "VisionUnit calibration config is null.");
                if (Side == PickerSequenceSide.Front && FrontPicker == null)
                    return Fail("PLACE-Z-CAL-FRONT", Name, "FrontPickerUnit is null.");
                if (Side == PickerSequenceSide.Rear && RearPicker == null)
                    return Fail("PLACE-Z-CAL-REAR", Name, "RearPickerUnit is null.");
                if (!IsPickerSideEnabled())
                    return Fail("PLACE-Z-CAL-SIDE-DISABLED", Name, "Picker side is disabled. side=" + Side);
                if (!IsPickerIndexEnabled(_pickerIndex))
                    return Fail("PLACE-Z-CAL-PICKER-DISABLED", Name, "Picker is disabled. pickerNo=" + _pickerNo);

                Context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                Context.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                Context.Machine.VisionUnit.Config.CalibrationData.PlaceZ.EnsureObjects();
                _settings = Context.Machine.VisionUnit.Config.CalibrationData.PlaceZ.Settings;
                _settings.EnsureDefaults();
                SetCalibrationMotion(_settings.Motion);

                _pickerZAxis = GetPickerZAxis(_pickerIndex);
                _oldPlacePosition = GetPickerTeachingPosition(_pickerZAxis, "PlacePosition");
                double avoid = GetPickerTeachingPosition(_pickerZAxis, "AvoidPosition");
                double downSign = Math.Sign(_oldPlacePosition - avoid);
                if (downSign == 0.0)
                    return Fail("PLACE-Z-CAL-Z-TEACH", Name,
                        "PlacePosition과 AvoidPosition이 같아 하강 방향을 계산할 수 없습니다. " +
                        "side=" + Side + ", pickerNo=" + _pickerNo +
                        ", pick=" + _oldPlacePosition.ToString("F6") +
                        ", avoid=" + avoid.ToString("F6"));

                OutputStageUnit outputStage = Context.Machine.OutputStageUnit;
                if (outputStage == null || outputStage.Recipe == null)
                    return Fail("PLACE-Z-CAL-OUTPUT-STAGE", Name, "OutputStageUnit or recipe is null.");

                outputStage.Recipe.EnsurePositionObjects();
                string offsetReason;
                if (!PickerCoordinateTransformHelper.TryResolveOutputVisionToPickerOffsets(
                    Context.Machine,
                    Side,
                    _pickerIndex,
                    _targetOutputSide,
                    out _outputVisionToPickerX,
                    out _outputVisionToPickerY,
                    out offsetReason))
                {
                    return Fail("PLACE-Z-CAL-OFFSET", Name,
                        "PlaceZ Calibration output vision to picker offset resolve failed. " +
                        "side=" + Side +
                        ", outputSide=" + _targetOutputSide +
                        ", pickerNo=" + _pickerNo +
                        ", reason=" + offsetReason);
                }

                _outputStageBaseY = _targetOutputSide == BinSide.Ng
                    ? outputStage.Recipe.NGStageY.ProcessPosition
                    : outputStage.Recipe.GoodStageY.ProcessPosition;

                _calibrationTarget = PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                    Context.Machine,
                    Side,
                    _pickerIndex,
                    "PlaceZCalibration",
                    "PLACE-Z-CAL",
                    _targetOutputSide,
                    _outputStageBaseY,
                    _settings.PositionOffsetXmm,
                    _settings.PositionOffsetYmm,
                    outputStage.Recipe.VisionX.ProcessPosition,
                    _outputVisionToPickerX,
                    _outputVisionToPickerY);

                _scanStartPosition = _settings.StartZMm;
                _searchDirection = downSign;
                _searchLimitPosition = _scanStartPosition + (downSign * Math.Abs(_settings.SearchMaxDistanceMm));

                // 스캔 시작 Z가 기존 PlacePosition보다 깊으면 검색이 시작되기 전에 일반 속도로 표면을 누르게 된다.
                // 설정 오입력(부호/자릿수 실수) 방어 — 축을 움직이기 전에 차단한다.
                // (현재 위치 반복 테스트 경로는 이후 TryConfigureCurrentPoseScanStart가 시작점을 실측 Z로 덮어쓴다.)
                if (downSign * (_scanStartPosition - _oldPlacePosition) > 0.0)
                    return Fail("PLACE-Z-CAL-START-Z", Name,
                        "스캔 시작 Z(StartZMm)가 기존 PlacePosition보다 깊어 시작할 수 없습니다. 설정을 확인하세요. " +
                        "startZ=" + _scanStartPosition.ToString("F6") +
                        ", oldPlace=" + _oldPlacePosition.ToString("F6"));

                Result.OldPlacePosition = _oldPlacePosition;
                Result.ScanStartPosition = _scanStartPosition;
                Result.SearchLimitPosition = _searchLimitPosition;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PLACE-Z-CAL-CHECK-EX", Name, "PlaceZ Calibration 준비 확인 예외 발생. error=" + ex.Message);
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
                    return Fail("PLACE-Z-CAL-RESOURCE", Name, "Picker resource acquire failed. side=" + Side);

                _outputPlaceLease = await AcquireResourceAsync(SequenceResourceKind.OutputPlaceArea, Name + ":OutputPlaceArea", ct).ConfigureAwait(false);
                if (_outputPlaceLease == null)
                    return Fail("PLACE-Z-CAL-RESOURCE", Name, "OutputPlaceArea resource acquire failed.");

                SequenceResourceKind stageResource = _targetOutputSide == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;
                _outputStageLease = await AcquireResourceAsync(stageResource, Name + ":OutputStageArea:" + _targetOutputSide, ct).ConfigureAwait(false);
                if (_outputStageLease == null)
                    return Fail("PLACE-Z-CAL-RESOURCE", Name, _targetOutputSide + " OutputStageArea resource acquire failed.");

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PLACE-Z-CAL-RESOURCE-EX", Name, "PlaceZ Calibration 리소스 점유 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareSafeStartPositionAsync(string description, CancellationToken ct)
        {
            // 시작 안전이동은 forceMove를 쓰지 않는다: 이미 Avoid(정지+무알람+톨러런스)면 확인만 하고 통과한다.
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " - PickerZ all Avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                description + " - PickerY Avoid",
                ct,
                "AvoidPosition;PickerPhase=SafeY",
                false,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveAllPickerTToAvoidAndVerifyAsync(
                description + " - PickerT all Avoid",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveOppositePickerToAvoidAndVerifyAsync(description + " - Opposite Picker Avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await EnsureInputOutputVisionAvoidForStartAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveOutputStageToCalibrationProcessAsync(description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveCurrentPickerToCalibrationTargetAsync(description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PlaceZCalibration",
                Name + " 안전 시작 위치 확인 완료. side=" + Side +
                ", outputSide=" + _targetOutputSide +
                ", pickerNo=" + _pickerNo +
                ", targetOutputStageY=" + (_calibrationTarget != null ? _calibrationTarget.OutputStageY.ToString("F6") : "-") +
                ", targetPickerX=" + (_calibrationTarget != null ? _calibrationTarget.PickerX.ToString("F6") : "-") +
                ", targetPickerY=" + (_calibrationTarget != null ? _calibrationTarget.PickerY.ToString("F6") : "-") +
                ", targetPickerT=" + (_calibrationTarget != null ? _calibrationTarget.PickerT.ToString("F6") : "-") +
                ", targetPickerZ=" + _pickerZAxis +
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
                return Fail("PLACE-Z-CAL-INPUT-VISION-MISSING", "InputStageUnit",
                    "PlaceZ Calibration start InputVisionX Avoid move requires axis/recipe.");

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (stage.CameraX.IsAtTargetPosition(target, 0.0))
                return 0;

            return await MoveInputStageAxisWithCalibrationMotionAsync(
                stage,
                WaferStageAxis.VisionX,
                target,
                "PlaceZ Calibration InputVisionX Avoid",
                ct).ConfigureAwait(false);
        }

        private async Task<int> EnsureOutputVisionAvoidForStartAsync(CancellationToken ct)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
            if (stage == null ||
                stage.OutputCameraX == null ||
                stage.Recipe == null ||
                stage.Recipe.VisionX == null)
                return Fail("PLACE-Z-CAL-OUTPUT-VISION-MISSING", "OutputStageUnit",
                    "PlaceZ Calibration start OutputVisionX Avoid move requires axis/recipe.");

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (stage.OutputCameraX.IsAtTargetPosition(target, 0.0))
                return 0;

            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveMoveTimeout(),
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("PLACE-Z-CAL-OUTPUT-VISION-AVOID", "OutputStageUnit",
                    "PlaceZ Calibration OutputVisionX Avoid move failed. result=" + result);

            return 0;
        }

        private async Task<int> MoveOutputStageToCalibrationProcessAsync(string description, CancellationToken ct)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null;
            if (stage == null || stage.Recipe == null || _calibrationTarget == null)
                return Fail("PLACE-Z-CAL-OUTPUT-STAGE-MISSING", "OutputStageUnit",
                    "PlaceZ Calibration OutputStage process move requires axis/recipe/target.");

            var options = OutputStageSequenceOptions.Default();
            options.Side = _targetOutputSide;
            options.RunMode = Options != null ? Options.RunMode : SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;
            options.FineMove = Options != null && Options.FineMove;
            options.MoveTimeoutMs = ResolveMoveTimeout();
            options.KeepVisionXAvoidOnProcessMove = true;

            // [GoodStageZ 왕복 제거 2026-08-06] ★캘 전용★ — 자동 운전은 이 옵션을 켜지 않으므로 무영향.
            // PlaceZ 캘은 같은 Y 목표(실측 287.944)로 픽커만 바꿔가며 반복하는데,
            // Y 를 0.05~0.25mm 옮기려고 GoodStageZ 를 32.953mm 내렸다 올리는 왕복이 매번 발생했다.
            // 인터락(OutputStageInterlockRules:1441)은 이 목표에서 Z 가 Process 여도 Y 이동을 허용한다.
            // 옵션이 켜져도 인터락이 Avoid 를 요구하면 시퀀스가 스스로 수행하므로 이중 안전이다.
            options.SkipTargetStageZAvoidBeforeYWhenInterlockAllows = true;

            int result = await new OutputStageSequence(Context)
                .RunMoveProcessAsync(ct, options)
                .ConfigureAwait(false);
            if (result != 0)
                return Fail("PLACE-Z-CAL-OUTPUT-PROCESS", stage.Name,
                    "PlaceZ Calibration OutputStage " + _targetOutputSide +
                    " process/avoid sequence failed. result=" + result +
                    ", " + stage.DescribeOutputStageInterlockState(_targetOutputSide));

            // ================================================================
            // [GoodStageZ 왕복 제거 2026-08-06]  ★실장비 미검증 — 실장비에서 테스트 필요★
            //
            // 사용자 지적(2026-08-06): "placeZ 캘리브레이션 할때 goodStageZ축이 계속
            //   내려갔다 올라갔다 하고 있다. 그냥 한자리에서 움직이게 해줘."
            //
            // 기존 조건: 픽커마다 무조건 [GoodStageZ→Avoid → GoodBinY 이동 → GoodStageZ→Process]
            //   를 수행해 Z 왕복이 반복됐다.
            //
            // 기존 인터락 규칙(새로 만들지 않고 그대로 재사용):
            //   OutputStageInterlockRules.VerifyGoodStageYMechanicalClear(:1441)
            //     requiresGoodZAvoid = (AxisHome) || IsGoodStageYTargetRequiringGoodZAvoid(target)
            //     · 목표가 Avoid/Load/Unload/Home  → GoodStageZ Avoid 필수
            //     · 그 외(캘 계산 좌표 등)          → GoodStageZ 가 Avoid "또는 Process" 면 허용
            //   캘 타겟(_calibrationTarget.OutputStageY)은 Avoid/Load/Unload 가 아니므로
            //   GoodStageZ 를 Process 에 둔 채로 Y 이동이 허용된다.
            //
            // 현재 기준: 인터락이 실제로 Avoid 를 요구할 때만 내린다.
            //   판정은 인터락 함수를 직접 호출한다(중복 구현 금지 — 규칙이 어긋나면 안 된다).
            // ================================================================
            if (_targetOutputSide == BinSide.Good && stage.HasStageAxis(BinStageAxis.GoodBinZ))
            {
                bool requiresGoodZAvoid = OutputStageInterlockRules.IsGoodStageYTargetRequiringGoodZAvoid(
                    stage, _calibrationTarget.OutputStageY);
                bool goodZAlreadyAllowed = stage.IsGoodStageZInAvoidOrProcessPosition();

                if (!requiresGoodZAvoid && goodZAlreadyAllowed)
                {
                    WriteLog("PlaceZCalibration",
                        Name + " GoodStageZ Avoid 생략: 캘 타겟은 Avoid/Load/Unload 가 아니라" +
                        " GoodStageZ 가 Process 여도 GoodStageY 이동이 허용됩니다. " +
                        "targetOutputStageY=" + _calibrationTarget.OutputStageY.ToString("F3") +
                        ", " + stage.BuildStageAxisState(
                            BinStageAxis.GoodBinZ, stage.Recipe.GoodStageZ.ProcessPosition) + " - Ok");
                }
                else
                {
                    WriteLog("PlaceZCalibration",
                        Name + " GoodStageZ Avoid 수행. requiresGoodZAvoid=" + requiresGoodZAvoid +
                        ", goodZAlreadyAllowed=" + goodZAlreadyAllowed + " - Check");

                    int zAvoid = await MoveOutputStageAxisWithCalibrationMotionAsync(
                        stage,
                        BinStageAxis.GoodBinZ,
                        stage.Recipe.GoodStageZ.AvoidPosition,
                        description + " GoodStageZ Avoid Before GoodY Cal Target",
                        ct,
                        "PlaceZCalibration;GoodStageZ;AvoidBeforeY").ConfigureAwait(false);
                    if (zAvoid != 0)
                        return zAvoid;
                }
            }

            BinStageAxis yAxis = _targetOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            result = await MoveOutputStageAxisWithCalibrationMotionAsync(
                stage,
                yAxis,
                _calibrationTarget.OutputStageY,
                description + " OutputStageY Place Cal Target",
                ct,
                "PlaceZCalibration;OutputStageY;OutputSide=" + _targetOutputSide).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (_targetOutputSide == BinSide.Good && stage.HasStageAxis(BinStageAxis.GoodBinZ))
            {
                return await MoveOutputStageAxisWithCalibrationMotionAsync(
                    stage,
                    BinStageAxis.GoodBinZ,
                    stage.Recipe.GoodStageZ.ProcessPosition,
                    description + " GoodStageZ Process For PlaceZ Cal",
                    ct,
                    "PlaceZCalibration;GoodStageZ;Process").ConfigureAwait(false);
            }

            return 0;
        }

        private int ResolveInitialVacuumOnDelayMs()
        {
            int configured = _settings != null ? _settings.VacuumOnDelayMs : 0;
            return Math.Max(configured, MinInitialVacuumOnDelayMs);
        }

        private string BuildOutputStageCalibrationPositionState(OutputStageUnit stage)
        {
            if (stage == null || stage.Recipe == null || _calibrationTarget == null)
                return "outputStageState=unknown";

            BinStageAxis yAxis = _targetOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            string state = stage.BuildStageAxisState(yAxis, _calibrationTarget.OutputStageY);

            if (_targetOutputSide == BinSide.Good)
            {
                if (stage.HasStageAxis(BinStageAxis.GoodBinZ))
                    state += ", " + stage.BuildStageAxisState(BinStageAxis.GoodBinZ, stage.Recipe.GoodStageZ.ProcessPosition);
                if (stage.HasStageAxis(BinStageAxis.NgBinY))
                    state += ", " + stage.BuildStageAxisState(BinStageAxis.NgBinY, stage.Recipe.NGStageY.AvoidPosition);
            }
            else if (stage.HasStageAxis(BinStageAxis.GoodBinZ))
            {
                state += ", " + stage.BuildStageAxisState(BinStageAxis.GoodBinZ, stage.Recipe.GoodStageZ.AvoidPosition);
            }

            return state;
        }

        private async Task<int> MoveCurrentPickerToCalibrationTargetAsync(string description, CancellationToken ct)
        {
            if (_calibrationTarget == null)
                return Fail("PLACE-Z-CAL-PICKER-TARGET", Name, "PlaceZ Calibration picker target is null.");

            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _calibrationTarget.PickerX,
                description + " Picker Output Place Cal X",
                ct,
                SearchTargetName,
                true,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _calibrationTarget.PickerY,
                description + " Picker Output Place Cal Y",
                ct,
                SearchTargetName,
                true,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MovePickerAxisAndVerifyAsync(
                GetPickerTAxis(_pickerIndex),
                _calibrationTarget.PickerT,
                description + " Picker Output Place Cal T",
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
                return Fail("PLACE-Z-CAL-STAGE-NULL", "InputStageUnit", description + " stage is null.");

            ct.ThrowIfCancellationRequested();
            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            int result = await stage.MoveInputStageAxisCommandWithMotion(
                axis,
                target,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration).ConfigureAwait(false);
            if (result != 0)
                return Fail("PLACE-Z-CAL-STAGE-MOVE", stage.Name,
                    description + " command failed. result=" + result +
                    ", " + BuildInputStageAxisState(stage, axis, target));

            // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            return 0;
        }

        private async Task<int> MoveOutputStageAxisWithCalibrationMotionAsync(
            OutputStageUnit stage,
            BinStageAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName)
        {
            if (stage == null)
                return Fail("PLACE-Z-CAL-OUTPUT-STAGE-NULL", "OutputStageUnit", description + " stage is null.");

            ct.ThrowIfCancellationRequested();
            CalibrationMotionSettings motion = ResolveCalibrationMotion();
            int result = await stage.MoveStageAxisCommandWithMotion(
                axis,
                target,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                targetName).ConfigureAwait(false);
            if (result != 0)
                return Fail("PLACE-Z-CAL-OUTPUT-STAGE-MOVE", stage.Name,
                    description + " command failed. result=" + result +
                    ", axis=" + axis +
                    ", target=" + target.ToString("F6") +
                    ", " + stage.DescribeOutputStageInterlockState(_targetOutputSide));

            // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
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
                return Fail("PLACE-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);

            Stopwatch totalWatch = Stopwatch.StartNew();
            try
            {
                if (_settings.FailIfFlowAlreadyOn)
                {
                    int offResult = await WaitPickerFlowStateForCalibrationAsync(
                        false,
                        "PlaceZ Calibration before coarse search",
                        ct).ConfigureAwait(false);
                    if (offResult != 0)
                        return offResult;
                }

                PlaceZFlowSearchResult coarse = await SearchFlowPassAsync(
                    "Coarse",
                    _scanStartPosition,
                    _searchLimitPosition,
                    _settings.CoarseSearchVelocityMmPerSec,
                    _settings.CoarseSearchAccelerationMmPerSec2,
                    _settings.CoarseSearchDecelerationMmPerSec2,
                    ct).ConfigureAwait(false);
                if (!coarse.Success)
                    return coarse.ResultCode;

                var fineDetections = new List<double>();
                double lastDetected = coarse.DetectedPosition;
                int repeatCount = Math.Max(1, _settings.RepeatCount);

                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration fine correction starts in current aligned pose. " +
                    "Only selected PickerZ will move for BackOff/Fine Search. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", pickerZAxis=" + _pickerZAxis +
                    ", coarseFlow=" + coarse.DetectedPosition.ToString("F6") +
                    ", repeatCount=" + repeatCount +
                    ", fineVelocity=" + _settings.FineSearchVelocityMmPerSec.ToString("F6") +
                    ", fineAcceleration=" + _settings.FineSearchAccelerationMmPerSec2.ToString("F6") +
                    ", fineDeceleration=" + _settings.FineSearchDecelerationMmPerSec2.ToString("F6") + " - Check");

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

                    PlaceZFlowSearchResult fine = await SearchFlowPassAsync(
                        "Fine#" + index,
                        backOffTarget,
                        _searchLimitPosition,
                        _settings.FineSearchVelocityMmPerSec,
                        _settings.FineSearchAccelerationMmPerSec2,
                        _settings.FineSearchDecelerationMmPerSec2,
                        ct).ConfigureAwait(false);
                    if (!fine.Success)
                        return fine.ResultCode;

                    fineDetections.Add(fine.DetectedPosition);
                    lastDetected = fine.DetectedPosition;

                    string toleranceReason;
                    if (!AreFineDetectionsWithinTolerance(fineDetections, out toleranceReason))
                    {
                        return Fail("PLACE-Z-CAL-REPEAT-TOLERANCE", Name,
                            "PlaceZ Calibration fine search repeat tolerance failed. " + toleranceReason +
                            ", tolerance=" + _settings.RepeatToleranceMm.ToString("F6") +
                            ", detections=" + FormatDetections(fineDetections));
                    }
                }

                _detectedFlowPosition = AverageDetections(fineDetections);
                _savedPlacePosition = CalculateSavedPlacePosition(_detectedFlowPosition);
                _detectElapsedMs = (int)Math.Min(int.MaxValue, totalWatch.ElapsedMilliseconds);
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPlacePosition = _savedPlacePosition;
                Result.DetectElapsedMs = _detectElapsedMs;

                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration search complete. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", coarseFlow=" + coarse.DetectedPosition.ToString("F6") +
                    ", fineFlows=" + FormatDetections(fineDetections) +
                    ", finalFlow=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPlaceZ=" + _savedPlacePosition.ToString("F6") +
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
                return Fail("PLACE-Z-CAL-SEARCH-RESET-EX", Name,
                    "PlaceZ Calibration search with reset exception. error=" + ex.Message);
            }
            finally
            {
                totalWatch.Stop();
            }
        }

        private async Task<PlaceZFlowSearchResult> SearchFlowPassAsync(
            string passName,
            double searchStart,
            double searchLimit,
            double velocity,
            double acceleration,
            double deceleration,
            CancellationToken ct)
        {
            var result = new PlaceZFlowSearchResult();
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
            {
                result.ResultCode = Fail("PLACE-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);
                return result;
            }

            if (IsPickerSimulationOrDryRun())
            {
                double simulatedFlow = ResolveSimulatedFlowPosition();
                if (!IsBetween(simulatedFlow, searchStart, searchLimit, 0.000001))
                {
                    result.ResultCode = Fail("PLACE-Z-CAL-SIM-FLOW-RANGE", Name,
                        "PlaceZ Calibration simulation Flow position is outside search range. pass=" + passName +
                        ", simulatedFlow=" + simulatedFlow.ToString("F6") +
                        ", start=" + searchStart.ToString("F6") +
                        ", limit=" + searchLimit.ToString("F6"));
                    return result;
                }

                Stopwatch simWatch = Stopwatch.StartNew();
                int moveResult = await MovePickerAxisAndVerifyAsync(
                    _pickerZAxis,
                    simulatedFlow,
                    "PlaceZ Calibration " + passName + " simulated Flow Z-only",
                    ct,
                    SearchTargetName,
                    false,
                    true).ConfigureAwait(false);
                simWatch.Stop();
                if (moveResult != 0)
                {
                    result.ResultCode = moveResult;
                    return result;
                }

                axis.UpdateStatus();
                result.DetectedPosition = axis.ActualPosition;
                result.FirstFlowOnPosition = result.DetectedPosition;
                result.StopPosition = result.DetectedPosition;
                result.ElapsedMs = (int)Math.Min(int.MaxValue, simWatch.ElapsedMilliseconds);
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration " + passName + " simulated Flow detected. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", start=" + searchStart.ToString("F6") +
                    ", limit=" + searchLimit.ToString("F6") +
                    ", flow=" + result.DetectedPosition.ToString("F6") + " - Ok");
                return result;
            }

            string interlockReason;
            if (!MotionGuardRuntime.VerifyAxisTeachingMove(axis, searchLimit, SearchTargetName, out interlockReason))
            {
                result.ResultCode = Fail("PLACE-Z-CAL-SEARCH-INTERLOCK", Name,
                    "PlaceZ Calibration search move blocked. pass=" + passName +
                    ", side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", target=" + searchLimit.ToString("F6") +
                    ". " + interlockReason);
                return result;
            }

            int maxAttempts = Math.Max(1, Math.Min(3, Math.Max(1, _settings.RepeatCount)));
            double currentStart = searchStart;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                string attemptName = passName;
                if (attempt > 1)
                    attemptName += "/Retry#" + (attempt - 1);

                Stopwatch watch = Stopwatch.StartNew();
                bool firstFlowOn = false;
                double firstFlowOnPosition = 0.0;
                double stopPosition = 0.0;
                bool stopCommanded = false;
                bool lastFlowState = ReadPickerFlowState(_pickerNo);

                // 기존 조건: Task<int> moveTask = MovePickerAxisCommandWithMotionAsync(...) — 블로킹 이동을
                //           백그라운드 태스크로 발행하고 Flow ON에서 정지시켰다. 그러면 태스크가 축 레이어의
                //           완주 검증(Command≠Target → -5)에 걸려 유닛 레벨 PK-MOVE Critical 오탐 알람이 발생했다
                //           (PickUpZ와 동일 구조 — 2026-08-12 사용자 지시로 두 캘리브레이션 함께 전환).
                // 현재 기준: 명령 전용 이동(발행 즉시 리턴, 완주 검증 없음)으로 전환한다.
                //           탐색형 이동(끝까지 안 가는 게 정상)에 맞는 구조이며, 도달/정지 판정은
                //           아래 감시 루프가 IsMoving으로 직접 수행한다. 전달 속도/가감속은 기존 경로와
                //           동일하게 최종값 그대로 보드에 적용된다(재스케일 없음 — MotionSpeedScale 불변).
                // To do: [Z캘 명령 전용 전환] Flow 탐색 하강을 명령 전용 API로 발행.
                int searchCommandResult = await MovePickerAxisCommandOnlyAsync(
                    _pickerZAxis,
                    searchLimit,
                    velocity,
                    acceleration,
                    deceleration,
                    SearchTargetName).ConfigureAwait(false);
                if (searchCommandResult != 0)
                {
                    result.ResultCode = Fail("PLACE-Z-CAL-Z-MOVE", Name,
                        "PlaceZ Calibration search move command failed. pass=" + attemptName +
                        ", result=" + searchCommandResult +
                        ", " + BuildPickerAxisState(_pickerZAxis, searchLimit));
                    return result;
                }

                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration " + attemptName + " Flow 검색 시작. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", start=" + currentStart.ToString("F6") +
                    ", limit=" + searchLimit.ToString("F6") +
                    ", velocity=" + velocity.ToString("F6") +
                    ", acceleration=" + acceleration.ToString("F6") +
                    ", deceleration=" + deceleration.ToString("F6") +
                    ", initialFlow=" + (lastFlowState ? "ON" : "OFF") +
                    ", pollMs=" + _settings.FlowPollIntervalMs + " - Check");

                try
                {
                    // 기존 조건: while (!moveTask.IsCompleted) — 블로킹 태스크 완료를 루프 종료 조건으로 썼다.
                    // 현재 기준: 명령 전용 이동은 태스크가 없으므로 축 상태(IsMoving)로 종료를 판정한다.
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (Context != null)
                            Context.StopIfCycleStopRequested(Name + ".SearchFlow." + attemptName);

                        if (_immediateStopRequested)
                        {
                            stopCommanded = true;
                            StopPickerZAxis("PlaceZ Calibration 검색 중 즉시 정지 요청");
                            await WaitPickerZStoppedAsync(attemptName + " 즉시 정지 요청").ConfigureAwait(false);
                            throw new OperationCanceledException();
                        }

                        // 현재 기준: 명령 전용 이동은 축 내부 대기 루프가 없으므로 감시 루프가 상태를 직접 갱신한다.
                        axis.UpdateStatus();

                        bool currentFlowState = ReadPickerFlowState(_pickerNo);
                        if (currentFlowState != lastFlowState)
                        {
                            axis.UpdateStatus();
                            WriteLog("PlaceZCalibration",
                                "PlaceZ Calibration " + attemptName + " Flow 상태 변화 감지. " +
                                "side=" + Side +
                                ", outputSide=" + _targetOutputSide +
                                ", pickerNo=" + _pickerNo +
                                ", flow=" + (currentFlowState ? "ON" : "OFF") +
                                ", z=" + axis.ActualPosition.ToString("F6") + " - Check");
                            lastFlowState = currentFlowState;
                        }

                        if (currentFlowState)
                        {
                            axis.UpdateStatus();
                            firstFlowOn = true;
                            firstFlowOnPosition = axis.ActualPosition;
                            stopCommanded = true;
                            StopPickerZAxis("Flow ON 감지");
                            WriteLog("PlaceZCalibration",
                                "PlaceZ Calibration " + attemptName + " Flow ON 감지로 PickerZ 즉시 정지 명령. " +
                                "side=" + Side +
                                ", outputSide=" + _targetOutputSide +
                                ", pickerNo=" + _pickerNo +
                                ", firstOnZ=" + firstFlowOnPosition.ToString("F6") + " - Stop");
                            break;
                        }

                        // 현재 기준: searchLimit 도달(또는 외부 정지)로 축이 멈추면 Flow 미감지로 루프를 종료한다.
                        //           발행 직후 보드 in-motion 반영 지연으로 인한 오판을 막기 위해 200ms 이후부터 판정한다.
                        if (watch.ElapsedMilliseconds >= 200 && !axis.IsMoving)
                            break;

                        if (watch.ElapsedMilliseconds > _settings.Motion.MoveTimeoutMs)
                        {
                            stopCommanded = true;
                            StopPickerZAxis("Flow 검색 타임아웃");
                            break;
                        }

                        await Task.Delay(_settings.FlowPollIntervalMs, ct).ConfigureAwait(false);
                    }

                    // 기존 조건: int moveResult = stopCommanded ? await WaitMoveTaskAfterStopAsync(moveTask, ...) : await moveTask;
                    //           (블로킹 이동 태스크의 결과 회수 — 완주 검증 -5가 여기서 튀어나왔다)
                    // 현재 기준: 명령 전용 이동은 태스크가 없다. 발행 실패는 발행 시점에 이미 처리했고,
                    //           여기서는 정지/도달 후 축이 완전히 멈출 때까지만 대기한다.
                    await WaitPickerZStoppedAsync(attemptName + " 검색 종료 후 정지 대기").ConfigureAwait(false);
                    axis.UpdateStatus();

                    // 정지 명령 후에도 축이 여전히 이동 중이면 현재 위치를 측정값으로 신뢰할 수 없다.
                    // (정지 실패/지연 상태에서 채택된 깊은 값이 PlacePosition으로 저장되는 것을 차단)
                    if (stopCommanded && axis.IsMoving)
                    {
                        StopPickerZAxis(attemptName + " 정지 실패 재정지");
                        result.ResultCode = Fail("PLACE-Z-CAL-STOP-FAILED", Name,
                            "PlaceZ Calibration 정지 명령 후에도 PickerZ가 이동 중이라 측정을 무효화합니다. pass=" + attemptName +
                            ", side=" + Side +
                            ", pickerNo=" + _pickerNo +
                            ", actual=" + axis.ActualPosition.ToString("F6"));
                        return result;
                    }

                    if (!firstFlowOn && ReadPickerFlowState(_pickerNo))
                    {
                        firstFlowOn = true;
                        firstFlowOnPosition = axis.ActualPosition;
                    }

                    if (firstFlowOn)
                    {
                        stopPosition = axis.ActualPosition;
                        bool stable = await WaitPickerFlowStableAfterStopAsync(
                            attemptName,
                            firstFlowOnPosition,
                            stopPosition,
                            velocity,
                            ct).ConfigureAwait(false);

                        if (stable)
                        {
                            result.FirstFlowOnPosition = firstFlowOnPosition;
                            result.StopPosition = stopPosition;
                            result.DetectedPosition = stopPosition;
                            result.ElapsedMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                            WriteLog("PlaceZCalibration",
                                "PlaceZ Calibration " + attemptName + " Flow detected after immediate stop. side=" + Side +
                                ", outputSide=" + _targetOutputSide +
                                ", pickerNo=" + _pickerNo +
                                ", start=" + currentStart.ToString("F6") +
                                ", limit=" + searchLimit.ToString("F6") +
                                ", velocity=" + velocity.ToString("F6") +
                                ", acceleration=" + acceleration.ToString("F6") +
                                ", deceleration=" + deceleration.ToString("F6") +
                                ", firstOnZ=" + firstFlowOnPosition.ToString("F6") +
                                ", stopZ=" + stopPosition.ToString("F6") +
                                ", stopOverrun=" + (stopPosition - firstFlowOnPosition).ToString("F6") +
                                ", stableMs=" + _settings.FlowStableMs +
                                ", pollMs=" + _settings.FlowPollIntervalMs +
                                ", elapsedMs=" + result.ElapsedMs + " - Ok");
                            return result;
                        }

                        WriteLog("PlaceZCalibration",
                            "PlaceZ Calibration " + attemptName + " Flow unstable after stop. BackOff/Blow reset and retry. side=" + Side +
                            ", outputSide=" + _targetOutputSide +
                            ", pickerNo=" + _pickerNo +
                            ", firstOnZ=" + firstFlowOnPosition.ToString("F6") +
                            ", stopZ=" + stopPosition.ToString("F6") +
                            ", attempt=" + attempt +
                            ", maxAttempts=" + maxAttempts + " - Check");

                        if (attempt < maxAttempts)
                        {
                            double backOffTarget = CalculateBackOffTarget(stopPosition);
                            int resetResult = await RunBackOffBlowResetAsync(
                                stopPosition,
                                backOffTarget,
                                attempt,
                                ct,
                                "unstable flow retry").ConfigureAwait(false);
                            if (resetResult != 0)
                            {
                                result.ResultCode = resetResult;
                                return result;
                            }

                            currentStart = backOffTarget;
                            continue;
                        }

                        result.ResultCode = Fail("PLACE-Z-CAL-FLOW-UNSTABLE", Name,
                            "PlaceZ Calibration Flow was detected but not stable after immediate stop. pass=" + attemptName +
                            ", side=" + Side +
                            ", outputSide=" + _targetOutputSide +
                            ", pickerNo=" + _pickerNo +
                            ", firstOnZ=" + firstFlowOnPosition.ToString("F6") +
                            ", stopZ=" + stopPosition.ToString("F6") +
                            ", stableMs=" + _settings.FlowStableMs +
                            ", attempts=" + maxAttempts);
                        return result;
                    }

                    // 기존 조건: if (moveResult != 0) → PLACE-Z-CAL-Z-MOVE 실패
                    //           (블로킹 태스크 결과 검사 — 명령 전용 전환으로 발행 결과는 발행 시점에 검사한다)

                    result.ResultCode = Fail("PLACE-Z-CAL-FLOW-NOT-DETECTED", Name,
                        "PlaceZ Calibration Flow was not detected. pass=" + attemptName +
                        ", side=" + Side +
                        ", outputSide=" + _targetOutputSide +
                        ", pickerNo=" + _pickerNo +
                        ", start=" + currentStart.ToString("F6") +
                        ", limit=" + searchLimit.ToString("F6") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + _settings.Motion.MoveTimeoutMs);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    StopPickerZAxis("Flow 검색 중 취소");
                    await WaitPickerZStoppedAsync(attemptName + " 취소 후 정지 대기").ConfigureAwait(false);
                    throw;
                }
                catch (SequenceStopException)
                {
                    StopPickerZAxis("Flow 검색 중 시퀀스 정지");
                    await WaitPickerZStoppedAsync(attemptName + " 정지 후 정지 대기").ConfigureAwait(false);
                    throw;
                }
                catch
                {
                    StopPickerZAxis("Flow 검색 중 예외");
                    await WaitPickerZStoppedAsync(attemptName + " 예외 후 정지 대기").ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    watch.Stop();
                }
            }

            result.ResultCode = Fail("PLACE-Z-CAL-FLOW-NOT-DETECTED", Name,
                "PlaceZ Calibration Flow search ended without result. pass=" + passName +
                ", side=" + Side +
                ", outputSide=" + _targetOutputSide +
                ", pickerNo=" + _pickerNo +
                ", start=" + searchStart.ToString("F6") +
                ", limit=" + searchLimit.ToString("F6"));
            return result;
        }

        private async Task<bool> WaitPickerFlowStableAfterStopAsync(
            string passName,
            double firstFlowOnPosition,
            double stopPosition,
            double velocity,
            CancellationToken ct)
        {
            int stableMs = Math.Max(0, _settings.FlowStableMs);
            int pollMs = Math.Max(1, _settings.FlowPollIntervalMs);
            if (stableMs <= 0)
                return ReadPickerFlowState(_pickerNo);

            int timeoutMs = Math.Max(stableMs + 100, stableMs + (pollMs * 4));
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            DateTime? stableSinceUtc = null;

            while (DateTime.UtcNow <= deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (Context != null)
                    Context.StopIfCycleStopRequested(Name + ".FlowStableAfterStop." + passName);

                if (ReadPickerFlowState(_pickerNo))
                {
                    if (!stableSinceUtc.HasValue)
                        stableSinceUtc = DateTime.UtcNow;

                    if ((DateTime.UtcNow - stableSinceUtc.Value).TotalMilliseconds >= stableMs)
                    {
                        WriteLog("PlaceZCalibration",
                            "PlaceZ Calibration Flow stable after stop. pass=" + passName +
                            ", side=" + Side +
                            ", outputSide=" + _targetOutputSide +
                            ", pickerNo=" + _pickerNo +
                            ", firstOnZ=" + firstFlowOnPosition.ToString("F6") +
                            ", stopZ=" + stopPosition.ToString("F6") +
                            ", stopOverrun=" + (stopPosition - firstFlowOnPosition).ToString("F6") +
                            ", velocity=" + velocity.ToString("F6") +
                            ", stableMs=" + stableMs +
                            ", pollMs=" + pollMs + " - Ok");
                        return true;
                    }
                }
                else
                {
                    stableSinceUtc = null;
                }

                await Task.Delay(pollMs, ct).ConfigureAwait(false);
            }

            WriteLog("PlaceZCalibration",
                "PlaceZ Calibration Flow stable check failed after stop. pass=" + passName +
                ", side=" + Side +
                ", outputSide=" + _targetOutputSide +
                ", pickerNo=" + _pickerNo +
                ", firstOnZ=" + firstFlowOnPosition.ToString("F6") +
                ", stopZ=" + stopPosition.ToString("F6") +
                ", velocity=" + velocity.ToString("F6") +
                ", stableMs=" + stableMs +
                ", timeoutMs=" + timeoutMs + " - Check");
            return false;
        }

        private async Task<int> RunBackOffBlowResetAsync(
            double detectedPosition,
            double backOffTarget,
            int repeatIndex,
            CancellationToken ct,
            string resetReason = "fine search")
        {
            int result = await MovePickerAxisAndVerifyAsync(
                _pickerZAxis,
                backOffTarget,
                "PlaceZ Calibration Z-only BackOff before " + resetReason + " #" + repeatIndex,
                ct,
                SearchTargetName,
                false,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            SetPickerVacuum(_pickerNo, false);
            WriteLog("PlaceZCalibration",
                "PlaceZ Calibration BackOff complete. side=" + Side +
                ", outputSide=" + _targetOutputSide +
                ", pickerNo=" + _pickerNo +
                ", repeat=" + repeatIndex +
                ", reason=" + resetReason +
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
                "PlaceZ Calibration Flow OFF after BackOff/Blow repeat #" + repeatIndex,
                ct).ConfigureAwait(false);
        }

        private async Task<int> PulsePickerBlowForCalibrationAsync(int repeatIndex, CancellationToken ct)
        {
            try
            {
                SetPickerBlow(_pickerNo, true);
                if (_settings.BlowPulseTimeMs > 0)
                    await Task.Delay(_settings.BlowPulseTimeMs, ct).ConfigureAwait(false);

                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration Blow pulse complete. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
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
                return Fail("PLACE-Z-CAL-BLOW-EX", Name,
                    "PlaceZ Calibration Blow pulse exception. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", repeat=" + repeatIndex +
                    ", error=" + ex.Message);
            }
            finally
            {
                try { SetPickerBlow(_pickerNo, false); }
                catch (Exception ex)
                {
                    WriteLog("PlaceZCalibration",
                        "PlaceZ Calibration Blow OFF cleanup failed. side=" + Side +
                        ", outputSide=" + _targetOutputSide +
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
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration Flow " + (expected ? "ON" : "OFF") +
                    " check bypassed in Simulation/DryRun. side=" + Side +
                    ", outputSide=" + _targetOutputSide +
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
            return Fail("PLACE-Z-CAL-FLOW-STATE", Name,
                "PlaceZ Calibration Flow state check failed. expected=" + (expected ? "ON" : "OFF") +
                ", actual=" + (actual ? "ON" : "OFF") +
                ", timeoutMs=" + _settings.FlowOffConfirmTimeoutMs +
                ", side=" + Side +
                ", outputSide=" + _targetOutputSide +
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

        private double CalculateSavedPlacePosition(double flowPosition)
        {
            return flowPosition + _settings.DieThicknessMm + _settings.FilmThicknessMm;
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

        // 레거시 Flow 검색(FlowStableMs 경과 후 정지 방식) 본문은 제거했다 — 신 경로
        // SearchFlowPositionWithResetAsync(즉시 정지 + BackOff/Blow 재시도)가 완전히 대체하며 호출부도 없었다.
        // 과거 가드가 반전되어 _settings==null일 때 본문이 _settings를 역참조하는 함정만 남아 있었다.
        private async Task<int> SearchFlowPositionAsync(CancellationToken ct)
        {
            if (_settings == null)
                return Fail("PLACE-Z-CAL-NO-SETTINGS", Name,
                    "PlaceZ Calibration 설정이 없어 Flow 검색을 실행할 수 없습니다.");

            return await SearchFlowPositionWithResetAsync(ct).ConfigureAwait(false);
        }

        private int SaveCalibrationResult()
        {
            try
            {
                if (Side == PickerSequenceSide.Front)
                    FrontPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PlacePosition", _savedPlacePosition);
                else
                    RearPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PlacePosition", _savedPlacePosition);

                double recipePlaceZ = GetPickerTeachingPosition(_pickerZAxis, "PlacePosition");
                if (Math.Abs(recipePlaceZ - _savedPlacePosition) > 0.000001)
                    return Fail("PLACE-Z-CAL-RECIPE-VERIFY", Name,
                        "PlaceZ Calibration Recipe PlacePosition read-back mismatch. " +
                        "axis=" + _pickerZAxis +
                        ", saved=" + _savedPlacePosition.ToString("F6") +
                        ", recipe=" + recipePlaceZ.ToString("F6"));

                CalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData;
                data.EnsureObjects();
                PlaceZCalibrationRecord record = data.PlaceZ.GetRecord(_calibrationSide, _pickerNo);
                record.OutputSide = _targetOutputSide;
                record.OldPlacePosition = _oldPlacePosition;
                record.DetectedFlowPosition = _detectedFlowPosition;
                record.SavedPlacePosition = _savedPlacePosition;
                record.ContactOffsetMm = _settings.DieThicknessMm;
                record.StartZMm = _scanStartPosition;
                record.FilmThicknessMm = _settings.FilmThicknessMm;
                record.DieThicknessMm = _settings.DieThicknessMm;
                record.PositionOffsetXmm = _settings.PositionOffsetXmm;
                record.PositionOffsetYmm = _settings.PositionOffsetYmm;
                record.DetectElapsedMs = _detectElapsedMs;
                record.Valid = true;
                record.UpdatedAt = DateTime.Now;
                record.UpdatedBy = ResolveUpdatedBy();
                record.Message = "OK";
                data.Touch("PlaceZCalibration");

                string saveReason;
                if (!CalibrationDataStore.Save(data, out saveReason))
                    return Fail("PLACE-Z-CAL-DATA-SAVE", Name,
                        "PlaceZ CalibrationData 저장 실패. reason=" + saveReason);

                EventLogger.Write(EventKind.Event, "CAL", "PLACE-Z-CAL-SAVE",
                    "PlaceZ Calibration 저장. side=" + _calibrationSide +
                    ", pickerNo=" + _pickerNo +
                    ", outputSide=" + _targetOutputSide +
                    ", axis=" + _pickerZAxis +
                    ", oldPlaceZ=" + _oldPlacePosition.ToString("F6") +
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPlaceZ=" + _savedPlacePosition.ToString("F6") +
                    ", recipePlaceZ=" + recipePlaceZ.ToString("F6") +
                    ", dieThickness=" + _settings.DieThicknessMm.ToString("F6") +
                    ", filmThickness=" + _settings.FilmThicknessMm.ToString("F6") +
                    ", positionOffsetX=" + _settings.PositionOffsetXmm.ToString("F6") +
                    ", positionOffsetY=" + _settings.PositionOffsetYmm.ToString("F6") +
                    ", formula=flow+die+film=" + _detectedFlowPosition.ToString("F6") +
                    "+" + _settings.DieThicknessMm.ToString("F6") +
                    "+" + _settings.FilmThicknessMm.ToString("F6"));
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PLACE-Z-CAL-SAVE-EX", Name,
                    "PlaceZ Calibration 결과 저장 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        // To do: [Z캘 명령 전용 전환] 명령 전용 탐색 이동의 정지/도달 후 축 정지 완료 대기.
        //        기존 WaitMoveTaskAfterStopAsync(블로킹 태스크 대기)를 대체한다 — 태스크가 없으므로 축 상태로 판정.
        //        재정지 1회 동작은 기존 헬퍼의 방어 동작을 그대로 유지한 것.
        private async Task WaitPickerZStoppedAsync(string reason)
        {
            try
            {
                BaseAxis axis = GetPickerAxis(_pickerZAxis);
                if (axis == null)
                    return;

                Stopwatch watch = Stopwatch.StartNew();
                bool restopIssued = false;
                while (watch.ElapsedMilliseconds < 2000)
                {
                    axis.UpdateStatus();
                    if (!axis.IsMoving)
                        return;

                    if (!restopIssued && watch.ElapsedMilliseconds > 1000)
                    {
                        restopIssued = true;
                        StopPickerZAxis(reason + " - 타임아웃 재정지");
                    }

                    await Task.Delay(10).ConfigureAwait(false);
                }

                axis.UpdateStatus();
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 정지 완료 대기 시간이 초과되었습니다. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", reason=" + reason +
                    ", " + BuildPickerAxisState(_pickerZAxis, axis.ActualPosition) + " - Check");
            }
            catch (Exception ex)
            {
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 정지 완료 대기 중 예외. " +
                    "outputSide=" + _targetOutputSide +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        // 기존 조건: 블로킹 이동 태스크 대기용 — 명령 전용 전환(2026-08-12)으로 미사용. 참고용으로 보존.
        private async Task<int> WaitMoveTaskAfterStopAsync(Task<int> moveTask, string reason)
        {
            if (moveTask == null)
                return 0;

            try
            {
                Task completed = await Task.WhenAny(moveTask, Task.Delay(2000)).ConfigureAwait(false);
                if (completed == moveTask)
                    return await moveTask.ConfigureAwait(false);

                BaseAxis axis = GetPickerAxis(_pickerZAxis);
                if (axis != null)
                    axis.UpdateStatus();

                // 타임아웃이면 축이 아직 이동 중일 수 있다 — 재정지 후 한 번 더 기다린다.
                if (axis != null && axis.IsMoving)
                {
                    StopPickerZAxis(reason + " - 타임아웃 재정지");
                    completed = await Task.WhenAny(moveTask, Task.Delay(2000)).ConfigureAwait(false);
                    if (completed == moveTask)
                        return await moveTask.ConfigureAwait(false);
                    axis.UpdateStatus();
                }

                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 정지 명령 후 이동 Task 완료 대기 시간이 초과되었습니다. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", reason=" + reason +
                    ", " + BuildPickerAxisState(_pickerZAxis, axis != null ? axis.ActualPosition : 0.0) +
                    " - Check");
                // 여기서 0을 반환해도 호출부가 축 IsMoving을 재확인해 이동 잔존 시 측정을 무효화한다.
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration 정지 후 이동 Task 대기 중 예외. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Check");
                return -1;
            }
        }

        private void StopPickerZAxis(string reason = null)
        {
            try
            {
                BaseAxis axis = GetPickerAxis(_pickerZAxis);
                if (axis == null)
                    return;

                axis.UpdateStatus();
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration PickerZ 정지 명령. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", axis=" + _pickerZAxis +
                    ", reason=" + (string.IsNullOrEmpty(reason) ? "-" : reason) +
                    ", actual=" + axis.ActualPosition.ToString("F6") +
                    ", command=" + axis.CommandPosition.ToString("F6") +
                    ", isMoving=" + axis.IsMoving + " - Stop");
                axis.StopJog();
                axis.Stop();
                axis.UpdateStatus();
            }
            catch (Exception ex)
            {
                WriteLog("PlaceZCalibration",
                    "PlaceZ Calibration PickerZ 정지 명령 예외. " +
                    "side=" + Side +
                    ", outputSide=" + _targetOutputSide +
                    ", pickerNo=" + _pickerNo +
                    ", axis=" + _pickerZAxis +
                    ", reason=" + (string.IsNullOrEmpty(reason) ? "-" : reason) +
                    ", error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        private void ReleaseArea()
        {
            try
            {
                if (_outputPlaceLease != null)
                {
                    _outputPlaceLease.Dispose();
                    _outputPlaceLease = null;
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

