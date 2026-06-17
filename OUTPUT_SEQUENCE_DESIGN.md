# OutputSequence 구현 설계서 — Bin Data 기반 좌표 생성

> 목적: 로딩된 OK/NG 아웃스테이지에 대해, **bin data**를 기준으로 픽커가 die를 OK/NG로 플레이스할 때
> 사용할 **수령 좌표를 아웃스테이지 ProcessPosition(센터) 기준으로 생성**하도록 OutputSequence를 완성한다.
> 이 문서는 Claude Code로 구현하기 위한 설계 명세이며, 실제 `.cs` 수정은 별도로 진행한다.
> 작성 규칙은 `AGENTS.md` / `SEQUENCE_RECOVERY_RULES.md`를 따른다.

---

## 1. 확정된 결정사항 (사용자 확인 완료)

| # | 항목 | 결정 |
|---|---|---|
| D1 | 좌표 센터 기준 | **레시피 `ProcessPosition` 사용** — 행(Y)=`{Good\|NG}StageY.ProcessPosition`, 열(X)=`VisionX.ProcessPosition` |
| D2 | OK/NG die 배치 형상 | **별도 빈 트레이 형상** — 입력 웨이퍼 die map과 무관하게 빈 트레이 자체의 행/열/피치를 레시피로 정의 |
| D3 | 축 구조 | **현재 구조 유지** — 열(X)=피커 X축, 행(Y)=아웃스테이지 Y축 |

---

## 2. 현재 구현 현황 (분석 결과)

### 2.1 좌표 생성 체인 (현재)

```
PickerPlaceSequence (열=피커X, 행=스테이지Y)
   └─ ReserveOutputStageTarget()
        └─ MaterialStateService.ReserveNextOutputStageReceiveTarget(side)
             ├─ (최초) InitializeOutputStageReceivePlan(side)
             │     · 소스(InputStage) 웨이퍼 die map + OutputPickup subset 으로 수령 순서 생성
             │     · pitchX/Y = 소스 웨이퍼 피치   ← (D2 위반: 빈 트레이 피치여야 함)
             │     · OriginX = 0.0 / OriginY = 0.0  ← ★ 하드코딩 (센터 미반영)
             └─ OutputStageReceiveTarget 반환
                   · OffsetX = OriginX + pitchX*DieMapX = pitchX*DieMapX (코너 기준)
                   · OffsetY = OriginY + pitchY*DieMapY = pitchY*DieMapY
                   · TargetX = OffsetX,  TargetY = OffsetY
```

### 2.2 좌표 소비 (현재)

`PickerPlaceSequence.cs`
- `MoveOutputStageReceivePosition()` (L327~342):
  `_targetOutputStageY = {NG|Good}StageY.LoadPosition + _receiveTarget.TargetY`  ← ★ 기준이 **Load**Position
  → `NgBinY`/`GoodBinY` 축 이동.
- `CalculatePlaceTarget()` (L344~356):
  `_targetPickerX = _receiveTarget.TargetX + ResolveOutputVisionToPickerXOffset(idx) + ResolvePickerAlignOffsetX(idx)`
  `_targetPickerY = ResolvePickerZoneY("DiePlacePosition", idx)`
  `_targetPickerT/Z = 피커 티칭(PlacePosition)`
  → 피커 X/Y/T 이동 후 Z 하강, Vacuum Off, Blow, Z Avoid, `MoveDieToOutputStage`.

### 2.3 관련 파일/심볼 인덱스

