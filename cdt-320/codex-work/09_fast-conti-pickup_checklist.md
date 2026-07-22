# 체크리스트 — FastContiSegmentedPickUp 고속 픽업 모드 (fast-conti-segmented-pickup.md)

작성일: 2026-07-20

## 계획 요약
- 신규 모드는 `MovePickerXStageYPickerTAsync` 분기에서 전용 함수로 진입, R3 표 0~14를 함수 내부에서
  수행 후 `UpdateMaterialToPicker` 스텝으로 전이 (VerifyPickTarget/MovePickerZPick 스텝 생략 —
  "이송~Z 마무리 구간 대체" 사양).
- 기존 default/conti 경로 무수정 (분기 추가만). `_pickerZContactedByContiPickUp` 미사용 —
  Fast는 자체 경로로 Z까지 완결.
- 배치 마지막 픽에서는 EjectPinZ를 Process 대신 **Avoid**로 복귀 + PickerZ Avoid 완료 대기 후
  `_currentPickSafeReturnCompleted = true` (InputStageDieComplete 신호가 안전 복귀를 요구하므로 —
  Process 상주는 배치 내 사이클 간에만 유지).
- X/StageY/NeedleX 이송 속도는 기존 conti node2 속도(GetTransferContiNode*) 재사용.
- SyncLift는 기존 관례대로 `PickUpNeedleSyncLiftDistance` + `PickUpEjectPinOffset`(EjectPin측) 적용,
  속도는 `PickUpNeedleSyncLiftVelocity/Acc/Dec` (D1).

## 구현
- [x] C1. enum `FastContiSegmentedPickUp = 3` 추가 (기존 값 유지 — 직렬화 호환)
- [x] C2. Config 신설 3종 + 기본값: `FastPickerZSafePosition=-3.0` / `FastPickerXApproachDistance=20.0` / `FastContactSlowZoneDistance=0.3`, `Ensure()` 보정(NaN/Inf/비정상), TransferMotionMode 검증에 새 모드 허용
- [x] C3. `MovePickerXStageYPickerTAsync` 분기 — Fast면 전용 함수 호출, 공용 `EnsureZAxesAtAvoidBeforePickerMove`는 생략(Fast 자체 Z 안전 확인으로 대체). Needle 작업영역/StageT 얼라인 확인은 기존 유지
- [x] C4. Step0 선행 보정 — PickerY+PickerT 병렬 (EjectPinZ Avoid 제외, D4)
- [x] C5. Step1 `EnsurePickerWorkAreaReserved(Input)` / Step2 4피커 Z `Actual > FastPickerZSafePosition` 검사, 미달 시 4축 Avoid 비동기 명령 + `IsInPosition` 신호만 확인
- [x] C6. Step3 PickerX 비동기 이송(conti node2 속도) / Step4 EjectPinZ 3분기(`Actual < Process+0.1` 패스 / 이동중 통과 / 정지면 Process 동기 이동)
- [x] C7. Step5-1 StageY∥NeedleX 비동기 + 완료 연속 Needle Vacuum ON / Step5-2 PickerX 잔여 `< FastPickerXApproachDistance` 10ms 폴링(알람·조기정지·타임아웃 Fail) — 동시 진행
- [x] C8. Step6 트리거 도달 시 PickerZ→PrePick 비동기 명령(공정 Vel/Acc/Dec×MotionSpeedScale) + Picker Vacuum ON / Step7 X+StageY/NeedleX 합류
- [x] C9. Step8 PickerZ 정지→새 Move, 이동중→`TryOverridePosition`(PickPosition) / Step8-1 0.3mm 구간 진입 감시→`TryOverrideVelocity`(PickerZSlowApproachSpeedPercent 저속)→InPosition 대기
- [x] C10. Step9 SyncLift — PickerZ(Pick+Lift) ∥ EjectPinZ(Process+Lift+Offset), InPosition만 확인(deferFinalPositionCheck) / Step10 `max(VacuumOnBeforePickDelayMs, SyncLiftSettleMs)` 대기 / Step11 Flow ON 확인(D2)
- [x] C11. Step12 PickerZ→Avoid 비동기(복귀 속도%) / Step13 `PickPosition+PickerSafeForWaferStageDistance` 통과 폴링→Needle Vacuum OFF(D3) / Step14 EjectPinZ→Process(마지막 픽은 Avoid+PickerZ Avoid 완료+안전복귀 플래그) / Step15 `UpdateMaterialToPicker` 스텝 전이
- [x] C12. R4 실패 처리 — 실패 시 `TryMovePickerNeedleAndEjectPinZToAvoidAsync` 안전 복귀 후 Fail, 폴백 없음(D5), 감시 루프 전부 `Max(TransferContiTimeoutMs, ResolveTimeout())` 타임아웃+ct+알람 확인, 백그라운드 태스크 observe
- [x] C13. 기존 default/Conti 경로 코드 무변경 (분기 추가만), 시뮬 분기 신설 없음(기존 헬퍼 경유, R5)

