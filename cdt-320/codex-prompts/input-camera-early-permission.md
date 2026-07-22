# 작업: InputCamera 선행검사 — 픽업 허가를 EPD 시점으로 앞당기고 RESULT 대기를 CalculatePickTargets로 이동

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Sequencing\Picker\InputCameraMarkInspectionSequence.cs`
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
  - `QMC.CDT-320\Sequencing\Picker\InputDieVisionPrepareSequence.cs` (회수 로직 재사용을 위한 최소 노출)
- 목적: InputCamera Mark 선행검사에서 **비전 RESULT 처리 시간이 픽업 허가를 지연시키지 않게** 한다.
  촬영(EPD)과 VisionX 회피가 끝나면 즉시 픽업 허가를 발행하고, RESULT는 픽업 시퀀스가
  **실제로 VisionOffset이 필요한 시점(`CalculatePickTargets`)에서** 회수한다.

## 배경 — 현재 구조 (수정 전 반드시 해당 파일들을 읽을 것)

### 현재 흐름 (문제)
`InputCameraMarkInspectionSequence.RunInputCameraMarkInspectionAsync()` (약 175행):
1. 존/리소스 획득 (선행검사 모드: `AutoSequenceGate.BeginInputCameraWorkAsync`)
2. `InputDieVisionPrepareSequence.RunAsync()` — Die당(피커 4→1): 촬영 위치 이동 → correlated
   MATCH 요청 → **EPD만 대기하고 즉시 다음 Die** (핸들 `VisionRequest` 보관, `ExposureCompleted=true`)
3. **★문제**: `Task.WhenAll(CollectVisionResultsAsync, MoveInputVisionXToAvoidAsync)` (약 213~218행)
   — RESULT 4건을 **순차 동기 회수**한 뒤에야 다음으로 진행
4. `GrantPickUpPermission()` (약 460행) — RESULT까지 완비된 배치를
   `InputCameraPickUpPermissionStore.Grant(Side, items)`로 발행
5. `PickerPickUpSequence.TryLoadInputCameraMarkInspectionPermission()` (약 530행)이
   `TryConsume`으로 수신 — 현재 `permitted.VisionOffset == null`이면 **Fail** (약 618행)
6. 픽업은 `CalculatePickTargets`(약 1128행, 동기 메서드)에서 `_visionOffset`을 사용해 좌표 계산

### RESULT 회수 로직 (재사용 대상)
`InputDieVisionPrepareSequence.CollectVisionResultsAsync()` (약 647행)의 Die 1건 회수 코어:
```
AutoVisionRequestService.WaitInspectionStageAsync(item.VisionRequest, Result, timeout, ct)
→ MatchResultDto → VisionCameraCalibrationTransform.ToAlignResult(Wafer, match, 0.0)
→ 성공 시 WaferVisionResultStore.RecordAlign(InputPickDie, offset)
→ 실패 시 해당 Die SKIP(예약 해제) 처리
```
실패 정리 패턴: EPD 완료·RESULT 미회수 핸들은 `WaitInspectionStageAsync`로 회수 시도 후
`MarkError` (같은 파일 약 100~141행).

### 데이터 구조 (확인 완료)
- `InputDieVisionPreparedItem` — `VisionRequest`(핸들), `ExposureCompleted`, `VisionRequestIndex`,
  `VisionOffset` 필드 **이미 보유**.
- `PickerPickUpSequence.PickUpBatchItem` (약 64행) — `VisionOffset`만 있고
  **`VisionRequest`/`VisionRequestIndex` 없음 → 필드 추가 필요**.
- 시뮬/DryRun 경로: prepare 시퀀스가 촬영 시점에 `VisionOffset`을 즉시 채움
  (`SimulateInputVisionOffset`) — 핸들 없이 offset 보유 상태로 온다.

## 요구사항

### R1. InputCameraMarkInspectionSequence — 허가를 EPD 시점으로 앞당김
`RunInputCameraMarkInspectionAsync()`에서:
- `CollectVisionResultsAsync` 호출을 **제거**한다 (이 시퀀스는 더 이상 RESULT를 기다리지 않는다).
- `MoveInputVisionXToAvoidAsync(ct)`는 **await로 완료 확인** 후 (피커 진입 안전 확보),
  RESULT 없이 `_inspectedItems`(핸들 보유 상태)를 채우고 `GrantPickUpPermission`으로 진행한다.
- 허가 로그에 "RESULT는 PickUp CalculatePickTargets에서 회수" 취지를 명시한다.
- 이 시퀀스의 실패 경로(허가 발행 전 실패)에서는 기존 실패 정리 패턴대로 EPD 완료 핸들을
  드레인(`WaitInspectionStageAsync` 시도 후 `MarkError`)하고 예약을 해제한다 —
  `ReleasePreparedReservations` 기존 호출 유지.

### R2. 허가 수신 검증 완화 (PickerPickUpSequence.TryLoadInputCameraMarkInspectionPermission)
- 항목 검증을 다음으로 변경: `VisionOffset != null` **또는**
  (`ExposureCompleted && VisionRequest != null`) 이면 유효.
  둘 다 없으면 기존대로 Fail ("허가에 VisionOffset도 Vision 핸들도 없습니다" 취지로 메시지 갱신).
- `PickUpBatchItem`에 `VisionRequest`(핸들)와 `VisionRequestIndex` 필드를 추가하고
  허가 항목에서 복사한다 (기존 두 곳의 batchItem 생성부 모두).

### R3. RESULT 대기를 CalculatePickTargets로 이동 (PickerPickUpSequence)
- `CalculatePickTargets`를 async로 전환한다 (`CalculatePickTargetsAsync(bool, ct)`;
  스텝 디스패처의 `Task.FromResult(CalculatePickTargets(true))`를 직접 Task 호출로 변경).
- 배치 계산 루프에서 각 아이템 계산 **직전**에:
  - `VisionOffset != null`이면 그대로 계산 (기존 동작 — 시뮬/내부 경로 호환).
  - `VisionOffset == null && VisionRequest != null`이면 위 "RESULT 회수 코어"와 동일한 로직으로
    회수: `WaitInspectionStageAsync(RESULT)` → `ToAlignResult` → 성공 시
    `WaferVisionResultStore.RecordAlign` + `item.VisionOffset` 설정.
  - 회수 실패(또는 offset null) 시 해당 Die는 **SKIP**: pick 예약 해제
    (`MaterialStateService` 예약 해제 — prepare 시퀀스의 skip 처리와 동일 경로), 배치에서 제외,
    로그 남기고 다음 아이템 계속. 전부 SKIP되면 배치 완료 처리(Complete)로 정상 종료.
  - 회수 순서는 배치 순서(피커 4→1) 그대로.
- 회수 로직은 중복 구현하지 말 것: `InputDieVisionPrepareSequence`의 회수 코어를
  internal static 헬퍼로 추출해 양쪽(CollectVisionResultsAsync / 픽업 CalculatePickTargets)에서
  재사용하는 방식을 권장한다 (동작 불변 리팩터링).
- 타임아웃은 기존 `InputVisionTimeoutMs` 관례를 따른다.

### R4. 미소비/중단 시 핸들 정리
- 허가가 소비되지 않고 폐기되는 경로(`InputCameraPickUpPermissionStore.Clear`,
  `StopRemainingPickBatchForWaferCompletion`, 허가 복구 실패 등)에서 항목에 남은
  RESULT 미회수 핸들을 드레인(회수 시도 후 `MarkError`)하는 정리 헬퍼를 추가한다 —
  orphan waiter/미회수 RESULT가 비전 측에 남지 않게 한다.
- 픽업 시퀀스가 CalculatePickTargets 이전에 실패/취소로 종료되는 경우에도 배치에 남은
  핸들을 동일하게 드레인한다.

### R5. 범위 제한
- **픽업 내부 경로(`PrepareInputDieVisionBatchAsync` → RunAsync + CollectVisionResultsAsync)는
  변경하지 않는다** — 그 경로는 기존처럼 RESULT까지 회수한 배치로 CalculatePickTargets에 온다
  (R3의 "VisionOffset != null이면 그대로" 분기로 자연 호환).
- `InputDieVisionPrepareSequence.CollectVisionResultsAsync`의 기존 공개 동작(다른 호출처)은
  유지한다 (내부 코어 추출만).
- Vision 통신 방식(pull 폴링)은 이 작업에서 바꾸지 않는다 — push 전환은 별도 작업
  (`vision-push-result-store.md`). 이 작업 후에도 그 작업과 충돌하지 않아야 한다
  (회수 지점이 `WaitInspectionStageAsync` 하나로 모이므로 충돌 없음).

## 제약 사항
- 기존 시퀀스 스텝 enum/전이 구조 유지 (CalculatePickTargets 스텝의 async화만 허용).
- 코드 스타일: 한국어 로그(Start/Ok/Failed/Check 접미), `Fail` 관례, 최신 C# 문법 자제.
- 시뮬/DryRun: prepare가 offset을 즉시 채우므로 새 대기 분기를 타지 않는다 — 회귀 없어야 함.
- 스레드/취소: `WaitInspectionStageAsync` 대기 중 취소(ct)는 기존 관례대로 전파, 취소 시 핸들 드레인.

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과.
2. 선행검사 모드 시나리오 (시뮬 또는 하네스로 결과 보고):
   - 허가 발행 시점이 "마지막 Die EPD + VisionX Avoid 완료" 직후임을 로그 타임스탬프로 확인
     (RESULT 회수 완료를 기다리지 않음).
   - 픽업이 허가를 소비해 X/T·스테이지 이동을 시작한 뒤, `CalculatePickTargets`에서
     RESULT를 회수해 좌표를 계산하고 정상 픽업 완료.
   - RESULT 1건을 강제 실패시키면 해당 Die만 SKIP되고(예약 해제 확인) 나머지 3개는 정상 픽업.
   - 전부 실패 시 배치가 Complete로 정상 종료.
3. 내부 경로(허가 없이 픽업 자체 비전 흐름) 회귀 없음 — 기존과 동일 동작.
4. 시뮬/DryRun 회귀 없음 (offset 즉시 보유 경로).
5. 웨이퍼 완료 드레인/허가 Clear 경로에서 핸들 드레인이 수행됨을 로그로 확인.
6. `cdt-320\pickup-sequence-front-command-wait-analysis.md`의 해당 구간 설명이 바뀌므로,
   변경 후 실제 순서(허가 시점, RESULT 회수 위치)를 작업 보고에 타임라인으로 정리한다.
