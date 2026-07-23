# 체크리스트 — OutputFeeder Stage→Cassette 정렬 조건 수정

작성일: 2026-07-23

## 현상과 근거

- 최신 로그에서 GOOD 제품 인출은 `GoodWaferUnloadAvoidPosition=501.367`을 거쳐
  `GoodWaferUnloadPosition=496.072`에 정상 도착한 뒤 완료됐다.
- 다음 `OutputFeederUnloadToCassetteSequence`는 CassetteUnload 이동 직전에 일반
  `AvoidPosition=0.000`을 요구하여 `OUT-FEEDER-CST-ALIGN-FEEDER-POS`를 발생시켰다.
- 2026-07-22 정상 로그는 `StageUnload=496.072 → CassetteUnload=-40.000 →
  Unclamp → Avoid=0.000` 순서로 완료됐다.

## 승인된 수정 계약

- [x] P1. 기존 물리 순서 `StageUnload → CassetteUnload → Unclamp → Avoid`를 유지한다.
- [x] P2. CassetteUnload 이동 전 요구 위치를 일반 Avoid가 아니라 해당 측의 정확한
  StageUnload 위치로 변경한다.
- [x] P3. GOOD/NG 모두 각 Recipe의 StageUnload/CassetteUnload Teaching 값을 사용한다.
- [x] P4. Servo ON, Alarm OFF, Moving OFF, InPosition ON, Overload OFF와
  Actual/Command 목표 공차 조건을 유지·확인한다.
- [x] P5. Clamp, Lift Down, Ring 감지, Stage Unload, Cassette offset 및 Material 검증은
  제거하거나 완화하지 않는다.
- [x] P6. CassetteUnload 도착 후 검사와 Unclamp 후 Avoid 복귀 검사는 유지한다.
- [x] P7. LoadToStage, UnloadFromStage, Manual 동작과 Teaching 값은 변경하지 않는다.

## 구현 체크리스트

- [x] I1. boolean 위치 인자를 명시적인 `StageUnload/CassetteUnload` 요구값으로 교체한다.
- [x] I2. 첫 번째 선행검사는 `StageUnload`, 이후 두 검사는 `CassetteUnload`를 요구한다.
- [x] I3. 요구 위치의 Actual과 Command가 모두 동일 Teaching target 공차 안인지 검사한다.
- [x] I4. 실패 메시지에 요구 위치명, target, side, 현재 축 상태를 기록한다.
- [x] I5. 신규 이동 Step이나 인터락 우회 경로를 추가하지 않는다.

## 검증 체크리스트

- [x] V1. 호출부 3곳이 각각 `StageUnload`, `CassetteUnload`, `CassetteUnload`인지 정적 확인한다.
- [x] V2. StageUnload에서 Clamp+LiftDown+Ring 조건이면 CassetteUnload 이동 전 검사가 통과하는지 검토한다.
- [x] V3. 일반 Avoid 또는 다른 위치에서 같은 Step에 진입하면 차단되는지 검토한다.
- [x] V4. Servo OFF, Alarm, Moving, InPosition OFF, Overload이면 이동 전에 차단되는지 검토한다.
- [x] V5. Actual 또는 Command가 target 공차 밖이면 이동 전에 차단되는지 검토한다.
- [x] V6. CassetteUnload 도착 전 Unclamp가 실행되지 않고, Unclamp 후 Avoid 확인이 유지되는지 검토한다.
- [x] V7. GOOD/NG target 분기가 올바른지 검토한다.
- [x] V8. `git diff --check`를 통과한다.
- [x] V9. `QMC.CDT-320.sln /t:Build`를 별도 OutDir로 실행해 컴파일 오류가 없는지 확인한다.
- [x] V10. 실제 장비 동작 시험은 수행하지 않고 현장 검증 항목으로 남긴다.

## 검증 결과 기록 (2026-07-23)

- 정적 검증: 호출부 3곳의 위치 계약과 `StageUnload -> CassetteUnload -> Unclamp -> Avoid` 순서를 확인했다.
- 상태 검증: Servo/Alarm/Moving/InPosition/Overload와 Actual/Command target 공차 조건이 이동 전에 적용됨을 확인했다.
- 회귀 범위: `OutputFeederUnloadToCassetteSequence`의 선행 위치 판정만 변경했으며 다른 시퀀스, Teaching 값, 인터락 규칙은 변경하지 않았다.
- 빌드 검증: Debug/Any CPU `/t:Build`를 `_build_check_handler/out/output-feeder-alignment` OutDir로 실행해 오류 없이 완료했다. 기존 소스의 경고는 남아 있다.
- 장비 검증: 수행하지 않았다. 아래 E1~E4는 실제 장비에서 별도로 확인해야 한다.

## 현장 검증 필요 항목

- [ ] E1. GOOD: `StageUnload → CassetteUnload → Unclamp → Avoid` 실제 이동과 Ring 인계 확인.
- [ ] E2. NG: `StageUnload → CassetteUnload → Unclamp → Avoid` 실제 이동과 Ring 인계 확인.
- [ ] E3. StageUnload가 아닌 위치에서 재개할 때 이동이 발행되지 않고 구체적인 알람이 발생하는지 확인.
- [ ] E4. CassetteUnload 도착 전 제품이 Clamp 상태로 유지되는지 확인.
