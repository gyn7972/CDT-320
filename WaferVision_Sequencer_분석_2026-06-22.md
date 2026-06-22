# CDT-320 WaferVision 시퀀서 분석 (BinVision 완료분 대비)

> 작성: 2026-06-22 · 대상: `QMC.Vision` 시퀀서 + `QMC.CDT-320` 핸들러 2점 정렬 결선
> 목적: BinVision 수정 완료분과 대응하여 **WaferVision 시퀀서 구현 전 현재 상태/갭**을 4개 범위로 분석한다.
> 원칙(AGENTS.md): 공유 코어 단일 구현, Vision은 값 반환·모션은 핸들러, mojibake 한글 복원, UTF-8.

---

## 0. 구조 요약 (공통)

시퀀서는 **2계층 + 공유 코어**로 구성된다.

```
[UI] SequencerPage  →  VisionAutoSequenceHost
                          ├─ AutoSequenceCoordinator (도구 단위: ToolSequence)   ← 현재 UI가 사용
                          └─ ModuleSequenceBase 파생 (모듈 단위: WaferVisionSequence 등)
                                     │
                       순서 SSOT = SequenceToolCatalog
                                     │
                       명령 실행 = VisionSequenceContext.Dispatch
                                     │
   ┌─────────────────────────────────┴─────────────────────────────────┐
   DirectVisionCommandDispatcher(시뮬 자체구동)        VisionTcpServer(핸들러 TCP 구동)
   └──────────────── 둘 다 동일 구현 공유 → VisionCommandCore ────────────┘
                       (GRAB / MATCH / INSPECT / TRAIN, mm변환·이미지로그·자재추적)
```

- **시퀀스 정의는 이미 존재**: `WaferVisionSequence`, `SequenceToolCatalog[WaferVision]` 모두 7개 finder 순서가 정의됨.
- **UI·코디네이터·메트릭은 범용**: `SequencerPage`/`AutoSequenceCoordinator`가 WaferVision을 이미 포함 → 별도 스캐폴딩 불필요.
- 따라서 WaferVision "수정"은 BinVision처럼 시퀀스 추가가 아니라, **알고리즘 동작·핸들러 결선·캘리브레이션의 실제화**가 핵심이다.

### BinVision 완료분에서 들어간 것 (참고 기준선)
- `BinVisionSequence` (GRAB→Reticle→Die→**PlacementInspector**→Scale)
- `PlacementGapInspector.cs` (462줄) — 실제 안착 갭 검사 알고리즘
- `AlignAngleEstimator.cs` — 격자 평균각(θ) 추정 (Sobel + 4θ 위상)
- `VisionCommandCore`에 `MaterialTracker.ApplyDieGap` 결선 (Placement/Bin/DieGap)

→ Bin의 핵심은 **Inspector(검사 알고리즘)**. **WaferVision은 Inspector 0개, Finder 7개**라 대응 작업의 성격이 다르다 (검사가 아니라 **2점 정렬 결선·좌표 캘리브레이션**이 핵심).

---

## 1. 2점 정렬 결선 검증·구현  ★ 최우선 갭

### 1-A. 핸들러 측 — 결선됨 (정상)
`QMC.CDT-320/Sequencing/InputStage/InputStageAlignSequence.cs` 가 완전한 상태머신으로 2점 정렬을 수행한다.

| 단계 | 동작 |
|---|---|
| MoveCenterMark → RequestCenterMark | AlignDie(Center) 매칭 |
| CorrectTheta → ThetaVerify (수렴 루프) | `StageT += DeltaTheta`, 허용오차 내 수렴까지 반복 |
| MoveRef1 → RequestRef1Mark | `_ref1X = CameraX + DeltaX`, `_ref1Y = StageY + DeltaY` |
| MoveRef2 → RequestRef2Mark | `_ref2X`, `_ref2Y` 동일 |
| **CalculateAlignResult** | pitch·origin·**θ** 계산 (아래) |
| ApplyAlignResult | `Stage.ApplyWaferAlignResult(origin, pitch, offset)` + 자재 저장 |

핵심 연산 (`CalculateAlignResult`, line 630–651):
```csharp
pitchX = (ref2X - ref1X) / (Ref2Col - Ref1Col);
pitchY = (ref2Y - ref1Y) / (Ref2Row - Ref1Row);
originX = ref1X - Ref1Col * pitchX;
originY = ref1Y - Ref1Row * pitchY;
θ = Math.Atan2(ref2Y - ref1Y, ref2X - ref1X) * 180/π;   // ← 2점 θ 계산 존재·정상
```
→ **지시서 6항이 우려한 "노드만 추가되고 연산이 빠지는" 문제는 핸들러에는 없다.** θ·pitch·origin 모두 두 기준점으로 계산되어 자재(`MaterialStateService.SaveInputStageAlignResult`)에 저장된다.

