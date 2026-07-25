# 체크리스트 — 비전 회피 위치 최소화 (vision-minimal-retreat.md)

작성일: 2026-07-23  /  브랜치: master (사용자 지시로 master 직접 작업 — 이전 작업 관례)

## 계획 요약 (병렬 조사 결과 반영)
- 수식 검증(닫힌 형): bound_i = HomeClearance − 피커부호×장애물 − (Safety+Extra),
  제약 비전부호×비전위치 ≤ bound_i → boundMin = Min(bound_i), target = 비전부호×boundMin.
  Input(88.5,+1,−1,10,40, 최소X=620) → 658.5 ✓ / Output(510,−1,+1,10,40, 최대X=540) → 80 ✓.
- 실측 파일(D:\CDT-320\Config\shared_rail_x.json) 값 = 프롬프트 표와 일치 확인.
- 프롬프트 라인 좌표는 일부 stale — 실좌표: PickerPickUpSequence.MoveInputVisionToAvoidForPickerMoveAsync
  1084~1192, PickerPlaceSequence 회피 결정부 1715~1767, InputCameraMarkInspectionSequence
  .MoveInputVisionXToAvoidAsync 386~485(실호출 220행, PreparedItems는 지역변수 → 파라미터 전달 필요),
  OutputPostPlaceInspectionQueue.MoveVisionXToAvoidAsync 1393~1420(호출 654행).
- 직렬화: DataContractJsonSerializer는 역직렬화 시 생성자/이니셜라이저 미실행 → Document 신설
  필드는 double?(Order 5/6) + Store.Normalize에서 !HasValue||<0 → 40.0 (기존 SafetyDistance double? 관례).
- ResolveEffectiveConfig(Service 613~617)가 스칼라 2개만 복사하는 새 config 생성 — Extra 2종도 복사 추가.
- 게이트: 픽업계열 = Options.RunMode==Auto && (IsCoordinatedPickUpTransferMotionMode || ==FastConti),
  플레이스계열 = Options.RunMode==Auto && IsCoordinatedPlaceMotionMode. 후검사 큐는 Options 부재 →
  HasPickerContext && PickerSide별 Config.Place.MotionMode Conti (수동 Place는 큐 등록 자체가 억제됨,
  복원 경로 HasPickerContext=false → 기존 전체 Avoid).
- 촬영 전 Feeder 정리용 전체 Avoid 2곳(PickerPlaceSequence 1676, 큐 1330)은 FeederY 경로 확보
  목적이므로 최소 회피 대상 아님 — 유지(레포트 명시).
- 배치 전체 place PickerX 목표 보관 필드는 없음 → R4-1 planned는 현재 아이템 _targetPickerX +
  서비스 자동 포함(Actual/Command)로 구성 (프롬프트 허용 폴백 — 레포트 명시).
- MarkInspection 462행 IsVisionXInAvoidPosition() 검사는 전체 Avoid 기준 → 최소 회피 경로 분기 필요.
- 하네스: 서비스에 순수 정적 계산 코어(public static)를 분리해 실측값 재현 검증. 인스턴스 경로는
  구성 가능 시 통합 검증, 불가 시 빌드+로그로 보고.

## 구현
- [ ] C1. R1 설정 신설: SharedRailXConfig에 InputVisionRetreatExtraClearance/
  OutputVisionRetreatExtraClearance(=40.0, XML주석에 R5 원칙 명시) 추가.
  Document에 double? Order 5/6, Store.Normalize(!HasValue||<0→40), ToConfig/FromConfig/
  CreateDefaultDocument 반영, Service.ResolveEffectiveConfig 복사 2종 추가.
- [ ] C2. R2 신설 메서드: SharedRailXMotionService.TryResolveMinimalVisionRetreatTarget(
  visionAxis, fullAvoid, planned, extraClearance, out retreatTarget, out detail) —
  기존 함수 무수정. 수집(페어/Actual/Command/planned) 공용 로직, 닫힌 수식(정적 코어),
  혼합 비전부호 시 false 폴백, fullAvoid 초과 클램프, 소프트리밋 클램프,
  IsVisionRetreatTargetSafe(extra) 재검증 belt-and-braces, detail F6.
- [ ] C3. R3-1 PickerPickUpSequence.MoveInputVisionToAvoidForPickerMoveAsync:
  Auto+Conti계열 게이트 시 신설 메서드(Extra=Config.InputVisionRetreatExtraClearance),
  미충족 시 기존(-0.1/1.0) 그대로. 로그 mode=minimal/legacy + 계산 근거.
- [ ] C4. R3-2 InputCameraMarkInspectionSequence.MoveInputVisionXToAvoidAsync:
  시그니처에 preparedItems 전달(220행 호출부), Auto+Conti계열 게이트 시 근사 planned
  (PickTarget.TargetX + TryResolveInputVisionToPickerOffsets X) → 신설 메서드, 근사 실패 시
  전체 Avoid. 최소 회피 경로에서 462행 검사를 목표 기준으로 분기. F3 경로 보존.
  근사 안전성 주석(이후 픽업 시퀀스가 정확 좌표로 재계산, Extra+인터락 유지).
