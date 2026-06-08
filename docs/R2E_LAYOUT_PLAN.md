# R2e 레이아웃 폴리시 — 진단 + 재배치안 (Step 0)

> 생성 2026-06-08. 읽기 전용 진단. **보고 후 정지 — 사용자 확정 후 Step 1~4 구현.**
> 방향 전환: Handler 3열 미러 **비강제**. 내부 UI 통일성 우선.
> 브랜치 `redesign-vision-recipepage-r2b` (R2d 0ab9f77 + chore 83420a4, 미머지).

---

## A. 현황 측정 (코드 산출, 추측 없음)

### VisionTargetPage (design 1710×832, AutoScaleMode.None)
- `_root`: 1열 / 2행 = [본문 100% | 상태바 24].  본문 높이 ≈ **808**.
- `_main`: 3열 = **[좌 42% | 중 30% | 우 380 Absolute]**, 1행 100%.
  - 좌 ≈ (1710−380)×0.42 = **≈559px**, 중 ≈ **≈399px**, 우 = **380px**.
- **좌 `_left`**: 1열 / 4행 = [secCam 24 | 카메라 100% | secMatch 24 | 결과그리드 **150**].
- **중 `_center`**: 1열 / 2행 = [secAction 24 | `_actionPanel` 100%].
  - `_actionPanel`: 3×3 (33.3% 균등), **버튼 7개**(GRAB/MATCH/TRAIN · LOAD/이미지저장/EDIT SEARCH · EDIT TRAIN).
  - 실측 footprint ≈ **399×784** → **버튼 1개 ≈ 133×261px** ⟵ ★과도하게 큼(특히 세로).
- **우 `_right`**: 1열 / 6행 = [secParam 24 | PARAMETERS 100%(≈296) | secLight 24 | 조명호스트 **240** | secLive 24 | 라이브호스트 **200**].

### InspectorTargetPage (design 1710×832) — VisionTargetPage 와 거의 동형, 차이:
- 좌 4행 = [secCam 24 | 카메라 100% | sec검사결과 24 | 결과그리드 150 | **verdict 라벨 40**] (실제 5행).
- 중 ACTION = **2열×3행**, INSPECT(2칸 span, 40%) + GRAB/LOAD(30%) + 이미지저장/EDIT ROI(30%). VisionTargetPage(3×3)와 **구성 불일치**.
- 우 = VisionTargetPage 동일(PARAMETERS+조명240+라이브200).

### 편입 대상 패널 현황
| 패널 | 구성 | 네이티브 Size | 변경이벤트 | 저장 경로 |
|---|---|---|---|---|
| **InspectionLightPanel** | DataGridView(컨트롤러/Ch/이름/Level/Page) + Save/Apply/Reset/Cancel 버튼 + 헤더·결선·상태 라벨 | 761×466 | **없음** | 자체 Save → `AlgorithmCameraMapStore` |
| **LightLiveTuningPanel** | 주기 numeric + 라이브토글/전체ON/전체OFF 버튼 + 상태·카운트 라벨 (그리드 아님) | 244×273 | 없음(읽기 provider) | 저장 없음(런타임 펄스 송신) |
| **ParameterGridControl**(R1) | DataGridView 4컬럼 **PARAMETER/VALUE/UNIT/SCOPE** + `SetItems/RefreshValues` + `ParameterValueChanged` 이벤트 | 640×480 | **있음** | 타깃 finder/inspector JSON |

---

## B. 목표별 진단·제안

### (1) 상태점 ↔ 검사조명 연결 — **편입 가능(조건부)**
- **현재**: InspectionLightPanel 은 자체 Save 버튼으로 `AlgorithmCameraMapStore` 에 직접 저장, **변경 이벤트 미노출** → 타깃 `IsDirty`/`DirtyChanged` 로 전파 안 됨 → 상태점 점등 안 됨.
- **편입 방식(제안)**: InspectionLightPanel(=Vision 파일, 수정 가능)에
  - `public event EventHandler LightChanged;` 추가, `_grid.CurrentCellDirtyStateChanged`/`CellValueChanged` 에서 raise.
  - 타깃 페이지가 구독 → `MarkDirty()` 호출 → 상태점 주황.
- ★**확정 필요(저장소 이원화)**: 조명=`AlgorithmCameraMapStore`(자체 Save), 타깃 ROI=`Config/VisionRecipe/*.json`(finder.SaveParameters). 둘이 별개라 "상단바 저장"으로 dirty 를 **함께 해제**하려면 하나로 통일 필요:
  - **(가) 통합 저장(권장)**: 상단바 "저장(레시피)" = ROI 저장 + 조명 `Save()` 동시 호출. 조명패널 자체 Save/Cancel 버튼 **제거**(Apply=하드웨어 적용은 유지). dirty 단일화.
  - (나) 분리 유지: 조명패널 Save 버튼 유지 + 변경 시 dirty 표시만(저장 시 각각 해제). 버튼 중복 잔존.

