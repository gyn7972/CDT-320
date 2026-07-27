using System.Collections.Generic;

namespace QMC.CDT320.Sequencing
{
    // [동적 선행 대기점, 지시서 2026-07-27] 촬영(선행검사) 배치의 die별 비전 점유 X 좌표를
    // side별로 공개하는 정적 저장소. 허가(Grant) 이전 "촬영 진행 중" 배치 좌표에 접근할 기존
    // 경로가 없어 신설(설계 조사 B — PreparedItems는 시퀀스 지역변수라 외부 접근 불가).
    // 발행: InputDieVisionPrepareSequence.BuildPickBatch(배치 생성 시 덮어쓰기),
    // 제거: 예약 해제(실패/취소) 시,
    // 소비: PickerProcessSequence 동적 선행 대기 모니터(max 좌표만 사용).
    // 스냅샷 복사본만 보관/반환 — 라이브 리스트 공유 없음(prepare 진행 중 Remove 변형과 무관).
    internal static class InputDieVisionBatchCoordinateStore
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<PickerSequenceSide, List<double>> Coordinates =
            new Dictionary<PickerSequenceSide, List<double>>();

        public static void Publish(PickerSequenceSide side, IList<double> visionXs)
        {
            lock (Sync)
            {
                if (visionXs == null || visionXs.Count == 0)
                {
                    Coordinates.Remove(side);
                    return;
                }

                Coordinates[side] = new List<double>(visionXs);
            }
        }

        public static void Clear(PickerSequenceSide side)
        {
            lock (Sync)
            {
                Coordinates.Remove(side);
            }
        }

        // 배치의 min/max 비전 점유 X — 소비자가 페어 direction 부호에 따라 구속 극값을 고른다
        // (검증 F2/B3: direction<0(현행 Input 구성: 비전+1/픽커-1)이면 max, direction>0이면 min이
        //  배치 전체를 구속하는 최근접 좌표다).
        public static bool TryGetVisionXRange(PickerSequenceSide side, out double minVisionX, out double maxVisionX, out int dieCount)
        {
            minVisionX = 0.0;
            maxVisionX = 0.0;
            dieCount = 0;

            lock (Sync)
            {
                List<double> list;
                if (!Coordinates.TryGetValue(side, out list) || list == null || list.Count == 0)
                    return false;

                double min = list[0];
                double max = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    if (list[i] > max)
                        max = list[i];
                    if (list[i] < min)
                        min = list[i];
                }

                minVisionX = min;
                maxVisionX = max;
                dieCount = list.Count;
                return true;
            }
        }
    }
}
