using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Security;
using QMC.Common.Alarms;
using QMC.Common.Data.Store;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraCalibrationDialog
    {
        private VisionCameraCalibrationData _manualDraft;
        private readonly HashSet<string> _manualEditedItems = new HashSet<string>();
        private CDT320_Machine _manualMachine;
        private string _manualRecipeName;
        private string _manualCameraBaseline;
        private string _manualInputRecipeBaseline;
        private string _manualOutputRecipeBaseline;
        private VisionCameraCalibrationData _measuredCameraSource;
        private string _measuredRecipeName;

        private static string Snapshot<T>(T data)
        {
            if (data == null) return string.Empty;
            using (var stream = new MemoryStream())
            {
                JsonPrettySerializer.WriteObject(stream, typeof(T), data, JsonPrettySerializer.CreateSettings(true));
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private VisionCameraCalibrationData CreateManualEditCandidate()
        {
            Form1 host = FindHostForm();
            if (host?.Machine == null) throw new InvalidOperationException("장비 객체가 없습니다.");
            VisionCameraCalibrationData live = Sequence.CalibrationData;
            if (_manualDraft == null)
            {
                live.EnsureObjects();
                _manualMachine = host.Machine;
                _manualRecipeName = host.ActiveRecipeName;
                _manualCameraBaseline = Snapshot(live);
                _manualInputRecipeBaseline = Snapshot(host.Machine.InputStageUnit?.Recipe);
                _manualOutputRecipeBaseline = Snapshot(host.Machine.OutputStageUnit?.Recipe);
            }
            else
            {
                ValidateManualEditContext(host);
            }
            // Invalid input must not leave a half-edited draft, either.
            return VisionCameraManualSaveService.CloneForEdit(_manualDraft ?? live);
        }

        private void ValidateManualEditContext(Form1 host)
        {
            if (_manualDraft == null) return;
            if (!ReferenceEquals(_manualMachine, host.Machine) ||
                !string.Equals(_manualRecipeName, host.ActiveRecipeName, StringComparison.Ordinal) ||
                _manualCameraBaseline != Snapshot(Sequence.CalibrationData))
                throw new InvalidOperationException("편집 이후 장비/Recipe/카메라 데이터가 변경되었습니다. LOAD 후 다시 수정하세요.");
            if ((_manualEditedItems.Contains(InputVisionXEncoderRow) &&
                 _manualInputRecipeBaseline != Snapshot(host.Machine.InputStageUnit?.Recipe)) ||
                (_manualEditedItems.Contains(OutputVisionXEncoderRow) &&
                 _manualOutputRecipeBaseline != Snapshot(host.Machine.OutputStageUnit?.Recipe)))
                throw new InvalidOperationException("편집 이후 Reticle Recipe가 변경되었습니다. LOAD 후 다시 수정하세요.");
        }

        private void DiscardManualDraft()
        {
            _manualDraft = null;
            _manualEditedItems.Clear();
            _manualMachine = null;
            _manualRecipeName = _manualCameraBaseline = null;
            _manualInputRecipeBaseline = _manualOutputRecipeBaseline = null;
            UpdateSaveReticleButtonEnabled();
        }

        private static void ValidateManualOffsetBasis(VisionCameraCalibrationData camera)
        {
            if (camera.BottomReticle == null || !camera.BottomReticle.Valid ||
                camera.InputReticle == null || !camera.InputReticle.Valid ||
                camera.OutputReticle == null || !camera.OutputReticle.Valid)
                throw new InvalidOperationException("Offset 수정에는 Bottom/Input/Output Reticle 기준값이 필요합니다.");
        }

        private void SaveAndApplyCameraValues(bool calculate)
        {
            if (_busy) return;
            try
            {
                string reason;
                if (!CanRunManualCalibration(out reason)) throw new InvalidOperationException(reason);
                if (_manualDraft != null && !CanEditAppliedValues())
                    throw new InvalidOperationException("수동 수정값 저장에는 Admin 권한이 필요합니다.");
                if (!calculate && !HasCameraValuesToSave())
                    throw new InvalidOperationException("저장할 수정값이나 이번에 측정한 Reticle 위치가 없습니다.");
                Form1 host = FindHostForm();
                if (host?.Machine == null || host.Controller == null)
                    throw new InvalidOperationException("장비 제어 객체가 없습니다.");
                IDisposable lease;
                if (!host.Controller.TryBeginCameraCalibrationSaveOperation(out lease, out reason))
                    throw new InvalidOperationException(reason);

                _busy = true;
                SetButtonsEnabled(false);
                string summary;
                using (lease)
                {
                    ValidateManualEditContext(host);
                    summary = CommitCameraValues(host, calculate);
                }
                DiscardManualDraft();
                ClearReticleMeasuredInSession();
                RefreshData();
                lblStatus.Text = summary;
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SAVE-APPLY", summary);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "저장·적용 실패: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-CAMERA-CAL-SAVE-APPLY-FAIL", lblStatus.Text);
                QMC.Common.MessageDialog.Show(this, lblStatus.Text, "VISION CAMERA CAL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busy = false;
                SetButtonsEnabled(true);
            }
        }

        private string CommitCameraValues(Form1 host, bool calculate)
        {
            CDT320_Machine machine = host.Machine;
            CalibrationData live = machine.VisionUnit?.Config?.CalibrationData;
            if (live?.Camera == null) throw new InvalidOperationException("카메라 캘리브레이션 데이터가 없습니다.");
            bool editInput = _manualEditedItems.Contains(InputVisionXEncoderRow);
            bool editOutput = _manualEditedItems.Contains(OutputVisionXEncoderRow);
            bool saveInput = editInput || _inputReticleMeasuredInSession;
            bool saveOutput = editOutput || _outputReticleMeasuredInSession;
            if (_inputReticleMeasuredInSession || _outputReticleMeasuredInSession)
            {
                if (!ReferenceEquals(_measuredCameraSource, live.Camera) ||
                    !string.Equals(_measuredRecipeName, host.ActiveRecipeName, StringComparison.Ordinal))
                    throw new InvalidOperationException("측정 이후 Recipe/카메라가 변경되었습니다. LOAD 후 다시 측정하거나 입력하세요.");
                if ((!editInput && _inputReticleMeasuredInSession &&
                     !SameMeasuredPosition(live.Camera.InputReticle, _measuredInputVisionX)) ||
                    (!editOutput && _outputReticleMeasuredInSession &&
                     !SameMeasuredPosition(live.Camera.OutputReticle, _measuredOutputVisionX)))
                    throw new InvalidOperationException("마지막 FIND 이후 Encoder 값이 변경되었습니다. 다시 측정하거나 직접 입력하세요.");
            }

            var request = new VisionCameraManualSaveRequest
            {
                CalibrationData = VisionCameraManualSaveService.CloneForEdit(live),
                RecipeName = host.ActiveRecipeName
            };
            VisionCameraCalibrationData camera = VisionCameraManualSaveService.CloneForEdit(_manualDraft ?? live.Camera);
            request.CalibrationData.Camera = camera;
            bool geometry = calculate || saveInput || saveOutput || _manualEditedItems.Any(IsCameraCalibrationGeometryItem);
            int pickerCount = 0;
            string simulated;
            if (Sequence.HasSimulatedMeasurementInSession(out simulated))
                throw new InvalidOperationException("시뮬레이션/bypass 측정(" + simulated + ")은 실 캘리브레이션으로 저장할 수 없습니다.");
            if (geometry)
            {
                bool hasBasis = camera.BottomReticle.Valid && camera.InputReticle.Valid && camera.OutputReticle.Valid;
                bool encoderOnly = !calculate && !_manualEditedItems.Any(item =>
                    IsCameraCalibrationGeometryItem(item) && item != InputVisionXEncoderRow && item != OutputVisionXEncoderRow);
                if (!hasBasis && !encoderOnly)
                    throw new InvalidOperationException("Bottom/Input/Output Reticle 기준값을 모두 준비한 뒤 저장하세요.");
                if (hasBasis && !camera.Calculate(UserSession.Name))
                    throw new InvalidOperationException("카메라 Offset 계산값이 유효하지 않습니다.");
                if (!hasBasis) camera.Valid = false;

                bool hasCollets = HasValidCollets(live.Collet?.FrontCollets) || HasValidCollets(live.Collet?.RearCollets);
                if (hasCollets)
                {
                    if (!camera.Valid)
                        throw new InvalidOperationException("유효한 콜렛이 있어 카메라 기준값과 픽커 보정을 함께 계산해야 합니다. Reticle 기준값을 확인하세요.");
                    if (HasValidCollets(live.Collet.FrontCollets))
                    {
                        if (machine.PickerFrontUnit?.Setup == null) throw new InvalidOperationException("Front Picker Setup이 없습니다.");
                        request.PickerFrontSetup = VisionCameraManualSaveService.CloneForEdit(machine.PickerFrontUnit.Setup);
                        pickerCount += FillCandidatePickerOffsets(camera, live.Collet.FrontCollets,
                            request.PickerFrontSetup.InputVisionToPicker, request.PickerFrontSetup.OutputVisionToPicker);
                    }
                    if (HasValidCollets(live.Collet.RearCollets))
                    {
                        if (machine.PickerRearUnit?.Setup == null) throw new InvalidOperationException("Rear Picker Setup이 없습니다.");
                        request.PickerRearSetup = VisionCameraManualSaveService.CloneForEdit(machine.PickerRearUnit.Setup);
                        pickerCount += FillCandidatePickerOffsets(camera, live.Collet.RearCollets,
                            request.PickerRearSetup.InputVisionToPicker, request.PickerRearSetup.OutputVisionToPicker);
                    }
                }
            }
            if (saveInput)
            {
                ValidateReticlePosition(camera.InputReticle, machine.InputStageUnit?.CameraX, "Input");
                if (machine.InputStageUnit?.Recipe?.VisionX == null) throw new InvalidOperationException("Input Reticle Recipe가 없습니다.");
                request.InputStageRecipe = VisionCameraManualSaveService.CloneForEdit(machine.InputStageUnit.Recipe);
                request.InputStageRecipe.VisionX.ReticlePosition = camera.InputReticle.VisionXPosition;
            }
            if (saveOutput)
            {
                ValidateReticlePosition(camera.OutputReticle, machine.OutputStageUnit?.OutputCameraX, "Output");
                if (machine.OutputStageUnit?.Recipe?.VisionX == null) throw new InvalidOperationException("Output Reticle Recipe가 없습니다.");
                request.OutputStageRecipe = VisionCameraManualSaveService.CloneForEdit(machine.OutputStageUnit.Recipe);
                request.OutputStageRecipe.VisionX.ReticlePosition = camera.OutputReticle.VisionXPosition;
            }
            request.CalibrationData.Touch(UserSession.Name);
            string reviewReason;
            if (!MaterialStateService.TryPrepareInputStageReviewForCameraSave(out reviewReason))
                throw new InvalidOperationException(reviewReason);
            var result = VisionCameraManualSaveService.Save(request);
            if (!result.Success)
            {
                if (result.RecoveryRequired) BlockInconsistentCalibration(host, result.Message);
                throw new InvalidOperationException(result.Message);
            }

            // Files are complete before any live value changes. Existing offset objects stay valid for their consumers.
            try
            {
                if (request.PickerFrontSetup != null)
                {
                    CopyPickerOffsets(request.PickerFrontSetup.InputVisionToPicker, machine.PickerFrontUnit.Setup.InputVisionToPicker);
                    CopyPickerOffsets(request.PickerFrontSetup.OutputVisionToPicker, machine.PickerFrontUnit.Setup.OutputVisionToPicker);
                }
                if (request.PickerRearSetup != null)
                {
                    CopyPickerOffsets(request.PickerRearSetup.InputVisionToPicker, machine.PickerRearUnit.Setup.InputVisionToPicker);
                    CopyPickerOffsets(request.PickerRearSetup.OutputVisionToPicker, machine.PickerRearUnit.Setup.OutputVisionToPicker);
                }
                if (request.InputStageRecipe != null)
                    machine.InputStageUnit.Recipe.VisionX.ReticlePosition = request.InputStageRecipe.VisionX.ReticlePosition;
                if (request.OutputStageRecipe != null)
                    machine.OutputStageUnit.Recipe.VisionX.ReticlePosition = request.OutputStageRecipe.VisionX.ReticlePosition;
                live.Camera = camera;
                live.UpdatedAt = request.CalibrationData.UpdatedAt;
                live.UpdatedBy = request.CalibrationData.UpdatedBy;
            }
            catch (Exception ex)
            {
                BlockInconsistentCalibration(host, "파일 저장 후 운전값 반영 실패: " + ex.Message);
                throw;
            }
            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-VALUES",
                "user=" + UserSession.Name + ", recipe=" + request.RecipeName +
                ", edited=" + string.Join(",", _manualEditedItems) +
                ", InputOffset=(" + FormatManualNumber(camera.InputToBottomOffsetX) + "," + FormatManualNumber(camera.InputToBottomOffsetY) + ")" +
                ", OutputOffset=(" + FormatManualNumber(camera.OutputToBottomOffsetX) + "," + FormatManualNumber(camera.OutputToBottomOffsetY) + ")" +
                ", InputManualCorrection=(" + FormatManualNumber(camera.InputToBottomManualCorrectionX) + "," + FormatManualNumber(camera.InputToBottomManualCorrectionY) + ")" +
                ", OutputManualCorrection=(" + FormatManualNumber(camera.OutputToBottomManualCorrectionX) + "," + FormatManualNumber(camera.OutputToBottomManualCorrectionY) + ")" +
                ", InputEncoder=" + FormatManualNumber(camera.InputReticle.VisionXPosition) +
                ", OutputEncoder=" + FormatManualNumber(camera.OutputReticle.VisionXPosition) + ", appliedCollets=" + pickerCount);
            string saved = "저장 완료. " + (geometry ?
                (pickerCount > 0 ? "픽커 " + pickerCount + "개 보정 적용 완료. 다음 START부터 사용합니다." :
                 "유효한 콜렛이 없어 픽커 보정은 미적용입니다. 콜렛 캘리브레이션이 필요합니다.") : "이동 파라미터를 적용했습니다.");
            if (!camera.Valid && geometry) saved += " 카메라 Offset 계산은 아직 완료되지 않았습니다.";
            if (saveInput || saveOutput) saved += " Recipe 위치 저장: " + (saveInput ? "Input " : "") + (saveOutput ? "Output" : "");
            return saved;
        }

        private static bool SameMeasuredPosition(VisionReticleMeasurement value, double measured)
        {
            return value != null && value.HasVisionXPosition && IsFinite(value.VisionXPosition) &&
                IsFinite(measured) && Math.Abs(value.VisionXPosition - measured) <= 1e-6;
        }

        private static bool HasValidCollets(ColletCalibrationRecord[] records)
        {
            return records != null && records.Any(record => record != null && record.Valid);
        }

        private static int FillCandidatePickerOffsets(VisionCameraCalibrationData camera, ColletCalibrationRecord[] records,
            PickerVisionCoordinateOffsets input, PickerVisionCoordinateOffsets output)
        {
            ValidateOffsetArrays(input);
            ValidateOffsetArrays(output);
            int count = 0;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i] == null || !records[i].Valid) continue;
                if (i >= 4) throw new InvalidOperationException("콜렛 번호가 허용 범위를 벗어났습니다.");
                double ix, iy, ox, oy;
                string reason;
                if (!PickerVisionOffsetCalibrationService.TryCalculatePickerOffsets(camera, records[i], out ix, out iy, out ox, out oy, out reason))
                    throw new InvalidOperationException("Collet " + (i + 1) + ": " + reason);
                input.OffsetX[i] = ix;
                input.OffsetY[i] = iy;
                output.OffsetX[i] = ox;
                output.OffsetY[i] = oy;
                count++;
            }
            return count;
        }

        private static void ValidateOffsetArrays(PickerVisionCoordinateOffsets offsets)
        {
            if (offsets?.OffsetX == null || offsets.OffsetY == null || offsets.OffsetX.Length != 4 || offsets.OffsetY.Length != 4)
                throw new InvalidOperationException("픽커 보정 배열이 올바르지 않습니다.");
        }

        private static void CopyPickerOffsets(PickerVisionCoordinateOffsets from, PickerVisionCoordinateOffsets to)
        {
            Array.Copy(from.OffsetX, to.OffsetX, 4);
            Array.Copy(from.OffsetY, to.OffsetY, 4);
        }

        private static void ValidateReticlePosition(VisionReticleMeasurement measurement, QMC.Common.Motion.BaseAxis axis, string name)
        {
            if (measurement == null || !measurement.HasVisionXPosition || !IsFinite(measurement.VisionXPosition))
                throw new InvalidOperationException(name + " Encoder 위치가 유효하지 않습니다.");
            if (axis?.Setup == null) throw new InvalidOperationException(name + " VisionX 축 설정이 없습니다.");
            if (axis.Setup.SoftLimitEnabled && (measurement.VisionXPosition < axis.Setup.SoftLimitMinus ||
                measurement.VisionXPosition > axis.Setup.SoftLimitPlus))
                throw new InvalidOperationException(name + " Encoder 위치가 축 소프트 리미트를 벗어났습니다.");
        }

        private static void BlockInconsistentCalibration(Form1 host, string reason)
        {
            string message = "카메라 저장값 복구/확인이 필요합니다. " + reason;
            host.Controller.BlockRecipeStartAfterFailedApply(message);
            AlarmManager.Raise(AlarmSeverity.Error, "VISION-CAMERA-CAL-RECOVERY", "VisionUnit", message);
        }
    }
}
