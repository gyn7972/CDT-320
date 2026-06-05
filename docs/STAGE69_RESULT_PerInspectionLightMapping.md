# STAGE 69 — RESULT: 검사별 조명 매핑 구현

- **작업 완료일**: 2026-05-29
- **브랜치**: `stage-69-per-insp-light-mapping-impl` (base: master)
- **연계**: `STAGE68_SPEC_*` / `STAGE68_CHECKLIST_*`

## 사용자 확정 결정 반영 (Stage 68 §12)
| # | 결정 | 적용 |
|---|---|---|
| 1 | 채널 풀 사용자 UI 설정 (예시 하드코딩 금지) | AlgorithmLightWiring 기본 빈 풀; LightSystemSetupPage "알고리즘 결선 표"에서 배정 |
| 2 | 옵션 A (통합) | `AlgorithmCameraMapping.InspectionLights` (algorithm_camera.json) |
| 3 | PageCount 기본 1 + no-op | `ILightController.SwitchPageAsync` 추가, LFine/Sim 모두 no-op(true) |
| 4 | UI 디바운스 50ms + StabilizeDelayMs | `InspectionLightSetting.StabilizeDelayMs`(기본 0) + 패널 안정화 컬럼 |
| 5 | 휴리스틱 마이그레이션 | `LightSystemMigrator.GuessAlgorithm` — 모호 항목 미할당 |
| 6 | 탭 분리 | SettingsPage 검사 노드 = TabControl [카메라][조명] |
| 7 | `.bak.YYYYMMDD` | `LightSystemMigrator.BackupLegacy` |
| 8 | 포트 일괄 변경 | `LightSystemSetup.RenamePort` + Setup 페이지 버튼 |

## 신규 파일
| 파일 | 역할 |
|---|---|
| `QMC.Common\Recipes\LightSystemSetup.cs` | LightSystemSetup/LightControllerEntry/LightChannelLabel/AlgorithmLightWiring + Store + RenamePort/Validate/EnsureWirings |
| `QMC.Common\Recipes\InspectionLightSubset.cs` | InspectionLightSetting(+StabilizeDelayMs) / InspectionLightOverride |
| `QMC.Common\Recipes\LightSystemMigrator.cs` | io_set.lightSource.json → LightSystemSetup, 채널 휴리스틱, .bak 백업 |
| `QMC.Vision\Comm\LightHub.cs` | Dictionary<PortName, ILightController> (VisionHub 패턴) |
| `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` | 컨트롤러 인벤토리 + 채널 라벨 + 알고리즘 결선 표 + 포트 변경/마이그레이션 버튼 |
| `QMC.Vision\Ui\Pages\InspectionLightPanel.cs` | 검사별 조명 그리드 (풀 내 채널 콤보 + Apply/Save/Reset/Cancel/All On·Off) |

## 수정 파일
- `QMC.Common\Recipes\AlgorithmCameraSubset.cs` — `InspectionLights` 필드 + GetOrCreate/GetLightOverride + Clone 깊은 복사
- `QMC.Vision\Optics\ILightController.cs` / `Sim\SimLightController.cs` / `LFine\LFineLightController.cs` — `SwitchPageAsync`
- `QMC.Vision\Ui\Pages\SettingsPage.cs` — sys:light 노드 + 검사 노드 [카메라][조명] 탭 + SwapEditor Control 화
- `QMC.Vision\Form1.cs` — LightHub 초기화 + 첫 기동 자동 마이그레이션 + OnFormClosing DisposeAll
- `QMC.Common\Alarms\AlarmMaster.cs` — 신규 3 (LIGHT-WIRING-MISS / LIGHT-MAP-INVALID / LIGHT-CHANNEL-OUT-OF-POOL); 등록 총 57→60
- `QMC.Common.csproj` / `QMC.Vision.csproj` — 신규 파일 등록
- `tools\verify_all.ps1` (알람 60 + Stage 69 3항목) + `tools\verify_stage69.ps1` (신규 11)

## 데이터 모델
- **Setup(기구적)**: `light_system.json` — 컨트롤러 인벤토리(PortName FK) + 알고리즘 결선(Controller/Channels[]/Page)
- **Recipe(제품)**: `algorithm_camera.json` 통합 — `InspectionLightOverride.Settings`(Channel/Level/On/StrobeTimeUs/StabilizeDelayMs)
- Controller/Page 는 Recipe 에 중복 저장 안 함(결선에서 추론). Setting.Channel 은 풀 검증.

## UI 흐름
- 설정 트리: "■ 카메라 매핑"(검사 3레벨) / "■ 검사 알고리즘" / **"■ 시스템 설정 / 조명 시스템"**(신규)
- 검사 노드 클릭 → 우측 **[카메라][조명] 탭** (각 full-width)
- 조명 탭: 결선 헤더(읽기전용) + 풀 채널 콤보 그리드. Apply=LightHub 라이브, Save=영속화.

## 검증
- **dotnet build (솔루션)**: clean — **warning 0 / error 0**
- **verify_stage69.ps1**: **11/11 PASS**
- **verify_all.ps1**: **68/68 PASS** (회귀 없음; AlarmMaster 60 카운트)
- **자동 마이그레이션 라운드트립** (io_set 8채널/3포트 → light_system.json):
  - 컨트롤러 **3개**(COM1/2/3) 생성 + 채널 라벨 채움 ✓
  - 휴리스틱 결선: Bin→COM3, BottomInspection→COM2, FrontSide→COM3, RearSide→COM3, Wafer→빈값 ✓
  - 모호(RING/ALIGN/SIDE VISION 1·2) 미할당 ✓, `.bak.YYYYMMDD` 백업 생성 ✓
- **Sim 스모크**: Vision 기동/응답 정상 (LightHub Sim 컨트롤러 등록, 기존 동작 불변)

## 비범위 / 다음 Stage
- **런타임 자동 적용** (Grab 직전 조명 적용 → StabilizeDelayMs 대기 → 그랩) — 다음 Stage. 컨트롤러별 SemaphoreSlim 직렬화.
- **3→2 컨트롤러 통합** — io_set 가 3포트라 자동 변환은 3개; 사용자가 Setup 결선 표/포트 변경/삭제로 실 하드웨어 2개에 맞춤 (M-69-1).
- **LFine 페이지 명령** — 실 페이지 모델 컨트롤러 도입 시 SwitchPageAsync 명령 포맷 추가 (M-69-2).
- **InspectionLightPanel 라이브 시연** — computer-use 미연결로 마우스 검증 생략; 빌드/verify/마이그레이션 라운드트립으로 확인. 실기 시연은 보조 모니터에서 후속.
