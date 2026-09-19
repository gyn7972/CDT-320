# NG BIN LOCK 수정 검증 체크리스트

| ID | 검증 항목 | 기준 | 결과 |
|---|---|---|---|
| P-01 | DI/DO 설정 영속 키 분리 | DI=`DI_NgBinCassetteLock`, DO=`DO_NgBinCassetteLock` | PASS |
| P-02 | 논리 이름 보존 | DI/DO `Name`은 모두 `NgBinCassetteLock` | PASS |
| P-03 | 기존 공유 운영 JSON 보존 | 파일 수정·삭제 없음 | PASS |
| R-01 | Recipe 키 하위 호환 | DI/DO 모두 기존 `NgBinCassetteLock.recipe.json`으로 검증 | PASS |
| F-01 | Lock 완료 및 이동 허용 | X082 ON, X083 ON일 때만 성공 | PASS (정적) |
| F-02 | Unlock 완료 조건 | X082 OFF, X083 OFF일 때 성공 | PASS (정적) |
| F-03 | X082 표시 극성 분리 | 인터락은 원신호, Recipe 화면 Unlock Check는 반전 표시 | PASS (정적) |
| I-01 | 공통 MotionGuard 위치 | `OutputCassetteInterlockRules.Verify()`에서 세부 분기 전에 검사 | PASS (정적) |
| I-02 | JOG/MOVE/TEACH/HOME/자동 공통 차단 | 모든 `OutputLifterZ` 요청에 동일 조건 적용 | PASS (정적) |
| I-03 | Lock 허용 조건 | X082/X083 원신호가 모두 ON일 때만 다음 인터락으로 진행 | PASS (정적) |
| I-04 | 중복 인터락 제거 | Unit/Sequence에 `EnsureNgBinCassetteLockedAsync` 없음 | PASS (정적) |
| U-01 | 수동 LOCK/UNLOCK 버튼 | 명령 후 피드백 완료를 await하고 실패 반환 | PASS (정적) |
| U-02 | UNLOCK CHECK 화면 표시 | X082 원신호를 반전한 `IsNgBinUnlockCheck()` 표시 | PASS (정적) |
| U-03 | 수동 버튼 실행 순서 | 센서 사전검증 없이 Y042/Y043 출력 후 X082/X083 확인 | PASS (정적) |
| U-04 | 수동 센서 확인 시간 | 3초 후 실패 반환하여 다음 복구 조작 허용 | PASS (정적) |
| L-01 | 실패 로그 | 출력과 Lock/Unlock 피드백 상태 포함 | PASS (정적) |
| B-01 | 격리 빌드 | Clean/Rebuild 없이 별도 OutDir Build 성공 | PASS |
| E-01 | 실장비 원시 I/O 확인 | X082/X083/Y042/Y043 현장 확인 | NOT RUN |
| E-02 | 실장비 자동 운전 확인 | NG Stage 비사용→사용 전환 후 자동 동작 확인 | NOT RUN |

상태 표기: `PASS`, `FAIL`, `NOT RUN`, `BLOCKED`.

## 검증 기록

- 운영 `ajin-map.json` 읽기 전용 확인: X082/M2-B18, X083/M2-B19, Y042/M4-B10, Y043/M4-B11.
- 영속 키 분리만 적용한 1단계 빌드: 오류 0, exit 0.
- 최종 인터락 적용 후 정적 회귀 검사: 전 항목 PASS.
- 최종 격리 빌드: 오류 0, exit 0. 경고는 기존 미사용 필드/미대기 호출 항목이다.
- 2026-09-11 회귀 수정: 설정 키가 Recipe 검증에도 사용되어 `DI_NgBinCassetteLock.recipe.json`을 요구하던 문제를 제거했다.
- 2026-09-11 11:28:59 운영 로그: X082/X083 Lock 공통 검사 없이 OutputLifterZ STEP JOG ABS MOVE가 반복 발행된 사실을 확인했다.
- 공통 `OutputCassetteInterlockRules` 진입점으로 Lock 검사를 이동하고 Unit/Sequence의 중복 이동 인터락을 제거했다.
- 컴파일된 어셈블리 reflection 확인: DI는 M2/B19 + 설정 키 `DI_NgBinCassetteLock`, DO는 M4/B10 + 설정 키 `DO_NgBinCassetteLock`; 두 객체의 Recipe `StorageKey`는 기존 `NgBinCassetteLock`.
- 운영 `JMB_Rework` 대상으로 컴파일된 DI/DO 객체의 `ValidateRecipe` 실행: 둘 다 PASS.
- 장비 프로그램 실행, I/O 출력, 축 이동 및 자동 운전은 수행하지 않았다.
