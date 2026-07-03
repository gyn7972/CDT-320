# SEQUENCE_SAFETY_REMEDIATION_SPEC.md

CDT-320 듀얼 픽커 — **시퀀스 자체가 인터락 상황을 만들지 않도록** 스텝 단위로 "무엇을 확인 / 동작 / 대기"하고, 현재 빠진 부분을 어떻게 보강할지 정의한 스펙. 인터락(실시간 감시자)은 최후 안전망이며, 이 문서는 그 이전에 시퀀스가 지켜야 할 규칙이다.

구동 속도 X/Y/Z ≈ 2000/12000/12000. **한 번 충돌 = 끝**, 인터락이 멈춰도 이미 시작하면 끝. 따라서 시퀀스는 "위험 궤적을 애초에 발행하지 않는다"를 원칙으로 한다. 작업 시 [AGENTS.md](AGENTS.md) 준수. 관련: [START_STOP_SAFETY_GAP_ANALYSIS.md](START_STOP_SAFETY_GAP_ANALYSIS.md), [REALTIME_COLLISION_SUPERVISOR_DESIGN.md](REALTIME_COLLISION_SUPERVISOR_DESIGN.md).

표기: **[OK]** 현재 있음 · **[ADD]** 없음, 추가 필요 · **[FIX]** 있으나 불완전/비대칭.

---

## 1. 불변식 (INVARIANT) — 모든 스텝이 절대 위반 금지

| ID | 불변식 | 검증 위치 |
|---|---|---|
| INV-1 | 두 픽커 X가 마주보기 거리(FacingClearanceX, 기본 150mm) 안이면 **최소 한쪽 Y=Avoid**. 양쪽 동시 비-Avoid 절대 금지. | 모든 Y 전진·X 이동 전 |
| INV-2 | Y 전진(비-Avoid)은 `ForwardYPermitted` phase(PickUp·Bottom·Side·Place + Die 보유)에서만. 그 외 phase는 이동 전 Y=Avoid 선행. | Y 이동·phase 진입 전 |
| INV-3 | X 이동(피치·존간)은 자기 Y=Avoid이거나 오토 연속/보정 예외에 한함. 마주보기 구간이면 상대 Y=Avoid 확인. | 모든 X 이동 전 |
| INV-4 | Z 하강은 Bottom/Side/Place phase에서만. Z 하강 중 X/Y/T는 CorrectionWindow(±한계) 내에서만. | Z·XYT 이동 전 |
| INV-5 | 인풋/아웃풋 검사 락 보유 중 상대 픽커의 해당 존 진입 금지(대기). | 존 진입 전 |
| INV-6 | **인터락(phase lease·신호) 해제 전 자기 픽커는 물리 Safe(Y=Avoid, Z=Avoid).** | Abort·완료·정지·phase 전환 |
| INV-7 | 모든 이동 재발행(시작·재개·정지후) 전 실제 좌표가 안전 배치인지 검증, 아니면 Safe 정규화 선행. | 시작/재개 첫 이동 전 |

> 현재 시퀀스는 INV-6, INV-7을 구조적으로 보장하지 않는다(가장 큰 구멍). 아래 스텝 보강의 핵심.

---

## 2. 공통 안전 프리미티브 (신규 추가) — 여러 스텝에서 재사용

이 헬퍼들을 `PickerSequenceBase`에 추가하고 각 스텝에서 호출한다. 중복 로직을 한 곳으로 모아 경로별 비대칭(현재 Place만 보호되는 문제)을 없앤다.

1. **`EnsureSelfSafeAsync(reason, ct)`** — 자기 픽커를 Z=Avoid → Y=Avoid 순으로 물리 후퇴, 완료 검증. INV-6/INV-7 이행. (실패 시 Fail + Alarm, 한글 로그.)
2. **`VerifySafeStartConfigAsync(ct)`** — 양 픽커 Y=Avoid·전체 Z=Avoid·X 비대면을 실제 좌표로 확인. 어긋나면 `EnsureSelfSafeAsync` 후 재확인. 시작/재개 첫 이동 전 게이트.
3. **`WaitOppositeYSafeForFacingAsync(targetX, ct)`** — 자기 X 목표가 상대와 마주보기 거리에 들어가면 상대 Y=Avoid까지 대기(오토)/Fail(수동). INV-1/INV-3.
4. **`CanAdvanceYNow(out reason)`** — Y 전진 허용 판정: 상대 Y=Avoid이거나 다른 존(CanShareForwardY). INV-1/INV-2. 실시간 감시자 판정과 동일 파라미터 공유.
5. **`WaitInspectZoneUnlockedAsync(zone, ct)`** — 인풋/아웃풋 검사 락 해제까지 대기. INV-5.
6. **`AssertZDownCorrectionWindow(target)`** — Z 하강 중 XYT 목표가 창 내인지. 창 밖이면 Block. INV-4.

