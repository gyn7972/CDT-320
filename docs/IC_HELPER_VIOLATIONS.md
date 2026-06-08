# IC 내 헬퍼 호출 위반 — 디자이너 로드 실패 (정찰)

생성: 2026-06-08. 브랜치 `fix-vision-pages-ic-helper-inline`.
증상: VS 디자이너에서 `'...UserControl.InitLbl/InitNum/BeginInitNums' 메서드를 찾을 수 없습니다` → 로드 실패.
원인: `InitializeComponent()` 본문이 사용자 헬퍼를 **호출** → CodeDom 직렬화기가 루트 타입(UserControl)에서 못 찾음.

## 영향 파일 (6) — IC 내 헬퍼 호출
| 파일 | 위반 호출 종류·건수 |
|---|---|
| Ui/Pages/CameraMappingPanel.Designer.cs | InitLbl 13, InitNum 8, InitBtn 9, BeginInitNums 1, EndInitNums 1 |
| Ui/Pages/LightSystemSetupPage.Designer.cs | InitBtn 10, InitTextCol 11, InitGrid 3, InitSectionLabel 3 |
| Ui/Pages/InspectionOverridePanel.Designer.cs | InitCheck 7, InitLbl 7, InitNum 8, InitBtn 4 |
| Ui/Pages/FinderPage.Designer.cs | InitBtn 7 |
| Ui/Pages/InspectorPage.Designer.cs | InitBtn 5 |
| Ui/Pages/InspectionLightPanel.Designer.cs | InitBtn 4 |

## 무관 (IC 없음 — 동적 BuildEditor 호출, 디자이너 비대상)
- Ui/Editors/*ParameterEditor.cs + ParameterEditorBase.cs: `MakeLabel/MakeNum/MakeText/MakeCombo/MakeCheck` 는
  `BuildEditor()`(런타임)에서 호출 — InitializeComponent 없음(Phase2 동적 잔류). 디자이너 로드 대상 아님.
- Form1.cs `MakeViewer`: Form1 런타임 코드(Form1.Designer.cs 의 IC 아님).
- HikGigECamera `MakeByRefType`: 리플렉션, UI 무관.

## 조치
각 영향 파일의 IC 헬퍼 호출을 **선언적 속성 할당으로 인라인 전개**(레이아웃·순서·z-order 불변),
헬퍼 정의는 IC 전용이므로 삭제. 정적검사 도구에 IC 내 비허용 메서드 호출 검출 규칙 추가(재발 방지).
1파일=1커밋.
