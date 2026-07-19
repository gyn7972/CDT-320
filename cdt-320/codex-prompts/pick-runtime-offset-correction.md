# 작업: Pick 런타임 오프셋 실시간 보정 — Bottom 검사 기반 LowPassFilter 폐루프 구현

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정/신규 파일:
  - 신규: `PickRuntimeOffsetService`(+Store) — `QMC.CDT-320` 프로젝트 내
  - 재사용: `LowPassFilter` 클래스 — Place 런타임 보정 작업(`place-runtime-offset-correction.md`)으로
    이미 존재하면 그대로 재사용, 없으면 이 작업에서 생성 (단일 채널 1차 EMA)
  - 수정: `QMC.CDT-320\Sequencing\Picker\DieCoordinateTransformService.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerMotionTargetResolver.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerBottomInspectionSequence.cs`
  - 수정: `QMC.CDT-320\Sequencing\Picker\PickerBottomAndSideInspectionSequence.cs`
- 신규 .cs 파일은 프로젝트 `.csproj`(구식 Compile Include 형식이면)에 반드시 등록.

## 배경 — 기존 구조 (수정 전 반드시 해당 파일들을 읽을 것)

### Pick 목표 좌표 계산 (단일 경로)
`PickerPickUpSequence.CalculateCurrentPickTarget()` (약 1160행)
→ `PickerMotionTargetResolver.TryCalculateInputPickTarget()` → `CalculateInputPickTarget()` (약 73행)
→ `DieCoordinateTransformService.CalculatePickTarget()` (약 68행)

핵심 수식 (`DieCoordinateTransformService.cs` 96~105행):
```
StageY  = inputStageY + needleYToVisionYOffset − alignOffsetY
PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX
PickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT
NeedleX = inputVisionX + alignOffsetX − needleXToVisionXOffset
PickerY = inputVisionToPickerY + needleYToVisionYOffset + pickerAlignOffsetY (고정 Pick Y — 이번 보정 미적용)
```
- `CalculateInputPickTarget`의 다른 호출처: `PickerPickUpZCalibrationSequence`,
  `Ui\Pages\Recipe\RecipePickerMoveTarget.cs`, `InputPickerPickTargetResolver.cs`(37행 경유) —
  **이들은 이번 보정의 적용 대상이 아니다** (아래 R4).

### Bottom 검사 결과 도착 지점 (갱신 소스, 두 시퀀스 모두 연결할 것)
1. `PickerBottomInspectionSequence.cs`:
   - 촬영 요청: `RequestBottomInspectionAsync()` (약 638행)
   - 결과 반영: `ApplyBottomInspectionResult()` (약 727행) — `_bottomResult.OffsetX/OffsetY/OffsetT` 사용
2. `PickerBottomAndSideInspectionSequence.cs`:
   - 결과 반영: `ApplyBottomInspectionResult(InspectionTarget target, BottomVisionOffset result)` (약 1564행,
     호출부 약 3098행) — `result.OffsetX/OffsetY/OffsetT` 사용
- 두 경로 모두 검사 Pass/NG 판정 값이 존재한다 (`inspectionResult` / `MaterialInspectionResult`). 정확한
  Pass 판정 변수는 코드를 읽고 확인할 것.

### 콜렛 캘리브레이션 Y 위치 (Y 전처리용 기준값)
- `QMC.CDT-320\Equipment\Calibration\ColletCalibrationData.cs`의 `ColletCalibrationRecord` —
  후보 필드: `FinalPickerY` (콜렛 캘 완료 시 피커 Y 최종 위치), `CenterMmY`.
- **작업 시작 시 `ColletCalibrationSequence.cs`를 읽고, 콜렛이 Bottom 비전 중심에 정렬된 시점의
  PickerY 위치가 어느 필드에 저장되는지 확정한 뒤 그 필드를 사용할 것** (주석으로 근거 남길 것).
