# CDT-320 Claude 인계 문서

> 이 문서는 2026-07-12 시점의 작업 이력이다. 현재 상시 규칙의 단일 정본은 `AGENTS.md`이며, 경로·브랜치·빌드 상태는 현재 저장소에서 다시 확인한다.

- 작성일: 2026-07-12
- 인계 대상: Claude Code
- 저장소: `D:\Source\CDT-320`
- 브랜치: `master`
- 인계 시점 HEAD: `f595e6034f95c199d2e0c79c30d26effe5337558`
- 인계 시점 원격: `origin/master`와 동일
- 작성 목적: Collet Calibration의 Bottom AF, COC, Side AF와 생산 Side 보정 작업을 안전하게 이어가기 위한 상세 인계

---

## 0. 작업 시작 전 반드시 읽을 것

다음 순서로 문서를 읽는다.

1. 단일 규칙 정본 `AGENTS.md`
2. 이 문서 `HANDOFF_TO_CLAUDE_2026-07-12.md`
3. Material 상세 설계가 필요할 때 `MATERIAL_ARCHITECTURE_PLAN.md`

이 저장소는 Handler, Vision PC, 공용 Motion/IO, 시뮬레이터가 함께 있는 실장비 제어 프로젝트다. 코드가 컴파일된다는 이유만으로 모션 변경을 승인된 것으로 간주하면 안 된다.

---

## 1. 사용자와 작업할 때의 절대 규칙

### 1.1 호칭과 대화 방식

- 사용자는 제이팀 팀장님이다.
- Claude는 제어팀 과장 역할로 응대한다.
- 반드시 존댓말을 사용한다.
- 사용자가 장비 로그를 주면 추측부터 하지 말고 로그 시각, 축 Actual/Command/Target, 인터락 사유, Sequence/Step을 코드와 대조한다.

### 1.2 코드 수정 허용 조건

- 팀장님이 명시적으로 `코드 수정`, `진행`, `적용`, `수정해라`라고 한 경우에만 코드를 수정한다.
- `분석만`, `확인해봐`, `어떻게 할지 말해봐`, `계획부터`라고 하면 코드를 수정하지 않는다.
- 현재 요청과 관계없는 리팩터링, 파일 정리, 산출물 삭제를 하지 않는다.
- 다른 채팅/작업자가 같은 저장소를 동시에 수정할 수 있다. 매 작업 전 `git status --short --branch`를 확인한다.
- 모르는 변경은 사용자 또는 다른 작업자의 변경으로 보고 되돌리지 않는다.

### 1.3 인터락 변경 절대 규칙

인터락은 장비 충돌과 직접 연결된다. 다음 규칙은 예외가 없다.

1. 인터락 변경 필요성을 발견해도 즉시 수정하지 않는다.
2. 먼저 별도의 `인터락 수정 사항 레포트`를 작성한다.
3. 레포트에는 현재 조건, 문제 재현, 변경 대상 Rule, 허용/차단 범위, 충돌 위험, 검증 항목을 적는다.
4. 팀장님에게 명시적 승인을 요청한다.
5. 승인 후에만 인터락 코드를 수정한다.
6. 인터락 조건을 추가/수정하면 해당 조건 바로 위에 한 줄 한글 주석을 남긴다.
7. 인터락을 우회하는 새 Move 함수를 만들지 않는다. 기존 Motion Guard/Rule 경로를 사용한다.

이번 인계 직전 Git 병합에서는 서버의 인터락 변경을 그대로 받은 것이며, Codex가 인터락 내용을 별도로 수정하지 않았다.

---

## 2. Git 상태와 최근 병합 이력

인계 문서 작성 직전 상태:

```text
master == origin/master
HEAD = f595e6034f95c199d2e0c79c30d26effe5337558
```

주요 최근 커밋:

```text
f595e603 오토포커스 구연완료.... 테스트전
e57f7532 Merge latest origin/master Side gate updates
d6abb63b Merge origin/master and resolve Side autofocus conflicts
3750e4f4 Merge origin/master: Side 게이트 + 원격 mm 센터 보정 구현 병합
54f58ba9 싸이드 비전 오토포커스 작업중./김영남
883fc7f8 Bottom 좌표(XYT+W/H) 선응답 + Side 촬영 전 도착 게이트
3d0788e7 스테이징
cc3837b8 1차 업데이트
```

병합 중 실제 충돌 파일은 다음이었다.

