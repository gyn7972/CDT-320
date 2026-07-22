# 체크리스트 — FastConti X 우선 + Y 클리어런스 게이트 분기 (fast-conti-x-first-y-clearance-gate.md)

작성일: 2026-07-22

## 계획 요약
- 사이클 진입 시 자기 PickerY 전진 여부로 분기:
  - **경로 A (Y 미전진)**: [0] T만 선행 → X 이송(비동기) ∥ StageY/NeedleX → [7] 합류 →
    [7.5] `MovePickerAxesAndVerifyAsync`(PickerY 단독, 동일 targetName)로 Y 전진 —
    기성 게이트 체인이 FacingY X 클리어런스 확인 + 해제까지 무한 대기 수행(D2/D5) →
    Picker Vacuum ON → [8'] Z 단일 하강(기존 MoveFastPickerZToPickWithSlowZoneAsync,
    정지 상태라 새 Move 경로).
  - **경로 B (Y 전진)**: 기존 사이클 그대로 — [0] Y 미세보정+T → [5-2] 접근 트리거 →
    [6] PrePick 선행 하강+Vacuum ON → [7] 합류 → [8] 오버라이드 Pick 하강.
- 판정(D6): 자기 PickerY가 Home(0, InPositionTolerance) 또는 AvoidPosition
  (`IsPickerAxisInTeachingPosition(PickerY, "AvoidPosition")` — PickerSequenceBase 1655행의
  상대축 판정과 동일 유닛 API)이면 미전진. **이동 중/예외/축 없음 = 보수적으로 경로 A.**
- processVelocity/Acc/Dec 산출은 분기 밖으로 이동 ([8]이 양 경로 공용으로 사용).
- default/Conti/공용 게이트/[9]~[14] 무수정.

## 구현
- [x] C1. 판정 헬퍼 `IsOwnPickerYForwardForFastEntry()` 신설 (Fast 리전 내 private) —
  Home/Avoid/이동중/예외 → false(경로 A). 근거: PickerSequenceBase.cs 1638~1691행 미러
- [x] C2. 사이클 시작 로그에 `yForwardEntry` 추가 + 분기 변수 도입
- [x] C3. [0] 분기 — 경로 B만 preTargets에 PickerY 포함, 경로 A는 PickerT만.
  설명 문자열도 경로별 표기
- [x] C4. process 속도 3종 산출을 [5-2] 앞(분기 밖)으로 이동 — [8] 공용 사용 유지
- [x] C5. [5-2]+[6]+Vacuum ON 블록을 `if (yForwardEntry)`로 감싸기 (경로 B 전용).
  경로 A에서 X 접근 감시/PrePick 하강/조기 Vacuum ON 미수행
- [x] C6. [7] 합류 직후 `if (!yForwardEntry)` — [7.5] PickerY 전진
  (`MovePickerAxesAndVerifyAsync`, PickerY 단독, 동일 targetName → IsForwardPickerYMoveTarget
  통과로 게이트 적용) → 성공 시 `SetPickerVacuum(true)` + 로그. 실패 시 결과 반환
  (기존 래퍼의 안전 복귀 경로 사용)
- [x] C7. [8]은 양 경로 공용 무변경 — 경로 A는 정지 상태 새 Move, 경로 B는 이동 중
  오버라이드 (기존 분기 로직이 자연 처리)
- [x] C8. 기존 경로 보호 — default/Conti/공용 게이트/PickerZoneInterlockRules/[9]~[14] diff 0.
  `FastPickerXApproachDistance`/`PickerZPrePickDistance` 경로 B에서 계속 사용

## 검증
- [x] V1. 빌드 통과(`/t:Build`, OutDir 고정), 신규 경고 0
- [x] V2. 경로 A 안무 하네스 — X 완료 전 Y 미발행 / Y InPosition 전 Z 미발행 /
  Z 저속존→InPosition 기존 동작 (타임스탬프 검증)
- [x] V3. 경로 B 회귀 — 기존 FastContiPickupHarness 풀 안무(V2) 재실행 ALL PASS
- [x] V4. 분기 판정 — 코드 리뷰(Home/Avoid/이동중/예외 → 경로 A) + 사이클 로그 yForwardEntry.
  시퀀스 인스턴스 판정 함수는 하네스 단위 실행 불가 → 코드 리뷰 대체 명시
- [x] V5. 게이트 대기 — 기성 게이트 체인 코드 리뷰(무한 대기·클리어런스 판정 행 인용) +
  통합 모사 불가 사실 레포트 명시
