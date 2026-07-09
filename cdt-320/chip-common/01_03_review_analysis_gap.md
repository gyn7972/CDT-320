# CDT-320 칩데이터·차트 리밋 공통화 — 문서/코드/갭 분석 (Stage 1–3 통합)

요청이 한 문단 규모라 Stage 1(문서)·2(코드)·3(갭)을 본 파일 하나로 압축한다.

## 요구사항 (Stage 1)

| ID | 분류 | 요구사항 | 출처 | 우선순위 |
|----|------|----------|------|---------|
| R-001 | Business Logic | 칩데이터 값(칩 폭/높이 스펙)을 모듈(검사기)별이 아니라 **프로젝트 레시피에서 공통 입력** → 검사 판정에 반영 | 사용자 요청 2026-07-09 | Must |
| R-002 | Business Logic | 칩 차트 리밋 값(운영뷰 차트 빨간 점선)도 **프로젝트 레시피 공통값에서 반영** | 사용자 요청 2026-07-09 | Must |
| R-003 | UI | "예전에 입력 가능했는데 삭제된 것 같다" — 입력 위치 확인/복원 | 사용자 요청 2026-07-09 | Must |

## 코드 현황 (Stage 2)

### 데이터 모델 — 공통 필드는 이미 존재
`QMC.Vision/Equipment/Unit/VisionMachineData.cs` (VisionMachineRecipe, 프로젝트 레시피):
- `ChipWidthMm`, `ChipHeightMm` (기준값), `ChipWidthLowerMm/UpperMm`, `ChipHeightLowerMm/UpperMm` (56–61행)
- `MaxChippingDepthMm`, `MaxChippingLengthMm`, `MaxForeignSizeMm` (70–72행)

### 입력 UI — 삭제되지 않았음
`QMC.Vision/Ui/Pages/Settings/Recipe/RecipePage.cs` `BuildCommonGrid()` (565–592행):
레시피 페이지에서 **[프로젝트] 버튼 → 우측 공통 그리드**에 Chip Width/Height, Chip W/H Lower/Upper,
Max Chipping Depth/Length, Max Foreign Size 입력칸이 현재도 존재한다(6/18 "Vision UI 머지"에서 추가, 이후 변경 없음).
구 `Ui/Pages/Recipe/*` 페이지는 6/18에 삭제됐지만 같은 기능이 `Ui/Pages/Settings/Recipe/RecipePage.cs`로 이전됨.
→ 사용자가 "삭제됐다"고 느낀 것은 UI 개편으로 위치가 바뀐 것 + 아래 '무반영' 문제로 추정.

### 실제 판정/차트 경로 — 모듈별 값만 사용
- Bottom 판정 스펙: `BottomInspector.ChipLower/UpperSpecLimit` ← 모듈별 `InspectorAlgoRecipe.Width/HeightLower/UpperLimit`
  (`AlgorithmNode.cs` ApplyToRuntime 242–243행, 입력은 `InspectorTargetPage.cs` 436–439행 "Width Lower" 등)
- 차트 점선: 모듈별 `InspectorAlgoRecipe.Chart1/2Upper/LowerLimit` → `ChartLimitStore.Set()`
  (`AlgorithmNode.cs` 272–305행 ② 블록, `InspectorTargetPage.cs` PushChartLimits/AddChartLimitItems)
- 뷰어 소비: `InspectionViewerControl.ApplyChartLimits()` → `ChartLimitStore.TryGet()`

## 갭 분석 (Stage 3)

| Req ID | Status | 근거 | 비고 |
|--------|--------|------|------|
| R-001 | ❌ Missing | `ChipWidthMm`/`ChipW·H Lower/UpperMm`의 **소비처가 없음** (UI 입력·직렬화만 존재) | 입력해도 검사에 무반영 — dead field |
| R-002 | ❌ Missing | 차트 리밋은 모듈별 `Chart1/2*Limit`만 `ChartLimitStore`로 push | 공통 유도 경로 없음 |
| R-003 | ✅ Implemented(위치 이동) | RecipePage [프로젝트] 공통 그리드에 입력칸 존재 | 사용자 안내 필요 |

유일한 공통값 소비 사례(참고 패턴): `MaxChippingDepthMm` → `InspectionViewerControl.cs` 352·397행(맵 칩핑 한계).

## Stage 4로 넘길 항목
- R-001: 공통 칩데이터 → Bottom 검사기 스펙 전파
- R-002: 공통 값 → 차트 리밋(ChartLimitStore) 유도
