# PickerZoneInterlockRules.cs 완전 분석

**경로**: `QMC.CDT-320\Equipment\Interlocks\PickerZoneInterlockRules.cs`
**규모**: 4005줄 / 약 130개 함수 / `namespace QMC.CDT320.Interlocks`
**구성**: `enum PickerWorkZone` + `class PickerZoneTransportState`(상태 DTO) + `static class PickerZoneInterlockRules`(본체) + 내부 IDisposable 스코프 2개(`ActiveZoneScope`, `PickerWorkAreaScope`)

## 0. 핵심 도메인 개념 (존)

`PickerWorkZone` enum (8~16행): `Unknown, Avoid, Input, Bottom, Side, Output`
- **Input** = 픽업(웨이퍼 픽), **Bottom/Side** = 검사(Process, 인터락상 하나로 정규화), **Output** = 플레이스, **Avoid** = 안전 대피
- `NormalizeInterlockZone`는 Bottom/Side를 모두 **Bottom(=Process 대표)**로 합침. 즉 인터락은 검사 두 존을 한 존으로 취급.
- Front/Rear 두 픽커가 공유 X레일에서 마주 보므로, 양쪽 PickerY가 동시에 "전진(돌출)" 상태이고 X거리가 안전거리 안이면 충돌 위험 → 이 파일의 최상위 목적.

---

## 1. Public/Internal 진입 함수 목록과 호출자

MotionGuard 규칙 시그니처(`bool Rule(MotionGuardRuleContext, out string reason)`)를 가진 것은 ★로 표시.

