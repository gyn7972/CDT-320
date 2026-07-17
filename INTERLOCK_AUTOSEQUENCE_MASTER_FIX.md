# INTERLOCK_AUTOSEQUENCE_MASTER_FIX.md

CDT-320 듀얼 픽커 — **인터락 / 오토 시퀀스 검증·수정 통합 마스터 스펙 (구현용, 단일 파일)**.
이 문서 하나로 근본 구조 수정을 진행한다. 기존 분리 문서(REALTIME_COLLISION_SUPERVISOR_DESIGN / START_STOP_SAFETY_GAP_ANALYSIS / SEQUENCE_SAFETY_REMEDIATION_SPEC / INPUTVISIONX_PREINSPECTION_INTERLOCK_FIX)를 통합·대체한다.

작업 전 필독 규칙: [AGENTS.md](AGENTS.md). Material 상세 설계 참고: [MATERIAL_ARCHITECTURE_PLAN.md](MATERIAL_ARCHITECTURE_PLAN.md).

## 절대 원칙
- 기존 변경 되돌리지 말 것. 실장비 충돌 방지 최우선(속도 X/Y/Z≈2000/12000/12000, 1회 충돌=끝).
- phase gate / resource gate / interlock 우회 금지. 빌드는 별도 OutDir. 신규 로그/알람 한글.
- Picker X/Y/Z/T 명령은 PickerProcess/해당 Picker 시퀀스가 소유. Input/Output 카메라 시퀀스는 Picker 축을 직접 움직이지 않으며 **공용 X 레일도 본 공정에 양보**.
- **인터락은 최후 안전망**이다. 시퀀스가 애초에 위험 궤적을 발행하지 않아야 한다.
- **협조적 경합(상대가 점유·검사 중)은 대기/양보. 실제 임박 충돌만 하드정지.** 부가작업(선행검사 등) 실패로 본 공정을 죽이지 않는다.

---

## 0. 선행 필수 — 손상 파일 복구 (빌드 차단 상태)

현재 워킹트리의 수정 `.cs` **39개가 파일 tail이 문장 중간에서 잘림(truncate)** — 컴파일 불가. 커밋(HEAD)은 정상.
- 증상 예: `AjinAxis.cs`=`return`에서 끊김, `MotionGuardRuntime.cs`=`public Mo`, `MotionGuardRuleRegistry.cs`=`return MotionGuardRuleHe`, `PickerPlaceSequence.cs`=`{`로 끝, 여러 `*SequenceBase.cs`=널바이트.
- 조치: 각 파일의 의도한 전체 내용을 **온전히 재작성**(잘린 tail 복구). 손으로 붙이지 말고 재적용. `PickerProcessSequence.cs`·`PickerZoneInterlockRules.cs` 포함 — 이 둘은 아래 수정 대상이므로 특히 우선.
- 복구 후 `git diff --ignore-cr-at-eol HEAD`로 실제 논리 변경만 확인(줄바꿈 노이즈 제외).

---

## 1. 불변식 (INVARIANT) — 모든 스텝이 절대 위반 금지

| ID | 불변식 | 검증 위치 |
|---|---|---|
| INV-1 | 두 픽커 X가 마주보기 거리(FacingClearanceX, 예 150mm) 안이면 **최소 한쪽 Y=Avoid**. 동시 비-Avoid 금지 | 모든 Y 전진·X 이동 전 |
| INV-2 | Y 전진(비-Avoid)은 ForwardYPermitted phase(PickUp·Bottom·Side·Place + Die 보유)에서만. 그 외 이동 전 Y=Avoid | Y 이동·phase 진입 전 |
| INV-3 | X 이동(피치·존간·공용레일)은 자기 Y=Avoid이거나 오토 연속/보정 예외에 한함. 마주보기면 상대 Y=Avoid 확인 | 모든 X 이동 전 |
| INV-4 | Z 하강은 Bottom/Side/Place phase에서만. Z 하강 중 X/Y/T는 CorrectionWindow(±한계) 내에서만(정렬 오프셋 보정) | Z·XYT 이동 전 |
| INV-5 | 인풋/아웃풋 검사 락 보유 중 상대 픽커 해당 존 진입 금지(대기) | 존 진입 전 |
| INV-6 | **인터락(phase lease·신호) 해제 전 자기 픽커는 물리 Safe(Y=Avoid, Z=Avoid)** | Abort·완료·정지·phase 전환 |
| INV-7 | 모든 이동 재발행(시작·재개·정지후) 전 실제 좌표가 안전 배치인지 검증, 아니면 Safe 정규화 선행 | 시작/재개 첫 이동 전 |

