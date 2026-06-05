# STAGE 78 — CHECKLIST: 조명 batch + Mode + Leesos 매뉴얼 정정

- **작성일**: 2026-06-02
- **연계 SPEC**: `docs/STAGE78_SPEC_LightBatchAndLeesos.md`
- **단계**: CHECKLIST (구현 전, 사용자 컨펌 대기) — 문서까지만, 코드 변경 없음
- **검증 게이트**: build 0/0 + verify_all 회귀 0
- 각 항목 예/아니오로 객관 판정 가능

> ⚠ **선결**: SPEC §16 확인 5개 컨펌 전 구현 진입 금지. 라운드트립(Vendor/Mode 마이그레이션) 실패 시 정지.

---

## A. 데이터 모델
- [ ] `QMC.Common\Recipes\LightControllerMode.cs` — enum (Continuous=0/StrobeExternal=1/StrobeOnCommand=2)
- [ ] `LightControllerEntry.Mode` (`[DataMember(EmitDefaultValue=false)]`, 기본 `StrobeOnCommand`)
- [ ] `[OnDeserializing]` 에 `Mode = StrobeOnCommand` 추가 (구버전 JSON 키 없을 때)
- [ ] `LightControllerEntry.Clone()` 가 Mode 복제
- [ ] `LeesosLightConfig` MaxPower 255→**4095**, ChannelCount 8→**4**
- [ ] (LFine) entry.Mode → Config/adapter 전달 경로

## B. 인터페이스
- [ ] `ILightController.Mode { get; }` 추가
- [ ] `ILightController.SetChannelBatchAsync(int page, int[] values)` 추가
- [ ] 구현체 3종(LFine/Sim/Leesos) 모두 컴파일

## C. LFine
- [ ] `Mode => _cfg.Mode`
- [ ] `SetChannelBatchAsync` = `LFineProtocol.PageOnTimeFrame` 1프레임
- [ ] Mode != StrobeOnCommand 면 `_lastPageTimes[page]` 비교 후 동일 시 skip
- [ ] Mode == StrobeOnCommand 면 **무조건 송신**(skip 안 함)
- [ ] 송신 성공 시 `_lastPageTimes`/`_power`/`_lastOnPower` 갱신
- [ ] SC 채널 인덱싱(0-based WIP) 실장비 기준 확정 (확인 필요 #2)

## D. Leesos 정정 (매뉴얼 §5.3)
- [ ] `LeesosProtocol.EncodeChannel` — 1~9 / A~G(10~16), `ChannelAll="T"`
- [ ] `BuildVolumeCommand(ch,v)` → `LC{enc}{v:X3}` (0~4095)
- [ ] `BuildVolumeAllCommand(v)` → `LCT{v:X3}`
- [ ] `BuildOnOffCommand(ch,on)` → `LH{enc}{ON|OF}`
- [ ] `BuildStatusCommand(ch,type)` → `LS{enc}{00|01|02}`
- [ ] `ValidateEcho(r,ch,v)` == `R{enc}{v:X3}` / `ValidateAllEcho` == `RT{v:X3}`
- [ ] `IsErrorResponse` = `EndsWith("ER")` (**"ERR" 아님**)
- [ ] `LeesosLightController.Mode => Continuous`
- [ ] `SetPowerAsync` 0~4095 클램프 + 캐시 skip + LC + ValidateEcho
- [ ] `SetOnOffAsync` LH + R{ch}ER → LIGHT-NAK
- [ ] `SetChannelBatchAsync` 전체동일값=LCT, 그 외=LC loop(변경분만)
- [ ] `CheckPowerOnAsync` LS{ch}01 → R{ch}ON/OF 판정
- [ ] Strobe/SwitchPage no-op + true, `_ioGate` 직렬화

## E. Sim
- [ ] `Mode { get; }` (entry 모드 흉내)
- [ ] `SetChannelBatchAsync` 구현 + 송신/skip 카운터(검증용)

## F. Factory + Hub
- [ ] `SimLightController` 생성 시 entry.Mode 전달
- [ ] 어댑터가 entry.Mode 를 LFine Config 로 전달 (Leesos 는 Continuous 강제)
- [ ] LFine/Leesos/Sim 회귀 없음

## G. UI — LightSystemSetupPage
- [ ] Mode 컬럼(DataGridViewComboBox, Items=["Continuous","StrobeExternal","StrobeOnCommand"]) 추가 (컬렉션 끝 + DisplayIndex)
- [ ] BindAllCore/CollectFromUi 가 Mode 왕복
- [ ] 신규 추가 시 기본 Mode="StrobeOnCommand"
- [ ] Vendor=Leesos 시 Mode=Continuous + readonly (+ PageCount=1)
- [ ] Mode 변경 안내 라벨

## H. InspectionLightPanel
- [ ] `Apply`(:215) 의 채널별 SetPower/SetOnOff/SetStrobe foreach(:237-246) → times[] + `SetChannelBatchAsync` 1회
- [ ] 풀 밖 채널 `LIGHT-CHANNEL-OUT-OF-POOL` 유지
- [ ] On/Off 일원화 정책 확정 (확인 필요 #5)

## I. 검증
- [ ] `MSBuild` warning 0 / error 0
- [ ] verify_all 회귀 0 (현재 76/76 유지)
- [ ] 신규 verify 항목:
  - [ ] `LeesosProtocol.BuildVolumeCommand(1, 0xFFF) == "LC1FFF"`
  - [ ] `LeesosProtocol.BuildVolumeAllCommand(0x800) == "LCT800"`
  - [ ] `LeesosProtocol.BuildOnOffCommand(10, true) == "LHAON"` (A~G 인코딩)
  - [ ] `LeesosProtocol.IsErrorResponse("R1ER") == true`, `("R1OK") == false`
  - [ ] `LFineProtocol.PageOnTimeFrame` byte 시퀀스 == 기대
  - [ ] 구버전 JSON: Vendor 누락→"LFine", Mode 누락→StrobeOnCommand
  - [ ] Mode==StrobeOnCommand 시 동일값 재호출도 송신 (Sim 카운터)
- [ ] Apply 시나리오:
  - [ ] LFine StrobeOnCommand 100회 반복 → SP 100회(skip 0)
  - [ ] LFine StrobeExternal/Continuous 100회 → SP 1회 + 99 skip
  - [ ] Leesos Continuous 100회 → LC 1회 + 99 skip (LH On/Off 정책 #5)

## J. 문서
- [ ] `MISMATCH_RESOLUTION_LOG.md` M-78-1~6 추가 (본 Stage)
- [ ] `STAGE78_RESULT_*.md` (구현 Stage)

## K. Git
- [x] 브랜치 `stage-78-light-batch-leesos-spec`
- [ ] SPEC + CHECKLIST + MISMATCH 첫 커밋 (문서만)
