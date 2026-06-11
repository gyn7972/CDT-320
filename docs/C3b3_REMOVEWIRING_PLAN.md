# C3b-3 (재설계) — 결선 제거 + 검사별 컨트롤러/페이지 Setup 지정 (Step 0 설계, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-c3b3-removewiring`(master=C3b-2 머지본 `1dd359b` 위).
> 구 `C3b3_WIRING_PLAN.md`(옵션 가/나/다)는 폐기 — 사용자 결정 **옵션 D: 결선(풀) 개념 자체 폐기**.
> **Step 0: 설계·감사 — 보고 후 정지. 설계 폭이 크니 1~7 전부 사용자 확인.** Step 0 보고 전 구현 금지.
> 목표: 채널 "사용 여부" = 레벨 0/양수로 암묵 표현. 검사가 쓰는 **컨트롤러/페이지는 노드 Setup 지정**, 레벨은 노드 Recipe. 컨트롤러 정의 유지.

---

## §A 감사 — GetWiring/AlgorithmWirings 소비처와 대체 (item 1·7)

| 소비처 | 현재(풀=AlgorithmWirings) | 옵션 D 대체 |
|---|---|---|
| `InspectionLightPanel.BindFieldsCore`(`:183-208`) | `cs.Channels` 순회로 행 생성 | 노드 Setup 지정(컨트롤러/페이지) → 컨트롤러 `ChannelCount` 로 채널 1..N 행 생성 |
| `InspectionLightPanel.ApplyControllerAsync`(`:272`) | `cs.Channels.Contains(ch)` 풀 검증 후 skip | **검증 제거** — `int[ChannelCount]` 전 채널 배열(미사용=0). `SetChannelBatchAsync(page, times)` 이미 이 shape |
| `InspectionLightPanel.Save`/`Wiring()`(`:113`) | `w.ControllerSets` → 노드 LightWirings 산출 | 제거(노드 LightWirings 폐기). Setup 지정은 별도 UI |
| `LightSystemSetupPage`(`:111`,`:487`) | 결선 표 편집(AlgorithmWirings 읽기/쓰기) | **결선 UI 제거**(컨트롤러 정의만 유지) |
| `LightSystemMigrator`(`:77`) | io_set 휴리스틱 → AlgorithmWirings 초기 제안 | 결선 제안부 제거 또는 노드 지정 도출로 전환(오세국 마이그) |

- **점등 로직(SetChannelBatchAsync)·컨트롤러 정의 불변.** 풀 검증만 제거(미사용=0 송신은 이미 동일 경로).

## §B 신규 노드 Setup "컨트롤러/페이지 지정" 모델 (item 2·3)
- **입도**: 구 `ControllerSets` 가 다중 → 검사 1개가 **N개 컨트롤러** 사용 가능. 각 컨트롤러는 페이지 1개(또는 채널별 페이지). 제안:
  ```
  // 노드 Setup (AlgoSetupBase): LightWirings(List<ControllerChannels>, 죽음) 대체
  [DataContract] class LightPageRef { ControllerPort:string; Page:int }      // 검사가 구동하는 (컨트롤러,페이지)
  List<LightPageRef> LightPages;   // N개
  ```
- **채널 열거**: 각 `LightPageRef` → `LightControllerEntry(ControllerPort).ChannelCount` → 채널 1..N. 레벨 그리드 행 = 그 채널들.
- **레벨**(노드 Recipe.LightSettings 유지): `InspectionLightSetting{ControllerPort, Channel, Level, On, Strobe, Stabilize, Page}`. Level 0 = 미사용. Page 는 Setup 지정과 일치(중복 시 Setup 가 권위, Recipe Page 는 그룹핑용 보조 또는 제거 — [확인2-b]).
- **미래 스트로브**: 페이지 트리거→전 채널 출력 = `int[ChannelCount]` 배열 shape 그대로 수용(트리거 구현 별도).

