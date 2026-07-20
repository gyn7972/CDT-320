# 체크리스트 — 1. AXM 벨로시티 오버라이드 래퍼 (axm-velocity-override-wrappers.md)

작성일: 2026-07-19 / 확인일: 2026-07-19 (1회차 통과)

- [x] C1. `AjinE\AXM.cs` public 오버라이드 리전에 `ModifyVelocity(int axis, double velocity)` 오버로드 추가 — `AxmOverrideVel` 호출, `AXL.CheckErrorCode` + `int ret = 0; ... return ret;` 패턴 → AjinE\AXM.cs:2147
- [x] C2. `AjinE\AXM.cs`에 `SetOverrideMaxVelocity(int axis, double velocity)` 추가 — `AxmOverrideSetMaxVel` 호출 → AjinE\AXM.cs:2158
- [x] C3. `AjinE\AXM.cs`에 `[Serializable] public enum OverridePositionTarget { Command = 0, Actual = 1 }` 추가 → AjinE\AXM.cs:2121
- [x] C4. `AjinE\AXM.cs`에 `MoveWithVelocityOverrideAtPosition(...)` 추가 — `AxmOverrideVelAtPos(..., (int)target)` 호출 → AjinE\AXM.cs:2171
- [x] C5. 새 메서드 3종에 한국어 XML doc 주석 (구동 중 호출 조건 / MaxVel 선행 설정 / 이동 시작 함수 명시 / target 기준) 반영
- [x] C6. `Ajin\AXM.cs`(미컴파일 미러)에 동일 변경 적용 — Ajin\AXM.cs:1972/1998/2009/2022 (양쪽 +53줄 동일)
- [x] C7. private extern 및 기존 래퍼 무변경 — diff 106 insertions / 0 deletions (추가만 존재)
- [x] C8. 다른 파일 diff 없음 — 변경 파일: AjinE\AXM.cs, Ajin\AXM.cs 2개뿐
- [x] C9. 별도 OutDir `/t:Build` 솔루션 빌드 성공. 경고는 기존 CS0162 4건(AxisInitializePlan/MachineController)뿐 — 신규 경고 없음

## 결과: 전 항목 통과 (재시도 불필요)
