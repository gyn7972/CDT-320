# Auto Sequence Flow 보완 작업 프롬프트 / 체크리스트

작성일: 2026-07-08

대상 코드:
- `QMC.CDT-320/Equipment/MachineController.cs`
- `QMC.CDT-320/Sequencing/InputSequence.cs`
- `QMC.CDT-320/Sequencing/OutputSequence.cs`
- `QMC.CDT-320/Sequencing/Picker/*`
- `QMC.CDT-320/Sequencing/InputStage/*`
- `QMC.CDT-320/Sequencing/OutputStage/*`
- `QMC.CDT-320/Sequencing/Common/*`

## 절대 규칙

- 코드 수정 전 반드시 분석 결과와 수정 후보를 먼저 보고한다.
- Interlock, MotionGuard, PickerZone, InputStage/OutputStage safety rule을 수정해야 할 가능성이 있으면 즉시 작업을 멈춘다.
- Interlock 관련 수정은 별도 `Interlock 수정 사항 리포트` 작성 후 사용자 승인을 받은 뒤에만 진행한다.
- 승인 전에는 Interlock 관련 코드 수정 금지.
- 검증은 `git diff --check`와 별도 `OutDir` MSBuild를 기본으로 한다.
- 기존 dirty worktree 변경은 되돌리지 않는다.

## 작업 프롬프트

아래 지시를 그대로 사용한다.

```text
CDT-320 Auto Sequence Flow를 보완한다.

목표:
1. Auto Start / Stop / CycleStop / Alarm / Resume이 안전하고 일관되게 동작하는지 확인한다.
2. Input / FrontPicker / RearPicker / Output 병렬 시퀀스의 resource lease, ready bus, material state 전환이 서로 충돌하지 않게 보완한다.
3. 조건별 이동, 공정, 좌표 계산식이 로그와 코드에서 추적 가능하도록 정리한다.
4. OutputFeeder 완료 wafer 복구 store 경로처럼 일반 store 경로와 안전 보호 수준이 다른 부분을 우선 검토한다.

진행 규칙:
- 먼저 관련 코드를 읽고 현재 Flow와 조건을 표로 정리한다.
- 코드 수정 전 문제 후보, 영향 범위, 수정 방향, 검증 방법을 보고한다.
- Interlock 또는 충돌 방지 규칙 수정이 필요하면 별도 Interlock 수정 사항 리포트를 작성하고 사용자 승인 전까지 코드를 수정하지 않는다.
- Interlock 외 Material/Sequence/Log 보완도 장비 동작 조건에 영향을 주면 승인 후 진행한다.
- 수정은 최소 범위로 제한한다.
- 수정 후 별도 OutDir 빌드와 시나리오별 로그 검증 체크리스트를 수행한다.
```

## 1. 분석 체크리스트

### 1.1 진입 / 종료 구조

- [ ] `StartAsync()` 진입 조건 확인
  - Alarm 상태 차단
  - 이미 실행 중 차단
  - ReadySequence 실행
  - Reticle Avoid 확인
  - Calibration Ready 확인
- [ ] `StartSequenceAsync()` 구성 확인
  - `SequenceSignalBus` 신규 생성
  - `SequenceResourceManager` 신규 생성
  - `InputSequence`, `OutputSequence`, `FrontPickerSequence`, `RearPickerSequence` 등록
  - Auto status 전환
- [ ] `StopAsync()` 동작 구분 확인
  - Auto 중에는 CycleStop 요청
  - Manual/Jog 중에는 축 Stop
  - 구형 `_cycleCts` 경로 잔존 영향 확인
- [ ] `StopSequenceAsync()` 즉시 cancel 경로 확인
- [ ] `StopSequenceForAlarmAsync()` 알람 cancel 경로 확인

### 1.2 병렬 시퀀스 구조

- [ ] `AutoSequenceCoordinator.RunAsync()` 병렬 task 생성 확인
- [ ] 첫 실패 시 처리 정책 확인
  - Critical alarm이면 즉시 abort
  - 일반 alarm이면 CycleStop 경계 대기
- [ ] pending task 대기 timeout 확인
  - CycleStop pending wait 5초
  - abort finish wait 3초
