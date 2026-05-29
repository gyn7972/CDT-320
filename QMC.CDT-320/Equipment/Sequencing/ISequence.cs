using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 실행 상태.</summary>
    public enum SequenceState
    {
        /// <summary>대기 ? 아직 시작 안 됨.</summary>
        Idle,

        /// <summary>실행 중.</summary>
        Running,

        /// <summary>일시 정지 (Pause / Manual 대기).</summary>
        Paused,

        /// <summary>정상 완료.</summary>
        Completed,

        /// <summary>사용자/Coordinator 에 의해 중단됨.</summary>
        Aborted,

        /// <summary>예외/알람으로 실패.</summary>
        Faulted
    }

    /// <summary>
    /// 병렬로 구동되는 단일 시퀀스(유닛 FSM)의 공통 인터페이스.
    /// <para>
    /// Coordinator(Main 시퀀스)가 여러 ISequence 를 Task 로 띄워 동시에 진행시키고,
    /// Pause/Resume/StepOnce/Abort 로 제어한다.
    /// </para>
    /// </summary>
    public interface ISequence
    {
        /// <summary>시퀀스 이름 (로그/UI 식별).</summary>
        string Name { get; }

        /// <summary>이 시퀀스가 담당하는 유닛.</summary>
        SequenceUnitKind Unit { get; }

        /// <summary>현재 상태.</summary>
        SequenceState State { get; }

        /// <summary>현재 step 의 사람이 읽을 수 있는 이름.</summary>
        string CurrentStep { get; }

        /// <summary>실행 모드 (Auto/Manual/Step/DryRun).</summary>
        SequenceRunMode Mode { get; set; }

        /// <summary>상태 변경 알림 (UI 구독용).</summary>
        event Action<ISequence, SequenceState> StateChanged;

        /// <summary>시퀀스를 끝(Completed)까지 실행. 취소 시 Aborted.</summary>
        Task RunAsync(CancellationToken ct);

        /// <summary>일시 정지 요청.</summary>
        void Pause();

        /// <summary>일시 정지 해제.</summary>
        void Resume();

        /// <summary>Manual 모드에서 한 step 진행 허용.</summary>
        void StepOnce();
    }
}
