# 데이터 관리 통일성 정리 계획 (UNIFORMITY REFACTOR PLAN)

- 일자: 2026-07-03
- 근거: [OPERATION_REVIEW_20260703.md](OPERATION_REVIEW_20260703.md) PART 2
- 확정된 방침:
  - **SSOT = 비전 레시피**: 노출·조명·검사 임계값 등 촬상/검사 파라미터의 진짜 저장소는 비전 레시피. 핸들러 쪽 중복 필드는 표시 전용화 또는 제거.
  - 작업 저장소: C:\Project\CDT-320
  - 진행: 본 계획 승인 → STAGE별 구현 → 각 STAGE 후 빌드 + perl 회귀

## 불변 제약 (전 STAGE 공통)
1. **통신 와이어 포맷 변경 금지** — `MODULE|CMD|args` 라인 프로토콜, RECIPEREQ/RECIPE, GRAB 인자 구조 유지. 동작(누가 값을 쓰는지)만 변경. 프로토콜에 남는 노출/조명 인자는 하위호환용으로 유지하되 비전이 무시.
2. **CDT-310 알고리즘 코어 위배 금지** — 임계값·필터 값의 "저장 위치"만 옮기고 알고리즘 로직/기본값 의미는 변경하지 않음.
3. Sim은 항상 실제와 동일 경로/값 사용.
4. 모든 파일 UTF-8, 대용량·한글 .cs 수정은 bash+python 경유(Edit 잘림 이슈).

---

## 목표 구조 (정리 후 모습)

```
[비전 레시피]  Data\<RecipeName>\...          ← 품목이 바뀌면 바뀌는 모든 것 (SSOT)
  머신: 웨이퍼 격자, 다이 사이즈/피치
  모듈(Wafer/Bottom/Side/Bin...) → 도구(Finder/Inspector)
    ├ CameraRecipe.Exposure(us)              ← 노출 SSOT
    ├ AlgoRecipe.LightSettings(채널 레벨)     ← 조명 레벨 SSOT
    ├ AutoFocus(ROI/Threshold)               ← [이동] VisionSettings에서
    └ 검사 임계값(Chipping/Foreign/Placement) ← 임계값 SSOT

[비전 설정]    Config\vision.json, light_system.json  ← 장비가 바뀌어야 바뀌는 것
  카메라별: CameraId, Scale, Delay, 조명 컨트롤러/페이지 매핑
  디바이스 목록 = 카메라 + 조명 + (추가 디바이스), 전부 동일 패턴 + 매뉴얼 테스트 버튼

[핸들러 레시피] Recipes\<Project>.Project     ← 핸들러 구동 데이터만
  픽업 순서, 모션 좌표, 웨이퍼 사양(구동용)
  검사 관련 필드(ExposureMs/LightIntensity/DieSubset 임계값) = 표시 전용(1단계) → 제거(최종)
```

---

## STAGE U1 — 노출·조명 SSOT 일원화 (P0)

**문제**: ExposureMs(ms)↔Exposure(us) 이중 관리·변환 없음, 조명 3원 관리.

작업:
1. 비전: GRAB/EXPOSE 처리 시 핸들러가 보낸 노출/조명 인자를 **무시하고 활성 레시피 값 사용** (인자는 파싱만 유지 — 포맷 불변). `VisionCommandCore` 촬상 경로 확인 후 레시피 값 주입 지점 단일화.
2. 핸들러: 레시피 UI의 ExposureMs/LightIntensity 입력란 → 읽기 전용 + "비전 레시피에서 관리" 라벨. 시퀀스 코드에서 이 값 참조 제거(전달값은 하위호환용 고정).
3. 조명: 채널 레벨 SSOT = `AlgoRecipeBase.LightSettings`(현행 유지). 핸들러 LightIntensity 참조 제거. `light_system.json`은 "하드웨어 정의(설정)"로 역할 명문화 — 코드 변경 없음, 주석/문서만.
4. 마이그레이션: 없음(비전 레시피 값이 이미 실사용 값).

수정 파일(예상): `QMC.Vision\...\VisionCommandCore.cs`, 핸들러 `RecipeStore.cs`(주석/Obsolete), 레시피 편집 UI 페이지, `PickerProcessSequence` 촬상 호출부.
검증: 핸들러에서 GRAB 요청 → Timing/LightApply 로그로 비전 레시피 노출·조명 적용 확인. `perl tools/verify_vision_features.pl`.

## STAGE U2 — AutoFocus ROI/Threshold 레시피 이동 (P0)

**문제**: `VisionSettings.AutoFocusRois`(전역) — 레시피별 관리 불가, Threshold=100 하드코딩.

작업:
1. `AutoFocusRoiSet`(Camera,Target,Rois)+Threshold를 비전 레시피(머신 또는 모듈 레벨)로 이동. 권장: 모듈 레시피에 `AutoFocus` 섹션.
2. 마이그레이션: 레시피 로드 시 레시피에 AF 데이터 없으면 VisionSettings 전역값을 1회 복사 후 저장(전역 필드는 Obsolete 유지 → 추후 제거).
3. AF ROI 설정 페이지 저장 대상을 활성 레시피로 변경.

