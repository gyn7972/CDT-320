using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal static class PickerPlaceSynchronizedArrivalMotion
    {
        public static async Task<InterpolatedMotionMoveResult> MoveStageYPickerXAndPickerZToPlaceAsync(
            BaseAxis outputStageY,
            double outputStageYTarget,
            BaseAxis pickerX,
            double pickerXTarget,
            BaseAxis pickerZ,
            double pickerZTarget,
            PickerPlaceMotionConfig config,
            CancellationToken ct)
        {
            try
            {
                if (config == null)
                    config = new PickerPlaceMotionConfig();
                config.Ensure();

                string readyReason;
                if (!IsAxisReady(outputStageY, "OutputStageY", out readyReason) ||
                    !IsAxisReady(pickerX, "PickerX", out readyReason) ||
                    !IsAxisReady(pickerZ, "PickerZ", out readyReason))
                {
                    return Fail("Place 보간 이동 전 축 준비 상태가 맞지 않습니다. " + readyReason);
                }

                int[] axes =
                {
                    outputStageY.Setup.AxisNo,
                    pickerX.Setup.AxisNo,
                    pickerZ.Setup.AxisNo
                };

                double[] rel =
                {
                    outputStageYTarget - outputStageY.ActualPosition,
                    pickerXTarget - pickerX.ActualPosition,
                    pickerZTarget - pickerZ.ActualPosition
                };

                return await AjinInterpolatedMotionService.RunSynchronizedArrivalRelativeMoveAsync(
                    config.InterpolationCoordinate,
                    axes,
                    rel,
                    config.SynchronizedVelocity,
                    config.SynchronizedAcceleration,
                    config.SynchronizedDeceleration,
                    config.SynchronizedTimeoutMs,
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("Place 보간 이동 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsAxisReady(BaseAxis axis, string name, out string reason)
        {
            reason = string.Empty;

            if (axis == null)
            {
                reason = name + " 축을 찾을 수 없습니다.";
                return false;
            }

            if (axis.Setup == null || axis.Setup.AxisNo < 0)
            {
                reason = name + " 축 번호가 설정되지 않았습니다.";
                return false;
            }

            if (!axis.IsServoOn)
            {
                reason = name + " 서보가 OFF 상태입니다.";
                return false;
            }

            if (axis.IsAlarm)
            {
                reason = name + " 축 알람이 ON 상태입니다.";
                return false;
            }

            if (axis.IsMoving)
            {
                reason = name + " 축이 이미 이동 중입니다.";
                return false;
            }

            return true;
        }

        private static InterpolatedMotionMoveResult Fail(string message)
        {
            return new InterpolatedMotionMoveResult
            {
                ResultCode = -1,
                Message = message
            };
        }
    }
}
