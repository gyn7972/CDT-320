using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Alarms;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 시퀀스가 하드웨어/인터록/알람/시그널버스에 접근하기 위한 어댑터.
    /// <para>
    /// 구체 시퀀스는 이 인터페이스만 의존하므로 단위 테스트 시 mock 으로 대체 가능.
    /// 실제 구현은 <see cref="MachineSequenceContext"/> (MachineController + CDT320_Machine 래핑).
    /// </para>
    /// </summary>
    public interface ISequenceContext
    {
        /// <summary>하드웨어 트리 루트.</summary>
        CDT320_Machine Machine { get; }

        /// <summary>MachineController (Action Library) ? 시퀀스가 호출하는 원자적 동작 모음.</summary>
        MachineController Controller { get; }

        /// <summary>유닛 간 시그널/티켓 채널.</summary>
        SequenceSignalBus Signals { get; }

        /// <summary>활성 차단 알람(Error 이상)이 있는지. true 면 시퀀스는 Hold.</summary>
        bool HasBlockingAlarm { get; }

        /// <summary>인터록 검증 ? false 면 이동 차단 + reason.</summary>
        bool VerifyMove(BaseAxis axis, double target, out string reason);

        /// <summary>알람 발생 + 통계 누적.</summary>
        void Alarm(string code, string source, string message, AlarmSeverity severity = AlarmSeverity.Error);

        /// <summary>로그 발행.</summary>
        void Log(string message);

        /// <summary>인터록 검증 후 절대 위치 이동. 차단 시 false.</summary>
        Task<bool> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0);
    }
}
