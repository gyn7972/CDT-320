# OutputStageMapTransferPage 설계서 — Input 페이지 참조 기반 Bin Data / Material 구현

> 목적: `InputStageMapTransferPage` 와 입력 스테이지의 **bin data / Material 사용 패턴**을 참조하여,
> `OutputStageMapTransferPage` 의 빈 데이터·Material 연동을 동일 수준으로 완성한다.
> 좌표 모델/빈 트레이 형상은 `OUTPUT_SEQUENCE_DESIGN.md` 를 따른다(ProcessPosition 센터, 별도 빈 트레이 형상).
> 구현은 Claude Code로 진행. 본 문서는 설계 명세이며 규칙은 `AGENTS.md` 준수.

---

## 1. 현황 비교 (Input vs Output 페이지)

| 파일 | 라인수 | 상태 |
|---|---|---|
| `Ui/Pages/Work/InputStageMapTransferPage.cs` | 1332 | 완성 (참조 원본) |
| `Ui/Pages/Work/OutputStageMapTransferPage.cs` | 576 | 표시 위주 / 미완성 |

출력 페이지에 이미 있는 것: 좌/우 사이드(GOOD/NG) 전환, 소스맵 표시, `InitializeOutputStageReceivePlan(side)` 호출, 그리드 표시, `SaveMaterialState()`(NotifyAndSave만).

**핵심 누락(빈 데이터·Material 관련):**
1. **빈 트레이 기준 맵 생성 부재** — 현재 `BuildDisplayMap(sourceMap,...)` 로 *소스 입력 웨이퍼 die map* 을 그대로 표시. → 별도 빈 트레이 형상 + ProcessPosition 센터 기준 맵이어야 함(D2/D1).
2. **수령 계획/빈 위치를 Material에 기록하는 메서드 부재** — 입력의 `PersistPickStatusToMaterialState` 에 대응하는 출력 persist 없음.
3. **선택 빈 슬롯 좌표 Manual 이동 부재** — 입력의 `MoveSelectedDieAsync` 대응 없음.
4. **그리드 컨텍스트 메뉴(MOVE) / target 토글 / Lot 진행률 / dirty 가드** 부재.

---

## 2. Input → Output 메서드 매핑 (참조표)

| InputStageMapTransferPage | 역할 | OutputStageMapTransferPage (목표) |
|---|---|---|
| `BuildOrFetchMap` / `CreateInputCircleMapFromRecipe` | 레시피로 맵 생성 | `BuildBinTrayMap(side)` — 빈 트레이 행/열/피치 + ProcessPosition 센터로 격자 생성 (신규) |
| `TryLoadActiveInputMap` / `RefreshActiveInputMapIfChanged` | 활성 맵 로드/변경감지 | `ReloadOutputMap`(존재) + `BuildMapSignature` 변경감지 추가 |
| `PersistPickStatusToMaterialState(map)` | **맵→Material 기록** | `PersistReceivePlanToMaterialState(side, map)` — 빈 위치/코드/인덱스 기록 (신규, §4) |
| `SavePickStatus` | 저장 + 시퀀스 갱신 | `SaveReceivePlan` — persist + plan 갱신 + dirty 해제 (신규) |
| `MoveSelectedDieAsync` | 선택 die 좌표 이동 | `MoveSelectedBinSlotAsync` — 선택 슬롯 좌표로 스테이지/피커 이동 (신규, §5) |
| `BuildGridContextMenu` / `OnGridDieListCellMouseDown` | 그리드 우클릭 MOVE | 동일 패턴 적용 (신규) |
| `ToggleSelectedEntryTarget` | target on/off | (선택) 빈 슬롯 사용/제외 토글 |
| `ApplyLotProgress` | Lot 진행률 표시 | `ApplyLotProgress` 출력 버전 |
| `RunInputStageSequenceActionAsync` | 시퀀스 버튼 실행 | (선택) Output receive 시퀀스 수동 실행 |
| `SetActionButtonsEnabled` | 버튼 잠금 | 동일 |
| `MarkManualAlignComplete` | 상태 저장 | `MarkOutputPlanReady`(WorkReady 저장) |

---

## 3. 빈 트레이 맵 생성 (BuildBinTrayMap) — §1.1 누락 해결

입력은 웨이퍼 원형 격자를 만들지만, 출력은 **빈 트레이 사각 격자**를 만든다.

