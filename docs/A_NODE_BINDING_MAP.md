# A — 알고리즘 노드 POCO ↔ 런타임 finder/inspector 매핑 (Step 0, 보고 후 정지)

> 작성 2026-06-09. **Step 0: 필드 매핑 — 보고 후 정지(컨펌).** 컨펌 후 Step 1(Apply/Collect) 구현.
> 브랜치 `refactor-baseunit-vision`(BaseUnit 리워크 WIP 스냅샷 위). 계층 확정: ROI/모델경로 전부 Recipe.

---

## 런타임 인터페이스 (실코드)
- `IPatternFinder` (`Core/IPatternFinder.cs`): `SearchRoi`(Roi, get/set), `TrainRoi`(Roi, get/set), `AcceptThreshold`(double), `MaxInstances`(int), `TrainImage`(get), `Train(Bitmap)`, `Match(Bitmap)`, `Load/SaveParameters(string)`.
- `IInspector` (`Core/IInspector.cs`): `InspectionRoi`(Roi, get/set), `Inspect(Bitmap)`, `Load/SaveParameters(string)`. **Threshold 없음**(인터페이스).
- 백엔드 한정: `CognexInspector.Threshold`(**int**, get/set, `CognexInspector.cs:23`). Sim/OpenCv Inspector 는 임계 속성 없음.
- ⚠ `IPatternFinder.Save/LoadParameters(path)` 는 **현재 P2 스토어 위임 no-op** — `=> ParameterStoreHost.Current?.SaveTarget/LoadTarget(ParameterTarget)` (`SimPatternFinder.cs` 등). `ParameterStoreHost.Current` 는 부팅에서 **미설정(null)** → **호출해도 아무 동작 안 함, `path` 무시**.

## 매핑표

### Finder (FinderAlgorithm.Finder = `f`)
| POCO 필드 | 계층 | 런타임 | 분류 | 비고 |
|---|---|---|---|---|
| FinderAlgoRecipe.AcceptThreshold | Recipe | `f.AcceptThreshold` | **직접**(double↔double) | |
| FinderAlgoRecipe.SearchRoi | Recipe | `f.SearchRoi` | **직접**(Roi) | null 가드 + Clone 권장 |
| FinderAlgoRecipe.TrainRoi | Recipe | `f.TrainRoi` | **직접**(Roi) | null 가드 + Clone 권장 |
| FinderAlgoConfig.MaxInstances | Config | `f.MaxInstances` | **직접**(int↔int) | |
| FinderAlgoRecipe.TrainModelPath | Recipe | `f.Save/LoadParameters(path)` | **모델경로(보류)** | ⚠ 현재 위임 no-op (아래 [확인1]) |
| FinderAlgoConfig.AngleEnabled | Config | (런타임 거처 없음) | **POCO-only** | 저장만, 후속 백엔드 소비 |

### Inspector (InspectorAlgorithm.Inspector = `i`)
| POCO 필드 | 계층 | 런타임 | 분류 | 비고 |
|---|---|---|---|---|
| InspectorAlgoRecipe.InspectionRoi | Recipe | `i.InspectionRoi` | **직접**(Roi) | null 가드 + Clone 권장 |
| InspectorAlgoRecipe.Threshold | Recipe | `(i is CognexInspector cog) cog.Threshold` | **백엔드한정(Cognex)** | ⚠ double↔int 변환 (아래 [확인2]) |
| InspectorAlgoConfig.Enable | Config | (런타임 거처 없음) | **POCO-only** | 저장만, 후속 |
| InspectorAlgoSetup.CalibModelPath | Setup | (런타임 거처 없음) | **POCO-only** | 저장만, 후속 |

## Apply/Collect 방향 (Step 1 구현안)
- **ApplyToRuntime()** (POCO→런타임, Load 후): `if (r.SearchRoi != null) f.SearchRoi = r.SearchRoi.Clone();` 등. 직접 항목만. 백엔드한정은 캐스트 후. POCO-only 건너뜀.
- **CollectFromRuntime()** (런타임→POCO, Save 전): `r.SearchRoi = f.SearchRoi?.Clone();` 등.
- Roi 는 `Clone()`(기존 [DataContract] Roi 메서드)로 **참조 공유 방지**(권장).

## ★ 확인 필요 (컨펌)
1. **TrainModelPath**: `f.Save/LoadParameters(path)` 가 현재 스토어 위임 no-op(path 무시)이고 Sim/OpenCv 는 실모델 파일 없음, Cognex .vpp 는 TODO. → **TrainModelPath = POCO-only(문자열만 저장, f.Save/Load 호출 안 함)** 로 둘지(권장), vs finder Save/LoadParameters 를 path 기록 방식으로 되돌릴지.
2. **InspectorAlgoRecipe.Threshold(double) ↔ CognexInspector.Threshold(int)**: 변환 바인딩(Cognex만), Sim/OpenCv 인스펙터는 임계 거처 없어 POCO-only. → 이대로 OK?
3. **Roi 바인딩**: Apply/Collect 에서 `Clone()` 사용(참조 공유 방지) OK?

## POCO-only 명시 (회귀 아님)
AngleEnabled / Enable / CalibModelPath / (Sim·OpenCv 의 Threshold) = 런타임 소비처 없음 → 저장/로드만, 동작 미반영. 후속 백엔드가 소비할 때 연결.
