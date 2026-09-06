# InputStage 리뷰 좌표 보정 검증 — 검토 결과와 한계

작성일: 2026-09-06
기준 소스: D:/Source/CDT-320_New의 현재 로컬 파일
실장비 로그: D:/Source/EQP_Handler/20260905/핸들러로그
연결 문서: [구현 프롬프트](01-implementation-prompt.md), [검증 체크리스트](02-validation-checklist.md), [구현 현황](03-implementation-status.md)

## 판단 범위

이번 보강은 Review에서 변경한 맵의 기준·보정 이력·검증 증거가 현재 자재와 일치하는지 확인하는 작업이다. 과거 간헐 픽업 쉬프트의 단일 원인을 확정하거나 현재 장비의 정상화를 입증한 작업은 아니다. 확인한 로그·코드 동작과 아직 확인하지 못한 물리 현상을 구분한다.

사용자의 최우선 지시인 **기존 Auto 이동 순서·축 명령·인터락 조건 불변**을 유지한다. 현재 InputStageAlignSequence.cs, InputSequence.Steps.Review.cs 및 Equipment/Interlocks에는 git diff가 없다. 자동 Align의 피치 계산·설정값 대체·실패 분기도 변경하지 않는다. InputStageDieMapApplyService.cs의 이번 추가는 baseline/검증 메타데이터 4줄이다.

Material의 공통 승인 검사는 증거가 없거나 오래된 상태를 막을 수 있다. 따라서 기존 축 명령과 인터락이 같다는 사실이 모든 기존 저장 자료의 자동 재개까지 허용한다는 뜻은 아니다. 아래의 부분 진행 자재와 검증점 부족은 실제 재개 제약으로 남는다.

## 1. 로그가 입증한 차이와 입증하지 못한 내용

9월 4일 18:10~18:16의 RAD↔JMB 전환·Lot 완료/시작 이후 JMB 처리에서 다음 프로그램 좌표 차이가 확인되었다. 구체적인 계산 기준은 [01의 실제 근거](01-implementation-prompt.md)에 함께 기록했다.

| 항목 | 확인된 사실 | 해석의 한계 |
|---|---|---|
| JMB 설정 피치 | die 8.07/6.07 mm + gap 0.30/0.30 mm = step 8.37/6.37 mm | 9월 6일 첨부한 읽기 전용 JMB 화면의 gap 0.05 mm와 같은 시점·설정이라고 취급하면 안 됨 |
| 자동 Align 최종 X 측정 | 8.229730 mm, 이후 매핑은 설정 8.370000 mm 사용 | 차이 0.140270 mm/칸은 기록된 값의 차이이며, 측정값 자체가 정확한 물리 피치인지는 미확정 |
| 최종 두 Ref 측정 | 같은 행의 두 점이며 최종 격자 각도 +0.002455° | 같은 행 측정만으로 Y 피치가 독립 검증되었다고 할 수 없음. 중앙 Vision 각도와 같은 기준인지도 구분해야 함 |
| 수동 T 저장 뒤 맵 | corrected T 179.819090°, origin(379.991945, 9.735110), step(8.37, 6.37) | 물리 다이의 고유 index 대응까지 좌표만으로 입증하지 못함 |
| Map(33,19) 최초 좌표와 첫 예약 | (656.201945, 130.765110) → (664.781275, 131.101475) | 두 좌표 차이 (+8.579330, +0.336365) mm는 프로그램 누적 평행이동. 실제 픽업 오차 크기라는 의미가 아님 |
| 두 APPLY | 18:28:06, 18:28:32. 개별 X 이동은 약 +8.218 mm, 약 +0.361 mm | 잘못된 최초 맵을 올바르게 보정했는지, 이웃 다이를 같은 index로 잘못 대응했는지는 로그만으로 확정 불가 |

로그 수치의 직접 근거는 [2026-09-04_014.csv](D:/Source/EQP_Handler/20260905/핸들러로그/2026-09-04_014.csv)의 18:25~18:28 기록이다. 레시피 모델명·사이즈가 비슷하다는 사실만으로 적용 원점·T·피치·카메라 보정·Material 기준이 같다고 보지는 않는다.

## 2. 이번 검토에서 보강한 판정과 저장 처리

