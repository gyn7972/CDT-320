# PAGE_TEXTCOL_PLAN — InspectionLightPanel Page 칸 ComboBox → TextBox

브랜치 `cleanup-lightpanel-page-textcolumn`. Vision 전용. Page 는 모듈 Setup.LightPages 에서 고정 → 레벨 그리드선 읽기전용 표시뿐이라 콤보 불필요.

## 확인 결과 (file:line)
- `Page` 컬럼 = `DataGridViewComboBoxColumn` — 필드 [Designer.cs:238], new [Designer.cs:46], 설정 [Designer.cs:214-216](FillWeight 12/HeaderText/Name).
- **편집 의존 없음**: 값 세팅 [cs:207](`Cells["Page"].Value = pr.Page`), 수집 [cs:234](`IntOf(r,"Page",0)` 텍스트 파싱), ResetAll [cs:318](`if(!ReadOnly)` 가드 → 읽기전용이라 미작동). 콤보 채우기만 [cs:182-186](cast→Items.Clear→Add→ReadOnly=true).
- `_colPg` [Designer.cs:20] = **dead**(전수 grep 결과 선언 외 참조 0).

## 변경
1. Designer: `Page` 필드/`new` 를 `DataGridViewTextBoxColumn` 으로. FillWeight/HeaderText/Name 유지. dead `_colPg` 제거.
2. Code: [cs:182-186] 콤보 cast+Items 블록 → `_grid.Columns["Page"].ReadOnly = true;` 한 줄. 값 세팅(:207)·수집(IntOf)·ResetAll 불변.

## 게이트
전체 빌드0 / 정적0 / verify 코어 FAIL0 / PanelLight·CamLight·ModuleLight PASS / 디자이너 로드 / R2(Page 텍스트·읽기전용 표시) 보존 / push 안 함.
