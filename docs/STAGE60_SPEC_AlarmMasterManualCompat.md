# Stage 60 SPEC — AlarmMaster 매뉴얼 호환 정비

**작성일**: 2026-05-04
**선행 산출물**: `docs/ALARM_AUDIT.md`, `docs/INDEX.md`
**참조 매뉴얼**: `D:\Work\CDT-320\2026.01.23_CDT310.docx` (CDT-310 H/W 사양, 텍스트 추출 `manual_310_2026.txt`)
**컨펌**: 사용자 권한 위임 (`사용자 컨펌 없이 진행`, 2026-05-04)

---

## 1. 배경

CDT-310 매뉴얼은 100여 종의 `CamelCase` 알람명(예: `InterlockDetected`, `ProtrusionDetected`, `MaterialDoesNotExist`)과 발생지(Locator) 를 정의한다.
CDT-320 의 `AlarmMaster.CreateDefaults()` 는 단축 코드 33개(예: `IS-MAP`, `OS-AVOID`)만 등록되어 있고 매뉴얼의 표준 명명과 1:1 매핑이 없다.

`docs/ALARM_AUDIT.md` 에서 다음 3종의 갭이 식별됨:

1. **미등록 호출 코드 15개** — 코드에서 `AlarmManager.Raise(...)` 로 호출되지만 마스터에 정의 없음 → UI 가 Title/Cause/Action 표시 못함.
2. **코드명 불일치** — 마스터엔 `EMG-PRESSED`, 실호출은 `E-STOP` (`MachineController.cs:510`).
3. **매뉴얼 호환 부재** — CDT-310 매뉴얼의 알람명을 알 수 있는 메타데이터 필드 없음 → ASE 측 매뉴얼 검수 시 추적 곤란.

본 Stage 는 위 3종 중 즉시 영향이 큰 (1)+(2) 를 처리하고, (3) 의 인프라(메타데이터 필드)만 추가한다. P2 항목(Console.WriteLine→Raise 승격)은 별도 Stage 로 분리.

---

## 2. 범위 (Scope)

### 2.1 In-Scope
1. `AlarmDefinition` 에 `ManualName` 필드 추가 (CDT-310 매뉴얼 알람명, 비어 있으면 호환 정보 없음).
2. `AlarmMaster.CreateDefaults()` 에 미등록 코드 15개 신규 등록 + 가능한 항목은 `ManualName` 매핑.
3. `EMG-PRESSED` 항목을 `E-STOP` 으로 코드명 통일 (실호출 측은 변경 없음 — 마스터를 호출 측에 맞춤).
4. 신규 등록 코드의 ko/en Title/Cause/Action 작성.

### 2.2 Out-of-Scope (별도 Stage)
- `Console.WriteLine` → `AlarmManager.Raise` 승격 (InputLoaderUnit/OutputUnloaderUnit/TransferPickerUnit/BaseCylinder 30+ 위치) — Stage 61 후보
- 사용되지 않는 마스터 코드 13개 삭제 또는 호출 추가 — Stage 62 후보
- 동적 `AX-{n}`, `HOME-{Name}` prefix 의 마스터 fallback 매처 — Stage 63 후보

### 2.3 비-목표 (Non-Goals)
- 매뉴얼의 100여 알람을 모두 등록하지 않는다. CDT-320 코드 흐름에서 실제 발생할 수 있는 것만.
- 알람 SECS S5F1 메시지 포맷 변경 (별도 검토 필요).

---

## 3. 변경 사양

### 3.1 `AlarmDefinition` 필드 추가 (`AlarmMaster.cs:26~49`)

```csharp
[DataContract]
public class AlarmDefinition
{
    [DataMember] public string Code { get; set; } = "";
    [DataMember] public AlarmCategory Category { get; set; } = AlarmCategory.System;
    [DataMember] public AlarmSeverity DefaultSeverity { get; set; } = AlarmSeverity.Warning;
    [DataMember] public string Title { get; set; } = "";
    [DataMember] public string Cause { get; set; } = "";
    [DataMember] public string Action { get; set; } = "";
    [DataMember] public string TitleEn { get; set; } = "";
    [DataMember] public string CauseEn { get; set; } = "";
    [DataMember] public string ActionEn { get; set; } = "";
    // ── NEW (Stage 60) ──
    [DataMember] public string ManualName { get; set; } = "";  // CDT-310 매뉴얼 알람명 (CamelCase)
    [DataMember] public string ManualLocator { get; set; } = ""; // CDT-310 매뉴얼 발생지 (e.g. "DieTransfer/WaferLifter")
    // 기존 메서드 유지...
}
```

### 3.2 `EMG-PRESSED` → `E-STOP` 통일 (`AlarmMaster.cs:164~166`)

기존:
```csharp
new AlarmDefinition { Code="EMG-PRESSED", Category=AlarmCategory.Safety, DefaultSeverity=AlarmSeverity.Critical,
    Title="비상 정지", ... }
```

변경:
```csharp
new AlarmDefinition { Code="E-STOP", Category=AlarmCategory.Safety, DefaultSeverity=AlarmSeverity.Critical,
    Title="비상 정지", Cause="E-Stop 버튼 또는 Door 센서", Action="안전 확인 후 비상 정지 해제 + Reset",
    TitleEn="Emergency Stop", CauseEn="E-Stop button or door sensor", ActionEn="Confirm safety, release E-Stop and Reset",
    ManualName="InterlockDetected", ManualLocator="모든 Interlock" }
```

