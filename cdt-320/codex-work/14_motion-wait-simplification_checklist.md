# 체크리스트 — 모션 대기 단순화 (motion-wait-simplification.md)

작성일: 2026-07-23  /  브랜치: feature/motion-wait-simplification

## 계획 요약 (전수 조사 반영)
- 참조 규모: 56개 파일. AXM.GetInMotion 래퍼 **기존 존재**(AjinE\AXM.cs:1908,
  `GetInMotion(int axis, ref bool value)` → AxmStatusReadInMotion) — 신규 추가 불필요(R1 보고).
- 치환 기반 헬퍼(신설, BaseAxis):
  ① `CanSkipMoveToTarget(target, tol)` — R2 스킵 공식 단일화
  ② `WaitMoveCompleteAsync(target, timeoutMs, ct)` → int — [B] 비동기 합류용 단일 대기
    (UpdateStatus 폴링 10ms + 정지/알람/타임아웃 + Command↔Target 톨러런스 확인, 시뮬 공용)
  ③ `IsAtTargetPosition(target, tol)` — [D] 스냅샷 판정 대체(FollowMove 루프 등 필요 지점만)
- 유닛 Wait 래퍼(WaitXxxMoveDoneInPosition 계열 ~15종)는 시그니처를 Task<int>(0=성공)로
  전환하고 내부는 축.WaitMoveCompleteAsync 위임 — [B] 합류 호출부는 int 검사로 축약.
- [A] 직후 재대기는 삭제(Move 리턴 0=완료). [C] 외부 스킵 체크는 IsAtTargetPosition 치환.
- [E] FormatResult/ResolveAlarmCode → `축.LastMotionFailureMessage` + 단일 코드(prefix+"-MOVE").
- 죽은 파일: WaferStageUnit.cs/BinStageUnit.cs는 csproj 미포함(컴파일 제외) — 수정 불필요,
  보고만. UnitDefined<TAxis>는 컴파일되나 파생 0건 — 래퍼 int 전환만.
- 현장 커밋 292c8ab4의 AXM.MovePosition 1초 재시도 루프는 **프롬프트 지시로 제거**(1회 호출).

## 구현
- [x] C1. R1: AjinAxis.MoveAbsoluteAsync 재작성 — MovePosition 1회, WaitUntilMoveDone 내부를
  GetInMotion 10ms 폴링으로 교체(정지시리얼 -4/알람/60s 타임아웃, 시작 유예 detectedMotion+20폴
  관례 유지), 리턴 전 Command↔Target 톨러런스 확인만(INP/Actual/settle 제거)
- [x] C2. R2: BaseAxis.CanSkipMoveToTarget 신설 + BaseAxis 448/516, AjinAxis 915/1064
  스킵 지점 교체(상태 합성 유지)
- [x] C3. BaseAxis.WaitMoveCompleteAsync/IsAtTargetPosition 신설 (시뮬·실장비 공용,
  R5: 시뮬 사다리꼴 엔진 무수정 — UpdateStatus가 프로파일 전진)
- [x] C4. AjinAxis 내부 의존 정리 — FollowMoveAsync(IsAtTargetPosition,
  WaitMoveCompleteAsync+int), MoveJogStepAsync 재대기 삭제, BaseAxis.MoveJogStepAsync 재대기 삭제
- [x] C5. AxisMoveWaiter.cs 삭제 + QMC.Common.csproj 제거 → 빌드 에러 배치 수정 완료:
  유닛 래퍼 int 전환(Picker F/R, InputStage, OutputStage, Vision, Feeder×2, Cassette×2,
  UnitDefined, MachineController), 시퀀스 베이스 정리(PickerSequenceBase, 4개 베이스 쌍,
  SequenceAwaiter.AwaitAxisWaitAsync 삭제), 호출부 [A] 삭제/[B] int/[C] IsAtTargetPosition
  48건 스크립트 치환/[E] waitCode+LastMotionFailureMessage, Ui(MotionTestDialog, MapTransfer,
  RecipePage×4, InputCassettePage) — **최종 빌드 에러 0**
- [x] C6. R4: 이동 후 스냅샷 재검증 제거 — Verify 하이브리드(PickerF/R
  VerifyPickerAxisAfterCompletedMoveAsync/VerifyPickerAxisMoveDoneInPosition) 삭제,
  PickerPlaceSequence 스테이지 최종 스냅샷 제거. 유지 목록은 레포트 참조
  (캘리브레이션 최종 실측 게이트, MachineReady 최종 IsAxisInPosition, Avoid 도착 안전 게이트,
  VerifyPickTarget, SyncLift 사전조건)
- [x] C7. 특기 로직 보존: OutputFeeder 오버로드(-9 반환 + BF-Y-OVERLOAD 알람),
  OutputStage 축 미존재 가짜 성공(0 반환 유지), Cassette ScanSettleTimeMs(물리 대기로만 보존),
  InputCassette 매핑 스캔 폴링 감시 구조 무수정
- [x] C8. 인코딩 BOM+CRLF 유지(치환 스크립트 UTF8-BOM 저장), csproj 정리

## 검증
- [x] V1. 빌드 통과(에러 0) + AxisMoveWaiter 참조 0건
  (파일 삭제 + csproj 항목 0 + 컴파일 대상 코드 참조 0 — 남은 grep 히트는 주석과
  csproj 제외 죽은 파일 WaferStageUnit.cs/BinStageUnit.cs 뿐)
- [x] V2. 시뮬 하네스 18/18 PASS — 단일 이동(리턴0+Command≈Target+프로파일 시간 603ms/예상 600ms),
  재호출 즉시 0(스킵, 0ms), 비동기 합류(Task 보관→WaitMoveCompleteAsync→0),
  이동 중 알람 → 알람코드 반환+합류 대기 실패코드, IsAtTargetPosition 판정.
  ※ 시뮬 BaseAxis 경로의 Stop()은 기존과 동일하게 0 리턴(Actual 중단 위치) —
    -4 관례는 실장비 AjinAxis 정지시리얼 경로에만 존재(기존 동작 유지, 레포트 명시)
- [x] V3. 픽업·플레이스 대표 패턴 회귀(축·대기 계층) — FastConti 명령전용 발행+합류(T8),
  XYZ 그룹 병렬 이동 전축 완료(T9) PASS. 상위 시퀀스 스텝 전이 로직은 이번 작업에서 무수정.
  GUI 풀사이클 회귀는 장비 HMI 실행이 필요해 미실시(레포트 명시)
- [x] V4. 제거/유지 판단 목록 — 레포트에 정리
- [x] V5. 레포트 — GetInMotion 래퍼 명세, 동작 변경 승인 사항, 알람 코드 체계 변화 명시
