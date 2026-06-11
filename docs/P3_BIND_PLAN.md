# P3 도메인 연결 + id 통일 — Step 0 조사·매핑 (보고 후 정지)

> 작성 2026-06-09. **Step 0: 백엔드 소비 실태 + 매핑 — 보고 후 정지(컨펌).** 컨펌 후 Step 1~3.
> 기준 `docs/PARAMETER_REWORK_DESIGN.md`(§4), `docs/PARAMETER_LAYER_DECISIONS.md`. 브랜치 `param-rework-p3-domainbind`.
> 전제(확정): ② 파라미터는 전부 orphan(어떤 인스펙터도 안 읽음). P3=구조 연결 + **백엔드 실소비 필드만** 통합. 알고리즘 구현 범위 밖.

---

## §A 백엔드 Inspect/Match 실소비 파라미터 전수 (코드 확인)

| 백엔드 | 메서드 | 실제 읽는 파라미터 | DLL 게이트 | 관리코드 테스트 가능? |
|---|---|---|---|---|
| SimPatternFinder | Match | `MaxInstances`(`SimPatternFinder.cs:44`), `TrainRoi.CenterX/Y`(`:52-53`) — **AcceptThreshold 미사용** | 없음 | ✅ (TrainRoi 편집→Match CenterX 이동) |
| OpenCvPatternFinder | Match | AcceptThreshold = **주석만**(`OpenCvPatternFinder.cs:74`), EmguLoaded 경로 TODO(`:48`) | EmguCV | ❌(미소비/fallback) |
| CognexPatternFinder | Match | `AcceptThreshold`(`:106`), `MaxInstances`(`:107`) — **CognexLoaded 일 때만**(`:90`) | **Cognex** | ❌(DLL 없음→fallback) |
| SimInspector | Inspect | 없음(랜덤, `SimInspector.cs:20-28`) | 없음 | — |
| OpenCvInspector | Inspect | `InspectionRoi.Width/Height`→"Area"(`OpenCvInspector.cs:32-34`); EmguLoaded 경로 TODO(`:25`) | 없음(fallback) | ✅ (InspectionRoi 편집→Area 변화) |
| CognexInspector | Inspect | `Threshold`(`:81`), `MinDefectArea`(`:84,102`), `MaxTotalDefectArea`(`:112`) — **CognexLoaded 일 때만**(`:43-44` 아니면 Sim fallback) | **Cognex** | ❌(DLL 없음→fallback) |

기타 Cognex 보조(CognexCaliper.EdgeThreshold 등)는 인스펙터 아님(IInspector 미구현) — 범위 밖.

## §B ②↔실소비 겹침 (알고리즘 추가 없이 통합 가능한 필드)
- **유일한 겹침 = `CognexInspector.Threshold` ↔ `BottomInspectionParameters.Threshold`** (둘 다 int, 기본 128, 같은 정규 target `BottomInspection/SurfaceInspector`).
- `MinDefectArea`/`MaxTotalDefectArea` = **Cognex blob 전용, ② 대응 필드 없음** → inline 유지(디스크립터 Recipe 로 노출 유지).
- 그 외 ② 24/12/3/5/4 필드(Bottom 나머지·Side·DieGap·Distortion·VisionScale) = **소비 백엔드 없음(310-era)** → 정의/편집/저장만 유지, Inspect 미소비(회귀 아님).

## §C ②↔인스턴스 정규 매핑 (레지스트리안, §4-2)
| ② 클래스 | 정규 target | 상태 |
|---|---|---|
| BottomInspectionParameters | `BottomInspection/SurfaceInspector` | **연결**(Threshold 실소비 통합) |
| DistortionParameters | `BottomInspection/DistortionCompensation` | 매핑 확정, **소비 백엔드 없음** → 보유만(소비 통합 없음) |
| VisionScaleParameters | `<module>/ScaleFinder`(+VisionConfig.ScaleX/Y) | **orphan 유지**(매핑 잠정·소비 없음) |
| SideInspectionParameters | `SideInspection/?` | **orphan 유지**(매핑 불명) |
| DieGapInspectionParameters | `DieGapInspection/?` | **orphan 유지**(대응 인스펙터 없음) |

## §D ★ 테스트 가능성 — 중요 제약 (보고)
- "Cognex Threshold 편집→실 Inspect 반영"은 **Cognex DLL 필요**(이 환경 미설치 → `CognexInspector.cs:43-44` fallback 으로 Threshold 미소비). → **실 blob 경로 런타임 반영 테스트 불가**.
- **관리코드로 테스트 가능한 소비-바인딩 체인**(DLL 무관, store→디스크립터→실객체→Inspect/Match 무결성 입증):
  - SimPatternFinder: store.SetValue(TrainRoi.CenterX) → `Match` 결과 CenterX 이동 ✅
  - OpenCvInspector: store.SetValue(InspectionRoi.Width/Height) → `Inspect` "Area" 변화 ✅
- Cognex Threshold 통합은 **white-box 구조 검증**(인스펙터 보유 ②.Threshold ← store 편집값, Inspect 가 `_params.Threshold` 읽도록 배선)으로 입증 가능. blob 결과 반영은 Cognex 설치 환경에서 별도 확인.

## §E P3 구현 범위 제안
1. **Step 1 (G3)**: 정규 target = `<AlgorithmKey>/<instanceId>`(이미 P1 ParameterTarget). ②↔인스턴스 매핑을 **코드 레지스트리 1곳**(`Core\Parameters\InspectionParamRegistry` 또는 Config)으로. `SettingsPage.cs:160-174` 문자열 tool 분기 → 레지스트리 참조로 교체.
2. **Step 2 (G2 구조)**: `CognexInspector` 가 `BottomInspectionParameters _params` **보유**. ParameterStoreBootstrap 의 독립 BottomParams → 인스펙터 보유 인스턴스로 일원화(dedupe 충돌 정식 해소). 디스크립터 Getter/Setter 가 인스펙터 보유 ②에 바인딩.
3. **Step 3 (실소비 단일화)**: `CognexInspector.Inspect` 가 `Threshold` 를 `_params.Threshold` 에서 읽도록 → 편집이 실검사 반영(단일 진실원). `MinDefectArea`/`MaxTotalDefectArea` inline 유지. 310-era·Side·DieGap = 정의only 유지(주석).
   - ⚠ 단 Cognex DLL 없으면 런타임 미검증 → §D white-box + 관리코드 체인 테스트로 게이트.

## 확정 요청 (2점)
1. **CognexInspector ② 보유·Threshold 통합 진행?** (가) 진행(white-box+구조 검증, Cognex 환경서 실반영 후속 확인) vs (나) id 레지스트리만 하고 소비 통합은 Cognex 가용 환경까지 보류.
2. **반영 테스트 대체 승인?** Cognex blob 런타임 불가 → SimFinder TrainRoi / OpenCvInspector Area 관리코드 체인 + Cognex white-box 로 게이트 인정.
