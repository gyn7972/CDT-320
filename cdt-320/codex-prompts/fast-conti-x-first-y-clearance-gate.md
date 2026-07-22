# 작업: FastConti 이동 순서 변경 — Y 미전진 진입 시 X 우선 + 클리어런스 게이트

## 목표 한 줄

FastContiSegmentedPickUp에서 **PickerY가 미전진(후퇴) 상태로 사이클에 진입할 때**(배치 첫 픽 등)
현재 "Y 선행 전진 → X 이송" 순서를 뒤집는다: **PickerX 이송을 먼저 완료**하고, PickerY를
움직이기 **직전에 X간 클리어런스 인터락을 확인**, 미충족이면 **해제될 때까지 계속 대기**한 뒤
Y를 전진시키고, 그 후에만 Z를 내린다.
**PickerY가 이미 전진 상태인 픽(배치 2번째 이후)은 기존 순서와 PrePick 선행 하강 오버랩을
그대로 유지한다** — Y 전진 상태에서는 "Y 전진 후 Z 하강" 불변식이 X 이송 중에도 충족되므로
오버랩이 유효하다.

## 환경

- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 4.7.2, C# 7.3)
- 빌드: `/t:Build`만 사용(Rebuild/Clean 금지), OutDir `D:\Source\CDT-320_NEW\_build_check_handler\out`
- 인코딩 UTF-8 BOM + CRLF, 로그/주석 한국어, `Task<int>`(0=성공), `ConfigureAwait(false)`,
  C# 7.3 문법만. 기존 코드 수정 시 "기존 조건:/현재 기준:" 주석 관례 준수.
- 주 수정 파일: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
  (`#region FastContiSegmentedPickUp`, `RunFastContiSegmentedPickUpCycleAsync` 약 2072행)
- **default/Conti 경로와 Fast 외 코드는 절대 수정하지 않는다.**

## 배경 — 현재 구조 (조사 확정, 커밋 6d36250d 이후 행 번호 기준. 수정 전 재확인할 것)

### 현재 순서 (문제 지점)
```
[0] PickerY + PickerT 선행 보정 — 완료 검증까지 await     (2103~2114행)
[3] PickerX 이송 시작 (비동기)                            (2128~2142행)
[5-1] StageY ∥ NeedleX (비동기)                           (2155~2156행)
[5-2] X 잔여 20mm 접근 감시                               (2158~2169행)
[6] PickerZ → PrePick 하강(명령 전용) + Picker Vacuum ON  (2171~2209행)
[7] 이송 합류                                             (2211~2226행)
[8][8-1] PickerZ → Pick + 저속존                          (2228~2239행)
```
즉 **Y 전진이 X 이송 시작보다 먼저**다 (conti 설계에서 물려받은 순서).

### 반드시 지켜야 할 기존 불변식 — "Y 전진 완료 후 Z 하강"
Default(1641~1657행: X/T·NeedleX/StageY 완료 → Y 전진 → 이후 Z), Conti(1721~1727행 Y/T 선행
후 1794행 X·1803행 Z 트리거), Fast(현재 [0]이 [3]보다 앞) — 전 경로가 이 불변식을 지킨다.
또한 `EnsurePickerYAtAvoidBeforePickMoveAsync`는 Y 수평 이동 전 Z 전축 Avoid를 강제한다
(3574~3576행) — **Z가 낮은 상태에서 Y를 움직이는 것은 충돌 위험으로 취급**된다.
따라서 Y를 X 뒤로 미루면, Z 하강 시작([5-2] 트리거 → [6]/[8])도 **Y 전진 완료 이후로 함께
재배치해야 한다.** [5-2]/[6]/[8]의 헬퍼들은 PickerY 상태를 전혀 확인하지 않으므로(2598~2643,
2650~2775행), 순서만 바꾸고 Z 트리거를 남겨두면 Y 후퇴 상태에서 Z가 내려가는 사고 구조가 된다.

