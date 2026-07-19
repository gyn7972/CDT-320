# 작업: Place 런타임 오프셋 실시간 보정 — LowPassFilter 기반 폐루프 보정 구현

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정/신규 파일:
  - 신규: LowPassFilter 유틸, PlaceRuntimeOffsetService(+Store) — `QMC.CDT-320` 프로젝트 내
  - 수정: `QMC.CDT-320\Sequencing\Picker\DieCoordinateTransformService.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerMotionTargetResolver.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs`
  - 수정: `QMC.CDT-320\Sequencing\OutputStage\OutputPostPlaceInspectionQueue.cs`
- 신규 .cs 파일은 프로젝트 `.csproj`가 구식(Compile Include 명시) 형식이면 반드시 등록할 것.

## 배경 — 기존 구조 (수정 전 반드시 해당 파일들을 읽을 것)

### Place 목표 좌표 계산 (단일 경로)
`PickerPlaceSequence.CalculatePlaceTargetValues()` (약 1478행)
→ `PickerMotionTargetResolver.CalculateOutputPlaceTarget()` (약 244행)
→ `DieCoordinateTransformService.CalculatePlaceTarget()` (약 135행)

최종 수식 (`DieCoordinateTransformService.cs` 158~170행):
```
PickerX      = outputVisionProcessX + receiveTargetX + outputVisionToPickerX + runtimeOffsetX − bottomOffsetX
OutputStageY = outputStageBaseY + receiveTargetY − bottomOffsetY − pickerColletOffsetY
PickerY      = pickerYTeaching (고정)
PickerT      = pickerTTeaching − bottomOffsetT
PickerZ      = pickerZTeaching (+ placeZOverDrive 는 시퀀스에서 가산)
```
- `bottomOffsetX/Y/T` 인자는 현재 `CalculatePlaceTargetValues()`에서 `VisionOffset {0,0,0}` 하드코딩으로
  0이 전달된다 (주석: "Bottom MRESULT OffsetY는 Side Vision Y 전용" — **이 의미는 유지하고 건드리지 말 것**).
- `CalculateOutputPlaceTarget`의 다른 호출처: `OutputStageMapTransferPage`, `OutputPlaceTargetSelectDialog`,
  `PickerPlaceZCalibrationSequence` — **이들은 이번 보정의 적용 대상이 아니다** (아래 R4 참조).

### Place 후 Bin 비전 검사 결과 도착 지점
`OutputPostPlaceInspectionQueue.ApplyPlacedDieResult(request)` (약 1056행, static):
- `request.InspectionResult` (`InspectionResultDto`): `OffsetX`, `OffsetY`, `OffsetT`, `HasOffset`, `IsPass`
- `request.PickerNo` (1~4), `request.PickerSide` (`PickerSequenceSide.Front/Rear`), `request.HasPickerContext`
- 이 메서드에서 `VisionOffset`을 만들어 `MaterialStateService.UpdateOutputStageDieInspection()`으로 반영 중.

### 설정 영속화 패턴
기존 스토어 클래스(`AppSettingsStore`, `QMC.CDT-320\Equipment\Motion\SharedRailX\SharedRailXConfigStore.cs`,
`Equipment\Ajin\IoSettingsStore.cs` 등)의 JSON 파일 저장/로드 패턴을 그대로 따를 것
(저장 경로·직렬화 방식·예외 처리 관례 포함).

## 기능 사양

Place → Bin 비전 후검사 → LowPassFilter → 다음 Place 목표 반영으로 도는 **폐루프 보정**.

### 필터 구성
- 피커 Z축 단위 = **PickerSide(Front/Rear) × PickerNo(1~4) = 8개** 필터 상태.
- 각 상태는 X, Y, T **3채널 독립** 저역통과 값 보유.
- 이산 1차 저역통과(EMA):
  ```
  filtered = filtered + alpha * (input − filtered)
  ```
