# CDT-320 Design — 원격 뷰어 (Vision 측 그랩 영상 송출)

> 참조: `01_document_review.md`, `02_code_analysis.md`, `03_gap_analysis.md`
> 제약: **QMC.CDT-320(Handler) 미수정.** QMC.Vision 만 변경. SP_RemoteViewer 클라이언트 무수정 호환.

## 전체 구조
```
[QMC.Vision]
  WaferMod ──┐
  BottomMod ─┤  각 모듈   VisionModule.AcquireViewerFrame()  (fresh tap or self-grab)
  BinMod ────┤                  │ Bitmap
  FrontSideMod┤                 ▼
  RearSideMod┘   GrabStreamServer(port, ()=>module.AcquireViewerFrame())  ×5
                          │  capture loop @FPS → JPEG → latest frame
                          │  per-client send thread:  [4B LE len][JPEG]
                          ▼  TCP (포트 5200/5201/5203/5205/5206)
[SP_RemoteViewer]  RemoteViewerControl  ── 무수정 수신/표시
```

---

## R-011 — VisionModule frame tap + AcquireViewerFrame
**Layer:** Business Logic
**Approach:** 모듈이 이미 생산하는 프레임을 수동적으로(tap) 보관. 그랩 hot-path는 건드리되 최소화 — `Grab()` 성공 직후 마지막 프레임을 clone 보관, `Camera.FrameReceived`(라이브) 도 보관. 뷰어는 `AcquireViewerFrame()` 으로 가져감: 최근(<staleMs) tap 프레임이 있으면 그 clone, 없으면(=Handler/라이브 미동작) self-`Grab()`.
**Touched files:**
- modify: `Modules/VisionModule.cs`
**Threading:** `_lastFrame` 접근은 `_tapLock` 으로 보호. tap clone 은 짧음. self-grab 은 idle(>staleMs)에서만 → 실HW에서 Handler grab 과 충돌 최소.
**Public API sketch:**
```csharp
// VisionModule 에 추가
private readonly object _tapLock = new object();
private Bitmap _lastFrame;          // clone 소유
private DateTime _lastFrameUtc = DateTime.MinValue;

// ctor 및 SetCamera 에서 Camera.FrameReceived 구독/해제
private void OnCameraFrameReceived(GrabResult r) { if (r?.IsSuccess == true && r.Image != null) TapFrame(r.Image); }

private void TapFrame(Bitmap src)
{
    Bitmap clone; try { clone = (Bitmap)src.Clone(); } catch { return; }
    lock (_tapLock) { _lastFrame?.Dispose(); _lastFrame = clone; _lastFrameUtc = DateTime.UtcNow; }
}

// Grab() 성공 분기에 TapFrame(g.Image) 추가 (Image null 아닐 때)

/// <summary>뷰어용 프레임 1장(clone, 호출자가 Dispose). 최근 tap 우선, 없으면 self-grab.</summary>
public Bitmap AcquireViewerFrame(int staleMs = 1500)
{
    lock (_tapLock)
        if (_lastFrame != null && (DateTime.UtcNow - _lastFrameUtc).TotalMilliseconds < staleMs)
            try { return (Bitmap)_lastFrame.Clone(); } catch { }
    try { using (var g = Grab()) if (g.IsSuccess && g.Image != null) return (Bitmap)g.Image.Clone(); }
    catch { }
    lock (_tapLock) if (_lastFrame != null) try { return (Bitmap)_lastFrame.Clone(); } catch { }
    return null;
}
```
- Dispose 시 `_lastFrame?.Dispose()`.
- **기존 동작 불변**: `Grab()` 반환값/이벤트는 그대로, TapFrame 만 추가.

---

## R-001/R-002/R-003/R-005/R-006/R-007 — GrabStreamServer
**Layer:** Equipment Communication
**Approach:** 프로토타입 `ScreenCaptureServer` 의 1:N 구조(캡처 루프 + latest-frame + frameId + 클라이언트별 send 스레드 + `[4B len][JPEG]`)를 이식하되, **프레임 소스를 `Func<Bitmap>` 으로 주입**. ScreenRegion 모드는 화면 사각형 캡처 함수를 주입.
**Touched files:**
- new: `Comm/GrabStreamServer.cs` (namespace `QMC.Vision.Comm`)
**Threading:** accept 스레드 + capture 스레드 + 클라이언트별 send 스레드(프로토타입과 동일). 모두 background.
**Public API sketch:**
```csharp
namespace QMC.Vision.Comm
{
    public sealed class GrabStreamServer : IDisposable
    {
        public GrabStreamServer(string name, int port, Func<Bitmap> frameProvider,
                                int fps = 10, long jpegQuality = 60);
        public string Name { get; }
        public int Port { get; }
        public bool IsRunning { get; }
        public int ConnectedClients { get; }
        public event Action<string> Status;   // 로그용(옵션)
        public void Start();   // listener + capture loop + accept loop
        public void Stop();
        public void Dispose(); // == Stop
    }
}
```
- capture loop: 매 `1000/fps` ms → `bmp = frameProvider()` → `CompressJpeg(bmp, quality)` → `_latest`/`_frameId` 갱신 → 클라이언트 신호. bmp 는 사용 후 Dispose(provider 가 clone 을 줌).
- send: `BitConverter.GetBytes(bytes.Length)`(int32 LE) + bytes + Flush. (RemoteViewerControl 호환)
- `tcpClient.NoDelay = true`, `SendBufferSize` 확대.
- 포트 바인드 실패/예외는 격리(`Start()` 가 throw 시 호출부에서 try/catch, 명령서버 패턴과 동일).

