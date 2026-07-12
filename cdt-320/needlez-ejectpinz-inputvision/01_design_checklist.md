# NeedleZ 공정 중 유지 / EjectPinZ 대기(Avoid) 하강 수정 — 설계 및 체크리스트

- 작성일: 2026-07-12
- 지시: 팀장님 — "니들Z는 공정 시작 후 공정 위치에서 움직이지 않는다. 인풋비전 촬영 시
  니들Z가 Avoid로 내려가는 잘못된 수정을 고치고, 이젝트핀을 대기 위치로 내려라.
  사이드 이팩트 점검해서 공정 중 알람이 없게 하라."
- 사전 조사: 멀티에이전트 5차원 전수 조사 (축 생애주기 / EjectPinZ 계약 / 인터락 /
  호출자·이력 / 공정 순서) — 결과는 본 문서 근거에 인라인 반영

## 배경 사실 (조사 확정)

1. **잘못된 수정의 위치** (모두 NeedleZ를 공정 중 하강시키는 코드):
   - `InputDieVisionPrepareSequence.cs` — `EnsureNeedleZSafeForCurrentStageTravelAsync`
     (1392행, 커밋 b3829315 도입) + `EnsureNeedleZProcessForVisionAsync`(1328행, 재상승).
     호출: 422/454/475/1491행
   - `PickerPickUpSequence.cs` — `EnsureNeedleZAvoidAndVacuumOffSettledBeforeXYAsync`
     (4566행, 커밋 34187778 도입) — **매 die 픽업 XY 이동 직전마다 NeedleZ Avoid 하강**.
     호출: 1764(ContiNode)/7197(default transfer)행
   - `PickerPickUpSequence.cs` 내부 비전 루프 사본(755/790/820/6450행 호출,
     6674/6738행 정의) — BuildPickBatch가 항상 외부 시퀀스로 위임 후
     CalculatePickTargets로 점프하므로 **현재 도달 불가(데드코드)**이나 동일 패턴 유지 시
     재활성화 위험이 있어 함께 수정
2. **올바른 계약의 선례**: 픽업 완료 복귀 `MoveEjectPinZToAvoidKeepNeedleZAsync`(7781행),
   `MovePickerEjectPinZToAvoidKeepNeedleZAsync`(5515행) — "NeedleZ teaching 고정,
   EjectPinZ만 왕복" 주석 명시(4382행)
3. **NeedleZ 생애주기(수정 후)**: 웨이퍼 로드→얼라인→매핑이 NeedleZ를 Avoid로 내린 채
   종료 → 첫 픽업의 `PrepareNeedlePinZForPickAsync`(4391행)가 pick teaching으로 상승
   (첫 픽은 항상 default 경로, conti는 _pickCursor>0 필요) → 이후 공정 내내 유지 →
   웨이퍼 교체(`PrepareUnloadWaferAsync` 2574행)/실패 리커버리에서만 하강 (공정 외, 유지)
4. **인터락 (수정 불필요 — 인터락 파일 무변경)**:
   - StageY/NeedleX 이동: EjectPinZ 0 이하 또는 Avoid 요구 (기존 규칙, 이번 수정과 정합)
   - NeedleZ 상승 상태 이동: 현재/목표 Needle 작업점이 작업원(반경 기본 125mm) 안이면
     허용. 비전/픽 목표는 기존에 작업원 검사를 통과해야 하므로 정상 공정 알람 없음
   - EjectPinZ→Avoid 이동: 작업영역 검사 면제(targetAtAvoid), NeedleZ 높이 무관 허용
   - 검증된 실행 경로: 픽업 시퀀스가 같은 인터락 경로로 EjectPinZ Avoid를 이미 수행 중
5. **"대기 위치" 구현**: `Recipe.EjectPinZ.AvoidPosition` (UI 라벨 "AVOID POSITION").
   ReadyPosition은 레거시 TPU 경로 전용이며, StageY/NeedleX 인터락이 0/Avoid만
   허용하므로 Avoid가 유일하게 정합적인 대기 위치

