# 작업: 모션 구동/대기 구조 단순화 — AxisMoveWaiter 전면 삭제

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - 삭제: `QMC.Common\Motion\AxisMoveWaiter.cs` (클래스·`AxisMoveWaitResult`·`AxisMoveWaitFailure` 전부)
  - `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` (MoveAbsoluteAsync 재작성)
  - `QMC.Common\Motion\BaseAxis.cs` (시뮬 완료 대기 정리)
  - 사용처 전체 마이그레이션: `MachineController.cs`, `VisionUnit.cs`, `PickerFrontUnit.cs`,
    `PickerRearUnit.cs`, `OutputStageUnit.cs`, `InputStageUnit.cs`, `WaferStageUnit.cs`,
    `RearPickerSequence.cs`, `MotionTestDialog.cs` 등 — **`AxisMoveWaiter` 참조를 grep으로
    전수 조사해서 하나도 남기지 말 것** (삭제 파일이므로 남으면 빌드 에러로 드러난다)
- 삭제한 파일은 `.csproj`에서도 제거.

## 배경 — 현재 구조의 문제 (수정 전 반드시 읽을 것)

완료 대기가 3겹으로 중복되어 있다:
1. `AjinAxis.MoveAbsoluteAsync` (약 860~1032행): 검사 → `AXM.MovePosition`을 **1초 재시도
   루프**로 호출 → 내부 `WaitUntilMoveDone(motionStopSerial)` 자체 대기 → 리턴.
2. 호출부(유닛/시퀀스의 `Move...AndVerifyAsync` 헬퍼)가 Move 완료 후 **또**
   `AxisMoveWaiter.WaitMoveDoneInPositionAsync`(INP 신호 + tolerance 이중 판정 + settle 재확인
   + 실패 7종 분류)를 호출.
3. 그 뒤에 **또** 스냅샷 재검증(`CheckPickerAxisInPosition` / `CheckInputStageAxisInPosition` 등)
   으로 같은 목표를 다시 비교.

목표: **"이동 함수가 완료를 보장한다"** 단일 원칙으로 정리한다.
- 동기 사용: `await MoveAbsoluteAsync(...)` 리턴 0 = 완료.
- 비동기 사용: Task 보관 → 나중에 Task 완료 확인 + 리턴 0 확인 = 끝.

## 요구사항

### R1. `AjinAxis.MoveAbsoluteAsync` 재작성 (실장비 경로)
유지할 것: `UpdateStatus()` 선행, 이동 생략(스킵) 판정, MotionGuard 인터락, 서보/알람/
소프트리밋 검사, 리밋 복구 분기, `MotionSpeedScale` 속도/가감속 스케일, 보드 단위 환산,
모션 프로파일 로그, `RaiseMoveStarted`/상태 플래그 갱신, `FailMotion` 에러 기록 관례.

교체할 것:
1. **`AXM.MovePosition` 1회 호출** — 기존 1초 재시도 루프 제거. 실패(ret≠0)면 기존 관례로
   `FailMotion` 반환.
2. **완료 대기 = GetInMotion 폴링 루프**로 단순화:
   ```
   while (true)
   {
       (10ms 대기, ct/정지시리얼/알람 확인)
       UpdateStatus();                       // ActualPosition 등 갱신 유지
       AXM.GetInMotion(AxisNo, ref inMotion) // 정확한 래퍼명은 AXM에서 확인
                                             // (AxmStatusReadInMotion 계열 — 없으면 동일 패턴으로 public 래퍼 추가)
       if (!inMotion) break;
   }
   ```
   - 루프 중 확인: ① `_motionStopSerial` 변경(정지 요청) → 기존 -4 관례로 반환,
     ② `IsAlarm` → 알람 코드 반환, ③ 타임아웃(무한 방지용 상한 — 기존 `WaitUntilMoveDone`의
     타임아웃 관례가 있으면 유지, 없으면 60초) → 실패 반환.
3. **리턴 전 최종 확인 1가지만**: 보드 Command 위치(축 `CommandPosition`)와 `targetPos`가
   `Config.InPositionTolerance` 이내인지 확인. 이내면 `ClearMotionFailure()` 후 0,
   벗어나면 `FailMotion` 반환.
   - **INP 신호 확인, ActualPosition tolerance 검증, settle 재확인은 전부 제거한다**
     (Actual−Command 잔차는 서보 책임이라는 설계 결정 — 사용자 승인 완료).
4. 기존 내부 `WaitUntilMoveDone(motionStopSerial)`은 위 루프로 대체(또는 내부 구현을 위
   루프로 교체). `MoveAbsoluteCommandOnlyAsync`의 스킵 판정도 R3의 내부 헬퍼로 전환.

### R2. 이동 생략(스킵) 판정 내부화
`AxisMoveWaiter.CanSkipMoveCommandAtTarget` 삭제에 따라, `BaseAxis`(또는 `AjinAxis`)에
**private/protected 헬퍼 1개**로 대체한다:
```
스킵 가능 = !IsMoving && !IsAlarm && IsServoOn
          && |ActualPosition − target| <= InPositionTolerance
          && |CommandPosition − target| <= InPositionTolerance
```
- `BaseAxis.MoveAbsoluteAsync`/`MoveAbsoluteCommandOnlyAsync`(약 448/516행)와
  `AjinAxis`의 스킵 지점들이 이 헬퍼를 사용.
- **호출부(유닛/시퀀스)의 외부 스킵 체크는 제거** — 그냥 Move를 호출하면 내부에서 스킵된다.
  (`CanSkipPickerMoveCommand`, `CanSkipOutputFeederMoveCommand` 등 래퍼가 있으면 내부 헬퍼
  호출로 바꾸거나 호출부에서 제거 — 각 지점에서 단순한 쪽을 선택하고 일관되게)

