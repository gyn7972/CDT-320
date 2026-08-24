using System;
using System.Linq;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Stats;
using QMC.Common;
using QMC.Common.Logging;

namespace QMC.CDT320.Lots
{
    /// <summary>
    /// LOT 시작/완료의 단일 진입점.
    ///
    /// [신규 2026-07-27] 기존에 LOT 체계가 두 갈래로 갈라져 있었고 한쪽은 死코드였다.
    ///  - 살아있던 경로: RecipeProject.LotId -> MaterialSnapshot.LotId -> GetProductionLotId()
    ///    -> TactTime CSV / 웨이퍼·검사 CSV / 비전 요청 전문 / 생산통계 / 화면 표시
    ///  - 죽어있던 경로: LotStorage.OpenLot/CloseLot/RecordDie. OpenLot 호출부가 커맨드라인
    ///    정상 자동 시퀀스 시작 시 ActiveLot이 준비되지 않는 경우를 차단한다.
    ///  - 게다가 두 경로의 ID 규칙이 달라(레시피 문자열 vs "LOT-yyyyMMdd-HHmmss")
    ///    StatePage / WorkMainPage / LiveLotMapView 세 곳이 ID 불일치로 lot을 강제 폐기했고,
    ///    화면에는 항상 "(no active lot)"만 보였다.
    ///
    /// 이 서비스는 두 체계를 "입력한 LOT ID 하나"로 묶는다. LotStorage.OpenLot에 자동 생성 ID가
    /// 아니라 사용자가 입력한 ID를 그대로 넘기므로, 위 세 곳의 비교가 저절로 성립한다.
    ///
    /// 사용자 확정 사항(2026-07-27):
    ///  - 여러 카세트를 묶어 한 LOT으로 본다. 입력 카세트 소진은 LOT 완료가 아니다.
    ///  - LOT 완료는 작업자가 명시적으로 요청할 때만 발생한다.
    ///  - 활성 LOT이 없으면 자동 운전을 차단한다. 임시 LOT을 자동 생성하지 않는다.
    ///  - LOT ID를 시작하면 활성 레시피(RecipeProject.LotId)에도 기록한다.
    /// </summary>
    public static class LotSessionService
    {
        /// <summary>현재 진행 중인 LOT이 있는지 여부.</summary>
        public static bool IsLotActive
        {
            get { return LotStorage.ActiveLot != null; }
        }

        /// <summary>현재 진행 중인 LOT ID. 없으면 빈 문자열.</summary>
        public static string ActiveLotId
        {
            get
            {
                Lot lot = LotStorage.ActiveLot;
                return lot != null ? (lot.LotID ?? "") : "";
            }
        }

        /// <summary>
        /// 프로그램 시작 시 진행 중이던 LOT을 되살린다.
        ///
        /// [LOT 관리 2026-07-27] LOT은 여러 카세트에 걸치고, 완료는 작업자가 명시적으로 누를 때만
        /// 발생한다(사용자 확정 사항). 따라서 프로그램 재시작이 LOT을 끝내서는 안 된다.
        /// 생산 LOT ID는 Material 스냅샷에 남아 있으므로, 그 ID와 같은 Running LOT이
        /// Log\Lots 이력에 있으면 활성 LOT으로 되돌린다.
        ///
        /// Material 스냅샷 복구가 끝난 뒤에 호출해야 한다(Form1 시작 시퀀스 참고).
        /// </summary>
        public static bool RestoreActiveLotOnStartup()
        {
            return RestoreActiveLotOnStartup("");
        }

