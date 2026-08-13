# CDT-320 구현 체크리스트 — 피커 선입선출(FIFO) 재시작 드레인 타이브레이크

- 작성일: 2026-08-13
- 선행 문서: [04_design.md](04_design.md)
- 수정지시 프롬프트: `..\..\피커선입선출_재시작드레인_FIFO_수정지시_프롬프트_2026-08-13.md`
  (프롬프트 §7과 동일 내용 — 함께 갱신할 것)
- 키 순서(팀장님 확정): **1차 InputSequenceNo(같은 웨이퍼일 때) → 2차 PickedAt → 폴백 Front**

## 1. 코드 편집 — PickerFirstForwardSequencer.cs
- [ ] `PickerResumeDrainFifoKey` 구조체 신설 (HasSequenceNo/SequenceNo/WaferKey/HasPickedAt/PickedAt)
- [ ] side별 FIFO 키 static 저장소 추가
- [ ] `BeginRun()` — 키 저장소 초기화 추가
- [ ] `ConfigureResumeDrain` — 시그니처 확장 + 키 저장 + 진입 시 초기화
- [ ] `IsResumeDrainFifoWinnerNoLock` 비교 함수 신설 (1차 순번→2차 시각→Front 폴백)
- [ ] `IsHighestResumeDrainPriorityNoLock` 동률 분기(:386~389) → 비교 함수 호출로 교체
- [ ] `IsHigherResumeDrainPriorityNoLock` 동률 분기(:429~433) → **동일** 비교 함수로 교체
- [ ] `DescribeResumeDrainSideNoLock` — 키 요약 추가

## 2. 코드 편집 — AutoSequenceCoordinator.cs
- [ ] `ResolveRestartPickerDrain` — IsInputTarget 다이만 집계해 FIFO 키 산출(out 추가)
      - InputSequenceNo > 0 중 최솟값 + 대표 다이 waferKey(InputWaferInstanceId, 비면 WaferID_Input)
      - PickedAt 유효 기준: `> new DateTime(1900,1,1,23,59,59)` (Compactor 관례)
      - `GetPickerDieSortTime`(UpdatedAt 혼입) 사용 금지 — `die.PickedAt` 원본만
- [ ] `ConfigureRestartPickerDrain` — 확장 시그니처로 호출 갱신
- [ ] 키 산출 로그: side별 minSeq/waferKey/earliestPickedAt(HH:mm:ss.fff)/targetDieCount,
      키 없으면 fifoKey=none + 사유
- [ ] 판정 결과 로그: `drainOrder=..., tiebreak=SequenceNo|PickedAt|FrontFallback` + 비교 수치

## 3. 무변경 확인
- [ ] rank 산정(RankBottomSide/RankPickUp) 무변경
- [ ] `IsHighestPriorityNoLock`(첫 전진 게이트) 무변경
- [ ] `TryYieldExpectedSideToPriority` / Rear 양보 로직 무변경
- [ ] `MaterialStateService` 무변경 (읽기 전용)
- [ ] `PickerProcessSequence` 무변경
- [ ] 신규 모션 코드 없음 (MotionSpeedScale 위반 없음)

## 4. 정적/빌드 확인
- [ ] `/p:OutDir=<임시경로>` 빌드만 사용 (실장비 폴더 직결 방지), 경고 0건
- [ ] `ConfigureResumeDrain` 호출부 1곳(AutoSequenceCoordinator) 전체 검색 확인
- [ ] 동률 분기 2곳이 동일 비교 함수 사용 확인

## 5. 시뮬레이션 검증 (설계 §2-5 워크스루 표 대조)
- [ ] 시나리오 1: 같은 웨이퍼, Rear 빠른 순번 → **Rear 먼저** + `tiebreak=SequenceNo` 로그
- [ ] 시나리오 2: 같은 웨이퍼, Front 빠른 순번 → Front 먼저
- [ ] 시나리오 3: 한쪽만 보유 → rank 차이로 기존 동작, FIFO 미개입
- [ ] 시나리오 4: 서로 다른 웨이퍼 → `tiebreak=PickedAt`, 먼저 픽업 쪽 먼저
- [ ] 시나리오 5: 양쪽 키 무효 → `tiebreak=FrontFallback` (기존과 동일)
- [ ] 시나리오 6: 드레인 중 정지→재시작 → 재산정, 교착 없음
- [ ] 시나리오 7: 신규 시작(다이 없음) → 드레인 미구성, 기존 동작 불변
- [ ] 회귀: Rear 양보 로직 불변, 정상 연속 생산에서 드레인 로그 미발생

## 6. 실장비 시험 전 팀장님 확인
- [ ] 프롬프트 §6 잔여 사항 3건 답변 수령
      (① 다른 웨이퍼 시 PickedAt 판정, ② non-target만 보유 시 Front 폴백, ③ 시각 변경 리스크 수용)
- [ ] 변경 파일·함수·라인 목록 최종 고지 및 편집 승인