| 역할 | 파일 | 핵심 심볼 |
|---|---|---|
| 좌표 생성 | `QMC.CDT-320/Equipment/Materials/MaterialStateService.cs` | `InitializeOutputStageReceivePlan` (L687), `ReserveNextOutputStageReceiveTarget` (L743), `BuildOutputReceiveOrder` (L935), `ResolveOutputPickup` (L959) |
| 수령 타깃 DTO | `QMC.CDT-320/Equipment/Materials/MaterialModel.cs` | `OutputStageReceiveTarget` (L329) |
| 웨이퍼 수령 상태 | `MaterialModel.cs` | `WaferMaterial.OutputReceive*` (L279~290) |
| die 빈 인덱스 | `MaterialModel.cs` | `DieMaterial.Bin_IndexX/Bin_IndexY` (L315~316), `Output_BinCode` (L311) |
| 플레이스 소비 | `QMC.CDT-320/Sequencing/Picker/PickerPlaceSequence.cs` | `MoveOutputStageReceivePosition` (L327), `CalculatePlaceTarget` (L344) |
| 스테이지 수령(TPU계열) | `QMC.CDT-320/Sequencing/OutputStage/OutputStageReceiveDieSequence.cs` | `ReserveReceiveTarget` (L121) |
| 레시피/위치 해석 | `QMC.CDT-320/Equipment/Unit/OutputStageUnit.cs` | `OutputStageRecipe` (L125), `GetStageTeachingPosition` (L1292), `StageAxisPositions`(InputStageUnit.cs L94) |
| 시퀀스 베이스 | `QMC.CDT-320/Sequencing/OutputStage/OutputStageSequenceBase.cs` | `ResolveTarget`, `ResolveSideTarget` |

---

## 3. Gap 분석 (왜 미완성인가)

1. **G1 — 센터 미반영**: `OutputReceiveOriginX/Y = 0.0` 하드코딩. 그리드가 빈 트레이 센터(ProcessPosition)에 정렬되지 않고 코너(0,0)에서 +방향으로만 증가.
2. **G2 — 기준 위치 오류**: 소비측이 `LoadPosition` 기준. (D1: ProcessPosition 이어야 함)
3. **G3 — 형상 불일치**: 피치/행열을 소스 웨이퍼 die map에서 가져옴. (D2: 별도 빈 트레이 형상이어야 함)
4. **G4 — 빈 슬롯 인덱스 미사용**: `DieMaterial.Bin_IndexX/Y` 필드가 있으나 수령 좌표 계산에 사용되지 않음. 빈 채움 순서/위치가 빈 트레이 슬롯에 매핑되지 않음.
5. **G5 — 경로 중복 위험**: `PickerPlaceSequence` 와 `OutputStageReceiveDieSequence` 둘 다 `ReserveNextOutputStageReceiveTarget` 호출. 활성 경로 확정 필요(§8).

---

## 4. 목표 좌표 모델 (설계)

### 4.1 좌표계 정의

- **센터(원점)**: 빈 트레이 중심 = 레시피 ProcessPosition.
  - 행(Y) 센터: `GoodStageY.ProcessPosition` / `NGStageY.ProcessPosition`
  - 열(X) 센터: `VisionX.ProcessPosition` (카메라/스테이지 X 좌표계 기준)
- **빈 트레이 형상(레시피, side별)**: `Columns(열,X)`, `Rows(행,Y)`, `PitchX`, `PitchY`, 채움 시작 코너/방향.
- **빈 슬롯 인덱스**: `binIx ∈ [0, Columns-1]`, `biny ∈ [0, Rows-1]`. 채움 순서(OrderIndex)→슬롯 인덱스 매핑은 StartCorner/Direction 으로 결정.

### 4.2 좌표 생성 공식 (센터 기준, 부호 있는 상대 오프셋)

```
TargetX = PitchX * ( binIx - (Columns - 1) / 2.0 )   // 센터 기준 좌우 오프셋(mm)
TargetY = PitchY * ( binIy - (Rows    - 1) / 2.0 )   // 센터 기준 상하 오프셋(mm)
```

- `TargetX/TargetY` 는 **센터 기준 상대 오프셋**으로 정의(절대좌표 아님).
- `OriginX/OriginY` 필드는 추적/로그용으로 사용한 ProcessPosition 센터값을 저장(선택).

### 4.3 소비측 절대 좌표 변환 (PickerPlaceSequence)

