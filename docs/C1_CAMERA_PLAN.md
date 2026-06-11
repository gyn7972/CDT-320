# C1 — 카메라 설정 BaseUnit 흡수 (Step 0 설계, 보고 후 정지)

> 작성 2026-06-09. **Step 0: 설계 — 보고 후 정지(컨펌, 특히 1·4).** 컨펌 후 Step 1~5.
> 브랜치 `refactor-baseunit-c1-camera`(master=BaseUnit 일원화 위). 목표: 모듈 Config/Recipe 가 카메라 구동 SSOT.

---

## §A 현재 카메라 구동 (코드)
- Form1 부팅(`Form1.cs:88-98`): `CreateCameraForAlgorithm(map, alg, fallback)` 로 카메라 생성 → 모듈 ctor 에 주입. `ApplyDelayFromMap`(`:237`)로 DelayBeforeGrabMs 적용.
- `CreateCameraForAlgorithm`(`:198`): algorithm_camera.json(AlgorithmCameraMapStore)에서 매핑 조회 → `AlgorithmCameraBinder.CreateAndApply` 로 카메라 생성+Open+파라미터 적용.
- `AlgorithmCameraBinder.TryApplyParameters`(`AlgorithmCameraBinder.cs:34`): `ExposureUs/Gain/AcquisitionFrameRate/TriggerMode/PixelFormat/Roi` → ICamera. `ParseTrigger/ParsePixel`(문자열→enum), `CameraFactory.CreateById`(생성).
- `ICamera`(`Core/ICamera.cs`): ExposureUs/Gain/AcquisitionFrameRate/TriggerMode/PixelFormat/Roi **모두 get/set**, IsOpen/Open(). → Collect(camera→Config) 가능.
- `VisionModule` ctor: **camera 필수(null 시 throw `:48`)**. `SetCamera`(`:96`) 런타임 교체 가능. 모듈 Config(CameraId/Gain/FrameRate/Trigger/Pixel/Delay/Roi)/Recipe(Exposure) = **orphan**(저장/로드만).
- per-inspection: `InspectionCameraOverride`(`AlgorithmCameraSubset.cs:140`, InspectionId + nullable ExposureUs/Gain/FrameRate…). `InspectionOverridePanel` 이 편집 — **모듈 레벨보다 세분(검사별)**.

## §B 설계

### B-1. 카메라 생애주기 — (a) 모듈 Config 먼저
부팅 순서 변경:
```
1) module = new XxxModule(camera: null, backend)        // ctor null 허용으로 완화(아래 [확인1])
2) module.LoadSettings()                                 // 모듈 Config(CameraId 포함) 로드 + 알고리즘 노드 cascade
3) cam = CameraFactory.CreateById(module.Config.CameraId)// CameraId=생성 트리거(적용 아님)
4) module.SetCamera(cam); cam.Open()
5) module.ApplyCameraSettings()                          // Config/Recipe → camera (Gain/FPS/Trig/Pixel/Roi/Delay/Exposure)
```
- **카메라 생성 주체 [확인1]**: (가) **Form1**(권장 — 모듈은 CameraFactory 비의존, `ApplyCameraSettings`/`CollectCameraSettings` 만 노출) vs (나) 모듈 내부(`InitializeCamera()` 가 Config.CameraId 로 생성·적용, 캡슐화↑·의존↑).
- **ctor null 허용**: `VisionModule` ctor 가 camera null 허용(Camera=null, throw 제거). `Camera` 접근부는 SetCamera 전엔 없음(부팅 순서상 안전). [확인1 부속]

### B-2. 모듈 Apply/Collect (AlgorithmNode A 의 모듈판)
- `ApplyCameraSettings()`(Config/Recipe→Camera): Camera null/미오픈 가드. Gain/FrameRate/TriggerMode/PixelFormat/Roi/DelayBeforeGrabMs ← Config, Exposure ← Recipe. **Binder.TryApplyParameters 재활용**(Config/Recipe → AlgorithmCameraMapping shim 구성 후 호출) [확인3].
- `CollectCameraSettings()`(Camera→Config/Recipe): camera getter → Config/Recipe(현재 카메라 상태 스냅샷). UI 가 Config 직접 편집하면 보조적.
- `VisionModule.LoadSettings` override = base + (Camera!=null ? ApplyCameraSettings). `SaveSettings/SaveRecipe` override = (Camera!=null ? CollectCameraSettings) + base. **부팅 LoadSettings 시 Camera null → Apply 스킵**, Form1 이 SetCamera 후 ApplyCameraSettings 명시 호출.

### B-3. 마이그레이션
최초 부팅: 모듈 Config 파일 없으면 algorithm_camera.json 의 해당 알고리즘 매핑(CameraId/Gain/FrameRate/Trigger/Pixel/Roi/Delay + ExposureUs→Recipe.Exposure) → 모듈 Config/Recipe 로 이전 후 SaveSettings/SaveRecipe. **algorithm_camera.json 보존**(조명·per-inspection 은 유지).

### B-4. UI 재배선
- `CameraMappingPanel`(카메라 매핑 편집) → 모듈 Config/Recipe 편집 + 저장=module.SaveSettings/SaveRecipe + ApplyCameraSettings(라이브). R2 레이아웃 보존.
- **`InspectionOverridePanel`(per-inspection 노출 override) [확인4]**: 검사별 세분이라 모듈 Config(1개)에 안 맞음. → (가) **C1 제외, algorithm_camera.json Inspections[] 유지**(권장, InspectionLights 처럼 후속) vs (나) C1 에서 모듈 하위 구조로 흡수(설계 확장).

### B-5. 구 카메라 적용 은퇴
- `AlgorithmCameraBinder.TryApplyParameters` = **모듈 ApplyCameraSettings 가 재활용**(헬퍼 유지, CreateById 유지). Form1 의 `CreateCameraForAlgorithm`→`CreateAndApply` 중복 적용 제거(생성만, 적용은 모듈).
- algorithm_camera.json 자체·InspectionLights 는 C2 까지 유지.

## ★ 확인 필요
1. **카메라 생성 주체**: (가) Form1(권장) vs (나) 모듈 내부. + ctor null 허용 OK?
2. **InspectionOverridePanel/per-inspection override**: (가) C1 제외·후속(권장) vs (나) C1 흡수.
3. **Binder 재활용**: TryApplyParameters 헬퍼 유지(모듈이 mapping shim 으로 호출, 권장) vs 모듈에 적용 직접 구현.

## 게이트(Step 6)
빌드0 / verify코어FAIL0 / 실동작(노출·게인·ROI 편집→적용→재시작→복원, 카메라 5/5 Open) / R2 보존 / Handler 무수정.
