# WaferMap 작업 모니터 표시

적용 화면: `InputStageMapTransferPage`, `OutputStageMapTransferPage`, `WorkMainPage`.

## 수정 위치

- 색상 값: `QMC.CDT-320/Ui/Controls/Styles/WaferMapPalette.cs` 한 곳.
- 상태 우선순위, 문구, 범례: `QMC.CDT-320/Ui/Common/WaferMaps/WaferMapDisplayStyle.cs`.
- 컨트롤 배치, 맵 표시 옵션, 목록의 정적 열: 각 화면의 `.Designer.cs`.
- 실제 Die 데이터는 기존 Material 서비스를 읽는다. 표시용 상태를 Material에 저장하지 않는다.

화면 배치는 변경하지 않는다. 동적으로 변하는 Die 셀은 공통 맵 컨트롤이 그린다.
정적 컨트롤을 런타임에 생성해서 Designer와 다른 화면으로 만드는 구조가 아니다.

## 공통 색상과 의미

| 상태 | 색상 | 의미 |
|---|---|---|
| 대기 | `#CCDDEE` | Input 검사/픽업 대기 또는 Output 배치 전 |
| 검사 완료 | `#F5A623` | 해당 단계의 검사 기록이 있음. 물류 GOOD 판정과 별도 |
| 픽커 보유 | `#8E63CE` | 픽커가 Die를 보유한 상태. 보유 F/R 번호는 상세 문구로 표시 |
| 안착 완료 / 검사 전 | `#4488CC` | Output에 안착했으나 Output 검사 전인 상태 |
| GOOD | `#33B26B` | Input에 기록된 GOOD 결과 |
| NG | `#B22222` | NG 결과 또는 Output 검사 NG. BIN 색으로 덮어쓰지 않음 |
| SKIP | `#555555` | 작업 제외. 화면에서는 숨기지 않음 |
| 상태 미확인 | `#505064` | Material과 연결할 수 없는 표시 상태 |

색상은 이 문서보다 실제 팔레트 코드를 기준으로 한다. 변경할 때 세 화면의 범례와 상태 문구도 함께 확인한다.
BIN은 계획/물류 분류값이다. `BIN=1`만으로 검사 GOOD 또는 배치 완료로 해석하지 않는다.

### Input

SKIP → NG → 픽커 보유 → GOOD → 검사 완료 → 대기 순서로 표시한다.
LOT의 총 처리/GOOD 수량으로 개별 Die의 Result/BIN을 배정하지 않는다.
Material 연결 시 Wafer ID·instance·소유 Die ID를 함께 확인하고 UID와 local grid가 모두 맞아야 연결한다.
중복 UID는 임의 선택하지 않고 상태 미확인으로 표시한다.
실시간 색·문구·Result/BIN 열은 같은 표시 사본의 값을 사용하여 이전 맵 결과와 새 검사 상태를 섞지 않는다.
레시피 미리보기와 수동 편집/적용 상태에서는 기존 편집 맵의 값 표시를 유지한다.

### Output

SKIP을 먼저 표시한다. 검사 완료일 때 검사 NG 또는 기존 물류 NG이면 NG색이다.
그 외 검사 완료는 검사 완료색, 검사 전 실물 Die 또는 완료 물류 결과가 있으면 안착색이다.
배치 전 슬롯은 BIN이 1/255여도 대기색이다.
상태 열은 이 공통 표시 상태를, Result 열은 실제 슬롯의 물류 결과를 보여준다.
예: Result가 Good이어도 Output 검사 NG라면 상태는 NG이며 NG색이다.

### Output의 Material 저장 버튼

`SAVE MATERIAL STATE`는 화면 맵을 Material로 역기록하지 않고 기존 Material 저장 서비스를 사용한다.
Grid는 읽기 전용이며, 수동 상태 APPLY와 GOOD/NG PLAN INIT은 각각의 기존 경로에서 Material을 갱신한다.
저장 버튼으로 미초기화 수납 계획을 새로 만들지는 않는다. 계획 초기화는 전용 버튼을 사용한다.

- UI 저장에서 슬롯 목록을 비우거나 재생성하지 않는다. SKIP 슬롯도 그대로 유지한다.
- 검사 완료/OK, 검사 Offset X/Y/T, Raw, SourceDieUid, PlacementUid, LegacyDieUid, IdentityRecoveryNote를 화면 값으로 덮어쓰지 않는다.
- 화면용 합성 UID, NextIndex 기반 추정 Result/BIN, 표시 좌표를 저장하지 않는다. OrderIndex와 NextIndex도 UI에서 재계산하지 않는다.
- 한 화면의 중복 저장 요청을 막고 백그라운드에서 `TryNotifyAndSave`와 `TryFlushPendingSave`를 수행한다.
- 저장 요청 접수만으로 완료를 알리지 않는다. Flush가 성공해야 완료 안내를 표시하고 실패하면 오류를 알린다.

저장 header/revision, 구형 identity 보정, 재시도 등 기존 Material 저장 파이프라인의 정책은 변경하지 않는다.

