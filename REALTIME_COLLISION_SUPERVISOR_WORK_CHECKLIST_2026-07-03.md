# RealtimeCollisionSupervisor 작업 체크리스트

작성일: 2026-07-03  
현재 단계: 1차 LogOnly 골격 + Front/Rear pair 설정 UI 보강

---

## 1차 목표

X가 붙어 있는데 양쪽 PickerY가 동시에 전진하는 사고 가능성을 제거하기 위한 실시간 감시 기반을 만든다.  
이번 단계에서는 실장비 안전을 위해 정지/알람 연결 없이 LogOnly로 상태와 설정 누락을 확인한다.

---

## 체크리스트

| 상태 | 항목 | 결과 |
|---|---|---|
| 완료 | 현재 인터락/SharedRailX/Form1 시작 구조 확인 | `MotionGuard`, `PickerZoneInterlockRules`, `SharedRailXMotionService`, `Form1.MotionMonitor` 확인 |
| 완료 | 작업 범위 1차 LogOnly로 고정 | 정지/알람 미연결, 로그 감시만 수행 |
| 완료 | `MotionSafetyState` 및 snapshot 타입 추가 | `CollisionSafetyTypes.cs` |
| 완료 | SharedRailX 방식 거리 계산기 추가 | `CollisionDistanceCalculator.cs` |
| 완료 | `RealtimeCollisionSupervisor` 골격 추가 | Start/Stop/CaptureState/LogOnly 감시 |
| 완료 | Front/Rear PickerY 상태 판정 추가 | Avoid/InputAvoid/OutputAvoid/0 근처 기준 |
| 완료 | FrontPickerX <-> RearPickerX pair 설정 누락 로그 추가 | HomeClearance/sign 미등록 시 10초 throttle 로그 |
| 완료 | Form1 시작/종료 연결 | `CollisionSupervisor.Start(...)`, `Dispose()` |
| 완료 | 프로젝트 파일 compile include 추가 | `QMC.CDT-320.csproj` |
| 완료 | 별도 OutDir 빌드 검증 | `_codex_verify/build_realtime_supervisor_logonly/` 빌드 성공 |
| 완료 | SHARED RAIL X에서 Front/Rear pair 추가 버튼 보강 | `Add Front/Rear Pair`, 미설정 row 경고색 표시 |
| 완료 | Front/Rear pair 저장 검증 추가 | Home Gap > 0, A/B Sign != 0 아니면 저장 차단 |
| 완료 | 미완성 pair 런타임 적용 방지 | `ToConfig`에서 clearance rule 없는 pair 제외 |
| 완료 | 감시자 SharedRailX 설정 재읽기 추가 | `shared_rail_x.json` 변경 시 1초 주기로 reload |
| 완료 | 별도 OutDir 빌드 검증 | `_codex_verify/build_realtime_supervisor_pair_ui/` 빌드 성공 |

---

## 이번 단계에서 의도적으로 제외

- 실시간 위험 감지 시 축 정지 연결
- Critical Alarm 발생
- MotionGuard Predictive Gate 연결
- `FrontPickerX <-> RearPickerX` 실제 HomeClearance/sign 자동 추정
- 검사존 lock
- Z Down 중 XYT CorrectionWindow
- 기존 zone 예외 정리

---

## 다음 체크리스트

1. 실행 로그에서 `실시간 충돌 감시 시작` 확인.
2. 실행 로그에서 `FrontPickerX<->RearPickerX 거리 pair가 등록되지 않았습니다` 확인.
3. 실제 장비 기준 `FrontPickerX <-> RearPickerX` HomeClearance/sign 확정.
4. SHARED RAIL X에서 `Add Front/Rear Pair`로 row를 추가하고 확정값 저장.
5. 실행 로그에서 actual clearance 값이 기구 거리와 맞는지 검증.
6. actual clearance 값 검증 후 MotionGuard 사전 차단 연결.
7. 시뮬 검증 후 실시간 정지/알람 연결.
