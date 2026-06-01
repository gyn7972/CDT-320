# Implementation changes — 원격 뷰어 (Vision 측)

> 모든 변경은 `D:\Work\CDT-320\QMC.CDT-320\QMC.Vision\` 하위. Handler(QMC.CDT-320) 0개 변경.

## Comm/GrabStreamServer.cs — NEW
충족: B1~B11
- 1:N TCP 그랩영상 서버. 와이어 프로토콜 `[4바이트 int32 LE 길이][JPEG]` (SP_RemoteViewer RemoteViewerControl 호환).
- 생성자 `(name, port, Func<Bitmap> frameProvider, fps=10, jpegQuality=60)`.
- accept/capture/클라이언트별 send 스레드(background). latest-frame + frameId 로 신규 프레임만 전송.
- `CompressJpeg`(캐시된 ImageCodecInfo + Quality). static `CaptureScreenRegion(Rectangle)`(Empty→주 모니터 전체).
- `Stop()/Dispose()` 가 listener/스레드/클라이언트 정리.

## Modules/VisionModule.cs — MODIFIED
충족: A1~A7
- frame tap 필드(`_lastFrame`,`_lastFrameUtc`,`_tapLock`).
- ctor/SetCamera 에서 `Camera.FrameReceived` 구독/해제(`OnCameraFrameReceived`→`TapFrame`).
- `Grab()` 성공 시 `TapFrame(g.Image)` (기존 반환/EPD 이벤트 동작 불변).
- `AcquireViewerFrame(staleMs=1500)` — fresh tap clone 우선 → self-grab → 마지막 tap → null.
- `Dispose()` 에서 FrameReceived 해제 + `_lastFrame` Dispose.

## Config/VisionConfig.cs — MODIFIED
충족: C1~C6
- `RemoteViewerEnable`(true), `RemoteViewerSource`("GrabImage"), `RemoteViewerFps`(10), `RemoteViewerQuality`(60).
- 뷰어 포트: Wafer 5200 / Inspection(Bottom) 5201 / Bin 5203 / FrontSide 5205 / RearSide 5206.
- ScreenRegion rect: `RemoteViewerScreenX/Y/W/H`(0). 모두 `[DataMember]` — 기존 json 자동 기본값.

## Form1.cs — MODIFIED
충족: D1~D6
- `using System.Drawing;` 추가. viewer 서버 필드 5개.
- `Form1_Load`: `RemoteViewerEnable` 시 모듈별 5개 viewer 생성+Start(`MakeViewer`). 채널↔모듈 매핑 정확.
- `MakeViewer`: 소스 모드에 따라 provider(GrabImage=`mod.AcquireViewerFrame`, ScreenRegion=`CaptureScreenRegion(rect)`).
- `OnFormClosing`: viewer 5개 Dispose 추가.

## QMC.Vision.csproj — MODIFIED
충족: E1
- `<Compile Include="Comm\GrabStreamServer.cs" />` 등록.

---

# Revision 2 (사용자 요청 반영) — 그랩-게이트 + 송출 게이트 + MainCommunicatorPort 제거

요청: ① 보는 사람(클라이언트) 있을 때만 송출 ② 상시 self-grab 제거 — 테스트 그랩/실제 운행/라이브일 때만 전송 ③ `MainCommunicatorPort` 제거 ④ 테스트는 모니터 2.

## VisionModule.cs — MODIFIED (rev2)
- `AcquireViewerFrame()` 에서 **self-grab 로직 제거** → tap 된 실제 프레임만 clone 반환, 없으면 null. (더 이상 staleMs 인자 없음)
- `_lastFrameUtc` 제거, `_frameSeq`(long) + `ViewerFrameSeq` 프로퍼티 추가 — TapFrame(실제 그랩/라이브) 마다 증가.

## Comm/GrabStreamServer.cs — MODIFIED (rev2)
- CaptureLoop 시작에 **클라이언트 게이트**: `if (ConnectedClients == 0) { Thread.Sleep(interval); continue; }` → 보는 사람 없으면 프레임 획득/인코딩/송출 일절 안 함.
- AcceptLoop: 신규 접속 클라이언트가 마지막 송출 프레임(있으면)을 즉시 받도록 `FrameReady.Set()`.

## Form1.cs — MODIFIED (rev2)
- GrabImage provider 를 **시퀀스 dedup** 으로 변경: `ViewerFrameSeq` 가 바뀐 경우(새 그랩)에만 프레임 제공, 아니면 null → 같은 프레임 재인코딩/재송출 방지.

## Config/VisionConfig.cs — MODIFIED (rev2)
- `MainCommunicatorPort` (`= 5104`) `[DataMember]` 제거. (구 json 의 해당 키는 역직렬화 시 무시됨)

## 검증 (rev2, 모니터 2에서 실행)
- 빌드 0/0.
- `viewer_test_grabgated.ps1`:
  - PHASE1 [접속만, 그랩X]: **PASS — 송출 없음**
  - PHASE2 [명령포트 `WaferVision|GRAB` 4회]: **PASS — 4 frames 수신** (각기 다른 내용)
- Handler 무변경(git), MainCommunicatorPort 소스에서 제거 확인.
