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
                        fineMove).ConfigureAwait(false);
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
