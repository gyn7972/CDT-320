# CDT-320 코드 분석 — 언로더 배출 픽업 캡(빈손 배출) 검증 + FIFO 병행 가능성

- 작성일: 2026-08-13
- 요구사항: 언로더(Output) 배출 시 픽커가 다이를 들고 있으면 안 됨 → 배출 잔여 개수까지만 픽업.
  기준 확정(팀장님): **Good side pending 기준 + 선착순 배분**.
- 이 문서: 팀장님이 주신 기존 분석(타 세션)의 코드 검증 + 추가 발견 + FIFO 건과의 병행 가능성.
- 관련 문서: [../picker-fifo/04_design.md](../picker-fifo/04_design.md) (FIFO 재시작 드레인)
- 범위: 분석만. 코드 수정 없음.

---

## 1. 기존 분석 검증 결과 — 인용 지점 전수 일치

| 주장 | 검증 |
|---|---|
| 배출 트리거: ActionPlanner:35~39, 완료 신호 기반 | ✅ 일치. NG 먼저(:35), Good 다음(:38) 순서까지 확인 |
| 완료 판정 원천: OutputReceive.cs:1046 `IsOutputStageReceiveComplete` = pending target 0 | ✅ 일치. pending 정의는 :2147~2153 — `IsTarget && Result==Unknown && DieUid==""` |
| 배출 게이트(Safety.cs:75~174)는 픽커 Avoid 위치·정지만 확인, 다이 보유 안 봄 | ✅ 일치 (`AreOutputPickersAvoidAndStopped`) |
| 보유 대기 경로: OutputStageReady.cs:140~ 스테이지 Full 시 다이 든 채 전체 Avoid + 존 반납 + 교체준비 publish + 대기 | ✅ 일치 (:140~184, 리소스 핸드오프까지 확인) |
| 픽업 개수 결정: InputDieVisionPrepareSequence.cs:275 `BuildPickBatch`, Output 잔여와 무연동 | ✅ 일치. 예약 호출 :310, 부분 배치 :312~317 continue |
| 예약 함수가 lock(_stateSync) 안에서 동작 → 락 안 캡 판정으로 선착순 원자성 확보 가능 | ✅ 일치. InputPick.cs:20 진입, :25 lock |
| 슬롯 소비는 Place 완료 시점만 (`MoveDieToOutputStage` → UpdateOutputReceiveSlot) | ✅ 일치. OutputReceive.cs:653에서 DieUid 기록 → pending 해제 |
| 시작 게이트: Gate:1021 `WaitLoaderInactiveBeforePickerStartAsync` (교체 중/완료 시 신규 사이클 차단, NG 미사용 예외 정비) | ✅ 일치. :1062 `IsPickerProcessStartBlocked` + :1067~1106 exchange pending 판정, NG스킵(:1090~1099)·드레인 예외(:1079~1087) 확인 |
| 캡=0이면 배치 0개 → 준비 대상 없음 → Complete 경로 | ✅ 성립. 첫 전진 게이트에도 no-work 보고 경로 존재(PickerProcessSequence.cs:341~349) |

## 2. 추가 발견 (기존 분석에 없던 구현 주의점)

### 2-1. 기존 예약 재발급 경로 — 캡 적용 제외 필수
`ReserveNextInputStagePickTarget`은 신규 예약 전에 **이미 예약된 대상 재요청이면 그대로
재반환**한다(`TryBuildExistingReservedInputStagePickTarget`, InputPick.cs:56~62). 캡 판정을
함수 첫머리에 넣으면 재개/재시도 흐름의 기존 예약 재발급까지 막혀 복구가 깨진다.
→ **캡은 신규 예약 루프(:65 이후)에만 적용**해야 한다.

### 2-2. 선행검사 모드의 겹침 예약 — 차감 항 실재 확인
InputDieVisionPrepareSequence.cs:291~299 — 선행검사 모드에서는 픽커가 다이를 든 채로도
다음 배치 예약을 허용한다(오버랩 프리페치). 즉 "예약됐지만 미픽업" 수량이 실제로 존재하며,
캡 공식의 세 번째 차감 항이 필수임을 코드로 확인했다.

