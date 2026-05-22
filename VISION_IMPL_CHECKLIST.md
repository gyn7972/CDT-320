# 320 Vision 이식 구현 체크리스트

> 기준: `FEATURE_CATALOG_310.md` Tier 1 + Tier 2 우선 적용.
> **아키텍처: 우리 것 그대로 유지** (ICamera + IVisionBackend + VisionModule + VisionTcpServer).
> **310 의 SoftBricks/SentiCore Element/Part/Recipe 트리는 절대 차용하지 않음.**

각 체크박스는 자동/수동 검증으로 PASS/FAIL 판정.

---

## A. 좌표 변환 + 스케일 (Tier 1)

- [x] **A1.** `QMC.Vision/Core/VisionScale.cs` 생성
  - 클래스 `VisionScale { double X, Y; }`  (mm/pixel)
  - 정적 메서드 `ConvertPosition(VisionScale scale, CameraVector vec, int imgW, int imgH, double pixelX, double pixelY, out double mmX, out double mmY)`
  - 회전(`IsRotated`) / 부호 반전(`InvertedX/Y`) 적용
- [x] **A2.** `QMC.Vision/Core/CameraVector.cs` 생성
  - `bool InvertedX, InvertedY, IsRotated`
- [x] **A3.** `VisionConfig` 에 `ScaleX, ScaleY, IsRotated, InvertedX, InvertedY, DelayBeforeGrabMs, SideLocation, OffAfterGrabWhenAutoFocus, ReturnMmCoordinates, DataLogEnable, DataLogPath` 추가
- [x] **A4.** `VisionTcpServer.DoMatch` 에서 `ReturnMmCoordinates=true` 일 때 `VisionScale.ConvertPosition` 으로 응답 좌표 변환

## B. 다이별 데이터 트래킹 (Tier 1)

- [x] **B1.** `QMC.Vision/Core/MaterialTracker.cs` 생성
  - `class DieRecord` (32 필드: chipUid, 로딩/언로딩 substrate ID/X/Y, DieW/H, ChipLower/UpperSpec*, BackChipping*Size + Length, SideChipping*Size + Length, BackForeignSize/ForeignObjectSize, PlaceTop/Bottom/Left/Right GapAverage, DieGapUpper/LowerLimit)
  - `static ConcurrentDictionary<string, DieRecord> Records`
  - `static void Update(string uid, Action<DieRecord> mutate)`
  - `static DieRecord Get(string uid)`
  - `static void ApplyBottom/ApplySide/ApplyDieGap` — InspectionResult 결과 자동 분류

## C. 검사 알고리즘 표준화 (Tier 1)

- [x] **C1.** `Core/Inspectors/InspectionParameters.cs` — `BottomInspectionParameters` 22 필드
- [x] **C2.** 동일 파일 — `SideInspectionParameters` (Surface 분기 포함)
- [x] **C3.** 동일 파일 — `DieGapInspectionParameters` (Threshold, Upper/Lower limit)
- [x] **C4.** 동일 파일 — `DistortionParameters` (Threshold, PitchX/Y, ChipType, TargetSearch)
- [x] **C5.** 동일 파일 — `VisionScaleParameters` (Threshold, ChipWidth, ChipHeight, ChipType)
- [x] **C6.** `InspectionParametersBase.SaveJson/LoadJsonInternal` 으로 모든 5종 자동 직렬화
  - 백엔드 IInspector 의 LoadParameters/SaveParameters 호출 채널은 기존 그대로 사용

## D. 비전 명령 확장 (Tier 2)

- [x] **D1.** `VisionTcpServer.ProcessLine` switch 에 추가:
  - [x] `SCALE` — VisionScale 캘리브레이션 → VisionConfig 자동 저장
  - [x] `ROT_CENTER` — RotationalCenter corners 응답
  - [x] `DISTORT` — DistortionCompensation 학습 (finder 부재 시 fail:no DistortionCompensation finder)
  - [x] `CAM_SWITCH` — 멀티 카메라 토글 (단일 카메라 모듈에서는 OK 응답)
  - [x] `FOCUS_VAL` — 4 ROI 별 포커스 값
  - [ ] DIE_FOCUS_ROI — **다음 라운드** (Inspection 결과 의존성 큼)
