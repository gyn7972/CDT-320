# 알람 코드 코드 발급 적용 로그 (작업 B)

- **작업 날짜**: 2026-04-30 (야간 무인 작업, R4~R6)
- **빌드**: warning 0, error 0
- **수정 원칙**: `Console.WriteLine` 은 유지 (디버그용), `AlarmManager.Raise` 호출만 추가

## AlarmMaster 신규 등록 코드 (15개)

> `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs` `CreateDefaults()` 메서드.

### InputStage (IS-*) 6개
| 코드 | Severity | Category | Title |
|---|---|---|---|
| IS-FEEDER | Warning | Material | 입력 피더 안전 위치 미확인 |
| IS-EXPZ | Error | Motion | ExpanderZ 알람 |
| IS-BARCODE | Warning | Material | 바코드 읽기 실패 |
| IS-MAP | Warning | Material | 웨이퍼 맵 파싱 실패 |
| IS-ALIGN | Warning | Vision | 비전 얼라인 실패 |
| IS-MOVE | Error | Motion | InputStage 이동 실패 |

### OutputStage (OS-*) 5개
| 코드 | Severity | Category | Title |
|---|---|---|---|
| OS-AVOID | Error | Safety | OutputStage 회피 실패 |
| OS-WORKZ | Error | Motion | OutputStage WorkZ 이동 실패 |
| OS-MOVEY | Error | Motion | OutputStage Y 이동 실패 |
| OS-PLACEDONE | Warning | Material | TPU Place 완료 대기 타임아웃 |
| OS-BINCAM | Error | Motion | BinCamera X 이동 실패 |

### OutputUnloader (OUT-*) 4개
| 코드 | Severity | Category | Title |
|---|---|---|---|
| OUT-FULL-GOOD | Error | Material | Good 카세트 가득 |
| OUT-FULL-NG | Error | Material | NG 카세트 가득 |
| OUT-STORE | Error | Material | 웨이퍼 저장 실패 |
| OUT-STORE-EX | Error | System | 웨이퍼 저장 예외 |

## InputStageUnit.cs (13곳 Raise 추가)

| # | 라인 | 메서드 | 코드 | Severity | 트리거 |
|---|---|---|---|---|---|
| 1 | 330 | LoadAndPrepareWaferAsync | IS-FEEDER | Warning | `!Loader.IsFeederAtSafePosition` |
| 2 | 345 | LoadAndPrepareWaferAsync | IS-EXPZ | Error | ExpanderZ.IsAlarm (Down 후) |
| 3 | 360 | LoadAndPrepareWaferAsync | IS-BARCODE | Warning | waferId null/empty |
| 4 | 377 | LoadAndPrepareWaferAsync | IS-MAP | Warning | ParseMapAsync == null |
| 5 | 445 | VisionAlignAndSetupOriginAsync | IS-ALIGN | Warning | 중앙 alignResult == null |
| 6 | 465 | 〃 | IS-ALIGN | Warning | StageT.IsAlarm (Theta 보정) |
| 7 | 490 | 〃 | IS-ALIGN | Warning | ref1Result == null |
| 8 | 509 | 〃 | IS-ALIGN | Warning | ref2Result == null |
| 9 | 701 | MultiScanAndPickupAsync | IS-MOVE | Error | Phase A: StageY/CameraX.IsAlarm |
| 10 | 765 | 〃 | IS-MOVE | Error | Phase B: StageY/NeedleBlockX.IsAlarm |
| 11 | 828 | UnloadWaferAsync | IS-EXPZ | Error | ExpanderZ.IsAlarm (Up 후) |
| 12 | 922 | ExecutePickupAsync | IS-MOVE | Error | NeedleZ.IsAlarm (상승/Eject) |
| 13 | 950 | 〃 | IS-MOVE | Error | NeedleZ.IsAlarm (하강) |

추가: `using QMC.CDT320.Alarms;` (라인 8)

## OutputStageUnit.cs (6곳 Raise 추가)

| # | 라인 | 메서드 | 코드 | Severity |
|---|---|---|---|---|
| 1 | 344~348 | StageModule.MoveToWorkPositionAsync | OS-WORKZ | Error |
| 2 | 369~374 | StageModule.MoveYAsync | OS-MOVEY | Error |
| 3 | 540~545 | OutputStageUnit.EnsureOppositeStageAvoidedAsync | OS-AVOID | Error |
| 4 | 651~656 | OutputStageUnit.InspectBinPositionAsync (WaitPlace 타임아웃) | OS-PLACEDONE | Warning |
| 5 | 671~676 | 〃 (BinCameraX 진입) | OS-BINCAM | Error |
| 6 | 697~702 | 〃 (BinCameraX 후퇴) | OS-BINCAM | Error |

추가: `using QMC.CDT320.Alarms;` (라인 7)

## OutputUnloaderUnit.cs (변경 없음 — 아키텍처 분석 결정)

이유:
1. **AlarmMaster 등록 OUT-* 4개 (FULL-GOOD/FULL-NG/STORE/STORE-EX) 모두 `MachineController.cs:365/378/391/420` 에서 이미 발급 중**
2. `OutputUnloaderUnit` 의 hardware-detail 실패 (FeederY/ElevatorZ IsAlarm, Protrusion, Clamp 등) 와 매칭되는 등록 코드 없음
3. **Peer file `InputLoaderUnit.cs` 도 동일 패턴** — Unit 레이어는 알람 미발급, MachineController 가 boolean/exception 변환
4. 추가 시 **이중 발급** 위험 (OUT-STORE 가 MC 에서 false-return 시 발급되는데 Unit 안에서도 발급하면 중복)

따라서 `OutputUnloaderUnit.cs`: 0 변경 (Console.WriteLine 53건 유지).

## 빌드 결과

```
QMC.Common -> bin\Debug\QMC.Common.dll
QMC.CDT-320 -> bin\Debug\QMC.CDT-320.exe
warning 0, error 0
```

## 회귀 테스트 (R6 진행)

- 빌드: ✓ PASS
- auto-cycle 정상 종료: (다음 단계에서 확인)
- 알람 발생 0 (정상 사이클): (다음 단계에서 확인)

## 종합

- **Raise 호출 추가**: 19곳 (Input 13 + Output 6)
- **AlarmMaster 신규 등록**: 15코드
- **변경 파일**: 3개 (`AlarmMaster.cs`, `InputStageUnit.cs`, `OutputStageUnit.cs`)
- **빌드 성공**: ✓
