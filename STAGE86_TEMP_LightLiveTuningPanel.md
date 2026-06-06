# Stage 86 (임시) — InspectionLightPanel 라이브 튜닝 패널

## 목적
카메라 라이브 영상 보면서 조명 세팅 맞추기 위한 **임시 사이드 패널**. 펄스 스트로브 컨트롤러 대응 — 주기적 명령 송신으로 라이브 화면 동안 조명 유지. 파일 2개 + InspectionLightPanel 블록만 제거하면 깔끔히 빠지도록 설계.

## 신규 / 수정
- 신규: `QMC.Vision\Ui\Controls\LightLiveTuningPanel.Designer.cs` (컨트롤 필드 + InitializeComponent + Dispose, named handler 참조만)
- 신규: `QMC.Vision\Ui\Controls\LightLiveTuningPanel.cs` (생성자 + 이벤트 핸들러 본문 + 비즈니스 로직)
- 수정: `QMC.Vision\Ui\Pages\InspectionLightPanel.cs` — 3 블록 (필드 / BuildLayout 3줄 / CollectRowsForLiveTuning 콜백). using 은 FQN 사용으로 불필요.
- 수정: `QMC.Vision\QMC.Vision.csproj` — Compile 2 엔트리(.cs + .Designer.cs DependentUpon).

## UI 규약 준수 (Designer/Code 분리)
- Designer.cs: partial, 컨트롤 필드, InitializeComponent, Dispose, `+= OnXxxClick` named 참조만. **람다 이벤트 0건.**
- Code.cs: partial, `LicenseManager` Designtime 가드 + 생성자, 핸들러 본문, 비즈니스 로직, 상태 필드(_timer 등).

## 검증 (computer-use 앱이름 미해석 → DrawToBitmap 렌더 + 헤드리스 기능 테스트로 실검증)

| 항목 | 방법 | 결과 |
|------|------|------|
| 빌드 | MSBuild QMC.Vision | ✅ 오류 0 / 신규 경고 0 |
| 패널 단독 렌더 | `DrawToBitmap` → `panel_standalone.png` | ✅ 헤더/주기/토글/ON·OFF/카운터/상태 정상 |
| 통합 배치 | InspectionLightPanel 호스트 → `inspectionpanel_integrated.png` | ✅ 우측 220px 전체높이 도킹, 좌측 그리드 정상 (도킹 충돌 없음) |
| 통합 리플렉션 | `_liveTuning` 필드/Dock=Right/Width=220/Controls 트리 | ✅ 전부 PASS |
| 라이브 토글 시작 | `OnToggleLiveClick` | ✅ _liveOn=true, timer 가동, 버튼 '■ 라이브 중지' |
| 단발 송신 카운터 | `SendOnceAsync(false)` + Sim COM1 | ✅ 송신 0→1 |
| 전체 OFF | `SendOnceAsync(true)` | ✅ 1→2, 예외 없음 |
| 라이브 중지 토글 | `OnToggleLiveClick` | ✅ _liveOn=false, timer 정지 |
| 제거 가능성 | 2파일+블록+csproj 제거 후 빌드 | ✅ clean (아래) |

> 하니스: `cdt-320\light-livetuning-test\` (LiveTuningRenderTest = 렌더+리플렉션, LiveTuningFuncTest = 라이브 로직). 송신 경로는 기존 Apply 와 동일 `LightHub.Get(port).SetChannelBatchAsync(...)`.
>
> ⚠️ **실 LED 점등 + 펄스 유지(카메라 라이브 화면)** 는 실장비 HW + `vision.json LightUseSim=false` 필요 — Sim 으로 송신 경로/카운터까지만 검증. 펄스 컨트롤러에서 화면 깜빡이면 주기를 20ms 로 낮춰 재시도(주기 NumericUpDown 10~1000ms).

## 제거 방법 (임시 기능 뺄 때)
1. `QMC.Vision\Ui\Controls\LightLiveTuningPanel.Designer.cs` 삭제
2. `QMC.Vision\Ui\Controls\LightLiveTuningPanel.cs` 삭제
3. `InspectionLightPanel.cs` 의 `[임시 라이브 튜닝]` 주석 표시 3 블록 삭제 (필드 / BuildLayout 3줄 / CollectRowsForLiveTuning)
4. `QMC.Vision.csproj` 의 LightLiveTuningPanel Compile 2 엔트리 삭제
→ 다른 파일 의존성 0 (제거 후 빌드 clean 확인됨).

## 커밋
로컬 커밋 + master 로컬 머지 + 브랜치 삭제. **remote push 안 함 — 사용자 컨펌 대기.**
