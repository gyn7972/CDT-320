# CDT-320 Verification — 원격 뷰어 (Vision 측 그랩 영상 송출)

> 참조: `05_checklist.md`. 검증 3 라운드 수행.
> 런타임 증거: QMC.Vision.exe 기동 → 헤드리스 TCP 클라이언트(`06_implementation/viewer_test_client.ps1`)로 5개 포트 프레임 수신/디코드.

## 검증 라운드 요약
- **Round 1** — 빌드 0/0 OK. 그러나 5개 포트 모두 연결거부(FAIL). 원인: `DataContractJsonSerializer` 가 프로퍼티 이니셜라이저를 실행하지 않아 구버전 `vision.json` 로드 시 `RemoteViewerEnable=false`, 포트=0 → 서버 미기동.
- **Round 2** — 수정(`[OnDeserializing]` 기본값 + `[OnDeserialized]` 자가치유) 후 재빌드 0/0. 5개 포트 전부 PASS(유효 JPEG, 2프레임 갱신). 자가치유로 degraded json 도 자동 정상화.
- **Round 3** — git diff 리뷰로 VisionModule/Form1 수정 정합성 확인. 체크리스트-구현 일치 재확인, 누락 없음.

## A. VisionModule frame tap
- ✅ A1 `_lastFrame/_lastFrameUtc/_tapLock` 추가 — VisionModule.cs
- ✅ A2 `Camera.FrameReceived` 구독/해제(ctor, SetCamera) — diff 확인
- ✅ A3 `TapFrame` clone+lock+이전 Dispose
- ✅ A4 `Grab()` 성공 시 `TapFrame(g.Image)` (기존 반환/EPD 불변)
- ✅ A5 `AcquireViewerFrame(staleMs=1500)` fresh→self-grab→last→null
- ✅ A6 `Dispose()` 에서 FrameReceived 해제 + `_lastFrame` Dispose
- ✅ A7 clone/grab 전부 try/catch 격리

## B. GrabStreamServer
- ✅ B1 `Comm/GrabStreamServer.cs`, `sealed : IDisposable`, ns `QMC.Vision.Comm`
- ✅ B2 생성자 `(name, port, Func<Bitmap>, fps=10, q=60)`
- ✅ B3 `Name/Port/IsRunning/ConnectedClients` + `Status` 이벤트
- ✅ B4 `Start()` listener+capture+accept (background)
- ✅ B5 capture loop fps 주기, JPEG 인코딩, 사용 Bitmap Dispose
- ✅ B6 accept: NoDelay, SendBufferSize, 클라이언트별 send 스레드
- ✅ B7 `[4B int32 LE][JPEG]` — **클라이언트가 길이/JPEG 디코드 성공으로 입증** (f1/f2 dec=True)
- ✅ B8 frameId 비교로 신규 프레임만 전송, 1:N
- ✅ B9 `Stop()/Dispose()` 정리 — exe 종료 시 stopped clean
- ✅ B10 ImageCodecInfo(Jpeg)+Quality
- ✅ B11 static `CaptureScreenRegion(Rectangle)` (Empty→주 모니터)

## C. VisionSettings
- ✅ C1 `RemoteViewerEnable` (런타임 config Enable=True)
- ✅ C2 `RemoteViewerSource` ("GrabImage")
- ✅ C3 `RemoteViewerFps`(10)/`RemoteViewerQuality`(60)
- ✅ C4 포트 5200/5201/5203/5205/5206 (런타임 config 확인)
- ✅ C5 ScreenRect X/Y/W/H
- ✅ C6 `[DataMember]` + **추가**: `[OnDeserializing]`/`[OnDeserialized]` 로 구버전 json 자가치유 (Round 1 결함 수정)

## D. Form1 wiring
- ✅ D1 viewer 필드 5개
- ✅ D2 `RemoteViewerEnable` 시 5개 생성+Start(try/catch)
- ✅ D3 GrabImage provider = `mod.AcquireViewerFrame`
- ✅ D4 ScreenRegion provider = `CaptureScreenRegion(rect)`
- ✅ D5 `OnFormClosing` viewer 5개 Dispose
- ✅ D6 매핑 정확: Wafer→WaferMod, InspectionViewerPort→BottomMod, Bin→BinMod, FrontSide→FrontSideMod, RearSide→RearSideMod (diff 확인)

## E. 프로젝트/빌드/제약
- ✅ E1 csproj `Comm\GrabStreamServer.cs` 등록 (rebuild 컴파일 목록에 포함)
- ✅ E2 Handler(QMC.CDT-320/QMC.CDT-320) **0개 변경** — `git status` 확인
- ✅ E3 빌드 error 0
- ✅ E4 빌드 warning 0

## F. 동작 검증 (런타임)
- ✅ F1 유효 JPEG: 5포트 전부 SOI(0xFFD8)+`Image.FromStream` 디코드 성공
- ✅ F2 라이브 갱신: 2프레임 수신, 내용 갱신(예: 5203 len 31052→31030)
- ✅ F3 5개 포트 listen: 5200/5201/5203/5205/5206 전부 접속+수신 성공
- ⏳ F4 SP_RemoteViewer GUI 표시: **사용자 확인 항목**. 와이어 프로토콜은 RemoteViewerControl 과 동일(`[4B len][JPEG]`, len≤10MB, `new Bitmap(ms)`)임을 헤드리스로 입증 → SP_RemoteViewer 에서 IP=127.0.0.1, Port=5200~5206 입력 시 표시 예상.

## 런타임 측정값 (Round 2)
```
PORT 5200 (Wafer)     : PASS  640x480   ~31KB/frame
PORT 5201 (Bottom)    : PASS  640x480   ~31KB/frame
PORT 5203 (Bin)       : PASS  640x480   ~31KB/frame
PORT 5205 (FrontSide) : PASS  5120x5120 ~412KB/frame   ← 해상도 큼(관찰)
PORT 5206 (RearSide)  : PASS  640x480   ~31KB/frame
config: Enable=True Source=GrabImage Fps=10 Q=60 Ports=5200,5201,5203,5205,5206
```

## Summary
- 체크리스트 항목 총: 39
- Passed(✅): 38
- 사용자 확인 대기(⏳): 1 (F4 — SP_RemoteViewer GUI 표시)
- Failed(❌): 0

## 관찰 / 후속(선택)
- **FrontSide(5205) 그랩 해상도 5120×5120** → 10fps 시 ~4MB/s/클라이언트. 송출 자체는 정상(디코드 OK)이나, 대역폭이 큰 환경이면 해당 채널 FPS/품질을 낮추거나 송출 전 다운스케일 옵션 고려 가능(현재 범위 외).
- ScreenRegion 모드는 코드/검증은 됐으나 기본값 GrabImage 로 런타임 테스트(ScreenRegion 은 단일 사각형 미러). 필요 시 `vision.json` 에서 `RemoteViewerSource="ScreenRegion"` 로 전환.
- 실 Hik 카메라에서 viewer self-grab 과 Handler grab 동시성: idle(>1.5s)에서만 self-grab 하도록 제한됨(passive tap 우선).
