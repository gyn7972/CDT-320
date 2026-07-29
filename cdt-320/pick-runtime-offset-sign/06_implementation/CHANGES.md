# Pick 런타임 오프셋(로패스 클로즈 루프) 적용 부호 정정

- 일자: 2026-07-30
- 지시: 픽업 로패스필터 클로즈 루프의 Offset 식 X/Y 부호 반대 → 정정 (사용자 실장비 확인)
- 범위: X/Y만. T는 지시에 없어 기존 감산 유지. 다른 파일 변경 없음.

## QMC.CDT-320\Sequencing\Picker\DieCoordinateTransformService.cs — MODIFIED

`CalculatePickTarget` 내 pickRuntimeOffset 적용 부호 감산→가산 (식 3곳 + Formula 로그 문자열 3곳 + 주석):

| 위치 | 변경 전 | 변경 후 |
|------|---------|---------|
| StageY 식 | `- pickRuntimeOffsetY` | `+ pickRuntimeOffsetY` |
| PickerX 식 | `- pickRuntimeOffsetX` | `+ pickRuntimeOffsetX` |
| NeedleX 식 | `- pickRuntimeOffsetX` | `+ pickRuntimeOffsetX` |
| Formula 문자열 (stageY/pickerX/needleX) | `- pickRuntimeOffset*(` | `+ pickRuntimeOffset*(` |
| 주석 | "전 채널 감산으로 상쇄" | 기존조건(~07-29)/현재기준(07-30 실장비 확인) 이력 기록 |

- X는 피커·니들 정렬 유지 규칙(PickerX/NeedleX 동일 적용)에 따라 두 식을 함께 반전 — 한쪽만 반전 시 정렬이 2×오프셋으로 벌어짐.
- PickerT 식(`- pickRuntimeOffsetT`)과 Formula의 T 항은 변경하지 않음.

## 영향 범위 확인 (수정 전 조사)

- 적용 식은 `CalculatePickTarget` 1곳뿐. `PickRuntimeOffsetService`는 raw 저장 설계(주석: "부호 반전은 적용 지점에서 수행"), `PickerMotionTargetResolver`는 값 통과만 — 양쪽 모두 무변경.
- 메카 오프셋 이관(RuntimeOffsetMonitorDialog): Pick 쪽 이관 버튼은 "부호 확정 전" 사유로 비활성 상태라 영향 없음. 이번 정정으로 Pick 부호가 확정되었으므로 향후 활성화 시 이관 부호는 "가산 적용" 기준으로 설계할 것.
- 필터 학습값(EMA)은 측정 raw라 부호 정정과 무관하게 유효. 단, 잘못된 방향으로 돌던 기간에 클램프(±2mm) 근처까지 발산했을 수 있음 — 필요 시 Settings > General의 Pick 런타임 오프셋 리셋으로 초기화 후 재학습 권장.

## 검증 계측

Formula 로그(`LogFormula` PICK / InputPickTarget Calc 로그)가 각 항의 부호·수치를 그대로 출력하므로,
실런 1회에서 `+ pickRuntimeOffsetX(...)` / `+ pickRuntimeOffsetY(...)` 표기와 최종 목표 좌표 이동 방향으로 정정 여부 즉시 확인 가능.
빌드는 이 창에서 수행하지 않음(부호/문자열 변경만이라 컴파일 위험 없음).

---

# (추가 2026-07-30) Y 전처리 P4 기준 환산 정정

- 지시: "rawOffsetY = Bottom 비전 OffsetY + (현재피커 Y − 4번피커 Y) 해서 나온 결과 값을 사용해야 됨"
- 근거: 통합검사(BottomAndSide)는 07-28 승인으로 촬영 Y를 항상 P4 기준으로 고정
  (PickerBottomAndSideInspectionSequence.cs:353-361). 측정 OffsetY가 P4 기준 좌표계이므로
  촬영시점 지령(CommandPosition) 기반 편차 감산이 아니라 콜렛Cal 고정값 환산이 맞음.

