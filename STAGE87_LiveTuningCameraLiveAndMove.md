# Stage 87 — 라이브 튜닝 패널 카메라 라이브 통합 + Finder/Inspector 우측 빈 공간 이동

## 목적
Stage 86 `LightLiveTuningPanel` 을 ① 카메라 라이브 grab 토글과 통합하고 ② InspectionLightPanel 의 우측 Dock 호스팅을 제거해 ③ FinderPage/InspectorPage 우측 하단 빈 공간(720,560 / 540×264)으로 이동.

## 카메라 fps (영남님 제공 2026-06-05)
- **Bottom 카메라 ≈ 1.5 fps → grab 주기 667ms**, 나머지 3~4 fps → **333ms**. 검사별 자동(`_module.Name` 의 "bottom"/"btm" 판별). 사용자 노출 컨트롤은 조명 송신 주기(ms)만.

## 변경 파일
| 파일 | 변경 |
|------|------|
| `Ui\Controls\LightLiveTuningPanel.cs` | `_startCameraLive`/`_stopCameraLive` + `BindCameraLive(start,stop)`; `StartLive`/`StopLive` 에서 카메라 콜백 invoke(try/catch); `OnPeriodChanged`+생성자에서 `UpdatePeriodHz()`(ms→Hz) |
| `Ui\Controls\LightLiveTuningPanel.Designer.cs` | `_lblPeriodHz`(주기 옆 Hz 환산) + `_lblCamInfo`(하단 카메라 fps 안내) 추가 |
| `Ui\Pages\FinderPage.cs` | `_liveTimer`/`_liveOn` + `StartLive(int)`/`StopLive()`/`IsLiveOn`/`OnLiveTick`/`ResolveDefaultLiveIntervalMs`/`CollectRowsForLiveTuning` + `Dispose(bool)` override(StopLive); BuildLayout 끝에 패널 부착(720,560) + `BindCameraLive` 주입. usings +Generic/Recipes/Config |
| `Ui\Pages\InspectorPage.cs` | 동일 패턴 (패널 720,566 — 상태라벨 562 끝 회피). `_inspector?.Id` 기준 CollectRows |
| `Ui\Pages\InspectionLightPanel.cs` | Stage 86 호스팅 블록 3개(필드/BuildLayout 3줄/CollectRowsForLiveTuning) **제거** |

- 카메라 grab: `_module.Grab()` → `r.IsSuccess` → `_cam.SetFrame(r)` (기존 GRAB 경로 재사용, GrabResult).
- CollectRows: `AlgorithmCameraMapStore.Current.Get(algorithm).GetLightOverride(id).Settings` → TuningRow(ControllerPort/Channel/Level). (이전엔 InspectionLightPanel 의 _grid 에서 읽었으나, 이동에 따라 저장된 Recipe 설정에서 직접 조회.)
- 조명 송신 Timer(패널 내부)와 카메라 grab Timer(호스트)는 **독립** — 조명은 사용자 주기(ms), 카메라는 검사별 자동.

## 검증 (computer-use 미가용 → DrawToBitmap + 헤드리스 리플렉션/기능)
하니스 `cdt-320\light-livetuning-cammove-test\LiveTuningCamMoveTest.cs` — **ALL PASS**:

| 항목 | 결과 |
|------|------|
| 빌드 (MSBuild QMC.Vision) | ✅ 오류 0 / 신규 경고 0 |
| 패널 렌더(540×264) `_lblPeriodHz`/`_lblCamInfo` | ✅ "≈ 20.0 Hz" + 카메라 안내 완전 표시(PNG 육안) |
| StartLive → 카메라 start 콜백 + _liveOn + timer | ✅ |
| StopLive → 카메라 stop 콜백 + _liveOn=false | ✅ |
| fps 환산 100ms→10.0 Hz (시나리오 4) | ✅ |
| null 카메라 콜백 안전 (시나리오 3) | ✅ 예외 없음 |
| FinderPage/InspectorPage StartLive/StopLive/CollectRows/Dispose 존재 | ✅ |
| InspectionLightPanel 호스팅 제거(_liveTuning/CollectRows 없음, 시나리오 5) | ✅ |

> ⚠️ 시나리오 1·2·6(실 앱에서 Sim 카메라 영상 갱신·노드전환 Dispose·Inspector 페이지)은 GUI 자동화 불가로 미실행. 단 카메라 grab 경로(`_module.Grab()`+`_cam.SetFrame`)는 기존 GRAB 버튼과 동일 검증된 코드, Dispose override·콜백 배선은 리플렉션으로 확인. 노드 전환 시 페이지 Dispose→StopLive→Timer 정지 보장.

## 규칙 준수
- LFine 측 변경 0 (LFineLightController/Protocol 미수정).
- UI Designer/Code 분리 유지, 람다 이벤트 0건(named handler).
- 수정 범위: FinderPage/InspectorPage/LightLiveTuningPanel(+Designer)/InspectionLightPanel 만.

## 커밋
로컬 커밋 + master 머지 + 브랜치 삭제. **remote push 안 함 — 사용자 컨펌 대기.**
