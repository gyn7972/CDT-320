# Material 성능 개선 — 0단계 체크리스트 (세대 누수 수정 + 기준선 계측)

작성: 2026-08-05 | 근거 조사: 성능 RCA 워크플로(31에이전트) + 구조 검토 워크플로(11에이전트)
규칙: 각 항목은 ✅/❌로 객관 판정 가능해야 하며, 구현 후 본 문서에 검증 결과를 기입한다.

## R-0A. 세대 누수 버그 수정 (ResolveWaferDies 인스턴스 키잉)

배경: `ResetInputStageWaferProcessingStateNoLock`(MSS:12763)이 세대 리셋 시 출력 부모 die를
남기고 웨이퍼에 새 `WaferInstanceId`를 발급하는데(12783행), `ResolveWaferDies`(10027행)는
표시 `WaferID_Input`만으로 필터링해 **구세대 die가 신세대 집합에 혼입**된다.
현재는 `DeduplicateWaferDiesByGrid`+`IsBetterWaferDie` 휴리스틱이 이를 가리고 있다.

- [ ] A1. `ResolveWaferDies`의 source 필터가 표시 WaferId 외에 **`InputWaferInstanceId` ==
      `wafer.WaferInstanceId`(OrdinalIgnoreCase)** 일치를 요구한다
- [ ] A2. **레거시 폴백**: wafer 또는 die의 instance id가 비어 있으면(공백 포함) 기존
      표시-ID 판정으로 폴백한다 — 구 스냅샷 로드 직후에도 die 집합이 비지 않는다
      (참고: `MaterialStorage.cs:332~347`이 로드 시 유일 표시 ID에 한해 backfill함)
- [ ] A3. `DeduplicateWaferDiesByGrid` / `IsBetterWaferDie`는 **변경하지 않았다**
      (방어선 유지 — 제거는 후속 단계에서 별도 판단)
- [ ] A4. 0단계에서 `State.Dies`를 조회/삭제하는 **다른 지점은 변경하지 않았다**
      (범위: `ResolveWaferDies` 1곳 + 신규 helper 1개)
- [ ] A5. 신규 helper는 `State`를 직접 참조하지 않는 순수 술어이며, 락 가정이 없다

## R-0B. 기준선 계측 (MaterialPerfProbe)

목적: RCA 보고서 확인항목 #1(dies 증가 곡선)·#2(게이트 실측)·#3(저장 캡처 락 구간 —
현재 코드 어디에도 계측 없음)의 기준선 확보. 1단계 효과 판정의 비교 기준.

- [ ] B1. 신규 클래스 `MaterialPerfProbe` (`Equipment\Materials\MaterialPerfProbe.cs`):
      이름별 Count / TotalMs / AvgMs / MaxMs 집계, **30초 주기 한 줄 요약 로그**
      (호출당 로그 금지 — 20Hz 폴링 스팸 방지)
- [ ] B2. 계측 API는 예외를 전부 삼킨다 — 계측 실패가 운전에 영향 줄 수 없다
- [ ] B3. `SetGauge`로 순간값(State.Dies/Wafers 개수)이 요약 로그에 포함된다
- [ ] B4. 계측 지점 6곳 삽입 (전부 기존 빈 `finally` 활용 또는 지역 측정 — 로직 변경 0):
      1. `ReserveNextInputStagePickTarget` (MSS:7725)
      2. `HasReadyInputStagePickTarget` (MSS:8658)
      3. `HasActionableInputStagePickTarget` (MSS:8734)
      4. `IsInputStagePickComplete` (MSS:9126)
      5. `BuildDieMapFromWafer` (MSS:9773)
      6. `SaveCurrentSnapshot`의 **`lock(_stateSync)` 캡처 구간**(MSS:11306~11320) —
         락 보유 시간을 별도 이름(`SaveCaptureLock`)으로 기록 + dies/wafers 게이지 갱신
- [ ] B5. 계측 코드가 락 보유 시간을 늘리지 않는다 — Begin/End는 락 밖(메서드 경계),
      6번만 예외적으로 락 구간 자체가 측정 대상
