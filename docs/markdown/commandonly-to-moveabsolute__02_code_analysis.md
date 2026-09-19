# MoveAbsoluteCommandOnlyAsync → MoveAbsoluteAsync 전환 영향 분석

- 작성일: 2026-07-29
- 기준: 로컬 워킹트리 (HEAD 8340c088, clean)
- 범위: MoveAbsoluteCommandOnlyAsync 전 사용처 조사 + 태스크 완료 대기 방식 전환 시 수정 지점 보고 (분석만, 수정 없음)

## 1. 핵심 결론

**MoveAbsoluteCommandOnlyAsync 체인은 현재 죽은 코드다.**
`PickerSequenceBase.cs:3134-3137` 주석에 명시: *"유일 사용처였던 고속 픽업 모드가 2026-07-25 삭제되어 현재 호출부 없음 — 사용자 승인 전 삭제 금지 지시에 따라 존치."*
따라서 지금 이 함수를 실제로 호출하는 생산 경로는 없고, 이 함수 뒤에서 인포지션을 체크하며 대기하는 감시 루프도 (고속 픽업 삭제와 함께) 이미 사라진 상태다.

## 2. 사용처 전수 인벤토리

| 계층 | 파일:라인 | 내용 |
|------|-----------|------|
| 축(기반) | QMC.Common\Motion\BaseAxis.cs:647 | `MoveAbsoluteCommandOnlyAsync` 가상 정의(시뮬 동작). 명령 발행만 하고 즉시 리턴 |
| 축(실장비) | QMC.CDT-320\Equipment\Ajin\AjinAxis.cs:1466 | 오버라이드. SharedRailX 축 거부, AXM.MovePosition 발행 후 즉시 리턴 |
| 축(실장비) | AjinAxis.cs:1483 | 시뮬 분기에서 base 호출 (내부 위임) |
| 유닛 | QMC.CDT-320\Equipment\Unit\PickerRearUnit.cs:1540, 1545 | `MovePickerAxisCommandOnly`(1524) 내부의 축 호출 2곳 (가드명 유/무 분기) |
| 유닛 | QMC.CDT-320\Equipment\Unit\PickerFrontUnit.cs:1828, 1833 | `MovePickerAxisCommandOnly`(1812) 내부의 축 호출 2곳 (Rear 미러) |
| 시퀀스 | QMC.CDT-320\Sequencing\Picker\PickerSequenceBase.cs:3138 | 래퍼 `MovePickerAxisCommandOnlyAsync` → 3149/3150에서 유닛 호출 |
| 시퀀스 상위 | (없음) | **래퍼 호출부 0곳** — 고속 픽업 모드 2026-07-25 삭제로 소멸 |

유닛 `MovePickerAxisCommandOnly`의 호출부도 시퀀스 래퍼(3149/3150)뿐 → 체인 전체가 미사용.
`CommandOnly` 변형은 MoveAbsolute 계열 하나뿐(MoveRelative 등 다른 변형 없음).

## 3. 전환 시 수정 지점 (어디를 어떻게)

전환 = "명령 발행 즉시 리턴 + 호출자 인포지션 감시" → "MoveAbsoluteAsync 태스크 완료 대기".

### 3-1. 직접 교체 대상 (축 호출 4곳)
1. **PickerRearUnit.cs:1540** — `item.MoveAbsoluteCommandOnlyAsync(target, vel, acc, dec)` → 완료 대기 호출로 교체
2. **PickerRearUnit.cs:1545** — 동일
3. **PickerFrontUnit.cs:1828** — 동일
4. **PickerFrontUnit.cs:1833** — 동일

단, §4-1 시그니처 문제 때문에 단순 `item.MoveAbsoluteAsync(target, vel)` 치환은 **명시 가감속이 유실**된다.
가감속을 보존하는 완료 대기 경로는 이미 존재: 형제 메서드 `MovePickerAxisCommandNamed`(6인자, Rear 1641 / Front 1929)가 쓰는 `SharedRailXMotionRuntime.MoveAxisAsync(item, target, vel, acc, dec, forceMove)`.
→ 이 경로로 바꾸면 `MovePickerAxisCommandOnly`는 기존 `MovePickerAxisCommandWithMotion`(Rear 1510/1515, Front 1798/1803)과 사실상 동일해짐. **메서드를 남길 이유가 없어지고 WithMotion으로 흡수하는 형태가 자연스러움** (삭제는 승인 필요).

### 3-2. 래퍼/정의 정리 대상 (전환 후 완전 미사용화)
5. **PickerSequenceBase.cs:3134-3151** — 시퀀스 래퍼: 의미 변경(완료 대기) 또는 삭제. 호출부 없음
6. **PickerRearUnit.cs:1520-1566 / PickerFrontUnit.cs:1808-1854** — 유닛 래퍼 본체(주석 포함): WithMotion으로 흡수 시 삭제 대상
7. **AjinAxis.cs:1462-1583** — 오버라이드 정의: 호출부 소멸 시 삭제 대상
8. **BaseAxis.cs:642-706** — 가상 정의: 호출부 소멸 시 삭제 대상

5~8 삭제는 전부 "사용자 승인 전 삭제 금지" 지시에 걸려 있으므로 **승인 후에만** 진행.

### 3-3. 수정 불필요 (인포지션 감시 루프)
CommandOnly와 짝이던 도달 감시 루프(FastContiSegmentedPickUp)는 07-25 삭제로 이미 없음 → 이번 전환에서 제거할 감시 코드는 0곳.

## 4. 동작 차이 (전환 시 반드시 인지할 것)

