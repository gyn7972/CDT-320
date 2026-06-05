# CDT-320 MIL 카메라 포팅 — 구현 + 검증

> 모든 변경은 `QMC.CDT-320\QMC.Vision\` 하위. Handler(QMC.CDT-320) 0개 변경(git 확인).

## 변경 파일
- **Cameras/Mil/MilSystem.cs** (신규) — MIL App/System 1회 공유 할당 + 가용성 가드(MappControl 로 에러 팝업 억제). 실패 시 IsAvailable=false → Sim fallback.
- **Cameras/Mil/MilCamera.cs** (신규) — `CameraBase` 구현. Open(MdigAlloc, format=`MilDcfPath`||"M_DEFAULT") / Grab(MdigGrab→MbufExport(M_BMP)→Bitmap) / StartLive·StopLive(백그라운드 MdigGrab 루프) / TriggerSoftware / param(GenICam feature, best-effort) / Enumerate(Mil/0..N) / Close.
- **Core/CameraFactory.cs** — EnumerateAll에 MilCamera.Enumerate() 추가, CreateById/Create "Mil/" 분기.
- **Config/VisionConfig.cs** — `MilDcfPath`("") + `MilSystemDescriptor`("M_DEFAULT") + OnDeserializing 기본값.
- **Ui/Pages/CameraMappingPanel.cs** — "Mil/..." 카메라 선택 시 "MIL DCF 경로"/"MIL System" 입력칸 표시·저장(전역 VisionSettings)·로드.
- **QMC.Vision.csproj** — Matrox.MatroxImagingLibrary 참조(lib\ 벤더링, Private) + MilSystem/MilCamera Compile 등록 + **TargetFramework v4.7.2→v4.8** + **PlatformTarget AnyCPU→x64**.
- **lib/Matrox.MatroxImagingLibrary.dll** (벤더링).

## ⚠️ 필수 프로젝트 변경 (MIL 요구사항)
- MIL.NET DLL이 **.NET 4.8 대상** → QMC.Vision을 v4.8로 상향(같은 CLR, 인플레이스, 저위험).
- MIL.NET DLL이 **Amd64(x64) 전용** → QMC.Vision을 x64로 빌드(32bit 프로세스는 MIL native 로드 불가).
- 영향 범위 = **QMC.Vision.exe 한정**. QMC.Common(4.7.2)/Handler 무관. Hik(x64 native 보유)도 x64에서 동작.

## 하드웨어 확인 (MilProbe)
- 보드 = **Matrox Rapixo CXP (systemType=18, M_SYSTEM_RAPIXOCXP_TYPE)**, 디지타이저 **4채널**, MIL 라이선스 OK.
- 단 현재 4채널 모두 카메라 미검출(MdigAlloc empty) → 실 grab은 카메라 연결 후.

## 체크리스트 검증 결과
- A1~A5 (MilSystem): ✅
- B1~B8 (MilCamera): ✅ (B8 Enumerate → 아래 D4에서 실측 검증)
- C1~C5 (Factory/Config/UI/csproj): ✅
- **D1 빌드 0 error / 0 warning**: ✅
- **D2 Handler 무변경**: ✅ (git — QMC.CDT-320/QMC.CDT-320 0건)
- **D3 앱 기동 OK**: ✅ (GUI 'CDT-320 VISION' 모니터2 기동, port5200 listen, 좀비 없음, clean 종료 — MIL x64 참조가 로드 실패로 앱을 죽이지 않음)
- **D4 카메라 검색에 MIL 노출**: ✅ — 빌드된 어셈블리의 `CameraFactory.EnumerateAll()` 실측:
  ```
  CAM | Mil/0 | Matrox | CoaXPress | Matrox MIL (sysType=18)
  CAM | Mil/1..3 | Matrox | CoaXPress | ...
  CAM | Sim/0, Sim/1 | QMC | Sim
  SUMMARY | total=6 | milCount=4
  ```
- **D5 Sim fallback / graceful**: ✅(부분) — MIL 미가용이면 Enumerate=[] + 앱 정상. 단, BottomInspection을 Mil/0에 매핑했는데 카메라가 없으면 Open 실패 → (Sim 자동대체가 아니라) 미개장 MilCamera + VISION-CAMOPEN 알람(기존 Hik와 동일 패턴). 기본 매핑은 여전히 Sim이라 기동 시 MIL Open 시도 없음.
- **D6 실 grab**: ⏳ 사용자 — 카메라 연결 + DataFormat 확정 후 검증.

## 사용 방법 (사용자)
1. Vision 실행 → 설정 → 카메라 매핑에서 **BottomInspection** 선택.
2. **[카메라 검색]** → 목록에 `Mil/0..3` 표시됨 → 원하는 채널 선택.
3. (CXP면 보통 불필요) Camera Link 등 포맷 필요 시 **MIL DCF 경로**에 .dcf 입력, 필요시 **MIL System** 수정 → 저장.
4. **[테스트 그랩]** 또는 [실행 모듈에 적용] → 카메라 연결돼 있으면 영상 확인.

## 관찰/주의
- 현재 카메라 미연결이라 [테스트 그랩]은 카메라 붙기 전까지 실패(정상).
- 보드가 CXP(GenICam)라 기본 "M_DEFAULT"로 자동 인식 시도. 만약 실제로 Camera Link 카메라라면 .dcf 경로를 MIL DCF 필드에 지정.
- 라이브는 백그라운드 MdigGrab 루프(단순). 고속/저지연 필요 시 후속으로 MdigProcess+hook 전환 가능.

---

# Revision 2 — MIL Enumerate alloc-probe + Hik x64 런타임 수정

## (1) MIL `Enumerate()` → alloc-probe 방식
- 변경: 디지타이저 슬롯 수만큼 무조건 나열하던 것을 **각 슬롯에 MdigAlloc 시도 → 성공(카메라 실재)한 것만** 목록에 넣고 즉시 MdigFree.
- 효과: **멀티링크(듀얼/쿼드) 카메라 = 1개 항목**(MIL 이 링크 묶음), 빈/소비 채널은 제외, 카메라 없으면 0개. (유령 Mil/0..3 사라짐)
- 검증: 현재 카메라 미연결 → 검색 결과 MIL 0개(Sim 2개만). 의도대로 동작.
- 참고: 카메라 연결 전에 BottomInspection 을 MIL 에 매핑하려면 카메라 ID 콤보에 "Mil/0" 을 **직접 입력**(콤보는 편집 가능).

## (2) Hik 카메라가 검색에 안 뜨던 문제 — x64 부작용, 수정 완료
- **원인:** x64 전환 후 Hik `MV_CC_EnumDevices_NET` 가 **0x8000000C (MV_E_LOAD_LIBRARY)** 반환. 코어 `MvCameraControl.dll`(x64)은 로드되지만, transport 플러그인(MVGigEVisionSDK.dll/MvUsb3vTL.dll/producers)이 **출력 폴더에 없어** 로드 실패. Hik SDK 는 이 플러그인을 **exe 폴더 기준**으로 로드(PATH prepend 무효 — 모듈 상대 로드 확인됨).
- **수정:** `QMC.Vision.csproj` 에 post-build Target `CopyMvsRuntimeX64` 추가 — `...\MVS\Runtime\Win64_x64\*.dll` 을 출력 폴더로 복사(SkipUnchangedFiles, MVS 미설치 시 자동 skip).
- **검증:** 플러그인 4개 삭제 후 리빌드 → target 이 4/4 복원, EnumDevices **ret=0x0 (MV_OK)** (deviceNum=0 = 카메라 미연결이라 정상, MV_E_LOAD_LIBRARY 사라짐). Hik 카메라 연결 시 검색에 표시될 것.
- 빌드 0 error / 0 warning, Handler 무변경 유지.

## 현재 카메라 검색 동작 (카메라 미연결 상태)
- MIL: 0 (카메라 없음 — alloc-probe), Hik: 0 (카메라 없음, 단 SDK 정상), Sim: 2. → 검색하면 Sim 만 표시. 카메라 꽂으면 해당 항목 등장.

---

# Revision 3 — Hik "OtherDevice"(GenTL/프레임그래버) 카메라 x64 미검출 수정

## 증상
실제 Hik 카메라(MV-CH250-90GM, 169.254.129.26, FrontSide 매핑)가 연결돼 있고 MVS에선 **"OtherDevice"** 로 보이는데, x64 전환 후 우리 "카메라 검색"에 안 뜸. (32bit 시절엔 보였음.)

## 진단 (프로브)
- GigE/USB/CL enum = MV_OK / 0대. **GenTL 플래그(0x40~0x200) enum = 0x8000000C (MV_E_LOAD_LIBRARY).**
- 카메라는 Hik **프레임그래버 GenTL producer**(MvFGProducerGEV/CXP/CML/XoF.cti)로 발견됨. x64 producer 미로드가 원인.
- `GENICAM_GENTL64_PATH` 에 x64 경로/.cti 는 있으나, producer + 의존 DLL 이 우리 프로세스에서 co-locate 로드 안 됨.
- 검증: exe 폴더에 .cti 복사 + GENTL64 경로 맨 앞에 exe 폴더 추가 → GigE enum num=1, `HikGigECamera.Enumerate()` → 169.254.129.26 검출.

## 수정 (코드+빌드)
- `Cameras/Hik/HikMvsDll.cs` — `EnsureGenTLSearchPath()` 추가: TryLoad 시 exe 폴더를 `GENICAM_GENTL64_PATH`(32bit→GENTL32) 맨 앞에 prepend.
- `QMC.Vision.csproj` — post-build `CopyMvsRuntimeX64` 가 `*.dll` **+ `*.cti`** 복사하도록 확장.
- `HikGigECamera.Enumerate()` 플래그는 **변경 불필요**(producer 로드되면 표준 GigE 0x01 로 검출됨).

## 검증 결과
- 빌드 0/0, post-build이 .cti 6개 복원.
- 실제 코드 경로(HikMvsDll, env 수동조작 없음)로 `CameraFactory.EnumerateAll()`:
  ```
  CAM | 169.254.129.26 | HIKVISION | GigE | MV-CH250-90GM
  CAM | Sim/0, Sim/1
  ```
- 수정 빌드를 모니터 2에 재시작(pid 정상, 좀비 없음). Handler 무변경.

## 결론
"x64 전환으로 Hik 안 뜸"은 맞았음 — 원인은 **x64 GenTL producer(.cti) 미배포/미로드**. 위 수정으로 x64에서도 Hik 카메라가 검색에 정상 표시. MIL(x64 필수)과 Hik(이제 x64 OK)이 한 프로세스에서 공존.

---

# Revision 4 — 설정 UI 정리 + 해상도 변경 시 겹침/잘림 수정

## 요청
① MIL System 칸 제거 ② DCF 경로 기본 숨김 + 필요시 토글 ③ 카메라 ID 콤보 직접입력 차단(검색 전용) ④ 해상도 변경 시 겹침/잘림(테스트그랩 PictureBox 잘림, Maintenance 좌측 트리 상단이 헤더에 가림) 수정.

## 변경
- **Config/VisionConfig.cs** — `MilSystemDescriptor` 제거(시스템은 항상 M_SYSTEM_DEFAULT). `MilSystem.cs` 도 M_SYSTEM_DEFAULT 직접 사용.
- **Ui/Pages/CameraMappingPanel.cs**
  - MIL System 입력칸 삭제.
  - DCF: "Mil/.." 선택 시 "MIL DCF 직접 지정" 체크박스 노출, 체크해야 경로칸 표시(기본 숨김). 미체크 시 MilDcfPath="" → enumerate 가 M_DEFAULT.
  - 카메라 ID 콤보 `DropDownStyle=DropDownList` (직접 타이핑 차단, [카메라 검색] 결과에서만 선택).
  - `_body.AutoScroll=true` → 하단 프리뷰 PictureBox 가 패널보다 길어도 스크롤로 접근.
- **Ui/Pages/MaintenancePage.cs** — 헤더(Label)+툴바(Panel)+콘텐트(TableLayoutPanel) 를 **루트 3행 TableLayoutPanel**(30/40/100%)로 재구성. Dock=Top+Fill 형제 혼용 시 z-order 에 따라 Fill 이 Top 을 덮어 트리 상단(Wafer vision)이 가려지던 문제를 원천 차단. 해상도 무관 유동 배치.

## 검증 (헤드리스 렌더)
- 빌드 0/0, Handler 무변경, Hik enumerate 회귀 없음(169.254.129.26 그대로).
- MaintenancePage: TreeView absTop=73 (헤더 30+툴바 40 아래) → **트리 상단 안 가려짐, PASS**.
- CameraMappingPanel: combo=DropDownList, body.AutoScroll=True(프리뷰 y≈872 스크롤 도달), TextBox=1개(System 제거·DCF 숨김), SelectAlgorithm 무예외.
- 수정 빌드 모니터2 기동(좀비 없음).

## 메모
WinForms 에서 Dock=Top + Dock=Fill 형제 혼용은 z-order/타이밍에 따라 Fill 이 Top 을 덮는다. **헤더/툴바/콘텐트 스택은 루트 TableLayoutPanel(행 분할)로** 구성하는 것이 안전(겹침 원천 차단, 반응형).
