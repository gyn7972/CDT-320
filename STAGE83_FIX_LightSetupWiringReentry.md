# Stage 83 Fix — LightSystemSetupPage 결선 그리드 재진입 예외

## 증상 / 예외

```
System.InvalidOperationException:
  'SetCurrentCellAddressCore 함수에 대한 재진입 호출을 발생시키므로 작업을 수행할 수 없습니다.'
   at LightSystemSetupPage.BindSetsGrid()  →  _gridSets.Rows.Clear()
```

### 재현 시퀀스 (사용자 보고)
1. `_gridSets` 의 셀(ControllerPort 콤보 또는 ChannelsCsv)을 편집 중
2. 좌측 `_treeWiring` 의 다른 알고리즘 노드를 클릭
3. `AfterSelect → OnAlgNodeSelected → BindSetsGrid()`
4. `_gridSets.Rows.Clear()` 가 편집 중 셀의 강제 EndEdit + CurrentCell 변경을 내부 유발 →
   이미 진행 중인 WinForms `SetCurrentCellAddressCore` 와 충돌 → **재진입 예외**

### 기존 가드의 한계
- `_suspendSets` 플래그는 `CellEndEdit → FlushSetsToModel` 의 cascade 호출만 차단.
- `Rows.Clear()` 자체가 내부적으로 강제 EndEdit + CurrentCell 변경을 호출하는 것은 못 막음 → 재진입의 직접 원인.

## 수정 (3중 안전망)

`QMC.Vision\Ui\Pages\LightSystemSetupPage.cs`

| 변경 | 내용 |
|------|------|
| 신규 필드 | `private bool _setsBindScheduled;` — BeginInvoke 중복 스케줄 가드 |
| `BindSetsGrid()` | ① `_setsBindScheduled` 중복 스케줄 가드 → ② `IsHandleCreated` 가드(미생성 시 `HandleCreated` 후 1회 재시도) → ③ `BeginInvoke((MethodInvoker)BindSetsGridCore)` 로 한 박자 뒤 실행 |
| 신규 `BindSetsGridOnHandleCreated()` | 핸들 생성 시 1회 호출 후 **자기 해제(unsubscribe)** — 핸들 재생성 시 누적/누수 방지 |
| 신규 `BindSetsGridCore()` | 실제 바인딩. `EndEdit()` + `CurrentCell = null` 명시 호출로 편집 상태 선정리 후 `Rows.Clear()` |

### 핵심 원리
**`BeginInvoke` 로 `BindSetsGridCore` 가 항상 새로운 최상위 메시지 디스패치에서 실행됨** → `Rows.Clear()` 가 어떤 호출 경로에서도 진행 중인 `SetCurrentCellAddressCore` 안에 중첩될 수 없음. 이것이 본 예외(재진입)의 정석 해법. 추가로 `EndEdit()`+`CurrentCell=null` 로 `Rows.Clear` 가 강제 EndEdit 할 필요 자체를 제거.

### diff 요약
`+29 / -2` (필드 1 + BindSetsGrid 재작성 + BindSetsGridCore/BindSetsGridOnHandleCreated 신규)

## 검증

| 항목 | 결과 |
|------|------|
| MSBuild (Debug) | **오류 0** / 경고 1 (선재 `System.IO.Ports` MSB3245 — master 기준에서도 동일, 본 수정과 무관) |
| 신규 경고/오류 | **0** (master stash 후 빌드와 대조하여 확인) |
| 무회귀 하니스 `ReentryTest.exe` | **PASS** — 노드선택 → 결선행 추가 → ChannelsCsv 셀 편집 중(`editingAtSwitch=True`) → 다른 노드 전환 → 5노드 연속 전환, 예외 없음 |
| `BindSetsGrid` 직후 `_gridSets.Rows.Count` 의존 코드 | **없음** — 호출처(AddControllerSet/DeleteControllerSet/DeleteController/TrimWiringPool)는 모두 직후 `RebuildWiringTree()` 호출이며 이는 `_wiringModel`만 읽음 → 비동기화 영향 없음 |

### ⚠️ 자동화 한계 — 실 GUI 시나리오 1~5 미자동검증
- 본 환경에서 **computer-use MCP 가 끊겨** 실제 마우스 클릭(포커스 이동)으로의 GUI 시나리오 1~5 자동 수행 불가.
- 프로그램적 하니스는 `tree.SelectedNode` 직접 설정 방식이라 **실 클릭의 포커스-이동 기반 `SetCurrentCellAddressCore` 중첩을 헤드리스로 충실히 재현하지 못함** → master/fixed 모두 PASS (버그 재현이 아닌 무회귀 스모크로만 유효).
- 따라서 시나리오 1~5(편집중 노드전환·CSV편집중·빠른연속·빈결선·정상저장회귀)는 **사용자 수동 클릭 확인 또는 computer-use 재연결 후 재검증** 필요. 단 수정은 구조적으로 해당 예외 클래스를 원천 차단함.

### 검증 하니스 위치
`cdt-320\light-setup-reentry-test\ReentryTest.cs` (+ 빌드된 `ReentryTest.exe`).
실행: `ReentryTest.exe "<QMC.Vision.exe 경로>"`.

## 빌드 대상 메모
- 수정 파일은 sln 빌드 대상인 **top-level** `QMC.Vision\Ui\Pages\LightSystemSetupPage.cs`.
- `QMC.CDT-320\QMC.Vision\Ui\Pages\LightSystemSetupPage.cs` 는 동일 내용 사본이나 sln 미참조(빌드 비대상) — 미수정.

## 커밋
- 로컬 커밋만. **remote push 안 함 — 사용자 컨펌 대기.**
