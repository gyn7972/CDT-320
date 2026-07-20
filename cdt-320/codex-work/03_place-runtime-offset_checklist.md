# 체크리스트 — 3. Place 런타임 오프셋 보정 (place-runtime-offset-correction.md)

작성일: 2026-07-19 / 확인일: 2026-07-19 (1회차 통과)

## 구현 항목
- [x] C1. `LowPassFilter` 신규 (`Sequencing\Common\LowPassFilter.cs`) — 단일 채널 1차 EMA, `alpha=(2π·fc)/(2π·fc+1)` (0,1] 클램프, `Reset`/`Update`/`Value`/`CalculateAlpha`
- [x] C2. `PlaceRuntimeOffsetService` 신규 static (`Sequencing\Picker\`) — 8세트 × X/Y/T, `lock(Sync)`, lazy 로드(`EnsureLoadedLocked`), `GetOffset`/`OnInspectionOffset`/`Reset`/`ResetAll`(+테스트용 `Reload`)
- [x] C3. 이상치 거부 — X/Y 1.0mm, T 0.5° 채널별 독립, 폐기 시 측정값/필터값/한계/die/picker 로그
- [x] C4. `PlaceRuntimeOffsetStore` 신규 — `Config\place_runtime_offset.json`, DataContractJson 로드 + JsonPrettySerializer 저장, fc 기본 0.1, 실패 시 기본값/로그만
- [x] C5. `CalculatePlaceTarget` 선택 인자 3개 — PickerX `−X` / OutputStageY `+Y` / PickerT `−T`, Formula에 세 항 추가, bottomOffset와 별항 유지
- [x] C6. `CalculateOutputPlaceTarget` 선택 인자 3개 통과 + OutputPlaceTarget/OutputPlaceFormula 로그 양쪽에 값 기록
- [x] C7. 적용은 `CalculatePlaceTargetValues()`에서만 — `GetOffset(Side, _currentPickerNo, ...)` + WriteLog 세 값 추가. 다른 3개 호출처는 기본값 0
- [x] C8. `ApplyPlacedDieResult` — `HasOffset && IsPass && HasPickerContext && !SkipInspection` 게이트로 `OnInspectionOffset(PickerSide, PickerNo, X, Y, R, DieId)`, 기존 Material 업데이트/로그 앞단 삽입만(불변)
- [x] C9. csproj 등록 — `Sequencing\Common\LowPassFilter.cs`, `Sequencing\Picker\PlaceRuntimeOffsetService.cs`, `PlaceRuntimeOffsetStore.cs` 3건

## 검증 (리플렉션 하네스 20개 검증 ALL PASS)
- [x] C10. 솔루션 빌드 성공, 신규 경고 0건 (기존 CS0162 4건만)
- [x] C11. 필터 — alpha(fc=0.1)=0.385870 정확 일치, 1.0 반복 입력 50회 후 1.000000 수렴 / 이상치: X=1.2 폐기·Y=0.5 수락(0.192935)·T=0.6 폐기, 이후 X=0.5·T=0.3 수락 — 채널 독립 확인
- [x] C12. 부호 — 필터 X=+0.1/Y=+0.2/T=+0.05에서 PickerX Δ=−0.100000, OutputStageY Δ=+0.200000, PickerT Δ=−0.050000 + Formula 항 포함 확인
- [x] C13. 영속화 — 갱신→저장→`Reload()` 재로드 후 X/Y/T 3채널 값 유지
- [x] C14. 비적용 경로 — 선택 인자(기본 0)라서 기존 호출처 재컴파일만으로 동작 불변 (하네스 baseline 호출이 인자 0 경로와 동일함을 확인). (Rear,1) 등 타 세트 독립성 확인
- [x] C15. 갱신 차단 — IsPass=false / HasOffset=false / HasPickerContext=false / SkipInspection=true 각각 필터 무변화, 전 조건 충족 시에만 갱신(0.3α/0.2α/0.1α 정확 일치)

## 결과: 전 항목 통과 (재시도 불필요)
