# 마지막 피커 Bottom 촬영 시작 = Side 첫 피커 촬영 동시 시작 수정 지시

- 날짜: 2026-07-28
- 대상: `D:\Source\CDT-320_NEW` (QMC.CDT-320)
- 빌드 확인: msbuild 컴파일 통과 필수 (별도 `OutDir`, Clean/Rebuild 금지 — AGENTS.md)

> **핵심: 이 기능은 이미 전부 구현되어 있고, 켜는 경로만 없다.**
> 신규 설계가 아니라 **활성화 + 미완결 3건 마감**이 이번 작업의 범위다.
> 새 오버랩 로직을 발명하지 말 것.

---

## 0. 작업 절차 (반드시 이 순서로)

```
① 계획 수립 → ② 체크리스트 작성 → ③ 코드 작성 → ④ 체크리스트 검토
   → (FAIL 항목 존재 시: 코드 수정 → 재검토, 최대 3회 반복)
→ ⑤ 레포트
```

- 3회 반복 후에도 FAIL이 남으면 **중단**하고 실패 항목·원인·제안을 레포트에 기록. 임의 우회·인터락 완화 금지.

---

## 1. 목표 동작

```
Bottom 검사 순서: P4 → P3 → P2 → P1  (마지막 = P1)
Side 검사 순서:   P4 → P3 → P2 → P1  (첫 번째 = P4)

마지막 피커(P1)의 Bottom 촬영 시작 명령을 내리는 그 시점에
→ Side 첫 피커(P4)의 Side 0도 촬영 시작 명령을 "같이" 발행
→ Bottom P1의 MRESULT 회수와 Side P4의 90도 촬영을 병렬 진행
→ 둘 다 끝나면 Side Pipeline은 P3부터 이어서 진행
```

**기구적 성립 근거**: Side P4의 검사 X는 설계상 Bottom P1의 검사 X와 **같은 좌표**다.
`ResolveBottomReferencePickerNoForSide(P4) = 5 - 4 = 1` → Side X 앵커 = Bottom P1의 X이고,
`targetX = sideXAnchor + (sideXAnchor - bottom[P1].X) = sideXAnchor` (`BuildSideTargetPlan` :542-580).
즉 PickerX 한 위치에서 P1은 바텀 카메라 위, P4는 사이드 비전 위에 동시에 놓인다.
PickerY도 `_inspectionFixedY`로 Bottom P4 진입 시점부터 Side 종료까지 고정되어 있다(:350-361).

---

## 2. 현재 코드 (사전 조사 완료 — 계획 단계에서 재확인)

대상 파일: `QMC.CDT-320/Sequencing/Picker/PickerBottomAndSideInspectionSequence.cs`

### 2-1. 이미 완성된 부분 (건드리지 말 것)

| # | 내용 | 위치 |
|---|---|---|
| C1 | 마지막 Bottom 타겟 판정 + Side 첫 타겟 준비 | `RunBottomPipelineAsync` :855-863, `PrepareAutoFirstSideTargetForLastBottom` :2382-2462 |
| C2 | Side P4 촬영 위치 선행 준비(X/VisionY0 병렬 → Z → T0) | :892-894, `PrepareSideTargetForInspectionAsync` :2523-2563 |
| C3 | Bottom 촬영 전 대기까지 끝낸 뒤 **제품 문맥 재확인 → 두 요청 병렬 송신** | :896-906, `StartBottomAndPreparedSide0InspectionAsync` :1512-1525 |
| C4 | Bottom P1 MRESULT 회수 ∥ Side P4 90도 촬영 → `Task.WhenAll` | :939-947, `CompletePreparedFirstSideInspectionDuringLastBottomAsync` :2464-2508 |
| C5 | Side Pipeline이 이미 촬영된 P4를 건너뛰고 P3부터 진행 | `RunSidePipelineAsync` :2032-2038 |
| C6 | 중첩 자격 재판정 + 해제 경로 | `CanStartSpecialBottomSideOverlap` :484-504, `DisableSpecialBottomSideOverlap` :2726-2733 |
| C7 | 실패/Abort 시 미완료 Vision 요청 정리 | :274-295, `AbandonPendingVisionInspections` :2793, `DrainCapturedSideResultsAfterFailureAsync` :2760 |