### 1-B. Vision 측 어댑터 — 미완 (실 캘리브레이션 미결선) ★
`QMC.CDT-320/Equipment/Vision/VisionAdapters.cs` 의 `WaferVisionAdapter`:

```csharp
private const double ImageCenterX = 320.0;   // TODO: 실제 해상도/원점
private const double ImageCenterY = 240.0;
private const double PixelToMm    = 0.001;    // TODO: SCALE 캘리브레이션 값
private const double DiePitchMm   = 0.15;     // TODO: 레퍼런스 마크 실측 피치

DeltaX = (r.X - ImageCenterX) * PixelToMm;    // 임시 환산
DeltaY = (r.Y - ImageCenterY) * PixelToMm;
DeltaTheta = r.AngleDeg;
```

**문제점**
1. **하드코딩 임시 상수** — 해상도(320/240)·스케일(0.001)·피치(0.15)가 실제 장비/레시피와 무관한 placeholder. 그대로면 mm 오프셋이 부정확 → 정렬 실패.
2. **단위 충돌 소지** — Vision 측 `VisionCommandCore.Match`는 이미 `ExportCameraMapping()`(모듈 CameraConfig가 SSOT)로 mm 변환(`ReturnMmCoordinates`, ScaleX/Y, Invert)을 수행해 `OK;x=..;y=..` 를 반환할 수 있다. 어댑터가 받은 `r.X/r.Y`가 **이미 mm일 경우 `(r.X-320)*0.001` 재환산은 이중 변환·오류**. px일 경우에만 의미. → 어느 단위로 오는지 확정하고 한 경로로 통일해야 함.
3. **mojibake** — 파일 전체 한글 주석 깨짐(`Ǳ��`). AGENTS.md에 따라 작업 범위 내 복원.

**구현 방향(권고)**
- 어댑터의 px 환산 제거 → Vision이 모듈 CameraConfig 기반 **mm 좌표를 직접 반환**하도록 통일(코어가 이미 지원). 어댑터는 받은 mm를 그대로 `DeltaX/DeltaY`로 전달.
- `PitchX/PitchY`는 핸들러가 Ref1/Ref2 간격으로 산출하므로(1-A), 어댑터의 `DiePitchMm` 상수는 fallback 외 제거 가능.
- SCALE(캘리브레이션 mm/px·축각도·Invert)은 `CameraMappingPanel`/모듈 CameraConfig에서 단일 관리.

---

## 2. 웨이퍼 Finder 알고리즘 동작

`WaferVisionModule` = Finder 7개 (Inspector 0). 전부 동일 `FinderAlgo Setup/Config/Recipe` → 범용 `VisionCommandCore.Match` 경로.

| Finder | 역할 | 반환 | 각도(θ) 사용 |
|---|---|---|---|
| EjectPinFinder | 이젝트핀 위치 | x,y | 불필요 |
| ReticleFinder | 기준 마크/원점 | x,y | 불필요 |
| FirstReferenceFinder | 2점 정렬 ① | **위치만** | 핸들러가 계산 |
| SecondReferenceFinder | 2점 정렬 ② | **위치만** | 핸들러가 계산 |
| AlignDieFinder | 정렬 기준 다이(Center) | x,y,r | θ 보정에 사용 |
| DieFinder | 픽업 다이 위치·각도 | x,y,r,score | 다이 각도 |
| ScaleFinder | px↔mm 스케일/피치 | x,y | 불필요 |

**현 상태**: `VisionCommandCore.Match`가 `AngleMode == AverageAll`이면 `AlignAngleEstimator`로 격자 평균각을 r에 대입(line 51–56). 단일 매칭이면 `b.AngleDeg`.

**확인/보정 필요 갭**
- **AlignDie/Die의 AngleMode** — 다이 격자 평균각이 필요한 노드(AlignDie, 경우에 따라 Die)에 `FinderAlgoRecipe.AngleMode = AverageAll`이 **레시피에 실제로 설정**돼 있는지 검증. 미설정이면 단일 매칭 각도라 정렬각이 흔들릴 수 있음.
- **EjectPin/Reticle/Scale** — 위치만 필요하므로 추가 작업 불필요(현행 유지).
- **FirstReference/SecondReference** — 위치만 반환이 설계상 정확. θ는 핸들러가 산출(1-A). 어댑터에서 `DeltaTheta=r.AngleDeg`로 개별 각도를 채우는 것은 핸들러가 무시하므로 무해하나 혼동 방지로 0 또는 미사용 권고.

---

