# 체크리스트 — CycleTime 간트 (모터 세그먼트 방식, 사용자 승인 설계)

작성일: 2026-07-22

## 승인된 설계 요약
- Entry = 다이 × 공정 구간, 유닛 행 5종 { INPUTVISION, PICKUP, BOTTOM, SIDE, PLACE }.
  키 = `unit|F/R+pickerNo-dieId`. 계측 필터 = Auto 운전.
- **단계 = 고정 슬롯이 아니라 모터(축) 동작 시작~종료 세그먼트 리스트** (사용자 지시 3):
  `CycleMotionSegment { Axis, StartMs, EndMs(-1=진행중) }`, 엔트리당 최대 64개.
- ERR = 시퀀스별 초크 포인트(ExecuteAsync 실패 반환/catch에서 활성 엔트리 일괄 ERR) —
  분기 전수 삽입 금지(사용자 지시 2). 보조 안전장치: UI 60초 초과 미완료 미표시.
- 비전 원본 원칙 유지: Stopwatch tick, 시작 강제, 중복 최초값, 링버퍼 2000/10분,
  Snapshot=복사본, UI 페인트 전용/500ms/숨김 정지, 지연 생성, Designer 분리.

## 구현
- [x] C1. `Diagnostics\CycleTimeStore.cs` 신규 — Entry/MotionSegment/Store
  (MarkStart/MarkMotionStart/MarkMotionEnd/MarkResult/MarkError/MarkErrorAllActive/
  Snapshot/Clear, 전부 try/catch+짧은 lock, 비전 정책값 유지)
- [x] C2. `Diagnostics\HandlerTactLog.cs` — 얇은 정적 래퍼(이중 try/catch)
- [x] C3. PICKUP 훅 — 시작(SelectNextPickTarget), 축 세그먼트(전용 이동 헬퍼 2종 내부 +
  fast Z/SyncLift/Avoid/EjectPinZ 호출부), RESULT(UpdateMaterialToPicker),
  ERR 초크(ExecuteAsync)
- [x] C4. INPUTVISION 훅 — 시작(다이 타겟 이동 진입), 세그먼트(스테이지/비전 이동,
  촬영요청→EPD), RESULT(EPD), ERR 초크
- [~] C5. BOTTOM/SIDE/PLACE 훅(기본) — 시작/주요 이동 1~2/EPD·완료/ERR 초크
- [x] C6. `Ui\Controls\CycleTimeGanttControl(.cs/.Designer.cs)` — 비전 원본 이식 +
  모터 세그먼트 렌더(축별 색), 유닛 행/색 교체, Die→"사이클" 문구, 툴팁=축별 소요
- [x] C7. 작업화면 버튼 + 지연 생성 + 배타 전환 (기존 네비 버튼 스타일)
- [x] C8. csproj 등록(신규 4파일), 인코딩 BOM+CRLF

## 검증
- [x] V1. 빌드 에러 0, 신규 경고 0
- [x] V2. 스토어 하네스 — 세그먼트 시작/종료·진행중(-1)·중복 최초값 유지·픽커 매핑
  (F n→n, R n→n+4, 정보없음 0)·ERR 종결(Failed+종료시각)·시작 강제(중간 합류 버림)·
  링버퍼 상한·Snapshot 복사본·MarkErrorAllActive 프리픽스·4스레드×200 동시 무결
- [x] V3. 렌더 확인 — DrawToBitmap PNG: 픽커 8행/유닛 서브레인/목록+상세/ERR 테두리/
  시간창/일시정지/빈 데이터 안내
- [x] V4. 고아 바 미표시(60초 초과 미완료), 숨김 시 타이머 정지(코드 리뷰)
- [x] V5. 시퀀스 로직 무변경 확인 — 훅 diff가 Mark 1줄 삽입만인지 리뷰
- [x] V6. 레포트 — 훅 지점 목록, 유닛별 세그먼트 구성, 실운전 확인 항목

## 검증 결과 기록 (2026-07-22)
- V1: 빌드 EXIT=0, 에러 0 (신규 경고 확인은 기존 5건+CS0219 외 신규 없음).
- V2: CycleTimeHarness 22체크 ALL PASS — 세그먼트 시작/종료/진행중 종결 시각 마감,
  중복 시작 무시, 픽커 매핑(F3→3/R2→6/무헤드→0), ERR(Failed+종료시각), 중복 종결 최초값,
  시작 강제(무시작 마크 버림), 링버퍼 50 상한+최고령 제거, Snapshot 복사본,
  MarkErrorAllActive 프리픽스(PICKUP|F만, R/타유닛 무영향), 4스레드×200=800건 무결+Seq 단조.
- V3: DrawToBitmap PNG 육안 확인 — 픽커 8행+범례+통계 / 유닛 5행+픽커 서브레인(라벨 안 잘림)
  / 사이클 목록(ERR 빨간 행)+상세 안내 / 빈 데이터 안내문. 툴바·시간축 정상.
- V4: 고아 60초 규칙·숨김 타이머 정지 — DrawBar ageMs>60000 미표시 및 VisibleChanged
  타이머 on/off 코드 리뷰 확인 (하네스에서 StartTick 조작 불가로 코드 리뷰 대체 명시).
- V5: 훅 diff 리뷰 — 시퀀스 수정은 전부 HandlerTactLog 1줄 삽입(+TactRequestId 헬퍼 2개)로
  로직 무변경. ERR은 초크 포인트(픽업 ExecuteAsync finally / prepare ExecuteAsync finally).
- C5 상태: BOTTOM/SIDE/PLACE 훅은 미구현(후속 증분) — 스토어/UI는 행·색 지원 완료라
  훅만 추가하면 표시됨. 레포트에 명시.
- V6: 레포트 기재.