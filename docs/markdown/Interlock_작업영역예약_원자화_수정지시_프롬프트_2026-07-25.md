# CDT-320: Picker 작업영역 예약 원자화 — 동시 통과(TOCTOU)로 인한 Critical 라인정지 제거 (M8)

## 작업 범위
로컬 폴더 `D:\Source\CDT-320_New` (C# WinForms, .NET Framework, `QMC.CDT-320.sln`) 기준으로만 작업한다.
**수정 파일은 아래 4개뿐이다.**
1. `QMC.CDT-320\Equipment\Interlocks\PickerZoneInterlockRules.cs` — 원자 예약 API **추가만**(기존 판정 로직 무변경)
2. `QMC.CDT-320\Sequencing\Picker\PickerSequenceBase.cs` — 원자 예약 헬퍼 추가
3. `QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.cs` — Output 예약 1곳 교체
4. `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` — Auto Input 예약 2곳 교체 (**[확인 요청 2] 통과 시에만**)

`QMC.Common\*`, `AjinAxis.cs`, `AXM.cs`, SharedRailX, UI, 설정 데이터, `D:\CDT-320\*`는 건드리지 않는다.

> ⚠ 이 작업은 **인터락 판정 규칙을 바꾸지 않는다.** 기존 차단 규칙은 최후 방어선으로 그대로 두고,
> 시퀀스가 그 차단에 **도달하기 전에 순서 대기로 흡수**하도록 예약 절차만 원자화한다.
> 사용자 승인 2026-07-25 (B안).

---

# ★ 작업 규칙 (필수 — 위반 시 작업 중단하고 사용자에게 보고)

### 규칙 1. 공정 속도 구간에는 스케일 미적용 코드를 작성하지 않는다
Auto 운전에서 `Config.DefaultVelocity` / `Config.Acceleration` / `Config.Deceleration`을 보드로
내려보내는 경로는 반드시 `MotionSpeedScale.ApplyDefaultVelocityScale` /
`ApplyDefaultAccelerationScale`을 통과한 값만 사용한다. Config 날값 직접 전달 금지.

### 규칙 2. 속도·가속·감속은 항상 같은 배율로 함께 스케일한다
전례: 커밋 `a690f6a5`, 2026-07-25 RearPickerX 폭주(팔로잉 최초 명령 가감속 20배).
**이번 작업은 모션 코드를 전혀 다루지 않는다.** 자원 예약 절차만 수정한다.

### 규칙 3. 범위 밖에서 스케일 미적용 지점을 발견하면 고치지 말고 보고한다

### ★ 규칙 4. 진행 절차 (이 순서를 지킬 것)
**설계 → 체크리스트 작성 → 구현 → 체크리스트 검증(실패 시 구현→검증 최대 3회 반복) → 레포트.**

### ★ 규칙 5. 보고 의무 (반드시 이행 — 하나라도 빠지면 작업 미완료)
1. 변경 지점 목록 (`파일:행` + 변경 내용).
2. **[확인 요청 1]** 답변 — 자기 재예약(같은 존) 호환성 증명.
3. **[확인 요청 2]** 답변 — Pick 경로 데드락 분석. **데드락 가능이면 Pick 적용 중단하고 보고.**
4. **[확인 요청 3]** 답변 — 락 순서(lock ordering) 분석.
5. 스케일 미적용 잔존 지점 감사 결과
   (`grep -rn "Config\.DefaultVelocity\|Config\.Acceleration\|Config\.Deceleration" --include=*.cs QMC.CDT-320 QMC.Common`).
   없으면 "없음" 명시.
6. 판단이 애매해서 손대지 않은 지점. 없으면 "없음" 명시.

---

# 배경

## 선행 작업 (이미 적용 완료 — 재작업 금지)
동시 운전 정책이 커밋 `b1641257`(M2), `81a8d7d8`(M1), `5332a26c`(M3), `d81cddc7`(M4),
`6e456a86`(M6), `b838c1fa`(M7)로 적용되어 있다. 정책은 다음과 같다.

| 나 \ 상대 | Pick | Bottom/Side 검사 | Place |
|---|---|---|---|
| **Pick** | ❌ 절대 금지 | ✅ 허용 | ✅ 허용 |
| **검사** | ✅ 허용 | ❌ 절대 금지 | ✅ 허용 |
| **Place** | ✅ 허용 | ✅ 허용 | ❌ 절대 금지 |

Bottom 검사와 Side 검사는 **동일 작업**으로 취급한다(`NormalizeInterlockZone`이 Process 존으로 정규화).

## 이번에 고치는 결함 — 확인과 등록이 분리되어 있다
`PickerZoneInterlockRules.BeginPickerWorkAreaUse`(현재 `:487`)는 **상대 점유를 확인하지 않고
카운터만 증가**시키는 함수다. 실패를 반환하지 않는다.

```csharp
public static IDisposable BeginPickerWorkAreaUse(bool isFront, PickerWorkZone zone, string owner)
{
    zone = NormalizeInterlockZone(zone);
    lock (activeZoneLock)
    {
        AddPickerWorkAreaUse(isFront, zone, owner);   // ← 확인 없이 등록만
    }
    return new PickerWorkAreaScope(isFront, zone);
}
```

그래서 시퀀스는 "① 상대 점유 확인(대기 게이트) → ② 등록 → ③ 축 이동" 3단계로 동작하는데,
①과 ② 사이에 락이 없어 Front/Rear가 **둘 다 ①을 통과하고 둘 다 ②를 등록**할 수 있다.
그러면 ③에서 서로의 X 이동이 인터락에 걸려 **양쪽 모두 -11 → Critical 승격 → 라인 전체 정지**가 된다.

- Place 경로: ①=`WaitOppositePickerOutputWorkAreaClearAsync`(M4), ②=`EnsurePickerWorkAreaReserved(Output, "Place")`.
  두 지점이 같은 메서드 안에 인접해 있어 창이 짧다.
- Pick 경로: ①=`WaitOppositePickerNotInInputPickAreaAsync`(스텝 `MoveOppositePickerToAvoidForPickerMove`),
  ②=`EnsurePickerWorkAreaReserved(Input, "PickUp")`(스텝 `MovePickerXStageYPickerT`).
  **두 스텝 사이에 실제 축 이동이 여럿 있어 창이 수백 ms~수 초로 훨씬 크다.**

## 현재 실제 발생 확률은 낮다 (그래도 고치는 이유)
동시 Pick/동시 Place는 이 창 앞에서 이미 두 겹으로 직렬화된다.
- `PickerPhaseCoordinator.TryEnter`(`:21`)가 `lock (_gate)` 안에서 판정+상태변경을 원자적으로 수행 →
  phase 매트릭스(`IsAllowedNoLock`, `:136~`)가 동시 Pick/동시 Place phase 진입을 막는다.
- Place는 추가로 `SequenceResourceKind.OutputPlaceArea` 전역 lease(1개)를 먼저 획득해야 예약 지점에 도달한다.

따라서 이번 수정은 **급한 화재 진압이 아니라 보험**이다. 목적은 두 가지다.
1. phase 매트릭스를 우회하는 경로(수동 조작, 복구 시퀀스, Place의 handoff처럼 존/lease를 해제했다
   재획득하는 흐름)에서도 Critical 정지가 나지 않게 한다.
2. 향후 상위 직렬화가 느슨해지는 변경이 생겨도 "차단→Critical"이 아니라 "대기→순서 진행"으로 흡수한다.

**따라서 이 작업으로 기존 동작이 바뀌는 것이 눈에 보이지 않는 것이 정상이다.**
정상 흐름에서는 새 대기 로그가 찍히지 않아야 한다(찍히면 상위 직렬화에 이미 구멍이 있다는 신호이므로 보고 대상).

---

# 수정 내용

> 아래 행 번호는 M1~M7 적용 후 기준이며 이동했을 수 있다. **행 번호가 아니라 심볼명으로 찾을 것.**

## 수정 A. 원자 예약 API 추가 — `PickerZoneInterlockRules.cs`

`BeginPickerWorkAreaUse`(`:487`) **바로 아래에 추가한다. 기존 함수는 그대로 남긴다**
(다른 호출부가 있고, 검사 경로는 계속 이 함수를 쓴다).

```csharp
        // 인터락 항목(사용자 승인 2026-07-25, M8): 상대 Picker가 같은 작업영역을 점유하고 있지 않을 때만
        //   점유를 등록한다. 확인과 등록을 하나의 activeZoneLock 안에서 수행해 원자적으로 만든다.
        // 기존 조건: BeginPickerWorkAreaUse는 확인 없이 등록만 했고, 시퀀스가 "대기 게이트 → 예약"을
        //   두 단계로 나눠 수행했다. 그 사이에 락이 없어 Front/Rear가 동시에 게이트를 통과하면 둘 다
        //   등록되고, 이후 X 이동이 서로의 점유 때문에 인터락 -11로 차단되어 Critical 승격 → 라인 정지가 됐다.
        // 현재 기준: 예약 자체가 상호배제를 보장한다. 먼저 락을 잡은 쪽이 예약에 성공하고, 늦은 쪽은
        //   null을 받아 시퀀스에서 순서 대기한다(차단이 아니라 대기).
        // 반환: 성공 시 점유 스코프(Dispose로 해제), 실패 시 null.
        // 주의: 같은 측(자기) Picker가 이미 같은 존을 점유 중인 경우는 상대 점유가 아니므로 성공이다
        //   (중복 등록 = 카운터 증가). 자기 재예약 호환성은 EnsurePickerWorkAreaReserved 쪽에서 유지된다.
        public static IDisposable TryBeginPickerWorkAreaUseExclusive(
            bool isFront,
            PickerWorkZone zone,
            string owner,
            out string occupiedOwner)
        {
            occupiedOwner = string.Empty;
            // 현재 기준: INSPECT_B/INSPECT_S 작업 점유는 같은 Process 존 점유로 관리한다.
            zone = NormalizeInterlockZone(zone);
            lock (activeZoneLock)
            {
                if (IsPickerWorkAreaActive(!isFront, zone, out occupiedOwner))
                    return null;

                AddPickerWorkAreaUse(isFront, zone, owner);
            }

            return new PickerWorkAreaScope(isFront, zone);
        }
```

구현 요건:
- `IsPickerWorkAreaActive`(private, 존별 점유 판정)와 `AddPickerWorkAreaUse`(private, 카운터 증가),
  `PickerWorkAreaScope`(내부 클래스), `activeZoneLock`, `NormalizeInterlockZone`을 **그대로 재사용**한다.
  새 판정 로직을 만들지 말 것.
- `IsPickerWorkAreaActive`가 `lock` 없이 호출되는 private 함수인지 확인하고, 기존 사용례
  (`IsOtherPickerWorkAreaActive`, M4에서 추가된 `IsPickerWorkAreaZoneActive`)와 동일하게
  **`lock (activeZoneLock)` 안에서만** 호출한다.
- **기존 `BeginPickerWorkAreaUse` / `BeginInputPickAreaUse` / `IsPickerWorkAreaZoneActive` /
  `TryGetPickerWorkArea` 본문은 무변경.**

## 수정 B. 원자 예약 헬퍼 추가 — `PickerSequenceBase.cs`

`EnsurePickerWorkAreaReserved`(현재 `:2599`) 아래에 추가한다. **기존 메서드는 무변경으로 남긴다**
(검사 시퀀스 등 다른 호출부가 계속 사용).

```csharp
        // 현재 기준(사용자 승인 2026-07-25, M8): 상대 Picker가 같은 작업영역을 점유하지 않을 때만
        //   원자적으로 예약한다. 성공하면 true, 상대 점유로 실패하면 false와 점유자를 돌려준다.
        //   기존 EnsurePickerWorkAreaReserved와 달리 실패를 삼키지 않고 호출부가 대기할 수 있게 한다.
        // 자기 재예약 호환: 이미 같은 존 스코프를 보유하면 그대로 true(기존 동작과 동일, no-op).
        protected bool TryReservePickerWorkAreaExclusive(
            PickerWorkZone zone,
            string description,
            out string occupiedOwner)
        {
            occupiedOwner = string.Empty;
            try
            {
                if (zone == PickerWorkZone.Unknown || zone == PickerWorkZone.Avoid)
                    return true;

                if (pickerWorkAreaScope != null && pickerWorkAreaZone == zone)
                    return true;

                // 다른 존 스코프를 들고 있으면 기존 관례대로 먼저 해제한다.
                ReleasePickerWorkArea();

                IDisposable scope = PickerZoneInterlockRules.TryBeginPickerWorkAreaUseExclusive(
                    Side == PickerSequenceSide.Front,
                    zone,
                    Name + ":" + description,
                    out occupiedOwner);
                if (scope == null)
                    return false;

                pickerWorkAreaScope = scope;
                pickerWorkAreaZone = zone;

                WriteLog("PickerWorkArea",
                    Name + " reserved picker work area exclusively. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                // 예약 예외는 fail-closed(대기)로 처리한다 — 기존 Ensure*는 예외를 삼키고 진행했으나
                // 원자 예약은 "성공 확인"이 목적이므로 실패로 간주해 호출부가 대기·재시도하게 한다.
                occupiedOwner = "예약 예외: " + ex.Message;
                WriteLog("PickerWorkArea",
                    Name + " exclusive picker work area reservation failed. side=" + Side +
                    ", zone=" + zone +
                    ", description=" + description +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }
```

- `pickerWorkAreaScope` / `pickerWorkAreaZone` 필드(현재 `:40~41`)와 `ReleasePickerWorkArea`(`:2635`)를
  그대로 사용한다. 필드 접근 수준을 바꾸지 말 것.

## 수정 C. Place 경로 교체 — `PickerPlaceSequence.cs`

`MoveOutputStageReceivePositionAsync` 안의 **M4 대기 게이트 호출 + `EnsurePickerWorkAreaReserved(Output, "Place")`
두 문장을 "원자 예약 성공까지 대기하는 루프" 하나로 합친다.**

### 변경 전 (현재 구조)
```csharp
            int oppositeOutputClear = await WaitOppositePickerOutputWorkAreaClearAsync(ct).ConfigureAwait(false);
            if (oppositeOutputClear != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("상대 Picker Output 점유 대기 실패 정리", ct).ConfigureAwait(false);
                return oppositeOutputClear;
            }

            EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "Place");
```

### 변경 후
```csharp
            int oppositeOutputClear = await ReserveOutputWorkAreaExclusiveWithWaitAsync(ct).ConfigureAwait(false);
            if (oppositeOutputClear != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Output 작업영역 원자 예약 실패 정리", ct).ConfigureAwait(false);
                return oppositeOutputClear;
            }
```

기존 `WaitOppositePickerOutputWorkAreaClearAsync`는 **삭제하고** 아래 메서드로 대체한다
(대기 루프 구조·로그·CycleStop 처리·비Auto 즉시 실패 관례를 그대로 계승할 것).

```csharp
        // 현재 기준(사용자 승인 2026-07-25, M8): 확인과 예약을 원자화한다.
        //   기존 조건: 상대 점유 확인(대기)과 예약이 분리되어 있어 양쪽이 동시에 게이트를 통과하면
        //             둘 다 예약되고 이후 X 이동이 인터락 -11 → Critical로 라인을 세웠다.
        //   현재 기준: TryReservePickerWorkAreaExclusive가 성공할 때까지 대기한다. 성공 = 그 순간
        //             상대 미점유가 보장된 상태이므로 이후 X 진입이 점유 경합으로 차단되지 않는다.
        // 동시 Place 금지 정책은 유지된다(늦은 쪽은 예약 실패 → 대기 → 순서 진행).
        // 계층 관계: 1차 직렬화는 OutputPlaceArea 자원 lease와 PickerPhaseCoordinator의 Place/Place
        //   차단이며, 이 예약은 그 뒤의 방어 계층이다. 정상 흐름에서는 첫 시도에 성공해
        //   Wait 로그가 찍히지 않는 것이 정상이다(찍히면 상위 직렬화 구멍 신호 → 보고 대상).
        private async Task<int> ReserveOutputWorkAreaExclusiveWithWaitAsync(CancellationToken ct)
```

요건:
- 첫 시도 실패 + **비Auto**(`Options == null || Options.RunMode != SequenceRunMode.Auto`)면
  기존과 동일하게 즉시 `Fail("PICKER-OPPOSITE-OUTPUT-ZONE", ...)`.
- Auto면 `while (true)` 루프: `ct.ThrowIfCancellationRequested()` →
  `Context.StopIfCycleStopRequested(Name + ".WaitOppositePickerOutputClearBeforePlace", ShouldDeferCycleStopForPickerDrain(), "Picker Place drain")` →
  재시도 → 실패 시 1초 스로틀 `- Wait` 로그 → `await Task.Delay(10, ct)`.
- `catch (OperationCanceledException) { throw; }` / `catch (SequenceStopException) { throw; }` /
  `catch (Exception ex) { return Fail(...); }` 구조 유지.

## 수정 D. Pick 경로 교체 — `PickerPickUpSequence.cs` (**[확인 요청 2] 통과 시에만**)

Auto 예약 2곳을 원자 예약 루프로 교체한다.
- 일반 이송: `EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUp")`
  (메서드 `MovePickerXStageYPickerTByDefaultAsync`, 현재 `:2346`)
- Conti: `EnsurePickerWorkAreaReserved(PickerWorkZone.Input, "PickUp ContiNode")` (현재 `:2535`)

대기 루프는 수정 C와 같은 구조로 만들되 **PickUp 관례를 따른다.**
- CycleStop: `Context.StopIfCycleStopRequested(Name + ".WaitInputWorkAreaExclusive", ShouldDeferCycleStopForPickUpDrain(), "PickUp batch drain")`
- 비Auto 즉시 실패 코드: `PICKER-OPPOSITE-INPUT-ZONE` (기존 `WaitOppositePickerNotInInputPickAreaAsync`와 동일 계열)
- 기존 물리 대기 게이트 `WaitOppositePickerNotInInputPickAreaAsync`(스텝 `MoveOppositePickerToAvoidForPickerMove`)는
  **그대로 유지한다.** 그것은 상대의 *물리 위치*를 보는 게이트이고, 이번 수정은 *논리 점유* 예약이다. 둘 다 필요하다.
- **수동 경로(현재 `:4624` `ManualSelectedPickUp`, `:5076` `ManualSelectedPickUpPrepare`/`ManualPreparedPickZ`)는
  무변경.** 수동은 대기하지 않고 즉시 실패가 기존 규약이다.

---

## 변경 금지
- **인터락 판정 규칙 전체.** 특히 `PickerZoneInterlockRules`의
  `VerifyPickerXMove` / `VerifyPickerYMove` / `VerifyFacingYDistanceFirst` /
  `CanAutoShareProcessWorkAreaWhenOppositeYSafe` / `IsPickerYOutOrMovingOut` /
  `ResolvePickerZoneTransportState` / 존 밴드 판정 / `ParseZone`.
  **차단 규칙을 완화해서 해결하려 하지 말 것.**
- `RealtimeCollisionSupervisor.cs`, `MotionGuardRuleRegistry.cs` 등록 순서, `SharedRailXInterlockRules.cs`.
- `PickerPhaseCoordinator.cs` — phase 매트릭스와 `TryEnter`/`TryTransition`/`Exit`. 무변경.
- 기존 `BeginPickerWorkAreaUse` / `BeginInputPickAreaUse` / `TryGetPickerWorkArea` /
  `IsPickerWorkAreaZoneActive` / `EnsurePickerWorkAreaReserved` / `ReleasePickerWorkArea` 본문.
- 검사 시퀀스(`PickerBottomInspectionSequence` / `PickerSideInspectionSequence` /
  `PickerBottomAndSideInspectionSequence`)의 예약 — `InspectionArea` 세마포어가 이미 원자적으로
  직렬화하므로 손대지 않는다.
- `SequenceResourceManager` / `SequenceResourceKind` / lease 획득 순서.
- `WaitOppositePickerReadyForAutoAsync`(M1/M7 적용분) — 무변경.
- `BeginMotionGuardBypass` 등 우회 API 사용 절대 금지.

---

## ★ [확인 요청 1] — 자기 재예약 호환성 증명 (필수)
1. `EnsurePickerWorkAreaReserved`는 "이미 같은 존 스코프 보유 시 no-op"이다. 새 헬퍼도 동일하게
   동작하는지 코드로 확인하라. **자기가 이미 점유 중인데 실패를 반환하면 대기 루프가 영구 정체된다.**
2. 같은 시퀀스 인스턴스가 같은 존을 두 번 예약하는 경로(예: PickUp 일반 → Conti 재진입)가 있는지 찾고,
   그때 새 헬퍼가 첫 분기(`pickerWorkAreaScope != null && pickerWorkAreaZone == zone`)로 통과하는지 확인하라.
3. **다른 시퀀스 인스턴스**가 같은 측 Picker로 같은 존을 점유한 경우(예: PickUp 시퀀스가 Input 점유 중
   다른 시퀀스가 Input 예약)에 새 API가 성공하는지 확인하라. `IsPickerWorkAreaActive(!isFront, ...)`는
   **상대측만** 보므로 성공해야 정상이다(기존 동작과 동일).

## ★ [확인 요청 2] — Pick 경로 데드락 분석 (필수, 위험 항목)
**Pick 예약 대기를 넣으면 "자원을 쥔 채 대기"가 된다.** 아래를 분석하고,
**데드락 가능성이 있으면 수정 D를 적용하지 말고 즉시 보고하라.**
1. `EnsurePickerWorkAreaReserved(Input, "PickUp")` 시점에 이 시퀀스가 보유 중인 자원을 전부 나열하라
   (`SequenceResourceKind.InputStageArea` lease 등 — `AcquireResourceAsync` 호출부 추적).
2. 상대 Picker가 Pick을 완료해 Input 점유를 해제하려면 그 자원들 중 하나가 필요한지 확인하라.
   필요하다면 **순환 대기(데드락)** 다.
3. `PickerPhaseCoordinator`의 phase 매트릭스가 동시 PickUp phase 진입을 막으므로
   "두 픽커가 동시에 Input 예약을 시도하는 상태" 자체가 성립 가능한지 판정하라.
   성립 불가면 수정 D는 순수 방어 계층이며 데드락도 발생하지 않는다.
4. Auto 배치 중 Conti 재진입 경로에서 Input 점유가 배치 내내 유지되는지, 아니면 다이마다
   해제·재예약되는지 확인하라. 배치 내내 유지되면 상대는 배치가 끝날 때까지 대기하게 되므로
   **처리량 손실**을 보고하라(정책 위반은 아니지만 사용자 판단 필요).

## ★ [확인 요청 3] — 락 순서(lock ordering) 분석 (필수)
1. 새 API는 `activeZoneLock`을 잡는다. 이 락을 잡은 상태에서 다른 락
   (`PickerPhaseCoordinator._gate`, `SequenceResourceManager` 내부 락, 축/유닛 락)을
   **획득하지 않음**을 확인하라(`IsPickerWorkAreaActive` / `AddPickerWorkAreaUse` 내부 호출 추적).
2. 역방향(다른 락을 쥔 채 `activeZoneLock`을 잡는 경로)이 있는지 확인하고, 있으면 락 순서가
   일관적인지 판정하라. 교차 순서가 발견되면 즉시 보고하라.
3. 새 API가 `await`을 락 안에서 수행하지 않음을 확인하라(동기 블록 안에서만 락 사용).

---

# ★ 인터락 사전 점검 (프롬프트 작성 시 조사 완료 — 재확인만)

이번 수정은 **모션 인터락 판정을 통과하는 방식을 바꾸지 않는다.** 예약 성공 시점이
"상대 미점유 보장" 상태이므로, 기존에 통과하던 인터락은 그대로 통과한다.

| 규칙 | 영향 |
|---|---|
| `VerifyFacingYDistanceFirst` (레지스트리 1순위, X 150mm + 한쪽 Y 정확 Avoid) | 무영향 — 물리 판정, 예약과 무관 |
| `SharedRailXInterlockRules.Verify` (페어 간격) | 무영향 |
| `VerifyPickerXMove` 의 Input 점유 차단(`Input 픽업 영역을 반대 픽커가 사용 중입니다`) | **도달 빈도 감소** — 예약 단계에서 대기로 흡수 |
| `VerifyPickerXMove` 의 Process 존 공유 판정(M3 포함) | 무영향 — Bottom/Side 대상, 이번 수정은 Input/Output |
| Output 점유 경합 차단 | **도달 빈도 감소** — 동일 |
| `RealtimeCollisionSupervisor` | 무영향 |

**예상 부작용 없음.** 단, 새 대기가 생기므로 [확인 요청 2]의 데드락 검증이 통과해야 한다.

---

## 작업 후 확인 사항
1. `QMC.CDT-320.sln` 빌드 통과, **경고 수 증가 없음**(현재 기준 44개).
2. `git diff --stat`이 위 4개 파일(또는 [확인 요청 2] 실패 시 3개)만 포함.
   `PickerPhaseCoordinator.cs`, `RealtimeCollisionSupervisor.cs`, `MotionGuardRuleRegistry.cs`,
   `PickerFrontInterlockRules.cs`, `PickerRearInterlockRules.cs`, `QMC.Common/*`가 목록에 **없어야 한다.**
3. `PickerZoneInterlockRules.cs`의 diff가 **새 메서드 추가뿐**이고 기존 메서드 본문 변경이 0인지 확인.
4. `grep -n "BeginMotionGuardBypass" QMC.CDT-320\Sequencing\Picker\*.cs` → 0건.
5. 기존 `EnsurePickerWorkAreaReserved` 호출부가 검사 시퀀스/수동 경로에 그대로 남아 있는지 확인.
6. 새 대기 루프 전부에 ① `ct.ThrowIfCancellationRequested()` ② `StopIfCycleStopRequested`
   ③ 1초 스로틀 로그 ④ `Task.Delay(10, ct)`가 있는지 확인.
7. 규칙 5의 보고 항목 6개.

## 검증 기준

**정적 검증 (필수)**
1. 원자 API가 `lock (activeZoneLock)` 안에서 "상대 확인 → 자기 등록"을 수행하며, 그 사이에
   락 해제·`await`·다른 락 획득이 없음을 코드로 제시.
2. 예약 실패 경로가 **등록을 하지 않고** null을 반환함(부분 등록 없음)을 확인.
3. 성공 경로에서 반환된 스코프가 `ReleasePickerWorkArea`로 정상 해제되고, 시퀀스 종료/Abort/예외
   경로(`ExecuteAsync` finally, `Abort`)에서 반드시 해제에 도달함을 추적해 보고.

**시뮬 검증**
4. Auto + Conti 1배치 — 정상 진행. **새 `- Wait` 로그가 찍히지 않는 것이 정상**
   (찍히면 상위 직렬화 구멍이므로 그 로그 전문을 보고).
5. 회귀: 동시 Pick / 동시 Bottom·Side 검사 / 동시 Place가 **여전히 발생하지 않음**.
6. 회귀: Pick ∥ 검사, Pick ∥ Place, 검사 ∥ Place 동시 진행이 **여전히 동작**(로그 타임스탬프 겹침 확인).
7. 강제 경합 테스트 — 양쪽 Place(또는 양쪽 Pick)를 근접 타이밍에 유도했을 때
   늦은 쪽이 `- Wait` 후 순서대로 진행하고 **`Critical`/`InterferenceGroup` 알람 0건**.
8. Cycle Stop을 새 대기 중에 눌러 즉시 탈출되는지 확인(무한 대기 없음).
9. 비Auto(수동)에서 같은 상황을 만들면 대기 없이 즉시 실패하는지 확인.

**실장비 (사용자 실행 — 작업자는 문서로만 제공)**
10. ScalePercent 5%, Auto 1배치. Critical 0건, 비상정지 0건, 처리량 저하 없음.
11. `PICKER-FACING-X-INTERLOCK`(실시간 감시)이 오동작 없이 유지되는지 확인.

## 알려진 잔여 사항 (이번 범위 아님 — 보고만)
1. 정상 경합을 `Critical`/`InterferenceGroup`으로 승격시켜 라인을 세우는 알람 severity 정책은
   별도 결정 사항이다. 이번 수정은 예약 단계에서 경합을 흡수해 그 경로에 **도달하지 않게** 하는 것이며,
   다른 존 경합에서 같은 승격이 재현될 수 있다.
2. `TryGetPickerWorkArea`는 Input→Process→Output 우선순위로 첫 활성 존 하나만 반환해 다중 점유가
   가려진다(마스킹). M4에서 추가한 `IsPickerWorkAreaZoneActive`(존 지정 조회)로 대체하는 것이
   정합적이나, `WaitOppositePickerReadyForAutoAsync` 등 기존 호출부 교체는 별도 승인 사항이다.
3. `RealtimeCollisionSupervisor`의 "EStop 정지거리 + 지연 < 150mm" 정량 근거가 코드/설정에 없다.
   동시 운전 정책 하에서는 최대 X 속도 기준 정지거리 실측이 필요하다(사용자 확인 사항).
4. M3의 자기 Y 후퇴 판정은 Home(0)도 후퇴로 인정하는데 `VerifyExactPickerYAvoidForFacingMove`는
   정확한 teaching Avoid만 인정한다. 기준 불일치로 Y=0 상태의 밴드 통과가 X 간격 150mm 안에서는
   여전히 차단될 수 있다(fail-closed, 안전엔 무해).

---

# 체크리스트

## 설계
- [ ] `IsPickerWorkAreaActive` / `AddPickerWorkAreaUse` / `PickerWorkAreaScope` / `activeZoneLock` /
      `NormalizeInterlockZone`의 현재 시그니처와 접근 수준 확인
- [ ] `EnsurePickerWorkAreaReserved` / `ReleasePickerWorkArea` / `pickerWorkAreaScope` / `pickerWorkAreaZone` 확인
- [ ] Place 예약 지점 1곳, Pick Auto 예약 지점 2곳, Pick 수동 예약 지점 2곳 위치 재확인(행 번호 갱신)
- [ ] [확인 요청 2] 데드락 분석 완료 → 수정 D 적용 여부 결정
- [ ] [확인 요청 3] 락 순서 분석 완료

## 구현
- [ ] A: `TryBeginPickerWorkAreaUseExclusive` 추가 (기존 메서드 무변경)
- [ ] B: `TryReservePickerWorkAreaExclusive` 추가 (자기 재예약 no-op 유지, 예외 시 fail-closed)
- [ ] C: Place — M4 게이트+예약을 원자 예약 대기 루프로 통합, 구 게이트 메서드 삭제
- [ ] D: Pick — Auto 2곳 교체 (수동 2곳 무변경, 물리 대기 게이트 유지) ※확인 요청 2 통과 시
- [ ] 모든 새 대기 루프에 ct / CycleStop / 스로틀 로그 / Delay(10) 포함

## 검증 (실패 시 구현→검증 최대 3회 반복)
- [ ] 빌드 통과, 경고 수 증가 없음
- [ ] diff 격리 확인 (금지 파일 0건)
- [ ] `PickerZoneInterlockRules.cs` diff가 추가만인지 확인
- [ ] 정적 검증 1~3 (원자성 / 부분 등록 없음 / 해제 도달성)
- [ ] 시뮬 4~9 (정상 무대기 / 3대 금지 유지 / 3대 허용 유지 / 강제 경합 / CycleStop / 비Auto)
- [ ] 규칙 5의 보고 항목 6개 작성

## 레포트
- [ ] 변경 지점 목록 (파일:행)
- [ ] [확인 요청 1~3] 답변
- [ ] 스케일 미적용 감사 결과
- [ ] 애매해서 손대지 않은 지점
- [ ] 실장비 확인 리스트 (사용자용)
