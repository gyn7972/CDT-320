# InputStage 리뷰 좌표 보정 검증 — 구현 프롬프트

작성일: 2026-09-06
현재 단계: 사용자 요청에 따라 필수 3점 선택·VERIFY MAP·점별 수동 확인 절차를 제거하고 기존 CONFIRM에서 현재 자료의 일관성을 확인하도록 수정했다. 최신 수정본의 오프라인 183개 검사와 4차 실제 등록 프로젝트 격리 Build(오류 0개·기존 경고 41개)가 통과했다. 앞 단계의 오프라인 113개·Build 오류 0개/기존 경고 41개는 이전 구현의 이력이며 이번 수정본의 최종 결과로 사용하지 않는다.
연결 문서: [검증 체크리스트](input-stage-review-offset__02-validation-checklist.md), [구현 현황](input-stage-review-offset__03-implementation-status.md), [검토 결과와 한계](04-review-findings-and-limits.md), [사용법과 로그 분석](05-operation-and-log-guide.md)

## 1. 수행할 작업과 범위

기존 Auto Review의 시작점·픽업 순서 확인과 CONFIRM / CONTINUE AUTO 조작을 유지한다. 추가했던 기준 다이 3개 선택, VERIFY MAP, 각 점으로 이동·촬영하여 작업자에게 대응을 묻는 절차를 필수 경로에서 제거한다. 기존 DIE DETECTION / APPLY OFFSET은 필요한 경우 사용하는 수동 기능으로 유지한다.

CONFIRM에서는 현재 wafer·mapping·recipe·T·pitch·후보 좌표와 세션/요청 자료의 일관성을 확인하고 승인 자료를 저장한다. 이는 물리 실측 검증이 아니다. 가짜 측정값이나 GEOMETRY VERIFIED 표시로 대체하지 않는다.

기존 Auto 이동 순서·축 명령·인터락 조건·픽업 수식은 변경하지 않는다. PickerPickUpSequence.PickTargets.cs에는 이미 계산한 메시지를 재사용하는 Audit 로그만 추가한다. 자동 Align 피치 계산·fallback·실패 분기와 자동 Review 전환 및 Simulation AutoSkip 조건은 그대로다.

오래된 pending, 유효하지 않은 후보, 저장 실패를 막는 보호와 넓게 조정 가능한 단발·누적 OFFSET 제한은 유지한다. 실제 좌표가 틀려도 내부 자료가 일관될 수 있으므로 이 보강으로 간헐 픽업 쉬프트가 모두 해결된다고 단정하지 않는다.

## 2. 기준 자료와 동시 작업 규칙

