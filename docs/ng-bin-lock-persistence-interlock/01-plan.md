# NG BIN LOCK 영속 키 및 이동 인터락 수정 계획

## 목적

- 동일한 논리 이름을 가진 NG BIN LOCK DI와 DO가 같은 Setup/Config 파일을 공유하는 문제를 제거한다.
- NG BIN LOCK 완료 및 Z 이동 허용은 X082와 X083 원신호가 모두 ON일 때로 판정한다.
- `OutputCassetteInterlockRules`의 공통 `OutputLifterZ` 진입점에서 X082/X083 Lock 완료를 확인한다.
- 완료 피드백을 얻지 못하면 JOG·MOVE·TEACH·HOME·자동 이동을 시작하지 않고 상태를 로그에 남긴다.

## 변경 범위

1. `DioDefault`에 표시 이름과 독립적인 Setup/Config 전용 영속 키를 추가한다.
2. `NgBinCassetteLock` DI 설정은 `DI_NgBinCassetteLock`, DO 설정은 `DO_NgBinCassetteLock`을 사용한다.
3. 실 I/O와 Sim I/O 생성 시 설정 키를 `SettingsStorageKey`에 전달한다. Recipe용 `StorageKey`는 기존 `NgBinCassetteLock`을 유지한다.
4. X082 B접점 원신호는 `IsNgBinUnlockCheck()`에서 반전해 화면에 표시하되, Lock 완료 인터락은 X082/X083 원신호가 모두 ON인지 확인한다.
5. Unit/Sequence별 중복 검사는 두지 않고 `OutputCassetteInterlockRules.Verify()`가 모든 `OutputLifterZ` 이동 요청을 공통 차단한다.
6. 수동 I/O 버튼도 출력 명령 직후 성공으로 처리하지 않고 피드백 완료까지 기다린다.

## 호환 및 안전 원칙

- 논리 `Name`은 변경하지 않아 `ajin-map.json`, I/O 화면 이름, 기존 코드 조회를 보존한다.
- 기존 Recipe 파일명 `NgBinCassetteLock.recipe.json`을 유지하며 `DI_...recipe.json` 또는 `DO_...recipe.json`을 요구하지 않는다.
- 운영 파일 `D:\CDT-320\EquipmentData\Setup\NgBinCassetteLock.json`은 수정·삭제하지 않는다.
- 새 영속 파일이 없으면 카탈로그/`ajin-map.json`에서 해석한 주소를 생성자 기본값으로 사용한다.
- Output Cassette 리프터는 GOOD/NG 목표와 무관하게 NG BIN이 물리적으로 Lock된 경우에만 이동한다.
- 실장비 I/O 구동 및 실제 모션 검증은 이 작업에서 수행하지 않는다.

## 검증 범위

- 소스 정적 검사와 별도 테스트 스크립트
- 기존 솔루션의 격리 `OutDir` Build
- 변경 파일 diff 및 기존 사용자 변경과의 비중첩 확인
- 실장비 X082/X083/Y042/Y043 검증은 별도 현장 확인
