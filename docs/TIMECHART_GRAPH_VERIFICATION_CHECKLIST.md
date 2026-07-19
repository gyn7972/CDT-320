# LOGIC TIMECHART 그래프 개선 검증 체크리스트

- 작성일: 2026-07-19
- 기준 소스: `D:\Source\CDT-320_New`
- 연계 계획서: `docs\TIMECHART_GRAPH_IMPLEMENTATION_PLAN.md`
- 실행 상태: 비장비 자동 검증 완료, Sim Auto 종단검증 대기
- 집계: PASS 89 / 미실행 14 / FAIL 0 / 전체 103

## 1. 판정 규칙과 증거 위치

- `[x]` PASS: 기대 결과를 만족하고 측정값 또는 자동시험 증거가 있음
- `[ ]` 미실행: 실제로 수행하지 않았으며 사유와 후속 절차를 기록함
- `[!]` FAIL: 기대 결과를 만족하지 못함
- `[-]` N/A: 적용 대상이 아님
- 안전한 빌드 출력: `D:\Source\CDT-320_New\_build_check_handler\timechart_out`
- 자동시험/렌더 증거: `D:\Source\CDT-320_New\_build_check_handler\timechart_tests`
- 실제 CSV: `D:\CDT-320\Log\TactTime\2026-07-18.csv`, `2026-07-10.csv`
- 전체 Handler 실행은 설정에 따라 실제 축·IO·Vision 연결을 시도할 수 있어 수행하지 않았다.

## 2. 구현 전 기준 확인

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | PRE-01 | 저장소 루트 | `git rev-parse --show-toplevel` = `D:/Source/CDT-320_New` |
| [x] | PRE-02 | 기존 변경 | 구현 시작 시 `master...CDT-320_NEW/master`, dirty 없음 확인 |
| [ ] | PRE-03 | 구현 전 화면 캡처 | 구현 전 캡처를 별도로 확보하지 못함. 구현 후 1678×900/1100×700 캡처는 확보 |
| [ ] | PRE-04 | 구현 전 실시간 기준값 | 장비/Sim을 실행하지 않아 Memory Snapshot 기준값 미확보 |
| [ ] | PRE-05 | 기존 정상·실패·정지 샘플 | 최신 실제 Run은 `Ok/Canceled`만 존재. 전체 Result는 검증 fixture로 대체 확인 |

## 3. 코드 구조 및 정적 검사

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | CODE-01 | CSV Reader 책임 분리 | `TactTimeCsvReader.cs`에 파일/CSV/Run 인덱스 책임 분리 |
| [x] | CODE-02 | Chart Control 책임 분리 | `TactTimeChartControl.cs`에 렌더·확대·이동·선택 책임 분리 |
| [x] | CODE-03 | Designer 규칙 | Designer에는 생성·속성·배치·이벤트 연결만 존재 |
| [x] | CODE-04 | 프로젝트 등록 | 신규 2개 `.cs`의 명시적 `<Compile Include>` 확인 |
| [x] | CODE-05 | 비동기 규칙 | `async void`는 2개 UI 이벤트뿐이며 Reader에 CancellationToken 적용 |
| [x] | CODE-06 | 예외 처리 | 빈 catch 없음. UI 메시지와 Main 로그에 operation/file/progress/error 기록 |
| [x] | CODE-07 | 인코딩 | 수정 소스 UTF-8 BOM, CRLF, final newline 검사 통과 |
| [x] | CODE-08 | 한글 문구 | 수정 범위의 한국어 문자열 정적 검색과 렌더 캡처 정상 |
| [x] | CODE-09 | 외부 패키지 | csproj diff에 Compile 등록만 존재, 신규 NuGet 없음 |
| [x] | CODE-10 | 장비 안전 | 신규 축·IO·Vision·Auto Start/Stop 호출 없음, CSV 읽기/화면 코드만 변경 |

