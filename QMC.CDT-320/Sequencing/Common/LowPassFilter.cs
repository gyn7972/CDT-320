using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 단일 채널 1차 저역통과 필터(EMA).
    /// 이산 갱신식: filtered = filtered + alpha * (input - filtered).
    /// 샘플링은 호출 1회 = 1샘플 고정(시간 기반 dt 없음)이며,
    /// alpha는 Cutoff Frequency fc(cycles/sample)에서 alpha = (2π·fc) / (2π·fc + 1)로 유도해
    /// (0,1] 범위로 클램프한다. 상태만 갖는 단순 클래스로, 스레드 안전은 서비스 계층이 담당한다.
    /// </summary>
    internal sealed class LowPassFilter
    {
        private double _value;

        /// <summary>fc(cycles/sample)로부터 alpha를 유도해 생성한다.</summary>
        public LowPassFilter(double cutoffFrequency)
        {
            Alpha = CalculateAlpha(cutoffFrequency);
            _value = 0.0;
        }

        /// <summary>현재 필터 계수 alpha (0,1].</summary>
        public double Alpha { get; private set; }

        /// <summary>현재 필터 출력값.</summary>
        public double Value
        {
            get { return _value; }
        }

        /// <summary>필터 상태를 지정 값으로 초기화한다.</summary>
        public void Reset(double initialValue)
        {
            _value = initialValue;
        }

        /// <summary>fc(cycles/sample)를 변경한다 — Alpha만 재계산하고 학습 상태(_value)는 유지한다.</summary>
        public void SetCutoffFrequency(double cutoffFrequency)
        {
            Alpha = CalculateAlpha(cutoffFrequency);
        }

        /// <summary>1샘플 갱신 후 필터 출력값을 반환한다.</summary>
        public double Update(double input)
        {
            _value = _value + Alpha * (input - _value);
            return _value;
        }

        /// <summary>fc(cycles/sample) → alpha 변환. 결과는 (0,1]로 클램프한다.</summary>
        public static double CalculateAlpha(double cutoffFrequency)
        {
            double fc = cutoffFrequency > 0.0 ? cutoffFrequency : 0.1;
            double omega = 2.0 * Math.PI * fc;
            double alpha = omega / (omega + 1.0);
            if (alpha <= 0.0)
                alpha = double.Epsilon;
            if (alpha > 1.0)
                alpha = 1.0;
            return alpha;
        }
    }
}