```
// 행(Y): 아웃스테이지 Y축
stageYTarget  = {side}StageY.ProcessPosition + TargetY        // (변경: Load → Process)

// 열(X): 피커 X축  (현 구조 유지)
pickerXTarget = (빈 센터에 대응하는 피커 X 기준값)
              + TargetX
              + ResolvePickerAlignOffsetX(idx)
   여기서 "빈 센터 피커 X 기준값" = VisionX.ProcessPosition + ResolveOutputVisionToPickerXOffset(idx)
   ※ ResolveOutputVisionToPickerXOffset 가 이미 ProcessPosition 을 카메라 기준으로 포함하는지
     구현 시 1회 확인 후, 중복 가산되지 않도록 결정 (§7 주의).
```

> 핵심 변경 요지: ① 기준 Load→**Process**, ② 좌표를 **센터 기준**으로, ③ 피치/행열을 **빈 트레이 형상**에서.

---

## 5. 데이터 모델 변경

### 5.1 레시피 — 빈 트레이 형상 추가

`QMC.CDT-320/Equipment/Unit/OutputStageUnit.cs` 의 `OutputStageRecipe` 에 side별 빈 트레이 레이아웃 추가.

```csharp
// 신규: 빈 트레이(슬롯 그리드) 형상
[DataContract]
public class BinTrayLayout
{
    [DataMember] public int Columns { get; set; } = 1;     // 열 수 (X)
    [DataMember] public int Rows { get; set; } = 1;        // 행 수 (Y)
    [DataMember] public double PitchX { get; set; }        // 열 간 피치 (mm)
    [DataMember] public double PitchY { get; set; }        // 행 간 피치 (mm)
    [DataMember] public string StartCorner { get; set; } = "TopLeft";  // 채움 시작 코너
    [DataMember] public string Direction { get; set; } = "RowMajor";   // 채움 방향
}

// OutputStageRecipe 에 추가
[DataMember] public BinTrayLayout GoodBinTray { get; set; } = new BinTrayLayout();
[DataMember] public BinTrayLayout NgBinTray { get; set; } = new BinTrayLayout();
// EnsurePositionObjects() 에 null 가드 추가
```

- 기존 enum 재사용 가능하면 재사용(예: `PickupSubset.StartCorner/Direction` 패턴). 없으면 문자열 유지 후 파싱.
- `StageAxisPositions` 에는 `ProcessPosition` 이 이미 존재(추가 불필요).

### 5.2 WaferMaterial — 빈 형상/센터 저장 필드

`MaterialModel.cs` `WaferMaterial` 의 `OutputReceive*` 를 빈 트레이 기준으로 의미 재정의(필드 추가/용도 변경).

```csharp
// 기존 OutputReceiveDieMapX/Y → 빈 트레이 Columns/Rows 의미로 사용하거나 신규 필드 추가
[DataMember] public int    OutputReceiveColumns { get; set; }   // = 빈 트레이 열(X)
[DataMember] public int    OutputReceiveRows { get; set; }      // = 빈 트레이 행(Y)
// OutputReceivePitchX/Y → 빈 트레이 피치로 채움 (소스 웨이퍼 피치 아님)
// OutputReceiveOriginX/Y → 사용한 ProcessPosition 센터값(추적용)
```

> 호환성: 기존 필드를 재사용하면 JSON 스냅샷 마이그레이션 부담이 적음. 신규 필드 추가 시 `OnDeserializing` 기본값 처리.

---

## 6. 수정 위치별 상세

### 6.1 `MaterialStateService.InitializeOutputStageReceivePlan(side)` (L687)

변경점:
- 피치/행열을 **빈 트레이 레시피**에서 가져온다(소스 웨이퍼 map 아님).
- `OriginX/Y` 를 ProcessPosition 센터값으로 저장(추적용).
- 수령 순서(`ordered`)는 die 식별/순서 결정에는 계속 사용하되, **좌표는 빈 슬롯 인덱스**로 생성.

