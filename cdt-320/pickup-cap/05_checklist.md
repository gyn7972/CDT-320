# CDT-320 구현 체크리스트 — 언로더 배출 픽업 캡(Good 빈손 보장)

- 작성일: 2026-08-13 / **v2 개정: 2026-08-13** — busy-loop 게이트 + 캡 on/off 스위치 추가(팀장님 승인)
- 선행 문서: [02_code_analysis.md](02_code_analysis.md) (§4-4 busy-loop 포함)
- 수정지시 프롬프트: `..\..\언로더배출_픽업캡_Good빈손보장_수정지시_프롬프트_2026-08-13.md`
  (프롬프트 §7과 동일 내용 — 함께 갱신할 것)
- 확정 사항: Good pending 기준 + 선착순 / 빈손 보장은 Good 배출만 / 교차 웨이퍼 근절 안 함 /
  방어 게이트는 경고만 / 보유 대기 경로 유지+경고 / **(v2) HasPickerWork 캡 게이트** /
  **(v2) UseOutputGoodPickupCap 스위치(기본 ON, OFF=기존과 완전 동일)**

## 1. 코드 편집
- [x] `MaterialStateService.OutputReceive.cs` — `CountPendingOutputReceiveSlotsNoLock(side)` 신설
      (IsOutputReceiveSlotPending 재사용, 슬롯 없는 웨이퍼는 total−placed 폴백 산식 일치)
- [x] `MaterialStateService.InputPick.cs` — `ResolveOutputGoodNewPickAllowanceNoLock` 신설
      - pending = Good 스테이지 pending 슬롯 수
      - held = 픽커 위 IsInputTarget 다이 수 (Front+Rear, non-target 제외)
      - reserved = IsDieReservedForPicker && CurrentLocation==InputStage (양쪽)
- [x] `MaterialStateService.InputPick.cs` — `ReserveNextInputStagePickTarget` 캡 판정(스위치 게이트)
      - **재발급 경로(:56~62) 뒤, 신규 예약 루프(:65~) 앞**에 배치
      - allowance<=0 → null + 거부 로그(pending/held/reserved/allowance/side/pickerNo)
      - 락 안 판정 = 선착순 원자성 (루프 밖 사전 계산 금지)
- [x] `MaterialStateService.InputPick.cs` — `GetOutputGoodNewPickAllowance` 공개 조회 API
- [x] `InputDieVisionPrepareSequence.cs` — BuildPickBatch 배치 축소 계측 로그(스위치 게이트)
      (enabledEmpty/batch/allowance/pending/held/reserved)
- [x] `OutputSequence.cs` — `ExecuteStoreStageToCassetteAsync`(:1721) 진입부:
      Auto+Good+보유 target 다이 존재 시 **경고만**(AboveNormal, 차단·대기·알람 금지,
      스위치 게이트), NG·수동 무변경
- [x] `PickerPlaceSequence.OutputStageReady.cs` — :140 보유 대기 블록 진입 경고 로그 1줄
      (AboveNormal, 동작 무변경, 스위치 게이트)
- [x] **(v2)** `FrontPickerSequence.cs` — `HasPickerWork` 캡 게이트(스위치 게이트):
      actionable && (자기 side 예약 잔존 OR allowance>0)만 진입.
      **OR 조건 생략 금지**(마지막 슬롯이 전부 자기 예약분이면 교착 — 분석 §4-4).
      차단 시작/해제 **전이 시에만** 로그(20ms 평가마다 금지)
- [x] **(v2)** `RearPickerSequence.cs` — 동일 게이트 (Front와 완전 대칭)
- [x] **(v2)** `AppSettings.cs` — `[DataMember] bool UseOutputGoodPickupCap = true` 추가
      + 기본값 초기화 메서드 반영
- [x] **(v2)** `GeneralPage(.Designer).cs` — 스위치 UI 항목(WaferCompleteRunMode 패턴),
      런타임 참조는 `AppSettingsStore.Current` 직접 읽기(캐시 금지)

