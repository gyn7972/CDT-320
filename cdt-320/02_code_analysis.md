# CDT-320 Code Analysis (마스터 브랜치 전체 코드 분석)

- 분석일: 2026-07-19
- 대상: `master` 브랜치 (커밋 41239328, working tree clean)
- 범위: `QMC.CDT-320`(597 .cs) + `QMC.Common`(93 .cs), 코드 수정 없음(읽기 전용 분석)

---

## 1. Solution structure

```
QMC.CDT-320.sln                  ← 기본 솔루션 (VS2022, .NET Framework 4.7.2, C# 7.3)
├─ QMC.CDT-320/  (WinExe)        메인 Handler WinForms 애플리케이션
│  ├─ Program.cs / Form1.cs      진입점 + 메인 셸 (Form1 약 2,148줄)
│  ├─ Equipment/                 장비 제어·통신 레이어 (Ajin, Vision, Interlocks, Unit, Secs …)
│  ├─ Sequencing/                자동 시퀀스/비즈니스 로직 (132 .cs)
│  └─ Ui/                        Tabs / Pages / Controls / Dialogs / Localization / Security
└─ QMC.Common/  (Library)        공용 라이브러리 (Motion, IO, Alarms, Logging, Persistence, 공용 UI)
```

- 장비 성격: **듀얼 픽커 다이 본더 Handler**. Vision 실행 프로그램·카메라 SDK·검사 엔진은 별도 Vision PC에 있으며 이 저장소는 Handler 측 코드만 보유 (README/AGENTS.md 명시).
- 별도 솔루션: `QMC-master/QMC.Core.sln`, `SP_RemoteViewer/` (기본 작업 범위 외).
- 작업 규칙 정본: [AGENTS.md](../AGENTS.md) — 안전 불변식(픽커 Y-Avoid, Z 하강 phase 제한 등), `Task<int>` 시퀀스 반환 규약, 빌드 시 Rebuild/Clean 금지(별도 OutDir Build만) 등.

---

## 2. Layer map

### 2-1. 진입점 / 메인 셸

| 파일 | 역할 |
|---|---|
| `Program.cs` | `[STAThread] Main`. 단일 인스턴스 뮤텍스, 전역 예외 → CrashDump + Alarm 로그, 자동화 인수(`--auto-cycle`, `--audit-all` 등), 로그 경로/보존 서비스 부트스트랩 |
| `Form1.cs` | 메인 셸. **모든 매니저를 생성·보유·주입**: `CDT320_Machine` → `SimulatorBridge` → `MachineController` → `AlarmResponseService` → 모니터 서비스들(MotionMonitor 50/250ms, CollisionSupervisor 10ms, IoScan 10/100ms, OpPanelMonitor) → `SecsHost` → 6개 탭 생성. 종료는 EXIT 버튼 경유 강제, 역순 Dispose |

초기화 순서(Form1_Load): 설정/언어 → Ajin 보드 오픈 → Vision 6채널 연결 + 재연결 워치독 → 장비 코어 생성 → Material 복구 질의 → 모니터링 기동 → 탭 생성 → 마지막 레시피 자동 로드.

### 2-2. Equipment 레이어 (`QMC.CDT-320\Equipment`)

**중심 클래스**
- `MachineController.cs` (497KB, 최대 파일) — 초기화/시작/정지/CycleRun, `AutoSequenceCoordinator` 기동, 축 병렬 초기화 레인, SharedRailX, 인터락 연동 총괄. public 메서드 약 128개
- `CDT320Machine.cs` — 유닛 트리 조립(InputCassette/Feeder/Stage, PickerFront/Rear, Vision, Output 3종, OpPanel, Ionizer, Resources)
- `IStageInterfaces.cs` — `IWaferLoader`, `IVisionTcpClient` 등 인터페이스 + Null Object 구현