## 4. CSV 파서 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | CSV-01 | 기존 19열 파일 | 13.75MB 실제 파일 39,499행 전부 파싱, skipped=0 |
| [x] | CSV-02 | UTF-8 BOM/한글 | 실제 파일 BOM `EF BB BF`; fixture 한글 Detail 무손상 |
| [x] | CSV-03 | 쉼표 포함 필드 | `한글, 쉼표와 ...` 단일 Detail로 복원 |
| [x] | CSV-04 | 큰따옴표 포함 필드 | `"인용부호"`로 복원 |
| [x] | CSV-05 | 줄바꿈 포함 필드 | `첫째 줄<LF>둘째 줄` 단일 레코드로 복원 |
| [x] | CSV-06 | Round-trip 시각 | `+09:00` 샘플 독립 Parse 대비 TickDifference=0, Kind=Local |
| [x] | CSV-07 | Enum 변환 | Category 10종, Result 5종 모두 일치; 정의되지 않은 `999` 제외 |
| [x] | CSV-08 | 잘못된 행 | 잘못된 시각/Enum 행만 제외, 정상 행 유지, skipped 집계/경고 확인 |
| [x] | CSV-09 | 누락 선택 열 | 필수 5열 샘플 로드 성공, 누락 열 목록 경고 확인 |
| [x] | CSV-10 | 미완성 마지막 행 | unmatched quote 마지막 행 제외, `IncompleteLastRecord=True` |
| [x] | CSV-11 | 읽기 공유 | ReadWrite 공유로 연 파일과 동시 읽기 성공, parsed=2 |
| [x] | CSV-12 | 취소 | 198.8MB 파일 취소 감지 39ms, 비동기 취소 41ms |

## 5. Run 인덱스 및 데이터 소스 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | DATA-01 | 파일 기본 위치 | `EventLogger.LogRoot\TactTime` 우선, `D:\CDT-320\Log\TactTime` fallback 코드 확인 |
| [x] | DATA-02 | Run 목록 | 2026-07-18 10개, 2026-07-10 52개 Run 분리 인덱싱 |
| [x] | DATA-03 | Run 정보 | 시작/종료, Mode, LotId, 건수와 short RunId의 `ToString()` 확인 |
| [x] | DATA-04 | 기본 Run 선택 | StartedAt 내림차순 인덱스와 `cmbRun.SelectedIndex=0` 확인 |
| [x] | DATA-05 | Run 격리 | 선택 Run 38,981건만 로드; UI fixture HISTORY Grid 10/10건 일치 |
| [x] | DATA-06 | LIVE→HISTORY | HISTORY 상태·Auto Refresh 비활성·Grid 적용 자동시험 통과 |
| [x] | DATA-07 | HISTORY→LIVE | LIVE 상태 복귀 자동시험 통과 |
| [x] | DATA-08 | Category 필터 | HISTORY `ALL=10`, `Vision=1` 자동시험 및 공통 필터 함수 확인 |
| [x] | DATA-09 | Item 필터 | 기존 9개 Item 매핑 자동시험 9/9 통과 |
| [x] | DATA-10 | Clear View | HISTORY 메모리만 0건으로 변경, fixture 파일 길이 1,162B 불변 |
| [x] | DATA-11 | 파일 오류 | 파일 없음과 필수 Result 열 누락에서 경로/원인 예외 확인 |

## 6. 통계 정확도 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | STAT-01 | 실제 표시 구간 | 합성 10s 샘플 및 실제 최신 Run 1,911,239.769ms 독립 계산 일치 |
| [x] | STAT-02 | 중첩 합계 분리 | 10s Run/9s Unit을 평균에서 제외, 상세 `[0,100,200,1000]` 평균 325ms |
| [x] | STAT-03 | 평균 | 합성 독립 산술평균 325ms 일치; 실제 상세 평균 405.391ms |
| [x] | STAT-04 | 최소/최대 | 합성 최소 0ms, 최대 1s; 실제 최대 16,793ms |
| [x] | STAT-05 | P95 | nearest-rank 합성 1s, 실제 독립 계산 3,529ms 일치 |
| [x] | STAT-06 | 결과 건수 | Enum fixture 및 실제 Canceled 5건 집계 확인 |
| [x] | STAT-07 | 0ms 레코드 | fixture의 0ms 1건이 누락되지 않음 |
| [x] | STAT-08 | ElapsedMs 재계산 | 누락 샘플을 1,250ms로 재계산하고 호환 경고 반환 |
| [x] | STAT-09 | 이질 항목 혼합 방지 | `평균·P95 혼합 참고값(N종)` 및 Run/Unit 제외 안내 확인 |

