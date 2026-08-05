using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QMC.CDT320.Materials
{
    /// <summary>
    /// Material 뜨거운 경로(픽업 게이트 / DieMap 재구축 / 저장 캡처 락 구간)의 기준선 계측기입니다.
    /// - BeginSample/EndSample로 이름별 호출 수·누적·평균·최대 시간을 집계하고,
    ///   30초 주기로 한 줄 요약 로그만 남긴다 (호출당 로그 없음 — 20Hz 폴링 스팸 방지).
    /// - SetGauge로 State.Dies/Wafers 개수 같은 순간값을 요약에 포함한다.
    /// - 계측 실패는 운전에 영향을 주지 않도록 전부 무해화한다.
    /// 도입 목적: 성능 개선(캐시/인덱스/저장 분리) 전후 비교의 기준선 확보.
    /// </summary>
    internal static class MaterialPerfProbe
    {
        private const int FlushIntervalMs = 30000;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, ProbeStat> Stats = new Dictionary<string, ProbeStat>(StringComparer.Ordinal);
        private static readonly Dictionary<string, long> Gauges = new Dictionary<string, long>(StringComparer.Ordinal);
        private static long _lastFlushTimestamp = Stopwatch.GetTimestamp();

        private sealed class ProbeStat
        {
            public long Count;
            public double TotalMs;
            public double MaxMs;
        }

        /// <summary>측정 시작 시점을 반환한다. EndSample에 그대로 전달한다.</summary>
        public static long BeginSample()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <summary>측정을 종료하고 이름별 통계에 누적한다. 주기 도달 시 요약을 로그로 내보낸다.</summary>
        public static void EndSample(string name, long beginTimestamp)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return;

                double elapsedMs = (Stopwatch.GetTimestamp() - beginTimestamp) * 1000.0 / Stopwatch.Frequency;
                bool flushDue = false;

                lock (Sync)
                {
                    ProbeStat stat;
                    if (!Stats.TryGetValue(name, out stat))
                    {
                        stat = new ProbeStat();
                        Stats[name] = stat;
                    }

                    stat.Count++;
                    stat.TotalMs += elapsedMs;
                    if (elapsedMs > stat.MaxMs)
                        stat.MaxMs = elapsedMs;

                    double sinceFlushMs = (Stopwatch.GetTimestamp() - _lastFlushTimestamp) * 1000.0 / Stopwatch.Frequency;
                    if (sinceFlushMs >= FlushIntervalMs)
                    {
                        _lastFlushTimestamp = Stopwatch.GetTimestamp();
                        flushDue = true;
                    }
                }

                if (flushDue)
                    Flush();
            }
            catch
            {
                // 계측은 절대 운전을 방해하지 않는다.
            }
        }

        /// <summary>순간값 게이지를 갱신한다. 다음 요약 로그에 포함된다.</summary>
        public static void SetGauge(string name, long value)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return;

                lock (Sync)
                {
                    Gauges[name] = value;
                }
            }
            catch
            {
            }
        }

        private static void Flush()
        {
            try
            {
                List<KeyValuePair<string, ProbeStat>> statCopy;
                List<KeyValuePair<string, long>> gaugeCopy;
                lock (Sync)
                {
                    statCopy = new List<KeyValuePair<string, ProbeStat>>(Stats);
                    gaugeCopy = new List<KeyValuePair<string, long>>(Gauges);
                    Stats.Clear();
                }

                if (statCopy.Count == 0 && gaugeCopy.Count == 0)
                    return;

                var builder = new StringBuilder();
                builder.Append("Perf summary(30s):");
                for (int i = 0; i < statCopy.Count; i++)
                {
                    ProbeStat stat = statCopy[i].Value;
                    double avgMs = stat.Count > 0 ? stat.TotalMs / stat.Count : 0.0;
                    builder.Append(" ")
                        .Append(statCopy[i].Key)
                        .Append("[n=").Append(stat.Count)
                        .Append(", avg=").Append(avgMs.ToString("0.000"))
                        .Append("ms, max=").Append(stat.MaxMs.ToString("0.000"))
                        .Append("ms, total=").Append(stat.TotalMs.ToString("0"))
                        .Append("ms]");
                }

                for (int i = 0; i < gaugeCopy.Count; i++)
                {
                    builder.Append(" ")
                        .Append(gaugeCopy[i].Key)
                        .Append("=").Append(gaugeCopy[i].Value);
                }

                string line = builder.ToString() + " - Ok";

                // LogLevel 지정 오버로드는 LogPolicy 최소 로그 게이트를 거치지 않고 항상 디스크에 기록된다.
                // (Normal 4-인자 오버로드는 ProductionMinimal 모드에서 조기 반환되어 기준선이 유실됨 — 리뷰 확인)
                // 호출자가 _stateSync 등 핫 락을 보유한 채 EndSample→Flush에 진입할 수 있으므로
                // 실제 파일 기록은 백그라운드로 넘겨 락 보유 시간을 늘리지 않는다.
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        QMC.Common.Log.Write(QMC.Common.LogLevel.Normal, "Main", "MaterialPerfProbe", line);
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }
    }
}
