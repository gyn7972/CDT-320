# Input/Output Sequence Safety Analysis

작성일: 2026-07-03

범위:
- `InputSequence.cs`
- `OutputSequence.cs`
- Picker PickUp/Place/InputCamera/OutputCamera 후검사
- 공통 리소스, CycleStop/Alarm 정지 재개 흐름

목표:
- 웨이퍼 완료 후 Input/Output 교체 구간에서 Picker와 Camera가 절대 진입하지 못하는지 확인한다.
- Alarm/Stop 이후 재개 시 이전 작업 순서, 모션 위치, Material 상태, Ready 신호가 일관되는지 확인한다.
- 코드 수정 전 반드시 확인해야 할 순서를 만든다.

## 1. 최우선 안전 불변조건

아래 조건은 코드 수정 전에 먼저 문서/로그/현장 재현으로 검증해야 한다.

| ID | 불변조건 | 필요한 근거 |
| --- | --- | --- |
| S-01 | Input 웨이퍼 작업 완료 후 Stage -> Feeder -> Cassette 언로드 중 Picker가 Input 영역에 들어오면 안 된다. | `InputStageReady` reset, `InputStageArea` 점유, Front/RearPicker 리소스 점유, Picker Avoid 실제 확인 |
| S-02 | Input 웨이퍼 교체 중 InputCamera/InputVisionX가 작업 위치로 들어오면 안 된다. | `InputStageArea` 점유 충돌, InputVisionX Avoid 확인, PickUp permission 생명주기 |
| S-03 | Output Stage/Feeder/Cassette 이송 중 Picker가 Output 영역에 들어오면 안 된다. | `OutputStageReady` side signal reset, `OutputPlaceArea`/StageArea/FeederArea 점유, Front/RearPicker 리소스 점유 |
| S-04 | Output 웨이퍼/Bin 교체 중 OutputCamera/OutputVisionX가 작업 위치로 들어오면 안 된다. | Output post-place queue, OutputVisionX Avoid 이동, OutputPlaceArea/StageArea 점유 |
| S-05 | Stop/Alarm 후 재개는 저장 step만 믿지 않고 Material location, sensor/axis position, Ready bus를 다시 검증해야 한다. | `Restore...FromRuntimeState`, `ResolveNext...Action`, child sequence start checks |

## 2. 병렬 실행 구조

Auto는 Input/FrontPicker/RearPicker/Output이 병렬 실행된다. 한 유닛에서 비Critical 알람이 발생하면 Coordinator가 CycleStop을 요청하고, 다른 유닛은 각자 작업 경계에서 멈춘다.

주요 코드:
- `Common/AutoSequenceCoordinator.cs`: 실패 시 `RequestCycleStop`, pending task 대기
- `Common/MachineSequenceContext.cs`: `RequestCycleStop`, `StopIfCycleStopRequested`
- `Common/UnitSequenceBase.cs`: Auto 리소스 대기 중 200ms 단위로 CycleStop 확인
- `Common/SequenceResourceKind.cs`: `InputStageArea`, `OutputPlaceArea`, `OutputGoodStageArea`, `OutputNgStageArea`, `OutputFeederArea`, `FrontPicker`, `RearPicker`

해석:
- 안전은 단일 시퀀스 순서만으로 보장되지 않는다.
- Ready bus, Material state, Resource lease, 축 Avoid 상태가 동시에 맞아야 한다.
- 따라서 재개 검증도 “어느 step이었는지”보다 “현재 물리/데이터 상태가 어느 작업을 허용하는지”가 먼저다.

## 3. Input 시퀀스 실제 흐름

핵심 위치:
- `InputSequence.cs:81` `ExecuteInputAutoCycleAsync`
- `InputSequence.cs:211` `WaitPickerToCompleteInputStageDiesAsync`
- `InputSequence.cs:286` `UnloadInputStageWaferIfPresentAsync`
- `InputSequence.cs:337` `PublishInputStageReadySignals`
- `InputSequence.cs:372` `ResetInputStageReadySignals`
- `InputSequence.cs:503` `RestoreInputStepSessionFromRuntimeState`
- `InputSequence.cs:608` `ResolveStageWaferResumeStep`
- `InputSequence.cs:1122` `ExecuteWaferUnloadingAsync`
- `InputSequence.cs:1379` `AcquireInputStageAreaAsync`
- `InputSequence.cs:1435` `EnsureInputPickersAvoidBeforeFeederMoveAsync`

