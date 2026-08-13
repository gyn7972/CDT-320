# CDT-320 코드 분석 — 피커 선입선출(FIFO) 재시작 우선순위

- 작성일: 2026-08-13
- 요구사항: 프론트/리어 픽커가 **둘 다 다이를 보유한 상태**에서 장비 정지 → 재시작 시,
  **다이 순서(투입 순번)가 더 빠른 다이를 가진 픽커부터** 공정을 진행해야 한다.
- 범위: 분석 단계만. 코드 수정 없음. (시퀀스/인터락 수정은 무조건 승인 후 진행 규칙 적용)

---

## 1. 재시작 시 픽커 순서 결정 구조 (콜 체인)

```
START(재시작)
└─ AutoSequenceCoordinator.RunAsync()                       [AutoSequenceCoordinator.cs:87]
   ├─ PickerFirstForwardSequencer.BeginRun()                 [:106]  ← run 단위 상태 초기화
   ├─ PickerFirstForwardSequencer.ConfigureActiveSides(...)  [:109]
   ├─ ConfigureRestartPickerDrain()                          [:112 → :386]
   │  ├─ ResolveRestartPickerDrain(Front/Rear)               [:463]  ← side별 required/rank 산정
   │  └─ PickerFirstForwardSequencer.ConfigureResumeDrain()  [:400]
   └─ (각 side) PickerProcessSequence.ExecuteProcessUntilCompleteAsync()
      ├─ EnsureResumeDrainTurnBeforeProcessStepAsync()       [PickerProcessSequence.cs:354]
      │  └─ PickerFirstForwardSequencer.WaitResumeDrainTurnAsync()  ← ★재시작 순서 결정 지점
      ├─ (첫 전진 스텝) PickerFirstForwardSequencer.AcquireAsync()  ← run당 1회 첫 전진 게이트
      └─ ... 공정 완료 후 CompleteResumeDrainAfterProcessAsync()
         └─ PickerFirstForwardSequencer.CompleteResumeDrain()       ← 다음 side에게 턴 넘김
```

재시작 시나리오(앱 재기동 포함)에서 픽커 위 다이는 `MaterialStateService`
스냅샷으로 복원되며, 두 시나리오(동일 세션 재시작 / 앱 재기동) 모두 위 경로를 동일하게 탄다.

## 2. 현재 우선순위 정책

### 2-1. 재시작 드레인 rank 산정 — `AutoSequenceCoordinator.ResolveRestartPickerDrain` (:463~513)

| 상태 | required | rank |
|---|---|---|
| 픽커 위에 다이 있음(target 또는 non-target) | true | **RankBottomSide(2) 고정** |
| 다이 없음 + Input 준비 픽 대상 있음 | true | RankPickUp(1) |
| 그 외 | false | — |

**양쪽 픽커가 다이를 들고 있으면 무조건 rank 2 동률**이 된다. Place 가능 상태 여부는 rank에 반영되지 않는다.

### 2-2. 동률 타이브레이크 — `PickerFirstForwardSequencer`

동률이면 **무조건 Front 우선**이 하드코딩되어 있다. 총 3곳:

| 위치 | 역할 |
|---|---|
| `IsHighestResumeDrainPriorityNoLock` (PickerFirstForwardSequencer.cs:371~393, 동률 분기 :386~389) | 재시작 드레인 홀더 결정 — **핵심 결정 지점** |
| `IsHigherResumeDrainPriorityNoLock` (:418~434, 동률 분기 :432~433) | 다음 드레인 side 예약(Expected 사전 설정, `ConfigureExpectedForNextResumeDrainNoLock`에서 사용) |
| `IsHighestPriorityNoLock` (:338~358, 동률 분기 :352~354) | run당 1회 첫 전진 게이트 — 드레인 활성 중에는 Expected가 드레인 홀더로 제한되므로 드레인 결정에 종속 |

### 2-3. 드레인 턴의 의미

