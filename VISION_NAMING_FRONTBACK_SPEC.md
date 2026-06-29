# QMC.Vision 측면검사 명칭 통일 명세 (FrontSide / BackSide)

작성: 2026-06-24 · 범위: **QMC.Vision 프로젝트 한정** (핸들러 QMC.CDT-320, QMC.Common 미수정)

---

## 1. 목표 / 범위 / 확정 사항

- 측면검사(Side) 관련 **C# 식별자**를 `FrontSide` / `BackSide` 한 체계로 통일한다.
- `BottomInspection` 모듈(다이 하부 검사)은 **그대로 유지**한다(요청 확정).
- **QMC.Vision만 수정.** 핸들러·Common·설정파일/레시피와의 **와이어·영속 계약 문자열은 보존**한다.
- 편집 대상 저장소: `C:\Project\CDT-320` (확정).

### 통일 규칙
| 현재(혼재) | 목표 |
|---|---|
| `FrontSide…` / `FrontSide…` (앞쪽) | **`FrontSide…`** |
| `RearSide…` / `RearSide…` (뒤쪽) | **`BackSide…`** |

---

## 2. 핵심 발견 — 왜 "보존"이 필요한가

같은 물리 측면검사 2개 모듈이 계층마다 다르게 불린다.

| 계층 | 앞쪽 | 뒤쪽 | 성격 |
|---|---|---|---|
| Vision 클래스/enum/폴더/타입 | `FrontSideVision*` | `RearSideVision*` | **C# 식별자 → 변경 대상** |
| TCP 모듈명(라우팅 토큰) | `"FrontSideVision"` | `"RearSideVision"` | 핸들러 SSOT → **보존** |
| AlgorithmKey(레시피 저장) | `FrontSide` | `RearSide` | Common SSOT → **보존** |
| Config JSON 키 | `FrontSideVisionPort`, `FrontSideViewerPort` | `RearSideVisionPort`, `RearSideViewerPort` | 설정파일 영속 → **직렬화명 보존** |
| 상태 표시 라벨(AddView) | `"FrontSide"` | `"RearSide"` | 단순 라벨 → 변경 가능 |

**근거**
- `Equipment/Comm/VisionCommandRouter.cs:35` — 수신 라인의 `MODULE` 토큰이 모듈 `Name`과 **정확히 일치해야** 명령 처리. 핸들러는 `VisionHub.cs`에서 `"FrontSideVision"`/`"RearSideVision"`을 송신 → 이 문자열을 바꾸면 통신 단절(`unknown module`).
- `RearSideVisionModule.cs:9~10` 주석: AlgorithmKey는 **데이터 호환 위해 RearSide 유지** (이미 와이어명과 분리 운용 중).
- `VisionConfig.cs` 포트 프로퍼티는 `DataContractJsonSerializer`로 설정파일에 저장 → C# 프로퍼티명을 바꾸면 JSON 키가 바뀌어 기존 설정값 유실. **`[DataMember(Name="…")]`으로 기존 키 고정** 필요.
- `QMC.Vision.csproj`는 구식(명시적 `<Compile Include>`) → 파일/폴더 rename 시 csproj 동시 수정 필수.

---

## 3. 변경 대상 — C# 식별자 (파일별)

> 문자열 리터럴(`"FrontSideVision"` 등)은 5장 "보존 목록"에 해당하면 **그대로 둔다.** 아래는 타입/멤버/필드/지역변수/enum 멤버 등 **코드 식별자**만 대상.

### 3.1 모듈/머신
| 파일 | 현재 → 목표 |
|---|---|
| `Equipment/Unit/FrontSideVisionModule.cs` → **파일명** `FrontSideVisionModule.cs` | 클래스 `FrontSideVisionModule`→`FrontSideVisionModule`; 타입 `FrontSideVisionSetup/Config/Recipe`→`FrontSideVision*`. `base("FrontSideVision")` **보존**, `AlgorithmKey => …FrontSide` **유지** |
| `Equipment/Unit/RearSideVisionModule.cs` → **파일명** `BackSideVisionModule.cs` | 클래스 `RearSideVisionModule`→`BackSideVisionModule`; 타입 `RearSideVision*`→`BackSideVision*`. `base("RearSideVision")` **보존**, `AlgorithmKey => …RearSide` **보존**(Common 미수정) |
| `Equipment/Unit/VisionMachine.cs` | 프로퍼티/필드 `FrontSideVision`→`FrontSideVision`, `RearSideVision`→`BackSideVision`, 타입참조 동기화. (line 77~78의 문자열이 모듈 Name이면 **보존**) |
| `Equipment/Unit/VisionModuleData.cs` | 멤버/타입 `FrontSideVision*`→`FrontSideVision*`, `RearSideVision*`→`BackSideVision*` |