### 4-1. 시그니처: 가감속 전달 (핵심)
- CommandOnly: 4인자 — vel/acc/dec를 **스케일 완료된 최종값** 그대로 보드에 전달 (0 이하만 Config 폴백)
- MoveAbsoluteAsync: 2인자 — acc/dec는 항상 `Config.GetDefaultAcc/Dec()`(스케일 적용값). 명시 가감속은 **명시 프로파일 스코프**(`BaseAxis.TryGetExplicitMotionProfile`, AjinAxis.cs:1350)로만 전달되며, 그 스코프는 `SharedRailXMotionRuntime.MoveAxisAsync` 6인자 경로(최내곽 MoveAxisWithTemporaryMotionAsync)가 세팅함
- → 명시 가감속을 살리려면 직접 MoveAbsoluteAsync가 아니라 6인자 런타임 경로 사용

### 4-2. SharedRailX 축
- CommandOnly: SharedRailX 축 **명시 거부** (AjinAxis.cs:1470-1474)
- MoveAbsoluteAsync: SharedRailX 축이면 중앙 중재로 **자동 리다이렉트** (AjinAxis.cs:1284-1289)
- → 전환하면 "PickerX 미지원" 제약 자체가 소멸 (주석·설계 메모도 함께 정리 대상)

### 4-3. 이동 생략(스킵) 판정
- CommandOnly(AjinAxis): 스킵 블록 **주석 처리** — 무조건 명령 발행 (1491-1501)
- MoveAbsoluteAsync(AjinAxis): ExactMatchEpsilonMm(0.0001mm) 일치 시에만 스킵 (1313-1325)
- → 전환 시 목표 완전 일치 케이스에서 명령이 생략되는 차이 발생 (실용상 동등, 인지만)

### 4-4. 인터락 (전수 점검)
양쪽 모두: `MotionGuardRuntime.VerifyAxisMove` + 서보/알람/AjinSystem + 소프트리밋 + (유닛 레이어) `PickerZoneInterlockRules.BeginPickerZoneMove` + 가드명 있을 때 `BeginAxisTeachingMove`. **새로 추가되는 인터락 없음.**
차이 1곳: 유닛 레이어 `using` 스코프 — CommandOnly는 **명령 발행 직후** 존 인터락 스코프를 빠져나왔지만, 완료 대기로 바꾸면 **이동 완료까지** 스코프를 유지(WithMotion 경로와 동일 동작, 정상 방향).

### 4-5. 속도 스케일 (MotionSpeedScale 규칙)
- 양쪽 모두 명시 velocity는 최종값으로 취급, 폴백은 스케일 적용된 GetDefault*. 스케일 미적용 경로 신설 없음.
- 로그 태그: `ABS MOVE CMD`/`commandOnly=True` → `ABS MOVE`로 바뀜 (AxisMoveProfile 로그에서 명령 전용 식별자 소실 — 계측 관점 인지)

### 4-6. 완료 대기의 의미
AjinAxis.MoveAbsoluteAsync의 await 리턴 = `WaitUntilMoveDone(stopSerial, target)` 완료(인포지션/알람/정지) + Command↔Target 톨러런스 최종 확인(오버라이드 리다이렉트 시 생략, 1417-1444). 즉 "태스크 컴플릿" 대기는 이미 축 내부에서 인포지션 도달을 보장함 — 호출자 측 재대기(AxisMoveWaiter 재대기)는 R3 원칙대로 불필요.

## 5. 참고: 지금 살아있는 "뒤에서 인포지션 체크" 대기는 별개 경로

CommandOnly가 아니라, **완료 보장 이동을 태스크로 병렬 시작해 두고 최종 배리어를 인포지션 폴링으로 거는 ContiNode 경로**가 현존:

- PickerPickUpSequence.EntryMotion.cs:1577, 1607 (`WaitContiSegmentedPickUpFinalPositionAsync`)
- PickerPlaceSequence.ContiPlace.cs:455, 469, 483 (`WaitContiSegmentedPlaceFinalPositionAsync`)
- 기타 `WaitPickerAxisMoveDoneAsync`/`WaitPickerAxisMoveDoneInPosition` 사용처: PickerSequenceBase.cs:1046, 1209 / PlaceDown.cs:351 / PickVerifyManual.cs:424 / InputStageMove.cs:856 / PickerBottomAndSideInspectionSequence.cs:4144, 4378, 4441 / PickerSideInspectionSequence.cs:1531 / ColletCleaningSequence.cs:805 / OutputStageMapTransferPage.cs:2619, 2626 / (유닛 내부) PickerRearUnit.cs:2253, PickerFrontUnit.cs:2541

이들을 "이동 태스크 완료 대기"로 바꾸는 것은 별도 설계 작업(태스크 핸들을 메서드 경계 넘어 전달, 타 축·스테이지 대기와 혼재)이며 이번 지시 범위(MoveAbsoluteCommandOnlyAsync) 밖 — 원하시면 별도 분석으로 진행.

## 6. 결정 필요 사항

1. 죽은 체인 처리 방식: (a) CommandOnly 체인 삭제(호출부가 없으므로 "교체"가 아니라 "제거"가 실질) vs (b) 향후 재사용 대비 완료 대기 방식으로 전환해 존치
2. (b)라면 축 호출 4곳의 교체 경로: 단순 `MoveAbsoluteAsync(target, vel)`(가감속 유실 감수) vs `SharedRailXMotionRuntime.MoveAxisAsync` 6인자(가감속 보존, WithMotion과 동일화)
3. §5의 살아있는 인포지션 폴링 대기(ContiNode 최종 배리어)도 전환 대상인지 여부
