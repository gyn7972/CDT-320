# 체크리스트 — InputCamera 선행검사 조기 허가 (input-camera-early-permission.md)

작성일: 2026-07-22

## 계획 요약 (코드 정독 반영 — 프롬프트 대비 보강 2건은 레포트에 명시)
- 허가를 "마지막 Die EPD + VisionX Avoid" 시점에 발행(R1), RESULT 회수는 픽업
  `CalculatePickTargets`로 이동(R3). 회수/드레인 코어는 prepare 시퀀스에서 추출·공용(R3/R4).
- **보강 1**: RESULT 회수에는 offset 변환만이 아니라 `ApplyInputDieVisionOffset`(811~917행)의
  ①InputPickVision 자재 기록(UpsertInspection) ②마지막 다이 오프셋의 미촬영 다이 전파
  (TryApplyLastVisionOffsetToPendingInputDies, 한계 검사 포함)가 붙어 있음 — 픽업 회수 시에도
  동일 수행해야 동작 동등 (프롬프트의 "회수 코어" 정의에는 누락되어 있었음).
- **보강 2**: 시뮬/DryRun 조기 허가 경로는 offset이 EPD 시점에 이미 채워져 회수 분기를 안 타므로
  기록/전파가 누락됨 → `VisionOffsetApplied` 플래그를 Prepared/Batch 아이템에 추가해
  "기록 미적용 항목"을 픽업에서 일괄 처리 (내부 경로는 prepare가 적용 후 플래그 세팅 → 무변화).
- 현장 디버그 커밋(292c8ab4/a3288119)의 임시 수정(alignOffsetX+=0.02, VerifyPickTarget 주석,
  저속존 제거, AXM 재시도)은 **보존** — 해당 영역 편집 시 그대로 유지.

## 구현
- [x] C1. InputDieVisionPrepareSequence — 회수 코어 추출:
  `internal static Task<VisionAlignResult> CollectInputDieVisionResultCoreAsync(handle, ct)`
  (WaitInspectionStageAsync(Result, 5000) → ToAlignResult(Wafer) → 성공 시 RecordAlign →
  offset/null). CollectVisionResultsAsync(668~686행)를 코어 호출로 치환 (동작 불변)
- [x] C2. 드레인 코어 추출: `internal static Task DrainInputVisionRequestHandleAsync(handle,
  reason, logOwner, ct)` — DrainOutstandingWaferResultsAfterFailureAsync(99~142행) 1건 처리
  미러, 기존 메서드는 코어 사용. + `public Task DrainPreparedResultsAfterFailureAsync(ct)` 노출
- [x] C3. InputDieVisionPreparedItem에 `VisionOffsetApplied` 추가, prepare
  ApplyInputDieVisionOffset 성공 시 true. InputCameraPickUpPermissionStore.CloneItem에
  VisionOffsetApplied 복사 추가
- [x] C4. InputCameraMarkInspectionSequence R1: WhenAll(216~235행) 제거 → visionAvoid만 await.
  실패 시 ReleasePreparedReservations + **핸들 드레인**(C2 공개 메서드). 성공 시 RESULT 없이
  _inspectedItems 채움 + "RESULT는 PickUp CalculatePickTargets에서 회수" 로그 → Grant
- [x] C5. R2: PickUpBatchItem에 VisionRequest/VisionRequestIndex/VisionOffsetApplied 추가,
  두 생성부(내부 489-497·허가 587-596) 복사. TryLoadInputCameraMarkInspectionPermission
  검증 완화(618-623행): `VisionOffset != null || (ExposureCompleted && VisionRequest != null)`
  아니면 Fail (메시지 갱신)
- [x] C6. R3: `CalculatePickTargets(bool)` → `CalculatePickTargetsAsync(bool, ct)` + 호출부 5곳
  (디스패처 232, 수동 4871/4985/5077/5169) 갱신. 계산 루프 전에
  `CollectPendingBatchVisionResultsAsync(ct)`: 아이템 순서(피커 4→1)대로 ①offset 없으면 코어
  회수(실패 → SKIP: ReleaseInputStagePickReservation+RemoveInspection+ApplyManualDieState+배치
  제거, prepare SkipCurrentVisionFailedDieAndContinue 758~809행 동일 경로) ②기록 미적용이면
  UpsertInspection(base 헬퍼 3529/3573/3590행) + 플래그 세팅. 루프 후 "픽업이 기록을 적용한
  마지막 항목 == 배치 마지막 항목"이면 미촬영 다이 전파(847~901행 동일: cameraOffset →
  한계 검사 Fail → TryApplyLastVisionOffsetToPendingInputDies). 전부 SKIP 시 Complete +
  ReleaseInputStageArea 정상 종료. async 전환에 따른 OperationCanceledException 재던짐 추가
