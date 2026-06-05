# 310 Vision 기능 카탈로그

> 분석 대상: `D:\Work\CDT-320\QMC.DieTransfers.Visions-master`
> 목적: 320 (QMC.Vision) 에 이식해야 하는 **기능** 추출.
> **주의: 코드 아키텍처(SoftBricks/SentiCore Composite/Element/Recipe 트리)는 절대 모방하지 않음. 기능만.**

---

## 1. 통신 계층 (Handler ↔ Vision PC)

### 1.1 프로토콜 (HandlerPCCommunicator)
| 항목 | 310 사양 |
|---|---|
| 프레임 | `STX(0x02) ... ETX(0x03)` 바이트 프레임 |
| 본문 | CSV: `TransactionId,REQUEST|REPLY,Command,DataCount,Data1,…,DataN` |
| 전송 ID | TransactionId 10001~20000, StartRandom |
| 인코딩 | ASCII (BytesConverter.ToBytes/ToString) |
| 트랜잭션 매칭 | 응답이 오면 `TransactionManager.GetTransactionById(id).Commit()` |
| 비동기 | RequestPackets ConcurrentQueue + 별도 Highest priority 모니터링 스레드 |

### 1.2 비동기 알림 (Vision → Handler)
| 명령 | 의미 |
|---|---|
| `EPD` | Exposure Done — 노광/그랩 완료 시점 |
| `ARM` | 알람 — `[TransactionId, reason]` 데이터로 송신. (Grab/Match/Inspection 실패 등) |

### 1.3 알람 키
- `Communicator.AlarmKeys.InterCharacterTimeout`
- `Communicator.AlarmKeys.InvalidResponseFormat`
- `Communicator.AlarmKeys.Transmission`
- `VisionPart.AlarmKeys.ResetTimeout`
- `VisionPart.AlarmKeys.ExposureTimeout`

### 1.4 (320 현재) 비교
- 320 의 `MODULE|CMD|args\n` 라인 프로토콜은 **동등 가독성** 이지만 TransactionId/MessageType/EPD/ARM 비동기 이벤트가 없음.
- 320 은 요청-응답만 1:1, 비동기 이벤트 푸시 없음.

---

## 2. 비전 서비스 (커맨드 디스패치)

VisionPart 의 `CommandExecutors` 사전이 **명령 → 실행기** 매핑. 실행기 종류:

| 서비스 | 입력 | 출력(응답 Data) | 비고 |
|---|---|---|---|
| **PatternMatch** | `[chipUid?]` | `[result, x, y, theta]` (좌표 변환 후) 또는 `[-1]` (no match) | 회전/플립 옵션, ImageLogSaver 자동 |
| **FocusValueMeasurement** | `[chipUid?]` | `[roiName, value, …]` 반복 | OffAfterGrabWhenAutoFocus 옵션 |
| **VisionScale** | (없음) | (없음) → recipe 갱신 | 캘리브레이션, Scale 자동 저장 |
| **Inspection** | `[chipUid, autoOff?]` | `resultCodes` Values 직렬화 | EPD 송신 후 비동기 inspection |
| **DistortionCompensation** | (없음) | (없음) | 왜곡 보정 학습 |
| **CameraSwitch** | `[toolName, liveState]` | (없음) | 멀티 카메라 전환 + Live ON/OFF |
| **RotationalCenter** | (없음) | `[x1,y1,x2,y2,…]` corners | 회전 중심 보정 |
| **DieFocusROISetting** | (없음) | (없음) | Inspection 결과 기반 4-ROI 자동 배치 (Left/Right top/bottom 각각 size 의 5/10/80% 비율) |

### 2.1 좌표 변환
- `VisionScale.ConvertPosition(scale, cameraVector, image, pixel) → coordinate` 으로 픽셀→mm 변환.
- `IsRotated == true` 일 때 X↔Y 스왑.
- `CameraVector.InvertedX/InvertedY` 로 부호 반전.

### 2.2 (320 현재) 비교
- 320 은 PatternMatch / Inspect / Train / Expose / Ping 만 존재.
- **빠진 기능**: VisionScale (캘리브레이션), FocusValueMeasurement, RotationalCenter, DistortionCompensation, CameraSwitch, DieFocusROISetting, 좌표 변환(픽셀→mm).

---

## 3. 검사 알고리즘 (VisionTools + Features)

