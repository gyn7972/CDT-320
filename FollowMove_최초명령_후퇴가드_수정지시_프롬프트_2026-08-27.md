# [수정 지시 + 구현 기록] FollowMoveAsync 최초 명령 후퇴 가드 — 역전 기하 대후퇴 발행 차단

작업 대상: `D:\Source\CDT-320_New`
지시자: 팀장님 (2026-08-27 "둘 다 프롬프트로 만들어라. 첫 명령 하한 가드 + Place 게이트 이식" → 같은 날 "일단 다 수정해라" 구현 승인)
상태: **구현 완료(미커밋)** — 우회 빌드 오류 0(검증 OutDir, 운영 폴더 무접촉), 실장비 검증 대기

## 0. 구현 기록 (2026-08-27)
- 본문 §2~§3 그대로 구현: [AjinAxis.cs](QMC.CDT-320/Equipment/Ajin/AjinAxis.cs)에
  `FollowMoveFirstRetreatErrorCode = -25` 상수 + Phase 1 제약 클램프 직후 가드 블록 + 07-27 허용 주석 보강.
- 로그 태그는 `AX-FOLLOW-GUARD-FIRST-RETREAT`로 통일(§2의 "AX-FOLLOW-FIRST-GUARD" 표기와 §5 표기 중 §5 채택).
- 같은 날 사건(20:52 InputVisionX AX-5)으로 게이트 이식이 4경로 전체로 확대됨 — 자매 지시서 §0 참조.
자매 지시서: `Place팔로잉_선행이동_시작게이트_이식_수정지시_프롬프트_2026-08-27.md` (같은 날 발행, 별건 — 게이트는 1차 방어, 본 가드는 축 계층 최종 방어)

> 공통 규칙(필수): ① 시킨 것만 — 본 문서의 변경 범위 외 코드 절대 수정 금지, 걸림돌 발견 시 중단 후 보고.
> ② 수정 전 변경목록 전부 고지. ③ 수정마다 로그 계측(발행/거부 사유·수치). ④ 빌드 출력이 실장비
> 폴더(`D:\CDT-320`) 직결 — 원본 저장소 Rebuild/Clean 금지, 별도 `/p:OutDir` 지정 `/t:Build`만 허용.

---

## 1. 배경 — 사건과 확정 메커니즘

### 사건 (2026-08-25, 팀장님 목격)
리어 픽커가 아웃풋 비전X를 추종(팔로잉)해 Place 진입하던 중 **갑자기 뒤로 크게 후퇴**했다가 다시 전진.

타임라인(로그 확정):
```
14:27:03  CYCLE STOP 요청 (status=Cycling, 운전 중)
14:27:17  STOP 강제     (status=CycleStopped — 드레인 완료 전 정지)
14:29~    재시작, 14:48까지 연속 운전  ← 이 경계에서 발생 추정
```
14:29~14:48 구간은 진단상세 OFF + 알람 덤프 없음이라 궤적 직접 증거는 없다.
코드 메커니즘은 아래와 같이 확정했다.

### 메커니즘 ([AjinAxis.cs](QMC.CDT-320/Equipment/Ajin/AjinAxis.cs) `FollowMoveAsync`)

