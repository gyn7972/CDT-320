using System;
using System.Threading;
using QMC.Common.Diagnostics.TactTime;

namespace QMC.CDT320.Sequencing
{
    /// <summary>하위 시퀀스가 장비 컨트롤러, 장비 트리, 신호 버스에 접근하기 위한 실행 컨텍스트입니다.</summary>
    public class MachineSequenceContext
    {
        /// <summary>지정한 컨트롤러와 신호 버스로 시퀀스 컨텍스트를 생성합니다.</summary>
        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus)
            : this(controller, bus, new SequenceResourceManager(), null, null)
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus, SequenceResourceManager resources)
            : this(controller, bus, resources, null, null)
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus,
            SequenceResourceManager resources, SequenceActivityMonitor activity)
            : this(controller, bus, resources, activity, null)
        {
        }

        public MachineSequenceContext(MachineController controller, SequenceSignalBus bus,
            SequenceResourceManager resources, SequenceActivityMonitor activity, TactTimeRecorder tact)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Resources = resources ?? new SequenceResourceManager();
            Activity = activity ?? new SequenceActivityMonitor();
            Tact = tact ?? NullTactTimeRecorder.Instance;
            PickerPhases = new PickerPhaseCoordinator();
            AutoSequenceGate = new AutoSequenceCoordinatorGate(this);
            AutoLoaderGate = AutoSequenceGate;
            OutputPostPlaceInspections = new OutputPostPlaceInspectionQueue(this);
            WaferCompletion = new WaferCompletionRunCoordinator(this);
        }

        /// <summary>4개 유닛(INPUT/FRONT/REAR/OUTPUT)의 동작 상태를 보관하는 공식 상태 객체입니다.</summary>
        public SequenceActivityMonitor Activity { get; private set; }

        /// <summary>Auto/Manual/Step 시퀀스 택타임 계측기입니다.</summary>
        public TactTimeRecorder Tact { get; private set; }

        /// <summary>시퀀스를 구동하는 장비 컨트롤러입니다.</summary>
        public MachineController Controller { get; private set; }

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine { get { return Controller.Machine; } }

        /// <summary>유닛 간 핸드오프 신호 버스입니다.</summary>
        public SequenceSignalBus Bus { get; private set; }

        public SequenceResourceManager Resources { get; private set; }

        internal PickerPhaseCoordinator PickerPhases { get; private set; }
        internal AutoSequenceCoordinatorGate AutoSequenceGate { get; private set; }
        internal AutoSequenceCoordinatorGate AutoLoaderGate { get; private set; }
        internal OutputPostPlaceInspectionQueue OutputPostPlaceInspections { get; private set; }
        internal WaferCompletionRunCoordinator WaferCompletion { get; private set; }
        private int _cycleStopRequested;
        private CancellationTokenSource _cycleStopCts = new CancellationTokenSource();

        /// <summary>현재 자동 시퀀스가 사이클 경계에서 정지해야 하는지 여부입니다.</summary>
        public bool IsCycleStopRequested
        {
            get { return Volatile.Read(ref _cycleStopRequested) != 0; }
        }

        /// <summary>
        /// CYCLE STOP 요청 시 취소되는 토큰입니다.
        /// 경계 폴링(<see cref="StopIfCycleStopRequested(string)"/>)만으로는 이미 await에 진입한 대기를 깨울 수 없으므로,
        /// 운영자 확인창·핸드셰이크처럼 중단해도 안전한 대기에만 이 토큰을 함께 관찰시킵니다.
        /// 모션 완료 대기와 인터락 확인에는 사용하지 않습니다(진행 중 이동을 중간에 버리지 않기 위함).
        /// </summary>
        public CancellationToken CycleStopToken
        {
            get { return Volatile.Read(ref _cycleStopCts).Token; }
        }

        /// <summary>CYCLE STOP 요청을 초기화합니다.</summary>
        public void ResetCycleStopRequest()
        {
            Interlocked.Exchange(ref _cycleStopRequested, 0);
            Bus.Reset("CycleStopRequested");

            // 다음 Auto 실행이 이전 정지 요청을 물려받지 않도록 새 토큰으로 교체한다.
            // 이전 CTS는 Dispose하지 않는다. 직전 실행의 잔여 Task가 CycleStopToken을 읽는 순간
            // ObjectDisposedException이 발생해 정상 종료가 고장으로 바뀔 수 있기 때문이다.
            // 링크되지 않은 단순 root CTS이고 WaitHandle을 쓰지 않으므로 GC 회수로 충분하다.
            Interlocked.Exchange(ref _cycleStopCts, new CancellationTokenSource());
        }

        /// <summary>현재 진행 중인 큰 작업이 끝나는 지점에서 자동 시퀀스를 정지하도록 요청합니다.</summary>
        public void RequestCycleStop()
        {
            Interlocked.Exchange(ref _cycleStopRequested, 1);
            Bus.Set("CycleStopRequested");

            // 경계 폴링은 다음 Step으로 넘어갈 때만 동작한다. 운영자 확인창처럼 이미 대기 중인 지점은
            // 토큰을 취소해야 깨어나므로 여기서 함께 취소한다. 깨어난 대기는 경계 정지 경로로 합류한다.
            try
            {
                Volatile.Read(ref _cycleStopCts).Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// 전달받은 취소 토큰과 <see cref="CycleStopToken"/>을 함께 관찰하는 링크 토큰을 만듭니다.
        /// 반환된 객체는 반드시 <c>using</c>으로 해제합니다.
        /// </summary>
        public CancellationTokenSource CreateCycleStopLinkedSource(CancellationToken ct)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(ct, CycleStopToken);
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

        /// <summary>CYCLE STOP 요청이 있어도 현재 공정을 안전 경계까지 drain해야 하면 정지를 보류합니다.</summary>
        public void StopIfCycleStopRequested(string boundaryName, bool allowDrain, string drainReason)
        {
            if (!IsCycleStopRequested)
                return;

            if (allowDrain)
                return;

            StopIfCycleStopRequested(boundaryName);
        }

        /// <summary>장비 컨트롤러의 공개 로그 브리지로 메시지를 출력합니다.</summary>
        public void LogPublic(string message)
        {
            Controller.LogPublic(message);
        }

        public void RequestOperatorMessage(string title, string message)
        {
            Controller.RequestOperatorMessage(title, message);
        }
    }
}

