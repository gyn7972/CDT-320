# REALTIME_COLLISION_SUPERVISOR_DESIGN.md

CDT-320 듀얼 픽커 충돌 방지를 위한 **상시 실시간 충돌 감시자(RealtimeCollisionSupervisor)** 설계 문서.
작업 전 [AGENTS.md](AGENTS.md) 규칙을 따른다. 이 문서는 구현 전 합의용 초안이며, 승인 후 단계별로 구현한다.

---

## 0. 문제 정의 (현재 사고 원인)

공용 X 레일 위에서 4개 캐리지가 겹치는 스트로크를 가진다: `InputCameraX(=InputVisionX)`, `FrontPickerX`, `RearPickerX`, `OutputCameraX(=OutputVisionX)`. 두 픽커는 각각 `PickerY`(전진=공정 방향)를 가진다. 서로 움직이며 물리적으로 간섭한다.

현재 인터락의 한계:

1. **실시간 X 거리 감시(`SharedRailXAutoMoveGuard`)가 per-move + X 레일 전용.** 20ms 루프로 실제 엔코더 거리를 보고 너무 가까우면 전 축 Stop + 알람(-11)까지 이미 구현돼 있으나, **공용 X 그룹 이동이 명령된 동안에만** 살아 있고(`movementSeen` 후 정지하면 종료), X 레일 쌍만 본다.
2. **Y축 마주보기 인터락(`PickerZoneInterlockRules.CanMovePickerYByFacingYInterlock`)은 Y 이동 시작 순간만 검사**(기본 150mm). Y가 이미 전진해 있는 상태에서 상대 X가 접근하는 실시간 상황을 감시하지 않는다.
3. **존(WorkArea) 점유 기반 판정이 이동 시작 시점 평가**라 상태 재구성/타이밍에 취약("존이다 뭐다 하면서 문제").
4. **오토 정상 예외**(픽업→바텀→사이드→플레이스까지 Y 전진 유지, Z 하강 중 정렬오프셋 XYT 미세보정)를 **수동 인터락이 위반으로 오판** → 예외를 하나씩 뚫으며 규칙이 복잡해지고 타이밍 꼬임.
5. **정지 후 재개**: 레디에서 현재 다이 상태로 복귀 시작 시 상태 재구성이 어긋나며 사고. (신규 시작은 문제 없음.)

핵심 요구: **명령 시점이 아니라 실시간으로**, **오토/수동 무관**, **상호 거리 기반**으로 인터락이 걸려야 하며, 오토 정상 예외와 재개를 구멍 없이 처리해야 한다.

---

## 1. 요구사항 정리 (사용자 확인 사항 반영)

### 1.1 X축
- 4개 캐리지 상호 거리를 실시간 측정. **현재 거리 + 타겟 거리** 모두로 판정.
- 접근 방향으로 필요 clearance 미만이면 차단(이동 전) / 하드정지(이동 중).

### 1.2 Y축
- **전진 허용 판정**: 픽커 Y 전진은 두 픽커의 X 엔코더를 실시간으로 보고, 마주보기 안전거리 밖이거나 상대가 후퇴(Avoid)일 때만 허용.
- **Y 전진 상태에서 X 이동**: 한쪽/양쪽 Y가 전진해 있을 때 X 이동이 마주보기 충돌을 유발하면 차단/정지.
- **X가 붙어 있을 때**: 두 픽커 X가 마주보기 거리 안이면 **한쪽은 무조건 Y 안전(후퇴) 위치**여야 한다. 양쪽 동시 전진 금지, 전진 명령 금지.

### 1.3 상시/실시간
- 오토든 수동이든 **항상 켜진 감시 루프**로 실제 위치를 감지. 상호 이동 순간이 아니라 상시.
- 실제 충돌 임박(실시간 가드) 감지 시 **전 축 하드정지 + 알람**(사용자 결정).

### 1.4 오토 정상 예외 (구멍 없이 허용해야 함)
오토 1사이클의 정상 궤적:
1. `PickUp`: Y 전진 → Die 픽업. 이후 Die 보유 → **Z는 안전(상승) 위치**.
2. Y 전진 유지 → Bottom으로 이동 → Z 하강 → Bottom 검사.
3. **Z 하강 상태에서 X 피치 이동**하며 Bottom → Side까지 비전검사.
4. Side 끝 → **Z 상승**, Y 전진 유지.
5. `Place`까지 이동 → Die place → 전부 끝나면 **Y 안전 위치 복귀**.
- **공정 중 정렬오프셋 보정**: Z가 내려온 상태에서도 X/Y/T가 그 위치에서 **일정 범위 내** 미세 이동 가능해야 한다(정렬 오프셋 반영). 범위 밖은 차단.

