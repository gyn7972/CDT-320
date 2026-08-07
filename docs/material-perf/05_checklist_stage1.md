# Material 성능 개선 — 1단계 체크리스트 (캐시/인덱스 도입)

작성: 2026-08-05 | 선행: 0단계 체크리스트 전 항목 ✅ 필요
목표: "돌릴수록 느려짐"의 주경로 3개(20Hz 게이트 M×N, die O(N) 조회, 락 안 레시피 I/O) 제거.
검증 안전망: 완료/픽업 판정의 최종 게이트는 항상 **live die 검사**(`CanUseInputPickCandidate`의
die-level 검사 10702/10710/10719행)이므로, 캐시 stale은 "조기 스킵 손실"로만 나타나고
오동작으로 이어지지 않는 구조를 유지한다.

## R-1A. CloneObject 방어 (MaterialSnapshotStore.cs:904~930)

- [ ] A1. `Array` 분기 추가 — rank/길이 보존(`Array.CreateInstance`), 요소 재귀 클론
- [ ] A2. `IDictionary` 분기 추가 — 키/값 재귀 클론으로 **정확 복제**
      (현재: 예외 없이 빈 사전 반환 → 무성 데이터 소실 경로)
- [ ] A3. 참조 동일성/순환 가드 — `Dictionary<object,object>`(참조 비교) visited 맵.
      같은 인스턴스가 그래프에 2회 나타나면 클론도 같은 인스턴스 1개를 공유,
      순환 참조에서 StackOverflow 불가
- [ ] A4. 기존 경로(primitive/enum/string/decimal/DateTime/IList) 동작 불변
- [ ] A5. 실패 시 기존 관례대로 상위 catch에서 로그 (신규 무성 실패 경로 없음)

## R-1B. RecipeStore 중복 파싱 제거 + 핫패스 레시피 캐시

- [ ] B1. `RecipeStore.GetLastProjectName`(299행): 존재 확인용 `Load(name)` 완전 역직렬화를
      **`File.Exists(Dir\name.Project)`** 확인으로 교체 (반환 의미 불변)
- [ ] B2. MSS 내부 `ResolveRecipeProjectCachedNoLock()` 신설 — `.last_project` 마커와
      프로젝트 파일의 `LastWriteTimeUtc` 기반 캐시. 파일 변경/이름 변경 시 자동 재로드
- [ ] B3. 교체 지점은 **핫패스 한정**: 게이트 4곳 + `ResolveOutputReceiveOrderCached`(5643) +
      `BuildDieMapFromWafer`(9852). 레시피 편집/UI 경로는 기존 `LoadLastOrDefault` 유지
- [ ] B4. 캐시 인스턴스는 읽기 전용 사용만 — 교체 지점의 소비 함수
      (`ResolveInputPickup`/`ResolveOutputPickup`/`ApplySequenceNumbers`)가 project를
      변형하지 않음을 확인하고 주석으로 명시

## R-1C. `_dieById` 파생 인덱스 (DieId → List<DieMaterial>)

- [ ] C1. `MaterialStateService`의 **private static** 필드 (스냅샷 그래프에 절대 노출 금지 —
      CloneObject/직렬화가 따라가면 안 됨). `OrdinalIgnoreCase` 키
- [ ] C2. **자기 치유**: 인덱스가 기억하는 `State` 참조와 현재 `State`가 다르면(스냅샷 로드/교체)
      접근 시점에 전체 재구축 — MaterialStorage 수정 없이 로드 경로 커버
- [ ] C3. 동기화 지점: `State.Dies.Add`(GetOrCreateDieMaterial 등 전체 Add 지점) 후 인덱스 추가,
      `RemoveAll` 4곳(310/348/2985/12777) 후 재구축 호출
- [ ] C4. 값이 `List<DieMaterial>` — `GetOrCreateDieMaterial`(271)의 중복 DieId
      `InvalidOperationException` 의미/시점/메시지 보존
- [ ] C5. 교체 지점(핫패스 한정): `GetDieMaterial`(448), `GetOrCreateDieMaterial`(271),
      게이트 루프 3곳(8688/8765 부근), Reserve 루프(7777), `IsInputStagePickComplete` 루프(9142),
      `TryBuildExistingReservedInputStagePickTarget`(7853 부근). 콜드 지점은 미변경(문서화)
- [ ] C6. 일관성 안전망: 저장 캡처 시 인덱스 총계 vs `State.Dies.Count` 비교 —
      불일치면 경고 로그 + 자동 재구축 (운전 지속)
- [ ] C7. `FirstOrDefault` 순서 의미 보존 — 인덱스 리스트는 `State.Dies` 삽입 순서 유지

## R-1D. 입력측 Pick Order/DieMap 캐시 (출력측 `_outputReceiveOrderCache` 동형)

- [ ] D1. 캐시 값: `(DieMap map, List<DieMapEntry> ordered)` — 게이트/Reserve/Candidates/
      `IsInputStageFinishCompleteNoLock`(9048)의 중복 `BuildDieMapFromWafer` + 정렬 제거
- [ ] D2. 캐시 키: `WaferInstanceId + Generation + MappingRevision(DieMapFrameObjId 기반) +
      ReviewApproved + DieIds.Count + wafer.UpdatedAt.Ticks`
      — 근거: 맵 편집(ISMTP 2317행)·매핑 적용·승인 변경은 전부 `wafer.UpdatedAt`을 갱신,
      die 단위 픽 진행은 갱신하지 않음 → 픽 진행 중 히트 유지 + 맵 변경 시 자동 무효화
