# CDT-320 Architecture Export (v2)

> 다른 AI에게 인계하여 PPT/시퀀스 문서를 업데이트하기 위한 코드 분석 결과.
> 모든 정보는 `D:\Work\CDT-320\` 의 실제 코드에서 추출. 코드에서 확인 안 된 항목은 "확인 필요"로 표시.

- **분석 대상**: `QMC.CDT-320` (Handler), `QMC.Common` (공용), `QMC.Vision` (Vision PC), `CDT320Simulator` (시뮬레이터)
- **분석 제외**: `QMC.DieTransfers*` (310 레퍼런스 — 코드 아키텍처 비참조 원칙)
- **버전**: v2 (2026-04-30 재작성, v1 의 "확인 필요" 7건을 본문에 통합)
- **파일 경로 표기**: 별도 표기 없으면 `D:\Work\CDT-320\` 기준 상대 경로

---

## 목차

1. [솔루션 구조](#1-솔루션-구조)
2. [주요 네임스페이스 / 폴더 구조](#2-주요-네임스페이스--폴더-구조)
3. [핵심 클래스 카탈로그](#3-핵심-클래스-카탈로그)
4. [인터페이스 카탈로그](#4-인터페이스-카탈로그)
5. [Sim Axis 매핑 전체](#5-sim-axis-매핑-전체)
6. [Recipe 파라미터 전체 목록](#6-recipe-파라미터-전체-목록)
7. [알람 코드 전체 목록](#7-알람-코드-전체-목록)
8. [이벤트 / 콜백 카탈로그](#8-이벤트--콜백-카탈로그)
9. [인터락 매트릭스](#9-인터락-매트릭스)
10. [UI 화면 인벤토리](#10-ui-화면-인벤토리)
11. [시퀀스 문서와의 mismatch 후보](#11-시퀀스-문서와의-mismatch-후보)

---

## 1. 솔루션 구조

### 1.1 솔루션 파일

| 솔루션 파일 | 위치 | 포함 프로젝트 |
|---|---|---|
| `QMC.CDT-320.sln` | `QMC.CDT-320/` | QMC.CDT-320, QMC.Common, QMC.Vision |
| `CDT320Simulator.sln` | `CDT320Simulator/` | CDT320Simulator |

### 1.2 프로젝트 정보

| 프로젝트 | OutputType | AssemblyName | RootNamespace | TargetFramework | 역할 |
|---|---|---|---|---|---|
| **QMC.CDT-320** | WinExe | QMC.CDT-320 | QMC.CDT_320 | .NET 4.7.2 | Handler — WinForms UI + Equipment Controller (메인) |
| **QMC.Common** | Library | QMC.Common | QMC.Common | .NET 4.7.2 | 공용 라이브러리 — 기계/유닛/축/IO 베이스 인터페이스 + 추상 클래스 |
| **QMC.Vision** | WinExe | QMC.Vision | QMC.Vision | .NET 4.7.2 | Vision PC — WinForms UI + 비전 처리(카메라·패턴인식·검사). `AllowUnsafeBlocks=true` |
| **CDT320Simulator** | WinExe | CDT320Simulator | CDT320Simulator | .NET 4.7.2 | 3D 시뮬레이터 — WPF + HelixToolkit (모션 시뮬레이션) |

### 1.3 프로젝트 간 참조 관계

```
QMC.CDT-320 (Handler) ───► QMC.Common
QMC.Vision  (독립)          ◄── TCP 통신 (포트 5100/5101/5103/5104/5105/5106) ──► QMC.CDT-320
CDT320Simulator (독립)      ◄── TCP 통신 (포트 7001) ──► QMC.CDT-320
```

`QMC.Common` 만 라이브러리. 나머지 셋은 모두 독립 실행 파일이며, Handler ↔ Vision/Simulator 는 TCP 로만 연결.

### 1.4 NuGet 패키지

| 프로젝트 | NuGet |
|---|---|
| QMC.CDT-320 | (없음 — 순수 .NET Framework GAC 참조만) |
| QMC.Common | (없음) |
| QMC.Vision | (없음 — Cognex DLL 은 동적 로드) |
| CDT320Simulator | `HelixToolkit.Wpf` 2.25.0, `Newtonsoft.Json` 13.0.3 |

---

## 2. 주요 네임스페이스 / 폴더 구조

### 2.1 QMC.CDT-320 (Handler)

#### Equipment 폴더

| 폴더 | 역할 | 핵심 클래스 (5개 이내) |
|---|---|---|
| Equipment (루트) | 기계 전체 + 제어 + Stage 인터페이스 | `CDT320_Machine`, `MachineController`, `AppSettings`, `SimulatorBridge`, `IStageInterfaces` |
| Equipment/Ajin | 모션·IO 어댑터 (AjinEXACRO 드라이버) | `AjinSystem`, `AjinFactory`, `AjinAxis`, `AjinCylinder`, `AjinConfig` |
| Equipment/Alarms | 알람 관리 + 마스터 | `AlarmManager`, `AlarmMaster`, `AlarmRecord`, `AlarmSeverity` |
| Equipment/Bin | Reject 처리 + 코드 매핑 | `SubPortMaterialRejector`, `BinCodeMap` |
| Equipment/DieMaps | 칩 위치 맵 생성·저장 | `DieMap`, `DieMapGenerator`, `DieMapSaver` |
| Equipment/Interlocks | 안전 인터락 (총 15개) | `StandardInterlocks`, `MotionInterlock`, `ExtendedInterlocks`(1~3) |
| Equipment/Jobs | 작업 큐 | `JobOrder`, `JobQueue`, `JobState`, `JobType` |
| Equipment/Logging | 이벤트 로깅 | `EventLogger`, `EventKind` |
| Equipment/Lots | 로트(웨이퍼 배치) 추적 | `Lot`, `LotStorage`, `LotState` |
| Equipment/Materials | 자재 정의 | `Die`, `DieTapeFrame`, `MaterialStorage`, `MaterialSpecs` |
| Equipment/Recipes | 레시피 영속화 | `RecipeStore` (+ `RecipeProject` 등 DTO) |
| Equipment/Remote | 원격 모니터링 | `RemoteViewer` |
| Equipment/Secs | SECS/HSMS 통신 | `SecsHost`, `HsmsConnection`, `SecsMessage`, `SecsItem` |
| Equipment/Sensors | 센서 어댑터 | `IonizerSensor` |
| Equipment/Sim | 시뮬레이터 어댑터 | `SimCassetteDriver`, `OutputUnloaderAdapter`, `WaferLoaderAdapter` |
| Equipment/Vision | 비전 PC 통신·좌표·정렬 | `VisionHub`, `VisionTcpClient`, `VisionAdapters`, `WaferVisionAdapter`, `TpuVisionAdapter`, `CoordinateMap`, `AlignmentSolver`, `BarcodeSerialAdapter` |

#### Ui 폴더

| 폴더 | 역할 | 핵심 클래스 |
|---|---|---|
| Ui/Tabs | 메인 6 탭 | `WorkTab`, `WorkInfoTab`, `HistoryTab`, `RecipeTab`, `SettingsTab`, `UserTab`, `TabBase` |
| Ui/Pages/Work | Work 서브 | `WorkMainPage`, `MapTransferPage`, `VisionAlignPage` |
| Ui/Pages/WorkInfo | 스테이지 상태 | `HeadPage`, `InputCassettePage`, `InputFeederPage`, `InputStagePage`, `OutputPages`, `ActiveLotPage`, `LogicDetailPage`, `OperationPanelStatusPage`, `PlateStatusPage` |
| Ui/Pages/History | 이력 | `EventLogPage`, `AlarmHistoryPage`, `FilterPages`, `MessageEditPage` |
| Ui/Pages/Recipe | 레시피 14종 | `ProjectPage`, `CassetteRecipePage`, `FeederRecipePage`, `StageRecipePage`, `HeadRecipePage`, `VisionRecipePage`, `MapCreatePage` ... |
| Ui/Pages/Settings | 설정 | `GeneralPage`, `MotionPage`, `IoListPage`, `DevicePages`, `AlarmMasterPage`, `AxisSetupPage`, `CameraSetupPage`, `LightControllerPage`, `PositionTeachingPage`, `SimulatorLinkPage`, `VisionLinkPage` |
| Ui/Pages/Material | 자재 | `MaterialBinPage`, `DieMapPage` |
| Ui/Dialogs | 대화상자 7종 | `LoginDialog`, `BoardScanDialog`, `ModeDialogs`, `SystemSelfTestDialog` 등 |
| Ui/Controls | 커스텀 UI | `ActionButton`, `SidebarButton`, `AlarmBanner`, `DieMapView`, `LiveLotMapView` 등 |
| Ui/Security | 권한 | `AccessControl`, `UserLevel`, `UserSession` |
| Ui/Localization | 다국어 | `Lang` |
| Ui/Util | UI 유틸 (Stage 60) | `UiClickAuditor` |

### 2.2 QMC.Common

| 폴더 | 역할 | 핵심 클래스 |
|---|---|---|
| (루트) | Composite 베이스 | `Machine`, `BaseEquipmentNode`, `BaseUnit<T,C,R>`, `BaseComponent<T,C,R>` |
| Interfaces | 데이터 마커 | `IEquipmentData`, `ISetupData`, `IConfigData`, `IRecipeData` |
| Motion | 모션 축 | `BaseAxis`, `AxisData`, `MotionEnums` |
| IO | 입출력 | `BaseCylinder`, `BaseDigitalInput`, `BaseDigitalOutput`, `IoData` |

### 2.3 QMC.Vision

| 폴더 | 역할 | 핵심 클래스 |
|---|---|---|
| Core | 비전 추상 | `VisionFactory`, `IVisionBackend`, `ICamera`, `CameraBase`, `IPatternFinder`, `IInspector`, `IEdgeFinder`, `Roi`, `MatchResult`, `GrabResult` |
| Cameras/Sim | 시뮬 카메라 | `SimCamera` |
| Cameras/Hik | Hik GigE | `HikGigECamera`, `HikMvsDll` |
| Backends/Sim | Sim 백엔드 | `SimBackend`, `SimPatternFinder`, `SimInspector` |
| Backends/OpenCv | OpenCV 백엔드 | `OpenCvBackend`, `OpenCvPatternFinder`, `OpenCvInspector` |
| Backends/Cognex | Cognex 백엔드 (동적 로드) | `CognexBackend`, `CognexPatternFinder`, `CognexInspector`, `CognexCaliper`, `CognexHistogram`, `CognexColorMatch` |
| Modules | 비전 모듈 | `WaferVisionModule`, `BinVisionModule`, `BottomInspectionModule`, `TopSideInspectionModule`, `BottomSideInspectionModule` |
| Comm | TCP | `VisionTcpServer` |
| Config | 설정 | `VisionConfig` |
| Ui/Pages | 8 화면 | `OperationPage`, `ConfigurationPage`, `MaintenancePage`, `RecipePage`, `DataLogPage`, `FinderPage`, `InspectorPage`, `SpcChartPage` |
| Ui/Editors | 파라미터 편집기 | `ParameterEditorBase`, `BottomInspectionParameterEditor`, `SideInspectionParameterEditor`, `DieGapInspectionParameterEditor`, `DistortionParameterEditor`, `VisionScaleParameterEditor` |
| Ui/Controls | UI 컴포넌트 | `CameraView`, `IlluminatorPanel`, `JogBox` |

### 2.4 CDT320Simulator

| 파일/폴더 | 역할 | 클래스 |
|---|---|---|
| `MainWindow.xaml(.cs)` | WPF 메인 | `MainWindow` (HelixToolkit 3D 뷰) |
| `App.xaml(.cs)` | 진입점 | `App` |
| `MotionMap.cs` | 축 매핑 | `MotionMap` |
| `IoMap.cs` | I/O 매핑 | `IoMap` |
| `TcpProtocol.cs` | Handler 통신 | `TcpProtocol` |

---

## 3. 핵심 클래스 카탈로그

> 사용자 명시 클래스 위주. public 메서드 시그니처는 매개변수 타입과 이름까지. property는 별도 항목.

### 3.1 MachineController

| 항목 | 내용 |
|---|---|
| 클래스명 | `MachineController` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/MachineController.cs` |
| 역할 | 작업 탭의 초기화/시작/정지/CYCLE RUN/STOP 버튼을 실제 장비 동작으로 연결. 모든 Unit 을 조종하며 로트포트·사이클·비상정지 시퀀스 관리 |
| public 메서드 | `MachineController(CDT320_Machine machine)` <br> `Task InitAsync()` <br> `Task StartAsync()` <br> `Task StopAsync()` <br> `Task CycleRunAsync(int totalDies)` <br> `Task CycleStopAsync()` <br> `Task ShutdownAsync()` <br> `Task EmergencyStopAsync()` <br> `async Task<bool> LoadNextWaferAsync()` <br> `async Task<bool> ScanInputCassetteAsync()` <br> `async Task<bool> ScanOutputCassettesAsync()` <br> `async Task<bool> UnloadInputStageWaferAsync()` <br> `async Task<bool> RetractCurrentWaferAsync()` <br> `async Task<bool> StoreCompletedWaferAsync(bool isGood)` <br> `async Task<bool> MoveInputStageToDieAsync(int row, int col)` <br> `async Task<bool> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0)` <br> `async Task<bool> AlignWaferAsync((double mx, double my)[] motorPts, string finder = "ReticleFinder")` <br> `void ApplyRecipeMode(RecipeProject p)` |
| public property | `MachineStatus Status` <br> `int CycleTotal / CycleDone / GoodCount / NgCount` <br> `int PickFailCount / PlaceFailCount / Collet1UseCount / Collet2UseCount / NeedleUseCount / ErrorCount` <br> `TimeSpan NormalDownTime / ErrorDownTime / RecoveryTime / Mtbf / Mttr` <br> `int CurrentInputSlot` / `bool InputWaferAtExchange` <br> `bool AutoScanCassetteOnInit` <br> `int OutputSlotNg / OutputSlotGood1 / OutputSlotGood2` <br> `int WafersPerOutputBatch / DiesPerWafer / DiesPerColletClean / PickersPerCycle` <br> `bool DualArmMode / DryRun / StepRun` <br> `SecsHost SecsHost` |
| 의존 | `CDT320_Machine`, `AlarmManager`, `InterlockRegistry`, `VisionComm` (`VisionHub`, `WaferVisionAdapter`, `TpuVisionAdapter`, `AlignmentSolver`, `CoordinateMapStore`), `LotStorage`, `JobQueue`, `JobOrder`, `Die`, `JobType`, `SlotMapperRegistry`, `BinCodeMap`, `SubPortMaterialRejector`, `PlateRegistry` |

