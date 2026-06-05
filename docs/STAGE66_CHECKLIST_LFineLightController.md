# STAGE 66 — CHECKLIST: LFine 디지털 조명 컨트롤러

- **연계 SPEC**: `docs/STAGE66_SPEC_LFineLightController.md`
- **작성일**: 2026-05-29
- **상태**: SPEC + CHECKLIST 작성 단계 (구현 미진입 — 사용자 컨펌 대기)
- **사전 조건**: SPEC §13 **전 항목 확정됨** (2026-05-29). 1=PS무시 / 2=Stx0x02·Etx0x03 / 3=OFF는Power0 / 4=무응답 / 5=컨트롤러2개(List) / 6=Vision로컬알람 / 7=클래스골격만(UI결선 제외)

> 위치: `QMC.Vision\Optics\`. UI 결선(F)은 #7 확정으로 **범위 외**.

## A. 인터페이스 / Sim
- [ ] `Optics\ILightController.cs` — 6 멤버 (Connect/Disconnect/SetPower/SetStrobeTime/SetOnOff/GetPower/CheckPowerOn) + IsConnected/PortName/ChannelCount
- [ ] `Optics\Sim\SimLightController.cs` — 캐시 + 로그, IsConnected=true, 0ms
- [ ] `Optics\LightControllerFactory.cs` — `Create(LFineLightConfig, bool useSim)`

## B. Config (컨트롤러 2개 확정 — #5)
- [ ] `Optics\LFine\LFineLightConfig.cs` — `LFineLightConfig` + `LFineChannel` DataContract (SPEC §5)
- [ ] **`List<LFineLightConfig>` 2개** (매뉴얼 Illuminator communicator 1·2) Load/Save — `Config\lfine_light.json`
- [ ] io_set.lightSource.json(8채널/COM1·2·3) → **2 컨트롤러로 재매핑** (어느 채널이 컨트롤러 1/2 인지 구현 시 확정, M-66-2)

## C. Protocol (LFineProtocol — 정적)
- [ ] `Optics\LFine\LFineProtocol.cs`
- [ ] `BuildPowerCommand(ch, power)` → `"{ch}P{power:000}R"` (코드 확인 포맷)
- [ ] `BuildStrobeTimeCommand(ch, us)` → `"{ch}T{us:000}R"`
- [ ] On/Off → **별도 명령 없음** (#3): `SetOnOffAsync(ch,false)`=`SetPowerAsync(ch,0)`, `true`=직전 저장 power 복원
- [ ] `WrapFrame(payload)` → `Stx(0x02) + payload + Etx(0x03)` (#2 확정)
- [ ] **응답 파싱 미구현** (#4 무응답) — `Receive`/`TryParseResponse` 생략
- [ ] 단위 시험 — Send 문자열 byte 비교 (예 `1P200R` → 02 31 50 32 30 30 52 03)

## D. LFineLightController (실장비)
- [ ] `Optics\LFine\LFineLightController.cs`
- [ ] SerialPort Open/Close + 명령 큐(Task + CTS) 직렬화
- [ ] 6 Async 메서드 구현 (SetPower/SetStrobeTime/SetOnOff/GetPower/CheckPowerOn/Connect)
- [ ] Power 범위 검사 (>MaxPower 거부 + `LIGHT-PWR-RANGE`)
- [ ] 타임아웃/NAK/송신예외 → `AlarmManager.Raise`
- [ ] Open 실패 → IsConnected=false + `LIGHT-OPEN-FAIL`

## E. 알람
- [ ] `QMC.Common\Alarms\AlarmMaster.cs` Vision 블록 끝에 6 LIGHT-* 코드 등록 (ko/en)
- [ ] 호출 지점 라이브 검증 (Open/Timeout/NAK/Invalid/Tx/Range)

## F. UI 결선 — **범위 외 (#7 클래스 골격만)** → 별도 Stage
- IlluminatorPanel 채널 가변 / 슬라이더 라이브 적용 / On·Off 버튼 / 상태 라벨 / Handler LightControllerPage 결선 — 전부 후속 Stage 로 분리

## G. Sim 검증
- [ ] Sim 모드 16채널 임의 시퀀스 → 알람 0 / 캐시값 일치
- [ ] 실장비 모드 포트 없음 → `LIGHT-OPEN-FAIL` 발생 확인

## H. 검증 / verify_all
- [ ] `dotnet build` warning 0 / error 0
- [ ] verify_all 회귀 없음 (현 63 PASS)
- [ ] `tools\verify_stage66.ps1` 신규 — "ILightController 6 메서드 시그니처 존재" + "BuildPowerCommand 출력 == `1P200R`"
- [ ] **실행 검증은 보조(2번) 모니터에서 수행** — 구현 Stage 프롬프트에 명시 (본 Stage 문서만 = N/A)

## I. 문서
- [ ] `MISMATCH_RESOLUTION_LOG.md` 5행 추가 (M-66-1~5)
- [ ] `ARCHITECTURE_EXPORT.md` 재생성 권장
- [ ] `STAGE67_RESULT_*.md` (구현 Stage 에서 작성)

## J. Git
- [ ] 브랜치 `stage-66-lfine-light-controller-spec` (완료)
- [ ] SPEC + CHECKLIST + MISMATCH 첫 커밋 (← 본 단계 종료점)

---

## 진행 순서
1. SPEC §13 확인 필요 #1·#3·#4·#5·#7 컨펌
2. 구현 Stage(67): A → B → C → D → E → F → G → H → I

## 예상 작업 시간
구현 Stage 67 기준 **6 ~ 9 시간**
