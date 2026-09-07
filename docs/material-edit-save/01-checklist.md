# Material 편집·저장 및 Recipe UI 지연 개선 체크리스트

기준: 2026-09-07 / D:\Source\CDT-320_New / master / c262240d 및 최신 로컬 변경.
근거 자료: D:\Source\DATA_LOG. 작업 규칙은 저장소 AGENTS.md를 따른다.

## 확인한 원인

- [x] 13:05:08 Input 재매핑 직후 기존 Output Die parent 연결 손상과 저장 실패를 로그·코드로 대조했다.
- [x] 14:23:22 Output 97개 SKIP 직후 DieUid/SourceDieUid 불일치가 발생한 것을 확인했다.
- [x] Wafer typed clone 기대 속성 82개와 실제 87개 불일치로 느린 복사를 사용하는 것을 확인했다.
- [x] Recipe 항목 편집이 UI에서 전체 Machine 데이터를 동기 저장하는 경로를 확인했다.
- [x] 사용자는 UI가 몇 초 후 다시 반응한다고 확인했다. 13.5초 StateReadLock은 대기와 처리 합산으로 해석한다.

## 작업 보호와 승인

- [x] 현재 master를 확인했다. 새 브랜치·worktree를 만들지 않는다.
- [x] 시작 시 다른 작업의 Calibration 데이터·Dialog 변경을 확인하고 범위에서 제외했다.
- [x] 수정 직전마다 대상 파일의 현재 내용과 다른 작업의 변경을 재확인했다.
- [ ] 재매핑·Output 작업 대상 의미·Clear 보호 변경의 구체 범위에 사용자 승인을 받는다. [변경안](02-approval-plan.md)
- [x] Recipe setter와 runtime 반영 시점을 유지했다. 초안/다음 작업 적용 정책이나 운전 진입 조건을 새로 변경하지 않았다.

## 구현

- [x] typed clone의 모든 저장 필드와 중첩 독립성을 검증하고 최적화 가드를 정상화했다.
- [x] 동일값 편집의 중복 저장을 생략했다.
- [x] 관련 Unit의 Recipe/Setup/Config만 사본으로 만들고 파일 쓰기를 UI 밖에서 수행한다. 사본의 JSON 직렬화는 기존 값/형식 계약을 보존하도록 UI에서 수행하며 100ms 이상을 진단 로그로 남긴다.
- [x] 저장 대상 Recipe 이름과 값의 사본을 고정하고 파일별 동기/비동기 요청 순서·실패·종료 처리를 구현했다.
- [x] UI 상단 표시의 Material 잠금 대기를 줄였다. 표시 사본을 제어 판단에 사용하지 않는다.
- [ ] 승인 후 일반 재매핑의 부모 연결 손상을 방지한다. Input의 명시 WAIT 재픽업 복구와 GUI에서 확정한 GOOD/NG/SKIP/FLYING 상태를 보존한다.
- [ ] 승인 후 Output 상태 변경을 일괄 처리하고 실제 배치 UID 관계를 보존한다.
- [ ] 승인 후 Input Clear가 Output 역참조를 가진 Die를 부분 삭제하지 않게 한다.
- [ ] 변경 검증과 저장 실패를 구분하여 실제 미저장을 성공으로 표시하지 않는다.
- [x] Input 상태 적용의 개별 동기화/최종 flush 실패를 성공으로 표시하지 않도록 수정했다. Pick Status는 실제 계약에 맞게 'Material 반영 및 저장 요청'이라고 표시한다. 기존 상태값·WAIT 복구·후속 runtime 반영 순서를 유지했다.

## 격리 검증

- [x] typed/reflection 복사 값 동등성과 중첩 객체 독립성: clone-run-tests.ps1 / 12 시나리오 통과.
- [x] 표시 잠금 경합/현재 ID/잠금 반환: display-run-tests.ps1 / 3 시나리오, 21 assertions 통과.
- [ ] 이미 Output에 배치한 Die를 포함하는 재매핑 후 저장·재로드 및 재픽업 대상 제외.
- [ ] 배치한 슬롯의 SKIP/GOOD/NG/WAIT 편집 후 UID·이력 보존과 저장 검증.
- [ ] Input Clear 후 Output 이력 유지, 잘못된 역참조에서 부분 삭제 없음.
- [x] 순차 UI 저장·동기 저장과 요청 순서 경합·느린 쓰기·실패/재시도·Load/Copy/Rename/Delete barrier: 실제 공용 저장 소스 격리 시험 57 assertions 통과.
- [x] 실제 Project 복사 경로의 직접 File.Copy 앞에도 UI 비동기 preflush를 연결했다. 일반 Recipe 페이지 전환에는 대기를 추가하지 않았다.
- [x] 실제 Handler의 설정·Recipe DTO 39종에서 기존 동기 JSON과 사본 JSON이 동일함을 확인했다. Machine/Unit/Axis/IO를 생성하지 않았다.
- [x] 현재 Input UI 소스의 결과 전달 분기 85 assertions 통과: 5상태 정상 경로, WAIT 복구 호출 유지, 부분 실패 이후 후속 처리, flush 실패, 취소와 기존 guard, Warning 안내. 의존성을 대역 처리한 메모리 하니스이며 실제 장비 동작 검증은 아니다.
- [ ] 실장비 UI에서 페이지 전환·저장 실패 안내·Retry/Cancel·종료를 직접 확인한다. 공용 저장 코드 시험과 GUI 실측을 구분한다.
- [x] 수정 파일 UTF-8 BOM/CRLF/final newline, 새 생산 파일 3개 csproj 등록, git diff --check, 최종 별도 OutDir/IntermediateOutputPath Build를 확인했다. Build 오류 0, 기존 미사용 필드 등 경고는 남아 있다.
- [x] 관련 없는 작업 변경을 보존하고 미검증 항목을 구분했다.
- [ ] 이 작업의 임시 빌드 out/obj 및 중간 시험 폴더 정리: 경로를 확인하는 삭제 명령이 자동 승인 검토에서 거부되어 실행하지 못했다. 세부 사유는 제공되지 않았으며 우회 삭제하지 않고 그대로 보존했다.
- [ ] 현장 검증: 실제 센서/자재와 저장 상태 대조, 작업 도중 정지·재개, 설정 변경 후 운전. 로컬 검증으로 완료 처리하지 않는다.