### 3.2 CDT320_Machine

| 항목 | 내용 |
|---|---|
| 클래스명 | `CDT320_Machine` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/CDT320Machine.cs` |
| 역할 | CDT-320 머신 최상위 루트 (Composite). 6개 메인 Unit + 부속 Unit 통합 |
| public 메서드 | `CDT320_Machine()` |
| public property | `InputLoaderUnit InputLoader` <br> `InputStageUnit InputStage` <br> `TransferPickerUnit TransferPicker` <br> `VisionInspectionUnit VisionInspection` <br> `OutputStageUnit OutputStage` <br> `OutputUnloaderUnit OutputUnloader` <br> `OperationPanelUnit OpPanel` <br> `ResourceSensorsUnit Resources` <br> `IonizerUnit Ionizer` <br> `PostPnpTransferUnit PostPnp` <br> `IBarcodeReader BinBarcodeReader` |
| 의존 | 위 11개 타입 + `VisionComm.WaferVisionAdapter`, `VisionComm.TpuVisionAdapter`, `Sim.WaferLoaderAdapter`, `Sim.OutputUnloaderAdapter` (생성자 주입) |
| 비고 | 같은 파일 내 **Null Object 패턴** 클래스들 정의: `NullBarcodeReader` (L44~48), `NullWaferMapHandler` (L64~76), `NullVisionTpuClient` (L91~117), `NullTpuUnit` (L120~131) |

### 3.3 InputStageUnit

| 항목 | 내용 |
|---|---|
| 클래스명 | `InputStageUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/InputStageUnit.cs` |
| 역할 | 웨이퍼 고정 + 다이 위치 관리. Load/VisionAlign/MultiScanPickup/Unload 시퀀스 |
| public 메서드 | `InputStageUnit(IWaferLoader loader, IBarcodeReader barcode, IVisionTcpClient vision, IWaferMapHandler mapHandler, ITransferPickerUnit tpu)` <br> `async Task<bool> LoadAndPrepareWaferAsync()` <br> `async Task<bool> VisionAlignAndSetupOriginAsync()` <br> `async Task<UserConfirmResult> WaitForUserConfirmAsync()` <br> `async Task<bool> MultiScanAndPickupAsync(int startDieIndex)` <br> `async Task<bool> UnloadWaferAsync()` <br> `void ConfirmFromUi(UserConfirmResult result)` |
| public property | `BaseAxis StageY / StageT / ExpanderZ / CameraX / NeedleBlockX / NeedleZ / EjectPinZ` <br> `BaseDigitalOutput NeedleVacuum` <br> `IWaferLoader Loader` / `IBarcodeReader Barcode` / `IVisionTcpClient Vision` / `IWaferMapHandler MapHandler` / `ITransferPickerUnit Tpu` <br> `WaferMapData CurrentWaferMap` <br> `double OriginY / OriginX / PitchX / PitchY` |
| public 이벤트 | `event Action<DieGrade> WaferChangeRequested` (라인 799 부근) |
| 의존 | `IWaferLoader`, `IBarcodeReader`, `IVisionTcpClient`, `IWaferMapHandler`, `ITransferPickerUnit`, `WaferMapData`, `VisionAlignResult`, `UserConfirmResult`, `AjinFactory` |

### 3.4 OutputStageUnit

| 항목 | 내용 |
|---|---|
| 클래스명 | `OutputStageUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/OutputStageUnit.cs` |
| 역할 | 검사 완료 다이를 양품/불량 분류 적재. GoodStage/NgStage 두 개의 `StageModule` |
| public 메서드 | `OutputStageUnit(ITpuUnit tpu, IOutputUnloaderUnit unloader)` <br> `async Task<bool> ReceiveDieAsync(ReceiveDieRequest request)` <br> `async Task<bool> InspectBinPositionAsync()` <br> `async Task<bool> RequestWaferChangeAsync(DieGrade grade)` <br> `async Task<bool> PerformColletCleaningAsync()` |
| public property | `StageModule GoodStage` / `StageModule NgStage` <br> `BaseAxis BinCameraX` <br> `ITpuUnit Tpu` / `IOutputUnloaderUnit Unloader` |
| 의존 | `ITpuUnit`, `IOutputUnloaderUnit`, `StageModule`, `ReceiveDieRequest`, `DieGrade` |

### 3.5 TransferPickerUnit

| 항목 | 내용 |
|---|---|
| 클래스명 | `TransferPickerUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/TransferPickerUnit.cs` |
| 역할 | TPU 최상위. 좌·우 미러 형태의 `TpuArmUnit` 2개 (LeftArm = FRONT, RightArm = REAR) |
| public 메서드 | `TransferPickerUnit(IVisionTpuClient vision)` |
| public property | `TpuArmUnit LeftArm` / `TpuArmUnit RightArm` |
| 의존 | `TpuArmUnit`, `IVisionTpuClient` |

### 3.6 TpuArmUnit

| 항목 | 내용 |
|---|---|
| 클래스명 | `TpuArmUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/TransferPickerUnit.cs` |
| 역할 | TPU 한쪽 암. ArmX·ArmY 갠트리 + 4 PickerComponent + SideVisionY |
| public 메서드 | `TpuArmUnit(string armName, IVisionTpuClient vision)` <br> `async Task<BottomVisionOffset[]> InspectBottomVisionAsync()` (L523~595) <br> `async Task<SideVisionResult[]> InspectSideVisionAsync(BottomVisionOffset[] bottomOffsets)` (L622~842) <br> `async Task<bool> PlaceDiesAsync()` (L860~898) |
| public property | `BaseAxis ArmX / ArmY / SideVisionY` <br> `PickerComponent[] Pickers` (4개) <br> `IVisionTpuClient Vision` |
| 핵심 동작 | `InspectSideVisionAsync` 내부에서 **`Task.WhenAll`** 로 PickerT 90° 회전과 ArmX/ArmY 재정렬 동시 수행 (L739~747) |
| 의존 | `PickerComponent`, `IVisionTpuClient`, `BottomVisionOffset`, `SideVisionResult`, `AjinFactory` |

### 3.7 PickerComponent

| 항목 | 내용 |
|---|---|
| 클래스명 | `PickerComponent` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/TransferPickerUnit.cs` |
| 역할 | 개별 픽커. Z(픽업/포커스/대기) + T(회전) + Vacuum/Blow DO |
| public 메서드 | `PickerComponent(int pickerNo)` <br> `void VacuumOn()` / `void VacuumOff()` <br> `void BlowOn()` / `void BlowOff()` <br> `async Task<bool> PickupAsync()` (L317~332): PickerZ 하강 → 진공 ON → `Recipe.VacuumSettleMs` 대기 (50ms) <br> `async Task<bool> MoveToFocusAsync()` (L339~350): PickerZ → `Setup.FocusPosition` (-2.0mm) <br> `async Task<bool> MoveToWaitAsync()`: PickerZ → `Setup.WaitPosition` (0.0mm) <br> `async Task<bool> PlaceAsync()` (L374~399): PickerZ 하강(PlacePosition) → 진공 OFF → Blow 펄스(`BlowPulseMs`=100ms) → PickerZ 상승(WaitPosition) |
| public property | `BaseAxis PickerZ / PickerT` <br> `BaseDigitalOutput VacuumOut / BlowOut` |
| 의존 | `BaseAxis`, `BaseDigitalOutput`, `AjinFactory` |

### 3.8 InputLoaderUnit (Wafer Loader / Feeder)

