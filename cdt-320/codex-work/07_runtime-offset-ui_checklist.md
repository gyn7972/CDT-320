# 체크리스트 — 후속: Place Enable 플래그 + 설정 UI 토글

작성일: 2026-07-20 / 확인일: 2026-07-20 (1회차 통과)

## 1) Place Enable 플래그 (Pick과 동일 구조)
- [x] C1. `PlaceRuntimeOffsetDocument`에 `UsePlaceRuntimeOffset`(기본 **false**) 추가 — 이름 기반 역직렬화로 기존 파일 호환
- [x] C2. `PlaceRuntimeOffsetService`에 `IsEnabled`/`SetEnabled` 추가 + 로드/저장 반영
- [x] C3. 적용 게이트 — `CalculatePlaceTargetValues()`에서 `IsEnabled`면 `GetOffset`, 아니면 0. 학습(ApplyPlacedDieResult)은 Enable 무관 지속. WriteLog에 `placeRuntimeEnabled` 기록
- [x] C4. 동작 변경 명시 — 직전 커밋에서는 Place 보정 항상 적용이었으나, 이제 기본 **사용안함** (설정에서 ENABLE해야 적용)

## 2) 설정 UI 토글 (Settings > General)
- [x] C5. "PICK RUNTIME OFFSET" / "PLACE RUNTIME OFFSET" 2행 추가 — 기존 행과 동일 스타일 ENABLE/DISABLE 콤보 + RESET 버튼(110px)
- [x] C6. Designer 규칙 준수 — 선언·생성·배치는 `GeneralPage.Designer.cs` InitializeComponent 인라인(중첩 TableLayoutPanel), 로직은 GeneralPage.cs. 레이아웃 행 10→12, grpSetting 372→440
- [x] C7. 콤보 변경 → `SetEnabled` 즉시 저장, `_loadingSettings` 가드 적용
- [x] C8. RESET 버튼 → MessageDialog Yes/No 확인 후 `ResetAll()` + 완료 안내
- [x] C9. LoadSettings에서 서비스 `IsEnabled`로 콤보 초기화

## 검증 (하네스 ALL PASS)
- [x] V1. 솔루션 빌드 성공, 신규 경고 0건 (기존 CS0162 4건만)
- [x] V2. Place 게이트 — 파일 없음 시 기본 Disable / Disable 중 학습·저장 지속(0.077174) / SetEnabled(true) 즉시 반영 ✅
- [x] V3. Place 플래그 영속화 — 재로드 후 플래그·필터 상태 유지 ✅
- [x] V4. 회귀 — Place 하네스 20/20, Pick 하네스 23/23 재통과 ✅
- [x] V5. UI 스모크 — `GeneralPage` 오프스크린 생성: 컨트롤 4종 생성·ENABLE/DISABLE 아이템·초기값 DISABLE·토글→서비스 즉시 반영(양방향)·라벨 텍스트 13개 검증 ✅
  (실제 장비 앱 기동은 AGENTS.md 안전 규칙상 미수행 — 화면 표시는 현장 확인 필요)

## 3) 추가: Place 발산 방지 클램프 (Pick과 동일 정책, 2026-07-20)
- [x] C10. `PlaceRuntimeOffsetService`에 클램프 상수 X/Y **±0.50mm** · T **±0.5°** 추가
- [x] C11. `ClampChannelLocked` — 갱신 수락 채널만 클램프, 한계 도달 시 `AlarmManager.Raise(Warning, "PLACE-RUNTIME-OFFSET-CLAMP")` + `EventLogger`(side/pickerNo/채널/클램프 전 값/한계)
- [x] C12. (side, pickerNo, 채널)별 래치 — 1회만 발생, 한계 미만 복귀 시 재무장. `Reset`/`ResetAll`에서 래치 해제
- [x] V6. 클램프 하네스 7건 ALL PASS — X 0.9 반복 → 0.500000 클램프+Warning 정확히 1회 / 복귀 후 재도달 시 1회 재발생 / T 0.45 수렴은 워닝 없음 / 정상 학습 무영향
- [x] V7. Place 회귀 하네스 20/20 재통과, 빌드 신규 경고 0건

## 결과: 전 항목 통과 (재시도 불필요)
