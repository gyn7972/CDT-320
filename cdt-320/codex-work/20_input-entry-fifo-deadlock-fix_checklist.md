# 체크리스트 — InputCamera 선행검사 진입 FIFO 큐 + 데드락 절단 + 타임아웃

작성일: 2026-07-24  /  사용자 승인: A안 + "타임아웃도 만들어서 진행"

## 원인 (데드락 진단 워크플로 wf_11f00ecc / 설계 wf_b43716d7 확정)
- Front 선행검사가 InputCamera Input 워크존을 쥔 채, 그 존이 있어야 소비되는 Rear PickUp
  허가가 클리어되길 무한 대기(InputDieVisionPrepareSequence.cs:1273 HasAnyPermission +
  :1298 타임아웃 없는 while(true) 폴링) → Rear는 그 존을 못 얻어 허가 소비 불가 → 순환 대기.
- 허가 저장소(InputCameraPickUpPermissionStore)는 side별 Dictionary, 순서(FIFO) 개념 없음.
- 지렛대: Grant(:623)는 그 side가 카메라 존을 쥐고 검사를 마친 상태에서만 발생 →
  "X가 카메라 존을 쥔 동안 상대 허가는 새로 생길 수 없다"가 코드로 성립.

## 수정 계획 (A안 — 저장소 FIFO 확장 + 카메라 존 획득 게이트 이설 + 타임아웃)
- [x] S1. InputCameraPickUpPermissionStore: Permission에 `long Seq` + static 단조 카운터.
  Grant는 신규 side면 next seq 부여, 동일 side 재발급이면 seq 보존(롤백 순번 유지).
  신규 API: `HasForeignPermission(side, out detail)`(나보다 앞선(seq 작은) 다른 side 허가),
  `IsHeadSide(side)`(이 side 허가가 최소 seq = 큐 head). HasAnyPermission은 유지(시작 게이트).
- [x] S2. 데드락 절단 — InputCameraMarkInspectionSequence.AcquireInputCameraWorkZoneAsync에서
  BeginInputCameraWorkAsync(:284) 호출 '직전'에 WaitUntilNoForeignPermissionAsync 게이트 삽입:
  HasForeignPermission(Side)인 동안 카메라 존을 안 잡고 양보 대기(존 미점유 = lock-ordering
  안전, 상대 픽업이 Input 진입해 허가 소비 가능). ct/CycleStop 정합. **bounded timeout
  = ResolveTimeout()**, 초과 시 Fail(알람) → 무언정지 대신 복구 가능. 선행검사 모드 아니면
  기존대로 조기 return(무변경).
- [x] S3. 방어선 — InputDieVisionPrepareSequence.cs:1273 HasAnyPermission → HasForeignPermission
  (Side) 교체 + while(true) 무한 폴링에 **bounded timeout = ResolveTimeout()** + 초과 시
  Fail(알람). 선행검사 모드에서만 실행되는 조건(:1259)은 유지. 자기-side 허가는 발급 직전
  시점이라 정상 존재 안 함 → foreign만 봄이 사용자 모델("결정된 피커만 진입")과 일치.
  선행검사 모드 무한 continue(기존 :1048 계열 규칙)는 timeout으로 대체.
- [x] S4. 픽업 권한 head-only(belt-and-suspenders) — TryConsume(:46)에 head 체크:
  이 side가 IsHeadSide가 아니면 소비 거부(Remove 안 함) + reason="not head". at-most-one
  불변식상 정상은 항상 head라 무영향. 호출부 PickerPickUpSequence.cs:604는 기존 실패 경로
  재사용(RequireInputCameraMarkInspectionPermission이면 Fail=알람, 아니면 기존 흐름).
