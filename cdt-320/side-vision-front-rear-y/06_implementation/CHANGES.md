# Side Vision Front/Rear Y 절대식 — 구현 변경 로그

빌드: MSBuild Debug, `/p:OutDir` scratchpad 우회(실장비 폴더 미접촉) — **에러 0**, 수정 파일 신규 경고 0.

---

## 2026-08-18 — 회전 방정식 정식 전개, 90도 부호 정정 (Rear 1·2번 90도 초점 이탈 대응)

### 증상
실장비에서 **Rear 콜렛 1·2번, 90도 촬영만** 포커스 이탈. 0도 정상, Rear 3·4번·Front 무증상.

### 프레임 규약 (팀장님 확정)
```
기계X = 이미지X (같은 부호)
기계Y = −이미지Y (이미지 위쪽이 모션 +Y)
```
비전의 OffsetX/OffsetY는 이미지 프레임이므로 기계 프레임 다이중심 = `(OffsetX, −OffsetY)`.
**0도식의 `−OffsetY`는 감산이 아니라 이미지→기계 환산이다.** 이전 판은 이걸 감산으로 오해해서
같은 자리에 기계 프레임 값인 `rot90Y`를 `−rot90Y`로 넣었다.

### 회전 방정식 (회전중심 c=(cX,cY) 기준 CCW θ)
```
o'y(θ) = cY + (OffsetX − cX)·sinθ + (−OffsetY − cY)·cosθ

θ=0  ⇒ −OffsetY               ← 회전중심이 완전히 소거. 0도식이 COC 없이 성립하는 이유(교차검증)
θ=90 ⇒ cY + (OffsetX − cX)    ← OffsetY는 cos90=0으로 소거되고 X성분으로 넘어감
```
**회전 방향이 CCW인 근거(실험 아님, 코드 근거):** T 널링 루프가
`targetT = actualT − theta × ThetaMoveGain`(gain=1, 실장비 수렴 확인, ColletCalibrationSequence.cs:1020)
⇒ Δ(이미지각) = ΔT ⇒ T+ = 이미지각+ = 화면상 CW. 기계 프레임은 Y가 반전돼 회전 감각이 뒤집혀 **CCW**.

### 최종식
```
0도:  Front = P_F − DieSizeY/2 − OffsetY + ΔY4   /  Rear = P_R + DieSizeY/2 − OffsetY + ΔY4
90도: Front = P_F − DieSizeX/2 + rot90Y + ΔY4   /  Rear = P_R + DieSizeX/2 + rot90Y + ΔY4

rot90Y = rotationCenterUsed ? cY + (OffsetX − cX) : −OffsetY
rotationCenterUsed = COC VALID && Bottom 측정 프레임 확보
```
폴백은 `d=0`(다이가 회전축 위에 있다) 가정 — 회전해도 위치가 안 변하므로 각도 무관 `−OffsetY`.
`c=(0,0)`(회전축이 광축에 있다) 가정은 실제로 축이 ΔY4만큼 벗어나 있어 틀린다.

### 원인과 크기
이전 판은 `−rot90Y_CW = −cY + (OffsetX−cX)`, 정답은 `+rot90Y_CCW = +cY + (OffsetX−cX)`.
**소비 부호 오류와 회전방향 오류가 휘둘림 항에서 서로 상쇄돼, 순수하게 `cY` 부호만 뒤집혀 있었다.**
차이 = `2cY`. Bottom 촬영 Y가 P4 고정이라 `cY ≈ −ΔY4`이므로 오차 = `2ΔY4`.

Rear 실값(`EquipmentData\Config\CalibrationData.json` + `PickerRearUnit.json`, fixedY=−31.978):

| 콜렛 | ΔY4 | cY | 수정 전 오차 | 수정 후 |
|---|---|---|---|---|
| 1 | −0.560 | +0.472 | **−1.03 mm** | −0.09 |
| 2 | −0.784 | +0.768 | **−1.55 mm** | −0.02 |
| 3 | −0.061 | +0.206 | −0.27 mm | +0.14 |
| 4 | 0 | +0.0005 | ~0 | ~0 |

Front 최대는 콜렛 3번 0.81mm(무증상이었으나 임계 근처) → 수정 후 ~0.
**Rear만 / 콜렛 1·2만 / 90도만** 세 조건이 모두 설명된다:
90도만 = cY가 90도식에만 존재. 콜렛 1·2만 = 오차 ∝ |ΔY4|, 4번은 정의상 0.
Rear만 = Rear 콜렛 Y 장착 산포(0.784mm)가 Front(0.405mm)의 1.9배.

### 변경 파일
- **SideVisionYTargetCalculator.cs**: `rot90Y` 정의를 CCW 삼항식으로 교체, 소비를 `−rot90Y`→`+rot90Y`.
  헤더 주석에 프레임 규약·회전 방정식 전개·CCW 근거 기록. `Rot90Y` 필드 주석 정정.
- 0도식·호출부·데이터 스키마는 무변경.

### 미확정 (정직하게)
휘둘림 항 `(OffsetX − cX)`의 부호는 "비전이 각도를 이미지 좌표계 atan2(화면상 CW가 +)로 보고한다"는
가정에 의존한다. 이 가정이 틀리면 그 항만 부호가 반대가 되며, 크기는 0.1mm대라 이번 증상과는 무관.
`cY` 부호 정정은 이 가정과 무관하게 확정(0도 상쇄 증거로도 동일 결론).

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
  (구판 기술 — 2026-08-18 폐기. 현재는 d=0 가정으로 rot90Y = −OffsetY)
  차단하지 않고 진행하는 정책은 유지.
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

## 실장비 확인 항목 (2026-08-18 기준)

1. **90도 1런 검증** — "SideVisionY 절대식 이동" 로그에 전 항이 찍힌다.
   Rear 2번 기준 기대값: `cocEccentric=(≈0, +0.768)`, `rot90Y ≈ +0.77`, `deltaY4 ≈ −0.784`,
   그리고 `Rear90Y`가 `Rear0Y`와 0.1mm 이내로 붙어야 한다(다이가 회전축 위에 있으므로).
   콜렛 1~4의 `Rear0Y` 산포가 ±0.02인데 `Rear90Y`만 벌어지면 아직 90도 항에 문제가 남은 것.
2. **휘둘림 항 부호(CW/CCW 잔여 검증)** — 수정 후 잔차가 콜렛별 `(OffsetX − cX)`와
   **같은 부호로 비례**하면 CCW(현재 구현)가 맞고, **반대 부호로 비례**하면 CW로 뒤집어야 한다.
   크기는 0.1~0.2mm대이므로 초점이 잡힌 뒤 정밀 조정 단계에서 판정하면 된다.
3. **소프트리밋** — 목표가 티칭Y 대비 최대 ±(DieSize/2 + 7mm + |ΔY4| + |편심|) 이탈.
   FrontSideVisionY0 / RearSideVisionY0 여유 확인.
4. **ProcessY 재티칭 전제** — 콜렛(4번) 중심면 초점 기준. 다이 면 기준 구티칭이면 DieSize/2 이중 반영.
5. **초점 심도(DOF) 실값 미확보** — 코드·설정 어디에도 없다. 수정 전 데이터상 무증상 최대가
   Front 3번 0.81mm, 불량 최소가 Rear 1번 1.03mm이므로 임계는 그 사이. 여유가 27%뿐이라
   수정 후에도 잔차가 0.2mm를 넘는 콜렛이 있으면 추가 원인을 봐야 한다.
