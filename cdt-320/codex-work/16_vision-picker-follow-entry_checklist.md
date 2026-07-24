# 체크리스트 — 비전 회피∥피커 진입 팔로잉 (vision-picker-follow-entry.md)

작성일: 2026-07-24  /  상태: **사용자 승인(2026-07-24) — 진행 중**
브랜치: feature/vision-minimal-retreat

## 진행 상태 (승인 후)
- [x] Phase A (커밋 a77d3dc9): 인터락 제3 분기 6곳(피커 진입 F/R×I/O 4 + 비전 진입 I/O 2,
  MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry + Service.IsPairClearanceSatisfied,
  목표 vs 상대축 Actual/Command 양쪽 판정, Extra 미포함) + 정위치 소비자 3곳
  (허가 소비/Conti 적격 entryLimit/StopAfterDrain) 정합. 빌드 통과.
- [x] Phase B 선반영분 (커밋 5fb19558/1a9a711b/e5ae3058): R1 FollowMoveAsync timeoutMs
  인자화(effectiveTimeoutMs — 루프+최종 대기 동일 적용, 기존 호출부 무변경) /
  R2 VisionFollowEntryTimeoutMs(기본 15000, Normalize ≥1000, Document Order 7,
  SharedRailXSetupDialog 노출 — Extra 2종 포함).
- [x] Phase B 본문 (R3/R4) — 구현 완료 (2026-07-24, 하네스 22/22 통과):
  - [x] B0. SharedRailXMotionService.TryGetFollowGapParameters(trailing, leading, extra,
    out direction/homeGap/safetyGap/detail) 신설 — direction=후행축 페어 TowardSign(부호 도출,
    하드코딩 금지), homeGap=pair.HomeClearance, safetyGap=(pair.SafetyDistance ?? 축 설정
    폴백 Max) + extra. 4개 결합 지점 공통 사용.
  - [x] B1. 픽업(R3): `_inputVisionRetreatMoveTask` 필드 신설(60행 옆). MoveInputVisionToAvoid
    ForPickerMove에서 useMinimalRetreat && targetsCalculated && 미인포지션이면 기존 3단
    헬퍼(MoveInputStageAxisCommandAsync→Wait→Check) 합성 Task를 **비동기 시작**(미await,
    기존 SharedRailX 중재 경유 유지). 그 외(첫 계산 패스/legacy/수동)는 기존 동기 경로.
  - [x] B2. 픽업 X 진입 follow: 공통 시작 헬퍼 StartPickUpPickerXEntryMoveTask(vel/acc/dec,
    …) — 적격(회피 Task 활성 && CameraX.IsMoving && AjinAxis && 파라미터 조회 성공)이면
    follow 합성(FollowMoveAsync(선행=CameraX, 선행목표=_inputVisionPickerEntryTarget,
    후행목표=_targetPickerX, direction=페어 도출(-1), timeout=VisionFollowEntryTimeoutMs)
    + R6 폴백: 실패 시 비전 Task join(observe) 후 기존 MovePickerAxisWithMotionAndVerify
    1회 재시도), 아니면 기존 헬퍼. 적용 지점 3곳: Conti(2184)/FastConti(2537)는 Task 생성부
    교체(Conti는 가드가 비전 이동 중을 거부하므로 실질 도달 경로는 Default/FastConti),
    Default(2007)는 X/T 묶음에서 X 분리(X=follow ∥ T=단독 ∥ NeedleX/StageY 유지 → Y 전진).
  - [x] B3. 픽업 join: MovePickerXStageYPickerTAsync에서 분기 반환 후 비전 회피 Task join —
    분기 실패 시 drain(observe), 성공 시 결과 0 확인(실패 시 Fail). 리셋/취소 drain:
    PrepareInputDieVisionBatchAsync + Abort(ContinueWith observe).
  - [x] B4. 플레이스(R4): `_outputVisionRetreatMoveTask` 필드. MoveOutputStageAvoidPosition
    1796의 동기 await를 useMinimalRetreat 시 비동기 시작으로 교체(동일 헬퍼
    MoveOutputStageAxisAndVerifyAsync 합성). 2번째 픽커부터는 비전 인포지션 → 즉시 완료
    Task → 진입측 IsMoving=false → 자연 일반 이동(스펙 R4-3).
  - [x] B5. 플레이스 X 진입 follow: MoveOutputStageYAndPickerXYTToPlaceAsync에서 적격 시
    X/T 분리 — X=follow 합성(선행=OutputCameraX, direction=페어 도출(+1)) ∥ T=단독,
    완료 후 Y 전진(기존 MovePickerXTThenYAndVerifyAsync 구조 미러). StageY 병렬 유지.
    비적격 시 기존 MovePickerXTThenYAndVerifyAsync 그대로.
  - [x] B6. 플레이스 join/drain: MoveOutputStageReceivePositionAsync — 선행 실패 경로
    (feeder/워크영역/Prepare)와 ByMode 반환 직후 join(성공 시 0 확인, 실패 시
    PICKER-PLACE-VISION-X-AVOID Fail), Abort에 observe drain.
