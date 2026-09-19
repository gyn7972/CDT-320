# 수동 Material 상태 변경과 다음 운전 반영 흐름

기준: 2026-09-07, `D:\Source\CDT-320_New`, 현재 `master`의 로컬 코드.
이 문서는 기존 동작을 추적한 분석 초안이다. 생산 코드 변경이나 인터락·시퀀스 변경 승인을 의미하지 않는다. 저장소 작업 규칙은 `AGENTS.md`를 따른다.

## 사용자 요구와 보존할 동작

사용자는 실장비에서 GUI의 GOOD, WAIT, NG, SKIP, 매핑, Clear 등을 수동 변경한 뒤 장비를 다시 구동하는 기능을 사용하고 있다. 수정의 성공 기준은 이 정상 사용을 유지하면서, 변경한 상태가 Material·화면·저장 파일·다음 동작에 일관되게 반영되는 것이다.

일반 상태 편집, 명시적인 재픽업 복구, 재매핑은 같은 의미가 아니다. 특히 Input GUI의 **WAIT 적용은 기존에 구현된 실물 확인 재픽업 복구**다. 모든 변경에 일괄적으로 이전 Pick/Output 이력을 보존하도록 적용하면 이 정상 기능을 막는다.

이하의 승인 목록·좌표 제한은 특정 조건의 기존 코드 동작이다. 사용자가 수행하는 일반 수동 상태 변경이 전부 차단된다는 뜻이 아니다. 실장비의 정상 변경 사례를 회귀 검증 기준으로 삼아야 한다.

## 1. Input GUI 상태와 데이터 의미

`InputStageMapTransferPage.InputDieManualState`에는 다섯 상태가 있다. `InputDieMapCellState`의 None/InspectionWait/InspectionDone/PickComplete는 표시용 분류이며 별도 편집 상태가 아니다.

| GUI 상태 | 최초 map entry 적용값 | 관련 처리 | 다음 픽업에서의 의미 |
|---|---|---|---|
| WAIT / 검사 대기 | IsTarget=true, Result=Unknown, BinCode=0 | 실물 InputStage 확인 후 선택 Die를 재픽업 준비 상태로 복구 | 승인·좌표·실물 조건이 유효하면 다시 픽업 대상으로 사용 |
| GOOD | IsTarget=true, Result=Good, GoodBin | InputPickVision 수동 OK 검사 기록, Input 쪽 Material 상태 동기화 | 현재 후보 함수는 Good를 완료 상태로 제외 |
| NG | IsTarget=true, Result=NG, MaxBin | InputPickVision 수동 NG/ManualInputMapEdit 기록, Input 쪽 Material 상태 동기화 | 현재 후보 함수는 NG를 완료 상태로 제외 |
| SKIP / 제외 | IsTarget=false, Result=Unknown, BinCode=0, SequenceNo=0 | InputPickVision 기록 제거, 대상에서 제외 | 픽업 대상 제외 |
| FLYING / 유실 | IsTarget=false, Result=NG, BinCode=255, SequenceNo=0 | 기존 검사 기록을 보존하고 NG 결과 파일 기록 요청 | 픽업 대상 제외. 아래 동기화 경로의 상태 소실 문제를 별도 확인 |

근거: `QMC.CDT-320\Ui\Pages\Work\InputStageMapTransferPage.cs:25`, `:5975`, `:6104`.

GOOD/NG/WAIT/SKIP의 의미를 임의로 재정의하지 않는다. 특히 GOOD를 자동으로 WAIT로 만들거나, WAIT를 과거 Pick 이력 때문에 항상 제외하는 방식은 기존 동작을 바꾸므로 사용할 수 없다.

## 2. Input 상태 적용 버튼의 전체 경로

