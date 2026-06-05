# STAGE 64 — RESULT: 검사별 카메라 파라미터 오버라이드

- **작업 완료일**: 2026-05-28
- **브랜치**: `stage-64-per-insp-camera-override`
- **연계**: `STAGE64_SPEC_*` / `STAGE64_CHECKLIST_*`

## 사용자 결정 반영
| # | 항목 | 결정 | 적용 |
|---|---|---|---|
| 1 | FrontSide/RearSide 라벨 | 앞쪽/뒤쪽 | 앞쪽 면·앞쪽 칩핑 / 뒤쪽 면·뒤쪽 칩핑 (InspectionLabel) |
| 2 | 클래스명 | `InspectionCameraOverride` | 그대로 |
| 3 | 토글 | 필드별 체크박스 | 7필드 각 "기본값" 체크박스 |
| 4 | override 표시 | (결정) ` ●` 접미사 | TreeView 노드 텍스트 |
| 5 | ROI | 4필드 묶음 | 단일 체크박스 X/Y/W/H 동시 |
| 6 | 런타임 | 분리 유지 | 본 Stage = 모델+UI+영속화, 런타임 = Stage 65 |

## 구현 산출물
- **신규**: `QMC.Vision\Ui\Pages\InspectionOverridePanel.cs` (7필드 체크박스 토글 + ROI 묶음 + 저장/복원/취소/테스트 그랩)
- **모델** (`QMC.Common\Recipes\AlgorithmCameraSubset.cs`):
  - `InspectionCameraOverride` (nullable 필드 + `EmitDefaultValue=false`, `IsEmpty/Clone/ApplyOver/HasRoiOverride`)
  - `AlgorithmCameraMapping.Inspections` + `GetOrCreateOverride` / `EffectiveFor`, `Clone()` 깊은 복사
  - `InspectionLabel` 사전 (26 매핑) + `InspectionsOf(algorithm)`
- **UI** (`SettingsPage.cs`): TreeView 2→3레벨 (알고리즘 아래 26 검사 노드), 키 `cam:<alg>:<insp>`, 라우팅, ● 표시, `_inspPanel` 영구 패널
- **영속화** (`AlgorithmCameraMap.cs`): `PruneEmptyOverrides` — Save 시 IsEmpty override 제거 + 중복 제거(첫 항목 우선), 전부 비면 `Inspections=null` (구버전 호환)

## 검증
- **dotnet build**: solution clean — **warning 0 / error 0**
- **verify_stage64.ps1**: **8/8 PASS**
- **verify_all.ps1**: **60/60 PASS** (회귀 없음)
- **라운드트립**:
  - override 없는 JSON → Load/Save → `Inspections` 키 미출현 (구버전 호환 ✓), Items 5 (EnsureDefaults ✓)
  - override 있는 JSON → EjectPinFinder Exposure=12000 유지 ✓, 빈 override(DieFinder) 자동 제거 ✓
- **런타임 스모크**: Vision 기동/응답 정상 (동작 변화 없음 — 런타임 적용은 Stage 65)

## 26 검사 ID (코드 확인 = 사전 조사 일치)
Wafer 7 / Bin 4 / BottomInspection 7 / FrontSide 4 / RearSide 4. 중복 ID 는 (Algorithm, InspectionId) 로 유일.

## 문서
- `MISMATCH_RESOLUTION_LOG.md`: M-64-1(편집기 레이어 구분) / M-64-2(중복 ID) 기록 완료
- `ARCHITECTURE_EXPORT.md`: 모델·UI 변경 → 재생성 권장 (후속)

## 비범위 / 다음 Stage
- **Stage 65 (런타임 적용)**: `VisionModule.Grab(inspectionId)` 또는 TCP 핸들러가 `EffectiveFor(inspectionId)` → `TryApplyParameters` → Grab. 신규 알람 `VISION-OVERRIDE-FAIL` 후보.
- 동일 CameraId 모듈 간 카메라 공유/lock — 별도 Stage.
- 알고리즘 파라미터(Threshold) 검사별 분리 — 별 작업 (편집기 5종).
