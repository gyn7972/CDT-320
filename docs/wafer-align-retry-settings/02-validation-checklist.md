# 웨이퍼 얼라인 설정 검증 체크리스트

- [x] 현재 로컬 master 확인, 기존 변경과 대상 파일 겹침 확인
- [x] 설정/UI와 JSON 하위 호환 구현
- [x] 얼라인 시퀀스의 독립 재시도/주변 탐색/timeout 연결
- [x] 기존 T 보정 반복, 작업 영역/안전 이동, 실패 후 Review/Alarm 분기 보존
- [x] JSON 누락키, 명시0, 범위 및 roundtrip 검증
- [x] 재시도0/1/3의 호출1/2/4, 조기 성공, 취소 검증
- [x] 탐색0/4/8, 작업 영역 제외 및 이동 실패 시 중단 검증
- [x] adapter timeout과 취소 전달, 기존 API5000ms 유지 검증
- [x] 격리 출력/중간 폴더에서 솔루션 Build
- [x] 변경 파일 diff --check, BOM/CRLF, 타작업 및 보호 코드 보존 확인

## 현장 확인 (미실행)

- [ ] INPUT STAGE 화면에서 세 항목 편집/저장 후 프로그램 재시작 시 유지 확인
- [ ] 다이가 없는 위치에서 재시도0/탐색0으로 최초1회 검사 후 기존 Review/Alarm 경로 도달 확인
- [ ] 재시도1/탐색4에서 상하좌우만 검사하고 중단/재개 시 안전 조건 유지 확인
- [ ] 정상 다이/부분 웨이퍼의 중심·Theta 검증·Ref1/Ref2 얼라인 확인
- [ ] 실장비 Vision의 정상 응답 시간을 확인한 뒤 timeout 조정 (5000ms 기본 유지 권장)

## 검증 결과

- 기준: D:\Source\CDT-320_New, master. 브랜치 생성·전환 없음.
- 변경 소스: InputStageUnit.cs, InputStageRecipePage.cs, InputStageAlignSequence.cs, IStageInterfaces.cs, VisionAdapters.cs, CDT320Machine.cs의 이번 기능 범위만 수정.
- Config: 실제 클래스 소스로 22개 시나리오/110 assertions 통과. 운영 파일 접근 없는 MemoryStream 직렬화 검증.
- Adapter: 실제 클래스와 인터페이스 소스로35 assertions 통과. 외부 통신 서비스는 fake로 대체.
- 시퀀스: 실제5개 메서드와 Config 정규화 소스로75 assertions 통과. 네트워크 요청·안전 이동은 fixture로 대체했으며 전체 시퀀스/실장비 검증은 아니다.
- 솔루션 Build 성공: 오류0/경고41. 별도 build 및 프로젝트별 obj 폴더 사용, Clean/Rebuild·배포·장비 실행 없음.
- 기존 CorrectThetaAsync / CorrectTwoPointThetaAsync / TryEnterManualAlignFallback 본문은 HEAD와 동일함을 확인.
- 보호된 PickerPickUpSequence.MotionResolvers.cs SHA256: 5CF43B053B518659063BBD00883A016C9E178312C18DBFA75654C8291957E3A4, 변경 없음.
- README는 다른 작업에서 수정 중이어서 문서 색인 연결을 생략했다. 이번 제품 구현 파일과 다른 작업의 겹침은 없었다.
- 검증 산출물: C:\Users\QMC\Documents\CDT-320 2\output\wafer-align-retry-settings-20260906 (build.log, config-tests, adapter-tests, sequence-tests).

## 설정 사용

레시피 → INPUT STAGE → OPTION → ALIGN VISION SETTING에서 설정한다. 장비 공통 Config로 저장되며 다음 얼라인 시작부터 적용된다. 시작 이후 Stop/Resume은 시작 시의 검사 설정을 유지한다.

재시도1 / 주변4 / timeout5000ms로 설정하면 중심과 주변 검색 한 차례의 최대 Vision 검사 수는36회에서10회로 줄어든다(종전 기본값 및 모든 후보가 작업 영역 안인 경우). 재시도0 / 주변0은 최초1회만 검사한다. 기존 Review/Alarm 처리 조건은 그대로다. timeout은 통신 단계별 제한이며 전체 얼라인 완료 시간은 아니다.