### (2) 조명/라이브튜닝 UI 통일 — **데이터 형상 차이로 "테마 통일" 권장**
- ParameterGridControl 은 **고정 속성 리스트**(1행=1파라미터)용. 반면:
  - InspectionLightPanel = **동적 다행 표**(컨트롤러×채널 N행, Page 콤보) → ParameterGridControl 로 1:1 치환 부적합.
  - LightLiveTuningPanel = **버튼/숫자 패널**(그리드 아님).
- **제안**: 리터럴 치환 대신 **공통 시각 테마 적용**(ParameterGridControl 룩으로 통일):
  - 공통 섹션 헤더 바(주황 `StatusBarBg`+`SectionFont`, 이미 타깃들이 사용) → 두 패널 상단에도 동일 적용.
  - DataGridView 스타일 헬퍼: 헤더 배경/폰트(`SectionFont`)·행높이·격자선색·셀폰트(`ValueFont`)를 ParameterGridControl 과 동일하게 맞추는 공용 스타일 함수.
  - 라이브튜닝 주기(numeric) 1행은 원하면 ParameterGridControl Int 행으로 흡수 가능(선택).
  - 버튼 규격(높이/폰트/색)도 타깃 ACTION 과 통일.
- ★확정 필요: **(가) 테마 통일(권장)** vs (나) 라이트 그리드를 ParameterGridControl 로 강제 치환(동적행 재구현 비용 큼).

### (3) ACTION 패널 축소 + 재배치
- 현 버튼 ≈133×**261**px(세로 과대). → **버튼 고정 높이 ≈44px**, 콤팩트 그리드로.
- 축소 후 중앙 컬럼(≈399px 폭)에 큰 빈 공간 발생 → 재배분안:
  - **(가) 중앙=ACTION(상단 콤팩트) + 결과그리드 이동(하단 확대)(권장)**: 좌측의 결과그리드(150h)를 중앙 하단으로 옮겨 크게. 좌측은 **카메라 전용**(더 큼).
  - (나) 중앙 ACTION 축소만 + 남는 공간 그대로(여백) — 단순하나 균형 떨어짐.
  - (다) 컬럼비 재조정: 좌 50%/중 22%/우 380 — 카메라 키우고 ACTION 좁힘.

### (4) 통일성 기준 두 페이지 일관 규격
- 공통 규격(제안): 컬럼비 동일, **ACTION 버튼 높이 44px·`ButtonFont`·동일 색규약**(주요 동작=주황 Accent, 보조=흰색, ROI편집=연노랑), 섹션 헤더 동일, 그리드 테마 동일.
- finder=ACTION[GRAB/MATCH/TRAIN/LOAD/이미지저장/EDIT SEARCH/EDIT TRAIN], inspector=ACTION[INSPECT/GRAB/LOAD/이미지저장/EDIT ROI] — 동작은 다르되 **버튼 규격·배치 패턴·결과그리드 위치·verdict 표기**를 동일 템플릿으로.
- 두 페이지 동일 골격: 좌(카메라) / 중(ACTION 콤팩트 + 결과/verdict) / 우(PARAMETERS + 조명 + 라이브, 통일 테마).

---

## C. 재배치 스케치 (권장안 (가) 조합, 1710×832)

```
┌──────────── 좌 559 ────────────┬──── 중 399 ────┬──── 우 380 ────┐
│ [CAMERA]                        │ [ACTION] (h~44 버튼 콤팩트)      │ [PARAMETERS]    │
│                                 │  GRAB MATCH TRAIN               │  (ParamGrid)    │
│   카메라 뷰 (전체 높이)          │  LOAD 이미지저장 EDIT…          ├─────────────────┤
│                                 ├────────────────┤ [검사 조명]      │
│                                 │ [결과/verdict]  │  (통일 테마 그리드)│
│                                 │  매치/검사 결과 │  + Apply        │
│                                 │  그리드(확대)   ├─────────────────┤
│                                 │  +verdict(insp) │ [라이브 튜닝]    │
│                                 │                 │  (통일 버튼/주기) │
├─────────────────────────────────┴────────────────┴─────────────────┤
│ 상태바                                                               │
└─────────────────────────────────────────────────────────────────────┘
```

## D. 확정 항목 (사용자 결정 필요 — 추측 금지)
1. **(1) 저장 통일**: (가) 상단바 저장이 조명까지 동시 저장(조명 자체 Save 제거) vs (나) 분리 유지.
2. **(2) 조명/라이브 통일 방식**: (가) 시각 테마 통일(권장) vs (나) ParameterGridControl 강제 치환.
3. **(3)/(4) 재배치**: (가) 결과그리드 중앙 이동+카메라 확대(권장) vs (나) ACTION 축소만 vs (다) 컬럼비 재조정.

## E. 게이트/제약
- Handler 무수정(InspectionLightPanel/LightLiveTuningPanel/ParameterGridControl 은 Vision 파일 — 수정 가능).
- TLP 베이스·IC 선언적·ic-helper-call 금지·디자인타임 Size 유지.
- 각 Step 빌드0/정적0/스모크/ verify 코어 FAIL0. 사인오프 후 master `--no-ff` 머지(push 안 함).
```
```
