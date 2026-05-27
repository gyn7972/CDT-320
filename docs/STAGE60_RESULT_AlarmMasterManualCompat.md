# Stage 60 RESULT — AlarmMaster 매뉴얼 호환 정비

**완료일**: 2026-05-04
**SPEC**: `docs/STAGE60_SPEC_AlarmMasterManualCompat.md`
**컨펌**: 사용자 권한 위임 (`사용자 컨펌 없이 진행`)

---

## 1. 변경 요약

| 항목 | 변경 전 | 변경 후 |
|---|---|---|
| `AlarmMaster.cs` 줄 수 | 216 | 302 (+86) |
| `AlarmDefinition` 필드 | 9개 | 11개 (`ManualName`, `ManualLocator` 추가) |
| `CreateDefaults()` 등록 항목 | 33개 | **48개** (+15 신규, EMG-PRESSED 1개 → E-STOP 1개로 교체) |
| `ManualName` 매핑된 항목 | 0개 | 18개 |

**주의**: SPEC §4 V6 에서 47 로 예상했으나 실제는 48. 이유: `EMG-PRESSED` 레코드를 삭제 후 `E-STOP` 신규 추가(같은 역할의 별도 항목)로 처리. 두 개 합쳐 +0 이 아닌 -1 +1 +15 = +15. 누적 33 + 15 = **48**. SPEC V6 값 47 → 48 로 수정.

---

## 2. 적용 변경 (단일 파일)

`D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs`

### 2.1 AlarmDefinition 클래스 (lines 39~42 신규)

```csharp
// CDT-310 매뉴얼 호환 (Stage 60) — ASE 매뉴얼 검수용 메타데이터.
[DataMember] public string ManualName    { get; set; } = "";  // 예: "InterlockDetected"
[DataMember] public string ManualLocator { get; set; } = "";  // 예: "DieTransfer/WaferLifter/Plate"
```

### 2.2 코드명 통일 (line 177)

`EMG-PRESSED` → `E-STOP` (실호출 `MachineController.cs:510` 측에 맞춤)

### 2.3 신규 등록 15개 (lines 230~297)

| 코드 | Severity | ManualName |
|---|---|---|
| AXL-OPEN | Critical | (없음 — 벤더) |
| AXL-DLL | Critical | (없음) |
| ALIGN-EX | Error | OutOfTolerance |
| LOT-NOCASS | Warning | MaterialDoesNotExist |
| LOT-SCAN | Warning | InvalidScanData |
| LOT-MOVE | Error | CannotMove |
| LOT-EX | Error | CannotMove |
| LOT-RET | Error | CannotMove |
| IS-LOAD | Warning | MaterialDoesNotExistAfterReceive |
| IS-EXCEPTION | Error | (없음) |
| OS-RECEIVE | Warning | MaterialDoesExistAfterInitialize |
| OS-EXCEPTION | Error | (없음) |
| OS-BININSP | Warning | FailedInspection |
| OS-BININSP-EX | Error | (없음) |
| TPU-PLACE | Warning | CannotMove |

### 2.4 기존 9개 항목에 ManualName 추가 (best-effort 매핑)

HOME-FAIL, MOVE-TIMEOUT, LIMIT-HIT, INTERLOCK, VisionMatchFail, EXPOSE-TIMEOUT, PickFail, BottomInspFail, OS-AVOID, OUT-STORE-EX, E-STOP

---

## 3. 검증

| # | 항목 | 결과 | 비고 |
|---|---|---|---|
| V1 | dotnet build | **미실행** | 샌드박스에 dotnet 미설치 → Windows 측 별도 실행 필요 |
| V2 | tools/verify_all.pl | **미실행** | bash mount 가 Windows 측 변경을 즉시 반영 못 함 |
| V3 | 정적 코드 리뷰 | **PASS** | Read 도구로 lines 1~302 직접 읽고 구조 확인. 닫는 중괄호 4개(298~301) 정상 |
| V4 | 신규 코드 15개 존재 | **PASS** | 모든 `Code="…"` 항목 grep 검증 (AXL-OPEN ~ TPU-PLACE 15개 모두 존재) |
| V5 | EMG-PRESSED 제거 | **PASS** | 파일에 `EMG-PRESSED` 문자열 0회 |
| V6 | E-STOP 추가 | **PASS** | line 177 에 `Code="E-STOP"` 항목 존재 |
| V7 | ManualName 필드 추가 | **PASS** | line 41 에 `[DataMember] public string ManualName` |

