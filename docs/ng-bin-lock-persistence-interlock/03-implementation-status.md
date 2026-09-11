# NG BIN LOCK 수정 결과

## 결론

소스 수정과 오프라인 검증은 완료했다. 같은 이름의 Lock DI/DO는 Setup/Config 파일을 더 이상 함께 읽지 않으며, 컴파일된 객체 기준으로 Lock feedback은 M2/B19(X083), Lock output은 M4/B10(Y042)를 유지한다. Recipe는 기존 파일명 `NgBinCassetteLock.recipe.json`을 계속 사용한다.

`OutputCassetteInterlockRules.Verify()`는 모든 `OutputLifterZ` 이동 요청의 공통 진입점에서 실장비 원신호 `X082=ON && X083=ON`을 먼저 확인한다. 하나라도 OFF이면 JOG·MOVE·TEACH·HOME·자동 이동을 기존 세부 인터락으로 넘기기 전에 차단한다. Unit/Sequence 경로에는 같은 이동 인터락을 중복 구현하지 않았다. 수동 LOCK/UNLOCK 실린더 명령의 피드백 완료 판정은 100 ms 연속 유지 조건을 사용한다.

## 주소 및 영속 키

| 역할 | 논리 이름 | 적용 주소 | Setup/Config 키 | Recipe 키 |
|---|---|---|---|---|
| Unlock feedback DI | `NgBinCassetteBw` | X082 / M2-B18 | `NgBinCassetteBw` | `NgBinCassetteBw` |
| Lock feedback DI | `NgBinCassetteLock` | X083 / M2-B19 | `DI_NgBinCassetteLock` | `NgBinCassetteLock` |
| Lock output DO | `NgBinCassetteLock` | Y042 / M4-B10 | `DO_NgBinCassetteLock` | `NgBinCassetteLock` |
| Unlock output DO | `NgBinCassetteUnlock` | Y043 / M4-B11 | `NgBinCassetteUnlock` | `NgBinCassetteUnlock` |

기존 운영 `D:\CDT-320\EquipmentData\Setup\NgBinCassetteLock.json`은 보존했다. 새 영속 키 파일이 아직 없으므로 최초 기동에서는 `ajin-map.json`의 정상 주소를 생성자 기본값으로 사용한다.

## 2026-09-11 Recipe 적용 회귀와 수정

최초 구현에서는 DI/DO 분리 키를 공용 `StorageKey`에 넣었다. `BaseComponent`가 같은 키를 Setup/Config뿐 아니라 Recipe 저장·로드·검증에도 사용하므로, 프로젝트 적용 시 존재하지 않는 `DI_NgBinCassetteLock.recipe.json`을 찾아 실패했다.

수정 후에는 `SettingsStorageKey`를 Setup/Config에만 사용하고, Recipe는 기존 `StorageKey=NgBinCassetteLock`을 유지한다. 별도의 `DI_...recipe.json` 또는 `DO_...recipe.json` 생성이나 운영 Recipe 복사는 필요 없다.

## 2026-09-11 Unlock Check 화면 표시

X082는 Output Cassette 화면의 `NG BIN UNLOCK CHECK` B접점 원신호이므로 화면에서는 `IsNgBinUnlockCheck() = !IsNgBinBW()`로 반전 표시한다. 현 화면처럼 Lock 상태에서 X082 원신호가 ON이면 램프는 OFF로 보여 현재 Unlock 상태가 아님을 구분한다. 단, 실제 Lock 완료와 Z 이동 인터락은 표시값이 아니라 X082/X083 원신호가 모두 ON인지 확인한다.

11:10:03 및 11:12:44 로그에서는 Y042 Lock 출력, X082, X083이 모두 ON이었다. 실장비 확인에 따라 이 조합을 정상 Lock 완료 및 이동 허용 조건으로 확정했다. Recipe 화면에서는 같은 X082를 반전하므로 Unlock Check 램프는 OFF로 보인다.

수동 `NG BIN LOCK` 버튼은 센서나 명명 실린더 가드로 출력을 먼저 차단하지 않는다. LOCK이면 Y043 OFF→Y042 ON, UNLOCK이면 Y042 OFF→Y043 ON을 먼저 실행하고 `NG-BIN-LOCK-COMMAND` 로그를 남긴 뒤 X082/X083을 확인한다. 수동 센서 확인은 3초로 제한하며, 완료되지 않으면 출력 명령은 유지한 채 실패를 보고하고 다음 복구 조작을 허용한다. 축 이동 인터락은 이 실린더 제어와 분리되어 `OutputCassetteInterlockRules` 한 곳에서 적용한다.

## 2026-09-11 11:29 JOG 우회 원인 및 수정

운영 로그 `D:\CDT-320\Log\Main_2026-09-11.log`의 11:28:59 구간에서 `OutputLifterZ`가 1 mm 단위 ABS MOVE를 반복 발행했다. 당시 공통 MotionGuard 로그에는 OutputFeeder Y/Lift/Clamp 조건만 있었고 NG BIN LOCK 조건은 없었다. Recipe 화면의 STEP JOG가 일반 ABS MOVE 경로를 사용하므로, NG 자동 이송 경로에만 있던 검사는 적용되지 않았다.

수정 후에는 `OutputCassetteInterlockRules.Verify()`가 `OutputLifterZ`로 식별된 요청을 세부 이동 종류로 분기하기 전에 `CheckNgBinCassetteLockReady()`를 호출한다. 따라서 캡처처럼 Unlock Check 표시 ON(X082 원신호 OFF)이고 Lock Check OFF(X083 OFF)인 상태는 이동 명령 전에 차단된다.

## 오프라인 검증

- 운영 `D:\CDT-320\Config\ajin-map.json` 읽기 전용 대조: 네 신호 모두 위 표와 일치.
- 영속 키 분리 단독 상태 격리 Build: PASS, exit 0.
- 최종 정적 회귀 검사: PASS.
- 최종 격리 Build: PASS, exit 0.
- 컴파일된 `QMC.CDT-320.exe` reflection 검사: 실제 DI 객체 M2/B19, 실제 DO 객체 M4/B10, 설정 키 분리와 Recipe 키 유지 확인.
- 운영 `D:\CDT-320\Recipes\JMB_Rework\NgBinCassetteLock.recipe.json` 존재 확인 및 컴파일된 DI/DO 객체 `ValidateRecipe("JMB_Rework")`: 둘 다 PASS.
- 원본 저장소에서 Clean/Rebuild를 수행하지 않았고 운영 출력 폴더에 배포하지 않았다.

## 실장비 확인 절차

1. 변경 빌드를 정식 배포 절차로 반영한 뒤 장비 프로그램을 완전히 재시작한다.
2. 기동 로그에서 `name=NgBinCassetteLock, 카탈로그=M2/B19, 적용값=M4/B10` 경고가 더 이상 발생하지 않는지 확인한다.
3. UNLOCK 명령 시 Y042=OFF, Y043=ON이고 X082=OFF, X083=OFF인지 확인한다.
4. LOCK 명령 시 Y042=ON, Y043=OFF이고 X082=ON, X083=ON인지 확인한다.
5. X082 또는 X083 중 하나라도 OFF이면 OutputLifterZ JOG/MOVE/HOME이 차단되고 `INTERLOCK` 알람과 `OutputCassetteInterlock` 로그에 X082/X083 상태가 남는지 확인한다.
6. 마지막으로 `USE NG CASSETTE`를 OFF→ON 전환한 조건에서 저속·단동으로 NG mapping/load/unload를 확인한다.

실장비 원시 I/O 및 자동 운전 검증은 현재 `NOT RUN`이다.
