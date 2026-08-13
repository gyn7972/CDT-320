# CDT-320 구현 체크리스트 — 피커 선입선출(FIFO) 재시작 드레인 타이브레이크

- 작성일: 2026-08-13 / **구현·정적검증 완료: 2026-08-13** (시뮬 검증은 실장비/시뮬 환경 대기)
- 선행 문서: [04_design.md](04_design.md)
- 수정지시 프롬프트: `..\..\피커선입선출_재시작드레인_FIFO_수정지시_프롬프트_2026-08-13.md`
  (프롬프트 §7과 동일 내용 — 함께 갱신할 것)
- 키 순서(팀장님 확정): **1차 InputSequenceNo(같은 웨이퍼일 때) → 2차 PickedAt → 폴백 Front**

## 1. 코드 편집 — PickerFirstForwardSequencer.cs
- [x] `PickerResumeDrainFifoKey` 구조체 신설 (HasSequenceNo/SequenceNo/WaferKey/HasPickedAt/PickedAt) — 파일 하단
- [x] side별 FIFO 키 static 저장소 추가 (`ResumeDrainFifoKeys` + `_resumeDrainConfigureDetail`)
- [x] `BeginRun()` — 키 저장소·detail 초기화 추가
- [x] `ConfigureResumeDrain` — 시그니처 확장(키 2개) + required side만 키 저장 + 진입 시 초기화
      + configure detail 산출, `GetResumeDrainConfigureDetail()` 조회 API
- [x] `IsResumeDrainFifoWinnerNoLock`/`CompareResumeDrainFifoNoLock` 비교 함수 신설
      (1차 순번[같은 WaferKey일 때만]→2차 시각→Front 폴백, out tiebreakDetail로 판정 근거 생성)
- [x] `IsHighestResumeDrainPriorityNoLock` 동률 분기 → 비교 함수 호출로 교체
- [x] `IsHigherResumeDrainPriorityNoLock` 동률 분기 → **동일** 비교 함수로 교체
- [x] `DescribeResumeDrainSideNoLock` — 키 요약 추가 (`DescribeFifoKey`/`ShortWaferKey` 신설)

## 2. 코드 편집 — AutoSequenceCoordinator.cs
- [x] `ResolveRestartPickerDrain` — IsInputTarget 다이만 집계해 FIFO 키 산출(out 추가)
      - InputSequenceNo > 0 중 최솟값 + 대표 다이 waferKey(InputWaferInstanceId, 비면 WaferID_Input)
      - PickedAt 유효 기준: `> new DateTime(1900,1,1,23,59,59)` (Compactor 관례)
      - `GetPickerDieSortTime`(UpdatedAt 혼입) 미사용 — `die.PickedAt` 원본만
- [x] `ConfigureRestartPickerDrain` — 확장 시그니처로 호출 갱신
- [x] 키 산출 로그 — frontFifoKey/rearFifoKey(seq/wafer축약/pickedAt ms포함) + reason에 targetDieCount
- [x] 판정 결과 로그 — `drainOrder=..., tiebreak=SequenceNo|PickedAt|FrontFallback(비교 수치 포함)`
      (LogPublic + 파일 로그 양쪽)

## 3. 무변경 확인
- [x] rank 산정(RankBottomSide/RankPickUp) 무변경
- [x] `IsHighestPriorityNoLock`(첫 전진 게이트) 무변경 — Front 동률 규칙 그대로
- [x] `TryYieldExpectedSideToPriority` / Rear 양보 로직 무변경
- [x] `MaterialStateService` 무변경 (읽기 전용)
- [x] `PickerProcessSequence` 무변경
- [x] 신규 모션 코드 없음 (MotionSpeedScale 위반 없음)

## 4. 정적/빌드 확인
- [x] `/p:OutDir=<scratchpad>\fifo_build\` 빌드만 사용 (실장비 폴더 무접촉) — Release|x64 빌드 성공
- [x] 수정 2개 파일의 신규 경고 0건 (빌드 경고는 전부 기존 파일分: MotionPage.Designer CS0169,
      OutputFeederInterlockRules CS0168, PickerProcessSequence CS4014 — 기존부터 존재, 미수정)
- [x] `ConfigureResumeDrain` 호출부 1곳(AutoSequenceCoordinator:402) 전체 검색 확인
- [x] 동률 분기 2곳이 동일 비교 함수(`IsResumeDrainFifoWinnerNoLock`) 사용 확인

## 5. 시뮬레이션 검증 (설계 §2-5 워크스루 표 대조) — **실장비/시뮬 환경에서 실행 필요**
- [ ] 시나리오 1: 같은 웨이퍼, Rear 빠른 순번 → **Rear 먼저** + `tiebreak=SequenceNo` 로그
- [ ] 시나리오 2: 같은 웨이퍼, Front 빠른 순번 → Front 먼저
- [ ] 시나리오 3: 한쪽만 보유 → rank 차이로 기존 동작, FIFO 미개입(`tiebreak=unused`)
- [ ] 시나리오 4: 서로 다른 웨이퍼 → `tiebreak=PickedAt`, 먼저 픽업 쪽 먼저
- [ ] 시나리오 5: 양쪽 키 무효 → `tiebreak=FrontFallback` (기존과 동일)
- [ ] 시나리오 6: 드레인 중 정지→재시작 → 재산정, 교착 없음
- [ ] 시나리오 7: 신규 시작(다이 없음) → 드레인 미구성(`drainOrder=none`), 기존 동작 불변
- [ ] 회귀: Rear 양보 로직 불변, 정상 연속 생산에서 드레인 로그 미발생

## 6. 실장비 시험 전 팀장님 확인
- [x] 프롬프트 §6 잔여 사항 3건 답변 수령 (2026-08-13 "3건 다 그대로" 승인
      — ① 다른 웨이퍼 시 PickedAt 판정 ② non-target만 보유 시 Front 폴백 ③ 시각 변경 리스크 수용)
- [x] 변경 파일·함수·라인 목록 고지 및 편집 승인 (2026-08-13 "① 진행해줘")

## 구현 기록 (2026-08-13)
- 수정 파일 2개: `QMC.CDT-320\Sequencing\Picker\PickerFirstForwardSequencer.cs`,
  `QMC.CDT-320\Sequencing\AutoSequenceCoordinator.cs`
- 검증 빌드: MSBuild(VS18) Release|x64, OutDir=세션 scratchpad `fifo_build\` → 성공
- 남은 것: §5 시뮬 시나리오 8건 (앱은 실장비에서만 구동 — 팀장님 실행 필요)