### 1.5 검사존 배타 (병렬 + 상호 대기)
- **인풋**: 오토 시 카메라가 들어와 Die 검사 중이면 픽커가 픽업존에 진입 금지.
- **아웃풋**: 플레이스 종료 후 다이 검사 중이면 픽커가 진입 금지.
- 병렬 동작하되 상대가 점유 중이면 **대기**했다가 상호 안전 확인 후 진입.

### 1.6 재개
- 레디에서 정지 후 재개(현재 다이 상태부터 시작)해도 **신규 시작과 동일하게** 안전해야 한다.

---

## 2. 아키텍처 개요

흩어진 규칙(존/마주보기/per-move 가드)을 **단일 권위**로 통합한다.

```
                 ┌───────────────────────────────────────────┐
   모든 이동 요청  │        RealtimeCollisionSupervisor         │
 (오토 시퀀스,     │  (단일 권위, 상시 서비스, 파라미터 기반)     │
  수동 조그,       │                                           │
  Home 등)        │  ┌─────────────┐   ┌───────────────────┐  │
      │           │  │ 예측 게이트   │   │ 반응 가드(상시)     │  │
      └──── CanMove(axis,target,ctx) ──▶│  10ms 루프         │  │
                  │  │ allow/wait/  │   │ 실제 엔코더 감시    │  │
                  │  │ block        │   │ 위반→전축 하드정지  │  │
                  │  └─────────────┘   └───────────────────┘  │
                  │        ▲                    │              │
                  │        │  MotionSafetyState │              │
                  │  ┌─────┴────────────────────┴───────────┐  │
                  │  │ 상태·예외 모델 (side별 phase/Y/Z/락) │  │
                  │  └──────────────────────────────────────┘  │
                  └───────────────────────────────────────────┘
```

두 책임:

### 2.1 예측 게이트 (이동 전) — `CanMove(axis, target, ctx)`
- 모든 이동이 통과하는 단일 진입점. X 상호거리+타겟, Y전진 vs 상대 X 마주보기, Z하강 중 XYT 보정한계, 검사존 락을 **한 번에** 판정.
- 결과 3종:
  - **Allow**: 이동 허용.
  - **Wait(reason)**: 협조적 대기(상대 busy, 존 락, 게이트 미충족). **알람/정지 없음**. 오토는 대기 루프, 수동은 거부 메시지.
  - **Block(reason)**: 기하적으로 불가한 이동(수동 오조작 등). 이동 미발행 + 경고.

### 2.2 반응 가드 (상시 연속) — 10ms 루프
- 명령 추적 없이 **실제 엔코더만으로** 임박/실제 위반 감지:
  - X 쌍 거리 < 필요 clearance 이고 접근 중.
  - 두 픽커 동시 Y 전진 + X 마주보기 거리 안.
  - 잠긴 검사존을 픽커가 실제 침범.
- 감지 시 **전 축 하드정지(StopAllMotion) + 알람 + 리셋 요구**. 오토/수동 동일. (사용자 결정)
- 기존 `SharedRailXAutoMoveGuard`의 clearance 계산·정지 로직을 재사용/흡수하되 **상시화**하고 Y·존까지 확장.

### 2.3 왜 단일 권위인가
- 지금은 오토 시퀀스의 존 점유 + `PickerZoneInterlockRules`(수동 규칙)가 **서로 다른 판정**을 해서 오토 예외를 뚫을 때마다 충돌. supervisor가 **하나의 상태 모델**로 오토·수동 모두 판정하면 이중 규칙 충돌이 사라진다.

---

## 3. 상태·예외 모델 (`MotionSafetyState`)

supervisor가 상시 보유하는 스냅샷. side(Front/Rear)별:

| 필드 | 의미 |
|---|---|
| `Phase` | Idle / PickUp / BottomInspect / SideInspect / Place / Retracting |
| `YState` | Retracted(안전) / Forward(전진) / Moving |
| `ZState` | Up(안전) / Down / Moving |
| `Carrying` | Die 보유 여부 |
| `ForwardYPermitted` | 이 side가 지금 Y 전진을 유지해도 되는 phase인가 |
| `CorrectionWindow` | Z하강 중 허용되는 XYT 미세보정 중심±한계 |