## 3. Sim 자체구동(chipUid) 경로

- 설정: `VisionConfig.SimEmitChipUid`(기본 **false**), `SimSequenceIntervalMs`(500).
- `ModuleSequenceBase.ResolveCycleChipUid()` / `ToolSequence.ResolveChipUid()` 가 true일 때 `"SIM-<name>-<ts>"` 합성 발급 → 실제(핸들러) 흐름과 동일하게 코어에 chipUid 전달.

**경로별 동작**
- chipUid 있음 + MATCH → `ImageLogSaver.Save` (이미지로그) 수행.
- chipUid 있음 + INSPECT → `MaterialTracker.Apply*` + 데이터로그. **단, WaferVision은 Inspector가 없어 이 경로를 타지 않음.**

**갭/판단 필요**
- **웨이퍼 Sim 자체구동은 finder MATCH + 이미지로그까지만** 수행. Bin의 `ApplyDieGap` 같은 자재추적 누적이 웨이퍼엔 없음(Inspector 부재 → 자연스러운 결과).
- 실제 흐름에서 웨이퍼의 "결과"는 검사 PASS/FAIL이 아니라 **정렬 결과(origin/pitch/θ)** 이고, 이는 핸들러 `SaveInputStageAlignResult`가 자재에 저장한다. → Sim 단독구동(핸들러 없음)에서는 **정렬 결과 자재 반영이 빠짐**.
- 결정 필요: (a) 현행 유지(Sim은 finder 측정·이미지로그까지만) vs (b) Sim 자체구동에 2점 정렬 산출(Atan2)을 Vision 측 공유 Core로 끌어와 chipUid 자재에 정렬결과 누적. 메모리의 "시뮬=Coordinator 공유 Core 구동" 방향과 정합하려면 (b)가 일관적이나 범위 확대.

---

## 4. 시퀀스 스텝 순서 정합성

`WaferVisionSequence.cs` ≡ `SequenceToolCatalog[WaferVision]` — **두 정의 일치**:
```
EjectPin → Reticle → FirstReference → SecondReference → AlignDie → Die → Scale
```

**핸들러 실제 정렬 순서(InputStageAlignSequence)와 비교**:
```
AlignDie(Center) → θ보정 → (검증) → Ref1 → Ref2 → 계산
```
→ **불일치 1건**: 카탈로그는 `FirstReference/SecondReference`를 `AlignDie`보다 **앞**에 두지만, 실제 정렬은 `AlignDie(Center)`로 θ를 먼저 보정한 뒤 Ref1/Ref2를 찍는다.

**영향/판단**
- Vision 시퀀서(`SequencerPage`)는 **모션 없는 측정/연속 테스트용**이라 순서가 결과값에 영향 없음(각 finder 독립 MATCH).
- 다만 "CDT-310 실제 흐름과 일치"라는 SSOT 취지상, 카탈로그 순서를 실 정렬 흐름(`AlignDie(Center) → Ref1 → Ref2 → Die → Scale`, EjectPin/Reticle은 선두 보조)으로 **정렬할지 검토 권고**. 변경 시 `SequenceToolCatalog` 한 곳만 수정하면 시퀀스/UI/메트릭 전부 반영됨.

---

## 5. 결론 — 구현 작업 목록(제안)

| # | 범위 | 작업 | 파일 | 난이도 |
|---|---|---|---|---|
| 1 | 2점 정렬 | `WaferVisionAdapter` 하드코딩 캘리브레이션 제거 → mm 좌표 단일 경로 통일 | `QMC.CDT-320/Equipment/Vision/VisionAdapters.cs` | 중 |
| 2 | 2점 정렬 | 어댑터 mojibake 한글 주석 복원(UTF-8) | 〃 | 하 |
| 3 | finder | AlignDie/Die `AngleMode=AverageAll` 레시피 설정 검증·보정 | WaferVision 레시피/`VisionCommandCore` | 중 |
| 4 | 스텝 순서 | 카탈로그 순서를 실 정렬 흐름과 일치화(선택) | `SequenceToolCatalog.cs` | 하 |
| 5 | Sim 경로 | Sim 자체구동 정렬결과 누적 여부 결정(현행 유지/Core 이관) | `VisionCommandCore`/Sim Core | 상(선택) |
| 6 | 검증 | `perl tools/verify_vision_features.pl` 회귀 | tools/ | — |

**핸들러 측 2점 정렬 핵심 연산(θ/pitch/origin)은 이미 정상 결선**되어 있어, 실질 작업은 **Vision 어댑터 캘리브레이션 실제화(1·2)** + **각도모드 검증(3)** 이 우선순위다. 4·5는 정합성/일관성 차원의 선택 항목.

