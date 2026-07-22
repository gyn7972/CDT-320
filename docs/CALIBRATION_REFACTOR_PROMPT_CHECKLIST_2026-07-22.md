# Calibration 개선 작업 프롬프트 & 체크리스트 (2026-07-22)

이 문서는 Calibration(콜렛/자동 캘 중심) 개선 작업의 **구현 지시서(프롬프트)와 검증 체크리스트**다.
분석은 완료된 상태이며, 아래 file:line 참조는 2026-07-22 기준 `D:\Source\CDT-320_New` 코드에서 확인한 값이다.
구현 시점에 라인 번호가 밀렸을 수 있으므로 심볼명 기준으로 재확인 후 수정한다.

- 작업 규칙: `AGENTS.md` 전체 준수. 특히 §2(요청 범위), §13(별도 OutDir 빌드만), 커밋은 사용자 명시 요청 시에만.
- 대상 저장소: `D:\Source\CDT-320_New` (다른 복사본 코드 사용/복붙 금지)
- 이 문서의 확정 요구사항은 사용자와 합의 완료된 내용이다. 임의 변경하지 않는다.

---

## 0. 확정 요구사항 요약

1. **[항목①] 캘 START 시 안전위치 이동 반복 제거**
   - 이미 안전위치(Avoid)에 있으면 이동을 다시 발행하지 않고 **확인만 하고 통과**한다. 아닐 때만 이동한다.
   - Auto Calibration(공용 안전위치 시퀀스)과 자식 캘 시퀀스의 **이중 안전이동**도 제거한다.

2. **[항목②] Collet Calibration 다중 선택 일괄 실행**
   - 다이얼로그에서 **Front 1~4 / Rear 1~4 픽커 8개를 전체 또는 개별 선택** → **별도 일괄 실행 버튼** 한 번으로 선택 콜렛을 연속 측정.
     체크가 1개면 자연히 단일 동작(별도 단일 기준 불필요). 기존 START/단일 콤보 회귀 방지를 위해 일괄은 별도 버튼으로.
   - 실행 순서: side별 **C4(기준 콜렛) 먼저 → C1~3**.
   - **각 콜렛 측정 직후 COC(회전중심) 연속 수행** — 현재도 1:1 Collet Calibration과 COC가 연속 수행되는 구조이므로 일괄 체인에도 포함.
   - **C4 미선택 시**: 기존 C4 저장값(Bottom 기준 티칭)이 유효하면 그 값으로 C1~3 진행 허용
     (기존 선행조건 검사 `IsReferenceColletCalibrationReady` 재사용).
   - 자동 저장 체인: **C4 측정 완료 시 Save Bottom 자동**(Save Bottom은 각 side의 4번 축 전용 규칙 유지)
     → 각 콜렛 **Apply T** → **Save**.

3. **[항목③] Calibration 속도 체계 통일**
   - **안전위치/Avoid 이동**: 각 축의 **Default 속도·가감속을 기반**으로 하고, 설정은 **% 하나**만 사용한다.
     - Default 출처: `AxisData.DefaultVelocity` / `AxisData.Acceleration` / `AxisData.Deceleration`. (사용자 확정)
     - **%는 속도와 가·감속 모두에 곱한다.** (픽커 축은 Default 가감속이 너무 높아 등속도와 함께 낮춰야 함 — 사용자 확정)
     - **1차 기본값 7%.** (사용자 확정)
     - **전역 감속 스케일(MotionSpeedScale)과 중첩 금지.** 캘 안전이동은 "축 Default × 캘 %"로만 계산되어야 하며
       전역 %가 다시 곱해지면 안 된다. (사용자 확정)
   - **측정 이동**: 각 캘의 기존 측정 속도 설정을 그대로 사용(안전이동과 완전 분리).
   - **% 설정 UI 위치**: 레시피 → 캘리브레이션 화면
     (`QMC.CDT-320\Ui\Pages\Recipe\CalibrationPage.cs` — 각 캘 다이얼로그를 여는 버튼들이 있는 화면)에 두고 여기서 설정/저장.

