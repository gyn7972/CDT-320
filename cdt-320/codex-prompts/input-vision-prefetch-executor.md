# 작업: Input Vision 촬영 실행자 분리 — 촬영/픽업 진짜 오버랩 (Vision Prefetch Executor)

## 목표 한 줄

Front 픽커가 Bottom 검사 중이고 Rear 픽커가 Place 중인 동안(픽커 둘 다 Input 존 밖),
**Input Vision이 다음 픽업 배치의 다이 촬영+옵셋 산출을 미리 끝내 놓도록** 촬영을 픽커
프로세스 체인에서 떼어 독립 실행자로 만든다. 픽업 시퀀스는 촬영 결과(허가)를 소비만 한다.

## 환경

- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 4.7.2, C# 7.3)
- 빌드: `/t:Build`만 사용(Rebuild/Clean 금지), OutDir `D:\Source\CDT-320_NEW\_build_check_handler\out`
- 인코딩: UTF-8 BOM + CRLF. 로그/주석 한국어, 시퀀스 반환 `Task<int>`(0=성공),
  `ConfigureAwait(false)`, `async void` 금지, C# 7.3 문법만(?. / 패턴매칭 금지)
- 주 수정/신규 파일:
  - **신규**: `QMC.CDT-320\Sequencing\InputVisionPrefetchSequence.cs` (촬영 실행자 — 이름은 관례에 맞게)
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (BuildPickBatch 소비 전환)
  - `QMC.CDT-320\Sequencing\Picker\InputDieVisionPrepareSequence.cs` (Grant 마무리 추가)
  - `QMC.CDT-320\Sequencing\Picker\InputCameraPickUpPermissionStore.cs` (필요 시 소폭 확장)
  - 실행자 기동 배선 파일 (D1에서 확인한 위치)
- **old-style csproj**: 신규 .cs 파일은 `QMC.CDT-320.csproj`에 `<Compile Include>` 수동 등록 필수.

## 배경 — 현재 구조 (조사 확정 사실, 수정 전 반드시 코드로 재확인)

### 촬영은 이미 별도 시퀀스지만, 픽업 체인 안에서만 실행된다
- 촬영+옵셋 산출 정본: `InputDieVisionPrepareSequence` (촬영 위치 이동, AutoVisionRequestService
  correlated 촬영, VisionOffset 산출, 촬영 전 픽커 Input 존 이탈 인터락까지 전부 보유).
- 그러나 실행 지점은 `PickerPickUpSequence.PrepareInputDieVisionBatchAsync`(약 454행,
  `BuildPickBatch` 스텝) **내부**: `new InputDieVisionPrepareSequence(...)` 생성·`RunAsync` 후
  `PreparedItems`를 `_pickBatchItems`로 복사(약 482-498행)하고 `CalculatePickTargets`(513행)로 점프.
- 결과: 해당 사이드가 PickUp phase에 진입해야 촬영이 시작된다. Front=Bottom ∥ Rear=Place인
  시간에는 비전/스테이지가 유휴 — 이것이 이번 작업이 없애려는 낭비다.

### 이미 존재하는 기반 (재사용 대상 — 새로 만들지 말 것)
- **핸드오프 계약**: `InputCameraPickUpPermissionStore` (internal static, Side별
  `List<InputDieVisionPreparedItem>` 보관). `Grant`(19행) / `TryConsume`(46행) /
  `HasPermission`·`HasAnyPermission`(84·96행) / `Clear`(124행, ReleaseItems로 예약 자동 해제).
  발행 선례: `InputCameraMarkInspectionSequence.cs:470` `Grant(Side, _inspectedItems)`.
  소비 선례: `PickerPickUpSequence.cs:538` `TryConsume` → 배치 구성(581-596행) →
  VisionX가 Avoid 아니면 롤백 `Grant`(566행).
