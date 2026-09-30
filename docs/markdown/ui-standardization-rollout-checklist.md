# GUI 공용화 순차 적용 — 검증 체크리스트

작성 기준: 2026-09-22. [실행 범위·순서·승인 조건](ui-standardization-rollout-prompt-2026-09-22.md)을 먼저 읽는다. 이 문서는 운영 적용의 실행 기록이며, [공용 기반의 과거 검증 기록](ui-common-button-designer-validation-checklist.md)과 구분한다.

**2026-09-28: 사용자 승인 후 2단계의 EventLogPage 버튼 3개만 고정 Role·기본 테두리로 적용했다. 정적 검사·격리 기능 검사·안전 Build는 통과했으나 실제 Designer/입력/운영 셸·장비 검증은 남아 있다. 2단계 전체 완료 판정과 3~8단계 확대는 보류한다.** 2026-09-27의 운영 무수정 사전검증 기록은 아래에 보존한다. 체크된 항목만 실제 확인한 결과이며, 부분 확인·실패·미검증을 통과로 바꾸지 않는다.

2026-09-29 추가 승인: 사용자가 EventLog의 `Last 1 hour` 기본 해제 및 AlarmHistoryPage 날짜별 조회 구현을 요청했다. 아래 별도 실행 기록을 따른다. 이는 3~8단계 UI 전면 적용이나 기존 미검증 항목의 통과 승인이 아니다.

## 기록 원칙과 작업 표지

체크는 실제 통과한 항목만 표시한다. 실패/미검증/해당 없음은 아래 결과표에 사유를 남긴다. 원본 Designer·실제 파일 선택기·무장비 자동 검사·사용자 현장 확인을 구분한다. 수행할 수 없는 필수 검사를 임의로 해당 없음 처리하지 않는다.

| 기록 항목 | 이번 실행값 |
| --- | --- |
| 실행 일시 / 담당 AI·검토자 | 2026-09-28 / Codex, 구현·독립 읽기 전용 리뷰·기능/클릭 통합 검사 분담 |
| 원본 Git 루트 / 브랜치 / HEAD | `D:\00.Source\CDT-320_New` / `master` / `57db566ec3db14d4d6cccadfea36b28697ce2053` |
| 승인된 단계 / 승인 내용 | 0~1단계 보고 후 사용자: “기능상 문제없게 적용해줘.” C04/C07 제한은 남기고 EventLog 고정 Role·기본 테두리 세 버튼만 제한 적용 |
| 허용 파일 / 컨트롤 | 운영: EventLogPage.Designer.cs의 btnRefresh/btnOpenFile/btnLive 타입·Role·충돌 외관 속성만. 이 체크리스트와 격리 검사 자료 갱신. Common·업무·시퀀스·인터락 변경 없음 |
| 시작 시 dirty/untracked 및 겹치는 변경 | 기존 추적 파일 22개 수정, untracked 소스·문서 존재. 전체 1,222개 파일 SHA-256 기준 확보. EventLogPage 본체/Designer/resx는 시작 시 clean. Common 버튼·팔레트 등 기존 untracked도 보호 대상 |
| VS / 구성·플랫폼 / DPI·해상도 / OS·PC | VS 18 Community MSBuild / Debug·Any CPU / Windows 11 Pro 10.0.26200, i7-1360P(16 논리 CPU). 운영 화면 DPI·해상도 및 실제 Designer 미검증 |
| 무장비 검사에서 대체한 외부 의존성 | 아래 날짜별 기록에서 구분. 기능 하네스는 PageBase를 대체하지만 통합 하네스는 원본 PageBase/Auditor 직접 사용. 기록기·설정 등은 양쪽 모두 대체 |
| 안전 OutDir / 결과 증거 경로 | `_codex_verify_ui_rollout_20260928/build/out`, 같은 날짜의 eventlog/integration. 미완료 검증 재현용으로 유지. 과거 산출물 정리 미완료 사유도 아래 보존 |
| 단계 판정 / 남은 검사 / 다음 승인 | 세 버튼 코드 적용·격리 검사 완료. 실제 Designer 저장/재열기, OS 입력·파일 선택, 8개 운영 탭과 실장비 부하는 미확인. 3단계 자동 진행 금지 |

## 0단계 — 변경 전 기준

- [x] G00: Git 루트가 `D:\00.Source\CDT-320_New`, 기본 브랜치가 master인지 확인. 다른 경우 임의 전환하지 않고 보고.
- [x] G01: AGENTS.md/인코딩 규칙을 읽고, 사용자 변경과 무관한 파일의 비교 기준(해시 등)을 확보.
- [x] G02: Common 버튼/팔레트, 기존 카탈로그/템플릿/UiTheme를 실제 소스에서 확인. 삭제된 Preview/DesignerCheck를 만들지 않음.
- [x] G03: 대상의 Name/Text/Tag/번역 키, 필드·생성 타입, Click 연결·횟수, TabIndex/TabStop, Enabled/권한 조건 기록.
- [ ] G04: Bounds/최소·최대 크기, Font, Margin/Padding, Dock/Anchor, TableLayout 행·열·셀·자식 순서, AutoScaleMode/Dimensions 기록.
- [ ] G05: 기본/hover/down/클릭 중/복원 후/Disabled 색상과 그 값을 대입하는 코드 위치 기록. 외관 변경 기대값은 기존 동작과 구분.
- [ ] G06: 같은 시험 데이터·조작 순서로 변경 전 기능 결과와 성능을 측정. 변경 후 옛 소스로 롤백해 기준을 만들지 않음.
- [x] G07: 대상 소스에 기존 오류가 있으면 이번 변경으로 생긴 문제와 구분. 승인 없이 관련 없는 오류 수정 금지.

## 1단계 — 공용 버튼과 기존 클릭 처리의 호환성

0~1단계는 운영 소스 무수정의 사전검증 묶음으로 수행하고 결과 보고 후 멈춘다. 현재 소스의 UiStandardButton과 실제 UiClickAuditor 경로를 사용한다. 기존 Button도 동일 조건으로 비교한다. 별도 장비 프로그램을 띄우지 않는다.

- [x] C01: Default/Primary/Dark/Danger 기본색과 개별 BackColor/ForeColor 지정·Reset을 확인. 크기·폰트·배치·Enabled가 Role 변경으로 바뀌지 않음.
- [ ] C02: 단일 클릭 후 기존 기능이 한 번 실행되고 임시 클릭색이 원래 의도한 색으로 복원됨. UI 타이머이므로 220ms를 실시간 정밀 보장으로 해석하지 않음.
- [ ] C03: 220ms 안팎의 연타, 버튼 간 교대 클릭, hover 상태·포커스 이동에서도 노란색 고착이나 잘못된 복원이 없음.
- [ ] C04: 클릭 후 Role 변경, 명시적 개별색 유지, Reset 후 Role 재변경을 각각 확인. 알려진 개별색 플래그 제한의 예상/실제 값 기록.
- [ ] C05: 마우스·Space·Tab·Shift+Tab·Enter가 기존 화면의 키보드 정책과 동일. Enabled=false에서 기능 미실행. 존재하지 않던 Enter 동작을 새로 만들지 않음.
- [ ] C06: 클릭 중 Enabled 변경, Hide/재진입, Dispose에서 예외·잘못된 색 복원·남은 타이머·중복 핸들러가 없음.
- [ ] C07: 현재 소스와 메모리 직렬화 검사에서 역할 기본 RGB 고정 여부·개별색 보존 및 FlatAppearance 저장 순서 확인. 1단계에서 원본에 시험값을 저장하지 않음. 실제 속성 편집·저장 검증은 2단계 또는 별도 승인 후 수행.

C04 등에서 제한이 재현되면 다음 단계 자동 진행을 중단한다. 공통 코드 수정안 또는 고정 Role 화면에만 제한 적용하는 안을 제시하고 승인받는다. 제한 적용 승인 시 이 항목을 통과로 바꾸지 말고 **‘실패/제한 재현, 예외 승인 범위’**를 기록한다. 그 승인은 동적 상태색 화면으로 확대할 수 없다.

## 2단계 — EventLogPage 버튼 3개 첫 운영 적용

### 적용 경계

- [x] A01: 사용자가 2단계를 승인했고 1단계 실패/제한에 대한 처리 방향이 확정됨.
- [x] A02: EventLogPage.Designer.cs의 btnRefresh→Primary, btnOpenFile→Default, btnLive→Dark만 적용. 각 버튼 확인 후 다음 버튼 진행.
- [x] A03: 필드 선언/생성 타입/Role 및 충돌하는 세 버튼 외관 속성 외의 변경이 없음. 업무 .cs/버퍼/기록기/공통 클릭 처리/프로젝트 설정은 그대로.
- [x] A04: 이름·문구·이벤트·TabIndex 12/13/14·현재 크기/글꼴/레이아웃/번역 키를 기준과 비교. S/M/L 규격에 맞춘 재배치 없음.

### 원본 Designer와 빌드

- [ ] D01: EventLogPage.cs에서 실제 Designer 열기 성공. 기본 생성자/디자인 모드에서 장비 서비스나 파일 읽기가 실행되지 않음.
- [ ] D02: 세 버튼을 선택할 수 있고 Common 타입/Role/일반 속성이 표시됨. 승인된 Role/색상 변경이 화면에 반영됨.
- [ ] D03: 승인된 변경을 속성 창에서 편집·저장→닫기→재열기 후 값이 유지됨. 불필요한 시험값을 남기거나 무단 원복하지 않음.
- [ ] D04: 저장 후 Name/Click/Tab/태그/개별색 보존, 기본 RGB 재고정 여부, 리소스/프로젝트 등록 추가 여부 확인.
- [ ] D05: 저장 DPI가 달라졌다면 숫자 diff만으로 판정하지 않고 같은 DPI에서 초기화·레이아웃 완료 후 실제 크기/잘림/배치를 비교. 대상 이외 레이아웃 변경은 보류.
- [x] D06: 기능 분할 partial 파일에 InitializeComponent나 resx가 새로 생기지 않음. 중복 리소스 출력 이름 없음.
- [x] B01: 허용 범위 diff와 인코딩 검사 통과. 기존 무관한 dirty 변경을 정리하지 않음.
- [x] B02: 원본 솔루션 Debug/Any CPU, 별도 안전 OutDir `/t:Build` 성공. 실제 오류/경고 수·명령·종료 코드 기록. Clean/Rebuild/운영 파일 덮어쓰기 없음.

### 기능·비동기·수명