4. **[최종 목표] Auto Calibration에서 여러 캘 동작 일괄 실행**
   - 기존 체인(SafePos → Collet → COC → PickZ → PlaceZ) 재사용하되 ①~③ 정리 후 대상 픽커×캘 종류 선택 실행.

---

## 1. 항목① 구현 프롬프트 — START 안전위치 반복 제거

### 배경(분석 확정)
- 공용 헬퍼 `MovePickerAxisAndVerifyAsync`에는 이미 스킵 로직이 있다:
  `QMC.CDT-320\Sequencing\Picker\PickerSequenceBase.cs:785` `if (!forceMove && CanSkipPickerMoveCommand(axis, target))` → "move skipped. Axis already in position".
- 그런데 `ColletCalibrationSequence`의 시작 안전이동이 전부 `forceMove=true`로 호출해 스킵을 무력화한다:
  - `MoveAllPickerZToAvoidAndVerifyAsync(..., ct, true)` — `ColletCalibrationSequence.cs:448`
  - PickerY Avoid `MovePickerAxisAndVerifyAsync(..., true)` — `:461`
  - `MoveAllPickerTToAvoidAndVerifyAsync(..., ct, true)` — `:474`
- `EnsureInputOutputVisionAvoidForStartAsync`가 같은 시작 단계에서 2회 호출된다 — `:279`, `:289`.
- Auto Cal 경로 이중 구조: `AutoCalibrationSequence.cs:288`에서 `AutoCalibrationSafePositionSequence`(상부축 전체 Avoid)를
  돌린 뒤, 자식 `ColletCalibrationSequence`가 또 자기 시작 안전이동(강제)을 수행한다.

### 구현 지시
1. `ColletCalibrationSequence`의 시작 안전이동 3곳(`:448`, `:461`, `:474`)에서 `forceMove=true` 제거(기본 false).
   - 측정 목표 이동(Bottom X/Y `:353`, T 기준 `:366`, Bottom Z `:378`)의 forceMove는 **유지한다(확정)**. 목표값을 소수점 3자리까지 정확히 전달하기 위한 경로로, 측정 정확도 목적이므로 건드리지 않는다.
2. `EnsureInputOutputVisionAvoidForStartAsync` 중복 호출(`:279`/`:289`)을 1회로 정리한다. **확정: VisionX Avoid는 상대 픽커 이동 "전"에 도달해야 하므로, 상대픽커 Avoid 호출(`:283`) 앞의 1회(`:279`)를 유지하고 뒤 중복(`:289`)을 제거**한다. (VisionX가 상대픽커 이동 전 Avoid에 있어야 간섭이 없음)
3. `CanSkipPickerMoveCommand`의 판정 내용(정지 여부 + in-position tolerance)을 확인하고, 스킵 시에도 "확인" 의미가 성립하는지 검증(축 이동 중/Alarm이면 스킵하지 않아야 함).
4. 다른 캘 시퀀스(NeedleCal, NeedlePin, PickZ, PlaceZ, VisionCamera, VisionFocus, COC)의 시작 안전이동에도 동일한 forceMove 남용이 있는지 grep 후 같은 방식 적용.
5. Auto Cal 이중 구조: `AutoCalibrationSafePositionSequence`가 선행된 경우 자식 캘의 시작 안전이동이 자연히 "이미 위치 → 스킵"으로 동작하는지 확인. forceMove 제거만으로 해소되면 별도 옵션 불필요. 해소가 안 되는 구간(예: Ensure류가 무조건 이동 발행)이 있으면 해당 Ensure 내부에 위치 확인 선행 로직 추가.

