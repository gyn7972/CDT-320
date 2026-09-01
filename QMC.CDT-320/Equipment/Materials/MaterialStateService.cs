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
        /// <summary>
        /// Material 상태 변경 알림(시그널 전용). [계약 보강 2026-08-07]
        /// 라이브 State 객체를 전달하지 않는다 — 시퀀스가 _stateSync 안에서 변이 중인 그래프를
        /// 구독자가 락 없이 순회하는 사고를 계약 수준에서 차단하기 위해서다.
        /// 구독자 규약: 콜백은 ThreadPool 스레드에서 호출될 수 있으므로 dirty flag만 세우고,
        /// 실제 데이터는 이후 ReadState(...) 또는 락을 잡는 public API로 읽는다.
        /// </summary>
        public static event Action StateChanged;
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
        // [내구성 워터마크 2026-08-18] 저장 완료 판정을 "저장 시도 일련번호(SnapshotRevision)"가 아니라
        // "자료 변경 세대"로 한다. Revision은 저장을 시작할 때 발급되므로 같은 자료를 두 번 저장해도
        // 두 개가 생기고, 옆 스레드가 하나 더 발급했다는 이유만으로 내 성공이 실패로 뒤집혔다
        // (2026-08-17 19:00:31 정상 Cycle Stop 오탐 알람의 직접 원인).
        // 더 새 스냅샷이 디스크에 있다는 것은 내 자료도 그 안에 있다는 뜻(상위집합)이므로 성공의 근거다.
        //   _stateVersion       : 자료가 바뀔 때마다 증가하는 세대 (_stateSync 보호)
        //   _durableStateVersion: 디스크에 확정된 최대 세대, 단조 증가 (_saveIoSync 보호)
        // 둘 다 0에서 시작한다 — 기동 직후 로드된 상태는 이미 디스크에 있으므로 내구성 있음(0>=0).
        private static long _stateVersion;
        private static long _durableStateVersion;

        public static MaterialSnapshot State => MaterialStorage.State;

        /// <summary>
        /// [계약 보강 2026-08-07] _stateSync 락 안에서 reader를 실행해 변이 중이 아닌 일관된 State를 읽는다.
        /// UI 등 락 밖 코드가 State 그래프를 직접 순회하는 대신 사용하는 공식 읽기 통로.
        /// 전역 락을 보유하므로 reader는 필요한 값만 복사해 즉시 반환하고,
        /// 무거운 가공/그리기는 반환된 사본으로 락 밖에서 수행한다.
        /// 락 보유 시간은 MaterialPerfProbe "StateReadLock" 샘플로 계측된다.
        /// </summary>
        public static T ReadState<T>(Func<MaterialSnapshot, T> reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            long probeToken = MaterialPerfProbe.BeginSample();
            try
            {
                lock (_stateSync)
                {
                    return reader(State);
                }
            }
            finally
            {
                MaterialPerfProbe.EndSample("StateReadLock", probeToken);
            }
        }

        /// <summary>반환값이 필요 없는 읽기용 ReadState 오버로드. 계약은 위와 동일하다.</summary>
        public static void ReadState(Action<MaterialSnapshot> reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            ReadState(state =>
            {
                reader(state);
                return 0;
            });
        }

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

        /// <summary>
        /// Input 고객 결과 파일명의 기준 시각을 물리 Wafer 단위로 한 번만 확정한다.
        /// 레거시 상태에는 필드가 없으므로 같은 Wafer instance의 기존 Die 이력 중
        /// 가장 이른 Pick 시작 시각을 복구해 재시작 전 파일명을 그대로 사용한다.
        /// </summary>
        internal static DateTime ResolveInputResultFileSessionStartedAt(
            DieMaterial die,
            DateTime candidate)
        {
            if (die == null)
                throw new ArgumentNullException("die");

            bool stateChanged = false;
            bool sessionCreated = false;
            DateTime resolved;
            string waferId = "";
            string waferInstanceId = "";
            lock (_stateSync)
            {
                DieMaterial stateDie = ResolveResultFileSessionDieNoLock(die);
                if (IsValidResultFileSessionTime(stateDie.InputResultFileSessionStartedAt))
                {
                    resolved = stateDie.InputResultFileSessionStartedAt.Value;
                }
                else
                {
                    WaferMaterial wafer = ResolveInputResultFileWaferNoLock(stateDie, ref stateChanged);
                    waferId = wafer.WaferId ?? "";
                    waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);

                    if (IsValidResultFileSessionTime(wafer.InputResultFileSessionStartedAt))
                    {
                        resolved = wafer.InputResultFileSessionStartedAt.Value;
                    }
                    else
                    {
                        resolved = ResolveEarliestInputResultFileSessionNoLock(
                            wafer,
                            NormalizeResultFileSessionTime(candidate));
                        wafer.InputResultFileSessionStartedAt = resolved;
                        wafer.UpdatedAt = DateTime.Now;
                        sessionCreated = true;
                    }

                    stateDie.InputResultFileSessionStartedAt = resolved;
                    stateDie.UpdatedAt = DateTime.Now;
                    stateChanged = true;
                }
            }

            if (stateChanged)
                NotifyAndSave("InputResultFileSessionStart");
            if (sessionCreated)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input 결과 파일 세션 시각을 확정했습니다. wafer=" + waferId +
                    ", instance=" + waferInstanceId +
                    ", startedAt=" + resolved.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                    " - Ok");
            }

            return resolved;
        }

        /// <summary>
        /// Output 고객 결과 파일명의 기준 시각을 물리 Bin 단위로 한 번만 확정한다.
        /// 레거시 상태에는 필드가 없으므로 같은 Bin instance에 배정된 기존 Die의
        /// 가장 이른 Pick 시각을 복구한다.
        /// </summary>
        internal static DateTime ResolveOutputResultFileSessionStartedAt(
            WaferMaterial outputWafer,
            DieMaterial die,
            DateTime candidate)
        {
            if (outputWafer == null)
                throw new ArgumentNullException("outputWafer");
            if (die == null)
                throw new ArgumentNullException("die");

            bool stateChanged = false;
            bool sessionCreated = false;
            DateTime resolved;
            string waferId = "";
            string waferInstanceId = "";
            lock (_stateSync)
            {
                DieMaterial stateDie = ResolveResultFileSessionDieNoLock(die);
                if (IsValidResultFileSessionTime(stateDie.OutputResultFileSessionStartedAt))
                {
                    resolved = stateDie.OutputResultFileSessionStartedAt.Value;
                }
                else
                {
                    WaferMaterial wafer = ResolveOutputResultFileWaferNoLock(outputWafer);
                    waferId = wafer.WaferId ?? "";
                    waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);

                    if (IsValidResultFileSessionTime(wafer.OutputResultFileSessionStartedAt))
                    {
                        resolved = wafer.OutputResultFileSessionStartedAt.Value;
                    }
                    else
                    {
                        resolved = ResolveEarliestOutputResultFileSessionNoLock(
                            wafer,
                            NormalizeResultFileSessionTime(candidate));
                        wafer.OutputResultFileSessionStartedAt = resolved;
                        wafer.UpdatedAt = DateTime.Now;
                        sessionCreated = true;
                    }

                    stateDie.OutputResultFileSessionStartedAt = resolved;
                    stateDie.UpdatedAt = DateTime.Now;
                    stateChanged = true;
                }
            }

            if (stateChanged)
                NotifyAndSave("OutputResultFileSessionStart");
            if (sessionCreated)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Output 결과 파일 세션 시각을 확정했습니다. bin=" + waferId +
                    ", instance=" + waferInstanceId +
                    ", startedAt=" + resolved.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                    " - Ok");
            }

            return resolved;
        }

        private static DieMaterial ResolveResultFileSessionDieNoLock(DieMaterial requestedDie)
        {
            if (State == null || State.Dies == null)
                throw new InvalidOperationException("결과 파일 세션을 확인할 Die Material 상태가 없습니다.");

            string dieId = (requestedDie.DieId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dieId))
                throw new InvalidOperationException("결과 파일 세션을 확인할 Die ID가 없습니다.");

            List<DieMaterial> matches;
            Dictionary<string, List<DieMaterial>> index = GetDieByIdIndexNoLock();
            if (!index.TryGetValue(dieId, out matches) || matches == null || matches.Count != 1)
            {
                throw new InvalidOperationException(
                    "결과 파일 세션의 물리 Die를 고유하게 확인할 수 없습니다. die=" +
                    dieId + ", candidates=" + (matches != null ? matches.Count : 0));
            }
            return matches[0];
        }

        private static WaferMaterial ResolveInputResultFileWaferNoLock(
            DieMaterial die,
            ref bool stateChanged)
        {
            if (State == null || State.Wafers == null)
                throw new InvalidOperationException("Input 결과 파일 세션을 확인할 Material 상태가 없습니다.");

            string instanceId = (die.InputWaferInstanceId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                List<WaferMaterial> instanceMatches = State.Wafers
                    .Where(wafer =>
                        wafer != null &&
                        string.Equals(
                            wafer.WaferInstanceId ?? "",
                            instanceId,
                            StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                if (instanceMatches.Count == 1)
                    return instanceMatches[0];
                if (instanceMatches.Count > 1)
                {
                    throw new InvalidOperationException(
                        "Input 결과 파일 세션의 Wafer instance가 중복되었습니다. instance=" + instanceId);
                }
                throw new InvalidOperationException(
                    "Input 결과 파일 세션의 Wafer instance를 찾을 수 없습니다. wafer=" +
                    (die.WaferID_Input ?? "") + ", instance=" + instanceId);
            }

            string waferId = (die.WaferID_Input ?? "").Trim();
            List<WaferMaterial> displayMatches = State.Wafers
                .Where(wafer =>
                    wafer != null &&
                    string.Equals(
                        wafer.WaferId ?? "",
                        waferId,
                        StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (displayMatches.Count != 1)
            {
                throw new InvalidOperationException(
                    "Input 결과 파일 세션의 물리 Wafer를 고유하게 확인할 수 없습니다. wafer=" +
                    waferId + ", instance=" + instanceId + ", candidates=" + displayMatches.Count);
            }

            WaferMaterial resolved = displayMatches[0];
            string resolvedInstanceId = EnsureWaferInstanceIdNoLock(resolved);
            if (!string.Equals(
                die.InputWaferInstanceId ?? "",
                resolvedInstanceId,
                StringComparison.OrdinalIgnoreCase))
            {
                die.InputWaferInstanceId = resolvedInstanceId;
                die.UpdatedAt = DateTime.Now;
                stateChanged = true;
            }
            return resolved;
        }

        private static WaferMaterial ResolveOutputResultFileWaferNoLock(WaferMaterial requestedWafer)
        {
            if (State == null || State.Wafers == null)
                throw new InvalidOperationException("Output 결과 파일 세션을 확인할 Material 상태가 없습니다.");

            string instanceId = EnsureWaferInstanceIdNoLock(requestedWafer);
            List<WaferMaterial> instanceMatches = State.Wafers
                .Where(wafer =>
                    wafer != null &&
                    string.Equals(
                        wafer.WaferInstanceId ?? "",
                        instanceId,
                        StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (instanceMatches.Count == 1)
                return instanceMatches[0];
            if (instanceMatches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Output 결과 파일 세션의 Bin instance가 중복되었습니다. instance=" + instanceId);
            }
            throw new InvalidOperationException(
                "Output 결과 파일 세션의 Bin instance를 찾을 수 없습니다. bin=" +
                (requestedWafer.WaferId ?? "") + ", instance=" + instanceId);
        }

        private static DateTime ResolveEarliestInputResultFileSessionNoLock(
            WaferMaterial wafer,
            DateTime fallback)
        {
            DateTime earliest = fallback;
            if (State == null || State.Dies == null)
                return earliest;

            string instanceId = EnsureWaferInstanceIdNoLock(wafer);
            bool previousSessionClosed = State.Dies.Any(die =>
                die != null &&
                string.Equals(
                    die.InputWaferInstanceId ?? "",
                    instanceId,
                    StringComparison.OrdinalIgnoreCase) &&
                IsValidResultFileSessionTime(die.InputResultFileSessionStartedAt));
            if (previousSessionClosed)
                return earliest;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null ||
                    !string.Equals(
                        die.InputWaferInstanceId ?? "",
                        instanceId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                DateTime dieStartedAt = ResolveInputResultFileCandidateNoLock(die);
                if (IsValidResultFileSessionTime(dieStartedAt) && dieStartedAt < earliest)
                    earliest = dieStartedAt;
            }
            return earliest;
        }

        private static DateTime ResolveEarliestOutputResultFileSessionNoLock(
            WaferMaterial wafer,
            DateTime fallback)
        {
            DateTime earliest = fallback;
            if (State == null || State.Dies == null)
                return earliest;

            string instanceId = EnsureWaferInstanceIdNoLock(wafer);
            bool previousSessionClosed = State.Dies.Any(die =>
                die != null &&
                string.Equals(
                    die.OutputWaferInstanceId ?? "",
                    instanceId,
                    StringComparison.OrdinalIgnoreCase) &&
                IsValidResultFileSessionTime(die.OutputResultFileSessionStartedAt));
            if (previousSessionClosed)
                return earliest;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null ||
                    !string.Equals(
                        die.OutputWaferInstanceId ?? "",
                        instanceId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !IsValidResultFileSessionTime(die.PickedAt))
                {
                    continue;
                }

                if (die.PickedAt < earliest)
                    earliest = die.PickedAt;
            }
            return earliest;
        }

        private static DateTime ResolveInputResultFileCandidateNoLock(DieMaterial die)
        {
            if (die == null)
                return DateTime.MinValue;

            DieInspectionRecord inputVision = FindResultFileInspectionNoLock(die, "InputPickVision");
            if (inputVision != null && IsValidResultFileSessionTime(inputVision.CreatedAt))
                return inputVision.CreatedAt;

            DieInspectionRecord pickUp = FindResultFileInspectionNoLock(die, "PickUp");
            if (pickUp != null && IsValidResultFileSessionTime(pickUp.CreatedAt))
                return pickUp.CreatedAt;

            return IsValidResultFileSessionTime(die.PickedAt)
                ? die.PickedAt
                : DateTime.MinValue;
        }

        private static DieInspectionRecord FindResultFileInspectionNoLock(
            DieMaterial die,
            string inspectionType)
        {
            if (die == null || die.Inspections == null)
                return null;

            return die.Inspections.FirstOrDefault(record =>
                record != null &&
                string.Equals(
                    record.InspectionType,
                    inspectionType,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 정상 완료 또는 수동 언로딩으로 Wafer/Bin이 Cassette에 복귀할 때 현재 세션을 닫는다.
        /// 이미 처리된 Die에는 세션 시각을 남겨 언로딩 직후 도착하는 후행 Place/중복 결과가
        /// 새 파일로 갈라지지 않고 원래 파일을 갱신하도록 한다.
        /// </summary>
        private static void CloseResultFileSessionForCassetteReturnNoLock(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            MaterialLocation previousLocation)
        {
            if (wafer == null || previousLocation == null)
                return;

            bool inputReturn = !IsOutputCassetteRole(cassetteRole) &&
                (previousLocation.Kind == MaterialLocationKind.InputStage ||
                 previousLocation.Kind == MaterialLocationKind.InputFeeder);
            bool outputReturn = IsOutputCassetteRole(cassetteRole) &&
                (previousLocation.Kind == MaterialLocationKind.OutputStageGood ||
                 previousLocation.Kind == MaterialLocationKind.OutputStageNg ||
                 previousLocation.Kind == MaterialLocationKind.OutputFeeder);
            if (!inputReturn && !outputReturn)
                return;

            string instanceId = EnsureWaferInstanceIdNoLock(wafer);
            DateTime? sessionStartedAt = inputReturn
                ? wafer.InputResultFileSessionStartedAt
                : wafer.OutputResultFileSessionStartedAt;
            if (IsValidResultFileSessionTime(sessionStartedAt) &&
                State != null &&
                State.Dies != null)
            {
                foreach (DieMaterial die in State.Dies)
                {
                    if (die == null)
                        continue;

                    if (inputReturn)
                    {
                        if (!string.Equals(
                                die.InputWaferInstanceId ?? "",
                                instanceId,
                                StringComparison.OrdinalIgnoreCase) ||
                            IsValidResultFileSessionTime(die.InputResultFileSessionStartedAt) ||
                            !IsValidResultFileSessionTime(ResolveInputResultFileCandidateNoLock(die)))
                        {
                            continue;
                        }
                        die.InputResultFileSessionStartedAt = sessionStartedAt.Value;
                    }
                    else
                    {
                        DieInspectionRecord placeRecord =
                            FindResultFileInspectionNoLock(die, "OutputPlaceVision");
                        if (!string.Equals(
                                die.OutputWaferInstanceId ?? "",
                                instanceId,
                                StringComparison.OrdinalIgnoreCase) ||
                            IsValidResultFileSessionTime(die.OutputResultFileSessionStartedAt) ||
                            placeRecord == null)
                        {
                            continue;
                        }
                        die.OutputResultFileSessionStartedAt = sessionStartedAt.Value;
                    }
                    die.UpdatedAt = DateTime.Now;
                }
            }

            if (inputReturn)
                wafer.InputResultFileSessionStartedAt = null;
            else
                wafer.OutputResultFileSessionStartedAt = null;
            wafer.UpdatedAt = DateTime.Now;
        }

        private static bool IsValidResultFileSessionTime(DateTime value)
        {
            return value > new DateTime(2000, 1, 1);
        }

        private static bool IsValidResultFileSessionTime(DateTime? value)
        {
            return value.HasValue && IsValidResultFileSessionTime(value.Value);
        }

        private static DateTime NormalizeResultFileSessionTime(DateTime value)
        {
            return IsValidResultFileSessionTime(value) ? value : DateTime.Now;
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

        /// <summary>
        /// [강제 Recipe 변경 2026-08-09] 장비 안이 비어 있다는 작업자 확인 아래, 남아 있는 Wafer/Die
        /// Material 기록을 모두 지운다. 카세트 구성(역할·레벨·슬롯 수·사용 여부)은 그대로 두고
        /// 슬롯 점유 표시와 매핑만 해제하므로, InitializeForRecipe 처럼 슬롯 수가 기본값으로 되돌아가지 않는다.
        ///
        /// 슬롯 단위 Clear 로는 지워지지 않는 고아 Die(부모 Wafer 가 이미 사라졌는데 위치만 카세트로
        /// 남은 기록)까지 제거하는 것이 목적이다. 실제 2026-08-09 사례에서 카세트 Clear 후에도
        /// InputCassette 위치 Die 18개가 남아 Recipe 변경이 계속 차단되었다.
        ///
        /// 생산 이력은 CSV(OutputWaferCsv/InputWaferInspectionCsv)에 이미 기록되어 있어 이 호출로 유실되지 않는다.
        /// </summary>
        public static bool ClearAllMaterialForRecipeChange(string reason, out string detail)
        {
            detail = string.Empty;

            try
            {
                int removedWafers;
                int removedDies;
                int clearedSlots;

                lock (_stateSync)
                {
                    MaterialSnapshot state = State;
                    if (state == null)
                    {
                        detail = "Material 상태를 확인할 수 없습니다.";
                        return false;
                    }

                    removedWafers = state.Wafers != null ? state.Wafers.Count : 0;
                    removedDies = state.Dies != null ? state.Dies.Count : 0;
                    clearedSlots = 0;

                    if (state.Wafers != null)
                        state.Wafers.Clear();
                    if (state.Dies != null)
                        state.Dies.Clear();

                    // [P4 2026-08-22] BIN 선택은 작업 단위 — 레시피 변경 전체 클리어 시 기본(All)로 복귀.
                    state.PickupBinMode = PickupBinModeAll;
                    state.PickupBinNumbers = new List<int>();

                    if (state.Cassettes != null)
                    {
                        foreach (CassetteMaterial cassette in state.Cassettes)
                        {
                            if (cassette == null)
                                continue;

                            // 카세트 자체(역할/레벨/슬롯 수/사용 여부)는 보존하고, 담고 있던 Wafer 가
                            // 모두 사라졌으므로 매핑 상태만 해제해 재매핑을 요구한다.
                            //
                            // [정정 2026-08-10] IsPresent 도 함께 내려야 한다. IsPresent 는 물리 센서가 아니라
                            // Mapping 이 만든 논리 상태이고, HasInMachineMaterial 은
                            // (IsEnabled && IsPresent && !IsMapped) 를 "Present/Unmapped" 잔재로 보고
                            // Recipe 변경을 차단한다. IsMapped 만 내리면 강제 정리 직후 재검증에서
                            // 다시 차단되어 강제 Recipe 변경이 항상 실패한다.
                            // (기존 ClearInput/OutputCassetteAllSlotData 도 같은 이유로 둘 다 내린다.)
                            cassette.IsMapped = false;
                            cassette.IsPresent = false;
                            // 잔존 LOT ID가 다음 mapping의 LOT 후보 충돌을 만들지 않도록
                            // 다른 전체 Clear 경로들과 동일하게 함께 소거한다.
                            cassette.CassetteLotId = "";

                            if (cassette.Slots == null)
                                continue;

                            foreach (CassetteSlotMaterial slot in cassette.Slots)
                            {
                                if (slot == null || (!slot.HasWafer &&
                                                     string.IsNullOrWhiteSpace(slot.WaferId) &&
                                                     string.IsNullOrWhiteSpace(slot.WaferInstanceId)))
                                {
                                    continue;
                                }

                                slot.HasWafer = false;
                                slot.WaferId = "";
                                slot.WaferInstanceId = "";
                                clearedSlots++;
                            }
                        }
                    }

                    // 파생 인덱스와 PickUp 컨텍스트 캐시는 Die 집합이 통째로 바뀌었으므로 모두 버린다.
                    InvalidateDieByIdIndexNoLock();
                    InvalidateInputPickContextCacheNoLock();
                }

                detail = "wafers=" + removedWafers + ", dies=" + removedDies + ", slots=" + clearedSlots;
                Log.Write(LogLevel.AboveNormal, "Main", "MaterialStateService",
                    "강제 Recipe 변경으로 장비 내부 Material 기록을 모두 정리했습니다. reason=" +
                    (reason ?? "") + ", " + detail + " - Ok");
                NotifyAndSave("ForceRecipeChangeMaterialClear");
                return true;
            }
            catch (Exception ex)
            {
                detail = "Material 정리 중 예외가 발생했습니다. error=" + ex.Message;
                Log.Write(LogLevel.AboveNormal, "Main", "MaterialStateService",
                    "강제 Recipe 변경 Material 정리 실패: " + ex.Message + " - Failed");
                return false;
            }
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
