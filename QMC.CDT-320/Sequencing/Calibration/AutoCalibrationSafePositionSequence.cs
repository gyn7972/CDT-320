using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    // 캘리브레이션 안전위치(Avoid) 이동용 SafeMovePercent 공용 헬퍼.
    // 안전이동 모션 = 각 축 Config.Default(속도/가속/감속) × (SafeMovePercent/100).
    // 값이 없거나(1 미만/NaN) 읽기 실패면 0을 돌려 기존 동작으로 폴백하고, 100 초과는 100으로 클램프한다.
    public static class CalibrationSafeMoveMotion
    {
        public static double ResolvePercent(CDT320_Machine machine)
        {
            try
            {
                if (machine == null || machine.VisionUnit == null ||
                    machine.VisionUnit.Config == null || machine.VisionUnit.Config.CalibrationData == null)
                    return 0.0;

                double percent = machine.VisionUnit.Config.CalibrationData.SafeMovePercent;
                if (double.IsNaN(percent) || percent < CalibrationData.MinSafeMovePercent)
                    return 0.0;
                if (percent > CalibrationData.MaxSafeMovePercent)
                    percent = CalibrationData.MaxSafeMovePercent;
                return percent;
            }
            catch
            {
                return 0.0;
            }
        }

        // percent가 유효하고 축 Config가 있으면 vel/acc/dec를 Default × %로 덮어쓰고 true를 돌려준다.
        // false면 인자로 들어온 기존 값이 그대로 유지된다(폴백).
        public static bool TryResolveAxisMotion(
            BaseAxis axis,
            double safeMovePercent,
            ref double velocity,
            ref double acceleration,
            ref double deceleration)
        {
            if (safeMovePercent <= 0.0 || axis == null || axis.Config == null || axis.Config.GetRawDefaultVelocity() <= 0.0)
                return false;

            double factor = Math.Min(safeMovePercent, CalibrationData.MaxSafeMovePercent) / 100.0;
            // [정정 2026-07-26] 스케일 적용값 × 퍼센트 — 원본 유출 차단.
            velocity = axis.Config.GetDefaultVel() * factor;
            acceleration = axis.Config.GetDefaultAcc() * factor;
            deceleration = axis.Config.GetDefaultDec() * factor;
            return true;
        }

        public static void LogAxisSafeMove(
            string owner,
            string description,
            double safeMovePercent,
            bool safeMoveApplied,
            double velocity,
            double acceleration,
            double deceleration)
        {
            EventLogger.Write(EventKind.Event, "QMC", "CAL-SAFEMOVE-MOTION",
                (owner ?? "-") + " calibration safe-move motion. target=" + (description ?? "-") +
                ", velocity=" + velocity.ToString("F6") +
                ", acceleration=" + acceleration.ToString("F6") +
                ", deceleration=" + deceleration.ToString("F6") +
                ", safeMovePercent=" + safeMovePercent.ToString("F3") +
                ", safeMoveApplied=" + safeMoveApplied +
                ", explicitVelocityNotDefaultScaled=True");
        }
    }

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

            // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 측정 모션으로 폴백.
            double safePercent = CalibrationSafeMoveMotion.ResolvePercent(Context != null ? Context.Machine : null);
            double velocity = _motion.MoveVelocity;
            double acceleration = _motion.MoveAcceleration;
            double deceleration = _motion.MoveDeceleration;
            bool safeMoveApplied = CalibrationSafeMoveMotion.TryResolveAxisMotion(
                stage.CameraX, safePercent, ref velocity, ref acceleration, ref deceleration);
            CalibrationSafeMoveMotion.LogAxisSafeMove(
                Name, "InputCameraX;AvoidPosition", safePercent, safeMoveApplied, velocity, acceleration, deceleration);

            int result = await stage.MoveInputStageAxisCommandWithMotion(
                WaferStageAxis.VisionX,
                target,
                velocity,
                acceleration,
                deceleration).ConfigureAwait(false);
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

            // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 측정 모션으로 폴백.
            double safePercent = CalibrationSafeMoveMotion.ResolvePercent(Context != null ? Context.Machine : null);
            double velocity = _motion.MoveVelocity;
            double acceleration = _motion.MoveAcceleration;
            double deceleration = _motion.MoveDeceleration;
            bool safeMoveApplied = CalibrationSafeMoveMotion.TryResolveAxisMotion(
                stage.OutputCameraX, safePercent, ref velocity, ref acceleration, ref deceleration);
            CalibrationSafeMoveMotion.LogAxisSafeMove(
                Name, "OutputCameraX;AvoidPosition", safePercent, safeMoveApplied, velocity, acceleration, deceleration);

            int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveMoveTimeout(),
                velocity,
                acceleration,
                deceleration,
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