## 7. 택타임 추이 그래프 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | TREND-01 | X축 순서 | StartedAt/EndedAt 정렬 코드와 실제 Run 렌더 확인 |
| [x] | TREND-02 | Y축 단위 | 0ms, s, hh:mm:ss.fff 동적 축 렌더 확인 |
| [x] | TREND-03 | 포인트 값 | hit area가 원본 Record를 보존하고 Grid 양방향 선택 통과 |
| [x] | TREND-04 | 평균선 | 합성 평균 계산과 AVG 기준선 코드/렌더 확인 |
| [x] | TREND-05 | P95선 | nearest-rank P95 계산과 P95 기준선 코드/렌더 확인 |
| [x] | TREND-06 | 최대값 보존 | 5,001점→1,200점 축소 후 9,000ms spike 보존 자동시험 |
| [x] | TREND-07 | 결과 표시 | Failed red, Stopped/Canceled amber 우선 렌더 코드와 합성 캡처 확인 |
| [x] | TREND-08 | 툴팁 | 시간·경로·ElapsedMs·Result·Alarm·Detail 원본 Record 기반 구성 확인 |

## 8. 장비 타임라인 그래프 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | TL-01 | Lane 순서 | Machine/Input/Front/Rear/Output/Vision 우선순위와 실제 캡처 확인 |
| [x] | TL-02 | 시작 위치 | 공통 시간축 `TimeToX(StartedAt)` 코드와 실제 캡처 확인 |
| [x] | TL-03 | 막대 길이 | `EndedAt-StartedAt` 비례, 최소 3px 처리 확인 |
| [x] | TL-04 | 병렬 동작 | 실제 Run Front/Rear 동시 동작이 공통 X축에서 수평 비교됨 |
| [x] | TL-05 | Lane 내 겹침 | 같은 Lane/Process 중첩 2건이 row 0/1로 분리됨 |
| [x] | TL-06 | 색상/범례 | Unit/Process/Vision/Motion/Wait/Fail 캡처와 색상 함수 일치 |
| [x] | TL-07 | 선택 연동 | Chart→Grid 원본 Record 선택 자동시험 통과 |
| [x] | TL-08 | Grid 연동 | Grid→Chart 선택 및 화면 포함 자동시험 통과 |
| [x] | TL-09 | 짧은 공정 | 1ms 레코드 hit width=3px 자동시험 통과 |
| [x] | TL-10 | 빈 데이터 | 빈 List 비트맵 렌더 예외 없음 |

## 9. 확대·이동·레이아웃 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | UI-01 | 마우스 휠 확대 | view span 1.000→0.750 자동시험 |
| [x] | UI-02 | 수평 이동 | view start 0.104→0.292 자동시험 |
| [x] | UI-03 | 전체 보기 | ResetView=0.0..1.0 자동시험 |
| [x] | UI-04 | 보기 전환 | ViewMode 전환 시 공통 Record 유지; 일반 LIVE 갱신 zoom span 0.626 유지 |
| [x] | UI-05 | 1678×900 | 통합 화면 캡처에서 잘림/중첩 없음, KPI 독립 행 확인 |
| [x] | UI-06 | 작은 해상도 | 1100×700 캡처에서 필수 버튼·필터·차트 접근 가능 |
| [x] | UI-07 | DPI 100% | 1678×900 및 1100×700 렌더 정상 |
| [ ] | UI-08 | DPI 125% | 실제 앱 DPI 전환 미실행. Sim 검증 시 Windows 배율 125%로 확인 |
| [ ] | UI-09 | DPI 150% | 실제 앱 DPI 전환 미실행. Sim 검증 시 Windows 배율 150%로 확인 |
| [ ] | UI-10 | 장시간 GDI | 장시간 실제 화면 유지 미실행. 1시간 후 GDI Object 추이 확인 필요 |

## 10. 성능 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | PERF-01 | 일반 파일 인덱스 | 13.75MB/39,499행, 439ms, 10 Run, 오류 0 |
| [x] | PERF-02 | 대용량 파일 인덱스 | 198.8MB/614,559행, 5,780ms, 52 Run, 오류 0 |
| [x] | PERF-03 | 최대 Run 로딩 | 288,539건, 6,257ms, 관리 메모리 +108.2MB(반복 문자열 공유 적용) |
| [x] | PERF-04 | 차트 재그리기 | 실제 38,981건 타임라인 295ms, 추이 281ms |
| [x] | PERF-05 | Grid 제한 | 전체 38,981건/Grid 5,000건을 KPI로 구분, 차트 전체 데이터 사용 |
| [x] | PERF-06 | 취소 응답 | 198.8MB 동기 39ms, 비동기 41ms |
| [x] | PERF-07 | 반복 열기 | 14MB 파일 10회 후 메모리 +0.18MB, Handle -1 |

