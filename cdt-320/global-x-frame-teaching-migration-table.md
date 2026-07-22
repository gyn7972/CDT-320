# 전역 X 좌표계 전환 — 티칭/설정 포지션 마이그레이션 테이블 (설비 재셋업용)

적용 프롬프트: `cdt-320/codex-prompts/global-x-frame-unification.md`

## 시프트량(Δ) 정의

| 기호 | 축 | 값 |
|---|---|---|
| Δiv | InputVisionX | **0** (전역 원점 기준축 — 좌표 무변경) |
| Δfp | FrontPickerX | FrontPickerX 홈의 전역 좌표 = `GlobalHomePosition` (바텀비전 코릴레이션 산출) |
| Δrp | RearPickerX | RearPickerX 홈의 전역 좌표 (코릴레이션 산출) |
| Δov | OutputVisionX | OutputVisionX 홈의 전역 좌표 (코릴레이션 산출) |

규칙: **X 성분 티칭값만** 해당 축의 Δ를 가산. Y/Z/T 성분은 전부 무변경.

---

## A. 신설 항목 — 이전에 없던 값 (새로 셋업)

| # | 항목 | 저장처 | 이전 | ⇒ 현재 | 셋업 방법 |
|---|---|---|---|---|---|
| A1 | `GlobalHomePosition` ×4축 | `motion_axes.json` (AxisSetup) | 없음 | 각 축 홈의 전역 좌표 (InputVisionX=0 고정) | 비전 카메라 코릴레이션 실행 → 자동 산출 |
| A2 | `UseGlobalHomePosition` ×4축 | `motion_axes.json` (AxisSetup) | 없음(=false) | true | 마이그레이션 도구 성공 완료 시 자동 설정 |
| A3 | InputVisionX 피커 진입 한계 | 신설 설정 (InputStage 레시피 또는 SharedRailX 설정) | 코드 하드코딩 `0.0` / `-0.1` | 전역 좌표의 진입 한계값 | 수동 셋업 (비전이 이 값보다 −쪽이면 피커 진입 허용) |
| A4 | OutputVisionX 피커 진입 한계 | 신설 설정 (동일) | 코드 하드코딩 `0` 가정 | 전역 좌표의 진입 한계값 (+방향) | 수동 셋업 |
| A5 | SharedRailX 순수 기구 간극 (4페어) | `shared_rail_x.json` | `HomeClearance`: Input↔Picker **19.0** / Output↔Picker **390.0** (원점차 포함값) | 원점차가 제거된 **순수 기구 최소 간극** | 기구 도면/실측으로 재산정 (예전 19/390에서 원점차 성분 제거) |

---

## B. 자동 시프트(+Δ) — 마이그레이션 도구가 변환, 설비에서 재확인 필요

### B-1. InputVisionX (Δiv = 0 → **값 무변경, 확인만**)

| 항목 | 저장처 | 이전 ⇒ 현재 |
|---|---|---|
| InputStage 레시피 `VisionX` (Avoid/Process/Reticle) | InputStage 레시피 | 무변경 (확인만) |
| `NeedleCalibrationData.VisionXPosition` | CalibrationData | 무변경 |
| `InputReticle.VisionXPosition`, `BottomReticle.VisionXPosition` ⚠ | VisionCameraCalibrationData | 무변경 (⚠ 각 측정값이 어느 축 좌표인지 도구가 판별 — InputVisionX 좌표면 무변경) |
| InputVisionX `SoftLimitMinus/Plus` | `motion_axes.json` | 무변경 |

### B-2. FrontPickerX (전부 `이전값 + Δfp`)

| 항목 | 저장처 | 이전 ⇒ 현재 |
|---|---|---|
| 피커 X 티칭 `AvoidPosition` | Picker 티칭 | 이전X ⇒ 이전X + Δfp |
| 피커 X 티칭 `InputAvoidPosition` | 〃 | 이전X ⇒ 이전X + Δfp |
| 피커 X 티칭 `OutputAvoidPosition` | 〃 | 이전X ⇒ 이전X + Δfp |
| 피커 X 티칭 `PickPosition` | 〃 | 이전X ⇒ 이전X + Δfp |
| 존 티칭 `DiePickPosition` X배열 (피커 1~4) | 〃 | 이전X ⇒ 이전X + Δfp |
| 존 티칭 `DieBottomPosition` X배열 | 〃 | 이전X ⇒ 이전X + Δfp |
| 존 티칭 `DieSidePosition` X배열 | 〃 | 이전X ⇒ 이전X + Δfp |
| 존 티칭 `DiePlacePosition` X배열 | 〃 | 이전X ⇒ 이전X + Δfp |
| `ZoneX` 인코더 존 경계: `Avoid/Input/Bottom/Side/Output` 각 `MinX/MaxX` (10개 값) | PickerFrontUnit Setup (`PickerZoneXSetup`) | 이전X ⇒ 이전X + Δfp |
| FrontPickerX `SoftLimitMinus/Plus` | `motion_axes.json` | 이전X ⇒ 이전X + Δfp |
| `ColletCalibrationRecord.FinalPickerX` (FrontCollets 1~4) | ColletCalibrationData | 이전X ⇒ 이전X + Δfp (단, C-4 재캘 시 자동 갱신) |