현재 시퀀스는 INV-6, INV-7을 구조적으로 보장하지 않음(최대 구멍).

---

## 2. 공통 안전 프리미티브 (신규, 여러 곳 재사용)

`PickerSequenceBase` / `MotionGuardService`에 추가하고 스텝에서 호출. 흩어진 판정을 **단일 소스**로 통일해 "대기 조건 = 실제 이동 인터락"을 보장한다.

1. **`MotionGuardService.CanMove(axis, target, ctx, out reason)`** — 실제 이동에 적용될 **MotionGuard 전체 판정을 부작용 없이(dry-run)** 반환. 대기 폴링과 이동 발행이 **동일 규칙**을 쓰게 하는 핵심.
2. **`EnsureSelfSafeAsync(reason, ct)`** — 자기 픽커 Z=Avoid→Y=Avoid 물리 후퇴+검증. INV-6/7.
3. **`VerifySafeStartConfigAsync(ct)`** — 양 픽커 Y=Avoid·전체 Z=Avoid·X 비대면 실제 좌표 확인, 어긋나면 정규화. 시작/재개 게이트. INV-7.
4. **`ReconcileSafeState()`** — 재개 시 실제 좌표+자재로 각 픽커 phase/Y/Z/carrying 재구성(재개=신규시작 취급).
5. **`WaitOppositeYSafeForFacingAsync(targetX, ct)`** — 자기 X 목표가 마주보기 거리면 상대 Y=Avoid까지 대기(오토)/Fail(수동). INV-1/3.
6. **`CanAdvanceYNow(out reason)`** — Y 전진 허용(상대 Y=Avoid 또는 다른 존). INV-1/2.
7. **`WaitInspectZoneUnlockedAsync(zone, ct)`** — 인풋/아웃풋 검사 락 대기. INV-5.
8. **`AssertZDownCorrectionWindow(target)`** — Z 하강 중 XYT 목표 창 검사. INV-4.

원칙: **동작 직전 확인 → 실패 시 대기 또는 Safe 정규화 후 재확인**, 기하 불가·오조작만 Fail. 위험 궤적 미발행.

---

## 3. 실시간 충돌 감시자 (RealtimeCollisionSupervisor) — 단일 권위

흩어진 존/마주보기/per-move 가드를 하나로 통합. 두 책임:

### 3.1 예측 게이트 (이동 전) = §2-1 `CanMove`
모든 이동이 통과. 결과: **Allow / Wait(협조, 알람·정지 없음) / Block(기하 불가)**. 협조적 대기(상대 busy·존 락·게이트 미충족)는 Wait이지 실패가 아니다.

### 3.2 반응 가드 (상시 연속, ~10ms)
명령 추적 없이 **실제 엔코더만으로** 임박 위반 감지 → **전 축 하드정지 + 알람 + 리셋 요구**(오토/수동 동일):
- X 쌍 거리 < 필요 clearance & 접근 중,
- 두 픽커 동시 Y 전진 + X 마주보기,
- 잠긴 검사존 실제 침범.
- 기존 `SharedRailXAutoMoveGuard`(20ms, per-move) 로직을 재사용하되 **상시화 + Y·존 확장**.

### 3.3 상태·예외 모델 (`MotionSafetyState`)
side별 Phase / YState / ZState / Carrying / ForwardYPermitted / CorrectionWindow. 오토 라이프사이클(픽업 Y전진→바텀 Z하강→피치 X이동 바텀→사이드→사이드후 Z상승→플레이스→Y안전복귀)과 Z하강 중 XYT 보정 창을 **한 곳**에 정의 → 오토/수동 이중 규칙 충돌 제거. 시퀀스가 phase 전환 시 `SetPhase(side,phase,carrying)` 통지.

