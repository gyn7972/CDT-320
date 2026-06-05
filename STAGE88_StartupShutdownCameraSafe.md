# Stage 88 — 시작 시 카메라 Open 상태바 표시 + 종료 시 카메라 안전 Disconnect

## 목적
- 종료(`OnFormClosing`) 시 5개 VisionModule(카메라) 정리 누락 → 카메라 핸들 잔존으로 다음 실행 시 port 점유 가능. 안전 Disconnect 추가.
- 시작 시 카메라 자동 Open 결과를 상태바에 표시(조명 상태 옆).

## 변경 (`QMC.Vision\Form1.cs` 단일 파일, +23/-0)
1. **OnFormClosing** — `_svr*` Dispose 뒤, `LightHub.DisposeAll` 앞에 삽입(정리 순서: 뷰어→TCP→**카메라**→조명→Backend):
   - `WaferMod/BinMod/BottomMod/FrontSideMod/RearSideMod` 각 `?.Camera?.StopLive()` (라이브 중이면 grab 정지) → `?.Dispose()` (내부 `Camera.Dispose()`→`Close()`→핸들 해제). 전부 `try{}catch{}` 무음.
2. **Form1_Load** — `ShowTab(Tab.Operation)` 직전, 5개 모듈 `Camera.IsOpen` 집계 후 `lblStatusR` 에 `| Camera: N/5 OK` 덧붙임(local function `Tally`, 중복 방지 가드). 추가 알람 raise 없음(Open 실패는 `CreateCameraForAlgorithm` 이 이미 raise).

## 근거 (코드 확인)
- `VisionModule.Dispose()`(`VisionModule.cs:253-258`): FrameReceived 해제 + `Camera.Dispose()` + lastFrame 해제.
- `CameraBase.Dispose()`(`:106`) → `Close()`. SimCamera.Close → IsOpen=false. **HikGigECamera.Close**(`:101-107`) → `MV_CC_StopGrabbing` → `MV_CC_CloseDevice` → `MV_CC_DestroyDevice/DestroyHandle` → IsOpen=false → **실 디바이스 핸들/포트 해제**(재시작 시 점유 없음 근거).
- `ICamera`: `IsOpen`/`Close()`/`StopLive()` 존재 확인.

## 검증 (computer-use 미가용 → 헤드리스 + 코드확인)
| 항목 | 방법 | 결과 |
|------|------|------|
| 빌드 (MSBuild QMC.Vision) | | ✅ 오류 0 / 신규 경고 0 |
| 종료 시퀀스 핸들 해제 | 하니스 `cdt-320\camera-shutdown-test\`: SimCamera.Open → `Camera.StopLive()`+`VisionModule.Dispose()` → IsOpen=false | ✅ ALL PASS (예외 없음, 핸들 해제) |
| null 모듈 안전(`?.`) | 하니스 | ✅ |
| 실 카메라 재시작 port 점유(시나리오 2) | HikGigECamera.Close CloseDevice+DestroyHandle 코드 확인 | ✅ (코드근거) — 실 HW 라이브 미실측 |
| 시작 상태바 `Camera: N/5 OK`(시나리오 1) | 코드 리뷰(IsOpen 집계, null-safe) | ⚠️ Form1 전체 GUI 미기동으로 실표시 미확인 — 로직 단순 |
| 라이브 중 종료(시나리오 3) | StopLive→Dispose 경로(하니스에서 StopLive 포함 검증) | ✅ 경로 검증 |

> ⚠️ 실 GigE HW 연결 상태의 종료→즉시 재시작(port 점유)·상태바 실표시는 GUI/HW 필요로 라이브 미실측. 단 핸들 해제 로직은 헤드리스 PASS + Hik Close 코드(CloseDevice/DestroyHandle)로 확인.

## 규칙 준수
- 수정 범위 `Form1.cs` 만. 추가 알람 raise 없음. try/catch 무음 패턴 유지.

## 커밋
로컬 커밋 + master 머지 + 브랜치 삭제. **remote push 안 함 — 사용자 컨펌 대기.**