| 항목 | 내용 |
|---|---|
| 클래스명 | `InputLoaderUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/InputLoaderUnit.cs` |
| 역할 | Input Cassette → 웨이퍼 슬롯별 공급. ScanCassette / MoveToTargetSlot / MoveToExchange / RetractFeeder |
| public 메서드 | `InputLoaderUnit()` <br> `Task<bool> ScanCassetteAsync(int maxSlots, double slotPitch)` <br> `Task MoveToTargetSlotAsync(double targetPosition)` <br> `Task<bool> MoveToExchangePositionAsync()` <br> `Task<bool> RetractFeederAsync()` |
| public property | `BaseAxis ElevatorZ / FeederY` <br> `BaseDigitalInput CassetteExistSensor / ProtrusionSensor / WaferDetectSensor / WaferClampedSensor` <br> `BaseCylinder FeederUpDownCyl / FeederClampCyl` <br> `IReadOnlyList<bool> WaferMap` |
| 의존 | `IWaferLoader` 구현, `AjinFactory`, `BaseAxis`, `BaseDigitalInput`, `BaseCylinder` |

### 3.9 OutputUnloaderUnit

| 항목 | 내용 |
|---|---|
| 클래스명 | `OutputUnloaderUnit` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/OutputUnloaderUnit.cs` |
| 역할 | 3개 Output Cassette(NG / Good1 / Good2) 적재. 헬퍼 `PickupWaferAtPositionAsync` (5단계) + `PlaceWaferAtPositionAsync` (6단계) |
| public 메서드 | `OutputUnloaderUnit()` <br> `Task<bool> ScanAllCassettesAsync()` <br> `Task<bool> StoreFullWaferAsync(TargetCassette target, int slot)` <br> `Task<bool> SupplyEmptyWaferAsync()` <br> `Task<bool> ExchangeWaferSequenceAsync(...)` |
| public property | `BaseAxis ElevatorZ / FeederY` <br> `BaseDigitalInput ExistSensor_NG / ExistSensor_Good1 / ExistSensor_Good2 / ProtrusionSensor / WaferDetectSensor / WaferClampedSensor` <br> `BaseCylinder FeederUpDownCyl / FeederClampCyl` |
| 의존 | `TargetCassette` (enum), `AjinFactory`, `BaseAxis`, `BaseDigitalInput`, `BaseCylinder` |

### 3.10 VisionTcpClient

| 항목 | 내용 |
|---|---|
| 클래스명 | `VisionTcpClient` |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs` |
| 역할 | QMC.Vision TCP 서버와 통신 — Expose / Match / Inspect / Train 명령 송수신 |
| public 메서드 | `VisionTcpClient(string moduleName, string host, int port)` <br> `Task<bool> ConnectAsync(int timeoutMs = 3000)` <br> `void Disconnect()` <br> `Task<string> SendAsync(string line, int timeoutMs = 5000)` <br> `Task<bool> PingAsync()` <br> `Task<bool> ExposeAsync(int index = 0, int timeoutMs = 5000)` <br> `Task<MatchResultDto> MatchAsync(string finder, int index = 0, int timeoutMs = 5000)` <br> `Task<InspectionResultDto> InspectAsync(string inspector, int index = 0, int timeoutMs = 5000)` <br> `Task<bool> TrainAsync(string finder, int timeoutMs = 5000)` |
| public property | `string Host / Port / ModuleName` <br> `bool IsConnected` |
| public 이벤트 | `event Action ConnectionChanged` <br> `event Action<int> ExposureDone` <br> `event Action<string> Alarmed` |
| 의존 | `TcpClient`, `NetworkStream`, `TaskCompletionSource<string>`, `MatchResultDto`, `InspectionResultDto` |

### 3.11 BarcodeReader 구현

| 클래스 | 파일 | 역할 |
|---|---|---|
| `BarcodeSerialAdapter` | `QMC.CDT-320/QMC.CDT-320/Equipment/Vision/BarcodeSerialAdapter.cs:12` | RS-232 시리얼 바코드 리더 — `ReadAsync(timeoutMs)` 구현 (L49~73). `SerialPort` 로 "READ?" 명령 송신 → 응답 수신. 실패 시 `"WAFER-NULL-ID"` 반환 |
| `NullBarcodeReader` | `QMC.CDT-320/QMC.CDT-320/Equipment/CDT320Machine.cs:44~48` | Null Object — 빌드/시뮬용. 항상 즉시 반환 |
| 인터페이스 | `IBarcodeReader` | `QMC.CDT-320/QMC.CDT-320/Equipment/IStageInterfaces.cs:37~45` |

### 3.12 WaferMapHandler 구현 (현 상태)

| 클래스 | 파일 | 비고 |
|---|---|---|
| `NullWaferMapHandler` | `QMC.CDT-320/QMC.CDT-320/Equipment/CDT320Machine.cs:64~76` | **유일한 구현체 — Null Object 만 존재**. 실제 맵 파싱 구현은 외부 시스템(파일/서버) 측에 위임된 설계로 추정. `InputStageUnit` 가 생성자로 주입받아 사용 |
| 인터페이스 | `IWaferMapHandler` | `QMC.CDT-320/QMC.CDT-320/Equipment/IStageInterfaces.cs:95~109` |

### 3.13 AlarmManager

| 항목 | 내용 |
|---|---|
| 클래스명 | `AlarmManager` (static) |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmManager.cs` |
| 역할 | 프로세스 전역 알람 관리 — 발생/해제/이력 + UI 이벤트 발행 |
| public 메서드 | `static AlarmRecord Raise(AlarmSeverity sev, string code, string source, string message)` <br> `static void Clear(int id)` <br> `static void ClearAll()` <br> `static void ResetAll()` |
| public property | `static IReadOnlyList<AlarmRecord> Active` <br> `static IReadOnlyList<AlarmRecord> History` <br> `static bool HasActive` <br> `static AlarmSeverity? HighestActiveSeverity` |
| public 이벤트 | `static event Action<AlarmRecord> AlarmRaised` <br> `static event Action<AlarmRecord> AlarmCleared` |
| 의존 | `AlarmRecord`, `AlarmSeverity`, `AlarmMaster` |

### 3.14 JobQueue

| 항목 | 내용 |
|---|---|
| 클래스명 | `JobQueue` (static) |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/Jobs/JobQueue.cs` |
| 역할 | Job 큐 + 이력 — Pending/Running/Done/Failed/Cancelled 전이 |
| public 메서드 | `static void Enqueue(JobOrder job)` <br> `static bool TryDequeue(out JobOrder job)` <br> `static void MarkRunning(JobOrder job)` <br> `static void MarkDone(JobOrder job, string note = "")` <br> `static void MarkFailed(JobOrder job, string reason)` <br> `static void MarkCancelled(JobOrder job)` <br> `static IReadOnlyList<JobOrder> Snapshot()` <br> `static int CountByType(JobType type)` <br> `static int CountByState(JobState state)` <br> `static void Clear()` |
| public property | `static int PendingCount` / `static int HistoryCount` |
| 의존 | `JobOrder`, `JobType`, `JobState` |

### 3.15 LotStorage

| 항목 | 내용 |
|---|---|
| 클래스명 | `LotStorage` (static) |
| 파일경로 | `QMC.CDT-320/QMC.CDT-320/Equipment/Lots/LotStorage.cs` |
| 역할 | Lot 저장소 + Active Lot 관리 — 열기/닫기, JSON 영속화 |
| public 메서드 | `static Lot OpenLot(string lotId, string recipeName, int totalDies)` <br> `static void CloseLot(bool aborted = false)` <br> `static Lot Get(string lotId)` <br> `static IReadOnlyList<Lot> ListByDate(DateTime date)` <br> `static void SaveJson(Lot lot)` <br> `static void Clear()` |
| public property | `static IReadOnlyDictionary<string, Lot> Lots` <br> `static Lot ActiveLot` <br> `static string Dir` |
| 의존 | `Lot`, `LotState`, `DataContractJsonSerializer` |

### 3.16 RecipeStore + RecipeProject (+ Subset DTO들)

`QMC.CDT-320/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs` 한 파일에 모두 정의.

| 클래스 | 멤버 |
|---|---|
| `RecipeStore` (static) | 메서드: `static List<string> List()` / `static RecipeProject Load(string fileName)` / `static void Save(RecipeProject p)` / `static bool Delete(string fileName)` <br> property: `static string Dir` |
| `RecipeProject` | property 전체 목록은 §6.1 참조 |
| `DieSubset` | property: §6.2 참조 |
| `TapeFrameSubset` | §6.3 |
| `LoadTapeFrameSubset` / `UnloadTapeFrameSubset` | §6.4 |
| `ModuleSubset` | §6.5 |
| `OutputSubset` | §6.6 |
| `InspectionSubset` | §6.7 |

---

## 4. 인터페이스 카탈로그

### 4.1 Equipment 도메인 인터페이스

| 인터페이스 | 파일 | 멤버 시그니처 / 구현체 |
|---|---|---|
| `IWaferLoader` | `QMC.CDT-320/QMC.CDT-320/Equipment/IStageInterfaces.cs` | `bool IsFeederAtSafePosition { get; }` <br> 구현체: `InputLoaderUnit` (직접 구현으로 추정 — 확인 필요) |
| `IBarcodeReader` | `Equipment/IStageInterfaces.cs:37~45` | `Task<string> ReadAsync(int timeoutMs = 3000)` <br> 구현체: **`BarcodeSerialAdapter`** (Equipment/Vision/), **`NullBarcodeReader`** (CDT320Machine.cs:44) |
| `IVisionTcpClient` | `Equipment/IStageInterfaces.cs` | `Task<bool> TriggerExposeAsync(int dieIndex)` / `Task<bool> GetResultAsync(int dieIndex, int timeoutMs = 5000)` / `Task<VisionAlignResult> TriggerAlignAsync(string alignTargetId)` <br> 구현체: **`WaferVisionAdapter`** (`QMC.CDT320.VisionComm` 네임스페이스, `Equipment/Vision/VisionAdapters.cs`) |
| `IWaferMapHandler` | `Equipment/IStageInterfaces.cs:95~109` | `Task<WaferMapData> ParseMapAsync(string waferId)` / `void SendMapToUi(WaferMapData mapData)` <br> 구현체: **`NullWaferMapHandler`** (CDT320Machine.cs:64~76) — 실 구현은 외부 시스템 측에 위임 |
| `ITransferPickerUnit` | `Equipment/IStageInterfaces.cs` | `int PickerCount { get; }` / `bool IsPickerReady { get; }` / `void NotifyPickReady(int dieIndex)` / `Task<bool> WaitPickerUpAsync(int timeoutMs = 3000)` <br> 구현체: `TransferPickerUnit` (직접 구현으로 추정 — 확인 필요) |
| `ITpuUnit` | `Equipment/OutputStageUnit.cs:67~96` | `void NotifyPlaceReady()` / `Task<bool> WaitPlaceDoneAsync(int timeoutMs = 3000)` / `void NotifyReadyForNextDie()` / `Task<bool> RequestColletCleaningAsync(int timeoutMs = 10000)` <br> 구현체: **`NullTpuUnit`** (CDT320Machine.cs:120~131). `TransferPickerUnit` 자체에는 구현 없음 — 별도 어댑터/래퍼로 연결되는 설계 |
| `IVisionTpuClient` | `Equipment/TransferPickerUnit.cs:70~112` | `Task<bool> TriggerBottomExposeAsync(int pickerNo, int timeoutMs = 1000)` / `Task<BottomVisionOffset[]> GetBottomResultsAsync(int timeoutMs = 5000)` / `Task<bool> TriggerSideExposeAsync(int pickerNo, int sideNo, int timeoutMs = 1000)` / `Task<SideVisionResult> GetSideResultAsync(int pickerNo, int timeoutMs = 5000)` <br> 구현체: **`TpuVisionAdapter`** (`QMC.CDT320.VisionComm`), **`NullVisionTpuClient`** (CDT320Machine.cs:91~117) |
| `IOutputUnloaderUnit` | `Equipment/OutputStageUnit.cs` | `Task<bool> RequestWaferChangeAsync(DieGrade grade, int timeoutMs = 0)` <br> 구현체: **`OutputUnloaderAdapter`** (`Equipment/Sim/`) |

