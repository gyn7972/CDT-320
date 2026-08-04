using System;

namespace QMC.CDT320.Materials
{
    internal static class MaterialSnapshotRevisionPolicy
    {
        // 5초마다 저장해도 약 15만 년의 여유가 있다. 이보다 큰 로드 값은
        // 실제 카운터가 아니라 손상/수동 변조로 보고 high-watermark 오염을 막는다.
        // 이 값은 손상 방지용 exclusive ceiling이다. ceiling 자체를 로드하거나
        // 새 snapshot에 발급하면 다음 저장에서 증가시킬 수 없으므로 사용하지 않는다.
        public const long MaximumTrustedLoadedRevision = 1000000000000L;

        public static long IssueNext(long stateRevision, long observedRevision, long lastIssuedRevision)
        {
            long baseline = Math.Max(
                Math.Max(Normalize(stateRevision), Normalize(observedRevision)),
                Normalize(lastIssuedRevision));
            if (baseline >= MaximumTrustedLoadedRevision - 1L)
                throw new InvalidOperationException("Material snapshot revision을 더 이상 증가시킬 수 없습니다.");

            return baseline + 1L;
        }

        public static bool IsSuperseded(long candidateRevision, long lastCommittedRevision)
        {
            return candidateRevision > 0L &&
                   candidateRevision <= Normalize(lastCommittedRevision);
        }

        public static long Normalize(long revision)
        {
            return revision < 0L ? 0L : revision;
        }

        public static bool IsTrustedLoadedRevision(long revision)
        {
            return revision >= 0L && revision < MaximumTrustedLoadedRevision;
        }
    }
}
