# CDT-320 MIL 카메라 포팅 — 설계 + 체크리스트

## 목표/범위
Matrox 보드(MIL)로 들어오는 카메라를 기존 `ICamera` 추상화에 추가. **이미지 처리(IVisionBackend) 아님.** Hik/Sim과 병행(대체 아님). BottomInspection에 매핑 예정. 검증은 기존 카메라 검색/매핑 UI(`CameraMappingPanel`).

## 하드웨어 확인 결과 (MilProbe)
- 보드 감지 OK: **Matrox Rapixo CXP (systemType=18)**, 디지타이저 **4채널**, MIL 라이선스 정상.
- 단, 4채널 모두 `MdigAlloc(M_DEFAULT)` NULL → 현재 grab 카메라 미검출(미연결/포맷 필요). 실 grab은 카메라 준비 후 검증.
- 보드가 CXP라 GenICam 기반(원래 답변 "Camera Link"와 상이) → DataFormat을 **config로** 두어 CXP("M_DEFAULT") / CL(.dcf 경로) 모두 수용.

## MIL.NET API (확정)
- `MappAlloc(M_DEFAULT, ref app)`, `MappControl(M_ERROR, M_PRINT_DISABLE)`(팝업 억제), `MsysAlloc(app, desc("M_DEFAULT"), M_DEFAULT, M_DEFAULT, ref sys)`
- `MsysInquire(sys, M_DIGITIZER_NUM/M_SYSTEM_TYPE, ref MIL_INT)`
- `MdigAlloc(sys, MIL_INT digNum, string dataFormat, M_DEFAULT, ref dig)`
- `MdigInquire(dig, M_SIZE_X/Y/BAND, ref MIL_INT)`, `MbufAlloc2d/AllocColor(sys, ...)`
- `MdigGrab(dig, buf)` → `MbufExport(tmp, M_BMP=64, buf)` → `new Bitmap(tmp)` (모든 포맷 안전)
- params: `MdigControlFeature(dig, M_FEATURE_VALUE, "ExposureTime"/"Gain"/..., M_TYPE_DOUBLE, ref double)` (CL/미지원이면 try/catch no-op)
- `MdigFree/MbufFree/MsysFree/MappFree`
- MIL_ID 암시적→long(null체크 `(long)id==0`), 암시적←int/long.

## 설계
- `Cameras/Mil/MilSystem.cs`: App/System 1회 공유 할당 + `IsAvailable` 가드(실패=Sim fallback). systemDescriptor=config.
- `Cameras/Mil/MilCamera.cs : CameraBase`: digNum(=Id "Mil/n") 디지타이저. Open=MdigAlloc(format=config.MilDcfPath||"M_DEFAULT")+버퍼. Grab=MdigGrab→MbufExport→Bitmap. Live=백그라운드 MdigGrab 루프→RaiseFrame. Trigger/params=best-effort. Enumerate=가용 시 Mil/0..N.
- `Core/CameraFactory.cs`: EnumerateAll에 MIL 추가, CreateById/Create에 "Mil/" 분기.
- `Config/VisionConfig.cs`: `MilDcfPath`(""), `MilSystemDescriptor`("M_DEFAULT") + OnDeserializing 기본값.
- `Ui/Pages/CameraMappingPanel.cs`: MIL 카메라 선택 시 "MIL DCF 경로"/"MIL System" 입력칸(전역 VisionSettings 저장).
- `QMC.Vision.csproj`: Matrox.MatroxImagingLibrary 참조(lib\ 벤더링, Private) + Compile 등록.

## 체크리스트
### A. MilSystem
- [ ] A1. App/System 1회 공유 할당(lock, _tried 가드)
- [ ] A2. MappControl(M_ERROR, M_PRINT_DISABLE)로 모달 팝업 억제
- [ ] A3. systemDescriptor=config(기본 "M_DEFAULT"), MsysAlloc 실패 시 IsAvailable=false + LastError
- [ ] A4. DigitizerCount/SystemType 조회, SysId 노출
- [ ] A5. 예외 전부 격리(throw 없음) → 가용성 플래그로만 표현

### B. MilCamera
- [ ] B1. CameraBase 상속, Id "Mil/n"→digNum 파싱
- [ ] B2. Open: !IsAvailable면 throw(→fallback). MdigAlloc(format=DcfPath||"M_DEFAULT"); 실패 시 throw
- [ ] B3. 해상도/밴드 조회 → Resolution 설정 + grab 버퍼 할당(MbufAlloc2d/Color)
- [ ] B4. Grab: MdigGrab→MbufExport(M_BMP)→Bitmap, 실패 시 GrabResult.Fail
- [ ] B5. StartLive/StopLive: 백그라운드 MdigGrab 루프 → RaiseFrame, 정리
- [ ] B6. TriggerSoftware/param hooks: try/catch best-effort(미지원 무시)
- [ ] B7. Close/Dispose: MbufFree/MdigFree(공유 sys/app은 미해제), 상태 이벤트
- [ ] B8. Enumerate(): 가용 시 Mil/0..N(Matrox/CoaXPress), 미가용 시 빈 목록

### C. Factory/Config/UI/csproj
- [ ] C1. CameraFactory.EnumerateAll에 MilCamera.Enumerate() 추가
- [ ] C2. CameraFactory.CreateById/Create "Mil/" → MilCamera
- [ ] C3. VisionConfig: MilDcfPath/MilSystemDescriptor + OnDeserializing 기본값
- [ ] C4. CameraMappingPanel: MIL 선택 시 DCF/System 입력칸 표시·저장·로드
- [ ] C5. csproj: 참조 + lib\ 벤더링 + Compile 2개 등록

### D. 빌드/검증
- [ ] D1. 빌드 0 error, 0 warning(신규)
- [ ] D2. Handler(QMC.CDT-320) 무변경
- [ ] D3. 앱 기동 OK(= MIL 참조가 로드 실패로 앱을 죽이지 않음)
- [ ] D4. 카메라 검색에 Mil/0..3 노출(모니터 2)
- [ ] D5. MIL 미가용/카메라 없음 시 Sim fallback(앱 정상)
- [ ] D6. (사용자) 카메라 연결 후 실 grab — 사용자 검증 항목