Input Auto 흐름:

| 순서 | 코드 경로 | 동작 | 안전 확인 포인트 |
| --- | --- | --- | --- |
| I-01 | `RestoreInputStepSessionFromRuntimeState` | Material 현재 위치로 `_autoStep` 복구 | Stage/Feeder/Cassette 중 실제 wafer 위치와 step 일치 여부 |
| I-02 | Loading steps | Mapping, slot resolve, cassette -> feeder -> stage, align, die mapping | stage/feeder 이동 전 `InputStageArea`와 Picker 리소스 점유 |
| I-03 | `PublishInputStageReadySignals` | `InputStageReady` 등 Ready 신호 set | FinishComplete/DieMapped/Die data 없으면 set 금지 |
| I-04 | `WaitPickerToCompleteInputStageDiesAsync` | Picker가 InputStage die 완료할 때까지 대기 | CycleStop boundary 존재, 완료 전 Ready가 살아 있는지 확인 |
| I-05 | `UnloadInputStageWaferIfPresentAsync` | 언로드 시작 전에 `InputStageReady` reset | Picker 신규 진입 차단의 1차 조건 |
| I-06 | `ExecuteWaferUnloadingAsync` PrepareUnload | Front/RearPicker 리소스 + `InputStageArea` 점유, Picker Avoid 강제 | Picker가 이미 작업 중이면 lease 대기, Stop 요청 가능 |
| I-07 | Stage -> Feeder | 다시 Front/RearPicker 리소스 + `InputStageArea` 점유, Picker Avoid 강제 | Stage/Feeder 이송 전 Picker 실제 Avoid 확인 |
| I-08 | Feeder -> Cassette | Cassette unload, Material Done, Stage runtime clear | Ready bus/Material cleanup 확인 |

현재 판단:
- Input 언로드 경로는 기본 보호 구조가 있다.
- 특히 `UnloadInputStageWaferIfPresentAsync`가 먼저 `InputStageReady`를 reset하고, 실제 이송 전 Picker 리소스와 `InputStageArea`를 잡는다.
- 확인 필요한 잔여 지점은 “Ready reset 후 이미 PickUp permission을 들고 있던 Picker/InputCamera 선행검사가 실제로 멈추는가”이다.

## 4. Input Picker/InputCamera 병렬 흐름

핵심 위치:
- `Picker/PickerPickUpSequence.cs:310` `CheckInputStageReady`
- `Picker/PickerPickUpSequence.cs:359` `AcquireInputStageAreaForPickUpAsync`
- `Picker/PickerPickUpSequence.cs:498` `TryLoadInputCameraMarkInspectionPermission`
- `Picker/PickerPickUpSequence.cs:895` `MoveInputVisionToAvoidForPickerMoveAsync`
- `Picker/PickerPickUpSequence.cs:3102` `SelectNextPickTargetOrComplete`
- `Picker/InputCameraMarkInspectionSequence.cs:172` `RunInputCameraMarkInspectionAsync`
- `Picker/InputCameraMarkInspectionSequence.cs:228` `AcquireInputStageAreaAsync`
- `Picker/InputCameraMarkInspectionSequence.cs:280` `MoveInputVisionXToAvoidAsync`
- `Picker/InputCameraMarkInspectionSequence.cs:351` `GrantPickUpPermission`

검증 포인트:

| ID | 확인 대상 | 이유 |
| --- | --- | --- |
| I-P-01 | PickUp 시작 전 `InputStageReady` 없으면 실패하는지 | 신규 Picker 진입 1차 차단 |
| I-P-02 | PickUp이 `InputStageArea`를 잡고 진행하는지 | InputSequence 언로드와 상호 배제 |
| I-P-03 | PickUp 전 InputVisionX Avoid가 강제되는지 | Picker와 InputCamera 충돌 방지 |
| I-P-04 | InputCamera 선행검사 permission 발급 후 InputVisionX가 다시 작업 위치로 이동하지 않는지 | 사용자가 말한 “카메라 절대 진입 금지” 핵심 |
| I-P-05 | InputCamera pre-inspection mode가 대기 중 `InputStageArea`를 놓는 구조의 영향 | 대기 중은 허용되지만 실제 VisionX 이동 전 재점유 필요 |

