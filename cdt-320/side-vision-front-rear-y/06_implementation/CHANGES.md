# Side Vision Front/Rear Y 절대식 — 구현 변경 로그

빌드: MSBuild Debug, `/p:OutDir` scratchpad 우회(실장비 폴더 미접촉) — **에러 0**, 수정 파일 신규 경고 0.

---

## 2026-08-17 — 90도 회전 중심 소스 정정 (실장비 90도 포커스 이탈 대응)

### 증상
0도 촬영은 정상, **90도만 포커스 이탈.**

### 원인
90도식의 회전중심에 `ColletCalibrationRecord.RotationCenterPixelX/Y`(픽셀)를 mm 변환해 쓰고 있었다.
이 값의 정체는 [ColletRotationCenterCalibrationSequence.cs:363] 기준 **파인얼라인으로 회전중심에 맞춘 뒤 남은 잔차**로,
정렬이 수렴할수록 0에 가까워진다. 즉 회전중심 항이 사실상 0으로 들어가 **콜렛별 회전중심이 식에서 누락**된 상태였다.
0도는 회전중심이 수식에서 소거되므로 정상 동작했고, 90도만 틀어졌다.

진짜 회전중심은 이미 저장돼 있다 — 같은 시퀀스 `:377`에서 `기계좌표 = 현재 PickerXY − 잔차*gain`으로 계산해
`Recipe.ColletRotationCenterX/Y/Valid[콜렛]`(Picker Recipe, UI "COLLET ROTATION CENTER")에 콜렛별로 저장.
콜렛 교체 시 갱신되는 값이며, 지금까지 아무도 읽지 않았다.

### 확정식 (팀장님 확정)

```
ΔY4  = 콜렛Cal FinalPickerY(현재) − FinalPickerY(4번)        (마지막 1회 가산)

[회전중심 유효 + Bottom 측정 프레임 확보]
  cX   = Bottom 촬영 PickerX − Recipe.ColletRotationCenterX[콜렛]
  cY   = Bottom 촬영 PickerY − Recipe.ColletRotationCenterY[콜렛]
  rotY = cY − (OffsetX − cX)                                 (CW 90도, 회전중심 c 기준)

[COC VALID=false 또는 Bottom 측정 없음 — 2026-08-17 지시]
  회전중심 계산 없이 편심 cX=cY=0으로 두고 같은 회전식 적용 ⇒ rotY = −OffsetX
  ⇒ 0도의 −OffsetY 자리에 +OffsetX가 들어간 형태(카메라축 중심 회전). 차단하지 않고 진행.
  (주: 최초 구현에서 치환 방향을 반대로 읽어 rotY=+OffsetY로 넣었다가 정정. cX=cY=0 대입과 일치해야 함)

0도:  FrontY = P_F − DieSizeY/2 − OffsetY + ΔY4
      RearY  = P_R + DieSizeY/2 − OffsetY + ΔY4
90도: FrontY = P_F − DieSizeX/2 − rotY   + ΔY4
      RearY  = P_R + DieSizeX/2 − rotY   + ΔY4
```

`ColletRotationCenterX/Y`는 "그 콜렛의 회전축이 Bottom 카메라 광축에 오는 PickerXY 기계좌표"이므로,
(Bottom 촬영 PickerXY − 그 값) = 촬영 프레임에서의 회전축 편심. 이 편심이 90도 회전 시 다이를 휘둘러 초점면을 이동시킨다.

