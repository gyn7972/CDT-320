# STALE_COMMENT_PLAN — 무효 주석 정리 (주석 전용, 코드·동작 변화 0)

브랜치 `cleanup-stale-comments` (master 분기). C3a(`algorithm_camera.json` 완전 은퇴) / C3b-3(결선=LightWirings 폐기) 이후 사실과 다른 주석을 분류.
판정: **(A) 무효** = 제거된 동작을 *현재인 양* 기술 → 제거/정정. **(B) 유효** = 현 상태/이력 정확 → 유지.

## 코드 확인 근거
- `ImportCameraMapping(AlgorithmCameraMapping m)` 본문(VisionModule.cs:111-125): 인자 버퍼 `m` 을 `Config/Recipe` 에 그대로 대입. **algorithm_camera.json 을 읽지 않음.** CameraMappingPanel 저장 경로(P4WireSmoke: Export↔Import 대칭). → "마이그레이션" 서술은 무효.
- `SavedSettings()` 본문(InspectionLightPanel.cs:98-103): `r?.LightSettings ?? new List<...>()`. **fallback 분기 없음.**
- C3b-3 에서 `Setup.LightWirings`(결선) 제거 → `Setup.LightPages`(지정) 로 대체(merged). → "LightWirings 결선" 서술은 무효.

## 분류표

| # | 파일:줄 | 현재 주석(요지) | 판정 | 조치 |
|---|---------|----------------|------|------|
| 1 | Modules/IVisionModule.cs:32 | `<summary>algorithm_camera.json 매핑 → 모듈 Config/Recipe(마이그레이션·UI 편집 반영)` | **A** | → `CameraMappingPanel 워킹버퍼(AlgorithmCameraMapping) → 모듈 Config/Recipe 반영(UI 편집 저장)` |
| 2 | Modules/VisionModule.cs:110 | `<summary>마이그레이션 — algorithm_camera.json 매핑 → 모듈 Config/Recipe(최초 부팅, 빈 경우)` | **A** | → `CameraMappingPanel 워킹버퍼 → 모듈 Config/Recipe 반영(UI 편집 저장)` |
| 3 | Ui/Pages/CameraMappingPanel.cs:26 | `<summary>… 없으면 null(테스트/디자인 시 구 store fallback)` | **A** | 무효 괄호 제거 → `… 미해결(테스트/디자인 등) 시 null` |
| 4 | Ui/Pages/InspectionLightPanel.cs:26 | `// null 이면 구 algorithm_camera.json fallback(호스트가 노드 미해결 시).` | **A** | → `// null 이면 호스트가 노드 미해결(조명 설정 비활성).` |
| 5 | Ui/Pages/InspectionLightPanel.cs:97 | `<summary>저장된 레벨 출처 — 노드 Recipe.LightSettings(있으면) 또는 구 algorithm_camera.json fallback` | **A** | → `<summary>저장된 레벨 출처 — 노드 Recipe.LightSettings(없으면 빈 목록)` |
| 6 | Ui/Pages/InspectionLightPanel.cs:25 | `// C2 — 조명 SSOT = … (Setup.LightWirings 결선 / Recipe.LightSettings 레벨).` | **A\*** | C3b-3 폐기 반영 → `// C2/C3b-3 — 조명 SSOT = 알고리즘 노드 BaseUnit(Setup.LightPages 지정 / Recipe.LightSettings 레벨).` |

\* #6 은 `algorithm_camera.json` 이 아니라 **C3b-3 결선(LightWirings) 폐기** 관련. 프롬프트 문구 범위 밖이나 #4 와 같은 주석 블록(25-26)이라 함께 정정 권고. 제외 원하면 #4 만 적용 가능.

## 유지(B) — 정확한 이력/현행 주석 (변경 안 함)
- Form1.cs:46,47,85,180 — "algorithm_camera.json 완전 은퇴 / 구 store·마이그 다리 제거 / 구파일 마이그 제거" = 제거 사실 정확.
- Form1.cs:51 — io_set 자동 마이그(별개 주제, 현행 동작).
- IVisionModule.cs:37 / VisionModule.cs:127-129 — MigrateLightPages(현행).
- CameraMappingPanel.cs:95,108,235 — "fallback 폐지 / 조용한 구경로 금지"(현 상태 정확).
- InspectionLightPanel.cs:100,118,140 — "fallback 폐지 / 마이그 전 표시용 폴백(ActivePages 라이브 코드) / 구경로 금지"(현행).
- Backends/*·Cameras/*·CameraFactory 등의 "Cognex/OpenCv/Sim fallback" = 런타임 실동작(무관).

## 적용 후 게이트
전체 빌드0(Common→Handler→Vision) / verify 코어 FAIL0 / `git diff` 주석 줄 한정(코드 토큰 0) / R2 2/2 보존.