- [ ] timeout 후 Coordinator 종료 시 실제 축/모션 상태 로그 필요 여부 확인

### 1.3 Resource / Bus

- [ ] `InputStageArea` 점유 경로 확인
- [ ] `OutputPlaceArea` 점유 경로 확인
- [ ] `OutputGoodStageArea`, `OutputNgStageArea` 점유 경로 확인
- [ ] `FrontPicker`, `RearPicker` pause resource 점유 경로 확인
- [ ] `InputStageReady`, `InputStageFinishComplete`, `InputStageDieMapped` set/reset 시점 확인
- [ ] `InputStageDieComplete` set/reset 시점 확인
- [ ] `OutputGoodStageReady`, `OutputNgStageReady` set/reset 시점 확인
- [ ] `OutputGoodStageReceiveComplete`, `OutputNgStageReceiveComplete` set/reset 시점 확인

## 2. 보완 후보 체크리스트

### 2.1 P1 - OutputFeeder 완료 wafer 복구 store 경로

대상:
- `OutputSequence.ExecuteOutputFeederStoreToCassetteAsync(...)`

현재 의심:
- OutputFeeder에 완료된 bin wafer가 남은 상태로 resume하면 cassette unload로 바로 진행한다.
- 일반 `ExecuteStoreStageToCassetteAsync(...)`와 달리 다음 보호가 빠져 있을 수 있다.
  - FrontPicker resource
  - RearPicker resource
  - `OutputPlaceArea`
  - side stage area
  - Picker Avoid 확인

검토:
- [ ] 이 경로가 실제 stage/feeder/cassette 이동 중 OutputCamera/Picker와 간섭 가능한지 확인
- [ ] 일반 Store 경로와 필요한 보호 조건 비교표 작성
- [ ] 현재 하위 `OutputFeederUnloadToCassetteSequence` 내부 보호 조건 확인
- [ ] OutputPostPlaceInspectionQueue와 동시 실행 가능성 확인
- [ ] Interlock 수정 필요 여부 판정

승인 게이트:
- [ ] Interlock 수정 필요 없음: Sequence resource 보완만으로 충분하다고 근거 작성
- [ ] Interlock 수정 필요 있음: Interlock 수정 사항 리포트 작성
- [ ] 사용자 승인 완료 전 코드 수정 금지

검증:
- [ ] OutputFeeder 완료 wafer 잔류 상태에서 Auto resume
- [ ] 동시에 PickerPlace가 OutputReady 대기 중인 상태 검증
- [ ] 동시에 OutputPostPlaceInspectionQueue가 OutputVisionX를 움직일 수 있는 상태 검증
- [ ] 리소스 획득/해제 로그 확인
- [ ] stage/feeder/cassette material location 정상 전환 확인

### 2.2 P1 - Place T 보정 정책 확인

대상:
- `DieCoordinateTransformService.CalculatePlaceTarget(...)`
- `PickerPlaceSequence.ResolvePlacePickerTTarget(...)`

현재 정책:
- Place T는 `PlacePosition` teaching T만 사용
- `pickerAlignOffsetT`는 Place 계산에서 무시

검토:
- [ ] Pick에서 적용한 `pickerAlignOffsetT` 의미 확인
- [ ] Bottom/Side 검사 후 carry되는 T 보정값의 공정 의미 확인
- [ ] Place 자세에서 die theta 보정이 필요한지 공정 기준 확인
- [ ] 이전 코드/로그에서 Place T에 보정이 들어가던 이유 확인
- [ ] 실제 장비 teaching 기준이 T home zero에 보정을 흡수하는 구조인지 확인

승인 게이트:
- [ ] 현 정책 유지 승인
- [ ] Place T 보정 반영 필요 승인
- [ ] 판단 전 코드 수정 금지

검증:
- [ ] Pick target formula 로그 확인
- [ ] Bottom/Side target formula 로그 확인
- [ ] Place target formula 로그 확인
- [ ] 같은 die에 대해 Pick T와 Place T 차이 추적
- [ ] 실제 place angle 결과 육안/비전 확인

### 2.3 P2 - CycleStop timeout 후 상태 정합성