- [x] V6. 회귀 — git diff가 Fast 리전 한정인지 확인, 설정 2종 경로 B 사용 잔존 확인
- [x] V7. 레포트 — 경로별 순서 표, 택트 영향(경로 A 한정), 재사용 근거 행 인용,
  현장 확인 항목

## 검증 결과 기록 (2026-07-22)
- V1: 빌드 EXIT=0, PickerPickUpSequence 신규 경고 0.
- V2: 하네스 V10 (경로 A 안무) — X 이송 중 Y/Z 정지 유지 확인(300ms 시점 y=0/z=0) →
  X 완료(t=20011ms) 후에야 Y 시작 → Y InPosition(t=21203ms) 전 Z 정지 유지 →
  Z 정지-시작 새 Move 하강에서 저속존 전환(remain=0.224mm, result=0) → Pick InPosition. ALL PASS.
- V3: 하네스 V2 (경로 B 기존 안무) 재실행 — 트리거(remain 19.8mm)에서 X 이동 중 Z PrePick
  오버랩 → 오버라이드 → 저속존 → SyncLift → stage-safe 전부 PASS (회귀 없음).
- V4: 판정 헬퍼 IsOwnPickerYForwardForFastEntry 코드 리뷰 — null/이동중/Home(tolerance)/
  AvoidPosition/예외 전부 false(경로 A) 확인. 사이클 시작 로그에 yForwardEntry 기록.
  시퀀스 인스턴스 판정이라 하네스 단위 실행 불가 — 코드 리뷰로 대체(명시).
- V5: 게이트 통합 모사 불가 — 대체 근거: [7.5]는 MovePickerAxesAndVerifyAsync 경유로
  WaitPickerXSharedRailDistance(936행)→WaitOppositePickerYAvoid(945행, 타임아웃 없는 해제
  대기+FacingY 클리어런스 포함 1504~1584행)→WaitPickerFacingY(983행, 타임아웃 없음) 체인이
  그대로 적용됨 (기존 [0]과 동일 경로 — 검증된 코드 재사용).
- V6: git diff — PickerPickUpSequence.cs 변경 헝크 전부 Fast 리전(2088행 이후)+신규 헬퍼.
  default/Conti/공용 게이트 무변경. FastPickerXApproachDistance/PickerZPrePickDistance는
  경로 B([5-2]/[6])에서 계속 사용. 인코딩 BOM+CRLF 유지.
- V7: 레포트 기재 — 경로별 순서 표, 택트 영향(경로 A=배치 첫 픽 한정), 현장 확인 항목.
- 3회 반복 불필요: 1차 구현에서 체크리스트 전 항목 충족.

## 개정 1 — [4-1] NeedleZ 확인/상승 추가 (2026-07-22, 사용자 지시 "2번")
- 배경: Fast 사이클에 NeedleZ 상승 단계·확인 가드가 전무(갭). 정상 운전은 default/conti-폴백이
  한 번 올린 뒤 유지되지만, 웨이퍼 교체 직후 NeedleZ Home/Safe 상태에서 첫 픽이 Fast 직행하면
  니들 없이 하강하는 사고 구조. conti식 가드(1번)만으로는 Fast에 폴백이 없어 정지만 하고
  니들이 올라오지 못함 → 확인 후 미달 시 동기 상승(2번)으로 구현.
- [x] R1. `EnsureFastNeedleZAtPickTargetAsync` 신설 — 목표(_targetNeedleZ) 유효성 확인,
  이미 위치면 통과(teaching 유지 로그, 추가 시간 0), 미달이면
  `MoveInputStageAxisCommandAsync`(명령+완료대기)로 상승 후 `CheckInputStageAxisInPosition`.
  default `PrepareNeedlePinZForPickAsync`(5510행) NeedleZ 파트 동일 패턴.
  Needle Vacuum ON([5-1] 기존 처리)·EjectPinZ([4] 기존 처리)는 불변.
- [x] R2. 호출 위치 = [4] EjectPinZ 확인 직후, [5-1] StageY/NeedleX 이송 시작 전 ([4-1]).
  실패 시 [4]와 동일하게 pickerXMoveTask observe 후 반환. 양 경로(A/B) 공통 적용.
- [x] R3. 검증 — 빌드 EXIT=0, 신규 경고 0. NeedleZ 상승 후 StageY/NeedleX 이동은 conti의
  "NeedleZ 유지 동시 XY" 기존 정책(3173행)과 동일 상태라 안전. 상승은 기존 검증된
  스테이지 이동 헬퍼 재사용이므로 하네스 추가 없이 코드 리뷰로 확인.