# R2b — 검사 알고리즘별 세팅 매핑 (ROI-슬롯 → 세팅 선택기)

> 생성: 2026-06-08. R2b Step 0(읽기 전용). 사이드바 평면화 + 중앙 ROI-라디오 슬롯 용도전환 근거.
> 출처(코드 직접확인): `QMC.Common/Recipes/AlgorithmCameraSubset.cs`(VisionAlgorithm.All, Label, InspectionLabel) + `QMC.Vision/Modules/*`(AddFinder/AddInspector).

## 검사 알고리즘 (사이드바 평면 — VisionAlgorithm.All, 5개)
| 키 | 라벨 | Form1 모듈 |
|---|---|---|
| Wafer | 웨이퍼 비전 | WaferMod |
| Bin | 빈 비전 | BinMod |
| BottomInspection | 바텀 검사 | BottomMod |
| FrontSide | 앞쪽 측면 검사 | FrontSideMod |
| RearSide | 뒤쪽 측면 검사 | RearSideMod |

## 알고리즘별 세팅(서브항목) = 그 알고리즘의 Finders/Inspectors (실재, 죽은컨트롤 아님)
세팅 선택기에 들어갈 실제 항목(라벨=InspectionLabel, 종류 F=Finder/I=Inspector):
| 알고리즘 | 세팅(선택기 버튼) |
|---|---|
| 웨이퍼 비전 | 이젝트핀(F) · 레티클(F) · 얼라인다이(F) · 첫째기준(F) · 둘째기준(F) · 다이(F) · 스케일(F) |
| 빈 비전 | 레티클(F) · 다이(F) · 스케일(F) · 안착검사(I) |
| 바텀 검사 | 레티클(F) · 콜렛(F) · 다이(F) · 포커스(F) · 스케일(F) · 왜곡보정(F) · 표면(I) |
| 앞쪽 측면 검사 | 다이에지(F) · 포커스(F) · 앞쪽면(I) · 앞쪽칩핑(I) |
| 뒤쪽 측면 검사 | 다이에지(F) · 포커스(F) · 뒤쪽면(I) · 뒤쪽칩핑(I) |
- ⇒ 세팅 선택기는 **module.Finders + module.Inspectors** 로 런타임 생성(라벨=InspectionLabel.Get(alg, id), 폴백=id). 모두 실재 → 죽은 컨트롤 없음.
- 각 세팅(finder/inspector) 자체의 내부 파라미터(ROI/threshold 등)는 **우측 ParameterGridControl** 에 표시(현행). 즉 2단계: 사이드바=알고리즘 → 세팅선택기=finder/inspector → 우측=그 항목의 파라미터.

## ⚠ 설계 논점 (구현 전 확인 필요)
1. **Finder vs Inspector 혼재**: 세팅 중 일부는 Inspector(안착/표면/칩핑 등). 현 VisionTargetPage 의 액션(GRAB/MATCH/TRAIN)·우측 파라미터(Search/Train ROI)는 **Finder 중심**. Inspector 선택 시:
   - (A) 같은 페이지에서 액션/파라미터를 Inspector용으로 전환(INSPECT/PASS·FAIL, InspectionParameters) — 페이지가 두 모드 지원.
   - (B) 세팅선택기는 **Finder만** 노출, Inspector 는 별도(기존 InspectorPage) — 단순하나 "알고리즘 단위" 일관성 약화.
   - (C) 세팅선택기에 Finder/Inspector 모두 노출하되, 선택 종류에 따라 본문 컨트롤(_content) 을 VisionTargetPage(finder) / InspectorPage(inspector) 로 스왑.
2. **VisionTargetPage ctor 모델 변경**: 현 `(module, IPatternFinder)` → 알고리즘 단위면 `(module)` + 내부 현재-세팅 상태. 또는 사이드바가 알고리즘→첫 세팅으로 VisionTargetPage(module, finder) 생성하고 세팅선택기가 finder 교체.
3. **글로벌 SPC/검사파라미터**: 평면 사이드바는 "검사 알고리즘만" → SPC/ParameterEditors 진입을 상단바 버튼 or 사이드바 하단 별도 그룹 중 어디로?
4. **상태점 판정 단위**: 알고리즘 단위 dirty vs 세팅(finder/inspector) 단위 dirty. 저장(상단바)도 알고리즘 단위 vs 세팅 단위.

## 권고
- 사이드바=5 알고리즘 평면(확정 가능). 세팅선택기=알고리즘의 finder/inspector(실재). **(C)** (종류별 본문 스왑)이 "알고리즘 단위 일관성 + 기존 InspectorPage 재사용" 균형. 단 위 논점 1~4 는 사용자 확인 후 구현(추측 금지, 죽은컨트롤 금지).
