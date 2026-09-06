# InputStage 리뷰 좌표 보정 검증 — 구현 현황

작성일: 2026-09-06
기준 저장소: D:/Source/CDT-320_New
확인 브랜치: master, origin/master보다 93개 커밋 앞섬
확인 HEAD: 478ffef077ff0b95bfa64e796e3970e4810b093d
연결 문서: [구현 프롬프트](01-implementation-prompt.md), [검증 체크리스트](02-validation-checklist.md), [검토 결과와 한계](04-review-findings-and-limits.md), [사용법과 로그 분석](05-operation-and-log-guide.md)

## 현재 상태와 최우선 범위

사용자가 추가 3점 선택·VERIFY MAP·점별 영상 확인 절차를 원하지 않는다고 명시하여 **기존 CONFIRM / CONTINUE AUTO에서 현재 자료를 확인하는 방식**으로 수정했다. 정상 시작에 별도의 세 점 이동·촬영을 요구하지 않는다. CONFIRM은 현재 좌표·레시피·자재·세션의 일관성 확인이며, 새 영상 실측이 아니다.

최신 수정본은 **오프라인 183개 검사와 4차 실제 등록 프로젝트 격리 Build(오류 0개·기존 경고 41개)를 통과**했다. 이전 단계 오프라인 113개와 Build 오류 0개/기존 경고 41개는 아래 이력으로 구분한다. 실제 장비·카메라·IO 실행, 운영 설정 변경과 배포는 수행하지 않았다.

이번 작업은 Auto 이동 순서·축 명령·인터락 조건·픽업 수식을 변경하지 않는다. PickerPickUpSequence.PickTargets.cs의 변경은 기존 계산 문자열과 일반 로그를 유지하고 PICKUP-TARGET Audit을 추가하는 계측이다. 다른 작업에서 Interlocks/Common/MotionGuardRuntime.cs 변경이 관측되었으므로 저장소 전체에 인터락 diff가 없다는 주장을 하지 않는다. 해당 외부 변경은 이 작업의 적용/검증 범위와 분리한다.

사건 분석 기준은 D:/Source/EQP_Handler/20260905/핸들러로그의 9월 4일 18:10~18:16 RAD↔JMB 전환과 이후다. 맵 좌표 이동 증거와 실제 픽업 쉬프트 원인은 구분한다. 수동 실측 절차를 제거해도 남는 자료 일관성/후보/저장 보호는 유지하지만 모든 물리 쉬프트가 예방된다고 보장하지 않는다.

## 이번 작업의 파일 범위

이번 후속 수정의 소스는 아래 **7개**다. 이미 반영된 앞 단계 소스와 전체 dirty 파일을 이번 수정 건수에 합산하지 않는다.

| 파일 | 이번 변경의 목적 |
|---|---|
| [Form1.InputStageRunReview.cs](D:/Source/CDT-320_New/QMC.CDT-320/Form1.InputStageRunReview.cs) | VERIFY 연결 제거, 기존 CONFIRM 및 검출/후보/Draft 적용/요청 무효화 진단 |
| [Form1.InputStageRunReview.Geometry.cs](D:/Source/CDT-320_New/QMC.CDT-320/Form1.InputStageRunReview.Geometry.cs) | 3점 이동·실측 확인 경로 제거, context 확인 연결과 raw 값 형식 |
| [InputStageRunReviewDialog.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.cs) | 3점 필수 상태/안내 제거, 기존 확인 조건·결정 잠금 유지 |
| [InputStageRunReviewDialog.Designer.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.Designer.cs) | VERIFY MAP 버튼·연결·추가 행 제거 |
| [MaterialStateService.InputPick.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputPick.cs) | 기존 CONFIRM context Commit, 부분 진행 상태/순서 보존과 저장 결과 연결 |
| [MaterialStateService.InputStageReviewGeometry.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs) | context 승인, legacy canonical baseline, 후보/승인 소비 보호, Audit 진단 |
| [PickerPickUpSequence.PickTargets.cs](D:/Source/CDT-320_New/QMC.CDT-320/Sequencing/Picker/PickerPickUpSequence.PickTargets.cs) | 실제 계산 완료 메시지 재사용 PICKUP-TARGET Audit만 추가 |

다른 작업의 AppSettings.cs, Recipes/RecipeStore.cs, QMC.CDT-320.csproj, README.md, RecipeMaterialStateFactory.cs, docs/recipe-change-material-reset 및 관측된 MotionGuardRuntime.cs 변경은 이번 작업에서 수정하거나 되돌리지 않는다. 다른 채팅의 변경으로 전체 프로젝트 빌드 결과가 달라질 수 있으며 최종 확인 시 구분한다.

| 이번 작업이 변경하지 않는 동작 | 확인 기준 |
|---|---|
| 자동 Align/Review 단계 | 기존 단계 전환·피치 계산/fallback·이동·취소/AutoSkip 분기 보존 |
| 인터락·공통 Motion | 이번 소유 변경 없음; 다른 작업의 diff와 분리 |
| Pick/Place 좌표식·learned XYT·콜렛 교정 | 새 재계산/초기화/교정 없음. 기존 calculated target 문자열을 로그에서 재사용 |
| baseline 생성 훅·기존 승인 복원 연결 | 이전 구현을 사용하고 이번에 해당 파일을 수정하지 않음 |

