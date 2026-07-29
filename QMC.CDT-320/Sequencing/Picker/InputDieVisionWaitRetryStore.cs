using System;
using System.Collections.Generic;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Input die vision 실패/과대 보정으로 Die를 Wait로 되돌린 횟수 보관소.
    /// [사용자 확정 2026-07-29] 같은 Die가 계속 실패하면 영구 루프가 되므로 상한(기본 3회)을 두고,
    ///   초과 시 기존 SKIP(IsInputTarget=false, 영구 제외)으로 전환한다.
    ///
    /// 보관 방식은 휘발성(메모리) — MaterialSnapshot/DieMaterial에 필드를 추가하지 않는다.
    ///   직렬화 키 추가는 마이그레이션·하위호환 부담이 있고 이 카운터는 그 대상이 아니다.
    ///   앱 재시작 시 카운터가 사라져 해당 Die가 상한을 다시 받는 것은 허용한다
    ///   (자재를 버리지 않는 쪽이 안전측). 영속으로 바꾸려면 사용자 승인 후 변경할 것.
    ///
    /// 초기화 시점: 새 웨이퍼 InputStage 로딩 완료 / 자동 운전 시작(START).
    /// Front/Rear 시퀀스 스레드에서 동시 접근되므로 lock으로 보호한다.
    /// 비전 실패와 과대 보정은 "이 Die를 못 찍었다"는 같은 문제이므로 카운터를 분리하지 않는다.
    /// </summary>
    internal static class InputDieVisionWaitRetryStore
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, int> Counts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>해당 Die의 Wait 반환 횟수를 1 증가시키고 증가 후 값을 반환한다.</summary>
        public static int Increment(string dieId)
        {
            if (string.IsNullOrWhiteSpace(dieId))
                return 0;

            lock (Sync)
            {
                int current;
                if (!Counts.TryGetValue(dieId, out current))
                    current = 0;

                current++;
                Counts[dieId] = current;
                return current;
            }
        }

        /// <summary>해당 Die의 현재 Wait 반환 횟수(증가 없이 조회).</summary>
        public static int GetCount(string dieId)
        {
            if (string.IsNullOrWhiteSpace(dieId))
                return 0;

            lock (Sync)
            {
                int current;
                return Counts.TryGetValue(dieId, out current) ? current : 0;
            }
        }

        /// <summary>전체 카운터를 초기화한다(웨이퍼 교체 / 자동 운전 시작).</summary>
        public static void ClearAll(string reason)
        {
            int cleared;
            lock (Sync)
            {
                cleared = Counts.Count;
                if (cleared == 0)
                    return;

                Counts.Clear();
            }

            EventLogger.Write(
                EventKind.Event,
                "COORD",
                "INPUT-DIE-VISION-WAIT-RETRY",
                "Input die vision Wait 재시도 카운터를 초기화했습니다. reason=" + (reason ?? string.Empty) +
                ", clearedCount=" + cleared);
        }
    }
}
