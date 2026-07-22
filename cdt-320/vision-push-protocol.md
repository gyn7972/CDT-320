# Vision 검사 결과 푸시 프로토콜 계약 (핸들러 ↔ 비전 PC)

- 확정일: 2026-07-20
- 목적: 검사 결과 회수를 **요청·폴링(Pull) 방식에서 자발 푸시(Push) 방식으로 전환**한다.
  이 문서는 비전 소프트웨어 수정 작업(별도 진행)이 따라야 할 와이어 계약의 단일 기준이다.
- 적용 채널: correlated 검사 규약을 쓰는 **BOTTOM / FRONTSIDE / REARSIDE / BIN / WAFER 전부**.
- 핸들러 구현 기준: `QMC.CDT-320\Equipment\Vision\VisionInspectionProtocol.cs`의 파서가
  받아들이는 형태를 그대로 확정했다 (ver=1).

## 0. 공통 와이어 규칙 (기존 유지)

- TCP 라인 프로토콜, UTF-8, 라인 종단 `\n`. 필드 구분 `|`, META(키=값 목록) 내부 구분 `;`.
- CAMERA 명: `WAFER` / `BOTTOM` / `FRONTSIDE` / `REARSIDE` / `BIN`.
- 숫자는 Invariant 표기(소수점 `.`).

## 1. INSPECT 요청 (핸들러 → 비전, **기존 그대로 — 변경 없음**)

```
CAMERA|INSPECT_SYNC 또는 INSPECT_ASYNC|FINDER|HEAD|HEAD_INDEX|DIE_INDEX|GRID_X|GRID_Y|CHANNEL|WAFER_ID|RECIPE_ID|LOT_ID|request_id=..;group_id=..;result_timing=..;operation=..
```

예시:

```
BOTTOM|INSPECT_ASYNC|DIE_BOTTOM|F|1|37|3;5|12|1|W2406-01|RCP-A|LOT-77|request_id=638612345678901234-000012-a1b2c3;group_id=638612345678901234-000012-a1b2c3;result_timing=DEFERRED;operation=INSPECT
```

- `request_id`: 요청(촬영) 1회 식별자. EPD 상관관계 키.
- `group_id`: 결과 단계(MRESULT/RESULT) 상관관계 키. 같은 검사 묶음이면 여러 요청이 공유할 수 있다.

## 2. EPD 푸시 (비전 → 핸들러, **기존 그대로 — 변경 없음**)

노출(촬상) 완료 즉시 자발 송신. 핸들러는 EPD 수신 후 즉시 다음 콜렛으로 모션을 진행한다.

```
EPD|CAMERA|HEAD|HEAD_INDEX|DIE_INDEX|CHANNEL|WAFER_ID|RECIPE_ID|LOT_ID|request_id=..;group_id=..;finder=..
```

- 핸들러는 CAMERA/HEAD/HEAD_INDEX/DIE_INDEX/CHANNEL/WAFER_ID/RECIPE_ID/LOT_ID/group_id/finder
  전 필드를 요청과 대조한다(불일치 시 무시).

## 3. MRESULT / RESULT 푸시 (비전 → 핸들러, **신규 확정 — 이 작업의 핵심**)

### 3-1. 전송 방식 변경
- **핸들러는 결과를 더 이상 요청하지 않는다.** 기존의
  `CAMERA|MRESULT|group_id=..` / `CAMERA|RESULT|group_id=..` 요청 라인은 **폐지**되었고,
  핸들러는 이 라인을 송신하지 않는다.
- **PENDING은 폐지**한다 — 핸들러는 PENDING을 보내지도 받지도 않는다.
  비전은 단계가 완료되기 전에는 아무것도 보내지 않고, **완료 즉시 결과 라인을 자발 푸시**한다.
- 송신 순서 보장 (한 검사 요청에 대해):
  `EPD` → (`MRESULT` — **BOTTOM INSPECT만**) → `RESULT`, 각 단계 완료 즉시 송신.
- 핸들러 수신부는 도착한 결과를 수신 스토어에 보관하므로, 소비 시퀀스가 늦게 도착해도
  (스토어 한도 500건, 연결 유지 시) 유실되지 않는다. 연결이 끊기면 미소비 결과는 무효 처리된다.

### 3-2. 라인 포맷 (기존 요청-응답의 응답 페이로드 포맷과 동일 — 헤더형 canonical)

```
MRESULT|CAMERA|FINDER|HEAD|HEAD_INDEX|DIE_INDEX|CHANNEL|WAFER_ID|RECIPE_ID|LOT_ID|STATUS|META
RESULT |CAMERA|FINDER|HEAD|HEAD_INDEX|DIE_INDEX|CHANNEL|WAFER_ID|RECIPE_ID|LOT_ID|STATUS|META
```

- 위치 필드(STATUS까지)는 **고정 순서**이며 핸들러가 요청 문맥과 전수 대조한다.
  STATUS는 CAMERA 뒤 9번째 위치 필드(파서 기준 index 8)가 정본이다.
- **META 필수 키**: `group_id`(필수 — 없으면 핸들러가 폐기+경고), `request_id`,
  `profile`, `ver=1`, 그리고 profile별 결과 값(아래 3-4).
- STATUS 값:
  - `MRESULT`: `OK` (오류는 ERR 라인 사용)
  - `RESULT`(INSPECT): `PASS` | `FAIL`
  - `RESULT`(MATCH): `OK`
- SIDE(FRONTSIDE/REARSIDE) 지연(DEFERRED) 집계 RESULT는 CHANNEL=0으로 송신한다 (기존 규약 유지).

### 3-3. 예시 라인

BOTTOM 중간 결과(MRESULT, BOTTOM INSPECT 전용):