- [ ] F01: 세 버튼을 마우스/키보드로 조작해 기존 핸들러 실행 횟수 확인. 클릭 효과가 기존 기능을 막거나 이중 호출하지 않음.
- [ ] F02: REFRESH 후 파일 조회 모드와 제목이 유지되고 새 실시간 행이 섞이지 않음. 개별 파일을 연 상태에서는 그 파일을 다시 조회.
- [ ] F03: OPEN FILE 선택 시 해당 CSV와 제목 표시. 취소 시 기존 모드·경로·행 유지. 실제 OS 선택기 검증과 테스트 대체 검증을 구분.
- [ ] F04: 빈 파일/없는 파일/접근 실패/잘못된 내용에 대한 기존 처리 결과와 재사용 가능 여부 비교. 기존 읽기 API가 오류를 빈 결과로 처리하는 경우 새 오류 메시지를 임의 요구·추가하지 않음.
- [ ] F05: 실시간 버튼으로 개별 파일 경로 해제·오늘 날짜·메모리 로그 복귀. 그 뒤 새 로그 반영. 다른 필터 조건은 기존 정책 유지.
- [ ] F06: 종류/검색어/최근 1시간/500·2000·ALL 필터와 행 순서·선택 동작이 기존과 동일. ALL 안전 상한 10,000행 보존.
- [ ] F07: 같은 CSV에서 파일 모드 검색, 날짜 변경, 다시 검색해 이전 날짜·파일 결과가 뒤섞이지 않음.
- [ ] F08: 큰 파일 조회 중 REFRESH 연타→날짜 변경→실시간 전환. 늦은 이전 결과가 최신 모드·행·제목을 덮지 않음. UI 입력 가능.
- [ ] F09: 조회 중 탭 이탈/재진입/Dispose 후 오래된 결과 갱신·중복 로그 구독·불필요한 타이머 지속이 없음.
- [ ] F10: 테스트 환경에서 이력 표시 설정 Off/On 시 기존 안내·세 버튼 비활성/복구 정책 유지. 운영 설정을 시험용으로 저장하지 않음.
- [ ] F11: 언어 변경과 재진입 후 번역·조회 모드 제목·문구 잘림·버튼 의미 보존.
- [ ] F12: 폭주 후 최신 범위 복원, 같은 객체 중복 제외와 별도 발생한 동일 문구 보존, 250ms/100행 처리·대기 1000행 상한이 기존과 동일. 처리 주기/상한 자체는 수정하지 않음.

### 공유 사용처 — 8개 모두 기록

HistoryTab의 EventLogPage 프리셋이다. 로그 종류 이름에 Sequence가 있어도 시퀀스 코드를 수정하는 단계가 아니다. AlarmHistoryPage는 별도 대상이다. 아래 “격리 통과”는 원본 EventLogPage에 각 EventKind를 전달한 합성 데이터 검사다. 실제 HistoryTab/TabBase 진입·번역·OS 입력의 통과는 아니므로 F01~F12 전체 체크는 보류한다.

| 프리셋 | 진입·종류/제목 | 세 버튼·색상 | 파일↔실시간·필터 | 재진입/구독 | 판정·근거 |
| --- | --- | --- | --- | --- | --- |
| Event | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 2026-09-28 / 운영 셸 미확인 |
| Warning | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| Data | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| Work | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| InputSeq | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| FrontHeadSeq | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| RearHeadSeq | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |
| OutputSeq | 격리 통과 | 고정 Role 값 통과 | 격리 통과 | 격리 통과 | 동일 |

## 성능 검증 — 변경 전후 동일 조건

단순 실행 성공이나 타이머 Tick 발생만으로 성능 통과를 선언하지 않는다. UI 소스 변경 없이 측정 가능한 방법을 우선 사용하고, 계측용 생산 코드 삽입이 필요하면 승인받는다.

- [ ] P01: 동일 PC/구성/DPI/데이터/선택 필터/로그 입력률/시험 시간으로 전후 각각 최소 3회 측정. 첫 실행 준비 비용과 정상 반복을 구분.
- [ ] P02: 조회 행 0/500/2000/10,000 수준, 큰 CSV 조회 중 모드 전환, 1000행 대기 상한을 넘기는 로그 burst 시나리오를 기록. 운영 로그 기록기를 부하 생성기로 변경하지 않음.
- [ ] P03: 입력→표시 응답 시간과 UI 메시지 처리 지연의 중앙값/p95/최대, 조회 완료 시간, CPU 평균/최대, 메모리, 핸들/타이머 수 또는 가능한 대체 지표를 전후 기록.
- [ ] P04: 20회 진입/이탈·연속 조회 후 메모리/구독/핸들의 지속 증가 여부를 안정화 시간까지 관찰. 측정 불가 지표는 미측정으로 표시.
- [ ] P05: 입력 누락/응답 없음/신규 UI 스레드 긴 작업이 없고, 기준의 변동 범위를 넘어 반복 재현되는 성능 악화가 없음. 변동이 커 판정할 수 없으면 재시험 또는 보류.
- [ ] P06: 실장비 Auto 부하와 시퀀스 주기 영향은 별도 승인된 현장 검증으로 분리. 무장비 결과로 보증하지 않음. 미검증이면 다음 운영 확대 판단에 명시.

| 시나리오·조건 | 변경 전 3회 | 변경 후 3회 | 응답/CPU/메모리 등 비교 | 통과·실패·보류와 이유 |
| --- | --- | --- | --- | --- |
| 일반 메모리 조회 0/500/2000행 | 각 18표본 | 각 18표본 | 2026-09-28 상세 표 참조. 최초 화면 진입 시간은 미측정 | 변동·일부 중앙값 증가로 성능 동등 판정 보류 |
| 10,000행 검색·필터 | 각 18표본 | 각 18표본 | 조회 432.710→387.266ms, 검색 198.355→200.979ms 중앙값 | 기존 UI 지연 유지. 성능 문제 없음으로 판정하지 않음 |
| 파일 조회 중 실시간 전환 | 합성 지연 응답 검사 | 동일 검사 통과 | 늦은 결과 폐기 확인. 실제 CSV·입력 응답 시간 미측정 | 기능 격리 통과 / 성능 보류 |
| 로그 burst·20회 재진입 | 구독·타이머·핸들 기록 | 동일 검사 통과 | 대기 상한/최신 범위 복구. Visible 20회 통합 검사 별도 통과 | 운영 셸·장시간 안정화 미검증 |
| 승인된 실장비 Auto 부하 | 미실시 | 미실시 | — | 별도 승인 필요 |

## 3~8단계 — 매번 새 범위 승인 후 반복

아래는 후속 단계의 추가 관문이다. G/A/D/B/F/P의 해당 항목도 매 단계 다시 수행한다. 여러 단계를 묶어 대량 적용하지 않는다.

| 단계 | 추가로 확인할 것 | 중단해야 할 경우 |
| --- | --- | --- |
| 3: 정적 라벨·입력·그리드 | 기존 공용 타입/샘플 재사용 가능 여부. Label의 자동 크기·말줄임, TextBox의 단일/여러 줄·ReadOnly·MaxLength·IME·입력/검증 이벤트, 숫자 범위·단위, Grid의 바인딩·선택·정렬·열 정책 보존 | 높이를 맞추려고 Multiline을 변경하거나 입력 형식·Enter 처리·행 수·선택/검색 동작이 달라짐. 새 타입 필요성 분석/승인 없음 |
| 4: MessageEditPage | Maintenance 권한, 편집·저장·가져오기, 번역/메시지 키, 취소·오류·변경 감지 | 실제 운영 메시지 카탈로그를 시험 저장하려 함. 기존 데이터/권한 변경 필요 |
| 5: AlarmHistoryPage | 현재 알람 상태, Clear 가능 조건, 타이머·구독·표시/언어, 기본 버튼과 상태 표시 구분 | 시험을 위해 운영 AlarmManager.ClearAll 실행 또는 해제 조건 변경 필요 |
| 6: Settings 비조작 영역 | SettingsPageLayoutStyler/테마의 런타임 덮어쓰기, 설정 변경 즉시 저장 여부, 저장·취소·검증 이벤트 | 전체 스타일러/기존 기능을 끄거나 운영 설정·IO·축/통신에 영향을 줌 |
| 7: Recipe/Material | 변경 중인 Designer 보호, 레시피/Material 키·단위·값/dirty 판단·저장 취소 보존. 테스트 데이터의 격리 확인 | 활성 레시피/생산 상태/맵 데이터를 시험 변경하거나 시퀀스 영향 분석 없이 저장 |
| 8: WorkInfo/Work·셸·조작 | 상태 갱신·다국어·권한, Action/Sidebar/BottomMenu의 특수 기능, 기존 WaferMap 의미색·선택/현재 다이·픽커 보유/안착, 타이머/비동기/부하 | 상태색을 장식용 Role로 치환, 기존 팔레트 복제, 명령·인터락·시퀀스 수정/실행 필요 |

## 단계 종료 보고 및 다음 AI 인계

- [x] R01: 허용 목록 외 코드/설정/리소스가 바뀌지 않았음을 diff/해시로 확인. 다른 작업 변경은 별도 표시하고 보존.
- [x] R02: 실제 변경·삭제 파일, 공용 사용처, 보존한 기능, 검사 ID별 결과와 미검증 범위를 보고.
- [x] R03: Designer/빌드/기능/성능 중 필수 실패가 있으면 종료 판정을 보류하고 다음 화면 적용을 중단. 예외 승인 없는 미검증도 확대 금지.
- [x] R04: 실장비·시뮬레이터 실행과 사용자 수동 확인 여부를 AI 자동 검사 결과와 구분.
- [ ] R05: 이번에 만든 임시 파일만 정확한 경로로 정리. 검증 수치·방법·판정은 아래 표에 남김. 원본·운영·사용자 파일이나 기존 bin/obj를 임의 삭제하지 않음.
- [x] R06: 다음 AI가 수행할 단계 하나와 필요한 승인/검사만 명시. 결과 없는 체크를 완료 처리하지 않음.