1. 현재 Input Map과 선택 Die를 확인한다. 기존 `CanEditSelectedDieState`는 AutoRunning/Initializing/IsSequenceRunning/IsManualBusy 또는 수동 좌표 이동 중 변경을 막는다.
2. 변경할 상태와 선택 개수를 확인창에 표시한다. WAIT에서는 실물이 InputStage에 있다는 작업자 확인과 Pick/검사/Output 수신 상태 초기화 안내를 추가한다.
3. WAIT이면 `PrepareInputDiesForManualRepick`을 먼저 실행한다.
4. 선택 entry 상태를 바꾸고 `PickupSequenceGenerator.ApplySequenceNumbers`로 map 순번을 갱신한다.
5. `LotStorage.ActiveInputDieMap`에 반영하고 InputPickVision 검사 및 `ApplyManualDieState(...InputMapOnly)`로 Material을 동기화한다.
6. `Controller.ApplyInputDieMap`으로 runtime 표시 맵과 픽업 순서 목록을 갱신한다.
7. Material 저장을 요청하고 화면·완료 메시지를 갱신한다. 현재는 저장 실패 반환 전달에 결함이 있다.

근거: `InputStageMapTransferPage.cs:5788`, `:5827`, `:5844`, `:5858`, `:5874`, `:5911`.

### WAIT가 명시적으로 수행하는 재픽업 복구

`MaterialStateService.InputPick.cs:3600`의 `PrepareInputDiesForManualRepick`은 선택 ID 목록을 정리하고 Die 존재를 확인한 뒤 `_stateSync` 안에서 복구한다. `:3780`의 개별 처리 내용은 다음과 같다.

- IsInputTarget=true, Result=Unknown, Input/Output Bin=0.
- Output 부모와 Bin index/offset 초기화.
- CurrentLocation=InputStage, Picker 예약·PickedPicker·PickedAt 초기화.
- InputPickVision/PickUp/Bottom/Side0/Side90/OutputPlaceVision/ManualPickerHeadEdit 검사 기록 제거.
- Active Input Map의 상태 동기화와 InputPickContext 캐시 무효화.
- `:3827`에서 이전 Output 슬롯의 DieUid/SourceDieUid/PlacementUid와 검사 상태를 함께 해제.
- 해당 Output Wafer.DieIds에서 Die 제거, 다음 수신 index 재계산, Finish였던 Output Wafer를 Working으로 복구.

따라서 Output으로 간 Die라도 작업자가 실제로 InputStage로 복귀시킨 뒤 WAIT를 적용하면 다시 처리할 수 있는 기존 기능이다. 이 경로에서 과거 Output 점유를 계속 유지하도록 바꾸면 실제 복구가 완료되지 않는다.

과거 기록을 더 보존하려면 현재 상태/점유와 분리한 감사 기록으로 남길 수 있지만, 이번 변경을 이유로 재픽업 기능 자체를 없애거나 운영 데이터 모델을 광범위하게 바꾸지 않는다.

### 일반 상태 동기화는 실물 복구와 다르다

`MaterialStateService.DieOps.cs:267`의 `ApplyManualDieState(...InputMapOnly)`는 Active Input Map을 갱신한 뒤, 현재 Input Wafer 소속이고 위치가 InputStage/Unknown인 Die에 Material 판정·대상 상태를 적용한다. Picker/Output으로 이미 이동한 Die를 일반 GOOD/NG/SKIP 편집만으로 InputStage로 되돌리지 않는다.

이 동작과 WAIT의 명시적 복구를 구분해야 한다. 단순한 GOOD/NG/SKIP 변경과 실물 위치 복구를 같은 함수 분기 하나로 합치지 않는다.

## 3. Target 토글, Pick Status 저장, 좌표 및 재매핑

