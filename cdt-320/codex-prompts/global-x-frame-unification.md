# 작업: X 레일 전역 좌표계 통일 — InputVisionX 홈 원점 기준, 홈 후 전역 좌표 선언 방식

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 대상 축 (물리 X 레일 공유 4축, 레일상 배치 순서):
  `InputVisionX`(최좌측 −쪽) → `FrontPickerX`/`RearPickerX` → (설비 중앙 고정 바텀비전) → `OutputVisionX`(최우측 +쪽)
- 주 수정 파일:
  - `QMC.Common\Motion\AxisData.cs`, `QMC.Common\Motion\BaseAxis.cs`
  - `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs`, `QMC.CDT-320\Equipment\Ajin\AjinFactory.cs`
  - `QMC.CDT-320\Equipment\Motion\SharedRailX\` 전체
  - `QMC.CDT-320\Equipment\Interlocks\PickerFrontInterlockRules.cs` / `PickerRearInterlockRules.cs`
  - `QMC.CDT-320\Equipment\Interlocks\PickerZoneInterlockRules.cs`
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` / `PickerPlaceSequence.cs`
  - `QMC.CDT-320\Equipment\Calibration\PickerVisionOffsetCalibrationService.cs`
  - 티칭/설정 마이그레이션 도구(신규)
- 참고(수정 금지): `QMC.Common\AjinE\AXM.cs`, `QMC.Common\Ajin\AXM.cs` —
  `AXM.SetActualPosition`(AjinE 1870행)/`SetCommandPosition`(1883행) 래퍼는 이미 존재하며 그대로 사용.
- 참고 문서: `QMC.CDT-320\Sequencing\SEQUENCE_SAFETY_ANALYSIS.md` (착수 전 정독)

## 배경 — 현재 구조 (수정 전 반드시 해당 코드를 읽을 것)

### 현재: 축별 개별 좌표계 (자기 홈 = 0)
- 4축 모두 홈 서치 후 자기 원점 = 0. 실장비 X축은 홈 후 소프트웨어 0점 재설정 없이
  보드가 원점을 설정한다 (`AjinAxis.HomeSearchAsync` 908~1031행, `AXM.SetHomeStart` 950행).
- `BlockSetupWriteToBoard = true`(`AjinAxis.cs` 16행) — 소프트웨어가 보드 홈 파라미터를 쓰지 않는다.
  **이번 작업에서도 보드 홈 파라미터는 건드리지 않는다.**
- 축 간 원점 차이는 SharedRailX 페어의 `HomeClearance`(Input↔Picker 19.0 / Output↔Picker 390.0)와
  `TowardSign(±1)`이 환산한다: `clearance = homeClearance − signA·posA − signB·posB`.
  이 공식이 4곳에 중복 구현되어 있다 (아래 R4).
- 바텀비전 코릴레이션은 이미 구현됨: `VisionCameraCalibrationData.Calculate()`(207~224행)가
  `InputToBottomOffsetX/Y`, `OutputToBottomOffsetX/Y`를 산출하고,
  `PickerVisionOffsetCalibrationService`(147~156행)가 콜렛 캘 결과(`FinalPickerX/Y`)와 조합해
  `InputVisionToPicker`/`OutputVisionToPicker` 오프셋을 산출·저장한다.

### 목표: 전역 단일 좌표계
- **전역 원점 = InputVisionX 홈 위치.** 모든 축의 좌표가 같은 자(ruler) 위의 값이 된다.
- **축 이동 방향·홈 서치 방향·부호 규약은 전부 불변.** 바뀌는 것은 "홈 완료 시 좌표값" 하나다:
  - InputVisionX: 홈 후 좌표 = 0 (기준축이므로 그대로)
  - FrontPickerX/RearPickerX/OutputVisionX: 홈 후 좌표 = 각 축 홈의 전역 위치
    (바텀비전 코릴레이션으로 측정한 값, 예: OutputVisionX 홈이 InputVisionX 홈에서 +1200mm면 1200.0)
- 방향 규약 확인 (통일 후에도 동일):
  - InputVisionX 회피 = 피커 헤드보다 −쪽 (간극 = 피커X − 비전X − 기구폭)
  - OutputVisionX 회피 = 피커 헤드보다 +쪽 (간극 = 비전X − 피커X − 기구폭)
  - 방향(부호) 판정 인터락(`IsInputVisionXAvoidOrNegativeDirectionMove`,
    `IsOutputCameraXAvoidOrPositiveDirectionMove`)은 **수정 불필요** — 전역 좌표에서도 참.