### 2-3. 미결 5-2 종결 — UseNgCassette=false는 캡에 영향 없음
`RouteByInspectionResult + UseNgCassette=false` 모순 조합은 Place CheckUnit에서 **알람으로
차단**된다(PickerPlaceSequence.cs:625~641). 따라서 NG 미사용이면 사실상 ForceGoodStage만
유효 → NG 다이 포함 전량이 Good 스테이지로 Place되어 Good pending을 소비 → **Good 기준
캡이 자동으로 정확**하다. 별도 처리 불필요.

### 2-4. Stop-After-Drain 모드와의 정합
웨이퍼 완료 드레인 중 보유 다이는 검사 결과와 무관하게 GOOD 스테이지로 배출한다
(ActionPlanner.cs:62~76 주석, Gate:1079~1087). Good 기준 캡과 방향이 일치한다.

## 3. ★핵심 리스크 — 캡이 "교차 인풋 웨이퍼 보유"를 없애주지 않는다

팀장님 전제("서로 다른 웨이퍼 다이 동시 보유는 캡을 넣으면 아예 없어진다")가 코드상
**보장되지 않는다.** 근거:

- 인풋 웨이퍼 교체 준비 신호(`InputStageDieComplete`)는
  `TryPublishInputStageExchangeReadyWithoutPickTarget`(FrontPickerSequence.cs:496~521,
  RearPickerSequence.cs:503~ 동일)이 발행하며, 조건은
  **① 픽 대상 없음 ② 자기 픽커 Avoid 위치** 뿐이다.
  **다이 보유 여부와 상대 픽커 상태는 보지 않는다.**
- 발생 시나리오(캡 적용 후에도 성립):
  1. Front가 웨이퍼 A의 마지막 배치를 픽업(캡 통과, Good pending 충분).
  2. Front가 검사/Place 진행 중(빈손 아님, 유휴 루프 아님).
  3. Rear는 빈손 + 픽 대상 없음 + Avoid → **교체 준비 신호 발행**.
  4. 웨이퍼 B 로드/매핑 완료 → Rear가 B 픽업(캡은 Good pending만 보므로 통과).
  5. → **Front(웨이퍼 A) + Rear(웨이퍼 B) 교차 보유 성립.**
- 즉 캡(B)은 "**배출 시점** 빈손"을 보장하는 기능이고, 교차 보유는 **인풋 교체 타이밍**이
  지배한다. 서로 다른 문제다.

**따라서 FIFO 설계의 2차 키(PickedAt)는 캡 도입 후에도 필요하다** (재시작이 교차 보유
상태에서 걸리면 1차 키(투입 순번)가 판별 불가 → 2차 키가 정답을 냄. 먼저 픽업된 쪽 =
먼저 투입된 웨이퍼 쪽이므로 투입 순서 원칙과도 일치).

교차 보유 자체를 근절하려면 인풋 교체 신호 발행 조건에 "양쪽 픽커 빈손"을 추가해야
하는데, 이는 마지막 배치 Place 완료까지 교체를 늦춰 **파이프라인 직렬화(UPH 저하)**를
유발한다 → 팀장님 결정 사항(§5-2).

## 4. FIFO(재시작 드레인) 건과의 병행 수정 가능성 — 결론: 가능, 충돌 없음

### 4-1. 파일 겹침: 0건

| 건 | 수정 파일 |
|---|---|
| A. FIFO 타이브레이크 | `PickerFirstForwardSequencer.cs`, `AutoSequenceCoordinator.cs` |
| B. 픽업 캡 | `MaterialStateService.InputPick.cs`(캡), `MaterialStateService.OutputReceive.cs`(pending 카운트 API), `InputDieVisionPrepareSequence.cs`(계측 로그), (선택) `OutputSequence.Safety.cs`(방어), `PickerPlaceSequence.OutputStageReady.cs`(경고 로그) |

### 4-2. 의미 결합 분석

- A는 "**이미 보유한** 다이의 재시작 진행 순서", B는 "**새로 집는** 개수 제한" — 작동
  시점이 달라 상호 입력이 없다. B의 캡 공식이 보유 다이 수를 읽지만 A는 보유 수를
  바꾸지 않는다.
