# STAGE 79 — RESULT: 조명 batch + Mode + Leesos 매뉴얼 정정 구현

- **작성일**: 2026-06-02
- **연계**: `STAGE78_SPEC_LightBatchAndLeesos.md` / `STAGE78_CHECKLIST_LightBatchAndLeesos.md`
- **브랜치**: `stage-79-light-batch-leesos-impl` → master ff
- **사용자 결정**: Spec 변경사항 확인 후 바로 구현 (SPEC 기본값)

---

## 1. 구현 요약
1. **batch**: `ILightController.SetChannelBatchAsync(page, int[])` 신설. LFine = **SP 1프레임**(`PageOnTimeFrame`) → 채널별 SC foreach 제거(tact-time 해소).
2. **Mode**: `LightControllerMode`(Continuous/StrobeExternal/StrobeOnCommand) + `LightControllerEntry.Mode`. **StrobeOnCommand 면 캐시 skip 안 함**(매 호출 송신) — 영남님 미발사 시나리오 차단.
3. **Leesos 매뉴얼 정정**: 8-bit→**12-bit(X3, 0~4095)**, 에러 `"ERR"`→**`"ER"`**, 채널 **1~9/A~G/T**, MaxPower **4095**, ChannelCount **4**, 전체동일값 **LCT** batch.

## 2. 변경/신규 파일
| 파일 | 구분 |
|---|---|
| `QMC.Common\Recipes\LightControllerMode.cs` | 신규 enum |
| `QMC.Common\Recipes\LightSystemSetup.cs` | LightControllerEntry.Mode + 마이그레이션 + Clone |
| `QMC.Vision\Optics\ILightController.cs` | Mode 프로퍼티 + SetChannelBatchAsync |
| `QMC.Vision\Optics\LFine\LFineLightConfig.cs` | Mode 필드 |
| `QMC.Vision\Optics\LFine\LFineLightController.cs` | Mode + SetChannelBatchAsync(SP, Mode-gated skip) + SC 채널 0-base(WIP 정식화) |
| `QMC.Vision\Optics\Sim\SimLightController.cs` | Mode + SetChannelBatchAsync + BatchSendCount(테스트) |
| `QMC.Vision\Optics\Leesos\LeesosProtocol.cs` | 매뉴얼 정정(EncodeChannel/LCT/12-bit/ValidateEcho/IsErrorResponse "ER") |
| `QMC.Vision\Optics\Leesos\LeesosLightController.cs` | 매뉴얼 재작성(Mode=Continuous, batch LCT/LC, echo 검증) |
| `QMC.Vision\Optics\Leesos\LeesosLightConfig.cs` | MaxPower 4095 / ChannelCount 4 |
| `QMC.Vision\Optics\LightControllerFactory.cs` | Sim/LFine 에 entry.Mode 전달 |
| `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` | Mode 콤보 컬럼(+Vendor=Leesos 시 Continuous readonly) |
| `QMC.Vision\Ui\Pages\InspectionLightPanel.cs` | Apply 채널별 foreach → SetChannelBatchAsync 1회 |
| `QMC.Common\QMC.Common.csproj` / `tools\verify_all.ps1` | 등록 / STAGE79 6행 |

## 3. 검증 결과
- **빌드**: 경고 0 / 오류 0
- **verify_all**: **82/82 PASS** (76 + STAGE79 6)
- **런타임(reflection)**:
  - Leesos: `BuildVolumeCommand(1,0xFFF)`=`LC1FFF`, `BuildVolumeAllCommand(0x800)`=`LCT800`, `BuildOnOffCommand(10,true)`=**`LHAON`**(A 인코딩), `BuildStatusCommand(1,OnOff)`=`LS101` ✓
  - `IsErrorResponse("R1ER")`=True / `("R1OK")`=False ✓ (**"ER"** 기준), `ValidateEcho("R1FFF",1,4095)`=True ✓
  - LFine `BuildChannelOnTimeCommand(1,3,50)`=`SC0102;050` (채널 3 → 0-base "02") ✓
  - **마이그레이션**: 구버전 JSON(키 없음) → Vendor=`LFine`, Mode=**`StrobeOnCommand`** ✓
  - **Mode skip 핵심**: Sim Continuous 동일값 2회 → 송신 **1회**(skip) / **StrobeOnCommand 2회 → 송신 2회(skip 안 함)** ✓
- **앱 스모크**: Sim `Responding=True`
- **UI 렌더**: Vendor + Mode 콤보가 PortName 옆 정상 표시 (DrawToBitmap)

## 4. 설계 결정 (SPEC 기본값 적용)
- 확인 필요 #1 timeout 1000ms / #2 LFine SC 0-base(WIP 유지) / #3 ChannelCount 4 / #4 Mode 기본 StrobeOnCommand / #5 **Apply On/Off = Level0=OFF batch 일원화**(별도 SetOnOff 제거, Strobe 호출 제거 — 밝기=on-time 으로 흡수).
- LFine 단일 `SetPowerAsync` 는 기존대로 매번 송신(skip 없음) → batch 에만 Mode-gated skip.
- Leesos `Mode` 강제 Continuous(UI readonly), batch 는 전체동일값 LCT 우선.

## 5. 알람
신규 0. 기존 LIGHT-* 7종 재사용 (개수 60 불변).

## 6. 남은 항목 (실 장비)
- **hex dump 1회** — LFine SP(채널 0-base) / Leesos LC/LH/LS·LCT 송수신 캡처해 실펌웨어 대조 (확인 필요 #2).
- StrobeOnCommand 발사 시점 ↔ 카메라 노출 동기 측정.
- 런타임 자동 적용(Form1 그랩 사이클 결선) — 별도 Stage.
- 실행 검증은 보조 모니터(2번)에서.