**Vision 통신 (`Equipment\Vision`, 네임스페이스 `QMC.CDT320.VisionComm`) — 최근 커밋의 핵심**
- 전송: **TCP 라인 프로토콜** `MODULE|CMD|args...`(UTF-8, `\n` 종단, 복합 인자 `;`)
- 채널: 명령 6개 — Wafer 5100 / Bottom 5101 / Bin 5103 / Main 5104 / FrontSide 5105 / RearSide 5106 (`VisionHub` static 허브가 소유, `Task.WhenAll` 병렬 연결). 영상 스트림은 +100 포트(5200~5206, `VisionViewerPorts`)
- 수신 분기: `ACK` / `ERR` / 푸시(`EPD` 노출완료, `ARM` 알람, `RECIPEREQ`, `XYT`) / `MRESULT`·`RESULT`
- **두 세대 프로토콜 공존**:
  - (A) Legacy FIFO — `VisionTcpClient.cs`(1,159줄): `SemaphoreSlim` 명령 게이트 + 응답 대기 `Queue<TaskCompletionSource>`, 수신 루프 백그라운드 Task
  - (B) Correlated(신형) — `VisionTcpClient.Correlated.cs`, `VisionInspectionProtocol.cs`: `request_id`/`group_id` 라우팅, Camera/Head/DieIndex/WaferId/LotId 등 전 필드 일치 검증으로 EPD 오인 방지. `INSPECT_SYNC`/`INSPECT_ASYNC`
  - 최근 커밋("비젼통신_1차", "비젼프로토콜2차수정본")은 (A)→(B) 전환 작업. **모션 안전 규약: ACK가 아니라 EPD 수신 후 픽커 이동**
- 서비스 스택: Unit(`WaferVisionAdapter`) → `AutoVisionRequestService`(Sim/DryRun 바이패스) → `VisionCommandService`(채널 라우팅) → `VisionTcpClient` → `VisionHub`
- 부속: `VisionReconnectWatchdog`(3초 무한 재연결, 알람 없음), `VisionFrameClient`(`[4B metaLen][meta][4B jpegLen][JPEG]` 코덱), `VisionCommLog`(TX/RX 메모리 링버퍼), `CoordinateMap`(픽셀↔mm 아핀 변환, coord_map.json)

**기타 주요 폴더**
| 폴더 | 내용 |
|---|---|
| `Ajin/` | AjinAxis/Cylinder/DI/DO 드라이버, AjinConfig(AxisMap/DioMap), CylinderManager |
| `Interlocks/` | `MotionInterlock` 파생 20+개, 유닛별 규칙 셋, `MotionGuardService`, `RealtimeCollisionSupervisor`(실시간 충돌 감시 스레드) |
| `Unit/` | `BaseUnit<Setup,Config,Recipe>` 파생 유닛들(엑셀 시트 기준 축/IO/티칭). PickerAxis = X/Y + T0~T3/Z0~Z3(4콜렛) |
| `Motion/SharedRailX/` | FrontSide/RearSide Vision이 X축 공유 → `SharedRailXMotionService` + 충돌 검증 |
| `Materials/` | `MaterialStateService`(자재 상태 SSOT), Cassette→Wafer→Die 모델, CSV/스냅샷 저장 |
| `Recipes/` | `RecipeStore`(.Project), RecipeProject Subset 구조, 픽업 패턴 |
| `Initialization/` | 축 호밍 플랜(병렬 레인 + 인터락 규칙 트리) |
| `Secs/` | SECS/GEM Host (HSMS 또는 라인 TCP, 기본 5000) |
| `Alarms/` `Calibration/` `DieMaps/` `Jobs/` `Lots/` `Stats/` `Sim/` `Remote/` | 알람 대응, 좌표/AF 보정, 다이맵 생성, Job/Lot, UPH 통계, 시뮬 어댑터, 원격 뷰어 |

### 2-3. Sequencing 레이어 (`QMC.CDT-320\Sequencing`)

3계층 구조:
1. **오케스트레이터** — `AutoSequenceCoordinator`: 4개 유닛 레인(INPUT / FRONT / REAR / OUTPUT)을 `Task.Run` 병렬 실행. `MachineController`가 생성·기동
2. **유닛 시퀀스** — `UnitSequenceBase` 파생: `InputSequence`(2,340줄), `OutputSequence`, `FrontPickerSequence`, `RearPickerSequence`. Auto는 무한 사이클 루프, Step 모드는 `SemaphoreSlim` 게이트로 1스텝씩
3. **작업 시퀀스** — `XxxSequenceBase<TStep>` 제네릭 상태머신(도메인마다 Base/Kind/Options/Sequence 4-파일 규약): InputCassette/Feeder/Stage, OutputCassette/Feeder/Stage, Picker, Calibration. `int` 결과코드(0 성공), `Fail()` 공통 실패 헬퍼