- 접점 ①: A의 rank1 판정(`HasReadyInputStagePickTarget`)은 캡을 모른다 → Good pending=0
  이어도 rank1 드레인이 요구될 수 있으나, 진입 후 0개 배치 → no-work 완료 경로
  (PickerProcessSequence.cs:341~349)로 자연 종료 → **무해**.
- 접점 ②: B의 방어 게이트(배출 전 빈손 확인)를 '차단'으로 구현하면, 수동 슬롯 완료 등
  예외 상황에서 보유>pending이 된 채 보유 대기 경로(OutputStageReady:140)와 **교착
  가능** → '경고'로 구현 권장(§5-3). A와는 무관하게 B 내부 문제.
- 접점 ③: B가 배치를 0으로 줄인 상태의 재시작 → A의 드레인은 보유 다이 기준으로만
  구성되므로 영향 없음.

### 4-3. 권장 진행 순서

1. **A(FIFO) 먼저** — 설계 승인 완료, 2개 파일 소규모, 이미 프롬프트/체크리스트 완비.
2. **B(캡) 다음** — §5 결정 확정 후 별도 프롬프트로. 같은 날 병행 구현도 기술적으론
   충돌 없으나, 실장비 검증 시 원인 분리를 위해 **커밋/시험은 분리** 권장.

## 4-4. ★추가 발견(2026-08-13 2차): 캡=0 구간의 빈 픽커 무한 재진입(busy loop)

`HasPickerWork`(FrontPickerSequence.cs:456~494, Rear 동일)는 빈 픽커의 사이클 진입을
`HasActionableInputStagePickTarget`(처리 가능 target 존재)으로만 판정한다. 캡 도입 후
**"픽 대상은 남아 있는데 allowance=0"인 구간**(마지막 배치가 Place되는 수십 초)에는:

진입(HasPickerWork=true) → 리소스 점유/phase 진입 → BuildPickBatch 0개 → 완료 →
20ms 뒤 재진입 → 반복

이 패턴은 이 코드베이스에서 **실제로 겪었던 버그와 동일**하다 — FrontPickerSequence.cs:482~484
주석: "전역 판정은 상대 픽커 예약 die에도 true를 반환해 빈 PickerProcess 무한 재진입(busy
loop)을 유발했다" (그래서 side별 actionable 판정으로 고친 이력). 캡은 같은 모양의 구멍을
새로 만든다.

**대책**: `HasPickerWork`의 마지막 판정에 조건 추가 —
`actionable && (allowance > 0 || 자기 side 예약 잔존)`.
- allowance>0만 걸면 **교착 함정**: 마지막 슬롯들이 전부 자기 side 예약분이면
  (pending=2, reserved=2 → allowance=0) 예약 다이를 픽업하러 진입조차 못 해 스테이지가
  영원히 완료되지 않는다. 반드시 `HasInputStagePickReservationForPickerLocation`(기존 API)
  OR 조건과 세트로 넣어야 한다.
- 이 대책은 변경 범위에 FrontPickerSequence.cs / RearPickerSequence.cs 2개 파일을
  추가한다(각 1개 판정식). → 팀장님 승인 필요(§5-4).

## 5. 팀장님 결정 필요 (B 진행 전)

1. **(기존 5-1) 빈손 보장 범위**: NG 스테이지 Full로 NG 배출될 때는 픽커가 Good행 다이를
   들고 있을 수 있음. "모든 배출 빈손"으로 엄격 적용하면 교착 위험 → **Good 배출만 적용
   권장**, NG 배출은 현행(Avoid 게이트) 유지.
2. **(신규) 교차 인풋 웨이퍼 보유 근절 여부**: §3 참조. 근절하려면 인풋 교체 게이트에
   "양쪽 픽커 빈손" 추가(UPH 트레이드오프). 아니면 현행 + FIFO 2차 키로 재시작 순서만
   보정(현 설계, 권장).
3. **(신규) 방어 게이트 성격**: Good 배출 직전 "픽커 보유 다이 0" 확인을 차단으로 할지
   경고(위반 로그만)로 할지. **경고 권장** — 정상 경로에선 도달 불가하고, 예외 상황
   (수동 슬롯 완료 등)에서 차단은 교착을 만든다. 보유 대기 경로(OutputStageReady:140)는
   기존 분석대로 안전망으로 유지 + "캡 있는데 도달" 경고 로그.