| 검사 ID / 날짜 | 변경 전 | 변경 후 | 상태 | 방법·근거·제한·승인 내용 |
| --- | --- | --- | --- | --- |
| G00~G03 / 2026-09-27 | 현재 원본 | 변경 없음 | 통과 | 루트/master/dirty/1,222개 해시, 기존 공용 참조와 세 버튼 계약 확인. 상세 기준 아래 기록 |
| G04~G05 / 2026-09-27 | 현재 정적 속성 및 격리 배치 | 변경 없음 | 부분 확인 | 소스 배치·색상 writer 기록. 운영 셸/DPI/실제 hover·disabled 픽셀은 미검증 |
| G06 / 2026-09-27 | 원본 EventLog 격리 51개 및 3회 측정 | 미실시 | 부분 확인 | 합성 데이터/외부 의존 대체. 실제 CSV/OS 선택기/8개 프리셋 통합/Auto와 전후 비교 없음 |
| G07 / 2026-09-27 | 클릭 Role 제한·테두리 순서·중복 연결·대량 UI 지연 | 변경 없음 | 확인 완료 | 기존 위험으로 구분하고 임의 수정하지 않음 |
| C01 / 2026-09-27 | 원본 Common | 변경 없음 | 통과 | 기존 51개 검사를 현재 Common 소스로 새로 컴파일·실행, 모두 통과 |
| C02 / 2026-09-27 | Native 및 4개 Role + 원본 Auditor | 변경 없음 | 격리 통과 / 통합 미검증 | 시험 Click 처리 각 1회, 실제 Timer 원색 복원. Common으로 바뀐 실제 EventLog 업무 경로는 아직 없음 |
| C03 / 2026-09-27 | 220ms 안팎 6회 연타·3버튼 교대 12회 | 변경 없음 | 부분 확인 | 호출 수·색상 복원 확인. 물리 hover/포커스 이동은 미검증 |
| C04 / 2026-09-27 | Primary 클릭 후 Dark로 변경 | 변경 없음 | 실패 / 제한 재현 | 예상 #404040/White, 실제 #D97706/Black. Reset은 별도 검사이며 실패를 숨기는 조치가 아님. 예외 승인 없음 |
| C05 / 2026-09-27 | 기본 입력 속성·disabled PerformClick | 변경 없음 | 부분 확인 | Enabled=false 기능 미실행. 실제 마우스/Space/Tab/Shift+Tab/Enter 미검증 |
| C06 / 2026-09-27 | Enabled/Hide/Dispose 및 재연결 | 변경 없음 | 부분 확인 / 기존 제한 재현 | 한 번 연결한 격리 수명 검사는 정상. EnsureFeedback을 두 번 호출하면 피드백 핸들러가 중복. 실제 PageBase 재진입 미검증 |
| C07 / 2026-09-27 | 메모리 CodeDOM 저장·복원 | 변경 없음 | 실패 / 제한 재현 | 역할 기본색과 일반 개별색은 보존. Dark에 개별 Gray/1px 지정 시 복원 결과 Dark/0px로 달라짐. 실제 VS 저장 미실시 |
| 현재 소스 Build / 2026-09-27 | Debug/Any CPU | 운영 변경 없음 | 통과 | 명시적 안전 OutDir, 종료 0 / 오류 0 / 경고 41. 단계2 B02의 대체 통과 아님 |
| A/D/F/P / 2026-09-27 | 기존 Button | 미적용 | 미실시 / 보류 | 2단계 구현·실제 Designer·운영 기능/성능 회귀는 승인 후 수행. 예외 승인 없이 확대 금지 |
| R01~R04, R06 / 2026-09-27 | 시작 기준 1,222개 | 체크리스트만 변경 | 사전검증 보고 완료 / 적용 보류 | 운영 소스·기존 변경 보존. 실제 입력/Designer/실장비 미검증 구분. 제한 적용도 승인 대기 |
| R05 / 2026-09-27 | 새 격리 산출물 | 남아 있음 | 정리 미완료 | 생성 바이너리만 정리하는 명령이 실행 환경 정책으로 시작 전에 차단됨. 우회하지 않았으며 파일 삭제 없음 |
| A01~A04 / 2026-09-28 | 기존 Button 3개 | Common 고정 Role 3개 | 통과 | 각 버튼 순차 적용. Designer만 9행 추가·21행 삭제. 허용 타입/외관 행을 제외한 전체 텍스트 동일 |
| C02~C03, C05~C06 / 2026-09-28 | 사전검증 | 원본 PageBase/Auditor 연결 71개 통과 | 부분 확인 | 업무 핸들러/색 복원/연타/Off·On/20회 Visible 전환/Dispose 확인. 물리 입력·hover·운영 탭 재부착 미확인 |
| C04/C07 / 2026-09-28 | 알려진 실패/제한 | 공통 코드는 동일 | 제한 유지 / 범위 한정 승인 | 동적 Role·개별 테두리 예외를 사용하지 않는 EventLog 세 버튼에만 적용. 실패 항목을 통과 처리하지 않음 |
| D01~D05 / 2026-09-28 | 실제 Designer 미검증 | 자동 검증 중단 | 미검증 | 실행 중인 원본 VS 창을 식별했으나 활성화가 최초·재시도 모두 실패. Designer 편집/저장/F5 조작 없음 |
| D06/B01/B02 / 2026-09-28 | 원본 등록·인코딩 | 새 리소스/프로젝트 없음 | 통과 | diff/UTF-8 BOM/CRLF 확인, 안전 Build 종료 0 / 오류 0 / 경고 41 / 8.84초 |
| F02, F04~F10, F12 / 2026-09-28 | 194개 통과 | 218개 통과 | 격리 통과 / 운영 미검증 | 원본 업무 코드 직접 컴파일. 파일 응답·설정 등은 대체. 검사 수 증가는 버튼 타입/Role 기대값 추가이며 신규 기능 추가가 아님 |
| F01/F03/F11 / 2026-09-28 | 기본 속성·주입 경로 | 동일 보존 | 부분 확인 | PerformClick과 실제 이벤트 연결만 확인. OS 파일 선택/취소·물리 키보드/마우스·실제 번역/잘림은 미확인 |
| P01~P05 / 2026-09-28 | 시나리오별 18표본 | 동일 18표본 | 부분 측정 / 성능 동등 판정 보류 | 전→후 3쌍과 후→전 3쌍. 범위 겹침과 혼합 결과이나 500/2000행 일부 중앙값 증가가 남음. 원인 단정·통과 처리 금지 |
| P06/R03~R04/R06 / 2026-09-28 | 실장비 미실시 | 동일 | 운영 확대 보류 | 실장비/시뮬레이터/운영 데이터 변경 없음. 다음은 2단계 실제 Designer·수동 기능 확인이며 3단계 자동 적용 아님 |
| R01~R02 / 2026-09-28 | 시작 시 1,222개 파일 SHA-256 | Designer + 이 체크리스트만 변경 | 통과 | 나머지 1,220개 동일, 보호 대상 추가/삭제 0. ignored 검사 자료는 별도 기록. 기존 22개 tracked 변경과 기존 untracked 소스 보존 |

## 2026-09-27 사전검증 상세 기록 — 운영 소스 무수정

### 0단계: 기존 구조와 보존 기준

기존 `UiStandardButton : Button`과 `UiStandardPalette`를 재사용한다. 실제 사용은 기존 카탈로그 활성 버튼 12개와 템플릿 2개이며, EventLogPage에는 아직 적용되지 않았다. Common 참조·Compile 등록이 이미 있어 새 프로젝트/공용 클래스/프로젝트 등록은 필요 없다. `UiTheme`의 기존 접근점과 공용 배경 참조 3개도 유지한다. ActionButton/SidebarButton/BottomMenuButton은 별도 기능 계약이므로 중복으로 삭제하지 않는다.

대상 근거: `QMC.CDT-320/Ui/Pages/History/EventLogPage.Designer.cs` 23~25, 49~51, 126~128, 275~337, 448~450행. 행 번호는 이번 읽기 시점 기준이다.

| 보존 속성 | btnRefresh | btnOpenFile | btnLive |
| --- | --- | --- | --- |
| 현재 타입 / 문구 | Button / REFRESH | Button / OPEN FILE | Button / 실시간 |
| 업무 Click / 직접 연결 수 | btnRefresh_Click / 1 | btnOpenFile_Click / 1 | btnLive_Click / 1 |
| TabIndex / Tag | 12 / 없음 | 13 / 없음 | 14 / 없음 |
| Designer Size | 114×24 | 128×24 | 88×24 |
| MinimumSize | 116×24 | 128×24 | 명시 없음 |
| Designer Location | 1416,8 | 1538,8 | 명시 없음·TableLayout 배치 |
| TableLayout 셀 | 10,0 | 11,0 | 12,0 |
| Padding | 8,0,8,0 | 8,0,8,0 | 명시 없음 |

공통 보존값: Anchor=Left/Right, Margin=4,0,4,0, 맑은 고딕 10pt Bold, FlatStyle=Flat, Cursor=Hand, UseVisualStyleBackColor=false. Dock/MaximumSize/TabStop/DialogResult/UseMnemonic/CausesValidation은 기존 기본값을 유지한다. 세 버튼에는 번역/권한 Tag가 없으며 새로 추가하지 않는다. `SetFilterUiEnabled`(EventLogPage.cs:184)의 `FileLogHistoryEnabled` 조건을 보존한다. History 진입 권한은 Operator이며, TabBase의 Tag 기반 권한·번역 적용 정책도 변경하지 않는다.

rootLayout은 1열/3행(30px, 40px, 나머지), filterLayout은 13열/1행이다. 열은 순서대로 66/170/76/220/76/150/82px, 가변 100%, 104/76/122/136/96px이고 Padding=8,3,8,3이다. 자식 순서·행열·Dock=Fill과 페이지 1678×900은 보존한다. 페이지와 PageBase에 AutoScaleMode/Dimensions 명시가 없으므로 새 DPI 설정을 넣지 않는다. REFRESH의 Size와 MinimumSize 차이도 기존 값이며 수정 대상이 아니다.

숨김 격리 페이지(96 DPI, PageBase 대체)의 레이아웃 결과는 REFRESH=(1320,8,116,24), OPEN FILE=(1442,8,128,24), 실시간=(1578,8,88,24)였다. 이는 운영 셸/실제 PageBase/DPI에서의 화면 실측을 대신하지 않는다. Designer 소스 좌표와 실제 레이아웃 결과를 혼동하지 않는다. 원본 resx는 스키마/헤더뿐이며 버튼 리소스는 없다.

| 현재 색상 속성 | REFRESH / 실시간 | OPEN FILE |
| --- | --- | --- |
| 기본 배경 / 글자 | #34495E / White | #546E7A / White |
| hover / down | #455A71 / #28394A | #65808C / #425A65 |
| 테두리 | 0px | 0px |
| 클릭 플래시 배경 / 글자 | #FFF19C / #333333 | 동일 |

정적 색상은 Designer가 지정한다. PageBase.OnLoad → UiClickAuditor.EnsureFeedback은 기존 업무 이벤트 뒤에 시각 피드백을 추가한다. 원본 Auditor는 임시색을 대입하고 UI Timer 220ms 후 캡처한 색으로 복원한다. Disabled 전용색 writer는 없고 네이티브 Button이 그리므로 실제 disabled 글자 픽셀은 미검증이다. 해당 경로에서 SettingsPageLayoutStyler 직접 호출과 실행 중 Role 변경은 발견되지 않았다.

HistoryTab.cs:19~27의 Event, Warning, Data, Work, InputSeq, FrontHeadSeq, RearHeadSeq, OutputSeq 8개는 모두 같은 Designer의 영향을 받는다. AlarmHistoryPage와 MessageEditPage는 별개다. 8개 운영 화면의 실제 진입 검사는 수행하지 않았다.

### 기존 EventLogPage 기능·성능 기준의 한계

