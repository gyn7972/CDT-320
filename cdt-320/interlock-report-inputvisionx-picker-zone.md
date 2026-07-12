# 인터락 수정 사항 레포트 — InputVisionX 이동 전 Picker Input 존 판정

- 작성일: 2026-07-12
- 상태: **승인·적용 완료** — 팀장님 기구 확인(2026-07-12): "InputVisionX가 Avoid로
  이동할 때만 간섭이 없다" → 예외를 **Avoid 목표 이동에 한정**해 §4보다 좁게 구현

## 0. 최종 구현 (§4 변경안에서 축소 적용)

`VerifyPickerInputZoneClearForInputVisionX`에 아래 조건을 **모두** 만족할 때만
허용하는 예외 추가 (하나라도 어긋나면 기존대로 차단):

1. **InputVisionX 이동 목표가 Avoid 티칭 위치** (신설 `IsInputVisionXTargetAtAvoid`,
   기존 `IsEjectPinZTargetAtAvoid` 패턴 준용 — CameraX InPositionTolerance 이내 비교)
2. PickerY가 Avoid/Home 후퇴 위치 (`state.YAvoid`)
3. Picker X/Y 축 정지 (이동 중이면 차단)
4. Input 존 진입/이탈 이동 위험 없음 (`movingIntoOrInsideInput == false`)
5. 작업영역 점유 없음 (`WorkAreaBlocksTransport == false`)
6. Unknown 아님 (`UnknownUnsafe == false`)

전달 경로: `VerifyWaferVisionX`(수동/자동 분기에서 `IsInputVisionXTargetAtAvoid(request)`
계산) → `CanManualInputVisionX` → `VerifyFrontRearPickerInputZoneClearForInputVisionX`
→ Front/Rear 개별 판정. Home 규칙(`CanHomeInputVisionX`)은 무변경.
차단 로그에 `targetAtAvoid=` 필드 추가(진단용). Clean Rebuild 통과.

**Avoid 외 목표(Process 등) 이동은 Picker가 Input 존에 있는 한 기존대로 차단 유지.**

## 1. 현재 조건 (코드 근거)

`InputStageInterlockRules.VerifyPickerInputZoneClearForInputVisionX` (693행)는
InputVisionX 이동 전 Front/Rear Picker 각각에 대해 다음이면 차단한다:

```
blocking = state.BlocksTransport || movingIntoOrInsideInput
BlocksTransport = IsRequestedZoneActive || WorkAreaBlocksTransport || UnknownUnsafe
IsRequestedZoneActive = (CurrentZone==Input || TargetZone==Input)
```

핵심: `CurrentZone`은 **PickerX 위치 기준**으로 판정되며, **PickerY가 Avoid(후퇴)
상태인지는 차단 판정에 반영되지 않는다** (yAvoid는 로그에만 출력).

## 2. 문제 재현 (2026-07-12 실장비 Alarm 로그)

```
21:18:45 INTERLOCK> InputVisionX 이동 불가: FrontPicker가 Input 영역을 점유...
  movingX=False, movingY=False, movingInputRisk=False,
  currentZone=Input, targetZone=Input,
  workAreaBlocksTransport=False, workAreaPhysicalClear=False,
  yAvoid=True, unknownUnsafe=False, x=312.947, y=0
21:18:45 READY-INPUT-STAGE-VISION-X> InputStage VisionX Avoid 이동 명령 실패. result=-11.
  actual=-0.1, target=0(Avoid), tolerance=0.01
```

- Ready 순서 변경은 정상 적용됨: **PickerY=0(Avoid), yAvoid=True, X/Y 정지** 상태.
- 그럼에도 PickerX=312.947이 Input 존 X 범위 안이라 `currentZone=Input` → 차단.
- 차단 사유는 오직 `IsRequestedZoneActive` (workArea/unknown/이동위험 전부 False).

### 교착 구조

