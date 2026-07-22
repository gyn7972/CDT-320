using System;
using System.Globalization;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal static class PickerInputStageMoveHelper
    {
        public static async Task<int> MoveStageYForPickerWorkPointCommandAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double targetStageY,
            bool fineMove,
            string owner,
            double? workAreaNeedleX = null,
            bool forceMove = false)
        {
            try
            {
                if (stage == null || stage.StageY == null)
                    return -1;

                string targetName = BuildWorkPointTargetName(owner, workAreaVisionX, workAreaNeedleX);
                using (MotionGuardRuntime.BeginAxisTeachingMove(stage.StageY, targetStageY, targetName))
                {
                    return await stage.MoveInputStageAxis(
                        WaferStageAxis.WaferY,
                        targetStageY,
                        fineMove,
                        forceMove).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerInputStageMove",
                    "InputStageY picker work point move command exception. owner=" +
                    owner + ", targetY=" + targetStageY.ToString("F3") +
                    ", workAreaVisionX=" + workAreaVisionX.ToString("F3") +
                    (workAreaNeedleX.HasValue ? ", workAreaNeedleX=" + workAreaNeedleX.Value.ToString("F3") : "") +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>
        /// InputCamera 선행검사에서만 사용하는 F3 이동 명령 생략 조건입니다.
        /// Actual/Command/Target 모두 F3 값이 같고, 기존 장비 완료 조건까지 만족할 때만 생략합니다.
        /// </summary>
        public static bool CanSkipInputCameraPreInspectionMoveAtThreeDecimals(
            BaseAxis axis,
            double target,
            double completionTolerance)
        {
            if (axis == null ||
                !IsFinite(axis.ActualPosition) ||
                !IsFinite(axis.CommandPosition) ||
                !IsFinite(target))
                return false;

            double tolerance = IsFinite(completionTolerance) && completionTolerance > 0.0
                ? completionTolerance
                : 0.05;

            // 기존 조건: AxisMoveWaiter.IsMoveCompletedAtTarget — 현재 기준: 동일 공식의
            // BaseAxis.IsAtTargetPosition(정지+무알람+서보ON+Actual/Command 톨러런스)로 통일(R2).
            return IsSameAtThreeDecimals(axis.ActualPosition, target) &&
                   IsSameAtThreeDecimals(axis.CommandPosition, target) &&
                   axis.IsAtTargetPosition(target, tolerance);
        }

        public static async Task<int> MoveStageYForPickerWorkPointCommandAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double targetStageY,
            JogSpeedType speedType,
            double customSpeed,
            string owner,
            double? workAreaNeedleX = null)
        {
            try
            {
                if (stage == null || stage.StageY == null)
                    return -1;

                string targetName = BuildWorkPointTargetName(owner, workAreaVisionX, workAreaNeedleX);
                using (MotionGuardRuntime.BeginAxisTeachingMove(stage.StageY, targetStageY, targetName))
                {
                    return await stage.MoveInputStageAxis(
                        WaferStageAxis.WaferY,
                        targetStageY,
                        speedType,
                        customSpeed).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerInputStageMove",
                    "InputStageY Picker 작업 위치 조그 프로파일 이동 중 예외가 발생했습니다. owner=" +
                    owner + ", targetY=" + targetStageY.ToString("F3") +
                    ", workAreaVisionX=" + workAreaVisionX.ToString("F3") +
                    (workAreaNeedleX.HasValue ? ", workAreaNeedleX=" + workAreaNeedleX.Value.ToString("F3") : "") +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public static string BuildLastStageMoveFailure(InputStageUnit stage)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", stageMoveFailure=" + stage.LastStageMoveFailureMessage;
        }

        private static bool IsSameAtThreeDecimals(double left, double right)
        {
            return Math.Round(left, 3, MidpointRounding.AwayFromZero) ==
                   Math.Round(right, 3, MidpointRounding.AwayFromZero);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string BuildWorkPointTargetName(string owner, double workAreaVisionX, double? workAreaNeedleX)
        {
            string prefix = string.IsNullOrWhiteSpace(owner) ? "PickerInputStageWorkPoint" : owner;
            string targetName = prefix + ";InputStageWorkAreaX=" +
                workAreaVisionX.ToString("R", CultureInfo.InvariantCulture);
            if (workAreaNeedleX.HasValue)
            {
                // 현재 기준: StageY 실제 간섭 반경은 CameraX가 아니라 NeedleX/StageY 좌표로 계산한다.
                targetName += ";InputStageWorkAreaNeedleX=" +
                    workAreaNeedleX.Value.ToString("R", CultureInfo.InvariantCulture);
            }

            return targetName;
        }
    }
}
