# Front 픽업 — ContiSegmentedPickUp 경로 명령/대기 순서 분석 (피커 4→1)

- 대상: `PickerPickUpSequence.MovePickerXStageYPickerTByContiSegmentedPickUpOrDefaultAsync` (1660행)
  + `RunPickupZMotionAfterContiContactAsync` (4302행)
- 조건: `PickerPickUpMotionConfig.TransferMotionMode == ContiSegmentedPickUp`
- 비전 준비(Phase A)와 배치 구성은 표준 경로와 동일 (`pickup-sequence-front-command-wait-analysis.md` 참조).
  이 문서는 **피커 1개의 이송~픽업 구간**만 다룬다. 피커 4→1로 반복.
- 표기: [명령] = 축 이동/IO 명령 송신, [명령+대기] = 명령 후 완료(InPosition)까지 await,
  [비동기 명령] = 태스크로 시작만 하고 await하지 않음, [대기] = 신호·합류·지연 대기.

## 핵심 개념

ContiSegmentedPickUp은 이송과 하강을 **오버랩**시키는 모드다:
- PickerX / StageY / NeedleX 3축을 **동시에 비동기로** 이송하면서,
- PickerX가 트리거 위치에 도달하는 순간 **PickerZ PrePick 하강을 이송 중에 시작**하고,
- 이송 합류 직후 Contact(저속 하강)까지 이 스텝 안에서 끝낸다.
- 이후 Z 모션 루틴(`RunPickupZMotionAfterContiContactAsync`)은 **SyncLift부터** 시작한다.
- 진입 조건(가드) 불충족 시 언제든 표준(default) 경로로 폴백한다.

## 명령/대기 순서표 — 피커 1개 사이클

### ① 이송+Contact (`MovePickerXStageYPickerT` 스텝, conti 분기)

| # | 동작 | 축/IO | 종류 | 비고 |
|---|---|---|---|---|
| 1 | Z축 안전 확인 | PickerZ들 | [명령+대기] 필요 시 | `EnsureZAxesAtAvoidBeforePickerMove` (이미 Avoid면 생략) |
| 2 | StageT 얼라인 각도 확인 | StageT | [명령+대기] 필요 시 | 이미 위치면 생략 |
| 3 | **가드 1**: conti 진입 조건 검사 | — | 검사 | 불충족 → **default 경로 폴백** |
| 4 | **선행 보정 (병렬)** | **PickerY + PickerT(해당 피커)** ∥ **EjectPinZ→Avoid** | [병렬 명령] → [대기] WhenAll | ★ conti에서는 **PickerY가 X 이송 전에 미리 전진** (default는 X/T 후 Y 전진). 완료 후 EjectPinZ 위치 재확인 |
| 5 | Input Z축 안전 검사 | NeedleZ/EjectPinZ | 검사 | 스냅샷 확인만 |
| 6 | **가드 2**: 노드 생성 + 노드 조건 검사 | — | 계산/검사 | 불충족 → **default 경로 폴백** |
| 7 | Picker Vacuum ON | 피커 진공(IO) | [명령] 즉시 | Needle Vacuum은 아직 OFF (XY 게이트에서 OFF됨) |
| 8 | Input work area 점유 | — | 리소스 | `EnsurePickerWorkAreaReserved(Input)` |
| 9 | **PickerX 이송 시작** | PickerX | **[비동기 명령]** | conti 전용 속도/가감속. await 없이 태스크 보관 |
| 10 | **PickerZ PrePick 트리거 태스크 시작** | PickerZ(해당 피커) | [백그라운드 대기→명령+대기] | PickerX **실측 위치**가 트리거 도달까지 10ms 폴링 → 도달 시 PickerZ를 PrePick 위치로 하강 (**X 이송 중 Z 하강 오버랩**). X가 트리거 전 정지/알람이면 Fail |
| 11 | StageY/NeedleX 이송 게이트 | EjectPinZ, Needle 진공 | **[대기]** | EjectPinZ Avoid + Needle Vacuum OFF settle 확인. 실패 시 X/Z 태스크 완료를 기다린 뒤 중단 |
| 12 | **StageY·NeedleX 이송 시작** | StageY ∥ NeedleX | **[비동기 명령]** | 같은 conti 속도. 태스크 보관 |
| 13 | **4태스크 합류** | PickerX, StageY, NeedleX, PickerZ(PrePick) | **[대기]** Task.WhenAll | 하나라도 실패 시 Fail |
| 14 | Needle Vacuum ON | 니들 진공(IO) | [명령] 즉시 | Contact 직전 재-ON |
| 15 | **Contact (병렬)** | **PickerZ 저속 Contact 하강+settle** ∥ **EjectPinZ→픽업 준비 위치** | [병렬 명령+대기] WhenAll | PickerZ는 저속(설정 %) + Contact settle 지연 포함 |
| 16 | **최종 위치 확정** | PickerX→NeedleX→StageY→PickerZ 순 | **[대기]** | 모션돈/InPosition 순차 확인 후 3축 스냅샷 재검증 (`WaitContiSegmentedPickUpFinalPositionAsync`) |
| 17 | Contact 완료 플래그 set | — | — | `_pickerZContactedByContiPickUp = true` → VerifyPickTarget 스텝으로 |

