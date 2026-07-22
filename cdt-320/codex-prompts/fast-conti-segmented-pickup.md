# 작업: FastContiSegmentedPickUp — 픽업 시퀀스 고속 모드 신설 (완전 신규 작성)

## 환경
- 저장소: `D:\Source\CDT-320_NEW` (C# WinForms, .NET Framework 솔루션)
- 주 수정 파일:
  - `QMC.CDT-320\Sequencing\Picker\PickerPickUpSequence.cs` (신규 모드 시퀀스 본체)
  - 픽업 모션 설정 클래스 (`PickerPickUpMotionConfig` / `PickerPickUpTransferMotionMode` enum이
    정의된 파일 — 검색해서 확인)
  - 필요 시 `QMC.CDT-320\Equipment\Ajin\AjinAxis.cs` (벨로시티 오버라이드 헬퍼가 없으면 추가)
- **기존 `ContiSegmentedPickUp` 모드와 default 모드는 절대 수정하지 않는다.** 새 모드는 별도
  분기로 추가한다.

## 배경 — 기존 구조 (수정 전 반드시 읽을 것)

### 기존 픽업 이송 분기
`MovePickerXStageYPickerTAsync()` (약 1498행)에서 `pickUpConfig.TransferMotionMode`가
`ContiSegmentedPickUp`이면 `MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync()`
(약 1660행)로, 아니면 default 경로로 분기한다. 기존 conti 경로의 상세 분석은
`cdt-320\pickup-sequence-conti-segmented-analysis.md` 참조.

### 활용할 기존 인프라
- `AjinAxis.TryOverridePosition(target, vel, acc, dec)` — 이동 중 목표 변경(포지션 오버라이드).
  시뮬/실장비 분기 완비.
- 벨로시티 오버라이드: `AXM.ModifyVelocity(axis, vel, acc, dec)` public 래퍼가 이미 존재
  (`QMC.Common\AjinE\AXM.cs` 약 2126행). `AjinAxis`에 `TryOverrideVelocity(vel, acc, dec)`
  헬퍼가 없으면 `TryOverridePosition`과 같은 구조(시뮬 분기: `base.OverrideVelocity`,
  실장비 분기: `AXM.ModifyVelocity`, IsMoving 확인, lock, FailMotion 관례)로 추가한다.
- `MotionSpeedScale` — 디폴트 속도/가감속 퍼센트 스케일.
- `BaseAxis.IsInPosition` / `IsMoving` / `ActualPosition` — 감시 루프용.
- 기존 config 항목: `PickerZPrePickDistance`, `PickerZSlowApproachSpeedPercent`,
  `PickUpNeedleSyncLiftDistance`(InputStage.Config), `SyncLiftSettleMs`/
  `PickUpNeedleSyncLiftSettleMs`, `VacuumOnBeforePickDelayMs`(로그상 존재 — 실명 확인),
  `PickerSafeForWaferStageDistance`.
- EjectPinZ 티칭: `stage.Recipe.EjectPinZ.ProcessPosition` / `AvoidPosition`
  (`ResolveEjectPinZPickTarget()`과의 관계를 코드에서 확인하고 "Process 높이"로
  `Recipe.EjectPinZ.ProcessPosition`을 사용한다. 다르면 보고).

## 요구사항

### R1. 모드 플래그 신설
- `PickerPickUpTransferMotionMode` enum에 `FastContiSegmentedPickUp` 추가.
- `MovePickerXStageYPickerTAsync`에서 이 모드일 때 신규 함수
  (`MovePickerXStageYPickerTByFastContiSegmentedPickUpAsync` — 이름은 관례에 맞게)로 분기.
- 설정 UI 연동은 범위 외. enum 직렬화 호환(기존 값 순서 유지)만 보장.

### R2. 신설 설정 파라미터 (`PickerPickUpMotionConfig`에 추가, 기존 스토어에 저장)
| 파라미터 | 기본값 | 용도 |
|---|---|---|
| `FastPickerZSafePosition` | **-3.0** | 사이클 시작 시 PickerZ 안전 판정 기준 (현재위치 > 이 값이면 OK) |
| `FastPickerXApproachDistance` | **20.0** | PickerZ PrePick 하강 시작 트리거: PickerX 잔여거리 판정 |
| `FastContactSlowZoneDistance` | **0.3** | PickPosition 도달 전 저속 전환 구간 거리 |
- `Ensure()`에서 0 이하/비정상 값 보정. 기존 config 직렬화 하위호환 유지.

### R3. FastContiSegmentedPickUp 사이클 (피커 1개, 4→1 반복) — 아래 순서를 정확히 구현

배치 구성·비전 준비·좌표 계산(Phase A, CalculatePickTargets)은 기존 그대로 사용한다.
이 모드는 이송~Z 마무리 구간을 대체한다.

| # | 동작 | 종류 | 상세 |
|---|---|---|---|
| 0 | 선행 보정: PickerY + PickerT(해당 피커) 이동 | 병렬 명령+대기 | 기존 conti 선행 보정과 동일하되 **EjectPinZ를 Avoid로 내리지 않는다** (EjectPin은 Process 높이 상주 개념) ※결정사항 D4 |
| 1 | Input work area 점유 | 리소스 | `EnsurePickerWorkAreaReserved(Input)` |
| 2 | PickerZ 안전 확인 | 검사→필요 시 명령 | 4개 피커 Z 모두 `ActualPosition > FastPickerZSafePosition`이면 통과. 하나라도 미달이면 **4피커 Z 전부 Avoid로 비동기 이동 명령** 후 **각 축 `IsInPosition` 신호만 확인되면 완료 처리** (풀 모션돈 대기·최종 스냅샷 검증 없이) |
| 3 | **PickerX 이송 시작** | **비동기 명령** | 목표 `_targetPickerX`. await하지 않고 Task 보관 |
| 4 | EjectPinZ 확인 | 검사→필요 시 동기 이동 | `Actual < (Recipe.EjectPinZ.ProcessPosition + 0.1)`이면 패스. 아니면: 이동 중인지 확인 → 이동 중이 아니면 ProcessPosition으로 **동기 이동(명령+완료대기)** |
| 5-1 | **StageY ∥ NeedleX 이송** | **비동기 명령** | 두 축 완료 시점에 **Needle Vacuum ON** (완료 연속 처리로 연결) |
| 5-2 | **PickerX 접근 감시** | **대기(폴링)** | `abs(_targetPickerX − PickerX.Actual) < FastPickerXApproachDistance` 도달까지 10ms 폴링. PickerX 알람/조기정지 시 Fail. **5-1과 5-2는 동시 진행** |
| 6 | (5-2 충족 시) **PickerZ → PrePick 하강 시작** | **비동기 명령** | 속도 = PickerZ 공정 Vel/Acc/Dec × `MotionSpeedScale` 디폴트 스케일. 동시에 **Picker Vacuum ON** |
| 7 | 이송 합류 | **대기** | 3) PickerX 태스크 + 5-1) StageY/NeedleX(니들 배큠 ON 포함) 완료까지 대기 |
| 8 | **PickerZ → PickPosition** | 명령 or 오버라이드 | PickerZ가 **정지 상태**(PrePick 도달)면 새 Move 명령으로 PickPosition 이동. **이동 중**이면 `TryOverridePosition`으로 목표를 PickPosition으로 변경 |
| 8-1 | **컨택 전 저속 전환** | 감시→벨로시티 오버라이드 | PickerZ 위치 감시: `abs(PickPosition − Actual) <= FastContactSlowZoneDistance(0.3)` 구간 진입 시 **설정 저속(Contact 저속: `PickerZSlowApproachSpeedPercent` 기반 속도/가감속)으로 벨로시티 오버라이드** (`TryOverrideVelocity`). 이미 그보다 느리면 생략 가능. 이후 PickPosition 도달(InPosition) 대기 |
| 9 | **동기 상승 (SyncLift 대응)** | 병렬 명령→**InPosition만 확인** | EjectPinZ → `Process 높이 + LiftDistance` ∥ PickerZ → `PickPosition + LiftDistance`. 완료 판정은 **위치 비교 없이 각 축 IsInPosition 신호만** 확인. LiftDistance = 기존 `PickUpNeedleSyncLiftDistance` 재사용 ※결정사항 D1 |
| 10 | 정착 대기 | 대기(지연) | `max(VacuumBeforePickDelay, SyncLiftSettle)` ms |
| 11 | 흡착 확인 | 대기 | Flow ON 확인 (기존 `VerifyPickerFlowStateAsync` 관례) ※결정사항 D2 |
| 12 | **PickerZ → Avoid** | **비동기 명령** | await하지 않음 |
| 13 | PickerZ 이탈 감시 | **대기(폴링)** | `PickerZ.Actual > (PickPosition + PickerSafeForWaferStageDistance)` 도달까지. 도달 시점에 **Needle Vacuum OFF** ※결정사항 D3 |
| 14 | EjectPinZ → ProcessPosition | 명령+대기 | 완료 시 **픽업 1사이클 완성** |
| 15 | 자재 갱신 | — | 기존 `UpdateMaterialToPicker` 호출 (Flow/Data 재확인 포함) → 다음 피커(4→1). 12번의 PickerZ Avoid 태스크는 사이클 종료 전 observe 처리 |

