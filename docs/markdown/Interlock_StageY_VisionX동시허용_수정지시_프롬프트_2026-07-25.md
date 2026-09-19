# CDT-320: InputStage busy 인터락에서 WaferStageY를 InputVisionX 이동 차단 대상에서 제외 (C안)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Interlocks\InputStageInterlockRules.cs` 단 하나이며,
그 안의 `VerifyInputStageNotBusy`(현재 `:1653`) 조건 1곳만 고친다.**

시퀀스, 유닛, AjinAxis, AXM, SharedRailX, 설정 데이터(`interlock-check-matrix.json` 포함), UI는
절대 건드리지 않는다.

> ⚠ **이 작업은 인터락 완화다.** 평소 규칙("인터락 판정 코드 diff 0건")의 예외로,
> **사용자 명시 승인(2026-07-25)** 에 따라 진행한다. 승인 범위는 아래 "허용 범위"에 적힌
> 조합 **1개**뿐이다. 그 밖의 어떤 인터락 조건도 완화·삭제·우회하지 말 것.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 속도값을 다루지 않는다** — 인터락 판정 조건 1줄만 바꾼다. 모션 관련 코드를
새로 작성하지 말 것.

### 규칙 3. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 4. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 (`파일:행` + 변경 전/후 조건식).
2. **변경 후에도 차단이 유지되는 조합 전체 목록** — `VerifyInputStageNotBusy`가 여전히 막는
   (moving 축 × 이동 중 축) 조합을 표로 정리해 보고한다. 완화 범위가 1개인지 검증하는 근거다.
3. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
4. **[확인 요청 1]** 의 답 (역방향 완화 필요 여부).
5. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경 — 실장비 알람 (2026-07-25 17:18:55)

마크검사 회피를 픽업 이연·팔로잉으로 위임한 직후, Auto 운전에서 아래 알람으로 시퀀스가 정지했다.

```
17:18:55.431  MotionGuard > Interlock blocked. moving=WaferStageY. InputVisionX is moving. - Check
17:18:55.437  Machine status set to Alarm by alarm response. code=INTERLOCK
17:18:55.439  Interference group stop requested. sourceAxis=InputStageY, emergency=True
17:18:55.445  InputStageY ABS MOVE failed. result=-11,
              reason=Interlock blocked. moving=WaferStageY. InputVisionX is moving.,
              target=215.105 mm, pos=196.749 mm
```

직전 타임라인:
```
55.305  InputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다. target=93.379
55.326  AxisMoveProfile > InputVisionX ABS MOVE. target=93.379, vel=50, acc=500  ← 422.55→93.38, 약 329mm
55.327  이연 최소 회피 이동을 비동기 시작
55.329  PickUp 피커X 팔로잉 진입을 시작합니다. leading=InputVisionX
55.426  NeedleX/StageY pick path selected. order=NeedleX->StageY, targetStageY=215.105
55.431  ★ Interlock blocked. moving=WaferStageY. InputVisionX is moving.
```

회피 거리가 약 329mm(5% 속도 50mm/s에서 약 6.6초)로 커지면서, 픽업 default 경로가 원래
**병렬로 설계한** "피커X 진입 ∥ NeedleX/StageY 이동"에 이연된 비전 회피까지 겹쳤다.
그런데 `VerifyInputStageNotBusy`가 "InputVisionX 이동 중이면 StageY 이동 금지"를 요구해
설계와 정면 충돌한다. 위임 이전에는 비전이 픽업 전에 이미 회피 위치였거나 수 mm 이동이라
겹치지 않아 드러나지 않았다.

## 완화 근거 (3건 — 주석으로 코드에 남길 것)

1. **물리 간섭 없음** — `InputVisionX`(카메라 X)와 `InputStageY`(웨이퍼 Y)는 기계적으로 간섭하지
   않는 축이다. **사용자 확인 2026-07-25.**
2. **선언 매트릭스와 불일치** — `D:\CDT-320\Config\interlock-check-matrix.json`의
   `MovingName="WaferY"` 행이 요구하는 검사는 `InputFeederY` / `Feeder Up/Down` /
   `Feeder Clamp/UnClamp` / `WaferExpandingZ` / `NeedleX` / `NeedleZ` / `EjectPinZ` **7건뿐이고
   `InputVisionX`는 없다.** 실장비 로그의 StageY MotionGuard 라인도 동일하다
   (`checks=[InputFeederY@E12, InputFeederLift@F12, InputFeederClamp@G12, WaferExpandingZ@J12,
   NeedleX@L12, NeedleZ@M12, EjectPinZ@N12]`). 즉 이 차단은 선언된 인터락이 아니라
   `VerifyInputStageNotBusy` 안의 하드코딩 결합이었다.