드레인 턴을 잡은 픽커는 **Place + Picker Avoid 복귀 + Output 후검사 idle까지** 남은 공정을
전부 완주하고(`CompleteResumeDrain`), 그동안 상대 픽커는 공정 진입 자체가 보류된다
(PickerProcessSequence.cs:404~416 로그 참조). 즉 재시작 시 첫 번째로 진행할 픽커
선택이 그대로 "어느 다이가 먼저 Place되는가"를 결정한다.

## 3. 왜 문제인가 — 출력 슬롯은 Place 순서대로 소비된다

- Output 웨이퍼 수납 슬롯은 `OrderIndex` **오름차순으로 다음 pending 슬롯을 소비**한다
  (`MaterialStateService.OutputReceive.cs:2122~2144 ResolveNextOutputReceiveIndex`, :243~250).
- 따라서 재시작 후 **나중 순번 다이를 든 픽커(Front)가 먼저 Place하면, 앞쪽 출력 슬롯을
  나중 다이가 차지**한다 → 출력 웨이퍼 상 다이 배열이 투입 순서와 어긋남 (선입선출 위반).

**문제 시나리오 예:**
1. Rear가 다이 #5~#8 픽업 → 검사 진행 중, Front가 다이 #9~#12 픽업.
2. 알람/정지 → 재시작.
3. `ResolveRestartPickerDrain`: Front=rank2, Rear=rank2 (동률).
4. 동률 → Front 우선 → **#9~#12가 #5~#8보다 먼저 Place됨.**

## 4. 다이 순서 데이터 (FIFO 키 후보)

| 필드 | 위치 | 의미 | 비고 |
|---|---|---|---|
| `DieMaterial.InputSequenceNo` | MaterialModel.cs:602 | 웨이퍼 내 픽업 순번 1-base | 0=미부여. 맵 승인 시 부여(MaterialStateService.InputPick.cs:1005). 비전 die_index로도 사용. **웨이퍼가 바뀌면 1부터 재시작** |
| `DieMaterial.PickedAt` | MaterialModel.cs:610 | 실제 픽업 시각 | 픽업 시 `DateTime.Now` 기록(InputPick.cs:2716, 3018). 스냅샷에 영속화(MaterialSnapshotStore.cs:1132) → 앱 재기동 후에도 유지 |
| `InputStagePickTarget.OrderIndex` | MaterialModel.cs:655 | 픽 대상 순번 | 픽커 위 다이에는 직접 남지 않음 |

- 픽커당 헤드 4개(pickerNo 1~4)이므로 side별 FIFO 키는 **보유 target 다이 중 최소값**
  (min InputSequenceNo 또는 earliest PickedAt)으로 산출해야 한다.
- 다이 조회는 `MaterialStateService.GetDieAtPicker(location, pickerNo)` (DieOps.cs:370) 사용 —
  `ResolveRestartPickerDrain`이 이미 동일 패턴으로 순회 중(AutoSequenceCoordinator.cs:485~494).

### 키 선택 시 고려사항 (미결 → 설계 단계에서 결정 필요)

1. **웨이퍼 교체 경계**: 두 픽커가 서로 다른 웨이퍼의 다이를 들고 있으면 `InputSequenceNo`
   비교가 역전될 수 있음(새 웨이퍼는 1부터). `PickedAt`은 전역 시각이라 이 경우에도 안전.
2. **PickedAt 미기록 케이스**: 수동 헤드 편집(PickerHeadDieDialog) 등으로 올라간 다이는
   `PickedAt=MinValue`일 수 있음 → 폴백 필요(InputSequenceNo → 최종 폴백 Front).
3. **non-target 다이만 보유한 side**: 현재도 required=true(rank2, 작업자 회수 사유)로 잡힘.
   FIFO 키가 없는 상태 → 폴백 규칙 필요.

## 5. 수정 후보 지점 (설계 예고 — 승인 전 수정 없음)