대상:
- `AutoSequenceCoordinator.AwaitPendingAfterCycleStopAsync(...)`
- `AutoSequenceCoordinator.AwaitPendingAfterAbortAsync(...)`
- `MachineController.StartSequenceAsync(...)`

검토:
- [ ] pending wait 5초 초과 후 cancel이 들어갈 때 축이 이동 중일 수 있는 경로 확인
- [ ] cancel 후 3초 초과 시 Coordinator 종료되는 로그와 UI 상태 확인
- [ ] timeout 종료 후 `EquipmentStatus`가 Ready/Stopped/Alarm 중 무엇이 되는지 확인
- [ ] timeout 종료 후 resource holder가 남을 가능성 확인
- [ ] timeout 종료 후 다음 Start 전에 ReadySequence가 실제 축 상태를 충분히 검사하는지 확인

보완 후보:
- [ ] timeout 직전/직후 axis snapshot 로그 추가 여부
- [ ] resource holder snapshot 로그 추가 여부
- [ ] pending task 이름/유닛 상태 로그 추가 여부

승인 게이트:
- [ ] 로그 보완만 진행
- [ ] 동작 정책 변경 필요 시 별도 승인

검증:
- [ ] InputStageDieComplete 대기 중 CycleStop
- [ ] OutputStageReceiveComplete 대기 중 CycleStop
- [ ] Picker inspection 중 CycleStop
- [ ] Resource 대기 중 CycleStop
- [ ] 각 케이스에서 다음 Start 가능 여부 확인

### 2.4 P2 - Resume 기준 정합성

검토:
- [ ] 하위 sequence의 `SequenceResumeStore` 저장/복구 범위 확인
- [ ] 상위 InputSequence는 material state 기반으로 `_autoStep` 복구하는지 확인
- [ ] 상위 OutputSequence는 material state 기반으로 action 결정하는지 확인
- [ ] 앱 재시작 후 in-memory resume store가 사라지는 영향 확인
- [ ] Material JSON 저장 지연으로 resume 판단이 늦거나 누락될 가능성 확인

검증:
- [ ] InputStage wafer 있음 + align 완료 + die mapping 미완료
- [ ] InputStage wafer 있음 + die mapping 완료 + pick 미완료
- [ ] InputFeeder wafer 있음
- [ ] OutputStage receive complete
- [ ] OutputFeeder unfinished bin 있음
- [ ] OutputFeeder finished bin 있음

### 2.5 P2 - PROCESS TEST DATA 경로 검증

대상:
- `MaterialStateService.CreateProcessTestDataSet(...)`

최근 보완:
- 테스트 wafer의 theta align 완료값이 `offsetT=0`으로 생성되어 DieMapping에서 실패하던 문제를 수정했다.

검증:
- [ ] PROCESS TEST DATA 생성
- [ ] 생성된 TEST-IN-STAGE wafer의 `HasInputStageThetaAlignResult == true`
- [ ] `InputStageAlignOffsetT != 0`
- [ ] `InputStageAlignCorrectedT = InputStageAlignReferenceT + InputStageAlignOffsetT`
- [ ] Auto 시작 후 `IN-STAGE-DIEMAP-THETA-ALIGN` 재발 여부 확인

## 3. 조건별 이동 / 공정 검증 체크리스트

### 3.1 Input

- [ ] Cassette Mapping 성공
- [ ] Ready slot 선택
- [ ] InputStage PrepareLoad 전 Front/RearPicker resource 점유
- [ ] InputStage PrepareLoad 전 `InputStageArea` 점유
- [ ] Feeder -> Stage 전 Picker Avoid 완료
- [ ] Align 전 `InputStageArea` 점유
- [ ] DieMapping 전 theta align 완료
- [ ] DieMapping 후 material die list 생성
- [ ] DieMapping 후 ready bus set
- [ ] Pick 완료 후 ready bus reset
- [ ] Stage unload 전 Picker resource + `InputStageArea` 점유

### 3.2 Picker