- 접근: `CalibrationCoordinateService.ResolveCollet(machine, side, pickerIndex)` 또는
  `machine.VisionUnit.Config.CalibrationData.Collet.GetRecord(side, colletNo)`.

## 기능 사양

Pickup → Bottom 비전 검사(콜렛에 물린 Die의 틀어짐 측정) → Y 전처리 → LowPassFilter →
다음 Pick 목표에 반영하는 **폐루프 보정**. Place 런타임 보정과 동일한 구조에 아래 차이가 있다:
Enable/Disable 설정, Y 전처리, 발산 방지 클램프+워닝, 적용 수식이 Pick 계열이라는 점.

### 사용 유무 (Enable/Disable)
- 설정값 `UsePickRuntimeOffset` (bool, 기본 false)을 스토어에 포함한다.
- **Disable이어도 필터 갱신(학습)·저장은 계속** 수행한다. **적용만 중지** (적용 지점에서 0 전달).

### 필터 구성
- Front/Rear × 피커 1~4 = **8세트**, 각 세트는 X/Y/T 3채널 `LowPassFilter`.
- 이산 1차 EMA: `filtered += alpha * (input − filtered)`,
  `alpha = (2π·fc)/(2π·fc + 1)`, fc 설정값(기본 0.1), **검사 1회 = 1샘플 고정**.
- 필터 상태는 전처리 후 측정 부호 그대로(raw) 저장. 부호 반전은 적용 지점에서.

### Y 오프셋 전처리 (필터 입력 전, Y 채널만)
```
피커Y편차       = 촬영명령시점_PickerY_CommandPosition − 콜렛Cal_Y위치
전처리 YOffset  = 촬영된 OffsetY − 피커Y편차
```
- **PickerY축의 CommandPosition** 값을 사용한다 (ActualPosition 아님).
- **Bottom 촬영 명령을 내리는 시점**에 캡처해 검사 요청 컨텍스트에 보관했다가, 결과 도착 시
  그 캡처값으로 전처리한다 (두 시퀀스 모두: 단독 Bottom의 `RequestBottomInspectionAsync`,
  BottomAndSide의 bottom shot 트리거 지점).
- 콜렛Cal 레코드가 없거나 `Valid == false`이면 해당 샘플은 폐기하고 로그를 남긴다.
- 검증 케이스 (콜렛Cal Y=30.0, 촬영 시 Command Y=29.7 → 편차 = −0.3):
  - Die가 콜렛 중심에 정확히 물림 → 카메라 측정 −0.3 → 전처리 = (−0.3) − (−0.3) = 0 (학습 없음)
  - 카메라 측정 0 → 전처리 = 0 − (−0.3) = +0.3 (Die가 콜렛 기준 +0.3 틀어짐만 학습)
- X/T 채널은 전처리 없이 측정값 그대로 입력.

### 필터 갱신 조건 / 이상치 거부 (채널별 독립)
- **Bottom 검사 Pass인 경우에만** 갱신 (NG 측정은 폐기).
- 이상치: 현재 필터 출력 대비 `|입력 − filtered| >= 1.0mm`(X/Y), `>= 0.5°`(T)이면
  해당 채널 샘플 폐기 (필터 상태 유지) + 로그.

### 발산 방지 클램프 + 워닝
- 필터 갱신 후 상태값 자체를 한계로 클램프한다 (적분 발산 방지):
  - X/Y: **±0.50mm**
  - T: **±0.5°**
- 클램프가 발생하면(한계 도달) **Warning을 발생**시킨다:
  `AlarmManager.Raise(AlarmSeverity.Warning, ...)` + `EventLogger` 기록 (side, pickerNo, 채널,
  클램프 전 값, 한계값 포함).
- 워닝 스팸 방지: (side, pickerNo, 채널)별로 한계 상태에 진입할 때 1회만 발생시키고,
  값이 한계 미만으로 복귀하면 다시 발생 가능하도록 래치를 관리한다.

