# CDT-320 Implementation Checklist — 원격 뷰어 (Vision 측)

> 참조: `04_design.md`. Stage 7 검증이 동일 체크리스트로 ✅/❌ 판정.

## A. VisionModule frame tap (R-011, R-005, R-006)
- [ ] A1. `Modules/VisionModule.cs` 에 `_lastFrame`(Bitmap), `_lastFrameUtc`, `_tapLock` 필드 추가
- [ ] A2. `Camera.FrameReceived` 구독 추가(ctor + SetCamera), SetCamera 에서 이전 카메라 구독 해제
- [ ] A3. `TapFrame(Bitmap)` — clone 후 lock 안에서 이전 프레임 Dispose + 교체 + 시각 갱신
- [ ] A4. `Grab()` 성공 분기에서 `g.Image != null` 일 때 `TapFrame(g.Image)` 호출 (기존 반환/이벤트 동작 불변)
- [ ] A5. `public Bitmap AcquireViewerFrame(int staleMs = 1500)` — fresh tap clone 우선, 없으면 self-grab clone, 그래도 없으면 마지막 tap clone, 다 실패 시 null
- [ ] A6. `Dispose()` 에서 `_lastFrame?.Dispose()` 추가
- [ ] A7. 모든 clone/grab 은 try/catch 로 격리(예외가 호출 스레드를 죽이지 않음)

## B. GrabStreamServer (R-001, R-003, R-007)
- [ ] B1. `Comm/GrabStreamServer.cs` 생성, namespace `QMC.Vision.Comm`, `sealed class : IDisposable`
- [ ] B2. 생성자 `(string name, int port, Func<Bitmap> frameProvider, int fps=10, long jpegQuality=60)`
- [ ] B3. 프로퍼티 `Name/Port/IsRunning/ConnectedClients`, 이벤트 `Status`
- [ ] B4. `Start()` — TcpListener 바인드 + capture 스레드 + accept 스레드 (background)
- [ ] B5. capture loop: 매 `1000/fps` ms `frameProvider()` 호출 → JPEG 인코딩 → latest+frameId 갱신 → 클라이언트 신호. 사용한 Bitmap 은 Dispose
- [ ] B6. accept loop: `NoDelay=true`, send 버퍼 확대, 클라이언트별 send 스레드 시작
- [ ] B7. send: `[4바이트 int32 LE 길이][JPEG 바이트]` + Flush (RemoteViewerControl 와 동일 프레이밍)
- [ ] B8. 새 프레임만 전송(frameId 비교로 중복 skip), 1:N 동시 클라이언트 지원
- [ ] B9. `Stop()/Dispose()` — listener/캡처/모든 클라이언트 스레드·소켓 정리
- [ ] B10. JPEG 인코더는 `ImageCodecInfo`(Jpeg) + Quality 파라미터 사용
- [ ] B11. ScreenRegion 캡처용 static 헬퍼 `CaptureScreenRegion(Rectangle)` 제공(또는 Form1 측 제공)

## C. VisionSettings (R-004, R-008)
- [ ] C1. `RemoteViewerEnable`(bool,true) 추가
- [ ] C2. `RemoteViewerSource`(string,"GrabImage") 추가
- [ ] C3. `RemoteViewerFps`(int,10), `RemoteViewerQuality`(int,60) 추가
- [ ] C4. 뷰어 포트 5개: Wafer 5200 / Inspection(Bottom) 5201 / Bin 5203 / FrontSide 5205 / RearSide 5206
- [ ] C5. ScreenRegion rect 4개(X/Y/W/H, 기본 0) 추가
- [ ] C6. 모두 `[DataMember]` — 기존 vision.json 로드 시 기본값으로 채워짐(마이그레이션 불필요)

## D. Form1 wiring (R-002, R-010, R-009)
- [ ] D1. `Form1` 에 viewer 서버 필드 5개 추가
- [ ] D2. `Form1_Load` 에서 `cfg.RemoteViewerEnable` 시 5개 viewer 생성+Start(try/catch), 소스 모드에 따라 provider 선택
- [ ] D3. GrabImage 모드 provider = `() => module.AcquireViewerFrame()`
- [ ] D4. ScreenRegion 모드 provider = `() => CaptureScreenRegion(rect)` (rect=config, 0,0,0,0→주모니터 전체)
- [ ] D5. `OnFormClosing` 에서 viewer 5개 `?.Dispose()`
- [ ] D6. 채널↔모듈 매핑이 정확: Wafer→WaferMod, Inspection포트→BottomMod, Bin→BinMod, FrontSide→FrontSideMod, RearSide→RearSideMod

## E. 프로젝트/빌드/제약 (R-009)
- [ ] E1. `QMC.Vision.csproj` 에 `<Compile Include="Comm\GrabStreamServer.cs" />` 등록
- [ ] E2. `QMC.CDT-320`(Handler) 디렉터리 내 파일 **0개** 수정 (git status 로 확인)
- [ ] E3. QMC.Vision 빌드 — error 0
- [ ] E4. QMC.Vision 빌드 — warning 0 (신규 코드 기준)

## F. 동작 검증 (R-003, R-006)
- [ ] F1. (헤드리스) 뷰어 포트에 TCP 접속 → `[4B len]` 읽고 len 바이트 수신 → 유효 JPEG(SOI 0xFFD8 / `new Bitmap(ms)` 디코드 성공) 확인
- [ ] F2. (헤드리스) 연속 2프레임 수신 시 frameId 진행(이미지 갱신)으로 "라이브" 동작 확인
- [ ] F3. 5개 포트 모두 listen 상태 확인(netstat 또는 접속 성공)
- [ ] F4. (사용자) SP_RemoteViewer 로 한 포트 접속 시 합성 그랩영상 표시 — 사용자 확인 항목으로 보고
