# Stage 90 Phase 1 — Vision UI 디자이너 로드 가능화 (Controls 4파일)

> Phase 1 만 수행하고 정지. Phase 2~4 는 별도 지시 대기.

## 경로 정정 (코드 확인)
프롬프트는 `QMC.CDT-320\QMC.Vision\`(중첩본)으로 적었으나, sln 빌드 대상 + 작업 위치는 **top-level `QMC.Vision\`**. 중첩본은 stale(LightLiveTuningPanel 없음). → top-level 에서 작업.

## 변환 (각 1커밋, Designer/Code partial 분리 + 직렬화 가능 InitializeComponent)
| 파일 | 유형 | 처리 | 커밋 |
|------|------|------|------|
| JogBox | 컴포지트 Panel(헤더+버튼6+콤보) | 생성자 UI → Designer.cs(리터럴 좌표, SuspendLayout/Resume), Code.cs(콤보 동적채움) | 83e10e7 |
| IlluminatorPanel | 컴포지트 Panel(루프+closure 람다) | 4채널 unroll + named `OnTrackValueChanged`(sender 비교)로 **closure 제거**, TrackBar BeginInit/EndInit | cd400b6 |
| BottomMenuButton | leaf 커스텀 페인트 Control | 직렬화 속성(Size/Cursor/Font/색)→Designer.cs, SetStyle/OnPaint→Code.cs | 9d06c18 |
| CameraView | leaf 커스텀 페인트 Control | BackColor+이벤트 named 연결→Designer.cs, DoubleClick 람다→`OnViewDoubleClick` 추출(closure 아님), public API 불변 | e78d797 |

Step0 도구/BEFORE: db843b7.

## 제거된 디자이너 로드 차단 요소
- 객체 초기화자 `new X { ... }` → 속성별 할당 (전 파일)
- 레이아웃 산술(`int y=...; y+=32`) + foreach 루프 → 리터럴 좌표 unroll (JogBox/IlluminatorPanel)
- 람다 이벤트 2건 → named EventHandler (IlluminatorPanel ValueChanged, CameraView DoubleClick). **closure 캡처 0**.
- 지역변수 컨트롤 → private 필드 + `this._x = new X()`

## 정적검사 (DesignerLoadabilityCheck.ps1)
- BEFORE: Controls 4파일 NO-IC(생성자 UI).
- AFTER: **위반 0**. Designer.cs 5개(신규 4 + 기존 LightLiveTuningPanel) 전부 OK. Code partial 은 NO-IC(=IC 가 Designer 파트너에 있음, 정상).

## 빌드 / 검증
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0** (선재 System.IO.Ports MSB3245 1건만).
- 4컨트롤 생성+렌더 스모크(헤드리스 DrawToBitmap): **ALL PASS**, 시각 원본과 동일(JogBox 방향/회전/Fine, Illuminator CH1~4 슬라이더). 하니스 `cdt-320\designer-phase1-test\`.
- public 시그니처(BottomMenuButton.IconText/Label/Selected, CameraView.SetFrame/SetOverlay/RoiEdited/BeginRoiDrag/ShowCrosshair/ShowLiveLabel/InfoText) **불변**. 동작 변경 0(순수 구조 리팩터).

### ⚠️ VS 디자이너 직접 로드 — 미적용 사유
대상 4개는 모두 `Control`/`Panel` 파생(UserControl/Form 아님)이라 **VS 디자이너로 직접 열리지 않음**(디자인 표면 없음). 따라서 "4파일 디자이너 열기"는 해당 없음. 대신 ① 직렬화 가능 InitializeComponent(정적검사 0) ② 생성+렌더 무예외(스모크)로 검증 — 이로써 이 컨트롤을 호스팅하는 Form/UserControl 의 디자이너 로드 시 안전하게 인스턴스화됨.
- verify_all: 이 클론에서 stale(없는 AlarmMaster.cs 경로 참조)로 미실행 — 별도 재작성 필요(본 작업 무관).

## closure 보류 파일
없음 (IlluminatorPanel closure 는 unroll+named 핸들러로 해소).

## 커밋
Phase 1 = Step0 + 4파일 = 5커밋. 로컬 master 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 2 진입 대기 (사용자 컨펌 필요).**