### ScreenRegion 소스(혼합)
```csharp
// Form1 에서 소스 모드에 따라 provider 선택
Func<Bitmap> provider = cfg.RemoteViewerSource == "ScreenRegion"
    ? (Func<Bitmap>)(() => CaptureScreenRegion(rect))   // 화면영역 캡처(전체 화면 또는 config rect)
    : (() => module.AcquireViewerFrame());              // 기본: 그랩 이미지
```
- `CaptureScreenRegion` 헬퍼는 GrabStreamServer 의 static util 또는 Form1 private. rect 기본값 = 주 모니터 전체(설정 0,0,0,0 → 전체).

---

## R-004/R-008 — VisionSettings 뷰어 설정
**Layer:** Data/Settings
**Touched files:** modify `Config/VisionConfig.cs`
**추가 `[DataMember]` (기본값):**
```csharp
[DataMember] public bool   RemoteViewerEnable        { get; set; } = true;
[DataMember] public string RemoteViewerSource        { get; set; } = "GrabImage"; // or "ScreenRegion"
[DataMember] public int    RemoteViewerFps           { get; set; } = 10;
[DataMember] public int    RemoteViewerQuality       { get; set; } = 60;
[DataMember] public int    WaferViewerPort           { get; set; } = 5200;
[DataMember] public int    InspectionViewerPort      { get; set; } = 5201; // Bottom
[DataMember] public int    BinViewerPort             { get; set; } = 5203;
[DataMember] public int    FrontSideViewerPort       { get; set; } = 5205;
[DataMember] public int    RearSideViewerPort        { get; set; } = 5206;
// ScreenRegion 용(0,0,0,0 = 주 모니터 전체)
[DataMember] public int    RemoteViewerScreenX       { get; set; } = 0;
[DataMember] public int    RemoteViewerScreenY       { get; set; } = 0;
[DataMember] public int    RemoteViewerScreenW       { get; set; } = 0;
[DataMember] public int    RemoteViewerScreenH       { get; set; } = 0;
```
- 기존 vision.json 에 없는 키는 DataContract 기본값으로 채워짐 → 마이그레이션 불필요.

---

## R-010 — Form1 wiring + 종료 정리
**Layer:** Host
**Touched files:** modify `Form1.cs`, `QMC.Vision.csproj`
**Form1_Load** (명령서버 Start 직후):
```csharp
if (cfg.RemoteViewerEnable)
{
    _viewWafer     = MakeViewer("Wafer",     cfg.WaferViewerPort,      WaferMod,     cfg);
    _viewBottom    = MakeViewer("Bottom",    cfg.InspectionViewerPort, BottomMod,    cfg);
    _viewBin       = MakeViewer("Bin",       cfg.BinViewerPort,        BinMod,       cfg);
    _viewFrontSide = MakeViewer("FrontSide", cfg.FrontSideViewerPort,  FrontSideMod, cfg);
    _viewRearSide  = MakeViewer("RearSide",  cfg.RearSideViewerPort,   RearSideMod,  cfg);
}
// MakeViewer: provider 선택(GrabImage/ScreenRegion) → new GrabStreamServer → try{Start();}catch{}
```
**OnFormClosing**: 5개 viewer `?.Dispose()` 추가.
**csproj**: `<Compile Include="Comm\GrabStreamServer.cs" />` 추가.

---

## Open question (escalate to user) — 없음
설계상 사용자 결정이 필요한 추가 항목 없음(4개 핵심 결정은 답변으로 확정). 뷰어 포트 기본값은 명령포트 미러링(+100)으로 확정하며 vision.json 으로 변경 가능.

## 비변경 보장 (R-009)
- `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\` (Handler) 내 파일 0개 수정.
- 변경 파일은 전부 `...\QMC.Vision\` 하위.