핵심만 바꾸면 되는 구조다. **rank 체계는 유지**하고, 동률일 때의 타이브레이크만
"Front 고정"에서 "다이 순서 빠른 쪽"으로 바꾼다:

1. `AutoSequenceCoordinator.ResolveRestartPickerDrain`: side별 FIFO 키(보유 target 다이의
   earliest PickedAt / min InputSequenceNo)를 함께 산출.
2. `PickerFirstForwardSequencer.ConfigureResumeDrain`: FIFO 키를 받도록 시그니처 확장.
3. `IsHighestResumeDrainPriorityNoLock` + `IsHigherResumeDrainPriorityNoLock`: 동률 시
   Front 고정 → FIFO 키 비교(키 없음/동일 시 기존 Front 폴백 유지).
4. 로그 계측: drain configure 로그(AutoSequenceCoordinator.cs:409~415)에 FIFO 키 값·선정
   사유 추가, 드레인 턴 획득/대기 로그에 키 노출 → 실런 1회로 선정 근거 확인 가능하게.

- 첫 전진 게이트(`IsHighestPriorityNoLock`)는 드레인 활성 중 Expected 제한에 종속되므로
  1차 범위에서는 손대지 않아도 재시작 FIFO 요구는 충족된다(신규 시작 시엔 보유 다이가 없어
  FIFO 무의미). 동일 정책으로 맞출지는 설계 단계에서 결정.

## 6. 통과해야 할 인터락/게이트 (사전 전수 점검)

재시작 드레인 순서를 바꿔도, 선정된 픽커가 실제로 움직이려면 아래 기존 게이트를 그대로 통과한다
(이번 수정 범위에서 이 게이트들은 변경 없음):

| 게이트 | 위치 | 영향 |
|---|---|---|
| CheckUnit 시작 안전배치(INV-7, Avoid 확인) | PickerProcessSequence.cs:632~639 | 순서 무관, side별 동일 |
| Resume drain 턴 대기 | PickerProcessSequence.cs:354 | ★이번 수정 대상 정책의 소비자 |
| 첫 전진 게이트(run당 1회) | PickerProcessSequence.cs:306~335 | 드레인 홀더로 Expected 제한됨 |
| PickerPhaseCoordinator phase matrix | PickerPhaseCoordinator.cs:136~201 | 상대 Idle이면 항상 진입 허용 → 드레인 보류 중 상대는 Idle이므로 충돌 없음 |
| 상대 PickerY Avoid 대기 + RealtimeCollisionSupervisor | 기존 물리 충돌 방지층 | 변경 없음, 최종 안전은 여기가 담당 |
| Process 리소스(AutoSequenceGate.BeginPickerProcessAsync) | PickerProcessSequence.cs:222~232 | 드레인 대기 중 일시 반환 후 재점유(기존 동작 유지) |

Front가 드레인 대기(보류)로 남는 동안 Rear가 완주하는 흐름은 현재 Rear가 대기하는 흐름의
좌우 대칭이므로, 새 인터락 경로는 발생하지 않는 것으로 판단된다.

## 7. 결론

- 재시작 시 "누가 먼저"는 `PickerFirstForwardSequencer`의 재시작 드레인 정책이 단독 결정하며,
  현재 양쪽 다이 보유 시 **rank 동률 → Front 고정**이라 선입선출이 보장되지 않는다.
- 다이 순서 판별 데이터(`InputSequenceNo`, `PickedAt`)는 이미 존재하고 재기동 후에도 복원된다.
- 수정 지점은 3개 파일 이내(AutoSequenceCoordinator + PickerFirstForwardSequencer, 로그 포함)로
  국소적이며, rank 체계·기존 인터락은 그대로 유지 가능하다.
- 설계 확정 전 결정 필요: FIFO 키(PickedAt 우선 vs InputSequenceNo 우선), 키 부재 시 폴백 규칙.