현재 판단:
- PickUp은 `InputStageReady`, Material finish complete, die target을 확인한다.
- PickUp은 `InputStageArea`를 잡아 InputSequence unload와 충돌하지 않게 설계되어 있다.
- InputCamera 선행검사는 pre-inspection mode에서 대기 중 `InputStageArea`를 들고 있지 않는다. 다만 실제 `MoveInputVisionXToAvoidAsync`에서는 다시 `InputStageArea`를 잡는다.
- 추가 확인 필요: permission이 이미 발급된 직후 CycleStop/Alarm/재개가 발생했을 때 InputCamera가 `InputStageReady` reset 이후에도 작업 위치 이동을 시도할 수 있는지.

## 5. Output 시퀀스 실제 흐름

핵심 위치:
- `OutputSequence.cs:170` `ExecuteNextOutputStepAsync`
- `OutputSequence.cs:353` `ResolveNextOutputAction`
- `OutputSequence.cs:444` `ResetOutputStageReadyForStore`
- `OutputSequence.cs:457` `WaitAnyOutputReceiveCompleteAsync`
- `OutputSequence.cs:635` `SetOutputStageReadySignals`
- `OutputSequence.cs:658` `EnsureOutputStageReadyForPlace`
- `OutputSequence.cs:883` `ExecuteStoreStageToCassetteAsync`
- `OutputSequence.cs:941` `ExecuteOutputFeederOccupiedAsync`
- `OutputSequence.cs:1004` `ExecuteOutputFeederStoreToCassetteAsync`
- `OutputSequence.cs:1205` `AcquireOutputStageAreaAsync`
- `OutputSequence.cs:1265` `EnsureOutputPickersAvoidBeforeFeederMoveAsync`
- `OutputSequence.cs:1321` `AcquireOutputPlaceAreaAsync`

Output Auto action 결정 순서:

| 우선 | 조건 | action |
| --- | --- | --- |
| O-01 | NG stage receive complete | `StoreNgStageToCassette` |
| O-02 | Good stage receive complete | `StoreGoodStageToCassette` |
| O-03 | OutputFeeder에 material 존재 | `ResumeOccupiedFeeder` |
| O-04 | stage empty + cassette supply 가능 | `SupplyGood/NgCassetteToStage` |
| O-05 | 더 이상 작업 없음 | `StopNoOutputBinWork` |
| O-06 | 그 외 | `WaitOutputStageReceiveComplete` |

정상 Store 흐름:

| 순서 | 코드 경로 | 안전 확인 |
| --- | --- | --- |
| O-S-01 | `ExecuteStoreStageToCassetteAsync` | Front/RearPicker 리소스 점유 |
| O-S-02 | same | `OutputPlaceArea` 점유 |
| O-S-03 | same | side별 `OutputGoodStageArea`/`OutputNgStageArea` 점유 |
| O-S-04 | same | `EnsureOutputPickersAvoidBeforeFeederMoveAsync` |
| O-S-05 | child feeder unload | `OutputFeederArea` 내부 점유, OutputVisionX/Pickers clear 확인 |

현재 최상위 위험 후보:

### H-01. OutputFeeder 완료품 재개 저장 경로 보호 누락 의심

경로:
- `OutputSequence.cs:941` `ExecuteOutputFeederOccupiedAsync`
- `OutputSequence.cs:953` feederWafer가 receive complete면 `ExecuteOutputFeederStoreToCassetteAsync`
- `OutputSequence.cs:1004` `ExecuteOutputFeederStoreToCassetteAsync`

관찰:
- 일반 `ExecuteStoreStageToCassetteAsync`는 Front/RearPicker, `OutputPlaceArea`, side StageArea를 잡고 Picker Avoid 후 stage/feeder/cassette를 움직인다.
- 반면 `ExecuteOutputFeederStoreToCassetteAsync`는 현재 확인한 코드상 `ExecuteCassetteMoveToSlotAsync`와 `ExecuteFeederUnloadToCassetteAsync`만 호출한다.
- `OutputFeederSequenceBase.RunAsync`는 내부에서 `OutputFeederArea`는 잡는다.
- 하지만 이 외곽 경로에는 Front/RearPicker 리소스, `OutputPlaceArea`, side StageArea, `EnsureOutputPickersAvoidBeforeFeederMoveAsync` 호출이 보이지 않는다.