### B-3. RearPickerX (전부 `이전값 + Δrp`)

B-2와 동일 목록을 Rear 쪽(RearPickerUnit Setup / RearCollets)에 적용. `이전X ⇒ 이전X + Δrp`.

### B-4. OutputVisionX (전부 `이전값 + Δov`)

| 항목 | 저장처 | 이전 ⇒ 현재 |
|---|---|---|
| OutputStage 레시피 `OutputCameraX`(VisionX) Avoid/Process 등 | OutputStage 레시피 | 이전X ⇒ 이전X + Δov |
| `OutputReticle.VisionXPosition` | VisionCameraCalibrationData | 이전X ⇒ 이전X + Δov |
| OutputVisionX `SoftLimitMinus/Plus` | `motion_axes.json` | 이전X ⇒ 이전X + Δov |

---

## C. 재산출 항목 — 단순 +Δ 불가, 캘리브레이션 재실행으로 갱신

| # | 항목 | 이전 ⇒ 현재 | 갱신 방법 |
|---|---|---|---|
| C1 | `InputVisionToPicker.OffsetX[4]/OffsetY[4]` (Front/Rear 각각) | 축간 변환 상수 (기본 예: X0=250.0, Y0=30.0) ⇒ **0 근방의 콜렛 미세 오프셋** | PickerVisionOffset 캘리브레이션 재실행 (자동 산출) |
| C2 | `OutputVisionToPicker.OffsetX[4]/OffsetY[4]` (Front/Rear) | 동일 ⇒ 0 근방 미세 오프셋 | 〃 |
| C3 | 비전 카메라 코릴레이션 (`InputToBottomOffsetX/Y`, `OutputToBottomOffsetX/Y`) | 축별 좌표계 기준 ⇒ 전역 좌표 기준 | **가장 먼저 재실행** — A1의 GlobalHomePosition 산출 입력 |
| C4 | 콜렛 캘리브레이션 (`FinalPickerX/Y`, `OffsetX/Y`) | 구좌표 기준 ⇒ 전역 좌표 기준 | 전환 후 재실행 권장 (B의 +Δ 변환값은 임시) |

---

## D. 무변경 항목 — 재셋업 불필요 (안심 목록)

| 항목 | 사유 |
|---|---|
| 모든 Y/Z/T축 티칭 (PickerY/Z/T, StageY, NeedleZ, EjectPinZ, ExpanderZ, FeederY, LifterZ 등) | X 레일 통일 대상 아님 |
| `NeedleXToVisionXOffset` / `NeedleYToVisionYOffset` | NeedleBlockX는 X 레일 4축이 아님 (InputStage 별도 축) |
| `HomeOffset`, `HomeDirection`, 홈 서치 방식/속도 | 홈 서치 자체는 불변 — 완료 후 좌표 선언만 추가 |
| `PickerPitchX/PickerPitchY` | 피커 간 상대 피치 (상대값) |
| `PickerYFacingXClearance` (Front 300 / Rear 150) | 두 축 좌표 차 기준 상대 거리 |
| Input/Output 비전 후퇴 방향 규약 (−/+) | 방향 불변 — 원점만 이동 |
| Feeder/Cassette 전체 티칭 | X 레일 무관 |

---

## 권장 셋업 순서

1. 코드 배포 후 `UseGlobalHomePosition=false` 상태로 기존 동작 회귀 확인
2. **비전 카메라 코릴레이션 실행** (C3) → `GlobalHomePosition` 4축 자동 산출 (A1)
3. **마이그레이션 도구 실행** → B 전체 자동 시프트 + 백업 + 변환 리포트 확인
4. 도구가 `UseGlobalHomePosition=true` 설정 (A2) → **전 X축 재호밍** → 4축 좌표가 전역값인지 확인
5. 신설값 수동 셋업: 진입 한계 A3/A4, 기구 간극 A5
6. 캘리브레이션 재실행: 콜렛 캘 (C4) → PickerVisionOffset (C1/C2)
7. 티칭 실물 검증: 각 존 위치(Pick/Bottom/Side/Place)로 저속 이동시켜 B 변환값 확인
8. 인터락 경계 테스트: 진입 한계 ±오차, Front/Rear facing 간극, 존 판정