| 함수(줄) | 종류 | 주요 호출자 |
|---|---|---|
| ★`VerifyFacingYDistanceFirst` (1104) | MotionGuard 규칙 | `MotionGuardRuleRegistry.cs:129` — **레지스트리 최초 등록**(모든 축/실린더 이동 전 1차 호출) |
| `VerifyPickerXGlobalMachineClearance` (140) | 공용 검증 | `MachineController.cs:1690`, `PickerFrontInterlockRules.cs:41`, `PickerRearInterlockRules.cs:41` |
| `VerifyPickerYHomePairSafety` (272) | 공용 검증 | `PickerFrontInterlockRules.cs:957`, `PickerRearInterlockRules.cs:933` |
| `VerifyPickerXStoppedForClearanceMechanismMove` (372) | 공용 검증 | `InputFeederInterlockRules.cs:22,31`, `OutputFeederInterlockRules.cs:21,30`, `InputStageInterlockRules.cs:24`, `OutputStageInterlockRules.cs:22` |
| `BeginPickerZoneMove` (415) | 점유 스코프 | `PickerFrontUnit.cs:1461,1527,1742,1804,1892`, `PickerRearUnit.cs:1227,1293,1454,1516,1604` |
| `BeginInputPickAreaUse` (437) | 점유 스코프 | (파일 내 wrapper, 외부 직접 호출 없음 — `BeginPickerWorkAreaUse`로 통합됨) |
| `BeginPickerWorkAreaUse` (443) | 점유 스코프 | `PickerBottomAndSideInspectionSequence.cs:3227,3238`, `VisionFocusScanSequence.cs:1342` |
| `TryGetPickerWorkArea` (456) | 조회 | `MotionGuardRuleHelpers.cs:281,343`, `FrontPickerSequence.cs:401`, `RearPickerSequence.cs:408`, `OutputStageSequenceBase.cs:336`, `PickerPickUpSequence.cs:2790`, `PickerWorkInfoPageRuntime.cs:1582` |
| `ClearPickerWorkAreasForReadyIfSafe` (505) | 해제 | `AutoSequenceCoordinator.cs:285`, `MachineReadySequence.cs:121` |
| `GetPickerYActiveTargetZone` (556) | 조회 | `PickerPickUpSequence.cs:2803` |
| `GetPickerCurrentXZone` (562) | 조회 | `FrontPickerSequence.cs:407`, `RearPickerSequence.cs:414` |
| `GetPickerXZoneByPosition` (568) | 조회 | (외부 직접 호출 미검출 — API 노출용) |
| `IsProcessZone` (574) internal | 판정 | `FrontPickerSequence.cs:410`, `RearPickerSequence.cs:417`, `PickerSequenceBase.cs:1557` |
| `NormalizeInterlockZone` (580) internal | 정규화 | `AutoSequenceCoordinatorGate.cs:303,420,558,621`, `PickerProcessSequence.cs:2692,2791,2792` |
| `IsSameInterlockZone` (587) internal | 판정 | `InputStageInterlockRules.cs:2044,2094,2095`, `MachineController.cs:3646,3647`, `AutoSequenceCoordinatorGate.cs:503,524,559,566`, `PickerProcessSequence.cs:2793,2794` |
| `CanMovePickerAxisByFacingYInterlock` (596) | 공용 검증 | `PickerProcessSequence.cs:1784`, `PickerSequenceBase.cs:1269,1542` + 내부 `VerifyFacingYDistanceFirst` |
| `CanMovePickerXPairByFacingYInterlock` (644) | 공용 검증 | `SharedRailXMotionService.cs:403` (그룹 X 이동) |
| `VerifyPickerZAtOrAboveZeroForZoneStageZMove` (712) | 공용 검증 | `OutputStageInterlockRules.cs:335`, `InputStageInterlockRules.cs:399` |
| `IsPickerBlockingZoneTransport` (811, 864 오버로드) | 판정 | Input/OutputFeederInterlockRules, Feeder/Stage SequenceBase, `AutoSequenceCoordinatorGate.cs:593,600`, `InputDieVisionPrepareSequence` 등 다수 |
| `IsPickerBlockingZoneTransportForFeederHome` (821) | 판정 | `InputFeederInterlockRules.cs:268,275`, `OutputFeederInterlockRules.cs:270,277` |
| `ResolvePickerZoneTransportState` (932) | 상태 계산 | InputStage/OutputStageInterlockRules, `MachineController.cs:3618`, `PickerProcessSequence.cs:1257,2779`, `PickerPickUpSequence.cs:2794`, `InputDieVisionPrepareSequence.cs:1021` |
| `ResolvePickerPhysicalZoneName` (988) | 조회 | `InputStageInterlockRules.cs:1921`, `PickerWorkInfoPageRuntime.cs:1560` |
| ★`VerifyFrontPickerXMove`/★`VerifyRearPickerXMove` (1004/1014) | 규칙 | `PickerFrontInterlockRules.cs:538`, `PickerRearInterlockRules.cs:180` |
| ★`VerifyFrontPickerYMove`/★`VerifyRearPickerYMove` (1024/1034) | 규칙 | `PickerFrontInterlockRules.cs:383`, `PickerRearInterlockRules.cs:762` |
| ★`VerifyFrontPickerYJogFacingMove`/★`VerifyRearPickerYJogFacingMove` (1044/1054) | 규칙 | `PickerFrontInterlockRules.cs:380`, `PickerRearInterlockRules.cs:746` |
| `VerifyPickerXOppositeYClearance` (2053) | 공용 검증 | `PickerFrontInterlockRules.cs:598,634`, `PickerRearInterlockRules.cs:238,274` |
| `CanShareForwardY` (2400) | 판정 | 내부 `VerifyPickerYMove`에서 사용 |
| `ResolveManualPickerXTargetZone` (2655) internal | 조회 | `PickerFrontInterlockRules.cs:508,509`, `PickerRearInterlockRules.cs:150,151` |
| `IsManualPickerXProcessZone` (2676) internal | 판정 | `PickerFrontInterlockRules.cs:531,532`, `PickerRearInterlockRules.cs:173,174` |

`PickerZoneTransportState`의 public 프로퍼티(`IsRequestedZoneActive`, `WorkAreaBlocksTransport`, `IsWorkAreaPhysicallyClearForTransport`, `BlocksTransport`, `Describe()`)는 `ResolvePickerZoneTransportState` 반환값을 통해 외부에서 직접 참조.

---

## 2. 핵심 판정 함수 상세

