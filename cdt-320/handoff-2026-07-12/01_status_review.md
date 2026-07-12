# CDT-320 인계 상태 점검 (2026-07-12)

## 점검 목적

`HANDOFF_TO_CLAUDE_2026-07-12.md` 인계 내용을 현재 HEAD 기준으로 재검증한 기록이다.
인계 문서는 HEAD `e57f7532` 기준으로 작성되었으나, 이후 커밋 `f595e603`이 추가되어
현재 상태가 문서와 달라졌는지 확인했다.

## 소스 문서

- `CLAUDE.md` — 진입점, 인계 문서 우선 참조 지시
- `AGENTS.md` — 고정 코딩 규칙 (Designer, 예외/로그/알람, Task<int> 모션, 인코딩 등)
- `HANDOFF_TO_CLAUDE_2026-07-12.md` — Codex 인계 본문

## Git 상태 (점검 시점)

```text
branch: master == origin/master
HEAD:   f595e603 오토포커스 구연완료.... 테스트전   ← 인계 문서 작성 이후 커밋
        e57f7532 Merge latest origin/master Side gate updates  ← 인계 시점 HEAD
working tree: CLAUDE.md 수정(인계 안내 추가), HANDOFF 문서 untracked
```

## 핵심 확인 결과

### 1. Handler Clean Rebuild — 여전히 실패 (오류 2건 재현)

별도 `OutDir`/`BaseIntermediateOutputPath`로 `/t:Rebuild` 수행 결과:

```text
VisionCameraCalibrationTransform.cs(103,27) CS0103: 'bottomOffsetX' 이름이 현재 컨텍스트에 없습니다.
VisionCameraCalibrationTransform.cs(104,27) CS0103: 'bottomOffsetY' 이름이 현재 컨텍스트에 없습니다.
```

인계 문서의 오류 2건이 현재 HEAD에서도 그대로 재현된다.

### 2. 커밋 `f595e603`은 오류를 수정하지 않았고 오히려 재도입했다

해당 커밋은 `VisionCameraCalibrationTransform.cs` 한 파일만 수정했으며:

- `OffsetX = 0.0 / OffsetY = 0.0` (서버 계약: Place Offset 미사용) →
  `OffsetX = bottomOffsetX / OffsetY = bottomOffsetY`로 변경. 그러나 `bottomOffsetX/Y`
  지역 변수 선언은 추가하지 않아 CS0103이 그대로 남았다.
- `TryReadValidatedBottomOffset(bool 반환)` →
  `ReadValidatedBottomOffset(double 반환, 50mm 초과 시 0.0)`으로 리팩터링했으나
  **현재 아무 곳에서도 호출하지 않는다** (미사용 상태).
- `BottomCenterOffsetX/Y` 읽기가 검증 헬퍼 경유에서 `TryGetDoubleValue` 직접 호출로
  바뀌면서 **절대 픽셀 차단(|value| ≤ 50mm) 검증이 이 지점에서 제거**되었다.
  (`IsFinite` 검사만 남음)

### 3. 커밋 의도 추정 (확인 필요)

미사용 `ReadValidatedBottomOffset` 헬퍼와 `OffsetX = bottomOffsetX` 변경으로 볼 때,
작성자(김영남)는 Place용 `OffsetX/Y`에도 검증된 Bottom Offset을 연결하려다
선언부(`double bottomOffsetX = ReadValidatedBottomOffset(result, ...)`)를
누락한 것으로 보인다. 이는 인계 문서 §3.2의 "권장 방향 2번(Place에도 Bottom X/Y 적용)"에
해당하며, 인계 문서가 더 일관적이라고 본 "1번(Offset 0 고정)"과 반대 방향이다.

## 수정 후보 (팀장님 결정 필요)

| 안 | 내용 | 근거 |
|---|---|---|
| A | `OffsetX/Y = 0.0` 복원 (서버 계약 유지, BottomCenterOffsetX/Y만 Side에 사용) | 인계 문서 권장 1번, 주변 주석과 일관 |
| B | `double bottomOffsetX = ReadValidatedBottomOffset(result, "bottom_offset_x_mm", ...)` 선언 추가 (Place에도 Bottom Offset 적용) | 마지막 커밋 `f595e603`의 의도로 추정 |

어느 쪽이든 `BottomCenterOffsetX/Y` 읽기에서 제거된 50mm 절대 픽셀 차단을
복원할지 함께 결정해야 한다. (생산 Runtime에는 2mm 제한이 별도로 있으나,
Collet Cal Side AF 계산 경로는 이 지점 검증에 의존했다.)

## 인계 문서의 미해결 위험 항목 (변동 없음)

1. ~~Handler 컴파일 오류 2건~~ → 여전히 존재 (위 참조)
2. Side 0/90 축 매핑 불일치 (Collet Cal: 0도←Y / 생산 Runtime: 0도←X) — 실장비 검증 전 확정 금지
3. AF Best + Runtime Offset 이중 적용 가능성 — 공정 의도 확인 필요
4. Bottom/Side 병렬 실행 제거(`bottomSideParallel=False`) — 과거 확정 요구와 상이, 재확인 필요
5. Side AF 중 Zone Tag `ColletCalibration;PickerZone=Bottom` 적절성 — 인터락 레포트 없이 수정 금지
6. Bottom Overall NG여도 Side AF 진행하는 정책 — 공정 정책 확인 필요

## 진행 결과 (2026-07-12, 팀장님 B안 승인 후)

### 적용한 수정

팀장님이 **B안**을 승인하여 `f595e603` 커밋의 의도대로 유실된 선언 2줄을 복원했다.
선언 형태는 병합 전 원본 커밋 `54f58ba9`에서 그대로 가져왔다.

- 파일: `QMC.CDT-320/Equipment/Calibration/VisionCameraCalibrationTransform.cs`
- `ToBottomVisionOffset(int, InspectionResultDto)` 내부에 추가:

```csharp
// Place 보정용 Bottom Offset은 mm 소량 값만 허용하는 검증 경로로 읽는다.
double bottomOffsetX = ReadValidatedBottomOffset(result, "bottom_offset_x_mm", "bottom_item_offset_x");
double bottomOffsetY = ReadValidatedBottomOffset(result, "bottom_offset_y_mm", "bottom_item_offset_y");
```

- `ReadValidatedBottomOffset`은 `|value| > 50mm`(절대 픽셀 유입)을 0.0으로 차단하므로
  Place Offset 경로에는 검증이 유지된다.
- `BottomCenterOffsetX/Y` 읽기의 50mm 차단 제거(f595e603의 변경)는 팀장님 지시가
  없어 되돌리지 않았다. (IsFinite 검사만 수행, 생산 Runtime에는 2mm 제한 별도 존재)

### Clean Rebuild 결과 (별도 obj/out)

- `QMC.CDT-320.csproj /t:Rebuild` → **성공** (CS0103 2건 해소, 경고 4건은 기존 CS0162)
- `QMC.Vision.csproj /t:Rebuild` → **성공** (경고는 기존 VisionInspector 항목)

### 남은 확인

- OffsetX/Y가 0.0 → 실값으로 바뀌므로 생산 Place 소비 경로 영향을 멀티에이전트로
  전수 추적/검증 중. 결과는 `02_impact_verification.md`에 기록 예정.
- 이후 실장비 검증은 인계 문서 §13/§14 순서를 따른다.