- **Phase 매트릭스는 이미 오버랩 허용**: `PickerPhaseCoordinator.cs:170-174` (Bottom ∥ 상대 Place
  허용), `:184-186` (Place ∥ 상대 비Place 허용). 촬영은 픽커 축을 쓰지 않으므로 **픽커 phase
  매트릭스에 편입하지 않는다** (매트릭스 수정 금지 — D4).
- **촬영측 인터락**: `InputDieVisionPrepareSequence.cs:919-1013`
  `WaitFrontRearPickerInputZonesClearBeforeVisionAsync`(촬영 전 양측 픽커 Input 존 이탈 대기),
  `:1132-1191` 허가 잔존 시 VisionX 이동 보류. 그대로 재사용.
- **InputStageArea lease**: `SequenceResourceKind.InputStageArea`. 픽업이
  `PickerPickUpSequence.cs:371→383-424`(`AcquireInputStageAreaForPickUpAsync`)에서 취득,
  InputSequence(웨이퍼 로드/언로드)도 동일 리소스를 취득 — 상호 배제 메커니즘으로 재사용.
- **완료 신호**: `InputStageDieComplete` 발행(`PickerPickUpSequence.cs:7189`,
  마지막 픽 안전 복귀 후)은 **픽업 시퀀스에 그대로 남긴다**. 촬영 실행자는 절대 발행하지 않는다.
- 안전 불변식: `QMC.CDT-320\Sequencing\SEQUENCE_SAFETY_ANALYSIS.md` S-01/S-02
  (InputStage 물리 점유 상호 배제) — 회귀 기준.

### 유닛 실행 루프 구조 (실행자 기동 위치의 근거)
`FrontPickerSequence.cs:20`(`ExecuteAutoAsync`)처럼 `UnitSequenceBase` 상속 클래스가
무한 루프로 `WaitForPickerWorkAsync` → `new PickerProcessSequence(...)` 실행. Front/Rear
유닛 루프가 어디서 생성·기동되는지(MachineController 또는 부트스트랩) 검색해 확인하고,
**같은 방식으로 촬영 실행자 루프를 등록**한다.

## 요구사항

### R1. 촬영 실행자 신설 (`InputVisionPrefetchSequence` + 기동 루프)
- Front/Rear 픽커 유닛 루프와 **병렬로 도는 독립 루프**로 기동한다 (D1).
- 1회 사이클: 트리거 조건 판정 → InputStageArea lease 취득 → 대상 사이드 결정(D2) →
  `InputDieVisionPrepareSequence` 실행(촬영+옵셋 산출) → 성공 항목
  `InputCameraPickUpPermissionStore.Grant(side, items)` → lease 해제 → 대기 후 반복.
- **트리거 조건 (전부 만족 시에만 기동)** — 별도 판정 함수로 분리해 단위 검증 가능하게:
  1. 오버랩 모드 플래그 ON (R5)
  2. `Context.Bus.IsSet("InputStageReady")`
  3. `MaterialStateService.HasReadyInputStagePickTarget()` (예약 가능 다이 존재)
  4. 대상 사이드에 미소비 허가 없음 (`InputCameraPickUpPermissionStore.HasPermission(side) == false`)
  5. 웨이퍼 완료 드레인 중 아님 (기존 `WaferCompletion` 관례 — 픽업의
     `ShouldBlockNewPickForWaferCompletion` 판정 로직 참조)
  6. 픽커 Input 존 이탈 확인은 `InputDieVisionPrepareSequence` 내부 인터락에 위임
     (실행자에서 중복 구현하지 않음)
- 조건 미충족 시 100ms 내외 폴링 대기. 알람/중지 시 안전 종료. Abort 배선 포함.

### R2. InputStageArea lease를 실행자가 직접 취득
- 촬영 시작 전 취득, Grant(또는 실패 정리) 후 **반드시 해제** (try/finally).
- 이로써: 촬영 중 픽업이 진입하면 픽업의 기존 취득 지점(371행)에서 자연 대기,
  웨이퍼 교체(InputSequence)와도 자동 상호 배제. **픽업의 lease 로직은 수정하지 않는다.**