Phase 1 최초 명령(현재 코드, 주석 "간격이 이미 safetyGap 미만이면 command가 현재 위치보다
뒤가 되어 후퇴 명령이 나간다 — 안전거리를 회복하는 방향이므로 허용(사용자 확인 2026-07-27)" 직하):
```csharp
double firstLeadingActual = leadingAxis.ActualPosition;
double firstBound = direction > 0
    ? firstLeadingActual + homeGap - safetyGap
    : firstLeadingActual - homeGap + safetyGap;
double firstCommand = direction > 0
    ? Math.Min(trailingTargetPosition, firstBound)
    : Math.Max(trailingTargetPosition, firstBound);
// (제약 클램프) → lastCommanded 시드 → MoveAbsoluteForFollowAsync(firstCommand ...) 즉시 발행
```
- `firstBound`는 **선행축 실측만의 함수**. `entryActual`(후행축 현재 위치, Phase 0에서 캡처)과
  비교하는 코드가 **없다**.
- Phase 0 방향 검사는 "목표 vs 후행 현재"만 본다. **"선행 vs 후행" 기하는 아무도 안 본다.**

수치 재구성(재시작 시나리오, RearPickerX←OutputVisionX, direction=+1, homeGap=515, safetyGap=37):
```
픽커 1200에 정지(정지 전 Place 주행 잔류) + 비전은 후검사 위치 561(픽커 프레임 1076 = 픽커보다 뒤)
다음 다이 목표 1217 → Phase 0 통과(1217 > 1200)
firstCommand = min(1217, 561+515−37=1039) = 1039  →  1200 → 1039 = 161mm 순간 후퇴 발행
이후 비전이 814로 회피 상승 → bound 열림 → 오버라이드 전진 = "뒤로 확 왔다가 다시 따라감"
```

### 07-27 허용 조항의 승인 범위
허용된 후퇴 = 스테이징 진입에서의 안전거리 회복. 인터락 하한(페어 SafetyDistance, 기본 10mm)
때문에 구조적 상한 ≈ `safetyGap − SafetyDistance` = 37−10 = **27mm**. 깊은 진입(기하 역전)의
100~250mm 후퇴는 이 승인 범위 밖이다.

### 기록 데이터 확인(08-12~08-25 알람 덤프 전수 스캔)
후퇴 최초 명령 0건 — 기록된 전 세션의 진입점은 798~835(스테이징)로 bound 여유 +96mm 이상.
즉 정상 운전에선 나오지 않고, **정지/알람 후 재기동 경계에서만** 노출되는 구조다.

---

## 2. 수정 원리 — 한도 초과 후퇴는 발행 대신 거부(-25) → 기존 폴백

- 가드 위치: **제약 클램프 직후**(최종 `firstCommand` 확정 후), `lastCommanded` 시드·`moveTask`
  발행 **직전**. Phase 0의 `entryActual`을 그대로 사용한다(Phase 0→1 사이 await 없음 — 직선 코드,
  확인 완료).
- 조건:
  ```csharp
  double firstBackwardExcursion = direction > 0
      ? entryActual - firstCommand
      : firstCommand - entryActual;
  if (firstBackwardExcursion > safetyGap)   // 임계 = safetyGap
      → AX-FOLLOW-FIRST-GUARD 로그 + return FailMotion(FollowMoveFirstRetreatErrorCode, ...)
  ```
- **임계 = safetyGap 근거**: 07-27 승인 회복 후퇴는 구조적으로 ≤ safetyGap − SafetyDistance
  < safetyGap → **승인분 전량 보존**(회귀 없음). 초과분 = 선행축이 후행축 envelope보다 뒤에 있는
  기하 역전 = 팔로잉 전제 붕괴 → 후퇴 발행 대신 거부가 옳다. safetyGap은 이미 검증된 스코프 내
  파라미터라 신규 설정 불요. (safetyGap은 `MinimumFollowSafetyGap=5.0` 클램프 후 값 — 그대로 사용.)
- **왜 클램프(firstCommand를 entryActual로 상향)가 아니라 거부인가**: `firstCommand==entryActual`
  발행 시 이동이 즉시 완료되어 -23(조기정지) 경로로 빠지는 등 오버라이드 루프 의미가 바뀐다.
  거부→기존 R6 폴백(일반 이동, 공유레일 wait-gate 경유)은 -21/-22/-23에서 이미 검증된 경로다.
  변경 최소 원칙.
- 거부 후 실동작(Place 기준): R6 폴백이 `JoinOutputVisionRetreat` 후 일반 이동 — wait-gate가
  비전 간격 확보까지 픽커를 **제자리 대기**시키고 전진만 시킨다. 후퇴 0.

---

## 3. 변경 범위 (1파일 — 이 목록 외 수정 금지)

**[AjinAxis.cs](QMC.CDT-320/Equipment/Ajin/AjinAxis.cs)**:

1. 에러코드 상수부(기존 -21/-22/-23 상수 인근, 현재 853~860행 부근)에 신설:
   ```csharp
   // [최초 명령 후퇴 가드 2026-08-27] 최초 명령이 후행축 현재 위치 대비 safetyGap 초과 후퇴일 때
   // (선행-후행 기하 역전 — 정지/재기동 경계에서 관측) 발행 대신 거부하는 코드.
   private const int FollowMoveFirstRetreatErrorCode = -25;
   ```
   충돌 확인 완료: FollowMoveAsync 계열 사용 코드 = -1/-11/-21/-22/-23, **-25 미사용**.
   (-24는 InputVisionXPrePositionCoordinator의 시퀀스층 위임 코드 — 다른 계층, 비충돌.)
2. Phase 1 가드 블록 삽입(위치 §2) + Motion 로그(태그 `AX-FOLLOW-FIRST-GUARD`, §5 수치 전부).
3. 07-27 허용 주석 1줄 보강: "후퇴 허용은 safetyGap 이내 한정 — 초과는 -25 거부(2026-08-27)".

다른 파일 변경 0. **이동 명령 신규 발행 0**(거부만) → 신규 인터락 통과 경로 없음.
MotionSpeedScale: 본 수정은 속도/이동을 발행하지 않으므로 해당 없음(규칙 확인함).

---

## 4. 호출부 전수 영향 점검 (5곳 — 분석 창에서 완료, 구현 시 재확인만)

| 호출부 | 후행←선행 | nonzero 처리(현행) | -25 실발생 가능성 |
|---|---|---|---|
| [PickerPlaceSequence.VisionRetreat.cs:536](QMC.CDT-320/Sequencing/Picker/PickerPlaceSequence.VisionRetreat.cs) | 픽커←아웃풋비전 | R6 폴백(일반 이동 재시도) | **있음(본 건 대상)** |
| [PickerPickUpSequence.VisionRetreat.cs:875](QMC.CDT-320/Sequencing/Picker/PickerPickUpSequence.VisionRetreat.cs) | 픽커←인풋비전 | R6 폴백 | 사실상 없음 — 인풋 기하상 역전 불가(비전 실측 상한 ~630 < 진입 700+safetyGap) |
| [InputVisionXPrePositionCoordinator.cs:863](QMC.CDT-320/Sequencing/Picker/InputVisionXPrePositionCoordinator.cs) | 비전←픽커 | nonzero 그대로 상향(기존 -21과 동일 취급) | 사실상 없음 — 인터락이 역전 기하 차단 |
| [InputDieVisionPrepareSequence.cs:3032](QMC.CDT-320/Sequencing/Picker/InputDieVisionPrepareSequence.cs) | 비전←선행 | nonzero 전파(-21과 동일) | 사실상 없음(동상) |
| [OutputPostPlaceInspectionQueue.cs:2705](QMC.CDT-320/Sequencing/OutputStage/OutputPostPlaceInspectionQueue.cs) | 아웃풋비전←픽커 | nonzero → OUT-POST-INSPECT-MOVE RaiseFailure(-21과 동일) | 사실상 없음(동상) |

결론: **어느 호출부도 -25 전용 분기 불요** — 기존 실패 코드와 동일하게 흐른다.
구현 세션은 위 5곳의 nonzero 처리를 코드에서 실제로 재확인하고, 다르면 중단 후 보고할 것.

---

## 5. 계측 로그 (필수)

거부 시 1회, Motion 카테고리:
```
AX-FOLLOW-GUARD-FIRST-RETREAT | {축명} 팔로잉 최초 명령이 후퇴 한도를 초과해 발행을 거부합니다(기하 역전). 
entryActual=, firstCommand=, firstBound=, leadingActual=, excursion=, safetyGap=, homeGap=, 
direction=, trailingTarget=, leading={선행축명}, trailingTargetName=, constraintClamped={여부} - Fail
```
`FailMotion` 메시지에도 동일 수치 포함 — **FailMotion은 LastMotionFailure 기록만 하고 자체 로그를
쓰지 않으므로**(BaseAxis.cs:573 확인) `Log.Write` 별도 호출이 반드시 필요하다.
폴백 합류는 기존 로그 재사용: "팔로잉 진입이 실패해 기존 일반 이동으로 재시도합니다. followResult=-25".

---

## 6. 검증

1. 우회 빌드: 원본 저장소 Rebuild/Clean 금지. 별도 OutDir `/t:Build`만(`/p:OutDir=<검증폴더>\ /m /v:minimal`). 오류 0.
2. 시뮬 ①(무회귀): 정상 Place/PickUp 사이클 — `AX-FOLLOW-FIRST` 정상 발행, 가드 로그 0건.
3. 시뮬 ②(재현): Place 주행 중 CYCLE STOP → 즉시 STOP → 재시작 —
   `AX-FOLLOW-GUARD-FIRST-RETREAT - Fail` → `followResult=-25` 폴백 → 일반 이동이 wait-gate 대기 후
   전진, **X 후퇴 명령 0건**(Motion 로그로 확인).
4. 실장비: 동일 시나리오 1회. 재현 시 **앱 재시작 금지·Motion 로그 보존**(굉음 건과 공유 규칙).

## 7. 롤백
가드 블록 + 상수 + 주석 보강 원복만. 타 경로 무접촉 — 단독 롤백 안전.

## 8. 범위 밖 (지시 없음 — 건드리지 말 것)
Place 게이트 이식(자매 지시서 별건), 굉음 B안(오버라이드 최소 증분 3~5mm), PickUp 게이트 이식,
임계값 레시피화, 코디네이터/호출부 코드 일체.
