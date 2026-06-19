using System;

namespace QMC.Common.Motion
{
    /// <summary>
    /// 전체 공통 <see cref="AxisConfig.DefaultVelocity"/> 퍼센트 스케일.<br/>
    /// 자동 시퀀스 일반 이동에서 DefaultVelocity 를 실제 이동 속도로 해석할 때만 적용한다.
    /// <list type="bullet">
    ///   <item><description>전체 공통 퍼센트로만 적용한다. 축별 개별 퍼센트는 동작 균형/충돌 위험이 있어 금지한다.</description></item>
    ///   <item><description>명시 velocity(velocity &gt; 0), Jog 속도, Mapping ScanVelocity, HomeVelocity 에는 적용하지 않는다.</description></item>
    ///   <item><description>100% = DefaultVelocity/Acceleration/Deceleration 그대로, 10% = 각각 10%로 구동.</description></item>
    /// </list>
    /// 값은 <c>AppSettings</c> 의 설정으로 저장/로드되며, 로드 시 <see cref="ScalePercent"/> 로 동기화된다.
    /// </summary>
    public static class MotionSpeedScale
    {
        /// <summary>퍼센트 허용 최소값.</summary>
        public const double MinPercent = 1.0;

        /// <summary>퍼센트 허용 최대값.</summary>
        public const double MaxPercent = 100.0;

        /// <summary>기본 퍼센트(스케일 미적용).</summary>
        public const double DefaultPercent = 100.0;

        /// <summary>스케일 적용 후 0 이하로 떨어지지 않도록 보장하는 최소 속도.</summary>
        private const double MinScaledVelocity = 0.001;

        /// <summary>스케일 적용 후 0 이하로 떨어지지 않도록 보장하는 최소 가감속도.</summary>
        private const double MinScaledAcceleration = 0.001;

        private static double _scalePercent = DefaultPercent;

        /// <summary>
        /// 현재 적용 중인 전체 DefaultVelocity 퍼센트(1~100).<br/>
        /// 0 이하/100 초과/숫자 아님 입력은 안전 범위로 보정해서 저장한다.
        /// </summary>
        public static double ScalePercent
        {
            get { return _scalePercent; }
            set { _scalePercent = ClampPercent(value); }
        }

        /// <summary>현재 스케일 배율(0.01~1.0).</summary>
        public static double ScaleFactor
        {
            get { return _scalePercent / 100.0; }
        }

        /// <summary>퍼센트를 안전 범위(<see cref="MinPercent"/>~<see cref="MaxPercent"/>)로 보정한다.</summary>
        public static double ClampPercent(double percent)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent))
                return DefaultPercent;
            if (percent < MinPercent)
                return MinPercent;
            if (percent > MaxPercent)
                return MaxPercent;
            return percent;
        }

        /// <summary>
        /// DefaultVelocity 기반 일반 이동 속도에 전체 퍼센트 스케일을 적용한다.<br/>
        /// velocity 가 0 이하이면 스케일 없이 그대로 반환하고,
        /// 스케일 적용 결과가 0 이하가 되지 않도록 최소값을 보장한다.
        /// </summary>
        /// <param name="velocity">DefaultVelocity 로 해석된 이동 속도.</param>
        /// <returns>퍼센트 스케일이 적용된 이동 속도.</returns>
        public static double ApplyDefaultVelocityScale(double velocity)
        {
            if (velocity <= 0.0)
                return velocity;

            double scaled = velocity * ScaleFactor;
            return scaled < MinScaledVelocity ? MinScaledVelocity : scaled;
        }

        /// <summary>
        /// DefaultVelocity 기반 일반 이동의 가속도/감속도에 전체 퍼센트 스케일을 적용한다.
        /// </summary>
        /// <param name="acceleration">AxisConfig.Acceleration 또는 Deceleration 값.</param>
        /// <returns>퍼센트 스케일이 적용된 가감속도.</returns>
        public static double ApplyDefaultAccelerationScale(double acceleration)
        {
            if (acceleration <= 0.0)
                return acceleration;

            double scaled = acceleration * ScaleFactor;
            return scaled < MinScaledAcceleration ? MinScaledAcceleration : scaled;
        }

        /// <summary>
        /// 이미 계산되어 전달된 속도가 현재 DefaultVelocity 스케일 결과와 같은지 확인한다.
        /// 유닛 시퀀스가 스케일된 DefaultVelocity 를 명시 속도로 넘기는 기존 경로를 식별하기 위한 용도다.
        /// </summary>
        public static bool MatchesDefaultVelocityScale(double velocity, double defaultVelocity)
        {
            if (velocity <= 0.0 || defaultVelocity <= 0.0)
                return false;

            double scaled = ApplyDefaultVelocityScale(defaultVelocity);
            double tolerance = Math.Max(0.000001, Math.Abs(scaled) * 0.000001);
            return Math.Abs(velocity - scaled) <= tolerance;
        }
    }
}
