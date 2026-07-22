# Front 픽업 시퀀스 분석 — 축별/비전별 명령·대기 시점 (피커 4→1)

- 분석 대상: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (+ `InputDieVisionPrepareSequence.cs`, `PickerSequenceBase.cs`)
- 기준: **자동 운전(Auto) 표준 경로**, Front 헤드. 특수 모드 분기는 문서 끝에 존재만 표기.
- 피커 순서: `BuildEnabledPickerIndexes()` (PickerSequenceBase.cs 642행)가 `RunOrderMode`(기본
  **Descending**, 759행)에 따라 `{3,2,1,0}` = **피커 4 → 3 → 2 → 1** 순서로 확정.
- 표기: **[명령]** = 축/비전에 명령이 나가는 시점, **[대기]** = 완료·신호·리소스를 기다리는 시점.
  이 코드의 이동 헬퍼(`Move...AndVerifyAsync`)는 "명령 송신 → InPosition 검증까지 await"가 기본이다.
  즉 별도 표기가 없으면 명령과 완료 대기가 한 스텝 안에서 연속으로 일어난다.

---

## 전체 구조 — 2개 국면(Phase)

```
[준비] CheckUnit → CheckPickerSideEnabled → BuildEnabledPickerList(4→1 확정)
     → CheckInputStageReady → MoveAllPickerZToAvoid → BuildPickBatch
[Phase A: 비전 준비 배치]  Die 예약(4→1) → Die마다 {스테이지/비전 이동 → 촬영 요청 → EPD만 대기}
                          → 배치 끝에서 RESULT 일괄 회수
[Phase B: 픽업 실행 배치]  좌표 일괄 계산 → InputVisionX 회피 → 피커마다(4→1)
                          {X/T ∥ NeedleX/StageY → Y 전진 → Z 픽업 모션 → 자재 갱신}
```

핵심 특징: **촬영(EPD)과 결과(RESULT) 회수가 분리된 파이프라인**이다. Die 4개를 연속 촬영하면서
RESULT는 기다리지 않고, 4개 촬영이 끝난 뒤 일괄 회수한다.

---

## 준비 국면 (배치 시작 전 1회)

| 스텝 | 명령 | 대기 | 비고 |
|---|---|---|---|
| CheckUnit / CheckPickerSideEnabled | 없음 | 없음 | 유닛·축 준비 상태 확인만 |
| BuildEnabledPickerList | 없음 | 없음 | RunOrderMode(Descending) → **4,3,2,1** 확정 |
| CheckInputStageReady | 없음 | 없음(즉시 판정) | Bus `InputStageReady` 신호·웨이퍼 Finish·Pick 대상 존재 확인. 미충족 시 Fail |
| MoveAllPickerZToAvoid | **[명령] Front PickerZ1~Z4 전체 → Avoid** | **[대기] InputStageArea 리소스 획득**(200ms 재시도 루프, CycleStop 체크) → **[대기] Z 전체 InPosition 검증** | 리소스 획득이 이동보다 먼저 |
| BuildPickBatch | 없음 | (하위 시퀀스 실행) | InputCamera Mark 선행검사 허가가 있으면 Phase A 전체 생략 → 바로 좌표 계산. 없으면 Phase A 실행 |

---

## Phase A — Input Die 비전 준비 (`InputDieVisionPrepareSequence`)

### A-0. Die 예약 (모션 없음)
`BuildPickBatch()`: 피커 **4→1** 순서로 빈 피커마다 `ReserveNextInputStagePickTarget()` — Die 1개씩
예약 (Die 맵 좌표 `TargetX/TargetY` 확보). 최대 4건의 배치 생성.

### A-1. Die당 반복 (예약 순서 = 4→1)

| 순서 | 스텝 | 명령 | 대기 |
|---|---|---|---|
| 1 | VerifyReservedInputDie | 없음 | 없음 (자재 상태 재검증) |
| 2 | MovePickersToAvoidForInputVisionMove | (필요 시) 피커 회피 이동 | **[대기] Front/Rear 피커가 Input 존을 비울 때까지** |
| 3 | MoveInputStageAndVisionToDie — 안전 준비 | **[명령+대기] EjectPinZ → Avoid** (스테이지 주행 전) → **[명령+대기] StageT → 웨이퍼 얼라인 보정 각도** → **[명령+대기] StageZ → Process 위치** (각각 이미 위치면 생략) | **[대기] Front/Rear 피커 Input 존 클리어**, **[대기] PickUp 허가 스토어 클리어**, (선행검사 모드) **[대기] InputStageArea 획득** |
| 4 | MoveInputStageAndVisionToDie — 촬영 위치 이동 | **[명령+대기] InputVisionX / NeedleX / StageY → Die 촬영 위치** | 그룹별 명령 직후 InPosition 검증 |
| 5 | StartInputDieVisionInspection | **[명령] Wafer 채널 correlated MATCH 검사 요청 송신** (`StartInspectionRequestAsync`) | **[대기] 촬영 전 정착 지연** → **[대기] EPD(노출 완료)만** — RESULT는 기다리지 않음 |
| 6 | (커서 증가) | — | EPD 수신 즉시 다음 Die(다음 피커)로 진행 |

