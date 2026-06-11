using System;

namespace QMC.CDT320.Sequencing
{
    /// <summary>하위 시퀀스가 장비 컨트롤러, 장비 트리, 신호 버스에 접근하기 위한 실행 컨텍스트입니다.</summary>
    public class MachineSequenceContext
    {
        /// <summary>지정한 컨트롤러와 신호 버스로 시퀀스 컨텍스트를 생성합니다.</summary>
        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus)
            : this(controller, bus, new SequenceResourceManager())
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus, SequenceResourceManager resources)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Resources = resources ?? new SequenceResourceManager();
        }

        /// <summary>시퀀스를 구동하는 장비 컨트롤러입니다.</summary>
        public MachineController Controller { get; private set; }

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine { get { return Controller.Machine; } }

        /// <summary>유닛 간 핸드오프 신호 버스입니다.</summary>
        public SequenceSignalBus Bus { get; private set; }

        public SequenceResourceManager Resources { get; private set; }

        /// <summary>장비 컨트롤러의 공개 로그 브리지로 메시지를 출력합니다.</summary>
        public void LogPublic(string message)
        {
            Controller.LogPublic(message);
        }
    }
}
