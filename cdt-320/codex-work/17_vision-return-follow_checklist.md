# 체크리스트 — 피커 퇴장∥비전 복귀 팔로잉 (vision-return-follow.md)

작성일: 2026-07-24  /  상태: **구현 완료 — 빌드/하네스(22/22) 검증 통과**

## 선행 의존 점검
- [x] FollowMoveAsync 존재 + timeoutMs 인자화 완료 (AjinAxis.cs:501, effectiveTimeoutMs).
- [x] Extra 설정 2종 + VisionFollowEntryTimeoutMs(기본 15000) — master 반영 완료(e5ae3058).
- [x] 인터락 제3 분기(페어 간격 ≥ SafetyDistance, Extra 미포함) — 비전 진입측
  (VerifyPickerInputZoneClearForInputVisionX / VerifyPickerOutputZoneClearForOutputCameraX)
  반영 완료 → 차단 사유 해소.

## 인터락 통과 체인 (R4 증명 — 코드 확인 완료)
- 정지 상태 명령: FollowMoveAsync(:662) → BaseAxis.MoveAbsoluteAsync(:546)
  VerifyMotionGuard → BaseAxis.MotionGuard(=MachineController.VerifyAxisMotionGuard:618)
  → MotionGuardRuntime.VerifyAxisMove(:633, skipSharedRailXRule=false — SharedRailX 규칙 포함)
  → 거부 시 -11.
- 이동 중 오버라이드: TryOverridePosition(:317 시뮬/:344 실장비) →
  MotionGuardRuntime.VerifyAxisTeachingMove(:176) → service.VerifyAxisTeachingMove
  (PickerZone·SharedRailX 포함, :217 주석) → 거부 시 -11.
- 우회 API(BeginMotionGuardBypass) 미사용 — grep으로 최종 증명 예정.

## 구현 결과 (Phase C — 2026-07-24 완료)
- [x] C0. 공통 파라미터 조회 — SharedRailXMotionService.TryGetFollowGapParameters
  (16번 체크리스트 B0과 동일 헬퍼 공유). direction까지 페어 TowardSign에서 도출.
- [x] C1. Input ①: InputDieVisionPrepareSequence
  - 게이트 IsVisionReturnFollowGateSatisfied() = Auto + PickUp TransferMotionMode ∈
    {ContiSegmentedPickUp, FastContiSegmentedPickUp} (InputCameraMarkInspectionSequence
    :552 선례 미러).
  - 게이트 시 존 클리어 블로킹 대기 대체: MoveInputStageAndVisionToDieAsync(:509)의
    WaitFrontRearPickerInputZonesClearBeforeVisionAsync → StageY/NeedleX 목표에 대한
    MotionGuardRuntime.CanAxisTeachingMove dry-run 폴링 대기(신설, 타임아웃/선행검사
    무한대기 규칙은 기존과 동일). X 간섭은 follow+MotionGuard가 담당.
  - MovePickersToAvoidForInputVisionMoveAsync 선행검사 분기(:456)의 존 대기도 게이트 시
    생략(같은 근거). 일반 모드 자기 피커 Avoid 이동은 무변경.
  - VisionX 진입 follow: MoveInputVisionXAndVerifyAsync(:2260) 내부에 게이트+제약 판정 —
    목표가 양 피커(Front/Rear) Actual/Command 페어 간격을 이미 만족하면 기존 일반 이동
    (존이 빈 경우, 2번째 이후 촬영 자연 일반화), 아니면 선행=제약 피커(X 작은 쪽,
    ActualPosition 기준) follow(+1, 선행 목표=선행 Command). 실패 시 R5 폴백(기존
    명령 경로 = WaitInputVisionXSharedRailClear 대기 + 일반 이동 1회 재시도).
    L자 경로 (b)/(d)/(e)는 이 헬퍼를 공유하므로 자동 커버, NeedleX/StageY 순서 로직 무변경.
- [x] C2. Output ②: OutputPostPlaceInspectionQueue.CapturePlacedDieAsync
  - 게이트 = 기존 IsMinimalRetreatGateSatisfied(request).
  - 게이트+제약 시 WaitOutputVisionXSharedRailClearAsync(:868) 생략, VisionX 이동을
    follow(선행=제약 피커 X 큰 쪽, direction 페어 도출(-1), 후행목표=targetVisionX)로 대체.
    실패 시 기존 Wait+MoveStageAxisAndVerify 1회 재시도, 알람코드 기존 유지.
    존이 빈 경우(양 피커 간격 만족)는 기존 경로(기존 인포지션 단락 유지).
- [x] C3. 정지 선행축도 항상 follow 시도(제약이 있으면) — follow 내부가 여유≤0이면 명령
  보류 후 대기, 타임아웃(-21) 시 폴백. 역방향 판단 없음(주석 근거: 접근 이동은 피커측
  인터락이 차단, follow는 후행 전진만 수행). 하네스 S1/S2로 검증.
- [x] C4. R1-①-3 StageY/NeedleX 안전 전제 분석 (보고서 반영):
  - 기존 존 대기가 보호하던 대상 = VisionX(X 간섭) + StageY/NeedleX/StageZ/StageT/EjectPinZ
    (피커 하강 상태 간섭) + Unknown 상태.
  - StageY 이동 인터락(InputStageInterlockRules CanManualWaferStageY :942/:946)이
    VerifyPickerZAxesAvoidWhenInputRisk(Front/Rear)를 하드 차단으로 이미 검증 —
    NeedleX도 동일 계열. EjectPinZ zero/avoid 인터락 별도 존재(:131).
  - 게이트 시 대체 대기(C1)는 같은 판정을 dry-run(CanAxisTeachingMove)으로 폴링하므로
    알람 대신 대기 의미 유지(fail-safe 동일, X존 소속 조건만 follow로 이관).
- [x] 검증(16번과 통합, 2026-07-24): 빌드 통과 / 하네스 22/22 — return R1(피커 620→750,
  비전 638.5→680 도달, minGap 62.9≥50), R2(피커 540→400, 비전 80→30 도달, minGap=50.000),
  S1(정지 선행축 한계 658.5 대기→외부 기동 후 680 도달), S2(부동 -21 타임아웃 후 정지) /
  오버랩 타임스탬프(E1: 비전 11ms/피커 30ms) / BeginMotionGuardBypass 0건 grep /
  인터락 파일 diff 0건 / 비Conti·수동·복원 경로 게이트 false → 기존 경로 그대로.
  B안 hold는 #16 기구현 유지(TryResolveMinimalVisionRetreatTarget 무변경).