원칙: **동작 명령 직전에 확인, 확인 실패 시 "대기(협조)" 또는 "Safe 정규화 후 재확인"**, 기하적 불가·오조작은 Fail. 위험 궤적은 발행하지 않는다.

---

## 3. 시작 우선순위 오케스트레이션 (Input → Front → Rear → Output)

**현재 문제**: `AutoSequenceCoordinator.RunAsync`(Sequencing/Common/AutoSequenceCoordinator.cs:83-84)가 4개 유닛을 우선순위 없이 동시에 `Task.Run`으로 실행 → 최초 시작 시 두 픽커가 동시에 Y 전진/X 이동을 시작해 경합. phase coordinator는 매 시작 Idle로 초기화(PickerPhaseCoordinator.cs:8-9)라 물리 상태를 모른 채 양쪽 진입 허용(INV 위반 소지).

**보강안 — 시작 배리어(StartupSequencer)**: 유닛은 병렬로 띄우되, 각 유닛의 **첫 전진 동작**을 우선순위 토큰으로 게이트한다.

| 순위 | 유닛 | 첫 동작 진입 조건 (게이트) |
|---|---|---|
| 0 (선행) | **콜드 세이프 정렬** | 어떤 유닛도 움직이기 전, `VerifySafeStartConfigAsync` 통과(양 픽커 Y=Avoid·Z=Avoid·X 비대면). 미충족 시 각 픽커 순차 Safe 정규화. INV-7. |
| 1 | **Input** | 콜드 세이프 완료 후 즉시 시작(웨이퍼 로드·Align·DieMapping·InputCamera). `InputReady` set. |
| 2 | **Front Picker** | `InputReady` **AND** Front가 startup 토큰 획득. PickUp 첫 진입(Input존)은 인풋 검사 락 해제 확인 후. |
| 3 | **Rear Picker** | Front가 **Input Pick존을 벗어나 비대면 상태 도달** 후(또는 Front 토큰 반납 후) 시작. 두 픽커가 동시에 Input존/마주보기로 진입하지 않도록. |
| 4 | **Output** | 스테이지 준비는 병행 가능하나, 픽커 Place 진입은 검사 완료 다이 발생 + 아웃풋 검사 락 해제 후(자연 gate). |

- 구현: `MachineController.StartSequenceAsync`에서 `SequenceRunOptions`에 **StartupPriority + ColdStart 플래그** 추가, `AutoSequenceCoordinator`가 각 유닛 첫 전진 게이트를 우선순위대로 해제. 콜드 세이프 완료 전에는 전 유닛 전진 보류.
- 콜드 스타트 이후에는 기존 phase coordinator/실시간 감시자가 상시 조정을 이어받는다(정상 생산 중에는 Front/Rear가 서로 다른 존이면 병렬 진행 허용, INV-1만 지키면 됨).

### 3.1 정지 → 재시작(Resume) 우선순위

**핵심 차이**: 콜드 스타트는 "고정 순서(Input→Front→Rear→Output)"면 되지만, **재시작은 각 유닛이 멈춰 있던 실제 물리·자재 상태에 따라 순서가 달라진다.** 두 픽커가 모두 다이를 들고 공정 중간에 멈췄을 수 있으므로, 고정 순서로 밀어붙이면 오히려 위험하다. 원칙은 **"한 번에 한 픽커만 전진 상태로 재개하고, 나머지는 그 픽커가 마주보기/공유존을 벗어날 때까지 Safe 대기"**.

재시작 게이트 순서:

