# Recipe 화면 Config 값 즉시 적용 안 됨 — 분석 프롬프트 & 체크리스트 (2026-07-25)

## ★ 진행 상태 (2026-07-25 코드 수정 완료 — 검증만 남음)

원인 확정: LoadSettings()가 Config/Setup 객체를 새 인스턴스로 교체(BaseUnit.cs:98-99)하는데,
일부 ParameterGrid 클로저가 중첩 객체를 지역 변수/인자로 캡처(패턴 A)해 낡은 객체에 읽고 씀.
→ UI만 갱신되고 런타임은 old 값 (실장비에서 PICKUP MECHANICAL X/Y=0 미적용으로 확인됨).

**수정 완료(빌드 에러 0, git diff --check 통과, 커밋 안 됨)** — 패턴 A 전수 정규화:
- FrontPickerRecipePage.cs / RearPickerRecipePage.cs: PickUp 전 항목(MECHANICAL X/Y·LIMIT,
  MOTION/TRANSFER MODE, CONTI 계열, Z 접근/분리/AVOID 속도, SETTLE 등), Place 전 항목
  (PLACE MECHANICAL X/Y, Z OVERDRIVE, CONTI 계열 등), Bottom FLYING 항목,
  INPUT/OUTPUT VISION→PICKER 1~4 X/Y OFFSET(헬퍼 시그니처를 Func 리졸버로 변경).
  신설 헬퍼: ResolveLivePickUpConfig / ResolveLivePlaceConfig / ResolveLiveBottomInspectionConfig / ResolveLiveVisionOffsets.
- OutputStageRecipePage.cs: AddKindGroup의 recipe 캡처 → ResolveLiveRecipe() (GOOD/NG Y·Z, VISION X 전 그룹).
- VisionRecipePage.cs: positions 캡처 → ResolveLiveVisionPositions(side) (FRONT/REAR Y AVOID·PROCESS 0/90°).
- 안전 확인됨(수정 불필요): Input/OutputCassette·Input/OutputFeeder·InputStage 페이지(전부 패턴 B),
  InputStageDieMapSetupDialog(클로저 아님).

**남은 작업 = 아래 §5 "수정 검증" 체크리스트 수행(실장비/시뮬)** + §3-4~5의 H2(런타임 캐싱/보드 푸시 분류) 확인.
검증 대표 항목: PICKUP MECHANICAL X/Y 변경 → 즉시 픽업 실행 → calculated pick target 로그의 적용값 확인.

이 문서는 "Recipe 화면에서 값을 바꾸면 UI만 갱신되고 실제 런타임은 old 값을 쓴다(특히 Config 스코프)"
문제의 **분석 지시서(프롬프트)와 검증 체크리스트**다. 아래 file:line은 2026-07-25 기준
`D:\Source\CDT-320_New` 코드에서 확인한 값 — 구현 시점에 심볼 기준으로 재확인한다.

- 작업 규칙: `AGENTS.md` 전체 준수(별도 OutDir 빌드만, 커밋은 사용자 명시 요청 시에만, 롤백 금지).
- 대상 저장소: `D:\Source\CDT-320_New`.

---

## 1. 증상

- Recipe 화면(예: FrontPickerRecipePage)에서 파라미터 값을 변경하면 그리드에는 새 값이 보인다.
- 그러나 장비 동작은 **old 값**으로 계속 돈다. 특히 **Config 스코프** 항목에서 확인됨.
- 사용자는 "값 변경 = 즉시 적용"으로 알고 있었음.

## 2. 확정 사실 (코드로 확인됨)

1. **그리드는 셀 편집 커밋 즉시 setter를 호출한다** — 지연 없음.
   - `ParameterGridControl.CommitValue` → `item.Setter(value)` (ParameterGridControl.cs:711-727).
   - 즉 "그리드가 Save 버튼까지 값을 들고 있다"는 가설은 **아님**.
2. **setter/getter는 페이지가 SetItems 시점에 만든 클로저**다.
   - 안전한 패턴: `() => unit.Recipe.PickLiftWaitMs` — 호출 시마다 `unit.Recipe`를 따라가므로
     객체가 교체되어도 항상 라이브 객체를 읽고 쓴다 (FrontPickerRecipePage.cs:338 등).
   - **위험한 패턴: 중첩 객체를 지역 변수로 캡처** —
     `PickerPickUpMotionConfig pickUp = unit.Config.PickUp;` 후
     `() => pickUp.MechanicalOffsetLimitMm` (FrontPickerRecipePage.cs:359-374).
     이 클로저는 **캡처 시점의 객체 인스턴스**에 고정된다.
3. **Config 객체는 LoadSettings 때 새 인스턴스로 교체된다.**
   - `BaseUnit.LoadSettings()`: `Config = UnitDataStore.LoadConfig(StorageKey, Config);`
     (QMC.Common\BaseUnit.cs:99, Setup도 :98에서 동일).
   - 따라서 LoadSettings가 페이지 SetItems **이후에 한 번이라도** 호출되면:
     - 지역 캡처 클로저 → **버려진 old 객체**에 read/write (UI는 자기가 쓴 값을 다시 읽으니 바뀐 것처럼 보임).
     - 런타임(`unit.Config.PickUp...`을 매번 따라가는 코드) → **새 객체의 old 파일 값**을 읽음.
   - 이것이 증상("UI만 업데이트, 실제는 old")과 정확히 일치하는 **유력 원인(H1)**.