## 검증
- [x] V1. 솔루션 빌드 통과, 신규 경고 없음
- [x] V2. 시뮬 하네스 — Fast 사이클 모션 안무 재현: X 이송 중 잔여 20mm 도달 시 Z PrePick 하강 시작 → Z 이동 중 PickPosition 포지션 오버라이드 → 0.3mm 구간 저속 전환 → InPosition → SyncLift 2축 InPosition → Avoid 상승 중 safe 거리 통과 시점 검출, 단계별 타임스탬프 로그
- [x] V3. Z 안전 분기 — Z 하나 -3 이하에서 시작 → 4축 Avoid 명령+InPosition 확인 후 진행
- [x] V4. EjectPinZ 3분기 각각 동작 확인
- [x] V5. PrePick 조기 완료(정지→새 Move) / 이동 중(오버라이드) 양쪽 경로
- [x] V6. 기존 모드 회귀 — default/Conti 경로 diff 없음 확인(코드 리뷰) + 빌드
- [x] V7. 실패 주입 — X 조기 정지/알람 시 Fail 반환 (프리미티브 수준)
- [x] V8. D6 보고 — `ResolveEjectPinZPickTarget()` == `Recipe.EjectPinZ.ProcessPosition` 확인 완료(동일). EjectPinZ Process 상주 중 StageY 이동은 InputStage 인터락(EjectPinZ 0이하|Avoid)과의 정합을 현장 확인 필요 사항으로 보고

## 검증 결과 기록 (2026-07-22)
- V1: `/t:Build` OutDir=_build_check_handler\out — 에러 0. 경고는 AxisInitializePlan CS0162×3,
  OutputFeederInterlockRules CS0168, MachineController CS0162 — 전부 미수정 파일의 기존 경고
  (신규 파일 2종에서 경고 0).
- V2~V5, V7: `FastContiPickupHarness.exe` (scratchpad\FastContiPickupHarness.cs, 시뮬 AjinAxis 안무 재현)
  — 26 체크 전부 PASS. 핵심 타임라인: X 이송 시작 t=58ms → 잔여 19.742mm 트리거 t=1759ms →
  Z PrePick 하강 시작 t=1761ms(X 이송 중=True, 오버랩 확인) → 3축 합류 t=2251ms →
  Z 포지션 오버라이드 수락(0) → 저속 전환 remain=0.215mm → Pick 도달/InPosition →
  SyncLift 2축 InPosition(z=-38.0, eject=+1.5=Process+Lift+Offset) → Avoid 상승 중
  progress=5.106mm에서 stage-safe 검출(이동 중=True) → Avoid 완료.
  V5: 정지 축 TryOverridePosition=-4 → 새 Move 폴백 도달 / 이동 축 오버라이드 수락 양쪽 확인.
  V7: X 조기 정지 주입 → 잔여 89.3mm에서 정지+미도달 판정 검출.
- V6: `git diff` — 변경 파일 2종뿐(PickerTransferTypes.cs +21, PickerPickUpSequence.cs +858).
  기존 경로 수정은 `MovePickerXStageYPickerTAsync`의 가드 분기(useFastContiTransfer)뿐이며
  Fast 외 모드에서는 기존 동작 동일(EnsureZAxesAtAvoid 실행 + 분기 미진입). 나머지는 전부 신규 리전.
- 인코딩: 두 파일 모두 UTF-8 BOM 유지, LF 단독 개행 0건(CRLF 일관).

## 개정 1 — 속도 체계 통일 (2026-07-22, 사용자 지시)
전 구간(0~14) 속도를 "축 Config 속도 × 기본 스케일(MotionSpeedScale)"로 통일.
- [x] R1. 이송 3축([3] PickerX, [5-1] StageY/NeedleX): conti node2 속도 → 각 축 Config×기본스케일
- [x] R2. [2] Z 안전 복귀 / [6] PrePick / [8] Pick / [12] Avoid 복귀: Config×기본스케일
  (기존 [12]의 PickerZAvoidReturnSpeedPercent 사용 제거)
- [x] R3. [8-1] 저속 구간: Config×기본스케일×PickerZSlowApproachSpeedPercent
- [x] R4. [9] SyncLift: PickerZ=Config×기본스케일×PickerZSeparateSpeedPercent,
  EjectPinZ=Config×기본스케일×PickUpNeedleSeparateSpeedPercent (PickUpNeedleSyncLift* 속도 사용 제거)
- [x] R5. [0]/[4]/[14]: 유닛/스테이지 기본 이동 경로가 이미 Config×기본스케일과 동일함을 확인(코드 리뷰,
  PickerRearUnit.ResolveMoveVelocity / InputStageUnit.ResolveAxisVelocity) — 무변경, 주석만 명시
- [x] R6. 이중 스케일 방지: 이동 명령 경로는 가감속 "원값" 전달(속도가 기본 스케일과 일치하면 축 레이어
  MoveAbsoluteAsync가 1회 스케일 — PickerRearUnit.cs:2756 S² 버그 주석 근거). %속도(<100)면 축 레이어가
  스케일하지 않으므로 시퀀스에서 기본스케일×% 1회 적용. 오버라이드 경로(TryOverridePosition/Velocity)는
  자동 스케일이 없어 항상 명시 스케일. 헬퍼 3종 신설(ResolveFastAxisVelocity /
  ResolveFastMoveAcceleration / ResolveFastOverrideAcceleration)
- [x] R7. 빌드 통과(에러 0, 신규 경고 0) + 하네스 V9(속도 규칙 8체크, 리플렉션) 추가 — 총 34체크 ALL PASS.
  V9는 100%: vel=스케일 기본속도·이동가감속=원값·오버라이드가감속=스케일값, 50%: 전부 스케일×0.5 확인
  (하네스 프로세스의 전역 스케일이 100%라 스케일값=원값으로 표시되나 규칙 분기·% 산식은 수치 검증됨)
- [x] R8. 기존 default/conti 경로 무변경 재확인 — conti node2(1778행)·SyncLift 속도(5978행)·ByPercent류
  기존 사용처 전부 비-Fast 경로에 그대로 잔존
