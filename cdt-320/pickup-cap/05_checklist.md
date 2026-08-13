# CDT-320 구현 체크리스트 — 언로더 배출 픽업 캡(Good 빈손 보장)

- 작성일: 2026-08-13
- 선행 문서: [02_code_analysis.md](02_code_analysis.md)
- 수정지시 프롬프트: `..\..\언로더배출_픽업캡_Good빈손보장_수정지시_프롬프트_2026-08-13.md`
  (프롬프트 §7과 동일 내용 — 함께 갱신할 것)
- 확정 사항: Good pending 기준 + 선착순 / 빈손 보장은 Good 배출만 / 교차 웨이퍼 근절 안 함 /
  방어 게이트는 경고만 / 보유 대기 경로 유지+경고

## 1. 코드 편집
- [ ] `MaterialStateService.OutputReceive.cs` — `CountPendingOutputReceiveSlotsNoLock(side)` 신설
      (IsOutputReceiveSlotPending 재사용, 슬롯 없는 웨이퍼는 total−placed 폴백 산식 일치)
- [ ] `MaterialStateService.InputPick.cs` — `ResolveOutputGoodNewPickAllowanceNoLock` 신설
      - pending = Good 스테이지 pending 슬롯 수
      - held = 픽커 위 IsInputTarget 다이 수 (Front+Rear, non-target 제외)
      - reserved = IsDieReservedForPicker && CurrentLocation==InputStage (양쪽)
- [ ] `MaterialStateService.InputPick.cs` — `ReserveNextInputStagePickTarget` 캡 판정
      - **재발급 경로(:56~62) 뒤, 신규 예약 루프(:65~) 앞**에 배치
      - allowance<=0 → null + 거부 로그(pending/held/reserved/allowance/side/pickerNo)
      - 락 안 판정 = 선착순 원자성 (루프 밖 사전 계산 금지)
- [ ] `MaterialStateService.InputPick.cs` — `GetOutputGoodNewPickAllowance` 공개 조회 API
- [ ] `InputDieVisionPrepareSequence.cs` — BuildPickBatch 배치 축소 계측 로그
      (enabledEmpty/batch/allowance/pending/held/reserved)
- [ ] `OutputSequence.cs` — `ExecuteStoreStageToCassetteAsync`(:1721) 진입부:
      Auto+Good+보유 target 다이 존재 시 **경고만**(AboveNormal, 차단·대기·알람 금지),
      NG·수동 무변경
- [ ] `PickerPlaceSequence.OutputStageReady.cs` — :140 보유 대기 블록 진입 경고 로그 1줄
      (AboveNormal, 동작 무변경)

## 2. 무변경 확인
- [ ] 기존 예약 재발급 경로 캡 미적용 (판정 위치 육안 확인)
- [ ] `TryPublishInputStageExchangeReadyWithoutPickTarget` (Front/Rear) 무변경
- [ ] `OutputSequence.Safety.cs` Avoid 위치 게이트 무변경
- [ ] `AutoSequenceCoordinatorGate` 시작 차단 로직(:1005~1106) 무변경
- [ ] `IsOutputStageReceiveComplete` / `IsOutputReceiveSlotPending` 판정 무변경
- [ ] `UpdateOutputReceiveSlot` / 슬롯 소비 무변경
- [ ] NG 배출 경로 무변경
- [ ] OutputStageReady 보유 대기 동작 무변경(로그만 추가)
- [ ] FIFO 건 파일(`PickerFirstForwardSequencer.cs`, `AutoSequenceCoordinator.cs`) 미접근
- [ ] 신규 모션 코드 없음 (MotionSpeedScale 위반 없음)

## 3. 정적/빌드 확인
- [ ] `/p:OutDir=<임시경로>` 빌드만 사용, 경고 0건
- [ ] `ReserveNextInputStagePickTarget` 호출부 1곳(InputDieVisionPrepareSequence:310) 재확인
- [ ] NoLock 함수의 락 밖 호출 없음 확인

## 4. 시뮬레이션 검증
- [ ] 1. pending 4 + 빈손 + 예약 0 → 배치 4 (캡 미개입)
- [ ] 2. pending 2 + 빈손 → 배치 2 + 축소 로그
- [ ] 3. pending 0 → 배치 0 → 완료 → 교체 후 재개 (교착 없음)
- [ ] 4. 보유 2 + pending 4 → 신규 2만
- [ ] 5. 겹침 예약 2 + pending 4 → 신규 2만
- [ ] 6. Front/Rear 동시 → 합계 allowance 이내 (선착순, 예: 6 → 4+2)
- [ ] 7. NG 1개 → 다음 배치 +1 회복
- [ ] 8. 수동 슬롯 완료로 보유>pending → 보유 대기 경고 + Good 배출 경고 + 배출 진행(무교착)
- [ ] 9. NG 배출 + 보유 다이 → 경고·차단 없음(현행)
- [ ] 10. FIFO 재시작 드레인 조합 → 드레인 후 새 배치 캡 정상
- [ ] 11. UseNgCassette=false → 전량 Good행, 캡 정확
- [ ] 회귀: 용량 여유 정상 생산에서 캡 로그 미발생·배치 크기 불변
- [ ] 회귀: 인풋 교체 타이밍 불변
- [ ] 회귀: Stop After Drain 흐름 불변

## 5. 실장비 시험 전 팀장님 확인
- [ ] 캡 on/off 설정 스위치 필요 여부 답변 (현 설계는 상시 적용)
- [ ] 시나리오 8 시뮬 재현 불가 시 코드 리뷰 대체 보고
- [ ] 변경 파일·함수·라인 목록 최종 고지 및 편집 승인
