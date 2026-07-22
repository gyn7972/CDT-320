# 체크리스트 — Input Vision 촬영 실행자 (input-vision-prefetch-executor.md)

작성일: 2026-07-22

## 계획 요약 (사전 숙지 결과 반영 — 프롬프트 대비 편차는 레포트에 명시)

숙지에서 확정된 기존 자산 (재사용, 무수정 원칙):
- **실행 엔진**: `InputCameraPreInspectionCoordinator.EnsureStarted(context, side, options, ct, reason)`
  — 사이드별 백그라운드 Task로 `InputCameraMarkInspectionSequence`(촬영+옵셋+Grant) 실행.
  중복 기동 방지·허가 존재 시 미기동·취소·CycleStop 처리 완비.
- **lease**: InputCameraMarkInspectionSequence가 InputStageArea lease 자체 취득 (프롬프트 R2는
  기존 코드로 충족 — 러너에서 lease 처리 불필요).
- **소비**: 픽업 `TryLoadInputCameraMarkInspectionPermission`(538행 TryConsume + VisionX-Avoid 롤백)
  및 프로세스 `WaitForPermissionOrCompletionAsync`(883행) 이미 존재 (프롬프트 R4는 기존 코드로
  충족 — **픽업 시퀀스 무수정**).
- **기존 트리거의 한계(=이번 작업의 갭)**: `StartSafeInputCameraPreInspections...`가
  PickUpComplete(1008행)/PlaceComplete(2138행, 자기 사이드만) **경계에서 1회성** 판정 —
  그 순간 조건 미충족이면 다음 경계까지 기회 상실. Front=Bottom ∥ Rear=Place 동안
  재시도가 없다.

구현 설계:
1. VisionConfig에 플래그 3종 (기본 OFF — OFF면 기존 흐름 100% 동일).
2. 게이트 추출: PickerProcessSequence의 안전 판정 5+1종을 정적 클래스로 이동(기계적 이동),
   프로세스는 위임 호출로 동작 무변경. 러너와 공용.
3. `InputVisionPrefetchRunner` 신설: Front→Rear 순회 주기 재시도 루프.
   조건 판정(순수 함수) → 게이트 → `EnsureStarted`. 시작 성공 시 홀드, 취소 안전.
4. `AutoSequenceCoordinator.RunAsync`에서 Auto+플래그+픽커 유닛 활성 시 러너 Task 기동,
   종료 시 취소·합류 (waferMonitorTask 배선 패턴 대칭).

## 구현
- [x] C1. VisionConfig 플래그: `UseInputVisionPrefetch`(기본 false),
  `InputVisionPrefetchIdlePollMs`(기본 200), `InputVisionPrefetchFailureHoldMs`(기본 5000)
  — DataMember, OnDeserialized/Ensure 보정, 직렬화 하위호환
- [x] C2. 게이트 추출 `Sequencing\Picker\InputCameraPreInspectionStartGate.cs` (internal static):
  `CanStart(MachineSequenceContext, out string blockReason)` — 이동 대상:
  CanStartSafeInputCameraPreInspection(1192) / IsPickerBlockingInputCameraPreInspection(1251) /
  IsSharedRailContendedForInputVisionStart(1294) / IsPickerInputZoneMotionRiskForProcess(1439) /
  UpdateNearestInputVisionCandidate(2360) / BuildNearestInputVisionTargetDetail(1420).
  PickerProcessSequence는 위임 래퍼로 축소(로그·시그니처·동작 무변경)
- [x] C3. 러너 `Sequencing\Picker\InputVisionPrefetchRunner.cs` (internal static):
  `RunAsync(MachineSequenceContext, bool frontActive, bool rearActive, CancellationToken)` —
  루프: 사이드 순회(Front→Rear, D2), 조건: ①플래그 ON ②Bus "InputStageReady"
  ③`MaterialStateService.HasReadyInputStagePickTarget()` ④`!HasPermission(side)`
  ⑤드레인 아님(`Context.WaferCompletion.Enabled && IsDrainRequested` 읽기 전용 —
  Observe는 WaferCompletion 모니터 태스크가 주기 수행) ⑥게이트 CanStart.
  통과 시 `EnsureStarted`(옵션: RunMode=Auto, InputCameraPreInspectionMode=true,
  RequireInputCameraMarkInspectionPermission=false, PickerNo=0, RestrictToPickerNo=0).
  시작 성공 시 FailureHoldMs 홀드, 아니면 IdlePollMs 대기. OperationCanceled 안전 종료.
  조건 판정 중 "순수 판정"은 `ShouldAttemptPrefetch(...)` 정적 함수로 분리(하네스 검증용)
