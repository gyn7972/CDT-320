# CDT-320 설계 — 칩데이터·차트 리밋 프로젝트 레시피 공통화 (Stage 4, 확정본)

근거: [01_03_review_analysis_gap.md](01_03_review_analysis_gap.md)

## 사용자 확정 사항 (2026-07-09)
1. **범위 = Bottom만** — Side 칩핑/Bin 갭은 모듈별 유지.
2. **우선순위 = 모듈 우선** — 모듈(검사기) 레시피 값이 설정돼 있으면 그대로 사용,
   모듈값 미설정(해당 축 상·하한 모두 0)일 때만 프로젝트 레시피 공통값으로 폴백.
3. **입력 방식 = 기준값 ± 공차** — 공통 그리드에서 Chip Width/Height 기준값 + 공차(±) 입력,
   Lower/Upper는 자동 계산해 기존 필드에 저장(신규 저장 필드 없음).

## 설계 원칙
- 신규 저장 필드 없음 — 기존 dead field(`ChipWidthMm/HeightMm`, `ChipW·H Lower/UpperMm`)에 소비처를 만든다. 직렬화 포맷 불변.
- 공통값 유효 조건: 해당 축 `Lower > 0 && Upper > Lower` (공차 0 = 스펙 미완성으로 간주, 비활성).
- 전파 시점 = 레시피 적용(ApplyToRuntime) + 공통설정 저장 직후(즉시 반영).

## 신규 헬퍼 — `QMC.Vision/Equipment/Core/CommonChipSpec.cs` (NEW)
공통값 조회 + Bottom 적용 로직을 한 곳에 모아 AlgorithmNode / RecipePage 가 공유(중복 금지).
```csharp
public static class CommonChipSpec   // namespace QMC.Vision.Core
{
    // ActiveRecipeContext.Current 의 ChipW/H Lower·UpperMm. 축별로 Lower>0 && Upper>Lower 일 때만 true.
    public static bool TryGetCommonWidth(out double lower, out double upper);
    public static bool TryGetCommonHeight(out double lower, out double upper);

    // Bottom 검사기 스펙 적용 — 모듈 레시피 값 우선, 미설정 축만 공통 폴백. 공통 사용 시 EventLogger 1줄.
    public static void ApplyToBottom(BottomInspector bi, InspectorAlgoRecipe r);

    // Bottom 차트1(Width)/차트2(Height) 리밋 push — 모듈 Chart*Limit 미설정(상·하한 0) 축만 공통값으로 유도.
    public static void PushBottomChartLimits(InspectorAlgoRecipe r);
}
```

## R-001 — Bottom 검사기 스펙 (모듈 우선 폴백)
**Touched:** `AlgorithmNode.cs` ApplyToRuntime BottomInspector 분기(242–243행) → `CommonChipSpec.ApplyToBottom(bi, r)` 호출로 대체.
- Width 축: `r.WidthLowerLimit`/`r.WidthUpperLimit` 중 하나라도 >0 이면 모듈값, 둘 다 0이면 공통 Width.
- Height 축 동일. 축 단위 독립 폴백.

## R-002 — 차트 리밋 유도
**Touched:** `AlgorithmNode.cs` ② 차트 push 블록 — `cm == "Bottom"` 인 경우만 `CommonChipSpec.PushBottomChartLimits(rec)` 로 대체(Bin/Side 기존 유지).
- 차트1: `rec.Chart1Upper/LowerLimit` 둘 다 0이고 공통 Width 유효 → `Set("Bottom", 0, 공통WUpper, 공통WLower)`.
- 차트2: Height 동일.

## R-003 — 공통 그리드 입력(기준값±공차) + 즉시 반영
**Touched:** `RecipePage.cs` BuildCommonGrid(565행)·OnCommonSaveClick(594행)
- 기존 "Chip W/H Lower·Upper" 4칸 제거 → "Chip Width ±", "Chip Height ±"(공차) 2칸 추가.
  - 공차 getter = `(Upper − Lower) / 2` (음수면 0). 
  - 기준값 setter: `ChipWidthMm = v` + 현재 공차로 Lower/Upper 재계산(`max(0, v−tol)` / `v+tol`).
  - 공차 setter: `tol = |v|` → Lower/Upper 재계산. 기준값 0이면 Lower 0 → 스펙 비활성(안전).
- OnCommonSaveClick: 저장 후 `BottomInspection.Algorithms` 순회 →
  `ApplyToBottom` + `PushBottomChartLimits` 재실행(운영뷰 차트 즉시 갱신, 레시피 재적용 불필요).
