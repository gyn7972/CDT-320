# C3b-1 — 카메라 구 타입 정리 (저위험) (Step 0 확인, 보고 후 정지)

> 작성 2026-06-11. 브랜치 `refactor-baseunit-c3b1-camera-cleanup`(master=C3a 머지본 `f52ab87` 위).
> **Step 0: 확인 — 보고 후 정지.** 확인 = ①참조0 ③Common 수정(C3 최초 QMC.Common 편집). 컨펌 후 Step 1~3.
> 목표: C3a 로 참조 0 된 카메라 구 타입만 제거 — (1) `AlgorithmCameraMapStore` 파일 통째 삭제(Vision), (2) `InspectionCameraOverride` 클래스 + `AlgorithmCameraMapping` per-inspection 카메라 멤버 제거(Common). **조명·결선·컨트롤러 무관(C3b-2/b-3).**

---

## §A 감사 결과 (솔루션 전체 grep — 정의 외 참조)

### A-1. 참조 0 확인 (정지조건 해당 없음)
- **`AlgorithmCameraMapStore`**: 정의 외 참조 **0**(C3a 가 Save/Load/마이그 다리 전부 제거). `QMC.Vision/Config/AlgorithmCameraMap.cs` 의 **유일 public 타입**(`:15`) → 파일 통째 삭제.
- **`InspectionCameraOverride`**: 정의 외 참조 **0**(C3a 가 InspectionOverridePanel·뱃지 드롭).
- **per-inspection 카메라 경로**(`GetOrCreateOverride`/`EffectiveFor`/`ApplyOver`): 정의 외 참조 **0**.
- `.Inspections` grep 잔여 = 무관:
  - `QMC.CDT-320/Equipment/Materials/*`(MaterialSnapshotStore·MaterialStateService) = **Die 소재 검사기록**(`die.Inspections`, InspectionType 레코드) — `AlgorithmCameraMapping.Inspections` 와 **다른 타입**.
  - `QMC.Vision/Config/AlgorithmCameraMap.cs:72-77` = **삭제 대상 파일 내부**(Store cleanup).

### A-2. Handler(QMC.CDT-320) 영향 = 0 (Common 수정 안전)
- Handler 의 `AlgorithmCameraMapping`/`AlgorithmCameraSubset` 참조 **0**.
- Handler 의 `GetOrCreateOverride`/`EffectiveFor`/`ApplyOver` 참조 **0**.
- → Common 죽은 카메라 타입 제거가 Handler 빌드를 깨지 않음.

### A-3. AlgorithmCameraMapping DTO 유지 참조처 (카메라 필드 전용)
- `AlgorithmCameraBinder`(적용 엔진) / `Form1.RebindAlgorithmCamera`(DTO 파라미터) / `IVisionModule`·`VisionModule`(Import/Export) / `CameraMappingPanel`(편집 버퍼) — 전부 **카메라 필드만**(per-inspection 미사용).
- 테스트: `C2LightSmoke`(주석 1줄만), `P1Smoke`(구 param-rework 스모크 — 빌드 무관, 후속 정리 후보).

---

## §B 제거/유지 (Step 1~2)

### Step 1. Vision 파일 삭제 (저위험)
- `QMC.Vision/Config/AlgorithmCameraMap.cs` 삭제 + `QMC.Vision.csproj` 의 `<Compile Include="Config\AlgorithmCameraMap.cs" />` 제거. 빌드0(Vision).

### Step 2. Common 죽은 클래스 제거 (`QMC.Common/Recipes/AlgorithmCameraSubset.cs`)
**제거:**
- `AlgorithmCameraMapping.Inspections` 멤버(`[DataMember] List<InspectionCameraOverride>`).
- `AlgorithmCameraMapping.GetOrCreateOverride(inspectionId)`.
- `AlgorithmCameraMapping.EffectiveFor(inspectionId)`(Inspections+ApplyOver 의존).
- `AlgorithmCameraMapping.Clone()` 의 Inspections 복제 블록(InspectionLights 복제는 유지).
- `InspectionCameraOverride` 클래스 전체(ApplyOver/IsEmpty/HasRoiOverride/Clone 포함).

**유지:**
- `AlgorithmCameraMapping` 카메라 DTO 필드(Algorithm/CameraId/ExposureUs/Gain/FrameRate/TriggerMode/PixelFormat/DelayBeforeGrabMs/Roi*·IsRoiFull·ToRectangle).
- `AlgorithmCameraMapping.InspectionLights`·`GetLightOverride`·`GetOrCreateLightOverride`(조명, C3b-2 까지).
- `VisionAlgorithm`·`InspectionLabel`·`AlgorithmCameraSubset`(Items/Get/Migrate*/EnsureDefaults/Clone — InspectionLights 사용분 포함, 조명 무관 정리는 C3b-2).

### Step 3. 게이트
전체 빌드0(**QMC.Common→QMC.CDT-320 Handler→QMC.Vision**) / 정적0 / verify 코어 FAIL0 / 스모크(CameraSsot·C2Light·PanelLight) / R2 보존 / Handler 무수정·빌드0 / push 안 함.

---

## ★ 확인 필요 (2건)
1. **참조 0**: `AlgorithmCameraMapStore`·`InspectionCameraOverride`·per-inspection 카메라 경로 = 솔루션 전체 정의 외 참조 **0**(Handler 의 die.Inspections 는 무관 타입). 이 전제로 삭제 진행 OK?
3. **QMC.Common 부분 수정**: C3 최초 Common 편집(죽은 카메라 타입 제거뿐, 조명·DTO·VisionAlgorithm·InspectionLabel 보존). Handler 참조 0 확인됨. 팀 공유 파일이므로 진행 컨펌 필요. OK?

## 정지 조건
참조 0 깨짐 / Common 수정 후 Handler 빌드 깨짐 / verify 회귀 / R2 깨짐 → 즉시 정지·보고. **Step 0 보고 전 구현 금지.**