## 이전 단계 구현과 프로젝트 등록 이력

이전 단계(기준 HEAD 7961f2ba772ea90fc6b08035b0a1604784e3a4c8)에서 InputStageReviewGeometryPolicy.cs, MaterialStateService.InputStageReviewGeometry.cs, Form1.InputStageRunReview.Geometry.cs를 실제 csproj에 등록했고, baseline/Snapshot/Config 및 UI/STOP/승인 소비 보호를 구현했다. 당시 113개 오프라인 검사와 Build 오류 0개/기존 경고 41개를 통과했다.

이들 자료형과 일부 기존 다점 정책 함수는 과거 저장 자료 호환 및 기존 검사 때문에 남을 수 있다. 현재 일반 Review에서 3점 실측을 호출하거나 필수로 요구한다는 뜻은 아니다. 이번에는 새 소스/Compile 등록이 필요하지 않으며 다른 작업 중인 csproj를 변경하지 않는다.

## 사용자 승인 후 적용한 중복 범위의 이력

이전 단계에서 신규 Compile 3개와 MaterialStateService.OutputReceive.cs의 TryBuildApprovedInputStagePickOrder 공통 승인 gate 2줄을 사용자 승인 후 적용했다. 다른 채팅의 등록 항목과 Place 완료 복구 처리를 보존했다. 두 승인 대기는 이미 해소된 이력이며 이번 변경 대기 항목이 아니다.

이번은 사용자의 3점 절차 제거·정확한 분석 로그 요청 범위에서 기존 로컬 파일을 최소 수정한다. 새 중복이 생기면 해당 범위를 별도로 확인한다. 소스 rollback/복붙/과거 코드 덮어쓰기, 운영 데이터·실장비 로그 수정은 수행하지 않는다.

## Review 동작과 판정 기준

1. 기존 시작점과 픽업 순서를 확인하고 CONFIRM / CONTINUE AUTO를 누른다. 별도 3점 선택·VERIFY MAP·점별 실측 확인은 없다. 읽기 전용 Preview는 원래와 같이 시작/모션 화면이 아니다.
2. CONFIRM에서 현재 wafer/mapping/recipe/T/pitch/후보/세션을 확인한다. 승인 ID는 CONFIRM-CONTEXT-PRODUCTION 또는 CONFIRM-CONTEXT-NONPRODUCTION으로 시작한다. 원시 측정 자료를 만들거나 실측 성공으로 표시하지 않는다.
3. 기존 Align/Mapping 완료·모드·자재·시작 순서·후보 출처 검사는 유지한다. 조건이 바뀌거나 저장에 실패하면 이유와 함께 거부될 수 있다.
4. DIE DETECTION/APPLY OFFSET은 필요할 때 쓰는 기존 수동 기능이다. 단발/누적 X/Y는 각각 기본 20 mm와 저장된 값을 사용한다. 반 피치 상한·자동 한계 축소는 없다.
5. baseline은 해당 wafer/mapping에 유지한다. 구형 자료에 기준이 없으면 변경하지 않은 canonical Material 원점을 1회 채택·저장한다. 이때 이전 적용분 누적량은 알 수 없으며 BASELINE 이벤트에 기록한다. 저장된 revision 불일치를 새 기준으로 바꿔 통과시키지 않는다.
6. 촬영 직전 actual XYT와 raw Vision, reference로 offset을 계산한다. 로그에서 검출 후보, 중심 이동 뒤 pending, Draft 적용, Material 저장 완료를 구분한다. 이전 pending의 세대/기준이 달라지면 다시 검출해야 한다.
7. 확인 제출 뒤 화면 편집 잠금과 STOP/callback 종료/scope 보호는 유지한다. 축 명령과 자동 인터락을 추가하거나 변경하지 않는다.
8. 저장 대기 중 승인 소비를 막고 완료 후 같은 wafer/승인 ID/조건을 재확인하며 캐시를 무효화한다. 저장 실패를 좌표 rollback으로 숨기지 않는다.
9. 부분 진행 자재는 좌표·상태·남은 순서가 완전히 같은 경우 확인 자료만 추가하고 기존 전체 순서·시작 기록·물리 이력을 유지한다. 기존 승인 목록/revision 소실 또는 이미 진행된 좌표 변경 제약은 남는다. 새 3점 부족 제약은 없다.
10. Simulation AutoSkip 설정 분기는 원래대로다. Skip이 꺼진 비생산 Review는 기존 CONFIRM을 사용하며 실측 검증으로 기록하지 않는다. 현재 실운전/비실운전과 저장 결과의 모드 비교는 유지한다.
11. Review의 IN-REVIEW-*와 실제 픽업 계산 완료의 PICKUP-TARGET은 Audit으로 최소모드에서도 영속 대상으로 기록한다. 승인 폴링 매번 기록하지 않고 로그 실패가 기존 반환 결과를 바꾸지 않도록 처리한다. 이벤트 목록·해석은 05 참조.

