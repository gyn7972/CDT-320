using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320
{
    /// <summary>Unit이 소유한 축의 조그 동작을 Unit 내부 함수로 처리하기 위한 공통 계약입니다.</summary>
    public interface IUnitJogController
    {
        bool CanHandleJogAxis(BaseAxis axis);

        Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance);

        Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed);

        Task<int> StopJogAsync(BaseAxis axis);
    }

    internal static class UnitJogVelocityResolver
    {
        public static double Resolve(BaseAxis axis, JogSpeedType speedType, double customSpeed)
        {
            if (axis == null || axis.Config == null)
                return customSpeed > 0.0 ? customSpeed : 0.0;

            if (speedType == JogSpeedType.Custom)
                return customSpeed > 0.0 ? customSpeed : axis.Config.JogFineVelocity;

            if (speedType == JogSpeedType.Coarse)
                return axis.Config.JogCoarseVelocity;

            return axis.Config.JogFineVelocity;
        }
    }
}
