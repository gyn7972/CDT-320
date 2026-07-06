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
        public int PickerNo { get; set; }
        public double OldPlacePosition { get; set; }
        public double ScanStartPosition { get; set; }
        public double SearchLimitPosition { get; set; }
        public double DetectedFlowPosition { get; set; }
        public double SavedPlacePosition { get; set; }
        public int DetectElapsedMs { get; set; }
        public string Message { get; set; }
    }

    internal sealed class PickerPlaceZCalibrationSequence : PickerSequenceBase<PlaceZCalibrationStep>
    {
        private const string SearchTargetName = "PlaceZCalibration;PickerZone=Output";

        private readonly VisionFocusPickerSide _calibrationSide;
        private readonly int _pickerNo;
        private readonly int _pickerIndex;
        private SequenceResourceLease _pickerLease;
        private SequenceResourceLease _outputPlaceLease;
        private PlaceZCalibrationSettings _settings;
        private PickerAxis _pickerZAxis;
        private double _oldPlacePosition;
        private double _scanStartPosition;
        private double _searchLimitPosition;
        private double _detectedFlowPosition;
        private double _savedPlacePosition;
        private int _detectElapsedMs;

        public PickerPlaceZCalibrationSequence(MachineSequenceContext context, VisionFocusPickerSide side, int pickerNo)
            : base(
                  context,
                  side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                  PickerSequenceKind.UnloadToOutput,
                  "PlaceZCalibration")
        {
            _calibrationSide = side;
            _pickerNo = NormalizePickerNo(pickerNo);
            _pickerIndex = _pickerNo - 1;
            Result = new PlaceZCalibrationResult
            {
                Side = side,
                PickerNo = _pickerNo,
                Message = string.Empty
            };
        }

        public PlaceZCalibrationResult Result { get; private set; }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool vacuumOn = false;
            try
            {
                CurrentStep = PlaceZCalibrationStep.CheckReady;
                int result = CheckReady();
                if (result != 0) return result;

                CurrentStep = PlaceZCalibrationStep.ReserveArea;
                result = await ReserveAreaAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "PlaceZCalibration");

                CurrentStep = PlaceZCalibrationStep.MoveZSafe;
                result = await PrepareSafeStartPositionAsync("PlaceZ Calibration 시작 전 안전 위치 이동", ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = PlaceZCalibrationStep.MoveScanStart;
                result = await MovePickerAxisAndVerifyAsync(
                    _pickerZAxis,
                    _scanStartPosition,
                    "PlaceZ Calibration Scan Start",
                    ct,
                    SearchTargetName).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = PlaceZCalibrationStep.VacuumOn;
                SetPickerVacuum(_pickerNo, true);
                vacuumOn = true;
                if (_settings.VacuumOnDelayMs > 0)
                    await Task.Delay(_settings.VacuumOnDelayMs, ct).ConfigureAwait(false);

                CurrentStep = PlaceZCalibrationStep.SearchFlow;
                result = await SearchFlowPositionAsync(ct).ConfigureAwait(false);
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
                        "AvoidPosition").ConfigureAwait(false);
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
                    ", elapsedMs=" + _detectElapsedMs + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                StopPickerZAxis();
                Result.Message = "PlaceZ Calibration canceled.";
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
                return Fail("PLACE-Z-CAL-EX", Name, "PlaceZ Calibration 예외 발생. error=" + ex.Message);
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
                        SearchTargetName).ConfigureAwait(false);
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
                        "AvoidPosition").ConfigureAwait(false);
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

                _scanStartPosition = _oldPlacePosition - (downSign * Math.Abs(_settings.SearchStartOffsetMm));
                _searchLimitPosition = _oldPlacePosition + (downSign * Math.Abs(_settings.SearchMaxDistanceMm));

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
            // PlaceZ Cal 시작 전 모든 Picker Z축을 Avoid로 올려 이후 Scan/Search 이동 인터락 조건을 만든다.
            int result = await MoveAllPickerZToAvoidAndVerifyAsync(description + " - PickerZ 전체 Avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            WriteLog("PlaceZCalibration",
                Name + " 안전 시작 위치 확인 완료. side=" + Side +
                ", pickerNo=" + _pickerNo +
                ", targetPickerZ=" + _pickerZAxis + " - Ok");
            return 0;
        }

        private async Task<int> SearchFlowPositionAsync(CancellationToken ct)
        {
            BaseAxis axis = GetPickerAxis(_pickerZAxis);
            if (axis == null)
                return Fail("PLACE-Z-CAL-Z-AXIS", Name, "PickerZ axis is null. axis=" + _pickerZAxis);

            if (IsPickerSimulationOrDryRun())
            {
                _detectedFlowPosition = _oldPlacePosition;
                _savedPlacePosition = _detectedFlowPosition + _settings.ContactOffsetMm;
                _detectElapsedMs = 0;
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPlacePosition = _savedPlacePosition;
                Result.DetectElapsedMs = _detectElapsedMs;
                return 0;
            }

            if (_settings.FailIfFlowAlreadyOn && ReadPickerFlowState(_pickerNo))
                return Fail("PLACE-Z-CAL-FLOW-ALREADY-ON", Name,
                    "PlaceZ Calibration 시작 전 Flow가 이미 ON입니다. Vacuum sensor 상태 또는 Picker 위치를 확인하세요. " +
                    "side=" + Side + ", pickerNo=" + _pickerNo);

            Stopwatch watch = Stopwatch.StartNew();
            DateTime? stableSinceUtc = null;
            bool detected = false;

            string interlockReason;
            if (!MotionGuardRuntime.VerifyAxisTeachingMove(axis, _searchLimitPosition, SearchTargetName, out interlockReason))
            {
                return Fail("PLACE-Z-CAL-SEARCH-INTERLOCK", Name,
                    "PlaceZ Calibration 검색 이동 인터락 차단. " +
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
                        return Fail("PLACE-Z-CAL-Z-MOVE", Name,
                            "PlaceZ Calibration 검색 이동 실패. result=" + moveResult +
                            ", " + BuildPickerAxisState(_pickerZAxis, _searchLimitPosition));
                    }
                }

                if (!detected)
                {
                    return Fail("PLACE-Z-CAL-FLOW-NOT-DETECTED", Name,
                        "PlaceZ Calibration Flow 감지 실패. 최대 하강 위치까지 Flow가 ON되지 않았습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _pickerNo +
                        ", start=" + _scanStartPosition.ToString("F6") +
                        ", limit=" + _searchLimitPosition.ToString("F6") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", timeoutMs=" + _settings.Motion.MoveTimeoutMs);
                }

                _detectedFlowPosition = axis.ActualPosition;
                _savedPlacePosition = _detectedFlowPosition + _settings.ContactOffsetMm;
                _detectElapsedMs = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                Result.DetectedFlowPosition = _detectedFlowPosition;
                Result.SavedPlacePosition = _savedPlacePosition;
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
                return Fail("PLACE-Z-CAL-SEARCH-EX", Name,
                    "PlaceZ Calibration Flow 검색 예외 발생. error=" + ex.Message);
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
                    FrontPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PlacePosition", _savedPlacePosition);
                else
                    RearPicker.SetPickerAxisTeachingPosition(_pickerZAxis, "PlacePosition", _savedPlacePosition);

                CalibrationData data = Context.Machine.VisionUnit.Config.CalibrationData;
                data.EnsureObjects();
                PlaceZCalibrationRecord record = data.PlaceZ.GetRecord(_calibrationSide, _pickerNo);
                record.OldPlacePosition = _oldPlacePosition;
                record.DetectedFlowPosition = _detectedFlowPosition;
                record.SavedPlacePosition = _savedPlacePosition;
                record.ContactOffsetMm = _settings.ContactOffsetMm;
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
                    ", axis=" + _pickerZAxis +
                    ", oldPlaceZ=" + _oldPlacePosition.ToString("F6") +
                    ", detectedZ=" + _detectedFlowPosition.ToString("F6") +
                    ", savedPlaceZ=" + _savedPlacePosition.ToString("F6") +
                    ", contactOffset=" + _settings.ContactOffsetMm.ToString("F6"));
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
                if (_outputPlaceLease != null)
                {
                    _outputPlaceLease.Dispose();
                    _outputPlaceLease = null;
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