왜 중요한가:
- Stop/Alarm 후 OutputFeeder에 완료 Bin이 남아 있으면 이 경로로 바로 cassette store가 실행될 수 있다.
- 이때 Picker/OutputCamera가 Output 영역을 점유하거나 진입 중이면 “피더만 안전”으로는 충분하지 않다.
- 사용자가 말한 “아웃풋쪽도 절대 픽커/카메라 들어오면 안 됨”의 가장 직접적인 후보 구멍이다.

수정 전 확인:
- 실제 현장/로그에서 OutputFeeder 완료품 상태로 재개했을 때 `OutputPlaceArea` 또는 Picker 리소스가 잡히는지 확인한다.
- child `OutputFeederUnloadToCassetteSequence` 내부의 Picker clear check가 unload-to-cassette 경로에도 실행되는지 확인한다.
- OutputPostPlaceInspectionQueue가 동시에 OutputVisionX를 움직일 수 있는지 확인한다.

## 6. Output Picker/OutputCamera 병렬 흐름

핵심 위치:
- `Picker/PickerPlaceSequence.cs:444` `VerifyOutputStageReadyAsync`
- `Picker/PickerPlaceSequence.cs:633` `MoveOutputStageAvoidPositionAsync`
- `Picker/PickerPlaceSequence.cs:748` `MoveOutputStageReceivePositionAsync`
- `Picker/PickerPlaceSequence.cs:802` `EnsureOutputFeederSafeBeforePlaceStageMoveAsync`
- `Picker/PickerPlaceSequence.cs:2153` `NotifySequenceProgressAfterPlace`
- `OutputStage/OutputPostPlaceInspectionQueue.cs:354` `InspectPlacedDieBatchAsync`
- `OutputStage/OutputPostPlaceInspectionQueue.cs:462` `InspectPlacedDieAsync`
- `OutputStage/OutputPostPlaceInspectionQueue.cs:684` `EnsureOutputFeederAvoidForInspectionAsync`
- `OutputStage/OutputPostPlaceInspectionQueue.cs:776` `MoveVisionXToAvoidAsync`

검증 포인트:

| ID | 확인 대상 | 이유 |
| --- | --- | --- |
| O-P-01 | Place는 side별 Ready bus와 Material receive available을 둘 다 확인하는지 | Output 신규 Picker 진입 차단 |
| O-P-02 | Place 진입 전 `OutputPlaceArea`와 side StageArea를 잡는지 | OutputSequence store/supply와 상호 배제 |
| O-P-03 | Place 진입 전 OutputVisionX Avoid가 강제되는지 | Picker와 OutputCamera 충돌 방지 |
| O-P-04 | Place 중 OutputFeederArea를 잡고 feeder avoid를 확인하는지 | Feeder/Stage/Picker 동시 충돌 방지 |
| O-P-05 | OutputPostPlaceInspectionQueue가 OutputPlaceArea/FeederArea/StageArea를 잡는지 | OutputCamera 후검사와 store/supply 충돌 방지 |
| O-P-06 | H-01 경로에서 post-place queue와 feeder cassette unload가 동시에 실행될 수 있는지 | OutputCamera 절대 진입 금지 검증 |

현재 판단:
- 일반 Place와 후검사는 리소스 기반 보호가 있다.
- OutputFeeder 재개 저장 경로가 이 리소스들과 같은 수준으로 보호되는지 추가 확인/수정 후보이다.

## 7. Stop/Alarm/Resume 검증 매트릭스

