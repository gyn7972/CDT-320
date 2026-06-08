# R2d 계획 — 조명설정+라이브튜닝 복원 / Inspector 3열 / 디자인타임 Size

> 생성: 2026-06-08. R2d Step 0(읽기 전용). 보고 후 컨펌 → 구현.

## (1) 검사별 조명 설정 + 라이브튜닝 — 현황·배치안
### 옛 진입경로 (FinderPage/InspectorPage BuildChildPanels — 절대좌표 자식)
- **InspectionLightPanel**(검사별 조명): `ctor(algorithm, inspectionId)` + `SelectInspection(alg, insp)`. 옛 위치 좌하단 (6,544) 440×280. **단위 = 검사(세팅)별**(algorithm+inspectionId).
- **LightLiveTuningPanel**(카메라 라이브+조명 펄스): `ctor()` + `Initialize(Func<IEnumerable<TuningRow>>)` + `BindCameraLive(startLive, stopLive)`. 옛 위치 우측 (720,560) 540×264. 카메라 라이브 start/stop 콜백 + 현재 조명세팅 행 제공.
- 새 3열엔 둘 다 빠져 있음 = 진짜 누락.

### 배치안 (★컨펌 필요)
현 우측 컬럼 = PARAMETERS + **JOG(inert)** + **SPEED(inert)**. JOG/SPEED 는 미구현 비활성이므로:
- **(가) 우측 컬럼의 JOG/SPEED 자리 교체 → 조명설정 + 라이브튜닝** (권장): 죽은 영역을 실기능으로. 우측 = PARAMETERS / 검사별 조명(InspectionLightPanel) / 라이브튜닝(LightLiveTuningPanel). JOG/SPEED 제거(또는 하단 작게).
- (나) **별도 하단 영역(4행)**: 본문 아래 조명/라이브튜닝 행 추가(3열 유지 + 하단).
- (다) **우측 컬럼 탭**: 우측을 [파라미터][조명][라이브튜닝] 탭.
- 단위: 조명=세팅별(SelectInspection(alg, settingId)). 라이브튜닝=카메라 라이브(VisionTargetPage 의 grab loop 와 연동 — start/stop 배선). 카메라 라이브 통합 보존.

## (2) Inspector 3열 신규UI — 현황·매핑안
### 현 InspectorPage (옛, 비-3열)
- CameraView + JogBox + 결과 DataGridView + INSPECT 버튼 + PASS/FAIL verdict 라벨 + BuildChildPanels(InspectionLightPanel + LightLiveTuningPanel). `ctor(VisionModule, IInspector)` + 무인자. IInspector.Inspect / InspectionRoi.
- Handler 엔 inspector 전용 레시피 화면 없음(VisionRecipePage 가 제네릭) → VisionTargetPage 패턴 준용.

### Inspector 3열 매핑안 (★컨펌 필요)
- **(가) 신규 InspectorTargetPage(3열, VisionTargetPage 병렬)** (권장): 좌 CAMERA+결과그리드 / 중 ACTION(INSPECT + 결과 PASS/FAIL 강조) / 우 PARAMETERS(R1 ParameterGridControl=검사 파라미터) + 조명 + 라이브튜닝. `ctor(VisionModule, IInspector)`.
- (나) **VisionTargetPage 를 finder/inspector 양 모드로 통합**: 한 페이지가 주입 종류에 따라 액션(GRAB/MATCH/TRAIN ↔ INSPECT)·파라미터 전환. 페이지 1개이나 복잡.
- 권고: **(가)** 명확성(액션/결과가 달라 분리). RecipePage 세팅선택기에서 inspector 선택 시 InspectorTargetPage 로 스왑(현 InspectorPage 대체). InspectorPage public·_inspector 주입·Inspect 동작 보존(신규 페이지가 동일 로직).
- 검사 파라미터: IInspector 의 파라미터(InspectionRoi 등) → ParameterGridControl 바인딩. 액션 = INSPECT(+ 결과 PASS/FAIL).

## (3) 디자인타임 Size 정정 (코드 산출, 추측 금지)
표준 해상도 = **Form1 ClientSize 1920×1080, Maximized, AutoScaleMode.None**.
- 도크 패널: header 70 + statusbar 28 + bottombar 80 → **pnlContent = 1920×902**.
- **RecipePage** Dock=Fill in pnlContent → design Size **1920×902**.
- RecipePage 내부: 헤더 30 + 세팅바 40 + 본문[_content | 사이드바 210]. → **_content = (1920−210)×(902−30−40) = 1710×832**.
- **VisionTargetPage / InspectorTargetPage** Dock=Fill in _content → design Size **1710×832**.
- 신규 조명/라이브튜닝 패널: 우측 컬럼(380 폭) 내 배치 시 그 셀 footprint(예: 380×N).
- 전 재설계 UserControl **AutoScaleMode = None**(Form1 일관). 런타임 Dock/Fill 불변(순수 디자인타임). 정정 후 VS 디자이너로 열어 비율 확인.
- 대상: RecipePage(1920×902), VisionTargetPage(1710×832), InspectorTargetPage(1710×832), 기타 신규. (여력 시 직전 스윕 페이지 150×150 도 정정 — 별도 보고.)

## 권고 요약 / 컨펌 항목
1. 조명+라이브튜닝 배치 = **(가) 우측 JOG/SPEED 자리 교체**.
2. Inspector = **(가) 신규 InspectorTargetPage(3열)**.
3. design Size = 위 산출(1920×902 / 1710×832), AutoScaleMode.None.
구현 전 1·2 컨펌 필요(추측 금지). 3 은 산출 확정.
