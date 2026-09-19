# 버그기록: 후검사 VisionX 팔로잉 진입 INTERLOCK 오탐 (stale CommandPosition)

- 기록일: 2026-07-29
- 상태: **수정 완료 (2026-07-30, 팀장 승인으로 8/3 보류 철회)** — 아웃풋 동일 알람 재발(07-30 2회)로 앞당김.
  - 근본수정: `MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry`에서 판정 직전 `otherAxis.UpdateStatus()`
    호출로 보드 최신값 기준 판정(수정 후보 1안 채택). 6개 룰 공통 적용.
  - 증폭기 제거: `AjinAxis.MoveAbsoluteForFollowAsync`에 조용한 Can 사전검사 추가 — 팔로잉 최초 이동이
    가드에 차단되면 알람 없이 -11 반환 → 호출자 폴백(대기+일반 이동). Critical 정지 소멸.
  - 계측: Output/Input 비전 룰 차단 메시지에 `clearance=[actual…, command…]` 페어 수치 포함.
- (이력) 임시 조치: 팀장이 비전·피커 **가속도 일치**로 완화 적용(2026-07-29).
- 심각도: Critical 알람 → 자동 사이클 전체 정지 + RearPickerX 이동 절단(PK-MOVE -5) 연쇄
- 재현성: 간헐 (타이밍 레이스 — 아래 "재발 조건" 참조)

## 1. 증상

Output 후검사에서 OutputVisionX 팔로잉 진입 첫 명령이 MotionGuard에 거부되며 알람 3연쇄:

1. `02:19:19.925` **INTERLOCK [Critical]** (Source=OutputVisionX, 알람 id 1745)
   - "OutputCameraX 이동 불가: RearPicker가 Output 영역을 점유하거나 간섭 중이고 페어 간격도 부족합니다. movingX=True, ... outputCameraTarget=613.238, ... x=1088.238 ..."
2. `02:19:19.975` **PK-MOVE** (id 1746) — Critical로 사이클 Abort되며 퇴장 중이던 RearPickerX ABS 700 이동이 1046.238에서 절단: "이동 완료 후 Command 위치가 목표와 다릅니다. command=1046.238, target=700" → **2차 피해(원인 아님)**
3. `02:19:19.976` **SHARED-RAIL-X** (id 1747) — 동일 사유 재보고(에코)
4. `02:20:05.699` 리셋 [CLEARED]

## 2. 확정 원인 (로그 실측)

팔로잉 진입 첫 명령은 **라이브 실측** 기준으로 정확히 유지갭 50mm 뒤를 잡았는데,
인터락 제3 분기(페어 간격 예외)의 command 판정이 **약 62ms 묵은 CommandPosition 캐시**로
계산되어 간격 부족(−12.283mm)으로 오판 → 차단 + Critical 알람.

### 수치 (페어 OutputVisionX<->RearPickerX: HomeClearance=525, SafetyDistance=10, 팔로잉 유지갭=50=10+Extra40)

```
첫 명령    = 피커실측(1088.238) − homeGap(525) + safetyGap(50) = 613.238
actual 판정 : 525 + 613.238 − 1088.238(라이브 실측)      = +50.000 ≥ 10 → 통과 (설계 그대로)
command 판정: 525 + 613.238 − 1150.521(62ms 묵은 캐시)   = −12.283 < 10 → 실패 → 차단
```

- 피커 퇴장 속도 ≈ 1000mm/s (실측 1157.126@.855 → 1088.238@.925)
- 1150.521 − 1088.238 = 62.283mm = 약 62ms 분량의 staleness
- CommandPosition이 라이브였다면 양쪽 모두 +50으로 통과 → 알람 없음

### 프로퍼티 소스 불일치 (구조 원인)

| 프로퍼티 | 소스 | 위치 |
|---|---|---|
| `ActualPosition` | 접근 시마다 보드 직독(`AXM.GetCommandPosition`) — 라이브 | `QMC.CDT-320/Equipment/Ajin/AjinAxis.cs:51` |
| `CommandPosition` | `UpdateStatus()`→`ApplyReadStatus`에서만 갱신 — 캐시 | `QMC.CDT-320/Equipment/Ajin/AjinAxis.cs:2321` |

이동 대기 루프는 보드 직독으로 재설계되어(`AjinAxis.cs:3388~`) 이동 중 캐시 갱신 주기가 보장되지 않음 →
고속 이동 중 캐시가 수십 ms 묵을 수 있음. `OutputPostPlaceInspectionQueue.cs:1864` 주석에
"CommandPosition은 최종 목표가 아니라 보드 순시 프로파일 값(B1 알람 실측 증명)" 교훈이 이미 기록돼 있으나
인터락 제3 분기는 여전히 이 캐시를 신뢰함.

### 부수적으로 깨진 설계 가정 2건

1. `OutputPostPlaceInspectionQueue.cs:1878` 주석은 "가드 거부 → 조용한 -11 → 폴백(대기+일반 이동), 신규 알람 없음"을
   가정하지만, 실제 `MotionGuardRuntime.VerifyAxisMove`는 거부 즉시 `AlarmManager.Raise(INTERLOCK)`
   (`MotionGuardRuntime.cs:204`) → 폴백이 받기 전에 Critical이 장비를 세움.
