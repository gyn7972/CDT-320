# CDT-320 Gap Analysis — 작업 시간/UPH 표시

근거: `01_requirement.md`(요구), `02_code_analysis.md`(코드).

| Req ID | Status | 코드 근거 | 비고 |
|--------|--------|-----------|------|
| R-001 사이클 타임 정확 측정 | ❌ Missing | `WorkMainPage.cs:214-233` (폴링 추정) | 실제 사이클 경계(`MachineController` 6699행 루프)에서 측정 안 함. 폴링 종속·무한 누적 평균 → 부정확 |
| R-002 UPH 일관 계산 | ⚠️ Partial | `WorkMainPage.cs:206-209` | 식은 있으나 소스(`lot.GoodCount`)가 0이라 항상 0. 사이클타임과 분리됨 |
| R-003 생산 카운트 실시간 | ❌ Missing | `Lot.ProcessedDies/GoodCount` 런타임 미증가 | 실측은 `MachineController.CycleDone/GoodCount/NgCount`에 있는데 UI가 안 읽음 → 0 표시 |
| R-004 가동/정지 시간 누적 | ⚠️ Partial | `MachineController` 필드 존재(260-264행) | 상태기반 누적 엔진 부재 의심 → 대부분 00:00:00 |
| R-005 이상횟수/MTBF/MTTR | ⚠️ Partial | `ErrorCount`(258), `Mtbf/Mttr`(266-268) | 필드만 있고 누적 로직 불명확 |
| R-006 가동률(%) | ❌ Missing(오류) | `WorkMainPage.cs:235-238` | 수율(Good/Processed)을 가동률로 잘못 표시 |
| R-007 UI 무지연 갱신 | ✅ Implemented | `WorkMainPage` 스냅샷/diff/가시성 타이머 | 골격은 양호. 단 Build가 컨트롤러 락에 잡히면 위험 → 설계에서 lock-free 스냅샷 보강 |
| R-008 LOT ID 표시 | ✅ Implemented | `WorkMainPage.cs:239` | OK |

## Summary
- Implemented: 2 (R-007, R-008)
- Partial: 3 (R-002, R-004, R-005)
- Missing/오류: 3 (R-001, R-003, R-006)

## 핵심 결론
표시 골격(타이머·스냅샷·diff)은 이미 좋으므로 **다시 만들 필요 없음.**
문제는 전부 **"데이터 소스"** 에 있다:
1. UI가 읽는 카운트 소스(`Lot`)와 실제 카운트 소스(`MachineController`)가 어긋나 있다.
2. 사이클 타임/가동시간/MTBF가 측정·누적되는 단일 지점(엔진)이 없다.

→ 해결책은 **컨트롤러 측에 "생산 통계 엔진"을 하나 두고**, 사이클 루프/상태 전이에서 이벤트를 먹여
정확한 값을 산출한 뒤, UI는 그 엔진의 **스냅샷만 읽어 표시**하는 구조로 정리하는 것.

## Items to design (Stage 4)
- R-001, R-002, R-003, R-004, R-005, R-006 → `04_design.md`
- R-007은 스냅샷 lock-free 보강만, R-008은 현행 유지.