- 샘플링은 **검사 1회 = 1샘플 고정** (시간 기반 dt 없음).
  alpha는 Cutoff Frequency 설정값 fc(단위: cycles/sample, 0 < fc)에서 유도:
  `alpha = (2π·fc) / (2π·fc + 1)`, 결과를 (0,1]로 클램프. 기본 fc = 0.1.
- 필터 상태는 **비전 측정 부호 그대로(raw)** 저장한다. 부호 변환은 적용 지점에서 수행 (아래 부호 규약).

### 부호 규약 (적용 시)
비전 옵셋의 + 의미는 설비 정면 기준 "더 멀리/더 회전" = 과이동. 목표에는 반대로 반영하되
Y만 스테이지 이동 방향 정의상 +로 반영한다:

| 채널 | 필터 출력(raw) → 목표 반영 |
|---|---|
| X | `PickerX      += (−filteredX)` |
| Y | `OutputStageY += (+filteredY)` |
| T | `PickerT      += (−filteredT)` |

### 이상치 거부 (필터 입력 전, 채널별 독립)
- X, Y: `|측정값 − 현재 필터 출력| >= 1.0mm` 이면 해당 채널 샘플 폐기 (필터 상태 유지)
- T: `|측정값 − 현재 필터 출력| >= 0.5°` 이면 해당 채널 샘플 폐기
- 폐기 시 로그를 남긴다 (측정값, 현재 필터값, 한계값, die/picker 식별 포함).

### 영속화
- 설비 시작(또는 서비스 최초 접근) 시 마지막 저장값을 로드해 필터 초기 상태로 복원.
- 필터 갱신이 수락될 때마다 저장 (8세트 × X/Y/T + fc 설정).
- 저장 파일에는 fc(cutoff), 각 (side, pickerNo)별 filteredX/Y/T, 마지막 갱신 시각을 포함.

## 구현 항목

### R1. `LowPassFilter` 클래스 (신규)
- 단일 채널 1차 저역통과. 생성자에서 alpha(또는 fc) 지정, `Reset(initialValue)`, `Update(input) → double`,
  `Value` 프로퍼티. 상태만 갖는 단순 클래스 (스레드 안전은 서비스 계층에서 담당).

### R2. `PlaceRuntimeOffsetService` (신규, static)
- 내부에 (PickerSide, pickerNo) → {X,Y,T LowPassFilter} 8세트 관리. `lock` 기반 스레드 안전
  (검사 큐 스레드에서 갱신, 시퀀스 스레드에서 조회).
- API:
  - `GetOffset(PickerSequenceSide side, int pickerNo, out double x, out double y, out double t)` —
    현재 필터 출력(raw) 반환. 미초기화면 0.
  - `OnInspectionOffset(PickerSequenceSide side, int pickerNo, double measuredX, double measuredY, double measuredT, string dieId)` —
    이상치 검사(채널별) → 통과 채널만 필터 갱신 → 저장 → 로그.
  - `Reset(PickerSequenceSide side, int pickerNo)` / `ResetAll()` — 필터·저장값 초기화 (UI 연동은 범위 외).
- 최초 접근 시 lazy 로드 (기동 코드 수정 최소화).

### R3. `PlaceRuntimeOffsetStore` (신규)
- 기존 설정 스토어 패턴(JSON 파일, 동일 저장 디렉터리, try/catch 관례)을 따라
  `Load()`/`Save()` 구현. 파일이 없으면 기본값(fc=0.1, 전부 0).

### R4. 좌표 계산 경로에 보정 반영
- `DieCoordinateTransformService.CalculatePlaceTarget`에 **선택적 인자 3개 추가** (기본값 0):
  `placeRuntimeOffsetX = 0.0, placeRuntimeOffsetY = 0.0, placeRuntimeOffsetT = 0.0`
  - 수식 반영: `PickerX`에 `− placeRuntimeOffsetX`, `OutputStageY`에 `+ placeRuntimeOffsetY`,
    `PickerT`에 `− placeRuntimeOffsetT`.
  - `Formula` 문자열에 세 항을 값과 함께 추가 (기존 F() 포맷 관례).
  - 기존 `bottomOffsetX/Y/T` 인자와 의미·부호를 혼동하지 말 것 (별도 항).