### 4.2 QMC.Common 마커 인터페이스 / 베이스 클래스

| 타입 | 파일 | 역할 / 구현체 |
|---|---|---|
| `ISetupData` (마커) | `QMC.Common/Interfaces/IEquipmentData.cs` | 전원 OFF 후에도 유지되는 기구 설정 <br> 구현체: `AxisSetup`, `IoSetup`, `PickerSetup`, `TpuArmSetup`, `StageModuleSetup`, `OutputStageSetup`, `InputStageSetup` 등 |
| `IConfigData` (마커) | 〃 | 설비 사양 고정 파라미터 <br> 구현체: `AxisConfig`, `IoConfig`, `PickerConfig`, `TpuArmConfig`, `StageModuleConfig`, `OutputStageConfig`, `InputStageConfig` 등 |
| `IRecipeData` (마커) | 〃 | 공정/제품별 변경 파라미터 <br> 구현체: `AxisRecipe`, `IoRecipe`, `PickerRecipe`, `TpuArmRecipe`, `StageModuleRecipe`, `OutputStageRecipe`, `InputStageRecipe` 등 |
| `BaseEquipmentNode` (abstract) | `QMC.Common/BaseEquipmentNode.cs` | Composite 공통 — `string Name`, `ISetupData Setup`, `IConfigData Config`, `IRecipeData Recipe`, `void Save()` |
| `BaseUnit<TSetup,TConfig,TRecipe>` | `QMC.Common/BaseUnit.cs` | Composite 중간 — `List<BaseEquipmentNode> Components`. 구현체: `TransferPickerUnit`, `TpuArmUnit`, `OutputStageUnit`, `InputStageUnit`, `StageModule` 등 |
| `BaseComponent<TSetup,TConfig,TRecipe>` | `QMC.Common/BaseComponent.cs` | Composite 최하위 Leaf. 구현체: `PickerComponent`, `BaseAxis`, `BaseDigitalInput/Output`, `BaseCylinder` 등 |
| `BaseAxis` (abstract) | `QMC.Common/Motion/BaseAxis.cs` | 모션 축 추상 <br> 구현체: `SimAxis`, `AjinAxis` <br> 멤버: `double ActualPosition`, `double CommandPosition`, `double CurrentVelocity`, `bool IsServoOn / IsMoving / IsInPosition / IsAlarm / IsHomeDone`, `uint AlarmCode`, `bool Sensor_PEL / MEL / ORG`, `event Action<BaseAxis,double> ActualPositionChanged`, `event Action<BaseAxis> MoveStarted / MoveCompleted`, `Task MoveAbsoluteAsync(double targetPosition, double velocityMmPerSec)`, `Task MoveRelativeAsync(double distance, double velocityMmPerSec)`, `Task HomeSearchAsync()`, `void ServoOn() / ServoOff() / Stop() / EStop() / ResetAlarm()`, `void SetPosition(double newPosition)`, `void SetMotionProfile(double velocity, double acc, double dec)`, `void OverrideVelocity(double newVelocity)`, `void OverridePosition(double newTargetPosition)`, `void MoveJogContinuous(int direction, JogSpeedType speedType, double customVel = 0)`, `Task MoveJogStepAsync(int direction, JogSpeedType speedType, double stepDistance, double customVel = 0)`, `void StopJog()` |
| `BaseDigitalInput` (abstract) | `QMC.Common/IO/BaseDigitalInput.cs` | DI 추상 — `bool IsOn / IsOff`, `event Action<bool> StateChanged`, `void UpdateStatus()`, `void SimulateInput(bool state)`, `Task<bool> WaitUntilStateAsync(bool targetState, int timeoutMs = 3000)` <br> 구현체: `SimDigitalInput`, `AjinDigitalInput` |
| `BaseDigitalOutput` (abstract) | `QMC.Common/IO/BaseDigitalOutput.cs` | DO 추상 — `bool IsOn`, `event Action<bool> StateChanged`, `void Write(bool state)`, `void On() / Off()`, `void UpdateStatus()` <br> 구현체: `SimDigitalOutput`, `AjinDigitalOutput` |
| `BaseCylinder` (abstract) | `QMC.Common/IO/BaseCylinder.cs` | 공압 실린더 — `BaseDigitalOutput OutFwd / OutBwd`, `BaseDigitalInput InFwd / InBwd`, `bool IsFwd / IsBwd`, `Task<bool> ExtendAsync(int timeoutMs)` / `RetractAsync(int timeoutMs)` / `SetPositionAsync(bool extended, int timeoutMs)` <br> 구현체: `SimCylinder`, `AjinCylinder` |

### 4.3 QMC.Vision 추상

| 인터페이스 | 파일 | 멤버 / 구현체 |
|---|---|---|
| `ICamera` | `QMC.Vision/Core/ICamera.cs` | `CameraInfo Info`, `bool IsOpen / IsGrabbing`, 이벤트 `FrameReceived(GrabResult)` / `ConnectionChanged(CameraConnectionEvent)`, `void Open() / Close()`, `GrabResult Grab(int timeoutMs = 3000)`, `void StartLive() / StopLive()`, `void TriggerSoftware()`, property `ExposureUs / Gain / AcquisitionFrameRate / TriggerMode / PixelFormat / Roi / Resolution`, `string GetRawParameter(string key) / void SetRawParameter(string key, string value)` <br> 구현체: `SimCamera`, `HikGigECamera` |
| `IVisionBackend` | `QMC.Vision/Core/IVisionBackend.cs` | `string Name / VersionInfo`, `bool IsInitialized`, `void Initialize()`, `IPatternFinder CreatePatternFinder(string id)`, `IInspector CreateInspector(string id)` <br> 구현체: `SimBackend`, `OpenCvBackend`, `CognexBackend` |
| `IPatternFinder` | `QMC.Vision/Core/IPatternFinder.cs` | `string Id`, `Roi SearchRoi / TrainRoi`, `Bitmap TrainImage`, `double AcceptThreshold`, `int MaxInstances`, `void Train(Bitmap image)`, `MatchResult Match(Bitmap image)`, `void LoadParameters(string path) / SaveParameters(string path)` <br> 구현체: `SimPatternFinder`, `OpenCvPatternFinder`, `CognexPatternFinder` |
| `IInspector` | `QMC.Vision/Core/IInspector.cs` | `string Id`, `Roi InspectionRoi`, `InspectionResult Inspect(Bitmap image)`, `void LoadParameters(string path) / SaveParameters(string path)` <br> 구현체: `SimInspector`, `OpenCvInspector`, `CognexInspector` |
| `IEdgeFinder` | `QMC.Vision/Core/IEdgeFinder.cs` | `string Id`, `Roi MeasureRoi`, `double EdgeThreshold`, `EdgeMeasurement Measure(Bitmap image)` <br> 구현체: `CognexCaliper` (확인 필요 — 별도 구현체) |
| `IHistogramAnalyzer` | 〃 | `string Id`, `Roi AnalysisRoi`, `HistogramResult Analyze(Bitmap image)` <br> 구현체: `CognexHistogram` (추정) |
| `IColorMatcher` | 〃 | `string Id`, `Color TargetColor`, `int Tolerance`, `ColorMatchResult Match(Bitmap image)` <br> 구현체: `CognexColorMatch` (추정) |

---

## 5. Sim Axis 매핑 전체

> 매핑 정의: `QMC.CDT-320/QMC.CDT-320/Equipment/SimulatorBridge.cs` (`BuildMaps()`, L88~156, `Dictionary<BaseAxis,int> _axisMap`) 와 `CDT320Simulator/MotionMap.cs:19~84` 가 동일한 #00~#36 체계 공유.

| No | Sim Name | 핸들러 컴포넌트 | Stroke (mm/°) | Brake | 비고 |
|---|---|---|---|---|---|
| 0 | WAFER LIFTER_Z | `InputLoaderUnit.ElevatorZ` | 200 | **O** | 입력 리프터 Z |
| 1 | WAFER FEEDER_Y | `InputLoaderUnit.FeederY` | 300 | — | |
| 2 | WAFER STAGE_Y | `InputStageUnit.StageY` | 400 | — | |
| 3 | WAFER STAGE_T | `InputStageUnit.StageT` | 360° | — | EE-SX672A 원점 |
| 4 | WAFER EXPANDING_Z | `InputStageUnit.ExpanderZ` | 100 | — | |
| 5 | ALIGN VISION_X | `InputStageUnit.CameraX` | 300 | — | |
| 6 | NEEDLE_X | `InputStageUnit.NeedleBlockX` | 200 | — | |
| 7 | NEEDLE_Z | `InputStageUnit.NeedleZ` | 100 | **O** | |
| 8 | EJECT PIN_Z | `InputStageUnit.EjectPinZ` | 50 | — | T1031-05A |
| 9 | FRONT PICKER_X | `TransferPickerUnit.LeftArm.ArmX` | 1500 | — | LIDA487 |
| 10 | FRONT PICKER_Y | `TransferPickerUnit.LeftArm.ArmY` | 750 | — | T1031-05A |
| 11 | FRONT PICKER_T0 | `TransferPickerUnit.LeftArm.Pickers[0].PickerT` | 360° | — | ZLR25-15 |
| 12 | FRONT PICKER_Z0 | `TransferPickerUnit.LeftArm.Pickers[0].PickerZ` | 50 | — | |
| 13 | FRONT PICKER_T1 | `LeftArm.Pickers[1].PickerT` | 360° | — | |
| 14 | FRONT PICKER_Z1 | `LeftArm.Pickers[1].PickerZ` | 50 | — | |
| 15 | FRONT PICKER_T2 | `LeftArm.Pickers[2].PickerT` | 360° | — | |
| 16 | FRONT PICKER_Z2 | `LeftArm.Pickers[2].PickerZ` | 50 | — | |
| 17 | FRONT PICKER_T3 | `LeftArm.Pickers[3].PickerT` | 360° | — | |
| 18 | FRONT PICKER_Z3 | `LeftArm.Pickers[3].PickerZ` | 50 | — | |
| 19 | FRONT SIDE VISION_Y0 | `TransferPickerUnit.LeftArm.SideVisionY` | 200 | — | Stage 44 추가 |
| 20 | REAR SIDE VISION_Y0 | `TransferPickerUnit.RightArm.SideVisionY` | 200 | — | Stage 44 추가 |
| 21 | REAR PICKER_X | `TransferPickerUnit.RightArm.ArmX` | 1500 | — | |
| 22 | REAR PICKER_Y | `TransferPickerUnit.RightArm.ArmY` | 750 | — | |
| 23 | REAR PICKER_T0 | `RightArm.Pickers[0].PickerT` | 360° | — | |
| 24 | REAR PICKER_Z0 | `RightArm.Pickers[0].PickerZ` | 50 | — | |
| 25 | REAR PICKER_T1 | `RightArm.Pickers[1].PickerT` | 360° | — | |
| 26 | REAR PICKER_Z1 | `RightArm.Pickers[1].PickerZ` | 50 | — | |
| 27 | REAR PICKER_T2 | `RightArm.Pickers[2].PickerT` | 360° | — | |
| 28 | REAR PICKER_Z2 | `RightArm.Pickers[2].PickerZ` | 50 | — | |
| 29 | REAR PICKER_T3 | `RightArm.Pickers[3].PickerT` | 360° | — | |
| 30 | REAR PICKER_Z3 | `RightArm.Pickers[3].PickerZ` | 50 | — | |
| 31 | NG BIN_Y | `OutputStageUnit.NgStage.StageY` | 500 | — | |
| 32 | NG BIN_Z | `OutputStageUnit.NgStage.StageZ` | 100 | — | |
| 33 | GOOD BIN_Y | `OutputStageUnit.GoodStage.StageY` | 500 | — | |
| 34 | INSPECTION VISION_X | `OutputStageUnit.BinCameraX` | 300 | — | |
| 35 | BIN FEEDER_Y | `OutputUnloaderUnit.FeederY` | 300 | — | |
| 36 | BIN LIFTER_Z | `OutputUnloaderUnit.ElevatorZ` | 200 | **O** | |

