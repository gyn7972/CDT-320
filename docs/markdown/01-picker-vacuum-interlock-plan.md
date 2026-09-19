# Picker Vacuum / Material 인터락 수정 계획

## 사용자 확정 조건

- Recipe 변경은 모든 Front/Rear Picker P1-P4의 실제 Vacuum Output이 `OFF`이고, Picker 보유 또는 예약 Die 데이터가 없을 때만 허용한다.
- 작업정보의 `INPUT CST CLEAR`도 같은 두 조건을 모두 만족해야 한다.
- `INPUT CST CLEAR`에서는 Output Stage/Feeder/Cassette에 남은 Die 이력과 제품 데이터만으로 작업을 차단하지 않는다.
- Flow Sensor는 실장비에서 Vacuum 비인가 상태에도 `ON`일 수 있으므로 위 두 작업의 제품 유무 근거로 사용하지 않는다.

## 구현 범위

1. AJIN의 실제 출력 상태를 동기 읽기하고 캐시를 갱신하는 공용 함수를 추가한다.
2. Material 단일 기준에서 Picker 현재 위치 및 Picker 예약 Die가 없는지 확인하는 읽기 전용 검사를 추가한다.
3. Recipe 변경과 `INPUT CST CLEAR`가 같은 `Vacuum Output OFF + Picker 제품 데이터 없음` 게이트를 사용하도록 한다.
4. 기존 Alarm, 작업 Gate, 축 정지, Input Feeder/Stage Ring 센서 및 Recipe 변경의 기타 물리 감지 인터락은 유지한다.
5. 실제 출력 읽기 실패, 장비/출력 객체 누락, Material 불명확 상태는 허용하지 않고 차단한다.

## 비대상

- Picker 공정 중 Flow Sensor 검사와 Pickup 판정은 변경하지 않는다.
- GOOD/NG Output Cassette의 별도 Clear 동작은 변경하지 않는다.
- 배포 폴더와 실장비 I/O는 건드리지 않는다.

## 검증

- 오프라인 Controller 회귀 테스트로 8개 Vacuum Output과 Picker 데이터의 AND 조건을 검증한다.
- Flow 입력이 `ON`이어도 두 대상 게이트의 판단에 사용되지 않는지 검증한다.
- Output 측 Die 데이터가 있어도 Picker 데이터가 없으면 `INPUT CST CLEAR`가 허용되는 기존 Material 계약을 회귀 검증한다.
- 격리 `OutDir`에 `/t:Build`만 수행한다. 실장비 검증은 별도 현장 항목으로 남긴다.
