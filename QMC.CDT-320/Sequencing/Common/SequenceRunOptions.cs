using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 실행 대상 유닛과 실행 모드를 담는 옵션입니다.</summary>
    public class SequenceRunOptions
    {
        /// <summary>실행할 시퀀스 유닛 플래그입니다.</summary>
        public SequenceUnitKind Units { get; set; }

        /// <summary>시퀀스 실행 모드입니다.</summary>
        public SequenceRunMode Mode { get; set; }

        /// <summary>모든 유닛을 자동 모드로 실행하는 옵션을 생성합니다.</summary>
        public static SequenceRunOptions FullAuto()
        {
            return new SequenceRunOptions
            {
                Units = SequenceUnitKind.All,
                Mode = SequenceRunMode.Auto
            };
        }

        /// <summary>지정한 단일 유닛을 지정 모드로 실행하는 옵션을 생성합니다.</summary>
        public static SequenceRunOptions Single(SequenceUnitKind unit, SequenceRunMode mode)
        {
            if (unit == SequenceUnitKind.None)
                throw new ArgumentException("실행할 시퀀스 유닛이 필요합니다.", nameof(unit));

            return new SequenceRunOptions
            {
                Units = unit,
                Mode = mode
            };
        }
    }
}