```text
입력: side(Good/NG)
binTray = (side==Ng) ? Recipe.NgBinTray : Recipe.GoodBinTray   // OUTPUT_SEQUENCE_DESIGN.md §5.1
centerX = Recipe.VisionX.ProcessPosition
centerY = (side==Ng) ? Recipe.NGStageY.ProcessPosition : Recipe.GoodStageY.ProcessPosition

map.DieMapX = binTray.Columns ; map.DieMapY = binTray.Rows
map.PitchX  = binTray.PitchX  ; map.PitchY  = binTray.PitchY
map.OriginX = centerX         ; map.OriginY = centerY   // 추적/표시용

for row in 0..Rows-1, col in 0..Columns-1:
    entry.DieMapX = col ; entry.DieMapY = row
    entry.PosX = centerX + PitchX * ( col - (Columns-1)/2.0 )   // 센터 기준
    entry.PosY = centerY + PitchY * ( row - (Rows-1)/2.0 )
    entry.IsTarget = true
    map.Entries.Add(entry)

DieMapGenerator.Normalize(map)
// 채움 순서: PickupSequenceGenerator + project.OutputPickup (BuildReceiveOrder 재사용)
```

- 표시 시 채워진 슬롯/다음 슬롯/대기 슬롯 색 구분은 기존 `BuildDisplayMap` 의 nextIndex 로직 재사용.
- `ReloadOutputMap` 을 `sourceMap` 대신 `BuildBinTrayMap(side)` 기반으로 교체.

---

## 4. 수령 계획 → Material 기록 (PersistReceivePlanToMaterialState) — 핵심

입력 `PersistPickStatusToMaterialState`(L773) 패턴을 출력 빈 데이터에 대응.

```text
입력: side, map(빈 트레이 맵)
DieMapGenerator.Normalize(map)
WaferMaterial outWafer = GetWaferAtLocation(OutputStageGood|OutputStageNg)
if outWafer != null:
    outWafer.OutputReceiveColumns = map.DieMapX        // OUTPUT_SEQUENCE_DESIGN §5.2
    outWafer.OutputReceiveRows    = map.DieMapY
    outWafer.OutputReceivePitchX  = map.PitchX
    outWafer.OutputReceivePitchY  = map.PitchY
    outWafer.OutputReceiveOriginX = map.OriginX        // ProcessPosition 센터
    outWafer.OutputReceiveOriginY = map.OriginY
    outWafer.OutputReceiveTotalCount = 채움순서.Count
    outWafer.UpdatedAt = DateTime.Now

ordered = BuildReceiveOrder(map)
for i, entry in ordered:
    (slot 좌표는 entry.PosX/PosY)
    // 빈 슬롯 메타를 die material 에 반영(있는 die에 한해, 또는 plan 메타로만)
    die = (해당 슬롯에 배정된 die가 있으면) GetOrCreateDieMaterial(...)
    die.Bin_IndexX = entry.DieMapX
    die.Bin_IndexY = entry.DieMapY
    die.Output_BinCode = (side==Ng ? NG코드 : GOOD코드)   // BinCodeMap 참조
    die.BinOffset.X/Y = entry.PosX/PosY ; BinOffset.IsValid = true
    die.WaferID_Output = outWafer.WaferId
    die.UpdatedAt = DateTime.Now

MaterialStateService.NotifyAndSave("OutputMapTransferReceivePlanSave")
```

> 주의: 입력은 die가 웨이퍼에 이미 존재(맵=die 1:1). 출력 빈 트레이는 **die가 채워지기 전 빈 슬롯 계획**이므로,
> die가 아직 없는 슬롯은 *plan 메타(OutputReceive*)* 로만 저장하고, die는 실제 플레이스 시
> `MoveDieToOutputStage`(이미 구현)에서 채운다. → "빈 슬롯 계획(좌표) 저장"과 "die 배정"을 분리.

신규/활용 MaterialStateService API:
- `InitializeOutputStageReceivePlan(side)` — 빈 트레이 기반으로 수정(OUTPUT_SEQUENCE_DESIGN §6.1).
- (선택) `SaveOutputBinTrayPlan(side, map)` — 페이지에서 직접 plan 메타 저장용 신규 헬퍼(입력 `SaveInputStageAlignResult` 대응).

---

## 5. 선택 빈 슬롯 좌표 이동 (MoveSelectedBinSlotAsync)

입력 `MoveSelectedDieAsync`(L949)는 `stage.MoveVisionPointSafelyAsync(PosX,PosY,...)` 호출.
출력은 축 구조(D3: 열=피커X, 행=스테이지Y)에 맞춰 분리 이동.