### R4. 실패 처리
- 각 단계 실패 시 기존 `Fail(...)` 관례로 에러코드 반환 + **Z 축 안전 복귀**
  (`TryMovePickerNeedleAndEjectPinZToAvoidAsync` 계열 재사용).
- 폴백 없음: Fast 모드는 가드 폴백 없이 실패 시 시퀀스 Fail로 처리한다 ※결정사항 D5.
- 감시 루프(5-2, 8-1, 13)는 전부 타임아웃 필요 — `Max(TransferContiTimeoutMs, ResolveTimeout())`
  기존 관례를 따르고, 취소(ct)와 축 알람을 매 폴링마다 확인한다.
- 백그라운드 태스크(PickerX, StageY/NeedleX, PickerZ Avoid)는 어떤 종료 경로에서도
  unobserved exception이 남지 않게 observe한다.

### R5. 시뮬레이션 지원
- 명령 경로가 전부 기존 헬퍼(`Move...Async`, `TryOverridePosition`, `TryOverrideVelocity`)를
  경유하므로 `Config.IsSimulationMode`에서 그대로 동작해야 한다. 별도 분기를 만들지 말 것.

## 제가 결정한 사항 (사용자 미지정 — 검토 후 수정 가능하도록 명시)
- **D1**: "Lift Distance"는 기존 `PickUpNeedleSyncLiftDistance`(InputStage.Config)를 재사용.
- **D2**: Flow ON 확인은 정착 대기(10) 직후 수행.
- **D3**: Needle Vacuum OFF는 PickerZ가 stage-safe 거리를 벗어나는 시점(13)에 수행.
- **D4**: PickerY/PickerT 선행 보정은 기존 conti와 동일하게 X 이송 전에 수행 (EjectPinZ Avoid만 제외).
- **D5**: 가드/폴백 없음 — Fast 모드 설정 시 무조건 이 경로 실행, 실패는 Fail 처리.
- **D6**: "EjectPin Process 높이" = `Recipe.EjectPinZ.ProcessPosition`
  (`ResolveEjectPinZPickTarget()`과 값이 다르면 구현 전에 보고).