## 수정 내용

### A. InputDieVisionPrepareSequence.cs
- [x] A1. `EnsureEjectPinZAvoidForStageTravelAsync` 신설 — 이미 Avoid면 무동작(no-op),
      하강 필요 시 Needle Vacuum OFF 확인 후 EjectPinZ→Avoid, 도달 확인
- [x] A2. 422행 호출 교체 (Motion Only Test 분기)
- [x] A3. 454행 호출 교체 (본 경로)
- [x] A4. 475행 `EnsureNeedleZProcessForVisionAsync` 호출 제거 (NeedleZ 재상승 삭제)
- [x] A5. 1491행 호출 교체 (Motion Only StageY 이동)
- [x] A6. 두 NeedleZ 함수 정의 삭제 (1328–1453행)

### B. PickerPickUpSequence.cs
- [x] B1. XY 게이트(4566행)에서 NeedleZ Avoid 이동 제거 → EjectPinZ Avoid로 교체,
      Vacuum OFF + settle 로직은 보존. 함수명
      `EnsureEjectPinZAvoidAndVacuumOffSettledBeforeXYAsync`로 변경, 알람 코드
      PICKER-PICKUP-EJECTPIN-XY-SAFE-* 로 정리
- [x] B2. 호출 2곳(1764/7197) 이름·설명 갱신
- [x] B3. "게이트로 이관" 로그 문구(NeedleZ 언급) 실제 동작에 맞게 갱신
      (+3211행 완료 로그의 needleZAvoid 잔여 참조도 needleZActual(유지)로 수정)
- [x] B4. 내부 비전 루프: 755/790/6450행 → 기존 `EnsureEjectPinZAtAvoidBeforePickStageMoveAsync`
      재사용, 820행 NeedleZ Process 재상승 제거
- [x] B5. 내부 사본 함수 정의 삭제 (6674–6796행)
- [x] B6. `ResolveNeedleZAvoidTarget` 잔여 사용처 확인 — 리커버리 경로
      (5848 레거시 Z Avoid 복귀, 5925 실패 복구) 2곳만 남음, 의도대로 유지

### C. 1차 적대적 리뷰(11에이전트)가 발견한 위험의 보강 — PickerPickUpSequence.cs
- [x] C1+C2. **(critical/warning 해소, 2차 검증 후 재배치)** Conti 동시 StageY/NeedleX
      이동의 L코너 인터락 간헐 차단 + NeedleZ 비목표 높이 진입 시 접촉 후 늦은 알람:
      신설 `CanRunContiConcurrentXyWithNeedleZKept`(NeedleZ 무이동중 + NeedleZ≈픽업목표 +
      양쪽 L코너 작업원 검사)를 **conti 적격성 게이트 `CanUseContiSegmentedPickUpFromCurrentPosition`
      끝에서 호출** — false면 default 순차 경로 폴백(무알람). 1차 배치(1678행 알람 경로의
      공용 함수)는 폴백이 아니라 알람을 만든다는 2차 적대적 검증 지적에 따라 원복 후 재배치.
      좌표 조합·볼록성 안전성은 인터락(VerifyNeedleZRaisedXyMoveInNeedleWorkArea)과 일치 검증 완료
- [x] C3. **(warning 해소)** conti가 Needle Vacuum OFF 상태로 Contact/SyncLift 진행하던
      순서 모순(수정 전에는 conti가 SyncLift 시작 검사에서 항상 알람이라 미노출):
      XY 전송 완료 후 Contact/EjectPinZ 상승 직전 `EnsureNeedleVacuumOnForPick` 추가.
      또한 conti 진입부의 `VacuumOnBeforePickAsync`(니들 진공 ON→게이트 즉시 OFF 낭비 펄스,
      매 사이클 settle 대기 유발)를 Picker Vacuum 단독 ON으로 교체 — 니들 진공은 Contact
      직전에만 ON

