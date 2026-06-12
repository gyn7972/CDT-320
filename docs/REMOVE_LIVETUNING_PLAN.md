# REMOVE_LIVETUNING_PLAN — LightLiveTuningPanel 제거 (중복)

브랜치 `remove-light-livetuning`. 채널 레벨 = InspectionLightPanel(RecipePage, Apply 점등) / 실물 확인 = Settings(CameraMappingPanel) 라이브·그랩.

## Step 0 감사 결과

### 1. 기능 커버 (사용자 확인)
LightLiveTuningPanel 고유 기능 [LightLiveTuningPanel.cs:12-15, 26-45]:
- (a) **주기 반복 송신**(50ms~ 타이머로 점등 명령 재송신 — "펄스 스트로브 컨트롤러용, 화면 동안 조명 유지")
- (b) 카메라 라이브 start/stop 연동(BindCameraLive)
- (c) 송신 주기/Hz 표시·송신 카운트

대체: (a) → **SM 하드웨어 모드 0 확정으로 불필요** — 컨트롤러가 카메라 트리거마다 페이지 데이터로 발사하므로 반복 송신 자체가 무의미(1회 SP 설정 = InspectionLightPanel Apply). (b)(c) → Settings(CameraMappingPanel) Live Start/Stop + 미리보기. **빠지는 고유 기능 = "연속 점등 유지(반복 송신)" 뿐이며 모드0 운용 전제에서 불필요** — 프롬프트 전제와 일치.

### 2. 제거 범위 (확정)
- **프로덕션 4페이지**: VisionTargetPage(:68-79 주입, :119-127 CollectRows) / InspectorTargetPage(:69-78, :120-126) / FinderPage(:52-56, :124-135) / InspectorPage(:50-54, :119-130).
  - **FinderPage·InspectorPage = 레거시(도달 불가)** — `new FinderPage`/`new InspectorPage` 생성처 0(전수 grep). 참조만 제거(파일 존치).
- 파일 3: `Ui/Controls/LightLiveTuningPanel.cs`/`.Designer.cs`/`.resx` + csproj 3엔트리(:178/:181/:272).
- 테스트: 전용 2디렉토리(`light-livetuning-test`/`light-livetuning-cammove-test`) 제거 + R2eLightSmoke 의 LiveTuning 렌더 단언(:29-35) 제거(나머지 유지).
- `LightHub.SetChannelBatchAsync` 는 InspectionLightPanel 점등이 사용 — **유지**.

### 3. 우측 레이아웃 (사용자 확인)
타깃 페이지 `_right` TableLayout(Vision/Inspector 동일 패턴): [24 라벨 / **100% 파라미터** / 24 라벨 / **240 조명(_lightHost)** / 24 라벨 / **200 라이브튜닝(_liveHost)**].
라이브튜닝 행 2개(24+200) 제거 후 남는 224px 처리:
- **(a) 조명 확대(권고)**: `_lightHost` 240 → **440(절대)** — 채널 8행 그리드가 240px 에선 빠듯(행~25px×8+지정헤더). 제거분을 조명이 흡수.
- (b) 파라미터 흡수: 조명 240 유지, 파라미터(100%)가 224px 흡수 — 가장 단순하나 조명 그리드 여전히 빠듯.

## Steps
1. 4페이지 주입·CollectRowsForLiveTuning·TuningRow 제거 + 타깃 2페이지 Designer `_right` 행 정리(라벨+_liveHost 행 삭제, 조명 확정안 반영). 빌드0.
2. 파일3 + csproj 3엔트리 삭제. 빌드0.
3. 전용 테스트 2디렉토리 삭제 + R2eLightSmoke 단언 제거. 빌드0.
4. 게이트: 전체 빌드0/정적0/verify0/run_smoke 전체 PASS/타깃 렌더 재확인/push 안 함.