원본 EventLogPage.cs/Designer.cs/EventLogDisplayBuffer.cs 및 EventRow/EventKind를 직접 컴파일한 숨김 STA 검사에서 **이번 실행 51개 통과, 실패 0개**였다. 이는 기존 Common 버튼 51개 검사와 별개다. PageBase의 가시성/디자인모드, 설정 저장소, EventLogger의 메모리·파일 응답, 번역/메시지 카탈로그·대화상자만 테스트 대체했다. 원본 페이지 이벤트와 표시 버퍼는 교체하지 않았다. 운영 어셈블리/CSV/장비/실제 OS 파일 선택기는 실행하지 않았다.

확인 범위: 기존 Button 3개와 업무 Click 각 1회, REFRESH의 파일 스냅샷/라이브 미혼합, 선택 경로가 주어진 상태에서 재조회, 실시간 전환 시 경로 해제/오늘 날짜, 표시 설정 Off/On과 disabled PerformClick 차단, 500/2000/ALL=10000 상한, 대소문자 검색, 늦은 대체 파일 결과 폐기, 1100행 입력 후 대기 1000행 상한/최신 500행 복구, Dispose 구독 해제. 선택 경로는 하네스에서 주입했으므로 OPEN FILE의 실제 선택/취소 통과를 뜻하지 않는다.

성능 조건: 같은 프로세스/PC, 숨김 1678×900·96 DPI, CLR 4.0.30319.42000·64bit, ALL/최근 1시간 켬, 현재 시각의 합성 Event 행, 절반이 `needle` 검색에 일치. 준비 실행 후 각 행 수에서 조회 → 50% 검색을 3회 측정했다. 소스 변경 전 수치만 있으며 전후 회귀 판정은 없다. 기록기·번역 대체 및 숨김 렌더링 때문에 운영 성능 절대값으로 사용할 수 없다.

| 원본 데이터 행 수 | 메모리 조회 완료 ms (1/2/3회) | 50% 검색 완료 ms (1/2/3회) | 조회 중 예약 UI 콜백 지연 ms (1/2/3회) |
| --- | --- | --- | --- |
| 0 | 2.265 / 0.287 / 0.203 | 0.377 / 0.230 / 0.212 | 2.253 / 0.272 / 0.197 |
| 500 | 12.153 / 15.314 / 14.280 | 7.133 / 8.473 / 8.404 | 12.122 / 15.308 / 14.273 |
| 2000 | 57.506 / 46.786 / 47.284 | 23.565 / 22.074 / 30.270 | 57.499 / 46.632 / 47.275 |
| 10000 | 279.497 / 555.375 / 646.577 | 156.232 / 231.150 / 278.163 | 279.429 / 555.325 / 646.038 |

**기존 위험:** 10,000행에서 원본 UI 스레드의 일괄 그리드 생성/갱신 때문에 예약된 UI 콜백도 약 0.28~0.65초 대기했다. 버튼 변경으로 생긴 회귀가 아니며 이번에 수정하지 않았다. ‘프리징/성능 문제 없음’으로 판정할 수 없다. 3회 값의 변동도 크므로 단계2 전후에는 동일 하네스 조건으로 다시 측정해야 한다.

추가 계측: 10,000행 조회 CPU 누적 시간 증분=281.250/562.500/671.875ms(사용률/최대값 아님), 검색=156.250/218.750/265.625ms. 프로세스 핸들은 해당 조회/검색 동안 353→353. 측정 구간 전체 private bytes는 약 92~183MB로 GC에 따라 오르내렸다. 이 값만으로 누수가 없다고 결론내리지 않는다. UI 입력의 p95, CPU 평균/최대 사용률, 실제 큰 CSV, 20회 화면 재진입 안정화, 실장비 Auto/시퀀스 주기는 미검증이다.

### 1단계: 원본 Common + 원본 Auditor 검사

기존 `_codex_verify_ui_common_20260922/CommonButtonChecks.cs`를 현재 Common 소스에 직접 연결해 다시 컴파일·실행했다(51개 통과). 추가 검사는 원본 UiStandardButton/UiStandardPalette/UiClickAuditor를 직접 컴파일했다. EventLogger.Write와 UserSession, 사용하지 않는 ActionButton/SidebarButton 타입만 대체했다. 원본 클릭 효과와 WinForms Timer는 대체하지 않았다. PageBase.OnLoad의 연결 경로는 소스로 확인했으며 실제 PageBase/운영 셸을 실행하지 않았다.

추가 하네스 결과의 `passed=80, limitations=7, unexpected_failures=0`은 **제한을 정확히 재현한 assertion과 시스템 어셈블리 확인도 포함한 수치**다. 전체 호환성 80개 통과를 뜻하지 않는다. limitations 7회는 중복 연결 5종(Native/4 Role), C04, C07의 세 종류 제한이다.

- Native와 Default/Primary/Dark/Danger 각각: 한 번 연결 시 시험 업무 1개 + Auditor 1개, 단일 클릭 1회 및 원색 복원. 최종 실행 복원 시각은 순서대로 237/233/251/252/236ms였다. 이는 UI Timer 실측이며 220ms 정밀 보장이 아니다.
- 각 타입에서 220ms 안팎 6회 연타 후 업무 호출 6회 및 색상 복원. Common 3개 교대 12회에서도 각 4회 호출하고 원색으로 복원했다.
- 플래시 중 Enabled=false 이후 PerformClick은 실행되지 않고 색상만 복원됐다. 한 번 연결한 Panel을 Hide/재표시해도 핸들러 수가 같았으며, Dispose 뒤 Tick 처리 후 Auditor의 진행 목록은 0개였다. 내부 catch가 있는 원본이므로 모든 내부 예외 부재나 OS Timer 누수 부재를 이 검사만으로 보증하지 않는다.
- **C04 실패:** Primary 클릭이 끝난 뒤 Role=Dark를 대입하면 Back/Fore가 #D97706/Black에 남는다(예상 #404040/White). Auditor의 임시 대입이 개별색 플래그를 남기기 때문이다. 별도 Reset 후 Role 변경은 정상이고, 의도적으로 지정한 Navy/Lime은 클릭·Role 변경 후 유지됐다. 운영 코드에 Reset 우회는 추가하지 않았다.
- **C06 기존 제한:** EnsureFeedback을 같은 루트에 두 번 부르면 Native/Common 모두 총 Click 핸들러 2→3이다. 시험 업무 자체는 1회 실행되지만 시각 피드백은 중복 연결된다. 현재 PageBase의 기존 OnLoad 호출 외에 새 연결을 만들지 않는다. 실제 재진입과 핸들 재생성은 다음 통합 검사에서 확인한다.
- **C07 실패:** 메모리 DesignSurface/CodeDOM 저장·복원에서 4개 Role 기본 RGB는 고정 저장되지 않고 정상 복원됐다. Navy/Gold, Magenta 3px, Aqua hover/Yellow down의 일반 개별값도 복원됐다. 그러나 Dark 버튼에 명시한 Gray 1px 테두리는 저장 순서(FlatAppearance → Role) 때문에 복원 후 #404040/0px가 됐다. ‘개별 테두리도 언제나 보존’으로 보증할 수 없다. 원본 Designer 파일은 저장하지 않았다.

EventLog 세 버튼은 런타임 Role 전환도, 승인안에서 개별 테두리 예외도 사용하지 않는다. 따라서 **역할을 고정하고 공용 기본 테두리만 사용하는 제한 적용 후보**이지, Common 전체 호환성 완료가 아니다. C04/C07 실패는 그대로 남기며, 동적 상태 버튼·임의 개별 테두리 화면으로 확대하지 않는다.

### 현재 소스 Build 및 기존 주의사항

다음은 이번 사전검증의 현재 소스 빌드이며, 공용 버튼을 적용한 뒤의 B02 통과가 아니다.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\00.Source\CDT-320_New\QMC.CDT-320.sln' /t:Build `
  /p:Configuration=Debug '/p:Platform=Any CPU' `
  '/p:OutDir=D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260927\build\out\' `
  /m:1 /v:minimal /nologo
