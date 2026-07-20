# 체크리스트 — 4. Pick 런타임 오프셋 보정 (pick-runtime-offset-correction.md)

작성일: 2026-07-19 / 확인일: 2026-07-19 (1회차 통과)

## 구현 항목
- [x] C1. `PickRuntimeOffsetService` 신규 static — 8세트 × X/Y/T `LowPassFilter` 재사용, `lock(Sync)`, lazy 로드, `IsEnabled`/`SetEnabled`/`GetOffset`/`OnBottomInspectionOffset`/`Reset`/`ResetAll`/`Reload`
- [x] C2. Y 전처리 — 서비스 담당: `편차 = capturedPickerYCommand − colletCalY`, `전처리Y = rawOffsetY − 편차`. 콜렛Cal 무효 시 샘플 전체 폐기+로그. X/T 원시값
- [x] C3. 콜렛Cal Y = `ColletCalibrationRecord.FinalPickerY` 확정 — ColletCalibrationSequence.cs:1436에서 최종 Vision 매치 시점 PickerY ActualPosition을 저장함을 확인, 양쪽 훅에 근거 주석 기재
- [x] C4. Pass만 갱신 (양 시퀀스 `result.IsOk` 게이트) + 이상치 X/Y 1.0mm·T 0.5° 채널별 폐기+로그
- [x] C5. 클램프 X/Y ±0.50mm·T ±0.5° + `AlarmManager.Raise(Warning, "PICK-RUNTIME-OFFSET-CLAMP")` + EventLogger, (side,pickerNo,채널) 래치 1회/재무장
- [x] C6. `PickRuntimeOffsetStore` — `Config\pick_runtime_offset.json`: UsePickRuntimeOffset(기본 false)·fc(0.1)·8세트·갱신시각, 실패 로그만
- [x] C7. 촬영 시점 PickerY CommandPosition 캡처 — 단독: `StartBottomInspectionRequestAsync` 진입부(`_bottomShotPickerYCommand`) / BottomAndSide: `StartBottomInspectionAsync`에서 `InspectionTarget.BottomShotPickerYCommand`에 보관
- [x] C8. 결과 반영 연결 — 두 시퀀스 `ApplyBottomInspectionResult`에서 `UpdatePickRuntimeOffsetFilter` 호출(Pass 게이트, try/catch로 기존 흐름 보호), Material 업데이트 불변
- [x] C9. `CalculatePickTarget` 선택 인자 3개 — PickerX/NeedleX `−X`(동일값), StageY `−Y`, PickerT `−T`, Formula 세 항 추가. PickerY/PickerZ 미적용
- [x] C10. `CalculateInputPickTarget`/`TryCalculateInputPickTarget` 선택 인자 통과 + InputPickTarget 로그 값 기록
- [x] C11. 적용은 `CalculateCurrentPickTarget()`에서만 — `IsEnabled`면 `GetOffset`, 아니면 0. WriteLog에 Enable+세 값 기록. 타 호출처(PickerPickUpZCalibrationSequence/RecipePickerMoveTarget/InputPickerPickTargetResolver 경유) 기본값 0 불변
- [x] C12. csproj 등록 — PickRuntimeOffsetService.cs, PickRuntimeOffsetStore.cs 2건

## 검증 (리플렉션 하네스 23개 검증 ALL PASS)
- [x] C13. 솔루션 빌드 성공, 신규 경고 0건 (기존 CS0162 4건만)
- [x] C14. Y 전처리 — calY=30.0/CmdY=29.7: 측정 −0.3 → 입력 0(필터 0 유지) / 측정 0 → 입력 +0.3(0.115761=0.3α) ✅
- [x] C15. 클램프+워닝 — 0.9 반복 입력 2샘플째 0.500000 클램프 + Warning 정확히 1회, 이후 반복에도 중복 없음, 0.5 미만 복귀 후 재도달 시 정확히 1회 재발생 ✅
- [x] C16. Enable/Disable — 기본 false, Disable 중 갱신·저장 지속(0.077174), SetEnabled(true) 즉시 반영 + 재로드 후 플래그 유지 ✅
- [x] C17. 부호 — PickerX/NeedleX Δ=−0.100000, StageY Δ=−0.200000, PickerT Δ=−0.050000, PickerY/PickerZ 불변 + Formula 항 확인 ✅
- [x] C18. 이상치/무효 — X=1.2·T=0.6 폐기(채널 독립), X=0.5·T=0.3 수락, 콜렛Cal 무효 시 샘플 전체 폐기 ✅
- [x] C19. 영속화 — 갱신→저장→Reload 후 상태 유지 ✅
- [x] C20. 비적용 경로 — 선택 인자 기본 0 (하네스 baseline=인자 0 경로 동일 확인, 기존 호출처 시그니처 무변경) ✅

## 결과: 전 항목 통과 (재시도 불필요)