| 순위 | 단계 | 진입 조건 / 판정 |
|---|---|---|
| 0 (선행) | **ReconcileSafeState** | 재개 전 **실제 좌표 + 자재상태**를 읽어 각 픽커를 분류: ⓐ 다이 보유+Y전진(공정중) / ⓑ Safe(Y·Z Avoid). 두 픽커가 마주보기 위험 구성이면, **낮은 우선 픽커를 먼저 Safe로 정규화**(그 이동도 게이트 통과). INV-7. |
| 1 | **완료 근접 픽커 우선** | 두 픽커 모두 공정중이면 **완료에 가까운 쪽(Place > Side > Bottom > PickUp 순)**을 먼저 재개해 공유존/마주보기에서 **먼저 빠져나가게** 한다. 나머지 픽커는 Safe에서 대기. |
| 2 | **Place 재개 최우선 vacate** | Place 단계였던 픽커는 먼저 Place 완료 → 전체 Avoid 복귀(vacate)하도록. Place 중이면 상대의 Bottom/Side/PickUp 전진은 대기(INV-1/5). |
| 3 | **Output 재무장** | Place 재개 전 Output 스테이지가 수령위치 신호(OutputGood/NgStageReady)를 **재-Set** 했는지 확인(재시작 시 신호는 초기화됨, §3-B 참고). |
| 4 | **Input 재개는 필요 시** | 두 픽커가 이미 다이 보유면 Input(웨이퍼 로드/카메라)은 픽커가 빌 때까지 idle 유지. PickUp 필요 픽커가 있을 때만 InputReady 재확립. |
| 5 | **나머지 픽커 해제** | 1순위 픽커가 마주보기/공유존을 벗어난 비대면 체크포인트 도달 후 두 번째 픽커 첫 전진 게이트 해제. |

- 콜드 스타트와 동일한 StartupSequencer 메커니즘을 쓰되, **순서를 고정값이 아니라 ReconcileSafeState 결과로 결정**한다.
- 재시작도 "첫 전진 게이트"는 반드시 한 픽커씩 순차 해제(동시 전진 금지). 이후 정상 생산은 상시 감시자/phase coordinator가 이어받음.
- 현재 문제: 재시작이 `ResolveNextProcessStepFromMaterial`로 자재만 보고 곧바로 이동 → 실제 위치·상대 상태 무시. 위 0~5 게이트를 `AutoSequenceCoordinator` 재시작 경로에 삽입.

---

## 3-B. 인풋 ↔ 픽커 ↔ 아웃풋 메시지 핸드셰이크 검증

유닛 간 통신은 `SequenceSignalBus`(TaskCompletionSource 원샷 래칭: Set/WaitAsync/Reset/IsSet)로 이뤄진다. 주요 신호: `InputStageReady`(Set 7 / Reset 3, 비대칭)·`InputStageDieComplete`·`OutputGoodStageReady`·`OutputNgStageReady`·`Output*StageReceiveComplete`·`OutputPostPlaceInspectionIdle` 등. 핸드셰이크가 "제대로 주고받는지" 관점의 갭:

| ID | 갭 | 위험 | 보강 |
|---|---|---|---|
| H-1 | **Set/Reset 비대칭·소유 불명** (InputStageReady Set 7 / Reset 3). 누가 소비 후 Reset하는지 규약 없음 | 이전 사이클의 Set가 남아 다음 사이클에서 픽커가 준비 안 된 스테이지로 **조기 진입** | 신호별 producer/consumer·Reset 시점을 1:1로 규정(핸드셰이크 계약표 작성) |
| H-2 | **IsSet 폴링 게이트 = TOCTOU** (예: OutputStageReady를 IsSet로 확인 후 이동). 확인과 동작이 원자적이지 않음 | 확인 직후 상대가 상태 변경 → 어긋난 채 진입 | 게이트는 IsSet 스냅샷이 아니라 **WaitAsync + 진입 직전 재확인**(또는 리스로 원자화) |
| H-3 | **재시작마다 새 Bus 생성**(StartSequenceAsync가 매번 new SignalBus) → 모든 신호 초기화 | 재개 시 consumer가 신호를 **기다리지 않고 진행하면** producer 재-Set 전에 움직임 | 모든 consumer는 IsSet 가정이 아니라 **WaitAsync로 대기**. 재개 시 Output/Input이 필요한 Ready 신호를 **반드시 재-Set**하도록 순서 보장(§3.1 순위 3·4) |
| H-4 | **WaitAsync 무한 대기**(타임아웃 없음, ct만) | producer가 영영 Set 안 하면(유닛 비활성/실패) 픽커가 **영구 대기(데드락)** — 충돌은 아니나 라인 정지 | Wait에 타임아웃+Alarm, 상대 유닛 실패/비활성 시 대기 해제 규칙 |
| H-5 | **검사존 배타가 명시적 락이 아니라 리스/존 점유 추론** | 인풋 카메라 검사 중 / 아웃풋 place검사 중 픽커 진입 배타가 타이밍 의존 | INV-5의 **명시적 InspectZoneLock**(인풋/아웃풋)으로 대체, 픽커는 락 대기 |
| H-6 | **픽커↔스테이지 수령 핸드셰이크의 상호 이동 금지 미보장** (OutputStageReceiveComplete 등) | 스테이지가 수령위치 도달 전 픽커 Place 하강 / 픽커 진입 중 스테이지 이동 | "스테이지 수령위치 확정 → 픽커 진입 허용", "픽커 존 이탈 확정 → 스테이지 이동 허용"을 신호+락으로 양방향 잠금 |