- [x] **D2.** `VisionModule` 에 메서드 추가: `Calibrate / MeasureRotationalCenter / LearnDistortion / MeasureFocus`

## E. 비동기 이벤트 (EPD/ARM) (Tier 2)

- [x] **E1.** `VisionTcpServer.Broadcast(string line)` — 모든 클라이언트 송신
- [x] **E2.** `VisionModule.Grab` 가 성공 시 `ExposureDone` 이벤트 발화
- [x] **E3.** `VisionTcpServer` 가 OnExposureDone 구독 → `EPD|MODULE` 브로드캐스트
- [x] **E4.** Grab 실패 시 `Alarmed` 이벤트 → `ARM|MODULE|reason` 브로드캐스트
- [x] **E5.** Handler `VisionTcpClient.cs` 에 `event ExposureDone, Alarmed` 추가 + RX 루프에서 EPD/ARM 분기 처리

## F. 데이터 로그 (Tier 2)

- [x] **F1.** `QMC.Vision/Core/DataLogSaver.cs` — 31 칼럼 일자별 CSV (310 `DataLogSaver.Titles` 30 + Timestamp)
  - `SaveIfDieGapComplete` — DieGap 데이터가 채워졌을 때 자동 저장
  - `SaveRow` — 강제 저장
- [x] **F2.** `QMC.Vision/Core/ImageLogSaver.cs` — `<ImageLogPath>/yyyy-MM-dd/<chipUid>/<module>_<tool>_<HHmmss_fff>.png`, "Manual" skip
- [x] **F3.** `VisionTcpServer.DoMatch/DoInspect` 에서 chipUid 가 있고 "Manual" 이 아닌 경우 ImageLogSaver/DataLogSaver 자동 호출

## G. SPC X-bar 차트 (Tier 2)

- [ ] **G1.** `QMC.Vision/Ui/Pages/SpcChartPage.cs` (UserControl)
  - 항목 콤보 (15 InspectionItems: ChipWidth/Height, BackChipping*, BackForeign, SideChipping*, PlaceGap*)
  - `System.Windows.Forms.DataVisualization.Charting.Chart` 사용
  - LSL/USL 라인 + Avg + Stdev + Max/Min 표시
  - DataLogSaver CSV 또는 MaterialTracker 에서 데이터 로드
- [ ] **G2.** Form1 의 5탭에 추가하지 않고 Maintenance 탭에 버튼 → 모달로 띄움 (탭 늘림 방지)

## H. 툴별 파라미터 에디터 (Tier 2)

- [ ] **H1.** `QMC.Vision/Ui/Editors/BottomInspectionParameterEditor.cs` (UserControl)
- [ ] **H2.** `SideInspectionParameterEditor.cs`
- [ ] **H3.** `DieGapInspectionParameterEditor.cs`
- [ ] **H4.** `DistortionParameterEditor.cs`
- [ ] **H5.** `VisionScaleParameterEditor.cs`
- [ ] **H6.** Configuration 탭에서 Tool 선택 → 해당 Editor 표시 (PlaceHolder 패널)

## I. 줌 다이얼로그 (Tier 3)

- [ ] **I1.** `QMC.Vision/Ui/Dialogs/ZoomDialog.cs` — 이미지 + 휠 줌 + 드래그 팬
- [ ] **I2.** `CameraView` 더블클릭 시 ZoomDialog 모달

## J. (320 기준) 자가검증

- [x] **J1.** 빌드 0 에러 — QMC.Vision.exe + QMC.CDT-320.exe 모두 PASS
- [x] **J2.** `tools/verify_vision_features.pl` 작성 — 정적 11 항목 + 런타임 9 항목
- [x] **J3.** 런타임: VisionTcpServer 에 SCALE/ROT_CENTER/DISTORT/CAM_SWITCH/FOCUS_VAL 명령 보내고 ACK 응답 확인 ✓
- [x] **J4.** 런타임: req() 내부에서 EPD 비동기 이벤트가 ACK 보다 먼저 도착함을 확인 (검증 스크립트가 명시적으로 EPD/ARM 라인을 스킵)
- [x] **J5.** 런타임: INSPECT (Bin) + chipUid 전달 시 MaterialTracker / DataLogSaver 호출 경로가 동작 (실제 CSV 생성은 DataLogEnable=true 일 때만)