### 안전 주의
- 스킵은 "실제로 안전위치에 있음"이 센서/엔코더로 확인될 때만. 확인 불가 시 이동(기존 동작)이 기본. fail-closed 유지.
- 인터락/MotionGuard 경로는 변경하지 않는다.

---

## 2. 항목② 구현 프롬프트 — 콜렛 다중 선택 일괄 측정 + 자동 저장

### 배경(분석 확정)
- 현재 다이얼로그는 Side/Collet No 단일 선택 → `ColletCalibrationSequence(side, colletNo)` 1회 실행
  (`QMC.CDT-320\Ui\Dialogs\ColletCalibrationDialog.cs:350` `RunCalibrationAsync`).
- 기준 콜렛=4번: C1~3은 C4 선행 필요 — `ColletCalibrationSequence.cs:210` `IsReferenceColletCalibrationReady`.
- Save Bottom은 4번 전용 — `ColletCalibrationDialog.cs:1489` (colletIndex==3 아니면 거부), 현재 X/Y/Z/T 실좌표를 Bottom 검사 티칭으로 저장.
- Apply T — `ApplySelectedTHomeOffset`(측정 TZero를 T축 HomeOffset으로 적용).
- side×picker 4→1 역순 루프 선례: `AutoCalibrationDialog.cs:272`, `AutoCalibrationSequence.cs:151` 부근.

### 구현 지시
1. `ColletCalibrationDialog`에 픽커 선택 체크박스 8개(Front C1~C4, Rear C1~C4) + 전체선택 추가.
   - 기존 Side/Collet 단일 콤보는 유지한다(개별 수동 기능 SAVE BOTTOM/APPLY T/COC/Z MOVE 등이 "현재 대상 콜렛" 기준으로 사용). 일괄 실행은 체크박스 기준.
   - **참고(확정)**: 별도 단일 실행 기준은 두지 않는다. 일괄 버튼이 체크된 콜렛만 실행하므로 **1개만 체크하면 단일 동작**이 된다.
2. **별도 일괄 실행 버튼(예: "BATCH START")** 신설(확정). 로직:
   - 선택 세트를 side별로 그룹화 → 각 side에서 **C4 먼저**, 이어 C1→C3 순서로 `ColletCalibrationSequence` 실행.
   - **각 콜렛 측정 성공 직후 COC 연속 수행**(확정): 현재 1:1 Collet Cal과 COC가 연속 수행되는 구조를 일괄 체인에도 반영. 콜렛별로 `ColletCalibrationSequence` → `ColletRotationCenterCalibrationSequence`(COC) 순.
   - C4 미선택 side는 `IsReferenceColletCalibrationReady`로 기존 C4 저장값 유효성 확인 → 유효하면 진행, 아니면 해당 side 중단 + 명확한 한국어 안내.
   - 각 콜렛 실행 간 취소/정지(SEQ STOP) 즉시 반영. 실패 시 해당 side 잔여 콜렛 중단(반대 side 진행 여부는 중단 권장 — 알람 상태에서 연속 동작 금지).
3. 자동 저장 체인:
   - C4 측정 성공 직후: Save Bottom 로직 자동 수행(기존 `SaveCurrentBottomTeachingPosition`의 검증/저장 코드를 재사용 가능한 메서드로 분리 — 4번 전용 규칙과 활성 Recipe 확인 유지. 단 자동 경로에서는 확인 메시지박스 없이 로그로 기록).
   - 각 콜렛 측정 성공 직후: Apply T 자동 수행(`ApplySelectedTHomeOffset` 재사용 분리. 대상 콜렛 파라미터화 필요 — 현재는 다이얼로그 선택값 기준이므로 콜렛 번호 인자를 받는 형태로 분리).
   - 전체 완료 후: Save(설정+레코드 영속, `host.SaveMachineSettings()` 경로) 1회.
4. 진행 상황 표시: lblStatus 및 SAVE HISTORY 영역에 콜렛별 진행/결과 기록.