## QMC.CDT-320\Sequencing\Picker\PickRuntimeOffsetService.cs — MODIFIED

`OnBottomInspectionOffset` Y 전처리:

| 구분 | 식 |
|------|-----|
| 기존(~07-30) | `pickerYDeviation = capturedPickerYCommand − colletCalY` / `preprocessedY = rawOffsetY − pickerYDeviation` |
| 현재(지시) | `colletYDeltaFromP4 = colletCalY(현재픽커) − basePicker4ColletCalY(4번픽커)` / `preprocessedY = rawOffsetY + colletYDeltaFromP4` |

- 시그니처에 `basePicker4ColletCalY`, `basePicker4ColletCalValid` 추가 (콜렛Cal Y 출처: ColletCalibrationRecord.FinalPickerY).
- 유효성: 현재 픽커 또는 4번 픽커 콜렛Cal이 Invalid면 샘플 전체 폐기(폐기 로그에 양쪽 Valid 값 명시).
- `capturedPickerYCommand`는 식에서 제외, 진단 로그 전용으로 유지.
- PICK-RUNTIME-OFFSET 로그에 `basePicker4ColletCalY`, `colletYDeltaFromP4` 필드 추가 (기존 `pickerYDeviation` 대체) — 실런 1회로 환산값 검증 가능.

## 호출부 2곳 — MODIFIED

- `PickerBottomInspectionSequence.cs` UpdatePickRuntimeOffsetFilter: 4번 픽커(index 3) 콜렛Cal 추가 조회·전달.
- `PickerBottomAndSideInspectionSequence.cs` UpdatePickRuntimeOffsetFilter: `ToPickerIndex(4)` 콜렛Cal 추가 조회·전달.

---

# (정정 2026-07-30, 실장비 재확인) Y 적용 부호 감산 복귀

- 지시: "pickRuntimeOffsetY 반영을 −로 하는 게 맞다 — − 반영 해라" (가산 시험 후 실장비 판정)
- DieCoordinateTransformService.cs `CalculatePickTarget`:
  - StageY 식 `+ pickRuntimeOffsetY` → `− pickRuntimeOffsetY` 복귀 (Formula 로그 문자열 동일 갱신)
  - **X는 가산(+) 유지, T는 감산 유지** — 최종 상태: X 가산 / Y 감산 / T 감산
- P4 기준 Y 전처리(colletYDeltaFromP4 가산)는 그대로 유지 — 이번 정정은 "적용 부호"만 해당.

---

# (최종 확정 2026-07-30) Y 적용 가산 + 전처리 델타 감산

- 지시: "적용은 + 가 맞다. 전처리는 rawOffsetY − (현재피커Y − 4번피커Y) 로 수정해라"
- DieCoordinateTransformService.cs: StageY `+ pickRuntimeOffsetY` 재확정 (Formula 로그 동일).
- PickRuntimeOffsetService.cs: `preprocessedY = rawOffsetY − colletYDeltaFromP4` 로 델타 부호 반전.
- **최종 상태**: 전처리 Y = raw − (현재픽커콜렛CalY − 4번픽커콜렛CalY) → EMA → 적용 X 가산 / Y 가산 / T 감산.

---

## 미결(팀장님 결정 필요)

1. **단독 Bottom 검사 경로**: PickerBottomInspectionSequence는 픽커별 자기 DieBottomPosition Y에서 촬영
   (375-376행) — P4 고정 촬영이 아니므로 P4 환산이 그 경로 측정과는 안 맞을 수 있음.
   지시대로 서비스 공통 식으로 일괄 적용해 둔 상태. 단독 경로도 쓰신다면 처리 방침 결정 필요
   (예: 단독 경로 촬영 Y도 P4 고정으로 통일).
2. **기존 학습값**: 이전 식으로 학습된 Y 필터 상태가 남아 있음 — 리셋(Settings > General) 후 재학습 권장.
3. X/T 채널은 지시에 없어 전처리 없이 유지.