- lease 취득 대기에 타임아웃을 두지 말 것(경합은 정상 상황) — 단 ct 취소는 즉시 반영.

### R3. `InputDieVisionPrepareSequence` 마무리에 Grant 연결
- 실행자 경로에서 실행이 성공하면 `PreparedItems`를 `Grant(side, items)`로 발행.
  (시퀀스 내부에 넣을지 실행자에서 호출할지는 구현 판단 — 단 기존
  `PickerPickUpSequence` 내부 호출 경로가 오버랩 OFF일 때 Grant를 타지 않도록 분리할 것.)
- 부분 실패(일부 다이 촬영 실패) 시 성공 항목만 Grant, 실패 항목은 기존 관례대로
  예약 해제(`MaterialStateService` / `ReleaseItems` 경로). 전체 실패 시 Grant 없이 정리.

### R4. 픽업 시퀀스 소비 전환 (`PrepareInputDieVisionBatchAsync`)
- **오버랩 모드 ON일 때**: 내부 `InputDieVisionPrepareSequence` 실행을 건너뛰고
  `TryConsume` 경로(기존 538-596행 로직)로만 배치를 구성한다.
  - 허가가 아직 없으면: **허가 발행 대기 폴링**(타임아웃 = 신설 config,
    기본 30000ms). 타임아웃 시 **폴백으로 기존 내부 촬영 1회 실행** 후 진행 (D3 —
    오버랩 실패가 생산 정지로 이어지지 않게).
  - 기존 방어 유지: TryConsume 후 VisionX Avoid 미충족 시 롤백 Grant(566행 관례).
- **오버랩 모드 OFF일 때**: 기존 흐름 100% 동일 (내부 실행 유지). 회귀 zero.

### R5. 오버랩 모드 플래그 + 설정
- 신설 config (기존 스토어/직렬화 관례에 맞는 위치 — VisionConfig 또는 시퀀스 옵션 계열
  검색 후 결정): `UseInputVisionPrefetch`(기본 **false**),
  `InputVisionPrefetchConsumeTimeoutMs`(기본 30000), `InputVisionPrefetchIdlePollMs`(기본 100).
- UI 연동은 범위 외. 직렬화 하위호환 유지.

### R6. 완료/드레인 규칙 유지
- `InputStageDieComplete` 발행은 픽업 시퀀스(7189행)에만 — 실행자/촬영은 발행 금지.
- 웨이퍼 드레인 시 기존 `Clear(Side)`(픽업 7294행, 유닛 루프, 코디네이터) 동작 유지.
  실행자는 드레인 감지 시 신규 촬영 기동 금지(R1 조건 5).
- 실행자가 촬영 중 드레인/Abort가 걸리면: 촬영 중단 → 예약 해제 정리 → lease 해제.

## 결정사항 (박제 — 구현 중 변경 금지, 변경 필요 시 보고)

- **D1. 기동 계층**: Front/Rear 픽커 유닛 루프와 동급의 독립 루프. 기존 유닛 루프 등록
  지점을 검색해 동일 패턴으로 배선한다. `PickerProcessSequence` 내부 병렬 태스크로 만들지
  않는다(사이드 체인에 묶이면 오버랩 목적 상실).
- **D2. 대상 사이드 결정 정책(초기 구현)**: Front→Rear 순회. "해당 사이드 허가 없음 +
  해당 사이드가 현재 PickUp phase 아님"인 첫 사이드를 대상으로 한다. 정교한 예측
  스케줄링은 범위 외(후속 작업).
- **D3. 소비 타임아웃 폴백**: 픽업의 허가 대기 타임아웃 시 기존 내부 촬영으로 폴백.
  Fail로 세우지 않는다.