- 소스 기준: D:/Source/CDT-320_New. 저장소의 AGENTS.md를 따른다.
- 현재 확인 브랜치: master, origin/master보다 93개 커밋 앞섬.
- 현재 확인 HEAD: 478ffef077ff0b95bfa64e796e3970e4810b093d.
- 이전 단계 기준 HEAD 7961f2ba772ea90fc6b08035b0a1604784e3a4c8 및 당시 master ahead 92는 이전 구현 이력이다.
- 현재 다른 작업의 AppSettings.cs, Recipes/RecipeStore.cs, csproj, README.md, RecipeMaterialStateFactory.cs 및 docs/recipe-change-material-reset은 수정 대상에서 제외한다.
- 이전 단계의 신규 Compile 3개와 OutputReceive 승인 검사 연결 2줄은 당시 사용자 승인 후 적용된 기존 구현이다. 이번에는 이 파일들을 수정하지 않는다.
- 실장비 증거: D:/Source/EQP_Handler/20260905/핸들러로그
- 주요 파일: 2026-09-04_012.csv(RAD), 2026-09-04_014.csv(18:10~18:16 전환과 이후 JMB).
- 동봉 CDT-320_DATA 설정은 사건 전 스냅샷이다. 사건 당시 적용값은 이벤트 로그를 우선한다.
- 첨부된 9월 6일 JMB 읽기 전용 화면을 9월 4일 JMB_Rework 사건 상태로 취급하지 않는다.
- 다른 채팅도 같은 저장소를 수정한다. 브랜치 전환, reset, restore, checkout으로 되돌리기, revert, clean, 타 버전 파일 덮어쓰기, 소스 복사·복붙을 하지 않는다.
- 다른 폴더·worktree·과거 커밋의 코드를 가져오지 않는다. 최신 로컬 파일의 필요한 함수만 문맥을 확인하여 최소 변경한다.
- 매 작업 묶음과 파일 수정 직전에 git 루트·브랜치·HEAD·status·대상 diff를 재확인한다. 오래전에 읽은 파일 전체를 다시 써서 다른 변경을 소실시키지 않는다.
- 다른 채팅의 작업과 수정 대상 파일·함수 또는 공유 상태/프로젝트 등록이 겹치면, 기술적으로 합칠 수 있어도 해당 수정은 사용자 승인 전까지 수행하지 않는다. 겹치는 경로·함수, 상대 작업 내용, 제안 변경과 영향 범위를 구체적으로 보여주고 승인을 받는다.
- 겹치지 않는 분석·문서 작업은 계속한다. 승인 범위를 넘는 새 중복이 생기면 그 부분은 다시 승인받는다. 외부 변경을 되돌리거나 복붙하지 않는다.
- 운영 D:/CDT-320 및 실장비 로그·레시피·보정 파일에 쓰지 않는다. 장비 이동, IO, 카메라 연결, 자동 운전, 배포는 수행하지 않는다.
- 이번 작업에서 픽커 Flow 복구, 맵 색상·레이아웃, Pick/Place 좌표식, 학습값 전체 초기화, 콜렛 캘리브레이션, Lot 수명 관리를 변경하지 않는다.

## 3. 이번 설계의 실제 근거

1. JMB_Rework는 die 8.07/6.07 mm, gap 0.30/0.30 mm, step 8.37/6.37 mm였다.
2. JMB 자동 얼라인은 측정 X pitch 8.229730을 기록하고, 매핑에서는 설정 8.370000을 사용했다. 차이는 0.140270 mm/칸이다. 측정값이 실제 피치라고 단정하지 않는다.
3. 자동 얼라인의 최종 Ref 각도는 +0.002455°, 중앙 Vision 각도는 -0.604°였다. 서로 같은 기준의 각도인지 확인하기 전 단순 차이를 고장으로 확정하지 않는다.
4. 수동 T 저장 후 corrected T는 179.819090°가 되었고 재매핑이 수행되었다.
5. 재매핑의 origin은 (379.991945, 9.735110), step은 (8.37, 6.37)이다. Map(33,19)의 좌표는 (656.201945, 130.765110)이다.
6. 18:28:06과 18:28:32에 APPLY OFFSET을 실행했고, 첫 예약 좌표는 (664.781275, 131.101475)였다.
7. 위 두 좌표로 계산한 누적 이동량은 (+8.579330, +0.336365) mm이다. 개별 적용량은 중간 로그의 표시 정밀도 때문에 약 +8.218 mm, 약 +0.361 mm로 기술한다.
8. 이것은 프로그램 좌표의 변경 증거다. 실제 다이를 잘못 선택했는지, 잘못 잡힌 최초 맵을 올바르게 고쳤는지는 좌표만으로 확정하지 못했다.
9. 개별 인풋 Vision 결과는 최종 픽업 계산에 다시 적용된다. 맵 검증 보강이 카메라→픽커 변환 또는 학습 보정 문제까지 해결한다는 주장을 하지 않는다.

## 4. 이번 수정과 소유 범위

경로는 모두 D:/Source/CDT-320_New 아래다. 이번 소스 변경은 아래 7개 파일이다. 이미 반영된 이전 단계 파일을 이번 수정 파일로 다시 계산하지 않는다.

