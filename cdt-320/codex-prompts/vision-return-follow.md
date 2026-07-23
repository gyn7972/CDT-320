# 작업: 피커 퇴장 ∥ 비전 복귀 동시 기동 — 비전이 피커를 FollowMoveAsync로 추종 진입 (Input/Output 공통)

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Sequencing\Picker\InputDieVisionPrepareSequence.cs` (웨이퍼 비전 복귀 진입)
  - `QMC.CDT-320\Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs` (빈 비전 검사 진입)
  - 필요 시 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` (선행 의존 미충족 시)
- **인터락/충돌 판정 코드 수정 금지** (R4).

## 선행 의존 (작업 시작 전 존재 확인 — 없으면 해당 사양대로 함께 구현)
1. `AjinAxis.FollowMoveAsync(...)` — 2축 팔로잉 (`cdt-320\codex-prompts\axis-follow-move.md`).
   **타임아웃 인자(`timeoutMs`, 0 이하 = 기본 5000)** 가 이미 인자화되어 있는지 확인
   (`vision-picker-follow-entry.md` R1). 없으면 그 사양대로 인자화한다.
2. SharedRailX 설정: `InputVisionRetreatExtraClearance` / `OutputVisionRetreatExtraClearance`
   (기본 40, `vision-minimal-retreat.md`) 및 `VisionFollowEntryTimeoutMs`(기본 15000,
   `vision-picker-follow-entry.md` R2). **타임아웃은 신설하지 않고 `VisionFollowEntryTimeoutMs`를
   그대로 재사용한다** (사용자 확정).

## 목적

피커가 픽업/플레이스를 마치고 존에서 퇴장할 때, 비전 축이 존 클리어 완료를 기다리지 않고
**퇴장하는 피커를 후행으로 추종하며 촬영/검사 위치로 진입**한다:

```
[현재]  피커 퇴장 → 존 클리어/공유레일 클리어 "대기" → 비전 진입   (순차)
[변경]  피커 퇴장 이동(선행축, 픽업/플레이스 시퀀스가 이미 명령해 둠)
        └→ 비전.FollowMoveAsync(선행=피커X, ...) 동시 호출
           비전이 안전거리 유지하며 피커가 빠지는 만큼 실시간 진입   (오버랩)
```

앞선 `vision-picker-follow-entry.md`(피커가 비전을 추종)와 **역할이 정확히 반대**다.

## 파라미터 매핑 (확정)

| FollowMoveAsync 인자 | 웨이퍼(Input) — 픽업 완료 후 | 빈(Output) — 플레이스 완료 후 |
|---|---|---|
| this(후행축) | **InputVisionX** | **OutputVisionX** |
| leadingAxis(선행축) | 제약이 되는 피커X (R2 규칙) | 동일 |
| 선행 목표(정보용) | 피커 퇴장 목표 (모름/불요 시 현재 Command) | 동일 |
| 후행 목표 | 다음 die 촬영 위치 X | placed die 검사 위치 X |
| direction | **+1** (피커 +방향 퇴장, 비전 +방향 진입) | **−1** (피커 −방향 퇴장, 비전 −방향 진입) |
| homeGap | 페어 HomeClearance — **런타임 조회** (현장값 88.5) | 동일 (현장값 510) |
| safetyGap | 페어 SafetyDistance + InputExtra = **10+40=50** | SafetyDistance + OutputExtra = **50** |
| timeoutMs | `VisionFollowEntryTimeoutMs` (재사용) | 동일 |

간격 공식 정합 (기존 페어 수식과 동일함을 주석으로 명시할 것):
```
Input(+방향):  거리 = (선행 + HomeGap) − 후행 = 피커 + 88.5 − 비전   (= 페어 간격식 ✓)
Output(−방향): 거리 = (후행 + HomeGap) − 선행 = 비전 + 510 − 피커    (= 페어 간격식 ✓)
```

수치 검증 기준 (현장 설정값 기준 — 이 결과가 그대로 나와야 한다):
- Input: 피커 620→750 퇴장, 비전 목표 680 → 비전 진입 한계 = 피커 + 38.5.
  피커 620일 때 658.5까지, 피커가 641.5를 지나는 순간 680 도달 가능. 전 구간 간격 ≥ 50.