### 2-2. 미완결 부분 (이번 작업 범위)

| # | 문제 | 위치 |
|---|---|---|
| **D1** | **`_parallelFirstSideEnabled`가 `BuildPickedPickerList`에서 무조건 `false`로 고정되고, 코드 어디에서도 `true`로 세팅하지 않는다.** 따라서 C1~C6 전부가 죽은 경로다 | :328, :348 (전 저장소 grep 결과 `= true` 대입 0건) |
| **D2** | 활성화 게이트로 쓰라고 만들어 둔 `HasValidFourPickerOverlapContext`(4-Picker 풀 적재 + 4개 전부 AutoVision 문맥 유효)가 **한 번도 호출되지 않는다** | :404-423 |
| **D3** | 로그에 `specialBottomP1SideP4Overlap=false` 문자열이 **하드코딩**되어 실제 상태를 반영하지 않는다 | :372 |
| **D4** | ⚠️ **오버랩 경로에서 `EnsureBottomReadyForSideAsync(P4)`를 건너뛴다.** 일반 Side 경로(:2040)는 이걸로 (a) 동일 Picker Bottom MRESULT 수신, (b) `ValidateRuntimeSideInspectionCorrection` 검증을 강제하는데, 오버랩 경로에는 두 단계가 모두 없다. `BuildSideTarget(P4)`(:2420)가 P4 MRESULT 도착 **전에** 호출되면 `correctionValid=false`가 되어 Side Vision Y 보정이 **0으로 조용히 무효화**된다 | :2420, 대비 :2040, `EnsureBottomReadyForSideAsync` :1563-1592 |
| **D5** | `PrepareAutoFirstSideTargetForLastBottom`이 계획된 Side X/Y를 Bottom X/Y로 **말없이 덮어쓴다**(:2436-2439). 1장 근거대로 원래 같은 값이어야 하므로, 덮어쓰기는 **불일치를 은폐**한다. 티칭이 틀어지면 P4가 엉뚱한 X에서 촬영된다 | :2436-2448 |
| **D6** | 오버랩 경로의 `PrepareSideTargetForInspectionAsync` → `MoveSideXAndVision0PositionAsync`(:2805)가 **PickerX 이동 명령을 발행**한다. D5 덮어쓰기로 목표가 현재 X와 같아 `CanSkip`으로 무명령이 되는 것이 전제인데, 이 전제가 코드로 보장되어 있지 않다. Bottom P1 촬영 직전에 PickerX가 실제로 움직이면 **촬영 위치가 깨진다** | :892 → :2529 → :2805-2843 |
| D7 | `CompleteSideInspectionAfterSide0Async(P4)` 끝에서 `QueuePendingZAvoid(P4)` + `QueuePendingT0Return(P4)` + `StartPendingT0ReturnCommandAsync`가 실행된다 — **Bottom P1 MRESULT 수신 중**에 P4의 T가 0도로 회전한다 | :2717-2723 |
| D8 | 활성화 스위치(설정 파라미터)가 없다. `FlyingZDownMode`/`ApproachPreMotionDistanceMm`처럼 유닛 Config에 존재해야 현장에서 끌 수 있다 | `Equipment/Unit/Common/PickerTransferTypes.cs` |

---

## 3. 수정 지점 (계획 단계에서 확정)

1. **[D8] 활성화 스위치 신설**
   - `PickerBottomInspectionMotionConfig`(`PickerTransferTypes.cs`)에 `bool LastBottomFirstSideOverlapMode` 추가.
   - **기본값 `false`** (기존 동작 유지 — 현장에서 켜서 검증). `[DataMember]` + `OnDeserializing` 기본값으로 구설정 파일 하위호환.
   - `BottomFlyingZDownMode` / `BottomApproachPreMotionDistanceMm`와 동일한 방식으로 `PickerFrontUnit.cs` / `PickerRearUnit.cs`에 프로퍼티 노출 + `FrontPickerRecipePage.cs` / `RearPickerRecipePage.cs`에 항목 추가.