### ② 검증 (`VerifyPickTarget` 스텝)

| # | 동작 | 축 | 종류 | 비고 |
|---|---|---|---|---|
| 18 | 자재/위치 스냅샷 검증 | StageY, PickerX, PickerY, PickerT, NeedleX | 검사 | 5축 위치 재확인 |
| 19 | Flow 사전확인 분기 | — | — | conti 플래그가 있으면 **VerifyPickerEmptyBeforePick(빈 피커 Flow 확인)을 생략**하고 바로 MovePickerZPick으로 (3469행 — Contact+Vacuum ON 상태라 사전 Flow 확인 불가) |

### ③ Z 픽업 마무리 (`MovePickerZPick` → `RunPickupZMotionAfterContiContactAsync`)

| # | 동작 | 축/IO | 종류 | 비고 |
|---|---|---|---|---|
| 20 | 시작 위치 검증 | PickerZ(Contact), NeedleZ(티칭), EjectPinZ(준비) | 검사 | 3축 스냅샷. 어긋나면 Fail |
| 21 | **SyncLift (병렬)** | **PickerZ ∥ EjectPinZ 동기 상승** (SyncLift 거리) | [병렬 명령+대기] | 완료 후 SyncLift settle 지연 [대기] |
| 22 | **Separate** | PickerZ 저속 Separate 거리 상승 | [명령+대기] | 저속(설정 %) |
| 23 | PickerZ Avoid 복귀 | PickerZ | [명령] → [대기] | 복귀 속도로 상승. **Stage-safe 거리 도달 시점**에 → |
| 24 | (23 도중) Needle Vacuum OFF + EjectPinZ→Avoid | 니들 진공(IO), EjectPinZ | [명령] | PickerZ가 안전 거리를 벗어난 뒤 실행 |
| 25 | PickSettle | — | [대기] 지연 | 설정 ms |
| 26 | 흡착 확인 | Flow 센서 | **[대기]** Flow ON 확인 | 실패 시 Fail. 성공 시 PickUp 검사 기록 |
| 27 | Z 안전 복귀 (병렬) | PickerZ→Avoid ∥ EjectPinZ→Avoid | [병렬 명령+대기] | NeedleZ는 티칭 위치 유지 |
| 28 | 자재 갱신 | — | — | `UpdateMaterialToPicker` → 다음 피커(4→1) |

## 표준(default) 경로와의 차이 요약

| 항목 | Default | ContiSegmentedPickUp |
|---|---|---|
| PickerY 전진 시점 | X/T·NeedleX/StageY 완료 **후** | X 이송 **전** 선행 (PickerT와 병렬) |
| 이송 방식 | [X+T] ∥ [NeedleX/StageY(순차 안전순서)] 2그룹 | **PickerX ∥ StageY ∥ NeedleX 3축 동시 비동기** |
| Z PrePick 시점 | 이송 완료 후 Z 루틴 안에서 | **X 이송 중** 트리거 위치 도달 시 하강 (오버랩) |
| Picker Vacuum ON | Z 루틴 내 Contact 직전 | **이송 시작 전** 미리 ON |
| Contact | Z 루틴(ⓒⓓ) | **이송 스텝 안에서 완료** |
| 빈 피커 Flow 사전확인 | 수행 | **생략** (Contact 완료 상태) |
| Z 루틴 시작점 | PrepareNeedlePinZ부터 전체 | **SyncLift부터** (Contact/EjectPinZ 준비 완료 전제) |
| 폴백 | — | 가드 1/2 불충족 시 default로 자동 폴백 |

## 참고 — 폴백(가드) 조건 위치
- 가드 1: `CanUseContiSegmentedPickUpFromCurrentPosition` (2025행) — 현재 축 위치 기준 진입 가능성
- 가드 2: `CanUseContiSegmentedPickUpNodesFromCurrentPosition` (2276행) — 생성된 노드 기준 검증
- 어느 쪽이든 거부되면 로그 남기고 `MovePickerXStageYPickerTByDefaultAsync`로 전환 (동작은 계속됨)