- `QMC.CDT-320/Equipment/Calibration/VisionCameraCalibrationTransform.cs`
- `QMC.CDT-320/Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs`
- `QMC.CDT-320/Ui/Dialogs/VisionFocusCalibrationDialog.cs`

최종 push 직전에 원격이 다시 진행되어 `PickerBottomAndSideInspectionSequence.cs`에서 추가 충돌이 한 번 더 발생했다. 해당 충돌은 `targetX` 미정의 참조를 `bottomTarget.X`로 복구하는 방식으로 해결했다.

주의: 원격 커밋에는 `.codex-build`, `_codex_verify/build` 등의 바이너리 산출물이 이미 추적되어 있다. 팀장님 지시 없이 삭제하거나 `.gitignore`를 대규모 변경하지 않는다.

---

## 3. 현재 빌드 상태

### 3.1 QMC.Vision

완전 재빌드 성공:

```text
QMC.Vision /t:Rebuild -> 성공
```

검증은 실행 파일 잠금을 피하기 위해 `%TEMP%` 아래 별도 `OutDir`와 `BaseIntermediateOutputPath`를 사용했다.

### 3.2 QMC.CDT-320 Handler

중요: 일반 증분 빌드는 성공처럼 보였지만, 완전히 분리된 `obj`로 재빌드하면 현재 HEAD는 컴파일 실패한다.

현재 오류는 정확히 2건이다.

```text
QMC.CDT-320/Equipment/Calibration/VisionCameraCalibrationTransform.cs(103,27)
CS0103: 'bottomOffsetX' 이름이 현재 컨텍스트에 없습니다.

QMC.CDT-320/Equipment/Calibration/VisionCameraCalibrationTransform.cs(104,27)
CS0103: 'bottomOffsetY' 이름이 현재 컨텍스트에 없습니다.
```

원인:

- `e57f7532` 시점에는 `OffsetX/Y = 0.0`과 검증 함수가 남아 있었으나, 이후 다른 작업 창에서 반영된 최신 커밋 `f595e603`이 `VisionCameraCalibrationTransform.cs`를 다시 변경했다.
- 최신 커밋은 `BottomCenterOffsetX/Y`를 직접 읽는 구조로 바꾸면서 `bottomOffsetX/Y` 선언은 제거했다.
- 반환 객체의 `OffsetX = bottomOffsetX`, `OffsetY = bottomOffsetY`는 남아 있다.
- 기존 `obj`가 남아 있으면 MSBuild 증분 판단 때문에 이 오류가 가려질 수 있다.

권장 해결 방향은 아래 둘 중 하나이나, 팀장님에게 코드 수정 지시를 받은 뒤 적용한다.

1. Place용 Offset과 Side 중심 Offset을 완전히 분리하는 현재 서버 계약을 유지한다.
   - `OffsetX = 0.0`
   - `OffsetY = 0.0`
   - `BottomCenterOffsetX/Y`만 Side에 사용
2. Place에도 Bottom X/Y를 적용해야 한다는 공정 요구가 다시 확정되면 별도 필드/부호 계약을 정한 뒤 명시적으로 연결한다.

현재 주변 코드와 주석상 1번이 더 일관적이다. 단, 팀장님이 과거 Place 시 X/Y/T 보정을 요구한 적이 있으므로 실제 공정 의도를 재확인해야 한다.

완전 재빌드 예시:

```powershell
$root = Join-Path $env:TEMP ("cdt320-rebuild-" + [Guid]::NewGuid().ToString("N"))
$out = Join-Path $root "out"
$obj = Join-Path $root "obj"
New-Item -ItemType Directory -Force -Path $out,$obj | Out-Null

msbuild .\QMC.CDT-320\QMC.CDT-320.csproj `
  /t:Rebuild `
  /p:Configuration=Debug `
  "/p:OutDir=$out\" `
  "/p:BaseIntermediateOutputPath=$obj\" `
  /v:minimal
