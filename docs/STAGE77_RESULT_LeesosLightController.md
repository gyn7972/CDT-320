# STAGE 77 — RESULT: LeesOS 디지털 조명 컨트롤러 (2차 벤더) 구현

- **작성일**: 2026-06-02
- **연계**: `STAGE76_SPEC_LeesosLightController.md` / `STAGE76_CHECKLIST_LeesosLightController.md`
- **브랜치**: `stage-77-leesos-light-controller-impl` → master ff 병합
- **사용자 결정**: 코드 기준(LH/LC/LS)로 구현 진행, 확인 필요 #1·3·4·6 은 SPEC 기본값

---

## 1. 구현 요약
기존 LFine 구조(ILightController/LightHub/Factory) 위에 **2차 벤더 LeesOS** 추가. 새 인터페이스·새 알람·새 화면 없음.

## 2. 변경/신규 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `QMC.Common\Recipes\LightSystemSetup.cs` | 수정 | `LightControllerEntry.Vendor`("LFine"\|"Leesos") + `[OnDeserializing]`/`[OnDeserialized]` 마이그레이션 + Clone |
| `QMC.Vision\Optics\Leesos\LeesosLightConfig.cs` | 신규 | `LeesosLightConfig`(COM2/9600/8N1/None/1000ms/Max255) + `LeesosChannel` |
| `QMC.Vision\Optics\Leesos\LeesosProtocol.cs` | 신규 | 정적 명령 빌더(LC/LH/LS) + WrapFrame(\r\n) + Classify/ParseStatusOn |
| `QMC.Vision\Optics\Leesos\LeesosLightController.cs` | 신규 | ILightController 9메서드+3속성, 응답형(SemaphoreSlim 직렬화, 블로킹 I/O 백그라운드) |
| `QMC.Vision\Optics\LightControllerFactory.cs` | 수정 | `Create(LightControllerEntry,bool)` 벤더 switch + `ToLFineConfig`/`ToLeesosConfig` 어댑터. 구 시그니처 유지 |
| `QMC.Vision\Comm\LightHub.cs` | 수정 | `Initialize` 가 entry 기반 `Create(entry,useSim)` 호출 |
| `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` | 수정 | Vendor 콤보 컬럼(컬렉션 index6 + DisplayIndex1) + bind/collect/add + Vendor변경 핸들러 |
| `QMC.Vision\QMC.Vision.csproj` | 수정 | Leesos 3파일 Compile 등록 |
| `tools\verify_all.ps1` | 수정 | STAGE77 검증 4행 추가 |

## 3. 프로토콜 (실제 코드 기준 — LH/LC/LS)
- 프레임 = ASCII + `0x0D 0x0A`
- `BuildVolumeCommand(ch,power)` = `LC{ch}{power:X2}` (0~255)
- `BuildOnOffCommand(ch,on)` = `LH{ch}{ON|OF}`
- `BuildStatusCommand(ch)` = `LS{ch}01`, 응답 `Substring(2,2)=="ON"`
- 응답: 정상 `R...`, NAK `RERR`(EndsWith "ERR") → `LIGHT-NAK`
- 매핑: 밝기=Volume(LC), Strobe/Page 미지원(no-op+true), CheckPowerOn=LS

## 4. 검증 결과
- **빌드**: 경고 0 / 오류 0
- **verify_all**: **76/76 PASS** (기존 72 + STAGE77 4)
- **런타임(reflection)**:
  - `BuildVolumeCommand(1,255)`=`LC1FF` / `BuildOnOffCommand(2,true)`=`LH2ON` / `BuildStatusCommand(1)`=`LS101` ✓
  - `Classify`: R1ON→Ok, RERR→Nak, null→Invalid ✓ / `ParseStatusOn`: R1ON→True, R1OF→False ✓
  - **Vendor 마이그레이션**: 구버전 JSON(Vendor 키 없음) 로드 → `"LFine"` ✓
- **앱 스모크**: Sim 모드 `Responding=True`
- **UI 렌더**: Vendor 콤보가 PortName 옆(DisplayIndex1)에 정상 표시 (오프스크린 DrawToBitmap)

## 5. 알람
신규 0. 기존 `LIGHT-OPEN-FAIL`/`LIGHT-TIMEOUT`/`LIGHT-NAK`/`LIGHT-INVALID-RESP`/`LIGHT-TX-FAIL`/`LIGHT-PWR-RANGE`/`LIGHT-MAP-INVALID` 재사용 (AlarmMaster 이미 등록 — 개수 60 불변).

## 6. 설계 메모 / SPEC 대비 차이
- Vendor 컬럼은 **컬렉션 끝(index6)에 추가 + DisplayIndex=1** 로 PortName 옆 표시 → 기존 인덱스 기반 셀 접근(Cells[0], IntOf r,2..5) 보존.
- Vendor=Leesos 선택 시 PageCount=1 자동 + MaxPower 0~255 보정(>255/0 일 때). PageCount 셀 강제 readonly 는 미적용(확인 필요 #7, 보수적).
- SetPower/SetStrobe/SetOnOff 등 블로킹 시리얼 I/O 는 `Task.Run` 으로 백그라운드 처리(UI 비차단).

## 7. 남은 항목 (실 장비 단계)
- **실 장비 hex dump 1회** — `LH/LC/LS` 송신 + 응답 1프레임 캡처해 펌웨어와 최종 대조 (확인 필요 #2/#5). 매뉴얼 부재로 코드 기준 채택했으므로 실측 확정 권장.
- 응답 echo **정밀 일치 비교**(`R{ch}{값}`)는 hex dump 확정 후 강화 (현재는 `R` prefix + non-`ERR` 까지 판정).
- 실행 검증은 **보조 모니터(2번)** 에서.