## §C UI 분리 (item 4)
- **SettingsPage**(검사 노드): **컨트롤러/페이지 지정** UI(Setup). 검사별 `LightPages` 추가/삭제(컨트롤러 콤보=인벤토리 PortName, 페이지 콤보=0..PageCount-1). 컨트롤러 *정의* 편집은 LightSystemSetupPage(분리 유지).
- **RecipePage**(target 페이지 VisionTargetPage/InspectorTargetPage + Finder/InspectorPage): **레벨 그리드**(Recipe). 행 = Setup 지정 컨트롤러/페이지의 채널 1..ChannelCount, 레벨 편집(0=미사용).
- **현 InspectionLightPanel**(레벨 그리드, 5 호스트) = RecipePage 측 레벨 편집으로 유지하되 행 출처를 **노드 Setup 지정 + ChannelCount** 로 재배선. SettingsPage 의 조명 노드는 레벨 그리드 → **지정 UI** 로 교체(또는 지정+레벨 탭).
- **LightSystemSetupPage**(오세국, 639줄): Section 2 결선 표(`_wiringModel`/`_gridSets`/wiring TreeView/RebuildWiringTree/BindSetsGrid/FlushSetsToModel ≈ 절반) **제거** → 컨트롤러 인벤토리만. ⚠ 대규모 공유코드 수정.

## §D 마이그레이션 (item 5)
- 최초 부팅: 구 `AlgorithmWirings` 풀 + 노드 Recipe `LightSettings`(ControllerPort/Page 존재) → 검사별 `LightPages` 도출:
  - 노드의 LightSettings 에 나오는 distinct (ControllerPort, Page) → 그 검사의 `LightPages`.
  - (조명 설정 없는 검사 = LightPages 빈 = 미사용.)
- 레벨은 노드 Recipe 유지(불변). 구 데이터(algorithm_camera.json 등)는 이미 C3a 은퇴.
- 노드 `LightWirings`(죽은 스냅샷)는 마이그 입력 아님 → 그냥 폐기.

## §E 은퇴 대상 (item 6, 참조 0 후)
- `LightSystemSetup.AlgorithmWirings` 필드 + `AlgorithmLightWiring` 타입(+ GetWiring/EnsureWirings 결선부).
- 노드 `AlgoSetupBase.LightWirings`(List<ControllerChannels>).
- `ControllerChannels` — 잔여 참조(LightSystemMigrator io_set 제안·테스트) 처리 후 제거 가능 여부 [확인6].
- `LightSystemMigrator` 결선 제안부(io_set → AlgorithmWirings) — 오세국 마이그, 제거/전환 [확인6].
- **유지**: `LightSystemSetup.Controllers`·`LightControllerEntry`·`LightSystemSetupStore`·`LightControllerMode`·`InspectionLightSetting`·컨트롤러 정의 일체.

---

## ★ 확인 필요 (Step 0 — 전부 사용자 결정)
1. **GetWiring 대체 매핑(§A)**: 행생성=ChannelCount, 점등=풀검증 제거(미사용0), 결선편집 UI 제거 — 이 방향 OK?
2. **노드 Setup 모델(§B)**: `List<LightPageRef>{ControllerPort, Page}`(검사당 N 컨트롤러/페이지). (b) Recipe.LightSettings 의 Page 는 유지 vs 제거(Setup 권위). 입도·Page 처리 확정.
3. **UI 분리(§C)**: SettingsPage=지정(Setup) / RecipePage=레벨(Recipe). InspectionLightPanel 레벨그리드 행출처 재배선 + SettingsPage 조명노드를 지정 UI 로. OK?
4. **LightSystemSetupPage 결선 UI 제거(§C, 오세국 639줄 ½)**: 대규모 공유코드 수정 — 진행 OK? **origin fetch + 오세국 조율 선결 필요.**
5. **마이그(§D)**: 노드 LightSettings 의 (Port,Page)→LightPages 도출. OK?
6. **은퇴 범위(§E)**: AlgorithmWirings/AlgorithmLightWiring/노드 LightWirings 제거 + ControllerChannels·LightSystemMigrator 결선부 처리 방식.

### ⚠ 팀 조율 / origin (필수 선결)
- `LightSystemSetup.cs`·`LightSystemSetupPage.cs`·`LightSystemMigrator.cs` = **오세국 편집 공유코드**. 로컬 master ahead origin 26·behind 0 이나 **마지막 fetch 캐시(`c3ddc15`)** 기준 — 그 후 오세국 푸시 가능성. **진입 전 `git fetch`(push 아님, 사용자 승인 후) + 오세국 조율 권장.**

## 정지 조건
GetWiring 강결합으로 재배선 불가 / 다중 컨트롤러·페이지 입도 모호 / 마이그 도출 불가 / 점등(미사용0) 오류 / R2 깨짐 / origin 미동기 충돌 → 즉시 정지·보고.