| 경로 | 현재 처리 | 수정 시 보존할 내용 |
|---|---|---|
| Target 토글 | `ToggleSelectedEntryTarget`에서 target 반전, Result/Bin 초기화, 순번 재생성, dirty 표시 | 토글한 대상이 저장·다음 동작에 반영되어야 함 |
| Save Pick Status | `PersistPickStatusToMaterialState`가 세대 확인 후 InputStage/Unknown Die의 대상·결과·좌표·순번을 반영 | Picker/Output의 실물 이력은 일반 편집으로 초기화하지 않음 |
| 이미 확정한 맵에 수동 Offset 적용 | `TryApplyInputMapOffsetPreservingDieState`로 맵과 Die 좌표를 갱신 | 상태 편집값 유지. 좌표 변경 후 기존 Review 확인 절차 유지 |
| Preview를 절대 좌표로 최초 확정 | `InputStageDieMapApplyService.Apply` 사용 | 새 좌표/맵 적용과 기존 Material 세대 대응 확인 |
| 시퀀스에서 새 Die Mapping 적용 | 동일 Apply 서비스 사용 | 명시 WAIT 복구와 구분. 같은 물리 Die의 Output 연결을 일방 초기화하지 않음 |

근거: `InputStageMapTransferPage.cs:5752`, `:2112`, `:2159`, `:3045`, `:3089`; `MaterialStateService.InputPick.cs:2987`; `Sequencing\InputStage\InputStageDieMapApplyService.cs:43`, `:553`.

현재 Save Pick Status는 Review 승인 필드를 무조건 지우지 않는다. 승인 목록 안의 대상 제외/완료 처리를 진행 상태로 인정하고, 승인 목록 밖의 새 WAIT 등 특정 변경은 아래 승인 경로에서 재확인한다. 이 구분을 없애면 부분 작업된 wafer가 불필요하게 재매핑되거나 진행이 막힐 수 있다.

일반 재매핑의 결함은 같은 물리 UID의 Die를 가져와 Output 부모·위치·Pick/검사 기록을 초기화하면서 Output의 역참조를 남기는 것이다. 이를 고칠 때 **GUI가 이미 확정한 정상 상태와 명시 WAIT 복구 결과를 예전 이력으로 덮어쓰면 안 된다.** 적용 시점의 최신 Material과 작업 유형을 구분하여 처리해야 한다.

## 4. Review에서 편집하는 상태·순서·시작점

운전 중 Review UI의 GOOD/NG/SKIP/WAIT 버튼은 먼저 Draft를 바꾼다. 순서·시작점 변경도 Draft에 적용한다. 최종 CONFIRM에서 `Form1.BuildInputStageRunReviewResult`가 모든 Die 상태·좌표·원점 및 OrderedDieIds/StartDieUid/StartDieIndex를 전달한다.

`CommitInputStageRunReviewCore`는 대상 wafer, revision, 전체 UID 집합, WAIT 대상과 순서 집합을 검증한 뒤 Material·Active Map·승인 목록을 반영하고 저장 완료를 확인한다. 진행 전에는 편집한 상태/좌표/순번을 일괄 반영한다. 이미 Picker/Output 진행 기록이 있는 경우 현재 코드는 진행 상태를 보존하는 재확인 경로를 사용한다.

근거: `Ui\Dialogs\InputStageRunReviewDialog.cs:1568`, `:1622`; `InputStageRunReviewDialog.PickupOrder.cs:58`; `Form1.InputStageRunReview.cs:378`; `MaterialStateService.InputPick.cs:851`, `:1040`.

주의: Work Input Map 화면의 별도 Review 보기 버튼(`InputStageMapTransferPage.cs:338`)은 `SetReadOnlyPreview(true)`로 여는 조회 화면이다. 시퀀스가 띄운 편집/CONFIRM Review와 혼동하면 안 된다.

## 5. START와 재시작이 실제로 읽는 상태

화면에서 `Controller.ApplyInputDieMap`을 호출하는 것만으로 실제 픽업 상태가 결정되지는 않는다.

