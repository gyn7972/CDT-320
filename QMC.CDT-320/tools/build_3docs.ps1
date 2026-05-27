# build_3docs.ps1 - Build 3 QMC-style PPTX docs (Design / Plan / Checklist)
. "D:\Work\CDT-320\QMC.CDT-320\tools\make_pptx_v2.ps1"

$root = "D:\Work\CDT-320\문서"
if (-not (Test-Path $root)) { New-Item -ItemType Directory -Path $root -Force | Out-Null }

$today = Get-Date -Format "yyyy-MM-dd"

# ════════════════════════════════════════════════════════════════════════════
# DOCUMENT 1: 설계도 (Design Document)
# ════════════════════════════════════════════════════════════════════════════

$design = @(
    # ── 표지
    @{ kind="title"; title="CDT-320 시스템 설계도";
       lines=@(
         "Die Transfer Handler — System Architecture & Design",
         "",
         "QMC Engineering",
         "Document ID: QMC-CDT320-DSN-001",
         "Revision: A.5 (Stage 27 — Feeder Integration)",
         "Date: $today"
       )
    }

    # ── §1 시스템 개요
    @{ kind="section"; title="§1. 시스템 개요"; subtitle="System Overview" }

    @{ kind="bullets"; title="1.1 — 시스템 구성"; lines=@(
        "## 3개 프로세스 분산 아키텍처",
        "QMC.CDT-320 (메인 핸들러) — WinForms .NET 4.7.2, 5단계 사용자 권한, 6 메인 탭",
        "QMC.Vision (비전 PC) — 별도 프로세스, TCP 5100/5101/5103 listen",
        "CDT320Simulator (3D 뷰어) — WPF + HelixToolkit, TCP 7001 listen",
        "",
        "## 통신 방식",
        "Vision ↔ Handler: 라인 프로토콜 (MODULE | CMD | args)",
        "Simulator ↔ Handler: B-plan JSON (master/viewer with HELLO)",
        "공용 라이브러리: QMC.Common (Motion/IO 추상화)"
    ) }

    @{ kind="diagram"; title="1.2 — 시스템 블록 다이어그램"; shapes=@(
        @{ kind="box"; name="Vision";    x=400000;  y=2200000; w=2200000; h=1200000; fill="00BCD4"; color="FFFFFF"; text="QMC.Vision`n(Cognex / OpenCV)`nTCP 5100/5101/5103"; textSize=1100; bold=$true }
        @{ kind="box"; name="Handler";   x=4400000; y=2200000; w=3300000; h=1200000; fill="E85D1A"; color="FFFFFF"; text="QMC.CDT-320`n(메인 핸들러 / WinForms)`n6 탭 + 5단계 권한"; textSize=1200; bold=$true }
        @{ kind="box"; name="Simulator"; x=9100000; y=2200000; w=2200000; h=1200000; fill="70AD47"; color="FFFFFF"; text="CDT320Simulator`n(WPF 3D)`nTCP 7001"; textSize=1100; bold=$true }
        @{ kind="box"; name="Real";      x=4400000; y=4200000; w=3300000; h=900000;  fill="2D2D30"; color="FFFFFF"; text="AJINEXTEK AXL Board (실보드)`n37 axes + DI/DO + Cylinders"; textSize=1100; bold=$true }
        @{ kind="arrow"; name="vh"; x1=2600000; y1=2800000; x2=4400000; y2=2800000 }
        @{ kind="arrow"; name="hv"; x1=4400000; y1=3000000; x2=2600000; y2=3000000 }
        @{ kind="arrow"; name="hs"; x1=7700000; y1=2800000; x2=9100000; y2=2800000 }
        @{ kind="arrow"; name="hr"; x1=6050000; y1=3400000; x2=6050000; y2=4200000 }
        @{ kind="text"; name="vh-l"; x=2700000; y=2900000; w=1700000; h=300000; text="MODULE|CMD"; size=900; color="555555" }
        @{ kind="text"; name="hs-l"; x=7800000; y=2700000; w=1300000; h=300000; text="JSON HELLO"; size=900; color="555555" }
        @{ kind="text"; name="hr-l"; x=6200000; y=3700000; w=2400000; h=300000; text="P/Invoke (AXL.dll)"; size=900; color="555555" }
    ) }

    # ── §2 Handler 아키텍처
    @{ kind="section"; title="§2. Handler 아키텍처"; subtitle="QMC.CDT-320 Internal Architecture" }

    @{ kind="diagram"; title="2.1 — Handler 4-Layer 아키텍처"; shapes=@(
        @{ kind="box"; name="L1"; x=600000; y=1500000; w=10900000; h=900000; fill="E85D1A"; color="FFFFFF"; text="UI Layer (Form1 + 6 Tabs + 50+ Pages + 17 Dialogs)"; bold=$true }
        @{ kind="box"; name="L2"; x=600000; y=2600000; w=10900000; h=900000; fill="D97706"; color="FFFFFF"; text="Controller Layer (MachineController — Init/Cycle/StepRun/Interlock 검증)"; bold=$true }
        @{ kind="box"; name="L3"; x=600000; y=3700000; w=10900000; h=900000; fill="595959"; color="FFFFFF"; text="Equipment Layer (CDT320_Machine + 6 Units: InputLoader, InputStage, TransferPicker, VisionInsp, OutputStage, OutputUnloader)"; bold=$true }
        @{ kind="box"; name="L4"; x=600000; y=4800000; w=10900000; h=900000; fill="2D2D30"; color="FFFFFF"; text="HAL Layer (BaseAxis / BaseDigitalIO / BaseCylinder + AjinFactory: Sim ↔ AjinAxis 자동 분기)"; bold=$true }
        @{ kind="text"; name="dn1"; x=600000; y=2400000; w=10900000; h=200000; text="↓"; size=1800; color="555555"; align="ctr" }
        @{ kind="text"; name="dn2"; x=600000; y=3500000; w=10900000; h=200000; text="↓"; size=1800; color="555555"; align="ctr" }
        @{ kind="text"; name="dn3"; x=600000; y=4600000; w=10900000; h=200000; text="↓"; size=1800; color="555555"; align="ctr" }
        @{ kind="text"; name="dn4"; x=600000; y=5800000; w=10900000; h=300000; text="실보드 (UseAjin=true) ← AjinSystem.Open(IRQ) → P/Invoke AXL.dll"; size=1100; color="333333"; align="ctr"; bold=$true }
    ) }

    @{ kind="bullets"; title="2.2 — UI Layer 구조"; lines=@(
        "## 6개 메인 탭",
        "Work / WorkInfo / History / Recipe / Settings / User",
        "",
        "## 사이드바 패턴 — TabBase",
        "각 탭은 PnlSidebar (210px Right Dock) + PnlContent (Fill) 로 구성",
        "AddSidebarButton(i18nKey, level, factory) — 버튼+lazy 페이지 등록",
        "Stage 26 — toBottomArea=true 1회 분리선 + 단일 스크롤로 통합",
        "",
        "## 권한 시스템 — UserLevel",
        "None / Operator / Engineer / Maintenance / Admin",
        "Tag = i18n:KEY;level:LEVEL → AccessControl.Apply 가 hide/disable",
        "",
        "## 다국어 (i18n)",
        "Lang.cs: dictionary lookup, Tag = i18n:KEY 패턴",
        "Stage 26 — Tag.IndexOf(';') 로 키 분리"
    ) }

    @{ kind="bullets"; title="2.3 — Controller Layer"; lines=@(
        "## MachineController (Equipment/MachineController.cs)",
        "InitAsync — 모든 축 servoOn + HomeSearch + (Stage 26) 카세트 자동 매핑",
        "StartAsync / StopAsync — 운전 준비 / 정지",
        "CycleRunAsync(N) — 자동 사이클 N 다이",
        "DoOneDieAsync(idx) — 다이 1개 처리 (Pick→Vision→Insp→Place→Bin)",
        "MoveAxisAsync — InterlockRegistry 검증 후 실 이동",
        "AlignWaferAsync — 3-point Vision Alignment + CoordinateMap",
        "",
        "## Stage 27 — Feeder 시퀀스 헬퍼",
        "ScanInputCassetteAsync / LoadNextWaferAsync / RetractCurrentWaferAsync",
        "ScanOutputCassettesAsync / StoreCompletedWaferAsync(isGood)",
        "DiesPerWafer (8) / WafersPerOutputBatch (8)"
    ) }

    @{ kind="bullets"; title="2.4 — Equipment Layer (6 Units)"; lines=@(
        "## CDT320_Machine — 루트 (Composite Pattern)",
        "Units: InputLoader / InputStage / TransferPicker / VisionInsp / OutputStage / OutputUnloader",
        "",
        "## InputLoaderUnit",
        "ElevatorZ / FeederY 축 + FeederUpDownCyl + FeederClampCyl",
        "센서: CassetteExist / Protrusion / WaferDetect / WaferClamped",
        "메서드: ScanCassetteAsync / MoveToTargetSlotAsync / MoveToExchangePositionAsync / RetractFeederAsync",
        "",
        "## OutputUnloaderUnit",
        "ElevatorZ / FeederY 축 + 동일 실린더 (3 카세트 NG/Good1/Good2)",
        "메서드: ScanAllCassettesAsync / StoreFullWaferAsync / SupplyEmptyWaferAsync / ExchangeWaferSequenceAsync",
        "Stage 27 — OutputUnloaderAdapter 로 OutputStage 에 주입"
    ) }

    @{ kind="bullets"; title="2.5 — HAL (Hardware Abstraction)"; lines=@(
        "## QMC.Common.Motion.BaseAxis",
        "MoveAbsoluteAsync / MoveRelativeAsync / HomeSearchAsync / EStop / ServoOn / ResetAlarm",
        "이벤트: ActualPositionChanged / MoveCompleted",
        "",
        "## QMC.Common.IO.BaseDigitalInput",
        "10ms 백그라운드 폴링 (UpdateStatus)",
        "SimulateInput(state) — 시뮬 모드 강제 주입",
        "WaitUntilStateAsync(target, timeoutMs) — 시퀀스 대기",
        "",
        "## QMC.Common.IO.BaseCylinder",
        "OutFwd/OutBwd (DO) + InFwd/InBwd (DI) 캡슐화",
        "MoveFwdAsync / MoveBwdAsync — 양솔/편솔 구분, sim 500ms 자동 토글",
        "",
        "## AjinFactory (Sim ↔ Real 분기)",
        "UseRealBoard + AjinSystem.IsOpen 시 → AjinAxis/AjinDigitalIO",
        "그 외 → SimAxis/SimDigitalInput (안전 fallback)"
    ) }

    # ── §3 데이터 흐름
    @{ kind="section"; title="§3. 데이터 흐름"; subtitle="Cycle Data Flow & Pipeline" }

    @{ kind="diagram"; title="3.1 — 사이클 파이프라인 (Stage 27 통합)"; shapes=@(
        @{ kind="box"; name="IL"; x=300000;  y=1500000; w=1700000; h=1100000; fill="00BCD4"; color="FFFFFF"; text="① Input Loader`n카세트 → Feeder"; bold=$true; textSize=1000 }
        @{ kind="box"; name="IS"; x=2200000; y=1500000; w=1700000; h=1100000; fill="00BCD4"; color="FFFFFF"; text="② Input Stage`n웨이퍼 정렬"; bold=$true; textSize=1000 }
        @{ kind="box"; name="TP"; x=4100000; y=1500000; w=1700000; h=1100000; fill="E85D1A"; color="FFFFFF"; text="③ Picker`n다이 픽업"; bold=$true; textSize=1000 }
        @{ kind="box"; name="VI"; x=6000000; y=1500000; w=1700000; h=1100000; fill="D97706"; color="FFFFFF"; text="④ Vision Insp`n5면 검사"; bold=$true; textSize=1000 }
        @{ kind="box"; name="OS"; x=7900000; y=1500000; w=1700000; h=1100000; fill="595959"; color="FFFFFF"; text="⑤ Output Stage`nGood/NG 분류"; bold=$true; textSize=1000 }
        @{ kind="box"; name="OU"; x=9800000; y=1500000; w=1700000; h=1100000; fill="2D2D30"; color="FFFFFF"; text="⑥ Output Unloader`n3 카세트 적재"; bold=$true; textSize=1000 }
        @{ kind="arrow"; name="a1"; x1=2000000; y1=2050000; x2=2200000; y2=2050000 }
        @{ kind="arrow"; name="a2"; x1=3900000; y1=2050000; x2=4100000; y2=2050000 }
        @{ kind="arrow"; name="a3"; x1=5800000; y1=2050000; x2=6000000; y2=2050000 }
        @{ kind="arrow"; name="a4"; x1=7700000; y1=2050000; x2=7900000; y2=2050000 }
        @{ kind="arrow"; name="a5"; x1=9600000; y1=2050000; x2=9800000; y2=2050000 }
        @{ kind="text"; name="lab"; x=300000; y=2900000; w=11200000; h=400000; text="좌측 → 우측 단방향 파이프라인. 모든 단계는 인터락 검증 + Lot Storage 추적."; size=1300; bold=$true; align="ctr" }
        @{ kind="box"; name="L1"; x=300000;  y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="ScanCassette`nLoadNextWafer"; textSize=900 }
        @{ kind="box"; name="L2"; x=2200000; y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="LoadAndPrepare`nVisionAlign"; textSize=900 }
        @{ kind="box"; name="L3"; x=4100000; y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="MoveTo PICK`nVacuum ON"; textSize=900 }
        @{ kind="box"; name="L4"; x=6000000; y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="Expose`nInspect"; textSize=900 }
        @{ kind="box"; name="L5"; x=7900000; y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="MoveTo PLACE`nVacuum OFF"; textSize=900 }
        @{ kind="box"; name="L6"; x=9800000; y=3700000; w=1700000; h=600000; fill="F0F0F0"; color="000000"; text="StoreFullWafer`n(Stage 27)"; textSize=900 }
        @{ kind="text"; name="ev"; x=300000; y=4500000; w=11200000; h=300000; text="이벤트: 매 8 다이마다 Stage 26: 다음 슬롯 / Stage 27: Output 카세트 적재"; size=1100; align="ctr"; color="E85D1A"; bold=$true }
        @{ kind="box"; name="LS"; x=2000000; y=5100000; w=8000000; h=900000; fill="F5C718"; color="000000"; text="LotStorage — OpenLot / CloseLot / Die 통계 (Good/Ng/Yield)"; bold=$true; textSize=1300 }
    ) }

    # ── §4 Stage 27 상세
    @{ kind="section"; title="§4. Stage 27 — Feeder 통합 설계"; subtitle="Feeder Sequence Integration Design" }

    @{ kind="diagram"; title="4.1 — Feeder 통합 전후 비교"; shapes=@(
        @{ kind="text"; name="bef"; x=300000; y=1400000; w=5500000; h=400000; text="BEFORE (Stage 26)"; size=1800; bold=$true; color="999999"; align="ctr" }
        @{ kind="box"; name="b1"; x=400000; y=1900000; w=5300000; h=600000; fill="F0F0F0"; color="000000"; text="① InputLoader feeder ✓"; textSize=1100 }
        @{ kind="box"; name="b2"; x=400000; y=2600000; w=5300000; h=600000; fill="FFE0CC"; color="333333"; text="② DoOneDie 8마다 다음 슬롯 ✓"; textSize=1100 }
        @{ kind="box"; name="b3"; x=400000; y=3300000; w=5300000; h=600000; fill="FFCCCC"; color="333333"; text="③ OutputUnloader 메서드 ✓"; textSize=1100 }
        @{ kind="box"; name="b4"; x=400000; y=4000000; w=5300000; h=600000; fill="FFCCCC"; color="990000"; text="✗ 사이클 호출 없음 (NullObject)"; textSize=1100; bold=$true }
        @{ kind="box"; name="b5"; x=400000; y=4700000; w=5300000; h=600000; fill="FFCCCC"; color="990000"; text="✗ FeederPage UI 정적"; textSize=1100; bold=$true }

        @{ kind="text"; name="aft"; x=6300000; y=1400000; w=5500000; h=400000; text="AFTER (Stage 27)"; size=1800; bold=$true; color="E85D1A"; align="ctr" }
        @{ kind="box"; name="a1"; x=6400000; y=1900000; w=5300000; h=600000; fill="F0F0F0"; color="000000"; text="① InputLoader feeder ✓"; textSize=1100 }
        @{ kind="box"; name="a2"; x=6400000; y=2600000; w=5300000; h=600000; fill="F0F0F0"; color="000000"; text="② DoOneDie 8마다 다음 슬롯 ✓"; textSize=1100 }
        @{ kind="box"; name="a3"; x=6400000; y=3300000; w=5300000; h=600000; fill="F0F0F0"; color="000000"; text="③ OutputUnloader 메서드 ✓"; textSize=1100 }
        @{ kind="box"; name="a4"; x=6400000; y=4000000; w=5300000; h=600000; fill="CCFFCC"; color="006600"; text="✓ DoOneDie 8마다 StoreCompletedWafer"; textSize=1100; bold=$true }
        @{ kind="box"; name="a5"; x=6400000; y=4700000; w=5300000; h=600000; fill="CCFFCC"; color="006600"; text="✓ FeederPage 라이브 + 액션 5/4 버튼"; textSize=1100; bold=$true }
        @{ kind="box"; name="a6"; x=6400000; y=5400000; w=5300000; h=600000; fill="CCFFCC"; color="006600"; text="✓ OutputUnloaderAdapter (NullObj 교체)"; textSize=1100; bold=$true }
    ) }

    @{ kind="diagram"; title="4.2 — Stage 27 시퀀스 다이어그램"; shapes=@(
        @{ kind="box"; name="ctrl"; x=400000; y=1400000; w=2200000; h=600000; fill="E85D1A"; color="FFFFFF"; text="MachineController"; bold=$true }
        @{ kind="box"; name="ada"; x=3300000; y=1400000; w=2200000; h=600000; fill="D97706"; color="FFFFFF"; text="OutputUnloaderAdapter"; bold=$true }
        @{ kind="box"; name="unl"; x=6200000; y=1400000; w=2200000; h=600000; fill="2D2D30"; color="FFFFFF"; text="OutputUnloaderUnit"; bold=$true }
        @{ kind="box"; name="sim"; x=9100000; y=1400000; w=2200000; h=600000; fill="595959"; color="FFFFFF"; text="SimCassetteDriver"; bold=$true }
        @{ kind="text"; name="t1"; x=400000; y=2200000; w=11200000; h=300000; text="DoOneDieAsync(idx=7) → StoreCompletedWaferAsync(isGood=true)"; size=1100; bold=$true }
        @{ kind="arrow"; name="ar1"; x1=2600000; y1=2400000; x2=3300000; y2=2400000 }
        @{ kind="text"; name="t2"; x=400000; y=2800000; w=11200000; h=300000; text="→ unloader.StoreFullWaferAsync(Good1, slot=0)"; size=1100; bold=$true }
        @{ kind="arrow"; name="ar2"; x1=5500000; y1=3000000; x2=6200000; y2=3000000 }
        @{ kind="text"; name="t3"; x=400000; y=3400000; w=11200000; h=300000; text="→ PickupWaferAtPositionAsync(stageY=Good1Exchange)"; size=1100 }
        @{ kind="text"; name="t4"; x=400000; y=3800000; w=11200000; h=300000; text="→ ElevatorZ MoveAbsolute(slotZ) — ProtrusionGuard 병렬 폴링"; size=1100 }
        @{ kind="arrow"; name="ar3"; x1=8400000; y1=3950000; x2=9100000; y2=3950000 }
        @{ kind="text"; name="t5"; x=400000; y=4200000; w=11200000; h=300000; text="→ FeederClampCyl InFwd ON ↔ WaferClampedSensor SimulateInput(true)"; size=1100; color="00BCD4" }
        @{ kind="text"; name="t6"; x=400000; y=4600000; w=11200000; h=300000; text="→ PlaceWaferAtPositionAsync(CassetteInsertY=250mm) — clamp 해제"; size=1100 }
        @{ kind="text"; name="t7"; x=400000; y=5000000; w=11200000; h=300000; text="→ slotMap[Good1][0] = true (적재 완료)"; size=1100; color="006600"; bold=$true }
        @{ kind="box"; name="rt"; x=2000000; y=5500000; w=8000000; h=700000; fill="CCFFCC"; color="006600"; text="런타임 검증: auto-cycle 16 다이 → idx 7, 15 두 번 호출 확인"; bold=$true; textSize=1200 }
    ) }

    @{ kind="bullets"; title="4.3 — SimCassetteDriver 확장"; lines=@(
        "## 책임",
        "시뮬레이션 모드에서 카세트 관련 DI 센서를 모방하는 드라이버",
        "InputLoader 4개 센서 + OutputUnloader 6개 센서 = 총 10개 센서",
        "",
        "## Input 측",
        "CassetteExistSensor.SimulateInput(true) — 카세트 안착",
        "ProtrusionSensor OFF — 돌출 없음",
        "ElevatorZ.MoveCompleted → 위치 환산 → WaferDetectSensor 토글",
        "FeederClampCyl.InFwd.StateChanged ↔ WaferClampedSensor",
        "",
        "## Output 측 (Stage 27 신규)",
        "ExistSensor_NG / Good1 / Good2 모두 ON",
        "ProtrusionSensor OFF",
        "ElevatorZ.MoveCompleted → NG/Good1/Good2 3 카세트 위치 자동 판별 → WaferDetect",
        "FeederClampCyl.InFwd.StateChanged ↔ WaferClampedSensor"
    ) }

    # ── §5 인터락 시스템
    @{ kind="section"; title="§5. 인터락 시스템"; subtitle="Interlock Registry — 25 Items" }

    @{ kind="bullets"; title="5.1 — InterlockRegistry"; lines=@(
        "## 25개 인터락 (Stage 1~25 누적)",
        "5 Standard: PickerVsStage / PickerVsPicker / StageLifter / 등",
        "5 Extended (1): 도어/EMG/Vacuum 라인",
        "5 Stage 8: 추가 안전 인터락",
        "5 ExtendedInterlocks2: 비전 라이트 / 카메라 충돌",
        "5 ExtendedInterlocks3: DoorVsAll / WaferVisionZVsStageLifter / VacuumVsPicker / BinLidVsBinVision / ServoOff",
        "",
        "## 호출 패턴",
        "InterlockRegistry.VerifyMove(axisName, position, out reason) → bool",
        "MoveAxisAsync 가 검증 후 차단 시 AlarmManager.Raise + false 반환",
        "",
        "## Feeder 측 인터락",
        "InputLoader.MoveToTargetSlotAsync — ProtrusionSensor 10ms 폴링 병렬 감시",
        "OutputUnloader.MoveElevatorWithProtrusionGuardAsync — 동일 패턴"
    ) }

    # ── §6 결론
    @{ kind="section"; title="§6. 결론"; subtitle="Conclusion" }

    @{ kind="bullets"; title="6.1 — 설계 완성도"; lines=@(
        "## 4-Layer 아키텍처 — 명확한 책임 분리",
        "UI / Controller / Equipment / HAL 각각 독립 — 단위 테스트 + 시뮬 가능",
        "",
        "## Sim ↔ Real 자동 분기",
        "AjinFactory.Ready 검사 — UseAjin + IsOpen + 컨피그 매핑 모두 만족 시만 실보드",
        "그 외 모든 경우 Sim* 으로 안전 fallback — 빌드/단독 테스트 보장",
        "",
        "## Stage 27 — 통합 완료",
        "InputLoader + OutputUnloader feeder 모두 사이클에서 호출",
        "InputFeederPage / OutputFeederPage UI 라이브 바인딩 + 액션",
        "",
        "## 검증",
        "verify_all 117/118 PASS — 회귀 무결성 유지",
        "auto-cycle 16 다이 — Stage 27 통합 호출 경로 활성 확인"
    ) }
)

New-Pptx2 -OutPath "$root\01_CDT320_설계도.pptx" `
    -DocTitle "CDT-320 시스템 설계도" `
    -DocAuthor "QMC Engineering" `
    -DocSubject "System Architecture & Design Document" `
    -Slides $design

Write-Output ""
Write-Output "Document 1: 설계도 — 작성 완료"
