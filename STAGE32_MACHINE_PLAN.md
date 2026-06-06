# Stage 32 — 설비 단위 (Machine-Level) 시퀀스 통합

## 1. 현황 — Stage 28~31 통합 결과

이미 6개 Unit 모두 사이클에 통합됨:
- ✅ InputLoaderUnit (Stage 26+27): LoadNextWafer / Retract
- ✅ InputStageUnit (Stage 28): LoadAndPrepare + VisionAlign + UnloadWafer
- ✅ TransferPickerUnit (Stage 29): PickerComponent.Pickup/Place + 4-Picker Bottom Vision
- ✅ VisionInspectionUnit (Stage 31): InspectBottomVisionAsync + InspectSideVisionAsync (sim fallback)
- ✅ OutputStageUnit (Stage 30): ReceiveDieAsync + InspectBinPositionAsync
- ✅ OutputUnloaderUnit (Stage 27): StoreFullWafer (매 8 다이) + Init 자동 스캔

## 2. Stage 32 목표

설비 수준 (Machine 전체) 의 시퀀스 — 6 Unit 협조 동작을 정리:

### A. 설비 시작 시퀀스 (Power-On / Init)
- 모든 축 ServoOn + ResetAlarm
- HomeSearch (모든 축)
- Input + Output 카세트 자동 매핑
- 모든 인터락 등록 + 검증
- Lot 미오픈 상태로 Ready 진입

### B. 사이클 시퀀스 (정상 운전)
- LotStorage.OpenLot
- LoadNextWafer → InputStage handoff → VisionAlign
- N 다이 처리 (DoOneDie)
  - InputStage.MoveToDie
  - TPU.Pickup
  - Bottom Vision
  - TPU.Place
  - OutputStage.ReceiveDie
  - OutputStage.InspectBin
- 매 8 다이: OutputUnloader.StoreFullWafer
- 다음 wafer 로드 (LoadNextWafer)
- 모든 다이 완료 시: InputStage.UnloadWafer + LotStorage.CloseLot

### C. 설비 종료 시퀀스 (Shutdown)
- 진행 중 사이클 정지 (CycleStopAsync)
- 모든 축 Stop + Servo 유지
- LotStorage 정리
- AppSettingsStore.Save

### D. 비상 정지 시퀀스 (E-Stop)
- 모든 축 EStop
- 모든 DO Off
- AlarmManager.Raise(Critical)
- CycleStopAsync

## 3. 실제 구현 작업

### Step A. ShutdownAsync 메서드 추가 (없으면)
- MachineController.ShutdownAsync()
- 모든 축 Stop + 카세트 정리

### Step B. EStopAsync 메서드 추가
- MachineController.EmergencyStopAsync()

### Step C. Stage 28~31 결과 통합 검증
- auto-cycle 32 다이 — 4 wafer 처리 시뮬
- 모든 Unit 협조 동작 확인

### Step D. 종합 시퀀스 다이어그램 (PPT)
- Init → Cycle → Shutdown 전체 흐름
- 6 Unit 간 메시지 시퀀스

## 4. 체크리스트

| ID | 항목 | 검증 |
|----|------|------|
| A1 | ShutdownAsync 신규 | grep |
| A2 | EmergencyStopAsync 신규 | grep |
| A3 | 32 다이 auto-cycle 종단 동작 | 로그 |
| A4 | Lot JSON 4 wafer 처리 기록 | 파일 |
| A5 | 회귀 무결성 verify_all PASS | 검증 |
| A6 | PPT 문서 종합 갱신 | 파일 |