## 3. 조사 지시 (구현 전 전수조사)

### H1 검증 — 객체 교체 × 지역 캡처 클로저
1. `LoadSettings()` 호출 경로 전수조사: 시작 시 외에 언제 다시 불리는가?
   (레시피 Active/Apply, SaveMachineSettings 이후 재로드, 설정 다이얼로그, VisionUnit.SaveSettings→LoadSettings 등.
   grep: `LoadSettings()` 호출부 전체.)
2. Recipe/설정 페이지 전체(FrontPickerRecipePage, RearPickerRecipePage, 기타 ParameterGrid 사용 페이지)에서
   **중첩 객체 지역 캡처 클로저 전수 목록** 작성:
   - 패턴 A: `var x = unit.Config.X;` 후 `() => x.Field` (예: AddPickUpSettingItems의 `pickUp`,
     Place 쪽 `place`(FrontPickerRecipePage.cs:485 부근), `AddVisionPickerOffsetItems(..., unit.Setup.InputVisionToPicker, ...)`처럼
     **객체를 인자로 넘기는 패턴**도 동일 위험).
   - 패턴 B(안전): `() => unit.Config.X.Field` — 교체에도 라이브 추적.
   - Recipe 스코프도 같은 방식으로 점검(레시피 Active 시 `Recipe` 객체 교체 여부 — UnitDataStore.LoadRecipe가
     인스턴스를 교체하는지 확인).
3. 각 위험 항목에 대해 "페이지 SetItems 이후 LoadSettings/LoadRecipe가 실제로 불리는 시나리오"를
   재현 절차로 명시(예: 페이지 열기 → 다른 화면에서 SAVE → 돌아와서 값 변경 → 동작 확인).

### H2 검증 — 런타임 캐싱(부차 원인)
4. 문제가 된 Config 값의 **소비처**가 값을 매번 `unit.Config...`에서 읽는지, 시퀀스/유닛 초기화 시
   1회 캐싱하는지 확인(캐싱이면 객체 문제와 무관하게 다음 시퀀스 시작부터 적용).
5. 축 파라미터처럼 **보드에 푸시되어야 적용**되는 값(속도/가감속/리밋 등)은 별도 분류 —
   이건 "저장+재적용(푸시)"이 필요한 정상 동작일 수 있으므로 버그 목록에서 분리.

### 재현 로그
6. 의심 항목 1개(예: PICKUP MECHANICAL OFFSET LIMIT)로 재현: UI 변경 → 즉시 시퀀스 실행 →
   런타임 로그의 실제 사용값과 UI 표시값 비교. LoadSettings 재호출 전/후 각각.

## 4. 수정 방향 (조사 후 택1 또는 병행)

- **(A) 페이지 클로저 정규화(권장, 국소적)**: 지역 캡처 클로저를 전부
  `() => unit.Config.PickUp.Field` / `v => unit.Config.PickUp.Field = v` 형태로 변경.
  객체 인자로 넘기는 헬퍼(AddVisionPickerOffsetItems 등)는 `Func<T>`(객체 리졸버)를 받도록 시그니처 변경.
- **(B) LoadConfig의 in-place 갱신**: `Config = LoadConfig(...)` 교체 대신 기존 인스턴스에 값 복사.
  전 장비 공통 기반(BaseUnit) 변경이라 **파급이 큼** — 모든 유닛/컴포넌트 회귀 검토 필요. 1차 권장 아님.
- **(C) 페이지 재바인딩**: LoadSettings/레시피 Active 후 열려 있는 페이지의 SetItems 재호출(이벤트 구독).
  A와 병행 가능. UI 표시가 낡는 문제(교체 후 old 객체 값 표시)도 함께 해결.
- 캐싱(H2) 항목은 소비처별로 "매번 읽기"로 바꾸거나, 적용 시점(다음 시퀀스부터)을 UI 툴팁에 명시.

## 5. 체크리스트

### 공통
- [ ] `git status` 시작 상태 확인, 무관 변경 미포함
- [ ] 별도 OutDir 빌드 성공, `git diff --check` 통과
- [ ] 커밋은 사용자 명시 요청 시에만

### 조사 산출물
- [ ] LoadSettings/LoadRecipe 재호출 경로 목록(file:line)
- [ ] 위험 클로저(지역 캡처/객체 인자) 전수 목록 — 페이지별, 항목별
- [ ] H2(런타임 캐싱) 항목 분류표: 즉시적용 / 다음 시퀀스부터 / 보드 푸시 필요
- [ ] 재현 로그 1건 (UI 값 vs 런타임 사용값 불일치 증명)

### 수정 검증
- [ ] 값 변경 → 즉시(또는 명시된 시점에) 런타임 반영 — 재현 시나리오로 확인
- [ ] LoadSettings 재호출 후에도 UI 편집이 라이브 객체에 반영됨
- [ ] UI 표시값 == 런타임 사용값 == 파일 저장값(저장 후) 3자 일치
- [ ] 기존 정상 항목(패턴 B) 회귀 없음
