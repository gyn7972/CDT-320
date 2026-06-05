# Stage 31 — VisionInspectionUnit + TPU 비전 시퀀스 통합

## 1. 현황

`VisionInspectionUnit` — 빈 stub (메서드/축 없음). 실제 비전 검사는 분산됨:
- **VisionComm.VisionHub** (TCP 통신) — Wafer / Inspection / Bin 3채널
- **TpuArmUnit.InspectBottomVisionAsync** — 4 picker bottom 비전 (IVisionTpuClient)
- **TpuArmUnit.InspectSideVisionAsync** — 4 면 검사 (90° 회전 포함)

현재 사이클 (DoOneDie):
- VisionComm.VisionHub.Wafer.MatchAsync — Wafer 측 매칭 ✓ (호출됨)
- VisionComm.VisionHub.Inspection.ExposeAsync + InspectAsync — ✓
- VisionComm.VisionHub.Bin.InspectAsync — ✓
- TpuArmUnit.InspectBottomVisionAsync — **호출 0건**
- TpuArmUnit.InspectSideVisionAsync — **호출 0건**

## 2. 계획

### A. TpuArmUnit.InspectBottomVisionAsync 호출
- ArmX 가 Bottom Vision 위치 (700mm) 도달 후 호출
- 4 picker 결과 수신 → 첫 번째 picker (현재 사이클은 1개) 의 OffsetX/Y 사용

### B. TpuArmUnit.InspectSideVisionAsync 호출
- Bottom 검사 후 Side 검사 위치 이동
- 90° 회전 + 4면 촬상

### C. 결과 활용
- Bottom 결과를 OutputStage.ReceiveDieRequest.VisionOffsetX/Y 에 적용
- Side 결과를 inspPass 에 반영

## 3. 체크리스트

| ID | 항목 | 검증 |
|----|------|------|
| A1 | InspectBottomVisionAsync 호출 추가 | grep |
| A2 | Bottom 결과 OffsetX/Y 추출 | 코드 |
| B1 | InspectSideVisionAsync 호출 추가 | grep |
| B2 | Side 결과 inspPass 반영 | 코드 |
| F1 | Build clean | warning 0 |
| F2 | verify_all PASS | 회귀 |
| F3 | auto-cycle 비전 시퀀스 동작 | 로그 |