| 상태 | 현재 복구 기준 | 확인해야 할 것 |
| --- | --- | --- |
| InputStage에 wafer 있음 | `ResolveStageWaferResumeStep` | Align/DieMapping/DieIds/FrameObjectId가 실제 축/비전 결과와 맞는지 |
| InputFeeder에 wafer 있음 | `LoadFeederToStage` | Feeder sensor occupied, Stage empty, Picker/InputCamera avoid |
| InputStage die pick 완료 대기 중 정지 | `InputStageReady`/`InputStageDieComplete`/Material pick complete | 재개 시 Ready 신호가 잘못 살아 있거나 이미 완료된 die를 다시 pick하지 않는지 |
| OutputStage receive complete | `StoreGood/NgStageToCassette` | side Ready reset, Picker/Camera/Feeder safe |
| OutputFeeder에 unfinished bin 있음 | `ResumeOccupiedFeeder` -> stage load | Stage empty, Picker/PlaceArea/StageArea 보호 |
| OutputFeeder에 finished bin 있음 | `ResumeOccupiedFeeder` -> cassette store | H-01. Picker/PlaceArea/StageArea 보호 누락 의심 |
| OutputPostPlaceInspection 진행 중 정지 | queue stop/alarm check | `CancellationToken.None` 계열 worker가 긴 모션 대기에서 늦게 멈추는지 |

## 8. 코드 수정 전 작업 순서

아래 순서대로 해야 한다. 이 순서는 바로 수정하자는 계획이 아니라, 수정 전에 구멍을 확정하기 위한 분석/검증 산출물 순서다.

1. Input 정상 완료-언로드 타임라인 확정
   - `InputStageReady` set/reset 시점
   - `InputStageDieComplete` set/reset 시점
   - `InputStageArea` 보유자 전환 순서
   - Front/RearPicker 리소스 보유자 전환 순서

2. Input permission/Camera 잔여 상태 검증
   - InputCamera 선행검사 permission 발급 후 CycleStop
   - PickUp이 permission을 받은 뒤 `InputStageReady` reset
   - InputVisionX가 Avoid 아닌 상태로 남는 케이스

3. Output 정상 Store/Supply 타임라인 확정
   - side별 `OutputGoodStageReady`/`OutputNgStageReady` reset
   - aggregate `OutputStageReady` reset/set 조건
   - `OutputPlaceArea`, side StageArea, `OutputFeederArea` 보유자 전환

4. H-01 재개 경로 단독 검증
   - OutputFeeder에 완료품이 남은 상태에서 Auto 재개
   - 동시에 PickerPlace가 OutputStageReady 대기 또는 Place 진입 직전인 상태
   - 동시에 OutputPostPlaceInspectionQueue가 OutputVisionX 이동 가능한 상태
   - 이때 `ExecuteOutputFeederStoreToCassetteAsync`가 Picker/Place/Stage 리소스를 잡지 않는지 로그로 확인

5. Stop/Alarm 재개 케이스 테이블 작성
   - InputStage/Feeder/Cassette 각 위치별
   - OutputGoodStage/OutputNgStage/OutputFeeder/Cassette 각 위치별
   - Material state와 sensor state mismatch 시 어떤 alarm으로 멈추는지

6. 수정 후보를 최소 단위로 나누기
   - H-01 보호 리소스 추가 여부
   - Ready bus reset 범위 보강 여부
   - InputCamera permission clear/revalidate 여부
   - OutputPostPlaceInspection cancellation/resume 보강 여부

7. 수정 후 검증 시나리오 작성
   - 정상 Auto 1 cycle
   - Input 작업 완료 직후 CycleStop 후 재개
   - Input 언로드 중 Alarm 후 재개
   - Output receive complete 직후 CycleStop 후 재개
   - OutputFeeder 완료품 보유 상태로 재개
   - OutputCamera 후검사 대기 중 Output store 재개

## 9. 1차 결론

Input:
- 현재 코드만 보면 완료 후 언로드 진입 시 Picker 신규 진입 차단 구조는 있다.
- `InputStageReady` reset, `InputStageArea` 점유, Picker 리소스 점유, Picker Avoid 확인이 같은 경로에 있다.
- 남은 핵심은 InputCamera 선행검사 permission과 Stop/Resume 경계에서 InputVisionX가 다시 들어올 수 있는지이다.

Output:
- 일반 Store/Supply/Place/후검사는 리소스 보호 구조가 있다.
- 가장 위험한 후보는 OutputFeeder에 완료품이 남은 상태로 재개하는 `ExecuteOutputFeederStoreToCassetteAsync` 경로다.
- 이 경로가 일반 Store와 동일한 Picker/Place/Stage 보호를 갖는지 확인해야 한다. 현재 읽은 코드 기준으로는 누락 의심이다.

수정은 H-01 확정 로그/재현을 먼저 잡고, 그 다음에 최소 범위로 진행해야 한다.