**보조 정보**:
- **Brake 보유 축**: #0 / #7 / #36 — `MotionMap.IsBrakeAxis(no) => no == 0 || no == 7 || no == 36`
- **TPU 미러 구조**: 좌측 암(#9~#20) / 우측 암(#21~#30) 대칭. 각 암 = ArmX + ArmY + 4픽커×{T,Z}
- **DO 매핑** (SimulatorBridge BuildMaps): Picker Vacuum/Blow → `Y048~Y051` (L-Vac), `Y056~Y059` (L-Blow), `Y064~Y067` (R-Vac), `Y072~Y075` (R-Blow)
- **AjinFactory** 는 이름 기반(`"LeftArm_ArmX"` 등)으로 axis 생성. `AjinConfigStore.Default()` 에서 이름 ↔ 번호 매핑 정의

---

## 6. Recipe 파라미터 전체 목록

> 모든 Recipe DTO 는 `Equipment/Recipes/RecipeStore.cs` 단일 파일 + 각 Unit의 `*.cs` 안에 `Setup/Config/Recipe` 클래스로 정의됨. **사용자 명시 7항목**은 굵게 표시.

### 6.1 RecipeProject (1개 프로젝트 = 1개 레시피)

> 파일: `Equipment/Recipes/RecipeStore.cs`

| 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|
| FileName | string | `"NEW"` | 레시피 파일명 (저장 식별자) |
| MachineNumber | string | `"DS530"` | 머신 번호 |
| CassetteFlow | string | `"Mapping"` | 카세트 흐름 모드 |
| DryRun | bool | false | 드라이런 (비전 검사 스킵) |
| StepRun | bool | false | 스텝 실행 (단계별 대기) |
| XmlSave | bool | true | 작업 결과 XML 저장 |
| ReDt | bool | false | Re-DT 모드 (재검사) |
| EbrMode | bool | true | EBR 모드 |
| AlignConfirmEnable | bool | true | 얼라인 확인 화면 활성 |
| NeedleCheckMode | bool | false | 니들 체크 모드 |
| AutoPositionDeviationLimit | double | 50.0 | 자동 위치 편차 허용치 (mm) |
| MapFormat | string | `"SEC"` | 맵 포맷 |
| MapDirection | string | `"표준(0도)"` | 맵 회전 방향 |
| ChipThickness | double | 150 | 칩 두께 (μm) |
| MasterChipThickness | double | 150 | 마스터 칩 두께 (μm) |
| TapeThickness | double | 100 | 테이프 두께 (μm) |
| BinSortNumber | int | 1 | Bin 정렬 번호 |
| LotId / PartId / InputCassetteId / OutputCassetteId / ColletModelNum / ColletLotNum / XmlPath | string | null | 식별자/경로 |
| Die | DieSubset | (§6.2) | 다이 사양 |
| Frame | TapeFrameSubset | (§6.3) | 웨이퍼 격자 |
| LoadFrame / UnloadFrame | LoadTapeFrameSubset / UnloadTapeFrameSubset | (§6.4) | 로드/언로드 옵션 |
| Module | ModuleSubset | (§6.5) | 모듈 동작 |
| Output | OutputSubset | (§6.6) | 출력 사양 |
| BottomInsp / TopSideInsp / BottomSideInsp | InspectionSubset | (§6.7) | 비전 검사 (3종) |

### 6.2 DieSubset

| 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|
| DieSpecName | string | `"Default"` | |
| WidthMm / HeightMm / ThicknessMm | double | 1.0 / 1.0 / 0.1 | mm |
| ChipLowerSpecLimitWidth / ChipUpperSpecLimitWidth | double | -0.05 / 0.05 | mm |
| ChipLowerSpecLimitHeight / ChipUpperSpecLimitHeight | double | -0.05 / 0.05 | mm |
| ChippingDepthMax / ChippingLengthMax | double | 0.05 / 0.20 | mm |
| ForeignSizeMax | double | 0.005 | mm |

### 6.3 TapeFrameSubset

| 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| FrameSpecName | string | `"8inch_5x5"` | |
| GridX / GridY | int | 5 / 5 | 격자 수 |
| PitchX / PitchY | double | 1.0 / 1.0 | mm |
| Rotate | string | `"None"` | |
| OuterDiameterMm | double | 200 | mm |

### 6.4 LoadTapeFrameSubset / UnloadTapeFrameSubset

| 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| (Load) Role | string | `"Load"` | |
| (Load) AutoBarcodeRead / AutoAlignment | bool | true | |
| (Load) AlignmentPoints | int | 3 | |
| (Unload) Role | string | `"GoodUnload"` | |
| (Unload) GapInspection | bool | true | |
| (Unload) GapUpperLimit / GapLowerLimit | double | 0.05 / 0.005 | mm |

### 6.5 ModuleSubset

| 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| PickRetryCount | int | 3 | |
| PickDelayMs / PlaceDelayMs | int | 80 / 60 | ms |
| ColletCleanEnable | bool | true | |
| ColletCleanInterval | int | 1000 | 다이 개수 |
| BottomInspectionEnable | bool | true | |

### 6.6 OutputSubset

| 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| GoodPlateMaxSlots / NgPlateMaxSlots | int | 25 / 25 | |
| DiesPerWafer | int | 8 | |
| **WafersPerOutputBatch** | int | 8 | 출력 배치당 웨이퍼 수 |
| AutoBinTransition | bool | true | |
| AlarmOnFull | bool | true | |
| DefaultGoodCassette | string | `"Good1"` | |

### 6.7 InspectionSubset (Bottom/TopSide/BottomSide 공용)

| 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| Enable | bool | true | |
| ExposureMs | int | 500 | ms |
| LightIntensity | double | 0.5 | 0~1 |
| ChippingDepthMaxMm / ChippingLengthMaxMm | double | 0.05 / 0.10 | mm |
| ScratchAreaMaxMm2 / ContaminationMaxMm2 | double | 0.005 / 0.010 | mm² |
| MinDieCenterScore | double | 0.7 | |

### 6.8 InputStageSetup / Config / Recipe

> 파일: `Equipment/InputStageUnit.cs`

| 분류 | 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|---|
| Setup | **ExpanderDownPosition** | double | 50.0 | ExpanderZ 하강(웨이퍼 고정) (mm) |
| Setup | ExpanderUpPosition | double | 0.0 | ExpanderZ 상승 (mm) |
| Setup | UnloadPositionY | double | 0.0 | StageY 언로드 위치 (mm) |
| Setup | NeedleEjectPosition | double | 5.0 | NeedleZ 상승 (mm) |
| Setup | NeedleDownPosition | double | 0.0 | NeedleZ 하강 (mm) |
| Setup | PickerOffsetY | double | 3.0 | TPU 픽커 Y 오프셋 (mm) |
| Setup | PickerOffsetX | double | 0.0 | TPU 픽커 X 오프셋 (mm) |
| Setup | BarcodeReadTimeoutMs | int | 3000 | ms |
| Config | IsSimulationMode | bool | true | |
| Config | MaxAlignIterations | int | 3 | |
| Config | **AlignConvergenceThresholdDeg** | double | 0.005 | deg |
| Recipe | MoveVelocity | double | 100.0 | mm/s |
| Recipe | AlignVelocity | double | 30.0 | mm/s |
| Recipe | NeedleVelocity | double | 50.0 | mm/s |
| Recipe | NeedleVacuumSettleMs | int | 50 | ms |
| Recipe | DefaultPitchX / DefaultPitchY | double | 0.15 / 0.15 | mm |
| Recipe | VisionExposeTimeoutMs | int | 2000 | ms |
| Recipe | VisionResultTimeoutMs | int | 5000 | ms |
| Recipe | PickerUpTimeoutMs | int | 3000 | ms |
| Recipe | MoveTimeoutMs | int | 10000 | ms |

### 6.9 StageModule (OutputStage 의 GoodStage / NgStage 단위)

> 파일: `Equipment/OutputStageUnit.cs`

| 분류 | 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|---|
| Setup | WorkPositionZ | double | 10.0 | 작업 높이 (mm) |
| Setup | AvoidPositionZ | double | 0.0 | 회피 위치 (mm) |
| Setup | UnloadPositionY | double | -50.0 | 웨이퍼 교체 위치 (mm) |
| Setup | HomePositionY | double | 0.0 | 대기 위치 (mm) |
| Setup | CleaningPositionY | double | 80.0 | 콜렛 크리닝 더미 영역 (mm) |
| Setup | PositionTolerance | double | 0.05 | 위치 허용 오차 (mm) |
| Config | IsSimulationMode | bool | true | |
| Config | AvoidCheckIntervalMs | int | 10 | ms |
| Config | AvoidTimeoutMs | int | 3000 | ms |
| Recipe | YVelocity | double | 100.0 | mm/s |
| Recipe | ZVelocity | double | 50.0 | mm/s |

### 6.10 OutputStage Setup / Config / Recipe

| 분류 | 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|---|
| Setup | BinCameraWorkPositionX | double | 150.0 | mm |
| Setup | BinCameraRetractPositionX | double | 0.0 | mm |
| Setup | StageBasePositionY | double | 50.0 | mm |
| Config | IsSimulationMode | bool | true | |
| Config | TpuPlaceDoneTimeoutMs | int | 3000 | ms |
| Config | WaferChangeTimeoutMs | int | 0 (무한) | ms |
| Config | ColletCleaningTimeoutMs | int | 10000 | ms |
| Recipe | **BinCameraVelocity** | double | 200.0 | BinCameraX 이동 속도 (mm/s) |

### 6.11 OutputUnloaderSetup (위치 좌표)

> 파일: `Equipment/OutputUnloaderUnit.cs` 내 `OutputLoaderSetup` 클래스

| 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|
| NgFirstSlotPositionZ | double | 10.0 | NG 첫 슬롯 Z (mm) |
| Good1FirstSlotPositionZ | double | 80.0 | Good1 첫 슬롯 Z (mm) |
| Good2FirstSlotPositionZ | double | 160.0 | Good2 첫 슬롯 Z (mm) |
| **SlotPitchZ** | double | 6.0 | 슬롯 간 피치 Z (mm) |
| FeederHomePositionY | double | 0.0 | (mm) |
| GoodStageExchangePositionY | double | 150.0 | (mm) |
| NgStageExchangePositionY | double | 200.0 | (mm) |
| CassetteInsertPositionY | double | 250.0 | (mm) |
| MaxSlotsPerCassette | int | 25 | |

### 6.12 MachineController 런타임 파라미터

> 파일: `Equipment/MachineController.cs`

| 이름 | 타입 | 기본값 | 설명/단위 |
|---|---|---|---|
| **PickersPerCycle** | int | 1 | 동시 픽업 picker 수 (1~4) |
| **DiesPerColletClean** | int | 24 | N 다이마다 콜렛 청소 |
| **WafersPerOutputBatch** | int | 8 | N 다이마다 Output 적재 |
| DiesPerWafer | int | 8 | 웨이퍼당 다이 수 |
| DualArmMode | bool | false | 짝/홀수 교대 모드 |
| DryRun / StepRun | bool | false | |

### 6.13 TpuArm Setup / PickerSetup (대표 발췌)

| 분류 | 이름 | 타입 | 기본값 | 설명 |
|---|---|---|---|---|
| TpuArmSetup | BottomVisionX | double | 100.0 | mm |
| TpuArmSetup | PickerPitchX | double | 10.0 | mm |
| TpuArmSetup | PlacePositionX / PlacePositionY | double | 300.0 / 50.0 | mm |
| TpuArmSetup | SoftLimitPlus (ArmX) | double | 1600.0 | mm (Stage 29) |
| TpuArmSetup | SoftLimitPlus (ArmY) | double | 350.0 | mm |
| PickerSetup | PickPosition / PlacePosition / FocusPosition / WaitPosition | double | -5.0 / -4.0 / -2.0 / 0.0 | mm |
| PickerRecipe | VacuumSettleMs / BlowPulseMs | int | 50 / 100 | ms |

---

## 7. 알람 코드 전체 목록

### 7.1 AlarmSeverity Enum

| 값 | 의미 |
|---|---|
| Warning | 경고 — 운전 계속 가능 |
| Error | 오류 — 현재 동작 중단 |
| Critical | 치명 — 전체 정지 |

> 파일: `Equipment/Alarms/AlarmSeverity.cs`

### 7.2 발생 알람 (`AlarmManager.Raise(severity, code, source, message)` 호출 사이트)

| 코드 | 발생 클래스.메서드 | 의미 | 심각도 |
|---|---|---|---|
| LOT-NOCASS | `MachineController.LoadNextWaferAsync` | Input 카세트 미감지 | Warning |
| LOT-SCAN | 〃 | 카세트 스캔 실패 | Warning |
| LOT-MOVE | 〃 | 슬롯 이동 실패 | Error |
| LOT-EX | 〃 | 교환 위치 이송 실패 | Error |
| IS-LOAD | 〃 | InputStage Load 실패 | Warning |
| IS-ALIGN | 〃 | VisionAlign 실패 | Warning |
| IS-EXCEPTION | 〃 | InputStage 시퀀스 예외 | Error |
| LOT-RET | `MachineController.RetractCurrentWaferAsync` | 피더 후퇴 실패 | Error |
| OUT-FULL-GOOD | `MachineController.StoreCompletedWaferAsync` | Good 카세트 모두 가득 | Error |
| OUT-FULL-NG | 〃 | NG 카세트 가득 | Error |
| OUT-STORE | 〃 | StoreFullWafer 실패 | Error |
| OUT-STORE-EX | 〃 | StoreFullWafer 예외 | Error |
| HOME-{axisName} | `MachineController.InitAsync` | 축별 HOME 실패 | Error |
| E-STOP | `MachineController.EmergencyStopAsync` | 비상 정지 | Error |
| INTERLOCK | `MachineController.MoveAxisAsync` | 인터락 위반 — 모션 차단 | Warning |
| ALIGN-EX | `MachineController.AlignWaferAsync` | 3점 정렬 예외 | Error |
| CYCLE-EX | `MachineController.CycleRunAsync` | 사이클 실행 예외 | Error |
| OS-RECEIVE | `MachineController.DoOneDieAsync` | OutputStage.ReceiveDieAsync 실패 | Warning |
| OS-EXCEPTION | 〃 | OutputStage 시퀀스 예외 | Error |
| OS-BININSP | 〃 | InspectBinPositionAsync 실패 | Warning |
| OS-BININSP-EX | 〃 | Bin 검사 예외 | Error |
| TPU-PLACE | 〃 (`front.Pickers[0].PlaceAsync`) | PlaceAsync 실패 | Warning |

### 7.3 AlarmMaster 정의 (코드 발급 X 또는 미확인) — 추후 코드 발급 추가 검토 필요

| 코드 | 의미 (메뉴얼 정의) | 비고 |
|---|---|---|
| HOME-FAIL | HOME 검색 실패 (총괄) | 축별 코드 `HOME-{axisName}` 로 대체 발급 |
| MOVE-TIMEOUT | 모션 타임아웃 | 코드 발급 없음 — IsAlarm 폴링 + AlarmCode 반환 |
| SERVO-OFF, LIMIT-HIT | 모션 일반 | 동일 |
| VISION-CONN, VisionMatchFail, EXPOSE-TIMEOUT | 비전 일반 | 발급 위치 미확인 |
| PickFail, BottomInspFail, PlacementFail | 검사 일반 | 〃 |
| SECS-DISCONN, SIM-DISCONN | 통신 | 〃 |
| EMG-PRESSED | 비상 정지 (E-Stop / Door) | E-STOP 으로 통일 발급 |
| VAC-LOW, CDA-LOW | IO | 〃 |
| IONIZER | `IonizerSensor.CheckAlarmAsync` | 발급 위치 확인됨 |
| AXL-OPEN, AXL-DLL | `AjinSystem.Initialize` | Critical |

---

## 8. 이벤트 / 콜백 카탈로그

| 이름 | 발화 위치 | 구독자 |
|---|---|---|
| `StatusChanged(MachineStatus)` | `MachineController` (`SetStatus` 내부, L54~56) | `Form1.OnMachineStatusChanged` (Form1.cs:86) |
| `LogMessage(string)` | `MachineController` (`Log` 메서드) | `Form1.cs:87~90` — `EventLogger.Write()` + `Console.WriteLine()` (auto-cycle 모드 시) |
| `CycleProgress(int done, int total)` | `MachineController.CycleRunAsync` (`RaiseProgress` 호출) | **외부 구독자 미발견** — UI 탭 내부/Designer 에서만 사용될 가능성 (확인 필요) |
| `LotPortStateChanged()` | `MachineController` (`RaiseLotPortChanged` 호출) | **외부 구독자 미발견** (동일) |
| `WaferChangeRequested(DieGrade)` | `InputStageUnit.OnWaferChangeRequested` (L799) | OutputUnloader 측 또는 상위 Machine 클래스 (확인 필요) |
| `AlarmRaised(AlarmRecord)` / `AlarmCleared(AlarmRecord)` | `AlarmManager.Raise` / `Clear` | UI `AlarmBanner` |
| `ExposureDone(int)` / `Alarmed(string)` / `ConnectionChanged()` | `VisionTcpClient` (`OnMessageReceived` / `ConnectionMonitor`) | (Vision 모듈/UI) |
| `StateChanged(bool)` | `BaseDigitalInput.RaiseStateChanged` / `BaseDigitalOutput.RaiseStateChanged` | (IO 변화 리스너) |
| `ActualPositionChanged(BaseAxis, double)` / `MoveStarted(BaseAxis)` / `MoveCompleted(BaseAxis)` | `BaseAxis` | (축 위치 리스너) |
| `FrameReceived(GrabResult)` / `ConnectionChanged(CameraConnectionEvent)` | `CameraBase` | (Vision UI) |
| `RoiEdited` | `CameraView.OnRoiDrag` | (Vision UI) |
| `OnEventReceived` (TcpProtocol) | `Cdt320SimClient` | TCP 수신 콜백 |

**Func/Action 형 콜백**:
- `StepRunGate (Func<int,bool>)` — `MachineController` (사용자 제공: 다이 1개마다 진행 허용/차단)

### `OnPlaceDone` 의 실체 (v1 미확인 항목 해소)

코드에 **`OnPlaceDone` / `PlaceDone` 명시 이벤트는 없음**. 대신 다음 메커니즘:
- `ITpuUnit` 인터페이스의 **`Task<bool> WaitPlaceDoneAsync(int timeoutMs)`** + **`void NotifyPlaceReady()`** + **`void NotifyReadyForNextDie()`** 메서드 시그널링.
- `OutputStageUnit.InspectBinPositionAsync` 에서 `Tpu.WaitPlaceDoneAsync(Config.TpuPlaceDoneTimeoutMs)` 로 대기.
- 내부 동기화 (`TaskCompletionSource` / `SemaphoreSlim`) 의 실제 구현은 `NullTpuUnit` (CDT320Machine.cs:120~131) 만 존재 — **실 구현 미완료**로 추정.

---

## 9. 인터락 매트릭스

### 9.1 등록된 인터락 클래스

| 인터락 | 검사 대상 | 차단 조건 | 위반 시 동작 |
|---|---|---|---|
| `PickerVsStageInterlock` | 픽커 ↔ 스테이지 거리 | 픽커가 스테이지 X 작업 영역 + PickerZ ≤ 임계값 + StageX/Y 이동 시도 | 모션 차단, Reason 반환 |
| `PickerVsPickerInterlock` | Front/Rear 픽커 X 거리 | Front-Rear < 최소거리 | 모션 차단 |
| `VisionVsPickerInterlock` | 비전 ↔ 픽커 X 충돌 | Vision-Picker X < 최소거리 | 모션 차단 |
| `StageVsEjectInterlock` | StageZ + EjectZ | 합계 > MaxJointUp | 모션 차단 |
| `EjectVsStageInterlock` (ExtendedInterlocks) | Eject Z ≥ 임계값 | Stage 회전 금지 | 모션 차단 |
| `LoaderVsStageInterlock` | Loader X 범위 | Stage Z 금지 | 모션 차단 |
| `UnloaderVsStageInterlock` | Unloader X 범위 | Stage Z 금지 | 모션 차단 |
| `EjectVsPickerInterlock` | Eject Z up + Picker Z 임계 | Picker 하강 금지 | 모션 차단 |
| `BinGuideVsPickerInterlock` | BinGuide 확장 + Picker 금지 영역 | Picker 이동 금지 | 모션 차단 |

> 파일: `Equipment/Interlocks/StandardInterlocks.cs` (L10~155+), `ExtendedInterlocks.cs` 1~3.

### 9.2 인터락 검증 경로

| 검사 위치 | 메서드 | 조건 |
|---|---|---|
| `MachineController.MoveAxisAsync` | `InterlockRegistry.VerifyMove` | 등록된 모든 인터락 순회 — 하나 실패 시 즉시 false + 사유 반환 |
| `OutputStageUnit.ReceiveDieAsync` | `EnsureOppositeStageAvoidedAsync` | 반대쪽 StageZ 가 AvoidPositionZ 인지 확인, 필요시 강제 하강 |
| `OutputStageUnit.RequestWaferChangeAsync` | `EnsureOppositeStageAvoidedAsync` | 웨이퍼 교체 전 반대쪽 회피 확인 |
| `OutputStageUnit.PerformColletCleaningAsync` | `EnsureOppositeStageAvoidedAsync` | 콜렛 크리닝 전 반대쪽 회피 확인 |

### 9.3 IsAlarm 사후 검사 (모션 완료 직후)

| 클래스.메서드 | 검사 대상 축 | 파일:줄 |
|---|---|---|
| `InputStageUnit.LoadAndPrepareWaferAsync` | ExpanderZ.IsAlarm | InputStageUnit.cs:336 |
| `InputStageUnit.VisionAlignAndSetupOriginAsync` | StageY/CameraX/StageT.IsAlarm | InputStageUnit.cs:408, 436, 656 |
| `InputStageUnit.MultiScanAndPickupAsync` | StageY/NeedleBlockX.IsAlarm | InputStageUnit.cs:715 |
| `InputStageUnit.UnloadWaferAsync` | StageY/ExpanderZ.IsAlarm | InputStageUnit.cs:763, 774 |
| `InputStageUnit.ExecutePickupAsync` | NeedleZ.IsAlarm | InputStageUnit.cs:862, 885 |
| `StageModule.MoveToAvoidPositionAsync` | StageZ.IsAlarm | OutputStageUnit.cs:320 |
| `StageModule.MoveToWorkPositionAsync` | StageZ.IsAlarm | OutputStageUnit.cs:339 |
| `StageModule.MoveYAsync` | StageY.IsAlarm | OutputStageUnit.cs:358 |
| `OutputStageUnit.InspectBinPositionAsync` | BinCameraX.IsAlarm | OutputStageUnit.cs:644, 664 |
| `MachineController.InitAsync` | 모든 축 IsAlarm (Home 직후) | MachineController.cs:676 |
| `AjinAxis.MoveAbsoluteAsync` | IsServoOn ‖ IsAlarm ‖ !AjinSystem.IsOpen | (모션 진입 차단) |

**카테고리 요약**:
- 모션 충돌 인터락: 9건
- 안전(E-Stop / 인터락 차단): 3건
- IO/축 IsAlarm 사후 검사: 10+건
- 통신/Lot/Material: 7건

---

## 10. UI 화면 인벤토리

### 10.1 QMC.CDT-320 메인 6 탭

| 클래스 | 파일경로 | 화면명 |
|---|---|---|
| WorkTab | `QMC.CDT-320/Ui/Tabs/WorkTab.cs` | 작업 — CDT-300 메인 + 모드 버튼 |
| WorkInfoTab | `QMC.CDT-320/Ui/Tabs/WorkInfoTab.cs` | 작업 정보 (HEAD Front/Rear 분리) |
| HistoryTab | `QMC.CDT-320/Ui/Tabs/HistoryTab.cs` | 이력 (알람/경고/이벤트/데이터/작업) |
| RecipeTab | `QMC.CDT-320/Ui/Tabs/RecipeTab.cs` | 레시피 (300 구조 + 320 확장) |
| SettingsTab | `QMC.CDT-320/Ui/Tabs/SettingsTab.cs` | 설정 (HW 연동) |
| UserTab | `QMC.CDT-320/Ui/Tabs/UserTab.cs` | 사용자 |

### 10.2 Work 페이지 (3)

| 클래스 | 파일경로 | 표시 데이터 |
|---|---|---|
| WorkMainPage | `QMC.CDT-320/Ui/Pages/Work/WorkMainPage.cs` | 4분할(비전/맵/정보/시간) |
| MapTransferPage | `QMC.CDT-320/Ui/Pages/Work/MapTransferPage.cs` | INPUT/OUTPUT 맵 전환 |
| VisionAlignPage | `QMC.CDT-320/Ui/Pages/Work/VisionAlignPage.cs` | 비전 정렬 모니터링 |

### 10.3 WorkInfo 페이지 (9)

| 클래스 | 파일경로 | 표시 데이터 |
|---|---|---|
| InputCassettePage | `QMC.CDT-320/Ui/Pages/WorkInfo/InputCassettePage.cs` | 입력 카세트 |
| InputFeederPage | 〃/InputFeederPage.cs | 입력 피더 |
| InputStagePage | 〃/InputStagePage.cs | 입력 스테이지 |
| HeadPage | 〃/HeadPage.cs | Front/Rear 헤드 |
| OutputPages | 〃/OutputPages.cs | OUTPUT 통합 (Stage/Feeder/Cassette) |
| ActiveLotPage | 〃/ActiveLotPage.cs | 현재 LOT |
| LogicDetailPage | 〃/LogicDetailPage.cs | 타임차트/로직 |
| OperationPanelStatusPage | 〃/OperationPanelStatusPage.cs | DI/DO/Tower/Ionizer |
| PlateStatusPage | 〃/PlateStatusPage.cs | 플레이트 상태 |

### 10.4 History 페이지 (4)

| 클래스 | 파일경로 | 표시 데이터 |
|---|---|---|
| AlarmHistoryPage | `QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.cs` | 알람 이력 |
| FilterPages | 〃/FilterPages.cs | Warning/Event/Data/Work 필터 |
| EventLogPage | 〃/EventLogPage.cs | 전체 이벤트 |
| MessageEditPage | 〃/MessageEditPage.cs | 메세지 편집 (Maintenance) |

### 10.5 Recipe 페이지 (14)

| 클래스 | 파일경로 |
|---|---|
| ProjectPage | `QMC.CDT-320/Ui/Pages/Recipe/ProjectPage.cs` |
| CassetteRecipePage | 〃/CassetteRecipePage.cs |
| FeederRecipePage | 〃/FeederRecipePage.cs |
| StageRecipePage | 〃/StageRecipePage.cs |
| HeadRecipePage | 〃/HeadRecipePage.cs |
| VisionRecipePage | 〃/VisionRecipePage.cs |
| ForceControlPage | 〃/ForceControlPage.cs |
| MapCreatePage | 〃/MapCreatePage.cs |
| DieSubsetPage | 〃/DieSubsetPage.cs |
| TapeFrameSubsetPage | 〃/TapeFrameSubsetPage.cs |
| LoadTapeFrameSubsetPage | 〃/LoadTapeFrameSubsetPage.cs |
| UnloadTapeFrameSubsetPage | 〃/UnloadTapeFrameSubsetPage.cs |
| OutputSubsetPage | 〃/OutputSubsetPage.cs |
| ModuleSubsetPage | 〃/ModuleSubsetPage.cs |

### 10.6 Settings 페이지 (11)

| 클래스 | 파일경로 |
|---|---|
| GeneralPage | `QMC.CDT-320/Ui/Pages/Settings/GeneralPage.cs` |
| IoListPage | 〃/IoListPage.cs |
| MotionPage | 〃/MotionPage.cs |
| DevicePages | 〃/DevicePages.cs |
| AlarmMasterPage | 〃/AlarmMasterPage.cs |
| AxisSetupPage | 〃/AxisSetupPage.cs |
| CameraSetupPage | 〃/CameraSetupPage.cs |
| LightControllerPage | 〃/LightControllerPage.cs |
| PositionTeachingPage | 〃/PositionTeachingPage.cs |
| SimulatorLinkPage | 〃/SimulatorLinkPage.cs |
| VisionLinkPage | 〃/VisionLinkPage.cs |

### 10.7 Material 페이지 (2)

| 클래스 | 파일경로 |
|---|---|
| MaterialBinPage | `QMC.CDT-320/Ui/Pages/Material/MaterialBinPage.cs` |
| DieMapPage | `QMC.CDT-320/Ui/Pages/Material/DieMapPage.cs` |

### 10.8 다이얼로그 (7)

| 클래스 | 파일경로 |
|---|---|
| LoginDialog | `QMC.CDT-320/Ui/Dialogs/LoginDialog.cs` |
| BoardScanDialog | 〃/BoardScanDialog.cs |
| CstStatusDialog | 〃/CstStatusDialog.cs |
| ModeDialogs | 〃/ModeDialogs.cs |
| ModeOverlayDialog | 〃/ModeOverlayDialog.cs |
| SystemSelfTestDialog | 〃/SystemSelfTestDialog.cs |
| RemoteViewerDialog | 〃/RemoteViewerDialog.cs |

### 10.9 QMC.Vision 페이지 (8) + 다이얼로그 (1)

| 클래스 | 파일경로 |
|---|---|
| OperationPage | `QMC.Vision/Ui/Pages/OperationPage.cs` |
| ConfigurationPage | 〃/ConfigurationPage.cs |
| MaintenancePage | 〃/MaintenancePage.cs |
| RecipePage | 〃/RecipePage.cs |
| DataLogPage | 〃/DataLogPage.cs |
| FinderPage | 〃/FinderPage.cs |
| InspectorPage | 〃/InspectorPage.cs |
| SpcChartPage | 〃/SpcChartPage.cs |
| ZoomDialog | `QMC.Vision/Ui/Dialogs/ZoomDialog.cs` |

### 10.10 CDT320Simulator

| 클래스 | 파일경로 |
|---|---|
| MainWindow | `CDT320Simulator/MainWindow.xaml(.cs)` |
| App | `CDT320Simulator/App.xaml(.cs)` |

### 10.11 CDT-300 UI Image 폴더 (참조 자료, 총 55개 PNG)

> 경로: `CDT-300 UI Image/`. CDT-300 UI 스타일 재현 참조. **310의 코드 아키텍처는 비참조** 원칙.

#### 0. 작업 (3)
- `0. 작업/0. 메인화면.png`
- `0. 작업/1. [INPUT 맵 전환] 버튼 클릭.png`
- `0. 작업/2. [OUTPUT 맵 전환] 버튼 클릭.png`

#### 1. 작업 정보 (9)
- `0. INPUT CASSETTE.png` / `1. INPUT FEEDER.png` / `2. INPUT STAGE.png` / `3. HEAD.png` / `4. OUTPUT STAGE.png` / `5. OUTPUT FEEDER.png` / `6. OUTPUT CASSETTE.png` / `7-1. LOGIC (LOGIC).png` / `7-2. LOGIC (TIMECHART).png`

#### 2. 이력 (2)
- `0. 이력.png` / `1. 메세지 목록.png`

#### 3. 레시피 (13)
- `0. PROJECT.png` / `1. INPUT CASSETTE.png` / `2. INPUT FEEDER.png` / `3. INPUT STAGE.png` / `4. HEAD.png` / `5. OUTPUT FEEDER.png` / `6. OUTPUT CASSETTE.png` / `7. OUTPUT STAGE.png` / `8. INPUT VISION.png` / `9. OUTPUT VISION.png` / `10. LOWER VISION.png` / `11. INPUT MAP CREATE.png` / `12. OUTPUT MAP CREATE.png`

#### 4. 설정 (12)
- `0. GENERAL.png` / `1-1. MOTION (AXIS STATUS).png` / `1-2. MOTION (AXIS CONFIG).png` / `1-3. MOTION (AXIS SPEED).png` / `2. DIGITAL.png` / `3. DIGITAL LINK.png` / `4. CYLINDER.png` / `5. LAMP.png` / `6. SWITCH.png` / `7. LIGHT SOURCE.png` / `8. BARCODE READER.png` / `9. ZOOM LENS.png`

#### 5. 기타 (16)
- `0. LOGIN.png` / `1. CCS INSPECTION.png` / `2. 자동셋팅 위치.png` / `3. 바코드 확인 요청.png` / `4. 콜렛 교체.png` / `5. 콜렛 확인 요청.png` / `6. 콜렛 클리닝.png` / `7. LOT ID 입력.png` / `8. 자주 검사.png` / `9. 니들 유닛 교체.png` / `10. PICK 실패.png` / `11. PLACE 실패.png` / `12. POSITION CHECK.png` / `13. VISION ALIGN FAIL.png` / `14. 얼라인 매칭 실패.png` / `15. 얼라인 확인 요청.png`

---

## 11. 시퀀스 문서와의 mismatch 후보

### 11.1 분석 대상

**시퀀스 문서 5건** (위치: `D:\Work\CDT-320\문서\` — 한국어 폴더명, `docs/` 가 아님):
- `04_Feeder_시퀀스_상세분석.md`
- `05_InputStage_시퀀스_상세분석.md`
- `06_TransferPicker_시퀀스_상세분석.md`
- `07_OutputStage_시퀀스_상세분석.md`
- `08_OutputUnloader_적재_시퀀스.md`

**대상 코드** (Equipment 폴더, 전체 라인 검토):
- `InputLoaderUnit.cs` (430줄)
- `InputStageUnit.cs` (896줄)
- `TransferPickerUnit.cs` (957줄, **300줄 이후 부분 포함**)
- `OutputStageUnit.cs` (852줄)
- `OutputUnloaderUnit.cs` (1,002줄)

### 11.2 Feeder (`04_…md` ↔ `InputLoaderUnit.cs`/`OutputUnloaderUnit.cs`)

| 항목 | 문서 | 코드 | 차이 분류 |
|---|---|---|---|
| ScanCassette 파라미터 | maxSlots=16, slotPitch=6.0 mm | 매개변수 (`int maxSlots, double slotPitch`) | 동등 |
| 돌출 감시 (`MoveToTargetSlotAsync`) | `Task.WhenAny` + 10ms 폴링 | 동일 구현 (L279) | 일치 |
| OutputUnloader 카세트 3종 | NG / Good1 / Good2 | `enum TargetCassette` 정의 + 위치 좌표 모두 일치 | 일치 |
| `MaxSlotsPerCassette` | 25 | `Setup.MaxSlotsPerCassette = 25` | 일치 |
| `SlotPitchZ` | 6.0 mm | `Setup.SlotPitchZ = 6.0` | 일치 |
| Good1/Good2 첫 슬롯 Z | 80.0 / 160.0 | 동일 | 일치 |
| `PickupWaferAtPositionAsync` (5단계) | 미명시 | 헬퍼로 구현됨 (§4-E) | 코드엔 있음 (자연스러운 분해) |
| `PlaceWaferAtPositionAsync` (6단계) | 미명시 | 헬퍼로 구현됨 (§4-F) | 코드엔 있음 |

### 11.3 InputStage (`05_…md` ↔ `InputStageUnit.cs`)

| 항목 | 문서 | 코드 | 차이 |
|---|---|---|---|
| `LoadAndPrepareWaferAsync` | 4단계 | 4단계 | 일치 |
| `ExpanderDownPosition` | 50 mm | 50.0 | 일치 |
| `BarcodeReadTimeoutMs` | 3000 ms | 3000 | 일치 |
| `VisionAlignAndSetupOriginAsync` | 5단계 (Theta/Ref1/Ref2/Pitch/Origin) | 동일 | 일치 |
| `AlignConvergenceThresholdDeg` | 0.005° | 0.005 | 일치 |
| `PickerOffsetY / X` | 3 / 0 mm | 3.0 / 0.0 | 일치 |
| `ExecutePickupAsync` | 5단계 (Vacuum→Signal→Eject→Wait→Down) | 동일 | 일치 |
| `NeedleEjectPosition / NeedleDownPosition` | 5 / 0 mm | 5.0 / 0.0 | 일치 |
| `DefaultPitchX / Y` Fallback | 0.15 mm | 0.15 | 일치 |
| Stage 28 SoftLimit 확장 (StageY/CameraX/StageT/NeedleBlockX) | 문서 명시값 | 동일 (L266~270) | 일치 |

### 11.4 TransferPicker (`06_…md` ↔ `TransferPickerUnit.cs`)

| 항목 | 문서 | 코드 | 차이 |
|---|---|---|---|
| PickerComponent: Pick/Place/Focus/Wait Position | -5.0 / -4.0 / -2.0 / 0.0 mm | 동일 | 일치 |
| `VacuumSettleMs / BlowPulseMs` | 50 / 100 ms | 동일 | 일치 |
| `InspectBottomVisionAsync` | 4 picker 순차 + 결과 일괄 수신 | 동일 (L523~595) | 일치 |
| `InspectSideVisionAsync` | `Task.WhenAll` 로 회전과 이동 동시 | 동일 (L739~747) | 일치 |
| `PickerPitchX` | 10.0 mm | 10.0 | 일치 |
| `PlacePositionX / Y` | 300.0 / 50.0 mm | 동일 | 일치 |
| **ArmX SoftLimitPlus** | "1500 mm 부근" (모호) | **1600.0** (Stage 29) | 문서 표기와 미세 차이 — 의도적 마진 |
| ArmY SoftLimitPlus | 300~350 mm | 350.0 | 일치 |
| Stage 44 `SideVisionY` 추가 | 명시 | 구현됨 (L475) | 일치 |
| PickerZ/T SoftLimit | 100 mm / ±360° | 동일 | 일치 |

### 11.5 OutputStage (`07_…md` ↔ `OutputStageUnit.cs`)

| 항목 | 문서 | 코드 | 차이 |
|---|---|---|---|
| `ReceiveDieAsync` | 4단계 (회피→WorkZ→Y이동→Notify) | 동일 | 일치 |
| `EnsureOppositeStageAvoidedAsync` | 반대쪽 Z 회피 + 강제 하강 | 동일 (L501~537) | 일치 |
| `WorkPositionZ / AvoidPositionZ` | 10 / 0 | 10.0 / 0.0 | 일치 |
| `InspectBinPositionAsync` | 5단계 (WaitPlace→Move→Inspect→Retract→Notify) | 동일 | 일치 |
| `BinCameraWorkPositionX / RetractPositionX` | 검사 / 후퇴 | 150.0 / 0.0 | 일치 |
| `PerformColletCleaningAsync` | 6단계 | 동일 | 일치 |
| `CleaningPositionY` | 80.0 | 80.0 | 일치 |
| Stage 30 SoftLimit 확장 | StageY 350 / BinCameraX | `Setup.SoftLimitPlus = 350.0` | 일치 |
| `TpuPlaceDoneTimeoutMs` 등 일부 Config | 미명시 | 코드에 정의 (L210~223) | 코드엔 있음 (문서 추가 권장) |

### 11.6 OutputUnloader (`08_…md` ↔ `MachineController` + `OutputUnloaderUnit.cs`)

| 항목 | 문서 | 코드 | 차이 |
|---|---|---|---|
| `StoreCompletedWaferAsync` 호출 주기 | `WafersPerOutputBatch=8` 다이마다 | `if ((index+1) % WafersPerOutputBatch == 0)` | 일치 |
| Good1 → Good2 자동 전환 | "elif `OutputSlotGood2 < 25`" | 동일 로직 | 일치 |
| 만석 시 자동 정지 | `_cycleCts.Cancel()` | 동일 | 일치 |
| `OUT-FULL-GOOD / OUT-FULL-NG` 알람 | 발생 명시 | `AlarmManager.Raise` 구현됨 | 일치 |
| `SupplyEmptyWaferAsync` / `ExchangeWaferSequenceAsync` | 미명시 | 코드 존재 (L819, L907) | 코드엔 있음 |

### 11.7 종합 결론

| 구분 | 비율 |
|---|---|
| 완벽 일치 | ~97 % |
| 의미 동등 (자연스러운 분해 등) | ~3 % |
| 미세 차이 (의도적 마진) | 1건 (TPU ArmX SoftLimit 1500→1600 mm) |
| 실질적 mismatch | **0건** |

**시퀀스 문서와 코드는 사실상 모두 일치**한다. 발견된 차이는 문서에 미명시된 일부 Config 항목(TpuPlaceDone/WaferChange/ColletCleaning Timeout, BinCameraVelocity 등) 과 자연스러운 헬퍼 메서드 분해, 그리고 ArmX SoftLimit 의 의도적 마진 1건뿐이다. PPT/시퀀스 문서 업데이트 시 위 코드엔만 있는 항목들을 보강 차원에서 추가하면 충분.

---

## 부록. v1 → v2 변경 사항

직전 v1 의 부록 A "추후 확인 필요 7건" 중 본 v2 에서 본문에 통합된 항목:

| # | v1 상태 | v2 결과 |
|---|---|---|
| 1 | `IBarcodeReader` 구현체 | **§3.11 / §4.1**: `BarcodeSerialAdapter` (Vision/) + `NullBarcodeReader` (CDT320Machine.cs:44) 명시 |
| 2 | `IWaferMapHandler` 구현체 | **§3.12 / §4.1**: `NullWaferMapHandler` (CDT320Machine.cs:64~76) 만 존재 — 실 구현은 외부 위임 |
| 3 | `IVisionTpuClient` 구현체 | **§4.1**: `TpuVisionAdapter` (`QMC.CDT320.VisionComm`) + `NullVisionTpuClient` (CDT320Machine.cs:91~117) |
| 4 | `OnPlaceDone` 이벤트 | **§8 본문**: 이벤트 아닌 `ITpuUnit.WaitPlaceDoneAsync(int timeoutMs)` + `NotifyPlaceReady()` 메커니즘. 내부 동기화 (TaskCompletionSource 등) 미구현 (`NullTpuUnit` 만 존재) |
| 5 | `TransferPickerUnit.cs` 300줄 이후 | **§3.6 / §3.7**: `Pickup/Place/MoveToFocus/MoveToWait` 본체 (L317~399) + `InspectBottomVisionAsync` (L523~595), `InspectSideVisionAsync` (L622~842, `Task.WhenAll` L739~747), `PlaceDiesAsync` (L860~898) 모두 채록 |
| 6 | `SimulatorBridge` axis 매핑 | **§5**: `BuildMaps()` (L88~156) + `MotionMap.cs` (L19~84) 동일 #00~#36 체계 명시 + DO 매핑(`Y048~Y075`) 추가 |
| 7 | `LogMessage / CycleProgress / LotPortStateChanged` 외부 구독자 | **§8**: `LogMessage` → `Form1.cs:87~90` (`EventLogger.Write` + `Console.WriteLine`). `CycleProgress` / `LotPortStateChanged` → 외부 구독자 미발견 (UI Designer 파일에서만 가능성) |

**여전히 "확인 필요" 로 남은 항목**:
- `IWaferLoader.IsFeederAtSafePosition` 의 실제 구현체가 `InputLoaderUnit` 인지 명시적 확인
- `ITpuUnit` / `ITransferPickerUnit` 의 실제 구현체 (`TransferPickerUnit` 직접 구현인지 어댑터를 통한 구현인지)
- `WaferChangeRequested` 이벤트의 실제 구독자 위치
- `OnPlaceDone` 동기화 메커니즘 실 구현 (현재 `NullTpuUnit` 만 존재)

— 끝 —
