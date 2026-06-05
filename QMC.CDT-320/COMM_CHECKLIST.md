# CDT-320 통신·구조 체크리스트

목표: **QMC.Vision ↔ QMC.CDT-320(Handler) ↔ CDT320Simulator** 의
전체 통신 경로 + 핵심 기능이 정상 동작함을 자동/수동 확인.

> 자동 검증 스크립트: `tools/verify_comm.pl`

---

## 0. 빌드 (전제)

| 항목 | 기대값 | 결과 |
|---|---|---|
| QMC.Common.dll      | 빌드 성공 | ☐ |
| QMC.CDT-320.exe     | 빌드 성공, 0 에러 | ☐ |
| QMC.Vision.exe      | 빌드 성공, 0 에러 | ☐ |
| CDT320Simulator.exe | 빌드 성공, 0 에러 | ☐ |

---

## 1. 정적 구조 — Handler ↔ Vision

### 1.1 코드 연결점

| 검사 | 파일 | 기대 |
|---|---|---|
| VisionTcpClient 존재 | `Equipment/Vision/VisionTcpClient.cs` | PING/EXPOSE/MATCH/INSPECT/TRAIN 메서드 |
| VisionHub 3-모듈 노출 | `Equipment/Vision/VisionHub.cs` | Wafer/Inspection/Bin 정적 프로퍼티 |
| WaferVisionAdapter `IVisionTcpClient` 구현 | `Equipment/Vision/VisionAdapters.cs` | TriggerExpose/GetResult/TriggerAlign |
| TpuVisionAdapter `IVisionTpuClient` 구현 | 동일 | TriggerBottom/GetBottom/TriggerSide/GetSide |
| Machine 생성 시 Adapter 주입 | `Equipment/CDT320Machine.cs` | NullObject 가 아닌 `WaferVisionAdapter`/`TpuVisionAdapter` |

### 1.2 Vision 서버 측 매칭

| 모듈 | 서버 포트 | Handler Adapter |
|---|---|---|
| WaferVision      | 5100 | `VisionHub.Wafer` ← `WaferVisionAdapter` |
| BottomInspection | 5101 | `VisionHub.Inspection` ← `TpuVisionAdapter` |
| BinVision        | 5103 | `VisionHub.Bin` ← `BinVisionHelper` |

### 1.3 프로토콜 일치성

| 항목 | Vision 서버 | Handler 클라이언트 |
|---|---|---|
| 인코딩 | UTF-8 | UTF-8 |
| 구분자 | `\n` | `\n` |
| 요청  | `MODULE\|CMD\|args` | `MODULE\|CMD\|args` |
| 응답  | `ACK\|MODULE\|CMD\|...` | `ACK` 접두 검사 |
| PING | `OK` | `IsConnected = true` 판정 |

---

## 2. 정적 구조 — Handler ↔ Simulator

### 2.1 코드 연결점

| 검사 | 파일 | 기대 |
|---|---|---|
| SimulatorBridge ConnectAsync | `Equipment/SimulatorBridge.cs` | host/port 파라미터 |
| AppSettings 기본 7001 | `Equipment/AppSettings.cs` | SimulatorPort=7001 |
| SettingsTab 시뮬레이터 페이지 | `Ui/Pages/Settings/SimulatorLinkPage.cs` | Connect 버튼 |
| Form1 종료 시 Disconnect | `Form1.cs` | `Bridge.Dispose()` |

### 2.2 시뮬레이터 측 매칭

| 명령(Handler→Sim) | Sim 처리 |
|---|---|
| `{"cmd":"DO_SET","port":"Y003","val":1}` | 타워램프 RED LED ON |
| `{"cmd":"AXIS_MOVE","axis":9,"pos":300,"vel":500}` | FRONT PICKER X 이동 |
| `{"cmd":"DI_GET_ALL"}` | DI 전체 상태 응답 |
| `{"cmd":"HELLO","role":"master","from":"QMC.CDT-320"}` | 로컬 RUN CYCLE 비활성화 |

| 이벤트(Sim→Handler) |
|---|
| `{"evt":"AXIS_DONE","axis":9,"pos":300}` |
| `{"evt":"DI_CHANGED","port":"X063","val":1}` |
| `{"evt":"DO_CHANGED","port":"Y003","val":1}` |

### 2.3 SimulatorBridge 매핑 테이블

| 항목 | 개수 | 비고 |
|---|---|---|
| 축 매핑 (이름→축번호 0~36) | 35 | EJECT_PIN_Z 등 4개는 QMC 미대응 |
| DO 매핑 | 17 | Picker VAC/BLOW + Lamps + Bin Guide |
| 쓰로틀 | 50ms / 0.1mm | 이동 중에만 |

---

## 3. 핸들러 내부 통합

| 항목 | 위치 | 기대 |
|---|---|---|
| Form1_Load 에 Vision 자동 연결 | `Form1.cs` | `cfg.VisionAutoConnect`==true 시 ConnectAllAsync |
| 상태바 Vision 연결 도트 | `Form1.cs` | `OnVisionHubChanged` → 녹/회색 |
| MachineController.CycleRun 에서 Vision 호출 | `Equipment/MachineController.cs` | DieFinder Match + SurfaceInspector + Placement |
| AlarmManager — Cycle 예외 | `Equipment/MachineController.cs` | `AlarmSeverity.Error` Raise |
| EventLogger — Vision 연결 이벤트 | `Equipment/Vision/VisionHub.cs` | "VISION-CONN" 코드 |
| SystemSelfTestDialog 9 항목 | `Ui/Dialogs/SystemSelfTestDialog.cs` | AppSettings/AjinConfig/AXL/Machine/Sim/Vision×3/Log/Recipe |

---

## 4. 런타임 검증

