# 픽커 FLOW 일회성 복구 체크리스트

기준 저장소: D:\Source\CDT-320_New
사용자 확정: 픽업 완료 VerifyPickerHasDieDataAndFlowAfterPick의 flowOn = true; 유지.

## 구현 및 로컬 검증

- [x] 최초 로컬 상태 확인 후 사용자 지시대로 기존 master에서 직접 수정. 작업 브랜치 없음.
- [x] 사용자 수정사항을 반영한 구현 프롬프트 작성.
- [x] Front/Rear P1~P4 FLOW 입력 더블클릭 연결.
- [x] Admin 이외 권한 차단, ‘아니오’/닫기 시 미적용.
- [x] 실장비·현재 Material·실제 FLOW OFF 조건과 확인창 이후 재검증.
- [x] 현재 다이와 위치/State 세대에만 유효한 메모리 승인.
- [x] 승인 상태 UI 표시와 실제 센서 값 구분. 리어 폭 449px에서 강제 ON·실제 OFF 문구 한 줄 표시 확인.
- [x] 생산용 흡착 확인 반영, Sim/DryRun 동작 보존.
- [x] Unit/raw 센서/캘리브레이션 판정 보존.
- [x] 픽업 완료 Test 코드 포함 MotionResolvers.cs 전체 SHA-256 원본 일치.
- [x] 픽업 백그라운드 확인의 다이 식별 고정. 동일 ID의 다른 객체/웨이퍼 승인 재사용 차단.
- [x] Place Material 전이 시 해당 픽커만 해제, 다른 픽커는 유지.
- [x] 수동 제거·다이 교체·상태 복원·모드 변경 시 재사용 방지.
- [x] 다음 픽업 전 해당 픽커의 이전 승인 제거 및 Material 점유 차단.
- [x] 동일 다이의 Alarm Reset/Start에서 승인 유지하도록 기존 재개 코드 보존. 실제 재개 모션은 현장 검증 대상.
- [x] 승인/해제 로그 파일 기록 확인과 새 파일 csproj 등록.
- [x] 실제 로컬 소스 별도 OutDir 및 프로젝트별 IntermediateOutputPath로 Build 성공.
- [x] 복구 상태 경계조건·실제 생산 FLOW 대기/취소·UI 이벤트 검증 183개 assertion 통과.
- [x] 이번 변경 파일의 git diff --check 및 UTF-8 BOM/CRLF 확인.

## 로컬 검증 결과 — 2026-09-06

- Visual Studio 2022 MSBuild, Debug / Any CPU, /t:Build 성공. 기존 미사용 필드 등의 경고가 있으며 컴파일 오류는 없음.
- 원본 프로젝트의 Clean/Rebuild를 실행하지 않았고 운영 폴더 D:\CDT-320에 빌드 결과를 배포하지 않음.
- 검사 프로그램은 현재 로컬 소스로 빌드한 어셈블리의 MaterialStateService, 실제 Material Place/수동 제거 메서드, 양쪽 생산 FLOW 판정·대기 메서드와 I/O 패널을 사용함.
- 장비/컨트롤러는 생성자를 실행하지 않은 테스트 객체, 입력은 하드웨어 접근이 없는 대역. 실제 Form1 대신 테스트 Form을 사용하고 확인창 결과를 Yes/No/닫기로 주입해 현재 Helper 소스의 승인 전후 검증을 실행함.
- MaterialSnapshotStore의 운영 저장 경로를 보호하기 위해 테스트 프로세스 안에서 백그라운드 저장 worker 시작을 차단함. 실제 Snapshot 저장·복원이나 장비 연결을 테스트한 결과가 아님.
- Front/Rear P1~P4 개별 승인, Good/NG 실제 Material 전이, 실패 Place 유지, 다른 픽커 유지, 다음 픽업 재사용 차단, 실제 입력 불변, 권한 변경/다이 교체/모드 만료를 검증함.
- 양쪽 생산 FLOW 대기가 승인 전에는 대기하고 취소가 전달되며 승인 후 성공하는 것을 확인함.
- WinForms 행/점/라벨의 더블클릭, 단일 클릭 무반응, 오른쪽 더블클릭 무반응, 확인 중 재진입 차단, 기존 출력 토글, 강제/실제 표시를 검증함. 실제 마우스와 전체 장비 화면에서의 조작은 현장 검증 대상임.
- 보호 파일 6개의 SHA-256 일치: MotionResolvers, Front/Rear Unit, BaseDigitalInput, PickUp/Place Z Calibration.
- 원래 LF였던 csproj는 저장소 .editorconfig에 따라 CRLF로 저장됨. 줄 끝 차이를 제외한 이번 작업의 실질 변경은 신규 Compile 항목 2개임. 마무리 중 다른 작업에서 추가한 EventLogDisplayBuffer Compile 항목도 보존함.
- 작업 중 별도로 변경된 GeneralPage.Designer.cs, EventLogPage.cs/Designer.cs, Ui/Common/History와 docs/input-stage-review-offset는 이번 작업에서 수정하거나 되돌리지 않음. 저장소 전체 diff의 해당 기존 공백 경고는 이번 검증 범위에서 제외함.

검증 스크립트와 로그는 C:\Users\QMC\Documents\CDT-320 2\output\picker-flow-recovery-20260906에 있음.
build-local.ps1 / build.log, run-checks.ps1 / checks.log, PickerFlowRecoveryChecks.cs를 통해 재현할 수 있음.
운영 실행파일로 이 검증 출력을 자동 복사하지 않음.

## 현장 검증 — 로컬 코드 검증으로 대체하지 않음

- [ ] 실제 실행파일과 수정 소스의 버전 일치 확인.
- [ ] Front/Rear 각각 P1~P4: 실제 다이 보유 + Material 일치 + FLOW OFF에서 Admin 승인.
- [ ] 비Admin과 ‘아니오’에서는 신호 판정 변화 없음.
- [ ] Bottom/Side/Place 흐름이 승인한 다이에 한해 진행.
- [ ] 여러 픽커 동시 승인 후 하나씩 Place할 때 개별 해제.
- [ ] Good/NG 및 일반/Conti Place 경로에서 Material 전이와 해제 확인.
- [ ] Place 실패/정지 후 같은 다이를 재개하면 승인 유지, 완료 시 해제.
- [ ] 다음 픽업의 기존 실제 FLOW 확인에서 OFF이면 알람 발생. 유지한 완료 Test 코드의 무조건 통과 범위는 기존과 동일.
- [ ] 실제 FLOW가 복구된 경우 강제 해제 후에도 실제 ON 표시 유지.
- [ ] 이미 알람 정지된 경우 승인 후 RESET ALARM → START로 재개. 기존 Ready·모션 인터락 유지.
- [ ] 프로그램 재시작, Material 제거/복원, 모드 변경 뒤 승인 재사용 없음.