```

결과: 종료 코드 0, 오류 0개, 경고 41개, 빌드 경과 9.93초. Clean/Rebuild/F5/운영 EXE 실행은 하지 않았다. 경고는 현재 소스의 기준으로 기록했으며 수정하지 않았다. TapeFrameSubsetPage.MapProcessing 등록은 여전히 SubType=UserControl이지만 중복 InitializeComponent/MapProcessing.resx는 발견되지 않았고 이번 Build에서 MSB3577도 발생하지 않았다. 이 등록은 대상 밖이므로 변경하지 않았다.

### 승인 후 필요한 최소 수정 범위

고정 Role 세 버튼의 제한 적용을 별도로 승인받는 경우, 운영 수정 파일은 `QMC.CDT-320/Ui/Pages/History/EventLogPage.Designer.cs` **하나**다.

| 대상 | 최소 변경 | 공용 기본색 / 테두리 |
| --- | --- | --- |
| btnRefresh | 필드·생성 타입을 UiStandardButton, Role=Primary | #D97706 / Black, 0px |
| btnOpenFile | 필드·생성 타입을 UiStandardButton, Role=Default(기본값) | White / Black, #808080 1px |
| btnLive | 필드·생성 타입을 UiStandardButton, Role=Dark | #404040 / White, 0px |

세 버튼의 기존 BackColor/ForeColor 및 hover/down 명시 RGB를 제거해 역할 기본색을 가리지 않게 한다. BorderSize도 역할 기본값을 사용하며, 특히 OPEN FILE의 기존 0px 지정을 남겨 Default의 1px을 가리지 않도록 한다. FlatStyle=Flat와 UseVisualStyleBackColor=false는 유지한다. 기존 노란 클릭 피드백은 유지되며 hover/down은 네이티브 Flat 기본 피드백을 실제 화면에서 확인해야 한다. Designer 안에 스타일 함수를 추가하거나 RGB를 다시 하드코딩하지 않는다.

업무 .cs/resx, Common 버튼·팔레트, Auditor/PageBase/TabBase/HistoryTab, 로그 기록기/표시 버퍼, 프로젝트·테마·설정·인터락·시퀀스는 수정하지 않는다. 기존 이름/문구/이벤트/TabIndex 12·13·14/크기·최소크기/폰트/배치/Enabled 조건을 그대로 보존한다. S/M/L 맞춤 재배치는 하지 않는다. 제한 적용이 승인되기 전에는 이 최소안도 구현하지 않는다.

### 보호 확인·산출물·다음 인계

시작 시 보호한 tracked/untracked 1,222개 파일을 SHA-256으로 비교해 이 체크리스트 하나만 변경된 것을 확인했다. 운영 소스 변경/새 공용 구현/프로젝트 추가/롤백/운영 데이터 변경/실장비·시뮬레이터 실행은 없다. 기존 22개 tracked 변경 및 기존 untracked는 그대로다.

비교·재현용 소스/스크립트/로그는 ignored 폴더 `D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260927`에 있다. 운영 소스 복사본이나 별도 GUI 프로젝트가 아니다.

- `build/solution-build.log`: 이번 원본 솔루션 Build 근거.
- `button-checks/ButtonAuditorChecks.cs`, `run-button-checks.ps1`, `button-checks.log`: 실제 Common/Auditor 재현. 기존 CommonButtonChecks.cs는 기존 위치의 원본 테스트를 참조한다.
- `eventlog-baseline/EventLogBaseline.cs`, `run-eventlog-baseline.ps1`, `eventlog-baseline.log`: 현재 Button 페이지의 합성 데이터 기준과 24개 성능 표본. 스크립트는 실행 명령 재현을 위해 검사 후 추가했으며 자체 재실행은 하지 않았다.

위 7개 비교 자료 외에 `build/out`의 Build 출력 11개 및 격리 검사 EXE 3개가 남아 있다. 정확한 신규 경로만 삭제하려 했으나 실행 환경이 정리 명령을 시작 전에 차단했다. 우회 삭제하지 않았으며 R05는 미완료다. 기존 검증 폴더·bin/obj·배포 경로는 정리하지 않았다. 다음 실행자는 비교 목적이 끝난 생성물만 정확한 경로를 확인해 정리한다.

2단계 적용 후에는 EventLog 하네스의 기본 Button 기대값 3개를 Common 타입으로 바꾸고 컴파일 입력에 현재 Common 소스 2개를 추가해야 한다. 원본 페이지는 복사하지 않는다. 시험 코드 조정과 운영 코드 변경을 구분하고 행 수/필터/측정 순서 등 기준 조건을 유지한다.

다음 제안은 **2단계의 고정 Role·기본 테두리 세 버튼에만 제한 적용** 한 가지다. C04/C07 제한을 수용하는 해당 화면 한정 승인과 실제 Designer/입력/8개 프리셋/성능 회귀 검사가 필요하다. 기존 대량 데이터 UI 지연을 허용 가능한 성능으로 간주하는 승인이 아니다. 공통 제한이나 기존 성능 문제를 먼저 고치려면 별도 파일·영향 범위 분석과 승인을 받아야 하며 이번 범위에서 자동 수정하지 않는다.

## 2026-09-28 2단계 제한 적용 기록

### 구현과 독립 검토

사용자가 0~1단계 결과와 제한 적용 설명 후 “기능상 문제없게 적용해줘.”라고 승인했다. 기존 Common 구현을 재사용했고, 새 공용 타입·프로젝트·DesignerCheck·Preview를 만들지 않았다. C04/C07 실패는 해결된 것이 아니며 **런타임 Role 변경이 없는 EventLog 세 버튼 + 역할 기본 테두리**만 이번 예외 승인 범위다.

운영 수정 파일은 `QMC.CDT-320/Ui/Pages/History/EventLogPage.Designer.cs` 하나다. 필드 3개와 생성 3개를 `QMC.Common.Ui.Controls.UiStandardButton`으로 변경하고 Primary/Default/Dark를 각각 지정했다. 기존 BackColor/ForeColor/MouseOverBackColor/MouseDownBackColor/BorderSize의 역할 충돌 지정만 제거했다. diff는 9행 추가·21행 삭제다. 스타일 함수·런타임 일괄 색상 대입은 추가하지 않았다.

독립 읽기 전용 리뷰와 주 검토에서 허용 타입/Role/외관 행을 제외한 **Designer 전체 텍스트가 변경 전후 동일함**을 확인했다. 업무 Click은 각각 1회 그대로이며 이름·문구·TabIndex·크기·최소크기·폰트·셀·자식 순서·배치를 보존했다. 업무 .cs/resx, Common, PageBase/Auditor/Theme, 기록기/버퍼, csproj, 시퀀스/인터락은 수정하지 않았다. 새 코드 결함은 발견하지 못했지만 실제 화면 검증을 대체하지 않는다.

Designer SHA-256:

- 변경 전: `926AD0E068489A0B68D236D76611E2DD54E95F18E3965F6AE1F6C5389C14297C`
- 변경 후: `0AB84EC3557A617942D59300A1A41CADCA52D88C3207AF55C20E62B9F8A17D94`

### 순차 기능 검사

기존 격리 하네스에 현재 원본을 직접 연결했으며 운영 소스 복사·롤백 없이 변경 전 기준 바이너리를 먼저 만들었다. 기록기 응답/설정/PageBase 가시성/번역/대화상자는 대체하지만 EventLogPage 업무 코드와 Designer, 표시 버퍼, EventRow/EventKind, Common 버튼/팔레트는 원본이다. 선택 경로는 시험 주입이며 실제 OS 파일 선택기나 CSV 파서를 검증한 것이 아니다.

| 적용 시점 | 통과 / 실패 | 증거 로그 (`_codex_verify_ui_rollout_20260928/eventlog/`) |
| --- | --- | --- |
| 변경 전 기존 Button 3개 | 194 / 0 | `before-20260928-200231-129.log` |
| REFRESH만 적용 | 202 / 0 | `refresh-20260928-200351-029.log` |
| REFRESH + OPEN FILE 적용 | 210 / 0 | `refresh-open-20260928-200438-530.log` |
| 실시간까지 3개 적용 | 218 / 0 | `after-20260928-200545-079.log` |

동일 기능 검사에 각 프리셋의 공용 Role 기대값을 추가했기 때문에 적용 버튼마다 assertion이 8개 증가한다. 원본 기능 자체를 추가한 것이 아니다. 조회 스냅샷/실시간 미혼합, 선택 경로 재조회, 실시간 복귀, 표시 설정 Off/On, 500/2000/ALL 상한, Source/RunId/검색/최근 1시간, 행 순서·시간 열 선택, 날짜 변경 후 이전 캐시 폐기, 합성 빈 결과/예외 후 복귀, 늦은 결과·숨김·Dispose, burst 1100행의 대기 1000행/최신 500행 복구, 가시성 플래그 20회 전환, 8개 프리셋을 검사했다.

같은 숨김 96 DPI 조건의 실제 레이아웃 값은 전후 모두 REFRESH=(1320,8,116,24), OPEN FILE=(1442,8,128,24), 실시간=(1578,8,88,24)였다. 운영 셸의 DPI·실제 잘림은 미확인이다.

### 원본 PageBase + 클릭 효과 통합 검사

추가 하네스는 **원본 PageBase/EventLogPage/UiClickAuditor/UiTheme/UiDoubleBuffer/Common/버퍼/행 타입을 직접 컴파일**했다. 설정, 기록기, 번역/메시지 카탈로그, UserSession, 사용하지 않는 버튼·시퀀스 enum 타입과 대화상자만 대체했다. 운영 어셈블리를 로드하거나 Form.Show/장비 실행을 하지 않았다.

`integration/after-20260928-200630-262.log`: **71개 통과 / 실패 0 / 종료 0**.

- 실제 PageBase.OnLoad에서 Auditor 연결 1회. 세 버튼마다 업무 핸들러 1개 + 피드백 핸들러 1개.
- REFRESH 원본 핸들러의 합성 파일 조회 1회, 실시간 원본 핸들러의 메모리 복귀 정상.
- 클릭 효과 복원 235/253/238ms(REFRESH/OPEN FILE/실시간). 복원색은 Primary #D97706/Black, Default White/Black, Dark #404040/White.
- 각 6회 연타·교대 12회에서 고착 없음. Off 중 PerformClick·조회 차단, On 복구 정상.
- **부모 없는 원본 UserControl의 실제 Visible 20회 전환**에서 Load=1/Audit=1 유지, 구독 0↔1·타이머 중지↔시작. 관찰자 추가 후 각 핸들러는 3개로 유지됐다. 이는 운영 HistoryTab의 재부착이나 핸들 재생성을 보장하지 않는다.
- 파일 조회/클릭 효과 진행 중 Dispose 후 구독·비동기 작업·피드백 추적 정리 확인.

OPEN FILE은 원본 핸들러를 교체하지 않고 시험 Site.DesignMode=true로 **원본 디자인 모드 가드**를 활성화해 선택기 진입을 막았다. 이벤트 연결·피드백만 통과한 것이며 실제 선택/취소 기능 통과가 아니다. 기존 EnsureFeedback 직접 중복 호출 위험도 수정하거나 해결하지 않았다.

### 변경 전후 성능 비교 — 동등 판정 보류

변경 전에 만든 before 바이너리와 최종 after 바이너리를 다시 컴파일하지 않고 사용했다. 같은 PC·숨김 페이지 1678×900·96 DPI·CLR4/64bit, ALL/최근 1시간, 같은 합성 입력 생성 및 필터 순서에서 **전→후 3쌍, 후→전 3쌍**을 실행했다. 각 프로세스의 준비 실행 후 시나리오별 3표본, 전후 각 6프로세스·18표본이다. 측정 동안 다른 검사/Build를 함께 실행하지 않았으나 PC의 다른 앱·OS 부하를 완전히 통제한 실험은 아니다.

12개 실행 모두 기능 검사는 before=194/0, after=218/0이었다. 원본 소스 롤백이나 before 소스 복사 없이 이전에 컴파일한 격리 검사 바이너리만 사용했다.

| 원본 행 수 / 시나리오 | 변경 전 완료 ms 중앙값 [최소~최대] | 변경 후 완료 ms 중앙값 [최소~최대] | 예약 UI 콜백 p95 ms 전→후 |
| --- | --- | --- | --- |
| 0 / 메모리 조회 | 1.195 [0.285~3.768] | 1.311 [0.212~5.936] | 3.747→5.274 |
| 0 / 50% 검색 | 0.676 [0.242~1.382] | 0.726 [0.232~2.128] | 3.156→2.567 |
| 500 / 메모리 조회 | 16.237 [14.002~25.956] | 19.110 [13.340~27.620] | 25.946→27.603 |
| 500 / 50% 검색 | 9.183 [7.632~16.110] | 14.369 [7.699~17.710] | 16.321→17.929 |
| 2000 / 메모리 조회 | 56.115 [47.841~84.077] | 63.331 [49.397~86.075] | 84.029→86.028 |
| 2000 / 50% 검색 | 30.117 [22.655~54.300] | 34.895 [21.858~48.176] | 55.186→49.805 |
| 10000 / 메모리 조회 | 432.710 [303.758~529.670] | 387.266 [291.421~452.928] | 528.591→452.892 |
| 10000 / 50% 검색 | 198.355 [142.746~279.665] | 200.979 [155.194~345.660] | 279.799→345.834 |

p95는 nearest-rank로 계산해 표본 18개에서는 관측 최댓값과 같다. **물리 입력의 응답 p95가 아니라 BeginInvoke 예약 콜백 지연**이다. 10000행 예약 콜백 중앙값은 조회 432.597→385.390ms, 검색 198.474→201.144ms였다. 기존 UI 스레드 대량 그리드 생성·갱신 지연이 남아 있으므로 프리징 부재를 보장하지 않는다.

500행 조회의 프로세스별 중앙값은 정순 before=14.868/21.625/16.202, after=24.004/15.694/27.107ms였고 역순 before=15.583/22.738/16.539, after=14.887/26.797/14.954ms였다. 범위가 겹치고 빠르거나 느린 결과가 섞여 있으나 일부 조회/검색의 전체 중앙값 증가가 남는다. **버튼 변경 때문이라고 단정하지도, 성능 저하가 없다고 통과 처리하지도 않는다. P01~P05 전체 완료 및 운영 확대는 보류한다.**

CPU는 사용률이 아닌 프로세스 누적 CPU 시간 증분이다. 500/2000행 조회 중앙값은 각각 전후 15.625/62.500ms로 같았고, 10000행 조회는 414.063→375.000ms, 검색은 179.688→195.313ms였다. CPU 평균·최대 사용률은 미측정이다. 20회 가시성 플래그 전환에서 최종 구독/타이머=1/1이며, 같은 프로세스의 전환 전후 핸들은 증가하지 않았다(11회 동일, 1회 390→386). GC 후 private bytes는 전 약 43.1~45.0MB, 후 약 43.9~45.0MB였지만 짧은 격리 검사이므로 장시간 누수 부재를 뜻하지 않는다. 실제 파일 I/O·초기 진입·그리기·입력·운영 셸·Auto/시퀀스 주기는 미검증이다.

증거: `eventlog/compare-{1,2,3}-{before,after}.log`, `eventlog/reverse-{1,2,3}-{before,after}.log`, `eventlog/comparison-summary.json`. 주 검토자가 12개 원시 로그의 18표본·완료 시간·예약 콜백을 별도 집계해 요약과 일치함을 확인했다.

### 안전 Build와 실제 Designer 제한

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\00.Source\CDT-320_New\QMC.CDT-320.sln' /t:Build `
  /p:Configuration=Debug '/p:Platform=Any CPU' `
  '/p:OutDir=D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260928\build\out\' `
  /m:1 /v:minimal /nologo /fl `
  '/flp:LogFile=D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260928\build\solution-build.log;Verbosity=normal'
```