- [x] C4. AutoSequenceCoordinator.RunAsync 배선: `_options.Mode==Auto` + VisionConfig 플래그 ON +
  픽커 유닛 1개 이상 활성 시 러너 Task 기동(waferMonitor 패턴), 종료 경로에서 취소+합류.
  ResetCoordinatorRunState의 기존 Clear(280-281행) 유지
- [x] C5. csproj `<Compile Include>` 2건 등록 (Gate/Runner)
- [x] C6. 무수정 확인: PickerPickUpSequence·InputCameraPreInspectionCoordinator·
  InputCameraMarkInspectionSequence·기존 경계 트리거(1008/2138행) 코드 무변경

## 검증
- [x] V1. 빌드 통과(`/t:Build`, OutDir 고정), 신규 경고 0, csproj 등록 확인
- [x] V2. OFF 회귀: 플래그 기본 false → 러너 미기동(배선 조건 코드 리뷰) +
  게이트 위임 동작 동등성(이동 전후 diff 대조 — 기계적 이동 확인)
- [x] V3. 하네스: ①`ShouldAttemptPrefetch` 순수 판정 조합(플래그/신호/허가/드레인별 기동·비기동)
  ②`InputCameraPickUpPermissionStore` Grant→HasPermission→TryConsume 왕복(깊은 복제·소진 확인)
  ③SequenceSignalBus Set/IsSet 신호 판정
- [x] V4. 러너 취소/드레인 경로 리뷰: ct 취소 즉시 종료, 드레인 중 신규 기동 금지,
  실행 중 태스크 정리는 기존 Coordinator.Clear 경로(AutoSequenceCoordinator 종료·Abort) 커버 확인
- [x] V5. 오버랩 성립 논증: Front=Bottom ∥ Rear=Place 시나리오에서 러너 조건이 충족되는
  근거(픽커 Input 존 밖 → 게이트 통과, lease 미점유) 코드 인용 정리
- [x] V6. 보고: 프롬프트 대비 편차(R2/R4 기존 충족, 실행 엔진 대체), D7(허가 무효화 커버리지),
  잔여 리스크

## 검증 결과 기록 (2026-07-22)
- V1: 빌드 EXIT=0. 경고 5건 전부 기존(미수정 파일: AxisInitializePlan CS0162×3,
  OutputFeederInterlockRules CS0168, MachineController CS0162). 신규 파일 2종 경고 0.
- V2: 러너 기동 조건 = Auto && (front||rear 활성) && `InputVisionPrefetchRunner.IsEnabled`
  (VisionConfig.UseInputVisionPrefetch, 기본 false) — OFF면 Task 자체가 생성되지 않음.
  게이트 추출은 코드 원문 이동(공개 정적화 + Context 파라미터화)이며, 유일한 동작 차이는
  CanStart 예외 경로의 blockReason 문자열에서 targetSide/reason 표기가 빠진 것(로그 문자열 한정).
- V3: PrefetchHarness 15체크 ALL PASS — ①ShouldAttemptPrefetch 7조합(전건 충족 기동,
  플래그/신호/타겟/허가/드레인 각각 저지 + 드레인 사유 우선) ②PermissionStore Grant→
  HasPermission→TryConsume 왕복(필드 보존 + 깊은 복제 참조 분리 + 소진 후 재소비 불가)
  ③SignalBus Set/IsSet.
- V4: 러너는 OperationCanceledException에서 로그 후 정상 종료, 코디네이터 finally에서
  Cancel+await+Dispose(StopInputVisionPrefetchRunnerAsync — waferMonitor 종료 패턴 대칭).
  드레인은 WaferCompletion.Enabled&&IsDrainRequested 읽기 전용 판정으로 신규 기동 금지.
  실행 중 선행검사 Task의 정리는 기존 InputCameraPreInspectionCoordinator.Clear 경로
  (AutoSequenceCoordinator.ResetCoordinatorRunState 280-281, PickerProcessSequence.Abort 52,
  Front/Rear 유닛 정리) 무수정 유지로 커버.
- V5: Front=Bottom ∥ Rear=Place 시 — 두 픽커 모두 Input 존 밖(Zone 판정 busy=false),
  InputStageArea lease 미점유(픽업만 점유), 허가 미발행이면 게이트 CanStart 통과 →
  EnsureStarted → InputCameraMarkInspectionSequence가 lease 취득 후 촬영 → Grant.
  이후 어느 쪽이든 PickUp 진입 시 WaitForPermissionOrCompletionAsync가 즉시
  PermissionReady로 통과(대기 시간 소멸 = 오버랩 성립).
- V6: 레포트에 기재(편차 3건: 실행 엔진=기존 코디네이터 재사용 / R2 lease·R4 소비 기존
  충족으로 미구현 / 소비 타임아웃 폴백(D3)은 기존 Wait 구조가 대체).