2. **[D1+D2] 활성화 경로 연결** — `BuildPickedPickerList` :347-348
   ```
   _parallelFirstSideEnabled =
        스위치 On
     && Options.RunMode == Auto
     && _sidePipelineEnabled
     && HasValidFourPickerOverlapContext(out reason)      // D2의 죽은 함수를 여기서 사용
     && _pickedPickerIndexes 순서가 4→3→2→1 전체 (부분집합 아님)
   ```
   - 미충족이면 `false` + **사유 로그 1건**(어느 조건에서 걸렸는지). Fail 아님 — 조용히 기존 순차 동작.
   - 신규 판정식 발명 금지. `HasValidFourPickerOverlapContext`를 그대로 쓴다.

3. **[D3] 로그 정정** — :372의 하드코딩 `false`를 `_parallelFirstSideEnabled` 실제 값으로. 미활성 시 사유도 같이 남긴다.

4. **[D4] ⚠️ 최우선 — Side 진입 자격 검증 복원**
   - `PrepareAutoFirstSideTargetForLastBottom`에서 `BuildSideTarget(P4)` **호출 전에** `EnsureBottomReadyForSideAsync(ToPickerIndex(4), ct)`를 await 한다.
     - P4 Bottom MRESULT는 P4 촬영 직후 백그라운드로 이미 수집 중(`StartBottomMResultCollection` :1594)이므로, P1 접근 시점에는 통상 완료되어 대기 비용이 0에 가깝다. **완료를 가정하지 말고 반드시 await 한다.**
     - 이를 위해 `PrepareAutoFirstSideTargetForLastBottom`을 `async Task<int>`로 바꾸고 호출부(:860)도 await 한다.
   - 실패하면 **오버랩만 해제**(`DisableSpecialBottomSideOverlap`)하고 기존 순차 경로로 폴백한다. 시퀀스 Fail로 만들지 않는다.
     - 단, `EnsureBottomReadyForSideAsync`가 반환한 Fail 코드는 Side Pipeline에서 어차피 다시 만나므로 **삼키지 말고 그대로 반환**하는 편이 맞는지 계획 단계에서 판정하고 근거를 레포트에 쓴다.
   - `ValidateRuntimeSideInspectionCorrection(P4)` 결과가 유효할 때만 오버랩을 진행한다. `SideCorrectionValid=false`인 채로 Side P4를 촬영하는 경로를 **남기지 않는다.**

5. **[D5+D6] 좌표 덮어쓰기 → 일치 검증으로 교체** — :2436-2439
   - `sideTarget.X = lastBottomTarget.X` 대입을 **검증으로 바꾼다**:
     `|sideTarget.X - lastBottomTarget.X| <= 축 InPositionTolerance` 그리고 Y도 동일 판정.
   - 불일치 시 **오버랩 해제 + 사유 로그**(기존 순차 경로 폴백). 덮어써서 진행하지 않는다.
   - 그 위에 D6 보강: `PrepareSideTargetForInspectionAsync` 진입 시점에 `CanSkipPickerMoveCommand(PickerX, sideTarget.X)`가 **true임을 확인**하고, false면 오버랩 해제 + 폴백. Bottom 촬영 위치에서 PickerX가 움직이는 경로를 원천 차단한다.
   - `MoveSideXAndVision0PositionAsync` 자체는 **수정하지 않는다**(일반 Side 경로 공유). 오버랩 경로에서 전제가 성립하는지만 사전 판정한다.

6. **[D7] P4 후처리 타이밍 확인**
   - `CompleteSideInspectionAfterSide0Async` :2717-2723의 P4 T0 복귀·Z Avoid 예약이 Bottom P1 결과 수신 구간과 겹치는 것이 의도인지 판정하고 근거를 레포트에 남긴다.
   - Bottom P1은 이미 EPD를 받은 뒤(노광 종료)이므로 다른 피커의 T 회전이 촬영에 영향을 주지 않는다는 것이 전제다. **이 전제를 코드/로그로 확인**하고, 성립하지 않으면 T0 복귀 발행을 `Task.WhenAll` 이후로 미루는 안을 제안만 한다(임의 변경 금지).

