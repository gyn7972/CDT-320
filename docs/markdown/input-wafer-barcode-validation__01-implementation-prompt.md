# Input Wafer 바코드 LOT 검사와 얼라인 전 복구

입력 웨이퍼 바코드 확인과 맵 확보 중 발생한 오류의 복구를 얼라인 전에 끝낸다. 현재 생산 LOT ID와 웨이퍼 바코드의 앞 N글자를 비교하는 정책을 장비 공통 Config로 설정한다. 레시피를 바꿔도 사용 여부와 글자수는 유지한다. 같은 LOT의 `.02`와 `.03`을 구분하는 추가 검사는 이번 범위에서 제외한다.

## 데이터 및 UI

- `InputStageConfig.UseBarcodeLotPrefixCheck`는 기본 false, `BarcodeLotPrefixLength`는 기본 5로 저장한다.
- 새 키가 없는 구 Config는 역직렬화 시 false/5로 복원한다. 저장된 0, 음수, 128 초과 값은 조용히 보정하지 않는다.
- Input Stage 레시피 화면에 사용 여부와 비교 글자수 두 항목을 Config 범위로 표시한다. N은 1~128의 정수만 입력할 수 있다.
- 실제 운영 Config·레시피 파일과 바코드 사용 설정은 자동 변경하지 않는다. 저장은 기존 공통 설정 경로인 EquipmentData/Config/InputStageUnit.json을 사용한다.

## 검사 정책

- 공용 순수 함수 `InputWaferBarcodePolicy.TryValidate(enabled, prefixLength, barcodeEnabled, lotId, barcode, out reason)`를 사용한다.
- 꺼져 있으면 기존 동작을 유지한다. 켜져 있으면 바코드 사용 OFF, 잘못된 N, 현재 LOT 없음, 어느 한쪽 길이 부족, 접두어 불일치를 실패로 처리한다.
- LOT은 `MaterialStateService.GetProductionLotId()`를 사용한다. 바코드는 기존 scanner 정규화 이후의 값을 전달한다.
- 양쪽 앞 N글자를 `OrdinalIgnoreCase`로 비교한다. 이 검사는 웨이퍼 슬롯·확장자까지의 전체 ID 일치 검사가 아니다.
- 바코드 읽기·수동 확인·재시작 경로가 공통 정책을 거치고, 오류가 해결되기 전에는 얼라인으로 넘어가지 않는다. 잘못된 바코드를 Material 상태에 먼저 반영하지 않는다.
- 후보의 정책·맵 사전검증과 물리 웨이퍼·LOT·Config·레시피·BIN 설정 재확인을 마친 후에만 Material에 확정한다. 오류 복구 중 수동 오입력은 다시 검사하고, 취소·미완료 창 닫기는 진행을 차단한다.
- 최초 이적재 후 Working 상태는 정정을 허용한다. 얼라인 진행 스텝, 얼라인/맵/리뷰 결과 또는 픽업 예약·실행이 있으면 후단 바코드 정정을 차단한다. 수동/STEP 얼라인의 공통 검사는 `allowRecovery: false`로 확정값만 검사하며 복구창이나 바코드 변경을 실행하지 않는다.
- 맵 확보 오류의 재시도·취소·수동 확인 흐름은 얼라인 전 단계에 둔다. 기존 맵 파서, 좌표 변환, BIN 필터, 모션 및 인터락은 유지한다.

## 검증

실제 장비를 연결하지 않고 정책의 경계값, 꺼짐 호환성, 구 Config 키 누락, 명시적 잘못된 N 보존, 공통 Config 저장 복원과 레시피 전환 시 설정 독립성을 검사한다. 테스트는 production policy와 `InputStageUnit.cs`의 실제 Config 및 레시피 데이터 선언부를 격리 컴파일하며 장비 클래스를 생성하지 않는다. 실제 InputStage 공통 Config는 읽기만 하고 전후 SHA256을 확인한다.

바코드·맵 복구가 얼라인보다 먼저 끝나는 시퀀스 순서, 수동·재시작 경로, 취소 및 오류 경로는 별도 정적 검토와 회귀로 확인한다. 전체 솔루션은 별도 OutDir의 `/t:Build`로 컴파일한다. 실제 카메라·원격 통신·축 이동·얼라인·픽업은 이 검증에서 수행하지 않는다.