```text
project   = RecipeStore.LoadLastOrDefault()
binTray   = (side==Ng) ? Recipe.NgBinTray : Recipe.GoodBinTray
centerX   = Recipe.VisionX.ProcessPosition
centerY   = (side==Ng) ? Recipe.NGStageY.ProcessPosition : Recipe.GoodStageY.ProcessPosition

outputWafer.OutputReceiveColumns = binTray.Columns
outputWafer.OutputReceiveRows    = binTray.Rows
outputWafer.OutputReceivePitchX  = binTray.PitchX
outputWafer.OutputReceivePitchY  = binTray.PitchY
outputWafer.OutputReceiveOriginX = centerX     // 추적용
outputWafer.OutputReceiveOriginY = centerY
outputWafer.OutputReceiveTotalCount = min(ordered.Count, Columns*Rows)
outputWafer.OutputReceiveStartCorner = binTray.StartCorner
outputWafer.OutputReceiveDirection   = binTray.Direction
```

- 빈 트레이 용량(`Columns*Rows`)보다 die 수가 많으면 정책 필요(§9 Q).

### 6.2 `MaterialStateService.ReserveNextOutputStageReceiveTarget(side)` (L743)

변경점: 좌표를 **빈 슬롯 인덱스 → 센터 기준 상대 오프셋**으로 생성.

```text
index   = 채워진 슬롯 수 (DieIds 유효 개수)   // 기존 로직 유지
if index >= TotalCount → null

(binIx, binIy) = MapOrderToBinSlot(index, Columns, Rows, StartCorner, Direction)

TargetX = PitchX * ( binIx - (Columns - 1) / 2.0 )
TargetY = PitchY * ( binIy - (Rows    - 1) / 2.0 )

target.OrderIndex = index
target.DieMapX = binIx          // 의미: 빈 슬롯 열
target.DieMapY = biny           // 의미: 빈 슬롯 행
target.OffsetX = TargetX
target.OffsetY = TargetY
target.TargetX = TargetX        // 센터 기준 상대 오프셋
target.TargetY = TargetY
```

- `MapOrderToBinSlot` 신규 헬퍼(StartCorner/Direction 반영). RowMajor/ColMajor + 4코너 지원.
- (선택) 예약 시 `DieMaterial.Bin_IndexX/Y`, `Output_BinCode` 동기화.

### 6.3 `PickerPlaceSequence.MoveOutputStageReceivePosition()` (L327)

```text
// 변경 전: baseY = {side}StageY.LoadPosition
// 변경 후: baseY = {side}StageY.ProcessPosition
double baseY = (side==Ng) ? OutputStage.Recipe.NGStageY.ProcessPosition
                          : OutputStage.Recipe.GoodStageY.ProcessPosition;
_targetOutputStageY = baseY + _receiveTarget.TargetY;
```
- 이동/대기/확인 3단 분리 구조(`MoveOutputStageAxisAndVerifyAsync`)는 그대로 유지(규칙 준수).

### 6.4 `PickerPlaceSequence.CalculatePlaceTarget()` (L344)

```text
// X 기준을 빈 센터(VisionX.ProcessPosition) 기준으로 정렬
double visionCenterX = OutputStage.Recipe.VisionX.ProcessPosition;
_targetPickerX = visionCenterX
               + ResolveOutputVisionToPickerXOffset(_currentPickerIndex)
               + _receiveTarget.TargetX
               + ResolvePickerAlignOffsetX(_currentPickerIndex);
// Y/T/Z 는 기존 유지
```
- ※ `ResolveOutputVisionToPickerXOffset` 가 이미 ProcessPosition 을 내부 포함하는지 확인 후
  `visionCenterX` 중복 가산 여부 결정(§7).

### 6.5 `OutputStageReceiveDieSequence` (L121) — 경로 확정 후 정합

- 활성 경로가 `PickerPlaceSequence` 면, 본 시퀀스의 좌표 계산도 동일 모델(Process+센터)로 통일하거나 호출 제거.
- `_targetY = ResolveSideTarget(side,"Load") + OffsetY + ...` → `"Process"` 기준 + 센터 오프셋으로 정합.

---

## 7. 구현 시 주의 (검증 포인트)

