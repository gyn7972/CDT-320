using System;
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

    internal sealed class PickerPickUpZCalibrationSequence : PickerSequenceBase<PickUpZCalibrationStep>
    {
        private const string SearchTargetName = "PickUpZCalibration;PickerZone=Input";

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly int _pickerNo;
        private readonly int _pickerIndex;
        private SequenceResourceLease _pickerLease;
        private SequenceResourceLease _inputStageLease;
        private PickUpZCalibrationSettings _settings;
        private PickerAxis _pickerZAxis;
        private double _oldPickPosition;
        private double _scanStartPosition;
        private double _searchLimitPosition;
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
                result = await SearchFlowPositionAsync(ct).ConfigureAwait(false);
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

                _scanStartPosition = _oldPickPosition - (downSign * Math.Abs(_settings.SearchStartOffsetMm));
                _searchLimitPosition = _oldPickPosition + (downSign * Math.Abs(_settings.SearchMaxDistanceMm));

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
            // PickUpZ Cal 시작 전 모든 Picker Z축을 Avoid로 올려 이후 Scan/Search 이동 인터락 조건을 만든다.
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " - PickerZ 전체 Avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PickUpZCalibration",
                Name + " 안전 시작 위치 확인 완료. side=" + Side +
                ", pickerNo=" + _pickerNo +
                ", targetPickerZ=" + _pickerZAxis + " - Ok");
            return 0;
        }

        private async Task<int> SearchFlowPositionAsync(CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
                return Fail("PICKUP-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);

            if (IsPickerSimulationOrDryRun())
            {
                _detectedFlowPosition = _oldPickPosition;
                _savedPickPosition = _detectedFlowPosition + _settings.ContactOffsetMm;
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
                _savedPickPosition = _detectedFlowPosition + _settings.ContactOffsetMm;
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

                CalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData;
                data.EnsureObjects();
                PickUpZCalibrationRecord record = data.PickUpZ.GetRecord(_calibrationSide, _pickerNo);
                record.OldPickPosition = _oldPickPosition;
                record.DetectedFlowPosition = _detectedFlowPosition;
                record.SavedPickPosition = _savedPickPosition;
                record.ContactOffsetMm = _settings.ContactOffsetMm;
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
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPickZ=" + _savedPickPosition.ToString("F6") +
                    ", contactOffset=" + _settings.ContactOffsetMm.ToString("F6"));
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
