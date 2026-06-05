# Stage 85 Fix — 프로그램 시작 시 조명 컨트롤러 자동 시리얼 Open

## 증상
프로그램 첫 실행 시 LightHub 에 컨트롤러는 등록(`Initialize`)되지만 **시리얼이 안 열려** 있어, SetupUI "조명 연결" 버튼을 사용자가 한 번 눌러야 점등 가능.

## 원인
`Form1_Load`(`Form1.cs:77`)가 `LightHub.Initialize(...)`(인스턴스 등록)만 호출하고, 실제 시리얼 Open 인 `LightHub.ConnectAllAsync()` 는 SetupUI "조명 연결" 버튼(`LightSystemSetupPage.ConnectLights`)에서만 호출됨.

## 수정 (`QMC.Vision\Form1.cs` 단일 파일, +43/-0)
1. `Form1_Load` 의 `LightHub.Initialize` 직후 `_ = ConnectLightsOnStartupAsync();` 추가 (UI 비차단 fire-and-forget).
2. 신규 `ConnectLightsOnStartupAsync()` — `ConnectAllAsync()` 호출 후 ok/total/fails 집계, UI 스레드(`BeginInvoke`)에서 상태바 갱신. 전체 try/catch + `IsHandleCreated` 가드.
3. 신규 `UpdateLightStartupStatus(ok, total, fails)` — 상태바(`lblStatusR`)에 `| Light: N/N OK` / `| Light: N/N (실패: ...)` / `| Light: (인벤토리 비어 있음)` 덧붙임(덮어쓰기 X).

- SetupUI "조명 연결" 버튼은 **그대로 유지**(재연결용). 실패 포트는 LightHub 가 내부에서 `LIGHT-OPEN-FAIL` 알람 raise(기존 동작).

## 검증

| 시나리오 | 방법 | 결과 |
|---------|------|------|
| 1. Sim 시작 | LightHub 하니스 `Initialize(2 ctrl, useSim=true)` → `ConnectAllAsync` | ✅ total=2, ok=2 (Sim 항상 성공) |
| 3. 일부 포트 실패 | `Initialize(COM99, useSim=false)` → `ConnectAllAsync` | ✅ COM99=false, **예외 hub 흡수**(no throw) |
| 4. 빈 인벤토리 | `Initialize(empty)` → `ConnectAllAsync` | ✅ total=0, 예외 없음 |
| 시작 크래시 | 앱 실행 7초 생존 후 종료(LightUseSim=true) | ✅ fire-and-forget 자동연결로 startup 크래시 없음 |
| 빌드 | MSBuild QMC.Vision target | ✅ 오류 0 / 신규 경고 0 (선재 System.IO.Ports만) |
| 2. 실장비 LED | 실 LFine/Leesos HW 필요 | ⚠️ 미검증(HW 부재) — 단 호출 경로는 S1/S3 로 검증됨 |
| 5. 수동 버튼 회귀 | GUI 필요 | ⚠️ 미검증(GUI 자동화 불가) — 단 `ConnectLights` 코드 **미수정** |

> LightHub 하니스(`cdt-320\light-autoconnect-test\LightHubConnectTest.cs`)는 시작 자동연결이 호출하는 바로 그 `ConnectAllAsync` 경로 + ok/total/fails 집계를 직접 검증. UI 자동 클릭(computer-use)은 이 환경에서 미가용.

## ⚠️ vision.json 안내 (사용자 액션 필요)
실장비 점등을 보려면 `bin\Debug\Config\vision.json` 의 `"LightUseSim": false` 설정 필수. **현재 `true`(Sim)** — 이 fix 만으로는 Sim 컨트롤러가 자동 Open 될 뿐 실 LED 는 안 켜짐. 실장비 시리얼 연결 + `LightUseSim=false` 후 재실행하면 버튼 없이 자동 점등.

## 커밋
로컬 커밋 + master 로컬 머지. **remote push 안 함 — 사용자 컨펌 대기.**
