# CDT-320 설계 — 피커 선입선출(FIFO) 재시작 드레인 타이브레이크

- 작성일: 2026-08-13
- 선행 문서: [02_code_analysis.md](cdt-320__picker-fifo__02_code_analysis.md)
- 팀장님 결정(2026-08-13, 정정 반영): **1차 키 = InputSequenceNo(투입 순번)**,
  2차 키 = PickedAt(픽업 시각), 최종 폴백 = Front(기존 동작)
  - 단, 투입 순번은 **웨이퍼마다 1부터 재시작**하므로(MaterialStateService.InputPick.cs:1005),
    순번 비교는 **두 픽커의 다이가 같은 입력 웨이퍼일 때만** 유효하다. 서로 다른 웨이퍼면
    2차 키(PickedAt)로 판정한다 — 먼저 픽업된 쪽 = 먼저 투입된 웨이퍼 쪽이므로 결과적으로
    투입 순서와 일치한다.
- 상태: 설계 확정 대기 → 승인 후 구현 (시퀀스 코드이므로 무조건 승인 필요)

---

## 1. 2차 분석(검증) 결과 — 1차 분석에서 확인 못 했던 사실

### 1-1. InputSequenceNo(1차 키) 특성

- 부여: 맵 승인 시 웨이퍼 내 픽업 순서대로 1-base 부여(InputPick.cs:1004~1005). 0 = 미부여.
- **웨이퍼 경계에서 재시작** → 단독 비교 불가. 웨이퍼 동일성 판정이 필요하다.
- 웨이퍼 동일성 키: `DieMaterial.InputWaferInstanceId` — 맵 적용/매핑/스토리지 로드 전 경로에서
  `EnsureWaferInstanceId`로 채워짐(InputStageDieMapApplyService.cs:580,
  InputStageDieMappingSequence.cs:2320, MaterialStorage.cs:339 등). 비어 있으면
  `WaferID_Input`으로 폴백 비교.

### 1-2. PickedAt(2차 키) 기록/소거 전수 확인

**기록 지점 (픽커에 다이가 올라가는 모든 경로):**

| 경로 | 위치 | 기록 |
|---|---|---|
| 자동 픽업 커밋 (`PickDie`) | MaterialStateService.InputPick.cs:3018 | `PickedAt = DateTime.Now` |
| 수동 헤드 편집 (`ManualInputDieToPicker`) | MaterialStateService.InputPick.cs:2716 | `PickedAt = DateTime.Now` ← 수동 편집도 기록됨 |

**소거(MinValue) 지점 — 전부 "픽커 위가 아닌 상태"로의 전이라서 무해:**

| 경로 | 위치 | 비고 |
|---|---|---|
| 수동 재픽업 준비 | InputPick.cs:3370 | 다이가 InputStage로 복귀 — 픽커 보유 아님 |
| Output 쪽 재구성 | OutputReceive.cs:1491 | 픽커 보유 아님 |
| 맵 적용 초기화 | InputStageDieMappingSequence.cs:2339, InputStageDieMapApplyService.cs:599 | 픽커 보유 아님 |

→ 픽커 위 다이의 PickedAt은 항상 유효한 것이 정상. 무효 가능성은 필드 도입 전 구버전
스냅샷 복원 정도.

**유효성 판정 관례**: `MaterialStateCompactor.cs:613`이 이미
`PickedAt > new DateTime(1900, 1, 1, 23, 59, 59)` 기준을 쓴다 → 동일 기준 재사용.

**영속화**: 스냅샷 저장(MaterialSnapshotStore.cs:1132) / 로드 정규화(:1771) 확인 —
앱 재기동 후에도 InputSequenceNo·PickedAt 모두 유지됨.

### 1-3. 기존 양보 로직과 무간섭 확인

- `RearPickerSequence.YieldInputPickupPriorityToFrontAsync`(RearPickerSequence.cs:603)는
  Rear가 **빈 픽커**일 때 Front에 픽업 우선권을 양보하는 로직인데, 이미
  ① `IsResumeDrainRequired(Rear)`면 스킵(:610), ② `HasLoadedDieOnPicker()`면 스킵(:618).
- `TryYieldExpectedSideToPriority` 자체도 재시작 드레인 활성 중이면 거부
  (PickerFirstForwardSequencer.cs:179~185).
- → 양쪽 다이 보유 재시작 시나리오에서 양보 로직은 개입하지 않는다. 수정 불필요, 충돌 없음.

### 1-4. 주의: `GetPickerDieSortTime` 재사용 금지

`GetDieAtPicker`의 정렬 키(`GetPickerDieSortTime`, DieOps.cs:400~408)는
`max(UpdatedAt, PickedAt)`인데 UpdatedAt은 모든 상태 변경에서 갱신되므로 FIFO 비교에
쓰면 오염된다. **2차 키 비교는 반드시 `die.PickedAt` 원본 필드만 사용한다.**

