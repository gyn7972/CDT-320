# OUTPUT CSV Bottom/Side 검사 컬럼 확장 구현 프롬프트

## 작업 목적

로컬 `D:\Source\CDT-320_New`의 현재 `master`에서 `D:\CDT-320\OUTPUT`에 생성되는 고객 결과 CSV의 다이 상세행에 INPUT CSV와 동일한 Bottom/Side 검사 컬럼 9개를 추가한다.

현재 INPUT CSV에는 아래 컬럼이 있지만 OUTPUT CSV 상세행은 18열까지만 생성하므로 값이 기록되지 않는다.

1. `Back_Chipping_Top_Size`
2. `Back_Chipping_Right_Size`
3. `Back_Chipping_Bottom_Size`
4. `Back_Chipping_Left_Size`
5. `Back_Foreign_Size`
6. `Side_Chipping_Bottom`
7. `Side_Chipping_Left`
8. `Side_Chipping_Top`
9. `Side_Chipping_Right`

## 기준 저장소와 작업 제한

1. 저장소 루트는 `git rev-parse --show-toplevel`로 확인하고 `D:\Source\CDT-320_New`만 사용한다.
2. 현재 로컬 `master`에서 작업하며 별도 요청 없이 브랜치 전환, 커밋, 푸시, pull, reset, checkout을 수행하지 않는다.
3. 기존 dirty 파일은 다른 작업으로 간주해 보존한다. 이번 작업 대상과 겹치면 변경 전 diff를 확인한다.
4. `D:\CDT-320`은 운영 배포·데이터 영역이다. INPUT/OUTPUT 샘플은 읽기 전용으로만 사용하고 운영 CSV를 수정하거나 빌드 출력으로 사용하지 않는다.
5. 원본 저장소에서 Clean/Rebuild를 실행하지 않는다. 빌드는 별도 `OutDir`를 지정한 `/t:Build`만 사용한다.
6. 실제 장비 구동, 자동 사이클, 축 이동, IO 출력, 카메라 연결은 수행하지 않는다.

## 구현 요구사항

### OUTPUT 상세 헤더

- `VisionInspectionResultFileWriter.OutputDieHeader`의 기존 18개 컬럼 순서와 이름을 변경하지 않는다.
- 기존 마지막 컬럼 `placement_offset_y_mm` 뒤에 요청된 9개 컬럼을 위 순서 그대로 추가한다.
- OUTPUT 다이 상세행의 기대 열 수를 18열에서 27열로 변경한다.
- OUTPUT 요약 헤더·요약 데이터, 파일명, Raw 파일, 네트워크 복사 정책은 변경하지 않는다.

### 검사값 매핑

- INPUT CSV 생성 경로의 현재 변환 함수와 유효성 정책을 그대로 사용한다. INPUT 생성 코드는 리팩터링하지 않는다.
- Back Chipping 4방향은 Bottom 검사 레코드에서 아래 키를 읽고 기존 `FormatBottomMetric(..., true)`를 사용해 mm를 um로 변환한다.
  - Top: `bottom_item_chipping_top`
  - Right: `bottom_item_chipping_right`
  - Bottom: `bottom_item_chipping_bottom`
  - Left: `bottom_item_chipping_left`
- `Back_Foreign_Size`는 INPUT의 현재 계약과 동일하게 공백으로 기록한다. `t_foreign`, Side foreign count/max 등 의미가 다른 값을 추측해 대입하지 않는다.
- Side Chipping은 `DieMaterial.Inspections`의 `Side0`, `Side90` 레코드를 사용하고 기존 `FormatSideChippingDepth`의 measure/channel valid 게이트와 mm→um 변환을 그대로 적용한다.
  - Bottom: Side0 FrontSide ch0
  - Left: Side90 RearSide ch1
  - Top: Side0 RearSide ch0
  - Right: Side90 FrontSide ch1
- 측정값 누락, `measure_valid` 실패 또는 채널 valid 실패는 INPUT과 동일하게 빈 문자열을 기록한다. 유효한 숫자 `0`은 빈 값으로 바꾸지 않는다.
- GOOD, NG, Flying Die 경로가 모두 기존 `BuildPlacePayload`를 통과하도록 유지한다. Flying Die의 기존 키 열과 `PlaceRow/Col=-1`, `TargetBin=255` 정책을 변경하지 않는다.

### 기존 18열 파일 호환

- 배포 후 재시작·재개로 기존 18열 OUTPUT 파일에 이어 쓰는 경우 헤더만 27열로 바뀌고 기존 상세행이 18열로 남는 혼합 스키마를 만들지 않는다.
- OUTPUT 파일 preamble 확인 시 상세행을 CSV 규칙으로 판별하고 정확히 18열인 기존 행에만 빈 필드 9개를 뒤에 추가한다.
- 기존 18개 필드의 문자열과 열 위치는 보존한다. 27열인 행은 다시 확장하지 않는다.
- 18열 또는 27열이 아닌 기존 행을 임의로 잘라내거나 데이터로 추정해 보정하지 않는다.
- 과거에 완료되어 다시 열리지 않는 운영 OUTPUT 파일을 일괄 수정하지 않는다. 별도 과거 데이터 백필은 이 작업 범위가 아니다.

## 검증 요구사항

1. 변경 전후 `OutputDieHeader`의 앞 18개 컬럼이 정확히 동일하고, 추가 9개 컬럼을 포함해 총 27개인지 확인한다.
2. OUTPUT 상세행의 앞 18개 값 생성식과 upsert key 열 인덱스가 변경되지 않았는지 확인한다.
3. INPUT과 OUTPUT에서 Back/Side 9개 컬럼의 순서, 변환 함수, Side 방향 매핑이 동일한지 정적 대조한다.
4. `Back_Foreign_Size`가 INPUT과 OUTPUT에서 모두 공백 계약인지 확인한다.
5. 합성 legacy 18열 상세행이 27열로 한 번만 확장되고 기존 18개 값이 보존되는지 확인한다. 기존 27열 행은 변경되지 않아야 한다.
6. 운영 샘플 `YZ9M6M1-03_2026091018.csv`와 `AK_DT_YZ9M6M1-03_DS534_202609101829.csv`는 읽기 전후 SHA256이 같아야 한다.
7. `git diff --check`, 관련 정적 검증, 별도 `OutDir`의 `/t:Build`를 수행한다.
8. 실제 장비에서 새 OUTPUT CSV 생성·재개·네트워크 복사는 미수행으로 구분한다.

## 결과 보고

- 변경 파일과 기존 dirty 파일 보존 여부를 보고한다.
- 정적, 합성/단위, 빌드, 운영 샘플 읽기 검증, 실제 장비 검증을 구분해 `PASS`, `FAIL`, `NOT RUN`, `BLOCKED`로 기록한다.
- 기존 18열 OUTPUT 파일의 처리 방식과 과거 데이터가 자동 백필되지 않는 점을 명시한다.
- `Back_Foreign_Size`의 비전 계약이 확정되지 않아 공백으로 유지된다는 점을 명시한다.