2. 차단 메시지에 페어 계산 상세(`clearanceDetail`)가 **버려짐**
   (`OutputStageInterlockRules.cs:640-648`) — actual/command 페어 수치가 로그에 없어
   블랙박스 없이는 원인 확정 불가(이번 확정도 AlarmContext 블랙박스 의존).

## 3. 타임라인 근거 (2026-07-29 02:19:19)

블랙박스: `D:\CDT-320\Log\AlarmContext\20260729_021919_925_INTERLOCK.log`

| 시각 | 이벤트 | 근거 라인 |
|---|---|---|
| .617 | Place 완료, Z/Y 복귀, X 복귀 전 후검사 핸드오버(존 해제+카메라 존 조기 승인) | 블랙박스 11447-11450 |
| .621 | RearPickerX ABS 700 퇴장 시작 (vel=1000, acc=3500, scale50%) | 블랙박스 11465 |
| .855 | 팔로잉 게이트 1차 폴링: 실측 1157.126, gap 143.534 — 확대 감지 대기 | 블랙박스 11516 |
| .925 | 게이트 통과(gap 212.422 확대 중) → 팔로잉 진입 결정. 실측 1088.238 / Command캐시 1150.521 | 블랙박스 11538-11539 |
| .925 | FollowMoveAsync 첫 명령 613.238 발행 → MotionGuard 거부 → INTERLOCK [Critical] | 블랙박스 11540, Event CSV 19478 |
| .968 | 자동 사이클 AbortChildren | Event CSV 19485-19496 |
| .975 | RearPickerX 이동 1046.238에서 절단 → PK-MOVE -5 | 알람 JSON id 1746 |
| 02:20:05 | 리셋 [CLEARED] | Event CSV 19568 |

- 당일 이벤트: `D:\CDT-320\Log\Event\2026-07-29.csv` 19478/19554(알람), 19568/19570(클리어)
- 알람 이력: `D:\CDT-320\Log\Alarms\alarm_2026-07-29.json` id 1745-1747
- 대상 다이: `GM1SP-T150-G300-INPUT-MAP-X0198-Y0209` (side=Good, visionTarget=527.595)

## 4. 관련 코드 위치

| 역할 | 위치 |
|---|---|
| 인터락 룰(3중 판정: 존 간섭위험 → 퇴피예외 → 페어간격예외) | `QMC.CDT-320/Equipment/Interlocks/OutputStageInterlockRules.cs:597-696` |
| 페어 간격 예외 — Actual/Command AND 판정 | `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuleHelpers.cs:19-43` |
| 간격식 `homeClearance − aSign·a − bSign·b` | `QMC.CDT-320/Equipment/Motion/SharedRailX/SharedRailXMotionService.cs:74-117` |
| 팔로잉 첫 명령 `bound = 선행실측 − homeGap + safetyGap` | `QMC.CDT-320/Equipment/Ajin/AjinAxis.cs:834-867` |
| 거부 시 즉시 알람 발생 | `QMC.CDT-320/Equipment/Interlocks/Common/MotionGuardRuntime.cs:199-212` |
| 후검사 팔로잉 게이트/진입/폴백 | `QMC.CDT-320/Sequencing/OutputStage/OutputPostPlaceInspectionQueue.cs:1055-1109, 1884-2038, 2170-2253` |

## 5. 재발 조건 / 잔존 리스크

- 재발식: **CommandPosition 캐시 staleness(ms) × 선행 피커 속도(mm/ms) > safetyGap − SafetyDistance = 40mm**
  이면 첫 명령이 거부됨. (이번 건: 62ms × 1.0 = 62mm > 40mm)
- 팀장 가설/조치(2026-07-29): 비전·피커 **가속도 차이**가 원인으로 판단, 가속도 일치 적용.
  → 진입 시점 피커 속도가 낮아져 stale 오차(속도×staleness)가 줄어 오탐 확률 감소(완화 유효).
  → 단, 로그 실측 기준 구조 원인은 stale 캐시이므로 스레드 부하 등으로 staleness가 길어지면
    낮은 속도에서도 재발 여지 있음. 8/3 이후 근본 수정 필요.
- **Input 측 미러 동일 구조**: InputStage 쪽 동일 제3 분기도 같은 캐시를 쓰므로 같은 잠재 리스크.

## 6. 수정 후보 (8/3 이후, 착수 전 팀장 승인 필요)

1. **(권장) 제3 분기 command 읽기를 라이브로** — `IsPairClearanceSatisfiedForEntry`에서 상대축
   Command를 캐시 대신 보드 직독(또는 판정 직전 해당 축 UpdateStatus 1회). fail-closed 취지
   (접근 중 피커 차단)는 유지, 낡은 값 오판만 제거. Input 미러 동일 적용.
2. 팔로잉 첫 명령 보수화 — firstBound를 `max(실측, Command캐시)` 기준으로 계산해 가드 통과 보장,
   이후 오버라이드 루프가 따라잡음(가드 무수정, 진입 소폭 지연).
3. 팔로잉 첫 명령에 한해 가드 거부를 조용한 -11(알람 없음)로 → 설계 주석의 폴백 의도 복원.
   단, 인터락 알람 정책 변경이라 영향 범위 넓음.

공통: 어느 방향이든 차단 메시지에 `clearanceDetail`(actual/command 페어 수치) 포함 계측 추가
(수정마다 로그 계측 규칙). 수정 시 MotionSpeedScale 적용 여부 확인 필수.