아래는 신규 Review 검증을 추가하면서 발견한 우회 또는 정상 작업 차단 가능성이다. 과거 장비 로그의 확정 원인이라는 의미는 아니다. 최종 테스트·Build 결과는 [03](03-implementation-status.md)에서 관리한다.

| 조건/발견 | 보강한 내용 | 확인 범위 |
|---|---|---|
| 임의 변경 좌표를 직접 등록하면 단발 한계 검사를 건너뛸 수 있음 | Material이 공통 정책을 통과한 평행이동 후보 이력을 관리. 현재 원본 또는 허용된 마지막 후보만 검증 등록 가능 | [TryRecordInputStageReviewOffset](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs) 및 Commit 공통 검사 구현. 전체 UI 경합 통합 시험은 별도 |
| APPLY·재시도·창 재열기마다 누적 기준을 바꾸면 누적량을 우회할 수 있음 | Wafer/mapping revision에 baseline 저장. 수동 평행이동에서 baseline 유지. 저장된 revision 불일치는 자동 기준 재설정으로 통과시키지 않음 | 새 모델 필드·Snapshot 복제·공통 Context 검사 구현 |
| 카메라 전체 설정을 해시하면 촬영 때마다 바뀌는 ResolutionUpdatedAt 때문에 정상 증거도 만료됨 | 해시에는 실제 영상 크기·중심·PixelToMm 값만 포함. 시각·출처 메타데이터 제외 | [카메라 geometry 서명](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs) 구현 |
| 저장된 T 메타데이터 동일성 공차 0.000001°를 실제 엔코더에도 적용하면 정상 정지에서도 거부될 수 있음 | 실제 T는 기존 StageT InPositionTolerance로 비교. 저장 T 두 값의 비교와 분리하고 정지·도착·유한 좌표를 확인 | 공통 Context/검증/Commit 검사 구현. 실제 장비 오차 분포는 미측정 |
| 저장 대기 중 승인 소비가 false로 캐시되면 저장 완료 뒤에도 재개가 막힐 수 있음 | 저장 완료 시 관련 픽업 캐시 무효화. 저장 완료 뒤 같은 Wafer 객체·검증 ID·세션/요청인지 확인 | [저장 완료 검사](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs) 보강. 저장 실패 주입의 전체 통합 시험은 미실행 |
| UI의 확인 bool 또는 오래된 저장 승인만 사용하면 현재 후보와 증거가 달라도 진행할 수 있음 | 원시 측정/Context/허용치 저장·복원 재검증, Commit과 승인 소비에서 현재 상태 비교 | 승인받은 OutputReceive 공통 gate 연결까지 실제 프로젝트에 적용 |
| DryRun/Simulation의 측정 또는 가상 이동을 생산 검증으로 재사용할 수 있음 | 현재 모드·축 상태를 Context 조건에 포함하고 생산 증거와 분리 | 순수 정책 검사 통과. 실제 앱 모드 전환 시험은 별도 |

단발 X/Y와 누적 X/Y 기본값은 각각 20 mm이고 기존 저장된 더 큰 값은 보존한다. 반 피치 상한은 추가하지 않았다. OFFSET 허용 범위를 넓게 설정해도 XY 잔차·피치·격자 각도의 품질 허용치는 별도로 적용한다. 운영 설정을 자동 수정하지 않았다.

## 3. 거의 일직선인 3점의 수치 불안정

서로 다른 행·열의 점 3개라고 해서 항상 안정적인 2차원 피치/각도 계산이 되는 것은 아니다. 예를 들어 grid(0,0), (31,30), (32,31)은 격자 행렬식이 1이지만 거의 일직선이다. 작은 측정 교란이 X/Y 격자 성분을 분리하는 과정에서 커져 실제 T/pitch 불량처럼 보일 수 있다.

Review 공통 정책은 예상 물리 좌표 삼각형의 **최소 높이/최장 변 비율이 0.1 이상**인 조합을 요구한다. UI는 이동 전에 같은 정책을 호출하여 서로 떨어진 삼각형으로 재선택하도록 안내한다. 이 값은 검증점 배치의 형상 기준이다. 장비의 생산 정확도, 허용 OFFSET 또는 실제 카메라 노이즈를 의미하지 않는다.