```

증분 `Build`만으로 검증 완료라고 보고하지 않는다.

---

## 4. 가장 최근 팀장님 요구사항

최근 핵심 요구는 다음과 같다.

> Collet Calibration에서 Bottom AutoFocus 후 회전 중심(COC)을 구하고, 그 중심까지 적용한 뒤 Side AutoFocus도 수행한다.

의도한 전체 순서:

```text
Bottom 위치 진입
  -> Bottom AutoFocus
  -> Bottom Best Z 실제 이동
  -> Collet T/XY Calibration
  -> Collet 결과 저장
  -> COC START
  -> T 360도 회전
  -> COC END 결과 수신
  -> COC 기계 중심 X/Y 계산 및 Recipe 저장
  -> COC 기계 중심으로 실제 X/Y 이동
  -> Bottom T 기준 복귀
  -> 현재 Picker Die의 Bottom 검사 결과 취득
  -> COC/Die Offset/Die Size 기반 Side 0도 Focus 보정 계산
  -> Side 0도 AutoFocus
  -> Side 90도 Focus 보정 계산
  -> Side 90도 AutoFocus
  -> Bottom T/Z 복귀
  -> Calibration/Focus 결과 저장
```

옵션은 기존 `ColletCalibrationSettings.RunAutoFocusAfterTheta`를 사용한다. 이름은 과거 구조 때문에 `AfterTheta`지만, 실제 Bottom AF는 Collet 검출 전에 실행된다.

---

## 5. Collet Calibration 구현 상세

### 5.1 진입과 Bottom AF

파일:

- `QMC.CDT-320/Sequencing/Calibration/ColletCalibrationSequence.cs`
- `QMC.CDT-320/Sequencing/Calibration/ColletCalibrationStep.cs`

핵심 메소드:

- `MoveColletToBottomViewAsync()`
- `RunAutoFocusIfNeededAsync()`
- `SaveColletCalibration()`
- `RunCocAndSideAutoFocusAsync()`

현재 Bottom 진입 순서:

1. Picker X/Y를 Bottom 기준 위치로 이동한다.
2. Picker T를 `_basePickerT`로 이동한다.
3. 선택 Collet PickerZ를 `_targetPickerZ`로 이동한다.
4. `RunAutoFocusIfNeededAsync()`를 호출한다.
5. Bottom AF Best를 `_targetPickerZ`에 반영한다.
6. Best Z로 실제 이동 완료 후 `FindCollet`로 넘어간다.

Bottom AF 요청은 `VisionFocusCalibrationData.BottomColletScan`의 다음 값을 그대로 전달한다.

- Rough Minus/Plus/Step
- Fine Minus/Plus/Step
- RepeatCount
- MoveVelocity/Acceleration/Deceleration
- SettleDelay
- MotionTimeout
- VisionTimeout
- VisionBestTimeout
- FocusValueReceiveMode

`ReturnToDefaultAfterScan=false`이므로 Best 위치를 유지한다.

### 5.2 Collet 보정 저장 후 COC/Side AF 전환

`SaveColletCalibration()`에서 Collet 결과 저장이 끝나면:

```csharp
CurrentStep = _settings != null && _settings.RunAutoFocusAfterTheta
    ? ColletCalibrationStep.RunCocAndSideAutoFocus
    : ColletCalibrationStep.Complete;