이유: 실제 호출은 `MachineController.cs:510` 의 `"E-STOP"`. 마스터를 호출 측에 맞춤.
호환성: 기존 alarm_master.json 에 `EMG-PRESSED` 항목이 있을 수 있으므로 `Load()` 시 자동으로 새 기본값으로 덮어쓰기 위해 `Save()` 강제는 하지 않음 — 운영자가 alarm_master.json 을 삭제 후 재생성하거나 UI 에서 두 코드 모두 등록된 채 운영(부작용 없음).

### 3.3 신규 등록 15개 코드

| 코드 | Severity | Category | Title (ko) | TitleEn | ManualName |
|---|---|---|---|---|---|
| AXL-OPEN | Critical | System | AXL 오픈 실패 | AXL Open failed | (없음 — 벤더 라이브러리) |
| AXL-DLL | Critical | System | AXL DLL 로드 실패 | AXL DLL load failed | (없음) |
| ALIGN-EX | Error | Vision | 얼라인 예외 | Align exception | OutOfTolerance |
| LOT-NOCASS | Warning | Material | 카세트 미감지 | Cassette not detected | MaterialDoesNotExist |
| LOT-SCAN | Warning | Motion | 카세트 스캔 실패 | Cassette scan failed | InvalidScanData |
| LOT-MOVE | Error | Motion | 카세트 슬롯 이동 실패 | Cassette slot move failed | CannotMove |
| LOT-EX | Error | Material | 카세트 교환 위치 이동 실패 | Cassette exchange move failed | CannotMove |
| LOT-RET | Error | Motion | 피더 후퇴 실패 | Feeder retract failed | CannotMove |
| IS-LOAD | Warning | Material | InputStage 로드 실패 | InputStage load failed | MaterialDoesNotExistAfterReceive |
| IS-EXCEPTION | Error | System | InputStage 사이클 예외 | InputStage cycle exception | (없음) |
| OS-RECEIVE | Warning | Material | OutputStage 수신 실패 | OutputStage receive failed | MaterialDoesExistAfterInitialize |
| OS-EXCEPTION | Error | System | OutputStage 사이클 예외 | OutputStage cycle exception | (없음) |
| OS-BININSP | Warning | Vision | Bin 검사 실패 | Bin inspection failed | FailedInspection |
| OS-BININSP-EX | Error | System | Bin 검사 예외 | Bin inspection exception | (없음) |
| TPU-PLACE | Warning | Motion | TPU Place 실패 | TPU Place failed | CannotMove |

### 3.4 영향 받는 기존 33개 코드 — `ManualName` 후속 매핑 (best-effort)

다음 8개 항목만 매뉴얼 호환 매핑 추가, 나머지는 빈 문자열 유지:

| 기존 코드 | ManualName | ManualLocator |
|---|---|---|
| HOME-FAIL | FailedReferencePositionFind | DieTransfer/WaferTransfer/Stage |
| MOVE-TIMEOUT | PositionMismatchedInMotionDone | 모든 Axis |
| LIMIT-HIT | PositivePositionExceed | 모든 Axis |
| INTERLOCK | InterlockDetected | 모든 Interlock |
| VisionMatchFail | OutOfTolerance | 모든 Vision Service |
| EXPOSE-TIMEOUT | ResultTimeOut | 모든 Vision Service |
| PickFail | MaterialDoesNotExistAfterReceive | DieTransfer/PickAndPlaceDieTransfer/Tool/PickerN |
| BottomInspFail | FailedInspection | DieTransfer/PickAndPlaceDieTransfer/BottomInspectionVision |
| OS-AVOID | InterlockDetected | DieTransfer/BinTransfer/DiePost/UpDownCylinder/*Interlock |
| OUT-STORE-EX | ProtrusionDetected | DieTransfer/BinLifter/*Plate |

---

## 4. 검증 게이트

| # | 항목 | 기대 |
|---|---|---|
| V1 | `dotnet build` | warning 0 / error 0 |
| V2 | `tools/verify_all.pl` | 117/118 PASS 유지 (회귀 0) |
| V3 | `AlarmMaster.Get("E-STOP")` | non-null, ManualName="InterlockDetected" |
| V4 | `AlarmMaster.Get("LOT-NOCASS")` | non-null |
| V5 | `AlarmMaster.Get("EMG-PRESSED")` | null (옛 코드 제거 검증) |
| V6 | `AlarmMaster.Count` | 33 - 1 (EMG-PRESSED 제거) + 15 (신규) = **47** |

---

## 5. 롤백 계획

문제 발생 시 다음 단일 파일만 revert:
- `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs`

Config 폴더의 `alarm_master.json` 은 자동 재생성됨 (`Load()` 가 빈 디렉토리 시 `CreateDefaults()` 호출).

---

## 6. 후속 Stage 후보

- **Stage 61** — Console.WriteLine → AlarmManager.Raise 일괄 승격 (Unit 단위)
- **Stage 62** — 미사용 마스터 코드 13개 처리 (사용 추가 또는 삭제)
- **Stage 63** — 동적 prefix 매처 (`AX-`, `HOME-`) UI fallback