**공통 인프라 (`Sequencing\Common`)**
- `MachineSequenceContext` — 모든 시퀀스가 공유하는 컨텍스트(Controller/Machine/Bus/Resources/Phases/Gate/CycleStop 플래그)
- `SequenceSignalBus` — `TaskCompletionSource` 기반 이름 신호로 유닛 간 핸드오프("InputStageReady" 등)
- `SequenceResourceManager` — 이름 있는 상호배제 자원(InspectionArea, FrontPicker 등 9종)을 `IDisposable` 리스로 임대
- `AutoSequenceCoordinatorGate` / `PickerPhaseCoordinator` — 로더↔픽커 작업 게이트, Front/Rear 픽커 phase 간섭 매트릭스(Idle/PickUp/Bottom/Side/Place)
- `SequenceResumeStore` — 스텝 단위 재개 저장. InputSequence는 실제 Material 위치로 재개 스텝 역산
- `SequenceStopException` — **정상 정지(CycleStop)를 오류와 구분하는 제어 흐름 예외**
- `WaferCompletionRunCoordinator` — Stop-After-Drain(웨이퍼 소진 후 자동 종료)

**정지 의미론 3단계**: CycleStop(안전 경계까지 완주 후 종료) / Abort(linked CTS 취소, 3~5s 타임아웃 강제 종료) / Alarm(실패 스텝 보존, 크리티컬 알람이면 즉시 Abort).

**Picker 계열이 최대 복잡도**: `PickerSequenceBase`(4,094줄, SharedRailX·상대픽커 Avoid·Facing-Y 인터락 내장), `PickerProcessSequence`(3,104줄, CheckUnit→InputCameraMark→PickUp→Bottom→Side→Place), `PickerPickUpStep` 28스텝, 연속모션 세그먼트, Runtime AutoFocus.

### 2-4. UI 레이어 (`QMC.CDT-320\Ui`)

- **구조**: 하단 6개 탭(`TabBase` 파생 Work/WorkInfo/History/Recipe/Settings/User) → 우측 사이드바 + 페이지 lazy 생성·캐싱(`ShowPage`). 페이지는 `PageBase`(UserControl) 파생
- **핵심 관용구**: `RegisterSidebarButton(btn, "recipe.project", en, factory)` 한 줄이 i18n 라벨 + 권한 등록 + 페이지 팩토리 + 클릭 배선을 동시 수행
- **UI↔로직 연결 3패턴**: (A) 명령 = 버튼 → `RunSafe(async c => await Controller.XxxAsync())`, (B) 전역 상태 = Controller 이벤트 구독(`StatusChanged` 등, `OnHandleDestroyed`에서 해제), (C) 세부 값 = 페이지별 WinForms Timer 폴링(보이는 페이지만 Start/Stop)
- **스레드 보호**: 핸들러 첫 줄 `if (InvokeRequired) { BeginInvoke(...); return; }` 가드-재게시 패턴 일관. `IsDisposed` 선검사, `try { BeginInvoke } catch {}` 핸들 경합 방어
- **다국어**: `Lang.cs` 정적 in-memory 사전(ko 기본/en/zh-CN/ja), 컨트롤 `Tag="i18n:key"` 선언 + `Lang.Apply(root)` 트리 순회. resx 미사용
- **권한**: `UserLevel`(None<Operator<Engineer<Maintenance<Admin), `AccessControl.Apply` + `AccessPolicy`(Config\permissions.json 오버라이드). DEBUG 빌드는 admin 강제 로그인
- **보조**: `UiDoubleBuffer`(깜빡임 방지), `UiClickAuditor`(핸들러 없는 dead button 자동 감사), `ModelessDialogHost`(key 기반 단일 인스턴스 모덜리스)

### 2-5. QMC.Common (공용 라이브러리)