## 진행 기록

- 구현 시작. 저장 복사와 Recipe UI 저장은 독립적으로 진행하며, 장비 동작에 영향을 주는 데이터 적용 부분은 승인 대기.
- 사용자 요청으로 잠시 보류 후 인터넷 재연결 시점에 재개했다. 승인 대상은 별도로 유지한다.
- 복사 시험: 4 Wafer / 3,128 Die 및 검사 자료를 사용한 합성 데이터, 3회 준비 후 7회 중앙값은 reflection 182.916ms / typed 16.359ms였다. 같은 장비 로그 규모를 사용한 로컬 참고값이며 실장비 시간 보장은 아니다.
- 표시 전용 TryGetProcessingDisplayIds는 즉시 잠금 획득 실패 시 다음 UI 주기로 미룬다. ReadState 진단은 StateReadWait/StateReadHold로 대기·처리를 분리했다.
- 사용자 추가 요구에 따라 [GOOD/WAIT/NG/SKIP/FLYING 전체 수동 흐름](03-manual-state-flow.md)을 분석했다. 기존 정상 수동 변경 기능을 보존하는 것으로 승인안을 수정했으며, 이전 답변을 승인으로 취급하지 않았다.
- 1차와 최종 통합 Build가 모두 성공했다. 최종 로그: `_build_check_material_edit_save/build-final.log`. 운영 경로로 복사하거나 프로그램을 실행하지 않았다.
- [구현 프롬프트](04-implementation-prompt.md)에 적용 범위와 승인 대기 범위를 구분했다. Material 부모 연결/상태 원자 적용 결함은 아직 고쳐진 것으로 표시하지 않는다.
- 실제 기본 DTO 사본 준비: 최초 0.25~61.9ms, warm 중앙값 0.002~7.308ms/종. 관련 Unit 전체의 현장 시간과 동일하지 않으며, 사용자 확장 설정의 캡처 시간은 UI-PARAM-SNAPSHOT 로그로 확인한다.
- 최종 시험 자료: `C:\Users\QMC\Documents\CDT-320 2\output\material-edit-save-20260907`의 clone/display/ui-final/ui-models 및 ui-final-results.log. 테스트는 장비 앱을 실행하지 않았다.

## 이번 수정 파일 범위

- MaterialSnapshotStore.cs, MaterialStateService.cs, 새 MaterialStateService.Display.cs: 복사 가드·잠금 진단·상단 표시 조회.
- Form1.cs, 새 Form1.ParameterSaving.cs: 관련 데이터 저장 큐 연결, 실패/재시도/종료 처리.
- ParameterGridControl.cs, Recipe의 InputCassette/InputFeeder/InputStage/FrontPicker/RearPicker/OutputStage/OutputFeeder/OutputCassette/VisionRecipePage.cs: 동일값 중복 저장 제외와 기존 Unit별 저장 범위 연결.
- Recipe/ProjectPage.cs: 명시적 파일 작업 전 비동기 저장 확인, 삭제 실패 반환 확인.
- Work/InputStageMapTransferPage.cs: 기존 동작을 유지하며 성공/실패 결과 전달과 안내만 수정.
- JsonDataStore.cs, 새 JsonDataSaveCoordinator.cs: 파일별 최신 요청 순서, 독립 사본의 파일 저장, 실패/재시도, 명시적 읽기/복사/삭제 경계.
- Handler/Common csproj의 새 파일 등록, README 링크, 이 폴더 문서와 격리 시험.

다른 작업의 Calibration 데이터/시퀀스/Dialog 6개 파일은 수정하지 않았다. Material의 재매핑·Output 상태 의미·Clear·Input 후보/Review 정책 및 축/센서/인터락은 변경하지 않았다.