```text
entry = _selectedEntry  (없으면 경고)
host.Machine.OutputStageUnit 확인
확인 다이얼로그 (X=entry.PosX, Y=entry.PosY)
SetActionButtonsEnabled(false)
// 행(Y): 스테이지 Y 축
r1 = OutputStage.MoveStageAxisAndVerifyAsync({side}BinY, entry.PosY, timeout, fine)
// 열(X): 피커 X 또는 VisionX (운전 정책에 따라) — 확인 필요(§7 Q)
r2 = ...
결과 != 0 → 경고 + 로그. 성공 → lblAxisX/Y 갱신.
finally SetActionButtonsEnabled(true)
```

- 모션 안전 규칙: 명령→Wait→InPosition 분리(`MoveStageAxisAndVerifyAsync` 이미 보장), `Task<int>` 반환, 실패 로그/알람.

---

## 6. 버튼/UI 매핑 (Designer 텍스트 기준)

현재 출력 페이지 버튼(ConfigureOutputDesignerText L74~79):

| 컨트롤 | 현재 텍스트 | 권장 동작 |
|---|---|---|
| `btnReloadActiveMap` | RELOAD OUTPUT DIE MAP | `ReloadOutputMap` (빈 트레이 맵) |
| `btnPickStatusSave` | SELECTED PLAN INIT | `SaveReceivePlan(_selectedSide)` (persist) |
| `btnManualAlignComplete` | GOOD PLAN INIT | `InitializeReceivePlan(Good)` (유지) |
| `btnNeedleBlockDown` | NG PLAN INIT | `InitializeReceivePlan(Ng)` (유지) |
| `btnThetaMatchMove` | SAVE MATERIAL STATE | `SaveMaterialState` (유지) |
| `btnXyMatchMove` | REFRESH DISPLAY | `ReloadOutputMap` (유지) |
| `gridDieList` 우클릭 | (없음) | `MOVE` 컨텍스트 메뉴 → `MoveSelectedBinSlotAsync` |

> Designer 규칙: 컨트롤 선언/배치는 `.Designer.cs` 인라인. 본 작업은 기존 컨트롤 재사용이라 Designer 변경 최소.
> 버튼 의미 변경 시 텍스트만 `ConfigureOutputDesignerText` 에서 조정.

---

## 7. 확인 필요 (구현 착수 전)

- Q1. 빈 슬롯 Manual 이동에서 **열(X) 축**: 피커 X로 이동인지, VisionX(카메라)로 이동인지. (운전 시나리오 확인)
- Q2. 빈 슬롯 계획 저장 시 die 사전 배정 여부: plan 메타만 저장 vs die material 미리 생성.
- Q3. `Output_BinCode` 값 규칙: `BinCodeMap`(Bin/BinCodeMap.cs)에서 GOOD/NG 코드 매핑 확정.
- Q4. 빈 트레이 형상 레시피 입력 UI: `OutputStageRecipePage` 에 Columns/Rows/Pitch/StartCorner 입력 추가 필요 여부.

---

## 8. 작업 분해 (구현 순서)

1. (선행) `OUTPUT_SEQUENCE_DESIGN.md` §5 레시피 `BinTrayLayout` + `WaferMaterial` 빈 필드.
2. `BuildBinTrayMap(side)` 신규 + `ReloadOutputMap` 을 빈 트레이 기반으로 교체.
3. `PersistReceivePlanToMaterialState` + `SaveReceivePlan` 신규(버튼 연결).
4. `MoveSelectedBinSlotAsync` + 그리드 컨텍스트 메뉴(MOVE) 신규.
5. `ApplyLotProgress` / dirty 가드 / `BuildMapSignature` 변경감지(입력 패턴 이식).
6. 버튼 텍스트/이벤트 정리(`ConfigureOutputDesignerText`, `WireEvents`).
7. 검증: 빌드 → 페이지 로드 → GOOD/NG 전환·plan init·persist·move 동작 → 회귀(`perl tools/verify_all.pl`).

---

## 9. 수용 기준

- [ ] 출력 맵이 소스 웨이퍼가 아닌 **빈 트레이 형상 + ProcessPosition 센터**로 표시된다.
- [ ] 수령 계획/빈 위치가 Material(`OutputReceive*`, die `Bin_IndexX/Y`, `Output_BinCode`, `BinOffset`)에 저장된다.
- [ ] 선택 빈 슬롯 좌표로 Manual 이동이 동작한다(축 구조 D3 준수).
- [ ] 입력 페이지와 동등한 UX(컨텍스트 메뉴, 진행률, dirty 가드)를 제공한다.
- [ ] 모션 안전/로그/알람/`Task<int>`/UTF-8/JSON pretty 규칙 준수, 빌드+회귀 PASS.
```
