# STAGE 62 — CHECKLIST: Vision 알고리즘별 카메라 파라미터 설정

- **연계 SPEC**: `D:\Work\CDT-320\docs\STAGE62_SPEC_VisionAlgorithmCameraConfig.md`
- **작성일**: 2026-05-27
- **상태**: SPEC + CHECKLIST 작성 단계 (구현 미진입 — 사용자 컨펌 대기)
- **사전 조건**: SPEC §11 의 확인 필요 항목 1~7 해소

> 본 CHECKLIST 의 각 항목은 SPEC 의 결정 사항에 따라 일부가 추가/삭제될 수 있다.
> 항목 옆 ⚠ 표시는 SPEC 의 확인 필요 항목 (옵션) 에 의존한다.

---

## A. 데이터 모델

- [ ] **A-1. `AlgorithmCameraMapping` ROI 필드 4개 추가** ⚠ (확인 필요 #1)
  - 대상: `QMC.Vision\Config\AlgorithmCameraMap.cs:42-66`
  - 필드: `RoiOffsetX / RoiOffsetY / RoiWidth / RoiHeight` (int, `[DataMember]`)
  - 헬퍼: `IsRoiFull`, `ToRectangle()`
  - `Clone()` 갱신 (`:56-65`)
- [ ] **A-2. `EnsureDefaults` 에 ROI 초기값 (0/0/0/0) 명시**
  - 대상: `QMC.Vision\Config\AlgorithmCameraMap.cs:125-152`
  - 누락 항목 생성 시 ROI 4 필드 = 0
- [ ] **A-3. 구버전 JSON 호환 가드 확인**
  - `algorithm_camera.json` 의 구버전(ROI 필드 없음) 파일을 새 코드로 로드 시 ROI = 0 자동 보충 (DataContract 기본동작)
  - 단위 시험: 가짜 구버전 JSON 파일 작성 → Load → ROI 4 필드 모두 0 인지 확인 (수동 또는 verify_all 스크립트)

## B. Camera 적용 로직

- [ ] **B-1. `AlgorithmCameraBinder.ApplyParameters` 에 ROI 반영** ⚠ (확인 필요 #1)
  - 대상: `QMC.Vision\Config\AlgorithmCameraBinder.cs:18-27`
  - 추가: `if (!m.IsRoiFull) cam.Roi = m.ToRectangle();`
  - 예외 처리: try/catch (기존 패턴과 동일)
- [ ] **B-2. `ApplyParameters` 실패 시 호출자에 boolean / out string err 반환**
  - 현행 시그니처: `public static void ApplyParameters(ICamera, AlgorithmCameraMapping)`
  - 변경: `public static bool ApplyParameters(ICamera, AlgorithmCameraMapping, out string error)` (또는 별도 `TryApplyParameters`)
  - 호출자 (`Form1.cs`, `CameraMappingPanel`) 수정

## C. UI (CameraMappingPanel)

- [ ] **C-1. ROI 입력 컨트롤 4개 추가** ⚠ (확인 필요 #1)
  - 대상: `QMC.Vision\Ui\Pages\CameraMappingPanel.cs`
  - NumericUpDown × 4 (Width 0 ~ 5000, Height 0 ~ 5000, OffsetX/Y 0 ~ 5000)
  - "0 = full sensor" 안내 라벨 1개
- [ ] **C-2. 기본값 복원 (Reset) 버튼**
  - 대상: 동일 파일
  - 클릭 시 현재 알고리즘의 매핑을 `EnsureDefaults` 로직으로 reset (단일 항목)
  - 확인 대화 (`MessageBox.Yes/No`)
- [ ] **C-3. 취소 (Cancel) 버튼**
  - 디스크 로드 후 `BindFields` 재호출 → 미저장 변경 폐기
- [ ] **C-4. Validation 강화**
  - `CameraId` 빈 문자열 거부 → `_lblStatus` 에 빨강 메시지
  - `ExposureUs` 가 0 이하 거부 (UI Min 으로 보조됨이지만 명시 검사)
  - ROI 의 Offset+Size 가 카메라 Resolution 초과 시 경고 (Resolution 은 Camera.Open 후에만 확정됨 → "확인 가능 시점만" 검사)
- [ ] **C-5. SettingsPage 진입 시 매핑 항상 reload**
  - 대상: `QMC.Vision\Ui\Pages\SettingsPage.cs:85-103` `LoadAlgorithms`
  - 페이지가 다시 표시될 때 `AlgorithmCameraMapStore.Load()` 호출 (현행도 호출되지만 명시 검증)

## D. Form1 결선 (Wiring)

- [ ] **D-1. 매핑 누락 알고리즘 검사**
  - 대상: `QMC.Vision\Form1.cs:48-58` (또는 그 직전)
  - `map.Get(alg) == null` 또는 `string.IsNullOrEmpty(mapping.CameraId)` → `VISION-MAPMISS` 로깅/알람 (옵션 A/B/C에 따라 — 확인 필요 #2)
- [ ] **D-2. `CreateCameraForAlgorithm` 의 실패 경로 분기**
  - 대상: `QMC.Vision\Form1.cs:97-107`
  - `cam.Open()` 시 예외 → `VISION-CAMOPEN`
  - `ApplyParameters` 실패 → `VISION-PARAMFAIL`
- [ ] **D-3. `RebindAlgorithmCamera` 의 실패 경로**
  - 대상: `QMC.Vision\Form1.cs:116-136`
  - 카메라 교체 실패 시 old camera 복원 (롤백)
  - status label 에 메시지 전달 (`CameraMappingPanel._lblStatus`)

## E. 알람 (선택 — 확인 필요 #2 의 옵션 B/C 채택 시)

- [ ] **E-1. AlarmMaster 신규 알람 3개 등록**
  - 대상: `QMC.CDT-320\Equipment\Alarms\AlarmMaster.cs:160` (Vision 블록 끝)
  - `VISION-MAPMISS`, `VISION-PARAMFAIL`, `VISION-CAMOPEN`
  - 다국어: TitleEn / CauseEn / ActionEn 채움
- [ ] **E-2. Vision → Handler 알람 채널 (옵션 C)**
  - 대상: `QMC.Vision\Comm\` 신규 파일 `AlarmReporter.cs` (TCP 클라이언트)
  - Handler 측 `VisionHub` 또는 별도 포트 수신
- [ ] **E-3. `IAlarmReporter` 추상화 (옵션 B)**
  - 대상: 새 공용 어셈블리 (QMC.Common 또는 `QMC.Vision\Core\IAlarmReporter.cs`)
  - 구현: `FileAlarmReporter` (Vision side), `AlarmManagerAdapter` (Handler side)
- [ ] **E-4. Raise 호출 지점**
  - `Form1.cs` D-1/D-2 지점 + `CameraMappingPanel` 의 Apply/TestGrab 실패 지점

## F. Sim 모드 검증

- [ ] **F-1. SimCamera 파라미터 setter no-op 확인**
  - 대상: `QMC.Vision\Cameras\Sim\SimCamera.cs` (조사 필요)
  - `OnExposureChanged / OnGainChanged / OnTriggerModeChanged / OnRoiChanged` 가 SDK 호출 없이 캐시만 갱신하는지 코드 확인
  - 미달 시 빈 override 추가
- [ ] **F-2. Sim auto-cycle (8 die) 정상 종료 확인**
  - Handler 의 `--auto-cycle 8` CLI 옵션 (메모리 reference_paths.md)
  - Vision 매핑이 모두 Sim/* 일 때 사이클 완주
- [ ] **F-3. AlgorithmCameraBinder + SimCamera 통합 — 예외 없이 통과**

## G. Recipe Layer 통합 (선택 — 확인 필요 #3)

- [ ] **G-1. `RecipeProject` 에 `AlgorithmCameraSubset` 필드 추가** ⚠ (확인 필요 #3, Project 별 분리 채택 시)
  - 대상: `QMC.CDT-320\Equipment\Recipes\RecipeStore.cs:210-253`
  - 본 Stage 에서는 **건너뛰는 것을 추천** — 별도 Stage 로 분리
- [ ] **G-2. Vision 측 매핑 로드 우선순위 결정**
  - Project (Recipe) > 전역 (`algorithm_camera.json`) > Defaults
  - G-1 가 보류되면 본 항목도 보류

## H. 검증 / verify_all

- [ ] **H-1. `tools\verify_all.ps1` 에 신규 검사 항목 추가**
  - "AlgorithmCameraMapping 모든 5 알고리즘 항목 존재" — `algorithm_camera.json` 의 Items.Count == 5
  - "ROI 4 필드 존재" (A-1 채택 시) — JSON 스키마 검사
- [ ] **H-2. `tools\verify_stage62.ps1` 또는 `.pl` 신규 작성**
  - 시나리오: 매핑 파일 삭제 → Vision 기동 → 자동 생성된 JSON 의 5 항목 확인
- [ ] **H-3. `dotnet build` warning 0 / error 0**
  - 대상 프로젝트: `QMC.CDT-320\QMC.Vision\QMC.Vision.csproj`
  - 추가로 솔루션 전체 (`QMC.CDT-320.sln`) — Handler 의 AlarmMaster 변경 검증
- [ ] **H-4. verify_all 전체 PASS 유지 (회귀 없음)**
  - 기존 통과 케이스가 모두 유지되는지 확인

## I. 문서

- [ ] **I-1. 시퀀스 문서 04~08 영향 재검토** — 본 Stage 는 직접 영향 없음을 SPEC §8.3 에 명시. 변경 없음을 확정.
- [ ] **I-2. `MISMATCH_RESOLUTION_LOG.md` 신규 행 추가** — SPEC §9 의 추가 안 적용
- [ ] **I-3. `ARCHITECTURE_EXPORT.md` 재생성 필요 여부**
  - SettingsPage 가 추가됐고 AlgorithmCameraMap 이 신규 → 재생성 권장
  - 도구: 기존 export 스크립트 사용 (위치 확인 필요)
- [ ] **I-4. `STAGE62_RESULT_VisionAlgorithmCameraConfig.md` 완료 보고서 작성**
  - 변경 통계 (라인 +/-, 신규/수정 파일)
  - verify_all 결과 캡처
  - 잔존 issue / 다음 Stage 후보

## J. Git

- [ ] **J-1. git 저장소 초기화** ⚠ (확인 필요 #7)
  - 현재 `.git` 부재 → `git init` 필요 여부 사용자 결정
- [ ] **J-2. 브랜치 `stage-62-vision-algo-camera-config` 생성** (J-1 후)
- [ ] **J-3. SPEC + CHECKLIST 초기 커밋** (J-1 후)
- [ ] **J-4. 단계별 커밋 (A, B, C, D, E, F, H 각 1 커밋 권장)**

---

## 진행 순서 권장

1. SPEC §11 확인 필요 항목 1~7 사용자 컨펌
2. J-1, J-2, J-3 (git)
3. A → B (모델 + 바인더)
4. C (UI)
5. D (Form1 wiring)
6. E (선택) — 옵션에 따라
7. F (Sim 검증)
8. H (verify_all)
9. I (문서)

예상 작업 시간: **6 ~ 9 시간** (옵션 E 의 옵션 C 채택 시 +2 시간)