| 파일 | 이번 수정 목적 |
|---|---|
| QMC.CDT-320/Form1.InputStageRunReview.cs | VERIFY 연결 제거, 기존 확인 연결, 검출/APPLY/요청 폐기 진단 로그 |
| QMC.CDT-320/Form1.InputStageRunReview.Geometry.cs | 3점 이동·수동 대응 확인 경로 제거, 기존 CONFIRM의 context 승인 연결 |
| QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.cs | 3점 검증 필수 조건·안내 제거, 기존 결정 잠금 보존 |
| QMC.CDT-320/Ui/Dialogs/InputStageRunReviewDialog.Designer.cs | VERIFY MAP 버튼·이벤트·추가 행 제거 |
| QMC.CDT-320/Equipment/Materials/MaterialStateService.InputPick.cs | context 승인 Commit 연결, 진행 자재 보존·저장 완료 판정 유지 |
| QMC.CDT-320/Equipment/Materials/MaterialStateService.InputStageReviewGeometry.cs | context 승인과 legacy baseline, 기존 후보 제한·승인 소비, Audit 진단 |
| QMC.CDT-320/Sequencing/Picker/PickerPickUpSequence.PickTargets.cs | 기존 계산 완료 문자열·로그 유지, PICKUP-TARGET Audit 기록만 추가 |

PickerMotionTargetResolver, InputPickerPickTargetResolver, PickerSequenceBase, 자동 Align/Review 단계, Interlocks 및 공통 Motion은 수정하지 않는다. 새 프로젝트 등록이나 운영 설정 변경은 이번 범위에 없다. 다른 채팅과 새 겹침이 발견되면 구체적인 범위를 사용자 승인 후 수정한다.

## 5. 구현 요구사항

### R1. 기존 시작 조작과 확인 의미

- Review에서 기존 시작점·픽업 순서를 확인한 뒤 CONFIRM / CONTINUE AUTO를 사용한다. 3점 선택·VERIFY MAP·개별 영상 대응 확인을 요구하지 않는다.
- CONFIRM은 현재 context와 후보의 일관성을 확인한다. CONFIRM-CONTEXT-PRODUCTION / NONPRODUCTION 구분으로 저장하며 실측 자료를 만들지 않는다.
- 기존 Align/Mapping 완료·모드·자재·순서 및 저장 성공 조건은 유지한다. 조건이 바뀌었으면 기존 CONFIRM에서 이유를 보여주며 무조건 승인하지 않는다.
- Simulation의 기존 SkipRunReview 설정 경로는 변경하지 않는다. 시뮬에서 진행됐다는 사실은 물리 좌표 검증 성공을 뜻하지 않는다.

### R2. 넓은 단발·누적 OFFSET과 baseline

- 기존 단발 X/Y와 누적 X/Y 설정을 유지한다. 기본 20 mm와 저장된 큰 값은 보존하고, 반 피치 상한이나 자동 clamp를 추가하지 않는다.
- finite·pitch·단발·누적·wafer/revision/context 검사를 검출/APPLY/Commit에 유지한다. 후보 origin - baseline origin으로 누적량을 계산한다.
- APPLY·pending 삭제·재선택·창 재열기로 baseline을 갱신하지 않는다. 새 mapping은 해당 revision의 새 기준을 만든다.
- baseline 없는 구형 자료는 현재 canonical Material과 완전히 같은 좌표만 최초 기준으로 채택하고 저장한다. 과거 누적량을 복원했다고 표시하지 않는다. 저장된 다른 revision 기준을 임의 재설정하지 않는다.
- Draft 변경은 기존 유효한 OFFSET 후보 이력을 따라야 한다. 실패 후보를 먼저 반영했다가 rollback하지 않는다.
- 단발/누적 제한을 만족한다는 사실은 실제 동일 다이, 물리 피치/각도/정확도의 입증이 아니다.

### R3. 결과 수명과 자재·저장 보호

