# InputStage 리뷰 좌표 보정 검증 — 구현 현황

작성일: 2026-09-06
기준 저장소: D:/Source/CDT-320_New
확인 브랜치: master, origin/master보다 92개 커밋 앞섬
확인 HEAD: 7961f2ba772ea90fc6b08035b0a1604784e3a4c8
연결 문서: [구현 프롬프트](01-implementation-prompt.md), [검증 체크리스트](02-validation-checklist.md), [검토 결과와 한계](04-review-findings-and-limits.md)

## 현재 상태와 최우선 범위

사용자가 중복 파일 2건을 승인하여 신규 소스 3개의 실제 프로젝트 등록과 OutputReceive 공통 승인 검사 연결을 적용했다. 등록된 프로젝트 기준 격리 Build까지 통과했다. UI 결정 잠금·STOP/수동 동작 종료·구형 부분 진행 Wafer 재검증 및 비생산 수동 Review 보강까지 소스 구현을 마쳤다. 오프라인 113개 검사와 마지막 소비 검사까지 포함한 최종 실제 프로젝트 Build가 통과했다(오류 0개·기존 경고 41개). **장비 배포·실장비 검증은 수행하지 않았다.**

사용자 최우선 조건은 **기존 Auto 이동 순서·축 명령·인터락 조건 불변**이다. 현재 InputStageAlignSequence.cs, InputSequence.Steps.Review.cs 및 Equipment/Interlocks의 git diff는 없다. 자동 Align의 피치 계산·설정값 fallback·실패 분기를 변경하지 않는다. 피치 검증 보강은 Review의 다점 측정과 Material 승인 검사에 한정한다.

InputStageDieMapApplyService.cs에는 새 맵 baseline/검증 메타데이터 4줄만 추가되어 있다. 이 훅은 기존 모션 명령이나 이동 순서를 바꾸지 않는다. 다만 현재 자료와 검증 증거가 맞지 않거나 증거가 없는 경우 Material 승인 검사가 자동 진행을 막고 재검증을 요구할 수 있다. 자동 동작 경로의 불변과 승인 가능 여부의 강화는 구분한다.

실장비 분석 기준은 D:/Source/EQP_Handler/20260905/핸들러로그이며, 핵심 시간대는 9월 4일 18:10~18:16 RAD↔JMB 전환과 그 이후다. 간헐 쉬프트의 단일 근본 원인은 확정하지 않았다. 큰 OFFSET 자체를 불량이라고 판정하거나 이 보강으로 모든 픽업 쉬프트가 해소된다고 주장하지 않는다.

## 이번 작업의 파일 범위

전체 작업 트리의 다른 dirty 파일을 이번 작업 결과로 포함하지 않는다. 아래는 이번 변경의 파일과 목적이며, Auto 보존 확인 대상은 별도 표로 구분한다.

| 파일 | 이번 변경의 목적 |
|---|---|
| [IStageInterfaces.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/IStageInterfaces.cs) | UserConfirmResult의 세션·요청 세대와 검증 token 전달 |
| [MaterialModel.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialModel.cs) | Wafer의 baseline/revision 및 원시 검증 증거 추가 필드 |
| [MaterialSnapshotStore.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialSnapshotStore.cs) | 신규 필드와 검증 자료의 독립 복제·저장 연결 |
| [MaterialStateService.InputPick.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputPick.cs) | Review Commit·승인 소비·저장 완료 조건 확인; 구형 부분 진행 자료의 변경 없는 증거 추가 경로와 비생산 승인 연결 |
| [InputStageUnit.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Unit/InputStageUnit.cs) | 누적 OFFSET X/Y 설정과 구형 설정의 누락 기본값 |
| [InputStageRecipePage.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Pages/Recipe/InputStageRecipePage.cs) | ParameterGrid의 누적 X/Y 한계 연결 |
| [Form1.InputStageRunReview.cs](D:/Source/CDT-320_New/QMC.CDT-320/Form1.InputStageRunReview.cs) | 검출/APPLY/확인 요청 수명, Jog·T·STOP·종료 후 결과 무효화 및 수동 동작 종료 확인 |
| [InputStageDieMapApplyService.cs](D:/Source/CDT-320_New/QMC.CDT-320/Sequencing/InputStage/InputStageDieMapApplyService.cs) | 새 맵의 baseline/검증 메타데이터 4줄; 수동 평행이동은 기존 누적 기준 보존 |
| [InputStageRunReviewDialog.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.cs) | 별도 좌표 검증 상태·VERIFY MAP 연결·확정 처리 중 UI 잠금 |
| [InputStageRunReviewDialog.Designer.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.Designer.cs) | VERIFY MAP 버튼 선언·배치·이벤트 연결 |
| [VisionViewerPanel.cs](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Controls/VisionViewerPanel.cs) | 정지 좌표의 별도 EXPOSE 표시, 수신 중 조건 변경 확인 및 과거 overlay 제거 |
| [CameraViewBase.cs](D:/Source/CDT-320_New/QMC.Common/Ui/Vision/CameraViewBase.cs) | 검증점 영상 표시 전 누적 평균 버퍼 초기화 |
| [QMC.CDT-320.csproj](D:/Source/CDT-320_New/QMC.CDT-320/QMC.CDT-320.csproj) | 승인받은 신규 Compile 3개만 등록; 다른 채팅의 등록 항목 보존 |
| [MaterialStateService.OutputReceive.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.OutputReceive.cs) | 승인받은 TryBuildApprovedInputStagePickOrder의 공통 geometry 검사 연결 2줄; 다른 채팅의 Place 완료 처리 보존 |