- 깨지는 것은 **"0"을 축 원점/안전선으로 쓰는 절대값 비교**와 **홈 환산 계층**뿐이다.

## 구현 항목

### R1. 축 원점 계층 — 전역 홈 좌표 필드 추가 + 홈 후 선언

1. `AxisData.cs`의 `AxisSetup`(284~382행)에 필드 추가:
   ```csharp
   /// <summary>원점 완료 후 좌표를 이 값으로 선언한다(전역 좌표계 홈 위치). [mm]</summary>
   public double GlobalHomePosition { get; set; } = 0.0;
   /// <summary>true면 원점 완료 후 SetPosition(GlobalHomePosition)을 수행한다.</summary>
   public bool UseGlobalHomePosition { get; set; } = false;
   ```
   - 기존 `HomeOffset`(320행)은 의미 변경 없이 그대로 둔다 (보드 홈 오프셋 용도와 분리).
2. `AjinAxis.HomeSearchAsync`(908~1031행): 홈 완료 확정(990~1002행, `IsHomeDone` latch) 직후,
   `Setup.UseGlobalHomePosition == true`이면 **정지 상태 확인 후** `SetPosition(Setup.GlobalHomePosition)`
   호출. 기존 `SetPosition`(1096~1112행)이 mm→펄스 환산 + `AXM.SetCommandPosition`/`SetActualPosition`
   + 시뮬 분기를 이미 처리하므로 그대로 사용한다.
   - Picker Theta 전용 처리(`ApplyPickerThetaPcHomeOffsetAfterHomeAsync`, 1033~1082행)와
     충돌하지 않게 순서를 정리한다 (X축은 Theta 예외 대상 아님 — 1084~1094행 확인).
3. `BaseAxis.HomeSearchAsync`(시뮬, 558~605행): 590행 `SetPosition(Setup.HomeOffset)` 뒤에
   동일 분기 추가 — `UseGlobalHomePosition`이면 `SetPosition(GlobalHomePosition)`.
4. **소프트리밋 동시 전환**: `SetPosition` 선언 직후 `Setup.SoftLimitMinus/Plus`가 전역 좌표
   기준으로 이미 마이그레이션된 값(R8)임을 전제로 한다. **보드 소프트리밋을 조사할 것** —
   보드 위치 레지스터를 `SetActPos/SetCmdPos`로 옮기면 보드에 프로그래밍된 소프트리밋의
   기준도 함께 어긋난다. 보드 소프트리밋이 사용 중이면 같은 시점에 갱신하는 코드
   (AXM 소프트리밋 설정 래퍼 — 없으면 추가 검토)를 넣거나, 사용 여부를 확인해 보고서에 남긴다.
5. `AjinFactory.ApplyPersistedAxisValues`(67~154행) / `MotionAxisStore`(`Config\motion_axes.json`):
   신규 2개 필드의 로드/저장 지원.

### R2. 전역 홈값 산출 — 바텀비전 코릴레이션 연동

전역 홈값은 손입력이 아니라 **기존 코릴레이션 데이터로 산출**한다.