```

`RunCocAndSideAutoFocus`는 이번 작업에서 추가된 Step이다.

### 5.3 COC 하위 시퀀스

파일:

- `QMC.CDT-320/Sequencing/Calibration/ColletRotationCenterCalibrationSequence.cs`

중요 순서:

1. 부모 Collet Cal이 점유한 Calibration Area를 `ReleaseCalibrationArea()`로 먼저 해제한다.
2. 같은 `MachineSequenceContext`, Side, Collet No로 COC 하위 시퀀스를 실행한다.
3. COC는 기존 Collet Calibration의 최종 X/Y/Z 위치에서만 시작한다.
4. Vision PC에 COC START를 보낸다.
5. 선택 PickerT를 360도 회전한다.
6. Vision PC COC END에서 회전 중심 픽셀을 받는다.
7. `machineCenter = actual - residualMm * gain`으로 기계 중심을 계산한다.
8. COC 픽셀 결과는 `ColletCalibrationRecord`에 저장한다.

COC 시작 위치 조건:

- `FinalPickerX/Y/Z`와 현재 Actual이 `0.05 mm` 이내여야 한다.
- 이 조건 때문에 과거 `FinalPickerZ`와 실제 AF Best Z 저장값이 다르면 COC 시작 알람이 발생했다.
- 이 허용값이나 인터락을 임의 완화하지 않는다.

### 5.4 COC 결과 저장 위치

`SaveAndApplyRotationCenter()`에서 다음 Recipe 배열에 콜렛별로 저장한다.

Front:

- `FrontPicker.Recipe.ColletRotationCenterX[index]`
- `FrontPicker.Recipe.ColletRotationCenterY[index]`
- `FrontPicker.Recipe.ColletRotationCenterValid[index]`

Rear:

- `RearPicker.Recipe.ColletRotationCenterX[index]`
- `RearPicker.Recipe.ColletRotationCenterY[index]`
- `RearPicker.Recipe.ColletRotationCenterValid[index]`

픽셀 COC 정보는 `ColletCalibrationRecord`에 별도로 저장된다.

- `RotationCenterPixelX`
- `RotationCenterPixelY`
- `RotationCenterRadiusPixel`
- `RotationCenterSampleCount`
- `RotationCenterValid`
- `RotationCenterUpdatedAt`

즉 Collet XY 보정값과 COC는 같은 값에 덮어쓰지 않는다.

---

## 6. Side AF 계산과 저장

### 6.1 Bottom 검사 결과 계약

타입:

- `BottomVisionOffset`
- 파일: `QMC.CDT-320/Equipment/Unit/Common/PickerTransferTypes.cs`

현재 Side 중심 보정 전용 필드:

- `BottomCenterOffsetX`
- `BottomCenterOffsetY`
- `HasBottomCenterOffset`

Place/Align용 기존 필드:

- `OffsetX`
- `OffsetY`
- `OffsetT`

이 두 계약을 섞지 않는 것이 현재 서버 병합 방향이다.

Vision 응답 후보 키:

```text
bottom_offset_x_mm
bottom_center_offset_x_mm
bottom_center_x_offset_mm
bottom_center_x_mm
center_offset_x_mm
center_x_offset_mm
center_x_mm
bottom_item_offset_x

bottom_offset_y_mm
bottom_center_offset_y_mm
bottom_center_y_offset_mm
bottom_center_y_mm
center_offset_y_mm
center_y_offset_mm
center_y_mm
bottom_item_offset_y
```

### 6.2 Vision PC의 절대 픽셀/실수 mm 호환

파일:

- `QMC.Vision/Equipment/Core/BottomInspector.cs`

일부 Inspector Library 버전은 `BottomResult.Offset.X/Y`에 mm가 아니라 절대 픽셀 중심을 반환했다. 실장비 로그에서 약 `2370`, `3270`처럼 `_mm`로 볼 수 없는 값이 확인됐다.

현재 Vision PC 코드는 절댓값이 `100`을 넘으면 절대 픽셀 중심으로 보고 다음처럼 정규화한다.

```text
OffsetXmm = (ResultCenterX - RoiNominalCenterX) * PixelSizeWidthMm
OffsetYmm = (ResultCenterY - RoiNominalCenterY) * PixelSizeHeightMm
```

Handler에서도 비정상 큰 값을 차단해야 한다. 현재 병합 후 `VisionCameraCalibrationTransform.cs`의 검증 로직이 불완전하므로 빌드 오류 수정과 함께 재검토한다.

### 6.3 Collet Side AF 보정식

파일:

- `QMC.CDT-320/Sequencing/Calibration/ColletCalibrationSequence.cs`
- 메소드: `TryBuildSideFocusCorrection()`

현재 코드의 식:

```text
referenceWidth  = Recipe DieSizeX
referenceHeight = Recipe DieSizeY

측정 Width/Height가 회전되어 들어온 것으로 판단되면 서로 swap

COC 기계 중심으로 실제 X/Y 이동한 뒤 Bottom을 다시 측정하므로
영상 내 COC 중심은 (0,0)으로 취급

rotatedY = BottomCenterOffsetX
size0     = (measuredHeight - referenceHeight) / 2
size90    = (measuredWidth  - referenceWidth)  / 2

