# 체크리스트 — 비전 회피∥피커 진입 팔로잉 (vision-picker-follow-entry.md)

작성일: 2026-07-24  /  상태: **사용자 승인(2026-07-24) — 진행 중**
브랜치: feature/vision-minimal-retreat

## 진행 상태 (승인 후)
- [x] Phase A (커밋 a77d3dc9): 인터락 제3 분기 6곳(피커 진입 F/R×I/O 4 + 비전 진입 I/O 2,
  MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry + Service.IsPairClearanceSatisfied,
  목표 vs 상대축 Actual/Command 양쪽 판정, Extra 미포함) + 정위치 소비자 3곳
  (허가 소비/Conti 적격 entryLimit/StopAfterDrain) 정합. 빌드 통과.
- [ ] Phase B (#17 본문): R1 FollowMoveAsync timeoutMs 인자화(const 5000 → 기본값,
  AjinAxis.cs:485/501/580 — 루프와 최종 대기에 동일 적용, 기존 호출부 무변경) /
  R2 VisionFollowEntryTimeoutMs(기본 15000, Normalize ≥1000) 설정+Document(Order 7)+
  SharedRailXSetupDialog UI(Extra 2종 포함 노출) / R3 픽업: MoveInputVisionToAvoidForPickerMove
  비전 이동 비동기 시작(Task 필드 보관)+첫 피커 X를 FollowMoveAsync(선행=CameraX, dir=-1,
  safetyGap=Safety+InputExtra, homeGap=페어 HomeClearance 런타임 조회)로 진입, 비전 정지 시
  일반 이동, join/observe, R6 폴백(일반 이동 1회 재시도) / R4 플레이스 동일(dir=+1, OutputExtra).
- [ ] Phase C (#18): InputDieVisionPrepare 존 클리어 대기 → follow 대체(+StageY/NeedleX 안전
  전제 분석 보고), OutputPostPlace 큐 WaitOutputVisionXSharedRailClear 대기 → follow 대체,
  선행 피커 선택(Input: X 작은 쪽 / Output: X 큰 쪽), 정지 선행축도 follow 시도, 폴백.
- [ ] 검증: 빌드 / 하네스(#17: Input 650→638.5+피커700→600 간격≥50, Output →80+540 간격≥50;
  #18: 피커 620→750+비전→680, 540→400+비전→30; 정지 선행축 케이스; 오버랩 타임스탬프;
  Extra=0 경계) / 인터락 diff = 제3 분기 6곳뿐임을 보고 / 비Conti 회귀 없음 / 우회 API 미사용 grep.

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