### 영속화
- 기존 설정 스토어 패턴(JSON, 기존 저장 디렉터리, try/catch 관례)으로 `PickRuntimeOffsetStore` 구현.
- 내용: `UsePickRuntimeOffset`, fc, 8세트 × filteredX/Y/T, 마지막 갱신 시각.
- 시작(또는 서비스 최초 접근) 시 로드, 갱신 수락 시마다 저장. 저장 실패는 로그만.

### 적용 수식 (Enable일 때만, 필터 출력을 − 부호로 신규 항 추가)
```
PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX − pickRuntimeOffsetX
NeedleX = inputVisionX + alignOffsetX − needleXToVisionXOffset − pickRuntimeOffsetX
StageY  = inputStageY + needleYToVisionYOffset − alignOffsetY − pickRuntimeOffsetY
PickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT − pickRuntimeOffsetT
```
- X는 PickerX와 NeedleX **양쪽에 동일 값** 적용 (피커·니들 정렬 유지).
- PickerY(고정 Pick Y)와 PickerZ에는 적용하지 않는다.

## 구현 항목

### R1. `PickRuntimeOffsetService` (신규, static) + `PickRuntimeOffsetStore` (신규)
- Place 보정의 `PlaceRuntimeOffsetService` 패턴을 따르되 Pick 사양(Enable 플래그, Y 전처리,
  클램프+워닝)을 반영. `lock` 스레드 안전, lazy 로드.
- API:
  - `bool IsEnabled` (설정 조회)
  - `GetOffset(PickerSequenceSide side, int pickerNo, out double x, out double y, out double t)` —
    필터 상태(클램프 반영된 값) 반환. **Enable 여부와 무관하게 상태를 반환**하고,
    Enable 판정은 적용 지점에서 한다 (또는 `GetAppliedOffset`으로 분리 — 구현 선택, 주석 명확히).
  - `OnBottomInspectionOffset(PickerSequenceSide side, int pickerNo, double offsetX, double preprocessedOffsetY, double offsetT, string dieId)` —
    이상치 검사 → 채널별 갱신 → 클램프+워닝 → 저장 → 로그.
    (Y 전처리는 호출 측(시퀀스)에서 수행해 전달할지, 서비스에 원시값+캡처Y+콜렛CalY를 넘겨
    서비스가 전처리할지 — **서비스가 전처리까지 담당하는 형태를 권장**:
    `OnBottomInspectionOffset(side, pickerNo, offsetX, rawOffsetY, offsetT, capturedPickerYCommand, colletCalY, dieId)`)
  - `Reset(side, pickerNo)` / `ResetAll()`.

### R2. 촬영 시점 PickerY CommandPosition 캡처
- `PickerBottomInspectionSequence.RequestBottomInspectionAsync()`: 검사 요청 직전에
  PickerY축 `CommandPosition`을 로컬/컨텍스트에 캡처.
- `PickerBottomAndSideInspectionSequence`: bottom shot을 트리거하는 지점(코드를 읽고 확정)에서
  동일하게 캡처해 `InspectionTarget`(또는 대응 컨텍스트)에 보관.
- PickerY축 접근은 시퀀스의 기존 축 접근 헬퍼(`GetPickerAxis(PickerAxis.PickerY)` 등)를 사용.

### R3. 결과 반영 지점에서 필터 갱신 연결
- `PickerBottomInspectionSequence.ApplyBottomInspectionResult()` (약 727행)과
  `PickerBottomAndSideInspectionSequence.ApplyBottomInspectionResult()` (약 1564행)에서
  Pass 판정 확인 후 `PickRuntimeOffsetService.OnBottomInspectionOffset(...)` 호출.
- 기존 측정 기록/Material 업데이트 로직은 변경하지 않는다.
- 콜렛Cal Y는 이때 `ColletCalibrationRecord`에서 조회 (확정한 필드 사용).

