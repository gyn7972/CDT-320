# STAGE 64 — SPEC: 검사별 카메라 파라미터 오버라이드

- **작성일**: 2026-05-28
- **단계**: SPEC (구현 전, 사용자 컨펌 대기)
- **Stage 번호 근거**: docs/ 최신 = `STAGE63_RESULT_RenameSideInspection.md`, 코드 최신 마커 = Stage 63 → **NN = 64**
- **브랜치**: `stage-64-per-insp-camera-override` (base: `stage-63` 리네임 결과 — FrontSide/RearSide 모듈 존재해야 26 검사 ID 가 정확)
- **git**: 작업 시작 시 working tree clean 확인

---

## 1. 목표 & 범위

### 목표
알고리즘(5) 내부의 **개별 검사(총 26개)** 마다 카메라 파라미터를 따로 설정(오버라이드)할 수 있게 한다.

### 범위 (In Scope)
- 설정 항목: **카메라 파라미터만** — Exposure / Gain / FrameRate / TriggerMode / PixelFormat / Delay / ROI
- 상속 모델: **알고리즘 기본값 상속 + 검사별 선택적 오버라이드** (빈 값 = 상속)
- 데이터 모델 (`InspectionCameraOverride`) + UI (TreeView 3레벨 + `InspectionOverridePanel`) + 영속화 (override 만 직렬화)
- **본 Stage 는 SPEC + CHECKLIST 까지** — 구현·런타임 적용은 Stage 65(분리)

