# Side 칩핑·이물 검사 알고리즘 검토 보고서 (2026-07-02)

대상: FrontSideVision/RearSideVision 칩핑·표면(이물) 경로 전체.
방법: 코드 정독 + CDT-310 원본 대조 + testimg 6종 실측(알고리즘 등가 Python 재현).

## 경로 구조

```
SideAppearanceInspector.Inspect (오케스트레이터, ROI/밴드/판정/오버레이)
 ├─ 칩핑: UseInspectorLib=true(기본)
 │    ├─ [활성] VI.SideChippingInspector (VisionInspector — 310 lib 포팅)
 │    └─ [폴백] SideChippingCore (레거시 — lib 예외/IsSuccess=false일 때만)
 └─ 표면(이물): ContaminationDetector (Black-Hat, Bottom과 공유) — 항상 이 경로
```

## 발견사항 (심각도순)

### F1. [치명] ✅ 수정 적용(2026-07-02, C:\Project) lib 칩핑 `chippingMargin = 0` — 측정 무력화
`VisionInspector\SideChippingInspector.cs:68-69`
```csharp
int chippingMargin = CalculateChippingMargin(parameter.ChippingDepth);
chippingMargin = 0;   // ← 계산값을 버림
```
Top 스캔 루프가 `y < topY+0`으로 0회 실행 → Top 항상 0. Bottom도 사실상 무력.
IsSuccess=true라 레거시 폴백도 안 탐 → 화면에 0.0000 고정.
- **310 대조: 310 원본(CDT-310_1)에도 같은 줄 존재** — 320 포팅이 만든 버그가 아니라 310 결함까지 충실 포팅됨. "업데이트 전엔 잘 됐던" 이유는 업데이트 전 활성 경로가 레거시 SideChippingCore(마진 정상 계산: `max(8, 하단라인-상단라인)`)였기 때문.
- **주의: 69행 단순 삭제로는 부족.** margin이 CalculateChippingMargin(Upper)로 스펙에서 유도되어
  Upper보다 깊은 칩핑은 '밝음 복귀'를 margin 안에서 못 찾아 **아예 미검출**(스펙 초과일수록 안 보이는 역설).
  실측: margin=64px에서 노치 0.19mm 검출 / 0.22mm 미검출.
- 수정: margin = 칩 두께 기반 `(int)(parameter.ChipThickness / PixelSizeHeightMm)` ≈ 78px
  (레거시 SideChippingCore와 동일 방식 — 업데이트 전 정상 동작의 근거).
- 검증: rough_ch1 Top 0 → **0.1031mm** (에지 실측 0.1047mm).

### F2. [중대] ✅ 수정 적용(2026-07-02) — lib 스캔 임계값 150 하드코딩 — 310에서 이탈
`SideChippingInspector.cs:71-76` — 310 원본은 `parameter.Threshold`를 넘기는데 320 포팅은 150 하드코딩. UI Chip Threshold(200)가 스캔에 미반영.
- 수정: `parameter.Threshold`로 환원(=310 회귀).

### F3. [중대] ✅ 수정 적용(2026-07-02) — 표면(이물) 툴이 에지 칩핑을 이물로 오검
실측: rough_ch1에서 이물 blob 3~4개 오검(전부 y≈311 = 상단 에지 노치).
원인: 310 side 이물(InspectForeign)은 칩 외곽선 검출 후 **MaskImage로 내부만 검사**하는데,
320 side 표면은 마스크 없이 ROI 전체에 Black-Hat 적용 → 에지 파임이 '어두운 구멍'으로 잡힘.
- 대책(택1): ① 표면 ROI를 띠 안쪽으로 좁힘(에지에서 상하 ~15px 여유)
  ② 코드 보강: 밴드 마스크 + ErodeSquare(Bottom 방식)로 에지 제외 — 코어 밖 마스킹이라 310 원칙과 충돌 없음.
- 참고: 띠 기울기 오검은 1°까지 없음(Closing 특성상 안전). 순수 이물(foreign_on_chip)은 blob 1개 정확 검출.

### F4. [중대] ✅ 수정 적용(2026-07-02) — AlgorithmNode side 레시피 매핑 가드 누락
`AlgorithmNode.cs:263-265` — Bottom은 `Max>0`, `Link>0` 가드가 있는데 Side는 무가드 대입.
저장된 레시피에 0이 있으면 레시피 적용 시마다 Max=0 → 모든 blob 탈락(검출 전멸).
- 수정: Bottom(238-239행)과 동일 가드 추가.