## 제약 사항
- 기존 default/ContiSegmentedPickUp 경로 코드 무변경 (분기 추가만).
- 코드 스타일: 기존 파일 관례 — 한국어 로그/주석, `WriteLog` Start/Ok/Failed 접미,
  `Fail`/`FailMotion` 에러 관례, 최신 C# 문법 자제.
- `_pickerZContactedByContiPickUp` 같은 기존 상태 플래그와 충돌하지 않게 Fast 모드 전용
  플래그/경로를 명확히 분리. 스텝 머신 전이(`CurrentStep`)는 기존 enum 안에서 처리
  (기존 conti처럼 이 함수가 Contact~Z 마무리까지 내부 수행하고 적절한 스텝으로 전이).
- Z 좌표 부호 방향(Avoid가 +방향인지)은 코드/티칭에서 확인 후 부등호를 맞춘다
  (스펙의 `> -3`, `> Pick+safe` 판정은 Avoid가 위(+) 기준).

## 검증 / 수용 기준
1. 솔루션 전체 빌드 통과.
2. 시뮬레이션 모드 하네스로 Fast 사이클 1회 실행 결과 보고:
   - 이벤트 순서가 R3 표와 일치 (특히: X 이송 중 잔여 20mm에서 Z 하강 시작,
     Z 이동 중 포지션 오버라이드로 PickPosition 변경, 0.3mm 구간에서 저속 전환,
     SyncLift 완료가 InPosition 신호 기준, Z Avoid 중 safe 거리 통과 시 EjectPin 이동)
   - 로그로 각 단계 타임스탬프 확인 가능할 것.
3. Z 안전 확인 분기: 피커 Z 하나를 -3 이하로 두고 시작 → 4축 Avoid 비동기 명령 +
   InPosition 확인 후 진행함을 확인.
4. EjectPinZ 분기 3케이스: (Process+0.1 미만 → 패스 / 초과+정지 → 동기 이동 /
   초과+이동중 → 대기 없이 통과 여부는 구현 확인) 각각 동작 보고.
5. PrePick 이동이 빨리 끝난 경우(신규 Move)와 이동 중인 경우(포지션 오버라이드) 양쪽 경로 검증.
6. 기존 ContiSegmentedPickUp/default 모드 회귀 없음 (설정 전환 시 기존 경로 그대로 동작).
7. 실패 주입(PickerX 알람, 오버라이드 실패 등) 시 Z 안전 복귀 후 Fail 반환 확인.
