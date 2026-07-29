# Side Vision Front/Rear Y — 신규 절대식 설계 (팀장님 확정본)

- 확정일: 2026-07-29 (팀장님 답변 반영)
- 기존 계산(AF BestPosition 우선순위, (X−Y)/2+부호파라미터 폴백, MRESULT OffsetY 0/90 동일가산) **전부 폐기**하고 아래 절대식으로 교체.
- 적용 범위: **통합(Bottom+Side) + 단독 Side 시퀀스 둘 다** (확정).

## 확정 식

```
ΔY4   = 현재피커 Y − 4번피커 Y                  (확정: 현재−4번 방향)
rotY  = COCy − (OffsetX − COCx)                 (CW 90도 회전, COC 중심. 확정: CW)

[0도]
FrontY = ProcessY_F − DieHeight/2 − OffsetY + ΔY4
RearY  = ProcessY_R + DieHeight/2 − OffsetY + ΔY4

[90도]
FrontY = ProcessY_F − DieWidth/2 − rotY + ΔY4
RearY  = ProcessY_R + DieWidth/2 − rotY + ΔY4
```

**2차 확정(2026-07-29, 팀장님 부호 정정): 오프셋 추종 항(OffsetY·rotY·ΔY4)은 Front/Rear 두 카메라에 동일 부호(−Offset, −rotY, +ΔY4)로 공통 적용. 카메라별로 갈리는 것은 반쪽치수 항(Front −, Rear +)뿐.** Front가 −방향, 다이(콜렛) 중앙, Rear가 +방향 배치이므로 다이 위치 추종은 두 카메라가 물리적으로 같은 방향으로 움직인다.

- `ProcessY_F/R`: 각 카메라 `Recipe.{Front|Rear}SideVision.Process0Position` (콜렛 4번 중심면 기준 티칭 전제)
- `DieHeight` = 레시피 `DieSizeY`(세로), `DieWidth` = `DieSizeX`(가로) — InputFrame 우선, Frame, Controller 순 폴백
- `OffsetX/Y`: 해당 다이 Bottom MRESULT (`BottomVisionOffset.OffsetX/OffsetY`)
- `COCx/y`: 콜렛Cal `RotationCenterPixelX/Y` → `PixelToMmOffsetX/Y` 변환값 (검사 중인 피커 쪽 콜렛)
- ΔY4 반영 횟수: **1회** (마지막 +ΔY4 항). 회전은 COC 원값 기준 — "COC Y에 ΔY4 반영 후 회전"과 90도 회전에서 Y성분 결과 동일(중심 Y시프트는 Y결과에 그대로 통과)하므로 중복 방지를 위해 마지막 1회로 구현.
- CW 회전행렬: (x,y)→(y,−x) ⇒ 점 o를 중심 c로 회전한 Y성분 = c_y − (o_x − c_x)

## 변경 목록 (파일·함수)

1. `PickerBottomAndSideInspectionSequence.cs`
   - `BuildSideVisionPositionTarget` → 신규 절대식으로 교체 (카메라2×각도2 = 4값)
   - `ResolveSideVisionBasePosition`, `TryResolveSideFocusFallbackCorrection` → 제거(경로 폐기)
   - `StoreRuntimeSideInspectionCorrection` → OffsetY 단일 저장 → (OffsetX, OffsetY) 벡터 저장
   - `ValidateRuntimeSideInspectionCorrection`, `TryResolveBottomMResultSideVisionOffsetY` → X성분 검증 추가(±7mm 캡 동일)
   - `ResolveSideVisionTargetY` 폴백 정리
   - "SideVisionY 보정 이동" 로그에 P, H/2·W/2, o, c, ΔY4, 최종Y 전 항 출력
2. `PickerFrontUnit.cs` / `PickerRearUnit.cs` — 사이드 보정 저장 구조에 OffsetX 추가 (Set/Get 확장)
3. `PickerSideInspectionSequence.cs` (단독) — 0/90 공용 `MoveBothSideVisionProcess0Position` 이동을 절대식 기반 카메라별 목표 이동으로 교체, `CanSkipSideVisionProcessMove`도 새 목표 기준
4. `VisionUnit.cs` — 무변경 (`MoveVisionAxis` 재사용 → MotionSpeedScale 1회 적용 유지)
5. 콜렛Cal Side AF — 런타임 미사용화만, 시퀀스/데이터는 존치

## 인터락 사전 점검 결과

- SideVisionY 자동/수동 이동 게이트 = Vision Busy 체크뿐 (`VisionInterlockRules.cs:74-83`, InputStage와 독립축 명시)
- `AxisInterferenceMap`의 SideVisionY 그룹은 주석 처리(비활성)
- 실질 차단 지점 = `MoveVisionAxis`의 소프트리밋(VS-SOFT-LIMIT). 신규 목표는 티칭Y 대비 최대 ±(H/2 + 7mm + |ΔY4|) 이탈 → **실장비 양 축 소프트리밋 여유 확인 필요**

## 확정 사항 (2026-07-29 "ㅇㅇ 이거 수정 해라" 승인, 구현 완료)

- A. ΔY4 = 콜렛Cal `FinalPickerY`(현재) − `FinalPickerY`(4번). 방향 확정: **현재 − 4번**. 콜렛Cal(FinalPickerY/COC) 무효 시 **검사 차단**(명확한 사유 알람).
- B. 단독 시퀀스에서 Bottom 결과 없는 다이: o=(0,0)으로 기하항+ΔY4만 적용.
- 구현 내역·실장비 확인 항목: [06_implementation/CHANGES.md](06_implementation/CHANGES.md) 참조.
- ProcessY 재티칭 전제: 콜렛(4번) 중심면 기준 (다이 면 기준 구티칭이면 DieSize/2 이중 반영).