### 재사용할 기성 자산 — "Y 전진 직전 X 클리어런스 확인 + 해제까지 대기"는 이미 존재
`MovePickerAxesAndVerifyAsync`(PickerSequenceBase.cs 917행)는 명령 발행 전 게이트를 통과한다:
1. `WaitPickerXSharedRailDistanceBeforeAutoMoveAsync`(1070/1093행) — targets에 **PickerX가 있을
   때만** 작동. SharedRailX 판정(Front↔Rear PickerX pair는 설정 계층에서 의도적으로 제외 —
   Vision↔Picker pair 전용)이므로 이번 요구의 판정기가 아니다.
2. `WaitOppositePickerYAvoidBeforeAutoForwardMoveAsync`(1367/1391행) — **Y 전진 목표일 때**
   상대 픽커 Y/존/페이즈 + **FacingY X 클리어런스**(`IsOppositePickerYReadyForForwardMove`
   1504~1584행, 내부에서 `PickerZoneInterlockRules.CanMovePickerAxisByFacingYInterlock` 판정
   포함)를 **타임아웃 없이 해제까지 1ms 폴링** 대기한다.
3. `WaitPickerFacingYInterlockBeforeAutoMoveAsync`(1217~1316행) — 축별 명령 직전 FacingY
   게이트, 역시 **타임아웃 없음**.
- 클리어런스 판정의 본가: `PickerZoneInterlockRules`(Equipment\Interlocks) —
  `CanMovePickerAxisByFacingYInterlock`(640~685행, PickerY 분기 =
  `CanMovePickerYByFacingYInterlock` 1956~2030행: 상대 Y 돌출 + X 경로가 클리어런스 안이면
  Y 전진 차단). 클리어런스 값 = `Setup.PickerYFacingXClearance`(Front 기본 300 / Rear 기본
  150, `ResolvePickerYFacingXClearance` 2414~2426행).
- 최후 방어선: `RealtimeCollisionSupervisor`(100ms, 위반 시 EStop) — 무수정 유지.

**결론: "Y 전진을 `MovePickerAxesAndVerifyAsync`(PickerY 단독)로 실행"하면 사용자가 요구한
"클리어런스 확인 → 실패 시 해제까지 계속 대기 → 이동"이 기성 게이트로 그대로 충족된다.**
새 인터락을 만들지 말고 순서 재배치 + 기성 게이트 재사용으로 구현한다.

## 요구사항

### R1. 사이클 진입 시 PickerY 상태 분기 신설
- 사이클 시작부([0] 직전)에서 **PickerY 전진 여부를 판정**해 두 경로로 분기한다:
  - **경로 A (Y 미전진)**: 배치 첫 픽, 또는 Y가 후퇴/Avoid 영역에 있는 모든 진입 → R2의
    "X 우선 + 게이트 + Y 전진 + Z 하강" 순서.
  - **경로 B (Y 이미 전진)**: 배치 2번째 이후 픽처럼 Y가 전진 영역에 있고 신규 다이의
    Y 타깃까지 잔여가 미세 보정 수준 → R3의 **기존 순서 유지**(Y 미세보정 선행 + PrePick
    오버랩).
- 판정 기준(D6): 기존 판정 자산 재사용 — `IsPickerYInAvoidPosition()`(또는
  `PickerZoneInterlockRules.IsPickerYOutOrMovingOut` 계열)로 "전진 영역" 여부를 보고,
  전진 영역이면 경로 B. 신규 임계 설정을 만들지 말고 기존 판정을 우선 조사·재사용하되,
  적합한 판정이 없으면 보고 후 결정.
- 분기 결과를 사이클 시작 로그에 남긴다 (예: "yForwardEntry=true/false").

### R2. 경로 A (Y 미전진 진입) — X 우선 + 클리어런스 게이트 + Y 전진 + Z 하강
- [0]에서 **PickerY 제거, PickerT만 선행 보정** (로그/주석 표기 수정).
- [5-2] X 접근 감시와 [6] PrePick 오버랩 하강은 **경로 A에서 수행하지 않는다**
  (Y 후퇴 상태에서 Z가 내려가는 사고 구조 방지 — 불변식).