## 검증 현황

**최신 오프라인 183개 검사 PASS, 4차 실제 등록 프로젝트 격리 Build 오류 0개·기존 경고 41개.** 이전 113개 결과를 재사용하지 않고 최신 assembly로 다시 실행했다. 최종 기록: [confirmation-offline-verification.json](D:/Source/CDT-320_New/_codex_verify_inputstage_review/confirmation-offline-verification.json). 소유 소스 점검 기록: [confirmation-source-checks.json](D:/Source/CDT-320_New/_codex_verify_inputstage_review/confirmation-source-checks.json).

| 항목 | 현재 결과와 해석 |
|---|---|
| 실제 프로젝트 Build | 4차 실제 등록 csproj 기준 Debug/Any CPU, 별도 OutDir·중간 경로 /t:Build PASS, 오류 0개·기존 다른 파일 경고 41개. Clean/Rebuild·소스 복제·운영 출력·배포 없음 |
| 정책/Config/Material geometry/진행 보존/context | 정책 30개, 최신 실제 Handler Config 12개·Material geometry 10개·진행 보존 30개·기존 검증점 상태 16개·기존 비생산 marker/context 15개·새 확인/provenance/JSON 44개 PASS. Common 26개와 합계 183개 |
| 실제 QMC.Common 로그 정책 | 26개 PASS. 별도 테스트 프로세스의 ProductionMinimal에서 Audit 영속 분류, 일반 로그 차이, Event CSV 왕복 확인. 설정 API/로그 writer/Handler/하드웨어 미실행 |
| 픽업 계측 불변 | 진단 예외 기록 보강 후에도 Audit helper/호출·문자열 보관만 제외하면 전체 소스가 HEAD와 동일함을 확인. 수식/분기/축 명령 불변 |
| 인코딩/diff/등록 | 이번 소스 7개의 BOM/CRLF/EOF 및 git diff --check PASS. 이번 신규 Compile 없음 |
| 실제 UI/지연 결과/STOP/저장 장애 | 전체 통합 미실시. 정적·오프라인 결과로 대체하지 않음 |
| 실장비·배포 | 축 이동/IO/카메라/운전·배포·운영 설정 저장 미수행 |

실행 도구: [정책](tests/run-policy-tests.ps1), [Config](tests/run-config-serialization-tests.ps1), [Material](tests/run-material-geometry-tests.ps1), [진행/context](tests/run-review-progress-tests.ps1), [CONFIRM context](tests/run-confirmation-tests.ps1), [로그 정책](tests/run-log-policy-tests.ps1).

이번 Material geometry 별도 helper 40회 평균은 1,257개 hash 3.487 ms/shape 0.360 ms, 1,645개 hash 4.306 ms/shape 0.415 ms였다. 한 PC의 개별 helper 측정이며 추가 로그·전체 잠금 경합·디스크 처리·장비 tact를 측정한 값이 아니다.

중간 Build의 외부 작업 9개→2개 오류는 해당 다른 작업이 직접 수정했고 이번 작업은 그 파일을 변경하지 않았으며, 마지막 4차 Build는 성공했다.

## 남은 확인 및 현장 한계

- 최신 소스의 격리 Build·183개 오프라인 검사는 완료했다. 현장 UI·STOP·지연 Vision·저장 장애 시험은 미실시다.
- CONFIRM은 데이터 일관성 확인이다. 모든 다이의 물리 대응, 실제 pitch/T/XY 정밀도 및 카메라 이후 픽업 쉬프트를 입증하지 않는다.
- legacy baseline 채택 이전의 누적 Offset은 복원하지 못한다. 이후 누적량과 과거 전체 누적량을 혼동하지 않는다.
- 부분 진행 wafer의 전체 승인 순서/revision 소실 또는 기존 좌표를 바꿔야 하는 경우는 이 변경으로 무조건 해결하지 않는다.
- Review와 픽업 Audit은 영속 정책에 포함되지만 비동기 logger의 전원 차단/디스크 실패까지 무조건 보존을 보장하지 않는다. 픽업 Audit은 계산 시마다 문자열/큐 기록 부하를 추가하며 실장비 성능은 미측정이다.
- 다른 작업의 recipe Material reset과 인터락 변경은 별도 소유 범위다. 이번 작업에서 적용·검증했다고 주장하지 않는다.
- 자세한 해석과 현장 분석 절차는 [04](04-review-findings-and-limits.md), [05](05-operation-and-log-guide.md)에 기록한다. 실장비 쉬프트가 해결됐다는 확답은 하지 않는다.

이전 임시 정책 테스트 EXE 정리가 자동 승인 검토에서 거부된 이력이 있다. 이번 문서 갱신에서는 삭제를 시도하지 않았으며 운영 데이터나 다른 작업자 파일을 정리하지 않는다.