## 2. 무변경 확인
- [x] 기존 예약 재발급 경로 캡 미적용 (판정 위치 육안 확인)
- [x] `TryPublishInputStageExchangeReadyWithoutPickTarget` (Front/Rear) 무변경
- [x] Front/RearPickerSequence — HasPickerWork(+전이 로그 필드) 외 무변경
      (`YieldInputPickupPriorityToFrontAsync`·`WaitForPickerWorkAsync` 무변경)
- [x] `OutputSequence.Safety.cs` Avoid 위치 게이트 무변경
- [x] `AutoSequenceCoordinatorGate` 시작 차단 로직(:1005~1106) 무변경
- [x] `IsOutputStageReceiveComplete` / `IsOutputReceiveSlotPending` 판정 무변경
- [x] `UpdateOutputReceiveSlot` / 슬롯 소비 무변경
- [x] NG 배출 경로 무변경
- [x] OutputStageReady 보유 대기 동작 무변경(로그만 추가)
- [x] FIFO 건 파일(`PickerFirstForwardSequencer.cs`, `AutoSequenceCoordinator.cs`) 미접근
- [x] AppSettings 기존 항목·저장 방식 무변경(항목 1개 추가만)
- [x] 신규 모션 코드 없음 (MotionSpeedScale 위반 없음)

## 3. 정적/빌드 확인
- [x] `/p:OutDir=<임시경로>` 빌드만 사용, 수정 파일 신규 경고 0건
- [x] `ReserveNextInputStagePickTarget` 호출부 1곳(InputDieVisionPrepareSequence:310) 재확인
- [x] NoLock 함수의 락 밖 호출 없음 확인
- [x] **(v2)** HasPickerWork OR 조건(자기 예약 잔존) 존재 육안 확인
- [x] **(v2)** 스위치 OFF 시 전 신규 경로 우회(기존과 완전 동일) 육안 확인

## 4. 시뮬레이션 검증
- [ ] 1. pending 4 + 빈손 + 예약 0 → 배치 4 (캡 미개입)
- [ ] 2. pending 2 + 빈손 → 배치 2 + 축소 로그
- [ ] 3. pending 0 → 차단 전이 로그 1줄 → **유휴 대기(재진입 사이클 없음)** → 교체 후 해제·재개
- [ ] 4. 보유 2 + pending 4 → 신규 2만
- [ ] 5. 겹침 예약 2 + pending 4 → 신규 2만
- [ ] 6. Front/Rear 동시 → 합계 allowance 이내 (선착순, 예: 6 → 4+2)
- [ ] 7. NG 1개 → 다음 배치 +1 회복
- [ ] 8. 수동 슬롯 완료로 보유>pending → 보유 대기 경고 + Good 배출 경고 + 배출 진행(무교착)
- [ ] 9. NG 배출 + 보유 다이 → 경고·차단 없음(현행)
- [ ] 10. FIFO 재시작 드레인 조합 → 드레인 후 새 배치 캡 정상
- [ ] 11. UseNgCassette=false → 전량 Good행, 캡 정확
- [ ] **(v2)** 12. 자기 예약 잔존 + allowance=0 → 진입 허용·예약 픽업·스테이지 완료(무교착)
- [ ] **(v2)** 13. 스위치 OFF → 배치 제한/차단/경고/캡 로그 전무, 기존 동작 완전 동일
- [ ] **(v2)** 14. 스위치 UI 변경 → 저장·반영·영속 확인
- [ ] 회귀: 용량 여유 정상 생산에서 캡 로그 미발생·배치 크기 불변
- [ ] 회귀: 인풋 교체 타이밍 불변
- [ ] 회귀: Stop After Drain 흐름 불변
- [ ] 회귀: Rear 양보 로직 불변

## 5. 실장비 시험 전 팀장님 확인
- [ ] 시뮬 재현 불가 항목(8·12) 코드 리뷰 대체 보고
- [ ] UPH 영향 실측 계획(웨이퍼 경계 손실 시간) 보고
- [x] 변경 파일·함수·라인 목록 최종 고지 및 편집 승인