### 2-1. `VerifyFacingYDistanceFirst` (1104~1157) — 최상위 진입점
```csharp
if (!IsAxisMotionRequest(request.MoveKind)) return true;            // 축이동/홈/티칭/연속조그/스텝조그만 대상
if (!TryResolvePickerXYRequest(...isFront, axis, movingName)) return true; // Front/Rear Picker X/Y 아니면 통과
bool allowed = CanMovePickerAxisByFacingYInterlock(
    request.Machine, isFront, axis, request.TargetValue, request.TargetName,
    null, null, out detail);
if (allowed) return true;
return MotionGuardRuleHelpers.Block(movingName,
    movingName + " 1차 거리 인터락 차단: Front/Rear PickerX 거리와 PickerY 돌출 상태가 안전하지 않습니다. ...", out reason);
```
- **검사 대상 축**: FrontPickerX/FrontPickerY/RearPickerX/RearPickerY
- **MoveKind 필터**: AxisMove, AxisHome, AxisTeachingMove, AxisContinuousJog, AxisStepJog
- 축별로 X면 `CanMovePickerXByFacingYInterlock`, Y면 `CanMovePickerYByFacingYInterlock`로 분기

### 2-2. `CanMovePickerYByFacingYInterlock` (1912~1986) — Y 전진 마주봄 검사
```csharp
if (IsPickerYRecoveryMove(machine, isFront, currentY, targetY)) return true;  // Home/Avoid로 줄어드는 복귀는 허용
bool ownMovingForward = IsPickerYForwardDirection(isFront, currentY, targetY); // Front:+Y, Rear:-Y가 전진
bool ownTargetOut = IsPickerYOutByPosition(machine, isFront, targetY);
if (!ownTargetOut && !ownMovingForward) return true;                          // 목표가 안전위치+비전진이면 허용
bool otherOut = IsPickerYOutOrMovingOut(machine, otherFront, null);           // 상대 Y가 돌출 아니면 허용
if (!otherOut) return true;
double clearance = ResolvePickerYFacingXClearance(machine);                   // Config 안전거리
double ownXTarget = pairedXTarget ?? ResolveAxisPathTarget(ownX);
double otherXTarget = ResolveAxisPathTarget(otherX);
if (!DoXMovePathsEnterFacingClearance(ownX.ActualPosition, ownXTarget,
        otherX.ActualPosition, otherXTarget, clearance)) return true;         // X경로가 안전거리 밖이면 허용
detail = BuildFacingYBlockedDetail(...); return false;                        // 양쪽 Y 동시 전진 + X 근접 → 차단
```
- 차단 detail: `"Y축 전진 불가: Front/Rear PickerY가 동시에 전진 상태이고 PickerX 엔코더 경로가 마주보는 안전거리 안에 있습니다. 한쪽 PickerY를 실제 Avoid 또는 0 위치로 이동한 뒤 진행하세요. xDistance=..., requiredClearance=..."`

### 2-3. `CanMovePickerXByFacingYInterlock` (1989~2050) — X 이동 마주봄 검사
`ownOut && otherOut`(양쪽 Y 돌출)이고 X경로가 `DoXMovePathsEnterFacingClearance`로 안전거리 안에 겹칠 때만 차단. `CanMovePickerXByOppositeYInterlock`(2094)는 거의 동일하나 자기 Y도 `IsPickerYOutOrMovingOut(...,null)` 기준.

### 2-4. `VerifyPickerXMove` (1219~1429) — X 이동 종합 규칙 (가장 복잡)
검사 순서:
1. `VerifyInputStageZSafeForInputZone` — 현재/목표가 Input이면 PickerZ0~3가 Avoid/0이상 + InputExpandingZ ≤ 0 요구 (2411)
2. 목표=Input이고 반대 픽커가 Input 점유 → 차단 (`"Input 픽업 영역을 반대 픽커가 사용 중입니다. owner=..."`)
3. 목표=비Input·비Avoid·점유 존이고 반대 픽커 점유 → `CanAutoShareProcessWorkAreaWhenOppositeYSafe`(1650) 예외 아니면 차단
4. 목표=Avoid: 자기 Y가 Avoid면 허용, 아니면 `CanMovePickerXByOppositeYInterlock`로 양쪽 동시 전진만 차단
5. 목표=Unknown: 자기 Y Avoid면 허용, 아니면 차단
6. ColletCalibrationFineAlign 이동: `CanMovePickerXByFacingYInterlock`만 확인
7. `pickerYAtAvoid`/`inspectionContinuousProcessMove`/`autoProcessCorrectionXMove` 중 하나가 아니면 → `"메뉴얼/단독 X축 이동 전 PickerY가 Avoid 또는 0 위치여야 합니다..."` 차단
8. 존 간 이동 시 위 예외 없으면 차단
9. 최종 `CanMovePickerXByFacingYInterlock`

