# Stage 94 Phase 4b-1 — Vision UI 디자이너 로드 가능화 (InspectionOverride + InspectionLight)

> Phase 4b-1 만 수행하고 정지. 4b-2(Finder+Inspector) 별도 지시 대기. 대상 top-level `D:\Work\source\QMC.Vision\Ui\Pages\`.

## 변환: 2파일 (각 1커밋)
| 파일 | 처리 | 커밋 |
|------|------|------|
| InspectionOverridePanel | 정적 컨트롤 **33개 전부 필드화** → Designer.cs InitializeComponent(리터럴 좌표, NumericUpDown×8 BeginInit/EndInit, InitCheck/Lbl/Num/Btn 디자이너 헬퍼). 람다 11(7 CheckedChanged+4 Click)→named. 콤보 enum Items 는 Code(BuildCombos). | 49935d3 |
| InspectionLightPanel | 정적 shell(헤더/결선/바4버튼/상태/그리드+5컬럼)→Designer.cs(그리드 BeginInit/EndInit, SetChildIndex). 람다 5(4버튼+DataError)→named. **DataGridView 동적 행(BindFields)·Page콤보 Items 는 Code 유지.** | 56faf29 |

## 추출/처리
- 객체초기화자→속성별, 지역변수 좌표산술→리터럴, 지역컨트롤→필드, 람다→named EventHandler.
- **closure 0**: 두 파일의 이벤트 람다는 전부 `this`/필드만 캡처(루프 근처 람다는 LINQ로 Code 메서드에 잔류 — 이벤트 아님). InspectionOverride 의 CheckedChanged 는 InitCheck 가 Checked 설정 후 연결돼 구성 시 미발화(원본 순서 보존).
- **동적 유지(Code)**: InspectionLight 의 BindFields(그리드 행), 두 파일 콤보 Items.
- **시그니처 불변**: InspectionLight 양 생성자(파라미터리스 + (algorithm,inspectionId)) + public SelectInspection/Apply; InspectionOverride 의 OverrideChanged 이벤트 + Save/Reset/Cancel/TestGrab.

## 검증 (게이트 = 빌드0 + 정적0 + 스모크; verify_all 은퇴)
- MSBuild QMC.Vision: **오류 0 / 신규 경고 0** (선재 System.IO.Ports 1건만).
- 정적검사 AFTER: 두 Designer.cs **위반 0**.
- 스모크(헤드리스 인스턴스화+DrawToBitmap, `cdt-320\designer-phase4b1-test\`): **ALL PASS** — 두 패널 무예외. InspectionOverride 렌더에 33컨트롤(토글헤더/7행/4버튼/상태) 원본 레이아웃 정상. InspectionLight 양 생성자 보존 확인.

## ⚠️ 부수 발견 — csproj linter 드롭
작업 중 linter 가 csproj 를 재작성하며 **InspectionOverridePanel.Designer.cs Compile 엔트리를 드롭**(빌드 실패 CS0103 다발) → 재추가로 해결. **향후 phase 에서 Designer 엔트리 누락 여부 빌드로 확인 필요**(linter 가 수동 추가 엔트리를 건드릴 수 있음).

## closure 보류: 없음.

## 커밋
2 변환 커밋 → 로컬 master `--no-ff` 머지 + 브랜치 삭제. **remote push 안 함.**

→ **Phase 4b-2 (Finder+Inspector) 진입 대기 (컨펌 필요).**