[CheckVerificationPointLayout](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/InputStageReviewGeometryPolicy.cs)은 거의 일직선인 점을 점 배치 부적합으로 처리한다. 테스트는 위 격자에 0.001 mm 가상 교란을 주는 재현과 정상 삼각형의 회전·축척·반전·선택 순서를 포함한다. 이 교란값을 실장비에서 측정한 노이즈라고 해석하지 않는다. 기존 Auto Align의 검증점이나 자동 이동은 변경하지 않았다.

이 배치 검사는 거의 일직선인 계산을 피할 뿐이다. 작은 삼각형의 국소 검증으로 전체 wafer의 모든 위치가 정상이라고 입증하지는 않는다. 실제 확인에서는 유효한 다이 중 X/Y 방향으로 충분히 떨어진 점을 선택하고, 먼 지점과 첫 픽업 결과도 별도로 확인해야 한다.

## 4. 부분 진행 Wafer의 재승인 제한

기존 Commit은 이미 픽업됐거나 예약됐거나 InputStage를 벗어난 다이가 제출 목록에 포함되면 변경을 거부한다. UI가 전체 맵 상태를 제출하므로, 과거 데이터에 검증 증거만 추가하려는 경우에도 부분 진행 자재가 막힐 수 있음을 확인했다.

현재는 원점·전체 정규화 Die 상태/Bin/좌표·남은 승인 순서가 현재 자료와 완전히 같은 경우 **검증 증거만 추가하는 경로**를 적용했고 진행 보존 전용 검사 30개를 통과했다. 기존 전체 승인 UID 목록·mapping revision·시작점 기록은 보존하며, 픽업된 다이의 위치나 보유 이력을 초기화하지 않는다. 근거는 [CheckInputStageReviewProgressPreservation](D:/Source/CDT-320_New/QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs)이다.

이 경로에는 다음 한계가 있다.

- 기존 전체 승인 UID 순서와 해당 revision이 남아 있어야 한다. 기존 Auto Review의 STOP/Retry/Auto 복귀 실패 처리에서 SetInputStageRunReviewApproval(false)가 이를 지우면, 뒤의 Review만으로 이전 순서를 입증할 수 없다. 이 경우 자동 보호를 우회하여 재승인을 허용했다고 기록하지 않는다.
- 현재 실제 촬영할 수 있는 다이가 3개 미만이거나 한 줄에만 남았다면 새 2차원 검증을 완료할 수 없다. 이미 픽업된 다이를 촬영 가능하다고 가정하거나 점 수를 줄여 합격시키지 않는다.
- 일부 진행 자재에서 좌표·상태·픽업 순서를 바꾸는 변경은 이 증거 추가 경로의 대상이 아니다. 기존 좌표가 틀려 수정해야 하는 경우를 해결하지 못하며, 모든 구형 자재가 재시작 없이 복구된다는 보장은 없다.

기존 자동 시퀀스/인터락 불변 조건 아래에서 위 제약을 숨기지 않는다. 실제 현장 재개 절차와 별도 보완 필요 여부는 자재 진행 상태와 보존된 승인 정보를 확인한 뒤 판단해야 한다.

## 5. UI/STOP 동시 실행과 영상·다이 대응의 한계

APPLY/CONTINUE 의사결정 잠금과 STOP 처리를 보강했다. 제한 시간 내 실제 작업 callback 종료를 확인한 뒤 기존 JogStop, 정지 확인, embedded scope 해제 순서로 처리한다. 시간 초과/실패에는 lease와 UI 잠금을 보존한다. 취소 token 전달을 실제 작업 종료로 간주하지 않는다. 코드/정적 확인과 실제 장비 STOP·지연 응답 통합 시험은 구분하며 기존 Auto 이동/인터락 조건은 수정하지 않는다.

검증점에서는 MATCH 측정 이후 같은 정지 X/Y/T를 확인하면서 별도의 새 EXPOSE 영상을 표시한다. 과거 overlay와 누적 평균 영상이 현재 점 확인을 혼동하지 않도록 처리한다. 다만 다음은 현재 보증할 수 없다.

