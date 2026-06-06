# Stage 30 — OutputStageUnit 시퀀스 통합

## 1. 현황

OutputStageUnit:
- GoodStage / NgStage (StageModule): StageY + StageZ 각각
- BinCameraX axis
- 메서드:
  - `ReceiveDieAsync(req)` — 충돌 회피 + Z up + Y 이동 + Tpu 신호
  - `InspectBinPositionAsync()` — BinCameraX 작업 위치 + 비전 + 후퇴
  - `RequestWaferChangeAsync(grade)` — Unloader 위임 (Stage 27 어댑터로 활성)
  - `PerformColletCleaningAsync()` — NgStage 더미 영역

현재 사이클 (DoOneDie):
- OutputStage 호출 0건
- ArmX 1200mm 이동만 (Place 위치)

## 2. 작업 계획

### A. SoftLimit
- StageY/Z 각 200mm 이상 사용 가능성 확인
- BinCameraX

### B. DoOneDie 통합
- Place 전: OutputStage.ReceiveDieAsync(grade=isGood ? Good : Ng)
- Place 후: OutputStage.InspectBinPositionAsync (선택)

### C. NullTpuUnit 안전 처리
- 그대로 둘 수도 (TpuArmUnit 와 별도)

## 3. 체크리스트

| ID | 항목 | 검증 |
|----|------|------|
| A1 | StageModule.StageY/Z SoftLimit 확장 | 코드 |
| B1 | DoOneDie ReceiveDieAsync 호출 | grep |
| B2 | DoOneDie InspectBinPositionAsync 호출 | grep |
| F1 | Build clean | warning 0 |
| F2 | verify_all PASS | 회귀 |
| F3 | auto-cycle OutputStage 시퀀스 동작 | 로그 |