1. 신규 서비스 (예: `GlobalXFrameCalibrationService`, `Equipment\Calibration\` 아래):
   - 입력: `VisionCameraCalibrationData`(`InputToBottomOffsetX`, `OutputToBottomOffsetX`,
     `InputReticle`/`OutputReticle`/`BottomReticle`의 `VisionXPosition`),
     `ColletCalibrationRecord.FinalPickerX` (콜렛 캘 기준 콜렛의 바텀 정렬 위치).
   - 산출: 전역 원점 = InputVisionX 홈. 바텀비전의 전역 X = InputVisionX가 바텀 레티클을
     본 위치 + `InputToBottomOffsetX` 관계식에서 유도. 이로부터
     `FrontPickerX.GlobalHomePosition`, `RearPickerX.GlobalHomePosition`,
     `OutputVisionX.GlobalHomePosition`을 계산한다.
   - **산출식은 `PickerVisionOffsetCalibrationService.cs` 147~156행의 기존 식
     (`FinalPickerX − VisionXPosition − BottomOffset`)을 기준으로 유도**하고,
     유도 과정을 주석으로 남긴다. 부호가 기구 배치(Input −쪽 / Output +쪽)와 일치하는지
     수치 예시로 검증한다.
   - 출력: 각 축 `Setup.GlobalHomePosition`에 기록 + `motion_axes.json` 저장 + 결과 로그.
2. 실행 진입점: 기존 `VisionCameraCalibrationSequence.SaveCalibration`(534~562행)이
   `PickerVisionOffsetCalibrationService.TryApplyAvailableOffsets`를 부르는 자리(546행)에
   전역 홈값 산출을 추가하거나, 별도 버튼/시퀀스로 노출한다 (UI는 최소한으로 —
   기존 `VisionCameraCalibrationDialog`에 결과 표시 정도).

### R3. 좌표 변환 계층 정리 (통일 후 잉여 오프셋)

전역 좌표계에서는 "비전이 본 die의 X = 피커가 가야 할 X"가 콜렛 미세 오프셋만 남기고 일치한다.

1. `DieCoordinateTransformService.cs`:
   - 102행 `PickerX = inputVisionX + inputVisionToPickerX + ...` — `inputVisionToPickerX`가
     전역화 이후 "순수 콜렛/기구 미세 오프셋"으로 의미 축소됨을 반영 (식 구조는 유지 가능,
     의미 주석 갱신 필수).
   - 177행 Place 식의 `outputVisionToPickerX` 동일.
2. `PickerVisionOffsetCalibrationService.cs` 147~156행: 전역 좌표 전제에서 산출식 재검토 —
   통일 후 `InputVisionToPicker.OffsetX`는 0 근방의 미세값이 되어야 정상. 산출식을 전역
   좌표 기준으로 다시 유도하고, 큰 값(수십 mm 이상)이 나오면 코릴레이션 불일치 경고 로그.
3. `PickerFrontUnit.cs` 149~150행 하드코딩 기본값 `OffsetX[0]=250.0, OffsetY[0]=30.0`:
   전역 좌표 전제와 충돌 — IsAllZero 폴백을 "미보정 알람"으로 바꾸거나 값을 재정의한다.
4. `NeedleXToVisionXOffset` 등 Needle 계열(`NeedleCalibrationData` 92~93행)은 **수정 금지** —
   NeedleBlockX는 X 레일 4축이 아니라 InputStage의 별도 축이다.
5. `InputStageUnit.ConvertNeedleXToVisionX`(641~644행, 현재 항등): 전역화 이후에도 항등이
   맞는지 검토만 하고, 변경이 필요하면 사유와 함께 보고 (임의 수정 금지).

### R4. SharedRailX 환산 계층 단순화

전역 좌표에서 페어 간극 = `|posA − posB| − 기구폭(고정)`. `HomeClearance`에서 "원점 차" 성분이
빠지고 순수 기구 최소 간극만 남는다. `TowardSign`은 전역 배치(레일상 순서)로 대체 가능.

1. 공식 구현 4곳을 **하나의 공용 계산기로 통일**한다 (기준: 이미 단일 좌표계 방식인
   `RealtimeCollisionSupervisor`의 `Math.Abs` 계산 + 고정 기구폭):
   - `SharedRailXCollisionValidator.cs` 123~131행 (`CalculatePairClearance`)
   - `SharedRailXMotionService.cs` 845~853행 (+ 802, 277, 897~908, 966행 소비처)
   - `CollisionDistanceCalculator.cs` 67~93행 (미배선 로깅 헬퍼 — 같이 정리)
   - `AjinAxis.FollowMoveAsync` 438~455행 — `homeGap` 인자는 시그니처 유지하되
     전역 좌표에서는 **기구폭 상수**로 의미가 바뀜을 문서 주석으로 명시 (AjinAxis 로직 수정 금지,
     호출측 인자 의미만 재정의).
2. `SharedRailXConfig.cs` 115~155행: `HomeClearance` → 순수 기구 최소 간극으로 의미 변경
   (필드명 유지 + XML 주석 갱신, 또는 `MechanicalClearance` 신설 후 마이그레이션 — 택1, 사유 보고).
   `AxisATowardSign/AxisBTowardSign`은 전역 배치 순서로 자동 결정 가능하면 제거,
   호환 부담이 크면 유지하되 검증 로직 추가 (통일 좌표 기준으로 부호 자동 산출과 일치 확인).
3. `SharedRailXConfigStore.cs` 181~184행 기본값(19.0/390.0) 및 `Normalize`(296~327행) 재정의 —
   전역 좌표 기준 신규 기본값 산출 근거를 주석으로.
4. `SharedRailXSetupDialog.cs` — "Home Gap" 열 라벨/의미 갱신.
5. `Config\shared_rail_x.json` 기존 파일 마이그레이션 (R8 도구에 포함).

### R5. 인터락 — "0 기준 절대값 비교"만 교체 (방향 판정은 불변)

**수정 대상** (0 = 자기 원점 = 안전선 가정):
1. `PickerFrontInterlockRules.cs`:
   - 742~762행 `VerifyInputVisionXAtAvoidOrBelowZero` — `actual <= tolerance`(0 기준)를
     **전역 진입 한계값**(신규 설정, 아래 R6) 비교로 교체.
   - 830~850행 `VerifyOutputVisionXAtAvoidOrBelowZero` — 동일 (Output은 `actual >= 한계값` 방향).
   - 868~885행 `CanHomeFrontPickerX` — "InputVisionX가 홈(0) 위치" 조건을
     "전역 홈값(`GlobalHomePosition`) 또는 미호밍" 조건으로.
2. `PickerRearInterlockRules.cs` 382~402행 / 470~490행 — Front와 동일 교체.
3. `PickerZoneInterlockRules.cs`:
   - 3031~3058행 존 분류, 3330~3336행 `IsPickerTargetBelowAvoidPosition`,
     2807~2821행 `IsManualPickerXOutputSide` — 티칭값 기반이므로 티칭 마이그레이션(R8) 후
     로직 자체는 유지. 단 **X 오름차순 배치 가정(Input < Avoid < Bottom < Side < Output)이
     전역 좌표에서도 유지됨을 검증**하고 주석 갱신.
   - 321~330행, 2327~2339행 `Math.Abs(frontX − rearX)` facing 계산 — 전역 좌표에서
     비로소 정확해짐. 수정 불필요, 검증 항목으로만.
4. `RealtimeCollisionSupervisor.cs` 212~262행 — 이미 단일 좌표계 방식. 수정 불필요, 검증만.

**수정 금지 (방향 판정 — 전역 좌표에서도 그대로 참):**
- `InputStageInterlockRules.IsInputVisionXAvoidOrNegativeDirectionMove`(759~785행)
- `OutputStageInterlockRules.IsOutputCameraXAvoidOrPositiveDirectionMove`(632~660행)
- Avoid 티칭값 비교 계열(`IsVisionXInAvoidPosition` 등) — 티칭 마이그레이션이 흡수.

### R6. 시퀀스 진입 한계 — 하드코딩 0 제거

1. `PickerPickUpSequence.cs`:
   - 2929행 `stage.CameraX.ActualPosition > 0.0` (entryLimit=0),
   - 1055행 `TryResolveNearestVisionRetreatTarget(..., -0.1, ...)`
   → 하드코딩 0/−0.1을 **"InputVisionX 피커 진입 한계(전역 좌표)" 설정값**으로 교체.
   설정 저장처는 InputStage 레시피 또는 SharedRailX 설정 중 기존 관례에 맞는 쪽 (택1, 사유 보고).
2. `PickerPlaceSequence.cs` 1223행 — Output 쪽 동일 (`+` 방향 한계값).
3. `SharedRailXMotionService.TryResolveNearestVisionRetreatTarget`(88행~)의
   `maximumPickerEntryPosition` 인자 의미를 전역 좌표 기준으로 문서 주석 갱신.

### R7. 존/티칭 소비 코드 검증 (수정 최소화)

- `CalibrationCoordinateService.cs` 235~243행(존 타깃 조립), 438~452행(PitchOffsetX 역순) —
  티칭 마이그레이션 후 동작 검증. 로직 수정은 검증 실패 시에만.
- `ColletCalibrationSequence.ApplyToTeaching`(2567~2614, 2749~2754행) — 티칭 역기록이
  전역 좌표로 일관되는지 검증.
- `MachineController.cs` 11834행 주석의 좌표 모델(`picker N absX = ArmX − N·PickerPitchX`) —
  전역 좌표 전제로 주석 갱신.

### R8. 데이터 마이그레이션 도구 (일회성, 필수)

신규 콘솔/메뉴 도구 또는 시작 시 1회 마이그레이터:
1. 입력: 각 축 `GlobalHomePosition` (R2 산출값). 축별 시프트량 `Δaxis = GlobalHomePosition − 0`.
2. 대상 (전부 `Δaxis`를 해당 축 값에 가산):
   - `motion_axes.json`: 4축 `SoftLimitMinus/Plus`
   - 전 Stage 레시피의 X 티칭 (`StageAxisPositions` — InputStage `VisionX`,
     OutputStage `OutputCameraX` 계열: Avoid/Process/Reticle/VisionXPosition 등)
   - Picker 존 티칭 (DiePick/DieBottom/DieSide/DiePlace의 X), `PickerZoneXSetup` 인코더 존 경계
   - `shared_rail_x.json` (R4의 재정의된 스키마로 변환)
   - 캘리브레이션 데이터 중 X 좌표 저장 필드 (`NeedleCalibrationData.VisionXPosition`,
     `VisionReticleMeasurement.VisionXPosition`, `ColletCalibrationRecord.FinalPickerX` 등 —
     각 값이 어느 축 좌표계인지 확인 후 해당 축 Δ 적용)
3. **마이그레이션 전 원본 전체 백업**(타임스탬프 폴더) + 변환 리포트 파일 생성
   (항목·이전값·이후값). 이중 실행 방지 마커.
4. 마이그레이션 완료 전에는 `UseGlobalHomePosition`을 켜지 않는다 — 도구가 성공 완료 시에만
   4축 `UseGlobalHomePosition = true`로 설정.

## 제약 사항
- `AXM.cs`(AjinE/Ajin 양쪽), 보드 홈 파라미터(`WriteSetupToBoard`/`BlockSetupWriteToBoard`) 수정 금지.
- 축 이동 방향, 홈 서치 방향(`HomeDirection`), 존 배치 순서를 바꾸는 수정 금지 —
  이 작업은 **원점 재선언 + 환산 계층 제거**로 한정한다.
- 듀얼 모드(구좌표/신좌표 동시 지원) 구현 금지 — 일회성 컷오버 + 백업으로 간다.
  단 `UseGlobalHomePosition=false`(기본값)이면 기존 동작과 100% 동일해야 한다
  (마이그레이션 전 회귀 안전).
- Needle/EjectPin/Expander/Y/Z/T 축 좌표계는 건드리지 않는다.
- 기존 public API 시그니처 변경 금지 (`FollowMoveAsync` 포함). 신규 추가 필드/서비스/도구만.
- 코드 스타일: 각 파일의 기존 관례 (한국어 주석, `_camelCase`, 최신 C# 문법 자제,
  `Fail`/`Log.Write` 관례).

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과 (MSBuild).
2. **회귀**: `UseGlobalHomePosition=false` 상태에서 시뮬레이션 홈 서치 → 4축 좌표 0,
   기존 인터락/시퀀스 동작 불변.
3. **전역 모드 시뮬레이션** (예시값: InputVisionX 홈=0, FrontPickerX 홈=+250,
   RearPickerX 홈=+900, OutputVisionX 홈=+1200 — 실측 전 임의값):
   - 4축 홈 서치 후 좌표가 각 `GlobalHomePosition`과 일치.
   - 페어 간극이 `|posA − posB| − 기구폭`으로 계산되어 기존 19/390 환산 결과와 동치임을
     수치 케이스 3개 이상으로 확인 (통일 전/후 같은 물리 상황 → 같은 간극값).
   - InputVisionX 회피 위치가 피커 진입 한계(전역값)보다 −쪽, OutputVisionX 회피가 +쪽임을
     인터락이 올바르게 허용/차단 (경계 ± 오차 케이스 포함).
   - PickUp/Place 진입 게이트(R6)가 전역 한계값 기준으로 동작.
   - Front/Rear facing `Math.Abs` 간극이 실제 레일 간격과 일치.
   - 존 분류(Input/Process/Output)가 마이그레이션된 티칭으로 기존과 동일 판정.
4. **마이그레이션 도구**: 샘플 설정 파일 세트에 대해 실행 → 백업 생성, 변환 리포트,
   이중 실행 차단 확인. Δ 적용 누락 항목 없음을 리포트와 R8 대상 목록 대조로 확인.
5. 보드 소프트리밋 사용 여부 조사 결과와 처리 방식을 보고서에 포함 (R1-4).
6. 변경 파일 전체 목록 + 항목별(R1~R8) 만족 근거를 최종 보고.