- wafer/revision/UID/grid/reference와 session/request generation, T/pitch/condition/candidate 서명 비교를 유지한다.
- 늦은 비동기 결과, 검출 후 Jog/T/선택 변경, STOP/창 종료/세션 변경에는 이전 pending을 재사용하지 않는다.
- 기존 결정 잠금과 수동 callback 종료 확인·기존 JogStop·정지 확인·scope 해제 처리를 보존한다. 자동 모션/인터락을 수정하지 않는다.
- Commit은 UI bool만 믿지 않고 현재 context와 승인 요청을 비교한다. 저장 완료 전 소비 차단, 완료 뒤 현재 wafer/승인 ID/조건 재확인과 캐시 무효화를 유지한다.
- 부분 진행 wafer에서 좌표·상태·남은 순서가 같은 경우 확인 자료만 추가하고 기존 물리 이력과 전체 승인 순서를 보존한다. 승인 순서/revision 소실 또는 진행된 자재의 좌표 변경은 무조건 허용하지 않는다.
- 새 3점 필수 조건은 없으므로 남은 실제 다이가 3개 미만이라는 이유만으로 context 확인을 막지 않는다.

### R4. 추후 분석 가능한 로그

- Review 진단은 input-stage-review-v1 JSON을 IN-REVIEW-* Audit 이벤트로 남긴다. 실행·후보 등록·Draft 적용·확인 요청·저장 완료·거부를 구분한다.
- 현재 wafer instance/처리 세대/Lot/recipe/align run/mapping revision과 context 자료, 원점·baseline·pitch·T·한계·카메라 변환값을 기록한다.
- DIE DETECTION의 촬영 직전 actual XYT, raw Vision 결과, 예상 reference, 계산 candidate 및 중심 이동 후 actual XYT를 구분한다. 미수신을 가짜 0으로 기록하지 않는다.
- PICKUP-TARGET Audit은 실제 계산 완료 시점의 기존 문자열과 UID/wafer/grid/order/side/head slot 및 run 문맥을 재사용한다. 새 자재 조회·수식 계산·축 명령은 추가하지 않는다.
- 50 ms 승인 폴링마다 새 로그를 만들지 않는다. 이벤트 발생과 거부 이유 변경에만 기록한다. 로그 작성 실패가 기존 승인·픽업 결과를 바꾸지 않게 한다.
- 상세모드 OFF에서도 새 Audit은 영속 대상으로 분류한다. 비동기 디스크 쓰기의 전원 차단/저장장치 실패까지 무조건 기록을 보장한다고 표현하지 않는다.

## 6. 현재 결정과 남은 확인

수동 3점 검증 절차는 사용자 작업 방식과 맞지 않아 필수 시작 경로에서 제거했다. 기존 CONFIRM에 현재 자료의 일관성 확인을 연결하고 실측 여부를 로그에 명시한다. 과거 3점 정책/증거 자료형이 호환성 때문에 소스에 남아 있더라도 일반 Review가 이를 실행하거나 필수로 요구한다는 뜻은 아니다.

운영 설정·레시피·학습 XYT·콜렛 교정은 변경하지 않았다. 과거 JMB 로그의 큰 이동량은 프로그램 좌표 차이이며 실제 물리 픽업 오차로 단정하지 않는다. 로그를 통해 새 정상/이상 사례를 비교할 수 있게 하되, 실제 장비 정상화 여부는 현장 측정으로 확인해야 한다.

## 7. 구현 순서와 검증

1. 현재 로컬 파일·동시 변경을 확인하고 이번 7개 소스의 필요한 문맥만 수정한다.
2. 3점 UI/이동 경로와 필수 조건을 제거하고 기존 CONFIRM context 승인 및 legacy baseline 저장을 연결한다.
3. 기존 단발/누적/후보 수명/진행 자재/저장 검사를 유지한다.
4. Review Audit과 기존 계산 재사용 PICKUP-TARGET Audit을 연결한다.
5. 정책·Config·Material·context·로그 정책 검사 및 모션/분기/수식 불변 diff를 확인한다.
6. 원본 소스를 복제하지 않고 작업 전용 OutDir/중간 경로의 /t:Build만 실행한다. Clean/Rebuild·운영 출력·배포는 수행하지 않는다.
7. 현재 수정본의 최종 검증 결과를 03에 기록하고 실제 UI/장비 미검증과 구분한다.

최신 수정본의 오프라인 183개 검사와 4차 실제 등록 프로젝트 격리 Build(오류 0개·기존 경고 41개)가 통과했다. 이전 단계의 113개 검사와 오류 0개/기존 경고 41개는 이력이며 이번 수정본의 성공 판정을 대신하지 않는다.
