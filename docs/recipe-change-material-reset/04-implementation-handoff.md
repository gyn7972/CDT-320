# 레시피 변경 초기화 — 구현 인계 및 후속 검토

작성일: 2026-09-06
기준: D:\Source\CDT-320_New / master / HEAD 478ffef077ff0b95bfa64e796e3970e4810b093d + 현재 미커밋 변경

## 구현 결과

ProjectPage에서 A→B를 적용하면 대상 Project/전체 Unit을 사전검증하고 적용 준비를 독점한다. 작업자가 실제 제품 제거에 ‘예’를 선택한 뒤 상태·운전·실입력·파일을 재검증한다. 새 레시피를 적용하고 새 카세트 구성의 빈 Material, 맵·재개·임시 검사·FLOW·Review 상태를 초기화한다. Material flush와 마지막 레시피/설정 저장이 끝나고 정합성을 확인한 경우에만 성공을 표시한다.

중간 실패에는 단계/대상 이름을 남겨 START를 차단한다. 실패 대상 재시도는 필요한 초기화를 다시 수행한다. 기존 Recipe를 임의로 재로드하여 복구하지 않는다. 정상 같은 레시피 재적용과 기동 Material 사용 ‘예’는 전체 초기화에서 제외하며, 기동 ‘아니오’는 같은 초기화 핵심을 사용한다.

## 주요 소유 코드

| 파일 | 역할 |
|---|---|
| [Form1.RecipeReset.cs](D:/Source/CDT-320_New/QMC.CDT-320/Form1.RecipeReset.cs:23) | 전체 전환 순서, UI 보호, 실패 처리, 기동 새 상태 공통 연결 |
| [MachineController.RecipeReset.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/MachineController.RecipeReset.cs) | 정지/작업 종료/실입력 게이트, 적용 보호, 종료 runtime 정리와 START 실패 차단 |
| [MaterialStateService.RecipeReset.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.RecipeReset.cs:19) | Review 실행 추적, reset 상호 배제, 새 State 교체/세대/캐시/잔여 Review 정리 |
| [RecipeMaterialStateFactory.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/RecipeMaterialStateFactory.cs) | 기존 State를 변경하지 않는 빈 Material 후보 생성 |
| [RecipeApplyFileSnapshot.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Recipes/RecipeApplyFileSnapshot.cs) | 적용 대상 파일 집합/내용의 동일성 검증 |
| [PendingSequenceTaskRegistry.cs](D:/Source/CDT-320_New/QMC.CDT-320/Sequencing/Common/PendingSequenceTaskRegistry.cs) | 부모가 취소/시간 초과로 먼저 반환해도 등록된 실제 Task 완료 전 전환·START 차단 |
| [RecipeStore.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs:298), [AppSettings.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/AppSettings.cs:477) | 기존 저장 API를 유지하며 성공/실패를 반환하는 저장 경로 추가 |

기존 Form1/ProjectPage 적용 진입점, Controller Recipe/Manual/INIT, MotionGuardRuntime 및 IO/Jog 컨트롤에 필요한 연결을 했다. 카세트 투영 초기화와 선행 검사/독립 후퇴의 실행 종료 확인도 연결했다. 새 소스는 csproj에 등록했다.

실제 종료 확인을 위해 AutoSequenceCoordinator의 Unit/waferMonitor/prefetch, 공통 SequenceAwaiter Step, MachineReadySequence 자체 대기, PickerPlace의 상승, PickerProcess의 동적 이동, PickerBottomAndSideInspectionSequence의 최종 결과 Task를 등록한다. Task.IsCompleted인 작업만 조회에서 제거한다. 기존 취소·시간 초과나 이동 순서를 변경하지 않는다. 이후 별도로 분리되는 비동기 작업을 추가하면 이 등록 또는 기존 실행 종료 게이트에 포함되는지 검토해야 한다.

## 다른 작업과 겹친 정확한 변경

사용자 승인 후 다음 기존 파일의 메서드 선언 3곳만 변경했다.

1. [MaterialStateService.InputPick.cs:701](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputPick.cs:701): TryApproveInputStageRunReviewWithDefaultOrder → private Core.
2. [MaterialStateService.InputPick.cs:851](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputPick.cs:851): CommitInputStageRunReview → private Core.
3. [MaterialStateService.InputStageReviewGeometry.cs:286](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs:286): TryEstablishInputStageReviewBaseline → private Core.

원래 public 이름/인자/반환형은 새 MaterialStateService.RecipeReset partial의 래퍼가 제공한다. 래퍼는 기존 Core를 그대로 호출하며 진입부터 동기 저장 후 반환까지 실행 카운터를 유지한다. reset 보호 중 신규 저장 진입은 false로 거절한다. 다른 작업이 public 원래 이름의 메서드를 다시 추가하면 중복되므로 기존 Core 본문과 새 래퍼를 함께 확인해야 한다.

