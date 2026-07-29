# Side Vision Front/Rear Y 절대식 교체 — 구현 변경 로그 (2026-07-29)

빌드: MSBuild Debug, `/p:OutDir` scratchpad 우회(실장비 폴더 미접촉) — **에러 0, 수정 파일 경고 0**.

## 확정식 (팀장님 2026-07-29, 부호 2차 정정 반영)

```
ΔY4  = 콜렛Cal FinalPickerY(현재) − FinalPickerY(4번)   (마지막 1회 가산)
rotY = COCy − (OffsetX − COCx)                          (CW 90도, COC 중심 회전의 Y성분)

0도:  FrontY = P_F − DieSizeY/2 − OffsetY + ΔY4
      RearY  = P_R + DieSizeY/2 − OffsetY + ΔY4
90도: FrontY = P_F − DieSizeX/2 − rotY   + ΔY4
      RearY  = P_R + DieSizeX/2 − rotY   + ΔY4
```

오프셋 추종 항(−OffsetY/−rotY/+ΔY4)은 두 카메라 공통, 반쪽치수 항만 Front −/Rear +.

## Sequencing\Picker\SideVisionYTargetCalculator.cs — 신규
- `SideVisionYTargetResult`: 4개 최종값 + 전 항(P/half/offset/COC/rotY/ΔY4) 보관, `BuildTermLogText()`로 로그 1줄 출력.
- `SideVisionYTargetCalculator.TryBuild(machine, controller, pickerSide, pickerNo, offsetX, offsetY, offsetApplied, out result, out failReason)`.
- 데이터 소스: P=Recipe.{Front|Rear}SideVision.Process0Position, 다이사이즈=레시피 InputFrame→Frame→Controller, ΔY4=콜렛Cal FinalPickerY 차, COC=RotationCenterPixel→Bottom 카메라 픽셀Cal mm 변환.
- 실패 조건(사유 문자열 반환): VisionUnit 없음 / 다이사이즈 무효 / 콜렛Cal 없음(현재·4번) / COC 없음 / 변환 NaN. → 호출측에서 검사 차단.
- csproj Compile Include 등록.

## Sequencing\Picker\PickerBottomAndSideInspectionSequence.cs — 통합 시퀀스
- `ResolveSideVisionBasePosition`, `TryResolveSideFocusFallbackCorrection` **삭제** (Side AF BestPosition 우선순위·COC 부호 파라미터 폴백 폐기).
- `BuildSideTarget`: `SideVisionYTargetCalculator.TryBuild`로 4개 목표 확정. 계산 실패 시 사유 로그 + null(→ PICKER-BOTTOM-SIDE-SIDE-TARGET 차단).
- `TryResolveBottomMResultSideVisionOffsetY` → `TryResolveBottomMResultSideVisionOffset`: `bottom_offset_x_mm`+`bottom_offset_y_mm` 둘 다 필수 파싱, 각각 ±7mm 캡.
- `StoreRuntimeSideInspectionCorrection`: (OffsetX, OffsetY) 벡터 저장으로 변경. Picker축 미적용 동일.
- `ValidateRuntimeSideInspectionCorrection`: X/Y 벡터 검증.
- `InspectionTarget`: SideVisionProcess0/90YOffset → SideBottomOffsetXmm/Ymm + SideVisionCalc.
- `SideVisionPositionTarget`: BaseY/FocusValid 제거, 최종 Process0Y/Process90Y만.
- `MoveSideVisionAxisProcessPositionAsync`: "SideVisionY 절대식 이동" 로그로 전 항 출력(BuildTermLogText). Front/Rear 병렬 이동 구조 유지.
- `ResolveSideVisionTargetY` null 폴백: 티칭 Process0Position(로그 표기용).
- `LogSideCorrectionTarget`: 절대식 전 항 + 4개 final 출력.
- 에러코드 변경: PICKER-BOTTOM-SIDE-BOTTOM-MRESULT-OFFSETY → **PICKER-BOTTOM-SIDE-BOTTOM-MRESULT-OFFSET** (X 포함).

## Sequencing\Picker\PickerSideInspectionSequence.cs — 단독 시퀀스 (확정: 둘 다 적용)
- `SelectNextPicker`: 절대식 목표 확정(`_sideVisionCalc`). Bottom MRESULT 보정이 같은 Die로 저장돼 있으면 (X,Y) 사용, 없으면 **(0,0) 기하항+ΔY4만 적용**(확정 B). 계산 실패 시 **PICKER-SIDE-VISIONY-CALC 차단**(확정 A: 콜렛Cal 무효 시 차단).
- `MoveSideVisionProcessPositionAsync(angleDeg)`: 공용 Process0 이동(MoveBothSideVisionProcess0PositionAsync) 폐기 → 각도별 Front/Rear 병렬 절대식 이동 + 전 항 로그.
- `CanSkipSideVisionProcessMove(angleDeg)`: 각도별 절대식 목표 기준 in-position 판정.

## Equipment\Unit\PickerFrontUnit.cs / PickerRearUnit.cs
- `PickerSideInspectionCorrection`: SideVisionYOffset(구형)·SideVisionProcess0/90YOffset 제거 → `BottomOffsetXmm`/`BottomOffsetYmm`. Set/Clone/Clear 갱신.
- `SetRuntimeSideInspectionCorrection` 파라미터명 (bottomOffsetXmm, bottomOffsetYmm, ...)로 변경 (양쪽 유닛).

## Equipment\MachineRuntimeState.cs / MachineController.cs
- `MachinePickerOffsetRuntimeState`: SideVisionYOffset·SideVisionProcess0/90YOffset → BottomOffsetXmm/BottomOffsetYmm (런타임 상태 저장/복원 스키마).
- MachineController 저장/복원 두 곳 필드 매핑 갱신. (구 스키마 파일은 새 필드 기본값 0으로 복원됨 — 보정은 Die 단위 재수신이므로 무해)

## 유지된 것
- `VisionUnit.MoveVisionAxis` 재사용 → MotionSpeedScale 1회 적용 그대로(신규 속도 코드 없음).
- 콜렛Cal Side AF 시퀀스/데이터·부호 파라미터 6종: 코드 존치, **런타임에서 미사용** (정리는 별도 지시 시).
- Bottom MRESULT 필수 정책(통합), ±7mm 캡, Die ID 일치 검증.

## 실장비 확인 항목 (첫 가동 전)
1. **소프트리밋**: 새 목표가 티칭Y 대비 최대 ±(DieSize/2 + 7mm + |ΔY4|) 이탈 — FrontSideVisionY0/RearSideVisionY0 소프트리밋 여유 확인 (부족 시 VS-SOFT-LIMIT 알람).
2. **부호 1런 검증**: "SideVisionY 절대식 이동" 로그가 전 항(P/half/offset/coc/rotY/deltaY4/finalY)을 찍으므로 실런 1회로 각 항 부호·크기 확정 가능.
3. **ProcessY 재티칭 전제**: Process0Position이 "콜렛(4번) 중심면 초점" 기준이어야 함. 다이 면 기준 구티칭이면 DieSize/2 이중 반영.
4. 콜렛Cal(FinalPickerY + COC) 4콜렛 전부 유효해야 Side 검사 진행(무효 시 차단 알람).
