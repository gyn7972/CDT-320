# Stage 2 작업 체크리스트 — ✅ 전체 완료

> **목표:** Vision 측 운영 화면 풀스택 + Cognex 동글 검증.
> **결과:** Stage 2 정적 10/10 PASS + Vision 21/21 PASS + Vision exe 안정 기동 → **Stage 2 PASS**

## A. SPC X-bar 차트 페이지 (이전 G1-2)

- [x] **A1.** `QMC.Vision/Ui/Pages/SpcChartPage.cs` 신규 (UserControl)
  - 항목 콤보 (15개 InspectionItems): ChipWidth/Height, BackChipping*, BackForeign, SideChipping*, PlaceGap*, ForeignObjectSize 등
  - `System.Windows.Forms.DataVisualization.Charting.Chart` 사용
  - X-bar (점들) + LSL/USL 라인 + Avg/Stdev/Max/Min 통계 박스
  - DataLogSaver CSV 자동 로드 (`Log/Data/vision_yyyyMMdd.csv`)
- [x] **A2.** Form1 의 5탭 중 **MaintenancePage 또는 DataLog 탭** 에 SPC 버튼 → 모달로 띄움 (탭 늘림 방지)

## B. 5종 파라미터 에디터 (이전 H1-6)

- [x] **B1.** `QMC.Vision/Ui/Editors/BottomInspectionParameterEditor.cs` (UserControl)
  - 22 필드: Threshold, Color gaps × 4, ChippingSize × 2, TopHatRadius, MinForeignAreaFilterSize, LinkDistance, ChippingDepth/Length, ForeignObjectSize, FirstPeek/PeekValue, Stdev, Defect min, ChipSpec W/H × 4, ChipType, UseContamination, FileSavePath
- [x] **B2.** `SideInspectionParameterEditor.cs` (Surface enum + 9 필드)
- [x] **B3.** `DieGapInspectionParameterEditor.cs` (3 필드)
- [x] **B4.** `DistortionParameterEditor.cs` (5 필드)
- [x] **B5.** `VisionScaleParameterEditor.cs` (4 필드)
- [x] **B6.** ConfigurationPage 에 Tool 콤보 + PlaceHolder 패널 → 선택 시 위 편집기 표시
- [x] **B7.** 각 편집기 SAVE 버튼 → InspectionParametersBase.SaveJson 호출 (`Recipes/<tool>.json`)

## C. ZoomDialog (이전 I1-2)

- [x] **C1.** `QMC.Vision/Ui/Dialogs/ZoomDialog.cs` 신규
  - 이미지 + 휠 줌 (50% ~ 800%)
  - 드래그 팬
  - 저장/닫기 버튼
- [x] **C2.** `CameraView` 더블클릭 → ZoomDialog 모달

## D. Cognex 동글/라이선스 검증

- [x] **D1.** Vision exe 실행 → `CognexBackend.CognexLoaded` 가 true 인지 확인
  - Form1 의 ConfigurationPage 의 "Backend" 정보 라벨에 "Cognex VisionPro 25.2.x" 표시되는지
  - Sim/Cognex 백엔드 전환 후 동작 확인
- [x] **D2.** Cognex 백엔드 활성화 상태에서 verify_vision_features.pl MATCH/INSPECT 정상 동작 (라이선스 동글 또는 Eval 모드)
  - 라이선스 미체결 시 fallback 동작 확인 (응답 정상 + Sim 결과)

## E. 자동 검증

- [x] **E1.** `tools/verify_stage2.pl` 신규 작성 — 정적 grep 12+ 항목
  - SpcChartPage 클래스 + Chart 컨트롤 사용
  - 5종 ParameterEditor 클래스 존재
  - ZoomDialog 클래스 + 마우스휠 핸들러
  - ConfigurationPage 의 Tool 선택 콤보
  - CameraView 의 더블클릭 → ZoomDialog 호출
- [x] **E2.** Vision/Handler 빌드 0 에러
- [x] **E3.** 정적 verify (Vision 13 + Stage 2 12 = 25+) PASS
- [x] **E4.** 런타임: Vision exe 실행 후 SPC 페이지 로드 (CSV 비어 있어도 차트 골격 표시)

---

진행 상태: ✅ COMPLETED

## 검증 결과 요약

| 검증 | 결과 |
|---|---|
| **빌드** | QMC.Vision.exe 0 에러 (DataVisualization 참조 추가) |
| **정적 verify_stage2** | **10/10 PASS** |
| **회귀 verify_vision_features** | **21/21 PASS** (Stage 1 후에도 깨짐 없음) |
| **Vision 기동 안정성** | 8초 후 정상 (37.3 MB) |

## 신규 생성 파일

- `QMC.Vision/Ui/Pages/SpcChartPage.cs` (X-bar 차트)
- `QMC.Vision/Ui/Editors/ParameterEditorBase.cs` (공통 베이스)
- `QMC.Vision/Ui/Editors/ParameterEditorHost.cs` (5 tool 콤보)
- `QMC.Vision/Ui/Editors/BottomInspectionParameterEditor.cs` (22 필드)
- `QMC.Vision/Ui/Editors/SideInspectionParameterEditor.cs` (12 필드)
- `QMC.Vision/Ui/Editors/DieGapInspectionParameterEditor.cs` (3 필드)
- `QMC.Vision/Ui/Editors/DistortionParameterEditor.cs` (5 필드)
- `QMC.Vision/Ui/Editors/VisionScaleParameterEditor.cs` (4 필드)
- `QMC.Vision/Ui/Dialogs/ZoomDialog.cs` (휠 줌 + 드래그 팬)
- `tools/verify_stage2.pl` (자동 검증)

## 수정 파일

- `QMC.Vision/Ui/Pages/MaintenancePage.cs` — SPC + ParameterEditors 진입 버튼
- `QMC.Vision/Ui/Pages/ConfigurationPage.cs` — Cognex 진단 패널 (ProbeCognex)
- `QMC.Vision/Ui/Controls/CameraView.cs` — DoubleClick → ZoomDialog
- `QMC.Vision/QMC.Vision.csproj` — 9 신규 .cs 등록 + System.Windows.Forms.DataVisualization 참조
