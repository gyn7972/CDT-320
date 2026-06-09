# Stage 91 Phase 2 — Vision UI 디자이너 로드 가능화 (Editors)

> Phase 2 만 수행하고 정지. Phase 3~4 별도 지시 대기. 대상 top-level `D:\Work\source\QMC.Vision\`.

## 변환 (Designer/Code 분리 + 직렬화 가능 InitializeComponent)
| 파일 | 처리 | 커밋 |
|------|------|------|
| ParameterEditorBase | abstract 베이스. BuildShell shell(헤더/바/Reload·SAVE/_lblPath/_editorPanel) → Designer.cs(원본 추가순서 유지), 람다 2개→OnReloadClick/OnSaveClick. Code.cs: 생성자(IC+동적텍스트+BuildEditor+Designtime가드 후 OnLoad)+MakeXxx 헬퍼. | 2098143 |
| ParameterEditorHost | shell(헤더/바/Tool콤보/content)→Designer.cs, SelectedIndexChanged→OnToolChanged. Code.cs: 콤보 동적채움+ShowEditor. | 155843c |

## 자식 6개 (BottomInspection/Side/DieGap/Distortion/VisionScale Editor) — Designer 분리 **비적용**
- UI가 base 의 **virtual `BuildEditor`로 100% 동적 생성**(MakeNum/MakeCombo 등). base 생성자가 자식 ctor 본문보다 먼저 `BuildEditor` 호출하는 구조라 자식 InitializeComponent 가 끼어들 여지가 없음.
- **정적 컨트롤 0** → 프롬프트 규칙("reflection/동적 폼은 Code.cs 유지, Designer 엔 정적 컨트롤만")에 정확히 해당 → Designer 분리 대상 아님.
- 이미 정적검사 **위반 0**(NO-IC)이고, **base 가 디자이너 로드 가능해지면 자식도 base 통해 안전 인스턴스화**됨(아래 스모크로 확인). 동적 폼을 정적 IC로 바꾸는 것은 프롬프트가 금지.

## 디자이너 로드 차단요소 제거
- 객체 초기화자 → 속성별 할당 (base/Host shell)
- 람다 이벤트 3건 → named (OnReloadClick/OnSaveClick/OnToolChanged). **closure 캡처 0.**
- 지역변수 컨트롤(hdr/bar/btn 등) → private 필드 + `this._x = new X()`

## 정적검사 (DesignerLoadabilityCheck.ps1)
- BEFORE: Editors 7파일 NO-IC, 위반 0.
- AFTER: **위반 0**. ParameterEditorBase.Designer.cs / ParameterEditorHost.Designer.cs OK. 나머지(자식 6 + base.cs/Host.cs Code partial) NO-IC(정상 — IC 는 Designer 파트너 또는 동적).

## 빌드 / 검증 (verify_all 은퇴 — 빌드0+정적0+스모크 게이트)
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0** (선재 System.IO.Ports MSB3245 1건만).
- 스모크(헤드리스 인스턴스화+DrawToBitmap, `cdt-320\designer-phase2-test\`): **ALL PASS** — Host + 자식 6 전부 인스턴스화·렌더 무예외(MessageBox 없음; LoadJson 누락 시 기본값). Host 렌더에 BottomInspection 동적 폼 정상 표시.
- **Load/Save (Host 가 쓰는 public/protected) 시그니처 불변** 확인. 동작 변경 0.

### 디자이너 로드 검증 방식
- ParameterEditorBase 는 **abstract** → VS 디자이너 직접 열기 불가가 정상. 자식은 base 통해 인스턴스화. 프롬프트 Step4 분기대로 **① 정적검사 0 + ② 인스턴스화·DrawToBitmap 무예외 스모크**로 검증(전부 PASS).
- ⚠️ 실제 앱 SettingsPage→Editor 클릭 경로(GUI)는 미구동(스모크로 대체). verify_all.pl 미실행(은퇴).

## closure 등 보류 파일
없음.

## 커밋
Phase 2 = base + Host = 2 변환 커밋 + 검증 커밋. 로컬 master `--no-ff` 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 3 진입 대기 (컨펌 필요).**