결과: 종료 0, 오류 0, 경고 41, 8.84초. 기존 경고는 수정하지 않았다. Clean/Rebuild/운영 EXE/F5/시뮬레이터 실행·배포 경로 변경은 없다.

computer-use 스킬로 현재 실행 중인 VS 18 Community 창과 원본 루트/master를 확인했다. 창 활성화가 최초와 허용된 재시도 모두 `failed to activate captured window`로 실패해 안전 절차에 따라 중단했다. EventLog Designer 편집·저장·재열기를 하지 않았고 우회 자동화를 시도하지 않았다. **D01~D05는 미검증이지 통과가 아니다.** 기존 열린 Form1 작업도 변경하지 않았다.

다음 실행자는 EventLogPage.cs의 Designer를 열어 세 Common 버튼 선택/Role·색상 확인 → 승인된 세 버튼 속성 저장 → 닫기/재열기를 확인한다. 원래 없던 시험값이나 다른 배치 변경을 저장하지 않는다. Default Role 행은 기본값이라 저장에서 생략될 수 있으므로 행 유무가 아니라 재열기 후 White/Black·Gray 1px 유지로 판정한다. 대상 밖 diff가 생기면 무단 원복하지 않고 보고한다.

### 산출물과 남은 관문

최종 1,222개 tracked/untracked 보호 파일의 SHA-256 비교에서 EventLogPage.Designer.cs와 이 체크리스트만 변경됐고, 나머지 1,220개는 동일했다. 보호 파일의 추가·삭제는 없다. ignored 검사 자료는 이 비교 밖이며 아래에 따로 기록한다. 기존 사용자 변경은 보존했다.

격리 검사용 기존 `20260927/eventlog-baseline/EventLogBaseline.cs`와 `run-eventlog-baseline.ps1`만 범위를 확장했고 출력은 `20260928/eventlog`로 분리했다. 새 `20260928/integration`의 소스 2개·스크립트 1개는 원본 직접 컴파일 검사용이며 운영 프로젝트에 등록하지 않았다. 이 자료와 안전 Build 출력은 미완료 Designer/현장 확인의 재현 자료로 유지한다. 삭제는 없으며 R05는 미완료다.

다음 작업은 새 화면 확대가 아니라 **이 2단계의 실제 Designer 및 수동 회귀 확인**이다. REFRESH, OPEN FILE 선택/취소, 실시간 복귀, 물리 마우스/키보드, 표시 설정, 실제 History 8개 탭의 진입·언어·잘림을 확인한다. 실제 운영 설정/데이터 저장이나 장비 Auto 부하 시험은 별도 승인 및 안전 상태 확인 없이 실행하지 않는다. 기존 대량 그리드 지연·C04/C07 제한 개선은 별도 분석/승인 대상으로 남긴다.

## 2026-09-29 추가 요청 — 조회 기본값 및 알람 날짜별 이력

### EventLog 최근 1시간 기본값

사용자 화면의 `LAST-1HOUR-FILTER` 안내와 직접 선택한 과거 CSV가 빈 화면이 되는 조건을 소스로 확인했고, 사용자가 체크 해제 후 정상 조회를 확인했다. 별도 승인에 따라 EventLogPage.Designer.cs의 `Checked=false`, `CheckState=Unchecked` 두 초기값만 변경했다. 사용자가 다시 체크하는 기능과 조회 로직은 유지한다. 당시 안전 Build는 오류 0/기존 경고 41개였다. 9월 28일 성능 수치는 당시 필터 조건의 과거 기록이지 변경된 기본값에 대한 재측정 결과가 아니다.

### 알람 날짜 조회의 범위와 사용법

사용자 요청: “알람도 날짜별로 검색해서 확인하게 코드 구현해줘. 알람페이지는 현재 조회가 안돼. 오늘 날짜 말고는.” 기준은 `D:\00.Source\CDT-320_New` / master다. 시작 시 AlarmHistoryPage.Designer.cs에 있던 이벤트 연결 위치 변경을 보존한다.

- 기존 필터 줄의 남는 오른쪽 공간에 Designer 컨트롤 `lblHistoryDate`, `dtpHistoryDate`, 공용 Primary `btnRefreshHistory`, `lblHistoryStatus`를 추가했다. 기존 열 0~5와 Clear 버튼은 그대로다. 새 배치·이벤트는 Designer에 인라인으로 작성했다.
- 날짜 선택 시 자동 조회, REFRESH 시 선택 날짜를 다시 읽는다. 오늘은 기존 메모리/실시간 경로, 다른 날짜는 `AlarmManager.Dir\alarm_yyyy-MM-dd.json`의 저장 이력이다. Severity·검색은 최신 500건의 화면 캐시에 적용하며 파일을 반복 읽지 않는다.
- 오늘 실시간 모드는 자정 및 숨김 후 재진입에도 오늘을 따라간다. 명시적으로 선택한 과거 날짜는 유지한다. 미래에 선택했던 날짜가 오늘이 되면 오늘 모드로 이어진다.
- 상태 표시로 오늘 실시간/저장 당시 상태/저장 이력 없음/읽는 중/조회 실패를 구분한다. 과거 파일의 미해제 색상은 **저장 당시 상태**이며 현재 장비의 활성 알람 판정이 아니다.

운영 수정은 3개 파일이다.

1. `QMC.Common/Alarms/AlarmManager.cs`: `ReadSavedHistory(date, maxRows)` 읽기 API만 추가. 기존 경로·DTO·ToRecord 변환을 재사용하고 별도 목록을 반환한다. 파일 없음은 빈 목록, 손상/접근 실패는 날짜·경로를 포함한 예외로 구분한다.
2. `QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.cs`: 화면 전용 저장 이력 캐시 및 비동기 조회. 파일 읽기는 Task.Run에서 작업 하나만 실행하고 마지막 대기 요청을 합친다. 날짜 변경/오늘 복귀/숨김/Dispose의 늦은 결과는 버전으로 폐기한다. 진행 중 JSON 역직렬화를 강제 중단하지는 않으며 완료 결과를 버린다.
3. `QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.Designer.cs`: 위 4개 컨트롤과 2개 이벤트 추가. 별도 Form/프로젝트/resx/DTO는 만들지 않았다.

과거 조회 결과를 `AlarmManager.History`의 내부 `_all`에 추가하지 않는다. Common은 새 메서드 부분을 제외하면 기존 본문 전체가 동일하다. 기존 `btnClear_Click`, `UpdateClearButtonState`, 행 스타일/상세보기/메시지·Cause·Action 해석도 원문과 동일함을 대조했다. **Clear는 날짜와 무관하게 기존 현재 알람 목록을 해제하는 동작 그대로**이며, 과거 파일을 수정하거나 과거 표시 행을 해제하지 않는다. 인터락/시퀀스/알람 발생·해제·저장·복원 코드는 변경하지 않았다.

### 검증 및 남은 확인

검증 자료는 `_codex_verify_ui_rollout_20260929/alarm-history`에 있다. Common 검사는 원본 AlarmManager/AlarmRecord/AlarmSeverity와 실제 JSON 직렬화를 사용하되 LogRoot를 해당 검증 폴더의 합성 데이터로 고정한다. UI 검사는 원본 PageBase/AlarmHistoryPage/Designer/Auditor/Common 버튼을 사용하고 AlarmManager는 지연/오류/호출 관찰 대체물로 분리한다. 운영 어셈블리/장비/운영 JSON을 실행·수정하지 않는다.

최초 숨김 UI 검사에서 기본 SynchronizationContext로 복구되어 await 복귀가 보장되지 않는 하네스 문제를 발견했다. 생산 코드를 우회 수정하지 않고 하네스에 WindowsFormsSynchronizationContext를 유지하고 교차 스레드 검사를 켠 뒤 전체 재검증을 통과했다.

