# STAGE 62 — RESULT: Vision 알고리즘별 카메라 파라미터 설정

- **작업 완료일**: 2026-05-27
- **연계 문서**:
  - SPEC: `D:\Work\CDT-320\docs\STAGE62_SPEC_VisionAlgorithmCameraConfig.md`
  - CHECKLIST: `D:\Work\CDT-320\docs\STAGE62_CHECKLIST_VisionAlgorithmCameraConfig.md`
  - MISMATCH 추가: `D:\Work\CDT-320\docs\MISMATCH_RESOLUTION_LOG.md` (Stage 62 섹션)
- **git 커밋**: 6개
  - `bbd0bf9` SPEC/CHECKLIST/.gitignore (initial)
  - `8f9f9b6` baseline (QMC.CDT-320 솔루션 스냅샷)
  - 알람 이동 — Common.Alarms (E)
  - A/B/C/D — ROI + Binder + UI + Wiring
  - G — Recipe Layer integration
  - H — verify_all + smoke test

---

## 1. 사용자 결정 사항 적용 결과

| # | 사용자 답변 | 적용 결과 |
|---|---|---|
| 1 | ROI 추가 — 진행 | ✅ `RoiOffsetX/Y/Width/Height` 4 필드 + UI 4 NumericUpDown |
| 2 | 옵션 B (핸들러 알람과 동일 클래스) | ✅ AlarmManager/Master/Record/Severity 를 QMC.Common.Alarms 로 이동. Vision 도 동일 타입 직접 호출 |
| 3 | Recipe Layer Project별 분리 | ✅ AlgorithmCameraSubset 을 QMC.Common.Recipes 에 두고 RecipeProject.VisionCameras 필드 추가 |
| 4 | Sim 모드 no-op 검증 — 진행 | ✅ CameraBase 의 Hook 메서드(`OnXxxChanged`) 가 빈 virtual; SimCamera 미오버라이드 → 자동 no-op |
| 5 | TCP SetCameraParam — 가능한만큼 + 리포트 | ✅ 매뉴얼 + 코드 조사. 동적 명령 부재 → Stage 외 처리 (보고서 §4) |
| 6 | MISMATCH 둘다 확인 | ✅ `MISMATCH_LOG.md` 부재 / `MISMATCH_RESOLUTION_LOG.md` 1개만 존재 → 후자에 추가 |
| 7 | git 커밋 후 작업 | ✅ git init + 작업 단계마다 커밋 (총 6 커밋) |
| 8 | 이후 작업 시작 전 매번 커밋 | ✅ 본 Stage 부터 정책 적용. 각 단계 (E/ABCD/G/H/I) 커밋 |

---

## 2. 산출물

### 2.1 신규 파일

| 경로 | 역할 |
|---|---|
| `QMC.CDT-320/QMC.Common/Alarms/AlarmManager.cs` | Common 으로 이동 (Vision/Handler 공유) |
| `QMC.CDT-320/QMC.Common/Alarms/AlarmMaster.cs` | Common 이동 + Stage 62 알람 3개 추가 |
| `QMC.CDT-320/QMC.Common/Alarms/AlarmRecord.cs` | Common 이동 |
| `QMC.CDT-320/QMC.Common/Alarms/AlarmSeverity.cs` | Common 이동 |
| `QMC.CDT-320/QMC.Common/Recipes/AlgorithmCameraSubset.cs` | VisionAlgorithm 상수 + Mapping + Subset 모델 |
| `QMC.CDT-320/tools/verify_stage62.ps1` | Stage 62 검증 스크립트 (12 PASS) |
| `docs/STAGE62_SPEC_VisionAlgorithmCameraConfig.md` | SPEC |
| `docs/STAGE62_CHECKLIST_VisionAlgorithmCameraConfig.md` | CHECKLIST |
| `docs/STAGE62_RESULT_VisionAlgorithmCameraConfig.md` | 본 문서 |
| `.gitignore` | 빌드 산출물 / IDE 폴더 제외 |

### 2.2 삭제/이동 파일

| 경로 | 처리 |
|---|---|
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmManager.cs` | 삭제 (Common 으로 이동) |
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs` | 삭제 (Common 으로 이동) |
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmRecord.cs` | 삭제 |
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmSeverity.cs` | 삭제 |
| `QMC.CDT-320/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs.bak` | 삭제 (.bak 잔존물) |

