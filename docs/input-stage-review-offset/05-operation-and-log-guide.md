# InputStage Review — 기존 시작 조작과 로그 분석 가이드

작성일: 2026-09-06
소스 기준: D:/Source/CDT-320_New, HEAD 478ffef077ff0b95bfa64e796e3970e4810b093d 이후 이번 로컬 수정
최신 오프라인 검증: 183개 PASS, 실제 등록 프로젝트 격리 Build 오류 0개·기존 경고 41개. 실제 UI/장비·저장 장애 통합 시험, 장비 구동·운영 설정 변경·배포는 수행하지 않았다.
연결 문서: [구현 현황](03-implementation-status.md), [검토 결과와 한계](04-review-findings-and-limits.md), [체크리스트](02-validation-checklist.md)

## 1. 작업자가 사용하는 순서

기존처럼 Align/Die Mapping 뒤 표시되는 **자동 운전 Review 창**에서 시작점과 픽업 순서를 확인하고 **CONFIRM / CONTINUE AUTO**를 누른다. 기준 다이 3개를 고르거나 VERIFY MAP을 실행하지 않는다. 각 점으로 이동·촬영한 영상을 하나씩 확인하는 추가 절차도 없다.

CONFIRM은 내부적으로 현재 wafer/mapping/recipe/T/pitch/후보/세션 자료와 저장 결과를 확인한다. 조건이 바뀌거나 저장에 실패하면 안내된 이유를 확인해야 하며 무조건 시작하도록 우회하지 않는다. 별도 실측을 하지 않았으므로 이 확인을 물리 좌표 합격으로 표시하지 않는다.

맵 위치를 수동 보정할 필요가 있을 때만 기존 기능을 사용한다.

1. 기존 방법으로 대상 Die/맵 기준과 현재 Vision 위치를 확인한다.
2. **DIE DETECTION**으로 현재 영상의 Die 중심을 검출한다. 이 기존 기능은 T/EjectPinZ 준비와 검출 중심 이동을 포함하므로 실제 모션 작업이다.
3. 필요하면 **APPLY OFFSET**으로 검출량을 Draft Map에 반영한다. 단발·누적 제한과 검출 기준의 유효성은 계속 확인한다.
4. 기존 시작점·순서를 확인한 뒤 **CONFIRM / CONTINUE AUTO**로 저장·확정한다. 이 뒤에 새 3점 절차가 붙지 않는다.

이미 자료가 일치하고 보정이 필요 없다면 DIE DETECTION/APPLY를 매번 실행하는 절차를 추가하지 않는다. 기존 완료·시작점·순서 조건은 유지한다. `READ ONLY PREVIEW`는 원래대로 표시용이며 이 화면에서 운전을 시작하거나 축을 움직이는 절차가 아니다.

## 2. Simulation과 구형 자료

- **SKIP RUN REVIEW IN SIMULATION**이 켜진 기존 경로는 그대로 Review를 건너뛴다. 꺼져 있으면 기존 Review에서 CONFIRM을 사용한다. 시뮬 시작은 실측 검증 성공이 아니다.
- 새 확인 ID의 `CONFIRM-CONTEXT-PRODUCTION-` / `CONFIRM-CONTEXT-NONPRODUCTION-`는 현재 모드를 구분한다. 실제 다이 측정 성공 표시가 아니다.
- baseline 없는 구형 Material은 변경하지 않은 현재 canonical 좌표를 최초 기준으로 채택하고 저장한다. **이전 누적 Offset은 복구하지 못한다.** 이후 누적량은 새 기준 이후 값이다.
- 이미 진행된 자재는 기존 좌표·상태·남은 순서를 보존한 확인만 허용한다. 전체 승인 목록/revision 소실이나 진행 좌표 변경은 별도 제약이다. 남은 다이가 3개 미만이라는 새 실측 조건은 없다.
- 큰 보정도 현재 단발/누적 설정을 따른다. 기본 20 mm와 저장된 큰 값을 유지하며 반 피치 상한을 추가하지 않았다. 한계 안이라는 사실은 동일 물리 다이의 입증이 아니다.

## 3. 로그 위치와 종류

새 로그는 기존 **Event CSV** 저장 경로에 들어간다. 기본 전체 모드는 `<LogRoot>/Event`이고, 종류별 모드에서는 Review는 `Event`, 픽업은 `FrontHeadSeq` 또는 `RearHeadSeq` 폴더를 사용한다. 실제 경로는 현재 EventLogger 경로 설정이 우선하므로 고정 배포 폴더를 추정하지 않는다. 파일이 분할되면 같은 날짜의 분할 CSV 전체를 함께 분석한다.