### F5. [주의] ✅ 수정 적용(2026-07-02) — Lower Limit 판정 의미 함정
`chipPass = (max <= Upper) && (max >= Lower)`, 기본 Lower = **-0.025**(노이즈 허용용 음수).
UI에서 Lower에 0.3 같은 양수를 넣으면 깨끗한 칩(0.000)이 전부 NG. Upper<Lower 입력도 차단 안 됨.
- 권장: Lower 기본 -0.025 유지. UI에 Upper≥Lower 검증 또는 툴팁 추가.

### F6. [경미] 310 대비 잔여 차이
- Bottom 스캔 조기중단: 310 `nScanDepth>10` vs 320 lib `>100` (완화 — 동작상 무해).
- ConvertPixelToMM이 config 픽셀사이즈를 0.003125로 덮어씀(310도 동일한 코드 냄새).
- 레시피의 PixelSize가 side에는 Width만(그것도 Bottom 필드 재사용), Height 미적용.
- IsDefect NG 시 d:\Log\Image\Side 하드코딩 경로 저장(310 유래) — 결과저장 설정과 무관하게 동작.

### F7. [정보] 의도된 320 보강 (문서화 확인됨)
- 표면 effRadius = min(TopHatRadius, 밴드두께/3) 클램프 — 얇은 띠 에지 오검 방지.
- 레거시 코어 EdgeGap(에지결합 허용 px), ScanRate/EnvelopeBin/KeepQuantile 레시피 노출.
- 320 side 표면이 310 단순임계 대신 Black-Hat 채택 — 조명 구배에 강한 상위 방식이나 F3 마스킹 보완 필요.


## F6. [기능개선] ✅ 적용(2026-07-02) — 다중 칩핑 검출 + 실측 bbox 마커

기존: 최대 깊이 1곳에 고정 48x48 박스 → (1) 실제 칩핑 윤곽과 무관한 마커 (2) 칩핑 2개여도 1개만 표시.
변경: 스펙(Upper) 초과 컬럼을 x-연속(간격≤5px) 그룹으로 묶어 영역별 실측 bbox 마커 + "Chipping Count" 결과 항목 추가.
- lib: SideChippingInspector.CollectChippingRegions → result.ChippingRegions 채움(기존 빈 리스트였음),
  ChippingRegion 에 XStart/XEnd 추가. 오케스트레이터가 영역 폭/높이 그대로 마커(C1, C2...).
- 레거시: SideChippingCore 에 RegionMark/Params.SpecMm 동일 구현(폴백 경로 일관성).
- Top/Bottom/Max 수치 계산은 미변경(310 로직 유지) — 마커/카운트만 추가.
- 더블 노치 검증: 노치 2개(55px/70px) → 영역 2개, bbox 실제 노치와 일치(W127/H49, W127/H64), Max 0.1969mm.

## 실측 검증표 (testimg, 수정 가정 시)

| 이미지 | 칩핑 Top | 칩핑 Bottom | 이물 blob |
|---|---|---|---|
| chip_clean | 0.0031 | 0.0000 | 0 |
| foreign_on_chip | 0.0031 | 0.0000 | **1 (면적225, x2000,y351)** |
| rough_ch1 | **0.1031** | 0.0094 | 0 (F3 적용: 에지오검 4→0) |
| rough_ch2 | **0.0875** | 0.0063 | — |
| rough_ok_ch1 | 0.0031 | 0.0031 | 0 |
| rough_ok_ch2 | 0.0063 | 0.0031 | 0 |

## 권장 파라메터

칩핑: Chip Threshold 200, Chip Thickness 0.24414, **Upper 0.05** (rough NG / rough_ok PASS 분리; 현재 0.2면 rough도 PASS), **Lower -0.025**.
표면: Chip Threshold 150, TopHat Radius 15, Threshold 30, Min 36, Max 100000, Link 25, ROI는 띠 상하 에지에서 15px 안쪽.

## 수정 적용 현황 (F1~F5 전부 적용 완료, C:\Project — 빌드 후 재검증 필요)

1. F1 한 줄 삭제 (즉시)  2. F4 가드 (즉시)  3. F2 임계 환원  4. F3 마스킹 or ROI 운영지침  5. F5 UI 검증

※ 수정은 V:\Source 기준 저장소에 적용해야 함(현 세션 미연결).
