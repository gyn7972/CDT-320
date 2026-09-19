# 픽업 편심 보상 — 구현 체크리스트 (Stage 5)

기준: [04_design.md](cdt-320__pick-coc-eccentric-comp__04_design.md) + 지시서 §2~§5. 편집은 팀장님 승인 후 시작.
전 항목은 Stage 7 검증에서 ✅/❌로 판정 가능해야 한다.

## A. 보상 산출 — PickerMotionTargetResolver.cs
- [ ] `TryCalculateInputPickTarget`(21행)에 `bool applyColletEccentricCompensation = false` 옵션 파라미터 추가, `CalculateInputPickTarget`으로 전달
- [ ] `CalculateInputPickTarget`(79행)에 동일 파라미터 추가 — 기본 false로 기존 호출부 무변경 컴파일 호환
- [ ] true일 때만 ΔP 산출: C=`PickerFront/RearUnit.Config.ColletRotationCenterX/Y[i]`, O=`VisionUnit.Config.CalibrationData.Collet.GetRecord(side,i+1).FinalPickerX/Y`, e=C−O (레코드 조합만 — RotationCenterPixel 잔차 방식 금지)
- [ ] θ_pick=티칭 "PickPosition"(T축), θ_cal=존 "DieBottomPosition" TeachingT — `MeasuredTPosition` 미사용
- [ ] R(θ)=[[cos,+sin],[−sin,cos]] (T+=CW), ΔP=(I−R(Δθ))·e — 부호 반전 스위치 없음
- [ ] 게이트 5종: ① Enable(신설 설정) ② `ColletRotationCenterValid[i]` ③ 세대 `RotationCenterUpdatedAt ≥ UpdatedAt` ④ 각도 ||Δθ|−180°|≤5° (±360 정규화) ⑤ 크기 |ΔP.x|,|ΔP.y| ≤ 한계(기본 0.2mm) — 하나라도 실패 시 ΔP=0
- [ ] (승인 시) 게이트 ② 확장: `record.Valid` 포함
- [ ] `PICK-COC-COMP` 로그: side, pickerNo, C, O, e, θ_pick, θ_cal, Δθ, ΔP, 게이트 판정 사유 — 산출마다 1줄, 폴백 사유는 콜렛당 상태 변화 시에만 (도배 금지)
- [ ] ΔP를 `CalculatePickTarget` 신규 파라미터로 전달

## B. 적용 — DieCoordinateTransformService.cs
- [ ] `CalculatePickTarget`에 `colletEccentricCompX = 0.0`, `colletEccentricCompY = 0.0` 추가
- [ ] PickerX(106행)에 X 가산, PickerY(114행)에 Y 가산 — StageY(105)·NeedleX(111)·T(109) 무변경
- [ ] Formula 문자열(117~126행)에 두 항 + "티칭 Δθ 기준" 명기 → DIE-COORD-CALC 자동 포함
- [ ] 죽은 코드(`ResolveInputPickerYTarget`/`ResolveSignedPickerYOffset` 162~171행) 미사용 유지

## C. 호출부 opt-in (2곳만 true)
- [ ] PickerPickUpSequence.PickTargets.cs:607 — `applyColletEccentricCompensation: true`
- [ ] InputPickerPickTargetResolver.CalculateManualInputMapTarget(37행 호출) — `true`
- [ ] PickerPickUpZCalibrationSequence.cs:397 / RecipePickerMoveTarget.cs:123 — 무인자(false) 유지 확인

## D. 설정 + UI — PickerTransferTypes.cs, Front/RearPickerRecipePage.cs
- [ ] `PickerPickUpMotionConfig`에 `[DataMember] bool UsePickRotationCenterCompensation`(기본 false), `[DataMember] double ColletEccentricCompensationLimitMm`(기본 0.2) 추가
- [ ] `OnDeserializing`에서 안전측 초기화(false / 0.2) — 클래스 하위호환 패턴 준수
- [ ] `Ensure()`에서 한계 정규화(0 이하·NaN → 0.2)
- [ ] FrontPickerRecipePage(463~484 바인딩 블록 부근) 체크박스 1 + 한계 입력 1 바인딩 — 기존 항목 이동 금지
- [ ] RearPickerRecipePage 동형
- [ ] MotionSpeedScale: 신규 이동 명령 0건 확인 (목표값 보정만)

## E. 회전중심 영속 보강 (F13)
- [ ] ColletCalibrationSequence.SaveAndApplyRotationCenter(1982~2014행): SaveRecipe 후 `machine.SaveSettings()` 추가 (기존 SaveRecipe 유지)
- [ ] ColletCalibrationApplyService.SaveRotationCenterToRecipe(106~177행): 동일 보강
- [ ] 실패 처리는 팀장님 확답대로 (제안: 선례와 같이 실패 반환)
- [ ] 다이얼로그 COC CENTER 경로 무변경 확인 (이미 SaveMachineSettings)

## F. 무변경 확인 (지시서 §4)
- [ ] SideVisionYTargetCalculator·Side 경로 diff 0
- [ ] InputVisionToPicker 저장값 소성 없음
- [ ] Place 산식·플레이스 런타임 필터 diff 0
- [ ] T 채널 무변경 (보상은 XY만)
- [ ] COC/콜렛 캘 측정 로직 무변경 (§3-5 저장 보강만)

## G. 빌드·시뮬 (Stage 7 = 지시서 §7)
- [ ] `/p:OutDir=<임시경로>` 우회 빌드 성공 (기본 빌드 금지 — 실장비 폴더 직결)
- [ ] 시뮬: Enable OFF → 기존과 완전 동일 (Formula 항 0)
- [ ] 시뮬: Enable ON + 게이트 각각 강제 실패 → 보상 0 + 사유 로그 확인
- [ ] 픽업 Y 인터락(반대측 Y-Avoid, 고정 Pick Y 전제) 새 Y 목표에서 미간섭 — 걸리면 수정 없이 즉시 보고
- [ ] 실장비 부호 확정 1런·필터 리셋(§6·§7-3)은 팀장님 진행
