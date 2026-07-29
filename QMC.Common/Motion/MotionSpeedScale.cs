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
    /// 자동 시퀀스 일반 이동은 <c>AppSettings</c> 의 <see cref="ScalePercent"/> 를 사용한다.
    /// 수동/READY 스코프에서는 전체 ScalePercent 와 독립적으로 각 전용 퍼센트를 사용한다.
    /// </summary>
    public static class MotionSpeedScale
    {
        /// <summary>퍼센트 허용 최소값.</summary>
        public const double MinPercent = 1.0;

        /// <summary>퍼센트 허용 최대값.</summary>
        public const double MaxPercent = 100.0;

        /// <summary>기본 퍼센트(스케일 미적용).</summary>
        public const double DefaultPercent = 100.0;

        /// <summary>Manual Sequence 속도 퍼센트 기본값입니다.</summary>
        public const double DefaultManualSequencePercent = 5.0;

        private static double _manualSequencePercent = DefaultManualSequencePercent;

        /// <summary>
        /// Manual Sequence Dialog / CYCLE RUN Step 수동 시퀀스에서 추가로 적용할 안전 속도 퍼센트입니다.
        /// 기본 5%이며 Manual Sequence 화면 하단의 "속도(%)" 입력으로 런타임에 1~100 범위에서 조정합니다.
        /// <see cref="EffectiveScaleFactor"/>를 통해 이동 속도와 가감속이 항상 같은 배율로 함께 스케일됩니다.
        /// </summary>
        public static double ManualSequencePercent
        {
            get { return _manualSequencePercent; }
            set { _manualSequencePercent = ClampPercent(value); }
        }

        /// <summary>READY 시퀀스 속도 퍼센트 기본값입니다.</summary>
        public const double DefaultReadySequencePercent = 5.0;

        private static double _readySequencePercent = DefaultReadySequencePercent;

        /// <summary>
        /// 작업 화면 READY 시퀀스에서만 추가로 적용할 안전 속도 퍼센트입니다.
        /// 전체 속도 ScalePercent 와 독립적으로 적용하며, Manual Sequence 화면의 "Ready 속도(%)"
        /// 입력으로 1~100 범위에서 조정하고 AppSettings 에 저장됩니다.
        /// Ready 복귀는 여러 축이 동시에 움직이므로 값을 올릴 때는 현장 확인 후 적용합니다.
        /// <see cref="EffectiveScaleFactor"/>를 통해 이동 속도와 가감속이 항상 같은 배율로 함께 스케일됩니다.
        /// </summary>
        public static double ReadySequencePercent
        {
            get { return _readySequencePercent; }
            set { _readySequencePercent = ClampPercent(value); }
        }

        /// <summary>스케일 적용 후 0 이하로 떨어지지 않도록 보장하는 최소 속도.</summary>
        private const double MinScaledVelocity = 0.001;

        /// <summary>스케일 적용 후 0 이하로 떨어지지 않도록 보장하는 최소 가감속도.</summary>
        private const double MinScaledAcceleration = 0.001;

        private static readonly object ScalePercentSync = new object();
        private static double _scalePercent = DefaultPercent;
        private static long _scalePercentRevision;
        private static int _manualSequenceScaleDepth;
        private static int _readySequenceScaleDepth;

        /// <summary>
        /// 현재 적용 중인 전체 DefaultVelocity 퍼센트(1~100).<br/>
        /// 0 이하/100 초과/숫자 아님 입력은 안전 범위로 보정해서 저장한다.
        /// </summary>
        public static double ScalePercent
        {
            get
            {
                lock (ScalePercentSync)
                    return _scalePercent;
            }
            set
            {
                double clamped = ClampPercent(value);
                lock (ScalePercentSync)
                {
                    if (_scalePercent != clamped)
                    {
                        _scalePercent = clamped;
                        _scalePercentRevision++;
                    }
                }
            }
        }

        /// <summary>통계 등에서 속도 값과 변경 번호를 같은 시점 기준으로 읽습니다.</summary>
        public static void GetScaleSnapshot(out double percent, out long revision)
        {
            lock (ScalePercentSync)
            {
                percent = _scalePercent;
                revision = _scalePercentRevision;
            }
        }

        /// <summary>현재 스케일 배율(0.01~1.0).</summary>
        public static double ScaleFactor
        {
            get { return ScalePercent / 100.0; }
        }

        /// <summary>Manual Sequence 추가 안전 스케일이 적용 중인지 여부입니다.</summary>
        public static bool IsManualSequenceScaleActive
        {
            get { return System.Threading.Volatile.Read(ref _manualSequenceScaleDepth) > 0; }
        }

        /// <summary>READY 시퀀스 추가 안전 스케일이 적용 중인지 여부입니다.</summary>
        public static bool IsReadySequenceScaleActive
        {
            get { return System.Threading.Volatile.Read(ref _readySequenceScaleDepth) > 0; }
        }

        /// <summary>
        /// 현재 실행 컨텍스트에 맞는 최종 스케일 배율입니다.
        /// Auto는 전체 ScalePercent, Manual/READY는 각 전용 퍼센트를 독립 적용합니다.
        /// </summary>
        public static double EffectiveScaleFactor
        {
            get
            {
                if (IsReadySequenceScaleActive)
                    return ClampPercent(ReadySequencePercent) / 100.0;
                if (IsManualSequenceScaleActive)
                    return ClampPercent(ManualSequencePercent) / 100.0;

                return ScaleFactor;
            }
        }

        /// <summary>
        /// Manual Sequence 범위 동안 DefaultVelocity/Acceleration/Deceleration을 추가 감속합니다.
        /// 반드시 using으로 감싸서 종료 시 원복되게 사용합니다.
        /// </summary>
        public static IDisposable BeginManualSequenceScale()
        {
            System.Threading.Interlocked.Increment(ref _manualSequenceScaleDepth);
            return new ManualSequenceScaleScope();
        }

        /// <summary>
        /// READY 시퀀스 범위 동안 DefaultVelocity/Acceleration/Deceleration을 추가 감속합니다.
        /// 반드시 using으로 감싸서 종료 시 원복되게 사용합니다.
        /// </summary>
        public static IDisposable BeginReadySequenceScale()
        {
            System.Threading.Interlocked.Increment(ref _readySequenceScaleDepth);
            return new ReadySequenceScaleScope();
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

            double scaled = velocity * EffectiveScaleFactor;
            return scaled < MinScaledVelocity ? MinScaledVelocity : scaled;
        }

        /// <summary>
        /// 전역 스케일 Scope를 열지 않고 호출자가 지정한 퍼센트로 DefaultVelocity를 계산합니다.
        /// Auto와 병행 가능한 Review 수동 이동처럼 해당 명령에만 속도를 적용할 때 사용합니다.
        /// </summary>
        public static double ApplyDefaultVelocityScale(double velocity, double percent)
        {
            if (velocity <= 0.0)
                return velocity;

            double scaled = velocity * (ClampPercent(percent) / 100.0);
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

            double scaled = acceleration * EffectiveScaleFactor;
            return scaled < MinScaledAcceleration ? MinScaledAcceleration : scaled;
        }

        /// <summary>
        /// 전역 스케일 Scope를 열지 않고 호출자가 지정한 퍼센트로 Default 가감속을 계산합니다.
        /// 속도와 같은 퍼센트를 전달해 특정 수동 명령의 모션 프로파일만 안전하게 감속합니다.
        /// </summary>
        public static double ApplyDefaultAccelerationScale(double acceleration, double percent)
        {
            if (acceleration <= 0.0)
                return acceleration;

            double scaled = acceleration * (ClampPercent(percent) / 100.0);
            return scaled < MinScaledAcceleration ? MinScaledAcceleration : scaled;
        }

        private sealed class ManualSequenceScaleScope : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                System.Threading.Interlocked.Decrement(ref _manualSequenceScaleDepth);
            }
        }

        private sealed class ReadySequenceScaleScope : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                System.Threading.Interlocked.Decrement(ref _readySequenceScaleDepth);
            }
        }

        /// <summary>스케일 반영 타임아웃 상한(ms). 1% 스케일에서도 무한 대기로 번지지 않게 제한한다.</summary>
        private const double MaxScaledTimeoutMs = 600000.0;

        /// <summary>
        /// 속도 스케일로 길어지는 이동 시간에 맞춰 타임아웃(ms)을 확장한다(C2, 2026-07-26).
        /// 100% 기준으로 튜닝된 타임아웃을 EffectiveScaleFactor 역수로 나눠 5% 스케일에서
        /// 팔로잉/회피 이동이 오탐 타임아웃(-21)으로 실패하지 않게 한다. 상한 600초.
        /// timeoutMs가 0 이하이면 그대로 반환한다(기본값 유지 의도 보존).
        /// </summary>
        public static int ScaleDefaultTimeoutMs(int timeoutMs)
        {
            if (timeoutMs <= 0)
                return timeoutMs;

            double factor = EffectiveScaleFactor;
            if (factor >= 1.0)
                return timeoutMs;
            if (factor < 0.01)
                factor = 0.01;

            double scaled = timeoutMs / factor;
            if (scaled > MaxScaledTimeoutMs)
                scaled = MaxScaledTimeoutMs;

            return (int)scaled;
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