- `MaterialStateService.TryResolveInputPickContextNoLock`는 **Material로부터 맵을 재구성**한다.
- wafer에 Review 승인이 있으면 저장된 UID 승인 순서에서 남은 WAIT 대상을 복원한다.
- 최종 `ReserveNextInputStagePickTarget`에서 현재 Die의 target, Result, Pick 완료 이력, 예약, 현재 위치를 다시 검사한다.
- 실제 `InputDieVisionPrepareSequence`는 이 Material 예약 API를 호출한다.
- 프로그램 재시작/화면 재진입에서는 저장된 Material로 맵과 runtime을 복원한다. 따라서 UI만 바뀌고 Material/파일 반영이 실패하면 다음 동작과 어긋난다.

근거: `MaterialStateService.cs:237`; `MaterialStateService.InputPick.cs:20`, `:100`, `:2438`, `:3457`; `Sequencing\Picker\InputDieVisionPrepareSequence.cs:310`; `InputStageMapTransferPage.cs:1293`.

`CanUseInputPickCandidate`는 target=false, Result=Good/NG, PickedAt/PickedPicker 또는 완료 PickUp 검사 이력을 제외한다. WAIT 복구가 이러한 완료 이력을 명시적으로 정리하므로 정상 재픽업이 가능하다. 일반 맵 편집에서만 과거 Pick 이력을 보존한다는 구분이 필요하다.

### 정상 반영과 특정 제한 조건

| 상황 | 현재 코드상 처리 |
|---|---|
| 승인 목록 안의 WAIT를 GOOD/NG/SKIP으로 변경 | 다음 후보에서 제외하고 나머지를 기존 승인 순서로 진행 |
| 같은 목록 안의 GOOD/NG/SKIP을 실물 확인 후 WAIT로 복구 | 좌표와 나머지 승인 조건이 유효하면 재픽업 대상으로 복귀 |
| 기존 승인 목록에 전혀 없던 Die를 새 WAIT로 활성화 | 새 WAIT가 승인 목록 밖이라는 특정 사유로 승인 재확인이 필요 |
| 상태/target/순번만 바뀌고 UID/좌표가 그대로임 | 기하 확인 signature는 상태/target/순번 자체를 포함하지 않음 |
| 원점·Die 좌표·관련 Recipe 확인 자료가 달라짐 | 기존 Review 확인 자료와 비교하여 다시 확인하는 경로를 사용 |
| 일부 Pick/Place 후 Review에서 새 대상·새 순서를 제출 | 현재 진행 보존 검증은 기존 전체 승인 집합과 남은 순서를 요구하므로 해당 경우 제한될 수 있음 |

근거: `MaterialStateService.OutputReceive.cs:2084`, `:2170`; `MaterialStateService.InputStageReviewGeometry.cs:600`, `:761`, `:1016`.

승인이 유효한 기존 wafer는 `InputSequence.ResolveStageWaferResumeStep`에서 준비 완료 경로로 복원하고, 승인이 불일치하는 경우 ReviewStage부터 재확인한다(`InputSequence.cs:1425`, `:1453`). 이것을 수동 변경 전체가 무시되거나 차단된다고 해석하면 안 된다.

승인 밖 신규 WAIT 추가나 부분 진행 후 승인 순서 변경까지 사용자가 요구하는 범위라면, 현재 제한과 의도한 동작을 구체적인 사례로 확인한 뒤 인터락·시퀀스 승인 절차에 따라 제안한다. 단순 저장 오류 수정에 끼워 넣어 자동 승인을 추가하지 않는다.

## 6. 확인한 성공·실패 전달 문제

아래 표는 분석 시작 시의 문제다. 이번에는 InputStageMapTransferPage의 결과 전달과 안내만 수정했다. 개별 실패는 수집하되 기존 나머지 Die 동기화→runtime projection→최종 flush→UI 갱신 순서를 유지하고, 반영 실패/저장 실패를 전체 성공이라고 표시하지 않는다. Pick Status의 기존 비동기 저장 요청은 새 동기 대기를 추가하지 않고 '저장 요청'으로 정확히 표시한다. 원자적인 일괄 변경과 FLYING 상태 의미 수정은 승인 대기 범위에 남긴다.