## 예약/보유와 사용자 선택

예약 F1/R1, 보유 F1/R1, GOOD/NG STAGE 등의 정보는 별도 상세 문구로 유지한다.
예약·보유는 정지 중에도 유지될 수 있으므로 현재 시퀀스가 실행 중이라는 뜻이 아니다.
마우스 선택 테두리는 사용자 선택이며, 자동 시퀀스 현재 타깃 마커로 사용하지 않는다.
WorkMain은 같은 Wafer instance의 갱신에서 선택을 새 셀에 연결한다. Wafer 교체·소실·표시 소스 변경 시 초기화한다.

## 갱신과 변경 경계

WorkMain의 각 맵은 진행 중 작업을 하나로 제한한 백그라운드 표시 사본 생성 방식을 사용한다.
이벤트는 dirty 신호만 전달하고 UI 타이머는 완료된 사본만 적용한다. Paint는 Material을 조회하지 않는다.
핸들 재생성은 타이머를 정지/재사용하며 최종 Dispose에서 이벤트와 타이머를 해제한다.
표시 갱신은 `LotStorage.ActiveInputDieMap`을 교체하거나 비우지 않는다.

Input/Output 전환 화면의 기존 레시피 승인 확인, 수동 이동, 상태 APPLY, 인터락, 시퀀스는 변경하지 않는다.
Output 저장 버튼의 화면 역기록만 제거하며, Material 저장 서비스와 직렬화 계약은 유지한다.
Material 맵 생성 API의 전역 lock과 전환 화면의 동기 레시피 읽기 비용은 남아 있다.
실제 장비 운전 중 성능/사이클 시간까지 검증한 것은 아니다.

## 2번: 실제 작업 대상 표시 분석 (2026-09-06)

상태: 분석 완료, 시퀀스의 표시 정보 게시 변경은 별도 승인 대기. 실행 마커는 아직 구현하지 않았다.

### 기존 값을 실행 대상으로 사용할 수 없는 이유

- `Sequencing/Common/SequenceActivitySnapshot.cs`는 유닛 상태·동작명·Step·시각만 제공한다. Die UID, Wafer instance, Output 슬롯 식별자는 없다.
- `PickerPickUpSequence.cs`의 `_currentDieId`와 `_pickTarget`는 검사 대상 선택에도 사용한다. `PickerPickUpSequence.PickTargets.cs`의 배치 좌표 계산 루프에서도 바뀌므로 단순 공개 getter를 추가하면 실제 픽업 대상과 다르게 표시될 수 있다.
- 실제 픽업 순서 대상은 `_pickCursor`로 선택되지만, 선택 이후에도 검사 결과를 기다릴 수 있다. 대상 선택과 실행·대기 상태를 함께 읽어야 한다.
- Output 슬롯 예약 후에도 자원 대기가 남는다. `NextIndex`는 예약 또는 다음 수납 위치이며, 현재 안착/후검사 대상의 실행 포인터가 아니다.
- `PickerPlaceSequence.PlaceDown.cs`의 `_placedWaferInstanceId`는 Input wafer instance를 담는다. Output 연결은 receive target의 `OutputWaferInstanceId`와 `OrderIndex`, local grid를 기준으로 확인해야 한다.
- `OutputPostPlaceInspectionQueue.cs`는 촬영 완료 뒤 RESULT 수집과 다음 Place가 겹칠 수 있다. 장비 전체의 현재 Die를 하나로 정하면 다른 진행 대상을 놓친다. `InspectionDone=false`만으로 검사 대기를 추정해서도 안 된다. NG는 후검사 등록 자체를 생략하는 경로가 있다.
- Cycle Stop 요청 뒤에도 제품 배출/후검사 정리가 이어질 수 있다. 요청 시점과 실제 실행 종료를 구분해야 하며, 기존 Stop 동작을 변경하는 작업은 아니다.

### 권장하는 1차 구현 범위 — 승인 전 제안

Input 픽업과 Output 안착의 실행 대상 연결부터 적용하고, Bottom/Side 검사 및 Output 후검사의 촬영·결과 대기 표시는 후속 범위로 나눈다.
1차만 적용한 상태를 전체 검사 공정의 실시간 추적 완료로 안내하지 않는다.