### 3.2 시퀀싱
| 파일 | 현재 → 목표 |
|---|---|
| `Sequencing/FrontSideVision/` (폴더) | → `Sequencing/FrontSideVision/` |
| `…/FrontSideVisionSequence.cs` | → `FrontSideVisionSequence.cs`; 클래스 `FrontSideVisionSequence`→`FrontSideVisionSequence`. line 11 `"FrontSideVision"`(모듈키)는 **보존** |
| `Sequencing/RearSideVision/` (폴더) | → `Sequencing/BackSideVision/` |
| `…/RearSideVisionSequence.cs` | → `BackSideVisionSequence.cs`; 클래스 rename. line 11 `"RearSideVision"` **보존**. 단계 tool ID `"BottomSurfaceInspector"`/`"BottomChippingInspector"`는 레시피 저장값 → **보존** |
| `Sequencing/Common/SequenceModuleKind.cs` | enum 멤버 `FrontSideVision`→`FrontSideVision`, `RearSideVision`→`BackSideVision`. 합성값 `SideVision = FrontSideVision | BackSideVision` (이름 `SideVision` 유지), `All` 갱신 |
| `Sequencing/Common/AutoSequenceCoordinator.cs` | enum 참조 동기화. `BuildTools(…, "FrontSideVision")`/`"RearSideVision"` **문자열 보존**(모듈키), 머신 프로퍼티 참조명만 갱신 |
| `Sequencing/Common/SequenceToolCatalog.cs` | line 51/58 `"FrontSideVision"`/`"RearSideVision"` 키 = 모듈키 → **보존** |

### 3.3 Form1 / UI
| 파일 | 현재 → 목표 |
|---|---|
| `Form1.cs` | 필드 `FrontSideVisionMod`→`FrontSideVisionMod`, `RearSideVisionMod`→`BackSideVisionMod`, `_svrFrontSideVision`→`_svrFrontSideVision`, `_svrRearSideVision`→`_svrBackSideVision`, `_viewRearSide`→`_viewBackSide`(`_viewFrontSide` 유지). 타입참조 동기화. **보존**: `"FrontSideVision"/"RearSideVision"`(서버 모듈Name), `"FrontSide"/"RearSide"`(AlgorithmKey switch, line 748~749), `"Sim/FrontSide"/"Sim/RearSide"`(Sim CameraId). `AddView("RearSide"…)`→`AddView("BackSide"…)` 라벨 변경 가능(선택) |
| `Ui/Pages/Work/SequencerPage.cs` | `SequenceModuleKind.FrontSideVision`→`FrontSideVision` 등 enum 참조 갱신. 표시문구 `"앞측면"/"뒤측면"`은 유지(또는 정책에 맞게) |
| `Ui/Pages/Work/OperationPage.cs` / `.Designer.cs` | 컨트롤/주석의 `FrontSide`/`RearSide` 식별자 갱신(Designer 컨트롤명 prefix 규칙 유지) |
| `Ui/Pages/Settings/Recipe/RecipePage.cs` | `FrontSideVisionMod`/`RearSideVisionMod` 참조 갱신 |
| `Ui/Pages/Settings/CommLinkPage.cs` | 프로퍼티 참조 `FrontSideVisionPort`/`RearSideVisionPort`/`FrontSideViewerPort`/`RearSideViewerPort` — 5장 Config 정책에 맞춰 갱신 |
| `Ui/Pages/Settings/CameraMappingPanel.cs` | line 561 `"Sim/FrontSide"/"Sim/RearSide"` = Sim CameraId → **보존** |
| `Ui/Localization/Lang.cs` | 키 `algo.FrontSide`/`algo.RearSide`는 AlgorithmKey 기반 → **키 보존**, 표시문구(한/영/중/일)만 정책에 맞게 |

### 3.4 Config (특수 — 직렬화명 고정)
`Equipment/Config/VisionConfig.cs`:
- C# 프로퍼티 rename을 원하면 **반드시 직렬화 키 고정**:
  - `FrontSideVisionPort` → 프로퍼티 `FrontSideVisionPort` + `[DataMember(Name="FrontSideVisionPort")]`
  - `RearSideVisionPort` → `BackSideVisionPort` + `[DataMember(Name="RearSideVisionPort")]`
  - `FrontSideViewerPort` → 유지(이미 Front)
  - `RearSideViewerPort` → `BackSideViewerPort` + `[DataMember(Name="RearSideViewerPort")]`
- **권고:** 혼란 최소화를 위해 **Config 프로퍼티명은 이번에 건드리지 않는 것**도 합리적(설정파일 호환 최우선). 결정 필요 → 4장.
- `Legacy*InspectionPort` 마이그레이션 shim은 그대로 둔다.

---

## 4. 결정 필요 항목 (다음 스텝 착수 전)