1. **MATCH 결과와 표시 EXPOSE가 동일 프레임인지:** [VisionFrameMeta.cs](D:/Source/CDT-320_New/QMC.Common/Ui/Vision/VisionFrameMeta.cs)의 현재 자료에는 MATCH 요청과 영상 프레임을 직접 연결하는 요청/프레임 식별 증거가 없다. [표시 receipt](D:/Source/CDT-320_New/QMC.CDT-320/Ui/Controls/VisionViewerPanel.cs)는 앱이 생성한 표시 기록이며 장비가 보낸 공통 프레임 ID가 아니다.
2. **선택 map UID와 동일 물리 다이인지:** 반복 패턴의 MATCH 성공이나 작은 잔차만으로 고유 index를 식별하지 못한다. 3점이 모두 같은 방향의 이웃 다이로 대응되어도 평행이동·피치·각도는 일관될 수 있다. 식별 가능한 맵 패턴·기준점 등 근거를 보고 작업자가 확인해야 하며 자동 고유 UID 검출이라고 표현하지 않는다.
3. **인풋 검증 후 실제 픽업까지 정상인지:** 맵 검증은 카메라→픽커 변환, 픽업 중 runtime learned XYT, 콜렛 편차·drift를 검증하지 않는다. 이 값들의 수정·초기화·교정은 이번 범위 밖이다.

따라서 새 검증이 있다는 이유만으로 물리 대응 문제나 픽업 보정 문제를 모두 방지한다고 주장하지 않는다.

## 6. 비생산 수동 Review의 별도 승인

Simulation에서 SkipRunReview=false이거나 DryRun/Hybrid로 수동 Review를 사용하는 경우, 새 geometryVerified 필수 조건이 실제 측정을 요구하여 기존 비생산 진행을 막는 회귀가 확인되었다. 생산 검증 증거를 요구할 수 없는 모드에 같은 UI 조건을 적용한 문제다.

별도 NONPRODUCTION-MANUAL token/context 경로로 보강했다. UI는 NONPRODUCTION REVIEW와 실측 검증이 아님을 표시한다. 명시적인 현재 settings/recipe/stage/axis 모드를 확인하며, null 상태만으로 승인하지 않는다. 가짜 Measurements를 만들지 않고 현재 모드·wafer/map·session/request·조건이 같을 때만 소비한다. 기존 저장 결과 모드 검사도 유지하여 생산용 검증으로 재사용하지 못한다. 기존 AutoSkip 및 자동 이동·분기·인터락은 변경하지 않았다.

비생산 marker/context 15개 오프라인 검사는 통과했다. 이는 실제 UI·runtime 모드 조회 또는 장비 모드 전환의 통합 시험이 아니며 해당 시험은 미실시다.

## 7. 오프라인 검증과 성능 측정의 의미

정책 30개·Config 직렬화 12개·Material geometry 10개·진행 보존 30개·검증점 자재 상태 16개·비생산 marker/context 15개, 총 113개 오프라인 검사가 통과했다. 마지막 61개는 backend helper 검사이며 실제 모드 조회·UI/장비 통합 시험이 아니다. request generation의 long 타입 컴파일 수정과 마지막 소비 검사 반영을 포함한 최종 실제 프로젝트 Build는 오류 0개·기존 경고 41개로 통과했다. 소스/테스트 인코딩·diff 및 수정 소스의 assembly 반영 확인도 완료했으며 근거는 [03](03-implementation-status.md)에 연결했다.

Material geometry 테스트의 PC 단일 helper 측정은 아래와 같다.

| 좌표 수 | geometry hash | shape 검사 |
|---|---:|---:|
| 1,257 | 3.202 ms | 0.337 ms |
| 1,645 | 4.315 ms | 0.417 ms |

이는 한 PC의 단일 실행에서 개별 helper를 측정한 값이다. 전체 승인 경로, 반복 캐시 검사, 잠금 경합, 디스크 저장, UI/Vision 지연 또는 장비 사이클 시간을 측정하지 않았다. 실장비 처리 시간에 영향이 없다는 결론으로 확대하지 않는다.

실제 장비 STOP·재시작·지연 응답·디스크 장애·모드/UI 전환의 통합 시험은 미실시다. 실장비 검증·배포·운영 설정 저장·축 이동·IO·촬영은 수행하지 않았다. 현장에서는 레시피 전환 여부, 동일 Material 재사용 여부, 프로그램 재실행 여부, 기준 맵·T·피치 및 보정 이력을 구분하여 정상/이상 사례를 비교해야 한다. 첫 픽업의 인풋 측정과 각 콜렛의 실제 픽업/Bottom 결과를 함께 확인해야 최종 현상을 판단할 수 있다.