| 기존 Auto 보존 확인 대상 | 확인 상태 |
|---|---|
| InputStageAlignSequence.cs | 이번 작업 git diff 없음. 기존 자동 Align 피치 계산·fallback·분기 유지 |
| InputSequence.Steps.Review.cs | 이번 작업 git diff 없음. 기존 자동 Review 전환·이동·취소 흐름 유지 |
| Equipment/Interlocks | 이번 작업 git diff 없음. 인터락 조건 변경 없음 |
| Picker Pick/Place 좌표식·learned XYT·콜렛 교정 | 이번 작업의 수정 범위 밖. 같은 저장소의 다른 채팅 변경과 구분 |

## 신규 소스 및 실제 등록

| 파일 | 목적 |
|---|---|
| [InputStageReviewGeometryPolicy.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/InputStageReviewGeometryPolicy.cs) | 단발/누적 후보 계산, Context 비교, 점 배치·잔차·격자 pitch/각도 검사 |
| [MaterialStateService.InputStageReviewGeometry.cs](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs) | baseline/조건 해시, 유효한 OFFSET 후보 이력, 원시 증거 등록 및 승인/저장 완료 검사 |
| [Form1.InputStageRunReview.Geometry.cs](D:/Source/CDT-320_New/QMC.CDT-320/Form1.InputStageRunReview.Geometry.cs) | 작업자가 요청한 Review 3점 확인, 기존 안전 이동 호출, 측정·대응 확인과 검증 token 연결 |

위 3개는 실제 QMC.CDT-320.csproj의 Compile 항목에 등록되어 있다. Form1 partial은 DependentUpon=Form1.cs를 유지한다. 임시 targets 등록만으로 빌드하던 상태는 현재 상태가 아니다.

## 사용자 승인 후 적용한 중복 범위

| 승인 대상 | 적용 내용과 보존 범위 |
|---|---|
| QMC.CDT-320.csproj | 위 신규 Compile 3개 등록 완료. 다른 채팅의 PickerFlowRecovery·EventLog 관련 Compile 항목 보존 |
| MaterialStateService.OutputReceive.cs / TryBuildApprovedInputStagePickOrder | 기존 승인/null 확인 다음에 IsInputStageReviewGeometryApprovalUsableNoLock 호출과 실패 반환 2줄 적용 완료. MoveDieToOutputStage의 다른 채팅 ClearPickerFlowRecoveryNoLock 처리는 보존 |

이 두 건의 승인 대기는 해소되었다. 추가로 다른 채팅과 겹치는 수정이 생기면 기존 사용자 지시에 따라 그 구체적인 범위를 다시 승인받는다. 운영 데이터·실장비 로그 변경, 소스 복붙·전체 덮어쓰기 및 다른 채팅 변경 제거는 수행하지 않는다.

## Review 동작과 판정 기준