1. **X 기준 중복 가산**: `ResolveOutputVisionToPickerXOffset` 반환값 정의 확인. 이미 ProcessPosition 기준 절대 피커X면 `visionCenterX` 를 더하지 말 것.
2. **TargetX/Y 의미 변경**: 코너 기준 → 센터 기준(부호 있음). 이 DTO를 읽는 모든 소비처(검색: `_receiveTarget.TargetX/TargetY`, `OutputStageReceiveTarget`)를 함께 수정.
3. **이동 안전 규칙(AGENTS Sequence Motion Safety)**: 이동 명령→결과확인→Wait(InPosition) 3단 분리 유지. 시뮬레이션에서도 완료 확인 생략 금지.
4. **반환/실패 규칙**: 모든 모션 함수 `Task<int>`, 성공 0 / 실패 -1·장비코드. 실패 시 로그 + Alarm(시퀀스 내부).
5. **JSON 저장**: 레시피/스냅샷 저장은 `JsonPrettySerializer.WriteObject` 사용.
6. **호환성**: 기존 Lot/스냅샷 JSON 로드 시 신규 빈 필드 기본값 가드.

---

## 8. 경로 중복 확정 (선결 과제)

`ReserveNextOutputStageReceiveTarget` 호출자 2곳:
- `PickerPlaceSequence` (피커가 직접 플레이스) — D3(축 구조 유지)와 부합.
- `OutputStageReceiveDieSequence` (TPU `NotifyPlaceReady` 모델).

→ **확인 필요**: 실제 운전 경로가 피커 직접 플레이스인지, TPU 경유인지. 둘 중 하나로 단일화하거나 역할 분리(스테이지=수령자세, 피커=플레이스)를 명문화. (구현 착수 전 사용자 확인 권장)

---

## 9. 미해결/추가 확인 항목

- Q1. 빈 트레이 채움 순서(StartCorner/Direction)의 정확한 규칙(예: TopLeft + RowMajor, 좌→우/상→하?).
- Q2. die 수 > 빈 트레이 용량(Columns×Rows)일 때 처리(다음 트레이 교체 트리거 / 알람 / 분할).
- Q3. OK/NG 트레이의 행/열·피치 실제 값(레시피 티칭) — 티칭 UI 노출 필요 여부.
- Q4. 비전 보정(BinOffset) 적용 시점 — 수령 좌표에 합산할지, 플레이스 직전 보정할지.

---

## 10. 작업 분해 (구현 순서 제안)

1. 레시피 모델 `BinTrayLayout` + `Good/NgBinTray` 추가 (+ `EnsurePositionObjects` 가드).
2. `WaferMaterial` 빈 형상/센터 필드 정리 (재사용 vs 신규).
3. `MapOrderToBinSlot` 헬퍼 + `InitializeOutputStageReceivePlan` 빈 트레이 기반화.
4. `ReserveNextOutputStageReceiveTarget` 센터 기준 좌표 생성.
5. `PickerPlaceSequence` 기준 Load→Process + 센터 오프셋 반영.
6. `OutputStageReceiveDieSequence` 정합 또는 단일화(§8).
7. 레시피 티칭 UI(OutputStageRecipePage)에 빈 트레이 형상 입력 추가(필요 시).
8. 검증: 빌드 → Sim/DryRun 사이클 → 좌표 로그 확인 → 회귀(`perl tools/verify_all.pl`).

---

## 11. 수용 기준 (Acceptance)

- [ ] 레시피 ProcessPosition(행 Y, 열 X)을 센터로, 빈 트레이 행/열/피치로 die 좌표가 생성된다.
- [ ] 그리드가 센터 대칭(짝수/홀수 행열 모두 중앙 정렬)으로 배치된다.
- [ ] OK/NG die가 채움 순서대로 올바른 빈 슬롯에 플레이스된다(로그/시뮬 확인).
- [ ] 기준이 LoadPosition → ProcessPosition 으로 전환되었다.
- [ ] 피치/행열이 입력 웨이퍼가 아닌 빈 트레이 형상에서 온다.
- [ ] 모션 안전 규칙(명령/Wait/Check 분리), Task<int>, 로그·알람 규칙 준수.
- [ ] 빌드 성공 + 기존 회귀 PASS 유지.