### R4. 좌표 계산 경로에 보정 반영
- `DieCoordinateTransformService.CalculatePickTarget`에 **선택적 인자 3개 추가** (기본값 0):
  `pickRuntimeOffsetX = 0.0, pickRuntimeOffsetY = 0.0, pickRuntimeOffsetT = 0.0`
  - 수식 반영: `PickerX`·`NeedleX`에 `− pickRuntimeOffsetX`, `StageY`에 `− pickRuntimeOffsetY`,
    `PickerT`에 `− pickRuntimeOffsetT`.
  - `Formula` 문자열에 세 항 추가 (F() 6자리 관례).
- `PickerMotionTargetResolver.CalculateInputPickTarget` / `TryCalculateInputPickTarget`에도
  선택적 인자로 통과시키고 `WriteCoordinateLog`에 값 로깅 추가.
- **적용은 `PickerPickUpSequence.CalculateCurrentPickTarget()`에서만**:
  `IsEnabled`이면 `GetOffset(Side, _currentPickerNo, ...)` 조회값을, 아니면 0을 전달.
  기존 `WriteLog("calculated pick target", ...)`에 세 값과 Enable 상태를 추가 기록.
- 다른 호출처(`PickerPickUpZCalibrationSequence`, `RecipePickerMoveTarget`,
  `InputPickerPickTargetResolver` 경유 호출)는 기본값 0 → 동작 불변.

## 제약 사항
- 기존 public/internal API 시그니처는 **선택적 인자 추가만** 허용. 기존 호출처는 재컴파일만으로 동작 불변.
- Place 런타임 보정 작업과 파일이 겹치므로(`DieCoordinateTransformService`, `PickerMotionTargetResolver`),
  해당 작업이 이미 적용돼 있으면 그 위에 추가하고, 없으면 독립적으로 적용 가능해야 한다
  (서로 다른 인자/서비스이므로 충돌 없음).
- 코드 스타일: 한국어 XML doc/설명 주석, F() 6자리 로그, `Log.Write`/`EventLogger`/`AlarmManager` 관례,
  try/catch/finally 관례, 최신 C# 문법 자제.
- 스레드 안전: 검사 시퀀스 스레드(갱신)와 픽업 시퀀스 스레드(조회)가 다르므로 서비스 내부 lock 필수.
- 시뮬레이션/DryRun에서 Bottom 검사가 생략/모의되는 경로는 그대로 둔다.

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과 (MSBuild). 신규 파일 csproj 등록 확인.
2. Y 전처리 단위 검증 (하네스/테스트로 결과 보고):
   - 콜렛Cal Y=30.0, Command Y=29.7, 측정 −0.3 → 필터 입력 0
   - 콜렛Cal Y=30.0, Command Y=29.7, 측정 0 → 필터 입력 +0.3
3. 클램프+워닝: 필터 상태가 +0.48에서 큰 입력 반복 → 상태가 0.50에서 멈추고 Warning 1회 발생,
   이후 반복 입력에도 워닝 중복 발생 없음, 값이 0.5 미만으로 내려간 뒤 다시 도달하면 재발생.
4. Enable/Disable: Disable 상태에서 검사 결과 유입 → 필터 상태는 갱신·저장되지만
   `CalculatePickTarget` 결과는 보정 미적용(기본값 0)과 동일. Enable 전환 후 즉시 적용됨.
5. 부호 검증: 필터 상태 X=+0.1, Y=+0.2, T=+0.05일 때 미적용 대비
   PickerX/NeedleX −0.1, StageY −0.2, PickerT −0.05 차이.
6. 이상치/Pass 조건: NG 검사 결과·1mm/0.5° 초과 입력이 필터를 변경하지 않음 (채널별 독립 확인).
7. 영속화: 갱신 → 저장 → 재로드 후 상태 유지.
8. 비적용 경로(`PickerPickUpZCalibrationSequence`, `RecipePickerMoveTarget`) 결과 불변.