### 안전 주의
- 연속 실행 중에도 매 콜렛 시작 시 기존 시퀀스의 조건 확인(CheckUnit 등)이 그대로 수행된다(우회 금지).
- Apply T는 HomeOffset 변경(=좌표계 영향)이므로, 측정 실패한 콜렛에는 절대 적용하지 않는다. 성공 레코드(Valid)만 적용.

---

## 3. 항목③ 구현 프롬프트 — 속도 체계 통일 (Default × %)

### 배경(분석 확정 — 속도 소스 인벤토리)
- 속도 소스 10종 혼재. 핵심 문제:
  - Auto Cal 공용 안전위치 시퀀스가 하드코딩 기본값 사용: `AutoCalibrationSafePositionSequence.cs:39` `new CalibrationMotionSettings()`(10/200/200), UI 편집 불가, 미영속.
  - Collet "Move Speed"가 측정+시작 안전이동 겸용: `ColletCalibrationDialog.cs:1051` 설명문 참조.
  - `JogSpeedType.Custom` 아보이드 그룹 이동은 속도만 전달, 가감속은 축 Jog 설정으로 대체: `Equipment\Unit\Common\IUnitJogController.cs:43-60` (`UnitJogVelocityResolver`).
  - 저장소 이원화: 대부분 `Config\CalibrationData`(`CalibrationDataStore.cs`), Focus 스캔 3세트만 VisionUnit 설정파일(`VisionUnit.cs:48` `VisionConfig.FocusCalibration`).
  - 전역 `MotionSpeedScale`은 Default 속도 해석 경로에만 적용되고 명시(explicit) 속도에는 미적용
    (캘 로그의 `explicitVelocityNotDefaultScaled=True`).

### 확정 설계
- **신규 설정 1개**: 캘 안전이동 퍼센트 (`CalibrationData.SafeMovePercent`, 범위 1~100, **기본값 7%** — 확정).
- **Default 출처(확정)**: `AxisData.DefaultVelocity`, `AxisData.Acceleration`, `AxisData.Deceleration`.
- **계산 규칙**: 안전이동의 속도 = `AxisData.DefaultVelocity` × (% / 100), 가속 = `AxisData.Acceleration` × (% / 100), 감속 = `AxisData.Deceleration` × (% / 100). **속도·가속·감속 모두에 % 적용**(확정).
- **중첩 금지 구현 방식(확정)**: 계산된 값을 **명시(explicit) 속도/가감속으로 이동 명령에 직접 전달**한다.
  명시 속도는 `MotionSpeedScale`의 영향을 받지 않으므로(위 인벤토리 확인) "전역 스케일과 중첩 금지" 요구가 자동 충족된다.
  Default 해석 경로(스케일 적용됨)로 보내면 안 된다.
- **UI**: `Ui\Pages\Recipe\CalibrationPage.cs`에 % 입력(NumericUpDown 등) + 저장. 저장은 `CalibrationData` 스토어.
- **측정 속도**: 각 캘 기존 설정 유지. 아래 특수 속도 3종도 그대로 별도 유지:
  1) COC T 회전 `CocRotationVelocityDegPerSec`(deg/s, 단위 다름) — **COC를 일괄 체인에 포함하되 이 T 회전 속도는 % 통일 대상 아님**. 단 COC의 XY 중심 이동(일반/안전 이동)은 % 통일 대상.
  2) PickZ/PlaceZ Coarse/Fine 서치(접촉 서치 저속 — 안전상 별도)
  3) Vision Focus 스캔 3세트(스캔 품질 파라미터)

