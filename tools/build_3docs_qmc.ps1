# build_3docs_qmc.ps1 - QMC template-style 3 docs (재사용 데이터)
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_qmc.ps1"

$root = "D:\Work\CDT-320\문서"
if (-not (Test-Path $root)) { New-Item -ItemType Directory -Path $root -Force | Out-Null }

# ════════════════════════════════════════════════════════════════════════════
# DOCUMENT 1 — 설계도 (Design)
# ════════════════════════════════════════════════════════════════════════════

$design = @(
    @{ kind="title"; title=""; subtitle2="System Architecture & Design Document" }

    @{ kind="section"; title="§1. 시스템 개요"; subtitle="System Overview" }

    @{ kind="bullets"; title="1.1 시스템 구성"; subtitle="3개 프로세스 분산 아키텍처";
       lines=@(
        "## 메인 핸들러",
        "QMC.CDT-320 — WinForms .NET 4.7.2 / 5단계 사용자 권한 / 6 메인 탭",
        "1920×1080 CDT-300 스타일 UI, 한국어/영어 다국어 (i18n)",
        "",
        "## Vision PC",
        "QMC.Vision — 별도 프로세스, TCP 5100 (Wafer) / 5101 (Inspection) / 5103 (Bin)",
        "Cognex VisionPro 25.2 (옵션) + OpenCV + Sim 백엔드 자동 fallback",
        "",
        "## 3D Simulator",
        "CDT320Simulator — WPF + HelixToolkit, TCP 7001 listen",
        "Master/Viewer 모드 (HELLO JSON 으로 결정)",
        "",
        "## 공용 라이브러리",
        "QMC.Common — Motion / IO 추상화 (BaseAxis / BaseDigitalIO / BaseCylinder)",
        "AjinFactory — Sim ↔ Ajin 자동 분기 (UseRealBoard + IsOpen)"
    ) }

    @{ kind="diagram"; title="1.2 시스템 블록 다이어그램"; subtitle="3개 프로세스 + 실보드 인터페이스"; shapes=@(
        @{ kind="box"; name="Vision";    x=300000;  y=1500000; w=1900000; h=1100000; fill="5B9BD5"; color="FFFFFF"; text="QMC.Vision`n(Cognex/OpenCV)`nTCP 5100/5101/5103"; textSize=900; bold=$true }
        @{ kind="box"; name="Handler";   x=2700000; y=1500000; w=2700000; h=1100000; fill="ED7D31"; color="FFFFFF"; text="QMC.CDT-320`n(메인 핸들러)`n6 탭 + 5 권한"; textSize=1100; bold=$true }
        @{ kind="box"; name="Simulator"; x=5900000; y=1500000; w=1900000; h=1100000; fill="70AD47"; color="FFFFFF"; text="CDT320Simulator`n(WPF 3D)`nTCP 7001"; textSize=900; bold=$true }
        @{ kind="box"; name="Real";      x=2700000; y=3100000; w=2700000; h=750000; fill="0F2D4F"; color="FFFFFF"; text="AJINEXTEK AXL 실보드`n37 axes + DI/DO"; textSize=1000; bold=$true }
        @{ kind="arrow"; name="vh"; x1=2200000; y1=2050000; x2=2700000; y2=2050000 }
        @{ kind="arrow"; name="hs"; x1=5400000; y1=2050000; x2=5900000; y2=2050000 }
        @{ kind="arrow"; name="hr"; x1=4050000; y1=2600000; x2=4050000; y2=3100000 }
        @{ kind="text"; name="vh-l"; x=2200000; y=2200000; w=500000; h=200000; text="MODULE|CMD"; size=800; color="595959" }
        @{ kind="text"; name="hs-l"; x=5400000; y=2200000; w=500000; h=200000; text="JSON HELLO"; size=800; color="595959" }
        @{ kind="text"; name="hr-l"; x=4150000; y=2750000; w=1500000; h=200000; text="P/Invoke (AXL.dll)"; size=800; color="595959" }
        @{ kind="text"; name="cap"; x=300000; y=4050000; w=7800000; h=300000; text="동작: Vision↔Handler 라인 프로토콜, Handler↔Simulator JSON, Handler→실보드 P/Invoke"; size=900; color="595959"; align="ctr"; italic=$true }
    ) }

    @{ kind="section"; title="§2. Handler 4-Layer 아키텍처"; subtitle="UI / Controller / Equipment / HAL" }

    @{ kind="diagram"; title="2.1 Handler 내부 4-Layer 구조"; subtitle="책임 분리 + 단위 테스트 가능"; shapes=@(
        @{ kind="box"; name="L1"; x=400000; y=1300000; w=8350000; h=600000; fill="ED7D31"; color="FFFFFF"; text="UI Layer — Form1 + 6 Tabs + 50+ Pages + 17 Dialogs"; bold=$true; textSize=1100 }
        @{ kind="box"; name="L2"; x=400000; y=2100000; w=8350000; h=600000; fill="1F4E79"; color="FFFFFF"; text="Controller Layer — MachineController (Init/Cycle/StepRun/Interlock)"; bold=$true; textSize=1100 }
        @{ kind="box"; name="L3"; x=400000; y=2900000; w=8350000; h=600000; fill="595959"; color="FFFFFF"; text="Equipment Layer — CDT320_Machine + 6 Units + Adapters"; bold=$true; textSize=1100 }
        @{ kind="box"; name="L4"; x=400000; y=3700000; w=8350000; h=600000; fill="0F2D4F"; color="FFFFFF"; text="HAL Layer — BaseAxis / BaseDigitalIO / BaseCylinder + AjinFactory"; bold=$true; textSize=1100 }
        @{ kind="text"; name="d1"; x=400000; y=1950000; w=8350000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d2"; x=400000; y=2750000; w=8350000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d3"; x=400000; y=3550000; w=8350000; h=120000; text="↓"; size=1400; color="595959"; align="ctr" }
        @{ kind="text"; name="d4"; x=400000; y=4450000; w=8350000; h=300000; text="실보드 (UseAjin=true) ← AjinSystem.Open(IRQ) → P/Invoke AXL.dll"; size=1100; bold=$true; align="ctr"; color="333333" }
    ) }

    @{ kind="bullets"; title="2.2 UI Layer 구조"; subtitle="6 Tab + 사이드바 패턴"; lines=@(
        "## 6 메인 탭",
        "Work / WorkInfo / History / Recipe / Settings / User",
        "",
        "## TabBase 사이드바 패턴",
        "PnlSidebar (210px Right Dock) + PnlContent (Fill)",
        "AddSidebarButton(i18nKey, level, factory) — 버튼 + lazy 페이지",
        "Stage 26 — toBottomArea=true 1회 분리선 + 단일 스크롤 통합",
        "",
        "## 다국어 (i18n)",
        "Lang.Apply(Control) — 컨트롤 트리 순회 후 Tag 파싱",
        "Tag = `"i18n:KEY;level:LEVEL`" — Stage 26 fix: Tag.IndexOf(';') 분리",
        "",
        "## 권한 시스템",
        "5 단계: None / Operator / Engineer / Maintenance / Admin",
        "AccessControl.Apply — 사용자 레벨에 따라 컨트롤 hide/disable"
    ) }

    @{ kind="bullets"; title="2.3 Controller Layer"; subtitle="MachineController.cs 의 모든 책임"; lines=@(
        "## 라이프사이클",
        "InitAsync — 모든 축 ServoOn + HomeSearch + 카세트 자동 매핑",
        "StartAsync / StopAsync — 운전 준비 / 비상 정지",
        "CycleRunAsync(N) — 자동 N 다이 사이클",
        "DoOneDieAsync(idx) — 다이 1개 처리",
        "",
        "## 안전 + 검증",
        "MoveAxisAsync — InterlockRegistry.VerifyMove 후 실 이동",
        "AlignWaferAsync — 3-point Vision Alignment + CoordinateMap",
        "",
        "## Stage 26+27 신규",
        "ScanInputCassetteAsync / LoadNextWaferAsync / RetractCurrentWaferAsync",
        "ScanOutputCassettesAsync / StoreCompletedWaferAsync(isGood)",
        "DiesPerWafer (8) / WafersPerOutputBatch (8) — 자동 슬롯 진행"
    ) }

    @{ kind="bullets"; title="2.4 Equipment Layer (6 Units)"; subtitle="Composite Pattern"; lines=@(
        "## 6 Units (CDT320_Machine 루트)",
        "InputLoaderUnit / InputStageUnit / TransferPickerUnit",
        "VisionInspectionUnit / OutputStageUnit / OutputUnloaderUnit",
        "",
        "## InputLoaderUnit (피더 + 카세트)",
        "축: ElevatorZ + FeederY",
        "실린더: FeederUpDownCyl + FeederClampCyl",
        "센서: CassetteExist / Protrusion / WaferDetect / WaferClamped",
        "메서드: ScanCassetteAsync / MoveToTargetSlotAsync / MoveToExchangePositionAsync / RetractFeederAsync",
        "",
        "## OutputUnloaderUnit (3 카세트 NG/Good1/Good2)",
        "축: ElevatorZ (axis 36) + FeederY (axis 35)",
        "메서드: ScanAllCassettesAsync / StoreFullWaferAsync / SupplyEmptyWaferAsync / ExchangeWaferSequenceAsync",
        "Stage 27 — OutputUnloaderAdapter 로 OutputStage 에 주입"
    ) }

    @{ kind="section"; title="§3. 사이클 데이터 흐름"; subtitle="6-Stage Pipeline" }

    @{ kind="diagram"; title="3.1 사이클 파이프라인 (Stage 27 통합)"; subtitle="Input Loader → Output Unloader 단방향"; shapes=@(
        @{ kind="box"; name="IL"; x=300000;  y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="① Input`nLoader"; bold=$true; textSize=1000 }
        @{ kind="box"; name="IS"; x=1700000; y=1300000; w=1300000; h=900000; fill="5B9BD5"; color="FFFFFF"; text="② Input`nStage"; bold=$true; textSize=1000 }
        @{ kind="box"; name="TP"; x=3100000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="③ Picker"; bold=$true; textSize=1000 }
        @{ kind="box"; name="VI"; x=4500000; y=1300000; w=1300000; h=900000; fill="ED7D31"; color="FFFFFF"; text="④ Vision`nInsp"; bold=$true; textSize=1000 }
        @{ kind="box"; name="OS"; x=5900000; y=1300000; w=1300000; h=900000; fill="595959"; color="FFFFFF"; text="⑤ Output`nStage"; bold=$true; textSize=1000 }
        @{ kind="box"; name="OU"; x=7300000; y=1300000; w=1300000; h=900000; fill="0F2D4F"; color="FFFFFF"; text="⑥ Output`nUnloader"; bold=$true; textSize=1000 }
        @{ kind="arrow"; name="a1"; x1=1600000; y1=1750000; x2=1700000; y2=1750000 }
        @{ kind="arrow"; name="a2"; x1=3000000; y1=1750000; x2=3100000; y2=1750000 }
        @{ kind="arrow"; name="a3"; x1=4400000; y1=1750000; x2=4500000; y2=1750000 }
        @{ kind="arrow"; name="a4"; x1=5800000; y1=1750000; x2=5900000; y2=1750000 }
        @{ kind="arrow"; name="a5"; x1=7200000; y1=1750000; x2=7300000; y2=1750000 }
        @{ kind="text"; name="cap"; x=300000; y=2400000; w=8400000; h=300000; text="좌측 → 우측 단방향 파이프라인. 모든 단계는 인터락 검증 + Lot Storage 추적."; size=1100; bold=$true; align="ctr"; color="595959" }
        @{ kind="box"; name="L1"; x=300000;  y=2900000; w=1300000; h=600000; fill="F2F2F2"; color="333333"; text="ScanCassette`nLoadNextWafer"; textSize=850 }
        @{ kind="box"; name="L2"; x=1700000; y=2900000; w=1300000; h=600000; fill="F2F2F2"; color="333333"; text="VisionAlign"; textSize=850 }
        @{ kind="box"; name="L3"; x=3100000; y=2900000; w=1300000; h=600000; fill="F2F2F2"; color="333333"; text="Pick @ 300mm`nVacuum ON"; textSize=850 }
        @{ kind="box"; name="L4"; x=4500000; y=2900000; w=1300000; h=600000; fill="F2F2F2"; color="333333"; text="Expose`nInspect"; textSize=850 }
        @{ kind="box"; name="L5"; x=5900000; y=2900000; w=1300000; h=600000; fill="F2F2F2"; color="333333"; text="Place @ 1200mm`nVacuum OFF"; textSize=850 }
        @{ kind="box"; name="L6"; x=7300000; y=2900000; w=1300000; h=600000; fill="ED7D31"; color="FFFFFF"; text="StoreFullWafer`n(Stage 27)"; textSize=850; bold=$true }
        @{ kind="text"; name="ev"; x=300000; y=3700000; w=8400000; h=300000; text="이벤트: 매 8 다이마다 — Stage 26: 다음 슬롯 / Stage 27: Output 카세트 적재"; size=1000; align="ctr"; color="ED7D31"; bold=$true }
        @{ kind="box"; name="LS"; x=2100000; y=4200000; w=4800000; h=550000; fill="FFC000"; color="000000"; text="LotStorage — OpenLot / CloseLot / Die 통계 (Good/NG/Yield%)"; bold=$true; textSize=1000 }
    ) }

    @{ kind="section"; title="§4. Stage 27 — Feeder 통합 설계"; subtitle="Feeder Sequence Integration" }

    @{ kind="diagram"; title="4.1 Stage 26 → 27 변화"; subtitle="3-Layer 통합 격차 해소"; shapes=@(
        @{ kind="text"; name="bef"; x=300000; y=1300000; w=4000000; h=350000; text="BEFORE (Stage 26)"; size=1500; bold=$true; color="A5A5A5"; align="ctr" }
        @{ kind="box"; name="b1"; x=400000; y=1750000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="① InputLoader feeder ✓ 통합됨"; textSize=1000 }
        @{ kind="box"; name="b2"; x=400000; y=2200000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="② DoOneDie 8마다 다음 슬롯 ✓"; textSize=1000 }
        @{ kind="box"; name="b3"; x=400000; y=2650000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="③ OutputUnloader 메서드 ✓"; textSize=1000 }
        @{ kind="box"; name="b4"; x=400000; y=3100000; w=3800000; h=400000; fill="FCE4D6"; color="C00000"; text="✗ 사이클 호출 0건 (NullObject)"; textSize=1000; bold=$true }
        @{ kind="box"; name="b5"; x=400000; y=3550000; w=3800000; h=400000; fill="FCE4D6"; color="C00000"; text="✗ FeederPage UI 정적 더미"; textSize=1000; bold=$true }

        @{ kind="text"; name="aft"; x=4900000; y=1300000; w=4000000; h=350000; text="AFTER (Stage 27)"; size=1500; bold=$true; color="ED7D31"; align="ctr" }
        @{ kind="box"; name="a1"; x=5000000; y=1750000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="① InputLoader feeder ✓"; textSize=1000 }
        @{ kind="box"; name="a2"; x=5000000; y=2200000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="② DoOneDie 8마다 다음 슬롯 ✓"; textSize=1000 }
        @{ kind="box"; name="a3"; x=5000000; y=2650000; w=3800000; h=400000; fill="F2F2F2"; color="333333"; text="③ OutputUnloader 메서드 ✓"; textSize=1000 }
        @{ kind="box"; name="a4"; x=5000000; y=3100000; w=3800000; h=400000; fill="E2EFDA"; color="375623"; text="✓ DoOneDie 8마다 StoreCompletedWafer"; textSize=1000; bold=$true }
        @{ kind="box"; name="a5"; x=5000000; y=3550000; w=3800000; h=400000; fill="E2EFDA"; color="375623"; text="✓ 2 FeederPage 라이브 + 9 액션"; textSize=1000; bold=$true }
        @{ kind="box"; name="a6"; x=5000000; y=4000000; w=3800000; h=400000; fill="E2EFDA"; color="375623"; text="✓ OutputUnloaderAdapter 주입"; textSize=1000; bold=$true }
    ) }

    @{ kind="diagram"; title="4.2 Stage 27 호출 시퀀스"; subtitle="Controller → Adapter → Unit → Sim Driver"; shapes=@(
        @{ kind="box"; name="ctrl"; x=300000; y=1300000; w=1900000; h=600000; fill="ED7D31"; color="FFFFFF"; text="MachineController"; bold=$true; textSize=1000 }
        @{ kind="box"; name="ada"; x=2400000; y=1300000; w=2000000; h=600000; fill="1F4E79"; color="FFFFFF"; text="OutputUnloaderAdapter"; bold=$true; textSize=1000 }
        @{ kind="box"; name="unl"; x=4600000; y=1300000; w=1900000; h=600000; fill="0F2D4F"; color="FFFFFF"; text="OutputUnloaderUnit"; bold=$true; textSize=1000 }
        @{ kind="box"; name="sim"; x=6700000; y=1300000; w=2000000; h=600000; fill="595959"; color="FFFFFF"; text="SimCassetteDriver"; bold=$true; textSize=1000 }
        @{ kind="text"; name="t1"; x=300000; y=2050000; w=8400000; h=250000; text="① DoOneDieAsync(idx=7) → StoreCompletedWaferAsync(isGood=true)"; size=950; bold=$true }
        @{ kind="arrow"; name="ar1"; x1=2200000; y1=2200000; x2=2400000; y2=2200000 }
        @{ kind="text"; name="t2"; x=300000; y=2400000; w=8400000; h=250000; text="② → unloader.StoreFullWaferAsync(Good1, slot=0)"; size=950; bold=$true }
        @{ kind="arrow"; name="ar2"; x1=4400000; y1=2550000; x2=4600000; y2=2550000 }
        @{ kind="text"; name="t3"; x=300000; y=2750000; w=8400000; h=250000; text="③ → PickupWaferAtPositionAsync(stageY=Good1Exchange)"; size=950 }
        @{ kind="text"; name="t4"; x=300000; y=3050000; w=8400000; h=250000; text="④ → ElevatorZ MoveAbsolute — ProtrusionGuard 병렬 폴링"; size=950 }
        @{ kind="arrow"; name="ar3"; x1=6500000; y1=3200000; x2=6700000; y2=3200000 }
        @{ kind="text"; name="t5"; x=300000; y=3400000; w=8400000; h=250000; text="⑤ → FeederClampCyl InFwd ON ↔ WaferClampedSensor SimulateInput(true)"; size=950; color="1F4E79" }
        @{ kind="text"; name="t6"; x=300000; y=3700000; w=8400000; h=250000; text="⑥ → PlaceWaferAtPositionAsync(CassetteInsertY=250mm) — clamp 해제"; size=950 }
        @{ kind="text"; name="t7"; x=300000; y=4000000; w=8400000; h=250000; text="⑦ → slotMap[Good1][0] = true (적재 완료)"; size=950; color="375623"; bold=$true }
        @{ kind="box"; name="rt"; x=2000000; y=4400000; w=5000000; h=450000; fill="E2EFDA"; color="375623"; text="런타임 검증: auto-cycle 16 다이 → idx 7, 15 두 번 호출 확인"; bold=$true; textSize=1050 }
    ) }

    @{ kind="bullets"; title="4.3 SimCassetteDriver 확장"; subtitle="Output 측 6 센서 모방"; lines=@(
        "## 책임",
        "시뮬레이션 모드에서 카세트 관련 DI 센서를 모방하는 드라이버",
        "InputLoader 4 + OutputUnloader 6 = 총 10개 센서 관리",
        "",
        "## Input 측 (Stage 26)",
        "CassetteExistSensor.SimulateInput(true) — 카세트 안착",
        "ProtrusionSensor OFF — 돌출 없음",
        "ElevatorZ.MoveCompleted → 위치 환산 → WaferDetect",
        "FeederClampCyl.InFwd.StateChanged ↔ WaferClamped",
        "",
        "## Output 측 (Stage 27 신규)",
        "ExistSensor_NG / Good1 / Good2 모두 ON",
        "ProtrusionSensor OFF",
        "ElevatorZ.MoveCompleted → NG/Good1/Good2 3 카세트 위치 자동 판별 → WaferDetect",
        "FeederClampCyl.InFwd.StateChanged ↔ WaferClamped"
    ) }

    @{ kind="section"; title="§5. 결론"; subtitle="Conclusion" }

    @{ kind="bullets"; title="5.1 설계 완성도"; subtitle="배포 가능 수준"; lines=@(
        "## 4-Layer 아키텍처 — 명확한 책임 분리",
        "UI / Controller / Equipment / HAL — 각 레이어 독립 + 단위 테스트",
        "",
        "## Sim ↔ Real 자동 분기",
        "AjinFactory.Ready 검사 — UseAjin + IsOpen + 컨피그 매핑 모두 만족 시만 실보드",
        "그 외 모든 경우 Sim* 으로 안전 fallback",
        "",
        "## Stage 27 통합 완료",
        "InputLoader + OutputUnloader feeder 모두 사이클에서 호출",
        "InputFeederPage / OutputFeederPage UI 라이브 바인딩 + 9 액션",
        "",
        "## 검증",
        "verify_all 117/118 PASS — 회귀 무결성 유지",
        "auto-cycle 16 다이 — Stage 27 통합 호출 경로 활성 확인"
    ) }
)

New-PptxQmc -OutPath "$root\01_CDT320_설계도.pptx" `
    -DocTitle "CDT-320" `
    -DocSubtitle "System Design Document" `
    -DocSubject "System Architecture & Design" `
    -DocAuthor "QMC Engineering" `
    -SubmittedTo "ASE" `
    -DocDate "2026. 04. 28" `
    -Slides $design

Write-Output ""
Write-Output "Document 1: 설계도 (QMC template) 작성 완료"