- [x] C7. R4: ①스토어 ReleaseItems(미소비 폐기)에서 pending 핸들 fire-and-forget 드레인
  (Task.Run + 코어, CancellationToken.None, 예외 관찰) ②픽업 ExecuteAsync finally +
  StopRemainingPickBatchForWaferCompletion에 배치 잔여 핸들 드레인(동일 방식, 회수 완료
  핸들은 코어의 IsResultDone/Error 검사로 자연 무시)
- [x] C8. R5 범위: 픽업 내부 경로(PrepareInputDieVisionBatchAsync)·CollectVisionResultsAsync
  공개 동작 무변경. 현장 디버그 임시 수정 보존

## 검증
- [x] V1. 솔루션 빌드 통과, 신규 경고 0
- [x] V2. 내부 경로 회귀 — CollectVisionResultsAsync가 기록·전파 후 플래그 세팅 →
  픽업 회수 루프 전 항목 스킵(코드 리뷰), 동작 불변
- [x] V3. 시뮬/DryRun 회귀 — 조기 허가: offset 보유+기록 미적용 → 픽업에서 기록/전파 수행
  (구 흐름과 동일 결과). 내부 경로: 무변화
- [x] V4. 조기 허가 타임라인 — 허가 발행이 RESULT 회수와 무관함을 코드 순서로 확인
  (Run: EPD 루프 → Avoid await → Grant, Collect 호출 없음)
- [x] V5. SKIP/전부 실패 경로 — 1건 실패 시 해당 다이만 제거·예약 해제 후 계속, 전부 실패 시
  Complete 정상 종료 (코드 리뷰)
- [x] V6. 핸들 드레인 — 스토어 폐기/픽업 조기 종료/mark 실패 경로 각각 드레인 연결 확인
- [x] V7. 하네스 — PrefetchHarness T2 확장: VisionRequestIndex/ExposureCompleted/
  VisionOffsetApplied 클론 보존 확인 (VisionRequest 참조 복사는 코드 리뷰)
- [x] V8. 레포트 — 변경 후 타임라인(허가 시점/RESULT 회수 위치), 프롬프트 보강 2건,
  현장 확인 항목(허가 시점 로그 타임스탬프, RESULT 지연 시 CalculatePickTargets 대기)

## 검증 결과 기록 (2026-07-22)
- V1: 빌드 EXIT=0. 신규 경고 0 — PickerPickUpSequence CS0219(slowApplied, 3152행)는 현장 디버그
  커밋 292c8ab4(저속존 블록 삭제)가 만든 기존 경고로 이번 작업과 무관.
- V2: 내부 경로 — CollectVisionResultsAsync의 Apply가 VisionOffsetApplied=true 세팅 →
  픽업 회수 루프에서 offset 보유+적용 완료로 전 항목 통과(무변화). 회수 코어 치환은
  원 코드 기계적 이동(동작 불변).
- V3: 시뮬/DryRun 조기 허가 — offset 보유+미적용 → 픽업에서 기록/전파 수행(구 흐름과 동일
  결과·시점만 이동). 핸들 없음이어도 R2 검증은 offset 보유로 통과.
- V4: 조기 허가 타임라인 — RunInputCameraMarkInspectionAsync: EPD 루프(RunAsync) →
  MoveInputVisionXToAvoidAsync await → Grant. CollectVisionResultsAsync 호출 제거 확인.
- V5: SKIP — 실패 항목: 예약 해제+RemoveInspection+ApplyManualDieState 후 배치 제거, 루프 계속.
  전부 SKIP → _pickBatchItems.Count==0 → Complete+ReleaseInputStageArea 정상 종료.
- V6: 드레인 3경로 — ①스토어 ReleaseItems(Grant 덮어쓰기/Clear) → ScheduleHandleDrain
  ②픽업 ExecuteAsync finally(!keepCurrentState) ③StopRemainingPickBatchForWaferCompletion.
  mark 실패 경로는 DrainPreparedResultsAfterFailureAsync. 회수 완료 핸들은 코어의
  IsResultDone/Error 검사로 자연 통과(중복 드레인 무해).
- V7: PrefetchHarness 확장 16체크 ALL PASS — T2에 VisionRequestIndex/ExposureCompleted/
  VisionOffsetApplied 클론 보존 추가. VisionRequest 핸들 참조 복사는 코드 리뷰
  (CloneItem 165행 계열 — 참조 복사 유지).
- V8: 레포트 기재. 3회 반복 불필요 — 1차 구현에서 전 항목 충족.