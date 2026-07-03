# INPUTVISIONX_PREINSPECTION_INTERLOCK_FIX.md

InputCamera 선행검사의 InputVisionX 이동이 인터락에 막혀 RearPicker 본 공정까지 오토 정지되는 문제의 구조적 원인·수정안. Codex 인수인계용. 작업 전 [AGENTS.md](AGENTS.md) 준수, 기존 변경 유지, 실장비 충돌 방지 최우선, 신규 로그/알람 한글.

---

## 0. 발생 알람 (2026-xx 13:11)

```
Error SEQ-EX: RearPicker 자동 시퀀스 실패. result=-1
  Cause=Input die vision 준비 VisionX 이동 명령 실패. result=-11, axis=VisionX(InputVisionX),
        actual=0, target=616
  Sequence=FrontInputDieVisionPrepareSequence | Step=MoveInputStageAndVisionToDie
  Alarm=INPUT-DIE-VISION-PREPARE-STAGE-MOVE
Critical INTERLOCK InputVisionX: 이동 불가 — RearPicker가 Input 영역 점유/간섭.
  RearPicker currentZone=Input, targetZone=Input, workArea=Bottom,
  owner=RearPickerBottomAndSideInspectionSequence:...:ProcessBottom,
  x=445.999, y=-29.13, targetName="InputVisionX 이동 전 Picker Input 영역 확인"
```

물리적 사실: 공용 X 레일 위에서 **RearPickerX=445.999**(Bottom 검사 중), Front 선행검사가 **InputVisionX를 0→616**으로 이동 시도 → 같은 레일에서 접근 → 인터락이 정확히 차단. **인터락은 제 일을 했다.** 문제는 시퀀스가 이 "예상 가능한 경합"을 **치명적 실패로 만들어 본 공정을 정지**시킨 것.

---

## 1. 근본 원인 (코드 근거)

파일: `Sequencing/Picker/InputDieVisionPrepareSequence.cs`

### 원인 A — 이동 전 대기 조건이 실제 이동 인터락과 불일치
`MoveInputStageAxisCommandAsync`(axis==VisionX, :2029~2044)는 이동 전 `WaitInputVisionXSharedRailClearAsync`(:2031)로 대기한다. 그 대기는 `service.VerifySingleAxisMove(stage.CameraX, target)` — **SharedRailX 거리 clearance만** 검사한다. 그러나 실제 `MoveInputStageAxis(VisionX)`는 MotionGuard를 거치며 **PickerZone "Input 영역 점유" 규칙**(별개 판정)에도 걸린다. 대기가 그 조건을 포함하지 않으므로 → 대기 통과 후 이동이 다른 인터락에 -11로 막힘.

### 원인 B — 협조적 차단(-11)을 치명적 Fail로 처리
`MoveInputStageAxisCommandAsync`는 result!=0이면 즉시 `Fail("INPUT-DIE-VISION-PREPARE-STAGE-MOVE")`(:2056). 선행검사는 **UPH용 비침습 부가작업**인데, 그 이동이 인터락(상대 픽커가 레일 점유 = 예상 가능·일시적)에 막히면 **대기/양보/이번 사이클 skip** 해야 한다. 대신 Fail → 상위 `RearPicker 자동 시퀀스 실패 result=-1` → 오토 정지. **부가작업 실패가 본 공정을 죽인다.**

### 원인 C — 대기와 이동 사이의 레이스
대기(`VerifySingleAxisMove`)가 통과한 순간과 실제 이동 명령 사이에 RearPicker가 Input/레일 영역으로 진입하면 이동이 막힌다. 대기·이동이 원자적이지 않고, -11 시 재대기 루프가 없다.

### 원인 D — 시작 판단이 이 상황을 못 막음
설계 의도상 선행검사 시작은 `PickerProcessSequence`가 중앙 판단(`IsPickerInputZoneMotionRiskForProcess` 등)한다. 그러나 (ㄱ) Rear가 Bottom workArea인데 물리 위치가 Input zone(x=445.999)인 경우를 시작 게이트가 못 걸렀거나, (ㄴ) 시작 후 Rear가 진입한 레이스. 즉 게이트가 **공용 레일에서 InputVisionX 경로(0→616)와 상대 PickerX 위치가 겹치는지**까지 보지 않는다.

> 참고: `PickerProcessSequence.cs`, `PickerZoneInterlockRules.cs`는 현재 워킹트리에서 파일 tail이 truncate된 손상 상태(별도 보고). 아래 수정 전 두 파일 무결성 복구 선행 필요.

---

## 2. 수정 방향 (구조적)

핵심 원칙: **선행검사(비침습)의 인터락 충돌은 절대 본 공정을 Fail시키지 않는다. 대기·양보·skip한다.** 그리고 **대기 조건 = 실제 이동에 적용되는 인터락 전체**로 통일한다.