- [ ] D3. 캐시 미스 시에만 기존 전체 경로 실행 (동작 등가), 히트 시 반환 리스트는
      호출자가 변형하지 않음을 확인
- [ ] D4. stale 안전 논거를 코드 주석으로 명시: 완료 die는 entry가 아니라
      live die 검사(10702/10710/10719)가 걸러낸다 — 캐시로 인해 잘못된 die가
      pick 대상이 될 수 없는 이유
- [ ] D5. `ResetInputStageWaferProcessingStateNoLock`에서 캐시 즉시 무효화 (belt & braces)

## R-1E. 보류 항목 (사유 문서화)

- [ ] E1. **픽커 8칸 슬롯 테이블 보류**: `GetDieAtPicker`의 필터 키(`die.CurrentLocation`)는
      쓰기 지점이 코드 전반에 분산되어 있어 하룻밤 안전 구현 범위를 초과.
      또한 20Hz 폴링 경로가 아닌 사이클당 호출(정합성 리스크 대비 이득 낮음).
      → 2단계 이후 별도 항목으로 이월. 이 결정이 본 문서에 기록되어 있다

## 공통 검증

- [ ] F1. 항목별(1A→1B→1C→1D) 순차 구현, 각 항목 후 빌드 통과 (에러 0)
- [ ] F2. 적대적 코드 리뷰(정합성/스레드/캐시무효화/회귀 렌즈)에서 CONFIRMED 결함 0건
- [ ] F3. 계측 유지 — 0단계 `MaterialPerfProbe`가 1단계 효과를 같은 이름으로 측정
      (전후 비교 가능해야 함)
- [ ] F4. 수정 파일 범위: `MaterialStateService.cs`, `MaterialSnapshotStore.cs`,
      `RecipeStore.cs` 3개 한정 (Sequencing/Interlock/UI diff 0)
- [ ] F5. 미커밋 워킹 트리 보존

## 검증 결과 기록

검증일: 2026-08-05 심야 | 1차 리뷰: 4렌즈 12에이전트 → CONFIRMED 8건(근원 4개) →
전부 수정 → 2차 폐쇄 검증: **allClosed=true**, 잔여 minor 2건도 수정 반영 → 최종 빌드 EXIT=0

- ✅ A1~A5 (CloneObject 방어): Array(rank1)/IDictionary/순환·공유참조 가드, 기존
      ReferenceEqualityComparer 재사용, 기존 경로 불변
- ✅ B1~B4 (RecipeStore): File.Exists 교체 + **손상 파일 fail-closed 의미 복원**
      (mtime당 1회 검증 파싱 캐시 — 2차 검증의 minor 지적 반영), LoadLastOrDefaultCached
      mtime 캐시, 핫패스 교체 (LoadRecipeBinMap은 project 변형 가능성 지적으로 신선 로드 유지),
      소비 함수 읽기 전용 확인(검증 에이전트가 전수 확인)
- ✅ C1~C7 (_dieById): private static + 자기치유 + Add/RemoveAll/컴팩션 훅 + 일관성 안전망
      (불일치 시 pick 컨텍스트 동반 무효화) + FirstOrDefault 순서 의미 보존 +
      GetOrCreateDieMaterial 락 추가(기존 무락 변이는 열거 경합 결함이었음 — 리뷰 확인) +
      공백 dieId 가드
- ✅ D1~D5 (InputPickContext 캐시): 게이트 6곳 + FinishComplete 전환.
      **1차 리뷰가 잡은 무효화 누락 4경로 전부 봉합**:
      ① 비전 오프셋 전파(TryApplyLastVisionOffsetToPendingInputDies) ② 수동 die 편집
      (ApplyManualDieStateNoLock 말미 — 전 경로 커버) ③ 수동 재픽업 복구 ④ 맵 편집 페이지
      (public InvalidateInputPickContextCache + finally 배치)
- ✅ E1 픽커 슬롯 테이블 보류 사유 기록
- ✅ F1 항목별 순차 구현 + 빌드 게이트 (총 6회 빌드 전부 EXIT=0)
- ✅ F2 CONFIRMED 0건 (2차 폐쇄 검증 기준. 정량 확인: 재구축 빈도 기존 ~80회/초 →
      최악 ~1회/초, ≥98.7% 감소 유지)
- ⚠️ F4 **예외 승인 기록**: CONFIRMED 결함 4번(맵 편집 페이지 무락 변이 창) 봉합을 위해
      `InputStageMapTransferPage.cs`에 무효화 호출 2곳 추가 — UI diff 0 원칙의 유일한 예외,
      사유는 결함 폐쇄. Sequencing/Interlock은 diff 0 유지
- ✅ F5 미커밋 워킹 트리 유지

### 데드락/경합 검증 (2차 검증 에이전트 확인 사항)
- MSS에 Invoke/SynchronizationContext 사용 전무, StateChanged는 Task.Run 비동기
- 락 순서 `_stateSync → {_saveRequestSync, _stateChangedSync, _lastCacheSync, probe Sync}` 일관 (역전 없음)
- UI 스레드의 public 무효화는 최악 저장 캡처 딥클론 동안의 유계 대기뿐

**1단계 판정: 통과** → 2단계 진행.
