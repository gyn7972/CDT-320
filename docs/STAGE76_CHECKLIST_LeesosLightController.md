# STAGE 76 — CHECKLIST: LeesOS 디지털 조명 컨트롤러 (2차 벤더)

- **작성일**: 2026-06-02
- **연계 SPEC**: `docs/STAGE76_SPEC_LeesosLightController.md`
- **단계**: CHECKLIST (구현 전, 사용자 컨펌 대기) — 본 Stage 는 문서까지만, 코드 변경 없음
- **검증 게이트**: build clean(0/0) + verify_all 회귀 0 + 실 장비 검증은 **보조 모니터(2번)**
- 각 항목은 **예/아니오로 객관 판정 가능**하게 작성 (구현 Stage 의 verify 에서 그대로 사용)

> ⚠ **선결 조건**: SPEC §13 의 **확인 필요 7개**(특히 #2 명령 포맷, M-76-2) 사용자 컨펌 + (매뉴얼 부재 시) **실 장비 hex dump 1회** 전까지 구현 진입 금지.

---

## A. 데이터 모델
- [ ] `LightControllerEntry` 에 `[DataMember(EmitDefaultValue=false)] string Vendor` 추가 (기본 "LFine")
- [ ] `LightControllerEntry` 에 `[OnDeserializing]` 콜백 추가 — `Vendor="LFine"` 기본값 주입 (이니셜라이저 미실행 대응)
- [ ] `[OnDeserialized]` 보강 — Vendor 빈 값이면 "LFine" (이중 안전)
- [ ] `LightControllerEntry.Clone()` 가 Vendor 복제
- [ ] `LeesosLightConfig` + `LeesosChannel` `[DataContract]` 정의 (기본값 SPEC §5)
- [ ] 어댑터: `ToLFineConfig(this LightControllerEntry)` / `ToLeesosConfig(this LightControllerEntry)`

## B. Protocol (`LeesosProtocol` — 정적 클래스)
- [ ] `BuildVolumeCommand(ch, power)` → `"LC{ch}{power:X2}"` (예: ch=1,power=255 → `"LC1FF"`)
- [ ] `BuildOnOffCommand(ch, on)` → `"LH{ch}{ON|OF}"` (예: ch=2,on=true → `"LH2ON"`)
- [ ] `BuildStatusCommand(ch)` → `"LS{ch}01"` (예: ch=1 → `"LS101"`)
- [ ] `WrapFrame(text)` → ASCII + `{0x0D,0x0A}` (Etx1 Etx2)
- [ ] `TryParseResponse(string raw, out LeesosResponse)` → `R` prefix 검증, `RERR`(EndsWith "ERR") 분기, 본문 파싱
- [ ] 명령 prefix/Etx 는 **실제 코드(LH/LC/LS, 0x0D 0x0A)** 기준 — 주석(H/C/SPWR) 아님 (M-76-2)
- [ ] 단위 시험: 송신 문자열 byte 대조 + 응답 sample 파싱

## C. LeesosLightController (실장비)
- [ ] `ILightController` **9 메서드 + 3 속성** 전부 구현 (SPEC §4)
- [ ] SerialPort Open/Close + `SemaphoreSlim _ioGate` 로 (송신→수신) 쌍 직렬화
- [ ] `SetPowerAsync` = Volume(`LC`) 송신 + 응답 검증, Power **0~255 클램프** + 범위밖 `LIGHT-PWR-RANGE`
- [ ] `SetOnOffAsync` = `LH{ch}ON|OF` 송신 + 응답 검증 (OFF→ON 복원은 lastOn 캐시)
- [ ] `SetStrobeTimeAsync` = **no-op + true** (LeesOS Strobe 미지원, 확인 필요 #4)
- [ ] `SwitchPageAsync` = **no-op + true** (Page 미지원)
- [ ] `CheckPowerOnAsync` = `LS{ch}01` 송신 + `Substring(2,2)=="ON"` 판정 (확인 필요 #5)
- [ ] `ReceiveResponseAsync` = ReadTo(0x0A) → `R` 검증, `RERR` 시 `LIGHT-NAK`+null, timeout 시 null
- [ ] 응답 timeout → `LIGHT-TIMEOUT`, 형식불일치 → `LIGHT-INVALID-RESP`
- [ ] 포트 미개방 송신 → `LIGHT-TX-FAIL` / Open 실패 → `LIGHT-OPEN-FAIL`
- [ ] `Dispose` 가 포트 Close + 리소스 해제

## D. Factory + Hub
- [ ] `LightControllerFactory.Create(LightControllerEntry, bool useSim)` 신규 시그니처 + 벤더 switch
- [ ] 어댑터 `ToLFineConfig`/`ToLeesosConfig` 로 entry→Config
- [ ] 알 수 없는 Vendor → `LIGHT-MAP-INVALID` + Sim fallback
- [ ] 기존 `Create(LFineLightConfig,bool)` 호환 유지(또는 내부 위임)
- [ ] `LightHub.Initialize` 가 entry 기반 `Create(entry, useSim)` 호출 (벤더별 인스턴스 자동)
- [ ] LFine 단독 구성 회귀 없음 (기존 동작 동일)

## E. UI — LightSystemSetupPage
- [ ] `_gridCtrl` 에 `Vendor` 컬럼(`DataGridViewComboBoxColumn`, Items=["LFine","Leesos"]) 추가 (PortName 옆)
- [ ] `BindAllCore` 가 entry.Vendor 를 셀에 반영, `CollectFromUi` 가 셀→entry.Vendor 수집
- [ ] 신규 컨트롤러 추가 시 기본 Vendor="LFine"
- [ ] Vendor 변경 시 ChannelCount/PageCount/MaxPower 기본값 자동 제안 (현재 0/빈 값일 때만)
- [ ] (옵션) Vendor=Leesos 시 PageCount 셀 readonly 1 (확인 필요 #7)
- [ ] `DataError` 억제로 콤보 외 값 입력 예외 방지

## F. Sim 모드 검증
- [ ] `SimLightController` 가 벤더 무관 그대로 동작 (코드 변경 0)
- [ ] Sim 모드 + Vendor=Leesos 임의 시퀀스 → 알람 0 / 캐시 값 일치
- [ ] 실장비 모드 + 포트 미확보 → `LIGHT-OPEN-FAIL` 알람

## G. 검증
- [ ] `MSBuild` warning 0 / error 0
- [ ] `verify_all` 회귀 0 (현재 72/72 유지)
- [ ] 신규 verify 항목 추가:
  - [ ] `LeesosProtocol.BuildVolumeCommand(1, 0xFF) == "LC1FF"`
  - [ ] `LeesosProtocol.BuildOnOffCommand(2, true) == "LH2ON"`
  - [ ] `LeesosProtocol.BuildStatusCommand(1) == "LS101"`
  - [ ] 구버전 JSON 라운드트립: Vendor 누락 → "LFine" 자동 채움
- [ ] (알람 추가 시) `AlarmMaster` LIGHT-* 개수 검사 갱신
- [ ] **실 장비 hex dump 1회** — Send/Recv 한 프레임 캡처 후 SPEC §3·§8 포맷과 대조 (구현 Stage 검증 절)
- [ ] **실행 검증은 보조 모니터(2번)** 에서 (구현 Stage 프롬프트에 명시)

## H. 문서
- [ ] `MISMATCH_RESOLUTION_LOG.md` 에 M-76-1~5 추가 (본 Stage 에서 기록)
- [ ] `ARCHITECTURE_EXPORT.md` 재생성 권장 (벤더 분기 도입 — 구현 Stage)
- [ ] `STAGE76_RESULT_LeesosLightController.md` (구현 Stage)

## I. Git
- [x] 브랜치 `stage-76-leesos-light-controller-spec`
- [ ] SPEC + CHECKLIST + MISMATCH 로그 첫 커밋
