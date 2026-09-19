# [수정 지시 + 구현 기록] Place 준비 대기 lease 양보 — 스테이지 공급 순환 교착 근본 해소

작업 대상: `D:\Source\CDT-320_New`
지시자: 팀장님 (2026-08-25 "이것도 진행 해라" + "코드 수정도 해" — 지시서·구현 동시 승인)
상태: **구현 완료(미커밋)** — 우회 빌드 오류 0, 시뮬/실장비 검증 대기

---

## 1. 배경 — 교착 사이클 (확정 구조)

08-23 배포 전 최종점검 주석([OutputSequence.cs:2493](../../QMC.CDT-320/Sequencing/OutputSequence.cs))이 이미 기록한 교착 클래스:
"픽커는 스테이지 Ready 신호를 **리소스를 쥔 채** 기다리므로 순환 대기 교착". 당시 교정은
**바코드 게이트만** 유한 lease(30초, 실패 시 보류)로 바꿨고, Place 시퀀스의 준비 대기 본체는 남아 있었다.

```
[Place]  VerifyOutputStageReadyAsync not-ready 루프(Task.Delay 폴링)
         — 배치 2번째+ 다이부터 OutputPlaceArea(+사이드 StageArea)를 쥔 채 대기
         — 스테이지에 빈이 없으면(materialReady=false) Ready가 열릴 때까지 무한
[공급]   OutputSupply/Store/FeederResume (OutputSequence.cs:1817/1947/2042/2177)
         — AcquireOutputPlaceAreaAsync → AcquireResourceForRunAsync = Auto 무한 재시도
         — OutputPlaceArea를 영원히 대기 → 공급 불가 → Ready 신호 영원히 안 섬
```

수령 완료(full) 상태는 기존 full-wait 분기가 픽커 전체 Avoid + lease 전량 반환으로 이미
해소한다. **남은 구멍은 "빈이 없어 공급이 필요한" not-ready 대기**였다.
Good 선배출·NG 유예 수정(같은 날)은 이를 악화시키지 않지만(전부-NG 배치는 lease-free 유지,
전환 스텝은 lease 획득 안 함), 혼합 배치의 노출은 그대로였다.

## 2. 수정 원리 — 검증된 full-wait 세트의 재사용

not-ready 대기가 **임계시간(5초)** 을 넘고 lease를 보유 중이면 **1회 양보**한다:

```
① MovePickerToAvoidAfterPlaceFastAsync  — 보유 Die 상태 픽커 전체 Avoid (full-wait와 동일)
② Conti 상태 정리 (ClearPendingContiRetreat, ForceSafeYBeforeFirstPlaceMove=true 등)
③ ReleaseOutputPlaceArea / ReleaseOutputStageArea / ReleaseOutputFeederArea
④ EndOutputPostPlaceInspectionBatch     — 대기 중 후검사 진행 허용
⑤ 이후 lease 없이 폴링 계속
```

ready가 열리면 성공 분기의 `BeginOutputPostPlaceInspectionBatch`와 다음 스텝
`MoveOutputStageAvoidPosition`의 null 가드(`_outputPlaceLease/_outputStageLease == null` →
후검사 유휴 확인 후 재획득)가 **기존 경로 그대로** 배치·lease를 재개한다. 신규 재획득
코드 0줄.

- full-wait 분기와의 차이: handoff(프로세스 리소스 양도)·부모 work zone 반환은 **제외** —
  공급 경로가 필요로 하는 것은 OutputPlaceArea + 사이드 StageArea뿐이므로 사이클
  구성요소만 최소 반환한다(범위 최소화).
- 임계 미만의 일시 not-ready(신호 토글 등)는 기존과 동일하게 lease 보유 대기(택트 보존).
- 체인 순서: full-wait → 안전 Y 확보(기존 첫 반복) → **양보 타이머** — 기존 첫 반복의
  안전 Y 정리가 그대로 선행된다.
- 양보는 배치 다이당 1회(`notReadyLeaseYielded`) — 반복 왕복 없음.

## 3. 변경 범위 (구현 완료 — 1파일 1함수)

**`QMC.CDT-320\Sequencing\Picker\PickerPlaceSequence.OutputStageReady.cs`
`VerifyOutputStageReadyAsync`만:**

1. 함수 진입부: `notReadyLeaseYielded` / `notReadyWaitStartedUtc` / 임계 상수(5000ms) 선언
2. 대기 루프 체인에 양보 분기 추가(§2의 ①~⑤) — 전부 기존 헬퍼 재사용
   (`MovePickerToAvoidAfterPlaceFastAsync`, `Release*`, `EndOutputPostPlaceInspectionBatch`)

**무변경**: 인터락 규칙 0건, OutputSequence(공급 측) 0건, 신규 이동/속도 코드 0
(MotionSpeedScale — 기존 헬퍼 경로만 사용).

## 4. 계측

양보 발동 시 **레벨 지정 로그 1줄**(최소 로그 정책에서도 잔존 — 08-23 "무로그 정지" 교훈):
side/outputSide/die/pickerNo/보유했던 lease 3종/reason(미준비 사유). 이후 기존 "Place 대기 - Wait"
로그가 lease-free 상태로 이어진다.

## 5. 검증 절차

1. 우회 빌드(`/p:OutDir`) — **완료, 오류 0·대상 파일 경고 0** (기본 빌드 금지 규칙 준수)
2. 시뮬: 정상 연속 생산(스테이지 준비 상태)에서 양보 로그 **0건** 확인(5초 임계 미도달)
3. 실장비 재현(팀장님): 스테이지 빈을 비운 상태에서 Place 준비 대기 유도 →
   5초 후 양보 로그 1줄 + 픽커 전체 Avoid → 공급 시퀀스가 lease를 획득해 빈 공급 진행 →
   Ready 상승 → Place가 lease 재획득 후 정상 재개(알람 없음)
4. 회귀: full 교체 대기(수령 완료) 경로가 기존과 동일하게 동작하는지

## 6. 범위 밖

- `AcquireResourceForRunAsync`(Auto 무한 재시도) 자체의 유한화 — 08-23 결정대로 게이트별 대응 유지
- VerifyOutputStageReady의 lease 보유 대기 구조 전면 개편(항상 lease-free 대기) — 택트 영향
  검토가 필요해 이번에는 임계 기반 양보로 한정
