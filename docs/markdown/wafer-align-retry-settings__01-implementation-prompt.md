# 웨이퍼 얼라인 재검사 설정 구현 프롬프트

## 사용자 승인과 범위

현재 로컬 D:\Source\CDT-320_New의 master에서 구현한다. 다른 작업의 변경을 보존하고 파일이 겹치면 필요한 수정 범위를 설명한 뒤 사용자 승인을 받는다. 브랜치/worktree 생성·전환, 다른 코드 복원·덮어쓰기, commit/push를 하지 않는다.

승인된 변경은 웨이퍼 얼라인의 미검출 추가 검사 횟수, 주변 탐색 위치 수, Vision 응답 대기 시간의 설정화다. 기존 시퀀스의 이동 경로·작업 영역 검사·인터락·실패 후 Review/Alarm 분기는 유지한다. 기존 픽업 완료 검사의 flowOn = true Test 코드는 수정하지 않는다.

## 구현 계약

레시피 → INPUT STAGE → OPTION → ALIGN VISION SETTING에 다음 장비 공통 Config 항목을 추가한다.

| 화면 항목 | Config 키 | 허용값 | 기본값 |
| --- | --- | --- | --- |
| ALIGN VISION RETRY | AlignVisionRetryCount | 추가 0~10회 | 3 |
| ALIGN SEARCH POINTS | AlignSearchPointCount | 0 / 4 / 8곳 | 8 |
| ALIGN VISION TIMEOUT | AlignVisionTimeoutMs | 500~30000ms | 5000 |

재시도 0은 최초 검사 1회다. 주변 0은 탐색 이동을 생략하고, 4는 기존 순서의 상하좌우, 8은 기존 대각선까지 사용한다. 각 위치에 동일한 재시도 설정을 적용한다. 중심과 주변 한 차례 검색에서 모든 위치가 작업 영역 안이라면 최대 호출 수는 (1 + 재시도) × (1 + 주변 위치 수)다. 기본값은36회, 1회/4곳은10회, 0회/0곳은1회다. 이는 전체 얼라인의 총 호출 수가 아니다.

기존 ALIGN ITERATIONS는 T 보정 반복에 계속 사용하고, 미검출 검사에서는 Math.Max(3, Options.AlignRetryCount) 강제를 제거한다. 새 검사는 기존 ALIGN ITERATIONS와 독립한다. 기본값3은 종전 기본 설정의 동작을 유지한다. 이전 ALIGN ITERATIONS가3보다 큰 경우 미검출 횟수까지 그대로 복제하지 않는다.

얼라인 시작 시 세 설정을 캡처하여 Auto/Manual/Step의 동일 얼라인 시퀀스에 적용한다. 진행 도중 변경은 다음 얼라인 시작부터 적용한다. JSON에 새 키가 없으면3/8/5000으로 복원하고 명시적인0회/0곳은 유지한다. 잘못된 값은 정해진 범위로 정규화한다.

Vision timeout은 얼라인 통신 단계별 응답 대기 제한이며 전체 요청/얼라인의 총 시간 제한이 아니다. 기존 string-only Vision API의 기본5000ms 동작은 유지하고 얼라인 전용 overload에서 설정값과 CancellationToken을 실제 Wafer adapter로 전달한다. 다른 Die 검사/Mapping/Picker/Vision 서비스의 전역 timeout은 변경하지 않는다. 순수 Sim 및 DryRun의 기존 대체 결과 처리도 유지한다.

## 검증과 산출물

장비 프로그램 실행, 축/IO/카메라 통신, 운영 Config 수정 없이 격리 검사와 별도 OutDir 및 프로젝트별 IntermediateOutputPath의 Build를 수행한다. JSON 기본값/0값/범위/왕복 저장, 실제 재시도 루프의 호출 횟수/조기 성공/취소, 주변0/4/8과 작업 영역 및 이동 실패 처리, adapter timeout/token 전달을 검증한다. 기존 T 보정/실패 분기와 보호 코드 보존을 diff로 확인한다. 실장비 확인은 체크리스트의 미검증 항목으로 남긴다.