### 2-5. `VerifyPickerYMove` (1736~1866) — Y 전진 종합 규칙
1. `VerifyInputStageZSafeForInputZone`
2. 목표 Avoid면 즉시 허용
3. 목표 Unknown이면 `IsPickerYTargetAvoid` 아닐 때 차단
4. 목표=Input이고 반대 픽커 Input 점유 → 차단
5. 목표 Process/Output이고 반대 픽커 점유 → 공유 예외 아니면 차단
6. `VerifyPickerYFacingXClearance` (X거리 마주봄)
7. 상대 활성 Y 목표 존과 `CanShareForwardY`(다른 존이면 병렬 허용, 같은 존이면 상대가 Avoid여야) 판정

### 2-6. `VerifyPickerXGlobalMachineClearance` (140~269) — X 전역 안전
Picker X 이동 시작 전 전체 장비 상태 검사:
- InputFeederY: `IsWaferFeederAvoidPositionCheck()`(Avoid Dog **X090** ON) + `IsWaferFeederDown()` + 비이동
- OutputFeederY: `IsBinFeederAvoidPositionCheck()`(Avoid Dog **X091** ON) + `IsFeederDown()` + 비이동
- InputExpandingZ: `ActualPosition <= tolerance`(0 이하)
- OutputGoodStageZ: `ActualPosition <= Recipe.GoodStageZ.ProcessPosition + tolerance`
- 각 실패마다 고유 한국어 reason

### 2-7. `VerifyPickerYHomePairSafety` (272~369) — Y HOME 페어 안전
두 PickerY를 모두 Servo Off 후 한 축만 On하고 HOME해야 함:
- 양쪽 Y/양쪽 X 비이동
- `otherY.IsServoOn && (frontNearHomeOrAvoid||rearNearHomeOrAvoid) && xDistance <= xClearance + DefaultTolerance` → 차단(밀림 위험)
- 반대 Y Servo On 시 차단, 자기 Y Servo Off 시 차단
- `xDistance = |frontX - rearX|`, `xClearance = ResolvePickerYFacingXClearance`

### 2-8. `VerifyPickerZAtOrAboveZeroForZoneStageZMove` (712/732) — Stage Z 상승 시 PickerZ 보호
StageZ가 Picker 방향(`movingTowardPicker=true`)일 때만 검사. 해당 존에 Picker가 있으면 PickerZ0~3가 `>= -tolerance`(0 이상) 또는 티칭 `AvoidPosition`이어야 함(`IsPickerZAtOrAboveZeroOrAvoid`, 788).

### 2-9. `ResolvePickerZoneTransportState` (932~985) — 운송 차단 상태 계산 (허브)
`PickerZoneTransportState`를 채움: `CurrentZone`(`ResolveCurrentXZoneWithContext`), `TargetZone`(`ResolveTargetXZoneWithContext`), `HasWorkArea`/`WorkAreaZone`(`TryGetPickerWorkArea`), `YAvoid`(`IsPickerYAtAvoid`), `UnknownUnsafe`(존 Unknown인데 Y가 Avoid 아님). Feeder/Stage 인터락들이 `state.BlocksTransport`로 판단.

---

## 3. 헬퍼 호출 관계 트리 (`VerifyFacingYDistanceFirst` 아래)