- Output: 피커 540→400 퇴장, 비전 목표 30 → 비전 한계 = 피커 − 460.
  피커 540일 때 80까지, 피커가 490을 지나는 순간 30 도달 가능. 전 구간 간격 ≥ 50.

## 요구사항

### R1. 적용 지점 (확정된 2곳)

**① 웨이퍼 비전 — `InputDieVisionPrepareSequence.MoveInputStageAndVisionToDieAsync`**
- 현재: `WaitFrontRearPickerInputZonesClearBeforeVisionAsync()`가 피커 존 클리어를 **블로킹
  대기**한 뒤 `MoveInputStageVisionPointForPickerAsync()`로 VisionX/NeedleX/StageY 이동.
- 변경 (Conti 게이트 충족 시):
  1. 존 클리어 블로킹 대기를 생략하고, **VisionX 진입을
     `InputVisionX.FollowMoveAsync(선행=제약 피커X, 후행목표=촬영 X, +1, 50, 88.5, timeout)`으로
     대체**한다 — follow의 간격 제약이 대기를 대신한다.
  2. VisionX 이동 경로 결정 로직(L자 경로: `MoveInputStageVisionPointForPickerAsync` 내부)에서
     VisionX 단독 이동에 해당하는 부분만 follow로 대체하고, NeedleX/StageY 이동·순서 로직은
     유지한다.
  3. **StageY/NeedleX 이동의 안전 전제 확인**: 기존 존 클리어 대기가 보호하던 대상을 코드로
     분석해서, "피커 Z 전체 Avoid 확인"(기존 `MovePickersToAvoidForInputVisionMove` 단계 등)이
     StageY/NeedleX 이동 전에 보장되는지 확인하고, 부족하면 해당 확인을 유지/추가한다.
     분석 결과를 보고서에 명시한다.
- 게이트 미충족 시: 기존 블로킹 대기 + 일반 이동 그대로.

**② 빈 비전 — `OutputPostPlaceInspectionQueue`**
- 현재: `WaitOutputVisionXSharedRailClearAsync()` (약 1085행)가 공유레일/가드 클리어를
  **폴링 대기**한 뒤 VisionX를 검사 위치로 이동.
- 변경 (Conti 게이트 충족 시): 이 대기를 생략하고 검사 위치 이동을
  `OutputVisionX.FollowMoveAsync(선행=제약 피커X, 후행목표=검사 X, −1, 50, 510, timeout)`으로
  대체한다. VisionX 외 축(StageY/Z 등)의 기존 이동·순서는 유지.
- 게이트 미충족 시: 기존 대기 + 일반 이동 그대로.

### R2. 선행 피커 선택 — 방향/이동 여부 판단 없음 (사용자 확정)
- **판단 로직 없이 항상 follow를 시도한다.** 선행 피커가 정지 상태여도 follow를 시작한다 —
  follow는 여유(간격−안전거리) ≤ 0이면 명령 없이 대기하고, 피커가 추후 외부 기동으로
  움직이면 그때부터 추종한다. 피커가 끝내 안 움직이면 타임아웃 → R5 폴백.
- **역방향 판단은 하지 않는다.** 안전 근거(코드 주석으로 명시할 것):
  - 피커가 비전 쪽으로 접근하는 이동은 피커 자신의 인터락(SafetyDistance 10)이 차단한다.
  - follow는 후행축을 전진만 시키고 후퇴시키지 않으므로, 간격 하한은 인터락이 보장한다.
- **선행축 선택 규칙**: Front/Rear 피커 중 페어 간격식으로 계산한 **여유가 더 작은(제약이
  되는) 축** 1개를 호출 시점의 ActualPosition으로 선택한다.
  - Input(+1): X가 **작은** 피커가 제약 → 선행으로 선택.
  - Output(−1): X가 **큰** 피커가 제약 → 선행으로 선택.
  - 선택되지 않은 다른 피커와의 충돌은 follow 내부 이동/오버라이드가 MotionGuard를 통과하며
    검증된다 — 위반 시도 시 인터락 거부(-11) → R5 폴백. (주석으로 명시)
