using System;

namespace QMC.CDT320.Stats
{
    /// <summary>
    /// 작업 MAIN "작업 시간/UPH" 표시용 불변 스냅샷입니다.
    /// <para>
    /// 숫자/원시값만 보관하며 문자열 포맷은 하지 않습니다(포맷은 UI 책임).
    /// <see cref="ProductionStatsEngine"/>가 갱신마다 새 인스턴스를 만들어 통째로 교체하고,
    /// UI는 이 참조 하나만 lock 없이 읽습니다.
    /// </para>
    /// </summary>
    public sealed class ProductionStatsSnapshot
    {
        /// <summary>전부 0/"" 인 초기 스냅샷입니다.</summary>
        public static readonly ProductionStatsSnapshot Empty = new ProductionStatsSnapshot(
            0, 0, 0,
            0.0, 0.0,
            0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0,
            0.0, 0.0, 0.0,
            string.Empty);

        public ProductionStatsSnapshot(
            int processedDies,
            int goodCount,
            int ngCount,
            double cycleMsPerDieRolling,
            double cycleMsPerCycleInstant,
            double uphInstant,
            double uphEffective,
            double loadSeconds,
            double upSeconds,
            double contUpSeconds,
            double normalDownSeconds,
            double errorDownSeconds,
            double recoverySeconds,
            int errorCount,
            double mtbfSeconds,
            double mttrSeconds,
            double uptimeRatePercent,
            string activeLotId)
        {
            ProcessedDies = processedDies;
            GoodCount = goodCount;
            NgCount = ngCount;
            CycleMsPerDieRolling = cycleMsPerDieRolling;
            CycleMsPerCycleInstant = cycleMsPerCycleInstant;
            UphInstant = uphInstant;
            UphEffective = uphEffective;
            LoadSeconds = loadSeconds;
            UpSeconds = upSeconds;
            ContUpSeconds = contUpSeconds;
            NormalDownSeconds = normalDownSeconds;
            ErrorDownSeconds = errorDownSeconds;
            RecoverySeconds = recoverySeconds;
            ErrorCount = errorCount;
            MtbfSeconds = mtbfSeconds;
            MttrSeconds = mttrSeconds;
            UptimeRatePercent = uptimeRatePercent;
            ActiveLotId = activeLotId ?? string.Empty;
        }

        /// <summary>처리한 다이 총수.</summary>
        public int ProcessedDies { get; }
        /// <summary>양품 다이 수.</summary>
        public int GoodCount { get; }
        /// <summary>불량 다이 수.</summary>
        public int NgCount { get; }

        /// <summary>다이당 ms, 최근 20 다이 이동 평균(화면 표시 기준값).</summary>
        public double CycleMsPerDieRolling { get; }
        /// <summary>직전 1 사이클 소요 ms(순간값).</summary>
        public double CycleMsPerCycleInstant { get; }

        /// <summary>순간 UPH = 3600000 / 다이당 ms.</summary>
        public double UphInstant { get; }
        /// <summary>실효 UPH = 양품수 × 3600 / 가동초.</summary>
        public double UphEffective { get; }

        /// <summary>부하 시간[초] = 사이클 Start ~ 현재(또는 종료). 가동률 분모.</summary>
        public double LoadSeconds { get; }
        /// <summary>가동 시간[초] = AutoRunning 누적.</summary>
        public double UpSeconds { get; }
        /// <summary>연속 가동 시간[초] = 마지막 정지 이후 AutoRunning 연속 구간.</summary>
        public double ContUpSeconds { get; }
        /// <summary>통상 정지 시간[초] = Stopped/CycleStopped 누적.</summary>
        public double NormalDownSeconds { get; }
        /// <summary>이상 정지 시간[초] = Alarm 누적.</summary>
        public double ErrorDownSeconds { get; }
        /// <summary>이상 복귀 시간[초] = Alarm 해제 후 재가동까지.</summary>
        public double RecoverySeconds { get; }

        /// <summary>이상 정지 횟수(Alarm 진입 횟수).</summary>
        public int ErrorCount { get; }
        /// <summary>MTBF[초] = 가동초 / 이상정지횟수.</summary>
        public double MtbfSeconds { get; }
        /// <summary>MTTR[초] = 이상정지초 / 이상정지횟수.</summary>
        public double MttrSeconds { get; }
        /// <summary>가동률[%] = 가동초 / 부하초 × 100.</summary>
        public double UptimeRatePercent { get; }

        /// <summary>작업 중 LOT ID.</summary>
        public string ActiveLotId { get; }
    }
}