> 요약: 핸드셰이크 자체는 동작하지만 **① 재시작 시 신호 초기화 후 재확립 순서, ② IsSet 폴링의 원자성, ③ Reset 소유권, ④ 무한 대기**가 구멍이다. 신호별 **핸드셰이크 계약표(producer/consumer/Set·Reset 시점/타임아웃)**를 만들어 문서화하고, 검사존 배타는 명시적 락으로 전환하는 것이 핵심.

---

## 4. 시퀀스별 스텝 분석 (확인 / 동작 / 대기 / 보강)

각 시퀀스의 충돌·대기 관련 스텝만 정밀 표기. 스텝 번호는 `*Step.cs` enum 순서. 나머지 스텝은 위험 무관으로 생략.

### 4.1 PickerProcessSequence (오케스트레이터)

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 1 | CheckUnit | Servo/Alarm/HomeDone | 자재상태로 시작스텝 분기 | — | **[ADD]** `VerifySafeStartConfigAsync` 호출(현재 위치·안전존 미검증, INV-7). ColdStart면 자재 무관 Safe 정렬 후 판정 |
| — | (phase 진입 전 공통) | 상대 phase | EnterOrTransitionPickerPhase | 상대 충돌 시 대기 | **[FIX]** phase 진입 전 `EnsureSelfSafeAsync` + `CanAdvanceYNow`. phase 초기값을 물리 위치로 세팅(현재 무조건 Idle) |
| 3 | RunInputCameraMarkInspection | Die 없음(occupied==0) | 선행검사 허가/대기 | 인풋 검사 락 | **[OK]** 락 대기. **[ADD]** 락을 supervisor 공유 락으로 |
| 4 | RunPickUp | — | PickUp 실행 | 상대 Side대기 gate | [OK] |
| 5 | RunBottomInspection | 파이프라인 여부 | Bottom or 통합검사 | 상대 gate | [OK] |
| 6 | RunSideInspection | — | Side 실행 | — | **[FIX]** Side 재개 직접진입 시 사전 Safe-Y 없음(A-4) → 진입 전 `EnsureSelfSafeAsync` |
| 7 | RunPlace | — | Place 실행 | OutputStage 준비 | [OK] Place만 `_forceSafeYBeforePlaceResume` 있음 |
| 8/9 | Complete/Error | — | phase·신호 해제 | — | **[ADD]** `Abort()`가 리스 해제 전 `EnsureSelfSafeAsync` (INV-6, 최우선 A-1) |

### 4.2 PickerPickUpSequence

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 5 | MoveAllPickerZToAvoid | InputStageArea 획득 | 전체 Z Avoid | InputStageArea 대기(Auto) | [OK] |
| 9 | MovePickersToAvoidForInputVisionMove | 상대 Input존 이탈 | 자기·상대 Avoid | 상대 이탈 대기 | [OK] |
| 10 | MoveInputStageAndVisionToDie | 작업영역·경로 | Stage/Vision→Die | — | **[ADD]** InputVisionX↔PickerX 마주보기 실시간 확인은 supervisor 위임 |
| 17 | MoveOppositePickerToAvoidForPickerMove | 상대 Input Pick존 없음 | — | 상대 이탈 | [OK] INV-5 |
| 18 | MovePickerXStageYPickerT | 작업/Needle영역 | X/T·StageY 이동 후 **Y 전진** | — | **[FIX]** 연속 아닐 때만 EnsurePickerYAtAvoid → **항상** X/T 전 자기 Y=Avoid 보장 + `WaitOppositeYSafeForFacing`(INV-1/3) |
| 19 | VerifyPickTarget | 축 In-Position·미점유 | 검증 | — | [OK] |
| 20 | MovePickerZPick | — | Z 하강 픽업(Vacuum·Needle) | — | **[ADD]** Z 하강 전 `CanAdvanceYNow` 상태에서만(이미 Y 전진 정상), Z 하강 중 X/Y/T는 CorrectionWindow(INV-4) |
| 23 | MovePickerZToAvoid | — | Z 안전복귀(Die 보유) | — | [OK] Die 보유→Z Up 정상 |