4번의 경로 결정 상세 (`MoveInputStageVisionPointForPickerAsync`, 1713행):
- 현재/목표 위치와 Needle 작업영역 검사로 **L자 경로**를 선택한다:
  ① X·Y 모두 도착: NeedleX만 보정 → ② X만 미도착: VisionX+NeedleX 동시 →
  ③ Y만 미도착: NeedleX/StageY(안전 순서) → ④ 둘 다 미도착: X먼저 가능하면 VisionX →
  NeedleX/StageY, Y먼저 가능하면 그 반대 → ⑤ 직행 불가면 작업영역 중심 경유
  (StageY→VisionX→NeedleX/StageY).
- 각 그룹은 "명령 → InPosition 검증"으로 진행. VisionX 이동은 **SharedRailX 인터락 클리어
  대기**(`WaitInputVisionXSharedRailClearAsync`)를 포함한다.
- NeedleX는 VisionX를 따라 함께 이동해 니들이 항상 Die 하부 부근을 유지한다.

### A-2. RESULT 일괄 회수 (`CollectVisionResultsAsync`, 647행)
- Die 순서대로(4→1) **[대기] 각 검사 핸들의 RESULT** (`WaitInspectionStageAsync` — 현재는
  요청+PENDING 폴링 방식).
- RESULT 실패 Die는 SKIP 처리(예약 해제) 후 다음 Die 계속.
- 회수된 DeltaX/DeltaY/DeltaTheta를 각 배치 아이템의 VisionOffset으로 확정.

### 백그라운드 병행 요소
`PickerProcessSequence`(1083행)가 `InputVisionXPrePositionCoordinator.EnsureStarted`로
**InputVisionX 선행 위치 이동**을 백그라운드로 시작할 수 있다 — 이동 중 목표 변경은
`AjinAxis.TryOverridePosition`(포지션 오버라이드)을 사용한다. 픽업 시퀀스 본체와는 비동기로 동작.

---

## Phase B — 픽업 실행 (좌표 계산 후 피커 4→1 반복)

### B-0. 배치 공통 (1회)

| 스텝 | 명령 | 대기 |
|---|---|---|
| CalculatePickTargets | 없음 | 없음 — Die 4개 전체의 StageY/PickerX/Y/T/Z/NeedleX/NeedleZ/EjectPinZ 목표를 일괄 계산 (`CalculatePickTarget` 수식) |
| MoveInputVisionToAvoidForPickerMove | **[명령+대기] InputVisionX → 회피 좌표** (이미 위치면 생략) | 배치 전체 PickerX 목표를 반영한 **SharedRailX 동적 최소 회피 좌표** 계산 후 이동 |

### B-1. 피커당 반복 (4→1)

| 순서 | 스텝 | 명령 | 대기 |
|---|---|---|---|
| 1 | SelectNextPickTarget | 없음 | 없음 (배치 컨텍스트 로드) |
| 2 | MoveOppositePickerToAvoidForPickerMove | 없음(자축 명령 없음) | **[대기] 상대(Rear) 피커가 Input Pick 영역 밖일 때까지** |
| 3 | MovePickerXStageYPickerT — 사전 안전 | (필요 시) **[명령+대기] Z축들 Avoid 복귀**, **[명령+대기] StageT 얼라인 각도**, **[명령+대기] EjectPinZ Avoid**, **[명령+대기] PickerY Avoid**(전진 전 후퇴 확인) | 각 항목 이미 위치면 생략. Needle 작업영역 검증(계산) |
| 4 | 〃 — 본 이동 (기본 모드) | **[병렬 명령] ①PickerX + PickerT(해당 피커) ∥ ②NeedleX/StageY**(안전 순서: NeedleX→StageY 또는 StageY→NeedleX, `TryResolveNeedleWorkPointMoveOrder`로 판단, 각각 명령+검증) | **[대기] Task.WhenAll 합류** (①② 모두 완료) — 직전에 Input work area 점유 |
| 5 | 〃 — Y 전진 | **[명령+대기] PickerY → Pick 위치** | X/T·NeedleX/StageY 완료 후에만 전진 |
| 6 | VerifyPickTarget | 없음 | 없음 (전 축 최종 위치 재검증) |
| 7 | VerifyPickerEmptyBeforePick | 없음 | **[대기] 해당 피커 Flow 센서 OFF 확인** |
| 8 | MovePickerZPick → `RunPickupZMotionAsync` | 아래 Z 세부 모션 표 | 〃 |
| 9 | UpdateMaterialToPicker | 없음 | 없음 — 자재 갱신, Flow/Data 재확인, **마지막 Pick이면 Bus `InputStageDieComplete` 발행** |
| 10 | SelectNextPickTargetOrComplete | 없음 | 없음 — 다음 피커(4→1)로, 전부 끝나면 Complete + InputStageArea 해제 |