- [ ] C5. R4-1 PickerPlaceSequence 회피 결정부(1715~): Auto+ContiSegmentedPlace 게이트 시
  신설 메서드(Extra=OutputVisionRetreatExtraClearance, planned=현재 _targetPickerX),
  미충족 시 기존 그대로. 1676행 Feeder 정리용 전체 Avoid는 무변경.
- [ ] C6. R4-2 OutputPostPlaceInspectionQueue.MoveVisionXToAvoidAsync: 게이트
  (HasPickerContext && PickerSide별 Place.MotionMode==ContiSegmentedPlace) 시 신설 메서드
  (planned 생략)로 목표 계산 후 큐 내부 MoveStageAxisAndVerifyAsync(VisionX, target)로 이동,
  실패코드 OUT-POST-INSPECT-VISION-AVOID/병렬 배리어 구조 보존. 게이트 미충족 시 기존
  전체 Avoid. 1330행(촬영 전)은 무변경.
- [ ] C7. R5 준수: 인터락/검증 코드(VerifySingleAxisMove, SharedRailXCollisionValidator,
  MotionGuardRuntime, WaitInput/OutputVisionXSharedRailClearAsync, IsVisionRetreatTargetSafe
  기존 호출부) diff 0건 확인.
- [ ] C8. 솔루션 빌드 통과.

## 검증
- [x] V1. 빌드 통과 (장비 가동 중이라 별도 OutDir로 검증 빌드 — D:\CDT-320 무접촉).
- [x] V2. 단위 계산 하네스 13/13 PASS (실측 설정값):
  Input {680,660,640,620} Extra=40 → 658.5 / Extra=0 → 698.5.
  Output {480,500,520,540} Extra=40 → 80 (전체 Avoid 폴백 아님, 기존 함수는 false 폴백 대비 확인) / Extra=0 → 40.
  페어 없음 → false+fullAvoid. 소프트리밋 클램프(658.5→650). fullAvoid 초과 클램프.
- [x] V4. 인터락 diff 0건 (R5 준수 — 신설 메서드 XML 주석의 이름 언급 1건뿐).
- [ ] **V3. 적대적 검증(3렌즈)에서 차단급 발견 — 구현 반복으로 해소 불가(스펙 전제 충돌), 사용자 결정 필요**:
  ① [critical] MotionGuard 존 진입 인터락 VerifyInput/OutputVisionXAtAvoidOrBelowZero
     (PickerFrontInterlockRules.cs:839/927, PickerRearInterlockRules.cs:480/568)가 피커 X
     존 진입 조건을 "비전=전체 Avoid 정위치 OR ≤0"으로 제한 — 최소 회피 좌표(658.5/80)는
     두 분기 모두 불통 → 피커 X 이동 차단. 기존 -0.1 상한이 사실 이 인터락을 만족시키는 값이었음.
     스펙 전제(진입 게이트=SharedRailX 간격≥Safety만)가 코드베이스와 불일치.
  ② [critical] 픽업 허가 소비부 TryLoadInputCameraMarkInspectionPermission
     (PickerPickUpSequence.cs:623)이 IsVisionXInAvoidPosition 정위치 요구 → 매 배치 Fail 루프.
  ③ [major] IsInputVisionXSafeForContinuousPick(PickerPickUpSequence.cs:4373) entryLimit=0
     하드코딩 → 최소 회피 좌표에서 Conti 이송 상시 부적격 → default 폴백 후 ①에 걸림.
  ④ [major] WaferCompletionRunCoordinator.IsOutputVisionAvoidAndStopped(:281)가 전체 Avoid
     정위치 요구 → 마지막 BIN 후 최소 회피 주차 시 Stop After Drain 영구 대기.
  ⑤ [minor] 큐 게이트: 복원 경로(MaterialPendingRestore)도 HasPickerContext=true 가능(주석 오류),
     RunMode==Auto 상당 판정 부재.
  ⑥ [info] 이미 더 후퇴해 있어도 경계 좌표로 진입 방향 전진 이동(다음 촬영 단축 목적이면 의도 부합,
     아니면 '유지' 처리 필요 — 사용자 확인).
- [ ] V5. 레포트 — 차단 사항과 선택지 제시로 대체 (아래 레포트 참조).

## 상태
- 구현/수식/설정/하네스는 완성 상태로 feature/vision-minimal-retreat 브랜치에 보존.
- master는 이 작업 이전 상태로 복원 (장비 가동 중 — 배포 안전 유지).
- 재개 조건: 인터락(존 진입 룰) 및 정위치 소비자 3곳 처리 방향에 대한 사용자 결정.