        /// <summary>
        /// Material 초기화 전에 읽어 둔 LOT ID를 구버전 데이터의 복구 후보로 함께 사용한다.
        /// 신규 버전에서는 State\active_lot.json 포인터를 우선하며, 포인터가 명시적으로 비어 있으면
        /// 과거 Running 이력을 임의로 선택하지 않는다.
        /// </summary>
        public static bool RestoreActiveLotOnStartup(string materialLotIdBeforeReset)
        {
            try
            {
                if (IsLotActive)
                {
                    SynchronizeActiveLotToMaterial("LotRestoreAlreadyActive");
                    return true;
                }

                string persistedLotId;
                bool pointerExists;
                string pointerError;
                bool pointerRead = LotStorage.TryGetPersistedActiveLotId(
                    out persistedLotId,
                    out pointerExists,
                    out pointerError);

                string materialLotId = MaterialStateService.GetProductionLotId();
                string fallbackLotId = string.IsNullOrWhiteSpace(materialLotId)
                    ? (materialLotIdBeforeReset ?? "")
                    : materialLotId;
                string lotId = "";

                if (pointerRead && pointerExists)
                {
                    // 빈 포인터도 의미가 있다. LOT 완료가 저장된 것이므로 과거 Running 이력을 되살리지 않는다.
                    if (string.IsNullOrWhiteSpace(persistedLotId))
                    {
                        Log.Write("Main", "SYSTEM", "LotRestore",
                            "활성 LOT 포인터에 진행 중 LOT이 없습니다. 과거 Running 이력은 자동 복원하지 않습니다. - Ok");
                        return false;
                    }

                    lotId = persistedLotId.Trim();
                }
                else
                {
                    // active_lot.json 도입 전 데이터는 Snapshot의 정확한 LOT ID로 1회 마이그레이션한다.
                    lotId = string.IsNullOrWhiteSpace(fallbackLotId) ? "" : fallbackLotId.Trim();
                    if (!pointerRead)
                    {
                        Log.Write("Main", "SYSTEM", "LotRestore",
                            pointerError + " Snapshot LOT ID를 이용한 제한 복구를 시도합니다. - Check");
                    }
                }

                if (string.IsNullOrWhiteSpace(lotId))
                {
                    Log.Write("Main", "SYSTEM", "LotRestore",
                        "활성 LOT 복구 건너뜀: 활성 포인터와 Material 스냅샷에 생산 LOT ID가 없습니다. " +
                        "diskRunningLots=" + DescribeRestorableLots() +
                        " - Check");
                    return false;
                }

                string restoreError;
                if (!LotStorage.TryRestoreActiveLot(lotId, out restoreError))
                {
                    Log.Write("Main", "SYSTEM", "LotRestore",
                        "활성 LOT을 복구하지 않았습니다. lot=" + lotId.Trim() +
                        ", detail=" + restoreError + " - Check");
                    return false;
                }

                // 사용자가 Material을 초기화했더라도 LOT 세션은 별도 수명이다.
                // 복원된 활성 LOT ID를 새 Material 상태에 다시 연결한다.
                SynchronizeActiveLotToMaterial("LotRestore");

                string message = "재시작 전 진행 중이던 LOT을 복구했습니다. lot=" + ActiveLotId;
                Log.Write("Main", "SYSTEM", "LotRestore", message + " - Ok");
                EventLogger.Write(EventKind.Event, "LOT", "LOT-RESTORE", message);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "LotRestore", "LOT 복구 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 레시피 변경 또는 Material 초기화가 발생해도 활성 LOT ID를 생산 상태에 다시 반영한다.
        /// LOT이 없을 때는 아무 값도 만들지 않는다.
        /// </summary>
        public static bool SynchronizeActiveLotToMaterial(string reason)
        {
            string activeLotId = ActiveLotId;
            if (string.IsNullOrWhiteSpace(activeLotId))
                return false;

            bool changed = MaterialStateService.SetProductionLotId(
                activeLotId,
                string.IsNullOrWhiteSpace(reason) ? "ActiveLotSync" : reason);

            if (changed)
            {
                Log.Write("Main", "SYSTEM", "LotPreserve",
                    "활성 LOT ID를 Material 상태에 다시 반영했습니다. lot=" + activeLotId +
                    ", reason=" + (reason ?? "") + " - Ok");
            }

            return true;
        }

        // To do: [LOT 복구] 복구 실패 로그에 "되살릴 후보가 있었는지"를 같이 남기기 위한 요약.
        /// <summary>
        /// 이력(디스크)에 남아 있는 진행 중 LOT을 요약한다.
        /// 스냅샷 LotId가 비어 복구를 건너뛸 때, 실제로 되살릴 LOT이 있었는지 로그만 보고 판단할 수 있게 한다.
        /// </summary>
        private static string DescribeRestorableLots()
        {
            try
            {
                var running = LotStorage.Lots.Values
                    .Where(l => l != null && l.State == LotState.Running)
                    .Select(l => l.LotID ?? "")
                    .Where(id => id.Length > 0)
                    .ToList();

                return running.Count == 0
                    ? "(없음)"
                    : running.Count + "건[" + string.Join(", ", running) + "]";
            }
            catch (Exception ex)
            {
                return "(조회 실패: " + ex.Message + ")";
            }
            finally
            {
            }
        }

        /// <summary>
        /// LOT을 시작한다.
        /// 검증을 모두 통과한 뒤에만 반영하며, 중간 실패 시 어떤 것도 적용하지 않는다(부분 적용 금지).
        /// </summary>
        /// <param name="machine">장비 객체(총 die 수 산출용). null 허용.</param>
        /// <param name="activeRecipeName">활성 레시피 이름. 비어 있으면 레시피 기록을 건너뛴다.</param>
        public static bool TryStartLot(
            CDT320_Machine machine,
            string activeRecipeName,
            string lotId,
            out string reason)
        {
            reason = "";
            try
            {
                // 1) 입력 검증
                string normalized = string.IsNullOrWhiteSpace(lotId) ? "" : lotId.Trim();
                if (normalized.Length == 0)
                {
                    reason = "LOT ID를 입력하세요.";
                    return false;
                }

                // 2) 중복 시작 방지
                if (IsLotActive)
                {
                    reason = "이미 진행 중인 LOT이 있습니다. 먼저 [LOT 완료]로 종료한 뒤 새 LOT을 시작하세요. 진행 중=" + ActiveLotId;
                    return false;
                }

                // 3) 레시피 기록 대상 확인(사용자 확정: 입력한 LOT ID를 레시피에도 남긴다).
                //    레시피 저장이 실패하면 LOT을 시작하지 않는다 — 재기동 시 LOT ID가 사라지는 것을 막는다.
                RecipeProject project = null;
                if (!string.IsNullOrWhiteSpace(activeRecipeName))
                {
                    project = RecipeStore.Load(activeRecipeName);
                    if (project == null)
                    {
                        reason = "활성 레시피를 불러오지 못해 LOT을 시작할 수 없습니다. recipe=" + activeRecipeName;
                        return false;
                    }
                }

                // ---- 여기서부터 실제 반영 ----

                string previousMaterialLotId = MaterialStateService.GetProductionLotId();
                string previousRecipeLotId = project != null ? project.LotId : null;

                // 4) 생산 LOT ID 설정. 이 값 하나로 TactTime/CSV/비전/통계/화면이 모두 따라온다.
                MaterialStateService.SetProductionLotId(normalized, "LotStart");

                // 5) 레시피에도 기록하고 저장한다.
                if (project != null)
                {
                    project.LotId = normalized;
                    if (!RecipeStore.Save(project))
                    {
                        // 레시피 저장 실패는 되돌린다(LOT ID만 남고 레시피가 어긋나는 상태 방지).
                        MaterialStateService.SetProductionLotId("", "LotStartRollback");
                        reason = "레시피에 LOT ID를 저장하지 못했습니다. recipe=" + activeRecipeName;
                        return false;
                    }
                }

                // 6) LotStorage에 동일 ID로 연다. 자동 생성 ID를 쓰지 않는 것이 핵심이다.
                int totalDies = ResolveTotalDies(machine);
                string recipeName = project != null
                    ? (project.FileName ?? activeRecipeName ?? "")
                    : (activeRecipeName ?? "");
                Lot openedLot;
                string storageError;
                if (!LotStorage.TryOpenLot(normalized, recipeName, totalDies, out openedLot, out storageError))
                {
                    // 활성 LOT 포인터까지 저장되지 않았으면 Material/Recipe 반영도 원래 값으로 되돌린다.
                    MaterialStateService.SetProductionLotId(previousMaterialLotId, "LotStartRollback");
                    if (project != null)
                    {
                        project.LotId = previousRecipeLotId;
                        if (!RecipeStore.Save(project))
                        {
                            Log.Write("Main", "SYSTEM", "LotStart",
                                "LOT 시작 실패 후 Recipe LOT ID 원복 저장도 실패했습니다. recipe=" +
                                (activeRecipeName ?? "") + " - Failed");
                        }
                    }

                    reason = storageError;
                    return false;
                }

                string message = "LOT을 시작했습니다. lot=" + normalized +
                                 ", recipe=" + (string.IsNullOrEmpty(recipeName) ? "(없음)" : recipeName) +
                                 ", totalDies=" + totalDies;
                Log.Write("Main", "SYSTEM", "LotStart", message + " - Ok");
                EventLogger.Write(EventKind.Event, "LOT", "LOT-START", message);
                return true;
            }
            catch (Exception ex)
            {
                reason = "LOT 시작 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "LotStart", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 진행 중인 LOT을 완료한다.
        /// 완료 직전 생산통계 스냅샷을 Lot에 옮겨 담는다.
        /// (다이 핫패스에 Lot.RecordDie를 붙이지 않는 이유: 택타임에 부하를 주지 않기 위함)
        /// </summary>
        public static bool TryCompleteLot(ProductionStatsEngine stats, out string reason)
        {
            reason = "";
            try
            {
                Lot lot = LotStorage.ActiveLot;
                if (lot == null)
                {
                    reason = "진행 중인 LOT이 없습니다.";
                    return false;
                }

                string lotId = lot.LotID ?? "";
                ApplyStatsSnapshot(lot, stats);

                // LOT 이력과 활성 포인터가 모두 저장되어야 완료로 인정한다.
                string closeError;
                if (!LotStorage.TryCloseLot(false, out closeError))
                {
                    reason = closeError;
                    return false;
                }

                // 사용자 확정 정책: LOT ID를 지우는 정상 경로는 명시적인 LOT 완료뿐이다.
                MaterialStateService.SetProductionLotId("", "LotComplete");
                // [P4 2026-08-22] BIN 선택은 작업(LOT) 단위 — LOT 완료 시 기본(All)로 복귀한다.
                MaterialStateService.ResetPickupBinSelectionToAll("LotComplete");
                ClearCompletedLotIdFromRecipe(lot.RecipeName, lotId);

                string message = "LOT을 완료했습니다. lot=" + lotId +
                                 ", 처리=" + lot.ProcessedDies +
                                 ", GOOD=" + lot.GoodCount +
                                 ", NG=" + lot.NgCount +
                                 ", 수율=" + lot.YieldPercent.ToString("F1") + "%";
                Log.Write("Main", "SYSTEM", "LotComplete", message + " - Ok");
                EventLogger.Write(EventKind.Event, "LOT", "LOT-COMPLETE", message);
                return true;
            }
            catch (Exception ex)
            {
                reason = "LOT 완료 실패: " + ex.Message;
                Log.Write("Main", "SYSTEM", "LotComplete", reason + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void ClearCompletedLotIdFromRecipe(string recipeName, string completedLotId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeName) || string.IsNullOrWhiteSpace(completedLotId))
                    return;

                RecipeProject project = RecipeStore.Load(recipeName);
                if (project == null ||
                    !string.Equals(project.LotId ?? "", completedLotId, StringComparison.Ordinal))
                    return;

                project.LotId = "";
                if (!RecipeStore.Save(project))
                {
                    Log.Write("Main", "SYSTEM", "LotComplete",
                        "LOT 완료 후 Recipe LOT ID 정리 저장에 실패했습니다. recipe=" + recipeName +
                        ", lot=" + completedLotId + " - Failed");
                }
            }
            catch (Exception ex)
            {
                // LOT 이력과 활성 포인터 완료가 끝난 뒤의 보조 정리 실패이므로 완료 자체를 되돌리지는 않는다.
                Log.Write("Main", "SYSTEM", "LotComplete",
                    "LOT 완료 후 Recipe LOT ID 정리 중 오류가 발생했습니다. recipe=" + recipeName +
                    ", lot=" + completedLotId + ", error=" + ex.Message + " - Failed");
            }
        }

        /// <summary>생산통계 스냅샷의 카운터를 Lot에 반영한다.</summary>
        private static void ApplyStatsSnapshot(Lot lot, ProductionStatsEngine stats)
        {
            try
            {
                if (lot == null || stats == null)
                    return;

                ProductionStatsSnapshot snapshot = stats.GetSnapshot();
                if (snapshot == null)
                    return;

                lot.ProcessedDies = snapshot.ProcessedDies;
                lot.GoodCount = snapshot.GoodCount;
                lot.NgCount = snapshot.NgCount;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "LotComplete",
                    "LOT 통계 반영에 실패했습니다(카운터 없이 저장). error=" + ex.Message + " - Check");
            }
            finally
            {
            }
        }

        /// <summary>
        /// LOT의 총 die 수를 산출한다.
        /// MachineController.ResolveProductionStatsTotalDies와 같은 기준(입력 대상 die 수)을 쓴다.
        /// </summary>
        private static int ResolveTotalDies(CDT320_Machine machine)
        {
            try
            {
                MaterialSnapshot state = MaterialStorage.State;
                if (state != null && state.Dies != null)
                {
                    int count = 0;
                    foreach (DieMaterial die in state.Dies)
                    {
                        if (die != null && die.IsInputTarget)
                            count++;
                    }

                    return count;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 0;
        }
    }
}