### 2.3 주요 수정 파일

| 경로 | 변경 요약 |
|---|---|
| `QMC.Common/QMC.Common.csproj` | Alarms 4 + Recipes 1 Compile 등록 + System.Runtime.Serialization + System.Drawing |
| `QMC.Vision/QMC.Vision.csproj` | ProjectReference QMC.Common 추가 |
| `QMC.CDT-320/QMC.CDT-320.csproj` | 구 Equipment\Alarms\\*.cs Compile 4개 제거 |
| `QMC.Vision/Config/AlgorithmCameraMap.cs` | 모델 제거 — Store 만 유지 (Common 의 AlgorithmCameraSubset 임포트) |
| `QMC.Vision/Config/AlgorithmCameraBinder.cs` | TryApplyParameters(out string error) + ROI 적용 + 오버로드 |
| `QMC.Vision/Form1.cs` | CreateCameraForAlgorithm 에서 VISION-MAPMISS/CAMOPEN/PARAMFAIL Raise. RebindAlgorithmCamera bool + out error + 롤백 |
| `QMC.Vision/Ui/Pages/CameraMappingPanel.cs` | ROI 4 NumericUpDown + Reset/Cancel 버튼 + Validate() |
| `QMC.CDT-320/Form1.cs` | AlarmManager.LanguageProvider = () => Lang.Current 설정 |
| `QMC.CDT-320/Equipment/Recipes/RecipeStore.cs` | RecipeProject.VisionCameras (AlgorithmCameraSubset) 필드 추가 |
| 12 파일 (Handler) | namespace `QMC.CDT320.Alarms` → `QMC.Common.Alarms` 일괄 치환 |
| 5 파일 (Handler) | `using Alarms = QMC.Common.Alarms;` 별칭 추가 (bare `Alarms.XXX` 사용처) |
| `tools/verify_all.ps1` | AlarmMaster 경로 갱신, 알람 코드 카운트 48→51, Stage 62 3 항목 추가 |

---

## 3. 신규 알람 코드

| Code | Category | Severity | 발생 조건 |
|---|---|---|---|
| `VISION-MAPMISS` | Vision | Warning | algorithm_camera.json 에 알고리즘 항목 누락 또는 CameraId 빈값 |
| `VISION-PARAMFAIL` | Vision | Warning | Exposure/Gain/Trigger/Pixel/ROI 적용 중 일부 실패 |
| `VISION-CAMOPEN` | Vision | Error | Camera.Open 단계 예외 |

호출 지점:
- `QMC.Vision\Form1.cs` `CreateCameraForAlgorithm` (기동 시)
- `QMC.Vision\Form1.cs` `RebindAlgorithmCamera` (Settings 페이지에서 적용 시)

---

## 4. TCP `SetCameraParam` 동적 명령 조사 결과 (사용자 답변 #5)

**조사 자료**:
- `D:\Work\CDT-320\manual_310_vision.txt` (40,533 bytes, CDT-310 Vision 매뉴얼 평문 추출본)
- `D:\Work\CDT-320\QMC.CDT-320\QMC.Vision\Comm\VisionTcpServer.cs` (실 TCP 프로토콜 구현)

**결과**:
- **매뉴얼**: Vision 측 카메라 IO 는 DIO 4 핀 (Reset / Expose / Ready / ExposeDone) 만 정의. Exposure 시간 (μs) / Gain / Trigger / ROI 등 카메라 파라미터를 **동적으로 변경하는 TCP/Serial 명령 부재**.
- **코드**: `VisionTcpServer.cs:23-35` 의 지원 명령 = `PING / EXPOSE / GRAB / MATCH / INSPECT / TRAIN / SCALE / ROT_CENTER / DISTORT / CAM_SWITCH / FOCUS_VAL`. 카메라 파라미터 변경 명령 없음.

**결론**: 본 Stage 범위 외. 사용자는 Settings 페이지의 "실행 모듈에 적용" 버튼으로 핫 변경 가능 (이미 구현 — `Form1.RebindAlgorithmCamera`).

향후 필요 시 별도 Stage 에서 `SET_PARAM <algorithm> <key>=<value>` 형태로 추가 가능 — `AlgorithmCameraBinder.TryApplyParameters` 가 이미 분리돼 있어 호출만 추가하면 됨.

