# STAGE 64 — CHECKLIST: 검사별 카메라 파라미터 오버라이드

- **연계 SPEC**: `docs/STAGE64_SPEC_PerInspectionCameraOverride.md`
- **작성일**: 2026-05-28
- **상태**: SPEC + CHECKLIST 작성 단계 (구현 미진입 — 사용자 컨펌 대기)
- **사전 조건**: SPEC §13 확인 필요 항목 1~6 해소

> ⚠ 표시 항목은 SPEC §13 의 옵션 결정에 의존.

---

## A. 데이터 모델 (QMC.Common)

- [ ] **A-1. `InspectionCameraOverride` 클래스 추가**
  - 대상: `QMC.Common\Recipes\AlgorithmCameraSubset.cs`
  - 필드: InspectionId + 7파라미터 nullable (`double?`/`string`/`int?`), 모두 `[DataMember(EmitDefaultValue=false)]` (InspectionId 제외)
  - 헬퍼: `Clone()`, `IsEmpty()`, `ApplyOver(AlgorithmCameraMapping baseDefaults)`
- [ ] **A-2. `AlgorithmCameraMapping.Inspections` 추가**
  - `[DataMember(EmitDefaultValue=false)] List<InspectionCameraOverride> Inspections`
  - 헬퍼: `GetOrCreateOverride(string id)`, `EffectiveFor(string id)`
- [ ] **A-3. `AlgorithmCameraMapping.Clone()` 갱신** — Inspections 깊은 복사 (`:64`)
- [ ] **A-4. `AlgorithmCameraSubset.Clone()` 확인** — Items 깊은 복사가 A-3 통해 override 까지 복제되는지 (`:145`)
- [ ] **A-5. 라벨 사전 `InspectionLabel.Get(algorithm, inspectionId)` 추가** ⚠ (#1) — 26 매핑
- [ ] **A-6. 구버전 JSON 라운드트립** — override 없는 JSON → Load → Save → `Inspections` 키 미출현 유지

## B. UI — SettingsPage

- [ ] **B-1. TreeView 3레벨화** — `LoadAlgorithms` (`:86`) 에서 각 `cam:<alg>` 노드 아래 26 검사 자식 추가
- [ ] **B-2. 노드 키 규약** — 알고리즘 `cam:<algorithm>` (기존), 검사 `cam:<algorithm>:<inspectionId>` (신규)
- [ ] **B-3. `Tree_AfterSelect` 라우팅** (`:116`) — 검사 키(콜론 2개) → `InspectionOverridePanel`, 알고리즘 키 → `CameraMappingPanel`
- [ ] **B-4. 검사 노드 override 표시** ⚠ (#4) — override 있으면 `●`(색/문자) 부착, 노드 텍스트 갱신

## C. UI — InspectionOverridePanel (신규)

- [ ] **C-1. 신규 파일** `QMC.Vision\Ui\Pages\InspectionOverridePanel.cs` + csproj 등록
- [ ] **C-2. 헤더** — 알고리즘 + 검사 라벨
- [ ] **C-3. 카메라 ID 읽기 전용 표시** (알고리즘에서 상속)
- [ ] **C-4. 7필드 토글 UI** ⚠ (#3) — 각 필드 "기본값" 체크박스 + 입력칸. 체크 시 알고리즘 기본값 표시(비활성), 해제 시 override 입력(활성)
- [ ] **C-5. ROI override** ⚠ (#5) — 4필드 묶음 vs 개별 결정 반영
- [ ] **C-6. 버튼** — 저장 / 기본값 복원(FR-7) / 취소(FR-8) / 테스트 그랩
- [ ] **C-7. Validation** (FR-9) — ROI W·H 한쪽만 입력 거부, 범위 밖 값 status 빨강
- [ ] **C-8. `SelectInspection(algorithm, inspectionId)` 진입점** — SettingsPage 가 호출

## D. 영속화

- [ ] **D-1. `AlgorithmCameraMapStore.Save` 빈 override 정리** — `IsEmpty()` override 는 `Inspections` 에서 제거
- [ ] **D-2. 동일 InspectionId 중복 항목 제거** — 정책 결정 후 명시 (첫 항목 우선 권장, Migrate 패턴과 동일)

## E. 검증

- [ ] **E-1. `dotnet build`** warning 0 / error 0
- [ ] **E-2. verify_all 회귀 없음** (현 60 PASS 유지)
- [ ] **E-3. `tools\verify_stage64.ps1` 신규** — override JSON 라운드트립 + 26 검사 노드 정합성 (5×알고리즘 자식 수 = 7/4/7/4/4)
- [ ] **E-4. Sim auto-cycle (8 die) 정상** — 런타임 적용 없음 = 동작 변화 없어야 함

## F. 문서

- [ ] **F-1. `STAGE65_RESULT_*.md`** (구현 Stage 에서 작성)
- [ ] **F-2. `MISMATCH_RESOLUTION_LOG.md` 신규 행** — 편집기 5종 vs 본 Stage 카메라-override 레이어 구분
- [ ] **F-3. `ARCHITECTURE_EXPORT.md` 재생성** (모델·UI 변경 → 권장)

## G. 비범위 (Out of Scope)
- 런타임 파라미터 적용 (Grab 직전 `EffectiveFor`) — Stage 65
- 동일 CameraId 모듈 간 카메라 공유/lock — 별도 Stage
- 알고리즘 파라미터(Threshold 등) 검사별 분리 — 별 작업

## H. 진행 순서
1. 사용자 컨펌 (SPEC §13 의 1~6)
2. git 브랜치 + SPEC/CHECKLIST 커밋 (← 본 단계 완료 지점)
3. 구현 Stage(65) 진입: A → B → C → D → E → F

---

## 예상 작업 시간
구현 Stage 65 기준 **5 ~ 8 시간** (모델 1.5h / UI TreeView+Panel 3h / 영속화+검증 2h / 문서 1h)