공용 X 스냅샷: 4개 캐리지 `ActualPosition`, 각 픽커 `PickerX/Y/Z`.

### 3.1 오토 전진‑Y 라이프사이클을 상태로 표현
`ForwardYPermitted = Phase ∈ {PickUp, BottomInspect, SideInspect, Place}` 이고 `Carrying` 이면 전진 유지 정상. 이 값은 시퀀스가 phase 전환 시 supervisor에 통지(§5). → 오토가 Y를 전진 유지해도 supervisor는 "정상"으로 인식하고, 오직 **상대 X 마주보기**만 감시한다.

### 3.2 Z하강 중 XYT 보정 예외
`ZState==Down` 이면 기본은 XYT 이동 차단이나, `CorrectionWindow`(파라미터: 예 ±2mm) 안이면 Allow. 창 밖이면 Block. → 정렬오프셋 보정 허용, 대이동 방지. (현재 `AutoProcessCorrection` 예외를 이 창으로 흡수.)

### 3.3 재개 = 신규 시작
Cycle 시작/재개 시 `ReconcileSafeState()`:
1. 실제 엔코더/자재상태를 읽어 각 side의 `Phase/YState/ZState/Carrying` 재구성.
2. "Die 보유 + Y 전진 + Z 상승" 같은 정상 상태를 인식(위반 아님).
3. 두 픽커가 동시에 마주보기 위험 구성이면, 재개 전에 한쪽을 안전화하는 **복귀 시퀀스**를 supervisor가 요구.
- 재개 경로와 신규 경로가 **같은 상태 모델**을 통과 → 재개 특유의 구멍 제거.

---

## 4. 규칙 정의 (판정 표)

파라미터는 §6. 아래 "차단"=이동 전 거부, "정지"=실시간 하드정지, "대기"=협조 대기.

### 4.1 X 규칙
| 조건 | 결과 |
|---|---|
| 쌍 현재거리 < 필요 clearance 이고 타겟이 더 가까워짐 | 차단 |
| 타겟거리 < 필요 clearance | 차단 |
| 현재 unsafe이나 타겟이 멀어지고 실시간 개선 중 | 허용 |
| (실시간) 이동 중 실제거리 ≤ 필요 & 개선 안 됨 | **전축 하드정지+알람** |

### 4.2 Y 규칙
| 조건 | 결과 |
|---|---|
| Y 전진 목표 + 상대 Y 후퇴(Retracted) | 허용 |
| Y 전진 목표 + 상대와 다른 존(CanShareForwardY) | 허용 |
| Y 전진 목표 + 두 픽커 X 마주보기 거리 안 + 상대도 Y 전진/전진예정 | 차단 |
| (X 붙음) 마주보기 거리 안인데 양쪽 다 Y 비후퇴 | **전축 하드정지+알람** |
| (실시간) 한쪽 Y 전진 유지 중 상대 X가 마주보기 거리로 접근 | **전축 하드정지+알람** |

### 4.3 Z / XYT 보정 규칙
| 조건 | 결과 |
|---|---|
| ZState==Down, XYT 목표가 CorrectionWindow 안 | 허용 |
| ZState==Down, 목표가 창 밖 | 차단 |
| Carrying인데 ZState==Down 요청(비검사 phase) | 차단 |

### 4.4 검사존 락 규칙
| 조건 | 결과 |
|---|---|
| 인풋 검사 락 보유 중 + 픽커가 픽업존 진입 목표 | 대기 |
| 아웃풋 검사 락 보유 중 + 픽커가 플레이스존 진입 목표 | 대기 |
| 락 해제됨 | 허용 |
| (실시간) 락 보유 중 픽커가 실제로 존 침범 | **전축 하드정지+알람** |

---

## 5. 통합 지점 (funnel)

1. **이동 발행 단일화**: 픽커/공용축 이동은 모두 `supervisor.CanMove(...)`를 통과. 진입점 후보:
   - `BaseAxis.Move*` / 기존 `MotionGuard` / `MotionInterlock.VerifyMove` 경로에 supervisor 훅.
   - `SharedRailXMotionService`의 검증을 supervisor로 위임/통합.
   - 조그(`SharedRailXMotionRuntime`, JogAxisMoveControl)도 동일 경로.