- `PickerMotionTargetResolver.CalculateOutputPlaceTarget`에도 동일하게 선택적 인자 3개를 추가해
  통과시키고, `WriteCoordinateLog` 두 곳(OutputPlaceTarget / OutputPlaceFormula)에 값 로깅 추가.
- **적용은 `PickerPlaceSequence.CalculatePlaceTargetValues()`에서만**:
  `PlaceRuntimeOffsetService.GetOffset(Side, _currentPickerNo, ...)`로 조회해 인자로 전달하고,
  기존 `WriteLog("calculated place target", ...)`에 세 값을 추가 기록.
  다른 호출처(`OutputStageMapTransferPage`, `OutputPlaceTargetSelectDialog`,
  `PickerPlaceZCalibrationSequence`)는 기본값 0 그대로 → 동작 불변.

### R5. 검사 결과 → 필터 갱신 연결
- `OutputPostPlaceInspectionQueue.ApplyPlacedDieResult()`에서 기존 `VisionOffset` 생성 직후:
  - 조건: `inspection != null && inspection.HasOffset && request.HasPickerContext && !request.SkipInspection`
    (IsPass 여부와 무관하게 오프셋이 유효하면 갱신할지 → **IsPass == true 인 경우에만 갱신**한다.
    NG 판정 Die의 위치 측정은 신뢰할 수 없으므로 제외.)
  - `PlaceRuntimeOffsetService.OnInspectionOffset(request.PickerSide, request.PickerNo, offset.X, offset.Y, offset.R, request.DieId)` 호출.
  - 기존 Material 업데이트/로그 동작은 변경하지 않는다.

## 제약 사항
- 기존 public/internal API 시그니처는 **선택적 인자 추가만** 허용 — 기존 호출처가 재컴파일만으로
  동작 불변이어야 한다.
- `bottomOffsetX/Y/T`의 기존 의미("Side Vision Y 전용 예약, Place 미적용")와 하드코딩 0은 그대로 둔다.
- 코드 스타일: 기존 파일 관례 — 한국어 XML doc/설명 주석, `F()` 6자리 포맷 로그, `Log.Write`/`EventLogger`
  로깅, try/catch/finally 관례, 최신 C# 문법 자제.
- 스레드 안전: 서비스의 조회/갱신/저장은 lock으로 보호. 저장 실패는 동작을 막지 않고 로그만 남긴다.
- 시뮬레이션/DryRun 모드에서 검사가 bypass되는 경로는 그대로 두면 된다 (갱신이 안 일어날 뿐).

## 검증 / 수용 기준
1. 솔루션 전체가 빌드 에러 없이 컴파일된다 (MSBuild). 신규 파일이 csproj에 등록되어 있다.
2. 필터 단위 검증 (간단 하네스 또는 단위 테스트로 결과 보고):
   - fc=0.1일 때 alpha 계산값 확인, 동일 입력 반복 시 목표값으로 수렴하는지 확인.
   - 이상치: 필터값 0 상태에서 X=1.2mm 입력 → 폐기(필터 0 유지), X=0.5mm 입력 → 수락.
     T=0.6° → 폐기, T=0.3° → 수락. 채널별 독립 동작 확인.
3. 부호 검증: 필터 상태 X=+0.1, Y=+0.2, T=+0.05일 때 `CalculatePlaceTarget` 결과가
   보정 미적용 대비 PickerX −0.1, OutputStageY +0.2, PickerT −0.05 만큼 차이나는지 확인.
4. 영속화: 갱신 → 저장 → 서비스 재초기화(재로드) 후 GetOffset 값이 유지되는지 확인.
5. 비적용 경로: `PickerPlaceZCalibrationSequence` 등 다른 호출처의 계산 결과가 이전과 동일한지
   (기본값 0) 확인.
6. `ApplyPlacedDieResult`에서 IsPass=false 또는 HasOffset=false 또는 HasPickerContext=false인 경우
   필터가 갱신되지 않는지 확인.