### 3.4 통합 지점
- 이동 발행 단일화: 픽커/공용축 이동 모두 `CanMove` 경유(`MotionGuard`/`SharedRailXMotionService` 위임). 조그 포함.
- 상시 구동: `Form1_Load`에서 MotionMonitorService와 함께 Start(). Cycle 시작·재개 시 `ReconcileSafeState()`.
- 기존 `PickerZoneInterlockRules` Y/존 판정은 supervisor에 위임(중복 제거).

---

## 4. 시작 / 정지→재시작 우선순위

### 4.1 현재 문제
`AutoSequenceCoordinator.RunAsync`(Sequencing/Common/AutoSequenceCoordinator.cs:83-84)가 4유닛(Input/Output/Front/Rear)을 **우선순위 없이 동시 `Task.Run`**. phase coordinator는 매 시작 Idle 초기화(PickerPhaseCoordinator.cs:8-9)라 물리 상태 모른 채 양쪽 진입 허용.

### 4.2 콜드 스타트 우선순위 (Input → Front → Rear → Output)
유닛은 병렬로 띄우되 **첫 전진 동작**을 우선순위 토큰으로 게이트:

| 순위 | 유닛 | 진입 게이트 |
|---|---|---|
| 0 | 콜드 세이프 | 전 유닛 이동 전 `VerifySafeStartConfigAsync` 통과(양 픽커 Y·Z Avoid·X 비대면), 미충족 시 순차 Safe 정규화 |
| 1 | Input | 콜드 세이프 후 시작, InputReady set |
| 2 | Front | InputReady & 토큰 획득, Input존 진입은 인풋 검사 락 해제 후 |
| 3 | Rear | Front가 Input존 벗어나 비대면 도달 후 |
| 4 | Output | 스테이지 준비 병행, Place 진입은 검사완료 다이 + 아웃풋 락 해제 후 |

구현: `SequenceRunOptions`에 **ColdStart + StartupPriority** 추가, coordinator가 첫 전진 게이트를 순차 해제. 이후 정상 생산은 supervisor/phase coordinator가 이어받음.

### 4.3 정지→재시작(Resume) 우선순위 (상태 기반, 고정 아님)
| 순위 | 단계 | 판정 |
|---|---|---|
| 0 | ReconcileSafeState | 실제 좌표+자재로 각 픽커 분류(공정중/Safe). 마주보기 위험이면 낮은 우선 픽커 먼저 Safe 정규화 |
| 1 | 완료 근접 우선 | 둘 다 공정중이면 완료 가까운 쪽(Place>Side>Bottom>PickUp) 먼저 재개해 공유존/마주보기에서 먼저 vacate |
| 2 | Place vacate 우선 | Place였던 픽커 먼저 완료·전체 Avoid, 그동안 상대 전진 대기 |
| 3 | Output 재무장 | Place 재개 전 OutputGood/NgStageReady **재-Set** 확인(재시작 시 신호 초기화됨, §5) |
| 4 | Input 조건부 | 둘 다 다이 보유면 Input idle, PickUp 필요 픽커 있을 때만 재확립 |
| 5 | 나머지 해제 | 1순위 픽커 비대면 도달 후 두 번째 픽커 첫 전진 게이트 해제 |

**한 번에 한 픽커만 전진 재개**. 현재 `ResolveNextProcessStepFromMaterial`(자재만 보고 즉시 이동)에 위 0~5 게이트 삽입.

---

## 5. 유닛 간 메시지 핸드셰이크(SequenceSignalBus) 보강

원샷 래칭(Set/WaitAsync/Reset/IsSet). 갭:
- **H-1** Set/Reset 비대칭·소유 불명(InputStageReady Set 7/Reset 3) → 이전 Set 잔존 조기 진입. → 신호별 producer/consumer/Reset 시점 계약표.
- **H-2** IsSet 폴링 = TOCTOU. → **WaitAsync + 진입 직전 재확인**(또는 리스 원자화).
- **H-3** 재시작마다 새 Bus → 신호 초기화. consumer는 IsSet 가정 말고 WaitAsync 대기. producer(Input/Output)가 재개 시 Ready 재-Set 보장(§4.3-3·4).
- **H-4** WaitAsync 무한 대기 → 유닛 실패/비활성 시 데드락. → 타임아웃+Alarm.
- **H-5** 검사존 배타가 추론 점유 → 명시적 InspectZoneLock(인풋/아웃풋)으로.
- **H-6** 픽커↔스테이지 수령 상호 이동 금지 미보장 → "스테이지 수령위치 확정→픽커 진입", "픽커 이탈 확정→스테이지 이동" 양방향 잠금.