### 3.1 BottomInspection (다이 후면 검사)
- 입력: 다이 후면 이미지
- 출력 키: `Width, Height, Offset X, Offset Y, Chipping Top Size, Chipping Right Size, Chipping Bottom Size, Chipping Left Size, Foreign Size, Chipping Length, Foreign Object Size, Chip Lower/Upper Spec Limit Width/Height`
- 파라미터: Threshold, Top hat radius, Min foreign area filter size, Link distance, Width/Height min/max color gap, Chipping size 1/2 max color gap, ChipType (White/Black), First peek/Peek value threshold, Stdev, Portentiol defact min size, Chipping depth/length, Foreign object size, ChipLower/UpperSpecLimitW/H, UseContaminationInspection, FileSavePath

### 3.2 SideInspection (측면 검사)
- 다이 4면 별 인스턴스: `Surfaces.FrontWidth | BackWidth | FrontHeight | BackHeight`
- 결과 매핑:
  - FrontWidth → SideChippingBottomSize
  - BackWidth → SideChippingTopSize
  - FrontHeight → SideChippingLeftSize
  - BackHeight → SideChippingRightSize
- 출력 키: `Max Chipping Depth, Chipping Length`
- 파라미터: Threshold, Chipping depth/length, Foreign object size, Blade width, Chip thickness, ChipType, Chip spec limits W/H

### 3.3 DieGapInspection (다이 배치 갭 검사)
- 입력: PLACE 후 이미지
- 출력 키: `Top Gap Avg, Bottom Gap Avg, Left Gap Avg, Right Gap Avg`
- 파라미터: Threshold, Upper limit, Lower limit
- DataLogSaver 자동 저장 트리거

### 3.4 DistortionCompensation (왜곡 보정)
- 출력: 보정 변환 매트릭스
- 파라미터: Threshold, Pitch X/Y, ChipType, TargetSearch (CrossLine/Circle)

### 3.5 VisionScaleCalculation (스케일 산출)
- 출력: Scale.X / Scale.Y (mm/pixel)
- 파라미터: Threshold, ChipWidth, ChipHeight (mm 실측), ChipType

### 3.6 (320 현재) 비교
- 320 의 `IInspector.InspectionResult` 는 `Items[]` (Name, Value, IsPass) 만 가짐.
- **빠진 기능**: 위 5종 검사 알고리즘의 구체적 파라미터 모델, 부위(Surface) 분기, 결과 키 표준화, ChipType 흑/백, ROI 자동 배치 비율.

---

## 4. 머티리얼 / 데이터 추적

### 4.1 MaterialDataManager
- chipUid 별 InspectionDatas:
  - Loading/Unloading Substrate ID/X/Y
  - DieWidth, DieHeight
  - ChipLower/UpperSpecLimit Width/Height
  - BackChipping Top/Right/Bottom/Left Size
  - BackChippingLength, ForeignObjectSize, BackForeignSize
  - SideChipping Top/Right/Bottom/Left Size + Length
  - PlaceTop/Bottom/Left/Right GapAverage
  - DieGapUpper/LowerLimit

### 4.2 (320 현재) 비교
- 320 핸들러에 `MaterialDataManager` 또는 다이별 데이터 누적 객체 없음. EventLogger + GoodCount/NgCount 만.
- **빠진 기능**: 다이당 30+ 필드 누적, chipUid 키, lot 보고서.

---

## 5. 로깅

### 5.1 DataLogSaver (CSV)
- 일자별 파일, 모든 검사 항목 칼럼 (위 InspectionItems 15종 + Titles 30종).
- DieGap inspection 완료 시 한 줄 추가.

### 5.2 ImageLogSaver
- chipUid 별 이미지 저장. `"Manual"` 명시 시 skip.
- 백엔드별 다이렉트 IImage 직렬화.

### 5.3 (320 현재) 비교
- 320 핸들러: EventLogger (CSV 일자별, 텍스트만) 있음.
- **빠진 기능**: 320 Vision 측 이미지 저장, chipUid 디렉토리, lot 데이터 CSV 칼럼화.

---

## 6. Recipe / 캘리브레이션

### 6.1 VisionScaleServiceExecutorRecipe
- Body.Scale = (X, Y) mm/pixel
- RecipeAssigner 가 활성 recipe 를 보유, 변경 시 모든 InspectionVisionToolServiceExecutor 의 `Tool.SetVisionConfig(VisionConfig)` 호출.

### 6.2 VisionConfig (320 에 이미 일부 존재)
- TargetVision / BottomVision / SideVisionFront / SideVisionBack
- 각각 CameraResolutionWidth/Height, PixelSizeWidth/HeightMm

### 6.3 6 가지 SubsetRecipe
- BottomInspection / SideInspection / DieGapInspection / DistortionCompensation / Inspection / VisionScale 각각 + Assigner

