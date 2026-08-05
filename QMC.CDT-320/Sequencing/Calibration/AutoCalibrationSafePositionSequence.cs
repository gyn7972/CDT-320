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

            // ================================================================
            // [Manual 스코프 분리 2026-08-06] 사용자 확정: 캘 안전이동은 SafeMovePercent 하나로만 정한다.
            //
            // 기존 조건: GetDefaultVel/Acc/Dec (= MotionSpeedScale 적용값) x 퍼센트.
            //   주석 "[정정 2026-07-26] 스케일 적용값 x 퍼센트 - 원본 유출 차단" 에 따른 것이었다.
            //   그런데 캘은 Manual Sequence 스코프 안에서 돌아
            //   EffectiveScaleFactor 가 전역 ScalePercent 가 아니라 ManualSequencePercent 를 쓴다
            //   (MotionSpeedScale:130 - Ready > Manual > 전역 순).
            //   현장 settings.json 의 ManualSequenceScalePercent = 50 이므로 실제로는
            //     2000(FrontPickerX DEFAULT VEL) x 0.50 x 0.10 = 100 mm/s
            //   가 되어, 화면에 10% 를 넣어도 실질 5% 로 동작했다.
            //   화면(DEFAULT SPEED SCALE % = 100)만 보면 200 을 기대하게 되어 값이 어긋났다.
            //
            // 현재 기준: 원본 DefaultVelocity x SafeMovePercent 만 적용한다.
            //   → 2000 x 0.10 = 200 mm/s. 화면 값과 실제가 1:1로 맞는다.
            //   숨은 배율이 사라져 % 하나로 캘 속도를 예측할 수 있다.
            //
            // 안전성: 명시 속도는 하위에서 재스케일되지 않고 그대로 보드에 전달된다
            //   (AjinAxis:1803 "스케일 완료된 최종값 그대로 보드에 전달").
            //   따라서 여기 값이 곧 실제 축 속도다.
            //
            // ★주의★ 이 변경으로 캘 이동이 기존보다 2배 빨라진다(Manual 50% 가 빠지므로).
            //   더 느리게 쓰시려면 CALIBRATION 화면의 안전위치 이동 속도 % 를 낮추면 된다.
            // ================================================================
            velocity = axis.Config.GetRawDefaultVelocity() * factor;
            acceleration = axis.Config.GetRawAcceleration() * factor;
            deceleration = axis.Config.GetRawDeceleration() * factor;
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

        // ============================================================================
        // [캘 속도 전수 추적 2026-08-06]  사용자 지시: "분석 안되면 로그 전부 남겨. 캘쪽에 전부 남겨."
        //
        // 배경 — 실제 적용 속도가 화면 표시와 다른 이유를 추적하는 데 시간이 걸렸다.
        //   CONFIGURATION > SPEED 탭   : FrontPickerX DEFAULT VEL = 2000
        //   CALIBRATION 화면 안전이동 % : 10
        //   기대값 200 mm/s 인데 실측 100 mm/s 였다.
        //
        //   원인: 이중 스케일이다.
        //     최종속도 = DefaultVelocity x EffectiveScaleFactor x (SafeMovePercent / 100)
        //              = 2000 x 0.5 x 0.10 = 100
        //     EffectiveScaleFactor 0.5 는 캘이 Manual Sequence 스코프 안에서 돌아
        //     ManualSequencePercent(=50) 가 추가로 곱해진 값이다.
        //     (MotionSpeedScale.EffectiveScaleFactor:130 — Ready > Manual > 전역 순 우선)
        //     GetDefaultVel() 이 이미 스케일을 적용하므로 "스케일 적용값 x 퍼센트" 가 된다.
        //     이는 원본 속도 유출을 막기 위한 의도된 설계다(PickerSequenceBase:3097 주석).
        //
        // 이 함수는 그 유도 과정을 한 줄에 전부 남긴다. 다음부터는 로그만 보면
        // 어느 단계에서 몇 배가 곱해졌는지 즉시 확인된다.
        //
        // 실장비 확인:
        //   findstr /C:"CAL-SPEED-TRACE" D:\CDT-320\Log\Calibration_*.log
        // ============================================================================
        public static void LogSpeedTrace(
            string owner,
            string axisLabel,
            string description,
            BaseAxis axis,
            double safeMovePercent,
            bool safeMoveApplied,
            double finalVelocity,
            double finalAcceleration,
            double finalDeceleration,
            double target)
        {
            try
            {
                double rawVel = axis != null && axis.Config != null ? axis.Config.GetRawDefaultVelocity() : 0.0;
                double scaledVel = axis != null && axis.Config != null ? axis.Config.GetDefaultVel() : 0.0;
                double actual = axis != null ? axis.ActualPosition : 0.0;
                double distance = Math.Abs(target - actual);
                double expectedMs = finalVelocity > 0.0 ? (distance / finalVelocity) * 1000.0 : -1.0;

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Calibration", "CAL-SPEED-TRACE",
                    (owner ?? "-") + " 캘 이동 속도 유도. axis=" + (axisLabel ?? "-") +
                    ", target=" + (description ?? "-") +
                    " | rawDefaultVel=" + rawVel.ToString("F3") +
                    ", scaledDefaultVel=" + scaledVel.ToString("F3") +
                    ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6") +
                    ", manualSeqScale=" + MotionSpeedScale.IsManualSequenceScaleActive +
                    ", readySeqScale=" + MotionSpeedScale.IsReadySequenceScaleActive +
                    ", safeMovePercent=" + safeMovePercent.ToString("F3") +
                    ", safeMoveApplied=" + safeMoveApplied +
                    " | finalVel=" + finalVelocity.ToString("F3") +
                    ", finalAcc=" + finalAcceleration.ToString("F3") +
                    ", finalDec=" + finalDeceleration.ToString("F3") +
                    " | actual=" + actual.ToString("F3") +
                    ", targetPos=" + target.ToString("F3") +
                    ", distance=" + distance.ToString("F3") +
                    ", expectedMs=" + expectedMs.ToString("F0") +
                    " - Check");
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// [캘 속도 전수 추적 2026-08-06] 이동 완료 후 실제 소요시간과 도달 위치를 남긴다.
        /// 2026-08-06 03:22 VISION-FOCUS-CAL-REAR-AXIS-FINAL 처럼
        /// "축이 아직 움직이는데 위치 확인이 먼저 실행된" 조기 반환을 잡기 위한 것이다.
        /// (그 건은 430mm 이동에 4.3초가 필요한데 3.19초 만에 반환되어 316mm 지점에서 실패했다.)
        /// expectedMs 대비 elapsedMs 가 짧으면서 위치가 안 맞으면 조기 반환이다.
        /// </summary>
        public static void LogMoveCompletion(
            string owner,
            string axisLabel,
            string description,
            BaseAxis axis,
            double target,
            double expectedMs,
            long elapsedMs,
            int result)
        {
            try
            {
                double actual = axis != null ? axis.ActualPosition : 0.0;
                double remain = Math.Abs(target - actual);
                bool suspectEarly = expectedMs > 0.0 && elapsedMs < expectedMs * 0.9 && remain > 0.05;

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Calibration", "CAL-MOVE-DONE",
                    (owner ?? "-") + " 캘 이동 완료. axis=" + (axisLabel ?? "-") +
                    ", target=" + (description ?? "-") +
                    ", result=" + result +
                    ", targetPos=" + target.ToString("F3") +
                    ", actual=" + actual.ToString("F3") +
                    ", remain=" + remain.ToString("F3") +
                    ", expectedMs=" + expectedMs.ToString("F0") +
                    ", elapsedMs=" + elapsedMs +
                    ", moving=" + (axis != null && axis.IsMoving) +
                    ", inpos=" + (axis != null && axis.IsInPosition) +
                    (suspectEarly
                        ? " - ★조기 반환 의심: 예상 시간보다 빨리 끝났는데 목표에 미달★"
                        : " - Ok"));
            }
            catch
            {
            }
            finally
            {
            }
        }

        // ============================================================================
        // [안전이동 누락 감시 2026-08-06]  ★실장비 미검증 — 실장비에서 테스트 필요★
        //
        // 배경(사용자 지시 2026-08-06):
        //   CALIBRATION 화면의 "안전위치(Avoid) 이동 속도 %" 는 화면 문구대로
        //   "모든 캘리브레이션 공통" 이어야 한다. 즉 캘 동작 중 이동은 전부
        //   축 Default × SafeMovePercent 로 감속돼야 하고,
        //   ★예외는 각 캘의 "측정 Z 스트로크" 하나뿐★ 이다(X/Y는 전부 안전이동).
        //
        // 실제 상태(2026-08-06 로컬 코드 전수 조사):
        //   화면의 캘 11개 중 안전이동이 온전히 적용된 것은 2개뿐이었다.
        //     적용 : VisionCamera, NeedlePin
        //     부분 : ColletCalibration (픽커 이동 14개 중 2개)
        //     미적용: PickUpZ(0/10), PlaceZ(0/10), VisionFocus(0/6),
        //             ColletRotationCenter(0/1), ColletCleaning(0/5), NeedleZ(0)
        //
        // 근본 원인:
        //   useSafeMoveMotion 기본값이 false 인 "옵트인" 설계다. 호출부가 기억해서
        //   명시해야 하고, 잊으면 조용히 측정 속도로 나간다 — 알람도 로그도 없었다.
        //   그래서 ColletCleaning:196 / ColletCalibration:355 처럼
        //   "적용된다"는 주석과 실제 코드가 어긋난 곳까지 생겼다.
        //
        // 이 함수의 역할:
        //   캘 컨텍스트에서 안전이동이 아닌 이동이 나갈 때마다 흔적을 남긴다.
        //   호출부를 하나씩 채우는 작업(누락 보정)의 검증 수단이며,
        //   앞으로 새 캘/새 이동이 추가되면서 같은 누락이 재발하는 것도 드러낸다.
        //
        // 로그가 밀리지 않도록 (owner, axis) 조합별 1회만 남긴다.
        // 측정 Z 스트로크는 정상적으로 측정 속도를 쓰므로 호출부에서 이 함수를 부르지 않는다.
        //
        // 실장비 확인: 각 캘을 1회씩 실행한 뒤 아래로 검색한다.
        //   findstr /C:"CAL-SAFEMOVE-MISS" D:\CDT-320\Log\Main_*.log
        //   한 줄도 없으면 측정 Z 외 모든 캘 이동이 안전이동으로 나간 것이다.
        // ============================================================================
        private static readonly object SafeMoveMissSync = new object();
        private static readonly System.Collections.Generic.HashSet<string> SafeMoveMissLogged =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void LogSafeMoveMiss(
            string owner,
            string axisLabel,
            string description,
            double safeMovePercent,
            double velocity)
        {
            try
            {
                string key = (owner ?? "-") + "|" + (axisLabel ?? "-");
                lock (SafeMoveMissSync)
                {
                    if (!SafeMoveMissLogged.Add(key))
                        return;
                }

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Calibration", "CAL-SAFEMOVE-MISS",
                    "캘리브레이션 이동인데 안전이동(%)이 적용되지 않았습니다. owner=" + (owner ?? "-") +
                    ", axis=" + (axisLabel ?? "-") +
                    ", target=" + (description ?? "-") +
                    ", velocity=" + velocity.ToString("F6") +
                    ", safeMovePercent=" + safeMovePercent.ToString("F3") +
                    (safeMovePercent <= 0.0
                        ? ". SafeMovePercent 를 읽지 못했습니다(설정 확인 필요)."
                        : ". 측정 Z 스트로크가 아니라면 useSafeMoveMotion 누락입니다.") +
                    " - Check");
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// [안전이동 누락 감시 2026-08-06] 누락 집계를 초기화한다(캘 시작 시 호출).
        /// 실행마다 새로 판정해야 이전 실행의 1회-로그 억제가 다음 실행을 가리지 않는다.
        /// </summary>
        public static void ResetSafeMoveMissLog()
        {
            lock (SafeMoveMissSync)
                SafeMoveMissLogged.Clear();
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
