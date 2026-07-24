# 체크리스트 — 피커 퇴장∥비전 복귀 팔로잉 (vision-return-follow.md)

작성일: 2026-07-24  /  상태: **계획 단계에서 중단 — 선행 전제(인터락) 불성립 확인, 사용자 결정 대기**

## 선행 의존 점검
- [x] FollowMoveAsync 존재 (AjinAxis.cs:501). 타임아웃 인자화(R1 of follow-entry)는 미적용 —
  결정 후 함께 진행.
- [x] Extra 설정 2종 — feature/vision-minimal-retreat 브랜치에 구현 보존. VisionFollowEntryTimeoutMs
  미신설(follow-entry R2와 함께).

## 차단 사유 (코드 직접 확인)
비전 진입 이동 자체가 존/상태 기반 인터락으로 차단된다:

- InputStageInterlockRules.cs:704~757 VerifyPickerInputZoneClearForInputVisionX:
  피커가 Input 존을 점유 중이거나 X/Y가 존 안에서 이동 중이면(`BlocksTransport ||
  movingIntoOrInsideInput`) InputVisionX의 **진입(비Avoid 목표) 이동을 차단**한다.
  허용 예외는 (1) Avoid 복귀 목표(:725) (2) 퇴피(마이너스) 방향 이동(:750) 뿐이다.
- "피커가 퇴장 이동 중인 동안 비전이 촬영 위치로 추종 진입"은 정확히
  `movingIntoOrInsideInput(퇴장 이동 포함) && 진입 목표` 조합 → -11 거부.
  → 스펙 R4(인터락 무수정 + follow가 인터락 통과)와 목적(오버랩 진입)이 동시 성립 불가.
- Output측(OutputPostPlaceInspectionQueue)도 동일 구조: 현재 코드가
  WaitOutputVisionXSharedRailClearAsync(VerifySingleAxisMove+MotionGuard 폴링)로 클리어를
  "기다린 뒤" 진입하는 이유가 이 인터락이다 — 대기 생략+follow로 바꿔도 같은 인터락이
  follow 내부 이동을 거부한다.

## 재개 조건
follow-entry(16번 체크리스트)와 동일한 단일 결정: 존/상태 기반 진입 인터락에
"SharedRailX 페어 간격 ≥ SafetyDistance(Extra 미포함) 충족 시 허용" 분기 추가 승인 여부.
승인 시 세 작업(#16/17/18)을 한 브랜치에서 일괄 구현·검증하는 것이 정합적이다
(간격 판정 공유 + 시뮬 하네스 공통).
