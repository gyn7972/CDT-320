# HandOff — 아진 모션 설정(프로파일/INPOSITION/타임아웃) 보드 적용 작업

- 작성: 2026-08-12 (다른 계정/세션 인수인계용)
- 대상 소스: `D:\Source\CDT-320_New` (branch: master)
- HEAD: `2ccff064` "장비1"
- **미커밋 변경 3파일** (아래 §2가 전부 이 상태다):
  - `QMC.CDT-320/Equipment/Ajin/AjinAxis.cs` (+99)
  - `QMC.CDT-320/Form1.cs` (+7)
  - `QMC.Common/AjinE/AXM.cs` (+19)
- 빌드: Release 통과. **반드시 `/p:OutDir=<임시경로>`로 빌드할 것** — 기본 빌드 출력이
  실장비 실행 폴더(`D:\CDT-320`)로 직결되어 장비 실행 중 덮어쓸 수 있다.
- 실장비 검증: **미실시** (검증 방법은 §5)

---

## 0. 세션 공통 규칙 (팀장님 지시, 반드시 유지)

1. 분석 먼저, 코드 수정은 명시적 승인 후에만. 수정 전 변경 목록 전부 고지.
2. 시킨 것만 한다. 걸림돌은 임의 해결 금지 — "문제 있다, 어떻게 할까요?"로 질문.
3. 수정마다 로그 계측 필수(실장비 1회 실행으로 원인 확정 가능하게).
4. 자동 시퀀스 일반 이동은 MotionSpeedScale 적용 경로만 사용. 미적용 발견 시 보고.
5. 호칭은 "팀장님".
6. EquipmentData\Setup / Config 파일은 앱 종료 시 런타임 값으로 덮어써짐 —
   파일 확인/수정은 앱 종료 상태에서.