- InputVisionX 이동 ← "Picker가 Input 존 밖일 것" 요구 (X 기준)
- Ready PickerX Avoid 이동 ← `CheckInputVisionXAvoidBeforeReadyPickerX`가
  "InputVisionX가 Avoid일 것" 요구 (현재 actual −0.1, avoid 0, tol 0.01 → 불충족)
- → **서로 상대를 선행 조건으로 요구하는 교착.** VisionX는 Avoid에서 불과 0.1mm
  떨어져 있는데도 풀리지 않음.

## 3. 변경 대상 Rule

- 파일: `QMC.CDT-320/Equipment/Interlocks/InputStageInterlockRules.cs`
- 함수: `VerifyPickerInputZoneClearForInputVisionX` (693–723행)

## 4. 변경안 (허용/차단 범위)

같은 파일군에 이미 존재하는 예외 패턴을 준용한다 —
`PickerZoneInterlockRules` 739–742행 (Feeder HOME): *"PickerY가 Home/Avoid이고
Picker X/Y/Z가 정지 상태면 허용"*.

**허용 추가**: 아래 조건을 모두 만족하면 InputVisionX 이동 허용
- `state.YAvoid == true` (PickerY가 Avoid/Home 후퇴 위치)
- PickerX / PickerY 축 정지 상태 (`IsMoving == false`)
- `WorkAreaBlocksTransport == false` (작업영역 점유 없음)
- `UnknownUnsafe == false`

**차단 유지** (변경 없음):
- PickerY가 전진 상태(yAvoid=False)면 기존대로 차단
- Picker X/Y 이동 중, Input 존 진입/이탈 중이면 기존대로 차단
- 작업영역 점유(workArea) / Unknown 상태면 기존대로 차단
- Front/Rear 각각 독립 판정 유지

조건 바로 위에 한 줄 한글 주석을 남긴다 (인터락 규칙 준수).

## 5. 전제 — 팀장님 기구 확인 필요

**"PickerY가 Avoid(후퇴) 위치면, PickerX가 Input 존 X 범위 어디에 있어도
InputVisionX가 전 구간을 이동할 때 물리 간섭이 없다"** — 이 기구적 전제가
맞아야 본 변경이 안전합니다. (InputVisionX 카메라 캐리지와 Picker 헤드가
Y 후퇴 상태에서 상하/전후로 완전히 비켜나는지 확인 요망)

만약 Y 후퇴만으로는 부족하고 특정 X 구간에서 간섭이 있다면, 대안으로
X 간섭 구간 검사를 추가한 조건부 허용으로 설계를 바꿔야 합니다.

## 6. 충돌 위험

- 전제(§5)가 참이면: 추가 위험 없음 — 물리적으로 간섭 없는 상태의 이동만 새로 허용
- 전제가 거짓이면: InputVisionX와 Picker 헤드 충돌 가능 → **승인 전 기구 확인 필수**

## 7. 검증 항목

1. Ready: Picker가 Input 존 X 위치 + Y Avoid 상태에서 Ready 실행 →
   InputVisionX Avoid 이동이 알람 없이 완료되는지
2. Ready: Picker Y 전진 상태에서 시작 → 기존대로 Z→Y Avoid 후 VisionX 이동 순서 동작
3. 차단 회귀: PickerY 전진 상태에서 수동 InputVisionX 이동 시도 → 여전히 차단(-11)되는지
4. 자동 공정: 선행검사(InputVisionX 이동)와 PickUp 병행 시 기존 대기/허가 흐름 정상
5. Front/Rear 각각 확인

## 8. 대안 (인터락 무수정)

Ready 순서에서 PickerX Avoid를 VisionX보다 앞으로 이동 — 단,
(a) Ready 자체 선행검사(`CheckInputVisionXAvoidBeforeReadyPickerX`)가 VisionX Avoid를
요구해 동일 교착이며 이 검사 완화도 결국 같은 기구 질문으로 귀결,
(b) SharedRailX 간섭 규칙상 PickerX 이동이 VisionX 위치(−0.1)와 충돌 판정될 수 있음.
→ 근본 해결이 아니어서 권장하지 않음.