- [x] Phase C (#18): 17번 체크리스트의 C0~C4 참조 — 구현 완료 (동일 커밋).
- [x] 검증 (2026-07-24): 빌드 통과(OutDir 스크래치) / 하네스 22/22 —
  E1(Input 650→638.5+피커 700→600, minGap=50.000, 오버랩 11ms/30ms 동시 시작),
  E2(Output 120→80+피커 440→540, minGap=50.000), R1(피커 620→750+비전 638.5→680 도달),
  R2(피커 540→400+비전 80→30 도달, minGap=50.000), S1(정지 선행축 — 한계 658.5 대기 후
  외부 기동 시 680 도달), S2(부동 선행축 -21 타임아웃, 658.5 정지), D1(역방향 -1 즉시 거부),
  M1(#16 수식 4건: 658.5/80/698.5/40) / 전 구간 인터락 요구(10) 침범 0회 /
  인터락 파일 diff 0건 / BeginMotionGuardBypass 사용 0건(grep) / 비Conti·수동 경로는
  게이트(false)에서 기존 동기 경로 그대로(코드 확인).

(이하 승인 전 차단 분석 기록 보존)

## 선행 의존 점검 (프롬프트 지시)
- [x] AjinAxis.FollowMoveAsync 존재 확인 — AjinAxis.cs:501 (선행축 읽기 전용, safetyGap/homeGap/
  direction 인자, 타임아웃은 const 5000(:485) → R1 인자화 대상 확인)
- [x] 최소 회피(#16) — feature/vision-minimal-retreat 브랜치에 구현 완료 상태로 보존
  (Extra 설정 2종 + TryResolveMinimalVisionRetreatTarget, 하네스 13/13). master 미적용 —
  **적용이 인터락 결정에 막혀 있음** (16번 체크리스트/레포트 참조).

## 차단 사유 (코드 직접 확인 완료 — 검증자 보고와 별개로 원문 대조)
스펙 R5는 "follow 경로가 MotionGuard를 통과해야 하고, 인터락 판정은 절대 변경 금지,
팔로잉 간격(50) > 인터락 요구(10)이므로 거부 없음"을 전제한다. 그러나 실제 인터락은
거리 기반이 아니라 **존/위치 기반**이다:

- PickerFrontInterlockRules.cs:598~625 (Rear 동일): 피커 X 이동은 AxisMove/AxisTeachingMove
  모두 CanManualFrontPickerX를 경유하며, **목표 존이 Input/Output이면**
  VerifyInput/OutputVisionXAtAvoidOrBelowZero(:691/:727 호출, 정의 :839/:927)가
  "비전 = 전체 Avoid 정위치 OR ≤0"을 요구한다.
- follow-entry의 목적 자체가 "비전이 아직 회피 중(>0, 비Avoid)일 때 피커가 존 목표로 진입"
  이므로, follow 내부의 이동/오버라이드가 이 룰에서 **-11로 즉시 거부**된다.
  → R6 폴백(순차 재시도)만 항상 타게 되어 오버랩이 성립하지 않는다.
- 즉 (a)인터락 무수정 ∧ (b)follow가 인터락 통과 ∧ (c)오버랩 진입 — 세 요구가 동시에
  성립 불가능하다.

## 구현 가능 독립 부분 (결정과 무관, 착수 보류 중)
- R1: FollowMoveAsync timeoutMs 인자화 (기본 5000, 기존 호출부 무변경)
- R2: VisionFollowEntryTimeoutMs 설정(기본 15000, Normalize ≥1000) + SharedRailXSetupDialog
  UI 노출 (Extra 2종 포함)
→ 결정 후 본 구현과 함께 진행 예정 (파편 선반영은 결정 방향에 따라 재작업 위험).

## 재개 조건
16/17/18 공통 단일 결정: 존 진입 인터락(VerifyInput/OutputVisionXAtAvoidOrBelowZero 및
비전 진입측 VerifyPickerInputZoneClearForInputVisionX 계열)에
"SharedRailX 페어 간격 ≥ SafetyDistance(Extra 미포함) 충족 시 허용" 제3 분기 추가 승인 여부.