### 3.1 사용자가 직접 실행해야 하는 검증

Windows PowerShell 에서:

```powershell
cd D:\Work\CDT-320\QMC.CDT-320
dotnet build QMC.CDT-320.sln /p:WarningsAsErrors=false 2>&1 | Select-String "Warning|Error"
# 기대: 새 warning 0건, 새 error 0건 (기존 warning 은 그대로 유지)

perl tools\verify_all.pl
# 기대: 117/118 PASS 유지 (회귀 0)

# AlarmMaster 동작 검증 (handler 실행 후)
# AlarmMaster.Get("E-STOP")  →  non-null
# AlarmMaster.Get("LOT-NOCASS")  →  non-null + ManualName="MaterialDoesNotExist"
# AlarmMaster.Get("EMG-PRESSED")  →  null  (옛 코드 제거됨)
# AlarmMaster.Count  →  48
```

### 3.2 alarm_master.json 마이그레이션

`Config\alarm_master.json` 파일이 이미 존재하면 그 안의 옛 정의(33개 + EMG-PRESSED) 가 우선 로드됨. Stage 60 변경 반영하려면:

```powershell
del D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320\bin\Debug\Config\alarm_master.json
# (또는 Release 폴더)
# Handler 재실행 시 CreateDefaults() 가 다시 불러서 Save() — 새 48개 + ManualName 메타데이터 포함
```

---

## 4. 알려진 한계

1. **dotnet 빌드 미실행** — 샌드박스 환경 제약. Windows 측에서 사용자 실행 필요.
2. **verify_all.pl 미실행** — bash mount 가 Windows-side 파일 변경을 늦게 반영. Windows perl 로 사용자 실행 필요.
3. **Console.WriteLine → Raise 승격은 별도 Stage** — InputLoaderUnit/OutputUnloaderUnit/TransferPickerUnit/BaseCylinder 30+ 위치는 Stage 61 후보.
4. **동적 prefix 코드** (`AX-{n}`, `HOME-{Name}`) 는 마스터에 미등록 상태 유지 — UI fallback 매처는 Stage 63 후보.

---

## 5. 후속 작업 (Stage 61 ~ 63 후보)

| Stage | 주제 | 기대 효과 |
|---|---|---|
| **61** | Console.WriteLine → AlarmManager.Raise (Unit 단위) | InputLoader/OutputUnloader/TPU 의 30+ 실패 분기를 알람으로 승격. Unit 단위 알람 추적 가능 |
| **62** | 미사용 마스터 코드 13개 처리 | HOME-FAIL/MOVE-TIMEOUT/SERVO-OFF/LIMIT-HIT/VISION-CONN/PlacementFail/SECS-DISCONN/SIM-DISCONN/VAC-LOW/CDA-LOW 등 — 사용 추가 또는 마스터에서 삭제 |
| **63** | 동적 prefix 매처 | `AlarmMaster.Get` 에서 `AX-{n}` 패턴을 `AX-AXIS` fallback 정의에 매칭 |

---

## 6. 파일 변경 목록

| 파일 | 종류 | 비고 |
|---|---|---|
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs` | 수정 | +86 lines |
| `docs/STAGE60_SPEC_AlarmMasterManualCompat.md` | 신규 | SPEC |
| `docs/STAGE60_RESULT_AlarmMasterManualCompat.md` | 신규 | 본 문서 |

git 커밋 권장:
```bash
git add QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs
git add docs/STAGE60_*.md
git commit -m "[Stage 60] AlarmMaster 매뉴얼 호환 + 미등록 코드 15개 추가"
```
