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

## 추가 수정 (팀장님 후속 지시): 다이얼로그 Side AF 시 Picker 이동

문제: 다이얼로그 수동 Side AF는 Picker를 전혀 움직이지 않아 카메라 앞에 대상이 없었음
(자동 Collet Cal 경로만 Z/T를 이동).

- [x] E1. `VisionFocusScanRequest.PrepareSidePickerPosition` 플래그 추가 (기본 false —
      자동 경로 무영향, 다이얼로그 Side kind에서만 true)
- [x] E2. 신설 `PrepareSideFocusPickerPositionAsync`: 다이얼로그에 표시되는 기준 좌표
      (`ResolveSideFocusReferenceTarget`, DieSidePosition)로 Picker 이동 후 스캔.
      표준 존 진입 순서(반대편 Picker Output 대피 → Z그룹 Avoid → Y 후진 → Side 작업영역
      점유 → X → Y → T(90도는 +90) → Z 하강), 존 태그 `VisionFocusCal;DieSidePosition;PickerPhase=*`
- [x] E3. Side kind에서 PickerSide를 Kind 기준으로 강제 (표시/이동/저장 불일치 방지)
- [x] E4. `ReserveFocusWorkArea(zone, front)` 오버로드 — Side 작업영역 점유 지원
- [x] E5. 델타 검증(에이전트): 자동 경로 무영향, 인터락 순서 정합(PickerX 이동 전제 충족),
      좌표 출처 = 다이얼로그 표시 Ref와 동일, 실패/취소 시 작업영역 해제 확인, 빌드 통과.
      검증 지적 2건(반대편 Picker 미대피, PickerSide 불일치)은 E2/E3에 반영

동작 참고:
- 수동 Side 스캔의 PickerZ는 항상 티칭(SidePosition) 기준으로 이동 → 스캔 완료 시 그 Z가
  저장됨. 즉 **다이얼로그 스캔 = 티칭 기준 재베이스라인**, 자동(Collet Cal) 스캔 = 저장값
  우선 적용. SidePosition 티칭을 바꾼 뒤 다이얼로그에서 한 번 스캔하면 저장값이 갱신됨
  (앞서 보고한 "저장값 무효화 경로 없음" 문제의 운영 해법)
- 기준 좌표는 런타임 Align offset 미포함(다이얼로그 표시값과 동일). 자동/생산 경로와
  Align offset만큼 차이가 날 수 있으나 스캔 범위(±0.2mm)가 흡수 — 실장비 확인 항목

## 추가 구현 (팀장님 컨펌 사양, 2026-07-12 저녁): Z옵셋 + COC 기반 Side 초점 보정

- [x] F1. Bottom↔Side 공용 Z옵셋: `UseBottomToSideZOffset`/`BottomToSideZOffsetMm`
      (VisionFocusCalibrationData 최상위). Side 촬영 PickerZ = 콜렛별 Bottom AF Best Z + 옵셋.
      캘리브레이션 AF와 생산 Side 검사 모두 **BottomDie AF Best 레코드**를 공통 기준으로 사용
      (캘리브레이션에서 레코드 없으면 FinalPickerZ 폴백, 로그에 소스 표기) — 검증 지적
      "콜렛 AF vs 다이 AF Z 기준 불일치" 해소
- [x] F2. Side 초점(카메라 Y) 보정을 Bottom 재측정 대신 **COC(회전 중심 편차 mm) + 레시피
      다이 사이즈**로 계산: Focus0 = coc0Sign×cocY, Focus90 = size90Sign×(가로−세로)/2 + coc90Sign×cocX.
      다이 사이즈는 RecipeProject.InputFrame(→Frame) 우선, Controller 기본값 폴백 — 검증 지적
      "Controller.DieSizeXMm 하드코딩(8.12×6.12), 레시피 아님" 해소
- [x] F3. 부호 6개(Front/Rear × size90/coc0/coc90)를 설정값으로 노출. 기본값은 팀장님 예제
      구조(die 8×6, COC 우측 1mm → Front90=0, Rear90=+2): sizeF=−1, sizeR=+1, coc*=+1.
      0 입력 시 해당 항 비활성. **실장비 테스트로 부호 확정 후 저장**
- [x] F4. Side 다이얼로그 설정 그리드에 8행 추가(B->S Z Offset Use/값, Sign 6개) —
      기존 패턴(enum/백킹/Load/Save/Refresh/Apply) 준수, SideOnly 프로파일에만 표시
- [x] F5. 델타 검증(에이전트) 1회 + 지적 2건(다이 사이즈 소스, Z 기준 불일치) 수정 반영,
      Clean Rebuild 통과

동작 변경 요약: 자동 Side AF의 Y 시작 = Process 티칭 + COC/사이즈 보정(저장 Best는 생산
소비/기록용), Z 시작 = 옵셋 사용 시 BottomDie AF Best + 옵셋 → 저장 PickerZ → 티칭 순.
Bottom 재측정(InspectBottomDieForSideFocusAsync) 호출 제거 — 다이 미보유여도 Side AF 진행.

실장비 부호 확정 절차: ① 다이얼로그에서 Z옵셋 입력+사용 ON ② Collet Cal AF 실행
③ `ColletCalSideFocusFormula`/`ColletCalSideAutoFocus` 로그의 cocOffsetMm/sizeTerm90/
focus0/focus90과 실제 초점 방향 대조 ④ 방향이 반대인 항은 다이얼로그에서 부호 반전 저장.
Process90 티칭이 이미 90도 면 기준으로 잡혀 있으면 size 항이 이중이 되므로 Sign Size90을
0으로 비활성.

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