---

## 5. 검증

### 5.1 빌드
```
> MSBuild QMC.CDT-320.sln /t:Build /p:Configuration=Debug
  QMC.Common -> QMC.Common.dll
  QMC.CDT-320 -> QMC.CDT-320.exe
  QMC.Vision -> QMC.Vision.exe
warning CS0169: 'HikGigECamera._handle' (기존, 무관)
error 0
```

### 5.2 verify_stage62.ps1
**TOTAL 12 / PASS 12 / FAIL 0**
- S62-E: AlarmManager moved + 3 new Vision codes + Handler csproj cleaned + LanguageProvider
- S62-G: AlgorithmCameraSubset + RecipeProject.VisionCameras + Vision references Common
- S62-A: ROI 4 fields + IsRoiFull/ToRectangle
- S62-B: TryApplyParameters + ROI in Binder
- S62-C: CameraMappingPanel ROI + Reset/Cancel/Validate
- S62-D: Form1 raises 3 alarm codes + using Common.Alarms

### 5.3 verify_all.ps1
**TOTAL 57 / PASS 57 / FAIL 0** (Stage 1~62 통합)

### 5.4 런타임 스모크 테스트
```
QMC.CDT-320 (4172) MainWindowTitle="CDT-320"           Responding=True
QMC.Vision  (24476) MainWindowTitle="CDT-320 VISION"   Responding=True
```

---

## 6. 잔존 작업 / 다음 Stage 후보

| 항목 | 우선순위 | Stage 후보 |
|---|---|---|
| Vision 이 Handler RecipeProject.VisionCameras 를 실제로 읽도록 — 디스크 동기 또는 TCP push | 높음 | Stage 63 |
| TCP 동적 카메라 파라미터 명령 (`SET_PARAM`) — 필요 시 | 중간 | Stage 64 (선택) |
| 다중 카메라 동기 트리거 / HW trigger sync | 낮음 | 별도 |
| Cognex 백엔드 ROI 의미 (PixelFormat=Bayer 등) 정합성 검증 | 낮음 | 백엔드별 인수 시험 |
| SettingsPage 의 카메라 매핑 변경을 **현재 활성 Recipe** 의 VisionCameras 에도 저장 (현재는 전역만) | 중간 | Stage 63 |
| 동적 ROI 입력 시 Resolution 초과 검사 (Camera.Open 후) | 낮음 | 후속 보강 |

---

## 7. 변경 통계

- 신규 파일: **10개** (Common Alarms 4 + Recipes 1 + verify 1 + docs 3 + .gitignore)
- 삭제 파일: **5개** (Handler Alarms 4 + .bak 1)
- 수정 파일: **~20개**
- 신규 알람 코드: **3개** (VISION-MAPMISS/PARAMFAIL/CAMOPEN)
- AlarmMaster 총 등록: **48 → 51**
- verify_all: **53 → 57 PASS**
- git 커밋: **6**

## 8. 빌드 요건

- Visual Studio 2022 Professional
- .NET Framework 4.7.2
- 솔루션: `D:\Work\CDT-320\QMC.CDT-320\QMC.CDT-320.sln`
- 빌드 명령: `MSBuild QMC.CDT-320.sln /t:Build /p:Configuration=Debug`

## 9. 사용자 운영 가이드

### 9.1 알고리즘별 카메라 설정 변경
1. Vision 프로그램 기동
2. 하단 `설정` 탭 클릭
3. 좌측 트리에서 `■ 카메라 매핑` → 5 알고리즘 중 선택
4. 우측 패널에서 CameraId / Exposure / Gain / FPS / Trigger / Pixel / Delay / ROI 편집
5. `저장` 버튼 (디스크 영속화) → `실행 적용` 버튼 (재시작 없이 핫 반영) → `테스트 그랩` (1장 그랩 미리보기)
6. `취소` (디스크 값 복원), `기본값 복원` (해당 알고리즘만 Sim/* 로 리셋) 가능

### 9.2 Project별 매핑 (향후)
- 현재 `Config\algorithm_camera.json` (전역) 만 작동
- `Recipes\<Project>.Project` 에 `VisionCameras` 필드는 저장됨 (Handler RecipeStore.Save 시)
- Vision 이 Project 별 매핑을 우선 로드하도록 하는 작업은 **Stage 63 예정**