주의: enum의 `VacuumOn`/`VerifyDiePicked`/`MovePickerZToAvoid` 스텝은 자동 표준 경로에서는
**독립 스텝으로 돌지 않고** 8번(`RunPickupZMotionAsync`) 내부에서 수행된다.
(`MovePickerZPickAsync`가 완료 후 곧바로 `UpdateMaterialToPicker`로 전이, 3634행)

### B-2. Z 픽업 세부 모션 (`RunPickupZMotionAsync` 표준 모드, 4221행)

| 순서 | 서브스텝 | 명령 | 대기 |
|---|---|---|---|
| a | PrepareNeedlePinZ | **[병렬 명령] NeedleZ → 픽업 준비 위치(이미 티칭 위치면 유지) ∥ EjectPinZ → 픽업 준비 위치** | **[대기] WhenAll + 양축 InPosition 검증** |
| b | VacuumOnBeforePick | **[명령] Needle Vacuum ON + Picker Vacuum ON** (IO 출력, 즉시) | 없음 |
| c | MovePickerZPrePick | **[명령+대기] PickerZ → Contact 상부 PrePick 위치** (기본 속도 하강) | PrePickDistance=0이면 생략 |
| d | SlowToContact | **[명령+대기] PickerZ → Contact(티칭 Z)** — 저속(설정 %)·전용 가감속 | **[대기] Contact settle 지연** |
| e | SyncLift | **[병렬 명령] PickerZ ∥ EjectPinZ 동기 상승(SyncLift 거리)** | 시작 전 PickerZ/NeedleZ/EjectPinZ 3축 위치 검증 → **[대기] 완료 + SyncLift settle 지연** |
| f | Separate | **[명령+대기] PickerZ 저속 Separate 거리 상승** → **[명령] PickerZ → Avoid(복귀 속도)** | **[대기] Stage-safe 거리 도달** 시점에 **[명령] Needle Vacuum OFF + EjectPinZ → Avoid** 후 **[대기] PickerZ Avoid 완료** |
| g | PickSettle | 없음 | **[대기] PickSettleMs 지연** |
| h | VerifyDiePicked | 없음 | **[대기] Flow 센서 ON 확인** → PickUp 검사 기록 |
| i | MoveZToSafeAfterPick | **[병렬 명령] PickerZ → Avoid ∥ EjectPinZ → Avoid** (NeedleZ는 티칭 유지) | **[대기] 완료** |

---

## 피커 1개 사이클 타임라인 요약 (Phase B, 표준 모드)

```
상대피커존클리어[대기]
 → (안전확인: Z/StageT/EjectPinZ/PickerY Avoid)
 → PickerX+T ──┐
               ├─[WhenAll 대기]─→ PickerY 전진[명령+대기]
 → NeedleX/StageY ─┘
 → Flow OFF[대기]
 → NeedleZ∥EjectPinZ 준비[병렬+대기] → Vacuum ON[명령]
 → PickerZ PrePick[명령+대기] → 저속 Contact[명령+대기+settle]
 → PickerZ∥EjectPinZ SyncLift[병렬+대기+settle]
 → PickerZ Separate→Avoid[명령+대기] (도중 Vacuum OFF·EjectPinZ Avoid)
 → PickSettle[대기] → Flow ON[대기]
 → PickerZ∥EjectPinZ Safe[병렬+대기]
 → 자재갱신 → 다음 피커(4→1)
```

---

## 특수 분기 (표준 경로 외 — 존재만 표기)

| 분기 | 내용 |
|---|---|
| ContiSegmentedPickUp 모드 | X/StageY/T 이동과 Z 하강(Contact까지)을 ContiNode로 결합. 이후 `RunPickupZMotionAfterContiContactAsync`가 SyncLift부터 수행 |
| SimpleZDownVacuumUp 모드 | 단순 Z하강 → Vacuum ON → settle → Z상승 (`RunSimplePickupZMotionAsync`) |
| InputCamera Mark 선행검사 허가 | 상위 시퀀스가 검사까지 끝낸 배치를 허가 스토어로 전달 → Phase A 전체 생략 |
| Picker Motion Only Test | InputVisionX 이동·비전 검사 생략, 오프셋 0 |
| Simulation/DryRun | 비전 오프셋 모의(랜덤/0), Flow 확인 bypass |
| 수동 경로 | 선택 Die PickUp / Z 단독 테스트 / 스텝 단위 실행 (`RunManual...`) |
| 웨이퍼 완료 드레인 | `ShouldBlockNewPickForWaferCompletion` — 안전 경계에서 잔여 Pick 중단 |
