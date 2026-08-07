using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: 코어 - 락/파생캐시(DieId 인덱스, InputPickContext)/식별자/LotId/Recipe 컨텍스트.
    // 도메인별 본체는 MaterialStateService.*.cs partial 파일에 있다 (2026-08-07 분할, 동작 변경 없음).
    public static partial class MaterialStateService
    {
        public static event Action<MaterialSnapshot> StateChanged;
        private static readonly object _stateSync = new object();
        private static readonly object _saveRequestSync = new object();
        private static readonly object _saveIoSync = new object();
        private static readonly object _stateChangedSync = new object();
        // Material snapshot is large during auto run. Keep UI/state events responsive,
        // but throttle full JSON disk saves so every die/inspection update does not
        // serialize/validate/replace the 10MB+ state file.
        private const int MaterialSaveQuietMs = 1000;
        private const int MaterialSaveMinimumIntervalMs = 5000;
        private const int MaterialSaveFailureRetryMs = 5000;
        private const int MaterialStateChangedQuietMs = 200;
        private const double InputStageThetaOffsetReadyEpsilon = 0.000001;
        private const double InputStageThetaMappingSnapshotToleranceDeg = 0.000001;
        private const double ProcessTestThetaAlignOffsetDeg = 0.000010;
        private static bool _saveWorkerRunning;
        private static bool _saveRequested;
        private static string _pendingSaveReason = "";
        private static DateTime _lastSaveCompletedUtc = DateTime.MinValue;
        private static bool _lastSaveSucceeded;
        private static bool _stateChangedQueued;
        private static DateTime _lastStateChangedAt = DateTime.MinValue;
        // _stateSync 보호: 저장 사본을 캡처할 때 발급한 가장 큰 Revision.
        private static long _lastIssuedSnapshotRevision;
        // _saveIoSync 보호: 디스크에 정상 커밋한 가장 큰 Revision.
        private static long _lastCommittedSnapshotRevision;
        // _saveRequestSync 보호: 저장 중 들어온 새 요청과 현재 시도를 구분한다.
        private static long _saveRequestVersion;

        public static MaterialSnapshot State => MaterialStorage.State;

        private static string CreateWaferInstanceId()
        {
            return Guid.NewGuid().ToString("N");
        }

        // [핫패스 인덱스 2026-08-05] DieId → DieMaterial 파생 인덱스.
        // - State.Dies 선형 스캔(FirstOrDefault, O(N))을 O(1) 사전 조회로 대체하는 런타임 전용 인덱스다.
        // - private static 필드: 스냅샷 그래프에 노출되면 CloneObject/직렬화가 따라가므로
        //   절대 public 프로퍼티로 만들지 않는다.
        // - 자기 치유: State 참조가 바뀌면(스냅샷 로드/교체) 접근 시점에 전체 재구축한다.
        // - 값이 List인 이유: 같은 DieId의 물리 Material 2개 이상 감지
        //   (GetOrCreateDieMaterial의 InvalidOperationException) 의미를 보존하기 위해서다.
        // - 접근/갱신은 전부 _stateSync 락 안에서만 수행한다.
        private static Dictionary<string, List<DieMaterial>> _dieByIdIndex;
        private static MaterialSnapshot _dieByIdIndexSource;

        private static Dictionary<string, List<DieMaterial>> GetDieByIdIndexNoLock()
        {
            if (_dieByIdIndex == null || !ReferenceEquals(_dieByIdIndexSource, State))
            {
                RebuildDieByIdIndexNoLock();
                // State 교체/전체 재구축 시 입력 pick 컨텍스트 캐시도 함께 초기화한다.
                _inputPickContextCache = null;
            }
            return _dieByIdIndex;
        }

        private static void RebuildDieByIdIndexNoLock()
        {
            MaterialSnapshot state = State;
            var index = new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);
            if (state != null && state.Dies != null)
            {
                for (int i = 0; i < state.Dies.Count; i++)
                {
                    DieMaterial die = state.Dies[i];
                    if (die == null || string.IsNullOrWhiteSpace(die.DieId))
                        continue;

                    List<DieMaterial> list;
                    if (!index.TryGetValue(die.DieId, out list))
                    {
                        list = new List<DieMaterial>(1);
                        index[die.DieId] = list;
                    }
                    // State.Dies 삽입 순서를 보존해 기존 FirstOrDefault 선택 의미를 유지한다.
                    list.Add(die);
                }
            }

            _dieByIdIndex = index;
            _dieByIdIndexSource = state;
        }

        private static void RegisterDieInIndexNoLock(DieMaterial die)
        {
            // 인덱스 미구축/다른 State면 다음 접근 때 전체 재구축되므로 여기서는 아무것도 안 해도 된다.
            if (die == null || string.IsNullOrWhiteSpace(die.DieId) ||
                _dieByIdIndex == null || !ReferenceEquals(_dieByIdIndexSource, State))
                return;

            List<DieMaterial> list;
            if (!_dieByIdIndex.TryGetValue(die.DieId, out list))
            {
                list = new List<DieMaterial>(1);
                _dieByIdIndex[die.DieId] = list;
            }
            list.Add(die);
        }

        private static void InvalidateDieByIdIndexNoLock()
        {
            _dieByIdIndex = null;
            _dieByIdIndexSource = null;
            // die 집합이 바뀌면 입력 pick 컨텍스트(맵/순서)도 함께 무효화한다.
            _inputPickContextCache = null;
        }

        private static DieMaterial FindDieByIdNoLock(string dieId)
        {
            if (string.IsNullOrWhiteSpace(dieId))
                return null;

            List<DieMaterial> list;
            if (!GetDieByIdIndexNoLock().TryGetValue(dieId, out list) || list == null || list.Count == 0)
                return null;

            return list[0];
        }

        // [핫패스 캐시 2026-08-05] 입력측 DieMap/Pick order 캐시 — 출력측 _outputReceiveOrderCache 동형.
        // 20Hz 픽업 게이트 1회가 (FinishComplete + 게이트 본체)에서 DieMap 전체 재구축과 승인 순서
        // 재구성을 최대 4회 반복하던 것을 wafer 상태 키 1개로 캐시한다.
        // 키 무효화 근거:
        //  - 맵 편집/매핑 적용/Review 승인 등 맵 골격을 바꾸는 경로는 전부 wafer.UpdatedAt(또는
        //    Generation/Revision/승인 필드)을 갱신한다. die 단위 픽 진행은 wafer를 갱신하지 않는다.
        //  - stale 안전 논거: pick 가능 판정의 최종 게이트는 항상 live die 검사
        //    (CanUseInputPickCandidate의 die-level 검사)이므로 캐시된 entry가 오래되어도
        //    잘못된 die가 pick 대상이 되지 않는다 — 손실은 entry-level 조기 스킵뿐이다.
        // 접근은 전부 _stateSync 락 안에서만 수행한다.
        private sealed class InputPickContext
        {
            public string Key;
            public DieMap Map;
            public List<DieMapEntry> Ordered;
            public bool ReviewApprovalValid;
            public string ReviewApprovalReason;
        }

        private static InputPickContext _inputPickContextCache;

        private static string BuildInputPickContextKeyNoLock(WaferMaterial wafer)
        {
            return (wafer.WaferInstanceId ?? "") + "|" +
                   wafer.InputStageProcessingGeneration + "|" +
                   (wafer.InputStageRunReviewMappingRevision ?? "") + "|" +
                   (wafer.HasInputStageRunReviewApproval ? "1" : "0") + "|" +
                   wafer.InputStageRunReviewStartDieIndex + "|" +
                   (wafer.InputStageRunReviewStartDieUid ?? "") + "|" +
                   (wafer.InputStageRunReviewOrderedDieIds != null ? wafer.InputStageRunReviewOrderedDieIds.Count : 0) + "|" +
                   (wafer.DieIds != null ? wafer.DieIds.Count : 0) + "|" +
                   wafer.UpdatedAt.Ticks;
        }

        private static bool TryResolveInputPickContextNoLock(WaferMaterial wafer, out InputPickContext context)
        {
            context = null;
            if (wafer == null)
                return false;

            string key = BuildInputPickContextKeyNoLock(wafer);
            InputPickContext cached = _inputPickContextCache;
            if (cached != null && string.Equals(cached.Key, key, StringComparison.Ordinal))
            {
                context = cached;
                return true;
            }

            DieMap map = BuildDieMapFromWafer(wafer);
            if (map == null || map.Entries == null || map.Entries.Count == 0)
                return false;   // 미완성 상태는 캐시하지 않는다 (게이트가 조기 탈출하는 구간)

            var built = new InputPickContext();
            built.Key = key;
            built.Map = map;

            if (wafer.HasInputStageRunReviewApproval)
            {
                List<DieMapEntry> approvedOrder;
                string approvalReason;
                built.ReviewApprovalValid = TryBuildApprovedInputStagePickOrder(
                    map, wafer, out approvedOrder, out approvalReason);
                built.ReviewApprovalReason = approvalReason ?? "";
                if (built.ReviewApprovalValid)
                {
                    built.Ordered = approvedOrder ?? new List<DieMapEntry>();
                }
                else
                {
                    // 기존 BuildInputStagePickOrder와 동일하게 recipe fallback을 차단하고 빈 순서를 유지한다.
                    // (기존에는 이 로그가 20Hz로 반복되었으나 캐시 도입으로 갱신 시 1회만 남는다)
                    Log.Write("Main", "MATERIAL", "InputStageRunReviewOrder",
                        "승인된 Input PickUp 순서를 복원하지 못해 recipe fallback을 차단했습니다. wafer=" +
                        (wafer.WaferId ?? "") + ", reason=" + built.ReviewApprovalReason + " - Blocked");
                    built.Ordered = new List<DieMapEntry>();
                }
            }
            else
            {
                var project = RecipeStore.LoadLastOrDefaultCached();
                PickupSubset pickup = ResolveInputPickup(project);
                built.ReviewApprovalValid = false;
                built.ReviewApprovalReason = "InputStage Review 승인이 없습니다.";
                built.Ordered = BuildOutputReceiveOrder(map, pickup) ?? new List<DieMapEntry>();
            }

            _inputPickContextCache = built;
            context = built;
            return true;
        }

        private static void InvalidateInputPickContextCacheNoLock()
        {
            _inputPickContextCache = null;
        }

        /// <summary>
        /// 입력 pick 컨텍스트 캐시를 명시적으로 무효화한다.
        /// MSS 밖(맵 편집 화면 등)에서 die/map을 직접 변이한 뒤 호출한다.
        /// </summary>
        public static void InvalidateInputPickContextCache(string reason)
        {
            try
            {
                lock (_stateSync)
                {
                    InvalidateInputPickContextCacheNoLock();
                }
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input pick context cache invalidate failed. reason=" + (reason ?? "") +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        /// <summary>
        /// [안전망] 파생 인덱스 총계와 State.Dies의 유효 die 수가 어긋나면(동기화 훅 누락 의심)
        /// 경고 로그 후 인덱스를 재구축한다. 저장 캡처 시(약 6초 주기) 호출된다.
        /// </summary>
        private static void VerifyDieByIdIndexConsistencyNoLock()
        {
            try
            {
                if (_dieByIdIndex == null || !ReferenceEquals(_dieByIdIndexSource, State))
                    return;

                int indexTotal = 0;
                foreach (List<DieMaterial> list in _dieByIdIndex.Values)
                    indexTotal += list != null ? list.Count : 0;

                int stateTotal = 0;
                List<DieMaterial> dies = State != null ? State.Dies : null;
                if (dies != null)
                {
                    for (int i = 0; i < dies.Count; i++)
                    {
                        DieMaterial die = dies[i];
                        if (die != null && !string.IsNullOrWhiteSpace(die.DieId))
                            stateTotal++;
                    }
                }

                MaterialPerfProbe.SetGauge("DieIndexEntries", indexTotal);
                if (indexTotal != stateTotal)
                {
                    Log.Write(LogLevel.Normal, "Main", "MaterialStateService",
                        "Die index/state count mismatch. index=" + indexTotal +
                        ", state=" + stateTotal + ". Index rebuilt. - Check");
                    RebuildDieByIdIndexNoLock();
                    // 불일치는 훅 누락(변이 미감지)을 의미하므로 pick 컨텍스트도 신뢰하지 않는다.
                    InvalidateInputPickContextCacheNoLock();
                }
            }
            catch
            {
                // 안전망 실패는 운전에 영향을 주지 않는다.
            }
        }

        private static string EnsureWaferInstanceIdNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return "";

            // 기존 nonblank 값은 형식과 관계없이 참조 중인 영속 identity이므로 그대로 보존한다.
            string instanceId = wafer.WaferInstanceId ?? "";
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instanceId = CreateWaferInstanceId();
                wafer.WaferInstanceId = instanceId;
                wafer.UpdatedAt = DateTime.Now;
            }

            return instanceId;
        }

        public static string EnsureWaferInstanceId(WaferMaterial wafer)
        {
            lock (_stateSync)
            {
                return EnsureWaferInstanceIdNoLock(wafer);
            }
        }

        public static bool IsSameWaferInstance(WaferMaterial left, WaferMaterial right)
        {
            if (left == null || right == null)
                return false;

            string leftInstance = EnsureWaferInstanceId(left);
            string rightInstance = EnsureWaferInstanceId(right);
            return !string.IsNullOrWhiteSpace(leftInstance) &&
                   string.Equals(leftInstance, rightInstance, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildPhysicalDieId(WaferMaterial wafer, int originalMapX, int originalMapY)
        {
            string instanceId = EnsureWaferInstanceId(wafer);
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new InvalidOperationException("물리 Die ID를 만들 Wafer instance가 없습니다.");

            return "D" + instanceId +
                   "X" + originalMapX.ToString("D6", CultureInfo.InvariantCulture) +
                   "Y" + originalMapY.ToString("D6", CultureInfo.InvariantCulture);
        }

        public static bool TryAssignPhysicalDieIds(DieMap map, WaferMaterial wafer, out string reason)
        {
            reason = "";
            if (map == null || map.Entries == null)
            {
                reason = "Input Die Map이 없습니다.";
                return false;
            }
            if (wafer == null)
            {
                reason = "Input Wafer Material이 없습니다.";
                return false;
            }

            var addresses = new HashSet<string>(StringComparer.Ordinal);
            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                string address = originalX.ToString(CultureInfo.InvariantCulture) + "," +
                                 originalY.ToString(CultureInfo.InvariantCulture);
                if (!addresses.Add(address))
                {
                    reason = "Input Die Map의 원본 좌표가 중복되었습니다. original=(" +
                             originalX + "," + originalY + ")";
                    return false;
                }
            }

            foreach (DieMapEntry entry in map.Entries)
            {
                if (entry == null)
                    continue;

                int originalX = DieMapGenerator.ResolveOriginalMapIndexX(entry);
                int originalY = DieMapGenerator.ResolveOriginalMapIndexY(entry);
                entry.DieUid = BuildPhysicalDieId(wafer, originalX, originalY);
                entry.OriginalMapX = originalX;
                entry.OriginalMapY = originalY;
            }

            return true;
        }

        private static string BuildOutputPlacementUid(WaferMaterial outputWafer, int orderIndex)
        {
            string instanceId = EnsureWaferInstanceIdNoLock(outputWafer);
            return string.IsNullOrWhiteSpace(instanceId)
                ? ""
                : "P" + instanceId + "O" + orderIndex.ToString("D6", CultureInfo.InvariantCulture);
        }

        public static string GetProductionLotId()
        {
            lock (_stateSync)
            {
                return State != null && !string.IsNullOrWhiteSpace(State.LotId)
                    ? State.LotId.Trim()
                    : "";
            }
        }

        /// <summary>
        /// 생산 LOT ID를 설정한다.
        /// [신규 2026-07-27] 기존에는 읽기(GetProductionLotId)만 있고 설정 경로가 없어,
        /// LOT ID가 최초 기동 시 레시피(RecipeProject.LotId)에서 한 번 복사되는 것 외에는 바꿀 수 없었다.
        /// 운전 중 LOT 시작/완료를 지원하기 위해 설정 API를 연다.
        /// 이 값 하나만 바꾸면 TactTime CSV / 웨이퍼·검사 CSV / 비전 요청 전문 / 생산통계 / 화면 표시가
        /// 모두 GetProductionLotId()를 통해 자동으로 따라온다(시퀀스 수정 불필요).
        ///
        /// 공백/null 정책: 빈 값은 "LOT 미지정"을 뜻하는 빈 문자열로 정규화해 저장한다.
        /// (LOT 완료 후 미지정 상태로 되돌릴 때 사용한다. 시작 시의 빈 값 거부는 호출자인
        ///  LotSessionService.TryStartLot에서 처리한다.)
        /// </summary>
        /// <returns>값이 실제로 바뀌었으면 true.</returns>
        public static bool SetProductionLotId(string lotId, string reason)
        {
            string normalized = string.IsNullOrWhiteSpace(lotId) ? "" : lotId.Trim();
            string previous;

            lock (_stateSync)
            {
                if (State == null)
                    return false;

                previous = State.LotId ?? "";
                if (string.Equals(previous, normalized, StringComparison.Ordinal))
                    return false;

                State.LotId = normalized;
            }

            NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "SetProductionLotId" : reason);
            Log.Write("Main", "SYSTEM", "SetProductionLotId",
                "생산 LOT ID를 변경했습니다. 이전=" + (string.IsNullOrEmpty(previous) ? "(없음)" : previous) +
                ", 이후=" + (string.IsNullOrEmpty(normalized) ? "(없음)" : normalized) +
                ", reason=" + (reason ?? "") + " - Ok");
            return true;
        }

        public static void InitializeForRecipe(int inputLevelCount, int goodLevelCount, int inputSlots, int outputSlots)
        {
            MaterialStorage.InitializeDefaultState(inputLevelCount, goodLevelCount, inputSlots, outputSlots);
            NotifyAndSave("InitializeForRecipe");
        }

        public static void UpdateRecipeContext(string recipeName, string reason)
        {
            string normalizedRecipeName = string.IsNullOrWhiteSpace(recipeName) ? "" : recipeName.Trim();
            bool changed;
            lock (_stateSync)
            {
                changed = !string.Equals(State.RecipeName ?? "", normalizedRecipeName, StringComparison.OrdinalIgnoreCase);
                State.RecipeName = normalizedRecipeName;
            }

            if (changed)
            {
                NotifyAndSave(string.IsNullOrWhiteSpace(reason) ? "UpdateRecipeContext" : reason);
                Log.Write("Main", "SYSTEM", "MaterialRecipeContext",
                    "Material Recipe 문맥을 갱신했습니다. recipe=" + normalizedRecipeName +
                    ", reason=" + (reason ?? "") + " - Ok");
            }
        }

    }
}
