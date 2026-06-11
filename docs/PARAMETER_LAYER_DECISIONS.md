# 파라미터 계층 배정 — 12건 결정 (Cowork 검토 2026-06-09)

> 근거: 실코드 직접 확인. [확정 권장]=바로 진행 가능 / [영남 확인]=도메인·운영 판단 필요.

## 계층 세부 (#1~5)

**#1 finder AcceptThreshold/MaxInstances → Recipe** [확정 권장]
제품 패턴 매칭 허용치·찾을 인스턴스 수 = 제품별. (`SimPatternFinder.cs:14-15`)

**#2 CognexInspector Threshold/MinDefectArea/MaxTotalDefectArea → Recipe** [확정 권장]
검사 판정 임계 = 제품/공정별. (`CognexInspector.cs:21-25`)

**#3 ② ChipSpecLimit(상·하한) → Recipe** [확정 권장] / **FileSavePath → Config** [영남 확인]
ChipSpecLimit Width/Height = 제품 치수 스펙 → 제품별(Recipe). FileSavePath = 운영 경로, 장비별이면 Setup·공통이면 Config. (`InspectionParameters.cs:81-86`)

**#4 ② Pitch(Distortion)·ChipWidth/Height(VisionScale) → [영남 확인]**
캘리브 목적이나 값은 제품 치수. **제안 분리**: 입력 치수 ChipWidth/Height·PitchX/Y = Recipe(제품), 산출 보정값 ScaleX/Y = Setup(#5). 마스터칩 1회 캘리브 운영이면 전부 Setup도 가능 — 운영 방식 확인. (`InspectionParameters.cs:142-176`)

**#5 VisionSettings ScaleX/Y/Inverted(X/Y)/IsRotated → Setup** [확정 권장]
mm/px·방향 캘리브, 장비·제품 무관. (`VisionConfig.cs:107-115`)

## 추가 요청 (#6~8)

**#6 Search ROI 타깃별 → 기본 Setup + 예외 Recipe override** [영남 확인(타깃별)]
정렬계 finder(EjectPin/Reticle/AlignDie/FirstRef/SecondRef)는 기구 고정 = Setup 타당. 제품따라 검색 위치 바뀌는 finder(예: Die)는 Recipe. 일괄 Setup 기본 + #8 override로 예외 처리. (`SimPatternFinder.cs:24`, 모듈 finder 목록)

**#7 누락 카테고리 (camera/light, `algorithm_camera.json` = AlgorithmCameraMapStore)**
- CameraId / FrameRate / PixelFormat / TriggerMode / RoiOffset·Width·Height → **Config**(HW 고정) [확정 권장]
- DelayBeforeGrabMs → **Config** [확정 권장]
- **ExposureUs / Gain → [영남 확인]** — 제품·재질별 노출/게인 튜닝이면 Recipe, HW 고정 취급이면 Config. (제 lean: **Recipe**)
- InspectionLights: **Level/On/Strobe/StabilizeDelay → Recipe**(검사별 조명 레벨, 제품별) / **Channel/ControllerPort(결선) → Setup**(HW 배선) [확정 권장 — 분리]

**#8 파라미터×타깃 계층 override 허용 → 허용** [확정 권장]
디스크립터가 (Target+Layer) 단위라 (target,key)별 계층 지정이 설계상 자연스러움(무료). 휴리스틱 기본 + 도메인 예외 override.

## id·매핑·구조 (#9~12)

**#9 ②↔인스턴스 매핑** (모듈 등록부 기준)
- BottomInspectionParameters → **BottomInspection/SurfaceInspector** [확정] (`BottomInspectionModule.cs:26`)
- DistortionParameters → **BottomInspection/DistortionCompensation** [확정] (finder, `:29`)
- VisionScaleParameters → **<module>/ScaleFinder + VisionConfig.ScaleX/Y** [확정-ish] (각 모듈 ScaleFinder)
- **SideInspectionParameters(Surface enum 4면) → [영남 확인]**: 대상 인스턴스 = FrontSide/TopSurfaceInspector·TopChippingInspector, RearSide/BottomSurfaceInspector·BottomChippingInspector(`FrontSide/RearSideInspectionModule.cs:23-24`). Surface(FrontWidth/BackWidth/FrontHeight/BackHeight) ↔ 이 4 인스턴스 정확 대응 확인 필요.
- **DieGapInspectionParameters → [영남 확인]**: 모듈에 명시적 DieGap 인스펙터 없음(Bin/PlacementInspector 또는 별도). 실사용처 도메인 확인.

**#10 Setup 저장 단위 → 통합 단일 파일** [확정 권장]
`Config/Setup/vision_setup.json`(내부 타깃 섹션). Setup 항목 적고 장비 글로벌 → 파일 1개가 단순. Handler `EquipmentDataStore`/`JsonDataStore<T>` 패턴.

**#11 Recipe 아키텍처 — [영남 결정 필수, 최우선]**
⚠ Handler `RecipeProject`(`RecipeStore.cs:210, 288-318`)가 **이미 Vision 검사 파라미터 보유**: InspectionSubset×3(BottomInsp/TopSideInsp/BottomSideInsp — Enable/ExposureMs/LightIntensity/ChippingDepthMaxMm/ScratchAreaMaxMm2/MinDieCenterScore), DieSubset(ChipSpecLimit·ChippingDepth/Length·ForeignSizeMax). 즉 **제품 레시피의 검사 파라미터를 Handler가 이미 소유.**
- (가) Vision이 Handler `.Project` 공유 읽기 (SSOT=Handler) — 제품=Handler 소유라 개념적으로 옳고 중복 0. 단 팀이 Handler 작업 중이라 파일·스키마 결합 위험.
- (나) Vision 별도 스토어(패턴 미러) — 결합 0이나 **같은 제품 검사 파라미터가 Handler·Vision 두 곳 → 발산 위험**.
- **제 lean(절충)**: (나)로 시작하되 ① Handler InspectionSubset/DieSubset 과 **1:1 매핑 가능한 스키마**로(나중 (가) 수렴 대비), ② 중복 필드(ExposureMs/LightIntensity/ChipSpecLimit/Chipping…)는 **어느 쪽이 SSOT인지 운영 규칙 1줄 명시**. → 운영 정책이라 영남 최종 결정.

**#12 ParameterEditorBase 5종 → P1~P3 병존, P4에서 그리드 흡수 후 폐기** [확정 권장]
재작업 중 제거 시 깨짐 → 최후 정리.

---

## ★ 최종 확정 (영남 결정 + Cowork 추가조사 2026-06-09)

**#11 Recipe 아키텍처 → (나) Vision 별도 스토어 [확정].**
조사 결과 Handler `RecipeProject` 의 InspectionSubset(×3)·DieSubset 비전 필드는 **정의만 되어 있고 Handler 어디서도 읽히지 않음**(실사용은 `ModuleSubset.BottomInspectionEnable` enable 플래그뿐). = 옛 비전-핸들러 통합 프로그램 **잔재(vestigial)**. → SSOT 충돌 없음, Vision 이 자기 Recipe 소유.
- Handler 레시피 파일: `./Recipes/<ProjectName>.Project` (`RecipeStore.cs:13,18`). Vision: `./Recipes/<tool>.json`(현) → 신 `Recipes/<default>/<target>.recipe.json`. **각자 bin·별도 프로세스 → 파일 충돌 없음.**
- Handler 잔재 InspectionSubset 정리는 **팀 몫(우리 범위 밖)**. Vision 은 신경 안 씀.

**#7 → ExposureUs = Recipe / Gain = Config [확정].** (제품·재질별 노출만 Recipe, Gain HW 고정)

**#4 → ChipWidth/Height·PitchX/Y = Recipe [확정].** 현재 저장: `Recipes/VisionScale.json`·`Recipes/Distortion.json` (`ParameterEditorBase.cs:23`) → 신 Recipe 스토어로 마이그레이션. (보정 산출 ScaleX/Y 는 #5 Setup 유지)

**#3 → FileSavePath = Setup [확정].** (경로 = 장비별)

**#9 매핑 [확정 + 일부 보류]:**
- BottomInspectionParameters → `BottomInspection/SurfaceInspector` [확정]
- DistortionParameters → `BottomInspection/DistortionCompensation` [확정]
- VisionScaleParameters → `<module>/ScaleFinder` + VisionConfig.ScaleX/Y [확정]
- **SideInspectionParameters·DieGapInspectionParameters → 연결 보류**. 현재 둘 다 인스펙터 미연결 **고아 에디터**(Side 는 Surface 드롭다운 단일셋 `SideInspectionParameterEditor.cs:37,50`, DieGap 은 대응 인스펙터 없음). 모듈에 Side 검사 인스턴스는 4개(Front/Rear×Surface/Chipping)지만 Surface enum(4면)과 1:1 대응이 코드상 불명 → **실 백엔드/매핑 명확해질 때 연결**(추측 금지). 디스크립터는 정의하되 P3 인스펙터 바인딩에서 Side/DieGap 은 제외(orphan 유지).

**확정 권장대로 진행:** #1·#2(Exposure)·#5·#6(기본 Setup+override)·#7(camera/light 결선분리)·#8·#10·#12 + #9 Bottom/Distortion/Scale.

→ 12건 전부 확정. **P1(디스크립터 모델) 구현 진입 가능.**

> **참고 — 계층 배정은 저비용·전환가능:** 계층은 디스크립터의 메타 한 줄(`Layer=...`). 나중에 바꾸면 노출 페이지·SCOPE·dirty 전부 자동 추종. 유일한 비용 = 값 저장 파일이 바뀌어 1회 재저장/소규모 마이그레이션 필요(단일 필드는 무시 수준). 따라서 단순 layer 태그(#3·#4·#7 등)는 합리적 기본으로 두고 나중에 조정 OK. 신중 대상은 **구조 결정(#11 등)**뿐.