- [ ] B6. `QMC.CDT-320.csproj`에 신규 파일 `<Compile Include>` 등록
- [ ] B7. 요약 로그는 기존 관례를 따른다 (`Log.Write("Main","SYSTEM","MaterialPerfProbe", ... " - Ok")`)

## 공통 검증

- [ ] C1. 솔루션 빌드 통과 (MSBuild, 신규 에러 0)
- [ ] C2. 수정 파일 목록이 정확히 3개다:
      `MaterialStateService.cs`(필터+계측), `MaterialPerfProbe.cs`(신규), `QMC.CDT-320.csproj`(등록)
- [ ] C3. 적대적 코드 리뷰(독립 검증)에서 CONFIRMED 결함 0건
- [ ] C4. git diff가 커밋되지 않은 워킹 트리 상태로 보존된다 (커밋은 사용자 지시 대기)

## 검증 결과 기록

검증일: 2026-08-05 (심야 자율 작업) | 리뷰: 3렌즈 적대적 워크플로(5에이전트) + 반박 검증

- ✅ A1. `ResolveWaferDies` source 필터에 `IsDieOwnedByInputWaferInstance(d, waferInstanceId)` 추가 확인
- ✅ A2. 양측 공백 폴백 구현 + `MaterialStorage` backfill 경로와 정합 확인
- ✅ A3. Dedup/IsBetterWaferDie 무변경 (git diff 확인)
- ✅ A4. 변경 범위 = ResolveWaferDies 필터 1곳 + helper 1개 (diff 확인)
- ✅ A5. helper는 인자만 사용, State/락 무접촉
- ✅ B1. MaterialPerfProbe 구현 (Count/Total/Avg/Max, 30초 요약)
- ✅ B2. 전 API try/catch 무해화
- ✅ B3. StateDies/StateWafers 게이지 포함
- ✅ B4. 계측 6곳 삽입 (게이트 4 + BuildDieMapFromWafer + SaveCaptureLock)
- ✅ B5. 게이트 계측은 메서드 경계(락 밖). SaveCaptureLock은 **리뷰 지적 반영으로 락 내부 Begin**
      → "락 보유 시간"만 측정 (대기 제외), try/finally로 예외 샘플도 포착
- ✅ B6. csproj 등록
- ✅ B7→**변경**: 관례 4-인자 `Log.Write`가 ProductionMinimal 모드에서 조기 반환됨이 리뷰에서
      확인되어(CONFIRMED 2건), **`Log.Write(LogLevel.Normal, ...)` 오버로드**(정책 게이트 미적용,
      항상 영속)로 교체 + 파일 기록은 백그라운드 Task로 이동 (핫 락 보유 중 I/O 방지)
- ✅ C1. 빌드 통과 (수정 반영 후 재빌드, EXIT=0)
- ✅ C2. 수정 파일 3개 (MaterialStateService.cs / MaterialPerfProbe.cs / csproj)
- ✅ C3. CONFIRMED 2건 → 전부 수정 반영. minor 중 수정 반영 2건(Trim 비교, SaveCaptureLock
      의미/예외 포착), 나머지 minor는 아래 "잔여 관찰"로 기록
- ✅ C4. 미커밋 워킹 트리 유지

### 잔여 관찰 (수정하지 않음 — 사유)

1. 구(인스턴스 도입 전) 스냅샷에 **이미 구워진 혼입**은 backfill이 현재 instance를 스탬프하므로
   이 필터로 교정 불가 — 구 스냅샷 복구 직후 1세대 동안 혼입 재현 가능. 데이터 기원 한계이며
   Dedup 방어선이 기존대로 커버. (신세대 생성 시점부터는 근절됨)
2. 락 없는 UI 조회 경로(LiveLotMapView 등)는 세대 회전 순간 한 틱 빈 맵 가능 — 다음 갱신에서
   자가 회복, 제어 경로는 전부 락 내부라 영향 없음. 기존에도 열거 경합은 존재.
3. DEBUG 빌드는 강제 verbose라 중복 기록될 수 있으나 무해.

**0단계 판정: 통과** → 1단계 진행.
