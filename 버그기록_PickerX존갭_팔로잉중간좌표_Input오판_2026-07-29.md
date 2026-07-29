# 버그기록 #2: PickerX 존 갭 — 팔로잉 중간좌표 Input 오판 Critical

- 기록일: 2026-07-29
- 상태: **설정 검증(재발 방지 장치) 코드 반영 완료 — 존 경계값 재설정(팀장 티칭)은 미완, 그 전까지 오탐 재발 가능**
- 관련: `버그기록_후검사VisionX팔로잉진입_INTERLOCK오탐_2026-07-29.md` (버그 #1)와 원인은 다르나
  "팔로잉 첫 명령이 MotionGuard에 거부되면 조용한 -11 폴백이 아니라 Critical로 전 시퀀스가 죽는"
  증폭 구조를 공유함(그 구조 자체는 8/3 이후 과제).

## 1. 증상 (2026-07-29 15:29:58.171)

```
Interlock blocked. moving=FrontPickerX. FrontPickerX 이동 불가: Input 픽업 영역을 반대 픽커가
사용 중입니다. owner=RearPickerPickUpSequence:PickUp currentZone=Side, targetZone=Input,
xActual=834.708, yActual=36.276.
```

- INTERLOCK [Critical] → 전 자동 시퀀스 Abort: **Rear 픽업이 진공 ON 상태 Z 싱크리프트 도중 취소**,
  GoodBinY 244.237 절단, OutputVisionX 403.821 절단(-5 ×2), Front/Rear/Input/Output 전부 Canceled.
- 15:30:24 리셋 [CLEARED].

## 2. 확정 원인 — 3단 오판정 체인

FrontPicker는 Input에 갈 의도가 없었고 **Output Place를 위한 팔로잉 진입**이었다.

1. **팔로잉 중간좌표가 존 갭에 낙하**: OutputVisionX(실측 402.8, 650.191로 이동 시작 직후)를 선행축으로
   FollowMoveAsync 첫 제약 명령 = `min(1014.616, 402.8 + 525(homeGap) − 50(safetyGap)) ≈ 877.8`.
   FrontPicker ZoneX 설정이 Side 715-835 / **(갭 836~899)** / Output 900-1350이라 877.8은 어느 존도 아님
   → Encoder 존 판정 Unknown.
2. **Y존 폴백이 가로챔**: X가 Unknown이면 `ResolveXZoneByPositionWithContext`(PickerZoneInterlockRules.cs:3223)가
   현재 PickerY 위치의 존으로 폴백 — FrontPickerY=36.276(사이드 검사 높이)이 픽 Y 밴드로 해석되어 **Input** 판정.
3. **명시 의도 토큰 무시**: targetName에 `PickerZone=Output` 토큰이 있었고 "빈 구간이면 명시 토큰 사용"
   분기(:2931)도 존재하지만, Y폴백이 그보다 먼저 값을 반환해 토큰까지 도달 못 함.

→ "targetZone=Input + Rear가 Input 픽업 영역 점유 중(정당)" 성립 → 정상 룰이 오탐 발동 → Verify 경로라 즉시 Critical.

**갭이 존재한 근본 이유**: 존 셋업 다이얼로그의 기존 저장 검증(`RangesOverlap`)이 "허용오차 포함
확장범위가 겹치면 저장 거부"여서 **인접 존 사이에 2×허용오차 초과의 갭을 구조적으로 강제**했다.
갭은 우연이 아니라 기존 검증 규칙의 산물.

## 3. 근거 로그

- 블랙박스: `D:\CDT-320\Log\AlarmContext\20260729_152958_171_INTERLOCK.log`
  - 11417-11419 (.167-.170): Place 피커X 팔로잉 진입 시작 (visionTarget=650.191, pickerTarget=1014.616,
    trailingTargetName=`DiePlacePosition[P4];...;PickerZone=Output`)
  - 11420-11423 (.171): Input 오판 차단 + Critical
  - 11164-11242 (.79x): Rear 픽업 Z 진행 중(점유 정당)
- Event CSV: `D:\CDT-320\Log\Event\2026-07-29.csv` 191400(알람), 191412-191413(연쇄 -5), 191430(클리어)
- 설정: `D:\CDT-320\EquipmentData\Setup\PickerFrontUnit.json` / `PickerRearUnit.json` ZoneX (양쪽 동일 값)

## 4. 조치 — 2번안 채택 (팀장 지시 2026-07-29): "판정 불가 구간을 만들 수 없게"

### 완료 (2026-07-29, 소스 반영·빌드 검증)

`QMC.CDT-320/Ui/Dialogs/PickerZoneSetupDialog.cs` (존 범위를 만드는 유일한 경로):

1. `ValidateGridRanges` 재작성 — Encoder Zone 사용 시 활성 존 정렬 후 인접쌍마다
   **`다음 MinX == 이전 MaxX + 2×허용오차`(±0.001)** 를 강제. 갭이든 겹침이든 저장 거부,
   위반 쌍 전부와 요구 시작값을 메시지에 표시. (확장범위가 정확히 맞닿는 이 배치가
   현행 판정식 matchCount==1에서 판정 불가 구간이 생기지 않는 유일한 배치.)
   Encoder Zone 미사용 시에는 기존 겹침 검사 유지(동작 무변경).
2. `LoadSelectedSetup` — 다이얼로그를 열 때 저장값을 같은 기준으로 검사해 위반 시
   상태줄에 "판정 불가 구간 있음(저장하려면 경계 수정 필요)" 경고.
3. 판정 로직·인터락·데이터 파일 무변경. 빌드 확인(에러 0).

### 미완 — 존 경계값 재설정 (팀장 티칭 결정 필요)

현 데이터(Front/Rear 동일)의 갭 4곳. 각 갭을 어느 존에 붙일지는 티칭 결정
(갭 구간이 어느 존이 되느냐에 따라 적용 인터락 성격이 달라짐):

| 인접쌍 | 현재 | 계약 충족 방법 (둘 중 택1) |
|---|---|---|
| Input(0~560) ↔ Bottom(570~684) | 갭 8 | Bottom.MinX=562 또는 Input.MaxX=568 |
| Bottom(~684) ↔ Avoid(689~710) | 갭 3 | Avoid.MinX=686 또는 Bottom.MaxX=687 |
| Avoid(~710) ↔ Side(715~835) | 갭 3 | Side.MinX=712 또는 Avoid.MaxX=713 |
| Side(~835) ↔ Output(900~1350) | **갭 63 (이번 사고 구간)** | Output.MinX=837 또는 Side.MaxX=898 |

- 이번 사고(877.8, Place 팔로잉 진입 경로)는 **Output.MinX=837(갭을 Output에 흡수)** 이 목적과 부합.
- 수정은 존 셋업 다이얼로그에서 저장(권장) 또는 **앱 종료 후** Setup json 직접 수정
  (Setup 파일은 앱 종료 시 덮어써짐 — 실행 중 파일 수정 금지).

## 5. 잔존 리스크

- 경계 재설정 전까지 갭 4곳 그대로 → 동일 오탐 재발 가능.
- 계약 충족 후에도 각 경계 중앙 1점(prev.MaxX+허용오차, 총 4점)은 이중매칭 → Unknown
  (이론상, 연산 좌표가 정확히 일치해야 해 확률 사실상 0. 완전 소멸은 판정식 타이브레이크 필요 — 후속 과제).
- 증폭기(-11이어야 할 팔로잉 첫 명령 거부가 Critical로 승격): 버그 #1과 공통, 8/3 이후 과제.

## 6. 운영 주의 (이번 분석 중 확인)

`QMC.CDT-320.csproj`의 출력 경로가 실장비 폴더 `D:\CDT-320\` 로 직결 —
**장비 앱 실행 중 소스 빌드 금지**(파일 잠금으로 실패하나 부분 복사 위험).
검증 빌드는 `/p:OutDir=<임시폴더>` 로 출력을 돌려서 할 것.