선언 변경을 메모리에서 역변환한 SHA256이 수정 전 파일과 동일하다. [보존 근거](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/review-existing-diff-preservation.log>). 기존 검사/OFFSET/CONFIRM 본문은 교체·롤백하지 않았다.

Form1.InputStageRunReview.cs와 Geometry partial 자체는 수정하지 않았다. [새 UI 초기화 함수](D:/Source/CDT-320_New/QMC.CDT-320/Form1.RecipeReset.cs:287)에서 기존 ClearInputStageRunReviewPendingOffset API를 호출해 요청 세대/토큰을 만료시키고 종료된 UI 참조와 캡처 좌표만 정리한다. 실제 Cleanup/Stop/Scope 강제 Dispose를 호출하지 않는다.

다른 작업의 Review Dialog/Designer, PickTargets, 새 PickupOrder 관련 파일 및 해당 csproj 등록은 유지했다. 현재 git diff 전체에는 그 작업이 함께 포함되므로 모든 변경을 이 초기화 작업의 결과로 취급하면 안 된다.

## 후속 검토 시 사용할 프롬프트

D:\Source\CDT-320_New의 현재 로컬 코드에서 레시피 변경 초기화와 InputStage Review를 함께 검토하라. 브랜치를 만들거나 다른 체크아웃을 복사하지 말고 기존 미커밋 변경을 보존하라. 위 public 래퍼/기존 Core 3곳의 연결을 유지하면서 새 Review 기능이 적용 중 진입하지 않는지, 종료되지 않은 저장을 남긴 채 reset이 되지 않는지 확인하라. 실패 후 남은 baseline/pending 표식은 실제 실행 카운터와 구분하라. UI 요청 세대·승인·Wafer/State 참조 확인과 동기 저장 완료 후 최종 판정이 함께 유지되는지 검토하라. 새로운 비동기 저장 진입점을 추가한다면 실행 추적 범위에 포함되는지 확인하라. 실제 장비 검증 전에는 아래 체크리스트의 현장 항목을 완료로 표시하지 말라. 추가 시퀀스/인터락 동작 변경이나 다른 작업과 겹치는 구현은 사용자 승인 범위를 먼저 확인하라.

## 검증 근거와 한계

최종 기능 빌드 실행파일 SHA256: B3CCE018ABAD8478E4A0B26D9B6AC992D8BBD6BC28889767BDFB134BA440F418.

- 전체 솔루션 격리 Build 성공, 종료 코드 0. 두 프로젝트의 .cs/.csproj는 빌드 전후 해시 변동 0건.
- 실제 어셈블리 Controller 185, MSS 67, Review UI helper 115 assertions 통과. Controller에는 실제 SequenceAwaiter 취소 후 underlying Task와 부모/자식 완료 차이의 START/전환 차단 검증이 포함된다.
- 실제 Form1.RecipeReset.cs + 외부 stub workflow 50개 시나리오/281 assertions 통과.
- 빈 후보 65, 저장 API 34, 카세트 투영 15, 파일 동일성 21, 직접 모션/IO 보호 103, 기존 FLOW 복구 회귀 183 assertions도 통과. 각 실행의 대상 범위와 해시는 결과 파일에서 구분한다.
- 소유/연결 소스와 프로젝트 30개 모두 UTF-8 BOM·CRLF·마지막 줄바꿈 확인, 최종 빌드 소스와 해시 일치, 전체 git diff --check 통과. 공유 csproj는 현재 등록을 보존한 채 새 파일을 등록하고 줄바꿈만 정규화했다. 다른 작업 Dialog의 줄바꿈 경고는 그 파일을 일괄 포맷하여 없애지 않았다.
- main Form/장비 생성자, 실제 모션/드라이버/IO, 운영 Snapshot 저장 및 실장비 시퀀스는 실행하지 않았다. 실제 센서, 적용 직후 재시작, 저장 중 프로세스 종료는 현장/별도 통합 시험이 필요하다.

[빌드 로그](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/build.log>) · [Controller](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/controller-results.log>) · [Material/Review](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/material-state-results.log>) · [Review UI](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/review-ui-checks/README.md>) · [Workflow](</C:/Users/QMC/Documents/CDT-320 2/output/recipe-change-material-reset-20260906/workflow-checks/README.md>)

[전체 체크리스트](D:/Source/CDT-320_New/docs/recipe-change-material-reset/02-validation-checklist.md)에 현장 미실시 항목을 별도로 남겼다. 이후 다른 작업이 수정한 소스는 다시 빌드하고 관련 검증을 수행해야 한다.
