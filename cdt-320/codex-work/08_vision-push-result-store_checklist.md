# 체크리스트 — Vision 결과 수신 Push+스토어 전환 (vision-push-result-store.md)

작성일: 2026-07-20 / 확인일: 2026-07-20 (1회차 통과)

## 계획 요약
- 결과 회수 Pull(요청+PENDING 폴링) 경로를 제거하고, 수신 루프가 자발 푸시 MRESULT/RESULT를
  전역 스토어(`VisionInspectionResultStore`)에 보관 → 시퀀스는 `WaitAndConsumeAsync`로 소비.
- 스토어는 **전역 static 1개 + (camera, command, group_id) 복합 키** — 5개 카메라 채널 동일 규약이라
  채널별 인스턴스보다 단순 (BottomXytStore 전역 static과 일관, 주석 명시).
- EPD 경로·legacy FIFO·수동 폴링·Bypass/Sim 무수정.

## 구현
- [x] C1. `VisionInspectionResultStore.cs` 신규 — `Add`(대기자 있으면 직접 전달)/`TryConsume`/`WaitAndConsumeAsync`(등록·재조회를 동일 lock에서 수행해 경합 원천 차단)/`ClearChannel`/`Clear`, lock 스레드 안전
- [x] C2. 보관 한도 500 (미소비 전역 FIFO) — 초과 시 최고령 제거 + Warning(키·수신시각)
- [x] C3. 동일 키 중복 수신 → 최신 교체 + Warning 로그
- [x] C4. `TryRouteInspectionProtocolResponse` 결과 분기 → 스토어 Add. `group_id` 없으면 폐기 + `VISION-PUSH-RESULT-DROP` Warning (FIFO `_pending` 유입 차단). ACK 결과 라인은 기존대로 진단 무시
- [x] C5. `Disconnect()`(`CancelInspectionWaiters` 내) — 모듈→채널→카메라 해석 후 `ClearChannel` (미소비 제거 + 대기자 오류 통지 + Warning)
- [x] C6. `WaitInspectionStageAsync` 내부 교체 — 공개 시그니처/반환/에러·타임아웃 관례 유지, Bypass/Sim 유지. 방어: 규약 외 PENDING 수신 시 Warning 후 잔여 시간 재대기(재요청 없음)
- [x] C7. Pull 잔재 제거 — `RequestInspectionResultAsync`/`_resultWaiters`/`RemoveResultWaiter`/`BuildResultWaiterKey`/`CorrelatedResultPollIntervalMs` 삭제. 삭제 후 저장소 전체 참조 0건 확인 (호출처는 AutoVisionRequestService.Correlated.cs 1곳뿐이었음)
- [x] C8. `VisionRequestHandle` 타이밍 유지 — MarkStageTx=대기 시작, RoundTrip=대기→푸시 소비 (필드명 유지, 주석으로 의미 변경 명시) + `epdToPushMs`(EPD→푸시 수신) 로그 추가
- [x] C9. R5 로그 — 수신 `VISION-PUSH-RESULT-RX`(채널/종류/groupId/잔량), 소비 `AUTO-VISION-CORRELATED-RESULT`(epdToPushMs/storeCount 추가), 한도 초과 `-EVICT`·무효 `-DROP`·단절 `-CLEAR` Warning
- [x] C10. csproj 등록 (`Equipment\Vision\VisionInspectionResultStore.cs`)
- [x] C11. R6 계약 문서 `cdt-320\vision-push-protocol.md` — INSPECT/EPD(기존), MRESULT/RESULT 푸시 canonical 포맷(STATUS 위치·META 필수 키·profile별 필수 값·예시 라인 6종), EPD→MRESULT(BOTTOM만)→RESULT 순서, PENDING·결과요청·결과ACK 폐지 목록

## 검증 (하네스 21개 ALL PASS)
- [x] V1. 솔루션 빌드 통과 (이번 작업 신규 경고 0 — CS0168 1건은 미수정 파일의 기존 경고로 확인), csproj 등록
- [x] V2. 푸시 선도착 — 즉시 반환(1ms) + 소비 후 재조회 실패 ✅
- [x] V3. 대기 후 도착 — 푸시로 기상 + **동시 Add/Wait 300회 반복 무손실** (등록 직전 도착 경합 없음) ✅
- [x] V4. 타임아웃 — 300ms 지정 시 308ms에 null (서비스가 기존 MarkError+알람 경로 수행) ✅
- [x] V5. 소비 후 제거 + 중복 수신 최신 교체 ✅
- [x] V6. 한도 500 — 501번째에 최고령(g-lim-1) 제거, 2번째 유지 ✅
- [x] V7. 단절 정리 — ClearChannel(BOTTOM) 시 BOTTOM 미소비 제거·대기자 예외, BIN 무영향 ✅
- [x] V8. group_id 없는 결과 — 라우터 폐기(+Warning), 스토어 미유입, legacy FIFO `_pending` 미소비(센티널 유지) ✅
- [x] V9. `WaitInspectionStageAsync` 호출처 전수 (전부 시퀀스 무수정으로 동작):
  - `MachineController.cs` 11807/11867 (수동/셋업 correlated 경로)
  - `OutputPostPlaceInspectionQueue.cs` 967/1027 (BIN)
  - `InputDieVisionPrepareSequence.cs` 116/668 (WAFER/Input)
  - `VisionAdapters.cs` 802/804 (FRONTSIDE/REARSIDE Side) + 288/312/344/363/439 (BOTTOM M/Final/Complete → PickerFront/RearUnit → Bottom 시퀀스들)
  - 서비스 내부 `WaitBottomMResultAsync`/`WaitBottomFinalResultAsync`/`CompleteBottomInspectionAsync`/`CompleteSyncMatchHandleAsync`/`RunSyncInspectionAsync` — 전부 `WaitInspectionStageAsync` 경유
- [x] V10. `vision-push-protocol.md` 생성 — 예시 라인·필수 필드 명세 포함 (비전 측 작업 입력 문서)

## 결과: 전 항목 통과 (재시도 불필요)

### 참고
- 실제 비전 PC 연동은 비전 소프트웨어의 푸시 구현(별도 작업) 이후 현장 검증 필요.
  그 전까지 실장비에서 결과 대기는 타임아웃으로 실패한다 (비전이 아직 푸시하지 않으므로) —
  배포 시 비전 업데이트와 함께 반영할 것.
