# Picker Vacuum / Material 인터락 검증 체크리스트

## 구현

- [x] 실제 AJIN Vacuum Output 동기 읽기 및 읽기 실패 차단
- [x] Front/Rear P1-P4 전체 Vacuum Output `OFF` 확인
- [x] Picker 현재 위치/예약 Die 데이터 없음 확인
- [x] Recipe 변경 Flow Sensor 의존 제거
- [x] `INPUT CST CLEAR` Flow Sensor 의존 제거
- [x] Output 측 Die 데이터가 `INPUT CST CLEAR`를 차단하지 않는 계약 유지
- [x] 기존 Ring/정지/Alarm/축/작업 Gate 유지

## 검증 결과

- [x] `PASS` Controller 오프라인 회귀 테스트: 59 assertions
- [x] `PASS` Input Cassette Material 회귀 테스트: 31 cases / 217 assertions
- [ ] `NOT RUN` 범위 밖의 다른 오프라인 테스트
- [x] `PASS` `git diff --check`
- [x] `PASS` UTF-8 BOM / CRLF / final newline 검사
- [x] `PASS` 격리 Debug Any CPU `/t:Build`
- [ ] `NOT RUN` 실장비에서 Vacuum Output/Material 조합 검증

## 현장 확인 조합

| Vacuum Output | Picker 제품 데이터 | 기대 결과 |
|---|---|---|
| OFF | 없음 | 허용 |
| ON | 없음 | 차단 |
| OFF | 있음 | 차단 |
| ON | 있음 | 차단 |

추가 확인: Flow Sensor가 `ON`이어도 Vacuum Output이 모두 `OFF`이고 Picker 제품 데이터가 없으면 Recipe 변경 및 `INPUT CST CLEAR`가 Flow 때문에 차단되지 않아야 한다.