Focus0  = BottomCenterOffsetY + size0
Focus90 = rotatedY + size90
```

Side 카메라 축 방향:

- Front Side: `+` 방향이 Focus에 가까워지는 방향
- Rear Side: `-` 방향이 Focus에 가까워지는 방향

따라서 Side AF 시작 위치:

```text
Front defaultY = teachingY + focusCorrection
Rear  defaultY = teachingY - focusCorrection
```

### 6.4 Side AF 모션

`RunSideAutoFocusAsync(angleDeg, focusCorrection, ct)` 순서:

1. 선택 PickerZ를 `SidePosition`으로 이동
2. 선택 PickerT를 `DieSidePosition`의 T0 또는 T0+90도로 이동
3. Front/Rear SideVisionY를 계산된 Default Y 기준으로 Focus Scan
4. Rough 후 Fine이 설정되어 있으면 Fine 수행
5. `FOCUS_BEST` 결과를 해당 Side/각도/Collet 레코드에 저장

Side Focus Scan 파라미터는 `VisionFocusCalibrationData.SideVisionScan`을 사용한다.

### 6.5 콜렛별 Side Focus 저장 구조

파일:

- `QMC.CDT-320/Equipment/Calibration/VisionFocusCalibrationData.cs`

콜렛별 배열:

- `FrontSide0Collets[4]`
- `FrontSide90Collets[4]`
- `RearSide0Collets[4]`
- `RearSide90Collets[4]`

접근 함수:

```csharp
GetSideRecord(VisionFocusScanKind kind, int pickerNo)
```

기존 단일 레코드 `FrontSide0`, `FrontSide90`, `RearSide0`, `RearSide90`는 이전 JSON 호환용으로 남아 있다. 새 배열이 없는 기존 파일을 읽으면 유효한 Legacy 값을 4개 콜렛에 복제한다.

---

## 7. 생산 Bottom/Side 검사에서의 적용 구조

파일:

- `QMC.CDT-320/Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs`

### 7.1 현재 생산 Side 기준 위치

`ResolveSideVisionBasePosition(angleDeg, pickerNo, out valid)`:

1. 해당 Front/Rear + Angle + Picker No의 Side AF 레코드가 유효하면 `BestPosition` 사용
2. 유효하지 않으면 기존 `Process0Position` 또는 `Process90Position` 티칭값 사용

최종 SideVisionY:

```text
Side 0 finalY  = per-collet Side0 AF Best(or Process0 teaching)
                 + SideVisionProcess0YOffset

Side 90 finalY = per-collet Side90 AF Best(or Process90 teaching)
                 + SideVisionProcess90YOffset
