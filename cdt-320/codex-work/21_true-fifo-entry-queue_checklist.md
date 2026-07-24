# 체크리스트 — 진짜 전역 FIFO 진입 큐 (라이브락 근본 수정, ②안)

작성일: 2026-07-24  /  사용자 승인: ②안(진짜 FIFO) + 실제 시뮬 운전 검증 위임
설계/검증: wf_53ea8b3c (조사→설계→데드락/라이브락/기아 3방향 적대 공격→보강). 원설계 치명
결함 2건(B1 워크존 head-게이트 데드락, B2 티켓 orphan 기아) + MAJOR 3건을 R1~R4로 제거.

## 원인 (라이브락 확정)
side별 두 seq 카운터 비교가 FIFO가 아님. mine=none이면 상대를 무조건 foreign 판정 → 두 선행검사가
서로 양보(21:30:08 Rear양보 foreign=Front14 / 21:30:13 Front양보 foreign=Rear15) → 무한정지.

## 설계 핵심
단일 Interlocked 단조 정수 티켓 큐 → 최소 티켓 유일 → head 정확히 1개 → 상호양보 구조적 불가.
순서 게이트는 '카메라존 admission(어떤 배타 자원도 안 쥔 lock-외 대기)' 한 곳에만. 워크존엔
head-게이트 미도입(PickUp은 at-most-one 허가+phase 상호배제로 이미 직렬화).

## 구현
- [x] Q1. InputEntryQueue.cs 신설(정적, 자체 lock 최내측). Interlocked _ticketCounter,
  Dictionary<Side,Entry{Ticket,Side,Kind,EnqueuedAt}>. API: Enqueue(멱등)/IsHead(유일최소 순수읽기)
  /Dequeue/HasTicket/Describe. csproj 컴파일 항목 추가.
- [x] Q2. Enqueue 지점 = InputCameraPreInspectionCoordinator.EnsureStarted, Running 삽입과 같은
  Sync 임계구역(:66). 카메라존 양보 대기(선행검사 Task 내부)보다 선행 발급 → 대기 중 항상 자기
  티켓 보유(mine=none 소멸).
- [x] Q3. Dequeue R2(중앙집중 원자 반납):
  (a) InputCameraPickUpPermissionStore.TryConsume 성공(Remove와 같은 Sync)에 Dequeue.
  (b) Store.Clear(side)의 Remove와 같은 Sync에 Dequeue → 우회 4곳(PickerProcess:1058,
      PickerPickUp:8242, Front/RearPickerSequence) 자동 정리.
  (c) Coordinator.RunPreInspectionAsync finally: !HasPermission(side) → Dequeue(NoTarget/Fail/
      Cancel 모든 완료 경로 스윕, 유령 head 제거).
  (d) 롤백 재-Grant(PickerPickUp:636→Store.Grant)는 허가 재생존이므로 Grant에 Enqueue(멱등)
      재삽입 — 새 티켓이어도 head 유일이라 무한정지 없음.
- [x] Q4. Store 정리: Seq/_seqCounter/Grant seq 삭제, IsHeadSide/IsHeadSideNoLock/TryConsume
  head-check 삭제(순서는 InputEntryQueue로 이관). HasForeignPermission은 seq-free '다른 side에
  살아있는 허가 존재'로 축소 유지(물리 인터락용). Grant에 Enqueue(멱등) 추가.
- [x] Q5. 카메라존 admission 게이트 IsHead 교체:
  (a) InputCameraMarkInspectionSequence.WaitUntilNoForeignPickUpPermissionAsync: HasForeignPermission
      → !InputEntryQueue.IsHead(Side). bounded timeout 유지.
  (b) AutoSequenceCoordinatorGate.WaitAndSetCameraWorkZoneAsync preInspectionSide 분기:
      HasForeignPermission → !IsHead.
- [x] Q6. R4/B5 타임아웃: WaitAndSetCameraWorkZoneAsync 외곽 획득 루프(preInspectionSide일 때)에
  경과시간 bounded timeout 신규 → head의 물리-clear/CanSet 무제한 대기를 예외로 전환 →
  AcquireInputCameraWorkZoneAsync가 Fail → finally Dequeue → 알람(무언정지 아님).
- [x] Q7. 유지(변경 금지): InputDieVisionPrepareSequence VisionX-Avoid 대기는 HasForeignPermission
  (허가-생존) 유지, IsHead로 교체 금지(B3). 워크존(Gate:478)엔 head-게이트/Enqueue 미도입(B1/B4).
  #17 follow-entry, Output #18, front-pending 우선, PickUp phase 상호배제 전부 유지.

## 검증 (완료 2026-07-24)
- [x] 빌드 통과(build22).
- [x] FIFO 큐 하네스 14/14: Enqueue 멱등/순서, IsHead 유일(T2 상호양보 불가=head 정확히 1개),
  Dequeue 후 승격, 롤백 재삽입 무한정지 없음(T7), 빈 큐.
- [x] 기존 하네스 회귀: follow-entry 22/22.
- [x] **실제 시뮬 Auto 운전 검증(커밋 fa3c6a60, D:\CDT-320 배포 후 22:41~22:46 5분+ 연속 운전)**:
  무언정지 재발 0. 로그 정체 매 스냅샷 0초, FIFO 대기≈head도달(즉시 승격, 상호양보 소멸),
  타임아웃/교착 알람 0건, 사이클 수백 건 연속 진행. 지난 실패(21:30 상호양보→30초 타임아웃→
  무언정지)와 정반대. 백업: D:\CDT-320\QMC.CDT-320.exe.bak_a1ef1807.
