# Side AF PickerZ/VisionY 저장·적용 — 설계 및 체크리스트

- 작성일: 2026-07-12
- 지시: 팀장님 — "Side 오토포커스 시 필요한 PickerZ 축과 FrontVisionY/RearVisionY 값을
  저장할 수 있게 하고, AF 진행 시 해당 값들을 저장하며, 실행 중 오토포커스에도 적용하라."

## 조사 확정 사실 (Explore 에이전트 전수 조사)

1. Side AF 실행 경로는 2개뿐: (a) 다이얼로그 수동 스캔(VisionFocusCalibrationDialog →
   VisionFocusScanSequence), (b) Collet Cal 자동 실행(ColletCalibrationSequence.
   RunSideAutoFocusAsync → 동일 엔진). 생산 런타임 AF는 Bottom 전용(Side 없음)
2. Side AF Best(FOCUS_BEST)는 `VisionFocusPositionRecord.BestPosition`에 저장되며
   이것이 해당 Side의 **Vision Y 초점 위치**다 (Front 레코드=FrontVisionY,
   Rear 레코드=RearVisionY). **PickerZ는 저장되지 않았고**, AF는 항상 picker 티칭
   `SidePosition`으로 Z를 이동했다
3. 생산 Side 검사는 BestPosition을 SideVisionY base로 소비, PickerZ는 티칭
   `SidePosition`+런타임 zOffset 사용 (이번 수정으로 변화 없음)

## 해석 (팀장님 지시 → 구현)

- "저장할 수 있도록" + "진행하면 저장" → AF 완료 시(수동/자동 공통 엔진 SaveBestStep)
  촬영 당시 PickerZ actual을 레코드에 자동 저장. VisionY는 기존 BestPosition이 담당
- "실행중 오토포커스에도 적용" → 자동 실행 AF(RunSideAutoFocusAsync)가 저장값이
  유효하면 **PickerZ=저장값, 스캔 중심 Y=저장 Best**로 시작. 저장값이 없으면
  기존 계약(SidePosition 티칭 / Process 티칭+보정) 그대로
- 수동 다이얼로그 스캔은 기존대로 Picker를 움직이지 않음(작업자가 위치시킨 상태
  그대로 스캔·저장) — 이것이 티칭 수단이 됨

## 수정 내용

- [x] D1. `VisionFocusPositionRecord`에 `PickerZPosition`/`PickerZValid` 필드 +
      `ApplyPickerZ()` 추가, `ClonePositionRecord` 복사 반영
      ([VisionFocusCalibrationData.cs](../../QMC.CDT-320/Equipment/Calibration/VisionFocusCalibrationData.cs))
- [x] D2. 스캔 엔진 `SaveBestStep`: Side kind일 때 요청 Side/PickerNo의 PickerZ actual
      저장 + `VisionFocusSideSave` 로그
      ([VisionFocusScanSequence.cs](../../QMC.CDT-320/Sequencing/Calibration/VisionFocusScanSequence.cs))
- [x] D3. `RunSideAutoFocusAsync`: 저장 레코드 유효 시 PickerZ/DefaultY에 저장값 적용,
      시작 위치 결정 로그(`pickerZSource`/`visionYSource`) 추가. 저장값 사용 시
      focusCorrection은 미적용(Best에 이미 이전 보정이 반영된 절대 위치 — 이중 적용 방지)
      ([ColletCalibrationSequence.cs](../../QMC.CDT-320/Sequencing/Calibration/ColletCalibrationSequence.cs))
- [x] D4. 다이얼로그 SAVED RESULT 그리드에 `PICKER Z` 컬럼 추가 (Designer 인라인,
      Bottom 레코드는 "-" 표시)

### 검증
- [x] V1. Clean Rebuild (별도 obj) 성공 (보강 후 재확인)
- [x] V2. 멀티에이전트 적대적 검증(7에이전트) 완료 — 핵심 로직 결함 0건.
      직렬화 하위호환(기존 JSON 로드/구버전 롤백 안전), 축·슬롯 매핑(오프바이원 없음),
      존 태그/이동 함수 동일, 첫 실행 폴백 HEAD 동일, 생산 경로 무영향, UI 컬럼/셀 정합 확인.
      검증이 권고한 보강 3건 반영: (1) PickerZ 축 선택을 요청 PickerSide가 아닌 Kind 기준으로
      변경(다이얼로그 측 선택 불일치 방지), (2) EnsureDefaults에 PickerZValid+NaN 조합 방어,
      (3) PickerZ 축 미해석 시 건너뜀 로그 추가

## 미반영 권고 (팀장님 판단 필요)

1. **저장값 무효화 경로 없음**: 저장된 PickerZ/Best는 티칭 재교시나 die/레시피 변경 후에도
   계속 적용됨. 스캔 범위(기본 ±0.2mm)를 넘는 변경은 AF가 자체 회복 불가 → 다이얼로그에서
   재스캔(재저장)으로 갱신하는 운영 전제. 티칭 저장 시 자동 무효화가 필요하면 별도 지시 요망
2. **수동 스캔 Z 저장은 무검증**: 작업자가 Picker를 임의 위치에 둔 채 Side 스캔하면 그 Z가
   저장되어 다음 자동 AF 시작점이 됨(티칭 수단이기도 함). SidePosition 티칭 대비 허용 편차
   가드가 필요하면 별도 지시 요망

## 실장비 확인 항목

1. 저장값이 아직 없는 첫 AF는 기존 동작과 동일 — 첫 실행 후 그리드에 PICKER Z가
   채워지는지 확인
2. 수동 다이얼로그 스캔은 현재 Picker 위치 그대로 저장하므로, 스캔 전 Picker를
   Side 촬영 높이에 위치시킨 뒤 실행할 것
3. 두 번째 자동 AF부터 로그 `ColletCalSideAutoFocus`의 `pickerZSource=SavedRecord`,
   `visionYSource=SavedBest` 확인
