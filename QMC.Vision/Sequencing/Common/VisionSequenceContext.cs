using System;
using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 모듈 시퀀스가 비전 머신·명령 디스패처·로그 싱크에 접근하기 위한 실행 컨텍스트.
    /// 핸들러 MachineSequenceContext 미러(Controller→VisionMachine, Bus→Dispatcher).
    /// </summary>
    public class VisionSequenceContext
    {
        private readonly Action<string> _logSink;

        /// <summary>지정 머신/디스패처/로그싱크로 컨텍스트를 생성한다. 디스패처 미지정 시 직접 호출 디스패처 사용.</summary>
        public VisionSequenceContext(VisionMachine machine, IVisionCommandDispatcher dispatcher = null, Action<string> logSink = null)
        {
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            Dispatcher = dispatcher ?? new DirectVisionCommandDispatcher();
            _logSink = logSink;
        }

        /// <summary>비전 머신(5개 모듈 소유).</summary>
        public VisionMachine Machine { get; private set; }

        /// <summary>명령 실행 디스패처(GRAB/MATCH/INSPECT).</summary>
        public IVisionCommandDispatcher Dispatcher { get; private set; }

        /// <summary>각 모듈 시퀀스의 기본 사이클 간격(ms). 시퀀스 생성 시 적용.</summary>
        public int CycleIntervalMs { get; set; } = 500;

        /// <summary>모듈에 명령 실행. 예외는 삼키지 않고 결과 문자열로 변환해 시퀀스가 판단하도록 한다.</summary>
        public string Dispatch(IVisionModule module, string cmd, string[] args)
        {
            try { return Dispatcher.Execute(module, cmd, args); }
            catch (Exception ex) { return "ERR:" + ex.Message; }
        }

        /// <summary>공개 로그 싱크로 메시지 출력(미지정 시 무시).</summary>
        public void LogPublic(string message)
        {
            try { _logSink?.Invoke(message); }
            catch { }
        }
    }
}
