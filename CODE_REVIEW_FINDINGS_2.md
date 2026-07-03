# CODE_REVIEW_FINDINGS.md — 인터락/오토시퀀스 수정 재검토

Code 수정 완료본 재검토 결과(실제 Windows 파일 기준). 검토 4개 클러스터: ①§7 선행검사 알람, ②인터락규칙/MotionGuard, ③SharedRailX/축, ④정지·재개·Abort(INV-6/7).

판정: 🟢정상 · 🟠확인/개선 · 🔴결함.

---

## 요약 (한 줄)
**이번 알람(§7 InputVisionX 선행검사)은 정확히 고쳐졌다. 그러나 사용자가 근본으로 지적한 "인터락 해제 전 물리 안전 후퇴(INV-6)"는 아직 배선되지 않았고(헬퍼가 데드코드), 인터락 리팩터에서 Front/Rear 비대칭 등 회귀 후보가 있다.**

---

## 🔴 최우선 결함

### CRIT-1. INV-6 미구현 — `EnsureSelfSafeAsync`가 데드코드
- 근거: 정의는 `PickerSequenceBase.cs:1345`, **호출부 0건**(grep 확인).
- `PickerProcessSequence.Abort()`(:37-75), `PickerPlaceSequence.Abort()`(:53-70), PickUp/Bottom/Side 자식 Abort 모두 phase 리스·신호 **해제만** 하고 Y/Z를 Avoid로 후퇴시키지 않음.
- 위험: die 들고 Y 전진·Z 하강 상태에서 Abort → 픽커가 전진 위치 그대로인데 phase lease가 풀림 → 상대 픽커가 진입하면 마주보기 충돌. **처음부터 지적한 문제가 그대로 남음.**
- 조치: Abort / `ExecuteAsync`·`RunAsync` finally / CycleStop 경로에서 리스 해제 **전** `EnsureSelfSafeAsync` 호출(Z상승→Y후퇴 완료 후 해제).

## 🟠 높은 위험 / 확인 필요

### HIGH-2. Cycle Stop 후 물리 안전 정규화 없음
- `RequestCycleStopSequenceAsync`(MachineController.cs:6420~)는 플래그만, 경계 `StopIfCycleStopRequested`(MachineSequenceContext.cs:81-90)는 예외 throw만. 정지 후 Y/Z Avoid 정규화 없음 → 멈춘 자리 방치.

### HIGH-3. 시작 우선순위(Input→Front→Rear→Output) 미구현
- `AutoSequenceCoordinator.RunAsync`(:86-87)가 여전히 4유닛 **무순서 병렬 Task.Run**. StartupSequencer/배리어 없음. 재시작 시 동시 첫 이동 시도.

### HIGH-4a. 인터락 회귀 후보 — Rear PickerY 매뉴얼/홈이 Front 대비 검사 누락
- `CanManualRearPickerY`(PickerRearInterlockRules.cs:311-350)는 Front(`CanManualFrontPickerY`:270)에 있는 **InputExpandingZ 검사가 없음**.
- `CanHomeRearPickerY`(:480-502)는 Z Avoid만, Front(`:494-532`)의 InputStage/OutputStage 검사 없음.
- 위험: Rear가 매뉴얼/홈으로 Y 전진 시 InputStage Z 간섭 방어가 Front보다 약함. 의도된 비대칭인지 확인 필요.

### HIGH-4b. zone 태그 없는 시퀀스 AxisMove가 매뉴얼 전제검사 우회
- `ResolveEffectiveMoveKind`(MotionGuardService.cs:298-312): Auto/Manual SequenceProcess의 `AxisMove`→`AxisTeachingMove` 승격 → `CanAuto*` 경로(zone 기반)로 라우팅. `CanAuto*`는 `CanManual*`의 FeederY/VisionX/ExpanderZ 전제 검사를 안 함.
- 위험: 시퀀스가 `PickerZone=` 태그를 안 붙이면 zone=Unknown → 전제 검사 생략된 채 통과 가능. **시퀀스 호출부가 항상 zone 태그를 부여하는지 확인 필요.**

