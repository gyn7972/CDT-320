# STAGE 80 — CHECKLIST: 다중 컨트롤러 결선 + Setup/Recipe UI 책임 분리

- **작성일**: 2026-06-02
- **연계 SPEC**: `docs/STAGE80_SPEC_MultiCtrl_UiSeparation.md`
- **단계**: CHECKLIST (구현 전, 사용자 컨펌 대기) — 문서까지만, 코드 변경 없음
- **검증 게이트**: build 0/0 + verify_all 회귀 0

> ⚠ 선결: baseline 5건 확인됨(SPEC §0). SPEC §8 확인 5개 컨펌 + 마이그레이션 라운드트립 통과 전 구현 진입/머지 금지.

---

## A. 데이터 모델
- [ ] `ControllerChannels` 클래스 신규 (ControllerPort + List<int> Channels)
- [ ] `AlgorithmLightWiring`: 단일 ControllerPort/Channels → `List<ControllerChannels> ControllerSets` (LegacyPage 유지)
- [ ] `InspectionLightSetting.ControllerPort` 추가 + Clone (StabilizeDelayMs 등 기존 필드 보존)
- [ ] Setup 마이그레이션 — 구 단일 ControllerPort/Channels → ControllerSets[0] (임시 보존 프로퍼티 + OnDeserialized)
- [ ] Recipe 마이그레이션 — ControllerPort 자동채움 (단일=자동 / 다중=ControllerSets[0]+안내), Form1.Load 1회 보정
- [ ] 다중 결선 임의선택 status/알람 보고
- [ ] 구버전 JSON 라운드트립(단일→ControllerSets[0]) + 다중(2항목 보존) 단위 시험

## B. Setup UI — 결선 TreeView
- [ ] `_gridWiring` 평면 그리드 제거 → TreeView + 우측 디테일
- [ ] 알고리즘 5노드 고정, expand
- [ ] 노드 클릭 → ControllerSets 목록 + (Controller 콤보, Channels 체크박스)
- [ ] "+ 컨트롤러 추가" — 그 알고리즘에 안 쓰인 PortName 만 콤보
- [ ] "삭제" — 해당 set 만
- [ ] (Controller,Channel) 중복 — 경고만 (확인 #2)

## C. Setup UI — 책임 분리
- [ ] Level/Brightness/TrackBar/DefaultLevel 컨트롤 미존재 재확인(grep) — 현재 0건, 회귀 방지
- [ ] 헤더 "조명 시스템 설정 — 기구적 결선 (한 번 설정 후 거의 변경 없음)"
- [ ] 안내 라벨 "검사별 밝기/On-Off 는 [레시피] 에서 설정"

## D. Recipe UI — Controller 컬럼
- [ ] 값 편집 표에 Controller 콤보 컬럼 추가
- [ ] Controller 콤보 items = `wiring.ControllerSets` PortName (표시 `PortName (Name)`)
- [ ] Channel 콤보 items = 선택 Controller Channels 풀만
- [ ] Controller 변경 → Channel 콤보 재구성 + 풀 밖 값이면 첫 채널 reset
- [ ] 단일 결선 → Controller 자동+readonly (확인 #4)
- [ ] 0개 결선 → 패널 비활성 + 안내

## E. Recipe UI — 결선 헤더 + 프리셋
- [ ] 결선 헤더(읽기전용) 다중 표시 "COM1 (Main) / 풀:[3,4,5,6]" 줄별
- [ ] "변경은 Setup 에서" 링크 라벨
- [ ] "현재 프리셋: {파일명}" 표시 (Vision 단일 파일 — 형식 확인 #5)
- [ ] Port/Baud/ChannelCount/MaxPower 등 기구적 컨트롤 미존재(grep)

## F. Apply — 컨트롤러별 group + Task.WhenAll
- [ ] Settings 를 ControllerPort 로 group
- [ ] group 마다 (ctrl, ControllerSet) 조회 → `SetChannelBatchAsync(page, times)` Task
- [ ] `await Task.WhenAll(tasks)` 병렬
- [ ] ctrl Hub 미등록 → `LIGHT-MAP-INVALID` + 그 ctrl skip, 나머지 계속
- [ ] wiring 누락 → `LIGHT-WIRING-MISS` + group skip
- [ ] 풀 밖 채널 → `LIGHT-CHANNEL-OUT-OF-POOL`

## G. Cascade — 인벤토리 삭제
- [ ] 삭제 시 그 포트 가진 ControllerSets 전부 찾기
- [ ] 다이얼로그 "결선 N건 함께 비웁니다" 확인
- [ ] ControllerSets 항목 제거 + Recipe InspectionLightSetting 매칭 행 처리 (확인 #3)

## H. 동기화
- [ ] InspectionLightPanel 진입 시 LightSystemSetup 도 Load
- [ ] Setup 변경 후 Recipe 다음 진입 자동 reload
- [ ] 풀 밖 채널 빨강 + `LIGHT-CHANNEL-OUT-OF-POOL`

## I. 검증
- [ ] MSBuild warning 0 / error 0
- [ ] verify_all 회귀 0 (현재 82/82)
- [ ] 신규 verify:
  - [ ] AlgorithmLightWiring 직렬화에 옛 ControllerPort/Channels 키 없음 (정규화 후)
  - [ ] 구버전 JSON 라운드트립: 단일 → ControllerSets[0]
  - [ ] 다중 컨트롤러 JSON 라운드트립: 2항목 보존
  - [ ] InspectionLightSetting.ControllerPort 라운드트립
  - [ ] LightSystemSetupPage 에 Level/Brightness/TrackBar 미존재(grep)
  - [ ] InspectionLightPanel 헤더에 프리셋/RecipeName 라벨 존재(grep)
  - [ ] Apply 병렬 — Sim 카운터/timestamp 로 두 ctrl ~동시 송신

## J. 문서
- [ ] `MISMATCH_RESOLUTION_LOG.md` M-80-1~5 (본 Stage 기록)
- [ ] `STAGE80_RESULT_*.md` (구현 Stage)

## K. Git
- [x] 브랜치 `stage-80-multi-ctrl-ui-separation-spec`
- [ ] SPEC + CHECKLIST + MISMATCH 첫 커밋 (문서만)