### 1-5. 드레인 중단 후 재시작 안전성

드레인 진행 중 알람으로 `CompleteResumeDrain`이 호출되지 못해도, 다음 START의
`BeginRun()`(PickerFirstForwardSequencer.cs:45~57)이 드레인 상태 전체를 초기화하고
`ConfigureRestartPickerDrain`이 재산정한다 → 잔여 상태로 인한 교착 없음. (기존 메커니즘 그대로)

### 1-6. 알려진 한계 (보고)

- 2차 키 PickedAt은 `DateTime.Now` 벽시계 기준 — 시스템 시각 변경(NTP 보정 등)이 픽업
  사이에 일어나면 역전 가능. 1차 키(같은 웨이퍼 투입 순번)가 대부분을 커버하므로 2차 키가
  실제 판정하는 경우는 웨이퍼 경계/구버전 스냅샷뿐 → 위험 극히 낮음, 수용 제안.
- 한 픽커가 서로 다른 웨이퍼의 다이를 동시 보유하는 경우는 현 드레인 정책(보유 다이 우선
  소진 후 다음 배치)상 발생하지 않는 것으로 판단 — 대표 다이 규칙(§2-1)으로 방어만 해 둔다.

---

## 2. 설계

### 2-1. FIFO 키 산출 — `AutoSequenceCoordinator.ResolveRestartPickerDrain` 확장

side별로 pickerNo 1~4 순회(기존 :485~494 루프 재사용)하며 **IsInputTarget 다이만** 집계:

- `minSequenceNo` = 유효(>0) InputSequenceNo 중 최솟값
- `waferKey` = minSequenceNo 대표 다이의 `InputWaferInstanceId`(비면 `WaferID_Input`)
- `earliestPickedAt` = 유효(1900 기준 초과) PickedAt 중 최솟값

non-target 다이만 보유한 side는 키 없음(→ 비교 시 하위 단계로 폴백).

키 운반용 구조체(신규, PickerFirstForwardSequencer.cs 내):

```csharp
internal struct PickerResumeDrainFifoKey
{
    public bool HasSequenceNo;    // 유효(>0) InputSequenceNo 보유 여부
    public int SequenceNo;        // 보유 target 다이 중 최소 InputSequenceNo
    public string WaferKey;       // 위 대표 다이의 InputWaferInstanceId(폴백 WaferID_Input)
    public bool HasPickedAt;      // 유효 PickedAt 보유 여부
    public DateTime PickedAt;     // 보유 target 다이 중 가장 이른 PickedAt
}
```

### 2-2. 시그니처 확장 — `PickerFirstForwardSequencer.ConfigureResumeDrain`

```csharp
public static void ConfigureResumeDrain(
    bool frontRequired, int frontRank, PickerResumeDrainFifoKey frontFifoKey,
    bool rearRequired,  int rearRank,  PickerResumeDrainFifoKey rearFifoKey)
```

- 키는 side별 static 저장소에 보관, `BeginRun()`/`ConfigureResumeDrain()` 진입 시 초기화.
- 키는 **configure 시 1회 캡처·고정**(드레인 대기 중 재계산 없음 — 픽커 위 다이는 드레인
  시작 전 변하지 않으며, "계산식 내 1회 캡처" 관례를 따름).

### 2-3. 동률 타이브레이크 교체 — 2곳, 공용 비교 함수 1개

신규 공용 비교 함수 (양쪽 rank 동률일 때만 호출):

```
IsResumeDrainFifoWinnerNoLock(side, otherSide):
  1) 1차(투입 순번): 양쪽 모두 HasSequenceNo
       && 양쪽 WaferKey 비어있지 않고 동일(OrdinalIgnoreCase)
       && SequenceNo 다름
     → 작은 SequenceNo 쪽 승
  2) 2차(픽업 시각): 양쪽 모두 HasPickedAt && PickedAt 다름 → 이른 PickedAt 쪽 승
  3) 폴백: Front 승 (기존 동작 그대로)
```

적용 지점 (동률 분기만 교체, rank 비교는 무변경):

| 위치 | 현재 | 변경 후 |
|---|---|---|
| `IsHighestResumeDrainPriorityNoLock` (PickerFirstForwardSequencer.cs:386~389) | 동률 → Front 고정 | 동률 → FIFO 비교 |
| `IsHigherResumeDrainPriorityNoLock` (:429~433) | 동률 → Front 고정 | 동률 → FIFO 비교 (**반드시 동일 함수** — Expected 사전설정과 홀더 결정이 어긋나면 안 됨) |