7. **로그**: 신규 분기마다 기존 규약 로그 추가 — 활성화 판정 결과·사유, D4 대기 ms, D5 좌표 일치 판정값(sideX/bottomX/차이), 오버랩 해제 사유. 기존 로그 문구 삭제 금지.

---

## 4. 인터락 전수 점검표 (계획·검토 단계에서 전 항목 판정)

### A. 무변경 유지 + 통과 확인
| # | 항목 | 확인 내용 |
|---|---|---|
| A1 | **두 피커 Z 동시 하강** — Bottom P1이 Bottom Z, Side P4가 Side Z로 동시에 내려간 상태 | `VerifyFront/RearPickerYAvoidBlocksZDown`(Y는 fixed-Y 전진 상태이므로 비해당), `MotionGuard` PickerZ row, 기구 간섭(바텀 카메라 / 사이드 비전 구조물)을 **티칭 좌표로** 확인 |
| A2 | `_inspectionFixedY` 고정 불변식 | `VerifyInspectionFixedY`가 오버랩 경로의 모든 기존 지점에서 그대로 호출되는지 (:1474, :2562, :2807, :2838, :2867) |
| A3 | `RealtimeCollisionSupervisor` PickerYFacingDistance | Y 전진 상태 유지 구간이 길어지지 않음(기존과 동일 구간) 확인 |
| A4 | 작업영역 예약 `BottomAndSideInspection` | Bottom 단계와 Side 단계가 같은 예약 안에서 겹침 — `EnsureBottomSideProcessAreaReserved`(:3798)가 두 번 호출돼도 안전한지 확인 |
| A5 | SideVisionY 축 이동 | Bottom 촬영과 병렬로 `FrontSideVisionY`/`RearSideVisionY`가 움직임 — `VisionInterlockRules` 통과 dry-run |
| A6 | PickerT 회전 (P4 0→90도) | Bottom P1 결과 수신 구간과 겹침 (D7). MotionGuard PickerT row 통과 확인 |

### B. 신규 확인 필요 (계획 단계 해소)
| # | 항목 | 내용 |
|---|---|---|
| B1 | **P4 MRESULT 미도착 시나리오** (D4) | P1 접근 시점에 P4 MRESULT가 아직 없을 때의 대기 시간 실측/추정. 대기가 길면 오버랩 이득이 사라지므로 타임아웃·폴백 기준을 정한다 |
| B2 | **Side X == Bottom X 전제 붕괴 시나리오** (D5/D6) | 티칭이 틀어졌을 때 폴백이 확실히 걸리는지. 실제 티칭값으로 두 좌표 차이를 계산해 레포트에 수치로 기록 |
| B3 | Vision 채널 동시 사용 | Bottom 채널과 Side 채널(Front/Rear 0도)에 **같은 순간** REQ가 나감(:1521-1523). `AutoVisionRequestService` / `VisionTcpClient.Correlated`가 채널별 독립인지, 상관관계(correlation) 충돌이 없는지 확인 |
| B4 | 결과 회수 순서 | Bottom MRESULT ∥ Side 90도 EPD 병렬 후, `_pendingSideResults.Count == _pickedPickerIndexes.Count` 검증(:2067)이 P4 선촬영으로 깨지지 않는지 |
| B5 | 실패 경로 | 두 요청 중 하나만 실패했을 때 나머지 Vision 핸들 정리(:930-937, C7) 동작 확인. 특히 Side만 실패 시 Bottom 완료 처리 후 반환하는 현재 코드(:933-936)가 옳은지 |
| B6 | CycleStop/Abort 타이밍 | 오버랩 진행 중 정지 시 P4가 "촬영은 됐지만 결과는 미회수" 상태로 남는 경로(`_sideCapturedPickerIndexes`) 복구 확인 |