- [x] S5. return-follow(#18) 오버랩 게이트(IsVisionReturnFollowGateSatisfied:2375) 무변경 —
  큐 도입 후 상대 선행검사는 S2 게이트에서 존 미점유 대기라 팔로잉 경로에 진입 불가.
  "선행검사 대상 피커만 팔로잉, 상대는 큐 대기"가 구조적으로 보장됨.

## 재발 수정 (2026-07-24, 커밋 fa720b4b 후 시뮬 재발 — PERMISSION-CLEAR-TIMEOUT)
원인(진단 wf_e5e58c4b 확정): S2가 '검사 후 존이 비기를 기다리는 블록 구간'에서 생긴 foreign을
못 막는 비원자 race. Rear가 S2 통과(foreign 부재) → 폴링 블록 중 16ms 뒤 Front가 grant+release
동시 → Rear가 갓 풀린 존을 재검사 없이 획득(foreign=Front:seq=4 존재). 방향만 반대인 동형 교착:
Rear 선행검사=카메라 존 보유+S3 foreign 대기 / Front PickUp=허가 보유+Input 워크존 대기(카메라
존에 막힘). 카메라 존은 정상 상호배제(위반 아님). 승인: A안 (a) — 존 획득을 FIFO에 원자 종속.
- [x] A1. AutoSequenceCoordinatorGate.BeginInputCameraWorkAsync에 optional
  `PickerSequenceSide? preInspectionSide = null` 추가(기존 로더 호출부 MachineController.cs:8414
  무변경). WaitAndSetCameraWorkZoneAsync까지 전달.
- [x] A2. WaitAndSetCameraWorkZoneAsync 폴링 루프(:309~344): 매 폴링에서 lock 진입 전
  (ArePickersPhysicallyClearForCameraZone 검사와 함께) `preInspectionSide.HasValue &&
  InputCameraPickUpPermissionStore.HasForeignPermission(side)`이면 존 승인 안 하고 continue(양보
  로그). Store.Sync를 _pickerWorkZoneGate lock 밖에서 호출 → 중첩 lock 회피. "카메라 존 보유 =
  foreign 부재"가 매 폴링 재검사되는 단일 불변식 → 검사~획득 race 소멸.
- [x] A3. InputCameraMarkInspectionSequence.cs:293 호출부에 Side 전달.
- [x] A4. 기존 S2(WaitUntilNoForeignPickUpPermissionAsync 조기 양보)와 S3(타임아웃 방어선)는
  그대로 유지 — 이중/삼중 안전망. 정상시 foreign 부재라 즉시 통과(회귀 없음).
- [x] A-검증: 데드락 제거 증명 = 카메라 존 상호배제 + A2로 획득 순간 foreign 부재 + 획득 후
  상대는 존 못 잡아 허가 발급 불가 ⇒ 존 쥔 전 구간 foreign 부재 ⇒ S3 blocking 불가 ⇒ 순환 불가.
  롤백 재발급(PickerPickUpSequence:636)도 Rear가 존 쥔 동안 Front가 소비/롤백 자체를 못 하므로
  차단됨. 빌드 + FIFO 하네스 재실행.

## 검증
- [x] 빌드 통과(OutDir 스크래치).
- [x] 데드락 제거 증명 재확인: 카메라 존 단일 상호배제 + Grant는 존 점유 중에만 + S2가 foreign
  없을 때만 존 획득 ⇒ 존 쥔 전 구간 상대 허가 부재 ⇒ S3 대기 blocking 불가 ⇒ 순환 불가.
- [x] 회귀: 수동/비Auto(선행검사 모드 아님 → S2/S3 조기 return), 실비전(허가 흐름 동일),
  비Conti(오버랩 게이트 false), 단일 side(foreign 발생 불가 → 게이트 즉시 통과) 무영향 확인.
- [x] 타임아웃 동작: foreign 대기/방어선 대기가 상한 초과 시 알람 코드로 Fail(무언정지 아님).
- [x] 시뮬 하네스: seq 부여/보존, HasForeignPermission/IsHeadSide, head-only TryConsume 단위 검증.