CSV 열은 `When, Kind, User, Code, Source, Description`이다. Review의 Code는 `IN-REVIEW-*`, Source는 `InputStageReviewGeometry`, Description은 `InputStage Review 진단 ` 뒤의 JSON이다. JSON schema는 **`input-stage-review-v1`**다. 픽업 Code는 **`PICKUP-TARGET`**이며 Source는 `PickerPickUpSequence`다.

두 신규 계열은 Audit으로 기록하므로 DiagnosticVerbose가 꺼져도 영속 대상으로 분류한다. 승인 소비 거부는 같은 상태·이유가 반복될 때 매 50 ms 다시 쓰지 않고 변경 시 기록한다. 기존 상세모드에서는 일반 픽업 로그와 Audit에 같은 계산 메시지가 함께 남을 수 있다. 이것을 두 번 픽업한 것으로 세지 않는다.

Audit은 비동기 logger의 영속 대상이라는 뜻이다. 전원 차단·디스크 장애까지 실제 파일 쓰기 성공을 무조건 보장하는 반환값은 없다. `SAVE-COMPLETE`의 저장 성공은 Material 승인 자료 저장에 관한 것이며 logger 디스크 쓰기 내구성과는 별개다.

사건 분석 원본 폴더는 **D:/Source/EQP_Handler/20260905/핸들러로그**다. 9월 4일 원본 로그에는 이번 신규 코드가 생성한 Audit 이벤트가 없을 수 있다. 신규 로그가 없다는 이유만으로 당시 작업이 없었다고 판단하지 않는다.

## 4. Review 이벤트를 읽는 순서

다음은 현재 실제 코드의 이벤트 이름이다. 밑줄과 하이픈을 임의로 바꾸지 않는다. Code는 표의 값 앞에 `IN-REVIEW-`를 붙인 이름이다.

| event / Code 뒤쪽 | 의미 | 성공으로 해석하면 안 되는 범위 |
|---|---|---|
| `REVIEW_OPEN` | Review 표시 시점의 현재 Material/recipe/map 스냅샷 | 실측 또는 저장 완료 아님 |
| `BASELINE-ADOPTED` | 구형 canonical 원점을 최초 기준으로 채택한 이력 | 과거 누적량 복원 아님, 저장 결과는 details 확인 |
| `BASELINE-SAVED` | 검출용 최초 기준의 저장·현재 조건 재확인 완료 | 물리 위치 합격 아님 |
| `DETECTION_REQUEST` | 선택 UID/reference로 단일 검출 시작 | 결과 수신 아님 |
| `MATCH_RESULT` | MATCH 원본 pixel/각도/score/원문 응답 | mm 변환 전 값이며 실제 피치 측정 아님 |
| `DETECTION_RAW` | 촬영 직전 고정 좌표와 mm 변환된 Vision 결과 | Draft 적용 전, 미수신 또는 시뮬 구분 필요 |
| `DETECTION_CANDIDATE` | reference 대비 offset·후보 원점·누적량 계산 | 제한 검사/이동/Draft 적용 완료 아님 |
| `DETECTION_PENDING` | 검출 중심 이동 뒤 pending 등록, 이동 후 actual 포함 | `draftApplied=false` |
| `OFFSET-RECEIPT` | Material의 단발/누적·출처 검사를 통과한 후보 이력 | Draft 변경·최종 저장 완료 아님 |
| `APPLY_REQUEST` | Draft 평행이동 요청, 적용 전 origin | 성공 확정 아님 |
| `APPLY_COMMITTED` | Draft 좌표 적용 성공, 적용 후 origin | `materialSaved=false`, 최종 Material 저장 아님 |
| `CONFIRM-PREPARED` | 기존 CONFIRM에 현재 context·승인 ID 준비 | 저장/자동 재개 완료 아님 |
| `CONFIRM_SUBMITTED` | UI 확인 자료·시작 UID·순서 수 제출 | 저장 완료 아님 |
| `COMMIT-PREPARED` | Material Commit에 전달된 확인 자료 | 최종 저장 완료 아님 |
| `SAVE-COMPLETE` | 승인 자료 저장 후 같은 wafer/승인/조건을 재확인 | 물리 좌표·실제 픽업 성공 아님 |
| `REQUEST_INVALIDATED` | selection/Jog/STOP/종료 등으로 이전 요청 무효화 | 과거 pending을 이어서 적용하면 안 됨 |
| `ACTION_CALLBACK_FINISHED` | 기존 수동 callback의 완료/결과 진단 | 새 좌표 승인 아님 |