```
VerifyFacingYDistanceFirst (1104)
├─ IsAxisMotionRequest (1160)
├─ TryResolvePickerXYRequest (1170) → MotionGuardRuleHelpers.IsMoving
└─ CanMovePickerAxisByFacingYInterlock (596)
   ├─[axis=Y]─ CanMovePickerYByFacingYInterlock (1912)
   │   ├─ GetPickerY / GetPickerX (3489/3479)
   │   ├─ IsPickerYRecoveryMove (2174) → IsPickerYOutByPosition, ResolvePickerYSafeDistance → GetPickerTeachingPosition
   │   ├─ IsPickerYForwardDirection (2165)         [DefaultTolerance]
   │   ├─ IsPickerYOutByPosition (2249) → IsPickerYSafeByPosition (2269) → IsNearPickerYTeachingPosition (2287)
   │   ├─ IsPickerYOutOrMovingOut (2216) → GetActivePickerYTargetZone, IsPickerYAtAvoid
   │   ├─ ResolvePickerYFacingXClearance (2370)    [Config or 150]
   │   ├─ ResolveAxisPathTarget (2304)             [IsMoving? CommandPosition : ActualPosition]
   │   ├─ DoXMovePathsEnterFacingClearance (2323)  [구간 겹침 계산]
   │   └─ BuildFacingYBlockedDetail (2338) → FormatAxis
   └─[axis=X]─ CanMovePickerXByFacingYInterlock (1989)
       ├─ IsPickerYOutOrMovingOut (2216) × 2 (own/other)
       ├─ GetPickerX/Y (3479/3489)
       ├─ ResolvePickerYFacingXClearance (2370)
       ├─ ResolveAxisPathTarget (2304)
       ├─ DoXMovePathsEnterFacingClearance (2323)
       └─ BuildFacingYBlockedDetail (2338)
```

존 판정 계열:
```
ResolveCurrentXZoneWithContext (2851)
├─ IsPickerYAtAvoid (3335) → IsAxisAtHomePosition, IsPickerAxisInTeachingPosition
├─ TryGetActiveProcessWorkArea (2974) → TryGetPickerWorkArea (456) → IsPickerWorkAreaActive (3616)
├─ ResolveXZoneByPosition (3002)
│   ├─ TryResolveEncoderXZoneByPosition (3057)   [Config ZoneX 우선]
│   │   ├─ GetPickerZoneXSetup (3225) → PickerFrontUnit.Setup.ZoneX / PickerRearUnit.Setup.ZoneX
│   │   ├─ IsInZone (3274) → PickerZoneXRange.Contains
│   │   └─ WriteEncoderZoneOverlapLog (3150) → QMC.Common.Log.Write
│   └─ ResolvePickerXTeachingZoneByPosition (3027) [티칭 위치 보조]
│       ├─ IsAtPickerPosition (3380) / IsAtPickerZonePosition (3394/3407)
│       │   └─ GetPickerTeachingPosition (3459) + GetRuntimePickerZoneOffset (3430)
│       ├─ IsManualPickerXInProcessRange (2754)
│       ├─ IsPickerTargetBelowAvoidPosition (3326)
│       └─ IsManualPickerXOutputSide (2804)
├─ GetActivePickerYTargetZone (3598)
└─ ResolveCurrentYZone (2992) → ResolveYZoneByPosition (3299)
```

작업영역 점유 계열:
```
BeginPickerWorkAreaUse (443) → AddPickerWorkAreaUse (3801) ── PickerWorkAreaScope.Dispose → RemovePickerWorkAreaUse (3863)
BeginPickerZoneMove (415) → ParseZone/NormalizeInterlockZone ── ActiveZoneScope.Dispose (복구)
ClearPickerWorkAreasForReadyIfSafe (505)
├─ IsFrontPickerReadyAvoidSafe / IsRearPickerReadyAvoidSafe (3647/3662)
│   └─ ArePickerAxesReadyAvoidSafe (3677) → axis.UpdateStatus/IsAlarm/IsMoving, ResolveReadyAvoidTolerance
└─ ClearAllPickerWorkAreasLocked (3765) → ClearPickerWorkAreaLocked (3784)
```

---

## 4. 참조하는 외부 상태 및 설정값 출처

**축 실측 상태 (BaseAxis)** — `GetPickerX/Y/Z`로 `machine.PickerFrontUnit`/`PickerRearUnit`에서 획득:
`ActualPosition`, `CommandPosition`, `IsMoving`, `IsServoOn`, `IsAlarm`, `UpdateStatus()`, `axis.Config.InPositionTolerance`