### FIX-A. 대기 조건을 실제 이동 인터락과 동일 소스로 통합
`WaitInputVisionXSharedRailClearAsync`가 SharedRailX 거리만 보지 말고, **InputVisionX 이동에 적용될 MotionGuard 전체 판정**(PickerZone Input 영역 점유 포함)을 대기 조건으로 사용. 즉 "이 이동이 지금 통과 가능한가?"를 이동과 같은 규칙으로 폴링하고, 통과할 때만 이동. (예: MotionGuardService에 `CanMove(axis,target,ctx,out reason)` dry-run 판정 노출 → 대기·이동이 동일 판정 공유.)

### FIX-B. 선행검사 이동의 인터락 차단은 Fail이 아니라 Wait/Yield
`MoveInputStageAxisCommandAsync`(및 상위 `MoveInputStageAndVisionToDieAsync`)에서 **선행검사 모드(IsInputCameraPreInspectionMode)** 일 때 인터락성 result(-11 등)는:
- 즉시 Fail 금지.
- 타임아웃 내 재대기·재시도(FIX-A 조건으로), 또는
- **이번 선행검사를 안전하게 양보/skip**(예약 반납, 다음 기회에 재개)하고 상위엔 성공/무해(0 또는 "skip")로 반환.
- 어떤 경우도 상위 PickerProcess가 result=-1로 죽지 않도록. (일반검사 모드에서는 기존 Fail 유지 가능.)

### FIX-C. 대기→이동 원자화 / -11 재대기 루프
이동 명령이 인터락 -11로 반환되면 곧바로 Fail하지 말고 FIX-A 대기 조건으로 되돌아가 재확인 후 재시도(타임아웃까지). 대기 통과와 이동 발행 간 갭에서 막히면 자연히 재대기.

### FIX-D. 중앙 시작 게이트에 "공용 레일 경로 겹침" 추가
`PickerProcessSequence`의 선행검사 시작 판단에 **상대 PickerX가 InputVisionX 이동 경로(현재~target) 및 안전거리와 겹치면 시작 금지/대기**를 추가. Rear가 Bottom workArea여도 물리 X가 Input/레일 경합 위치면 시작 금지. (workArea 논리뿐 아니라 실제 X 좌표 기준.)

---

## 3. 우선순위·불변식

- 선행검사는 **본 공정에 종속**된 부가작업이다: 본 공정(Front/Rear PickUp·Bottom·Side·Place)의 축·레일 사용이 우선, 선행검사는 양보. (이전 설계 원칙 "InputCamera 하위 시퀀스는 Picker 축을 직접 안 움직인다"에 더해, **공용 레일도 본 공정에 양보**.)
- INV: PickUp 허가가 살아 있으면 InputVisionX는 Avoid 유지(기존). 여기에 **상대 픽커가 공용 레일에서 InputVisionX 경로와 경합 중이면 InputVisionX 이동 보류**를 추가.
- 인터락 차단(-11)은 "협조 대기" 신호로 해석(선행검사 등 부가작업). 실제 임박 충돌만 하드정지. (SEQUENCE_SAFETY_REMEDIATION_SPEC.md의 대기 vs 하드정지 원칙과 동일.)

---

## 4. 검증 포인트

- 정상: `"InputVisionX SharedRailX clearance 대기"`가 이제 **PickerZone Input 점유 조건까지 포함**해 대기하고, Rear가 Bottom/Side 끝나고 레일을 비운 뒤 이동.
- 재발 금지: `INPUT-DIE-VISION-PREPARE-STAGE-MOVE result=-11`로 **RearPicker 본 공정이 죽는** 로그.
- 선행검사가 인터락에 막히면 `"선행검사를 양보/보류합니다"`(신규 한글 로그) 후 본 공정 계속.
- Rear Bottom 검사 중 Front 선행검사가 InputVisionX를 Rear 쪽으로 이동시키지 않음.
- 빌드는 별도 OutDir, `perl tools/verify_all.pl` + 실운전 UPH/충돌 로그 확인.

---

## 5. 수정 대상 파일

- `Sequencing/Picker/InputDieVisionPrepareSequence.cs` — FIX-A(대기 조건 통합), FIX-B(선행검사 -11 → 양보/대기), FIX-C(재대기 루프).
- `Equipment/Interlocks/Common/MotionGuardService.cs` — dry-run 판정(`CanMove`) 노출(대기·이동 동일 소스).
- `Sequencing/Picker/PickerProcessSequence.cs` — FIX-D(공용 레일 경로 겹침 시작 게이트). **※ 현재 truncate 손상 → 복구 선행.**
- (참고) `Equipment/Interlocks/PickerZoneInterlockRules.cs` — 판정 소스 공유. **※ 현재 truncate 손상 → 복구 선행.**
