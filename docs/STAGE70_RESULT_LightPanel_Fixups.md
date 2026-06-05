# STAGE 70 — RESULT: Light Panel 5건 fixup

- **작업 완료일**: 2026-05-29
- **브랜치**: `stage-70-light-ui-fixups` (base: master / Stage 69)
- **연계 프롬프트**: PROMPT_LightPanel_Fixups.md

## 5건 수정 결과
| # | 항목 | 구현 |
|---|---|---|
| A | 컨트롤러 삭제 | "컨트롤러 삭제" 버튼 + Delete 키 → 참조 결선 확인 다이얼로그 후 비움 + 행 제거 |
| B | 채널 라벨 ↔ ChannelCount 양방향 동기 | 라벨 행 증감→ChannelCount, ChannelCount 편집→라벨 행 증감, Channel 1..N 강제(readonly), 축소 시 결선 풀 잘림 보고 |
| C | 결선 Controller 콤보박스 | DataGridViewComboBoxColumn + 인벤토리 PortName 동기(RefreshControllerCombo) |
| D | Page 이동 (Setup→Recipe) | Wiring.Page 제거(LegacyPage 마이그레이션) / Setting.Page 추가 / Recipe UI Page 콤보 |
| E | FinderPage·InspectorPage 더미 교체 | IlluminatorPanel → InspectionLightPanel(algorithmKey, inspectionId) |

## 변경 통계
- 신규 파일: 0
- 수정 파일: **11**
  - `QMC.Common\Recipes\InspectionLightSubset.cs` — Setting.Page 추가
  - `QMC.Common\Recipes\LightSystemSetup.cs` — Wiring.Page → LegacyPage(읽기전용) + RenamePort(기존)
  - `QMC.Common\Recipes\AlgorithmCameraSubset.cs` — MigrateWiringPageToSettings
  - `QMC.Vision\Modules\VisionModule.cs` + 5 모듈 — AlgorithmKey 프로퍼티(virtual + 5 override)
  - `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` — A/B/C/D-2 (삭제·동기·콤보·Page컬럼 제거)
  - `QMC.Vision\Ui\Pages\InspectionLightPanel.cs` — D-3(Page 컬럼/헤더) + E 생성자 오버로드
  - `QMC.Vision\Ui\Pages\FinderPage.cs` / `InspectorPage.cs` — E 패널 교체
  - `QMC.Vision\Form1.cs` — Page 마이그레이션 1회 호출
- 신규 알람: 없음
- 줄: 약 +210 / -40

## 데이터 모델 변경 (D)
- `AlgorithmLightWiring.Page` 제거 → `[DataMember(Name="Page", EmitDefaultValue=false)] LegacyPage`(로드 전용)
- `InspectionLightSetting.Page` 추가 (0 ~ controller.PageCount-1)
- `AlgorithmCameraSubset.MigrateWiringPageToSettings(setup)`: Wiring.LegacyPage(!=0) → 해당 알고리즘 검사들의 Setting.Page(0인 것만) 전파, 소비 후 LegacyPage=0
- Form1.Load: 양쪽 Store 로드 후 1회 마이그레이션 + 양쪽 Save

## E — 모듈→알고리즘 매핑
- `VisionModule.AlgorithmKey` (virtual, 기본 "") + 5 모듈 override (Wafer/Bin/BottomInspection/FrontSide/RearSide)
- FinderPage: `new InspectionLightPanel(_module.AlgorithmKey, _finder.Id)`
- InspectorPage: `new InspectionLightPanel(_module.AlgorithmKey, _inspector.Id)`
- InspectionLightPanel 에 `(algorithm, inspectionId)` 생성자 오버로드 추가 (기존 무인자 생성자 + SelectInspection 유지)
- 결선 미설정 시 "결선 없음 — [설정 > 조명 시스템]..." 안내 라벨 (기존 내장)
- IlluminatorPanel 클래스는 **삭제 안 함** (다른 참조 가능성)

## 검증
- **dotnet build (솔루션)**: clean — warning 0 / error 0
- **verify_all.ps1**: **72/72 PASS** (Stage 70 항목 4개 추가, 회귀 없음)
- **Page 마이그레이션 라운드트립**: 구버전 light_system.json(Wiring.Page=2) + algorithm_camera.json(검사 Setting Page 0) → Form1.Load 후 **Setting.Page=2 채워짐 ✓, light_system Wiring.Page 키 소멸 ✓**
- **FinderPage IlluminatorPanel 미사용 grep**: PASS (InspectionLightPanel 사용 확인)
- **Sim 스모크**: Vision 기동/응답 정상

## 실 UI 검증 (보조 모니터) — 미수행 사유
- 본 세션은 computer-use MCP 연결이 끊긴 상태 → 보조 모니터 실행/마우스 클릭 시연 불가.
- 정지 조건("보조 모니터 미감지 시 정지")에 따라 **빌드/verify/마이그레이션 라운드트립/Sim 스모크로 대체 검증**하고, 실 UI 클릭 시연(컨트롤러 추가·삭제 다이얼로그, ChannelCount 동기 화면, Controller 콤보, Recipe Page 컬럼, FinderPage 패널 교체)은 computer-use 복구 후 보조 모니터에서 후속 캡처 권장.

## 잔존 / 다음 제안
- 보조 모니터 실 UI 스크린샷 캡처 (computer-use 복구 시)
- 런타임 자동 적용 Stage (Grab 직전 EffectiveLight → SwitchPage → SetPower → StabilizeDelayMs 대기 → Grab)
- io_set 3포트 → 2 컨트롤러 통합 (사용자 Setup 작업)