표의 순서를 모든 이벤트의 엄격한 발생 순서로 가정하지 않는다. 특히 `BASELINE-ADOPTED`는 채택 경로의 finally 기록이므로 `BASELINE-SAVED` 뒤에 나올 수도 있고 저장 실패 후에도 남을 수 있다. 실제 저장 성공은 `BASELINE-SAVED` 또는 해당 최종 승인 저장 결과와 details로 판단한다.

거부/실패 검색은 `DETECTION_REJECTED`, `APPLY_REJECTED`, `CONFIRM_REJECTED`, `CONFIRM-REJECT`, `COMMIT-REJECT`, `SAVE-REJECT`, `APPROVAL-REJECT`, `COMMIT_PROCESSING_FAILED`를 사용한다. UI와 Material의 다른 경계에서 같은 요청의 거부가 각각 남을 수 있다. `details`의 이유와 wafer/session/request를 함께 읽는다. 취소나 일부 예외에서는 RAW/PENDING/SAVE까지 모든 단계가 생성되는 것이 정상 조건이 아니다.

## 5. JSON과 raw 값의 해석

| 필드 묶음 | 읽을 항목 |
|---|---|
| 현재 물리 자재/작업 | `waferId`, `waferInstanceId`, `processingGeneration`, `lotId`, `recipe`, `materialRecipe` |
| Align/Mapping 출처 | `alignRunId`, `mappingAlignRunId`, `alignMode`, `mappingMode`, `mappingRevision`, `recipeMapHash`, `mappingRecipeMapHash` |
| 요청/저장/후보 비교 | `contextWaferId`, `contextMappingRevision`, `session`, `request`, `contextConditionSignature`, `contextCandidateSignature`, `savedConditionSignature`, `savedCandidateSignature`, `draftCandidateSignature` |
| 원점/피치/기준 | `originX/Y`, `pitchX/Y`, `dieSizeX/Y`, `mapCount`, `baselinePresent`, `baselineOriginX/Y`, `cumulativeX/Y` |
| T와 이벤트 시점 실제 축 | `alignT`, `mappingT`, `actualCameraX`, `actualStageY`, `actualStageT` |
| 설정 한계/카메라 | `singleLimitX/Y`, `cumulativeLimitX/Y`, `pixelToMmX/Y`, `imageCenterPixelX/Y`, `inputToBottomOffsetX/Y` |
| 모드·실측 여부 | `contextNonProduction`, `simulationMode`, `dryRunMode`, `useVision`, `useAjin`, `savedApprovalId`, `savedApprovalKind`, `savedMeasurementCount`, `physicalVerification` |

일부 이벤트에는 context/draft가 없으므로 해당 값이 `null`일 수 있다. JSON의 현재 Material 값과 검출 당시 context 값이 다른 경우에는 어느 시점 값인지 구분한다. `physicalVerification=not-performed-by-this-event`와 `savedApprovalKind=context-confirmation-without-physical-measurement`는 의도적으로 실측하지 않았음을 나타낸다. 과거 자료의 `legacy-multipoint-evidence`는 이전에 저장된 종류이지 현재 이벤트의 새 측정이 아니다.

`details`에는 이벤트별 원시 자료가 문자열로 들어간다.

- `MATCH_RESULT`: `dieUid`, `requestId`, `groupId`, `matchSuccess`, `matchPixelX`, `matchPixelY`, `matchAngleDeg`, `matchScore`, `hasImageSize`, `imageWidthPixel/HeightPixel`, `raw`, `rawError`.
- `DETECTION_RAW`: `dieUid`, `gridX/Y`, `captureX/Y/T`, `referenceX/Y`, `visionResult`, `visionDeltaMmX/Y`, `visionDeltaThetaDeg`, `alignResultPitchFieldX/Y`, `simulatedNominalFallback`.
- `DETECTION_CANDIDATE`: `detectedCenterX/Y`, `offsetX/Y`, `candidateOriginX/Y`, `cumulativeX/Y`와 계산 부호.
- `DETECTION_PENDING`: `afterCenterMoveX`, `afterCenterMoveY`, `afterCenterMoveT`와 `draftApplied=false`.
- `APPLY_*`: `beforeOriginX`, `beforeOriginY`, 성공 시 `afterOriginX`, `afterOriginY`, `materialSaved=false`.