### 배선 지점(전환 대상 — 안전이동만)
| 대상 | 위치 |
|---|---|
| Auto Cal 공용 안전위치 | `AutoCalibrationSafePositionSequence.cs:39, 151, 191` (하드코딩 제거) |
| Collet 시작 안전이동/VisionX Avoid/상대픽커 Avoid | `ColletCalibrationSequence.cs:536-541, 605-610, 678, 720` (+①에서 forceMove 제거된 Z/Y/T Avoid 경로) |
| PickUpZ 안전이동 | `PickerPickUpZCalibrationSequence.cs:554-559, 769-775, 604-611` |
| PlaceZ 안전이동 | `PickerPlaceZCalibrationSequence.cs:822, 991, 1029` |
| NeedleCal Z Avoid | `NeedleCalibrationSequence.cs:513-524, 717-723` |
| NeedlePin Avoid | `NeedlePinCalibrationSequence.cs:212-227` |
| VisionCamera Avoid | `VisionCameraCalibrationSequence.cs:623-627, 836-876` |
| VisionFocus 카메라 Avoid | `VisionFocusScanSequence.cs:665-670, 722-727` |
| `JogSpeedType.Custom` 아보이드 그룹 경로 | 가감속 전달 가능한 오버로드 보강 또는 명시 속도 경로로 대체(가감속 축 Jog 대체 문제 동시 해소) |

### 구현 순서 제안
1. 축 Default 속도/가감속 프로퍼티의 정확한 이름·해석 헬퍼 확인(축 Config — 구현 전 필수 확인 항목).
2. `SafeMovePercent` 필드 + CalibrationPage UI + 저장/로드.
3. "Default×% → 명시 속도" 계산 헬퍼 1개 신설(공용, 예: CalibrationMotionResolver).
4. 위 배선 지점을 헬퍼 사용으로 전환(안전이동만, 측정이동 불변).
5. 각 다이얼로그의 "Move Speed" 설명문을 "측정 전용"으로 갱신(예: `ColletCalibrationDialog.cs:1051`).

---

## 4. 최종 통합(참고 — 이번 구현 범위 아님)
- ①~③ 완료 후 Auto Calibration 다이얼로그에 대상 픽커 선택(현재는 종류 체크만: `AutoCalibrationDialog.cs:522-524`) 추가,
  콜렛 일괄 체인(②)을 Auto 체인에 편입.

---

## 5. 체크리스트

### 공통(모든 항목)
- [ ] `git status`로 시작 상태 확인, 무관 변경 미포함
- [ ] 별도 OutDir 빌드 성공(원본에서 Clean/Rebuild 금지 — AGENTS.md §13)
- [ ] `git diff --check` 통과, 인코딩(UTF-8 BOM/CRLF) 유지
- [ ] 신규 .cs 파일 생성 시 csproj `<Compile Include>` 등록 확인
- [ ] 커밋은 사용자 명시 요청 시에만
- [ ] 인터락/MotionGuard/와이어 계약 문자열 미변경

### 항목① 검증
- [ ] 안전위치에서 START → Z/Y/T Avoid 이동이 **스킵**되고 로그에 "move skipped. Axis already in position" 기록
- [ ] 안전위치가 아닌 상태에서 START → 이동이 정상 수행
- [ ] 축 이동 중/Alarm 상태에서는 스킵되지 않음(fail-closed)
- [ ] VisionX Avoid가 상대픽커 이동 "전"에 도달(`:279` 유지, `:289` 제거) 후에도 VisionX Avoid 보장됨
- [ ] Auto Cal 실행 시 SafePos 시퀀스 후 자식 캘에서 중복 안전이동 없음
- [ ] 측정 이동(forceMove 유지 구간)의 동작 회귀 없음 — 목표값 소수점 3자리 전달 유지