## 11. 실시간 및 회귀 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [ ] | REG-01 | LIVE 자동 갱신 | 1초 Timer와 zoom 유지 자동시험은 통과. 실제 Sim 신규 레코드 반영은 E2E에서 확인 필요 |
| [x] | REG-02 | 화면 비표시 상태 | `ShouldRefreshVisible`, Visible, HISTORY/loading 조건으로 Timer 중지 코드 확인 |
| [ ] | REG-03 | 페이지 재진입 | 생성/해제 코드와 Timer Dispose 확인. 실제 탭 10회 재진입은 미실행 |
| [x] | REG-04 | 기존 Grid | 11개 열, NotSortable, 5,000건 제한, 선택 상세 자동시험 통과 |
| [x] | REG-05 | 기존 필터 | Category fixture와 Item 9/9 매핑 통과 |
| [ ] | REG-06 | LIVE Clear View | `_viewSince` 코드 확인. 실제 Memory Snapshot 이후 데이터만 표시되는지 Sim 확인 필요 |
| [x] | REG-07 | Cycle Stop 기록 | 실제 최신 Run Canceled 5건을 amber로 유지, Failed로 변환하지 않음 |
| [x] | REG-08 | 실패 기록 | fixture Failed/AlarmCode와 red 렌더 확인 |
| [x] | REG-09 | 장비 영향 없음 | 신규 코드에 시퀀스·축·IO·Vision 명령 호출 없음 |

## 12. 빌드 및 최종 검증

| 상태 | ID | 검증 항목 | 결과/증거 |
|---|---|---|---|
| [x] | BUILD-01 | `git diff --check` | 통과 |
| [x] | BUILD-02 | QMC.Common Compile 등록 | `Diagnostics\TactTime\TactTimeCsvReader.cs` 등록 확인 |
| [x] | BUILD-03 | QMC.CDT-320 Compile 등록 | `Ui\Pages\WorkInfo\TactTimeChartControl.cs` 등록 확인 |
| [x] | BUILD-04 | 안전한 OutDir Build | Debug/Any CPU 성공, 기존 CS0162 경고 4건만 존재 |
| [x] | BUILD-05 | Designer/Control 생성 | LogicDetailPage STA 생성 및 2개 해상도 DrawToBitmap 성공 |
| [x] | BUILD-06 | 정적 검색 | 빈 catch/깨진 한글/하드웨어 명령/파일 쓰기/신규 패키지 이상 없음 |
| [x] | BUILD-07 | 운영 폴더 보호 | 출력은 `_build_check_handler\timechart_out`만 사용, `D:\CDT-320` 미변경 |

## 13. 시뮬레이션 종단 검증

| 상태 | ID | 검증 항목 | 남은 절차 |
|---|---|---|---|
| [ ] | E2E-01 | 실시간 표시 | Sim 확정 후 Auto Run, LIVE Grid/두 그래프 신규 레코드 확인 |
| [ ] | E2E-02 | CSV 저장 | Run 종료 후 당일 CSV의 동일 RunId 확인 |
| [ ] | E2E-03 | 과거 재로딩 | 방금 생성된 파일/Run을 HISTORY로 다시 선택 |
| [ ] | E2E-04 | 수치 동일성 | 대표 10건의 LIVE/Grid/CSV/HISTORY StartedAt/EndedAt/ElapsedMs 대조 |
| [ ] | E2E-05 | Cycle Stop 재로딩 | Sim Cycle Stop 후 Stopped/Canceled와 종료시각 확인 |

## 14. 완료 판정

| 구분 | 결과 |
|---|---|
| 코드/안전 | PASS |
| CSV/이력 | PASS |
| 통계/그래프 | PASS |
| 100% DPI UI/성능 | PASS |
| 빌드 | PASS |
| 실제 Handler/Sim 종단 | 미실행 |
| 최종 판정 | 구현 및 비장비 검증 PASS. 배포 전 E2E-01~05와 UI-08~10, REG-01/03/06 필요 |