### 6.4 (320 현재) 비교
- 320 핸들러에 `RecipeStore` (Recipe JSON) 있음. Vision 측은 RecipePage 빈 골격만.
- **빠진 기능**: Vision 측 Recipe 본문(스케일/임계값/ROI), Tool 별 Subset.

---

## 7. Configuration / 설정

| 항목 | 310 위치 |
|---|---|
| DelayBeforeGrab | VisionPartConfigurationBody |
| IsRotated | VisionPartConstructConfiguration |
| InvertedX, InvertedY | 동일 (CameraVector) |
| AxisNames | 동일 |
| MotionConsumerLocator | 동일 |
| SideLocation (None/Front/Back) | 동일 |
| OffAfterGrabWhenAutoFocus | 동일 |
| SelectDisplayMonitor (멀티 모니터 표시) | UITypeEditor |

### 7.1 (320 현재) 비교
- 320 `VisionConfig` 에 카메라/포트만. **빠짐**: DelayBeforeGrab, IsRotated, Inverted, SideLocation, OffAfterGrabWhenAutoFocus, 모니터 선택.

---

## 8. HMI Forms (310)

| Form | 용도 |
|---|---|
| VisionTopForm | 헤더(로고/알람/종료) |
| VisionSideOperationForm | 좌측 메뉴: DieGap / Side / Operation |
| VisionSideConfigurationForm | 좌측 메뉴: Configuration |
| VisionSideMaintenanceForm | 좌측 메뉴: Maintenance |
| VisionSideDataLogForm | 좌측 메뉴: DataLog |
| VisionEuipmentBottomInspectionMonitoringForm | 라이브 + ROI + 결과 표시 |
| VisionEuipmentSideInspectionMonitoringForm | 측면 라이브 |
| VisionEuipmentDieGapInspectionMonitoringForm | 갭 라이브 |
| VisionEuipmentInspectionMonitoringForm | 일반 inspection 라이브 |
| VisionEuipmentCameraMonitoringForm | 카메라 raw 라이브 |
| VisionPartMaintenanceControl | Vision Part 매뉴얼 조작 |
| VisionModuleConfigurationForm | 모듈 카메라 스위치 등 설정 |
| VisionModuleMaintenanceForm | 모듈 maintenance |
| VisionPartConfigurationEditor | Configuration body 편집 |
| XBarChartForm | SPC X-bar 차트 (LSL/USL/Avg/Stdev/Max/Min) |
| ZoomDialog | 이미지 확대 다이얼로그 |
| RemoteJoystick | 원격 조이스틱 |

### 8.1 검사기/툴 별 ParameterEditor (5종)
- BottomInspectionVisionToolServiceExecutorParameterEditor
- SideInspectionVisionToolServiceExecutorParameterEditor
- DieGapInspectionVisionToolServiceExecutorParameterEditor
- DistortionCompensationVisionToolServiceExecutorParameterEditor
- VisionScaleVisionToolServiceExecutorParameterEditor

### 8.2 Feature Editor (5종)
- BottomInspectionFeatureEditor
- SideInspectionFeatureEditor
- DieGapInspectionFeatureEditor
- DistortionCompensationFeatureEditor
- VisionScaleCalculationFeatureEditor

### 8.3 Maintenance Control (서비스 별)
- DistortionCompensationServiceExecutorMaintenanceControl
- InspectionServiceExecutorMaintenanceControl
- RotationalCenterServiceExecutorMaintenanceControl
- VisionScaleServiceExecutorMaintenanceControl

### 8.4 (320 현재) 비교
- 320 Vision: 5탭(Operation, Configuration, Maintenance, Recipe, DataLog) UserControl + 카메라뷰 + 일루미네이터 + 조그박스.
- **빠진 기능**: SPC X-bar 차트, 모니터링 폼(Bottom/Side/DieGap/Camera 별 라이브), 줌 다이얼로그, 툴별 파라미터 에디터, Feature 에디터, Maintenance 컨트롤, 알람 뷰어.

---

## 9. 기타 부가 기능

| 기능 | 310 |
|---|---|
| RemoteViewer | 원격 화면 보기 |
| MaterialDataManageServiceExecutor | 머티리얼 데이터 매니지 명령 |
| SelectDisplayMonitorUITypeEditor | 폼이 표시될 모니터 선택 |
| GeneralUnionRecipeAssigner | 모든 SubsetRecipe 통합 적용 |

