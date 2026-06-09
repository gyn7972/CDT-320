# Stage 92 Phase 3 — Vision UI 디자이너 로드 가능화 (Dialogs)

> Phase 3 만 수행하고 정지. Phase 4 별도 지시 대기. 대상 top-level `D:\Work\source\QMC.Vision\`.

## 변환: ZoomDialog (Form 파생, 1커밋)
- `Ui\Dialogs\ZoomDialog.cs` → Designer/Code 분리.
- **ZoomDialog.Designer.cs**: 컨트롤 필드(_statusBar/_bar/_btnReset/_btnSave/_btnClose/_canvas) + `InitializeComponent`(직렬화 가능: 속성별 할당, 리터럴, 폼 속성 블록, SuspendLayout/ResumeLayout, 원본 추가순서+SetChildIndex) + `Dispose(bool)`.
- **ZoomDialog.cs**: partial+Form, 상태필드(_image/_zoom/_offset/_dragging/_lastDrag), 생성자, 이벤트 핸들러 본문, 줌/팬/그리기 로직, PanelStyleExt.

### 핵심 변경
- 람다 이벤트 4개 → named: `OnResetClick`/`OnSaveClick`/`OnCanvasDoubleClick`/`OnCanvasMouseEnter` (전부 `this`만 캡처 → **closure 없음**). reset 공통 로직은 `ResetView()` 헬퍼.
- 객체 초기화자 → 속성별 할당, 지역변수 컨트롤(bar) → `_bar` 필드.
- 동적 텍스트(폼 Text=title, 상태바 이미지크기)는 생성자에서 설정(Designer 엔 placeholder).
- `_canvas.SetStyle()`(reflection 더블버퍼 헬퍼) → 생성자에서 호출(Designer 금지).
- **디자이너 인스턴스화용 파라미터리스 생성자 `public ZoomDialog() : this(null)` 추가** (additive — 기존 `(Bitmap, title)` 생성자 보존). Form 디자이너는 파라미터리스 ctor 필요.

## 검증 (게이트 = 빌드0 + 정적0 + 스모크; verify_all 은퇴·미실행)
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0** (선재 System.IO.Ports 1건만).
- 정적검사: BEFORE NO-IC → **AFTER 위반 0** (ZoomDialog.Designer.cs OK).
- 스모크(헤드리스, `cdt-320\designer-phase3-test\`): **ALL PASS** — 양 생성자(파라미터리스/`(Bitmap,title)`) 인스턴스화 + DrawToBitmap 무예외. 런타임 렌더에 제목/상태바/이미지+크로스헤어/1:1·SAVE·CLOSE 정상.
- **public 시그니처 보존**: `ZoomDialog(Bitmap, string)` 생성자 + DialogResult(_btnClose=OK) 불변. 동작 변경 0.

### 디자이너 로드 검증
- Form 파생 + 파라미터리스 ctor + 정적검사 0 + 인스턴스화·렌더 무예외 → VS 디자이너 로드 가능 조건 충족(헤드리스로 확인). 실제 VS 열기 시각확인은 사용자 가능(선택).

## closure 등 보류: 없음.

## 커밋
변환 1커밋 + 검증 1커밋 → 로컬 master `--no-ff` 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 4 진입 대기 (컨펌 필요).**