| 코드 | 현재 문제 | 필요한 수정 방향 |
|---|---|---|
| InputStageMapTransferPage.cs:5876 | TryFlushPendingSave 실패 반환을 무시하고 뒤에서 완료 메시지 표시 | 실제 저장 완료/실패를 받아 정확히 표시 |
| 같은 파일 :6014 | SyncManualDieState가 서비스 실패를 로그만 남기고 일괄 처리를 계속함 | 전체 사전검증과 변경 결과를 호출부에 전달 |
| 같은 파일 :2159 | PersistPickStatusToMaterialState가 void이고 세대 불일치 등의 조기 종료를 호출자가 구분하지 못함 | 실패 사유 반환. 변경되지 않았는데 저장 완료로 표시하지 않기 |
| 같은 파일 :2233 이후 | wafer.DieIds와 여러 Die를 락 밖에서 순차 변경하여 중간 상태가 보일 수 있음 | 관련 변경을 같은 Material 변경 단위로 적용 |
| 같은 파일 :5999 → :6020, MaterialStateService.DieOps.cs:680 | FLYING의 최초 NG/255가 IsTarget=false 공통 동기화에서 Unknown/0으로 덮일 수 있음 | SKIP과 FLYING의 상태 의미를 유지하며 Material·Active Map에 동일 적용 |

FLYING 후 `RecordFlyingDieResult`는 NG 결과 파일 기록을 요청하는 기능이다(`MaterialStateService.OutputReceive.cs:224`). 소실된 Material 상태를 NG/255로 복원하는 함수는 아니므로, 결과 파일 요청이 있다는 이유만으로 상태 동기화가 정상이라고 판단하지 않는다.

이 문제를 수정할 때 새 운전 차단 정책을 추가하는 대신, 우선 기존 성공/실패 계약과 상태 의미를 정확히 보존한다. 인터락·시퀀스에 영향을 주는 추가 변경은 사용자 승인을 받는다.

## 7. Input 회귀 검증 목록

- [ ] 실장비에서 사용자가 이미 수행한 정상 GOOD/WAIT/NG/SKIP 변경 사례를 기록하고 그대로 재현한다.
- [ ] WAIT → GOOD/NG/SKIP 각각의 GUI, Material, 저장 후 재로드, 다음 후보가 일치한다.
- [ ] GOOD/NG/SKIP → WAIT가 같은 승인 목록 안에서 다시 처리 대상으로 복귀한다.
- [ ] Picker/Output에서 실제 복귀 확인 후 WAIT를 적용하면 선택 Die의 이전 점유·검사 상태를 함께 정리하고 재픽업할 수 있다.
- [ ] 여러 Die 선택 변경에서 일부만 적용하고 전체 성공을 표시하지 않는다.
- [ ] 일반 Target 토글·Save Pick Status가 명시 WAIT 재픽업과 혼동되지 않는다.
- [ ] FLYING이 SKIP으로 조용히 바뀌지 않고 대상 제외와 유실 결과 의미를 유지한다.
- [ ] 상태 변경 후 화면 전환, 프로그램 재시작, START에서 같은 Material 상태가 복원된다.
- [ ] 운전 전 Review의 상태·순서·시작점·좌표 편집과 CONFIRM 저장이 기존대로 동작한다.
- [ ] 일부 작업된 wafer의 일반 상태 변경과 이미 허용된 재확인 동작을 불필요하게 막지 않는다.
- [ ] 승인 목록 밖 신규 WAIT와 좌표 불일치는 해당 특정 조건에서만 검출된다. 정책 변경은 승인 없이 적용하지 않는다.
- [ ] 일반 재매핑이 GUI에서 확정한 상태·명시 WAIT 복구 결과를 예전 진행 이력으로 되돌리지 않는다.
- [ ] 파일 저장 실패·Material 세대 불일치에서 성공 메시지를 표시하지 않고 구체적인 실패 사유를 전달한다.