---

## 6. 시퀀스별 스텝 보강 ([OK]있음 [ADD]추가 [FIX]불완전)

### 6.1 PickerProcess (오케스트레이터)
- CheckUnit: **[ADD]** `VerifySafeStartConfigAsync`, ColdStart면 자재 무관 Safe 정렬 후 판정.
- phase 진입 전: **[FIX]** `EnsureSelfSafeAsync` + `CanAdvanceYNow`, phase 초기값 물리 위치로.
- RunSideInspection(6): **[FIX]** Side 재개 직접진입 시 사전 Safe-Y 추가.
- Complete/Abort: **[ADD]** 리스 해제 **전** `EnsureSelfSafeAsync`(INV-6, 최우선).

### 6.2 PickerPickUp
- 18 MovePickerXStageYPickerT: **[FIX]** 연속 아닐 때만이던 EnsurePickerYAtAvoid를 **항상** + `WaitOppositeYSafeForFacing`.
- 20 MovePickerZPick: **[ADD]** Z 하강 중 XYT는 CorrectionWindow(INV-4).

### 6.3 Bottom / 6.4 Side
- SelectNextPicker: **[FIX]** 미안전 시 Y Avoid 항상 보장.
- MoveXToInspection(피치): **[ADD]** X 이동 전 `WaitOppositeYSafeForFacing`(INV-3).
- MoveYToInspection: **[FIX]** Y 전진 전 `CanAdvanceYNow`.
- Side 지연 T0(15/16): **[ADD]** 정지/재개와 겹치면 백그라운드 명령 취소·재검증.

### 6.5 BottomAndSide (파이프라인)
- MoveOpposite…(4): **[FIX]** 자기 Y Avoid 선행을 강제모드뿐 아니라 **모든 재개**에 적용.
- RunBottom/SidePipeline(5/6): **[ADD]** 피치 X마다 `WaitOppositeYSafeForFacing`, 다음 Z 선행하강은 자기 X 안전 확정 후.
- Final Z/T0(7/8): **[ADD]** Place까지 Y전진+마주보기 무결을 supervisor가 상시 감시.

### 6.6 Place
- MoveOutputStageAvoid(8): **[ADD]** 아웃풋 검사 락 대기(INV-5).
- MoveReceivePosition(9): **[FIX]** 재시작 X/T선행→Y전진(안전). 기본/보간 모드도 X 이동 마주보기 확인.
- MovePickerZPlace(11): Z 하강 중 XYT CorrectionWindow.
- MovePickerToAvoidAfterPlace(19): **[ADD]** Safe 후퇴 완료 → 그 다음 phase 리스 해제(순서).

---

## 7. 현재 알람 수정 — InputCamera 선행검사 InputVisionX 인터락

### 7.1 증상
`FrontInputDieVisionPrepareSequence / MoveInputStageAndVisionToDie`가 InputVisionX 0→616 이동, RearPickerX=445.999(Bottom 검사 중, currentZone=Input)와 공용 레일 경합 → 인터락 -11 차단 → `INPUT-DIE-VISION-PREPARE-STAGE-MOVE` Fail → `RearPicker 자동 시퀀스 실패 result=-1` 오토 정지. **인터락은 정상, 시퀀스가 협조 경합을 치명적 실패로 만든 것이 문제.**

### 7.2 근본 원인 (InputDieVisionPrepareSequence.cs)
- **A** 이동 전 대기 `WaitInputVisionXSharedRailClearAsync`(:2031)가 `VerifySingleAxisMove`(SharedRailX 거리)만 확인. 실제로 막은 **PickerZone "Input 영역 점유"** 판정은 대기에 없음 → 대기 통과 후 이동이 다른 인터락에 -11.
- **B** `MoveInputStageAxisCommandAsync` result!=0 → 즉시 `Fail`(:2056). 선행검사(비침습 부가작업)가 본 공정을 죽임.
- **C** 대기↔이동 레이스, -11 재대기 루프 없음.
- **D** 중앙 시작 게이트가 "상대 PickerX가 InputVisionX 경로(0→616)+안전거리와 겹침"을 안 봄(Rear가 Bottom workArea여도 물리 X는 레일 경합).