| 최종 검사 | 결과 | 근거·한계 |
| --- | --- | --- |
| 실제 Common + 합성 JSON | 21개 통과 / 실패 0 | 날짜/정렬/동일 시각 Id/500건 상한, 빈 JSON 목록·없는 파일·손상·독점 잠금, Cleared/Restored·독립 객체 검증. History/Active/Id/저장 플래그/파일 목록·바이트 불변. `common-20260929-002151-715.log` |
| 실제 UI + 관찰용 AlarmManager | 46개 통과 / 실패 0 | 날짜·검색·Severity·REFRESH·오늘 복귀·늦은 결과/오류·숨김·5회 재진입·Dispose. 읽기 최대 동시 실행 1개, UI 스레드 밖 실행과 대기 중 UI 메시지 처리 확인. `ui-20260929-002153-730.log` |
| 자정 경계 | 상태 주입 검사 통과 | 실제 시계는 변경하지 않고 live-follow/숨김 재진입/미래일이 오늘이 되는 상태/명시 과거일 유지를 구성해 확인 |
| 배치 | 96 DPI/1678×900에서 통과 | 기존 6개 셀·열 폭 유지. 새 4개 컨트롤 bounds 겹침·영역 밖 없음. 실제 픽셀·글자 잘림·다른 DPI·Designer 저장 검증과 구분 |
| 최종 안전 Build | 종료 0 / 오류 0 / 기존 경고 41 / 17.54초 | 아래 명령. `build/solution-build-final.log`. Clean/Rebuild/운영 프로그램 실행 없음 |
| diff/인코딩 | 신규 코드 정상 / 기존 공백 경고 2개 유지 | UTF-8 BOM/CRLF/final newline 확인. Common·본체 diff check 정상. Designer의 기존 사용자 저장분 `// ` 2줄은 보존하여 전체 diff check 경고가 남음 |

Clear 버튼 검사는 실제 AlarmManager.ClearAll이 아니라 관찰용 대체물만 1회 호출했다. 실제 알람을 발생·해제·저장하는 테스트는 하지 않았다. 실제 ACL 권한 오류, OS 입력/문자 렌더링, Designer 저장·재열기, 운영 알람 파일 및 대용량 실장비 부하는 미검증이다. 합성 파일·검사 실행물은 현장 재확인용으로 검증 폴더에 유지하며 삭제는 없다.

알람 작업 시작 시의 1,222개 tracked/untracked 보호 파일을 SHA-256으로 비교했다. 위 운영 소스 3개와 이 기록 문서만 변경됐고 나머지 1,218개는 동일하다. 보호 파일의 추가·삭제는 없다. ignored 검사 자료는 별도이며, 직전 EventLog 초기값 수정과 기존 사용자 Designer 변경도 보존했다.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe' `
  'D:\00.Source\CDT-320_New\QMC.CDT-320.sln' /t:Build `
  /p:Configuration=Debug '/p:Platform=Any CPU' `
  '/p:OutDir=D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260929\alarm-history\build\out\' `
  /m:1 /v:minimal /nologo /fl `
  '/flp:LogFile=D:\00.Source\CDT-320_New\_codex_verify_ui_rollout_20260929\alarm-history\build\solution-build-final.log;Verbosity=normal'
```

현장 확인은 수정본으로 안전하게 실행한 뒤 알람 페이지의 날짜 선택 → 해당 날짜 저장 이력/없음 안내 → 검색/Severity → REFRESH → 오늘 복귀 순서로 한다. **조회 검증을 위해 Clear 버튼을 누를 필요는 없다.** 해당 날짜 JSON이 저장돼 있지 않으면 과거 이력을 새로 만들어낼 수 없으며 ‘저장된 이력이 없습니다’가 정상이다. 기존 Event/Alarm CSV를 자동으로 혼합해 상태를 추정하지 않는다.

### 완료 보고 템플릿

```text
현재 단계 / 루트 / 브랜치:
승인 범위 / 실제 변경 파일:
기존 공용 구현 재사용 및 사용자 변경 보존 근거:
Designer 결과 / Build 구성·오류·경고:
기능 검사 ID별 결과 / 8개 프리셋 결과:
성능 전후 측정 조건·값·판정:
실패·미검증 / 알려진 제한 / 예외 승인:
실장비 실행·운영 데이터 변경 여부:
이번 단계 판정 / 다음 단계 제안 / 필요한 승인:
```

## 2026-09-29 추가 요청 — Common 반복 로그 1차 보완

사용자 요청: 로그 정리 구현. 시퀀스 호출부는 별도 승인 후 변경하며 동작 조건·폴링 주기·인터락은 변경하지 않는다. 기준은 `D:\00.Source\CDT-320_New` / `master`다. 이번에는 기존 수정과 겹치지 않는 `QMC.Common/Logging/LogManager.cs`만 운영 수정했다. 새 로거·프로젝트·공용 API·설정은 추가하지 않았다.

### 적용한 범위

- 기존 Normal 이하 반복 필터와 1초 간격을 유지하되, 분류·출처별 마지막 문구와 중요도를 비교한다. 사용자 지정 오버로드는 기존 사용자 포함 출처 형식으로 구분한다. A → B → A, 대기 → 완료/실패 → 대기 전환은 즉시 원문을 남긴다.
- 축약은 명시적인 `- Wait` / `- Check` 끝 문구만 대상으로 한다. `Gate` / `Pending` 단어만으로 축약하지 않는다. AboveNormal 이상, 기존 완료·실패 표식과 추가 차단·취소·타임아웃·재시도 표식은 보존한다.
- 실제 기존 코드에서 발견된 `Move ready check failed ... - Check`, `LOT 통계 반영에 실패했습니다 ... - Check`, `게이트 타임아웃 ... - Check`도 보존한다. 한영 실패·예외 등의 명시 문구를 먼저 제외하되, 일반 설정값 `timeoutMs=1000 - Check`는 정상 후보로 유지한다.
- 다음 동일 로그가 오지 않아도 마지막 억제 횟수를 `반복 로그 요약: 원문 [repeat suppressed: N, intervalMs=M]` 별도 행으로 남긴다. 원문과 파일명·행 형식은 유지한다. 요약은 실제 동작이 한 번 더 발생했다는 뜻이 아니다.
- 상태 변경 시 이전 요약을 먼저 큐에 넣는다. 기존 writer의 100ms 대기 루프에서 만료 상태를 회수하며, 매 로그마다 전체 상태를 순회하지 않도록 회수 간격도 제한한다. 새 타이머·Task는 없다. 실제 파일 저장 시간은 writer/디스크 상태에 영향을 받는다.
- 반복 상태는 4,096개 이내다. 상한에 도달하면 기존 요약을 지우지 않고 새 출처는 축약 없이 통과시킨다. 큐·반복 상태는 한 잠금으로 보호하고 파일 IO는 잠금 밖에서 수행한다.
- 기존 정상 writer 종료 경로에서 남은 요약을 회수한다. 기존 Close의 비동기 종료 계약은 유지하므로 프로세스 강제 종료까지 저장을 보장하는 변경은 아니다.

### 검증 결과

- [x] 실제 원본 LogManager/LogInfo를 복제 없이 직접 컴파일한 격리 검사 **95개 통과 / 실패 0개**. 실패 보고용 Log.Write(Exception)만 무해한 대체물 사용.
- [x] 같은 대기 축약, 마지막 요약, A/B/A 전환, 완료/실패 전환, 중요도·사용자·출처·분류 구분, 명시 실패 문구 보호, 가변 문구 보존, 4,096 상태 상한, TickCount 음수/랩, 기존 공개 호출과 형식 검증.
- [x] 8개 병렬 작업에서 단일/8개 출처 각각 원문 수 + 억제 횟수 합계가 8,000회 호출과 일치함을 확인.
- [x] 격리 실행물의 `Log` 폴더에서만 합성 로그 파일을 생성해 실제 writer의 마지막 요약·Close 회수·중요도별 기존 통합 미러를 확인. 운영 어셈블리·장비·운영 로그는 사용하지 않음.
- [x] Debug / Any CPU, 별도 OutDir `/t:Build` 종료 0 / 오류 0 / 기존 경고 41개 / 8.65초. Clean/Rebuild/운영 실행 없음.
- [x] 수정 소스 `git diff --check`, UTF-8 BOM/CRLF/final newline, 기존 public 메서드 선언 유지 확인.
- [ ] 실장비 운전 중 CPU·메모리·디스크·시퀀스 주기 영향 측정. 이번 격리 검사는 성능 동등성 보장이 아님.

검증 자료: `_codex_verify_log_repeat_20260929/RepeatLogChecks.cs`, `run-repeat-checks.ps1`, `run-20260929-195735-787/results.log`, `build/solution-build-final.log`. 운영 소스 SHA-256은 `16CFAA236793EFD32EB96FAB79508B72610CE8FDE4E4D99E757B855FA46B70EA`다. 자료는 후속 승인 단계에서 재현할 수 있도록 유지했으며 삭제는 없다.

시작 시 보호 파일 1,158개의 SHA-256 대조에서 LogManager.cs와 이 체크리스트만 변경됐고 나머지 1,156개는 동일했다. 보호 파일 추가·삭제는 없으며 ignored 검증 소스 2개와 산출물은 이 비교 밖이다. 기존 사용자 변경과 직전 UI·알람 수정은 보존했다.

### 미변경·승인 대기

**Common/Main .log 경로의 반복 요약 보완이지 전체 로그 축약 완료가 아니다.** `Log.cs`의 블랙박스 캡처, `SequenceLog`, `EventLogger` CSV, 이력 UI, 상세모드 기본값, 큐 상한·저장 실패 처리, 시퀀스/인터락/모션/설정은 변경하지 않았다. 따라서 호출 전 문자열 생성·블랙박스 부하는 여전히 남는다.

Common 입력에는 RunId가 없으므로 서로 다른 실행의 동일 문구를 완전히 구분하지 못한다. `- Check`와 문구 검사는 완전한 의미 분류가 아니며 모든 호출의 중요도 검토가 끝난 것도 아니다. `failed=False` 같은 정상 문구도 보존될 수 있다. 상세모드를 일괄 해제하지 않는다.

다음 승인 요청은 **PickerPlaceSequence.OutputStageReady.cs의 OutputStage 준비 대기 로그 한 곳**이다. 동일 대기는 최초·주기 요약, 상태/사유 변경은 즉시 기록하도록 로그 생성 조건만 좁히는 방향이다. 실제 준비 조건, 성공/실패 처리, 취소·정지 확인, `Task.Delay(1)`, 이동 명령과 인터락은 그대로 유지한다. 아직 해당 파일과 Front/Rear 호출부는 수정하지 않았다. 별도 승인 전 진행하지 않는다.

## 2026-09-29 추가 승인 — OutputStage 준비 대기 로그 한 곳

위 승인 대기 항목에 대한 사용자 지시 `다음 진행해줘`를 받아 적용했다. 기준은 `D:\00.Source\CDT-320_New` / `master`다. 이번 운영 수정 파일은 `QMC.CDT-320/Sequencing/Picker/PickerPlaceSequence.OutputStageReady.cs` 하나이며 기존 Common 변경과 UI 변경은 보존했다.

### 적용 범위와 동작 보존

