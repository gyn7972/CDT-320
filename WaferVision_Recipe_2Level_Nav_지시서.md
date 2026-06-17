# CDT-320 WaferVision 레시피 UI — 2레벨 네비게이션 작업 지시서

> 대상: `QMC.Vision` 레시피 탭(`RecipePage`). 우측 기준 2레벨 네비로 재구성하고, WaferVision(Finder 7개)을 범용 에디터에 바인딩한다.
> 원칙: **기존 구조 준수**(Designer/.cs 분리, `FindForm() as Form1`로 모듈 접근, 노드별 폼 금지·범용 1개 재사용). 모션 구동은 핸들러 — Vision은 값 반환만.

---

## 1. 네비게이션 — 2레벨 (우측 기준, 상단 탭 없음)

```
[ 에디터 (카메라 + 파라미터/결과) ]   [ Finder 목록 ]   [ 모듈 레일 ]
            (좌·중앙)                     (우측 1)         (우측 2, 고정)
```

- **모듈 레일(우측 고정)**: WaferVision · BinVision · BottomInspection · TopSideVision · BottomSideVision.
- **모듈 클릭 → 콘텐츠 직접 표시**(상단 탭 스트립 없음, 탭 누적 X). 선택 모듈은 레일에서 활성 표시.
- **콘텐츠 = Finder/Inspector 목록(우측 서브레일) + 선택 시 에디터**.
- **Finder 클릭 → 에디터로 진입**. 즉 `모듈 → Finder → 에디터` 2레벨.
- 콘텐츠 구조는 모듈마다 **노드만 다르고 동일** → **범용 모듈 UserControl 1개**로 Wafer/Bin/Bottom/Top/Bottom 재사용.
- 상단 별도 "저장(레시피)" 버튼 없음 — 저장/로드는 에디터의 **레시피(품목별)** 섹션에서 처리.

> 좌→우 배치를 모두 우측 가중으로: 에디터는 좌·중앙, 내비(Finder 목록·모듈 레일)는 우측. (기존 앱의 우측 사이드바 기준 유지)

---

## 2. 모듈 구조 (확정)

`WaferVisionModule` = **Finder 7개, Inspector 0개**. 각 노드는 `FinderAlgo Setup/Config/Recipe` 동일 타입 → **노드별 폼 금지, 범용 Finder 에디터 1개에 바인딩**.

| Finder | 역할 |
|---|---|
| EjectPin | 이젝트 핀 위치 (핀–다이–콜렛 정렬) |
| Reticle | 기준 마크 / 원점 |
| AlignDie | 정렬 기준 다이 |
| FirstReference | 2점 정렬 기준점 ① (위치만 반환) |
| SecondReference | 2점 정렬 기준점 ② (위치만 반환) |
| Die | 픽업할 개별 다이 위치·각도 |
| Scale | 픽셀↔mm 스케일 / 피치 |

> FirstReference/SecondReference는 **310 `TwoPointAligner` 재구조화** — 노드는 위치만 찾고, θ·offset 계산은 핸들러가 수행.

---

## 3. 범용 Finder 에디터 — 들어갈 것 (레시피: 품목별 + 라이브 액션)

- **ROI**: TRAIN ROI / SEARCH ROI (드래그 편집).
- **파라미터**: MinScore · MaxInstance · AngleTolerance · Accept · Contrast · Algorithm.
- **MATCH 결과**: X · Y · R(각도) · Score · instance 수.
- **검사 조명**: 라이브 레벨/채널.
- **라이브 액션**: GRAB · LOAD · TRAIN · MATCH.
- **레시피**: 저장 · 로드 (품목별).

> 현재 `VisionTargetPage`가 이미 `IVisionModule`+`IPatternFinder` 범용 바인딩이라, `RecipePage`가 `module.Finders`를 나열하면 7개가 자동 표시됨 — 이 에디터를 2레벨 탭 안에서 재사용.

---

## 4. 설정(Settings) — 카메라만

- WaferVision **카메라 IP 매핑**.
- **캘리브레이션 스케일**: mm/px · 축각도 · 반전(Invert).
- **Backend / Provider**.
- **jog · stage · compensator 절대 두지 않음.**

---

## 5. Vision UI에서 제거 / 콜백 (모션은 핸들러)

- **제거**: Jog + Step, Stage Move, Compensator.
  - 활성 에디터(`VisionTargetPage`)엔 이미 없음. Jog 포함 레거시 `FinderPage`/`InspectorPage`(미사용)는 삭제.
- **콜백 2건 (Vision은 값만 반환, 구동은 핸들러)**:
  - **AutoFocus** → Vision은 sharpness 반환 (`IVisionModule.MeasureFocus`).
  - **AutoScale** → Vision은 mm/px 반환 (`IVisionModule.Calibrate`).

---

## 6. 필수 검증 (핸들러 결선)

FirstReference/SecondReference는 **위치만** 찾으므로, **두 점으로 θ·offset 계산하는 정렬 단계가 핸들러 `MachineController`에 정상 연결**돼 있는지 검증한다. 노드만 추가되고 연산이 빠지면 정렬이 안 됨.
- 확인 위치: `QMC.CDT-320/Equipment/MachineController.cs`, `QMC.CDT-320/Equipment/Vision/VisionAdapters.cs` (정렬 오프셋 `PickerAlignOffset.AlignOffsetX/Y/T`).

---

## 7. 진행 순서

1. 범용 **모듈 UserControl** 작성(내용 = Finder/Inspector 목록 + 에디터) → 5개 모듈 재사용.
2. `RecipePage`: 우측 모듈 레일 + 콘텐츠 직접 스왑(상단 탭 없음)으로 재구성. 모듈 클릭 → 해당 모듈 콘텐츠 표시. 상단 "저장(레시피)" 버튼 제거(저장은 품목별 섹션).
3. 탭 안 Finder 목록 → 범용 Finder 에디터(`VisionTargetPage`) 바인딩.
4. 설정 카메라-only 정리 / 레거시 모션 페이지 제거 / AutoFocus·AutoScale 콜백 연결.
5. **2점 정렬 핸들러 결선 검증** (6항).
6. `perl tools/verify_vision_features.pl` 회귀 검증.