### 7.3 수정 (§2·§3 프리미티브 사용)
- **FIX-A** 대기 조건을 §2-1 `CanMove`(전체 MotionGuard 판정, PickerZone 포함)로 통일 — 대기·이동 동일 소스.
- **FIX-B** 선행검사 모드(IsInputCameraPreInspectionMode)에서 인터락성 -11은 **Fail 금지** → 재대기/양보/이번 사이클 skip(예약 반납), 상위엔 무해(0/skip) 반환. 본 공정 절대 정지 금지. (일반검사 모드는 기존 Fail 유지.)
- **FIX-C** -11 시 FIX-A 조건으로 재대기 루프(타임아웃까지).
- **FIX-D** `PickerProcessSequence` 선행검사 시작 게이트에 **공용 레일 경로 겹침(실제 상대 PickerX 좌표 기준)** 추가.

### 7.4 대상 파일
`InputDieVisionPrepareSequence.cs`(A/B/C), `MotionGuardService.cs`(CanMove dry-run), `PickerProcessSequence.cs`(D)·`PickerZoneInterlockRules.cs`(판정 공유) — 뒤 둘은 §0 복구 선행.

---

## 8. 적용 순서 (권장)

1. **§0 손상 39파일 복구** → 빌드 가능화(선행 필수).
2. **§2 프리미티브** `CanMove`(dry-run) + `EnsureSelfSafeAsync` + `VerifySafeStartConfigAsync`.
3. **§7 현재 알람 수정**(FIX-A~D) — 즉효, 오토 정지 재발 차단.
4. **INV-6/INV-7 삽입**: Abort·CycleStop·CheckUnit·Resume(§6.1, §9).
5. **§4 시작/재시작 우선순위**(ColdStart + StartupSequencer + Resume 게이트).
6. **§3 실시간 감시자**(예측 게이트 + 상시 반응 가드) + Y/X 마주보기 스텝 삽입(§6).
7. **§5 핸드셰이크**·**§9 정지 안전** 보강. 기존 규칙 중복 정리.
8. 각 단계: 별도 OutDir 빌드 + `perl tools/verify_all.pl` + 시나리오(신규시작/정지→재개/수동조그/인풋·아웃풋 병렬대기/저속 충돌유도 하드정지).

---

## 9. 정지 / 재개 / Abort 안전 (전 시퀀스 공통)

| 상황 | 현재 | 보강 |
|---|---|---|
| Cycle Stop | 경계서 예외만 throw, 축 방치(진행 이동 완주) | 예외 이탈 시 각 픽커 `EnsureSelfSafeAsync` 후 정지 확정 |
| Abort/finally | 리스·신호만 해제, 물리 후퇴 없음 | 리스 해제 **전** `EnsureSelfSafeAsync`, 후퇴 완료 전 상대 게이트 개방 금지 (INV-6, 최우선) |
| Resume | 자재상태로만 스텝 결정 | `VerifySafeStartConfigAsync`/`ReconcileSafeState` 선행, 미안전이면 정규화 후 재개 (INV-7) |
| 병렬 이동(Task.WhenAll) 정지 | 롤백 없음 | 실패/취소 시 관련 축 Safe 롤백 |
| 백그라운드 pending(T0·선행검사) | 타임아웃 후 방치 | 정지 시 취소·완전종료 확인, 미종료 시 축 강제정지/Alarm |
| stop-time catch{} | 예외 은폐(AGENTS.md 위반) | 최소 로그 + 안전관련 실패 Alarm 승격 |

---

## 10. 확정 필요 파라미터

- FacingClearanceX(마주보기 X 거리, 예 150mm)
- PickerY SafePosition/ForwardThreshold(side별), ZDownThreshold(side별)
- CorrectionWindowXYT(Z 하강 중 정렬보정 ±한계 — 실제 최대 오프셋 기준)
- InputPickZone / OutputPlaceZone X 범위(검사존 락)
- Supervisor 루프 주기(기본 10ms), Wait 타임아웃
- 전부 설정(JSON pretty UTF-8, JsonPrettySerializer) + UI 등록.
