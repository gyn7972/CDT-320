using System;
using System.Threading;

namespace QMC.CDT320.Sequencing
{
    /// <summary>하위 시퀀스가 장비 컨트롤러, 장비 트리, 신호 버스에 접근하기 위한 실행 컨텍스트입니다.</summary>
    public class MachineSequenceContext
    {
        /// <summary>지정한 컨트롤러와 신호 버스로 시퀀스 컨텍스트를 생성합니다.</summary>
        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus)
            : this(controller, bus, new SequenceResourceManager(), null)
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus, SequenceResourceManager resources)
            : this(controller, bus, resources, null)
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus,
            SequenceResourceManager resources, SequenceActivityMonitor activity)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Resources = resources ?? new SequenceResourceManager();
            Activity = activity ?? new SequenceActivityMonitor();
            PickerPhases = new PickerPhaseCoordinator();
            OutputPostPlaceInspections = new OutputPostPlaceInspectionQueue(this);
        }

        /// <summary>4개 유닛(INPUT/FRONT/REAR/OUTPUT)의 동작 상태를 보관하는 공식 상태 객체입니다.</summary>
        public SequenceActivityMonitor Activity { get; private set; }

        /// <summary>시퀀스를 구동하는 장비 컨트롤러입니다.</summary>
        public MachineController Controller { get; private set; }

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine { get { return Controller.Machine; } }

        /// <summary>유닛 간 핸드오프 신호 버스입니다.</summary>
        public SequenceSignalBus Bus { get; private set; }

        public SequenceResourceManager Resources { get; private set; }

        internal PickerPhaseCoordinator PickerPhases { get; private set; }
        internal OutputPostPlaceInspectionQueue OutputPostPlaceInspections { get; private set; }
        private int _cycleStopRequested;

        /// <summary>현재 자동 시퀀스가 사이클 경계에서 정지해야 하는지 여부입니다.</summary>
        public bool IsCycleStopRequested
        {
            get { return Volatile.Read(ref _cycleStopRequested) != 0; }
        }

        /// <summary>CYCLE STOP 요청을 초기화합니다.</summary>
        public void ResetCycleStopRequest()
        {
            Interlocked.Exchange(ref _cycleStopRequested, 0);
            Bus.Reset("CycleStopRequested");
        }

        /// <summary>현재 진행 중인 큰 작업이 끝나는 지점에서 자동 시퀀스를 정지하도록 요청합니다.</summary>
        public void RequestCycleStop()
        {
            Interlocked.Exchange(ref _cycleStopRequested, 1);
            Bus.Set("CycleStopRequested");
        }

        /// <summary>CYCLE STOP 요청이 있으면 지정한 경계에서 시퀀스를 정지합니다.</summary>
        public void StopIfCycleStopRequested(string boundaryName)
        {
            if (!IsCycleStopRequested)
                return;

            string reason = "CYCLE STOP 요청으로 현재 작업 경계에서 정지합니다. boundary=" +
                            (boundaryName ?? "-");
            LogPublic("[SEQ] " + reason);
            throw new SequenceStopException(reason);
        }

        /// <summary>장비 컨트롤러의 공개 로그 브리지로 메시지를 출력합니다.</summary>
        public void LogPublic(string message)
        {
            Controller.LogPublic(message);
        }
    }
}