1. 단발 X/Y와 누적 X/Y는 독립 설정이며 기본값은 각각 20 mm이다. 저장되어 있는 더 큰 값은 보존한다. Review에 반 피치 상한을 새로 적용하지 않았고 운영 Config/Recipe는 자동 변경하지 않았다.
2. 누적량은 후보 원점에서 wafer/mapping baseline 원점을 뺀 값이다. APPLY·검출 재시도·선택 변경·재열기로 baseline을 새로 만들지 않는다. 구형 자료의 누락은 변경하지 않은 현재 맵의 다점 검증 또는 새 매핑으로 처리한다.
3. APPLY는 단발/누적 정책을 통과한 평행이동 후보를 등록하고 좌표 검증 상태를 해제한다. 출처 없는 임의 좌표를 직접 제출해 단발 제한을 우회할 수 없도록 Material에서 후보 이력을 비교한다.
4. 하단 표에서 기준 다이 3개를 선택한다. 예상 좌표 삼각형의 최소 높이/최장변이 0.1 이상인지 이동 전에 확인하고 공통 정책에서도 검사한다. 이 비율은 거의 일직선인 점의 불안정한 계산을 피하는 점 배치 기준이며 OFFSET/정밀도 공차가 아니다.
5. 기존 Review 안전 이동 경로로 후보 예상 위치에 이동하고 정지·도착을 확인한다. 원래 예상 좌표와 촬영 XYT/원시 Vision 값으로 잔차를 계산하며 각 점을 개별 중심으로 재이동해 잔차를 0으로 만들지 않는다.
6. 같은 정지 XYT에서 별도 EXPOSE 영상을 표시하고 작업자가 실제 다이와 map index의 대응을 확인한다. MATCH와 동일 프레임이라는 wire 수준 연결 증거 또는 Vision이 고유한 물리 UID를 읽었다는 보장은 없다.
7. 품질 기준은 현재 AlignCenterToleranceMm, AlignPitchCompareToleranceMm, MaxEffectiveThetaToleranceDeg이다. 실제 촬영 T는 축 InPositionTolerance로 비교하고 저장 Mapping T 두 값의 동일성 공차 0.000001°와 구분한다. 자동 Align의 기존 처리 분기는 유지한다.
8. 세션/요청/wafer/revision/조건/후보 및 원시 측정 증거를 Material Commit과 승인 소비에서 다시 확인한다. 촬영마다 바뀌는 ResolutionUpdatedAt은 조건 해시에서 제외하고 실제 해상도·중심·변환 배율을 비교한다.
9. 저장 완료 전에는 승인 소비를 막고 저장 완료 뒤 같은 Wafer·검증 ID·요청·조건인지 다시 확인한다. 저장 대기 중 false로 생성된 픽업 캐시도 저장 완료 시 무효화한다. 저장 실패·STOP·동시 확정 경합에서 이전 작업이 더 새 증거의 승인을 해제하지 않도록 한다.
10. 구형 부분 진행 Wafer의 좌표/상태/남은 순서가 완전히 같은 경우에 증거만 추가하는 경로와 전용 검사 30개를 완료했다. 기존 전체 승인 UID 목록/revision 소실, 실제 촬영 가능한 비공선 3점 부족, 기존 오좌표를 변경해야 하는 경우는 이 경로로 해결하지 못한다.
11. APPLY/CONTINUE 결정 잠금과 STOP 처리를 보강했다. 제한 시간 내 callback 종료 확인 → 기존 JogStop → 정지 확인 → embedded scope 해제 순서이며 시간 초과/실패에는 lease와 UI 잠금을 보존한다. request generation의 long 타입 컴파일 수정도 완료했다.
12. 비생산 수동 Review는 별도 NONPRODUCTION-MANUAL token/context를 저장하고 UI에 NONPRODUCTION REVIEW 및 실측 검증이 아님을 표시한다. 명시적 settings/recipe/stage/axis 현재 모드가 필요하고 null을 승인 근거로 삼지 않는다. 가짜 측정 자료는 생성하지 않는다. 현재 모드·조건 및 기존 저장 결과 모드 검사를 유지하여 생산에 재사용하지 않는다. 기존 AutoSkip 분기는 그대로다.

## 검증 현황

총 113개 오프라인 검사와 최종 실제 프로젝트 Build가 통과했다. 실제 UI/장비 통합 시험은 수행하지 않았다. 최종 기계 판독 기록: [final-offline-verification.json](D:/Source/CDT-320_New/_codex_verify_inputstage_review/final-offline-verification.json).