### 범위 외 (Out of Scope)
- 런타임 파라미터 적용 (Grab 직전 `EffectiveFor` 호출) — Stage 65
- 동일 CameraId 모듈 간 카메라 인스턴스 공유/직렬화(lock) — 별도 Stage
- 알고리즘 파라미터(Threshold/Template 등)의 검사별 분리 — 별 작업 (`Ui\Editors\` 5종은 별 레이어)

---

## 2. 현황 분석 (코드 직접 재확인)

### 2.1 5 모듈 × 26 검사 (사전 조사와 **완전 일치** — 줄번호 재확인)

| 모듈 (Algorithm) | base 이름 | 위치 | 검사 ID | 개수 |
|---|---|---|---|---|
| `WaferVisionModule` | `"WaferVision"` | `Modules\WaferVisionModule.cs:22-28` | EjectPinFinder / ReticleFinder / AlignDieFinder / FirstReferenceFinder / SecondReferenceFinder / DieFinder / ScaleFinder | 7 |
| `BinVisionModule` | `"BinVision"` | `Modules\BinVisionModule.cs:18-21` | ReticleFinder / DieFinder / PlacementInspector / ScaleFinder | 4 |
| `BottomInspectionModule` | `"BottomInspection"` | `Modules\BottomInspectionModule.cs:21-27` | ReticleFinder / ColletFinder / DieFinder / SurfaceInspector / FocusFinder / ScaleFinder / DistortionCompensation | 7 |
| `FrontSideInspectionModule` | `"FrontSideInspection"` | `Modules\FrontSideInspectionModule.cs:20-23` | DieEdgeFinder / TopSurfaceInspector / TopChippingInspector / FocusFinder | 4 |
| `RearSideInspectionModule` | `"RearSideInspection"` | `Modules\RearSideInspectionModule.cs:20-23` | DieEdgeFinder / BottomSurfaceInspector / BottomChippingInspector / FocusFinder | 4 |
| **합계** | | | | **26** |

검사 ID = `VisionModule.AddFinder/AddInspector(id)` 인자 문자열 (`Finders`/`Inspectors` Dictionary 키).

### 2.2 중복 InspectionId — 키 충돌 없음 (정지 조건 명시)
여러 알고리즘이 동일 InspectionId 를 가짐:
- `ReticleFinder`: Wafer / Bin / BottomInspection
- `DieFinder`: Wafer / Bin / BottomInspection
- `ScaleFinder`: Wafer / Bin / BottomInspection
- `FocusFinder`: BottomInspection / FrontSide / RearSide
- `DieEdgeFinder`: FrontSide / RearSide

→ override 는 **(Algorithm, InspectionId) 조합으로 유일**. `Inspections` 리스트가 `AlgorithmCameraMapping`(알고리즘 스코프) 내부에 있으므로, 알고리즘 안에서 InspectionId 만으로 유일. **키 충돌 없음.**

### 2.3 현재 데이터 모델 / UI 한계
- `AlgorithmCameraMapping` (`QMC.Common\Recipes\AlgorithmCameraSubset.cs:40`, Clone `:64`): 알고리즘 1개에 카메라 1대 + 파라미터 1셋. **검사 단위 분기 없음.**
- `AlgorithmCameraSubset.Items` (`:86`), `Clone()` (`:145-149`).
- `SettingsPage` (`QMC.Vision\Ui\Pages\SettingsPage.cs`): TreeView 2레벨. `LoadAlgorithms` (`:86`) — "카메라 매핑" 루트(`camRoot`) 아래 알고리즘 5개 자식(`cam:<alg>`, `:94`). 검사 자식 노드 없음. `Tree_AfterSelect` (`:116`) — `cam:` → CameraMappingPanel, `insp:` → ParameterEditor.
- 결과: **알고리즘별까지만 설정, 검사별 설정 UI 부재.**

### 2.4 편집기 5종은 별 레이어
- `Ui\Editors\` 의 `BottomInspectionParameterEditor` 등은 **알고리즘 파라미터**(Threshold/Template) 편집기 — 카메라 파라미터와 무관. 본 Stage 는 손대지 않음. (혼동 주의 → §12, MISMATCH 기록)

---

## 3. 기능 요구사항

| ID | 요구 |
|---|---|
| FR-1 | 알고리즘 노드 클릭 = 기존 `CameraMappingPanel` 그대로 (알고리즘 기본값 편집) |
| FR-2 | TreeView 에 알고리즘 자식으로 26 검사 노드 추가 |
| FR-3 | 검사 노드 클릭 → 새 `InspectionOverridePanel` 표시 (필드별 "기본값 사용" 토글 + 값 입력) |
| FR-4 | 효과적 파라미터 = override 있으면 override, 없으면 알고리즘 기본값 (`EffectiveFor`) |
| FR-5 | Save = `algorithm_camera.json` 에 override **만** 직렬화 (null 필드 제외, `EmitDefaultValue=false`) |
| FR-6 | 구버전 JSON 호환 = override 없으면 알고리즘 기본값만 사용 (현행 동작 유지) |
| FR-7 | 기본값 복원 (Reset) = 검사 1개 override 전부 클리어 → 상속만 |
| FR-8 | 취소 (Cancel) = 미저장 변경 폐기, 디스크에서 리로드 |
| FR-9 | Validation = 잘못된 ROI(W·H 한쪽만) / 범위 밖 값 입력 거부 |

---

## 4. 데이터 모델 (의사 C# — 확인 필요 #2)

```csharp
// QMC.Common\Recipes\AlgorithmCameraSubset.cs 에 추가
[DataContract]
public class InspectionCameraOverride
{
    [DataMember] public string InspectionId { get; set; } = "";   // 예: "EjectPinFinder"

    [DataMember(EmitDefaultValue = false)] public double? ExposureUs        { get; set; }
    [DataMember(EmitDefaultValue = false)] public double? Gain              { get; set; }
    [DataMember(EmitDefaultValue = false)] public double? FrameRate         { get; set; }
    [DataMember(EmitDefaultValue = false)] public string  TriggerMode       { get; set; } // null/"" = 상속
    [DataMember(EmitDefaultValue = false)] public string  PixelFormat       { get; set; }
    [DataMember(EmitDefaultValue = false)] public int?    DelayBeforeGrabMs { get; set; }
    [DataMember(EmitDefaultValue = false)] public int?    RoiOffsetX        { get; set; }
    [DataMember(EmitDefaultValue = false)] public int?    RoiOffsetY        { get; set; }
    [DataMember(EmitDefaultValue = false)] public int?    RoiWidth          { get; set; }
    [DataMember(EmitDefaultValue = false)] public int?    RoiHeight         { get; set; }

    public InspectionCameraOverride Clone();
    public bool IsEmpty();   // InspectionId 제외 모든 필드 null/공백 → true (상속만)
    /// <summary>base 위에 override 를 덮어 효과적 매핑 생성 (null 필드는 base 유지).</summary>
    public AlgorithmCameraMapping ApplyOver(AlgorithmCameraMapping baseDefaults);
}

// AlgorithmCameraMapping 에 추가
[DataMember(EmitDefaultValue = false)]
public List<InspectionCameraOverride> Inspections { get; set; }  // 기본 null/빈 — 구버전 호환

public InspectionCameraOverride GetOrCreateOverride(string inspectionId);
public AlgorithmCameraMapping        EffectiveFor(string inspectionId);  // override.ApplyOver(this)
```

- `DataContractJsonSerializer` + `Nullable<T>` + `EmitDefaultValue=false`: 채워진 필드만 JSON 출력.
- JSON 예시:
```json
{ "Algorithm":"Wafer", "CameraId":"169.254.129.26",
  "ExposureUs":5000, "Gain":1.0,
  "Inspections":[
    { "InspectionId":"EjectPinFinder", "ExposureUs":12000 },
    { "InspectionId":"AlignDieFinder", "Gain":2.5, "RoiOffsetX":100, "RoiOffsetY":100, "RoiWidth":1024, "RoiHeight":1024 }
  ]
}
```

---

## 5. 검사 ID → 한글 라벨 (26개 — 확인 필요 #1)

`AlgorithmCameraSubset.cs` 에 정적 사전 `InspectionLabel.Get(algorithm, inspectionId) → 한글` 추가 제안. **아래는 안 — 사용자 컨펌 필요:**

| 알고리즘 | InspectionId | 한글 라벨(안) |
|---|---|---|
| Wafer | EjectPinFinder | 이젝트 핀 |
| Wafer | ReticleFinder | 레티클 |
| Wafer | AlignDieFinder | 얼라인 다이 |
| Wafer | FirstReferenceFinder | 첫째 기준 |
| Wafer | SecondReferenceFinder | 둘째 기준 |
| Wafer | DieFinder | 다이 |
| Wafer | ScaleFinder | 스케일 |
| Bin | ReticleFinder | 레티클 |
| Bin | DieFinder | 다이 |
| Bin | PlacementInspector | 안착 검사 |
| Bin | ScaleFinder | 스케일 |
| BottomInspection | ReticleFinder | 레티클 |
| BottomInspection | ColletFinder | 콜렛 |
| BottomInspection | DieFinder | 다이 |
| BottomInspection | SurfaceInspector | 표면 |
| BottomInspection | FocusFinder | 포커스 |
| BottomInspection | ScaleFinder | 스케일 |
| BottomInspection | DistortionCompensation | 왜곡 보정 |
| FrontSide | DieEdgeFinder | 다이 에지 |
| FrontSide | TopSurfaceInspector | 상면 |
| FrontSide | TopChippingInspector | 상면 칩핑 |
| FrontSide | FocusFinder | 포커스 |
| RearSide | DieEdgeFinder | 다이 에지 |
| RearSide | BottomSurfaceInspector | 하면 |
| RearSide | BottomChippingInspector | 하면 칩핑 |
| RearSide | FocusFinder | 포커스 |

> 참고: FrontSide/RearSide 의 inspector 내부 id 는 Stage 63 에서 백엔드 호환 위해 `Top*/Bottom*` 으로 유지됨. 라벨은 "상면/하면" 이 아니라 모듈 의미(앞/뒤 측면)에 맞춰 다시 조정할지 확인 필요.

---

## 6. UI 와이어프레임

```
┌── 알고리즘 선택 (TreeView 3 레벨) ──┬── 우측 디테일 ──────────────────────┐
│ ■ 카메라 매핑                       │ 검사 카메라 오버라이드 — 이젝트 핀    │
│   ▼ 웨이퍼 비전 (알고리즘 기본값)   │ 알고리즘: 웨이퍼 비전                 │
│      이젝트 핀          (override ●)│ 카메라:   169.254.129.26 (상속, RO)   │
│      레티클                         │ ─ 필드별 토글 ──────────────          │
│      얼라인 다이                    │ [✓기본값] Exposure(μs) [ 5000 ] (상속)│
│      ...                            │ [ override] Exposure(μs) [12000]      │
│   ▶ 빈 비전                         │ [✓기본값] Gain         [  1.0 ]       │
│   ▶ 바텀 검사                       │ [✓기본값] FrameRate    [  30  ]       │
│   ▶ 앞쪽 측면 검사                  │ [✓기본값] Trigger      [Software]     │
│   ▶ 뒤쪽 측면 검사                  │ [✓기본값] PixelFormat  [Mono8 ]       │
│ ■ 검사 알고리즘                     │ [✓기본값] Delay(ms)    [  0   ]       │
│   ...                               │ [ override] ROI (W×H+X+Y)             │
│                                     │ [저장][기본값 복원][취소][테스트 그랩]│
│                                     │ Status: ...                           │
└─────────────────────────────────────┴───────────────────────────────────────┘
```
- 검사 노드 옆 `●` = override 1개 이상 존재 (확인 필요 #4: 색/문자).
- 필드 옆 "✓ 기본값" 체크 해제 시 입력칸 활성 = override 입력 (확인 필요 #3: 체크박스 vs 라디오).

---

## 7. 시퀀스 (Save / Load)

- **Load**: 기존 `AlgorithmCameraMapStore.Load` 가 신 필드 `Inspections` 도 함께 역직렬화 (DataContract 기본). override 없는 구버전 JSON → `Inspections` = null/빈 → 현행 동작 유지 (FR-6).
- **Save**: 각 override 의 모든 값 필드가 null 이면 (`IsEmpty()`) 그 항목을 `Inspections` 에서 제거 → JSON 비대화 방지 (FR-5/D-1).
- 알고리즘 기본값 변경 시 검사 override 영향 없음 (상속 관계 유지).

---

## 8. 런타임 적용 (범위 밖 — 설계만 명시)

- Stage 65 후보: `VisionModule.Grab(string inspectionId)` 오버로드 또는 `VisionTcpServer` 명령 핸들러가 `mapping.EffectiveFor(inspectionId)` → `AlgorithmCameraBinder.TryApplyParameters` → Grab.
- 같은 카메라 공유 검사의 직렬화(lock) — 별도 Stage.

---

## 9. 알람
- 본 Stage 신규 알람 **없음** (데이터/UI만).
- Stage 65(런타임) 후보: `VISION-OVERRIDE-FAIL` (override 파라미터 적용 실패).

---

## 10. 영향 범위

### 신규 파일
- `QMC.Vision\Ui\Pages\InspectionOverridePanel.cs` (신규 UserControl)

### 수정 파일 (정확 위치는 CHECKLIST)
| 파일 | 변경 |
|---|---|
| `QMC.Common\Recipes\AlgorithmCameraSubset.cs:40-76, 84-150` | `InspectionCameraOverride` 추가, `AlgorithmCameraMapping.Inspections` + 헬퍼, `Clone()` 깊은 복사, `InspectionLabel` 사전 |
| `QMC.Vision\Ui\Pages\SettingsPage.cs:86-103, 116-130` | TreeView 3레벨화, 노드 키 규약, 라우팅 |
| `QMC.Vision\Ui\Pages\CameraMappingPanel.cs` | 알고리즘 모드 그대로 (변경 최소) |
| `QMC.Vision\Config\AlgorithmCameraMap.cs` (Store) | Save 시 빈 override 정리 |

---

## 11. 시퀀스 문서 04~08 영향
- **직접 영향 없음** (Vision 카메라 설정 UI/모델 확장). 명시.

---

## 12. Mismatch / 결정 충돌
- `Ui\Editors\` 5종 ParameterEditor(알고리즘 파라미터) ≠ 본 Stage(카메라 파라미터 검사별 오버라이드). **별 레이어** — `MISMATCH_RESOLUTION_LOG.md` 1행 기록.
- 중복 InspectionId(ReticleFinder/DieFinder/ScaleFinder/FocusFinder/DieEdgeFinder)는 알고리즘 스코프로 유일 — 충돌 없음 (§2.2).

---

## 13. 확인 필요 항목 (사용자 컨펌 요청)
1. 한글 라벨 26개 안 (§5) 그대로 채택? FrontSide/RearSide 의 "상면/하면" 라벨을 "앞쪽/뒤쪽" 의미로 바꿀지?
2. 데이터 모델 클래스명 `InspectionCameraOverride` 채택?
3. UI 토글 방식 — 필드별 체크박스 vs "전부 상속 / 일부 override" 라디오?
4. 검사 노드 override 표시 — `●` 색/문자?
5. ROI 는 4필드 동시 override(W+H+X+Y 한 묶음)로 묶을지, 개별 필드별 override 로 둘지?
6. 런타임 적용을 본 Stage 와 합칠지 분리할지 (현재 분리 결정 유지?)

---

## 14. 다음 단계
SPEC + CHECKLIST 컨펌 → 구현 Stage(65) 분리 진행. 예상 작업 시간: 5~8 시간 (모델 1.5h + UI TreeView/패널 3h + 영속화/검증 2h).
