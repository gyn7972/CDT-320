using System.Threading.Tasks;
using QMC.Common.Motion;
using QMC.CDT320.Alarms;
using QMC.CDT320.Interlocks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// <see cref="ISequenceContext"/> 의 실제 구현 ? MachineController(Action Library) 와
    /// CDT320_Machine(하드웨어 트리)을 래핑.
    /// <para>
    /// 알람/인터록은 기존 전역 <see cref="AlarmManager"/> / <see cref="InterlockRegistry"/> 를 그대로 사용하여
    /// 시퀀스 계층과 레거시 MachineController 계층이 동일한 안전 정책을 공유한다.
    /// </para>
    /// </summary>
    public sealed class MachineSequenceContext : ISequenceContext
    {
        private readonly MachineController _controller;

        public MachineSequenceContext(MachineController controller, SequenceSignalBus signals)
        {
            _controller = controller ?? throw new System.ArgumentNullException(nameof(controller));
            Signals = signals ?? new SequenceSignalBus();
        }

        /// <inheritdoc/>
        public CDT320_Machine Machine => _controller.Machine;

        /// <inheritdoc/>
        public MachineController Controller => _controller;

        /// <inheritdoc/>
        public SequenceSignalBus Signals { get; }

        /// <inheritdoc/>
        public bool HasBlockingAlarm
        {
            get
            {
                var active = AlarmManager.Active;
                if (active == null) return false;
                foreach (var a in active)
                    if (a.Severity >= AlarmSeverity.Error) return true;
                return false;
            }
        }

        /// <inheritdoc/>
        public bool VerifyMove(BaseAxis axis, double target, out string reason)
        {
            if (axis == null) { reason = "axis is null"; return false; }
            return InterlockRegistry.VerifyMove(axis.Name, target, out reason);
        }

        /// <inheritdoc/>
        public void Alarm(string code, string source, string message, AlarmSeverity severity = AlarmSeverity.Error)
        {
            AlarmManager.Raise(severity, code, source, message);
            _controller.ErrorCount++;
        }

        /// <inheritdoc/>
        public void Log(string message) => _controller.LogPublic(message);

        /// <inheritdoc/>
        public Task<bool> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0)
            => _controller.MoveAxisAsync(axis, position, velocity);
    }
}