### 9.1 (320 현재) 비교
- 핸들러측은 다중 모니터 / 원격 뷰어 / 머티리얼 매니저 없음 (스코프 외 가능성).

---

## 10. 핵심 동작 시퀀스 (310 표준)

```
[Handler] Send Packet (Match/Inspect/...)
     ↓ STX...ETX
[Vision PC: HandlerPCCommunicator]
     ↓ ReceiveRequest event
[VisionPart: RequestPackets queue]
     ↓ Monitoring thread
[CommandExecutors[command] 디스패치]
     ↓
[Optional: DelayBeforeGrab]
     ↓
[Camera.GrabSync(out IImage[])]
     ↓
[(Inspection 만) "EPD" 비동기 송신]
     ↓
[Tool.MatchSync / MeasureSync / InspectionSync]
     ↓
[VisionScale.ConvertPosition (픽셀→mm)]
     ↓
[Recipe 갱신 (필요시)]
     ↓
[ImageLogSaver / DataLogSaver]
     ↓
[Communicator.SendResponse(transactionId, command, results)]
```

실패 시: `Send("ARM", [transactionId, reason])` 비동기 송신.

---

## 11. 320 이식 시 우선순위 분류

### Tier 1 — 필수 (검사 정확도/추적성 직결)
- VisionScale 캘리브레이션 + 좌표 변환 (픽셀→mm)
- 5 검사 알고리즘의 파라미터 표준화 (Bottom/Side/DieGap/Distortion/VisionScale)
- 다이별 데이터 누적 (chipUid 기반 Material 트래킹)
- DelayBeforeGrab, IsRotated, InvertedX/Y, SideLocation 설정

### Tier 2 — 강력 권장 (운영 편의/품질)
- TransactionId + MessageType 프로토콜 보강 (응답 매칭, EPD/ARM 비동기 이벤트)
- 추가 명령 4종: VisionScale, RotationalCenter, DistortionCompensation, CameraSwitch
- Vision 측 ImageLogSaver + DataLogSaver
- SPC X-bar 차트
- 툴별 ParameterEditor (5종)

### Tier 3 — 선택
- DieFocusROISetting 자동 ROI 배치
- FocusValueMeasurement
- RemoteViewer
- 멀티 모니터 표시 선택
- 좌측 사이드 메뉴 폼 분리(현 320 은 상단 5탭으로 충분)

---

## 12. 320 아키텍처 매핑 (이식 가이드)

| 310 개념 | 320 우리 아키텍처 매핑 |
|---|---|
| `VisionPart.CommandExecutors` 사전 | `VisionTcpServer.ProcessLine` switch 확장 |
| `MachineVisionServiceExecutor` 추상 클래스 | **만들지 않음**. 명령은 `VisionTcpServer` 에서 직접 처리 + `VisionModule` 메서드로 위임 |
| `VisionTool.MatchSync/MeasureSync/InspectionSync` | `IPatternFinder.Match` / `IInspector.Inspect` (이미 있음) + 새로운 메서드 추가 |
| `Feature` (파라미터 enum 군) | `Backends/Sim,OpenCv,Cognex` 의 `LoadParameters/SaveParameters` JSON body 로 표준화 |
| `RecipeAssigner.AssignedSubsetRecipe` | `Config/VisionRecipe.cs` (새 파일) JSON Recipe 모델 |
| `MaterialDataManager` | `Core/MaterialTracker.cs` (새 파일) ConcurrentDictionary<string, DieRecord> |
| `DataLogSaver` | `Core/DataLogSaver.cs` (새 파일) — CSV 일자별 |
| `ImageLogSaver` | `Core/ImageLogSaver.cs` (새 파일) — chipUid 디렉토리 |
| `XBarChartForm` | `Ui/Pages/SpcChartPage.cs` (새 파일) `Chart` 컨트롤 사용 |
| `ZoomDialog` | `Ui/Dialogs/ZoomDialog.cs` |
| `VisionScale.ConvertPosition` | `Core/VisionScale.cs` (정적 메서드) |
| `VisionPart.Monitoring` 스레드 | **불필요**. 320 은 TcpServer 가 이미 비동기 스레드. |
| SoftBricks `Element/Part/Module` 트리 | **버림**. Plain C# class + ICamera + IVisionBackend + VisionModule 유지. |
| `Composite/Aggregate/Multiplicity` 어트리뷰트 | **버림**. |
| `ConstructConfiguration` | `Config/VisionConfig.cs` 를 확장. JSON 직렬화. |

---

이 카탈로그를 기반으로 Gap 분석과 구현 체크리스트를 작성합니다.