## 8. Output 및 Clear

### Output GOOD/WAIT/NG/SKIP

`OutputStageMapTransferPage.cs:1471`에서 기존 운전/수동 이동 조건을 검사하고 선택 개수·상태 확인창을 거친다. 화면 entry, 실제 Die, Output 수신 슬롯을 차례로 바꾸고 저장을 요청한다. 다음 대상은 화면 색상이 아니라 Material의 수신 슬롯에서 결정된다.

| 변경 | 현재 수신 슬롯 상태 | 다음 배치에 미치는 영향 |
|---|---|---|
| GOOD | IsTarget=true, Result=Good, 검사 완료/OK | 실제 Die UID가 없어도 완료로 인정하여 배치 제외 |
| NG | IsTarget=true, Result=NG, 검사 완료/NG | 실제 Die UID가 없어도 완료로 인정하여 배치 제외 |
| SKIP | IsTarget=false, Result=Unknown, 검사 초기화 | 대상 제외. 현재 코드는 DieUid만 비우므로 이미 배치된 슬롯에서 SourceDieUid와 불일치 발생 |
| WAIT, 빈 슬롯 | IsTarget=true, Result=Unknown, 검사 초기화, DieUid는 기존값 유지 | DieUid가 비어 있으면 다시 배치 후보 |
| WAIT, 이미 배치된 슬롯 | 동일하나 실제 DieUid가 남음 | 현재는 빈 슬롯으로 취급하지 않음. 실물 제거 후 재배치 용도까지 요구되는지는 사용자 확인 중 |

근거: `OutputStageMapTransferPage.cs:1673`, `:1705`; `MaterialStateService.OutputReceive.cs:2386`, `:2411`.

선택 상태 반영 후 다음 index와 Wafer Working/Finish를 다시 계산한다. `PickerPlaceSequence.OutputStageReady.cs:456`은 `ReserveNextOutputStageReceiveTarget`을 호출하며, 그 안에서 수신 슬롯의 현재 target/Result/DieUid로 다음 OrderIndex를 고른다. 따라서 정상 빈 슬롯의 GOOD/NG/SKIP→WAIT 변경은 다음 배치에 반영되는 기존 기능이다. START가 이 상태를 매번 저장 파일로 덮어쓰는 구조가 아니다.

현재 순서 좌표 캐시는 wafer instance에 대응하는 Recipe 빈맵 순서를 사용한다(`MaterialStateService.OutputReceive.cs:189`, `:280`). 상태 변경은 해당 순서 중 진행할 슬롯을 고른다. Receive Plan 초기화 버튼은 별도 확인 후 빈맵으로 수신 슬롯 전체를 다시 만드는 동작(`OutputStageMapTransferPage.cs:3248`)이며 단순 상태 편집과 다르다. 부분 배치 상태에서 계획을 초기화하는 경우에는 기존 실제 배치와 슬롯 재생성의 일관성을 별도 검증해야 한다.

로그의 SKIP 후 저장 실패는 `SourceDieUid`가 있는데 `DieUid`만 비워진 연결을 검증기가 거부한 것이다. 검증을 끄거나 모든 상태 편집을 막는 방식으로 해결하면 안 된다. 상태 의미를 유지하면서 점유 식별자와 실제 Die의 부모 연결을 같은 변경 단위로 처리해야 한다. SourceDieUid가 없는 계획 슬롯의 DieUid는 화면용 셀 ID일 수 있어 실제 Die로 잘못 해석하면 안 된다.