### HIGH-5a. Front↔Rear 픽커 X쌍이 충돌 감시에서 제외 (기구 확인)
- 근거: `IsFrontRearPickerPair`(SharedRailXConfig.cs:103-107)로 제외. 감시 쌍은 Input↔Front, Input↔Rear, Output↔Front, Output↔Rear **4개뿐**.
- 판단: 두 픽커가 **별도 평행 X 레일**이고 서로 X로 충돌 불가하면 정상(둘의 충돌은 Y-마주보기이며 PickerZone facing-Y가 담당 → 단 HIGH-4a 비대칭 주의). **기구 배치로 확인 요망.** 같은 레일이면 치명적 hole.

### HIGH-5b. work-area 조건부 충돌쌍 동적 바이패스
- `IsInputVisionPickerPairRequired`(MotionGuardService.cs:387~)가 픽커가 Bottom work-area면 InputVision↔Picker 쌍을 **pre-move + 20ms 실시간 guard 양쪽에서 제외**.
- 위험: work-area 판정이 틀리면 InputVision↔Picker 충돌 무방비. `catch{return true}`로 판정 실패 시엔 안전측(쌍 유지)이나, **정상 오판 시 무방비**. 안전이 work-area 인터락 정확성에 전적으로 의존.

## 🟢 정상 확인

- **§7 현재 알람 수정 정확**: FIX-A(대기 조건을 SharedRailX+MotionGuard dry-run 이중 소스로 통일, InputDieVisionPrepareSequence.cs:2147-2154), FIX-B(선행검사 -11을 치명화 않고 양보, :2164-2179), FIX-C(-11 재대기 루프, :2035-2077), FIX-D(PickerProcess 중앙 게이트+공용레일 dry-run, PickerProcessSequence.cs:662-799). **원래 버그(선행검사 -11이 RearPicker 본 공정을 죽이던 것) 제거됨.**
- MotionGuard 바이패스 설계 견고: `AsyncLocal` + `using`, 누수/스레드 오염 없음. JOG STEP 바이패스는 hole 아님(shared 축은 SharedRailX pre-validate + 20ms guard 선행).
- `MotionGuardMoveIntent` null-safe(항상 non-null, enum 기본 Unknown). Front/Rear 축 copy-paste 오배치 없음. Rear X 이동 시 Front Y까지 검사(공유레일 정상).

## 🟠 중/저 (개선 권장)

- **INV-7 부분 구현**: `VerifySafeStartConfig`가 Process CheckUnit(PickerProcessSequence.cs:262-276)에서만 확인·실패 시 정지(자동 정규화 없음, 수동 재시작+Ready 선행 전제). Place/PickUp 독립 진입엔 없음. Process 선행 정상 흐름은 커버.
- **실시간 감시자 상시화 미구현**: SharedRail 20ms guard는 여전히 **명령된 이동 중에만** 동작(상시 아님). 설계상 supervisor(별도) 미착수.
- **AGENTS `catch{}` 위반**: 안전판정 경로 빈 catch 다수(MotionGuardService.cs:346,399; PickerZoneInterlockRules.cs:1637,1762; 자식 Abort들 PickerPlaceSequence.cs:64-66 등). 안전측 폴백이 많으나 로그 없어 진단성 저하 + 규칙 위반. 로그/알람 추가 권장.
- 선행검사 양보 대기 중 InputStageArea 점유 유지(데드락 아님, 지연 가능), 허가 확인~이동 사이 좁은 TOCTOU(VISIONX-NOT-AVOID로 fail-safe 수렴).

---

## 다음 액션 (권장 순서)
1. **CRIT-1**: `EnsureSelfSafeAsync`를 Abort/finally/CycleStop에 배선(INV-6). — 최우선 안전.
2. **HIGH-5a / HIGH-4a**: 기구 배치 확인(두 픽커 X 충돌 가능성) + Rear Y 인터락 비대칭 정정.
3. **HIGH-4b**: 시퀀스 이동이 항상 `PickerZone=` 태그를 붙이는지 호출부 점검.
4. **HIGH-2 / HIGH-3**: Cycle Stop 정규화, 시작 우선순위.
5. catch{} 로그 보강.

각 수정 후 별도 OutDir 빌드 + `perl tools/verify_all.pl` + 시뮬 시나리오(INTERLOCK_VERIFICATION_CHECKLIST.md).
