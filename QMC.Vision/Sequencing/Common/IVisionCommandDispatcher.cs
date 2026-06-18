using QMC.Vision.Modules;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// 비전 명령(GRAB/MATCH/INSPECT 등) 실행 추상화 — TCP 서버와 Sim 자동 시퀀서가 같은 처리부를 공유하기 위한 seam.
    /// <para>현재는 <see cref="DirectVisionCommandDispatcher"/> 가 모듈 API 를 직접 호출한다.
    /// 추후 <c>VisionTcpServer</c> 의 명령 처리부(DoExpose/DoMatch/DoInspect)를 이 인터페이스로 추출·통합하여
    /// TCP 경로와 Sim 시퀀서가 동일 로직을 쓰도록 한다.</para>
    /// </summary>
    public interface IVisionCommandDispatcher
    {
        /// <summary>지정 모듈에 명령을 실행하고 결과 문자열(ACK 페이로드 상당)을 반환한다.</summary>
        string Execute(IVisionModule module, string cmd, string[] args);
    }
}
