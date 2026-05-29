using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 시퀀스 실행 모드.
    /// <list type="bullet">
    ///   <item><description><see cref="Auto"/> ? 게이트 없이 끝까지 자동 진행.</description></item>
    ///   <item><description><see cref="Manual"/> ? 매 step 마다 외부 Tick(StepOnce) 대기.</description></item>
    ///   <item><description><see cref="Step"/> ? N cycle 처리 후 자동 Pause (StepCycles).</description></item>
    ///   <item><description><see cref="DryRun"/> ? 모션 없이 흐름만 검증.</description></item>
    /// </list>
    /// </summary>
    public enum SequenceRunMode
    {
        /// <summary>게이트 없이 자동 진행.</summary>
        Auto,

        /// <summary>매 step 마다 외부 Tick 대기.</summary>
        Manual,

        /// <summary>지정 cycle 처리 후 Pause.</summary>
        Step,

        /// <summary>모션 없이 흐름만 검증.</summary>
        DryRun
    }

    /// <summary>
    /// 시퀀스로 구동할 유닛 선택 플래그. Main 시퀀스(Coordinator)에서
    /// 특정 한 개 유닛만 구동하고 싶을 때 사용.
    /// </summary>
    [Flags]
    public enum SequenceUnitKind
    {
        /// <summary>없음.</summary>
        None = 0,

        /// <summary>Input Loader (카세트 → InputStage 교환 위치).</summary>
        InputLoader = 1,

        /// <summary>Input Stage (웨이퍼 정렬 + 다이 위치 관리).</summary>
        InputStage = 2,

        /// <summary>Transfer Picker LEFT Arm.</summary>
        TpuLeft = 4,

        /// <summary>Transfer Picker RIGHT Arm.</summary>
        TpuRight = 8,

        /// <summary>Output Stage (Good/NG 분류 적재).</summary>
        OutputStage = 16,

        /// <summary>Output Unloader (완성 웨이퍼 → Output 카세트).</summary>
        OutputUnloader = 32,

        /// <summary>전체 유닛.</summary>
        All = InputLoader | InputStage | TpuLeft | TpuRight | OutputStage | OutputUnloader
    }

    /// <summary>
    /// 시퀀스 실행 옵션 ? Coordinator.Configure / MachineController.StartSequenceAsync 에 전달.
    /// </summary>
    public class SequenceRunOptions
    {
        /// <summary>구동할 유닛 (기본 전체).</summary>
        public SequenceUnitKind Units { get; set; } = SequenceUnitKind.All;

        /// <summary>실행 모드 (기본 Auto).</summary>
        public SequenceRunMode Mode { get; set; } = SequenceRunMode.Auto;

        /// <summary><see cref="SequenceRunMode.Step"/> 일 때 Pause 전 처리할 cycle 수.</summary>
        public int StepCycles { get; set; } = 1;

        /// <summary>전체 자동 운전 (모든 유닛 + Auto).</summary>
        public static SequenceRunOptions FullAuto()
            => new SequenceRunOptions { Units = SequenceUnitKind.All, Mode = SequenceRunMode.Auto };

        /// <summary>단일 유닛 구동.</summary>
        public static SequenceRunOptions Single(SequenceUnitKind unit, SequenceRunMode mode = SequenceRunMode.Auto)
            => new SequenceRunOptions { Units = unit, Mode = mode };
    }
}