**티칭/오프셋** (`PickerFrontUnit`/`PickerRearUnit`):
- `GetPickerTeachingPosition(axis, positionName)` — "AvoidPosition","InputAvoidPosition","OutputAvoidPosition","PickPosition","BottomPosition","SidePosition","PlacePosition"
- `GetRuntimePickerOffset(index)` → `PickerAlignOffset.AlignOffsetX/AlignOffsetY` (collet별 런타임 보정)

**Config/Setup 값 출처**:
| 값 | 출처 | 기본값 |
|---|---|---|
| `PickerYFacingXClearance` | Front Setup=**300.0** / Rear Setup=**150.0**, 런타임 `Max` 사용 → 실질 300 | 파일 상수 150.0 |
| `PickerYOutDistance` | Picker Setup | 1.0 |
| `ZoneX` (엔코더 존) | `Setup.ZoneX` = `PickerZoneXSetup` | `UseEncoderZone=true`, `ZoneTolerance=1.0` |
| `AppSettings` | `AppSettingsStore.Current` → `UseAjin`, `DryRunMode`, `SimulationMode` | — |

**Feeder/Stage 상태** (`VerifyPickerXGlobalMachineClearance`):
- `InputFeederUnit`: `FeederY`, `Recipe.AvoidPosition`, `IsWaferFeederAvoidPositionCheck()`(X090), `IsWaferFeederDown()`
- `OutputFeederUnit`: `FeederY`, `Recipe.AvoidPosition`, `IsBinFeederAvoidPositionCheck()`(X091), `IsFeederDown()`
- `InputStageUnit`: `ExpanderZ`, `Recipe.WaferZ`
- `OutputStageUnit`: `GoodStage.StageZ`, `Recipe.GoodStageZ.ProcessPosition`

**Intent 메타**: `MotionGuardMoveIntent.Parse(targetName)` — `AutoProcessCorrection(Max)`, `InspectionContinuous`, `InspectionZHold`, `InspectionFromZone/ToZone`

> **주의**: `PickerPhaseCoordinator`, `MaterialStateService`는 이 파일에서 직접 참조하지 않음. 존 점유 상태는 이 클래스의 **static 필드(전역, `activeZoneLock` 보호)**로 자체 관리: `front/rearPickerYActiveTargetZone`, 존별 `UseCount`/`Owner`(Input/Bottom/Side/Output × Front/Rear).

---

## 5. 상수 / 임계값 목록

파일 상단(112~116행) `private const`:

| 상수 | 값 | 용도 |
|---|---|---|
| `DefaultTolerance` | **0.05** | 위치 비교 기본 tolerance, Home(0) 근접, 방향 판정 |
| `DefaultPickerYFacingXClearance` | **150.0** | Y 동시 전진 시 X 최소 안전거리 (Config 미설정 fallback) |
| `DefaultPickerYOutDistance` | **1.0** | Y가 Avoid/Home에서 이 거리 이상 벗어나면 "전진(돌출)" 판단 |
| `DefaultAutoProcessCorrectionMaxDistance` | **2.0** | 오토 공정 보정 X 이동 최대 허용 이동량(mm) |
| `DefaultAutoProcessZoneEntryYTolerance` | **2.0** | 오토 보정 진입 시 PickerY Ready 위치 근접 tolerance |

Setup 기본값: `PickerYFacingXClearance` Front=**300.0**/Rear=**150.0**(`Max`이므로 실질 300), `PickerYOutDistance`=**1.0**, `ZoneTolerance`=**1.0**.
암묵 임계값: PickerZ 안전 = `>= -tolerance`(0 이상) 또는 티칭 AvoidPosition; InputExpandingZ 안전 = `<= tolerance`(0 이하).

---

## 6. 방향/판정 규칙 요약

- **전진 방향**: Front PickerY는 `target > current + 0.05`(+Y), Rear는 `target < current - 0.05`(-Y)
- **X경로 겹침**(`DoXMovePathsEnterFacingClearance`): 자기 X의 [min-clearance, max+clearance] 구간과 상대 X의 [min,max] 구간이 겹치면 위험
- **존 판정 우선순위**: (1) Process 작업영역 점유+Y진입 → Process, (2) 엔코더 존(ZoneX, `UseEncoderZone`), (3) 티칭 위치/공정 범위 보조. 엔코더 존 range 밖/중복이면 `Unknown` → 상위 인터락이 보수적으로 차단