### 변경 파일
- **SideVisionYTargetCalculator.cs**: COC 소스를 픽셀 잔차 → `Recipe.ColletRotationCenterX/Y/Valid`로 교체(`TryResolveRecipeRotationCenter`). `TryBuild`에 `bottomShotPickerX/Y` 파라미터 추가. COC 무효/측정 프레임 없음이면 폴백(`rotY = OffsetY`), **차단하지 않음**. 결과에 `RotationCenterUsed / RotationCenterPickerX,Y / BottomShotPickerX,Y / CocEccentricX,Y` 추가 — 로그에 전 항 출력.
- **PickerBottomAndSideInspectionSequence.cs**: `InspectionTarget.BottomShotPickerXCommand` 추가 + Bottom REQ 직전 캡처(기존 Y 캡처 옆). 보정 저장/계산기 호출에 PickerXY 전달.
- **PickerFrontUnit.cs / PickerRearUnit.cs**: `PickerSideInspectionCorrection`에 `BottomShotPickerX/Y` 추가(Set/Clone/Clear), `SetRuntimeSideInspectionCorrection` 파라미터 확장.
- **PickerSideInspectionSequence.cs**: 저장된 보정의 PickerXY를 계산기에 전달.
- **MachineRuntimeState.cs / MachineController.RuntimeState.cs**: 런타임 상태 저장·복원에 `BottomShotPickerX/Y` 매핑 추가.

### 차단 정책 (현행)
| 조건 | 동작 |
|---|---|
| 콜렛Cal 레코드 없음(FinalPickerY, ΔY4 불가) | **차단** |
| 다이 사이즈 확보 실패 | **차단** |
| `COC VALID = false` | **폴백 진행** (편심 0, rotY = −OffsetX) |
| Bottom 측정 없음(단독 Side) | **폴백 진행** (편심 0 + Offset 0 ⇒ rotY = 0) |

---

## 2026-07-29 — 절대식 최초 도입 (기존 체계 폐기)

- 폐기: Side AF `BestPosition` 우선순위, COC 부호 파라미터 6종 폴백((X−Y)/2 방식), MRESULT OffsetY 0/90 동일가산.
- 신설: `SideVisionYTargetCalculator` 단일 진입점. 통합(Bottom+Side)·단독 Side 시퀀스 **둘 다** 적용.
- 단독 시퀀스: 0/90 공용 `MoveBothSideVisionProcess0PositionAsync` 폐기 → 각도별 Front/Rear 병렬 절대식 이동, 스킵 판정도 절대식 목표 기준.
- 보정 저장: OffsetY 단일 → (OffsetX, OffsetY) 벡터. MRESULT `bottom_offset_x_mm` 필수 파싱, 각각 ±7mm 캡.
- 오프셋 추종 항(−OffsetY/−rotY/+ΔY4)은 두 카메라 공통 부호, 반쪽치수 항만 카메라별(Front −, Rear +).
- 유지: `VisionUnit.MoveVisionAxis` 재사용(MotionSpeedScale 1회 적용), Die ID 일치 검증, 콜렛Cal Side AF 시퀀스·데이터 코드 존치(런타임 미사용).

---

## 실장비 확인 항목
1. **90도 1런 검증**: "SideVisionY 절대식 이동" 로그에 `rotCenterUsed / recipeCoc / bottomShotPicker / cocEccentric / rot90Y`가 전부 찍힌다. `cocEccentric`이 0에 가까우면 바텀 촬영 위치가 회전중심과 거의 같다는 뜻이므로, 남은 이탈은 다른 항(반쪽치수·회전방향)에서 찾아야 한다.
2. **회전방향(CW)**: 바텀 카메라는 아래에서 올려보는 뷰라 상면 기준과 좌우가 뒤집힐 수 있다. CW/CCW가 반대면 `rotY` 부호 한 줄만 바꾸면 된다(어긋남이 `2×(cX−OffsetX)` 수준으로 나타남).
3. **소프트리밋**: 목표가 티칭Y 대비 최대 ±(DieSize/2 + 7mm + |ΔY4| + |편심|) 이탈 — FrontSideVisionY0/RearSideVisionY0 여유 확인.
4. **ProcessY 재티칭 전제**: 콜렛(4번) 중심면 초점 기준. 다이 면 기준 구티칭이면 DieSize/2 이중 반영.