### 검증
- [x] V1. Clean Rebuild (별도 obj) Handler 성공 (EXIT=0, 기존 CS0162 경고 4건만) — 보강 후 재확인
- [x] V2. NeedleZ 이동 명령 grep 재확인 — 두 파일에서 공정 중 NeedleZ 이동 0건
      (잔존: 픽업 Z모션 teaching 상승 4391행, 리커버리 하강 5848/5925행 계열)
- [x] V3. 멀티에이전트 적대적 리뷰 1차(11에이전트) — critical 1건/warning 5건 발견,
      critical과 warning 2건은 C1~C3으로 보강, 나머지는 실장비 검증 항목으로 분류
- [x] V3b. 보강분 적대적 재검증 2라운드 완료 — 1라운드(3에이전트)가 가드 배치 오류를
      적발(corner-parity verdict=fail), 재배치 후 2라운드(1에이전트 정밀 델타 검증)에서
      전 항목 통과: 원복 무결(HEAD 동일), 신설 가드는 적격성 게이트에서만 호출·무알람 폴백,
      적격성→XY 명령 사이 상태 불변으로 인터락 통과 보장, 진공 교체 부작용 없음,
      1678행 알람은 도달 불가 방어 검사로만 잔존
- [x] V4. 인터락 파일 무변경 확인 (git diff: Sequencing/Picker 두 파일만)

## 실장비 검증 필요 항목 (코드 보강 불가/불필요, 팀장님 확인 요망)

1. **비전 촬영 시 NeedleZ 높이 조건 변화**: 기존에는 매 비전 전 NeedleZ를 Process로
   올렸으나, 이제 웨이퍼 교체 후 첫 비전 배치는 NeedleZ=Avoid 상태, 이후는 pick teaching
   유지 상태로 촬영됨. NeedleZ가 필름을 지지하는 구조라면 초점/보정값이 달라질 수 있음
   → 첫 배치와 이후 배치의 비전 결과 비교 확인 필요
2. **die 목표 좌표가 티칭 안전 위치(StageY Avoid/Ready/Load/Unload ±0.05)와 우연 일치**하면
   NeedleZ 상승 상태 이동이 차단될 수 있음(기존 인터락 규칙) — 티칭 값이 die 좌표 범위와
   겹치지 않는지 확인
3. **비정상 상태(수동 개입 후 NeedleZ 상승+작업원 밖)**에서 이전에는 NeedleZ 자동 하강으로
   조용히 복구됐으나, 이제 인터락 알람으로 표면화됨 — "NeedleZ 무이동" 정책의 의도된
   결과이나 리커버리 절차 숙지 필요
4. **정상 픽업 완료 경로 중 레거시 스텝(MovePickerZToAvoid)** 경유 시 NeedleZ가 여전히
   하강함(diff 밖 기존 코드, 현행 정상 루프는 keep-NeedleZ 경로 사용). "픽업 후에도 절대
   하강 금지"로 해석해야 한다면 별도 지시 필요

## 잔여 리스크 (기존부터 존재, 이번 수정으로 새로 생기지 않음)

1. NeedleZ 상승 + 현재 작업점이 작업원 밖(수동 개입/알람 복구 후)이면 StageY/NeedleX
   이동이 인터락 차단됨 — 기존에는 자동 하강으로 가려졌으나 이제 알람으로 표면화.
   이는 "공정 중 NeedleZ 무이동" 정책의 의도된 결과 (작업자 복구 필요 상태)
2. L자 경로 코너가 작업원 밖인 먼 die 간 이동 시 IN-STAGE-NEEDLE-XY-PATH 알람 가능
   — NeedleZ 상승 상태 이동의 기존 규칙 (수정 전에도 비전 이동은 동일 조건)
3. 픽 teaching(NeedlePinReadyPosition)과 Recipe ProcessPosition이 다르면 NeedleZ는
   teaching에 유지됨 — 비전 촬영은 상부 카메라라 NeedleZ 높이 무관