- **D4. 픽커 phase 매트릭스 무수정**: 촬영 실행자는 phase에 편입하지 않고
  InputStageArea lease + 기존 존 인터락만으로 배제한다.
- **D5. 레거시 정리 범위 외**: `PickerPickUpSequence` 인라인 비전 스텝 7개(652-1016행)와
  인라인 촬영(7478-7568행)은 이번 작업에서 **건드리지 않는다** (별도 정리 작업).
- **D6. 수동(UI) 경로 범위 외**: `PickerWorkInfoPageRuntime` 수동 픽업은 기존 동작 유지
  (오버랩 모드와 무관하게 기존 경로).
- **D7. 예약 유효성**: 촬영~소비 사이 스테이지가 움직이면 옵셋이 무효가 될 수 있다.
  기존 VisionX-Avoid 롤백 검증(566행)을 유지하되, 추가 무효화 규칙(스테이지 이동 감지 등)은
  범위 외 — 대신 "허가 발행 이후 InputStage 축을 움직이는 주체가 lease 배제상 존재할 수
  없음"을 구현 후 검토·보고한다 (InputSequence가 lease를 잡으면 웨이퍼 교체 = 허가 무효
  상황이므로, InputSequence 쪽 기존 Clear 경로가 커버하는지 확인해 보고).

## 실패 처리

- 실행자 내부 실패는 픽커/스테이지 시퀀스를 세우지 않는다: 로그(WriteLog, 한국어,
  Start/Ok/Failed 관례) + 예약/lease 정리 후 다음 사이클 재시도.
- 연속 실패 시 폭주 방지: 같은 사이드 연속 N회(기본 3회) 실패하면 해당 사이드 촬영을
  일정 시간(기본 5000ms) 보류하고 로그 경고.
- 픽업 소비 경로의 실패 처리(Fail 코드)는 기존 관례 유지.

## 검증 (수용 기준)

- V1. 솔루션 빌드 통과(`/t:Build`, 지정 OutDir), 신규 경고 0. 신규 파일 csproj 등록 확인.
- V2. **오버랩 OFF 회귀**: `UseInputVisionPrefetch=false`에서 기존 흐름과 diff 수준으로
  동일 동작(BuildPickBatch 내부 촬영 실행 유지, 실행자는 기동돼도 즉시 대기만).
- V3. 트리거 판정 단위 검증: 판정 함수를 하네스(리플렉션)로 호출해 6조건 조합별
  기동/비기동 판정 확인 (InputStageReady 미설정, 허가 잔존, 드레인 중, 타겟 없음 등).
- V4. Grant→TryConsume 왕복 검증: 실행자 경로로 Grant된 항목을 픽업 소비 로직이
  `_pickBatchItems`로 정확히 복원하는지 (PickTarget/VisionOffset 필드 보존, 깊은 복제).
- V5. lease 상호 배제 검증: 실행자가 lease 보유 중 픽업의 취득 시도가 대기하는지
  (시뮬 또는 하네스 수준에서 리소스 매니저 직접 검증).
- V6. 소비 타임아웃 폴백 검증: 허가 미발행 상태에서 타임아웃 후 내부 촬영 폴백 경로 진입.
- V7. 드레인/Abort 정리 검증: 촬영 중 취소 시 예약 해제 + lease 해제 + Grant 미발행.
- V8. 보고 항목: D7 검토 결과(허가 무효화가 기존 Clear 경로로 커버되는지),
  실행자 기동 지점(D1)에서 실제 배선한 위치, 오버랩 성립 시나리오
  (Front=Bottom ∥ Rear=Place 중 촬영 기동) 로그 근거.

## 진행 방법

계획 → 체크리스트 작성(`cdt-320\codex-work\10_input-vision-prefetch_checklist.md`) →
구현 → 체크리스트 확인(실패 시 구현→확인 3회 반복) → 작업 내용 레포트.