- **Composite 장비 트리**: `Machine<> → BaseUnit<> → BaseComponent<>`, 각 노드가 Setup(기구 설정)/Config(설비 사양)/Recipe(공정 파라미터) 3종 데이터(`ISetupData`/`IConfigData`/`IRecipeData` 마커). 루트 `SaveSettings()` 한 번으로 트리 전체 재귀 저장
- **Motion**: `BaseAxis`(1,250줄) — 사다리꼴 가감속 **시뮬레이션 엔진** 내장(하드웨어 없이 검증 가능), 정적 `MotionGuard` 델리게이트 훅 + `AsyncLocal` 우회 스코프. `MotionAxisManager`, `MotionMonitorService`(50ms), 보간 이동(`AjinInterpolatedMotionService`)
- **AjinE**: AJINEXTEK AXL C API P/Invoke 래퍼(AXL/AXM/AXD/AXA…). `Ajin/` 폴더는 csproj 미포함 레거시
- **IO**: `BaseDigitalInput/Output`(10ms 폴링, A/B접점, `WaitUntilStateAsync` + SettleTime), `BaseCylinder`(편솔/양솔, 시뮬 센서 주입), `AjinIoScanService`(전역 AXD 락)
- **Logging 이중 체계**: (A) `LogManager`/`Log` 텍스트 로그(4MB 로테이션) + (B) `EventLogger` CSV 구조화 로그(`EventKind` 9종, 100MB 로테이션, 역방향 tail 고속 조회, 메모리 최근 버퍼) + `MessageCatalog`(코드→한/영 자동 카탈로그)
- **Persistence**: 전부 JSON(`DataContractJsonSerializer`), `.tmp → File.Replace` 원자적 쓰기, `JsonPrettySerializer`. 활성 스택은 `Data/Store`(`UnitDataStore` 파사드), `Persistence/`는 레거시 병존
- **Alarms**: `AlarmManager`(전역 static, Raise/Clear, 디바운스 저장, E-STOP류 Critical 자동 승격) + `AlarmMaster`(한/영 정의 60여 개, Config\alarm_master.json)
- **공용 UI**: `CameraViewBase`(1,178줄 범용 카메라 뷰: 줌/팬/측정/누적평균/오버레이), `VisionFrameCodec`(비전 영상 와이어 프로토콜 — Handler가 직접 사용, 삭제 금지), `MessageDialog`(한글화 메시지박스), `ProgressDialog`
- **Diagnostics**: `CrashDumpWriter`(MiniDump), TactTime 계측(계층 스코프 + AsyncLocal, CSV 싱크)

---

## 3. Existing capabilities (코드가 이미 할 수 있는 것)

- 듀얼 픽커(Front/Rear) 4-레인 병렬 자동 운전: Cassette 공급 → Wafer 정렬/맵핑 → Die Pickup → Bottom/Side 검사 → Output Place → 배출
- 픽커 간 충돌 방지: phase 간섭 매트릭스 + 자원/워크존 임대 + Y-Avoid 인터락 + 실시간 충돌 감시(10ms) + SharedRailX 검증
- 외부 Vision PC와 6채널 TCP 통신(명령/결과/푸시/영상 스트림), 자동 재연결, Legacy FIFO·Correlated 신형 프로토콜 공존
- 스텝 단위 알람 재개(resume), CycleStop/Abort/Alarm 3단계 정지, Stop-After-Drain
- 시뮬레이션 모드: 축 가감속 시뮬, IO 상태 주입, Vision 바이패스 — 하드웨어 없이 로직 검증
- 캘리브레이션 자동 시퀀스(Collet/Needle/Vision 카메라/AutoFocus/Z 보정)
- Material(Cassette→Wafer→Die) 상태 추적 + 스냅샷 영속화 + 재시작 복구 질의
- Recipe(.Project) 관리, DieMap 생성/전환, Job/Lot, UPH 통계, TactTime 계측
- SECS/GEM Host, Remote Viewer, 알람 마스터(한/영), 4개국어 UI, 5단계 사용자 권한
- CLI 자동화(`--auto-cycle`, `--audit-all` 등) + dead button 자동 감사

---

## 4. Notable patterns and conventions (Stage 6에서 반드시 따라야 할 것)

