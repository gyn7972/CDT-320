# CDT-320 Code Analysis — Vision 측 (그랩 영상 송출 대상)

> 분석 기준 경로: `D:\Work\CDT-320\QMC.CDT-320\QMC.Vision` (메모리의 `D:\Work\CDT-320\QMC.Vision` 는 구경로 — 실제는 솔루션 폴더 하위에 위치).

## Solution structure
- 솔루션: `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320.sln` (QMC.CDT-320 Handler / QMC.Common / QMC.Vision)
- Vision 프로젝트: `QMC.Vision.csproj` — **classic .NET Framework csproj (v4.7.2, C# 7.3)**. ⚠️ 새 .cs 파일은 반드시 `<Compile Include>` 로 명시 등록 필요 (SDK 스타일 아님).

## Layer map (Vision)
### Entry / Host
- `Form1.cs` — `Form1_Load` 에서 ① VisionConfigStore.Load(), ② 5개 모듈 카메라 생성, ③ 5개 모듈 생성(WaferMod/BinMod/BottomMod/FrontSideMod/RearSideMod), ④ 5개 `VisionTcpServer` 생성+Start, ⑤ 6 탭 UI. `OnFormClosing` 에서 서버 Dispose.
- `Program.cs` — 엔트리.

### Equipment communication (기존 명령 서버)
- `Comm/VisionTcpServer.cs` — 모듈당 1 서버. 줄단위 UTF-8 텍스트 프로토콜 (`MODULE|CMD|args` → `ACK|...`/`ERR|...`, 비동기 push `EPD|`/`ARM|`). 포트는 모듈별.

### Business logic (모듈/카메라/검사)
- `Modules/VisionModule.cs` — 베이스. `Grab(timeoutMs)` → `Camera.Grab()` → `GrabResult`. 성공 시 `ExposureDone` 이벤트(이미지 없이 모듈명만). `Camera.ExposureEnded` 구독. **현재 Grab()은 FrameReceived를 발행하지 않음.**
- `Modules/WaferVisionModule/BinVisionModule/BottomInspectionModule/FrontSideInspectionModule/RearSideInspectionModule.cs` — 각 모듈의 Finder/Inspector 구성.
- `Core/ICamera.cs` — `Grab()`, `StartLive()`/`StopLive()`(연속 촬영, 프레임마다 `FrameReceived`), `TriggerSoftware()`, 이벤트 `FrameReceived(GrabResult)` / `ExposureEnded` / `ConnectionChanged`.
- `Core/CameraBase.cs` — 공용 구현. `RaiseFrame(GrabResult)` 로 FrameReceived 발행.
- `Cameras/Sim/SimCamera.cs` — 합성 640×480. `StartLive()` 는 타이머로 매 틱 `Grab()` 후 `RaiseFrame`. `Grab()` 은 단발 합성 프레임(프레임카운터 증가). Provider 기본값이 Sim 이라 단독 실행 시 동작.
- `Cameras/Hik/HikGigECamera.cs` — 실 GigE. Live 는 콜백/폴링 스레드.
- `Core/GrabResult.cs` — `Bitmap Image`(소유, Dispose 시 Image.Dispose), `Width/Height`, `IsSuccess`, `ErrorMessage`. 팩토리 `Success/Fail`.

### Data / Settings
- `Config/VisionConfig.cs` — `VisionSettings`(DataContract JSON, `Config/vision.json`). 모듈 포트 필드: `WaferVisionPort=5100`, `InspectionVisionPort=5101`(Bottom), `BinVisionPort=5103`, `MainCommunicatorPort=5104`, `FrontSideInspectionPort=5105`, `RearSideInspectionPort=5106`. `VisionConfigStore.Load/Save`.
- `Config/AlgorithmCameraMap.cs`, `AlgorithmCameraBinder.cs` — 알고리즘↔카메라 매핑.

### UI
- `Ui/Controls/CameraView.cs` — 카메라 영상 표시 컨트롤. `SetFrame(GrabResult)` 가 `r.Image.Clone()` 후 Invalidate. (프레임을 clone 해서 보관하는 안전 패턴 참고용.)

## Existing capabilities
- 모듈별 명령 TCP 서버(5채널) 이미 운용 중 — Handler가 EXPOSE/GRAB/MATCH/INSPECT 명령으로 구동.
- 카메라 추상화로 Sim/Hik 교체 가능. Sim 은 라이브/단발 모두 합성영상 생성.
- 설정 JSON 영속 패턴 존재(VisionSettings).

## Notable patterns and conventions
- **Threading**: 명령 서버는 async Task. 카메라 라이브는 타이머/폴링 스레드 + `FrameReceived` 이벤트. Grab() 은 동기.
- **이벤트 발행**: `try { handler?.Invoke(...); } catch {}` 패턴(예외 격리).
- **Bitmap 소유권**: `GrabResult.Dispose()` 가 Image 를 Dispose → **다른 곳에서 계속 쓰려면 Clone 필수** (CameraView.SetFrame 가 그렇게 함).
- **csproj 수동 등록**: 새 파일 `<Compile Include="...\X.cs" />` 추가 필수.
- **설정 추가**: `VisionSettings` 에 `[DataMember]` 프로퍼티 추가 + 기본값.
- **서버 수명**: Form1_Load 에서 생성/Start, OnFormClosing 에서 Dispose. 동일 패턴으로 뷰어 서버도 부착.

## 기존 SP_RemoteViewer 프로토타입 (재사용/참고 대상)
- `SP_RemoteViewer/QMC.RemoteViewer/ScreenCaptureServer.cs` — 1:N TCP 서버. 캡처 루프(FPS), latest-frame + frameId, 클라이언트별 send 스레드, `[4바이트 길이][JPEG]` 송신, `NoDelay`. **단, 프레임 소스가 고정 화면영역 캡처(680×680)로 하드코딩** → 그랩 이미지 송출로 바꾸려면 소스를 교체해야 함.
- `SP_RemoteViewer/QMC.RemoteViewer/RemoteViewerControl.cs` — 클라이언트 뷰어 컨트롤. `[4바이트 길이][JPEG]` 수신/표시. **무수정 호환 목표**.
- 송신 프레이밍(서버) = `BitConverter.GetBytes(len)`(int32 LE) + bytes + Flush. 수신(클라) = 4바이트 int32 → len 바이트, `len<=10MB` 검증. → 신규 서버는 이 프레이밍을 정확히 따라야 함.
