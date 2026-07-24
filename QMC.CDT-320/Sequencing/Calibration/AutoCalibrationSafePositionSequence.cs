using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    internal enum AutoCalibrationSafePositionStep
    {
        None,
        MoveCurrentZsAvoid,
        MoveCurrentYAvoid,
        MoveCurrentTsAvoid,
        MoveOppositePickerAvoid,
        MoveInputCameraXAvoid,
        MoveOutputCameraXAvoid,
        MoveCurrentXAvoid,
        VerifyAllAvoid,
        Complete
    }

    internal sealed class AutoCalibrationSafePositionSequence
        : PickerSequenceBase<AutoCalibrationSafePositionStep>
    {
        private readonly CalibrationMotionSettings _motion;

        public AutoCalibrationSafePositionSequence(
            MachineSequenceContext context,
            VisionFocusPickerSide side)
            : base(
                context,
                side == VisionFocusPickerSide.Front ? PickerSequenceSide.Front : PickerSequenceSide.Rear,
                PickerSequenceKind.Inspect,
                side == VisionFocusPickerSide.Front
                    ? "FrontAutoCalibrationSafePositionSequence"
                    : "RearAutoCalibrationSafePositionSequence")
        {
            _motion = new CalibrationMotionSettings();
            _motion.EnsureDefaults();
            SetCalibrationMotion(_motion);
            CurrentStep = AutoCalibrationSafePositionStep.MoveCurrentZsAvoid;
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            try
            {
                // 안전 복귀 이동은 forceMove를 쓰지 않는다: 이미 Avoid(정지+무알람+톨러런스)면 확인만 하고 통과한다.
                // (자식 캘 시퀀스 시작 안전이동과의 이중 이동 제거 — 최종 VerifyAllUpperAxesAvoid 검증은 그대로 수행)
                CurrentStep = AutoCalibrationSafePositionStep.MoveCurrentZsAvoid;
                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Auto Calibration 안전 복귀 - 진행 Picker Z 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveCurrentYAvoid;
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    "Auto Calibration 안전 복귀 - 진행 Picker Y Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeY",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveCurrentTsAvoid;
                result = await MoveAllPickerTToAvoidAndVerifyAsync(
                    "Auto Calibration 안전 복귀 - 진행 Picker T 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveOppositePickerAvoid;
                result = await MoveOppositePickerToAvoidAndVerifyAsync(
                    "Auto Calibration 안전 복귀 - 상대 Picker 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveInputCameraXAvoid;
                result = await MoveInputCameraXToAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveOutputCameraXAvoid;
                result = await MoveOutputCameraXToAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.MoveCurrentXAvoid;
                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"),
                    "Auto Calibration 안전 복귀 - 진행 Picker X Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=SafeX",
                    false,
                    true).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.VerifyAllAvoid;
                result = VerifyAllUpperAxesAvoid();
                if (result != 0)
                    return result;

                CurrentStep = AutoCalibrationSafePositionStep.Complete;
                WriteLog("AutoCalibrationSafePosition",
                    Name + " 상부축 전체 Avoid 확인 완료. side=" + Side + " - Ok");
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
                return Fail("AUTO-CAL-SAFE-POS-EX", Name,
                    "Auto Calibration 상부축 전체 Avoid 이동 중 예외가 발생했습니다. side=" + Side +
                    ", step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                ReleasePickerWorkArea();
            }
        }

        private async Task<int> MoveInputCameraXToAvoidAsync(CancellationToken ct)
        {
            InputStageUnit stage = Context != null && Context.Machine != null
                ? Context.Machine.InputStageUnit
                : null;
            if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                return Fail("AUTO-CAL-SAFE-INPUT-CAMERA", "InputStageUnit",
                    "Input Camera X Avoid 이동에 필요한 축 또는 Recipe가 없습니다.");

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (stage.CameraX.IsAtTargetPosition(target, 0.0))
                return 0;

            int result = await stage.MoveInputStageAxisCommandWithMotion(
                WaferStageAxis.VisionX,
                target,
                _motion.MoveVelocity,
                _motion.MoveAcceleration,
                _motion.MoveDeceleration).ConfigureAwait(false);
            if (result != 0)
                return Fail("AUTO-CAL-SAFE-INPUT-CAMERA-MOVE", stage.Name,
                    "Input Camera X Avoid 이동 명령에 실패했습니다. result=" + result +
                    ", target=" + target.ToString("F6"));

            // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 재대기는 제거하고
            // Avoid 도착 안전 게이트만 유지(R4).
            if (!stage.IsVisionXInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-INPUT-CAMERA-WAIT", stage.Name,
                    "Input Camera X Avoid 도착 확인에 실패했습니다. target=" + target.ToString("F6") +
                    ", actual=" + stage.CameraX.ActualPosition.ToString("F6"));

            return 0;
        }

        private async Task<int> MoveOutputCameraXToAvoidAsync(CancellationToken ct)
        {
            OutputStageUnit stage = Context != null && Context.Machine != null
                ? Context.Machine.OutputStageUnit
                : null;
            if (stage == null ||
                stage.OutputCameraX == null ||
                stage.Recipe == null ||
                stage.Recipe.VisionX == null)
                return Fail("AUTO-CAL-SAFE-OUTPUT-CAMERA", "OutputStageUnit",
                    "Output Camera X Avoid 이동에 필요한 축이 없습니다.");

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (stage.OutputCameraX.IsAtTargetPosition(target, 0.0))
                return 0;

            int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveMoveTimeout(),
                _motion.MoveVelocity,
                _motion.MoveAcceleration,
                _motion.MoveDeceleration,
                ct).ConfigureAwait(false);
            if (result != 0 || !stage.IsVisionXInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-OUTPUT-CAMERA-MOVE", stage.Name,
                    "Output Camera X Avoid 이동 또는 도착 확인에 실패했습니다. result=" + result);

            return 0;
        }

        private int VerifyAllUpperAxesAvoid()
        {
            if (Context == null || Context.Machine == null)
                return Fail("AUTO-CAL-SAFE-NO-MACHINE", Name, "장비 객체가 없습니다.");

            if (Context.Machine.PickerFrontUnit != null &&
                !Context.Machine.PickerFrontUnit.IsFrontPickerInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-FRONT", "PickerFrontUnit",
                    "Front Picker X/Y/Z/T 전체 Avoid 최종 확인에 실패했습니다.");

            if (Context.Machine.PickerRearUnit != null &&
                !Context.Machine.PickerRearUnit.IsRearPickerInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-REAR", "PickerRearUnit",
                    "Rear Picker X/Y/Z/T 전체 Avoid 최종 확인에 실패했습니다.");

            if (Context.Machine.InputStageUnit != null &&
                !Context.Machine.InputStageUnit.IsVisionXInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-INPUT-CAMERA-CHECK", "InputStageUnit",
                    "Input Camera X가 Avoid 위치가 아닙니다.");

            if (Context.Machine.OutputStageUnit != null &&
                !Context.Machine.OutputStageUnit.IsVisionXInAvoidPosition())
                return Fail("AUTO-CAL-SAFE-OUTPUT-CAMERA-CHECK", "OutputStageUnit",
                    "Output Camera X가 Avoid 위치가 아닙니다.");

            return 0;
        }
    }
}
