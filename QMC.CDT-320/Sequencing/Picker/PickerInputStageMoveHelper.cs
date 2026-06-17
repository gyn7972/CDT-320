using System;
using System.Globalization;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;

namespace QMC.CDT320.Sequencing
{
    internal static class PickerInputStageMoveHelper
    {
        public static async Task<int> MoveStageYForPickerWorkPointCommandAsync(
            InputStageUnit stage,
            double workAreaVisionX,
            double targetStageY,
            bool fineMove,
            string owner)
        {
            try
            {
                if (stage == null || stage.StageY == null)
                    return -1;

                string targetName = BuildWorkPointTargetName(owner, workAreaVisionX);
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

        private static string BuildWorkPointTargetName(string owner, double workAreaVisionX)
        {
            string prefix = string.IsNullOrWhiteSpace(owner) ? "PickerInputStageWorkPoint" : owner;
            return prefix + ";InputStageWorkAreaX=" +
                workAreaVisionX.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