- **시퀀스 표준**: 반환형 `Task<int>`(0 성공/-1 실패), `async void` 금지(UI 이벤트 제외), `ConfigureAwait(false)` 유지, 이동은 "명령 발행 → 결과 확인 → 완료·InPosition 확인" 3단계 분리, 병렬 이동은 `Task.WhenAll` 2회(명령/완료)
- **상태머신 규약**: 도메인마다 `XxxSequenceBase<TStep>` + Kind + Options + Sequence 4-파일. 스텝 enum 세밀 분할
- **자원 관리**: 모든 락/리스/신호는 `IDisposable` + `using`/`finally`, 단 물리 Safe(Y/Z Avoid) 확인 후 반납
- **로그**: `Log.Write("Main", UserSession.Name, "동작", "한국어 메시지 - Ok")` 형식, `EventLogger.Write(EventKind, category, code, message)`. Vision TX/RX는 메모리 링버퍼만
- **예외**: 빈 catch 금지(로깅 경로 제외), `SequenceStopException`/`OperationCanceledException`을 일반 고장으로 오인 금지
- **UI**: 컨트롤 prefix(btn/lbl/txt/grid…), `컨트롤명_이벤트명` 핸들러, Designer에 로직 금지, `InvokeRequired` 가드-재게시, `Tag="i18n:key"` 다국어
- **영속화**: JSON pretty + 원자적 교체(`JsonPrettySerializer`), 직렬화 키 변경 시 마이그레이션 필수
- **와이어 계약 불변**: Vision 모듈명/포트/명령 토큰/AlgorithmKey/CameraId/직렬화 키는 외부 계약 — 임의 변경 금지 (AGENTS.md §10)
- **빌드**: 원본 저장소에서 Rebuild/Clean 금지(운영 경로 `D:\CDT-320` 삭제 위험), 별도 OutDir 지정 `/t:Build`만. 구형 csproj라 새 파일은 `<Compile Include>` 수동 등록
- **csproj 주의**: `Equipment\Bin`은 소스 폴더(이름만 bin) — 삭제 금지

---

## 5. 관찰된 주의점 (버그 아님, 유지보수 리스크)

| # | 항목 | 내용 |
|---|---|---|
| 1 | 초대형 파일 | `MachineController.cs`(497KB), `PickerSequenceBase`(4,094줄), `PickerProcessSequence`(3,104줄), `MachineReadySequence`(2,535줄), `InputSequence`(2,340줄), `Form1.cs`(2,148줄) — 로직·로깅·인터락 판정 혼재 |
| 2 | 중복 스택 | `QMC.Common\Data\Store`(활성) vs `Persistence\`(레거시) 스토어 이중화, `Ajin\`(미컴파일) vs `AjinE\`(활성), LogManager vs EventLogger 이중 로그 체계 |
| 3 | Vision 프로토콜 2세대 공존 | Legacy FIFO와 Correlated 방식이 `VisionTcpClient` + `.Correlated.cs`로 병존 — 전환 진행 중 상태 |
| 4 | 강결합 | Sequencing 레이어에 인터페이스 0개 — Equipment 구상 타입 직접 참조 → 단위테스트/모킹 곤란 |
| 5 | 인코딩 깨짐 | `BarcodeSerialAdapter.cs`, `SecsHost.cs`, `UiClickAuditor.cs`, `TabBase.cs` 등 한글 주석 mojibake (AGENTS.md §12: 수정 범위에서만 복원) |
| 6 | 잔존 파일 | `Ui\Pages\Recipe\VisionRecipePage_Old.cs` 구버전 |
| 7 | 하드코딩 경로 | `LogManager.WriteWorkLog` → `D:\Log\{날짜}` 고정 |

---

## 6. 참조

- 저장소 규칙: [AGENTS.md](../AGENTS.md) / 개요: [README.md](../README.md)
- 설계 문서: `docs/` (VISION_PROTOCOL_DELIMITER_SPEC.md, ARCHITECTURE_EXPORT.md, PARAMETER_SYSTEM_GUIDE.md 등 80여 개)
- 기존 기능별 워크플로 산출물: `cdt-320/bottom-xyt-first/`, `chip-common/`, `handoff-2026-07-12/`, `needlez-ejectpinz-inputvision/`, `side-af-pickerz-save/`
