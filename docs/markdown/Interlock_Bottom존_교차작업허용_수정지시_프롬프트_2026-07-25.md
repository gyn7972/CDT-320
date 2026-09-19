# CDT-320: Bottom/Side 작업영역 점유 인터락 완화 — 반대 피커의 픽업 존 작업 허용

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 `QMC.CDT-320\Equipment\Interlocks\PickerZoneInterlockRules.cs` 단 하나이며,
`CanAutoShareProcessWorkAreaWhenOppositeYSafe`(현재 `:1835`) 조건 1곳만 고친다.**

시퀀스, AjinAxis, AXM, SharedRailX, UI, 설정 데이터, `D:\CDT-320\*`는 건드리지 않는다.

> ⚠ **이 작업은 인터락 완화다.** 평소 규칙("인터락 판정 코드 diff 0건")의 예외로,
> **사용자 명시 승인(2026-07-25: "바텀 촬영중에 다른 피커가 픽업 존에서 작업해도 괜찮아,
> 인터락 해제하고 정상 작동하도록")** 에 따라 진행한다.
> 승인 범위는 아래 "허용 범위"의 조건 완화 **1건**뿐이다. 그 밖의 어떤 인터락도 손대지 말 것.

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 모션 코드를 전혀 다루지 않는다.** 인터락 판정 조건만 수정한다.

### 규칙 3. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 4. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 (`파일:행` + 변경 전/후 조건식).
2. **변경 후에도 차단이 유지되는 조합 전체 목록** — 표로 정리해 보고한다. 완화 범위가 1건인지
   검증하는 근거다.
3. **[확인 요청 1]** 답변 (물리 충돌 방지 2중 구조가 그대로 살아 있음을 호출 체인으로 증명).
4. 스케일 미적용 잔존 지점 감사 결과. 없으면 "없음" 명시.
5. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경 — 실장비 확정 (2026-07-25 20:27:24)

Front가 Bottom/Side 검사 중인 상태에서 Rear가 픽업 진입을 시도하자 인터락이 차단하고,
그 차단이 `Critical` 알람으로 승격되어 **라인 전체가 정지**했다.

```
20:27:24.487  RearPickerPickUpSequence PickUp 피커X 팔로잉 진입을 시작합니다.
              visionTarget=345.346510, pickerTarget=376.346510
20:27:24.495  ★ Interlock blocked. moving=RearPickerX.
              RearPickerX 이동 불가: Bottom 작업 영역을 반대 픽커가 사용 중입니다.
              owner=FrontPickerBottomAndSideInspectionSequence:BottomAndSideInspection:ProcessSide,
              Auto 검사 연속 이동 태그가 없습니다.
              currentZone=Avoid, targetZone=Bottom, xActual=700, yActual=0.
20:27:24.498  Machine status set to Alarm. code=INTERLOCK
20:27:24.498  Interference group stop requested. sourceAxis=RearPickerX, emergency=True
20:27:24.498~ Front/Rear/Input/Output 전 시퀀스 즉시 취소
20:27:24.510  (부수 증상) VisionX move failed. result=-5 — 이연 회피가 8ms 만에 감속 정지됨
```

## 차단 지점
`PickerZoneInterlockRules.cs:1460~1485`:
```csharp
                if (targetZone != PickerWorkZone.Unknown &&
                    targetZone != PickerWorkZone.Input &&
                    !IsAvoidZone(targetZone) &&
                    IsOtherPickerWorkAreaActive(isFront, targetZone, out occupiedOwner))
                {
                    string shareDetail;
                    if (CanAutoShareProcessWorkAreaWhenOppositeYSafe(request, isFront, targetZone, out shareDetail))
                    { /* 허용 */ }
                    else
                    {
                        return MotionGuardRuleHelpers.Block(... targetZone + " 작업 영역을 반대 픽커가 사용 중입니다. ...");
                    }
                }
```
공유 허용 판정은 `CanAutoShareProcessWorkAreaWhenOppositeYSafe`(`:1835`)이며, 네 조건을 모두
요구한다:
1. `request.MoveKind == AxisTeachingMove` (Auto 이동)
2. **`request.Intent.InspectionContinuous` 태그** (`:1860`)
3. `IsProcessZone(targetZone)`
4. 상대 PickerY가 전진/이동 중이 아님 (`IsPickerYOutOrMovingOut`, `:1875`)

이번 차단의 사유는 **조건 2 미충족**이다 — 로그의 `"Auto 검사 연속 이동 태그가 없습니다."` 가
그 문구다(`:1862`). Rear는 픽업(PickUp) 이동이라 검사 연속 태그가 없다.

## 왜 targetZone이 Bottom인가
Rear는 X=700(Avoid)에서 픽업 목표 376.35(Input)로 가는 중이었다. 그런데 차단 시점의
`request.TargetValue`는 **팔로잉의 중간 좌표**이고, 그 좌표가 Bottom 존 밴드에 속해
`targetZone=Bottom`으로 해석됐다. 즉 Rear는 Bottom에서 작업할 의도가 없고 **Bottom 밴드를 통과**
하려던 것이다. 로그의 `yActual=0`(Rear PickerY 후진/Avoid)이 물리적으로 안전한 상태였음을 보여준다.

## 완화 근거
1. **물리 간섭 없음** — 이 규칙은 물리 충돌 판정이 아니라 **논리적 작업영역 점유(리소스) 판정**이다.
   피커 헤드간 물리 충돌은 아래 2중 구조가 독립적으로 담당한다(둘 다 이번 작업에서 무변경):
   - 사전 차단: `VerifyFacingYDistanceFirst`(`:1156`) — `MotionGuardRuleRegistry:129`에 **가장 먼저
     등록**되어 모든 축 이동에서 최우선 평가. X 안전거리(기본 150mm,
     `DefaultPickerYFacingXClearance` `:113`) 안에서는 최소 한쪽 PickerY가 정확한 teaching Avoid에
     정지해 있어야 한다.
   - 실시간 감시: `RealtimeCollisionSupervisor.EvaluateRealtime`(`:185`) — 인터락 통과 여부와 무관하게
     폴링하며 `bothYNotRetracted && xPathUnsafe`면 전축 하드정지.
2. **선례가 이미 있다** — 같은 함수의 조건 4(상대 PickerY 안전)만으로 Process 존 공유를 허용하는
   경로가 이미 승인·구현되어 있다(`:1466~1471` 주석: "Auto Bottom/Side 연속동작은 반대 PickerY가
   실제 Avoid/Home이면 같은 Process 점유 중에도 X 이동을 허용한다"). 이번 완화는 그 예외를
   **검사 연속 동작에서 Auto 전체로 확장**하는 것이다.
3. **사용자 확인(2026-07-25)** — 바텀 촬영 중 다른 피커가 픽업 존에서 작업해도 무방하다.

---

# 수정 내용

## 허용 범위 (승인된 완화 — 이것만)
| 조건 | 변경 전 | 변경 후 |
|---|---|---|
| Auto 이동 + Process 존 점유 + **검사 연속 태그 있음** + 상대 PickerY 안전 | 허용 | 허용(무변경) |
| Auto 이동 + Process 존 점유 + **검사 연속 태그 없음** + 상대 PickerY 안전 | **차단** | **허용** |
| Manual 이동 (`MoveKind != AxisTeachingMove`) | 차단 | **차단 유지** |
| 상대 PickerY 전진/이동 중 | 차단 | **차단 유지** |
| `targetZone`이 Process 계열이 아님 | 차단 | **차단 유지** |

## 변경 — `CanAutoShareProcessWorkAreaWhenOppositeYSafe`(`:1835`)의 조건 2 제거

### 변경 전 (`:1859~1864`)
```csharp
                // 인터락 조건: 검사 연속 이동 태그가 없으면 Process 작업영역 공유 예외를 적용하지 않는다.
                if (request.Intent == null || !request.Intent.InspectionContinuous)
                {
                    detail = "Auto 검사 연속 이동 태그가 없습니다.";
                    return false;
                }
```

### 변경 후
위 블록을 **삭제**하고 그 자리에 아래 주석을 남긴다.
```csharp
                // 기존 조건: 검사 연속 이동 태그(Intent.InspectionContinuous)가 있어야만 Process
                //           작업영역 공유를 허용했다. 그래서 픽업/플레이스 이동이 Bottom/Side 밴드를
                //           통과할 때 "Bottom 작업 영역을 반대 픽커가 사용 중입니다"로 차단됐고,
                //           그 차단이 Critical 알람으로 승격되어 라인이 정지했다
                //           (실장비 2026-07-25 20:27:24, Rear 픽업 진입 / Front Bottom 검사 중,
                //            currentZone=Avoid, targetZone=Bottom, yActual=0).
                // 현재 기준(사용자 승인 2026-07-25): Auto 이동은 태그와 무관하게, 상대 PickerY가
                //           실제 Avoid/0 위치면 Process 작업영역 공유를 허용한다.
                //   근거 1 — 이 규칙은 논리적 작업영역 점유 판정이며 물리 충돌 판정이 아니다.
                //            피커 헤드간 물리 충돌은 VerifyFacingYDistanceFirst(:1156, 레지스트리
                //            최우선 등록, X 안전거리 150mm + 한쪽 PickerY 정확 Avoid)와
                //            RealtimeCollisionSupervisor(전축 하드정지)가 독립적으로 담당하며
                //            이번 완화로 바뀌지 않는다.
                //   근거 2 — 사용자 확인: 바텀 촬영 중 다른 피커가 픽업 존에서 작업해도 무방하다.
                //   주의 — Manual 이동(조건 1)과 상대 PickerY 전진/이동 중(아래 조건)은 그대로 차단된다.
```

### 함께 수정할 것
- `detail` 성공 문구(`:1881`)를 태그 조건 제거에 맞게 갱신한다:
  ```csharp
                detail = "Auto 이동이고 상대 PickerY가 실제 Avoid 또는 0 위치입니다.";
  ```
- 메서드 XML/요약 주석에 "검사 연속" 표현이 있으면 실제 조건과 맞게 수정한다.
- **메서드 이름은 바꾸지 않는다.** 호출부가 있고 이름 변경은 diff를 불필요하게 넓힌다.
  (이름이 실제 조건과 어긋나는 점은 [확인 요청 1]에 보고만 한다.)

## 변경 금지 (같은 파일 안에서도)
- `:1445~1458` **Input 존 점유 차단** (`"Input 픽업 영역을 반대 픽커가 사용 중입니다."`).
  두 피커가 동시에 Input을 노리는 경우이므로 **그대로 차단 유지.** 손대지 말 것.
- `:1460~1463`의 차단 진입 조건 4개(`targetZone != Unknown`, `!= Input`, `!IsAvoidZone`,
  `IsOtherPickerWorkAreaActive`) — 무변경. 여기를 고쳐 우회하지 말 것.
- `CanAutoShareProcessWorkAreaWhenOppositeYSafe`의 나머지 조건:
  - `request == null || request.Machine == null` 방어(`:1846`)
  - **`MoveKind != AxisTeachingMove` → Manual 차단(`:1853`)**
  - `!IsProcessZone(targetZone)` 차단(`:1867`)
  - **`IsPickerYOutOrMovingOut(상대)` 차단(`:1875`)**
  - `catch` 블록의 fail-closed 동작(`:1884~1888`)
- `VerifyFacingYDistanceFirst`(`:1156`), `CanMovePickerAxisByFacingYInterlock`(`:640`),
  `CanMovePickerXPairByFacingYInterlock`(`:688`), `VerifyExactPickerYAvoidForFacingMove`,
  `ResolvePickerYFacingXClearance`, `DoXMovePathsEnterFacingClearance` — **물리 충돌 방지 핵심. 무변경.**
- `RealtimeCollisionSupervisor.cs` 전체 — 무변경.
- `MotionGuardRuleRegistry.cs` 등록 순서 — 무변경.
- `IsOtherPickerWorkAreaActive`, `ResolveTargetXZoneWithContext`, `ParseZone`,
  `ResolvePickerXZoneByNameOrPosition`, 존 밴드 판정 전체 — 무변경.
  존 해석을 바꿔서 해결하려 하지 말 것.
- `AlarmResponseService` / 알람 severity·scope — 이 차단이 `Critical`/`InterferenceGroup`으로
  승격되어 라인을 세우는 문제는 **별도 결정 사항**이다. 손대지 말 것.
- `BeginMotionGuardBypass` 등 우회 API 사용 절대 금지.

---

## ★ [확인 요청 1] — 물리 충돌 방지가 그대로 살아 있음을 증명 (필수)
사용자 요청(2026-07-25)에 따라 **피커 헤드간 Y 충돌 방지 인터락이 정상 작동함을 재확인**한다.
**코드 수정 없이 분석·보고만 한다.**

1. `VerifyFacingYDistanceFirst`가 이번 완화와 **독립적으로 평가되는지** — `MotionGuardRuleRegistry`
   등록 순서상 `PickerZoneInterlockRules.VerifyFacingYDistanceFirst`가 먼저이고,
   `PickerFrontInterlockRules` / `PickerRearInterlockRules`(이번 차단이 나온 경로)가 나중임을
   호출 체인으로 확인한다. 즉 **완화된 경로도 Y 대향 게이트를 이미 통과한 뒤**임을 증명한다.
2. `IsAxisMotionRequest`(`:1345`)가 `AxisTeachingMove`를 포함하므로 **위치 오버라이드
   (`TryOverridePosition` → `VerifyAxisTeachingMove`)도 Y 대향 게이트를 통과해야 함**을 확인한다.
3. `RealtimeCollisionSupervisor`가 인터락 통과 여부와 무관하게 폴링하며, 이번 완화로 판정이
   바뀌지 않음을 확인한다.
4. 시뮬 2건 실행 결과 보고:
   - 양쪽 PickerY를 전진 상태로 두고 X 거리를 150mm 안으로 접근 → **차단되어야 한다.**
   - Front Bottom 검사(Y 전진) 중 Rear가 Y=Avoid 상태로 픽업 진입 → **허용되어야 한다**(이번 완화 목적).
5. 부가 보고: `CanAutoShareProcessWorkAreaWhenOppositeYSafe`라는 이름이 조건 완화 후 실제 조건과
   어긋난다(더 이상 "검사 연속" 전용이 아님). 이름 변경 필요 여부를 의견으로만 제시한다.

---

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과.
2. `git diff --stat` = `PickerZoneInterlockRules.cs` **한 파일만**.
   `RealtimeCollisionSupervisor.cs`, `MotionGuardRuleRegistry.cs`,
   `PickerFrontInterlockRules.cs`, `PickerRearInterlockRules.cs`가 목록에 **없어야 한다.**
3. `git diff` 실제 변경이 **조건 블록 1개 삭제 + 주석 + detail 문구 1줄**뿐인지 확인.
4. `"Input 픽업 영역을 반대 픽커가 사용 중입니다."` 차단이 그대로 남아 있는지 확인.
5. `MoveKind != AxisTeachingMove` 차단과 `IsPickerYOutOrMovingOut` 차단이 그대로인지 확인.
6. `grep -n "BeginMotionGuardBypass" QMC.CDT-320/Equipment/Interlocks/PickerZoneInterlockRules.cs`
   → 0건.
7. 규칙 4의 보고 항목 5개.

## 검증 기준
**시뮬**
1. Front Bottom/Side 검사 진행 중 Rear 픽업 1배치 →
   `"Bottom 작업 영역을 반대 픽커가 사용 중입니다"` 차단이 **0건**, 비상정지 0건.
2. 두 피커가 **동시에 진행**된다(Front Bottom 촬영 ∥ Rear 픽업). 로그 타임스탬프로 겹침 확인.
3. Side 존에 대해서도 동일하게 동작한다(`IsProcessZone`이 Bottom/Side를 포함).
4. **회귀 확인**:
   - Manual 조그/이동으로 같은 상황을 만들면 **여전히 차단**된다.
   - 상대 PickerY가 전진 중이면 **여전히 차단**된다.
   - 두 피커가 동시에 Input 존을 노리면 **여전히 차단**된다(`Input 픽업 영역...`).
5. [확인 요청 1]의 시뮬 2건 결과가 각각 "차단"/"허용"으로 나온다.

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
6. ScalePercent 5%, Auto 1배치. Bottom 존 차단 0건, 비상정지 0건.
7. Front Bottom 촬영과 Rear 픽업이 실제로 겹쳐 진행되는지 눈으로 확인. **두 헤드 간 접촉이나
   이상 접근이 보이면 즉시 정지하고 로그 보존 후 보고.**
8. `PICKER-FACING-X-INTERLOCK`(실시간 감시)이 오동작 없이 유지되는지 확인.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. 이 차단이 단순 `-11` 반환이 아니라 `Critical` / `InterferenceGroup` 알람으로 승격되어 라인
   전체를 세운다. 이번 완화로 이 경로의 차단 자체는 없어지지만, **다른 존 경합에서 같은 승격이
   재현된다.** 정상 경합(대기·재시도로 처리해야 할 상황)을 알람으로 올리는 정책은 별도 재검토가 필요하다.
2. 팔로잉의 중간 좌표가 통과 밴드의 존으로 해석되어 그 존의 점유 규칙에 걸리는 구조는 그대로 남는다.
   근본적으로는 오버라이드/팔로잉 전용 판정 경로(중간 좌표는 위치 기반 안전만 검증)가 정합적이다.
   인터락 구조 변경이므로 승인 후 별도 작업으로 다룬다.
3. 앞선 지시로 마크검사 회피를 픽업 이연·팔로잉에 위임한 뒤 회피 거리가 커져(최대 ~330mm),
   팔로잉이 Bottom/Side 밴드를 통과하는 빈도가 늘었다. 이번 완화가 그 부작용을 덮는 성격도 있음을
   인지하고, 통과 경로 자체를 줄일 수 있는지는 별도 검토 사항이다.