### 4.3 PickerBottomInspectionSequence

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 4 | MoveOppositePickerToAvoidBeforeInspection | 상대 Avoid | 상대 정리 | — | [OK] |
| 5 | SelectNextPicker | Y 안전 여부 | 목표 계산, 미안전 시 Y Avoid 선행 | — | **[FIX]** "미안전 시 Y Avoid"를 조건부가 아니라 항상 확인 |
| 6 | MoveBottomYToAvoidBeforeInspection | — | Y Avoid | — | [OK] INV-2 |
| 7 | MoveBottomXToInspection | — | X 검사위치 + Flying Z Down 병렬 | — | **[ADD]** X 이동 전 `WaitOppositeYSafeForFacing`(피치·존 이동 마주보기, INV-3). Flying Z Down은 자기 X 안전 진입 확정 후 |
| 8 | MoveBottomYToInspection | X 정렬 | Y 전진 | — | **[FIX]** Y 전진 전 `CanAdvanceYNow`(상대 마주보기 확인) |
| 9 | MoveBottomZ | — | Z 하강 | — | [OK] 검사 phase |
| 13/16 | MoveBottomZToAvoid / XToAvoid | — | Z·X Avoid | — | [OK] |

### 4.4 PickerSideInspectionSequence

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 3 | MoveAllPickerZToAvoid | 연속검사 여부 | Z Avoid(연속이면 유지) | InspectionArea | [OK] |
| 5 | SelectNextPicker | 연속·안전 여부 | 목표 계산 | — | **[FIX]** 비연속·미안전 시 Y Avoid를 항상 보장 |
| 7 | MoveSideXToInspection | X 정렬·연속 | X/Y + SideVisionY0 + 이전Z Avoid 3중 병렬 | — | **[ADD]** X 피치 이동 마주보기 `WaitOppositeYSafeForFacing`(INV-3). 병렬에 Y 전진 포함 시 `CanAdvanceYNow` |
| 8 | MoveSideYToInspection | X 정렬 | Y 전진 | — | **[FIX]** Y 전진 전 `CanAdvanceYNow` |
| 12 | MoveSideT90 | — | T90 + SideVisionY90 병렬 | — | [OK] (Z 하강 유지 → XYT는 CorrectionWindow 안이어야, INV-4) |
| 15/16 | MoveSideZToAvoid / TToSafe(지연T0) | — | Z Avoid, T0 지연복귀 | — | **[ADD]** 지연 T0 백그라운드 명령이 정지/재개와 겹칠 때 취소·재검증(C-3) |

### 4.5 PickerBottomAndSideInspectionSequence (Auto 통합·파이프라인)

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 4 | MoveOppositePickerToAvoidBeforeInspection | ForceBottom 재개 여부 | 상대 Avoid, (강제면) 자기 Y Avoid 선행 | — | **[FIX]** 자기 Y Avoid 선행을 강제모드에만 두지 말고 **모든 재개**에 적용(A-4) |
| 5 | RunBottomPipeline | 기존결과·강제 | Bottom 몰아 노출, Z 선행하강 | pending≥3 회수 | **[ADD]** 피치 X 이동마다 `WaitOppositeYSafeForFacing`. 다음 Z 선행하강은 자기 X 안전 확정 후(INV-3/4) |
| 6 | RunSidePipeline | Bottom 완료 | Side 0/90, Side X=Bottom 피치 | EnsureBottomReadyForSide | [OK] 파이프라인. **[ADD]** X 피치 마주보기 확인 |
| 7/8 | MoveFinalZToAvoid / CompletePendingT0 | — | Z Avoid, T0 복귀 | — | **[ADD]** 이후 Y/X Avoid 생략(Place가 이어감) 시, Place 진입까지 **Y 전진+마주보기 무결**을 supervisor가 상시 감시하도록 명시 |

