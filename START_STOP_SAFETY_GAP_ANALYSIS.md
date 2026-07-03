# START_STOP_SAFETY_GAP_ANALYSIS.md

CDT-320 듀얼 픽커 핸들러의 **시작(START) / 정지(STOP) / 재개(RESUME)** 경로에서 인터락·준비상태 점검이 부족한 지점 분석. 읽기 전용 분석(코드 미수정). 인터락 실시간 감시자 구현은 별도(Codex) 진행 중이며, 이 문서는 그 작업의 입력 자료다.

기준 진입점: `MachineController.StartSequenceAsync`(auto 시작) / `StopAsync`·`CycleStopAsync`(정지) / `PickerProcessSequence.ResolveNextProcessStepFromMaterial`(재개 스텝 결정). 라인번호는 분석 시점 기준 근사값.

---

## 핵심 결론 (한 줄)

시작·정지·재개 **모두 "물리적 안전 배치(양 픽커 Y 후퇴·Z 업·X 비대면)"를 실제 좌표로 검증/정규화하지 않고**, 초기화 플래그·자재상태·소프트 위상만으로 이동을 발행한다. "레디=안전"이라는 암묵 가정과 fresh/resume 미구분이 최초시작·재개 사고의 근본 원인이다.

---

## A. 최우선 위험 (충돌 직결)

### [A-1] Abort/finally가 인터락 리스만 풀고 물리 후퇴는 안 함 — 가장 위험
`PickerProcessSequence.Abort()`(PickerProcessSequence.cs:34-72)는 `ResetPickerPhaseSignals` + `ReleasePickerProcessPhase` + 하위 Abort + `CurrentStep=Complete`만 수행하고 **축을 Avoid로 후퇴시키지 않는다.** `PickerPlaceSequence.Abort/finally`(53-70,119-128)도 리소스·batch만 해제.
- 위험: phase 리스가 풀리면 상대 픽커 인터락 게이트가 열리는데, 정작 abort된 픽커는 **Y 돌출·X 마주보기 위치에 물리적으로 남아 있어** 게이트 개방 순간 충돌 조건 성립.
- 보강: 리스 해제 **전에** 자기 픽커 Y-Avoid/Z-Avoid 물리 후퇴 완료, 또는 후퇴 완료 전에는 상대 게이트를 열지 않도록 순서 보장.

### [A-2] Cycle Stop이 축 정지 명령 없는 순수 협조적 플래그 정지
`StopIfCycleStopRequested`(MachineSequenceContext.cs:81-90)는 경계에서 `SequenceStopException`만 throw. 자동 사이클 정지 시 `StopAsync`/`CycleStopAsync`는 `RequestCycleStop`만 부르고 return하며 축 `Stop()`은 수동/조그일 때만(MachineController.cs:5496-5518) 호출. cycle stop은 CTS를 취소하지 않아 **진행 중 이동은 목표까지 완주**한다.
- 위험: "Z 하강 중"·"Y 전진 중" 정지해도 그 위험 위치까지 도달 후 멈추고, 이후 어떤 코드도 Y/Z/X를 안전 위치로 되돌리지 않음. 공유 레일 마주보기 구간에 방치 가능.
- 보강: stop 접수→시퀀스 이탈 시점에 각 픽커 Avoid 정규화 후처리 추가.

### [A-3] Resume가 물리 위치가 아닌 자재/검사 상태로만 재개 스텝 결정
`ResolveNextProcessStepFromMaterial`(PickerProcessSequence.cs:195-328)은 다이 유무·Bottom/Side/최종판정 record만 보고 Bottom(:284)/Side(:311)/Place(:320) 진입. **픽커가 물리적으로 어디 멈춰 있는지 조회 안 함.** Place 재개만 `_forceSafeYBeforePlaceResume`(:321)로 사전 Safe-Y가 있고, **Bottom/Side 재개는 대칭 보호가 없다**(A-4 참조).
- 위험: Y 전진·비정상 X에서 정지 후 재개 시 안전 복귀 없이 곧바로 검사용 X 이동 발행 → 공유 레일 충돌.
- 보강: 재개 진입 시 실제 좌표로 SAFE 검증, 아니면 이동 재발행 전 강제 Avoid 정규화. `ForceSafeYBeforeFirstPlaceMove` 개념을 Bottom/Side 재개로 확장.

### [A-4] 재개 사전 Safe-Y 보호가 경로별 비대칭
Place: `MovePickerToSafeYBeforeOutputStageReadyWaitAsync`(PickerPlaceSequence.cs:536-586)로 Z-Avoid+Y-Avoid 선행. 강제-Bottom 재개: `MoveOwnPickerYToAvoidBeforeForcedBottomResumeAsync`(BottomAndSide:456)는 `ForceBottomInspectionBeforeSideResume==true`일 때만(:137). **Side 단독 재개(RunSideInspection 직접 진입) 등에는 사전 후퇴가 빠짐.** `MoveBottomTargetAsync`(:409)는 PickerX·Y를 묶어 이동하므로 Y 전진 상태로 X 레일 횡단 위험.
- 보강: "재개 첫 이동 전 SafeY/SafeZ" 공통 헬퍼를 Bottom/Side/Place 모든 재개에 동일 적용.

---

## B. 시작(START) 준비상태 갭

### [B-1] 첫 물리 동작 전 안전 시작 배치 미검증
`StartSequenceAsync`(MachineController.cs:6215-6270) 게이트 = `EnsureMachineInitializedForRun`+`EnsureReticleAvoidForAutoStart`+`EnsureCalibrationReadyForAutoStart`뿐. `CheckUnit`의 `BuildRequiredPickerAxesReason`(PickerSequenceBase.cs:2079-2110)은 Servo/Alarm만 보고 `ActualPosition`은 실패 메시지용, **IsHomeDone·안전존 위치 미검증.** → "레디=안전" 암묵 가정.
- 보강: START 직전 양 픽커 Y=Avoid·전체 Z=Avoid·X 비대면을 라이브 좌표로 검증, 어긋나면 첫 이동 전 강제 Safe 복귀.

