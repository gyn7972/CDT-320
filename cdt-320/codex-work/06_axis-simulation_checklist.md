# 체크리스트 — 5번째 프롬프트: Axis 시뮬레이션 강화 (axis-simulation-enhancement.md)

작성일: 2026-07-19 / 확인일: 2026-07-19 (1회차 통과 — **코드 수정 0건**)

## 사전 갭 분석 결과 (구현 전 코드 확인)
프롬프트가 지적한 3개 문제점(초기속도 0 가정 / TryOverridePosition velocity 무시 / 잔류 오버라이드)은
현재 master의 `BaseAxis.cs`에서 **이미 해소된 상태**로 확인됨 — 프롬프트가 이전 코드 스냅샷 기준으로 작성된 것으로 판단:
- `_simMotionInitialVelocity` + `BuildSimulationMotionSegments(initialVelocity)` — v₀ 지원 (역사다리꼴 v₀>v 분기 791행, 사다리꼴 817행, 삼각형 831행)
- `ResetSimulationMotionReference(initialSignedVelocity)` (723행) — 방향 반전/제동거리 초과 시 `_simMotionContinuationPending` → `ContinueSimulationMotionAfterDeceleration()` (1355행) 연속 프로파일
- `SimulateMotion()` (1168행) — 오버라이드 소비 시 `ResolveSimulationSignedVelocity()`로 현재 속도 기반 재구성
- `AjinAxis.TryOverridePosition` 시뮬 분기 (138행) — `base.OverridePosition` + `velocity>0`이면 `base.OverrideVelocity` (CurrentVelocity 직접 대입 없음)
- `ClearSimulationOverrides()` 호출처 — ConfigureSimulationMotionProfile(660)·Stop(268)·SetPosition(307)·RestoreRuntimeState(341)·CompleteSimulationMove(1381)·TriggerSoftLimitAlarm(1406)·StopJog(→Stop 경유)

## 요구사항 확인
- [x] R1. v₀ 지원 프로파일 3형태 + v₀=0 기존 동일 — 코드 확인 + V2 검증
- [x] R2. PositionOverride 현재 위치·속도 재구성 + 방향 반전 감속→재구성 + CompleteSimulationMove 1회 — V3/V4 검증
- [x] R3. VelocityOverride 상향/하향 + AjinAxis 시뮬 분기 위치+속도 동시 반영 — V5/V6 검증
- [x] R4. 잔류 오버라이드 초기화 전 지점 — 코드 확인 + V7 검증
- [x] R5. 제약 준수 — 이번 작업 코드 수정 0건이므로 public API/Jog/실장비 경로 자동 충족

## 검증 (시뮬 하네스 24개 검증 ALL PASS)
- [x] V1. 솔루션 빌드 성공 (기존 최종 빌드 그대로 — 수정 없음)
- [x] V2. v₀=0 신규 이동 — 100.0000 도달, 2233ms(이론 2.2s), MoveCompleted 정확히 1회 ✅
- [x] V3. OverridePosition(100→200 연장) — 속도 유지(minVel=50.000, 재가속 없음), 200.0000 도달, MoveCompleted 1회 ✅
- [x] V4. OverridePosition(+100→−50 반전) — 감속→반전→−50.0000 도달, 알람 없음, MoveCompleted 1회 ✅
- [x] V5. OverrideVelocity 상향 50→100(max=100.000)/하향 100→20(cruise=20.000) 후 목표 도달 ✅
- [x] V6. TryOverridePosition(시뮬, target=400·vel=90) — 위치 400.0000·속도 max=90.000·CommandPosition=400 모두 반영 ✅
- [x] V7. 정지 상태 OverrideVelocity(999) 후 새 이동 — max=50.000 (잔류값 미적용) ✅

## 결과: 전 항목 통과 — 기존 구현이 사양 충족, 신규 코드 변경 없음 (재시도 불필요)