별도 경계 조건도 확인했다. **배치가 일부만 진행된 Output의 모든 슬롯을 SKIP으로 바꾸면**, 현재 완료/잔여 수량 계산은 targetSlots.Count가 0이어서 예전 총수량/배치수 fallback으로 내려간다. `ResolveNextOutputReceiveIndex`도 남은 target이 없는데 DieIds.Count로 index를 반환할 수 있다. 예약 함수는 이 index로 원래 빈맵 순서의 셀을 선택하고, 현재 예약 세대 검사는 슬롯의 IsTarget까지 확인하지 않는다(`MaterialStateService.OutputReceive.cs:280`, `:540`, `:1285`, `:1316`, `:2386`). 따라서 전체 제외를 일반 잔여 배치 대상으로 오해할 수 있는 경로가 있다. 일부 SKIP이 정상 반영되는 것과 별개의 조건이며 실장비 발생을 재현한 결과는 아니다. 제어 대상 선택 변경이므로 이번 생산 코드에는 적용하지 않고 승인안과 회귀 항목에 포함한다.

### Input Clear와 다음 카세트 매핑

`InputCassettePage.cs:743`은 CST CLEAR 클릭 시 먼저 확인창을 표시한다. 예를 선택하면 기존 Controller 작업 보호 아래 Finish 여부와 실제 센서/Picker·Input 이송 자재를 확인하고 Clear, 저장 완료, runtime projection 동기화를 수행한다. 취소 시 데이터를 변경하지 않는다. 이 확인창은 현재 로컬 코드에 이미 구현돼 있어 이번에 중복 추가하지 않았다.

정상 Clear는 Input 슬롯을 비우고 IsMapped/IsPresent/CassetteLotId를 초기화한다(`MaterialStateService.Clear.cs:1377`, `:1470`). Input Stage/Feeder가 비어 있고 카세트가 미매핑이면 `InputSequence.cs:1254`가 다음 시작을 Mapping으로 결정한다. 기존 Clear→다음 START 매핑 기능을 유지해야 한다.

Output Die 이력이 있는 경우 해당 기록을 보존하는 경로가 이미 있지만, 현재 판단은 Die의 Output 부모 필드에 의존한다(`MaterialStateService.Clear.cs:958`). 재매핑으로 이 부모 필드가 지워지고 Output 슬롯만 역참조를 유지하면 Clear가 Die를 삭제해 저장 손상을 키울 수 있다. 정상 Finish Clear를 막는 새 조건을 일괄 추가할 것이 아니라, 실제 Output 참조를 함께 보존하고 모순을 삭제 전 확인하도록 수정안을 검토한다.

- [ ] Output의 빈 슬롯에서 GOOD/NG/SKIP→WAIT 후 다음 배치 대상에 포함된다.
- [ ] Output의 WAIT→GOOD/NG/SKIP 후 다음 배치에서 제외된다.
- [ ] 부분 배치 후 모든 슬롯을 SKIP으로 바꾸면 제외한 슬롯이 다시 예약되지 않는다. 전체 제외의 완료/교체 처리 정책은 승인 후 적용한다.
- [ ] 이미 배치한 슬롯의 상태 편집에서 Die/슬롯/부모 연결이 일치하고 저장·재로드된다.
- [ ] 실물 제거 후 Output WAIT 재배치의 요구가 확인되면 기존 점유 연결의 완전한 해제와 다음 배치를 검증한다.
- [ ] 부분 배치 후 Receive Plan 초기화는 기존 Die와 슬롯의 일관성을 검증한 뒤 승인 범위를 정한다.
- [ ] 카세트 Finish, 실제 Input Stage/Feeder/Picker 비움 후 확인→Clear→저장→다음 START 매핑을 검증한다.
- [ ] Clear 취소 또는 사전검증 실패 시 데이터가 변경되지 않는다.
- [ ] 정상 Output 이력 때문에 Input Clear가 불필요하게 차단되지 않는다.
- [ ] 손상된 역참조는 삭제 전 보고하며 부분 삭제 후 성공을 표시하지 않는다.