### [B-2] fresh/resume 구분 플래그 자체가 없음
`SequenceRunOptions`에 ColdStart/Resume 구분 없음. 전원 재기동 후 첫 START라도 다이 상태가 남아 있으면 resume 경로로 취급되어 축 위치와 무관하게 검사·플레이스 이동 시작(A-3과 결합).
- 보강: 콜드 스타트 플래그 도입 → 첫 START는 자재 상태와 무관하게 전 픽커 Safe 정렬 후에만 재질 기반 재개 판정.

### [B-3] PickerPhaseCoordinator가 물리 위치 모른 채 항상 Idle 초기화
매 START마다 새 Coordinator 생성, front/rear를 무조건 Idle로 시작(PickerPhaseCoordinator.cs:8-9). 상호배제는 "상대 Idle이면 허용"(:137-138)이라 **한 픽커가 물리적으로 전진해 있어도 상대 진입 허용.**
- 보강: START 시 실제 위치로 초기 Phase(또는 Unknown) 세팅, 안전 확인 전 진입 보류.

### [B-4] Developer 모드 Ready 복원이 물리 재확인 없이 이뤄짐
`ApplyStartupMachineRuntimeState`(:362-423)는 저장 JSON의 `IsMachineInitialized`만 보고 Ready 승격. `AreAllAxesInitializedAndReady`(:1298)도 Servo/Alarm/HomeDone/Moving만 보고 **좌표 미검증.** → 축이 임의 위치여도 START 가능.
- 보강: Ready 승격 조건에 픽커 Safe 좌표 확인 추가, Developer 복원 시 홈 재확인 요구.

### [B-5] START 게이트에 도어/EMO/Vision·Sim 연결 준비 확인 없음
init/reticle/calibration만 검사. 도어·EMO·안전 인터록, Vision(5100/5101/5103)·Sim(7001) 연결, Feeder/Stage 준비 미검증. Vision 연결 확인은 `AlignWaferAsync`(:1972) 내부라 첫 동작 전 보장 안 됨.
- 보강: START 전 종합 게이트(도어/EMO/인터록 + Vision/Sim heartbeat).

---

## C. 정지/타이밍 부수 갭

### [C-1] E-Stop vs Cycle Stop 최종 축 상태 상이·예측 불가
E-Stop(:1776-1819)은 `EStop()`+전축 `ServoOff()` 즉시 정지(서보 오프 시 Z 중력낙하 검토 필요). Cycle Stop은 정지 명령 없이 완주 위치 방치(A-2). 어느 쪽도 듀얼 픽커 상대 안전 배치 보장 안 함.

### [C-2] 병렬 이동(Task.WhenAll) 중 정지 시 비대칭 상태 방치
Place의 StageY+Picker XYT 동시 이동(PickerPlaceSequence.cs:936-981), 그룹 병렬 이동(PickerSequenceBase.cs:565-605)은 실패/취소 시 롤백 없음 → OutputStage 위치·Picker Y 전진 불일치 방치.

### [C-3] 정지 후에도 백그라운드 pending 작업이 이동 발행 가능
`StartInputCameraPreInspectionsAfterPickUpComplete`(fire-and-forget, :500-531). `AwaitPendingAfterAbort/CycleStopAsync`(AutoSequenceCoordinator.cs:212-291)는 5초 타임아웃 후 "READY 차단 회피"로 대기 포기 return(:258,:344) → **살아있는 백그라운드 이동을 정지 완료로 오인식.**
- 보강: 타임아웃 시 return 대신 축 강제 정지/알람 확정 + 백그라운드 핸들 완전 종료 확인.

### [C-4] 안전측이 아닌 낙관적 처리(soft-check / catch{})
- `AreAllAxesInitializedAndReady`의 `try{UpdateStatus();}catch{}`(:1308) — 갱신 실패를 이전 낙관 상태로 통과.
- `HasCriticalActiveAlarm`/`IsCriticalMotionOrInterlockAlarm`(AutoSequenceCoordinator.cs:379,403) `catch{return false;}` — 알람 판정 실패를 "위험 없음"으로.
- stop-time `catch{}` 다수(MachineController.cs:1763,5531,7820,7837,7854,7867; PickerPlaceSequence.cs:64-66) — AGENTS.md `catch{}` 금지 위반, 정지 이상 은폐.
- 보강: 실패 시 안전측(false) 처리 + 로그/알람 승격.

---

## D. 권장 우선순위

1. **A-1 (Abort 물리 후퇴)** — 인터락 해제 전 Safe 후퇴 순서 보장.
2. **A-2 (Cycle Stop 정규화)** — 정지 후 Avoid 정규화 후처리.
3. **A-3/A-4 (Resume 물리 재검증 + 대칭 Safe-Y)**.
4. **B-1/B-2 (첫 이동 전 좌표 안전검증 + cold/resume 구분)**.
5. **B-3 (Phase 물리 초기화)**, 이후 B-4/B-5, C-계열.

공통 대응: **시작·정지·Abort·재개 네 경로 모두에 "인터락 해제 전 물리 SAFE-STATE 정규화" 및 "이동 재발행 전 실제 좌표 기반 안전 검증"을 삽입.** 이는 별도 진행 중인 실시간 충돌 감시자의 `ReconcileSafeState()`(재개=신규시작 취급)와 정확히 맞물린다.