```
MRESULT|BOTTOM|DIE_BOTTOM|F|1|37|1|W2406-01|RCP-A|LOT-77|OK|group_id=638612345678901234-000012-a1b2c3;request_id=638612345678901234-000012-a1b2c3;profile=BOTTOM_SURFACE;ver=1;bottom_offset_x_mm=0.012;bottom_offset_y_mm=-0.004;bottom_angle_deg=0.031
```

BOTTOM 최종 결과(RESULT):

```
RESULT|BOTTOM|DIE_BOTTOM|F|1|37|1|W2406-01|RCP-A|LOT-77|PASS|group_id=638612345678901234-000012-a1b2c3;request_id=638612345678901234-000012-a1b2c3;profile=BOTTOM_SURFACE;ver=1;measure_valid=1;bottom_width_mm=1.998;bottom_height_mm=1.002;algo_ms=42
```

SIDE 최종 결과(RESULT, 집계 CHANNEL=0):

```
RESULT|FRONTSIDE|DIE_SIDE|F|1|37|0|W2406-01|RCP-A|LOT-77|PASS|group_id=..;request_id=..;profile=SIDE_JUDGE;ver=1;measure_valid=1;ch0_valid=1;ch1_valid=1;ch0_side_item_foreign_count=0;ch0_side_item_foreign_count_pass=1;ch0_side_item_foreign_max=0.0;ch0_side_item_foreign_max_pass=1;ch1_side_item_foreign_count=0;ch1_side_item_foreign_count_pass=1;ch1_side_item_foreign_max=0.0;ch1_side_item_foreign_max_pass=1
```

BIN 최종 결과(RESULT):

```
RESULT|BIN|DIE_PLACE|F|1|37|1|W2406-01|RCP-A|LOT-77|PASS|group_id=..;request_id=..;profile=PLACEMENT_POSE;ver=1;measure_valid=1;x=320.5;y=240.1;width=64.0;height=64.0;placement_offset_x_mm=0.021;placement_offset_y_mm=-0.008;placement_angle_deg=0.05
```

측정 실패(FAIL, measure_valid=0):

```
RESULT|BIN|DIE_PLACE|F|1|37|1|W2406-01|RCP-A|LOT-77|FAIL|group_id=..;request_id=..;profile=PLACEMENT_POSE;ver=1;measure_valid=0;fail_code=NO_DIE;fail_message=die not found
```

오류(ERR — 기존 canonical 9필드 형식 그대로):

```
ERR|BOTTOM|INSPECT_ASYNC|E1001|inspection engine timeout|F|1|37|1|W2406-01|RCP-A|LOT-77|request_id=..;group_id=..
```

### 3-4. profile별 필수 결과 키 (핸들러 파서 기준, ver=1)

| profile | 대상 | STATUS | 필수 키 |
|---|---|---|---|
| `BOTTOM_SURFACE` (MRESULT) | BOTTOM INSPECT 중간 | OK | `bottom_offset_x_mm`, `bottom_offset_y_mm`, `bottom_angle_deg` (유한 숫자) |
| `BOTTOM_SURFACE` (RESULT) | BOTTOM INSPECT 최종 | PASS/FAIL | `measure_valid`(1/0), 유효 시 `bottom_width_mm`, `bottom_height_mm` (+선택 계측 `bottom_item_*`, `algo_ms`) |
| `SIDE_JUDGE` (RESULT) | FRONTSIDE/REARSIDE | PASS/FAIL | `measure_valid`, `ch0_valid`, `ch1_valid`, 유효 채널별 `chN_side_item_foreign_count(+_pass)`, `chN_side_item_foreign_max(+_pass)` |
| `PLACEMENT_POSE` (RESULT) | BIN | PASS/FAIL | `measure_valid`, `x`, `y`, `width`, `height`, `placement_offset_x_mm`, `placement_offset_y_mm`, `placement_angle_deg` |
| `MATCH_POSE` 등 MATCH 계열 (RESULT) | WAFER 등 MATCH | OK | `x`, `y` (+finder별 추가 키 — 기존 파서 기준) |
| 공통 | 전부 | FAIL + `measure_valid=0` | `fail_code`, `fail_message` |

- `measure_valid=0`이면 STATUS는 PASS일 수 없다.

## 4. 핸들러 수신 동작 (참고 — 비전 구현과 무관하게 핸들러가 보장)

- 수신 루프가 MRESULT/RESULT 라인을 (CAMERA, 종류, group_id) 키로 수신 스토어에 보관하고,
  대기 중 소비자가 있으면 즉시 전달한다.
- `group_id`가 없는 결과 라인은 폐기하고 경고를 기록한다 (legacy 응답 큐로 유입되지 않음).
- 동일 키 중복 수신은 최신 값으로 교체된다.
- 미소비 보관 한도 500건 — 초과 시 가장 오래된 항목부터 제거(경고 기록).
- 연결 단절 시 해당 채널의 미소비 결과는 전부 폐기된다 — 재연결 후 이전 검사 결과는 무효.

## 5. 폐지 목록 (비전 측에서 제거해야 할 것)

| 항목 | 기존 | 신규 |
|---|---|---|
| 결과 요청 라인 `CAMERA|MRESULT|group_id=..` / `CAMERA|RESULT|group_id=..` | 핸들러가 송신, 비전이 응답 | **폐지** — 핸들러는 송신하지 않음. 수신되어도 응답 불필요 |
| `PENDING` 상태 응답 | 미완료 시 PENDING 응답 → 핸들러 재요청 폴링 | **폐지** — 완료 전에는 침묵, 완료 즉시 푸시 |
| 결과 ACK (`ACK|CAMERA|MRESULT|...`) | 요청 수신 확인 | 불필요 (핸들러는 수신 시 진단 무시) |
