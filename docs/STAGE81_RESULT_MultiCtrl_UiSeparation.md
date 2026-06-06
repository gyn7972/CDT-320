# STAGE 81 — RESULT: 다중 컨트롤러 결선 + Setup/Recipe UI 책임 분리 구현

- **작성일**: 2026-06-02
- **연계**: `STAGE80_SPEC/CHECKLIST_MultiCtrl_UiSeparation.md`
- **브랜치**: `stage-81-multi-ctrl-ui-impl` → master ff
- **사용자 결정**: 바로 구현 (SPEC 기본값)

---

## 1. 구현 요약
- **Part A 다중 컨트롤러**: `AlgorithmLightWiring.ControllerSets`(List) + `ControllerChannels` + `InspectionLightSetting.ControllerPort`. 한 알고리즘이 여러 컨트롤러를 동시 결선.
- **Part B UI 분리**: Setup 결선을 **TreeView + ControllerSets 디테일**로, 헤더에 책임 안내. Recipe 패널에 **Controller 컬럼 + 다중 결선 헤더**.
- **Apply 병렬**: 컨트롤러별 group → `Task.WhenAll`(독립 시리얼 포트 동시 발사).

## 2. 변경 파일
| 파일 | 변경 |
|---|---|
| `LightSystemSetup.cs` | AlgorithmLightWiring.ControllerSets + ControllerChannels + OnDeserialized(단일→Sets) + RenamePort/Validate 갱신 + GetSet/Ports/IsWired 헬퍼 |
| `InspectionLightSubset.cs` | InspectionLightSetting.ControllerPort + Clone |
| `LightSystemMigrator.cs` | io_set → ControllerSets (get-or-create) |
| `AlgorithmCameraSubset.cs` | FillRecipeControllerPorts (Recipe ControllerPort 자동채움, 채널 소유 ControllerSet 우선) |
| `Form1.cs` | FillRecipeControllerPorts 1회 보정 호출 |
| `LightSystemSetupPage.cs` | 결선 평면grid → TreeView + _gridSets 디테일 + _wiringModel + cascade + 헤더 안내 |
| `InspectionLightPanel.cs` | Controller 컬럼 + 다중 결선 헤더 + (port,ch) 키 + Apply Task.WhenAll 병렬 |
| `verify_all.ps1` | STAGE81 5행 |

## 3. 검증 결과
- **빌드**: 경고 0 / 오류 0
- **verify_all**: **87/87 PASS** (82 + STAGE81 5)
- **마이그레이션 라운드트립(런타임 DataContract)** — 정지조건 통과:
  - [1] 구버전 단일 `{ControllerPort:"COM1",Channels:[3,4,5]}` → `ControllerSets[0]=COM1/[3,4,5]` ✓
  - [2] 재직렬화→재역직렬화 idempotent (이중 변환 없음, count=1) ✓
  - [3] 다중 ControllerSets 2항목(COM1,COM2) 보존 ✓
  - [4] `InspectionLightSetting.ControllerPort` 라운드트립(COM2) ✓
- **앱 스모크**: Sim `Responding=True`
- **UI 렌더**: Setup 헤더(책임 안내) + 결선 TreeView(5 알고리즘 노드) + ControllerSets 디테일 정상

## 4. 설계 결정 (SPEC 기본값)
- 확인 #1 다중결선 Recipe ControllerPort = 채널 소유 ControllerSet 우선, 없으면 ControllerSets[0].
- 확인 #2 (Controller,Channel) 중복 = **경고만**(SetStatus 빨강, 차단 X).
- 확인 #3 cascade 삭제 = ControllerSets 제거 + 다이얼로그(Recipe 값은 다음 로드 시 풀 밖 표시).
- 확인 #4 Recipe Controller 컬럼 = 행 자동생성(컨트롤러별 채널) + **읽기전용**(Stage 72 auto-list 철학 일관 — Level/Page 만 편집).
- 확인 #5 프리셋 = Vision 단일 파일이라 별도 표시 생략(헤더 안내로 대체).
- Setup 결선 TreeView 디테일은 채널 체크박스 대신 **ChannelsCsv 그리드**(단순/안전, 다중 컨트롤러 기능 동일).

## 5. 알람
신규 0. LIGHT-WIRING-MISS / LIGHT-MAP-INVALID / LIGHT-CHANNEL-OUT-OF-POOL 재사용.

## 6. 남은 항목
- 실 장비에서 두 컨트롤러 동시(Task.WhenAll) 발사 시점 측정(Sim 카운터/timestamp).
- Recipe 풀 밖 채널 빨강 표시(현재 status 보고 — 행 색상 강조는 후속).
- 명명된 RecipeProject(다중 프리셋) 도입은 별도 Stage(M-80-4).
- 권한 분리(Setup=관리자/Recipe=운영자)는 별도 Stage.
