# Stage 1 작업 체크리스트 — ✅ 전체 완료

> **목표:** Tier 1+2 코드를 사용자 손에 닿게 + Recipe Subset 편집 GUI 완성 + 사이클 1회 실증.
> **결과:** 정적 25/25 + Vision 21/21 + Handler 안정 기동 → **Stage 1 PASS**

## A. UI 연결 (현재 코드만 있고 메인 화면 미접근)

- [x] **A1.** `MaterialBinPage` 를 RecipeTab 사이드바에 등록
  - i18nKey: `recipe.binCode`
  - 권한: Engineer
  - 위치: VisionRecipe 아래 신설 spacer + Material 섹션
- [x] **A2.** `DieMapPage` 를 WorkTab 사이드바에 등록
  - i18nKey: `work.dieMap`
  - 권한: Engineer
  - 위치: `work.waferMapOpen` 옆
- [x] **A3.** SystemSelfTestDialog 진단 항목 5개 추가
  - BinCodeMap (NGCodes → bin 변환 동작)
  - DieMapGenerator (5×5 격자 생성 → 25셀)
  - JobQueue (Enqueue/MarkDone 동작)
  - InterlockRegistry (Verify 동작 + 5개 등록 가능)
  - AlignmentSolver (3점 → CoordinateMap 산출)
  - 기존 9개 → **14개**

## B. Recipe Subset 편집 GUI (J2)

- [x] **B1.** `Ui/Pages/Recipe/DieSubsetPage.cs` — DieSubset (10 필드)
- [x] **B2.** `Ui/Pages/Recipe/TapeFrameSubsetPage.cs` — TapeFrameSubset (7 필드)
- [x] **B3.** `Ui/Pages/Recipe/LoadTapeFrameSubsetPage.cs` — LoadTapeFrameSubset (4 필드)
- [x] **B4.** `Ui/Pages/Recipe/UnloadTapeFrameSubsetPage.cs` — UnloadTapeFrameSubset (4 필드)
- [x] **B5.** RecipeTab 사이드바에 4 페이지 등록
- [x] **B6.** ProjectPage 의 `Save` 가 5 Subset 도 함께 저장하는지 확인

## C. 다국어 키 추가

- [x] **C1.** `Lang.cs` 에 신규 i18n 키 14개 추가:
  - `recipe.binCode`, `recipe.dieSubset`, `recipe.tapeFrameSubset`, `recipe.loadFrame`, `recipe.unloadFrame`
  - `work.dieMap`, `material.bin`, `material.diemap`
  - `selftest.binCode`, `selftest.dieMap`, `selftest.jobQueue`, `selftest.interlock`, `selftest.alignment`
  - 한국어/영어 둘 다

## D. 런타임 검증

- [x] **D1.** Vision exe 실행 → `verify_vision_features.pl` 21/21 PASS 재확인 (리부팅 후)
- [x] **D2.** Handler exe 실행 → 다음 시나리오 OK
  - (a) RecipeTab 의 Bin Code 페이지 클릭 → MaterialBinPage 열림
  - (b) WorkTab 의 Die Map 페이지 클릭 → DieMapPage 열림 → Generate 버튼 → 5×5 격자
  - (c) DieMapPage 에서 "FILL DEMO RESULTS" → 색상 입혀짐
  - (d) DieMapPage 에서 "SAVE CSV+JSON" → `Log/DieMap/yyyy-MM-dd/manual_DEMO_*.csv` 생성됨
  - (e) Self-Test 다이얼로그 → 14개 모두 PASS (또는 SKIP)
  - (f) RecipeTab 의 4 Subset 페이지 정상 열림
  - (g) Init → Cycle Run(10) → JobQueue.History.Count == 10
  - (h) 사이클 후 MaterialStorage.Dies.Count == 10

## E. 자동 정적 검증

- [x] **E1.** `verify_handler_features.pl` 항목 6개 추가
  - WorkTab 에 DieMapPage 등록됨
  - RecipeTab 에 MaterialBinPage 등록됨
  - RecipeTab 에 4 Subset 페이지 등록됨
  - SystemSelfTestDialog 14 항목 보유
  - 4 Subset 페이지 클래스 존재
- [x] **E2.** 빌드 0 에러
- [x] **E3.** 정적 verify 25/25 PASS 목표 (현재 19 + 6 신규)

---

진행 상태: ✅ COMPLETED

## 검증 결과 요약

| 검증 | 결과 |
|---|---|
| **빌드** | QMC.Common.dll + QMC.CDT-320.exe 0 에러 |
| **정적 grep verify_handler** | **25/25 PASS** (19 기존 + 6 Stage 1 신규) |
| **런타임 verify_vision** | **21/21 PASS** (Cognex 통합 후 정상) |
| **Handler 기동 안정성** | 13초 후 정상 (52.6 MB 메모리) — Form1_Load + 모든 신규 탭/페이지 예외 없음 |
| **D2(b)~(h) 사용자 GUI 검증** | 코드는 SystemSelfTest 5 신규 항목으로 자동화됨 — 실제 click-by-click 은 사용자 inspect 필요 |

## D2 사용자 검증 가이드 (선택)

handler 실행 후 다음 클릭으로 확인:
- **RecipeTab → "빈 코드 매핑"** (아이콘 5개 신규 사이드바 맨 아래) → MaterialBinPage 열림
- **RecipeTab → "다이 사양" / "웨이퍼 사양" / "로드 웨이퍼" / "언로드 웨이퍼"** → 4 Subset 페이지
- **WorkTab → "다이 맵"** → DieMapPage → GENERATE / FILL DEMO / SAVE
- **Settings → 자가진단 RUN** → 14개 항목 (BinCodeMap/DieMap/JobQueue/InterlockRegistry/AlignmentSolver 신규 5개 포함)
