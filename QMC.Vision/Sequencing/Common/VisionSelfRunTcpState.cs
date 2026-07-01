namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// Sim 자체 실행을 "실제 TCP 경로"로 구동할 때(자기 자신의 VisionTcpServer 로 루프백 접속) 활성 상태 플래그.
    /// <para>
    /// VisionTcpServer 의 RUN 게이트(<c>IsCommandAllowed</c>)는 평소 핸들러용으로 READY 상태에서만 명령을 허용한다.
    /// 자체 TCP 구동 중에는 핸들러/READY 가 없으므로, 이 플래그가 true 인 동안 게이트를 함께 열어(자기 명령 허용)
    /// MATCH/INSPECT 가 소켓을 통과하도록 한다. 핸들러 실제 모드에서는 항상 false 이므로 기존 동작에 영향이 없다.
    /// </para>
    /// </summary>
    public static class VisionSelfRunTcpState
    {
        /// <summary>자체 TCP 루프백 구동이 실행 중이면 true. <see cref="VisionAutoSequenceHost"/> 가 set/clear 한다.</summary>
        public static volatile bool Active;
    }
}