7. 로그 위치: 실장비 `D:\CDT-320\Log\` (Main/Alarm/CDT-320/Motion/TactTime 등).

---

## 1. 배경 — 왜 이 작업을 했나

화면(모션 설정)의 PROFILE(사다리꼴/SCurve, ACC/DEC JERK %)과
INPOSITION(HIGH/LOW/Unused, TOLERANCE, MOVE TIMEOUT ms)이 **저장만 되고
보드에 전혀 적용되지 않았다.**

- 보드 오픈 시 `AjinSystem.Open()` → `AxmMotLoadParaAll("Motor\CDT-320.mot")`가
  파라미터를 일괄 로드하고, 그 뒤로 Setup JSON 값을 쓰는 코드가 없었다.
  (`WriteSetupToBoard()`의 유일한 호출부는 주석 처리, 게다가
  `BlockSetupWriteToBoard = true`로 전면 금지 — 이 전면 금지는 **그대로 유지**했다)
- `IsInPosition`은 설정과 무관하게 항상 보드 INP 신호(`GetInPositionValue`)였다.
  8/12 20:38 `PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER-TIMEOUT` 알람(두 축 모두 목표
  도달했는데 INP=False 30초)이 이 계열의 실사고.
- `ActualPosition`은 항상 지령(GetCommandPosition) 반환(의도 설계였음).
- 이동 대기 타임아웃은 TEST 임시상수 300초 고정(`DefaultAxisMoveTimeoutMs`).

팀장님 결정 사항:
- 프로파일/INPOSITION 설정이 보드에 먹도록 하되 **다른 설정값은 절대 쓰지 말 것**.
- INPOSITION=Unused면: INP 신호 대신 수식 판정 + ActualPosition은 **실제 엔코더**.
- 수식의 기준은 Command(이동 중 궤적값)가 아니라 **최종 목표**여야 함 →
  구현은 "정지 상태(!IsMoving) + |엔코더−보드지령| ≤ Tolerance"
  (정지 후엔 보드지령=최종목표이므로 동일. 이동 중엔 무조건 false라 궤적 오판 없음).
- ContiSegmented 모션의 SCurve 강제 2곳은 그대로 둔다.
- `SetInPositionEnable`의 무동작 버그도 수정.

---

## 2. 변경 상세 (미커밋 — 현재 워킹트리)

### 2-1. `QMC.Common\AjinE\AXM.cs` — SetInPositionEnable 버그 수정 (:1499)
기존: `if (enable != false) return ret;` → **enable=true면 아무것도 안 쓰고 리턴**.
현재: false → Unused(2) 기록 / true → 보드 현재값이 Unused(2)일 때만 High(1)로
활성화, 이미 Low/High면 보존. (레벨 지정은 `SetInPositionLevel` 사용)
※ 컴파일되는 AXM은 `AjinE\AXM.cs` 하나다. `Ajin\AXM.cs`는 csproj에 미포함(레거시).

### 2-2. `AjinAxis.ActualPosition` 게터 (:55)
- `Setup.InPosition == Unused` → `AXM.GetActualPosition`(실제 엔코더)
- Low/High → 기존대로 지령 반환 (메모리의 "지령=의도 설계" 규칙은 Low/High에서만 유효.
  **Unused 축은 로그/티칭의 actual 의미가 실측으로 바뀐다** — 로그 해석 주의)

### 2-3. `AjinAxis.UpdateStatus` — Unused 축은 `GetInPositionValue` 호출 스킵

### 2-4. `AjinAxis.ApplyReadStatus` — IsInPosition 판정 분기
```csharp
Unused:  IsInPosition = !mot && |FromBoard(act) − FromBoard(cmd)| ≤ InPositionTolerance
Low/High: IsInPosition = inp (보드 INP 신호, 기존과 동일)
```
act=엔코더 실측, cmd=보드 지령, mot=보드 InMotion. 폴백 tolerance 0.01.

### 2-5. `AjinAxis.ApplyInPositionSetupToBoard()` 신설
보드 오픈 후 `SetInPositionLevel(AxisNo, Setup.InPosition)` **한 번만** 기록
(Low=0/High=1/Unused=2 → AxmSignalSetInpos 값과 1:1). 가드: 시뮬/보드 미오픈/Setup null.
계측 로그 태그 `AxisInPositionApply`: 적용 전 보드값→설정값, ret, tolerance, moveTimeoutMs.

### 2-6. `AjinAxis` 이동 대기 타임아웃 2곳 — MOVE TIMEOUT(ms) 적용
- `WaitUntilMoveDone(serial, target)` (약 :4122): `const 300초` →
  `Setup.MoveTimeoutMs > 0 ? Setup.MoveTimeoutMs : 300000`. AX-MOVE-WAIT 알람 표기도 반영.
- `WaitMoveCompleteAsync` (약 :4028): 호출자 timeoutMs≤0일 때 폴백 동일 변경.
- 화면 60000 → 이제 60초로 동작.

### 2-7. `Form1.ApplyRuntimeMode` (약 :152~)
실보드 모드(`!forceSimulation`)일 때 전 축 루프:
`ApplyProfileSetupToBoard()` + `ApplyInPositionSetupToBoard()` 호출.
(앱 시작 + 런타임 모드 재적용 시 모두 탄다)

### 건드리지 않은 것
- `BlockSetupWriteToBoard = true` 전면 금지, `WriteSetupToBoard`와 주석 처리된 호출부
- ContiSegmented SCurve 강제 2곳
  (`PickerPickUpContiSegmentedMotion.cs:310`, `PickerPlaceContiSegmentedMotion.cs:337`)
- .mot 로드 흐름, 다른 Setup 항목 전부, 시뮬레이션 경로, TOLERANCE 값 자체

---

## 3. 직전 커밋(2ccff064 "장비1")에 이미 들어간 관련 작업

- `AjinAxis.ApplyProfileSetupToBoard()` — PROFILE(사다리꼴/SCurve + ACC/DEC JERK %)
  3개 항목만 보드 적용. 보드가 이미 같은 계열이면 raw 보존(action=keep).
  로그 태그 `AxisProfileApply`. Form1 호출 포함.

### ⚠ 확인 필요 — FeederHome VisionX 퇴피 제거(8/11 작업)가 HEAD에 없음
8/11에 Step 180/260의 VisionX 물리 퇴피 제거 + Output 인터락 Dog 게이트 작업을
완료했었는데(문서: `인수인계_FeederHOME_VisionX퇴피제거_2026-08-11.txt`,
`FeederHome_VisionX퇴피제거_수정지시_프롬프트_2026-08-11.md`),
**현재 HEAD에는 `ExecuteFeederHomeWithVisionRetreatAsync`가 다시 존재한다 = 롤백된 상태.**
누가/왜 되돌렸는지 이 세션에서는 확인 못 했다. 팀장님께 확인할 것.

---

## 4. 영향 범위 — Unused 전환 시 주의 (중요)

`InPositionTolerance` 판정처는 **약 240곳/40여 파일** (공통 AxisMoveWaiter,
AjinAxis 내부, Input/Output 유닛 위치판정 헬퍼, 픽커 시퀀스(IsPickerAxisInPosition,
ContiPlace StrongAtTarget 폴백 0.001 주의), 인터락 룰, SharedRailX, UI/캘리브레이션).
전부 `ActualPosition` 기반이므로 **Unused로 바꾼 축은 이 모든 판정의 actual이
지령→실측 엔코더로 바뀐다.** 서보 정지 편차가 TOLERANCE(0.01)보다 크면 기존에
통과하던 판정이 실패할 수 있다 → 축별로 하나씩 전환·확인 권장.

## 5. 실장비 검증 방법 (미실시 — 다음 담당자 수행)

1. 앱 재시작 → Main 로그에서 `AxisProfileApply` / `AxisInPositionApply` 검색
   → 전 축(약 37축) 적용/keep/ret 확인. `readFail`이 나오면 보드 판독 문제 물증.
2. 이동 1회 후 타임아웃 알람 발생 시 `timeoutMs=60000`으로 찍히는지 확인.
3. Unused로 바꾼 축이 있으면: 이동 완료 후 IsInPosition이 서는지,
   위치 판정(Place ContiNode StrongAtTarget 등)이 통과하는지.
4. 8/12 20:38 `PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER-TIMEOUT` 재발 여부 관찰 —
   이 알람은 INP 신호에 단일 의존이라 이번 변경(레벨 정상 적용 또는 Unused 수식)으로
   해소가 기대되는 항목.

## 6. 롤백

미커밋이므로:
```
git checkout -- QMC.CDT-320/Equipment/Ajin/AjinAxis.cs
git checkout -- QMC.CDT-320/Form1.cs
git checkout -- QMC.Common/AjinE/AXM.cs
```
(프로파일 적용은 HEAD에 커밋되어 있어 이 롤백으로 사라지지 않음)

---

## 7. 진행 중인 다른 주제 (참고 컨텍스트)

1. **보드 판독 불안정 계열 미해결**: 8/11~12에 걸쳐
   - `-3` (WaitUntilMoveDone: GetInMotion 안 내려감/판독 지연, RearPickerZ2·Z3)
   - `-5` (이동 완료 후 Command≠target = 명령 덮임, PickerX/VisionX 다수)
   - INP=False 30초 (20:38 Place ContiNode)
   - 27축 동시 amp fault 0x0001(앱 시작 시), 저속 Contact 중 servo=OFF(19:37)
   → EMG/안전회로/드라이브 전원 공통 원인 + AXM 판독 부하 의심. GetInMotion/INP
   판독 ret 계측 부재가 원인 확정을 막고 있음(계측 보강 후보).
2. **FrontPickerZ0(AxisNo 12) 소프트리밋 불일치**: 런타임 SoftLimitPlus=1 vs
   Setup 파일 20(6/20자) — 앱 종료 상태에서 확인 필요. AxisNo 9도 유사.
3. **언로더 배출-픽업 캡 작업 (분석 완료, 수정 대기)**: 배출 전 남은 Good pending
   슬롯 수만큼만 픽업하도록 캡. 확정된 방향: Good side pending 기준 + 선착순
   (캡 판정을 `ReserveNextInputStagePickTarget`의 `_stateSync` 락 안에 넣어 원자화),
   `BuildPickBatch` 계측, NG 배출은 현행 유지. **코드 수정은 제일 나중** 지시.
   상세는 대화 분석 참조: 슬롯 소비는 Place 완료 시점(`MoveDieToOutputStage`→
   `UpdateOutputReceiveSlot`), 기존 시작 게이트는 `IsOutputStageExchangePending`.
4. **택타임 분석 결과 (수정 미착수)**: no-op 재명령 페널티(구 WaitUntilMoveDone
   guard>20, 신버전은 구조 다름 — 재확인 필요), 인풋 다이검사 1.56s vs 아웃풋
   0.37s/다이, 5%에서 658UPH → 100% 환산 ~1,400-1,500UPH(고정시간 18.4s 지배).
5. **Front/Rear 픽업 경로 차이**: Rear `IsPickerAxisInPosition`만 UpdateStatus()
   선행(보드 부하), PickUp.TransferContiPickerYMaxCorrectionDistance F=3/R=1,
   유닛 Config 저장본 UseUnit F=False/R=True — Rear 문제 분석 맥락.

## 8. 관련 문서

- `FeederHome_VisionX퇴피제거_수정지시_프롬프트_2026-08-11.md` (§3-⚠ 롤백 상태 확인 필요)
- `인수인계_FeederHOME_VisionX퇴피제거_2026-08-11.txt`
