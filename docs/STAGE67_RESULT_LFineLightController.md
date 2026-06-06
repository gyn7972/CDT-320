# STAGE 67 — RESULT: LFine 조명 컨트롤러 구현 (클래스 골격)

- **작업 완료일**: 2026-05-29
- **브랜치**: `stage-67-lfine-light-controller-impl` (base: stage-66 SPEC)
- **연계**: `STAGE66_SPEC_LFineLightController.md` / `STAGE66_CHECKLIST_*`

## 사용자 확정 결정 반영 (Stage 66 §13)
| # | 결정 | 적용 |
|---|---|---|
| 1 | PS 변종 무시 | 미구현 (범위 외) |
| 2 | Stx=0x02 / Etx=0x03 | `LFineProtocol.Stx/Etx` |
| 3 | OFF = Power 0 | `SetOnOffAsync(false)`=Power 0, `true`=직전 저장 power 복원 |
| 4 | 무응답 유지 | 송신 전용, 응답 파싱 없음 |
| 5 | 컨트롤러 2개 | `LFineLightSetup.Controllers` List, CreateDefault 가 COM1/COM2 2대 |
| 6 | Vision 로컬 알람 | `QMC.Common.Alarms.AlarmManager.Raise` 직접 호출 (Vision 프로세스 로컬) |
| 7 | 클래스 골격만 | UI 결선(IlluminatorPanel) 미포함 — 별도 Stage |

## 신규 파일 (QMC.Vision\Optics\)
| 파일 | 역할 |
|---|---|
| `ILightController.cs` | 6 메서드 인터페이스 (Connect/SetPower/SetStrobeTime/SetOnOff/GetPower/CheckPowerOn) |
| `Sim\SimLightController.cs` | Sim — 캐시만 갱신, IsConnected=true, 0ms, Log 이벤트 |
| `LightControllerFactory.cs` | `Create(cfg, useSim)` — Sim 자동 fallback 안 함 (가짜통과 방지) |
| `LFine\LFineLightConfig.cs` | `LFineLightConfig`+`LFineChannel`+`LFineLightSetup`(2 컨트롤러)+`LFineLightConfigStore` (lfine_light.json) |
| `LFine\LFineProtocol.cs` | Stx/Etx + `BuildPowerCommand`/`BuildStrobeTimeCommand`/`WrapFrame` (송신 전용) |
| `LFine\LFineLightController.cs` | 실장비 — SerialPort + lock 직렬화 + 6 async + 알람 raise |

## 수정 파일
- `QMC.Common\Alarms\AlarmMaster.cs` — 6 LIGHT-* 알람 추가 (ko/en), 등록 총 51→57
- `QMC.Vision\QMC.Vision.csproj` — `System.IO.Ports` 참조 + Optics 6 Compile 등록
- `tools\verify_all.ps1` — AlarmMaster 카운트 57, Stage 67 항목 2개 추가
- `tools\verify_stage67.ps1` — 신규 (9 검사)

## 프로토콜 (코드 확인 기반)
- Power: `{ch}P{power:000}R` → 프레임 `02 {payload} 03` (예 `1P200R` → `02 31 50 32 30 30 52 03`)
- StrobeTime: `{ch}T{time:000}R`
- OFF: Power 0 전송. On/Off 전용 명령 없음.

## 알람 (6, Vision 로컬)
`LIGHT-OPEN-FAIL`(Error) / `LIGHT-TIMEOUT` / `LIGHT-NAK` / `LIGHT-INVALID-RESP` / `LIGHT-TX-FAIL` / `LIGHT-PWR-RANGE` (Warning)
- 현재 코드 raise 지점: OPEN-FAIL(ConnectAsync), PWR-RANGE(SetPower/SetStrobeTime 범위), TX-FAIL(미개방/쓰기예외), TIMEOUT(쓰기 타임아웃).
- NAK / INVALID-RESP 는 무응답 정책(#4)이라 현재 미발생 — 등록만 (응답 파싱 도입 시 사용).

## 검증
- **dotnet build**: solution clean — warning 0 / error 0
- **verify_stage67.ps1**: **9/9 PASS**
- **verify_all.ps1**: **65/65 PASS** (회귀 없음; AlarmMaster 57 카운트 반영)
- **Sim 스모크**: Vision 기동/응답 정상 (조명 드라이버는 미결선이라 기존 동작 불변)

## 비범위 / 다음 Stage
- **UI 결선** (IlluminatorPanel 채널 가변 + 슬라이더 라이브 적용 + On/Off 버튼) — 별도 Stage. **모니터 2번 검증 규칙** 적용.
- **검사별 조명 override** (Stage 62/64 카메라 오버라이드와 동일 형태) — 별도 Stage.
- **Handler 알람 배너 전파** (Vision→Handler TCP) — 별도 Stage.
- **응답 파싱** (NAK/INVALID 실사용) — 공급자 프로토콜 문서 확보 후.
- io_set.json 8채널 → 2 컨트롤러 실 포트/채널 매핑 확정 (M-67-1).