- `VerifyOutputStageReadyAsync`의 기존 반복 대기 로그 호출 위치만 축약 경로로 연결했다. 최초 대기는 즉시 기록하고 동일 상태 반복은 1초 경과 후 다음 폴링에서 생략 횟수로 요약한다. 동작 횟수를 줄인 것이 아니라 같은 대기 로그의 생성 횟수를 줄인 것이다.
- OutputSide·DieId·PickerNo·materialReady·signalReady·reason이 같으면 로그 상세 문자열만 재사용한다. 실제 준비 조건 조회는 매 반복 그대로 수행한다. `stageReceiveComplete`를 포함해 관찰 상태가 달라지면 이전 요약과 새 상태를 즉시 기록한다.
- 대기 호출별 상태를 지역 객체로 분리한다. 기존 `SequenceTrace.NextTraceId()`와 기록 순번 `waitEntry`로 빠른 재진입/A → B → A 로그가 기존 하위 반복 필터에 다시 숨겨지지 않도록 한다. Source와 기존 `WriteLog` → Common/SequenceLog 경로는 유지한다.
- 로그에는 `stageReceiveComplete`, `waitId`, `waitEntry`, `repeatSuppressed`, `intervalMs`가 추가된다. 반복 요약은 원래 대기 기록을 생략한 횟수이며 추가 동작을 뜻하지 않는다.
- 정상 준비 완료 시 배치 시작 전에 마지막 요약을 기록한다. 실패·취소·Cycle Stop·조기 반환은 finally에서 남은 횟수를 정리하며, 종료 요약 오류가 원래 반환값/제어 예외를 덮어쓰지 않도록 한다. 강제 프로세스 종료나 기존 sink/디스크 실패 시 저장까지 보장하는 변경은 아니다.
- 준비/실패 조건, 안전 이동, 리소스 해제, Full 교체 handoff, 5초 lease 양보 조건, 정지 확인, `Task.Delay(1, ct).ConfigureAwait(false)`, 기존 catch/return은 변경하지 않았다. 새 로거·공용 API·프로젝트·타이머·Task·설정은 없다.

### 완료한 검증

- [x] 로그 전용 추가/치환 부분만 메모리에서 제외하고 수정 전 파일 전체와 대조: 나머지 원문 동일. 원본 롤백·복사본 덮어쓰기 없음.
- [x] 별도 읽기 전용 코드 검토: 동작 조건·안전 이동·리스·정지·취소 흐름 변경 없음 확인.
- [x] 실제 대상 메서드/로그 helper/기존 WriteLog/NextTraceId 원문을 메모리에서 추출해 실제 SequenceLog·LogManager·LogInfo와 함께 격리 컴파일: **69개 통과 / 실패 0개**.
- [x] 즉시 준비/수동 실패, 30회 동일 대기, 상태별 문자열 캐시, A/B/A, 수령 완료 상태 변화, 요약 간격/음수 경과시간, 성공·실패·취소·정지 꼬리 요약, Full 교체 경계별 오류 반환, SafeY 옵션, 연속/동시 호출 분리, Main/시퀀스 이력 전달 횟수 검증.
- [x] 장비/자재/이벤트 저장 의존성은 관찰용 대체물 사용. 운영 어셈블리/장비 실행/운영 데이터 접근 없음. 실제 메서드의 `Task.Delay(1)`은 그대로 실행했으나 실시간 주기 성능 검증을 뜻하지 않음.
- [x] 기존 Common 격리 회귀 검사 재실행: **95개 통과 / 실패 0개**. 합성 writer 파일은 해당 검증 실행 폴더에만 생성.
- [x] Debug / Any CPU, 별도 OutDir `/t:Build`: 종료 0 / 오류 0 / 기존 경고 41개 / 8.15초. Clean/Rebuild/운영 실행 없음. 격리 테스트 컴파일의 별도 참조 버전·대체 클래스 경고는 테스트 로그에 기록됨.
- [x] 이번 수정 소스의 `git diff --check`, 수정 파일 UTF-8 BOM/CRLF/final newline 확인. 전체 작업 트리 검사에는 기존 Designer 파일의 공백 경고가 남아 있으며 해당 파일은 수정하지 않음.

검증 자료는 `_codex_verify_place_wait_log_20260929/PlaceWaitTestHost.cs`, `PlaceWaitChecks.cs`, `run-place-wait-checks.ps1`, `run-20260929-201626-393/results.log`, `build/solution-build.log`에 있다. Common 재검사 결과는 `_codex_verify_log_repeat_20260929/run-20260929-201446-423/results.log`다. 이번 운영 소스 SHA-256은 `0F6CB8D929871748E6620D294374C5FDF6C1F805319193641C9B48EA8346F113`이다. 검증 자료는 아래 현장 확인 및 재현을 위해 유지하며 삭제는 없다.

### 남은 현장 확인과 다음 승인 범위

- [ ] 사용자가 안전을 확인한 시험 환경에서 OutputStage 준비 대기 → 준비 완료, 취소/Cycle Stop, Full 교체 흐름 및 기존 알람 확인.
- [ ] Main 로그와 시퀀스 이력에서 최초/사유 변경/마지막 요약, 대상 Die·Picker, 생략 횟수 확인. 파일 저장 및 GUI 조회는 이번 대체 sink 검사를 넘어 별도 확인 필요.
- [ ] 실제 구동 중 CPU·메모리·디스크·UI 응답·시퀀스 주기 영향 측정. 성능 동등성이나 실장비 무영향을 확정한 것은 아님.
- [ ] 5초 lease 양보 및 pickup cap 경고 분기는 원문 보존 정적 검증만 완료했으며 이번 동적 격리 검사에는 포함하지 않음.

Front/Rear idle 등 다른 호출부, 전역 SequenceLog/EventLogger 정책, 상세모드 설정은 이번에 변경하지 않았다. 다른 시퀀스 호출부로 확대하려면 해당 위치와 위험을 분석해 다시 승인받는다.

## 2026-09-30 알람 화면 생성 오류 및 빈 Designer 수정

사용자 보고: 알람 탭 진입 시 `AlarmHistoryPage.ApplyHistoryWhiteSurface()` 85줄에서 NullReferenceException으로 종료되고, Visual Studio Designer에도 컨트롤이 보이지 않음. 기준은 `D:\00.Source\CDT-320_New` / `master`다.

### 원인과 수정 범위

작업 시작 시 `AlarmHistoryPage.Designer.cs`의 InitializeComponent에는 SuspendLayout/Name/ResumeLayout만 남아 있었다. 필드는 선언돼 있지만 rootLayout을 포함한 컨트롤 생성·속성·배치·이벤트 연결이 없어 생성자에서 rootLayout.Margin에 접근할 때 null 참조가 발생한다. 컨트롤이 보이지 않는 Designer 증상도 같은 원인이다. 해당 코드가 빠진 경위는 이번 검증으로 확정하지 않았다. 9월 29일의 검사는 당시 소스 상태에 대한 기록이며, 이번 빈 초기화 상태의 통과 증거가 아니다.

운영 수정은 `QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.Designer.cs` 하나다. 현재 선언과 업무 코드의 계약에 맞게 InitializeComponent 안에 컨트롤 14개와 그리드 컬럼 7개 생성, 정적 속성, TableLayout 부모/셀 연결, 이벤트 7개를 명시적으로 작성했다. root는 헤더/필터/그리드 3행이고 필터는 기존 6개 셀 뒤에 날짜/달력/REFRESH/상태 4개 셀을 둔다. 기존 첫 6개 열의 실제 너비와 알람 컬럼 순서·읽기 전용·행 선택·더블클릭 기능을 유지한다. REFRESH는 기존 Common UiStandardButton Primary를 사용한다.

일반 AlarmHistoryPage.cs, 알람 발생·해제·파일 조회 API, Common, 시퀀스, 인터락은 이번에 수정하지 않았다. null 검사로 필수 UI 생성을 생략하는 방식이나 일반 .cs에서 새 컨트롤을 만드는 방식은 사용하지 않았다. 과거 파일 덮어쓰기/소스 롤백/다른 복사본 이식은 없다.

### 검증 결과

- [x] 독립 읽기 전용 코드 검토: 생성 순서, root 3행/filter 10열, 컬럼 7개, 기능 이벤트 7개 각 1회 연결 확인.
- [x] 실제 PageBase/AlarmHistoryPage/Designer/공용 버튼을 직접 컴파일한 구조 검사 **55개 통과 / 실패 0개**. 디자인 모드 생성, 컨트롤 부모/셀 연결, 날짜/그리드 초기화, 이벤트, 구독/타이머 보호, 재진입/Dispose 확인.
- [x] 기존 UI 회귀 검사 원문 재실행 **46개 통과 / 실패 0개**. 오늘/과거 날짜, 검색·Severity·REFRESH, 읽기 오류와 복구, 늦은 결과 폐기, 오늘 복귀, 숨김/재진입, 최대 1개 백그라운드 읽기 및 UI 메시지 처리 확인.
- [x] 테스트 알람 2건을 넣은 실제 컨트롤 DrawToBitmap 결과를 확인: 헤더·날짜·REFRESH·7개 컬럼/행 표시. 번역·알람 저장 서비스는 대체물이므로 운영 화면 캡처나 실제 OS 입력 검증과 구분한다.
- [x] 사용자 화면과 같은 Release / x64, 별도 OutDir `/t:Build`: 종료 0 / 오류 0 / 기존 경고 41개 / 13.42초. 운영 실행/배포, Clean/Rebuild 없음.
- [x] Designer 수정분 `git diff --check`, UTF-8 BOM/CRLF/final newline 정상. 보호 파일 해시 대조에서 해당 Designer와 이 기록만 변경됐고 나머지 기존 파일은 보존됨.
- [ ] 실제 Visual Studio에서 AlarmHistoryPage 디자인 탭 닫기/재열기, 컨트롤 선택·속성 편집·저장 후 재열기 확인.
- [ ] 사용자 실행 환경에서 다시 빌드한 프로그램의 알람 탭 진입, 날짜 변경/REFRESH, 탭 재진입 확인. 운영 알람·장비 실행은 이번 격리 검사에서 수행하지 않음.

최초 구조 검사의 날짜 이벤트 횟수 판정은 하네스가 EventHandlerList만 관찰해 실패했다. 실제 .NET Framework DateTimePicker는 ValueChanged를 직접 EventHandler 필드에 저장하므로 그 필드도 관찰하도록 테스트 코드만 보완했고 최종 구조 검사 55개를 통과했다. 이 하네스 문제를 이유로 운영 이벤트나 조회 코드를 변경하지 않았다.

검증 자료는 `_codex_verify_alarm_designer_crash_20260930`에 있다. 구조 결과: `run-20260930-175403-871-22ef312acbdd498b8b10ca2a46ac32c7/structure-results.log` 및 `alarm-page-preview.png`. UI 회귀 결과: `run-20260930-175255-442-aa0d92b8451f49b5ab3ca4fe0ccb7657/ui-results.log`. 빌드: `build-release-x64/solution-build.log`. 실제 Designer/현장 재확인용 자료를 유지하며 삭제는 없다. 수정 Designer SHA-256은 `2C9C2E96353EDDE34DA65EB7781A8E72E6E7B6E7AC3035082F5CC7095D4B55E2`다.