### 항목② 검증
- [ ] 8개 체크박스 + 전체선택 동작
- [ ] 별도 일괄 버튼으로 실행, 체크 1개면 단일 동작으로 처리
- [ ] Front/Rear 각각 C4 → C1~3 순서로 실행됨
- [ ] 각 콜렛 측정 직후 COC 연속 수행됨(Collet Cal → COC 순)
- [ ] C4 미선택 + 기존 C4 저장값 유효 → C1~3 진행 / 무효 → 해당 side 중단 + 한국어 안내
- [ ] C4 성공 직후 Save Bottom 자동(로그 확인), C4 외 콜렛에서 Save Bottom 수행되지 않음
- [ ] 성공 콜렛만 Apply T 적용(실패 콜렛 미적용), 전체 완료 후 Save 1회
- [ ] SEQ STOP으로 중간 정지 시 즉시 중단, 잔여 콜렛 미실행, 상태 표시 정상
- [ ] 기존 수동 개별 버튼(SAVE BOTTOM/APPLY T/COC/Z MOVE) 단일 동작 회귀 없음
- [ ] SAVE HISTORY/상태 표시에 콜렛별 결과 기록

### 항목③ 검증
- [ ] `AxisData.DefaultVelocity/Acceleration/Deceleration` 값을 기반으로 산출됨
- [ ] `SafeMovePercent` 기본값 7% 적용
- [ ] CalibrationPage에 % 설정 표시/저장/재로드(재시작 후 유지) 확인
- [ ] 안전이동 속도 = 축 Default × % (로그의 velocity/acc/dec 값으로 산식 확인)
- [ ] **가감속에도 %가 적용**됨(속도만 적용 아님)
- [ ] 전역 MotionSpeedScale을 변경해도 캘 안전이동 속도는 **변하지 않음**(중첩 금지 확인)
- [ ] Auto Cal 공용 안전위치 시퀀스가 하드코딩(10/200/200) 대신 % 적용 속도로 동작
- [ ] `JogSpeedType.Custom` 경로의 가감속이 축 Jog값 대체가 아닌 % 적용값으로 전달됨
- [ ] 측정 이동 속도는 기존 설정 그대로(각 캘 다이얼로그 값) — 회귀 없음
- [ ] 특수 속도 3종(COC T/Coarse·Fine/Focus 스캔) 기존 동작 유지
- [ ] % 범위 클램프(1~100) 및 기본값 적용 확인

### 구현 전 확인(오픈 항목) — 모두 확정됨 (2026-07-22 사용자 결정)
1. **축 Default 속도/가감속 프로퍼티**: `AxisData` 클래스의 `DefaultVelocity`, `Acceleration`, `Deceleration` 를 사용한다.
2. **캘 안전이동 %(`SafeMovePercent`) 기본값**: **7%** (1차 지정).
3. **VisionX Avoid 1회화 위치**: **상대 픽커 이동 "전"에 VisionX가 Avoid에 도달**해야 한다(상대픽커 이동 전 1회 확보). 상대픽커 이동 뒤 중복 호출 제거.
4. **측정 이동 forceMove=true 유지**: 유지한다. 소수점 3자리까지 목표값을 전달하기 위해 구현된 경로로 확인됨 → 측정 정확도 목적이므로 그대로 둔다.
5. **② 일괄 실행 버튼**: **별도 버튼 신설**. 대상은 Front C1~4 / Rear C1~4 **체크박스 8개(개별 전체 선택/미선택)**. 별도 버튼은 **체크된 콜렛 전체**를 side별 C4→C1~3 순서로 실행하며, **체크가 1개면 자연히 단일 동작**이 된다(별도 단일 실행 기준 불필요). 기존 수동 개별 버튼(COC/Z MOVE/SAVE BOTTOM/APPLY T 등)이 쓰는 "현재 대상 콜렛" 개념은 유지 방식 확인 필요(§2-6 참조).
6. **COC 일괄 포함**: 포함한다. COC는 1:1 Collet Calibration과 **연속 수행**하는 구조이므로, 일괄 체인에서 각 콜렛 측정 직후 COC를 이어서 수행한다. 단 **COC T 회전 속도는 별도 파라미터(`CocRotationVelocityDegPerSec`, deg/s)를 그대로 유지**(안전이동 % 통일 대상 아님). COC의 XY 중심 이동(일반/안전 이동)은 % 통일 대상.