### R3. AxisMoveWaiter 사용처 전수 마이그레이션 (유형별 규칙)
`grep AxisMoveWaiter`로 전 사용처를 나열한 뒤 아래 규칙으로 치환한다:

| 유형 | 현재 | 치환 |
|---|---|---|
| Move 직후 재대기 | `await Move...` → `WaitMoveDoneInPositionAsync(...)` | Wait 호출 **삭제**, Move 리턴 0 확인만 |
| 비동기 합류 | Move Task 보관 → 별도 시점에 `WaitMoveDoneInPositionAsync` | **Task await + 리턴 0 확인** (Task를 보관하도록 호출부 정리) |
| 스킵 판정 | `CanSkipMoveCommandAtTarget(...)` | 호출부 체크 제거 (R2 내부 스킵) |
| 완료 스냅샷 판정 | `IsMoveCompletedAtTarget(...)` | R2 헬퍼 노출이 필요한 곳만 public 헬퍼 사용, 그 외 제거 |
| 로그/알람 문자열 | `FormatResult`/`BuildAxisState`/`ResolveAlarmCode` | 실패 시 `axis.LastMotionFailureMessage` 사용 (축이 이미 상세 메시지 생성). 알람코드 분류 7종은 단일 코드(`prefix + "-MOVE"`)로 단순화 |

특기 지점:
- `AjinAxis`의 팔로잉 함수(약 613~736행)와 Step Jog(약 1743행): 내부 대기를 R1 루프/Task
  기반으로 정리.
- `BaseAxis.MoveJogStepAsync`(약 1110행): `MoveRelativeAsync`가 완료를 보장하므로 뒤따르는
  `WaitMoveDoneInPositionAsync` 제거.
- `MachineController` 약 11488행: `WaitMoveDoneInPositionAsync`를 그대로 반환하는 래퍼 —
  호출 구조를 Task 기반으로 정리.
- `MotionTestDialog`: UI 테스트 경로도 동일 규칙 적용.

### R4. 3겹째 스냅샷 재검증 제거 (사용자 승인 완료)
- **"방금 완료된 Move의 목표를 같은 흐름에서 다시 비교하는" 재검증 코드만 제거**한다:
  `Move...AndVerifyAsync` 헬퍼 내부의 이동 후 `Check...InPosition` 호출,
  Wait 성공 직후의 중복 스냅샷 비교 등.
- **유지할 것**: 이동과 무관한 사전조건/안전 게이트 검사 — 예: SyncLift 시작 전 3축 위치
  확인, VerifyPickTarget(다른 시점에 이동한 축들의 집합 검증), 인터락용 위치 확인.
  구분 기준: "직전 라인에서 완료 확인이 끝난 같은 축·같은 목표"의 재확인이면 제거,
  아니면 유지. 애매하면 유지하고 보고서에 목록을 남긴다.

### R5. 시뮬레이션 경로 (BaseAxis)
- 기존 사다리꼴 프로파일 시뮬 엔진(이동 시간 = 속도/가감속으로 계산, 경과시간 기반 위치
  갱신, 완료 시 목표 확정)은 **그대로 유지**한다 — 이것이 사용자가 원하는
  "무브 시점에 가감속·속도로 계산된 시간만큼 대기하며 위치값 업데이트" 동작이다.
- `BaseAxis` 내 `AxisMoveWaiter` 의존(스킵 판정 448/516행, MoveJogStep 1110행)만 R2/R3
  규칙으로 제거. 시뮬 완료 판정은 기존 `CompleteSimulationMove` 경로 유지.
- 시뮬/실장비 모두 "MoveAbsoluteAsync 리턴 = 완료"의 의미가 동일해야 한다.

## 제약 사항
- 시퀀스들의 상위 로직(스텝 전이, 병렬 구조, 인터락)은 변경하지 않는다 — 이번 작업은
  "이동+대기+검증" 층의 단순화만.
- 코드 스타일: 기존 관례(한국어 주석/로그, `FailMotion`/`Fail`, 최신 C# 문법 자제).
- `AxisMoveWaiter.cs` 삭제 후 솔루션 전체에서 참조 0건이어야 한다 (컴파일로 강제됨).
- 동작 변경 승인 사항(보고서에 명시할 것): INP 신호/Actual tolerance/settle 검증 제거,
  실패 분류 7종 → 단순화, 이동 후 스냅샷 재검증 제거.

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과. `AxisMoveWaiter` 참조 0건 (파일·csproj 제거 포함).
2. 시뮬레이션 하네스로 확인 (결과 보고):
   - 단일 절대이동: 리턴 0 + Command≈Target(톨러런스 이내) + 프로파일 계산 시간과 유사한
     소요 시간.
   - 이미 목표 위치에서 재호출: 즉시 0 (내부 스킵).
   - 비동기 합류: Task 보관 → 나중에 await → 0 확인 패턴 동작.
   - 이동 중 Stop(): -4 관례 반환. 이동 중 알람: 알람 코드 반환.
3. 자동 시퀀스 대표 경로(픽업/플레이스 1사이클, 시뮬 모드) 회귀 실행 — 스텝 전이가 기존과
   동일하게 완료되는지 확인.
4. 제거한 스냅샷 재검증 지점 목록과 "유지" 판단한 애매 지점 목록을 보고서로 제출.
5. `AXM.GetInMotion` 래퍼의 실제 이름/시그니처(신규 추가 여부 포함)를 보고서에 명시.
