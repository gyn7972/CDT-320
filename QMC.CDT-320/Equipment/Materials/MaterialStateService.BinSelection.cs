using System;
using System.Collections.Generic;
using QMC.Common.Logging;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: [P4 2026-08-22] 픽업 BIN 선택 상태 (작업/LOT 단위).
    // - 기본 "All": 맵의 다이 전부 픽업(기존 동작과 동일).
    // - "Selected": PickupBinNumbers에 있는 bin만 픽업 대상(IsInputTarget) 유지.
    // - 적용 경계는 웨이퍼 단위: 맵 적용(DieMapping) 시점에 그때의 선택을 읽으므로
    //   진행 중 변경은 다음 웨이퍼부터 자연 반영된다(현재 웨이퍼 재적용은 별도 승인 항목).
    // - LOT 완료/레시피 변경 클리어 시 All로 복귀한다.
    public static partial class MaterialStateService
    {
        public const string PickupBinModeAll = "All";
        public const string PickupBinModeSelected = "Selected";

        /// <summary>현재 BIN 선택을 읽는다. 반환 목록은 복사본이다.</summary>
        public static void GetPickupBinSelection(out string mode, out List<int> binNumbers)
        {
            lock (_stateSync)
            {
                mode = NormalizePickupBinModeNoLock();
                binNumbers = State.PickupBinNumbers != null
                    ? new List<int>(State.PickupBinNumbers)
                    : new List<int>();
            }
        }

        /// <summary>
        /// Selected 모드가 실제로 유효한지(bin 목록 보유) 판정하고 선택 bin 집합을 돌려준다.
        /// All 모드(또는 빈 목록)면 false — 호출자는 필터 없이 진행한다.
        /// </summary>
        public static bool IsPickupBinFilterActive(out HashSet<int> selectedBins)
        {
            lock (_stateSync)
            {
                string mode = NormalizePickupBinModeNoLock();
                if (!string.Equals(mode, PickupBinModeSelected, StringComparison.OrdinalIgnoreCase) ||
                    State.PickupBinNumbers == null || State.PickupBinNumbers.Count == 0)
                {
                    selectedBins = null;
                    return false;
                }

                selectedBins = new HashSet<int>(State.PickupBinNumbers);
                return true;
            }
        }

        /// <summary>
        /// BIN 선택을 저장한다. Selected인데 목록이 비면 거부(fail-closed — "아무것도 안 집는 로트" 방지).
        /// </summary>
        public static bool TrySetPickupBinSelection(string mode, IEnumerable<int> binNumbers, string reason, out string failReason)
        {
            failReason = "";
            bool selected = string.Equals(mode, PickupBinModeSelected, StringComparison.OrdinalIgnoreCase);
            var normalized = new List<int>();
            if (binNumbers != null)
            {
                foreach (int bin in binNumbers)
                {
                    if (bin > 0 && !normalized.Contains(bin))
                        normalized.Add(bin);
                }
            }
            normalized.Sort();

            if (selected && normalized.Count == 0)
            {
                failReason = "Selected 모드에는 최소 1개의 BIN 번호가 필요합니다.";
                return false;
            }

            string before;
            lock (_stateSync)
            {
                before = DescribePickupBinSelectionNoLock();
                State.PickupBinMode = selected ? PickupBinModeSelected : PickupBinModeAll;
                State.PickupBinNumbers = selected ? normalized : new List<int>();
                NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "SetPickupBinSelection" : reason);
            }

            // [검토수정 2026-08-22] after는 이 호출이 쓴 값에서 직접 만든다 — 락 해제 후 재조회하면
            // 그 사이 끼어든 변경(LOT 완료의 All 복귀 등)이 찍혀 사고 조사 시 실제 선택값 복원이 불가능해진다.
            string after = selected
                ? PickupBinModeSelected + ":" + string.Join(",", normalized)
                : PickupBinModeAll;
            EventLogger.Write(EventKind.Event, "SYSTEM", "LOT-BIN-SELECT",
                "픽업 BIN 선택 변경. before=[" + before + "] -> after=[" + after + "]" +
                ", reason=" + (reason ?? "-") +
                " (적용은 다음 웨이퍼의 맵 적용 시점부터)");
            return true;
        }

        /// <summary>LOT 완료/레시피 변경 클리어 등 작업 경계에서 기본 All로 복귀시킨다.</summary>
        public static void ResetPickupBinSelectionToAll(string reason)
        {
            bool changed;
            lock (_stateSync)
            {
                changed = !string.Equals(NormalizePickupBinModeNoLock(), PickupBinModeAll, StringComparison.OrdinalIgnoreCase) ||
                          (State.PickupBinNumbers != null && State.PickupBinNumbers.Count > 0);
                if (changed)
                {
                    State.PickupBinMode = PickupBinModeAll;
                    State.PickupBinNumbers = new List<int>();
                    NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "ResetPickupBinSelection" : reason);
                }
            }

            if (changed)
            {
                EventLogger.Write(EventKind.Event, "SYSTEM", "LOT-BIN-SELECT",
                    "픽업 BIN 선택을 기본(All)으로 복귀. reason=" + (reason ?? "-"));
            }
        }

        public static string DescribePickupBinSelection()
        {
            lock (_stateSync)
                return DescribePickupBinSelectionNoLock();
        }

        private static string DescribePickupBinSelectionNoLock()
        {
            string mode = NormalizePickupBinModeNoLock();
            if (!string.Equals(mode, PickupBinModeSelected, StringComparison.OrdinalIgnoreCase))
                return PickupBinModeAll;

            var bins = State.PickupBinNumbers;
            return PickupBinModeSelected + ":" +
                   (bins == null || bins.Count == 0 ? "-" : string.Join(",", bins));
        }

        private static string NormalizePickupBinModeNoLock()
        {
            string mode = State != null ? State.PickupBinMode : null;
            return string.Equals(mode, PickupBinModeSelected, StringComparison.OrdinalIgnoreCase)
                ? PickupBinModeSelected
                : PickupBinModeAll;
        }

        // [P5 2026-08-24] GetMappedInputCassetteOccupiedSlots는 삭제 — 유일 소비자였던 매핑 직후
        // LOT 맵 전수 게이트가 "바코드=파일명" 전환으로 함께 삭제됐다(바코드 전 슬롯별 확인 불가).
    }
}