```

### 7.2 런타임 Bottom 중심 보정

Bottom 검사 결과를 받은 뒤:

```text
SideVisionProcess0YOffset  = BottomCenterOffsetX
SideVisionProcess90YOffset = BottomCenterOffsetY
```

저장 타입:

- `PickerSideInspectionCorrection`
- Front/Rear Picker Unit이 콜렛별 Runtime 배열로 보관

유효 조건:

- `HasBottomCenterOffset == true`
- X/Y가 finite
- Side 검사 시 현재 Die ID와 SourceDieId 일치
- 절댓값 `2.0 mm` 이하

범위를 넘거나 Die ID가 다르면 Side 검사 전에 Alarm으로 실패한다.

### 7.3 Bottom XYT 선응답 게이트

최신 서버 병합에서 Bottom 외곽 탐색이 끝나면 Vision PC가 `XYT`를 선 Push한다.

관련 파일:

- `QMC.CDT-320/Equipment/Vision/BottomXytStore.cs`
- `QMC.CDT-320/Equipment/Vision/VisionProtocol.cs`
- `QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs`
- `QMC.Vision/Equipment/Core/BottomXytPushService.cs`
- `QMC.CDT-320/Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs`

Side 진입 전 `WaitBottomXytForSideAsync()`가 해당 Front/Rear, Picker No의 XYT 수신을 기다린다. Full Bottom 결과와 별개로 Side 회전/촬영 준비 게이트 역할을 한다.

### 7.4 현재 Bottom/Side 실행 순서

최신 서버 병합본은 다음으로 동작한다.

- Picker 처리 순서: `4 -> 3 -> 2 -> 1`
- Bottom과 Side 모두 동일 Picker 번호의 각 티칭 위치 사용
- Front Y는 양수 공정 방향, Rear Y는 음수 공정 방향 검증
- X는 Picker4에서 Picker1 방향으로 증가해야 함
- 현재 `bottomSideParallel=False`
- 모든 Bottom 검사 완료 후 Side를 순차 실행

이 부분은 과거 팀장님이 확정한 `Bottom 마지막 촬영과 Side 첫 촬영을 같은 위치에서 병렬 시작` 요구와 다르다. 최신 서버 병합이 병렬 동작을 제거했다. 임의 복원하지 말고 팀장님에게 현재 요구를 다시 확인한다.

---

## 8. UI 구조

### 8.1 Collet Calibration Dialog

파일:

- `QMC.CDT-320/Ui/Dialogs/ColletCalibrationDialog.cs`

`AutoFocus=True`의 현재 의미:

- Bottom AF
- Collet 보정
- COC 계산/적용
- Side 0/90 AF

Tooltip에도 이 흐름이 설명되어 있다.

### 8.2 Vision Focus Dialog 분리

파일:

- `QMC.CDT-320/Ui/Dialogs/VisionFocusCalibrationDialog.cs`
- `QMC.CDT-320/Ui/Dialogs/SideVisionFocusCalibrationDialog.cs`
- `QMC.CDT-320/Ui/Pages/Recipe/CalibrationPage.cs`

프로파일:

- `BottomOnly`: Bottom Collet / Bottom Die
- `SideOnly`: Front/Rear Side 0/90

Side 전용 창은 선택 Picker의 다음 기준 좌표를 표시한다.

- Picker X Ref
- Picker Y Ref
- Picker Z Ref
- Picker T Ref

Side 저장 결과 Grid는 현재 선택한 Side/각도에 대해 Picker 1~4 레코드를 표시한다.

숫자 입력은 기존 Calibration Dialog 정책대로 Keypad를 사용하고, ComboBox는 선택 가능해야 한다.

---

## 9. 좌표계와 부호 관련 고정 지식

### 9.1 비전 픽셀 좌표

팀장님이 정의한 영상 기준:

- 화면 왼쪽: 설비 X `+`
- 화면 오른쪽: 설비 X `-`
- 화면 위쪽: 설비 Y `+`
- 화면 아래쪽: 설비 Y `-`

Pixel Scale의 부호를 그대로 사용한다.

- Scale이 음수면 음수로 계산
- Scale이 양수면 양수로 계산
- `abs(scale)`로 방향을 지우지 않는다

현재 `VisionCameraPixelCalibration`의 기본 식:

```text
X = (pixelX - centerX) * PixelToMmX
Y = (centerY - pixelY) * PixelToMmY
```

방향은 설정된 `PixelToMmX/Y` 부호와 함께 결정된다.

### 9.2 Picker 번호

- 사용자 표시와 로그는 무조건 Picker/Collet `1~4`
- 내부 배열 인덱스만 `0~3`
- 로그에 0-base Picker 번호를 노출하지 않는다

### 9.3 미세 이동

- Calibration과 Jog는 1 um 명령도 내려야 한다.
- 단, 완료 확인을 1 um 오차로 강제하면 Actual 흔들림 때문에 InPosition Timeout이 발생한다.
- 이동 명령은 정확한 목표로 내리고, 완료 판정은 축 설정의 InPositionTolerance 안이면 OK로 처리한다.

---

## 10. 합병 후 반드시 확인해야 할 위험 항목

### 우선순위 1: Handler 컴파일 오류

`VisionCameraCalibrationTransform.cs`의 `bottomOffsetX/Y` 미정의 두 건을 먼저 해결해야 한다. 해결 후 반드시 별도 `obj`로 `/t:Rebuild`한다.

### 우선순위 2: Side 0/90 축 매핑 불일치

현재 Collet Side AF 계산:

```text
0도  <- BottomCenterOffsetY
90도 <- 회전된 BottomCenterOffsetX
```

현재 생산 Runtime Side 보정:

```text
0도  <- BottomCenterOffsetX
90도 <- BottomCenterOffsetY
```

둘 중 어느 계약이 실제 장비 물리축에 맞는지 실장비 검증이 필요하다.

검증 방법:

1. Bottom에서 X만 의도적으로 `+` 이동한 샘플 확보
2. Side 0도/90도에서 어느 카메라 Y가 같은 방향으로 이동해야 Focus가 맞는지 기록
3. Bottom에서 Y만 `+` 이동한 샘플로 반복
4. Front/Rear 각각 수행
5. COC 중심 이동 전/후 비교

코드만 보고 임의로 X/Y를 교환하지 않는다.

### 우선순위 3: AF Best와 Runtime Offset 이중 적용 가능성

Collet 자동 Side AF는 현재 Die의 Bottom 중심 보정을 Default Y에 적용한 상태로 Best를 찾고, 그 실제 Best Y를 저장한다.

생산에서는 다시:

```text
saved Best + current Die Bottom center offset
```

을 적용한다. Calibration Die의 보정이 Best에 이미 포함되어 있다면 이중 적용될 수 있다.

가능한 해결 모델:

1. AF 결과 저장 시 Calibration Die 보정량을 빼고 정규화된 Base Best를 저장
2. AF 레코드에 CalibrationReferenceOffset을 함께 저장하고 생산에서는 `current - reference`만 적용
3. 저장 Best를 절대 위치로 사용하고 생산 Runtime Offset 추가를 하지 않음

추천은 2번이지만, 데이터 모델 변경 전 팀장님에게 공정 의도를 확인한다.

### 우선순위 4: Bottom/Side 병렬 동작 제거

현재 원격 병합본은 `bottomSideParallel=False`다. 과거 확정 요구와 다르므로 Auto Cycle Tact Time 및 실제 동작을 팀장님과 확인한다.

### 우선순위 5: Collet Calibration에서 사용하는 Zone Tag

Side AF 중 PickerZ/T 이동이 현재 다음 Target Name을 사용한다.

```text
ColletCalibration;PickerZone=Bottom
```

실제 XY는 Bottom 공정 위치를 유지한 채 Side Z/T를 구동한다는 의도지만, Zone 의미가 적절한지 인터락 레포트 없이 수정하면 안 된다.

### 우선순위 6: Bottom 검사 Overall NG 처리

`RunCocAndSideAutoFocusAsync()`는 Bottom 결과 객체가 존재하고 Width/Height/Center Offset이 유효하면 Overall `IsOk=false`여도 Side AF 계산을 진행할 수 있다. Chipping/Foreign NG와 중심 검출 유효성을 분리하려는 의도다. 이것이 공정 정책과 맞는지 확인한다.

---

## 11. 로그 확인 위치와 핵심 태그

실장비 로그 경로:

```text
D:\CDT-320\Log
```

주요 파일:

- `Event_YYYY-MM-DD.log`
- `Alarm_YYYY-MM-DD.log`
- Calibration 관련 로그 파일

핵심 태그:

```text
ColletCalAutoFocus
ColletCalSave
ColletCalCocCenter
ColletCalSideFocusFormula
ColletCalSideAutoFocus
ColletCOC
AUTO-VISION-BOTTOM-INSPECT-CAL
AUTO-VISION-INSPECTRESULT-RAW
VISION-COMM-RX
PICKER-BOTTOM-SIDE-CENTER-OFFSET-*
PICKER-BOTTOM-SIDE-*
```

Side AF 식 로그에는 최소 다음이 남는다.

- Die Offset X/Y
- COC 이동 전 Residual
- COC 이동 후 중심 `(0,0)` 가정
- Measured Width/Height
- Reference Width/Height
- Focus0/Focus90
- Front/Rear Axis Sign
- Default Y
- Best Y/Score/Sample Count

---

## 12. 파일별 책임 지도

| 파일 | 책임 |
|---|---|
| `Equipment/Calibration/ColletCalibrationData.cs` | Collet Cal 설정/결과, COC 픽셀 결과 |
| `Equipment/Calibration/VisionFocusCalibrationData.cs` | Bottom/Side Focus 설정과 콜렛별 Best 저장 |
| `Equipment/Calibration/VisionCameraCalibrationTransform.cs` | Vision 결과의 좌표/단위 계약 변환 |
| `Equipment/Unit/Common/PickerTransferTypes.cs` | Bottom/Side 통신 결과 DTO |
| `Equipment/Unit/PickerFrontUnit.cs` | Front 런타임 Side 보정 저장 |
| `Equipment/Unit/PickerRearUnit.cs` | Rear 런타임 Side 보정 저장 |
| `Sequencing/Calibration/VisionFocusScanSequence.cs` | Rough/Fine Focus Scan 공통 엔진 |
| `Sequencing/Calibration/ColletCalibrationSequence.cs` | Bottom AF, Collet Cal, COC, Side AF 오케스트레이션 |
| `Sequencing/Calibration/ColletRotationCenterCalibrationSequence.cs` | COC START/회전/END/중심 계산 |
| `Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs` | 생산 Bottom/Side 검사와 런타임 보정 |
| `Equipment/Vision/BottomXytStore.cs` | Bottom XYT 선 Push 저장 |
| `Ui/Dialogs/ColletCalibrationDialog.cs` | Collet Cal 설정/실행 UI |
| `Ui/Dialogs/VisionFocusCalibrationDialog.cs` | 공통 Bottom/Side Focus UI 본체 |
| `Ui/Dialogs/SideVisionFocusCalibrationDialog.cs` | Side 전용 Focus UI 진입점 |
| `QMC.Vision/Equipment/Core/BottomInspector.cs` | Bottom Width/Height/Angle/Offset 생성 및 단위 정규화 |
| `QMC.Vision/Equipment/Core/BottomXytPushService.cs` | Bottom XYT 선응답 Push |

---

## 13. 권장 실장비 검증 순서

코드 수정 후 바로 Auto Cycle 전체를 돌리지 말고 다음 순서로 검증한다.

1. Clean Rebuild
   - Handler
   - Vision
2. Simulation/DryRun
   - Front C4 Collet Cal
   - Rear C4 Collet Cal
   - Side 0/90 AF 레코드 저장 확인
3. 실장비 Manual
   - 모든 축 Servo/Alarm/Home 상태 확인
   - Bottom 위치에서 COC 시작 조건 X/Y/Z 확인
   - COC 360도 회전 방향/속도 확인
   - COC 중심으로 X/Y 실제 이동 확인
4. Bottom Center Offset 단위 확인
   - 값이 수 mm 이내인지 확인
   - 2000~6000 수준이면 절대 픽셀이 잘못 유입된 것
5. Front Side 0도
   - `+` 방향이 Focus 접근 방향인지 확인
6. Front Side 90도
7. Rear Side 0도
   - `-` 방향이 Focus 접근 방향인지 확인
8. Rear Side 90도
9. 콜렛 4부터 검증 후 3,2,1
10. 생산 Side 검사
    - AF Best fallback 여부
    - Current Die와 SourceDieId 일치
    - Runtime Offset 2 mm 제한
11. Stop/Alarm/Resume
    - Main Stop 즉시 취소되는지
    - Alarm 후 Motion이 재개되지 않는지

장비 이동 중 이상이 보이면 인터락을 풀지 말고 로그와 Actual/Target을 먼저 확인한다.

---

## 14. 다음 Claude 작업 권장 순서

팀장님이 코드 수정을 승인하면 다음 순서가 안전하다.

1. `VisionCameraCalibrationTransform.cs` 컴파일 오류 2건 수정
2. Handler/Vision Clean Rebuild
3. Side 0/90 X/Y 매핑을 로그와 실장비로 검증
4. AF Best + Runtime Offset 이중 적용 여부 검증
5. Bottom/Side 병렬 실행 정책을 팀장님에게 재확인
6. 필요한 변경안을 먼저 설명
7. 인터락 변경이 필요하면 별도 레포트와 승인
8. 승인된 범위만 수정
9. Clean Rebuild 및 로그 기반 리뷰

팀장님께 첫 응답 예시:

```text
팀장님, 인계 문서와 현재 HEAD를 확인했습니다.
먼저 Handler Clean Rebuild에서 VisionCameraCalibrationTransform.cs의
bottomOffsetX/Y 미정의 오류 2건이 재현됩니다.
현재 서버 계약대로 Place Offset은 0으로 유지하고 BottomCenterOffsetX/Y만
Side 보정에 사용하도록 수정하는 방향이 가장 일관적입니다.
다만 코드 수정 전 팀장님 승인부터 받겠습니다.
```

---

## 15. 마지막 체크리스트

- [ ] 단일 규칙 정본 `AGENTS.md`를 읽었는가
- [ ] `git status --short --branch`를 확인했는가
- [ ] 팀장님이 코드 수정을 명시적으로 지시했는가
- [ ] 인터락 변경 여부를 확인했는가
- [ ] Side 0/90 X/Y 계약을 추측하지 않았는가
- [ ] Picker 번호를 사용자에게 1~4로 표시했는가
- [ ] Signed Scale에 `abs`를 적용하지 않았는가
- [ ] Motion Command와 Wait/Final Check를 분리했는가
- [ ] `AGENTS.md`에 따라 운영 폴더와 분리된 임시 복제본에서 Rebuild했는가
- [ ] Handler와 Vision을 모두 검증했는가
- [ ] 실장비 로그의 값과 단위를 확인했는가

이 문서의 핵심은 세 가지다.

1. 인터락은 승인 없이 수정하지 않는다.
2. 현재 Handler는 Clean Rebuild 기준 컴파일 오류 2건이 남아 있다.
3. Side 0/90 축 매핑과 AF Best/Runtime Offset 중복 여부는 실장비 검증 전 확정하지 않는다.