| 항목 | 현재까지의 결과와 해석 |
|---|---|
| 순수 정책 | 30개 시나리오 PASS. 큰 OFFSET, 누적 경계, NaN/overflow, stale Context, 원시 증거 직렬화, 점 배치, 회전·확대·반전·선택 순서 및 Simulation 분리. 장비 assembly/하드웨어 미사용 |
| 실제 Config 직렬화 | 12개 검사 PASS. 누락 누적값의 기본 20 mm, 축별 독립 값, 300/400 mm 유지, JSON 왕복 및 명시적 0/음수 보존 확인. 운영 설정 미수정 |
| Material geometry | 10개 검사 PASS. 현재 assembly의 geometry/condition 관련 검사이며 실제 자동 사이클·하드웨어 검증이 아님 |
| 최종 Build | 실제 등록 프로젝트의 격리 /t:Build 성공, 오류 0개·기존 다른 파일 경고 41개. 모든 수정 소스가 최종 Handler assembly보다 이전이며 미반영 소스 없음 |
| backend helper | 진행 보존 30개·검증점 자재 상태 16개·비생산 marker/context 15개, 61개 PASS. 위 정책/Config/geometry 52개와 합계 113개. 실제 모드 조회·UI/runtime 통합은 미실시 |
| 비생산 수동 Review | 별도 모드·token/context·UI 표시 구현 완료. 가짜 실측 없음, 생산 재사용 거부, 기존 AutoSkip·저장 결과 모드 검사 유지. marker/context 15개 PASS와 실제 모드/UI 통합 미실시를 구분 |
| Auto 보존 점검 | InputStageAlignSequence.cs, InputSequence.Steps.Review.cs, InputPickerPickTargetResolver.cs, Equipment/Interlocks 및 QMC.Common/Motion의 git diff 없음. DieMapApplyService는 메타데이터 4줄 |
| 최종 diff/인코딩/등록 | 소스 17개·테스트 7개 BOM/CRLF/EOF PASS, 변경 범위 diff --check PASS. 원래 있던 trailing whitespace 3개 파일은 HEAD와 확인하여 보존. 신규 Compile 3개와 다른 작업 Compile 3개는 각각 1회 등록 |
| UI/취소/저장 실패·부분 진행 재개 | 코드 보강·정적 확인 완료. 실제 장비 STOP·재시작·지연 응답·디스크 장애·화면/모드 전환의 전체 통합 시험은 미실시 |
| 실제 장비 | 이동·IO·카메라 연결·운전·배포 미수행. 허용치 적합성, 물리 대응, 첫 픽업/Bottom 정확도 확인 필요 |

실행 도구: [정책 검사](tests/run-policy-tests.ps1), [Config 직렬화 검사](tests/run-config-serialization-tests.ps1), [Material geometry 검사](tests/run-material-geometry-tests.ps1), [진행 보존/검증점/비생산 검사](tests/run-review-progress-tests.ps1).

Material geometry의 PC 단일 helper 측정은 단일 실행에서 1,257개 좌표는 hash 3.202 ms / shape 0.337 ms, 1,645개는 hash 4.315 ms / shape 0.417 ms였다. 전체 승인 검사·잠금 경합·디스크 저장·UI/Vision·장비 사이클 시간을 측정한 수치가 아니며 실장비 성능 보증으로 사용할 수 없다.

## 남은 확인 및 현장 한계

- 소스 구현·최종 격리 Build·113개 오프라인 검사·대상 diff/인코딩 확인을 마쳤다. 이후 코드를 다시 변경하면 해당 영향에 대한 검증이 새로 필요하다.
- UI·지연 Vision·STOP/취소·저장 실패·동일 조건 재개 시험은 수행 범위와 미실행 범위를 따로 기록한다.
- 부분 진행 Wafer에서 기존 승인 전체 UID 순서/revision이 소실됐거나 실제 촬영 가능한 비공선 3점이 부족한 경우의 재승인 한계가 남는다. 기존 Auto 보호를 우회하여 이 문제를 해결했다고 판정하지 않는다.
- 실장비에서는 RAD↔JMB 전환, 동일 Material 재사용, 프로그램 재시작, 큰 OFFSET이 필요한 정상 사례를 구분하여 검증한다. 실제 다이의 대응 근거와 첫 픽업/Bottom 결과를 함께 확인한다.
- Review 검증 통과는 runtime learned XYT, 카메라→픽커 변환, 콜렛 drift의 정상 증거가 아니다. 해당 수정·교정은 이번 범위 밖이다.
- 비생산 수동 Review는 별도 승인으로 보강했으나 실제 UI·현재 모드 조회·운전 모드 전환 통합 시험은 미실시다. 생산 증거로 승격하거나 기존 Auto 분기를 바꿔 우회하지 않는다.
- 상세 발견 사항 및 제한은 [04 문서](04-review-findings-and-limits.md)에 기록한다. 현재 실제 장비가 정상화되었다는 결론은 내리지 않는다.

이전 임시 정책 테스트 EXE 정리는 자동 승인 검토에서 거부되어 산출물을 보존했다. 이번 문서 갱신에서는 추가 삭제를 시도하지 않았으며 운영 데이터나 다른 작업자의 파일을 정리 대상으로 삼지 않았다.
