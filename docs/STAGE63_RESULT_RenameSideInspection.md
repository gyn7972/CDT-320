# STAGE 63 — RESULT: TopSide/BottomSide → FrontSide/RearSide 리네임

- **작업 완료일**: 2026-05-28
- **브랜치**: `rename-sideinsp-front-rear` (base: `fix-vision-ui-overlap` b12f352)

## Stage 번호 근거
**NN = 63** — docs/ 에 STAGE63 산출물 부재 (STAGE62 가 최신), 코드 최신 마커 Stage 62 → +1.

## 배경
기존 `TopSide`/`BottomSide` 는 수직면(상/하)을 암시했으나 실제로는 다이의 **앞쪽/뒤쪽 측면을 카메라로 검사**. 이름-의미 불일치 해소.

## 변경 통계
- 수정 파일: **20** (코드 13 + verify 4 + 문서/데이터 3)
- 이름변경(git mv): **2** (모듈 .cs)
- 줄: +189 / -85 (리네임 커밋 기준)
- 신규: 0
- **활성 코드 TopSide/BottomSide 잔존: 0** (Legacy DataMember / 마이그레이션 비교 리터럴 / 주석만 허용 — verify_all STAGE63 항목 PASS)

## 리네임 매핑 (적용 완료)

| 종류 | 옛 | 새 |
|---|---|---|
| 알고리즘 상수/값 | TopSide / BottomSide | FrontSide / RearSide |
| 한글 라벨 | 상면 검사 / 하면 검사 | 앞쪽 측면 검사 / 뒤쪽 측면 검사 |
| Sim 키 | Sim/TopSide / Sim/BottomSide | Sim/FrontSide / Sim/RearSide |
| Vision 모듈 클래스/파일 | TopSideInspectionModule / BottomSideInspectionModule | FrontSideInspectionModule / RearSideInspectionModule |
| 모듈 base 이름 | TopSideInspection / BottomSideInspection | FrontSideInspection / RearSideInspection |
| Form1 필드/변수 | TopSideMod / camTopSide / _svrTopSide (+ Bottom) | FrontSideMod / camFrontSide / _svrFrontSide (+ Rear) |
| VisionHub 클라이언트/모듈명 | TopSide / "TopSideVision" (+ Bottom) | FrontSide / "FrontSideVision" (+ Rear) |
| AppSettings 포트 | VisionTopSidePort / VisionBottomSidePort | VisionFrontSidePort / VisionRearSidePort |
| RecipeProject 필드 | TopSideInsp / BottomSideInsp | FrontSideInsp / RearSideInsp |
| VisionConfig 포트 | TopSideInspectionPort / BottomSideInspectionPort | FrontSideInspectionPort / RearSideInspectionPort |
| CameraSetupPage Channel/Role | TopSide / TopSide4Side (+ Bottom) | FrontSide / FrontSide4Side (+ Rear) |

**포트 번호 5105/5106 유지** — 키 이름만 변경.

### 범위 외 의도적 유지
- inspector 내부 id `TopSurfaceInspector` / `TopChippingInspector` / `BottomSurfaceInspector` / `BottomChippingInspector` — 백엔드 finder/inspector 키 (외부 미노출). 매핑표 밖이라 유지. (M-63-3)

## 데이터 마이그레이션 라운드트립 (dev PC 검증)

| 파일 | 구 → 신 | 결과 |
|---|---|---|
| algorithm_camera.json | TopSide/BottomSide → FrontSide/RearSide, Sim/TopSide → Sim/FrontSide | ✅ Items 5/5 (Wafer/Bin/BottomInspection/FrontSide/RearSide) |
| vision.json | TopSideInspectionPort:5105 → FrontSideInspectionPort:5105 / BottomSideInspectionPort:5106 → RearSideInspectionPort:5106 | ✅ 구 키 소멸, 포트 유지 |
| settings.json | VisionTopSidePort → VisionFrontSidePort (동일 패턴) | ✅ OnDeserialized + 정규화 Save |
| RecipeProject | TopSideInsp → FrontSideInsp / BottomSideInsp → RearSideInsp | ✅ Legacy [DataMember] + OnDeserialized |

마이그레이션 메커니즘:
- **AlgorithmCameraSubset.MigrateLegacyAlgorithmNames()** — Algorithm 값 + Sim CameraId 변환 + 중복 제거. `AlgorithmCameraMapStore.Load` 가 항상 Migrate → EnsureDefaults → 정규화 Save.
- **VisionSettings / AppSettings / RecipeProject** — `[DataMember(Name="<old>", EmitDefaultValue=false)]` Legacy 프로퍼티 + `[OnDeserialized]` 콜백이 구 키 값을 새 프로퍼티로 이전 후 0/null 로 비움. Store.Load 가 정규화 Save → 구 키는 1회 로드 후 사라짐.

## 검증
- **dotnet build (솔루션)**: warning 0 (기존 무관 1건 `HikGigECamera._handle`) / error 0
- **verify_all.ps1**: **TOTAL 60 / PASS 60 / FAIL 0** (Stage 63 항목 3개 추가)
  - 활성 코드 TopSide/BottomSide 잔존 0
  - VisionAlgorithm FrontSide/RearSide + 마이그레이션 메서드 존재
  - FrontSide/RearSideInspectionModule.cs 존재
- **verify_stage43/51/52.pl**: 새 이름으로 갱신
- **Sim auto-cycle**: Vision Sim 기동/종료 정상 (라운드트립 시 확인)

## 문서
- `MISMATCH_RESOLUTION_LOG.md`: Stage 63 행 3개 추가 (M-63-1/2/3)
- `fill_doc2_doc3_data{,_v2,_v3}.json`: stage 43/51/52 라벨 새 이름 갱신 (PPT 재생성 데이터)
- `10_설정_페이지_가이드.md` / `11_설정_편집_고급기능.md` / `ARCHITECTURE_EXPORT.md`: 상면/하면 → 앞쪽/뒤쪽 측면, TopSide/BottomSide → FrontSide/RearSide
- 시퀀스 04~08: 직접 매치 없음 (영향 없음 확정)
- PPT 직접 수정은 범위 밖 (데이터만 갱신)

## git 커밋
```
WIP: camera SDK work sync (사전 미커밋 변경 보존)
Stage 63 - Rename TopSide/BottomSide -> FrontSide/RearSide + auto-migration
```

## 다음 Stage 후보
- 이름 정합성 확보 완료 → Stage 62 (Recipe별 Vision 카메라 동기) 의 Vision↔Handler 디스크/TCP 동기 구현 진입 권고.
- inspector 내부 id (Top*/Bottom*) 정합성까지 맞출지 여부 — 백엔드 영향 평가 후 별도 결정.