1. 시퀀스 공통 영역에 표시 전용 사본과 조회 창구를 둔다. 실행 세대/갱신 번호, F/R·Picker 번호, 단계와 실행/대기 구분, Die UID, Input/Output wafer instance, Output 슬롯 순번과 local grid만 전달한다. Material 객체 자체를 UI에 넘기거나 표시 정보를 Material에 저장하지 않는다.
2. 실제 대상 선택, Step 실행/대기, 픽업·안착 완료, Abort/종료 경계에서 표시 사본만 갱신한다. 배치 좌표 계산 중간값은 게시하지 않는다. 수동 Step 반환 후에는 실행 중으로 남기지 않는다.
3. UI/Common에서 현재 표시 중인 wafer와 UID/슬롯/local grid 일치를 확인한다. 예약·픽커 보유·마우스 선택으로 실행 여부를 추정하지 않는다. 정보가 없거나 일치하지 않으면 실행 마커를 표시하지 않는다.
4. 공통 `DieMapView`에서 기존 8가지 채움색은 유지하고, 사용자 선택과 다른 이중 코너선 및 `F1`/`R1` 문구로 실행 대상을 표시한다. 대기는 실행과 다른 문구로 구분한다. F/R은 독립적으로 표시하며 같은 셀이면 둘 다 보존한다.
5. 정적 표시 옵션과 필요한 컨트롤은 Designer에서 수정 가능하게 둔다. 실행 사본은 런타임 데이터이며 Designer에 계산 함수나 장비 조회 코드를 넣지 않는다. 화면 배치는 유지한다.

승인이 필요한 예상 연결 위치는 다음과 같다. 실제 게시 호출은 필요한 경계에만 한정한다.

- Input: `PickerPickUpSequence.cs`, `.PickTargets.cs`, `.MotionResolvers.cs`.
- Output: `PickerPlaceSequence.cs`, `.OutputStageReady.cs`, `.PlaceDown.cs`.
- 공통: 표시 전용 사본/저장소, Controller/Context의 읽기 창구 및 실행 시작·종료 수명 연결.

기존 동작 분기, 모션 명령과 순서, 좌표/Teaching, 인터락, 자원 획득/반환, 정지·재개 정책, Material 저장/예약 규칙은 변경 대상이 아니다.
UI 코드는 이 표시 창구를 읽기만 하며 시퀀스를 호출하거나 제어하지 않는다.

### 갱신 및 검증 조건

- Input/Output의 현재 전체 갱신은 1.5초 주기다. WorkMain의 Material 변경 신호만으로도 Material 변화 없는 실행 시작/종료를 놓칠 수 있다. 실행 표시에는 별도의 가벼운 갱신 경로가 필요하다.
- 시퀀스 게시 경로에서 UI 호출, 디스크 I/O, 전체 맵 생성, UI 처리 완료 대기를 하지 않는다. UI는 최신 사본의 변경만 합쳐 반영하고, 마커 변경 때문에 레시피 재읽기나 그리드 재바인딩을 하지 않는다.
- 실제 종료·Wafer 교체 시 실행 마커를 지운다. 이전 실행 세대의 늦은 갱신이 마커를 되살리지 않아야 한다. Cycle Stop 요청만으로 아직 진행 중인 배출을 완료 처리하지 않는다.
- 격리 검증: 채움색/Result/BIN/선택 불변, 예약만 있는 셀의 오표시 방지, F/R 동시 대상, UID·instance·좌표 불일치, 수동 Step 대기, 종료 후 지연 사본, Designer/Dispose, 대형 맵의 부분 갱신을 확인한다.
- UI 갱신 사이에 끝나는 짧은 동작을 모두 육안으로 확인할 수 있다고 보장하지 않는다. 현재 상태 표시와 전체 이력 추적은 별도 요구사항이다. 실제 장비에서 표시 지연과 사이클 영향은 승인된 현장 검증이 필요하다.

이번 분석에서는 문서만 갱신했다. 장비 실행, 시퀀스/인터락 및 C# 소스 수정은 하지 않았다.

## 별도 확인·승인이 필요한 기존 항목

- 실제 실행 중인 Die/Output 타깃 노출: 기존 공개 데이터로 추정하지 않으며 시퀀스 변경 전 별도 승인 필요.
- Output 내부 표시 맵에는 NextIndex 기반 Result/BIN 추정 경로가 남아 있다. 모니터 색·상태·Result 열은 실제 슬롯 사본을 사용하며, 이 표시 맵을 저장에 사용하지 않는다. 수동 관리 경로의 별도 검토는 남아 있다.
- Input 초기 진입의 기존 Stage/Controller 복원 및 명시적 저장 경로는 유지했다. UI와 장비 제어의 전면 분리를 완료했다고 해석하면 안 된다.

## 현장 확인

1. 같은 Die/슬롯의 상태가 전환 화면과 WorkMain에서 같은 색·문구인지 확인한다.
2. GOOD BIN의 배치 전/배치 완료/검사 OK/검사 NG를 각각 확인한다.
3. Input에서 NG와 GOOD의 위치를 바꾸어도 실제 위치대로 표시되는지 확인한다.
4. Wafer 교체, SKIP, 예약/보유 픽커 변경, 화면 숨김/다시 열기, 확대/선택 유지를 확인한다.
5. 정지/알람/재개 중의 표시 지연을 측정한다. 실제 장비 구동은 승인과 장비 상태 확인 후 수행한다.
6. Output 저장 전후에 검사 NG/OK, 보정값/Raw, Die UID/Placement UID, SKIP 슬롯, 다음 수납 순번이 유지되는지 확인한다. 저장 실패 시 완료 메시지가 나오지 않는지도 확인한다.