### 4.6 PickerPlaceSequence

| No | 스텝 | 확인 | 동작 | 대기 | 보강 |
|---|---|---|---|---|---|
| 3 | MoveAllPickerZToAvoid | — | 전체 Z Avoid | — | [OK] |
| 6 | VerifyOutputStageReady | 자재·신호 Ready | 수령 대기, PickerY Safe 정리 | OutputStage Ready | [OK] 재시작 ForceSafeY 있음 |
| 8 | MoveOutputStageAvoidPosition | Bin Clamp/Guide | 리소스·Bin 정리·Avoid | 검사 batch idle | **[ADD]** 아웃풋 검사 락 대기(INV-5) 명시 |
| 9 | MoveOutputStageReceivePosition | 3모드 분기 | StageY+PickerXYT+Z 이동 | — | **[FIX]** 재시작=X/T선행→Y전진(안전). 기본/보간 모드도 X 이동 마주보기 `WaitOppositeYSafeForFacing` |
| 10/11 | VerifyPlaceTarget / MovePickerZPlace | 위치 | Z 하강 Place | — | [OK] Z 하강 중 XYT는 CorrectionWindow(INV-4) |
| 19 | MovePickerToAvoidAfterPlace | — | 전체 Avoid 복귀 | — | [OK] INV-6 이행. **[ADD]** 완료 전 phase 해제 순서: Safe 후퇴 완료 → 그 다음 리스 해제 |

---

## 5. 정지·재개 스텝 보강 (모든 시퀀스 공통)

| 상황 | 현재 | 보강 |
|---|---|---|
| Cycle Stop 접수 | 경계에서 예외만 throw, 축 방치 | **[ADD]** 예외 이탈 시점에 각 픽커 `EnsureSelfSafeAsync`(Z Avoid→Y Avoid) 후 정지 확정 (A-2/INV-6) |
| Abort/finally | 리스·신호만 해제 | **[ADD]** 리스 해제 **전** `EnsureSelfSafeAsync`, 후퇴 완료 전 상대 게이트 개방 금지 (A-1/INV-6) |
| Resume 진입 | 자재상태로만 스텝 결정 | **[ADD]** `VerifySafeStartConfigAsync` 선행. 미안전이면 Safe 정규화 후 재개 (A-3/INV-7) |
| 병렬 이동(Task.WhenAll) 정지 | 롤백 없음 | **[ADD]** 실패/취소 시 관련 축 Safe 롤백 (C-2) |
| 백그라운드 pending(T0·선행검사) | 타임아웃 후 방치 | **[ADD]** 정지 시 취소·완전종료 확인, 미종료 시 축 강제정지/Alarm (C-3) |
| stop-time catch{} | 예외 은폐 | **[FIX]** 최소 로그 + 안전관련 실패 Alarm 승격 (C-4, AGENTS.md) |

---

## 6. 적용 순서 (권장)

1. **INV-6/INV-7 프리미티브**(`EnsureSelfSafeAsync`, `VerifySafeStartConfigAsync`) 추가 → Abort·CycleStop·CheckUnit·Resume에 삽입. (A-1/A-2/A-3, 즉효·최우선)
2. **시작 우선순위 오케스트레이션**(§3) — ColdStart 플래그 + StartupSequencer.
3. **Y 전진/X 피치 마주보기 확인**(`CanAdvanceYNow`, `WaitOppositeYSafeForFacing`)을 PickUp18/Bottom7·8/Side7·8/BottomAndSide5·6/Place9에 삽입.
4. 검사 락(INV-5)·Z 하강 CorrectionWindow(INV-4)를 supervisor 공유 파라미터로 정리.
5. 정지·병렬·백그라운드·catch{} 보강(§5).
6. 각 단계 후 빌드(별도 OutDir) + `perl tools/verify_all.pl` + 시나리오(신규시작/정지→재개/수동조그/인풋·아웃풋 병렬대기/저속 충돌유도).

**공통 코딩 규칙**: 확인 실패 시 위험 궤적 미발행(대기 또는 Safe 정규화), 기하 불가·오조작만 Fail. 신규 로그/알람 한글. `catch{}` 금지. 이동 반환 `Task<int>`.