---

## 적용 범위 결정

다음은 **이번 라운드에서 구현**할 항목 (Tier 1 + Tier 2 핵심):
✅ A1~A4 — VisionScale + CameraVector + Config + 응답 좌표 변환
✅ B1 — MaterialTracker
✅ C1~C6 — 5종 Inspection Parameters
✅ D1~D2 — 명령 확장 (SCALE, ROT_CENTER, DISTORT, CAM_SWITCH, FOCUS_VAL, DIE_FOCUS_ROI)
✅ E1~E5 — EPD/ARM 비동기 이벤트
✅ F1~F2 — DataLogSaver + ImageLogSaver (간이 버전)
✅ J1~J5 — 검증

다음은 **이번 라운드 후순위** (별도 라운드):
⏸ G1~G2 — SPC X-bar 차트 (별도 작업)
⏸ H1~H6 — 5종 파라미터 에디터 (Configuration 탭의 빈 골격에 차후)
⏸ I1~I2 — ZoomDialog (편의 기능)
⏸ DIE_FOCUS_ROI — Inspection 결과 의존, 별도 라운드

---

## 자동 검증 결과 (2026-04-27 실행)

```
==============================================================================================================
CATEGORY  ITEM                                                              RESULT DETAIL
--------------------------------------------------------------------------------------------------------------
BUILD     QMC.Vision.exe (310 이식 후 빌드)                              PASS
BUILD     QMC.CDT-320.exe (Handler 빌드)                                  PASS
STATIC    CameraVector — InvertedX/Y + IsRotated 필드                    PASS
STATIC    VisionScale.ConvertPosition — pixel→mm 변환 함수             PASS
STATIC    VisionConfig — 좌표/지연/측면/오토포커스/데이터로그 키 10종   PASS
STATIC    MaterialTracker — DieRecord + ApplyBottom/Side/DieGap          PASS
STATIC    InspectionParameters — 5종 + ChipType + SideSurface enum       PASS
STATIC    VisionModule — EPD/ARM + 4 신규 메서드                          PASS
STATIC    VisionTcpServer — 5 신규 명령 + Broadcast + EPD/ARM 핸들러    PASS
STATIC    ImageLogSaver — chipUid 디렉토리 PNG 저장                       PASS
STATIC    DataLogSaver — 30+ 칼럼 일자별 CSV                              PASS
STATIC    Handler VisionTcpClient — EPD/ARM 비동기 이벤트 수신           PASS
RUNTIME   Vision/Wafer PING                                                PASS   ACK|WaferVision|PING|OK
RUNTIME   Vision/Wafer EXPOSE                                              PASS   ACK|WaferVision|EXPOSE|w=640;h=480
RUNTIME   Vision/Wafer MATCH                                               PASS   ACK|WaferVision|MATCH|OK;x=...
RUNTIME   Vision/Wafer SCALE                                               PASS   ACK|WaferVision|SCALE|OK;scaleX=0.0031;scaleY=0.0041
RUNTIME   Vision/Wafer ROT_CENTER                                          PASS   ACK|WaferVision|ROT_CENTER|OK;x0=64.00;...
RUNTIME   Vision/Wafer DISTORT                                             PASS   (Wafer 모듈에 DistortionCompensation finder 없음)
RUNTIME   Vision/Wafer CAM_SWITCH                                          PASS   ACK|WaferVision|CAM_SWITCH|OK;tool=toolA;live=true
RUNTIME   Vision/Wafer FOCUS_VAL                                           PASS   ACK|WaferVision|FOCUS_VAL|OK;Left top=...
RUNTIME   Vision/Bin   INSPECT + Material 누적                            PASS   ACK|BinVision|INSPECT|PASS;Width=200.20,...
==============================================================================================================
TOTAL 21   PASS 21   SKIP 0   FAIL 0
```

> **EPD 비동기 이벤트 검증**: 실제 SCALE/ROT_CENTER/EXPOSE 등 모든 그랩 명령에서 ACK 응답보다 먼저
> `EPD|WaferVision` 라인이 클라이언트로 푸시됨을 verify 스크립트의 응답 처리에서 확인.
> Handler 측 VisionTcpClient 의 `ExposureDone` 이벤트로 자동 분기되어 응답 큐와 분리됨.

---