2. **phase 통지**: 픽커 시퀀스(PickUp/Bottom/Side/Place)가 phase 전환 시 `supervisor.SetPhase(side, phase, carrying)` 호출 → §3.1 상태 갱신. 기존 PickerPhase lease와 병행/대체 검토.
3. **검사존 락**: `InputCameraMarkInspection`/아웃풋 place검사 스텝이 `supervisor.AcquireInspectZoneLock(input/output)` / `Release`. 오토·수동 공통.
4. **상시 구동**: `Form1_Load`에서 MotionMonitorService와 함께 `supervisor.Start()`(오토/수동 무관). Cycle 시작·재개 시 `ReconcileSafeState()`.

기존 규칙(`PickerZoneInterlockRules` 등)은 supervisor 판정에 **위임**하도록 단계적으로 정리(중복 제거)하되, 초기에는 supervisor를 상위 권위로 얹고 기존은 보조로 남겨 회귀 위험을 낮춘다.

---

## 6. 파라미터 (레시피/설정 등록)

기존 `SharedRailXConfig`(쌍별 HomeClearance, toward-sign, SafetyDistance)를 확장/재사용.

| 파라미터 | 설명 | 비고 |
|---|---|---|
| 쌍별 `HomeClearance`, `TowardSign`, `SafetyDistance` | X 쌍 거리 계산·필요거리 | 기존 SharedRailXConfig |
| `FacingClearanceX` | 두 픽커 Y 동시 전진 금지 마주보기 X 거리 | 기본 150mm |
| `PickerYForwardThreshold` | Y 전진(비안전)으로 간주하는 경계 | side별 |
| `PickerYSafePosition` | Y 안전(후퇴) 위치 | side별 |
| `ZDownThreshold` | Z 하강으로 간주하는 경계 | side별 |
| `CorrectionWindowXYT` | Z하강 중 허용 XYT 미세보정 ±한계 | 예 X/Y/T 각각 |
| `InputPickZoneRange`, `OutputPlaceZoneRange` | 검사존 배타 X 범위 | 락과 연동 |
| `SupervisorPeriodMs` | 상시 루프 주기 | 기본 10ms |

전부 설정 파일(JSON pretty UTF-8, `JsonPrettySerializer`) + UI 등록.

---

## 7. 단계별 구현 계획 (승인 후)

각 단계 후 빌드(`MSBuild`) + `perl tools/verify_all.pl` 검증. AGENTS.md 예외/로그/알람·명명·Designer 규칙 준수. 실장비 충돌 방지 최우선, gate/interlock 우회 금지, 별도 OutDir 빌드, 신규 로그·알람 한글.

1. **골격**: `RealtimeCollisionSupervisor` 서비스 + `MotionSafetyState` + 파라미터 모델. 상시 루프(감시만, 정지 미연결)로 로그만 출력해 관찰.
2. **X 통합**: 기존 SharedRailX clearance 계산을 supervisor로 흡수, 예측 게이트 + 상시 반응 가드(전축 하드정지) 연결.
3. **Y 통합**: 마주보기 규칙을 예측+실시간 양쪽으로. `PickerZoneInterlockRules`의 Y 판정 위임.
4. **Z/XYT 보정 창** + **검사존 락**(인풋/아웃풋) 연동.
5. **phase 통지 + ReconcileSafeState**: 시퀀스 훅, 재개 안전.
6. **기존 규칙 정리**: 중복 존/마주보기 규칙을 supervisor 위임으로 단순화.
7. **회귀·시나리오 검증**: 신규 시작 / 정지→재개 / 수동 조그 / 인풋·아웃풋 병렬 검사 대기 / Z하강 보정 창 / 강제 충돌 유도(시뮬) 시 하드정지.

---

## 8. 미결/확인 필요

- 마주보기 판정에 사용할 위치는 **엔코더 실제값**(현재 `ActualPosition`) 기준으로 확정. 지령값 병용 여부.
- `CorrectionWindow` 실제 허용 한계값(정렬오프셋 최대치 기준).
- 하드정지 후 복구 절차(리셋 → 안전화 복귀 시퀀스) UX.
- supervisor와 기존 `AlarmResponseService`/`MotionInterlock` 레지스트리의 책임 경계 최종 정리.