수정 파일: `VisionConfig.cs`, 레시피 데이터 클래스, `AutoFocusRoiPage`, AF 실행 경로.
검증: 레시피 A/B 전환 시 ROI 달라지는지, 기존 장비 설정 자동 이관 확인.

## STAGE U3 — 검사 임계값 SSOT = 비전 (P0~P1)

**문제**: ChippingDepthMax 등이 핸들러 DieSubset + 비전 VisionMachineRecipe 중복 정의.

작업:
1. 판정 기준 = `VisionMachineRecipe`(비전) 확정. 비전 판정 경로가 자체 레시피 값만 쓰는지 확인·고정.
2. 핸들러 DieSubset 임계값 필드: UI 읽기 전용(비전 값 표시용) 또는 제거. RECIPE 동기 페이로드에 임계값이 있으면 포맷은 유지하되 비전이 무시.
3. 검사 활성화 Enable 일원화: 모듈 검사 on/off = 핸들러(공정 스킵 여부), 도구 세부 Enable = 비전 레시피 — 역할 구분 명문화, 이중 제어 지점 주석 처리.

검증: 임계값 변경 → NG 판정 변화가 비전 레시피 수정으로만 발생하는지.

## STAGE U4 — 레시피 이름 동기 단일화 + default 폴백 제거 (P1)

작업:
1. 마지막 레시피 저장을 핸들러 `.last_project` 1곳으로. 비전 `LastRecipeName`은 캐시로 강등 — 기동 시 RECIPEREQ로 핸들러에 조회(기존 프로토콜 활용), 핸들러 부재 시에만 캐시 사용.
2. `VisionMachine`의 `"default"` 치환 제거: 레시피 미선택 시 검사/시퀀스 차단 + 상태바 경고. 단독 테스트용으로는 명시적 레시피 선택 강제.

검증: 양쪽 재시작 후 동일 레시피 자동 복원. 레시피 없이 GRAB 시 명확한 거부 메시지.

## STAGE U5 — 설정 UI 디바이스 패턴 통일 (P1)

**문제**: 카메라만 "디바이스별 구성+매뉴얼 테스트" 패턴, 조명·AF는 제각각.

작업:
1. 설정 페이지 구조를 "디바이스 목록(좌측) → 파라미터 + [테스트] 버튼(우측)" 공통 패턴으로: 카메라(기존 CameraMappingPanel), 조명 컨트롤러(채널 레벨 슬라이더+즉시 점등 테스트 추가), 이후 디바이스는 같은 패턴으로 하단 추가.
2. 조명 테스트는 LightHub 경유 실점등(레시피 저장과 무관한 임시 적용, 페이지 이탈 시 원복).
3. AF CUDA on/off 전역 → 카메라별 이동은 보류(실사용 요구 확인 후).

검증: 각 디바이스에서 매뉴얼 테스트 동작, 신규 디바이스 추가 절차 문서화.

## STAGE U6 — 하드코딩 정리 (P2)

- `D:\CDT-320`, `EmguCV` 폴백 경로 → 설정 필수화(빈값이면 기동 시 설정 유도).
- BottomInspector 필터(TopHatRadius=21 등)·Side/Bin 0.05 → 레시피 스키마 승격(값 의미 불변, 위치만).
- TrainRoi 초기값 → 센서 해상도 기반 계산.
- VisionSettings 구버전 Scale 필드 등 잔존 필드 Obsolete 처리.

---

## 진행 순서·리스크

| STAGE | 규모 | 리스크 | 선행 |
|---|---|---|---|
| U1 노출·조명 | 중 | 낮음(비전 값이 이미 실사용) | - |
| U2 AutoFocus | 중 | 중(마이그레이션) | - |
| U3 임계값 | 소 | 낮음 | U1 |
| U4 이름동기·default | 소 | 중(기동 시나리오) | - |
| U5 설정 UI | 대 | 낮음(UI 위주) | U1 |
| U6 하드코딩 | 중 | 낮음 | U2,U3 |

각 STAGE 완료 시: 빌드(3 csproj) → `perl tools/verify_all.pl` → 해당 기능 수동 확인 → REPORT 추가.

## 이 계획에서 아직 열려 있는 결정
1. 핸들러 중복 필드(ExposureMs/LightIntensity/DieSubset 임계값)를 **읽기 전용 유지 vs 완전 제거** — 1단계는 읽기 전용 권장(레시피 파일 하위호환), 안정화 후 제거.
2. U2에서 AF를 모듈 레시피 vs 머신 레시피 어디에 둘지 — 카메라별 특성이므로 모듈 권장.
3. U5 조명 채널 레벨의 "설정 탭 테스트"와 "레시피 편집 값"의 관계 — 테스트는 임시 적용만, 저장은 레시피에서만(권장).
