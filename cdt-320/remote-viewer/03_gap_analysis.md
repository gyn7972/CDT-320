# CDT-320 Gap Analysis — 원격 뷰어 (그랩 영상 송출)

| Req ID | Status | Evidence in code | Notes |
|--------|--------|------------------|-------|
| R-001 모듈별 그랩 송출 서버 | ❌ Missing | 명령용 `Comm/VisionTcpServer.cs` 만 존재, 영상 송출 서버 없음 | 신규 `GrabStreamServer` 필요 |
| R-002 모듈 5채널 | ⚠️ Partial | 모듈/포트 5개 구조는 있음(Form1) | 뷰어용 서버 인스턴스 5개 추가 필요 |
| R-003 `[4B len][JPEG]` 프로토콜 | ⚠️ Partial | 프로토타입 `ScreenCaptureServer`/`RemoteViewerControl` 에 동일 프레이밍 존재 | 신규 서버에 동일 프레이밍 이식 |
| R-004 별도 뷰어 포트 | ❌ Missing | 명령포트만 정의(5100~5106) | VisionSettings 에 뷰어 포트 5개 추가 |
| R-005 혼합 소스(그랩+화면캡처) | ❌ Missing | 그랩=메모리 Bitmap 존재(GrabResult), 화면캡처=프로토타입 코드 존재 | 둘 다 지원하는 소스 추상화 필요 |
| R-006 단독 테스트 가시성 | ⚠️ Partial | SimCamera 라이브/단발 합성영상 생성 가능 | 뷰어 캡처 타이머가 프레임을 주기적으로 확보(필요 시 self-grab) |
| R-007 1:N 클라이언트 | ✅ (참고) | 프로토타입 `ScreenCaptureServer` 가 1:N 처리 | 동일 구조 이식 |
| R-008 설정 영속 + ON/OFF | ⚠️ Partial | `VisionSettings`/`VisionConfigStore` 패턴 존재 | 뷰어 항목 추가 |
| R-009 Handler 미수정 | ✅ (제약) | — | QMC.CDT-320 파일 절대 변경 금지 |
| R-010 종료 정리 | ⚠️ Partial | Form1.OnFormClosing 가 명령서버 Dispose | 뷰어 서버 Dispose 추가 |
| R-011 최소 침습 프레임 tap | ❌ Missing | Grab() 은 이미지 이벤트 미발행 | VisionModule 에 frame tap + AcquireViewerFrame 추가 |

## Summary
- Implemented(참고/제약 포함): 2 (R-007, R-009)
- Partial: 5 (R-002, R-003, R-006, R-008, R-010)
- Missing: 4 (R-001, R-004, R-005, R-011)

## Items to design (Stage 4)
- R-001/R-002/R-003/R-007: 신규 `Comm/GrabStreamServer.cs` (1:N, `[4B len][JPEG]`, FPS/품질, 5 인스턴스).
- R-005/R-006/R-011: `VisionModule` frame tap(`Grab()` 성공 + `Camera.FrameReceived`) + `AcquireViewerFrame()`(fresh tap 우선, stale 시 self-grab) ; 서버 소스 모드 GrabImage/ScreenRegion.
- R-004/R-008: `VisionSettings` 뷰어 포트/소스/FPS/품질/ON-OFF 추가.
- R-010: Form1 wiring(생성/Start/Dispose) + csproj 등록.
