# 체크리스트 — 2. 2축 팔로잉 모션 (axis-follow-move.md)

작성일: 2026-07-19 / 확인일: 2026-07-19 (1회차 통과)

## 구현 항목
- [x] C1. `AjinAxis.TryOverrideVelocity(velocity, acceleration, deceleration)` 신규 — 정지 시 `-4` / 시뮬 `base.OverrideVelocity` / 실장비 AjinSystem·알람·서보 검사 후 `lock(_sync)` `AXM.ModifyVelocity(보드단위)` / 실패 `FailMotion` / 성공 `CurrentVelocity` 갱신(base.OverrideVelocity) / 0 이하 인자 Config+`MotionSpeedScale` 대체
- [x] C2. `AjinAxis.FollowMoveAsync` 시그니처 사양 일치 (13개 인자 + `ct` 기본값)
- [x] C3. 상수 — `MinimumFollowSafetyGap=40.0` / `FollowMoveTimeoutMs=5000` 고정 / `FollowMovePollIntervalMs=10`
- [x] C4. 초기 검증 — null→-1 / direction≠±1→-1 / 서보OFF·알람→-2(`FailAxisNotReady`) / 반대방향→-1 / 이미 도달→0 (하네스 5케이스 PASS)
- [x] C5. 팔로잉 프로파일 — 성분별 `Min(선행, 후행)`, 0 이하 성분은 해당 축 Config 기본값 대체
- [x] C6. 간격/여유/중간명령/Min·Max 클램프/이동 후 간격 재검증(epsilon 포함) + 명령이 진행 방향인지 가드
- [x] C7. 명령 방법 — 정지: `MoveAbsoluteAsync` 백그라운드 보관(최종 구간이면 자기 속도로 시작) / 이동 중: `TryOverridePosition`(-4 무시, -11 정지 후 반환, 기타 에러 정지 후 반환) / 직전 명령과 공차 이내면 생략
- [x] C8. 최종 구간 — 명령==목표 루프에서 `TryOverrideVelocity(자기 프로파일)` → `WaitMoveDoneInPositionAsync(남은 타임아웃)` → drain(observe) → 0
- [x] C9. 이상 처리 — 타임아웃: Stop→drain(2s)→`RecordMotionFailure`→-21 / 취소: Stop+drain 후 OCE 전파 / 후행 알람 `(int)AlarmCode`·선행 알람 -22 / 백그라운드 Task 실패 코드 회수 / unobserved 방지(`ObserveFollowMoveResult`, ContinueWith observe)
- [x] C10. 선행축 절대 불가침 — 읽기 전용 (`ActualPosition`/`IsAlarm`/`Name`/`Config` 조회만, 명령 호출 0건. 하네스에서 CommandPosition 불변 확인)
- [x] C11. 로깅 — Start(인자 요약)/최초 명령 Ok/최종 구간 Ok/정상 완료 Ok/타임아웃·알람·인터락·백그라운드 실패 Failed, 한국어 `Log.Write("Motion","SYSTEM","AX-FOLLOW-MOVE",...)`
- [x] C12. 다른 파일 무수정 — 변경 파일 `AjinAxis.cs` 1개

## 검증
- [x] C13. 솔루션 빌드 성공 — 신규 경고 0건 (최초 CS4014 1건 발생 → observe 변수 할당으로 해소 후 재빌드 확인. 잔여 경고는 기존 CS0162 4건)
- [x] C14. 예시 A — `-21` 반환(5022ms), 후행 500.000 미이동·정지 상태 ✅
- [x] C15. 예시 B(-방향) minGap=100.001 / 예시 C(+방향) minGap=200.000 — 전 폴링 간격 ≥ safetyGap(50) 유지, 최종 목표(150/800) 도달 `0` 반환 ✅
- [x] C16. 선행축 CommandPosition 불변 — A: 600 유지 / B: 외부 명령 100 유지 / C: 외부 명령 1000 유지 ✅
- [x] C17. 초기 검증 케이스 — null→-1, direction 0→-1, 반대방향→-1, 이미 도달→0, 서보OFF→-2 전부 지정 코드 ✅
- [x] C18. `safetyGap=30` → 40 클램프 — 선행 정지(600, homeGap=0) 시 후행이 정확히 560.000(=선행-40)에서 정지, gap=40.000 ✅ (30이었다면 570까지 진행했을 것)

## 결과: 전 항목 통과 (하네스 23개 검증 ALL PASS, 재시도 불필요)