**`0.15`를 실제 피치 측정으로 해석하지 않는다.** 기존 `ToAlignResult(..., 0.15)` 호출의 변환 인수가 `alignResultPitchFieldX/Y`에 들어갈 수 있다. MATCH_RESULT에 `conversionPitchArgumentMm=0.15; pitchMeasured=false`를 명시한다. JMB 맵 피치 8.37/6.37과 이 0.15를 비교해 피치 불량이라고 결론 내리면 안 된다. 이번에 해당 변환 호출·인수·실제 수식은 변경하지 않았다.

MATCH의 pixel과 변환된 mm를 직접 빼지 않는다. 기존 단일 검출 수식은 다음과 같다.

- `detectedCenterX = captureX + visionDeltaMmX`
- `detectedCenterY = captureY - visionDeltaMmY`
- `offsetX/Y = detectedCenterX/Y - referenceX/Y`
- `candidateOriginX/Y = beforeOriginX/Y + offsetX/Y`
- `cumulativeX/Y = candidateOriginX/Y - baselineOriginX/Y`

`visionResult=unavailable` / `matchResult=unavailable`은 실제 0 측정과 다르다. `simulatedNominalFallback=true`는 실측 결과로 계산하지 않는다. `captureX/Y/T`는 촬영 직전 고정값, `afterCenterMoveX`, `afterCenterMoveY`, `afterCenterMoveT`는 이동 후 값, JSON 최상위 `actual*`는 로그 작성 시점 값이다. 기존 per-die 검사 AlignmentSnapshot 역시 결과 반영 시 actual을 읽을 수 있으므로 선행 촬영 시 촬영 순간 좌표라고 단정하지 않는다.

## 6. UID별 실제 픽업 계산과 연결

`PICKUP-TARGET`은 `CalculateCurrentPickTarget()`에서 실제 계산이 끝난 직후 1회 기록한다. 원래 `calculated pick target` 메시지/수식과 일반 WriteLog를 유지하며 좌표를 다시 계산하지 않는다.

기존 메시지에는 `die`, `pickerNo`, `inputVisionX`, `inputStageY`, `stageY`, `pickerX/Y/T/Z`, `needleX/Z`, `ejectPinZ`, `formula`, `cameraOffsetX/Y`, `alignOffsetX/Y/T`, `visionTotalOffsetX/Y`, `pickMechanicalOffsetX/Y/T`, `pickRuntimeEnabled`, `pickRuntimeOffsetX/Y/T` 등이 포함된다. `formula` 안에는 실제 적용된 PickerAlignOffset과 변환·보정 순서가 남는다.

추가 연결 필드는 `pickupWafer`, `pickupGridX/Y`, `pickupOrder`, `pickupSide`, `pickupHeadSlot`이다. 기존 SequenceLog context가 있으면 `run`, `seq`, `step`, `mode`, `trace`도 함께 저장한다. `pickupHeadSlot`은 Front/Rear의 1~4번 head 위치이며 물리 콜렛 serial이 아니다. 새 Material 재조회나 전체 맵 dump를 하지 않는다.

분석 순서는 다음과 같다.

1. `RECIPE-LOAD`, `LOT-START`, `LOT-COMPLETE`로 전환·Lot 구간을 잡는다. 사건 원본에서는 2026-09-04_014.csv의 18:10~18:16과 이후 작업을 연결한다.
2. 해당 `waferId/waferInstanceId/processingGeneration/mappingRevision`의 IN-REVIEW 로그를 시간순으로 모은다. recipe hash, T, pitch, baseline과 두 번 이상의 APPLY를 비교한다.
3. 마지막 APPLY_COMMITTED와 SAVE-COMPLETE를 구분하여 실제 확정 원점을 찾는다. 거부/무효화가 있었는지 함께 확인한다.
4. 동일 `die`/wafer/grid의 PICKUP-TARGET을 찾아 raw Vision, learned XYT, 기계 보정과 최종 목표를 비교한다. 실제 Motion·Bottom 검사 결과를 연결한다.
5. 정상과 이상 사례를 같은 head slot·recipe·Material 재사용/재시작 조건으로 비교한다. 목표가 맞다는 사실만으로 물리 픽업이 맞았다고 결론 내리지 않는다.

기존 `AUTO-VISION-CORRELATED-REQ/RESULT`에는 wafer/recipe/Lot/grid/request/group 및 원문 결과가 있다. 이 식별자는 실제 값이 있는 경우에만 연결한다. 시간이나 반복 Die 모양만으로 다른 요청·프레임을 동일하다고 추정하지 않는다. 새 Review 로그만으로 인풋 카메라 이후의 콜렛 편차·물리 미끄러짐까지 해소됐다고 판단하지 않는다.
