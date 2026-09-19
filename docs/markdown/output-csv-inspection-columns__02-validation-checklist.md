# OUTPUT CSV Bottom/Side 검사 컬럼 확장 검증 체크리스트

기록일: 2026-09-11. `01-implementation-prompt.md`에 따른 구현과 검증 결과를 기록한다.

## 작업 상태

| 구분 | 검증 항목 | 상태 | 실행 근거 |
|---|---|---|---|
| 저장소 | 기준 루트가 `D:\Source\CDT-320_New`, 브랜치가 `master`인지 확인 | PASS | 구현 시작 전 `git rev-parse`, `git branch --show-current` 확인 |
| 작업 트리 | 기존 WaferMap/UI 변경과 대상 Writer 변경이 겹치지 않음 | PASS | 대상 `VisionInspectionResultFileWriter.cs` 기존 diff 없음 |
| 구현 | OUTPUT 상세 헤더 뒤에 9개 컬럼 추가 | PASS | 빌드된 Handler 상수 reflection 검증: 앞 18열 유지, 추가 9열 포함 총 27열 |
| 구현 | OUTPUT 상세행 27열 생성 및 열 수 검사 | PASS | 빌드된 `BuildPlacePayload` fixture 실행, 27열 확인 |
| 구현 | INPUT과 동일한 Bottom/Side 값·단위·방향 매핑 | PASS | 같은 `DieMaterial`로 INPUT 29~37열과 OUTPUT 18~26열 값 일치 확인 |
| 구현 | `Back_Foreign_Size` 공백 계약 유지 | PASS | INPUT/OUTPUT fixture 모두 공백 확인 |
| 호환성 | legacy 18열 상세행에 빈 9열 보강, 27열 재확장 방지 | PASS | quoted comma 포함 18열 행을 27열로 1회 확장, 기존 문자열 보존 및 2회차 무변경 확인 |
| 회귀 | 기존 18개 OUTPUT 열과 upsert key 보존 | PASS | 헤더 앞 18열 정적 대조, Flying Die `PlaceRow/Col=-1`, `TargetBin=255`, key 열 `5/3/4` 확인 |
| 운영 자료 | INPUT/OUTPUT 샘플 SHA256 전후 보존 | PASS | INPUT `26D6297B...F1CF2A`, OUTPUT `D0A7070F...F73C8C` 전후 동일 |
| 정적 검사 | `git diff --check` 및 매핑 검증 | PASS | 공백 오류 없음, 27열 헤더와 9개 매핑 순서 확인 |
| 빌드 | 별도 OutDir `/t:Build`, Debug / Any CPU | PASS | exit 0, 오류 0, 기존 경고 41개 |
| 장비 | 실제 OUTPUT 신규 생성·재개·네트워크 복사 | NOT RUN | 현장 검증 필요 |

### 회귀 검증 재실행

전체 솔루션을 별도 `OutDir`로 빌드한 뒤 실제 Handler 어셈블리의 private CSV 생성·legacy 확장 메서드를 reflection으로 호출한다. 장비 애플리케이션을 시작하거나 운영 폴더에 파일을 쓰지 않는다.

```powershell
& .\docs\output-csv-inspection-columns\tests\run-tests.ps1 `
  -BuildOut .\_build_check_handler\output-csv-inspection-columns\out
```

## 현장 검증 항목

| 항목 | 확인 내용 | 상태 |
|---|---|---|
| 신규 OUTPUT | 새 파일의 상세 헤더와 모든 상세행이 27열인지 확인 | NOT RUN |
| 값 대조 | 같은 다이의 INPUT/OUTPUT 9개 컬럼이 동일한지 확인 | NOT RUN |
| 재개 파일 | 18열 파일 재개 시 기존 행은 빈 9열, 신규·갱신 행은 실제 9개 값인지 확인 | NOT RUN |
| NG/Flying Die | 기존 위치·빈 코드 정책을 유지하면서 검사 컬럼이 기록되는지 확인 | NOT RUN |
| 네트워크 복사 | 복사된 OUTPUT CSV도 로컬 원본과 동일한 27열 구조인지 확인 | NOT RUN |

## 남은 계약

- `Back_Foreign_Size`는 현재 Bottom Vision 원문에 대응하는 확정된 Size 측정 키가 없어 INPUT과 OUTPUT 모두 공백으로 유지한다.
- 실제 값을 기록하려면 Vision 측정 키, 의미, 단위, valid 조건을 별도로 합의해야 한다.

## 검증 결과 기록

- 변경된 생산 코드는 `VisionInspectionResultFileWriter.cs` 한 파일이다. INPUT 생성 경로, 출력 파일명, 요약행, Raw 저장, 네트워크 복사, queue/upsert key 정책은 변경하지 않았다.
- 회귀 테스트는 빌드된 실제 Handler 어셈블리의 private 메서드를 reflection으로 호출했으며 장비 애플리케이션은 실행하지 않았다.
- Bottom Chipping의 mm→um 변환, Side 4방향 매핑, Side valid 실패 공백, 유효한 0 보존, `Back_Foreign_Size` 공백을 확인했다.
- Flying Die는 27열로 확장되면서 기존 `PlaceRow/Col=-1`, `TargetBin=255`, 입력 좌표 기반 upsert key를 유지한다.
- legacy 18열 행은 기존 텍스트 뒤에 빈 필드 9개만 붙이며, 이미 27열인 행은 변경하지 않는다.
- 빌드 출력은 `D:\Source\CDT-320_New\_build_check_handler\output-csv-inspection-columns\out`에 생성했다. 원본 저장소에서 Clean/Rebuild를 실행하지 않았다.
- 실제 생산 OUTPUT 파일 생성, 재시작 재개, NG 안착, Flying Die 기록, 네트워크 복사는 현장 미수행이다.