> 다음 단계: 위 1–4를 구현 범위로 확정하면 코드 수정 진행. 5는 별도 결정 필요.

---

## 6. 확정 설계 (2026-06-22 협의 반영) ★ 본 절이 구현 기준

> 협의 결과, 1·5장의 "핸들러가 θ 계산" 전제를 **"Vision PC가 비전 좌표로 θ 계산"** 으로 변경한다. 아래가 최종 구현 스펙이다.

### 6-0. 핵심 모델 (요청-수행-저장)
핸들러가 TCP로 **도구 단위 요청**(`MODULE|MATCH|<도구>|<idx>`)을 보내면, 모듈의 해당 **도구(Tool = Finder 위치검출 / Inspector 양·불)** 가 실행되고 **결과를 캐시(LastResult)에 보관**한다. 도구 종류와 무관하게 동일.

용어: **모듈(Module)** ⊃ **도구(Tool)** = **Finder**(x/y/θ/score) | **Inspector**(PASS/FAIL). 캐시 = **검출 결과(LastResult)**.

### 6-1. 2점 정렬 — Vision 측 계산 (스테이지 피드백 불요)
전제: Ref1·Ref2는 **같은 행(Row)**, 그 사이 스테이지는 **X로만 D 이동, Y=0**. (다이맵 기본 기준쌍과 부합)

- Vision은 각 기준의 **카메라 센터 기준 화면내 오프셋(mm)** 을 캐시: 1차 `(x₁,y₁)`.
- 2차 요청 시:
  ```
  θ = atan2( y₂ − y₁,  D + (x₂ − x₁) )      // D = 명목 Ref1→Ref2 X거리(상수)
  ```
- 반환: `OK;x=..;y=..;r=..;score=..;align_t=<θ>` (2차 검출값 + 정렬데이터 동봉).
- **D 출처**: Vision 레시피 상수(예 `RefSpanXmm`). 미설정 시 `pitchX × (Ref2Col−Ref1Col)` fallback. ← *최종 확인 필요(소)*
- 핸들러: RX 파서에 `align_t` 추가, 적용만(Atan2 자체계산은 fallback로 잔존 가능).
- **검증 전제**: 두 기준이 같은 행 + X-only 이동인지 실제 설비와 일치 확인.

### 6-2. 모든 도구 결과 캐시
- `ModuleResultStore`를 **Finder 포함**으로 확장: `(모듈,도구Id)` → `{cmd, ok/ng, x/y/r/score 또는 items, time}`.
- 기록 지점은 **공유 코어 `VisionCommandCore.Match`/`Inspect` 한 곳** → 핸들러TCP·시퀀서·Sim 전부 자동 반영.

### 6-3. 결과 표시 UI — 양쪽 모두
- **`OperationPage`(작업 모니터링)**: 모듈별 최신 검출결과 그리드 추가(이미 store 소비 중).
- **`SequencerPage`**: 기존 도구 그리드가 **같은 store 재사용** → 운영/테스트 일관.

### 6-4. 얼라인 = 비전 센터 최근접 매칭
- `VisionCommandCore.Match`: `r.Best`(최고 score) → **이미지 센터 최근접 인스턴스**(`Instances` 거리 최소) 선택. score는 `AcceptThreshold` 게이트 유지.
- "모든 얼라인 검증"에 적용(얼라인 계열 finder 기본 동작).

### 6-5. 파일별 변경 (예정)
| 영역 | 파일 | 변경 |
|---|---|---|
| 캐시 | `QMC.Vision/Equipment/Core/ModuleResultStore.cs` | Finder 결과 포함 통합 캐시로 확장 |
| 코어 | `QMC.Vision/Equipment/Core/VisionCommandCore.cs` | ① 센터 최근접 선택 ② 2점 θ 계산·`align_t` 반환 ③ 캐시 기록 |
| 코어 | (신규) 2점 정렬 계산 헬퍼 | `θ = atan2(Δy, D+Δx)` |
| 레시피 | WaferVision 레시피/설정 | `RefSpanXmm`(D), AlignDie `AngleMode` |
| UI | `QMC.Vision/Ui/Pages/Work/OperationPage.cs` | 최신 검출결과 그리드 |
| UI | `QMC.Vision/Ui/Pages/Work/SequencerPage.cs` | 동일 store 재사용 |
| 핸들러 | `QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs` | `MatchResultDto`에 `align_t` 파싱 |
| 핸들러 | `QMC.CDT-320/Equipment/Vision/VisionAdapters.cs` | mojibake 복원 + 하드코딩 캘리브 제거(mm 단일경로) |
| 검증 | `tools/verify_vision_features.pl` | 회귀 |