1. **Config 프로퍼티명**: (a) `[DataMember(Name=…)]` 고정하며 rename, (b) 설정 호환 위해 현행 유지 — 권고는 (b).
2. **AddView 상태 라벨 / Localization 표시문구**: `RearSide`/"뒤측면" → `BackSide`/"뒤측면" 표기 통일 여부.
3. **Sequence 단계 tool ID**(`BottomSurfaceInspector` 등): 레시피 저장값이라 기본 **보존** 권고. 통일 원하면 별도 마이그레이션 필요(현재 tool-ID 마이그레이션 로직 없음).

---

## 5. 보존 목록 (절대 변경 금지 — 변경 시 런타임/데이터 파손)

| 항목 | 값 | 이유 |
|---|---|---|
| 모듈 Name (`base(...)`, 라우터 토큰, 코디네이터/카탈로그 키) | `"FrontSideVision"`, `"RearSideVision"` | 핸들러 TCP 계약 (handler 미수정) |
| AlgorithmKey / `VisionAlgorithm.*` | `FrontSide`, `RearSide` | Common SSOT·레시피 저장값 |
| Sim CameraId | `"Sim/FrontSide"`, `"Sim/RearSide"` | Common 마이그레이션·Sim 백엔드 키 |
| Config 직렬화 키 | `FrontSideVisionPort`, `RearSideVisionPort`, `RearSideViewerPort` | 설정파일 호환 |
| Localization 키 | `algo.FrontSide`, `algo.RearSide` | AlgorithmKey 매핑 |
| 시퀀스 tool ID | `BottomSurfaceInspector`, `BottomChippingInspector`, `TopSurfaceInspector`, `TopChippingInspector` | 레시피 저장값 (마이그레이션 부재) |

---

## 6. 잔존 불일치 & 후속(별도 스텝) 권고

QMC.Vision만 수정하면 **C#은 Front/Back로 통일되지만 와이어/저장값은 레거시(Top/Bottom/Rear)로 남는다.** 완전 통일하려면 후속 작업 필요:

- **핸들러(QMC.CDT-320)**: TCP 모듈명 `VisionHub.cs`/`VisionViewerPorts.cs`의 `"FrontSideVision"/"RearSideVision"`, 그리고 `RearSideVisionY*`·`ReticleRearSide*` 축/IO/인터락(~120개) → Back 전파. (축 바인딩은 index 기준이라 명칭 일괄 변경은 안전하나 광범위.)
- **Common(QMC.Common)**: `VisionAlgorithm.RearSide`→`BackSide` + `MigrateLegacyAlgorithmNames`에 `RearSide→BackSide` 추가(기존 `FrontSide→FrontSide`, `RearSide→RearSide` 패턴 미러).

이 후속 작업은 핸들러·레시피 동시 변경이라 본 스펙과 분리한다.

---

## 7. 변경 영향 파일 (18개 .cs + csproj)

```
Equipment/Config/VisionConfig.cs            (Config 정책에 따름)
Equipment/Unit/FrontSideVisionModule.cs       → FrontSideVisionModule.cs
Equipment/Unit/RearSideVisionModule.cs    → BackSideVisionModule.cs
Equipment/Unit/VisionMachine.cs
Equipment/Unit/VisionModuleData.cs
Form1.cs
Sequencing/FrontSideVision/FrontSideVisionSequence.cs       → FrontSideVision/FrontSideVisionSequence.cs
Sequencing/RearSideVision/RearSideVisionSequence.cs → BackSideVision/BackSideVisionSequence.cs
Sequencing/Common/SequenceModuleKind.cs
Sequencing/Common/AutoSequenceCoordinator.cs
Sequencing/Common/SequenceToolCatalog.cs
Ui/Localization/Lang.cs
Ui/Pages/Settings/CameraMappingPanel.cs
Ui/Pages/Settings/CommLinkPage.cs
Ui/Pages/Settings/Recipe/RecipePage.cs
Ui/Pages/Work/OperationPage.cs
Ui/Pages/Work/OperationPage.Designer.cs
Ui/Pages/Work/SequencerPage.cs
QMC.Vision.csproj                           (Compile Include 4개 경로 갱신)
```

---

## 8. 검증 절차 (Windows)

1. 빌드: `MSBuild QMC.Vision\QMC.Vision.csproj /t:Build /p:Configuration=Debug` — 0 error.
2. 잔여 C# 레거시 식별자 점검: `FrontSideVisionModule|RearSideVisionModule|SequenceModuleKind.FrontSideVision|SequenceModuleKind.RearSideVision` 검색 → 0건.
3. 보존 문자열 유지 확인: `"FrontSideVision"`/`"RearSideVision"`(모듈Name), `FrontSide`/`RearSide`(AlgorithmKey) 잔존 확인.
4. 런타임: 핸들러↔Vision 연결(5105/5106) → 측면 MATCH/INSPECT 명령 정상 ACK. 기존 설정파일 로드 시 포트값 유지.
5. `perl tools/verify_vision_features.pl` 회귀.