3. **StageY의 실제 간섭 판정은 CameraX 기준이 아니다** — StageY 이동 가능 영역은
   `stage.IsNeedleWorkPointInArea` / `TryResolveNeedleWorkPointMoveOrder`가 **NeedleX/StageY 좌표**로
   판정한다(`PickerInputStageMoveHelper.BuildWorkPointTargetName` 주석: "StageY 실제 간섭 반경은
   CameraX가 아니라 NeedleX/StageY 좌표로 계산한다"). 그리고 `NeedleX`는 이미 이 규칙의 예외이므로
   **NeedleX ∥ StageY 동시 이동은 기존에도 허용**돼 있었다. CameraX 동시 이동을 허용하는 것은
   이 설계와 일관된다.

---

# 수정 내용

대상: `InputStageInterlockRules.cs`, `VerifyInputStageNotBusy`(`:1653`) 내부의
**"InputVisionX is moving." 차단 조건**(현재 `:1667~1669`).

## 허용 범위 (승인된 완화 — 이것만)
| moving 축 | 이동 중 축 | 변경 전 | 변경 후 |
|---|---|---|---|
| `WaferStageY` (= InputStageY / StageY / WaferY) | `InputVisionX` (CameraX) | 차단 | **허용** |

그 밖의 모든 조합은 **변경 없이 차단 유지**다.

## 변경 전 (이 코드를 찾아라)
```csharp
            if (!IsNeedleXMove(movingName) &&
                IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
```

## 변경 후
```csharp
            // 기존 조건: InputVisionX가 이동 중이면 NeedleX를 제외한 모든 InputStage 축 이동을 차단했다.
            // 현재 기준(사용자 승인 2026-07-25): WaferStageY도 예외로 둔다.
            //   근거 1 — 물리 간섭 없음(사용자 확인 2026-07-25): InputVisionX(카메라 X)와
            //            InputStageY(웨이퍼 Y)는 기계적으로 간섭하지 않는 축이다.
            //   근거 2 — 선언 매트릭스 불일치: interlock-check-matrix.json의 MovingName="WaferY" 행이
            //            요구하는 검사는 InputFeederY / Feeder Up-Down / Feeder Clamp-UnClamp /
            //            WaferExpandingZ / NeedleX / NeedleZ / EjectPinZ 7건뿐이며 InputVisionX는 없다.
            //            이 차단은 선언된 인터락이 아니라 이 함수의 하드코딩 결합이었다.
            //   근거 3 — StageY의 실제 간섭 반경은 CameraX가 아니라 NeedleX/StageY 좌표로 판정한다
            //            (IsNeedleWorkPointInArea / TryResolveNeedleWorkPointMoveOrder,
            //             PickerInputStageMoveHelper.BuildWorkPointTargetName 주석). NeedleX는 이미
            //            이 규칙의 예외이므로 NeedleX∥StageY 동시 이동은 기존에도 허용됐다.
            //   목적 — 픽업의 InputVisionX 이연 최소 회피(약 330mm, 5%에서 ~6.6초)와 StageY 진입
            //          이동이 겹쳐 -11로 실패하던 문제 해소(실장비 2026-07-25 17:18:55).
            //   주의 — 완화 범위는 (moving=WaferStageY × 이동 중=InputVisionX) 조합 1개뿐이다.
            //          WaferStageT / ExpanderZ / NeedleZ / EjectPinZ는 그대로 차단된다.
            if (!IsNeedleXMove(movingName) &&
                !IsWaferStageYMove(movingName) &&
                IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
```

구현 요건:
- `IsWaferStageYMove`는 **이미 존재하는 헬퍼**(`:1699`)다. 새로 만들지 말고 그대로 쓴다
  (`WaferStageY` / `InputStageY` / `StageY` / `WaferY` 4개 이름을 모두 인식한다).
- 조건 3개의 순서는 위 코드대로 유지한다(`IsNeedleXMove` → `IsWaferStageYMove` → `IsMovingExcept`).
  `IsMovingExcept`가 마지막이어야 축 상태 조회를 불필요하게 하지 않는다.
- `MotionGuardRuleHelpers.Block` 호출과 메시지 문자열(`"InputVisionX is moving."`)은 **변경 금지**.
  기존 로그/알람 문구가 바뀌면 현장 대조가 끊긴다.

## 변경 금지 (같은 함수 안에서도)
- `:1660~1662` **"WaferStageY is moving." 차단** — 역방향(moving=InputVisionX 등이 StageY 이동에
  막히는 조건). **손대지 말 것.** [확인 요청 1] 참조.
- `:1663~1664` "WaferStageT is moving." 차단
- `:1665~1666` "ExpanderZ is moving." 차단
- `:1670` 이후 `VerifyInputStageNotBusy`의 나머지 조건 전체
- `IsNeedleXMove`(`:1692`) / `IsWaferStageYMove`(`:1699`) / `IsMovingExcept`(`:1981`) 본문
- `VerifyInputVisionXClearForExpanderZ`(`:1746`) — ExpanderZ 전용 별도 규칙
- `VerifyInputStageNotBusy` 호출부 9곳(`:80`, `:256`, `:420`, `:623`, `:848`, `:1143`, `:1230`,
  `:1245`, `:1320`) — 인자·호출 위치 무변경
- 이 파일의 다른 모든 인터락 규칙
- `interlock-check-matrix.json` — 설정으로 완화하지 말 것. 코드 조건 1줄만 바꾼다.

## [확인 요청 1] — 역방향 완화 필요 여부 (수정하지 말고 분석·보고)
`:1660~1662`는 반대 방향, 즉 **"WaferStageY가 이동 중이면 InputVisionX 이동 금지"** 를 만든다.
물리 간섭이 없다면 논리적으로는 이쪽도 완화 대상이지만, **실장비에서 관측된 적이 없어 이번
승인 범위에서 제외**했다. 아래를 코드로 확인해 보고하라:
1. 현재 코드 흐름에서 "StageY가 이동 중인 상태로 InputVisionX 이동 명령이 발행되는" 경로가
   존재하는가? 확인 대상:
   - `PickerPickUpSequence`의 이연 회피 시작 지점(`StartDeferredInputVisionRetreatIfPendingAsync`)과
     NeedleX/StageY 이동 시작 지점의 선후 관계 (로그상 비전이 105ms 먼저 출발했다)
   - `InputDieVisionPrepareSequence.MoveInputStageVisionPointForPickerAsync`의 L자 경로
     (y-first 분기에서 StageY를 await 완료 후 VisionX를 이동하는지)
   - FastConti / Conti 픽업 경로의 StageY 태스크와 이연 회피 시작 순서
2. 존재하지 않는다면 "역방향 완화 불필요"로 보고한다. 존재한다면 **어느 경로에서 언제 발생하는지**
   구체적으로 적고 사용자 승인을 요청한다. **승인 없이 완화하지 말 것.**

---

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat` = `InputStageInterlockRules.cs` **한 파일만**.
3. `git diff`의 실제 변경이 **조건 1줄 추가(`!IsWaferStageYMove(movingName) &&`) + 주석**뿐인지
   확인. 다른 인터락 조건이 한 글자도 바뀌지 않아야 한다.
4. `"WaferStageY is moving."` / `"WaferStageT is moving."` / `"ExpanderZ is moving."` 세 차단이
   그대로 남아 있는지 확인.
5. `interlock-check-matrix.json` 및 `D:\CDT-320\Config\*` 무변경 확인.
6. `grep -n "BeginMotionGuardBypass" QMC.CDT-320/Equipment/Interlocks/InputStageInterlockRules.cs`
   → 0건 (우회 API 미사용).
7. 규칙 4의 보고 항목 5개 작성 완료.

## 검증 기준
**시뮬**
1. Auto + Conti 픽업 1배치. `Interlock blocked. moving=WaferStageY. InputVisionX is moving.`
   로그가 **0건**이어야 한다.
2. 로그 타임스탬프로 겹침 확인: `AxisMoveProfile > InputVisionX ABS MOVE`가 진행 중인 동안
   `AxisMoveProfile > InputStageY ABS MOVE`가 발행되고 정상 완료된다.
3. StageY 이동의 MotionGuard 라인에 선언된 7개 검사가 그대로 수행되는지 확인
   (`checks=[InputFeederY@E12, InputFeederLift@F12, InputFeederClamp@G12, WaferExpandingZ@J12,
   NeedleX@L12, NeedleZ@M12, EjectPinZ@N12]` — 항목이 줄어들면 안 된다).
4. 회귀 확인: WaferStageT / ExpanderZ / NeedleZ / EjectPinZ 이동이 InputVisionX 이동 중에는
   여전히 `-11`로 차단되는지 최소 1건 확인(수동 조작 또는 코드 리뷰로 근거 제시).

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
1. ScalePercent 5%, Auto + Conti 1배치.
2. `Main_*.log`에서 시뮬 검증 1~3과 동일 항목 확인.
3. 겹침 구간에서 InputStageY/InputVisionX의 실제 거동을 눈으로 확인 — 두 축이 동시에 움직여도
   기구 접촉·이상 진동이 없는지. **이상이 보이면 즉시 정지하고 로그 보존 후 보고.**

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. 겹침이 성립하면 StageY는 비전 회피(~6.6초)를 기다리지 않게 되지만, 픽업 분기 종료 시점의
   `JoinInputVisionRetreatMoveTaskAsync("피커 X 진입 완료")`에서 여전히 회피 완료를 await한다.
   즉 배치 첫 픽의 택타임은 비전 회피 시간에 하한이 걸린다. 개선은 별도 결정 사항이다.
2. 위임으로 회피 거리가 커진 데 따른 팔로잉 타임아웃(`VisionFollowEntryTimeoutMs` 15000ms)
   여유 검토는 앞선 프롬프트의 [확인 요청 2] 항목이며 이번 작업 범위가 아니다.
3. `VerifyInputStageNotBusy`는 선언 매트릭스에 없는 결합을 여러 개 하드코딩하고 있다
   (StageT/ExpanderZ/NeedleZ/EjectPinZ 조합). 매트릭스와 코드의 정합성 전면 점검은 별도 과제다.
   **이번 작업에서 추가 완화하지 말 것.**