### C. 금지·제약
1. **인터락 판정 로직 완화 금지.** 필요하다고 판단되면 수정하지 말고 **보고**.
2. `MotionSpeedScale`: 신규 이동 명령 금지. 기존 헬퍼만 재사용.
3. **오버랩 로직(C1~C7) 재설계 금지.** 활성화 + D3~D7 마감만 한다.
4. Bottom/Side 촬영 프로토콜(REQ/EPD/MRESULT/RESULT) 무변경.
5. 4-Picker 풀 적재가 아닌 경우, Auto가 아닌 경우, 스위치 Off인 경우 **기존 동작과 diff 0**.
6. 첫 피커 접근 구간 Z 선행 하강 작업(`Bottom첫피커_90mm접근_Z선행하강_수정지시_프롬프트_2026-07-28.md`)과 **같은 파일을 건드린다.** 두 작업을 동시에 진행하지 말고 순서를 정해 하나씩 완료·검증한다.
7. 기존 로그 문구 삭제 금지.

---

## 5. 체크리스트 (④ 검토에서 PASS/FAIL + 근거)

### 기능
- [ ] F1. 스위치 On + Auto + 4-Picker 풀 적재(4→3→2→1) 조건에서만 `_parallelFirstSideEnabled=true`
- [ ] F2. `HasValidFourPickerOverlapContext`가 실제 게이트로 호출됨 (죽은 코드 해소)
- [ ] F3. Bottom P1 촬영 시작 명령과 Side P4 0도 시작 명령이 **같은 대기 조건에서 병렬 발행**됨 (코드 경로 추적)
- [ ] F4. Bottom P1 MRESULT 회수와 Side P4 90도 촬영이 병렬, 둘 다 완료 후 진행
- [ ] F5. Side Pipeline이 P4를 건너뛰고 P3부터 정상 진행, 최종 EPD 카운트 일치(:2067)
- [ ] F6. **D4 해소** — P4 Bottom MRESULT + `ValidateRuntimeSideInspectionCorrection` 통과 후에만 Side P4 목표 생성. `SideCorrectionValid=false` 촬영 경로 없음
- [ ] F7. **D5/D6 해소** — 좌표 덮어쓰기 제거, 일치 검증 + `CanSkip(PickerX)` 확인, 불일치 시 폴백
- [ ] F8. **D3 해소** — 로그가 실제 활성 상태·사유 반영
- [ ] F9. 스위치 Off / 비Auto / 4-Picker 미만에서 기존 동작 diff 0
- [ ] F10. 오버랩 해제(`DisableSpecialBottomSideOverlap`) 후 순차 경로가 P4를 정상 촬영

### 인터락 (4장 전 항목)
- [ ] L1. A1~A6 통과 근거 (dry-run / 코드 추적 / 티칭 좌표)
- [ ] L2. B1~B6 해소 근거 (B2는 실 티칭 수치 첨부)
- [ ] L3. **인터락 완화 0건** 증명 (Interlocks 폴더 diff 없음)

### 회귀
- [ ] R1. 비Auto/수동 경로 무변경
- [ ] R2. 첫 피커 접근 구간 Z 선행 로직(:1131-1190) diff 없음
- [ ] R3. Vision 프로토콜 경로 diff 없음
- [ ] R4. 실패/Abort 시 Vision 핸들 정리 경로 동작
- [ ] R5. msbuild 컴파일 통과

---

## 6. 레포트 양식

1. 계획 요약 (3장 항목별 채택/변경/기각 + 사유, B1~B6 해소 내용, D7 판정 결론)
2. 변경 파일·핵심 diff 요약 (**Interlocks 폴더가 diff에 포함되면 즉시 중단·보고**)
3. 체크리스트 전 항목 PASS/FAIL 표 + FAIL 이력 (반복 회차별)
4. 신규 로그 문구 목록 + 실장비 검증 가이드
   - 확인할 로그 패턴: `parallelFirstSideEnabled=True`, `Bottom #1 / Side #4 검사 시작 명령을 같은 대기 조건에서 연속 발행합니다`, D4 대기 ms, D5 좌표 일치 판정값
   - 비교 지표: 4-Picker 1배치의 Bottom 시작 → Side 전체 EPD 완료까지 택트 (스위치 Off/On)
5. 별도 이슈 보고 (범위 제외 발견 사항)