- [7] 이송 합류(`Task.WhenAll`) 성공 **직후** 신규 [7.5]: `MovePickerAxesAndVerifyAsync`에
  **PickerY 단독** 타깃으로 호출 (targetName은 기존 [0]과 동일한 전진 타깃명 유지 —
  `IsForwardPickerYMoveTarget` 판정이 통과해야 게이트가 적용됨. AvoidPosition 계열 이름 금지).
  이 호출 하나로 상대 픽커 Y/존/페이즈 확인 + FacingY X 클리어런스 확인 + **미충족 시
  해제까지 무한 대기**(ct/사이클스톱 감시 포함)가 기성 코드로 수행된다. 별도 폴링 루프 금지.
- Y 전진 완료 후 `SetPickerVacuum(_currentPickerNo, true)` → **[8'] PickerZ 하강**: 기존
  `MoveFastPickerZToPickWithSlowZoneAsync`(명령 전용 + 저속존 오버라이드 + UpdateStatus 펌핑)
  그대로 사용. 정지 상태 시작이므로 새 Move 명령 경로로 진입 (TryOverridePosition 분기는
  코드 유지 — 이 경로에서 자연히 미사용).
- Y 전진 실패 시 기존 Fast 실패 처리(안전 복귀 후 Fail) 경로.

### R3. 경로 B (Y 이미 전진) — 기존 순서·오버랩 유지
- **현행 사이클 그대로**: [0] Y 미세보정+T 선행(기존 게이트 체인 통과 — 이미 클리어런스
  확인+해제 대기 포함) → [3] X 이송 → [5-1] StageY∥NeedleX → [5-2] X 잔여
  `FastPickerXApproachDistance` 트리거 → [6] PrePick 선행 하강 + Picker Vacuum ON →
  [7] 합류 → [8][8-1] Pick 하강(이동 중이면 포지션 오버라이드).
- 이 경로는 코드 변경 없이 기존 블록을 재사용한다 — 분기만 추가.
- 근거: Y가 이미 전진 상태이므로 "Y 전진 후 Z 하강" 불변식이 X 이송 중에도 충족되어
  PrePick 오버랩이 유효하다. `FastPickerXApproachDistance`/`PickerZPrePickDistance`는
  경로 B에서 **계속 사용된다** (삭제·미사용 처리 금지).

### R4. 이후 단계 무변경 (양 경로 공통)
- [9] SyncLift ~ [14] EjectPinZ 복귀, 실패 처리, 배치 마지막 픽 안전 복귀 로직은 그대로.
- [2] Z 안전 확인, [4] EjectPinZ 확인, [5-1] StageY∥NeedleX 비동기 + Needle Vacuum ON도 그대로
  (StageY/NeedleX는 픽커 축이 아니므로 X와 병렬 유지).
- 주의: [2] Z 안전 확인(4피커 Z Avoid 기준)과 경로 B의 전제(직전 픽에서 Z가 Avoid 복귀)가
  일관되는지 구현 중 확인하고 어긋나면 보고.

### R5. 기존 경로 보호
- default/Conti 경로, `MovePickerAxesAndVerifyAsync`/게이트/`PickerZoneInterlockRules` 등
  공용 코드는 **무수정**. 이번 작업은 Fast 사이클 본체의 순서 재배치만이다.

## 결정사항 (박제 — 변경 필요 시 보고)

- **D1. 경로 A의 Z 하강 단순화**: 경로 A에서 Z는 정지 상태에서 한 번에 내려가므로 PrePick
  중간 정지 없이 `MoveFastPickerZToPickWithSlowZoneAsync` 단일 하강으로 통일한다.
  **경로 B의 [6] PrePick 블록은 그대로 유지** (조건 분기이지 삭제가 아님).
- **D2. 대기 방식**: 클리어런스 게이트는 기성 무한 대기(타임아웃 없음, ct/사이클스톱 감시)를
  그대로 따른다 — 사용자 요구("실패하면 계속 대기, 풀리면 이동")와 일치. 타임아웃 Fail을
  추가하지 않는다.
- **D3. 택트 영향 명시**: 오버랩 상실은 **경로 A(Y 미전진 진입 — 통상 배치 첫 픽 1회)에
  한정**된다. 경로 B(배치 2~4번째 픽)는 기존 택트 유지. 레포트에 경로별 영향과 배치당
  추가 시간(대략 첫 픽의 Y 전진+Z 하강 직렬화분)을 정량 명시한다.
- **D4. T 선행 유지**: PickerT 회전 보정은 양 경로 모두 X 이송 전 선행 유지 (Y와 달리
  돌출 간섭 없음).
- **D5. Y 전진 게이트 재사용**: 신규 인터락/설정을 만들지 않는다. 판정·대기·클리어런스 값
  전부 기성 자산(`MovePickerAxesAndVerifyAsync` 게이트 체인 + `PickerYFacingXClearance`).
- **D6. Y 전진 판정 재사용**: 경로 분기 판정도 신규 설정 없이 기존 판정
  (`IsPickerYInAvoidPosition` / `IsPickerYOutOrMovingOut` 계열)을 조사·재사용. 코드에서
  적합한 판정을 찾은 근거(행 인용)를 체크리스트에 기록하고, 애매하면 보수적으로
  경로 A(안전 순서)를 태운다 — 오판 시 결과가 "느려짐"이지 "위험"이 아니도록.

## 검증 (수용 기준)

- V1. 빌드 통과(`/t:Build`, 지정 OutDir), 신규 경고 0.
- V2. 경로 A 순서 검증 — 코드 리뷰 + 시뮬 하네스(안무 재현): ① X 이송 완료(합류) 전에
  PickerY 이동 명령이 발행되지 않는다 ② Y 전진 완료 전에 PickerZ 하강 명령이 발행되지 않는다
  ③ Z 하강은 저속존 전환 → InPosition까지 기존과 동일 동작.
- V3. 경로 B 회귀 검증 — Y 전진 상태 진입 시 기존 안무 그대로: [0] Y 미세보정 → X 이송 중
  잔여 트리거 → PrePick 선행 하강 → 합류 → 오버라이드 Pick 하강 (기존 하네스
  FastContiPickupHarness의 V2 안무와 동일하게 재현·통과).
- V4. 분기 판정 검증 — Y 전진/미전진 각각에서 올바른 경로 선택 (판정 함수 단위 검증 +
  사이클 시작 로그 확인). 애매 상태(경계값)에서 경로 A로 폴백함을 확인.
- V5. 게이트 대기 검증 — 게이트 차단 상황(상대 픽커 Y 돌출 + X 경로 클리어런스 내 모사 또는
  판정 함수 단위 검증)에서 Y 이동이 발행되지 않고 대기하며, 해제 시 이동이 발행됨.
  시퀀스 통합 모사가 어려우면 `CanMovePickerAxisByFacingYInterlock` 판정 단위 검증 +
  게이트 루프 코드 리뷰로 대체하고 그 사실을 레포트에 명시.
- V6. 회귀 — default/Conti 경로 및 공용 게이트 코드 diff 0. Fast [9]~[14] 무변경 확인.
  `FastPickerXApproachDistance`/`PickerZPrePickDistance`가 경로 B에서 계속 사용됨을 확인.
- V7. 레포트 — 변경 전/후 순서 표(경로 A/B 각각), 택트 영향(D3 — 경로 A 한정), 게이트·판정
  재사용 근거(행 인용), 현장 확인 항목(배치 첫 픽 사이클 타임 실측, 게이트 대기 발생 빈도
  로그, 분기 판정 로그 정합).

## 진행 방법

계획 → 체크리스트 작성(`cdt-320\codex-work\11_fast-conti-x-first-y-gate_checklist.md`) →
구현 → 체크리스트 확인(실패 시 구현→확인 3회 반복) → 작업 내용 레포트.
