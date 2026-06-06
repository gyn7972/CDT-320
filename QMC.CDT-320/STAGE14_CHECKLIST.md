# Stage 14 체크리스트 — Cognex 동글 실 검증

## A. 자동 검증 스크립트
- [ ] **A1.** `tools/verify_cognex_runtime.pl` 신규
  - Vision exe 자동 기동 (없을 때)
  - 다음 검증:
    - `WaferVision|TRAIN|ReticleFinder` → ACK
    - `WaferVision|MATCH|ReticleFinder|0` → ACK + score
    - 5회 반복 매칭 → score 변동 ≤ 0.1 (Cognex 안정적), Sim 은 매번 random
    - 매칭 좌표가 image center (320,240) 근처인지

## B. 검증 결과
- [ ] **B1.** Cognex 패스 안정성 — 5회 score 표준편차 < 0.05
- [ ] **B2.** 모든 verify_* 회귀 PASS

## C. 정합성
- [ ] **C1.** PLAN ↔ CHECKLIST 1차
- [ ] **C2.** CHECKLIST ↔ 구현 2차
