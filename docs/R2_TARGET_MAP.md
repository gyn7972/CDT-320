# R2 — RecipePage 타깃 매핑 (Handler VisionRecipePage 미러, 우측 사이드바)

> 생성: 2026-06-08. R2 Step 0(읽기 전용). 현 RecipePage → 우측 사이드바 미러 재설계 기준.
> 기준: docs/R1_API_MIRROR.md(§VisionRecipePage, §ui/tab). R1 컨트롤 완료(ParameterGridControl/Item/NumericKeypadDialog).

## 현 RecipePage 구조 (보존 대상)
- 3행 TLP `_root`: `_hdr`(30) / `_bar`(40: `_btnSpc`·`_btnParams`) / `_table`(2열: `_tree` 280 | `_content` %).
- `_tree`: 5 모듈 루트 → Finder/Inspector 자식(Tag=FinderLauncher/InspectorLauncher).
- `_content` 동적 호스트: ShowFinder→`FinderPage(mod,finder)` / ShowInspector→`InspectorPage(mod,insp)` / ShowSpc→`SpcChartPage` / ShowParameterEditors→`ParameterEditorHost`. 전부 `_content.Controls.Clear()`+Dock.Fill.
- 무인자 ctor / Load→PopulateTree(FindForm()=Form1 의 5모듈) / public 진입 ShowSpc·ShowParameterEditors.
- **보존 필수**: 무인자 ctor, `_content` 호스트 계약, ShowSpc/ShowParameterEditors, FinderPage/InspectorPage(mod,x) 인스턴스화, Form1 모듈 발견.

## 타깃 taxonomy (런타임 — 모듈별 Finders/Inspectors + 글로벌)
AddFinder/AddInspector 등록(코드 확인):
| 모듈(그룹) | 카메라 | Finder 타깃(→FinderPage) | Inspector 타깃(→InspectorPage) |
|---|---|---|---|
| Wafer vision | WaferMod | EjectPinFinder, ReticleFinder, AlignDieFinder, FirstReferenceFinder, SecondReferenceFinder, DieFinder, ScaleFinder (7) | — |
| Bin vision | BinMod | ReticleFinder, DieFinder, ScaleFinder (3) | PlacementInspector (1) |
| Bottom inspection | BottomMod | ReticleFinder, ColletFinder, DieFinder, FocusFinder, ScaleFinder, DistortionCompensation (6) | SurfaceInspector (1) |
| Front side | FrontSideMod | DieEdgeFinder, FocusFinder (2) | TopSurfaceInspector, TopChippingInspector (2) |
| Rear side | RearSideMod | DieEdgeFinder, FocusFinder (2) | BottomSurfaceInspector, BottomChippingInspector (2) |
| **글로벌** | — | SPC X-bar(→SpcChartPage), Inspection Parameters(→ParameterEditorHost) | |

- 총 **타깃 ≈ 28 finder/inspector + 2 글로벌**(런타임, 모듈 Finders/Inspectors 사전 기반). 모듈 5그룹 + 글로벌 1그룹.

## 타깃 → 페이지 (재사용 vs 신규)
| 타깃 종류 | 페이지 | 재사용/신규 |
|---|---|---|
| Finder 타깃 (전부) | `FinderPage(VisionModule, IPatternFinder)` | **재사용**(기존, 무수정) |
| Inspector 타깃 (전부) | `InspectorPage(VisionModule, IInspector)` | **재사용**(기존, 무수정) |
| SPC | `SpcChartPage` | **재사용** |
| Inspection Parameters | `Editors.ParameterEditorHost` | **재사용** |
- ⇒ **모든 타깃이 기존 호스트 페이지를 재사용**. R2 의 핵심 신규 = **사이드바 쉘**(트리 대체). 타깃 본문은 기존 페이지.

## 쉘 재설계 (Step 1)
- `_root` 2행(헤더 / 본문)로. 본문 TLP 2열: **좌 `_content`(%) | 우 사이드바(Absolute ~240)**. 트리 제거.
- 사이드바(우측, RecipeTab 경량 미러): 모듈 그룹 헤더 + 타깃 버튼(상태 점) → lazy 페이지 팩토리 → `_content` 스왑. `RegisterTarget(name, Func<UserControl>)`/`ShowTarget(name)` (Handler RegisterSidebarButton/ShowPage 경량판, 쉘/Security/i18n/UiClickAuditor 결합 미사용).
- 글로벌(SPC/Parameters)도 사이드바 버튼으로(기존 `_bar` 버튼 대체 or 병행).
- 동적 버튼은 런타임 BuildSidebar(Code), IC 엔 정적 TLP+컨테이너만(ic-helper-call/객체초기화자/산술 0).

## ★ 열린 결정 (Step 2 대표 타깃 — 사용자 확인 필요)
프롬프트 Step 2 = "대표 타깃 1개를 **3열 TLP 페이지 클래스**(좌 카메라+매치 / 중 ROI라디오+액션3×3 / 우 ParameterGridControl+JOG+SPEED)로 완성". 그런데 현 타깃 본문은 전부 FinderPage/InspectorPage(다른 레이아웃: CameraView+JogBox+결과그리드+버튼). 따라서 결정 필요:
- **(가) 신규 3열 페이지 = Handler VisionRecipePage 그대로 미러**: 대표 타깃(예: Wafer/ReticleFinder 또는 AlignDie)용으로 신규 `VisionTargetPage`(3열 TLP, R1 ParameterGridControl + 액션패널 디자인차용 + JOG/SPEED) 생성, 기능(Grab/Match/Teach/Save)을 해당 Finder 동작에 배선. 나머지 타깃은 Step 4에서 동일 패턴 reskin 또는 FinderPage 유지.
- **(나) 기존 FinderPage/InspectorPage 재사용 + 사이드바만 교체**(최소 변경, 기능 100% 보존): 3열 신규 페이지 없이 쉘만 미러. 단 프롬프트의 "3열 TLP 페이지" 의도와 다름.
- **권고: (가)** 로 대표 1개만 신규 프로토타입 → Step 3 사인오프 → Step 4 확산. 단 **대표 타깃 선정**(어느 Finder/Inspector가 카메라+ROI+액션+스케일 필드를 가장 잘 대표하는가)과 **기존 FinderPage 대체 여부**는 사용자 확인.
- 부수 결정: **저장 의미**(타깃단위 vs 레시피전체), **사이드바 스크롤**(타깃 28개 → 그룹 접기/스크롤), ROI 라디오 구성(Vision Finder 에 Main/Sub/Chip/Cross·Index 개념 매핑 여부).

## 다음
Step 1(사이드바 쉘, 트리 제거, 계약 보존)은 결정과 무관하게 진행 가능. Step 2(대표 3열 페이지)는 위 (가)/(나) + 대표 타깃 확정 후.
