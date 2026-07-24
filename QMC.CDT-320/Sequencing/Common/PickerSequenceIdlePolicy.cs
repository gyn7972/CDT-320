namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Front/Rear 픽커 유휴 대기 루프의 공통 폴 주기.<br/>
    /// 기존 1ms 폴은 빈 재진입과 축 상태 조회 폭주(CPU/로그)를 유발했다.
    /// AutoSequenceCoordinatorGate와 동일한 20ms를 기본으로 하고, 장비 설정(PickerIdlePollMs)으로 조정한다.
    /// 대기 루프의 부수 임무(교체 준비 publish/Avoid 유지/CycleStop 관찰) 주기가 함께 늘어나므로 500ms를 상한으로 둔다.
    /// </summary>
    internal static class PickerSequenceIdlePolicy
    {
        private const int DefaultDelayMs = 20;
        private const int MinDelayMs = 1;
        private const int MaxDelayMs = 500;

        private static int _idlePollDelayMs = DefaultDelayMs;

        public static int IdlePollDelayMs
        {
            get { return _idlePollDelayMs; }
        }

        /// <summary>장비 설정 로드 시 적용한다. 범위를 벗어나면 안전 범위로 보정한다.</summary>
        public static void Configure(int delayMs)
        {
            if (delayMs < MinDelayMs)
                delayMs = DefaultDelayMs;
            if (delayMs > MaxDelayMs)
                delayMs = MaxDelayMs;
            _idlePollDelayMs = delayMs;
        }
    }
}