- **이미 존이 비어 제약이 없으면**(비전 목표가 두 피커 모두에 대해 안전) follow 없이 기존
  일반 이동을 사용한다 — 배치 내 2번째 이후 촬영 이동은 자연히 일반 이동이 된다.

### R3. 게이트
- `Options.RunMode == Auto`(큐는 자동 운전 컨텍스트 확인) **그리고** Conti 계열:
  - Input: `PickerPickUpMotionConfig.TransferMotionMode` ∈ Conti 계열
    (`ContiSegmentedPickUp`/`FastContiSegmentedPickUp`)
  - Output: `PickerPlaceMotionConfig.MotionMode` — `IsCoordinatedPlaceMotionMode` 판정
- 게이트 미충족 시 기존 경로 완전 무변경.

### R4. 인터락 — 우회 절대 금지 + 경로 통과 검증 (필수)
- `BeginMotionGuardBypass` 등 우회 API 사용 절대 금지.
- follow 내부 이동/오버라이드 경로가 공유레일 축에서 `MotionGuardRuntime`(SharedRailX 충돌
  검증 포함)을 통과하는 **호출 체인을 보고서로 증명**한다.
- 인터락/충돌 판정 코드 diff 0건.

### R5. 실패 처리 / 폴백
- follow 실패(타임아웃 -21, 인터락 -11, 선행축 알람 -22 등) 시:
  1. 비전 축 정지 확인(함수가 정지 처리),
  2. **기존 경로(존/레일 클리어 대기 → 일반 이동)로 1회 재시도**,
  3. 재시도 실패 시 기존 Fail/에러 관례.
- 취소(ct) 전파와 태스크 observe는 기존 관례.

## 제약 사항
- homeGap/safetyGap/timeout 하드코딩 금지 — SharedRailX 설정에서 런타임 조회
  (페어는 비전축↔선택된 피커축으로 정확히 매칭).
- 비Conti/수동/캘리브레이션 경로 무변경. `vision-picker-follow-entry` 작업과 동일 파일을
  건드릴 수 있으므로 충돌 없이 공존(두 기능은 방향이 반대일 뿐 독립).
- 시뮬레이션 모드 동작 필수 (follow 시뮬 분기 경유, 별도 분기 금지).
- 코드 스타일: 기존 관례 (한국어 로그 Start/Ok/Failed, F6 포맷).

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과.
2. **수치 정합성** (시뮬 하네스, 실측 설정 88.5/510/10/40, 결과 보고):
   - Input: 피커 620→750 퇴장과 동시에 비전 진입 시작 → 전 구간 간격(피커+88.5−비전) ≥ 50
     (매 폴링 로그), 비전 최종 680 도달, 피커 641.5 통과 시점 근처에서 비전 680 도달.
   - Output: 피커 540→400 퇴장 + 비전 목표 30 → 간격(비전+510−피커) ≥ 50, 최종 30 도달.
   - 전 구간 인터락 요구(10) 침범 0회.
3. **정지 선행축 케이스**: 피커를 존 안에 정지시켜 두고 follow 시작 → 비전이 한계 위치까지만
   진입 후 대기 → 피커를 외부에서 퇴장 이동 → 비전이 이어서 목표 도달. 피커를 계속 정지시키면
   타임아웃 후 폴백 경로로 정상 완료.
4. **오버랩 검증**: 로그 타임스탬프로 "피커 퇴장 시작 ≈ 비전 진입 시작" 확인 (기존 순차 대비).
5. **인터락 검증(필수)**: 호출 체인 보고 + 우회 API 미사용 grep 증명 + 인터락 diff 0건 +
   시뮬 사이클 인터락 위반/알람 0건.
6. 존이 이미 빈 경우(2번째 이후 촬영) follow를 타지 않고 일반 이동함을 로그로 확인.
7. 비Conti 모드 회귀 없음 (기존 대기/이동 경로 동일).
8. R1-①-3의 "존 클리어 대기가 보호하던 대상" 분석 결과와 유지/추가한 안전 확인 목록 보고.