**변경하지 않는 곳**: 첫 전진 게이트 `IsHighestPriorityNoLock`(:352~354)의 Front 동률 규칙.
드레인 활성 중에는 Expected가 드레인 홀더로 제한되어 이 게이트는 드레인 결정에 종속되고,
신규 시작(다이 없음)에서는 FIFO가 무의미하므로 재시작 FIFO 요구는 드레인 쪽 수정만으로 충족된다.

### 2-4. 로그 계측 (실런 1회로 판정 근거 확정)

1. **키 산출 로그** — `ConfigureRestartPickerDrain` 기존 로그(AutoSequenceCoordinator.cs:409~415)에
   side별 `minSequenceNo`·`waferKey`·`earliestPickedAt`(ms 포함 포맷)·집계 다이 수 추가.
2. **판정 결과 로그** — `ConfigureResumeDrain` 직후, 양쪽 required일 때 확정 순서와 판정 근거
   (`tiebreak=SequenceNo|PickedAt|FrontFallback`, 비교 수치 포함)를 1줄로. 사전 Expected 설정
   (`ConfigureExpectedForNextResumeDrainNoLock`) 결과와 일치해야 한다.
3. **대기/획득 로그 보강** — `BuildResumeDrainWaitReasonNoLock`(:442~452)과
   `DescribeResumeDrainSideNoLock`(:454~461)에 FIFO 키 요약 포함 → 드레인 턴
   확보/보류 로그(PickerProcessSequence.cs:405~416)에 자동 반영됨.

### 2-5. 논리 워크스루 (시나리오 표)

| # | 상황 | rank | 판정 | 결과 |
|---|---|---|---|---|
| 1 | 같은 웨이퍼: Rear #5~8, Front #9~12 | 2:2 | 1차 순번: 5 < 9 | **Rear 먼저** (요구사항 핵심) |
| 2 | 같은 웨이퍼: Front가 빠른 순번 | 2:2 | 1차 순번: Front 작음 | Front 먼저 (기존과 동일 결과) |
| 3 | Front만 다이 보유, Rear 빈 픽커+준비 픽 대상 | 2:1 | rank 차 — FIFO 미사용 | Front 드레인 → Rear (기존 동작) |
| 4 | 서로 다른 웨이퍼의 다이 보유(교체 경계) | 2:2 | 1차 판별 불가(웨이퍼 다름) → 2차 PickedAt | 먼저 픽업된 쪽 = 먼저 투입된 웨이퍼 쪽 먼저 |
| 5 | 한쪽 순번 무효(0, 구버전/특수) | 2:2 | 1차 불가 → 2차 PickedAt | 이른 픽업 쪽 먼저 |
| 6 | 양쪽 모두 키 무효 | 2:2 | 폴백 | Front 먼저 (기존과 동일) |
| 7 | 한쪽이 non-target 다이만 보유(회수 대상) | 2:2 | 그쪽 키 없음 → 2차→폴백 | 폴백 경로로 판정 |
| 8 | 드레인 중 알람 재발 → 재시작 | — | BeginRun 초기화 후 재산정 | 교착 없음 (기존 메커니즘) |
| 9 | Rear 양보 로직과의 간섭 | — | 드레인 활성/다이 보유 가드로 미개입 (§1-3) | 무간섭 |

## 3. 변경 파일 요약 (이것만)

| 파일 | 변경 |
|---|---|
| `QMC.CDT-320\Sequencing\Picker\PickerFirstForwardSequencer.cs` | FIFO 키 구조체·저장소·비교 함수 신설, `ConfigureResumeDrain` 시그니처 확장, 동률 분기 2곳 교체, 대기 사유 문자열 보강, `BeginRun` 초기화 추가 |
| `QMC.CDT-320\Sequencing\AutoSequenceCoordinator.cs` | `ResolveRestartPickerDrain`에서 FIFO 키 산출(out 추가), `ConfigureResumeDrain` 호출부 갱신, 로그 보강 |

- 모션 코드 변경 없음(순수 중재 로직) → MotionSpeedScale 대상 이동 코드 신규 작성 금지.
- MaterialStateService·PickerPhaseCoordinator·물리 인터락·양보 로직 무변경 (읽기만).

## 4. 팀장님 확인 필요 잔여 사항

1. 서로 다른 웨이퍼일 때 2차 키(PickedAt)로 판정하는 규칙 — "먼저 투입된 웨이퍼 먼저"와
   결과가 일치하므로 투입 순번 원칙에 부합한다고 판단. 이대로 갈지.
2. non-target 다이만 보유한 side가 폴백(Front 우선) 경로로 가는 것 — 회수 대상 다이는
   Place 순서 개념이 없어 폴백이 합리적이라고 판단. 이대로 갈지.
3. 첫 전진 게이트(신규 시작·비드레인 동률)의 Front 고정은 이번 범위에서 유지 — 향후
   일반 운전 중에도 FIFO를 확장할지는 별도 건.