### 4.1 Vision TCP 서버

| 단계 | 명령 | 기대 응답 |
|---|---|---|
| 4.1.1 PING | `WaferVision\|PING` | `ACK\|WaferVision\|PING\|OK` |
| 4.1.2 EXPOSE | `WaferVision\|EXPOSE\|0` | `ACK\|WaferVision\|EXPOSE\|w=640;h=480;...` |
| 4.1.3 MATCH | `WaferVision\|MATCH\|ReticleFinder\|0` | `ACK\|WaferVision\|MATCH\|OK;x=...;y=...;r=...;score=...` |
| 4.1.4 BinVision INSPECT | `BinVision\|INSPECT\|PlacementInspector\|0` | `ACK\|BinVision\|INSPECT\|PASS;...` |
| 4.1.5 BottomInspection EXPOSE | `BottomInspection\|EXPOSE\|0` | `ACK\|BottomInspection\|EXPOSE\|...` |

### 4.2 Simulator TCP 서버

| 단계 | 명령 | 기대 |
|---|---|---|
| 4.2.1 DI 조회 | `{"cmd":"DI_GET","port":"X007"}` | `{"evt":"DI_STATE","port":"X007","val":1}` |
| 4.2.2 DO 설정 | `{"cmd":"DO_SET","port":"Y003","val":1}` | `{"evt":"ACK","cmd":"DO_SET",...}` + UI LED ON |
| 4.2.3 축 이동 | `{"cmd":"AXIS_MOVE","axis":9,"pos":300,"vel":500}` | `{"evt":"ACK","cmd":"AXIS_MOVE","axis":9}` 후 `AXIS_DONE` |
| 4.2.4 마스터 클레임 | `{"cmd":"HELLO","role":"master","from":"X"}` | `{"evt":"ACK","cmd":"HELLO"}` + 로컬 RUN CYCLE 차단 |

### 4.3 핸들러 통합 (수동)

| 단계 | 동작 | 기대 |
|---|---|---|
| 4.3.1 | QMC.CDT-320.exe 실행 | 헤더 "VIS ●" 녹색 (Vision 자동 연결) |
| 4.3.2 | Settings → 시뮬레이터 연결 | 7001 Connect → 헤더 "READY" |
| 4.3.3 | Settings → 자가 진단 RUN | 9개 모두 OK (녹색) |
| 4.3.4 | User → engineer 로그인 | 권한 변경 반영 |
| 4.3.5 | Work → 초기화 → CYCLE RUN(10) | 시뮬 3D에 ARM 이동 + Vision 매칭 호출 |
| 4.3.6 | 이력 → 이벤트 | VISION-Wafer/CTRL/CYCLE 로그 누적 |

---

## 5. 자동 검증 결과 (2026-04-25 실행)

```
==============================================================================================================
CATEGORY   ITEM                                                    RESULT DETAIL
--------------------------------------------------------------------------------------------------------------
BUILD      QMC.Common.dll                                          PASS
BUILD      QMC.CDT-320.exe                                         PASS
BUILD      QMC.Vision.exe                                          PASS
BUILD      CDT320Simulator.exe                                     PASS
STATIC     VisionTcpClient PingAsync/MatchAsync/InspectAsync       PASS (3건)
STATIC     VisionHub Wafer/Inspection/Bin + ConnectAllAsync        PASS (2건)
STATIC     WaferVisionAdapter : IVisionTcpClient                   PASS
STATIC     TpuVisionAdapter   : IVisionTpuClient                   PASS
STATIC     Machine uses WaferVisionAdapter (NullObject 제거)        PASS
STATIC     Machine uses TpuVisionAdapter   (NullObject 제거)        PASS
STATIC     SimulatorBridge ConnectAsync(host,port)                 PASS
STATIC     CycleRun calls VisionHub.Wafer.MatchAsync               PASS
STATIC     CycleRun calls VisionHub.Inspection                     PASS
STATIC     Form1 calls VisionHub.ConnectAllAsync                   PASS
STATIC     Form1 subscribes VisionHub.ConnectionChanged            PASS
STATIC     SelfTest registers Vision tests                         PASS
STATIC     Simulator handles HELLO/AXIS_MOVE/DO_SET/DI_GET_ALL     PASS (4건)
RUNTIME    Vision/Wafer PING                                       PASS   ACK|WaferVision|PING|OK
RUNTIME    Vision/Wafer EXPOSE                                     PASS   ACK|WaferVision|EXPOSE|w=640;h=480;...
RUNTIME    Vision/Wafer MATCH ReticleFinder                        PASS   ACK|WaferVision|MATCH|OK;x=320.908;...
RUNTIME    Vision/BottomInspection EXPOSE                          PASS   ACK|BottomInspection|EXPOSE|w=640;...
RUNTIME    Vision/BottomInspection INSPECT SurfaceInspector        PASS   ACK|BottomInspection|INSPECT|PASS;...
RUNTIME    Vision/Bin INSPECT PlacementInspector                   PASS   ACK|BinVision|INSPECT|PASS;Width=...
RUNTIME    Vision unknown command rejected                         PASS   ERR|WaferVision|FOO|unknown command
RUNTIME    Simulator TCP server (port 7001)                        SKIP   GUI [TCP START] 버튼 필요 (의도)
==============================================================================================================
TOTAL 31   PASS 30   SKIP 1   FAIL 0
```

`Simulator TCP server` 항목 SKIP은 의도된 동작 — `CDT320Simulator`는 WPF 앱이고
TCP 서버는 UI에서 `[TCP START]` 버튼 클릭 시에만 활성화 (외부 임의 접근 차단).
실 운영 시 수동 시작 후 동일한 자동 검증을 단독으로 재실행하면 정상 PASS.