- [ ] InputCamera permission 중복 발급 없음
- [ ] PickUp 전 `InputStageReady` 확인
- [ ] PickUp 전 target die 존재 확인
- [ ] PickUp 중 InputVisionX Avoid 조건 확인
- [ ] Bottom/Side 검사 후 결과 material 반영
- [ ] Place 전 OutputReady 확인
- [ ] Place 전 `OutputPlaceArea` 및 side stage area 보호 확인
- [ ] Place 후 Output receive complete set

### 3.3 Output

- [ ] Output stage empty 시 cassette supply
- [ ] OutputReady set 조건 확인
- [ ] Place receive 후 receive complete set
- [ ] Receive complete stage 우선 store
- [ ] Store 중 OutputReady reset
- [ ] Store 중 Picker resource + `OutputPlaceArea` + side stage area 점유
- [ ] OutputFeeder unfinished resume load 검증
- [ ] OutputFeeder finished resume store 검증

## 4. 계산식 검증 체크리스트

### 4.1 Input Pick 계산

- [ ] `stageY = inputStageY + needleYToVisionYOffset`
- [ ] `pickerX = inputVisionX - cameraOffsetX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX`
- [ ] `pickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT`
- [ ] `needleX = inputVisionX - cameraOffsetX + alignOffsetX - needleXToVisionXOffset`
- [ ] `pickerY = signedInputVisionToPickerY + signed(-cameraOffsetY + alignOffsetY + pickerAlignOffsetY)`
- [ ] Front/Rear sign 반전 확인

### 4.2 Output Place 계산

- [ ] `pickerY = pickerYTeaching + pickerAlignOffsetY`
- [ ] `outputStageY = outputStageBaseY + receiveTargetY + outputVisionToPickerY - abs(pickerY)`
- [ ] `pickerX = outputVisionProcessX + receiveTargetX + outputVisionToPickerX + pickerAlignOffsetX`
- [ ] `pickerT = placeTeachingT`
- [ ] `pickerAlignOffsetT ignored` 정책 승인 여부 확인

### 4.3 DieMap 계산

- [ ] `mappingOffsetX = dieMap.OriginX - stage.OriginX`
- [ ] `mappingOffsetY = dieMap.OriginY - stage.OriginY`
- [ ] source input die map과 frame spec size 일치 확인
- [ ] pickup sequence number 적용 확인
- [ ] material die id 생성/갱신 확인

## 5. 빌드 / 로그 검증

- [ ] `git status --short --branch`
- [ ] `git diff --check`
- [ ] 별도 OutDir MSBuild

권장 빌드:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe' `
  'D:\00.PROJECT\CDT-320\Source\CDT320Simulator\QMC.CDT-320\QMC.CDT-320.sln' `
  /t:Build /p:Configuration=Debug /p:Platform="Any CPU" `
  /p:OutDir="D:\00.PROJECT\CDT-320\Source\CDT320Simulator\QMC.CDT-320\_codex_verify\auto_sequence_flow\" `
  /v:minimal
```

로그 확인:
- [ ] `SequenceResource acquired/released` 짝 확인
- [ ] `InputStageReady` set/reset 순서 확인
- [ ] `OutputStageReady` set/reset 순서 확인
- [ ] `DIE-COORD-CALC` formula 확인
- [ ] Alarm 발생 시 원인 chain이 중복 없이 읽히는지 확인
- [ ] CycleStop 후 status와 resume state 확인

## 6. Interlock 수정 사항 리포트 템플릿

Interlock 수정이 필요하다고 판단되면 아래 템플릿을 먼저 작성하고 승인받는다.

```text
# Interlock 수정 사항 리포트

대상:
- 파일:
- 함수:
- 관련 축/유닛:

현재 위험:
- 어떤 충돌/간섭 가능성이 있는가:
- 현재 코드가 어떤 조건을 놓치고 있는가:

수정 필요 이유:
- Sequence resource 보완만으로 충분하지 않은 이유:
- Interlock 레벨에서 막아야 하는 이유:

제안 수정:
- 추가/변경 조건:
- 알람 코드:
- 실패 메시지:
- 기존 정상 동작 영향:

검증 시나리오:
- 정상 동작:
- 차단되어야 하는 동작:
- Stop/CycleStop/Alarm 후 재개:

승인:
- 사용자 승인 여부:
- 승인 일시:
```

